namespace Acode.Application.Fallback;

using Acode.Application.Routing;

/// <summary>
/// Interface for fallback chain configuration.
/// </summary>
/// <remarks>
/// <para>AC-014 to AC-019: Fallback configuration structure.</para>
/// </remarks>
public interface IFallbackConfiguration
{
    /// <summary>
    /// Gets the escalation policy.
    /// </summary>
    EscalationPolicy Policy { get; }

    /// <summary>
    /// Gets the number of retries for retry-based policies.
    /// </summary>
    int RetryCount { get; }

    /// <summary>
    /// Gets the retry delay in milliseconds.
    /// </summary>
    int RetryDelayMs { get; }

    /// <summary>
    /// Gets the request timeout in milliseconds.
    /// </summary>
    int TimeoutMs { get; }

    /// <summary>
    /// Gets the failure threshold before circuit opens.
    /// </summary>
    int FailureThreshold { get; }

    /// <summary>
    /// Gets the circuit cooling period.
    /// </summary>
    TimeSpan CoolingPeriod { get; }

    /// <summary>
    /// Gets a value indicating whether user should be notified on fallback.
    /// </summary>
    bool NotifyUser { get; }

    /// <summary>
    /// Gets the global fallback chain.
    /// </summary>
    /// <returns>Ordered list of fallback model IDs.</returns>
    IReadOnlyList<string> GetGlobalChain();

    /// <summary>
    /// Gets the fallback chain for a specific role.
    /// </summary>
    /// <param name="role">The agent role.</param>
    /// <returns>Ordered list of fallback model IDs for the role.</returns>
    IReadOnlyList<string> GetRoleChain(AgentRole role);
}
