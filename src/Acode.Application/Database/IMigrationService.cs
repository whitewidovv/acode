// src/Acode.Application/Database/IMigrationService.cs
namespace Acode.Application.Database;

/// <summary>
/// Primary interface for database migration operations.
/// </summary>
public interface IMigrationService
{
    /// <summary>
    /// Gets the current migration status including applied and pending migrations.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Migration status report with current state.</returns>
    Task<MigrationStatusReport> GetStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Applies pending migrations to the database.
    /// </summary>
    /// <param name="options">Migration options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the migration operation.</returns>
    Task<MigrateResult> MigrateAsync(MigrateOptions options, CancellationToken ct = default);

    /// <summary>
    /// Rolls back applied migrations.
    /// </summary>
    /// <param name="options">Rollback options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the rollback operation.</returns>
    Task<RollbackResult> RollbackAsync(RollbackOptions options, CancellationToken ct = default);

    /// <summary>
    /// Creates new migration files with the specified name.
    /// </summary>
    /// <param name="options">Creation options including migration name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing file paths of created migration files.</returns>
    Task<CreateResult> CreateAsync(CreateOptions options, CancellationToken ct = default);

    /// <summary>
    /// Validates checksums of all applied migrations.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result with any checksum mismatches.</returns>
    Task<ValidationResult> ValidateAsync(CancellationToken ct = default);

    /// <summary>
    /// Forces release of a stale migration lock.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if lock was released successfully.</returns>
    Task<bool> ForceUnlockAsync(CancellationToken ct = default);
}
