namespace Acode.Infrastructure.Truncation;

using Acode.Application.Truncation;
using Acode.Infrastructure.Truncation.Strategies;

/// <summary>
/// Processes tool output through truncation or artifact creation.
/// </summary>
public sealed class TruncationProcessor : ITruncationProcessor
{
    private readonly TruncationConfiguration configuration;
    private readonly IArtifactStore artifactStore;
    private readonly Dictionary<TruncationStrategy, ITruncationStrategy> strategies;

    /// <summary>
    /// Initializes a new instance of the <see cref="TruncationProcessor"/> class.
    /// </summary>
    /// <param name="configuration">The truncation configuration.</param>
    /// <param name="artifactStore">The artifact store.</param>
    public TruncationProcessor(TruncationConfiguration configuration, IArtifactStore artifactStore)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(artifactStore);

        this.configuration = configuration;
        this.artifactStore = artifactStore;

        // Initialize strategies
        this.strategies = new Dictionary<TruncationStrategy, ITruncationStrategy>
        {
            [TruncationStrategy.Head] = new HeadStrategy(),
            [TruncationStrategy.Tail] = new TailStrategy(),
            [TruncationStrategy.HeadTail] = new HeadTailStrategy(),
            [TruncationStrategy.Element] = new ElementStrategy()
        };
    }

    /// <inheritdoc />
    public async Task<TruncationResult> ProcessAsync(
        string content,
        string toolName,
        string contentType = "text/plain")
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        var limits = GetLimitsForTool(toolName);
        var strategy = GetStrategyForTool(toolName);

        // Empty content - pass through
        if (string.IsNullOrEmpty(content))
        {
            return TruncationResult.NotTruncated(content);
        }

        // Content under inline limit - no truncation needed
        if (content.Length <= limits.InlineLimit)
        {
            return TruncationResult.NotTruncated(content);
        }

        // Content exceeds artifact threshold - create artifact
        if (content.Length > limits.ArtifactThreshold)
        {
            return await CreateArtifactResultAsync(content, toolName, contentType, limits)
                .ConfigureAwait(false);
        }

        // Content between limits - apply truncation strategy
        if (strategy == TruncationStrategy.None)
        {
            return TruncationResult.NotTruncated(content);
        }

        if (!strategies.TryGetValue(strategy, out var truncationStrategy))
        {
            // Default to head+tail if strategy not found
            truncationStrategy = strategies[TruncationStrategy.HeadTail];
        }

        return truncationStrategy.Truncate(content, limits);
    }

    /// <inheritdoc />
    public TruncationLimits GetLimitsForTool(string toolName)
    {
        return configuration.GetLimitsForTool(toolName);
    }

    /// <inheritdoc />
    public TruncationStrategy GetStrategyForTool(string toolName)
    {
        return configuration.GetStrategyForTool(toolName);
    }

    /// <summary>
    /// Builds an artifact reference string.
    /// </summary>
    private static string BuildArtifactReference(Artifact artifact)
    {
        var preview = artifact.Preview ?? string.Empty;
        if (preview.Length > 100)
        {
            preview = preview[..100] + "...";
        }

        // Escape any newlines in preview for cleaner display
        preview = preview.Replace("\n", "\\n", StringComparison.Ordinal)
                         .Replace("\r", string.Empty, StringComparison.Ordinal);

        return $"""
            [Artifact: {artifact.Id}]
            Type: {artifact.ContentType}
            Size: {FormatSize(artifact.Size)} (~{artifact.TokenEstimate:N0} tokens)
            Source: {artifact.SourceTool}
            Created: {artifact.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC

            Preview: {preview}

            Retrieval options:
            - Full content: get_artifact(id="{artifact.Id}")
            - Line range: get_artifact(id="{artifact.Id}", start_line=1, end_line=100)
            """;
    }

    /// <summary>
    /// Formats a byte size as a human-readable string.
    /// </summary>
    private static string FormatSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} bytes",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
        };
    }

    /// <summary>
    /// Creates an artifact and returns a reference result.
    /// </summary>
    private async Task<TruncationResult> CreateArtifactResultAsync(
        string content,
        string toolName,
        string contentType,
        TruncationLimits limits)
    {
        // Check if content exceeds max artifact size
        if (content.Length > limits.MaxArtifactSize)
        {
            return new TruncationResult
            {
                Content = $"Error: Content size ({FormatSize(content.Length)}) exceeds maximum artifact size ({FormatSize(limits.MaxArtifactSize)}).",
                Metadata = new TruncationMetadata
                {
                    OriginalSize = content.Length,
                    TruncatedSize = 0,
                    WasTruncated = false,
                    StrategyUsed = TruncationStrategy.None
                }
            };
        }

        var artifact = await artifactStore.CreateAsync(content, toolName, contentType)
            .ConfigureAwait(false);

        var reference = BuildArtifactReference(artifact);

        return new TruncationResult
        {
            Content = reference,
            ArtifactReference = reference,
            Metadata = new TruncationMetadata
            {
                OriginalSize = content.Length,
                TruncatedSize = reference.Length,
                WasTruncated = true,
                StrategyUsed = TruncationStrategy.None,
                ArtifactId = artifact.Id,
                OmittedCharacters = content.Length
            }
        };
    }
}
