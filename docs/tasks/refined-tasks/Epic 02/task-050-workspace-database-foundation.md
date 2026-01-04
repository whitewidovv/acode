# Task 050: Workspace Database Foundation

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 2 – Persistence + Reliability Core  
**Dependencies:** Task 000 (Project Structure), Task 002 (Config), Task 003 (CLI)  

---

## Description

Task 050 implements the workspace database foundation—the shared persistence layer used by run state, queue persistence, and chat history. This includes SQLite for local storage, a migration framework, and PostgreSQL connector configuration.

The workspace database is the persistence backbone of Acode. All stateful operations depend on it: session management, conversation history, approval records, sync queues. A solid foundation here enables reliable operation everywhere.

SQLite serves as the local database. It's embedded, zero-config, and reliable. The database file lives in `.agent/data/workspace.db`. WAL mode enables concurrent reads with writes. Busy timeouts handle contention.

The migration framework manages schema evolution. Each schema change is a versioned migration. Migrations run automatically on startup. Forward migrations apply changes. Rollback migrations undo them. Version tracking prevents duplicate application.

PostgreSQL serves as the remote database. It provides durability, cross-device access, and team features. Connection configuration is flexible: connection strings, environment variables, or config file. Connection pooling manages resources.

The database access layer abstracts storage details. Application code uses repository interfaces. Infrastructure provides SQLite and PostgreSQL implementations. Switching backends requires no application changes.

Connection management handles lifecycle. Connections are pooled and reused. Transactions scope multiple operations. Timeouts prevent hangs. Health checks detect problems.

The foundation provides building blocks for all persistence needs. Other tasks (049.a for conversations, 011.b for sessions) build on this foundation. Consistency across features comes from shared infrastructure.

Error handling is comprehensive. Connection failures, constraint violations, timeout errors all have specific handling. Retries for transient issues. Circuit breakers for cascading failures. Clear error messages for users.

The CLI provides database management commands. Check status. Run migrations. View schema. Backup data. These commands enable troubleshooting and maintenance.

Security considerations protect sensitive data. Connection strings can use environment variables. Secrets are never logged. Access control follows OS permissions.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Workspace DB | Per-workspace database |
| SQLite | Embedded local database |
| PostgreSQL | Remote database server |
| Migration | Schema version change |
| WAL | Write-Ahead Logging mode |
| Connection Pool | Reusable connections |
| Transaction | Atomic operation scope |
| Repository | Data access abstraction |
| Schema | Database structure |
| Rollback | Undo migration |
| Busy Timeout | Wait for lock |
| Connection String | Database address |
| Health Check | Verify database working |
| Constraint | Data rule |
| Circuit Breaker | Failure protection |

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

- FR-001: Database MUST be in .agent/data/
- FR-002: Filename MUST be workspace.db
- FR-003: WAL mode MUST be enabled
- FR-004: Busy timeout MUST be configurable
- FR-005: Default timeout: 5 seconds

### PostgreSQL Connection

- FR-006: Connection string MUST work
- FR-007: Environment variable MUST work
- FR-008: Config file MUST work
- FR-009: Pool size MUST be configurable
- FR-010: Default pool: 10 connections

### Migration Framework

- FR-011: Migrations MUST be versioned
- FR-012: Version format: NNN_name
- FR-013: Migrations MUST be idempotent
- FR-014: Version table MUST exist
- FR-015: Applied versions MUST be tracked

### Migration Execution

- FR-016: Auto-run on startup MUST work
- FR-017: Manual run MUST work
- FR-018: Forward migration MUST work
- FR-019: Rollback migration MUST work
- FR-020: Dry-run MUST be supported

### Migration Safety

- FR-021: Failed migration MUST rollback
- FR-022: Partial application MUST NOT occur
- FR-023: Backup before MUST be optional
- FR-024: Lock during migration MUST prevent concurrent

### Connection Lifecycle

- FR-025: Connection MUST be pooled
- FR-026: Connection MUST be disposed
- FR-027: Timeout MUST be configurable
- FR-028: Default timeout: 30 seconds

