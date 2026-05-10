namespace Maui.Assemblage.Core.Layout;

/// <summary>
/// Grid layout where designated items (e.g., section headers) span the full cross-axis width.
/// Non-spanning items are arranged in a multi-column grid. Useful for grouped grids
/// where group headers span all columns.
/// </summary>
public sealed class SpanningGridLayoutProvider : ILayoutProvider
{
    private readonly Func<int, bool> _isSpanningItem;

    public SpanningGridLayoutProvider(
        int spanCount,
        double itemHeight,
        double spanningItemHeight = 40d,
        double horizontalSpacing = 0d,
        double verticalSpacing = 0d,
        LayoutOrientation orientation = LayoutOrientation.Vertical,
        Func<int, bool>? isSpanningItem = null)
    {
        if (spanCount < 1)
            throw new ArgumentOutOfRangeException(nameof(spanCount));
        if (itemHeight <= 0d)
            throw new ArgumentOutOfRangeException(nameof(itemHeight));
        if (spanningItemHeight <= 0d)
            throw new ArgumentOutOfRangeException(nameof(spanningItemHeight));
        if (horizontalSpacing < 0d)
            throw new ArgumentOutOfRangeException(nameof(horizontalSpacing));
        if (verticalSpacing < 0d)
            throw new ArgumentOutOfRangeException(nameof(verticalSpacing));

        SpanCount = spanCount;
        ItemHeight = itemHeight;
        SpanningItemHeight = spanningItemHeight;
        HorizontalSpacing = horizontalSpacing;
        VerticalSpacing = verticalSpacing;
        Orientation = orientation;
        _isSpanningItem = isSpanningItem ?? (_ => false);
    }

    public int SpanCount { get; }
    public double ItemHeight { get; }
    public double SpanningItemHeight { get; }
    public double HorizontalSpacing { get; }
    public double VerticalSpacing { get; }
    public LayoutOrientation Orientation { get; }

    public LayoutCapabilities Capabilities => LayoutCapabilities.SupportsStickyHeaders;

    public LayoutSnapshot Arrange(LayoutContext context, ItemRange requestRange)
    {
        if (context.ItemCount < 0)
            throw new ArgumentOutOfRangeException(nameof(context));

        var clampedRange = requestRange.Clamp(0, context.ItemCount);
        var items = new List<LayoutItemAttributes>(clampedRange.Length);

        var isVertical = Orientation == LayoutOrientation.Vertical;
        var availableCross = isVertical ? context.ViewportWidth : context.ViewportHeight;
        var totalSpacing = HorizontalSpacing * (SpanCount - 1);
        var colWidth = (availableCross - totalSpacing) / SpanCount;
        if (colWidth < 0d) colWidth = 0d;

        // We need to compute positions for all items up to clampedRange.EndExclusive
        // by walking sequentially (spanning items break the grid flow)
        double currentMain = 0d;
        int currentCol = 0;

        // Walk items before request range to establish currentMain
        for (var i = 0; i < clampedRange.Start && i < context.ItemCount; i++)
        {
            if (_isSpanningItem(i))
            {
                if (currentCol > 0)
                {
                    currentMain += ItemHeight + VerticalSpacing;
                    currentCol = 0;
                }
                currentMain += SpanningItemHeight + VerticalSpacing;
            }
            else
            {
                currentCol++;
                if (currentCol >= SpanCount)
                {
                    currentMain += ItemHeight + VerticalSpacing;
                    currentCol = 0;
                }
            }
        }

        // Lay out items in the request range
        for (var index = clampedRange.Start; index < clampedRange.EndExclusive; index++)
        {
            if (_isSpanningItem(index))
            {
                // Finish any partial row
                if (currentCol > 0)
                {
                    currentMain += ItemHeight + VerticalSpacing;
                    currentCol = 0;
                }

                var frame = isVertical
                    ? new LayoutRect(0d, currentMain, availableCross, SpanningItemHeight)
                    : new LayoutRect(currentMain, 0d, SpanningItemHeight, availableCross);

                items.Add(new LayoutItemAttributes(index, frame));
                currentMain += SpanningItemHeight + VerticalSpacing;
            }
            else
            {
                var x = currentCol * (colWidth + HorizontalSpacing);
                var frame = isVertical
                    ? new LayoutRect(x, currentMain, colWidth, ItemHeight)
                    : new LayoutRect(currentMain, x, ItemHeight, colWidth);

                items.Add(new LayoutItemAttributes(index, frame));
                currentCol++;
                if (currentCol >= SpanCount)
                {
                    currentMain += ItemHeight + VerticalSpacing;
                    currentCol = 0;
                }
            }
        }

        // Continue to compute remaining items for total content size
        for (var i = clampedRange.EndExclusive; i < context.ItemCount; i++)
        {
            if (_isSpanningItem(i))
            {
                if (currentCol > 0)
                {
                    currentMain += ItemHeight + VerticalSpacing;
                    currentCol = 0;
                }
                currentMain += SpanningItemHeight + VerticalSpacing;
            }
            else
            {
                currentCol++;
                if (currentCol >= SpanCount)
                {
                    currentMain += ItemHeight + VerticalSpacing;
                    currentCol = 0;
                }
            }
        }

        // Add the last partial row
        if (currentCol > 0)
        {
            currentMain += ItemHeight;
        }
        else if (currentMain > 0)
        {
            // Remove trailing spacing
            currentMain -= VerticalSpacing;
        }

        var contentWidth = isVertical ? context.ViewportWidth : currentMain;
        var contentHeight = isVertical ? currentMain : context.ViewportHeight;

        return new LayoutSnapshot(contentWidth, contentHeight, items);
    }

    public InvalidationPlan Invalidate(LayoutInvalidationContext context)
    {
        if (!context.ViewportChanged && !context.DataChanged)
            return new InvalidationPlan(false);
        return new InvalidationPlan(true, context.AffectedRange);
    }
}
