using System.Text.Json;
using System.Text.Json.Serialization;
using Acode.Infrastructure.Vllm.Models;

namespace Acode.Infrastructure.Vllm.Serialization;

/// <summary>
/// Serializer for vLLM requests and responses using System.Text.Json.
/// </summary>
public static class VllmRequestSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a vLLM request to JSON.
    /// </summary>
    /// <param name="request">The request to serialize.</param>
    /// <returns>JSON string.</returns>
    public static string Serialize(VllmRequest request)
    {
        return JsonSerializer.Serialize(request, Options);
    }

    /// <summary>
    /// Deserializes a vLLM response from JSON.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <returns>Parsed response.</returns>
    public static VllmResponse DeserializeResponse(string json)
    {
        return JsonSerializer.Deserialize<VllmResponse>(json, Options)
            ?? throw new JsonException("Failed to deserialize VllmResponse");
    }

    /// <summary>
    /// Deserializes a vLLM streaming chunk from JSON.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <returns>Parsed stream chunk.</returns>
    public static VllmStreamChunk DeserializeStreamChunk(string json)
    {
        return JsonSerializer.Deserialize<VllmStreamChunk>(json, Options)
            ?? throw new JsonException("Failed to deserialize VllmStreamChunk");
    }
}
