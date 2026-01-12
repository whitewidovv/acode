namespace Acode.Application.Audit.Queries;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Audit;

/// <summary>
/// Query to search audit events with multiple filter criteria.
/// </summary>
public sealed record SearchEventsQuery
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchEventsQuery"/> class.
    /// </summary>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="eventType">Optional event type filter.</param>
    /// <param name="minSeverity">Optional minimum severity filter.</param>
    /// <param name="searchText">Optional text search filter.</param>
    public SearchEventsQuery(
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        AuditEventType? eventType = null,
        AuditSeverity? minSeverity = null,
        string? searchText = null)
    {
        FromDate = fromDate;
        ToDate = toDate;
        EventType = eventType;
        MinSeverity = minSeverity;
        SearchText = searchText;
    }

    /// <summary>
    /// Gets the optional start date filter.
    /// </summary>
    public DateTimeOffset? FromDate { get; }

    /// <summary>
    /// Gets the optional end date filter.
    /// </summary>
    public DateTimeOffset? ToDate { get; }

    /// <summary>
    /// Gets the optional event type filter.
    /// </summary>
    public AuditEventType? EventType { get; }

    /// <summary>
    /// Gets the optional minimum severity filter.
    /// </summary>
    public AuditSeverity? MinSeverity { get; }

    /// <summary>
    /// Gets the optional text search filter.
    /// </summary>
    public string? SearchText { get; }
}

/// <summary>
/// Handler for SearchEventsQuery.
/// </summary>
public sealed class SearchEventsQueryHandler
{
    private readonly IAuditEventSearchRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchEventsQueryHandler"/> class.
    /// </summary>
    /// <param name="repository">The audit event search repository.</param>
    public SearchEventsQueryHandler(IAuditEventSearchRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Handles the SearchEventsQuery.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching audit events.</returns>
    public async Task<IReadOnlyList<AuditEvent>> HandleAsync(
        SearchEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await _repository.SearchEventsAsync(
            query.FromDate,
            query.ToDate,
            query.EventType,
            query.MinSeverity,
            query.SearchText,
            cancellationToken).ConfigureAwait(false);
    }
}
