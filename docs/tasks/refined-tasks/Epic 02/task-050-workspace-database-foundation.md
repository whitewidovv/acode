# Task 050: Workspace Database Foundation

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 2 – Persistence + Reliability Core  
**Dependencies:** Task 000 (Project Structure), Task 002 (Config), Task 003 (CLI)  

---

## Description

### Business Value and ROI

The workspace database foundation represents the most critical infrastructure investment in Acode's architecture. Every stateful operation—session management, conversation history, approval records, sync queues, configuration persistence—depends on reliable database access. A poorly designed persistence layer cascades failures throughout the entire system, while a well-designed foundation enables rapid feature development and operational reliability.

**Quantified Business Value:**

| Impact Area | Before (Without Task 050) | After (With Task 050) | Annual ROI |
|-------------|---------------------------|----------------------|------------|
| Feature Development | 40+ hours per feature for custom persistence | 8 hours using shared patterns | $128,000/year (40 features × 32 hours × $100/hr) |
| Production Incidents | 15 incidents/year from data corruption | 1 incident/year with proper transactions | $56,000/year (14 incidents × 40 hours × $100/hr) |
| Developer Onboarding | 2 weeks to understand ad-hoc storage | 2 days with standardized patterns | $24,000/year (10 devs × 8 days × $300/day) |
| Migration Safety | 50% success rate for schema changes | 99.5% success with migration framework | $20,000/year (avoided rollbacks) |
| Cross-device Sync | Impossible without PostgreSQL | Seamless with dual-provider architecture | $80,000/year (team productivity) |
| **Total Annual ROI** | | | **$308,000/year** |

**Why This Foundation Matters:**

Without a shared database foundation, each feature team would implement its own persistence:
- Task 049 (Conversations) would create its own SQLite handling
- Task 011 (Sessions) would duplicate connection management
- Task 013 (Approvals) would reinvent migration patterns
- Task 026 (Queue) would build another transaction layer

This duplication wastes developer time, creates inconsistent behaviors, and multiplies bug surface area. By investing 8-10 days in a proper foundation, we save 100+ days of duplicated work across 20+ downstream tasks.

### Technical Architecture

The workspace database foundation implements a three-tier architecture:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        Application Layer                                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │
│  │ Chat Repo   │  │ Run Repo    │  │ Message Repo│  │ Approval    │    │
│  │ (Task 049a) │  │ (Task 011b) │  │ (Task 049a) │  │ Repo (013b) │    │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘    │
│         │                │                │                │            │
│         ▼                ▼                ▼                ▼            │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │              IConnectionFactory / ITransaction                   │   │
│  │                   (Task 050 provides)                           │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                     Infrastructure Layer                                 │
│  ┌────────────────────────┐       ┌────────────────────────┐           │
│  │ SqliteConnectionFactory│       │PostgresConnectionFactory│           │
│  │  - WAL mode enabled    │       │  - Connection pooling   │           │
│  │  - Busy timeout        │       │  - SSL/TLS support      │           │
│  │  - Single file db      │       │  - Retries with backoff │           │
│  └───────────┬────────────┘       └───────────┬────────────┘           │
│              │                                │                         │
│              ▼                                ▼                         │
│  ┌────────────────────────┐       ┌────────────────────────┐           │
│  │   Migration Runner     │       │    Health Check        │           │
│  │  - Version tracking    │       │  - Connection test     │           │
│  │  - Forward/rollback    │       │  - Latency monitoring  │           │
│  │  - Checksum validation │       │  - Pool status         │           │
│  └────────────────────────┘       └────────────────────────┘           │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         Data Layer                                       │
│  ┌────────────────────────┐       ┌────────────────────────┐           │
│  │  .agent/data/          │       │   PostgreSQL Server    │           │
│  │    workspace.db        │       │   - acode database     │           │
│  │    workspace.db-wal    │       │   - Connection pool    │           │
│  │    workspace.db-shm    │       │   - TLS encryption     │           │
│  └────────────────────────┘       └────────────────────────┘           │
│                                                                         │
│  Local (offline-capable)          Remote (team sync)                    │
└─────────────────────────────────────────────────────────────────────────┘
```

### Dual-Provider Architecture

The workspace database supports two database providers with automatic failover and seamless switching:

**SQLite (Local Provider):**
- Zero-configuration embedded database
- Single file storage in `.agent/data/workspace.db`
- WAL (Write-Ahead Logging) mode for concurrent reads during writes
- Busy timeout handling for multi-process access
- Ideal for: single-user development, offline work, CI/CD pipelines

**PostgreSQL (Remote Provider):**
- Full-featured relational database server
- Connection pooling with configurable min/max connections
- SSL/TLS encrypted connections
- Ideal for: team environments, cross-device sync, enterprise deployments

**Provider Selection Logic:**
```
1. Check configuration for explicit provider selection
2. If remote.enabled = true AND network available → PostgreSQL
3. If remote.enabled = false OR network unavailable → SQLite
4. Fallback to SQLite if PostgreSQL connection fails (graceful degradation)
```

### Migration Framework Architecture

Schema evolution is managed through a migration framework that ensures safe, versioned, reversible changes:

**Migration File Structure:**
```
migrations/
├── 001_initial_schema.sql          # Forward migration
├── 001_initial_schema_down.sql     # Rollback migration
├── 002_add_conversations.sql
├── 002_add_conversations_down.sql
├── 003_add_sync_status.sql
├── 003_add_sync_status_down.sql
└── ...
```

**Migration Lifecycle:**
```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Pending    │────▶│  Applying    │────▶│   Applied    │
│              │     │              │     │              │
│ File exists  │     │ Transaction  │     │ Recorded in  │
│ Not in DB    │     │ open         │     │ sys_migratns │
└──────────────┘     └──────┬───────┘     └──────────────┘
                           │
                           │ (on failure)
                           ▼
                    ┌──────────────┐
                    │  Rolled Back │
                    │              │
                    │ Changes      │
                    │ reverted     │
                    └──────────────┘
```

**Version Tracking Table (sys_migrations):**
```sql
CREATE TABLE sys_migrations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    version TEXT NOT NULL UNIQUE,           -- "001_initial_schema"
    applied_at TEXT NOT NULL,               -- ISO8601 timestamp
    checksum TEXT NOT NULL,                 -- SHA256 of migration content
    applied_by TEXT,                        -- Username/hostname
    execution_time_ms INTEGER               -- How long migration took
);
```

### Connection Pool Management

PostgreSQL connections are expensive to establish (~100-200ms). Connection pooling maintains a warm pool of ready connections:

**Pool Configuration:**
```yaml
pool:
  min_size: 2       # Always keep 2 connections warm
  max_size: 10      # Never exceed 10 connections
  idle_timeout: 300 # Close idle connections after 5 minutes
  lifetime: 3600    # Recycle connections after 1 hour
```

**Pool States:**
```
┌─────────────────────────────────────────────────────────────────────┐
│                    Connection Pool State                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [IDLE] [IDLE] [BUSY] [BUSY] [BUSY] [    ] [    ] [    ] [    ] [  ]│
│    ▲      ▲      ▲      ▲      ▲      │      │      │      │      │ │
│    │      │      │      │      │      └──────┴──────┴──────┴──────┘ │
│    │      │      │      │      │                                     │
│  min=2  warm   active connections          available slots           │
│                                                                      │
│  Total: 5 active (2 idle, 3 busy)  Max: 10                          │
└─────────────────────────────────────────────────────────────────────┘
```

### Transaction Management

Transactions ensure ACID properties for multi-statement operations:

**Transaction Scopes:**
```csharp
// Unit of Work pattern
using var transaction = await connectionFactory.BeginTransactionAsync(ct);
try
{
    await chatRepository.CreateAsync(chat, ct);
    await messageRepository.CreateAsync(message, ct);
    await transaction.CommitAsync(ct);
}
catch
{
    await transaction.RollbackAsync(ct);
    throw;
}
```

**Transaction Isolation Levels:**
| Level | SQLite | PostgreSQL | Use Case |
|-------|--------|------------|----------|
| Read Committed | WAL mode default | Default | Standard reads |
| Serializable | IMMEDIATE | SERIALIZABLE | Financial operations |
| Read Uncommitted | Not supported | READ UNCOMMITTED | Analytics only |

### Error Handling Strategy

Database operations can fail in predictable ways. Each failure type has specific handling:

**Transient Failures (Retry):**
- Connection timeout → Retry with exponential backoff (100ms, 200ms, 400ms, max 3 retries)
- Server busy → Wait and retry
- Network blip → Reconnect and retry

**Permanent Failures (Throw):**
- Constraint violation → Throw with specific error code (ACODE-DB-010)
- Invalid SQL → Throw with query details (dev mode only)
- Missing table → Throw migration required error

**Degradation Failures (Fallback):**
- PostgreSQL unavailable → Fall back to SQLite (if configured)
- SSL certificate error → Log warning, continue with warning flag

**Circuit Breaker Pattern:**
```
┌────────────┐    5 failures    ┌────────────┐    30 seconds    ┌─────────────┐
│   Closed   │─────────────────▶│    Open    │─────────────────▶│  Half-Open  │
│            │                  │            │                  │             │
│ Normal ops │                  │ Fast-fail  │                  │ Probe test  │
└────────────┘                  └────────────┘                  └──────┬──────┘
      ▲                                                                │
      │                              success                           │
      └────────────────────────────────────────────────────────────────┘
```

### Integration Points

Task 050 provides the foundation that other tasks build upon:

| Dependent Task | Integration Point | What Task 050 Provides |
|----------------|-------------------|------------------------|
| Task 049a | ChatRepository, RunRepository | IConnectionFactory, migrations |
| Task 049b | CLI database commands | DatabaseCommand base class |
| Task 049c | Worktree binding persistence | Transaction support |
| Task 049d | FTS5 search indexing | SQLite with FTS5 extension |
| Task 049f | Sync outbox/inbox tables | PostgreSQL connector, migrations |
| Task 011b | Session persistence | Repository patterns, connection factory |
| Task 013b | Approval records | Transaction scopes |
| Task 026a | Queue persistence | SQLite schema migrations |

### Constraints and Limitations

**Technical Constraints:**
1. SQLite version 3.35+ required for JSON functions and window functions
2. PostgreSQL version 13+ required for proper JSON support and performance
3. Single-writer model for SQLite (WAL enables concurrent reads)
4. No distributed transactions across SQLite and PostgreSQL
5. Migration files are immutable once applied (checksum verification)
6. Maximum 10,000 rows per table for SQLite performance (soft limit)
7. Connection pool size limited by PostgreSQL max_connections setting

**Operational Constraints:**
1. Database file must be on local filesystem (no network drives for SQLite)
2. Backup operations require exclusive access
3. Migrations must be sequential (no parallel migration execution)
4. Schema changes require application restart to take effect
5. PostgreSQL connection requires network access

### Trade-offs and Design Decisions

**Decision 1: ADO.NET vs Entity Framework**
- Chose: Raw ADO.NET with Dapper for queries
- Rationale: Better performance, no ORM overhead, explicit SQL control
- Trade-off: More boilerplate, but full control over queries

**Decision 2: Embedded migrations vs DbUp/FluentMigrator**
- Chose: Custom embedded migration runner
- Rationale: No external dependencies, full control, simpler debugging
- Trade-off: Must maintain migration runner code

**Decision 3: WAL mode vs Delete journal**
- Chose: WAL (Write-Ahead Logging) mode for SQLite
- Rationale: Concurrent reads during writes, better crash recovery
- Trade-off: Additional .db-wal and .db-shm files

**Decision 4: Single file vs per-feature databases**
- Chose: Single workspace.db file for all features
- Rationale: Simpler backup, single transaction scope, easier migration
- Trade-off: All features share same file lock

### Performance Targets

| Operation | Target | Maximum | Measurement Method |
|-----------|--------|---------|-------------------|
| Connection acquire (SQLite) | 1ms | 5ms | Stopwatch in ConnectionFactory |
| Connection acquire (PostgreSQL pool hit) | 2ms | 10ms | Stopwatch in ConnectionFactory |
| Connection acquire (PostgreSQL new) | 50ms | 200ms | Stopwatch in ConnectionFactory |
| Simple INSERT | 2ms | 10ms | Benchmark suite |
| Simple SELECT by PK | 0.5ms | 5ms | Benchmark suite |
| Transaction commit | 5ms | 20ms | Benchmark suite |
| Migration step (average) | 500ms | 5s | Migration runner timing |
| Health check | 5ms | 50ms | Health endpoint timing |

### Observability

**Metrics (exported to JSONL events):**
- `db.connections.active` - Current active connections
- `db.connections.idle` - Current idle pool connections
- `db.query.duration_ms` - Query execution time histogram
- `db.migrations.pending` - Count of unapplied migrations
- `db.health.status` - 1=healthy, 0=unhealthy

**Structured Logging Fields:**
```json
{
  "event": "db.query.executed",
  "provider": "sqlite|postgres",
  "operation": "select|insert|update|delete|transaction",
  "table": "chats|runs|messages|...",
  "duration_ms": 5,
  "rows_affected": 1,
  "correlation_id": "01HXYZ..."
}
```

**Error Codes:**
| Code | Description | Recovery Action |
|------|-------------|-----------------|
| ACODE-DB-001 | Connection failed | Retry with backoff |
| ACODE-DB-002 | Migration failed | Check migration file |
| ACODE-DB-003 | Transaction failed | Check for deadlock |
| ACODE-DB-004 | Database locked | Wait or kill blocking process |
| ACODE-DB-005 | Schema mismatch | Run migrations |
| ACODE-DB-006 | Constraint violation | Fix data or constraint |
| ACODE-DB-007 | Query timeout | Optimize query or increase timeout |
| ACODE-DB-008 | Pool exhausted | Increase pool size or check for leaks |

---

## Use Cases

### Use Case 1: DevBot - Feature Development with Shared Database Patterns

**Persona:** DevBot, a senior developer at a mid-sized SaaS company, is implementing conversation history storage for their AI assistant. The company has 15 developers and deploys to both on-premises and cloud environments.

**Before Task 050 (Custom Persistence):**
DevBot starts implementing conversation storage and immediately faces decisions:
- "Should I use SQLite or PostgreSQL? Both?"
- "How do I handle concurrent access?"
- "What's the migration strategy?"
- "How do I test this without a real database?"

DevBot spends 3 weeks building custom persistence:
- Week 1: Research and prototype SQLite wrapper
- Week 2: Add PostgreSQL support with connection pooling
- Week 3: Build migration framework and test infrastructure

Total: **120 hours of development time**, resulting in code that:
- Has inconsistent error handling
- Lacks proper connection pooling
- Has no migration rollback capability
- Is not reusable by other features

**After Task 050 (Foundation in Place):**
DevBot opens the conversation task and sees clear integration points:
```csharp
public class ChatRepository : IChatRepository
{
    private readonly IConnectionFactory _connectionFactory;
    
    public ChatRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;  // Injected by DI
    }
    
    public async Task<Chat> CreateAsync(Chat chat, CancellationToken ct)
    {
        using var connection = await _connectionFactory.CreateAsync(ct);
        // Focus on business logic, not connection management
    }
}
```

DevBot implements the feature in 3 days:
- Day 1: Define Chat entity and repository interface
- Day 2: Implement repository with provided connection factory
- Day 3: Add migrations using established patterns

Total: **24 hours of development time**, with:
- Consistent error handling (provided by foundation)
- Proper connection pooling (built into factory)
- Migration support with rollback (framework ready)
- Patterns reused across 20+ features

**Metrics:**
- Development time: 120 hours → 24 hours (80% reduction)
- Code consistency: 0% reuse → 95% pattern reuse
- Bug density: ~15 persistence bugs → ~2 bugs (87% reduction)
- **Annual savings per developer: $9,600** (96 hours × $100/hr)
- **Team annual savings (15 developers): $144,000**

---

### Use Case 2: Jordan - Production Migration with Zero Downtime

**Persona:** Jordan, a DevOps engineer, is responsible for upgrading Acode in production. The company runs Acode on 50 developer workstations and 3 shared servers. A failed migration would disrupt 50 developers for 2+ hours.

**Before Task 050 (Ad-hoc Migration):**
Jordan receives a new Acode version with schema changes. The release notes say "run these SQL commands manually":
```sql
ALTER TABLE conversations ADD COLUMN sync_status TEXT;
CREATE INDEX idx_conversations_sync ON conversations(sync_status);
```

Jordan's deployment process:
1. Schedule 2-hour maintenance window (lost productivity: 50 devs × 2 hours = 100 dev-hours)
2. Backup databases manually (45 minutes)
3. Run SQL commands on each database (30 minutes per server × 3 servers = 90 minutes)
4. If error occurs, restore from backup (45 minutes)
5. Verify data integrity manually (30 minutes)

**Risk realization:** One server fails mid-migration due to disk space. Jordan spends 3 hours debugging, restoring backup, and retrying. Total downtime: 5 hours.

**After Task 050 (Migration Framework):**
Jordan's deployment process:
```bash
# Pre-flight check (no downtime required)
$ acode db migrate --dry-run
Checking migrations...
Pending: 2 migrations
  - 007_add_sync_status (estimated: 2 seconds)
  - 008_add_sync_indexes (estimated: 5 seconds)
  
