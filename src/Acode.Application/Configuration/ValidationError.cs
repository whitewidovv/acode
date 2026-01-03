namespace Acode.Application.Configuration;

/// <summary>
/// Represents a single validation error or warning.
/// </summary>
public sealed record ValidationError
{
    /// <summary>
    /// Gets the error code (e.g., "CFG001").
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the severity of this validation issue.
    /// </summary>
    public required ValidationSeverity Severity { get; init; }

    /// <summary>
    /// Gets the configuration path where the error occurred (e.g., "model.provider").
    /// Null if error is global.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Gets the line number in the YAML file where the error occurred.
    /// Null if line information is not available.
    /// </summary>
    public int? Line { get; init; }

    /// <summary>
    /// Gets the column number in the YAML file where the error occurred.
    /// Null if column information is not available.
    /// </summary>
    public int? Column { get; init; }

    /// <summary>
    /// Gets the suggested fix for this error.
    /// Null if no suggestion is available.
    /// </summary>
    public string? Suggestion { get; init; }
}
