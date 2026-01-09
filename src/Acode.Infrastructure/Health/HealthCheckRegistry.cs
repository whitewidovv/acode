// src/Acode.Infrastructure/Health/HealthCheckRegistry.cs
namespace Acode.Infrastructure.Health;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Health;

/// <summary>
/// Implementation of health check registry with parallel execution.
/// </summary>
public sealed class HealthCheckRegistry : IHealthCheckRegistry
{
    private readonly List<IHealthCheck> _checks = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public void Register(IHealthCheck healthCheck)
    {
        ArgumentNullException.ThrowIfNull(healthCheck);

        lock (_lock)
        {
            // Idempotent - don't add duplicates by name
            if (_checks.Any(c => c.Name == healthCheck.Name))
            {
                return;
            }

            _checks.Add(healthCheck);
        }
    }

    /// <inheritdoc/>
    public async Task<CompositeHealthResult> CheckAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IHealthCheck> checksToRun;

        lock (_lock)
        {
            checksToRun = _checks.ToList();
        }

        if (checksToRun.Count == 0)
        {
            return new CompositeHealthResult
            {
                AggregateStatus = HealthStatus.Healthy,
                Results = Array.Empty<HealthCheckResult>(),
                TotalDuration = TimeSpan.Zero
            };
        }

        // Run all checks in parallel
        var checkTasks = checksToRun.Select(check => ExecuteCheckSafelyAsync(check, cancellationToken)).ToArray();
        var results = await Task.WhenAll(checkTasks).ConfigureAwait(false);

        // Aggregate status (worst-case wins)
        var aggregateStatus = results.Max(r => r.Status);

        // Total duration is the max of all individual durations (since parallel)
        var totalDuration = results.Any() ? results.Max(r => r.Duration) : TimeSpan.Zero;

        return new CompositeHealthResult
        {
            AggregateStatus = aggregateStatus,
            Results = results,
            TotalDuration = totalDuration
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<IHealthCheck> GetRegisteredChecks()
    {
        lock (_lock)
        {
            return _checks.ToList();
        }
    }

    private static async Task<HealthCheckResult> ExecuteCheckSafelyAsync(
        IHealthCheck check,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await check.CheckAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Convert exceptions to unhealthy results
            return new HealthCheckResult
            {
                Name = check.Name,
                Status = HealthStatus.Unhealthy,
                Duration = stopwatch.Elapsed,
                Description = $"Health check threw exception: {ex.Message}",
                ErrorCode = "HEALTH_CHECK_EXCEPTION",
                Suggestion = "Check application logs for detailed exception information"
            };
        }
    }
}
