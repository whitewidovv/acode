// src/Acode.Domain/Conversation/SyncStatus.cs
namespace Acode.Domain.Conversation;

/// <summary>
/// Tracks synchronization state between local and remote storage.
/// </summary>
public enum SyncStatus
{
    /// <summary>Created locally, not yet synced to remote.</summary>
    Pending,

    /// <summary>Successfully synced with remote.</summary>
    Synced,

    /// <summary>Local and remote versions conflict.</summary>
    Conflict,

    /// <summary>Sync failed after retries.</summary>
    Failed,
}
