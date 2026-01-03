namespace Acode.Domain.Audit;

/// <summary>
/// Severity levels for audit events.
/// Same as SecuritySeverity but in Audit namespace for clarity.
/// </summary>
public enum AuditSeverity
{
    /// <summary>
    /// Debug-level audit events.
    /// </summary>
    Debug = 0,

    /// <summary>
    /// Informational audit events.
    /// </summary>
    Info = 1,

    /// <summary>
    /// Warning-level audit events.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error-level audit events.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Critical audit events.
    /// </summary>
    Critical = 4
}
