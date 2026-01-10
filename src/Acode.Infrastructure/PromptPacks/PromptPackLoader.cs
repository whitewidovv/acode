using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using Acode.Domain.PromptPacks.Exceptions;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Loads prompt packs from filesystem and embedded resources.
/// </summary>
public sealed class PromptPackLoader : IPromptPackLoader
{
    private readonly ManifestParser _manifestParser;
    private readonly ContentHasher _contentHasher;
    private readonly EmbeddedPackProvider _embeddedPackProvider;
    private readonly ILogger<PromptPackLoader> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptPackLoader"/> class.
    /// </summary>
    /// <param name="manifestParser">The manifest parser.</param>
    /// <param name="contentHasher">The content hasher.</param>
    /// <param name="embeddedPackProvider">The embedded pack provider.</param>
    /// <param name="logger">The logger.</param>
    public PromptPackLoader(
        ManifestParser manifestParser,
        ContentHasher contentHasher,
        EmbeddedPackProvider embeddedPackProvider,
        ILogger<PromptPackLoader> logger)
    {
        _manifestParser = manifestParser;
        _contentHasher = contentHasher;
        _embeddedPackProvider = embeddedPackProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<PromptPack> LoadPackAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var manifestPath = Path.Combine(path, "manifest.yml");

        if (!File.Exists(manifestPath))
        {
            throw new PackLoadException(
                "ACODE-PKL-001",
                $"manifest.yml not found at {manifestPath}",
                path);
        }

        _logger.LogInformation("Loading pack from {Path}", path);

        try
        {
            var manifest = _manifestParser.ParseFile(manifestPath, PackSource.User);
            var components = await LoadComponentsAsync(path, manifest.Components, cancellationToken)
                .ConfigureAwait(false);

            var pack = new PromptPack(
                manifest.Id,
                manifest.Version,
                manifest.Name,
                manifest.Description,
                manifest.Source,
                manifest.PackPath,
                manifest.ContentHash,
                components);

            await VerifyHashAsync(pack, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Loaded pack {PackId} v{Version} with {ComponentCount} components",
                pack.Id,
                pack.Version,
                pack.Components.Count);

            return pack;
        }
        catch (ManifestParseException ex)
        {
            throw new PackLoadException(
                "ACODE-PKL-002",
                $"Failed to parse manifest: {ex.Message}",
                path,
                ex);
        }
    }

    /// <inheritdoc/>
    public Task<PromptPack> LoadBuiltInPackAsync(string packId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);

        return _embeddedPackProvider.LoadPackAsync(packId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<PromptPack> LoadUserPackAsync(string path, CancellationToken cancellationToken = default)
    {
        return LoadPackAsync(path, cancellationToken);
    }

    /// <inheritdoc/>
    public bool TryLoadPack(string path, out PromptPack? pack, out string? errorMessage)
    {
        try
        {
            pack = LoadPackAsync(path).GetAwaiter().GetResult();
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            pack = null;
            errorMessage = ex.Message;
            return false;
        }
    }

    private async Task<IReadOnlyList<LoadedComponent>> LoadComponentsAsync(
        string packPath,
        IReadOnlyList<PackComponent> componentDefs,
        CancellationToken cancellationToken)
    {
        var components = new List<LoadedComponent>();

        foreach (var def in componentDefs)
        {
            PathNormalizer.EnsurePathSafe(def.Path);

            var fullPath = Path.Combine(packPath, def.Path.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(fullPath))
            {
                throw new PackLoadException(
                    "ACODE-PKL-003",
                    $"Component file not found: {def.Path}",
                    packPath);
            }

            // Check for symlinks
            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                _logger.LogError("SYMLINK_REJECTED: {Path}", def.Path);
                throw new PackLoadException(
                    "ACODE-PKL-004",
                    $"Symlink rejected: {def.Path}",
                    packPath);
            }

            var content = await File.ReadAllTextAsync(fullPath, cancellationToken)
                .ConfigureAwait(false);

            var metadata = def.Metadata is null
                ? null
                : def.Metadata.ToDictionary(k => k.Key, v => v.Value);

            components.Add(new LoadedComponent(
                def.Path,
                def.Type,
                content,
                metadata?.AsReadOnly()));
        }

        return components.AsReadOnly();
    }

    private async Task VerifyHashAsync(PromptPack pack, CancellationToken cancellationToken)
    {
        if (pack.ContentHash is null)
        {
            return;
        }

        var componentData = pack.Components
            .Select(c => (c.Path, c.Content))
            .ToArray();

        var actualHash = _contentHasher.ComputeHash(componentData);

        if (!pack.ContentHash.Matches(actualHash))
        {
            _logger.LogWarning(
                "Content hash mismatch for pack {PackId}. Expected: {Expected}, Actual: {Actual}",
                pack.Id,
                pack.ContentHash,
                actualHash);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
