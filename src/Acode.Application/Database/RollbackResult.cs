// src/Acode.Application/Database/RollbackResult.cs
namespace Acode.Application.Database;

/// <summary>
/// Result of a rollback operation.
/// </summary>
public sealed record RollbackResult
{
    /// <summary>
    /// Gets a value indicating whether the rollback operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the number of migrations that were rolled back.
    /// </summary>
    public required int RolledBackCount { get; init; }

    /// <summary>
    /// Gets the total duration of the rollback operation.
    /// </summary>
    public required TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Gets the current version after rollback (optional).
    /// </summary>
    public string? CurrentVersion { get; init; }

    /// <summary>
    /// Gets the list of versions that were rolled back (optional).
    /// </summary>
    public IReadOnlyList<string>? RolledBackVersions { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed (optional).
    /// </summary>
    public string? ErrorMessage { get; init; }
}
