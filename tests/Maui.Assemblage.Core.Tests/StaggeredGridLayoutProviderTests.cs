using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Tests;

public class StaggeredGridLayoutProviderTests
{
    [Fact]
    public void Arrange_UniformHeights_ProducesGridLikeLayout()
    {
        var provider = new StaggeredGridLayoutProvider(2, defaultItemHeight: 50, horizontalSpacing: 10, verticalSpacing: 10);
        var context = new LayoutContext(4, 310, 600);
        var result = provider.Arrange(context, new ItemRange(0, 4));

        Assert.Equal(4, result.Items.Count);
        // With uniform heights, items should fill columns evenly
        // Col 0: items 0, 2; Col 1: items 1, 3
        Assert.Equal(0d, result.Items[0].Frame.X);
        Assert.Equal(160d, result.Items[1].Frame.X); // colWidth(150) + spacing(10)
    }

    [Fact]
    public void Arrange_VaryingHeights_PlacesInShortestColumn()
    {
        // Item 0 = 100, Item 1 = 50, Item 2 should go in col 1 (shorter)
        var heights = new[] { 100d, 50d, 80d, 60d };
        var provider = new StaggeredGridLayoutProvider(2, defaultItemHeight: 50,
            verticalSpacing: 10, itemHeightResolver: i => heights[i]);
        var context = new LayoutContext(4, 200, 600);
        var result = provider.Arrange(context, new ItemRange(0, 4));

        Assert.Equal(4, result.Items.Count);
        // Item 0 → col 0 (y=0, h=100)
        Assert.Equal(0d, result.Items[0].Frame.Y);
        Assert.Equal(100d, result.Items[0].Frame.Height);
        // Item 1 → col 1 (y=0, h=50)
        Assert.Equal(0d, result.Items[1].Frame.Y);
        Assert.Equal(50d, result.Items[1].Frame.Height);
        // Item 2 → col 1 (shorter: 50+10=60 vs 100+10=110)
        Assert.Equal(60d, result.Items[2].Frame.Y); // 50 + 10 spacing
    }

    [Fact]
    public void Arrange_ThreeColumns_DistributesCorrectly()
    {
        var provider = new StaggeredGridLayoutProvider(3, defaultItemHeight: 40, horizontalSpacing: 5, verticalSpacing: 5);
        var context = new LayoutContext(6, 310, 600);
        var result = provider.Arrange(context, new ItemRange(0, 6));

        Assert.Equal(6, result.Items.Count);
        // First 3 items should be in row 0
        Assert.Equal(0d, result.Items[0].Frame.Y);
        Assert.Equal(0d, result.Items[1].Frame.Y);
        Assert.Equal(0d, result.Items[2].Frame.Y);
        // Next 3 should be in row 1
        Assert.Equal(45d, result.Items[3].Frame.Y); // 40 + 5 spacing
    }

    [Fact]
    public void Arrange_Empty_ReturnsZeroContent()
    {
        var provider = new StaggeredGridLayoutProvider(2, defaultItemHeight: 50);
        var context = new LayoutContext(0, 200, 600);
        var result = provider.Arrange(context, new ItemRange(0, 0));

        Assert.Empty(result.Items);
        Assert.Equal(0d, result.ContentHeight);
    }

    [Fact]
    public void Arrange_Horizontal_SwapsAxes()
    {
        var provider = new StaggeredGridLayoutProvider(2, defaultItemHeight: 50,
            orientation: LayoutOrientation.Horizontal);
        var context = new LayoutContext(2, 600, 200);
        var result = provider.Arrange(context, new ItemRange(0, 2));

        Assert.Equal(2, result.Items.Count);
        // In horizontal mode, items flow along X axis
        Assert.Equal(0d, result.Items[0].Frame.X);
        Assert.Equal(0d, result.Items[1].Frame.X);
    }

    [Fact]
    public void ContentSize_ReflectsAllItems()
    {
        var heights = new[] { 100d, 200d, 150d, 50d };
        var provider = new StaggeredGridLayoutProvider(2, defaultItemHeight: 100, verticalSpacing: 10,
            itemHeightResolver: i => heights[i]);
        var context = new LayoutContext(4, 200, 600);
        var result = provider.Arrange(context, new ItemRange(0, 4));

        // Col 0: 100 + 10 + 150 = 260; Col 1: 200 + 10 + 50 = 260
        Assert.Equal(260d, result.ContentHeight);
    }

    [Fact]
    public void Constructor_Validates_SpanCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StaggeredGridLayoutProvider(0, defaultItemHeight: 50));
    }
}
