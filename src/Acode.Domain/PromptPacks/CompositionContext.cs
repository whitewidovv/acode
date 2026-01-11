namespace Acode.Domain.PromptPacks;

/// <summary>
/// Context for prompt composition including role, language, framework, and template variables.
/// </summary>
/// <remarks>
/// The CompositionContext provides all the information needed to compose a final system prompt
/// from pack components. It includes:
/// - Role: The agent role (planner, coder, reviewer) that determines which role-specific prompts to include.
/// - Language: The primary programming language for language-specific prompts.
/// - Framework: The framework being used for framework-specific prompts.
/// - Variables: Template variable values from multiple sources with defined priority.
/// Variable resolution priority (highest to lowest):
/// 1. ConfigVariables - from .agent/config.yml prompts.variables section.
/// 2. EnvironmentVariables - from ACODE_PROMPT_VAR_* environment variables.
/// 3. ContextVariables - from runtime context (workspace, date, etc.).
/// 4. DefaultVariables - built-in defaults.
/// </remarks>
public sealed record CompositionContext
{
    /// <summary>
    /// Gets the current agent role (planner, coder, reviewer).
    /// </summary>
    /// <remarks>
    /// Used to select role-specific prompts from the pack.
    /// If null, no role-specific prompt is included in composition.
    /// </remarks>
    public string? Role { get; init; }

    /// <summary>
    /// Gets the primary programming language.
    /// </summary>
    /// <remarks>
    /// Used to select language-specific prompts (e.g., csharp.md, typescript.md).
    /// If null, no language-specific prompt is included in composition.
    /// </remarks>
    public string? Language { get; init; }

    /// <summary>
    /// Gets the framework being used.
    /// </summary>
    /// <remarks>
    /// Used to select framework-specific prompts (e.g., aspnetcore.md, react.md).
    /// If null, no framework-specific prompt is included in composition.
    /// </remarks>
    public string? Framework { get; init; }

    /// <summary>
    /// Gets template variables (all sources merged).
    /// </summary>
    /// <remarks>
    /// Pre-merged variables from all sources with priority already applied.
    /// Use this for simple scenarios where priority handling is done externally.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Variables { get; init; }
        = new Dictionary<string, string>();

    /// <summary>
    /// Gets variables from configuration file (.agent/config.yml).
    /// </summary>
    /// <remarks>
    /// Highest priority variable source.
    /// These override all other variable sources.
    /// </remarks>
    public IReadOnlyDictionary<string, string> ConfigVariables { get; init; }
        = new Dictionary<string, string>();

    /// <summary>
    /// Gets variables from environment.
    /// </summary>
    /// <remarks>
    /// Second highest priority.
    /// Populated from ACODE_PROMPT_VAR_* environment variables.
    /// </remarks>
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; init; }
        = new Dictionary<string, string>();

    /// <summary>
    /// Gets variables from runtime context.
    /// </summary>
    /// <remarks>
    /// Third priority.
    /// Includes workspace_name, current date, OS info, etc.
    /// </remarks>
    public IReadOnlyDictionary<string, string> ContextVariables { get; init; }
        = new Dictionary<string, string>();

    /// <summary>
    /// Gets default built-in variables.
    /// </summary>
    /// <remarks>
    /// Lowest priority.
    /// Provides fallback values when not specified elsewhere.
    /// </remarks>
    public IReadOnlyDictionary<string, string> DefaultVariables { get; init; }
        = new Dictionary<string, string>();
}