No schema conflicts detected.
Disk space required: 12 MB (available: 45 GB) ✓

# Apply migrations (automatic rollback on failure)
$ acode db migrate
Applying 007_add_sync_status...
  ✓ Added sync_status column (1.8s)
Applying 008_add_sync_indexes...
  ✓ Created idx_conversations_sync (4.2s)

All migrations applied successfully.
Backup created: .agent/backups/pre_migrate_20240115_100000.db
```

**Metrics:**
- Deployment time: 5 hours → 10 minutes (97% reduction)
- Downtime: 2-5 hours → 0 (migrations run while app continues)
- Risk of data loss: High → Near-zero (automatic backup + rollback)
- Rollback time: 45 minutes → 30 seconds (automatic)
- **Annual savings per deployment: $5,000** (50 dev-hours × $100/hr)
- **Annual savings (10 deployments): $50,000**

---

### Use Case 3: Alex - Cross-Device Development with Team Sync

**Persona:** Alex, a mobile developer, works on an AI-assisted coding tool from multiple locations: office desktop, home laptop, and occasionally a coffee shop tablet. Alex's team of 8 developers shares context about ongoing projects.

**Before Task 050 (SQLite-Only, No Sync):**
Alex's workflow pain points:
- Morning at office: Start conversation about refactoring AuthService
- Afternoon at home: Can't continue—conversation only on office machine
- Workaround: Copy-paste chat history into Slack (loses context, formatting, tool calls)
- Team collaboration: No shared conversations, everyone duplicates research

Alex spends 2 hours/week recreating context that was lost between devices.

**After Task 050 (PostgreSQL Team Sync):**
Alex configures remote database:
```yaml
# .agent/config.yml
database:
  remote:
    enabled: true
    connection_string: ${TEAM_PG_CONNECTION}  # From 1Password/environment
```

Alex's new workflow:
- Morning at office: Start conversation about AuthService refactoring
- Afternoon at home: Continue same conversation seamlessly
  ```bash
  $ acode chat list
  ID            Title                    Last Updated
  01HXYZ123     Refactoring AuthService  2 hours ago
  
  $ acode chat open 01HXYZ123
  Continuing conversation from office desktop...
  ```
- Team collaboration: Share conversation IDs, teammates can view/continue

**Metrics:**
- Context recreation time: 2 hours/week → 0 (sync handles it)
- Cross-device friction: High → None
- Team knowledge sharing: Manual copy-paste → Automatic sync
- **Annual savings per developer: $10,400** (2 hours/week × 52 weeks × $100/hr)
- **Team annual savings (8 developers): $83,200**

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Workspace DB | Per-workspace database file containing all persistent state for a single agent workspace |
| SQLite | Embedded relational database engine that stores data in a single file with zero configuration |
| PostgreSQL | Enterprise-grade open-source relational database server for team/production deployments |
| Migration | A versioned, reversible schema change that transforms database structure from version N to N+1 |
| WAL | Write-Ahead Logging - SQLite journaling mode enabling concurrent reads during writes |
| Connection Pool | Pre-established reusable database connections maintained to avoid connection overhead |
| Transaction | Atomic unit of work grouping multiple operations that either all succeed or all fail |
| Repository | Data access pattern abstracting persistence details from business logic |
| Schema | Database structure definition including tables, columns, indexes, and constraints |
| Rollback | Reverting a migration to restore the previous schema version |
| Busy Timeout | SQLite wait time when database is locked by another process |
| Connection String | Database connection parameters encoded as URI or key-value pairs |
| Health Check | Diagnostic probe verifying database accessibility and responsiveness |
| Constraint | Database rule enforcing data integrity (NOT NULL, UNIQUE, CHECK, FOREIGN KEY) |
| Circuit Breaker | Failure protection pattern that stops requests to failing services |
| ACID | Atomicity, Consistency, Isolation, Durability - transaction guarantees |
| Idempotent | Operation that produces same result whether applied once or multiple times |
| Checksum | Hash value verifying migration file integrity hasn't changed |
| sys_migrations | System table tracking which migrations have been applied |
| Graceful Degradation | Maintaining functionality with reduced capability when components fail |

---

## Out of Scope

The following items are explicitly excluded from Task 050:

- **Specific schemas** - Other tasks define their schemas
- **Sync logic** - Task 049.f
- **Conversation data** - Task 049.a
- **Session data** - Task 011.b
- **Distributed transactions** - Not supported
- **Sharding** - Single database
- **Read replicas** - Not in scope
- **Database encryption** - OS-level
- **Query optimization** - Per-feature
- **ORM frameworks** - Raw ADO.NET

---

## Assumptions

### Technical Assumptions

1. **SQLite 3.35+** - The runtime SQLite version supports WAL mode, JSON functions, and window functions required for efficient storage and querying
2. **PostgreSQL 13+** - For production deployments, PostgreSQL 13 or later is available with required extensions (pg_stat_statements, pgcrypto)
3. **File System Access** - The .agent/data/ directory has write permissions and sufficient disk space for database files
4. **Single Process Access** - Only one agent process writes to SQLite at a time; concurrent reads are allowed
5. **Transaction Support** - Both database engines support ACID transactions with proper isolation levels
6. **Connection Pooling** - PostgreSQL connections benefit from pooling; SQLite uses single-connection model
7. **UTF-8 Encoding** - All text data is stored and retrieved as UTF-8 encoded strings
8. **Timestamp Handling** - DateTime values are stored as UTC ISO8601 strings for cross-platform compatibility

### Architectural Assumptions

9. **Repository Pattern** - Data access follows repository pattern with interface abstractions for testing
10. **Dependency Injection** - Database services are registered in DI container and resolved at runtime
11. **Configuration Binding** - Connection strings and settings are read from agent-config.yml
12. **Migration-First Schema** - All schema changes go through versioned migrations; no ad-hoc ALTER TABLE
13. **No ORM** - Raw ADO.NET/Dapper is used; no Entity Framework or other ORM dependencies
14. **Provider Abstraction** - IDatabaseProvider interface allows swapping SQLite/PostgreSQL implementations

### Operational Assumptions

15. **Local Development** - Developers use SQLite for local testing; PostgreSQL for integration tests
16. **Backup Responsibility** - Backup/restore is handled separately in Task 050.e; this task provides hooks
17. **No Auto-Migration** - Migrations require explicit CLI commands or startup configuration
18. **Graceful Degradation** - Database unavailability surfaces clear errors; no silent failures

---

## Functional Requirements

### SQLite Database

- FR-001: Database file MUST be created in `.agent/data/` directory
- FR-002: Database filename MUST be `workspace.db`
- FR-003: WAL (Write-Ahead Logging) mode MUST be enabled on connection
- FR-004: Busy timeout MUST be configurable via configuration
- FR-005: Default busy timeout MUST be 5000 milliseconds
- FR-006: Database file MUST be created with 0600 permissions (owner read/write only)
- FR-007: Parent directory `.agent/data/` MUST be created if not exists
- FR-008: SQLite version MUST be validated as 3.35+ on startup
- FR-009: Database MUST support concurrent read operations while write in progress
- FR-010: Journal files (`.db-wal`, `.db-shm`) MUST be in same directory as main file

### PostgreSQL Connection

- FR-011: Connection string MUST be accepted from configuration
- FR-012: Connection string MUST support `${ENV_VAR}` variable substitution
- FR-013: Individual connection parameters (host, port, database, user, password) MUST be configurable
- FR-014: SSL/TLS MUST be enabled by default for PostgreSQL connections
- FR-015: Connection pool minimum size MUST be configurable (default: 2)
- FR-016: Connection pool maximum size MUST be configurable (default: 10)
- FR-017: Connection idle timeout MUST be configurable (default: 300 seconds)
- FR-018: Connection lifetime MUST be configurable (default: 3600 seconds)
- FR-019: PostgreSQL version MUST be validated as 13+ on first connection
- FR-020: Connection MUST retry on transient failures with exponential backoff

### Provider Selection

- FR-021: Provider (SQLite/PostgreSQL) MUST be selectable via configuration
- FR-022: SQLite MUST be default when no remote configuration present
- FR-023: PostgreSQL MUST be used when `remote.enabled = true` and network available
- FR-024: System MUST fall back to SQLite when PostgreSQL unavailable (if configured)
- FR-025: Provider switch MUST NOT require application restart

### Migration Framework

- FR-026: Migrations MUST be versioned with sequential numbers
- FR-027: Migration filename format MUST be `NNN_descriptive_name.sql`
- FR-028: Each migration MUST have a corresponding rollback file `NNN_descriptive_name_down.sql`
- FR-029: Migration content MUST be idempotent (safe to run multiple times)
- FR-030: `sys_migrations` table MUST be created automatically if not exists
- FR-031: Applied migration versions MUST be tracked with timestamps
- FR-032: Migration checksum (SHA-256) MUST be stored for integrity verification
- FR-033: Migration files MUST be embedded as assembly resources
- FR-034: Migration runner MUST discover all embedded migrations automatically

### Migration Execution

- FR-035: Migrations MUST run automatically on application startup (if enabled)
- FR-036: Manual migration MUST be available via `acode db migrate` command
- FR-037: Forward migration MUST apply all pending migrations in sequence
- FR-038: Rollback migration MUST revert single migration at a time
- FR-039: Dry-run mode MUST show what would be applied without applying
- FR-040: Migration progress MUST be logged with estimated completion time
- FR-041: Migration MUST acquire exclusive lock before execution
- FR-042: Concurrent migration attempts MUST be blocked

### Migration Safety

- FR-043: Failed migration MUST automatically rollback changes within that migration
- FR-044: Partial migration application MUST NOT be possible (atomic)
- FR-045: Optional backup MUST be created before migration (configurable)
- FR-046: Migration lock MUST prevent concurrent migration execution
- FR-047: Checksum validation MUST fail if migration file changed after application
- FR-048: Migration timeout MUST be configurable (default: 5 minutes per migration)
- FR-049: Migration MUST log detailed error message on failure

### Connection Factory

- FR-050: IConnectionFactory interface MUST abstract SQLite/PostgreSQL differences
- FR-051: Connection MUST be created asynchronously with CancellationToken
- FR-052: Connection MUST be returned in open state
- FR-053: Connection MUST implement IAsyncDisposable for proper cleanup
- FR-054: Connection creation failure MUST throw specific exception types

### Connection Lifecycle

- FR-055: PostgreSQL connections MUST be pooled and reused
- FR-056: SQLite connections MUST be created per-request (single file)
- FR-057: Connection MUST be disposed after use via `using` pattern
- FR-058: Connection timeout MUST be configurable (default: 30 seconds)
- FR-059: Command timeout MUST be configurable (default: 60 seconds)
- FR-060: Stale connections MUST be detected and removed from pool

### Transactions

- FR-061: Transaction scope MUST be supported via ITransaction interface
- FR-062: Transaction MUST require explicit commit to persist changes
- FR-063: Transaction rollback MUST be available explicitly or on dispose
- FR-064: Nested transactions MUST throw exception (not supported)
- FR-065: Transaction isolation level MUST be configurable
- FR-066: Read Committed MUST be default isolation level
- FR-067: Transaction timeout MUST be configurable

### Error Handling

- FR-068: Connection errors MUST retry with exponential backoff (100ms, 200ms, 400ms)
- FR-069: Maximum retry count MUST be configurable (default: 3)
- FR-070: Constraint violation errors MUST throw specific exception with constraint name
- FR-071: Timeout errors MUST retry once before failing
- FR-072: All database errors MUST be logged with correlation ID
- FR-073: Errors MUST include provider type (SQLite/PostgreSQL) in message
- FR-074: Database-specific errors MUST be translated to provider-agnostic exceptions

### Circuit Breaker

- FR-075: Circuit breaker MUST engage after 5 consecutive failures
- FR-076: Open circuit MUST fast-fail requests for 30 seconds
- FR-077: Half-open state MUST allow single probe request
- FR-078: Successful probe MUST close circuit
- FR-079: Circuit state MUST be logged on transitions

### Health Checks

- FR-080: SQLite health check MUST verify file exists and is readable
- FR-081: SQLite health check MUST verify database can be opened
- FR-082: PostgreSQL health check MUST verify connection can be established
- FR-083: PostgreSQL health check MUST execute simple query (`SELECT 1`)
- FR-084: Health check MUST return detailed status object
- FR-085: Health check MUST complete within configurable timeout (default: 5 seconds)
- FR-086: Unhealthy status MUST log with details
- FR-087: Health check MUST include connection pool statistics for PostgreSQL

### CLI Commands

- FR-088: `acode db status` MUST show database path, size, health status
- FR-089: `acode db status` MUST show migration status (applied/pending counts)
- FR-090: `acode db status` MUST show connection pool status for PostgreSQL
- FR-091: `acode db migrate` MUST apply all pending migrations
- FR-092: `acode db migrate --dry-run` MUST show what would be applied
- FR-093: `acode db migrate --to VERSION` MUST migrate to specific version
- FR-094: `acode db rollback` MUST rollback most recent migration
- FR-095: `acode db rollback --to VERSION` MUST rollback to specific version
- FR-096: `acode db schema` MUST display current schema definition
- FR-097: `acode db schema --table NAME` MUST display specific table schema
- FR-098: `acode db backup` MUST create timestamped backup file
- FR-099: `acode db backup --output PATH` MUST allow custom backup path
- FR-100: `acode db verify` MUST check database integrity

### Configuration

- FR-101: Configuration MUST support `database.local.path` for SQLite location
- FR-102: Configuration MUST support `database.local.busy_timeout_ms` setting
- FR-103: Configuration MUST support `database.remote.enabled` boolean
- FR-104: Configuration MUST support `database.remote.connection_string`
- FR-105: Configuration MUST support `database.remote.pool.min_size`
- FR-106: Configuration MUST support `database.remote.pool.max_size`
- FR-107: Configuration MUST support `database.remote.timeouts.connect_seconds`
- FR-108: Configuration MUST support `database.remote.timeouts.command_seconds`
- FR-109: Configuration MUST validate settings on load
- FR-110: Invalid configuration MUST throw descriptive error message

---

## Non-Functional Requirements

### Performance

- NFR-001: SQLite connection acquire MUST complete in < 5ms
- NFR-002: PostgreSQL connection acquire from pool MUST complete in < 10ms
- NFR-003: PostgreSQL new connection establish MUST complete in < 200ms
- NFR-004: Simple SELECT query (by primary key) MUST complete in < 5ms
- NFR-005: Simple INSERT query MUST complete in < 10ms
- NFR-006: Transaction commit MUST complete in < 20ms
- NFR-007: Migration step (average) MUST complete in < 5 seconds
- NFR-008: Migration step (maximum) MUST complete in < 30 seconds
- NFR-009: Health check MUST complete in < 50ms
- NFR-010: Connection pool warm-up MUST complete in < 2 seconds

### Reliability

- NFR-011: System MUST guarantee ACID transaction properties
- NFR-012: SQLite WAL mode MUST provide crash-safe durability
- NFR-013: No committed data MUST be lost on application crash
- NFR-014: No committed data MUST be lost on power failure (with WAL checkpoint)
- NFR-015: Migration rollback MUST restore exact previous schema state
- NFR-016: Circuit breaker MUST prevent cascade failures to database
- NFR-017: Connection pool MUST handle connection failures gracefully
- NFR-018: System MUST recover automatically from transient network failures

### Security

- NFR-019: Connection strings MUST support environment variable substitution for secrets
- NFR-020: Passwords MUST NOT appear in log files
- NFR-021: Connection strings MUST NOT appear in error messages shown to users
- NFR-022: Database file permissions MUST be restricted to owner (0600)
- NFR-023: PostgreSQL connections MUST use SSL/TLS encryption
- NFR-024: SQL queries MUST use parameterized statements (no string concatenation)
- NFR-025: Secrets in memory MUST be cleared after connection established

### Scalability

- NFR-026: System MUST handle 1000+ concurrent read operations (SQLite WAL)
- NFR-027: PostgreSQL pool MUST scale to 50 connections when configured
- NFR-028: System MUST handle 10,000 rows per table without degradation
- NFR-029: Migration framework MUST handle 100+ sequential migrations

### Compatibility

- NFR-030: System MUST support SQLite version 3.35 and later
- NFR-031: System MUST support PostgreSQL version 13 and later
- NFR-032: System MUST run on .NET 8.0 runtime
- NFR-033: System MUST run on Windows 10/11, macOS 12+, Ubuntu 20.04+
- NFR-034: SQLite database file MUST be portable across supported platforms

### Maintainability

- NFR-035: Migration files MUST follow consistent naming convention (NNN_description)
- NFR-036: Schema changes MUST be documented in migration comments
- NFR-037: Migration history MUST be queryable via sys_migrations table
- NFR-038: Connection factory MUST be swappable via dependency injection
- NFR-039: All database operations MUST be testable with in-memory database

### Observability

- NFR-040: All database operations MUST log with correlation IDs
- NFR-041: Connection pool metrics MUST be exposed (active, idle, waiting)
- NFR-042: Query duration MUST be logged for queries > 100ms
- NFR-043: Health check status MUST be available via CLI and API
- NFR-044: Migration events MUST be logged with timestamps and durations

### Usability

- NFR-045: Database CLI commands MUST provide helpful error messages
- NFR-046: Migration status MUST be human-readable in CLI output
- NFR-047: Configuration validation MUST provide specific error details

---

## User Manual Documentation

### Overview

The workspace database provides persistent storage for all Acode operations. SQLite handles local storage with zero configuration; PostgreSQL enables team sync and cross-device access. This manual covers configuration, CLI commands, troubleshooting, and best practices.

### Database Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Acode Application                             │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │              IConnectionFactory (injected)                       │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│              │                                │                         │
│              ▼                                ▼                         │
│  ┌────────────────────────┐       ┌────────────────────────┐           │
│  │ SqliteConnectionFactory│       │PostgresConnectionFactory│           │
│  │                        │       │                        │           │
│  │  • WAL mode enabled    │       │  • Connection pooling  │           │
│  │  • Busy timeout 5s     │       │  • SSL/TLS encryption  │           │
│  │  • Single file         │       │  • Retry with backoff  │           │
│  └───────────┬────────────┘       └───────────┬────────────┘           │
│              │                                │                         │
└──────────────┼────────────────────────────────┼─────────────────────────┘
               │                                │
               ▼                                ▼
   ┌────────────────────────┐       ┌────────────────────────┐
   │  .agent/data/          │       │   PostgreSQL Server    │
   │    workspace.db        │       │   (remote)             │
   │    workspace.db-wal    │       │                        │
   │    workspace.db-shm    │       │   • Durability         │
   │                        │       │   • Team access        │
   │  Local (offline-first) │       │   • Cross-device sync  │
   └────────────────────────┘       └────────────────────────┘
```

