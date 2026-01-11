using System.Text.RegularExpressions;
using Acode.Domain.Database;

namespace Acode.Infrastructure.Database.Layout;

/// <summary>
/// Validates database naming conventions.
/// Ensures tables, columns, indexes follow established patterns.
/// </summary>
public sealed partial class NamingConventionValidator
{
    private static readonly HashSet<string> ValidPrefixes = new(StringComparer.Ordinal)
    {
        "conv_", // Conversation domain
        "sess_", // Session domain
        "appr_", // Approval domain
        "sync_", // Synchronization domain
        "sys_", // System domain
        "__" // Reserved/internal
    };

    /// <summary>
    /// Validates a table name against naming conventions.
    /// </summary>
    /// <param name="tableName">The table name to validate.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    public ValidationResult ValidateTableName(string tableName)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(tableName))
        {
            return ValidationResult.Failure("Table name cannot be empty");
        }

        // Check prefix first (special case for "__" prefix)
        var hasValidPrefix = ValidPrefixes.Any(p => tableName.StartsWith(p, StringComparison.Ordinal));
        if (!hasValidPrefix)
        {
            errors.Add($"Table name '{tableName}' must have a valid domain prefix: {string.Join(", ", ValidPrefixes)}");
        }

        // Check snake_case (skip if starts with "__" since pattern requires [a-z] start)
        if (!tableName.StartsWith("__", StringComparison.Ordinal) && !SnakeCasePattern().IsMatch(tableName))
        {
            errors.Add($"Table name '{tableName}' must use snake_case (lowercase letters, numbers, underscores)");
        }

        // Check length (PostgreSQL limit)
        if (tableName.Length > 63)
        {
            errors.Add($"Table name '{tableName}' exceeds 63 character PostgreSQL limit");
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }

    /// <summary>
    /// Validates a column name against naming conventions.
    /// </summary>
    /// <param name="columnName">The column name to validate.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    public ValidationResult ValidateColumnName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return ValidationResult.Failure("Column name cannot be empty");
        }

        if (!SnakeCasePattern().IsMatch(columnName))
        {
            return ValidationResult.Failure(
                $"Column name '{columnName}' must use snake_case (lowercase letters, numbers, underscores)");
        }

        if (columnName.Length > 63)
        {
            return ValidationResult.Failure(
                $"Column name '{columnName}' exceeds 63 character PostgreSQL limit");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates an index name against naming conventions.
    /// </summary>
    /// <param name="indexName">The index name to validate.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    public ValidationResult ValidateIndexName(string indexName)
    {
        if (string.IsNullOrWhiteSpace(indexName))
        {
            return ValidationResult.Failure("Index name cannot be empty");
        }

        var isValidIndex = IndexPattern().IsMatch(indexName);
        var isValidUnique = UniqueIndexPattern().IsMatch(indexName);
        var isValidFk = ForeignKeyPattern().IsMatch(indexName);

        if (!isValidIndex && !isValidUnique && !isValidFk)
        {
            return ValidationResult.Failure(
                $"Index name '{indexName}' must follow pattern: idx_{{table}}_{{columns}}, ux_{{table}}_{{columns}}, or fk_{{table}}_{{ref}}");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a table has a correctly named primary key.
    /// </summary>
    /// <param name="table">The table schema to validate.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    public ValidationResult ValidatePrimaryKey(TableSchema table)
    {
        ArgumentNullException.ThrowIfNull(table);

        var pk = table.Columns.FirstOrDefault(c => c.IsPrimaryKey);

        if (pk == null)
        {
            return ValidationResult.Failure($"Table '{table.Name}' must have a primary key");
        }

        if (pk.Name != "id")
        {
            return ValidationResult.Failure(
                $"Table '{table.Name}' primary key must be named 'id', found '{pk.Name}'");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates a foreign key column name against naming conventions.
    /// </summary>
    /// <param name="columnName">The foreign key column name.</param>
    /// <param name="referencedTable">The table this column references.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    public ValidationResult ValidateForeignKeyColumn(string columnName, string referencedTable)
    {
        ArgumentNullException.ThrowIfNull(columnName);
        ArgumentNullException.ThrowIfNull(referencedTable);

        // FK column should be named {table}_id (without prefix)
        var tableWithoutPrefix = referencedTable.Substring(referencedTable.IndexOf('_', StringComparison.Ordinal) + 1);
        var expectedName = tableWithoutPrefix + "_id";

        // Also allow just removing the 's' for plurals (chats -> chat_id)
        var singularName = tableWithoutPrefix.TrimEnd('s') + "_id";

        if (columnName != expectedName && columnName != singularName && !columnName.EndsWith("_id", StringComparison.Ordinal))
        {
            return ValidationResult.Failure(
                $"Foreign key column '{columnName}' referencing '{referencedTable}' should follow {{table}}_id pattern");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates a timestamp column name against naming conventions.
    /// </summary>
    /// <param name="columnName">The column name to validate.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    public ValidationResult ValidateTimestampColumn(string columnName)
    {
        ArgumentNullException.ThrowIfNull(columnName);

        if (!columnName.EndsWith("_at", StringComparison.Ordinal))
        {
            return ValidationResult.Failure(
                $"Timestamp column '{columnName}' should follow {{action}}_at pattern (e.g., created_at, updated_at)");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates a boolean column name against naming conventions.
    /// </summary>
    /// <param name="columnName">The column name to validate.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    public ValidationResult ValidateBooleanColumn(string columnName)
    {
        ArgumentNullException.ThrowIfNull(columnName);

        if (!columnName.StartsWith("is_", StringComparison.Ordinal))
        {
            return ValidationResult.Failure(
                $"Boolean column '{columnName}' should follow is_{{condition}} pattern (e.g., is_deleted, is_active)");
        }

        return ValidationResult.Success();
    }

    [GeneratedRegex(@"^[a-z][a-z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex SnakeCasePattern();

    [GeneratedRegex(@"^idx_[a-z][a-z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex IndexPattern();

    [GeneratedRegex(@"^ux_[a-z][a-z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex UniqueIndexPattern();

    [GeneratedRegex(@"^fk_[a-z][a-z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex ForeignKeyPattern();
}
