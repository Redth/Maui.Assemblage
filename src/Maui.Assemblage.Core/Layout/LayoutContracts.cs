namespace Maui.Assemblage.Core.Layout;

[Flags]
public enum LayoutCapabilities
{
    None = 0,
    SupportsStickyHeaders = 1 << 0,
    SupportsTransforms = 1 << 1,
    SupportsSnapping = 1 << 2
}

public enum LayoutOrientation
{
    Vertical,
    Horizontal
}

public readonly record struct LayoutRect(double X, double Y, double Width, double Height);

public readonly record struct LayoutItemAttributes(
    int Index,
    LayoutRect Frame,
    int ZIndex = 0,
    double Opacity = 1d,
    double Scale = 1d,
    double RotationY = 0d,
    double TranslateX = 0d,
    double RotationX = 0d);

public readonly record struct LayoutContext(
    int ItemCount,
    double ViewportWidth,
    double ViewportHeight,
    double ScrollOffset = 0d);

public readonly record struct LayoutInvalidationContext(
    bool ViewportChanged,
    bool DataChanged,
    ItemRange? AffectedRange = null);

public readonly record struct InvalidationPlan(
    bool RequiresFullArrange,
    ItemRange? AffectedRange = null);

public sealed class LayoutSnapshot
{
    public LayoutSnapshot(double contentWidth, double contentHeight, IReadOnlyList<LayoutItemAttributes> items)
    {
        ContentWidth = contentWidth;
        ContentHeight = contentHeight;
        Items = items;
    }

    public double ContentWidth { get; }

    public double ContentHeight { get; }

    public IReadOnlyList<LayoutItemAttributes> Items { get; }
}

public interface ILayoutProvider
{
    LayoutCapabilities Capabilities { get; }

    LayoutSnapshot Arrange(LayoutContext context, ItemRange requestRange);

    InvalidationPlan Invalidate(LayoutInvalidationContext context);
}

/// <summary>
/// Optional layout-provider contract for estimating the visible item range without
/// relying on a previously arranged snapshot.
/// </summary>
public interface IVisibleRangeProvider
{
    ItemRange GetVisibleRange(LayoutContext context);
}

/// <summary>
/// Implemented by layout providers whose per-item extents can change without
/// the item count changing, such as measured dynamic item sizing.
/// </summary>
public interface IVariableExtentLayoutProvider
{
    void InvalidateExtents();
}

/// <summary>
/// Layout providers that support snapping implement this to compute snap targets.
/// </summary>
public interface ISnappingLayoutProvider : ILayoutProvider
{
    /// <summary>
    /// Returns the scroll offset that centers the item at <paramref name="index"/>.
    /// </summary>
    double GetSnapOffset(int index, double viewportSize);

    /// <summary>
    /// Given the current scroll offset and fling velocity, returns the index to snap to.
    /// Velocity is in px/s; positive = scrolling forward.
    /// </summary>
    int GetSnapTargetIndex(double offset, double velocity, int itemCount, double viewportSize);
}
