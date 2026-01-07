// src/Acode.Application/Database/IMigrationBootstrapper.cs
namespace Acode.Application.Database;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Orchestrates database migration checking and application during startup.
/// </summary>
public interface IMigrationBootstrapper
{
    /// <summary>
    /// Bootstraps the database by checking for pending migrations and optionally applying them.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Bootstrap result indicating success and migrations applied.</returns>
    Task<BootstrapResult> BootstrapAsync(CancellationToken cancellationToken = default);
}
