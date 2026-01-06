namespace Acode.Application.PromptPacks;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
/// <param name="IsValid">Whether the validation passed.</param>
/// <param name="Errors">Collection of validation errors (empty if valid).</param>
public sealed record ValidationResult(
    bool IsValid,
    IReadOnlyList<ValidationError> Errors)
{
    /// <summary>
    /// Creates a successful validation result with no errors.
    /// </summary>
    /// <returns>A validation result indicating success.</returns>
    public static ValidationResult Success()
    {
        return new ValidationResult(true, Array.Empty<ValidationError>());
    }

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors that occurred.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors)
    {
        return new ValidationResult(false, errors.ToList());
    }

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="error">The validation error that occurred.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static ValidationResult Failure(ValidationError error)
    {
        return new ValidationResult(false, new[] { error });
    }
}
