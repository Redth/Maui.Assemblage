using Maui.Assemblage.Core.Interactions;
using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Tests;

public class SwipeActionsTests
{
    [Fact]
    public void SwipeAction_RequiresTitle()
    {
        Assert.Throws<ArgumentException>(() => new SwipeAction(""));
        Assert.Throws<ArgumentException>(() => new SwipeAction("  "));
    }

    [Fact]
    public void SwipeAction_Properties()
    {
        var action = new SwipeAction("Delete", "trash")
        {
            BackgroundColor = "#FF0000",
            IsDestructive = true
        };

        Assert.Equal("Delete", action.Title);
        Assert.Equal("trash", action.IconName);
        Assert.Equal("#FF0000", action.BackgroundColor);
        Assert.True(action.IsDestructive);
    }

    [Fact]
    public void SwipeActionSet_TotalWidth()
    {
        var set = new SwipeActionSet([
            new SwipeAction("Delete"),
            new SwipeAction("Archive")
        ]);

        Assert.Equal(144d, set.TotalWidth()); // 2 * 72
        Assert.Equal(160d, set.TotalWidth(80d)); // 2 * 80
    }

    [Fact]
    public void SwipeGestureTracker_Pan_ClampsToLimits()
    {
        var tracker = new SwipeGestureTracker(0);

        tracker.OnPan(-200d, leadingWidth: 0d, trailingWidth: 72d);

        // Max trailing = 72 * 1.5 = 108
        Assert.True(tracker.CurrentOffset >= -108d);
        Assert.True(tracker.IsTrailingRevealed);
        Assert.False(tracker.IsLeadingRevealed);
    }

    [Fact]
    public void SwipeGestureTracker_NoActions_ClampsToZero()
    {
        var tracker = new SwipeGestureTracker(0);

        tracker.OnPan(-50d, leadingWidth: 0d, trailingWidth: 0d);

        Assert.Equal(0d, tracker.CurrentOffset);
    }

    [Fact]
    public void SwipeGestureTracker_Release_SnapsOpen()
    {
        var tracker = new SwipeGestureTracker(0);

        // Pan past 40% of trailing width (72 * 0.4 = 28.8)
        tracker.OnPan(-40d, leadingWidth: 0d, trailingWidth: 72d);
        var target = tracker.OnRelease(0d, leadingWidth: 0d, trailingWidth: 72d);

        Assert.Equal(-72d, target);
    }

    [Fact]
    public void SwipeGestureTracker_Release_SnapsClosed()
    {
        var tracker = new SwipeGestureTracker(0);

        // Pan less than 40% of trailing width
        tracker.OnPan(-10d, leadingWidth: 0d, trailingWidth: 72d);
        var target = tracker.OnRelease(0d, leadingWidth: 0d, trailingWidth: 72d);

        Assert.Equal(0d, target);
    }

    [Fact]
    public void SwipeGestureTracker_Release_VelocityOverride()
    {
        var tracker = new SwipeGestureTracker(0);

        // Small pan but high velocity
        tracker.OnPan(-5d, leadingWidth: 0d, trailingWidth: 72d);
        var target = tracker.OnRelease(-300d, leadingWidth: 0d, trailingWidth: 72d);

        Assert.Equal(-72d, target); // Velocity override opens it
    }

    [Fact]
    public void SwipeGestureTracker_Reset()
    {
        var tracker = new SwipeGestureTracker(5);

        tracker.OnPan(-50d, leadingWidth: 0d, trailingWidth: 72d);
        tracker.Reset();

        Assert.Equal(0d, tracker.CurrentOffset);
        Assert.Equal(5, tracker.ItemIndex);
    }
}

public class InteractionArbiterTests
{
    [Fact]
    public void InitialState_NoGesture()
    {
        var arbiter = new InteractionArbiter();

        Assert.Equal(GestureKind.None, arbiter.ActiveGesture);
        Assert.False(arbiter.IsLocked);
    }

    [Fact]
    public void BelowThreshold_ReturnsNone()
    {
        var arbiter = new InteractionArbiter { LockThreshold = 10d };

        var result = arbiter.OnPanUpdate(3d, 2d);

        Assert.Equal(GestureKind.None, result);
        Assert.False(arbiter.IsLocked);
    }

    [Fact]
    public void VerticalScroll_VerticalDominant_LocksToScroll()
    {
        var arbiter = new InteractionArbiter
        {
            ScrollOrientation = LayoutOrientation.Vertical,
            LockThreshold = 10d
        };

        var result = arbiter.OnPanUpdate(2d, 15d);

        Assert.Equal(GestureKind.Scroll, result);
        Assert.True(arbiter.IsLocked);
    }

    [Fact]
    public void VerticalScroll_HorizontalDominant_LocksToSwipe()
    {
        var arbiter = new InteractionArbiter
        {
            ScrollOrientation = LayoutOrientation.Vertical,
            LockThreshold = 10d
        };

        var result = arbiter.OnPanUpdate(15d, 2d);

        Assert.Equal(GestureKind.Swipe, result);
        Assert.True(arbiter.IsLocked);
    }

    [Fact]
    public void OnceLocked_SubsequentUpdatesReturnSameGesture()
    {
        var arbiter = new InteractionArbiter { LockThreshold = 10d };

        arbiter.OnPanUpdate(2d, 15d); // Lock to scroll

        var result = arbiter.OnPanUpdate(20d, 0d); // Would be swipe, but locked

        Assert.Equal(GestureKind.Scroll, result);
    }

    [Fact]
    public void OnPanEnd_Resets()
    {
        var arbiter = new InteractionArbiter { LockThreshold = 10d };

        arbiter.OnPanUpdate(2d, 15d);
        arbiter.OnPanEnd();

        Assert.Equal(GestureKind.None, arbiter.ActiveGesture);
        Assert.False(arbiter.IsLocked);
    }

    [Fact]
    public void ForceGesture_LocksImmediately()
    {
        var arbiter = new InteractionArbiter();

        arbiter.ForceGesture(GestureKind.Drag);

        Assert.Equal(GestureKind.Drag, arbiter.ActiveGesture);
        Assert.True(arbiter.IsLocked);
    }
}
