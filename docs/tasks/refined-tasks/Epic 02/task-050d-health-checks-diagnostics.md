# Task 050.d: Health Checks + Diagnostics (DB Status, Sync Status, Storage Stats)

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – Persistence + Reliability Core  
**Dependencies:** Task 050 (Database Foundation), Task 049.f (Sync Engine)  

---

## Description

### Overview

Task 050.d implements a comprehensive health check and diagnostics system for the database layer. This system provides real-time visibility into database connectivity, sync status, storage capacity, and overall system health. Without operational visibility, teams spend hours diagnosing issues that could be detected automatically in milliseconds.

### Business Value and ROI

**Annual savings for a 10-developer team: $156,000/year**

| Category | Current Cost | With Health Checks | Annual Savings |
|----------|--------------|-------------------|----------------|
| **Incident Detection** | 45 min avg MTTD | 30 sec automated | $72,000/year |
| **Troubleshooting Time** | 3 hours/incident | 30 min with diagnostics | $48,000/year |
| **Preventive Monitoring** | Manual spot checks | Automated degradation alerts | $24,000/year |
| **CI/CD Reliability** | 15% deployment failures | 3% with health gates | $12,000/year |

**Quantified Improvements:**
- **Mean Time to Detect (MTTD):** 45 minutes manual → 30 seconds automated (99.9% reduction)
- **Troubleshooting Time:** 3 hours per incident → 30 minutes with comprehensive diagnostics (83% reduction)
- **Deployment Success Rate:** 85% → 97% with pre-deployment health gates (14% improvement)
- **Proactive Issue Prevention:** 0 → 15 issues caught before user impact per month

### Technical Architecture

The health check system follows a registry pattern with parallel execution:

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Health Check System                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │                   IHealthCheckRegistry                      │    │
│  │  ┌──────────────┬──────────────┬──────────────────────┐    │    │
│  │  │ RegisterCheck│ RunAllAsync  │ GetAggregateStatus   │    │    │
│  │  └──────────────┴──────────────┴──────────────────────┘    │    │
│  └────────────────────────────────────────────────────────────┘    │
│                            │                                        │
│                            ▼                                        │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │               Parallel Execution Engine                     │    │
│  │     (Task.WhenAll with per-check timeout wrappers)         │    │
│  └────────────────────────────────────────────────────────────┘    │
│                            │                                        │
│         ┌──────────────────┼──────────────────┐                    │
│         ▼                  ▼                  ▼                    │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐              │
│  │  Database   │   │    Sync     │   │   Storage   │              │
│  │   Checks    │   │   Checks    │   │   Checks    │              │
│  ├─────────────┤   ├─────────────┤   ├─────────────┤              │
│  │ Connectivity│   │ Queue Depth │   │ Disk Space  │              │
│  │ Schema Ver  │   │ Sync Lag    │   │ DB Size     │              │
│  │ Pool Status │   │ Last Sync   │   │ Growth Rate │              │
│  │ Lock Status │   │ Error Count │   │ WAL Size    │              │
│  └─────────────┘   └─────────────┘   └─────────────┘              │
│                            │                                        │
│                            ▼                                        │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │                 HealthCheckResult Aggregator               │    │
│  │   Worst Status: Healthy < Degraded < Unhealthy            │    │
│  │   Duration: Max of all checks                              │    │
│  │   Details: Merged from all checks                          │    │
│  └────────────────────────────────────────────────────────────┘    │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Health Check Lifecycle

```
┌─────────────────────────────────────────────────────────────────────┐
│                      Health Check Execution                          │
└─────────────────────────────────────────────────────────────────────┘

  Request          Check Cache        Cache Hit?        Return Cached
  ─────────────> ──────────────────> ────────────────> ────────────────>
                       │                    │
                       │ No                 │ Yes
                       ▼                    │
               ┌─────────────┐              │
               │   Acquire   │              │
               │ Check Lock  │              │
               └──────┬──────┘              │
                      │                     │
                      ▼                     │
  ┌─────────────────────────────────────────┼─────────────────────────┐
  │           Parallel Execution Phase      │                          │
  │                                         │                          │
  │   ┌──────────┐  ┌──────────┐  ┌────────┴───┐                      │
  │   │  Check 1 │  │  Check 2 │  │  Check N   │                      │
  │   │ Timeout: │  │ Timeout: │  │  Timeout:  │                      │
  │   │   5s     │  │   5s     │  │    5s      │                      │
  │   └────┬─────┘  └────┬─────┘  └─────┬──────┘                      │
  │        │             │              │                              │
  │        ▼             ▼              ▼                              │
  │   ┌──────────────────────────────────────┐                        │
  │   │         Wait for Completion          │                        │
  │   │    (Task.WhenAll with Cancellation)  │                        │
  │   └──────────────────────────────────────┘                        │
  │                      │                                             │
  └──────────────────────┼─────────────────────────────────────────────┘
                         │
                         ▼
               ┌─────────────────┐
               │   Aggregate     │
               │   Results       │
               └────────┬────────┘
                        │
                        ▼
               ┌─────────────────┐
               │  Update Cache   │
               │   (TTL: 30s)    │
               └────────┬────────┘
                        │
                        ▼
                  Return Result
```

### Status Aggregation Logic

The overall health status follows a worst-case aggregation:

| Component Statuses | Aggregate Result |
|-------------------|------------------|
| All Healthy | **Healthy** |
| Any Degraded, No Unhealthy | **Degraded** |
| Any Unhealthy | **Unhealthy** |
| Check Timeout | **Unhealthy** (check marked as failed) |
| Check Exception | **Unhealthy** (with exception details) |

### Health Check Categories

#### Database Checks
- **Connectivity Check:** Executes `SELECT 1` to verify database responds
- **Schema Version Check:** Compares current migration version to expected
- **Connection Pool Check:** Reports active/idle/max connections and utilization
- **Lock Status Check:** Detects long-running locks or potential deadlocks

#### Sync Checks
- **Queue Depth Check:** Reports pending outbox items count
- **Sync Lag Check:** Measures age of oldest pending sync item
- **Last Sync Check:** Reports time since last successful sync
- **Error Count Check:** Tracks consecutive sync failures

#### Storage Checks
- **Database Size Check:** Reports SQLite file size or PostgreSQL database size
- **Available Space Check:** Monitors disk/volume free space
- **Growth Rate Check:** Calculates daily growth trend
- **WAL Size Check:** SQLite-specific WAL file monitoring

### Integration Points

| Component | Integration Method | Purpose |
|-----------|-------------------|---------|
| Task 050 Database Foundation | IConnectionFactory | Get connections for probes |
| Task 049.f Sync Engine | IOutboxRepository | Query sync queue metrics |
| Task 050.c Migration Runner | IMigrationRepository | Check schema version |
| CLI Commands | IHealthCheckRegistry | Execute health checks |
| CI/CD Pipelines | Exit codes + JSON | Automated health gates |
| Monitoring Systems | JSON output | External observability |

### Constraints

1. **Performance Budget:** Total health check must complete in <100ms; individual probes <50ms
2. **No Side Effects:** Health checks must be read-only; no data modification
3. **Graceful Degradation:** Failed checks return Unhealthy; they never throw
4. **Timeout Handling:** All checks wrapped with configurable timeout
5. **Caching Required:** Results cached (30s default) to prevent check hammering
6. **No Secrets in Output:** Connection strings, passwords redacted from all output
7. **Exit Code Contract:** 0=Healthy, 1=Degraded, 2=Unhealthy, 3=Check Error

### Trade-offs and Design Decisions

| Decision | Option A | Option B | **Choice** | Rationale |
|----------|----------|----------|------------|-----------|
| **Execution Model** | Sequential checks | Parallel checks | **Parallel** | Faster overall completion; all checks independent |
| **Status Levels** | Binary (Up/Down) | Ternary (Healthy/Degraded/Unhealthy) | **Ternary** | Degraded allows warnings without alerts |
| **Caching Strategy** | No cache | TTL-based cache | **TTL (30s)** | Prevents hammering while staying current |
| **Timeout Behavior** | Throw exception | Return Unhealthy | **Unhealthy** | Graceful degradation; no exception propagation |
| **Output Format** | Text only | Text + JSON | **Both** | Human-readable CLI + machine-readable automation |

### Error Codes

| Code | Name | Description |
|------|------|-------------|
| ACODE-HLT-001 | HealthCheckFailed | Individual check failed to execute |
| ACODE-HLT-002 | HealthCheckTimeout | Check exceeded timeout duration |
| ACODE-HLT-003 | DatabaseConnectionFailed | Cannot connect to database |
| ACODE-HLT-004 | LowDiskSpace | Available space below threshold |
| ACODE-HLT-005 | SyncStalled | Sync queue not draining |
| ACODE-HLT-006 | SchemaVersionMismatch | Database schema doesn't match expected |
| ACODE-HLT-007 | PoolExhausted | Connection pool at capacity |
| ACODE-HLT-008 | DiagnosticsFailed | Diagnostics report generation failed |

---

## Use Cases

### Use Case 1: DevBot Automated Deployment Health Gate

**Persona:** DevBot - CI/CD Pipeline Agent

**Context:** DevBot manages automated deployments to staging and production. Before deploying, it must verify the target environment is healthy. A deployment to an unhealthy environment risks data corruption or service outages.

**Current State (Without Health Checks):**
- DevBot deploys blindly, hoping the environment is ready
- 15% of deployments fail due to undetected environment issues
- Each failed deployment costs 2 hours of rollback and investigation
- Developers are paged at 3am for issues that could have been prevented

**Workflow:**
1. DevBot initiates deployment to staging
2. DevBot executes `acode status --json` on target environment
3. DevBot parses JSON response to check `overall` status
4. If `overall != "healthy"`:
   - Abort deployment with clear error message
   - Log failing component details to deployment log
   - Notify team with remediation suggestions
5. If `overall == "healthy"`:
   - Proceed with deployment
   - Run post-deployment health check
   - Mark deployment as successful only if post-check passes

**Commands:**
```bash
# Pre-deployment check
acode status --json | jq -e '.overall == "healthy"'
HEALTH_EXIT=$?

if [ $HEALTH_EXIT -ne 0 ]; then
    echo "Environment unhealthy, aborting deployment"
    acode status --json | jq '.components | to_entries[] | select(.value.status != "healthy")'
    exit 1
fi

# Deploy
./deploy.sh

# Post-deployment check
acode status --json | jq -e '.overall == "healthy"'
```

**Quantified Impact:**
- Deployment failure rate: 15% → 3% (80% reduction)
- Failed deployment cost: 2 hours × $150/hour × 12 failures/year = $3,600/year saved per developer
- Team of 10: $36,000/year in prevented failed deployments
- MTTR improvement: 2 hours → 15 minutes (87% reduction)

---

### Use Case 2: Jordan Incident Response Acceleration

**Persona:** Jordan - Senior DevOps Engineer

**Context:** Jordan is on-call when an alert fires at 2am: "API response times elevated." Jordan needs to quickly determine if the database is the bottleneck. Traditional approach involves SSH, running queries, checking logs—15 minutes just to gather data.

**Current State (Without Diagnostics):**
- Jordan SSHes to server, runs `sqlite3` commands manually
- Jordan checks disk space with `df -h`, connection count with `lsof`
- Jordan reviews sync logs by grepping log files
- Total time to gather diagnostic data: 15-20 minutes
- By the time data is gathered, the incident may have resolved or escalated

**Workflow:**
1. Alert fires: Jordan receives page
2. Jordan runs `acode diagnostics --json > /tmp/diag.json`
3. In 5 seconds, Jordan has:
   - Connection pool state (is pool exhausted?)
   - Sync queue depth (is sync backing up?)
   - Recent slow queries (which queries are slow?)
   - Disk space status (is storage full?)
   - Configuration summary (what are current settings?)
4. Jordan identifies: Connection pool at 95% utilization, 3 slow queries
5. Jordan increases pool size, identifies and optimizes slow query
6. Incident resolved in 15 minutes instead of 1 hour

**Commands:**
```bash
# Quick health overview
acode status

# Detailed diagnostics
acode diagnostics

# Focus on slow queries
acode diagnostics --slow-queries

# Export for post-mortem
acode diagnostics --json > incident_$(date +%Y%m%d_%H%M).json
```

**Sample Output During Incident:**
```
Acode Diagnostics Report
═══════════════════════════════════
Generated: 2024-01-20 02:15:30

⚠ CONNECTION POOL: Degraded
  Active: 19 / 20 (95% utilization)
  Wait Queue: 5 requests waiting
  Avg Wait: 250ms
  
⚠ SLOW QUERIES (last hour): 3 detected
  1. SELECT * FROM conv_messages WHERE... (2.5s, 15 calls)
  2. UPDATE sync_outbox SET... (1.2s, 50 calls)
  3. DELETE FROM sys_sessions WHERE... (0.8s, 100 calls)
  
✓ SYNC QUEUE: Healthy
  Pending: 50 items
  Oldest: 30 seconds
  
✓ STORAGE: Healthy
  Database: 145 MB
  Available: 50 GB

SUGGESTIONS:
  → Increase pool_size to 30 in config
  → Add index on conv_messages(conversation_id, created_at)
  → Consider batch processing for sync_outbox updates
```

**Quantified Impact:**
- Diagnostic data gathering: 15 min → 5 sec (99.5% reduction)
- MTTD: 45 min → 30 sec (99% reduction)
- MTTR: 1 hour → 15 min (75% reduction)
- Cost per incident: $150/hour × 1 hour → $150/hour × 0.25 hour = $112 saved/incident
- At 50 incidents/year: $5,600/year savings per engineer

---

### Use Case 3: Alex Proactive Capacity Planning

**Persona:** Alex - Team Lead responsible for infrastructure capacity

**Context:** Alex needs to plan infrastructure capacity for the next quarter. Without historical data on storage growth and resource utilization, Alex relies on guesswork. This leads to either over-provisioning (wasted budget) or under-provisioning (outages).

**Current State (Without Health Metrics):**
- Alex manually checks disk usage monthly
- No trend data available—just point-in-time snapshots
- Capacity planning based on gut feeling
- Surprises: "We're out of disk space!" at worst possible time

**Workflow:**
1. Alex sets up daily health check collection:
   ```bash
   # Cron job: collect metrics daily
   0 0 * * * acode status --json >> /var/log/acode-health.jsonl
   ```

2. Alex queries trending data monthly:
   ```bash
   # Extract storage metrics from last 30 days
   tail -30 /var/log/acode-health.jsonl | \
     jq -r '.components.storage.details | [.database_size_mb, .growth_rate_mb_day] | @csv'
   ```

3. Alex projects capacity needs:
   - Current DB size: 145 MB
   - Growth rate: 0.5 MB/day
   - Days until 500 MB limit: (500 - 145) / 0.5 = 710 days ✓
   - Action: No immediate concern, revisit in 6 months

4. Alex monitors pool utilization trends:
   - Peak utilization last month: 85%
   - Trend: +2% per week
   - Projection: Will hit 100% in 7 weeks
   - Action: Increase pool size from 20 to 30 before next release

**Commands:**
```bash
# Export current metrics for trending
acode status --json | jq '.components | {
    pool_utilization: .connection_pool.details.utilization,
    db_size_mb: .storage.details.database_size_mb,
    sync_pending: .sync_queue.details.pending,
    timestamp: now
}'

# Weekly capacity report
acode diagnostics --json | jq '{
    week: (now | strftime("%Y-W%W")),
    avg_pool_util: ...,
    max_db_size: ...,
    storage_growth: ...
}'
```

**Quantified Impact:**
- Capacity planning accuracy: Guesswork → Data-driven (unmeasurable → measurable)
- Over-provisioning reduction: 30% wasted resources → 10% buffer (67% efficiency gain)
- Outage prevention: 2 capacity-related outages/year → 0 (100% prevention)
- Cost avoidance: $24,000/year in prevented emergency provisioning

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

- NFR-001: Total health check MUST complete in <100ms under normal conditions
- NFR-002: Individual probe MUST complete in <50ms
- NFR-003: Full diagnostics report MUST complete in <5 seconds
- NFR-004: JSON serialization MUST add <5ms overhead
- NFR-005: Cache hit response MUST return in <1ms
- NFR-006: Health check MUST NOT block normal database operations
- NFR-007: Parallel execution MUST reduce total time vs sequential

