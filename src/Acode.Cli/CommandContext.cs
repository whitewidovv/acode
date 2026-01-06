namespace Acode.Cli;

/// <summary>
/// Context passed to command handlers during execution.
/// </summary>
/// <remarks>
/// CommandContext provides access to resolved configuration, output stream,
/// and cancellation tokens for command execution. This record is immutable
/// to ensure consistent state throughout command execution.
/// </remarks>
public sealed record CommandContext
{
    /// <summary>
    /// Gets the resolved configuration for this command invocation.
    /// </summary>
    /// <remarks>
    /// Configuration is merged from CLI arguments, environment variables,
    /// configuration file, and defaults (in that precedence order).
    /// </remarks>
    public required IReadOnlyDictionary<string, object> Configuration { get; init; }

    /// <summary>
    /// Gets the command-line arguments passed to the command (excluding the command name itself).
    /// </summary>
    /// <remarks>
    /// For "acode config validate --verbose", Args would be ["validate", "--verbose"].
    /// Commands can parse these arguments to handle subcommands and options.
    /// </remarks>
    public required string[] Args { get; init; }

    /// <summary>
    /// Gets the output formatter for command output.
    /// </summary>
    /// <remarks>
    /// Commands use this formatter to write output. The formatter is selected
    /// based on --json flag and TTY detection in Program.cs.
    /// - --json → JsonLinesFormatter.
    /// - TTY detected → ConsoleFormatter with colors.
    /// - No TTY → ConsoleFormatter without colors.
    /// </remarks>
    public required IOutputFormatter Formatter { get; init; }

    /// <summary>
    /// Gets the output writer for command output.
    /// </summary>
    /// <remarks>
    /// Commands write all output (human-readable or JSONL) to this writer.
    /// Typically stdout, but can be redirected for testing.
    /// </remarks>
    public required TextWriter Output { get; init; }

    /// <summary>
    /// Gets the cancellation token for cooperative cancellation.
    /// </summary>
    /// <remarks>
    /// Commands should check this token periodically during long-running
    /// operations and gracefully cancel when requested (e.g., Ctrl+C).
    /// </remarks>
    public required CancellationToken CancellationToken { get; init; }
}
