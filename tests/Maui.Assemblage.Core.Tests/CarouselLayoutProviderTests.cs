using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Tests;

public class CarouselLayoutProviderTests
{
    [Fact]
    public void Arrange_Horizontal_ComputesPagingFrames()
    {
        var provider = new CarouselLayoutProvider(peekAmount: 20d, itemSpacing: 10d);
        var context = new LayoutContext(ItemCount: 3, ViewportWidth: 320d, ViewportHeight: 200d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 3));

        // itemExtent = 320 - 2*20 = 280
        Assert.Equal(3, snapshot.Items.Count);

        // Item 0: x = 20 + 0*290 = 20
        Assert.Equal(20d, snapshot.Items[0].Frame.X);
        Assert.Equal(280d, snapshot.Items[0].Frame.Width);
        Assert.Equal(200d, snapshot.Items[0].Frame.Height);

        // Item 1: x = 20 + 1*290 = 310
        Assert.Equal(310d, snapshot.Items[1].Frame.X);

        // Item 2: x = 20 + 2*290 = 600
        Assert.Equal(600d, snapshot.Items[2].Frame.X);

        // contentWidth = 2*20 + 3*280 + 2*10 = 900
        Assert.Equal(900d, snapshot.ContentWidth);
        Assert.Equal(200d, snapshot.ContentHeight);
    }

    [Fact]
    public void GetSnapOffset_ReturnsCorrectOffset()
    {
        var provider = new CarouselLayoutProvider(peekAmount: 20d, itemSpacing: 10d);

        Assert.Equal(0d, provider.GetSnapOffset(0, 320d));
        Assert.Equal(290d, provider.GetSnapOffset(1, 320d));
        Assert.Equal(580d, provider.GetSnapOffset(2, 320d));
    }

    [Fact]
    public void GetSnapTargetIndex_ClampsToRange()
    {
        var provider = new CarouselLayoutProvider(peekAmount: 0d, itemSpacing: 0d);

        // Viewport = 300, so itemExtent = 300, pitch = 300
        Assert.Equal(0, provider.GetSnapTargetIndex(-100d, 0d, 5, 300d));
        Assert.Equal(4, provider.GetSnapTargetIndex(9999d, 0d, 5, 300d));
        Assert.Equal(1, provider.GetSnapTargetIndex(300d, 0d, 5, 300d));
    }

    [Fact]
    public void GetSnapTargetIndex_VelocityInfluencesTarget()
    {
        var provider = new CarouselLayoutProvider(peekAmount: 0d, itemSpacing: 0d);

        // offset=150 (midway between 0 and 1), forward velocity should snap to 1
        var target = provider.GetSnapTargetIndex(150d, 500d, 5, 300d);
        Assert.Equal(1, target);

        // same offset, backward velocity should snap to 0
        var targetBack = provider.GetSnapTargetIndex(150d, -500d, 5, 300d);
        Assert.Equal(0, targetBack);
    }

    [Fact]
    public void Capabilities_HasSnapping()
    {
        var provider = new CarouselLayoutProvider();
        Assert.True(provider.Capabilities.HasFlag(LayoutCapabilities.SupportsSnapping));
    }
}
