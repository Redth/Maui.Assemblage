using Maui.Assemblage.Core.Layout;
using Maui.Assemblage.Core.Collections;

namespace Maui.Assemblage.Core.Tests;

public class StickyHeaderLayoutDecoratorTests
{
    [Fact]
    public void NoHeaders_ReturnsOriginalSnapshot()
    {
        var items = new[]
        {
            new LayoutItemAttributes(0, new LayoutRect(0, 0, 400, 50)),
            new LayoutItemAttributes(1, new LayoutRect(0, 50, 400, 50)),
        };
        var snapshot = new LayoutSnapshot(400, 100, items);
        var nodes = new[]
        {
            new CollectionNode(CollectionNodeKind.Item, 0, 0, "A"),
            new CollectionNode(CollectionNodeKind.Item, 0, 1, "B"),
        };

        var result = StickyHeaderLayoutDecorator.Apply(snapshot, nodes, 0d, 300d);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(0d, result.Items[0].Frame.Y);
    }

    [Fact]
    public void Header_BeforeViewport_StaysOriginal()
    {
        var items = new[]
        {
            new LayoutItemAttributes(0, new LayoutRect(0, 0, 400, 30)),
            new LayoutItemAttributes(1, new LayoutRect(0, 30, 400, 50)),
            new LayoutItemAttributes(2, new LayoutRect(0, 80, 400, 50)),
        };
        var snapshot = new LayoutSnapshot(400, 130, items);
        var nodes = new[]
        {
            new CollectionNode(CollectionNodeKind.SectionHeader, 0, 0, "Header"),
            new CollectionNode(CollectionNodeKind.Item, 0, 0, "A"),
            new CollectionNode(CollectionNodeKind.Item, 0, 1, "B"),
        };

        // Scroll offset = 0, header is at Y=0 which is at the top
        var result = StickyHeaderLayoutDecorator.Apply(snapshot, nodes, 0d, 300d);

        Assert.Equal(0d, result.Items[0].Frame.Y);
    }

    [Fact]
    public void Header_ScrolledPast_PinsAtScrollOffset()
    {
        var items = new[]
        {
            new LayoutItemAttributes(0, new LayoutRect(0, 0, 400, 30)),
            new LayoutItemAttributes(1, new LayoutRect(0, 30, 400, 50)),
            new LayoutItemAttributes(2, new LayoutRect(0, 80, 400, 50)),
        };
        var snapshot = new LayoutSnapshot(400, 130, items);
        var nodes = new[]
        {
            new CollectionNode(CollectionNodeKind.SectionHeader, 0, 0, "Header"),
            new CollectionNode(CollectionNodeKind.Item, 0, 0, "A"),
            new CollectionNode(CollectionNodeKind.Item, 0, 1, "B"),
        };

        // Scroll offset = 20, header originally at 0 → should pin at 20
        var result = StickyHeaderLayoutDecorator.Apply(snapshot, nodes, 20d, 300d);

        Assert.Equal(20d, result.Items[0].Frame.Y);
        Assert.True(result.Items[0].ZIndex >= 1000);
    }

    [Fact]
    public void Header_DoesNotExceedSectionEnd()
    {
        var items = new[]
        {
            new LayoutItemAttributes(0, new LayoutRect(0, 0, 400, 30)),
            new LayoutItemAttributes(1, new LayoutRect(0, 30, 400, 50)),
        };
        var snapshot = new LayoutSnapshot(400, 80, items);
        var nodes = new[]
        {
            new CollectionNode(CollectionNodeKind.SectionHeader, 0, 0, "Header"),
            new CollectionNode(CollectionNodeKind.Item, 0, 0, "A"),
        };

        // Scroll offset = 100, section ends at 80, header is 30px tall → max sticky = 80-30=50
        var result = StickyHeaderLayoutDecorator.Apply(snapshot, nodes, 100d, 300d);

        Assert.Equal(50d, result.Items[0].Frame.Y);
    }

    [Fact]
    public void EmptySnapshot_ReturnsSame()
    {
        var snapshot = new LayoutSnapshot(400, 0, Array.Empty<LayoutItemAttributes>());
        var nodes = Array.Empty<CollectionNode>();

        var result = StickyHeaderLayoutDecorator.Apply(snapshot, nodes, 50d, 300d);

        Assert.Empty(result.Items);
    }
}
