using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Tests;

public class WrapLayoutProviderTests
{
    [Fact]
    public void Arrange_ItemsFitInOneLine()
    {
        var provider = new WrapLayoutProvider(50, 50, horizontalSpacing: 10, verticalSpacing: 10);
        var context = new LayoutContext(3, 200, 600);
        var result = provider.Arrange(context, new ItemRange(0, 3));

        Assert.Equal(3, result.Items.Count);
        // 3 items of width 50 + 2*10 spacing = 170, fits in 200
        Assert.Equal(0d, result.Items[0].Frame.X);
        Assert.Equal(60d, result.Items[1].Frame.X); // 50 + 10
        Assert.Equal(120d, result.Items[2].Frame.X); // 100 + 20
        // All on same row
        Assert.Equal(0d, result.Items[0].Frame.Y);
        Assert.Equal(0d, result.Items[1].Frame.Y);
        Assert.Equal(0d, result.Items[2].Frame.Y);
    }

    [Fact]
    public void Arrange_ItemsWrapToNextLine()
    {
        // 200px wide, items 80px each + 10 spacing = 90 per. Fits 2 per line: (80+10)*2-10=170 ≤ 200
        var provider = new WrapLayoutProvider(80, 40, horizontalSpacing: 10, verticalSpacing: 10);
        var context = new LayoutContext(5, 200, 600);
        var result = provider.Arrange(context, new ItemRange(0, 5));

        Assert.Equal(5, result.Items.Count);
        // Line 0: items 0,1
        Assert.Equal(0d, result.Items[0].Frame.Y);
        Assert.Equal(0d, result.Items[1].Frame.Y);
        // Line 1: items 2,3
        Assert.Equal(50d, result.Items[2].Frame.Y); // 40 + 10
        // Line 2: item 4
        Assert.Equal(100d, result.Items[4].Frame.Y); // 80 + 20
    }

    [Fact]
    public void Arrange_ContentHeight_CorrectForMultipleLines()
    {
        var provider = new WrapLayoutProvider(80, 40, horizontalSpacing: 10, verticalSpacing: 10);
        var context = new LayoutContext(5, 200, 600);
        var result = provider.Arrange(context, new ItemRange(0, 5));

        // 3 lines of 40px + 2*10 spacing = 140
        Assert.Equal(140d, result.ContentHeight);
    }

    [Fact]
    public void Arrange_Empty_ReturnsZero()
    {
        var provider = new WrapLayoutProvider(50, 50);
        var context = new LayoutContext(0, 200, 600);
        var result = provider.Arrange(context, new ItemRange(0, 0));

        Assert.Empty(result.Items);
        Assert.Equal(0d, result.ContentHeight);
    }

    [Fact]
    public void Arrange_Horizontal_FlowsTopToBottom()
    {
        var provider = new WrapLayoutProvider(60, 50, horizontalSpacing: 10, verticalSpacing: 10,
            orientation: LayoutOrientation.Horizontal);
        var context = new LayoutContext(4, 600, 120);
        var result = provider.Arrange(context, new ItemRange(0, 4));

        Assert.Equal(4, result.Items.Count);
        // Horizontal: items flow in columns (cross = height=120, item cross = height=50)
        // Items per line (column): (120+10)/(50+10) = 2
        // Item 0: col0, row0
        // Item 1: col0, row1
        // Item 2: col1, row0
        // Item 3: col1, row1
        Assert.Equal(0d, result.Items[0].Frame.X); // main pos
        Assert.Equal(0d, result.Items[0].Frame.Y); // cross pos
        Assert.Equal(0d, result.Items[1].Frame.X);
        Assert.Equal(60d, result.Items[1].Frame.Y); // 50+10
        Assert.Equal(70d, result.Items[2].Frame.X); // 60+10
    }

    [Fact]
    public void Arrange_SingleItemPerLine_StillWorks()
    {
        // Viewport 50px, item 80px. Still fits 1 per line (Math.Max(1,...))
        var provider = new WrapLayoutProvider(80, 40);
        var context = new LayoutContext(3, 50, 600);
        var result = provider.Arrange(context, new ItemRange(0, 3));

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(0d, result.Items[0].Frame.Y);
        Assert.Equal(40d, result.Items[1].Frame.Y);
        Assert.Equal(80d, result.Items[2].Frame.Y);
    }

