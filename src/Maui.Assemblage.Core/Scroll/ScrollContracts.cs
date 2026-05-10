namespace Maui.Assemblage.Core.Scroll;

/// <summary>
/// Describes a programmatic scroll request.
/// </summary>
public sealed class ScrollRequest
{
    private ScrollRequest(ScrollRequestKind kind, double offset, int section, int index, bool animated)
    {
        Kind = kind;
        Offset = offset;
        Section = section;
        Index = index;
        Animated = animated;
    }

    public ScrollRequestKind Kind { get; }
    public double Offset { get; }
    public int Section { get; }
    public int Index { get; }
    public bool Animated { get; }

    public static ScrollRequest ToOffset(double offset, bool animated = true)
        => new(ScrollRequestKind.Offset, offset, 0, 0, animated);

    public static ScrollRequest ToItem(int section, int index, bool animated = true)
        => new(ScrollRequestKind.Item, 0d, section, index, animated);

    public static ScrollRequest ToStart(bool animated = true)
        => new(ScrollRequestKind.Start, 0d, 0, 0, animated);

    public static ScrollRequest ToEnd(bool animated = true)
        => new(ScrollRequestKind.End, 0d, 0, 0, animated);
}

public enum ScrollRequestKind
{
    Offset,
    Item,
    Start,
    End
}

/// <summary>
/// Provides scroll control to the host view.
/// Platform adapters implement this to bridge native scroll containers.
/// </summary>
public interface IScrollController
{
    double CurrentOffset { get; }
    double ViewportSize { get; }
    void ScrollTo(double offset, bool animated);
    event EventHandler<ScrollChangedEventArgs>? ScrollChanged;
}

public sealed class ScrollChangedEventArgs : EventArgs
{
    public ScrollChangedEventArgs(double offset, double velocity)
    {
        Offset = offset;
        Velocity = velocity;
    }

    public double Offset { get; }
    public double Velocity { get; }
}

/// <summary>
/// Resolves a <see cref="ScrollRequest"/> into a concrete offset using the layout provider.
/// </summary>
public static class ScrollRequestResolver
{
    public static double Resolve(
        ScrollRequest request,
        Layout.ILayoutProvider layoutProvider,
        Layout.LayoutContext context,
        Layout.SectionIndexMap? indexMap)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(layoutProvider);

        return request.Kind switch
        {
            ScrollRequestKind.Start => 0d,
            ScrollRequestKind.End => ResolveEnd(layoutProvider, context),
            ScrollRequestKind.Offset => Math.Max(0d, request.Offset),
            ScrollRequestKind.Item => ResolveItem(request, layoutProvider, context, indexMap),
            _ => 0d
        };
    }

    private static double ResolveEnd(Layout.ILayoutProvider layoutProvider, Layout.LayoutContext context)
    {
        var snapshot = layoutProvider.Arrange(context, new Layout.ItemRange(0, context.ItemCount));
        var contentExtent = snapshot.ContentHeight;
        var viewportExtent = context.ViewportHeight;
        return Math.Max(0d, contentExtent - viewportExtent);
    }

    private static double ResolveItem(
        ScrollRequest request,
        Layout.ILayoutProvider layoutProvider,
        Layout.LayoutContext context,
        Layout.SectionIndexMap? indexMap)
    {
        var flatIndex = indexMap is not null
            ? indexMap.GetFlatIndex(request.Section, request.Index)
            : request.Index;

        if (flatIndex < 0 || flatIndex >= context.ItemCount)
        {
            return 0d;
        }

        var range = new Layout.ItemRange(flatIndex, flatIndex + 1);
        var snapshot = layoutProvider.Arrange(context, range);

        if (snapshot.Items.Count == 0)
        {
            return 0d;
        }

        var frame = snapshot.Items[0].Frame;
        return Math.Max(frame.X, frame.Y);
    }
}
