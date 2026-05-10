using Maui.Assemblage.Core.Data;
using SelectionMode = Maui.Assemblage.Core.Interactions.SelectionMode;

namespace Maui.Assemblage.Sample;

public partial class ContactsPage : ContentPage
{
    private SelectionMode _currentMode = SelectionMode.Single;

    public ContactsPage()
    {
        InitializeComponent();

        ContactList.SectionHeaderTemplate = new DataTemplate(() =>
        {
            var border = new Border
            {
                BackgroundColor = Color.FromArgb("#6750A4"),
                Padding = new Thickness(16, 8),
                StrokeThickness = 0,
            };
            var label = new Label
            {
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
            };
            label.SetBinding(Label.TextProperty, ".");
            border.Content = label;
            return border;
        });

        ContactList.ItemTemplate = new DataTemplate(() =>
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(new GridLength(56)),
                    new ColumnDefinition(GridLength.Star),
                },
                ColumnSpacing = 12,
                Padding = new Thickness(16, 8),
            };

            var avatar = new Border
            {
                WidthRequest = 44,
                HeightRequest = 44,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 22 },
                BackgroundColor = Color.FromArgb("#E8DEF8"),
                StrokeThickness = 0,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            };
            var avatarLabel = new Label
            {
                FontSize = 20,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            };
            avatarLabel.SetBinding(Label.TextProperty, "Avatar");
            avatar.Content = avatarLabel;

            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            var name = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold };
            name.SetBinding(Label.TextProperty, "Name");
            var dept = new Label { FontSize = 12, TextColor = Color.FromArgb("#888888") };
            dept.SetBinding(Label.TextProperty, "Department");
            var email = new Label { FontSize = 11, TextColor = Color.FromArgb("#AAAAAA") };
            email.SetBinding(Label.TextProperty, "Email");
            info.Children.Add(name);
            info.Children.Add(dept);
            info.Children.Add(email);

            grid.Add(avatar, 0, 0);
            grid.Add(info, 1, 0);
            return grid;
        });

        ContactList.SelectionMode = SelectionMode.Single;

        var groups = SampleData.GenerateGroupedContacts(60);
        var totalContacts = groups.Sum(g => g.Count);
        CountLabel.Text = $"{totalContacts} contacts · {groups.Count} depts";

        var sections = groups.Select(g =>
            new GroupSection(g.First().Department, g.Cast<object?>().ToList())).ToList();

        ContactList.ItemsSource = new GroupedCollectionDataSource(sections);
    }

    private void OnStickyToggled(object? sender, ToggledEventArgs e)
    {
        ContactList.StickyHeaders = e.Value;
    }

    private void OnSelectionModeClicked(object? sender, EventArgs e)
    {
        _currentMode = _currentMode switch
        {
            SelectionMode.None => SelectionMode.Single,
            SelectionMode.Single => SelectionMode.Multiple,
            _ => SelectionMode.None,
        };
        ContactList.SelectionMode = _currentMode;
        SelectionBtn.Text = _currentMode.ToString();
    }
}
