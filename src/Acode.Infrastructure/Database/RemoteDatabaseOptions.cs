using Npgsql;

namespace Acode.Infrastructure.Database;

/// <summary>
/// Configuration options for remote PostgreSQL database.
/// </summary>
/// <remarks>
/// Used when the application connects to a PostgreSQL server.
/// Supports both explicit connection strings and individual connection parameters.
/// </remarks>
public sealed class RemoteDatabaseOptions
{
    /// <summary>
    /// Gets or sets the complete connection string.
    /// </summary>
    /// <remarks>
    /// If provided, this takes precedence over individual connection parameters.
    /// Supports environment variable expansion via ${VAR} syntax.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the PostgreSQL server hostname or IP address.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// Gets or sets the PostgreSQL server port.
    /// </summary>
    /// <remarks>
    /// Default is 5432.
    /// </remarks>
    public int? Port { get; set; }

    /// <summary>
    /// Gets or sets the database name to connect to.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    /// <remarks>
    /// Supports environment variable expansion via ${VAR} syntax.
    /// </remarks>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    /// <remarks>
    /// Supports environment variable expansion via ${VAR} syntax.
    /// </remarks>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the SSL mode for the connection.
    /// </summary>
    /// <remarks>
    /// Default is Prefer (use SSL if server supports it).
    /// </remarks>
    public SslMode? SslMode { get; set; }

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    /// <remarks>
    /// Default is 30 seconds.
    /// </remarks>
    public int? ConnectionTimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    /// <remarks>
    /// Default is 60 seconds.
    /// </remarks>
    public int? CommandTimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets connection pool configuration.
    /// </summary>
    public PoolOptions? Pool { get; set; }
}
