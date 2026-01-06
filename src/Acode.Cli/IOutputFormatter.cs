namespace Acode.Cli;

/// <summary>
/// Defines the contract for formatting command output.
/// </summary>
/// <remarks>
/// Output formatters decouple command logic from presentation.
/// Two main implementations:
/// - ConsoleFormatter: Human-readable output with colors and formatting.
/// - JsonLinesFormatter: Machine-parseable JSONL output for automation.
///
/// FR-014: --json MUST enable JSONL output.
/// FR-017: --no-color MUST disable colored output.
/// </remarks>
public interface IOutputFormatter
{
    /// <summary>
    /// Writes a message to output.
    /// </summary>
    /// <param name="message">The message text to write.</param>
    /// <param name="type">The type of message (info, warning, error, etc.).</param>
    void WriteMessage(string message, MessageType type = MessageType.Info);

    /// <summary>
    /// Writes a heading to output.
    /// </summary>
    /// <param name="heading">The heading text.</param>
    /// <param name="level">Heading level (1 = top-level, 2 = sub-heading, etc.).</param>
    void WriteHeading(string heading, int level = 1);

    /// <summary>
    /// Writes a key-value pair to output.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <param name="value">The value to display.</param>
    void WriteKeyValue(string key, string value);

    /// <summary>
    /// Writes a list of items to output.
    /// </summary>
    /// <param name="items">The list items to write.</param>
    /// <param name="ordered">Whether to use ordered (numbered) list.</param>
    void WriteList(IEnumerable<string> items, bool ordered = false);

    /// <summary>
    /// Writes a table to output.
    /// </summary>
    /// <param name="headers">Column headers.</param>
    /// <param name="rows">Table rows (each row is an array of cell values).</param>
    void WriteTable(string[] headers, IEnumerable<string[]> rows);

    /// <summary>
    /// Writes a blank line to output.
    /// </summary>
    void WriteBlankLine();

    /// <summary>
    /// Writes a horizontal separator line.
    /// </summary>
    void WriteSeparator();
}
