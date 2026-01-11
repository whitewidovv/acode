namespace Acode.Domain.Models.Routing;

/// <summary>
/// Represents the result of a routing policy decision.
/// </summary>
/// <remarks>
/// A routing decision contains the selected model ID, whether the selection
/// was a fallback due to primary model unavailability, and the reasoning
/// behind the decision for audit and debugging purposes.
///
/// Routing decisions are immutable and logged for traceability. The reason
/// field provides context for why a particular model was selected, enabling
/// debugging of routing behavior and workflow analysis.
/// </remarks>
public sealed record RoutingDecision
{
    /// <summary>
    /// Gets the selected model identifier.
    /// </summary>
    /// <remarks>
    /// Model ID follows the format "name:tag" (e.g., "llama3.2:70b", "mistral:7b").
    /// The ID must be recognized by the configured model provider (Ollama, vLLM).
    /// </remarks>
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets a value indicating whether this selection is a fallback.
    /// </summary>
    /// <remarks>
    /// True if the primary model was unavailable and the policy fell back
    /// to an alternative. False if the primary model was selected successfully.
    /// Fallback status is logged for monitoring and capacity planning.
    /// </remarks>
    public required bool IsFallback { get; init; }

    /// <summary>
    /// Gets the human-readable reason for this routing decision.
    /// </summary>
    /// <remarks>
    /// Example reasons:
    /// - "Role-based routing: planner role → llama3.2:70b"
    /// - "User override specified: llama3.2:7b"
    /// - "Primary model unavailable, using fallback chain"
    /// - "Single model strategy: llama3.2:7b for all roles"
    ///
    /// The reason is logged for audit purposes and debugging.
    /// </remarks>
    public required string Reason { get; init; }
}
