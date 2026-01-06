using Acode.Domain.PromptPacks;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Composes final prompts from prompt pack components based on context.
/// </summary>
public interface IPromptComposer
{
    /// <summary>
    /// Composes a final prompt by combining pack components according to context.
    /// </summary>
    /// <param name="pack">Prompt pack containing components.</param>
    /// <param name="context">Composition context specifying role, language, framework, and variables.</param>
    /// <returns>Composed prompt text with all components merged and variables substituted.</returns>
    /// <remarks>
    /// Composition order (hierarchical merging):
    /// 1. Base system prompt (system.md)
    /// 2. Role-specific prompt (roles/{role}.md) if context.Role specified
    /// 3. Language-specific prompt (languages/{language}.md) if context.Language specified
    /// 4. Framework-specific prompt (frameworks/{framework}.md) if context.Framework specified
    /// 5. Template variable substitution applied to final result
    ///
    /// Components are separated by double newlines.
    /// Missing optional components are skipped.
    /// Maximum composed prompt length: 32,000 characters (truncated with warning if exceeded).
    /// </remarks>
    string Compose(PromptPack pack, CompositionContext context);
}
