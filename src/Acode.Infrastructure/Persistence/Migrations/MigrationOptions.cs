// src/Acode.Infrastructure/Persistence/Migrations/MigrationOptions.cs
namespace Acode.Infrastructure.Persistence.Migrations;

/// <summary>
/// Configuration options for migration discovery and execution.
/// </summary>
public sealed class MigrationOptions
{
    /// <summary>
    /// Gets or sets the directory where file-based migrations are stored.
    /// </summary>
    public string Directory { get; set; } = ".agent/migrations";
}
