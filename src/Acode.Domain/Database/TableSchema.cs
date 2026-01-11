namespace Acode.Domain.Database;

/// <summary>
/// Represents a database table's schema definition.
/// </summary>
public sealed record TableSchema
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableSchema"/> class.
    /// </summary>
    /// <param name="name">The table name.</param>
    /// <param name="columns">The columns in the table.</param>
    /// <param name="indexes">The indexes on the table.</param>
    public TableSchema(
        string name,
        IEnumerable<ColumnSchema> columns,
        IEnumerable<string>? indexes = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Columns = columns?.ToList() ?? throw new ArgumentNullException(nameof(columns));
        Indexes = indexes?.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the list of columns in the table.
    /// </summary>
    public IReadOnlyList<ColumnSchema> Columns { get; init; }

    /// <summary>
    /// Gets the list of indexes on the table.
    /// </summary>
    public IReadOnlyList<string> Indexes { get; init; }

    /// <summary>
    /// Gets the primary key column, if any.
    /// </summary>
    public ColumnSchema? PrimaryKey => Columns.FirstOrDefault(c => c.IsPrimaryKey);

    /// <summary>
    /// Gets all foreign key columns in the table.
    /// </summary>
    public IEnumerable<ColumnSchema> ForeignKeys => Columns.Where(c => c.IsForeignKey);

    /// <summary>
    /// Gets the domain prefix of the table (e.g., "conv_", "sess_", "sys_", "__").
    /// </summary>
    public string DomainPrefix
    {
        get
        {
            // Handle special case of "__" prefix (internal tables like __migrations)
            if (Name.StartsWith("__", StringComparison.Ordinal))
            {
                return "__";
            }

            // Extract prefix up to first underscore
            var parts = Name.Split('_');
            return parts[0] + "_";
        }
    }
}
