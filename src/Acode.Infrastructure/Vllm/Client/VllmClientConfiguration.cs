namespace Acode.Infrastructure.Vllm.Client;

/// <summary>
/// Configuration for the vLLM HTTP client, including connection pooling and timeout settings.
/// </summary>
public sealed class VllmClientConfiguration
{
    /// <summary>
    /// Gets or sets the vLLM server endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:8000";

    /// <summary>
    /// Gets or sets the optional API key for authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent connections.
    /// </summary>
    public int MaxConnections { get; set; } = 10;

    /// <summary>
    /// Gets or sets the idle timeout in seconds for pooled connections.
    /// </summary>
    public int IdleTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Gets or sets the maximum lifetime in seconds for pooled connections.
    /// </summary>
    public int ConnectionLifetimeSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int ConnectTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the request timeout in seconds for non-streaming requests.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the streaming read timeout in seconds (per chunk).
    /// </summary>
    public int StreamingReadTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Validates the configuration and throws if invalid.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Endpoint must be a valid URI.", nameof(Endpoint));
        }

        if (MaxConnections <= 0)
        {
            throw new ArgumentException("MaxConnections must be greater than 0.", nameof(MaxConnections));
        }

        if (IdleTimeoutSeconds <= 0)
        {
            throw new ArgumentException("IdleTimeoutSeconds timeout must be greater than 0.", nameof(IdleTimeoutSeconds));
        }

        if (ConnectionLifetimeSeconds <= 0)
        {
            throw new ArgumentException("ConnectionLifetimeSeconds timeout must be greater than 0.", nameof(ConnectionLifetimeSeconds));
        }

        if (ConnectTimeoutSeconds <= 0)
        {
            throw new ArgumentException("ConnectTimeoutSeconds timeout must be greater than 0.", nameof(ConnectTimeoutSeconds));
        }

        if (RequestTimeoutSeconds <= 0)
        {
            throw new ArgumentException("RequestTimeoutSeconds timeout must be greater than 0.", nameof(RequestTimeoutSeconds));
        }

        if (StreamingReadTimeoutSeconds <= 0)
        {
            throw new ArgumentException("StreamingReadTimeoutSeconds timeout must be greater than 0.", nameof(StreamingReadTimeoutSeconds));
        }
    }
}
