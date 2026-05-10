using Maui.Assemblage.Core;
using Maui.Assemblage.Core.Collections;
using Maui.Assemblage.Core.Data;
using Maui.Assemblage.Core.Layout;
using Maui.Assemblage.Core.Realization;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using SelectionMode = Maui.Assemblage.Core.Interactions.SelectionMode;
#if IOS || MACCATALYST
using Microsoft.Maui.Controls.PlatformConfiguration;
#endif

namespace Maui.Assemblage;

/// <summary>
/// The main collection host control. Uses a ScrollView + AbsoluteLayout to display
/// virtualized, recycled items driven by the shared <see cref="CollectionEngine"/>.
/// </summary>
public class CollectionHostView : ContentView
{
    private readonly CollectionEngine _engine = new();
    private readonly ScrollView _scrollView;
    private readonly AbsoluteLayout _contentSurface;
    private readonly RecyclePool<string, View> _viewPool = new(maxPerBucket: 20);
    private readonly Dictionary<int, View> _realizedViews = [];
    private readonly Dictionary<int, string> _realizedTemplateKeys = [];
    private readonly Dictionary<View, TapGestureRecognizer> _selectionTapRecognizers = [];
    private readonly Dictionary<View, Brush?> _originalBackgrounds = [];
    private readonly Dictionary<DataTemplate, string> _templateKeyMap = [];
    private int _templateKeyCounter;
    private RefreshView? _refreshView;
    private bool _isUpdating;
    private bool _hasEmptyNode;
    private Thickness _effectiveContentInset;
#if ANDROID
    private readonly HashSet<View> _cameraDistanceSet = [];
#endif

    // Snap-to-center infrastructure
    private IDispatcherTimer? _snapDebounceTimer;
    private double _lastScrollOffset;
    private double _scrollVelocity;
    private DateTime _lastScrollTime;
    private bool _isSnapping;

    // Sticky header overlay (sits outside ScrollView so it never jitters with scroll)
    private readonly AbsoluteLayout _stickyOverlay;
    private View? _stickyCloneView;
    private int _currentStickySection = -1;

    public CollectionHostView()
    {
        _contentSurface = new AbsoluteLayout
        {
            VerticalOptions = LayoutOptions.Start,
            IsClippedToBounds = false,
            SafeAreaEdges = Microsoft.Maui.SafeAreaEdges.None
        };
        _scrollView = new ScrollView
        {
            Content = _contentSurface,
            Orientation = ScrollOrientation.Vertical,
            SafeAreaEdges = Microsoft.Maui.SafeAreaEdges.None
        };
        _stickyOverlay = new AbsoluteLayout
        {
            InputTransparent = true,
            IsClippedToBounds = true,
            IsVisible = false,
            SafeAreaEdges = Microsoft.Maui.SafeAreaEdges.None
        };
        var root = new Grid
        {
            SafeAreaEdges = Microsoft.Maui.SafeAreaEdges.None
        };
        root.Children.Add(_scrollView);
        root.Children.Add(_stickyOverlay);
        Content = root;

        _scrollView.Scrolled += OnScrolled;
        _engine.UpdateRequested += OnEngineUpdate;
        _engine.Selection.SelectionChanged += OnSelectionChanged;
        Loaded += (_, _) => UpdateEffectiveContentInset(refreshViewport: true);
        HandlerChanged += (_, _) => UpdateEffectiveContentInset(refreshViewport: true);
    }

