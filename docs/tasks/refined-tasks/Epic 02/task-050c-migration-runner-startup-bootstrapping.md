# Task 050.c: Migration Runner CLI + Startup Bootstrapping

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – Persistence + Reliability Core  
**Dependencies:** Task 050 (Database Foundation), Task 050.a (Layout), Task 050.b (Access Layer)  

---

## Description

Task 050.c implements the migration runner and startup bootstrapping. The migration runner applies schema changes safely. Startup bootstrapping ensures the database is ready before other operations begin.

The migration runner is the engine that applies schema changes. It reads migration files, tracks applied versions, and executes pending migrations. Forward migrations add features. Rollback migrations undo them.

Startup bootstrapping runs automatically. On every startup, the system checks for pending migrations. Pending migrations are applied automatically. This ensures the database matches the code version.

The CLI provides manual control. Developers can run migrations explicitly. They can rollback when needed. They can check status before deployment. They can create new migration files.

Migration execution is atomic. Each migration runs in a transaction. If any step fails, the whole migration rolls back. No partial migrations occur. The database stays consistent.

Version tracking prevents duplicate application. The __migrations table records what's applied. Checksums detect if migrations were modified. Applied migrations are never re-run.

Locking prevents concurrent migrations. Only one process can migrate at a time. Other processes wait or fail fast. This prevents corruption from parallel migrations.

Dry-run mode enables safe verification. Show what would happen without actually changing. Verify migration order. Catch errors before they affect real data.

The runner supports both SQLite and PostgreSQL. Migrations can be database-specific or shared. Dialect markers identify which database a migration targets.

Backup integration provides safety. Before migrations, an optional backup runs. If migration fails, restore is possible. Backup is configurable (on/off, location).

Logging captures migration history. Each migration logs start, progress, and completion. Errors are logged with full details. Audit trails support debugging.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Migration Runner | Executes migrations |
| Bootstrapping | Startup initialization |
| Pending | Not yet applied |
| Applied | Already executed |
| Rollback | Undo migration |
| Version Table | Tracks applied migrations |
| Checksum | Integrity hash |
| Lock | Prevent concurrent |
| Dry-Run | Preview without change |
| Atomic | All or nothing |
| Dialect | Database-specific |
| Backup | Pre-migration copy |
| Embedded | In assembly |
| File-Based | On disk |
| Idempotent | Safe to repeat |

---

## Out of Scope

The following items are explicitly excluded from Task 050.c:

- **Schema design** - Task 050.a
- **Connection layer** - Task 050.b
- **Health checks** - Task 050.d
- **Backup export** - Task 050.e
- **Data migrations** - Schema only
- **Seeding** - No default data
- **Squashing** - No merge old migrations
- **Branching** - Linear versions only
- **Remote execution** - Local only
- **GUI tools** - CLI only

---

## Assumptions

### Technical Assumptions