### Transactions

- FR-029: Transaction scope MUST work
- FR-030: Commit MUST be explicit
- FR-031: Rollback MUST work
- FR-032: Nested transactions MUST NOT occur

### Error Handling

- FR-033: Connection error MUST retry
- FR-034: Constraint error MUST throw
- FR-035: Timeout error MUST retry
- FR-036: All errors MUST log

### Health Checks

- FR-037: SQLite health MUST check file access
- FR-038: PostgreSQL health MUST check connection
- FR-039: Health MUST return status
- FR-040: Unhealthy MUST log

### CLI Commands

- FR-041: `acode db status` MUST work
- FR-042: `acode db migrate` MUST work
- FR-043: `acode db rollback` MUST work
- FR-044: `acode db schema` MUST work
- FR-045: `acode db backup` MUST work

### Configuration

- FR-046: Config MUST support local path
- FR-047: Config MUST support remote URL
- FR-048: Config MUST support pool settings
- FR-049: Config MUST support timeouts

---

## Non-Functional Requirements

### Performance

- NFR-001: Connection acquire < 10ms
- NFR-002: Simple query < 50ms
- NFR-003: Migration < 30s per step

### Reliability

- NFR-004: ACID compliance
- NFR-005: Crash-safe WAL
- NFR-006: No data loss

### Security

- NFR-007: Connection string secrets
- NFR-008: No secrets in logs
- NFR-009: OS file permissions

### Compatibility

- NFR-010: SQLite 3.35+
- NFR-011: PostgreSQL 14+
- NFR-012: .NET 8+

### Maintainability

- NFR-013: Clear migration naming
- NFR-014: Schema documentation
- NFR-015: Version history

---

## User Manual Documentation

### Overview

The workspace database provides persistent storage for all Acode operations. SQLite handles local storage; PostgreSQL handles remote sync.

### Database Location

```
.agent/
├── data/
│   ├── workspace.db      # Main database
│   ├── workspace.db-wal  # Write-ahead log
│   └── workspace.db-shm  # Shared memory
```

### Configuration

```yaml
# .agent/config.yml
database:
  # Local SQLite settings
  local:
    path: .agent/data/workspace.db
    wal_mode: true
    busy_timeout_ms: 5000
    
  # Remote PostgreSQL settings
  remote:
    enabled: true
    connection_string: ${ACODE_PG_CONNECTION}
    # Or specify components:
    # host: localhost
    # port: 5432
    # database: acode
    # username: ${ACODE_PG_USER}
    # password: ${ACODE_PG_PASSWORD}
    
    pool:
      min_size: 2
      max_size: 10
      
    timeouts:
      connect_seconds: 5
      command_seconds: 30
```

### CLI Commands

```bash
# Check database status
$ acode db status

Database Status
────────────────────────────────────
Local (SQLite):
  Path: .agent/data/workspace.db
  Size: 4.2 MB
  Status: Healthy
  WAL Size: 128 KB
  
Remote (PostgreSQL):
  Status: Connected
  Latency: 23ms
  Pool: 2/10 connections

Migrations:
  Applied: 5
  Pending: 0
  Latest: 005_add_approval_records (2024-01-15)
```

```bash
# Run pending migrations
$ acode db migrate

Checking migrations...
Applied: 3 migrations
Pending: 2 migrations

Applying 004_add_sync_status...
  ✓ Added sync_status column to chats
  ✓ Added sync_status column to messages
  
Applying 005_add_approval_records...
  ✓ Created approval_records table
  ✓ Added indexes

All migrations applied.
```

```bash
# Rollback last migration
$ acode db rollback

Rolling back 005_add_approval_records...
  ✓ Dropped approval_records table

Rollback complete.
```

```bash
# View current schema
$ acode db schema

Tables:
────────────────────────────────────
chats (7 columns)
  id          TEXT PRIMARY KEY
  title       TEXT NOT NULL
  tags        TEXT
  worktree_id TEXT
  is_deleted  INTEGER
  created_at  TEXT
  updated_at  TEXT

runs (6 columns)
  id          TEXT PRIMARY KEY
  chat_id     TEXT REFERENCES chats
  ...

messages (6 columns)
  ...
```

