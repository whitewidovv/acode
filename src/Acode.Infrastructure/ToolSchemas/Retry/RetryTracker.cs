using System.Collections.Concurrent;
using Acode.Application.ToolSchemas.Retry;

namespace Acode.Infrastructure.ToolSchemas.Retry;

/// <summary>
/// Tracks retry attempts and validation history for tool calls.
/// Thread-safe implementation using ConcurrentDictionary and Interlocked operations.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3858-3955.
/// Provides O(1) lookup and thread-safe increment operations.
/// </remarks>
public sealed class RetryTracker : IRetryTracker
{
    private const int MaxHistoryEntries = 10;

    private readonly ConcurrentDictionary<string, RetryState> states = new();
    private readonly int maxAttempts;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryTracker"/> class.
    /// </summary>
    /// <param name="maxAttempts">Maximum retry attempts before exceeded.</param>
    public RetryTracker(int maxAttempts)
    {
        this.maxAttempts = maxAttempts;
    }

    /// <inheritdoc/>
    public int IncrementAttempt(string toolCallId)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolCallId);

        var state = this.states.GetOrAdd(toolCallId, _ => new RetryState());
        return state.IncrementAttemptCount();
    }

    /// <inheritdoc/>
    public int GetCurrentAttempt(string toolCallId)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolCallId);

        return this.states.TryGetValue(toolCallId, out var state)
            ? state.ReadAttemptCount()
            : 0;
    }

    /// <inheritdoc/>
    public void RecordError(string toolCallId, string errorMessage)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolCallId);
        ArgumentException.ThrowIfNullOrEmpty(errorMessage);

        var state = this.states.GetOrAdd(toolCallId, _ => new RetryState());

        lock (state.History)
        {
            // Limit history to prevent unbounded memory growth
            if (state.History.Count >= MaxHistoryEntries)
            {
                state.History.RemoveAt(0);
            }

            state.History.Add(errorMessage);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetHistory(string toolCallId)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolCallId);

        if (!this.states.TryGetValue(toolCallId, out var state))
        {
            return Array.Empty<string>();
        }

        lock (state.History)
        {
            // Return a copy to ensure thread safety
            return state.History.ToList();
        }
    }

    /// <inheritdoc/>
    public bool HasExceededMaxRetries(string toolCallId)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolCallId);

        return this.GetCurrentAttempt(toolCallId) >= this.maxAttempts;
    }

    /// <inheritdoc/>
    public void Clear(string toolCallId)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolCallId);

        this.states.TryRemove(toolCallId, out _);
    }

    /// <summary>
    /// Internal state for tracking retry attempts.
    /// </summary>
    private sealed class RetryState
    {
        private int attemptCount;

        /// <summary>
        /// Gets the history of error messages.
        /// </summary>
        public List<string> History { get; } = new();

        /// <summary>
        /// Increments the attempt count atomically and returns the new value.
        /// </summary>
        /// <returns>The incremented attempt count.</returns>
        public int IncrementAttemptCount() => Interlocked.Increment(ref this.attemptCount);

        /// <summary>
        /// Reads the attempt count with volatile semantics.
        /// </summary>
        /// <returns>The current attempt count.</returns>
        public int ReadAttemptCount() => Volatile.Read(ref this.attemptCount);
    }
}
