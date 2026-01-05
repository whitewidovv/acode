namespace Acode.Infrastructure.Tools;

using System.Collections.Concurrent;
using Acode.Application.Tools.Retry;
using Acode.Domain.Tools;

/// <summary>
/// Thread-safe tracker for validation retry attempts.
/// </summary>
/// <remarks>
/// FR-007b: Validation error retry contract.
/// FR-036 to FR-040: Retry tracking requirements.
/// </remarks>
public sealed class RetryTracker : IRetryTracker
{
    private readonly ConcurrentDictionary<string, List<ValidationAttempt>> attempts = new(StringComparer.Ordinal);
    private readonly RetryConfiguration config;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryTracker"/> class.
    /// </summary>
    /// <param name="config">The retry configuration.</param>
    public RetryTracker(RetryConfiguration config)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc />
    public void RecordAttempt(string toolCallId, IReadOnlyCollection<SchemaValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(toolCallId);
        ArgumentNullException.ThrowIfNull(errors);

        var attemptList = this.attempts.GetOrAdd(toolCallId, _ => new List<ValidationAttempt>());

        lock (attemptList)
        {
            var attemptNumber = attemptList.Count + 1;
            var attempt = new ValidationAttempt(attemptNumber, errors, DateTimeOffset.UtcNow);
            attemptList.Add(attempt);
        }
    }

    /// <inheritdoc />
    public int GetAttemptCount(string toolCallId)
    {
        if (this.attempts.TryGetValue(toolCallId, out var attemptList))
        {
            lock (attemptList)
            {
                return attemptList.Count;
            }
        }

        return 0;
    }

    /// <inheritdoc />
    public IReadOnlyList<ValidationAttempt> GetHistory(string toolCallId)
    {
        if (this.attempts.TryGetValue(toolCallId, out var attemptList))
        {
            lock (attemptList)
            {
                return attemptList.ToList().AsReadOnly();
            }
        }

        return Array.Empty<ValidationAttempt>();
    }

    /// <inheritdoc />
    public bool HasExceededMaxRetries(string toolCallId)
    {
        var count = this.GetAttemptCount(toolCallId);
        return count > this.config.MaxRetries;
    }

    /// <inheritdoc />
    public void Clear(string toolCallId)
    {
        this.attempts.TryRemove(toolCallId, out _);
    }
}