### Reliability

- NFR-008: Health check framework MUST NOT throw exceptions; all errors become Unhealthy status
- NFR-009: Individual check timeouts MUST return Unhealthy, not propagate timeout exception
- NFR-010: Registry MUST continue running remaining checks if one check fails
- NFR-011: Cached results MUST be returned if live check fails (graceful degradation)
- NFR-012: Check registration MUST be idempotent; duplicate registration ignored
- NFR-013: Health checks MUST be thread-safe for concurrent callers
- NFR-014: Diagnostics MUST succeed even if some data sources unavailable

### Usability

- NFR-015: CLI output MUST use color coding (green/yellow/red) for status when terminal supports
- NFR-016: Status descriptions MUST be human-readable sentences, not codes
- NFR-017: Suggestions MUST be actionable commands, not just descriptions
- NFR-018: JSON output MUST be valid, parseable, and schema-consistent
- NFR-019: Exit codes MUST follow convention: 0=healthy, 1=degraded, 2=unhealthy, 3=error
- NFR-020: Help text MUST document all command options and output formats

### Safety

- NFR-021: Output MUST NEVER include connection strings or passwords
- NFR-022: Output MUST NEVER include API keys or tokens
- NFR-023: File paths MUST be redacted if they contain usernames (Windows: C:\Users\...)
- NFR-024: Exception messages MUST be sanitized before inclusion in diagnostics
- NFR-025: Query content MUST be truncated/redacted if contains user data
- NFR-026: Health checks MUST be read-only; no data modification permitted
- NFR-027: Diagnostics MUST use separate read-only connection when possible

### Scalability

- NFR-028: Health check system MUST support 100 concurrent callers
- NFR-029: Cache MUST support configurable TTL (default 30 seconds)
- NFR-030: Registry MUST support 50+ registered checks without degradation
- NFR-031: JSON output size MUST be bounded (<1MB for full diagnostics)

### Maintainability

- NFR-032: Adding new health check MUST require only implementing IHealthCheck
- NFR-033: Check thresholds MUST be configurable without code changes
- NFR-034: New diagnostics sections MUST be addable via extension points
- NFR-035: All checks MUST log execution details at Debug level

---

## Security Considerations

### Threat 1: Information Disclosure via Health Check Output

**Risk:** Health check output may inadvertently expose sensitive configuration, file paths, or internal system details that could aid an attacker in reconnaissance.

**Attack Scenario:** An attacker gains access to health check output (via logs, CI/CD artifacts, or direct access) and uses exposed database paths, version numbers, and configuration details to craft targeted attacks.

**Mitigation Code:**

```csharp
// Infrastructure/Health/HealthOutputSanitizer.cs
namespace Acode.Infrastructure.Health;

public sealed class HealthOutputSanitizer
{
    private static readonly Regex ConnectionStringPattern = new(
        @"(Server|Host|Data Source|User Id|Password|Pwd)=[^;]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex PathWithUsernamePattern = new(
        @"[A-Za-z]:\\Users\\[^\\]+\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex ApiKeyPattern = new(
        @"(api[_-]?key|apikey|token|secret|password|pwd|auth)[\s]*[=:]\s*['""]?[A-Za-z0-9+/=_-]{16,}['""]?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private readonly ILogger<HealthOutputSanitizer> _logger;
    
    public HealthOutputSanitizer(ILogger<HealthOutputSanitizer> logger)
    {
        _logger = logger;
    }
    
    public string Sanitize(string content)
    {
        var result = content;
        
        // Redact connection strings
        result = ConnectionStringPattern.Replace(result, match =>
        {
            var key = match.Value.Split('=')[0];
            _logger.LogDebug("Redacted connection string component: {Key}", key);
            return $"{key}=[REDACTED]";
        });
        
        // Redact user paths
        result = PathWithUsernamePattern.Replace(result, match =>
        {
            _logger.LogDebug("Redacted user path");
            return @"C:\Users\[REDACTED]\";
        });
        
        // Redact API keys and secrets
        result = ApiKeyPattern.Replace(result, match =>
        {
            var key = match.Value.Split(new[] { '=', ':' })[0].Trim();
            _logger.LogDebug("Redacted secret: {Key}", key);
            return $"{key}=[REDACTED]";
        });
        
        return result;
    }
    
    public HealthCheckResult SanitizeResult(HealthCheckResult result)
    {
        return result with
        {
            Description = result.Description is not null 
                ? Sanitize(result.Description) 
                : null,
            Details = result.Details?.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value is string s ? (object)Sanitize(s) : kvp.Value)
        };
    }
}
```

---

### Threat 2: Denial of Service via Health Check Hammering

**Risk:** Malicious or misconfigured clients could flood the health check endpoint, consuming database connections and CPU, degrading service for legitimate users.

**Attack Scenario:** A monitoring system misconfiguration causes 1000 health checks/second, exhausting the connection pool and causing application timeouts.

**Mitigation Code:**

```csharp
// Infrastructure/Health/HealthCheckRateLimiter.cs
namespace Acode.Infrastructure.Health;

public sealed class HealthCheckRateLimiter
{
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly TokenBucket _rateLimiter;
    private readonly ILogger<HealthCheckRateLimiter> _logger;
    private readonly int _maxConcurrent;
    private readonly int _maxPerSecond;
    
    public HealthCheckRateLimiter(
        ILogger<HealthCheckRateLimiter> logger,
        IOptions<HealthCheckSettings> settings)
    {
        _logger = logger;
        _maxConcurrent = settings.Value.MaxConcurrentChecks;
        _maxPerSecond = settings.Value.MaxChecksPerSecond;
        _concurrencyLimiter = new SemaphoreSlim(_maxConcurrent);
        _rateLimiter = new TokenBucket(_maxPerSecond, TimeSpan.FromSeconds(1));
    }
    
    public async Task<HealthCheckResult?> ExecuteWithLimitAsync(
        Func<Task<HealthCheckResult>> checkFunc,
        string checkName,
        CancellationToken ct)
    {
        // Check rate limit first
        if (!_rateLimiter.TryConsume())
        {
            _logger.LogWarning(
                "Health check rate limit exceeded for {Check}. Max: {Max}/sec",
                checkName, _maxPerSecond);
            
            return new HealthCheckResult(
                checkName,
                HealthStatus.Unhealthy,
                TimeSpan.Zero,
                "Rate limit exceeded - try again later",
                new Dictionary<string, object>
                {
                    ["rate_limited"] = true,
                    ["retry_after_ms"] = 1000
                });
        }
        
        // Check concurrency limit
        if (!await _concurrencyLimiter.WaitAsync(TimeSpan.FromMilliseconds(100), ct))
        {
            _logger.LogWarning(
                "Health check concurrency limit reached for {Check}. Max: {Max}",
                checkName, _maxConcurrent);
            
            return new HealthCheckResult(
                checkName,
                HealthStatus.Degraded,
                TimeSpan.Zero,
                "Too many concurrent checks - result may be cached",
                new Dictionary<string, object>
                {
                    ["queued"] = true
                });
        }
        
        try
        {
            return await checkFunc();
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }
}

public sealed class TokenBucket
{
    private readonly int _capacity;
    private readonly TimeSpan _refillInterval;
    private int _tokens;
    private DateTime _lastRefill;
    private readonly object _lock = new();
    
    public TokenBucket(int capacity, TimeSpan refillInterval)
    {
        _capacity = capacity;
        _refillInterval = refillInterval;
        _tokens = capacity;
        _lastRefill = DateTime.UtcNow;
    }
    
    public bool TryConsume()
    {
        lock (_lock)
        {
            Refill();
            if (_tokens > 0)
            {
                _tokens--;
                return true;
            }
            return false;
        }
    }
    
    private void Refill()
    {
        var now = DateTime.UtcNow;
        var elapsed = now - _lastRefill;
        
        if (elapsed >= _refillInterval)
        {
            var refills = (int)(elapsed / _refillInterval);
            _tokens = Math.Min(_capacity, _tokens + refills * _capacity);
            _lastRefill = now;
        }
    }
}
```

---

### Threat 3: Timing Attack via Health Check Latency

**Risk:** Differences in health check response times could reveal information about system state (e.g., whether database exists, pool size, sync state).

**Attack Scenario:** An attacker measures health check latency variations to infer database load patterns, identify peak usage times, or detect when sensitive operations occur.

**Mitigation Code:**

```csharp
// Infrastructure/Health/ConstantTimeHealthCheck.cs
namespace Acode.Infrastructure.Health;

public sealed class ConstantTimeHealthCheck : IHealthCheck
{
    private readonly IHealthCheck _innerCheck;
    private readonly TimeSpan _minimumDuration;
    private readonly ILogger<ConstantTimeHealthCheck> _logger;
    
    public string Name => _innerCheck.Name;
    
    public ConstantTimeHealthCheck(
        IHealthCheck innerCheck,
        TimeSpan minimumDuration,
        ILogger<ConstantTimeHealthCheck> logger)
    {
        _innerCheck = innerCheck;
        _minimumDuration = minimumDuration;
        _logger = logger;
    }
    
    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        
        HealthCheckResult result;
        try
        {
            result = await _innerCheck.CheckAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check {Name} failed", Name);
            result = new HealthCheckResult(
                Name,
                HealthStatus.Unhealthy,
                stopwatch.Elapsed,
                "Check failed");
        }
        
        stopwatch.Stop();
        
        // Pad to minimum duration to prevent timing attacks
        var remaining = _minimumDuration - stopwatch.Elapsed;
        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining, ct);
        }
        
        // Report the padded duration
        var totalDuration = TimeSpan.FromMilliseconds(
            Math.Max(stopwatch.ElapsedMilliseconds, (long)_minimumDuration.TotalMilliseconds));
        
        return result with { Duration = totalDuration };
    }
}
```

---

### Threat 4: Privilege Escalation via Diagnostic Commands

**Risk:** Diagnostic commands that execute queries or access configuration could be abused to access data or configuration beyond intended scope.

**Attack Scenario:** An attacker with CLI access runs `acode diagnostics` which exposes configuration settings, query patterns, or internal paths that enable further exploitation.

**Mitigation Code:**

```csharp
// Infrastructure/Health/DiagnosticsAccessControl.cs
namespace Acode.Infrastructure.Health;

public sealed class DiagnosticsAccessControl
{
    private readonly ILogger<DiagnosticsAccessControl> _logger;
    private readonly DiagnosticsSettings _settings;
    
    public DiagnosticsAccessControl(
        ILogger<DiagnosticsAccessControl> logger,
        IOptions<DiagnosticsSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }
    
    public DiagnosticsLevel GetAllowedLevel()
    {
        // Determine access level based on context
        if (IsRunningInCI())
        {
            _logger.LogDebug("CI environment detected - limiting diagnostics");
            return DiagnosticsLevel.Basic;
        }
        
        if (!IsInteractiveTerminal())
        {
            _logger.LogDebug("Non-interactive context - limiting diagnostics");
            return DiagnosticsLevel.Standard;
        }
        
        return DiagnosticsLevel.Full;
    }
    
    public DiagnosticsReport FilterReport(DiagnosticsReport report, DiagnosticsLevel level)
    {
        return level switch
        {
            DiagnosticsLevel.Basic => report with
            {
                Configuration = null,
                RecentErrors = null,
                SlowQueries = null,
                SystemInfo = FilterSystemInfo(report.SystemInfo)
            },
            DiagnosticsLevel.Standard => report with
            {
                Configuration = RedactConfiguration(report.Configuration),
                SlowQueries = report.SlowQueries?.Take(5).ToList()
            },
            DiagnosticsLevel.Full => report with
            {
                Configuration = RedactConfiguration(report.Configuration)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };
    }
    
    private static bool IsRunningInCI()
    {
        return Environment.GetEnvironmentVariable("CI") == "true" ||
               Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" ||
               Environment.GetEnvironmentVariable("TF_BUILD") == "true";
    }
    
    private static bool IsInteractiveTerminal()
    {
        return Environment.UserInteractive && 
               !Console.IsInputRedirected;
    }
    
    private static SystemInfo FilterSystemInfo(SystemInfo? info)
    {
        if (info is null) return new SystemInfo();
        
        return info with
        {
            MachineName = "[REDACTED]",
            UserName = "[REDACTED]"
        };
    }
    
    private static Dictionary<string, object>? RedactConfiguration(
        Dictionary<string, object>? config)
    {
        if (config is null) return null;
        
        var sensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "secret", "key", "token", "connectionstring", "credentials"
        };
        
        return config.ToDictionary(
            kvp => kvp.Key,
            kvp => sensitiveKeys.Any(k => kvp.Key.Contains(k, StringComparison.OrdinalIgnoreCase))
                ? (object)"[REDACTED]"
                : kvp.Value);
    }
}

public enum DiagnosticsLevel { Basic, Standard, Full }
```

---

### Threat 5: Cache Poisoning via Stale Health Data

**Risk:** If health check cache is poisoned with stale "healthy" results, unhealthy conditions may go undetected, leading to deployments to failing environments.

**Attack Scenario:** A brief healthy period is cached, then conditions degrade. Cached healthy result continues to be served for 30 seconds, during which a deployment proceeds to an unhealthy environment.

**Mitigation Code:**

```csharp
// Infrastructure/Health/SecureHealthCache.cs
namespace Acode.Infrastructure.Health;

public sealed class SecureHealthCache
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<SecureHealthCache> _logger;
    private readonly TimeSpan _healthyTtl;
    private readonly TimeSpan _unhealthyTtl;
    private readonly TimeSpan _degradedTtl;
    
    public SecureHealthCache(
        IMemoryCache cache,
        ILogger<SecureHealthCache> logger,
        IOptions<HealthCacheSettings> settings)
    {
        _cache = cache;
        _logger = logger;
        _healthyTtl = TimeSpan.FromSeconds(settings.Value.HealthyTtlSeconds);
        _unhealthyTtl = TimeSpan.FromSeconds(settings.Value.UnhealthyTtlSeconds);
        _degradedTtl = TimeSpan.FromSeconds(settings.Value.DegradedTtlSeconds);
    }
    
    public HealthCheckResult? TryGetCached(string checkName)
    {
        if (_cache.TryGetValue<CacheEntry>(GetCacheKey(checkName), out var entry))
        {
            // Validate entry hasn't been tampered with
            if (!ValidateEntry(entry))
            {
                _logger.LogWarning(
                    "Cache entry validation failed for {Check} - discarding",
                    checkName);
                _cache.Remove(GetCacheKey(checkName));
                return null;
            }
            
            return entry.Result;
        }
        
        return null;
    }
    
    public void Cache(HealthCheckResult result)
    {
        var ttl = result.Status switch
        {
            HealthStatus.Healthy => _healthyTtl,
            HealthStatus.Degraded => _degradedTtl,
            HealthStatus.Unhealthy => _unhealthyTtl,
            _ => _healthyTtl
        };
        
        // Unhealthy results cached for shorter duration
        // to ensure faster recovery detection
        var entry = new CacheEntry
        {
            Result = result,
            CachedAt = DateTime.UtcNow,
            Checksum = ComputeChecksum(result)
        };
        
        _cache.Set(GetCacheKey(result.Name), entry, ttl);
        
        _logger.LogDebug(
            "Cached {Check} result with status {Status} for {Ttl}",
            result.Name, result.Status, ttl);
    }
    
    private static string GetCacheKey(string checkName) 
        => $"health_check_{checkName}";
    
    private bool ValidateEntry(CacheEntry entry)
    {
        var expectedChecksum = ComputeChecksum(entry.Result);
        return entry.Checksum == expectedChecksum;
    }
    
    private static string ComputeChecksum(HealthCheckResult result)
    {
        var data = $"{result.Name}|{result.Status}|{result.Description}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(bytes);
    }
    
    private sealed record CacheEntry
    {
        public required HealthCheckResult Result { get; init; }
        public required DateTime CachedAt { get; init; }
        public required string Checksum { get; init; }
    }
}
```

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

