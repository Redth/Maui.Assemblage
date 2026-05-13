namespace Maui.Assemblage.Core.Layout;

public sealed class LinearLayoutProvider : ILayoutProvider, IVisibleRangeProvider, IVariableExtentLayoutProvider
{
    private readonly Func<int, double>? _itemExtentResolver;

    public LinearLayoutProvider(double itemExtent, double spacing = 0d, LayoutOrientation orientation = LayoutOrientation.Vertical,
        Func<int, double>? itemExtentResolver = null)
    {
        if (itemExtent <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent));
        }

        if (spacing < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(spacing));
        }

        ItemExtent = itemExtent;
        Spacing = spacing;
        Orientation = orientation;
        _itemExtentResolver = itemExtentResolver;
    }

    public double ItemExtent { get; }

    public double Spacing { get; }

    public LayoutOrientation Orientation { get; }

    public LayoutCapabilities Capabilities => LayoutCapabilities.SupportsStickyHeaders;

    public LayoutSnapshot Arrange(LayoutContext context, ItemRange requestRange)
    {
        if (context.ItemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(context.ItemCount));
        }

        if (_itemExtentResolver is null)
        {
            return ArrangeUniform(context, requestRange);
        }

        return ArrangeVariable(context, requestRange);
    }

    private LayoutSnapshot ArrangeUniform(LayoutContext context, ItemRange requestRange)
    {
        var clampedRange = requestRange.Clamp(0, context.ItemCount);
        var items = new List<LayoutItemAttributes>(clampedRange.Length);
        var pitch = ItemExtent + Spacing;

        for (var index = clampedRange.Start; index < clampedRange.EndExclusive; index++)
        {
            var leading = index * pitch;
            var frame = Orientation == LayoutOrientation.Vertical
                ? new LayoutRect(0d, leading, context.ViewportWidth, ItemExtent)
                : new LayoutRect(leading, 0d, ItemExtent, context.ViewportHeight);
            items.Add(new LayoutItemAttributes(index, frame));
        }

        var contentPrimary = context.ItemCount == 0
            ? 0d
            : (context.ItemCount * ItemExtent) + ((context.ItemCount - 1) * Spacing);

        var contentWidth = Orientation == LayoutOrientation.Vertical
            ? context.ViewportWidth
            : contentPrimary;
        var contentHeight = Orientation == LayoutOrientation.Vertical
            ? contentPrimary
            : context.ViewportHeight;

        return new LayoutSnapshot(contentWidth, contentHeight, items);
    }

    private double[]? _prefixSums;
    private int _prefixSumCount;

    private LayoutSnapshot ArrangeVariable(LayoutContext context, ItemRange requestRange)
    {
        var clampedRange = requestRange.Clamp(0, context.ItemCount);
        var items = new List<LayoutItemAttributes>(clampedRange.Length);

        EnsurePrefixSums(context.ItemCount);

        // Use prefix sums for O(1) leading offset lookup
        var leading = clampedRange.Start > 0 ? _prefixSums![clampedRange.Start - 1] + Spacing : 0d;

        for (var index = clampedRange.Start; index < clampedRange.EndExclusive; index++)
        {
            var extent = _itemExtentResolver!(index) is var e && e > 0 ? e : ItemExtent;
            var frame = Orientation == LayoutOrientation.Vertical
                ? new LayoutRect(0d, leading, context.ViewportWidth, extent)
                : new LayoutRect(leading, 0d, extent, context.ViewportHeight);
            items.Add(new LayoutItemAttributes(index, frame));
            leading += extent + Spacing;
        }

        var contentPrimary = context.ItemCount == 0
            ? 0d
            : _prefixSums![context.ItemCount - 1];

        var contentWidth = Orientation == LayoutOrientation.Vertical
            ? context.ViewportWidth
            : contentPrimary;
        var contentHeight = Orientation == LayoutOrientation.Vertical
            ? contentPrimary
            : context.ViewportHeight;

        return new LayoutSnapshot(contentWidth, contentHeight, items);
    }

    /// <summary>Build/rebuild the prefix-sum array when item count changes.</summary>
    private void EnsurePrefixSums(int itemCount)
    {
        if (_prefixSums is not null && _prefixSumCount == itemCount) return;

        _prefixSums = new double[itemCount];
        _prefixSumCount = itemCount;
        var running = 0d;
        for (var i = 0; i < itemCount; i++)
        {
            var extent = _itemExtentResolver!(i) is var e && e > 0 ? e : ItemExtent;
            running += extent + (i > 0 ? Spacing : 0d);
            _prefixSums[i] = running;
        }
    }

    public InvalidationPlan Invalidate(LayoutInvalidationContext context)
    {
        if (!context.ViewportChanged && !context.DataChanged)
        {
            return new InvalidationPlan(false);
        }

        return new InvalidationPlan(true, context.AffectedRange);
    }

    public void InvalidateExtents()
    {
        _prefixSums = null;
        _prefixSumCount = 0;
    }

    public ItemRange GetVisibleRange(LayoutContext context)
    {
        if (context.ItemCount <= 0)
        {
            return ItemRange.Empty;
        }

        return _itemExtentResolver is null
            ? GetUniformVisibleRange(context)
            : GetVariableVisibleRange(context);
    }

    private ItemRange GetUniformVisibleRange(LayoutContext context)
    {
        var pitch = ItemExtent + Spacing;
        var viewportSize = Orientation == LayoutOrientation.Vertical
            ? context.ViewportHeight
            : context.ViewportWidth;
        var safeOffset = Math.Max(0d, context.ScrollOffset);
        var safeViewport = Math.Max(0d, viewportSize);
        var firstVisible = (int)Math.Floor(safeOffset / pitch);
        var endEdge = safeOffset + safeViewport;
        var lastVisible = (int)Math.Floor(Math.Max(safeOffset, endEdge - double.Epsilon) / pitch);
        return new ItemRange(
            Math.Clamp(firstVisible, 0, context.ItemCount),
            Math.Clamp(lastVisible + 1, 0, context.ItemCount));
    }

    private ItemRange GetVariableVisibleRange(LayoutContext context)
    {
        EnsurePrefixSums(context.ItemCount);
        var viewportSize = Orientation == LayoutOrientation.Vertical
            ? context.ViewportHeight
            : context.ViewportWidth;
        var safeOffset = Math.Max(0d, context.ScrollOffset);
        var viewportEnd = safeOffset + Math.Max(0d, viewportSize);
        var start = FindFirstIndexAfter(safeOffset);
        var end = FindFirstIndexStartingAfter(viewportEnd) + 1;
        return new ItemRange(
            Math.Clamp(start, 0, context.ItemCount),
            Math.Clamp(end, 0, context.ItemCount));
    }

    private int FindFirstIndexAfter(double offset)
    {
        var sums = _prefixSums!;
        var lo = 0;
        var hi = _prefixSumCount - 1;
        var result = _prefixSumCount;
        while (lo <= hi)
        {
            var mid = lo + (hi - lo) / 2;
            if (sums[mid] > offset)
            {
                result = mid;
                hi = mid - 1;
            }
            else
            {
                lo = mid + 1;
            }
        }

        return result;
    }

    private int FindFirstIndexStartingAfter(double offset)
    {
        var sums = _prefixSums!;
        var lo = 0;
        var hi = _prefixSumCount - 1;
        var result = _prefixSumCount - 1;
        while (lo <= hi)
        {
            var mid = lo + (hi - lo) / 2;
            var extent = _itemExtentResolver!(mid) is var e && e > 0 ? e : ItemExtent;
            var start = sums[mid] - extent;
            if (start <= offset)
            {
                result = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return result;
    }
}
