namespace Acode.Domain.Models.Inference.Serialization;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Acode.Domain.Models.Inference;

/// <summary>
/// JSON source generator context for response types (performance optimization).
/// </summary>
/// <remarks>
/// FR-004b-101, FR-004b-109: System.Text.Json source generators for AOT compilation.
/// NFR-004b-06, NFR-004b-07: Optimized serialization performance.
/// </remarks>
[JsonSerializable(typeof(ChatResponse))]
[JsonSerializable(typeof(FinishReason))]
[JsonSerializable(typeof(UsageInfo))]
[JsonSerializable(typeof(ResponseMetadata))]
[JsonSerializable(typeof(ResponseDelta))]
[JsonSerializable(typeof(ContentFilterResult))]
[JsonSerializable(typeof(FilterCategory))]
[JsonSerializable(typeof(FilterSeverity))]
[JsonSerializable(typeof(List<ChatResponse>))]
[JsonSerializable(typeof(List<ContentFilterResult>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
internal partial class ResponseJsonContext : JsonSerializerContext
{
}
