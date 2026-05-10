using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Tests;

public class WheelLayoutProviderTests
{
    [Fact]
    public void Arrange_CenterItem_HasFullScaleAndNoRotation()
    {
        var provider = new WheelLayoutProvider(itemHeight: 48d, maxRotationDegrees: 60d, minScale: 0.7d);
        var context = new LayoutContext(ItemCount: 10, ViewportWidth: 300d, ViewportHeight: 280d,
            ScrollOffset: 0d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 10));

        // Item 0 at scrollOffset=0 should be centered
        Assert.Equal(1d, snapshot.Items[0].Scale);
        Assert.Equal(1d, snapshot.Items[0].Opacity);
        Assert.Equal(0d, snapshot.Items[0].RotationX, 0.1d);
    }

    [Fact]
    public void Arrange_ItemsAwayFromCenter_HaveSmallerScaleAndRotation()
    {
        var provider = new WheelLayoutProvider(itemHeight: 48d, maxRotationDegrees: 60d,
            minScale: 0.7d, minOpacity: 0.3d);
        var context = new LayoutContext(ItemCount: 10, ViewportWidth: 300d, ViewportHeight: 280d,
            ScrollOffset: 0d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 10));

        // Item 0 (center): full scale
        // Items farther away should have smaller scale and more rotation
        Assert.True(snapshot.Items[0].Scale > snapshot.Items[2].Scale);
        Assert.True(snapshot.Items[0].Opacity > snapshot.Items[3].Opacity);

        // Items below center should have positive RotationX
        Assert.True(snapshot.Items[1].RotationX > 0);
        Assert.True(snapshot.Items[2].RotationX > snapshot.Items[1].RotationX);
    }

    [Fact]
    public void Arrange_CenterItem_HasHighestZIndex()
    {
        var provider = new WheelLayoutProvider(itemHeight: 48d);
        var context = new LayoutContext(ItemCount: 5, ViewportWidth: 300d, ViewportHeight: 280d,
            ScrollOffset: 0d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 5));

        Assert.True(snapshot.Items[0].ZIndex > snapshot.Items[1].ZIndex);
        Assert.True(snapshot.Items[1].ZIndex > snapshot.Items[2].ZIndex);
    }

    [Fact]
    public void Arrange_ContentHeight_AllowsCentering()
    {
        var provider = new WheelLayoutProvider(itemHeight: 48d);
        var context = new LayoutContext(ItemCount: 10, ViewportWidth: 300d, ViewportHeight: 280d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 10));

        // Content height: 2*centerOffset + itemCount*itemHeight
        // centerOffset = (280-48)/2 = 116
        // contentHeight = 2*116 + 10*48 = 712
        Assert.Equal(712d, snapshot.ContentHeight);
    }

    [Fact]
    public void Arrange_ScrolledToItem_CentersIt()
    {
        var provider = new WheelLayoutProvider(itemHeight: 48d, maxRotationDegrees: 60d);
        // Scroll to item 5: offset = 5 * 48 = 240
        var context = new LayoutContext(ItemCount: 20, ViewportWidth: 300d, ViewportHeight: 280d,
            ScrollOffset: 240d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 20));

        // Item 5 should now be centered (scale ~1, rotationX ~0)
        var item5 = snapshot.Items[5];
        Assert.Equal(1d, item5.Scale, 0.01d);
        Assert.Equal(0d, item5.RotationX, 0.1d);
    }

    [Fact]
    public void Arrange_Empty_ReturnsEmptySnapshot()
    {
        var provider = new WheelLayoutProvider(itemHeight: 48d);
        var context = new LayoutContext(ItemCount: 0, ViewportWidth: 300d, ViewportHeight: 280d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 0));

        Assert.Empty(snapshot.Items);
        Assert.Equal(0d, snapshot.ContentHeight);
    }

    [Fact]
    public void GetSnapOffset_ReturnsItemIndexTimesHeight()
    {
        var provider = new WheelLayoutProvider(itemHeight: 48d);

        Assert.Equal(0d, provider.GetSnapOffset(0, 280d));
        Assert.Equal(48d, provider.GetSnapOffset(1, 280d));
        Assert.Equal(240d, provider.GetSnapOffset(5, 280d));
    }

    [Fact]
    public void GetSnapTargetIndex_SnapsToNearestItem()
    {
        var provider = new WheelLayoutProvider(itemHeight: 48d);

        // Exactly at item 3
        Assert.Equal(3, provider.GetSnapTargetIndex(144d, 0d, 20, 280d));

        // Between items 3 and 4 (closer to 3)
        Assert.Equal(3, provider.GetSnapTargetIndex(150d, 0d, 20, 280d));

        // Between items 3 and 4 (closer to 4)
        Assert.Equal(4, provider.GetSnapTargetIndex(175d, 0d, 20, 280d));
    }

    [Fact]
    public void GetSnapTargetIndex_ClampsToRange()
    {
        var provider = new WheelLayoutProvider(itemHeight: 48d);

        Assert.Equal(0, provider.GetSnapTargetIndex(-100d, 0d, 10, 280d));
        Assert.Equal(9, provider.GetSnapTargetIndex(10000d, 0d, 10, 280d));
    }

    [Fact]
    public void GetSnapTargetIndex_ConsidersVelocity()
    {
        var provider = new WheelLayoutProvider(itemHeight: 48d);

        // At item 3 with forward velocity should snap to 4
        var withVelocity = provider.GetSnapTargetIndex(144d, 200d, 20, 280d);
        var withoutVelocity = provider.GetSnapTargetIndex(144d, 0d, 20, 280d);

        Assert.True(withVelocity >= withoutVelocity);
    }

    [Fact]
    public void Capabilities_HasTransformsAndSnapping()
    {
        var provider = new WheelLayoutProvider(itemHeight: 48d);
        Assert.True(provider.Capabilities.HasFlag(LayoutCapabilities.SupportsTransforms));
        Assert.True(provider.Capabilities.HasFlag(LayoutCapabilities.SupportsSnapping));
    }

    [Fact]
    public void Constructor_Validates_ItemHeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WheelLayoutProvider(0d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WheelLayoutProvider(-5d));
    }

    [Fact]
    public void Constructor_Validates_VisibleItems()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WheelLayoutProvider(48d, visibleItems: 0d));
    }

    [Fact]
    public void Arrange_YCompression_MovesItemsTowardCenter()
    {
        var provider = new WheelLayoutProvider(itemHeight: 48d, cylinderRadius: 3d);
        var context = new LayoutContext(ItemCount: 10, ViewportWidth: 300d, ViewportHeight: 280d,
            ScrollOffset: 0d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 10));

        // Items below center should be compressed upward
        // Item 1 should be shifted slightly toward center compared to pure linear layout
        var centerOffset = (280d - 48d) / 2d;
        var linearY1 = centerOffset + 1 * 48d;
        Assert.True(snapshot.Items[1].Frame.Y < linearY1);
    }

    [Fact]
    public void Invalidate_RequiresFullArrange_OnChanges()
    {
        var provider = new WheelLayoutProvider(itemHeight: 48d);

        Assert.True(provider.Invalidate(
            new LayoutInvalidationContext(ViewportChanged: true, DataChanged: false)).RequiresFullArrange);
        Assert.True(provider.Invalidate(
            new LayoutInvalidationContext(ViewportChanged: false, DataChanged: true)).RequiresFullArrange);
        Assert.True(provider.Invalidate(
            new LayoutInvalidationContext(ViewportChanged: true, DataChanged: true)).RequiresFullArrange);
        Assert.False(provider.Invalidate(
            new LayoutInvalidationContext(ViewportChanged: false, DataChanged: false)).RequiresFullArrange);
    }
}
