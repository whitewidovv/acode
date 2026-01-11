namespace Acode.Application.Routing;

using System;

/// <summary>
/// Represents the result of a routing decision, including the selected model,
/// fallback status, and decision reasoning.
/// </summary>
/// <remarks>
/// Routing decisions are immutable value objects. They can be safely logged,
/// cached, and passed between components without defensive copying.
/// AC-002, AC-003: GetModel returns RoutingDecision with model configuration.
/// </remarks>
public sealed class RoutingDecision
{
    /// <summary>
    /// Gets the selected model identifier (e.g., "llama3.2:70b" or "llama3.2:70b@ollama").
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets a value indicating whether this decision represents a fallback selection (primary model unavailable).
    /// </summary>
    /// <remarks>
    /// AC-030 to AC-034: Fallback status tracking.
    /// </remarks>
    public required bool IsFallback { get; init; }

    /// <summary>
    /// Gets the reason for fallback (only populated when IsFallback is true).
    /// Examples: "primary_unavailable", "mode_constraint_violation", "capability_mismatch".
    /// </summary>
    public string? FallbackReason { get; init; }

    /// <summary>
    /// Gets a human-readable explanation of why this model was selected.
    /// Examples: "role-based strategy", "user override", "adaptive strategy (high complexity)".
    /// </summary>
    /// <remarks>
    /// AC-050: Selection reason is logged.
    /// </remarks>
    public required string SelectionReason { get; init; }

    /// <summary>
    /// Gets the provider hosting the selected model (e.g., "Ollama", "vLLM").
    /// </summary>
    public string? SelectedProvider { get; init; }

    /// <summary>
    /// Gets the time taken to make this routing decision in milliseconds.
    /// Used for performance monitoring and optimization.
    /// </summary>
    public required long DecisionTimeMs { get; init; }

    /// <summary>
    /// Gets the timestamp when this decision was made.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}
