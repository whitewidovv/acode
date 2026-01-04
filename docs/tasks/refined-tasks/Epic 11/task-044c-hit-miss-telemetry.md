# Task 044.c: Hit/Miss Telemetry

**Priority:** P1 – High  
**Tier:** F – Foundation Layer  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 11 – Optimization  
**Dependencies:** Task 044 (Caching), Task 012 (Logging)  

---

## Description

Task 044.c implements hit/miss telemetry—the instrumentation that tracks cache effectiveness in real-time. Every cache access is recorded as either a hit or miss, with contextual information that enables analysis of cache behavior, identification of optimization opportunities, and detection of cache misuse.

Telemetry goes beyond simple counters. It captures temporal patterns (hit rate over time), categorical breakdowns (hit rate by operation type), and correlation data (hit rate by file type, repository size). This data informs tuning decisions and validates caching strategy effectiveness.

### Business Value

Telemetry provides:
- Cache optimization insights
- Performance monitoring
- Problem detection
- Tuning guidance
- ROI measurement

### Scope Boundaries

This task covers hit/miss instrumentation and analysis. Core caching is Task 044. Key generation is Task 044.a. Management commands are Task 044.b.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Cache | Task 044 | Events | Source |
| Logging | Task 012 | Telemetry logs | Output |
| Metrics | Store | Counters/gauges | Storage |
| CLI | Stats display | Aggregates | Consumer |
| Event Log | Task 040 | Audit | Storage |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Telemetry overhead | Perf check | Sample | Reduced data |
| Storage full | Disk check | Rotate | Old data lost |
| Aggregation error | Validation | Recompute | Wrong stats |
| High volume | Rate check | Sample | Approximate |
| Concurrent issues | Race detect | Lock | Slight overhead |

### Assumptions

1. **Telemetry needed**: Performance matters
2. **Overhead acceptable**: <1ms
3. **Storage available**: For metrics
4. **Analysis useful**: Data drives decisions
5. **Retention finite**: Roll-up old data

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Telemetry | Performance data |
| Hit | Cache found entry |
| Miss | Cache didn't find |
| Hit Rate | Hits / Total |
| Latency | Time to access |
| Breakdown | Stats by category |
| Sampling | Capture subset |
| Aggregation | Combine data |
| Window | Time period |
| Roll-up | Compress old data |

---

## Out of Scope

- Distributed telemetry
- External APM integration
- Real-time alerting
- Machine learning analysis
- Predictive caching
- Telemetry visualization UI

---

## Functional Requirements

### FR-001 to FR-020: Event Capture

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-044c-01 | Hit events MUST be captured | P0 |
| FR-044c-02 | Miss events MUST be captured | P0 |
| FR-044c-03 | Eviction events MUST be captured | P0 |
| FR-044c-04 | Set events MUST be captured | P0 |
| FR-044c-05 | Delete events MUST be captured | P0 |
| FR-044c-06 | Timestamp MUST be recorded | P0 |
| FR-044c-07 | Key MUST be recorded | P0 |
| FR-044c-08 | Operation type MUST be recorded | P0 |
| FR-044c-09 | Latency MUST be recorded | P0 |
| FR-044c-10 | Size MUST be recorded | P0 |
| FR-044c-11 | Tier MUST be recorded | P0 |
| FR-044c-12 | Memory vs Disk MUST be known | P0 |
| FR-044c-13 | Source MUST be recorded | P1 |
| FR-044c-14 | File type MUST be extractable | P1 |
| FR-044c-15 | Repository MUST be known | P1 |
| FR-044c-16 | Commit MUST be recorded | P1 |
| FR-044c-17 | Capture MUST be async | P0 |
| FR-044c-18 | Capture MUST not block | P0 |
| FR-044c-19 | Capture MUST be lightweight | P0 |
| FR-044c-20 | Sampling MUST be optional | P1 |

