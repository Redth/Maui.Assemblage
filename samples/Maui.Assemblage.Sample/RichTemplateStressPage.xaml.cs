using System.Diagnostics;

namespace Maui.Assemblage.Sample;

public enum RichStressKind
{
    Article,
    Hero,
    Metric,
    Checklist,
    Event,
    Profile,
    Review,
    Alert
}

public sealed class RichStressItem
{
    public int Index { get; init; }
    public RichStressKind Kind { get; init; }
    public double EstimatedHeight { get; init; }
    public string HeightLabel => $"#{Index:N0} · {KindLabel} · fallback {EstimatedHeight:0}dp";
    public string KindLabel { get; init; } = "";
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";
    public string Icon { get; init; } = "";
    public string Initials { get; init; } = "";
    public string Value { get; init; } = "";
    public string Meta1 { get; init; } = "";
    public string Meta2 { get; init; } = "";
    public string Meta3 { get; init; } = "";
    public string Metric1 { get; init; } = "";
    public string Metric2 { get; init; } = "";
    public string Metric3 { get; init; } = "";
    public string Detail1 { get; init; } = "";
    public string Detail2 { get; init; } = "";
    public string Detail3 { get; init; } = "";
    public string Progress1Text { get; init; } = "";
    public double Progress1 { get; init; }
    public double Progress2 { get; init; }
    public double Progress3 { get; init; }
    public double SliderValue { get; init; }
    public bool Flag1 { get; init; }
    public bool Flag2 { get; init; }
    public bool Flag3 { get; init; }
    public Color AccentColor { get; init; } = Colors.Gray;
    public Color SoftAccentColor { get; init; } = Color.FromArgb("#F4F4F5");
}

public sealed class RichStressTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ArticleTemplate { get; set; }
    public DataTemplate? HeroTemplate { get; set; }
    public DataTemplate? MetricTemplate { get; set; }
    public DataTemplate? ChecklistTemplate { get; set; }
    public DataTemplate? EventTemplate { get; set; }
    public DataTemplate? ProfileTemplate { get; set; }
    public DataTemplate? ReviewTemplate { get; set; }
    public DataTemplate? AlertTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is not RichStressItem stressItem)
        {
            return ArticleTemplate!;
        }

        return stressItem.Kind switch
        {
            RichStressKind.Hero => HeroTemplate!,
            RichStressKind.Metric => MetricTemplate!,
            RichStressKind.Checklist => ChecklistTemplate!,
            RichStressKind.Event => EventTemplate!,
            RichStressKind.Profile => ProfileTemplate!,
            RichStressKind.Review => ReviewTemplate!,
            RichStressKind.Alert => AlertTemplate!,
            _ => ArticleTemplate!,
        };
    }
}

public partial class RichTemplateStressPage : ContentPage
{
    private List<RichStressItem> _items = [];

    public RichTemplateStressPage()
    {
        InitializeComponent();

        RichList.ItemExtentResolver = index =>
            index >= 0 && index < _items.Count ? _items[index].EstimatedHeight : 180d;

        LoadItems(5_000);
    }

    private async void LoadItems(int count)
    {
        StatusLabel.Text = $"Generating {count:N0}...";

        var sw = Stopwatch.StartNew();
        var items = await Task.Run(() => GenerateItems(count));
        var genMs = sw.ElapsedMilliseconds;

        sw.Restart();
        _items = items;
        RichList.ItemsSource = _items;
        sw.Stop();

        StatusLabel.Text = $"{count:N0} items · gen:{genMs}ms bind:{sw.ElapsedMilliseconds}ms";
    }

    private void OnLoad1K(object? sender, EventArgs e) => LoadItems(1_000);

    private void OnLoad5K(object? sender, EventArgs e) => LoadItems(5_000);

    private void OnLoad20K(object? sender, EventArgs e) => LoadItems(20_000);

    private void OnScrollTop(object? sender, EventArgs e)
    {
        RichList.ScrollToStart();
        StatusLabel.Text = $"{_items.Count:N0} items · scrolled top";
    }

    private void OnScrollMiddle(object? sender, EventArgs e)
    {
        if (_items.Count == 0)
        {
            return;
        }

        var index = _items.Count / 2;
        RichList.ScrollToItem(index);
        StatusLabel.Text = $"{_items.Count:N0} items · scrolled #{index:N0}";
    }