    #region Bindable Properties

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(object), typeof(CollectionHostView),
        propertyChanged: OnItemsSourceChanged);

    public static readonly BindableProperty LayoutProviderProperty = BindableProperty.Create(
        nameof(LayoutProvider), typeof(ILayoutProvider), typeof(CollectionHostView),
        propertyChanged: (b, _, n) => ((CollectionHostView)b)._engine.LayoutProvider = (ILayoutProvider?)n);

    public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(CollectionHostView));

    public static readonly BindableProperty ItemTemplateSelectorProperty = BindableProperty.Create(
        nameof(ItemTemplateSelector), typeof(DataTemplateSelector), typeof(CollectionHostView),
        propertyChanged: (b, _, _) => ((CollectionHostView)b).UpdateTemplateKeyProvider());

    public static readonly BindableProperty SectionHeaderTemplateProperty = BindableProperty.Create(
        nameof(SectionHeaderTemplate), typeof(DataTemplate), typeof(CollectionHostView));

    public static readonly BindableProperty SectionFooterTemplateProperty = BindableProperty.Create(
        nameof(SectionFooterTemplate), typeof(DataTemplate), typeof(CollectionHostView));

    public static readonly BindableProperty HeaderTemplateProperty = BindableProperty.Create(
        nameof(HeaderTemplate), typeof(DataTemplate), typeof(CollectionHostView));

    public static readonly BindableProperty FooterTemplateProperty = BindableProperty.Create(
        nameof(FooterTemplate), typeof(DataTemplate), typeof(CollectionHostView));

    public static readonly BindableProperty EmptyViewTemplateProperty = BindableProperty.Create(
        nameof(EmptyViewTemplate), typeof(DataTemplate), typeof(CollectionHostView));

    public static readonly BindableProperty SelectionModeProperty = BindableProperty.Create(
        nameof(SelectionMode), typeof(SelectionMode), typeof(CollectionHostView),
        SelectionMode.None,
        propertyChanged: (b, _, n) => ((CollectionHostView)b).OnSelectionModeChanged((SelectionMode)n));

    public static readonly BindableProperty HeaderProperty = BindableProperty.Create(
        nameof(Header), typeof(object), typeof(CollectionHostView),
        propertyChanged: (b, _, _) => ((CollectionHostView)b).UpdateFlattenOptions());

    public static readonly BindableProperty FooterProperty = BindableProperty.Create(
        nameof(Footer), typeof(object), typeof(CollectionHostView),
        propertyChanged: (b, _, _) => ((CollectionHostView)b).UpdateFlattenOptions());

    public static readonly BindableProperty EmptyViewProperty = BindableProperty.Create(
        nameof(EmptyView), typeof(object), typeof(CollectionHostView),
        propertyChanged: (b, _, _) => ((CollectionHostView)b).UpdateFlattenOptions());

    public static readonly BindableProperty DataSourceProperty = BindableProperty.Create(
        nameof(DataSource), typeof(ICollectionDataSource), typeof(CollectionHostView),
        propertyChanged: (b, _, n) =>
        {
            var host = (CollectionHostView)b;
            host._engine.DataSource = (ICollectionDataSource?)n;
        });

    public static readonly BindableProperty IsRefreshEnabledProperty = BindableProperty.Create(
        nameof(IsRefreshEnabled), typeof(bool), typeof(CollectionHostView), false,
        propertyChanged: (b, _, n) => ((CollectionHostView)b).UpdateRefreshView());

    public static readonly BindableProperty IsRefreshingProperty = BindableProperty.Create(
        nameof(IsRefreshing), typeof(bool), typeof(CollectionHostView), false,
        propertyChanged: (b, _, n) =>
        {
            var host = (CollectionHostView)b;
            if (host._refreshView is not null)
                host._refreshView.IsRefreshing = (bool)n;
        });

    public static readonly BindableProperty RefreshCommandProperty = BindableProperty.Create(
        nameof(RefreshCommand), typeof(ICommand), typeof(CollectionHostView));

    public static readonly BindableProperty ScrollDirectionProperty = BindableProperty.Create(
        nameof(ScrollDirection), typeof(ScrollOrientation), typeof(CollectionHostView),
        ScrollOrientation.Vertical,
        propertyChanged: (b, _, n) => ((CollectionHostView)b).OnScrollDirectionChanged((ScrollOrientation)n));

    public static readonly BindableProperty StickyHeadersProperty = BindableProperty.Create(
        nameof(StickyHeaders), typeof(bool), typeof(CollectionHostView), true,
        propertyChanged: (b, _, n) =>
        {
            var host = (CollectionHostView)b;
            host._engine.StickyHeaders = (bool)n;
            host._engine.InvalidateLayout();
        });

    public static readonly BindableProperty SnapToCenterProperty = BindableProperty.Create(
        nameof(SnapToCenter), typeof(bool), typeof(CollectionHostView), false);

    public static readonly BindableProperty HorizontalScrollBarVisibilityProperty = BindableProperty.Create(
        nameof(HorizontalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(CollectionHostView),
        ScrollBarVisibility.Default,
        propertyChanged: (b, _, n) => ((CollectionHostView)b)._scrollView.HorizontalScrollBarVisibility = (ScrollBarVisibility)n);

    public static readonly BindableProperty VerticalScrollBarVisibilityProperty = BindableProperty.Create(
        nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(CollectionHostView),
        ScrollBarVisibility.Default,
        propertyChanged: (b, _, n) => ((CollectionHostView)b)._scrollView.VerticalScrollBarVisibility = (ScrollBarVisibility)n);

    public static readonly BindableProperty ContentInsetProperty = BindableProperty.Create(
        nameof(ContentInset), typeof(Thickness), typeof(CollectionHostView), default(Thickness),
        propertyChanged: (b, _, _) => ((CollectionHostView)b).UpdateEffectiveContentInset(refreshViewport: true));

    public static readonly BindableProperty IncludeSafeAreaInsetsProperty = BindableProperty.Create(
        nameof(IncludeSafeAreaInsets), typeof(bool), typeof(CollectionHostView), true,
        propertyChanged: (b, _, _) => ((CollectionHostView)b).UpdateEffectiveContentInset(refreshViewport: true));

    public static readonly BindableProperty SelectedBackgroundColorProperty = BindableProperty.Create(
        nameof(SelectedBackgroundColor), typeof(Color), typeof(CollectionHostView),
        Color.FromArgb("#E8DEF8"),
        propertyChanged: (b, _, _) => ((CollectionHostView)b).RefreshSelectionVisuals());

    public object? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ILayoutProvider? LayoutProvider
    {
        get => (ILayoutProvider?)GetValue(LayoutProviderProperty);
        set => SetValue(LayoutProviderProperty, value);
    }

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public DataTemplateSelector? ItemTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(ItemTemplateSelectorProperty);
        set => SetValue(ItemTemplateSelectorProperty, value);
    }

    public DataTemplate? SectionHeaderTemplate
    {
        get => (DataTemplate?)GetValue(SectionHeaderTemplateProperty);
        set => SetValue(SectionHeaderTemplateProperty, value);
    }

    public DataTemplate? SectionFooterTemplate
    {
        get => (DataTemplate?)GetValue(SectionFooterTemplateProperty);
        set => SetValue(SectionFooterTemplateProperty, value);
    }

    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    public DataTemplate? FooterTemplate
    {
        get => (DataTemplate?)GetValue(FooterTemplateProperty);
        set => SetValue(FooterTemplateProperty, value);
    }

    public DataTemplate? EmptyViewTemplate
    {
        get => (DataTemplate?)GetValue(EmptyViewTemplateProperty);
        set => SetValue(EmptyViewTemplateProperty, value);
    }

    public SelectionMode SelectionMode
    {
        get => (SelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    public object? EmptyView
    {
        get => GetValue(EmptyViewProperty);
        set => SetValue(EmptyViewProperty, value);
    }

    /// <summary>
    /// Sets a typed <see cref="ICollectionDataSource"/> directly, bypassing IEnumerable conversion.
    /// Use for grouped data sources or custom data adapters.
    /// </summary>
    public ICollectionDataSource? DataSource
    {
        get => (ICollectionDataSource?)GetValue(DataSourceProperty);
        set => SetValue(DataSourceProperty, value);
    }

    /// <summary>Enables pull-to-refresh by wrapping the scroll in a RefreshView.</summary>
    public bool IsRefreshEnabled
    {
        get => (bool)GetValue(IsRefreshEnabledProperty);
        set => SetValue(IsRefreshEnabledProperty, value);
    }

    /// <summary>Whether a refresh is currently in progress.</summary>
    public bool IsRefreshing
    {
        get => (bool)GetValue(IsRefreshingProperty);
        set => SetValue(IsRefreshingProperty, value);
    }

    /// <summary>Command executed when refresh is triggered.</summary>
    public ICommand? RefreshCommand
    {
        get => (ICommand?)GetValue(RefreshCommandProperty);
        set => SetValue(RefreshCommandProperty, value);
    }

    /// <summary>Scroll direction. Vertical by default; set Horizontal for horizontal layouts.</summary>
    public ScrollOrientation ScrollDirection
    {
        get => (ScrollOrientation)GetValue(ScrollDirectionProperty);
        set => SetValue(ScrollDirectionProperty, value);
    }

    /// <summary>Whether section headers stick to the top while scrolling. Default is true.</summary>
    public bool StickyHeaders
    {
        get => (bool)GetValue(StickyHeadersProperty);
        set => SetValue(StickyHeadersProperty, value);
    }

    /// <summary>Whether to snap the nearest item to center after scrolling ends. Default is false.
    /// Automatically enabled for layout providers with <see cref="LayoutCapabilities.SupportsSnapping"/>.</summary>
    public bool SnapToCenter
    {
        get => (bool)GetValue(SnapToCenterProperty);
        set => SetValue(SnapToCenterProperty, value);
    }

    /// <summary>Visibility of the horizontal scrollbar. Default, Always, or Never.</summary>
    public ScrollBarVisibility HorizontalScrollBarVisibility
    {
        get => (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty);
        set => SetValue(HorizontalScrollBarVisibilityProperty, value);
    }

    /// <summary>Visibility of the vertical scrollbar. Default, Always, or Never.</summary>
    public ScrollBarVisibility VerticalScrollBarVisibility
    {
        get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
        set => SetValue(VerticalScrollBarVisibilityProperty, value);
    }

    /// <summary>Extra content inset applied inside the scrollable surface.</summary>
    public Thickness ContentInset
    {
        get => (Thickness)GetValue(ContentInsetProperty);
        set => SetValue(ContentInsetProperty, value);
    }

    /// <summary>When true, MAUI safe-area insets are applied on edges this view touches.</summary>
    public bool IncludeSafeAreaInsets
    {
        get => (bool)GetValue(IncludeSafeAreaInsetsProperty);
        set => SetValue(IncludeSafeAreaInsetsProperty, value);
    }

    /// <summary>Background color applied to selected items. Default is light purple.</summary>
    public Color SelectedBackgroundColor
    {
        get => (Color)GetValue(SelectedBackgroundColorProperty);
        set => SetValue(SelectedBackgroundColorProperty, value);
    }

    #endregion

    /// <summary>Provides access to the underlying engine for advanced scenarios.</summary>
    public CollectionEngine Engine => _engine;

    /// <summary>The selected items from the selection tracker.</summary>
    public IReadOnlySet<object> SelectedItems => _engine.Selection.SelectedItems;

    /// <summary>Fired when selection changes.</summary>
    public event EventHandler<Core.Interactions.SelectionChangedEventArgs>? SelectionChanged
    {
        add => _engine.Selection.SelectionChanged += value;
        remove => _engine.Selection.SelectionChanged -= value;
    }

    /// <summary>Fired when a pull-to-refresh gesture completes.</summary>
    public event EventHandler? Refreshing;

    /// <summary>Fired on each scroll event with the current scroll offset.</summary>
    public event EventHandler<ScrolledEventArgs>? Scrolled;

    /// <summary>Scrolls to the given item index, centering it in the viewport if snapping is enabled.</summary>
    public void ScrollToItem(int index, bool animated = true)
    {
        var isHorizontal = _scrollView.Orientation == ScrollOrientation.Horizontal;
        var leadingInset = GetLeadingContentInset();
        var viewportSize = GetEngineViewportMainAxisSize();
        if (_engine.LayoutProvider is ISnappingLayoutProvider snap)
        {
            var offset = snap.GetSnapOffset(index, viewportSize);
            _ = isHorizontal
                ? _scrollView.ScrollToAsync(offset + leadingInset, 0, animated)
                : _scrollView.ScrollToAsync(0, offset + leadingInset, animated);
        }
        else
        {
            var offset = index * 48d + leadingInset;
            _ = isHorizontal
                ? _scrollView.ScrollToAsync(offset, 0, animated)
                : _scrollView.ScrollToAsync(0, offset, animated);
        }
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        UpdateEffectiveContentInset(refreshViewport: false);
        var viewport = GetEngineViewportSize(width, height);
        _engine.OnViewportChanged(viewport.Width, viewport.Height);
    }

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var host = (CollectionHostView)bindable;

        // Unsubscribe from old collection changes
        if (oldValue is INotifyCollectionChanged oldNcc)
        {
            oldNcc.CollectionChanged -= host.OnCollectionChanged;
        }

        // Subscribe to new collection changes
        if (newValue is INotifyCollectionChanged newNcc)
        {
            newNcc.CollectionChanged += host.OnCollectionChanged;
        }

        host.RebuildDataSource();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Use incremental update path for non-Reset changes
        if (e.Action != NotifyCollectionChangedAction.Reset)
        {
            var changeSet = new Core.Data.CollectionChangeSet();
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    changeSet.Add(Core.Data.CollectionChange.Insert(0, e.NewStartingIndex, e.NewItems?.Count ?? 1));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    changeSet.Add(Core.Data.CollectionChange.Remove(0, e.OldStartingIndex, e.OldItems?.Count ?? 1));
                    break;
                case NotifyCollectionChangedAction.Replace:
                    changeSet.Add(Core.Data.CollectionChange.Replace(0, e.NewStartingIndex, e.NewItems?.Count ?? 1));
                    break;
                case NotifyCollectionChangedAction.Move:
                    changeSet.Add(Core.Data.CollectionChange.Move(0, e.OldStartingIndex, e.NewStartingIndex));
                    break;
            }

            // Refresh the data source snapshot without triggering a full engine rebuild,
            // then apply the incremental change to shift realized indices.
            RefreshDataSourceQuiet();
            _engine.ApplyIncrementalChanges(changeSet);
            return;
        }

        RebuildDataSource();
    }

    /// <summary>
    /// Updates the engine's data source without triggering a Rebuild.
    /// Used for incremental changes where we manually call ApplyIncrementalChanges.
    /// </summary>
    private void RefreshDataSourceQuiet()
    {
        var source = ItemsSource;
        if (source is null) return;

        if (source is ICollectionDataSource cds)
        {
            _engine.SetDataSourceQuiet(cds);
        }
        else if (source is IReadOnlyList<object?> rol)
        {
            _engine.SetDataSourceQuiet(new EnumerableCollectionDataSource(rol));
        }
        else if (source is IEnumerable enumerable)
        {
            var items = new List<object?>();
            foreach (var item in enumerable) items.Add(item);
            _engine.SetDataSourceQuiet(new EnumerableCollectionDataSource(items));
        }
    }

    private void RebuildDataSource()
    {
        var source = ItemsSource;
        if (source is null)
        {
            _engine.DataSource = null;
            return;
        }

        if (source is ICollectionDataSource cds)
        {
            _engine.DataSource = cds;
        }
        else if (source is IReadOnlyList<object?> rol)
        {
            _engine.DataSource = new EnumerableCollectionDataSource(rol);
        }
        else if (source is IEnumerable enumerable)
        {
            var items = new List<object?>();
            foreach (var item in enumerable)
            {
                items.Add(item);
            }

            _engine.DataSource = new EnumerableCollectionDataSource(items);
        }
    }

    private void UpdateFlattenOptions()
    {
        _engine.FlattenOptions = new CollectionNodeFlattenOptions
        {
            Header = Header,
            Footer = Footer,
            EmptyView = EmptyView
        };
    }

    private void OnScrolled(object? sender, ScrolledEventArgs e)
    {
        var rawOffset = _scrollView.Orientation == ScrollOrientation.Horizontal ? e.ScrollX : e.ScrollY;
        var offset = Math.Max(0d, rawOffset - GetLeadingContentInset());

        // Track velocity for snap targeting
        var now = DateTime.UtcNow;
        var dt = (now - _lastScrollTime).TotalSeconds;
        if (dt > 0 && dt < 0.5)
        {
            _scrollVelocity = (offset - _lastScrollOffset) / dt;
        }

        _lastScrollOffset = offset;
        _lastScrollTime = now;

        _engine.OnScroll(offset);

        Scrolled?.Invoke(this, e);

        // Reset snap debounce on every scroll event
        if (SnapToCenter && _engine.LayoutProvider is ISnappingLayoutProvider && !_isSnapping)
        {
            _snapDebounceTimer?.Stop();
            _snapDebounceTimer ??= Dispatcher.CreateTimer();
            _snapDebounceTimer.Interval = TimeSpan.FromMilliseconds(100);
            _snapDebounceTimer.Tick -= OnSnapDebounceElapsed;
            _snapDebounceTimer.Tick += OnSnapDebounceElapsed;
            _snapDebounceTimer.Start();
        }
    }

    private void OnSnapDebounceElapsed(object? sender, EventArgs e)
    {
        _snapDebounceTimer?.Stop();

        if (!SnapToCenter || _isSnapping)
            return;

        if (_engine.LayoutProvider is not ISnappingLayoutProvider snapper)
            return;

        // Decay velocity based on time since last scroll event.
        // If it's been a while since the last event, the scroll has stopped.
        var timeSinceLastScroll = (DateTime.UtcNow - _lastScrollTime).TotalSeconds;
        var effectiveVelocity = timeSinceLastScroll > 0.2 ? 0d : _scrollVelocity;

        // If scroll velocity is still significant, the fling is still active.
        // Defer snapping by restarting the debounce timer.
        if (Math.Abs(effectiveVelocity) > 50d)
        {
            _snapDebounceTimer!.Interval = TimeSpan.FromMilliseconds(50);
            _snapDebounceTimer.Start();
            return;
        }

        var isHorizontal = _scrollView.Orientation == ScrollOrientation.Horizontal;
        var viewportSize = GetEngineViewportMainAxisSize();
        var leadingInset = GetLeadingContentInset();
        var currentRawOffset = isHorizontal ? _scrollView.ScrollX : _scrollView.ScrollY;
        var currentOffset = Math.Max(0d, currentRawOffset - leadingInset);
        var itemCount = _engine.LastSnapshot?.Items.Count > 0 ? _engine.DataSource?.GetItemCount(0) ?? 0 : 0;

        if (itemCount <= 0 || viewportSize <= 0)
            return;

        var targetIndex = snapper.GetSnapTargetIndex(currentOffset, effectiveVelocity, itemCount, viewportSize);
        var targetOffset = snapper.GetSnapOffset(targetIndex, viewportSize);
        var targetRawOffset = targetOffset + leadingInset;

        // Only snap if we need to move more than 1px
        if (Math.Abs(targetRawOffset - currentRawOffset) < 1d)
            return;

        _isSnapping = true;

        // Cancel any ongoing native fling before starting snap animation.
        // Without this, the native fling and ScrollToAsync fight each other on Android.
        StopNativeFling();

        var startOffset = currentRawOffset;
        var deltaOffset = targetRawOffset - currentRawOffset;
        var anim = new Animation(v =>
        {
            var off = startOffset + deltaOffset * v;
            if (isHorizontal)
                _scrollView.ScrollToAsync(off, 0d, false);
            else
                _scrollView.ScrollToAsync(0d, off, false);
        }, 0d, 1d, Easing.CubicOut);

        anim.Commit(_scrollView, "SnapAnim", length: 150, finished: (_, __) =>
        {
            _scrollVelocity = 0d;
            _isSnapping = false;
        });
    }

    private void OnEngineUpdate(object? sender, EngineUpdateEventArgs e)
    {
        if (_isUpdating)
        {
            return;
        }

        _isUpdating = true;

        try
        {
            if (e.Kind == EngineUpdateKind.Reset)
            {
                _hasEmptyNode = _engine.Nodes.Count > 0 && _engine.Nodes.Any(n => n.Kind == CollectionNodeKind.Empty);
                RecycleAll();
                UpdateContentSize(e.Snapshot);
                // Scroll back to top on full reset
                _ = _scrollView.ScrollToAsync(0, 0, false);
                return;
            }

            // Recycle views that are no longer needed
            foreach (var idx in e.RecycledIndices)
            {
                if (_realizedViews.Remove(idx, out var view))
                {
                    var key = _realizedTemplateKeys.Remove(idx, out var storedKey) ? storedKey : _engine.GetTemplateKey(idx);
                    ReleaseView(view);
                    _contentSurface.Children.Remove(view);
                    _viewPool.Return(key, view);
                }
            }

            // Realize new views
            foreach (var entry in e.RealizedEntries)
            {
                if (_realizedViews.ContainsKey(entry.FlatIndex))
                {
                    // Already realized — just reposition
                    PositionView(_realizedViews[entry.FlatIndex], entry.Attributes, entry.FlatIndex);
                    continue;
                }

                var view = ObtainView(entry);
                if (view is null)
                {
                    continue;
                }

                BindView(view, entry.FlatIndex);
                PositionView(view, entry.Attributes, entry.FlatIndex);

                _realizedViews[entry.FlatIndex] = view;
                _realizedTemplateKeys[entry.FlatIndex] = entry.TemplateKey;
                _contentSurface.Children.Add(view);
            }

            UpdateContentSize(e.Snapshot);
            UpdateStickyOverlay();
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private View? ObtainView(RealizationEntry entry)
    {
        // Try recycling first
        if (_viewPool.TryRent(entry.TemplateKey, out var recycled))
        {
            return recycled;
        }

        // Create from template
        var template = GetTemplateForKey(entry.TemplateKey, entry.FlatIndex);
        if (template is null)
        {
            return CreateDefaultView();
        }

        var content = template.CreateContent();
        return content as View ?? CreateDefaultView();
    }

    private DataTemplate? GetTemplateForKey(string templateKey, int flatIndex)
    {
        return templateKey switch
        {
            "item" => ResolveItemTemplate(flatIndex),
            "sectionHeader" => SectionHeaderTemplate,
            "sectionFooter" => SectionFooterTemplate,
            "header" => HeaderTemplate,
            "footer" => FooterTemplate,
            "empty" => EmptyViewTemplate,
            _ => ResolveItemTemplate(flatIndex)
        };
    }

    private DataTemplate? ResolveItemTemplate(int flatIndex)
    {
        if (ItemTemplateSelector is not null && flatIndex >= 0 && flatIndex < _engine.Nodes.Count)
        {
            var node = _engine.Nodes[flatIndex];
            return ItemTemplateSelector.SelectTemplate(node.Data, this);
        }

        return ItemTemplate;
    }

    private void UpdateTemplateKeyProvider()
    {
        if (ItemTemplateSelector is not null)
        {
            _engine.TemplateKeyProvider = GetSelectorAwareTemplateKey;
        }
        else
        {
            _engine.TemplateKeyProvider = null;
        }
    }

    private string GetSelectorAwareTemplateKey(int flatIndex)
    {
        var baseKey = _engine.GetTemplateKey(flatIndex);
        if (baseKey != "item" || ItemTemplateSelector is null)
            return baseKey;

        if (flatIndex < 0 || flatIndex >= _engine.Nodes.Count)
            return baseKey;

        var node = _engine.Nodes[flatIndex];
        var template = ItemTemplateSelector.SelectTemplate(node.Data, this);
        if (template is null)
            return baseKey;

        if (!_templateKeyMap.TryGetValue(template, out var key))
        {
            key = $"item_{_templateKeyCounter++}";
            _templateKeyMap[template] = key;
        }

        return key;
    }

    private void BindView(View view, int flatIndex)
    {
        if (flatIndex >= 0 && flatIndex < _engine.Nodes.Count)
        {
            var node = _engine.Nodes[flatIndex];
            view.BindingContext = node.Data;

            // Apply selection visual feedback
            if (node.Kind == CollectionNodeKind.Item)
            {
                ApplySelectionVisual(view, node.Data);
            }
            else
            {
                ClearSelectionVisual(view);
            }

            // Add tap gesture for item selection
            if (node.Kind == CollectionNodeKind.Item && _engine.Selection.Mode != Core.Interactions.SelectionMode.None)
            {
                AttachSelectionTap(view, node.Data);
            }
            else
            {
                DetachSelectionTap(view);
            }
        }
    }

    private void PositionView(View view, LayoutItemAttributes attr, int flatIndex)
    {
        var node = flatIndex >= 0 && flatIndex < _engine.Nodes.Count ? _engine.Nodes[flatIndex] : (CollectionNode?)null;
        var isHorizontal = _scrollView.Orientation == ScrollOrientation.Horizontal;

        // Empty view fills the viewport
        if (node?.Kind == CollectionNodeKind.Empty)
        {
            var inset = _effectiveContentInset;
            AbsoluteLayout.SetLayoutBounds(
                view,
                new Rect(
                    inset.Left,
                    inset.Top,
                    Math.Max(0d, _scrollView.Width - inset.HorizontalThickness),
                    Math.Max(0d, _scrollView.Height - inset.VerticalThickness)));
            AbsoluteLayout.SetLayoutFlags(view, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
            view.ZIndex = 0;
            view.Opacity = 1d;
            return;
        }

        // Only update layout bounds when position/size actually changed
        var leadingInset = GetLeadingContentInset();
        var crossInset = GetCrossLeadingContentInset();
        var newBounds = new Rect(
            isHorizontal ? attr.Frame.X + leadingInset : attr.Frame.X + crossInset,
            isHorizontal ? attr.Frame.Y + crossInset : attr.Frame.Y + leadingInset,
            attr.Frame.Width,
            attr.Frame.Height);
        var currentBounds = AbsoluteLayout.GetLayoutBounds(view);
        if (Math.Abs(currentBounds.X - newBounds.X) > 0.5 ||
            Math.Abs(currentBounds.Y - newBounds.Y) > 0.5 ||
            Math.Abs(currentBounds.Width - newBounds.Width) > 0.5 ||
            Math.Abs(currentBounds.Height - newBounds.Height) > 0.5)
        {
            AbsoluteLayout.SetLayoutBounds(view, newBounds);
            AbsoluteLayout.SetLayoutFlags(view, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
        }

        view.ZIndex = attr.ZIndex;

        // Only set transform properties when they differ from current to avoid unnecessary layout passes
        if (Math.Abs(view.Opacity - attr.Opacity) > 0.001)
            view.Opacity = attr.Opacity;

        // Set MAUI properties for state tracking (threshold checks use these values)
        if (Math.Abs(view.Scale - attr.Scale) > 0.001)
            view.Scale = attr.Scale;
        if (Math.Abs(view.RotationY - attr.RotationY) > 0.01)
            view.RotationY = attr.RotationY;
        if (Math.Abs(view.TranslationX - attr.TranslateX) > 0.1)
            view.TranslationX = attr.TranslateX;
        if (Math.Abs(view.RotationX - attr.RotationX) > 0.01)
            view.RotationX = attr.RotationX;

#if ANDROID
        if (view.Handler is not null)
        {
            var platformNative = view.Handler.PlatformView as Android.Views.View;
            var containerNative = view.Handler.ContainerView as Android.Views.View;
            var hasWrapper = containerNative is not null && platformNative is not null
                && !ReferenceEquals(containerNative, platformNative);
            var target = hasWrapper ? containerNative! : platformNative!;

            if (target is not null)
            {
                // Fix pivots every frame: MAUI sets them at handler creation when the
                // native view is still 0×0, leaving PivotX/PivotY at 0 or -1.
                // Once the view has actual dimensions, correct to center.
                if (target.Width > 0)
                {
                    var centerX = target.Width / 2f;
                    var centerY = target.Height / 2f;
                    if (Math.Abs(target.PivotX - centerX) > 1f)
                        target.PivotX = centerX;
                    if (Math.Abs(target.PivotY - centerY) > 1f)
                        target.PivotY = centerY;
                }

                // One-time setup: camera distance and parent clipping
                if (!_cameraDistanceSet.Contains(view))
                {
                    var density = target.Resources?.DisplayMetrics?.Density ?? 2.625f;
                    target.SetCameraDistance(density * 1400);

                    // Disable clipping on ancestor layouts so 3D rotations aren't clipped
                    if (target.Parent is Android.Views.ViewGroup parentVg)
                    {
                        parentVg.SetClipChildren(false);
                        parentVg.SetClipToPadding(false);
                        if (parentVg.Parent is Android.Views.ViewGroup grandparentVg)
                        {
                            grandparentVg.SetClipChildren(false);
                            grandparentVg.SetClipToPadding(false);
                        }
                    }

                    _cameraDistanceSet.Add(view);
                }

                // Apply transforms to the outermost native view
                target.ScaleX = (float)attr.Scale;
                target.ScaleY = (float)attr.Scale;
                target.RotationY = (float)attr.RotationY;
                target.TranslationX = (float)attr.TranslateX;
                target.RotationX = (float)attr.RotationX;

                // If wrapped, reset inner PlatformView to identity
                if (hasWrapper)
                {
                    platformNative!.ScaleX = 1f;
                    platformNative.ScaleY = 1f;
                    platformNative.RotationY = 0f;
                    platformNative.TranslationX = 0f;
                    platformNative.RotationX = 0f;
                }
            }
        }
#endif
    }

    private void UpdateContentSize(LayoutSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            _contentSurface.WidthRequest = 0;
            _contentSurface.HeightRequest = 0;
            return;
        }

        var isHorizontal = _scrollView.Orientation == ScrollOrientation.Horizontal;
        var axisInset = GetLeadingContentInset() + GetAxisTrailingContentInsetForScrollSize();
        var crossInset = GetCrossLeadingContentInset() + GetCrossTrailingContentInset();
        var contentWidth = snapshot.ContentWidth + (isHorizontal ? axisInset : crossInset);
        var contentHeight = snapshot.ContentHeight + (isHorizontal ? crossInset : axisInset);
        var w = _hasEmptyNode ? Math.Max(contentWidth, _scrollView.Width) : contentWidth;
        var h = _hasEmptyNode ? Math.Max(contentHeight, _scrollView.Height) : contentHeight;

        // Avoid triggering layout if size unchanged
        if (Math.Abs(_contentSurface.WidthRequest - w) > 0.5)
            _contentSurface.WidthRequest = w;
        if (Math.Abs(_contentSurface.HeightRequest - h) > 0.5)
            _contentSurface.HeightRequest = h;
    }

    /// <summary>
    /// Manages a sticky header clone in an overlay above the ScrollView.
    /// Because the overlay is outside the ScrollView, its position is always correct
    /// regardless of scroll timing, eliminating jitter during fast flings.
    /// </summary>
    private void UpdateStickyOverlay()
    {
        if (!StickyHeaders || _engine.LastSnapshot is null ||
            !(_engine.LayoutProvider?.Capabilities.HasFlag(LayoutCapabilities.SupportsStickyHeaders) ?? false))
        {
            HideStickyOverlay();
            return;
        }

        var nodes = _engine.Nodes;
        if (nodes.Count == 0)
        {
            HideStickyOverlay();
            return;
        }

        var isHorizontal = _scrollView.Orientation == ScrollOrientation.Horizontal;
        var scrollOffset = GetEngineScrollOffset();
        var viewportSize = isHorizontal ? _scrollView.Width : _scrollView.Height;

        // Build a map of snapshot items for fast lookup
        var snapshot = _engine.LastSnapshot;
        LayoutItemAttributes? stickyHeaderAttr = null;
        int stickySection = -1;
        double stickyOriginalLeading = 0;
        double sectionEnd = 0;

        // Find section headers and determine which one should stick.
        // Walk through snapshot items (which include section headers thanks to range expansion).
        var headerAttrs = new List<(int section, LayoutItemAttributes attr)>();
        var sectionEnds = new Dictionary<int, double>();

        foreach (var attr in snapshot.Items)
        {
            if (attr.Index >= nodes.Count) continue;
            var node = nodes[attr.Index];
            var itemEnd = isHorizontal
                ? attr.Frame.X + attr.Frame.Width
                : attr.Frame.Y + attr.Frame.Height;

            if (node.Kind == CollectionNodeKind.SectionHeader)
            {
                headerAttrs.Add((node.Section, attr));
            }

            if (!sectionEnds.ContainsKey(node.Section) || itemEnd > sectionEnds[node.Section])
                sectionEnds[node.Section] = itemEnd;
        }

        // Find the header that should be sticky: the last header whose original leading <= scrollOffset
        foreach (var (section, attr) in headerAttrs)
        {
            var leading = isHorizontal ? attr.Frame.X : attr.Frame.Y;
            if (leading <= scrollOffset)
            {
                stickyHeaderAttr = attr;
                stickySection = section;
                stickyOriginalLeading = leading;
                sectionEnd = sectionEnds.TryGetValue(section, out var end) ? end : leading + (isHorizontal ? attr.Frame.Width : attr.Frame.Height);
            }
        }

        if (stickyHeaderAttr is null || stickySection < 0)
        {
            HideStickyOverlay();
            return;
        }

        var headerExtent = isHorizontal ? stickyHeaderAttr.Value.Frame.Width : stickyHeaderAttr.Value.Frame.Height;
        var maxStickyOffset = sectionEnd - headerExtent;

        // If the header is at its natural position (not past the viewport top), no sticking needed
        if (scrollOffset <= stickyOriginalLeading)
        {
            HideStickyOverlay();
            return;
        }

        // Compute push-up: when next section's header approaches, the sticky slides up
        var pushUp = Math.Max(0, scrollOffset - maxStickyOffset);
        var overlayY = -pushUp;
        var crossInset = isHorizontal ? _effectiveContentInset.Top : _effectiveContentInset.Left;

        // Create or rebind the clone if the section changed
        if (stickySection != _currentStickySection || _stickyCloneView is null)
        {
            if (_stickyCloneView is not null)
            {
                _stickyOverlay.Children.Remove(_stickyCloneView);
                _stickyCloneView = null;
            }

            var template = SectionHeaderTemplate;
            if (template is not null)
            {
                var content = template.CreateContent();
                _stickyCloneView = content as View;
            }

            if (_stickyCloneView is null)
            {
                _currentStickySection = -1;
                HideStickyOverlay();
                return;
            }

            // Bind to the section header data
            var headerNode = nodes[stickyHeaderAttr.Value.Index];
            _stickyCloneView.BindingContext = headerNode.Data;
            _stickyOverlay.Children.Add(_stickyCloneView);
            _currentStickySection = stickySection;
        }

        // Position clone in overlay (viewport-relative, not content-relative)
        var cloneWidth = stickyHeaderAttr.Value.Frame.Width;
        var cloneHeight = stickyHeaderAttr.Value.Frame.Height;
        AbsoluteLayout.SetLayoutBounds(_stickyCloneView, isHorizontal
            ? new Rect(overlayY, crossInset, cloneWidth, cloneHeight)
            : new Rect(crossInset, overlayY, cloneWidth, cloneHeight));
        AbsoluteLayout.SetLayoutFlags(_stickyCloneView, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
        _stickyCloneView.ZIndex = 1000;
        _stickyOverlay.IsVisible = true;
    }

    private void HideStickyOverlay()
    {
        _stickyOverlay.IsVisible = false;
        if (_stickyCloneView is not null)
        {
            _stickyOverlay.Children.Remove(_stickyCloneView);
            _stickyCloneView = null;
        }
        _currentStickySection = -1;
    }

    /// <summary>
    /// Cancels any in-progress native fling/scroll animation.
    /// Required on Android where the native fling and ScrollToAsync fight each other.
    /// </summary>
    private void StopNativeFling()
    {
#if ANDROID
        var nativeView = _scrollView.Handler?.PlatformView;
        if (nativeView is Android.Widget.HorizontalScrollView hsv)
            hsv.Fling(0);
        else if (nativeView is AndroidX.Core.Widget.NestedScrollView nsv)
            nsv.Fling(0);
#endif
    }

    private void OnScrollDirectionChanged(ScrollOrientation orientation)
    {
        _scrollView.Orientation = orientation;
        _engine.ScrollAxis = orientation == ScrollOrientation.Horizontal
            ? Core.Layout.LayoutOrientation.Horizontal
            : Core.Layout.LayoutOrientation.Vertical;
        if (orientation == ScrollOrientation.Horizontal)
        {
            _contentSurface.VerticalOptions = LayoutOptions.Fill;
            _contentSurface.HorizontalOptions = LayoutOptions.Start;
        }
        else
        {
            _contentSurface.VerticalOptions = LayoutOptions.Start;
            _contentSurface.HorizontalOptions = LayoutOptions.Fill;
        }

        UpdateEffectiveContentInset(refreshViewport: true);
    }

    private double GetLeadingContentInset()
        => _scrollView.Orientation == ScrollOrientation.Horizontal
            ? _effectiveContentInset.Left
            : _effectiveContentInset.Top;

    private double GetAxisTrailingContentInsetForViewport()
        => _scrollView.Orientation == ScrollOrientation.Horizontal
            ? ContentInset.Right
            : ContentInset.Bottom;

    private double GetAxisTrailingContentInsetForScrollSize()
        => _scrollView.Orientation == ScrollOrientation.Horizontal
            ? ContentInset.Right
            : ContentInset.Bottom;

    private double GetCrossLeadingContentInset()
        => _scrollView.Orientation == ScrollOrientation.Horizontal
            ? _effectiveContentInset.Top
            : _effectiveContentInset.Left;

    private double GetCrossTrailingContentInset()
        => _scrollView.Orientation == ScrollOrientation.Horizontal
            ? _effectiveContentInset.Bottom
            : _effectiveContentInset.Right;

    private double GetEngineScrollOffset()
    {
        var rawOffset = _scrollView.Orientation == ScrollOrientation.Horizontal ? _scrollView.ScrollX : _scrollView.ScrollY;
        return Math.Max(0d, rawOffset - GetLeadingContentInset());
    }

    private double GetEngineViewportMainAxisSize()
    {
        var viewport = GetEngineViewportSize(_scrollView.Width, _scrollView.Height);
        return _scrollView.Orientation == ScrollOrientation.Horizontal ? viewport.Width : viewport.Height;
    }

    private (double Width, double Height) GetEngineViewportSize(double hostWidth, double hostHeight)
    {
        var isHorizontal = _scrollView.Orientation == ScrollOrientation.Horizontal;
        var leading = GetLeadingContentInset();
        var trailing = GetAxisTrailingContentInsetForViewport();
        var crossLeading = GetCrossLeadingContentInset();
        var crossTrailing = GetCrossTrailingContentInset();
        var mainSize = Math.Max(0d, (isHorizontal ? hostWidth : hostHeight) - leading - trailing);
        var crossSize = Math.Max(0d, (isHorizontal ? hostHeight : hostWidth) - crossLeading - crossTrailing);
        return isHorizontal ? (mainSize, crossSize) : (crossSize, mainSize);
    }

    private void UpdateEffectiveContentInset(bool refreshViewport)
    {
        var safeAreaInsets = IncludeSafeAreaInsets ? GetAppliedSafeAreaInsets() : default;
        var newInset = new Thickness(
            ContentInset.Left + safeAreaInsets.Left,
            ContentInset.Top + safeAreaInsets.Top,
            ContentInset.Right + safeAreaInsets.Right,
            ContentInset.Bottom + safeAreaInsets.Bottom);

        if (Math.Abs(_effectiveContentInset.Left - newInset.Left) < 0.5 &&
            Math.Abs(_effectiveContentInset.Top - newInset.Top) < 0.5 &&
            Math.Abs(_effectiveContentInset.Right - newInset.Right) < 0.5 &&
            Math.Abs(_effectiveContentInset.Bottom - newInset.Bottom) < 0.5)
        {
            return;
        }

        _effectiveContentInset = newInset;

        if (refreshViewport && _scrollView.Width > 0 && _scrollView.Height > 0)
        {
            var viewport = GetEngineViewportSize(_scrollView.Width, _scrollView.Height);
            _engine.OnViewportChanged(viewport.Width, viewport.Height);
            _engine.OnScroll(GetEngineScrollOffset());
            UpdateStickyOverlay();
        }
    }

    private Thickness GetAppliedSafeAreaInsets()
    {
        var safeAreaInsets = GetSafeAreaInsets();
        if (Math.Abs(safeAreaInsets.Left) < 0.5 &&
            Math.Abs(safeAreaInsets.Top) < 0.5 &&
            Math.Abs(safeAreaInsets.Right) < 0.5 &&
            Math.Abs(safeAreaInsets.Bottom) < 0.5)
        {
            return default;
        }

        var parent = Parent as VisualElement;
        if (parent is null || parent.Width <= 0 || parent.Height <= 0)
        {
            return safeAreaInsets;
        }

        var left = X <= 0.5 ? safeAreaInsets.Left : 0d;
        var top = Y <= 0.5 ? safeAreaInsets.Top : 0d;
        var right = parent.Width - (X + Width) <= 0.5 ? safeAreaInsets.Right : 0d;
        var bottom = parent.Height - (Y + Height) <= 0.5 ? safeAreaInsets.Bottom : 0d;
        return new Thickness(left, top, right, bottom);
    }

    private Thickness GetSafeAreaInsets()
    {
#if IOS || MACCATALYST
        var page = FindParentPage();
        if (page is not null)
        {
            var platformConfig = page.On<Microsoft.Maui.Controls.PlatformConfiguration.iOS>();
            return Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.SafeAreaInsets(platformConfig);
        }
#elif ANDROID
        if (_scrollView.Handler?.PlatformView is Android.Views.View nativeView)
        {
            var insetsCompat = AndroidX.Core.View.ViewCompat.GetRootWindowInsets(nativeView);
            if (insetsCompat is not null)
            {
                var systemBars = insetsCompat.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
                if (systemBars is null)
                {
                    return default;
                }
                var density = nativeView.Resources?.DisplayMetrics?.Density ?? 1f;
                return new Thickness(
                    systemBars.Left / density,
                    systemBars.Top / density,
                    systemBars.Right / density,
                    systemBars.Bottom / density);
            }
        }
#endif

        return default;
    }

    private Page? FindParentPage()
    {
        Element? current = this;
        while (current is not null)
        {
            if (current is Page page)
            {
                return page;
            }

            current = current.Parent;
        }

        return null;
    }

    private void RecycleAll()
    {
        foreach (var (idx, view) in _realizedViews)
        {
            var key = _realizedTemplateKeys.Remove(idx, out var storedKey) ? storedKey : _engine.GetTemplateKey(idx);
            ReleaseView(view);
            _contentSurface.Children.Remove(view);
            _viewPool.Return(key, view);
        }

        _realizedViews.Clear();
        _realizedTemplateKeys.Clear();
    }

    private void UpdateRefreshView()
    {
        if (IsRefreshEnabled && _refreshView is null)
        {
            _refreshView = new RefreshView();
            _refreshView.Refreshing += OnRefreshViewRefreshing;
            _refreshView.IsRefreshing = IsRefreshing;
            _refreshView.Content = _scrollView;
            Content = _refreshView;
        }
        else if (!IsRefreshEnabled && _refreshView is not null)
        {
            _refreshView.Refreshing -= OnRefreshViewRefreshing;
            _refreshView.Content = null;
            Content = _scrollView;
            _refreshView = null;
        }
    }

    private void OnRefreshViewRefreshing(object? sender, EventArgs e)
    {
        if (!IsRefreshing)
        {
            IsRefreshing = true;
        }

        Refreshing?.Invoke(this, EventArgs.Empty);

        if (RefreshCommand?.CanExecute(null) == true)
        {
            RefreshCommand.Execute(null);
        }
    }

    private void OnSelectionChanged(object? sender, Core.Interactions.SelectionChangedEventArgs e)
    {
        // Update visual state for all realized item views affected by selection change.
        foreach (var (idx, view) in _realizedViews)
        {
            if (idx >= 0 && idx < _engine.Nodes.Count)
            {
                var node = _engine.Nodes[idx];
                if (node.Kind == CollectionNodeKind.Item && node.Data is not null)
                {
                    var isAffected = e.AddedItems.Contains(node.Data) || e.RemovedItems.Contains(node.Data);
                    if (isAffected)
                    {
                        ApplySelectionVisual(view, node.Data);
                    }
                }
            }
        }
    }

    private void ApplySelectionVisual(View view, object? data)
    {
        var isSelected = data is not null && _engine.Selection.IsSelected(data);
        if (!_originalBackgrounds.ContainsKey(view))
        {
            _originalBackgrounds[view] = view.Background;
        }

        view.Background = isSelected
            ? new SolidColorBrush(SelectedBackgroundColor)
            : _originalBackgrounds[view];
    }

    private void ClearSelectionVisual(View view)
    {
        if (_originalBackgrounds.TryGetValue(view, out var original))
        {
            view.Background = original;
        }
    }

    private void AttachSelectionTap(View view, object? data)
    {
        DetachSelectionTap(view);
        if (data is null)
        {
            return;
        }

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => _engine.Selection.Toggle(data);
        _selectionTapRecognizers[view] = tap;
        view.GestureRecognizers.Add(tap);
    }

    private void DetachSelectionTap(View view)
    {
        if (_selectionTapRecognizers.Remove(view, out var tap))
        {
            view.GestureRecognizers.Remove(tap);
        }
    }

    private void ReleaseView(View view)
    {
        DetachSelectionTap(view);
        ClearSelectionVisual(view);
        // Reset visual state to prevent stale properties leaking into reuse
        view.BindingContext = null;
        view.Opacity = 1d;
        view.Scale = 1d;
        view.ScaleX = 1d;
        view.ScaleY = 1d;
        view.Rotation = 0d;
        view.RotationX = 0d;
        view.RotationY = 0d;
        view.TranslationX = 0d;
        view.TranslationY = 0d;
        view.IsVisible = true;
    }

    private void OnSelectionModeChanged(SelectionMode mode)
    {
        _engine.Selection.Mode = mode;
        if (mode == SelectionMode.None)
        {
            _engine.Selection.ClearSelection();
        }

        RefreshSelectionVisuals();
    }

    private void RefreshSelectionVisuals()
    {
        foreach (var (idx, view) in _realizedViews)
        {
            if (idx < 0 || idx >= _engine.Nodes.Count)
            {
                continue;
            }

            var node = _engine.Nodes[idx];
            if (node.Kind == CollectionNodeKind.Item)
            {
                ApplySelectionVisual(view, node.Data);
            }
        }
    }

    private static View CreateDefaultView()
    {
        return new Label { Text = "•", VerticalTextAlignment = TextAlignment.Center };
    }
}
