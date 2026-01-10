namespace Acode.Application.Routing;

/// <summary>
/// Defines model capabilities that tasks may require.
/// </summary>
/// <remarks>
/// Used for capability-based model filtering in routing decisions.
/// </remarks>
public enum ModelCapability
{
    /// <summary>
    /// Model supports tool calling protocol.
    /// </summary>
    ToolCalling,

    /// <summary>
    /// Model supports function calling protocol.
    /// </summary>
    FunctionCalling,

    /// <summary>
    /// Model supports structured output (JSON mode).
    /// </summary>
    StructuredOutput,
}
