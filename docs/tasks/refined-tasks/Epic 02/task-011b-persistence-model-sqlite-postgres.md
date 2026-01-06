# Task 011.b: Persistence Model (SQLite Workspace Cache + Postgres Source-of-Truth)

**Priority:** P0 – Critical Path  
**Tier:** Infrastructure  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 050 (Workspace DB), Task 011.a (Run Entities), Task 026 (Observability)  

---

## Description

Task 011.b implements the two-tier persistence model for run session state. SQLite serves as the workspace cache providing fast, offline-capable, crash-safe storage. PostgreSQL optionally serves as the canonical system of record when remote connectivity is available. This architecture ensures Acode works reliably in all environments.

The two-tier design addresses competing requirements. Users need immediate response and offline capability—SQLite provides this with zero network latency and no dependency on external services. Organizations need centralized visibility and backup—PostgreSQL provides this with proper multi-user access and backup infrastructure. Both needs are satisfied without compromise.

SQLite is the primary storage for all local operations. Every session state change writes to SQLite first. This write is synchronous and must complete before the operation proceeds. SQLite's transactional guarantees ensure crash safety—incomplete writes are rolled back, and the database remains consistent.

PostgreSQL sync is asynchronous and non-blocking. After writing to SQLite, changes are queued for PostgreSQL sync. The agent continues working immediately—it never waits for network operations. Sync happens in the background when the network is available. This design ensures that network issues never slow down the user.

The outbox pattern ensures reliable sync. Each SQLite write also inserts a record into an outbox table. A background process reads the outbox and syncs to PostgreSQL. On success, the outbox record is marked processed. On failure, records are retried with exponential backoff. This pattern guarantees eventual consistency.

Idempotency keys enable safe replay. Every sync record includes a unique idempotency key. PostgreSQL rejects duplicate keys, making replays safe. If sync fails partway through and retries, already-synced records are skipped. This eliminates the risk of duplicate data from retry storms.

Conflict resolution favors the latest timestamp. If the same entity is modified on multiple machines before sync, the latest modification wins. Conflicts are logged for audit but resolved automatically. This simple policy avoids complex merge logic while maintaining practical correctness.

The agent behaves identically whether PostgreSQL is available or not. All functionality works with SQLite alone. PostgreSQL adds centralized backup and multi-machine visibility but is not required for any feature. This ensures Acode works in air-gapped environments, on airplanes, or wherever network is unavailable.

Schema migrations are versioned and automatic. On startup, the persistence layer checks schema version and applies pending migrations. Migrations are forward-only—rollback is not supported (use database restore if needed). Schema changes are tested extensively before deployment.

Connection pooling and lifecycle management are handled by the infrastructure layer. SQLite connections are managed per-thread with proper disposal. PostgreSQL connections use a pool with configurable size. All connections are properly closed on application shutdown.

Error handling distinguishes transient from permanent failures. Network timeouts are transient and trigger retry. Authentication failures are permanent and trigger alert. Schema version mismatches are permanent and require migration. Each error type has appropriate handling and logging.

Testing verifies both tiers independently and together. Unit tests mock database connections. Integration tests use real SQLite. E2E tests use real PostgreSQL. Sync tests verify outbox processing and conflict resolution. This layered testing ensures reliability.

---

## Use Cases

### Use Case 1: Offline Development on Flight with Background Sync on Landing

**Persona:** Alex (Senior Developer, DevOps Engineer)

**Context:** Alex is flying from San Francisco to New York for a conference (6-hour flight). During the flight, Alex works on implementing a new CI/CD pipeline using Acode to generate GitHub Actions workflows. Alex's laptop has no internet connection, but Acode should work normally. Upon landing and connecting to airport WiFi, all work should automatically sync to the company's PostgreSQL database for backup and team visibility.

**Problem:** Without two-tier persistence, Alex faces a dilemma:
- **Cloud-only approach:** Cannot work offline—agent requires internet for every operation, making 6 hours of flight time wasted
- **Local-only approach:** No backup or team visibility—if laptop is lost/stolen, 6 hours of work is gone forever
- **Manual sync:** Alex must remember to sync upon landing—error-prone and often forgotten

**Solution (With Task 011.b):** Acode uses SQLite as the local workspace cache, enabling full offline functionality. Upon connecting to WiFi, the outbox pattern automatically syncs all work to PostgreSQL:

1. **Pre-Flight (14:30 PST):** Alex checks Acode status before boarding
   ```bash
   $ acode db status
   SQLite: Connected (.agent/workspace.db, 48 MB, healthy)
   PostgreSQL: Connected (company.example.com:5432, sync up-to-date)
   Outbox: Empty (0 pending records)
   ```

2. **In-Flight (15:00-21:00 PST):** Alex runs 8 agent sessions to generate workflows, all operations write to SQLite with no network dependency:
   - Session 1: Analyze existing .github/workflows/ (3 files)
   - Session 2: Generate build workflow (creates build.yml)
   - Session 3: Generate test workflow (creates test.yml)
   - Session 4: Generate deploy workflow (creates deploy.yml)
   - Sessions 5-8: Refinements and documentation updates
   
   Each session state transition writes to SQLite (< 50ms) and creates outbox record:
   ```sql
   INSERT INTO outbox (idempotency_key, entity_type, entity_id, payload, created_at)
   VALUES (
     'session:abc123:2026-01-05T18:23:45Z',
     'Session',
     'abc123',
     '{"id":"abc123","state":"Completed","task_description":"Generate build workflow"}',
     '2026-01-05T18:23:45Z'
   );
   ```
   
   Sync attempts fail due to no network (expected), records remain in outbox:
   ```bash
   $ acode db status
   SQLite: Connected (healthy)
   PostgreSQL: Offline (cannot connect)
   Outbox: 47 pending records (oldest: 3h 23m)
   ```

3. **Post-Landing (21:15 EST / 18:15 PST):** Alex connects to airport WiFi, sync process automatically resumes:
   - Background sync worker detects network connectivity
   - Processes outbox records oldest-first with exponential backoff
   - Uses idempotency keys to prevent duplicates from retry logic
   - Syncs all 47 records (8 sessions × ~6 records/session) in 12 seconds
   
   ```bash
   $ acode db status
   SQLite: Connected (healthy)
   PostgreSQL: Connected (company.example.com:5432, syncing...)
   Outbox: 0 pending records (synced 47 records in last 12s)
   
   $ acode db sync-log --tail 5
   [2026-01-05T21:15:23 EST] Sync started (47 pending records)
   [2026-01-05T21:15:27 EST] Synced session abc123 (attempt 1/10, 0ms)
   [2026-01-05T21:15:27 EST] Synced session def456 (attempt 1/10, 0ms)
   ...
   [2026-01-05T21:15:35 EST] Sync complete (47/47 succeeded, 0 failed)
   ```

4. **Team Visibility:** Alex's manager Jordan checks team dashboard and sees Alex's 8 completed sessions, all workflows, and progress during the flight:
   ```sql
   -- Query from PostgreSQL (team dashboard)
   SELECT s.id, s.task_description, s.state, s.created_at, s.updated_at
   FROM sessions s
   WHERE s.user_id = 'alex@company.com'
     AND s.created_at >= '2026-01-05T15:00:00Z'
   ORDER BY s.created_at;
   
   -- Result: 8 sessions, all synced within 12 seconds of WiFi connection
   ```

**Business Impact:**
- **Productivity Preservation:** 6 hours of flight time fully productive (8 sessions completed) vs. 0 sessions with cloud-only approach = **6 hours saved** = **$1,500 per flight** ($250/hour developer rate)
- **Annual Savings:** 4 flights/year/developer × 15 developers × $1,500/flight = **$90,000/year** in preserved productivity
- **Data Protection:** PostgreSQL backup ensures 0% risk of data loss from laptop theft/damage during travel
- **Team Transparency:** Managers have real-time visibility into work completed during offline periods (within minutes of reconnection)
- **Developer Experience:** Seamless offline-to-online transition with zero manual intervention—Alex doesn't even notice the sync happening

**Success Metrics:**
- Offline session completion rate: 100% (SQLite never blocks operations)
- Sync success rate: 99.8% of outbox records sync successfully within 60 seconds of connectivity
- Sync latency: < 500ms per record (average 250ms for session state updates)
- Data loss: 0% from network issues (outbox ensures eventual delivery)
- Developer satisfaction: 95% report "seamless offline experience" in quarterly survey

---

### Use Case 2: Database Migration from SQLite-Only to PostgreSQL with Zero Downtime

**Persona:** Morgan (DevOps Manager), Taylor (Database Administrator)

**Context:** Company starts with 5 developers using Acode with SQLite-only configuration (no central database). After 6 months, company grows to 20 developers and needs centralized visibility, backup, and compliance audit trails. Morgan needs to migrate to PostgreSQL without disrupting ongoing work or losing any data.

**Problem:** Traditional database migrations require:
- **Downtime:** Stop all agents, export data, import to new database, reconfigure, restart—causing 2-4 hours of blocked development
- **Data Loss Risk:** Manual export/import may miss in-progress sessions or corrupt data
- **Testing Difficulty:** Cannot validate migration without affecting production
- **Rollback Complexity:** If migration fails, restoring to original state is manual and error-prone

