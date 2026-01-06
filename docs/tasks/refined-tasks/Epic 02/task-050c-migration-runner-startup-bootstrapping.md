# Task 050.c: Migration Runner CLI + Startup Bootstrapping

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – Persistence + Reliability Core  
**Dependencies:** Task 050 (Database Foundation), Task 050.a (Layout), Task 050.b (Access Layer)  

---

## Description

### Business Value and ROI

The migration runner and startup bootstrapping system provides **$186,000/year in savings** for a team of 10 developers through:

| Value Stream | Annual Savings | Calculation |
|--------------|----------------|-------------|
| Deployment automation | $72,000 | 3 hours/week manual DB work × 10 devs × 48 weeks × $50/hr |
| Schema consistency | $54,000 | 2 production incidents/quarter avoided × 4 quarters × $6,750/incident |
| Rollback capability | $36,000 | 1 failed deployment/quarter × 4 quarters × 6 hours recovery × 3 senior devs × $150/hr |
| Development velocity | $24,000 | 15 min/day saved per dev × 10 devs × 240 days × $50/hr ÷ 60 |
| **Total** | **$186,000** | |

**Key Metrics:**
- **Deployment time:** 45 minutes manual → 2 minutes automated (96% reduction)
- **Rollback time:** 2 hours manual → 30 seconds automated (99.6% reduction)
- **Migration failures:** 15% with manual process → 0.5% with atomic transactions (97% reduction)
- **Schema drift incidents:** 4 per quarter → 0 per quarter (100% elimination)

### Technical Architecture

The migration runner is built on three core subsystems that work together to provide safe, atomic schema evolution:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        MIGRATION RUNNER ARCHITECTURE                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────┐     ┌──────────────┐     ┌──────────────────────────────┐ │
│  │  DISCOVERY   │────▶│   EXECUTOR   │────▶│    VERSION TRACKING          │ │
│  │              │     │              │     │                              │ │
│  │ • Embedded   │     │ • Atomic     │     │ • __migrations table         │ │
│  │ • File-based │     │   txn wrap   │     │ • Checksum validation        │ │
│  │ • Ordering   │     │ • Rollback   │     │ • Applied timestamps         │ │
│  │ • Validation │     │   on fail    │     │ • Version gaps detected      │ │
│  └──────────────┘     └──────────────┘     └──────────────────────────────┘ │
│         │                    │                          │                    │
│         │                    │                          │                    │
│         ▼                    ▼                          ▼                    │
│  ┌──────────────────────────────────────────────────────────────────────────┐│
│  │                        CONCURRENCY CONTROL                                ││
│  │                                                                           ││
│  │  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐   ││
│  │  │   LOCK SERVICE   │    │  STARTUP GUARD   │    │   HEALTH CHECK   │   ││
│  │  │                  │    │                  │    │                  │   ││
│  │  │ • File lock      │    │ • Block startup  │    │ • Version verify │   ││
│  │  │ • DB advisory    │    │ • Timeout config │    │ • Checksum match │   ││
│  │  │ • 60s timeout    │    │ • Fail-fast mode │    │ • Gap detection  │   ││
│  │  └──────────────────┘    └──────────────────┘    └──────────────────┘   ││
│  └──────────────────────────────────────────────────────────────────────────┘│
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Migration Lifecycle

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         MIGRATION LIFECYCLE                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   CREATE                    APPLY                     VERIFY                 │
│   ──────                    ─────                     ──────                 │
│                                                                              │
│   ┌─────────────┐          ┌─────────────┐          ┌─────────────┐         │
│   │ db create   │          │ Acquire     │          │ Compute     │         │
│   │ <name>      │          │ Lock        │          │ Checksum    │         │
│   └──────┬──────┘          └──────┬──────┘          └──────┬──────┘         │
│          │                        │                        │                 │
│          ▼                        ▼                        ▼                 │
│   ┌─────────────┐          ┌─────────────┐          ┌─────────────┐         │
│   │ Generate    │          │ Begin       │          │ Compare to  │         │
│   │ Version #   │          │ Transaction │          │ Stored Hash │         │
│   └──────┬──────┘          └──────┬──────┘          └──────┬──────┘         │
│          │                        │                        │                 │
│          ▼                        ▼                        ▼                 │
│   ┌─────────────┐          ┌─────────────┐          ┌─────────────┐         │
│   │ Create Up   │          │ Execute     │          │ Detect      │         │
│   │ Script      │          │ DDL/DML     │          │ Tampering   │         │
│   └──────┬──────┘          └──────┬──────┘          └──────┬──────┘         │
│          │                        │                        │                 │
│          ▼                        │         ┌──────┬───────┴────────┐       │
│   ┌─────────────┐          ┌──────▼──────┐  │ PASS │    ┌───────────▼───┐   │
│   │ Create Down │          │ Success?    │──┴──────┴───▶│ Log Warning   │   │
│   │ Script      │          └──────┬──────┘              │ or Fail-Fast  │   │
│   └──────┬──────┘                 │ YES                 └───────────────┘   │
│          │                        ▼                                          │
│          ▼                 ┌─────────────┐                                   │
│   ┌─────────────┐          │ Record in   │                                   │
│   │ Add to      │          │ __migrations│                                   │
│   │ migrations/ │          └──────┬──────┘                                   │
│   └─────────────┘                 │                                          │
│                                   ▼                                          │
│                            ┌─────────────┐                                   │
│                            │ Commit      │                                   │
│                            │ Transaction │                                   │
│                            └──────┬──────┘                                   │
│                                   │                                          │
│                                   ▼                                          │
│                            ┌─────────────┐                                   │
│                            │ Release     │                                   │
│                            │ Lock        │                                   │
│                            └─────────────┘                                   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Startup Bootstrap Sequence

The startup bootstrap ensures database readiness before any other operations. This is critical for data integrity and prevents race conditions:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         STARTUP BOOTSTRAP SEQUENCE                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   APPLICATION START                                                          │
│         │                                                                    │
│         ▼                                                                    │
│   ┌─────────────────────────────────────────┐                               │
│   │ 1. Load Configuration                   │ ◀── agent-config.yml          │
│   │    • database.autoMigrate               │                               │
│   │    • database.local.path                │                               │
│   │    • migrations.backup.enabled          │                               │
│   └────────────────────┬────────────────────┘                               │
│                        │                                                     │
│                        ▼                                                     │
│   ┌─────────────────────────────────────────┐                               │
│   │ 2. Check Database Exists                │                               │
│   │    IF NOT EXISTS:                       │                               │
│   │      • Create database file             │                               │
│   │      • Create __migrations table        │                               │
│   └────────────────────┬────────────────────┘                               │
│                        │                                                     │
│                        ▼                                                     │
│   ┌─────────────────────────────────────────┐                               │
│   │ 3. Validate Checksum Integrity          │                               │
│   │    FOR EACH applied migration:          │                               │
│   │      • Load file, compute SHA-256       │                               │
│   │      • Compare to stored checksum       │                               │
│   │      • WARN or FAIL on mismatch         │                               │
│   └────────────────────┬────────────────────┘                               │
│                        │                                                     │
│                        ▼                                                     │
│   ┌─────────────────────────────────────────┐                               │
│   │ 4. Discover Pending Migrations          │                               │
│   │    • Scan embedded resources            │                               │
│   │    • Scan .agent/migrations/ folder     │                               │
│   │    • Sort by version number             │                               │
│   │    • Filter out already applied         │                               │
│   └────────────────────┬────────────────────┘                               │
│                        │                                                     │
│            ┌───────────┴───────────┐                                        │
│            │ Pending Count > 0?    │                                        │
│            └───────────┬───────────┘                                        │
│                        │                                                     │
│         ┌──── NO ──────┴────── YES ────┐                                    │
│         │                              │                                     │
│         ▼                              ▼                                     │
│  ┌──────────────┐           ┌─────────────────────────────────┐             │
│  │ 5a. Continue │           │ 5b. Check autoMigrate Config    │             │
│  │     Startup  │           └──────────────┬──────────────────┘             │
│  └──────────────┘                          │                                 │
│                              ┌─────────────┴─────────────┐                  │
│                              │ autoMigrate = true?       │                  │
│                              └─────────────┬─────────────┘                  │
│                                            │                                 │
│                         ┌─── NO ───────────┴───────── YES ───┐              │
│                         │                                    │               │
│                         ▼                                    ▼               │
│              ┌─────────────────────┐          ┌─────────────────────────┐   │
│              │ 6a. Block startup   │          │ 6b. Acquire Lock        │   │
│              │     with warning:   │          │     Apply all pending   │   │
│              │     "Pending        │          │     Release lock        │   │
│              │     migrations"     │          │     Continue startup    │   │
│              └─────────────────────┘          └─────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Integration Points

The migration runner integrates with several key subsystems:

| Integration Point | Description | Interface/Method |
|-------------------|-------------|------------------|
| **Connection Factory** | Obtains database connections for migration execution | `IConnectionFactory.CreateAsync()` |
| **Unit of Work** | Wraps each migration in atomic transaction | `IUnitOfWork.CommitAsync()`, `RollbackAsync()` |
| **Configuration** | Reads migration settings from config | `IOptions<MigrationOptions>` |
| **Logging** | Records migration progress and errors | `ILogger<MigrationRunner>` |
| **Backup Service** | Creates pre-migration backup when configured | `IBackupService.CreateAsync()` |
| **Health Check** | Reports migration status for diagnostics | `IMigrationHealthCheck.CheckAsync()` |
| **CLI Commands** | Exposes migration operations to users | `DbCommand` subcommands |
| **Host Lifecycle** | Blocks startup until migrations complete | `IHostedService.StartAsync()` |

### Constraints and Limitations

| Constraint | Description | Workaround |
|------------|-------------|------------|
| **Linear versions only** | No branched migration history supported | Use sequential version numbers, coordinate in teams |
| **SQLite DDL limitations** | Some ALTER TABLE operations not atomic in SQLite | Use CREATE/COPY/DROP pattern for table modifications |
| **Single database** | Cannot migrate multiple databases in single operation | Run separate migration commands for each database |
| **File-based or embedded** | No remote migration sources | Copy migrations locally before running |
| **60-second lock timeout** | Long migrations may cause lock contention | Configure longer timeout for known long migrations |
| **No data migrations** | Schema changes only, no data transformation | Use application-level data migration scripts |

### Trade-offs and Design Decisions

| Decision | Options Considered | Choice | Rationale |
|----------|-------------------|--------|-----------|
| **Migration format** | EF Core migrations vs raw SQL | Raw SQL | Direct SQL provides full control, avoids ORM abstraction leaks, works with both SQLite and PostgreSQL |
| **Version tracking** | File markers vs database table | Database table | Atomic with transaction, queryable, survives file system changes |
| **Locking mechanism** | File lock vs database advisory lock | Both (fallback) | File lock for SQLite, advisory lock for PostgreSQL, file as universal fallback |
| **Checksum algorithm** | MD5 vs SHA-256 | SHA-256 | More secure, no collision concerns, standard practice |
| **Rollback strategy** | Auto-rollback vs require confirmation | Require confirmation | Production safety, prevents accidental data loss |
| **Embedded vs file-based** | Embedded only vs file-based only vs both | Both | Embedded for version control, file-based for hotfixes |

### Error Handling Strategy

The migration runner uses a fail-fast approach with clear error codes:

| Error Code | Condition | Recovery Action |
|------------|-----------|-----------------|
| ACODE-MIG-001 | Migration execution failed | Check migration SQL, fix syntax, re-run |
| ACODE-MIG-002 | Lock acquisition timeout | Wait for other process, or force unlock if stale |
| ACODE-MIG-003 | Checksum mismatch detected | Restore original file or reset checksum with --force |
| ACODE-MIG-004 | Down script missing | Create down script before production deployment |
| ACODE-MIG-005 | Rollback execution failed | Manual intervention required, check schema state |
| ACODE-MIG-006 | Version gap detected | Apply missing intermediate migrations |
| ACODE-MIG-007 | Database connection failed | Check connection configuration, verify database running |
| ACODE-MIG-008 | Backup failed | Check disk space, verify backup path permissions |

---

## Use Cases

### Use Case 1: DevBot Automated Deployment Pipeline

**Persona:** DevBot, an AI developer assistant managing continuous deployment

**Scenario:** DevBot needs to deploy a new feature that requires database schema changes. The deployment must be zero-downtime with automatic rollback on failure.

**Before State (Manual Process):**
- DevBot generates SQL scripts and commits to repository
- Human operator reviews and manually applies migrations during maintenance window
- Maintenance window: 2 AM Saturday, 4-hour outage
- Manual rollback if issues detected (45 minutes average)
- Post-deployment verification requires manual schema comparison

**After State (Automated Migration Runner):**
```bash
$ acode db status
Migration Status: 2 pending migrations
  - 047_add_feature_flags
  - 048_add_analytics_tracking

$ acode db migrate --dry-run
Would apply:
  047_add_feature_flags (12 DDL statements)
  048_add_analytics_tracking (8 DDL statements)
No changes made.

$ acode db migrate
Acquiring migration lock...
Backing up database to .agent/backups/pre-047-20240115-143022.db
Applying 047_add_feature_flags...
  ✓ 12 statements executed (234ms)
  ✓ Checksum stored: sha256:a4f2c...
Applying 048_add_analytics_tracking...
  ✓ 8 statements executed (156ms)
  ✓ Checksum stored: sha256:b7e3d...
Lock released.
All migrations applied successfully.
```

**Quantified Improvement:**
- Deployment time: 4 hours → 2 minutes (97% reduction)
- Downtime: 4 hours → 0 (100% elimination)
- Rollback time: 45 minutes → 30 seconds (99% reduction)
- Human intervention: Required → Optional (full automation possible)
- **Annual savings:** $72,000 (elimination of 4-hour maintenance windows)

---

### Use Case 2: Jordan Team Collaboration with Migration Conflicts

**Persona:** Jordan, a senior developer leading a team of 5 engineers working on parallel features

**Scenario:** Multiple team members are creating database migrations simultaneously. Jordan needs to ensure migrations don't conflict and are applied in correct order across all development, staging, and production environments.

**Before State (Manual Coordination):**
- Team uses shared spreadsheet to track migration versions
- Conflicts discovered during code review (late in cycle)
- Manual renumbering required when conflicts found
- Production deployment requires synchronizing with all developers
- Average 2.5 hours per week spent on migration coordination
- 3 incidents per quarter due to migration order issues

**After State (Migration Runner with Version Tracking):**
```bash
# Developer A creates migration
$ acode db create add_user_preferences
Created: 049_add_user_preferences.sql
         049_add_user_preferences_down.sql
Next available version: 050

# Developer B (simultaneously) creates migration
$ acode db create add_notification_settings
Created: 050_add_notification_settings.sql
         050_add_notification_settings_down.sql
Next available version: 051

# Jordan checks status before deployment
$ acode db status
Applied: 48 migrations (current: 048_add_analytics)
Pending: 2 migrations
  049_add_user_preferences (Developer A)
  050_add_notification_settings (Developer B)

Checksum validation: ✓ All applied migrations verified
Version gap check: ✓ No gaps detected
Order validation: ✓ Sequential ordering confirmed
```

**Quantified Improvement:**
- Coordination time: 2.5 hours/week → 15 minutes/week (90% reduction)
- Conflict detection: At code review → At creation time (shift left)
- Migration incidents: 3/quarter → 0/quarter (100% elimination)
- Team velocity: 5% increase (less time on coordination)
- **Annual savings:** $54,000 (conflict prevention and incident reduction)

---

### Use Case 3: Alex Emergency Rollback in Production

**Persona:** Alex, an on-call SRE responding to a production incident at 3 AM

**Scenario:** A migration deployed earlier in the day is causing query performance degradation. Alex needs to quickly rollback the schema change while preserving data integrity.

**Before State (Manual Rollback):**
- Alex pages the developer who wrote the migration
- Developer writes ad-hoc rollback SQL at 3 AM
- Manual execution with copy-paste into production database
- No transaction wrapping (partial rollback possible)
- 2-hour average resolution time
- 15% of rollbacks cause additional issues

**After State (Safe Automated Rollback):**
```bash
# Alex identifies the problematic migration
$ acode db status
Current Version: 052_add_complex_indexes

# Preview the rollback
$ acode db rollback --dry-run
Would rollback: 052_add_complex_indexes
Down script preview:
  DROP INDEX IF EXISTS idx_messages_compound_search;
  DROP INDEX IF EXISTS idx_sessions_analytics;
No changes made.

# Execute rollback with backup
$ acode db rollback
Creating backup: .agent/backups/pre-rollback-20240116-030512.db
Acquiring migration lock...
Rolling back 052_add_complex_indexes...
  ✓ 2 statements executed (89ms)
  ✓ Version record removed
Lock released.
Rollback complete. Current version: 051_add_user_metrics

# Verify system health
$ acode db health
Database: HEALTHY
Schema version: 051
Checksum validation: ✓ PASS
```

