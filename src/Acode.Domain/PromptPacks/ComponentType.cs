namespace Acode.Domain.PromptPacks;

/// <summary>
/// Defines the types of components that can be included in a prompt pack.
/// </summary>
public enum ComponentType
{
    /// <summary>
    /// Core system-level prompts defining fundamental agent behavior.
    /// </summary>
    System,

    /// <summary>
    /// Role-specific behavioral configurations.
    /// </summary>
    Role,

    /// <summary>
    /// Programming language-specific guidelines.
    /// </summary>
    Language,

    /// <summary>
    /// Framework-specific patterns and practices.
    /// </summary>
    Framework,

    /// <summary>
    /// User-defined custom component type.
    /// </summary>
    Custom,
}
