using System.Text.Json;
using System.Text.Json.Serialization;
using Acode.Application.Audit;
using Acode.Domain.Audit;

namespace Acode.Infrastructure.Audit;

/// <summary>
/// Writes audit events to a JSON Lines file (one JSON object per line).
/// </summary>
public sealed class JsonAuditLogger : IAuditLogger, IDisposable
{
    private readonly string _logFilePath;
    private readonly StreamWriter _writer;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _writeLock;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonAuditLogger"/> class.
    /// </summary>
    /// <param name="logFilePath">Path to the audit log file.</param>
    public JsonAuditLogger(string logFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath, nameof(logFilePath));

        _logFilePath = logFilePath;

        // Ensure directory exists
        var directory = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(File.Open(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = false // Manual flush for better performance
        };

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false, // One line per event
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)
            }
        };

        _writeLock = new SemaphoreSlim(1, 1);
    }

    /// <inheritdoc/>
    public async Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var json = JsonSerializer.Serialize(auditEvent, _jsonOptions);
            await _writer.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task LogAsync(
        AuditEventType eventType,
        AuditSeverity severity,
        string source,
        IDictionary<string, object> data,
        IDictionary<string, object>? context = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement convenience method that constructs AuditEvent
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public IDisposable BeginCorrelation(string description)
    {
        // TODO: Implement correlation scope
        return new NoOpDisposable();
    }

    /// <inheritdoc/>
    public IDisposable BeginSpan(string operation)
    {
        // TODO: Implement span scope
        return new NoOpDisposable();
    }

    /// <summary>
    /// Disposes the logger and flushes remaining data.
    /// </summary>
    public void Dispose()
    {
        _writer.Dispose();
        _writeLock.Dispose();
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