**Quantified Improvement:**
- Resolution time: 2 hours → 5 minutes (96% reduction)
- Developer escalation: Required → Not required
- Rollback failures: 15% → 0.5% (97% reduction)
- Data integrity: At risk → Guaranteed (atomic transactions)
- **Annual savings:** $36,000 (faster incident resolution, fewer escalations)

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

- NFR-001: Startup migration check completes in < 100ms with up to 100 applied migrations
- NFR-002: Individual migration execution completes in < 30s for migrations with up to 50 DDL statements
- NFR-003: Status command returns in < 50ms with full migration history display
- NFR-004: Lock acquisition completes in < 100ms when no contention exists
- NFR-005: Checksum computation for 10KB migration file completes in < 10ms
- NFR-006: Migration discovery (embedded + file-based) completes in < 200ms for 100 migrations
- NFR-007: Dry-run preview generates output in < 500ms for 10 pending migrations

### Reliability

- NFR-008: All migrations execute atomically - complete success or complete rollback, no partial state
- NFR-009: Failed migration automatically rolls back transaction before returning error
- NFR-010: Rollback operations successfully restore schema to previous state in 99.9% of cases
- NFR-011: Lock mechanism prevents concurrent migration execution with zero race conditions
- NFR-012: Version tracking survives application crash between migrations

### Safety

- NFR-013: Locking prevents database corruption from concurrent migration attempts
- NFR-014: SHA-256 checksums detect any modification to applied migration files
- NFR-015: Pre-migration backup (when enabled) completes before any schema changes
- NFR-016: Checksum mismatch warnings logged but do not block by default (configurable)
- NFR-017: Version gap detection prevents skipping migrations

### Usability

- NFR-018: Status output clearly distinguishes applied vs pending migrations
- NFR-019: Error messages include specific error code, migration version, and recovery suggestions
- NFR-020: Dry-run mode shows exact SQL that would execute with no side effects
- NFR-021: Progress output shows real-time status for long-running migrations
- NFR-022: Create command generates properly formatted templates with documentation headers

### Maintainability

- NFR-023: Migration runner code has 90%+ test coverage
- NFR-024: All public APIs documented with XML comments
- NFR-025: Error codes follow ACODE-MIG-XXX pattern for consistency

---

## User Manual Documentation

### Quick Reference Card

| Command | Description | Example |
|---------|-------------|---------|
| `acode db status` | Show migration status | `acode db status` |
| `acode db migrate` | Apply pending migrations | `acode db migrate` |
| `acode db migrate --dry-run` | Preview changes | `acode db migrate --dry-run` |
| `acode db migrate --target <ver>` | Migrate to specific version | `acode db migrate --target 005` |
| `acode db rollback` | Rollback last migration | `acode db rollback` |
| `acode db rollback --steps N` | Rollback N migrations | `acode db rollback --steps 3` |
| `acode db rollback --target <ver>` | Rollback to version | `acode db rollback --target 003` |
| `acode db create <name>` | Create new migration | `acode db create add_users` |
| `acode db list` | List all migrations | `acode db list --all` |
| `acode db validate` | Validate migration checksums | `acode db validate` |
| `acode db unlock --force` | Force release stale lock | `acode db unlock --force` |
| `acode db backup` | Create database backup | `acode db backup --path ./backups` |

### Overview

The migration runner manages database schema changes through versioned SQL scripts. It provides:

- **Automatic bootstrapping** - Migrations run automatically when acode starts
- **Version tracking** - Applied migrations are recorded in `__migrations` table
- **Checksum validation** - Detects if migration files are modified after application
- **Concurrent safety** - Distributed locking prevents simultaneous migrations
- **Rollback support** - Each migration has an "up" and "down" script for reversibility

### Automatic Startup Bootstrapping

When acode starts, it automatically checks for pending migrations:

```bash
$ acode run "Hello"

╔═══════════════════════════════════════════════════╗
║            Database Migration Check               ║
╠═══════════════════════════════════════════════════╣
║ Database: .agent/data/acode.db                    ║
║ Current Version: 005_add_sync_status              ║
║ Applied: 5 migrations                             ║
║ Pending: 1 migration                              ║
╚═══════════════════════════════════════════════════╝

Acquiring migration lock...
  ✓ Lock acquired

Applying pending migrations...
  006_add_feature_flags...
    → Creating feature_flags table...
    → Creating indexes...
    ✓ Applied in 45ms
    
Releasing migration lock...
  ✓ Lock released

All migrations applied successfully.
Starting agent...
```

**Disable auto-migration** (for manual control):

```yaml
# .agent/config.yml
database:
  autoMigrate: false  # Require manual 'acode db migrate'
```

### CLI Commands - Detailed Examples

#### Check Migration Status

```bash
$ acode db status

╔═══════════════════════════════════════════════════════════════════════╗
║                        Migration Status                                ║
╠═══════════════════════════════════════════════════════════════════════╣
║ Database:        .agent/data/acode.db                                 ║
║ Provider:        SQLite                                                ║
║ Current Version: 005_add_sync_status                                  ║
║ Total Applied:   5 migrations                                          ║
║ Pending:         1 migration                                           ║
║ Schema Health:   ✓ Valid                                               ║
╚═══════════════════════════════════════════════════════════════════════╝

Applied Migrations:
┌────┬──────────────────────────┬─────────────────────┬────────────┬──────────┐
│ #  │ Version                  │ Applied At          │ Duration   │ Checksum │
├────┼──────────────────────────┼─────────────────────┼────────────┼──────────┤
│ 1  │ 001_initial_schema       │ 2024-01-01 10:00:00 │ 125ms      │ a1b2c3.. │
│ 2  │ 002_add_conversations    │ 2024-01-01 10:00:01 │ 89ms       │ d4e5f6.. │
│ 3  │ 003_add_sessions         │ 2024-01-01 10:00:02 │ 67ms       │ g7h8i9.. │
│ 4  │ 004_add_approvals        │ 2024-01-15 09:00:00 │ 234ms      │ j0k1l2.. │
│ 5  │ 005_add_sync_status      │ 2024-01-15 09:00:01 │ 156ms      │ m3n4o5.. │
└────┴──────────────────────────┴─────────────────────┴────────────┴──────────┘

Pending Migrations:
┌────┬──────────────────────────┬───────────────────────────────────────────────┐
│ #  │ Version                  │ Description                                    │
├────┼──────────────────────────┼───────────────────────────────────────────────┤
│ 1  │ 006_add_feature_flags    │ Add feature flag configuration table          │
└────┴──────────────────────────┴───────────────────────────────────────────────┘

Run 'acode db migrate' to apply pending migrations.
```

#### Apply Pending Migrations

```bash
$ acode db migrate

╔═══════════════════════════════════════════════════╗
║            Applying Migrations                     ║
╚═══════════════════════════════════════════════════╝

Acquiring migration lock...
  ✓ Lock acquired (SQLite file lock)

Validating checksums...
  ✓ All applied migrations valid

Applying 006_add_feature_flags...
  Statement 1/3: CREATE TABLE feature_flags...
    ✓ Executed in 12ms
  Statement 2/3: CREATE INDEX idx_feature_flags_key...
    ✓ Executed in 8ms
  Statement 3/3: CREATE INDEX idx_feature_flags_env...
    ✓ Executed in 7ms
  ✓ Migration applied in 45ms
  ✓ Recorded in __migrations table

Releasing lock...
  ✓ Lock released

╔═══════════════════════════════════════════════════╗
║            Migration Complete                      ║
╠═══════════════════════════════════════════════════╣
║ Applied:  1 migration                              ║
║ Duration: 52ms                                     ║
║ Status:   Success                                  ║
╚═══════════════════════════════════════════════════╝
```

#### Dry-Run Preview

```bash
$ acode db migrate --dry-run

╔═══════════════════════════════════════════════════╗
║            Migration Preview (Dry Run)             ║
╚═══════════════════════════════════════════════════╝

Would apply: 006_add_feature_flags

SQL Statements:
────────────────────────────────────────────────────
-- Statement 1
CREATE TABLE IF NOT EXISTS feature_flags (
    id TEXT PRIMARY KEY,
    key TEXT NOT NULL UNIQUE,
    value TEXT,
    environment TEXT DEFAULT 'all',
    enabled INTEGER DEFAULT 1,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

-- Statement 2
CREATE INDEX IF NOT EXISTS idx_feature_flags_key 
    ON feature_flags(key);

-- Statement 3
CREATE INDEX IF NOT EXISTS idx_feature_flags_env 
    ON feature_flags(environment);
────────────────────────────────────────────────────

⚠ DRY RUN - No changes were made to the database.
Run 'acode db migrate' to apply these changes.
```

#### Rollback Migrations

```bash
# Rollback last migration
$ acode db rollback

╔═══════════════════════════════════════════════════╗
║            Rolling Back Migration                  ║
╚═══════════════════════════════════════════════════╝

Acquiring migration lock...
  ✓ Lock acquired

Rolling back 006_add_feature_flags...
  Statement 1/3: DROP INDEX idx_feature_flags_env...
    ✓ Executed in 5ms
  Statement 2/3: DROP INDEX idx_feature_flags_key...
    ✓ Executed in 4ms
  Statement 3/3: DROP TABLE feature_flags...
    ✓ Executed in 8ms
  ✓ Rollback complete in 25ms
  ✓ Removed from __migrations table

Releasing lock...
  ✓ Lock released

╔═══════════════════════════════════════════════════╗
║            Rollback Complete                       ║
╠═══════════════════════════════════════════════════╣
║ Rolled back: 1 migration                           ║
║ Current:     005_add_sync_status                   ║
╚═══════════════════════════════════════════════════╝
```

```bash
# Rollback multiple migrations
$ acode db rollback --steps 2

Rolling back 006_add_feature_flags...
  ✓ Complete (25ms)
  
Rolling back 005_add_sync_status...
  ✓ Complete (18ms)

Rolled back 2 migrations.
Current version: 004_add_approvals
```

```bash
# Rollback to specific version
$ acode db rollback --target 003

Rolling back to version 003_add_sessions...

Rolling back 006_add_feature_flags...
  ✓ Complete

Rolling back 005_add_sync_status...
  ✓ Complete

Rolling back 004_add_approvals...
  ✓ Complete

Rolled back 3 migrations.
Current version: 003_add_sessions
```

#### Create New Migration

```bash
$ acode db create add_analytics

╔═══════════════════════════════════════════════════╗
║            Creating New Migration                  ║
╚═══════════════════════════════════════════════════╝

Created migration files:
  📄 .agent/migrations/007_add_analytics.sql
  📄 .agent/migrations/007_add_analytics_down.sql

Template content added to both files.
Edit the files and run: acode db migrate

Tip: Use --template <name> to use a predefined template:
  --template table    - Create table template
  --template index    - Create index template
  --template column   - Add column template
```

#### List All Migrations

```bash
$ acode db list --all

╔═══════════════════════════════════════════════════════════════════════════╗
║                           All Migrations                                    ║
╠═══════════════════════════════════════════════════════════════════════════╣
│ Status │ Version                  │ Source    │ Has Down │ Checksum       │
├────────┼──────────────────────────┼───────────┼──────────┼────────────────┤
│ ✓      │ 001_initial_schema       │ Embedded  │ Yes      │ a1b2c3d4e5f6.. │
│ ✓      │ 002_add_conversations    │ Embedded  │ Yes      │ b2c3d4e5f6g7.. │
│ ✓      │ 003_add_sessions         │ Embedded  │ Yes      │ c3d4e5f6g7h8.. │
│ ✓      │ 004_add_approvals        │ Embedded  │ Yes      │ d4e5f6g7h8i9.. │
│ ✓      │ 005_add_sync_status      │ Embedded  │ Yes      │ e5f6g7h8i9j0.. │
│ ○      │ 006_add_feature_flags    │ File      │ Yes      │ f6g7h8i9j0k1.. │
│ ○      │ 007_add_analytics        │ File      │ No       │ g7h8i9j0k1l2.. │
╚═══════════════════════════════════════════════════════════════════════════╝

Legend: ✓ = Applied, ○ = Pending, ⚠ = Missing down script
```

#### Validate Checksums

```bash
$ acode db validate

╔═══════════════════════════════════════════════════╗
║            Validating Migration Checksums          ║
╚═══════════════════════════════════════════════════╝

Checking 5 applied migrations...

  001_initial_schema      ✓ Valid
  002_add_conversations   ✓ Valid
  003_add_sessions        ✓ Valid
  004_add_approvals       ✓ Valid
  005_add_sync_status     ✓ Valid

All checksums valid. No tampering detected.
```

### Configuration Reference

```yaml
# .agent/config.yml - Complete migration configuration

database:
  # Database provider: sqlite or postgresql
  provider: sqlite
  
  # SQLite-specific settings
  sqlite:
    path: .agent/data/acode.db
    
  # PostgreSQL-specific settings  
  postgresql:
    host: localhost
    port: 5432
    database: acode
    username: acode_user
    password: ${ACODE_DB_PASSWORD}  # Environment variable
    sslMode: prefer
    
  # Migration settings
  migrations:
    # Auto-apply pending migrations on startup
    autoMigrate: true
    
    # Directory for file-based migrations
    directory: .agent/migrations
    
    # Backup settings
    backup:
      enabled: true
      directory: .agent/backups
      retentionDays: 30
      maxBackups: 10
      
    # Locking settings
    lock:
      timeoutSeconds: 60
      staleThresholdMinutes: 10
      
    # Checksum validation
    validateChecksums: true
    failOnChecksumMismatch: true
    
    # Security settings
    security:
      validateSqlPatterns: true
      blockPrivilegeEscalation: true
      requireDownScripts: true
      
    # Logging
    logging:
      logStatements: false      # Log each SQL statement
      logTiming: true           # Log execution timing
      verboseOutput: false      # Extra detail in output
```

### Migration File Templates

#### Standard Table Migration

```sql
-- .agent/migrations/007_add_analytics.sql
-- 
-- Purpose: Add analytics tracking tables for usage metrics
-- Author: developer
-- Date: 2024-01-20
-- Dependencies: 001_initial_schema

-- ============================================================
-- UP Migration: Create analytics tables
-- ============================================================

CREATE TABLE IF NOT EXISTS sys_analytics (
    id TEXT PRIMARY KEY,
    event_type TEXT NOT NULL,
    event_source TEXT NOT NULL DEFAULT 'agent',
    payload TEXT,  -- JSON payload
    user_context TEXT,
    session_id TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    
    -- Foreign key to sessions (if exists)
    FOREIGN KEY (session_id) REFERENCES sys_sessions(id) ON DELETE SET NULL
);

-- Indexes for common queries
CREATE INDEX IF NOT EXISTS idx_sys_analytics_type 
    ON sys_analytics(event_type);
    
CREATE INDEX IF NOT EXISTS idx_sys_analytics_created 
    ON sys_analytics(created_at);
    
CREATE INDEX IF NOT EXISTS idx_sys_analytics_session 
    ON sys_analytics(session_id);

-- Composite index for type + date range queries
CREATE INDEX IF NOT EXISTS idx_sys_analytics_type_date 
    ON sys_analytics(event_type, created_at);
```

```sql
-- .agent/migrations/007_add_analytics_down.sql
-- 
-- Purpose: Rollback analytics tables
-- Author: developer
-- Date: 2024-01-20

-- ============================================================
-- DOWN Migration: Remove analytics tables
-- ============================================================

-- Drop indexes first (in reverse order of creation)
DROP INDEX IF EXISTS idx_sys_analytics_type_date;
DROP INDEX IF EXISTS idx_sys_analytics_session;
DROP INDEX IF EXISTS idx_sys_analytics_created;
DROP INDEX IF EXISTS idx_sys_analytics_type;

-- Drop table
DROP TABLE IF EXISTS sys_analytics;
```

#### Add Column Migration

```sql
-- .agent/migrations/008_add_user_preferences.sql
-- 
-- Purpose: Add preferences column to users
-- Author: developer  
-- Date: 2024-01-21

-- SQLite doesn't support ADD COLUMN IF NOT EXISTS, 
-- so we check and create conditionally

-- Add preferences column (JSON)
ALTER TABLE sys_sessions ADD COLUMN preferences TEXT DEFAULT '{}';

-- Add index for JSON extraction queries
CREATE INDEX IF NOT EXISTS idx_sessions_pref_theme 
    ON sys_sessions(json_extract(preferences, '$.theme'));
```

```sql
-- .agent/migrations/008_add_user_preferences_down.sql
-- 
-- Purpose: Remove preferences column

-- SQLite doesn't support DROP COLUMN directly
-- Must recreate table without the column

-- Create temp table without preferences
CREATE TABLE sys_sessions_temp AS 
SELECT id, conversation_id, started_at, ended_at, status 
FROM sys_sessions;

-- Drop original and rename
DROP TABLE sys_sessions;
ALTER TABLE sys_sessions_temp RENAME TO sys_sessions;

-- Recreate necessary indexes
CREATE INDEX IF NOT EXISTS idx_sessions_conversation 
    ON sys_sessions(conversation_id);
```

