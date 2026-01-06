// src/Acode.Infrastructure/Configuration/RemoteDatabaseOptions.cs
namespace Acode.Infrastructure.Configuration;

/// <summary>
/// Configuration options for PostgreSQL remote database.
/// </summary>
public sealed class RemoteDatabaseOptions
{
    /// <summary>Gets or sets the full connection string (takes precedence over components).</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Gets or sets the database host.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Gets or sets the database port.</summary>
    public int Port { get; set; } = 5432;

    /// <summary>Gets or sets the database name.</summary>
    public string Database { get; set; } = "acode";

    /// <summary>Gets or sets the username for authentication.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the password for authentication.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the SSL mode: disable, prefer, require, verify-ca, verify-full.</summary>
    public string SslMode { get; set; } = "prefer";

    /// <summary>Gets or sets a value indicating whether to trust server certificate (development only).</summary>
    public bool TrustServerCertificate { get; set; } = false;

    /// <summary>Gets or sets the command timeout in seconds.</summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>Gets or sets the connection pool options.</summary>
    public PoolOptions Pool { get; set; } = new();
}
