using System.Text.Json;
using System.Text.Json.Serialization;
using Acode.Infrastructure.Vllm.Models;

namespace Acode.Infrastructure.Vllm.Client.Serialization;

/// <summary>
/// Serializer for vLLM requests and responses using System.Text.Json.
/// </summary>
/// <remarks>
/// FR-016, AC-016: Uses source-generated serializers from VllmJsonSerializerContext
/// for performance and AOT compatibility (no reflection).
/// </remarks>
public static class VllmRequestSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        TypeInfoResolver = VllmJsonSerializerContext.Default,  // FR-016: Use source generator
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    // FR-016: Generic serialization options (uses reflection for unknown types)
    private static readonly JsonSerializerOptions GenericOptions = new()
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
    /// Serializes any object to JSON using vLLM serialization conventions.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="request">The object to serialize.</param>
    /// <returns>JSON string.</returns>
    /// <remarks>
    /// FR-016, AC-016: Uses reflection-based serialization for generic types.
    /// Applies snake_case naming policy to match vLLM API expectations.
    /// </remarks>
    public static string SerializeGeneric<T>(T request)
        where T : class
    {
        return JsonSerializer.Serialize(request, GenericOptions);
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
