using System.Collections.Concurrent;
using Acode.Domain.Configuration;

namespace Acode.Application.Configuration;

/// <summary>
/// Thread-safe in-memory cache for parsed configurations.
/// </summary>
public sealed class ConfigCache : IConfigCache
{
    private readonly ConcurrentDictionary<string, AcodeConfig> _cache = new();

    /// <inheritdoc/>
    public bool TryGet(string repositoryRoot, out AcodeConfig? config)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);

        return _cache.TryGetValue(repositoryRoot, out config);
    }

    /// <inheritdoc/>
    public void Store(string repositoryRoot, AcodeConfig config)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(config);

        _cache[repositoryRoot] = config;
    }

    /// <inheritdoc/>
    public void Invalidate(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);

        _cache.TryRemove(repositoryRoot, out _);
    }

    /// <inheritdoc/>
    public void InvalidateAll()
    {
        _cache.Clear();
    }
}