**Solution (With Task 011.b):** Two-tier architecture enables zero-downtime migration by adding PostgreSQL as optional tier without disrupting SQLite:

1. **Phase 1: PostgreSQL Provisioning (Day 1):** Taylor provisions PostgreSQL database:
   ```bash
   # Create PostgreSQL database
   createdb -h postgres.company.com -U admin acode_production
   
   # Run initial schema migration
   psql -h postgres.company.com -U admin acode_production < migrations/schema_v1.sql
   
   # Verify schema
   psql -h postgres.company.com -U admin acode_production -c "\dt"
   # Output: sessions, session_events, tasks, steps, tool_calls, artifacts, outbox
   ```

2. **Phase 2: Configuration Update (Day 1, during lunch break):** Morgan updates `.agent/config.yml` for all developers via Git commit:
   ```yaml
   persistence:
     postgres:
       enabled: true
       connection_string_env: ACODE_POSTGRES_URL
       sync_interval: 30s
       batch_size: 100
   ```
   
   Developers run `acode config reload` (< 5 seconds, no restart required):
   ```bash
   $ export ACODE_POSTGRES_URL="postgresql://acode:***@postgres.company.com:5432/acode_production"
   $ acode config reload
   [INFO] Configuration reloaded
   [INFO] PostgreSQL sync enabled (connection verified)
   [INFO] Outbox processing started (checking every 30s)
   ```

3. **Phase 3: Historical Data Sync (Day 1-2, background):** Each developer's Acode instance syncs historical data from SQLite to PostgreSQL:
   ```bash
   # Automatic historical sync on first PostgreSQL connection
   $ acode db sync --historical
   [INFO] Scanning SQLite for historical sessions...
   [INFO] Found 127 sessions (34 completed, 2 failed, 91 cancelled, 0 active)
   [INFO] Creating outbox records for 127 sessions...
   [INFO] Outbox: 127 pending records
   [INFO] Sync started (estimated 3-5 minutes)
   ...
   [INFO] Sync complete (127/127 succeeded, 0 failed, duration: 4m 12s)
   ```
   
   Morgan monitors sync progress across team:
   ```bash
   # Query PostgreSQL for sync status
   SELECT 
     user_id,
     COUNT(*) as total_sessions,
     MIN(created_at) as oldest_session,
     MAX(created_at) as newest_session
   FROM sessions
   GROUP BY user_id
   ORDER BY total_sessions DESC;
   
   # After 24 hours: 20 developers, 2,543 historical sessions synced
   ```

4. **Phase 4: Ongoing Sync (Day 2+):** All new sessions automatically sync to PostgreSQL within 30 seconds:
   - Developer creates new session → writes to SQLite (< 50ms)
   - Background worker reads outbox every 30s → syncs to PostgreSQL (< 500ms per record)
   - PostgreSQL has complete view of all team activity with 30-second lag

5. **Phase 5: Validation (Day 3):** Taylor validates data integrity:
   ```bash
   # Count sessions in SQLite (local)
   sqlite3 .agent/workspace.db "SELECT COUNT(*) FROM sessions;"
   # Output: 143
   
   # Count sessions in PostgreSQL (remote)
   psql -h postgres.company.com -c "SELECT COUNT(*) FROM sessions WHERE user_id = 'taylor@company.com';"
   # Output: 143 (matches!)
   
   # Verify no data loss
   acode db validate --compare-checksums
   [INFO] Comparing 143 sessions between SQLite and PostgreSQL
   [INFO] Checksum match: 143/143 (100%)
   [INFO] Data integrity: VERIFIED
   ```

6. **Rollback Capability (if needed):** If PostgreSQL has issues, simply disable sync in config—SQLite continues working:
   ```yaml
   persistence:
     postgres:
       enabled: false  # Disable sync, continue with SQLite-only
   ```
   No data loss, no downtime, instant rollback.

**Business Impact:**
- **Zero Downtime:** Migration completed with 0 minutes of blocked development time (vs. 2-4 hours traditional approach) = **$10,000 saved** (20 developers × 3 hours × $167/hour)
- **Risk Mitigation:** Gradual rollout with instant rollback capability eliminates "big bang" migration risk
- **Compliance:** PostgreSQL provides centralized audit trail for SOC 2 compliance (requirement for enterprise customers)
- **Backup Strategy:** Automated PostgreSQL backups protect against laptop failures (3 laptop failures in 6 months × avg 40 hours lost work = **$20,000 risk elimination**)
- **Team Visibility:** Managers gain real-time dashboard of all developer activity across 20-person team (estimated 5 hours/week management time savings = **$10,800/year**)

**Success Metrics:**
- Migration downtime: 0 seconds (vs. 2-4 hours traditional approach)
- Data loss during migration: 0 sessions lost
- Sync accuracy: 100% checksum validation match between SQLite and PostgreSQL
- Rollback time: < 30 seconds (config change only)
- Developer satisfaction: 100% reported "didn't notice migration happening"

---

### Use Case 3: Disaster Recovery from Corrupted SQLite Database Using PostgreSQL Replica

**Persona:** Jordan (Senior Developer), Morgan (DevOps Manager)

**Context:** Jordan's laptop experiences a filesystem corruption event (power failure during disk write). The SQLite database (`.agent/workspace.db`) becomes corrupted and unrecoverable. Jordan has 3 active sessions in progress (total 18 hours of work over past 2 days) that would be lost without PostgreSQL backup.

**Problem:** Without PostgreSQL tier:
- **Total Data Loss:** 3 sessions × 18 hours = complete work loss
- **Recovery Time:** Must recreate all work from memory—estimated 20+ hours to reconstruct
- **Merge Conflicts:** Other developers' work has progressed—Jordan's recreation may conflict
- **Business Impact:** Project deadline at risk, customer deliverable delayed

**Solution (With Task 011.b):** PostgreSQL serves as source-of-truth backup, enabling complete recovery:

1. **Corruption Detection (9:15 AM):** Jordan attempts to run Acode, detects corruption:
   ```bash
   $ acode session list
   [ERROR] SQLite database corrupted: .agent/workspace.db
   [ERROR] Database integrity check failed (error code 11: SQLITE_CORRUPT)
   [ERROR] Attempted recovery failed (journal file missing)
   
   $ sqlite3 .agent/workspace.db "PRAGMA integrity_check;"
   *** in database main ***
   On tree page 47 cell 12: Rowid 1523 out of order
   Error: database disk image is malformed
   ```

2. **Recovery Initiation (9:18 AM):** Jordan triggers recovery from PostgreSQL:
   ```bash
   # Backup corrupted database (for forensics)
   mv .agent/workspace.db .agent/workspace.db.corrupted.2026-01-05
   
   # Trigger recovery from PostgreSQL
   $ acode db recover --from postgres
   [INFO] PostgreSQL connection: OK (postgres.company.com:5432)
   [INFO] Querying sessions for user: jordan@company.com
   [INFO] Found 127 historical sessions
   [INFO] Found 3 active sessions (created in last 48 hours)
   [INFO] Creating new SQLite database: .agent/workspace.db
   [INFO] Restoring schema (version 12)
   [INFO] Restoring 127 sessions...
   [INFO] Restoring 2,847 events...
   [INFO] Restoring 384 tasks...
   [INFO] Restoring 1,923 steps...
   [INFO] Restoring 5,672 tool calls...
   [INFO] Restoring 1,234 artifacts...
   [INFO] Recovery complete (duration: 18 seconds)
   [INFO] Verifying data integrity...
   [INFO] Checksum validation: 127/127 sessions OK
   [INFO] SQLite database restored successfully
   ```

3. **Verification (9:19 AM):** Jordan verifies all data recovered:
   ```bash
   # List sessions
   $ acode session list --active
   ID       State       Description                              Created
   ──────────────────────────────────────────────────────────────────────────────
   abc123   Executing   Implement OAuth2 authorization flow      2026-01-04 14:23
   def456   Planning    Add token refresh endpoint               2026-01-05 08:45
   ghi789   Paused      Write OAuth2 integration tests           2026-01-05 09:02
   
   # Verify session details
   $ acode session show abc123 --verbose
   Session: abc123
   State: Executing
   Description: Implement OAuth2 authorization flow
   Created: 2026-01-04T14:23:17Z
   Updated: 2026-01-05T09:12:34Z (12 minutes ago, before corruption)
   Tasks: 4 (2 completed, 1 in-progress, 1 pending)
   Steps: 18 (14 completed, 4 pending)
   Tool Calls: 47
   Artifacts: 12 files modified
   
   # All data present - can resume immediately!
   ```

4. **Resume Work (9:20 AM):** Jordan resumes exactly where left off:
   ```bash
   $ acode session resume abc123
   [INFO] Resuming session abc123 from EXECUTING state
   [INFO] Validating environment (Git branch, dependencies)
   [INFO] Environment: OK
   [INFO] Checkpoint found: Step 14 of Task 2 completed
   [INFO] Resuming from Step 15: "Add token validation middleware"
   [Agent continues working normally, no data loss]
   ```

5. **Root Cause Analysis (Later):** Morgan investigates corruption:
   ```bash
   # Review audit logs from PostgreSQL
   SELECT 
     session_id,
     from_state,
     to_state,
     timestamp,
     reason
   FROM session_events
   WHERE session_id IN ('abc123', 'def456', 'ghi789')
     AND timestamp >= '2026-01-05T09:10:00Z'
   ORDER BY timestamp;
   
   # Last sync before corruption: 09:12:34Z (3 minutes before detection)
   # Data loss window: 3 minutes (vs. 18 hours without PostgreSQL)
   ```

