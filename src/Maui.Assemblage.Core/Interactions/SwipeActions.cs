namespace Maui.Assemblage.Core.Interactions;

/// <summary>
/// A single swipe action (e.g., Delete, Archive, Flag).
/// </summary>
public sealed class SwipeAction
{
    public SwipeAction(string title, string? iconName = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        Title = title;
        IconName = iconName;
    }

    public string Title { get; }
    public string? IconName { get; }

    /// <summary>
    /// Background color for the swipe action button (hex string).
    /// </summary>
    public string BackgroundColor { get; init; } = "#FF3B30";

    /// <summary>
    /// Whether this action is destructive (affects visual treatment).
    /// </summary>
    public bool IsDestructive { get; init; }
}

/// <summary>
/// A set of swipe actions for one side of an item.
/// </summary>
public sealed class SwipeActionSet
{
    public SwipeActionSet(IReadOnlyList<SwipeAction> actions)
    {
        Actions = actions ?? throw new ArgumentNullException(nameof(actions));
    }

    public IReadOnlyList<SwipeAction> Actions { get; }

    /// <summary>
    /// The total width required to display all actions.
    /// </summary>
    public double TotalWidth(double actionWidth = 72d)
        => Actions.Count * actionWidth;
}

/// <summary>
/// Provides swipe actions for items. Implement this to supply context actions per item.
/// </summary>
public interface ISwipeActionProvider
{
    /// <summary>
    /// Gets the leading (left in LTR) swipe actions for the item at the given index.
    /// Return null if no leading actions are available.
    /// </summary>
    SwipeActionSet? GetLeadingActions(int flatIndex);

    /// <summary>
    /// Gets the trailing (right in LTR) swipe actions for the item at the given index.
    /// Return null if no trailing actions are available.
    /// </summary>
    SwipeActionSet? GetTrailingActions(int flatIndex);
}

/// <summary>
/// Tracks the state of a swipe gesture on a single item.
/// </summary>
public sealed class SwipeGestureTracker
{
    private double _currentOffset;

    public int ItemIndex { get; }

    /// <summary>
    /// Current horizontal offset of the item content.
    /// Negative = swiped left (revealing trailing actions).
    /// Positive = swiped right (revealing leading actions).
    /// </summary>
    public double CurrentOffset => _currentOffset;

    /// <summary>
    /// Whether leading actions are currently revealed.
    /// </summary>
    public bool IsLeadingRevealed => _currentOffset > 0d;

    /// <summary>
    /// Whether trailing actions are currently revealed.
    /// </summary>
    public bool IsTrailingRevealed => _currentOffset < 0d;

    public SwipeGestureTracker(int itemIndex)
    {
        ItemIndex = itemIndex;
    }

    /// <summary>
    /// Updates the swipe offset based on a pan gesture delta.
    /// </summary>
    /// <param name="delta">Horizontal delta from the pan gesture.</param>
    /// <param name="leadingWidth">Maximum reveal width for leading actions (0 if none).</param>
    /// <param name="trailingWidth">Maximum reveal width for trailing actions (0 if none).</param>
    public void OnPan(double delta, double leadingWidth, double trailingWidth)
    {
        _currentOffset += delta;

        // Clamp to action widths with rubber-band overscroll (50% past limit)
        var maxLeading = leadingWidth > 0d ? leadingWidth * 1.5d : 0d;
        var maxTrailing = trailingWidth > 0d ? trailingWidth * 1.5d : 0d;

        _currentOffset = Math.Clamp(_currentOffset, -maxTrailing, maxLeading);
    }

    /// <summary>
    /// Called when the user releases the swipe gesture.
    /// Returns the target offset to animate to (snap open or closed).
    /// </summary>
    /// <param name="velocity">Release velocity (positive = rightward).</param>
    /// <param name="leadingWidth">Action width for leading side.</param>
    /// <param name="trailingWidth">Action width for trailing side.</param>
    /// <returns>The target offset to animate to.</returns>
    public double OnRelease(double velocity, double leadingWidth, double trailingWidth)
    {
        const double velocityThreshold = 200d;
        const double positionThreshold = 0.4d;

        double target;

        if (_currentOffset > 0d && leadingWidth > 0d)
        {
            // Swiping right (leading actions)
            var shouldOpen = velocity > velocityThreshold
                || _currentOffset > leadingWidth * positionThreshold;
            target = shouldOpen ? leadingWidth : 0d;
        }
        else if (_currentOffset < 0d && trailingWidth > 0d)
        {
            // Swiping left (trailing actions)
            var shouldOpen = velocity < -velocityThreshold
                || Math.Abs(_currentOffset) > trailingWidth * positionThreshold;
            target = shouldOpen ? -trailingWidth : 0d;
        }
        else
        {
            target = 0d;
        }

        _currentOffset = target;
        return target;
    }

    /// <summary>
    /// Resets the swipe to closed position.
    /// </summary>
    public void Reset()
    {
        _currentOffset = 0d;
    }
}
