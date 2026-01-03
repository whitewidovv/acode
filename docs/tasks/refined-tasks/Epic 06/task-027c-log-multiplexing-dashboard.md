# Task 027.c: Log Multiplexing/Dashboard

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 027 (Worker Pool), Task 027.a (Local), Task 027.b (Docker)  

---

## Description

Task 027.c implements log multiplexing and a worker dashboard. Multiple workers produce logs simultaneously. These MUST be multiplexed correctly for display and storage.

Log multiplexing MUST preserve order within a worker. Cross-worker ordering MUST use timestamps. Each log line MUST be tagged with its source. Log streams MUST be filterable.

The dashboard MUST show real-time status. Workers, tasks, and queue state MUST be visible. The dashboard MUST update automatically. Both CLI and TUI modes MUST be supported.

### Business Value

Log multiplexing enables:
- Debugging parallel execution
- Correlating events
- Real-time monitoring
- Problem diagnosis
- Performance analysis

### Scope Boundaries

This task covers log aggregation and display. Worker execution is in Task 027.a and 027.b. Pool management is in Task 027.

### Integration Points

- Task 027: Pool provides workers
- Task 027.a: Local worker logs
- Task 027.b: Docker worker logs
- Task 020: Audit log integration

### Failure Modes

- Buffer overflow → Drop oldest
- Display error → Fallback to simple
- High throughput → Rate limit display
- Corrupt stream → Skip and continue

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Multiplex | Combine multiple streams |
| Demultiplex | Separate combined streams |
| Buffer | Temporary log storage |
| Tag | Source identifier prefix |
| Stream | Continuous log flow |
| Dashboard | Status overview display |
| TUI | Terminal User Interface |

---

## Out of Scope

- Web dashboard
- Log shipping to external services
- Log retention policies
- Log search/query language
- Alert generation
- Historical analysis

---

## Functional Requirements

### FR-001 to FR-030: Log Multiplexing

- FR-001: `ILogMultiplexer` interface MUST be defined
- FR-002: Multiple streams MUST be combined
- FR-003: Each line MUST have timestamp
- FR-004: Each line MUST have source tag
- FR-005: Source format: `[worker-id]` or `[task-id]`
- FR-006: Timestamp format MUST be ISO8601
- FR-007: Output MUST be ordered by timestamp
- FR-008: Same-timestamp MUST preserve order
- FR-009: Buffer MUST handle backpressure
- FR-010: Buffer size MUST be configurable
- FR-011: Default buffer: 10000 lines
- FR-012: Overflow MUST drop oldest
- FR-013: Overflow MUST log warning
- FR-014: Filtering by source MUST work
- FR-015: Filtering by level MUST work
- FR-016: Levels: debug, info, warn, error
- FR-017: Output formats MUST be supported
- FR-018: Formats: plain, json, colored
- FR-019: Default format: colored
- FR-020: NO_COLOR MUST be respected
- FR-021: Stdout/stderr MUST be distinguished
- FR-022: Stderr MUST be highlighted
- FR-023: Tailing MUST be supported
- FR-024: Tail MUST follow new lines
- FR-025: Tail MUST handle high throughput
- FR-026: Rate limiting MUST be optional
- FR-027: Rate limit: N lines per second
- FR-028: Aggregation MUST suppress duplicates
- FR-029: Duplicate count MUST be shown
- FR-030: Stream end MUST be detected

### FR-031 to FR-055: Log Storage

- FR-031: Logs MUST be persisted
- FR-032: Storage location MUST be configurable
- FR-033: Default: `.agent/logs/workers/`
- FR-034: File per worker MUST be created
- FR-035: File naming: `worker-{id}.log`
- FR-036: Rotation MUST be supported
- FR-037: Rotation by size MUST work
- FR-038: Default size: 10MB
- FR-039: Rotation by count MUST work
- FR-040: Default count: 5 files
- FR-041: Old files MUST be compressed
- FR-042: Compression: gzip
- FR-043: Total retention MUST be limited
- FR-044: Default retention: 7 days
- FR-045: Cleanup MUST run periodically
- FR-046: Cleanup MUST be logged
- FR-047: Log files MUST be queryable
- FR-048: Query by time range MUST work
- FR-049: Query by worker MUST work
- FR-050: Query by task MUST work
- FR-051: Export MUST be supported
- FR-052: Export formats: plain, json
- FR-053: Combined log MUST be optional
- FR-054: Combined file: `all-workers.log`
- FR-055: Combined MUST be rotated

### FR-056 to FR-080: Dashboard

- FR-056: Dashboard command MUST exist
- FR-057: `acode dashboard` MUST show status
- FR-058: Dashboard MUST update in real-time
- FR-059: Refresh rate MUST be configurable
- FR-060: Default refresh: 1 second
- FR-061: Worker status MUST be shown
- FR-062: Task queue MUST be shown
- FR-063: Recent logs MUST be shown
- FR-064: Resource usage MUST be shown
- FR-065: Layout MUST be configurable
- FR-066: Compact mode MUST exist
- FR-067: Full mode MUST exist
- FR-068: Default: full mode
- FR-069: TUI MUST be keyboard navigable
- FR-070: Vim keys MUST work (hjkl)
- FR-071: Arrow keys MUST work
- FR-072: Tab MUST switch sections
- FR-073: Enter MUST expand details
- FR-074: q MUST quit
- FR-075: Filter MUST be interactive
- FR-076: / MUST start filter
- FR-077: Escape MUST clear filter
- FR-078: Export MUST be available
- FR-079: s MUST save snapshot
- FR-080: Help MUST be shown with ?

---

## Non-Functional Requirements

- NFR-001: Multiplex latency MUST be <10ms
- NFR-002: Display refresh MUST be <100ms
- NFR-003: 1000 lines/sec MUST be handled
- NFR-004: Memory MUST be bounded
- NFR-005: CPU MUST be minimal for display
- NFR-006: Graceful degradation
- NFR-007: Terminal size aware
- NFR-008: Cross-platform display
- NFR-009: No blocking on slow terminals
- NFR-010: Clean exit on interrupt

