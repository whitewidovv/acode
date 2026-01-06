namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents a validation error encountered during pack validation.
/// </summary>
/// <param name="Code">Error code identifying the type of error.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Path">Optional file path where the error occurred.</param>
/// <param name="Severity">Severity level of the error.</param>
public sealed record ValidationError(
    string Code,
    string Message,
    string? Path = null,
    ValidationSeverity Severity = ValidationSeverity.Error);
