namespace Acode.Infrastructure.Vllm.Health;

/// <summary>
/// Represents the health status of a vLLM server.
/// </summary>
public sealed class VllmHealthStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VllmHealthStatus"/> class.
    /// </summary>
    /// <param name="isHealthy">Whether the server is healthy.</param>
    /// <param name="endpoint">The server endpoint.</param>
    /// <param name="responseTimeMs">Response time in milliseconds.</param>
    /// <param name="errorMessage">Error message if unhealthy.</param>
    public VllmHealthStatus(bool isHealthy, string endpoint, long? responseTimeMs, string? errorMessage = null)
    {
        IsHealthy = isHealthy;
        Endpoint = endpoint;
        ResponseTimeMs = responseTimeMs;
        ErrorMessage = errorMessage;
        CheckedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets a value indicating whether the server is healthy.
    /// </summary>
    public bool IsHealthy { get; }

    /// <summary>
    /// Gets the server endpoint.
    /// </summary>
    public string Endpoint { get; }

    /// <summary>
    /// Gets the response time in milliseconds.
    /// </summary>
    public long? ResponseTimeMs { get; }

    /// <summary>
    /// Gets the error message if unhealthy.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the timestamp when the check was performed.
    /// </summary>
    public DateTimeOffset CheckedAt { get; }
}
