namespace Maui.Assemblage.Core.Realization;

public sealed class RecyclePool<TKey, TView>
    where TKey : notnull
    where TView : class
{
    private readonly Dictionary<TKey, Stack<TView>> _buckets = [];
    private int _maxPerBucket;

    /// <param name="maxPerBucket">
    /// Maximum pooled views per template bucket. Views beyond this limit are discarded
    /// on <see cref="Return"/> to prevent unbounded memory growth. 0 = unlimited.
    /// </param>
    public RecyclePool(int maxPerBucket = 0)
    {
        _maxPerBucket = Math.Max(0, maxPerBucket);
    }

    public int Count { get; private set; }

    /// <summary>Maximum pooled views per bucket. 0 = unlimited.</summary>
    public int MaxPerBucket
    {
        get => _maxPerBucket;
        set => _maxPerBucket = Math.Max(0, value);
    }

    /// <summary>
    /// Returns a view to the pool. If the bucket already has <see cref="MaxPerBucket"/> views,
    /// the view is discarded and this method returns false.
    /// </summary>
    public bool Return(TKey key, TView view)
    {
        if (!_buckets.TryGetValue(key, out var stack))
        {
            stack = new Stack<TView>();
            _buckets[key] = stack;
        }

        if (_maxPerBucket > 0 && stack.Count >= _maxPerBucket)
        {
            return false; // discard — pool is full for this bucket
        }

        stack.Push(view);
        Count++;
        return true;
    }

    public bool TryRent(TKey key, out TView? view)
    {
        if (_buckets.TryGetValue(key, out var stack) && stack.Count > 0)
        {
            view = stack.Pop();
            Count--;
            return true;
        }

        view = default;
        return false;
    }

    public int CountFor(TKey key) => _buckets.TryGetValue(key, out var stack) ? stack.Count : 0;

    /// <summary>Clears all pooled views.</summary>
    public void Clear()
    {
        _buckets.Clear();
        Count = 0;
    }
}
