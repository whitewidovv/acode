using System.Collections.Concurrent;
using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Registry for discovering, indexing, and retrieving prompt packs.
/// </summary>
public sealed class PromptPackRegistry : IPromptPackRegistry
{
    private const string DefaultPackId = "acode-standard";
    private const string PacksDirectoryName = ".acode/prompts";
    private const string EnvVarPromptPack = "ACODE_PROMPT_PACK";

    private readonly IPromptPackLoader _loader;
    private readonly string _workspaceRoot;
    private readonly ConcurrentDictionary<string, PromptPack> _packs = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptPackRegistry"/> class.
    /// </summary>
    /// <param name="loader">Pack loader for loading packs from disk.</param>
    /// <param name="workspaceRoot">Workspace root directory.</param>
    public PromptPackRegistry(IPromptPackLoader loader, string workspaceRoot)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);

        _loader = loader;
        _workspaceRoot = workspaceRoot;
    }

    /// <inheritdoc/>
    public void Initialize()
    {
        DiscoverAndLoadPacks();
    }

    /// <inheritdoc/>
    public IReadOnlyList<PromptPackInfo> ListPacks()
    {
        return _packs.Values
            .Select(pack => new PromptPackInfo(
                pack.Manifest.Id,
                pack.Manifest.Version,
                pack.Manifest.Name,
                pack.Manifest.Description,
                pack.Source,
                pack.Manifest.Author))
            .OrderBy(p => p.Id)
            .ToList();
    }

    /// <inheritdoc/>
    public PromptPack GetPack(string packId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);

        if (_packs.TryGetValue(packId, out var pack))
        {
            return pack;
        }

        throw new PackNotFoundException(packId);
    }

    /// <inheritdoc/>
    public PromptPack GetActivePack()
    {
        // Configuration precedence:
        // 1. Environment variable ACODE_PROMPT_PACK
        // 2. Default "acode-standard"
        var packId = Environment.GetEnvironmentVariable(EnvVarPromptPack);

        if (!string.IsNullOrWhiteSpace(packId))
        {
            if (_packs.TryGetValue(packId, out var envPack))
            {
                return envPack;
            }

            // Environment variable set but pack not found - log warning and fall back to default
            Console.WriteLine($"WARNING: Pack '{packId}' specified in {EnvVarPromptPack} not found. Falling back to '{DefaultPackId}'.");
        }

        // Fall back to default pack
        if (_packs.TryGetValue(DefaultPackId, out var defaultPack))
        {
            return defaultPack;
        }

        throw new PackNotFoundException(DefaultPackId);
    }

    /// <inheritdoc/>
    public void Refresh()
    {
        _packs.Clear();
        DiscoverAndLoadPacks();
    }

    private void DiscoverAndLoadPacks()
    {
        var packsDirectory = Path.Combine(_workspaceRoot, PacksDirectoryName);

        if (!Directory.Exists(packsDirectory))
        {
            // No packs directory - registry will be empty
            return;
        }

        // Discover pack directories (each subdirectory is a potential pack)
        var packDirs = Directory.GetDirectories(packsDirectory);

        foreach (var packDir in packDirs)
        {
            try
            {
                var pack = _loader.LoadPack(packDir);

                // User packs override built-in packs with same ID
                _packs[pack.Manifest.Id] = pack;
            }
            catch (Exception ex)
            {
                // Log warning but continue loading other packs
                Console.WriteLine($"WARNING: Failed to load pack from '{packDir}': {ex.Message}");
            }
        }
    }
}
