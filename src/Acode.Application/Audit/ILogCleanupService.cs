namespace Acode.Application.Audit;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service for cleaning up old audit logs.
/// </summary>
public interface ILogCleanupService
{
    /// <summary>
    /// Cleans up expired logs based on retention policy.
    /// </summary>
    /// <param name="logDirectory">The directory containing logs.</param>
    /// <param name="retentionDays">The number of days to retain logs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of files deleted.</returns>
    Task<int> CleanupExpiredLogsAsync(
        string logDirectory,
        int retentionDays,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enforces storage limits by deleting oldest logs.
    /// </summary>
    /// <param name="logDirectory">The directory containing logs.</param>
    /// <param name="maxStorageBytes">The maximum storage in bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of bytes freed.</returns>
    Task<long> EnforceStorageLimitAsync(
        string logDirectory,
        long maxStorageBytes,
        CancellationToken cancellationToken = default);
}
