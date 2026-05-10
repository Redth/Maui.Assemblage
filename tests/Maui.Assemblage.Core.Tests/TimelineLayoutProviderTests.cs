using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Tests;

public class TimelineLayoutProviderTests
{
    [Fact]
    public void Arrange_AlternatesLeftAndRight()
    {
        var provider = new TimelineLayoutProvider(itemHeight: 80d, verticalSpacing: 10d, spineWidth: 4d, itemInset: 0d);
        var context = new LayoutContext(ItemCount: 4, ViewportWidth: 400d, ViewportHeight: 600d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 4));

        Assert.Equal(4, snapshot.Items.Count);

        // Even items on left (x=0), odd items on right
        var halfViewport = 200d;
        var itemWidth = halfViewport - 0d - 2d; // half - inset - spine/2
        Assert.Equal(0d, snapshot.Items[0].Frame.X);         // item 0: left
        Assert.Equal(itemWidth, snapshot.Items[0].Frame.Width, 1);

        // item 1: right side = halfViewport + spine/2 + inset
        Assert.Equal(202d, snapshot.Items[1].Frame.X, 1);    // 200 + 2 + 0
    }

    [Fact]
    public void Arrange_FixedHeight_CorrectYPositions()
    {
        var provider = new TimelineLayoutProvider(itemHeight: 60d, verticalSpacing: 10d, spineWidth: 2d);
        var context = new LayoutContext(ItemCount: 3, ViewportWidth: 300d, ViewportHeight: 400d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 3));

        // First item starts at verticalSpacing (10)
        Assert.Equal(10d, snapshot.Items[0].Frame.Y);
        Assert.Equal(80d, snapshot.Items[1].Frame.Y);  // 10 + 60 + 10
        Assert.Equal(150d, snapshot.Items[2].Frame.Y);  // 80 + 60 + 10
    }

    [Fact]
    public void Arrange_VariableHeight_UsesResolver()
    {
        var heights = new[] { 100d, 50d, 80d };
        var provider = new TimelineLayoutProvider(
            itemHeight: 60d, verticalSpacing: 10d, spineWidth: 2d,
            itemHeightResolver: idx => heights[idx]);
        var context = new LayoutContext(ItemCount: 3, ViewportWidth: 300d, ViewportHeight: 400d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 3));

        Assert.Equal(100d, snapshot.Items[0].Frame.Height);
        Assert.Equal(50d, snapshot.Items[1].Frame.Height);
        Assert.Equal(80d, snapshot.Items[2].Frame.Height);

        // Y starts at verticalSpacing (10)
        Assert.Equal(10d, snapshot.Items[0].Frame.Y);
        Assert.Equal(120d, snapshot.Items[1].Frame.Y);  // 10 + 100 + 10
        Assert.Equal(180d, snapshot.Items[2].Frame.Y);  // 120 + 50 + 10
    }

    [Fact]
    public void Arrange_ContentHeight_IsCorrect()
    {
        var provider = new TimelineLayoutProvider(itemHeight: 80d, verticalSpacing: 10d, spineWidth: 2d);
        var context = new LayoutContext(ItemCount: 5, ViewportWidth: 400d, ViewportHeight: 600d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 5));

        // yPositions[0] = 10 (verticalSpacing)
        // Each step: +80 +10 = +90
        // yPositions[5] = 10 + 5*90 = 460
        Assert.Equal(460d, snapshot.ContentHeight);
    }

    [Fact]
    public void Arrange_EmptyItems_ReturnsEmptySnapshot()
    {
        var provider = new TimelineLayoutProvider(itemHeight: 80d, verticalSpacing: 10d, spineWidth: 2d);
        var context = new LayoutContext(ItemCount: 0, ViewportWidth: 400d, ViewportHeight: 600d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 0));

        Assert.Empty(snapshot.Items);
        Assert.Equal(0d, snapshot.ContentHeight);
    }

    [Fact]
    public void Arrange_SpineWidth_AffectsItemWidth()
    {
        var narrow = new TimelineLayoutProvider(itemHeight: 60d, verticalSpacing: 10d, spineWidth: 2d);
        var wide = new TimelineLayoutProvider(itemHeight: 60d, verticalSpacing: 10d, spineWidth: 20d);
        var context = new LayoutContext(ItemCount: 1, ViewportWidth: 400d, ViewportHeight: 400d);

        var narrowSnap = narrow.Arrange(context, new ItemRange(0, 1));
        var wideSnap = wide.Arrange(context, new ItemRange(0, 1));

        Assert.True(narrowSnap.Items[0].Frame.Width > wideSnap.Items[0].Frame.Width);
    }

    [Fact]
    public void Arrange_AllItems_HaveDefaultTransforms()
    {
        var provider = new TimelineLayoutProvider(itemHeight: 60d);
        var context = new LayoutContext(ItemCount: 3, ViewportWidth: 300d, ViewportHeight: 400d);

        var snapshot = provider.Arrange(context, new ItemRange(0, 3));

        foreach (var item in snapshot.Items)
        {
            Assert.Equal(1d, item.Scale);
            Assert.Equal(1d, item.Opacity);
            Assert.Equal(0d, item.RotationY);
            Assert.Equal(0d, item.RotationX);
        }
    }

    [Fact]
    public void Capabilities_HasNone()
    {
        var provider = new TimelineLayoutProvider(itemHeight: 60d);
        Assert.Equal(LayoutCapabilities.None, provider.Capabilities);
    }

    [Fact]
    public void Invalidate_ReturnsCorrectPlan()
    {
        var provider = new TimelineLayoutProvider(itemHeight: 60d);
        var ctx = new LayoutInvalidationContext(ViewportChanged: true, DataChanged: false);
        Assert.True(provider.Invalidate(ctx).RequiresFullArrange);

        ctx = new LayoutInvalidationContext(ViewportChanged: false, DataChanged: true);
        Assert.True(provider.Invalidate(ctx).RequiresFullArrange);

        ctx = new LayoutInvalidationContext(ViewportChanged: false, DataChanged: false);
        Assert.False(provider.Invalidate(ctx).RequiresFullArrange);
    }

    [Fact]
    public void Constructor_Validates_ItemHeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineLayoutProvider(0d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineLayoutProvider(-5d));
    }
}
