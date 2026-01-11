using System.Text.RegularExpressions;
using Acode.Domain.Commands;

namespace Acode.Application.Commands;

/// <summary>
/// Validates command specifications per Task 002.c FR-002c-51 through FR-002c-95.
/// Ensures working directories are safe, timeouts are valid, retry counts are reasonable,
/// and environment variables follow naming conventions.
/// </summary>
public sealed partial class CommandValidator : ICommandValidator
{
    private const int MaxRetryCount = 10;

    /// <inheritdoc/>
    public ValidationResult Validate(CommandSpec spec, string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(repositoryRoot);

        // Validate working directory
        var cwdResult = ValidateWorkingDirectory(spec.Cwd, repositoryRoot);
        if (!cwdResult.IsValid)
        {
            return ValidationResult.Failure($"Invalid working directory: {cwdResult.ErrorMessage}");
        }

        // Validate timeout
        var timeoutResult = ValidateTimeout(spec.Timeout);
        if (!timeoutResult.IsValid)
        {
            return ValidationResult.Failure($"Invalid timeout: {timeoutResult.ErrorMessage}");
        }

        // Validate retry count
        var retryResult = ValidateRetry(spec.Retry);
        if (!retryResult.IsValid)
        {
            return ValidationResult.Failure($"Invalid retry count: {retryResult.ErrorMessage}");
        }

        // Validate environment variables
        var envResult = ValidateEnvironment(spec.Env);
        if (!envResult.IsValid)
        {
            return ValidationResult.Failure($"Invalid environment variables: {envResult.ErrorMessage}");
        }

        return ValidationResult.Success();
    }

    /// <inheritdoc/>
    public ValidationResult ValidateWorkingDirectory(string cwd, string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(cwd);
        ArgumentNullException.ThrowIfNull(repositoryRoot);

        // Check for absolute paths (Unix: starts with /, Windows: starts with drive letter)
        if (Path.IsPathRooted(cwd) || IsWindowsAbsolutePath(cwd))
        {
            return ValidationResult.Failure(
                "Working directory must be relative to repository root, not an absolute path. " +
                $"Got: '{cwd}'");
        }

        // Check for path traversal (..)
        if (cwd.Contains("..", StringComparison.Ordinal))
        {
            return ValidationResult.Failure(
                "Working directory cannot contain path traversal (..). " +
                $"Got: '{cwd}'");
        }

        return ValidationResult.Success();
    }

    /// <inheritdoc/>
    public ValidationResult ValidateTimeout(int timeoutSeconds)
    {
        if (timeoutSeconds < 0)
        {
            return ValidationResult.Failure(
                $"Timeout must be non-negative. Zero means no timeout. Got: {timeoutSeconds}");
        }

        return ValidationResult.Success();
    }

    /// <inheritdoc/>
    public ValidationResult ValidateRetry(int retryCount)
    {
        if (retryCount < 0)
        {
            return ValidationResult.Failure(
                $"Retry count must be non-negative. Got: {retryCount}");
        }

        if (retryCount > MaxRetryCount)
        {
            return ValidationResult.Failure(
                $"Retry count exceeds maximum of {MaxRetryCount}. Got: {retryCount}");
        }

        return ValidationResult.Success();
    }

    /// <inheritdoc/>
    public ValidationResult ValidateEnvironment(IReadOnlyDictionary<string, string> env)
    {
        ArgumentNullException.ThrowIfNull(env);

        foreach (var (name, value) in env)
        {
            // Validate environment variable name
            if (!IsValidEnvironmentVariableName(name))
            {
                return ValidationResult.Failure(
                    $"Invalid environment variable name: '{name}'. " +
                    "Names must start with a letter or underscore and contain only letters, digits, and underscores.");
            }
        }

        return ValidationResult.Success();
    }

    private static bool IsValidEnvironmentVariableName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        // Environment variable names:
        // - Must start with letter or underscore
        // - Can contain letters, digits, underscores
        // - Cannot contain =, space, or other special characters
        return EnvVarNameRegex().IsMatch(name);
    }

    [GeneratedRegex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex EnvVarNameRegex();

    private static bool IsWindowsAbsolutePath(string path)
    {
        // Check for Windows absolute paths like C:\, D:\, etc.
        // Pattern: single letter followed by colon
        return path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':';
    }
}