### Frequently Asked Questions

**Q: What happens if a migration fails partway through?**

A: The migration runner wraps each migration in a transaction. If any statement fails, the entire migration is rolled back, leaving the database in its previous state. The migration is not recorded as applied.

**Q: Can I modify a migration after it's been applied?**

A: No. Once a migration is applied, its checksum is recorded. Modifying the file will cause a checksum mismatch error on the next validation. Create a new migration for additional changes.

**Q: How do I handle migrations in a team environment?**

A: 
1. Always pull latest migrations before creating new ones
2. Use sequential version numbers (001, 002, etc.)
3. Test migrations locally before pushing
4. Never modify pushed migrations

**Q: What's the difference between embedded and file-based migrations?**

A: Embedded migrations are compiled into the acode binary and represent the core schema. File-based migrations in `.agent/migrations/` are for custom extensions and local development.

**Q: How do I migrate from SQLite to PostgreSQL?**

A: Use `acode db export --format sql --target postgres` to generate PostgreSQL-compatible SQL, then run migrations on the new database. The migration runner abstracts provider differences in migrations.

**Q: Can I run migrations manually without the CLI?**

A: Yes, but not recommended. The migration runner handles locking, checksum recording, and version tracking. Manual SQL execution bypasses these safety mechanisms.

**Q: What if I need to run a data migration (not just DDL)?**

A: Data migrations are supported. Include INSERT/UPDATE/DELETE statements in your migration. Wrap complex data transformations in transactions for atomicity.

**Q: How do I skip a problematic migration?**

A: Use `acode db migrate --skip 006` to skip a specific version. This records the migration as "skipped" in the version table. Use with caution and document why.

**Q: Can I run migrations in a Docker container?**

A: Yes. Set `database.autoMigrate: true` and ensure the migrations directory is mounted or migrations are embedded. The container will bootstrap on startup.

**Q: How do I troubleshoot "lock timeout" errors?**

A: See the Troubleshooting section. Common causes: another process running, crashed migration left stale lock, or lock file permissions.

---

## Acceptance Criteria

### Migration Discovery

- [ ] AC-001: MigrationDiscovery scans embedded resources for `*.sql` migration files
- [ ] AC-002: MigrationDiscovery scans `.agent/migrations/` directory for file-based migrations
- [ ] AC-003: Migrations are ordered by version number prefix (001, 002, 003...)
- [ ] AC-004: Discovery pairs `XXX_name.sql` (up) with `XXX_name_down.sql` (down) files
- [ ] AC-005: Discovery logs warning if down script is missing for a migration
- [ ] AC-006: Discovery throws if duplicate version numbers are detected
- [ ] AC-007: Discovery respects `migrations.directory` config setting
- [ ] AC-008: Discovery handles mixed embedded + file-based migrations correctly

### Version Table Management

- [ ] AC-009: __migrations table is auto-created if not exists on first migration check
- [ ] AC-010: Version table schema includes: version, checksum, applied_at, applied_by, duration_ms
- [ ] AC-011: Applied migrations are recorded with SHA-256 checksum of file content
- [ ] AC-012: Version table query returns migrations ordered by applied_at ascending
- [ ] AC-013: GetAppliedMigrations returns empty list for fresh database
- [ ] AC-014: Version table uses TEXT for version to support alphanumeric names
- [ ] AC-015: RecordMigration atomically inserts with current timestamp

### Migration Execution

- [ ] AC-016: MigrationRunner applies all pending migrations in version order
- [ ] AC-017: Each migration executes within a database transaction
- [ ] AC-018: Transaction is committed only after all statements succeed
- [ ] AC-019: Transaction is rolled back if any statement fails
- [ ] AC-020: Failed migration throws MigrationException with error details
- [ ] AC-021: MigrationRunner logs each statement before execution
- [ ] AC-022: MigrationRunner records execution time for each migration
- [ ] AC-023: MigrationRunner validates SQL patterns before execution (security)
- [ ] AC-024: MigrationRunner blocks migrations with privilege escalation patterns

### Rollback Operations

- [ ] AC-025: Rollback executes down script for specified migration version
- [ ] AC-026: Rollback removes migration record from __migrations table
- [ ] AC-027: Rollback --steps N rolls back last N migrations in reverse order
- [ ] AC-028: Rollback --target VERSION rolls back to specified version (exclusive)
- [ ] AC-029: Rollback fails with clear error if down script is missing
- [ ] AC-030: Rollback executes within transaction for atomicity
- [ ] AC-031: Rollback logs each statement before execution
- [ ] AC-032: Rollback validates down script for destructive patterns

### Startup Bootstrapping

- [ ] AC-033: Application startup checks for pending migrations before main execution
- [ ] AC-034: Auto-migrate applies pending if `database.autoMigrate: true`
- [ ] AC-035: Startup blocks until all migrations complete (no concurrent operations)
- [ ] AC-036: Startup logs migration activity to console and log file
- [ ] AC-037: Startup fails fast if migration fails (application does not start)
- [ ] AC-038: Startup respects `autoMigrate: false` and logs pending count only
- [ ] AC-039: Startup displays migration summary in structured format
- [ ] AC-040: Startup timeout is configurable (default 120 seconds)

### CLI Commands - db status

- [ ] AC-041: `acode db status` shows current database version
- [ ] AC-042: `acode db status` lists all applied migrations with timestamps
- [ ] AC-043: `acode db status` lists all pending migrations
- [ ] AC-044: `acode db status` shows database provider (SQLite/PostgreSQL)
- [ ] AC-045: `acode db status` shows checksum validation status
- [ ] AC-046: `acode db status` returns exit code 0 if healthy, 1 if issues

### CLI Commands - db migrate

- [ ] AC-047: `acode db migrate` applies all pending migrations
- [ ] AC-048: `acode db migrate --dry-run` shows SQL without executing
- [ ] AC-049: `acode db migrate --target VERSION` migrates to specific version
- [ ] AC-050: `acode db migrate --skip VERSION` skips specified migration
- [ ] AC-051: `acode db migrate` shows progress for each migration
- [ ] AC-052: `acode db migrate` displays total duration on completion
- [ ] AC-053: `acode db migrate` returns exit code 0 on success, non-zero on failure

### CLI Commands - db rollback

- [ ] AC-054: `acode db rollback` rolls back last applied migration
- [ ] AC-055: `acode db rollback --steps N` rolls back N migrations
- [ ] AC-056: `acode db rollback --target VERSION` rolls back to version
- [ ] AC-057: `acode db rollback --dry-run` shows what would be rolled back
- [ ] AC-058: `acode db rollback` prompts for confirmation unless --yes flag
- [ ] AC-059: `acode db rollback` returns exit code 0 on success

### CLI Commands - db create

- [ ] AC-060: `acode db create NAME` creates new migration files
- [ ] AC-061: Created files use next sequential version number
- [ ] AC-062: Created files include header comments with metadata
- [ ] AC-063: `acode db create --template TABLE` uses table template
- [ ] AC-064: `acode db create --template INDEX` uses index template
- [ ] AC-065: Created files are placed in configured migrations directory

### CLI Commands - db validate

- [ ] AC-066: `acode db validate` checks checksums of all applied migrations
- [ ] AC-067: `acode db validate` reports mismatches with file paths
- [ ] AC-068: `acode db validate` returns exit code 0 if all valid
- [ ] AC-069: `acode db validate` returns exit code 1 if any mismatch

### CLI Commands - db backup

- [ ] AC-070: `acode db backup` creates timestamped database backup
- [ ] AC-071: `acode db backup --path DIR` specifies backup directory
- [ ] AC-072: Backup is created before migration if `backup.enabled: true`
- [ ] AC-073: Old backups are pruned based on `backup.retentionDays`

### Locking Mechanism

- [ ] AC-074: Migration lock is acquired before any migration operation
- [ ] AC-075: Lock prevents concurrent migrations from multiple processes
- [ ] AC-076: PostgreSQL uses advisory lock `pg_try_advisory_lock()`
- [ ] AC-077: SQLite uses file-based lock `.migration-lock`
- [ ] AC-078: Lock acquisition times out after configurable period (default 60s)
- [ ] AC-079: Lock is released on migration completion (success or failure)
- [ ] AC-080: Lock is released on process termination via cleanup hook
- [ ] AC-081: Stale locks (>10 min) are automatically released with warning
- [ ] AC-082: `acode db unlock --force` manually releases stale lock

### Checksum Validation

- [ ] AC-083: SHA-256 checksum computed for each migration file content
- [ ] AC-084: Checksum uses normalized UTF-8 content (FormC)
- [ ] AC-085: Checksum is stored when migration is applied
- [ ] AC-086: Checksum is validated before each migration operation
- [ ] AC-087: Checksum mismatch throws ChecksumMismatchException
- [ ] AC-088: Checksum mismatch is logged as security event
- [ ] AC-089: `--force` flag bypasses checksum validation (with warning)

### Error Handling

- [ ] AC-090: ACODE-MIG-001 returned when migration execution fails
- [ ] AC-091: ACODE-MIG-002 returned when lock acquisition times out
- [ ] AC-092: ACODE-MIG-003 returned when checksum mismatch detected
- [ ] AC-093: ACODE-MIG-004 returned when down script is missing for rollback
- [ ] AC-094: ACODE-MIG-005 returned when rollback execution fails
- [ ] AC-095: ACODE-MIG-006 returned when version gap detected
- [ ] AC-096: ACODE-MIG-007 returned when database connection fails
- [ ] AC-097: ACODE-MIG-008 returned when backup creation fails
- [ ] AC-098: All errors include actionable resolution guidance

### Logging and Observability

- [ ] AC-099: All migration operations logged at INFO level
- [ ] AC-100: SQL statements logged at DEBUG level (if enabled)
- [ ] AC-101: Execution timing logged for performance analysis
- [ ] AC-102: Security events logged at WARNING/ERROR level
- [ ] AC-103: Structured logging includes migration version, duration, outcome

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

## Security Considerations

### Threat 1: SQL Injection via Migration Files

**Risk:** Malicious SQL injected into migration files could execute arbitrary commands, drop tables, or exfiltrate data during migration execution.

**Attack Scenario:** An attacker gains write access to the migrations folder and modifies an existing migration or creates a new one containing `DROP DATABASE` or `SELECT * INTO OUTFILE` commands.

**Mitigation Code:**

```csharp
// Infrastructure/Persistence/Migrations/MigrationSqlValidator.cs
namespace Acode.Infrastructure.Persistence.Migrations;

public sealed class MigrationSqlValidator
{
    private static readonly string[] ForbiddenPatterns = new[]
    {
        @"DROP\s+DATABASE",
        @"DROP\s+SCHEMA",
        @"TRUNCATE\s+TABLE\s+__migrations",
        @"DELETE\s+FROM\s+__migrations",
        @"INTO\s+OUTFILE",
        @"INTO\s+DUMPFILE",
        @"LOAD_FILE\s*\(",
        @"xp_cmdshell",
        @"sp_executesql",
        @"EXEC\s*\(",
        @"EXECUTE\s+IMMEDIATE",
        @"--\s*BYPASS",
        @"/\*.*ADMIN.*\*/"
    };
    
    private readonly ILogger<MigrationSqlValidator> _logger;
    
    public MigrationSqlValidator(ILogger<MigrationSqlValidator> logger)
    {
        _logger = logger;
    }
    
    public ValidationResult Validate(string migrationContent, string migrationName)
    {
        var errors = new List<string>();
        
        foreach (var pattern in ForbiddenPatterns)
        {
            if (Regex.IsMatch(migrationContent, pattern, RegexOptions.IgnoreCase))
            {
                errors.Add($"Forbidden SQL pattern detected: {pattern}");
                _logger.LogWarning(
                    "Security: Forbidden SQL pattern in migration {Migration}: {Pattern}",
                    migrationName, pattern);
            }
        }
        
        // Check for multiple statements that could hide malicious code
        var statementCount = migrationContent.Split(';').Length;
        if (statementCount > 100)
        {
            _logger.LogWarning(
                "Security: Migration {Migration} has {Count} statements, manual review recommended",
                migrationName, statementCount);
        }
        
        return new ValidationResult(errors.Count == 0, errors);
    }
}
```

---

### Threat 2: Migration File Tampering Post-Deployment

**Risk:** After a migration is applied, an attacker modifies the migration file to hide malicious changes or to cause checksum validation failures that disrupt operations.

**Attack Scenario:** Attacker modifies a previously applied migration file. On next startup, checksum validation fails, potentially blocking the application or (if validation is bypassed with --force) masking evidence of the original migration content.

**Mitigation Code:**

```csharp
// Infrastructure/Persistence/Migrations/SecureChecksumValidator.cs
namespace Acode.Infrastructure.Persistence.Migrations;

public sealed class SecureChecksumValidator
{
    private readonly ILogger<SecureChecksumValidator> _logger;
    private readonly IMigrationRepository _repository;
    
    public SecureChecksumValidator(
        ILogger<SecureChecksumValidator> logger,
        IMigrationRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }
    
    public async Task<ChecksumValidationResult> ValidateAsync(
        IReadOnlyList<MigrationFile> migrationFiles,
        CancellationToken ct)
    {
        var appliedMigrations = await _repository.GetAppliedAsync(ct);
        var appliedByVersion = appliedMigrations.ToDictionary(m => m.Version);
        var mismatches = new List<ChecksumMismatch>();
        
        foreach (var file in migrationFiles)
        {
            if (!appliedByVersion.TryGetValue(file.Version, out var applied))
                continue; // Not yet applied, skip validation
            
            var currentChecksum = ComputeChecksum(file.Content);
            
            if (currentChecksum != applied.Checksum)
            {
                var mismatch = new ChecksumMismatch(
                    file.Version,
                    applied.Checksum,
                    currentChecksum,
                    applied.AppliedAt);
                
                mismatches.Add(mismatch);
                
                _logger.LogError(
                    "SECURITY ALERT: Migration {Version} checksum mismatch! " +
                    "Expected: {Expected}, Actual: {Actual}. " +
                    "File may have been tampered with after application on {AppliedAt}",
                    file.Version, applied.Checksum, currentChecksum, applied.AppliedAt);
                
                // Record security event for audit
                await _repository.RecordSecurityEventAsync(new SecurityEvent
                {
                    EventType = "CHECKSUM_MISMATCH",
                    MigrationVersion = file.Version,
                    ExpectedChecksum = applied.Checksum,
                    ActualChecksum = currentChecksum,
                    DetectedAt = DateTime.UtcNow
                }, ct);
            }
        }
        
        return new ChecksumValidationResult(
            IsValid: mismatches.Count == 0,
            Mismatches: mismatches);
    }
    
    private static string ComputeChecksum(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content.Normalize(NormalizationForm.FormC));
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
```

---

### Threat 3: Concurrent Migration Race Condition

**Risk:** Multiple processes attempting migrations simultaneously could cause schema corruption, duplicate table creation attempts, or inconsistent version tracking.

**Attack Scenario:** During high-availability deployment, multiple application instances start simultaneously. Without proper locking, both attempt to apply the same migration, causing constraint violations or partial schema states.

**Mitigation Code:**

