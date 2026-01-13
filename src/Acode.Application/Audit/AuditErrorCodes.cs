namespace Acode.Application.Audit;

/// <summary>
/// Defines standard error codes for audit system failures.
/// All codes follow the format: ACODE-AUD-XXX where XXX is a zero-padded number.
/// </summary>
public static class AuditErrorCodes
{
    /// <summary>
    /// ACODE-AUD-001: Audit initialization failed.
    /// Indicates the audit system could not be initialized at startup.
    /// </summary>
    public const string InitializationFailed = "ACODE-AUD-001";

    /// <summary>
    /// ACODE-AUD-002: Audit write failed.
    /// Indicates an event could not be written to the audit log.
    /// </summary>
    public const string WriteFailed = "ACODE-AUD-002";

    /// <summary>
    /// ACODE-AUD-003: Audit directory not writable.
    /// Indicates the log directory exists but does not have write permissions.
    /// </summary>
    public const string DirectoryNotWritable = "ACODE-AUD-003";

    /// <summary>
    /// ACODE-AUD-004: Disk full - audit halted.
    /// Indicates the disk is full and audit logging has been halted.
    /// </summary>
    public const string DiskFull = "ACODE-AUD-004";

    /// <summary>
    /// ACODE-AUD-005: Log rotation failed.
    /// Indicates the log file rotation process failed.
    /// </summary>
    public const string RotationFailed = "ACODE-AUD-005";

    /// <summary>
    /// ACODE-AUD-006: Integrity verification failed.
    /// Indicates the audit log integrity check failed.
    /// </summary>
    public const string IntegrityVerificationFailed = "ACODE-AUD-006";

    /// <summary>
    /// ACODE-AUD-007: Checksum mismatch detected.
    /// Indicates a checksum mismatch was detected, suggesting tampering or corruption.
    /// </summary>
    public const string ChecksumMismatch = "ACODE-AUD-007";

    /// <summary>
    /// ACODE-AUD-008: Session not found.
    /// Indicates the requested audit session ID does not exist.
    /// </summary>
    public const string SessionNotFound = "ACODE-AUD-008";

    /// <summary>
    /// ACODE-AUD-009: Export failed.
    /// Indicates the audit log export operation failed.
    /// </summary>
    public const string ExportFailed = "ACODE-AUD-009";

    /// <summary>
    /// ACODE-AUD-010: Invalid query parameters.
    /// Indicates the audit query contains invalid or malformed parameters.
    /// </summary>
    public const string InvalidQueryParameters = "ACODE-AUD-010";

    /// <summary>
    /// Gets a human-readable error message for the specified error code.
    /// </summary>
    /// <param name="errorCode">The error code (e.g., "ACODE-AUD-001").</param>
    /// <returns>A descriptive error message, or a generic message if the code is unknown.</returns>
    public static string GetErrorMessage(string errorCode)
    {
        return errorCode switch
        {
            InitializationFailed => "Audit system initialization failed. Check log directory permissions and disk space.",
            WriteFailed => "Failed to write audit event. Check log directory permissions and disk space.",
            DirectoryNotWritable => "Audit log directory is not writable. Check directory permissions.",
            DiskFull => "Disk full - audit logging has been halted. Free disk space immediately.",
            RotationFailed => "Log file rotation failed. Check disk space and file permissions.",
            IntegrityVerificationFailed => "Audit log integrity verification failed. Logs may be corrupted or tampered with.",
            ChecksumMismatch => "Checksum mismatch detected in audit log. Possible tampering or corruption.",
            SessionNotFound => "Audit session not found. Verify the session ID is correct.",
            ExportFailed => "Audit log export failed. Check output path permissions and disk space.",
            InvalidQueryParameters => "Invalid audit query parameters. Check date formats, event types, and severity levels.",
            _ => $"Unknown audit error code: {errorCode}"
        };
    }

    /// <summary>
    /// Gets a suggested resolution for the specified error code.
    /// </summary>
    /// <param name="errorCode">The error code (e.g., "ACODE-AUD-001").</param>
    /// <returns>A suggested resolution, or a generic message if the code is unknown.</returns>
    public static string GetResolution(string errorCode)
    {
        return errorCode switch
        {
            InitializationFailed => "Verify the log directory exists and is writable. Check disk space. Review system logs for details.",
            WriteFailed => "Ensure sufficient disk space. Verify log directory permissions. Check for filesystem errors.",
            DirectoryNotWritable => "Run 'chmod +w' on the log directory or move to a writable location.",
            DiskFull => "Free disk space immediately. Consider cleanup with 'acode audit cleanup' or archiving old logs.",
            RotationFailed => "Check disk space. Verify no processes have log files open. Review log directory permissions.",
            IntegrityVerificationFailed => "Investigate potential security breach. Compare checksums. Restore from backup if available.",
            ChecksumMismatch => "Do not trust affected log file. Investigate cause. Restore from backup if available.",
            SessionNotFound => "Use 'acode audit list' to see available sessions. Verify session ID format (sess_xxx).",
            ExportFailed => "Verify output directory exists. Check write permissions. Ensure sufficient disk space.",
            InvalidQueryParameters => "Check date format (ISO 8601). Verify event type and severity level spelling.",
            _ => "Consult documentation or contact support."
        };
    }

    /// <summary>
    /// Gets the severity level for the specified error code.
    /// </summary>
    /// <param name="errorCode">The error code (e.g., "ACODE-AUD-001").</param>
    /// <returns>The severity level: Critical, Error, or Warning.</returns>
    public static string GetSeverity(string errorCode)
    {
        return errorCode switch
        {
            InitializationFailed => "Critical",
            WriteFailed => "Error",
            DirectoryNotWritable => "Critical",
            DiskFull => "Critical",
            RotationFailed => "Error",
            IntegrityVerificationFailed => "Critical",
            ChecksumMismatch => "Critical",
            SessionNotFound => "Warning",
            ExportFailed => "Error",
            InvalidQueryParameters => "Warning",
            _ => "Error"
        };
    }
}
