namespace Acode.Infrastructure.Heuristics;

using Acode.Application.Routing;
using Acode.Domain.Modes;
using Microsoft.Extensions.Logging;

/// <summary>
/// Resolves model overrides in precedence order: request > session > config.
/// Validates overrides against operating mode constraints.
/// </summary>
/// <remarks>
/// <para>AC-026: Request override highest.</para>
/// <para>AC-027: Session overrides config.</para>
/// <para>AC-028: Config overrides heuristics.</para>
/// <para>AC-029: Heuristics lowest.</para>
/// </remarks>
public sealed class OverrideResolver
{
    private readonly OperatingMode _currentMode;
    private readonly ILogger<OverrideResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OverrideResolver"/> class.
    /// </summary>
    /// <param name="currentMode">The current operating mode.</param>
    /// <param name="logger">The logger.</param>
    public OverrideResolver(OperatingMode currentMode, ILogger<OverrideResolver> logger)
    {
        _currentMode = currentMode;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Resolves override from context, checking in precedence order.
    /// Returns null if no overrides are present.
    /// </summary>
    /// <param name="context">The override context to resolve.</param>
    /// <returns>The resolved override, or null if no overrides present.</returns>
    public OverrideResult? Resolve(OverrideContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // AC-026: Check request override (highest precedence)
        if (!string.IsNullOrEmpty(context.RequestOverride))
        {
            return ValidateAndReturn(context.RequestOverride, OverrideSource.Request);
        }

        // AC-027: Check session override
        if (!string.IsNullOrEmpty(context.SessionOverride))
        {
            return ValidateAndReturn(context.SessionOverride, OverrideSource.Session);
        }

        // AC-028: Check config override (lowest override precedence)
        if (!string.IsNullOrEmpty(context.ConfigOverride))
        {
            return ValidateAndReturn(context.ConfigOverride, OverrideSource.Config);
        }

        // AC-029: No overrides present - fall through to heuristics
        return null;
    }

    /// <summary>
    /// Gets the current override precedence chain for introspection.
    /// </summary>
    /// <param name="context">The override context to inspect.</param>
    /// <returns>Description of the override chain.</returns>
    public string GetOverrideChainDescription(OverrideContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(context.RequestOverride))
        {
            parts.Add($"Request: {context.RequestOverride} [ACTIVE]");
        }
        else
        {
            parts.Add("Request: (none)");
        }

        if (!string.IsNullOrEmpty(context.SessionOverride))
        {
            var active = string.IsNullOrEmpty(context.RequestOverride) ? " [ACTIVE]" : string.Empty;
            parts.Add($"Session: {context.SessionOverride}{active}");
        }
        else
        {
            parts.Add("Session: (none)");
        }

        if (!string.IsNullOrEmpty(context.ConfigOverride))
        {
            var active =
                string.IsNullOrEmpty(context.RequestOverride)
                && string.IsNullOrEmpty(context.SessionOverride)
                    ? " [ACTIVE]"
                    : string.Empty;
            parts.Add($"Config: {context.ConfigOverride}{active}");
        }
        else
        {
            parts.Add("Config: (none)");
        }

        return string.Join(Environment.NewLine, parts);
    }

    private static bool IsValidModelId(string modelId)
    {
        // Valid format: name:tag (e.g., llama3.2:7b, gpt-4:latest)
        return !string.IsNullOrWhiteSpace(modelId)
            && modelId.Contains(':', StringComparison.Ordinal);
    }

    private OverrideResult? ValidateAndReturn(string modelId, OverrideSource source)
    {
        // AC-032, AC-067: Validate model ID format
        if (!IsValidModelId(modelId))
        {
            _logger.LogWarning(
                "Override validation failed: invalid model ID format '{ModelId}'. Source: {Source}",
                modelId,
                source
            );

            throw new RoutingException(
                "ACODE-HEU-001",
                $"Invalid model ID format: '{modelId}'. Expected format: 'name:tag'.",
                new[] { modelId }
            )
            {
                Suggestion = "Use format like 'llama3.2:7b' or 'gpt-4:latest'",
            };
        }

        // AC-066: Validate mode compatibility
        if (!IsCompatibleWithMode(modelId))
        {
            _logger.LogWarning(
                "Override validation failed: model {ModelId} incompatible with mode {Mode}. Source: {Source}",
                modelId,
                _currentMode,
                source
            );

            throw new RoutingException(
                "ACODE-HEU-002",
                $"Model '{modelId}' is not compatible with {_currentMode} operating mode.",
                new[] { modelId }
            )
            {
                Suggestion = "Switch to a different operating mode or select a compatible model.",
            };
        }

        // AC-034, AC-040: Logged
        _logger.LogInformation(
            "Override applied: model={ModelId}, source={Source}, mode={Mode}",
            modelId,
            source,
            _currentMode
        );

        return new OverrideResult { ModelId = modelId, Source = source };
    }

    private bool IsCompatibleWithMode(string modelId)
    {
        // In LocalOnly mode, reject known cloud model prefixes
        if (_currentMode == OperatingMode.LocalOnly)
        {
            var cloudPrefixes = new[] { "gpt-", "claude-", "gemini-", "o1-" };
            return !cloudPrefixes.Any(prefix =>
                modelId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            );
        }

        // Other modes allow all models
        return true;
    }
}
