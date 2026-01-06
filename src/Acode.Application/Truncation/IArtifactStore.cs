namespace Acode.Application.Truncation;

/// <summary>
/// Interface for artifact storage operations.
/// </summary>
public interface IArtifactStore
{
    /// <summary>
    /// Creates a new artifact with the given content.
    /// </summary>
    /// <param name="content">The content to store.</param>
    /// <param name="sourceTool">The name of the tool that produced the content.</param>
    /// <param name="contentType">The content type (e.g., "text/plain").</param>
    /// <returns>The created artifact metadata.</returns>
    Task<Artifact> CreateAsync(string content, string sourceTool, string contentType);

    /// <summary>
    /// Retrieves the full content of an artifact by ID.
    /// </summary>
    /// <param name="artifactId">The artifact ID to retrieve.</param>
    /// <returns>The artifact content, or null if not found.</returns>
    Task<string?> GetContentAsync(string artifactId);

    /// <summary>
    /// Retrieves a partial content range from an artifact.
    /// </summary>
    /// <param name="artifactId">The artifact ID.</param>
    /// <param name="startLine">The starting line (1-based, inclusive).</param>
    /// <param name="endLine">The ending line (1-based, inclusive).</param>
    /// <returns>The partial content, or null if not found.</returns>
    Task<string?> GetPartialContentAsync(string artifactId, int startLine, int endLine);

    /// <summary>
    /// Gets artifact metadata by ID.
    /// </summary>
    /// <param name="artifactId">The artifact ID.</param>
    /// <returns>The artifact metadata, or null if not found.</returns>
    Task<Artifact?> GetMetadataAsync(string artifactId);

    /// <summary>
    /// Lists all artifacts in the current session.
    /// </summary>
    /// <returns>Collection of artifact metadata.</returns>
    Task<IReadOnlyCollection<Artifact>> ListAsync();

    /// <summary>
    /// Deletes an artifact by ID.
    /// </summary>
    /// <param name="artifactId">The artifact ID to delete.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(string artifactId);

    /// <summary>
    /// Cleans up all artifacts in the session.
    /// </summary>
    /// <returns>The number of artifacts cleaned up.</returns>
    Task<int> CleanupAsync();

    /// <summary>
    /// Checks if an artifact exists.
    /// </summary>
    /// <param name="artifactId">The artifact ID.</param>
    /// <returns>True if exists, false otherwise.</returns>
    Task<bool> ExistsAsync(string artifactId);
}
