using Acode.Domain.Audit;

namespace Acode.Application.Audit;

/// <summary>
/// Service for logging audit events to persistent storage.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs an audit event asynchronously.
    /// </summary>
    /// <param name="auditEvent">The audit event to log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes any buffered audit events to persistent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
