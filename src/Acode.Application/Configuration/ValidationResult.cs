namespace Acode.Application.Configuration;

/// <summary>
/// Represents the result of configuration validation.
/// Contains all validation errors and warnings.
/// </summary>
public sealed record ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether validation succeeded (no errors).
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets the list of validation errors and warnings.
    /// Empty if validation succeeded.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();

    /// <summary>
    /// Gets the list of errors only (excludes warnings).
    /// </summary>
    public IReadOnlyList<ValidationError> ErrorsOnly =>
        Errors.Where(e => e.Severity == ValidationSeverity.Error).ToList();

    /// <summary>
    /// Gets the list of warnings only.
    /// </summary>
    public IReadOnlyList<ValidationError> WarningsOnly =>
        Errors.Where(e => e.Severity == ValidationSeverity.Warning).ToList();

    /// <summary>
    /// Creates a successful validation result with no errors.
    /// </summary>
    /// <returns>A successful validation result.</returns>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Failure(params ValidationError[] errors) =>
        new() { IsValid = false, Errors = errors };

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors) =>
        new() { IsValid = false, Errors = errors.ToList() };
}
