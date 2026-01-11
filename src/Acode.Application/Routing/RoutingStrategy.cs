namespace Acode.Application.Routing;

/// <summary>
/// Defines the routing strategies available for model selection.
/// </summary>
/// <remarks>
/// AC-017 to AC-019: Configuration supports multiple routing strategies.
/// </remarks>
public enum RoutingStrategy
{
    /// <summary>
    /// Single model strategy—uses the same model for all roles.
    /// Simplest configuration for homogeneous workloads.
    /// </summary>
    /// <remarks>
    /// AC-019: Default strategy when not explicitly configured.
    /// AC-023 to AC-025: Single strategy uses default_model for all roles.
    /// </remarks>
    SingleModel = 0,

    /// <summary>
    /// Role-based strategy—assigns different models to different roles.
    /// Enables optimization by matching model capabilities to role requirements.
    /// </summary>
    /// <remarks>
    /// AC-026 to AC-029: Role-based strategy reads role_models configuration.
    /// </remarks>
    RoleBased = 1,

    /// <summary>
    /// Adaptive strategy—dynamically selects models based on task complexity.
    /// Analyzes context to choose optimal model for each request.
    /// </summary>
    /// <remarks>
    /// Future enhancement: complexity-aware model selection.
    /// </remarks>
    Adaptive = 2,
}
