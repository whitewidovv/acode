using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Composes final prompts from prompt pack components.
/// </summary>
public sealed class PromptComposer : IPromptComposer
{
    private const int MaxPromptLength = 32000;
    private const string ComponentSeparator = "\n\n";

    private readonly ITemplateEngine _templateEngine;
    private readonly ILogger<PromptComposer>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptComposer"/> class.
    /// </summary>
    /// <param name="templateEngine">Template engine for variable substitution.</param>
    /// <param name="logger">Optional logger.</param>
    public PromptComposer(ITemplateEngine templateEngine, ILogger<PromptComposer>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(templateEngine);
        _templateEngine = templateEngine;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string Compose(PromptPack pack, CompositionContext context)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(context);

        var components = new List<string>();

        // 1. Base system prompt (always included)
        if (pack.Components.TryGetValue("system.md", out var systemComponent) &&
            !string.IsNullOrEmpty(systemComponent.Content))
        {
            components.Add(systemComponent.Content);
        }

        // 2. Role-specific prompt (if context.Role specified)
        if (!string.IsNullOrWhiteSpace(context.Role))
        {
            var rolePath = $"roles/{context.Role}.md";
            if (pack.Components.TryGetValue(rolePath, out var roleComponent) &&
                !string.IsNullOrEmpty(roleComponent.Content))
            {
                components.Add(roleComponent.Content);
            }
        }

        // 3. Language-specific prompt (if context.Language specified)
        if (!string.IsNullOrWhiteSpace(context.Language))
        {
            var languagePath = $"languages/{context.Language}.md";
            if (pack.Components.TryGetValue(languagePath, out var languageComponent) &&
                !string.IsNullOrEmpty(languageComponent.Content))
            {
                components.Add(languageComponent.Content);
            }
        }

        // 4. Framework-specific prompt (if context.Framework specified)
        if (!string.IsNullOrWhiteSpace(context.Framework))
        {
            var frameworkPath = $"frameworks/{context.Framework}.md";
            if (pack.Components.TryGetValue(frameworkPath, out var frameworkComponent) &&
                !string.IsNullOrEmpty(frameworkComponent.Content))
            {
                components.Add(frameworkComponent.Content);
            }
        }

        // Join components with double newlines
        var composed = string.Join(ComponentSeparator, components);

        // 5. Template variable substitution
        var variables = context.VariablesOrEmpty;
        var substituted = _templateEngine.Substitute(composed, new Dictionary<string, string>(variables));

        // Enforce maximum length
        if (substituted.Length > MaxPromptLength)
        {
            _logger?.LogWarning(
                "Composed prompt exceeds maximum length of {MaxLength} characters. Truncating from {ActualLength} characters.",
                MaxPromptLength,
                substituted.Length);

            substituted = substituted.Substring(0, MaxPromptLength);
        }

        return substituted;
    }
}
