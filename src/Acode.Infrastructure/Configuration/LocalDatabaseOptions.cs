// src/Acode.Infrastructure/Configuration/LocalDatabaseOptions.cs
namespace Acode.Infrastructure.Configuration;

/// <summary>
/// Configuration options for SQLite local database.
/// </summary>
public sealed class LocalDatabaseOptions
{
    /// <summary>Gets or sets the path to SQLite database file.</summary>
    public string Path { get; set; } = ".agent/data/workspace.db";

    /// <summary>Gets or sets a value indicating whether to enable WAL mode for concurrent readers.</summary>
    public bool WalMode { get; set; } = true;

    /// <summary>Gets or sets the busy timeout in milliseconds.</summary>
    public int BusyTimeoutMs { get; set; } = 5000;
}
