using Acode.Domain.Database;

namespace Acode.Infrastructure.Database.Layout;

/// <summary>
/// Validates database column data types.
/// Ensures correct types for IDs, timestamps, booleans, etc.
/// </summary>
public sealed class DataTypeValidator
{
    private static readonly HashSet<string> ValidIdTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "TEXT", "VARCHAR", "VARCHAR(26)"
    };

    private static readonly HashSet<string> ValidTimestampTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "TEXT"
    };

    private static readonly HashSet<string> ValidBooleanTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "INTEGER", "INT"
    };

    private static readonly HashSet<string> ValidJsonTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "TEXT", "JSONB"
    };

    /// <summary>
    /// Validates that an ID column uses the correct data type.
    /// </summary>
    /// <param name="column">The column to validate.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    public ValidationResult ValidateIdColumn(ColumnSchema column)
    {
        ArgumentNullException.ThrowIfNull(column);

        if (!ValidIdTypes.Contains(column.DataType))
        {
            return ValidationResult.Failure(
                $"ID column '{column.Name}' must use TEXT type for ULID format, found '{column.DataType}'");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a timestamp column uses the correct data type.
    /// </summary>
    /// <param name="column">The column to validate.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    public ValidationResult ValidateTimestampColumn(ColumnSchema column)
    {
        ArgumentNullException.ThrowIfNull(column);

        if (!ValidTimestampTypes.Contains(column.DataType))
        {
            var result = ValidationResult.WithWarnings(
                $"Timestamp column '{column.Name}' should use TEXT type for ISO 8601 format, found '{column.DataType}'");

            return result;
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a boolean column uses the correct data type.
    /// </summary>
    /// <param name="column">The column to validate.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    public ValidationResult ValidateBooleanColumn(ColumnSchema column)
    {
        ArgumentNullException.ThrowIfNull(column);

        if (!ValidBooleanTypes.Contains(column.DataType))
        {
            return ValidationResult.Failure(
                $"Boolean column '{column.Name}' must use INTEGER type (0/1) for SQLite compatibility, found '{column.DataType}'");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a JSON column uses the correct data type.
    /// </summary>
    /// <param name="column">The column to validate.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    public ValidationResult ValidateJsonColumn(ColumnSchema column)
    {
        ArgumentNullException.ThrowIfNull(column);

        if (!ValidJsonTypes.Contains(column.DataType))
        {
            return ValidationResult.Failure(
                $"JSON column '{column.Name}' must use TEXT (SQLite) or JSONB (PostgreSQL) type, found '{column.DataType}'");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a foreign key column uses the correct data type.
    /// </summary>
    /// <param name="column">The column to validate.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    public ValidationResult ValidateForeignKeyColumn(ColumnSchema column)
    {
        ArgumentNullException.ThrowIfNull(column);

        // FK must match PK type (TEXT for ULID)
        if (!ValidIdTypes.Contains(column.DataType))
        {
            return ValidationResult.Failure(
                $"Foreign key column '{column.Name}' must use TEXT type to match ULID primary keys, found '{column.DataType}'");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that an enum column uses the correct data type.
    /// </summary>
    /// <param name="column">The column to validate.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    public ValidationResult ValidateEnumColumn(ColumnSchema column)
    {
        ArgumentNullException.ThrowIfNull(column);

        if (!column.DataType.Equals("TEXT", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.WithWarnings(
                $"Enum column '{column.Name}' should use TEXT for enum portability, found '{column.DataType}'");
        }

        return ValidationResult.Success();
    }
}
