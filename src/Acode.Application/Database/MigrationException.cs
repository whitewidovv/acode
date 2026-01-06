// src/Acode.Application/Database/MigrationException.cs
namespace Acode.Application.Database;

/// <summary>
/// Exception thrown when a migration operation fails.
/// </summary>
/// <remarks>
/// Contains structured error codes (ACODE-MIG-XXX) for different migration failure scenarios.
/// </remarks>
public sealed class MigrationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationException"/> class.
    /// </summary>
    /// <param name="errorCode">Structured error code (e.g., ACODE-MIG-001).</param>
    /// <param name="message">Error message describing the failure.</param>
    public MigrationException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationException"/> class.
    /// </summary>
    /// <param name="errorCode">Structured error code (e.g., ACODE-MIG-001).</param>
    /// <param name="message">Error message describing the failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public MigrationException(string errorCode, string message, Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the structured error code for this migration failure.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Creates a MigrationException for migration execution failure.
    /// </summary>
    /// <param name="details">Details about the execution failure.</param>
    /// <param name="innerException">The underlying exception.</param>
    /// <returns>MigrationException with error code ACODE-MIG-001.</returns>
    public static MigrationException ExecutionFailed(string details, Exception? innerException) =>
        new("ACODE-MIG-001", $"Migration execution failed: {details}", innerException);

    /// <summary>
    /// Creates a MigrationException for migration lock acquisition timeout.
    /// </summary>
    /// <param name="timeout">The timeout duration that was exceeded.</param>
    /// <returns>MigrationException with error code ACODE-MIG-002.</returns>
    public static MigrationException LockTimeout(TimeSpan timeout) =>
        new("ACODE-MIG-002", $"Could not acquire migration lock within {timeout.TotalSeconds} seconds");

    /// <summary>
    /// Creates a MigrationException for migration file checksum mismatch.
    /// </summary>
    /// <param name="version">The migration version with checksum mismatch.</param>
    /// <param name="expectedChecksum">The checksum stored in the database.</param>
    /// <param name="actualChecksum">The current file checksum.</param>
    /// <returns>MigrationException with error code ACODE-MIG-003.</returns>
    public static MigrationException ChecksumMismatch(string version, string expectedChecksum, string actualChecksum)
    {
        var message = $"Checksum mismatch for migration {version}. Expected: {expectedChecksum}, Actual: {actualChecksum}. " +
                      "Migration file may have been tampered with.";
        return new MigrationException("ACODE-MIG-003", message);
    }

    /// <summary>
    /// Creates a MigrationException for missing down script during rollback.
    /// </summary>
    /// <param name="version">The migration version missing a down script.</param>
    /// <returns>MigrationException with error code ACODE-MIG-004.</returns>
    public static MigrationException MissingDownScript(string version) =>
        new("ACODE-MIG-004", $"Migration {version} does not have a down script for rollback");

    /// <summary>
    /// Creates a MigrationException for rollback execution failure.
    /// </summary>
    /// <param name="version">The migration version that failed to roll back.</param>
    /// <param name="details">Details about the rollback failure.</param>
    /// <param name="innerException">The underlying exception.</param>
    /// <returns>MigrationException with error code ACODE-MIG-005.</returns>
    public static MigrationException RollbackFailed(string version, string details, Exception? innerException) =>
        new("ACODE-MIG-005", $"Rollback failed for migration {version}: {details}", innerException);

    /// <summary>
    /// Creates a MigrationException for version gap detection.
    /// </summary>
    /// <param name="appliedVersions">The versions that have been applied.</param>
    /// <param name="missingVersion">The version that is missing causing a gap.</param>
    /// <returns>MigrationException with error code ACODE-MIG-006.</returns>
    public static MigrationException VersionGapDetected(IEnumerable<string> appliedVersions, string missingVersion)
    {
        var message = $"Version gap detected: migration {missingVersion} is missing. " +
                      $"Applied versions: {string.Join(", ", appliedVersions)}";
        return new MigrationException("ACODE-MIG-006", message);
    }

    /// <summary>
    /// Creates a MigrationException for database connection failure.
    /// </summary>
    /// <param name="details">Details about the connection failure.</param>
    /// <param name="innerException">The underlying exception.</param>
    /// <returns>MigrationException with error code ACODE-MIG-007.</returns>
    public static MigrationException DatabaseConnectionFailed(string details, Exception? innerException) =>
        new("ACODE-MIG-007", $"Database connection failed during migration: {details}", innerException);

    /// <summary>
    /// Creates a MigrationException for backup creation failure.
    /// </summary>
    /// <param name="details">Details about the backup failure.</param>
    /// <param name="innerException">The underlying exception.</param>
    /// <returns>MigrationException with error code ACODE-MIG-008.</returns>
    public static MigrationException BackupFailed(string details, Exception? innerException) =>
        new("ACODE-MIG-008", $"Backup creation failed: {details}", innerException);
}
