using Acode.Application.Configuration;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Configuration for prompt pack selection.
/// Supports precedence: env var > config file > default.
/// </summary>
public sealed class PackConfiguration
{
    private const string DefaultPackId = "acode-standard";
    private const string EnvVarName = "ACODE_PROMPT_PACK";

    private readonly IConfigLoader? _configLoader;
    private readonly ILogger<PackConfiguration> _logger;
    private readonly string? _repositoryRoot;
    private string? _cachedPackId;
    private bool _configLoaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackConfiguration"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public PackConfiguration(ILogger<PackConfiguration> logger)
        : this(configLoader: null, logger, repositoryRoot: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackConfiguration"/> class.
    /// </summary>
    /// <param name="configLoader">The configuration loader.</param>
    /// <param name="logger">The logger.</param>
    public PackConfiguration(IConfigLoader? configLoader, ILogger<PackConfiguration> logger)
        : this(configLoader, logger, repositoryRoot: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackConfiguration"/> class.
    /// </summary>
    /// <param name="configLoader">The configuration loader.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="repositoryRoot">The repository root path for finding .agent/config.yml.</param>
    public PackConfiguration(IConfigLoader? configLoader, ILogger<PackConfiguration> logger, string? repositoryRoot)
    {
        _configLoader = configLoader;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repositoryRoot = repositoryRoot;
    }

    /// <summary>
    /// Gets the default pack ID.
    /// </summary>
    public static string DefaultPack => DefaultPackId;

    /// <summary>
    /// Gets the active pack ID based on configuration precedence:
    /// 1. Environment variable ACODE_PROMPT_PACK
    /// 2. Config file prompts.pack_id
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

        // For synchronous calls without async, use default
        _logger.LogDebug("Using default pack ID: {PackId}", DefaultPackId);
        _cachedPackId = DefaultPackId;
        return _cachedPackId;
    }

    /// <summary>
    /// Gets the active pack ID asynchronously, reading from config file if available.
    /// Precedence: env var > config file > default.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The active pack ID.</returns>
    public async Task<string> GetActivePackIdAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedPackId is not null)
        {
            return _cachedPackId;
        }

        // Check environment variable first (highest precedence)
        var envValue = Environment.GetEnvironmentVariable(EnvVarName);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            _logger.LogDebug("Using pack ID from environment variable: {PackId}", envValue);
            _cachedPackId = envValue;
            return _cachedPackId;
        }

        // Try to read from config file (middle precedence)
        if (_configLoader is not null && !string.IsNullOrWhiteSpace(_repositoryRoot))
        {
            var configPackId = await TryGetPackIdFromConfigAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(configPackId))
            {
                _logger.LogDebug("Using pack ID from config file: {PackId}", configPackId);
                _cachedPackId = configPackId;
                return _cachedPackId;
            }
        }

        // Fall back to default (lowest precedence)
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
        _configLoaded = false;
    }

    private async Task<string?> TryGetPackIdFromConfigAsync(CancellationToken cancellationToken)
    {
        if (_configLoaded)
        {
            return null;
        }

        _configLoaded = true;

        try
        {
            var config = await _configLoader!.LoadAsync(_repositoryRoot!, cancellationToken).ConfigureAwait(false);
            return config.Prompts?.PackId;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogDebug(ex, "Config file not found, using default pack");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Config file validation failed, using default pack");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read config file, using default pack");
            return null;
        }
    }
}
