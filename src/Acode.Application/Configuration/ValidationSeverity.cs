namespace Acode.Application.Configuration;

/// <summary>
/// Severity level for validation issues.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message, does not block startup.
    /// </summary>
    Information = 0,

    /// <summary>
    /// Warning, does not block startup but should be addressed.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error, blocks startup and must be fixed.
    /// </summary>
    Error = 2
}
