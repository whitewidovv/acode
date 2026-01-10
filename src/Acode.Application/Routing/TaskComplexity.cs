namespace Acode.Application.Routing;

/// <summary>
/// Defines task complexity levels for adaptive routing decisions.
/// </summary>
/// <remarks>
/// Used by adaptive strategy to select appropriately-sized models.
/// Simple tasks use smaller models; complex tasks use larger models.
/// </remarks>
public enum TaskComplexity
{
    /// <summary>
    /// Unknown complexity—routing policy uses defaults.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Low complexity tasks—variable renaming, simple refactors, documentation.
    /// Can use small models (7B parameters) effectively.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium complexity tasks—standard implementations, bug fixes.
    /// Benefits from medium models (13B parameters).
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High complexity tasks—architecture decisions, complex algorithms.
    /// Requires large models (70B parameters) for quality output.
    /// </summary>
    High = 3,
}
