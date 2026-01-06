namespace Acode.Application.Truncation;

/// <summary>
/// Metadata describing the truncation operation performed on content.
/// </summary>
public sealed class TruncationMetadata
{
    /// <summary>
    /// Gets or sets the original content size in characters.
    /// </summary>
    public int OriginalSize { get; set; }

    /// <summary>
    /// Gets or sets the truncated content size in characters.
    /// </summary>
    public int TruncatedSize { get; set; }

    /// <summary>
    /// Gets the estimated token count for original content (chars ÷ 4).
    /// </summary>
    public int OriginalTokenEstimate => OriginalSize / 4;

    /// <summary>
    /// Gets the estimated token count for truncated content (chars ÷ 4).
    /// </summary>
    public int TruncatedTokenEstimate => TruncatedSize / 4;

    /// <summary>
    /// Gets or sets a value indicating whether truncation was applied.
    /// </summary>
    public bool WasTruncated { get; set; }

    /// <summary>
    /// Gets or sets the truncation strategy used.
    /// </summary>
    public TruncationStrategy StrategyUsed { get; set; }

    /// <summary>
    /// Gets or sets the number of characters omitted.
    /// </summary>
    public int OmittedCharacters { get; set; }

    /// <summary>
    /// Gets or sets the number of lines omitted (if line-based truncation).
    /// </summary>
    public int OmittedLines { get; set; }

    /// <summary>
    /// Gets or sets the number of elements omitted (if element-based truncation).
    /// </summary>
    public int OmittedElements { get; set; }

    /// <summary>
    /// Gets or sets the artifact ID if content was stored as artifact.
    /// </summary>
    public string? ArtifactId { get; set; }

    /// <summary>
    /// Gets a value indicating whether an artifact was created.
    /// </summary>
    public bool ArtifactCreated => ArtifactId is not null;

    /// <summary>
    /// Creates metadata for content that was not truncated.
    /// </summary>
    /// <param name="contentLength">The content length in characters.</param>
    /// <returns>Metadata indicating no truncation occurred.</returns>
    public static TruncationMetadata NotTruncated(int contentLength)
    {
        return new TruncationMetadata
        {
            OriginalSize = contentLength,
            TruncatedSize = contentLength,
            WasTruncated = false,
            StrategyUsed = TruncationStrategy.None,
            OmittedCharacters = 0,
            OmittedLines = 0,
            OmittedElements = 0,
            ArtifactId = null
        };
    }
}
