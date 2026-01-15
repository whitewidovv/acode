namespace Acode.Infrastructure.Truncation.Tools;

/// <summary>
/// Represents the result of a get_artifact tool invocation.
/// </summary>
/// <remarks>
/// Task-007c: Truncation + Artifact Attachment Rules.
/// Provides artifact content retrieval results with success/error handling.
/// </remarks>
public sealed class GetArtifactResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the artifact content (if successful).
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Gets the error message (if unsuccessful).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the artifact ID that was requested.
    /// </summary>
    public string? ArtifactId { get; init; }

    /// <summary>
    /// Gets the total line count in the artifact.
    /// </summary>
    public int TotalLines { get; init; }

    /// <summary>
    /// Gets the start line of retrieved content (if partial).
    /// </summary>
    public int? StartLine { get; init; }

    /// <summary>
    /// Gets the end line of retrieved content (if partial).
    /// </summary>
    public int? EndLine { get; init; }

    /// <summary>
    /// Creates a successful result with full content.
    /// </summary>
    /// <param name="content">The artifact content.</param>
    /// <param name="artifactId">The artifact ID.</param>
    /// <returns>A successful result.</returns>
    public static GetArtifactResult Success(string content, string artifactId)
    {
        ArgumentNullException.ThrowIfNull(content);

        var lines = content.Split('\n').Length;
        return new GetArtifactResult
        {
            IsSuccess = true,
            Content = content,
            ArtifactId = artifactId,
            TotalLines = lines
        };
    }

    /// <summary>
    /// Creates a successful result with partial content.
    /// </summary>
    /// <param name="content">The partial artifact content.</param>
    /// <param name="artifactId">The artifact ID.</param>
    /// <param name="startLine">The start line retrieved.</param>
    /// <param name="endLine">The end line retrieved.</param>
    /// <param name="totalLines">The total lines in the artifact.</param>
    /// <returns>A successful result with partial content.</returns>
    public static GetArtifactResult PartialSuccess(
        string content,
        string artifactId,
        int startLine,
        int endLine,
        int totalLines)
    {
        return new GetArtifactResult
        {
            IsSuccess = true,
            Content = content,
            ArtifactId = artifactId,
            StartLine = startLine,
            EndLine = endLine,
            TotalLines = totalLines
        };
    }

    /// <summary>
    /// Creates an error result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="artifactId">The artifact ID that was requested (if available).</param>
    /// <returns>An error result.</returns>
    public static GetArtifactResult Error(string errorMessage, string? artifactId = null)
    {
        return new GetArtifactResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ArtifactId = artifactId
        };
    }
}
