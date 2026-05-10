using Maui.Assemblage.Core.Data;

namespace Maui.Assemblage.Core.Tests;

public class CollectionChangeSetTests
{
    [Fact]
    public void Add_InsertsTrackChanges()
    {
        var set = new CollectionChangeSet();
        set.Add(CollectionChange.Insert(0, 5, 3));
        set.Add(CollectionChange.Remove(0, 2, 1));

        Assert.Equal(2, set.Changes.Count);
        Assert.Equal(CollectionChangeAction.Insert, set.Changes[0].Action);
        Assert.Equal(5, set.Changes[0].StartIndex);
        Assert.Equal(3, set.Changes[0].Count);
        Assert.Equal(CollectionChangeAction.Remove, set.Changes[1].Action);
    }

    [Fact]
    public void AddReset_ClearsAndSetsReset()
    {
        var set = new CollectionChangeSet();
        set.Add(CollectionChange.Insert(0, 0, 1));
        set.AddReset();

        Assert.True(set.IsReset);
        Assert.Single(set.Changes);
        Assert.Equal(CollectionChangeAction.Reset, set.Changes[0].Action);
    }

    [Fact]
    public void Move_SetsDestinationIndex()
    {
        var change = CollectionChange.Move(0, 3, 7);

        Assert.Equal(CollectionChangeAction.Move, change.Action);
        Assert.Equal(3, change.StartIndex);
        Assert.Equal(7, change.DestinationIndex);
    }
}
