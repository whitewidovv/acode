namespace Acode.Infrastructure.Audit;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Acode.Domain.Audit;

/// <summary>
/// Exports audit logs to various formats (JSON, CSV, Text).
/// Supports filtering by date, session, event type, and severity.
/// </summary>
public sealed class AuditExporter
{
    private static readonly JsonSerializerOptions ExportJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    private readonly AuditRedactor _redactor = new();

    /// <summary>
    /// Exports audit events from a JSONL file to the specified format.
    /// </summary>
    /// <param name="logPath">Path to the JSONL audit log file.</param>
    /// <param name="exportPath">Path for the exported file.</param>
    /// <param name="options">Export options (format, filters, redaction).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExportAsync(
        string logPath,
        string exportPath,
        AuditExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(logPath);
        ArgumentNullException.ThrowIfNull(exportPath);
        ArgumentNullException.ThrowIfNull(options);

        if (!File.Exists(logPath))
        {
            throw new FileNotFoundException($"Log file not found: {logPath}");
        }

        // Read and parse events
        var events = await ReadEventsAsync(logPath).ConfigureAwait(false);

        // Apply filters
        events = ApplyFilters(events, options);

        // Apply redaction if requested
        if (options.RedactSensitiveData)
        {
            events = events.Select(RedactEvent).ToList();
        }

        // Export to requested format
        await ExportToFormatAsync(events, exportPath, options.Format).ConfigureAwait(false);
    }

    private static async Task<List<AuditEvent>> ReadEventsAsync(string logPath)
    {
        var events = new List<AuditEvent>();
        var lines = await File.ReadAllLinesAsync(logPath).ConfigureAwait(false);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var evt = JsonSerializer.Deserialize<AuditEvent>(line);
                if (evt != null)
                {
                    events.Add(evt);
                }
            }
            catch (JsonException)
            {
                // Skip malformed lines
                continue;
            }
        }

        return events;
    }

    private static List<AuditEvent> ApplyFilters(
        List<AuditEvent> events,
        AuditExportOptions options)
    {
        var filtered = events.AsEnumerable();

        // Date range filter
        if (options.FromDate.HasValue)
        {
            filtered = filtered.Where(e => e.Timestamp >= options.FromDate.Value);
        }

        if (options.ToDate.HasValue)
        {
            filtered = filtered.Where(e => e.Timestamp <= options.ToDate.Value);
        }

        // Session filter
        if (options.SessionId != null)
        {
            filtered = filtered.Where(e => e.SessionId.Value == options.SessionId.Value);
        }

        // Event type filter
        if (options.EventType.HasValue)
        {
            filtered = filtered.Where(e => e.EventType == options.EventType.Value);
        }

        // Severity filter
        if (options.MinSeverity.HasValue)
        {
            filtered = filtered.Where(e => e.Severity >= options.MinSeverity.Value);
        }

        return filtered.ToList();
    }

    private static async Task ExportToFormatAsync(
        List<AuditEvent> events,
        string exportPath,
        ExportFormat format)
    {
        switch (format)
        {
            case ExportFormat.Json:
                await ExportToJsonAsync(events, exportPath).ConfigureAwait(false);
                break;

            case ExportFormat.Csv:
                await ExportToCsvAsync(events, exportPath).ConfigureAwait(false);
                break;

            case ExportFormat.Text:
                await ExportToTextAsync(events, exportPath).ConfigureAwait(false);
                break;

            default:
                throw new ArgumentException($"Unsupported format: {format}");
        }
    }

    private static async Task ExportToJsonAsync(List<AuditEvent> events, string exportPath)
    {
        var json = JsonSerializer.Serialize(events, ExportJsonOptions);
        await File.WriteAllTextAsync(exportPath, json).ConfigureAwait(false);
    }

    private static async Task ExportToCsvAsync(List<AuditEvent> events, string exportPath)
    {
        var sb = new StringBuilder();

        // CSV Header
        sb.AppendLine("EventId,Timestamp,SessionId,CorrelationId,EventType,Severity,Source,OperatingMode");

        // CSV Rows
        foreach (var evt in events)
        {
            sb.AppendLine(
                $"{CsvEscape(evt.EventId.Value)}," +
                $"{CsvEscape(evt.Timestamp.ToString("o", CultureInfo.InvariantCulture))}," +
                $"{CsvEscape(evt.SessionId.Value)}," +
                $"{CsvEscape(evt.CorrelationId.Value)}," +
                $"{CsvEscape(evt.EventType.ToString())}," +
                $"{CsvEscape(evt.Severity.ToString())}," +
                $"{CsvEscape(evt.Source)}," +
                $"{CsvEscape(evt.OperatingMode)}");
        }

        await File.WriteAllTextAsync(exportPath, sb.ToString()).ConfigureAwait(false);
    }

    private static async Task ExportToTextAsync(List<AuditEvent> events, string exportPath)
    {
        var sb = new StringBuilder();

        foreach (var evt in events)
        {
            sb.AppendLine("==================================================");
            sb.AppendLine($"EventId: {evt.EventId.Value}");
            sb.AppendLine($"Timestamp: {evt.Timestamp:o}");
            sb.AppendLine($"SessionId: {evt.SessionId.Value}");
            sb.AppendLine($"CorrelationId: {evt.CorrelationId.Value}");
            sb.AppendLine($"EventType: {evt.EventType}");
            sb.AppendLine($"Severity: {evt.Severity}");
            sb.AppendLine($"Source: {evt.Source}");
            sb.AppendLine($"OperatingMode: {evt.OperatingMode}");

            if (evt.Data.Any())
            {
                sb.AppendLine("Data:");
                foreach (var (key, value) in evt.Data)
                {
                    sb.AppendLine($"  {key}: {value}");
                }
            }

            sb.AppendLine();
        }

        await File.WriteAllTextAsync(exportPath, sb.ToString()).ConfigureAwait(false);
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',', StringComparison.Ordinal) ||
            value.Contains('"', StringComparison.Ordinal) ||
            value.Contains('\n', StringComparison.Ordinal))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }

    private AuditEvent RedactEvent(AuditEvent evt)
    {
        // Redact data dictionary
        var redactedData = _redactor.RedactData(evt.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

        // Redact context if present
        IDictionary<string, object>? redactedContext = null;
        if (evt.Context != null)
        {
            redactedContext = _redactor.RedactData(evt.Context.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        return evt with
        {
            Data = redactedData.AsReadOnly(),
            Context = redactedContext?.AsReadOnly(),
        };
    }
}
