namespace Acode.Infrastructure.Routing;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Acode.Application.Inference;
using Acode.Application.Routing;
using Microsoft.Extensions.Logging;

/// <summary>
/// Maintains registry of available models with caching for availability checks.
/// </summary>
/// <remarks>
/// AC-039 to AC-042: Availability checking with caching.
/// </remarks>
public sealed class ModelRegistry
{
    private readonly IEnumerable<IModelProvider> _providers;
    private readonly ILogger<ModelRegistry> _logger;
    private readonly ConcurrentDictionary<string, CachedAvailability> _availabilityCache = new();
    private readonly TimeSpan _cacheTtl;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelRegistry"/> class.
    /// </summary>
    /// <param name="providers">The model providers to query.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cacheTtlSeconds">Cache TTL in seconds (default 5).</param>
    public ModelRegistry(
        IEnumerable<IModelProvider> providers,
        ILogger<ModelRegistry> logger,
        int cacheTtlSeconds = 5
    )
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheTtl = TimeSpan.FromSeconds(cacheTtlSeconds);
    }

    /// <summary>
    /// Checks if a model is currently available.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <returns>True if available, false otherwise.</returns>
    public bool IsModelAvailable(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return false;
        }

        // Check cache first
        if (_availabilityCache.TryGetValue(modelId, out var cached) && !cached.IsExpired(_cacheTtl))
        {
            _logger.LogTrace(
                "Cache hit for model {ModelId}: available={Available}",
                modelId,
                cached.IsAvailable
            );
            return cached.IsAvailable;
        }

        // Query providers
        var isAvailable = CheckAvailabilityFromProviders(modelId);

        // Update cache
        _availabilityCache[modelId] = new CachedAvailability(isAvailable, DateTime.UtcNow);

        _logger.LogDebug("Model {ModelId} availability: {Available}", modelId, isAvailable);
        return isAvailable;
    }

    /// <summary>
    /// Gets the provider name for a model.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <returns>The provider name, or null if not found.</returns>
    public string? GetProviderForModel(string modelId)
    {
        ArgumentNullException.ThrowIfNull(modelId);

        // Check if model ID includes provider hint
        if (modelId.Contains('@', StringComparison.Ordinal))
        {
            return modelId.Split('@')[1];
        }

        // Find the first provider that supports this model
        foreach (var provider in _providers)
        {
            var supportedModels = provider.GetSupportedModels();
            if (supportedModels.Contains(modelId, StringComparer.OrdinalIgnoreCase))
            {
                return provider.ProviderName;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets model information.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <returns>Model info or null if not found.</returns>
    public ModelInfo? GetModelInfo(string modelId)
    {
        ArgumentNullException.ThrowIfNull(modelId);

        foreach (var provider in _providers)
        {
            var supportedModels = provider.GetSupportedModels();
            if (supportedModels.Contains(modelId, StringComparer.OrdinalIgnoreCase))
            {
                var capabilities = provider.Capabilities;

                return new ModelInfo
                {
                    ModelId = modelId,
                    Provider = provider.ProviderName,
                    IsLocal = IsLocalProvider(provider.ProviderName),
                    ParameterCount = EstimateParameterCount(modelId),
                    SupportsToolCalling = capabilities.SupportsTools,
                    IsAvailable = IsModelAvailable(modelId),
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Lists all available models across all providers.
    /// </summary>
    /// <returns>List of available model information.</returns>
    public IReadOnlyList<ModelInfo> ListAvailableModels()
    {
        var models = new List<ModelInfo>();

        foreach (var provider in _providers)
        {
            foreach (var modelId in provider.GetSupportedModels())
            {
                var info = GetModelInfo(modelId);
                if (info != null)
                {
                    models.Add(info);
                }
            }
        }

        return models.AsReadOnly();
    }

    private static bool IsLocalProvider(string providerName)
    {
        // Ollama and vLLM are local providers
        return providerName.Equals("ollama", StringComparison.OrdinalIgnoreCase)
            || providerName.Equals("vllm", StringComparison.OrdinalIgnoreCase);
    }

    private static long EstimateParameterCount(string modelId)
    {
        // Extract parameter count from model ID if present (e.g., "llama3.2:70b")
        var parts = modelId.Split(':');
        if (parts.Length >= 2)
        {
            var tag = parts[1].ToLowerInvariant();
            if (tag.EndsWith('b'))
            {
                var numPart = tag.TrimEnd('b');
                if (double.TryParse(numPart, out var billions))
                {
                    return (long)(billions * 1_000_000_000);
                }
            }
        }

        return 0; // Unknown
    }

    private bool CheckAvailabilityFromProviders(string modelId)
    {
        // Extract base model ID if provider is specified
        var baseModelId = modelId.Contains('@', StringComparison.Ordinal)
            ? modelId.Split('@')[0]
            : modelId;

        foreach (var provider in _providers)
        {
            var supportedModels = provider.GetSupportedModels();
            if (supportedModels.Contains(baseModelId, StringComparer.OrdinalIgnoreCase))
            {
                // Model is supported by this provider
                // Check if provider is healthy (model is loaded)
                try
                {
                    var healthTask = provider.IsHealthyAsync();
                    healthTask.Wait(TimeSpan.FromSeconds(5));
                    return healthTask.Result;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Health check failed for provider {Provider}",
                        provider.ProviderName
                    );
                    return false;
                }
            }
        }

        return false;
    }

    private sealed record CachedAvailability(bool IsAvailable, DateTime CachedAt)
    {
        public bool IsExpired(TimeSpan ttl) => DateTime.UtcNow - CachedAt > ttl;
    }
}