### Database Location

```
.agent/
├── config.yml            # Database configuration
├── data/
│   ├── workspace.db      # Main SQLite database file
│   ├── workspace.db-wal  # Write-ahead log (concurrent access)
│   └── workspace.db-shm  # Shared memory file (coordination)
└── backups/
    ├── workspace_2024-01-15_100000.db
    └── workspace_2024-01-14_180000.db
```

### Configuration Reference

```yaml
# .agent/config.yml - Complete database configuration

database:
  # ═══════════════════════════════════════════════════════════════════
  # LOCAL SQLITE SETTINGS
  # ═══════════════════════════════════════════════════════════════════
  local:
    # Path to SQLite database file (relative to workspace root)
    path: .agent/data/workspace.db
    
    # Enable Write-Ahead Logging for concurrent read/write
    # HIGHLY RECOMMENDED: Leave this enabled
    wal_mode: true
    
    # How long to wait when database is locked (milliseconds)
    # Higher values reduce "database is locked" errors
    busy_timeout_ms: 5000
    
  # ═══════════════════════════════════════════════════════════════════
  # REMOTE POSTGRESQL SETTINGS
  # ═══════════════════════════════════════════════════════════════════
  remote:
    # Enable remote database (for team sync/cross-device)
    enabled: false
    
    # Option 1: Full connection string (supports env var substitution)
    connection_string: ${ACODE_PG_CONNECTION}
    
    # Option 2: Individual components (use if connection_string not set)
    # host: db.example.com
    # port: 5432
    # database: acode
    # username: ${ACODE_PG_USER}
    # password: ${ACODE_PG_PASSWORD}
    # ssl_mode: require  # require | prefer | disable
    
    # Connection pool settings
    pool:
      min_size: 2       # Minimum connections to keep warm
      max_size: 10      # Maximum concurrent connections
      idle_timeout: 300 # Close idle connections after N seconds
      lifetime: 3600    # Recycle connections after N seconds
      
    # Timeout settings
    timeouts:
      connect_seconds: 5   # Timeout for establishing connection
      command_seconds: 30  # Timeout for query execution
      
  # ═══════════════════════════════════════════════════════════════════
  # MIGRATION SETTINGS
  # ═══════════════════════════════════════════════════════════════════
  migrations:
    # Run pending migrations automatically on startup
    auto_migrate: true
    
    # Create backup before running migrations
    backup_before_migrate: true
    
    # Maximum time for a single migration (seconds)
    timeout_seconds: 300
```

### CLI Commands Reference

#### `acode db status`

Shows current database status, health, and migration information.

```bash
$ acode db status

Database Status
════════════════════════════════════════════════════════════════════════

LOCAL DATABASE (SQLite)
────────────────────────────────────────────────────────────────────────
  Path:           .agent/data/workspace.db
  Size:           4.2 MB
  WAL Size:       128 KB
  Status:         ✓ Healthy
  SQLite Version: 3.40.1
  Journal Mode:   WAL
  Busy Timeout:   5000ms

REMOTE DATABASE (PostgreSQL)
────────────────────────────────────────────────────────────────────────
  Status:         ✓ Connected
  Host:           db.example.com:5432
  Database:       acode
  Latency:        23ms
  PostgreSQL:     15.2
  
  Connection Pool:
    Active:       3
    Idle:         2
    Waiting:      0
    Max:          10

MIGRATIONS
────────────────────────────────────────────────────────────────────────
  Applied:        5
  Pending:        0
  Latest:         005_add_approval_records (applied 2024-01-15 10:30:00)
  
  Migration History:
    001_initial_schema          2024-01-01 09:00:00
    002_add_chats               2024-01-02 14:30:00
    003_add_messages            2024-01-05 11:15:00
    004_add_sync_status         2024-01-10 16:45:00
    005_add_approval_records    2024-01-15 10:30:00
```

#### `acode db migrate`

Applies pending database migrations.

```bash
# Apply all pending migrations
$ acode db migrate

Checking migrations...
────────────────────────────────────────────────────────────────────────
Applied: 3 migrations
Pending: 2 migrations

Creating pre-migration backup...
  ✓ Backup saved: .agent/backups/pre_migrate_20240115_100000.db

Applying 004_add_sync_status...
  → Adding sync_status column to chats
  → Adding sync_status column to messages
  → Creating index idx_chats_sync_status
  ✓ Completed in 1.8 seconds
  
Applying 005_add_approval_records...
  → Creating approval_records table
  → Creating approval_decisions table
  → Adding foreign key constraints
  → Creating indexes
  ✓ Completed in 2.4 seconds

════════════════════════════════════════════════════════════════════════
All migrations applied successfully.
Total time: 4.2 seconds

# Preview migrations without applying (dry-run)
$ acode db migrate --dry-run

Dry Run - No changes will be made
────────────────────────────────────────────────────────────────────────
Would apply 2 migrations:
  1. 004_add_sync_status
     - ADD COLUMN chats.sync_status TEXT DEFAULT 'pending'
     - ADD COLUMN messages.sync_status TEXT DEFAULT 'pending'
     - CREATE INDEX idx_chats_sync_status
     
  2. 005_add_approval_records
     - CREATE TABLE approval_records (...)
     - CREATE TABLE approval_decisions (...)
     
Estimated time: ~5 seconds

# Migrate to specific version
$ acode db migrate --to 004

Migrating to version 004...
```

#### `acode db rollback`

Reverts the most recent migration or rolls back to a specific version.

```bash
# Rollback last migration
$ acode db rollback

Rolling back 005_add_approval_records...
────────────────────────────────────────────────────────────────────────
  → Dropping approval_decisions table
  → Dropping approval_records table
  ✓ Rollback completed in 0.8 seconds

Current version: 004_add_sync_status

# Rollback to specific version
$ acode db rollback --to 002

This will rollback 3 migrations:
  - 005_add_approval_records
  - 004_add_sync_status
  - 003_add_messages
  
Are you sure? [y/N]: y

Rolling back migrations...
  ✓ 005_add_approval_records (0.8s)
  ✓ 004_add_sync_status (0.5s)
  ✓ 003_add_messages (1.2s)

Current version: 002_add_chats
```

#### `acode db schema`

Displays the current database schema.

```bash
# Show all tables
$ acode db schema

Database Schema
════════════════════════════════════════════════════════════════════════

TABLE: chats (7 columns)
────────────────────────────────────────────────────────────────────────
  Column       Type        Nullable  Default       Notes
  ──────────   ─────────   ────────  ────────────  ─────────────────────
  id           TEXT        NO        -             PRIMARY KEY (ULID)
  title        TEXT        NO        -             Chat title
  tags         TEXT        YES       NULL          JSON array of tags
  worktree_id  TEXT        YES       NULL          FK to worktrees
  is_deleted   INTEGER     NO        0             Soft delete flag
  created_at   TEXT        NO        -             ISO8601 UTC timestamp
  updated_at   TEXT        NO        -             ISO8601 UTC timestamp
  
  Indexes:
    idx_chats_worktree     (worktree_id)
    idx_chats_created      (created_at DESC)
    idx_chats_deleted      (is_deleted) WHERE is_deleted = 0

TABLE: messages (8 columns)
────────────────────────────────────────────────────────────────────────
  Column       Type        Nullable  Default       Notes
  ...

# Show specific table
$ acode db schema --table chats
```

#### `acode db backup`

Creates a backup of the local SQLite database.

```bash
# Create backup with default naming
$ acode db backup

Creating backup...
────────────────────────────────────────────────────────────────────────
Source:     .agent/data/workspace.db
Backup:     .agent/backups/workspace_2024-01-15_100000.db
Size:       4.2 MB
Checksum:   sha256:a1b2c3d4...
Duration:   0.3 seconds

✓ Backup created successfully

# Create backup to specific location
$ acode db backup --output /path/to/backup.db

# Create backup with compression
$ acode db backup --compress

Backup saved: .agent/backups/workspace_2024-01-15_100000.db.gz
Compressed:   4.2 MB → 1.1 MB (74% reduction)
```

#### `acode db verify`

Verifies database integrity.

```bash
$ acode db verify

Database Integrity Check
════════════════════════════════════════════════════════════════════════

SQLite Integrity:
  ✓ File exists and readable
  ✓ Database opens successfully
  ✓ PRAGMA integrity_check passed
  ✓ Foreign key constraints valid
  ✓ All indexes valid

Migration Integrity:
  ✓ sys_migrations table exists
  ✓ All migration checksums match
  ✓ No gaps in migration sequence
  
Schema Integrity:
  ✓ All expected tables exist
  ✓ All expected columns present
  ✓ All expected indexes present

════════════════════════════════════════════════════════════════════════
All checks passed. Database is healthy.
```

### Migration File Format

```sql
-- migrations/006_add_sync_queue.sql
-- Description: Add outbox table for sync operations
-- Author: DevBot
-- Date: 2024-01-20

-- Create outbox table for change tracking
CREATE TABLE IF NOT EXISTS sync_outbox (
    id TEXT PRIMARY KEY,
    entity_type TEXT NOT NULL,
    entity_id TEXT NOT NULL,
    operation TEXT NOT NULL CHECK (operation IN ('INSERT', 'UPDATE', 'DELETE')),
    payload TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'processing', 'completed', 'failed')),
    retry_count INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    processed_at TEXT
);

-- Index for efficient polling
CREATE INDEX IF NOT EXISTS idx_outbox_status_created 
ON sync_outbox(status, created_at) 
WHERE status IN ('pending', 'processing');
```

```sql
-- migrations/006_add_sync_queue_down.sql
-- Rollback: Remove outbox table

DROP INDEX IF EXISTS idx_outbox_status_created;
DROP TABLE IF EXISTS sync_outbox;
```

### Troubleshooting Guide

#### Database Locked (SQLite)

**Symptoms:**
- Error: "database is locked"
- Operations hang then timeout
- Concurrent operations fail

**Causes:**
1. Another Acode process is writing
2. Long-running transaction holding lock
3. Crashed process left lock file

**Solutions:**
```bash
# Check for other agent processes
$ Get-Process | Where-Object { $_.Name -match 'acode' }

# Look for stale lock files
$ ls .agent/data/workspace.db*

# If process crashed, WAL files may need cleanup
# (Only if no process is running!)
$ rm .agent/data/workspace.db-wal
$ rm .agent/data/workspace.db-shm

# Increase busy timeout in config
database:
  local:
    busy_timeout_ms: 30000  # 30 seconds
```

#### PostgreSQL Connection Failed

**Symptoms:**
- Error: "could not connect to server"
- Remote status shows "Disconnected"
- Timeout on remote operations

**Solutions:**
```bash
# Test network connectivity
$ Test-NetConnection -ComputerName db.example.com -Port 5432

# Verify connection string
$ acode db status --verbose

# Check PostgreSQL logs on server
$ sudo tail -f /var/log/postgresql/postgresql-15-main.log

# Test with psql directly
$ psql -h db.example.com -U acode_user -d acode -c "SELECT 1"
```

#### Migration Failed

**Symptoms:**
- Migration halts with error
- Schema in unexpected state
- Application won't start

**Solutions:**
```bash
# Check current migration state
$ acode db status

# View detailed migration error
$ acode db migrate --verbose

# If auto-rollback failed, manual intervention:
$ acode db rollback --force

# Restore from backup if needed
$ cp .agent/backups/pre_migrate_*.db .agent/data/workspace.db
```

### FAQ

**Q: Can I use PostgreSQL without SQLite?**
A: No, SQLite is always used for local operations. PostgreSQL is optional for sync.

**Q: How do I reset the database completely?**
A: Delete `.agent/data/workspace.db*` files and restart. A fresh database will be created.

**Q: Are migrations reversible?**
A: Yes, every migration must have a corresponding `_down.sql` rollback file.

**Q: What happens if PostgreSQL is unavailable?**
A: Acode continues working with local SQLite. Changes sync when PostgreSQL is available.

**Q: How often should I backup?**
A: Automatic backups are created before migrations. Manual backups depend on data importance.

---

#### Database Corrupted

**Problem:** SQLite file corrupted

**Solutions:**
1. Restore from backup
2. Run `sqlite3 workspace.db ".recover"`
3. Rebuild from sync

---

## Acceptance Criteria

### SQLite Connection Factory

- [ ] AC-001: Database file is created in `.agent/data/` directory on first connection
- [ ] AC-002: Database filename is `workspace.db`
- [ ] AC-003: WAL mode is enabled automatically on connection
- [ ] AC-004: Busy timeout is configurable via configuration (default 5000ms)
- [ ] AC-005: Concurrent read operations work while write in progress
- [ ] AC-006: Database file permissions are restricted to owner only (0600)
- [ ] AC-007: Parent directory `.agent/data/` is created if not exists
- [ ] AC-008: SQLite version is validated as 3.35+ on startup
- [ ] AC-009: Connection creation fails gracefully with descriptive error if SQLite unavailable
- [ ] AC-010: Journal files (.db-wal, .db-shm) are created in same directory

### PostgreSQL Connection Factory

- [ ] AC-011: Connection string from configuration works
- [ ] AC-012: Environment variable substitution in connection string works
- [ ] AC-013: Individual connection parameters (host, port, etc.) work
- [ ] AC-014: SSL/TLS is enabled by default
- [ ] AC-015: Connection pool minimum size is configurable (default 2)
- [ ] AC-016: Connection pool maximum size is configurable (default 10)
- [ ] AC-017: Idle connections are closed after configured timeout (default 300s)
- [ ] AC-018: Connections are recycled after configured lifetime (default 3600s)
- [ ] AC-019: PostgreSQL version is validated as 13+ on first connection
- [ ] AC-020: Transient connection failures trigger retry with exponential backoff

### Provider Selection

- [ ] AC-021: SQLite is used by default when no remote configuration
- [ ] AC-022: PostgreSQL is used when remote.enabled = true
- [ ] AC-023: System falls back to SQLite when PostgreSQL unavailable (if configured)
- [ ] AC-024: Provider switch does not require application restart

### Migration Framework

- [ ] AC-025: Migrations are discovered from embedded assembly resources
- [ ] AC-026: Migration filename format NNN_descriptive_name.sql is enforced
- [ ] AC-027: Each migration has corresponding rollback file
- [ ] AC-028: sys_migrations table is created automatically
- [ ] AC-029: Applied migration versions are tracked with timestamps
- [ ] AC-030: Migration checksum (SHA-256) is stored and validated
- [ ] AC-031: Checksum mismatch triggers critical security alert

### Migration Execution

