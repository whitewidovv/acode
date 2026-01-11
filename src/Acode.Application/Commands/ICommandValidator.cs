using Acode.Domain.Commands;

namespace Acode.Application.Commands;

/// <summary>
/// Interface for validating command specifications.
/// Validates working directory, timeout, retry count, and environment variables per Task 002.c FR-002c-51 through FR-002c-95.
/// </summary>
public interface ICommandValidator
{
    /// <summary>
    /// Validates an entire CommandSpec object.
    /// </summary>
    /// <param name="spec">The command specification to validate.</param>
    /// <param name="repositoryRoot">The repository root directory path.</param>
    /// <returns>A ValidationResult indicating success or failure with error message.</returns>
    ValidationResult Validate(CommandSpec spec, string repositoryRoot);

    /// <summary>
    /// Validates a working directory path.
    /// Must be relative to repository root, no path traversal, no absolute paths.
    /// Per FR-002c-51 through FR-002c-65.
    /// </summary>
    /// <param name="cwd">The working directory path (relative).</param>
    /// <param name="repositoryRoot">The repository root directory path.</param>
    /// <returns>A ValidationResult indicating success or failure with error message.</returns>
    ValidationResult ValidateWorkingDirectory(string cwd, string repositoryRoot);

    /// <summary>
    /// Validates a timeout value.
    /// Must be non-negative integer. Zero means no timeout. Default is 300 seconds.
    /// Per FR-002c-96 through FR-002c-110.
    /// </summary>
    /// <param name="timeoutSeconds">The timeout in seconds.</param>
    /// <returns>A ValidationResult indicating success or failure with error message.</returns>
    ValidationResult ValidateTimeout(int timeoutSeconds);

    /// <summary>
    /// Validates a retry count.
    /// Must be non-negative integer. Default is 0 (no retries). Max is 10.
    /// Per FR-002c-103 through FR-002c-110.
    /// </summary>
    /// <param name="retryCount">The number of retry attempts.</param>
    /// <returns>A ValidationResult indicating success or failure with error message.</returns>
    ValidationResult ValidateRetry(int retryCount);

    /// <summary>
    /// Validates environment variables dictionary.
    /// Names must be valid, values must be strings, empty values allowed.
    /// Per FR-002c-66 through FR-002c-80.
    /// </summary>
    /// <param name="env">The environment variables dictionary.</param>
    /// <returns>A ValidationResult indicating success or failure with error message.</returns>
    ValidationResult ValidateEnvironment(IReadOnlyDictionary<string, string> env);
}
