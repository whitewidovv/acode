namespace Acode.Application.Routing;

/// <summary>
/// Represents metadata about an available model.
/// </summary>
/// <remarks>
/// AC-006: ListAvailableModels returns model metadata.
/// </remarks>
public sealed class ModelInfo
{
    /// <summary>
    /// Gets the model identifier (e.g., "llama3.2:70b").
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets the provider hosting this model (e.g., "Ollama", "vLLM").
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// Gets a value indicating whether this model is hosted locally (true) or remotely (false).
    /// </summary>
    /// <remarks>
    /// AC-035 to AC-037: Used for operating mode constraint validation.
    /// </remarks>
    public required bool IsLocal { get; init; }

    /// <summary>
    /// Gets the parameter count of the model.
    /// </summary>
    public required long ParameterCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether this model supports tool calling.
    /// </summary>
    public required bool SupportsToolCalling { get; init; }

    /// <summary>
    /// Gets a value indicating whether this model is currently loaded and available.
    /// </summary>
    /// <remarks>
    /// AC-039 to AC-042: Availability checking.
    /// </remarks>
    public required bool IsAvailable { get; init; }
}
