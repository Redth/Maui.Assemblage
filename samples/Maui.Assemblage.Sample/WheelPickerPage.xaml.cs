namespace Maui.Assemblage.Sample;

public partial class WheelPickerPage : ContentPage
{
    private CountryItem[] _countries = null!;
    private int _lastSelectedIndex = -1;

    public WheelPickerPage()
    {
        InitializeComponent();

        _countries =
        [
            new CountryItem("🇺🇸", "United States", "+1"),
            new CountryItem("🇬🇧", "United Kingdom", "+44"),
            new CountryItem("🇨🇦", "Canada", "+1"),
            new CountryItem("🇦🇺", "Australia", "+61"),
            new CountryItem("🇩🇪", "Germany", "+49"),
            new CountryItem("🇫🇷", "France", "+33"),
            new CountryItem("🇯🇵", "Japan", "+81"),
            new CountryItem("🇰🇷", "South Korea", "+82"),
            new CountryItem("🇧🇷", "Brazil", "+55"),
            new CountryItem("🇮🇳", "India", "+91"),
            new CountryItem("🇲🇽", "Mexico", "+52"),
            new CountryItem("🇮🇹", "Italy", "+39"),
            new CountryItem("🇪🇸", "Spain", "+34"),
            new CountryItem("🇳🇱", "Netherlands", "+31"),
            new CountryItem("🇸🇪", "Sweden", "+46"),
            new CountryItem("🇳🇴", "Norway", "+47"),
            new CountryItem("🇨🇭", "Switzerland", "+41"),
            new CountryItem("🇵🇱", "Poland", "+48"),
            new CountryItem("🇦🇷", "Argentina", "+54"),
            new CountryItem("🇿🇦", "South Africa", "+27"),
            new CountryItem("🇳🇿", "New Zealand", "+64"),
            new CountryItem("🇸🇬", "Singapore", "+65"),
            new CountryItem("🇮🇪", "Ireland", "+353"),
            new CountryItem("🇵🇹", "Portugal", "+351"),
            new CountryItem("🇹🇷", "Turkey", "+90"),
        ];

        WheelView.ItemsSource = _countries;
        WheelView.Scrolled += OnWheelScrolled;

        // Scroll to a middle item so items appear above and below center
        const int startIndex = 7; // South Korea
        Loaded += async (_, _) =>
        {
            await Task.Delay(100);
            WheelView.ScrollToItem(startIndex, false);
            UpdateSelection(startIndex);
        };
    }

    private void OnWheelScrolled(object? sender, ScrolledEventArgs e)
    {
        var index = (int)Math.Round(e.ScrollY / 48.0);
        index = Math.Clamp(index, 0, _countries.Length - 1);
        UpdateSelection(index);
    }

    private void UpdateSelection(int index)
    {
        if (index == _lastSelectedIndex) return;
        _lastSelectedIndex = index;

        var c = _countries[index];
        SelectedFlag.Text = c.Flag;
        SelectedLabel.Text = c.Name;
        SelectedCode.Text = c.Code;
    }
}

public record CountryItem(string Flag, string Name, string Code);
