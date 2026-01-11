namespace Acode.Domain.Audit;

/// <summary>
/// Manages span hierarchy using AsyncLocal.
/// Enables hierarchical tracing with parent-child relationships.
/// </summary>
public sealed class SpanContext : IDisposable
{
    private static readonly AsyncLocal<SpanId?> CurrentSpan = new();
    private readonly SpanId? _previousSpan;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanContext"/> class.
    /// </summary>
    /// <param name="spanId">The span ID for this scope.</param>
    /// <param name="operation">Name of the operation being traced.</param>
    public SpanContext(SpanId spanId, string operation)
    {
        SpanId = spanId ?? throw new ArgumentNullException(nameof(spanId));
        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        ParentSpanId = CurrentSpan.Value; // Capture current span as parent

        // Save previous and set new
        _previousSpan = CurrentSpan.Value;
        CurrentSpan.Value = spanId;
        StartedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the current span ID.
    /// </summary>
    public static SpanId? Current => CurrentSpan.Value;

    /// <summary>
    /// Gets the span ID for this scope.
    /// </summary>
    public SpanId SpanId { get; }

    /// <summary>
    /// Gets the parent span ID (if nested).
    /// </summary>
    public SpanId? ParentSpanId { get; }

    /// <summary>
    /// Gets the operation name.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Gets the timestamp when the span started.
    /// </summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>
    /// Gets the timestamp when the span ended.
    /// </summary>
    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>
    /// Gets the span duration.
    /// </summary>
    public TimeSpan Duration =>
        EndedAt.HasValue
            ? EndedAt.Value - StartedAt
            : DateTimeOffset.UtcNow - StartedAt;

    /// <summary>
    /// Disposes the span context and restores previous span.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        EndedAt = DateTimeOffset.UtcNow;

        // Restore previous span
        CurrentSpan.Value = _previousSpan;
        _disposed = true;
    }
}
