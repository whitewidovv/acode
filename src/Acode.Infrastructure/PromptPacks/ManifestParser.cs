using Acode.Domain.PromptPacks;
using Acode.Domain.PromptPacks.Exceptions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Parses manifest.yml files into <see cref="PackManifest"/> domain objects.
/// </summary>
public sealed class ManifestParser
{
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestParser"/> class.
    /// </summary>
    public ManifestParser()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Parses YAML content into a pack manifest using default values for pack path and source.
    /// </summary>
    /// <param name="yamlContent">The YAML content to parse.</param>
    /// <returns>The parsed manifest.</returns>
    /// <exception cref="ManifestParseException">Thrown when parsing fails.</exception>
    public PackManifest Parse(string yamlContent)
    {
        return Parse(yamlContent, string.Empty, PackSource.User);
    }

    /// <summary>
    /// Parses YAML content into a pack manifest.
    /// </summary>
    /// <param name="yamlContent">The YAML content to parse.</param>
    /// <param name="packPath">The path to the pack directory.</param>
    /// <param name="source">The pack source.</param>
    /// <returns>The parsed manifest.</returns>
    /// <exception cref="ManifestParseException">Thrown when parsing fails.</exception>
    public PackManifest Parse(string yamlContent, string packPath, PackSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yamlContent);
        ArgumentNullException.ThrowIfNull(packPath);

        try
        {
            var dto = _deserializer.Deserialize<ManifestDto>(yamlContent);
            return MapToManifest(dto, packPath, source);
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new ManifestParseException(
                "ACODE-PKL-001",
                $"Failed to parse manifest YAML: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Parses a manifest file into a pack manifest.
    /// </summary>
    /// <param name="manifestPath">The path to the manifest file.</param>
    /// <param name="source">The pack source.</param>
    /// <returns>The parsed manifest.</returns>
    /// <exception cref="ManifestParseException">Thrown when parsing fails.</exception>
    public PackManifest ParseFile(string manifestPath, PackSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

        if (!File.Exists(manifestPath))
        {
            throw new ManifestParseException(
                "ACODE-PKL-006",
                $"Manifest file not found: {manifestPath}");
        }

        var yamlContent = File.ReadAllText(manifestPath);
        var packPath = Path.GetDirectoryName(manifestPath)
            ?? throw new ManifestParseException(
                "ACODE-PKL-002",
                $"Could not determine pack directory from: {manifestPath}");

        return Parse(yamlContent, packPath, source);
    }

    private static PackManifest MapToManifest(ManifestDto dto, string packPath, PackSource source)
    {
        if (string.IsNullOrWhiteSpace(dto.FormatVersion))
        {
            throw new ManifestParseException("ACODE-PKL-002", "format_version is required");
        }

        if (dto.FormatVersion != "1.0")
        {
            throw new ManifestParseException(
                "ACODE-PKL-003",
                $"Unsupported format_version '{dto.FormatVersion}'. Only version 1.0 is supported.");
        }

        if (string.IsNullOrWhiteSpace(dto.Id))
        {
            throw new ManifestParseException("ACODE-PKL-002", "id is required");
        }

        if (!PackManifest.IsValidPackId(dto.Id))
        {
            throw new ManifestParseException(
                "ACODE-PKL-004",
                $"Pack ID '{dto.Id}' is not valid. Must be kebab-case, 1-64 characters.");
        }

        if (string.IsNullOrWhiteSpace(dto.Version))
        {
            throw new ManifestParseException("ACODE-PKL-002", "version is required");
        }

        if (!PackVersion.TryParse(dto.Version, out var version) || version is null)
        {
            throw new ManifestParseException(
                "ACODE-PKL-005",
                $"Version '{dto.Version}' is not a valid semantic version.");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ManifestParseException("ACODE-PKL-002", "name is required");
        }

        ContentHash? contentHash = null;
        if (!string.IsNullOrWhiteSpace(dto.ContentHash))
        {
            try
            {
                contentHash = new ContentHash(dto.ContentHash);
            }
            catch (ArgumentException ex)
            {
                throw new ManifestParseException(
                    "ACODE-PKL-002",
                    $"Invalid content_hash format: {ex.Message}",
                    ex);
            }
        }

        if (string.IsNullOrWhiteSpace(dto.CreatedAt))
        {
            throw new ManifestParseException("ACODE-PKL-002", "created_at is required");
        }

        if (!DateTimeOffset.TryParse(dto.CreatedAt, out var createdAt))
        {
            throw new ManifestParseException(
                "ACODE-PKL-002",
                $"Invalid created_at format: {dto.CreatedAt}");
        }

        var components = new List<PackComponent>();
        if (dto.Components is not null)
        {
            foreach (var componentDto in dto.Components)
            {
                var component = MapToComponent(componentDto);
                components.Add(component);
            }
        }

        return new PackManifest(
            dto.FormatVersion,
            dto.Id,
            version,
            dto.Name,
            dto.Description,
            contentHash,
            createdAt,
            components.AsReadOnly(),
            source,
            packPath);
    }

    private static PackComponent MapToComponent(ComponentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Path))
        {
            throw new ManifestParseException("ACODE-PKL-002", "Component path is required");
        }

        PathNormalizer.EnsurePathSafe(dto.Path);

        if (!Enum.TryParse<ComponentType>(dto.Type, ignoreCase: true, out var componentType))
        {
            componentType = ComponentType.Custom;
        }

        return new PackComponent(
            dto.Path,
            componentType,
            dto.Metadata?.AsReadOnly(),
            dto.Description);
    }

#pragma warning disable SA1600 // DTOs for YAML deserialization
#pragma warning disable SA1401

    private sealed class ManifestDto
    {
        public string? FormatVersion { get; set; }

        public string? Id { get; set; }

        public string? Version { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? ContentHash { get; set; }

        public string? CreatedAt { get; set; }

        public List<ComponentDto>? Components { get; set; }
    }

    private sealed class ComponentDto
    {
        public string? Path { get; set; }

        public string? Type { get; set; }

        public string? Description { get; set; }

        public Dictionary<string, string>? Metadata { get; set; }
    }

#pragma warning restore SA1401
#pragma warning restore SA1600
}
