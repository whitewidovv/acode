namespace Acode.Infrastructure.Audit;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Acode.Domain.Audit;
using Acode.Infrastructure.Common;
using Microsoft.Extensions.Logging;

/// <summary>
/// Writes audit events to JSONL files with integrity checksums.
/// SECURITY CRITICAL: Append-only, tamper-evident logging.
/// </summary>
public sealed class FileAuditWriter : IAuditWriter
{
    private readonly string _auditDirectory;
    private readonly AuditConfiguration _config;
    private readonly ILogger<FileAuditWriter> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;
    private StreamWriter? _currentWriter;
    private string? _currentFilePath;
    private IncrementalHash? _runningHash;
    private long _currentFileSize;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileAuditWriter"/> class.
    /// </summary>
    /// <param name="auditDirectory">Directory for audit logs.</param>
    /// <param name="config">Audit configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public FileAuditWriter(
        string auditDirectory,
        AuditConfiguration config,
        ILogger<FileAuditWriter> logger)
    {
        _auditDirectory = auditDirectory ?? throw new ArgumentNullException(nameof(auditDirectory));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false, // JSONL requires single-line
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
        };

        EnsureDirectoryExists();
    }

    /// <inheritdoc/>
    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FileAuditWriter));
        }

        // Serialize event to JSON (single line)
        var json = JsonSerializer.Serialize(auditEvent, _jsonOptions);

        // Ensure no newlines in output (prevent log injection)
        if (json.Contains('\n', StringComparison.Ordinal) ||
            json.Contains('\r', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Event contains invalid characters that would break JSONL format");
        }

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Check if rotation needed
            if (await NeedsRotationAsync().ConfigureAwait(false))
            {
                await RotateAsync().ConfigureAwait(false);
            }

            // Ensure writer exists
            if (_currentWriter == null)
            {
                await InitializeWriterAsync().ConfigureAwait(false);
            }

            // Write event
            await _currentWriter!.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
            await _currentWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

            // Update file size
            _currentFileSize += Encoding.UTF8.GetByteCount(json) + Environment.NewLine.Length;

            // Update checksum
            UpdateChecksum(json);

            _logger.LogDebug("Audit event written: {EventId}", auditEvent.EventId.Value);
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
            if (_currentWriter != null)
            {
                await _currentWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_currentWriter != null)
            {
                await _currentWriter.DisposeAsync().ConfigureAwait(false);
                _currentWriter = null;
            }

            _runningHash?.Dispose();
            _runningHash = null;

            _disposed = true;
        }
        finally
        {
            _writeLock.Release();
            _writeLock.Dispose();
        }
    }

    private static DateTimeOffset ExtractTimestampFromFileName(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var parts = fileName.Split('-');
        if (parts.Length >= 3)
        {
            // Format: audit-yyyyMMdd-HHmmss
            var dateStr = parts[1];
            var timeStr = parts[2];
            if (DateTimeOffset.TryParseExact(
                $"{dateStr}-{timeStr}",
                "yyyyMMdd-HHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out var timestamp))
            {
                return timestamp;
            }
        }

        return DateTimeOffset.UtcNow;
    }

    private static int GetWeekOfYear(DateTimeOffset date)
    {
        var calendar = System.Globalization.CultureInfo.InvariantCulture.Calendar;
        return calendar.GetWeekOfYear(
            date.DateTime,
            System.Globalization.CalendarWeekRule.FirstDay,
            DayOfWeek.Monday);
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_auditDirectory))
        {
            Directory.CreateDirectory(_auditDirectory);
            _logger.LogInformation("Created audit directory: {Directory}", _auditDirectory);
        }
    }

    private async Task InitializeWriterAsync()
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        _currentFilePath = Path.Combine(_auditDirectory, $"audit-{timestamp}.jsonl");

        _currentWriter = new StreamWriter(_currentFilePath, append: true, Encoding.UTF8)
        {
            AutoFlush = false, // We'll flush manually for performance
        };

        _runningHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        _currentFileSize = 0;

        _logger.LogInformation("Initialized audit log file: {FilePath}", _currentFilePath);

        // If file already exists (unlikely but possible), read existing content for checksum
        if (new FileInfo(_currentFilePath).Length > 0)
        {
            var existing = await File.ReadAllTextAsync(_currentFilePath).ConfigureAwait(false);
            var bytes = Encoding.UTF8.GetBytes(existing);
            _runningHash.AppendData(bytes);
            _currentFileSize = bytes.Length;
        }
    }

    private async Task<bool> NeedsRotationAsync()
    {
        if (_currentFilePath == null)
        {
            return false;
        }

        // Size-based rotation
        var maxBytes = _config.RotationSizeMb * 1024 * 1024;
        if (_currentFileSize >= maxBytes)
        {
            _logger.LogInformation(
                "Rotation needed: size {CurrentSize} >= {MaxSize}",
                _currentFileSize,
                maxBytes);
            return true;
        }

        // Time-based rotation (simplified - check if current file is from different interval)
        var fileTimestamp = ExtractTimestampFromFileName(_currentFilePath);
        var currentTimestamp = DateTimeOffset.UtcNow;

        var needsRotation = _config.RotationInterval switch
        {
            RotationInterval.Hourly => fileTimestamp.Hour != currentTimestamp.Hour ||
                                       fileTimestamp.Day != currentTimestamp.Day,
            RotationInterval.Daily => fileTimestamp.Day != currentTimestamp.Day,
            RotationInterval.Weekly => GetWeekOfYear(fileTimestamp) != GetWeekOfYear(currentTimestamp),
            _ => false,
        };

        if (needsRotation)
        {
            _logger.LogInformation("Rotation needed: time interval changed");
        }

        return await Task.FromResult(needsRotation).ConfigureAwait(false);
    }

    private async Task RotateAsync()
    {
        if (_currentWriter != null)
        {
            await _currentWriter.DisposeAsync().ConfigureAwait(false);
            _currentWriter = null;

            // Write final checksum
            WriteFinalChecksum();
        }

        // Initialize new writer
        await InitializeWriterAsync().ConfigureAwait(false);

        _logger.LogInformation("Log rotation completed");
    }

    private void UpdateChecksum(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json + Environment.NewLine);
        _runningHash!.AppendData(bytes);

        // Write updated checksum to sidecar file
        // Note: We can't get current hash without resetting, so we compute it separately
        using var tempHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        // Re-read file and compute hash (inefficient but correct)
        if (File.Exists(_currentFilePath!))
        {
            var fileBytes = File.ReadAllBytes(_currentFilePath);
            tempHash.AppendData(fileBytes);

            // Add the new line we just wrote
            tempHash.AppendData(bytes);

            var checksumPath = _currentFilePath + ".sha256";
            var hash = tempHash.GetHashAndReset();
            var hashString = Convert.ToHexString(hash).ToLowerInvariant();
            File.WriteAllText(checksumPath, hashString);
        }
    }

    private void WriteFinalChecksum()
    {
        if (_currentFilePath == null || _runningHash == null)
        {
            return;
        }

        var checksumPath = _currentFilePath + ".sha256";

        // Re-read entire file to get final checksum
        if (File.Exists(_currentFilePath))
        {
            using var finalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var fileBytes = File.ReadAllBytes(_currentFilePath);
            finalHash.AppendData(fileBytes);
            var hash = finalHash.GetHashAndReset();
            var hashString = Convert.ToHexString(hash).ToLowerInvariant();
            File.WriteAllText(checksumPath, hashString);

            _logger.LogDebug("Final checksum written: {ChecksumPath}", checksumPath);
        }
    }
}
