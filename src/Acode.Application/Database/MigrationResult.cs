// src/Acode.Application/Database/MigrationResult.cs
namespace Acode.Application.Database;

/// <summary>
/// Result of migration operation.
/// </summary>
public sealed record MigrationResult
{
    /// <summary>
    /// Gets a value indicating whether the migration succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the number of migrations applied.
    /// </summary>
    public required int AppliedCount { get; init; }

    /// <summary>
    /// Gets the total duration of all migrations.
    /// </summary>
    public required TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the exception if the operation failed.
    /// </summary>
    public Exception? Exception { get; init; }
}
