using Maui.Assemblage.Core.Layout;
using Maui.Assemblage.Core.Realization;

namespace Maui.Assemblage.Core.Tests;

public class RealizationStrategyTests
{
    [Fact]
    public void Realize_ReturnsOnlyVisibleEntries()
    {
        var provider = new LinearLayoutProvider(itemExtent: 50d);
        var context = new LayoutContext(ItemCount: 20, ViewportWidth: 300d, ViewportHeight: 200d);
        var snapshot = provider.Arrange(context, new ItemRange(0, 20));

        var strategy = new WindowedRealizationStrategy();
        var visible = new ItemRange(2, 6);
        var entries = strategy.Realize(snapshot, visible, _ => "default");

        Assert.Equal(4, entries.Count);
        Assert.Equal(2, entries[0].FlatIndex);
        Assert.Equal(5, entries[3].FlatIndex);
        Assert.All(entries, e => Assert.Equal("default", e.TemplateKey));
    }

    [Fact]
    public void GetRecyclableIndices_ReturnsOutOfRangeIndices()
    {
        var strategy = new WindowedRealizationStrategy();
        var realized = new HashSet<int> { 0, 1, 2, 3, 4, 5 };
        var keepRange = new ItemRange(2, 5);

        var recyclable = strategy.GetRecyclableIndices(realized, keepRange);

        Assert.Equal(3, recyclable.Count);
        Assert.Contains(0, recyclable);
        Assert.Contains(1, recyclable);
        Assert.Contains(5, recyclable);
    }

    [Fact]
    public void Realize_UsesTemplateKeySelector()
    {
        var provider = new LinearLayoutProvider(itemExtent: 30d);
        var context = new LayoutContext(ItemCount: 4, ViewportWidth: 100d, ViewportHeight: 200d);
        var snapshot = provider.Arrange(context, new ItemRange(0, 4));

        var strategy = new WindowedRealizationStrategy();
        var entries = strategy.Realize(
            snapshot,
            new ItemRange(0, 4),
            index => index % 2 == 0 ? "even" : "odd");

        Assert.Equal("even", entries[0].TemplateKey);
        Assert.Equal("odd", entries[1].TemplateKey);
        Assert.Equal("even", entries[2].TemplateKey);
        Assert.Equal("odd", entries[3].TemplateKey);
    }
}
