namespace Acode.Application.Providers.Selection;

using System;
using System.Collections.Generic;
using System.Linq;
using Acode.Application.Inference;

/// <summary>
/// Capability-based provider selection strategy that matches request requirements to provider capabilities.
/// </summary>
/// <remarks>
/// FR-092 to FR-100 from task-004c spec.
/// Gap #11 from task-004c completion checklist.
/// </remarks>
public sealed class CapabilityProviderSelector : IProviderSelector
{
    private readonly Func<ProviderDescriptor, IModelProvider?> _providerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CapabilityProviderSelector"/> class.
    /// </summary>
    /// <param name="providerFactory">Factory to create provider instances from descriptors.</param>
    public CapabilityProviderSelector(Func<ProviderDescriptor, IModelProvider?> providerFactory)
    {
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

        // FR-092, FR-093: Filter providers by capability requirements
        var requiredStreamingSupport = request.Stream;
        var requiredToolSupport = request.Tools != null && request.Tools.Length > 0;

        // FR-094, FR-095, FR-096: Find providers matching capabilities and health
        // Prefer healthy providers, but allow degraded if no healthy available
        var healthyProviders = providers
            .Where(p => IsHealthy(p.Id, healthStatus))
            .Where(p => MatchesCapabilities(p, requiredStreamingSupport, requiredToolSupport))
            .ToList();

        if (healthyProviders.Count > 0)
        {
            // FR-097: Return first matching healthy provider
            var provider = _providerFactory(healthyProviders[0]);
            if (provider != null)
            {
                return provider;
            }
        }

        // FR-098, FR-099, FR-100: No capable healthy providers found
        return null;
    }

    private static bool IsHealthy(string providerId, IReadOnlyDictionary<string, ProviderHealth> healthStatus)
    {
        if (!healthStatus.TryGetValue(providerId, out var health))
        {
            return false;
        }

        // Only Healthy status is considered healthy
        return health.Status == HealthStatus.Healthy;
    }

    private static bool MatchesCapabilities(
        ProviderDescriptor descriptor,
        bool requiresStreaming,
        bool requiresTools)
    {
        var capabilities = descriptor.Capabilities;

        // FR-093: Check streaming support if required
        if (requiresStreaming && !capabilities.SupportsStreaming)
        {
            return false;
        }

        // FR-094: Check tool support if required
        if (requiresTools && !capabilities.SupportsTools)
        {
            return false;
        }

        // All required capabilities are met
        return true;
    }
}