---

## User Manual Documentation

### CLI Commands

```bash
# View worker logs
acode worker logs

# Follow specific worker
acode worker logs worker-abc123 --follow

# Filter by task
acode worker logs --task xyz789

# Filter by level
acode worker logs --level error

# Output as JSON
acode worker logs --format json

# Show dashboard
acode dashboard

# Compact dashboard
acode dashboard --compact

# Export logs
acode worker logs --since 1h --export logs.json
```

### Dashboard Layout (Full)

```
┌─────────────────────── Acode Dashboard ───────────────────────┐
│ Workers: 4 active (2 busy, 2 idle)      Queue: 12 pending     │
├───────────────────────────────────────────────────────────────┤
│ WORKERS                                                        │
│ ● worker-abc123  busy   task-xyz789  2m 30s                   │
│ ○ worker-def456  idle   -            -                        │
│ ● worker-ghi012  busy   task-uvw345  45s                      │
│ ○ worker-jkl678  idle   -            -                        │
├───────────────────────────────────────────────────────────────┤
│ RECENT LOGS                                                    │
│ 10:30:15 [abc123] Building project...                         │
│ 10:30:18 [abc123] Build succeeded                             │
│ 10:30:20 [ghi012] Running tests...                            │
│ 10:30:22 [ghi012] 15/20 tests passed                          │
├───────────────────────────────────────────────────────────────┤
│ RESOURCES  CPU: 45%  Memory: 1.2GB/4GB  Disk: 2.3GB           │
└─────────────────────── q:quit  ?:help ────────────────────────┘
```

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| q | Quit dashboard |
| ? | Show help |
| j/↓ | Move down |
| k/↑ | Move up |
| Enter | Expand selected |
| Tab | Switch section |
| / | Start filter |
| Esc | Clear filter |
| s | Save snapshot |
| r | Refresh now |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Logs multiplexed
- [ ] AC-002: Timestamps correct
- [ ] AC-003: Source tags shown
- [ ] AC-004: Filtering works
- [ ] AC-005: Logs persisted
- [ ] AC-006: Rotation works
- [ ] AC-007: Dashboard displays
- [ ] AC-008: Real-time updates
- [ ] AC-009: Keyboard nav works
- [ ] AC-010: Export works
- [ ] AC-011: Performance OK
- [ ] AC-012: Cross-platform works

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Multiplex ordering
- [ ] UT-002: Buffer overflow
- [ ] UT-003: Filtering logic
- [ ] UT-004: Rotation logic
- [ ] UT-005: Format output

### Integration Tests

- [ ] IT-001: Multi-worker logs
- [ ] IT-002: Dashboard rendering
- [ ] IT-003: High throughput
- [ ] IT-004: File persistence

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Logging/
│       ├── LogLine.cs
│       ├── TaggedLogLine.cs
│       ├── LogLevel.cs
│       └── LogFilter.cs
├── Acode.Application/
│   └── Logging/
│       ├── ILogMultiplexer.cs
│       ├── ILogPersistence.cs
│       ├── ILogFormatter.cs
│       └── IDashboard.cs
├── Acode.Infrastructure/
│   └── Logging/
│       ├── Multiplexing/
│       │   ├── LogMultiplexer.cs
│       │   ├── LogBuffer.cs
│       │   └── TimestampOrderer.cs
│       ├── Persistence/
│       │   ├── FileLogPersistence.cs
│       │   ├── LogRotator.cs
│       │   └── LogCleaner.cs
│       ├── Formatting/
│       │   ├── PlainLogFormatter.cs
│       │   ├── ColoredLogFormatter.cs
│       │   └── JsonLogFormatter.cs
│       └── Dashboard/
│           ├── TerminalDashboard.cs
│           ├── DashboardLayout.cs
│           ├── Widgets/
│           │   ├── WorkerStatusWidget.cs
│           │   ├── QueueWidget.cs
│           │   ├── LogStreamWidget.cs
│           │   └── ResourceWidget.cs
│           └── Input/
│               └── KeyboardHandler.cs
└── Acode.Cli/
    └── Commands/
        └── Worker/
            ├── WorkerLogsCommand.cs
            └── DashboardCommand.cs
tests/
└── Acode.Infrastructure.Tests/
    └── Logging/
        ├── LogMultiplexerTests.cs
        ├── LogBufferTests.cs
        └── LogRotatorTests.cs
```

### Part 1: Domain Models

```csharp
// File: src/Acode.Domain/Logging/LogLevel.cs
namespace Acode.Domain.Logging;

/// <summary>
/// Log severity levels.
/// </summary>
public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warn = 2,
    Error = 3
}

// File: src/Acode.Domain/Logging/LogLine.cs
namespace Acode.Domain.Logging;

/// <summary>
/// A single log line from a source.
/// </summary>
public sealed record LogLine
{
    public required DateTimeOffset Timestamp { get; init; }
    public required LogLevel Level { get; init; }
    public required string Message { get; init; }
    public bool IsStderr { get; init; } = false;
    
    /// <summary>
    /// Parse log level from string prefix.
    /// </summary>
    public static LogLevel ParseLevel(string message)
    {
        var upper = message.TrimStart().ToUpperInvariant();
        
        if (upper.StartsWith("[ERROR]") || upper.StartsWith("ERROR:"))
            return LogLevel.Error;
        if (upper.StartsWith("[WARN]") || upper.StartsWith("WARNING:"))
            return LogLevel.Warn;
        if (upper.StartsWith("[DEBUG]") || upper.StartsWith("DEBUG:"))
            return LogLevel.Debug;
        
        return LogLevel.Info;
    }
}

// File: src/Acode.Domain/Logging/TaggedLogLine.cs
namespace Acode.Domain.Logging;

/// <summary>
/// Log line tagged with source identifier.
/// </summary>
public sealed record TaggedLogLine
{
    public required string SourceId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required LogLevel Level { get; init; }
    public required string Message { get; init; }
    public bool IsStderr { get; init; } = false;
    
