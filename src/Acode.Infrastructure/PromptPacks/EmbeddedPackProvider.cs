// <copyright file="EmbeddedPackProvider.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using System.Reflection;
using Acode.Domain.PromptPacks;
using Acode.Domain.PromptPacks.Exceptions;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Provides access to built-in prompt packs embedded as assembly resources.
/// </summary>
public sealed class EmbeddedPackProvider
{
    private const string ResourcePrefix = "Acode.Infrastructure.Resources.PromptPacks.";

    private readonly ManifestParser _manifestParser;
    private readonly ILogger<EmbeddedPackProvider> _logger;
    private readonly Assembly _assembly;
    private readonly Dictionary<string, string> _extractionCache = new();
    private readonly object _cacheLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddedPackProvider"/> class.
    /// </summary>
    /// <param name="manifestParser">The manifest parser.</param>
    /// <param name="logger">The logger.</param>
    public EmbeddedPackProvider(ManifestParser manifestParser, ILogger<EmbeddedPackProvider> logger)
    {
        _manifestParser = manifestParser;
        _logger = logger;
        _assembly = typeof(EmbeddedPackProvider).Assembly;
    }

    /// <summary>
    /// Gets the list of available built-in pack IDs.
    /// </summary>
    /// <returns>The available pack IDs.</returns>
    public IReadOnlyList<string> GetAvailablePackIds()
    {
        var packIds = new HashSet<string>();
        var resourceNames = _assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal));

        foreach (var name in resourceNames)
        {
            var remainder = name[ResourcePrefix.Length..];
            var firstDot = remainder.IndexOf('.', StringComparison.Ordinal);
            if (firstDot > 0)
            {
                var resourcePackId = remainder[..firstDot];

                // Convert underscores back to hyphens for canonical pack ID format
                packIds.Add(DenormalizeFromResource(resourcePackId));
            }
        }

        return packIds.ToList().AsReadOnly();
    }

    /// <summary>
    /// Checks if a built-in pack exists.
    /// </summary>
    /// <param name="packId">The pack ID.</param>
    /// <returns><c>true</c> if the pack exists; otherwise, <c>false</c>.</returns>
    public bool HasPack(string packId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);

        var normalizedPackId = NormalizeForResource(packId);
        var prefix = ResourcePrefix + normalizedPackId + ".";
        return _assembly.GetManifestResourceNames()
            .Any(n => n.StartsWith(prefix, StringComparison.Ordinal));
    }

    /// <summary>
    /// Loads a built-in pack manifest.
    /// </summary>
    /// <param name="packId">The pack ID.</param>
    /// <returns>The loaded pack manifest.</returns>
    /// <exception cref="PackNotFoundException">Thrown when the pack is not found.</exception>
    public PackManifest LoadManifest(string packId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);

        var manifestResourceName = GetResourceName(packId, "manifest.yml");
        using var stream = _assembly.GetManifestResourceStream(manifestResourceName);

        if (stream is null)
        {
            throw new PackNotFoundException(packId);
        }

        using var reader = new StreamReader(stream);
        var manifestContent = reader.ReadToEnd();

        return _manifestParser.Parse(manifestContent, GetExtractedPath(packId), PackSource.BuiltIn);
    }

    /// <summary>
    /// Loads a complete built-in pack with all components.
    /// </summary>
    /// <param name="packId">The pack ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded prompt pack.</returns>
    /// <exception cref="PackNotFoundException">Thrown when the pack is not found.</exception>
    public async Task<PromptPack> LoadPackAsync(string packId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);

        if (!HasPack(packId))
        {
            throw new PackNotFoundException(packId);
        }

        _logger.LogInformation("Loading built-in pack {PackId}", packId);

        // Extract pack to temp directory
        var extractPath = ExtractPack(packId);
        var manifest = LoadManifest(packId);

        // Load component contents
        var components = new List<LoadedComponent>();

        foreach (var componentDef in manifest.Components)
        {
            var content = ReadResourceContent(packId, componentDef.Path);

            if (content is null)
            {
                throw new PackLoadException(
                    "ACODE-PKL-003",
                    $"Component file not found in embedded resources: {componentDef.Path}",
                    packId);
            }

            var metadata = componentDef.Metadata is null
                ? null
                : componentDef.Metadata.ToDictionary(k => k.Key, v => v.Value);

            components.Add(new LoadedComponent(
                componentDef.Path,
                componentDef.Type,
                content,
                metadata?.AsReadOnly()));
        }

        _logger.LogInformation(
            "Loaded built-in pack {PackId} v{Version} with {ComponentCount} components",
            packId,
            manifest.Version,
            components.Count);

        await Task.CompletedTask.ConfigureAwait(false);

        return new PromptPack(
            manifest.Id,
            manifest.Version,
            manifest.Name,
            manifest.Description,
            PackSource.BuiltIn,
            extractPath,
            manifest.ContentHash,
            components.AsReadOnly());
    }

    /// <summary>
    /// Extracts a built-in pack to the temporary directory.
    /// </summary>
    /// <param name="packId">The pack ID.</param>
    /// <returns>The path to the extracted pack.</returns>
    public string ExtractPack(string packId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);

        lock (_cacheLock)
        {
            if (_extractionCache.TryGetValue(packId, out var cachedPath) && Directory.Exists(cachedPath))
            {
                // Verify cached path has manifest (not empty directory from failed extraction)
                if (File.Exists(Path.Combine(cachedPath, "manifest.yml")))
                {
                    return cachedPath;
                }
            }

            var extractPath = GetExtractedPath(packId);

            // Check if already extracted and valid
            if (Directory.Exists(extractPath) && File.Exists(Path.Combine(extractPath, "manifest.yml")))
            {
                _extractionCache[packId] = extractPath;
                return extractPath;
            }

            // Create or clear the directory
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            Directory.CreateDirectory(extractPath);

            var normalizedPackId = NormalizeForResource(packId);
            var prefix = ResourcePrefix + normalizedPackId + ".";
            var resourceNames = _assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith(prefix, StringComparison.Ordinal));

            foreach (var resourceName in resourceNames)
            {
                var relativePath = resourceName[prefix.Length..];

                // Convert resource name dots back to path separators
                // (except the last dot which is the file extension)
                var lastDotIndex = relativePath.LastIndexOf('.');
                if (lastDotIndex > 0)
                {
                    var pathPart = relativePath[..lastDotIndex].Replace('.', Path.DirectorySeparatorChar);
                    var extension = relativePath[lastDotIndex..];
                    relativePath = pathPart + extension;
                }

                var targetPath = Path.Combine(extractPath, relativePath);
                var targetDir = Path.GetDirectoryName(targetPath);

                if (targetDir is not null && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                using var stream = _assembly.GetManifestResourceStream(resourceName);
                if (stream is not null)
                {
                    using var fileStream = File.Create(targetPath);
                    stream.CopyTo(fileStream);
                }
            }

            _extractionCache[packId] = extractPath;
            _logger.LogDebug("Extracted pack {PackId} to {Path}", packId, extractPath);
            return extractPath;
        }
    }

    private static string GetExtractedPath(string packId)
    {
        return Path.Combine(Path.GetTempPath(), "acode", "packs", packId);
    }

    private static string GetResourceName(string packId, string componentPath)
    {
        // Convert path separators to dots for resource name
        // Also normalize pack ID for resource lookup (hyphens -> underscores)
        var normalizedPackId = NormalizeForResource(packId);
        var resourcePath = componentPath.Replace('/', '.').Replace('\\', '.');
        return ResourcePrefix + normalizedPackId + "." + resourcePath;
    }

    /// <summary>
    /// Normalizes a pack ID for resource lookup (converts hyphens to underscores).
    /// .NET's embedded resource naming converts folder hyphens to underscores.
    /// </summary>
    private static string NormalizeForResource(string packId) => packId.Replace('-', '_');

    /// <summary>
    /// Denormalizes a resource pack ID back to the canonical format (converts underscores to hyphens).
    /// </summary>
    private static string DenormalizeFromResource(string resourcePackId) => resourcePackId.Replace('_', '-');

    private string? ReadResourceContent(string packId, string componentPath)
    {
        var resourceName = GetResourceName(packId, componentPath);
        using var stream = _assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
