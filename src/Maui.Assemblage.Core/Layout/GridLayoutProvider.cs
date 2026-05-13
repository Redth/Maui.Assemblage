namespace Maui.Assemblage.Core.Layout;

public sealed class GridLayoutProvider : ILayoutProvider, IVisibleRangeProvider
{
    public GridLayoutProvider(
        int spanCount,
        double itemHeight,
        double horizontalSpacing = 0d,
        double verticalSpacing = 0d,
        LayoutOrientation orientation = LayoutOrientation.Vertical)
    {
        if (spanCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(spanCount));
        }

        if (itemHeight <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(itemHeight));
        }

        if (horizontalSpacing < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(horizontalSpacing));
        }

        if (verticalSpacing < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(verticalSpacing));
        }

        SpanCount = spanCount;
        ItemHeight = itemHeight;
        HorizontalSpacing = horizontalSpacing;
        VerticalSpacing = verticalSpacing;
        Orientation = orientation;
    }

    public int SpanCount { get; }

    public double ItemHeight { get; }

    public double HorizontalSpacing { get; }

    public double VerticalSpacing { get; }

    public LayoutOrientation Orientation { get; }

    public LayoutCapabilities Capabilities => LayoutCapabilities.None;

    public LayoutSnapshot Arrange(LayoutContext context, ItemRange requestRange)
    {
        if (context.ItemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(context));
        }

        var clampedRange = requestRange.Clamp(0, context.ItemCount);
        var items = new List<LayoutItemAttributes>(clampedRange.Length);

        var availableWidth = Orientation == LayoutOrientation.Vertical
            ? context.ViewportWidth
            : context.ViewportHeight;

        var totalSpacing = HorizontalSpacing * (SpanCount - 1);
        var itemWidth = (availableWidth - totalSpacing) / SpanCount;
        if (itemWidth < 0d)
        {
            itemWidth = 0d;
        }

        for (var index = clampedRange.Start; index < clampedRange.EndExclusive; index++)
        {
            var row = index / SpanCount;
            var col = index % SpanCount;

            var x = col * (itemWidth + HorizontalSpacing);
            var y = row * (ItemHeight + VerticalSpacing);

            var frame = Orientation == LayoutOrientation.Vertical
                ? new LayoutRect(x, y, itemWidth, ItemHeight)
                : new LayoutRect(y, x, ItemHeight, itemWidth);

            items.Add(new LayoutItemAttributes(index, frame));
        }

        var rowCount = context.ItemCount == 0
            ? 0
            : ((context.ItemCount - 1) / SpanCount) + 1;

        var contentPrimary = rowCount == 0
            ? 0d
            : (rowCount * ItemHeight) + ((rowCount - 1) * VerticalSpacing);

        var contentWidth = Orientation == LayoutOrientation.Vertical
            ? context.ViewportWidth
            : contentPrimary;
        var contentHeight = Orientation == LayoutOrientation.Vertical
            ? contentPrimary
            : context.ViewportHeight;

        return new LayoutSnapshot(contentWidth, contentHeight, items);
    }

    public InvalidationPlan Invalidate(LayoutInvalidationContext context)
    {
        if (!context.ViewportChanged && !context.DataChanged)
        {
            return new InvalidationPlan(false);
        }

        return new InvalidationPlan(true, context.AffectedRange);
    }

    public ItemRange GetVisibleRange(LayoutContext context)
        => GetVisibleRange(context, SpanCount, ItemHeight, VerticalSpacing, Orientation);

    internal static ItemRange GetVisibleRange(
        LayoutContext context,
        int spanCount,
        double itemHeight,
        double verticalSpacing,
        LayoutOrientation orientation)
    {
        if (context.ItemCount <= 0)
        {
            return ItemRange.Empty;
        }

        var rowPitch = itemHeight + verticalSpacing;
        var viewportSize = orientation == LayoutOrientation.Vertical
            ? context.ViewportHeight
            : context.ViewportWidth;
        var safeOffset = Math.Max(0d, context.ScrollOffset);
        var firstRow = (int)Math.Floor(safeOffset / rowPitch);
        var endEdge = safeOffset + Math.Max(0d, viewportSize);
        var lastRow = (int)Math.Floor(Math.Max(safeOffset, endEdge - double.Epsilon) / rowPitch);
        var start = Math.Clamp(firstRow * spanCount, 0, context.ItemCount);
        var end = Math.Clamp(((lastRow + 1) * spanCount), 0, context.ItemCount);
        return new ItemRange(start, end);
    }
}
