namespace Acode.Application.Providers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Inference;

/// <summary>
/// Registry for managing and selecting model provider instances.
/// </summary>
/// <remarks>
/// FR-057 to FR-076 from task-004c spec.
/// Gap #8 from task-004c completion checklist.
/// </remarks>
public interface IProviderRegistry : IAsyncDisposable
{
    /// <summary>
    /// Registers a provider using its descriptor.
    /// </summary>
    /// <param name="descriptor">Provider descriptor containing configuration.</param>
    /// <remarks>
    /// FR-057: Register method accepting ProviderDescriptor.
    /// FR-058: Must validate descriptor is not null.
    /// FR-059: Must throw ProviderRegistrationException if provider with same ID already registered.
    /// </remarks>
    void Register(ProviderDescriptor descriptor);

    /// <summary>
    /// Unregisters a provider by ID.
    /// </summary>
    /// <param name="providerId">Provider identifier.</param>
    /// <remarks>
    /// FR-060: Unregister method accepting provider ID.
    /// FR-061: Must be idempotent (no error if provider not found).
    /// </remarks>
    void Unregister(string providerId);

    /// <summary>
    /// Gets a provider instance by ID.
    /// </summary>
    /// <param name="providerId">Provider identifier.</param>
    /// <returns>Provider instance.</returns>
    /// <remarks>
    /// FR-062: GetProvider method accepting provider ID.
    /// FR-063: Must throw ProviderNotFoundException if provider not found.
    /// </remarks>
    IModelProvider GetProvider(string providerId);

    /// <summary>
    /// Gets the default provider instance.
    /// </summary>
    /// <returns>Default provider instance.</returns>
    /// <remarks>
    /// FR-064: GetDefaultProvider method returning IModelProvider.
    /// FR-065: Must throw ProviderNotFoundException if no default provider configured.
    /// </remarks>
    IModelProvider GetDefaultProvider();

    /// <summary>
    /// Gets the most suitable provider for a given chat request.
    /// </summary>
    /// <param name="request">Chat request containing model and capability requirements.</param>
    /// <returns>Selected provider instance.</returns>
    /// <remarks>
    /// FR-066: GetProviderFor method accepting ChatRequest.
    /// FR-067: Must use IProviderSelector strategy for selection.
    /// FR-068: Must throw NoCapableProviderException if no provider can handle request.
    /// </remarks>
    IModelProvider GetProviderFor(ChatRequest request);

    /// <summary>
    /// Lists all registered provider descriptors.
    /// </summary>
    /// <returns>Read-only list of provider descriptors.</returns>
    /// <remarks>
    /// FR-069: ListProviders method returning IReadOnlyList&lt;ProviderDescriptor&gt;.
    /// </remarks>
    IReadOnlyList<ProviderDescriptor> ListProviders();

    /// <summary>
    /// Checks if a provider is registered.
    /// </summary>
    /// <param name="providerId">Provider identifier.</param>
    /// <returns>True if provider is registered, false otherwise.</returns>
    /// <remarks>
    /// FR-070: IsRegistered method accepting provider ID.
    /// </remarks>
    bool IsRegistered(string providerId);

    /// <summary>
    /// Gets the health status of a specific provider.
    /// </summary>
    /// <param name="providerId">Provider identifier.</param>
    /// <returns>Provider health status.</returns>
    /// <remarks>
    /// FR-071: GetProviderHealth method accepting provider ID.
    /// FR-072: Must return ProviderHealth with status and last check time.
    /// </remarks>
    ProviderHealth GetProviderHealth(string providerId);

    /// <summary>
    /// Checks health of all registered providers asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping provider IDs to health status.</returns>
    /// <remarks>
    /// FR-073: CheckAllHealthAsync method with CancellationToken.
    /// FR-074: Must call IsHealthyAsync on each provider.
    /// FR-075: Must update health status for each provider.
    /// FR-076: Must return IReadOnlyDictionary&lt;string, ProviderHealth&gt;.
    /// </remarks>
    Task<IReadOnlyDictionary<string, ProviderHealth>> CheckAllHealthAsync(
        CancellationToken cancellationToken = default);
}