### Health Check Framework

- [ ] AC-001: `IHealthCheck` interface exists with `Name` property and `CheckAsync` method
- [ ] AC-002: `IHealthCheckRegistry` interface defines `Register`, `GetRegisteredChecks`, and `CheckAllAsync`
- [ ] AC-003: `HealthCheckResult` record contains Name, Status, Duration, Description, Details, Suggestion
- [ ] AC-004: `HealthStatus` enum has three values: Healthy, Degraded, Unhealthy
- [ ] AC-005: `CompositeHealthResult` aggregates individual results with worst-case status
- [ ] AC-006: Registry executes all checks in parallel using `Task.WhenAll`
- [ ] AC-007: Registry handles check exceptions without failing other checks
- [ ] AC-008: Registry respects per-check timeout (individual check can timeout)
- [ ] AC-009: Registry logs all check executions at Debug level
- [ ] AC-010: Duplicate check registration is idempotent (no exception)

### Health Check Caching

- [ ] AC-011: Cache implementation exists with TTL support
- [ ] AC-012: Healthy results cached for 30 seconds (configurable)
- [ ] AC-013: Degraded results cached for 10 seconds (configurable)
- [ ] AC-014: Unhealthy results cached for 5 seconds (configurable)
- [ ] AC-015: Cache can be bypassed with `--no-cache` flag
- [ ] AC-016: Cache hit returns in <1ms
- [ ] AC-017: Cache clear method exists for testing

### Database Connectivity Check

- [ ] AC-018: Check named "DatabaseConnectivity" with Category "Database"
- [ ] AC-019: Returns Healthy when database responds within threshold
- [ ] AC-020: Returns Degraded when response time exceeds threshold but succeeds
- [ ] AC-021: Returns Unhealthy when connection fails
- [ ] AC-022: Returns Unhealthy when database is locked (SQLITE_BUSY)
- [ ] AC-023: Includes `connection_time_ms` in Details
- [ ] AC-024: Provides actionable Suggestion when Degraded or Unhealthy
- [ ] AC-025: Timeout is configurable (default 100ms)

### Schema Version Check

- [ ] AC-026: Check named "SchemaVersion" with Category "Database"
- [ ] AC-027: Returns Healthy when actual version matches expected version
- [ ] AC-028: Returns Degraded when version mismatch (needs migration)
- [ ] AC-029: Returns Degraded when migrations table missing (needs init)
- [ ] AC-030: Includes `expected_version` and `actual_version` in Details
- [ ] AC-031: Includes `needs_migration` boolean in Details
- [ ] AC-032: Suggestion includes command: "Run `acode migrate --apply`"

### Connection Pool Check

- [ ] AC-033: Check named "ConnectionPool" with Category "Database"
- [ ] AC-034: Returns Healthy when pool has available connections
- [ ] AC-035: Returns Degraded when pool utilization > 80%
- [ ] AC-036: Returns Unhealthy when pool exhausted
- [ ] AC-037: Includes `active`, `available`, `peak`, `total` in Details
- [ ] AC-038: Suggestion for exhausted pool: reduce concurrent operations

### Sync Queue Check

- [ ] AC-039: Check named "SyncQueue" with Category "Sync"
- [ ] AC-040: Returns Healthy when queue depth < threshold (default 100)
- [ ] AC-041: Returns Degraded when queue depth > threshold but processing
- [ ] AC-042: Returns Unhealthy when sync stalled (no progress for 10+ minutes)
- [ ] AC-043: Includes `queue_depth` and `last_processed_at` in Details
- [ ] AC-044: Suggestion for stalled sync: check sync service

### Storage Check

- [ ] AC-045: Check named "Storage" with Category "Storage"
- [ ] AC-046: Returns Healthy when free space > 15%
- [ ] AC-047: Returns Degraded when free space 5-15%
- [ ] AC-048: Returns Unhealthy when free space < 5%
- [ ] AC-049: Includes `percent_free`, `available_bytes`, `database_size_bytes` in Details
- [ ] AC-050: Handles UNC paths on Windows
- [ ] AC-051: Gracefully degrades if space info unavailable
- [ ] AC-052: Suggestion includes action: "Free up disk space"

### Status Command

- [ ] AC-053: `acode status` command exists
- [ ] AC-054: Default output is human-readable text format
- [ ] AC-055: `--format json` produces valid JSON output
- [ ] AC-056: `--verbose` shows detailed component information
- [ ] AC-057: `--no-cache` bypasses cache
- [ ] AC-058: `--timeout` sets overall timeout (default 30s)
- [ ] AC-059: Shows overall status with visual indicator (✓/⚠/✗)
- [ ] AC-060: Shows each component with name, duration, description
- [ ] AC-061: Shows suggestions for non-healthy components

### Diagnostics Command

- [ ] AC-062: `acode diagnostics` command exists
- [ ] AC-063: Default output shows all sections
- [ ] AC-064: `--section <name>` filters to specific section
- [ ] AC-065: `--section a,b` supports multiple sections
- [ ] AC-066: `--format json` produces valid JSON
- [ ] AC-067: Database section shows path, size, version, pool
- [ ] AC-068: Sync section shows queue depth, last sync, processing time
- [ ] AC-069: Storage section shows size, available, growth rate
- [ ] AC-070: Invalid section name shows helpful error

### Exit Codes

- [ ] AC-071: Exit code 0 when all checks Healthy
- [ ] AC-072: Exit code 1 when any check Degraded (none Unhealthy)
- [ ] AC-073: Exit code 2 when any check Unhealthy
- [ ] AC-074: Exit code 3 for command errors (timeout, exception)
- [ ] AC-075: Exit code works correctly with JSON output
- [ ] AC-076: Exit code documented in help text

### Output Formatting

- [ ] AC-077: Text output uses color (green/yellow/red) when terminal supports
- [ ] AC-078: Text output degrades gracefully without color
- [ ] AC-079: JSON output includes `status`, `timestamp`, `checks` array
- [ ] AC-080: JSON output includes `duration_ms` for each check
- [ ] AC-081: JSON timestamp uses ISO 8601 format
- [ ] AC-082: Verbose mode shows Details dictionary contents

### Security & Sanitization

- [ ] AC-083: Connection strings never appear in output
- [ ] AC-084: Passwords and secrets never appear in output
- [ ] AC-085: User paths redacted: `C:\Users\xxx` → `C:\Users\[REDACTED]`
- [ ] AC-086: API keys and tokens redacted
- [ ] AC-087: Stack traces redacted to hide local paths
- [ ] AC-088: Exception messages sanitized before output
- [ ] AC-089: Sanitizer applied to ALL output paths (text, JSON, verbose)

### Rate Limiting

- [ ] AC-090: Rate limiter prevents more than N checks/second
- [ ] AC-091: Concurrent check limit prevents resource exhaustion
- [ ] AC-092: Rate-limited response returns cached result
- [ ] AC-093: Rate limit configurable via settings

### Performance

- [ ] AC-094: Full health check completes in <100ms under normal conditions
- [ ] AC-095: Individual probe completes in <50ms
- [ ] AC-096: Cache hit returns in <1ms
- [ ] AC-097: Diagnostics report completes in <5 seconds
- [ ] AC-098: JSON serialization adds <5ms overhead
- [ ] AC-099: No blocking of main application during checks

### Testing

- [ ] AC-100: Unit tests for each health check with mocks
- [ ] AC-101: Unit tests for registry with parallel execution
- [ ] AC-102: Unit tests for cache TTL behavior
- [ ] AC-103: Unit tests for sanitizer patterns
- [ ] AC-104: Integration tests with real SQLite database
- [ ] AC-105: E2E tests for CLI commands and exit codes
- [ ] AC-106: Performance benchmarks documented and passing

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

### Issue 1: Health Check Returns False Positives

**Error Code:** ACODE-HLT-001

**Symptoms:**
- `acode status` reports Unhealthy but database queries work normally
- Intermittent unhealthy status with no actual problems
- Green dashboard flickers to red briefly

**Root Cause Analysis:**
```
┌────────────────────────────────────────────────────────┐
│ DIAGNOSTIC: False Positive Health Check                │
├────────────────────────────────────────────────────────┤
│ 1. Check timeout (50ms) < actual latency (75ms)        │
│ 2. Network micro-partition during check                │
│ 3. DB connection pool temporarily exhausted            │
│ 4. Heavy concurrent load causing timeout               │
│ 5. Clock skew affecting timeout calculation            │
└────────────────────────────────────────────────────────┘
```

**Diagnostic Steps:**
```powershell
# 1. Check actual database latency
acode diagnostics --section database

# 2. View health check timing history
acode status --verbose --format json | Select-Object -ExpandProperty duration_history

# 3. Test with extended timeout
acode status --timeout 200

# 4. Check connection pool stats
acode diagnostics --section pool
```

**Solutions:**

```csharp
// Solution 1: Increase timeout threshold
public sealed class DatabaseConnectivityCheckOptions
{
    // Increase from 50ms default to accommodate network variability
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(150);
    
    // Enable retry for transient failures
    public int RetryCount { get; set; } = 2;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(25);
}

// Solution 2: Implement sliding window for status determination
public sealed class SlidingWindowHealthAggregator
{
    private readonly Queue<HealthCheckResult> _history = new();
    private readonly int _windowSize = 5;
    private readonly double _unhealthyThreshold = 0.6; // 60% must be unhealthy
    
    public HealthStatus GetAggregatedStatus(HealthCheckResult current)
    {
        _history.Enqueue(current);
        if (_history.Count > _windowSize)
            _history.Dequeue();
        
        var unhealthyRatio = _history.Count(r => r.Status == HealthStatus.Unhealthy) 
            / (double)_history.Count;
        
        return unhealthyRatio >= _unhealthyThreshold 
            ? HealthStatus.Unhealthy 
            : HealthStatus.Healthy;
    }
}
```

**Prevention:**
- Use sliding window aggregation to prevent single-check false positives
- Set timeouts to 3x normal latency (p95)
- Enable retry with exponential backoff

---

### Issue 2: Diagnostic Output Contains Sensitive Data

**Error Code:** ACODE-HLT-002

**Symptoms:**
- Connection strings visible in `acode diagnostics` output
- File paths expose usernames (C:\Users\realname\...)
- Exception messages contain API keys or tokens

**Root Cause Analysis:**
```
┌────────────────────────────────────────────────────────┐
│ DIAGNOSTIC: Information Disclosure                      │
├────────────────────────────────────────────────────────┤
│ 1. HealthOutputSanitizer not applied to all outputs    │
│ 2. Exception.Message includes raw connection string    │
│ 3. Verbose mode bypassing redaction                    │
│ 4. Third-party library exposing internals              │
│ 5. Stack trace includes sensitive local paths          │
└────────────────────────────────────────────────────────┘
```

**Diagnostic Steps:**
```powershell
# 1. Test output sanitization
acode diagnostics --format json | Select-String -Pattern "(password|secret|token)"

# 2. Check sanitizer configuration
acode config show --key diagnostics.sanitization

# 3. Review exception handling
acode diagnostics --section errors | Select-String -Pattern "connectionstring"
```

**Solutions:**

```csharp
// Solution 1: Enhanced sanitizer coverage
public sealed class ComprehensiveOutputSanitizer
{
    private readonly List<Func<string, string>> _sanitizers = new()
    {
        // Connection strings
        s => Regex.Replace(s, 
            @"(Server|Host|Data Source|User Id|Password|Pwd)=[^;]+", 
            "$1=[REDACTED]", 
            RegexOptions.IgnoreCase),
        
        // User paths (Windows)
        s => Regex.Replace(s, 
            @"[A-Za-z]:\\Users\\[^\\]+", 
            @"C:\Users\[REDACTED]", 
            RegexOptions.IgnoreCase),
        
        // User paths (Unix)
        s => Regex.Replace(s, 
            @"/home/[^/]+", 
            "/home/[REDACTED]"),
        
        // API keys (various formats)
        s => Regex.Replace(s, 
            @"(api[_-]?key|token|secret|bearer)\s*[=:]\s*[A-Za-z0-9+/=_-]{16,}", 
            "$1=[REDACTED]", 
            RegexOptions.IgnoreCase),
        
        // Stack traces with paths
        s => Regex.Replace(s, 
            @"at .+ in (/[^:]+|[A-Za-z]:\\[^:]+):", 
            "at [method] in [REDACTED]:"),
        
        // IP addresses
        s => Regex.Replace(s, 
            @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b", 
            "[IP-REDACTED]")
    };
    
    public string Sanitize(string input)
    {
        return _sanitizers.Aggregate(input, (current, sanitizer) => sanitizer(current));
    }
}

// Solution 2: Wrapper for all diagnostic exceptions
public sealed class SanitizedException : Exception
{
    public SanitizedException(Exception inner, IOutputSanitizer sanitizer)
        : base(sanitizer.Sanitize(inner.Message), inner)
    {
    }
    
    public override string StackTrace => "[Stack trace redacted]";
}
```

**Prevention:**
- Apply sanitizer to ALL output paths (JSON, text, verbose)
- Wrap exceptions at the boundary before logging
- Audit diagnostic output in CI pipeline

---

### Issue 3: Health Checks Causing Database Load

**Error Code:** ACODE-HLT-003

**Symptoms:**
- Database CPU spikes correlate with health check intervals
- Connection pool exhaustion during heavy health check load
- Health checks themselves showing as slow queries

**Root Cause Analysis:**
```
┌────────────────────────────────────────────────────────┐
│ DIAGNOSTIC: Health Check Database Load                 │
├────────────────────────────────────────────────────────┤
│ 1. Caching disabled or TTL too short                   │
│ 2. Multiple consumers polling same endpoint            │
│ 3. Rate limiting not configured                        │
│ 4. Health checks using full connections                │
│ 5. Parallel checks causing connection spike            │
└────────────────────────────────────────────────────────┘
```

**Diagnostic Steps:**
```powershell
# 1. Check current cache configuration
acode config show --key health.cache

# 2. Monitor health check frequency
acode diagnostics --section health-metrics

# 3. View connection pool during checks
acode status --verbose --format json | Select-Object -ExpandProperty pool_stats
```

**Solutions:**

```csharp
// Solution 1: Aggressive caching
public sealed class HealthCacheConfiguration
{
    public TimeSpan HealthyTtl { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan DegradedTtl { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan UnhealthyTtl { get; set; } = TimeSpan.FromSeconds(5);
    
    // Stale-while-revalidate pattern
    public TimeSpan StaleWhileRevalidate { get; set; } = TimeSpan.FromSeconds(10);
}

// Solution 2: Dedicated connection for health checks
public sealed class DedicatedHealthConnection : IHealthConnection
{
    private readonly string _connectionString;
    private SqliteConnection? _dedicatedConnection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    
    public async Task<IDbConnection> GetConnectionAsync(CancellationToken ct)
    {
        await _connectionLock.WaitAsync(ct);
        try
        {
            if (_dedicatedConnection is null or { State: ConnectionState.Closed })
            {
                _dedicatedConnection = new SqliteConnection(_connectionString);
                await _dedicatedConnection.OpenAsync(ct);
            }
            return _dedicatedConnection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }
}

// Solution 3: Rate limiting
public sealed class RateLimitedHealthRegistry : IHealthCheckRegistry
{
    private readonly IHealthCheckRegistry _inner;
    private readonly RateLimiter _rateLimiter;
    
    public async Task<CompositeHealthResult> CheckAllAsync(CancellationToken ct)
    {
        if (!await _rateLimiter.TryAcquireAsync(ct))
        {
            // Return cached result instead of checking again
            return _lastCachedResult ?? await _inner.CheckAllAsync(ct);
        }
        
        var result = await _inner.CheckAllAsync(ct);
        _lastCachedResult = result;
        return result;
    }
}
```

**Prevention:**
- Configure minimum 30-second cache TTL for healthy results
- Use dedicated read-only connection for health checks
- Implement rate limiting (max 2 checks/second)

---

### Issue 4: Diagnostics Command Times Out

**Error Code:** ACODE-HLT-004

**Symptoms:**
- `acode diagnostics` hangs or times out after 5+ seconds
- Partial output followed by timeout error
- Works fine with `--section` but not full report

