using System.Text.Json.Serialization;
using Acode.Infrastructure.Vllm.Models;

namespace Acode.Infrastructure.Vllm.Client.Serialization;

/// <summary>
/// JSON serializer context for vLLM types using source generators.
/// </summary>
/// <remarks>
/// FR-016, AC-016: MUST use System.Text.Json source generators for performance.
/// Source generators compile-time generate serialization code, avoiding reflection overhead.
/// </remarks>
[JsonSerializable(typeof(VllmRequest))]
[JsonSerializable(typeof(VllmResponse))]
[JsonSerializable(typeof(VllmStreamChunk))]
[JsonSerializable(typeof(VllmMessage))]
[JsonSerializable(typeof(VllmChoice))]
[JsonSerializable(typeof(VllmDelta))]
[JsonSerializable(typeof(VllmToolCall))]
[JsonSerializable(typeof(VllmFunction))]
[JsonSerializable(typeof(VllmUsage))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
public partial class VllmJsonSerializerContext : JsonSerializerContext
{
}
