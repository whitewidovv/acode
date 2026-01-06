namespace Acode.Application.Truncation;

/// <summary>
/// Configuration limits for truncation processing.
/// </summary>
public sealed class TruncationLimits
{
    /// <summary>
    /// Gets the default inline character limit (8000 characters).
    /// </summary>
    public const int DefaultInlineLimit = 8000;

    /// <summary>
    /// Gets the default artifact threshold (50000 characters).
    /// </summary>
    public const int DefaultArtifactThreshold = 50000;

    /// <summary>
    /// Gets the default maximum artifact size (10MB).
    /// </summary>
    public const int DefaultMaxArtifactSize = 10 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the inline limit in characters. Content under this limit is not truncated.
    /// </summary>
    public int InlineLimit { get; set; } = DefaultInlineLimit;

    /// <summary>
    /// Gets or sets the artifact threshold in characters.
    /// Content exceeding this becomes an artifact.
    /// </summary>
    public int ArtifactThreshold { get; set; } = DefaultArtifactThreshold;

    /// <summary>
    /// Gets or sets the maximum artifact size in bytes.
    /// </summary>
    public int MaxArtifactSize { get; set; } = DefaultMaxArtifactSize;

    /// <summary>
    /// Gets or sets the head ratio for head+tail truncation (0.0 to 1.0).
    /// Default is 0.6 (60% head, 40% tail).
    /// </summary>
    public double HeadRatio { get; set; } = 0.6;

    /// <summary>
    /// Gets or sets the number of tail lines to keep for tail-only truncation.
    /// </summary>
    public int TailLines { get; set; } = 200;

    /// <summary>
    /// Gets or sets the number of head lines to keep for head-only truncation.
    /// </summary>
    public int HeadLines { get; set; } = 300;

    /// <summary>
    /// Gets or sets the number of first elements to keep for element truncation.
    /// </summary>
    public int FirstElements { get; set; } = 5;

    /// <summary>
    /// Gets or sets the number of last elements to keep for element truncation.
    /// </summary>
    public int LastElements { get; set; } = 5;

    /// <summary>
    /// Validates the limits are within acceptable ranges.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when limits are invalid.</exception>
    public void Validate()
    {
        if (InlineLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(InlineLimit), "Inline limit must be positive.");
        }

        if (ArtifactThreshold <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ArtifactThreshold), "Artifact threshold must be positive.");
        }

        if (MaxArtifactSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxArtifactSize), "Max artifact size must be positive.");
        }

        if (InlineLimit > ArtifactThreshold)
        {
            throw new ArgumentOutOfRangeException(
                nameof(InlineLimit),
                "Inline limit cannot exceed artifact threshold.");
        }

        if (HeadRatio < 0 || HeadRatio > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(HeadRatio), "Head ratio must be between 0 and 1.");
        }

        if (TailLines <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(TailLines), "Tail lines must be positive.");
        }

        if (HeadLines <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(HeadLines), "Head lines must be positive.");
        }

        if (FirstElements < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(FirstElements), "First elements cannot be negative.");
        }

        if (LastElements < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(LastElements), "Last elements cannot be negative.");
        }
    }
}
