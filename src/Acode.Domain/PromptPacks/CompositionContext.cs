namespace Acode.Domain.PromptPacks;

/// <summary>
/// Provides context for composing prompts from a prompt pack.
/// </summary>
/// <param name="Role">Optional role to include in composition (e.g., "planner", "coder", "reviewer").</param>
/// <param name="Language">Optional language-specific component to include (e.g., "csharp", "typescript").</param>
/// <param name="Framework">Optional framework-specific component to include (e.g., "aspnetcore", "react").</param>
/// <param name="Variables">Template variables for substitution. Key is variable name, value is replacement value.</param>
public sealed record CompositionContext(
    string? Role = null,
    string? Language = null,
    string? Framework = null,
    IReadOnlyDictionary<string, string>? Variables = null)
{
    /// <summary>
    /// Gets the variables dictionary, creating an empty one if null.
    /// </summary>
    public IReadOnlyDictionary<string, string> VariablesOrEmpty =>
        Variables ?? new Dictionary<string, string>();

    /// <summary>
    /// Creates a composition context with only variables.
    /// </summary>
    /// <param name="variables">Template variables.</param>
    /// <returns>Composition context with variables.</returns>
    public static CompositionContext WithVariables(IReadOnlyDictionary<string, string> variables)
    {
        return new CompositionContext(Variables: variables);
    }

    /// <summary>
    /// Creates a composition context for a specific role.
    /// </summary>
    /// <param name="role">Role name.</param>
    /// <param name="variables">Optional template variables.</param>
    /// <returns>Composition context with role.</returns>
    public static CompositionContext ForRole(string role, IReadOnlyDictionary<string, string>? variables = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        return new CompositionContext(Role: role, Variables: variables);
    }

    /// <summary>
    /// Creates a composition context for a specific language and framework.
    /// </summary>
    /// <param name="language">Language name.</param>
    /// <param name="framework">Optional framework name.</param>
    /// <param name="variables">Optional template variables.</param>
    /// <returns>Composition context with language and framework.</returns>
    public static CompositionContext ForTechnology(
        string language,
        string? framework = null,
        IReadOnlyDictionary<string, string>? variables = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        return new CompositionContext(Language: language, Framework: framework, Variables: variables);
    }
}
