using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Tests;

public class GridLayoutProviderTests
{
    [Fact]
    public void Arrange_3Columns_ComputesCorrectFrames()
    {
        var provider = new GridLayoutProvider(
            spanCount: 3,
            itemHeight: 60d,
            horizontalSpacing: 10d,
            verticalSpacing: 8d);

        var context = new LayoutContext(ItemCount: 7, ViewportWidth: 320d, ViewportHeight: 600d);
        var snapshot = provider.Arrange(context, new ItemRange(0, 7));

        // itemWidth = (320 - 20) / 3 = 100
        Assert.Equal(320d, snapshot.ContentWidth);

        // 3 rows: row0(0-2), row1(3-5), row2(6)
        // contentHeight = 3*60 + 2*8 = 196
        Assert.Equal(196d, snapshot.ContentHeight);

        Assert.Equal(7, snapshot.Items.Count);

        // Row 0
        Assert.Equal(new LayoutRect(0d, 0d, 100d, 60d), snapshot.Items[0].Frame);
        Assert.Equal(new LayoutRect(110d, 0d, 100d, 60d), snapshot.Items[1].Frame);
        Assert.Equal(new LayoutRect(220d, 0d, 100d, 60d), snapshot.Items[2].Frame);

        // Row 1
        Assert.Equal(new LayoutRect(0d, 68d, 100d, 60d), snapshot.Items[3].Frame);
        Assert.Equal(new LayoutRect(110d, 68d, 100d, 60d), snapshot.Items[4].Frame);
        Assert.Equal(new LayoutRect(220d, 68d, 100d, 60d), snapshot.Items[5].Frame);

        // Row 2
        Assert.Equal(new LayoutRect(0d, 136d, 100d, 60d), snapshot.Items[6].Frame);
    }

    [Fact]
    public void Arrange_EmptyItems_ReturnsZeroExtent()
    {
        var provider = new GridLayoutProvider(spanCount: 2, itemHeight: 40d);
        var context = new LayoutContext(ItemCount: 0, ViewportWidth: 200d, ViewportHeight: 400d);

        var snapshot = provider.Arrange(context, ItemRange.Empty);

        Assert.Equal(0d, snapshot.ContentHeight);
        Assert.Empty(snapshot.Items);
    }

    [Fact]
    public void Arrange_SubRange_OnlyIncludesRequestedItems()
    {
        var provider = new GridLayoutProvider(spanCount: 2, itemHeight: 50d, horizontalSpacing: 5d);
        var context = new LayoutContext(ItemCount: 10, ViewportWidth: 205d, ViewportHeight: 400d);

        var snapshot = provider.Arrange(context, new ItemRange(2, 5));

        Assert.Equal(3, snapshot.Items.Count);
        Assert.Equal(2, snapshot.Items[0].Index);
        Assert.Equal(4, snapshot.Items[2].Index);
    }
}
