// src/Acode.Application/Database/IMigrationValidator.cs
namespace Acode.Application.Database;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Validates discovered migrations against applied migration history.
/// </summary>
public interface IMigrationValidator
{
    /// <summary>
    /// Validates discovered migrations against applied migrations.
    /// </summary>
    /// <param name="discovered">Discovered migrations.</param>
    /// <param name="applied">Applied migrations from database.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<ValidationResult> ValidateAsync(
        IReadOnlyList<MigrationFile> discovered,
        IReadOnlyList<AppliedMigration> applied,
        CancellationToken cancellationToken = default);
}