**Root Cause Analysis:**
```
┌────────────────────────────────────────────────────────┐
│ DIAGNOSTIC: Diagnostics Timeout                        │
├────────────────────────────────────────────────────────┤
│ 1. Large sync queue causing slow enumeration           │
│ 2. Disk I/O bottleneck reading database files          │
│ 3. Schema analysis scanning large table                │
│ 4. Network timeout reaching external resources         │
│ 5. Sequential execution of independent sections        │
└────────────────────────────────────────────────────────┘
```

**Diagnostic Steps:**
```powershell
# 1. Test individual sections to identify bottleneck
acode diagnostics --section database --timeout 30
acode diagnostics --section sync --timeout 30
acode diagnostics --section storage --timeout 30

# 2. Check file system performance
acode diagnostics --section storage

# 3. Measure section timing
acode diagnostics --verbose --format json | Select-Object -ExpandProperty section_timings
```

**Solutions:**

```csharp
// Solution 1: Parallel section execution with timeout
public sealed class ParallelDiagnosticsExecutor
{
    private readonly TimeSpan _sectionTimeout = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _totalTimeout = TimeSpan.FromSeconds(30);
    
    public async Task<DiagnosticsReport> ExecuteAsync(
        IEnumerable<IDiagnosticsSection> sections,
        CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_totalTimeout);
        
        var tasks = sections.Select(async section =>
        {
            using var sectionCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
            sectionCts.CancelAfter(_sectionTimeout);
            
            try
            {
                return await section.GatherAsync(sectionCts.Token);
            }
            catch (OperationCanceledException)
            {
                return new SectionResult(section.Name, SectionStatus.Timeout, 
                    "Section timed out - try running independently");
            }
        });
        
        var results = await Task.WhenAll(tasks);
        return new DiagnosticsReport(results);
    }
}

// Solution 2: Lazy section loading with streaming output
public sealed class StreamingDiagnosticsCommand
{
    public async IAsyncEnumerable<SectionResult> StreamSectionsAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var section in _sections.OrderBy(s => s.Priority))
        {
            yield return await section.GatherAsync(ct);
        }
    }
}
```

**Prevention:**
- Run sections in parallel by default
- Set per-section timeouts (10s) independent of total timeout
- Stream output for large diagnostic reports

---

### Issue 5: Exit Code Not Matching Status

**Error Code:** ACODE-HLT-005

**Symptoms:**
- `acode status` shows "Degraded" but exit code is 0
- CI pipeline doesn't fail on unhealthy status
- Scripts not detecting health check failures

**Root Cause Analysis:**
```
┌────────────────────────────────────────────────────────┐
│ DIAGNOSTIC: Exit Code Mismatch                         │
├────────────────────────────────────────────────────────┤
│ 1. Exit code mapping not configured correctly          │
│ 2. Exception swallowed returning success               │
│ 3. PowerShell vs cmd.exe exit code interpretation      │
│ 4. --format json overriding exit code behavior         │
│ 5. Background execution ignoring exit code             │
└────────────────────────────────────────────────────────┘
```

**Diagnostic Steps:**
```powershell
# 1. Verify exit code explicitly
acode status; Write-Host "Exit code: $LASTEXITCODE"

# 2. Check with different formats
acode status --format text; Write-Host "Text: $LASTEXITCODE"
acode status --format json; Write-Host "JSON: $LASTEXITCODE"

# 3. Test in subshell
powershell -Command "acode status; exit $LASTEXITCODE"
```

**Solutions:**

```csharp
// Solution 1: Explicit exit code mapping
public sealed class HealthStatusExitCodeMapper
{
    private static readonly Dictionary<HealthStatus, int> ExitCodes = new()
    {
        [HealthStatus.Healthy] = 0,
        [HealthStatus.Degraded] = 1,
        [HealthStatus.Unhealthy] = 2
    };
    
    public int MapToExitCode(HealthStatus status)
    {
        return ExitCodes.TryGetValue(status, out var code) ? code : 3;
    }
}

// Solution 2: Ensure all code paths set exit code
public sealed class StatusCommand
{
    public async Task<int> ExecuteAsync(StatusOptions options, CancellationToken ct)
    {
        try
        {
            var result = await _healthRegistry.CheckAllAsync(ct);
            
            // Output result
            await _outputFormatter.WriteAsync(result, options.Format, Console.Out);
            
            // ALWAYS return mapped exit code
            return _exitCodeMapper.MapToExitCode(result.AggregateStatus);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            return 3; // Error exit code
        }
    }
}

// Solution 3: CI-friendly wrapper script
// scripts/health-check.ps1
$result = & acode status --format json
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    Write-Error "Health check failed with status: $exitCode"
    exit $exitCode
}

exit 0
```

**Prevention:**
- Document exit code convention clearly in help text
- Test exit codes in CI pipeline for all status values
- Provide wrapper scripts for common CI systems

---

### Issue 6: Schema Version Mismatch Detection Fails

**Error Code:** ACODE-HLT-006

**Symptoms:**
- Health check shows "Healthy" but migrations are pending
- Schema version check returns wrong version
- Upgrade prompts appear despite recent migration

**Root Cause Analysis:**
```
┌────────────────────────────────────────────────────────┐
│ DIAGNOSTIC: Schema Version Detection                   │
├────────────────────────────────────────────────────────┤
│ 1. Metadata table doesn't exist (first-time setup)     │
│ 2. Multiple databases with different versions          │
│ 3. Version number parsing error (format mismatch)      │
│ 4. Cache returning stale version                       │
│ 5. Read connection to wrong database file              │
└────────────────────────────────────────────────────────┘
```

**Diagnostic Steps:**
```powershell
# 1. Check metadata table directly
acode diagnostics --section schema

# 2. Compare expected vs actual version
acode status --verbose | Select-String "schema"

# 3. Verify database path
acode config show --key database.path
acode diagnostics --section storage
```

**Solutions:**

```csharp
// Solution 1: Robust schema version check
public sealed class SchemaVersionCheck : IHealthCheck
{
    public string Name => "SchemaVersion";
    
    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct)
    {
        try
        {
            // Check if metadata table exists
            var tableExists = await _db.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__acode_migrations'",
                ct);
            
            if (tableExists == 0)
            {
                return new HealthCheckResult(Name, HealthStatus.Degraded, TimeSpan.Zero,
                    "Migrations table not found - database may need initialization",
                    new Dictionary<string, object>
                    {
                        ["expected_version"] = _expectedVersion,
                        ["actual_version"] = "N/A",
                        ["needs_init"] = true
                    });
            }
            
            var version = await _db.QueryFirstOrDefaultAsync<string>(
                "SELECT version FROM __acode_migrations ORDER BY applied_at DESC LIMIT 1",
                ct);
            
            var isMatch = version == _expectedVersion;
            
            return new HealthCheckResult(
                Name,
                isMatch ? HealthStatus.Healthy : HealthStatus.Degraded,
                TimeSpan.Zero,
                isMatch ? "Schema version matches" : $"Schema mismatch: expected {_expectedVersion}, found {version}",
                new Dictionary<string, object>
                {
                    ["expected_version"] = _expectedVersion,
                    ["actual_version"] = version ?? "none",
                    ["needs_migration"] = !isMatch
                });
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(Name, HealthStatus.Unhealthy, TimeSpan.Zero,
                $"Schema check failed: {ex.Message}");
        }
    }
}
```

**Prevention:**
- Include schema version in every diagnostic output
- Clear version cache after migration runs
- Verify database path before version check

---

### Issue 7: Storage Health Check Incorrect on Network Drives

**Error Code:** ACODE-HLT-007

**Symptoms:**
- Disk space shows 0 bytes available on network share
- Storage check fails on UNC paths
- Incorrect drive letter mapping

**Root Cause Analysis:**
```
┌────────────────────────────────────────────────────────┐
│ DIAGNOSTIC: Network Storage Detection                  │
├────────────────────────────────────────────────────────┤
│ 1. DriveInfo doesn't support UNC paths directly        │
│ 2. Mapped drive disconnected/stale                     │
│ 3. Permissions insufficient to read space              │
│ 4. Quota vs actual space mismatch                      │
│ 5. Cloud storage provider sync state                   │
└────────────────────────────────────────────────────────┘
```

**Solutions:**

```csharp
// Solution: Cross-platform storage check with UNC support
public sealed class CrossPlatformStorageCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct)
    {
        var dbPath = _configuration.DatabasePath;
        
        try
        {
            long availableBytes, totalBytes;
            
            if (dbPath.StartsWith(@"\\") || dbPath.StartsWith("//"))
            {
                // UNC path - use WMI or directory-based approach
                (availableBytes, totalBytes) = await GetUncPathSpaceAsync(dbPath, ct);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var root = Path.GetPathRoot(dbPath);
                var drive = new DriveInfo(root!);
                availableBytes = drive.AvailableFreeSpace;
                totalBytes = drive.TotalSize;
            }
            else
            {
                // Unix: use statfs via P/Invoke or df command
                (availableBytes, totalBytes) = await GetUnixSpaceAsync(dbPath, ct);
            }
            
            var percentFree = (double)availableBytes / totalBytes * 100;
            var status = percentFree switch
            {
                < 5 => HealthStatus.Unhealthy,
                < 15 => HealthStatus.Degraded,
                _ => HealthStatus.Healthy
            };
            
            return new HealthCheckResult(Name, status, TimeSpan.Zero,
                $"Storage: {FormatBytes(availableBytes)} available ({percentFree:F1}% free)",
                new Dictionary<string, object>
                {
                    ["available_bytes"] = availableBytes,
                    ["total_bytes"] = totalBytes,
                    ["percent_free"] = percentFree,
                    ["is_network"] = dbPath.StartsWith(@"\\") || dbPath.StartsWith("//")
                });
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(Name, HealthStatus.Degraded, TimeSpan.Zero,
                $"Could not determine storage space: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["path"] = RedactPath(dbPath)
                });
        }
    }
    
    private static async Task<(long available, long total)> GetUncPathSpaceAsync(
        string uncPath, CancellationToken ct)
    {
        // Create a temp file to get space info
        var testDir = Path.GetDirectoryName(uncPath)!;
        if (!Directory.Exists(testDir))
            throw new DirectoryNotFoundException($"UNC path not accessible");
        
        // Use GetDiskFreeSpaceEx via P/Invoke
        if (!GetDiskFreeSpaceEx(testDir, out var free, out var total, out _))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        
        return ((long)free, (long)total);
    }
    
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetDiskFreeSpaceEx(
        string lpDirectoryName,
        out ulong lpFreeBytesAvailable,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);
}
```

**Prevention:**
- Test storage checks on all expected deployment scenarios
- Graceful degradation for inaccessible storage metrics
- Document network storage limitations

---

## Testing Requirements

### Unit Tests

```csharp
// Tests/Acode.Application.Tests/Health/HealthCheckRegistryTests.cs
namespace Acode.Application.Tests.Health;

public sealed class HealthCheckRegistryTests
{
    private readonly Mock<ILogger<HealthCheckRegistry>> _loggerMock;
    private readonly Mock<IHealthCheckCache> _cacheMock;
    private readonly HealthCheckRegistry _registry;
    
    public HealthCheckRegistryTests()
    {
        _loggerMock = new Mock<ILogger<HealthCheckRegistry>>();
        _cacheMock = new Mock<IHealthCheckCache>();
        _registry = new HealthCheckRegistry(_loggerMock.Object, _cacheMock.Object);
    }
    
    [Fact]
    public void Register_AddsHealthCheck_ToRegistry()
    {
        // Arrange
        var check = CreateMockCheck("TestCheck", HealthStatus.Healthy);
        
        // Act
        _registry.Register(check);
        
        // Assert
        _registry.GetRegisteredChecks().Should().Contain(c => c.Name == "TestCheck");
    }
    
    [Fact]
    public void Register_DuplicateName_IsIdempotent()
    {
        // Arrange
        var check1 = CreateMockCheck("TestCheck", HealthStatus.Healthy);
        var check2 = CreateMockCheck("TestCheck", HealthStatus.Unhealthy);
        
        // Act
        _registry.Register(check1);
        _registry.Register(check2);
        
        // Assert
        _registry.GetRegisteredChecks().Should().HaveCount(1);
    }
    
    [Fact]
    public async Task CheckAllAsync_RunsAllChecksInParallel()
    {
        // Arrange
        var slowCheck1 = CreateSlowCheck("Slow1", 50);
        var slowCheck2 = CreateSlowCheck("Slow2", 50);
        var slowCheck3 = CreateSlowCheck("Slow3", 50);
        
        _registry.Register(slowCheck1);
        _registry.Register(slowCheck2);
        _registry.Register(slowCheck3);
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _registry.CheckAllAsync(CancellationToken.None);
        stopwatch.Stop();
        
        // Assert - parallel should complete in ~50ms, not ~150ms
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
        result.Results.Should().HaveCount(3);
    }
    
    [Theory]
    [InlineData(HealthStatus.Healthy, HealthStatus.Healthy, HealthStatus.Healthy)]
    [InlineData(HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Degraded)]
    [InlineData(HealthStatus.Healthy, HealthStatus.Unhealthy, HealthStatus.Unhealthy)]
    [InlineData(HealthStatus.Degraded, HealthStatus.Unhealthy, HealthStatus.Unhealthy)]
    public async Task CheckAllAsync_AggregatesStatus_WorstCaseWins(
        HealthStatus status1, HealthStatus status2, HealthStatus expected)
    {
        // Arrange
        _registry.Register(CreateMockCheck("Check1", status1));
        _registry.Register(CreateMockCheck("Check2", status2));
        
        // Act
        var result = await _registry.CheckAllAsync(CancellationToken.None);
        
        // Assert
        result.AggregateStatus.Should().Be(expected);
    }
    
    [Fact]
    public async Task CheckAllAsync_ContinuesAfterCheckFailure()
    {
        // Arrange
        var throwingCheck = new Mock<IHealthCheck>();
        throwingCheck.Setup(c => c.Name).Returns("Throwing");
        throwingCheck.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Boom"));
        
        _registry.Register(throwingCheck.Object);
        _registry.Register(CreateMockCheck("Normal", HealthStatus.Healthy));
        
        // Act
        var result = await _registry.CheckAllAsync(CancellationToken.None);
        
        // Assert
        result.Results.Should().HaveCount(2);
        result.Results.First(r => r.Name == "Throwing").Status.Should().Be(HealthStatus.Unhealthy);
        result.Results.First(r => r.Name == "Normal").Status.Should().Be(HealthStatus.Healthy);
    }
    
    [Fact]
    public async Task CheckAllAsync_ReturnsCachedResult_WhenAvailable()
    {
        // Arrange
        var cachedResult = new CompositeHealthResult(HealthStatus.Healthy, new List<HealthCheckResult>());
        _cacheMock.Setup(c => c.TryGetCached(out cachedResult)).Returns(true);
        
        // Act
        var result = await _registry.CheckAllAsync(CancellationToken.None);
        
        // Assert
        result.Should().BeSameAs(cachedResult);
        _loggerMock.VerifyLog(LogLevel.Debug, "Returning cached health result");
    }
    
    private static IHealthCheck CreateMockCheck(string name, HealthStatus status)
    {
        var mock = new Mock<IHealthCheck>();
        mock.Setup(c => c.Name).Returns(name);
        mock.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealthCheckResult(name, status, TimeSpan.FromMilliseconds(10)));
        return mock.Object;
    }
    
    private static IHealthCheck CreateSlowCheck(string name, int delayMs)
    {
        var mock = new Mock<IHealthCheck>();
        mock.Setup(c => c.Name).Returns(name);
        mock.Setup(c => c.CheckAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(delayMs);
                return new HealthCheckResult(name, HealthStatus.Healthy, TimeSpan.FromMilliseconds(delayMs));
            });
        return mock.Object;
    }
}
```

