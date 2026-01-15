namespace Acode.Application.ToolSchemas.Retry;

/// <summary>
/// Defines the severity levels for validation errors.
/// Determines whether execution is blocked and how errors are handled.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3212-3242.
/// Values are ordered from lowest to highest severity for proper sorting.
/// </remarks>
public enum ErrorSeverity
{
    /// <summary>
    /// Informational message, no action required. Execution proceeds normally.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning message, should be fixed but execution can proceed with degradation.
    /// Example: deprecated parameter used.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error message, must be fixed. Execution is blocked until corrected.
    /// Example: required field missing, type mismatch.
    /// </summary>
    Error = 2,
}
