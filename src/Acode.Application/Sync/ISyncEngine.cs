// src/Acode.Application/Sync/ISyncEngine.cs
namespace Acode.Application.Sync;

using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Sync;

/// <summary>
/// Sync engine for managing background synchronization between SQLite and Postgres.
/// </summary>
public interface ISyncEngine
{
    /// <summary>
    /// Starts the background sync processor.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the background sync processor gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers an immediate sync cycle.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SyncNowAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current sync engine status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sync status.</returns>
    Task<SyncStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses the background sync processor.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes the background sync processor.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResumeAsync(CancellationToken cancellationToken = default);
}
