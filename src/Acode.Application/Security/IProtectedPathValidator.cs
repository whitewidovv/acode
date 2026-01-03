namespace Acode.Application.Security;

/// <summary>
/// Service for validating if file paths are protected by the denylist.
/// </summary>
public interface IProtectedPathValidator
{
    /// <summary>
    /// Checks if the given path is protected by the denylist.
    /// </summary>
    /// <param name="path">The path to check (may be relative or absolute).</param>
    /// <returns>Result indicating if path is protected and why.</returns>
    PathValidationResult Validate(string path);

    /// <summary>
    /// Checks if the given path is protected, with operation context.
    /// </summary>
    /// <param name="path">The path to check (may be relative or absolute).</param>
    /// <param name="operation">The operation being performed.</param>
    /// <returns>Result indicating if path is protected and why.</returns>
    PathValidationResult Validate(string path, FileOperation operation);
}
