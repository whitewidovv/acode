using System.Text.RegularExpressions;
using Acode.Domain.Database;

namespace Acode.Infrastructure.Database.Layout;

/// <summary>
/// Validates migration file structure and content.
/// Ensures migration files follow naming conventions and contain safe SQL.
/// </summary>
public sealed partial class MigrationFileValidator
{
    private static readonly (string Pattern, string Description)[] ForbiddenPatterns = new[]
    {
        (@"\bDROP\s+DATABASE\b", "DROP DATABASE"),
        (@"\bTRUNCATE\s+TABLE\b", "TRUNCATE TABLE"),
        (@"\bGRANT\b", "GRANT"),
        (@"\bREVOKE\b", "REVOKE"),
        (@"\bCREATE\s+USER\b", "CREATE USER"),
        (@"\bALTER\s+USER\b", "ALTER USER"),
        (@"\bload_extension\b", "load_extension")
    };

    /// <summary>
    /// Validates that a migration filename follows the required pattern.
    /// </summary>
    /// <param name="fileName">The migration filename to validate.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    public ValidationResult ValidateFileName(string fileName)
    {
        var match = FileNamePattern().Match(Path.GetFileName(fileName));

        if (!match.Success)
        {
            return ValidationResult.Failure(
                $"Migration filename '{fileName}' must match pattern NNN_description.sql (e.g., 001_initial_schema.sql)");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a migration has a corresponding rollback script.
    /// </summary>
    /// <param name="upFilePath">The path to the up migration file.</param>
    /// <param name="downFileExists">Whether the corresponding down file exists.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    public ValidationResult ValidateDownScriptExists(string upFilePath, bool downFileExists)
    {
        ArgumentNullException.ThrowIfNull(upFilePath);

        if (!downFileExists)
        {
            var expectedDown = upFilePath.Replace(".sql", "_down.sql", StringComparison.Ordinal);
            return ValidationResult.Failure(
                $"Migration '{Path.GetFileName(upFilePath)}' is missing rollback script: {Path.GetFileName(expectedDown)}");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates migration file content for safety and best practices.
    /// </summary>
    /// <param name="content">The migration file content to validate.</param>
    /// <returns>Validation result with errors for forbidden patterns and warnings for best practices.</returns>
    public ValidationResult ValidateContent(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var errors = new List<string>();
        var warnings = new List<string>();

        // Check for header comment
        if (!content.TrimStart().StartsWith("--", StringComparison.Ordinal))
        {
            warnings.Add("Migration should start with a header comment explaining purpose and dependencies");
        }

        // Check for forbidden patterns
        foreach (var (pattern, description) in ForbiddenPatterns)
        {
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
            {
                errors.Add($"Forbidden SQL pattern detected: {description}");
            }
        }

        // Check for IF NOT EXISTS / IF EXISTS
        if (content.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase) &&
            !content.Contains("IF NOT EXISTS", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("CREATE TABLE statements should use IF NOT EXISTS for idempotency");
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    /// <summary>
    /// Extracts the version number from a migration filename.
    /// </summary>
    /// <param name="fileName">The migration filename.</param>
    /// <returns>The version number if valid, null otherwise.</returns>
    public int? ExtractVersion(string fileName)
    {
        var match = FileNamePattern().Match(Path.GetFileName(fileName));

        if (match.Success && int.TryParse(match.Groups[1].Value, out var version))
        {
            return version;
        }

        return null;
    }

    /// <summary>
    /// Validates that a set of migration files have sequential version numbers.
    /// </summary>
    /// <param name="migrationFiles">The migration filenames to validate.</param>
    /// <returns>Validation result indicating gaps in the sequence.</returns>
    public ValidationResult ValidateMigrationSequence(IEnumerable<string> migrationFiles)
    {
        var versions = migrationFiles
            .Select(f => ExtractVersion(f))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .OrderBy(v => v)
            .ToList();

        var errors = new List<string>();

        for (int i = 0; i < versions.Count; i++)
        {
            var expected = i + 1;
            if (versions[i] != expected)
            {
                errors.Add($"Migration sequence gap: expected version {expected:D3}, found {versions[i]:D3}");
            }
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }

    [GeneratedRegex(@"^(\d{3})_([a-z][a-z0-9_]*)\.sql$", RegexOptions.Compiled)]
    private static partial Regex FileNamePattern();

    [GeneratedRegex(@"^--\s*.*$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex HeaderCommentPattern();
}
