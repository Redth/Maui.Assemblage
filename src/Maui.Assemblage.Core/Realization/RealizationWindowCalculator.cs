using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Realization;

public static class RealizationWindowCalculator
{
    public static ItemRange Calculate(
        double offset,
        double viewportSize,
        int itemCount,
        double itemExtent,
        double spacing,
        int cacheBefore,
        int cacheAfter)
    {
        if (itemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount));
        }

        if (itemExtent <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent));
        }

        if (spacing < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(spacing));
        }

        if (cacheBefore < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cacheBefore));
        }

        if (cacheAfter < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cacheAfter));
        }

        if (itemCount == 0)
        {
            return ItemRange.Empty;
        }

        var pitch = itemExtent + spacing;
        var safeOffset = Math.Max(0d, offset);
        var safeViewport = Math.Max(0d, viewportSize);
        var firstVisible = (int)Math.Floor(safeOffset / pitch);
        var endEdge = safeOffset + safeViewport;
        var lastVisible = (int)Math.Floor(Math.Max(safeOffset, endEdge - double.Epsilon) / pitch);
        var start = Math.Max(0, firstVisible - cacheBefore);
        var end = Math.Min(itemCount, lastVisible + 1 + cacheAfter);
        return new ItemRange(start, end);
    }
}
