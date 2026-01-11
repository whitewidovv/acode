namespace Acode.Infrastructure.Audit;

using Acode.Domain.Audit;

/// <summary>
/// Interface for writing audit events to persistent storage.
/// </summary>
public interface IAuditWriter : IAsyncDisposable
{
    /// <summary>
    /// Writes an audit event to storage.
    /// </summary>
    /// <param name="auditEvent">The audit event to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes any buffered writes to disk.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
