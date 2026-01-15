using System.Diagnostics;

namespace Acode.Infrastructure.Vllm.Health;

/// <summary>
/// Health checker for vLLM servers.
/// </summary>
public sealed class VllmHealthChecker
{
    private readonly VllmHealthConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmHealthChecker"/> class.
    /// </summary>
    /// <param name="healthConfig">Health check configuration.</param>
    /// <param name="endpoint">The vLLM server endpoint.</param>
    public VllmHealthChecker(VllmHealthConfiguration healthConfig, string endpoint)
    {
        _config = healthConfig ?? throw new ArgumentNullException(nameof(healthConfig));
        ArgumentException.ThrowIfNullOrEmpty(endpoint, nameof(endpoint));
        _config.Validate();

        _endpoint = new Uri(endpoint);
        _httpClient = new HttpClient
        {
            BaseAddress = _endpoint,
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };
    }

    /// <summary>
    /// Checks if the vLLM server is healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if healthy, false otherwise. Never throws exceptions.</returns>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await GetHealthStatusAsync(cancellationToken).ConfigureAwait(false);
            return result.Status == HealthStatus.Healthy;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets detailed health status of the vLLM server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health result with timing and detailed status information.</returns>
    public async Task<VllmHealthResult> GetHealthStatusAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Try /health endpoint first
            var response = await _httpClient.GetAsync(_config.HealthEndpoint, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var status = DetermineStatus(stopwatch.Elapsed);
                return new VllmHealthResult(
                    status: status,
                    endpoint: _endpoint.ToString(),
                    responseTime: stopwatch.Elapsed,
                    errorMessage: null,
                    models: null,
                    load: null);
            }

            // Fall back to /v1/models if /health fails
            return await TryModelsEndpointAsync(stopwatch, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return VllmHealthResult.Unknown(_endpoint.ToString(), "Health check cancelled");
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            return VllmHealthResult.Unhealthy(_endpoint.ToString(), stopwatch.Elapsed, $"Connection failed: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            return VllmHealthResult.Unknown(_endpoint.ToString(), $"Request timeout: {ex.Message}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return VllmHealthResult.Unknown(_endpoint.ToString(), $"Unexpected error: {ex.Message}");
        }
    }

    private async Task<VllmHealthResult> TryModelsEndpointAsync(Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync("/v1/models", cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var status = DetermineStatus(stopwatch.Elapsed);
                return new VllmHealthResult(
                    status: status,
                    endpoint: _endpoint.ToString(),
                    responseTime: stopwatch.Elapsed,
                    errorMessage: null,
                    models: null,
                    load: null);
            }

            return VllmHealthResult.Unhealthy(_endpoint.ToString(), stopwatch.Elapsed, $"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return VllmHealthResult.Unhealthy(_endpoint.ToString(), stopwatch.Elapsed, ex.Message);
        }
    }

    private HealthStatus DetermineStatus(TimeSpan responseTime)
    {
        if (responseTime.TotalMilliseconds < _config.HealthyThresholdMs)
        {
            return HealthStatus.Healthy;
        }

        if (responseTime.TotalMilliseconds > _config.DegradedThresholdMs)
        {
            return HealthStatus.Degraded;
        }

        return HealthStatus.Healthy;
    }
}
