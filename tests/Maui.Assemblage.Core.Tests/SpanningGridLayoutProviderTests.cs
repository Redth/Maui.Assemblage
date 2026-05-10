using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Tests;

public class SpanningGridLayoutProviderTests
{
    [Fact]
    public void Arrange_NoSpanningItems_BehavesLikeGrid()
    {
        var provider = new SpanningGridLayoutProvider(3, itemHeight: 50, spanningItemHeight: 30,
            horizontalSpacing: 10, verticalSpacing: 10);
        var context = new LayoutContext(6, 310, 600);
        var result = provider.Arrange(context, new ItemRange(0, 6));

        Assert.Equal(6, result.Items.Count);
        // Row 0: items 0,1,2
        Assert.Equal(0d, result.Items[0].Frame.Y);
        Assert.Equal(0d, result.Items[1].Frame.Y);
        Assert.Equal(0d, result.Items[2].Frame.Y);
        // Row 1: items 3,4,5
        Assert.Equal(60d, result.Items[3].Frame.Y); // 50 + 10
    }

    [Fact]
    public void Arrange_SpanningItemSpansFullWidth()
    {
        // Items: [spanning, item, item, item, spanning, item]
        var spanningIndices = new HashSet<int> { 0, 4 };
        var provider = new SpanningGridLayoutProvider(3, itemHeight: 50, spanningItemHeight: 30,
            horizontalSpacing: 10, verticalSpacing: 10,
            isSpanningItem: i => spanningIndices.Contains(i));
        var context = new LayoutContext(6, 310, 600);
        var result = provider.Arrange(context, new ItemRange(0, 6));

        Assert.Equal(6, result.Items.Count);

        // Item 0: spanning, full width
        Assert.Equal(0d, result.Items[0].Frame.X);
        Assert.Equal(310d, result.Items[0].Frame.Width);
        Assert.Equal(30d, result.Items[0].Frame.Height);

        // Items 1,2,3: grid row after spanning header
        Assert.Equal(40d, result.Items[1].Frame.Y); // 30 + 10
        Assert.Equal(40d, result.Items[2].Frame.Y);
        Assert.Equal(40d, result.Items[3].Frame.Y);

        // Item 4: spanning again
        Assert.Equal(0d, result.Items[4].Frame.X);
        Assert.Equal(310d, result.Items[4].Frame.Width);
        Assert.Equal(100d, result.Items[4].Frame.Y); // 30+10+50+10

        // Item 5: grid item after second spanning (100+30+10=140)
        Assert.Equal(140d, result.Items[5].Frame.Y);
    }

    [Fact]
    public void Arrange_SpanningBreaksPartialRow()
    {
        // Items: [item, item, spanning, item]
        // With 3 columns: items 0,1 are in partial row, then spanning forces new row
        var provider = new SpanningGridLayoutProvider(3, itemHeight: 50, spanningItemHeight: 30,
            verticalSpacing: 10,
            isSpanningItem: i => i == 2);
        var context = new LayoutContext(4, 300, 600);
        var result = provider.Arrange(context, new ItemRange(0, 4));

        Assert.Equal(4, result.Items.Count);
        // Items 0,1 at y=0
        Assert.Equal(0d, result.Items[0].Frame.Y);
        Assert.Equal(0d, result.Items[1].Frame.Y);
        // Spanning item 2: partial row closes (50+10=60), then spanning at y=60
        Assert.Equal(60d, result.Items[2].Frame.Y);
        Assert.Equal(300d, result.Items[2].Frame.Width);
        // Item 3 at y=100 (60+30+10)
        Assert.Equal(100d, result.Items[3].Frame.Y);
    }

    [Fact]
    public void Arrange_Empty_ReturnsZero()
    {
        var provider = new SpanningGridLayoutProvider(2, itemHeight: 50);
        var context = new LayoutContext(0, 200, 600);
        var result = provider.Arrange(context, new ItemRange(0, 0));

        Assert.Empty(result.Items);
    }

    [Fact]
    public void Arrange_AllSpanning_StacksVertically()
    {
        var provider = new SpanningGridLayoutProvider(3, itemHeight: 50, spanningItemHeight: 30,
            verticalSpacing: 10, isSpanningItem: _ => true);
        var context = new LayoutContext(3, 300, 600);
        var result = provider.Arrange(context, new ItemRange(0, 3));

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(0d, result.Items[0].Frame.Y);
        Assert.Equal(40d, result.Items[1].Frame.Y); // 30+10
        Assert.Equal(80d, result.Items[2].Frame.Y); // 60+20
    }

    [Fact]
    public void ContentSize_IncludesAllItems()
    {
        var provider = new SpanningGridLayoutProvider(2, itemHeight: 50, spanningItemHeight: 30,
            verticalSpacing: 10, isSpanningItem: i => i == 0);
        var context = new LayoutContext(5, 200, 600);
        var result = provider.Arrange(context, new ItemRange(0, 5));

        // Spanning(30) + spacing(10) + row0(50) + spacing(10) + row1(50) = 150
        Assert.Equal(150d, result.ContentHeight);
    }

    [Fact]
    public void Constructor_Validates_SpanCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SpanningGridLayoutProvider(0, itemHeight: 50));
    }
}
