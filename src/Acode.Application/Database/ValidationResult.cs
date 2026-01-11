// src/Acode.Application/Database/ValidationResult.cs
namespace Acode.Application.Database;

/// <summary>
/// Result of validating discovered migrations against applied migration history.
/// </summary>
/// <remarks>
/// Includes checksum validation, pending migrations, and version gap detection.
/// </remarks>
public sealed record ValidationResult
{
    /// <summary>
    /// Gets the migrations that have been discovered but not yet applied.
    /// </summary>
    public required IReadOnlyList<MigrationFile> PendingMigrations { get; init; }

    /// <summary>
    /// Gets detected checksum mismatches indicating migration file tampering.
    /// </summary>
    public required IReadOnlyList<ChecksumMismatch> ChecksumMismatches { get; init; }

    /// <summary>
    /// Gets detected version gaps in the migration sequence.
    /// </summary>
    public required IReadOnlyList<VersionGap> VersionGaps { get; init; }

    /// <summary>
    /// Gets a value indicating whether the validation passed without critical issues.
    /// </summary>
    /// <remarks>
    /// Checksum mismatches are warnings (not critical) by default.
    /// Version gaps are critical and cause this to return false.
    /// </remarks>
    public bool IsValid => VersionGaps.Count == 0;
}
