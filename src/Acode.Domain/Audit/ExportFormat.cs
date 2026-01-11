namespace Acode.Domain.Audit;

/// <summary>
/// Export format for audit logs.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// JSON format (array of events).
    /// </summary>
    Json,

    /// <summary>
    /// CSV format (comma-separated values).
    /// </summary>
    Csv,

    /// <summary>
    /// Plain text format.
    /// </summary>
    Text,
}
