namespace Acode.Domain.Commands;

/// <summary>
/// Standard exit codes and their descriptions.
/// Provides semantic meaning for common command exit codes.
/// </summary>
/// <remarks>
/// Per Task 002.c FR-002c-95 through FR-002c-110.
/// Exit codes follow Unix conventions.
/// </remarks>
public static class ExitCodes
{
    /// <summary>
    /// Exit code 0: Success.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// Exit code 1: General error.
    /// </summary>
    public const int GeneralError = 1;

    /// <summary>
    /// Exit code 2: Misuse of shell command.
    /// </summary>
    public const int Misuse = 2;

    /// <summary>
    /// Exit code 124: Command timed out.
    /// </summary>
    public const int Timeout = 124;

    /// <summary>
    /// Exit code 126: Command found but not executable.
    /// </summary>
    public const int NotExecutable = 126;

    /// <summary>
    /// Exit code 127: Command not found.
    /// </summary>
    public const int NotFound = 127;

    /// <summary>
    /// Exit code 130: Terminated by Ctrl+C (SIGINT).
    /// </summary>
    public const int Interrupted = 130;

    /// <summary>
    /// Gets a human-readable description for an exit code.
    /// </summary>
    /// <param name="exitCode">The exit code to describe.</param>
    /// <returns>A description of what the exit code means.</returns>
    public static string GetDescription(int exitCode) => exitCode switch
    {
        Success => "Success",
        GeneralError => "General error",
        Misuse => "Misuse of command",
        Timeout => "Command timed out",
        NotExecutable => "Command not executable",
        NotFound => "Command not found",
        Interrupted => "Interrupted (Ctrl+C)",
        _ when exitCode > 128 => $"Killed by signal {exitCode - 128}",
        _ => $"Failed with exit code {exitCode}"
    };
}
