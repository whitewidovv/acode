namespace Acode.Cli;

/// <summary>
/// Human-readable console output formatter.
/// </summary>
/// <remarks>
/// Formats output for terminal display with optional ANSI colors.
/// Respects --no-color flag and TTY detection.
/// FR-017: --no-color MUST disable colored output.
/// </remarks>
public sealed class ConsoleFormatter : IOutputFormatter
{
    private readonly TextWriter _output;
    private readonly bool _enableColors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleFormatter"/> class.
    /// </summary>
    /// <param name="output">Output writer (typically Console.Out).</param>
    /// <param name="enableColors">Whether to enable ANSI color codes.</param>
    public ConsoleFormatter(TextWriter output, bool enableColors)
    {
        ArgumentNullException.ThrowIfNull(output);
        _output = output;
        _enableColors = enableColors;
    }

    /// <inheritdoc/>
    public void WriteMessage(string message, MessageType type = MessageType.Info)
    {
        ArgumentNullException.ThrowIfNull(message);

        var prefix = type switch
        {
            MessageType.Success => "✓ ",
            MessageType.Warning => "⚠ ",
            MessageType.Error => "✗ ",
            MessageType.Debug => "[debug] ",
            _ => string.Empty,
        };

        _output.WriteLine($"{prefix}{message}");
    }

    /// <inheritdoc/>
    public void WriteHeading(string heading, int level = 1)
    {
        ArgumentNullException.ThrowIfNull(heading);

        WriteBlankLine();
        var underline = level == 1 ? new string('=', heading.Length) : new string('-', heading.Length);
        _output.WriteLine(heading);
        _output.WriteLine(underline);
        WriteBlankLine();
    }

    /// <inheritdoc/>
    public void WriteKeyValue(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        _output.WriteLine($"{key}: {value}");
    }

    /// <inheritdoc/>
    public void WriteList(IEnumerable<string> items, bool ordered = false)
    {
        ArgumentNullException.ThrowIfNull(items);

        var index = 1;
        foreach (var item in items)
        {
            var prefix = ordered ? $"{index}. " : "• ";
            _output.WriteLine($"{prefix}{item}");
            index++;
        }
    }

    /// <inheritdoc/>
    public void WriteTable(string[] headers, IEnumerable<string[]> rows)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);

        // Calculate column widths
        var columnWidths = headers.Select((h, i) =>
        {
            var headerWidth = h.Length;
            var maxRowWidth = rows.Select(r => r.Length > i ? r[i].Length : 0).DefaultIfEmpty(0).Max();
            return Math.Max(headerWidth, maxRowWidth);
        }).ToArray();

        // Write headers
        for (int i = 0; i < headers.Length; i++)
        {
            _output.Write(headers[i].PadRight(columnWidths[i] + 2));
        }

        _output.WriteLine();

        // Write separator
        for (int i = 0; i < headers.Length; i++)
        {
            _output.Write(new string('-', columnWidths[i] + 2));
        }

        _output.WriteLine();

        // Write rows
        foreach (var row in rows)
        {
            for (int i = 0; i < headers.Length && i < row.Length; i++)
            {
                _output.Write(row[i].PadRight(columnWidths[i] + 2));
            }

            _output.WriteLine();
        }
    }

    /// <inheritdoc/>
    public void WriteBlankLine()
    {
        _output.WriteLine();
    }

    /// <inheritdoc/>
    public void WriteSeparator()
    {
        _output.WriteLine(new string('-', 80));
    }
}
