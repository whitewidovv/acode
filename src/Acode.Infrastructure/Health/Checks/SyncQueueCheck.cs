// src/Acode.Infrastructure/Health/Checks/SyncQueueCheck.cs
namespace Acode.Infrastructure.Health.Checks;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Health;
using Acode.Application.Sync;

/// <summary>
/// Health check for sync queue status.
/// Monitors outbox queue depth and sync lag to detect sync issues.
/// </summary>
public sealed class SyncQueueCheck : IHealthCheck
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly int _degradedThreshold;
    private readonly int _unhealthyThreshold;
    private readonly TimeSpan _lagThreshold;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncQueueCheck"/> class.
    /// </summary>
    /// <param name="outboxRepository">The outbox repository.</param>
    /// <param name="degradedThreshold">Queue depth threshold for degraded status (default 100).</param>
    /// <param name="unhealthyThreshold">Queue depth threshold for unhealthy status (default 500).</param>
    /// <param name="lagThreshold">Sync lag threshold for degraded status (default 5 minutes).</param>
    public SyncQueueCheck(
        IOutboxRepository outboxRepository,
        int degradedThreshold = 100,
        int unhealthyThreshold = 500,
        TimeSpan? lagThreshold = null)
    {
        ArgumentNullException.ThrowIfNull(outboxRepository);

        if (degradedThreshold <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(degradedThreshold), "Degraded threshold must be greater than zero");
        }

        if (unhealthyThreshold <= degradedThreshold)
        {
            throw new ArgumentOutOfRangeException(nameof(unhealthyThreshold), "Unhealthy threshold must be greater than degraded threshold");
        }

        _outboxRepository = outboxRepository;
        _degradedThreshold = degradedThreshold;
        _unhealthyThreshold = unhealthyThreshold;
        _lagThreshold = lagThreshold ?? TimeSpan.FromMinutes(5);
    }

    /// <inheritdoc/>
    public string Name => "Sync Queue";

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var pendingEntries = await _outboxRepository.GetPendingAsync(limit: 1000, cancellationToken).ConfigureAwait(false);
            var queueDepth = pendingEntries.Count;

            TimeSpan? syncLag = null;
            if (pendingEntries.Count > 0)
            {
                var oldestEntry = pendingEntries[0]; // GetPendingAsync returns ordered by CreatedAt ASC
                syncLag = DateTimeOffset.UtcNow - oldestEntry.CreatedAt;
            }

            stopwatch.Stop();

            var details = new Dictionary<string, object>
            {
                ["QueueDepth"] = queueDepth,
                ["SyncLag"] = syncLag?.ToString() ?? "None"
            };

            // Check unhealthy conditions
            if (queueDepth >= _unhealthyThreshold)
            {
                return new HealthCheckResult
                {
                    Name = Name,
                    Status = HealthStatus.Unhealthy,
                    Duration = stopwatch.Elapsed,
                    Description = $"Sync queue depth critically high: {queueDepth} entries (threshold: {_unhealthyThreshold})",
                    ErrorCode = "SYNC_QUEUE_CRITICAL",
                    Suggestion = "Check sync service status and network connectivity. Consider increasing sync workers.",
                    Details = details
                };
            }

            if (syncLag.HasValue && syncLag.Value > _lagThreshold)
            {
                return new HealthCheckResult
                {
                    Name = Name,
                    Status = HealthStatus.Unhealthy,
                    Duration = stopwatch.Elapsed,
                    Description = $"Sync lag critically high: {syncLag.Value.TotalMinutes:F1} minutes (threshold: {_lagThreshold.TotalMinutes:F1} minutes)",
                    ErrorCode = "SYNC_LAG_CRITICAL",
                    Suggestion = "Oldest pending entry is too old. Check sync service logs for errors.",
                    Details = details
                };
            }

            // Check degraded conditions
            if (queueDepth >= _degradedThreshold)
            {
                return HealthCheckResult.Degraded(
                    Name,
                    stopwatch.Elapsed,
                    $"Sync queue depth elevated: {queueDepth} entries (threshold: {_degradedThreshold})",
                    "Monitor queue depth. May need to increase sync frequency.");
            }

            // Healthy
            var description = queueDepth == 0
                ? "Sync queue is empty"
                : $"Sync queue healthy: {queueDepth} entries pending";

            return new HealthCheckResult
            {
                Name = Name,
                Status = HealthStatus.Healthy,
                Duration = stopwatch.Elapsed,
                Description = description,
                Details = details
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return HealthCheckResult.Unhealthy(
                Name,
                stopwatch.Elapsed,
                $"Sync queue check failed: {ex.Message}",
                "SYNC_QUEUE_CHECK_FAILED",
                "Check outbox repository and database connectivity");
        }
    }
}
