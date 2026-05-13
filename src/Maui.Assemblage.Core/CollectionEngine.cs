using Maui.Assemblage.Core.Collections;
using Maui.Assemblage.Core.Data;
using Maui.Assemblage.Core.Interactions;
using Maui.Assemblage.Core.Layout;
using Maui.Assemblage.Core.Realization;

namespace Maui.Assemblage.Core;

/// <summary>
/// Central orchestrator that wires data → flatten → layout → realization → recycling.
/// This is the shared-code engine; the MAUI control layer delegates all logic here.
/// </summary>
public sealed class CollectionEngine
{
    private ICollectionDataSource? _dataSource;
    private ILayoutProvider? _layoutProvider;
    private IRealizationStrategy _realizationStrategy = new WindowedRealizationStrategy();
    private CollectionNodeFlattenOptions _flattenOptions = new();
    private readonly SelectionTracker _selectionTracker = new();

    private IReadOnlyList<CollectionNode> _nodes = [];
    private SectionIndexMap? _sectionIndexMap;
    private LayoutSnapshot? _lastSnapshot;
    private double _scrollOffset;
    private double _viewportWidth;
    private double _viewportHeight;
    private int _cacheBefore = 5;
    private int _cacheAfter = 5;

    // Realized item tracking
    private readonly HashSet<int> _realizedIndices = [];
    private bool _isApplyingChanges;

    /// <summary>Current flattened nodes.</summary>
    public IReadOnlyList<CollectionNode> Nodes => _nodes;

    /// <summary>Current section index map.</summary>
    public SectionIndexMap? SectionIndexMap => _sectionIndexMap;

    /// <summary>Last computed layout snapshot.</summary>
    public LayoutSnapshot? LastSnapshot => _lastSnapshot;

    /// <summary>Selection tracker for the engine.</summary>
    public SelectionTracker Selection => _selectionTracker;

    /// <summary>Whether sticky section headers are enabled. Default is true.</summary>
    public bool StickyHeaders { get; set; } = true;
    public int CacheBefore { get => _cacheBefore; set => _cacheBefore = Math.Max(0, value); }

    /// <summary>Number of extra items to realize after visible range.</summary>
    public int CacheAfter { get => _cacheAfter; set => _cacheAfter = Math.Max(0, value); }

    /// <summary>
    /// Axis used for scroll-offset calculations (Vertical = Y, Horizontal = X).
    /// Set this to match the layout orientation.
    /// </summary>
    public LayoutOrientation ScrollAxis { get; set; } = LayoutOrientation.Vertical;

    /// <summary>
    /// Whether data source notifications are applied incrementally. Hosts that cannot
    /// safely rekey/rebind realized views can disable this to use full resets.
    /// </summary>
    public bool UseIncrementalChanges { get; set; } = true;

    /// <summary>
    /// Optional override for template key resolution. When set, the engine uses this
    /// instead of the default node-kind-based key, enabling per-DataTemplate recycling pools.
    /// </summary>
    public Func<int, string>? TemplateKeyProvider { get; set; }

    public ICollectionDataSource? DataSource
    {
        get => _dataSource;
        set
        {
            if (_dataSource is INotifyDataSourceChanged oldNotify)
            {
                oldNotify.ChangesApplied -= OnDataSourceChangesApplied;
            }

            _dataSource = value;

            if (_dataSource is INotifyDataSourceChanged newNotify)
            {
                newNotify.ChangesApplied += OnDataSourceChangesApplied;
            }

            Rebuild();
        }
    }

    /// <summary>
    /// Replaces the data source reference without triggering a full rebuild.
    /// Used by the host when it needs to refresh the snapshot before applying incremental changes.
    /// </summary>
    public void SetDataSourceQuiet(ICollectionDataSource? dataSource)
    {
        _dataSource = dataSource;
    }

    public ILayoutProvider? LayoutProvider
    {
        get => _layoutProvider;
        set
        {
            _layoutProvider = value;
            InvalidateLayout();
        }
    }

    public IRealizationStrategy RealizationStrategy
    {
        get => _realizationStrategy;
        set => _realizationStrategy = value ?? throw new ArgumentNullException(nameof(value));
    }

    public CollectionNodeFlattenOptions FlattenOptions
    {
        get => _flattenOptions;
        set
        {
            _flattenOptions = value ?? throw new ArgumentNullException(nameof(value));
            Rebuild();
        }
    }

