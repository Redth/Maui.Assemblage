using Maui.Assemblage.Core.Data;

namespace Maui.Assemblage.Core.Collections;

public static class CollectionNodeFlattener
{
    public static IReadOnlyList<CollectionNode> Flatten(
        ICollectionDataSource dataSource,
        CollectionNodeFlattenOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        var opts = options ?? new CollectionNodeFlattenOptions();

        if (dataSource.SectionCount < 0)
        {
            throw new InvalidOperationException("Section count cannot be negative.");
        }

        var nodes = new List<CollectionNode>();
        var hasItems = false;

        if (opts.Header is not null)
        {
            nodes.Add(new CollectionNode(CollectionNodeKind.Header, Section: -1, Index: -1, Data: opts.Header));
        }

        for (var section = 0; section < dataSource.SectionCount; section++)
        {
            if (opts.IncludeSectionHeaders)
            {
                var sectionHeader = dataSource.GetSectionHeader(section);
                if (sectionHeader is not null)
                {
                    nodes.Add(new CollectionNode(CollectionNodeKind.SectionHeader, Section: section, Index: -1, Data: sectionHeader));
                }
            }

            var itemCount = dataSource.GetItemCount(section);
            if (itemCount < 0)
            {
                throw new InvalidOperationException("Item count cannot be negative.");
            }

            for (var index = 0; index < itemCount; index++)
            {
                hasItems = true;
                nodes.Add(new CollectionNode(CollectionNodeKind.Item, section, index, dataSource.GetItem(section, index)));
            }

            if (opts.IncludeSectionFooters)
            {
                var sectionFooter = dataSource.GetSectionFooter(section);
                if (sectionFooter is not null)
                {
                    nodes.Add(new CollectionNode(CollectionNodeKind.SectionFooter, Section: section, Index: -1, Data: sectionFooter));
                }
            }
        }

        if (!hasItems && opts.EmptyView is not null)
        {
            nodes.Add(new CollectionNode(CollectionNodeKind.Empty, Section: -1, Index: -1, Data: opts.EmptyView));
        }

        if (opts.Footer is not null)
        {
            nodes.Add(new CollectionNode(CollectionNodeKind.Footer, Section: -1, Index: -1, Data: opts.Footer));
        }

        return nodes;
    }
}
