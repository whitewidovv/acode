namespace Acode.Application.PromptPacks;

/// <summary>
/// Represents a single validation error from pack validation.
/// </summary>
public sealed class ValidationError
{
    /// <summary>
    /// Gets the error code (e.g., ACODE-VAL-001).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the file path where the error occurred, if applicable.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets the line number where the error occurred, if applicable.
    /// </summary>
    public int? LineNumber { get; init; }
}
