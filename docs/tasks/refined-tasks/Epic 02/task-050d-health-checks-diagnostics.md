# Task 050.d: Health Checks + Diagnostics (DB Status, Sync Status, Storage Stats)

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – Persistence + Reliability Core  
**Dependencies:** Task 050 (Database Foundation), Task 049.f (Sync Engine)  

---

## Description

Task 050.d implements health checks and diagnostics for the database layer. Health checks provide real-time status. Diagnostics enable troubleshooting. Together they ensure operational visibility.

Health checks answer: "Is the system healthy?" They probe database connectivity, sync status, and storage capacity. A healthy system returns green. An unhealthy system returns red with reasons.

Diagnostics answer: "Why is it broken?" They collect detailed information about database state. Connection pool metrics. Query performance. Error logs. Sync queue depth.

The health check CLI command is `acode status`. It shows overall health at a glance. It shows component-level health. It shows recent issues. It suggests remediation.

Programmatic health checks support automation. CI/CD pipelines check health before deployment. Monitoring systems poll health endpoints. Scripts verify system readiness.

Database status checks include connectivity, version, and performance. Is the database reachable? Is the schema current? Are queries fast? Are connections available?

Sync status checks cover queue depth and lag. How many changes are pending sync? How old is the oldest change? Is sync making progress? Are there failures?

Storage stats track disk usage. How much space is the database using? How fast is it growing? Is there space remaining? Should old data be purged?

The diagnostics report is more detailed. It includes everything from health checks plus additional information. Full connection pool state. Recent slow queries. Error summaries. Configuration dump.

Health checks run fast. Under 100ms for basic checks. They must not impact normal operations. They use separate connections when possible. They timeout quickly.

Diagnostics can run slower. They gather comprehensive information. They may scan tables. They may collect historical data. They are for troubleshooting, not monitoring.

Health checks are structured for machine consumption. JSON output for scripts. Exit codes for CI/CD. Status levels: healthy, degraded, unhealthy.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Health Check | Fast status probe |
| Diagnostics | Detailed troubleshooting |
| DB Status | Database health |
| Sync Status | Sync engine health |
| Storage Stats | Disk usage metrics |
| Connectivity | Can reach database |
| Pool Status | Connection pool state |
| Queue Depth | Pending sync items |
| Sync Lag | Time since oldest pending |
| Degraded | Partially healthy |
| Unhealthy | Failing |
| Healthy | All good |
| Probe | Single health check |
| Aggregate | Combined health |
| Threshold | Alert boundary |

---

## Out of Scope

The following items are explicitly excluded from Task 050.d:

- **Schema migrations** - Task 050.c
- **External monitoring** - No remote endpoints
- **Alerting** - No notifications
- **Historical trending** - No time series
- **Remote diagnostics** - Local only
- **Performance tuning** - Reporting only
- **Auto-remediation** - Report only
- **Cloud health** - Local databases only

---

## Assumptions

### Technical Assumptions

1. **IHealthCheck Interface** - Standard health check contract with Check() returning HealthCheckResult
2. **Registry Pattern** - IHealthCheckRegistry manages registration and execution of all checks
3. **Parallel Execution** - Health checks run concurrently with configurable timeout per check
4. **Aggregate Status** - Overall health is worst status among all checks (Healthy < Degraded < Unhealthy)
5. **Caching** - Health results cached for configurable duration to prevent hammering
6. **Timeout Handling** - Checks exceeding timeout return Unhealthy with timeout reason

### Database-Specific Assumptions

7. **SQLite Checks** - Verify file exists, readable, WAL mode active, integrity_check passes
8. **PostgreSQL Checks** - Verify connection, authentication, schema version, replication lag
9. **Connection Check** - Simple SELECT 1 query validates basic connectivity
10. **Migration Check** - Compares sys_migrations against expected migrations list
11. **Disk Space Check** - Warns if database volume below threshold (configurable MB)
12. **Lock Check** - Detects long-running locks or deadlock potential

### Diagnostic Assumptions

