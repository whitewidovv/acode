using Acode.Domain.Audit;

namespace Acode.Application.Audit;

/// <summary>
/// Service for logging audit events to persistent storage.
/// All operations MUST be non-blocking.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs an audit event asynchronously.
    /// MUST NOT block the calling thread.
    /// </summary>
    /// <param name="auditEvent">The audit event to log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an audit event with automatic timestamp and session context.
    /// Convenience method that constructs AuditEvent internally.
    /// </summary>
    /// <param name="eventType">The type of audit event.</param>
    /// <param name="severity">The severity level.</param>
    /// <param name="source">The source component generating the event.</param>
    /// <param name="data">Event-specific data.</param>
    /// <param name="context">Additional context information (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task LogAsync(
        AuditEventType eventType,
        AuditSeverity severity,
        string source,
        IDictionary<string, object> data,
        IDictionary<string, object>? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a new correlation scope.
    /// Events logged within the scope share the same correlation ID.
    /// </summary>
    /// <param name="description">Description of the correlation scope.</param>
    /// <returns>Disposable that ends the correlation scope when disposed.</returns>
    IDisposable BeginCorrelation(string description);

    /// <summary>
    /// Starts a new span within the current correlation.
    /// Enables hierarchical tracing of operations.
    /// </summary>
    /// <param name="operation">Name of the operation being traced.</param>
    /// <returns>Disposable that ends the span when disposed.</returns>
    IDisposable BeginSpan(string operation);

    /// <summary>
    /// Flushes any buffered audit events to persistent storage.
    /// MUST be called during graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