**Business Impact:**
- **Data Recovery:** 18 hours of work recovered in 18 seconds (1 second per hour of work!) = **$4,500 saved** (18 hours × $250/hour)
- **Productivity Loss:** 3 minutes of work lost (last sync to corruption) vs. 18 hours total loss = **99.7% data protection**
- **Recovery Time:** 5 minutes total (detection + recovery + verification) vs. 20+ hours recreation = **$4,875 saved** (19.9 hours × $245/hour)
- **Business Continuity:** Project deadline met on schedule (vs. 2-day delay without recovery)
- **Risk Mitigation:** Filesystem corruption (1-2 events/year/developer) no longer causes catastrophic data loss
- **Insurance Value:** PostgreSQL backup acts as "insurance policy" for $0/month incremental cost (already provisioned)

**Success Metrics:**
- Recovery time: 18 seconds to restore 127 sessions (vs. 20+ hours manual recreation)
- Data loss window: 3 minutes (vs. complete loss without backup)
- Checksum validation: 100% integrity verified
- Resume success: All 3 sessions resumed correctly with no manual intervention
- Developer confidence: "I don't worry about losing work anymore" (post-incident survey)

---

**Total Business Value Summary:**
- **Offline Productivity:** $90,000/year from flight/travel work preservation
- **Zero-Downtime Migration:** $10,000 one-time + $10,800/year ongoing management efficiency
- **Disaster Recovery:** $9,375 per corruption event (estimated 2-3 events/year/20 developers) = $56,250/year expected value
- **Total Annual ROI:** **$157,050/year** for a 20-developer team
- **Intangible Benefits:** Developer peace of mind, compliance readiness, team visibility, centralized audit trail

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| SQLite | Embedded relational database |
| PostgreSQL | Server-based relational database |
| Workspace Cache | Local SQLite database |
| Source of Truth | Canonical PostgreSQL database |
| Outbox Pattern | Queue for reliable async sync |
| Inbox Pattern | Queue for receiving sync |
| Idempotency Key | Unique identifier for safe replay |
| Eventual Consistency | Data syncs over time |
| Conflict Resolution | Handling concurrent modifications |
| Schema Migration | Database structure updates |
| Connection Pool | Reusable database connections |
| WAL Mode | Write-Ahead Logging for SQLite |
| Transaction | Atomic database operation |
| Retry Backoff | Increasing delays between retries |
| Transient Failure | Temporary recoverable error |

---

## Out of Scope

The following items are explicitly excluded from Task 011.b:

- **Multi-master sync** - Single source of truth
- **Real-time sync** - Eventual consistency only
- **Conflict merge logic** - Latest timestamp wins
- **Cross-database joins** - Single-tier queries only
- **Database sharding** - Single PostgreSQL instance
- **Read replicas** - Primary only
- **Custom SQL** - ORM/abstraction only
- **Database encryption at rest** - OS-level only
- **Point-in-time recovery** - Backup-based only
- **Foreign database support** - SQLite + PostgreSQL only

---

## Assumptions

### Technical Assumptions

- ASM-001: SQLite 3.35+ is available for WAL mode and JSON support
- ASM-002: PostgreSQL 14+ is available for remote tier deployment
- ASM-003: Entity Framework Core supports both SQLite and PostgreSQL providers
- ASM-004: Connection strings can be configured via environment or config file
- ASM-005: Transaction isolation levels are configurable per database
- ASM-006: Database migrations can be applied automatically on startup

### Environmental Assumptions

- ASM-007: SQLite database file location (.agent/workspace.db) is writable
- ASM-008: Network connectivity exists when PostgreSQL tier is configured
- ASM-009: Sufficient disk space exists for database growth
- ASM-010: Concurrent access from single process is the primary pattern

### Dependency Assumptions

- ASM-011: Task 011.a entity model defines what needs to be persisted
- ASM-012: Task 050 workspace database provides shared infrastructure
- ASM-013: Configuration system provides database connection settings

### Reliability Assumptions

- ASM-014: SQLite WAL mode provides crash recovery
- ASM-015: PostgreSQL transactions provide ACID guarantees
- ASM-016: Connection pooling is handled by the database provider
- ASM-017: Transient failures are retryable with exponential backoff
- ASM-018: Schema migrations are backwards-compatible

---

## Functional Requirements

### SQLite Storage

- FR-001: SQLite file MUST be in .agent/workspace.db
- FR-002: SQLite MUST use WAL mode
- FR-003: SQLite MUST use STRICT tables
- FR-004: Database MUST be created on first access
- FR-005: All writes MUST be transactional
- FR-006: Transactions MUST have timeout (30s)
- FR-007: Concurrent reads MUST be supported
- FR-008: Write locks MUST be exclusive

### Schema Management

- FR-009: Schema version MUST be tracked
- FR-010: Migrations MUST be versioned
- FR-011: Migrations MUST run automatically on startup
- FR-012: Migrations MUST be idempotent
- FR-013: Migration failures MUST halt startup
- FR-014: Current schema version MUST be queryable
- FR-015: Migration history MUST be persisted

### Session Tables

- FR-016: sessions table for Session entities
- FR-017: session_events table for events
- FR-018: session_tasks table for Tasks
- FR-019: steps table for Steps
- FR-020: tool_calls table for ToolCalls
- FR-021: artifacts table for Artifacts
- FR-022: All tables MUST have created_at
- FR-023: All tables MUST have updated_at
- FR-024: Primary keys MUST be UUID strings

### PostgreSQL Storage (Optional)

- FR-025: PostgreSQL MUST be configurable
- FR-026: Connection string via config or env
- FR-027: Missing PostgreSQL MUST NOT fail startup
- FR-028: Schema MUST mirror SQLite structure
- FR-029: Indexes MUST optimize common queries
- FR-030: Connection pool size MUST be configurable

### Outbox Pattern

- FR-031: outbox table for pending syncs
- FR-032: Outbox records MUST have idempotency_key
- FR-033: Outbox records MUST have entity_type
- FR-034: Outbox records MUST have entity_id
- FR-035: Outbox records MUST have payload (JSON)
- FR-036: Outbox records MUST have created_at
- FR-037: Outbox records MUST have processed_at (nullable)
- FR-038: Outbox records MUST have attempts count
- FR-039: Outbox records MUST have last_error

### Sync Process

- FR-040: Sync MUST run in background
- FR-041: Sync MUST be non-blocking
- FR-042: Sync MUST process oldest first
- FR-043: Sync MUST retry failed records
- FR-044: Retry MUST use exponential backoff
- FR-045: Max retry attempts: 10
- FR-046: Max backoff: 1 hour
- FR-047: Sync MUST be resumable after restart

### Idempotency

- FR-048: Each sync MUST have unique key
- FR-049: Key format: {entity_type}:{entity_id}:{timestamp}
- FR-050: PostgreSQL MUST reject duplicate keys
- FR-051: Duplicate rejection MUST mark outbox processed
- FR-052: Idempotency MUST be logged

### Conflict Resolution

- FR-053: Conflicts MUST be detected
- FR-054: Latest timestamp MUST win
- FR-055: Conflicts MUST be logged
- FR-056: Conflict count MUST be tracked
- FR-057: Conflict details MUST be preserved

### Query Operations

- FR-058: Get session by ID
- FR-059: List sessions with filter
- FR-060: Get session hierarchy
- FR-061: Query events by session
- FR-062: Pagination MUST be supported
- FR-063: Queries MUST use indexes

### Write Operations

- FR-064: Create session atomically
- FR-065: Update session state
- FR-066: Add task to session
- FR-067: Add step to task
- FR-068: Add tool call to step
- FR-069: Add artifact to tool call
- FR-070: All writes to outbox too

### Abstraction Layer

- FR-071: IRunStateStore interface for queries
- FR-072: ISyncService interface for sync
- FR-073: No direct database access from domain
- FR-074: Database choice via dependency injection

---

## Non-Functional Requirements

### Performance

- NFR-001: SQLite write MUST complete < 50ms
- NFR-002: SQLite read MUST complete < 10ms
- NFR-003: Sync batch MUST process 100 records/sec
- NFR-004: Connection pool MUST handle 10 connections
- NFR-005: Query with pagination MUST be < 100ms

### Reliability

- NFR-006: Crash MUST NOT corrupt database
- NFR-007: Partial sync MUST be recoverable
- NFR-008: Connection loss MUST queue for retry
- NFR-009: Schema mismatch MUST halt startup

### Security

- NFR-010: Database file MUST have 600 permissions
- NFR-011: Connection string MUST NOT be logged
- NFR-012: Passwords MUST be from env var or secret
- NFR-013: SQL injection MUST be prevented

### Durability

- NFR-014: All writes MUST be fsync'd
- NFR-015: Outbox MUST survive crash
- NFR-016: Processed records MUST NOT be lost

### Observability

- NFR-017: All queries MUST be logged with duration
- NFR-018: Sync status MUST be exposed
- NFR-019: Outbox depth MUST be tracked
- NFR-020: Connection health MUST be monitored

---

## Security Considerations

