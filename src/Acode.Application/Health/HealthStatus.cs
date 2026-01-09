// src/Acode.Application/Health/HealthStatus.cs
namespace Acode.Application.Health;

/// <summary>
/// Represents the health status of a component or the overall system.
/// Status values are ordered by severity: Healthy &lt; Degraded &lt; Unhealthy.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Component is functioning normally.
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// Component is functioning but with warnings.
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// Component is not functioning correctly.
    /// </summary>
    Unhealthy = 2
}
