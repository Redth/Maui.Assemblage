using Maui.Assemblage.Core.Data;

namespace Maui.Assemblage.Sample;

public partial class StickyHeadersPage : ContentPage
{
    public StickyHeadersPage()
    {
        InitializeComponent();

        var sections = Enumerable.Range(0, 8)
            .Select(section =>
            {
                var letter = (char)('A' + section);
                var items = Enumerable.Range(1, 24)
                    .Select(i => (object?)$"{letter} Item {i}")
                    .ToArray();
                return new GroupSection($"Section {letter}", items);
            })
            .ToList();

        StickyList.DataSource = new GroupedCollectionDataSource(sections);
    }

    private void OnStickyToggled(object? sender, ToggledEventArgs e)
        => StickyList.StickyHeaders = e.Value;

    private void OnSpacingChanged(object? sender, ValueChangedEventArgs e)
        => StickyList.Spacing = e.NewValue;
}
