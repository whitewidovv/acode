namespace Acode.Cli.Events;

/// <summary>
/// Event emitted at the end of a CLI session.
/// </summary>
/// <remarks>
/// Contains exit code, duration, and summary statistics for the session.
/// </remarks>
public sealed record SessionEndEvent : BaseEvent
{
    /// <summary>
    /// Gets the exit code of the session.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Gets the session duration in milliseconds.
    /// </summary>
    public long DurationMs { get; init; }

    /// <summary>
    /// Gets the session summary statistics.
    /// </summary>
    public SessionSummary? Summary { get; init; }
}
