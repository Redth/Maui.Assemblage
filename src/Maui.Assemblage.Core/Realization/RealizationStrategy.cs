using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Realization;

public readonly record struct RealizationEntry(
    int FlatIndex,
    LayoutItemAttributes Attributes,
    string TemplateKey);

public interface IRealizationStrategy
{
    IReadOnlyList<RealizationEntry> Realize(
        LayoutSnapshot snapshot,
        ItemRange visibleRange,
        Func<int, string> templateKeySelector);

    IReadOnlyList<int> GetRecyclableIndices(
        IReadOnlySet<int> currentlyRealized,
        ItemRange keepRange);
}

public sealed class WindowedRealizationStrategy : IRealizationStrategy
{
    public IReadOnlyList<RealizationEntry> Realize(
        LayoutSnapshot snapshot,
        ItemRange visibleRange,
        Func<int, string> templateKeySelector)
    {
        var entries = new List<RealizationEntry>();

        foreach (var attr in snapshot.Items)
        {
            if (visibleRange.Contains(attr.Index))
            {
                var key = templateKeySelector(attr.Index);
                entries.Add(new RealizationEntry(attr.Index, attr, key));
            }
        }

        return entries;
    }

    public IReadOnlyList<int> GetRecyclableIndices(
        IReadOnlySet<int> currentlyRealized,
        ItemRange keepRange)
    {
        var recyclable = new List<int>();

        foreach (var index in currentlyRealized)
        {
            if (!keepRange.Contains(index))
            {
                recyclable.Add(index);
            }
        }

        return recyclable;
    }
}
