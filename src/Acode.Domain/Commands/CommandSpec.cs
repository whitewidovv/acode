namespace Acode.Domain.Commands;

/// <summary>
/// Immutable specification for a command to be executed.
/// Supports string, array, and object formats from config.
/// </summary>
/// <remarks>
/// Per Task 002.c FR-002c-31 through FR-002c-50.
/// This is the domain model - execution is in Application layer.
/// </remarks>
public sealed record CommandSpec
{
    /// <summary>
    /// Gets the command to execute.
    /// Required field containing the shell command.
    /// </summary>
    public required string Run { get; init; }

    /// <summary>
    /// Gets the working directory for command execution.
    /// Relative to repository root. Default is "." (current directory).
    /// Maps to "cwd" in YAML configuration.
    /// </summary>
    public string Cwd { get; init; } = ".";

    /// <summary>
    /// Gets the environment variables to set for this command.
    /// These override inherited process environment variables.
    /// Maps to "env" in YAML configuration.
    /// </summary>
    public IReadOnlyDictionary<string, string> Env { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets the timeout in seconds for command execution.
    /// Default is 300 seconds (5 minutes). Zero means no timeout.
    /// Maps to "timeout" in YAML configuration.
    /// </summary>
    public int Timeout { get; init; } = 300;

    /// <summary>
    /// Gets the number of retry attempts on failure.
    /// Default is 0 (no retries). Uses exponential backoff.
    /// Maps to "retry" in YAML configuration.
    /// </summary>
    public int Retry { get; init; } = 0;

    /// <summary>
    /// Gets a value indicating whether to continue execution if this command fails.
    /// Default is false (stop on error).
    /// </summary>
    public bool ContinueOnError { get; init; } = false;

    /// <summary>
    /// Gets platform-specific command variants.
    /// Maps platform name to command string (e.g., "windows" -> "cmd /c build.bat").
    /// Null means no platform variants.
    /// </summary>
    public IReadOnlyDictionary<string, string>? PlatformVariants { get; init; }
}
