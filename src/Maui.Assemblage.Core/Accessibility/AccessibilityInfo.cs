namespace Maui.Assemblage.Core.Accessibility;

/// <summary>
/// Accessibility role for collection items.
/// </summary>
public enum AccessibilityRole
{
    None,
    Button,
    Header,
    Image,
    ListItem,
    StaticText,
    SearchField,
    Summary
}

/// <summary>
/// Provides accessibility metadata for a collection item.
/// </summary>
public sealed record AccessibilityInfo
{
    public string? Label { get; init; }
    public string? Hint { get; init; }
    public AccessibilityRole Role { get; init; } = AccessibilityRole.ListItem;
    public int SortOrder { get; init; } = -1;
    public bool IsAccessible { get; init; } = true;
}

/// <summary>
/// Provides accessibility info for items.
/// </summary>
public interface IAccessibilityInfoProvider
{
    AccessibilityInfo? GetAccessibilityInfo(int flatIndex, object? data);
}

/// <summary>
/// Builds accessible reading order from layout snapshots.
/// </summary>
public static class AccessibilityOrderBuilder
{
    public static IReadOnlyList<int> BuildReadingOrder(
        Layout.LayoutSnapshot snapshot,
        bool isRTL = false)
    {
        return snapshot.Items
            .OrderBy(a => a.Frame.Y)
            .ThenBy(a => isRTL ? -a.Frame.X : a.Frame.X)
            .Select(a => a.Index)
            .ToList();
    }
}
