// src/Acode.Infrastructure/Persistence/Migrations/IFileSystem.cs
namespace Acode.Infrastructure.Persistence.Migrations;

/// <summary>
/// Abstraction for file system operations used during migration discovery.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Gets all files matching the specified pattern in a directory.
    /// </summary>
    /// <param name="directory">The directory to search.</param>
    /// <param name="pattern">The search pattern (e.g., "*.sql").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Array of file paths matching the pattern.</returns>
    Task<string[]> GetFilesAsync(string directory, string pattern, CancellationToken ct = default);

    /// <summary>
    /// Reads all text content from a file.
    /// </summary>
    /// <param name="path">The file path to read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The file content as a string.</returns>
    Task<string> ReadAllTextAsync(string path, CancellationToken ct = default);
}
