// src/Acode.Application/Database/CreateOptions.cs
namespace Acode.Application.Database;

/// <summary>
/// Options for creating a new migration.
/// </summary>
public sealed record CreateOptions
{
    /// <summary>
    /// Gets the name of the migration to create.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the template to use for the migration (optional).
    /// </summary>
    /// <remarks>
    /// Supported templates: TABLE, INDEX, COLUMN, etc.
    /// </remarks>
    public string? Template { get; init; }

    /// <summary>
    /// Gets a value indicating whether to skip creating a down script.
    /// </summary>
    public bool NoDown { get; init; } = false;
}
