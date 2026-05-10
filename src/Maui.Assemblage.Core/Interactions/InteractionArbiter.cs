namespace Maui.Assemblage.Core.Interactions;

/// <summary>
/// The active gesture being tracked.
/// </summary>
public enum GestureKind
{
    /// <summary>No gesture is active.</summary>
    None,
    /// <summary>Vertical or horizontal scrolling.</summary>
    Scroll,
    /// <summary>Horizontal swipe-to-reveal on an item.</summary>
    Swipe,
    /// <summary>Long press / drag (future).</summary>
    Drag
}

/// <summary>
/// Resolves conflicts between competing gesture types using directional locking.
/// Once a gesture is locked, competing gestures are suppressed until release.
/// </summary>
public sealed class InteractionArbiter
{
    private double _accumulatedX;
    private double _accumulatedY;
    private bool _isLocked;

    /// <summary>
    /// Minimum distance in points before a gesture direction is locked.
    /// </summary>
    public double LockThreshold { get; init; } = 10d;

    /// <summary>
    /// The currently active gesture kind.
    /// </summary>
    public GestureKind ActiveGesture { get; private set; } = GestureKind.None;

    /// <summary>
    /// Whether a gesture direction has been locked.
    /// </summary>
    public bool IsLocked => _isLocked;

    /// <summary>
    /// The scroll orientation used for conflict resolution.
    /// Vertical scroll → horizontal swipe; Horizontal scroll → vertical swipe.
    /// </summary>
    public Layout.LayoutOrientation ScrollOrientation { get; init; } = Layout.LayoutOrientation.Vertical;

    /// <summary>
    /// Feed a touch/pan delta to determine which gesture should be active.
    /// Returns the gesture that should receive the delta.
    /// </summary>
    /// <param name="deltaX">Horizontal movement delta.</param>
    /// <param name="deltaY">Vertical movement delta.</param>
    /// <returns>The gesture kind that should handle this delta.</returns>
    public GestureKind OnPanUpdate(double deltaX, double deltaY)
    {
        if (_isLocked)
        {
            return ActiveGesture;
        }

        _accumulatedX += deltaX;
        _accumulatedY += deltaY;

        var absX = Math.Abs(_accumulatedX);
        var absY = Math.Abs(_accumulatedY);

        // Check if we've exceeded the lock threshold
        if (absX < LockThreshold && absY < LockThreshold)
        {
            return GestureKind.None;
        }

        // Determine dominant direction
        _isLocked = true;

        if (ScrollOrientation == Layout.LayoutOrientation.Vertical)
        {
            // Vertical scroll layout: vertical movement = scroll, horizontal = swipe
            ActiveGesture = absY >= absX ? GestureKind.Scroll : GestureKind.Swipe;
        }
        else
        {
            // Horizontal scroll layout: horizontal movement = scroll, vertical = swipe
            ActiveGesture = absX >= absY ? GestureKind.Scroll : GestureKind.Swipe;
        }

        return ActiveGesture;
    }

    /// <summary>
    /// Called when the touch/pan gesture ends. Resets the arbiter for the next gesture.
    /// </summary>
    public void OnPanEnd()
    {
        _accumulatedX = 0d;
        _accumulatedY = 0d;
        _isLocked = false;
        ActiveGesture = GestureKind.None;
    }

    /// <summary>
    /// Force-locks a specific gesture kind (e.g., for programmatic scroll).
    /// </summary>
    public void ForceGesture(GestureKind kind)
    {
        _isLocked = true;
        ActiveGesture = kind;
    }
}