1. **Startup Mode Flag** - agent-config.yml contains database.autoMigrate boolean for startup behavior
2. **CLI Override** - --migrate and --no-migrate flags override config setting
3. **Embedded Discovery** - Assembly scanning finds migration classes/resources automatically
4. **File Discovery** - .agent/migrations/*.sql scanned for external migration files
5. **Order Guarantee** - Migrations applied in strict numeric order (001 before 002)
6. **Transaction Isolation** - Each migration in separate transaction; failures don't corrupt state
7. **Checksum Validation** - Applied migration checksums verified to detect tampering

### Bootstrapping Assumptions

8. **First Run Detection** - Missing workspace.db or empty sys_migrations indicates fresh install
9. **Version Gap Detection** - Missing intermediate versions prevent migration (fail-safe)
10. **Concurrent Guard** - Lock file or database lock prevents concurrent migration runs
11. **Startup Blocking** - Agent waits for migrations before serving requests
12. **Failure Exit Code** - Migration failures return non-zero exit for scripting

### Operational Assumptions

13. **Dry Run Mode** - --dry-run shows pending migrations without applying
14. **Status Command** - `agent db status` shows current version and pending migrations
15. **Force Option** - --force skips checksum validation (dangerous, requires confirmation)
16. **Backup Prompt** - Migrations prompt for backup on production databases
17. **Rollback Scripting** - Rollback generates scripts but doesn't auto-execute without confirmation
18. **Logging Verbosity** - Migration progress logged with timing for each step

---

## Functional Requirements

### Migration Discovery

- FR-001: Embedded migrations MUST work
- FR-002: File-based migrations MUST work
- FR-003: Migration naming: NNN_description
- FR-004: Up/down scripts MUST pair
- FR-005: Missing down MUST warn

### Version Table

- FR-006: __migrations MUST exist
- FR-007: Auto-create if missing
- FR-008: Columns: version, applied_at, checksum
- FR-009: Version MUST be unique
- FR-010: Checksum MUST be SHA-256

### Migration Execution

- FR-011: Execute in version order
- FR-012: Each migration in transaction
- FR-013: Failed migration MUST rollback
- FR-014: Success MUST record in version table
- FR-015: Logging MUST occur

### Rollback Execution

- FR-016: Rollback MUST use down script
- FR-017: Rollback MUST be in reverse order
- FR-018: Rollback MUST remove version record
- FR-019: Failed rollback MUST log

### Startup Bootstrap

- FR-020: Run on every startup
- FR-021: Check pending migrations
- FR-022: Apply automatically if enabled
- FR-023: Block startup until complete
- FR-024: Log migration activity

### CLI Commands

- FR-025: `acode db migrate` MUST work
- FR-026: `acode db rollback` MUST work
- FR-027: `acode db status` MUST work
- FR-028: `acode db create` MUST work
- FR-029: `acode db list` MUST work

### Migrate Command

- FR-030: Apply all pending
- FR-031: `--to <version>` partial apply
- FR-032: `--dry-run` preview
- FR-033: `--force` skip confirmation

### Rollback Command

- FR-034: Rollback last by default
- FR-035: `--steps <n>` rollback n
- FR-036: `--to <version>` rollback to
- FR-037: `--dry-run` preview

### Status Command

- FR-038: Show applied migrations
- FR-039: Show pending migrations
- FR-040: Show current version
- FR-041: Show last applied time

### Create Command

- FR-042: Generate migration files
- FR-043: Auto-generate version number
- FR-044: Create up and down files
- FR-045: Add template content

### Locking

- FR-046: Acquire lock before migrate
- FR-047: Release lock after migrate
- FR-048: Lock timeout: 60 seconds
- FR-049: Lock conflict MUST error

### Checksum Validation

- FR-050: Compute checksum on apply
- FR-051: Store checksum in version table
- FR-052: Validate on startup
- FR-053: Mismatch MUST warn

### Backup Integration

- FR-054: Optional backup before migrate
- FR-055: Backup location configurable
- FR-056: Backup MUST succeed before migrate
- FR-057: Failed backup MUST abort

---

## Non-Functional Requirements

### Performance

- NFR-001: Startup check < 100ms
- NFR-002: Migration < 30s each
- NFR-003: Status < 50ms

### Reliability

- NFR-004: Atomic migrations
- NFR-005: No partial state
- NFR-006: Safe rollback

### Safety

- NFR-007: Locking prevents corruption
- NFR-008: Checksums detect tampering
- NFR-009: Backup enables recovery

### Usability

- NFR-010: Clear status output
- NFR-011: Helpful error messages
- NFR-012: Dry-run for safety

---

## User Manual Documentation

### Overview

The migration runner manages database schema changes. Migrations run automatically on startup and can be controlled via CLI.

### Automatic Startup

```bash
$ acode run "Hello"
Checking migrations...
Applied: 5 migrations
Pending: 1 migration

Applying 006_add_feature_flags...
  ✓ Created feature_flags table
  ✓ Added indexes

Starting agent...
```

### CLI Commands

```bash
# Check migration status
$ acode db status

Migration Status
────────────────────────────────────
Current Version: 005_add_sync_status
Applied: 5 migrations
Pending: 1 migration

Applied:
  001_initial_schema      (2024-01-01 10:00)
  002_add_conversations   (2024-01-01 10:00)
  003_add_sessions        (2024-01-01 10:00)
  004_add_approvals       (2024-01-15 09:00)
  005_add_sync_status     (2024-01-15 09:00)

Pending:
  006_add_feature_flags
```

```bash
# Apply pending migrations
$ acode db migrate

Applying migrations...
  006_add_feature_flags...
    ✓ Created feature_flags table
    ✓ Added indexes
    
All migrations applied.
```

```bash
# Dry-run (preview)
$ acode db migrate --dry-run

Would apply:
  006_add_feature_flags
    - CREATE TABLE feature_flags (...)
    - CREATE INDEX idx_feature_flags_key (...)
    
No changes made.
```

```bash
# Rollback last migration
$ acode db rollback

Rolling back 006_add_feature_flags...
  ✓ Dropped indexes
  ✓ Dropped table
  
Rollback complete.
```

```bash
# Rollback multiple
$ acode db rollback --steps 2

Rolling back 006_add_feature_flags...
  ✓ Complete
  
Rolling back 005_add_sync_status...
  ✓ Complete

Rolled back 2 migrations.
```

```bash
# Create new migration
$ acode db create add_analytics

Created migration:
  migrations/007_add_analytics.sql
  migrations/007_add_analytics_down.sql

Edit the files and run: acode db migrate
```

### Configuration

```yaml
# .agent/config.yml
migrations:
  # Auto-apply on startup
  auto_apply: true
  
  # Backup before migrations
  backup:
    enabled: true
    path: .agent/backups/
    
  # Lock settings
  lock:
    timeout_seconds: 60
    
  # Checksum validation
  validate_checksums: true
```

### Migration File Template

```sql
-- migrations/007_add_analytics.sql
-- 
-- Purpose: Add analytics tracking tables
-- Dependencies: 001_initial_schema
-- Author: developer
-- Date: 2024-01-20

CREATE TABLE IF NOT EXISTS sys_analytics (
    id TEXT PRIMARY KEY,
    event_type TEXT NOT NULL,
    payload TEXT,  -- JSON
    created_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_sys_analytics_type 
    ON sys_analytics(event_type);
CREATE INDEX IF NOT EXISTS idx_sys_analytics_created 
    ON sys_analytics(created_at);
```

```sql
-- migrations/007_add_analytics_down.sql
-- 
-- Rollback: Remove analytics tables

DROP INDEX IF EXISTS idx_sys_analytics_created;
DROP INDEX IF EXISTS idx_sys_analytics_type;
DROP TABLE IF EXISTS sys_analytics;
```

### Troubleshooting

#### Migration Lock Timeout

**Problem:** Another process is migrating

**Solutions:**
1. Wait for other process
2. Increase lock timeout
3. Check for stale locks: `acode db unlock --force`

#### Checksum Mismatch

**Problem:** Migration file changed after apply

**Solutions:**
1. Restore original file
2. Or reset checksum: `acode db reset-checksum 005`

#### Rollback Failed

**Problem:** Down script has errors

**Solutions:**
1. Fix down script
2. Apply manually
3. Update version table

---

## Acceptance Criteria

### Discovery

- [ ] AC-001: Finds migrations
- [ ] AC-002: Ordered correctly
- [ ] AC-003: Pairs up/down

### Version Table

- [ ] AC-004: Auto-creates
- [ ] AC-005: Tracks applied
- [ ] AC-006: Stores checksum

### Execution

- [ ] AC-007: Applies pending
- [ ] AC-008: Atomic transaction
- [ ] AC-009: Rollback on fail

### Rollback

- [ ] AC-010: Runs down script
- [ ] AC-011: Updates version table
- [ ] AC-012: Reverse order

### Startup

- [ ] AC-013: Auto-runs
- [ ] AC-014: Blocks until complete
- [ ] AC-015: Logs activity

### CLI

- [ ] AC-016: Status works
- [ ] AC-017: Migrate works
- [ ] AC-018: Rollback works
- [ ] AC-019: Create works
- [ ] AC-020: Dry-run works

### Locking

- [ ] AC-021: Acquires lock
- [ ] AC-022: Releases lock
- [ ] AC-023: Handles conflict

---

## Best Practices

### Migration Execution

1. **Always backup first** - Run `agent db backup` before any migration
2. **Verify in staging** - Apply migrations to non-production environment first
3. **Use dry-run mode** - Preview changes with `--dry-run` before applying
4. **Monitor progress** - Watch migration output for warnings or slow steps

### Startup Configuration

5. **Explicit over implicit** - Prefer explicit `agent db migrate` over auto-migrate at startup
6. **Fail-fast startup** - Application should not start if database is in unknown state
7. **Log startup sequence** - Record migration status check and outcome at startup
8. **Version check on connect** - Validate schema version matches expected on first connection

### Safety Measures

9. **Never force in production** - Avoid `--force` flag; fix underlying issue instead
10. **Lock during migration** - Prevent concurrent migrations with file or database lock
11. **Rollback plan ready** - Have tested rollback procedure before applying migrations
12. **Health check after migration** - Run `agent db health` after migration completes

---

## Troubleshooting

### Issue: Migration lock timeout

**Symptoms:** "Could not acquire migration lock" error

**Causes:**
- Previous migration crashed without releasing lock
- Another migration process running
- Lock file permissions issue

**Solutions:**
1. Check for other agent processes running migrations
2. Remove stale lock file: `.agent/data/.migration-lock`
3. Inspect sys_migrations for partially applied migration

### Issue: Auto-migrate fails at startup

**Symptoms:** Application exits with migration error before serving

**Causes:**
- Pending migrations require manual intervention
- Database connection configuration wrong
- Migration file corrupt or missing

**Solutions:**
1. Run `agent db status` to see pending migrations
2. Apply migrations manually with `agent db migrate`
3. Set `database.autoMigrate: false` and handle manually

### Issue: Inconsistent state after failed migration

**Symptoms:** sys_migrations shows partial version, schema incomplete

**Causes:**
- DDL statement failed mid-migration
- Transaction not properly rolled back (DDL in PostgreSQL)
- Power failure or crash during migration

**Solutions:**
1. Check sys_migrations for last applied version
2. Manually inspect schema for partial changes
3. Create recovery migration to fix schema state
4. Consider restoring from pre-migration backup

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Migrations/
├── MigrationDiscoveryTests.cs
│   ├── Should_Find_Embedded()
│   ├── Should_Find_FileBased()
│   └── Should_Order_By_Version()
│
├── MigrationRunnerTests.cs
│   ├── Should_Apply_Pending()
│   ├── Should_Rollback_On_Failure()
│   └── Should_Track_Version()
│
├── ChecksumTests.cs
│   ├── Should_Compute_Checksum()
│   └── Should_Detect_Mismatch()
│
└── LockingTests.cs
    ├── Should_Acquire_Lock()
    └── Should_Timeout()
```

### Integration Tests

```
Tests/Integration/Migrations/
├── MigrationRunnerIntegrationTests.cs
│   ├── Should_Apply_All_Pending()
│   ├── Should_Rollback_Steps()
│   └── Should_Handle_Concurrent()
```

### E2E Tests

```
Tests/E2E/Migrations/
├── MigrationE2ETests.cs
│   ├── Should_Bootstrap_On_Startup()
│   └── Should_Create_And_Apply()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Status check | 25ms | 50ms |
| Single migration | 5s | 30s |
| Startup check | 50ms | 100ms |

---

## User Verification Steps

### Scenario 1: Auto Bootstrap

1. Add new migration
2. Start acode
3. Verify: Migration applied

### Scenario 2: Manual Migrate

1. Add migration
2. Run `acode db migrate`
3. Verify: Applied and logged

### Scenario 3: Rollback

1. Apply migration
2. Run `acode db rollback`
3. Verify: Reverted

### Scenario 4: Dry Run

1. Add migration
2. Run `acode db migrate --dry-run`
3. Verify: Preview shown, not applied

### Scenario 5: Create

1. Run `acode db create test_feature`
2. Verify: Files created

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/
├── Persistence/
│   └── Migrations/
│       ├── MigrationRunner.cs
│       ├── MigrationDiscovery.cs
│       ├── MigrationExecutor.cs
│       ├── MigrationLock.cs
│       └── ChecksumValidator.cs
│
src/AgenticCoder.Application/
├── Database/
│   └── IMigrationService.cs
│
src/AgenticCoder.CLI/
└── Commands/
    └── DbMigrateCommand.cs
```

### IMigrationService Interface

```csharp
namespace AgenticCoder.Application.Database;

public interface IMigrationService
{
    Task<MigrationStatus> GetStatusAsync(CancellationToken ct);
    Task<MigrationResult> MigrateAsync(MigrateOptions options, CancellationToken ct);
    Task<MigrationResult> RollbackAsync(RollbackOptions options, CancellationToken ct);
    Task<string> CreateAsync(string name, CancellationToken ct);
}
```

### MigrationRunner

```csharp
namespace AgenticCoder.Infrastructure.Persistence.Migrations;

public sealed class MigrationRunner : IMigrationService
{
    public async Task<MigrationResult> MigrateAsync(
        MigrateOptions options,
        CancellationToken ct)
    {
        await using var @lock = await _lockService.AcquireAsync(ct);
        
        var pending = await _discovery.GetPendingAsync(ct);
        
        foreach (var migration in pending)
        {
            if (options.DryRun)
            {
                _logger.LogInformation("Would apply: {Version}", migration.Version);
                continue;
            }
            
            await _executor.ExecuteAsync(migration, ct);
            await _versionTable.RecordAsync(migration, ct);
        }
        
        return new MigrationResult(pending.Count);
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-MIG-001 | Migration failed |
| ACODE-MIG-002 | Lock timeout |
| ACODE-MIG-003 | Checksum mismatch |
| ACODE-MIG-004 | Missing down script |
| ACODE-MIG-005 | Rollback failed |

### Implementation Checklist

1. [ ] Create migration discovery
2. [ ] Create version table manager
3. [ ] Create migration executor
4. [ ] Create lock service
5. [ ] Create checksum validator
6. [ ] Create migration runner
7. [ ] Add startup bootstrap
8. [ ] Add CLI commands
9. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Discovery
2. **Phase 2:** Version table
3. **Phase 3:** Executor
4. **Phase 4:** Locking
5. **Phase 5:** CLI
6. **Phase 6:** Bootstrap

---

**End of Task 050.c Specification**