13. **CLI Integration** - `agent db health` and `agent db diagnostics` commands available
14. **JSON Output** - --json flag outputs structured health data for automation
15. **Verbose Mode** - --verbose includes timing, query plans, and detailed metrics
16. **No Secrets** - Diagnostic output never includes passwords or connection strings
17. **Performance Metrics** - Query execution times, connection pool stats exposed
18. **Exit Codes** - Health check exit code reflects overall status (0=healthy, 1=degraded, 2=unhealthy)

---

## Functional Requirements

### Health Check Framework

- FR-001: IHealthCheck interface MUST exist
- FR-002: IHealthCheckRegistry MUST manage checks
- FR-003: Checks MUST run in parallel
- FR-004: Timeout MUST be configurable
- FR-005: Default timeout: 5 seconds

### Health Check Results

- FR-006: HealthCheckResult MUST have status
- FR-007: Status: Healthy, Degraded, Unhealthy
- FR-008: Result MUST have description
- FR-009: Result MUST have duration
- FR-010: Result MUST have timestamp

### Database Connectivity Check

- FR-011: Check MUST verify connection
- FR-012: Check MUST verify query works
- FR-013: Timeout: 2 seconds
- FR-014: Report: connection time

### Database Version Check

- FR-015: Check MUST read schema version
- FR-016: Compare to expected version
- FR-017: Mismatch: degraded status

### Connection Pool Check

- FR-018: Report: active connections
- FR-019: Report: idle connections
- FR-020: Report: max connections
- FR-021: High usage: degraded status

### Sync Queue Check

- FR-022: Report: pending items
- FR-023: Report: failed items
- FR-024: Report: oldest pending age
- FR-025: High queue: degraded status

### Sync Progress Check

- FR-026: Report: last sync time
- FR-027: Report: sync rate
- FR-028: Report: consecutive failures
- FR-029: Stalled sync: unhealthy

### Storage Check

- FR-030: Report: database file size
- FR-031: Report: available disk space
- FR-032: Report: growth rate
- FR-033: Low space: degraded status

### CLI Status Command

- FR-034: `acode status` MUST work
- FR-035: Show overall health
- FR-036: Show component health
- FR-037: Show recent issues
- FR-038: Suggest remediation

### CLI Diagnostics Command

- FR-039: `acode diagnostics` MUST work
- FR-040: Output detailed report
- FR-041: Include all health checks
- FR-042: Include configuration
- FR-043: Include recent errors

### Output Formats

- FR-044: Text output (default)
- FR-045: JSON output (--json)
- FR-046: Exit code: 0 healthy
- FR-047: Exit code: 1 degraded
- FR-048: Exit code: 2 unhealthy

### Thresholds

- FR-049: Thresholds MUST be configurable
- FR-050: Default pool threshold: 80%
- FR-051: Default queue threshold: 1000
- FR-052: Default space threshold: 100MB

### Diagnostics Report

- FR-053: Include system info
- FR-054: Include database info
- FR-055: Include sync info
- FR-056: Include storage info
- FR-057: Include configuration
- FR-058: Include recent errors

---

## Non-Functional Requirements

### Performance

- NFR-001: Health check < 100ms total
- NFR-002: Individual probe < 50ms
- NFR-003: Diagnostics < 5 seconds

### Reliability

- NFR-004: Health check MUST NOT fail
- NFR-005: Errors become unhealthy status
- NFR-006: Timeouts become unhealthy

### Usability

- NFR-007: Clear status output
- NFR-008: Actionable suggestions
- NFR-009: Machine-readable JSON

### Safety

- NFR-010: No sensitive data in output
- NFR-011: No connection strings
- NFR-012: Redact paths if needed

---

## User Manual Documentation

### Overview

The health check and diagnostics system provides visibility into database health. Use `acode status` for quick checks. Use `acode diagnostics` for troubleshooting.

### Quick Status Check

```bash
$ acode status

Acode Health Status
═══════════════════════════════════
Overall: ✓ Healthy

Components:
  ✓ Database Connectivity  (12ms)
  ✓ Schema Version         (8ms)
  ✓ Connection Pool        (5ms)
  ✓ Sync Queue             (15ms)
  ✓ Storage                (10ms)

Last check: 2024-01-20 14:30:45
```

