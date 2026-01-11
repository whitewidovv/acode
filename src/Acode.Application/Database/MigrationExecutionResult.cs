// src/Acode.Application/Database/MigrationExecutionResult.cs
namespace Acode.Application.Database;

/// <summary>
/// Result of executing a migration (apply or rollback).
/// </summary>
public sealed record MigrationExecutionResult
{
    /// <summary>
    /// Gets a value indicating whether the migration executed successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the migration version that was executed.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets the duration of the migration execution.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the exception that caused the failure, if any.
    /// </summary>
    public Exception? Exception { get; init; }
}
