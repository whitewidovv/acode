namespace Acode.Infrastructure.Ollama.ToolCall.Models;

/// <summary>
/// Configuration for tool call retry behavior.
/// </summary>
public sealed class RetryConfig
{
    /// <summary>
    /// Gets the default retry prompt template.
    /// </summary>
    public const string DefaultRetryPromptTemplate = """
        The tool call arguments you provided contain invalid JSON.

        Error: {error_message}
        Position: {error_position}

        Your output:
        ```json
        {malformed_json}
        ```

        Please provide corrected JSON arguments for the '{tool_name}' tool.
        Do not include any explanation, only the corrected JSON object.
        """;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts before giving up.
    /// Default: 3. Range: 1-10.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets a value indicating whether to attempt automatic JSON repair before retrying.
    /// Default: true.
    /// </summary>
    public bool EnableAutoRepair { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout for repair attempts in milliseconds.
    /// Default: 100.
    /// </summary>
    public int RepairTimeoutMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets the base delay between retry attempts in milliseconds.
    /// Uses exponential backoff: delay * 2^attempt.
    /// Default: 100 (so: 100ms, 200ms, 400ms).
    /// </summary>
    public int RetryDelayMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether to enforce strict schema validation (no extra properties).
    /// Default: true.
    /// </summary>
    public bool StrictValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum JSON nesting depth.
    /// Default: 64.
    /// </summary>
    public int MaxNestingDepth { get; set; } = 64;

    /// <summary>
    /// Gets or sets the maximum argument size in bytes.
    /// Default: 1MB.
    /// </summary>
    public int MaxArgumentSize { get; set; } = 1_048_576;

    /// <summary>
    /// Gets or sets the template for retry prompts.
    /// Supports placeholders: {error_message}, {error_position}, {malformed_json}, {tool_name}, {schema_example}.
    /// </summary>
    public string RetryPromptTemplate { get; set; } = DefaultRetryPromptTemplate;
}
