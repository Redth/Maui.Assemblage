using System.Collections.ObjectModel;

namespace Maui.Assemblage.Sample;

public partial class SelectionPage : ContentPage
{
	private readonly ObservableCollection<string> _items = [];
	private bool _isMulti;

	public SelectionPage()
	{
		InitializeComponent();

		for (var i = 0; i < 30; i++)
		{
			_items.Add($"Item {i}");
		}

		SelectionList.ItemsSource = _items;
		SelectionList.SelectionChanged += OnSelectionChanged;

		// Pre-select a couple items to demonstrate visual feedback
		SelectionList.Engine.Selection.Select(_items[2]);
		UpdateInfo();
	}

	private void OnSelectionChanged(object? sender, Maui.Assemblage.Core.Interactions.SelectionChangedEventArgs e)
	{
		UpdateInfo();
	}

	private void OnToggleModeClicked(object? sender, EventArgs e)
	{
		_isMulti = !_isMulti;
		SelectionList.SelectionMode = _isMulti
			? Maui.Assemblage.Core.Interactions.SelectionMode.Multiple
			: Maui.Assemblage.Core.Interactions.SelectionMode.Single;
		ModeBtn.Text = _isMulti ? "Single" : "Multi";
		UpdateInfo();
	}

	private void OnClearClicked(object? sender, EventArgs e)
	{
		SelectionList.Engine.Selection.ClearSelection();
		UpdateInfo();
	}

	private void UpdateInfo()
	{
		var mode = _isMulti ? "Multi" : "Single";
		var count = SelectionList.SelectedItems.Count;
		InfoLabel.Text = $"{mode} · {count} selected";
	}
}
