namespace Maui.Assemblage.Core.Data;

public interface ICollectionDataSource
{
    int SectionCount { get; }

    int GetItemCount(int section);

    object? GetItem(int section, int index);

    object? GetSectionHeader(int section);

    object? GetSectionFooter(int section);
}

/// <summary>
/// Implemented by data sources that can notify the engine of incremental changes,
/// allowing the engine to avoid full rebuilds on add/remove/replace/move operations.
/// </summary>
public interface INotifyDataSourceChanged
{
    event EventHandler<CollectionChangeSet>? ChangesApplied;
}
