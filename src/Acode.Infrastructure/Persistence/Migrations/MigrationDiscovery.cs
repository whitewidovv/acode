// src/Acode.Infrastructure/Persistence/Migrations/MigrationDiscovery.cs
namespace Acode.Infrastructure.Persistence.Migrations;

using System.Security.Cryptography;
using System.Text;
using Acode.Application.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Discovers migration files from embedded resources and file system.
/// </summary>
public sealed class MigrationDiscovery : IMigrationDiscovery
{
    private readonly IFileSystem _fileSystem;
    private readonly IEmbeddedResourceProvider _embeddedProvider;
    private readonly ILogger<MigrationDiscovery> _logger;
    private readonly MigrationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationDiscovery"/> class.
    /// </summary>
    /// <param name="fileSystem">File system abstraction.</param>
    /// <param name="embeddedProvider">Embedded resource provider.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">Migration configuration options.</param>
    public MigrationDiscovery(
        IFileSystem fileSystem,
        IEmbeddedResourceProvider embeddedProvider,
        ILogger<MigrationDiscovery> logger,
        IOptions<MigrationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _fileSystem = fileSystem;
        _embeddedProvider = embeddedProvider;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Discovers all available migrations from embedded resources and file system.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of discovered migration files ordered by version.</returns>
    public async Task<IReadOnlyList<MigrationFile>> DiscoverAsync(CancellationToken ct = default)
    {
        var migrations = new Dictionary<string, MigrationFile>();

        // Discover embedded migrations
        var embeddedResources = await _embeddedProvider.GetMigrationResourcesAsync(ct).ConfigureAwait(false);
        foreach (var resource in embeddedResources)
        {
            var version = ExtractVersion(resource.Name);
            if (migrations.ContainsKey(version))
            {
                throw new DuplicateMigrationVersionException(version);
            }

            var checksum = CalculateChecksum(resource.Content);
            var migration = new MigrationFile
            {
                Version = version,
                UpContent = resource.Content,
                DownContent = null,
                Checksum = checksum,
                Source = MigrationSource.Embedded
            };

            migrations[version] = migration;
            _logger.LogDebug("Discovered embedded migration: {Version}", version);

            // Embedded migrations never have down scripts
            _logger.LogWarning("Migration {Version} does not have a down script for rollback", version);
        }

        // Discover file-based migrations
        var files = await _fileSystem.GetFilesAsync(_options.Directory, "*.sql", ct).ConfigureAwait(false);
        var fileGroups = files
            .Select(f =>
            {
                var filename = Path.GetFileName(f);
                var nameWithoutSql = filename.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
                    ? filename[..^4]
                    : filename;
                var isDown = nameWithoutSql.EndsWith("_down", StringComparison.OrdinalIgnoreCase);
                return new
                {
                    Path = f,
                    Version = ExtractVersion(filename),
                    IsDown = isDown
                };
            })
            .GroupBy(x => x.Version);

        foreach (var group in fileGroups)
        {
            var version = group.Key;
            var upFile = group.FirstOrDefault(x => !x.IsDown);
            var downFile = group.FirstOrDefault(x => x.IsDown);

            if (upFile == null)
            {
                // Only down script found, skip
                continue;
            }

            if (migrations.ContainsKey(version))
            {
                throw new DuplicateMigrationVersionException(version);
            }

            var upContent = await _fileSystem.ReadAllTextAsync(upFile.Path, ct).ConfigureAwait(false);
            var checksum = CalculateChecksum(upContent);

            string? downContent = null;
            if (downFile != null)
            {
                downContent = await _fileSystem.ReadAllTextAsync(downFile.Path, ct).ConfigureAwait(false);
            }

            var migration = new MigrationFile
            {
                Version = version,
                UpContent = upContent,
                DownContent = downContent,
                Checksum = checksum,
                Source = MigrationSource.File
            };

            migrations[version] = migration;
            _logger.LogDebug(
                "Discovered file-based migration: {Version} (HasDownScript: {HasDown})",
                version,
                migration.HasDownScript);

            if (downFile == null)
            {
                _logger.LogWarning("Migration {Version} does not have a down script for rollback", version);
            }
        }

        // Order by version and return
        return migrations.Values
            .OrderBy(m => m.Version, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Gets only pending (not yet applied) migrations.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of pending migration files.</returns>
    public async Task<IReadOnlyList<MigrationFile>> GetPendingAsync(CancellationToken ct = default)
    {
        // This method will be implemented when we have IMigrationRepository integration
        // For now, just return all discovered migrations
        return await DiscoverAsync(ct).ConfigureAwait(false);
    }

    private static string ExtractVersion(string filename)
    {
        // Remove .sql extension
        var nameWithoutExtension = filename.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
            ? filename[..^4]
            : filename;

        // Remove _down suffix if present (must check before extracting version)
        if (nameWithoutExtension.EndsWith("_down", StringComparison.OrdinalIgnoreCase))
        {
            nameWithoutExtension = nameWithoutExtension[..^5];
        }

        // Extract numeric version prefix (e.g., "001_initial_schema" -> "001")
        // This ensures duplicate detection works correctly
        var firstUnderscore = nameWithoutExtension.IndexOf('_', StringComparison.Ordinal);
        if (firstUnderscore > 0)
        {
            return nameWithoutExtension[..firstUnderscore];
        }

        // No underscore found, return entire name
        return nameWithoutExtension;
    }

    private static string CalculateChecksum(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
