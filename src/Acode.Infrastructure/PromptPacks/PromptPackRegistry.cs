using System.Collections.Concurrent;
using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using Acode.Domain.PromptPacks.Exceptions;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Registry that indexes and provides access to prompt packs.
/// </summary>
public sealed class PromptPackRegistry : IPromptPackRegistry
{
    private readonly PackDiscovery _discovery;
    private readonly PromptPackLoader _loader;
    private readonly PackValidator _validator;
    private readonly PackCache _cache;
    private readonly PackConfiguration _configuration;
    private readonly ILogger<PromptPackRegistry> _logger;

    private readonly ConcurrentDictionary<string, PackManifest> _index = new();
    private readonly object _refreshLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptPackRegistry"/> class.
    /// </summary>
    /// <param name="discovery">The pack discovery service.</param>
    /// <param name="loader">The pack loader.</param>
    /// <param name="validator">The pack validator.</param>
    /// <param name="cache">The pack cache.</param>
    /// <param name="configuration">The pack configuration.</param>
    /// <param name="logger">The logger.</param>
    public PromptPackRegistry(
        PackDiscovery discovery,
        PromptPackLoader loader,
        PackValidator validator,
        PackCache cache,
        PackConfiguration configuration,
        ILogger<PromptPackRegistry> logger)
    {
        _discovery = discovery;
        _loader = loader;
        _validator = validator;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the registry by discovering available packs.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the initialization.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing pack registry");

        var packs = await _discovery.DiscoverAsync(cancellationToken).ConfigureAwait(false);

        foreach (var pack in packs)
        {
            // User packs override built-in packs with same ID
            _index[pack.Id] = pack;
            _logger.LogDebug("Indexed pack {PackId} v{Version} from {Source}", pack.Id, pack.Version, pack.Source);
        }

        _logger.LogInformation("Pack registry initialized with {Count} packs", _index.Count);
    }

    /// <inheritdoc/>
    public PromptPack GetPack(string packId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);

        var pack = TryGetPack(packId);
        if (pack is null)
        {
            throw new PackNotFoundException(packId);
        }

        return pack;
    }

    /// <inheritdoc/>
    public PromptPack? TryGetPack(string packId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);

        // Check cache first
        var cached = _cache.GetByPackId(packId);
        if (cached is not null)
        {
            return cached;
        }

        // Check if pack is in index
        if (!_index.TryGetValue(packId, out var manifest))
        {
            return null;
        }

        // Load the pack
        try
        {
            var pack = _loader.LoadPackAsync(manifest.PackPath).GetAwaiter().GetResult();

            // Validate
            var validationResult = _validator.Validate(pack);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Pack {PackId} has {ErrorCount} validation errors",
                    packId,
                    validationResult.Errors.Count);

                foreach (var error in validationResult.Errors)
                {
                    _logger.LogWarning("  {Code}: {Message}", error.Code, error.Message);
                }
            }

            // Cache it
            _cache.Set(pack);

            return pack;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load pack {PackId}", packId);
            return null;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<PromptPackInfo> ListPacks()
    {
        var activePackId = GetActivePackId();

        return _index.Values
            .OrderBy(p => p.Id)
            .Select(p => new PromptPackInfo(
                p.Id,
                p.Version,
                p.Name,
                p.Source,
                IsActive: string.Equals(p.Id, activePackId, StringComparison.OrdinalIgnoreCase),
                p.PackPath))
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc/>
    public PromptPack GetActivePack()
    {
        var packId = GetActivePackId();
        var pack = TryGetPack(packId);

        if (pack is null)
        {
            _logger.LogWarning(
                "Active pack '{PackId}' not found, falling back to default '{DefaultPack}'",
                packId,
                PackConfiguration.DefaultPack);

            pack = TryGetPack(PackConfiguration.DefaultPack);
        }

        if (pack is null)
        {
            throw new PackNotFoundException(packId);
        }

        _logger.LogInformation("Active pack: {PackId} v{Version}", pack.Id, pack.Version);
        return pack;
    }

    /// <inheritdoc/>
    public string GetActivePackId()
    {
        return _configuration.GetActivePackId();
    }

    /// <inheritdoc/>
    public void Refresh()
    {
        lock (_refreshLock)
        {
            _logger.LogInformation("Refreshing pack registry");

            _cache.Clear();
            _index.Clear();
            _configuration.ClearCache();

            InitializeAsync().GetAwaiter().GetResult();
        }
    }
}