```csharp
// Tests/Acode.Application.Tests/Health/DatabaseConnectivityCheckTests.cs
namespace Acode.Application.Tests.Health;

public sealed class DatabaseConnectivityCheckTests
{
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock;
    private readonly Mock<ILogger<DatabaseConnectivityCheck>> _loggerMock;
    private readonly DatabaseConnectivityCheck _check;
    
    public DatabaseConnectivityCheckTests()
    {
        _connectionFactoryMock = new Mock<IDbConnectionFactory>();
        _loggerMock = new Mock<ILogger<DatabaseConnectivityCheck>>();
        _check = new DatabaseConnectivityCheck(
            _connectionFactoryMock.Object,
            _loggerMock.Object,
            Options.Create(new DatabaseConnectivityCheckOptions()));
    }
    
    [Fact]
    public async Task CheckAsync_ReturnsHealthy_WhenDatabaseResponds()
    {
        // Arrange
        var connectionMock = new Mock<IDbConnection>();
        connectionMock.Setup(c => c.State).Returns(ConnectionState.Open);
        _connectionFactoryMock.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(connectionMock.Object);
        
        // Act
        var result = await _check.CheckAsync(CancellationToken.None);
        
        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Details.Should().ContainKey("connection_time_ms");
    }
    
    [Fact]
    public async Task CheckAsync_ReturnsUnhealthy_WhenConnectionFails()
    {
        // Arrange
        _connectionFactoryMock.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SqliteException("Database is locked", 5));
        
        // Act
        var result = await _check.CheckAsync(CancellationToken.None);
        
        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("locked");
        result.Suggestion.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public async Task CheckAsync_ReturnsUnhealthy_WhenTimeout()
    {
        // Arrange
        _connectionFactoryMock.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) =>
            {
                await Task.Delay(5000, ct); // Longer than timeout
                return new Mock<IDbConnection>().Object;
            });
        
        var options = Options.Create(new DatabaseConnectivityCheckOptions 
        { 
            Timeout = TimeSpan.FromMilliseconds(50) 
        });
        var check = new DatabaseConnectivityCheck(
            _connectionFactoryMock.Object, _loggerMock.Object, options);
        
        // Act
        var result = await check.CheckAsync(CancellationToken.None);
        
        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("timeout");
    }
    
    [Fact]
    public async Task CheckAsync_ReturnsDegraded_WhenSlowButSuccessful()
    {
        // Arrange
        var connectionMock = new Mock<IDbConnection>();
        connectionMock.Setup(c => c.State).Returns(ConnectionState.Open);
        _connectionFactoryMock.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken _) =>
            {
                await Task.Delay(80); // Slow but within timeout
                return connectionMock.Object;
            });
        
        var options = Options.Create(new DatabaseConnectivityCheckOptions 
        { 
            Timeout = TimeSpan.FromMilliseconds(100),
            DegradedThreshold = TimeSpan.FromMilliseconds(50)
        });
        var check = new DatabaseConnectivityCheck(
            _connectionFactoryMock.Object, _loggerMock.Object, options);
        
        // Act
        var result = await check.CheckAsync(CancellationToken.None);
        
        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
    }
    
    [Fact]
    public async Task CheckAsync_IncludesDuration_InResult()
    {
        // Arrange
        var connectionMock = new Mock<IDbConnection>();
        connectionMock.Setup(c => c.State).Returns(ConnectionState.Open);
        _connectionFactoryMock.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(connectionMock.Object);
        
        // Act
        var result = await _check.CheckAsync(CancellationToken.None);
        
        // Assert
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }
}
```

```csharp
// Tests/Acode.Application.Tests/Health/SchemaVersionCheckTests.cs
namespace Acode.Application.Tests.Health;

public sealed class SchemaVersionCheckTests
{
    private readonly SqliteConnection _connection;
    private readonly SchemaVersionCheck _check;
    
    public SchemaVersionCheckTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        var options = Options.Create(new SchemaVersionCheckOptions 
        { 
            ExpectedVersion = "1.0.5" 
        });
        _check = new SchemaVersionCheck(_connection, options);
    }
    
    [Fact]
    public async Task CheckAsync_ReturnsHealthy_WhenVersionMatches()
    {
        // Arrange
        await CreateMigrationsTable(_connection, "1.0.5");
        
        // Act
        var result = await _check.CheckAsync(CancellationToken.None);
        
        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Details?["actual_version"].Should().Be("1.0.5");
    }
    
    [Fact]
    public async Task CheckAsync_ReturnsDegraded_WhenVersionMismatch()
    {
        // Arrange
        await CreateMigrationsTable(_connection, "1.0.3");
        
        // Act
        var result = await _check.CheckAsync(CancellationToken.None);
        
        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Details?["expected_version"].Should().Be("1.0.5");
        result.Details?["actual_version"].Should().Be("1.0.3");
        result.Details?["needs_migration"].Should().Be(true);
    }
    
    [Fact]
    public async Task CheckAsync_ReturnsDegraded_WhenTableMissing()
    {
        // Arrange - no migrations table created
        
        // Act
        var result = await _check.CheckAsync(CancellationToken.None);
        
        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Details?["needs_init"].Should().Be(true);
    }
    
    private static async Task CreateMigrationsTable(SqliteConnection connection, string version)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE __acode_migrations (
                version TEXT NOT NULL,
                applied_at TEXT NOT NULL
            );
            INSERT INTO __acode_migrations (version, applied_at) 
            VALUES (@version, datetime('now'));";
        cmd.Parameters.AddWithValue("@version", version);
        await cmd.ExecuteNonQueryAsync();
    }
}
```

```csharp
// Tests/Acode.Application.Tests/Health/StorageCheckTests.cs
namespace Acode.Application.Tests.Health;

public sealed class StorageCheckTests
{
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<ILogger<StorageCheck>> _loggerMock;
    private readonly StorageCheck _check;
    
    public StorageCheckTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _loggerMock = new Mock<ILogger<StorageCheck>>();
        
        var options = Options.Create(new StorageCheckOptions
        {
            DatabasePath = "/data/acode.db",
            LowSpaceThresholdPercent = 15,
            CriticalSpaceThresholdPercent = 5
        });
        
        _check = new StorageCheck(_fileSystemMock.Object, _loggerMock.Object, options);
    }
    
    [Fact]
    public async Task CheckAsync_ReturnsHealthy_WhenSpaceAboveThreshold()
    {
        // Arrange
        _fileSystemMock.Setup(fs => fs.GetDiskSpaceInfo(It.IsAny<string>()))
            .Returns(new DiskSpaceInfo(1_000_000_000, 500_000_000)); // 50% free
        
        // Act
        var result = await _check.CheckAsync(CancellationToken.None);
        
        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Details?["percent_free"].Should().Be(50.0);
    }
    
    [Fact]
    public async Task CheckAsync_ReturnsDegraded_WhenLowSpace()
    {
        // Arrange
        _fileSystemMock.Setup(fs => fs.GetDiskSpaceInfo(It.IsAny<string>()))
            .Returns(new DiskSpaceInfo(1_000_000_000, 100_000_000)); // 10% free
        
        // Act
        var result = await _check.CheckAsync(CancellationToken.None);
        
        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Suggestion.Should().Contain("free up space");
    }
    
    [Fact]
    public async Task CheckAsync_ReturnsUnhealthy_WhenCriticalSpace()
    {
        // Arrange
        _fileSystemMock.Setup(fs => fs.GetDiskSpaceInfo(It.IsAny<string>()))
            .Returns(new DiskSpaceInfo(1_000_000_000, 30_000_000)); // 3% free
        
        // Act
        var result = await _check.CheckAsync(CancellationToken.None);
        
        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
    
    [Fact]
    public async Task CheckAsync_IncludesDatabaseSize_InDetails()
    {
        // Arrange
        _fileSystemMock.Setup(fs => fs.GetDiskSpaceInfo(It.IsAny<string>()))
            .Returns(new DiskSpaceInfo(1_000_000_000, 500_000_000));
        _fileSystemMock.Setup(fs => fs.GetFileSize("/data/acode.db"))
            .Returns(50_000_000); // 50MB
        
        // Act
        var result = await _check.CheckAsync(CancellationToken.None);
        
        // Assert
        result.Details?["database_size_bytes"].Should().Be(50_000_000L);
    }
}
```

```csharp
// Tests/Acode.Application.Tests/Health/SyncQueueCheckTests.cs
namespace Acode.Application.Tests.Health;

public sealed class SyncQueueCheckTests
{
    private readonly Mock<ISyncQueueMetrics> _metricsMock;
    private readonly SyncQueueCheck _check;
    
    public SyncQueueCheckTests()
    {
        _metricsMock = new Mock<ISyncQueueMetrics>();
        var options = Options.Create(new SyncQueueCheckOptions
        {
            HighQueueThreshold = 100,
            CriticalQueueThreshold = 500,
            StalledMinutes = 10
        });
        _check = new SyncQueueCheck(_metricsMock.Object, options);
    }
    
    [Fact]
    public async Task CheckAsync_ReturnsHealthy_WhenQueueEmpty()
    {
        // Arrange
        _metricsMock.Setup(m => m.GetQueueDepthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _metricsMock.Setup(m => m.GetLastProcessedTimeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTime.UtcNow.AddMinutes(-1));
        
        // Act
        var result = await _check.CheckAsync(CancellationToken.None);
        
        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }
    
    [Fact]
    public async Task CheckAsync_ReturnsDegraded_WhenQueueHigh()
    {
        // Arrange
        _metricsMock.Setup(m => m.GetQueueDepthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(150);
        _metricsMock.Setup(m => m.GetLastProcessedTimeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTime.UtcNow.AddMinutes(-1));
        
        // Act
        var result = await _check.CheckAsync(CancellationToken.None);
        
        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Details?["queue_depth"].Should().Be(150);
    }
    
    [Fact]
    public async Task CheckAsync_ReturnsUnhealthy_WhenSyncStalled()
    {
        // Arrange
        _metricsMock.Setup(m => m.GetQueueDepthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);
        _metricsMock.Setup(m => m.GetLastProcessedTimeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTime.UtcNow.AddMinutes(-30)); // Stalled
        
        // Act
        var result = await _check.CheckAsync(CancellationToken.None);
        
        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("stalled");
    }
}
```

```csharp
// Tests/Acode.Application.Tests/Health/HealthOutputSanitizerTests.cs
namespace Acode.Application.Tests.Health;

public sealed class HealthOutputSanitizerTests
{
    private readonly HealthOutputSanitizer _sanitizer;
    
    public HealthOutputSanitizerTests()
    {
        _sanitizer = new HealthOutputSanitizer(
            Mock.Of<ILogger<HealthOutputSanitizer>>());
    }
    
    [Theory]
    [InlineData("Server=localhost;Password=secret123", "Server=[REDACTED];Password=[REDACTED]")]
    [InlineData("Data Source=db;User Id=admin;Pwd=hunter2", "Data Source=[REDACTED];User Id=[REDACTED];Pwd=[REDACTED]")]
    public void Sanitize_RedactsConnectionStrings(string input, string expected)
    {
        // Act
        var result = _sanitizer.Sanitize(input);
        
        // Assert
        result.Should().Be(expected);
    }
    
    [Theory]
    [InlineData(@"C:\Users\john.doe\acode\db.sqlite", @"C:\Users\[REDACTED]\acode\db.sqlite")]
    [InlineData("/home/john.doe/.acode/db.sqlite", "/home/[REDACTED]/.acode/db.sqlite")]
    public void Sanitize_RedactsUserPaths(string input, string expected)
    {
        // Act
        var result = _sanitizer.Sanitize(input);
        
        // Assert
        result.Should().Be(expected);
    }
    
    [Theory]
    [InlineData("api_key=sk-abc123xyz789", "api_key=[REDACTED]")]
    [InlineData("Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", "Authorization: Bearer [REDACTED]")]
    public void Sanitize_RedactsApiKeysAndTokens(string input, string expected)
    {
        // Act
        var result = _sanitizer.Sanitize(input);
        
        // Assert
        result.Should().Contain("[REDACTED]");
    }
    
    [Fact]
    public void SanitizeResult_AppliesSanitization_ToAllFields()
    {
        // Arrange
        var result = new HealthCheckResult(
            "Test",
            HealthStatus.Healthy,
            TimeSpan.Zero,
            "Error at C:\\Users\\admin\\file.db",
            new Dictionary<string, object>
            {
                ["connection"] = "Server=prod;Password=secret"
            });
        
        // Act
        var sanitized = _sanitizer.SanitizeResult(result);
        
        // Assert
        sanitized.Description.Should().Contain("[REDACTED]");
        sanitized.Details?["connection"].Should().Be("Server=[REDACTED];Password=[REDACTED]");
    }
}
```

### Integration Tests

```csharp
// Tests/Acode.Integration.Tests/Health/HealthCheckIntegrationTests.cs
namespace Acode.Integration.Tests.Health;

[Collection("Database")]
public sealed class HealthCheckIntegrationTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private readonly IHealthCheckRegistry _registry;
    
    public HealthCheckIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _registry = fixture.Services.GetRequiredService<IHealthCheckRegistry>();
    }
    
    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;
    
    [Fact]
    public async Task CheckAllAsync_WithRealDatabase_ReturnsHealthyStatus()
    {
        // Arrange - database is initialized by fixture
        
        // Act
        var result = await _registry.CheckAllAsync(CancellationToken.None);
        
        // Assert
        result.AggregateStatus.Should().Be(HealthStatus.Healthy);
        result.Results.Should().Contain(r => r.Name == "DatabaseConnectivity");
        result.Results.Should().Contain(r => r.Name == "SchemaVersion");
        result.Results.Should().Contain(r => r.Name == "Storage");
    }
    
    [Fact]
    public async Task CheckAllAsync_WithMissingMigrations_ReturnsDegradedStatus()
    {
        // Arrange
        await _fixture.SimulateSchemaMismatchAsync("0.9.0");
        
        // Act
        var result = await _registry.CheckAllAsync(CancellationToken.None);
        
        // Assert
        result.AggregateStatus.Should().Be(HealthStatus.Degraded);
        result.Results.First(r => r.Name == "SchemaVersion").Status
            .Should().Be(HealthStatus.Degraded);
    }
    
    [Fact]
    public async Task CheckAllAsync_WithLockedDatabase_ReturnsUnhealthyStatus()
    {
        // Arrange
        await using var lockingConnection = await _fixture.LockDatabaseAsync();
        
        // Act
        var result = await _registry.CheckAllAsync(CancellationToken.None);
        
        // Assert
        result.Results.First(r => r.Name == "DatabaseConnectivity").Status
            .Should().Be(HealthStatus.Unhealthy);
    }
    
    [Fact]
    public async Task CheckAllAsync_ReturnsValidDurations_ForAllChecks()
    {
        // Act
        var result = await _registry.CheckAllAsync(CancellationToken.None);
        
        // Assert
        result.Results.Should().AllSatisfy(r =>
        {
            r.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            r.Duration.Should().BeLessThan(TimeSpan.FromSeconds(5));
        });
    }
}
```

```csharp
// Tests/Acode.Integration.Tests/Health/DiagnosticsReportIntegrationTests.cs
namespace Acode.Integration.Tests.Health;

[Collection("Database")]
public sealed class DiagnosticsReportIntegrationTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private readonly IDiagnosticsReportBuilder _builder;
    
    public DiagnosticsReportIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _builder = fixture.Services.GetRequiredService<IDiagnosticsReportBuilder>();
    }
    
    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;
    
    [Fact]
    public async Task BuildAsync_IncludesAllSections()
    {
        // Act
        var report = await _builder.BuildAsync(CancellationToken.None);
        
        // Assert
        report.Sections.Should().ContainKey("database");
        report.Sections.Should().ContainKey("sync");
        report.Sections.Should().ContainKey("storage");
        report.Sections.Should().ContainKey("system");
    }
    
    [Fact]
    public async Task BuildAsync_DoesNotIncludeSensitiveData()
    {
        // Act
        var report = await _builder.BuildAsync(CancellationToken.None);
        var json = JsonSerializer.Serialize(report);
        
        // Assert
        json.Should().NotContainAny("password", "secret", "token", "apikey");
        json.Should().NotMatchRegex(@"C:\\Users\\[^\\]+\\"); // No user paths
    }
    
    [Fact]
    public async Task BuildAsync_CompletesWithinTimeout()
    {
        // Arrange
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var report = await _builder.BuildAsync(cts.Token);
        stopwatch.Stop();
        
        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
    }
    
    [Fact]
    public async Task BuildAsync_WithSectionFilter_ReturnsOnlyRequestedSection()
    {
        // Act
        var report = await _builder.BuildAsync(
            new DiagnosticsOptions { Sections = new[] { "database" } },
            CancellationToken.None);
        
        // Assert
        report.Sections.Should().ContainKey("database");
        report.Sections.Should().NotContainKey("sync");
        report.Sections.Should().NotContainKey("storage");
    }
}
```

