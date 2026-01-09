// src/Acode.Domain/Conversation/ToolCall.cs
namespace Acode.Domain.Conversation;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Value object representing a tool invocation within a Message.
/// Immutable record type with status tracking.
/// </summary>
public sealed record ToolCall
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCall"/> class.
    /// </summary>
    /// <param name="id">The tool call ID.</param>
    /// <param name="function">The function name.</param>
    /// <param name="arguments">The arguments JSON.</param>
    public ToolCall(string id, string function, string arguments)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Function = function ?? throw new ArgumentNullException(nameof(function));
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        Status = ToolCallStatus.Pending;
    }

    /// <summary>
    /// Gets the unique identifier for this tool call.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; }

    /// <summary>
    /// Gets the function/tool name being invoked.
    /// </summary>
    [JsonPropertyName("function")]
    public string Function { get; init; }

    /// <summary>
    /// Gets the arguments passed to the tool as JSON string.
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; init; }

    /// <summary>
    /// Gets the result of the tool execution.
    /// </summary>
    [JsonPropertyName("result")]
    public string? Result { get; init; }

    /// <summary>
    /// Gets the current status of the tool call.
    /// </summary>
    [JsonPropertyName("status")]
    public ToolCallStatus Status { get; init; }

    /// <summary>
    /// Creates a new ToolCall with the result set and status Completed.
    /// </summary>
    /// <param name="result">The tool execution result.</param>
    /// <returns>A new ToolCall instance with the result.</returns>
    public ToolCall WithResult(string result)
    {
        return this with
        {
            Result = result,
            Status = ToolCallStatus.Completed,
        };
    }

    /// <summary>
    /// Creates a new ToolCall with the error set and status Failed.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A new ToolCall instance with the error.</returns>
    public ToolCall WithError(string error)
    {
        return this with
        {
            Result = error,
            Status = ToolCallStatus.Failed,
        };
    }

    /// <summary>
    /// Parses the arguments JSON into a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>The deserialized arguments or null if deserialization fails.</returns>
    public T? ParseArguments<T>()
        where T : class
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        return JsonSerializer.Deserialize<T>(Arguments, options);
    }
}
