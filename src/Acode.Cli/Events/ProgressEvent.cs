namespace Acode.Cli.Events;

/// <summary>
/// Event emitted to report progress during long-running operations.
/// </summary>
/// <remarks>
/// Enables progress bars, monitoring dashboards, and timeout handling.
/// </remarks>
public sealed record ProgressEvent : BaseEvent
{
    /// <summary>
    /// Gets the current step number.
    /// </summary>
    public required int Step { get; init; }

    /// <summary>
    /// Gets the total number of steps, if known.
    /// </summary>
    public int? Total { get; init; }

    /// <summary>
    /// Gets the completion percentage (0-100), if calculable.
    /// </summary>
    public int? Percentage { get; init; }

    /// <summary>
    /// Gets the estimated time remaining in seconds.
    /// </summary>
    public int? EtaSeconds { get; init; }

    /// <summary>
    /// Gets a human-readable progress message.
    /// </summary>
    public required string Message { get; init; }
}
