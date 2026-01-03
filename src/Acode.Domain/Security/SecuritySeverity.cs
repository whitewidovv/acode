namespace Acode.Domain.Security;

/// <summary>
/// Severity levels for security events and audit logging.
/// Ordered from least (Debug) to most (Critical) severe.
/// </summary>
public enum SecuritySeverity
{
    /// <summary>
    /// Debug-level information for troubleshooting.
    /// Not typically logged in production.
    /// </summary>
    Debug = 0,

    /// <summary>
    /// Informational security events.
    /// Normal operations that provide context.
    /// </summary>
    Info = 1,

    /// <summary>
    /// Warning-level events that require attention.
    /// Potential security concerns that don't block operations.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error-level security events.
    /// Security controls blocked an operation.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Critical security violations.
    /// Security invariant violated, system halts.
    /// </summary>
    Critical = 4
}
