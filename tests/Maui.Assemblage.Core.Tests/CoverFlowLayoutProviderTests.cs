using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Tests;

public class CoverFlowLayoutProviderTests
{
    [Fact]
    public void Arrange_Horizontal_CentersFirstItem()
    {
        var provider = new CoverFlowLayoutProvider(itemWidth: 200d, itemSpacing: 10d);
        var context = new LayoutContext(ItemCount: 5, ViewportWidth: 400d, ViewportHeight: 300d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 5));

        // centerOffset = (400 - 200) / 2 = 100
        Assert.Equal(5, snapshot.Items.Count);
        Assert.Equal(100d, snapshot.Items[0].Frame.X);
        Assert.Equal(200d, snapshot.Items[0].Frame.Width);
        Assert.Equal(300d, snapshot.Items[0].Frame.Height);

        // Item 1: x = 100 + 1*210 = 310
        Assert.Equal(310d, snapshot.Items[1].Frame.X);
    }

    [Fact]
    public void Arrange_AppliesOpacityAndZIndex()
    {
        var provider = new CoverFlowLayoutProvider(itemWidth: 100d, itemSpacing: 0d, minOpacity: 0.5d);
        var context = new LayoutContext(ItemCount: 3, ViewportWidth: 100d, ViewportHeight: 100d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 3));

        Assert.Equal(1d, snapshot.Items[0].Opacity);
        Assert.True(snapshot.Items[0].ZIndex > snapshot.Items[2].ZIndex);
    }

    [Fact]
    public void Arrange_CenterItem_HasFullScale_NoRotation()
    {
        var provider = new CoverFlowLayoutProvider(itemWidth: 100d, itemSpacing: 10d);
        var context = new LayoutContext(ItemCount: 5, ViewportWidth: 100d, ViewportHeight: 200d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 5));

        Assert.Equal(1d, snapshot.Items[0].Scale);
        Assert.Equal(0d, snapshot.Items[0].RotationY);
        Assert.Equal(0d, snapshot.Items[0].TranslateX);
    }

    [Fact]
    public void Arrange_SideItems_HaveSmallerScale_AndRotation()
    {
        var provider = new CoverFlowLayoutProvider(itemWidth: 100d, itemSpacing: 0d,
            maxRotationDegrees: 60d, minScale: 0.5d);
        var context = new LayoutContext(ItemCount: 5, ViewportWidth: 100d, ViewportHeight: 200d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 5));

        Assert.True(snapshot.Items[0].Scale > snapshot.Items[2].Scale);
        Assert.NotEqual(0d, snapshot.Items[1].RotationY);
        Assert.NotEqual(0d, snapshot.Items[2].RotationY);
        Assert.True(snapshot.Items[1].RotationY < 0);
    }

    [Fact]
    public void Arrange_NegativeSpacing_AllowsOverlap()
    {
        var provider = new CoverFlowLayoutProvider(itemWidth: 200d, itemSpacing: -50d);
        var context = new LayoutContext(ItemCount: 3, ViewportWidth: 400d, ViewportHeight: 300d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 3));

        // pitch = 200 - 50 = 150
        Assert.Equal(100d, snapshot.Items[0].Frame.X); // (400-200)/2
        Assert.Equal(250d, snapshot.Items[1].Frame.X); // 100+150
    }

    [Fact]
    public void Arrange_ItemHeight_CentersVertically()
    {
        var provider = new CoverFlowLayoutProvider(itemWidth: 100d, itemHeight: 200d);
        var context = new LayoutContext(ItemCount: 1, ViewportWidth: 400d, ViewportHeight: 400d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 1));

        Assert.Equal(200d, snapshot.Items[0].Frame.Height);
        Assert.Equal(100d, snapshot.Items[0].Frame.Y); // (400-200)/2
    }

    [Fact]
    public void Arrange_PerspectiveDepth_ControlsTransitionSpeed()
    {
        // Shallow depth = faster transition to side state
        var shallow = new CoverFlowLayoutProvider(itemWidth: 100d, perspectiveDepth: 1d, minScale: 0.5d);
        // Deep depth = slower transition
        var deep = new CoverFlowLayoutProvider(itemWidth: 100d, perspectiveDepth: 5d, minScale: 0.5d);
        var context = new LayoutContext(ItemCount: 3, ViewportWidth: 100d, ViewportHeight: 200d);

        var shallowSnap = shallow.Arrange(context, new ItemRange(0, 3));
        var deepSnap = deep.Arrange(context, new ItemRange(0, 3));

        // Item 1 (1 pitch away): shallow should be more transformed than deep
        Assert.True(shallowSnap.Items[1].Scale < deepSnap.Items[1].Scale);
    }

    [Fact]
    public void Arrange_SideOffset_ShiftsSideItems()
    {
        var withOffset = new CoverFlowLayoutProvider(itemWidth: 100d, sideOffset: 100d);
        var noOffset = new CoverFlowLayoutProvider(itemWidth: 100d, sideOffset: 0d);
        var context = new LayoutContext(ItemCount: 3, ViewportWidth: 100d, ViewportHeight: 200d);

        var withSnap = withOffset.Arrange(context, new ItemRange(0, 3));
        var noSnap = noOffset.Arrange(context, new ItemRange(0, 3));

        Assert.Equal(0d, withSnap.Items[0].TranslateX); // center has no shift
        Assert.Equal(0d, noSnap.Items[1].TranslateX);   // no offset = no shift
        Assert.NotEqual(0d, withSnap.Items[1].TranslateX); // offset applied to side items
    }

    [Fact]
    public void Capabilities_HasTransformsAndSnapping()
    {
        var provider = new CoverFlowLayoutProvider(itemWidth: 100d);
        Assert.True(provider.Capabilities.HasFlag(LayoutCapabilities.SupportsTransforms));
        Assert.True(provider.Capabilities.HasFlag(LayoutCapabilities.SupportsSnapping));
    }

    [Fact]
    public void GetSnapOffset_ReturnsCorrectValue()
    {
        var provider = new CoverFlowLayoutProvider(itemWidth: 150d, itemSpacing: 20d);
        Assert.Equal(0d, provider.GetSnapOffset(0, 400d));
        Assert.Equal(170d, provider.GetSnapOffset(1, 400d));
    }

    [Fact]
    public void Constructor_Validates_ItemWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CoverFlowLayoutProvider(0d));
    }

    [Fact]
    public void Constructor_Validates_PerspectiveDepth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CoverFlowLayoutProvider(100d, perspectiveDepth: 0d));
    }
}
