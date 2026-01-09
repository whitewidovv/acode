// src/Acode.Domain/Conversation/Message.cs
namespace Acode.Domain.Conversation;

using Acode.Domain.Common;

/// <summary>
/// Message entity representing a single exchange within a Run.
/// Temporary stub - will be fully implemented in Phase 6.
/// </summary>
public sealed class Message : Entity<MessageId>
{
    /// <summary>
    /// Gets the parent Run ID.
    /// </summary>
    public RunId RunId { get; private set; }

    // Full implementation in Phase 6
}
