// src/Acode.Application/Health/IHealthCheck.cs
namespace Acode.Application.Health;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Interface for implementing health checks.
/// </summary>
public interface IHealthCheck
{
    /// <summary>
    /// Gets the unique name of this health check.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the health check asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The health check result.</returns>
    Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
