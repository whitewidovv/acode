using System.Text;
using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Loads prompt packs from disk or embedded resources.
/// </summary>
public sealed class PromptPackLoader : IPromptPackLoader
{
    private readonly IContentHasher _contentHasher;
    private readonly IDeserializer _yamlDeserializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptPackLoader"/> class.
    /// </summary>
    /// <param name="contentHasher">Content hasher for verifying pack integrity.</param>
    public PromptPackLoader(IContentHasher contentHasher)
    {
        ArgumentNullException.ThrowIfNull(contentHasher);

        _contentHasher = contentHasher;
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
    }

    /// <inheritdoc/>
    public PromptPack LoadPack(string packPath)
    {
        ArgumentNullException.ThrowIfNull(packPath);

        if (!Directory.Exists(packPath))
        {
            throw new PackLoadException("unknown", $"Pack directory not found: {packPath}");
        }

        // Load and parse manifest
        var manifestPath = Path.Combine(packPath, "manifest.yml");
        if (!File.Exists(manifestPath))
        {
            throw new PackLoadException("unknown", $"manifest.yml not found in pack directory: {packPath}");
        }

        PackManifest manifest;
        try
        {
            var manifestYaml = File.ReadAllText(manifestPath, Encoding.UTF8);
            var manifestDto = _yamlDeserializer.Deserialize<ManifestDto>(manifestYaml);
            manifest = ConvertToPackManifest(manifestDto, packPath);
        }
        catch (Exception ex) when (ex is not PackLoadException)
        {
            throw new PackLoadException("unknown", $"Failed to parse manifest.yml: {ex.Message}", ex);
        }

        // Load component files
        var componentContents = new Dictionary<string, string>();
        var loadedComponents = new List<PackComponent>();

        foreach (var component in manifest.Components)
        {
            // Validate and normalize path
            string normalizedPath;
            try
            {
                normalizedPath = PathNormalizer.NormalizeAndValidate(component.Path);
            }
            catch (PathTraversalException ex)
            {
                throw new PackLoadException(
                    manifest.Id,
                    $"Component path '{component.Path}' contains path traversal sequences",
                    ex);
            }

            // Construct absolute file path and validate it's within pack root
            var componentFilePath = Path.Combine(packPath, normalizedPath.Replace('/', Path.DirectorySeparatorChar));
            var fullComponentPath = Path.GetFullPath(componentFilePath);
            var fullPackPath = Path.GetFullPath(packPath);

            if (!fullComponentPath.StartsWith(fullPackPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new PackLoadException(
                    manifest.Id,
                    $"Component path '{component.Path}' resolves outside pack directory (path traversal attempt)");
            }

            // Check file exists
            if (!File.Exists(fullComponentPath))
            {
                throw new PackLoadException(
                    manifest.Id,
                    $"Component file not found: {component.Path}");
            }

            // Load content
            var content = File.ReadAllText(fullComponentPath, Encoding.UTF8);
            componentContents[normalizedPath] = content;

            // Create loaded component with normalized path and content
            loadedComponents.Add(component with { Path = normalizedPath, Content = content });
        }

        // Verify content hash (warning if mismatch, not error)
        var actualHash = _contentHasher.Compute(componentContents);
        if (!actualHash.Equals(manifest.ContentHash))
        {
            // In production, this would log a warning via ILogger
            // For now, we just continue loading (dev workflow support)
            Console.WriteLine($"WARNING: Content hash mismatch for pack '{manifest.Id}'. Expected: {manifest.ContentHash.Value}, Actual: {actualHash.Value}");
        }

        // Construct PromptPack
        return new PromptPack
        {
            Manifest = manifest with { Components = loadedComponents },
            Components = loadedComponents.ToDictionary(c => c.Path, c => c),
            Source = PackSource.User,
        };
    }

    /// <inheritdoc/>
    public PromptPack LoadBuiltInPack(string packId)
    {
        throw new NotImplementedException("Built-in pack loading from embedded resources not yet implemented");
    }

    private static ComponentType ParseComponentType(string? type)
    {
        return type?.ToLowerInvariant() switch
        {
            "system" => ComponentType.System,
            "role" => ComponentType.Role,
            "language" => ComponentType.Language,
            "framework" => ComponentType.Framework,
            "custom" => ComponentType.Custom,
            _ => ComponentType.Custom,
        };
    }

    private PackManifest ConvertToPackManifest(ManifestDto dto, string packPath)
    {
        if (string.IsNullOrWhiteSpace(dto.Id))
        {
            throw new PackLoadException("unknown", "Manifest 'id' field is required");
        }

        if (string.IsNullOrWhiteSpace(dto.Version))
        {
            throw new PackLoadException(dto.Id, "Manifest 'version' field is required");
        }

        PackVersion version;
        try
        {
            version = PackVersion.Parse(dto.Version);
        }
        catch (Exception ex)
        {
            throw new PackLoadException(dto.Id, $"Invalid version format '{dto.Version}': {ex.Message}", ex);
        }

        ContentHash contentHash;
        try
        {
            contentHash = new ContentHash(dto.ContentHash ?? string.Empty);
        }
        catch (Exception ex)
        {
            throw new PackLoadException(dto.Id, $"Invalid content_hash format: {ex.Message}", ex);
        }

        var components = new List<PackComponent>();
        if (dto.Components != null)
        {
            foreach (var componentDto in dto.Components)
            {
                components.Add(new PackComponent
                {
                    Path = componentDto.Path ?? string.Empty,
                    Type = ParseComponentType(componentDto.Type),
                    Role = componentDto.Role,
                    Language = componentDto.Language,
                    Framework = componentDto.Framework,
                });
            }
        }

        return new PackManifest
        {
            FormatVersion = dto.FormatVersion ?? "1.0",
            Id = dto.Id,
            Version = version,
            Name = dto.Name ?? dto.Id,
            Description = dto.Description ?? string.Empty,
            ContentHash = contentHash,
            CreatedAt = dto.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = dto.UpdatedAt,
            Author = dto.Author,
            Components = components,
        };
    }

    // DTOs for YAML deserialization
    private sealed class ManifestDto
    {
        public string? FormatVersion { get; set; }

        public string? Id { get; set; }

        public string? Version { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? ContentHash { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? Author { get; set; }

        public List<ComponentDto>? Components { get; set; }
    }

    private sealed class ComponentDto
    {
        public string? Path { get; set; }

        public string? Type { get; set; }

        public string? Role { get; set; }

        public string? Language { get; set; }

        public string? Framework { get; set; }
    }
}
