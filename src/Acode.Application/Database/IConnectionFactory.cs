namespace Acode.Application.Database;

/// <summary>
/// Factory for creating database connections with proper lifecycle management.
/// </summary>
/// <remarks>
/// Implementations manage provider-specific connection initialization,
/// pooling (for PostgreSQL), and resource cleanup.
/// </remarks>
public interface IConnectionFactory
{
    /// <summary>
    /// Gets the database provider type implemented by this factory.
    /// </summary>
    DatabaseProvider Provider { get; }

    /// <summary>
    /// Creates and opens a new database connection asynchronously.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An opened database connection that must be disposed after use.</returns>
    /// <exception cref="InvalidOperationException">When connection cannot be created.</exception>
    Task<IDbConnection> CreateAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks the health of the database connection.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Health check result with status and diagnostic data.</returns>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default);
}
