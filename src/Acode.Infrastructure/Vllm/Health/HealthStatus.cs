namespace Acode.Infrastructure.Vllm.Health;

/// <summary>
/// Health status values for vLLM provider.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Provider is responding normally (response time &lt; 1s).
    /// </summary>
    Healthy,

    /// <summary>
    /// Provider is responding but slow or overloaded (response time &gt; 5s).
    /// </summary>
    Degraded,

    /// <summary>
    /// Provider is not responding or returning errors.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// Cannot determine status (timeout or connection failure).
    /// </summary>
    Unknown
}
