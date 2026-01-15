namespace Acode.Domain.Providers.Vllm;

/// <summary>
/// Tracks progress of model loading/downloading.
/// </summary>
public sealed class ModelLoadProgress
{
    /// <summary>
    /// Gets the Huggingface model ID being loaded.
    /// </summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the progress percentage (0-100).
    /// </summary>
    public double ProgressPercent { get; init; }

    /// <summary>
    /// Gets bytes downloaded so far (if known).
    /// </summary>
    public long? BytesDownloaded { get; init; }

    /// <summary>
    /// Gets total bytes to download (if known).
    /// </summary>
    public long? TotalBytes { get; init; }

    /// <summary>
    /// Gets current status message ("downloading", "extracting", "loaded", "failed", etc.).
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets when load operation started.
    /// </summary>
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets when load operation completed (null if still in progress).
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether progress is known (has BytesDownloaded and TotalBytes).
    /// </summary>
    public bool IsProgressKnown => BytesDownloaded.HasValue && TotalBytes.HasValue;

    /// <summary>
    /// Gets a value indicating whether loading is complete.
    /// </summary>
    public bool IsComplete => CompletedAt.HasValue;

    /// <summary>
    /// Factory method for in-progress loading.
    /// </summary>
    /// <param name="modelId">The Huggingface model ID being downloaded.</param>
    /// <param name="downloaded">Bytes downloaded so far.</param>
    /// <param name="total">Total bytes to download.</param>
    /// <param name="status">Current status message (default: "downloading").</param>
    /// <returns>A new ModelLoadProgress instance for downloading state.</returns>
    public static ModelLoadProgress FromDownloading(
        string modelId,
        long downloaded,
        long total,
        string status = "downloading") =>
        new()
        {
            ModelId = modelId,
            BytesDownloaded = downloaded,
            TotalBytes = total,
            ProgressPercent = total > 0 ? (downloaded * 100.0) / total : 0,
            Status = status,
            StartedAt = DateTime.UtcNow,
        };

    /// <summary>
    /// Factory method for completed load.
    /// </summary>
    /// <param name="modelId">The Huggingface model ID that completed loading.</param>
    /// <returns>A new ModelLoadProgress instance for completed state.</returns>
    public static ModelLoadProgress FromComplete(string modelId) =>
        new()
        {
            ModelId = modelId,
            ProgressPercent = 100,
            Status = "loaded",
            CompletedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow,
        };
}
