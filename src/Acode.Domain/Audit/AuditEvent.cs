namespace Acode.Domain.Audit;

/// <summary>
/// Represents a single audit event.
/// Immutable record serializable to JSON for audit logs.
/// </summary>
public sealed record AuditEvent
{
    /// <summary>
    /// Gets the audit event schema version.
    /// </summary>
    public required string SchemaVersion { get; init; }

    /// <summary>
    /// Gets the unique event identifier.
    /// </summary>
    public required EventId EventId { get; init; }

    /// <summary>
    /// Gets the timestamp when event occurred (UTC).
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    public required SessionId SessionId { get; init; }

    /// <summary>
    /// Gets the correlation identifier for related events.
    /// </summary>
    public required CorrelationId CorrelationId { get; init; }

    /// <summary>
    /// Gets the type of audit event.
    /// </summary>
    public required AuditEventType EventType { get; init; }

    /// <summary>
    /// Gets the severity level.
    /// </summary>
    public required AuditSeverity Severity { get; init; }

    /// <summary>
    /// Gets the source component that generated the event.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Gets the operating mode when event occurred.
    /// </summary>
    public required string OperatingMode { get; init; }

    /// <summary>
    /// Gets the event-specific data.
    /// </summary>
    public required IReadOnlyDictionary<string, object> Data { get; init; }

    /// <summary>
    /// Gets additional context information.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Context { get; init; }
}
