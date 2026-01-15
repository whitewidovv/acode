using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Acode.Infrastructure.Vllm.Health.Metrics;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Vllm.Health;

/// <summary>
/// Health checker for vLLM servers.
/// </summary>
public sealed class VllmHealthChecker
{
    private readonly VllmHealthConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<VllmHealthChecker> _logger;
    private readonly Uri _endpoint;
    private readonly VllmMetricsClient? _metricsClient;
    private readonly VllmMetricsParser _metricsParser;
    private HealthStatus? _previousStatus;

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmHealthChecker"/> class.
    /// </summary>
    /// <param name="healthConfig">Health check configuration.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="metricsClient">Optional client for querying metrics.</param>
    /// <param name="metricsParser">Optional parser for Prometheus metrics.</param>
    public VllmHealthChecker(
        VllmHealthConfiguration healthConfig,
        ILogger<VllmHealthChecker> logger,
        VllmMetricsClient? metricsClient = null,
        VllmMetricsParser? metricsParser = null)
    {
        _config = healthConfig ?? throw new ArgumentNullException(nameof(healthConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config.Validate();

        _endpoint = new Uri("http://localhost:8000");
        _httpClient = new HttpClient
        {
            BaseAddress = _endpoint,
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };

        _metricsClient = metricsClient;
        _metricsParser = metricsParser ?? new VllmMetricsParser();
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
        _logger.LogDebug("Starting health check for vLLM at {Endpoint}", _endpoint);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Try /health endpoint first
            var response = await _httpClient.GetAsync(_config.HealthEndpoint, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            var responseTime = stopwatch.Elapsed;
            var status = DetermineStatus(response.StatusCode, responseTime);

            var models = await GetLoadedModelsAsync(cancellationToken).ConfigureAwait(false);
            var load = _config.LoadMonitoring.Enabled
                ? await GetLoadStatusAsync(cancellationToken).ConfigureAwait(false)
                : null;

            LogStatusResult(status, responseTime);
            CheckStatusTransition(status);

            return new VllmHealthResult(
                status: status,
                endpoint: _endpoint.ToString(),
                responseTime: responseTime,
                errorMessage: response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}",
                models: models,
                load: load);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Health check was cancelled for {Endpoint}", _endpoint);
            return VllmHealthResult.Unknown(_endpoint.ToString(), "Health check cancelled");
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Connection failed for {Endpoint}", _endpoint);
            return VllmHealthResult.Unhealthy(_endpoint.ToString(), stopwatch.Elapsed, $"Connection failed: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Health check timed out for {Endpoint}", _endpoint);
            return VllmHealthResult.Unknown(_endpoint.ToString(), $"Request timeout: {ex.Message}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Health check failed for {Endpoint}: {Error}", _endpoint, ex.Message);
            return VllmHealthResult.Unknown(_endpoint.ToString(), $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a specific model is loaded on the vLLM server.
    /// </summary>
    /// <param name="modelId">The model ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the model is loaded, false otherwise.</returns>
    public async Task<bool> IsModelLoadedAsync(string modelId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId, nameof(modelId));

        var models = await GetLoadedModelsAsync(cancellationToken).ConfigureAwait(false);
        return models.Contains(modelId);
    }

    private async Task<string[]> GetLoadedModelsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(_config.LoadMonitoring.MetricsEndpoint, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<string>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            // Parse JSON: { "data": [{ "id": "model-name" }] }
            var doc = JsonDocument.Parse(json);
            var models = doc.RootElement
                .GetProperty("data")
                .EnumerateArray()
                .Select(m => m.GetProperty("id").GetString() ?? string.Empty)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToArray();

            return models;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query models endpoint for {Endpoint}", _endpoint);
            return Array.Empty<string>();
        }
    }

    private async Task<VllmLoadStatus?> GetLoadStatusAsync(CancellationToken cancellationToken)
    {
        if (_metricsClient == null)
        {
            return null;
        }

        try
        {
            var prometheusText = await _metricsClient.GetMetricsAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(prometheusText))
            {
                return null;
            }

            var metrics = _metricsParser.Parse(prometheusText);

            var loadStatus = VllmLoadStatus.Create(
                metrics.RunningRequests,
                metrics.WaitingRequests,
                metrics.GpuUtilizationPercent,
                _config.LoadMonitoring.QueueThreshold,
                _config.LoadMonitoring.GpuThresholdPercent);

            return loadStatus;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get load status");
            return null;
        }
    }

    private HealthStatus DetermineStatus(HttpStatusCode statusCode, TimeSpan responseTime)
    {
        if (statusCode != HttpStatusCode.OK)
        {
            return HealthStatus.Unhealthy;
        }

        if (responseTime > TimeSpan.FromMilliseconds(_config.DegradedThresholdMs))
        {
            return HealthStatus.Degraded;
        }

        if (responseTime <= TimeSpan.FromMilliseconds(_config.HealthyThresholdMs))
        {
            return HealthStatus.Healthy;
        }

        // Between healthy and degraded thresholds - consider healthy
        return HealthStatus.Healthy;
    }

    private void LogStatusResult(HealthStatus status, TimeSpan responseTime)
    {
        _logger.LogInformation(
            "Health check complete: Status={Status}, ResponseTime={ResponseTimeMs}ms",
            status,
            responseTime.TotalMilliseconds);
    }

    private void CheckStatusTransition(HealthStatus newStatus)
    {
        if (_previousStatus.HasValue && _previousStatus != newStatus)
        {
            _logger.LogWarning(
                "Health status changed: {PreviousStatus} → {CurrentStatus}",
                _previousStatus.Value,
                newStatus);
        }

        _previousStatus = newStatus;
    }
}
