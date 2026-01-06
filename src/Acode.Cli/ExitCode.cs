namespace Acode.Cli;

/// <summary>
/// Standard exit codes for the CLI application.
/// </summary>
/// <remarks>
/// Exit codes follow Unix conventions and are documented for scripting and automation.
/// FR-036 through FR-043: Exit code definitions and documentation.
/// </remarks>
public enum ExitCode
{
    /// <summary>
    /// Indicates successful completion.
    /// </summary>
    /// <remarks>
    /// FR-036: 0 MUST indicate success.
    /// </remarks>
    Success = 0,

    /// <summary>
    /// Indicates a general error occurred.
    /// </summary>
    /// <remarks>
    /// FR-037: 1 MUST indicate general error.
    /// Use this when no more specific error code applies.
    /// </remarks>
    GeneralError = 1,

    /// <summary>
    /// Indicates invalid command-line arguments were provided.
    /// </summary>
    /// <remarks>
    /// FR-038: 2 MUST indicate invalid arguments.
    /// Examples: unknown option, missing required argument, invalid value.
    /// </remarks>
    InvalidArguments = 2,

    /// <summary>
    /// Indicates a configuration error.
    /// </summary>
    /// <remarks>
    /// FR-039: 3 MUST indicate configuration error.
    /// Examples: invalid YAML syntax, missing config file, invalid values.
    /// </remarks>
    ConfigurationError = 3,

    /// <summary>
    /// Indicates a runtime error occurred during command execution.
    /// </summary>
    /// <remarks>
    /// FR-040: 4 MUST indicate runtime error.
    /// Examples: model unavailable, operation failed, permission denied.
    /// </remarks>
    RuntimeError = 4,

    /// <summary>
    /// Indicates the user cancelled the operation.
    /// </summary>
    /// <remarks>
    /// FR-041: 5 MUST indicate user cancellation.
    /// User explicitly chose to cancel via prompt or approval gate.
    /// </remarks>
    UserCancellation = 5,

    /// <summary>
    /// Indicates the process received SIGINT (Ctrl+C).
    /// </summary>
    /// <remarks>
    /// FR-042: 130 MUST indicate SIGINT (Ctrl+C).
    /// Standard Unix convention: 128 + signal number (SIGINT = 2).
    /// </remarks>
    SignalInterrupt = 130,
}
