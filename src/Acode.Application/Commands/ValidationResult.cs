namespace Acode.Application.Commands;

/// <summary>
/// Result of command specification validation.
/// Immutable record indicating success or failure with error message.
/// </summary>
public sealed record ValidationResult
{
    private ValidationResult(bool isValid, string? errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the error message if validation failed, otherwise null.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A ValidationResult indicating success.</returns>
    public static ValidationResult Success() => new(true, null);

    /// <summary>
    /// Creates a failed validation result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing why validation failed.</param>
    /// <returns>A ValidationResult indicating failure.</returns>
    public static ValidationResult Failure(string errorMessage) =>
        new(false, errorMessage ?? throw new ArgumentNullException(nameof(errorMessage)));
}