    /// <summary>
    /// Fired when the engine needs the host to create/remove/position views.
    /// </summary>
    public event EventHandler<EngineUpdateEventArgs>? UpdateRequested;

    /// <summary>
    /// Rebuilds the entire pipeline from data source through to realization.
    /// </summary>
    public void Rebuild()
    {
        if (_dataSource is null)
        {
            _nodes = [];
            _sectionIndexMap = null;
            _lastSnapshot = null;
            _realizedIndices.Clear();
            RaiseUpdate(EngineUpdateKind.Reset);
            return;
        }

        _nodes = CollectionNodeFlattener.Flatten(_dataSource, _flattenOptions);
        _sectionIndexMap = new SectionIndexMap(_nodes);
        _scrollOffset = 0;
        _realizedIndices.Clear();
        RaiseUpdate(EngineUpdateKind.Reset);
        InvalidateLayout();
    }

    /// <summary>
    /// Applies incremental changes from the data source without a full reset.
    /// Re-flattens nodes and shifts realized indices to preserve existing views.
    /// Falls back to full <see cref="Rebuild"/> for Reset actions or grouped data.
    /// </summary>
    public void ApplyIncrementalChanges(CollectionChangeSet changeSet)
    {
        if (!UseIncrementalChanges || _dataSource is null || changeSet.IsReset)
        {
            Rebuild();
            return;
        }

        // For grouped data sources with multiple sections, fall back to full rebuild
        // because flat-index mapping is complex with section headers/footers
        if (_dataSource.SectionCount > 1)
        {
            Rebuild();
            return;
        }

        var oldCount = _nodes.Count;
        _nodes = CollectionNodeFlattener.Flatten(_dataSource, _flattenOptions);
        _sectionIndexMap = new SectionIndexMap(_nodes);

        // Shift realized indices for each change
        foreach (var change in changeSet.Changes)
        {
            // Compute flat index offset (account for global header)
            var headerOffset = _flattenOptions.Header is not null ? 1 : 0;
            var flatStart = change.StartIndex + headerOffset;

            switch (change.Action)
            {
                case CollectionChangeAction.Insert:
                    ShiftRealizedIndices(flatStart, change.Count);
                    break;

                case CollectionChangeAction.Remove:
                    // Remove any realized indices in the deleted range
                    for (var i = 0; i < change.Count; i++)
                    {
                        _realizedIndices.Remove(flatStart + i);
                    }
                    ShiftRealizedIndices(flatStart + change.Count, -change.Count);
                    break;

                case CollectionChangeAction.Replace:
                    // Items at same indices, just need re-bind via layout invalidation
                    break;

                case CollectionChangeAction.Move:
                    // Remove old, shift, then let realization handle the new position
                    _realizedIndices.Remove(flatStart);
                    var flatDest = change.DestinationIndex + headerOffset;
                    if (flatDest > flatStart)
                    {
                        ShiftRealizedIndices(flatStart + 1, -1);
                        ShiftRealizedIndices(flatDest, 1);
                    }
                    else
                    {
                        ShiftRealizedIndices(flatStart + 1, -1);
                        ShiftRealizedIndices(flatDest, 1);
                    }
                    break;

                default:
                    Rebuild();
                    return;
            }
        }

        InvalidateLayout();
    }

    private void ShiftRealizedIndices(int fromIndex, int delta)
    {
        if (delta == 0) return;

        var shifted = new HashSet<int>();
        foreach (var idx in _realizedIndices)
        {
            shifted.Add(idx >= fromIndex ? idx + delta : idx);
        }

        _realizedIndices.Clear();
        _realizedIndices.UnionWith(shifted);
    }

    private void OnDataSourceChangesApplied(object? sender, CollectionChangeSet changeSet)
    {
        if (_isApplyingChanges) return;
        _isApplyingChanges = true;
        try
        {
            ApplyIncrementalChanges(changeSet);
        }
        finally
        {
            _isApplyingChanges = false;
        }
    }

    /// <summary>
    /// Re-runs layout and realization with current state.
    /// </summary>
    public void InvalidateLayout()
    {
        if (_layoutProvider is null || _nodes.Count == 0)
        {
            _lastSnapshot = null;
            _realizedIndices.Clear();
            RaiseUpdate(EngineUpdateKind.Reset);
            return;
        }

        var context = new LayoutContext(_nodes.Count, _viewportWidth, _viewportHeight, _scrollOffset);
        var visibleRange = ComputeVisibleRange(context);
        _lastSnapshot = _layoutProvider.Arrange(context, visibleRange);

        // Sticky headers are handled by the view-layer overlay (CollectionHostView).
        // The snapshot retains original positions so the binary search in EstimateFirstVisible
        // always works with monotonically increasing Y values.

        Realize(visibleRange);
    }

