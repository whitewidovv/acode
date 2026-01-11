using Acode.Domain.PromptPacks;

namespace Acode.Application.PromptPacks;

/// <summary>
/// Composes final system prompts from pack components.
/// </summary>
/// <remarks>
/// The PromptComposer takes a prompt pack and composition context, then assembles
/// the final system prompt by:
/// 1. Selecting relevant components based on context (role, language, framework).
/// 2. Merging components in the correct order (system → role → language → framework).
/// 3. Applying template variable substitution.
/// 4. Deduplicating repeated sections.
/// 5. Enforcing maximum length limits.
/// </remarks>
public interface IPromptComposer
{
    /// <summary>
    /// Compose a prompt from pack components using provided context.
    /// </summary>
    /// <param name="pack">The prompt pack containing components to compose.</param>
    /// <param name="context">Composition context (role, language, framework, variables).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Composed system prompt string.</returns>
    /// <exception cref="ArgumentNullException">Pack or context is null.</exception>
    Task<string> ComposeAsync(
        PromptPack pack,
        CompositionContext context,
        CancellationToken cancellationToken = default);
}
