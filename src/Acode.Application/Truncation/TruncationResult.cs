namespace Acode.Application.Truncation;

/// <summary>
/// Represents the result of a truncation operation.
/// </summary>
public sealed class TruncationResult
{
    /// <summary>
    /// Gets the truncated content (or original if no truncation needed).
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the metadata describing the truncation operation.
    /// </summary>
    public required TruncationMetadata Metadata { get; init; }

    /// <summary>
    /// Gets the artifact reference string if content was stored as artifact.
    /// </summary>
    public string? ArtifactReference { get; init; }

    /// <summary>
    /// Gets a value indicating whether truncation was applied to the content.
    /// </summary>
    public bool WasTruncated => Metadata.WasTruncated;

    /// <summary>
    /// Gets the artifact ID if content was stored as artifact.
    /// </summary>
    public string? ArtifactId => Metadata.ArtifactId;

    /// <summary>
    /// Creates a result for content that was not truncated.
    /// </summary>
    /// <param name="content">The original content.</param>
    /// <returns>A truncation result indicating no truncation occurred.</returns>
    public static TruncationResult NotTruncated(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        return new TruncationResult
        {
            Content = content,
            Metadata = TruncationMetadata.NotTruncated(content.Length),
            ArtifactReference = null
        };
    }
}
