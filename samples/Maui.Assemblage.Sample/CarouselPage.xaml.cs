using Maui.Assemblage.Core.Data;
using Maui.Assemblage.Core.Layout;
using Microsoft.Maui.Controls.Shapes;

namespace Maui.Assemblage.Sample;

public partial class CarouselPage : ContentPage
{
	private const int PageCount = 8;
	private const double PeekAmount = 30d;
	private const double PageSpacing = 12d;
	private int _currentPage;

	public CarouselPage()
	{
		InitializeComponent();
	}

	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);
		RenderCarousel();
	}

	private void OnPrevClicked(object? sender, EventArgs e)
	{
		_currentPage = Math.Max(0, _currentPage - 1);
		RenderCarousel();
	}

	private void OnNextClicked(object? sender, EventArgs e)
	{
		_currentPage = Math.Min(PageCount - 1, _currentPage + 1);
		RenderCarousel();
	}

	private void RenderCarousel()
	{
		var surfaceWidth = CarouselSurface.Width > 0 ? CarouselSurface.Width : 400d;
		var surfaceHeight = CarouselSurface.Height > 0 ? CarouselSurface.Height : 300d;

		var provider = new CarouselLayoutProvider(PeekAmount, PageSpacing);
		var context = new LayoutContext(ItemCount: PageCount, ViewportWidth: surfaceWidth, ViewportHeight: surfaceHeight);
		var snapshot = provider.Arrange(context, new ItemRange(0, PageCount));

		CarouselSurface.Children.Clear();
		CarouselSurface.WidthRequest = snapshot.ContentWidth;
		CarouselSurface.HeightRequest = surfaceHeight;

		var colors = new[]
		{
			"#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4",
			"#FFEAA7", "#DDA0DD", "#98D8C8", "#F7DC6F"
		};

		foreach (var attr in snapshot.Items)
		{
			var color = Color.FromArgb(colors[attr.Index % colors.Length]);
			var isActive = attr.Index == _currentPage;

			var border = new Border
			{
				AutomationId = $"CarouselCard_{attr.Index}",
				BackgroundColor = color,
				StrokeShape = new RoundRectangle { CornerRadius = 12 },
				Stroke = Colors.Transparent,
				StrokeThickness = 0,
				Opacity = isActive ? 1d : 0.6d,
				Shadow = isActive ? new Shadow { Brush = Colors.Black, Offset = new Point(0, 2), Radius = 8, Opacity = 0.3f } : null!,
				Content = new VerticalStackLayout
				{
					VerticalOptions = LayoutOptions.Center,
					HorizontalOptions = LayoutOptions.Center,
					Children =
					{
						new Label
						{
							Text = $"Page {attr.Index}",
							FontSize = 28,
							FontAttributes = FontAttributes.Bold,
							HorizontalTextAlignment = TextAlignment.Center,
							TextColor = Colors.White
						},
						new Label
						{
							Text = isActive ? "● Active" : "○ Inactive",
							FontSize = 14,
							HorizontalTextAlignment = TextAlignment.Center,
							TextColor = Colors.White
						}
					}
				}
			};

			AbsoluteLayout.SetLayoutBounds(border,
				new Rect(attr.Frame.X, attr.Frame.Y, attr.Frame.Width, attr.Frame.Height));
			AbsoluteLayout.SetLayoutFlags(border, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
			CarouselSurface.Children.Add(border);
#if ANDROID
			border.HandlerChanged += (s, _) =>
			{
				if (s is View v && v.Handler?.PlatformView is Android.Views.View nv)
				{
					var d = nv.Resources?.DisplayMetrics?.Density ?? 2.625f;
					nv.SetCameraDistance(d * 10000);
				}
			};
#endif
		}

		CarouselInfo.Text = $"Page {_currentPage} of {PageCount}";
	}
}
