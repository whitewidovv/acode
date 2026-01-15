namespace Acode.Domain.Providers.Ollama;

/// <summary>
/// Represents the result of a model pull/download operation.
/// </summary>
/// <remarks>
/// Task 005d Functional Requirements: FR-025 to FR-037.
/// </remarks>
public sealed class ModelPullResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelPullResult"/> class representing a successful pull.
    /// </summary>
    /// <param name="modelName">The name/ID of the model that was pulled.</param>
    /// <param name="sizeBytes">The size of the downloaded model in bytes.</param>
    /// <param name="duration">How long the pull took.</param>
    private ModelPullResult(string modelName, long sizeBytes, TimeSpan duration)
    {
        IsSuccess = true;
        ModelName = modelName;
        SizeBytes = sizeBytes;
        Duration = duration;
        ErrorMessage = string.Empty;
        ErrorCode = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelPullResult"/> class representing a failed pull.
    /// </summary>
    /// <param name="modelName">The name/ID of the model that failed to pull.</param>
    /// <param name="errorMessage">Human-readable error message.</param>
    /// <param name="errorCode">Machine-readable error code (404, DISK_FULL, NETWORK_ERROR, etc.).</param>
    private ModelPullResult(string modelName, string errorMessage, string? errorCode = null)
    {
        IsSuccess = false;
        ModelName = modelName;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        SizeBytes = 0;
        Duration = TimeSpan.Zero;
    }

    /// <summary>
    /// Gets a value indicating whether the pull operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the name/ID of the model.
    /// </summary>
    public string ModelName { get; }

    /// <summary>
    /// Gets the size of the pulled model in bytes (success only).
    /// </summary>
    public long SizeBytes { get; }

    /// <summary>
    /// Gets how long the pull operation took (success only).
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets the error message if pull failed (failure only).
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Gets the machine-readable error code if pull failed.
    /// Possible values: "404_NOT_FOUND", "DISK_FULL", "NETWORK_ERROR", "AIRGAPPED_REJECTED", etc.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Creates a result for a successful pull operation.
    /// </summary>
    /// <param name="modelName">The model name/ID.</param>
    /// <param name="sizeBytes">Downloaded model size in bytes.</param>
    /// <param name="duration">How long the operation took.</param>
    /// <returns>A successful <see cref="ModelPullResult"/>.</returns>
    public static ModelPullResult Success(string modelName, long sizeBytes, TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be empty", nameof(modelName));
        }

        if (sizeBytes < 0)
        {
            throw new ArgumentException("Size bytes cannot be negative", nameof(sizeBytes));
        }

        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentException("Duration cannot be negative", nameof(duration));
        }

        return new ModelPullResult(modelName, sizeBytes, duration);
    }

    /// <summary>
    /// Creates a result for a failed pull operation.
    /// </summary>
    /// <param name="modelName">The model name/ID.</param>
    /// <param name="errorMessage">Human-readable error description.</param>
    /// <param name="errorCode">Machine-readable error code.</param>
    /// <returns>A failed <see cref="ModelPullResult"/>.</returns>
    public static ModelPullResult Failure(string modelName, string errorMessage, string? errorCode = null)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be empty", nameof(modelName));
        }

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("Error message cannot be empty", nameof(errorMessage));
        }

        return new ModelPullResult(modelName, errorMessage, errorCode);
    }
}
