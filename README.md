# Maui.Assemblage

A high-performance, cross-platform collection controls engine for **.NET MAUI / .NET 10**.

All layout, virtualization, realization, and recycling logic lives in shared C# code — native scroll containers are only used for gesture physics, momentum, and bounce.

> [!NOTE]
> This project is in active development. APIs may change while the core engine, MAUI controls, samples, and tests are being finalized.

## Features

- **List** — vertical/horizontal fixed-height lists
- **Grid** — fixed-span and adaptive grids
- **Carousel** — paged/snap layouts with peek
- **CoverFlow** — center-distance opacity/z-index transforms
- **Grouping** — section headers, footers, global headers/footers
- **Selection** — None / Single / Multiple selection modes
- **Empty View** — template shown when the data source is empty
- **Virtualization** — windowed realization with cache-before/cache-after
- **View Recycling** — keyed pool with template-based reuse
- **Sticky Headers** — section headers pinned at the viewport leading edge
- **Pull-to-Refresh** — state machine (Idle → Pulling → Armed → Refreshing)
- **Swipe Actions** — swipe-to-reveal context actions with gesture arbitration
- **Accessibility** — per-item metadata, reading order, focus traversal, RTL support

## Packages

| Package | Description |
|---------|-------------|
| `Maui.Assemblage.Core` | Shared engine — layout, data, realization, recycling, interactions (no MAUI dependency) |
| `Maui.Assemblage` | MAUI controls — `CollectionHostView` and convenience wrappers |

## Repository Layout

| Path | Description |
|------|-------------|
| `src/Maui.Assemblage.Core` | Platform-independent collection engine |
| `src/Maui.Assemblage` | .NET MAUI controls and host views |
| `samples/Maui.Assemblage.Sample` | Sample MAUI app demonstrating layouts and interactions |
| `tests/Maui.Assemblage.Core.Tests` | Unit tests for the shared engine |
| `benchmarks/Maui.Assemblage.Benchmarks` | BenchmarkDotNet performance benchmarks |

## Quick Start

Reference the MAUI control namespace in XAML:

```xml
xmlns:ma="clr-namespace:Maui.Assemblage;assembly=Maui.Assemblage"
```

### ListHostView

```xml
<ma:ListHostView ItemsSource="{Binding Items}"
                 ItemExtent="52"
                 Spacing="4">
    <ma:ListHostView.ItemTemplate>
        <DataTemplate>
            <Border Padding="12,8" StrokeShape="RoundRectangle 8">
                <Label Text="{Binding .}" FontSize="16" />
            </Border>
        </DataTemplate>
    </ma:ListHostView.ItemTemplate>
</ma:ListHostView>
```

### GridHostView

```xml
<ma:GridHostView ItemsSource="{Binding Items}"
                 SpanCount="3"
                 ItemHeight="80"
                 HorizontalSpacing="8"
                 VerticalSpacing="8">
    <ma:GridHostView.ItemTemplate>
        <DataTemplate>
            <Border Padding="8" StrokeShape="RoundRectangle 6">
                <Label Text="{Binding .}" HorizontalTextAlignment="Center" />
            </Border>
        </DataTemplate>
    </ma:GridHostView.ItemTemplate>
</ma:GridHostView>
```

### CarouselHostView

```xml
<ma:CarouselHostView ItemsSource="{Binding Items}"
                     PeekAmount="40"
                     ItemSpacing="16">
    <ma:CarouselHostView.ItemTemplate>
        <DataTemplate>
            <Border Padding="24" StrokeShape="RoundRectangle 12">
                <Label Text="{Binding .}" FontSize="24" HorizontalTextAlignment="Center" />
            </Border>
        </DataTemplate>
    </ma:CarouselHostView.ItemTemplate>
</ma:CarouselHostView>
```

### LibraryHostView (CoverFlow)

```xml
<ma:LibraryHostView ItemsSource="{Binding Items}"
                    ItemWidth="180"
                    ItemSpacing="16"
                    MinOpacity="0.5">
    <ma:LibraryHostView.ItemTemplate>
        <DataTemplate>
            <Border Padding="16" StrokeShape="RoundRectangle 12">
                <Label Text="{Binding .}" FontSize="20" HorizontalTextAlignment="Center" />
            </Border>
        </DataTemplate>
    </ma:LibraryHostView.ItemTemplate>
</ma:LibraryHostView>
```

## Grouped Data

Use `GroupedCollectionDataSource` with the `DataSource` property:

```csharp
var sections = new List<GroupSection>
{
    new("Fruits", new object[] { "Apple", "Banana", "Cherry" }),
    new("Vegetables", new object[] { "Carrot", "Broccoli", "Spinach" }),
};

myListHostView.DataSource = new GroupedCollectionDataSource(sections);
```

