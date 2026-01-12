namespace Acode.Application.Audit.Queries;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Query to list audit sessions.
/// </summary>
public sealed record ListSessionsQuery
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListSessionsQuery"/> class.
    /// </summary>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    public ListSessionsQuery(DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null)
    {
        FromDate = fromDate;
        ToDate = toDate;
    }

    /// <summary>
    /// Gets the optional start date filter.
    /// </summary>
    public DateTimeOffset? FromDate { get; }

    /// <summary>
    /// Gets the optional end date filter.
    /// </summary>
    public DateTimeOffset? ToDate { get; }
}

/// <summary>
/// Handler for ListSessionsQuery.
/// </summary>
public sealed class ListSessionsQueryHandler
{
    private readonly IAuditSessionRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListSessionsQueryHandler"/> class.
    /// </summary>
    /// <param name="repository">The audit session repository.</param>
    public ListSessionsQueryHandler(IAuditSessionRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Handles the ListSessionsQuery.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of session information.</returns>
    public async Task<IReadOnlyList<AuditSessionInfo>> HandleAsync(
        ListSessionsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await _repository.GetSessionsAsync(
            query.FromDate,
            query.ToDate,
            cancellationToken).ConfigureAwait(false);
    }
}
