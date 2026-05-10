namespace Maui.Assemblage.Core.Layout;

/// <summary>
/// Cylindrical wheel/picker layout. Items are arranged vertically, and those away
/// from the center are rotated on the X-axis to create a 3D barrel effect, similar
/// to the iOS UIPickerView. Center items appear flat and full-size; side items
/// curve away with reduced scale and opacity.
/// </summary>
public sealed class WheelLayoutProvider : ISnappingLayoutProvider, IVisibleRangeProvider
{
    public WheelLayoutProvider(
        double itemHeight = 44d,
        double visibleItems = 5d,
        double maxRotationDegrees = 70d,
        double minScale = 0.75d,
        double minOpacity = 0.35d,
        double cylinderRadius = 3d)
    {
        if (itemHeight <= 0d)
            throw new ArgumentOutOfRangeException(nameof(itemHeight));
        if (visibleItems <= 0d)
            throw new ArgumentOutOfRangeException(nameof(visibleItems));

        ItemHeight = itemHeight;
        VisibleItems = visibleItems;
        MaxRotationDegrees = maxRotationDegrees;
        MinScale = minScale;
        MinOpacity = minOpacity;
        CylinderRadius = Math.Max(0.5d, cylinderRadius);
    }

    /// <summary>Height of each item row.</summary>
    public double ItemHeight { get; }

    /// <summary>Number of visible items (determines viewport height usage).</summary>
    public double VisibleItems { get; }

    /// <summary>Maximum X-axis rotation for items at the edge (degrees).</summary>
    public double MaxRotationDegrees { get; }

    /// <summary>Minimum scale for items at the edge.</summary>
    public double MinScale { get; }

    /// <summary>Minimum opacity for items at the edge.</summary>
    public double MinOpacity { get; }

    /// <summary>Number of item-pitches from center to reach full rotation/scale effect.
    /// Higher = more gradual curve.</summary>
    public double CylinderRadius { get; }

    public LayoutCapabilities Capabilities =>
        LayoutCapabilities.SupportsTransforms | LayoutCapabilities.SupportsSnapping;

    public LayoutSnapshot Arrange(LayoutContext context, ItemRange requestRange)
    {
        if (context.ItemCount < 0)
            throw new ArgumentOutOfRangeException(nameof(context));

        var clampedRange = requestRange.Clamp(0, context.ItemCount);
        var items = new List<LayoutItemAttributes>(clampedRange.Length);

        var viewportCenter = context.ScrollOffset + (context.ViewportHeight / 2d);
        var centerOffset = (context.ViewportHeight - ItemHeight) / 2d;

        for (var index = clampedRange.Start; index < clampedRange.EndExclusive; index++)
        {
            var itemCenterY = centerOffset + (index * ItemHeight) + (ItemHeight / 2d);
            var distanceFromCenter = (itemCenterY - viewportCenter) / ItemHeight;
            var absDistance = Math.Abs(distanceFromCenter);

            // Smoothstep easing for natural barrel curve
            var t = Math.Clamp(absDistance / CylinderRadius, 0d, 1d);
            var eased = t * t * (3d - 2d * t);

            var scale = 1d - ((1d - MinScale) * eased);
            var opacity = Math.Max(MinOpacity, 1d - ((1d - MinOpacity) * eased));

            // Rotation around X-axis: positive for items above center, negative for below
            var sign = distanceFromCenter < 0 ? -1d : 1d;
            var rotationX = sign * MaxRotationDegrees * eased;

            // Z-index: center items on top
            var zIndex = 1000 - (int)(absDistance * 100);

            // Vertical compression: items away from center move toward center
            var yCompression = sign * ItemHeight * 0.15d * eased;

            var y = centerOffset + (index * ItemHeight) - yCompression;
            var frame = new LayoutRect(0d, y, context.ViewportWidth, ItemHeight);

            items.Add(new LayoutItemAttributes(
                index, frame, zIndex, opacity, scale,
                0d,         // RotationY
                0d,         // TranslateX
                rotationX));
        }

        // Content extends above and below to allow scrolling
        var contentHeight = context.ItemCount == 0
            ? 0d
            : (2d * centerOffset) + (context.ItemCount * ItemHeight);

        return new LayoutSnapshot(context.ViewportWidth, contentHeight, items);
    }

    public InvalidationPlan Invalidate(LayoutInvalidationContext context)
    {
        return new InvalidationPlan(context.ViewportChanged || context.DataChanged);
    }

    public double GetSnapOffset(int index, double viewportSize)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        return index * ItemHeight;
    }

    public int GetSnapTargetIndex(double offset, double velocity, int itemCount, double viewportSize)
    {
        if (itemCount <= 0)
            return 0;

        var projectedOffset = offset + (velocity * 0.25d);
        var rawIndex = projectedOffset / ItemHeight;
        var snapped = (int)Math.Round(rawIndex, MidpointRounding.AwayFromZero);
        return Math.Clamp(snapped, 0, itemCount - 1);
    }

    public ItemRange GetVisibleRange(LayoutContext context)
    {
        if (context.ItemCount <= 0)
        {
            return ItemRange.Empty;
        }

        var safeOffset = Math.Max(0d, context.ScrollOffset);
        var first = (int)Math.Floor(safeOffset / ItemHeight);
        var last = (int)Math.Floor((safeOffset + Math.Max(0d, context.ViewportHeight)) / ItemHeight);
        return new ItemRange(
            Math.Clamp(first, 0, context.ItemCount),
            Math.Clamp(last + 1, 0, context.ItemCount));
    }
}
