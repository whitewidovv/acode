namespace Acode.Domain.Models.Inference;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages an ordered conversation history with validation rules.
/// </summary>
/// <remarks>
/// FR-004a-101 to FR-004a-115: Thread-safe conversation management.
/// Validates message ordering: System first, User/Assistant alternation, Tool after ToolCalls.
/// </remarks>
public sealed class ConversationHistory : IEnumerable<ChatMessage>
{
    private readonly List<ChatMessage> _messages = new();
    private readonly object _lock = new();
    private HashSet<string>? _pendingToolCallIds;

    /// <summary>
    /// Gets the number of messages in the conversation.
    /// </summary>
    /// <remarks>
    /// FR-004a-111: ConversationHistory MUST have Count property.
    /// </remarks>
    public int Count
    {
        get
        {
            lock (this._lock)
            {
                return this._messages.Count;
            }
        }
    }

    /// <summary>
    /// Gets the last message in the conversation, or null if empty.
    /// </summary>
    /// <remarks>
    /// FR-004a-112: ConversationHistory MUST have LastMessage property.
    /// </remarks>
    public ChatMessage? LastMessage
    {
        get
        {
            lock (this._lock)
            {
                return this._messages.Count > 0 ? this._messages[^1] : null;
            }
        }
    }

    /// <summary>
    /// Adds a message to the conversation with validation.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <exception cref="InvalidOperationException">Thrown when message violates ordering rules.</exception>
    /// <remarks>
    /// FR-004a-103, FR-004a-104: Add method validates message order.
    /// FR-004a-105: First message MUST be System role.
    /// FR-004a-106: User/Assistant MUST alternate (with Tool interjections).
    /// FR-004a-107: Tool messages MUST follow Assistant with ToolCalls.
    /// </remarks>
    public void Add(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        lock (this._lock)
        {
            // FR-004a-105: First message MUST be System
            if (this._messages.Count == 0)
            {
                if (message.Role != MessageRole.System)
                {
                    throw new InvalidOperationException(
                        "The first message in a conversation must be a System message.");
                }

                this._messages.Add(message);
                return;
            }

            // Reject second System message
            if (message.Role == MessageRole.System)
            {
                throw new InvalidOperationException(
                    "A System message has already been added. Only one System message is allowed per conversation.");
            }

            var lastMessage = this._messages[^1];

            // Handle Tool messages - FR-004a-107
            if (message.Role == MessageRole.Tool)
            {
                // Tool must follow an Assistant message with ToolCalls
                if (lastMessage.Role != MessageRole.Assistant && lastMessage.Role != MessageRole.Tool)
                {
                    throw new InvalidOperationException(
                        "Tool messages must follow an Assistant message with tool_calls.");
                }

                // Find the most recent Assistant message to validate ToolCallId
                var recentAssistant = this._messages
                    .Where(m => m.Role == MessageRole.Assistant && m.ToolCalls != null)
                    .LastOrDefault();

                if (recentAssistant == null)
                {
                    throw new InvalidOperationException(
                        "Tool messages must follow an Assistant message with tool_calls.");
                }

                // Lazy init pending tool call IDs
                if (this._pendingToolCallIds == null)
                {
                    this._pendingToolCallIds = new HashSet<string>(
                        recentAssistant.ToolCalls!.Select(tc => tc.Id));
                }

                // Validate ToolCallId matches one of the pending calls
                if (!this._pendingToolCallIds.Contains(message.ToolCallId!))
                {
                    throw new InvalidOperationException(
                        $"Tool message tool_call_id '{message.ToolCallId}' does not match any pending tool call.");
                }

                // Remove from pending set
                this._pendingToolCallIds.Remove(message.ToolCallId!);

                // If all tool calls resolved, clear the set
                if (this._pendingToolCallIds.Count == 0)
                {
                    this._pendingToolCallIds = null;
                }

                this._messages.Add(message);
                return;
            }

            // Handle User/Assistant alternation - FR-004a-106
            // After System or Tool, expect User
            if (lastMessage.Role == MessageRole.System || lastMessage.Role == MessageRole.Tool)
            {
                if (message.Role != MessageRole.User)
                {
                    throw new InvalidOperationException(
                        "After a System or Tool message, the next message must be a User message.");
                }

                this._messages.Add(message);
                return;
            }

            // After User, expect Assistant
            if (lastMessage.Role == MessageRole.User)
            {
                if (message.Role != MessageRole.Assistant)
                {
                    throw new InvalidOperationException(
                        "After a User message, the next message must be an Assistant message.");
                }

                // Initialize pending tool calls if Assistant has ToolCalls
                if (message.ToolCalls != null && message.ToolCalls.Count > 0)
                {
                    this._pendingToolCallIds = new HashSet<string>(
                        message.ToolCalls.Select(tc => tc.Id));
                }

                this._messages.Add(message);
                return;
            }

            // After Assistant (without pending tool calls), expect User
            if (lastMessage.Role == MessageRole.Assistant)
            {
                // If there are pending tool calls, User is not allowed yet
                if (this._pendingToolCallIds != null && this._pendingToolCallIds.Count > 0)
                {
                    throw new InvalidOperationException(
                        "Tool result messages are still pending. Provide all tool results before continuing.");
                }

                if (message.Role != MessageRole.User)
                {
                    throw new InvalidOperationException(
                        "User and Assistant messages must alternate.");
                }

                this._messages.Add(message);
                return;
            }

            throw new InvalidOperationException($"Unexpected message role: {message.Role}");
        }
    }

    /// <summary>
    /// Gets an immutable copy of all messages in the conversation.
    /// </summary>
    /// <returns>Read-only list of messages in chronological order.</returns>
    /// <remarks>
    /// FR-004a-108, FR-004a-109: GetMessages returns IReadOnlyList&lt;ChatMessage&gt;.
    /// </remarks>
    public IReadOnlyList<ChatMessage> GetMessages()
    {
        lock (this._lock)
        {
            return this._messages.ToArray();
        }
    }

    /// <summary>
    /// Clears all messages from the conversation.
    /// </summary>
    /// <remarks>
    /// FR-004a-110: ConversationHistory MUST have Clear method.
    /// </remarks>
    public void Clear()
    {
        lock (this._lock)
        {
            this._messages.Clear();
            this._pendingToolCallIds = null;
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the conversation messages.
    /// </summary>
    /// <returns>An enumerator for the messages.</returns>
    /// <remarks>
    /// FR-004a-113: ConversationHistory MUST support enumeration.
    /// </remarks>
    public IEnumerator<ChatMessage> GetEnumerator()
    {
        // Return a copy to avoid lock issues during enumeration
        return this.GetMessages().GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the conversation messages.
    /// </summary>
    /// <returns>An enumerator for the messages.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
