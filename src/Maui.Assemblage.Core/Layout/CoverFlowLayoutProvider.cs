namespace Maui.Assemblage.Core.Layout;

/// <summary>
/// Classic Apple-style CoverFlow layout. The center item is displayed at full scale
/// facing the viewer, while side items progressively shrink, rotate away on the Y axis,
/// and slide inward to create depth. Items overlap via z-index ordering.
/// All visual parameters are fully configurable.
/// </summary>
public sealed class CoverFlowLayoutProvider : ISnappingLayoutProvider
{
    public CoverFlowLayoutProvider(
        double itemWidth,
        double itemHeight = 0d,
        double itemSpacing = 0d,
        double maxRotationDegrees = 60d,
        double maxScale = 1d,
        double minScale = 0.65d,
        double minOpacity = 0.4d,
        double perspectiveDepth = 3d,
        double sideOffset = 80d,
        LayoutOrientation orientation = LayoutOrientation.Horizontal)
    {
        if (itemWidth <= 0d)
            throw new ArgumentOutOfRangeException(nameof(itemWidth));
        if (perspectiveDepth <= 0d)
            throw new ArgumentOutOfRangeException(nameof(perspectiveDepth));

        ItemWidth = itemWidth;
        ItemHeight = itemHeight;
        ItemSpacing = itemSpacing;
        MaxRotationDegrees = maxRotationDegrees;
        MaxScale = maxScale;
        MinScale = minScale;
        MinOpacity = minOpacity;
        PerspectiveDepth = perspectiveDepth;
        SideOffset = sideOffset;
        Orientation = orientation;
    }

    /// <summary>Width (primary axis extent) of each item.</summary>
    public double ItemWidth { get; }
    /// <summary>Height (secondary axis extent). 0 = stretch to viewport.</summary>
    public double ItemHeight { get; }
    /// <summary>Spacing between item leading edges. Negative values cause overlap.</summary>
    public double ItemSpacing { get; }
    /// <summary>Maximum Y-axis rotation for fully-off-center items.</summary>
    public double MaxRotationDegrees { get; }
    /// <summary>Scale of the center (focused) item.</summary>
    public double MaxScale { get; }
    /// <summary>Scale of items at the maximum perspective depth.</summary>
    public double MinScale { get; }
    /// <summary>Opacity of items at the maximum perspective depth.</summary>
    public double MinOpacity { get; }
    /// <summary>Number of item-pitches from center to reach full effect (rotation, scale, opacity).
    /// Higher = more gradual transition. Default 3.</summary>
    public double PerspectiveDepth { get; }
    /// <summary>Horizontal pixel shift applied to side items to compress them toward center.
    /// Creates the classic CoverFlow stacking look. 0 = no compression.</summary>
    public double SideOffset { get; }
    public LayoutOrientation Orientation { get; }

    public LayoutCapabilities Capabilities => LayoutCapabilities.SupportsTransforms | LayoutCapabilities.SupportsSnapping;

    public LayoutSnapshot Arrange(LayoutContext context, ItemRange requestRange)
    {
        if (context.ItemCount < 0)
            throw new ArgumentOutOfRangeException(nameof(context));

        var clampedRange = requestRange.Clamp(0, context.ItemCount);
        var items = new List<LayoutItemAttributes>(clampedRange.Length);

        var isVertical = Orientation == LayoutOrientation.Vertical;
        var viewportPrimary = isVertical ? context.ViewportHeight : context.ViewportWidth;
        var viewportSecondary = isVertical ? context.ViewportWidth : context.ViewportHeight;

        var actualItemHeight = ItemHeight > 0 ? ItemHeight : viewportSecondary;
        var secondaryOffset = (viewportSecondary - actualItemHeight) / 2d;

        var pitch = ItemWidth + ItemSpacing;
        var centerOffset = (viewportPrimary - ItemWidth) / 2d;
        // The center of the visible viewport in content coordinates
        var viewportCenter = context.ScrollOffset + (viewportPrimary / 2d);

        for (var index = clampedRange.Start; index < clampedRange.EndExclusive; index++)
        {
            var itemCenter = centerOffset + (index * pitch) + (ItemWidth / 2d);
            var distanceFromCenter = (itemCenter - viewportCenter) / pitch;
            var absDistance = Math.Abs(distanceFromCenter);

            // Normalize to 0..1 over PerspectiveDepth items, then smoothstep
            var t = Math.Clamp(absDistance / PerspectiveDepth, 0d, 1d);
            var eased = t * t * (3d - 2d * t);

            var scale = MaxScale - ((MaxScale - MinScale) * eased);

            var sign = distanceFromCenter < 0 ? 1d : -1d;
            var rotation = sign * MaxRotationDegrees * eased;

            var opacity = Math.Max(MinOpacity, 1d - ((1d - MinOpacity) * eased));

            var zIndex = 1000 - (int)(absDistance * 100);

            // Side offset: shift items toward center. The shift increases with distance
            // so far items stack tightly while the center item stands alone.
            var sideShift = -sign * SideOffset * eased;

            var leading = centerOffset + (index * pitch);

            var frame = isVertical
                ? new LayoutRect(secondaryOffset, leading, actualItemHeight, ItemWidth)
                : new LayoutRect(leading, secondaryOffset, ItemWidth, actualItemHeight);

            items.Add(new LayoutItemAttributes(
                index, frame, zIndex, opacity, scale,
                isVertical ? 0d : rotation,
                isVertical ? 0d : sideShift));
        }

        var contentPrimary = context.ItemCount == 0
            ? 0d
            : (2d * centerOffset) + (context.ItemCount * ItemWidth) + ((context.ItemCount - 1) * ItemSpacing);

        var contentWidth = isVertical ? context.ViewportWidth : contentPrimary;
        var contentHeight = isVertical ? contentPrimary : context.ViewportHeight;

        return new LayoutSnapshot(contentWidth, contentHeight, items);
    }

    public InvalidationPlan Invalidate(LayoutInvalidationContext context)
    {
        return new InvalidationPlan(context.ViewportChanged || context.DataChanged);
    }

    public double GetSnapOffset(int index, double viewportSize)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        var pitch = ItemWidth + ItemSpacing;
        return index * pitch;
    }

    public int GetSnapTargetIndex(double offset, double velocity, int itemCount, double viewportSize)
    {
        if (itemCount <= 0)
            return 0;

        var pitch = ItemWidth + ItemSpacing;
        if (pitch <= 0)
            return 0;

        var projectedOffset = offset + (velocity * 0.3d);
        var rawIndex = projectedOffset / pitch;
        var snapped = (int)Math.Round(rawIndex, MidpointRounding.AwayFromZero);
        return Math.Clamp(snapped, 0, itemCount - 1);
    }
}
