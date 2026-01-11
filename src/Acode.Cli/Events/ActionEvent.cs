namespace Acode.Cli.Events;

/// <summary>
/// Event emitted when an action is taken.
/// </summary>
public sealed record ActionEvent : BaseEvent
{
    /// <summary>
    /// Gets the action type.
    /// </summary>
    public required string ActionType { get; init; }

    /// <summary>
    /// Gets a description of the action.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets affected files, if applicable.
    /// </summary>
    public IReadOnlyList<string>? AffectedFiles { get; init; }

    /// <summary>
    /// Gets a value indicating whether the action succeeded.
    /// </summary>
    public bool? Success { get; init; }
}
