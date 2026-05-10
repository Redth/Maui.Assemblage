using Maui.Assemblage.Core.Collections;
using Maui.Assemblage.Core.Data;
using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Sample;

public partial class MainPage : ContentPage
{
	private int _itemCount = 50;
	private bool _useGrid;
	private const double ItemHeight = 48d;
	private const double ItemSpacing = 4d;
	private const int GridSpanCount = 3;
	private const double GridHSpacing = 6d;
	private const double GridVSpacing = 6d;
	private const double GridItemHeight = 80d;

	public MainPage()
	{
		InitializeComponent();
	}

	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);
		RenderItems();
	}

	private void OnAdd10Clicked(object? sender, EventArgs e)
	{
		_itemCount += 10;
		RenderItems();
	}

	private void OnRemove10Clicked(object? sender, EventArgs e)
	{
		_itemCount = Math.Max(0, _itemCount - 10);
		RenderItems();
	}

	private void OnToggleLayoutClicked(object? sender, EventArgs e)
	{
		_useGrid = !_useGrid;
		if (sender is Button btn)
		{
			btn.Text = _useGrid ? "List" : "Grid";
		}

		RenderItems();
	}

	private void RenderItems()
	{
		var surfaceWidth = ItemSurface.Width > 0 ? ItemSurface.Width : 300d;

		// Build grouped data source
		var groups = BuildGroupedData(_itemCount);
		var nodes = CollectionNodeFlattener.Flatten(
			groups,
			new CollectionNodeFlattenOptions { EmptyView = "No items" });

		// Choose layout provider
		ILayoutProvider layout = _useGrid
			? new GridLayoutProvider(GridSpanCount, GridItemHeight, GridHSpacing, GridVSpacing)
			: new LinearLayoutProvider(ItemHeight, ItemSpacing);

		var context = new LayoutContext(ItemCount: nodes.Count, ViewportWidth: surfaceWidth, ViewportHeight: 600d);
		var snapshot = layout.Arrange(context, new ItemRange(0, nodes.Count));

		// Render into AbsoluteLayout
		ItemSurface.Children.Clear();
		ItemSurface.HeightRequest = snapshot.ContentHeight;

		foreach (var attr in snapshot.Items)
		{
			var node = nodes[attr.Index];
			var view = CreateView(node, attr);

			AbsoluteLayout.SetLayoutBounds(view,
				new Rect(attr.Frame.X, attr.Frame.Y, attr.Frame.Width, attr.Frame.Height));
			AbsoluteLayout.SetLayoutFlags(view, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
			ItemSurface.Children.Add(view);
		}

		var layoutName = _useGrid ? "Grid" : "List";
		InfoLabel.Text = $"{layoutName} · {_itemCount} items · {nodes.Count} nodes";
	}

	private static GroupedCollectionDataSource BuildGroupedData(int totalItems)
	{
		if (totalItems == 0)
		{
			return new GroupedCollectionDataSource([]);
		}

		var sections = new List<GroupSection>();
		var sectionSize = 10;
		var remaining = totalItems;
		var sectionIndex = 0;

		while (remaining > 0)
		{
			var count = Math.Min(sectionSize, remaining);
			var start = totalItems - remaining;
			var items = Enumerable.Range(start, count)
				.Select(i => (object?)$"Item {i}")
				.ToArray();
			sections.Add(new GroupSection($"Section {sectionIndex}", items));
			remaining -= count;
			sectionIndex++;
		}

		return new GroupedCollectionDataSource(sections);
	}

	private static View CreateView(CollectionNode node, LayoutItemAttributes attr)
	{
		return node.Kind switch
		{
			CollectionNodeKind.SectionHeader => CreateSectionHeaderView(node, attr),
			CollectionNodeKind.Empty => CreateEmptyView(node),
			_ => CreateItemView(node, attr)
		};
	}

	private static Label CreateSectionHeaderView(CollectionNode node, LayoutItemAttributes attr)
	{
		return new Label
		{
			Text = $"▸ {node.Data}",
			AutomationId = $"SectionHeader_{node.Section}",
			VerticalTextAlignment = TextAlignment.Center,
			Padding = new Thickness(12, 0),
			FontAttributes = FontAttributes.Bold,
			FontSize = 14,
			BackgroundColor = Color.FromArgb("#C0C0E0"),
			TextColor = Colors.Black
		};
	}

	private static Label CreateItemView(CollectionNode node, LayoutItemAttributes attr)
	{
		return new Label
		{
			Text = $"{node.Data}",
			AutomationId = $"Item_{node.Section}_{node.Index}",
			VerticalTextAlignment = TextAlignment.Center,
			HorizontalTextAlignment = TextAlignment.Center,
			Padding = new Thickness(8, 0),
			BackgroundColor = node.Index % 2 == 0
				? Color.FromArgb("#E0E0FF")
				: Color.FromArgb("#FFE0E0"),
			TextColor = Colors.Black
		};
	}

	private static Label CreateEmptyView(CollectionNode node)
	{
		return new Label
		{
			Text = node.Data?.ToString() ?? "Empty",
			AutomationId = "EmptyView",
			HorizontalTextAlignment = TextAlignment.Center,
			VerticalTextAlignment = TextAlignment.Center,
			FontSize = 18,
			TextColor = Colors.Gray
		};
	}
}
