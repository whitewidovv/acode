// src/Acode.Application/Database/IMigrationLock.cs
namespace Acode.Application.Database;

/// <summary>
/// Interface for distributed migration locking to prevent concurrent migration execution.
/// </summary>
public interface IMigrationLock : IAsyncDisposable
{
    /// <summary>
    /// Attempts to acquire the migration lock.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if lock acquired, false if timeout.</returns>
    Task<bool> TryAcquireAsync(CancellationToken ct = default);

    /// <summary>
    /// Forces release of a potentially stale lock.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task ForceReleaseAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets information about the current lock holder, if any.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Lock information if a lock exists, null otherwise.</returns>
    Task<LockInfo?> GetLockInfoAsync(CancellationToken ct = default);
}
