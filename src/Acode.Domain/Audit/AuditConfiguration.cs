namespace Acode.Domain.Audit;

/// <summary>
/// Configuration settings for audit behavior.
/// Controls retention, rotation, and output settings.
/// </summary>
public sealed record AuditConfiguration
{
    /// <summary>
    /// Gets a value indicating whether audit logging is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the minimum severity level to log.
    /// Events below this level are not logged.
    /// </summary>
    public AuditSeverity LogLevel { get; init; } = AuditSeverity.Info;

    /// <summary>
    /// Gets the directory where audit logs are stored.
    /// </summary>
    public string LogDirectory { get; init; } = ".acode/logs";

    /// <summary>
    /// Gets the number of days to retain audit logs.
    /// Logs older than this are eligible for cleanup.
    /// </summary>
    public int RetentionDays { get; init; } = 90;

    /// <summary>
    /// Gets the maximum size in MB before log rotation.
    /// </summary>
    public int RotationSizeMb { get; init; } = 10;

    /// <summary>
    /// Gets the rotation interval.
    /// </summary>
    public RotationInterval RotationInterval { get; init; } = RotationInterval.Daily;

    /// <summary>
    /// Gets the supported export formats.
    /// </summary>
    public IReadOnlyList<ExportFormat> ExportFormats { get; init; } =
        new List<ExportFormat> { ExportFormat.Json, ExportFormat.Csv, ExportFormat.Text }.AsReadOnly();
}
