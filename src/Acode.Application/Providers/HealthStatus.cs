namespace Acode.Application.Providers;

/// <summary>
/// Health status of a provider.
/// </summary>
/// <remarks>
/// FR-052 to FR-056 from task-004c spec.
/// Gap #7 from task-004c completion checklist.
/// </remarks>
public enum HealthStatus
{
    /// <summary>
    /// Provider health is unknown (not yet checked).
    /// </summary>
    Unknown,

    /// <summary>
    /// Provider is healthy and available.
    /// </summary>
    Healthy,

    /// <summary>
    /// Provider is degraded but may still function.
    /// </summary>
    Degraded,

    /// <summary>
    /// Provider is unhealthy and unavailable.
    /// </summary>
    Unhealthy
}
