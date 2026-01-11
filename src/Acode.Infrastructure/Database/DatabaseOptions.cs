namespace Acode.Infrastructure.Database;

/// <summary>
/// Configuration options for database connections.
/// </summary>
/// <remarks>
/// Supports both local (SQLite) and remote (PostgreSQL) database configurations.
/// At runtime, either Local or Remote should be populated based on deployment mode.
/// </remarks>
public sealed class DatabaseOptions
{
    /// <summary>
    /// The configuration section name for database options.
    /// </summary>
    public const string SectionName = "database";

    /// <summary>
    /// Gets or sets local database configuration for embedded SQLite.
    /// </summary>
    public LocalDatabaseOptions? Local { get; set; }

    /// <summary>
    /// Gets or sets remote database configuration for PostgreSQL.
    /// </summary>
    public RemoteDatabaseOptions? Remote { get; set; }
}
