namespace Maui.Assemblage.Core.Layout;

/// <summary>
/// An adaptive grid layout provider that calculates the span count automatically
/// based on a minimum item width and the available viewport width.
/// </summary>
public sealed class AdaptiveGridLayoutProvider : ILayoutProvider
{
    public AdaptiveGridLayoutProvider(
        double minItemWidth,
        double itemHeight,
        double horizontalSpacing = 0d,
        double verticalSpacing = 0d,
        LayoutOrientation orientation = LayoutOrientation.Vertical)
    {
        if (minItemWidth <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(minItemWidth));
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

        MinItemWidth = minItemWidth;
        ItemHeight = itemHeight;
        HorizontalSpacing = horizontalSpacing;
        VerticalSpacing = verticalSpacing;
        Orientation = orientation;
    }

    public double MinItemWidth { get; }
    public double ItemHeight { get; }
    public double HorizontalSpacing { get; }
    public double VerticalSpacing { get; }
    public LayoutOrientation Orientation { get; }

    public LayoutCapabilities Capabilities => LayoutCapabilities.None;

    /// <summary>
    /// Calculates the optimal span count for the given viewport width.
    /// </summary>
    public static int CalculateSpanCount(double viewportWidth, double minItemWidth, double horizontalSpacing)
    {
        if (viewportWidth <= 0d || minItemWidth <= 0d)
        {
            return 1;
        }

        // Solve: spanCount * minItemWidth + (spanCount - 1) * spacing <= viewportWidth
        // spanCount * (minItemWidth + spacing) - spacing <= viewportWidth
        // spanCount <= (viewportWidth + spacing) / (minItemWidth + spacing)
        var span = (int)Math.Floor((viewportWidth + horizontalSpacing) / (minItemWidth + horizontalSpacing));
        return Math.Max(1, span);
    }

    public LayoutSnapshot Arrange(LayoutContext context, ItemRange requestRange)
    {
        if (context.ItemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(context));
        }

        var availableWidth = Orientation == LayoutOrientation.Vertical
            ? context.ViewportWidth
            : context.ViewportHeight;

        var spanCount = CalculateSpanCount(availableWidth, MinItemWidth, HorizontalSpacing);

        // Delegate to GridLayoutProvider with the computed span count
        var inner = new GridLayoutProvider(spanCount, ItemHeight, HorizontalSpacing, VerticalSpacing, Orientation);
        return inner.Arrange(context, requestRange);
    }

    public InvalidationPlan Invalidate(LayoutInvalidationContext context)
    {
        // Viewport changes always require full re-layout since span count may change
        if (context.ViewportChanged || context.DataChanged)
        {
            return new InvalidationPlan(true, context.AffectedRange);
        }

        return new InvalidationPlan(false);
    }
}
