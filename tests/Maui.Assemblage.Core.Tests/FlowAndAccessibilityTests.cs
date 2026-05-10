using Maui.Assemblage.Core.Layout;
using Maui.Assemblage.Core.Accessibility;

namespace Maui.Assemblage.Core.Tests;

public class FlowDirectionHelperTests
{
    [Fact]
    public void LTR_ReturnsOriginal()
    {
        var items = new[]
        {
            new LayoutItemAttributes(0, new LayoutRect(0, 0, 100, 50)),
            new LayoutItemAttributes(1, new LayoutRect(100, 0, 100, 50)),
        };
        var snapshot = new LayoutSnapshot(400, 50, items);

        var result = FlowDirectionHelper.Apply(snapshot, FlowDirection.LeftToRight);

        Assert.Same(snapshot, result);
    }

    [Fact]
    public void RTL_MirrorsXPositions()
    {
        var items = new[]
        {
            new LayoutItemAttributes(0, new LayoutRect(0, 0, 100, 50)),
            new LayoutItemAttributes(1, new LayoutRect(100, 0, 100, 50)),
        };
        var snapshot = new LayoutSnapshot(400, 50, items);

        var result = FlowDirectionHelper.Apply(snapshot, FlowDirection.RightToLeft);

        // Item 0: mirrored X = 400 - 0 - 100 = 300
        Assert.Equal(300d, result.Items[0].Frame.X);
        // Item 1: mirrored X = 400 - 100 - 100 = 200
        Assert.Equal(200d, result.Items[1].Frame.X);
    }
}

public class FocusTraversalHelperTests
{
    [Fact]
    public void EmptySnapshot_ReturnsNegativeOne()
    {
        var snapshot = new LayoutSnapshot(400, 600, Array.Empty<LayoutItemAttributes>());

        var next = FocusTraversalHelper.GetNextFocusIndex(0, FocusDirection.Down, snapshot);

        Assert.Equal(-1, next);
    }

    [Fact]
    public void Down_FindsItemBelow()
    {
        var items = new[]
        {
            new LayoutItemAttributes(0, new LayoutRect(0, 0, 400, 50)),
            new LayoutItemAttributes(1, new LayoutRect(0, 50, 400, 50)),
            new LayoutItemAttributes(2, new LayoutRect(0, 100, 400, 50)),
        };
        var snapshot = new LayoutSnapshot(400, 150, items);

        var next = FocusTraversalHelper.GetNextFocusIndex(0, FocusDirection.Down, snapshot);

        Assert.Equal(1, next);
    }

    [Fact]
    public void Up_FindsItemAbove()
    {
        var items = new[]
        {
            new LayoutItemAttributes(0, new LayoutRect(0, 0, 400, 50)),
            new LayoutItemAttributes(1, new LayoutRect(0, 50, 400, 50)),
        };
        var snapshot = new LayoutSnapshot(400, 100, items);

        var next = FocusTraversalHelper.GetNextFocusIndex(1, FocusDirection.Up, snapshot);

        Assert.Equal(0, next);
    }

    [Fact]
    public void Right_InGrid_FindsNextColumn()
    {
        var items = new[]
        {
            new LayoutItemAttributes(0, new LayoutRect(0, 0, 100, 100)),
            new LayoutItemAttributes(1, new LayoutRect(110, 0, 100, 100)),
            new LayoutItemAttributes(2, new LayoutRect(0, 110, 100, 100)),
        };
        var snapshot = new LayoutSnapshot(400, 210, items);

        var next = FocusTraversalHelper.GetNextFocusIndex(0, FocusDirection.Right, snapshot);

        Assert.Equal(1, next);
    }

    [Fact]
    public void RTL_Right_NavigatesLeft()
    {
        var items = new[]
        {
            new LayoutItemAttributes(0, new LayoutRect(200, 0, 100, 100)),
            new LayoutItemAttributes(1, new LayoutRect(0, 0, 100, 100)),
        };
        var snapshot = new LayoutSnapshot(400, 100, items);

        // In RTL, "Right" navigates to visually-left items (mapped to Left)
        var next = FocusTraversalHelper.GetNextFocusIndex(0, FocusDirection.Right, snapshot, FlowDirection.RightToLeft);

        // RTL flips Right→Left. Item 1 is at X=0, which is to the left of item 0 at X=200
        Assert.Equal(1, next);
    }
}

public class AccessibilityOrderBuilderTests
{
    [Fact]
    public void ReadingOrder_TopToBottomLeftToRight()
    {
        var items = new[]
        {
            new LayoutItemAttributes(0, new LayoutRect(100, 0, 100, 50)),
            new LayoutItemAttributes(1, new LayoutRect(0, 0, 100, 50)),
            new LayoutItemAttributes(2, new LayoutRect(0, 50, 100, 50)),
        };
        var snapshot = new LayoutSnapshot(400, 100, items);

        var order = AccessibilityOrderBuilder.BuildReadingOrder(snapshot);

        Assert.Equal(new[] { 1, 0, 2 }, order);
    }

    [Fact]
    public void ReadingOrder_RTL()
    {
        var items = new[]
        {
            new LayoutItemAttributes(0, new LayoutRect(0, 0, 100, 50)),
            new LayoutItemAttributes(1, new LayoutRect(100, 0, 100, 50)),
        };
        var snapshot = new LayoutSnapshot(400, 50, items);

        var order = AccessibilityOrderBuilder.BuildReadingOrder(snapshot, isRTL: true);

        // RTL: right-to-left within rows
        Assert.Equal(new[] { 1, 0 }, order);
    }
}
