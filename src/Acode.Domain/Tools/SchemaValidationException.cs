namespace Acode.Domain.Tools;

/// <summary>
/// Exception thrown when tool arguments fail JSON Schema validation.
/// </summary>
/// <remarks>
/// FR-007: Tool Schema Registry requires a dedicated exception type for validation failures.
/// This exception carries all validation errors to enable model retry with context.
/// </remarks>
public sealed class SchemaValidationException : Exception
{
    private readonly IReadOnlyList<SchemaValidationError> errors;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaValidationException"/> class.
    /// </summary>
    /// <param name="toolName">The name of the tool that failed validation.</param>
    /// <param name="errors">The validation errors.</param>
    /// <exception cref="ArgumentException">Thrown when toolName is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when errors is null.</exception>
    /// <exception cref="ArgumentException">Thrown when errors is empty.</exception>
    public SchemaValidationException(string toolName, IEnumerable<SchemaValidationError> errors)
        : this(toolName, errors, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaValidationException"/> class.
    /// </summary>
    /// <param name="toolName">The name of the tool that failed validation.</param>
    /// <param name="errors">The validation errors.</param>
    /// <param name="innerException">The inner exception, if any.</param>
    /// <exception cref="ArgumentException">Thrown when toolName is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when errors is null.</exception>
    /// <exception cref="ArgumentException">Thrown when errors is empty.</exception>
    public SchemaValidationException(
        string toolName,
        IEnumerable<SchemaValidationError> errors,
        Exception? innerException)
        : base(CreateMessage(toolName, errors), innerException)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException("Tool name must not be null or empty.", nameof(toolName));
        }

        ArgumentNullException.ThrowIfNull(errors);

        var errorList = errors.ToList();
        if (errorList.Count == 0)
        {
            throw new ArgumentException("At least one validation error is required.", nameof(errors));
        }

        this.ToolName = toolName;
        this.errors = errorList.AsReadOnly();
    }

    /// <summary>
    /// Gets the name of the tool that failed validation.
    /// </summary>
    public string ToolName { get; }

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyCollection<SchemaValidationError> Errors => this.errors;

    /// <summary>
    /// Gets the error code of the first validation error.
    /// </summary>
    public string ErrorCode => this.errors[0].Code;

    /// <summary>
    /// Gets a value indicating whether multiple validation errors occurred.
    /// </summary>
    public bool HasMultipleErrors => this.errors.Count > 1;

    /// <summary>
    /// Gets a formatted string containing all validation errors.
    /// </summary>
    /// <returns>A formatted error string for model consumption.</returns>
    public string GetFormattedErrors()
    {
        var lines = new List<string>();
        foreach (var error in this.errors)
        {
            lines.Add(error.ToString());
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string CreateMessage(string toolName, IEnumerable<SchemaValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var errorList = errors.ToList();
        var errorCount = errorList.Count;

        if (errorCount == 0)
        {
            return $"Validation failed for tool '{toolName}'.";
        }

        var errorWord = errorCount == 1 ? "error" : "errors";
        return $"Validation failed for tool '{toolName}' with {errorCount} {errorWord}.";
    }
}
