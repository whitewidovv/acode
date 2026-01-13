namespace Acode.Application.Audit.Commands;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Audit;

/// <summary>
/// Command to log an audit event.
/// </summary>
public sealed record LogEventCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogEventCommand"/> class.
    /// </summary>
    /// <param name="eventType">The type of event.</param>
    /// <param name="severity">The severity level.</param>
    /// <param name="source">The source component.</param>
    /// <param name="data">Event-specific data.</param>
    /// <param name="context">Optional context data.</param>
    public LogEventCommand(
        AuditEventType eventType,
        AuditSeverity severity,
        string source,
        IDictionary<string, object> data,
        IDictionary<string, object>? context = null)
    {
        EventType = eventType;
        Severity = severity;
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Context = context;
    }

    /// <summary>
    /// Gets the event type.
    /// </summary>
    public AuditEventType EventType { get; }

    /// <summary>
    /// Gets the severity level.
    /// </summary>
    public AuditSeverity Severity { get; }

    /// <summary>
    /// Gets the source component.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the event-specific data.
    /// </summary>
    public IDictionary<string, object> Data { get; }

    /// <summary>
    /// Gets the optional context data.
    /// </summary>
    public IDictionary<string, object>? Context { get; }
}

/// <summary>
/// Handler for LogEventCommand.
/// </summary>
public sealed class LogEventCommandHandler
{
    private readonly IAuditLogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogEventCommandHandler"/> class.
    /// </summary>
    /// <param name="logger">The audit logger.</param>
    public LogEventCommandHandler(IAuditLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the LogEventCommand.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(
        LogEventCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _logger.LogAsync(
            command.EventType,
            command.Severity,
            command.Source,
            command.Data,
            command.Context,
            cancellationToken).ConfigureAwait(false);
    }
}
