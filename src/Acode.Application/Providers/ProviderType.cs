namespace Acode.Application.Providers;

/// <summary>
/// Type of model provider (local vs remote).
/// </summary>
/// <remarks>
/// FR-027 to FR-030 from task-004c spec.
/// Gap #2 from task-004c completion checklist.
/// </remarks>
public enum ProviderType
{
    /// <summary>
    /// Local provider (Ollama, local vLLM).
    /// </summary>
    Local,

    /// <summary>
    /// Remote provider (remote vLLM, other remote endpoints).
    /// </summary>
    Remote,

    /// <summary>
    /// Embedded provider (future: embedded models).
    /// </summary>
    Embedded
}
