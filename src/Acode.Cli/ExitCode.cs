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
    /// Indicates that user input was required but not available.
    /// </summary>
    /// <remarks>
    /// Task 010.c FR-053: 10 MUST indicate input required.
    /// Used in non-interactive mode when input is needed but unavailable.
    /// </remarks>
    InputRequired = 10,

    /// <summary>
    /// Indicates that an operation timed out.
    /// </summary>
    /// <remarks>
    /// Task 010.c FR-054: 11 MUST indicate timeout.
    /// Used when configured timeout expires during operation.
    /// </remarks>
    Timeout = 11,

    /// <summary>
    /// Indicates that approval was denied by policy.
    /// </summary>
    /// <remarks>
    /// Task 010.c FR-055: 12 MUST indicate approval denied.
    /// Used when approval policy rejects an action in non-interactive mode.
    /// </remarks>
    ApprovalDenied = 12,

    /// <summary>
    /// Indicates that pre-flight checks failed.
    /// </summary>
    /// <remarks>
    /// Task 010.c FR-056: 13 MUST indicate pre-flight check failed.
    /// Used when configuration, model, or permission checks fail.
    /// </remarks>
    PreflightFailed = 13,

    /// <summary>
    /// Indicates the process received SIGINT (Ctrl+C).
    /// </summary>
    /// <remarks>
    /// FR-042: 130 MUST indicate SIGINT (Ctrl+C).
    /// Standard Unix convention: 128 + signal number (SIGINT = 2).
    /// </remarks>
    SignalInterrupt = 130,
}
