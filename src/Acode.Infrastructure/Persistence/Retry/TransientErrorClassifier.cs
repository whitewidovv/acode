// src/Acode.Infrastructure/Persistence/Retry/TransientErrorClassifier.cs
namespace Acode.Infrastructure.Persistence.Retry;

using Acode.Domain.Exceptions;
using Microsoft.Data.Sqlite;
using Npgsql;

/// <summary>
/// Classifies database exceptions as transient or permanent.
/// Transient errors can be retried, permanent errors should not be retried.
/// </summary>
internal static class TransientErrorClassifier
{
    /// <summary>
    /// Determines if an exception represents a transient error that can be retried.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns>True if the error is transient and can be retried; false otherwise.</returns>
    public static bool IsTransient(Exception exception)
    {
        return exception switch
        {
            DatabaseException dbEx => dbEx.IsTransient,
            SqliteException sqliteEx => IsTransientSqliteError(sqliteEx),
            NpgsqlException npgsqlEx => IsTransientNpgsqlError(npgsqlEx),
            TimeoutException => true,
            _ => false
        };
    }

    private static bool IsTransientSqliteError(SqliteException exception)
    {
        // SQLite transient errors:
        // SQLITE_BUSY (5) - database is locked
        // SQLITE_LOCKED (6) - table is locked
        // SQLITE_IOERR (10) - disk I/O error (may be transient)
        // SQLITE_PROTOCOL (15) - locking protocol error
        // SQLITE_FULL (13) - disk full (transient if space becomes available)
        return exception.SqliteErrorCode is
            5 or // SQLITE_BUSY
            6 or // SQLITE_LOCKED
            10 or // SQLITE_IOERR
            13 or // SQLITE_FULL
            15; // SQLITE_PROTOCOL
    }

    private static bool IsTransientNpgsqlError(NpgsqlException exception)
    {
        // PostgreSQL transient errors:
        // 08000 - connection_exception
        // 08003 - connection_does_not_exist
        // 08006 - connection_failure
        // 40001 - serialization_failure (deadlock)
        // 40P01 - deadlock_detected
        // 53300 - too_many_connections
        // 57P03 - cannot_connect_now (server shutting down)
        var sqlState = exception.SqlState;

        return sqlState is
            "08000" or // connection_exception
            "08003" or // connection_does_not_exist
            "08006" or // connection_failure
            "40001" or // serialization_failure
            "40P01" or // deadlock_detected
            "53300" or // too_many_connections
            "57P03" or // cannot_connect_now
            "55P03"; // lock_not_available
    }
}
