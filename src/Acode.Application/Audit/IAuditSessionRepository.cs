namespace Acode.Application.Audit;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Audit;

/// <summary>
/// Repository for querying audit sessions.
/// </summary>
public interface IAuditSessionRepository
{
    /// <summary>
    /// Gets all audit sessions within a date range.
    /// </summary>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of session information.</returns>
    Task<IReadOnlyList<AuditSessionInfo>> GetSessionsAsync(
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events for a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit events.</returns>
    Task<IReadOnlyList<AuditEvent>> GetSessionEventsAsync(
        SessionId sessionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for searching audit events.
/// </summary>
public interface IAuditEventSearchRepository
{
    /// <summary>
    /// Searches audit events with optional filters.
    /// </summary>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="eventType">Optional event type filter.</param>
    /// <param name="minSeverity">Optional minimum severity filter.</param>
    /// <param name="searchText">Optional text search filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching audit events.</returns>
    Task<IReadOnlyList<AuditEvent>> SearchEventsAsync(
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        AuditEventType? eventType = null,
        AuditSeverity? minSeverity = null,
        string? searchText = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for retrieving audit statistics.
/// </summary>
public interface IAuditStatsRepository
{
    /// <summary>
    /// Gets comprehensive audit statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audit statistics.</returns>
    Task<AuditStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Summary information about an audit session.
/// </summary>
public sealed record AuditSessionInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditSessionInfo"/> class.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="operatingMode">The operating mode.</param>
    /// <param name="startedAt">When the session started.</param>
    public AuditSessionInfo(SessionId sessionId, string operatingMode, DateTimeOffset startedAt)
    {
        SessionId = sessionId;
        OperatingMode = operatingMode ?? throw new ArgumentNullException(nameof(operatingMode));
        StartedAt = startedAt;
    }

    /// <summary>
    /// Gets the session ID.
    /// </summary>
    public SessionId SessionId { get; }

    /// <summary>
    /// Gets the operating mode.
    /// </summary>
    public string OperatingMode { get; }

    /// <summary>
    /// Gets when the session started.
    /// </summary>
    public DateTimeOffset StartedAt { get; }
}

/// <summary>
/// Audit statistics summary.
/// </summary>
public sealed record AuditStatistics
{
    /// <summary>
    /// Gets or initializes the total number of audit sessions.
    /// </summary>
    public required int TotalSessions { get; init; }

    /// <summary>
    /// Gets or initializes the total number of audit events.
    /// </summary>
    public required int TotalEvents { get; init; }

    /// <summary>
    /// Gets or initializes event counts grouped by event type.
    /// </summary>
    public required IReadOnlyDictionary<AuditEventType, int> EventsByType { get; init; }

    /// <summary>
    /// Gets or initializes event counts grouped by severity.
    /// </summary>
    public required IReadOnlyDictionary<AuditSeverity, int> EventsBySeverity { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp of the oldest event.
    /// </summary>
    public DateTimeOffset? OldestEventTimestamp { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp of the newest event.
    /// </summary>
    public DateTimeOffset? NewestEventTimestamp { get; init; }
}
