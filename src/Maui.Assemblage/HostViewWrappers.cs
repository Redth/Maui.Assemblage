using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage;

/// <summary>
/// Preconfigured list collection view using <see cref="LinearLayoutProvider"/>.
/// </summary>
public class ListHostView : CollectionHostView
{
    public static readonly BindableProperty ItemExtentProperty = BindableProperty.Create(
        nameof(ItemExtent), typeof(double), typeof(ListHostView), 48d,
        propertyChanged: (b, _, _) => ((ListHostView)b).UpdateLayout());

    public static readonly BindableProperty SpacingProperty = BindableProperty.Create(
        nameof(Spacing), typeof(double), typeof(ListHostView), 0d,
        propertyChanged: (b, _, _) => ((ListHostView)b).UpdateLayout());

    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(
        nameof(Orientation), typeof(LayoutOrientation), typeof(ListHostView),
        LayoutOrientation.Vertical,
        propertyChanged: (b, _, _) => ((ListHostView)b).UpdateLayout());

    private Func<int, double>? _itemExtentResolver;

    public double ItemExtent
    {
        get => (double)GetValue(ItemExtentProperty);
        set => SetValue(ItemExtentProperty, value);
    }

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public LayoutOrientation Orientation
    {
        get => (LayoutOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>
    /// Optional resolver for per-item extent (height in vertical, width in horizontal).
    /// When set, each item can have a different size. Falls back to <see cref="ItemExtent"/> for items that return 0 or less.
    /// </summary>
    public Func<int, double>? ItemExtentResolver
    {
        get => _itemExtentResolver;
        set { _itemExtentResolver = value; UpdateLayout(); }
    }

    public ListHostView()
    {
        UpdateLayout();
    }

    protected override void OnUseMeasuredItemExtentsChanged()
    {
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        Func<int, double>? resolver = UseMeasuredItemExtents || _itemExtentResolver is not null
            ? ResolveItemExtent
            : null;
        LayoutProvider = new LinearLayoutProvider(
            Math.Max(1d, ItemExtent),
            Math.Max(0d, Spacing),
            Orientation,
            resolver);
        ScrollDirection = Orientation == LayoutOrientation.Horizontal
            ? ScrollOrientation.Horizontal
            : ScrollOrientation.Vertical;
    }

    private double ResolveItemExtent(int index)
    {
        if (UseMeasuredItemExtents && TryGetMeasuredItemExtent(index, out var measuredExtent))
        {
            return measuredExtent;
        }

        var resolvedExtent = _itemExtentResolver?.Invoke(index) ?? ItemExtent;
        return resolvedExtent > 0d ? resolvedExtent : ItemExtent;
    }
}

/// <summary>
/// Preconfigured grid collection view using <see cref="GridLayoutProvider"/>.
/// </summary>
public class GridHostView : CollectionHostView
{
    public static readonly BindableProperty SpanCountProperty = BindableProperty.Create(
        nameof(SpanCount), typeof(int), typeof(GridHostView), 2,
        propertyChanged: (b, _, _) => ((GridHostView)b).UpdateLayout());

    public static readonly BindableProperty ItemHeightProperty = BindableProperty.Create(
        nameof(ItemHeight), typeof(double), typeof(GridHostView), 80d,
        propertyChanged: (b, _, _) => ((GridHostView)b).UpdateLayout());

    public static readonly BindableProperty HorizontalSpacingProperty = BindableProperty.Create(
        nameof(HorizontalSpacing), typeof(double), typeof(GridHostView), 0d,
        propertyChanged: (b, _, _) => ((GridHostView)b).UpdateLayout());

    public static readonly BindableProperty VerticalSpacingProperty = BindableProperty.Create(
        nameof(VerticalSpacing), typeof(double), typeof(GridHostView), 0d,
        propertyChanged: (b, _, _) => ((GridHostView)b).UpdateLayout());

    public int SpanCount
    {
        get => (int)GetValue(SpanCountProperty);
        set => SetValue(SpanCountProperty, value);
    }

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    public double HorizontalSpacing
    {
        get => (double)GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    public double VerticalSpacing
    {
        get => (double)GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    public GridHostView()
    {
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        LayoutProvider = new GridLayoutProvider(
            Math.Max(1, SpanCount),
            Math.Max(1d, ItemHeight),
            Math.Max(0d, HorizontalSpacing),
            Math.Max(0d, VerticalSpacing));
    }
}

/// <summary>
/// Preconfigured carousel collection view using <see cref="CarouselLayoutProvider"/>.
/// </summary>
public class CarouselHostView : CollectionHostView
{
    public static readonly BindableProperty PeekAmountProperty = BindableProperty.Create(
        nameof(PeekAmount), typeof(double), typeof(CarouselHostView), 0d,
        propertyChanged: (b, _, _) => ((CarouselHostView)b).UpdateLayout());

    public static readonly BindableProperty ItemSpacingProperty = BindableProperty.Create(
        nameof(ItemSpacing), typeof(double), typeof(CarouselHostView), 0d,
        propertyChanged: (b, _, _) => ((CarouselHostView)b).UpdateLayout());

    public double PeekAmount
    {
        get => (double)GetValue(PeekAmountProperty);
        set => SetValue(PeekAmountProperty, value);
    }

    public double ItemSpacing
    {
        get => (double)GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    public CarouselHostView()
    {
        SnapToCenter = true;
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        LayoutProvider = new CarouselLayoutProvider(
            Math.Max(0d, PeekAmount),
            Math.Max(0d, ItemSpacing),
            LayoutOrientation.Horizontal);
        ScrollDirection = ScrollOrientation.Horizontal;
    }
}

/// <summary>
/// Preconfigured staggered/masonry grid using <see cref="StaggeredGridLayoutProvider"/>.
/// </summary>
public class StaggeredGridHostView : CollectionHostView
{
    public static readonly BindableProperty SpanCountProperty = BindableProperty.Create(
        nameof(SpanCount), typeof(int), typeof(StaggeredGridHostView), 2,
        propertyChanged: (b, _, _) => ((StaggeredGridHostView)b).UpdateLayout());

    public static readonly BindableProperty DefaultItemHeightProperty = BindableProperty.Create(
        nameof(DefaultItemHeight), typeof(double), typeof(StaggeredGridHostView), 100d,
        propertyChanged: (b, _, _) => ((StaggeredGridHostView)b).UpdateLayout());

    public static readonly BindableProperty HorizontalSpacingProperty = BindableProperty.Create(
        nameof(HorizontalSpacing), typeof(double), typeof(StaggeredGridHostView), 0d,
        propertyChanged: (b, _, _) => ((StaggeredGridHostView)b).UpdateLayout());

    public static readonly BindableProperty VerticalSpacingProperty = BindableProperty.Create(
        nameof(VerticalSpacing), typeof(double), typeof(StaggeredGridHostView), 0d,
        propertyChanged: (b, _, _) => ((StaggeredGridHostView)b).UpdateLayout());

    private Func<int, double>? _itemHeightResolver;

    public int SpanCount
    {
        get => (int)GetValue(SpanCountProperty);
        set => SetValue(SpanCountProperty, value);
    }

    public double DefaultItemHeight
    {
        get => (double)GetValue(DefaultItemHeightProperty);
        set => SetValue(DefaultItemHeightProperty, value);
    }

    public double HorizontalSpacing
    {
        get => (double)GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    public double VerticalSpacing
    {
        get => (double)GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    /// <summary>Optional callback returning height per item index for staggered effect.</summary>
    public Func<int, double>? ItemHeightResolver
    {
        get => _itemHeightResolver;
        set { _itemHeightResolver = value; UpdateLayout(); }
    }

    public StaggeredGridHostView()
    {
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        LayoutProvider = new StaggeredGridLayoutProvider(
            Math.Max(1, SpanCount),
            Math.Max(1d, DefaultItemHeight),
            Math.Max(0d, HorizontalSpacing),
            Math.Max(0d, VerticalSpacing),
            itemHeightResolver: _itemHeightResolver);
    }
}

/// <summary>
/// Preconfigured wrap/flow layout using <see cref="WrapLayoutProvider"/>.
/// </summary>
public class WrapHostView : CollectionHostView
{
    public static readonly BindableProperty ItemWidthProperty = BindableProperty.Create(
        nameof(ItemWidth), typeof(double), typeof(WrapHostView), 80d,
        propertyChanged: (b, _, _) => ((WrapHostView)b).UpdateLayout());

    public static readonly BindableProperty ItemHeightProperty = BindableProperty.Create(
        nameof(ItemHeight), typeof(double), typeof(WrapHostView), 40d,
        propertyChanged: (b, _, _) => ((WrapHostView)b).UpdateLayout());

    public static readonly BindableProperty HorizontalSpacingProperty = BindableProperty.Create(
        nameof(HorizontalSpacing), typeof(double), typeof(WrapHostView), 0d,
        propertyChanged: (b, _, _) => ((WrapHostView)b).UpdateLayout());

    public static readonly BindableProperty VerticalSpacingProperty = BindableProperty.Create(
        nameof(VerticalSpacing), typeof(double), typeof(WrapHostView), 0d,
        propertyChanged: (b, _, _) => ((WrapHostView)b).UpdateLayout());

    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(
        nameof(Orientation), typeof(LayoutOrientation), typeof(WrapHostView),
        LayoutOrientation.Vertical,
        propertyChanged: (b, _, _) => ((WrapHostView)b).UpdateLayout());

    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    public double HorizontalSpacing
    {
        get => (double)GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    public double VerticalSpacing
    {
        get => (double)GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    public LayoutOrientation Orientation
    {
        get => (LayoutOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>
    /// Optional resolver for per-item width. When set, items have variable widths
    /// and the layout flows/wraps based on available space. Signature: index → width.
    /// </summary>
    public Func<int, double>? ItemWidthResolver
    {
        get => _itemWidthResolver;
        set { _itemWidthResolver = value; UpdateLayout(); }
    }

    private Func<int, double>? _itemWidthResolver;

    public WrapHostView()
    {
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        LayoutProvider = new WrapLayoutProvider(
            Math.Max(1d, ItemWidth),
            Math.Max(1d, ItemHeight),
            Math.Max(0d, HorizontalSpacing),
            Math.Max(0d, VerticalSpacing),
            Orientation,
            ItemWidthResolver);
    }
}

/// <summary>
/// Preconfigured coverflow collection view using <see cref="CoverFlowLayoutProvider"/>.
/// </summary>
/// <summary>
/// CoverFlow host view. Scrolls horizontally with scale, rotation, opacity, and
/// translation transforms for the classic Apple CoverFlow effect.
/// </summary>
public class LibraryHostView : CollectionHostView
{
    public static readonly BindableProperty ItemWidthProperty = BindableProperty.Create(
        nameof(ItemWidth), typeof(double), typeof(LibraryHostView), 180d,
        propertyChanged: (b, _, _) => ((LibraryHostView)b).UpdateLayout());

    public static readonly BindableProperty ItemHeightProperty = BindableProperty.Create(
        nameof(ItemHeight), typeof(double), typeof(LibraryHostView), 0d,
        propertyChanged: (b, _, _) => ((LibraryHostView)b).UpdateLayout());

    public static readonly BindableProperty ItemSpacingProperty = BindableProperty.Create(
        nameof(ItemSpacing), typeof(double), typeof(LibraryHostView), -40d,
        propertyChanged: (b, _, _) => ((LibraryHostView)b).UpdateLayout());

    public static readonly BindableProperty MinOpacityProperty = BindableProperty.Create(
        nameof(MinOpacity), typeof(double), typeof(LibraryHostView), 0.4d,
        propertyChanged: (b, _, _) => ((LibraryHostView)b).UpdateLayout());

    public static readonly BindableProperty MinScaleProperty = BindableProperty.Create(
        nameof(MinScale), typeof(double), typeof(LibraryHostView), 0.65d,
        propertyChanged: (b, _, _) => ((LibraryHostView)b).UpdateLayout());

    public static readonly BindableProperty MaxRotationProperty = BindableProperty.Create(
        nameof(MaxRotation), typeof(double), typeof(LibraryHostView), 60d,
        propertyChanged: (b, _, _) => ((LibraryHostView)b).UpdateLayout());

    public static readonly BindableProperty PerspectiveDepthProperty = BindableProperty.Create(
        nameof(PerspectiveDepth), typeof(double), typeof(LibraryHostView), 3d,
        propertyChanged: (b, _, _) => ((LibraryHostView)b).UpdateLayout());

    public static readonly BindableProperty SideOffsetProperty = BindableProperty.Create(
        nameof(SideOffset), typeof(double), typeof(LibraryHostView), 80d,
        propertyChanged: (b, _, _) => ((LibraryHostView)b).UpdateLayout());

    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    /// <summary>Item height. 0 = stretch to fill available height.</summary>
    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    /// <summary>Spacing between item leading edges. Negative = overlap.</summary>
    public double ItemSpacing
    {
        get => (double)GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    public double MinOpacity
    {
        get => (double)GetValue(MinOpacityProperty);
        set => SetValue(MinOpacityProperty, value);
    }

    public double MinScale
    {
        get => (double)GetValue(MinScaleProperty);
        set => SetValue(MinScaleProperty, value);
    }

    /// <summary>Max Y-axis rotation in degrees (0-90).</summary>
    public double MaxRotation
    {
        get => (double)GetValue(MaxRotationProperty);
        set => SetValue(MaxRotationProperty, value);
    }

    /// <summary>Items-from-center to reach full effect. Higher = more gradual.</summary>
    public double PerspectiveDepth
    {
        get => (double)GetValue(PerspectiveDepthProperty);
        set => SetValue(PerspectiveDepthProperty, value);
    }

    /// <summary>Pixel shift to compress side items toward center. 0 = no compression.</summary>
    public double SideOffset
    {
        get => (double)GetValue(SideOffsetProperty);
        set => SetValue(SideOffsetProperty, value);
    }

    public LibraryHostView()
    {
        ScrollDirection = ScrollOrientation.Horizontal;
        SnapToCenter = true;
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        LayoutProvider = new CoverFlowLayoutProvider(
            Math.Max(1d, ItemWidth),
            Math.Max(0d, ItemHeight),
            ItemSpacing,
            maxRotationDegrees: Math.Clamp(MaxRotation, 0d, 90d),
            minScale: Math.Clamp(MinScale, 0.1d, 1d),
            minOpacity: Math.Clamp(MinOpacity, 0d, 1d),
            perspectiveDepth: Math.Max(0.5d, PerspectiveDepth),
            sideOffset: Math.Max(0d, SideOffset));
    }
}

/// <summary>
/// Timeline collection view. Items alternate left and right of a center spine.
/// </summary>
public class TimelineHostView : CollectionHostView
{
    public static readonly BindableProperty ItemHeightProperty = BindableProperty.Create(
        nameof(ItemHeight), typeof(double), typeof(TimelineHostView), 80d,
        propertyChanged: (b, _, _) => ((TimelineHostView)b).UpdateLayout());

    public static readonly BindableProperty SpacingProperty = BindableProperty.Create(
        nameof(Spacing), typeof(double), typeof(TimelineHostView), 12d,
        propertyChanged: (b, _, _) => ((TimelineHostView)b).UpdateLayout());

    public static readonly BindableProperty SpineWidthProperty = BindableProperty.Create(
        nameof(SpineWidth), typeof(double), typeof(TimelineHostView), 2d,
        propertyChanged: (b, _, _) => ((TimelineHostView)b).UpdateLayout());

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public double SpineWidth
    {
        get => (double)GetValue(SpineWidthProperty);
        set => SetValue(SpineWidthProperty, value);
    }

    /// <summary>Optional resolver for per-item heights.</summary>
    public Func<int, double>? ItemHeightResolver
    {
        get => _itemHeightResolver;
        set { _itemHeightResolver = value; UpdateLayout(); }
    }
    private Func<int, double>? _itemHeightResolver;

    public TimelineHostView()
    {
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        LayoutProvider = new TimelineLayoutProvider(
            Math.Max(1d, ItemHeight),
            Math.Max(0d, Spacing),
            Math.Max(0d, SpineWidth),
            itemHeightResolver: _itemHeightResolver);
    }
}

/// <summary>
/// Cylindrical wheel/picker collection view. Items curve on the X-axis for a barrel effect.
/// Snaps to center item.
/// </summary>
public class WheelHostView : CollectionHostView
{
    public static readonly BindableProperty ItemHeightProperty = BindableProperty.Create(
        nameof(ItemHeight), typeof(double), typeof(WheelHostView), 44d,
        propertyChanged: (b, _, _) => ((WheelHostView)b).UpdateLayout());

    public static readonly BindableProperty MaxRotationProperty = BindableProperty.Create(
        nameof(MaxRotation), typeof(double), typeof(WheelHostView), 70d,
        propertyChanged: (b, _, _) => ((WheelHostView)b).UpdateLayout());

    public static readonly BindableProperty MinScaleProperty = BindableProperty.Create(
        nameof(MinScale), typeof(double), typeof(WheelHostView), 0.75d,
        propertyChanged: (b, _, _) => ((WheelHostView)b).UpdateLayout());

    public static readonly BindableProperty MinOpacityProperty = BindableProperty.Create(
        nameof(MinOpacity), typeof(double), typeof(WheelHostView), 0.35d,
        propertyChanged: (b, _, _) => ((WheelHostView)b).UpdateLayout());

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    public double MaxRotation
    {
        get => (double)GetValue(MaxRotationProperty);
        set => SetValue(MaxRotationProperty, value);
    }

    public double MinScale
    {
        get => (double)GetValue(MinScaleProperty);
        set => SetValue(MinScaleProperty, value);
    }

    public double MinOpacity
    {
        get => (double)GetValue(MinOpacityProperty);
        set => SetValue(MinOpacityProperty, value);
    }

    public WheelHostView()
    {
        SnapToCenter = true;
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        LayoutProvider = new WheelLayoutProvider(
            Math.Max(1d, ItemHeight),
            maxRotationDegrees: Math.Clamp(MaxRotation, 0d, 90d),
            minScale: Math.Clamp(MinScale, 0.1d, 1d),
            minOpacity: Math.Clamp(MinOpacity, 0d, 1d));
    }
}