### Threat 1: Connection String Exposure via Configuration Files or Logs

**Attack Scenario:**
A developer accidentally commits `.agent/config.yml` containing PostgreSQL connection string with embedded password to Git repository. The repository is public or accessible to unauthorized users. Attacker clones repository, extracts connection string, and gains full access to PostgreSQL database containing all team session data.

**Impact Assessment:**
- **Confidentiality:** **CRITICAL** - Full database access exposes all session data, tool calls, artifacts
- **Integrity:** **HIGH** - Attacker can modify or delete session records
- **Availability:** **MEDIUM** - Attacker could drop tables or delete database

**Mitigation Strategy:**

1. **Environment Variable Mandate:** Never store passwords in config files, require environment variables

```csharp
namespace AgenticCoder.Infrastructure.Persistence;

public sealed class PostgresConnectionStringBuilder
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PostgresConnectionStringBuilder> _logger;
    
    public string Build()
    {
        // Read from environment variable (preferred)
        var connectionString = Environment.GetEnvironmentVariable("ACODE_POSTGRES_URL");
        if (!string.IsNullOrEmpty(connectionString))
        {
            ValidateConnectionString(connectionString);
            return connectionString;
        }
        
        // Fallback: Build from individual config values
        var host = _configuration["Persistence:Postgres:Host"];
        var port = _configuration["Persistence:Postgres:Port"] ?? "5432";
        var database = _configuration["Persistence:Postgres:Database"];
        var username = _configuration["Persistence:Postgres:Username"];
        
        // Password MUST come from environment variable
        var passwordEnvVar = _configuration["Persistence:Postgres:PasswordEnv"];
        if (string.IsNullOrEmpty(passwordEnvVar))
        {
            throw new InvalidOperationException(
                "PostgreSQL password must be provided via environment variable. "
                + "Set 'Persistence:Postgres:PasswordEnv' to environment variable name (e.g., 'ACODE_POSTGRES_PASSWORD').");
        }
        
        var password = Environment.GetEnvironmentVariable(passwordEnvVar);
        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                $"Environment variable '{passwordEnvVar}' not set or empty. "
                + "PostgreSQL password is required.");
        }
        
        // Build connection string
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.Parse(port),
            Database = database,
            Username = username,
            Password = password,
            SslMode = SslMode.Require, // Enforce SSL
            Timeout = 30,
            CommandTimeout = 60,
            Pooling = true,
            MaxPoolSize = 10
        };
        
        return builder.ConnectionString;
    }
    
    private void ValidateConnectionString(string connectionString)
    {
        // Ensure no plain-text passwords in logs
        if (connectionString.Contains("password=", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Connection string contains embedded password. "
                + "Consider using environment variable ACODE_POSTGRES_URL with password.");
        }
    }
}
```

2. **Log Redaction:** Strip passwords from all logged connection strings

```csharp
public sealed class ConnectionStringRedactor
{
    private static readonly Regex PasswordPattern = new(
        @"password\s*=\s*([^;]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    public static string Redact(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;
        
        // Replace password value with ***
        return PasswordPattern.Replace(connectionString, "password=***");
    }
}

public sealed class PostgresRunStateStore : IRunStateStore
{
    private readonly ILogger<PostgresRunStateStore> _logger;
    
    public PostgresRunStateStore(string connectionString, ILogger<PostgresRunStateStore> logger)
    {
        _logger = logger;
        
        // Log redacted connection string
        var redacted = ConnectionStringRedactor.Redact(connectionString);
        _logger.LogInformation(
            "Initializing PostgreSQL store: {ConnectionString}",
            redacted);
    }
}
```

3. **Configuration File Template:** Provide template with environment variable placeholders

```yaml
# .agent/config.yml.template (checked into Git)
persistence:
  postgres:
    enabled: true
    connection_string_env: ACODE_POSTGRES_URL  # Set environment variable with actual connection string
    # OR use individual settings:
    # host: postgres.company.com
    # port: 5432
    # database: acode
    # username: acode
    # password_env: ACODE_POSTGRES_PASSWORD  # Environment variable containing password
```

4. **Git Pre-Commit Hook:** Prevent committing secrets

```bash
#!/bin/bash
# .git/hooks/pre-commit

# Check for potential secrets in .agent/config.yml
if git diff --cached --name-only | grep -q '.agent/config.yml'; then
    if git diff --cached .agent/config.yml | grep -iE '(password|secret|key)\s*=\s*[^\$]'; then
        echo "ERROR: .agent/config.yml contains potential secret (password/key not using environment variable)"
        echo "Use 'password_env: ENV_VAR_NAME' instead of 'password: actual-password'"
        exit 1
    fi
fi
```

**Defense in Depth:**
- **Configuration:** Environment variables only, never plain-text passwords in files
- **Logging:** Automatic redaction of passwords in all log output
- **Version Control:** Pre-commit hooks prevent accidental commits
- **Documentation:** Clear guidance on secret management in user manual
- **Validation:** Startup fails if password not provided via environment variable

---

### Threat 2: SQL Injection via Malicious Session Metadata

**Attack Scenario:**
A compromised dependency or malicious plugin injects SQL code into session metadata fields (e.g., `task_description`, `reason`). When this data is queried or displayed, the SQL injection executes arbitrary commands on PostgreSQL, potentially dropping tables or exfiltrating data.

**Impact Assessment:**
- **Confidentiality:** **HIGH** - Attacker can read all database contents
- **Integrity:** **CRITICAL** - Attacker can modify or delete records
- **Availability:** **HIGH** - Attacker can DROP tables or database

**Mitigation Strategy:**

1. **Parameterized Queries:** Use Entity Framework Core's parameterized queries, never string concatenation

```csharp
public sealed class PostgresRunStateStore : IRunStateStore
{
    private readonly AcodeDbContext _context;
    
    public async Task SaveAsync(Session session, CancellationToken ct)
    {
        // Entity Framework Core automatically parameterizes queries
        var entity = new SessionEntity
        {
            Id = session.Id.Value,
            TaskDescription = session.TaskDescription, // Automatically parameterized
            State = session.State.ToString(),
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
        
        _context.Sessions.Add(entity);
        await _context.SaveChangesAsync(ct);
        
        // WRONG (vulnerable to SQL injection):
        // var sql = $"INSERT INTO sessions (id, task_description) VALUES ('{session.Id}', '{session.TaskDescription}')";
        // _context.Database.ExecuteSqlRaw(sql); // NEVER DO THIS
    }
    
    public async Task<IReadOnlyList<Session>> SearchAsync(string query, CancellationToken ct)
    {
        // Correct: Use parameterized LIKE query
        var entities = await _context.Sessions
            .Where(s => EF.Functions.Like(s.TaskDescription, $"%{query}%")) // Parameterized
            .ToListAsync(ct);
        
        // WRONG (vulnerable to SQL injection):
        // var sql = $"SELECT * FROM sessions WHERE task_description LIKE '%{query}%'";
        // var entities = await _context.Sessions.FromSqlRaw(sql).ToListAsync(ct); // NEVER DO THIS
        
        return entities.Select(MapToDomain).ToList();
    }
}
```

2. **Input Validation:** Validate and sanitize all user-provided strings

```csharp
public sealed class SessionInputValidator
{
    private static readonly Regex SafeDescriptionPattern = new(
        @"^[a-zA-Z0-9\s\-_.,;:!?()\[\]{}@#$%&*+=<>/\\|~`'\"]+$",
        RegexOptions.Compiled);
    
    public static string ValidateTaskDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Task description cannot be empty");
        
        if (description.Length > 1000)
            throw new ArgumentException("Task description too long (max 1000 characters)");
        
        // Check for SQL injection patterns
        var suspiciousPatterns = new[]
        {
            ";", "--", "/*", "*/", "xp_", "sp_", "exec", "execute",
            "drop", "delete", "insert", "update", "create", "alter"
        };
        
        var lower = description.ToLowerInvariant();
        foreach (var pattern in suspiciousPatterns)
        {
            if (lower.Contains(pattern))
            {
                throw new ArgumentException(
                    $"Task description contains disallowed pattern: '{pattern}'. "
                    + "This may be a SQL injection attempt.");
            }
        }
        
        return description;
    }
}

public sealed class Session
{
    public Session(SessionId id, string taskDescription)
    {
        Id = id;
        TaskDescription = SessionInputValidator.ValidateTaskDescription(taskDescription);
        // ...
    }
}
```

3. **Database User Permissions:** Run with minimal permissions (no DROP, no admin)

```sql
-- Create restricted database user for Acode
CREATE USER acode_app WITH PASSWORD 'strong-password-here';

-- Grant only necessary permissions (no DDL)
GRANT CONNECT ON DATABASE acode TO acode_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO acode_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO acode_app;

-- Revoke dangerous permissions
REVOKE CREATE ON SCHEMA public FROM acode_app;
REVOKE DROP ON ALL TABLES IN SCHEMA public FROM acode_app;

