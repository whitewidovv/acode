// src/Acode.Infrastructure/Persistence/Migrations/MigrationValidator.cs
namespace Acode.Infrastructure.Persistence.Migrations;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Database;
using Microsoft.Extensions.Logging;

/// <summary>
/// Validates discovered migrations against applied migration history.
/// </summary>
public sealed class MigrationValidator
{
    private readonly ILogger<MigrationValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationValidator"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public MigrationValidator(ILogger<MigrationValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates discovered migrations against applied migrations.
    /// </summary>
    /// <param name="discovered">Discovered migration files.</param>
    /// <param name="applied">Applied migrations from history.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with pending migrations, checksum mismatches, and version gaps.</returns>
    public Task<ValidationResult> ValidateAsync(
        IReadOnlyList<MigrationFile> discovered,
        IReadOnlyList<AppliedMigration> applied,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(discovered);
        ArgumentNullException.ThrowIfNull(applied);

        var appliedVersions = applied.ToDictionary(m => m.Version, m => m);
        var discoveredVersions = discovered.ToDictionary(m => m.Version, m => m);

        // Identify pending migrations (discovered but not applied)
        var pending = discovered
            .Where(m => !appliedVersions.ContainsKey(m.Version))
            .ToList();

        // Identify checksum mismatches (applied migrations with different checksums)
        var mismatches = new List<ChecksumMismatch>();
        foreach (var appliedMigration in applied)
        {
            if (discoveredVersions.TryGetValue(appliedMigration.Version, out var discoveredMigration))
            {
                if (appliedMigration.Checksum != discoveredMigration.Checksum)
                {
                    var mismatch = new ChecksumMismatch(
                        appliedMigration.Version,
                        appliedMigration.Checksum,
                        discoveredMigration.Checksum,
                        appliedMigration.AppliedAt);

                    mismatches.Add(mismatch);

                    _logger.LogWarning(
                        "Migration {Version} checksum mismatch. Expected: {Expected}, Actual: {Actual}. File may have been modified after application.",
                        appliedMigration.Version,
                        appliedMigration.Checksum,
                        discoveredMigration.Checksum);
                }
            }
        }

        // Detect version gaps
        var versionGaps = DetectVersionGaps(discovered.Select(m => m.Version).ToList());

        var result = new ValidationResult
        {
            PendingMigrations = pending,
            ChecksumMismatches = mismatches,
            VersionGaps = versionGaps
        };

        return Task.FromResult(result);
    }

    private static List<VersionGap> DetectVersionGaps(List<string> versions)
    {
        var gaps = new List<VersionGap>();

        if (versions.Count <= 1)
        {
            return gaps;
        }

        var sorted = versions.OrderBy(v => v, StringComparer.Ordinal).ToList();

        for (var i = 0; i < sorted.Count - 1; i++)
        {
            var current = sorted[i];
            var next = sorted[i + 1];

            // Check if versions are sequential (e.g., "001", "002", "003")
            // This is a simple check assuming numeric prefixes with leading zeros
            if (int.TryParse(current, out var currentNum) && int.TryParse(next, out var nextNum))
            {
                if (nextNum - currentNum > 1)
                {
                    // Gap detected
                    var missing = (currentNum + 1).ToString().PadLeft(current.Length, '0');
                    gaps.Add(new VersionGap
                    {
                        MissingVersion = missing,
                        BeforeVersion = current,
                        AfterVersion = next
                    });
                }
            }
        }

        return gaps;
    }
}
