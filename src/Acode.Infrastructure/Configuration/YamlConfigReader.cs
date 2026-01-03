using Acode.Application.Configuration;
using Acode.Domain.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Acode.Infrastructure.Configuration;

/// <summary>
/// Reads and deserializes YAML configuration files.
/// Implements <see cref="IConfigReader"/> from Application layer.
/// </summary>
public sealed class YamlConfigReader : IConfigReader
{
    private readonly IDeserializer _deserializer;

    public YamlConfigReader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .WithNodeDeserializer(new ReadOnlyCollectionNodeDeserializer(), s => s.Before<YamlDotNet.Serialization.NodeDeserializers.CollectionNodeDeserializer>())
            .Build();
    }

    /// <summary>
    /// Reads and parses a YAML configuration file.
    /// </summary>
    /// <param name="filePath">Path to the YAML file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized configuration.</returns>
    public async Task<AcodeConfig> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}", filePath);
        }

        var yaml = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);

        var config = _deserializer.Deserialize<AcodeConfig>(yaml);

        if (config == null)
        {
            throw new InvalidOperationException($"Failed to deserialize configuration from {filePath}");
        }

        return config;
    }

    /// <summary>
    /// Reads and parses YAML configuration from a string.
    /// </summary>
    /// <param name="yaml">The YAML content.</param>
    /// <returns>The deserialized configuration.</returns>
    public AcodeConfig Read(string yaml)
    {
        var config = _deserializer.Deserialize<AcodeConfig>(yaml);

        if (config == null)
        {
            throw new InvalidOperationException("Failed to deserialize configuration from YAML string");
        }

        return config;
    }
}