### E2E Tests

```csharp
// Tests/Acode.E2E.Tests/Health/StatusCommandE2ETests.cs
namespace Acode.E2E.Tests.Health;

public sealed class StatusCommandE2ETests : E2ETestBase
{
    [Fact]
    public async Task StatusCommand_WithHealthyDatabase_ReturnsZeroExitCode()
    {
        // Arrange
        await InitializeHealthyDatabaseAsync();
        
        // Act
        var result = await ExecuteCommandAsync("acode status");
        
        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("Healthy");
    }
    
    [Fact]
    public async Task StatusCommand_WithDegradedStatus_ReturnsOneExitCode()
    {
        // Arrange
        await InitializeDatabaseWithSchemaMismatchAsync();
        
        // Act
        var result = await ExecuteCommandAsync("acode status");
        
        // Assert
        result.ExitCode.Should().Be(1);
        result.StandardOutput.Should().Contain("Degraded");
    }
    
    [Fact]
    public async Task StatusCommand_WithUnhealthyStatus_ReturnsTwoExitCode()
    {
        // Arrange
        await LockDatabaseAsync();
        
        // Act
        var result = await ExecuteCommandAsync("acode status");
        
        // Assert
        result.ExitCode.Should().Be(2);
        result.StandardOutput.Should().Contain("Unhealthy");
    }
    
    [Fact]
    public async Task StatusCommand_WithJsonFormat_ReturnsValidJson()
    {
        // Arrange
        await InitializeHealthyDatabaseAsync();
        
        // Act
        var result = await ExecuteCommandAsync("acode status --format json");
        
        // Assert
        result.ExitCode.Should().Be(0);
        var json = JsonDocument.Parse(result.StandardOutput);
        json.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
        json.RootElement.GetProperty("checks").GetArrayLength().Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task StatusCommand_WithVerbose_ShowsDetailedOutput()
    {
        // Arrange
        await InitializeHealthyDatabaseAsync();
        
        // Act
        var result = await ExecuteCommandAsync("acode status --verbose");
        
        // Assert
        result.StandardOutput.Should().Contain("DatabaseConnectivity");
        result.StandardOutput.Should().Contain("SchemaVersion");
        result.StandardOutput.Should().Contain("duration");
    }
    
    [Fact]
    public async Task DiagnosticsCommand_ShowsComprehensiveReport()
    {
        // Arrange
        await InitializeHealthyDatabaseAsync();
        
        // Act
        var result = await ExecuteCommandAsync("acode diagnostics");
        
        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("Database");
        result.StandardOutput.Should().Contain("Sync");
        result.StandardOutput.Should().Contain("Storage");
    }
    
    [Fact]
    public async Task DiagnosticsCommand_WithSectionFilter_ShowsOnlyThatSection()
    {
        // Arrange
        await InitializeHealthyDatabaseAsync();
        
        // Act
        var result = await ExecuteCommandAsync("acode diagnostics --section database");
        
        // Assert
        result.StandardOutput.Should().Contain("Database");
        result.StandardOutput.Should().NotContain("Sync Queue");
    }
    
    [Fact]
    public async Task DiagnosticsCommand_DoesNotExposeSecrets()
    {
        // Arrange
        await InitializeHealthyDatabaseAsync();
        
        // Act
        var result = await ExecuteCommandAsync("acode diagnostics --format json");
        
        // Assert
        result.StandardOutput.Should().NotContain("password");
        result.StandardOutput.Should().NotContain("secret");
        result.StandardOutput.Should().NotMatchRegex(@"[A-Za-z]:\\Users\\[^\\]+\\");
    }
}
```

### Performance Benchmarks

```csharp
// Tests/Acode.Benchmarks/Health/HealthCheckBenchmarks.cs
namespace Acode.Benchmarks.Health;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class HealthCheckBenchmarks
{
    private IHealthCheckRegistry _registry = null!;
    private IDiagnosticsReportBuilder _diagnosticsBuilder = null!;
    private IDbConnection _connection = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddAcodeHealthChecks();
        services.AddAcodeDiagnostics();
        
        _connection = new SqliteConnection("Data Source=benchmark.db");
        _connection.Open();
        
        // Initialize schema
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS __acode_migrations (version TEXT, applied_at TEXT)";
        cmd.ExecuteNonQuery();
        
        var provider = services.BuildServiceProvider();
        _registry = provider.GetRequiredService<IHealthCheckRegistry>();
        _diagnosticsBuilder = provider.GetRequiredService<IDiagnosticsReportBuilder>();
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _connection.Dispose();
        File.Delete("benchmark.db");
    }
    
    [Benchmark(Description = "Full health check (all components)")]
    public async Task<CompositeHealthResult> FullHealthCheck()
    {
        return await _registry.CheckAllAsync(CancellationToken.None);
    }
    
    [Benchmark(Description = "Database connectivity check only")]
    public async Task<HealthCheckResult> DatabaseConnectivityCheck()
    {
        var check = _registry.GetCheck("DatabaseConnectivity");
        return await check.CheckAsync(CancellationToken.None);
    }
    
    [Benchmark(Description = "Schema version check only")]
    public async Task<HealthCheckResult> SchemaVersionCheck()
    {
        var check = _registry.GetCheck("SchemaVersion");
        return await check.CheckAsync(CancellationToken.None);
    }
    
    [Benchmark(Description = "Full diagnostics report")]
    public async Task<DiagnosticsReport> FullDiagnosticsReport()
    {
        return await _diagnosticsBuilder.BuildAsync(CancellationToken.None);
    }
    
    [Benchmark(Description = "Single section diagnostics")]
    public async Task<DiagnosticsReport> SingleSectionDiagnostics()
    {
        return await _diagnosticsBuilder.BuildAsync(
            new DiagnosticsOptions { Sections = new[] { "database" } },
            CancellationToken.None);
    }
}

/*
Expected Results:
| Method                              | Mean      | Error    | StdDev   | Allocated |
|------------------------------------ |----------:|---------:|---------:|----------:|
| Full health check (all components)  |  45.23 ms | 1.22 ms  | 1.14 ms  |    12 KB  |
| Database connectivity check only    |   8.45 ms | 0.18 ms  | 0.17 ms  |     2 KB  |
| Schema version check only           |   5.12 ms | 0.12 ms  | 0.11 ms  |     1 KB  |
| Full diagnostics report             | 1,823 ms  | 35.4 ms  | 33.1 ms  |   256 KB  |
| Single section diagnostics          |  312.4 ms | 8.23 ms  | 7.70 ms  |    48 KB  |

Performance Targets:
- Full health check: < 100ms (PASS: 45ms)
- Individual probe: < 50ms (PASS: 8.45ms)
- Full diagnostics: < 5s (PASS: 1.8s)
*/
```

| Benchmark | Target | Maximum | Expected |
|-----------|--------|---------|----------|
| Full health check | 50ms | 100ms | ~45ms |
| Individual probe | 25ms | 50ms | ~8ms |
| Database connectivity | 10ms | 25ms | ~8ms |
| Schema version | 5ms | 15ms | ~5ms |
| Diagnostics (full) | 2s | 5s | ~1.8s |
| Diagnostics (single) | 500ms | 1s | ~312ms |
| JSON serialization | 3ms | 5ms | ~3ms |
| Cache hit | 0.1ms | 1ms | ~0.05ms |

---

## User Verification Steps

### Scenario 1: Basic Health Status Check

**Objective:** Verify that `acode status` shows overall system health with component breakdown.

**Prerequisites:**
- Acode installed and configured
- Database initialized with migrations applied

**Steps:**
```powershell
# 1. Run basic status command
acode status

# Expected output:
# ┌─────────────────────────────────────────────────────┐
# │ ACODE HEALTH STATUS                                 │
# ├─────────────────────────────────────────────────────┤
# │ Overall Status: ✓ Healthy                           │
# │ Checked: 4 components                               │
# │                                                     │
# │ Components:                                         │
# │   ✓ DatabaseConnectivity  (8ms)  - Connection OK   │
# │   ✓ SchemaVersion         (3ms)  - Version 1.0.5   │
# │   ✓ SyncQueue             (2ms)  - 0 items pending │
# │   ✓ Storage               (5ms)  - 45% free        │
# └─────────────────────────────────────────────────────┘

# 2. Verify exit code
$LASTEXITCODE
# Expected: 0
```

**Verification Checklist:**
- [ ] Overall status shows "Healthy" with green checkmark
- [ ] Each component listed with name, duration, and description
- [ ] Exit code is 0
- [ ] No error messages in output

---

### Scenario 2: JSON Output Format

**Objective:** Verify JSON output is valid and contains all required fields for machine consumption.

**Prerequisites:**
- Same as Scenario 1

**Steps:**
```powershell
# 1. Run status with JSON output
$json = acode status --format json

# 2. Parse and validate JSON structure
$status = $json | ConvertFrom-Json

# 3. Verify required fields exist
$status.status           # Should be "Healthy"
$status.timestamp        # Should be ISO 8601 timestamp
$status.checks.Count     # Should be 4 or more
$status.checks[0].name   # Should be component name
$status.checks[0].status # Should be "Healthy"/"Degraded"/"Unhealthy"
$status.checks[0].duration_ms # Should be number

# Expected JSON structure:
# {
#   "status": "Healthy",
#   "timestamp": "2024-01-15T10:30:00Z",
#   "total_duration_ms": 18,
#   "checks": [
#     {
#       "name": "DatabaseConnectivity",
#       "status": "Healthy",
#       "duration_ms": 8,
#       "description": "Connection OK",
#       "details": {
#         "connection_time_ms": 7.5
#       }
#     },
#     ...
#   ]
# }
```

**Verification Checklist:**
- [ ] Output is valid JSON (no parse errors)
- [ ] Has `status` field with expected value
- [ ] Has `timestamp` field with valid ISO 8601 format
- [ ] Has `checks` array with all components
- [ ] Each check has `name`, `status`, `duration_ms`
- [ ] Exit code matches status (0=Healthy, 1=Degraded, 2=Unhealthy)

---

### Scenario 3: Exit Codes for CI/CD Integration

**Objective:** Verify exit codes correctly map to health status for scripting.

**Prerequisites:**
- Ability to simulate different health states

**Steps:**
```powershell
# Test 1: Healthy status
acode status
Write-Host "Healthy exit code: $LASTEXITCODE"
# Expected: 0

# Test 2: Degraded status (schema mismatch)
# Simulate by rolling back one migration
acode migrate --rollback 1
acode status
Write-Host "Degraded exit code: $LASTEXITCODE"
# Expected: 1

# Restore schema
acode migrate --apply

# Test 3: Unhealthy status (lock database)
# In another terminal, lock the database
$lock = [System.IO.File]::Open("acode.db", [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
acode status
Write-Host "Unhealthy exit code: $LASTEXITCODE"
$lock.Dispose()
# Expected: 2

# Test 4: Use in CI script
acode status
if ($LASTEXITCODE -ne 0) {
    Write-Error "Health check failed - blocking deployment"
    exit $LASTEXITCODE
}
Write-Host "Health check passed - proceeding with deployment"
```

**Verification Checklist:**
- [ ] Healthy status returns exit code 0
- [ ] Degraded status returns exit code 1
- [ ] Unhealthy status returns exit code 2
- [ ] Exit codes work correctly in conditional scripts
- [ ] Non-zero exit codes propagate through pipelines

---

### Scenario 4: Full Diagnostics Report

**Objective:** Verify diagnostics command provides comprehensive troubleshooting information.

**Prerequisites:**
- Database with some activity history

**Steps:**
```powershell
# 1. Run full diagnostics
acode diagnostics

# Expected output:
# ╔════════════════════════════════════════════════════════════════╗
# ║ ACODE DIAGNOSTICS REPORT                                       ║
# ║ Generated: 2024-01-15 10:30:00 UTC                             ║
# ╠════════════════════════════════════════════════════════════════╣
# ║ DATABASE                                                       ║
# ╠════════════════════════════════════════════════════════════════╣
# ║ Path: [REDACTED]/acode.db                                      ║
# ║ Size: 12.4 MB                                                  ║
# ║ Schema Version: 1.0.5                                          ║
# ║ Connection Pool:                                               ║
# ║   Active: 2 / 10                                               ║
# ║   Available: 8                                                 ║
# ║   Peak: 5                                                      ║
# ╠════════════════════════════════════════════════════════════════╣
# ║ SYNC QUEUE                                                     ║
# ╠════════════════════════════════════════════════════════════════╣
# ║ Pending Items: 0                                               ║
# ║ Last Sync: 2 minutes ago                                       ║
# ║ Avg Processing Time: 45ms                                      ║
# ╠════════════════════════════════════════════════════════════════╣
# ║ STORAGE                                                        ║
# ╠════════════════════════════════════════════════════════════════╣
# ║ Database Size: 12.4 MB                                         ║
# ║ Available Space: 45.2 GB (45%)                                 ║
# ║ Growth Rate: 1.2 MB/day                                        ║
# ╚════════════════════════════════════════════════════════════════╝

# 2. Verify no sensitive data
acode diagnostics | Select-String -Pattern "password|secret|token"
# Expected: No matches

# 3. Verify user paths are redacted
acode diagnostics | Select-String -Pattern "Users\\[^\\]+"
# Expected: No matches (paths should show [REDACTED])
```

**Verification Checklist:**
- [ ] Report includes Database section with size, version, pool stats
- [ ] Report includes Sync Queue section with queue depth and timing
- [ ] Report includes Storage section with space and growth rate
- [ ] No passwords, secrets, or tokens visible
- [ ] User paths are redacted (show [REDACTED])
- [ ] Connection strings not exposed

---

### Scenario 5: Section-Specific Diagnostics

**Objective:** Verify ability to request specific diagnostic sections.

**Steps:**
```powershell
# 1. Database section only
acode diagnostics --section database

# Expected: Only database-related information
# ╔════════════════════════════════════════════════════════════════╗
# ║ DATABASE DIAGNOSTICS                                           ║
# ╠════════════════════════════════════════════════════════════════╣
# ║ Path: [REDACTED]/acode.db                                      ║
# ║ Size: 12.4 MB                                                  ║
# ║ ...                                                            ║
# ╚════════════════════════════════════════════════════════════════╝

# 2. Sync section only
acode diagnostics --section sync

# 3. Storage section only
acode diagnostics --section storage

# 4. Multiple sections
acode diagnostics --section database,sync
```

**Verification Checklist:**
- [ ] `--section database` shows only database info
- [ ] `--section sync` shows only sync queue info
- [ ] `--section storage` shows only storage info
- [ ] Multiple sections can be combined with commas
- [ ] Invalid section name shows helpful error

---

### Scenario 6: Verbose Mode

**Objective:** Verify verbose output provides additional detail for troubleshooting.

**Steps:**
```powershell
# 1. Run status with verbose flag
acode status --verbose

# Expected additional output:
# ┌─────────────────────────────────────────────────────┐
# │ ACODE HEALTH STATUS (VERBOSE)                       │
# ├─────────────────────────────────────────────────────┤
# │ Overall Status: ✓ Healthy                           │
# │                                                     │
# │ ✓ DatabaseConnectivity                              │
# │   Duration: 8.23ms                                  │
# │   Description: Connection established successfully  │
# │   Details:                                          │
# │     connection_time_ms: 7.5                         │
# │     pool_active: 2                                  │
# │     pool_available: 8                               │
# │                                                     │
# │ ✓ SchemaVersion                                     │
# │   Duration: 3.12ms                                  │
# │   Description: Schema version 1.0.5 matches         │
# │   Details:                                          │
# │     expected_version: 1.0.5                         │
# │     actual_version: 1.0.5                           │
# │     needs_migration: false                          │
# │ ...                                                 │
# └─────────────────────────────────────────────────────┘

# 2. Verbose diagnostics
acode diagnostics --verbose
```

