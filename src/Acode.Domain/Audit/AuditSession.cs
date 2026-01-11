namespace Acode.Domain.Audit;

/// <summary>
/// Represents a bounded audit session from start to end.
/// Tracks session-level metadata and state.
/// </summary>
public sealed class AuditSession
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditSession"/> class.
    /// </summary>
    /// <param name="sessionId">The unique session identifier.</param>
    /// <param name="operatingMode">The operating mode for this session.</param>
    public AuditSession(SessionId sessionId, string operatingMode)
    {
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        OperatingMode = operatingMode ?? throw new ArgumentNullException(nameof(operatingMode));
        StartedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the unique session identifier.
    /// </summary>
    public SessionId SessionId { get; }

    /// <summary>
    /// Gets the operating mode for this session.
    /// </summary>
    public string OperatingMode { get; }

    /// <summary>
    /// Gets the timestamp when the session started.
    /// </summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>
    /// Gets the timestamp when the session ended.
    /// </summary>
    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>
    /// Gets the total number of events logged in this session.
    /// </summary>
    public int EventCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the session is active.
    /// </summary>
    public bool IsActive => EndedAt == null;

    /// <summary>
    /// Gets the session duration.
    /// </summary>
    public TimeSpan Duration =>
        EndedAt.HasValue
            ? EndedAt.Value - StartedAt
            : DateTimeOffset.UtcNow - StartedAt;

    /// <summary>
    /// Records an event being logged.
    /// </summary>
    public void RecordEvent()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Cannot record events on ended session");
        }

        EventCount++;
    }

    /// <summary>
    /// Ends the audit session.
    /// </summary>
    public void End()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Session already ended");
        }

        EndedAt = DateTimeOffset.UtcNow;
    }
}
