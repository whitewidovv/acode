namespace Acode.Application.Database;

/// <summary>
/// Factory for creating database connections.
/// </summary>
/// <remarks>
/// Abstracts the creation of SQLite and PostgreSQL connections.
/// Implementations handle provider-specific configuration, connection strings,
/// WAL mode enablement (SQLite), connection pooling (PostgreSQL), and lifecycle management.
/// </remarks>
public interface IConnectionFactory
{
    /// <summary>
    /// Gets the database provider type for this factory.
    /// </summary>
    DbProviderType ProviderType { get; }

    /// <summary>
    /// Gets the connection string for this factory.
    /// </summary>
    /// <remarks>
    /// For SQLite: file path (e.g., "Data Source=.agent/data/workspace.db").
    /// For PostgreSQL: standard connection string (e.g., "Host=localhost;Port=5432;Database=acode").
    /// </remarks>
    string ConnectionString { get; }

    /// <summary>
    /// Creates a new database connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An open database connection.</returns>
    /// <remarks>
    /// The connection is returned in Open state.
    /// Caller is responsible for disposing the connection when done.
    /// SQLite: Creates database file if not exists, enables WAL mode.
    /// PostgreSQL: Returns connection from pool.
    /// </remarks>
    Task<IDbConnection> CreateAsync(CancellationToken cancellationToken = default);
}
