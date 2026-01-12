namespace Acode.Application.Audit.Queries;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Query to get audit statistics.
/// </summary>
public sealed record GetAuditStatsQuery
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetAuditStatsQuery"/> class.
    /// </summary>
    public GetAuditStatsQuery()
    {
    }
}

/// <summary>
/// Handler for GetAuditStatsQuery.
/// </summary>
public sealed class GetAuditStatsQueryHandler
{
    private readonly IAuditStatsRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAuditStatsQueryHandler"/> class.
    /// </summary>
    /// <param name="repository">The audit stats repository.</param>
    public GetAuditStatsQueryHandler(IAuditStatsRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Handles the GetAuditStatsQuery.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audit statistics.</returns>
    public async Task<AuditStatistics> HandleAsync(
        GetAuditStatsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await _repository.GetStatisticsAsync(cancellationToken).ConfigureAwait(false);
    }
}
