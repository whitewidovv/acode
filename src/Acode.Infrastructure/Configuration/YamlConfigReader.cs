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

        // FR-002b-13: Enforce maximum file size of 1MB
        var fileInfo = new FileInfo(filePath);
        const long maxSize = 1024 * 1024; // 1MB
        if (fileInfo.Length > maxSize)
        {
            throw new InvalidOperationException(
                $"Configuration file exceeds maximum size of 1MB (actual: {fileInfo.Length} bytes)");
        }

        var yaml = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);

        // FR-002b-21: Reject YAML with multiple documents
        if (yaml.Contains("\n---", StringComparison.Ordinal) || yaml.Contains("\r\n---", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Configuration file contains multiple YAML documents (--- separator)");
        }

        // FR-002b-14: Enforce maximum nesting depth of 20
        ValidateNestingDepth(yaml, maxDepth: 20);

        // FR-002b-15: Enforce maximum key count of 1000
        ValidateKeyCount(yaml, maxKeys: 1000);

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

    private static void ValidateNestingDepth(string yaml, int maxDepth)
    {
        var lines = yaml.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            var leadingSpaces = line.Length - line.TrimStart().Length;
            var depth = leadingSpaces / 2; // YAML uses 2-space indentation

            if (depth > maxDepth)
            {
                throw new InvalidOperationException(
                    $"YAML nesting depth exceeds maximum of {maxDepth} levels (found: {depth})");
            }
        }
    }

    private static void ValidateKeyCount(string yaml, int maxKeys)
    {
        var keyCount = 0;
        var lines = yaml.Split('\n');

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            // Count lines that look like key-value pairs
            if (line.Contains(':', StringComparison.Ordinal) && !line.TrimStart().StartsWith('-'))
            {
                keyCount++;
            }
        }

        if (keyCount > maxKeys)
        {
            throw new InvalidOperationException(
                $"YAML contains too many keys (found: {keyCount}, maximum: {maxKeys})");
        }
    }
}
