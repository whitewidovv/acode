namespace Acode.Application.Database;

/// <summary>
/// Specifies the database provider type for workspace persistence.
/// </summary>
public enum DatabaseProvider
{
    /// <summary>
    /// SQLite embedded database.
    /// </summary>
    SQLite,

    /// <summary>
    /// PostgreSQL relational database server.
    /// </summary>
    PostgreSQL,
}
