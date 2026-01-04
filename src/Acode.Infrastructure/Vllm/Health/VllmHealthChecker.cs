using System.Diagnostics;
using Acode.Infrastructure.Vllm.Client;

namespace Acode.Infrastructure.Vllm.Health;

/// <summary>
/// Health checker for vLLM servers.
/// </summary>
public sealed class VllmHealthChecker
{
    private readonly VllmClientConfiguration _config;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmHealthChecker"/> class.
    /// </summary>
    /// <param name="config">Client configuration.</param>
    public VllmHealthChecker(VllmClientConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _config.Validate();

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_config.Endpoint),
            Timeout = TimeSpan.FromSeconds(_config.HealthCheckTimeoutSeconds)
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
            var response = await _httpClient.GetAsync("/health", cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
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
    /// <returns>Health status with timing and error information.</returns>
    public async Task<VllmHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            return new VllmHealthStatus(
                isHealthy: response.IsSuccessStatusCode,
                endpoint: _config.Endpoint,
                responseTimeMs: stopwatch.ElapsedMilliseconds,
                errorMessage: response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new VllmHealthStatus(
                isHealthy: false,
                endpoint: _config.Endpoint,
                responseTimeMs: stopwatch.ElapsedMilliseconds,
                errorMessage: ex.Message);
        }
    }
}
