namespace Acode.Infrastructure.Providers.Ollama.Lifecycle;

/// <summary>
/// Enforces restart rate limiting to prevent restart loops.
/// </summary>
/// <remarks>
/// Implements exponential backoff: 1s → 2s → 4s → 8s → ...
/// Prevents service from constantly restarting if it keeps crashing.
/// Task 005d Functional Requirements: FR-045 to FR-051.
/// </remarks>
internal sealed class RestartPolicyEnforcer
{
    private readonly int _maxRestartsPerMinute;
    private readonly object _lockObject = new();
    private readonly List<DateTime> _restartTimestamps = new();
    private int _restartAttempt;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestartPolicyEnforcer"/> class.
    /// </summary>
    /// <param name="maxRestartsPerMinute">Maximum number of restarts allowed per 60 seconds.</param>
    public RestartPolicyEnforcer(int maxRestartsPerMinute = 3)
    {
        if (maxRestartsPerMinute <= 0)
        {
            throw new ArgumentException("Max restarts must be positive", nameof(maxRestartsPerMinute));
        }

        _maxRestartsPerMinute = maxRestartsPerMinute;
    }

    /// <summary>
    /// Determines whether a restart is allowed based on rate limiting.
    /// </summary>
    /// <returns>True if restart is allowed, false if rate limit exceeded.</returns>
    public bool CanRestart()
    {
        lock (_lockObject)
        {
            // Remove timestamps older than 60 seconds
            var cutoffTime = DateTime.UtcNow.AddSeconds(-60);
            _restartTimestamps.RemoveAll(ts => ts < cutoffTime);

            // Check if we've hit the limit
            return _restartTimestamps.Count < _maxRestartsPerMinute;
        }
    }

    /// <summary>
    /// Records a restart attempt.
    /// </summary>
    public void RecordRestart()
    {
        lock (_lockObject)
        {
            _restartTimestamps.Add(DateTime.UtcNow);
            _restartAttempt++;
        }
    }

    /// <summary>
    /// Gets the next backoff duration (exponential: 1s, 2s, 4s, 8s, ...).
    /// </summary>
    /// <returns>Recommended backoff duration before next restart attempt.</returns>
    public TimeSpan GetNextBackoffDuration()
    {
        lock (_lockObject)
        {
            // Exponential backoff: 2^n seconds where n is restart attempt (0-based)
            // Attempt 0 → 1s, Attempt 1 → 2s, Attempt 2 → 4s, Attempt 3 → 8s
            var seconds = Math.Pow(2, _restartAttempt);

            // Cap at 60 seconds maximum
            var maxSeconds = 60.0;
            seconds = Math.Min(seconds, maxSeconds);

            return TimeSpan.FromSeconds(seconds);
        }
    }

    /// <summary>
    /// Resets the restart policy (call on successful startup).
    /// </summary>
    public void Reset()
    {
        lock (_lockObject)
        {
            _restartTimestamps.Clear();
            _restartAttempt = 0;
        }
    }
}
