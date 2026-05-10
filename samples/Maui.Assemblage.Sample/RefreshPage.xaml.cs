using System.Collections.ObjectModel;

namespace Maui.Assemblage.Sample;

public partial class RefreshPage : ContentPage
{
	private readonly ObservableCollection<string> _items = [];
	private int _refreshCount;
	private bool _isLoading;

	public RefreshPage()
	{
		InitializeComponent();

		RefreshList.ItemsSource = _items;
		RefreshList.Refreshing += OnRefreshing;
		UpdateInfo();
	}

	private void OnRefreshBtnClicked(object? sender, EventArgs e)
	{
		if (!RefreshList.IsRefreshing)
		{
			RefreshList.IsRefreshing = true;
		}
	}

	private async void OnRefreshing(object? sender, EventArgs e)
	{
		if (_isLoading)
		{
			return;
		}

		_isLoading = true;

		try
		{
			// Simulate async data fetch
			await Task.Delay(1500);

			_refreshCount++;
			for (var i = 0; i < 5; i++)
			{
				_items.Insert(0, $"Refresh #{_refreshCount} — Item {i}");
			}

			UpdateInfo();
		}
		finally
		{
			RefreshList.IsRefreshing = false;
			_isLoading = false;
		}
	}

	private void UpdateInfo()
	{
		RefreshInfo.Text = $"{_items.Count} items · Pull or tap Refresh";
	}
}