-- Verify permissions
\du acode_app
```

4. **Query Monitoring:** Log and alert on suspicious query patterns

```csharp
public sealed class QueryMonitoringInterceptor : DbCommandInterceptor
{
    private readonly ILogger<QueryMonitoringInterceptor> _logger;
    
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken ct)
    {
        // Log all queries with duration
        var commandText = command.CommandText;
        
        // Alert on suspicious patterns
        if (commandText.Contains("DROP", StringComparison.OrdinalIgnoreCase) ||
            commandText.Contains("DELETE FROM", StringComparison.OrdinalIgnoreCase) ||
            commandText.Contains("TRUNCATE", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Suspicious SQL command detected: {CommandText}",
                commandText);
        }
        
        return base.ReaderExecutingAsync(command, eventData, result, ct);
    }
}
```

**Defense in Depth:**
- **ORM Layer:** Entity Framework Core parameterizes all queries automatically
- **Input Validation:** Reject suspicious patterns before reaching database
- **Database Permissions:** Minimal privileges (no DROP, no admin commands)
- **Monitoring:** Alert on dangerous SQL patterns in production
- **Testing:** SQL injection test suite validates all input paths

---

### Threat 3: Outbox Flooding Denial of Service

**Attack Scenario:**
A malicious script or runaway process creates thousands of sessions rapidly, each generating multiple outbox records. The outbox table grows to millions of records, exhausting disk space and degrading sync performance. Legitimate sessions cannot sync because background worker is overwhelmed processing backlog.

**Impact Assessment:**
- **Confidentiality:** Low
- **Integrity:** Low
- **Availability:** **CRITICAL** - Sync system becomes unusable, disk full

**Mitigation Strategy:**

1. **Outbox Size Limits:** Enforce maximum pending records per user

```csharp
public sealed class OutboxService : IOutboxService
{
    private readonly AcodeDbContext _context;
    private readonly ILogger<OutboxService> _logger;
    
    private const int MaxPendingPerUser = 1000;
    
    public async Task EnqueueAsync(OutboxRecord record, CancellationToken ct)
    {
        // Check current outbox depth for this user
        var pendingCount = await _context.OutboxRecords
            .CountAsync(r => r.UserId == record.UserId && r.ProcessedAt == null, ct);
        
        if (pendingCount >= MaxPendingPerUser)
        {
            _logger.LogError(
                "Outbox limit exceeded for user {UserId}: {PendingCount} pending records",
                record.UserId, pendingCount);
            
            throw new OutboxLimitExceededException(
                $"Too many pending outbox records ({pendingCount}). "
                + $"Maximum: {MaxPendingPerUser}. Wait for sync to complete before creating more sessions.");
        }
        
        // Add to outbox
        _context.OutboxRecords.Add(record);
        await _context.SaveChangesAsync(ct);
    }
}
```

2. **Automatic Cleanup:** Purge old processed records to prevent unbounded growth

```csharp
public sealed class OutboxCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxCleanupService> _logger;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(1), ct);
            await CleanupProcessedRecordsAsync(ct);
        }
    }
    
    private async Task CleanupProcessedRecordsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AcodeDbContext>();
        
        // Delete records processed more than 7 days ago
        var threshold = DateTimeOffset.UtcNow.AddDays(-7);
        
        var deleted = await context.OutboxRecords
            .Where(r => r.ProcessedAt != null && r.ProcessedAt < threshold)
            .ExecuteDeleteAsync(ct);
        
        if (deleted > 0)
        {
            _logger.LogInformation(
                "Cleaned up {DeletedCount} processed outbox records older than {Threshold}",
                deleted, threshold);
        }
    }
}
```

3. **Rate Limiting:** Throttle session creation per user

```csharp
public sealed class SessionRateLimiter
{
    private readonly IDistributedCache _cache;
    private const int MaxSessionsPerHour = 50;
    
    public async Task<bool> TryAcquireAsync(string userId, CancellationToken ct)
    {
        var key = $"session-rate-limit:{userId}:{DateTimeOffset.UtcNow:yyyy-MM-dd-HH}";
        
        var currentCountStr = await _cache.GetStringAsync(key, ct);
        var currentCount = int.TryParse(currentCountStr, out var count) ? count : 0;
        
        if (currentCount >= MaxSessionsPerHour)
        {
            return false; // Rate limit exceeded
        }
        
        // Increment counter
        await _cache.SetStringAsync(
            key,
            (currentCount + 1).ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) },
            ct);
        
        return true;
    }
}
```

4. **Monitoring and Alerting:** Alert on abnormal outbox growth

```csharp
public sealed class OutboxHealthCheck : IHealthCheck
{
    private readonly AcodeDbContext _context;
    private const int WarningThreshold = 5000;
    private const int CriticalThreshold = 10000;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct)
    {
        var pendingCount = await _context.OutboxRecords
            .CountAsync(r => r.ProcessedAt == null, ct);
        
        if (pendingCount >= CriticalThreshold)
        {
            return HealthCheckResult.Unhealthy(
                $"Outbox critically full: {pendingCount} pending records (threshold: {CriticalThreshold})");
        }
        
        if (pendingCount >= WarningThreshold)
        {
            return HealthCheckResult.Degraded(
                $"Outbox filling up: {pendingCount} pending records (warning threshold: {WarningThreshold})");
        }
        
        return HealthCheckResult.Healthy($"Outbox healthy: {pendingCount} pending records");
    }
}
```

**Defense in Depth:**
- **Per-User Limits:** Maximum 1000 pending outbox records per user
- **Automatic Cleanup:** Purge processed records older than 7 days
- **Rate Limiting:** Maximum 50 sessions per user per hour
- **Health Checks:** Alert at 5000 pending (warning) and 10000 (critical)
- **Disk Quotas:** OS-level disk quotas prevent unbounded database growth

---

## Best Practices

### Database Configuration

**BP-001: Always Enable SQLite WAL Mode**
- **Reason:** Write-Ahead Logging provides better concurrency and crash recovery
- **Example:** `PRAGMA journal_mode=WAL;` on database initialization
- **Anti-pattern:** Using default DELETE journaling mode (poor concurrent read performance)

**BP-002: Use STRICT Tables in SQLite**
- **Reason:** Enforces column type checking, prevents data type confusion
- **Example:** `CREATE TABLE sessions (...) STRICT;`
- **Anti-pattern:** Relying on SQLite's dynamic typing (allows integer in text column)

**BP-003: Set Appropriate Connection Pool Size**
- **Reason:** Balances resource usage with concurrency needs
- **Example:** `MaxPoolSize=10` for PostgreSQL (sufficient for single-user CLI)
- **Anti-pattern:** Using default pool size (100+) for single-threaded application

### Sync Strategy

**BP-004: Sync Asynchronously, Never Block User**
- **Reason:** Network latency should not impact local operation speed
- **Example:** Write to SQLite immediately, queue for PostgreSQL sync in background
- **Anti-pattern:** Waiting for PostgreSQL write to complete before returning to user

**BP-005: Use Idempotency Keys for Safe Replay**
- **Reason:** Enables retry logic without risk of duplicate data
- **Example:** Key format `{entity_type}:{entity_id}:{timestamp_utc}`
- **Anti-pattern:** Replaying sync without idempotency checking (creates duplicates)

**BP-006: Implement Exponential Backoff for Transient Failures**
- **Reason:** Avoids overwhelming failing service with rapid retries
- **Example:** 1s, 2s, 4s, 8s, 16s, ... up to 1 hour between retries
- **Anti-pattern:** Fixed 1-second retry interval (amplifies load during outage)

### Schema Management

**BP-007: Version All Schema Migrations**
- **Reason:** Enables tracking applied migrations and detecting drift
- **Example:** `migrations/v001_initial_schema.sql`, `v002_add_artifacts_table.sql`
- **Anti-pattern:** Manually applying schema changes without version tracking

**BP-008: Make Migrations Idempotent**
- **Reason:** Allows safe re-run if migration fails partway through
- **Example:** `CREATE TABLE IF NOT EXISTS sessions (...);`
- **Anti-pattern:** `CREATE TABLE sessions (...);` fails on re-run

**BP-009: Test Migrations on Copy of Production Data**
- **Reason:** Catches data-dependent migration failures before production
- **Example:** Export production DB, run migration on copy, verify data integrity
- **Anti-pattern:** Testing migration on empty database only

### Error Handling

**BP-010: Distinguish Transient from Permanent Errors**
- **Reason:** Transient errors should retry, permanent errors should alert
- **Example:** Network timeout = transient (retry), authentication failure = permanent (alert)
- **Anti-pattern:** Retrying authentication failures indefinitely (never succeeds)

**BP-011: Log All Database Errors with Context**
- **Reason:** Enables debugging without reproducing error
- **Example:** Log query, parameters, exception, duration, connection state
- **Anti-pattern:** Logging only exception message ("Connection refused")

**BP-012: Gracefully Degrade When PostgreSQL Unavailable**
- **Reason:** Application should work offline, sync when reconnected
- **Example:** SQLite continues working, outbox queues for later sync
- **Anti-pattern:** Failing startup if PostgreSQL unreachable

### Testing

**BP-013: Test Both Tiers Independently**
- **Reason:** Isolates failures to specific tier
- **Example:** Unit tests with in-memory SQLite, integration tests with real PostgreSQL
- **Anti-pattern:** Testing only two-tier integration (can't isolate which tier failed)

**BP-014: Test Sync with Simulated Network Failures**
- **Reason:** Validates retry logic and outbox processing
- **Example:** Inject network errors, verify outbox records retry with backoff
- **Anti-pattern:** Testing only happy path (sync succeeds first try)

**BP-015: Verify Idempotency with Duplicate Replay**
- **Reason:** Ensures retry logic doesn't create duplicate records
- **Example:** Sync same record twice, verify only one instance in PostgreSQL
- **Anti-pattern:** Assuming idempotency works without testing replay

---

## Troubleshooting

### Problem 1: "Cannot Connect to PostgreSQL" Error During Startup

**Symptoms:**
- Application fails to start with error "Cannot connect to PostgreSQL"
- Error message includes connection details (host, port, database)
- SQLite continues working but no sync occurs

**Possible Causes:**
1. PostgreSQL server not running or unreachable
2. Network firewall blocking connection
3. Incorrect connection string (wrong host/port/database/credentials)
4. SSL required but not configured in connection string
5. Database user lacks connection permission

**Diagnosis:**

```bash
# Test network connectivity to PostgreSQL server
telnet postgres.company.com 5432
# Or use nc (netcat)
nc -zv postgres.company.com 5432

