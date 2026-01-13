namespace Acode.Application.Providers;

/// <summary>
/// Provider-specific configuration settings.
/// </summary>
/// <remarks>
/// FR-042 to FR-046 from task-004c spec.
/// Gap #4 from task-004c completion checklist.
/// </remarks>
public sealed record ProviderConfig
{
    /// <summary>
    /// Gets or initializes the default model to use with this provider.
    /// </summary>
    public string? DefaultModel { get; init; }

    /// <summary>
    /// Gets a value indicating whether to enable health checks for this provider.
    /// </summary>
    public bool EnableHealthChecks { get; init; } = true;

    /// <summary>
    /// Gets or initializes the health check interval.
    /// </summary>
    public TimeSpan? HealthCheckInterval { get; init; }

    /// <summary>
    /// Gets or initializes custom provider-specific settings.
    /// </summary>
    public Dictionary<string, object>? CustomSettings { get; init; }
}
