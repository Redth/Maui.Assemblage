using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Maui.Assemblage.Core.Layout;
using Maui.Assemblage.Core.Realization;

BenchmarkSwitcher.FromAssembly(typeof(LayoutBenchmarks).Assembly).Run(args);

[MemoryDiagnoser]
[ShortRunJob]
public class LayoutBenchmarks
{
    private LinearLayoutProvider _linear = null!;
    private GridLayoutProvider _grid = null!;
    private CarouselLayoutProvider _carousel = null!;
    private AdaptiveGridLayoutProvider _adaptive = null!;

    [Params(100, 1_000, 10_000, 100_000)]
    public int ItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _linear = new LinearLayoutProvider(48d, 4d);
        _grid = new GridLayoutProvider(3, 80d, 8d, 8d);
        _carousel = new CarouselLayoutProvider(30d, 12d);
        _adaptive = new AdaptiveGridLayoutProvider(100d, 80d, 8d, 8d);
    }

    [Benchmark(Baseline = true)]
    public LayoutSnapshot Linear_Arrange()
    {
        var context = new LayoutContext(ItemCount, 400d, 800d);
        return _linear.Arrange(context, new ItemRange(0, Math.Min(ItemCount, 50)));
    }

    [Benchmark]
    public LayoutSnapshot Grid_Arrange()
    {
        var context = new LayoutContext(ItemCount, 400d, 800d);
        return _grid.Arrange(context, new ItemRange(0, Math.Min(ItemCount, 50)));
    }

    [Benchmark]
    public LayoutSnapshot Carousel_Arrange()
    {
        var context = new LayoutContext(ItemCount, 400d, 800d);
        return _carousel.Arrange(context, new ItemRange(0, Math.Min(ItemCount, 20)));
    }

    [Benchmark]
    public LayoutSnapshot AdaptiveGrid_Arrange()
    {
        var context = new LayoutContext(ItemCount, 400d, 800d);
        return _adaptive.Arrange(context, new ItemRange(0, Math.Min(ItemCount, 50)));
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class RealizationBenchmarks
{
    private WindowedRealizationStrategy _strategy = null!;
    private LayoutSnapshot _snapshot = null!;

    [Params(50, 200, 1000)]
    public int VisibleItems { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _strategy = new WindowedRealizationStrategy();
        var provider = new LinearLayoutProvider(48d, 4d);
        var context = new LayoutContext(100_000, 400d, 800d);
        _snapshot = provider.Arrange(context, new ItemRange(0, VisibleItems));
    }

    [Benchmark]
    public IReadOnlyList<RealizationEntry> Realize()
    {
        return _strategy.Realize(_snapshot, new ItemRange(0, VisibleItems), _ => "default");
    }

    [Benchmark]
    public IReadOnlyList<int> GetRecyclable()
    {
        var realized = new HashSet<int>(Enumerable.Range(0, VisibleItems * 2));
        return _strategy.GetRecyclableIndices(realized, new ItemRange(0, VisibleItems));
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class RecyclePoolBenchmarks
{
    [Params(100, 1000)]
    public int Operations { get; set; }

    [Benchmark]
    public void ReturnAndRent()
    {
        var pool = new RecyclePool<string, object>();

        for (var i = 0; i < Operations; i++)
        {
            pool.Return("key", new object());
        }

        for (var i = 0; i < Operations; i++)
        {
            pool.TryRent("key", out _);
        }
    }
}
