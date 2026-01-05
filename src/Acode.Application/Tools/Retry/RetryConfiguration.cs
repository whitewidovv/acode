namespace Acode.Application.Tools.Retry;

/// <summary>
/// Configuration for validation retry behavior.
/// </summary>
/// <remarks>
/// FR-007b: Validation error retry contract.
/// FR-056 to FR-060: Configuration requirements.
/// </remarks>
public sealed record RetryConfiguration
{
    /// <summary>
    /// Gets the maximum number of retry attempts before escalation.
    /// </summary>
    /// <remarks>Default: 3.</remarks>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Gets the maximum number of errors to show in a single message.
    /// </summary>
    /// <remarks>Default: 10.</remarks>
    public int MaxErrorsShown { get; init; } = 10;

    /// <summary>
    /// Gets the maximum length of the formatted error message in characters.
    /// </summary>
    /// <remarks>Default: 2000.</remarks>
    public int MaxMessageLength { get; init; } = 2000;

    /// <summary>
    /// Gets the maximum length of value preview before truncation.
    /// </summary>
    /// <remarks>Default: 100.</remarks>
    public int MaxValuePreview { get; init; } = 100;

    /// <summary>
    /// Gets the default configuration instance.
    /// </summary>
    public static RetryConfiguration Default { get; } = new();
}
