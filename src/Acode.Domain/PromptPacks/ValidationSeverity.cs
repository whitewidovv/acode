namespace Acode.Domain.PromptPacks;

/// <summary>
/// Severity level for validation errors.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message (does not prevent pack usage).
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning message (pack can still be used, but may have issues).
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error message (pack cannot be used).
    /// </summary>
    Error = 2,
}
