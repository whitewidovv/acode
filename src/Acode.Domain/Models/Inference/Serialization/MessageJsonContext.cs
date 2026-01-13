namespace Acode.Domain.Models.Inference.Serialization;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON source generator context for message types.
/// </summary>
/// <remarks>
/// NFR-004a-10: System MUST use System.Text.Json source generators for performance.
/// Provides ahead-of-time JSON serialization for all message and tool-call types.
/// </remarks>
[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(ToolCall))]
[JsonSerializable(typeof(ToolResult))]
[JsonSerializable(typeof(ToolDefinition))]
[JsonSerializable(typeof(ToolCallDelta))]
[JsonSerializable(typeof(MessageRole))]
[JsonSerializable(typeof(List<ChatMessage>))]
[JsonSerializable(typeof(IReadOnlyList<ChatMessage>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
internal partial class MessageJsonContext : JsonSerializerContext
{
}
