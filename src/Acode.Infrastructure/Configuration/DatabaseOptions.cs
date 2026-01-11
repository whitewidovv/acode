// src/Acode.Infrastructure/Configuration/DatabaseOptions.cs
namespace Acode.Infrastructure.Configuration;

/// <summary>
/// Configuration options for database connections.
/// Bound from agent-config.yml database section.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>Configuration section name in appsettings/agent-config.</summary>
    public const string SectionName = "database";

    /// <summary>Gets or sets the database provider: sqlite or postgresql.</summary>
    public string Provider { get; set; } = "sqlite";

    /// <summary>Gets or sets the SQLite local database options.</summary>
    public LocalDatabaseOptions Local { get; set; } = new();

    /// <summary>Gets or sets the PostgreSQL remote database options.</summary>
    public RemoteDatabaseOptions Remote { get; set; } = new();

    /// <summary>Gets or sets the retry policy options.</summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>Gets or sets the transaction timeout in seconds.</summary>
    public int TransactionTimeoutSeconds { get; set; } = 30;
}
