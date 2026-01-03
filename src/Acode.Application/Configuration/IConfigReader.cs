using Acode.Domain.Configuration;

namespace Acode.Application.Configuration;

/// <summary>
/// Interface for reading configuration files.
/// Implemented by Infrastructure layer (e.g., YamlConfigReader).
/// </summary>
public interface IConfigReader
{
    /// <summary>
    /// Reads and deserializes a configuration file.
    /// </summary>
    /// <param name="filePath">Path to the configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized configuration.</returns>
    Task<AcodeConfig> ReadAsync(string filePath, CancellationToken cancellationToken = default);
}
