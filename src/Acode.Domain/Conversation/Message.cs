// src/Acode.Domain/Conversation/Message.cs
namespace Acode.Domain.Conversation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Acode.Domain.Common;

/// <summary>
/// Message entity representing a single exchange within a Run.
/// Contains role, content, and optional tool calls.
/// </summary>
public sealed class Message : Entity<MessageId>
{
    private const int MaxContentLength = 100 * 1024; // 100KB
    private readonly List<ToolCall> _toolCalls = new();

    // Private constructor for ORM/deserialization
    private Message()
    {
    }

    private Message(MessageId id, RunId runId, string role, string content, DateTimeOffset createdAt, int sequenceNumber)
    {
        Id = id;
        RunId = runId;
        Role = role;
        Content = content;
        CreatedAt = createdAt;
        SequenceNumber = sequenceNumber;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Gets the parent Run ID.
    /// </summary>
    public RunId RunId { get; private set; }

    /// <summary>
    /// Gets the message role (user, assistant, system, tool).
    /// </summary>
    public string Role { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the message content.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the tool calls associated with this message.
    /// </summary>
    public IReadOnlyList<ToolCall> ToolCalls => _toolCalls.AsReadOnly();

    /// <summary>
    /// Gets the timestamp when the message was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the sequence number of this message within its run.
    /// </summary>
    public int SequenceNumber { get; private set; }

    /// <summary>
    /// Gets the sync status.
    /// </summary>
    public SyncStatus SyncStatus { get; private set; }

    /// <summary>
    /// Creates a new Message.
    /// </summary>
    /// <param name="runId">The parent run ID.</param>
    /// <param name="role">The message role (user, assistant, system, tool).</param>
    /// <param name="content">The message content.</param>
    /// <param name="sequenceNumber">The sequence number within the run.</param>
    /// <returns>A new Message instance.</returns>
    public static Message Create(RunId runId, string role, string content, int sequenceNumber = 0)
    {
        if (runId == RunId.Empty)
        {
            throw new ArgumentException("RunId cannot be empty", nameof(runId));
        }

        ValidateRole(role);
        ValidateContent(content);

        return new Message(
            MessageId.NewId(),
            runId,
            role.ToLowerInvariant(),
            content,
            DateTimeOffset.UtcNow,
            sequenceNumber);
    }

    /// <summary>
    /// Reconstitutes a Message from persisted data.
    /// </summary>
    /// <param name="id">The message ID.</param>
    /// <param name="runId">The parent run ID.</param>
    /// <param name="role">The message role.</param>
    /// <param name="content">The message content.</param>
    /// <param name="toolCalls">The tool calls collection.</param>
    /// <param name="createdAt">The creation timestamp.</param>
    /// <param name="sequenceNumber">The sequence number.</param>
    /// <param name="syncStatus">The sync status.</param>
    /// <returns>A reconstituted Message instance.</returns>
    public static Message Reconstitute(
        MessageId id,
        RunId runId,
        string role,
        string content,
        IEnumerable<ToolCall>? toolCalls,
        DateTimeOffset createdAt,
        int sequenceNumber,
        SyncStatus syncStatus)
    {
        var message = new Message
        {
            Id = id,
            RunId = runId,
            Role = role,
            Content = content,
            CreatedAt = createdAt,
            SequenceNumber = sequenceNumber,
            SyncStatus = syncStatus,
        };

        if (toolCalls != null)
        {
            message._toolCalls.AddRange(toolCalls);
        }

        return message;
    }

    /// <summary>
    /// Adds tool calls to this message.
    /// Only valid for assistant messages.
    /// </summary>
    /// <param name="toolCalls">The tool calls to add.</param>
    public void AddToolCalls(IEnumerable<ToolCall> toolCalls)
    {
        ArgumentNullException.ThrowIfNull(toolCalls);

        if (Role != "assistant")
        {
            throw new InvalidOperationException("Tool calls can only be added to assistant messages");
        }

        _toolCalls.AddRange(toolCalls);
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Gets the tool calls serialized as JSON.
    /// </summary>
    /// <returns>The tool calls JSON string, or null if no tool calls.</returns>
    public string? GetToolCallsJson()
    {
        return _toolCalls.Count > 0
            ? JsonSerializer.Serialize(_toolCalls)
            : null;
    }

    /// <summary>
    /// Marks the message as synced to remote.
    /// </summary>
    public void MarkSynced()
    {
        SyncStatus = SyncStatus.Synced;
    }

    /// <summary>
    /// Marks the message as having a sync conflict.
    /// </summary>
    public void MarkConflict()
    {
        SyncStatus = SyncStatus.Conflict;
    }

    private static void ValidateRole(string role)
    {
        ArgumentNullException.ThrowIfNull(role);

        var validRoles = new[] { "user", "assistant", "system", "tool" };
        if (!validRoles.Contains(role.ToLowerInvariant()))
        {
            throw new ArgumentException(
                $"Invalid role: {role}. Must be one of: {string.Join(", ", validRoles)}",
                nameof(role));
        }
    }

    private static void ValidateContent(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.Length > MaxContentLength)
        {
            throw new ArgumentException(
                $"Content cannot exceed {MaxContentLength} bytes",
                nameof(content));
        }
    }
}
