// src/Acode.Application/Health/CompositeHealthResult.cs
namespace Acode.Application.Health;

using System;
using System.Collections.Generic;

/// <summary>
/// Aggregated health check results from all registered checks.
/// </summary>
public sealed record CompositeHealthResult
{
    /// <summary>
    /// Gets the overall system health (worst-case aggregation).
    /// </summary>
    public required HealthStatus AggregateStatus { get; init; }

    /// <summary>
    /// Gets the individual check results.
    /// </summary>
    public required IReadOnlyList<HealthCheckResult> Results { get; init; }

    /// <summary>
    /// Gets the total duration of all checks combined.
    /// </summary>
    public required TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Gets the timestamp when checks were executed.
    /// </summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets a value indicating whether the overall status is healthy.
    /// </summary>
    public bool IsHealthy => AggregateStatus == HealthStatus.Healthy;

    /// <summary>
    /// Gets a value indicating whether any checks are degraded.
    /// </summary>
    public bool IsDegraded => AggregateStatus == HealthStatus.Degraded;

    /// <summary>
    /// Gets a value indicating whether any checks are unhealthy.
    /// </summary>
    public bool IsUnhealthy => AggregateStatus == HealthStatus.Unhealthy;
}