```bash
# Backup database
$ acode db backup

Creating backup...
Backup saved: .agent/backups/workspace_2024-01-15_100000.db
Size: 4.2 MB
```

### Migration Files

```
migrations/
├── 001_initial_schema.sql
├── 001_initial_schema_down.sql
├── 002_add_chats.sql
├── 002_add_chats_down.sql
├── 003_add_messages.sql
├── 003_add_messages_down.sql
├── 004_add_sync_status.sql
├── 004_add_sync_status_down.sql
└── 005_add_approval_records.sql
└── 005_add_approval_records_down.sql
```

### Troubleshooting

#### Database Locked

**Problem:** SQLite database is locked

**Solutions:**
1. Wait—another process is writing
2. Increase busy_timeout
3. Check for hung processes

#### Connection Failed

**Problem:** Cannot connect to PostgreSQL

**Solutions:**
1. Check connection string
2. Verify network access
3. Check credentials

#### Migration Failed

**Problem:** Migration fails partway

**Solutions:**
1. Check error message
2. Database auto-rolled-back
3. Fix migration and retry

#### Database Corrupted

**Problem:** SQLite file corrupted

**Solutions:**
1. Restore from backup
2. Run `sqlite3 workspace.db ".recover"`
3. Rebuild from sync

---

## Acceptance Criteria

### SQLite

- [ ] AC-001: Database created
- [ ] AC-002: WAL enabled
- [ ] AC-003: Busy timeout works
- [ ] AC-004: Concurrent access works

### PostgreSQL

- [ ] AC-005: Connection works
- [ ] AC-006: Pool works
- [ ] AC-007: Timeout works

### Migrations

- [ ] AC-008: Auto-run works
- [ ] AC-009: Manual run works
- [ ] AC-010: Rollback works
- [ ] AC-011: Tracking works

### Transactions

- [ ] AC-012: Commit works
- [ ] AC-013: Rollback works
- [ ] AC-014: Scope works

### Health

- [ ] AC-015: SQLite health works
- [ ] AC-016: PostgreSQL health works

### CLI

- [ ] AC-017: Status works
- [ ] AC-018: Migrate works
- [ ] AC-019: Rollback works
- [ ] AC-020: Backup works

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

### Issue: Database file locked (SQLite)

**Symptoms:** Error "database is locked" during operations

**Causes:**
- Multiple agent processes accessing same database
- Long-running transaction holding lock
- Crashed process left lock file

**Solutions:**
1. Check for other agent processes: `Get-Process | Where-Object { $_.Name -match 'agent' }`
2. Look for stale lock file: `.agent/data/workspace.db-wal` and delete if process not running
3. Enable WAL mode if not already: `PRAGMA journal_mode=WAL;`

### Issue: Migration checksum mismatch

**Symptoms:** Migration fails with "checksum validation failed" error

**Causes:**
- Migration file was edited after being applied
- Different file encoding (CRLF vs LF)
- Embedded resource not updated after source change

**Solutions:**
1. Compare file hash with sys_migrations.checksum value
2. Normalize line endings and re-embed resource
3. Use `--force` flag if change is intentional (dangerous)

### Issue: PostgreSQL connection timeout

**Symptoms:** Operations hang then fail with timeout

**Causes:**
- Firewall blocking port 5432
- PostgreSQL not accepting connections
- Connection pool exhausted

**Solutions:**
1. Test connectivity: `Test-NetConnection -ComputerName host -Port 5432`
2. Check pg_hba.conf allows connections from agent host
3. Increase pool size in connection string or investigate connection leaks

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Database/
├── SqliteConnectionTests.cs
│   ├── Should_Create_Database()
│   ├── Should_Enable_Wal()
│   └── Should_Handle_BusyTimeout()
│
├── PostgresConnectionTests.cs
│   ├── Should_Parse_ConnectionString()
│   └── Should_Pool_Connections()
│
└── MigrationTests.cs
    ├── Should_Track_Versions()
    ├── Should_Apply_Forward()
    └── Should_Apply_Rollback()
