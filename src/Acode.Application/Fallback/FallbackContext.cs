using Acode.Domain.Modes;

namespace Acode.Application.Fallback;

/// <summary>
/// Context for fallback resolution.
/// </summary>
/// <remarks>
/// <para>Provides all context needed to determine appropriate fallback model.</para>
/// </remarks>
public sealed class FallbackContext
{
    /// <summary>
    /// Gets the original model that failed.
    /// </summary>
    public required string OriginalModel { get; init; }

    /// <summary>
    /// Gets the trigger that initiated fallback.
    /// </summary>
    public required EscalationTrigger Trigger { get; init; }

    /// <summary>
    /// Gets the current operating mode for constraint validation.
    /// </summary>
    public required OperatingMode OperatingMode { get; init; }

    /// <summary>
    /// Gets the optional error that caused escalation.
    /// </summary>
    public Exception? Error { get; init; }

    /// <summary>
    /// Gets the optional session ID for logging.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Gets the optional task ID for logging.
    /// </summary>
    public string? TaskId { get; init; }
}
