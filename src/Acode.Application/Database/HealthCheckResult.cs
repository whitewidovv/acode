namespace Acode.Application.Database;

/// <summary>
/// Result of a database health check operation.
/// </summary>
/// <param name="Status">The health status of the database.</param>
/// <param name="Description">Human-readable description of the health check result.</param>
/// <param name="Data">Optional diagnostic data collected during the health check.</param>
/// <remarks>
/// Health checks are used to monitor database connectivity and integrity.
/// The Data dictionary may contain provider-specific metrics such as:
/// - SQLite: path, size_bytes, wal_size_bytes, integrity_check_result.
/// - PostgreSQL: pool_size, pool_idle, pool_busy, server_version.
/// </remarks>
public sealed record HealthCheckResult(
    HealthStatus Status,
    string Description,
    IReadOnlyDictionary<string, object>? Data = null);
