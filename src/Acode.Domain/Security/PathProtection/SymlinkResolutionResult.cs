namespace Acode.Domain.Security.PathProtection;

/// <summary>
/// Result of symlink resolution operation.
/// </summary>
public sealed record SymlinkResolutionResult
{
    /// <summary>
    /// Gets a value indicating whether resolution succeeded.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the resolved path (original path if not a symlink, or final target if symlink).
    /// Null if resolution failed.
    /// </summary>
    public string? ResolvedPath { get; init; }

    /// <summary>
    /// Gets the error that occurred during resolution (None if successful).
    /// </summary>
    public required SymlinkError Error { get; init; }

    /// <summary>
    /// Gets the number of symlinks traversed during resolution.
    /// 0 if path is not a symlink.
    /// </summary>
    public required int Depth { get; init; }

    /// <summary>
    /// Creates a successful resolution result.
    /// </summary>
    /// <param name="path">The resolved path.</param>
    /// <param name="depth">Number of symlinks traversed.</param>
    /// <returns>Success result.</returns>
    public static SymlinkResolutionResult Success(string path, int depth) =>
        new()
        {
            IsSuccess = true,
            ResolvedPath = path,
            Error = SymlinkError.None,
            Depth = depth,
        };

    /// <summary>
    /// Creates a failed resolution result.
    /// </summary>
    /// <param name="error">The error that occurred.</param>
    /// <returns>Failure result.</returns>
    public static SymlinkResolutionResult Failure(SymlinkError error) =>
        new()
        {
            IsSuccess = false,
            ResolvedPath = null,
            Error = error,
            Depth = 0,
        };
}