```csharp
// Infrastructure/Persistence/Migrations/DistributedMigrationLock.cs
namespace Acode.Infrastructure.Persistence.Migrations;

public sealed class DistributedMigrationLock : IAsyncDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<DistributedMigrationLock> _logger;
    private readonly TimeSpan _timeout;
    private readonly string _lockId;
    private IDbConnection? _lockConnection;
    private bool _lockAcquired;
    
    public DistributedMigrationLock(
        IConnectionFactory connectionFactory,
        ILogger<DistributedMigrationLock> logger,
        TimeSpan timeout)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _timeout = timeout;
        _lockId = $"acode_migration_{Environment.MachineName}_{Process.GetCurrentProcess().Id}";
    }
    
    public async Task<bool> TryAcquireAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.Elapsed < _timeout)
        {
            ct.ThrowIfCancellationRequested();
            
            try
            {
                _lockConnection = await _connectionFactory.CreateAsync(ct);
                
                // Use database-specific advisory lock
                if (_connectionFactory.DatabaseType == DatabaseType.Postgres)
                {
                    // PostgreSQL advisory lock (session-level)
                    var lockKey = Math.Abs("acode_migrations".GetHashCode());
                    await using var cmd = ((NpgsqlConnection)_lockConnection).CreateCommand();
                    cmd.CommandText = $"SELECT pg_try_advisory_lock({lockKey})";
                    var result = await cmd.ExecuteScalarAsync(ct);
                    
                    if (result is true)
                    {
                        _lockAcquired = true;
                        _logger.LogDebug("Acquired PostgreSQL advisory lock for migrations");
                        return true;
                    }
                }
                else
                {
                    // SQLite: Use file-based lock + database lock
                    var lockFile = Path.Combine(
                        Path.GetDirectoryName(_connectionFactory.DatabasePath)!,
                        ".migration-lock");
                    
                    try
                    {
                        // Atomic file creation as lock
                        using var fs = new FileStream(
                            lockFile,
                            FileMode.CreateNew,
                            FileAccess.Write,
                            FileShare.None);
                        
                        await fs.WriteAsync(Encoding.UTF8.GetBytes(_lockId), ct);
                        _lockAcquired = true;
                        _logger.LogDebug("Acquired file lock for migrations: {LockFile}", lockFile);
                        return true;
                    }
                    catch (IOException)
                    {
                        // Lock file exists, check if stale
                        if (await IsLockStaleAsync(lockFile, ct))
                        {
                            File.Delete(lockFile);
                            continue; // Retry
                        }
                    }
                }
                
                // Lock not acquired, wait and retry
                await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to acquire migration lock, retrying...");
                await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
            }
        }
        
        _logger.LogError("Failed to acquire migration lock within {Timeout}", _timeout);
        return false;
    }
    
    private async Task<bool> IsLockStaleAsync(string lockFile, CancellationToken ct)
    {
        try
        {
            var lockInfo = await File.ReadAllTextAsync(lockFile, ct);
            var creationTime = File.GetCreationTimeUtc(lockFile);
            
            // Consider lock stale if older than 10 minutes
            if (DateTime.UtcNow - creationTime > TimeSpan.FromMinutes(10))
            {
                _logger.LogWarning("Detected stale migration lock from {Time}, removing", creationTime);
                return true;
            }
            
            return false;
        }
        catch
        {
            return true;
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_lockAcquired)
        {
            // Release the lock
            if (_connectionFactory.DatabaseType == DatabaseType.Postgres && _lockConnection != null)
            {
                var lockKey = Math.Abs("acode_migrations".GetHashCode());
                await using var cmd = ((NpgsqlConnection)_lockConnection).CreateCommand();
                cmd.CommandText = $"SELECT pg_advisory_unlock({lockKey})";
                await cmd.ExecuteScalarAsync();
            }
            else
            {
                var lockFile = Path.Combine(
                    Path.GetDirectoryName(_connectionFactory.DatabasePath)!,
                    ".migration-lock");
                
                if (File.Exists(lockFile))
                    File.Delete(lockFile);
            }
            
            _logger.LogDebug("Released migration lock");
        }
        
        if (_lockConnection != null)
        {
            await ((IAsyncDisposable)_lockConnection).DisposeAsync();
        }
    }
}
```

---

### Threat 4: Privilege Escalation via Migration Execution

**Risk:** Migration execution typically runs with elevated database privileges. A malicious migration could grant excessive permissions or create backdoor accounts.

**Attack Scenario:** An attacker creates a migration that includes `GRANT ALL PRIVILEGES` or `CREATE USER admin WITH SUPERUSER` statements, establishing persistent unauthorized access.

**Mitigation Code:**

```csharp
// Infrastructure/Persistence/Migrations/PrivilegeEscalationDetector.cs
namespace Acode.Infrastructure.Persistence.Migrations;

public sealed class PrivilegeEscalationDetector
{
    private static readonly string[] DangerousPatterns = new[]
    {
        @"GRANT\s+ALL",
        @"GRANT\s+.*SUPERUSER",
        @"GRANT\s+.*ADMIN",
        @"CREATE\s+USER",
        @"CREATE\s+ROLE",
        @"ALTER\s+USER.*PASSWORD",
        @"ALTER\s+ROLE.*PASSWORD",
        @"SECURITY\s+DEFINER",
        @"SET\s+ROLE",
        @"SET\s+SESSION\s+AUTHORIZATION",
        @"pg_read_server_files",
        @"pg_write_server_files",
        @"pg_execute_server_program"
    };
    
    private readonly ILogger<PrivilegeEscalationDetector> _logger;
    
    public PrivilegeEscalationDetector(ILogger<PrivilegeEscalationDetector> logger)
    {
        _logger = logger;
    }
    
    public PrivilegeCheckResult Analyze(string migrationContent, string migrationName)
    {
        var findings = new List<PrivilegeFinding>();
        
        foreach (var pattern in DangerousPatterns)
        {
            var matches = Regex.Matches(migrationContent, pattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                var finding = new PrivilegeFinding(
                    Pattern: pattern,
                    MatchedText: match.Value,
                    LineNumber: GetLineNumber(migrationContent, match.Index),
                    Severity: GetSeverity(pattern));
                
                findings.Add(finding);
                
                _logger.LogWarning(
                    "Security: Privilege escalation pattern in migration {Migration} at line {Line}: {Match}",
                    migrationName, finding.LineNumber, finding.MatchedText);
            }
        }
        
        if (findings.Any(f => f.Severity == Severity.Critical))
        {
            _logger.LogError(
                "SECURITY BLOCK: Migration {Migration} contains critical privilege escalation patterns. " +
                "Manual review required. Use --allow-privileged to bypass (not recommended).",
                migrationName);
        }
        
        return new PrivilegeCheckResult(
            IsClean: findings.Count == 0,
            Findings: findings,
            RequiresManualApproval: findings.Any(f => f.Severity >= Severity.High));
    }
    
    private static int GetLineNumber(string content, int charIndex)
    {
        return content[..charIndex].Count(c => c == '\n') + 1;
    }
    
    private static Severity GetSeverity(string pattern) => pattern switch
    {
        var p when p.Contains("SUPERUSER") => Severity.Critical,
        var p when p.Contains("CREATE USER") => Severity.Critical,
        var p when p.Contains("GRANT ALL") => Severity.High,
        var p when p.Contains("PASSWORD") => Severity.High,
        _ => Severity.Medium
    };
}

public enum Severity { Low, Medium, High, Critical }

public record PrivilegeFinding(string Pattern, string MatchedText, int LineNumber, Severity Severity);

public record PrivilegeCheckResult(bool IsClean, IReadOnlyList<PrivilegeFinding> Findings, bool RequiresManualApproval);
```

---

### Threat 5: Denial of Service via Migration Lock Exhaustion

**Risk:** An attacker could intentionally acquire and hold the migration lock, preventing legitimate migrations from running and blocking application startup.

**Attack Scenario:** During deployment, an attacker runs a script that acquires the migration lock and sleeps indefinitely, causing all application instances to timeout waiting for the lock.

**Mitigation Code:**

```csharp
// Infrastructure/Persistence/Migrations/MigrationLockGuard.cs
namespace Acode.Infrastructure.Persistence.Migrations;

public sealed class MigrationLockGuard
{
    private readonly ILogger<MigrationLockGuard> _logger;
    private readonly IMigrationRepository _repository;
    private readonly TimeSpan _maxLockDuration;
    private readonly TimeSpan _warningThreshold;
    
    public MigrationLockGuard(
        ILogger<MigrationLockGuard> logger,
        IMigrationRepository repository,
        IOptions<MigrationOptions> options)
    {
        _logger = logger;
        _repository = repository;
        _maxLockDuration = TimeSpan.FromMinutes(options.Value.MaxLockDurationMinutes);
        _warningThreshold = TimeSpan.FromSeconds(options.Value.LockWarningThresholdSeconds);
    }
    
    public async Task<LockStatus> CheckLockHealthAsync(CancellationToken ct)
    {
        var lockInfo = await _repository.GetActiveLockAsync(ct);
        
        if (lockInfo == null)
            return LockStatus.Available;
        
        var lockAge = DateTime.UtcNow - lockInfo.AcquiredAt;
        
        if (lockAge > _maxLockDuration)
        {
            _logger.LogError(
                "SECURITY: Migration lock held for {Duration} exceeds maximum {Max}. " +
                "Lock holder: {Holder}. Force releasing as potential DoS.",
                lockAge, _maxLockDuration, lockInfo.HolderId);
            
            await _repository.ForceReleaseLockAsync(lockInfo.LockId, ct);
            
            await _repository.RecordSecurityEventAsync(new SecurityEvent
            {
                EventType = "LOCK_FORCE_RELEASED",
                Details = $"Lock held by {lockInfo.HolderId} for {lockAge} exceeded maximum",
                DetectedAt = DateTime.UtcNow
            }, ct);
            
            return LockStatus.ForceReleased;
        }
        
        if (lockAge > _warningThreshold)
        {
            _logger.LogWarning(
                "Migration lock held for {Duration} by {Holder}. " +
                "Will force release after {Max}.",
                lockAge, lockInfo.HolderId, _maxLockDuration);
            
            return LockStatus.Warning;
        }
        
        return LockStatus.Healthy;
    }
    
    public async Task<bool> CanBypassLockAsync(string reason, CancellationToken ct)
    {
        // Only allow bypass in specific recovery scenarios
        if (string.IsNullOrWhiteSpace(reason))
            return false;
        
        var lockInfo = await _repository.GetActiveLockAsync(ct);
        if (lockInfo == null)
            return true;
        
        var lockAge = DateTime.UtcNow - lockInfo.AcquiredAt;
        
        // Only allow bypass if lock is clearly stale
        if (lockAge > _maxLockDuration)
        {
            _logger.LogWarning(
                "Allowing lock bypass due to stale lock. Reason: {Reason}", reason);
            return true;
        }
        
        return false;
    }
}

public enum LockStatus { Available, Healthy, Warning, ForceReleased }

public record LockInfo(string LockId, string HolderId, DateTime AcquiredAt);
```

---

## Troubleshooting

### Issue 1: Migration Lock Timeout (ACODE-MIG-002)

**Error Message:**
```
Error: Could not acquire migration lock within 60 seconds
Code: ACODE-MIG-002
```

**Symptoms:**
- `acode db migrate` hangs then fails with timeout
- Application startup blocked waiting for lock
- Multiple instances show lock contention warnings

**Causes:**
- Previous migration crashed without releasing lock
- Another migration process is currently running
- Lock file has incorrect permissions
- PostgreSQL advisory lock held by abandoned connection

**Diagnostic Steps:**
```bash
# Check for other agent processes
$ ps aux | grep acode

# Check lock file (SQLite)
$ ls -la .agent/data/.migration-lock

# Check PostgreSQL locks
$ psql -c "SELECT * FROM pg_locks WHERE locktype = 'advisory';"
```

**Solutions:**

1. **Wait for other process** - If another migration is legitimately running, wait for completion

2. **Remove stale lock file (SQLite)**:
```bash
$ rm .agent/data/.migration-lock
$ acode db migrate
```

3. **Force unlock via CLI**:
```bash
$ acode db unlock --force
Force releasing migration lock...
  ✓ Lock released

$ acode db migrate
```

4. **Kill orphaned PostgreSQL connection**:
```sql
-- Find the blocking connection
SELECT pid, usename, state, query 
FROM pg_stat_activity 
WHERE query LIKE '%advisory%';

-- Terminate if stale
SELECT pg_terminate_backend(<pid>);
```

5. **Increase timeout** (for slow migrations):
```yaml
# .agent/config.yml
database:
  migrations:
    lock:
      timeoutSeconds: 180  # Increase from default 60
```

---

### Issue 2: Checksum Mismatch (ACODE-MIG-003)

**Error Message:**
```
Error: Migration 005_add_sync_status checksum mismatch
Expected: a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6
Actual:   z9y8x7w6v5u4t3s2r1q0p9o8n7m6l5k4
Code: ACODE-MIG-003
```

**Symptoms:**
- Migration validation fails on startup
- `acode db validate` reports mismatch
- Security event logged as potential tampering

**Causes:**
- Migration file edited after being applied
- Git merge conflict corrupted file
- Different line endings (CRLF vs LF) across systems
- Encoding change (UTF-8 BOM added)

**Diagnostic Steps:**
```bash
# Check file for recent changes
$ git log -p -- .agent/migrations/005_add_sync_status.sql

# Compare checksum
$ acode db validate --verbose

# Check file encoding
$ file .agent/migrations/005_add_sync_status.sql
```

**Solutions:**

1. **Restore original file** (recommended):
```bash
$ git checkout HEAD~1 -- .agent/migrations/005_add_sync_status.sql
$ acode db validate
```

2. **Normalize line endings**:
```bash
# Convert to LF
$ dos2unix .agent/migrations/005_add_sync_status.sql
```

3. **Reset checksum** (use with caution):
```bash
$ acode db reset-checksum 005_add_sync_status --confirm
Warning: This updates the stored checksum to match current file.
Only use if you are certain the current file content is correct.
  ✓ Checksum updated

$ acode db validate
  ✓ All checksums valid
```

4. **Force bypass** (last resort, not recommended):
```bash
$ acode db migrate --force --skip-checksum
Warning: Bypassing checksum validation is a security risk.
```

---

### Issue 3: Auto-Migrate Fails at Startup

**Error Message:**
```
Error: Startup migration failed
Migration: 006_add_feature_flags
Statement: CREATE TABLE feature_flags (...)
SQLite Error: table feature_flags already exists
Code: ACODE-MIG-001
```

**Symptoms:**
- Application exits immediately with migration error
- Service fails to start in production
- Container restart loops

**Causes:**
- Previous partial migration left schema in inconsistent state
- Migration file missing `IF NOT EXISTS` clauses
- Database was manually modified outside of migrations
- Migration applied manually but not recorded

**Diagnostic Steps:**
```bash
# Check current migration status
$ acode db status

# Inspect actual schema
$ sqlite3 .agent/data/acode.db ".schema feature_flags"

# Check __migrations table
$ sqlite3 .agent/data/acode.db "SELECT * FROM __migrations ORDER BY applied_at"
```

**Solutions:**

1. **Disable auto-migrate temporarily**:
```yaml
# .agent/config.yml
database:
  autoMigrate: false
```

2. **Fix migration file** (add IF NOT EXISTS):
```sql
-- Before
CREATE TABLE feature_flags (...);

-- After
CREATE TABLE IF NOT EXISTS feature_flags (...);
```

3. **Mark migration as applied** (if already in schema):
```bash
$ acode db mark-applied 006_add_feature_flags
Recording 006_add_feature_flags as applied (manual)...
  ✓ Migration marked as applied
```

4. **Restore from backup**:
```bash
$ acode db restore --from .agent/backups/acode_2024-01-20_100000.db
Restoring database from backup...
  ✓ Database restored
$ acode db migrate
```

---

### Issue 4: Rollback Fails with Missing Down Script (ACODE-MIG-004)

**Error Message:**
```
Error: Cannot rollback 006_add_feature_flags
Reason: Down script not found
Expected: .agent/migrations/006_add_feature_flags_down.sql
Code: ACODE-MIG-004
```

**Symptoms:**
- `acode db rollback` fails
- Cannot revert to previous version
- Rollback command shows "missing down script" error

**Causes:**
- Down script was never created
- Down script was deleted or moved
- Filename doesn't match pattern `XXX_name_down.sql`
- File permissions prevent reading

**Diagnostic Steps:**
```bash
# List migration files
$ ls -la .agent/migrations/

# Check for misnamed files
$ ls .agent/migrations/*006*

# Verify file permissions
$ ls -l .agent/migrations/006_add_feature_flags_down.sql
```

**Solutions:**

1. **Create the missing down script**:
```bash
# Create the file manually
$ cat > .agent/migrations/006_add_feature_flags_down.sql << 'EOF'
-- Rollback: Remove feature_flags table
DROP INDEX IF EXISTS idx_feature_flags_key;
DROP TABLE IF EXISTS feature_flags;
EOF

$ acode db rollback
```

2. **Manual rollback with SQL**:
```bash
# Execute rollback SQL directly
$ sqlite3 .agent/data/acode.db << 'EOF'
DROP INDEX IF EXISTS idx_feature_flags_key;
DROP TABLE IF EXISTS feature_flags;
DELETE FROM __migrations WHERE version = '006_add_feature_flags';
EOF
```

3. **Force skip rollback** (creates gap):
```bash
$ acode db rollback --skip 006
Skipping 006_add_feature_flags (no down script)
Rolling back 005_add_sync_status...
  ✓ Complete
```

4. **Require down scripts** (prevent future issues):
```yaml
# .agent/config.yml
database:
  migrations:
    security:
      requireDownScripts: true  # Fail on apply if down missing
```

---

### Issue 5: Inconsistent State After Failed Migration

**Error Message:**
```
Error: Database in inconsistent state
Version table shows: 006_add_feature_flags (partial)
Schema state: table exists, index missing
Code: ACODE-MIG-001
```

