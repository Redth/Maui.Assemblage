namespace Maui.Assemblage.Sample;

public partial class CoverFlowPage : ContentPage
{
    public CoverFlowPage()
    {
        InitializeComponent();

        var albums = new[]
        {
            new AlbumItem("Midnight Drive", "Synthwave", "#E74C3C", "🎵"),
            new AlbumItem("Ocean Waves", "Ambient", "#3498DB", "🌊"),
            new AlbumItem("Forest Walk", "Nature", "#2ECC71", "🌲"),
            new AlbumItem("Golden Hour", "Chill", "#F39C12", "☀️"),
            new AlbumItem("Neon Lights", "Electronic", "#9B59B6", "💡"),
            new AlbumItem("Deep Blue", "Jazz", "#1ABC9C", "🎷"),
            new AlbumItem("Sunset Blvd", "Lo-Fi", "#E67E22", "🌅"),
            new AlbumItem("Stargazer", "Classical", "#2980B9", "⭐"),
            new AlbumItem("Rainforest", "World", "#27AE60", "🌴"),
            new AlbumItem("Purple Haze", "Rock", "#8E44AD", "🎸"),
            new AlbumItem("City Lights", "Pop", "#E74C3C", "🏙️"),
            new AlbumItem("Moonlight", "R&B", "#3498DB", "🌙"),
        };

        CoverFlowView.ItemsSource = albums;
    }

    private void OnSnapToggled(object? sender, ToggledEventArgs e)
        => CoverFlowView.SnapToCenter = e.Value;

    private void OnSpacingChanged(object? sender, ValueChangedEventArgs e)
        => CoverFlowView.ItemSpacing = e.NewValue;

    private void OnRotationChanged(object? sender, ValueChangedEventArgs e)
        => CoverFlowView.MaxRotation = e.NewValue;

    private void OnScaleChanged(object? sender, ValueChangedEventArgs e)
        => CoverFlowView.MinScale = e.NewValue;
}

public record AlbumItem(string Title, string Subtitle, string ColorHex, string Icon)
{
    public Color Color => Color.FromArgb(ColorHex);
}
