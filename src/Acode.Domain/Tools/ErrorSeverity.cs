namespace Acode.Domain.Tools;

/// <summary>
/// Defines the severity level of a validation error.
/// </summary>
/// <remarks>
/// FR-007b: Validation error retry contract requires severity levels.
/// AC-005, AC-008: Severity enum with Error, Warning, Info values.
/// Lower numeric values indicate higher severity (Error is most severe).
/// </remarks>
public enum ErrorSeverity
{
    /// <summary>
    /// Error severity - must be fixed, blocks execution.
    /// The model must correct this error before the tool can execute.
    /// </summary>
    Error = 0,

    /// <summary>
    /// Warning severity - should be fixed, execution may proceed with degradation.
    /// The model should consider fixing this, but the tool may still execute.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Info severity - advisory only, no action required.
    /// Informational message that does not require any correction.
    /// </summary>
    Info = 2,
}
