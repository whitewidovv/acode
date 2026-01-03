namespace Acode.Domain.Commands;

/// <summary>
/// Immutable record of command execution result.
/// Contains exit code, output, timing, and retry information.
/// </summary>
/// <remarks>
/// Per Task 002.c command execution requirements.
/// This is the domain model for execution results.
/// </remarks>
public sealed record CommandResult
{
    /// <summary>
    /// Gets the exit code returned by the command.
    /// Zero indicates success, non-zero indicates failure.
    /// </summary>
    public required int ExitCode { get; init; }

    /// <summary>
    /// Gets the standard output captured from the command.
    /// Never null, empty string if no output.
    /// </summary>
    public required string Stdout { get; init; }

    /// <summary>
    /// Gets the standard error captured from the command.
    /// Never null, empty string if no error output.
    /// </summary>
    public required string Stderr { get; init; }

    /// <summary>
    /// Gets the total duration of command execution.
    /// Includes all retry attempts.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets a value indicating whether the command timed out.
    /// When true, ExitCode should be 124.
    /// </summary>
    public required bool TimedOut { get; init; }

    /// <summary>
    /// Gets the number of attempts made to execute this command.
    /// Minimum value is 1 (initial attempt). Includes retries.
    /// </summary>
    public required int AttemptCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the command succeeded.
    /// True when exit code is zero.
    /// </summary>
    public bool Success => ExitCode == 0;
}
