using System.Text.Json.Serialization;
using Acode.Infrastructure.Ollama.Models;

namespace Acode.Infrastructure.Ollama.Serialization;

/// <summary>
/// JSON source generator context for Ollama model types.
/// Provides compile-time JSON serialization without reflection for better performance.
/// </summary>
/// <remarks>
/// FR-009: RequestSerializer MUST use System.Text.Json source generators.
/// NFR-008: JSON serialization MUST use source generators (no reflection).
/// Uses snake_case naming policy to match Ollama API format.
/// Omits null values to reduce payload size.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(OllamaRequest))]
[JsonSerializable(typeof(OllamaResponse))]
[JsonSerializable(typeof(OllamaStreamChunk))]
[JsonSerializable(typeof(OllamaMessage))]
[JsonSerializable(typeof(OllamaOptions))]
[JsonSerializable(typeof(OllamaTool))]
[JsonSerializable(typeof(OllamaToolCall))]
[JsonSerializable(typeof(OllamaFunction))]
public partial class OllamaJsonContext : JsonSerializerContext
{
}