**Verification Checklist:**
- [ ] Verbose shows detailed timing for each check
- [ ] Verbose shows internal details dictionary
- [ ] Verbose shows suggestions where applicable
- [ ] Verbose doesn't expose any additional sensitive data

---

### Scenario 7: Degraded Status Detection

**Objective:** Verify system correctly identifies degraded conditions.

**Steps:**
```powershell
# 1. Simulate high sync queue
# (Requires test data or simulation mode)
acode test-mode --simulate-queue-depth 150

acode status
# Expected: Status shows Degraded (yellow warning)
# ┌─────────────────────────────────────────────────────┐
# │ Overall Status: ⚠ Degraded                          │
# │                                                     │
# │   ✓ DatabaseConnectivity  - Connection OK           │
# │   ⚠ SyncQueue             - 150 items pending       │
# │     → Sync queue depth above threshold (100)        │
# │   ✓ Storage               - 45% free                │
# └─────────────────────────────────────────────────────┘

# 2. Verify exit code
$LASTEXITCODE
# Expected: 1

# 3. Verify suggestion is actionable
# Output should include: "Run 'acode sync --process' to clear queue"
```

**Verification Checklist:**
- [ ] Overall status shows Degraded with warning indicator
- [ ] Specific component causing degradation is highlighted
- [ ] Actionable suggestion provided
- [ ] Exit code is 1
- [ ] Other healthy components still show as healthy

---

### Scenario 8: Unhealthy Status with Suggestions

**Objective:** Verify unhealthy conditions provide clear guidance for resolution.

**Steps:**
```powershell
# 1. Simulate low disk space
# (Create large temp file to fill disk or use test mode)
acode test-mode --simulate-disk-space 3

acode status
# Expected: Status shows Unhealthy (red error)
# ┌─────────────────────────────────────────────────────┐
# │ Overall Status: ✗ Unhealthy                         │
# │                                                     │
# │   ✓ DatabaseConnectivity  - Connection OK           │
# │   ✗ Storage               - 3% free (CRITICAL)      │
# │     → Available space critically low                │
# │     → Suggestion: Free up disk space or move        │
# │       database to larger volume                     │
# └─────────────────────────────────────────────────────┘

# 2. Verify exit code
$LASTEXITCODE
# Expected: 2

# 3. Verify diagnostics shows details
acode diagnostics --section storage
# Should show current usage, path, suggestions for cleanup
```

**Verification Checklist:**
- [ ] Overall status shows Unhealthy with error indicator
- [ ] Critical component clearly identified
- [ ] Actionable suggestion provided
- [ ] Exit code is 2
- [ ] Diagnostics provides more detail for resolution

---

### Scenario 9: Cache Behavior Verification

**Objective:** Verify health check caching works correctly.

**Steps:**
```powershell
# 1. Run status twice in quick succession
$start = Get-Date
acode status
$first = (Get-Date) - $start

$start = Get-Date
acode status
$second = (Get-Date) - $start

# Second call should be much faster (cached)
Write-Host "First call: $($first.TotalMilliseconds)ms"
Write-Host "Second call: $($second.TotalMilliseconds)ms"
# Expected: Second call < 5ms if cached

# 2. Force fresh check
acode status --no-cache
# Should take same time as first call

# 3. Verify cache TTL (wait 35 seconds)
Start-Sleep -Seconds 35
acode status
# Should take same time as first call (cache expired)
```

**Verification Checklist:**
- [ ] Second call within TTL is significantly faster
- [ ] `--no-cache` flag forces fresh check
- [ ] Cache expires after TTL (default 30s)
- [ ] Cached results show same data as fresh

---

### Scenario 10: Network Drive Storage Check

**Objective:** Verify storage checks work on network paths (Windows-specific).

**Steps:**
```powershell
# 1. Configure database on network share
acode config set database.path "\\\\server\\share\\acode.db"

# 2. Run status
acode status

# Expected: Storage check should work (or gracefully degrade)
# ┌─────────────────────────────────────────────────────┐
# │   ✓ Storage  - Network storage                      │
# │     Available: 120 GB                               │
# │     Note: Growth rate not available for network     │
# └─────────────────────────────────────────────────────┘

# 3. If network unavailable
acode status
# Expected: Degraded storage check, not failure
# ┌─────────────────────────────────────────────────────┐
# │   ⚠ Storage  - Network path unreachable             │
# │     → Cannot determine available space              │
# │     → Suggestion: Check network connectivity        │
# └─────────────────────────────────────────────────────┘
```

**Verification Checklist:**
- [ ] UNC paths are handled correctly
- [ ] Space calculation works for network drives
- [ ] Graceful degradation if network unavailable
- [ ] User paths in network shares are still redacted

---

## Implementation Prompt

### File Structure

```
src/Acode.Application/
├── Health/
│   ├── IHealthCheck.cs           # Core interface for health checks
│   ├── IHealthCheckRegistry.cs   # Registry for managing checks
│   ├── IHealthCheckCache.cs      # Caching abstraction
│   ├── HealthCheckResult.cs      # Result value object
│   ├── CompositeHealthResult.cs  # Aggregated results
│   ├── HealthStatus.cs           # Status enum
│   └── IDiagnosticsReportBuilder.cs  # Diagnostics interface
│
src/Acode.Infrastructure/
├── Health/
│   ├── HealthCheckRegistry.cs    # Registry implementation
│   ├── HealthCheckCache.cs       # In-memory cache implementation
│   ├── HealthOutputSanitizer.cs  # Security sanitization
│   ├── HealthCheckRateLimiter.cs # Rate limiting
│   ├── DiagnosticsReportBuilder.cs  # Report builder
│   ├── Checks/
│   │   ├── DatabaseConnectivityCheck.cs  # DB connection check
│   │   ├── SchemaVersionCheck.cs         # Schema version check
│   │   ├── ConnectionPoolCheck.cs        # Pool statistics
│   │   ├── SyncQueueCheck.cs             # Sync queue depth
│   │   └── StorageCheck.cs               # Disk space check
│   └── Diagnostics/
│       ├── DatabaseDiagnosticsSection.cs
│       ├── SyncDiagnosticsSection.cs
│       └── StorageDiagnosticsSection.cs
│
src/Acode.Cli/
├── Commands/
│   ├── StatusCommand.cs          # acode status command
│   └── DiagnosticsCommand.cs     # acode diagnostics command
└── Formatters/
    ├── TextHealthFormatter.cs    # Human-readable output
    └── JsonHealthFormatter.cs    # JSON output
```

### Core Domain Models

```csharp
// Application/Health/HealthStatus.cs
namespace Acode.Application.Health;

/// <summary>
/// Represents the health status of a component or the overall system.
/// Status values are ordered by severity: Healthy < Degraded < Unhealthy.
/// </summary>
public enum HealthStatus
{
    /// <summary>Component is functioning normally.</summary>
    Healthy = 0,
    
    /// <summary>Component is functioning but with warnings.</summary>
    Degraded = 1,
    
    /// <summary>Component is not functioning correctly.</summary>
    Unhealthy = 2
}
```

```csharp
// Application/Health/HealthCheckResult.cs
namespace Acode.Application.Health;

/// <summary>
/// Immutable result from a single health check execution.
/// </summary>
public sealed record HealthCheckResult
{
    /// <summary>Unique name identifying this health check.</summary>
    public required string Name { get; init; }
    
    /// <summary>Current health status of the component.</summary>
    public required HealthStatus Status { get; init; }
    
    /// <summary>Time taken to execute the health check.</summary>
    public required TimeSpan Duration { get; init; }
    
    /// <summary>Human-readable description of the status.</summary>
    public string? Description { get; init; }
    
    /// <summary>Additional details for debugging or monitoring.</summary>
    public IReadOnlyDictionary<string, object>? Details { get; init; }
    
    /// <summary>Actionable suggestion if status is not Healthy.</summary>
    public string? Suggestion { get; init; }
    
    /// <summary>Error code if an error occurred.</summary>
    public string? ErrorCode { get; init; }
    
    /// <summary>Timestamp when check was executed.</summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>Creates a healthy result with the given description.</summary>
    public static HealthCheckResult Healthy(string name, TimeSpan duration, string? description = null)
        => new()
        {
            Name = name,
            Status = HealthStatus.Healthy,
            Duration = duration,
            Description = description
        };
    
    /// <summary>Creates a degraded result with suggestion.</summary>
    public static HealthCheckResult Degraded(
        string name, TimeSpan duration, string description, string suggestion)
        => new()
        {
            Name = name,
            Status = HealthStatus.Degraded,
            Duration = duration,
            Description = description,
            Suggestion = suggestion
        };
    
    /// <summary>Creates an unhealthy result with error code.</summary>
    public static HealthCheckResult Unhealthy(
        string name, TimeSpan duration, string description, string errorCode, string suggestion)
        => new()
        {
            Name = name,
            Status = HealthStatus.Unhealthy,
            Duration = duration,
            Description = description,
            ErrorCode = errorCode,
            Suggestion = suggestion
        };
}
```

```csharp
// Application/Health/CompositeHealthResult.cs
namespace Acode.Application.Health;

/// <summary>
/// Aggregated health check results from all registered checks.
/// </summary>
public sealed record CompositeHealthResult
{
    /// <summary>Overall system health (worst-case aggregation).</summary>
    public required HealthStatus AggregateStatus { get; init; }
    
    /// <summary>Individual check results.</summary>
    public required IReadOnlyList<HealthCheckResult> Results { get; init; }
    
    /// <summary>Total time to execute all checks.</summary>
    public required TimeSpan TotalDuration { get; init; }
    
    /// <summary>Timestamp when checks were executed.</summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>Whether result came from cache.</summary>
    public bool IsCached { get; init; }
    
    /// <summary>Aggregates status from all results (worst wins).</summary>
    public static HealthStatus Aggregate(IEnumerable<HealthCheckResult> results)
    {
        var statuses = results.Select(r => r.Status).ToList();
        if (!statuses.Any()) return HealthStatus.Healthy;
        return (HealthStatus)statuses.Max(s => (int)s);
    }
}
```

### Core Interfaces

```csharp
// Application/Health/IHealthCheck.cs
namespace Acode.Application.Health;

/// <summary>
/// Defines a single health check that can be registered with the system.
/// Health checks should be lightweight and complete quickly (&lt;50ms).
/// </summary>
public interface IHealthCheck
{
    /// <summary>Unique name for this health check.</summary>
    string Name { get; }
    
    /// <summary>Category for grouping related checks.</summary>
    string Category => "General";
    
    /// <summary>Timeout for this specific check.</summary>
    TimeSpan Timeout => TimeSpan.FromMilliseconds(50);
    
    /// <summary>
    /// Executes the health check and returns the result.
    /// Implementations MUST NOT throw exceptions; return Unhealthy status instead.
    /// </summary>
    Task<HealthCheckResult> CheckAsync(CancellationToken ct);
}
```

```csharp
// Application/Health/IHealthCheckRegistry.cs
namespace Acode.Application.Health;

/// <summary>
/// Registry for managing and executing health checks.
/// Supports parallel execution with caching.
/// </summary>
public interface IHealthCheckRegistry
{
    /// <summary>Registers a health check with the system.</summary>
    void Register(IHealthCheck check);
    
    /// <summary>Gets all registered health checks.</summary>
    IReadOnlyList<IHealthCheck> GetRegisteredChecks();
    
    /// <summary>Gets a specific check by name.</summary>
    IHealthCheck? GetCheck(string name);
    
    /// <summary>Executes all registered checks in parallel.</summary>
    Task<CompositeHealthResult> CheckAllAsync(CancellationToken ct);
    
    /// <summary>Executes checks in a specific category.</summary>
    Task<CompositeHealthResult> CheckCategoryAsync(string category, CancellationToken ct);
}
```

```csharp
// Application/Health/IHealthCheckCache.cs
namespace Acode.Application.Health;

/// <summary>
/// Cache for health check results to prevent hammering.
/// </summary>
public interface IHealthCheckCache
{
    /// <summary>Attempts to get a cached composite result.</summary>
    bool TryGetCached(out CompositeHealthResult? result);
    
    /// <summary>Attempts to get a cached individual result.</summary>
    bool TryGetCached(string checkName, out HealthCheckResult? result);
    
    /// <summary>Caches a composite result.</summary>
    void Cache(CompositeHealthResult result);
    
    /// <summary>Caches an individual result.</summary>
    void Cache(HealthCheckResult result);
    
    /// <summary>Clears all cached results.</summary>
    void Clear();
}
```

### Infrastructure Implementations

```csharp
// Infrastructure/Health/HealthCheckRegistry.cs
namespace Acode.Infrastructure.Health;

public sealed class HealthCheckRegistry : IHealthCheckRegistry
{
    private readonly ConcurrentDictionary<string, IHealthCheck> _checks = new();
    private readonly IHealthCheckCache _cache;
    private readonly IHealthOutputSanitizer _sanitizer;
    private readonly ILogger<HealthCheckRegistry> _logger;
    private readonly HealthCheckSettings _settings;
    
    public HealthCheckRegistry(
        IHealthCheckCache cache,
        IHealthOutputSanitizer sanitizer,
        ILogger<HealthCheckRegistry> logger,
        IOptions<HealthCheckSettings> settings)
    {
        _cache = cache;
        _sanitizer = sanitizer;
        _logger = logger;
        _settings = settings.Value;
    }
    
    public void Register(IHealthCheck check)
    {
        if (_checks.TryAdd(check.Name, check))
        {
            _logger.LogDebug("Registered health check: {Name}", check.Name);
        }
        else
        {
            _logger.LogWarning("Health check {Name} already registered", check.Name);
        }
    }
    
    public IReadOnlyList<IHealthCheck> GetRegisteredChecks() 
        => _checks.Values.ToList().AsReadOnly();
    
    public IHealthCheck? GetCheck(string name) 
        => _checks.GetValueOrDefault(name);
    
    public async Task<CompositeHealthResult> CheckAllAsync(CancellationToken ct)
    {
        // Check cache first
        if (_settings.EnableCaching && _cache.TryGetCached(out var cached) && cached is not null)
        {
            _logger.LogDebug("Returning cached health result");
            return cached with { IsCached = true };
        }
        
        var stopwatch = Stopwatch.StartNew();
        
        // Execute all checks in parallel
        var tasks = _checks.Values.Select(check => ExecuteCheckSafeAsync(check, ct));
        var results = await Task.WhenAll(tasks);
        
        stopwatch.Stop();
        
        var composite = new CompositeHealthResult
        {
            AggregateStatus = CompositeHealthResult.Aggregate(results),
            Results = results.ToList().AsReadOnly(),
            TotalDuration = stopwatch.Elapsed,
            IsCached = false
        };
        
        // Cache the result
        if (_settings.EnableCaching)
        {
            _cache.Cache(composite);
        }
        
        return composite;
    }
    
    public async Task<CompositeHealthResult> CheckCategoryAsync(string category, CancellationToken ct)
    {
        var checks = _checks.Values.Where(c => c.Category == category);
        var stopwatch = Stopwatch.StartNew();
        
        var tasks = checks.Select(check => ExecuteCheckSafeAsync(check, ct));
        var results = await Task.WhenAll(tasks);
        
        stopwatch.Stop();
        
        return new CompositeHealthResult
        {
            AggregateStatus = CompositeHealthResult.Aggregate(results),
            Results = results.ToList().AsReadOnly(),
            TotalDuration = stopwatch.Elapsed
        };
    }
    
    private async Task<HealthCheckResult> ExecuteCheckSafeAsync(
        IHealthCheck check, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(check.Timeout);
            
            var result = await check.CheckAsync(cts.Token);
            stopwatch.Stop();
            
            // Sanitize output
            return _sanitizer.SanitizeResult(result with { Duration = stopwatch.Elapsed });
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Health check {Name} timed out", check.Name);
            
            return HealthCheckResult.Unhealthy(
                check.Name,
                stopwatch.Elapsed,
                "Health check timed out",
                "ACODE-HLT-002",
                $"Check took longer than {check.Timeout.TotalMilliseconds}ms timeout");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Health check {Name} failed", check.Name);
            
            return HealthCheckResult.Unhealthy(
                check.Name,
                stopwatch.Elapsed,
                _sanitizer.Sanitize(ex.Message),
                "ACODE-HLT-001",
                "Health check threw an exception");
        }
    }
}
```

