namespace Acode.Infrastructure.Vllm.Health;

/// <summary>
/// Represents the health status result of a vLLM server.
/// </summary>
public sealed class VllmHealthResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VllmHealthResult"/> class.
    /// </summary>
    /// <param name="status">The health status.</param>
    /// <param name="endpoint">The server endpoint.</param>
    /// <param name="responseTime">Response time.</param>
    /// <param name="errorMessage">Error message if unhealthy.</param>
    /// <param name="models">Loaded models.</param>
    /// <param name="load">Load status.</param>
    public VllmHealthResult(
        HealthStatus status,
        string endpoint,
        TimeSpan responseTime,
        string? errorMessage = null,
        string[]? models = null,
        VllmLoadStatus? load = null)
    {
        Status = status;
        Endpoint = endpoint;
        ResponseTime = responseTime;
        ErrorMessage = errorMessage;
        Models = models ?? Array.Empty<string>();
        Load = load;
        CheckedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the health status.
    /// </summary>
    public HealthStatus Status { get; }

    /// <summary>
    /// Gets the server endpoint.
    /// </summary>
    public string Endpoint { get; }

    /// <summary>
    /// Gets the response time.
    /// </summary>
    public TimeSpan ResponseTime { get; }

    /// <summary>
    /// Gets the error message if unhealthy.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the loaded models.
    /// </summary>
    public string[] Models { get; }

    /// <summary>
    /// Gets the load status.
    /// </summary>
    public VllmLoadStatus? Load { get; }

    /// <summary>
    /// Gets the timestamp when the check was performed.
    /// </summary>
    public DateTimeOffset CheckedAt { get; }

    /// <summary>
    /// Creates an Unknown health result.
    /// </summary>
    /// <param name="endpoint">The vLLM server endpoint.</param>
    /// <param name="reason">The reason for the unknown status.</param>
    /// <returns>A new VllmHealthResult with Unknown status and zero response time.</returns>
    public static VllmHealthResult Unknown(string endpoint, string reason)
    {
        return new VllmHealthResult(
            HealthStatus.Unknown,
            endpoint,
            TimeSpan.Zero,
            errorMessage: reason);
    }

    /// <summary>
    /// Creates an Unhealthy health result.
    /// </summary>
    /// <param name="endpoint">The vLLM server endpoint.</param>
    /// <param name="responseTime">The response time of the failed check.</param>
    /// <param name="message">The error message describing why the server is unhealthy.</param>
    /// <returns>A new VllmHealthResult with Unhealthy status and the provided response time and error message.</returns>
    public static VllmHealthResult Unhealthy(string endpoint, TimeSpan responseTime, string message)
    {
        return new VllmHealthResult(
            HealthStatus.Unhealthy,
            endpoint,
            responseTime,
            errorMessage: message);
    }
}

/// <summary>
/// Backward compatibility: VllmHealthStatus is now VllmHealthResult.
/// </summary>
[Obsolete("Use VllmHealthResult instead", false)]
public sealed class VllmHealthStatus
{
    /// <summary>
    /// Gets a value indicating whether the server is healthy.
    /// </summary>
    public bool IsHealthy { get; init; }

    /// <summary>
    /// Gets the server endpoint.
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the response time in milliseconds.
    /// </summary>
    public long? ResponseTimeMs { get; init; }

    /// <summary>
    /// Gets the error message if unhealthy.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the timestamp when the check was performed.
    /// </summary>
    public DateTimeOffset CheckedAt { get; init; }
}
