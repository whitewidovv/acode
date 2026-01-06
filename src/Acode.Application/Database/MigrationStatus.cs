namespace Acode.Application.Database;

/// <summary>
/// Status of a migration execution.
/// </summary>
public enum MigrationStatus
{
    /// <summary>
    /// Migration was successfully applied to the database.
    /// </summary>
    Applied = 0,

    /// <summary>
    /// Migration was skipped (e.g., due to --skip flag or conditional logic).
    /// </summary>
    Skipped = 1,

    /// <summary>
    /// Migration execution failed and was rolled back.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Migration partially applied (some statements succeeded, transaction rolled back).
    /// </summary>
    Partial = 3,
}
