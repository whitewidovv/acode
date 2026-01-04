namespace Acode.Application.Inference;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Registry for managing model provider instances.
/// </summary>
/// <remarks>
/// FR-004c-01: IProviderRegistry interface defined.
/// FR-004c-02 to FR-004c-10: Methods and properties for provider management.
/// </remarks>
public interface IProviderRegistry
{
    /// <summary>
    /// Gets the number of registered providers.
    /// </summary>
    /// <remarks>
    /// FR-004c-10: Count property (int, number of registered providers).
    /// </remarks>
    int Count { get; }

    /// <summary>
    /// Registers a model provider.
    /// </summary>
    /// <param name="provider">Provider to register.</param>
    /// <remarks>
    /// FR-004c-01: Register method accepting IModelProvider.
    /// </remarks>
    void Register(IModelProvider provider);

    /// <summary>
    /// Gets a provider by name.
    /// </summary>
    /// <param name="providerName">Provider name/identifier.</param>
    /// <returns>Provider instance, or null if not found.</returns>
    /// <remarks>
    /// FR-004c-02, FR-004c-03: GetProvider method, returns null for unknown.
    /// </remarks>
    IModelProvider? GetProvider(string providerName);

    /// <summary>
    /// Gets all registered providers.
    /// </summary>
    /// <returns>Array of all providers.</returns>
    /// <remarks>
    /// FR-004c-04: GetAllProviders method returning all registered providers.
    /// </remarks>
    IModelProvider[] GetAllProviders();

    /// <summary>
    /// Unregisters a provider by name.
    /// </summary>
    /// <param name="providerName">Provider name/identifier.</param>
    /// <returns>True if provider was removed, false if not found.</returns>
    /// <remarks>
    /// FR-004c-05: Unregister method accepting provider name, returns bool.
    /// </remarks>
    bool Unregister(string providerName);

    /// <summary>
    /// Checks if a provider is registered.
    /// </summary>
    /// <param name="providerName">Provider name/identifier.</param>
    /// <returns>True if registered, false otherwise.</returns>
    /// <remarks>
    /// FR-004c-06: Contains method accepting provider name.
    /// </remarks>
    bool Contains(string providerName);

    /// <summary>
    /// Gets the first healthy provider, or null if none are healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Healthy provider instance, or null.</returns>
    /// <remarks>
    /// FR-004c-07: GetHealthyProviderAsync method with CancellationToken.
    /// </remarks>
    Task<IModelProvider?> GetHealthyProviderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default provider name.
    /// </summary>
    /// <returns>Default provider name, or null if not set.</returns>
    /// <remarks>
    /// FR-004c-08: GetDefaultProviderName method.
    /// </remarks>
    string? GetDefaultProviderName();

    /// <summary>
    /// Sets the default provider name.
    /// </summary>
    /// <param name="providerName">Provider name to set as default.</param>
    /// <remarks>
    /// FR-004c-09: SetDefaultProviderName method.
    /// </remarks>
    void SetDefaultProviderName(string? providerName);
}
