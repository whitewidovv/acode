namespace Acode.Infrastructure.Fallback;

using Acode.Application.Fallback;
using Acode.Application.Routing;

/// <summary>
/// Default implementation of fallback configuration.
/// </summary>
/// <remarks>
/// <para>AC-014 to AC-019: Fallback configuration implementation.</para>
/// <para>AC-091 to AC-097: Default configuration values.</para>
/// </remarks>
public sealed class FallbackConfiguration : IFallbackConfiguration
{
    private readonly Dictionary<AgentRole, IReadOnlyList<string>> _roleChains;
    private readonly List<string> _globalChain;

    /// <summary>
    /// Initializes a new instance of the <see cref="FallbackConfiguration"/> class with defaults.
    /// </summary>
    public FallbackConfiguration()
    {
        _roleChains = new Dictionary<AgentRole, IReadOnlyList<string>>();
        _globalChain = new List<string>();
        Policy = EscalationPolicy.RetryThenFallback;
        RetryCount = 2;
        RetryDelayMs = 1000;
        TimeoutMs = 60000;
        FailureThreshold = 5;
        CoolingPeriod = TimeSpan.FromSeconds(60);
        NotifyUser = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FallbackConfiguration"/> class.
    /// </summary>
    /// <param name="globalChain">The global fallback chain.</param>
    /// <param name="roleChains">Per-role fallback chains.</param>
    /// <param name="policy">The escalation policy.</param>
    /// <param name="retryCount">Number of retries.</param>
    /// <param name="retryDelayMs">Retry delay in milliseconds.</param>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <param name="failureThreshold">Failure threshold for circuit breaker.</param>
    /// <param name="coolingPeriod">Circuit cooling period.</param>
    /// <param name="notifyUser">Whether to notify user on fallback.</param>
    public FallbackConfiguration(
        IEnumerable<string> globalChain,
        IDictionary<AgentRole, IReadOnlyList<string>>? roleChains = null,
        EscalationPolicy policy = EscalationPolicy.RetryThenFallback,
        int retryCount = 2,
        int retryDelayMs = 1000,
        int timeoutMs = 60000,
        int failureThreshold = 5,
        TimeSpan? coolingPeriod = null,
        bool notifyUser = false
    )
    {
        ArgumentNullException.ThrowIfNull(globalChain);

        _globalChain = globalChain.ToList();
        _roleChains =
            roleChains != null
                ? new Dictionary<AgentRole, IReadOnlyList<string>>(roleChains)
                : new Dictionary<AgentRole, IReadOnlyList<string>>();

        Policy = policy;
        RetryCount = ValidateRange(retryCount, 0, 10, nameof(retryCount));
        RetryDelayMs = ValidateRange(retryDelayMs, 100, 30000, nameof(retryDelayMs));
        TimeoutMs = ValidateRange(timeoutMs, 1000, 600000, nameof(timeoutMs));
        FailureThreshold = ValidateRange(failureThreshold, 1, 20, nameof(failureThreshold));
        CoolingPeriod = coolingPeriod ?? TimeSpan.FromSeconds(60);
        NotifyUser = notifyUser;

        ValidateCoolingPeriod(CoolingPeriod);
    }

    /// <inheritdoc />
    public EscalationPolicy Policy { get; }

    /// <inheritdoc />
    public int RetryCount { get; }

    /// <inheritdoc />
    public int RetryDelayMs { get; }

    /// <inheritdoc />
    public int TimeoutMs { get; }

    /// <inheritdoc />
    public int FailureThreshold { get; }

    /// <inheritdoc />
    public TimeSpan CoolingPeriod { get; }

    /// <inheritdoc />
    public bool NotifyUser { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> GetGlobalChain()
    {
        return _globalChain.AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetRoleChain(AgentRole role)
    {
        if (_roleChains.TryGetValue(role, out var chain))
        {
            return chain;
        }

        return Array.Empty<string>();
    }

    private static int ValidateRange(int value, int min, int max, string name)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(
                name,
                value,
                $"Value must be between {min} and {max}"
            );
        }

        return value;
    }

    private static void ValidateCoolingPeriod(TimeSpan period)
    {
        if (period < TimeSpan.FromSeconds(5) || period > TimeSpan.FromMinutes(10))
        {
            throw new ArgumentOutOfRangeException(
                nameof(period),
                period,
                "Cooling period must be between 5 seconds and 10 minutes"
            );
        }
    }
}
