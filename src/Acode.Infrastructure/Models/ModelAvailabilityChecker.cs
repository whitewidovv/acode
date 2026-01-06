using Acode.Application.Inference;
using Acode.Application.Models;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Models;

/// <summary>
/// Checks model availability by querying registered providers.
/// </summary>
/// <remarks>
/// The availability checker queries all registered providers to determine
/// which models are currently loaded and ready for inference.
///
/// Caching:
/// - Results are cached with a configurable TTL (default: 5 seconds).
/// - Cache is invalidated when TTL expires.
/// - Cache reduces provider query overhead during routing decisions.
///
/// A model is considered available if it appears in at least one provider's
/// supported models list.
/// </remarks>
public sealed class ModelAvailabilityChecker : IModelAvailabilityChecker
{
    private readonly ILogger<ModelAvailabilityChecker> _logger;
    private readonly IProviderRegistry _providerRegistry;
    private readonly int _cacheTtlSeconds;
    private readonly object _cacheLock = new();

    private HashSet<string>? _cachedModels;
    private DateTime _cacheExpiry = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelAvailabilityChecker"/> class.
    /// </summary>
    /// <param name="logger">Logger for availability checks.</param>
    /// <param name="providerRegistry">Provider registry to query for models.</param>
    /// <param name="cacheTtlSeconds">Cache TTL in seconds (default: 5).</param>
    public ModelAvailabilityChecker(
        ILogger<ModelAvailabilityChecker> logger,
        IProviderRegistry providerRegistry,
        int cacheTtlSeconds = 5)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(providerRegistry);

        _logger = logger;
        _providerRegistry = providerRegistry;
        _cacheTtlSeconds = cacheTtlSeconds;
    }

    /// <inheritdoc/>
    public bool IsModelAvailable(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        var availableModels = GetAvailableModelsWithCache();
        return availableModels.Contains(modelId);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> ListAvailableModels()
    {
        var availableModels = GetAvailableModelsWithCache();
        return availableModels.ToList();
    }

    private HashSet<string> GetAvailableModelsWithCache()
    {
        lock (_cacheLock)
        {
            // Check if cache is valid
            if (_cachedModels != null && DateTime.UtcNow < _cacheExpiry)
            {
                return _cachedModels;
            }

            // Cache expired or not initialized, refresh
            _cachedModels = QueryAvailableModels();
            _cacheExpiry = DateTime.UtcNow.AddSeconds(_cacheTtlSeconds);

            _logger.LogDebug(
                "Model availability cache refreshed. {ModelCount} models available.",
                _cachedModels.Count);

            return _cachedModels;
        }
    }

    private HashSet<string> QueryAvailableModels()
    {
        var availableModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var providers = _providerRegistry.GetAllProviders();
        foreach (var provider in providers)
        {
            try
            {
                var supportedModels = provider.GetSupportedModels();
                foreach (var model in supportedModels)
                {
                    availableModels.Add(model);
                }

                _logger.LogDebug(
                    "Provider {ProviderName} supports {ModelCount} models",
                    provider.ProviderName,
                    supportedModels.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to query supported models from provider {ProviderName}",
                    provider.ProviderName);
            }
        }

        return availableModels;
    }
}
