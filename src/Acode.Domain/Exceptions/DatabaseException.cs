// src/Acode.Domain/Exceptions/DatabaseException.cs
namespace Acode.Domain.Exceptions;

/// <summary>
/// Exception thrown for database access errors with structured error codes.
/// </summary>
public sealed class DatabaseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseException"/> class.
    /// </summary>
    /// <param name="errorCode">Structured error code.</param>
    /// <param name="message">Exception message.</param>
    /// <param name="isTransient">Whether error is transient and retriable.</param>
    /// <param name="innerException">Inner exception if any.</param>
    /// <param name="correlationId">Correlation ID for tracing (auto-generated if null).</param>
    public DatabaseException(
        string errorCode,
        string message,
        bool isTransient = false,
        Exception? innerException = null,
        string? correlationId = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        IsTransient = isTransient;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString("N")[..12];
    }

    /// <summary>Gets the structured error code in format ACODE-DB-ACC-XXX.</summary>
    public string ErrorCode { get; }

    /// <summary>Gets a value indicating whether this error is transient and can be retried.</summary>
    public bool IsTransient { get; }

    /// <summary>Gets the correlation ID for tracing across logs.</summary>
    public string CorrelationId { get; }

    /// <summary>Connection failed - network, auth, or configuration error.</summary>
    /// <param name="details">Failure details.</param>
    /// <param name="inner">Inner exception.</param>
    /// <returns>DatabaseException with ACODE-DB-ACC-001 error code.</returns>
    public static DatabaseException ConnectionFailed(string details, Exception? inner = null) =>
        new("ACODE-DB-ACC-001", $"Database connection failed: {details}", isTransient: true, inner);

    /// <summary>Connection pool exhausted.</summary>
    /// <param name="timeout">Time waited before pool exhaustion.</param>
    /// <returns>DatabaseException with ACODE-DB-ACC-002 error code.</returns>
    public static DatabaseException PoolExhausted(TimeSpan timeout) =>
        new("ACODE-DB-ACC-002", $"Connection pool exhausted after {timeout.TotalSeconds}s wait", isTransient: true);

    /// <summary>Transaction failed to commit or rollback.</summary>
    /// <param name="operation">Operation that failed (commit/rollback).</param>
    /// <param name="inner">Inner exception.</param>
    /// <returns>DatabaseException with ACODE-DB-ACC-003 error code.</returns>
    public static DatabaseException TransactionFailed(string operation, Exception? inner = null) =>
        new("ACODE-DB-ACC-003", $"Transaction {operation} failed", isTransient: false, inner);

    /// <summary>Command execution timed out.</summary>
    /// <param name="timeout">Timeout duration.</param>
    /// <param name="command">Command that timed out.</param>
    /// <returns>DatabaseException with ACODE-DB-ACC-004 error code.</returns>
    public static DatabaseException CommandTimeout(TimeSpan timeout, string command) =>
        new("ACODE-DB-ACC-004", $"Command timed out after {timeout.TotalSeconds}s", isTransient: true);

    /// <summary>Constraint violation (unique, foreign key, check).</summary>
    /// <param name="constraint">Constraint name.</param>
    /// <param name="inner">Inner exception.</param>
    /// <returns>DatabaseException with ACODE-DB-ACC-005 error code.</returns>
    public static DatabaseException ConstraintViolation(string constraint, Exception? inner = null) =>
        new("ACODE-DB-ACC-005", $"Constraint violation: {constraint}", isTransient: false, inner);

    /// <summary>SQL syntax error.</summary>
    /// <param name="details">Syntax error details.</param>
    /// <param name="inner">Inner exception.</param>
    /// <returns>DatabaseException with ACODE-DB-ACC-006 error code.</returns>
    public static DatabaseException SyntaxError(string details, Exception? inner = null) =>
        new("ACODE-DB-ACC-006", $"SQL syntax error: {details}", isTransient: false, inner);

    /// <summary>Permission denied.</summary>
    /// <param name="operation">Operation that was denied.</param>
    /// <param name="inner">Inner exception.</param>
    /// <returns>DatabaseException with ACODE-DB-ACC-007 error code.</returns>
    public static DatabaseException PermissionDenied(string operation, Exception? inner = null) =>
        new("ACODE-DB-ACC-007", $"Permission denied for: {operation}", isTransient: false, inner);

    /// <summary>Database does not exist.</summary>
    /// <param name="database">Database name.</param>
    /// <param name="inner">Inner exception.</param>
    /// <returns>DatabaseException with ACODE-DB-ACC-008 error code.</returns>
    public static DatabaseException DatabaseNotFound(string database, Exception? inner = null) =>
        new("ACODE-DB-ACC-008", $"Database not found: {database}", isTransient: false, inner);
}
