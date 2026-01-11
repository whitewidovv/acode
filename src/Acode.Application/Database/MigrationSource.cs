namespace Acode.Application.Database;

/// <summary>
/// Identifies the source of a migration file.
/// </summary>
public enum MigrationSource
{
    /// <summary>
    /// Migration is embedded in the assembly as a resource.
    /// </summary>
    Embedded = 0,

    /// <summary>
    /// Migration is located in the file system (.agent/migrations/).
    /// </summary>
    File = 1,
}
