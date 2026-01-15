using System.Net.Sockets;
using Acode.Infrastructure.Vllm.Exceptions;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Vllm.Client.Retry;

/// <summary>
/// Retry policy for vLLM requests with exponential backoff.
/// </summary>
/// <remarks>
/// FR-075 to FR-084: VllmRetryPolicy implementation.
/// Implements exponential backoff strategy for transient failures.
/// </remarks>
public sealed class VllmRetryPolicy : IVllmRetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _maxDelay;
    private readonly double _backoffMultiplier;
    private readonly ILogger<VllmRetryPolicy>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmRetryPolicy"/> class.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3).</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds (default: 100).</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds (default: 30000).</param>
    /// <param name="backoffMultiplier">Backoff multiplier (default: 2.0).</param>
    /// <param name="logger">Optional logger.</param>
    public VllmRetryPolicy(
        int maxRetries = 3,
        int initialDelayMs = 100,
        int maxDelayMs = 30000,
        double backoffMultiplier = 2.0,
        ILogger<VllmRetryPolicy>? logger = null)
    {
        _maxRetries = maxRetries;
        _initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
        _maxDelay = TimeSpan.FromMilliseconds(maxDelayMs);
        _backoffMultiplier = backoffMultiplier;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        int attempt = 0;
        Exception? lastException = null;

        while (attempt < _maxRetries)
        {
            attempt++;

            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsTransientException(ex))
            {
                lastException = ex;

                // FR-084: Throw after max retries exceeded
                if (attempt >= _maxRetries)
                {
                    _logger?.LogError(ex, "Operation failed after {Attempts} attempts", attempt);
                    throw;
                }

                // FR-081: Calculate exponential backoff delay
                var delay = CalculateDelay(attempt);
                _logger?.LogWarning(
                    "Transient error on attempt {Attempt}/{MaxRetries}. Retrying after {Delay}ms. Error: {Error}",
                    attempt,
                    _maxRetries,
                    delay.TotalMilliseconds,
                    ex.Message);

                // FR-083: Respect cancellation token
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        // Should never reach here, but throw last exception if we do
        throw lastException ?? new InvalidOperationException("Retry logic failed unexpectedly");
    }

    /// <summary>
    /// Determines if an exception is transient and should be retried.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the exception is transient, false otherwise.</returns>
    private bool IsTransientException(Exception exception)
    {
        // FR-076: Retry on SocketException (network errors)
        if (exception is SocketException)
        {
            return true;
        }

        // FR-077: Retry on HttpRequestException (transient network errors)
        if (exception is HttpRequestException)
        {
            return true;
        }

        // FR-078: Retry on 503 Service Unavailable
        if (exception is VllmServerException)
        {
            return true;
        }

        // FR-079: Retry on 429 Too Many Requests (rate limit)
        if (exception is VllmRateLimitException)
        {
            return true;
        }

        // FR-082: Do NOT retry on 401 Unauthorized
        if (exception is VllmAuthException)
        {
            return false;
        }

        // FR-082: Do NOT retry on 404 Not Found
        if (exception is VllmModelNotFoundException)
        {
            return false;
        }

        // FR-080: Do NOT retry on 400 Bad Request or other client errors
        if (exception is VllmRequestException)
        {
            return false;
        }

        return false;
    }

    /// <summary>
    /// Calculates the exponential backoff delay for a given attempt.
    /// </summary>
    /// <param name="attempt">The attempt number (1-indexed).</param>
    /// <returns>The delay to wait before the next attempt.</returns>
    /// <remarks>
    /// FR-081: Exponential backoff formula: initialDelay * (backoffMultiplier ^ (attempt - 1))
    /// The result is capped at maxDelay.
    /// </remarks>
    private TimeSpan CalculateDelay(int attempt)
    {
        // Formula: initialDelay * (backoffMultiplier ^ (attempt - 1))
        var delayMs = _initialDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attempt - 1);

        // Cap at maximum delay
        delayMs = Math.Min(delayMs, _maxDelay.TotalMilliseconds);

        return TimeSpan.FromMilliseconds(delayMs);
    }
}
