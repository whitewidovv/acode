// src/Acode.Infrastructure/Sync/RetryPolicy.cs
namespace Acode.Infrastructure.Sync;

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Implements exponential backoff retry policy for transient errors.
/// </summary>
public sealed class RetryPolicy
{
    private readonly int _maxRetries;
    private readonly int _baseDelayMs;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryPolicy"/> class.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="baseDelayMs">Base delay in milliseconds for exponential backoff.</param>
    public RetryPolicy(int maxRetries, int baseDelayMs)
    {
        if (maxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries must be non-negative");
        }

        if (baseDelayMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseDelayMs), "Base delay must be non-negative");
        }

        _maxRetries = maxRetries;
        _baseDelayMs = baseDelayMs;
    }

    /// <summary>
    /// Executes an action with retry logic for transient errors.
    /// Uses exponential backoff: delay = baseDelayMs * 2^(attemptNumber - 1).
    /// </summary>
    /// <typeparam name="T">The return type of the action.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the action.</returns>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        int attemptNumber = 0;
        Exception? lastException = null;

        while (attemptNumber <= _maxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                attemptNumber++;
                return await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lastException = ex;

                // Check if this is a transient error that should be retried
                if (!IsTransientError(ex))
                {
                    throw;
                }

                // If we've exhausted retries, throw the last exception
                if (attemptNumber > _maxRetries)
                {
                    throw;
                }

                // Calculate exponential backoff delay
                var delayMs = _baseDelayMs * Math.Pow(2, attemptNumber - 1);
                var delay = TimeSpan.FromMilliseconds(delayMs);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        // This should never be reached, but satisfies the compiler
        throw lastException!;
    }

    private static bool IsTransientError(Exception exception)
    {
        // Check the exception type directly
        if (exception is HttpRequestException or TimeoutException)
        {
            return true;
        }

        // Check for AggregateException with transient inner exceptions
        if (exception is AggregateException aggregateException)
        {
            return aggregateException.InnerExceptions.Any(IsTransientError);
        }

        return false;
    }
}
