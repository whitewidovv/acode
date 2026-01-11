namespace Acode.Infrastructure.Database;

/// <summary>
/// Exception thrown when a database connection operation fails.
/// </summary>
/// <remarks>
/// This exception includes structured error codes for diagnostics and monitoring.
/// Error codes follow the pattern ACODE-DB-XXX for consistent error classification.
/// </remarks>
public sealed class DatabaseConnectionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseConnectionException"/> class.
    /// </summary>
    /// <param name="errorCode">The structured error code (e.g., ACODE-DB-001).</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="innerException">The exception that caused this failure, if any.</param>
    public DatabaseConnectionException(string errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(errorCode);
        ArgumentNullException.ThrowIfNull(message);

        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the structured error code identifying the type of database failure.
    /// </summary>
    /// <remarks>
    /// Error codes:
    /// - ACODE-DB-001: Connection failed - unable to establish database connection.
    /// - ACODE-DB-002: Migration failed - error during migration execution.
    /// - ACODE-DB-003: Transaction failed - error during transaction commit/rollback.
    /// - ACODE-DB-004: Database locked - resource busy, retry later.
    /// - ACODE-DB-005: Schema error - table/column not found.
    /// - ACODE-DB-006: Constraint violation - unique/FK/check constraint failed.
    /// - ACODE-DB-007: Timeout - operation exceeded time limit.
    /// - ACODE-DB-008: Pool exhausted - no connections available.
    /// - ACODE-DB-009: Checksum mismatch - migration tampering detected.
    /// - ACODE-DB-010: Validation failed - migration content validation error.
    /// </remarks>
    public string ErrorCode { get; }
}
