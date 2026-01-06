namespace Acode.Domain.PromptPacks;

/// <summary>
/// Exception thrown when a prompt pack fails validation.
/// </summary>
public sealed class PackValidationException : PackException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PackValidationException"/> class.
    /// </summary>
    /// <param name="packId">The ID of the pack that failed validation.</param>
    /// <param name="validationResult">The validation result containing errors.</param>
    public PackValidationException(string packId, ValidationResult validationResult)
        : base(FormatMessage(packId, validationResult))
    {
        PackId = packId;
        ValidationResult = validationResult;
    }

    /// <summary>
    /// Gets the ID of the pack that failed validation.
    /// </summary>
    public string PackId { get; }

    /// <summary>
    /// Gets the validation result containing all errors.
    /// </summary>
    public ValidationResult ValidationResult { get; }

    private static string FormatMessage(string packId, ValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(validationResult);
        return $"Pack '{packId}' validation failed with {validationResult.Errors.Count} error(s)";
    }
}
