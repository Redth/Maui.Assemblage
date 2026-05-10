using Maui.Assemblage.Core.Collections;
using Maui.Assemblage.Core.Data;

namespace Maui.Assemblage.Core.Tests;

public class GroupedCollectionDataSourceTests
{
    [Fact]
    public void MultiSection_ReturnsCorrectData()
    {
        var source = new GroupedCollectionDataSource(
        [
            new GroupSection("Fruits", ["Apple", "Banana", "Cherry"]),
            new GroupSection("Veggies", ["Carrot", "Pea"], footer: "end-veggies")
        ]);

        Assert.Equal(2, source.SectionCount);
        Assert.Equal(3, source.GetItemCount(0));
        Assert.Equal(2, source.GetItemCount(1));
        Assert.Equal("Banana", source.GetItem(0, 1));
        Assert.Equal("Pea", source.GetItem(1, 1));
        Assert.Equal("Fruits", source.GetSectionHeader(0));
        Assert.Equal("Veggies", source.GetSectionHeader(1));
        Assert.Null(source.GetSectionFooter(0));
        Assert.Equal("end-veggies", source.GetSectionFooter(1));
    }

    [Fact]
    public void Flatten_GroupedSource_ProducesCorrectNodes()
    {
        var source = new GroupedCollectionDataSource(
        [
            new GroupSection("S0", ["a", "b"]),
            new GroupSection("S1", ["c"])
        ]);

        var nodes = CollectionNodeFlattener.Flatten(source);

        var kinds = nodes.Select(n => n.Kind).ToArray();

        Assert.Equal(
        [
            CollectionNodeKind.SectionHeader,
            CollectionNodeKind.Item,
            CollectionNodeKind.Item,
            CollectionNodeKind.SectionHeader,
            CollectionNodeKind.Item
        ], kinds);

        Assert.Equal("S0", nodes[0].Data);
        Assert.Equal("a", nodes[1].Data);
        Assert.Equal("b", nodes[2].Data);
        Assert.Equal("S1", nodes[3].Data);
        Assert.Equal("c", nodes[4].Data);
    }

    [Fact]
    public void InvalidSection_Throws()
    {
        var source = new GroupedCollectionDataSource(
        [
            new GroupSection("S0", ["x"])
        ]);

        Assert.Throws<ArgumentOutOfRangeException>(() => source.GetItemCount(5));
    }
}
