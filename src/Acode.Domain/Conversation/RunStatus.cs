// src/Acode.Domain/Conversation/RunStatus.cs
namespace Acode.Domain.Conversation;

/// <summary>
/// Status of a Run (request/response cycle).
/// </summary>
public enum RunStatus
{
    /// <summary>Run is currently executing.</summary>
    Running,

    /// <summary>Run completed successfully.</summary>
    Completed,

    /// <summary>Run failed with error.</summary>
    Failed,

    /// <summary>Run was cancelled by user.</summary>
    Cancelled,
}
