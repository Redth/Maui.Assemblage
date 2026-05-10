using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Sample;

public partial class HorizontalListPage : ContentPage
{
    public HorizontalListPage()
    {
        InitializeComponent();

        var items = new[]
        {
            new HCardItem("Home", "🏠", Color.FromArgb("#6750A4")),
            new HCardItem("Work", "💼", Color.FromArgb("#7D5260")),
            new HCardItem("Travel", "✈️", Color.FromArgb("#006C51")),
            new HCardItem("Food", "🍕", Color.FromArgb("#006493")),
            new HCardItem("Music", "🎵", Color.FromArgb("#984061")),
            new HCardItem("Sport", "⚽", Color.FromArgb("#5C6BC0")),
            new HCardItem("Books", "📚", Color.FromArgb("#00897B")),
            new HCardItem("Games", "🎮", Color.FromArgb("#E65100")),
            new HCardItem("Photos", "📷", Color.FromArgb("#6750A4")),
            new HCardItem("Movies", "🎬", Color.FromArgb("#7D5260")),
        };

        HList.ItemsSource = items;

        // Horizontal grid: 2 rows, scrolls horizontally
        var gridItems = Enumerable.Range(1, 20)
            .Select(i => new HCardItem($"Item {i}", "", Color.FromArgb(i % 2 == 0 ? "#6750A4" : "#006C51")))
            .ToList();

        HGrid.LayoutProvider = new GridLayoutProvider(
            spanCount: 2,
            itemHeight: 70,
            horizontalSpacing: 8,
            verticalSpacing: 8,
            orientation: LayoutOrientation.Horizontal);
        HGrid.ScrollDirection = ScrollOrientation.Horizontal;
        HGrid.ItemsSource = gridItems;
    }
}

public record HCardItem(string Name, string Icon, Color Color);