    [Fact]
    public void Constructor_Validates_ItemWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WrapLayoutProvider(0, 50));
    }

    [Fact]
    public void Arrange_VariableWidth_WrapsCorrectly()
    {
        // Viewport 200px wide. Items: 80, 60, 100, 50, 120
        // Line 0: 80 + 8 + 60 = 148 ≤ 200, then +8+100=256 > 200 → wrap
        // Line 1: 100 + 8 + 50 = 158 ≤ 200, then +8+120=286 > 200 → wrap
        // Line 2: 120
        var widths = new double[] { 80, 60, 100, 50, 120 };
        var provider = new WrapLayoutProvider(80, 40, horizontalSpacing: 8, verticalSpacing: 8,
            itemWidthResolver: i => widths[i]);
        var context = new LayoutContext(5, 200, 600);
        var result = provider.Arrange(context, new ItemRange(0, 5));

        Assert.Equal(5, result.Items.Count);
        // Line 0: items 0 (x=0, w=80), 1 (x=88, w=60)
        Assert.Equal(0d, result.Items[0].Frame.X);
        Assert.Equal(0d, result.Items[0].Frame.Y);
        Assert.Equal(80d, result.Items[0].Frame.Width);
        Assert.Equal(88d, result.Items[1].Frame.X);
        Assert.Equal(0d, result.Items[1].Frame.Y);
        Assert.Equal(60d, result.Items[1].Frame.Width);
        // Line 1: items 2 (x=0), 3 (x=108)
        Assert.Equal(0d, result.Items[2].Frame.X);
        Assert.Equal(48d, result.Items[2].Frame.Y); // 40+8
        Assert.Equal(100d, result.Items[2].Frame.Width);
        Assert.Equal(108d, result.Items[3].Frame.X); // 100+8
        Assert.Equal(48d, result.Items[3].Frame.Y);
        // Line 2: item 4 (x=0)
        Assert.Equal(0d, result.Items[4].Frame.X);
        Assert.Equal(96d, result.Items[4].Frame.Y); // 48+40+8
        Assert.Equal(120d, result.Items[4].Frame.Width);
    }

    [Fact]
    public void Arrange_VariableWidth_ContentHeight_Correct()
    {
        var widths = new double[] { 80, 60, 100, 50, 120 };
        var provider = new WrapLayoutProvider(80, 40, horizontalSpacing: 8, verticalSpacing: 8,
            itemWidthResolver: i => widths[i]);
        var context = new LayoutContext(5, 200, 600);
        var result = provider.Arrange(context, new ItemRange(0, 5));

        // 3 lines × 40 + 2 × 8 = 136
        Assert.Equal(136d, result.ContentHeight);
    }

    [Fact]
    public void Arrange_VariableWidth_SingleLargeItem_WrapsAlone()
    {
        // Item wider than viewport still gets its own line
        var widths = new double[] { 50, 250, 50 };
        var provider = new WrapLayoutProvider(80, 30, horizontalSpacing: 5, verticalSpacing: 5,
            itemWidthResolver: i => widths[i]);
        var context = new LayoutContext(3, 200, 600);
        var result = provider.Arrange(context, new ItemRange(0, 3));

        Assert.Equal(3, result.Items.Count);
        // Item 0: line 0, x=0
        Assert.Equal(0d, result.Items[0].Frame.Y);
        // Item 1: line 1 (50+5+250 > 200)
        Assert.Equal(0d, result.Items[1].Frame.X);
        Assert.Equal(35d, result.Items[1].Frame.Y); // 30+5
        Assert.Equal(250d, result.Items[1].Frame.Width);
        // Item 2: line 2 (250+5+50 > 200)
        Assert.Equal(0d, result.Items[2].Frame.X);
        Assert.Equal(70d, result.Items[2].Frame.Y); // 35+30+5
    }
}
