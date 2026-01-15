namespace Acode.Infrastructure.Vllm.StructuredOutput.Fallback;

/// <summary>
/// Enumeration of fallback result reasons.
/// </summary>
public enum FallbackReason
{
    /// <summary>
    /// Extraction of valid JSON from output succeeded.
    /// </summary>
    ExtractionSucceeded,

    /// <summary>
    /// Output regeneration is required.
    /// </summary>
    RegenerationRequired,

    /// <summary>
    /// Maximum fallback attempts have been exceeded.
    /// </summary>
    MaxAttemptsExceeded,

    /// <summary>
    /// The structured output failure is unrecoverable.
    /// </summary>
    Unrecoverable,
}
