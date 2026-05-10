namespace Maui.Assemblage.Core.Data;

public sealed class GroupedCollectionDataSource : ICollectionDataSource
{
    private readonly IReadOnlyList<GroupSection> _sections;

    public GroupedCollectionDataSource(IEnumerable<GroupSection> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);
        _sections = sections as IReadOnlyList<GroupSection> ?? sections.ToArray();
    }

    public int SectionCount => _sections.Count;

    public int GetItemCount(int section)
    {
        EnsureSection(section);
        return _sections[section].Items.Count;
    }

    public object? GetItem(int section, int index)
    {
        EnsureSection(section);
        var items = _sections[section].Items;

        if ((uint)index >= (uint)items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return items[index];
    }

    public object? GetSectionHeader(int section)
    {
        EnsureSection(section);
        return _sections[section].Header;
    }

    public object? GetSectionFooter(int section)
    {
        EnsureSection(section);
        return _sections[section].Footer;
    }

    private void EnsureSection(int section)
    {
        if ((uint)section >= (uint)_sections.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(section));
        }
    }
}

public sealed class GroupSection
{
    public GroupSection(object? header, IReadOnlyList<object?> items, object? footer = null)
    {
        Header = header;
        Items = items;
        Footer = footer;
    }

    public object? Header { get; }

    public IReadOnlyList<object?> Items { get; }

    public object? Footer { get; }
}