- [ ] AC-032: Migrations run automatically on startup when enabled
- [ ] AC-033: `acode db migrate` applies all pending migrations
- [ ] AC-034: `acode db migrate --dry-run` shows what would be applied
- [ ] AC-035: `acode db migrate --to VERSION` migrates to specific version
- [ ] AC-036: Migration progress is logged with estimated completion time
- [ ] AC-037: Concurrent migration attempts are blocked
- [ ] AC-038: Migration timeout is configurable (default 5 minutes)

### Migration Safety

- [ ] AC-039: Failed migration automatically rolls back changes
- [ ] AC-040: Partial migration application is impossible (atomic)
- [ ] AC-041: Optional backup is created before migration
- [ ] AC-042: Migration lock prevents concurrent execution
- [ ] AC-043: Detailed error message is logged on failure
- [ ] AC-044: Rollback migration (`_down.sql`) is applied correctly

### Connection Lifecycle

- [ ] AC-045: Connections are created asynchronously with CancellationToken
- [ ] AC-046: Connections are returned in open state
- [ ] AC-047: Connections implement IAsyncDisposable
- [ ] AC-048: Connection timeout is configurable (default 30s)
- [ ] AC-049: Command timeout is configurable (default 60s)
- [ ] AC-050: Stale connections are detected and removed from pool

### Transactions

- [ ] AC-051: Transaction scope is supported via ITransaction interface
- [ ] AC-052: Transaction requires explicit commit to persist
- [ ] AC-053: Transaction rollback works explicitly and on dispose
- [ ] AC-054: Nested transactions throw exception
- [ ] AC-055: Transaction isolation level is configurable
- [ ] AC-056: Read Committed is default isolation level

### Error Handling

- [ ] AC-057: Connection errors retry with exponential backoff (100ms, 200ms, 400ms)
- [ ] AC-058: Maximum retry count is configurable (default 3)
- [ ] AC-059: Constraint violation throws specific exception with constraint name
- [ ] AC-060: Timeout errors retry once before failing
- [ ] AC-061: All database errors are logged with correlation ID
- [ ] AC-062: Errors include provider type in message

### Circuit Breaker

- [ ] AC-063: Circuit breaker engages after 5 consecutive failures
- [ ] AC-064: Open circuit fast-fails requests for 30 seconds
- [ ] AC-065: Half-open state allows single probe request
- [ ] AC-066: Successful probe closes circuit
- [ ] AC-067: Circuit state transitions are logged

### Health Checks

- [ ] AC-068: SQLite health check verifies file exists and readable
- [ ] AC-069: SQLite health check verifies database can be opened
- [ ] AC-070: PostgreSQL health check verifies connection
- [ ] AC-071: PostgreSQL health check executes `SELECT 1`
- [ ] AC-072: Health check returns detailed status object
- [ ] AC-073: Health check completes within 5 seconds
- [ ] AC-074: Unhealthy status is logged with details
- [ ] AC-075: PostgreSQL health check includes pool statistics

### CLI - Status Command

- [ ] AC-076: `acode db status` shows database path and size
- [ ] AC-077: `acode db status` shows health status
- [ ] AC-078: `acode db status` shows migration status (applied/pending)
- [ ] AC-079: `acode db status` shows connection pool status for PostgreSQL
- [ ] AC-080: `acode db status --verbose` shows detailed information

### CLI - Migrate Command

- [ ] AC-081: `acode db migrate` applies all pending migrations
- [ ] AC-082: `acode db migrate --dry-run` shows preview
- [ ] AC-083: `acode db migrate --to VERSION` migrates to specific version
- [ ] AC-084: Migration output shows progress and timing

### CLI - Rollback Command

- [ ] AC-085: `acode db rollback` reverts most recent migration
- [ ] AC-086: `acode db rollback --to VERSION` reverts to specific version
- [ ] AC-087: Rollback prompts for confirmation

### CLI - Other Commands

- [ ] AC-088: `acode db schema` displays current schema
- [ ] AC-089: `acode db schema --table NAME` displays specific table
- [ ] AC-090: `acode db backup` creates timestamped backup
- [ ] AC-091: `acode db backup --output PATH` allows custom path
- [ ] AC-092: `acode db verify` checks database integrity

### Configuration

- [ ] AC-093: Configuration validates settings on load
- [ ] AC-094: Invalid configuration throws descriptive error
- [ ] AC-095: Configuration supports hot-reload for non-connection settings
- [ ] AC-096: Default values are applied for optional settings

### Security

- [ ] AC-097: Connection strings are redacted in logs
- [ ] AC-098: Passwords never appear in error messages
- [ ] AC-099: Database file permissions are set securely
- [ ] AC-100: Migration content is validated for dangerous patterns
- [ ] AC-101: Migration checksums are verified before execution

### Performance

- [ ] AC-102: SQLite connection acquire < 5ms
- [ ] AC-103: PostgreSQL connection acquire (pool hit) < 10ms
- [ ] AC-104: Simple SELECT by PK < 5ms
- [ ] AC-105: Transaction commit < 20ms
- [ ] AC-106: Health check < 50ms

---

## Security Considerations

### Threat 1: Connection String Exposure in Logs

**Risk:** PostgreSQL connection strings may contain passwords that could be logged, exposing credentials in log files, error reports, or monitoring systems.

**Attack Scenario:** An attacker gains read access to log files (via misconfigured permissions or log aggregation service compromise) and extracts database credentials from connection errors or debug logs.

**Mitigation Code:**

```csharp
// Infrastructure/Database/SafeConnectionStringLogger.cs
namespace AgenticCoder.Infrastructure.Database;

public sealed class SafeConnectionStringLogger
{
    private static readonly string[] SensitiveKeys = 
        { "password", "pwd", "secret", "key", "token" };
    
    public static string Redact(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString;
        
        var result = connectionString;
        
        // Handle key=value format (PostgreSQL, SQL Server)
        foreach (var key in SensitiveKeys)
        {
            // Match: password=secret123; or password='secret123'
            var pattern = $@"({key}\s*=\s*)([^;'""]+|'[^']*'|""[^""]*"")";
            result = Regex.Replace(
                result, 
                pattern, 
                $"$1***REDACTED***", 
                RegexOptions.IgnoreCase);
        }
        
        // Handle URL format: postgres://user:password@host/db
        result = Regex.Replace(
            result,
            @"://([^:]+):([^@]+)@",
            "://$1:***REDACTED***@",
            RegexOptions.IgnoreCase);
        
        return result;
    }
}

// Usage in connection factory
public sealed class PostgresConnectionFactory : IConnectionFactory
{
    private readonly ILogger<PostgresConnectionFactory> _logger;
    private readonly string _connectionString;
    
    public async Task<IDbConnection> CreateAsync(CancellationToken ct)
    {
        try
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            return connection;
        }
        catch (Exception ex)
        {
            // NEVER log the actual connection string
            _logger.LogError(ex, 
                "Failed to connect to PostgreSQL. Connection: {SafeConnection}",
                SafeConnectionStringLogger.Redact(_connectionString));
            throw;
        }
    }
}
```

---

### Threat 2: SQL Injection via Migration Files

**Risk:** Maliciously crafted migration files could contain SQL injection attacks that execute unauthorized commands during migration.

**Attack Scenario:** An attacker gains commit access to the repository and adds a migration file containing `DROP TABLE users; --` or data exfiltration queries.

**Mitigation Code:**

```csharp
// Infrastructure/Migrations/MigrationValidator.cs
namespace AgenticCoder.Infrastructure.Migrations;

public sealed class MigrationValidator
{
    private static readonly string[] DangerousPatterns = new[]
    {
        @"\bEXEC\s*\(",           // Dynamic SQL execution
        @"\bxp_\w+",              // Extended stored procedures
        @"\bINTO\s+OUTFILE\b",    // File writes
        @"\bLOAD_FILE\b",         // File reads
        @"--\s*\n.*\n",           // Comment-based injection
        @";\s*DROP\s+",           // Drop after semicolon
        @"GRANT\s+",              // Permission escalation
        @"CREATE\s+USER\b",       // User creation
        @"ALTER\s+USER\b",        // User modification
    };
    
    private static readonly string[] AllowedStatements = new[]
    {
        "CREATE TABLE",
        "CREATE INDEX",
        "CREATE UNIQUE INDEX",
        "ALTER TABLE",
        "DROP TABLE",
        "DROP INDEX",
        "INSERT INTO",
        "UPDATE",
        "DELETE FROM",
    };
    
    public ValidationResult Validate(string migrationContent)
    {
        var errors = new List<string>();
        
        // Check for dangerous patterns
        foreach (var pattern in DangerousPatterns)
        {
            if (Regex.IsMatch(migrationContent, pattern, RegexOptions.IgnoreCase))
            {
                errors.Add($"Dangerous pattern detected: {pattern}");
            }
        }
        
        // Verify only allowed statements (for stricter environments)
        var statements = migrationContent.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var statement in statements)
        {
            var trimmed = statement.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("--"))
                continue;
                
            var isAllowed = AllowedStatements.Any(allowed => 
                trimmed.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));
                
            if (!isAllowed)
            {
                errors.Add($"Unrecognized statement type: {trimmed.Substring(0, Math.Min(50, trimmed.Length))}...");
            }
        }
        
        return new ValidationResult(errors.Count == 0, errors);
    }
}

public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);
```

---

### Threat 3: Database File Permission Escalation

**Risk:** SQLite database file with overly permissive permissions could be read or modified by other users on the system, exposing sensitive conversation data.

**Attack Scenario:** On a shared development server, another user reads the workspace.db file to extract API keys, conversation history, or approval records.

**Mitigation Code:**

```csharp
// Infrastructure/Database/SecureDatabaseFileCreator.cs
namespace AgenticCoder.Infrastructure.Database;

public sealed class SecureDatabaseFileCreator
{
    private readonly ILogger<SecureDatabaseFileCreator> _logger;
    
    public void EnsureSecureDatabase(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath)!;
        
        // Create directory with restricted permissions
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            SetDirectoryPermissions(directory);
        }
        
        // Create database file if not exists
        if (!File.Exists(databasePath))
        {
            // Touch the file
            using var fs = File.Create(databasePath);
        }
        
        // Set file permissions
        SetFilePermissions(databasePath);
        
        // Also secure WAL and SHM files if they exist
        var walPath = databasePath + "-wal";
        var shmPath = databasePath + "-shm";
        
        if (File.Exists(walPath)) SetFilePermissions(walPath);
        if (File.Exists(shmPath)) SetFilePermissions(shmPath);
    }
    
    private void SetFilePermissions(string filePath)
    {
        if (OperatingSystem.IsWindows())
        {
            // Windows: Remove inheritance, grant only current user
            var fileInfo = new FileInfo(filePath);
            var security = fileInfo.GetAccessControl();
            
            // Remove inherited permissions
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            
            // Remove all existing rules
            var rules = security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            foreach (FileSystemAccessRule rule in rules)
            {
                security.RemoveAccessRule(rule);
            }
            
            // Add only current user with full control
            var currentUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            security.AddAccessRule(new FileSystemAccessRule(
                currentUser,
                FileSystemRights.FullControl,
                AccessControlType.Allow));
            
            fileInfo.SetAccessControl(security);
        }
        else
        {
            // Unix: chmod 600 (owner read/write only)
            var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            File.SetUnixFileMode(filePath, mode);
        }
        
        _logger.LogDebug("Set secure permissions on {FilePath}", filePath);
    }
    
    private void SetDirectoryPermissions(string directoryPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            // Unix: chmod 700 (owner only)
            var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
            Directory.SetUnixFileMode(directoryPath, mode);
        }
    }
}
```

---

### Threat 4: Connection Pool Exhaustion (DoS)

**Risk:** An attacker or buggy code could exhaust the PostgreSQL connection pool, causing denial of service for legitimate operations.

**Attack Scenario:** A malicious input triggers many concurrent database operations that acquire connections but don't release them promptly, exhausting the pool.

**Mitigation Code:**

```csharp
// Infrastructure/Database/ResilientConnectionFactory.cs
namespace AgenticCoder.Infrastructure.Database;

public sealed class ResilientConnectionFactory : IConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<ResilientConnectionFactory> _logger;
    private readonly SemaphoreSlim _connectionLimiter;
    private readonly TimeSpan _acquireTimeout = TimeSpan.FromSeconds(5);
    private int _waitingCount;
    
    public ResilientConnectionFactory(DatabaseOptions options, ILogger<ResilientConnectionFactory> logger)
    {
        _logger = logger;
        
        var builder = new NpgsqlDataSourceBuilder(options.Remote.ConnectionString);
        builder.ConnectionStringBuilder.MaxPoolSize = options.Remote.Pool.MaxSize;
        builder.ConnectionStringBuilder.MinPoolSize = options.Remote.Pool.MinSize;
        builder.ConnectionStringBuilder.ConnectionIdleLifetime = options.Remote.Pool.IdleTimeout;
        
        _dataSource = builder.Build();
        
        // Additional application-level limiting beyond pool
        _connectionLimiter = new SemaphoreSlim(
            initialCount: options.Remote.Pool.MaxSize,
            maxCount: options.Remote.Pool.MaxSize);
    }
    
    public async Task<IDbConnection> CreateAsync(CancellationToken ct)
    {
        // Track waiting threads for monitoring
        var waiting = Interlocked.Increment(ref _waitingCount);
        
        try
        {
            if (waiting > 50) // Alert threshold
            {
                _logger.LogWarning(
                    "High connection wait queue: {WaitingCount} threads waiting for connection",
                    waiting);
            }
            
            // Try to acquire with timeout
            var acquired = await _connectionLimiter.WaitAsync(_acquireTimeout, ct);
            
            if (!acquired)
            {
                _logger.LogError(
                    "Connection pool exhausted. Waiting: {WaitingCount}, Timeout: {Timeout}ms",
                    waiting, _acquireTimeout.TotalMilliseconds);
                
                throw new DatabaseConnectionException(
                    "ACODE-DB-008",
                    "Connection pool exhausted. Try again later or increase pool size.");
            }
            
            try
            {
                var connection = await _dataSource.OpenConnectionAsync(ct);
                return new PooledConnectionWrapper(connection, _connectionLimiter);
            }
            catch
            {
                // Release semaphore if connection creation fails
                _connectionLimiter.Release();
                throw;
            }
        }
        finally
        {
            Interlocked.Decrement(ref _waitingCount);
        }
    }
    
    // Wrapper that releases semaphore on dispose
    private sealed class PooledConnectionWrapper : IDbConnection, IAsyncDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly SemaphoreSlim _limiter;
        private bool _disposed;
        
        public PooledConnectionWrapper(NpgsqlConnection connection, SemaphoreSlim limiter)
        {
            _connection = connection;
            _limiter = limiter;
        }
        
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            
            await _connection.DisposeAsync();
            _limiter.Release(); // Always release the semaphore
        }
        
        // Delegate IDbConnection members to _connection...
        public string ConnectionString { get => _connection.ConnectionString; set => _connection.ConnectionString = value; }
        public int ConnectionTimeout => _connection.ConnectionTimeout;
        public string Database => _connection.Database;
        public ConnectionState State => _connection.State;
        public IDbTransaction BeginTransaction() => _connection.BeginTransaction();
        public IDbTransaction BeginTransaction(IsolationLevel il) => _connection.BeginTransaction(il);
        public void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);
        public void Close() => _connection.Close();
        public IDbCommand CreateCommand() => _connection.CreateCommand();
        public void Open() => _connection.Open();
        public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
```

---

### Threat 5: Migration Tampering (Checksum Bypass)

**Risk:** An attacker could modify a previously applied migration file to inject malicious code, hoping it will be re-executed.

**Attack Scenario:** After initial deployment, an attacker modifies an existing migration file. On next deployment, the migration runner might re-execute if checksum validation is weak or disabled.

**Mitigation Code:**

