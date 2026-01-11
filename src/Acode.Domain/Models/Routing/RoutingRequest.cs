namespace Acode.Domain.Models.Routing;

/// <summary>
/// Represents a request for model routing based on agent role and context.
/// </summary>
/// <remarks>
/// Routing requests are created when the system needs to select a model for
/// a specific agent role. The request can optionally include a user override
/// to bypass the routing policy and force a specific model selection.
///
/// User overrides are useful for debugging, experimentation, or situations
/// where the user has context the routing policy cannot capture.
/// </remarks>
public sealed record RoutingRequest
{
    /// <summary>
    /// Gets the agent role for which a model is being requested.
    /// </summary>
    public required AgentRole Role { get; init; }

    /// <summary>
    /// Gets the user-specified model override, if any.
    /// </summary>
    /// <remarks>
    /// When set, the routing policy should bypass normal routing logic
    /// and select the specified model (subject to operating mode constraints
    /// and availability checks).
    ///
    /// Example: "llama3.2:70b", "mistral:7b", "qwen2.5:14b".
    /// </remarks>
    public string? UserOverride { get; init; }

    /// <summary>
    /// Gets a value indicating whether a user override is present.
    /// </summary>
    public bool HasUserOverride => !string.IsNullOrWhiteSpace(UserOverride);
}
