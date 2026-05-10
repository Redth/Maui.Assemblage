using Maui.Assemblage.Core.Collections;
using Maui.Assemblage.Core.Data;

namespace Maui.Assemblage.Core.Tests;

public class CollectionNodeFlattenerTests
{
    [Fact]
    public void Flatten_ComposesNodesInExpectedOrder()
    {
        var source = new StubDataSource(
            headers: ["section-0-header", "section-1-header"],
            items:
            [
                ["a", "b"],
                ["c"]
            ],
            footers: ["section-0-footer", null]);

        var nodes = CollectionNodeFlattener.Flatten(
            source,
            new CollectionNodeFlattenOptions { Header = "global-header", Footer = "global-footer" });

        var signature = nodes.Select(node => $"{node.Kind}:{node.Section}:{node.Index}:{node.Data}").ToArray();

        Assert.Equal(
        [
            "Header:-1:-1:global-header",
            "SectionHeader:0:-1:section-0-header",
            "Item:0:0:a",
            "Item:0:1:b",
            "SectionFooter:0:-1:section-0-footer",
            "SectionHeader:1:-1:section-1-header",
            "Item:1:0:c",
            "Footer:-1:-1:global-footer"
        ], signature);
    }

    [Fact]
    public void Flatten_UsesEmptyNodeWhenNoItems()
    {
        var source = new StubDataSource(
            headers: [null],
            items:
            [
                []
            ],
            footers: [null]);

        var nodes = CollectionNodeFlattener.Flatten(
            source,
            new CollectionNodeFlattenOptions { Header = "global-header", Footer = "global-footer", EmptyView = "empty" });

        Assert.Equal(
        [
            CollectionNodeKind.Header,
            CollectionNodeKind.Empty,
            CollectionNodeKind.Footer
        ],
        nodes.Select(node => node.Kind).ToArray());
    }

    private sealed class StubDataSource : ICollectionDataSource
    {
        private readonly IReadOnlyList<object?[]> _items;
        private readonly IReadOnlyList<object?> _headers;
        private readonly IReadOnlyList<object?> _footers;

        public StubDataSource(IReadOnlyList<object?> headers, IReadOnlyList<object?[]> items, IReadOnlyList<object?> footers)
        {
            _headers = headers;
            _items = items;
            _footers = footers;
        }

        public int SectionCount => _items.Count;

        public int GetItemCount(int section) => _items[section].Length;

        public object? GetItem(int section, int index) => _items[section][index];

        public object? GetSectionHeader(int section) => _headers[section];

        public object? GetSectionFooter(int section) => _footers[section];
    }
}
