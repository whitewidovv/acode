namespace Acode.Infrastructure.Ollama;

using System;
using Acode.Infrastructure.Ollama.ToolCall.Models;

/// <summary>
/// Configuration settings for Ollama provider.
/// </summary>
/// <remarks>
/// FR-005-017 to FR-005-025: Ollama-specific configuration with validation.
/// </remarks>
public sealed record OllamaConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaConfiguration"/> class.
    /// </summary>
    /// <param name="baseUrl">Ollama server base URL.</param>
    /// <param name="defaultModel">Default model name.</param>
    /// <param name="requestTimeoutSeconds">Request timeout in seconds.</param>
    /// <param name="healthCheckTimeoutSeconds">Health check timeout in seconds.</param>
    /// <param name="maxRetries">Maximum retry attempts.</param>
    /// <param name="enableRetry">Whether retry is enabled.</param>
    public OllamaConfiguration(
        string baseUrl = "http://localhost:11434",
        string defaultModel = "llama3.2:latest",
        int requestTimeoutSeconds = 120,
        int healthCheckTimeoutSeconds = 5,
        int maxRetries = 3,
        bool enableRetry = true)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("BaseUrl must be non-empty.", nameof(baseUrl));
        }

        if (string.IsNullOrWhiteSpace(defaultModel))
        {
            throw new ArgumentException("DefaultModel must be non-empty.", nameof(defaultModel));
        }

        if (requestTimeoutSeconds <= 0)
        {
            throw new ArgumentException("RequestTimeoutSeconds must be positive.", nameof(requestTimeoutSeconds));
        }

        if (healthCheckTimeoutSeconds <= 0)
        {
            throw new ArgumentException("HealthCheckTimeoutSeconds must be positive.", nameof(healthCheckTimeoutSeconds));
        }

        if (maxRetries < 0)
        {
            throw new ArgumentException("MaxRetries must be non-negative.", nameof(maxRetries));
        }

        this.BaseUrl = baseUrl;
        this.DefaultModel = defaultModel;
        this.RequestTimeoutSeconds = requestTimeoutSeconds;
        this.HealthCheckTimeoutSeconds = healthCheckTimeoutSeconds;
        this.MaxRetries = maxRetries;
        this.EnableRetry = enableRetry;
    }

    /// <summary>
    /// Gets the Ollama server base URL.
    /// </summary>
    /// <remarks>
    /// FR-005-017: BaseUrl defaults to http://localhost:11434.
    /// </remarks>
    public string BaseUrl { get; init; }

    /// <summary>
    /// Gets the default model name.
    /// </summary>
    /// <remarks>
    /// FR-005-018: DefaultModel defaults to llama3.2:latest.
    /// </remarks>
    public string DefaultModel { get; init; }

    /// <summary>
    /// Gets the request timeout in seconds.
    /// </summary>
    /// <remarks>
    /// FR-005-019: RequestTimeoutSeconds defaults to 120.
    /// </remarks>
    public int RequestTimeoutSeconds { get; init; }

    /// <summary>
    /// Gets the health check timeout in seconds.
    /// </summary>
    /// <remarks>
    /// FR-005-020: HealthCheckTimeoutSeconds defaults to 5.
    /// </remarks>
    public int HealthCheckTimeoutSeconds { get; init; }

    /// <summary>
    /// Gets the maximum retry attempts.
    /// </summary>
    /// <remarks>
    /// FR-005-021: MaxRetries defaults to 3.
    /// </remarks>
    public int MaxRetries { get; init; }

    /// <summary>
    /// Gets a value indicating whether retry is enabled.
    /// </summary>
    /// <remarks>
    /// FR-005-022: EnableRetry defaults to true.
    /// </remarks>
    public bool EnableRetry { get; init; }

    /// <summary>
    /// Gets the configuration for tool call retry behavior.
    /// </summary>
    /// <remarks>
    /// FR-053: Tool call parsing with configurable retry on malformed JSON.
    /// Defaults to 3 retries with 100ms base delay and automatic repair enabled.
    /// </remarks>
    public RetryConfig ToolCallRetryConfig { get; init; } = new RetryConfig
    {
        MaxRetries = 3,
        EnableAutoRepair = true,
        RetryDelayMs = 100,
        RepairTimeoutMs = 100,
        StrictValidation = true,
        MaxNestingDepth = 64,
        MaxArgumentSize = 1_048_576, // 1MB
        RetryPromptTemplate = RetryConfig.DefaultRetryPromptTemplate,
    };

    /// <summary>
    /// Gets the request timeout as TimeSpan.
    /// </summary>
    /// <remarks>
    /// FR-005-023: RequestTimeout returns TimeSpan.FromSeconds(RequestTimeoutSeconds).
    /// </remarks>
    public TimeSpan RequestTimeout => TimeSpan.FromSeconds(this.RequestTimeoutSeconds);

    /// <summary>
    /// Gets the health check timeout as TimeSpan.
    /// </summary>
    /// <remarks>
    /// FR-005-024: HealthCheckTimeout returns TimeSpan.FromSeconds(HealthCheckTimeoutSeconds).
    /// </remarks>
    public TimeSpan HealthCheckTimeout => TimeSpan.FromSeconds(this.HealthCheckTimeoutSeconds);
}