```csharp
// Infrastructure/Migrations/MigrationIntegrityChecker.cs
namespace AgenticCoder.Infrastructure.Migrations;

public sealed class MigrationIntegrityChecker
{
    private readonly ILogger<MigrationIntegrityChecker> _logger;
    
    public async Task<IntegrityResult> VerifyAllMigrationsAsync(
        IDbConnection connection,
        IReadOnlyList<MigrationFile> embeddedMigrations,
        CancellationToken ct)
    {
        var issues = new List<IntegrityIssue>();
        
        // Get applied migrations from database
        var appliedMigrations = await GetAppliedMigrationsAsync(connection, ct);
        
        foreach (var applied in appliedMigrations)
        {
            var embedded = embeddedMigrations.FirstOrDefault(m => m.Version == applied.Version);
            
            if (embedded == null)
            {
                // Migration was applied but file is missing (might be intentional removal)
                issues.Add(new IntegrityIssue(
                    applied.Version,
                    IssueSeverity.Warning,
                    $"Migration {applied.Version} was applied but file not found in assembly"));
                continue;
            }
            
            // Compute current checksum
            var currentChecksum = ComputeChecksum(embedded.Content);
            
            if (currentChecksum != applied.Checksum)
            {
                // CRITICAL: Migration content changed after application
                issues.Add(new IntegrityIssue(
                    applied.Version,
                    IssueSeverity.Critical,
                    $"Migration {applied.Version} content has changed! " +
                    $"Expected checksum: {applied.Checksum}, Current: {currentChecksum}. " +
                    "This could indicate tampering."));
                
                _logger.LogCritical(
                    "SECURITY ALERT: Migration {Version} checksum mismatch. " +
                    "Applied: {AppliedChecksum}, Current: {CurrentChecksum}",
                    applied.Version, applied.Checksum, currentChecksum);
            }
        }
        
        // Check for gaps in sequence
        var versions = appliedMigrations.Select(m => int.Parse(m.Version.Split('_')[0])).OrderBy(v => v).ToList();
        for (int i = 1; i < versions.Count; i++)
        {
            if (versions[i] != versions[i - 1] + 1)
            {
                issues.Add(new IntegrityIssue(
                    $"{versions[i-1]+1:D3}",
                    IssueSeverity.Warning,
                    $"Gap in migration sequence between {versions[i-1]} and {versions[i]}"));
            }
        }
        
        return new IntegrityResult(
            issues.All(i => i.Severity != IssueSeverity.Critical),
            issues);
    }
    
    private static string ComputeChecksum(string content)
    {
        // Normalize line endings before hashing
        var normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");
        var bytes = System.Text.Encoding.UTF8.GetBytes(normalized);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    
    private async Task<IReadOnlyList<AppliedMigration>> GetAppliedMigrationsAsync(
        IDbConnection connection, 
        CancellationToken ct)
    {
        const string sql = "SELECT version, checksum, applied_at FROM sys_migrations ORDER BY version";
        return (await connection.QueryAsync<AppliedMigration>(sql)).ToList();
    }
}

public record IntegrityIssue(string Version, IssueSeverity Severity, string Message);
public enum IssueSeverity { Info, Warning, Critical }
public record IntegrityResult(bool IsValid, IReadOnlyList<IntegrityIssue> Issues);
public record AppliedMigration(string Version, string Checksum, DateTime AppliedAt);
public record MigrationFile(string Version, string Content);
```

---

## Best Practices

### Database Design

1. **Use migrations for all schema changes** - Never modify schema directly; all changes go through versioned migrations
2. **Design for forward compatibility** - Add nullable columns, avoid removing columns without deprecation period
3. **Keep transactions short** - Long transactions block other operations; commit frequently
4. **Index foreign keys** - Always index FK columns for efficient joins and cascading operations

### Connection Management

5. **Use connection factories** - Never create raw connections; use IConnectionFactory for consistency
6. **Configure timeouts** - Set appropriate command and connection timeouts for workload type
7. **Close connections promptly** - Use `using` statements to ensure connections are disposed
8. **Handle transient failures** - Implement retry logic with exponential backoff for network issues

### Data Integrity

9. **Validate before inserting** - Check data validity at application layer before database write
10. **Use constraints liberally** - NOT NULL, UNIQUE, CHECK constraints catch bugs early
11. **Soft delete by default** - Use deleted_at column instead of hard DELETE for audit trail
12. **Log schema changes** - Record who made schema changes and when in sys_migrations

---

## Troubleshooting

### Issue 1: Database file locked (SQLite)

**Symptoms:**
- Error message: "database is locked"
- Error message: "database is busy"
- Error code: SQLITE_BUSY (5) or SQLITE_LOCKED (6)
- Operations hang indefinitely then timeout

**Causes:**
- Multiple agent processes accessing same database file simultaneously
- Long-running transaction holding exclusive lock
- Previous agent process crashed leaving lock file orphaned
- External tool (DB browser) has file open
- Anti-virus software scanning database file during write

**Solutions:**

1. **Check for other processes:**
   ```powershell
   # Windows
   Get-Process | Where-Object { $_.Name -match 'acode|agent' }
   
   # Linux/Mac
   pgrep -f 'acode|agent'
   ```

2. **Remove stale lock files (if process not running):**
   ```powershell
   Remove-Item .agent/data/workspace.db-wal -ErrorAction SilentlyContinue
   Remove-Item .agent/data/workspace.db-shm -ErrorAction SilentlyContinue
   ```

3. **Enable WAL mode if not already:**
   ```sql
   PRAGMA journal_mode=WAL;
   ```

4. **Increase busy timeout:**
   ```yaml
   # acode.yml
   database:
     local:
       busyTimeoutMs: 10000  # 10 seconds
   ```

5. **Add anti-virus exclusion** for `.agent/data/` directory

---

### Issue 2: Migration checksum mismatch

**Symptoms:**
- Error: "Migration checksum validation failed for version NNN"
- Error: "SECURITY ALERT: Migration content changed"
- Migration refuses to run even though file looks correct

**Causes:**
- Migration file edited after being applied to database
- Line ending differences (CRLF vs LF) between environments
- Embedded resource not rebuilt after source file modification
- Merge conflict improperly resolved in migration file
- Encoding differences (UTF-8 with/without BOM)

**Solutions:**

1. **Compare checksums:**
   ```powershell
   # Get hash from database
   acode db status --verbose
   
   # Compute hash of file (normalize line endings)
   $content = (Get-Content -Path "migrations/001_initial.sql" -Raw) -replace "`r`n", "`n"
   $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
   $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
   [Convert]::ToHexString($hash).ToLowerInvariant()
   ```

2. **Reset migration state (DANGEROUS - development only):**
   ```sql
   DELETE FROM sys_migrations WHERE version = '001_initial_schema';
   ```

3. **Force re-apply with acknowledgment:**
   ```bash
   acode db migrate --force --version 001
   ```

4. **Standardize line endings:**
   ```bash
   # Configure git to normalize
   git config core.autocrlf false
   echo "*.sql text eol=lf" >> .gitattributes
   ```

---

### Issue 3: PostgreSQL connection timeout

**Symptoms:**
- Error: "A connection attempt failed because the connected party did not properly respond"
- Error: "Connection timed out"
- Operations hang for 30+ seconds before failing
- Intermittent connection failures

**Causes:**
- Firewall blocking port 5432 (or custom port)
- PostgreSQL server not running or not accepting connections
- Incorrect host/port configuration
- SSL/TLS handshake failure
- VPN or network routing issues
- Connection pool exhausted

**Solutions:**

1. **Test network connectivity:**
   ```powershell
   # Windows
   Test-NetConnection -ComputerName <host> -Port 5432
   
   # Linux
   nc -zv <host> 5432
   ```

2. **Verify PostgreSQL is listening:**
   ```bash
   # On PostgreSQL server
   sudo ss -tlnp | grep 5432
   ```

3. **Check pg_hba.conf allows connections:**
   ```
   # Add line to allow agent host
   host    agentdb    agentuser    <agent-ip>/32    scram-sha-256
   ```

4. **Test with psql:**
   ```bash
   PGPASSWORD=<pwd> psql -h <host> -U <user> -d <db> -c "SELECT 1"
   ```

5. **Check SSL requirements:**
   ```yaml
   # acode.yml - disable SSL for testing (not production!)
   database:
     remote:
       sslMode: disable
   ```

---

### Issue 4: Connection pool exhausted

**Symptoms:**
- Error: "The connection pool has been exhausted"
- Error: "ACODE-DB-008: Connection pool exhausted"
- Requests fail immediately without waiting
- High wait queue count in logs

**Causes:**
- Too many concurrent operations for pool size
- Connections not being returned to pool (leak)
- Long-running transactions holding connections
- MaxPoolSize too small for workload
- Deadlocked transactions holding multiple connections

**Solutions:**

1. **Check pool statistics:**
   ```bash
   acode db status --verbose
   # Shows: Pool size, active connections, waiting requests
   ```

2. **Increase pool size:**
   ```yaml
   # acode.yml
   database:
     remote:
       pool:
         maxSize: 20  # Increase from default 10
   ```

3. **Find connection leaks in code:**
   ```csharp
   // WRONG - connection never disposed
   var conn = await factory.CreateAsync(ct);
   var data = await conn.QueryAsync<T>(sql);
   
   // CORRECT - using ensures disposal
   await using var conn = await factory.CreateAsync(ct);
   var data = await conn.QueryAsync<T>(sql);
   ```

4. **Enable connection lifetime recycling:**
   ```yaml
   database:
     remote:
       pool:
         connectionLifetime: 1800  # 30 minutes
   ```

5. **Check for long transactions:**
   ```sql
   SELECT pid, now() - xact_start AS duration, query
   FROM pg_stat_activity
   WHERE state = 'active'
   ORDER BY duration DESC;
   ```

---

### Issue 5: Migration fails mid-execution

**Symptoms:**
- Error: "Migration 003_add_index failed"
- Database in inconsistent state
- Some tables created, others missing
- Subsequent migrations also fail

**Causes:**
- SQL syntax error in migration file
- Constraint violation (duplicate key, FK reference)
- Disk space exhausted during large migration
- Connection lost during migration
- Timeout on long-running migration

**Solutions:**

1. **Check migration logs:**
   ```bash
   acode db migrate --verbose
   # Shows each statement as executed
   ```

2. **Run dry-run first:**
   ```bash
   acode db migrate --dry-run
   # Shows what would be executed without applying
   ```

3. **Manual rollback (if atomic rollback failed):**
   ```sql
   -- Check what was applied
   SELECT * FROM sys_migrations ORDER BY version;
   
   -- Manually run rollback SQL
   -- Then delete migration record
   DELETE FROM sys_migrations WHERE version = '003_add_index';
   ```

4. **Increase migration timeout:**
   ```yaml
   database:
     migrations:
       timeoutSeconds: 600  # 10 minutes for large migrations
   ```

5. **Split large migrations into smaller steps:**
   ```
   003a_create_table.sql
   003b_add_indexes.sql
   003c_add_constraints.sql
   ```

---

### Issue 6: WAL file grows unbounded (SQLite)

**Symptoms:**
- workspace.db-wal file is gigabytes in size
- Disk space running low
- Database operations becoming slow
- Checkpoint warnings in logs

**Causes:**
- Long-running read transaction preventing checkpoint
- Checkpoint not running (process always busy)
- Very high write volume
- WAL checkpoint threshold too high

**Solutions:**

1. **Force checkpoint:**
   ```sql
   PRAGMA wal_checkpoint(TRUNCATE);
   ```

2. **Check for blocking readers:**
   ```bash
   # Find processes with file open
   lsof .agent/data/workspace.db
   ```

3. **Configure auto-checkpoint threshold:**
   ```yaml
   database:
     local:
       walAutocheckpoint: 1000  # Checkpoint after 1000 pages
   ```

4. **Schedule periodic checkpoint:**
   ```csharp
   // In maintenance service
   await connection.ExecuteAsync("PRAGMA wal_checkpoint(PASSIVE);");
   ```

---

### Issue 7: Health check fails but database works

**Symptoms:**
- `acode db status` shows unhealthy
- Health endpoint returns 503
- But manual queries work fine
- Intermittent health failures

**Causes:**
- Health check timeout too aggressive
- Health check query hitting cold cache
- Network latency spikes
- Health check connection competing with normal traffic
- SSL certificate verification failure

**Solutions:**

1. **Increase health check timeout:**
   ```yaml
   database:
     healthCheck:
       timeoutSeconds: 10  # Default is 5
   ```

2. **Use dedicated connection for health:**
   ```yaml
   database:
     healthCheck:
       separateConnection: true
   ```

3. **Simplify health check query:**
   ```csharp
   // Default (can be slow on cold cache)
   SELECT COUNT(*) FROM sys_migrations
   
   // Better
   SELECT 1
   ```

4. **Check SSL certificate:**
   ```bash
   openssl s_client -connect <host>:5432 -starttls postgres
   ```

---

### Issue 8: Database corruption (SQLite)

**Symptoms:**
- Error: "database disk image is malformed"
- Error: "SQLITE_CORRUPT"
- Queries return unexpected results
- Integrity check fails

**Causes:**
- Power failure during write
- Disk hardware failure
- File system corruption
- Copying database while in use (WAL not included)
- Force-killing process during transaction

**Solutions:**

1. **Run integrity check:**
   ```bash
   acode db verify
   # Or directly
   sqlite3 .agent/data/workspace.db "PRAGMA integrity_check;"
   ```

2. **Attempt recovery:**
   ```bash
   sqlite3 .agent/data/workspace.db ".recover" | sqlite3 recovered.db
   ```

3. **Restore from backup:**
   ```bash
   # List backups
   ls .agent/data/backups/
   
   # Restore
   cp .agent/data/backups/workspace-20240115-120000.db .agent/data/workspace.db
   ```

4. **Rebuild from sync (if available):**
   ```bash
   rm .agent/data/workspace.db
   acode sync pull
   ```

5. **Prevent future corruption:**
   ```yaml
   database:
     local:
       synchronous: FULL  # Safer but slower
       journalMode: WAL   # More robust than DELETE
   ```

---

## Testing Requirements

### Unit Tests

#### SqliteConnectionFactoryTests.cs

```csharp
// tests/Acode.Infrastructure.Tests/Database/SqliteConnectionFactoryTests.cs
namespace Acode.Infrastructure.Tests.Database;

[TestClass]
public class SqliteConnectionFactoryTests
{
    private readonly string _testDbPath;
    private readonly SqliteConnectionFactory _factory;
    
    public SqliteConnectionFactoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        var options = new DatabaseOptions
        {
            Local = new LocalDatabaseOptions
            {
                Path = _testDbPath,
                BusyTimeoutMs = 5000
            }
        };
        _factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        File.Delete(_testDbPath);
        File.Delete(_testDbPath + "-wal");
        File.Delete(_testDbPath + "-shm");
    }
    
    [TestMethod]
    public async Task CreateAsync_Should_Create_Database_File()
    {
        // Act
        await using var connection = await _factory.CreateAsync(CancellationToken.None);
        
        // Assert
        Assert.IsTrue(File.Exists(_testDbPath), "Database file should be created");
    }
    
    [TestMethod]
    public async Task CreateAsync_Should_Return_Open_Connection()
    {
        // Act
        await using var connection = await _factory.CreateAsync(CancellationToken.None);
        
        // Assert
        Assert.AreEqual(ConnectionState.Open, connection.State);
    }
    
    [TestMethod]
    public async Task CreateAsync_Should_Enable_WAL_Mode()
    {
        // Act
        await using var connection = await _factory.CreateAsync(CancellationToken.None);
        
        // Assert
        var result = await connection.QuerySingleAsync<string>("PRAGMA journal_mode;");
        Assert.AreEqual("wal", result.ToLowerInvariant());
    }
    
    [TestMethod]
    public async Task CreateAsync_Should_Set_Busy_Timeout()
    {
        // Act
        await using var connection = await _factory.CreateAsync(CancellationToken.None);
        
        // Assert
        var result = await connection.QuerySingleAsync<int>("PRAGMA busy_timeout;");
        Assert.AreEqual(5000, result);
    }
    
    [TestMethod]
    public async Task CreateAsync_Should_Respect_Cancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act & Assert
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => _factory.CreateAsync(cts.Token));
    }
    
    [TestMethod]
    public async Task CreateAsync_Should_Create_Parent_Directory()
    {
        // Arrange
        var nestedPath = Path.Combine(Path.GetTempPath(), "nested", "dirs", $"test_{Guid.NewGuid()}.db");
        var options = new DatabaseOptions { Local = new LocalDatabaseOptions { Path = nestedPath } };
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);
        
        try
        {
            // Act
            await using var connection = await factory.CreateAsync(CancellationToken.None);
            
            // Assert
            Assert.IsTrue(Directory.Exists(Path.GetDirectoryName(nestedPath)));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(nestedPath)!, true);
        }
    }
    
    [TestMethod]
    public async Task CheckHealthAsync_Should_Return_Healthy_When_Database_Accessible()
    {
        // Arrange
        await using var _ = await _factory.CreateAsync(CancellationToken.None);
        
        // Act
        var result = await _factory.CheckHealthAsync(CancellationToken.None);
        
        // Assert
        Assert.AreEqual(HealthStatus.Healthy, result.Status);
    }
    
    [TestMethod]
    public async Task CheckHealthAsync_Should_Return_Unhealthy_When_Database_Missing()
    {
        // Arrange - don't create the database
        
        // Act
        var result = await _factory.CheckHealthAsync(CancellationToken.None);
        
        // Assert
        Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
        Assert.IsTrue(result.Description.Contains("not found"));
    }
}
```

#### PostgresConnectionFactoryTests.cs

```csharp
// tests/Acode.Infrastructure.Tests/Database/PostgresConnectionFactoryTests.cs
namespace Acode.Infrastructure.Tests.Database;

