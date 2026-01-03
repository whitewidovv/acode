namespace Acode.Domain.Risks;

/// <summary>
/// Severity levels for risks after DREAD scoring.
/// Ordered from least severe (Low) to most severe (Critical).
/// Used to prioritize risk mitigation efforts.
/// </summary>
public enum Severity
{
    /// <summary>
    /// Low severity risk.
    /// DREAD score: 0-4.
    /// Requires documentation but may be accepted.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium severity risk.
    /// DREAD score: 5-7.
    /// Should be mitigated if practical.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High severity risk.
    /// DREAD score: 8-9.
    /// Must be mitigated before release.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical severity risk.
    /// DREAD score: 10.
    /// Must be mitigated immediately, blocks all operations.
    /// </summary>
    Critical = 3
}
