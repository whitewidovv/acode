// src/Acode.Domain/Enums/DatabaseType.cs
namespace Acode.Domain.Enums;

/// <summary>
/// Supported database types for the connection factory.
/// </summary>
public enum DatabaseType
{
    /// <summary>SQLite embedded database for local workspace storage.</summary>
    Sqlite,

    /// <summary>PostgreSQL server for remote/shared storage.</summary>
    Postgres
}
