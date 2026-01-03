namespace Acode.Domain.Security;

/// <summary>
/// Classification levels for data sensitivity.
/// Ordered from least sensitive (Public) to most sensitive (Secret).
/// Used to determine logging, redaction, and audit requirements.
/// </summary>
public enum DataClassification
{
    /// <summary>
    /// Public data with no sensitivity.
    /// Can be freely logged, shared, and displayed.
    /// Examples: README files, public documentation.
    /// </summary>
    Public = 0,

    /// <summary>
    /// Internal data not meant for external distribution.
    /// Should be logged with care, not shared externally.
    /// Examples: Internal configuration, source code structure.
    /// </summary>
    Internal = 1,

    /// <summary>
    /// Confidential data requiring protection.
    /// Should be redacted in logs, audit trail only.
    /// Examples: Business logic, proprietary algorithms, user data.
    /// </summary>
    Confidential = 2,

    /// <summary>
    /// Secret data requiring maximum protection.
    /// MUST be redacted in all logs, NEVER displayed or shared.
    /// Examples: API keys, passwords, tokens, credentials.
    /// </summary>
    Secret = 3
}