### Degraded Status

```bash
$ acode status

Acode Health Status
═══════════════════════════════════
Overall: ⚠ Degraded

Components:
  ✓ Database Connectivity  (12ms)
  ✓ Schema Version         (8ms)
  ⚠ Connection Pool        (5ms)
      → 85% utilization (threshold: 80%)
      → Consider increasing max connections
  ✓ Sync Queue             (15ms)
  ✓ Storage                (10ms)

Recent Issues:
  2024-01-20 14:25: Connection pool exhausted briefly
  2024-01-20 14:20: Slow query detected (2.5s)

Suggestions:
  → Increase pool_size in config
  → Review slow queries with: acode diagnostics --slow-queries
```

### Unhealthy Status

```bash
$ acode status

Acode Health Status
═══════════════════════════════════
Overall: ✗ Unhealthy

Components:
  ✗ Database Connectivity  (timeout)
      → Cannot connect to database
      → Error: ECONNREFUSED
  ! Schema Version         (skipped)
  ! Connection Pool        (skipped)
  ✓ Sync Queue             (15ms)
  ⚠ Storage                (10ms)
      → 95MB free (threshold: 100MB)

Suggestions:
  → Check if database file exists
  → Run: acode db repair
  → Check disk space
```

### JSON Output

```bash
$ acode status --json
```

```json
{
  "overall": "healthy",
  "timestamp": "2024-01-20T14:30:45Z",
  "duration_ms": 50,
  "components": {
    "database_connectivity": {
      "status": "healthy",
      "duration_ms": 12,
      "details": {
        "connection_time_ms": 10
      }
    },
    "schema_version": {
      "status": "healthy",
      "duration_ms": 8,
      "details": {
        "current": "006_add_feature_flags",
        "expected": "006_add_feature_flags"
      }
    },
    "connection_pool": {
      "status": "healthy",
      "duration_ms": 5,
      "details": {
        "active": 2,
        "idle": 8,
        "max": 20,
        "utilization": 0.10
      }
    },
    "sync_queue": {
      "status": "healthy",
      "duration_ms": 15,
      "details": {
        "pending": 5,
        "failed": 0,
        "oldest_age_seconds": 30
      }
    },
    "storage": {
      "status": "healthy",
      "duration_ms": 10,
      "details": {
        "database_size_mb": 45.2,
        "available_space_mb": 5120,
        "growth_rate_mb_day": 0.5
      }
    }
  }
}
```

### Full Diagnostics

```bash
$ acode diagnostics

Acode Diagnostics Report
═══════════════════════════════════
Generated: 2024-01-20 14:30:45

System Information
──────────────────
  OS: Windows 11
  Runtime: .NET 8.0.1
  Acode Version: 1.0.0

Database Information
────────────────────
  Provider: SQLite
  File: C:\Users\user\.agent\acode.db
  Size: 45.2 MB
  Schema Version: 006_add_feature_flags
  Connection String: [REDACTED]

Connection Pool
───────────────
  Active: 2
  Idle: 8
  Max: 20
  Wait Queue: 0
  Total Created: 150
  Total Destroyed: 140

Sync Status
───────────
  Mode: PostgreSQL sync enabled
  Pending: 5 items
  Failed: 0 items
  Last Sync: 2024-01-20 14:25:00
  Sync Rate: 100 items/minute
  Queue Age: 30 seconds

Storage
───────
  Database Size: 45.2 MB
  WAL Size: 2.1 MB
  Available: 5.1 GB
  Daily Growth: 0.5 MB

Recent Errors (last 24h)
────────────────────────
  2024-01-20 10:15: Slow query (2.5s) - SELECT * FROM conv_messages...
  2024-01-20 09:30: Connection timeout - retry succeeded

Configuration
─────────────
  [database]
    provider = sqlite
    path = .agent/acode.db
    pool_size = 20
    
  [sync]
    enabled = true
    target = postgresql
    interval_seconds = 60
    
Health Checks
─────────────
  All 5 checks passed.
```

