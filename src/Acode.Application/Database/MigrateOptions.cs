// src/Acode.Application/Database/MigrateOptions.cs
namespace Acode.Application.Database;

/// <summary>
/// Options for the migrate operation.
/// </summary>
public sealed record MigrateOptions
{
    /// <summary>
    /// Gets a value indicating whether to perform a dry run without executing migrations.
    /// </summary>
    public bool DryRun { get; init; } = false;

    /// <summary>
    /// Gets the target migration version to migrate to (optional).
    /// </summary>
    /// <remarks>
    /// If specified, migrations will be applied up to and including this version.
    /// </remarks>
    public string? TargetVersion { get; init; }

    /// <summary>
    /// Gets the migration version to skip during execution (optional).
    /// </summary>
    public string? SkipVersion { get; init; }

    /// <summary>
    /// Gets a value indicating whether to force migration even if checksums don't match.
    /// </summary>
    /// <remarks>
    /// WARNING: Use with caution. Bypasses checksum validation.
    /// </remarks>
    public bool Force { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to skip checksum validation entirely.
    /// </summary>
    public bool SkipChecksum { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to create a backup before migration.
    /// </summary>
    public bool CreateBackup { get; init; } = true;
}
