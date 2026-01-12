namespace Acode.Application.Audit.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Audit.Commands;
using Acode.Domain.Audit;

/// <summary>
/// Service for orchestrating audit operations.
/// Manages session lifecycle and coordinates audit commands.
/// </summary>
public sealed class AuditService
{
    private readonly StartAuditSessionCommandHandler? _startHandler;
    private readonly EndAuditSessionCommandHandler? _endHandler;
    private readonly LogEventCommandHandler? _logEventHandler;
    private readonly CleanupLogsCommandHandler? _cleanupHandler;

    private AuditSession? _activeSession;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditService"/> class.
    /// </summary>
    /// <param name="startHandler">Handler for starting sessions.</param>
    /// <param name="endHandler">Handler for ending sessions.</param>
    /// <param name="logEventHandler">Handler for logging events.</param>
    /// <param name="cleanupHandler">Handler for cleanup operations.</param>
    public AuditService(
        StartAuditSessionCommandHandler? startHandler = null,
        EndAuditSessionCommandHandler? endHandler = null,
        LogEventCommandHandler? logEventHandler = null,
        CleanupLogsCommandHandler? cleanupHandler = null)
    {
        _startHandler = startHandler;
        _endHandler = endHandler;
        _logEventHandler = logEventHandler;
        _cleanupHandler = cleanupHandler;
    }

    /// <summary>
    /// Starts a new audit session.
    /// </summary>
    /// <param name="operatingMode">The operating mode for the session.</param>
    /// <param name="source">The source component starting the session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The started audit session.</returns>
    public async Task<AuditSession> StartSessionAsync(
        string operatingMode,
        string source,
        CancellationToken cancellationToken = default)
    {
        if (_startHandler == null)
        {
            throw new InvalidOperationException("StartAuditSessionCommandHandler is required but was not provided.");
        }

        var command = new StartAuditSessionCommand(operatingMode, source);
        var session = await _startHandler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        _activeSession = session;

        return session;
    }

    /// <summary>
    /// Ends an active audit session.
    /// </summary>
    /// <param name="session">The session to end.</param>
    /// <param name="source">The source component ending the session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task EndSessionAsync(
        AuditSession session,
        string source,
        CancellationToken cancellationToken = default)
    {
        if (_endHandler == null)
        {
            throw new InvalidOperationException("EndAuditSessionCommandHandler is required but was not provided.");
        }

        var command = new EndAuditSessionCommand(session, source);
        await _endHandler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        if (_activeSession == session)
        {
            _activeSession = null;
        }
    }

    /// <summary>
    /// Logs an audit event.
    /// </summary>
    /// <param name="eventType">The type of event.</param>
    /// <param name="severity">The severity level.</param>
    /// <param name="source">The source component.</param>
    /// <param name="data">Event-specific data.</param>
    /// <param name="context">Optional context data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LogEventAsync(
        AuditEventType eventType,
        AuditSeverity severity,
        string source,
        IDictionary<string, object> data,
        IDictionary<string, object>? context = null,
        CancellationToken cancellationToken = default)
    {
        if (_logEventHandler == null)
        {
            throw new InvalidOperationException("LogEventCommandHandler is required but was not provided.");
        }

        var command = new LogEventCommand(eventType, severity, source, data, context);
        await _logEventHandler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Cleans up old audit logs.
    /// </summary>
    /// <param name="logDirectory">The directory containing audit logs.</param>
    /// <param name="retentionDays">The number of days to retain logs.</param>
    /// <param name="maxStorageBytes">Optional maximum storage in bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cleanup result.</returns>
    public async Task<CleanupLogsResult> CleanupLogsAsync(
        string logDirectory,
        int retentionDays,
        long? maxStorageBytes = null,
        CancellationToken cancellationToken = default)
    {
        if (_cleanupHandler == null)
        {
            throw new InvalidOperationException("CleanupLogsCommandHandler is required but was not provided.");
        }

        var command = new CleanupLogsCommand(logDirectory, retentionDays, maxStorageBytes);
        return await _cleanupHandler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the currently active session ID.
    /// </summary>
    /// <returns>The active session ID, or null if no session is active.</returns>
    public SessionId? GetActiveSessionId()
    {
        return _activeSession?.SessionId;
    }
}
