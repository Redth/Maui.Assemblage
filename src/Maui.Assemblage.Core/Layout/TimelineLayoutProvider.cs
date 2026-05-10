namespace Maui.Assemblage.Core.Layout;

/// <summary>
/// Timeline layout: items alternate left and right of a center spine.
/// Even-indexed items are placed on the left, odd-indexed on the right.
/// Supports variable item heights via an optional resolver.
/// </summary>
public sealed class TimelineLayoutProvider : ILayoutProvider
{
    public TimelineLayoutProvider(
        double itemHeight = 80d,
        double verticalSpacing = 12d,
        double spineWidth = 2d,
        double itemInset = 8d,
        Func<int, double>? itemHeightResolver = null)
    {
        if (itemHeight <= 0d)
            throw new ArgumentOutOfRangeException(nameof(itemHeight));

        ItemHeight = itemHeight;
        VerticalSpacing = verticalSpacing;
        SpineWidth = spineWidth;
        ItemInset = itemInset;
        _itemHeightResolver = itemHeightResolver;
    }

    /// <summary>Default height of each item (used when no resolver is provided).</summary>
    public double ItemHeight { get; }

    /// <summary>Vertical spacing between items.</summary>
    public double VerticalSpacing { get; }

    /// <summary>Width of the center spine/line (for visual reference).</summary>
    public double SpineWidth { get; }

    /// <summary>Horizontal inset between spine and item edge.</summary>
    public double ItemInset { get; }

    private readonly Func<int, double>? _itemHeightResolver;

    public LayoutCapabilities Capabilities => LayoutCapabilities.None;

    public LayoutSnapshot Arrange(LayoutContext context, ItemRange requestRange)
    {
        if (context.ItemCount < 0)
            throw new ArgumentOutOfRangeException(nameof(context));

        var clampedRange = requestRange.Clamp(0, context.ItemCount);
        var items = new List<LayoutItemAttributes>(clampedRange.Length);

        var halfViewport = context.ViewportWidth / 2d;
        var itemWidth = halfViewport - ItemInset - (SpineWidth / 2d);
        if (itemWidth < 20d)
            itemWidth = 20d;

        // Build prefix sums for Y positions (need full scan for variable heights)
        var yPositions = new double[context.ItemCount + 1];
        yPositions[0] = VerticalSpacing;
        for (var i = 0; i < context.ItemCount; i++)
        {
            var h = _itemHeightResolver?.Invoke(i) ?? ItemHeight;
            yPositions[i + 1] = yPositions[i] + h + VerticalSpacing;
        }

        for (var index = clampedRange.Start; index < clampedRange.EndExclusive; index++)
        {
            var isLeft = index % 2 == 0;
            var h = _itemHeightResolver?.Invoke(index) ?? ItemHeight;
            var y = yPositions[index];

            var x = isLeft
                ? (SpineWidth / 2d) + ItemInset - halfViewport + (context.ViewportWidth / 2d) - itemWidth
                : halfViewport + (SpineWidth / 2d) + ItemInset;

            // Simpler: left side starts at left edge, right side starts at center + inset
            x = isLeft
                ? ItemInset
                : halfViewport + (SpineWidth / 2d) + ItemInset;

            var frame = new LayoutRect(x, y, itemWidth, h);
            items.Add(new LayoutItemAttributes(index, frame));
        }

        var contentHeight = context.ItemCount > 0
            ? yPositions[context.ItemCount]
            : 0d;

        return new LayoutSnapshot(context.ViewportWidth, contentHeight, items);
    }

    public InvalidationPlan Invalidate(LayoutInvalidationContext context)
    {
        return new InvalidationPlan(context.ViewportChanged || context.DataChanged);
    }
}
