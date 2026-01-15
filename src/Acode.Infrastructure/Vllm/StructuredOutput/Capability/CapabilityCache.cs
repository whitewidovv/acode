namespace Acode.Infrastructure.Vllm.StructuredOutput.Capability;

using System.Collections.Concurrent;

/// <summary>
/// Caches model capabilities to avoid repeated detection.
/// </summary>
/// <remarks>
/// FR-044 through FR-046: Thread-safe capability caching.
/// </remarks>
public sealed class CapabilityCache
{
    private readonly ConcurrentDictionary<string, ModelCapabilities> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tries to get cached capabilities for a model.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="capabilities">The cached capabilities, if found.</param>
    /// <returns>True if capabilities were found in cache, false otherwise.</returns>
    public bool TryGetCached(string modelId, out ModelCapabilities? capabilities)
    {
        if (string.IsNullOrEmpty(modelId))
        {
            capabilities = null;
            return false;
        }

        var found = this._cache.TryGetValue(modelId, out var cached);
        capabilities = cached;
        return found;
    }

    /// <summary>
    /// Stores capabilities in the cache.
    /// </summary>
    /// <param name="capabilities">The capabilities to cache.</param>
    public void Cache(ModelCapabilities capabilities)
    {
        if (capabilities != null && !string.IsNullOrEmpty(capabilities.ModelId))
        {
            this._cache[capabilities.ModelId] = capabilities;
        }
    }

    /// <summary>
    /// Clears cached capabilities for a specific model.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    public void Invalidate(string modelId)
    {
        if (!string.IsNullOrEmpty(modelId))
        {
            this._cache.TryRemove(modelId, out _);
        }
    }

    /// <summary>
    /// Clears all cached capabilities.
    /// </summary>
    public void Clear()
    {
        this._cache.Clear();
    }

    /// <summary>
    /// Gets the number of cached entries.
    /// </summary>
    /// <returns>The count of cached model capabilities.</returns>
    public int GetCacheSize()
    {
        return this._cache.Count;
    }
}
