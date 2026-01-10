namespace Acode.Application.Fallback;

/// <summary>
/// Information about a circuit breaker's current state.
/// </summary>
/// <remarks>
/// <para>AC-046, AC-049, AC-050: Circuit state reporting.</para>
/// </remarks>
public sealed class CircuitStateInfo
{
    /// <summary>
    /// Gets the model ID this state applies to.
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets the current circuit state.
    /// </summary>
    public required CircuitState State { get; init; }

    /// <summary>
    /// Gets the current failure count.
    /// </summary>
    public required int FailureCount { get; init; }

    /// <summary>
    /// Gets the time of the last failure, if any.
    /// </summary>
    public DateTimeOffset? LastFailureTime { get; init; }

    /// <summary>
    /// Gets the time when circuit will transition to half-open (if currently open).
    /// </summary>
    public DateTimeOffset? NextRetryTime { get; init; }

    /// <summary>
    /// Gets a value indicating whether the circuit is allowing requests.
    /// </summary>
    public bool IsAllowingRequests => State != CircuitState.Open;
}
