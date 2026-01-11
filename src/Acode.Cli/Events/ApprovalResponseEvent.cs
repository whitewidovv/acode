namespace Acode.Cli.Events;

/// <summary>
/// Event emitted when approval is given or denied.
/// </summary>
public sealed record ApprovalResponseEvent : BaseEvent
{
    /// <summary>
    /// Gets a value indicating whether the request was approved.
    /// </summary>
    public required bool Approved { get; init; }

    /// <summary>
    /// Gets an optional reason for the decision.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets the approval source (user, auto, policy).
    /// </summary>
    public string? Source { get; init; }
}
