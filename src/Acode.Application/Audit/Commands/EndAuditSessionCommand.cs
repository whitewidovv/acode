namespace Acode.Application.Audit.Commands;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Audit;

/// <summary>
/// Command to end an audit session.
/// </summary>
public sealed record EndAuditSessionCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndAuditSessionCommand"/> class.
    /// </summary>
    /// <param name="session">The session to end.</param>
    /// <param name="source">The source component ending the session.</param>
    public EndAuditSessionCommand(AuditSession session, string source)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary>
    /// Gets the session to end.
    /// </summary>
    public AuditSession Session { get; }

    /// <summary>
    /// Gets the source component ending the session.
    /// </summary>
    public string Source { get; }
}

/// <summary>
/// Handler for EndAuditSessionCommand.
/// </summary>
public sealed class EndAuditSessionCommandHandler
{
    private readonly IAuditLogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndAuditSessionCommandHandler"/> class.
    /// </summary>
    /// <param name="logger">The audit logger.</param>
    public EndAuditSessionCommandHandler(IAuditLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the EndAuditSessionCommand.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(
        EndAuditSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // End the session (will throw if already ended)
        command.Session.End();

        // Log session end event with metrics
        var eventData = new Dictionary<string, object>
        {
            ["session_id"] = command.Session.SessionId.Value,
            ["event_count"] = command.Session.EventCount,
            ["duration_seconds"] = command.Session.Duration.TotalSeconds,
            ["started_at"] = command.Session.StartedAt,
            ["ended_at"] = command.Session.EndedAt!.Value,
        };

        await _logger.LogAsync(
            AuditEventType.SessionEnd,
            AuditSeverity.Info,
            command.Source,
            eventData,
            context: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
