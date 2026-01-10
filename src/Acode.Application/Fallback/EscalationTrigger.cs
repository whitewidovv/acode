namespace Acode.Application.Fallback;

/// <summary>
/// Conditions that trigger fallback escalation.
/// </summary>
/// <remarks>
/// <para>AC-020 to AC-025: Escalation trigger conditions.</para>
/// </remarks>
public enum EscalationTrigger
{
    /// <summary>
    /// Model server not responding (connection refused, DNS failure).
    /// </summary>
    Unavailable = 0,

    /// <summary>
    /// Model response exceeded configured timeout.
    /// </summary>
    Timeout = 1,

    /// <summary>
    /// Model returned errors repeatedly (malformed responses, 500-series codes).
    /// </summary>
    RepeatedErrors = 2,
}
