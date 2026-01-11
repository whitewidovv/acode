namespace Acode.Application.Audit.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Command to cleanup old audit logs.
/// </summary>
public sealed record CleanupLogsCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupLogsCommand"/> class.
    /// </summary>
    /// <param name="logDirectory">The directory containing audit logs.</param>
    /// <param name="retentionDays">The number of days to retain logs.</param>
    /// <param name="maxStorageBytes">The maximum storage in bytes (optional).</param>
    public CleanupLogsCommand(string logDirectory, int retentionDays, long? maxStorageBytes = null)
    {
        LogDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));

        if (retentionDays <= 0)
        {
            throw new ArgumentException("Retention days must be positive", nameof(retentionDays));
        }

        RetentionDays = retentionDays;
        MaxStorageBytes = maxStorageBytes ?? 1024L * 1024 * 1024; // 1GB default
    }

    /// <summary>
    /// Gets the directory containing audit logs.
    /// </summary>
    public string LogDirectory { get; }

    /// <summary>
    /// Gets the number of days to retain logs.
    /// </summary>
    public int RetentionDays { get; }

    /// <summary>
    /// Gets the maximum storage in bytes.
    /// </summary>
    public long MaxStorageBytes { get; }
}

/// <summary>
/// Result of cleanup operation.
/// </summary>
public sealed record CleanupLogsResult
{
    /// <summary>
    /// Gets the number of files deleted.
    /// </summary>
    public required int FilesDeleted { get; init; }

    /// <summary>
    /// Gets the number of bytes freed.
    /// </summary>
    public required long BytesFreed { get; init; }
}

/// <summary>
/// Handler for CleanupLogsCommand.
/// </summary>
public sealed class CleanupLogsCommandHandler
{
    private readonly ILogCleanupService _cleanupService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupLogsCommandHandler"/> class.
    /// </summary>
    /// <param name="cleanupService">The log cleanup service.</param>
    public CleanupLogsCommandHandler(ILogCleanupService cleanupService)
    {
        _cleanupService = cleanupService ?? throw new ArgumentNullException(nameof(cleanupService));
    }

    /// <summary>
    /// Handles the CleanupLogsCommand.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cleanup result.</returns>
    public async Task<CleanupLogsResult> HandleAsync(
        CleanupLogsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Cleanup expired logs and enforce storage limits
        var filesDeleted = await _cleanupService.CleanupExpiredLogsAsync(
            command.LogDirectory,
            command.RetentionDays,
            cancellationToken).ConfigureAwait(false);

        var bytesFreed = await _cleanupService.EnforceStorageLimitAsync(
            command.LogDirectory,
            command.MaxStorageBytes,
            cancellationToken).ConfigureAwait(false);

        return new CleanupLogsResult
        {
            FilesDeleted = filesDeleted,
            BytesFreed = bytesFreed,
        };
    }
}
