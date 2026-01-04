namespace Acode.Infrastructure.Vllm.Exceptions;

/// <summary>
/// Exception thrown when vLLM rate limit is exceeded (HTTP 429).
/// </summary>
public sealed class VllmRateLimitException : VllmException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VllmRateLimitException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VllmRateLimitException(string message)
        : base("ACODE-VLM-012", message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmRateLimitException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public VllmRateLimitException(string message, Exception innerException)
        : base("ACODE-VLM-012", message, innerException)
    {
    }

    /// <inheritdoc/>
    public override bool IsTransient => true;

    /// <summary>
    /// Gets or sets the suggested retry-after delay.
    /// </summary>
    public TimeSpan? RetryAfter { get; set; }
}
