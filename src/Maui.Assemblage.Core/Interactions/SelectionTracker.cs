namespace Maui.Assemblage.Core.Interactions;

public enum SelectionMode
{
    None,
    Single,
    Multiple
}

public sealed class SelectionTracker
{
    private readonly HashSet<object> _selected = [];

    public SelectionMode Mode { get; set; } = SelectionMode.Single;

    public IReadOnlySet<object> SelectedItems => _selected;

    public int Count => _selected.Count;

    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    public bool Toggle(object item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (Mode == SelectionMode.None)
        {
            return false;
        }

        if (_selected.Contains(item))
        {
            _selected.Remove(item);
            RaiseChanged([], [item]);
            return false;
        }

        if (Mode == SelectionMode.Single && _selected.Count > 0)
        {
            var previous = _selected.ToArray();
            _selected.Clear();
            _selected.Add(item);
            RaiseChanged([item], previous);
        }
        else
        {
            _selected.Add(item);
            RaiseChanged([item], []);
        }

        return true;
    }

    public bool Select(object item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (Mode == SelectionMode.None)
        {
            return false;
        }

        if (_selected.Contains(item))
        {
            return false;
        }

        if (Mode == SelectionMode.Single && _selected.Count > 0)
        {
            var previous = _selected.ToArray();
            _selected.Clear();
            _selected.Add(item);
            RaiseChanged([item], previous);
        }
        else
        {
            _selected.Add(item);
            RaiseChanged([item], []);
        }

        return true;
    }

    public bool Deselect(object item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!_selected.Remove(item))
        {
            return false;
        }

        RaiseChanged([], [item]);
        return true;
    }

    public void ClearSelection()
    {
        if (_selected.Count == 0)
        {
            return;
        }

        var previous = _selected.ToArray();
        _selected.Clear();
        RaiseChanged([], previous);
    }

    public bool IsSelected(object item) => _selected.Contains(item);

    private void RaiseChanged(object[] added, object[] removed)
    {
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(added, removed));
    }
}

public sealed class SelectionChangedEventArgs : EventArgs
{
    public SelectionChangedEventArgs(IReadOnlyList<object> addedItems, IReadOnlyList<object> removedItems)
    {
        AddedItems = addedItems;
        RemovedItems = removedItems;
    }

    public IReadOnlyList<object> AddedItems { get; }

    public IReadOnlyList<object> RemovedItems { get; }
}
