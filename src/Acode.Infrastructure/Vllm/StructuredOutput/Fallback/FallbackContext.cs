namespace Acode.Infrastructure.Vllm.StructuredOutput.Fallback;

/// <summary>
/// Context information for fallback handling operations.
/// </summary>
/// <remarks>
/// FR-047 through FR-049: Fallback operation context tracking.
/// </remarks>
public sealed class FallbackContext
{
    /// <summary>
    /// Gets or sets the model identifier for the current request.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current fallback mode (Managed, Monitored, External).
    /// </summary>
    public string FallbackMode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the structured output validation failure that triggered fallback.
    /// </summary>
    public string ValidationError { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of fallback attempts made so far.
    /// </summary>
    public int FallbackAttempts { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of fallback attempts allowed.
    /// </summary>
    public int MaxFallbackAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the timestamp when fallback was initiated.
    /// </summary>
    public DateTime InitiatedUtc { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the fallback handler should regenerate output.
    /// </summary>
    public bool ShouldRegenerateOutput { get; set; }

    /// <summary>
    /// Gets or sets the original (invalid) output that needs fixing.
    /// </summary>
    public string? InvalidOutput { get; set; }
}