[TestClass]
public class PostgresConnectionFactoryTests
{
    [TestMethod]
    public void Constructor_Should_Parse_ConnectionString()
    {
        // Arrange
        var options = new DatabaseOptions
        {
            Remote = new RemoteDatabaseOptions
            {
                ConnectionString = "Host=localhost;Database=test;Username=user;Password=pass"
            }
        };
        
        // Act
        var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);
        
        // Assert - no exception thrown
        Assert.IsNotNull(factory);
    }
    
    [TestMethod]
    public void Constructor_Should_Build_ConnectionString_From_Parts()
    {
        // Arrange
        var options = new DatabaseOptions
        {
            Remote = new RemoteDatabaseOptions
            {
                Host = "localhost",
                Port = 5432,
                Database = "testdb",
                Username = "testuser",
                Password = "testpass"
            }
        };
        
        // Act
        var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);
        
        // Assert
        Assert.IsNotNull(factory);
    }
    
    [TestMethod]
    public void Constructor_Should_Apply_Pool_Settings()
    {
        // Arrange
        var options = new DatabaseOptions
        {
            Remote = new RemoteDatabaseOptions
            {
                ConnectionString = "Host=localhost;Database=test",
                Pool = new PoolOptions
                {
                    MinSize = 5,
                    MaxSize = 25,
                    IdleTimeout = 600
                }
            }
        };
        
        // Act
        var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);
        
        // Assert - settings applied (verified via internal state or connection string)
        Assert.IsNotNull(factory);
    }
    
    [TestMethod]
    public void Constructor_Should_Throw_On_Invalid_ConnectionString()
    {
        // Arrange
        var options = new DatabaseOptions
        {
            Remote = new RemoteDatabaseOptions
            {
                ConnectionString = "invalid=garbage=connection=string"
            }
        };
        
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(
            () => new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance));
    }
}
```

#### MigrationRunnerTests.cs

```csharp
// tests/Acode.Infrastructure.Tests/Migrations/MigrationRunnerTests.cs
namespace Acode.Infrastructure.Tests.Migrations;

[TestClass]
public class MigrationRunnerTests
{
    private readonly Mock<IConnectionFactory> _mockFactory;
    private readonly Mock<IDbConnection> _mockConnection;
    private readonly MigrationRunner _runner;
    
    public MigrationRunnerTests()
    {
        _mockConnection = new Mock<IDbConnection>();
        _mockFactory = new Mock<IConnectionFactory>();
        _mockFactory.Setup(f => f.CreateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockConnection.Object);
        
        _runner = new MigrationRunner(
            _mockFactory.Object,
            new MigrationOptions { AutoRunOnStartup = false },
            NullLogger<MigrationRunner>.Instance);
    }
    
    [TestMethod]
    public async Task GetPendingMigrationsAsync_Should_Return_Unapplied_Migrations()
    {
        // Arrange
        SetupAppliedMigrations("001_initial");
        
        // Act
        var pending = await _runner.GetPendingMigrationsAsync(CancellationToken.None);
        
        // Assert
        Assert.IsFalse(pending.Any(m => m.Version == "001_initial"));
        Assert.IsTrue(pending.Any(m => m.Version.CompareTo("001_initial") > 0));
    }
    
    [TestMethod]
    public async Task ApplyMigrationAsync_Should_Execute_Migration_SQL()
    {
        // Arrange
        var migration = new Migration("002_add_table", "CREATE TABLE test (id INT);");
        
        // Act
        await _runner.ApplyMigrationAsync(migration, CancellationToken.None);
        
        // Assert
        _mockConnection.Verify(c => c.ExecuteAsync(
            It.Is<string>(s => s.Contains("CREATE TABLE test")),
            It.IsAny<object>(),
            It.IsAny<IDbTransaction>(),
            It.IsAny<int?>(),
            It.IsAny<CommandType?>()),
            Times.Once);
    }
    
    [TestMethod]
    public async Task ApplyMigrationAsync_Should_Record_In_SysMigrations()
    {
        // Arrange
        var migration = new Migration("002_add_table", "CREATE TABLE test (id INT);");
        
        // Act
        await _runner.ApplyMigrationAsync(migration, CancellationToken.None);
        
        // Assert
        _mockConnection.Verify(c => c.ExecuteAsync(
            It.Is<string>(s => s.Contains("INSERT INTO sys_migrations")),
            It.IsAny<object>(),
            It.IsAny<IDbTransaction>(),
            It.IsAny<int?>(),
            It.IsAny<CommandType?>()),
            Times.Once);
    }
    
    [TestMethod]
    public async Task ApplyMigrationAsync_Should_Use_Transaction()
    {
        // Arrange
        var migration = new Migration("002_add_table", "CREATE TABLE test (id INT);");
        var mockTransaction = new Mock<IDbTransaction>();
        _mockConnection.Setup(c => c.BeginTransaction()).Returns(mockTransaction.Object);
        
        // Act
        await _runner.ApplyMigrationAsync(migration, CancellationToken.None);
        
        // Assert
        _mockConnection.Verify(c => c.BeginTransaction(), Times.Once);
        mockTransaction.Verify(t => t.Commit(), Times.Once);
    }
    
    [TestMethod]
    public async Task ApplyMigrationAsync_Should_Rollback_On_Failure()
    {
        // Arrange
        var migration = new Migration("002_add_table", "CREATE TABLE test (id INT);");
        var mockTransaction = new Mock<IDbTransaction>();
        _mockConnection.Setup(c => c.BeginTransaction()).Returns(mockTransaction.Object);
        _mockConnection.Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>(), 
            It.IsAny<IDbTransaction>(), It.IsAny<int?>(), It.IsAny<CommandType?>()))
            .ThrowsAsync(new Exception("SQL error"));
        
        // Act
        await Assert.ThrowsExceptionAsync<MigrationException>(
            () => _runner.ApplyMigrationAsync(migration, CancellationToken.None));
        
        // Assert
        mockTransaction.Verify(t => t.Rollback(), Times.Once);
    }
    
    [TestMethod]
    public async Task RollbackMigrationAsync_Should_Execute_Down_Script()
    {
        // Arrange
        var migration = new Migration("002_add_table", "CREATE TABLE test (id INT);", 
            rollbackSql: "DROP TABLE test;");
        
        // Act
        await _runner.RollbackMigrationAsync(migration, CancellationToken.None);
        
        // Assert
        _mockConnection.Verify(c => c.ExecuteAsync(
            It.Is<string>(s => s.Contains("DROP TABLE test")),
            It.IsAny<object>(),
            It.IsAny<IDbTransaction>(),
            It.IsAny<int?>(),
            It.IsAny<CommandType?>()),
            Times.Once);
    }
    
    [TestMethod]
    public async Task ValidateChecksumAsync_Should_Detect_Tampering()
    {
        // Arrange
        SetupAppliedMigrations("001_initial", checksum: "abc123");
        var migration = new Migration("001_initial", "MODIFIED CONTENT");
        
        // Act
        var result = await _runner.ValidateChecksumAsync(migration, CancellationToken.None);
        
        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Message.Contains("checksum mismatch"));
    }
    
    private void SetupAppliedMigrations(string version, string checksum = null)
    {
        var migrations = new List<AppliedMigration>
        {
            new AppliedMigration(version, checksum ?? "hash", DateTime.UtcNow)
        };
        
        _mockConnection.Setup(c => c.QueryAsync<AppliedMigration>(
            It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(),
            It.IsAny<int?>(), It.IsAny<CommandType?>()))
            .ReturnsAsync(migrations);
    }
}
```

### Integration Tests

#### SqliteIntegrationTests.cs

```csharp
// tests/Acode.Integration.Tests/Database/SqliteIntegrationTests.cs
namespace Acode.Integration.Tests.Database;

[TestClass]
public class SqliteIntegrationTests
{
    private string _testDbPath;
    private SqliteConnectionFactory _factory;
    
    [TestInitialize]
    public void Setup()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"integration_test_{Guid.NewGuid()}.db");
        var options = new DatabaseOptions
        {
            Local = new LocalDatabaseOptions { Path = _testDbPath, BusyTimeoutMs = 5000 }
        };
        _factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        File.Delete(_testDbPath);
        File.Delete(_testDbPath + "-wal");
        File.Delete(_testDbPath + "-shm");
    }
    
    [TestMethod]
    public async Task Should_Write_And_Read_Data()
    {
        // Arrange
        await using var connection = await _factory.CreateAsync(CancellationToken.None);
        await connection.ExecuteAsync("CREATE TABLE test (id INTEGER PRIMARY KEY, value TEXT)");
        
        // Act
        await connection.ExecuteAsync("INSERT INTO test (id, value) VALUES (1, 'hello')");
        var result = await connection.QuerySingleAsync<string>("SELECT value FROM test WHERE id = 1");
        
        // Assert
        Assert.AreEqual("hello", result);
    }
    
    [TestMethod]
    public async Task Should_Handle_Concurrent_Reads()
    {
        // Arrange
        await using var connection = await _factory.CreateAsync(CancellationToken.None);
        await connection.ExecuteAsync("CREATE TABLE test (id INTEGER PRIMARY KEY, value TEXT)");
        await connection.ExecuteAsync("INSERT INTO test (id, value) VALUES (1, 'data')");
        
        // Act - multiple concurrent reads
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            await using var conn = await _factory.CreateAsync(CancellationToken.None);
            return await conn.QuerySingleAsync<string>("SELECT value FROM test WHERE id = 1");
        });
        
        var results = await Task.WhenAll(tasks);
        
        // Assert - all reads succeed
        Assert.IsTrue(results.All(r => r == "data"));
    }
    
    [TestMethod]
    public async Task Should_Handle_Transaction_Commit()
    {
        // Arrange
        await using var connection = await _factory.CreateAsync(CancellationToken.None);
        await connection.ExecuteAsync("CREATE TABLE test (id INTEGER PRIMARY KEY, value TEXT)");
        
        // Act
        await using var transaction = await connection.BeginTransactionAsync();
        await connection.ExecuteAsync("INSERT INTO test (id, value) VALUES (1, 'committed')", transaction: transaction);
        await transaction.CommitAsync();
        
        // Assert - data persists after commit
        var result = await connection.QuerySingleAsync<string>("SELECT value FROM test WHERE id = 1");
        Assert.AreEqual("committed", result);
    }
    
    [TestMethod]
    public async Task Should_Handle_Transaction_Rollback()
    {
        // Arrange
        await using var connection = await _factory.CreateAsync(CancellationToken.None);
        await connection.ExecuteAsync("CREATE TABLE test (id INTEGER PRIMARY KEY, value TEXT)");
        
        // Act
        await using var transaction = await connection.BeginTransactionAsync();
        await connection.ExecuteAsync("INSERT INTO test (id, value) VALUES (1, 'rollback me')", transaction: transaction);
        await transaction.RollbackAsync();
        
        // Assert - data not persisted
        var count = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM test");
        Assert.AreEqual(0, count);
    }
}
```

#### MigrationRunnerIntegrationTests.cs

```csharp
// tests/Acode.Integration.Tests/Migrations/MigrationRunnerIntegrationTests.cs
namespace Acode.Integration.Tests.Migrations;

[TestClass]
public class MigrationRunnerIntegrationTests
{
    private string _testDbPath;
    private SqliteConnectionFactory _factory;
    private MigrationRunner _runner;
    
    [TestInitialize]
    public void Setup()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"migration_test_{Guid.NewGuid()}.db");
        var options = new DatabaseOptions
        {
            Local = new LocalDatabaseOptions { Path = _testDbPath }
        };
        _factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);
        _runner = new MigrationRunner(_factory, new MigrationOptions(), NullLogger<MigrationRunner>.Instance);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        File.Delete(_testDbPath);
    }
    
    [TestMethod]
    public async Task Should_Apply_All_Pending_Migrations()
    {
        // Arrange
        await using var conn = await _factory.CreateAsync(CancellationToken.None);
        
        // Act
        var result = await _runner.MigrateAsync(CancellationToken.None);
        
        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.AppliedCount > 0);
        
        // Verify sys_migrations table exists and has records
        var count = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM sys_migrations");
        Assert.IsTrue(count > 0);
    }
    
    [TestMethod]
    public async Task Should_Rollback_Failed_Migration()
    {
        // Arrange - inject a bad migration
        var badMigration = new Migration("999_bad", "CREATE TABLE test (id INTEGER); INVALID SQL HERE;");
        
        // Act
        var result = await _runner.ApplyMigrationAsync(badMigration, CancellationToken.None);
        
        // Assert
        Assert.IsFalse(result.Success);
        
        // Verify table wasn't created (rollback worked)
        await using var conn = await _factory.CreateAsync(CancellationToken.None);
        var tables = await conn.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name='test'");
        Assert.IsFalse(tables.Any());
    }
}
```

### E2E Tests

```csharp
// tests/Acode.E2E.Tests/Database/DatabaseE2ETests.cs
namespace Acode.E2E.Tests.Database;

[TestClass]
public class DatabaseE2ETests
{
    [TestMethod]
    public async Task Should_Initialize_Database_On_First_Run()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"e2e_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Act - run CLI
            var result = await CliRunner.RunAsync(
                "acode", "run", "echo test",
                workingDir: tempDir);
            
            // Assert
            Assert.AreEqual(0, result.ExitCode);
            Assert.IsTrue(File.Exists(Path.Combine(tempDir, ".agent", "data", "workspace.db")));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
    
    [TestMethod]
    public async Task Should_Show_Status_After_Init()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"e2e_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Initialize
            await CliRunner.RunAsync("acode", "run", "echo init", workingDir: tempDir);
            
            // Act
            var result = await CliRunner.RunAsync("acode", "db", "status", workingDir: tempDir);
            
            // Assert
            Assert.AreEqual(0, result.ExitCode);
            Assert.IsTrue(result.Output.Contains("SQLite"));
            Assert.IsTrue(result.Output.Contains("Healthy") || result.Output.Contains("OK"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
```

### Performance Benchmarks

```csharp
// tests/Acode.Benchmarks/Database/DatabaseBenchmarks.cs
namespace Acode.Benchmarks.Database;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class DatabaseBenchmarks
{
    private SqliteConnectionFactory _sqliteFactory;
    private string _testDbPath;
    
    [GlobalSetup]
    public void Setup()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"benchmark_{Guid.NewGuid()}.db");
        var options = new DatabaseOptions
        {
            Local = new LocalDatabaseOptions { Path = _testDbPath }
        };
        _sqliteFactory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);
        
        // Pre-create database with test table
        using var conn = _sqliteFactory.CreateAsync(CancellationToken.None).Result;
        conn.Execute("CREATE TABLE test (id INTEGER PRIMARY KEY, value TEXT)");
        for (int i = 0; i < 1000; i++)
        {
            conn.Execute($"INSERT INTO test (id, value) VALUES ({i}, 'value_{i}')");
        }
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        File.Delete(_testDbPath);
    }
    
    [Benchmark(Description = "SQLite: Connection Acquire")]
    public async Task<IDbConnection> ConnectionAcquire()
    {
        var conn = await _sqliteFactory.CreateAsync(CancellationToken.None);
        await conn.DisposeAsync();
        return conn;
    }
    
    [Benchmark(Description = "SQLite: Simple Insert")]
    public async Task SimpleInsert()
    {
        await using var conn = await _sqliteFactory.CreateAsync(CancellationToken.None);
        await conn.ExecuteAsync("INSERT INTO test (value) VALUES (@Value)", new { Value = "test" });
    }
    
    [Benchmark(Description = "SQLite: Simple Query by PK")]
    public async Task<string> SimpleQueryByPK()
    {
        await using var conn = await _sqliteFactory.CreateAsync(CancellationToken.None);
        return await conn.QuerySingleAsync<string>("SELECT value FROM test WHERE id = 500");
    }
    
    [Benchmark(Description = "SQLite: Health Check")]
    public async Task<HealthCheckResult> HealthCheck()
    {
        return await _sqliteFactory.CheckHealthAsync(CancellationToken.None);
    }
}
```

### Performance Targets

| Benchmark | Target | Maximum | Notes |
|-----------|--------|---------|-------|
| Connection acquire (SQLite) | 2ms | 5ms | Local file, warm cache |
| Connection acquire (PostgreSQL pool hit) | 5ms | 10ms | From pool, no network |
| Connection acquire (PostgreSQL cold) | 50ms | 200ms | New connection, network |
| Simple insert | 2ms | 10ms | Single row, no index rebuild |
| Simple query by PK | 1ms | 5ms | Indexed lookup |
| Transaction commit | 5ms | 20ms | WAL mode sync |
| Migration step (simple) | 100ms | 1s | CREATE TABLE |
| Migration step (complex) | 5s | 30s | Large data migration |
| Health check | 10ms | 50ms | Connection + SELECT 1 |

---

## User Verification Steps

### Scenario 1: First Run - Database Initialization

**Objective:** Verify database is created automatically on first use

**Prerequisites:**
- Clean workspace with no `.agent/` directory
- `acode` CLI installed and configured

**Steps:**
1. Open terminal in clean workspace directory
2. Verify no database exists: `ls .agent/data/` (should fail or be empty)
3. Run any agent command: `acode run "echo hello"`
4. Wait for command to complete

**Expected Results:**
- Command completes successfully
- Database file exists: `.agent/data/workspace.db`
- WAL file created: `.agent/data/workspace.db-wal`
- Database contains sys_migrations table with initial migrations applied
- File permissions restrict access to owner only

**Verification Commands:**
```bash
# Check file exists
ls -la .agent/data/workspace.db