    /// <summary>
    /// Create tagged line from source and raw line.
    /// </summary>
    public static TaggedLogLine FromLogLine(string sourceId, LogLine line) => new()
    {
        SourceId = sourceId,
        Timestamp = line.Timestamp,
        Level = line.Level,
        Message = line.Message,
        IsStderr = line.IsStderr
    };
    
    /// <summary>
    /// Format as plain text with tag.
    /// </summary>
    public string ToPlainText() =>
        $"{Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ} [{SourceId}] {Message}";
}

// File: src/Acode.Domain/Logging/LogFilter.cs
namespace Acode.Domain.Logging;

/// <summary>
/// Filter criteria for log streams.
/// </summary>
public sealed record LogFilter
{
    /// <summary>
    /// Only include logs from these sources.
    /// </summary>
    public IReadOnlyList<string>? SourceIds { get; init; }
    
    /// <summary>
    /// Minimum log level to include.
    /// </summary>
    public LogLevel? MinLevel { get; init; }
    
    /// <summary>
    /// Only include logs after this time.
    /// </summary>
    public DateTimeOffset? Since { get; init; }
    
    /// <summary>
    /// Only include logs before this time.
    /// </summary>
    public DateTimeOffset? Until { get; init; }
    
    /// <summary>
    /// Only include logs containing this text.
    /// </summary>
    public string? Contains { get; init; }
    
    /// <summary>
    /// Check if a log line matches this filter.
    /// </summary>
    public bool Matches(TaggedLogLine line)
    {
        if (SourceIds != null && SourceIds.Count > 0 && 
            !SourceIds.Contains(line.SourceId))
            return false;
        
        if (MinLevel.HasValue && line.Level < MinLevel.Value)
            return false;
        
        if (Since.HasValue && line.Timestamp < Since.Value)
            return false;
        
        if (Until.HasValue && line.Timestamp > Until.Value)
            return false;
        
        if (!string.IsNullOrEmpty(Contains) && 
            !line.Message.Contains(Contains, StringComparison.OrdinalIgnoreCase))
            return false;
        
        return true;
    }
}
```

### Part 2: Application Interfaces

```csharp
// File: src/Acode.Application/Logging/ILogMultiplexer.cs
namespace Acode.Application.Logging;

/// <summary>
/// Combines multiple log streams into a single ordered stream.
/// </summary>
public interface ILogMultiplexer
{
    /// <summary>
    /// Add a log source stream.
    /// </summary>
    void AddSource(string sourceId, IAsyncEnumerable<LogLine> stream);
    
    /// <summary>
    /// Remove a log source.
    /// </summary>
    void RemoveSource(string sourceId);
    
    /// <summary>
    /// Get list of active source IDs.
    /// </summary>
    IReadOnlyList<string> GetActiveSources();
    
    /// <summary>
    /// Get combined stream with optional filtering.
    /// </summary>
    IAsyncEnumerable<TaggedLogLine> GetCombinedStream(
        LogFilter? filter = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Get recent lines from buffer.
    /// </summary>
    IReadOnlyList<TaggedLogLine> GetRecentLines(int count);
}

// File: src/Acode.Application/Logging/ILogPersistence.cs
namespace Acode.Application.Logging;

/// <summary>
/// Persists logs to storage.
/// </summary>
public interface ILogPersistence
{
    /// <summary>
    /// Write a log line.
    /// </summary>
    Task WriteAsync(TaggedLogLine line, CancellationToken ct = default);
    
