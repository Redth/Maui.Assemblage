namespace Maui.Assemblage.Core.Layout;

/// <summary>
/// Flow/wrap layout: items are placed left-to-right (or top-to-bottom),
/// wrapping to the next row (or column) when they exceed the viewport extent.
/// Similar to CSS flexbox with flex-wrap: wrap.
/// Supports per-item widths via an optional resolver for variable-size tag clouds.
/// </summary>
public sealed class WrapLayoutProvider : ILayoutProvider
{
    public WrapLayoutProvider(
        double itemWidth,
        double itemHeight,
        double horizontalSpacing = 0d,
        double verticalSpacing = 0d,
        LayoutOrientation orientation = LayoutOrientation.Vertical,
        Func<int, double>? itemWidthResolver = null)
    {
        if (itemWidth <= 0d)
            throw new ArgumentOutOfRangeException(nameof(itemWidth));
        if (itemHeight <= 0d)
            throw new ArgumentOutOfRangeException(nameof(itemHeight));
        if (horizontalSpacing < 0d)
            throw new ArgumentOutOfRangeException(nameof(horizontalSpacing));
        if (verticalSpacing < 0d)
            throw new ArgumentOutOfRangeException(nameof(verticalSpacing));

        ItemWidth = itemWidth;
        ItemHeight = itemHeight;
        HorizontalSpacing = horizontalSpacing;
        VerticalSpacing = verticalSpacing;
        Orientation = orientation;
        ItemWidthResolver = itemWidthResolver;
    }

    public double ItemWidth { get; }
    public double ItemHeight { get; }
    public double HorizontalSpacing { get; }
    public double VerticalSpacing { get; }
    public LayoutOrientation Orientation { get; }

    /// <summary>
    /// Optional resolver for per-item cross-axis extent (width in vertical mode, height in horizontal).
    /// When null, all items use <see cref="ItemWidth"/>.
    /// </summary>
    public Func<int, double>? ItemWidthResolver { get; }

    public LayoutCapabilities Capabilities => LayoutCapabilities.None;

    public LayoutSnapshot Arrange(LayoutContext context, ItemRange requestRange)
    {
        if (context.ItemCount < 0)
            throw new ArgumentOutOfRangeException(nameof(context));

        var clampedRange = requestRange.Clamp(0, context.ItemCount);
        var items = new List<LayoutItemAttributes>(clampedRange.Length);

        var isVertical = Orientation == LayoutOrientation.Vertical;
        var crossExtent = isVertical ? context.ViewportWidth : context.ViewportHeight;
        var itemMain = isVertical ? ItemHeight : ItemWidth;
        var mainSpacing = isVertical ? VerticalSpacing : HorizontalSpacing;
        var crossSpacing = isVertical ? HorizontalSpacing : VerticalSpacing;

        if (ItemWidthResolver is not null)
        {
            // Variable-width flow: walk all items up to the request range to compute positions
            var crossPos = 0d;
            var mainPos = 0d;
            var lineMaxMain = itemMain;

            for (var index = 0; index < clampedRange.EndExclusive; index++)
            {
                var itemCross = ItemWidthResolver(index);

                // Wrap to next line if this item would exceed the viewport
                if (crossPos > 0 && crossPos + itemCross > crossExtent)
                {
                    mainPos += lineMaxMain + mainSpacing;
                    crossPos = 0d;
                    lineMaxMain = itemMain;
                }

                if (index >= clampedRange.Start)
                {
                    var frame = isVertical
                        ? new LayoutRect(crossPos, mainPos, itemCross, ItemHeight)
                        : new LayoutRect(mainPos, crossPos, ItemWidth, itemCross);
                    items.Add(new LayoutItemAttributes(index, frame));
                }

                crossPos += itemCross + crossSpacing;
            }

            // Compute total content size by walking all items
            var totalCrossPos = 0d;
            var totalMainPos = 0d;

            for (var index = 0; index < context.ItemCount; index++)
            {
                var itemCross = ItemWidthResolver(index);
                if (totalCrossPos > 0 && totalCrossPos + itemCross > crossExtent)
                {
                    totalMainPos += itemMain + mainSpacing;
                    totalCrossPos = 0d;
                }
                totalCrossPos += itemCross + crossSpacing;
            }

            var contentMain = totalMainPos + itemMain;
            var contentWidth = isVertical ? context.ViewportWidth : contentMain;
            var contentHeight = isVertical ? contentMain : context.ViewportHeight;
            return new LayoutSnapshot(contentWidth, contentHeight, items);
        }

        // Fixed-width fast path (original algorithm)
        var fixedItemCross = isVertical ? ItemWidth : ItemHeight;
        var itemsPerLine = Math.Max(1, (int)((crossExtent + crossSpacing) / (fixedItemCross + crossSpacing)));

        for (var index = clampedRange.Start; index < clampedRange.EndExclusive; index++)
        {
            var lineIndex = index / itemsPerLine;
            var posInLine = index % itemsPerLine;

            var cp = posInLine * (fixedItemCross + crossSpacing);
            var mp = lineIndex * (itemMain + mainSpacing);

            var frame = isVertical
                ? new LayoutRect(cp, mp, ItemWidth, ItemHeight)
                : new LayoutRect(mp, cp, ItemWidth, ItemHeight);

            items.Add(new LayoutItemAttributes(index, frame));
        }

        var totalLines = context.ItemCount == 0 ? 0 : ((context.ItemCount - 1) / itemsPerLine) + 1;
        var fixedContentMain = totalLines == 0 ? 0d : (totalLines * itemMain) + ((totalLines - 1) * mainSpacing);

        var cw = isVertical ? context.ViewportWidth : fixedContentMain;
        var ch = isVertical ? fixedContentMain : context.ViewportHeight;

        return new LayoutSnapshot(cw, ch, items);
    }

    public InvalidationPlan Invalidate(LayoutInvalidationContext context)
    {
        if (context.ViewportChanged || context.DataChanged)
            return new InvalidationPlan(true);
        return new InvalidationPlan(false);
    }
}
