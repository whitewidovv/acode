namespace Acode.Application.Truncation;

/// <summary>
/// Represents a stored artifact containing large content.
/// </summary>
public sealed class Artifact
{
    /// <summary>
    /// Gets the unique artifact identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the content size in bytes.
    /// </summary>
    public required long Size { get; init; }

    /// <summary>
    /// Gets the content type (e.g., "text/plain", "application/json").
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Gets the name of the tool that produced this artifact.
    /// </summary>
    public required string SourceTool { get; init; }

    /// <summary>
    /// Gets the timestamp when the artifact was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the file path where the artifact is stored.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the preview content (first N characters).
    /// </summary>
    public string? Preview { get; init; }

    /// <summary>
    /// Gets the estimated token count (size ÷ 4).
    /// </summary>
    public long TokenEstimate => Size / 4;
}
