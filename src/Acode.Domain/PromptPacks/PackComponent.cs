namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents a component within a prompt pack.
/// </summary>
/// <remarks>
/// A pack component is a file within the pack directory that contains prompt content.
/// Components can be system prompts, role-specific guidance, language conventions, or framework patterns.
/// </remarks>
public sealed record PackComponent
{
    /// <summary>
    /// Gets the relative path to the component file within the pack directory.
    /// </summary>
    /// <remarks>
    /// Path must use forward slashes and be relative to the pack root.
    /// Examples: "system.md", "roles/coder.md", "languages/csharp.md".
    /// </remarks>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the component type.
    /// </summary>
    public required ComponentType Type { get; init; }

    /// <summary>
    /// Gets the role identifier for Role-type components.
    /// </summary>
    /// <remarks>
    /// Only applicable when Type is ComponentType.Role.
    /// Examples: "planner", "coder", "reviewer".
    /// </remarks>
    public string? Role { get; init; }

    /// <summary>
    /// Gets the language identifier for Language-type components.
    /// </summary>
    /// <remarks>
    /// Only applicable when Type is ComponentType.Language.
    /// Examples: "csharp", "typescript", "python".
    /// </remarks>
    public string? Language { get; init; }

    /// <summary>
    /// Gets the framework identifier for Framework-type components.
    /// </summary>
    /// <remarks>
    /// Only applicable when Type is ComponentType.Framework.
    /// Examples: "aspnetcore", "react", "nextjs".
    /// </remarks>
    public string? Framework { get; init; }

    /// <summary>
    /// Gets the component content (markdown text).
    /// </summary>
    /// <remarks>
    /// Content is loaded from the component file and may contain template variables.
    /// </remarks>
    public string? Content { get; init; }
}
