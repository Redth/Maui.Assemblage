namespace Maui.Assemblage.Sample;

public partial class WrapLayoutPage : ContentPage
{
    public WrapLayoutPage()
    {
        InitializeComponent();

        var tags = new[]
        {
            "C#", "MAUI", ".NET", "Xamarin Forms", "iOS", "Android",
            "Blazor Hybrid", "WinUI 3", "gRPC", "SignalR", "Entity Framework Core",
            "ASP.NET Core", "Azure", "Docker", "Kubernetes", "Redis",
            "SQLite", "GraphQL", "REST API", "OAuth 2.0", "JWT", "gzip",
            "HTTP/3", "WebSocket", "MVVM", "Dependency Injection", "LINQ",
            "Reactive Extensions", "NuGet", "MSBuild", "Hot Reload",
            "AOT Compilation", "Minimal APIs", "Source Generators",
            "CommunityToolkit", "SkiaSharp", "ML.NET", "YARP", "Aspire",
        };

        var colors = new[]
        {
            "#6750A4", "#7D5260", "#006C51", "#006493",
            "#984061", "#5C6BC0", "#00897B", "#E65100",
        };

        var items = tags.Select((t, i) => new TagItem(t, Color.FromArgb(colors[i % colors.Length]),
            EstimateWidth(t))).ToList();

        WrapView.ItemWidthResolver = idx => idx < items.Count ? items[idx].Width : 80;
        WrapView.ItemsSource = items;
    }

    private static double EstimateWidth(string text)
    {
        // Approximate: ~9px per character + 32px padding for pill shape
        return Math.Max(50, text.Length * 9.0 + 32);
    }
}

public record TagItem(string Name, Color Color, double Width);
