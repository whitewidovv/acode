namespace Acode.Application.Fallback;

/// <summary>
/// Circuit breaker states.
/// </summary>
/// <remarks>
/// <para>AC-031 to AC-038: Circuit breaker state machine.</para>
/// </remarks>
public enum CircuitState
{
    /// <summary>
    /// Normal operation, requests pass through.
    /// </summary>
    Closed = 0,

    /// <summary>
    /// Circuit open, model temporarily disabled.
    /// </summary>
    Open = 1,

    /// <summary>
    /// Testing recovery, one request allowed through.
    /// </summary>
    HalfOpen = 2,
}
