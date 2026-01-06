namespace Acode.Application.Database;

/// <summary>
/// Specifies the database provider type for workspace persistence.
/// </summary>
/// <remarks>
/// Acode supports a dual-provider architecture:
/// - SQLite: Local embedded database for offline-capable operation.
/// - PostgreSQL: Remote relational database for team environments and cross-device sync.
/// Provider selection affects connection handling, pooling strategy, and feature availability.
/// </remarks>
public enum DbProviderType
{
    /// <summary>
    /// SQLite embedded database.
    /// </summary>
    /// <remarks>
    /// Single-file database stored in `.agent/data/workspace.db`.
    /// Ideal for: single-user development, offline work, CI/CD pipelines.
    /// Features: WAL mode, busy timeout handling, zero configuration.
    /// </remarks>
    SQLite = 0,

    /// <summary>
    /// PostgreSQL relational database server.
    /// </summary>
    /// <remarks>
    /// Full-featured client-server database.
    /// Ideal for: team environments, cross-device sync, enterprise deployments.
    /// Features: connection pooling, SSL/TLS, advanced query optimization.
    /// </remarks>
    PostgreSQL = 1,
}
