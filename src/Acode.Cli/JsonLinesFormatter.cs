using System.Text.Json;
using Acode.Infrastructure.Common;

namespace Acode.Cli;

/// <summary>
/// JSONL (JSON Lines) output formatter for machine-parseable output.
/// </summary>
/// <remarks>
/// Outputs newline-delimited JSON records, one per line.
/// Each record has: type, timestamp (optional), and type-specific data fields.
/// Used with --json flag for automation and scripting.
/// FR-014: --json MUST enable JSONL output.
/// </remarks>
public sealed class JsonLinesFormatter : IOutputFormatter
{
    private readonly TextWriter _output;
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonLinesFormatter"/> class.
    /// </summary>
    /// <param name="output">Output writer (typically Console.Out).</param>
    public JsonLinesFormatter(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        _output = output;
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
            WriteIndented = false, // JSONL must be compact (one line per record)
        };
    }

    /// <inheritdoc/>
    public void WriteMessage(string message, MessageType type = MessageType.Info)
    {
        ArgumentNullException.ThrowIfNull(message);

        var record = new
        {
            type = "message",
            level = type.ToString().ToLowerInvariant(),
            message,
        };

        WriteJsonLine(record);
    }

    /// <inheritdoc/>
    public void WriteHeading(string heading, int level = 1)
    {
        ArgumentNullException.ThrowIfNull(heading);

        var record = new
        {
            type = "heading",
            text = heading,
            level,
        };

        WriteJsonLine(record);
    }

    /// <inheritdoc/>
    public void WriteKeyValue(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        var record = new
        {
            type = "key_value",
            key,
            value,
        };

        WriteJsonLine(record);
    }

    /// <inheritdoc/>
    public void WriteList(IEnumerable<string> items, bool ordered = false)
    {
        ArgumentNullException.ThrowIfNull(items);

        var record = new
        {
            type = "list",
            items = items.ToArray(),
            ordered,
        };

        WriteJsonLine(record);
    }

    /// <inheritdoc/>
    public void WriteTable(string[] headers, IEnumerable<string[]> rows)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);

        var record = new
        {
            type = "table",
            headers,
            rows = rows.ToArray(),
        };

        WriteJsonLine(record);
    }

    /// <inheritdoc/>
    public void WriteBlankLine()
    {
        var record = new
        {
            type = "blank_line",
        };

        WriteJsonLine(record);
    }

    /// <inheritdoc/>
    public void WriteSeparator()
    {
        var record = new
        {
            type = "separator",
        };

        WriteJsonLine(record);
    }

    private void WriteJsonLine(object record)
    {
        var json = JsonSerializer.Serialize(record, _options);
        _output.WriteLine(json);
    }
}
