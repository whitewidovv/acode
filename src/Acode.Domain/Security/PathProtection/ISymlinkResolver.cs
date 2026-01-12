namespace Acode.Domain.Security.PathProtection;

/// <summary>
/// Resolves symbolic links to their real target paths.
/// SECURITY: Prevents symlink bypass attacks where malicious actors
/// use symlinks to circumvent path protection.
/// </summary>
public interface ISymlinkResolver
{
    /// <summary>
    /// Resolves a path that may be a symlink to its real target.
    /// Handles symlink chains (a → b → c) and detects circular references.
    /// </summary>
    /// <param name="path">Path that may be a symlink.</param>
    /// <returns>
    /// Resolution result containing:
    /// - ResolvedPath: Final target path if successful
    /// - IsSuccess: True if resolution succeeded
    /// - Error: Error code if resolution failed
    /// - Depth: Number of symlinks traversed.
    /// </returns>
    SymlinkResolutionResult Resolve(string path);
}
