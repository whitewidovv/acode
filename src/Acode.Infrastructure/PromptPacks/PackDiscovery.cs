using Acode.Domain.PromptPacks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Discovers available prompt packs from built-in and user directories.
/// </summary>
public sealed class PackDiscovery
{
    private const string ManifestFileName = "manifest.yml";

    private readonly ManifestParser _parser;
    private readonly EmbeddedPackProvider _embeddedProvider;
    private readonly PackDiscoveryOptions _options;
    private readonly ILogger<PackDiscovery> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackDiscovery"/> class.
    /// </summary>
    /// <param name="parser">The manifest parser.</param>
    /// <param name="embeddedProvider">The embedded pack provider for built-in packs.</param>
    /// <param name="options">The discovery options.</param>
    /// <param name="logger">The logger.</param>
    public PackDiscovery(
        ManifestParser parser,
        EmbeddedPackProvider embeddedProvider,
        IOptions<PackDiscoveryOptions> options,
        ILogger<PackDiscovery> logger)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _embeddedProvider = embeddedProvider ?? throw new ArgumentNullException(nameof(embeddedProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Discovers all available prompt packs.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of discovered pack manifests.</returns>
    public async Task<IReadOnlyList<PackManifest>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        var packs = new List<PackManifest>();
        var discoveredIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Discover user packs first (they take precedence over built-in)
        var userPacksPath = _options.GetResolvedUserPacksPath();
        if (Directory.Exists(userPacksPath))
        {
            var userPacks = await DiscoverPacksInDirectoryAsync(
                userPacksPath,
                PackSource.User,
                cancellationToken).ConfigureAwait(false);

            foreach (var pack in userPacks)
            {
                packs.Add(pack);
                discoveredIds.Add(pack.Id);
            }
        }

        // Discover built-in packs (only add if not overridden by user)
        var builtInPackIds = _embeddedProvider.GetAvailablePackIds();
        foreach (var packId in builtInPackIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (discoveredIds.Contains(packId))
            {
                _logger.LogDebug("Built-in pack {PackId} overridden by user pack", packId);
                continue;
            }

            try
            {
                var manifest = _embeddedProvider.LoadManifest(packId);
                packs.Add(manifest);
                discoveredIds.Add(packId);
                _logger.LogDebug("Discovered built-in pack: {PackId}", packId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load built-in pack manifest for {PackId}", packId);
            }
        }

        _logger.LogDebug("Discovered {PackCount} prompt packs", packs.Count);

        return packs.AsReadOnly();
    }

    /// <summary>
    /// Discovers a specific pack by ID.
    /// </summary>
    /// <param name="packId">The pack ID to find.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The pack manifest if found; otherwise, <c>null</c>.</returns>
    public async Task<PackManifest?> DiscoverByIdAsync(
        string packId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);

        var allPacks = await DiscoverAsync(cancellationToken).ConfigureAwait(false);
        return allPacks.FirstOrDefault(p =>
            string.Equals(p.Id, packId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<PackManifest>> DiscoverPacksInDirectoryAsync(
        string basePath,
        PackSource source,
        CancellationToken cancellationToken)
    {
        var packs = new List<PackManifest>();

        if (!Directory.Exists(basePath))
        {
            return packs;
        }

        foreach (var packDir in Directory.GetDirectories(basePath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var manifestPath = Path.Combine(packDir, ManifestFileName);
            if (!File.Exists(manifestPath))
            {
                _logger.LogDebug("No manifest found in {PackDir}", packDir);
                continue;
            }

            try
            {
                var manifest = _parser.ParseFile(manifestPath, source);
                packs.Add(manifest);
                _logger.LogDebug("Discovered pack: {PackId} at {PackPath}", manifest.Id, manifest.PackPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse manifest at {ManifestPath}", manifestPath);
            }
        }

        await Task.CompletedTask.ConfigureAwait(false); // Placeholder for potential async ops

        return packs;
    }
}
