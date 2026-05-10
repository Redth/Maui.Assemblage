using Maui.Assemblage.Core.Interactions;

namespace Maui.Assemblage.Core.Tests;

public class SelectionTrackerTests
{
    [Fact]
    public void Toggle_SingleMode_OnlyOneSelected()
    {
        var tracker = new SelectionTracker { Mode = SelectionMode.Single };

        tracker.Toggle("A");
        tracker.Toggle("B");

        Assert.Single(tracker.SelectedItems);
        Assert.True(tracker.IsSelected("B"));
        Assert.False(tracker.IsSelected("A"));
    }

    [Fact]
    public void Toggle_MultipleMode_AddsAndRemoves()
    {
        var tracker = new SelectionTracker { Mode = SelectionMode.Multiple };

        tracker.Toggle("A");
        tracker.Toggle("B");
        tracker.Toggle("A");

        Assert.Single(tracker.SelectedItems);
        Assert.True(tracker.IsSelected("B"));
        Assert.False(tracker.IsSelected("A"));
    }

    [Fact]
    public void Toggle_NoneMode_DoesNothing()
    {
        var tracker = new SelectionTracker { Mode = SelectionMode.None };

        var result = tracker.Toggle("A");

        Assert.False(result);
        Assert.Empty(tracker.SelectedItems);
    }

    [Fact]
    public void Select_SingleMode_ReplacesExisting()
    {
        var tracker = new SelectionTracker { Mode = SelectionMode.Single };

        tracker.Select("A");
        tracker.Select("B");

        Assert.Equal(1, tracker.Count);
        Assert.True(tracker.IsSelected("B"));
    }

    [Fact]
    public void Deselect_RemovesItem()
    {
        var tracker = new SelectionTracker { Mode = SelectionMode.Multiple };

        tracker.Select("A");
        tracker.Select("B");
        tracker.Deselect("A");

        Assert.Equal(1, tracker.Count);
        Assert.False(tracker.IsSelected("A"));
    }

    [Fact]
    public void ClearSelection_RemovesAll()
    {
        var tracker = new SelectionTracker { Mode = SelectionMode.Multiple };

        tracker.Select("A");
        tracker.Select("B");
        tracker.ClearSelection();

        Assert.Equal(0, tracker.Count);
    }

    [Fact]
    public void SelectionChanged_FiresWithCorrectArgs()
    {
        var tracker = new SelectionTracker { Mode = SelectionMode.Single };
        SelectionChangedEventArgs? lastArgs = null;
        tracker.SelectionChanged += (_, args) => lastArgs = args;

        tracker.Select("A");
        Assert.NotNull(lastArgs);
        Assert.Single(lastArgs!.AddedItems);
        Assert.Equal("A", lastArgs.AddedItems[0]);

        tracker.Select("B");
        Assert.Equal("B", lastArgs!.AddedItems[0]);
        Assert.Single(lastArgs.RemovedItems);
        Assert.Equal("A", lastArgs.RemovedItems[0]);
    }
}