### FR-021 to FR-040: Counters

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-044c-21 | Hit counter MUST exist | P0 |
| FR-044c-22 | Miss counter MUST exist | P0 |
| FR-044c-23 | Total counter MUST exist | P0 |
| FR-044c-24 | Eviction counter MUST exist | P0 |
| FR-044c-25 | Counters MUST be thread-safe | P0 |
| FR-044c-26 | Counters MUST be atomic | P0 |
| FR-044c-27 | Counters MUST be persistent | P0 |
| FR-044c-28 | Counter reset MUST work | P0 |
| FR-044c-29 | Per-type counters MUST exist | P0 |
| FR-044c-30 | Per-tier counters MUST exist | P0 |
| FR-044c-31 | Latency histogram MUST exist | P0 |
| FR-044c-32 | Size histogram MUST exist | P1 |
| FR-044c-33 | Hit rate gauge MUST exist | P0 |
| FR-044c-34 | Rate MUST be calculated | P0 |
| FR-044c-35 | Rate MUST update in real-time | P0 |
| FR-044c-36 | Window rate MUST work | P0 |
| FR-044c-37 | 1-minute window MUST exist | P0 |
| FR-044c-38 | 5-minute window MUST exist | P0 |
| FR-044c-39 | 1-hour window MUST exist | P0 |
| FR-044c-40 | Session window MUST exist | P0 |

### FR-041 to FR-055: Aggregation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-044c-41 | Aggregation MUST work | P0 |
| FR-044c-42 | Time-based aggregation MUST work | P0 |
| FR-044c-43 | Type-based aggregation MUST work | P0 |
| FR-044c-44 | Tier-based aggregation MUST work | P0 |
| FR-044c-45 | Repository aggregation MUST work | P1 |
| FR-044c-46 | Aggregation MUST be incremental | P0 |
| FR-044c-47 | Aggregation MUST be async | P0 |
| FR-044c-48 | Aggregation MUST run periodically | P0 |
| FR-044c-49 | Default period MUST be 1 minute | P0 |
| FR-044c-50 | Period MUST be configurable | P1 |
| FR-044c-51 | Roll-up MUST work | P0 |
| FR-044c-52 | Roll-up MUST be automatic | P0 |
| FR-044c-53 | Minute → Hour roll-up | P0 |
| FR-044c-54 | Hour → Day roll-up | P0 |
| FR-044c-55 | Retention MUST be configurable | P0 |

### FR-056 to FR-065: Reporting

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-044c-56 | Report MUST be available | P0 |
| FR-044c-57 | Current stats MUST show | P0 |
| FR-044c-58 | Historical stats MUST show | P0 |
| FR-044c-59 | Trend MUST be calculable | P1 |
| FR-044c-60 | Export MUST work | P0 |
| FR-044c-61 | JSON export MUST work | P0 |
| FR-044c-62 | CSV export MUST work | P1 |
| FR-044c-63 | Time range MUST be selectable | P0 |
| FR-044c-64 | Comparison MUST work | P1 |
| FR-044c-65 | Anomaly detection MUST be basic | P2 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-044c-01 | Capture overhead | <1ms | P0 |
| NFR-044c-02 | Counter increment | <100μs | P0 |
| NFR-044c-03 | Aggregation | <100ms | P0 |
| NFR-044c-04 | Report generation | <500ms | P0 |
| NFR-044c-05 | Memory overhead | <10MB | P0 |
| NFR-044c-06 | Disk overhead | <100MB/day | P0 |
| NFR-044c-07 | Concurrent capture | 1000/s | P0 |
| NFR-044c-08 | No lock contention | Minimal | P0 |
| NFR-044c-09 | Sampling rate | 1-100% | P1 |
| NFR-044c-10 | Batch write | 100/batch | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-044c-11 | Counter accuracy | 100% | P0 |
| NFR-044c-12 | No data loss | Best effort | P0 |
| NFR-044c-13 | Thread-safe | 100% | P0 |
| NFR-044c-14 | Cross-platform | All OS | P0 |
| NFR-044c-15 | Crash recovery | Consistent | P0 |
| NFR-044c-16 | Overflow handling | Wrap/reset | P0 |
| NFR-044c-17 | Storage fault | Degrade graceful | P0 |
| NFR-044c-18 | No cache impact | On failure | P0 |
| NFR-044c-19 | Isolation | From cache ops | P0 |
| NFR-044c-20 | Idempotent roll-up | Yes | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-044c-21 | Telemetry logged | Debug | P0 |
| NFR-044c-22 | Aggregation logged | Debug | P0 |
| NFR-044c-23 | Errors logged | Warning | P0 |
| NFR-044c-24 | Stats logged | Info | P0 |
| NFR-044c-25 | Metrics exposed | Counter/Gauge | P0 |
| NFR-044c-26 | Histogram exposed | Latency/Size | P0 |
| NFR-044c-27 | Prometheus format | Optional | P2 |
| NFR-044c-28 | Structured logging | JSON | P0 |
| NFR-044c-29 | Trace correlation | Optional | P1 |
| NFR-044c-30 | Self-telemetry | Minimal | P2 |

