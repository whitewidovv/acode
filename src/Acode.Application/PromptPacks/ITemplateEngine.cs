using Acode.Domain.PromptPacks;
using Acode.Domain.PromptPacks.Exceptions;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Processes Mustache-style template variables in prompts.
/// </summary>
/// <remarks>
/// The TemplateEngine substitutes {{variable}} placeholders with values from
/// the CompositionContext. Features include:
/// - Single and multiple variable substitution.
/// - Missing variable handling (replaced with empty string).
/// - HTML entity escaping for security.
/// - Variable value length limits.
/// - Recursive expansion detection.
/// - Variable priority resolution (config > env > context > defaults).
/// </remarks>
public interface ITemplateEngine
{
    /// <summary>
    /// Substitute template variables in content.
    /// </summary>
    /// <param name="content">Template content with {{variable}} placeholders.</param>
    /// <param name="context">Composition context with variable values.</param>
    /// <returns>Content with variables substituted.</returns>
    /// <exception cref="ArgumentNullException">Context is null.</exception>
    /// <exception cref="TemplateVariableException">
    /// Variable value exceeds maximum length or circular reference detected.
    /// </exception>
    string Substitute(string content, CompositionContext context);
}
