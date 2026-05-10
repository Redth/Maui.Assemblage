namespace Maui.Assemblage.Core.Data;

public enum CollectionChangeAction
{
    Insert,
    Remove,
    Replace,
    Move,
    Reset
}

public readonly record struct CollectionChange(
    CollectionChangeAction Action,
    int Section,
    int StartIndex,
    int Count,
    int DestinationIndex = -1)
{
    public static CollectionChange Insert(int section, int startIndex, int count = 1)
        => new(CollectionChangeAction.Insert, section, startIndex, count);

    public static CollectionChange Remove(int section, int startIndex, int count = 1)
        => new(CollectionChangeAction.Remove, section, startIndex, count);

    public static CollectionChange Replace(int section, int startIndex, int count = 1)
        => new(CollectionChangeAction.Replace, section, startIndex, count);

    public static CollectionChange Move(int section, int startIndex, int destinationIndex)
        => new(CollectionChangeAction.Move, section, startIndex, 1, destinationIndex);

    public static CollectionChange Reset()
        => new(CollectionChangeAction.Reset, -1, -1, -1);
}

public sealed class CollectionChangeSet
{
    private readonly List<CollectionChange> _changes = [];

    public IReadOnlyList<CollectionChange> Changes => _changes;

    public bool IsReset => _changes.Count == 1 && _changes[0].Action == CollectionChangeAction.Reset;

    public void Add(CollectionChange change) => _changes.Add(change);

    public void AddReset()
    {
        _changes.Clear();
        _changes.Add(CollectionChange.Reset());
    }

    public void Clear() => _changes.Clear();
}
