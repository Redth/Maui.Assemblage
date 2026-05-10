namespace Maui.Assemblage.Sample;

public class ChatTemplateSelector : DataTemplateSelector
{
    public DataTemplate? SentTemplate { get; set; }
    public DataTemplate? ReceivedTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        return item is ChatMessage msg && msg.IsMe ? SentTemplate! : ReceivedTemplate!;
    }
}

public partial class TemplateSelectorPage : ContentPage
{
    public TemplateSelectorPage()
    {
        InitializeComponent();

        var messages = SampleData.GenerateChatMessages(40);
        CountLabel.Text = $"{messages.Count} messages";
        ChatList.ItemsSource = messages;
    }
}
