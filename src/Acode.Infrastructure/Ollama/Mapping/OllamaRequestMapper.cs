using Acode.Application.Inference;
using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Ollama.Models;

namespace Acode.Infrastructure.Ollama.Mapping;

/// <summary>
/// Maps Acode's ChatRequest to Ollama's request format.
/// </summary>
public static class OllamaRequestMapper
{
    /// <summary>
    /// Maps a ChatRequest to an OllamaRequest.
    /// </summary>
    /// <param name="chatRequest">The Acode ChatRequest.</param>
    /// <param name="defaultModel">The default model to use if none specified in request.</param>
    /// <param name="keepAlive">The keep_alive duration (e.g., "5m").</param>
    /// <returns>An OllamaRequest ready for serialization.</returns>
    public static OllamaRequest Map(ChatRequest chatRequest, string? defaultModel = null, string? keepAlive = null)
    {
        ArgumentNullException.ThrowIfNull(chatRequest);

        // FR-010: Set model from request or default
        var model = chatRequest.ModelParameters?.Model ?? defaultModel ?? "llama3.2:latest";

        // FR-012: Map messages array
        var messages = chatRequest.Messages.Select(MapMessage).ToArray();

        // FR-014: Include options if parameters provided
        var options = chatRequest.ModelParameters != null
            ? MapOptions(chatRequest.ModelParameters)
            : null;

        // FR-013: Map tool definitions if present
        var tools = chatRequest.Tools != null && chatRequest.Tools.Length > 0
            ? chatRequest.Tools.Select(MapTool).ToArray()
            : null;

        return new OllamaRequest(
            model: model,
            messages: messages,
            stream: chatRequest.Stream,
            tools: tools,
            format: null, // JSON mode not yet supported
            options: options,
            keepAlive: keepAlive);
    }

    /// <summary>
    /// Maps a ChatMessage to an OllamaMessage.
    /// </summary>
    private static OllamaMessage MapMessage(ChatMessage message)
    {
        return new OllamaMessage(
            role: message.Role.ToString().ToLowerInvariant(),
            content: message.Content,
            toolCalls: null, // Tool calls will be added in tool call integration
            toolCallId: null);
    }

    /// <summary>
    /// Maps ModelParameters to OllamaOptions.
    /// </summary>
    private static OllamaOptions MapOptions(ModelParameters parameters)
    {
        return new OllamaOptions(
            temperature: parameters.Temperature,
            topP: parameters.TopP,
            seed: parameters.Seed,
            numCtx: parameters.MaxTokens,
            stop: parameters.StopSequences);
    }

    /// <summary>
    /// Maps a ToolDefinition to an OllamaTool.
    /// </summary>
    private static OllamaTool MapTool(ToolDefinition tool)
    {
        var function = new OllamaToolDefinition(
            name: tool.Name,
            description: tool.Description,
            parameters: tool.Parameters,
            strict: tool.Strict);

        return new OllamaTool(
            type: "function",
            function: function);
    }
}
