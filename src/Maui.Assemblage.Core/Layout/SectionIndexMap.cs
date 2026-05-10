using Maui.Assemblage.Core.Collections;
using Maui.Assemblage.Core.Data;

namespace Maui.Assemblage.Core.Layout;

public sealed class SectionIndexMap
{
    private readonly IReadOnlyList<CollectionNode> _nodes;
    private readonly Dictionary<(int Section, int Index), int>? _itemToFlat;
    private readonly Dictionary<(CollectionNodeKind Kind, int Section), int> _supplementaryToFlat = [];
    private readonly int _singleSectionOffset; // optimization: for 1 section, flat = offset + index

    public SectionIndexMap(IReadOnlyList<CollectionNode> flattenedNodes)
    {
        _nodes = flattenedNodes;

        // Detect single-section flat list for O(1) lookup path
        var sectionCount = 0;
        var firstItemFlat = -1;
        for (var flat = 0; flat < _nodes.Count; flat++)
        {
            var node = _nodes[flat];
            if (node.Kind == CollectionNodeKind.Item)
            {
                if (firstItemFlat < 0) firstItemFlat = flat;
                if (node.Section > sectionCount) sectionCount = node.Section;
            }
            else
            {
                _supplementaryToFlat[(node.Kind, node.Section)] = flat;
            }
        }

        if (sectionCount == 0 && firstItemFlat >= 0)
        {
            // Single section — use arithmetic lookup instead of dictionary
            _singleSectionOffset = firstItemFlat;
            _itemToFlat = null;
        }
        else
        {
            // Multiple sections — build full dictionary
            _singleSectionOffset = -1;
            _itemToFlat = new Dictionary<(int, int), int>();
            for (var flat = 0; flat < _nodes.Count; flat++)
            {
                var node = _nodes[flat];
                if (node.Kind == CollectionNodeKind.Item)
                {
                    _itemToFlat[(node.Section, node.Index)] = flat;
                }
            }
        }
    }

    public int Count => _nodes.Count;

    public int GetFlatIndex(int section, int index)
    {
        if (TryGetFlatIndex(section, index, out var flat))
            return flat;
        throw new ArgumentOutOfRangeException($"No flat index for section {section}, index {index}.");
    }

    public bool TryGetFlatIndex(int section, int index, out int flatIndex)
    {
        if (_itemToFlat is null)
        {
            // Fast path: single-section arithmetic — must be section 0
            if (section != 0)
            {
                flatIndex = -1;
                return false;
            }
            flatIndex = _singleSectionOffset + index;
            return (uint)flatIndex < (uint)_nodes.Count
                && _nodes[flatIndex].Kind == CollectionNodeKind.Item;
        }
        return _itemToFlat.TryGetValue((section, index), out flatIndex);
    }

    public int GetSupplementaryFlatIndex(CollectionNodeKind kind, int section)
    {
        if (_supplementaryToFlat.TryGetValue((kind, section), out var flat))
        {
            return flat;
        }

        throw new ArgumentOutOfRangeException($"No flat index for {kind} in section {section}.");
    }

    public bool TryGetSupplementaryFlatIndex(CollectionNodeKind kind, int section, out int flatIndex)
        => _supplementaryToFlat.TryGetValue((kind, section), out flatIndex);

    public CollectionNode GetNode(int flatIndex) => _nodes[flatIndex];
}
