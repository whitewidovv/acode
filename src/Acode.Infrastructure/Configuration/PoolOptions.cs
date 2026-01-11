// src/Acode.Infrastructure/Configuration/PoolOptions.cs
namespace Acode.Infrastructure.Configuration;

/// <summary>
/// Configuration options for PostgreSQL connection pooling.
/// </summary>
public sealed class PoolOptions
{
    /// <summary>Gets or sets the minimum pool size.</summary>
    public int MinSize { get; set; } = 2;

    /// <summary>Gets or sets the maximum pool size.</summary>
    public int MaxSize { get; set; } = 10;

    /// <summary>Gets or sets the connection lifetime in seconds before recycling.</summary>
    public int ConnectionLifetimeSeconds { get; set; } = 300;

    /// <summary>Gets or sets the time to wait for pool connection before timeout.</summary>
    public int AcquisitionTimeoutSeconds { get; set; } = 15;
}
