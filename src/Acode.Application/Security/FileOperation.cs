namespace Acode.Application.Security;

/// <summary>
/// File system operations that may be restricted by path protection.
/// </summary>
public enum FileOperation
{
    /// <summary>
    /// Reading file contents.
    /// </summary>
    Read,

    /// <summary>
    /// Writing or modifying file contents.
    /// </summary>
    Write,

    /// <summary>
    /// Deleting files or directories.
    /// </summary>
    Delete,

    /// <summary>
    /// Listing directory contents.
    /// </summary>
    List
}