    /// <summary>
    /// Updates the scroll offset and re-runs realization if the visible range changed.
    /// </summary>
    public void OnScroll(double offset)
    {
        _scrollOffset = offset;
        InvalidateLayout();
    }

    /// <summary>
    /// Updates viewport dimensions and re-runs layout.
    /// </summary>
    public void OnViewportChanged(double width, double height)
    {
        if (Math.Abs(width - _viewportWidth) < 0.5 && Math.Abs(height - _viewportHeight) < 0.5)
        {
            return;
        }

        _viewportWidth = width;
        _viewportHeight = height;
        InvalidateLayout();
    }

    /// <summary>
    /// Gets the template key for a given flat index based on node kind.
    /// </summary>
    public string GetTemplateKey(int flatIndex)
    {
        if (flatIndex < 0 || flatIndex >= _nodes.Count)
        {
            return "item";
        }

        return _nodes[flatIndex].Kind switch
        {
            CollectionNodeKind.Item => "item",
            CollectionNodeKind.SectionHeader => "sectionHeader",
            CollectionNodeKind.SectionFooter => "sectionFooter",
            CollectionNodeKind.Header => "header",
            CollectionNodeKind.Footer => "footer",
            CollectionNodeKind.Empty => "empty",
            _ => "item"
        };
    }

    private ItemRange ComputeVisibleRange(LayoutContext context)
    {
        var visible = GetUncachedVisibleRange(context);
        var start = Math.Max(0, visible.Start - _cacheBefore);
        var end = Math.Min(context.ItemCount, visible.EndExclusive + _cacheAfter);

        if (end <= start && context.ItemCount > 0)
        {
            end = Math.Min(context.ItemCount, start + 1);
        }

        // Ensure the current section's header is included so the sticky decorator can pin it.
        // Without this, a header scrolled far off-screen falls outside the range and disappears.
        if (StickyHeaders && _layoutProvider?.Capabilities.HasFlag(LayoutCapabilities.SupportsStickyHeaders) == true)
        {
            for (var i = start - 1; i >= 0; i--)
            {
                if (_nodes[i].Kind == CollectionNodeKind.SectionHeader)
                {
                    start = i;
                    break;
                }
            }
        }

        return new ItemRange(start, end);
    }

    private ItemRange GetUncachedVisibleRange(LayoutContext context)
    {
        if (context.ItemCount <= 0)
        {
            return ItemRange.Empty;
        }

        if (_layoutProvider is IVisibleRangeProvider rangeProvider)
        {
            return rangeProvider.GetVisibleRange(context).Clamp(0, context.ItemCount);
        }

        if (TryEstimateVisibleRangeFromSnapshot(context, out var visibleRange))
        {
            return visibleRange;
        }

        if (TryEstimateVisibleRangeFromContentExtent(context, out visibleRange))
        {
            return visibleRange;
        }

        return new ItemRange(0, Math.Min(context.ItemCount, Math.Max(1, _cacheBefore + _cacheAfter + 1)));
    }

    private bool TryEstimateVisibleRangeFromSnapshot(LayoutContext context, out ItemRange visibleRange)
    {
        visibleRange = ItemRange.Empty;
        if (_lastSnapshot is null || _lastSnapshot.Items.Count == 0)
        {
            return false;
        }

        var items = _lastSnapshot.Items;
        var isHorizontal = ScrollAxis == LayoutOrientation.Horizontal;
        var viewportEnd = _scrollOffset + (isHorizontal ? _viewportWidth : _viewportHeight);
        var firstStart = isHorizontal ? items[0].Frame.X : items[0].Frame.Y;
        var last = items[items.Count - 1];
        var lastEnd = isHorizontal
            ? last.Frame.X + last.Frame.Width
            : last.Frame.Y + last.Frame.Height;

        if (viewportEnd < firstStart || _scrollOffset > lastEnd)
        {
            return false;
        }

        visibleRange = new ItemRange(EstimateFirstVisible(), EstimateLastVisible() + 1)
            .Clamp(0, context.ItemCount);
        return true;
    }

