namespace Acode.Infrastructure.Audit;

using System;
using System.IO;
using System.Security.Cryptography;

/// <summary>
/// Verifies integrity of audit logs using SHA256 checksums.
/// Detects tampering, modification, truncation, or insertion.
/// </summary>
public sealed class AuditIntegrityVerifier
{
    /// <summary>
    /// Computes SHA256 checksum of a log file.
    /// </summary>
    /// <param name="logPath">Path to the log file.</param>
    /// <returns>Lowercase hexadecimal SHA256 checksum (64 characters).</returns>
    public string ComputeChecksum(string logPath)
    {
        ArgumentNullException.ThrowIfNull(logPath);

        if (!File.Exists(logPath))
        {
            throw new FileNotFoundException($"Log file not found: {logPath}");
        }

        using var sha256 = SHA256.Create();
        var fileBytes = File.ReadAllBytes(logPath);
        var hash = sha256.ComputeHash(fileBytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Writes checksum to sidecar .sha256 file.
    /// </summary>
    /// <param name="logPath">Path to the log file.</param>
    public void WriteChecksumFile(string logPath)
    {
        ArgumentNullException.ThrowIfNull(logPath);

        var checksum = ComputeChecksum(logPath);
        var checksumPath = logPath + ".sha256";

        File.WriteAllText(checksumPath, checksum);
    }

    /// <summary>
    /// Verifies a log file against its .sha256 checksum.
    /// </summary>
    /// <param name="logPath">Path to the log file.</param>
    /// <returns>True if checksum matches, false otherwise.</returns>
    public bool Verify(string logPath)
    {
        ArgumentNullException.ThrowIfNull(logPath);

        // Check if log file exists
        if (!File.Exists(logPath))
        {
            return false;
        }

        var checksumPath = logPath + ".sha256";

        // Check if checksum file exists
        if (!File.Exists(checksumPath))
        {
            return false;
        }

        try
        {
            // Compute current checksum
            var currentChecksum = ComputeChecksum(logPath);

            // Read expected checksum
            var expectedChecksum = File.ReadAllText(checksumPath).Trim();

            // Compare checksums
            return string.Equals(
                currentChecksum,
                expectedChecksum,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // Any error during verification = failure
            return false;
        }
    }
}
