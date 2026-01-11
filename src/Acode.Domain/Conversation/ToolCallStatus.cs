// src/Acode.Domain/Conversation/ToolCallStatus.cs
namespace Acode.Domain.Conversation;

/// <summary>
/// Status of a tool call within a message.
/// </summary>
public enum ToolCallStatus
{
    /// <summary>Tool call is pending execution.</summary>
    Pending,

    /// <summary>Tool call is currently running.</summary>
    Running,

    /// <summary>Tool call completed successfully.</summary>
    Completed,

    /// <summary>Tool call failed with error.</summary>
    Failed,
}
