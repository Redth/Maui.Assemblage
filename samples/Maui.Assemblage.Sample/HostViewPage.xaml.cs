using System.Collections.ObjectModel;

namespace Maui.Assemblage.Sample;

public partial class HostViewPage : ContentPage
{
	private readonly ObservableCollection<string> _items = [];
	private bool _isGrid;

	public HostViewPage()
	{
		InitializeComponent();

		for (var i = 0; i < 50; i++)
		{
			_items.Add($"Item {i}");
		}

		HostView.ItemsSource = _items;
		UpdateInfo();
	}

	private void OnAddClicked(object? sender, EventArgs e)
	{
		var start = _items.Count;
		for (var i = 0; i < 10; i++)
		{
			_items.Add($"Item {start + i}");
		}

		UpdateInfo();
	}

	private void OnRemoveClicked(object? sender, EventArgs e)
	{
		for (var i = 0; i < 10 && _items.Count > 0; i++)
		{
			_items.RemoveAt(_items.Count - 1);
		}

		UpdateInfo();
	}

	private void OnToggleClicked(object? sender, EventArgs e)
	{
		_isGrid = !_isGrid;

		if (_isGrid)
		{
			HostView.LayoutProvider = new Maui.Assemblage.Core.Layout.GridLayoutProvider(
				spanCount: 3,
				itemHeight: 80,
				horizontalSpacing: 8,
				verticalSpacing: 8);
			ToggleBtn.Text = "List";
		}
		else
		{
			HostView.LayoutProvider = new Maui.Assemblage.Core.Layout.LinearLayoutProvider(
				itemExtent: 52,
				spacing: 4);
			ToggleBtn.Text = "Grid";
		}

		UpdateInfo();
	}

	private void UpdateInfo()
	{
		var mode = _isGrid ? "Grid" : "List";
		InfoLabel.Text = $"{mode} · {_items.Count} items";
	}
}