    private bool TryEstimateVisibleRangeFromContentExtent(LayoutContext context, out ItemRange visibleRange)
    {
        visibleRange = ItemRange.Empty;
        if (_lastSnapshot is null)
        {
            return false;
        }

        var isHorizontal = ScrollAxis == LayoutOrientation.Horizontal;
        var contentExtent = isHorizontal ? _lastSnapshot.ContentWidth : _lastSnapshot.ContentHeight;
        var viewportExtent = isHorizontal ? context.ViewportWidth : context.ViewportHeight;
        if (contentExtent <= 0d || context.ItemCount <= 0)
        {
            return false;
        }

        var averageExtent = contentExtent / context.ItemCount;
        if (averageExtent <= 0d)
        {
            return false;
        }

        var safeOffset = Math.Max(0d, context.ScrollOffset);
        var viewportEnd = safeOffset + Math.Max(0d, viewportExtent);
        var start = (int)Math.Floor(safeOffset / averageExtent);
        var end = (int)Math.Ceiling(viewportEnd / averageExtent);
        visibleRange = new ItemRange(start, Math.Max(start + 1, end))
            .Clamp(0, context.ItemCount);
        return true;
    }

    private int EstimateFirstVisible()
    {
        if (_lastSnapshot is null || _lastSnapshot.Items.Count == 0)
        {
            return 0;
        }

        var items = _lastSnapshot.Items;
        var isHorizontal = ScrollAxis == LayoutOrientation.Horizontal;

        // Binary search: find first item whose trailing edge > _scrollOffset
        int lo = 0, hi = items.Count - 1, result = 0;
        while (lo <= hi)
        {
            var mid = lo + (hi - lo) / 2;
            var attr = items[mid];
            var itemEnd = isHorizontal
                ? attr.Frame.X + attr.Frame.Width
                : attr.Frame.Y + attr.Frame.Height;
            if (itemEnd > _scrollOffset)
            {
                result = attr.Index;
                hi = mid - 1;
            }
            else
            {
                lo = mid + 1;
            }
        }

        return result;
    }

    private int EstimateLastVisible()
    {
        if (_lastSnapshot is null || _lastSnapshot.Items.Count == 0)
        {
            return 0;
        }

        var items = _lastSnapshot.Items;
        var isHorizontal = ScrollAxis == LayoutOrientation.Horizontal;
        var viewportEnd = _scrollOffset + (isHorizontal ? _viewportWidth : _viewportHeight);

        // Binary search: find last item whose leading edge <= viewportEnd
        int lo = 0, hi = items.Count - 1, result = 0;
        while (lo <= hi)
        {
            var mid = lo + (hi - lo) / 2;
            var attr = items[mid];
            var itemStart = isHorizontal ? attr.Frame.X : attr.Frame.Y;
            if (itemStart <= viewportEnd)
            {
                result = attr.Index;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return result;
    }

    private void Realize(ItemRange visibleRange)
    {
        if (_lastSnapshot is null)
        {
            return;
        }

        var keyProvider = TemplateKeyProvider ?? GetTemplateKey;
        var entries = _realizationStrategy.Realize(_lastSnapshot, visibleRange, keyProvider);
        var recyclable = _realizationStrategy.GetRecyclableIndices(_realizedIndices, visibleRange);

        _realizedIndices.ExceptWith(recyclable);
        foreach (var entry in entries)
        {
            _realizedIndices.Add(entry.FlatIndex);
        }

        RaiseUpdate(EngineUpdateKind.Incremental, entries, recyclable);
    }

    private void RaiseUpdate(
        EngineUpdateKind kind,
        IReadOnlyList<RealizationEntry>? realized = null,
        IReadOnlyList<int>? recycled = null)
    {
        UpdateRequested?.Invoke(this, new EngineUpdateEventArgs(kind, _lastSnapshot, realized, recycled));
    }
}

public enum EngineUpdateKind
{
    Reset,
    Incremental
}

public sealed class EngineUpdateEventArgs : EventArgs
{
    public EngineUpdateEventArgs(
        EngineUpdateKind kind,
        LayoutSnapshot? snapshot,
        IReadOnlyList<RealizationEntry>? realizedEntries,
        IReadOnlyList<int>? recycledIndices)
    {
        Kind = kind;
        Snapshot = snapshot;
        RealizedEntries = realizedEntries ?? [];
        RecycledIndices = recycledIndices ?? [];
    }

    public EngineUpdateKind Kind { get; }
    public LayoutSnapshot? Snapshot { get; }
    public IReadOnlyList<RealizationEntry> RealizedEntries { get; }
    public IReadOnlyList<int> RecycledIndices { get; }
}
