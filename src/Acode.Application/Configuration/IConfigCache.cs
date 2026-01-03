using Acode.Domain.Configuration;

namespace Acode.Application.Configuration;

/// <summary>
/// Interface for caching parsed configuration.
/// Implementations must be thread-safe.
/// </summary>
public interface IConfigCache
{
    /// <summary>
    /// Attempts to retrieve a cached configuration.
    /// </summary>
    /// <param name="repositoryRoot">Repository root path (cache key).</param>
    /// <param name="config">The cached configuration if found.</param>
    /// <returns>True if cached configuration exists, false otherwise.</returns>
    bool TryGet(string repositoryRoot, out AcodeConfig? config);

    /// <summary>
    /// Stores a configuration in the cache.
    /// </summary>
    /// <param name="repositoryRoot">Repository root path (cache key).</param>
    /// <param name="config">The configuration to cache.</param>
    void Store(string repositoryRoot, AcodeConfig config);

    /// <summary>
    /// Removes a specific entry from the cache.
    /// </summary>
    /// <param name="repositoryRoot">Repository root path (cache key).</param>
    void Invalidate(string repositoryRoot);

    /// <summary>
    /// Clears all cached configurations.
    /// </summary>
    void InvalidateAll();
}