**Symptoms:**
- Schema partially applied (some tables exist, some don't)
- `acode db status` shows "partial" or "failed" state
- Application crashes with schema errors

**Causes:**
- DDL statement failed mid-migration
- Transaction not properly rolled back (PostgreSQL DDL limitations)
- Power failure or process crash during migration
- Disk full during migration

**Diagnostic Steps:**
```bash
# Check migration status
$ acode db status --verbose

# Inspect actual schema vs expected
$ acode db verify-schema

# Check for partial records
$ sqlite3 .agent/data/acode.db \
  "SELECT * FROM __migrations WHERE version LIKE '%006%'"
```

**Solutions:**

1. **Automatic recovery** (if supported):
```bash
$ acode db recover
Analyzing database state...
  Migration 006: 2/3 statements applied
  
Attempting recovery...
  Reverting partial changes...
  ✓ Recovery complete

Database restored to version 005_add_sync_status
```

2. **Manual schema cleanup**:
```bash
# Identify what was created
$ sqlite3 .agent/data/acode.db ".schema" | grep feature

# Remove partial objects
$ sqlite3 .agent/data/acode.db << 'EOF'
DROP TABLE IF EXISTS feature_flags;
DELETE FROM __migrations WHERE version = '006_add_feature_flags';
EOF

# Re-run migration
$ acode db migrate
```

3. **Restore from pre-migration backup**:
```bash
$ ls .agent/backups/
acode_2024-01-20_095959.db  # Before migration
acode_2024-01-20_100000.db  # After (corrupted)

$ acode db restore --from .agent/backups/acode_2024-01-20_095959.db
$ acode db migrate
```

4. **Create fixup migration**:
```sql
-- .agent/migrations/006b_fix_feature_flags.sql
-- Fixup: Complete partial 006 migration

-- Drop any partial state
DROP TABLE IF EXISTS feature_flags;
DROP INDEX IF EXISTS idx_feature_flags_key;

-- Recreate correctly
CREATE TABLE feature_flags (...);
CREATE INDEX idx_feature_flags_key ON feature_flags(key);
```

---

### Issue 6: Database Connection Failed (ACODE-MIG-007)

**Error Message:**
```
Error: Cannot connect to database
Provider: PostgreSQL
Host: localhost:5432
Details: Connection refused
Code: ACODE-MIG-007
```

**Symptoms:**
- All database commands fail
- Startup fails before migration check
- Network-related error messages

**Causes:**
- Database server not running
- Incorrect connection string
- Firewall blocking connection
- Authentication failure
- SSL/TLS configuration mismatch

**Diagnostic Steps:**
```bash
# Test PostgreSQL connection
$ psql -h localhost -p 5432 -U acode_user -d acode -c "SELECT 1"

# Check if PostgreSQL is running
$ pg_isready -h localhost -p 5432

# Test SQLite file access
$ sqlite3 .agent/data/acode.db "SELECT 1"
```

**Solutions:**

1. **Start database server**:
```bash
# PostgreSQL
$ sudo systemctl start postgresql

# Or Docker
$ docker start acode-postgres
```

2. **Fix connection configuration**:
```yaml
# .agent/config.yml
database:
  provider: postgresql
  postgresql:
    host: localhost      # Verify host
    port: 5432           # Verify port
    database: acode      # Verify database name
    username: acode_user
    password: ${ACODE_DB_PASSWORD}  # Check env var set
```

3. **Check authentication**:
```bash
# Verify password
$ echo $ACODE_DB_PASSWORD

# Test with password
$ PGPASSWORD=$ACODE_DB_PASSWORD psql -h localhost -U acode_user -d acode
```

4. **SQLite path issues**:
```bash
# Ensure directory exists
$ mkdir -p .agent/data

# Check file permissions
$ touch .agent/data/acode.db
$ chmod 644 .agent/data/acode.db
```

---

### Issue 7: Version Gap Detected (ACODE-MIG-006)

**Error Message:**
```
Error: Version gap detected in migrations
Applied: 001, 002, 003, 005
Missing: 004_add_approvals
Code: ACODE-MIG-006
```

**Symptoms:**
- Migration validation fails with "gap" error
- `acode db list` shows missing versions
- Out-of-order application detected

**Causes:**
- Migration file deleted after being applied
- Migrations merged in wrong order from branches
- Manual manipulation of __migrations table
- Migration skipped with `--skip` flag

**Diagnostic Steps:**
```bash
# List all migrations
$ acode db list --all

# Check for missing files
$ ls .agent/migrations/ | sort

# Check __migrations table
$ sqlite3 .agent/data/acode.db \
  "SELECT version, applied_at FROM __migrations ORDER BY version"
```

**Solutions:**

1. **Restore missing migration file**:
```bash
$ git checkout origin/main -- .agent/migrations/004_add_approvals.sql
$ acode db validate
```

2. **Insert missing record** (if already in schema):
```bash
$ acode db mark-applied 004_add_approvals --verify-schema
Verifying 004_add_approvals schema exists...
  ✓ approvals table found
Recording as applied...
  ✓ Migration marked as applied
```

3. **Ignore gaps** (for legacy databases):
```yaml
# .agent/config.yml
database:
  migrations:
    allowVersionGaps: true  # Not recommended
```

---

## Testing Requirements

### Unit Tests

#### MigrationDiscoveryTests.cs

```csharp
// tests/Acode.Infrastructure.Tests/Persistence/Migrations/MigrationDiscoveryTests.cs
namespace Acode.Infrastructure.Tests.Persistence.Migrations;

public sealed class MigrationDiscoveryTests
{
    private readonly MigrationDiscovery _sut;
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<IEmbeddedResourceProvider> _embeddedMock;
    private readonly Mock<ILogger<MigrationDiscovery>> _loggerMock;
    private readonly string _migrationsDir = "/app/.agent/migrations";
    
    public MigrationDiscoveryTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _embeddedMock = new Mock<IEmbeddedResourceProvider>();
        _loggerMock = new Mock<ILogger<MigrationDiscovery>>();
        
        _sut = new MigrationDiscovery(
            _fileSystemMock.Object,
            _embeddedMock.Object,
            _loggerMock.Object,
            Options.Create(new MigrationOptions { Directory = _migrationsDir }));
    }
    
    [Fact]
    public async Task DiscoverAsync_FindsEmbeddedMigrations()
    {
        // Arrange
        _embeddedMock.Setup(x => x.GetMigrationResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new EmbeddedResource("001_initial_schema.sql", "CREATE TABLE test (id TEXT);"),
                new EmbeddedResource("002_add_column.sql", "ALTER TABLE test ADD col TEXT;")
            });
        _fileSystemMock.Setup(x => x.GetFilesAsync(_migrationsDir, "*.sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        
        // Act
        var result = await _sut.DiscoverAsync(CancellationToken.None);
        
        // Assert
        result.Should().HaveCount(2);
        result[0].Version.Should().Be("001_initial_schema");
        result[1].Version.Should().Be("002_add_column");
        result.All(m => m.Source == MigrationSource.Embedded).Should().BeTrue();
    }
    
    [Fact]
    public async Task DiscoverAsync_FindsFileBasedMigrations()
    {
        // Arrange
        _embeddedMock.Setup(x => x.GetMigrationResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmbeddedResource>());
        _fileSystemMock.Setup(x => x.GetFilesAsync(_migrationsDir, "*.sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                "/app/.agent/migrations/003_add_feature.sql",
                "/app/.agent/migrations/003_add_feature_down.sql"
            });
        _fileSystemMock.Setup(x => x.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("CREATE TABLE feature (id TEXT);");
        
        // Act
        var result = await _sut.DiscoverAsync(CancellationToken.None);
        
        // Assert
        result.Should().HaveCount(1);
        result[0].Version.Should().Be("003_add_feature");
        result[0].Source.Should().Be(MigrationSource.File);
        result[0].HasDownScript.Should().BeTrue();
    }
    
    [Fact]
    public async Task DiscoverAsync_OrdersByVersionNumber()
    {
        // Arrange
        _embeddedMock.Setup(x => x.GetMigrationResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new EmbeddedResource("010_tenth.sql", "SQL"),
                new EmbeddedResource("002_second.sql", "SQL"),
                new EmbeddedResource("001_first.sql", "SQL")
            });
        _fileSystemMock.Setup(x => x.GetFilesAsync(_migrationsDir, "*.sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        
        // Act
        var result = await _sut.DiscoverAsync(CancellationToken.None);
        
        // Assert
        result.Should().HaveCount(3);
        result[0].Version.Should().Be("001_first");
        result[1].Version.Should().Be("002_second");
        result[2].Version.Should().Be("010_tenth");
    }
    
    [Fact]
    public async Task DiscoverAsync_PairsUpAndDownScripts()
    {
        // Arrange
        _embeddedMock.Setup(x => x.GetMigrationResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmbeddedResource>());
        _fileSystemMock.Setup(x => x.GetFilesAsync(_migrationsDir, "*.sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                "/app/.agent/migrations/001_create_table.sql",
                "/app/.agent/migrations/001_create_table_down.sql",
                "/app/.agent/migrations/002_no_down.sql"
            });
        _fileSystemMock.Setup(x => x.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("SQL content");
        
        // Act
        var result = await _sut.DiscoverAsync(CancellationToken.None);
        
        // Assert
        result.Should().HaveCount(2);
        result[0].HasDownScript.Should().BeTrue();
        result[1].HasDownScript.Should().BeFalse();
    }
    
    [Fact]
    public async Task DiscoverAsync_ThrowsOnDuplicateVersion()
    {
        // Arrange
        _embeddedMock.Setup(x => x.GetMigrationResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new EmbeddedResource("001_first.sql", "SQL")
            });
        _fileSystemMock.Setup(x => x.GetFilesAsync(_migrationsDir, "*.sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "/app/.agent/migrations/001_duplicate.sql" });
        _fileSystemMock.Setup(x => x.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("SQL");
        
        // Act
        var act = () => _sut.DiscoverAsync(CancellationToken.None);
        
        // Assert
        await act.Should().ThrowAsync<DuplicateMigrationVersionException>()
            .WithMessage("*001*");
    }
    
    [Fact]
    public async Task DiscoverAsync_LogsWarningForMissingDownScript()
    {
        // Arrange
        _embeddedMock.Setup(x => x.GetMigrationResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new EmbeddedResource("001_no_down.sql", "SQL") });
        _fileSystemMock.Setup(x => x.GetFilesAsync(_migrationsDir, "*.sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        
        // Act
        await _sut.DiscoverAsync(CancellationToken.None);
        
        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("001_no_down")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
```

#### MigrationRunnerTests.cs

```csharp
// tests/Acode.Infrastructure.Tests/Persistence/Migrations/MigrationRunnerTests.cs
namespace Acode.Infrastructure.Tests.Persistence.Migrations;

public sealed class MigrationRunnerTests
{
    private readonly MigrationRunner _sut;
    private readonly Mock<IMigrationDiscovery> _discoveryMock;
    private readonly Mock<IMigrationExecutor> _executorMock;
    private readonly Mock<IMigrationRepository> _repositoryMock;
    private readonly Mock<IMigrationLock> _lockMock;
    private readonly Mock<IChecksumValidator> _checksumMock;
    private readonly Mock<ILogger<MigrationRunner>> _loggerMock;
    
    public MigrationRunnerTests()
    {
        _discoveryMock = new Mock<IMigrationDiscovery>();
        _executorMock = new Mock<IMigrationExecutor>();
        _repositoryMock = new Mock<IMigrationRepository>();
        _lockMock = new Mock<IMigrationLock>();
        _checksumMock = new Mock<IChecksumValidator>();
        _loggerMock = new Mock<ILogger<MigrationRunner>>();
        
        _lockMock.Setup(x => x.TryAcquireAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _checksumMock.Setup(x => x.ValidateAsync(It.IsAny<IReadOnlyList<MigrationFile>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChecksumValidationResult(true, Array.Empty<ChecksumMismatch>()));
        
        _sut = new MigrationRunner(
            _discoveryMock.Object,
            _executorMock.Object,
            _repositoryMock.Object,
            _lockMock.Object,
            _checksumMock.Object,
            _loggerMock.Object);
    }
    
    [Fact]
    public async Task MigrateAsync_AppliesPendingMigrations()
    {
        // Arrange
        var migrations = new List<MigrationFile>
        {
            new("001_first", "CREATE TABLE t1 (id TEXT);", MigrationSource.Embedded, true),
            new("002_second", "CREATE TABLE t2 (id TEXT);", MigrationSource.Embedded, true)
        };
        
        _discoveryMock.Setup(x => x.DiscoverAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(migrations);
        _repositoryMock.Setup(x => x.GetAppliedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AppliedMigration>());
        
        // Act
        var result = await _sut.MigrateAsync(new MigrateOptions(), CancellationToken.None);
        
        // Assert
        result.AppliedCount.Should().Be(2);
        _executorMock.Verify(x => x.ExecuteAsync(migrations[0], It.IsAny<CancellationToken>()), Times.Once);
        _executorMock.Verify(x => x.ExecuteAsync(migrations[1], It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.RecordAsync(It.IsAny<MigrationFile>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
    
    [Fact]
    public async Task MigrateAsync_SkipsAlreadyApplied()
    {
        // Arrange
        var migrations = new List<MigrationFile>
        {
            new("001_first", "SQL", MigrationSource.Embedded, true),
            new("002_second", "SQL", MigrationSource.Embedded, true)
        };
        var applied = new[] { new AppliedMigration("001_first", "checksum", DateTime.UtcNow) };
        
        _discoveryMock.Setup(x => x.DiscoverAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(migrations);
        _repositoryMock.Setup(x => x.GetAppliedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(applied);
        
        // Act
        var result = await _sut.MigrateAsync(new MigrateOptions(), CancellationToken.None);
        
        // Assert
        result.AppliedCount.Should().Be(1);
        _executorMock.Verify(x => x.ExecuteAsync(It.Is<MigrationFile>(m => m.Version == "001_first"), It.IsAny<CancellationToken>()), Times.Never);
        _executorMock.Verify(x => x.ExecuteAsync(It.Is<MigrationFile>(m => m.Version == "002_second"), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task MigrateAsync_DryRunDoesNotExecute()
    {
        // Arrange
        var migrations = new List<MigrationFile>
        {
            new("001_first", "CREATE TABLE t1 (id TEXT);", MigrationSource.Embedded, true)
        };
        
        _discoveryMock.Setup(x => x.DiscoverAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(migrations);
        _repositoryMock.Setup(x => x.GetAppliedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AppliedMigration>());
        
        // Act
        var result = await _sut.MigrateAsync(new MigrateOptions { DryRun = true }, CancellationToken.None);
        
        // Assert
        result.AppliedCount.Should().Be(0);
        result.WouldApply.Should().HaveCount(1);
        _executorMock.Verify(x => x.ExecuteAsync(It.IsAny<MigrationFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task MigrateAsync_RollsBackOnFailure()
    {
        // Arrange
        var migrations = new List<MigrationFile>
        {
            new("001_first", "CREATE TABLE t1;", MigrationSource.Embedded, true)
        };
        
        _discoveryMock.Setup(x => x.DiscoverAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(migrations);
        _repositoryMock.Setup(x => x.GetAppliedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AppliedMigration>());
        _executorMock.Setup(x => x.ExecuteAsync(It.IsAny<MigrationFile>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SqlException("Syntax error"));
        
        // Act
        var act = () => _sut.MigrateAsync(new MigrateOptions(), CancellationToken.None);
        
        // Assert
        await act.Should().ThrowAsync<MigrationException>()
            .WithMessage("*001_first*")
            .Where(e => e.ErrorCode == "ACODE-MIG-001");
        _repositoryMock.Verify(x => x.RecordAsync(It.IsAny<MigrationFile>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task MigrateAsync_AcquiresAndReleasesLock()
    {
        // Arrange
        var sequence = new MockSequence();
        _lockMock.InSequence(sequence)
            .Setup(x => x.TryAcquireAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _lockMock.InSequence(sequence)
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask);
        
        _discoveryMock.Setup(x => x.DiscoverAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MigrationFile>());
        _repositoryMock.Setup(x => x.GetAppliedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AppliedMigration>());
        
        // Act
        await _sut.MigrateAsync(new MigrateOptions(), CancellationToken.None);
        
        // Assert
        _lockMock.Verify(x => x.TryAcquireAsync(It.IsAny<CancellationToken>()), Times.Once);
        _lockMock.Verify(x => x.DisposeAsync(), Times.Once);
    }
    
    [Fact]
    public async Task MigrateAsync_ThrowsOnLockTimeout()
    {
        // Arrange
        _lockMock.Setup(x => x.TryAcquireAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        // Act
        var act = () => _sut.MigrateAsync(new MigrateOptions(), CancellationToken.None);
        
        // Assert
        await act.Should().ThrowAsync<MigrationLockException>()
            .Where(e => e.ErrorCode == "ACODE-MIG-002");
    }
}
```

#### ChecksumValidatorTests.cs

```csharp
// tests/Acode.Infrastructure.Tests/Persistence/Migrations/ChecksumValidatorTests.cs
namespace Acode.Infrastructure.Tests.Persistence.Migrations;

public sealed class ChecksumValidatorTests
{
    private readonly SecureChecksumValidator _sut;
    private readonly Mock<IMigrationRepository> _repositoryMock;
    private readonly Mock<ILogger<SecureChecksumValidator>> _loggerMock;
    
    public ChecksumValidatorTests()
    {
        _repositoryMock = new Mock<IMigrationRepository>();
        _loggerMock = new Mock<ILogger<SecureChecksumValidator>>();
        _sut = new SecureChecksumValidator(_loggerMock.Object, _repositoryMock.Object);
    }
    
    [Fact]
    public void ComputeChecksum_ReturnsSha256Hash()
    {
        // Arrange
        const string content = "CREATE TABLE test (id TEXT);";
        
        // Act
        var checksum = SecureChecksumValidator.ComputeChecksum(content);
        
        // Assert
        checksum.Should().HaveLength(64); // SHA-256 = 64 hex chars
        checksum.Should().MatchRegex("^[a-f0-9]+$");
    }
    
    [Fact]
    public void ComputeChecksum_IsDeterministic()
    {
        // Arrange
        const string content = "SELECT 1;";
        
        // Act
        var checksum1 = SecureChecksumValidator.ComputeChecksum(content);
        var checksum2 = SecureChecksumValidator.ComputeChecksum(content);
        
        // Assert
        checksum1.Should().Be(checksum2);
    }
    
    [Fact]
    public void ComputeChecksum_DifferentContentProducesDifferentHash()
    {
        // Arrange
        const string content1 = "SELECT 1;";
        const string content2 = "SELECT 2;";
        
        // Act
        var checksum1 = SecureChecksumValidator.ComputeChecksum(content1);
        var checksum2 = SecureChecksumValidator.ComputeChecksum(content2);
        
        // Assert
        checksum1.Should().NotBe(checksum2);
    }
    
    [Fact]
    public async Task ValidateAsync_ReturnsValidForMatchingChecksums()
    {
        // Arrange
        const string content = "CREATE TABLE test;";
        var expectedChecksum = SecureChecksumValidator.ComputeChecksum(content);
        
        var migrations = new List<MigrationFile>
        {
            new("001_test", content, MigrationSource.Embedded, true)
        };
        var applied = new[]
        {
            new AppliedMigration("001_test", expectedChecksum, DateTime.UtcNow)
        };
        
        _repositoryMock.Setup(x => x.GetAppliedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(applied);
        
        // Act
        var result = await _sut.ValidateAsync(migrations, CancellationToken.None);
        
        // Assert
        result.IsValid.Should().BeTrue();
        result.Mismatches.Should().BeEmpty();
    }
    
    [Fact]
    public async Task ValidateAsync_DetectsMismatch()
    {
        // Arrange
        var migrations = new List<MigrationFile>
        {
            new("001_test", "MODIFIED CONTENT", MigrationSource.Embedded, true)
        };
        var applied = new[]
        {
            new AppliedMigration("001_test", "original_checksum_value", DateTime.UtcNow)
        };
        
        _repositoryMock.Setup(x => x.GetAppliedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(applied);
        
        // Act
        var result = await _sut.ValidateAsync(migrations, CancellationToken.None);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Mismatches.Should().HaveCount(1);
        result.Mismatches[0].Version.Should().Be("001_test");
    }
    
    [Fact]
    public async Task ValidateAsync_SkipsUnappliedMigrations()
    {
        // Arrange
        var migrations = new List<MigrationFile>
        {
            new("001_applied", "SQL", MigrationSource.Embedded, true),
            new("002_pending", "SQL", MigrationSource.Embedded, true)
        };
        var applied = new[]
        {
            new AppliedMigration("001_applied", SecureChecksumValidator.ComputeChecksum("SQL"), DateTime.UtcNow)
        };
        
        _repositoryMock.Setup(x => x.GetAppliedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(applied);
        
        // Act
        var result = await _sut.ValidateAsync(migrations, CancellationToken.None);
        
        // Assert
        result.IsValid.Should().BeTrue();
    }
}
```

#### MigrationLockTests.cs

```csharp
// tests/Acode.Infrastructure.Tests/Persistence/Migrations/MigrationLockTests.cs
namespace Acode.Infrastructure.Tests.Persistence.Migrations;

public sealed class MigrationLockTests
{
    private readonly Mock<IConnectionFactory> _connectionFactoryMock;
    private readonly Mock<ILogger<DistributedMigrationLock>> _loggerMock;
    
    public MigrationLockTests()
    {
        _connectionFactoryMock = new Mock<IConnectionFactory>();
        _loggerMock = new Mock<ILogger<DistributedMigrationLock>>();
    }
    
    [Fact]
    public async Task TryAcquireAsync_ReturnsTrue_WhenLockAcquired()
    {
        // Arrange
        var sut = CreateSut(TimeSpan.FromSeconds(5));
        SetupSuccessfulLockAcquisition();
        
        // Act
        var result = await sut.TryAcquireAsync(CancellationToken.None);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task TryAcquireAsync_ReturnsFalse_AfterTimeout()
    {
        // Arrange
        var sut = CreateSut(TimeSpan.FromMilliseconds(100));
        SetupFailedLockAcquisition();
        
        // Act
        var result = await sut.TryAcquireAsync(CancellationToken.None);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public async Task DisposeAsync_ReleasesLock()
    {
        // Arrange
        var sut = CreateSut(TimeSpan.FromSeconds(5));
        SetupSuccessfulLockAcquisition();
        await sut.TryAcquireAsync(CancellationToken.None);
        
        // Act
        await sut.DisposeAsync();
        
        // Assert
        _connectionFactoryMock.Verify(
            x => x.ReleaseLockAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    private DistributedMigrationLock CreateSut(TimeSpan timeout)
    {
        return new DistributedMigrationLock(
            _connectionFactoryMock.Object,
            _loggerMock.Object,
            timeout);
    }
    
    private void SetupSuccessfulLockAcquisition()
    {
        _connectionFactoryMock.Setup(x => x.TryAcquireLockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }
    
    private void SetupFailedLockAcquisition()
    {
        _connectionFactoryMock.Setup(x => x.TryAcquireLockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }
}
```

### Integration Tests

```csharp
// tests/Acode.Integration.Tests/Persistence/Migrations/MigrationRunnerIntegrationTests.cs
namespace Acode.Integration.Tests.Persistence.Migrations;

[Collection("Database")]
public sealed class MigrationRunnerIntegrationTests : IAsyncLifetime
{
    private readonly SqliteTestDatabase _database;
    private readonly IMigrationService _sut;
    
    public MigrationRunnerIntegrationTests()
    {
        _database = new SqliteTestDatabase();
        var services = new ServiceCollection();
        services.AddMigrationServices(_database.ConnectionString);
        var provider = services.BuildServiceProvider();
        _sut = provider.GetRequiredService<IMigrationService>();
    }
    
    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }
    
    [Fact]
    public async Task MigrateAsync_AppliesAllPendingMigrations()
    {
        // Arrange
        await AddMigrationFile("001_create_test", "CREATE TABLE test (id TEXT PRIMARY KEY);");
        await AddMigrationFile("002_add_column", "ALTER TABLE test ADD name TEXT;");
        
        // Act
        var result = await _sut.MigrateAsync(new MigrateOptions(), CancellationToken.None);
        
        // Assert
        result.AppliedCount.Should().Be(2);
        result.Success.Should().BeTrue();
        
        var status = await _sut.GetStatusAsync(CancellationToken.None);
        status.AppliedMigrations.Should().HaveCount(2);
        status.PendingMigrations.Should().BeEmpty();
    }
    
    [Fact]
    public async Task RollbackAsync_RollsBackMultipleSteps()
    {
        // Arrange
        await AddMigrationFile("001_create_test", "CREATE TABLE test (id TEXT);", 
            "DROP TABLE test;");
        await AddMigrationFile("002_create_other", "CREATE TABLE other (id TEXT);",
            "DROP TABLE other;");
        await _sut.MigrateAsync(new MigrateOptions(), CancellationToken.None);
        
        // Act
        var result = await _sut.RollbackAsync(
            new RollbackOptions { Steps = 2 }, 
            CancellationToken.None);
        
        // Assert
        result.RolledBackCount.Should().Be(2);
        
        var status = await _sut.GetStatusAsync(CancellationToken.None);
        status.AppliedMigrations.Should().BeEmpty();
    }
    
    [Fact]
    public async Task MigrateAsync_HandlesTransactionRollbackOnFailure()
    {
        // Arrange
        await AddMigrationFile("001_valid", "CREATE TABLE valid (id TEXT);");
        await AddMigrationFile("002_invalid", "INVALID SQL STATEMENT;");
        
        // Act
        var act = () => _sut.MigrateAsync(new MigrateOptions(), CancellationToken.None);
        
        // Assert
        await act.Should().ThrowAsync<MigrationException>();
        
        // Verify first migration was applied
        var status = await _sut.GetStatusAsync(CancellationToken.None);
        status.AppliedMigrations.Should().HaveCount(1);
        status.AppliedMigrations[0].Version.Should().Be("001_valid");
    }
    
    [Fact]
    public async Task MigrateAsync_HandlesConcurrentMigrations()
    {
        // Arrange
        await AddMigrationFile("001_test", "CREATE TABLE test (id TEXT);");
        
        // Act - Run two migrations concurrently
        var task1 = _sut.MigrateAsync(new MigrateOptions(), CancellationToken.None);
        var task2 = _sut.MigrateAsync(new MigrateOptions(), CancellationToken.None);
        
        var results = await Task.WhenAll(
            Task.Run(async () => { try { return await task1; } catch { return null; } }),
            Task.Run(async () => { try { return await task2; } catch { return null; } }));
        
        // Assert - One should succeed, one should wait or fail gracefully
        results.Count(r => r?.AppliedCount == 1).Should().Be(1);
        
        var status = await _sut.GetStatusAsync(CancellationToken.None);
        status.AppliedMigrations.Should().HaveCount(1);
    }
    
    private async Task AddMigrationFile(string version, string upSql, string? downSql = null)
    {
        var dir = Path.Combine(_database.DataDir, "migrations");
        Directory.CreateDirectory(dir);
        
        await File.WriteAllTextAsync(Path.Combine(dir, $"{version}.sql"), upSql);
        if (downSql != null)
        {
            await File.WriteAllTextAsync(Path.Combine(dir, $"{version}_down.sql"), downSql);
        }
    }
}
```

### E2E Tests

```csharp
// tests/Acode.E2E.Tests/Migrations/MigrationE2ETests.cs
namespace Acode.E2E.Tests.Migrations;

public sealed class MigrationE2ETests : E2ETestBase
{
    [Fact]
    public async Task Startup_BootstrapsMigrationsAutomatically()
    {
        // Arrange
        await CreateConfigFile(new { database = new { autoMigrate = true } });
        await CreateMigrationFile("001_initial", "CREATE TABLE startup_test (id TEXT);");
        
        // Act
        var result = await RunAcodeAsync("run", "--test-mode", "echo hello");
        
        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Applying 001_initial");
        result.Output.Should().Contain("Migration complete");
        
        await AssertTableExists("startup_test");
    }
    
    [Fact]
    public async Task DbCreate_CreatesNewMigrationFiles()
    {
        // Arrange & Act
        var result = await RunAcodeAsync("db", "create", "add_users");
        
        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Created migration files");
        
        var migrationsDir = Path.Combine(TestDir, ".agent", "migrations");
        Directory.GetFiles(migrationsDir, "*add_users.sql").Should().HaveCount(1);
        Directory.GetFiles(migrationsDir, "*add_users_down.sql").Should().HaveCount(1);
    }
    
    [Fact]
    public async Task DbStatus_ShowsMigrationStatus()
    {
        // Arrange
        await CreateMigrationFile("001_applied", "CREATE TABLE t1 (id TEXT);");
        await CreateMigrationFile("002_pending", "CREATE TABLE t2 (id TEXT);");
        await RunAcodeAsync("db", "migrate", "--target", "001");
        
        // Act
        var result = await RunAcodeAsync("db", "status");
        
        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Applied: 1");
        result.Output.Should().Contain("Pending: 1");
        result.Output.Should().Contain("001_applied");
        result.Output.Should().Contain("002_pending");
    }
    
    [Fact]
    public async Task DbMigrateDryRun_ShowsPreview()
    {
        // Arrange
        await CreateMigrationFile("001_test", "CREATE TABLE dryrun_test (id TEXT);");
        
        // Act
        var result = await RunAcodeAsync("db", "migrate", "--dry-run");
        
        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Would apply");
        result.Output.Should().Contain("001_test");
        result.Output.Should().Contain("No changes made");
        
        await AssertTableNotExists("dryrun_test");
    }
}
```

### Performance Benchmarks

```csharp
// tests/Acode.Benchmarks/Migrations/MigrationBenchmarks.cs
namespace Acode.Benchmarks.Migrations;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class MigrationBenchmarks
{
    private IMigrationService _migrationService = null!;
    private SqliteTestDatabase _database = null!;
    
    [GlobalSetup]
    public async Task Setup()
    {
        _database = new SqliteTestDatabase();
        await _database.InitializeAsync();
        
        var services = new ServiceCollection();
        services.AddMigrationServices(_database.ConnectionString);
        var provider = services.BuildServiceProvider();
        _migrationService = provider.GetRequiredService<IMigrationService>();
        
        // Pre-apply some migrations for status check benchmark
        for (int i = 1; i <= 10; i++)
        {
            await AddMigration($"{i:D3}_migration_{i}", $"CREATE TABLE t{i} (id TEXT);");
        }
        await _migrationService.MigrateAsync(new MigrateOptions(), CancellationToken.None);
    }
    
    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _database.DisposeAsync();
    }
    
    [Benchmark(Description = "Status check (10 applied)")]
    public async Task<MigrationStatus> StatusCheck()
    {
        return await _migrationService.GetStatusAsync(CancellationToken.None);
    }
    
    [Benchmark(Description = "Single migration apply")]
    public async Task<MigrationResult> SingleMigration()
    {
        await AddMigration("999_benchmark", "CREATE TABLE benchmark (id TEXT);");
        var result = await _migrationService.MigrateAsync(new MigrateOptions(), CancellationToken.None);
        await _migrationService.RollbackAsync(new RollbackOptions { Steps = 1 }, CancellationToken.None);
        return result;
    }
    
    [Benchmark(Description = "Checksum validation")]
    public async Task<ChecksumValidationResult> ChecksumValidation()
    {
        var discovery = _migrationService as MigrationRunner;
        var files = await discovery!.DiscoverAsync(CancellationToken.None);
        return await discovery.ValidateChecksumsAsync(files, CancellationToken.None);
    }
    
    private async Task AddMigration(string version, string sql)
    {
        var dir = Path.Combine(_database.DataDir, "migrations");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, $"{version}.sql"), sql);
    }
}
```

### Performance Targets

| Benchmark | Target (P50) | Maximum (P99) | Memory Budget |
|-----------|--------------|---------------|---------------|
| Status check (10 applied) | 25ms | 50ms | <1 MB |
| Status check (100 applied) | 100ms | 250ms | <2 MB |
| Single migration apply | 50ms | 200ms | <5 MB |
| Checksum validation (10 files) | 10ms | 25ms | <1 MB |
| Lock acquisition | 5ms | 50ms | <100 KB |
| Startup migration check | 50ms | 100ms | <2 MB |
| Rollback single | 25ms | 100ms | <2 MB |

---

## User Verification Steps

### Scenario 1: Auto-Bootstrap on Startup

**Objective:** Verify migrations apply automatically when acode starts.

**Prerequisites:**
- Fresh acode installation or clean database state
- `database.autoMigrate: true` in configuration

**Steps:**

1. Create a new migration file:
   ```bash
   mkdir -p .agent/migrations
   cat > .agent/migrations/001_test_table.sql << 'EOF'
   CREATE TABLE verification_test (
       id TEXT PRIMARY KEY,
       name TEXT NOT NULL,
       created_at TEXT DEFAULT (datetime('now'))
   );
   EOF
   ```

2. Start acode with any command:
   ```bash
   acode run "Hello world"
   ```

3. Observe startup output

**Expected Output:**
```
╔═══════════════════════════════════════════════════╗
║            Database Migration Check               ║
╠═══════════════════════════════════════════════════╣
║ Pending: 1 migration                              ║
╚═══════════════════════════════════════════════════╝

Applying 001_test_table...
  ✓ Applied in 23ms

Starting agent...
```

**Verification:**
```bash
sqlite3 .agent/data/acode.db ".tables"
# Should show: verification_test

sqlite3 .agent/data/acode.db "SELECT * FROM __migrations"
# Should show: 001_test_table with timestamp and checksum
```

---

### Scenario 2: Manual Migration with Status Check

**Objective:** Verify CLI migration workflow with status checking.

**Prerequisites:**
- `database.autoMigrate: false` or pending migration created after startup

**Steps:**

1. Create migration file:
   ```bash
   cat > .agent/migrations/002_add_column.sql << 'EOF'
   ALTER TABLE verification_test ADD email TEXT;
   EOF
   ```

2. Check current status:
   ```bash
   acode db status
   ```

3. Observe pending migration listed

4. Apply migration:
   ```bash
   acode db migrate
   ```

5. Verify status again:
   ```bash
   acode db status
   ```

**Expected Output (Step 2):**
```
Current Version: 001_test_table
Applied: 1 migration
Pending: 1 migration

Pending:
  002_add_column
```

**Expected Output (Step 4):**
```
Applying migrations...
  002_add_column...
    ✓ Applied in 15ms
    
All migrations applied.
```

**Expected Output (Step 5):**
```
Current Version: 002_add_column
Applied: 2 migrations
Pending: 0 migrations
```

---

### Scenario 3: Rollback Single Migration

**Objective:** Verify rollback functionality with down script.

**Prerequisites:**
- At least one migration applied
- Down script exists for the migration

**Steps:**

1. Ensure down script exists:
   ```bash
   cat > .agent/migrations/002_add_column_down.sql << 'EOF'
   -- SQLite workaround for dropping column
   CREATE TABLE verification_test_new AS 
   SELECT id, name, created_at FROM verification_test;
   DROP TABLE verification_test;
   ALTER TABLE verification_test_new RENAME TO verification_test;
   EOF
   ```

2. Execute rollback:
   ```bash
   acode db rollback
   ```

3. Confirm when prompted (or use `--yes`)

4. Verify status:
   ```bash
   acode db status
   ```

**Expected Output (Step 2):**
```
Rolling back 002_add_column...
  ✓ Rollback complete in 45ms

Current version: 001_test_table
```

**Verification:**
```bash
sqlite3 .agent/data/acode.db ".schema verification_test"
# Should NOT show email column
```

---

### Scenario 4: Dry-Run Preview

**Objective:** Verify dry-run shows SQL without applying changes.

**Steps:**

1. Re-create the migration (if rolled back):
   ```bash
   cat > .agent/migrations/002_add_column.sql << 'EOF'
   ALTER TABLE verification_test ADD email TEXT;
   CREATE INDEX idx_test_email ON verification_test(email);
   EOF
   ```

2. Run dry-run:
   ```bash
   acode db migrate --dry-run
   ```

3. Verify database unchanged:
   ```bash
   acode db status
   ```

**Expected Output (Step 2):**
```
╔═══════════════════════════════════════════════════╗
║            Migration Preview (Dry Run)             ║
╚═══════════════════════════════════════════════════╝

Would apply: 002_add_column

SQL Statements:
────────────────────────────────────────────────────
ALTER TABLE verification_test ADD email TEXT;
CREATE INDEX idx_test_email ON verification_test(email);
────────────────────────────────────────────────────

⚠ DRY RUN - No changes were made to the database.
```

**Verification:**
- Status still shows 1 pending migration
- `email` column does NOT exist in table

---

### Scenario 5: Create New Migration

**Objective:** Verify `db create` generates proper migration files.

**Steps:**

1. Create new migration:
   ```bash
   acode db create add_user_preferences
   ```

2. List generated files:
   ```bash
   ls -la .agent/migrations/*preferences*
   ```

3. Inspect file content:
   ```bash
   cat .agent/migrations/003_add_user_preferences.sql
   ```

**Expected Output (Step 1):**
```
╔═══════════════════════════════════════════════════╗
║            Creating New Migration                  ║
╚═══════════════════════════════════════════════════╝

Created migration files:
  📄 .agent/migrations/003_add_user_preferences.sql
  📄 .agent/migrations/003_add_user_preferences_down.sql

Edit the files and run: acode db migrate
```

**Verification:**
- Both `.sql` and `_down.sql` files exist
- Version number is sequential (003)
- Files contain template comments

---

### Scenario 6: Checksum Validation

**Objective:** Verify checksum mismatch is detected when files are modified.

**Steps:**

1. Apply a migration first:
   ```bash
   acode db migrate
   ```

2. Modify an applied migration file:
   ```bash
   echo "-- Malicious modification" >> .agent/migrations/001_test_table.sql
   ```

3. Run validation:
   ```bash
   acode db validate
   ```

4. Attempt to migrate:
   ```bash
   acode db migrate
   ```

**Expected Output (Step 3):**
```
╔═══════════════════════════════════════════════════╗
║            Checksum Validation                     ║
╚═══════════════════════════════════════════════════╝

  001_test_table    ✗ MISMATCH
    Expected: a1b2c3d4e5f6...
    Actual:   z9y8x7w6v5u4...
    
⚠ SECURITY WARNING: 1 migration file has been modified after application.
This could indicate tampering. Restore original files or use --force to bypass.
```

**Expected Output (Step 4):**
```
Error: Checksum mismatch detected for 001_test_table
Code: ACODE-MIG-003

The migration file has been modified since it was applied.
Restore the original file or run with --force to bypass validation.
```

**Cleanup:**
```bash
git checkout .agent/migrations/001_test_table.sql
```

---

### Scenario 7: Lock Contention Handling

**Objective:** Verify graceful handling when another process holds the lock.

**Steps:**

1. In terminal 1, simulate long-running migration:
   ```bash
   # Create a lock file manually
   echo "test_lock" > .agent/data/.migration-lock
   ```

2. In terminal 2, attempt migration:
   ```bash
   acode db migrate
   ```

3. Observe timeout behavior (wait 60 seconds or use short timeout)

4. Clean up lock:
   ```bash
   rm .agent/data/.migration-lock
   ```

**Expected Output (Terminal 2):**
```
Acquiring migration lock...
  ⏳ Waiting for lock (attempt 1/60)...
  ⏳ Waiting for lock (attempt 2/60)...
  ...
  
Error: Could not acquire migration lock within 60 seconds
Code: ACODE-MIG-002

Another process may be running migrations. 
Check for stale locks with: acode db unlock --force
```

---

### Scenario 8: Multi-Step Rollback

**Objective:** Verify rolling back multiple migrations in reverse order.

**Steps:**

1. Ensure multiple migrations are applied:
   ```bash
   acode db status
   # Should show 3+ applied migrations
   ```

2. Rollback 2 steps:
   ```bash
   acode db rollback --steps 2
   ```

3. Verify state:
   ```bash
   acode db status
   ```

**Expected Output (Step 2):**
```
Rolling back migrations...

Rolling back 003_add_user_preferences...
  ✓ Complete (12ms)

Rolling back 002_add_column...
  ✓ Complete (18ms)

Rolled back 2 migrations.
Current version: 001_test_table
```

---

### Scenario 9: Target Version Migration

**Objective:** Verify migrating to a specific version (not all pending).

**Steps:**

1. Create multiple pending migrations:
   ```bash
   acode db create feature_a
   acode db create feature_b  
   acode db create feature_c
   ```

2. Migrate only to feature_b:
   ```bash
   acode db migrate --target 002_feature_b
   ```

3. Verify state:
   ```bash
   acode db status
   ```

**Expected Output (Step 2):**
```
Applying migrations to target version 002_feature_b...

  001_feature_a...
    ✓ Applied
  002_feature_b...
    ✓ Applied
    
Applied 2 of 3 available migrations.
Remaining: 003_feature_c
```

---

### Scenario 10: Backup Before Migration

**Objective:** Verify automatic backup creation before migrations.

**Prerequisites:**
- Configure backup:
  ```yaml
  database:
    migrations:
      backup:
        enabled: true
        directory: .agent/backups
  ```

**Steps:**

1. Apply a migration:
   ```bash
   acode db migrate
   ```

2. Check backup directory:
   ```bash
   ls -la .agent/backups/
   ```

**Expected Output (Step 1):**
```
Creating pre-migration backup...
  ✓ Backup created: .agent/backups/acode_2024-01-20_100000.db

Applying migrations...
  ...
```

**Verification:**
```bash
ls .agent/backups/
# Should show: acode_2024-01-20_100000.db (or similar timestamp)

# Backup should be restorable
sqlite3 .agent/backups/acode_*.db ".tables"
```

---

## Implementation Prompt

### File Structure

```
src/Acode.Application/
├── Database/
│   ├── IMigrationService.cs
│   ├── MigrationOptions.cs
│   ├── MigrationStatus.cs
│   ├── MigrationResult.cs
│   └── MigrationException.cs

src/Acode.Infrastructure/
├── Persistence/
│   └── Migrations/
│       ├── MigrationRunner.cs
│       ├── MigrationDiscovery.cs
│       ├── MigrationExecutor.cs
│       ├── MigrationRepository.cs
│       ├── MigrationLock.cs
│       ├── DistributedMigrationLock.cs
│       ├── ChecksumValidator.cs
│       ├── MigrationSqlValidator.cs
│       ├── PrivilegeEscalationDetector.cs
│       ├── MigrationLockGuard.cs
│       ├── MigrationFile.cs
│       ├── AppliedMigration.cs
│       ├── MigrationSource.cs
│       └── DependencyInjection/
│           └── MigrationServiceCollectionExtensions.cs

src/Acode.Cli/
└── Commands/
    ├── DbCommand.cs
    ├── DbStatusCommand.cs
    ├── DbMigrateCommand.cs
    ├── DbRollbackCommand.cs
    ├── DbCreateCommand.cs
    ├── DbValidateCommand.cs
    └── DbUnlockCommand.cs

tests/Acode.Infrastructure.Tests/
└── Persistence/
    └── Migrations/
        ├── MigrationDiscoveryTests.cs
        ├── MigrationRunnerTests.cs
        ├── ChecksumValidatorTests.cs
        └── MigrationLockTests.cs

tests/Acode.Integration.Tests/
└── Persistence/
    └── Migrations/
        └── MigrationRunnerIntegrationTests.cs
```

### Domain Models

```csharp
// src/Acode.Application/Database/MigrationFile.cs
namespace Acode.Application.Database;

/// <summary>
/// Represents a discovered migration file with its content and metadata.
/// </summary>
public sealed record MigrationFile
{
    public required string Version { get; init; }
    public required string UpContent { get; init; }
    public string? DownContent { get; init; }
    public required MigrationSource Source { get; init; }
    public bool HasDownScript => DownContent is not null;
    public required string Checksum { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public DateTime? CreatedAt { get; init; }
}

public enum MigrationSource
{
    Embedded,
    File
}
```

```csharp
// src/Acode.Application/Database/AppliedMigration.cs
namespace Acode.Application.Database;

/// <summary>
/// Represents a migration that has been applied to the database.
/// </summary>
public sealed record AppliedMigration
{
    public required string Version { get; init; }
    public required string Checksum { get; init; }
    public required DateTime AppliedAt { get; init; }
    public required TimeSpan Duration { get; init; }
    public string? AppliedBy { get; init; }
    public MigrationStatus Status { get; init; } = MigrationStatus.Applied;
}

public enum MigrationStatus
{
    Applied,
    Skipped,
    Failed,
    Partial
}
```

```csharp
// src/Acode.Application/Database/MigrationOptions.cs
namespace Acode.Application.Database;

public sealed record MigrateOptions
{
    public bool DryRun { get; init; } = false;
    public string? TargetVersion { get; init; }
    public string? SkipVersion { get; init; }
    public bool Force { get; init; } = false;
    public bool SkipChecksum { get; init; } = false;
    public bool CreateBackup { get; init; } = true;
}

public sealed record RollbackOptions
{
    public int Steps { get; init; } = 1;
    public string? TargetVersion { get; init; }
    public bool DryRun { get; init; } = false;
    public bool Force { get; init; } = false;
    public bool Confirm { get; init; } = false;
}

public sealed record CreateOptions
{
    public required string Name { get; init; }
    public string? Template { get; init; }
    public bool NoDown { get; init; } = false;
}
```

### Service Interfaces

```csharp
// src/Acode.Application/Database/IMigrationService.cs
namespace Acode.Application.Database;

/// <summary>
/// Primary interface for database migration operations.
/// </summary>
public interface IMigrationService
{
    /// <summary>
    /// Gets the current migration status including applied and pending migrations.
    /// </summary>
    Task<MigrationStatusReport> GetStatusAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Applies pending migrations to the database.
    /// </summary>
    Task<MigrateResult> MigrateAsync(MigrateOptions options, CancellationToken ct = default);
    
    /// <summary>
    /// Rolls back applied migrations.
    /// </summary>
    Task<RollbackResult> RollbackAsync(RollbackOptions options, CancellationToken ct = default);
    
    /// <summary>
    /// Creates new migration files with the specified name.
    /// </summary>
    Task<CreateResult> CreateAsync(CreateOptions options, CancellationToken ct = default);
    
    /// <summary>
    /// Validates checksums of all applied migrations.
    /// </summary>
    Task<ValidationResult> ValidateAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Forces release of a stale migration lock.
    /// </summary>
    Task<bool> ForceUnlockAsync(CancellationToken ct = default);
}
```

```csharp
// src/Acode.Application/Database/IMigrationDiscovery.cs
namespace Acode.Application.Database;

public interface IMigrationDiscovery
{
    /// <summary>
    /// Discovers all available migrations from embedded resources and file system.
    /// </summary>
    Task<IReadOnlyList<MigrationFile>> DiscoverAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets only pending (not yet applied) migrations.
    /// </summary>
    Task<IReadOnlyList<MigrationFile>> GetPendingAsync(CancellationToken ct = default);
}
```

```csharp
// src/Acode.Application/Database/IMigrationRepository.cs
namespace Acode.Application.Database;

public interface IMigrationRepository
{
    /// <summary>
    /// Gets all applied migrations from the version table.
    /// </summary>
    Task<IReadOnlyList<AppliedMigration>> GetAppliedAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Records a newly applied migration.
    /// </summary>
    Task RecordAsync(MigrationFile migration, TimeSpan duration, CancellationToken ct = default);
    
    /// <summary>
    /// Removes a migration record (for rollback).
    /// </summary>
    Task RemoveAsync(string version, CancellationToken ct = default);
    
    /// <summary>
    /// Ensures the __migrations table exists.
    /// </summary>
    Task EnsureVersionTableAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Updates checksum for an existing migration (use with caution).
    /// </summary>
    Task UpdateChecksumAsync(string version, string newChecksum, CancellationToken ct = default);
}
```

```csharp
// src/Acode.Application/Database/IMigrationLock.cs
namespace Acode.Application.Database;

public interface IMigrationLock : IAsyncDisposable
{
    /// <summary>
    /// Attempts to acquire the migration lock.
    /// </summary>
    /// <returns>True if lock acquired, false if timeout.</returns>
    Task<bool> TryAcquireAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Forces release of a potentially stale lock.
    /// </summary>
    Task ForceReleaseAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets information about the current lock holder, if any.
    /// </summary>
    Task<LockInfo?> GetLockInfoAsync(CancellationToken ct = default);
}

public sealed record LockInfo(
    string LockId,
    string HolderId,
    DateTime AcquiredAt,
    string? MachineName);
```

### Result Types

```csharp
// src/Acode.Application/Database/MigrationResults.cs
namespace Acode.Application.Database;

public sealed record MigrationStatusReport
{
    public required string? CurrentVersion { get; init; }
    public required IReadOnlyList<AppliedMigration> AppliedMigrations { get; init; }
    public required IReadOnlyList<MigrationFile> PendingMigrations { get; init; }
    public required string DatabaseProvider { get; init; }
    public required bool ChecksumsValid { get; init; }
    public IReadOnlyList<string>? ChecksumWarnings { get; init; }
}

public sealed record MigrateResult
{
    public required bool Success { get; init; }
    public required int AppliedCount { get; init; }
    public required TimeSpan TotalDuration { get; init; }
    public IReadOnlyList<MigrationFile>? AppliedMigrations { get; init; }
    public IReadOnlyList<MigrationFile>? WouldApply { get; init; } // For dry-run
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
}

public sealed record RollbackResult
{
    public required bool Success { get; init; }
    public required int RolledBackCount { get; init; }
    public required TimeSpan TotalDuration { get; init; }
    public string? CurrentVersion { get; init; }
    public IReadOnlyList<string>? RolledBackVersions { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed record CreateResult
{
    public required bool Success { get; init; }
    public required string Version { get; init; }
    public required string UpFilePath { get; init; }
    public required string DownFilePath { get; init; }
}

public sealed record ValidationResult
{
    public required bool IsValid { get; init; }
    public required IReadOnlyList<ChecksumMismatch> Mismatches { get; init; }
}

public sealed record ChecksumMismatch(
    string Version,
    string ExpectedChecksum,
    string ActualChecksum,
    DateTime AppliedAt);
```

### Exception Types

```csharp
// src/Acode.Application/Database/MigrationExceptions.cs
namespace Acode.Application.Database;

public class MigrationException : Exception
{
    public string ErrorCode { get; }
    public string? MigrationVersion { get; }
    
    public MigrationException(string errorCode, string message, string? migrationVersion = null)
        : base(message)
    {
        ErrorCode = errorCode;
        MigrationVersion = migrationVersion;
    }
    
    public MigrationException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

public sealed class MigrationLockException : MigrationException
{
    public TimeSpan Timeout { get; }
    
    public MigrationLockException(TimeSpan timeout)
        : base("ACODE-MIG-002", $"Could not acquire migration lock within {timeout.TotalSeconds} seconds")
    {
        Timeout = timeout;
    }
}

public sealed class ChecksumMismatchException : MigrationException
{
    public string ExpectedChecksum { get; }
    public string ActualChecksum { get; }
    
    public ChecksumMismatchException(string version, string expected, string actual)
        : base("ACODE-MIG-003", 
            $"Migration {version} checksum mismatch. Expected: {expected}, Actual: {actual}", 
            version)
    {
        ExpectedChecksum = expected;
        ActualChecksum = actual;
    }
}

public sealed class MissingDownScriptException : MigrationException
{
    public MissingDownScriptException(string version)
        : base("ACODE-MIG-004", 
            $"Cannot rollback {version}: down script not found", 
            version)
    {
    }
}

public sealed class RollbackException : MigrationException
{
    public RollbackException(string version, string message, Exception? inner = null)
        : base("ACODE-MIG-005", message, inner!)
    {
        MigrationVersion = version;
    }
    
    public new string? MigrationVersion { get; }
}
```

### Core Implementation

```csharp
// src/Acode.Infrastructure/Persistence/Migrations/MigrationRunner.cs
namespace Acode.Infrastructure.Persistence.Migrations;

public sealed class MigrationRunner : IMigrationService
{
    private readonly IMigrationDiscovery _discovery;
    private readonly IMigrationRepository _repository;
    private readonly IMigrationExecutor _executor;
    private readonly IMigrationLockFactory _lockFactory;
    private readonly IChecksumValidator _checksumValidator;
    private readonly IMigrationSqlValidator _sqlValidator;
    private readonly IBackupService _backupService;
    private readonly ILogger<MigrationRunner> _logger;
    private readonly MigrationSettings _settings;
    
    public MigrationRunner(
        IMigrationDiscovery discovery,
        IMigrationRepository repository,
        IMigrationExecutor executor,
        IMigrationLockFactory lockFactory,
        IChecksumValidator checksumValidator,
        IMigrationSqlValidator sqlValidator,
        IBackupService backupService,
        ILogger<MigrationRunner> logger,
        IOptions<MigrationSettings> settings)
    {
        _discovery = discovery;
        _repository = repository;
        _executor = executor;
        _lockFactory = lockFactory;
        _checksumValidator = checksumValidator;
        _sqlValidator = sqlValidator;
        _backupService = backupService;
        _logger = logger;
        _settings = settings.Value;
    }
    
    public async Task<MigrateResult> MigrateAsync(
        MigrateOptions options,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Acquire distributed lock
        await using var migrationLock = _lockFactory.Create();
        if (!await migrationLock.TryAcquireAsync(ct))
        {
            throw new MigrationLockException(_settings.Lock.Timeout);
        }
        
        _logger.LogInformation("Migration lock acquired");
        
        try
        {
            // Ensure version table exists
            await _repository.EnsureVersionTableAsync(ct);
            
            // Discover all migrations
            var allMigrations = await _discovery.DiscoverAsync(ct);
            var applied = await _repository.GetAppliedAsync(ct);
            var appliedVersions = applied.Select(a => a.Version).ToHashSet();
            
            // Validate checksums unless skipped
            if (!options.SkipChecksum && !options.Force)
            {
                var validation = await _checksumValidator.ValidateAsync(allMigrations, ct);
                if (!validation.IsValid)
                {
                    var mismatch = validation.Mismatches.First();
                    throw new ChecksumMismatchException(
                        mismatch.Version, 
                        mismatch.ExpectedChecksum, 
                        mismatch.ActualChecksum);
                }
            }
            
            // Filter to pending migrations
            var pending = allMigrations
                .Where(m => !appliedVersions.Contains(m.Version))
                .OrderBy(m => m.Version)
                .ToList();
            
            // Apply target filter if specified
            if (options.TargetVersion is not null)
            {
                pending = pending
                    .TakeWhile(m => string.CompareOrdinal(m.Version, options.TargetVersion) <= 0)
                    .ToList();
            }
            
            // Skip specified version if requested
            if (options.SkipVersion is not null)
            {
                pending = pending
                    .Where(m => m.Version != options.SkipVersion)
                    .ToList();
            }
            
            if (pending.Count == 0)
            {
                _logger.LogInformation("No pending migrations");
                return new MigrateResult
                {
                    Success = true,
                    AppliedCount = 0,
                    TotalDuration = stopwatch.Elapsed
                };
            }
            
            // Dry-run mode: return what would be applied
            if (options.DryRun)
            {
                _logger.LogInformation("Dry-run mode: would apply {Count} migrations", pending.Count);
                return new MigrateResult
                {
                    Success = true,
                    AppliedCount = 0,
                    TotalDuration = stopwatch.Elapsed,
                    WouldApply = pending
                };
            }
            
            // Create backup before migration
            if (options.CreateBackup && _settings.Backup.Enabled)
            {
                _logger.LogInformation("Creating pre-migration backup");
                await _backupService.CreateBackupAsync(ct);
            }
            
            // Apply each pending migration
            var appliedMigrations = new List<MigrationFile>();
            
            foreach (var migration in pending)
            {
                _logger.LogInformation("Applying migration {Version}", migration.Version);
                
                // Validate SQL for security patterns
                var sqlValidation = _sqlValidator.Validate(migration.UpContent, migration.Version);
                if (!sqlValidation.IsValid && !options.Force)
                {
                    throw new MigrationException(
                        "ACODE-MIG-009",
                        $"Migration {migration.Version} contains forbidden SQL patterns: {string.Join(", ", sqlValidation.Errors)}",
                        migration.Version);
                }
                
                var migrationStopwatch = Stopwatch.StartNew();
                
                await _executor.ExecuteAsync(migration, ct);
                
                migrationStopwatch.Stop();
                await _repository.RecordAsync(migration, migrationStopwatch.Elapsed, ct);
                
                appliedMigrations.Add(migration);
                _logger.LogInformation(
                    "Applied migration {Version} in {Duration}ms",
                    migration.Version,
                    migrationStopwatch.ElapsedMilliseconds);
            }
            
            stopwatch.Stop();
            _logger.LogInformation(
                "Migration complete: {Count} migrations in {Duration}ms",
                appliedMigrations.Count,
                stopwatch.ElapsedMilliseconds);
            
            return new MigrateResult
            {
                Success = true,
                AppliedCount = appliedMigrations.Count,
                TotalDuration = stopwatch.Elapsed,
                AppliedMigrations = appliedMigrations
            };
        }
        catch (Exception ex) when (ex is not MigrationException)
        {
            _logger.LogError(ex, "Migration failed");
            throw new MigrationException("ACODE-MIG-001", $"Migration failed: {ex.Message}", ex);
        }
    }
    
    public async Task<RollbackResult> RollbackAsync(
        RollbackOptions options,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        await using var migrationLock = _lockFactory.Create();
        if (!await migrationLock.TryAcquireAsync(ct))
        {
            throw new MigrationLockException(_settings.Lock.Timeout);
        }
        
        try
        {
            var applied = await _repository.GetAppliedAsync(ct);
            var allMigrations = await _discovery.DiscoverAsync(ct);
            var migrationsByVersion = allMigrations.ToDictionary(m => m.Version);
            
            // Determine which migrations to rollback
            IEnumerable<AppliedMigration> toRollback;
            
            if (options.TargetVersion is not null)
            {
                toRollback = applied
                    .Where(a => string.CompareOrdinal(a.Version, options.TargetVersion) > 0)
                    .OrderByDescending(a => a.Version);
            }
            else
            {
                toRollback = applied
                    .OrderByDescending(a => a.Version)
                    .Take(options.Steps);
            }
            
            var rollbackList = toRollback.ToList();
            
            if (rollbackList.Count == 0)
            {
                return new RollbackResult
                {
                    Success = true,
                    RolledBackCount = 0,
                    TotalDuration = stopwatch.Elapsed
                };
            }
            
            // Dry-run mode
            if (options.DryRun)
            {
                return new RollbackResult
                {
                    Success = true,
                    RolledBackCount = 0,
                    TotalDuration = stopwatch.Elapsed,
                    RolledBackVersions = rollbackList.Select(r => r.Version).ToList()
                };
            }
            
            var rolledBack = new List<string>();
            
            foreach (var migration in rollbackList)
            {
                if (!migrationsByVersion.TryGetValue(migration.Version, out var file))
                {
                    throw new MigrationException(
                        "ACODE-MIG-001",
                        $"Migration file not found for {migration.Version}",
                        migration.Version);
                }
                
                if (!file.HasDownScript)
                {
                    throw new MissingDownScriptException(migration.Version);
                }
                
                _logger.LogInformation("Rolling back {Version}", migration.Version);
                
                await _executor.ExecuteDownAsync(file, ct);
                await _repository.RemoveAsync(migration.Version, ct);
                
                rolledBack.Add(migration.Version);
            }
            
            var remainingApplied = await _repository.GetAppliedAsync(ct);
            var currentVersion = remainingApplied
                .OrderByDescending(a => a.Version)
                .FirstOrDefault()?.Version;
            
            return new RollbackResult
            {
                Success = true,
                RolledBackCount = rolledBack.Count,
                TotalDuration = stopwatch.Elapsed,
                CurrentVersion = currentVersion,
                RolledBackVersions = rolledBack
            };
        }
        catch (Exception ex) when (ex is not MigrationException)
        {
            throw new RollbackException("unknown", $"Rollback failed: {ex.Message}", ex);
        }
    }
    
    // ... additional methods for GetStatusAsync, CreateAsync, ValidateAsync, ForceUnlockAsync
}
```

### Dependency Injection Setup

```csharp
// src/Acode.Infrastructure/Persistence/Migrations/DependencyInjection/MigrationServiceCollectionExtensions.cs
namespace Acode.Infrastructure.Persistence.Migrations.DependencyInjection;

public static class MigrationServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind settings
        services.Configure<MigrationSettings>(
            configuration.GetSection("Database:Migrations"));
        
        // Core services
        services.AddSingleton<IMigrationService, MigrationRunner>();
        services.AddSingleton<IMigrationDiscovery, MigrationDiscovery>();
        services.AddSingleton<IMigrationRepository, MigrationRepository>();
        services.AddSingleton<IMigrationExecutor, MigrationExecutor>();
        
        // Lock services
        services.AddSingleton<IMigrationLockFactory, MigrationLockFactory>();
        services.AddTransient<IMigrationLock, DistributedMigrationLock>();
        
        // Validation services
        services.AddSingleton<IChecksumValidator, SecureChecksumValidator>();
        services.AddSingleton<IMigrationSqlValidator, MigrationSqlValidator>();
        services.AddSingleton<IPrivilegeEscalationDetector, PrivilegeEscalationDetector>();
        
        // Backup service
        services.AddSingleton<IBackupService, DatabaseBackupService>();
        
        // Startup bootstrap
        services.AddHostedService<MigrationBootstrapService>();
        
        return services;
    }
}
```

### Startup Bootstrap

```csharp
// src/Acode.Infrastructure/Persistence/Migrations/MigrationBootstrapService.cs
namespace Acode.Infrastructure.Persistence.Migrations;

public sealed class MigrationBootstrapService : IHostedService
{
    private readonly IMigrationService _migrationService;
    private readonly ILogger<MigrationBootstrapService> _logger;
    private readonly MigrationSettings _settings;
    
    public MigrationBootstrapService(
        IMigrationService migrationService,
        ILogger<MigrationBootstrapService> logger,
        IOptions<MigrationSettings> settings)
    {
        _migrationService = migrationService;
        _logger = logger;
        _settings = settings.Value;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking database migrations...");
        
        var status = await _migrationService.GetStatusAsync(cancellationToken);
        
        _logger.LogInformation(
            "Database status - Applied: {Applied}, Pending: {Pending}",
            status.AppliedMigrations.Count,
            status.PendingMigrations.Count);
        
        if (status.PendingMigrations.Count == 0)
        {
            _logger.LogInformation("Database is up to date");
            return;
        }
        
        if (!_settings.AutoMigrate)
        {
            _logger.LogWarning(
                "Auto-migrate disabled. {Count} pending migrations. Run 'acode db migrate' to apply.",
                status.PendingMigrations.Count);
            return;
        }
        
        _logger.LogInformation("Auto-applying {Count} pending migrations", status.PendingMigrations.Count);
        
        var result = await _migrationService.MigrateAsync(
            new MigrateOptions { CreateBackup = true },
            cancellationToken);
        
        if (!result.Success)
        {
            _logger.LogError("Migration failed: {Error}", result.ErrorMessage);
            throw new MigrationException(
                result.ErrorCode ?? "ACODE-MIG-001",
                $"Startup migration failed: {result.ErrorMessage}");
        }
        
        _logger.LogInformation(
            "Applied {Count} migrations in {Duration}ms",
            result.AppliedCount,
            result.TotalDuration.TotalMilliseconds);
    }
    
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

### Error Codes Reference

| Code | Name | Description | Resolution |
|------|------|-------------|------------|
| ACODE-MIG-001 | MigrationFailed | Migration execution failed | Check SQL syntax, database state |
| ACODE-MIG-002 | LockTimeout | Could not acquire migration lock | Wait or force unlock |
| ACODE-MIG-003 | ChecksumMismatch | Migration file modified | Restore file or reset checksum |
| ACODE-MIG-004 | MissingDownScript | Rollback script not found | Create down script |
| ACODE-MIG-005 | RollbackFailed | Rollback execution failed | Check down script, manual fix |
| ACODE-MIG-006 | VersionGap | Missing migration version | Restore file or mark applied |
| ACODE-MIG-007 | ConnectionFailed | Database connection failed | Check config, database running |
| ACODE-MIG-008 | BackupFailed | Pre-migration backup failed | Check disk space, permissions |
| ACODE-MIG-009 | ForbiddenSql | Security pattern detected | Review SQL, use --force if safe |

### Implementation Checklist

1. [ ] **Domain Models**
   - [ ] Create MigrationFile record
   - [ ] Create AppliedMigration record
   - [ ] Create MigrationOptions records
   - [ ] Create result types

2. [ ] **Exception Types**
   - [ ] Create MigrationException base
   - [ ] Create MigrationLockException
   - [ ] Create ChecksumMismatchException
   - [ ] Create MissingDownScriptException
   - [ ] Create RollbackException

3. [ ] **Discovery**
   - [ ] Implement embedded resource scanning
   - [ ] Implement file system scanning
   - [ ] Implement version ordering
   - [ ] Implement up/down pairing

4. [ ] **Repository**
   - [ ] Implement EnsureVersionTableAsync
   - [ ] Implement GetAppliedAsync
   - [ ] Implement RecordAsync
   - [ ] Implement RemoveAsync

5. [ ] **Locking**
   - [ ] Implement SQLite file lock
   - [ ] Implement PostgreSQL advisory lock
   - [ ] Implement timeout handling
   - [ ] Implement stale lock detection

6. [ ] **Validation**
   - [ ] Implement SHA-256 checksum
   - [ ] Implement SQL pattern validation
   - [ ] Implement privilege escalation detection

7. [ ] **Executor**
   - [ ] Implement transaction wrapper
   - [ ] Implement statement splitting
   - [ ] Implement error handling

8. [ ] **Runner**
   - [ ] Implement MigrateAsync
   - [ ] Implement RollbackAsync
   - [ ] Implement GetStatusAsync
   - [ ] Implement CreateAsync
   - [ ] Implement ValidateAsync

9. [ ] **Bootstrap**
   - [ ] Implement MigrationBootstrapService
   - [ ] Wire into startup pipeline

10. [ ] **CLI Commands**
    - [ ] Implement db status
    - [ ] Implement db migrate
    - [ ] Implement db rollback
    - [ ] Implement db create
    - [ ] Implement db validate
    - [ ] Implement db unlock

11. [ ] **Unit Tests**
    - [ ] MigrationDiscoveryTests
    - [ ] MigrationRunnerTests
    - [ ] ChecksumValidatorTests
    - [ ] MigrationLockTests

12. [ ] **Integration Tests**
    - [ ] SQLite migration tests
    - [ ] Concurrent migration tests
    - [ ] Rollback tests

13. [ ] **E2E Tests**
    - [ ] Startup bootstrap tests
    - [ ] CLI command tests

### Rollout Plan

| Phase | Component | Duration | Dependencies |
|-------|-----------|----------|--------------|
| 1 | Domain models & exceptions | 1 day | None |
| 2 | Discovery service | 1 day | Phase 1 |
| 3 | Repository (version table) | 1 day | Phase 1 |
| 4 | Executor | 1 day | Phase 1, task-050b |
| 5 | Locking service | 1 day | Phase 1 |
| 6 | Checksum validation | 0.5 day | Phase 1 |
| 7 | SQL validation | 0.5 day | Phase 1 |
| 8 | Migration runner | 2 days | Phases 2-7 |
| 9 | Bootstrap service | 0.5 day | Phase 8 |
| 10 | CLI commands | 1.5 days | Phase 8 |
| 11 | Unit tests | 2 days | All phases |
| 12 | Integration tests | 1 day | All phases |

**Total Estimated Duration:** 12 developer-days

---

**End of Task 050.c Specification**