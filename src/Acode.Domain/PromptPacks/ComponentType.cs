namespace Acode.Domain.PromptPacks;

/// <summary>
/// Defines the types of components that can be included in a prompt pack.
/// </summary>
public enum ComponentType
{
    /// <summary>
    /// Base system prompt that defines the agent's core identity and capabilities.
    /// </summary>
    System = 0,

    /// <summary>
    /// Role-specific guidance (e.g., planner, coder, reviewer).
    /// </summary>
    Role = 1,

    /// <summary>
    /// Language-specific conventions and patterns (e.g., csharp, typescript, python).
    /// </summary>
    Language = 2,

    /// <summary>
    /// Framework-specific guidance (e.g., aspnetcore, react, nextjs).
    /// </summary>
    Framework = 3,

    /// <summary>
    /// Custom user-defined component.
    /// </summary>
    Custom = 4,
}
