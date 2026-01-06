namespace Acode.Application.Database;

/// <summary>
/// Represents the health status of a database connection.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// The database is healthy and all checks passed.
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// The database is operational but with degraded performance or partial functionality.
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// The database is unhealthy and not functioning properly.
    /// </summary>
    Unhealthy = 2,
}