    private void OnScrollEnd(object? sender, EventArgs e)
    {
        RichList.ScrollToEnd();
        StatusLabel.Text = $"{_items.Count:N0} items · scrolled end";
    }

    private void OnItemButtonClicked(object? sender, EventArgs e)
    {
        SetInteractionStatus(sender, "button clicked");
    }

    private void OnItemCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        SetInteractionStatus(sender, e.Value ? "checked" : "unchecked");
    }

    private void OnItemToggled(object? sender, ToggledEventArgs e)
    {
        SetInteractionStatus(sender, e.Value ? "switch on" : "switch off");
    }

    private void OnItemSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        SetInteractionStatus(sender, $"slider {e.NewValue:0}");
    }

    private void SetInteractionStatus(object? sender, string action)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not RichStressItem item)
        {
            return;
        }

        StatusLabel.Text = $"{item.KindLabel} #{item.Index:N0} · {action}";
    }

    private static List<RichStressItem> GenerateItems(int count)
    {
        var rng = new Random(8675309);
        var titles = new[]
        {
            "Viewport recycling with nested controls",
            "Mixed template selection under fast scroll",
            "Large-offset jump with variable extents",
            "Template pool reuse and binding churn",
            "Adaptive UI card with measured content",
            "High-density diagnostics panel",
            "Generated dashboard entry",
            "Long-form content block"
        };
        var bodies = new[]
        {
            "This item intentionally combines labels, controls, progress indicators, nested borders, and wrapping text to exercise template creation and recycling.",
            "A longer body appears here so some rows need extra height while nearby rows stay compact. The content is deterministic to keep test runs repeatable.",
            "Scrolling through thousands of these records should keep realization bounded, maintain item identity, and avoid clipping when templates change shape.",
            "The sample avoids network images and external assets; all visual weight comes from MAUI controls, colors, and generated data.",
            "Use the top, middle, and end buttons to force large offset changes while the list is using per-index item extents."
        };
        var colors = new[]
        {
            ("#6750A4", "#F1ECFF"),
            ("#006C51", "#E7F6F0"),
            ("#B3261E", "#FCEEEE"),
            ("#006493", "#E8F5FC"),
            ("#984061", "#FBEFF4"),
            ("#5D6B00", "#F2F6DE"),
            ("#7D5260", "#F8EEF2"),
            ("#D35400", "#FFF1E6")
        };
        var icons = new[] { "🧪", "📊", "✅", "📅", "👤", "🧵", "⚠️", "🚀" };
        var locations = new[] { "Conference Room A", "Remote", "Build Lab", "Design Studio", "Device Farm", "Ops Bridge" };
        var names = new[] { "Alex Morgan", "Sam Rivera", "Jordan Kim", "Taylor Chen", "Casey Brooks", "Riley Singh" };
        var tags = new[] { "Android", "iOS", "WinUI", "MAUI", "Virtualized", "Template", "Perf", "A11y" };

        var items = new List<RichStressItem>(count);
        for (var i = 0; i < count; i++)
        {
            var kind = (RichStressKind)(i % 8);
            var (accent, softAccent) = colors[(i + rng.Next(colors.Length)) % colors.Length];
            var progress1 = Math.Round(0.15 + rng.NextDouble() * 0.8, 2);
            var progress2 = Math.Round(0.10 + rng.NextDouble() * 0.75, 2);
            var progress3 = Math.Round(0.05 + rng.NextDouble() * 0.7, 2);
            var body = bodies[(i + rng.Next(bodies.Length)) % bodies.Length];
            if (i % 5 == 0)
            {
                body = $"{body} {bodies[(i + 1) % bodies.Length]}";
            }

            var estimatedHeight = GetEstimatedHeight(kind, body.Length, i);
            var name = names[(i + rng.Next(names.Length)) % names.Length];
            var title = $"{titles[(i + rng.Next(titles.Length)) % titles.Length]} {i:N0}";

            items.Add(new RichStressItem
            {
                Index = i,
                Kind = kind,
                KindLabel = kind.ToString(),
                EstimatedHeight = estimatedHeight,
                Title = title,
                Body = body,
                Icon = icons[(i + (int)kind) % icons.Length],
                Initials = GetInitials(name),
                Value = GetValue(kind, i, rng),
                Meta1 = GetMeta1(kind, i, rng),
                Meta2 = tags[(i + 2) % tags.Length],
                Meta3 = tags[(i + 5) % tags.Length],
                Metric1 = "CPU",
                Metric2 = "GPU",
                Metric3 = "Memory",
                Detail1 = GetDetail1(kind, i, rng, locations),
                Detail2 = GetDetail2(kind, i, rng),
                Detail3 = GetDetail3(kind, i, rng),
                Progress1 = progress1,
                Progress2 = progress2,
                Progress3 = progress3,
                Progress1Text = $"{progress1:P0}",
                SliderValue = Math.Round(progress1 * 100d),
                Flag1 = i % 2 == 0,
                Flag2 = i % 3 == 0,
                Flag3 = i % 5 == 0,
                AccentColor = Color.FromArgb(accent),
                SoftAccentColor = Color.FromArgb(softAccent)
            });
        }

        return items;
    }

    private static double GetEstimatedHeight(RichStressKind kind, int bodyLength, int index)
    {
        var bodyLines = Math.Clamp((int)Math.Ceiling(bodyLength / 42d), 1, 5);
        var bodyBonus = bodyLines * 16d;
        var variation = (index % 4) * 10d;

        return kind switch
        {
            RichStressKind.Article => 240d + bodyBonus + variation,
            RichStressKind.Hero => 360d + Math.Min(bodyBonus, 48d) + variation,
            RichStressKind.Metric => 300d + variation,
            RichStressKind.Checklist => 330d + variation,
            RichStressKind.Event => 280d + Math.Min(bodyBonus, 40d) + variation,
            RichStressKind.Profile => 290d + Math.Min(bodyBonus, 40d) + variation,
            RichStressKind.Review => 320d + Math.Min(bodyBonus, 48d) + variation,
            RichStressKind.Alert => 260d + Math.Min(bodyBonus, 64d) + variation,
            _ => 240d
        };
    }

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}"
            : name[..Math.Min(2, name.Length)].ToUpperInvariant();
    }

    private static string GetValue(RichStressKind kind, int index, Random rng)
    {
        return kind switch
        {
            RichStressKind.Metric => $"{rng.Next(20, 99)}%",
            RichStressKind.Checklist => $"{rng.Next(1, 4)}/3",
            RichStressKind.Event => $"{(index % 28) + 1}",
            RichStressKind.Review => rng.NextDouble() > 0.4 ? "Ready" : "Needs work",
            RichStressKind.Alert => rng.NextDouble() > 0.5 ? "Active" : "Muted",
            _ => $"#{index:N0}"
        };
    }

    private static string GetMeta1(RichStressKind kind, int index, Random rng)
    {
        return kind switch
        {
            RichStressKind.Event => new[] { "JAN", "FEB", "MAR", "APR", "MAY", "JUN" }[index % 6],
            RichStressKind.Profile => $"@{new[] { "alex", "sam", "jordan", "taylor", "casey", "riley" }[index % 6]}{rng.Next(10, 99)}",
            RichStressKind.Review => $"src/Stress/{index % 24}/Template.cs",
            RichStressKind.Hero => $"generated scene {index:N0}",
            _ => $"{rng.Next(1, 48)}m ago"
        };
    }

    private static string GetDetail1(RichStressKind kind, int index, Random rng, string[] locations)
    {
        return kind switch
        {
            RichStressKind.Checklist => "Validate visible range after fast scroll",
            RichStressKind.Event => locations[(index + rng.Next(locations.Length)) % locations.Length],
            RichStressKind.Profile => $"{rng.Next(6, 42)} repos",
            RichStressKind.Review => $"+ public void RenderTemplate{index % 17}()",
            _ => "Primary detail with generated content"
        };
    }

    private static string GetDetail2(RichStressKind kind, int index, Random rng)
    {
        return kind switch
        {
            RichStressKind.Checklist => "Recycle controls without stale bindings",
            RichStressKind.Event => $"{rng.Next(4, 64)} attendees",
            RichStressKind.Profile => $"{rng.Next(10, 400)} followers",
            RichStressKind.Review => "- old layout cache path",
            _ => $"Secondary detail {index % 13}"
        };
    }

    private static string GetDetail3(RichStressKind kind, int index, Random rng)
    {
        return kind switch
        {
            RichStressKind.Checklist => "Confirm row height estimate covers content",
            RichStressKind.Profile => $"{rng.Next(1, 9)} teams",
            RichStressKind.Review => "// variable template stress case",
            _ => $"Tertiary detail {index % 17}"
        };
    }
}