    /// <summary>
    /// Read logs matching filter.
    /// </summary>
    IAsyncEnumerable<TaggedLogLine> ReadAsync(
        LogFilter? filter = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Export logs to file.
    /// </summary>
    Task ExportAsync(
        string outputPath,
        LogExportFormat format,
        LogFilter? filter = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Trigger log rotation.
    /// </summary>
    Task RotateAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Cleanup old log files.
    /// </summary>
    Task CleanupAsync(CancellationToken ct = default);
}

public enum LogExportFormat
{
    Plain,
    Json
}

// File: src/Acode.Application/Logging/ILogFormatter.cs
namespace Acode.Application.Logging;

/// <summary>
/// Formats log lines for output.
/// </summary>
public interface ILogFormatter
{
    /// <summary>
    /// Format a single log line.
    /// </summary>
    string Format(TaggedLogLine line);
    
    /// <summary>
    /// Format multiple lines.
    /// </summary>
    IEnumerable<string> FormatMany(IEnumerable<TaggedLogLine> lines);
}

// File: src/Acode.Application/Logging/IDashboard.cs
namespace Acode.Application.Logging;

/// <summary>
/// Interactive worker dashboard.
/// </summary>
public interface IDashboard
{
    /// <summary>
    /// Run the dashboard until cancelled.
    /// </summary>
    Task RunAsync(DashboardOptions options, CancellationToken ct = default);
}

/// <summary>
/// Dashboard display options.
/// </summary>
public sealed record DashboardOptions
{
    public DashboardMode Mode { get; init; } = DashboardMode.Full;
    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromSeconds(1);
    public int LogLineCount { get; init; } = 10;
    public bool ShowResources { get; init; } = true;
}

public enum DashboardMode
{
    Full,
    Compact
}
```

*(continued in Part 3...)*

### Part 3: LogMultiplexer Implementation

```csharp
// File: src/Acode.Infrastructure/Logging/Multiplexing/LogBuffer.cs
namespace Acode.Infrastructure.Logging.Multiplexing;

/// <summary>
/// Bounded circular buffer for log lines.
/// </summary>
public sealed class LogBuffer
{
    private readonly TaggedLogLine[] _buffer;
    private readonly object _lock = new();
    private int _head;
    private int _count;
    private long _droppedCount;
    
    public int Capacity { get; }
    public int Count => _count;
    public long DroppedCount => _droppedCount;
    
    public LogBuffer(int capacity = 10000)
    {
        Capacity = capacity;
        _buffer = new TaggedLogLine[capacity];
    }
    
    public void Add(TaggedLogLine line)
    {
        lock (_lock)
        {
            if (_count == Capacity)
            {
                // Buffer full, overwrite oldest
                _droppedCount++;
            }
            else
            {
                _count++;
            }
            
            _buffer[_head] = line;
            _head = (_head + 1) % Capacity;
        }
    }
    
    public IReadOnlyList<TaggedLogLine> GetRecent(int count)
    {
        lock (_lock)
        {
            var take = Math.Min(count, _count);
            var result = new TaggedLogLine[take];
            
            var start = (_head - take + Capacity) % Capacity;
            for (int i = 0; i < take; i++)
            {
                result[i] = _buffer[(start + i) % Capacity];
            }
            
            return result;
        }
    }
    
    public void Clear()
    {
        lock (_lock)
        {
            _count = 0;
            _head = 0;
            Array.Clear(_buffer);
        }
    }
}

// File: src/Acode.Infrastructure/Logging/Multiplexing/LogMultiplexer.cs
namespace Acode.Infrastructure.Logging.Multiplexing;

/// <summary>
/// Combines multiple log streams with timestamp ordering.
/// </summary>
public sealed class LogMultiplexer : ILogMultiplexer, IAsyncDisposable
{
    private readonly Dictionary<string, LogSourceInfo> _sources = new();
    private readonly LogBuffer _buffer;
    private readonly Channel<TaggedLogLine> _outputChannel;
    private readonly ILogger<LogMultiplexer> _logger;
    private readonly object _lock = new();
    
    public LogMultiplexer(
        ILogger<LogMultiplexer> logger,
        int bufferCapacity = 10000)
    {
        _logger = logger;
        _buffer = new LogBuffer(bufferCapacity);
        _outputChannel = Channel.CreateBounded<TaggedLogLine>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
    }
    
    public void AddSource(string sourceId, IAsyncEnumerable<LogLine> stream)
    {
        lock (_lock)
        {
            if (_sources.ContainsKey(sourceId))
                throw new InvalidOperationException(
                    $"Source {sourceId} already registered");
            
            var cts = new CancellationTokenSource();
            var task = ConsumeSourceAsync(sourceId, stream, cts.Token);
            
            _sources[sourceId] = new LogSourceInfo(stream, cts, task);
            
            _logger.LogDebug("Added log source {SourceId}", sourceId);
        }
    }
    
    public void RemoveSource(string sourceId)
    {
        lock (_lock)
        {
            if (_sources.TryGetValue(sourceId, out var info))
            {
                info.Cts.Cancel();
                _sources.Remove(sourceId);
                
                _logger.LogDebug("Removed log source {SourceId}", sourceId);
            }
        }
    }
    
    public IReadOnlyList<string> GetActiveSources()
    {
        lock (_lock)
        {
            return _sources.Keys.ToList();
        }
    }
    
    private async Task ConsumeSourceAsync(
        string sourceId,
        IAsyncEnumerable<LogLine> stream,
        CancellationToken ct)
    {
        try
        {
            await foreach (var line in stream.WithCancellation(ct))
            {
                var tagged = TaggedLogLine.FromLogLine(sourceId, line);
                
                // Add to buffer
                _buffer.Add(tagged);
                
                // Send to output channel
                await _outputChannel.Writer.WriteAsync(tagged, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error consuming log source {SourceId}", sourceId);
        }
    }
    
    public async IAsyncEnumerable<TaggedLogLine> GetCombinedStream(
        LogFilter? filter = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var line in _outputChannel.Reader.ReadAllAsync(ct))
        {
            if (filter == null || filter.Matches(line))
            {
                yield return line;
            }
        }
    }
    
    public IReadOnlyList<TaggedLogLine> GetRecentLines(int count)
    {
        return _buffer.GetRecent(count);
    }
    
    public async ValueTask DisposeAsync()
    {
        lock (_lock)
        {
            foreach (var (_, info) in _sources)
            {
                info.Cts.Cancel();
            }
            _sources.Clear();
        }
        
        _outputChannel.Writer.Complete();
        
        await Task.CompletedTask;
    }
    
    private sealed record LogSourceInfo(
        IAsyncEnumerable<LogLine> Stream,
        CancellationTokenSource Cts,
        Task ConsumerTask);
}
```

### Part 4: Log Formatters

```csharp
// File: src/Acode.Infrastructure/Logging/Formatting/PlainLogFormatter.cs
namespace Acode.Infrastructure.Logging.Formatting;

public sealed class PlainLogFormatter : ILogFormatter
{
    public string Format(TaggedLogLine line)
    {
        var level = line.Level.ToString().ToUpper().PadRight(5);
        var stream = line.IsStderr ? "ERR" : "OUT";
        
        return $"{line.Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ} " +
               $"[{level}] [{line.SourceId}] [{stream}] {line.Message}";
    }
    
    public IEnumerable<string> FormatMany(IEnumerable<TaggedLogLine> lines)
    {
        foreach (var line in lines)
            yield return Format(line);
    }
}

// File: src/Acode.Infrastructure/Logging/Formatting/ColoredLogFormatter.cs
namespace Acode.Infrastructure.Logging.Formatting;

public sealed class ColoredLogFormatter : ILogFormatter
{
    private readonly bool _useColor;
    
    public ColoredLogFormatter()
    {
        // Respect NO_COLOR environment variable
        _useColor = Environment.GetEnvironmentVariable("NO_COLOR") == null &&
                    !Console.IsOutputRedirected;
    }
    
    public string Format(TaggedLogLine line)
    {
        if (!_useColor)
            return new PlainLogFormatter().Format(line);
        
        var timestamp = $"\x1b[90m{line.Timestamp:HH:mm:ss.fff}\x1b[0m";
        var source = $"\x1b[36m[{line.SourceId}]\x1b[0m";
        var level = FormatLevel(line.Level);
        var message = line.IsStderr 
            ? $"\x1b[31m{line.Message}\x1b[0m"
            : line.Message;
        
        return $"{timestamp} {level} {source} {message}";
    }
    
    private static string FormatLevel(LogLevel level) => level switch
    {
        LogLevel.Debug => "\x1b[90m[DEBUG]\x1b[0m",
        LogLevel.Info  => "\x1b[32m[INFO]\x1b[0m ",
        LogLevel.Warn  => "\x1b[33m[WARN]\x1b[0m ",
        LogLevel.Error => "\x1b[31m[ERROR]\x1b[0m",
        _ => $"[{level}]"
    };
    
    public IEnumerable<string> FormatMany(IEnumerable<TaggedLogLine> lines)
    {
        foreach (var line in lines)
            yield return Format(line);
    }
}

// File: src/Acode.Infrastructure/Logging/Formatting/JsonLogFormatter.cs
namespace Acode.Infrastructure.Logging.Formatting;

public sealed class JsonLogFormatter : ILogFormatter
{
    public string Format(TaggedLogLine line)
    {
        var obj = new
        {
            timestamp = line.Timestamp.ToString("O"),
            level = line.Level.ToString().ToLower(),
            source = line.SourceId,
            stream = line.IsStderr ? "stderr" : "stdout",
            message = line.Message
        };
        
        return JsonSerializer.Serialize(obj);
    }
    
    public IEnumerable<string> FormatMany(IEnumerable<TaggedLogLine> lines)
    {
        foreach (var line in lines)
            yield return Format(line);
    }
}
```

### Part 5: Log Persistence

```csharp
// File: src/Acode.Infrastructure/Logging/Persistence/FileLogPersistence.cs
namespace Acode.Infrastructure.Logging.Persistence;

public sealed class FileLogPersistence : ILogPersistence
{
    private readonly string _logDirectory;
    private readonly LogRotator _rotator;
    private readonly LogCleaner _cleaner;
    private readonly ILogger<FileLogPersistence> _logger;
    private readonly ConcurrentDictionary<string, StreamWriter> _writers = new();
    private readonly object _lock = new();
    
    public FileLogPersistence(
        string logDirectory,
        ILogger<FileLogPersistence> logger)
    {
        _logDirectory = logDirectory;
        _logger = logger;
        _rotator = new LogRotator(logDirectory, maxSizeBytes: 10 * 1024 * 1024);
        _cleaner = new LogCleaner(logDirectory, retentionDays: 7);
        
        Directory.CreateDirectory(logDirectory);
    }
    
    public async Task WriteAsync(TaggedLogLine line, CancellationToken ct = default)
    {
        var writer = GetOrCreateWriter(line.SourceId);
        var formatted = new PlainLogFormatter().Format(line);
        
        await writer.WriteLineAsync(formatted.AsMemory(), ct);
        await writer.FlushAsync();
    }
    
    private StreamWriter GetOrCreateWriter(string sourceId)
    {
        return _writers.GetOrAdd(sourceId, id =>
        {
            var path = Path.Combine(_logDirectory, $"worker-{id}.log");
            return new StreamWriter(path, append: true)
            {
                AutoFlush = false
            };
        });
    }
    
    public async IAsyncEnumerable<TaggedLogLine> ReadAsync(
        LogFilter? filter = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var files = Directory.GetFiles(_logDirectory, "worker-*.log");
        
        foreach (var file in files)
        {
            var sourceId = Path.GetFileNameWithoutExtension(file)
                .Replace("worker-", "");
            
            await foreach (var line in ReadFileAsync(file, sourceId, ct))
            {
                if (filter == null || filter.Matches(line))
                    yield return line;
            }
        }
    }
    
    private static async IAsyncEnumerable<TaggedLogLine> ReadFileAsync(
        string path,
        string sourceId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var reader = new StreamReader(path);
        
        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;
            
            if (TryParseLine(line, sourceId, out var tagged))
                yield return tagged;
        }
    }
    
    private static bool TryParseLine(
        string line,
        string sourceId,
        out TaggedLogLine result)
    {
        result = default!;
        
        // Parse: 2024-01-15T10:30:00.000Z [INFO ] [source] [OUT] message
        var match = Regex.Match(line, 
            @"^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z)\s+" +
            @"\[(\w+)\s*\]\s+\[([^\]]+)\]\s+\[(\w+)\]\s+(.*)$");
        
        if (!match.Success)
            return false;
        
        result = new TaggedLogLine
        {
            Timestamp = DateTimeOffset.Parse(match.Groups[1].Value),
            Level = Enum.Parse<LogLevel>(match.Groups[2].Value, ignoreCase: true),
            SourceId = match.Groups[3].Value,
            IsStderr = match.Groups[4].Value == "ERR",
            Message = match.Groups[5].Value
        };
        
        return true;
    }
    
    public async Task ExportAsync(
        string outputPath,
        LogExportFormat format,
        LogFilter? filter = null,
        CancellationToken ct = default)
    {
        ILogFormatter formatter = format switch
        {
            LogExportFormat.Json => new JsonLogFormatter(),
            _ => new PlainLogFormatter()
        };
        
        await using var writer = new StreamWriter(outputPath);
        
        await foreach (var line in ReadAsync(filter, ct))
        {
            await writer.WriteLineAsync(formatter.Format(line).AsMemory(), ct);
        }
    }
    
    public async Task RotateAsync(CancellationToken ct = default)
    {
        // Close current writers
        foreach (var (_, writer) in _writers)
        {
            await writer.DisposeAsync();
        }
        _writers.Clear();
        
        // Rotate files
        await _rotator.RotateAsync(ct);
    }
    
    public Task CleanupAsync(CancellationToken ct = default)
    {
        return _cleaner.CleanupAsync(ct);
    }
}

// File: src/Acode.Infrastructure/Logging/Persistence/LogRotator.cs
namespace Acode.Infrastructure.Logging.Persistence;

public sealed class LogRotator
{
    private readonly string _logDirectory;
    private readonly long _maxSizeBytes;
    private readonly int _maxFiles;
    
    public LogRotator(
        string logDirectory,
        long maxSizeBytes = 10 * 1024 * 1024,
        int maxFiles = 5)
    {
        _logDirectory = logDirectory;
        _maxSizeBytes = maxSizeBytes;
        _maxFiles = maxFiles;
    }
    
    public async Task RotateAsync(CancellationToken ct = default)
    {
        var files = Directory.GetFiles(_logDirectory, "worker-*.log");
        
        foreach (var file in files)
        {
            var info = new FileInfo(file);
            if (info.Length >= _maxSizeBytes)
            {
                await RotateFileAsync(file, ct);
            }
        }
    }
    
    private async Task RotateFileAsync(string path, CancellationToken ct)
    {
        var baseName = Path.GetFileNameWithoutExtension(path);
        var dir = Path.GetDirectoryName(path)!;
        
        // Delete oldest if at max
        var existing = Directory.GetFiles(dir, $"{baseName}.*.log.gz")
            .OrderByDescending(f => f)
            .ToList();
        
        while (existing.Count >= _maxFiles)
        {
            File.Delete(existing.Last());
            existing.RemoveAt(existing.Count - 1);
        }
        
        // Rotate: file.log -> file.1.log.gz
        var rotatedName = $"{baseName}.{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.log.gz";
        var rotatedPath = Path.Combine(dir, rotatedName);
        
        await using var input = File.OpenRead(path);
        await using var output = File.Create(rotatedPath);
        await using var gzip = new GZipStream(output, CompressionLevel.Optimal);
        
        await input.CopyToAsync(gzip, ct);
        
        // Truncate original
        File.WriteAllText(path, "");
    }
}

// File: src/Acode.Infrastructure/Logging/Persistence/LogCleaner.cs
namespace Acode.Infrastructure.Logging.Persistence;

public sealed class LogCleaner
{
    private readonly string _logDirectory;
    private readonly int _retentionDays;
    
    public LogCleaner(string logDirectory, int retentionDays = 7)
    {
        _logDirectory = logDirectory;
        _retentionDays = retentionDays;
    }
    
    public Task CleanupAsync(CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-_retentionDays);
        var files = Directory.GetFiles(_logDirectory, "*.log.gz");
        
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            
            var info = new FileInfo(file);
            if (info.CreationTimeUtc < cutoff)
            {
                File.Delete(file);
            }
        }
        
        return Task.CompletedTask;
    }
}
```

---

**End of Task 027.c Specification - Part 2/3**

### Part 6: Dashboard Implementation

```csharp
// File: src/Acode.Infrastructure/Logging/Dashboard/TerminalDashboard.cs
namespace Acode.Infrastructure.Logging.Dashboard;

/// <summary>
/// Terminal-based worker dashboard with real-time updates.
/// </summary>
public sealed class TerminalDashboard : IDashboard
{
    private readonly IWorkerPool _pool;
    private readonly ITaskQueue _queue;
    private readonly ILogMultiplexer _multiplexer;
    private readonly ILogger<TerminalDashboard> _logger;
    
