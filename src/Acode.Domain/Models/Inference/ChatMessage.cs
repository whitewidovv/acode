namespace Acode.Domain.Models.Inference;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a chat message in a conversation.
/// </summary>
/// <remarks>
/// FR-004a-11: System MUST define ChatMessage record.
/// FR-004a-12: ChatMessage MUST be immutable.
/// FR-004a-13 to FR-004a-35: Properties, validation, factories, serialization.
/// </remarks>
[method: JsonConstructor]
public sealed record ChatMessage(
    MessageRole Role,
    string? Content,
    IReadOnlyList<ToolCall>? ToolCalls,
    string? ToolCallId)
{
    /// <summary>
    /// Gets the role of the message sender.
    /// </summary>
    /// <remarks>
    /// FR-004a-13: ChatMessage MUST have Role property (required).
    /// </remarks>
    [JsonPropertyName("role")]
    public MessageRole Role { get; init; } = Role;

    /// <summary>
    /// Gets the content of the message.
    /// </summary>
    /// <remarks>
    /// FR-004a-14: ChatMessage MUST have Content property (nullable).
    /// FR-004a-20: Content MUST be non-null for User messages.
    /// FR-004a-21: Content MUST be non-null for System messages.
    /// FR-004a-22: Content MUST be non-null for Tool messages.
    /// </remarks>
    [JsonPropertyName("content")]
    public string? Content { get; init; } = ValidateContent(Role, Content, ToolCalls);

    /// <summary>
    /// Gets the list of tool calls requested by the assistant.
    /// </summary>
    /// <remarks>
    /// FR-004a-15: ChatMessage MUST have ToolCalls property (nullable).
    /// FR-004a-18: ToolCalls MUST be IReadOnlyList&lt;ToolCall&gt;.
    /// FR-004a-19: Content OR ToolCalls MUST be non-null for Assistant.
    /// </remarks>
    [JsonPropertyName("toolCalls")]
    public IReadOnlyList<ToolCall>? ToolCalls { get; init; } = ValidateToolCalls(Role, Content, ToolCalls);

    /// <summary>
    /// Gets the ID of the tool call this message is responding to.
    /// </summary>
    /// <remarks>
    /// FR-004a-16: ChatMessage MUST have ToolCallId property (nullable).
    /// FR-004a-17: ToolCallId MUST be set when Role is Tool.
    /// </remarks>
    [JsonPropertyName("toolCallId")]
    public string? ToolCallId { get; init; } = ValidateToolCallId(Role, ToolCallId);

    /// <summary>
    /// Creates a system message.
    /// </summary>
    /// <param name="content">The system message content.</param>
    /// <returns>A system ChatMessage.</returns>
    /// <remarks>
    /// FR-004a-27, FR-004a-28: Factory: CreateSystem(content).
    /// </remarks>
    public static ChatMessage CreateSystem(string content)
    {
        return new ChatMessage(MessageRole.System, content, null, null);
    }

    /// <summary>
    /// Creates a user message.
    /// </summary>
    /// <param name="content">The user message content.</param>
    /// <returns>A user ChatMessage.</returns>
    /// <remarks>
    /// FR-004a-27, FR-004a-29: Factory: CreateUser(content).
    /// </remarks>
    public static ChatMessage CreateUser(string content)
    {
        return new ChatMessage(MessageRole.User, content, null, null);
    }

    /// <summary>
    /// Creates an assistant message.
    /// </summary>
    /// <param name="content">The assistant message content (optional if toolCalls provided).</param>
    /// <param name="toolCalls">The tool calls requested by the assistant (optional if content provided).</param>
    /// <returns>An assistant ChatMessage.</returns>
    /// <remarks>
    /// FR-004a-27, FR-004a-30: Factory: CreateAssistant(content, toolCalls).
    /// </remarks>
    public static ChatMessage CreateAssistant(string? content = null, IReadOnlyList<ToolCall>? toolCalls = null)
    {
        return new ChatMessage(MessageRole.Assistant, content, toolCalls, null);
    }

    /// <summary>
    /// Creates a tool result message.
    /// </summary>
    /// <param name="toolCallId">The ID of the tool call being responded to.</param>
    /// <param name="result">The tool execution result.</param>
    /// <returns>A tool ChatMessage.</returns>
    /// <remarks>
    /// FR-004a-27, FR-004a-31: Factory: CreateToolResult(toolCallId, result, isError).
    /// </remarks>
    public static ChatMessage CreateToolResult(string toolCallId, string result)
    {
        return new ChatMessage(MessageRole.Tool, result, null, toolCallId);
    }

    /// <summary>
    /// Returns a string representation of the message.
    /// </summary>
    /// <returns>A string showing the role and content preview.</returns>
    /// <remarks>
    /// FR-004a-35: ChatMessage MUST have meaningful ToString().
    /// </remarks>
    public override string ToString()
    {
        var contentPreview = this.Content?.Length > 50
            ? this.Content.Substring(0, 47) + "..."
            : this.Content ?? string.Empty;

        return $"[{this.Role}] {contentPreview}";
    }

    private static string? ValidateContent(MessageRole role, string? content, IReadOnlyList<ToolCall>? toolCalls)
    {
        // FR-004a-32, FR-004a-33: ChatMessage MUST validate on construction
        if (role == MessageRole.User && content is null)
        {
            throw new ArgumentException("User messages must have non-null Content.", nameof(Content));
        }

        if (role == MessageRole.System && content is null)
        {
            throw new ArgumentException("System messages must have non-null Content.", nameof(Content));
        }

        if (role == MessageRole.Tool && content is null)
        {
            throw new ArgumentException("Tool messages must have non-null Content.", nameof(Content));
        }

        // FR-004a-19: Content OR ToolCalls MUST be non-null for Assistant
        if (role == MessageRole.Assistant && content is null && toolCalls is null)
        {
            throw new ArgumentException("Assistant messages must have either Content or ToolCalls (or both).", nameof(Content));
        }

        return content;
    }

    private static IReadOnlyList<ToolCall>? ValidateToolCalls(MessageRole role, string? content, IReadOnlyList<ToolCall>? toolCalls)
    {
        // FR-004a-19: Content OR ToolCalls MUST be non-null for Assistant
        if (role == MessageRole.Assistant && content is null && toolCalls is null)
        {
            throw new ArgumentException("Assistant messages must have either Content or ToolCalls (or both).", nameof(ToolCalls));
        }

        return toolCalls;
    }

    private static string? ValidateToolCallId(MessageRole role, string? toolCallId)
    {
        // FR-004a-17: ToolCallId MUST be set when Role is Tool
        if (role == MessageRole.Tool && string.IsNullOrWhiteSpace(toolCallId))
        {
            throw new ArgumentException("Tool messages must have a non-empty ToolCallId.", nameof(ToolCallId));
        }

        return toolCallId;
    }
}