### Configuration

```yaml
# .agent/config.yml
health:
  # Individual probe timeout
  timeout_seconds: 5
  
  # Thresholds
  thresholds:
    pool_utilization_percent: 80
    sync_queue_size: 1000
    disk_space_mb: 100
    sync_lag_seconds: 300
```

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Healthy |
| 1 | Degraded |
| 2 | Unhealthy |
| 3 | Error running checks |

### CI/CD Integration

```bash
# Check health before deployment
acode status --json | jq -e '.overall == "healthy"'
if [ $? -ne 0 ]; then
  echo "Health check failed"
  exit 1
fi
```

### Troubleshooting

#### High Connection Pool Usage

**Problem:** Pool utilization above threshold

**Solutions:**
1. Increase pool_size in config
2. Check for connection leaks
3. Review concurrent operations

#### Sync Queue Growing

**Problem:** Queue not draining

**Solutions:**
1. Check network connectivity
2. Verify PostgreSQL is running
3. Review sync errors: `acode diagnostics --sync-errors`

#### Low Disk Space

**Problem:** Available space below threshold

**Solutions:**
1. Run `acode db vacuum` to reclaim space
2. Purge old conversations
3. Move database to larger disk

---

## Acceptance Criteria

### Framework

- [ ] AC-001: IHealthCheck interface exists
- [ ] AC-002: Registry manages checks
- [ ] AC-003: Parallel execution works

### Database Checks

- [ ] AC-004: Connectivity check works
- [ ] AC-005: Version check works
- [ ] AC-006: Pool check works

### Sync Checks

- [ ] AC-007: Queue check works
- [ ] AC-008: Progress check works
- [ ] AC-009: Lag detection works

### Storage Checks

- [ ] AC-010: Size check works
- [ ] AC-011: Space check works
- [ ] AC-012: Growth check works

### CLI

- [ ] AC-013: Status command works
- [ ] AC-014: Diagnostics works
- [ ] AC-015: JSON output works
- [ ] AC-016: Exit codes correct

### Output

- [ ] AC-017: Clear formatting
- [ ] AC-018: Actionable suggestions
- [ ] AC-019: No sensitive data

---

## Best Practices

### Health Check Design

1. **Fast checks only** - Health checks should complete in <1 second; move slow checks to diagnostics
2. **Meaningful status** - Return Degraded for minor issues, Unhealthy only for critical failures
3. **Include context** - Return useful messages: "Connection timeout after 5s" not just "Failed"
4. **Cache results** - Avoid hammering database with repeated health checks; cache for 30s

### Diagnostic Safety

5. **Never expose secrets** - Strip passwords, tokens from all diagnostic output
6. **Read-only operations** - Diagnostics should never modify data
7. **Timeout all checks** - Set maximum duration; mark as failed if exceeded
8. **Run checks in parallel** - Don't block one slow check from returning overall status

### Operational Excellence

9. **JSON output for automation** - Always support `--json` for scripted monitoring
10. **Exit codes matter** - Return 0/1/2 for healthy/degraded/unhealthy for scripting
11. **Actionable suggestions** - Include fix recommendations in diagnostic output
12. **Trending support** - Output metrics in format suitable for time-series collection

---

## Troubleshooting

### Issue: Health check returns false positives

**Symptoms:** Reports Unhealthy when database is actually working

**Causes:**
- Check timeout too short for normal latency
- Check query competing with heavy workload
- Transient network blip during check

**Solutions:**
1. Increase check timeout to accommodate normal latency
2. Schedule checks during low-activity periods
3. Implement retry logic for transient failures before marking unhealthy

### Issue: Diagnostic output contains sensitive data

**Symptoms:** Connection strings or credentials visible in output

**Causes:**
- Exception message includes connection string
- Query parameters logged with sensitive values
- Verbose mode too verbose

**Solutions:**
1. Audit all diagnostic output paths for secrets
2. Implement connection string sanitizer
3. Review exception handling to wrap messages

### Issue: Checks run too frequently

**Symptoms:** Health checks causing measurable database load