    private int _selectedSection;
    private int _selectedRow;
    private string? _filterText;
    private bool _showHelp;
    
    public TerminalDashboard(
        IWorkerPool pool,
        ITaskQueue queue,
        ILogMultiplexer multiplexer,
        ILogger<TerminalDashboard> logger)
    {
        _pool = pool;
        _queue = queue;
        _multiplexer = multiplexer;
        _logger = logger;
    }
    
    public async Task RunAsync(DashboardOptions options, CancellationToken ct = default)
    {
        Console.CursorVisible = false;
        Console.Clear();
        
        try
        {
            var keyHandler = new KeyboardHandler();
            using var keyTask = keyHandler.StartAsync(ct);
            
            while (!ct.IsCancellationRequested)
            {
                // Handle input
                while (keyHandler.TryGetKey(out var key))
                {
                    if (HandleKey(key))
                        return; // Quit requested
                }
                
                // Render
                await RenderAsync(options, ct);
                
                // Wait for next refresh
                await Task.Delay(options.RefreshInterval, ct);
            }
        }
        finally
        {
            Console.CursorVisible = true;
            Console.Clear();
        }
    }
    
    private bool HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Q:
                return true; // Quit
            
            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                _selectedRow++;
                break;
            
            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                _selectedRow = Math.Max(0, _selectedRow - 1);
                break;
            