# Test PostgreSQL connection with psql
export PGPASSWORD='your-password'
psql -h postgres.company.com -U acode -d acode -c "SELECT version();"

# Check Acode configuration
acode config show --format json | jq '.persistence.postgres'

# Check environment variables
echo $ACODE_POSTGRES_URL
echo $ACODE_POSTGRES_PASSWORD

# Review connection logs
acode db status --verbose
```

**Solutions:**

1. **Verify PostgreSQL Server Status:**
   ```bash
   # Check if PostgreSQL is running (on server)
   sudo systemctl status postgresql
   
   # Check PostgreSQL logs for errors
   sudo journalctl -u postgresql -n 50
   
   # Verify PostgreSQL listening on correct interface
   sudo netstat -tulpn | grep 5432
   # Should show: tcp 0 0 0.0.0.0:5432 LISTEN
   ```

2. **Fix Connection String:**
   ```bash
   # Correct format:
   export ACODE_POSTGRES_URL="postgresql://username:password@host:port/database?sslmode=require"
   
   # Example with SSL:
   export ACODE_POSTGRES_URL="postgresql://acode:mypass123@postgres.company.com:5432/acode?sslmode=require"
   
   # Test connection
   acode db status
   ```

3. **Configure Firewall:**
   ```bash
   # Allow PostgreSQL through firewall (on server)
   sudo ufw allow 5432/tcp
   
   # Or for specific IP only:
   sudo ufw allow from 192.168.1.100 to any port 5432
   ```

4. **Grant Database Permissions:**
   ```sql
   -- Run on PostgreSQL server as admin
   GRANT CONNECT ON DATABASE acode TO acode_user;
   GRANT USAGE ON SCHEMA public TO acode_user;
   GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO acode_user;
   ```

5. **Disable PostgreSQL Temporarily (Work Offline):**
   ```yaml
   # .agent/config.yml - disable PostgreSQL sync
   persistence:
     postgres:
       enabled: false  # Continue with SQLite only
   ```

**Prevention:**
- Test connection string during configuration setup
- Use health checks to monitor PostgreSQL connectivity
- Document PostgreSQL prerequisites in deployment guide
- Provide clear error messages with troubleshooting steps

---

### Problem 2: Outbox Records Not Syncing (Stuck in Pending State)

**Symptoms:**
- `acode db status` shows large number of pending outbox records (100+)
- Records remain pending for hours or days
- Sync worker logs show no activity or repeated errors
- SQLite database growing but PostgreSQL not receiving updates

**Possible Causes:**
1. Sync worker not running (service crashed or disabled)
2. Network connectivity lost after initial connection
3. PostgreSQL connection pool exhausted
4. Idempotency key conflicts causing silent failures
5. Transient errors exhausted retry attempts

**Diagnosis:**

```bash
# Check outbox status
acode db status --verbose
# Output shows: "Outbox: 472 pending records (oldest: 2d 14h)"

# Query outbox directly
sqlite3 .agent/workspace.db "SELECT COUNT(*) as pending, MIN(created_at) as oldest FROM outbox WHERE processed_at IS NULL;"

# Check sync worker status
acode db sync-status
# Output: "Sync worker: STOPPED (last heartbeat: 2h 34m ago)"

# Review sync error logs
sqlite3 .agent/workspace.db "SELECT entity_type, entity_id, attempts, last_error FROM outbox WHERE processed_at IS NULL ORDER BY attempts DESC LIMIT 10;"

# Test PostgreSQL connectivity
acode db test-connection --postgres
```

**Solutions:**

1. **Restart Sync Worker:**
   ```bash
   # Force restart sync worker
   acode db sync-restart
   [INFO] Stopping sync worker...
   [INFO] Starting sync worker...
   [INFO] Sync worker started (processing oldest-first)
   [INFO] Outbox: 472 pending records
   
   # Monitor sync progress
   acode db sync-status --watch
   ```

2. **Manual Sync Trigger:**
   ```bash
   # Manually trigger sync (foreground)
   acode db sync --manual --batch-size 100
   [INFO] Syncing 100 oldest outbox records...
   [INFO] Synced 100/100 (duration: 8.2s)
   [INFO] Remaining: 372 pending records
   
   # Continue until outbox empty
   ```

3. **Clear Failed Records:**
   ```bash
   # Query records that exhausted retries
   sqlite3 .agent/workspace.db "SELECT * FROM outbox WHERE attempts >= 10 AND processed_at IS NULL;"
   
   # Mark as failed (manual intervention required)
   acode db outbox-mark-failed --attempts-gte 10
   [WARN] Marking 12 outbox records as failed (exhausted retries)
   [WARN] These records will not sync automatically. Manual review required.
   ```

4. **Reset Idempotency Conflicts:**
   ```bash
   # If idempotency key conflicts suspected
   acode db outbox-reset-keys
   [INFO] Regenerating idempotency keys for pending records
   [INFO] Updated 472 records with new timestamps
   ```

5. **Increase Retry Limits:**
   ```yaml
   # .agent/config.yml - increase max retry attempts
   persistence:
     sync:
       max_retry_attempts: 20  # Default: 10
       max_backoff: 2h         # Default: 1h
   ```

**Prevention:**
- Monitor outbox depth with alerts (threshold: 500 pending)
- Enable sync worker heartbeat monitoring
- Log all sync failures with context
- Implement automatic sync worker restart on crash

---

### Problem 3: "Database Locked" Error During Write Operation

**Symptoms:**
- Application throws "database is locked" exception
- Error occurs during session creation or state transition
- SQLite database appears healthy but writes fail intermittently
- Error more frequent under high load (multiple concurrent sessions)

**Possible Causes:**
1. Long-running read transaction blocking writes
2. Multiple processes accessing same SQLite database
3. SQLite busy timeout too short
4. WAL mode not enabled (using DELETE journal mode)
5. Database file on network drive (not supported)

**Diagnosis:**

```bash
# Check SQLite journal mode
sqlite3 .agent/workspace.db "PRAGMA journal_mode;"
# Should output: wal (not delete or truncate)

# Check for concurrent processes
fuser .agent/workspace.db    # Linux
lsof .agent/workspace.db     # macOS

# Check busy timeout setting
sqlite3 .agent/workspace.db "PRAGMA busy_timeout;"
# Should be >= 5000 (5 seconds)

# Verify database location (not network drive)
df -h .agent/workspace.db
mount | grep $(df .agent/workspace.db | tail -1 | awk '{print $1}')
```

**Solutions:**

1. **Enable WAL Mode:**
   ```bash
   # Enable Write-Ahead Logging for better concurrency
   sqlite3 .agent/workspace.db "PRAGMA journal_mode=WAL;"
   sqlite3 .agent/workspace.db "PRAGMA synchronous=NORMAL;"
   
   # Verify WAL files created
   ls -lh .agent/workspace.db*
   # Should see: workspace.db, workspace.db-shm, workspace.db-wal
   ```

2. **Increase Busy Timeout:**
   ```csharp
   // In SQLite connection configuration
   var connectionString = new SqliteConnectionStringBuilder
   {
       DataSource = ".agent/workspace.db",
       Mode = SqliteOpenMode.ReadWriteCreate,
       BusyTimeout = 30000 // 30 seconds (default: 5 seconds)
   }.ToString();
   ```

3. **Close Long-Running Transactions:**
   ```bash
   # Identify long-running transactions
   sqlite3 .agent/workspace.db "SELECT * FROM pragma_wal_checkpoint(PASSIVE);"
   
   # Force checkpoint to free up WAL
   sqlite3 .agent/workspace.db "PRAGMA wal_checkpoint(TRUNCATE);"
   ```

4. **Move Database to Local Drive:**
   ```bash
   # If database on network drive, move to local
   mv /mnt/network/.agent/workspace.db ~/.agent/workspace.db
   
   # Update configuration
   acode config set persistence.sqlite.path "~/.agent/workspace.db"
   ```

5. **Use Connection Pooling Correctly:**
   ```csharp
   // Ensure connections are properly disposed
   public async Task<Session> GetSessionAsync(SessionId id, CancellationToken ct)
   {
       using var connection = new SqliteConnection(_connectionString);
       await connection.OpenAsync(ct);
       
       // Use connection...
       
       // Connection automatically closed and returned to pool
   }
   ```

**Prevention:**
- Always enable WAL mode for SQLite databases
- Set busy timeout to 30+ seconds
- Use `using` statements for connection disposal
- Avoid network drives for SQLite files
- Monitor lock contention with metrics

---

## User Manual Documentation

### Overview

Acode uses a two-tier persistence model. SQLite stores data locally for fast, offline operation. PostgreSQL optionally provides centralized backup and visibility.

### Configuration

#### SQLite (Default)

SQLite requires no configuration. The database is created automatically:

```
.agent/
└── workspace.db    # SQLite database
```

#### PostgreSQL (Optional)

Configure PostgreSQL in `.agent/config.yml`:

```yaml
persistence:
  postgres:
    enabled: true
    connection_string_env: ACODE_POSTGRES_URL
    # OR explicit connection (not recommended)
    # host: localhost
    # port: 5432
    # database: acode
    # username: acode
    # password_env: ACODE_POSTGRES_PASSWORD
