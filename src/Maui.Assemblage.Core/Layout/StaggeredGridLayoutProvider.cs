namespace Maui.Assemblage.Core.Layout;

/// <summary>
/// Pinterest-style masonry layout with columns of varying-height items.
/// Items are placed in the shortest column to produce a staggered effect.
/// </summary>
public sealed class StaggeredGridLayoutProvider : ILayoutProvider
{
    private readonly Func<int, double>? _itemHeightResolver;

    public StaggeredGridLayoutProvider(
        int spanCount,
        double defaultItemHeight = 100d,
        double horizontalSpacing = 0d,
        double verticalSpacing = 0d,
        LayoutOrientation orientation = LayoutOrientation.Vertical,
        Func<int, double>? itemHeightResolver = null)
    {
        if (spanCount < 1)
            throw new ArgumentOutOfRangeException(nameof(spanCount));
        if (defaultItemHeight <= 0d)
            throw new ArgumentOutOfRangeException(nameof(defaultItemHeight));
        if (horizontalSpacing < 0d)
            throw new ArgumentOutOfRangeException(nameof(horizontalSpacing));
        if (verticalSpacing < 0d)
            throw new ArgumentOutOfRangeException(nameof(verticalSpacing));

        SpanCount = spanCount;
        DefaultItemHeight = defaultItemHeight;
        HorizontalSpacing = horizontalSpacing;
        VerticalSpacing = verticalSpacing;
        Orientation = orientation;
        _itemHeightResolver = itemHeightResolver;
    }

    public int SpanCount { get; }
    public double DefaultItemHeight { get; }
    public double HorizontalSpacing { get; }
    public double VerticalSpacing { get; }
    public LayoutOrientation Orientation { get; }

    public LayoutCapabilities Capabilities => LayoutCapabilities.None;

    public LayoutSnapshot Arrange(LayoutContext context, ItemRange requestRange)
    {
        if (context.ItemCount < 0)
            throw new ArgumentOutOfRangeException(nameof(context));

        var clampedRange = requestRange.Clamp(0, context.ItemCount);
        var items = new List<LayoutItemAttributes>(clampedRange.Length);

        var isVertical = Orientation == LayoutOrientation.Vertical;
        var availableCross = isVertical ? context.ViewportWidth : context.ViewportHeight;
        var totalSpacing = HorizontalSpacing * (SpanCount - 1);
        var columnWidth = (availableCross - totalSpacing) / SpanCount;
        if (columnWidth < 0d) columnWidth = 0d;

        // Track the bottom edge of each column
        var columnTops = new double[SpanCount];

        // First pass: lay out items before the request range to build column heights
        for (var i = 0; i < clampedRange.Start && i < context.ItemCount; i++)
        {
            var col = GetShortestColumn(columnTops);
            var itemHeight = _itemHeightResolver?.Invoke(i) ?? DefaultItemHeight;
            columnTops[col] += itemHeight + VerticalSpacing;
        }

        // Second pass: lay out items in the request range
        for (var index = clampedRange.Start; index < clampedRange.EndExclusive; index++)
        {
            var col = GetShortestColumn(columnTops);
            var itemHeight = _itemHeightResolver?.Invoke(index) ?? DefaultItemHeight;

            var x = col * (columnWidth + HorizontalSpacing);
            var y = columnTops[col];

            var frame = isVertical
                ? new LayoutRect(x, y, columnWidth, itemHeight)
                : new LayoutRect(y, x, itemHeight, columnWidth);

            items.Add(new LayoutItemAttributes(index, frame));
            columnTops[col] = y + itemHeight + VerticalSpacing;
        }

        // Continue to compute remaining column heights for total content size
        for (var i = clampedRange.EndExclusive; i < context.ItemCount; i++)
        {
            var col = GetShortestColumn(columnTops);
            var itemHeight = _itemHeightResolver?.Invoke(i) ?? DefaultItemHeight;
            columnTops[col] += itemHeight + VerticalSpacing;
        }

        var maxColumn = 0d;
        for (var c = 0; c < SpanCount; c++)
        {
            var top = columnTops[c] > 0 ? columnTops[c] - VerticalSpacing : 0;
            if (top > maxColumn) maxColumn = top;
        }

        var contentWidth = isVertical ? context.ViewportWidth : maxColumn;
        var contentHeight = isVertical ? maxColumn : context.ViewportHeight;

        return new LayoutSnapshot(contentWidth, contentHeight, items);
    }

    public InvalidationPlan Invalidate(LayoutInvalidationContext context)
    {
        if (!context.ViewportChanged && !context.DataChanged)
            return new InvalidationPlan(false);
        return new InvalidationPlan(true, context.AffectedRange);
    }

    private static int GetShortestColumn(double[] columnTops)
    {
        var minIdx = 0;
        for (var i = 1; i < columnTops.Length; i++)
        {
            if (columnTops[i] < columnTops[minIdx])
                minIdx = i;
        }
        return minIdx;
    }
}
