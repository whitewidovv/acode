namespace Acode.Application.Fallback;

/// <summary>
/// Escalation policy types.
/// </summary>
/// <remarks>
/// <para>AC-026 to AC-030: Escalation policy configuration.</para>
/// </remarks>
public enum EscalationPolicy
{
    /// <summary>
    /// Falls back on first failure with zero retries (minimum latency).
    /// </summary>
    Immediate = 0,

    /// <summary>
    /// Retries primary model N times before falling back.
    /// </summary>
    RetryThenFallback = 1,

    /// <summary>
    /// Combines retry logic with circuit breaker state tracking.
    /// </summary>
    CircuitBreaker = 2,
}
