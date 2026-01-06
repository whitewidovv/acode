// src/Acode.Application/Database/MigrateResult.cs
namespace Acode.Application.Database;

/// <summary>
/// Result of a migration operation.
/// </summary>
public sealed record MigrateResult
{
    /// <summary>
    /// Gets a value indicating whether the migration operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the number of migrations that were applied.
    /// </summary>
    public required int AppliedCount { get; init; }

    /// <summary>
    /// Gets the total duration of the migration operation.
    /// </summary>
    public required TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Gets the list of migrations that were applied (optional).
    /// </summary>
    public IReadOnlyList<MigrationFile>? AppliedMigrations { get; init; }

    /// <summary>
    /// Gets the list of migrations that would be applied in a dry-run (optional).
    /// </summary>
    /// <remarks>
    /// Only populated when DryRun = true.
    /// </remarks>
    public IReadOnlyList<MigrationFile>? WouldApply { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed (optional).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the error code if the operation failed (optional).
    /// </summary>
    public string? ErrorCode { get; init; }
}