            case ConsoleKey.Tab:
                _selectedSection = (_selectedSection + 1) % 3;
                _selectedRow = 0;
                break;
            
            case ConsoleKey.Oem2 when key.KeyChar == '?':
                _showHelp = !_showHelp;
                break;
            
            case ConsoleKey.Oem2 when key.KeyChar == '/':
                // Start filter mode
                Console.Write("Filter: ");
                _filterText = Console.ReadLine();
                break;
            
            case ConsoleKey.Escape:
                _filterText = null;
                _showHelp = false;
                break;
        }
        
        return false;
    }
    
    private async Task RenderAsync(DashboardOptions options, CancellationToken ct)
    {
        Console.SetCursorPosition(0, 0);
        
        var width = Console.WindowWidth;
        var height = Console.WindowHeight;
        
        if (_showHelp)
        {
            RenderHelp(width, height);
            return;
        }
        
        var layout = new DashboardLayout(width, height, options.Mode);
        
        // Header
        RenderHeader(layout);
        
        // Workers section
        RenderWorkers(layout);
        
        // Logs section
        RenderLogs(layout, options.LogLineCount);
        
        // Resources section (if full mode)
        if (options.Mode == DashboardMode.Full && options.ShowResources)
        {
            RenderResources(layout);
        }
        
        // Footer
        RenderFooter(layout);
    }
    
    private void RenderHeader(DashboardLayout layout)
    {
        var workers = _pool.GetWorkers();
        var busy = workers.Count(w => w.Status == WorkerStatus.Running);
        var idle = workers.Count(w => w.Status == WorkerStatus.Idle);
        var pending = _queue.GetPendingCount();
        
        var title = $" Acode Dashboard ";
        var stats = $"Workers: {workers.Count} ({busy} busy, {idle} idle)  Queue: {pending} pending";
        
        Console.WriteLine(Box.Top(layout.Width, title));
        Console.WriteLine(Box.Content(layout.Width, stats));
        Console.WriteLine(Box.Separator(layout.Width));
    }
    
    private void RenderWorkers(DashboardLayout layout)
    {
        Console.WriteLine(Box.Content(layout.Width, " WORKERS", highlight: _selectedSection == 0));
        
        var workers = _pool.GetWorkers().ToList();
        for (int i = 0; i < Math.Min(workers.Count, layout.WorkerRows); i++)
        {
            var w = workers[i];
            var status = w.Status == WorkerStatus.Running ? "●" : "○";
            var statusText = w.Status.ToString().ToLower().PadRight(7);
            var task = w.CurrentTaskId ?? "-";
            var duration = w.CurrentTaskStarted.HasValue
                ? (DateTimeOffset.UtcNow - w.CurrentTaskStarted.Value).ToString(@"m\:ss")
                : "-";
            
            var selected = _selectedSection == 0 && _selectedRow == i;
            var line = $" {status} {w.Id,-15} {statusText} {task,-12} {duration}";
            Console.WriteLine(Box.Content(layout.Width, line, highlight: selected));
        }
        
        Console.WriteLine(Box.Separator(layout.Width));
    }
    
    private void RenderLogs(DashboardLayout layout, int count)
    {
        Console.WriteLine(Box.Content(layout.Width, " RECENT LOGS", highlight: _selectedSection == 1));
        
        var logs = _multiplexer.GetRecentLines(count);
        var filter = _filterText != null ? new LogFilter { Contains = _filterText } : null;
        
        var filtered = filter != null 
            ? logs.Where(l => filter.Matches(l)).ToList() 
            : logs;
        
        var formatter = new ColoredLogFormatter();
        foreach (var log in filtered.TakeLast(layout.LogRows))
        {
            var formatted = formatter.Format(log);
            if (formatted.Length > layout.Width - 2)
                formatted = formatted[..(layout.Width - 5)] + "...";
            
            Console.WriteLine(Box.Content(layout.Width, " " + formatted));
        }
        
        Console.WriteLine(Box.Separator(layout.Width));
    }
    
    private void RenderResources(DashboardLayout layout)
    {
        var proc = Process.GetCurrentProcess();
        var cpu = 0; // Would need performance counter
        var memMb = proc.WorkingSet64 / 1024 / 1024;
        var diskGb = 0; // Would need disk usage check
        
        var line = $" RESOURCES  CPU: {cpu}%  Memory: {memMb}MB  Disk: {diskGb}GB";
        Console.WriteLine(Box.Content(layout.Width, line));
    }
    
    private void RenderFooter(DashboardLayout layout)
    {
        var help = " q:quit  ?:help  /:filter  Tab:switch ";
        if (_filterText != null)
            help = $" Filter: {_filterText}  Esc:clear ";
        
        Console.WriteLine(Box.Bottom(layout.Width, help));
    }
    
    private void RenderHelp(int width, int height)
    {
        Console.WriteLine(Box.Top(width, " Keyboard Shortcuts "));
        Console.WriteLine(Box.Content(width, ""));
        Console.WriteLine(Box.Content(width, "  q         Quit dashboard"));
        Console.WriteLine(Box.Content(width, "  ?         Toggle help"));
        Console.WriteLine(Box.Content(width, "  j/↓       Move down"));
        Console.WriteLine(Box.Content(width, "  k/↑       Move up"));
        Console.WriteLine(Box.Content(width, "  Tab       Switch section"));
        Console.WriteLine(Box.Content(width, "  Enter     Expand details"));
        Console.WriteLine(Box.Content(width, "  /         Start filter"));
        Console.WriteLine(Box.Content(width, "  Esc       Clear filter"));
        Console.WriteLine(Box.Content(width, "  s         Save snapshot"));
        Console.WriteLine(Box.Content(width, "  r         Refresh now"));
        Console.WriteLine(Box.Content(width, ""));
        Console.WriteLine(Box.Bottom(width, " Press ? to close "));
    }
}

