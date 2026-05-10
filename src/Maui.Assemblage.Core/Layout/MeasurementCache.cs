namespace Maui.Assemblage.Core.Layout;

/// <summary>
/// Caches measured sizes for dynamically-sized items.
/// Keys are (templateKey, constrainedExtent) tuples for efficient lookup.
/// </summary>
public sealed class MeasurementCache
{
    private readonly Dictionary<MeasurementKey, double> _cache = [];
    private readonly int _maxEntries;

    public MeasurementCache(int maxEntries = 10_000)
    {
        if (maxEntries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries));
        }

        _maxEntries = maxEntries;
    }

    public int Count => _cache.Count;

    /// <summary>
    /// Tries to get a cached measurement for the given item.
    /// </summary>
    /// <param name="templateKey">The template key for the item.</param>
    /// <param name="constrainedExtent">The constrained cross-axis extent (e.g., width for vertical lists).</param>
    /// <param name="dataHash">A hash of the item data that affects measurement.</param>
    /// <param name="measuredExtent">The previously measured primary-axis extent if found.</param>
    /// <returns>True if a cached measurement was found.</returns>
    public bool TryGet(string templateKey, double constrainedExtent, int dataHash, out double measuredExtent)
    {
        var key = new MeasurementKey(templateKey, constrainedExtent, dataHash);
        return _cache.TryGetValue(key, out measuredExtent);
    }

    /// <summary>
    /// Stores a measurement in the cache.
    /// </summary>
    public void Put(string templateKey, double constrainedExtent, int dataHash, double measuredExtent)
    {
        if (_cache.Count >= _maxEntries)
        {
            Evict();
        }

        var key = new MeasurementKey(templateKey, constrainedExtent, dataHash);
        _cache[key] = measuredExtent;
    }

    /// <summary>
    /// Invalidates all cached measurements.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Invalidates cached measurements for a specific template key.
    /// </summary>
    public void Invalidate(string templateKey)
    {
        var keysToRemove = new List<MeasurementKey>();

        foreach (var key in _cache.Keys)
        {
            if (key.TemplateKey == templateKey)
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }
    }

    /// <summary>
    /// Invalidates cached measurements for a given constrained extent (e.g., when viewport width changes).
    /// </summary>
    public void InvalidateForExtent(double constrainedExtent)
    {
        var keysToRemove = new List<MeasurementKey>();

        foreach (var key in _cache.Keys)
        {
            // Use tolerance for floating point comparison
            if (Math.Abs(key.ConstrainedExtent - constrainedExtent) > 0.5d)
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }
    }

    private void Evict()
    {
        // Simple eviction: clear half the cache
        var keysToRemove = _cache.Keys.Take(_cache.Count / 2).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }
    }

    private readonly record struct MeasurementKey(string TemplateKey, double ConstrainedExtent, int DataHash);
}
