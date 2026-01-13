namespace Acode.Infrastructure.Audit;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Acode.Domain.Audit;

/// <summary>
/// Handles log rotation, cleanup, and storage limit enforcement.
/// Rotates files by appending numeric suffixes (.1, .2, .3, etc.).
/// </summary>
public sealed class AuditLogRotator
{
    private readonly AuditConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogRotator"/> class.
    /// </summary>
    /// <param name="config">Audit configuration.</param>
    public AuditLogRotator(AuditConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Event fired before a file is deleted.
    /// Allows logging or auditing of deletion operations.
    /// </summary>
    public event Action<string>? OnBeforeDelete;

    /// <summary>
    /// Rotates a log file if it exceeds the size limit.
    /// Renames the file with numeric suffix (.1, .2, .3, etc.).
    /// </summary>
    /// <param name="logPath">Path to the log file to check.</param>
    /// <returns>Result indicating if rotation occurred.</returns>
    public async Task<RotationResult> RotateIfNeededAsync(string logPath)
    {
        ArgumentNullException.ThrowIfNull(logPath);

        // Check if file exists
        if (!File.Exists(logPath))
        {
            return new RotationResult { RotationOccurred = false };
        }

        // Check file size
        var fileInfo = new FileInfo(logPath);
        if (fileInfo.Length < _config.MaxFileSize)
        {
            return new RotationResult { RotationOccurred = false };
        }

        // Find next available rotation number
        var nextNumber = FindNextRotationNumber(logPath);
        var rotatedPath = $"{logPath}.{nextNumber}";

        // Preserve Unix permissions if applicable
        UnixFileMode? originalMode = null;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            originalMode = File.GetUnixFileMode(logPath);
        }

        // Atomic rename
        File.Move(logPath, rotatedPath);

        // Restore permissions
        if (originalMode.HasValue &&
            (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
             RuntimeInformation.IsOSPlatform(OSPlatform.OSX)))
        {
            File.SetUnixFileMode(rotatedPath, originalMode.Value);
        }

        return await Task.FromResult(
            new RotationResult
            {
                RotationOccurred = true,
                RotatedPath = rotatedPath,
            }).ConfigureAwait(false);
    }

    /// <summary>
    /// Cleans up expired log files based on retention policy.
    /// Deletes files older than the specified retention period.
    /// </summary>
    /// <param name="directory">Directory containing log files.</param>
    /// <param name="retentionDays">Number of days to retain logs.</param>
    /// <returns>List of deleted file paths.</returns>
    public async Task<List<string>> CleanupExpiredLogsAsync(string directory, int retentionDays)
    {
        ArgumentNullException.ThrowIfNull(directory);

        if (!Directory.Exists(directory))
        {
            return new List<string>();
        }

        var cutoffDate = DateTime.Now.AddDays(-retentionDays);
        var deleted = new List<string>();

        var logFiles = Directory.GetFiles(directory, "*.jsonl");

        foreach (var filePath in logFiles)
        {
            var lastWriteTime = File.GetLastWriteTime(filePath);
            if (lastWriteTime < cutoffDate)
            {
                OnBeforeDelete?.Invoke(filePath);
                File.Delete(filePath);
                deleted.Add(filePath);
            }
        }

        return await Task.FromResult(deleted).ConfigureAwait(false);
    }

    /// <summary>
    /// Enforces storage limits by deleting oldest files.
    /// Deletes files oldest-first until total storage is under limit.
    /// </summary>
    /// <param name="directory">Directory containing log files.</param>
    /// <param name="maxBytes">Maximum total storage in bytes.</param>
    /// <returns>List of deleted file paths.</returns>
    public async Task<List<string>> EnforceStorageLimitAsync(string directory, long maxBytes)
    {
        ArgumentNullException.ThrowIfNull(directory);

        if (!Directory.Exists(directory))
        {
            return new List<string>();
        }

        var deleted = new List<string>();
        var logFiles = Directory.GetFiles(directory, "*.jsonl")
            .Select(f => new FileInfo(f))
            .OrderBy(f => f.LastWriteTime) // Oldest first
            .ToList();

        var totalSize = logFiles.Sum(f => f.Length);

        foreach (var fileInfo in logFiles)
        {
            if (totalSize <= maxBytes)
            {
                break;
            }

            OnBeforeDelete?.Invoke(fileInfo.FullName);
            File.Delete(fileInfo.FullName);
            deleted.Add(fileInfo.FullName);
            totalSize -= fileInfo.Length;
        }

        return await Task.FromResult(deleted).ConfigureAwait(false);
    }

    private static int FindNextRotationNumber(string logPath)
    {
        var directory = Path.GetDirectoryName(logPath) ?? string.Empty;
        var fileName = Path.GetFileName(logPath);
        var pattern = $"{fileName}.*";

        var existingRotations = Directory.Exists(directory)
            ? Directory.GetFiles(directory, pattern)
                .Select(f => Path.GetFileName(f))
                .Where(f => f.StartsWith(fileName + ".", StringComparison.Ordinal))
                .Select(f => f.Substring(fileName.Length + 1))
                .Where(suffix => int.TryParse(suffix, out _))
                .Select(suffix => int.Parse(suffix))
                .ToList()
            : new List<int>();

        return existingRotations.Any() ? existingRotations.Max() + 1 : 1;
    }
}
