using Maui.Assemblage.Core.Realization;

namespace Maui.Assemblage.Core.Tests;

public class RecyclePoolTests
{
    [Fact]
    public void TryRent_ReusesReturnedViews()
    {
        var pool = new RecyclePool<string, object>();
        var first = new object();
        var second = new object();

        pool.Return("item", first);
        pool.Return("item", second);

        var foundSecond = pool.TryRent("item", out var rentedSecond);
        var foundFirst = pool.TryRent("item", out var rentedFirst);
        var foundNone = pool.TryRent("item", out _);

        Assert.True(foundSecond);
        Assert.Same(second, rentedSecond);
        Assert.True(foundFirst);
        Assert.Same(first, rentedFirst);
        Assert.False(foundNone);
        Assert.Equal(0, pool.Count);
    }

    [Fact]
    public void Return_RespectsMaxPerBucket()
    {
        var pool = new RecyclePool<string, object>(maxPerBucket: 2);

        var a = new object();
        var b = new object();
        var c = new object();

        Assert.True(pool.Return("item", a));
        Assert.True(pool.Return("item", b));
        Assert.False(pool.Return("item", c)); // over limit, discarded

        Assert.Equal(2, pool.Count);
        Assert.Equal(2, pool.CountFor("item"));
    }

    [Fact]
    public void Return_UnlimitedWhenMaxIsZero()
    {
        var pool = new RecyclePool<string, object>(maxPerBucket: 0);

        for (var i = 0; i < 100; i++)
        {
            pool.Return("item", new object());
        }

        Assert.Equal(100, pool.Count);
    }

    [Fact]
    public void MaxPerBucket_CanBeChangedAtRuntime()
    {
        var pool = new RecyclePool<string, object>(maxPerBucket: 10);
        Assert.Equal(10, pool.MaxPerBucket);

        pool.MaxPerBucket = 3;
        Assert.Equal(3, pool.MaxPerBucket);

        // Negative clamps to 0 (unlimited)
        pool.MaxPerBucket = -1;
        Assert.Equal(0, pool.MaxPerBucket);
    }

    [Fact]
    public void Return_DifferentBucketsHaveSeparateLimits()
    {
        var pool = new RecyclePool<string, object>(maxPerBucket: 1);

        Assert.True(pool.Return("item", new object()));
        Assert.True(pool.Return("header", new object()));
        Assert.False(pool.Return("item", new object())); // item bucket full

        Assert.Equal(2, pool.Count);
        Assert.Equal(1, pool.CountFor("item"));
        Assert.Equal(1, pool.CountFor("header"));
    }

    [Fact]
    public void Clear_RemovesAllPooledViews()
    {
        var pool = new RecyclePool<string, object>();
        pool.Return("item", new object());
        pool.Return("header", new object());

        pool.Clear();

        Assert.Equal(0, pool.Count);
        Assert.False(pool.TryRent("item", out _));
        Assert.False(pool.TryRent("header", out _));
    }
}