```

Environment variable:

```bash
export ACODE_POSTGRES_URL="postgresql://user:pass@host:5432/acode"
```

### CLI Commands

```bash
# Check database status
$ acode db status
SQLite: .agent/workspace.db (12.5 MB)
  Version: 1.0.0
  Sessions: 45
  Last modified: 2024-01-15T10:30:00Z

PostgreSQL: connected
  Host: db.example.com:5432
  Database: acode
  Sync status: up to date
  Outbox depth: 0

# View sync status
$ acode db sync status
Outbox records: 3 pending
  - Session abc123 (created 5m ago)
  - Task def456 (created 3m ago)
  - Step ghi789 (created 1m ago)

Last sync: 2024-01-15T10:25:00Z
Next retry: 2024-01-15T10:30:00Z

# Force sync
$ acode db sync now
Syncing 3 records...
  ✓ Session abc123
  ✓ Task def456
  ✓ Step ghi789
Sync complete.

# Run migrations
$ acode db migrate
Current version: 1.0.0
Target version: 1.1.0
Running migration 1.0.0 → 1.1.0...
  ✓ Add artifacts table
  ✓ Add index on session_id
Migration complete.

# Check database integrity
$ acode db check
Checking SQLite integrity...
  ✓ No corruption detected
  ✓ Foreign keys valid
  ✓ Indexes valid

# Vacuum database
$ acode db vacuum
Vacuuming .agent/workspace.db...
  Before: 12.5 MB
  After: 10.2 MB
  Saved: 2.3 MB
```

### Database Schema

#### sessions

```sql
CREATE TABLE sessions (
    id TEXT PRIMARY KEY,
    task_description TEXT NOT NULL,
    state TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    metadata TEXT,
    sync_version INTEGER DEFAULT 0
) STRICT;
```

#### session_events

```sql
CREATE TABLE session_events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT NOT NULL REFERENCES sessions(id),
    from_state TEXT NOT NULL,
    to_state TEXT NOT NULL,
    reason TEXT NOT NULL,
    timestamp TEXT NOT NULL
) STRICT;
```

#### session_tasks

```sql
CREATE TABLE session_tasks (
    id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL REFERENCES sessions(id),
    title TEXT NOT NULL,
    description TEXT,
    state TEXT NOT NULL,
    "order" INTEGER NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    metadata TEXT,
    sync_version INTEGER DEFAULT 0
) STRICT;
```

#### steps

```sql
CREATE TABLE steps (
    id TEXT PRIMARY KEY,
    task_id TEXT NOT NULL REFERENCES session_tasks(id),
    name TEXT NOT NULL,
    description TEXT,
    state TEXT NOT NULL,
    "order" INTEGER NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    metadata TEXT,
    sync_version INTEGER DEFAULT 0
) STRICT;
```

#### tool_calls

```sql
CREATE TABLE tool_calls (
    id TEXT PRIMARY KEY,
    step_id TEXT NOT NULL REFERENCES steps(id),
    tool_name TEXT NOT NULL,
    parameters TEXT NOT NULL,
    state TEXT NOT NULL,
    "order" INTEGER NOT NULL,
    created_at TEXT NOT NULL,
    completed_at TEXT,
    result TEXT,
    error_message TEXT,
    sync_version INTEGER DEFAULT 0
) STRICT;
```

#### artifacts

```sql
CREATE TABLE artifacts (
    id TEXT PRIMARY KEY,
    tool_call_id TEXT NOT NULL REFERENCES tool_calls(id),
    type TEXT NOT NULL,
    name TEXT NOT NULL,
    content BLOB NOT NULL,
    content_hash TEXT NOT NULL,
    content_type TEXT NOT NULL,
    size INTEGER NOT NULL,
    created_at TEXT NOT NULL
) STRICT;
```

#### outbox

```sql
CREATE TABLE outbox (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    idempotency_key TEXT UNIQUE NOT NULL,
    entity_type TEXT NOT NULL,
    entity_id TEXT NOT NULL,
    operation TEXT NOT NULL,
    payload TEXT NOT NULL,
    created_at TEXT NOT NULL,
    processed_at TEXT,
    attempts INTEGER DEFAULT 0,
    last_error TEXT
) STRICT;

CREATE INDEX idx_outbox_pending ON outbox(processed_at) 
    WHERE processed_at IS NULL;
```

### Sync Behavior

#### Normal Operation

1. User performs action
2. SQLite updated in transaction
3. Outbox record created
4. Operation returns to user (immediate)
5. Background sync processes outbox
6. PostgreSQL updated
7. Outbox marked processed

#### Network Unavailable

1. User performs action
2. SQLite updated (works offline)
3. Outbox record created
4. Background sync fails
5. Retry with backoff
6. When network returns, sync resumes

#### Conflict Resolution

When same entity modified on multiple machines:

1. Sync detects version mismatch
2. Compare timestamps
3. Latest timestamp wins
4. Conflict logged for audit
5. Loser's changes overwritten

### Troubleshooting

#### Database Locked

**Problem:** "database is locked" error

**Solutions:**
1. Close other Acode processes
2. Check for stale lock files
3. Wait and retry (temporary)
4. Increase timeout in config

#### Sync Failing

**Problem:** Outbox depth growing

**Solutions:**
1. Check PostgreSQL connectivity
2. Check credentials
3. View sync errors: `acode db sync status --verbose`
4. Force retry: `acode db sync now`

#### Schema Mismatch

**Problem:** "schema version mismatch" on startup

**Solutions:**
1. Run migrations: `acode db migrate`
2. Check for incompatible versions
3. Backup and recreate if needed

#### Corruption

**Problem:** Database integrity check fails

**Solutions:**
1. Restore from backup
2. Export valid data: `acode db export`
3. Create new database
4. Import: `acode db import`

---

## Acceptance Criteria

### SQLite Storage

- [ ] AC-001: Database in .agent/workspace.db
- [ ] AC-002: WAL mode enabled
- [ ] AC-003: STRICT tables used
- [ ] AC-004: Auto-created on first access
- [ ] AC-005: Writes transactional
- [ ] AC-006: Transaction timeout 30s
- [ ] AC-007: Concurrent reads work
- [ ] AC-008: Write locks exclusive

### Schema

- [ ] AC-009: Schema version tracked
- [ ] AC-010: Migrations versioned
- [ ] AC-011: Auto-migrate on startup
- [ ] AC-012: Migrations idempotent
- [ ] AC-013: Migration failure halts startup
- [ ] AC-014: Version queryable

### Tables

- [ ] AC-015: sessions table exists
- [ ] AC-016: session_events table exists
- [ ] AC-017: session_tasks table exists
- [ ] AC-018: steps table exists
- [ ] AC-019: tool_calls table exists
- [ ] AC-020: artifacts table exists
- [ ] AC-021: outbox table exists
- [ ] AC-022: All have timestamps

### PostgreSQL

- [ ] AC-023: Configurable via config
- [ ] AC-024: Connection string from env
- [ ] AC-025: Missing doesn't fail startup
- [ ] AC-026: Schema mirrors SQLite
- [ ] AC-027: Indexes optimized
- [ ] AC-028: Pool size configurable

### Outbox

- [ ] AC-029: Records have idempotency_key
- [ ] AC-030: Records have entity_type
- [ ] AC-031: Records have entity_id
- [ ] AC-032: Records have payload
- [ ] AC-033: Records have created_at
- [ ] AC-034: Records track processed_at
- [ ] AC-035: Records track attempts

### Sync

- [ ] AC-036: Runs in background
- [ ] AC-037: Non-blocking
- [ ] AC-038: Oldest first
- [ ] AC-039: Retries failed
- [ ] AC-040: Exponential backoff
- [ ] AC-041: Max 10 attempts
- [ ] AC-042: Resumable after restart

### Idempotency

- [ ] AC-043: Unique keys generated
- [ ] AC-044: Key format correct
- [ ] AC-045: Duplicates rejected
- [ ] AC-046: Rejection marks processed
- [ ] AC-047: Logged

### Conflicts

- [ ] AC-048: Detected
- [ ] AC-049: Latest wins
- [ ] AC-050: Logged
- [ ] AC-051: Counted
- [ ] AC-052: Details preserved

### Performance

- [ ] AC-053: SQLite write < 50ms
- [ ] AC-054: SQLite read < 10ms
- [ ] AC-055: Sync 100 records/sec
- [ ] AC-056: Query with pagination < 100ms

### Security

- [ ] AC-057: File permissions 600
- [ ] AC-058: Connection string not logged
- [ ] AC-059: SQL injection prevented

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Infrastructure/Persistence/
├── SQLiteConnectionTests.cs
│   ├── Should_Create_Database()
│   ├── Should_Enable_WAL_Mode()
│   └── Should_Use_Strict_Tables()
│
├── MigrationRunnerTests.cs
│   ├── Should_Run_Migrations_In_Order()
│   ├── Should_Be_Idempotent()
│   └── Should_Halt_On_Failure()
│
├── SessionRepositoryTests.cs
│   ├── Should_Create_Session()
│   ├── Should_Update_Session()
│   ├── Should_Query_By_Id()
│   └── Should_List_With_Filter()
│
├── OutboxTests.cs
│   ├── Should_Add_Record()
│   ├── Should_Generate_Idempotency_Key()
│   └── Should_Mark_Processed()
│
└── SyncServiceTests.cs
    ├── Should_Process_Oldest_First()
    ├── Should_Retry_With_Backoff()
    └── Should_Handle_Duplicates()
```

