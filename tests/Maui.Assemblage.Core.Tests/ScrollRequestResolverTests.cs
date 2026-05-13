using Maui.Assemblage.Core.Scroll;
using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Tests;

public class ScrollRequestResolverTests
{
    [Fact]
    public void ToStart_ReturnsZero()
    {
        var request = ScrollRequest.ToStart();
        var provider = new LinearLayoutProvider(50d);
        var context = new LayoutContext(100, 400d, 600d);

        var offset = ScrollRequestResolver.Resolve(request, provider, context, null);

        Assert.Equal(0d, offset);
    }

    [Fact]
    public void ToEnd_ReturnsContentMinusViewport()
    {
        var request = ScrollRequest.ToEnd();
        var provider = new LinearLayoutProvider(50d, spacing: 0d);
        var context = new LayoutContext(20, 400d, 600d);

        var offset = ScrollRequestResolver.Resolve(request, provider, context, null);

        // Content height = 20 * 50 = 1000, viewport = 600 → 400
        Assert.Equal(400d, offset);
    }

    [Fact]
    public void ToEnd_Horizontal_UsesContentWidth()
    {
        var request = ScrollRequest.ToEnd();
        var provider = new LinearLayoutProvider(50d, spacing: 0d, LayoutOrientation.Horizontal);
        var context = new LayoutContext(20, 400d, 600d);

        var offset = ScrollRequestResolver.Resolve(request, provider, context, null, LayoutOrientation.Horizontal);

        Assert.Equal(600d, offset);
    }

    [Fact]
    public void ToOffset_ClampsToZero()
    {
        var request = ScrollRequest.ToOffset(-100d);
        var provider = new LinearLayoutProvider(50d);
        var context = new LayoutContext(10, 400d, 600d);

        var offset = ScrollRequestResolver.Resolve(request, provider, context, null);

        Assert.Equal(0d, offset);
    }

    [Fact]
    public void ToOffset_PassesPositiveOffset()
    {
        var request = ScrollRequest.ToOffset(250d);
        var provider = new LinearLayoutProvider(50d);
        var context = new LayoutContext(10, 400d, 600d);

        var offset = ScrollRequestResolver.Resolve(request, provider, context, null);

        Assert.Equal(250d, offset);
    }

    [Fact]
    public void ToItem_ResolvesItemOffset()
    {
        var request = ScrollRequest.ToItem(0, 5);
        var provider = new LinearLayoutProvider(50d, spacing: 2d);
        var context = new LayoutContext(20, 400d, 600d);

        var offset = ScrollRequestResolver.Resolve(request, provider, context, null);

        // Item 5 at Y = 5 * (50 + 2) = 260
        Assert.Equal(260d, offset);
    }

    [Fact]
    public void ToItem_SnappingLayout_UsesSnapOffset()
    {
        var request = ScrollRequest.ToItem(0, 5);
        var provider = new CarouselLayoutProvider(peekAmount: 40d, itemSpacing: 10d);
        var context = new LayoutContext(20, 400d, 600d);

        var offset = ScrollRequestResolver.Resolve(request, provider, context, null, LayoutOrientation.Horizontal);

        Assert.Equal(1_650d, offset);
    }

    [Fact]
    public void ToItem_OutOfRange_ReturnsZero()
    {
        var request = ScrollRequest.ToItem(0, 999);
        var provider = new LinearLayoutProvider(50d);
        var context = new LayoutContext(10, 400d, 600d);

        var offset = ScrollRequestResolver.Resolve(request, provider, context, null);

        Assert.Equal(0d, offset);
    }

    [Fact]
    public void ScrollRequest_FactoryMethods()
    {
        var start = ScrollRequest.ToStart(false);
        Assert.Equal(ScrollRequestKind.Start, start.Kind);
        Assert.False(start.Animated);

        var end = ScrollRequest.ToEnd();
        Assert.Equal(ScrollRequestKind.End, end.Kind);
        Assert.True(end.Animated);

        var offset = ScrollRequest.ToOffset(100d);
        Assert.Equal(ScrollRequestKind.Offset, offset.Kind);
        Assert.Equal(100d, offset.Offset);

        var item = ScrollRequest.ToItem(2, 5, false);
        Assert.Equal(ScrollRequestKind.Item, item.Kind);
        Assert.Equal(2, item.Section);
        Assert.Equal(5, item.Index);
    }
}
