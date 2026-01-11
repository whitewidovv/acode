// src/Acode.Application/Database/CreateResult.cs
namespace Acode.Application.Database;

/// <summary>
/// Result of creating a new migration.
/// </summary>
public sealed record CreateResult
{
    /// <summary>
    /// Gets a value indicating whether the creation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the version assigned to the new migration.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets the file path of the created up script.
    /// </summary>
    public required string UpFilePath { get; init; }

    /// <summary>
    /// Gets the file path of the created down script.
    /// </summary>
    public required string DownFilePath { get; init; }
}
