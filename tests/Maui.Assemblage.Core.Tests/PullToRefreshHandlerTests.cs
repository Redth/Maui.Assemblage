using Maui.Assemblage.Core.Scroll;

namespace Maui.Assemblage.Core.Tests;

public class PullToRefreshHandlerTests
{
    [Fact]
    public void InitialState_IsIdle()
    {
        var handler = new PullToRefreshHandler();

        Assert.Equal(PullToRefreshState.Idle, handler.State);
        Assert.Equal(0d, handler.PullDistance);
        Assert.Equal(0d, handler.Progress);
    }

    [Fact]
    public void OnPull_BelowThreshold_TransitionsToPulling()
    {
        var handler = new PullToRefreshHandler(threshold: 100d);
        PullToRefreshState? lastState = null;
        handler.StateChanged += (_, s) => lastState = s;

        handler.OnPull(50d);

        Assert.Equal(PullToRefreshState.Pulling, handler.State);
        Assert.Equal(50d, handler.PullDistance);
        Assert.Equal(0.5d, handler.Progress, 3);
        Assert.Equal(PullToRefreshState.Pulling, lastState);
    }

    [Fact]
    public void OnPull_ExceedsThreshold_TransitionsToArmed()
    {
        var handler = new PullToRefreshHandler(threshold: 80d);

        handler.OnPull(90d);

        Assert.Equal(PullToRefreshState.Armed, handler.State);
        Assert.Equal(1d, handler.Progress);
    }

    [Fact]
    public void OnRelease_WhenArmed_TriggersRefresh()
    {
        var handler = new PullToRefreshHandler(threshold: 80d);
        var refreshed = false;
        handler.RefreshRequested += (_, _) => refreshed = true;

        handler.OnPull(100d);
        handler.OnRelease();

        Assert.True(refreshed);
        Assert.Equal(PullToRefreshState.Refreshing, handler.State);
    }

    [Fact]
    public void OnRelease_WhenPulling_ResetsToIdle()
    {
        var handler = new PullToRefreshHandler(threshold: 80d);
        var refreshed = false;
        handler.RefreshRequested += (_, _) => refreshed = true;

        handler.OnPull(30d);
        handler.OnRelease();

        Assert.False(refreshed);
        Assert.Equal(PullToRefreshState.Idle, handler.State);
        Assert.Equal(0d, handler.PullDistance);
    }

    [Fact]
    public void EndRefreshing_ResetsToIdle()
    {
        var handler = new PullToRefreshHandler(threshold: 80d);

        handler.OnPull(100d);
        handler.OnRelease();
        Assert.Equal(PullToRefreshState.Refreshing, handler.State);

        handler.EndRefreshing();

        Assert.Equal(PullToRefreshState.Idle, handler.State);
        Assert.Equal(0d, handler.PullDistance);
    }

    [Fact]
    public void OnPull_WhileRefreshing_IsIgnored()
    {
        var handler = new PullToRefreshHandler(threshold: 80d);

        handler.OnPull(100d);
        handler.OnRelease();
        handler.OnPull(50d);

        Assert.Equal(PullToRefreshState.Refreshing, handler.State);
    }

    [Fact]
    public void IsEnabled_False_IgnoresPulls()
    {
        var handler = new PullToRefreshHandler(threshold: 80d);
        handler.IsEnabled = false;

        handler.OnPull(100d);

        Assert.Equal(PullToRefreshState.Idle, handler.State);
        Assert.Equal(0d, handler.PullDistance);
    }

    [Fact]
    public void Constructor_ZeroThreshold_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PullToRefreshHandler(0d));
    }
}
