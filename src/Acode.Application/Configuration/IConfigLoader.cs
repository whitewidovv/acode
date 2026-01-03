using Acode.Domain.Configuration;

namespace Acode.Application.Configuration;

/// <summary>
/// Service for loading and parsing configuration from .agent/config.yml.
/// </summary>
public interface IConfigLoader
{
    /// <summary>
    /// Loads configuration from the default location (.agent/config.yml in repository root).
    /// </summary>
    /// <param name="repositoryRoot">The repository root directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed and validated configuration.</returns>
    Task<AcodeConfig> LoadAsync(string repositoryRoot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads configuration from a specific file path.
    /// </summary>
    /// <param name="configFilePath">The path to the config file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed and validated configuration.</returns>
    Task<AcodeConfig> LoadFromPathAsync(string configFilePath, CancellationToken cancellationToken = default);
}
