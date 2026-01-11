namespace Acode.Infrastructure.Database;

/// <summary>
/// Configuration options for database connection pooling.
/// </summary>
/// <remarks>
/// Connection pooling reduces latency by reusing existing connections
/// instead of creating new ones for each operation.
/// </remarks>
public sealed class PoolOptions
{
    /// <summary>
    /// Gets or sets the minimum number of connections to maintain in the pool.
    /// </summary>
    /// <remarks>
    /// Default is 2 connections.
    /// </remarks>
    public int? MinSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of connections allowed in the pool.
    /// </summary>
    /// <remarks>
    /// Default is 10 connections.
    /// </remarks>
    public int? MaxSize { get; set; }

    /// <summary>
    /// Gets or sets the idle timeout in seconds before closing unused connections.
    /// </summary>
    /// <remarks>
    /// Default is 300 seconds (5 minutes).
    /// </remarks>
    public int? IdleTimeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum lifetime in seconds for any connection.
    /// </summary>
    /// <remarks>
    /// Connections are closed and recreated after this duration to prevent stale connections.
    /// Default is 3600 seconds (1 hour).
    /// </remarks>
    public int? ConnectionLifetime { get; set; }
}
