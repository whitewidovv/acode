// src/Acode.Application/Health/IHealthCheckRegistry.cs
namespace Acode.Application.Health;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Registry for managing and executing health checks.
/// </summary>
public interface IHealthCheckRegistry
{
    /// <summary>
    /// Registers a health check in the registry.
    /// </summary>
    /// <param name="healthCheck">The health check to register.</param>
    void Register(IHealthCheck healthCheck);

    /// <summary>
    /// Executes all registered health checks in parallel and aggregates the results.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregated health check result.</returns>
    Task<CompositeHealthResult> CheckAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered health checks.
    /// </summary>
    /// <returns>A read-only list of registered health checks.</returns>
    IReadOnlyList<IHealthCheck> GetRegisteredChecks();
}
