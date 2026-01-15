namespace Acode.Domain.Providers.Ollama;

/// <summary>
/// Represents progress information for an ongoing model pull operation.
/// </summary>
/// <remarks>
/// Used for streaming progress updates during model downloading.
/// Task 005d Functional Requirements: FR-025 to FR-037.
/// </remarks>
public sealed class ModelPullProgress
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelPullProgress"/> class.
    /// </summary>
    /// <param name="modelName">The model being pulled.</param>
    /// <param name="status">Current status (e.g., "downloading layer 1/4", "verifying").</param>
    /// <param name="currentBytes">Bytes downloaded so far.</param>
    /// <param name="totalBytes">Total bytes to download (or -1 if unknown).</param>
    /// <param name="percentComplete">Percentage complete (0-100, or -1 if unknown).</param>
    public ModelPullProgress(string modelName, string status, long currentBytes, long totalBytes, int percentComplete)
    {
        ModelName = modelName;
        Status = status;
        CurrentBytes = currentBytes;
        TotalBytes = totalBytes;
        PercentComplete = percentComplete;
    }

    /// <summary>
    /// Gets the name/ID of the model being pulled.
    /// </summary>
    public string ModelName { get; }

    /// <summary>
    /// Gets the current status message (e.g., "downloading", "verifying", "complete").
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the number of bytes downloaded so far.
    /// </summary>
    public long CurrentBytes { get; }

    /// <summary>
    /// Gets the total number of bytes to download, or -1 if unknown.
    /// </summary>
    public long TotalBytes { get; }

    /// <summary>
    /// Gets the completion percentage (0-100), or -1 if unknown.
    /// </summary>
    public int PercentComplete { get; }

    /// <summary>
    /// Gets a value indicating whether progress can be calculated.
    /// </summary>
    public bool IsProgressKnown => TotalBytes > 0;
}
