namespace Maui.Assemblage.Core.Layout;

public sealed class LinearLayoutProvider : ILayoutProvider
{
    private readonly Func<int, double>? _itemExtentResolver;

    public LinearLayoutProvider(double itemExtent, double spacing = 0d, LayoutOrientation orientation = LayoutOrientation.Vertical,
        Func<int, double>? itemExtentResolver = null)
    {
        if (itemExtent <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent));
        }

        if (spacing < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(spacing));
        }

        ItemExtent = itemExtent;
        Spacing = spacing;
        Orientation = orientation;
        _itemExtentResolver = itemExtentResolver;
    }

    public double ItemExtent { get; }

    public double Spacing { get; }

    public LayoutOrientation Orientation { get; }

    public LayoutCapabilities Capabilities => LayoutCapabilities.SupportsStickyHeaders;

    public LayoutSnapshot Arrange(LayoutContext context, ItemRange requestRange)
    {
        if (context.ItemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(context.ItemCount));
        }

        if (_itemExtentResolver is null)
        {
            return ArrangeUniform(context, requestRange);
        }

        return ArrangeVariable(context, requestRange);
    }

    private LayoutSnapshot ArrangeUniform(LayoutContext context, ItemRange requestRange)
    {
        var clampedRange = requestRange.Clamp(0, context.ItemCount);
        var items = new List<LayoutItemAttributes>(clampedRange.Length);
        var pitch = ItemExtent + Spacing;

        for (var index = clampedRange.Start; index < clampedRange.EndExclusive; index++)
        {
            var leading = index * pitch;
            var frame = Orientation == LayoutOrientation.Vertical
                ? new LayoutRect(0d, leading, context.ViewportWidth, ItemExtent)
                : new LayoutRect(leading, 0d, ItemExtent, context.ViewportHeight);
            items.Add(new LayoutItemAttributes(index, frame));
        }

        var contentPrimary = context.ItemCount == 0
            ? 0d
            : (context.ItemCount * ItemExtent) + ((context.ItemCount - 1) * Spacing);

        var contentWidth = Orientation == LayoutOrientation.Vertical
            ? context.ViewportWidth
            : contentPrimary;
        var contentHeight = Orientation == LayoutOrientation.Vertical
            ? contentPrimary
            : context.ViewportHeight;

        return new LayoutSnapshot(contentWidth, contentHeight, items);
    }

    private double[]? _prefixSums;
    private int _prefixSumCount;

    private LayoutSnapshot ArrangeVariable(LayoutContext context, ItemRange requestRange)
    {
        var clampedRange = requestRange.Clamp(0, context.ItemCount);
        var items = new List<LayoutItemAttributes>(clampedRange.Length);

        EnsurePrefixSums(context.ItemCount);

        // Use prefix sums for O(1) leading offset lookup
        var leading = clampedRange.Start > 0 ? _prefixSums![clampedRange.Start - 1] + Spacing : 0d;

        for (var index = clampedRange.Start; index < clampedRange.EndExclusive; index++)
        {
            var extent = _itemExtentResolver!(index) is var e && e > 0 ? e : ItemExtent;
            var frame = Orientation == LayoutOrientation.Vertical
                ? new LayoutRect(0d, leading, context.ViewportWidth, extent)
                : new LayoutRect(leading, 0d, extent, context.ViewportHeight);
            items.Add(new LayoutItemAttributes(index, frame));
            leading += extent + Spacing;
        }

        var contentPrimary = context.ItemCount == 0
            ? 0d
            : _prefixSums![context.ItemCount - 1];

        var contentWidth = Orientation == LayoutOrientation.Vertical
            ? context.ViewportWidth
            : contentPrimary;
        var contentHeight = Orientation == LayoutOrientation.Vertical
            ? contentPrimary
            : context.ViewportHeight;

        return new LayoutSnapshot(contentWidth, contentHeight, items);
    }

    /// <summary>Build/rebuild the prefix-sum array when item count changes.</summary>
    private void EnsurePrefixSums(int itemCount)
    {
        if (_prefixSums is not null && _prefixSumCount == itemCount) return;

        _prefixSums = new double[itemCount];
        _prefixSumCount = itemCount;
        var running = 0d;
        for (var i = 0; i < itemCount; i++)
        {
            var extent = _itemExtentResolver!(i) is var e && e > 0 ? e : ItemExtent;
            running += extent + (i > 0 ? Spacing : 0d);
            _prefixSums[i] = running;
        }
    }

    public InvalidationPlan Invalidate(LayoutInvalidationContext context)
    {
        if (!context.ViewportChanged && !context.DataChanged)
        {
            return new InvalidationPlan(false);
        }

        return new InvalidationPlan(true, context.AffectedRange);
    }
}
