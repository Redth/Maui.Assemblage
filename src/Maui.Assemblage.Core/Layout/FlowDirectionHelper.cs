namespace Maui.Assemblage.Core.Layout;

/// <summary>
/// Specifies the text/layout flow direction.
/// </summary>
public enum FlowDirection
{
    LeftToRight,
    RightToLeft
}

/// <summary>
/// Provides RTL-aware layout transformations.
/// </summary>
public static class FlowDirectionHelper
{
    /// <summary>
    /// Mirrors a layout snapshot horizontally for RTL flow direction.
    /// </summary>
    /// <param name="snapshot">The original LTR snapshot.</param>
    /// <param name="flowDirection">The flow direction to apply.</param>
    /// <returns>A mirrored snapshot for RTL, or the original for LTR.</returns>
    public static LayoutSnapshot Apply(LayoutSnapshot snapshot, FlowDirection flowDirection)
    {
        if (flowDirection == FlowDirection.LeftToRight)
        {
            return snapshot;
        }

        var mirrored = new List<LayoutItemAttributes>(snapshot.Items.Count);

        foreach (var attr in snapshot.Items)
        {
            var mirroredX = snapshot.ContentWidth - attr.Frame.X - attr.Frame.Width;
            var mirroredFrame = attr.Frame with { X = mirroredX };
            mirrored.Add(attr with { Frame = mirroredFrame });
        }

        return new LayoutSnapshot(snapshot.ContentWidth, snapshot.ContentHeight, mirrored);
    }
}

/// <summary>
/// Computes keyboard/focus traversal order for collection items.
/// </summary>
public static class FocusTraversalHelper
{
    /// <summary>
    /// Gets the next focusable index when navigating in the given direction.
    /// </summary>
    /// <param name="currentIndex">Currently focused flat index.</param>
    /// <param name="direction">Navigation direction.</param>
    /// <param name="snapshot">Current layout snapshot.</param>
    /// <param name="flowDirection">Flow direction for horizontal navigation.</param>
    /// <returns>The next index to focus, or -1 if no valid target exists.</returns>
    public static int GetNextFocusIndex(
        int currentIndex,
        FocusDirection direction,
        LayoutSnapshot snapshot,
        FlowDirection flowDirection = FlowDirection.LeftToRight)
    {
        if (snapshot.Items.Count == 0)
        {
            return -1;
        }

        // Build position lookup
        var attrByIndex = new Dictionary<int, LayoutItemAttributes>();
        foreach (var attr in snapshot.Items)
        {
            attrByIndex[attr.Index] = attr;
        }

        if (!attrByIndex.TryGetValue(currentIndex, out var current))
        {
            return snapshot.Items.Count > 0 ? snapshot.Items[0].Index : -1;
        }

        var effectiveDirection = direction;
        if (flowDirection == FlowDirection.RightToLeft)
        {
            effectiveDirection = direction switch
            {
                FocusDirection.Left => FocusDirection.Right,
                FocusDirection.Right => FocusDirection.Left,
                _ => direction
            };
        }

        // Find the nearest item in the requested direction
        var bestIndex = -1;
        var bestDistance = double.MaxValue;

        foreach (var attr in snapshot.Items)
        {
            if (attr.Index == currentIndex)
            {
                continue;
            }

            var isValidDirection = effectiveDirection switch
            {
                FocusDirection.Up => attr.Frame.Y + attr.Frame.Height <= current.Frame.Y + 1d,
                FocusDirection.Down => attr.Frame.Y >= current.Frame.Y + current.Frame.Height - 1d,
                FocusDirection.Left => attr.Frame.X + attr.Frame.Width <= current.Frame.X + 1d,
                FocusDirection.Right => attr.Frame.X >= current.Frame.X + current.Frame.Width - 1d,
                _ => false
            };

            if (!isValidDirection)
            {
                continue;
            }

            var dx = (attr.Frame.X + attr.Frame.Width / 2d) - (current.Frame.X + current.Frame.Width / 2d);
            var dy = (attr.Frame.Y + attr.Frame.Height / 2d) - (current.Frame.Y + current.Frame.Height / 2d);
            var distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = attr.Index;
            }
        }

        return bestIndex;
    }
}

/// <summary>
/// Direction for keyboard/focus navigation.
/// </summary>
public enum FocusDirection
{
    Up,
    Down,
    Left,
    Right
}
