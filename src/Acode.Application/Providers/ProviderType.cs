namespace Acode.Application.Providers;

using System.Text.Json.Serialization;

/// <summary>
/// Type of model provider.
/// </summary>
/// <remarks>
/// FR-024 to FR-028 from task-004c spec.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProviderType
{
    /// <summary>
    /// Ollama provider (local inference server).
    /// </summary>
    /// <remarks>
    /// FR-025: ProviderType MUST include Ollama value.
    /// </remarks>
    Ollama,

    /// <summary>
    /// vLLM provider (high-performance inference server).
    /// </summary>
    /// <remarks>
    /// FR-026: ProviderType MUST include Vllm value.
    /// </remarks>
    Vllm,

    /// <summary>
    /// Mock provider (for testing).
    /// </summary>
    /// <remarks>
    /// FR-027: ProviderType MUST include Mock value.
    /// </remarks>
    Mock
}
