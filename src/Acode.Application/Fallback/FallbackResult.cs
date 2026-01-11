namespace Acode.Application.Fallback;

/// <summary>
/// Result of fallback resolution attempt.
/// </summary>
/// <remarks>
/// <para>AC-003 to AC-005: FallbackResult structure.</para>
/// </remarks>
public sealed class FallbackResult
{
    /// <summary>
    /// Gets a value indicating whether fallback was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the ID of fallback model if successful, null otherwise.
    /// </summary>
    public string? ModelId { get; init; }

    /// <summary>
    /// Gets the reason for fallback or failure.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets the list of models tried during fallback resolution.
    /// </summary>
    public IReadOnlyList<string>? TriedModels { get; init; }

    /// <summary>
    /// Gets the failure reasons per model if chain was exhausted.
    /// </summary>
    public IReadOnlyDictionary<string, string>? FailureReasons { get; init; }

    /// <summary>
    /// Creates a successful fallback result.
    /// </summary>
    /// <param name="modelId">The fallback model ID.</param>
    /// <param name="reason">The reason for fallback.</param>
    /// <param name="triedModels">Models that were tried.</param>
    /// <returns>A successful FallbackResult.</returns>
    public static FallbackResult Succeeded(
        string modelId,
        string reason,
        IReadOnlyList<string> triedModels
    )
    {
        return new FallbackResult
        {
            Success = true,
            ModelId = modelId,
            Reason = reason,
            TriedModels = triedModels,
        };
    }

    /// <summary>
    /// Creates a failed fallback result.
    /// </summary>
    /// <param name="reason">The reason for failure.</param>
    /// <param name="triedModels">Models that were tried.</param>
    /// <param name="failureReasons">Reasons each model failed.</param>
    /// <returns>A failed FallbackResult.</returns>
    public static FallbackResult Failed(
        string reason,
        IReadOnlyList<string> triedModels,
        IReadOnlyDictionary<string, string>? failureReasons = null
    )
    {
        return new FallbackResult
        {
            Success = false,
            ModelId = null,
            Reason = reason,
            TriedModels = triedModels,
            FailureReasons = failureReasons,
        };
    }
}