# Check migrations applied
sqlite3 .agent/data/workspace.db "SELECT * FROM sys_migrations;"

# Check WAL mode enabled
sqlite3 .agent/data/workspace.db "PRAGMA journal_mode;"
# Expected output: wal
```

---

### Scenario 2: Database Migration - Forward Migration

**Objective:** Verify migrations can be applied via CLI

**Prerequisites:**
- Workspace with existing database
- New migration files available (bundled with newer version)

**Steps:**
1. Check current migration status: `acode db status`
2. Note pending migrations count
3. Run dry-run first: `acode db migrate --dry-run`
4. Review what will be applied
5. Apply migrations: `acode db migrate`

**Expected Results:**
- Status shows pending migrations before apply
- Dry-run shows migration names without applying
- Migration applies successfully
- Status shows no pending migrations after
- New tables/columns exist in database

**Verification Commands:**
```bash
# Check status before
acode db status
# Shows: "Pending migrations: 2"

# Dry run
acode db migrate --dry-run
# Shows: "Would apply: 003_add_sessions, 004_add_indexes"

# Apply
acode db migrate
# Shows: "Applied 2 migrations in 0.45s"

# Verify
acode db status
# Shows: "Pending migrations: 0"
```

---

### Scenario 3: Database Migration - Rollback

**Objective:** Verify migrations can be rolled back

**Prerequisites:**
- Database with at least 2 migrations applied
- Most recent migration has rollback script

**Steps:**
1. Check current version: `acode db status`
2. Roll back most recent: `acode db rollback`
3. Confirm when prompted
4. Check status again

**Expected Results:**
- Rollback prompts for confirmation
- Most recent migration is reversed
- Migration record removed from sys_migrations
- Schema changes are undone

**Verification Commands:**
```bash
# Check before rollback
acode db status
# Shows: "Applied migrations: 4"

# Rollback
acode db rollback
# Prompts: "Roll back 004_add_indexes? [y/N]"
# Type: y

# Check after
acode db status
# Shows: "Applied migrations: 3, Pending: 1"

# Verify table/index removed
sqlite3 .agent/data/workspace.db ".schema"
```

---

### Scenario 4: Database Status Command

**Objective:** Verify status command shows comprehensive information

**Prerequisites:**
- Initialized database with migrations applied

**Steps:**
1. Run status command: `acode db status`
2. Run verbose status: `acode db status --verbose`

**Expected Results:**
- Status shows database location
- Status shows database size
- Status shows health (Healthy/Unhealthy)
- Status shows migration count (applied/pending)
- Verbose shows detailed table information

**Expected Output:**
```
Database Status
───────────────────────────────
Provider:     SQLite
Location:     .agent/data/workspace.db
Size:         245 KB
Health:       ✓ Healthy
Migrations:   4 applied, 0 pending
Last Applied: 004_add_indexes (2024-01-15 10:30:22)

Tables:
  - sys_migrations (4 rows)
  - conversations (127 rows)
  - messages (1,043 rows)
  - approvals (23 rows)
```

---

### Scenario 5: Database Backup

**Objective:** Verify backup creates valid copy

**Prerequisites:**
- Database with data

**Steps:**
1. Run backup: `acode db backup`
2. Run backup with custom path: `acode db backup --output ./my-backup.db`
3. Verify backup is valid

**Expected Results:**
- Backup file created with timestamp
- Custom path backup created at specified location
- Backup file is valid SQLite database
- Backup contains all data

**Verification Commands:**
```bash
# Default backup
acode db backup
# Output: "Backup created: .agent/data/backups/workspace-20240115-103022.db"

# Custom path
acode db backup --output ./my-backup.db
# Output: "Backup created: ./my-backup.db"

# Verify backup integrity
sqlite3 ./my-backup.db "PRAGMA integrity_check;"
# Expected: "ok"

# Verify data copied
sqlite3 ./my-backup.db "SELECT COUNT(*) FROM conversations;"
```

---

### Scenario 6: PostgreSQL Remote Configuration

**Objective:** Verify PostgreSQL connection works when configured

**Prerequisites:**
- PostgreSQL server accessible
- Valid credentials

**Steps:**
1. Configure PostgreSQL in acode.yml:
   ```yaml
   database:
     remote:
       enabled: true
       host: localhost
       port: 5432
       database: agentdb
       username: agentuser
       password: ${POSTGRES_PASSWORD}
   ```
2. Set environment variable: `export POSTGRES_PASSWORD=secret`
3. Check status: `acode db status`

**Expected Results:**
- Status shows PostgreSQL provider
- Connection successful
- Pool statistics displayed
- Health check passes

**Expected Output:**
```
Database Status
───────────────────────────────
Provider:     PostgreSQL
Host:         localhost:5432
Database:     agentdb
Health:       ✓ Healthy
Pool:         2/10 connections (min/max)
Active:       1 connection
Migrations:   4 applied, 0 pending
```

---

### Scenario 7: Schema Inspection

**Objective:** Verify schema command displays database structure

**Prerequisites:**
- Initialized database with tables

**Steps:**
1. Run schema command: `acode db schema`
2. Run for specific table: `acode db schema --table conversations`

**Expected Results:**
- All tables listed with columns
- Column types and constraints shown
- Indexes listed
- Foreign keys shown

**Expected Output:**
```
Database Schema
───────────────────────────────
Table: sys_migrations
  - version (TEXT NOT NULL PRIMARY KEY)
  - checksum (TEXT NOT NULL)
  - applied_at (TEXT NOT NULL)

Table: conversations
  - id (TEXT NOT NULL PRIMARY KEY)
  - title (TEXT)
  - created_at (TEXT NOT NULL)
  - updated_at (TEXT NOT NULL)
  Indexes:
    - idx_conversations_created (created_at)

Table: messages
  - id (TEXT NOT NULL PRIMARY KEY)
  - conversation_id (TEXT NOT NULL FK:conversations.id)
  - role (TEXT NOT NULL)
  - content (TEXT NOT NULL)
  - created_at (TEXT NOT NULL)
```

---

### Scenario 8: Database Integrity Verification

**Objective:** Verify integrity check detects and reports issues

**Prerequisites:**
- Database file

**Steps:**
1. Run verify command: `acode db verify`
2. Check output

**Expected Results:**
- Integrity check runs successfully
- Foreign key violations reported if any
- Checksum validation performed
- Clear pass/fail indication

**Expected Output (Healthy):**
```
Database Verification
───────────────────────────────
SQLite Integrity:     ✓ OK
Foreign Keys:         ✓ No violations
Migration Checksums:  ✓ 4/4 valid
Overall:              ✓ PASS
```

**Expected Output (Issues):**
```
Database Verification
───────────────────────────────
SQLite Integrity:     ✓ OK
Foreign Keys:         ⚠ 2 violations found
  - messages.conversation_id: 3 orphan rows
Migration Checksums:  ✗ FAIL
  - 003_add_sessions: checksum mismatch (SECURITY ALERT)
Overall:              ✗ FAIL
```

---

### Scenario 9: Concurrent Access Test

**Objective:** Verify database handles concurrent operations

**Prerequisites:**
- SQLite database initialized

**Steps:**
1. Open two terminal windows
2. In terminal 1: Start long read operation
3. In terminal 2: Attempt write operation
4. Verify both complete

**Terminal 1:**
```bash
sqlite3 .agent/data/workspace.db "BEGIN; SELECT * FROM messages LIMIT 1000; SELECT sleep(5); COMMIT;"
```

**Terminal 2 (while terminal 1 running):**
```bash
acode run "test concurrent write"
```

**Expected Results:**
- Both operations complete (WAL mode allows concurrent read/write)
- No "database is locked" error
- Write completes after short wait (busy timeout)

---

### Scenario 10: Migration Checksum Security Alert

**Objective:** Verify checksum mismatch triggers security alert

**Prerequisites:**
- Database with migrations applied

**Steps:**
1. Manually modify a migration file content (simulate tampering)
2. Run migrate or verify command
3. Check for security alert

**Simulation:**
```bash
# Note: This simulates what happens if migration files are tampered with
acode db verify
```

**Expected Results:**
- Security alert logged at CRITICAL level
- Migration refuses to run
- Clear indication of which migration was modified
- Recommended action provided

**Expected Output:**
```
⚠️  SECURITY ALERT ⚠️
───────────────────────────────
Migration integrity check failed!

Migration 002_add_sessions has been modified since it was applied.
  Expected checksum: a1b2c3d4e5f6...
  Current checksum:  9z8y7x6w5v4u...

This could indicate:
  - Unauthorized modification of migration files
  - Source control merge conflict
  - Encoding changes (CRLF/LF)

Action required: Investigate the change before proceeding.
If intentional, use --force flag (not recommended for production).
```

---

## Implementation Prompt

### File Structure

```
src/Acode.Infrastructure/
├── Database/
│   ├── IConnectionFactory.cs
│   ├── SqliteConnectionFactory.cs
│   ├── PostgresConnectionFactory.cs
│   ├── DatabaseHealthCheck.cs
│   ├── DatabaseOptions.cs
│   ├── DatabaseException.cs
│   └── CircuitBreaker/
│       ├── ICircuitBreaker.cs
│       └── DatabaseCircuitBreaker.cs
├── Migrations/
│   ├── IMigrationRunner.cs
│   ├── MigrationRunner.cs
│   ├── Migration.cs
│   ├── MigrationOptions.cs
│   ├── MigrationValidator.cs
│   ├── MigrationIntegrityChecker.cs
│   └── Scripts/
│       ├── 001_initial_schema.sql
│       ├── 001_initial_schema_down.sql
│       └── ...

src/Acode.Application/
├── Database/
│   ├── IDatabaseService.cs
│   └── DatabaseService.cs

src/Acode.Cli/
└── Commands/
    └── DatabaseCommand.cs
```

### IConnectionFactory Interface

```csharp
// src/Acode.Infrastructure/Database/IConnectionFactory.cs
namespace Acode.Infrastructure.Database;

using System.Data;

/// <summary>
/// Factory for creating database connections with proper configuration.
/// </summary>
public interface IConnectionFactory
{
    /// <summary>
    /// Creates and opens a new database connection.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>An open database connection</returns>
    /// <exception cref="DatabaseConnectionException">When connection fails</exception>
    Task<IDbConnection> CreateAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Checks the health of the database connection.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Health check result with status and details</returns>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets the provider type (SQLite, PostgreSQL).
    /// </summary>
    DatabaseProvider Provider { get; }
}

public enum DatabaseProvider
{
    SQLite,
    PostgreSQL
}

public record HealthCheckResult(
    HealthStatus Status,
    string Description,
    IReadOnlyDictionary<string, object>? Data = null);

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}
```

### SqliteConnectionFactory Implementation

```csharp
// src/Acode.Infrastructure/Database/SqliteConnectionFactory.cs
namespace Acode.Infrastructure.Database;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;

public sealed class SqliteConnectionFactory : IConnectionFactory, IDisposable
{
    private readonly DatabaseOptions _options;
    private readonly ILogger<SqliteConnectionFactory> _logger;
    private readonly string _connectionString;
    private bool _disposed;
    
    public DatabaseProvider Provider => DatabaseProvider.SQLite;
    
    public SqliteConnectionFactory(
        IOptions<DatabaseOptions> options,
        ILogger<SqliteConnectionFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        var dbPath = _options.Local?.Path 
            ?? Path.Combine(Environment.CurrentDirectory, ".agent", "data", "workspace.db");
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogDebug("Created database directory: {Directory}", directory);
        }
        
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
        
        _logger.LogDebug("SQLite connection factory initialized. Path: {Path}", dbPath);
    }
    
    public async Task<IDbConnection> CreateAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        try
        {
            var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            
            // Configure connection for optimal performance
            await ConfigureConnectionAsync(connection, ct);
            
            _logger.LogTrace("SQLite connection created successfully");
            return connection;
        }
        catch (SqliteException ex)
        {
            _logger.LogError(ex, "Failed to create SQLite connection");
            throw new DatabaseConnectionException(
                "ACODE-DB-001",
                $"Failed to connect to SQLite database: {ex.Message}",
                ex);
        }
    }
    
    private async Task ConfigureConnectionAsync(SqliteConnection connection, CancellationToken ct)
    {
        // Enable WAL mode for better concurrency
        await ExecutePragmaAsync(connection, "journal_mode", "WAL", ct);
        
        // Set busy timeout
        var busyTimeout = _options.Local?.BusyTimeoutMs ?? 5000;
        await ExecutePragmaAsync(connection, "busy_timeout", busyTimeout.ToString(), ct);
        
        // Enable foreign keys
        await ExecutePragmaAsync(connection, "foreign_keys", "ON", ct);
        
        // Optimize for performance
        await ExecutePragmaAsync(connection, "synchronous", "NORMAL", ct);
        await ExecutePragmaAsync(connection, "temp_store", "MEMORY", ct);
        await ExecutePragmaAsync(connection, "mmap_size", "268435456", ct); // 256MB
    }
    
    private async Task ExecutePragmaAsync(
        SqliteConnection connection, 
        string pragma, 
        string value, 
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA {pragma}={value};";
        await cmd.ExecuteNonQueryAsync(ct);
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var data = new Dictionary<string, object>();
        
        try
        {
            // Check file exists
            var dbPath = new SqliteConnectionStringBuilder(_connectionString).DataSource;
            if (!File.Exists(dbPath))
            {
                return new HealthCheckResult(
                    HealthStatus.Unhealthy,
                    $"Database file not found: {dbPath}",
                    data);
            }
            
            data["path"] = dbPath;
            data["size_bytes"] = new FileInfo(dbPath).Length;
            
            // Test connection
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            
            // Run integrity check (quick version)
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA quick_check;";
            var result = await cmd.ExecuteScalarAsync(ct);
            
            if (result?.ToString() != "ok")
            {
                return new HealthCheckResult(
                    HealthStatus.Degraded,
                    $"Database integrity check returned: {result}",
                    data);
            }
            
            // Get WAL size
            var walPath = dbPath + "-wal";
            if (File.Exists(walPath))
            {
                data["wal_size_bytes"] = new FileInfo(walPath).Length;
            }
            
            return new HealthCheckResult(
                HealthStatus.Healthy,
                "SQLite database is healthy",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQLite health check failed");
            return new HealthCheckResult(
                HealthStatus.Unhealthy,
                $"Health check failed: {ex.Message}",
                data);
        }
    }
    
    public void Dispose()
    {
        _disposed = true;
    }
}
```

### PostgresConnectionFactory Implementation

```csharp
// src/Acode.Infrastructure/Database/PostgresConnectionFactory.cs
namespace Acode.Infrastructure.Database;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data;

public sealed class PostgresConnectionFactory : IConnectionFactory, IAsyncDisposable
{
    private readonly DatabaseOptions _options;
    private readonly ILogger<PostgresConnectionFactory> _logger;
    private readonly NpgsqlDataSource _dataSource;
    private bool _disposed;
    
    public DatabaseProvider Provider => DatabaseProvider.PostgreSQL;
    
    public PostgresConnectionFactory(
        IOptions<DatabaseOptions> options,
        ILogger<PostgresConnectionFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        var connectionString = BuildConnectionString();
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        
        // Configure the data source
        ConfigureDataSource(builder);
        
        _dataSource = builder.Build();
        _logger.LogDebug("PostgreSQL connection factory initialized");
    }
    
    private string BuildConnectionString()
    {
        var remote = _options.Remote 
            ?? throw new InvalidOperationException("Remote database configuration is required");
        
        // Use explicit connection string if provided
        if (!string.IsNullOrEmpty(remote.ConnectionString))
        {
            return ExpandEnvironmentVariables(remote.ConnectionString);
        }
        
        // Build from individual properties
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = remote.Host ?? "localhost",
            Port = remote.Port ?? 5432,
            Database = remote.Database ?? "agentdb",
            Username = ExpandEnvironmentVariables(remote.Username ?? ""),
            Password = ExpandEnvironmentVariables(remote.Password ?? ""),
            SslMode = remote.SslMode ?? SslMode.Prefer,
            Timeout = remote.ConnectionTimeoutSeconds ?? 30,
            CommandTimeout = remote.CommandTimeoutSeconds ?? 60
        };
        
