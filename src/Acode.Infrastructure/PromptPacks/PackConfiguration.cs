using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Configuration for prompt pack selection.
/// </summary>
public sealed class PackConfiguration
{
    private const string DefaultPackId = "acode-standard";
    private const string EnvVarName = "ACODE_PROMPT_PACK";

    private readonly ILogger<PackConfiguration> _logger;
    private string? _cachedPackId;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackConfiguration"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public PackConfiguration(ILogger<PackConfiguration> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the default pack ID.
    /// </summary>
    public static string DefaultPack => DefaultPackId;

    /// <summary>
    /// Gets the active pack ID based on configuration precedence:
    /// 1. Environment variable ACODE_PROMPT_PACK
    /// 2. Config file prompts.pack_id (not yet implemented)
    /// 3. Default: acode-standard.
    /// </summary>
    /// <returns>The active pack ID.</returns>
    public string GetActivePackId()
    {
        if (_cachedPackId is not null)
        {
            return _cachedPackId;
        }

        // Check environment variable first
        var envValue = Environment.GetEnvironmentVariable(EnvVarName);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            _logger.LogDebug("Using pack ID from environment variable: {PackId}", envValue);
            _cachedPackId = envValue;
            return _cachedPackId;
        }

        // TODO: Read from .agent/config.yml when config system is available
        // For now, use default
        _logger.LogDebug("Using default pack ID: {PackId}", DefaultPackId);
        _cachedPackId = DefaultPackId;
        return _cachedPackId;
    }

    /// <summary>
    /// Clears the cached pack ID, forcing re-read on next call.
    /// </summary>
    public void ClearCache()
    {
        _cachedPackId = null;
    }
}
