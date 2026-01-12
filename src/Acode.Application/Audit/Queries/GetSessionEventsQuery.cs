namespace Acode.Application.Audit.Queries;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Audit;

/// <summary>
/// Query to get events for a specific audit session.
/// </summary>
public sealed record GetSessionEventsQuery
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSessionEventsQuery"/> class.
    /// </summary>
    /// <param name="sessionId">The session ID to query.</param>
    public GetSessionEventsQuery(SessionId sessionId)
    {
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
    }

    /// <summary>
    /// Gets the session ID to query.
    /// </summary>
    public SessionId SessionId { get; }
}

/// <summary>
/// Handler for GetSessionEventsQuery.
/// </summary>
public sealed class GetSessionEventsQueryHandler
{
    private readonly IAuditSessionRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSessionEventsQueryHandler"/> class.
    /// </summary>
    /// <param name="repository">The audit session repository.</param>
    public GetSessionEventsQueryHandler(IAuditSessionRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Handles the GetSessionEventsQuery.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit events for the session.</returns>
    public async Task<IReadOnlyList<AuditEvent>> HandleAsync(
        GetSessionEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await _repository.GetSessionEventsAsync(
            query.SessionId,
            cancellationToken).ConfigureAwait(false);
    }
}
