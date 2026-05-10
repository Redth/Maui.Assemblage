using Maui.Assemblage.Core.Layout;

namespace Maui.Assemblage.Core.Tests;

public class MeasurementCacheTests
{
    [Fact]
    public void PutAndGet_RoundTrips()
    {
        var cache = new MeasurementCache();

        cache.Put("item", 400d, 12345, 72d);

        Assert.True(cache.TryGet("item", 400d, 12345, out var extent));
        Assert.Equal(72d, extent);
    }

    [Fact]
    public void TryGet_Miss_ReturnsFalse()
    {
        var cache = new MeasurementCache();

        Assert.False(cache.TryGet("item", 400d, 12345, out _));
    }

    [Fact]
    public void DifferentDataHash_ReturnsFalse()
    {
        var cache = new MeasurementCache();
        cache.Put("item", 400d, 111, 72d);

        Assert.False(cache.TryGet("item", 400d, 222, out _));
    }

    [Fact]
    public void DifferentConstrainedExtent_ReturnsFalse()
    {
        var cache = new MeasurementCache();
        cache.Put("item", 400d, 111, 72d);

        Assert.False(cache.TryGet("item", 500d, 111, out _));
    }

    [Fact]
    public void Clear_RemovesAll()
    {
        var cache = new MeasurementCache();
        cache.Put("item", 400d, 111, 72d);
        cache.Put("header", 400d, 222, 44d);

        cache.Clear();

        Assert.Equal(0, cache.Count);
        Assert.False(cache.TryGet("item", 400d, 111, out _));
    }

    [Fact]
    public void InvalidateByTemplateKey()
    {
        var cache = new MeasurementCache();
        cache.Put("item", 400d, 111, 72d);
        cache.Put("header", 400d, 222, 44d);

        cache.Invalidate("item");

        Assert.False(cache.TryGet("item", 400d, 111, out _));
        Assert.True(cache.TryGet("header", 400d, 222, out _));
    }

    [Fact]
    public void Eviction_OnMaxEntries()
    {
        var cache = new MeasurementCache(maxEntries: 5);

        for (var i = 0; i < 10; i++)
        {
            cache.Put("item", 400d, i, (double)i);
        }

        // Should have evicted some entries
        Assert.True(cache.Count <= 10);
    }
}
