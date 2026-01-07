// src/Acode.Application/Database/MigrationBootstrapperOptions.cs
namespace Acode.Application.Database;

/// <summary>
/// Configuration options for migration bootstrapper.
/// </summary>
public sealed class MigrationBootstrapperOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to automatically apply pending migrations during bootstrap.
    /// Default is false (validate only).
    /// </summary>
    public bool AutoMigrate { get; set; }

    /// <summary>
    /// Gets or sets the timeout for acquiring the migration lock.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