**Causes:**
- Caching disabled or too short
- Multiple consumers polling simultaneously
- Missing rate limiting

**Solutions:**
1. Enable result caching with appropriate TTL
2. Consolidate monitoring endpoints
3. Implement rate limiting on health check endpoint

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Health/
├── HealthCheckRegistryTests.cs
│   ├── Should_Register_Checks()
│   ├── Should_Run_Parallel()
│   └── Should_Aggregate_Status()
│
├── DatabaseConnectivityCheckTests.cs
│   ├── Should_Return_Healthy()
│   ├── Should_Return_Unhealthy_On_Timeout()
│   └── Should_Report_Connection_Time()
│
├── SyncQueueCheckTests.cs
│   ├── Should_Report_Queue_Depth()
│   └── Should_Detect_High_Queue()
│
└── StorageCheckTests.cs
    ├── Should_Report_Size()
    └── Should_Warn_Low_Space()
```

### Integration Tests

```
Tests/Integration/Health/
├── HealthCheckIntegrationTests.cs
│   ├── Should_Check_Real_Database()
│   └── Should_Aggregate_All_Checks()
```

### E2E Tests

```
Tests/E2E/Health/
├── StatusCommandE2ETests.cs
│   ├── Should_Show_Healthy()
│   ├── Should_Show_Degraded()
│   └── Should_Return_Exit_Code()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Full health check | 50ms | 100ms |
| Individual probe | 25ms | 50ms |
| Diagnostics | 2s | 5s |

---

## User Verification Steps

### Scenario 1: Basic Status

1. Run `acode status`
2. Verify: Shows overall health
3. Verify: Shows component health

### Scenario 2: JSON Output

1. Run `acode status --json`
2. Verify: Valid JSON
3. Verify: All components present

### Scenario 3: Exit Codes

1. Healthy: verify exit code 0
2. Degrade: verify exit code 1
3. Unhealthy: verify exit code 2

### Scenario 4: Diagnostics

1. Run `acode diagnostics`
2. Verify: Full report shown
3. Verify: No sensitive data

### Scenario 5: Threshold Warning

1. Set low threshold
2. Exceed threshold
3. Verify: Degraded status

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/
├── Health/
│   ├── IHealthCheck.cs
│   ├── IHealthCheckRegistry.cs
│   └── HealthCheckResult.cs
│
src/AgenticCoder.Infrastructure/
├── Health/
│   ├── HealthCheckRegistry.cs
│   ├── Checks/
│   │   ├── DatabaseConnectivityCheck.cs
│   │   ├── SchemaVersionCheck.cs
│   │   ├── ConnectionPoolCheck.cs
│   │   ├── SyncQueueCheck.cs
│   │   └── StorageCheck.cs
│   └── DiagnosticsReportBuilder.cs
│
src/AgenticCoder.CLI/
└── Commands/
    ├── StatusCommand.cs
    └── DiagnosticsCommand.cs
```

### IHealthCheck Interface

```csharp
namespace AgenticCoder.Application.Health;

public interface IHealthCheck
{
    string Name { get; }
    Task<HealthCheckResult> CheckAsync(CancellationToken ct);
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

public sealed record HealthCheckResult(
    string Name,
    HealthStatus Status,
    TimeSpan Duration,
    string? Description = null,
    Dictionary<string, object>? Details = null,
    string? Suggestion = null);
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-HLT-001 | Check failed |
| ACODE-HLT-002 | Timeout |
| ACODE-HLT-003 | Connection failed |
| ACODE-HLT-004 | Low disk space |
| ACODE-HLT-005 | Sync stalled |

### Implementation Checklist

1. [ ] Create health check interface
2. [ ] Create registry
3. [ ] Implement database checks
4. [ ] Implement sync checks
5. [ ] Implement storage checks
6. [ ] Create status command
7. [ ] Create diagnostics command
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Framework
2. **Phase 2:** Database checks
3. **Phase 3:** Sync checks
4. **Phase 4:** Storage checks
5. **Phase 5:** CLI commands

---

**End of Task 050.d Specification**