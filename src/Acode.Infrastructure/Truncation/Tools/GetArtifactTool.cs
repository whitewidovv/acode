namespace Acode.Infrastructure.Truncation.Tools;

using Acode.Application.Truncation;

/// <summary>
/// Tool for retrieving artifact content stored during truncation.
/// </summary>
/// <remarks>
/// Task-007c: Truncation + Artifact Attachment Rules.
/// Provides the get_artifact tool for model to retrieve full content
/// from artifacts created when large outputs were truncated.
/// Supports partial retrieval via line ranges for efficiency.
/// </remarks>
public sealed class GetArtifactTool
{
    private readonly IArtifactStore artifactStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetArtifactTool"/> class.
    /// </summary>
    /// <param name="artifactStore">The artifact store to retrieve from.</param>
    public GetArtifactTool(IArtifactStore artifactStore)
    {
        ArgumentNullException.ThrowIfNull(artifactStore);
        this.artifactStore = artifactStore;
    }

    /// <summary>
    /// Gets the tool name.
    /// </summary>
    public static string ToolName => "get_artifact";

    /// <summary>
    /// Retrieves artifact content by ID with optional line range.
    /// </summary>
    /// <param name="artifactId">The artifact ID to retrieve.</param>
    /// <param name="startLine">Optional start line (1-indexed, inclusive).</param>
    /// <param name="endLine">Optional end line (1-indexed, inclusive).</param>
    /// <returns>The retrieval result with content or error.</returns>
    public async Task<GetArtifactResult> GetAsync(
        string artifactId,
        int? startLine = null,
        int? endLine = null)
    {
        // Validate artifact ID
        if (string.IsNullOrWhiteSpace(artifactId))
        {
            return GetArtifactResult.Error("Artifact ID is required.");
        }

        // Validate ID format (security check)
        if (!IsValidArtifactId(artifactId))
        {
            return GetArtifactResult.Error($"Invalid artifact ID format: {artifactId}");
        }

        // Check if artifact exists
        if (!await this.artifactStore.ExistsAsync(artifactId).ConfigureAwait(false))
        {
            return GetArtifactResult.Error($"Artifact not found: {artifactId}", artifactId);
        }

        // Retrieve content
        if (startLine.HasValue || endLine.HasValue)
        {
            return await this.GetPartialContentAsync(artifactId, startLine, endLine).ConfigureAwait(false);
        }

        return await this.GetFullContentAsync(artifactId).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates artifact ID format to prevent security issues.
    /// </summary>
    private static bool IsValidArtifactId(string artifactId)
    {
        // Check for path traversal attempts
        if (artifactId.Contains("..", StringComparison.Ordinal) ||
            artifactId.Contains('/', StringComparison.Ordinal) ||
            artifactId.Contains('\\', StringComparison.Ordinal))
        {
            return false;
        }

        // Must start with "art_" prefix
        if (!artifactId.StartsWith("art_", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Retrieves the full artifact content.
    /// </summary>
    private async Task<GetArtifactResult> GetFullContentAsync(string artifactId)
    {
        var content = await this.artifactStore.GetContentAsync(artifactId).ConfigureAwait(false);

        if (content is null)
        {
            return GetArtifactResult.Error($"Failed to retrieve artifact content: {artifactId}", artifactId);
        }

        return GetArtifactResult.Success(content, artifactId);
    }

    /// <summary>
    /// Retrieves partial artifact content by line range.
    /// </summary>
    private async Task<GetArtifactResult> GetPartialContentAsync(
        string artifactId,
        int? startLine,
        int? endLine)
    {
        // Get full content first to determine total lines
        var fullContent = await this.artifactStore.GetContentAsync(artifactId).ConfigureAwait(false);

        if (fullContent is null)
        {
            return GetArtifactResult.Error($"Failed to retrieve artifact content: {artifactId}", artifactId);
        }

        var lines = fullContent.Split('\n');
        var totalLines = lines.Length;

        // Default start to 1, end to total lines
        var start = startLine ?? 1;
        var end = endLine ?? totalLines;

        // Clamp values to valid range
        start = Math.Max(1, Math.Min(start, totalLines));
        end = Math.Max(start, Math.Min(end, totalLines));

        // Extract requested lines (convert to 0-indexed)
        var selectedLines = lines.Skip(start - 1).Take(end - start + 1);
        var partialContent = string.Join('\n', selectedLines);

        return GetArtifactResult.PartialSuccess(partialContent, artifactId, start, end, totalLines);
    }
}
