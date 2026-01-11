namespace Acode.Domain.Commands;

/// <summary>
/// Standard logging field names for command execution logging.
/// Provides consistent field names for structured logging across command execution.
/// </summary>
/// <remarks>
/// Per Task 002.c spec lines 1106-1121.
/// These constants enable structured logging with consistent field names.
/// </remarks>
public static class CommandLogFields
{
    /// <summary>
    /// Logging field name for the command group (setup, build, test, lint, format, start).
    /// </summary>
    public const string CommandGroup = "command_group";

    /// <summary>
    /// Logging field name for the command string being executed.
    /// </summary>
    public const string Command = "command";

    /// <summary>
    /// Logging field name for the working directory where command executes.
    /// </summary>
    public const string WorkingDirectory = "working_directory";

    /// <summary>
    /// Logging field name for the exit code returned by the command.
    /// </summary>
    public const string ExitCode = "exit_code";

    /// <summary>
    /// Logging field name for the command duration in milliseconds.
    /// </summary>
    public const string DurationMs = "duration_ms";

    /// <summary>
    /// Logging field name for the attempt number (1-based, includes retries).
    /// </summary>
    public const string Attempt = "attempt";

    /// <summary>
    /// Logging field name for timeout status (boolean).
    /// </summary>
    public const string TimedOut = "timed_out";

    /// <summary>
    /// Logging field name for the platform (windows, linux, macos).
    /// </summary>
    public const string Platform = "platform";

    /// <summary>
    /// Logging field name for the count of environment variables set.
    /// </summary>
    public const string EnvVarCount = "env_var_count";
}