---

## Acceptance Criteria / Definition of Done

### Capture
- [ ] AC-001: Hit captured
- [ ] AC-002: Miss captured
- [ ] AC-003: Eviction captured
- [ ] AC-004: Timestamp recorded
- [ ] AC-005: Latency recorded
- [ ] AC-006: Type recorded
- [ ] AC-007: Async capture
- [ ] AC-008: Non-blocking

### Counters
- [ ] AC-009: Hit counter works
- [ ] AC-010: Miss counter works
- [ ] AC-011: Thread-safe
- [ ] AC-012: Persistent
- [ ] AC-013: Per-type counters
- [ ] AC-014: Latency histogram
- [ ] AC-015: Hit rate gauge
- [ ] AC-016: Window rates

### Aggregation
- [ ] AC-017: Time aggregation
- [ ] AC-018: Type aggregation
- [ ] AC-019: Incremental
- [ ] AC-020: Periodic
- [ ] AC-021: Roll-up works
- [ ] AC-022: Automatic
- [ ] AC-023: Retention
- [ ] AC-024: Configurable

### Reporting
- [ ] AC-025: Report available
- [ ] AC-026: Current stats
- [ ] AC-027: Historical
- [ ] AC-028: JSON export
- [ ] AC-029: Time range
- [ ] AC-030: Cross-platform
- [ ] AC-031: Tests pass
- [ ] AC-032: Documented

---

## User Verification Scenarios

### Scenario 1: Real-Time Stats
**Persona:** Developer monitoring  
**Preconditions:** Cache in use  
**Steps:**
1. Use cache
2. Check stats
3. Hits/misses shown
4. Rate accurate

**Verification Checklist:**
- [ ] Real-time data
- [ ] Accurate counts
- [ ] Rate correct
- [ ] Low latency

### Scenario 2: Historical Analysis
**Persona:** Developer analyzing  
**Preconditions:** Telemetry collected  
**Steps:**
1. Request last hour
2. Data returned
3. Trends visible
4. Export to JSON

**Verification Checklist:**
- [ ] Historical available
- [ ] Time range works
- [ ] Trends shown
- [ ] Export works

### Scenario 3: Per-Type Breakdown
**Persona:** Developer optimizing  
**Preconditions:** Multiple cache types  
**Steps:**
1. Request breakdown
2. By-type shown
3. Identify weak areas
4. Optimize

**Verification Checklist:**
- [ ] Breakdown works
- [ ] All types shown
- [ ] Rates per type
- [ ] Actionable

### Scenario 4: Low Overhead
**Persona:** Performance-sensitive user  
**Preconditions:** High cache volume  
**Steps:**
1. Heavy cache use
2. Telemetry active
3. Measure impact
4. <1ms overhead

**Verification Checklist:**
- [ ] Minimal overhead
- [ ] No blocking
- [ ] Performance OK
- [ ] No degradation

### Scenario 5: Latency Tracking
**Persona:** Developer debugging slow  
**Preconditions:** Cache in use  
**Steps:**
1. Check latency stats
2. Histogram shown
3. P95/P99 visible
4. Identify outliers

**Verification Checklist:**
- [ ] Latency tracked
- [ ] Histogram works
- [ ] Percentiles shown
- [ ] Outliers visible

### Scenario 6: Window Rates
**Persona:** Developer checking trends  
**Preconditions:** Extended use  
**Steps:**
1. Check 1-min rate
2. Check 5-min rate
3. Check 1-hour rate
4. Compare

