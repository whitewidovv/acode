// src/Acode.Application/Database/RollbackOptions.cs
namespace Acode.Application.Database;

/// <summary>
/// Options for the rollback operation.
/// </summary>
public sealed record RollbackOptions
{
    /// <summary>
    /// Gets the number of migrations to roll back.
    /// </summary>
    public int Steps { get; init; } = 1;

    /// <summary>
    /// Gets the target version to roll back to (optional).
    /// </summary>
    /// <remarks>
    /// If specified, rolls back to (but not including) this version.
    /// </remarks>
    public string? TargetVersion { get; init; }

    /// <summary>
    /// Gets a value indicating whether to perform a dry run without executing rollbacks.
    /// </summary>
    public bool DryRun { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to force rollback even if down scripts are missing.
    /// </summary>
    /// <remarks>
    /// WARNING: Use with caution. May result in inconsistent database state.
    /// </remarks>
    public bool Force { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether the user has confirmed the rollback operation.
    /// </summary>
    /// <remarks>
    /// Used by CLI to track user confirmation (--yes flag).
    /// </remarks>
    public bool Confirm { get; init; } = false;
}
