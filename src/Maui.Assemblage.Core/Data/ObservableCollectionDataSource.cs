using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Maui.Assemblage.Core.Data;

public sealed class ObservableCollectionDataSource : ICollectionDataSource, INotifyDataSourceChanged, IDisposable
{
    private INotifyCollectionChanged? _observable;
    private IList<object?> _items;

    public ObservableCollectionDataSource(ObservableCollection<object?> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _items = source;
        _observable = source;
        _observable.CollectionChanged += OnCollectionChanged;
    }

    public event EventHandler<CollectionChangeSet>? ChangesApplied;

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
        return null;
    }

    public object? GetSectionFooter(int section)
    {
        EnsureSection(section);
        return null;
    }

    public void Dispose()
    {
        if (_observable is not null)
        {
            _observable.CollectionChanged -= OnCollectionChanged;
            _observable = null;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var changeSet = new CollectionChangeSet();

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                changeSet.Add(CollectionChange.Insert(0, e.NewStartingIndex, e.NewItems?.Count ?? 1));
                break;

            case NotifyCollectionChangedAction.Remove:
                changeSet.Add(CollectionChange.Remove(0, e.OldStartingIndex, e.OldItems?.Count ?? 1));
                break;

            case NotifyCollectionChangedAction.Replace:
                changeSet.Add(CollectionChange.Replace(0, e.NewStartingIndex, e.NewItems?.Count ?? 1));
                break;

            case NotifyCollectionChangedAction.Move:
                changeSet.Add(CollectionChange.Move(0, e.OldStartingIndex, e.NewStartingIndex));
                break;

            case NotifyCollectionChangedAction.Reset:
                changeSet.AddReset();
                break;
        }

        ChangesApplied?.Invoke(this, changeSet);
    }

    private static void EnsureSection(int section)
    {
        if (section != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(section));
        }
    }
}