```csharp
// Infrastructure/Health/Checks/DatabaseConnectivityCheck.cs
namespace Acode.Infrastructure.Health.Checks;

public sealed class DatabaseConnectivityCheck : IHealthCheck
{
    public string Name => "DatabaseConnectivity";
    public string Category => "Database";
    public TimeSpan Timeout => TimeSpan.FromMilliseconds(100);
    
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseConnectivityCheck> _logger;
    private readonly DatabaseConnectivityCheckOptions _options;
    
    public DatabaseConnectivityCheck(
        IDbConnectionFactory connectionFactory,
        ILogger<DatabaseConnectivityCheck> logger,
        IOptions<DatabaseConnectivityCheckOptions> options)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _options = options.Value;
    }
    
    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
            
            // Execute simple query to verify database is responsive
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync(ct);
            
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;
            
            // Determine status based on response time
            var status = duration > _options.DegradedThreshold
                ? HealthStatus.Degraded
                : HealthStatus.Healthy;
            
            return new HealthCheckResult
            {
                Name = Name,
                Status = status,
                Duration = duration,
                Description = status == HealthStatus.Healthy
                    ? "Database connection OK"
                    : $"Database responding slowly ({duration.TotalMilliseconds:F1}ms)",
                Details = new Dictionary<string, object>
                {
                    ["connection_time_ms"] = duration.TotalMilliseconds,
                    ["threshold_ms"] = _options.DegradedThreshold.TotalMilliseconds
                },
                Suggestion = status == HealthStatus.Degraded
                    ? "Database response time is elevated. Check for locks or high load."
                    : null
            };
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 5) // SQLITE_BUSY
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy(
                Name,
                stopwatch.Elapsed,
                "Database is locked",
                "ACODE-HLT-003",
                "Database is locked by another process. Check for long-running transactions.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Database connectivity check failed");
            
            return HealthCheckResult.Unhealthy(
                Name,
                stopwatch.Elapsed,
                $"Cannot connect to database: {ex.Message}",
                "ACODE-HLT-003",
                "Verify database file exists and is not corrupted.");
        }
    }
}

public sealed class DatabaseConnectivityCheckOptions
{
    public TimeSpan DegradedThreshold { get; set; } = TimeSpan.FromMilliseconds(50);
    public int RetryCount { get; set; } = 1;
}
```

```csharp
// Infrastructure/Health/Checks/StorageCheck.cs
namespace Acode.Infrastructure.Health.Checks;

public sealed class StorageCheck : IHealthCheck
{
    public string Name => "Storage";
    public string Category => "Storage";
    
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<StorageCheck> _logger;
    private readonly StorageCheckOptions _options;
    
    public StorageCheck(
        IFileSystem fileSystem,
        ILogger<StorageCheck> logger,
        IOptions<StorageCheckOptions> options)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _options = options.Value;
    }
    
    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var dbPath = _options.DatabasePath;
            var spaceInfo = await GetDiskSpaceAsync(dbPath, ct);
            var dbSize = _fileSystem.FileExists(dbPath) 
                ? _fileSystem.GetFileSize(dbPath) 
                : 0;
            
            var percentFree = (double)spaceInfo.Available / spaceInfo.Total * 100;
            
            stopwatch.Stop();
            
            var status = percentFree switch
            {
                < 5 => HealthStatus.Unhealthy,
                < 15 => HealthStatus.Degraded,
                _ => HealthStatus.Healthy
            };
            
            return new HealthCheckResult
            {
                Name = Name,
                Status = status,
                Duration = stopwatch.Elapsed,
                Description = $"{percentFree:F1}% free ({FormatBytes(spaceInfo.Available)} available)",
                Details = new Dictionary<string, object>
                {
                    ["percent_free"] = percentFree,
                    ["available_bytes"] = spaceInfo.Available,
                    ["total_bytes"] = spaceInfo.Total,
                    ["database_size_bytes"] = dbSize
                },
                ErrorCode = status == HealthStatus.Unhealthy ? "ACODE-HLT-004" : null,
                Suggestion = status switch
                {
                    HealthStatus.Unhealthy => 
                        "Disk space critically low. Free up space immediately or move database.",
                    HealthStatus.Degraded => 
                        "Disk space is low. Consider freeing space or monitoring growth.",
                    _ => null
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Storage check failed");
            
            return new HealthCheckResult
            {
                Name = Name,
                Status = HealthStatus.Degraded,
                Duration = stopwatch.Elapsed,
                Description = "Could not determine storage space",
                Details = new Dictionary<string, object> { ["error"] = ex.Message }
            };
        }
    }
    
    private async Task<DiskSpaceInfo> GetDiskSpaceAsync(string path, CancellationToken ct)
    {
        // Handle UNC paths on Windows
        if (path.StartsWith(@"\\") || path.StartsWith("//"))
        {
            return await GetNetworkDriveSpaceAsync(path, ct);
        }
        
        return _fileSystem.GetDiskSpaceInfo(path);
    }
    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}

public sealed class StorageCheckOptions
{
    public string DatabasePath { get; set; } = "acode.db";
    public double LowSpaceThresholdPercent { get; set; } = 15;
    public double CriticalSpaceThresholdPercent { get; set; } = 5;
}
```

### CLI Commands

```csharp
// Cli/Commands/StatusCommand.cs
namespace Acode.Cli.Commands;

[Command("status", Description = "Check system health status")]
public sealed class StatusCommand : ICommand
{
    [Option("--format", Description = "Output format: text, json")]
    public string Format { get; set; } = "text";
    
    [Option("--verbose", "-v", Description = "Show detailed output")]
    public bool Verbose { get; set; }
    
    [Option("--no-cache", Description = "Skip cache and run fresh checks")]
    public bool NoCache { get; set; }
    
    [Option("--timeout", Description = "Overall timeout in seconds")]
    public int TimeoutSeconds { get; set; } = 30;
    
    private readonly IHealthCheckRegistry _registry;
    private readonly IHealthCheckCache _cache;
    private readonly IHealthFormatter _textFormatter;
    private readonly IHealthFormatter _jsonFormatter;
    
    public StatusCommand(
        IHealthCheckRegistry registry,
        IHealthCheckCache cache,
        [FromKeyedServices("text")] IHealthFormatter textFormatter,
        [FromKeyedServices("json")] IHealthFormatter jsonFormatter)
    {
        _registry = registry;
        _cache = cache;
        _textFormatter = textFormatter;
        _jsonFormatter = jsonFormatter;
    }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (NoCache)
        {
            _cache.Clear();
        }
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
        
        try
        {
            var result = await _registry.CheckAllAsync(cts.Token);
            
            var formatter = Format.ToLowerInvariant() switch
            {
                "json" => _jsonFormatter,
                _ => _textFormatter
            };
            
            await formatter.WriteAsync(result, console.Output, Verbose);
            
            // Set exit code based on status
            Environment.ExitCode = result.AggregateStatus switch
            {
                HealthStatus.Healthy => 0,
                HealthStatus.Degraded => 1,
                HealthStatus.Unhealthy => 2,
                _ => 0
            };
        }
        catch (OperationCanceledException)
        {
            await console.Error.WriteLineAsync("Health check timed out");
            Environment.ExitCode = 3;
        }
        catch (Exception ex)
        {
            await console.Error.WriteLineAsync($"Error: {ex.Message}");
            Environment.ExitCode = 3;
        }
    }
}
```

```csharp
// Cli/Commands/DiagnosticsCommand.cs
namespace Acode.Cli.Commands;

[Command("diagnostics", Description = "Generate detailed diagnostics report")]
public sealed class DiagnosticsCommand : ICommand
{
    [Option("--format", Description = "Output format: text, json")]
    public string Format { get; set; } = "text";
    
    [Option("--section", "-s", Description = "Specific section(s) to include")]
    public string? Section { get; set; }
    
    [Option("--verbose", "-v", Description = "Show detailed output")]
    public bool Verbose { get; set; }
    
    private readonly IDiagnosticsReportBuilder _builder;
    private readonly IDiagnosticsFormatter _textFormatter;
    private readonly IDiagnosticsFormatter _jsonFormatter;
    
    public DiagnosticsCommand(
        IDiagnosticsReportBuilder builder,
        [FromKeyedServices("text")] IDiagnosticsFormatter textFormatter,
        [FromKeyedServices("json")] IDiagnosticsFormatter jsonFormatter)
    {
        _builder = builder;
        _textFormatter = textFormatter;
        _jsonFormatter = jsonFormatter;
    }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var options = new DiagnosticsOptions
        {
            Sections = Section?.Split(',', StringSplitOptions.RemoveEmptyEntries),
            Verbose = Verbose
        };
        
        try
        {
            var report = await _builder.BuildAsync(options, CancellationToken.None);
            
            var formatter = Format.ToLowerInvariant() switch
            {
                "json" => _jsonFormatter,
                _ => _textFormatter
            };
            
            await formatter.WriteAsync(report, console.Output);
            
            Environment.ExitCode = 0;
        }
        catch (Exception ex)
        {
            await console.Error.WriteLineAsync($"Error generating diagnostics: {ex.Message}");
            Environment.ExitCode = 3;
        }
    }
}
```

### Dependency Injection Setup

```csharp
// Infrastructure/DependencyInjection/HealthCheckServiceCollectionExtensions.cs
namespace Acode.Infrastructure.DependencyInjection;

public static class HealthCheckServiceCollectionExtensions
{
    public static IServiceCollection AddAcodeHealthChecks(
        this IServiceCollection services,
        Action<HealthCheckSettings>? configure = null)
    {
        // Configure settings
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<HealthCheckSettings>(_ => { });
        }
        
        // Register core services
        services.AddSingleton<IHealthCheckRegistry, HealthCheckRegistry>();
        services.AddSingleton<IHealthCheckCache, HealthCheckCache>();
        services.AddSingleton<IHealthOutputSanitizer, HealthOutputSanitizer>();
        
        // Register health checks
        services.AddSingleton<IHealthCheck, DatabaseConnectivityCheck>();
        services.AddSingleton<IHealthCheck, SchemaVersionCheck>();
        services.AddSingleton<IHealthCheck, ConnectionPoolCheck>();
        services.AddSingleton<IHealthCheck, SyncQueueCheck>();
        services.AddSingleton<IHealthCheck, StorageCheck>();
        
        // Auto-register checks with registry
        services.AddHostedService<HealthCheckRegistrationService>();
        
        // Register formatters
        services.AddKeyedSingleton<IHealthFormatter, TextHealthFormatter>("text");
        services.AddKeyedSingleton<IHealthFormatter, JsonHealthFormatter>("json");
        
        return services;
    }
    
    public static IServiceCollection AddAcodeDiagnostics(this IServiceCollection services)
    {
        services.AddSingleton<IDiagnosticsReportBuilder, DiagnosticsReportBuilder>();
        
        // Register diagnostic sections
        services.AddSingleton<IDiagnosticsSection, DatabaseDiagnosticsSection>();
        services.AddSingleton<IDiagnosticsSection, SyncDiagnosticsSection>();
        services.AddSingleton<IDiagnosticsSection, StorageDiagnosticsSection>();
        services.AddSingleton<IDiagnosticsSection, SystemDiagnosticsSection>();
        
        // Register formatters
        services.AddKeyedSingleton<IDiagnosticsFormatter, TextDiagnosticsFormatter>("text");
        services.AddKeyedSingleton<IDiagnosticsFormatter, JsonDiagnosticsFormatter>("json");
        
        return services;
    }
}

public sealed class HealthCheckSettings
{
    public bool EnableCaching { get; set; } = true;
    public int HealthyTtlSeconds { get; set; } = 30;
    public int DegradedTtlSeconds { get; set; } = 10;
    public int UnhealthyTtlSeconds { get; set; } = 5;
    public int MaxConcurrentChecks { get; set; } = 10;
    public int MaxChecksPerSecond { get; set; } = 10;
}
```

### Error Codes

| Code | Name | Meaning | Suggestion |
|------|------|---------|------------|
| ACODE-HLT-001 | HealthCheckFailed | A health check threw an exception | Check logs for exception details |
| ACODE-HLT-002 | HealthCheckTimeout | Health check exceeded timeout | Increase timeout or investigate slow check |
| ACODE-HLT-003 | DatabaseConnectionFailed | Cannot connect to database | Verify database path and permissions |
| ACODE-HLT-004 | LowDiskSpace | Disk space below threshold | Free up disk space |
| ACODE-HLT-005 | SyncStalled | Sync queue not processing | Check sync service status |
| ACODE-HLT-006 | SchemaVersionMismatch | Database schema outdated | Run `acode migrate --apply` |
| ACODE-HLT-007 | PoolExhausted | Connection pool exhausted | Reduce concurrent operations |
| ACODE-HLT-008 | DiagnosticsFailed | Diagnostics report failed | Check individual sections |

### Implementation Checklist

1. [ ] Create `HealthStatus` enum in Application layer
2. [ ] Create `HealthCheckResult` record in Application layer
3. [ ] Create `CompositeHealthResult` record in Application layer
4. [ ] Create `IHealthCheck` interface in Application layer
5. [ ] Create `IHealthCheckRegistry` interface in Application layer
6. [ ] Create `IHealthCheckCache` interface in Application layer
7. [ ] Implement `HealthCheckRegistry` in Infrastructure layer
8. [ ] Implement `HealthCheckCache` with TTL in Infrastructure layer
9. [ ] Implement `HealthOutputSanitizer` in Infrastructure layer
10. [ ] Implement `DatabaseConnectivityCheck` health check
11. [ ] Implement `SchemaVersionCheck` health check
12. [ ] Implement `ConnectionPoolCheck` health check
13. [ ] Implement `SyncQueueCheck` health check
14. [ ] Implement `StorageCheck` health check
15. [ ] Create `IDiagnosticsReportBuilder` interface
16. [ ] Implement `DiagnosticsReportBuilder` in Infrastructure
17. [ ] Implement diagnostic sections (Database, Sync, Storage)
18. [ ] Create `StatusCommand` CLI command
19. [ ] Create `DiagnosticsCommand` CLI command
20. [ ] Implement `TextHealthFormatter` for human output
21. [ ] Implement `JsonHealthFormatter` for machine output
22. [ ] Add DI extension methods
23. [ ] Write unit tests for all health checks
24. [ ] Write unit tests for registry and cache
25. [ ] Write integration tests with real database
26. [ ] Write E2E tests for CLI commands
27. [ ] Add performance benchmarks

### Rollout Plan

#### Phase 1: Foundation (Day 1)
- Create all Application layer types (interfaces, records, enums)
- Set up DI infrastructure
- Write unit test scaffolding

#### Phase 2: Health Check Registry (Day 2)
- Implement `HealthCheckRegistry` with parallel execution
- Implement `HealthCheckCache` with TTL support
- Implement `HealthOutputSanitizer`
- Write unit tests for registry

#### Phase 3: Health Checks (Day 3-4)
- Implement `DatabaseConnectivityCheck`
- Implement `SchemaVersionCheck`
- Implement `ConnectionPoolCheck`
- Implement `SyncQueueCheck`
- Implement `StorageCheck`
- Write unit tests for each check

#### Phase 4: CLI Commands (Day 5)
- Implement `StatusCommand`
- Implement `DiagnosticsCommand`
- Implement formatters (text and JSON)
- Wire up exit codes

#### Phase 5: Diagnostics (Day 6)
- Implement `DiagnosticsReportBuilder`
- Implement diagnostic sections
- Add section filtering

#### Phase 6: Testing & Polish (Day 7)
- Write integration tests
- Write E2E tests
- Performance benchmarks
- Documentation

---

**End of Task 050.d Specification**