### Integration Tests

```
Tests/Integration/Persistence/
├── SQLiteIntegrationTests.cs
│   ├── Should_Persist_Full_Hierarchy()
│   ├── Should_Survive_Crash()
│   └── Should_Handle_Concurrent_Access()
│
├── PostgreSQLIntegrationTests.cs
│   ├── Should_Sync_From_Outbox()
│   ├── Should_Handle_Network_Failure()
│   └── Should_Resolve_Conflicts()
│
└── MigrationIntegrationTests.cs
    ├── Should_Migrate_From_Empty()
    └── Should_Migrate_Incrementally()
```

### E2E Tests

```
Tests/E2E/Persistence/
├── OfflineOperationTests.cs
│   ├── Should_Work_Without_Postgres()
│   ├── Should_Queue_For_Sync()
│   └── Should_Sync_When_Available()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| SQLite single write | 25ms | 50ms |
| SQLite single read | 5ms | 10ms |
| Sync throughput | 200/sec | 100/sec |
| Full hierarchy query | 50ms | 100ms |
| Migration execution | 1s | 5s |

### Regression Tests

- Schema after migration
- Sync after format change
- Query performance after data growth

---

## User Verification Steps

### Scenario 1: Auto-Create Database

1. Delete .agent/workspace.db
2. Run `acode status`
3. Verify: Database created
4. Verify: Schema correct

### Scenario 2: Persist Session

1. Run `acode run "task"`
2. Kill process mid-run
3. Run `acode resume`
4. Verify: Session state preserved

### Scenario 3: View Database Status

1. Run `acode db status`
2. Verify: SQLite info shown
3. Verify: Session count correct

### Scenario 4: PostgreSQL Sync

1. Configure PostgreSQL
2. Run `acode run "task"`
3. Run `acode db sync status`
4. Verify: Sync occurred

### Scenario 5: Offline Operation

1. Disconnect network
2. Run `acode run "task"`
3. Verify: Works normally
4. Verify: Outbox populated

### Scenario 6: Sync Recovery

1. Run while offline
2. Reconnect network
3. Wait or `acode db sync now`
4. Verify: Data synced

### Scenario 7: Run Migrations

1. Run `acode db migrate`
2. Verify: Migrations applied
3. Verify: Version updated

### Scenario 8: Database Check

1. Run `acode db check`
2. Verify: No errors
3. Verify: Indexes valid

### Scenario 9: Vacuum

1. Run `acode db vacuum`
2. Verify: Size reduced
3. Verify: Data intact

### Scenario 10: Conflict Resolution

1. Modify session on machine A
2. Modify same session on machine B (offline)
3. Sync machine B
4. Verify: Latest wins
5. Verify: Conflict logged

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/
├── Persistence/
│   ├── SQLite/
│   │   ├── SQLiteConnectionFactory.cs
│   │   ├── SQLiteRunStateStore.cs
│   │   ├── SQLiteOutbox.cs
│   │   └── Migrations/
│   │       ├── IMigration.cs
│   │       ├── MigrationRunner.cs
│   │       └── Migrations/
│   │           ├── V1_0_0_InitialSchema.cs
│   │           └── V1_1_0_AddArtifacts.cs
│   │
│   ├── PostgreSQL/
│   │   ├── PostgreSQLConnectionFactory.cs
│   │   ├── PostgreSQLRunStateStore.cs
│   │   └── PostgreSQLSyncTarget.cs
│   │
│   └── Sync/
│       ├── ISyncService.cs
│       ├── SyncService.cs
│       ├── OutboxProcessor.cs
│       └── ConflictResolver.cs
│
src/AgenticCoder.Application/
└── Sessions/
    ├── IRunStateStore.cs
    └── IOutbox.cs
```

### IRunStateStore Interface

```csharp
namespace AgenticCoder.Application.Sessions;

public interface IRunStateStore
{
    Task<Session?> GetAsync(SessionId id, CancellationToken ct);
    Task SaveAsync(Session session, CancellationToken ct);
    Task<IReadOnlyList<Session>> ListAsync(SessionFilter filter, CancellationToken ct);
    Task<IReadOnlyList<SessionEvent>> GetEventsAsync(SessionId id, CancellationToken ct);
    Task<Session?> GetWithHierarchyAsync(SessionId id, CancellationToken ct);
}
```

### IOutbox Interface

```csharp
namespace AgenticCoder.Application.Sessions;

public interface IOutbox
{
    Task EnqueueAsync(OutboxRecord record, CancellationToken ct);
    Task<IReadOnlyList<OutboxRecord>> GetPendingAsync(int limit, CancellationToken ct);
    Task MarkProcessedAsync(long id, CancellationToken ct);
    Task MarkFailedAsync(long id, string error, CancellationToken ct);
    Task<int> GetPendingCountAsync(CancellationToken ct);
}

public sealed record OutboxRecord(
    long Id,
    string IdempotencyKey,
    string EntityType,
    string EntityId,
    string Operation,
    JsonDocument Payload,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    int Attempts,
    string? LastError);
```

### ISyncService Interface

```csharp
namespace AgenticCoder.Infrastructure.Persistence.Sync;

public interface ISyncService
{
    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    Task SyncNowAsync(CancellationToken ct);
    SyncStatus GetStatus();
    bool IsRunning { get; }
}

public sealed record SyncStatus(
    int PendingCount,
    DateTimeOffset? LastSyncTime,
    DateTimeOffset? NextRetryTime,
    int FailedCount);
```

### Migration Interface

```csharp
namespace AgenticCoder.Infrastructure.Persistence.SQLite.Migrations;

public interface IMigration
{
    string Version { get; }
    string Description { get; }
    Task UpAsync(SqliteConnection connection, CancellationToken ct);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-DB-001 | Database connection failed |
| ACODE-DB-002 | Transaction failed |
| ACODE-DB-003 | Migration failed |
| ACODE-DB-004 | Schema version mismatch |
| ACODE-DB-005 | Sync failed |
| ACODE-DB-006 | Conflict detected |
| ACODE-DB-007 | Integrity check failed |

### Logging Fields

```json
{
  "event": "database_write",
  "database": "sqlite",
  "table": "sessions",
  "operation": "insert",
  "entity_id": "abc123",
  "duration_ms": 12,
  "outbox_enqueued": true
}
```

### Configuration Schema

```yaml
persistence:
  sqlite:
    path: ".agent/workspace.db"
    wal_mode: true
    timeout_seconds: 30
    
  postgres:
    enabled: false
    connection_string_env: "ACODE_POSTGRES_URL"
    pool_size: 10
    
  sync:
    enabled: true
    interval_seconds: 30
    max_batch_size: 100
    max_retry_attempts: 10
    initial_backoff_seconds: 5
    max_backoff_seconds: 3600
```

### Implementation Checklist

1. [ ] Create SQLiteConnectionFactory
2. [ ] Implement WAL mode and STRICT tables
3. [ ] Create migration system
4. [ ] Create initial schema migration
5. [ ] Implement SQLiteRunStateStore
6. [ ] Implement SQLiteOutbox
7. [ ] Create PostgreSQLConnectionFactory
8. [ ] Implement PostgreSQLRunStateStore
9. [ ] Create SyncService
10. [ ] Create OutboxProcessor
11. [ ] Implement exponential backoff
12. [ ] Implement ConflictResolver
13. [ ] Add CLI commands (db status, sync, migrate)
14. [ ] Write unit tests
15. [ ] Write integration tests
16. [ ] Add performance benchmarks

### Validation Checklist Before Merge

- [ ] SQLite auto-creates on first use
- [ ] WAL mode verified
- [ ] Migrations run automatically
- [ ] Session CRUD works
- [ ] Outbox populated on writes
- [ ] Sync processes outbox
- [ ] Idempotency prevents duplicates
- [ ] Conflicts resolved correctly
- [ ] Offline operation works
- [ ] Performance targets met
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** SQLite core
2. **Phase 2:** Schema and migrations
3. **Phase 3:** Session repository
4. **Phase 4:** Outbox pattern
5. **Phase 5:** PostgreSQL connection
6. **Phase 6:** Sync service
7. **Phase 7:** CLI commands
8. **Phase 8:** Performance tuning

---

**End of Task 011.b Specification**