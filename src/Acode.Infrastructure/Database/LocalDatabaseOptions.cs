namespace Acode.Infrastructure.Database;

/// <summary>
/// Configuration options for local SQLite database.
/// </summary>
/// <remarks>
/// Used when the application runs in local-first mode with an embedded SQLite database.
/// </remarks>
public sealed class LocalDatabaseOptions
{
    /// <summary>
    /// Gets or sets the file path to the SQLite database.
    /// </summary>
    /// <remarks>
    /// If null, defaults to .agent/data/workspace.db relative to current directory.
    /// </remarks>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the busy timeout in milliseconds for database locking.
    /// </summary>
    /// <remarks>
    /// SQLite will retry locked operations for this duration before throwing an error.
    /// Default is 5000ms (5 seconds).
    /// </remarks>
    public int? BusyTimeoutMs { get; set; }
}