```

### Integration Tests

```
Tests/Integration/Database/
├── SqliteIntegrationTests.cs
│   ├── Should_Write_And_Read()
│   └── Should_Handle_Concurrent()
│
└── MigrationRunnerTests.cs
    ├── Should_Apply_All_Pending()
    └── Should_Rollback_On_Failure()
```

### E2E Tests

```
Tests/E2E/Database/
├── DatabaseE2ETests.cs
│   ├── Should_Initialize_On_First_Run()
│   └── Should_Migrate_On_Upgrade()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Connection acquire | 5ms | 10ms |
| Simple insert | 5ms | 50ms |
| Simple query | 2ms | 50ms |
| Migration step | 5s | 30s |

---

## User Verification Steps

### Scenario 1: First Run

1. Delete .agent/data/
2. Run `acode run "test"`
3. Verify: Database created

### Scenario 2: Migration

1. Add new migration
2. Run `acode db migrate`
3. Verify: Schema updated

### Scenario 3: Rollback

1. Apply migration
2. Run `acode db rollback`
3. Verify: Migration reversed

### Scenario 4: Status

1. Run `acode db status`
2. Verify: Shows database info

### Scenario 5: Backup

1. Run `acode db backup`
2. Verify: Backup file created

### Scenario 6: PostgreSQL

1. Configure remote
2. Run `acode db status`
3. Verify: Connection shown

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/
├── Persistence/
│   ├── Database/
│   │   ├── SqliteConnectionFactory.cs
│   │   ├── PostgresConnectionFactory.cs
│   │   ├── IConnectionFactory.cs
│   │   └── DatabaseHealthCheck.cs
│   └── Migrations/
│       ├── MigrationRunner.cs
│       ├── Migration.cs
│       └── Migrations/
│           ├── 001_InitialSchema.cs
│           └── ...
│
src/AgenticCoder.Application/
├── Database/
│   └── IDatabaseService.cs
│
src/AgenticCoder.CLI/
└── Commands/
    └── DatabaseCommand.cs
```

### IConnectionFactory Interface

```csharp
namespace AgenticCoder.Infrastructure.Persistence.Database;

public interface IConnectionFactory
{
    Task<IDbConnection> CreateAsync(CancellationToken ct);
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct);
}
```

### SqliteConnectionFactory

```csharp
namespace AgenticCoder.Infrastructure.Persistence.Database;

public sealed class SqliteConnectionFactory : IConnectionFactory
{
    private readonly string _connectionString;
    
    public SqliteConnectionFactory(DatabaseOptions options)
    {
        var path = options.Local.Path;
        _connectionString = $"Data Source={path};Mode=ReadWriteCreate";
    }
    
    public async Task<IDbConnection> CreateAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        
        // Enable WAL mode
        await connection.ExecuteAsync("PRAGMA journal_mode=WAL;");
        await connection.ExecuteAsync($"PRAGMA busy_timeout={_options.BusyTimeoutMs};");
        
        return connection;
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-DB-001 | Connection failed |
| ACODE-DB-002 | Migration failed |
| ACODE-DB-003 | Transaction failed |
| ACODE-DB-004 | Database locked |
| ACODE-DB-005 | Schema error |

### Implementation Checklist

1. [ ] Create connection factory interface
2. [ ] Implement SQLite factory
3. [ ] Implement PostgreSQL factory
4. [ ] Create migration framework
5. [ ] Implement migration runner
6. [ ] Create initial migrations
7. [ ] Add health checks
8. [ ] Add CLI commands
9. [ ] Write unit tests
10. [ ] Write integration tests

### Rollout Plan

1. **Phase 1:** Connection factories
2. **Phase 2:** Migration framework
3. **Phase 3:** Initial migrations
4. **Phase 4:** Health checks
5. **Phase 5:** CLI commands
6. **Phase 6:** PostgreSQL support

---

**End of Task 050 Specification**