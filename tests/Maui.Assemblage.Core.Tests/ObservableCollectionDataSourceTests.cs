using System.Collections.ObjectModel;
using Maui.Assemblage.Core.Data;

namespace Maui.Assemblage.Core.Tests;

public class ObservableCollectionDataSourceTests
{
    [Fact]
    public void Wraps_ObservableCollection()
    {
        var oc = new ObservableCollection<object?>(["a", "b", "c"]);
        using var source = new ObservableCollectionDataSource(oc);

        Assert.Equal(1, source.SectionCount);
        Assert.Equal(3, source.GetItemCount(0));
        Assert.Equal("b", source.GetItem(0, 1));
    }

    [Fact]
    public void Fires_ChangesApplied_OnAdd()
    {
        var oc = new ObservableCollection<object?>(["a"]);
        using var source = new ObservableCollectionDataSource(oc);

        CollectionChangeSet? received = null;
        source.ChangesApplied += (_, set) => received = set;

        oc.Add("b");

        Assert.NotNull(received);
        Assert.Single(received!.Changes);
        Assert.Equal(CollectionChangeAction.Insert, received.Changes[0].Action);
        Assert.Equal(1, received.Changes[0].StartIndex);
    }

    [Fact]
    public void Fires_ChangesApplied_OnRemove()
    {
        var oc = new ObservableCollection<object?>(["a", "b", "c"]);
        using var source = new ObservableCollectionDataSource(oc);

        CollectionChangeSet? received = null;
        source.ChangesApplied += (_, set) => received = set;

        oc.RemoveAt(1);

        Assert.NotNull(received);
        Assert.Equal(CollectionChangeAction.Remove, received!.Changes[0].Action);
        Assert.Equal(1, received.Changes[0].StartIndex);
    }

    [Fact]
    public void Fires_ChangesApplied_OnReplace()
    {
        var oc = new ObservableCollection<object?>(["a", "b"]);
        using var source = new ObservableCollectionDataSource(oc);

        CollectionChangeSet? received = null;
        source.ChangesApplied += (_, set) => received = set;

        oc[0] = "z";

        Assert.NotNull(received);
        Assert.Equal(CollectionChangeAction.Replace, received!.Changes[0].Action);
    }

    [Fact]
    public void Fires_ChangesApplied_OnClear()
    {
        var oc = new ObservableCollection<object?>(["a", "b"]);
        using var source = new ObservableCollectionDataSource(oc);

        CollectionChangeSet? received = null;
        source.ChangesApplied += (_, set) => received = set;

        oc.Clear();

        Assert.NotNull(received);
        Assert.True(received!.IsReset);
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        var oc = new ObservableCollection<object?>(["a"]);
        var source = new ObservableCollectionDataSource(oc);

        CollectionChangeSet? received = null;
        source.ChangesApplied += (_, set) => received = set;

        source.Dispose();
        oc.Add("b");

        Assert.Null(received);
    }

    [Fact]
    public void ReflectsLiveCount_AfterMutation()
    {
        var oc = new ObservableCollection<object?>(["a"]);
        using var source = new ObservableCollectionDataSource(oc);

        oc.Add("b");
        oc.Add("c");

        Assert.Equal(3, source.GetItemCount(0));
        Assert.Equal("c", source.GetItem(0, 2));
    }
}