```xml
<ma:ListHostView x:Name="myListHostView"
                 ItemExtent="44" Spacing="2"
                 Header="My Groceries">
    <ma:ListHostView.SectionHeaderTemplate>
        <DataTemplate>
            <Border BackgroundColor="#E8DEF8" Padding="16,8">
                <Label Text="{Binding .}" FontSize="17" FontAttributes="Bold" />
            </Border>
        </DataTemplate>
    </ma:ListHostView.SectionHeaderTemplate>
    <ma:ListHostView.ItemTemplate>
        <DataTemplate>
            <Border Padding="16,8" StrokeShape="RoundRectangle 6">
                <Label Text="{Binding .}" FontSize="15" />
            </Border>
        </DataTemplate>
    </ma:ListHostView.ItemTemplate>
</ma:ListHostView>
```

## Selection

Enable selection via the `SelectionMode` property:

```xml
<ma:ListHostView SelectionMode="Single" SelectionChanged="OnSelectionChanged" />
```

```csharp
void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
{
    foreach (var item in e.AddedItems)
        Console.WriteLine($"Selected: {item}");
}
```

Access selected items at any time via `myList.SelectedItems`.

## Empty View

Show a placeholder when the data source is empty:

```xml
<ma:ListHostView ItemsSource="{Binding Items}"
                 EmptyView="No items yet">
    <ma:ListHostView.EmptyViewTemplate>
        <DataTemplate>
            <VerticalStackLayout VerticalOptions="Center" HorizontalOptions="Center">
                <Label Text="📭" FontSize="48" HorizontalTextAlignment="Center" />
                <Label Text="{Binding .}" FontSize="18" TextColor="Gray" />
            </VerticalStackLayout>
        </DataTemplate>
    </ma:ListHostView.EmptyViewTemplate>
</ma:ListHostView>
```

## Architecture

```
┌─────────────────────────────────────────────┐
│          Public Control Layer               │
│  CollectionHostView / ListHostView / etc.   │
│  (ScrollView + AbsoluteLayout host)         │
└───────────────┬─────────────────────────────┘
                │
┌───────────────▼─────────────────────────────┐
│            CollectionEngine                 │
│  Orchestrates: data → flatten → layout →    │
│  realization → recycling                    │
├─────────────────────────────────────────────┤
│  Data Pipeline        │  Layout Pipeline    │
│  ICollectionDataSource│  ILayoutProvider    │
│  CollectionNodeFlatter│  LinearLayout       │
│  SectionIndexMap      │  GridLayout         │
│                       │  CarouselLayout     │
│                       │  CoverFlowLayout    │
├───────────────────────┼─────────────────────┤
│  Realization Pipeline │  Interaction Layer  │
│  WindowedRealization  │  SelectionTracker   │
│  RecyclePool          │  SwipeActions       │
│  MeasurementCache     │  InteractionArbiter │
├───────────────────────┴─────────────────────┤
│  Scroll Pipeline   │  Accessibility         │
│  PullToRefreshHndlr│  AccessibilityInfo     │
│  ScrollRequest     │  FocusTraversal        │
│  StickyHeaders     │  FlowDirection (RTL)   │
└─────────────────────────────────────────────┘
```

## Using CollectionHostView Directly

For advanced scenarios, use `CollectionHostView` with any `ILayoutProvider`:

```csharp
var host = new CollectionHostView
{
    ItemsSource = myItems,
    LayoutProvider = new GridLayoutProvider(spanCount: 3, itemHeight: 100),
    SelectionMode = SelectionMode.Multiple,
};
```

Switch layouts at runtime:

```csharp
// Toggle between list and grid
host.LayoutProvider = useGrid
    ? new GridLayoutProvider(3, 80, 8, 8)
    : new LinearLayoutProvider(52, 4);
```

## Building

Install the .NET 10 SDK and MAUI workload first:

```bash
dotnet workload install maui
```

```bash
# Restore and build the solution
dotnet build Maui.Assemblage.slnx

# Run tests
dotnet test tests/Maui.Assemblage.Core.Tests/

# Run sample app (Mac Catalyst)
dotnet build samples/Maui.Assemblage.Sample/ -f net10.0-maccatalyst -t:Run

# Run benchmarks
dotnet run --project benchmarks/Maui.Assemblage.Benchmarks/ -c Release
```

## Requirements

- .NET 10 SDK
- MAUI workload (`dotnet workload install maui`)

## Contributing

Keep shared behavior in `Maui.Assemblage.Core` where possible, with MAUI-specific code limited to `Maui.Assemblage` and the sample app. Please include tests for engine behavior changes and update samples when public control APIs change.

## License

MIT
