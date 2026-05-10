using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Tests;

public class LinearLayoutProviderTests
{
    [Fact]
    public void Arrange_Vertical_ComputesFramesAndExtent()
    {
        var provider = new LinearLayoutProvider(itemExtent: 40d, spacing: 10d, orientation: LayoutOrientation.Vertical);
        var context = new LayoutContext(ItemCount: 5, ViewportWidth: 300d, ViewportHeight: 600d);

        var snapshot = provider.Arrange(context, new ItemRange(1, 4));

        Assert.Equal(300d, snapshot.ContentWidth);
        Assert.Equal(240d, snapshot.ContentHeight);
        Assert.Collection(
            snapshot.Items,
            item =>
            {
                Assert.Equal(1, item.Index);
                Assert.Equal(new LayoutRect(0d, 50d, 300d, 40d), item.Frame);
            },
            item =>
            {
                Assert.Equal(2, item.Index);
                Assert.Equal(new LayoutRect(0d, 100d, 300d, 40d), item.Frame);
            },
            item =>
            {
                Assert.Equal(3, item.Index);
                Assert.Equal(new LayoutRect(0d, 150d, 300d, 40d), item.Frame);
            });
    }

    [Fact]
    public void Arrange_Horizontal_ComputesFramesAndExtent()
    {
        var provider = new LinearLayoutProvider(itemExtent: 80d, spacing: 20d, orientation: LayoutOrientation.Horizontal);
        var context = new LayoutContext(ItemCount: 3, ViewportWidth: 320d, ViewportHeight: 200d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 2));

        Assert.Equal(280d, snapshot.ContentWidth);
        Assert.Equal(200d, snapshot.ContentHeight);
        Assert.Collection(
            snapshot.Items,
            item => Assert.Equal(new LayoutRect(0d, 0d, 80d, 200d), item.Frame),
            item => Assert.Equal(new LayoutRect(100d, 0d, 80d, 200d), item.Frame));
    }
}
