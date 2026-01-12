namespace Acode.Application.Providers.Selection;

using System;
using System.Collections.Generic;
using System.Linq;
using Acode.Application.Inference;

/// <summary>
/// Default provider selection strategy that prefers a configured default provider.
/// </summary>
/// <remarks>
/// FR-086 to FR-091 from task-004c spec.
/// Gap #10 from task-004c completion checklist.
/// </remarks>
public sealed class DefaultProviderSelector : IProviderSelector
{
    private readonly string? _defaultProviderId;
    private readonly Func<ProviderDescriptor, IModelProvider?> _providerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultProviderSelector"/> class.
    /// </summary>
    /// <param name="defaultProviderId">Default provider ID to prefer.</param>
    /// <param name="providerFactory">Factory to create provider instances from descriptors.</param>
    public DefaultProviderSelector(
        string? defaultProviderId,
        Func<ProviderDescriptor, IModelProvider?> providerFactory)
    {
        _defaultProviderId = defaultProviderId;
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
    }

    /// <inheritdoc/>
    public IModelProvider? SelectProvider(
        IReadOnlyList<ProviderDescriptor> providers,
        ChatRequest request,
        IReadOnlyDictionary<string, ProviderHealth> healthStatus)
    {
        ArgumentNullException.ThrowIfNull(providers, nameof(providers));
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentNullException.ThrowIfNull(healthStatus, nameof(healthStatus));

        if (providers.Count == 0)
        {
            return null;
        }

        // FR-086, FR-087: Try default provider first if configured and healthy
        if (!string.IsNullOrEmpty(_defaultProviderId))
        {
            var defaultDescriptor = providers.FirstOrDefault(p => p.Id == _defaultProviderId);
            if (defaultDescriptor != null && IsHealthy(defaultDescriptor.Id, healthStatus))
            {
                var defaultProvider = _providerFactory(defaultDescriptor);
                if (defaultProvider != null)
                {
                    return defaultProvider;
                }
            }
        }

        // FR-088, FR-089: Fallback to first healthy provider
        foreach (var descriptor in providers)
        {
            if (IsHealthy(descriptor.Id, healthStatus))
            {
                var provider = _providerFactory(descriptor);
                if (provider != null)
                {
                    return provider;
                }
            }
        }

        // FR-090, FR-091: No healthy providers available
        return null;
    }

    private static bool IsHealthy(string providerId, IReadOnlyDictionary<string, ProviderHealth> healthStatus)
    {
        if (!healthStatus.TryGetValue(providerId, out var health))
        {
            // Unknown health status - treat as unhealthy for safety
            return false;
        }

        // Only Healthy status is considered healthy
        // Degraded, Unhealthy, Unknown are all treated as unhealthy
        return health.Status == HealthStatus.Healthy;
    }
}
