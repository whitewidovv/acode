// src/Acode.Application/Database/ValidationResult.cs
namespace Acode.Application.Database;

/// <summary>
/// Result of checksum validation for applied migrations.
/// </summary>
/// <remarks>
/// Named ChecksumValidationResult (not ValidationResult) to avoid confusion
/// with Configuration.ValidationResult.
/// </remarks>
public sealed record ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether all checksums are valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets the list of checksum mismatches found during validation.
    /// </summary>
    public required IReadOnlyList<ChecksumMismatch> Mismatches { get; init; }
}
