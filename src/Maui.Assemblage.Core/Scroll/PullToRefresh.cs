namespace Maui.Assemblage.Core.Scroll;

/// <summary>
/// Describes the current state of a pull-to-refresh gesture.
/// </summary>
public enum PullToRefreshState
{
    Idle,
    Pulling,
    Armed,
    Refreshing
}

/// <summary>
/// Manages pull-to-refresh state transitions.
/// Platform adapters feed overscroll deltas; the handler fires refresh events.
/// </summary>
public sealed class PullToRefreshHandler
{
    private PullToRefreshState _state = PullToRefreshState.Idle;
    private double _pullDistance;

    public PullToRefreshHandler(double threshold = 80d)
    {
        if (threshold <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold));
        }

        Threshold = threshold;
    }

    public double Threshold { get; }
    public bool IsEnabled { get; set; } = true;
    public PullToRefreshState State => _state;
    public double PullDistance => _pullDistance;
    public double Progress => Threshold > 0d ? Math.Clamp(_pullDistance / Threshold, 0d, 1d) : 0d;

    public event EventHandler? RefreshRequested;
    public event EventHandler<PullToRefreshState>? StateChanged;

    public void OnPull(double delta)
    {
        if (!IsEnabled || _state == PullToRefreshState.Refreshing)
        {
            return;
        }

        _pullDistance = Math.Max(0d, _pullDistance + delta);

        var newState = _pullDistance >= Threshold
            ? PullToRefreshState.Armed
            : PullToRefreshState.Pulling;

        TransitionTo(newState);
    }

    public void OnRelease()
    {
        if (!IsEnabled)
        {
            return;
        }

        if (_state == PullToRefreshState.Armed)
        {
            TransitionTo(PullToRefreshState.Refreshing);
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (_state != PullToRefreshState.Refreshing)
        {
            _pullDistance = 0d;
            TransitionTo(PullToRefreshState.Idle);
        }
    }

    public void EndRefreshing()
    {
        _pullDistance = 0d;
        TransitionTo(PullToRefreshState.Idle);
    }

    private void TransitionTo(PullToRefreshState newState)
    {
        if (_state == newState)
        {
            return;
        }

        _state = newState;
        StateChanged?.Invoke(this, newState);
    }
}
