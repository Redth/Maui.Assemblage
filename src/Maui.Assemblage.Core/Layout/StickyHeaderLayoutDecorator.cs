using Maui.Assemblage.Core.Collections;

namespace Maui.Assemblage.Core.Layout;

/// <summary>
/// Decorates a layout snapshot by pinning section header items to the viewport's leading edge
/// when the section has scrolled past but is still partially visible.
/// </summary>
public static class StickyHeaderLayoutDecorator
{
    /// <summary>
    /// Adjusts section header positions in the snapshot to stick at the top of the viewport.
    /// </summary>
    /// <param name="snapshot">The original layout snapshot.</param>
    /// <param name="nodes">The flattened collection nodes (to identify section headers).</param>
    /// <param name="scrollOffset">Current scroll offset in the primary direction.</param>
    /// <param name="viewportSize">Viewport extent in the primary direction.</param>
    /// <param name="orientation">Layout orientation.</param>
    /// <returns>A new snapshot with adjusted header positions.</returns>
    public static LayoutSnapshot Apply(
        LayoutSnapshot snapshot,
        IReadOnlyList<CollectionNode> nodes,
        double scrollOffset,
        double viewportSize,
        LayoutOrientation orientation = LayoutOrientation.Vertical)
    {
        if (snapshot.Items.Count == 0 || nodes.Count == 0)
        {
            return snapshot;
        }

        // Build a map of flat index -> node kind for quick lookup
        var headerIndices = new HashSet<int>();
        var sectionStartIndices = new Dictionary<int, int>(); // section -> flat index of header
        var sectionEndOffsets = new Dictionary<int, double>(); // section -> end offset of last item

        for (var i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].Kind == CollectionNodeKind.SectionHeader)
            {
                headerIndices.Add(i);
                sectionStartIndices[nodes[i].Section] = i;
            }
        }

        if (headerIndices.Count == 0)
        {
            return snapshot;
        }

        // Find the end offset of each section's last item
        var attrByIndex = new Dictionary<int, LayoutItemAttributes>();
        foreach (var attr in snapshot.Items)
        {
            attrByIndex[attr.Index] = attr;
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (attrByIndex.TryGetValue(i, out var attr))
            {
                var itemEnd = orientation == LayoutOrientation.Vertical
                    ? attr.Frame.Y + attr.Frame.Height
                    : attr.Frame.X + attr.Frame.Width;

                if (!sectionEndOffsets.ContainsKey(node.Section) || itemEnd > sectionEndOffsets[node.Section])
                {
                    sectionEndOffsets[node.Section] = itemEnd;
                }
            }
        }

        // Adjust header positions
        var adjustedItems = new List<LayoutItemAttributes>(snapshot.Items.Count);

        foreach (var attr in snapshot.Items)
        {
            if (!headerIndices.Contains(attr.Index))
            {
                adjustedItems.Add(attr);
                continue;
            }

            var node = nodes[attr.Index];
            var headerExtent = orientation == LayoutOrientation.Vertical
                ? attr.Frame.Height
                : attr.Frame.Width;

            var originalLeading = orientation == LayoutOrientation.Vertical
                ? attr.Frame.Y
                : attr.Frame.X;

            // The header should stick at the scroll offset (top of viewport)
            // but not go past the end of its section minus its own height
            var sectionEnd = sectionEndOffsets.TryGetValue(node.Section, out var end)
                ? end
                : originalLeading + headerExtent;

            var maxStickyOffset = sectionEnd - headerExtent;
            var stickyOffset = Math.Clamp(scrollOffset, originalLeading, maxStickyOffset);

            if (stickyOffset <= originalLeading)
            {
                // Header hasn't scrolled past viewport top yet - keep original
                adjustedItems.Add(attr);
            }
            else
            {
                // Pin the header at the adjusted offset with elevated z-index
                var adjustedFrame = orientation == LayoutOrientation.Vertical
                    ? attr.Frame with { Y = stickyOffset }
                    : attr.Frame with { X = stickyOffset };

                adjustedItems.Add(attr with { Frame = adjustedFrame, ZIndex = 1000 + node.Section });
            }
        }

        return new LayoutSnapshot(snapshot.ContentWidth, snapshot.ContentHeight, adjustedItems);
    }
}
