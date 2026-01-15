namespace Acode.Infrastructure.Vllm.StructuredOutput;

using Acode.Infrastructure.Vllm.StructuredOutput.Fallback;

/// <summary>
/// Result of applying structured output constraints to a vLLM request.
/// </summary>
/// <remarks>
/// Indicates whether structured output was successfully applied, disabled, or requires fallback.
/// </remarks>
public sealed class ApplyResult
{
    /// <summary>
    /// Gets a value indicating whether structured output was successfully applied.
    /// </summary>
    public bool IsApplied { get; init; }

    /// <summary>
    /// Gets a value indicating whether structured output is disabled for this request.
    /// </summary>
    public bool IsDisabled { get; init; }

    /// <summary>
    /// Gets the mode of structured output that was applied.
    /// </summary>
    public StructuredOutputMode? Mode { get; init; }

    /// <summary>
    /// Gets the fallback reason if fallback was activated.
    /// </summary>
    public FallbackReason? FallbackReason { get; init; }

    /// <summary>
    /// Gets the fallback message providing context for the fallback decision.
    /// </summary>
    public string? FallbackMessage { get; init; }

    /// <summary>
    /// Creates a result indicating structured output is disabled.
    /// </summary>
    /// <returns>An ApplyResult with IsDisabled=true.</returns>
    public static ApplyResult Disabled() =>
        new() { IsDisabled = true };

    /// <summary>
    /// Creates a result indicating structured output was successfully applied.
    /// </summary>
    /// <param name="mode">The structured output mode that was applied.</param>
    /// <returns>An ApplyResult with IsApplied=true and the specified mode.</returns>
    public static ApplyResult Applied(StructuredOutputMode mode) =>
        new() { IsApplied = true, Mode = mode };

    /// <summary>
    /// Creates a result indicating fallback mode was activated.
    /// </summary>
    /// <param name="reason">The reason fallback was activated.</param>
    /// <param name="message">Optional message providing context for the fallback.</param>
    /// <returns>An ApplyResult with fallback information.</returns>
    public static ApplyResult Fallback(FallbackReason reason, string? message = null) =>
        new() { FallbackReason = reason, FallbackMessage = message };

    /// <summary>
    /// Creates a result indicating structured output was not applicable to this request.
    /// </summary>
    /// <returns>An ApplyResult with default values (not applied, not disabled, no fallback).</returns>
    public static ApplyResult NotApplicable() =>
        new();
}
