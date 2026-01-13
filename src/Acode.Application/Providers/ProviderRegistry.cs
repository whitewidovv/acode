namespace Acode.Application.Providers;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Inference;
using Acode.Application.Providers.Exceptions;
using Acode.Application.Providers.Selection;
using Acode.Domain.Modes;
using Microsoft.Extensions.Logging;

/// <summary>
/// Thread-safe registry for managing and selecting model provider instances.
/// </summary>
/// <remarks>
/// FR-057 to FR-076 from task-004c spec.
/// Gap #12 from task-004c completion checklist.
/// </remarks>
public sealed class ProviderRegistry : IProviderRegistry
{
    private readonly ILogger<ProviderRegistry> _logger;
    private readonly IProviderSelector _selector;
    private readonly string? _defaultProviderId;
    private readonly Func<ProviderDescriptor, IModelProvider?>? _providerFactory;
    private readonly OperatingMode _operatingMode;
    private readonly ConcurrentDictionary<string, ProviderRegistration> _providers = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderRegistry"/> class.
    /// </summary>
    /// <param name="logger">Logger for registry operations.</param>
    /// <param name="selector">Provider selection strategy.</param>
    /// <param name="defaultProviderId">Optional default provider ID.</param>
    /// <param name="providerFactory">Optional factory for creating provider instances.</param>
    /// <param name="operatingMode">Operating mode for endpoint validation (defaults to LocalOnly).</param>
    public ProviderRegistry(
        ILogger<ProviderRegistry> logger,
        IProviderSelector selector,
        string? defaultProviderId = null,
        Func<ProviderDescriptor, IModelProvider?>? providerFactory = null,
        OperatingMode operatingMode = OperatingMode.LocalOnly)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _defaultProviderId = defaultProviderId;
        _providerFactory = providerFactory;
        _operatingMode = operatingMode;
    }

    /// <inheritdoc/>
    public void Register(ProviderDescriptor descriptor)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(descriptor, nameof(descriptor));

        // Gap #33: Validate endpoint against operating mode
        ValidateEndpointForOperatingMode(descriptor);

        var registration = new ProviderRegistration(descriptor, _providerFactory);

        if (!_providers.TryAdd(descriptor.Id, registration))
        {
            _logger.LogWarning(
                "Attempted to register duplicate provider: {ProviderId}",
                descriptor.Id);
            throw new ProviderRegistrationException(
                $"Provider '{descriptor.Id}' is already registered",
                "ACODE-PRV-001");
        }

        _logger.LogInformation(
            "Registered provider: {ProviderId} ({ProviderType})",
            descriptor.Id,
            descriptor.Type);
    }

    /// <inheritdoc/>
    public void Unregister(string providerId)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(providerId, nameof(providerId));

        if (_providers.TryRemove(providerId, out _))
        {
            _logger.LogInformation("Unregistered provider: {ProviderId}", providerId);
        }
        else
        {
            _logger.LogDebug(
                "Attempted to unregister non-existent provider: {ProviderId}",
                providerId);
        }
    }

    /// <inheritdoc/>
    public IModelProvider GetProvider(string providerId)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(providerId, nameof(providerId));

        if (!_providers.TryGetValue(providerId, out var registration))
        {
            _logger.LogWarning("Provider not found: {ProviderId}", providerId);
            throw new ProviderNotFoundException(providerId);
        }

        // Get or create provider instance
        var provider = registration.GetOrCreateProvider();
        if (provider == null)
        {
            _logger.LogError(
                "Failed to create provider instance: {ProviderId}",
                providerId);
            throw new ProviderNotFoundException(providerId);
        }

        return provider;
    }

    /// <inheritdoc/>
    public IModelProvider GetDefaultProvider()
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(_defaultProviderId))
        {
            _logger.LogWarning("No default provider configured");
            throw new ProviderNotFoundException("default");
        }

        return GetProvider(_defaultProviderId);
    }

    /// <inheritdoc/>
    public IModelProvider GetProviderFor(ChatRequest request)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var descriptors = _providers.Values
            .Select(r => r.Descriptor)
            .ToList();

        var healthStatus = _providers.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Health);

        var provider = _selector.SelectProvider(
            descriptors,
            request,
            healthStatus);

        if (provider == null)
        {
            _logger.LogWarning(
                "No capable provider found for request (Stream: {Stream}, Tools: {HasTools})",
                request.Stream,
                request.Tools?.Length > 0);
            throw new NoCapableProviderException(
                "No provider capable of handling the request was found",
                "No providers support the required capabilities");
        }

        _logger.LogDebug(
            "Selected provider {ProviderId} for request",
            provider.ProviderName);

        return provider;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ProviderDescriptor> ListProviders()
    {
        ThrowIfDisposed();

        return _providers.Values
            .Select(r => r.Descriptor)
            .ToList();
    }

    /// <inheritdoc/>
    public bool IsRegistered(string providerId)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(providerId, nameof(providerId));

        return _providers.ContainsKey(providerId);
    }

    /// <inheritdoc/>
    public ProviderHealth GetProviderHealth(string providerId)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(providerId, nameof(providerId));

        if (!_providers.TryGetValue(providerId, out var registration))
        {
            throw new ProviderNotFoundException(providerId);
        }

        return registration.Health;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, ProviderHealth>> CheckAllHealthAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var results = new Dictionary<string, ProviderHealth>();
        var tasks = new List<Task>();

        foreach (var (providerId, registration) in _providers)
        {
            tasks.Add(Task.Run(
                async () =>
                {
                    try
                    {
                        var provider = registration.GetOrCreateProvider();
                        if (provider != null)
                        {
                            var isHealthy = await provider.IsHealthyAsync(cancellationToken).ConfigureAwait(false);
                            var status = isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;
                            registration.UpdateHealth(status, null);

                            lock (results)
                            {
                                results[providerId] = registration.Health;
                            }

                            _logger.LogDebug(
                                "Health check for {ProviderId}: {Status}",
                                providerId,
                                status);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Health check failed for {ProviderId}",
                            providerId);
                        registration.UpdateHealth(HealthStatus.Unhealthy, ex.Message);

                        lock (results)
                        {
                            results[providerId] = registration.Health;
                        }
                    }
                },
                cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing ProviderRegistry");

        foreach (var (providerId, registration) in _providers)
        {
            try
            {
                if (registration.ProviderInstance is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else if (registration.ProviderInstance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Error disposing provider {ProviderId}",
                    providerId);
            }
        }

        _providers.Clear();
        _disposed = true;
    }

    /// <summary>
    /// Checks if an endpoint is local (localhost or 127.0.0.1 or ::1).
    /// </summary>
    /// <param name="endpoint">Endpoint URI to check.</param>
    /// <returns>True if endpoint is local, false otherwise.</returns>
    private static bool IsLocalEndpoint(Uri endpoint)
    {
        var host = endpoint.Host.ToLowerInvariant();

        // Handle IPv6 addresses (brackets are included in Host property)
        if (host.StartsWith("[") && host.EndsWith("]"))
        {
            var ipv6 = host.Trim('[', ']');
            return ipv6 == "::1";
        }

        return host == "localhost" ||
               host == "127.0.0.1" ||
               host == "::1" ||
               host.StartsWith("127.") ||
               host.EndsWith(".local");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ProviderRegistry));
        }
    }

    /// <summary>
    /// Validates provider endpoint against operating mode constraints.
    /// </summary>
    /// <param name="descriptor">Provider descriptor to validate.</param>
    /// <exception cref="ProviderRegistrationException">Thrown when endpoint violates operating mode constraints.</exception>
    /// <remarks>
    /// Gap #33: Operating mode integration per task-004c spec.
    /// - Airgapped: REJECT non-local endpoints.
    /// - LocalOnly: WARN about non-local endpoints.
    /// - Burst: ALLOW all endpoints.
    /// </remarks>
    private void ValidateEndpointForOperatingMode(ProviderDescriptor descriptor)
    {
        var endpoint = descriptor.Endpoint.BaseUrl;
        var isLocal = IsLocalEndpoint(endpoint);

        switch (_operatingMode)
        {
            case OperatingMode.Airgapped:
                if (!isLocal)
                {
                    _logger.LogError(
                        "Provider {ProviderId} rejected: External endpoint {Endpoint} not allowed in Airgapped mode",
                        descriptor.Id,
                        endpoint);
                    throw new ProviderRegistrationException(
                        $"Provider '{descriptor.Id}' has external endpoint '{endpoint}' which is not allowed in Airgapped mode",
                        "ACODE-PRV-002");
                }

                break;

            case OperatingMode.LocalOnly:
                if (!isLocal)
                {
                    _logger.LogWarning(
                        "Provider {ProviderId} uses external endpoint {Endpoint} in LocalOnly mode. Consider using localhost for privacy.",
                        descriptor.Id,
                        endpoint);
                }

                break;

            case OperatingMode.Burst:
                // All endpoints allowed in Burst mode
                break;
        }
    }

    private sealed class ProviderRegistration
    {
        private readonly Func<ProviderDescriptor, IModelProvider?>? _factory;
        private IModelProvider? _providerInstance;
        private ProviderHealth _health;

        public ProviderRegistration(
            ProviderDescriptor descriptor,
            Func<ProviderDescriptor, IModelProvider?>? factory = null)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            _factory = factory;
            _health = new ProviderHealth(HealthStatus.Unknown);
        }

        public ProviderDescriptor Descriptor { get; }

        public ProviderHealth Health => _health;

        public IModelProvider? ProviderInstance => _providerInstance;

        public IModelProvider? GetOrCreateProvider()
        {
            if (_providerInstance != null)
            {
                return _providerInstance;
            }

            if (_factory != null)
            {
                _providerInstance = _factory(Descriptor);
                return _providerInstance;
            }

            return null;
        }

        public void UpdateHealth(HealthStatus status, string? errorMessage)
        {
            var consecutiveFailures = status == HealthStatus.Unhealthy
                ? _health.ConsecutiveFailures + 1
                : 0;

            _health = new ProviderHealth(
                status,
                DateTime.UtcNow,
                errorMessage,
                consecutiveFailures);
        }
    }
}
