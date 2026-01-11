namespace Acode.Cli.Events;

/// <summary>
/// Event emitted when approval is requested from the user.
/// </summary>
public sealed record ApprovalRequestEvent : BaseEvent
{
    /// <summary>
    /// Gets the action type requiring approval.
    /// </summary>
    public required string ActionType { get; init; }

    /// <summary>
    /// Gets the context for the approval decision.
    /// </summary>
    public required string Context { get; init; }

    /// <summary>
    /// Gets the risk level (low, medium, high).
    /// </summary>
    public required string RiskLevel { get; init; }

    /// <summary>
    /// Gets the affected files, if applicable.
    /// </summary>
    public IReadOnlyList<string>? AffectedFiles { get; init; }

    /// <summary>
    /// Gets the proposed changes summary.
    /// </summary>
    public string? ProposedChanges { get; init; }
}
