using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Sample;

public partial class StaggeredGridPage : ContentPage
{
    private readonly List<StaggeredItem> _items = [];
    private readonly Random _rng = new(42); // Seeded for consistent heights

    public StaggeredGridPage()
    {
        InitializeComponent();

        var colors = new[] { "#6750A4", "#7D5260", "#006C51", "#006493", "#984061", "#5C6BC0", "#00897B", "#E65100" };

        for (var i = 0; i < 30; i++)
        {
            var height = 80 + _rng.Next(120); // Heights from 80 to 200
            _items.Add(new StaggeredItem($"Card {i + 1}", $"{height}pt tall", Color.FromArgb(colors[i % colors.Length]), height));
        }

        StaggeredGrid.ItemHeightResolver = index =>
            index < _items.Count ? _items[index].Height : 100;
        StaggeredGrid.ItemsSource = _items;
    }
}

public record StaggeredItem(string Title, string Subtitle, Color Color, double Height);
