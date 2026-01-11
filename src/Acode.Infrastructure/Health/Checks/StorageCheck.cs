// src/Acode.Infrastructure/Health/Checks/StorageCheck.cs
namespace Acode.Infrastructure.Health.Checks;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Health;

/// <summary>
/// Health check for storage capacity.
/// Monitors disk space and database file size.
/// </summary>
public sealed class StorageCheck : IHealthCheck
{
    private readonly string _databasePath;
    private readonly long _degradedThresholdBytes;
    private readonly long _unhealthyThresholdBytes;
    private readonly double _degradedPercentage;
    private readonly double _unhealthyPercentage;

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageCheck"/> class.
    /// </summary>
    /// <param name="databasePath">Path to the database file.</param>
    /// <param name="degradedThresholdMb">Free space threshold for degraded status in MB (default 500 MB).</param>
    /// <param name="unhealthyThresholdMb">Free space threshold for unhealthy status in MB (default 100 MB).</param>
    /// <param name="degradedPercentage">Free space percentage threshold for degraded status (default 10%).</param>
    /// <param name="unhealthyPercentage">Free space percentage threshold for unhealthy status (default 5%).</param>
    public StorageCheck(
        string databasePath,
        int degradedThresholdMb = 500,
        int unhealthyThresholdMb = 100,
        double degradedPercentage = 10.0,
        double unhealthyPercentage = 5.0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        if (degradedThresholdMb <= unhealthyThresholdMb)
        {
            throw new ArgumentOutOfRangeException(nameof(degradedThresholdMb), "Degraded threshold must be greater than unhealthy threshold");
        }

        if (degradedPercentage <= unhealthyPercentage)
        {
            throw new ArgumentOutOfRangeException(nameof(degradedPercentage), "Degraded percentage must be greater than unhealthy percentage");
        }

        _databasePath = databasePath;
        _degradedThresholdBytes = degradedThresholdMb * 1024L * 1024L;
        _unhealthyThresholdBytes = unhealthyThresholdMb * 1024L * 1024L;
        _degradedPercentage = degradedPercentage;
        _unhealthyPercentage = unhealthyPercentage;
    }

    /// <inheritdoc/>
    public string Name => "Storage";

    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Get database file info
            var dbFile = new FileInfo(_databasePath);
            var dbSizeMb = dbFile.Exists ? dbFile.Length / 1024.0 / 1024.0 : 0;

            // Get drive info
            var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(_databasePath))!);

            if (!drive.IsReady)
            {
                stopwatch.Stop();
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    Name,
                    stopwatch.Elapsed,
                    $"Drive {drive.Name} is not ready",
                    "STORAGE_DRIVE_NOT_READY",
                    "Check drive connectivity and mount status"));
            }

            var freeSpaceBytes = drive.AvailableFreeSpace;
            var totalSpaceBytes = drive.TotalSize;
            var freeSpaceMb = freeSpaceBytes / 1024.0 / 1024.0;
            var freeSpaceGb = freeSpaceBytes / 1024.0 / 1024.0 / 1024.0;
            var freePercentage = (freeSpaceBytes * 100.0) / totalSpaceBytes;

            stopwatch.Stop();

            var details = new Dictionary<string, object>
            {
                ["DatabaseSizeMB"] = Math.Round(dbSizeMb, 2),
                ["FreeSpaceGB"] = Math.Round(freeSpaceGb, 2),
                ["FreePercentage"] = Math.Round(freePercentage, 1),
                ["DriveName"] = drive.Name
            };

            // Check unhealthy conditions (absolute threshold OR percentage threshold)
            if (freeSpaceBytes < _unhealthyThresholdBytes || freePercentage < _unhealthyPercentage)
            {
                return Task.FromResult(new HealthCheckResult
                {
                    Name = Name,
                    Status = HealthStatus.Unhealthy,
                    Duration = stopwatch.Elapsed,
                    Description = $"Critically low disk space: {freeSpaceGb:F2} GB free ({freePercentage:F1}%)",
                    ErrorCode = "STORAGE_CRITICALLY_LOW",
                    Suggestion = "Free up disk space immediately or database operations may fail",
                    Details = details
                });
            }

            // Check degraded conditions
            if (freeSpaceBytes < _degradedThresholdBytes || freePercentage < _degradedPercentage)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    Name,
                    stopwatch.Elapsed,
                    $"Low disk space: {freeSpaceGb:F2} GB free ({freePercentage:F1}%)",
                    "Monitor disk usage and plan for cleanup or expansion"));
            }

            // Healthy
            return Task.FromResult(new HealthCheckResult
            {
                Name = Name,
                Status = HealthStatus.Healthy,
                Duration = stopwatch.Elapsed,
                Description = $"Sufficient disk space: {freeSpaceGb:F2} GB free ({freePercentage:F1}%)",
                Details = details
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return Task.FromResult(HealthCheckResult.Unhealthy(
                Name,
                stopwatch.Elapsed,
                $"Storage check failed: {ex.Message}",
                "STORAGE_CHECK_FAILED",
                "Verify database path and file system permissions"));
        }
    }
}
