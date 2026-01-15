namespace Acode.Infrastructure.Vllm.Health.Metrics;

/// <summary>
/// Client for querying vLLM Prometheus metrics.
/// </summary>
public sealed class VllmMetricsClient
{
    private readonly string _metricsEndpoint;
    private HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmMetricsClient"/> class.
    /// </summary>
    /// <param name="baseUrl">The vLLM base URL.</param>
    /// <param name="metricsEndpoint">The metrics endpoint path.</param>
    public VllmMetricsClient(string baseUrl, string metricsEndpoint = "/metrics")
    {
        ArgumentNullException.ThrowIfNull(baseUrl);
        ArgumentNullException.ThrowIfNull(metricsEndpoint);

        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _metricsEndpoint = metricsEndpoint;
    }

    /// <summary>
    /// Gets Prometheus metrics from vLLM.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Raw Prometheus text, or empty string on failure.</returns>
    public async Task<string> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(_metricsEndpoint, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var text = await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            return text;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Sets the HTTP client for testing purposes.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use.</param>
    internal void SetHttpClientForTesting(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
}
