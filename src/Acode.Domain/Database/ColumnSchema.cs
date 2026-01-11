namespace Acode.Domain.Database;

/// <summary>
/// Represents a database column's schema definition.
/// </summary>
public sealed record ColumnSchema
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnSchema"/> class.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <param name="dataType">The column data type.</param>
    /// <param name="isNullable">Whether the column allows null values. Default is true.</param>
    /// <param name="isPrimaryKey">Whether the column is a primary key. Default is false.</param>
    /// <param name="isForeignKey">Whether the column is a foreign key. Default is false.</param>
    /// <param name="foreignKeyTable">The table this foreign key references, if applicable.</param>
    /// <param name="defaultValue">The default value for the column, if any.</param>
    public ColumnSchema(
        string name,
        string dataType,
        bool isNullable = true,
        bool isPrimaryKey = false,
        bool isForeignKey = false,
        string? foreignKeyTable = null,
        string? defaultValue = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        IsNullable = isNullable;
        IsPrimaryKey = isPrimaryKey;
        IsForeignKey = isForeignKey;
        ForeignKeyTable = foreignKeyTable;
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Gets the column name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the column data type.
    /// </summary>
    public string DataType { get; init; }

    /// <summary>
    /// Gets a value indicating whether the column allows null values.
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// Gets a value indicating whether the column is a primary key.
    /// </summary>
    public bool IsPrimaryKey { get; init; }

    /// <summary>
    /// Gets a value indicating whether the column is a foreign key.
    /// </summary>
    public bool IsForeignKey { get; init; }

    /// <summary>
    /// Gets the name of the table this foreign key references, if applicable.
    /// </summary>
    public string? ForeignKeyTable { get; init; }

    /// <summary>
    /// Gets the default value for the column, if any.
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Gets a value indicating whether this column is a timestamp column (ends with "_at").
    /// </summary>
    public bool IsTimestamp => Name.EndsWith("_at", StringComparison.Ordinal);

    /// <summary>
    /// Gets a value indicating whether this column is a boolean column (starts with "is_").
    /// </summary>
    public bool IsBoolean => Name.StartsWith("is_", StringComparison.Ordinal);

    /// <summary>
    /// Gets a value indicating whether this column is an ID column (equals "id" or ends with "_id").
    /// </summary>
    public bool IsId => Name == "id" || Name.EndsWith("_id", StringComparison.Ordinal);
}
