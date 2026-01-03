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

### Interface

```csharp
public interface ILogMultiplexer
{
    void AddSource(string id, IAsyncEnumerable<LogLine> stream);
    void RemoveSource(string id);
    
    IAsyncEnumerable<TaggedLogLine> GetCombinedStream(
        LogFilter? filter = null,
        CancellationToken ct = default);
}

public record LogLine(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Message,
    bool IsStderr);

public record TaggedLogLine(
    string SourceId,
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Message,
    bool IsStderr);

public record LogFilter(
    IReadOnlyList<string>? SourceIds,
    LogLevel? MinLevel,
    DateTimeOffset? Since,
    string? Contains);

public interface IDashboard
{
    Task RunAsync(DashboardOptions options,
        CancellationToken ct = default);
}

public record DashboardOptions(
    DashboardMode Mode,
    TimeSpan RefreshInterval,
    int LogLines);

public enum DashboardMode { Full, Compact }
```

---

**End of Task 027.c Specification**