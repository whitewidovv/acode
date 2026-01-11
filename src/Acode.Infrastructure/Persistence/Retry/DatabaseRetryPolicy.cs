// src/Acode.Infrastructure/Persistence/Retry/DatabaseRetryPolicy.cs
namespace Acode.Infrastructure.Persistence.Retry;

using Acode.Application.Interfaces.Persistence;
using Acode.Domain.Exceptions;
using Acode.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Retry policy for transient database errors with exponential backoff and jitter.
/// </summary>
public sealed class DatabaseRetryPolicy : IDatabaseRetryPolicy
{
    private readonly RetryOptions _options;
    private readonly ILogger<DatabaseRetryPolicy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseRetryPolicy"/> class.
    /// </summary>
    /// <param name="options">Retry policy configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public DatabaseRetryPolicy(
        IOptions<DatabaseOptions> options,
        ILogger<DatabaseRetryPolicy> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value.Retry;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (!_options.Enabled)
        {
            return await operation(ct).ConfigureAwait(false);
        }

        var attempt = 0;
        var exceptions = new List<Exception>();

        while (true)
        {
            attempt++;
            ct.ThrowIfCancellationRequested();

            try
            {
                return await operation(ct).ConfigureAwait(false);
            }
            catch (DatabaseException ex) when (ex.IsTransient && attempt < _options.MaxAttempts)
            {
                exceptions.Add(ex);
                var delay = CalculateDelay(attempt);

                _logger.LogWarning(
                    "Transient database error, retrying. Attempt={Attempt}/{Max}, Delay={Delay}ms, Error={Error}",
                    attempt,
                    _options.MaxAttempts,
                    delay.TotalMilliseconds,
                    ex.ErrorCode);

                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
            catch (DatabaseException ex) when (!ex.IsTransient)
            {
                // Permanent error - don't retry
                _logger.LogError(
                    "Permanent database error, not retrying. Attempt={Attempt}, Error={Error}",
                    attempt,
                    ex.ErrorCode);
                throw;
            }
            catch (DatabaseException ex)
            {
                // Exhausted retries
                exceptions.Add(ex);
                _logger.LogError(
                    "Database error after all retries exhausted. Attempts={Attempts}, Errors={Errors}",
                    attempt,
                    string.Join(", ", exceptions.Select(e => e.Message)));
                throw;
            }
        }
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await ExecuteAsync(
            async ct2 =>
            {
                await operation(ct2).ConfigureAwait(false);
                return true;
            },
            ct).ConfigureAwait(false);
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        // Exponential backoff: base * 2^(attempt-1)
        var exponentialMs = _options.BaseDelayMs * Math.Pow(2, attempt - 1);
        var cappedMs = Math.Min(exponentialMs, _options.MaxDelayMs);

        // Add jitter (10-30% of delay) using thread-safe Random.Shared
        var jitterFactor = 0.1 + (Random.Shared.NextDouble() * 0.2);
        var jitterMs = cappedMs * jitterFactor;

        return TimeSpan.FromMilliseconds(cappedMs + jitterMs);
    }
}