        // Pool settings
        if (remote.Pool != null)
        {
            builder.MinPoolSize = remote.Pool.MinSize ?? 2;
            builder.MaxPoolSize = remote.Pool.MaxSize ?? 10;
            builder.ConnectionIdleLifetime = remote.Pool.IdleTimeout ?? 300;
            builder.ConnectionLifetime = remote.Pool.ConnectionLifetime ?? 3600;
        }
        
        return builder.ToString();
    }
    
    private static string ExpandEnvironmentVariables(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        
        // Replace ${VAR} or $VAR patterns
        return System.Text.RegularExpressions.Regex.Replace(
            value,
            @"\$\{?([A-Za-z_][A-Za-z0-9_]*)\}?",
            match => Environment.GetEnvironmentVariable(match.Groups[1].Value) ?? match.Value);
    }
    
    private void ConfigureDataSource(NpgsqlDataSourceBuilder builder)
    {
        // Add logging
        builder.UseLoggerFactory(LoggerFactory.Create(b => b.AddConsole()));
        
        // Configure JSON serialization if needed
        // builder.EnableDynamicJson();
    }
    
    public async Task<IDbConnection> CreateAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        try
        {
            var connection = await _dataSource.OpenConnectionAsync(ct);
            _logger.LogTrace("PostgreSQL connection acquired from pool");
            return connection;
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Failed to create PostgreSQL connection");
            throw new DatabaseConnectionException(
                "ACODE-DB-001",
                $"Failed to connect to PostgreSQL: {ex.Message}",
                ex);
        }
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var data = new Dictionary<string, object>();
        
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            
            // Simple connectivity test
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT 1;";
            await cmd.ExecuteScalarAsync(ct);
            
            // Get pool statistics
            var stats = _dataSource.Statistics;
            data["pool_size"] = stats.Total;
            data["pool_idle"] = stats.Idle;
            data["pool_busy"] = stats.Busy;
            
            // Get server version
            data["server_version"] = connection.ServerVersion;
            
            return new HealthCheckResult(
                HealthStatus.Healthy,
                "PostgreSQL connection is healthy",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PostgreSQL health check failed");
            return new HealthCheckResult(
                HealthStatus.Unhealthy,
                $"Health check failed: {ex.Message}",
                data);
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        
        await _dataSource.DisposeAsync();
    }
}
```

### MigrationRunner Implementation

```csharp
// src/Acode.Infrastructure/Migrations/MigrationRunner.cs
namespace Acode.Infrastructure.Migrations;

using Acode.Infrastructure.Database;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

public sealed class MigrationRunner : IMigrationRunner
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly MigrationOptions _options;
    private readonly ILogger<MigrationRunner> _logger;
    private readonly MigrationValidator _validator;
    private readonly IReadOnlyList<Migration> _embeddedMigrations;
    
    public MigrationRunner(
        IConnectionFactory connectionFactory,
        IOptions<MigrationOptions> options,
        ILogger<MigrationRunner> logger,
        MigrationValidator validator)
    {
        _connectionFactory = connectionFactory;
        _options = options.Value;
        _logger = logger;
        _validator = validator;
        _embeddedMigrations = LoadEmbeddedMigrations();
    }
    
    public async Task<MigrationResult> MigrateAsync(
        string? targetVersion = null,
        bool dryRun = false,
        CancellationToken ct = default)
    {
        var appliedCount = 0;
        var errors = new List<string>();
        
        await using var connection = await _connectionFactory.CreateAsync(ct);
        
        // Ensure sys_migrations table exists
        await EnsureMigrationTableAsync(connection, ct);
        
        // Get pending migrations
        var pending = await GetPendingMigrationsAsync(connection, ct);
        
        if (targetVersion != null)
        {
            pending = pending.Where(m => 
                string.Compare(m.Version, targetVersion, StringComparison.Ordinal) <= 0).ToList();
        }
        
        if (pending.Count == 0)
        {
            _logger.LogInformation("No pending migrations");
            return new MigrationResult(true, 0, errors);
        }
        
        _logger.LogInformation("Found {Count} pending migration(s)", pending.Count);
        
        foreach (var migration in pending.OrderBy(m => m.Version))
        {
            if (dryRun)
            {
                _logger.LogInformation("[DRY-RUN] Would apply: {Version}", migration.Version);
                continue;
            }
            
            try
            {
                // Validate migration content
                var validation = _validator.Validate(migration.Content);
                if (!validation.IsValid)
                {
                    errors.Add($"Migration {migration.Version} failed validation: {string.Join(", ", validation.Errors)}");
                    break;
                }
                
                await ApplyMigrationAsync(connection, migration, ct);
                appliedCount++;
                _logger.LogInformation("Applied migration: {Version}", migration.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply migration: {Version}", migration.Version);
                errors.Add($"Migration {migration.Version} failed: {ex.Message}");
                break;
            }
        }
        
        return new MigrationResult(errors.Count == 0, appliedCount, errors);
    }
    
    private async Task ApplyMigrationAsync(
        IDbConnection connection, 
        Migration migration, 
        CancellationToken ct)
    {
        using var transaction = connection.BeginTransaction();
        
        try
        {
            // Execute migration SQL
            await connection.ExecuteAsync(migration.Content, transaction: transaction);
            
            // Record in sys_migrations
            var checksum = ComputeChecksum(migration.Content);
            await connection.ExecuteAsync(
                @"INSERT INTO sys_migrations (version, checksum, applied_at) 
                  VALUES (@Version, @Checksum, @AppliedAt)",
                new 
                { 
                    migration.Version, 
                    Checksum = checksum, 
                    AppliedAt = DateTime.UtcNow.ToString("O") 
                },
                transaction: transaction);
            
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
    
    public async Task<MigrationResult> RollbackAsync(
        string? targetVersion = null,
        CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateAsync(ct);
        
        var applied = await GetAppliedMigrationsAsync(connection, ct);
        var toRollback = applied
            .OrderByDescending(m => m.Version)
            .Where(m => targetVersion == null || 
                        string.Compare(m.Version, targetVersion, StringComparison.Ordinal) > 0)
            .Take(targetVersion == null ? 1 : int.MaxValue)
            .ToList();
        
        if (toRollback.Count == 0)
        {
            _logger.LogInformation("No migrations to rollback");
            return new MigrationResult(true, 0, new List<string>());
        }
        
        var rolledBack = 0;
        var errors = new List<string>();
        
        foreach (var applied in toRollback)
        {
            var migration = _embeddedMigrations.FirstOrDefault(m => m.Version == applied.Version);
            if (migration?.RollbackContent == null)
            {
                errors.Add($"No rollback script found for {applied.Version}");
                break;
            }
            
            try
            {
                using var transaction = connection.BeginTransaction();
                await connection.ExecuteAsync(migration.RollbackContent, transaction: transaction);
                await connection.ExecuteAsync(
                    "DELETE FROM sys_migrations WHERE version = @Version",
                    new { applied.Version },
                    transaction: transaction);
                transaction.Commit();
                
                rolledBack++;
                _logger.LogInformation("Rolled back migration: {Version}", applied.Version);
            }
            catch (Exception ex)
            {
                errors.Add($"Rollback of {applied.Version} failed: {ex.Message}");
                break;
            }
        }
        
        return new MigrationResult(errors.Count == 0, rolledBack, errors);
    }
    
    private async Task EnsureMigrationTableAsync(IDbConnection connection, CancellationToken ct)
    {
        var sql = _connectionFactory.Provider == DatabaseProvider.SQLite
            ? @"CREATE TABLE IF NOT EXISTS sys_migrations (
                    version TEXT PRIMARY KEY NOT NULL,
                    checksum TEXT NOT NULL,
                    applied_at TEXT NOT NULL
                );"
            : @"CREATE TABLE IF NOT EXISTS sys_migrations (
                    version VARCHAR(255) PRIMARY KEY NOT NULL,
                    checksum VARCHAR(64) NOT NULL,
                    applied_at TIMESTAMPTZ NOT NULL
                );";
        
        await connection.ExecuteAsync(sql);
    }
    
    private async Task<IReadOnlyList<Migration>> GetPendingMigrationsAsync(
        IDbConnection connection, 
        CancellationToken ct)
    {
        var applied = await GetAppliedMigrationsAsync(connection, ct);
        var appliedVersions = applied.Select(m => m.Version).ToHashSet();
        
        return _embeddedMigrations
            .Where(m => !appliedVersions.Contains(m.Version))
            .ToList();
    }
    
    private async Task<IReadOnlyList<AppliedMigration>> GetAppliedMigrationsAsync(
        IDbConnection connection, 
        CancellationToken ct)
    {
        var sql = "SELECT version, checksum, applied_at FROM sys_migrations ORDER BY version";
        var results = await connection.QueryAsync<AppliedMigration>(sql);
        return results.ToList();
    }
    
    private IReadOnlyList<Migration> LoadEmbeddedMigrations()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var migrations = new List<Migration>();
        
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.Contains(".Migrations.Scripts.") && n.EndsWith(".sql"))
            .Where(n => !n.EndsWith("_down.sql"))
            .OrderBy(n => n);
        
        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) continue;
            
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            
            // Extract version from filename (e.g., "001_initial_schema.sql")
            var fileName = resourceName.Split('.').Reverse().Skip(1).First();
            var version = fileName;
            
            // Try to load rollback script
            var rollbackName = resourceName.Replace(".sql", "_down.sql");
            string? rollbackContent = null;
            
            using var rollbackStream = assembly.GetManifestResourceStream(rollbackName);
            if (rollbackStream != null)
            {
                using var rollbackReader = new StreamReader(rollbackStream);
                rollbackContent = rollbackReader.ReadToEnd();
            }
            
            migrations.Add(new Migration(version, content, rollbackContent));
        }
        
        return migrations;
    }
    
    private static string ComputeChecksum(string content)
    {
        var normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public record Migration(string Version, string Content, string? RollbackContent = null);
public record AppliedMigration(string Version, string Checksum, DateTime AppliedAt);
public record MigrationResult(bool Success, int AppliedCount, IReadOnlyList<string> Errors);
```

### DatabaseCommand CLI Implementation

```csharp
// src/Acode.Cli/Commands/DatabaseCommand.cs
namespace Acode.Cli.Commands;

using Acode.Infrastructure.Database;
using Acode.Infrastructure.Migrations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

[Description("Database management commands")]
public class DatabaseCommand : AsyncCommand<DatabaseCommand.Settings>
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly IMigrationRunner _migrationRunner;
    
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<action>")]
        [Description("Action: status, migrate, rollback, schema, backup, verify")]
        public string Action { get; set; } = "";
        
        [CommandOption("--verbose|-v")]
        [Description("Show verbose output")]
        public bool Verbose { get; set; }
        
        [CommandOption("--dry-run")]
        [Description("Show what would be done without making changes")]
        public bool DryRun { get; set; }
        
        [CommandOption("--to")]
        [Description("Target version for migrate/rollback")]
        public string? TargetVersion { get; set; }
        
        [CommandOption("--output|-o")]
        [Description("Output path for backup")]
        public string? OutputPath { get; set; }
        
        [CommandOption("--table")]
        [Description("Table name for schema command")]
        public string? TableName { get; set; }
    }
    
    public DatabaseCommand(
        IConnectionFactory connectionFactory,
        IMigrationRunner migrationRunner)
    {
        _connectionFactory = connectionFactory;
        _migrationRunner = migrationRunner;
    }
    
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        return settings.Action.ToLowerInvariant() switch
        {
            "status" => await ExecuteStatusAsync(settings),
            "migrate" => await ExecuteMigrateAsync(settings),
            "rollback" => await ExecuteRollbackAsync(settings),
            "schema" => await ExecuteSchemaAsync(settings),
            "backup" => await ExecuteBackupAsync(settings),
            "verify" => await ExecuteVerifyAsync(settings),
            _ => throw new InvalidOperationException($"Unknown action: {settings.Action}")
        };
    }
    
    private async Task<int> ExecuteStatusAsync(Settings settings)
    {
        var health = await _connectionFactory.CheckHealthAsync();
        
        var table = new Table();
        table.AddColumn("Property");
        table.AddColumn("Value");
        
        table.AddRow("Provider", _connectionFactory.Provider.ToString());
        table.AddRow("Status", health.Status == HealthStatus.Healthy 
            ? "[green]✓ Healthy[/]" 
            : $"[red]✗ {health.Status}[/]");
        table.AddRow("Description", health.Description);
        
        if (health.Data != null)
        {
            foreach (var (key, value) in health.Data)
            {
                table.AddRow(key, value?.ToString() ?? "N/A");
            }
        }
        
        AnsiConsole.Write(table);
        return health.Status == HealthStatus.Healthy ? 0 : 1;
    }
    
    private async Task<int> ExecuteMigrateAsync(Settings settings)
    {
        var result = await _migrationRunner.MigrateAsync(
            settings.TargetVersion,
            settings.DryRun);
        
        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Applied {result.AppliedCount} migration(s)");
            return 0;
        }
        
        foreach (var error in result.Errors)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] {error}");
        }
        return 1;
    }
    
    private async Task<int> ExecuteRollbackAsync(Settings settings)
    {
        if (!settings.DryRun)
        {
            var confirmed = AnsiConsole.Confirm("Are you sure you want to rollback?", false);
            if (!confirmed) return 0;
        }
        
        var result = await _migrationRunner.RollbackAsync(settings.TargetVersion);
        
        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Rolled back {result.AppliedCount} migration(s)");
            return 0;
        }
        
        foreach (var error in result.Errors)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] {error}");
        }
        return 1;
    }
    
    private async Task<int> ExecuteSchemaAsync(Settings settings)
    {
        // Implementation for schema display
        await using var connection = await _connectionFactory.CreateAsync();
        // Query and display schema...
        return 0;
    }
    
    private async Task<int> ExecuteBackupAsync(Settings settings)
    {
        // Implementation for backup
        return 0;
    }
    
    private async Task<int> ExecuteVerifyAsync(Settings settings)
    {
        // Implementation for verify
        return 0;
    }
}
```

### Error Codes

| Code | Meaning | HTTP Status |
|------|---------|-------------|
| ACODE-DB-001 | Connection failed - unable to establish database connection | 503 |
| ACODE-DB-002 | Migration failed - error during migration execution | 500 |
| ACODE-DB-003 | Transaction failed - error during transaction commit/rollback | 500 |
| ACODE-DB-004 | Database locked - resource busy, retry later | 423 |
| ACODE-DB-005 | Schema error - table/column not found | 500 |
| ACODE-DB-006 | Constraint violation - unique/FK/check constraint failed | 409 |
| ACODE-DB-007 | Timeout - operation exceeded time limit | 408 |
| ACODE-DB-008 | Pool exhausted - no connections available | 503 |
| ACODE-DB-009 | Checksum mismatch - migration tampering detected | 500 |
| ACODE-DB-010 | Validation failed - migration content validation error | 400 |

### Implementation Checklist

1. [ ] Create IConnectionFactory interface with CreateAsync and CheckHealthAsync
2. [ ] Implement SqliteConnectionFactory with WAL mode and busy timeout
3. [ ] Implement PostgresConnectionFactory with connection pooling
4. [ ] Create DatabaseOptions configuration model
5. [ ] Implement DatabaseException with error codes
6. [ ] Create IMigrationRunner interface
7. [ ] Implement MigrationRunner with checksum validation
8. [ ] Create MigrationValidator for content validation
9. [ ] Implement embedded migration loading
10. [ ] Add DatabaseHealthCheck for health endpoints
11. [ ] Create DatabaseCommand CLI handler
12. [ ] Implement status, migrate, rollback subcommands
13. [ ] Implement schema, backup, verify subcommands
14. [ ] Register services in DI container
15. [ ] Write unit tests for connection factories
16. [ ] Write unit tests for migration runner
17. [ ] Write integration tests with real databases
18. [ ] Add performance benchmarks

### Rollout Plan

| Phase | Components | Duration | Dependencies |
|-------|------------|----------|--------------|
| 1 | IConnectionFactory, SqliteConnectionFactory | 2 days | None |
| 2 | MigrationRunner, embedded migrations | 3 days | Phase 1 |
| 3 | Initial schema migrations (001-003) | 2 days | Phase 2 |
| 4 | DatabaseHealthCheck, health endpoints | 1 day | Phase 1 |
| 5 | DatabaseCommand CLI | 2 days | Phase 1-4 |
| 6 | PostgresConnectionFactory | 2 days | Phase 1 |
| 7 | Integration tests, E2E tests | 3 days | Phase 1-6 |

### Dependencies

```xml
<!-- Required NuGet packages -->
<ItemGroup>
  <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.0" />
  <PackageReference Include="Npgsql" Version="8.0.0" />
  <PackageReference Include="Dapper" Version="2.1.24" />
  <PackageReference Include="Spectre.Console.Cli" Version="0.48.0" />
</ItemGroup>
```

---

**End of Task 050 Specification**