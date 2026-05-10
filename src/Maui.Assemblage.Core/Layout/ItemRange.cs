namespace Maui.Assemblage.Core.Layout;

public readonly record struct ItemRange(int Start, int EndExclusive)
{
    public static ItemRange Empty => new(0, 0);

    public bool IsEmpty => EndExclusive <= Start;

    public int Length => IsEmpty ? 0 : EndExclusive - Start;

    public bool Contains(int index) => index >= Start && index < EndExclusive;

    public ItemRange Clamp(int minInclusive, int maxExclusive)
    {
        var start = Math.Max(minInclusive, Start);
        var end = Math.Min(maxExclusive, EndExclusive);
        return end <= start ? new ItemRange(start, start) : new ItemRange(start, end);
    }
}
