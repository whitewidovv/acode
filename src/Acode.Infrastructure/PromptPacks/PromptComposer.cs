using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Composes final system prompts from pack components.
/// </summary>
public sealed class PromptComposer : IPromptComposer
{
    private readonly ITemplateEngine _templateEngine;
    private readonly ComponentMerger _merger;
    private readonly int _maxLength;
    private readonly ILogger<PromptComposer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptComposer"/> class.
    /// </summary>
    /// <param name="templateEngine">The template engine for variable substitution.</param>
    /// <param name="maxLength">Maximum allowed prompt length in characters.</param>
    /// <param name="logger">Optional logger instance.</param>
    public PromptComposer(
        ITemplateEngine templateEngine,
        int maxLength = 128000,
        ILogger<PromptComposer>? logger = null)
    {
        _templateEngine = templateEngine ?? throw new ArgumentNullException(nameof(templateEngine));
        _merger = new ComponentMerger(deduplicateHeadings: true);
        _maxLength = maxLength;
        _logger = logger ?? NullLogger<PromptComposer>.Instance;
    }

    /// <inheritdoc />
    public Task<string> ComposeAsync(
        PromptPack pack,
        CompositionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(context);

        cancellationToken.ThrowIfCancellationRequested();

        // Merge components with context filtering and deduplication
        var merged = _merger.Merge(pack.Components, context);

        // Apply template variable substitution
        var result = _templateEngine.Substitute(merged, context);

        // Enforce maximum length
        if (result.Length > _maxLength)
        {
            _logger.LogWarning(
                "Composed prompt exceeds maximum length ({Length} > {MaxLength}), truncating",
                result.Length,
                _maxLength);
            result = result[.._maxLength];
        }

        // Log composition hash for debugging
        var hash = ComputeHash(result);
        _logger.LogInformation("Composed prompt hash: {Hash}, length: {Length}", hash, result.Length);

        return Task.FromResult(result);
    }

    private static string ComputeHash(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes)[..16];
    }
}
