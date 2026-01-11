namespace Acode.Application.Audit.Commands;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Audit;

/// <summary>
/// Command to start a new audit session.
/// </summary>
public sealed record StartAuditSessionCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartAuditSessionCommand"/> class.
    /// </summary>
    /// <param name="operatingMode">The operating mode for this session.</param>
    /// <param name="source">The source component starting the session.</param>
    public StartAuditSessionCommand(string operatingMode, string source)
    {
        OperatingMode = operatingMode ?? throw new ArgumentNullException(nameof(operatingMode));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary>
    /// Gets the operating mode for this session.
    /// </summary>
    public string OperatingMode { get; }

    /// <summary>
    /// Gets the source component starting the session.
    /// </summary>
    public string Source { get; }
}

/// <summary>
/// Handler for StartAuditSessionCommand.
/// </summary>
public sealed class StartAuditSessionCommandHandler
{
    private readonly IAuditLogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartAuditSessionCommandHandler"/> class.
    /// </summary>
    /// <param name="logger">The audit logger.</param>
    public StartAuditSessionCommandHandler(IAuditLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the StartAuditSessionCommand.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created audit session.</returns>
    public async Task<AuditSession> HandleAsync(
        StartAuditSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Create new session
        var sessionId = SessionId.New();
        var session = new AuditSession(sessionId, command.OperatingMode);

        // Log session start event
        var eventData = new Dictionary<string, object>
        {
            ["session_id"] = sessionId.Value,
            ["operating_mode"] = command.OperatingMode,
        };

        await _logger.LogAsync(
            AuditEventType.SessionStart,
            AuditSeverity.Info,
            command.Source,
            eventData,
            context: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return session;
    }
}