**Verification Checklist:**
- [ ] Windows work
- [ ] Rates accurate
- [ ] Trends visible
- [ ] Comparison useful

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-044c-01 | Hit capture | FR-044c-01 |
| UT-044c-02 | Miss capture | FR-044c-02 |
| UT-044c-03 | Eviction capture | FR-044c-03 |
| UT-044c-04 | Latency recording | FR-044c-09 |
| UT-044c-05 | Counter increment | FR-044c-21 |
| UT-044c-06 | Thread safety | FR-044c-25 |
| UT-044c-07 | Hit rate calc | FR-044c-34 |
| UT-044c-08 | Window rates | FR-044c-36 |
| UT-044c-09 | Aggregation | FR-044c-41 |
| UT-044c-10 | Roll-up | FR-044c-51 |
| UT-044c-11 | Histogram | FR-044c-31 |
| UT-044c-12 | Per-type | FR-044c-29 |
| UT-044c-13 | Export | FR-044c-60 |
| UT-044c-14 | Sampling | FR-044c-20 |
| UT-044c-15 | Reset | FR-044c-28 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-044c-01 | Cache integration | Task 044 |
| IT-044c-02 | Logging integration | Task 012 |
| IT-044c-03 | Real cache flow | E2E |
| IT-044c-04 | High volume | NFR-044c-07 |
| IT-044c-05 | Persistence | FR-044c-27 |
| IT-044c-06 | Concurrent | NFR-044c-13 |
| IT-044c-07 | Cross-platform | NFR-044c-14 |
| IT-044c-08 | Performance | NFR-044c-01 |
| IT-044c-09 | Crash recovery | NFR-044c-15 |
| IT-044c-10 | Roll-up schedule | FR-044c-52 |
| IT-044c-11 | Retention | FR-044c-55 |
| IT-044c-12 | Stats command | Task 044.b |
| IT-044c-13 | Time range | FR-044c-63 |
| IT-044c-14 | Export formats | FR-044c-61 |
| IT-044c-15 | Memory overhead | NFR-044c-05 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Cache/
│       ├── CacheEvent.cs
│       ├── CacheTelemetry.cs
│       └── TelemetryWindow.cs
├── Acode.Application/
│   └── Cache/
│       ├── ICacheTelemetry.cs
│       ├── ITelemetryAggregator.cs
│       └── TelemetryReport.cs
├── Acode.Infrastructure/
│   └── Cache/
│       ├── CacheTelemetryCollector.cs
│       ├── TelemetryCounters.cs
│       ├── TelemetryAggregator.cs
│       ├── LatencyHistogram.cs
│       └── TelemetryStore.cs
```

### Telemetry Schema

```sql
CREATE TABLE CacheTelemetryEvents (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EventType TEXT NOT NULL,      -- hit, miss, evict, set, delete
    Timestamp TEXT NOT NULL,
    Key TEXT,
    OperationType TEXT,           -- index, search, embed
    Tier TEXT,                    -- memory, disk
    LatencyMs REAL,
    SizeBytes INTEGER,
    Repository TEXT
);

CREATE TABLE CacheTelemetryAggregates (
    Period TEXT NOT NULL,         -- 2024-01-22T10:00 (minute)
    OperationType TEXT NOT NULL,
    Tier TEXT NOT NULL,
    Hits INTEGER NOT NULL DEFAULT 0,
    Misses INTEGER NOT NULL DEFAULT 0,
    Evictions INTEGER NOT NULL DEFAULT 0,
    TotalLatencyMs REAL NOT NULL DEFAULT 0,
    TotalSizeBytes INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (Period, OperationType, Tier)
);
```

### Counter Interface

```csharp
public interface ICacheTelemetry
{
    void RecordHit(string key, string operation, string tier, TimeSpan latency, long size);
    void RecordMiss(string key, string operation, string tier, TimeSpan latency);
    void RecordEviction(string key, string reason);
    
    CacheStats GetCurrentStats();
    CacheStats GetWindowStats(TimeSpan window);
    Task<TelemetryReport> GetReportAsync(DateTime from, DateTime to);
}
```

**End of Task 044.c Specification**
