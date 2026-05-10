using System.Diagnostics;

namespace Maui.Assemblage.Sample;

public partial class StressTestPage : ContentPage
{
    private string _currentLayout = "List";

    public StressTestPage()
    {
        InitializeComponent();

        StressList.ItemTemplate = new DataTemplate(() =>
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(new GridLength(44)),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(new GridLength(60)),
                },
                ColumnSpacing = 12,
                Padding = new Thickness(16, 6),
            };

            var idx = new Label
            {
                FontSize = 11,
                TextColor = Color.FromArgb("#AAAAAA"),
                HorizontalTextAlignment = TextAlignment.End,
                VerticalTextAlignment = TextAlignment.Center,
            };
            idx.SetBinding(Label.TextProperty, "Index");

            var name = new Label
            {
                FontSize = 15,
                VerticalTextAlignment = TextAlignment.Center,
            };
            name.SetBinding(Label.TextProperty, "Name");

            var value = new Label
            {
                FontSize = 13,
                TextColor = Color.FromArgb("#6750A4"),
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.End,
                VerticalTextAlignment = TextAlignment.Center,
            };
            value.SetBinding(Label.TextProperty, "Value");

            grid.Add(idx, 0, 0);
            grid.Add(name, 1, 0);
            grid.Add(value, 2, 0);
            return grid;
        });

        LoadItems(1000);
    }

    private async void LoadItems(int count)
    {
        StatusLabel.Text = $"Generating {count:N0} items…";

        var sw = Stopwatch.StartNew();
        var items = await Task.Run(() =>
        {
            var rng = new Random(42);
            return Enumerable.Range(0, count).Select(i =>
                new StressItem($"#{i}", $"Item {i}", $"${rng.Next(1, 9999):N0}")).ToList();
        });
        var genTime = sw.ElapsedMilliseconds;

        sw.Restart();
        StressList.ItemsSource = items;
        sw.Stop();

        StatusLabel.Text = $"{count:N0} items · gen:{genTime}ms bind:{sw.ElapsedMilliseconds}ms";
    }

    private void OnLoad1K(object? sender, EventArgs e) => LoadItems(1_000);
    private void OnLoad10K(object? sender, EventArgs e) => LoadItems(10_000);
    private void OnLoad50K(object? sender, EventArgs e) => LoadItems(50_000);

    private void OnLayoutToggle(object? sender, EventArgs e)
    {
        _currentLayout = _currentLayout switch
        {
            "List" => "Grid",
            _ => "List",
        };
        LayoutBtn.Text = _currentLayout;
        // Layout switch would require swapping out the host view type.
        // For now, just show the label change as a placeholder.
        StatusLabel.Text = $"Layout: {_currentLayout}";
    }
}

public record StressItem(string Index, string Name, string Value);
