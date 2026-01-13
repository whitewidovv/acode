namespace Acode.Infrastructure.Audit;

using System;
using Acode.Domain.Audit;

/// <summary>
/// Options for audit log export.
/// </summary>
public sealed record AuditExportOptions
{
    /// <summary>
    /// Gets the export format (JSON, CSV, or Text).
    /// </summary>
    public ExportFormat Format { get; init; } = ExportFormat.Json;

    /// <summary>
    /// Gets the start date for filtering (inclusive).
    /// </summary>
    public DateTimeOffset? FromDate { get; init; }

    /// <summary>
    /// Gets the end date for filtering (inclusive).
    /// </summary>
    public DateTimeOffset? ToDate { get; init; }

    /// <summary>
    /// Gets the session ID filter.
    /// </summary>
    public SessionId? SessionId { get; init; }

    /// <summary>
    /// Gets the event type filter.
    /// </summary>
    public AuditEventType? EventType { get; init; }

    /// <summary>
    /// Gets the minimum severity level filter.
    /// </summary>
    public AuditSeverity? MinSeverity { get; init; }

    /// <summary>
    /// Gets a value indicating whether to redact sensitive data.
    /// </summary>
    public bool RedactSensitiveData { get; init; } = true;
}
