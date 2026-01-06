using Acode.Domain.PromptPacks;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Provides template variable substitution for prompt pack components.
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    /// Substitutes variables in a template string using Mustache-style syntax ({{variable}}).
    /// </summary>
    /// <param name="templateText">Template string containing {{variable}} placeholders.</param>
    /// <param name="variables">Dictionary of variable names to values.</param>
    /// <returns>Template string with all variables substituted.</returns>
    /// <remarks>
    /// Missing variables are replaced with empty strings.
    /// Variable values exceeding 1024 characters are rejected.
    /// Supports variable resolution priority: config > env > context > default.
    /// Detects recursive expansion (max depth 3).
    /// </remarks>
    string Substitute(string templateText, Dictionary<string, string> variables);

    /// <summary>
    /// Validates that a template string has correct syntax.
    /// </summary>
    /// <param name="templateText">Template string to validate.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    ValidationResult ValidateTemplate(string templateText);
}
