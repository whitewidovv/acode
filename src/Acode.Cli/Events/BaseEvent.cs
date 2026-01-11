namespace Acode.Cli.Events;

/// <summary>
/// Base record for all JSONL events.
/// </summary>
/// <remarks>
/// All event types inherit from this record, ensuring consistent
/// structure with type, timestamp, event ID, and optional correlation ID.
/// </remarks>
public abstract record BaseEvent
{
    private static int _eventCounter;

    /// <summary>
    /// Gets the event type identifier.
    /// </summary>
    public virtual string Type =>
        GetType().Name.Replace("Event", string.Empty, StringComparison.Ordinal).ToLowerInvariant();

    /// <summary>
    /// Gets the timestamp when the event was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the unique event identifier.
    /// </summary>
    public string EventId { get; init; } = $"evt_{Interlocked.Increment(ref _eventCounter)}";

    /// <summary>
    /// Gets the correlation ID linking related events.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the schema version for this event format.
    /// </summary>
    public string SchemaVersion => "1.0.0";
}
