namespace Maui.Assemblage.Core.Layout;

public sealed class CarouselLayoutProvider : ISnappingLayoutProvider, IVisibleRangeProvider
{
    public CarouselLayoutProvider(
        double peekAmount = 0d,
        double itemSpacing = 0d,
        LayoutOrientation orientation = LayoutOrientation.Horizontal)
    {
        if (peekAmount < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(peekAmount));
        }

        if (itemSpacing < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(itemSpacing));
        }

        PeekAmount = peekAmount;
        ItemSpacing = itemSpacing;
        Orientation = orientation;
    }

    public double PeekAmount { get; }

    public double ItemSpacing { get; }

    public LayoutOrientation Orientation { get; }

    public LayoutCapabilities Capabilities => LayoutCapabilities.SupportsSnapping;

    public LayoutSnapshot Arrange(LayoutContext context, ItemRange requestRange)
    {
        if (context.ItemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(context));
        }

        var clampedRange = requestRange.Clamp(0, context.ItemCount);
        var items = new List<LayoutItemAttributes>(clampedRange.Length);

        var isVertical = Orientation == LayoutOrientation.Vertical;
        var viewportPrimary = isVertical ? context.ViewportHeight : context.ViewportWidth;
        var viewportSecondary = isVertical ? context.ViewportWidth : context.ViewportHeight;

        // Each item fills the viewport minus peek on both sides
        var itemExtent = viewportPrimary - (2d * PeekAmount);
        if (itemExtent < 0d)
        {
            itemExtent = 0d;
        }

        var pitch = itemExtent + ItemSpacing;

        for (var index = clampedRange.Start; index < clampedRange.EndExclusive; index++)
        {
            var leading = PeekAmount + (index * pitch);
            var frame = isVertical
                ? new LayoutRect(0d, leading, viewportSecondary, itemExtent)
                : new LayoutRect(leading, 0d, itemExtent, viewportSecondary);
            items.Add(new LayoutItemAttributes(index, frame));
        }

        var contentPrimary = context.ItemCount == 0
            ? 0d
            : (2d * PeekAmount) + (context.ItemCount * itemExtent) + ((context.ItemCount - 1) * ItemSpacing);

        var contentWidth = isVertical ? context.ViewportWidth : contentPrimary;
        var contentHeight = isVertical ? contentPrimary : context.ViewportHeight;

        return new LayoutSnapshot(contentWidth, contentHeight, items);
    }

    public InvalidationPlan Invalidate(LayoutInvalidationContext context)
    {
        // Viewport changes always require full re-layout since item size depends on viewport
        if (context.ViewportChanged || context.DataChanged)
        {
            return new InvalidationPlan(true);
        }

        return new InvalidationPlan(false);
    }

    /// <summary>
    /// Computes the scroll offset that centers the given item index in the viewport.
    /// </summary>
    public double GetSnapOffset(int index, double viewportSize)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var itemExtent = viewportSize - (2d * PeekAmount);
        if (itemExtent < 0d)
        {
            itemExtent = 0d;
        }

        var pitch = itemExtent + ItemSpacing;
        return index * pitch;
    }

    /// <summary>
    /// Given a scroll offset and velocity, returns the index of the item to snap to.
    /// </summary>
    public int GetSnapTargetIndex(double offset, double velocity, int itemCount, double viewportSize)
    {
        if (itemCount <= 0)
        {
            return 0;
        }

        var itemExtent = viewportSize - (2d * PeekAmount);
        if (itemExtent <= 0d)
        {
            return 0;
        }

        var pitch = itemExtent + ItemSpacing;

        // Project offset forward based on velocity for momentum targeting
        var projectedOffset = offset + (velocity * 0.3d);
        var rawIndex = projectedOffset / pitch;
        var snapped = (int)Math.Round(rawIndex, MidpointRounding.AwayFromZero);
        return Math.Clamp(snapped, 0, itemCount - 1);
    }

    public ItemRange GetVisibleRange(LayoutContext context)
    {
        if (context.ItemCount <= 0)
        {
            return ItemRange.Empty;
        }

        var viewportPrimary = Orientation == LayoutOrientation.Vertical
            ? context.ViewportHeight
            : context.ViewportWidth;
        var itemExtent = Math.Max(0d, viewportPrimary - (2d * PeekAmount));
        var pitch = itemExtent + ItemSpacing;
        if (pitch <= 0d)
        {
            return new ItemRange(0, Math.Min(context.ItemCount, 1));
        }

        var safeOffset = Math.Max(0d, context.ScrollOffset);
        var first = (int)Math.Floor(Math.Max(0d, safeOffset - PeekAmount) / pitch);
        var last = (int)Math.Floor((safeOffset + Math.Max(0d, viewportPrimary)) / pitch);
        return new ItemRange(
            Math.Clamp(first, 0, context.ItemCount),
            Math.Clamp(last + 1, 0, context.ItemCount));
    }
}
