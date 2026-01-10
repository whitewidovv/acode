namespace Acode.Infrastructure.Fallback;

using Acode.Application.Fallback;

/// <summary>
/// Thread-safe circuit breaker implementation for model failure tracking.
/// </summary>
/// <remarks>
/// <para>AC-031 to AC-038: Circuit breaker pattern implementation.</para>
/// <para>AC-101: Thread-safe with locking.</para>
/// </remarks>
public sealed class CircuitBreaker
{
    private readonly object _lock = new();
    private readonly int _threshold;
    private readonly TimeSpan _coolingPeriod;
    private int _failureCount;
    private DateTimeOffset _lastFailure;
    private CircuitState _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreaker"/> class.
    /// </summary>
    /// <param name="threshold">Number of failures before circuit opens (must be >= 1).</param>
    /// <param name="coolingPeriod">How long circuit stays open (must be >= 5 seconds).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if parameters are invalid.</exception>
    public CircuitBreaker(int threshold, TimeSpan coolingPeriod)
    {
        if (threshold < 1 || threshold > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold),
                threshold,
                "Threshold must be between 1 and 20"
            );
        }

        if (coolingPeriod < TimeSpan.FromSeconds(5) || coolingPeriod > TimeSpan.FromMinutes(10))
        {
            throw new ArgumentOutOfRangeException(
                nameof(coolingPeriod),
                coolingPeriod,
                "Cooling period must be between 5 seconds and 10 minutes"
            );
        }

        _threshold = threshold;
        _coolingPeriod = coolingPeriod;
        _state = CircuitState.Closed;
        _failureCount = 0;
        _lastFailure = DateTimeOffset.MinValue;
    }

    /// <summary>
    /// Gets the current failure count.
    /// </summary>
    public int FailureCount
    {
        get
        {
            lock (_lock)
            {
                return _failureCount;
            }
        }
    }

    /// <summary>
    /// Gets the current circuit state.
    /// </summary>
    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Gets the last failure timestamp.
    /// </summary>
    public DateTimeOffset LastFailure
    {
        get
        {
            lock (_lock)
            {
                return _lastFailure;
            }
        }
    }

    /// <summary>
    /// Gets the configured threshold.
    /// </summary>
    public int Threshold => _threshold;

    /// <summary>
    /// Gets the configured cooling period.
    /// </summary>
    public TimeSpan CoolingPeriod => _coolingPeriod;

    /// <summary>
    /// Records a failure. Opens circuit if threshold exceeded.
    /// </summary>
    public void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailure = DateTimeOffset.UtcNow;

            if (_failureCount >= _threshold)
            {
                _state = CircuitState.Open;
            }
        }
    }

    /// <summary>
    /// Records a success. Closes circuit and resets failure count.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitState.Closed;
        }
    }

    /// <summary>
    /// Checks if circuit should allow requests through.
    /// </summary>
    /// <returns>True if request should be allowed, false if circuit is open.</returns>
    public bool ShouldAllow()
    {
        lock (_lock)
        {
            if (_state == CircuitState.Closed)
            {
                return true;
            }

            if (
                _state == CircuitState.Open
                && DateTimeOffset.UtcNow - _lastFailure > _coolingPeriod
            )
            {
                _state = CircuitState.HalfOpen;
                return true;
            }

            if (_state == CircuitState.HalfOpen)
            {
                // Half-open allows one test request
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Manually resets circuit to closed state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitState.Closed;
            _lastFailure = DateTimeOffset.MinValue;
        }
    }

    /// <summary>
    /// Gets the time when circuit will transition to half-open.
    /// </summary>
    /// <returns>The next retry time, or null if circuit is not open.</returns>
    public DateTimeOffset? GetNextRetryTime()
    {
        lock (_lock)
        {
            if (_state == CircuitState.Open && _lastFailure != DateTimeOffset.MinValue)
            {
                return _lastFailure + _coolingPeriod;
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the current state information for reporting.
    /// </summary>
    /// <param name="modelId">The model ID for context.</param>
    /// <returns>Circuit state information.</returns>
    public CircuitStateInfo GetStateInfo(string modelId)
    {
        lock (_lock)
        {
            return new CircuitStateInfo
            {
                ModelId = modelId,
                State = _state,
                FailureCount = _failureCount,
                LastFailureTime = _lastFailure == DateTimeOffset.MinValue ? null : _lastFailure,
                NextRetryTime = GetNextRetryTimeInternal(),
            };
        }
    }

    private DateTimeOffset? GetNextRetryTimeInternal()
    {
        if (_state == CircuitState.Open && _lastFailure != DateTimeOffset.MinValue)
        {
            return _lastFailure + _coolingPeriod;
        }

        return null;
    }
}