// File: src/Acode.Infrastructure/Logging/Dashboard/DashboardLayout.cs
namespace Acode.Infrastructure.Logging.Dashboard;

public sealed class DashboardLayout
{
    public int Width { get; }
    public int Height { get; }
    public DashboardMode Mode { get; }
    
    public int HeaderRows => 3;
    public int FooterRows => 1;
    public int ResourceRows => Mode == DashboardMode.Full ? 1 : 0;
    public int WorkerRows => Mode == DashboardMode.Full ? 5 : 3;
    public int LogRows => Height - HeaderRows - FooterRows - ResourceRows - WorkerRows - 4;
    
    public DashboardLayout(int width, int height, DashboardMode mode)
    {
        Width = width;
        Height = height;
        Mode = mode;
    }
}

// File: src/Acode.Infrastructure/Logging/Dashboard/Box.cs
namespace Acode.Infrastructure.Logging.Dashboard;

/// <summary>
/// Box drawing helpers for terminal UI.
/// </summary>
public static class Box
{
    public static string Top(int width, string title = "")
    {
        var padding = (width - title.Length - 2) / 2;
        return $"┌{new string('─', padding)}{title}{new string('─', width - padding - title.Length - 2)}┐";
    }
    
    public static string Bottom(int width, string text = "")
    {
        var padding = (width - text.Length - 2) / 2;
        return $"└{new string('─', padding)}{text}{new string('─', width - padding - text.Length - 2)}┘";
    }
    
    public static string Separator(int width)
    {
        return $"├{new string('─', width - 2)}┤";
    }
    
    public static string Content(int width, string text, bool highlight = false)
    {
        if (text.Length > width - 4)
            text = text[..(width - 7)] + "...";
        
        var padded = text.PadRight(width - 2);
        
        if (highlight)
            padded = $"\x1b[7m{padded}\x1b[0m"; // Reverse video
        
        return $"│{padded}│";
    }
}

// File: src/Acode.Infrastructure/Logging/Dashboard/Input/KeyboardHandler.cs
namespace Acode.Infrastructure.Logging.Dashboard.Input;

public sealed class KeyboardHandler : IDisposable
{
    private readonly ConcurrentQueue<ConsoleKeyInfo> _keys = new();
    private CancellationTokenSource? _cts;
    private Task? _readTask;
    
