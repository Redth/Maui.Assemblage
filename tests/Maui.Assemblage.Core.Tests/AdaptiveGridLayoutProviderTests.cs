using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Tests;

public class AdaptiveGridLayoutProviderTests
{
    [Theory]
    [InlineData(400d, 100d, 0d, 4)]   // 400 / 100 = 4
    [InlineData(400d, 100d, 10d, 3)]  // (400+10)/(100+10) = 3.72 → 3
    [InlineData(400d, 150d, 0d, 2)]   // 400 / 150 = 2.66 → 2
    [InlineData(100d, 200d, 0d, 1)]   // viewport smaller than minWidth → 1
    [InlineData(0d, 100d, 0d, 1)]     // zero viewport → 1
    public void CalculateSpanCount_Tests(double viewportWidth, double minItemWidth, double spacing, int expected)
    {
        var result = AdaptiveGridLayoutProvider.CalculateSpanCount(viewportWidth, minItemWidth, spacing);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Arrange_AdaptsToViewport()
    {
        var provider = new AdaptiveGridLayoutProvider(minItemWidth: 100d, itemHeight: 80d);

        // Viewport width = 400 → should get 4 columns
        var context = new LayoutContext(8, 400d, 600d);
        var snapshot = provider.Arrange(context, new ItemRange(0, 8));

        Assert.Equal(8, snapshot.Items.Count);

        // 4 columns × 2 rows
        // Items 0-3 should be on row 0, items 4-7 on row 1
        Assert.Equal(0d, snapshot.Items[0].Frame.Y);
        Assert.Equal(0d, snapshot.Items[3].Frame.Y);
        Assert.Equal(80d, snapshot.Items[4].Frame.Y);
    }

    [Fact]
    public void Arrange_NarrowViewport_SingleColumn()
    {
        var provider = new AdaptiveGridLayoutProvider(minItemWidth: 150d, itemHeight: 60d);

        // Viewport width = 100 → can't fit even 1 at 150, so falls back to 1 column
        var context = new LayoutContext(3, 100d, 600d);
        var snapshot = provider.Arrange(context, new ItemRange(0, 3));

        Assert.Equal(3, snapshot.Items.Count);
        // All items in a single column
        Assert.Equal(0d, snapshot.Items[0].Frame.Y);
        Assert.Equal(60d, snapshot.Items[1].Frame.Y);
        Assert.Equal(120d, snapshot.Items[2].Frame.Y);
    }

    [Fact]
    public void Invalidate_ViewportChanged_RequiresFullArrange()
    {
        var provider = new AdaptiveGridLayoutProvider(minItemWidth: 100d, itemHeight: 80d);

        var plan = provider.Invalidate(new LayoutInvalidationContext(ViewportChanged: true, DataChanged: false));

        Assert.True(plan.RequiresFullArrange);
    }

    [Fact]
    public void Constructor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AdaptiveGridLayoutProvider(minItemWidth: 0d, itemHeight: 80d));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AdaptiveGridLayoutProvider(minItemWidth: 100d, itemHeight: -1d));
    }
}
