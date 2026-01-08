// src/Acode.Domain/Sync/OutboxStatus.cs
namespace Acode.Domain.Sync;

/// <summary>
/// Status of an outbox entry in the sync pipeline.
/// </summary>
public enum OutboxStatus
{
    /// <summary>
    /// Entry is pending and ready to be processed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Entry is currently being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Entry has been successfully synced.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Entry has exceeded retry limit and moved to dead letter queue.
    /// </summary>
    DeadLetter = 3
}
