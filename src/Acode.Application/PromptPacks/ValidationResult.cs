namespace Acode.Application.PromptPacks;

/// <summary>
/// Represents the result of pack validation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    public ValidationResult(IEnumerable<ValidationError>? errors = null)
    {
        Errors = errors?.ToList().AsReadOnly() ?? new List<ValidationError>().AsReadOnly();
    }

    /// <summary>
    /// Gets a value indicating whether the pack is valid.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// Creates a successful validation result with no errors.
    /// </summary>
    /// <returns>A valid result.</returns>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>An invalid result.</returns>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors) => new(errors);

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="filePath">The optional file path.</param>
    /// <returns>An invalid result.</returns>
    public static ValidationResult Failure(string code, string message, string? filePath = null)
        => new(new[] { new ValidationError { Code = code, Message = message, FilePath = filePath } });
}
