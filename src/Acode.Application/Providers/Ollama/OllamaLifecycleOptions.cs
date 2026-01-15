using Acode.Domain.Providers.Ollama;

namespace Acode.Application.Providers.Ollama;

/// <summary>
/// Configuration options for Ollama service lifecycle management.
/// </summary>
/// <remarks>
/// Loaded from `.agent/config.yml` providers.ollama.lifecycle section.
/// Task 005d Functional Requirements: FR-055 to FR-087.
/// </remarks>
public sealed class OllamaLifecycleOptions
{
    /// <summary>
    /// Gets or sets the lifecycle management mode.
    /// Default: Managed.
    /// </summary>
    public OllamaLifecycleMode Mode { get; set; } = OllamaLifecycleMode.Managed;

    /// <summary>
    /// Gets or sets the maximum time (in seconds) to wait for Ollama to start.
    /// Default: 30 seconds.
    /// </summary>
    public int StartTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the interval (in seconds) for health checks.
    /// Default: 60 seconds.
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum consecutive health check failures before marking as Failed.
    /// Default: 3.
    /// </summary>
    public int MaxConsecutiveFailures { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum number of restarts allowed per minute.
    /// Prevents restart loops when the service keeps crashing.
    /// Default: 3.
    /// </summary>
    public int MaxRestartsPerMinute { get; set; } = 3;

    /// <summary>
    /// Gets or sets a value indicating whether to gracefully stop Ollama when Acode exits.
    /// Only applicable in Managed mode.
    /// Default: false (leave running for user to manage).
    /// </summary>
    public bool StopOnExit { get; set; } = false;

    /// <summary>
    /// Gets or sets the path to the Ollama binary.
    /// If not specified, searches system PATH.
    /// Default: "ollama" (searches PATH on Unix, PATH or default Windows location).
    /// </summary>
    public string OllamaBinaryPath { get; set; } = "ollama";

    /// <summary>
    /// Gets or sets the port where Ollama listens.
    /// Default: 11434 (Ollama's standard port).
    /// </summary>
    public int Port { get; set; } = 11434;

    /// <summary>
    /// Gets or sets the model name/ID to auto-pull on startup (Managed mode only).
    /// Optional - if not set, models must be pulled manually.
    /// </summary>
    public string? AutoPullModel { get; set; }

    /// <summary>
    /// Gets or sets the timeout (in seconds) for model pulling.
    /// Default: 600 seconds (10 minutes).
    /// </summary>
    public int ModelPullTimeoutSeconds { get; set; } = 600;

    /// <summary>
    /// Gets or sets the number of retries for model pull on network errors.
    /// Default: 3.
    /// </summary>
    public int ModelPullMaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the timeout (in seconds) for graceful shutdown.
    /// After this, process is force-killed.
    /// Default: 10 seconds.
    /// </summary>
    public int ShutdownGracePeriodSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether to operate in airgapped mode.
    /// In airgapped mode, model pulling from remote registries is disabled.
    /// Default: false.
    /// </summary>
    public bool AirgappedMode { get; set; } = false;
}
