using System.Collections.Concurrent;
using Acode.Domain.PromptPacks;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Thread-safe cache for loaded prompt packs.
/// </summary>
public sealed class PackCache
{
    private readonly ConcurrentDictionary<string, PromptPack> _cache = new();

    /// <summary>
    /// Gets the number of cached packs.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Builds a cache key from pack ID and content hash.
    /// </summary>
    /// <param name="packId">The pack ID.</param>
    /// <param name="contentHash">The content hash.</param>
    /// <returns>The cache key.</returns>
    public static string BuildCacheKey(string packId, ContentHash? contentHash)
    {
        return contentHash is null
            ? packId
            : $"{packId}:{contentHash}";
    }

    /// <summary>
    /// Gets a cached pack by key.
    /// </summary>
    /// <param name="key">The cache key (pack ID + content hash).</param>
    /// <returns>The cached pack if found; otherwise, <c>null</c>.</returns>
    public PromptPack? Get(string key)
    {
        return _cache.TryGetValue(key, out var pack) ? pack : null;
    }

    /// <summary>
    /// Gets a cached pack by pack ID, ignoring hash.
    /// </summary>
    /// <param name="packId">The pack ID.</param>
    /// <returns>The cached pack if found; otherwise, <c>null</c>.</returns>
    public PromptPack? GetByPackId(string packId)
    {
        return _cache.Values.FirstOrDefault(p =>
            string.Equals(p.Id, packId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Stores a pack in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="pack">The pack to cache.</param>
    public void Set(string key, PromptPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        _cache[key] = pack;
    }

    /// <summary>
    /// Stores a pack in the cache using pack ID + hash as key.
    /// </summary>
    /// <param name="pack">The pack to cache.</param>
    public void Set(PromptPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        var key = BuildCacheKey(pack.Id, pack.ContentHash);
        _cache[key] = pack;
    }

    /// <summary>
    /// Removes a pack from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns><c>true</c> if the pack was removed; otherwise, <c>false</c>.</returns>
    public bool Remove(string key)
    {
        return _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }
}
