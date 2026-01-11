// src/Acode.Domain/Conversation/Run.cs
namespace Acode.Domain.Conversation;

using System;
using System.Collections.Generic;
using Acode.Domain.Common;

/// <summary>
/// Run entity representing a single request/response cycle within a Chat.
/// Contains one or more Messages with their tool calls.
/// </summary>
public sealed class Run : Entity<RunId>
{
    private readonly List<Message> _messages = new();

    // Private constructor for ORM/deserialization
    private Run()
    {
    }

    private Run(RunId id, ChatId chatId, string modelId, DateTimeOffset startedAt, int sequenceNumber)
    {
        Id = id;
        ChatId = chatId;
        ModelId = modelId;
        StartedAt = startedAt;
        SequenceNumber = sequenceNumber;
        Status = RunStatus.Running;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Gets the parent Chat ID.
    /// </summary>
    public ChatId ChatId { get; private set; }

    /// <summary>
    /// Gets the model identifier used for this run.
    /// </summary>
    public string ModelId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the current status of the run.
    /// </summary>
    public RunStatus Status { get; private set; }

    /// <summary>
    /// Gets the timestamp when the run started.
    /// </summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the run completed (null if still running).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Gets the number of input tokens consumed.
    /// </summary>
    public int TokensIn { get; private set; }

    /// <summary>
    /// Gets the number of output tokens produced.
    /// </summary>
    public int TokensOut { get; private set; }

    /// <summary>
    /// Gets the sequence number of this run within its chat.
    /// </summary>
    public int SequenceNumber { get; private set; }

    /// <summary>
    /// Gets the error message if the run failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the sync status.
    /// </summary>
    public SyncStatus SyncStatus { get; private set; }

    /// <summary>
    /// Gets the messages in this run.
    /// </summary>
    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

    /// <summary>
    /// Gets the total tokens used (input + output).
    /// </summary>
    public int TotalTokens => TokensIn + TokensOut;

    /// <summary>
    /// Gets the duration of the run if completed.
    /// </summary>
    public TimeSpan? Duration => CompletedAt.HasValue
        ? CompletedAt.Value - StartedAt
        : null;

    /// <summary>
    /// Creates a new Run for a Chat.
    /// </summary>
    /// <param name="chatId">The parent chat ID.</param>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="sequenceNumber">The sequence number within the chat.</param>
    /// <returns>A new Run instance.</returns>
    public static Run Create(ChatId chatId, string modelId, int sequenceNumber = 0)
    {
        if (chatId == ChatId.Empty)
        {
            throw new ArgumentException("ChatId cannot be empty", nameof(chatId));
        }

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("ModelId cannot be empty", nameof(modelId));
        }

        return new Run(RunId.NewId(), chatId, modelId, DateTimeOffset.UtcNow, sequenceNumber);
    }

    /// <summary>
    /// Reconstitutes a Run from persisted data.
    /// </summary>
    /// <param name="id">The run ID.</param>
    /// <param name="chatId">The parent chat ID.</param>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="status">The run status.</param>
    /// <param name="startedAt">The start timestamp.</param>
    /// <param name="completedAt">The completion timestamp.</param>
    /// <param name="tokensIn">The input token count.</param>
    /// <param name="tokensOut">The output token count.</param>
    /// <param name="sequenceNumber">The sequence number.</param>
    /// <param name="errorMessage">The error message if failed.</param>
    /// <param name="syncStatus">The sync status.</param>
    /// <returns>A reconstituted Run instance.</returns>
    public static Run Reconstitute(
        RunId id,
        ChatId chatId,
        string modelId,
        RunStatus status,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt,
        int tokensIn,
        int tokensOut,
        int sequenceNumber,
        string? errorMessage,
        SyncStatus syncStatus)
    {
        return new Run
        {
            Id = id,
            ChatId = chatId,
            ModelId = modelId,
            Status = status,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            TokensIn = tokensIn,
            TokensOut = tokensOut,
            SequenceNumber = sequenceNumber,
            ErrorMessage = errorMessage,
            SyncStatus = syncStatus,
        };
    }

    /// <summary>
    /// Completes the run successfully with token counts.
    /// </summary>
    /// <param name="tokensIn">The number of input tokens consumed.</param>
    /// <param name="tokensOut">The number of output tokens produced.</param>
    public void Complete(int tokensIn, int tokensOut)
    {
        if (Status != RunStatus.Running)
        {
            throw new InvalidOperationException($"Cannot complete run in {Status} status");
        }

        TokensIn = tokensIn;
        TokensOut = tokensOut;
        Status = RunStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Marks the run as failed with error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    public void Fail(string errorMessage)
    {
        if (Status != RunStatus.Running)
        {
            throw new InvalidOperationException($"Cannot fail run in {Status} status");
        }

        ErrorMessage = errorMessage ?? "Unknown error";
        Status = RunStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Cancels the run.
    /// </summary>
    public void Cancel()
    {
        if (Status != RunStatus.Running)
        {
            throw new InvalidOperationException($"Cannot cancel run in {Status} status");
        }

        Status = RunStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Marks the run as synced to remote.
    /// </summary>
    public void MarkSynced()
    {
        SyncStatus = SyncStatus.Synced;
    }

    /// <summary>
    /// Marks the run as having a sync conflict.
    /// </summary>
    public void MarkConflict()
    {
        SyncStatus = SyncStatus.Conflict;
    }

    /// <summary>
    /// Adds a message to this run.
    /// </summary>
    /// <param name="message">The message to add.</param>
    internal void AddMessage(Message message)
    {
        _messages.Add(message);
    }
}
