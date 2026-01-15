namespace Acode.Infrastructure.Providers.Vllm.Lifecycle;

/// <summary>
/// Enforces restart rate limiting (max 3 restarts per 60 seconds).
/// </summary>
public sealed class VllmRestartPolicyEnforcer
{
    private readonly List<DateTime> _restartHistory = [];
    private readonly object _lockObject = new();

    /// <summary>
    /// Gets a value indicating whether a restart is allowed (max 3 per 60 seconds).
    /// </summary>
    /// <returns>True if restart is allowed, false if rate limit exceeded.</returns>
    public bool CanRestart()
    {
        lock (_lockObject)
        {
            // Remove restarts older than 60 seconds
            var cutoff = DateTime.UtcNow.AddSeconds(-60);
            _restartHistory.RemoveAll(r => r < cutoff);

            // Allow restart if fewer than 3 in the window
            return _restartHistory.Count < 3;
        }
    }

    /// <summary>
    /// Records a restart event (timestamp = now UTC).
    /// </summary>
    public void RecordRestart()
    {
        lock (_lockObject)
        {
            _restartHistory.Add(DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Resets the restart history (clears all recorded restarts).
    /// </summary>
    public void Reset()
    {
        lock (_lockObject)
        {
            _restartHistory.Clear();
        }
    }

    /// <summary>
    /// Gets the restart history (read-only list of restart times).
    /// </summary>
    /// <returns>List of restart timestamps in UTC.</returns>
    public IReadOnlyList<DateTime> GetRestartHistory()
    {
        lock (_lockObject)
        {
            // Remove old restarts before returning
            var cutoff = DateTime.UtcNow.AddSeconds(-60);
            _restartHistory.RemoveAll(r => r < cutoff);

            return _restartHistory.AsReadOnly();
        }
    }
}