    public Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _readTask = Task.Run(() => ReadLoop(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }
    
    private void ReadLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                _keys.Enqueue(key);
            }
            else
            {
                Thread.Sleep(50);
            }
        }
    }
    
    public bool TryGetKey(out ConsoleKeyInfo key)
    {
        return _keys.TryDequeue(out key);
    }
    
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
```

### Part 7: CLI Commands

```csharp
// File: src/Acode.Cli/Commands/Worker/WorkerLogsCommand.cs
namespace Acode.Cli.Commands.Worker;

[Command("worker logs", Description = "View worker logs")]
public class WorkerLogsCommand : ICommand
{
    [CommandArgument(0, "[WORKER_ID]")]
    public string? WorkerId { get; set; }
    
    [CommandOption("-f|--follow")]
    public bool Follow { get; set; }
    
    [CommandOption("--task")]
    public string? TaskId { get; set; }
    
    [CommandOption("--level")]
    public string? Level { get; set; }
    
    [CommandOption("--since")]
    public string? Since { get; set; }
    
    [CommandOption("--format")]
    [DefaultValue("colored")]
    public string Format { get; set; } = "colored";
    
    [CommandOption("--export")]
    public string? ExportPath { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var filter = BuildFilter();
        var formatter = GetFormatter();
        
        if (ExportPath != null)
        {
            // Export mode
            var persistence = GetLogPersistence();
            var exportFormat = Format == "json" 
                ? LogExportFormat.Json 
                : LogExportFormat.Plain;
            
            await persistence.ExportAsync(ExportPath, exportFormat, filter);
            console.Output.WriteLine($"Exported logs to {ExportPath}");
            return;
        }
        
        if (Follow)
        {
            // Follow mode - stream live
            var multiplexer = GetLogMultiplexer();
            
            await foreach (var line in multiplexer.GetCombinedStream(filter))
            {
                console.Output.WriteLine(formatter.Format(line));
            }
        }
        else
        {
            // Read from persistence
            var persistence = GetLogPersistence();
            
            await foreach (var line in persistence.ReadAsync(filter))
            {
                console.Output.WriteLine(formatter.Format(line));
            }
        }
    }
    
    private LogFilter BuildFilter()
    {
        return new LogFilter
        {
            SourceIds = WorkerId != null ? [WorkerId] : null,
            MinLevel = Level != null ? Enum.Parse<LogLevel>(Level, true) : null,
            Since = Since != null ? ParseSince(Since) : null,
            Contains = TaskId
        };
    }
    
    private ILogFormatter GetFormatter() => Format.ToLower() switch
    {
        "json" => new JsonLogFormatter(),
        "plain" => new PlainLogFormatter(),
        _ => new ColoredLogFormatter()
    };
    
    private static DateTimeOffset ParseSince(string since)
    {
        // Parse relative times like "1h", "30m", "7d"
        if (Regex.IsMatch(since, @"^\d+[hmd]$"))
        {
            var value = int.Parse(since[..^1]);
            return since[^1] switch
            {
                'h' => DateTimeOffset.UtcNow.AddHours(-value),
                'm' => DateTimeOffset.UtcNow.AddMinutes(-value),
                'd' => DateTimeOffset.UtcNow.AddDays(-value),
                _ => DateTimeOffset.Parse(since)
            };
        }
        
        return DateTimeOffset.Parse(since);
    }
}

// File: src/Acode.Cli/Commands/Worker/DashboardCommand.cs
namespace Acode.Cli.Commands.Worker;

[Command("dashboard", Description = "Show worker dashboard")]
public class DashboardCommand : ICommand
{
    [CommandOption("--compact")]
    public bool Compact { get; set; }
    
    [CommandOption("--refresh")]
    [DefaultValue(1000)]
    public int RefreshMs { get; set; } = 1000;
    
    [CommandOption("--logs")]
    [DefaultValue(10)]
    public int LogLines { get; set; } = 10;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var dashboard = GetDashboard();
        
        var options = new DashboardOptions
        {
            Mode = Compact ? DashboardMode.Compact : DashboardMode.Full,
            RefreshInterval = TimeSpan.FromMilliseconds(RefreshMs),
            LogLineCount = LogLines
        };
        
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        
        await dashboard.RunAsync(options, cts.Token);
    }
}
```

### Implementation Checklist

- [ ] Create `LogLine`, `TaggedLogLine`, `LogLevel`, `LogFilter` domain models
- [ ] Create `ILogMultiplexer` interface with stream combination
- [ ] Create `ILogPersistence` interface with read/write/export
- [ ] Create `ILogFormatter` interface with format implementations
- [ ] Create `IDashboard` interface with options
- [ ] Implement `LogBuffer` circular buffer with overflow handling
- [ ] Implement `LogMultiplexer` with channel-based streaming
- [ ] Implement `PlainLogFormatter`, `ColoredLogFormatter`, `JsonLogFormatter`
- [ ] Implement `FileLogPersistence` with per-worker files
- [ ] Implement `LogRotator` with size-based rotation and gzip
- [ ] Implement `LogCleaner` with retention policy
- [ ] Implement `TerminalDashboard` with widgets
- [ ] Implement `KeyboardHandler` for vim-style navigation
- [ ] Implement box drawing helpers
- [ ] Add `acode worker logs` CLI command with filters
- [ ] Add `acode dashboard` CLI command
- [ ] Support NO_COLOR environment variable
- [ ] Write unit tests for buffer and multiplexer
- [ ] Write unit tests for formatters
- [ ] Write integration tests for persistence
- [ ] Test dashboard rendering

### Rollout Plan

1. **Day 1**: Domain models and interfaces
2. **Day 2**: LogBuffer and LogMultiplexer
3. **Day 3**: Log formatters (plain, colored, json)
4. **Day 4**: FileLogPersistence and rotation
5. **Day 5**: Dashboard layout and widgets
6. **Day 6**: Keyboard handling and navigation
7. **Day 7**: CLI commands
8. **Day 8**: Integration testing

---

**End of Task 027.c Specification**