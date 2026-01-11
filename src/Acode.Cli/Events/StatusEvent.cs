namespace Acode.Cli.Events;

/// <summary>
/// Event emitted for status changes.
/// </summary>
public sealed record StatusEvent : BaseEvent
{
    /// <summary>
    /// Gets the status name.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets an optional status message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets optional additional data.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Data { get; init; }
}
