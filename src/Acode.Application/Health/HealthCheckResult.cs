// src/Acode.Application/Health/HealthCheckResult.cs
namespace Acode.Application.Health;

using System;
using System.Collections.Generic;

/// <summary>
/// Immutable result from a single health check execution.
/// </summary>
public sealed record HealthCheckResult
{
    /// <summary>
    /// Gets the unique name identifying this health check.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the current health status of the component.
    /// </summary>
    public required HealthStatus Status { get; init; }

    /// <summary>
    /// Gets the time taken to execute the health check.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the human-readable description of the status.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets additional details for debugging or monitoring.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Details { get; init; }

    /// <summary>
    /// Gets the actionable suggestion if status is not Healthy.
    /// </summary>
    public string? Suggestion { get; init; }

    /// <summary>
    /// Gets the error code if an error occurred.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets the timestamp when check was executed.
    /// </summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a healthy result with the given description.
    /// </summary>
    /// <param name="name">The health check name.</param>
    /// <param name="duration">Time taken to execute.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A healthy health check result.</returns>
    public static HealthCheckResult Healthy(string name, TimeSpan duration, string? description = null)
        => new()
        {
            Name = name,
            Status = HealthStatus.Healthy,
            Duration = duration,
            Description = description
        };

    /// <summary>
    /// Creates a degraded result with suggestion.
    /// </summary>
    /// <param name="name">The health check name.</param>
    /// <param name="duration">Time taken to execute.</param>
    /// <param name="description">Description of the degraded state.</param>
    /// <param name="suggestion">Actionable suggestion for remediation.</param>
    /// <returns>A degraded health check result.</returns>
    public static HealthCheckResult Degraded(
        string name,
        TimeSpan duration,
        string description,
        string suggestion)
        => new()
        {
            Name = name,
            Status = HealthStatus.Degraded,
            Duration = duration,
            Description = description,
            Suggestion = suggestion
        };

    /// <summary>
    /// Creates an unhealthy result with error code.
    /// </summary>
    /// <param name="name">The health check name.</param>
    /// <param name="duration">Time taken to execute.</param>
    /// <param name="description">Description of the unhealthy state.</param>
    /// <param name="errorCode">Error code identifier.</param>
    /// <param name="suggestion">Actionable suggestion for remediation.</param>
    /// <returns>An unhealthy health check result.</returns>
    public static HealthCheckResult Unhealthy(
        string name,
        TimeSpan duration,
        string description,
        string errorCode,
        string suggestion)
        => new()
        {
            Name = name,
            Status = HealthStatus.Unhealthy,
            Duration = duration,
            Description = description,
            ErrorCode = errorCode,
            Suggestion = suggestion
        };
}
