using Maui.Assemblage.Core.Collections;
using Maui.Assemblage.Core.Data;
using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Tests;

public class SectionIndexMapTests
{
    [Fact]
    public void GetFlatIndex_MapsCorrectly()
    {
        var source = new GroupedCollectionDataSource(
        [
            new GroupSection("S0", ["a", "b"]),
            new GroupSection("S1", ["c"])
        ]);

        var nodes = CollectionNodeFlattener.Flatten(source);
        var map = new SectionIndexMap(nodes);

        Assert.Equal(5, map.Count);

        // S0 header is flat 0
        Assert.Equal(0, map.GetSupplementaryFlatIndex(CollectionNodeKind.SectionHeader, 0));
        // a -> flat 1, b -> flat 2
        Assert.Equal(1, map.GetFlatIndex(0, 0));
        Assert.Equal(2, map.GetFlatIndex(0, 1));
        // S1 header is flat 3
        Assert.Equal(3, map.GetSupplementaryFlatIndex(CollectionNodeKind.SectionHeader, 1));
        // c -> flat 4
        Assert.Equal(4, map.GetFlatIndex(1, 0));
    }

    [Fact]
    public void TryGetFlatIndex_ReturnsFalseForMissing()
    {
        var source = new EnumerableCollectionDataSource(new object?[] { "x" });
        var nodes = CollectionNodeFlattener.Flatten(source, new CollectionNodeFlattenOptions { IncludeSectionHeaders = false });
        var map = new SectionIndexMap(nodes);

        Assert.False(map.TryGetFlatIndex(5, 0, out _));
    }

    [Fact]
    public void GetNode_ReturnsCorrectNode()
    {
        var source = new EnumerableCollectionDataSource(new object?[] { "hello", "world" });
        var nodes = CollectionNodeFlattener.Flatten(source, new CollectionNodeFlattenOptions { IncludeSectionHeaders = false });
        var map = new SectionIndexMap(nodes);

        var node = map.GetNode(1);
        Assert.Equal(CollectionNodeKind.Item, node.Kind);
        Assert.Equal("world", node.Data);
    }
}
