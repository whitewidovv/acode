namespace Acode.Domain.Database;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public sealed record ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether validation succeeded (no errors).
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the list of validation warnings.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates a successful validation result with no errors or warnings.
    /// </summary>
    /// <returns>A successful validation result.</returns>
    public static ValidationResult Success() => new()
    {
        IsValid = true,
        Errors = Array.Empty<string>(),
        Warnings = Array.Empty<string>()
    };

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors,
        Warnings = Array.Empty<string>()
    };

    /// <summary>
    /// Creates a successful validation result with warnings.
    /// </summary>
    /// <param name="warnings">The validation warnings.</param>
    /// <returns>A validation result with warnings.</returns>
    public static ValidationResult WithWarnings(params string[] warnings) => new()
    {
        IsValid = true,
        Errors = Array.Empty<string>(),
        Warnings = warnings
    };

    /// <summary>
    /// Combines this validation result with another, merging errors and warnings.
    /// </summary>
    /// <param name="other">The other validation result to combine.</param>
    /// <returns>A combined validation result.</returns>
    public ValidationResult Combine(ValidationResult other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return new ValidationResult
        {
            IsValid = IsValid && other.IsValid,
            Errors = Errors.Concat(other.Errors).ToList(),
            Warnings = Warnings.Concat(other.Warnings).ToList()
        };
    }
}
