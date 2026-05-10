namespace Maui.Assemblage.Core.Data;

public sealed class EnumerableCollectionDataSource : ICollectionDataSource
{
    private readonly IReadOnlyList<object?> _items;

    public EnumerableCollectionDataSource(IEnumerable<object?> items, object? sectionHeader = null, object? sectionFooter = null)
    {
        ArgumentNullException.ThrowIfNull(items);

        _items = items as IReadOnlyList<object?> ?? items.ToArray();
        SectionHeader = sectionHeader;
        SectionFooter = sectionFooter;
    }

    public object? SectionHeader { get; }

    public object? SectionFooter { get; }

    public int SectionCount => 1;

    public int GetItemCount(int section)
    {
        EnsureSection(section);
        return _items.Count;
    }

    public object? GetItem(int section, int index)
    {
        EnsureSection(section);

        if ((uint)index >= (uint)_items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _items[index];
    }

    public object? GetSectionHeader(int section)
    {
        EnsureSection(section);
        return SectionHeader;
    }

    public object? GetSectionFooter(int section)
    {
        EnsureSection(section);
        return SectionFooter;
    }

    private static void EnsureSection(int section)
    {
        if (section != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(section));
        }
    }
}
