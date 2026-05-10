using Maui.Assemblage.Core.Collections;
using Maui.Assemblage.Core.Data;
using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Sample;

public partial class SpanningHeaderGridPage : ContentPage
{
    public SpanningHeaderGridPage()
    {
        InitializeComponent();

        var categories = new Dictionary<string, string[]>
        {
            ["Fruits"] = ["🍎 Apple", "🍌 Banana", "🍒 Cherry", "🍇 Grape", "🥝 Kiwi", "🍋 Lemon"],
            ["Vegetables"] = ["🥕 Carrot", "🥦 Broccoli", "🌽 Corn", "🫑 Pepper", "🥒 Cucumber"],
            ["Drinks"] = ["☕ Coffee", "🍵 Tea", "🥤 Juice", "🧋 Boba", "🍺 Beer", "🍷 Wine", "🥛 Milk"],
        };

        var sections = categories.Select(kvp =>
            new GroupSection(kvp.Key, kvp.Value.Cast<object?>().ToArray())).ToList();

        var dataSource = new GroupedCollectionDataSource(sections);
        SpanningGrid.DataSource = dataSource;

        // Build the spanning grid layout: section headers span full width, items in 3-column grid
        var engine = SpanningGrid.Engine;
        var nodes = engine.Nodes;

        SpanningGrid.LayoutProvider = new SpanningGridLayoutProvider(
            spanCount: 3,
            itemHeight: 80,
            spanningItemHeight: 40,
            horizontalSpacing: 8,
            verticalSpacing: 8,
            isSpanningItem: index =>
            {
                if (index < 0 || index >= engine.Nodes.Count)
                    return false;
                return engine.Nodes[index].Kind == CollectionNodeKind.SectionHeader;
            });
    }
}
