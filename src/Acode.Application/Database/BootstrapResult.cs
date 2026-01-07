// src/Acode.Application/Database/BootstrapResult.cs
namespace Acode.Application.Database;

/// <summary>
/// Result of database bootstrap operation.
/// </summary>
public sealed record BootstrapResult
{
    /// <summary>
    /// Gets a value indicating whether the bootstrap operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the number of pending migrations detected.
    /// </summary>
    public required int PendingMigrationsCount { get; init; }

    /// <summary>
    /// Gets the number of migrations applied during bootstrap.
    /// </summary>
    public required int AppliedMigrationsCount { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the exception if the operation failed.
    /// </summary>
    public Exception? Exception { get; init; }
}
