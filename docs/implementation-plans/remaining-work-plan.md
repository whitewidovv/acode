# Remaining Work Implementation Plan

## Status: Task 049f, 050d, 050e pending

## Context: 77k tokens remaining

## Completed So Far
✅ Tasks 050a, 050b, 050c (all 7 phases) - 602 tests passing
✅ Fixed 2 pre-existing ConfigE2ETests failures
✅ All tests GREEN (607 total)

## Remaining Tasks (IN ORDER)

### Task 049f: SQLite-Postgres Sync Engine (COMPLEXITY: 13)
**File**: `docs/tasks/refined-tasks/Epic 02/task-049f-sqlite-postgres-sync-engine.md`
**Lines**: ~4000+ (comprehensive spec)
**Dependencies**: Task 049a (Data Model), Task 050 (DB Foundation)

**Following GAP_ANALYSIS_METHODOLOGY.md - Phase 3: Read Complete Specification**

#### Step 1: Locate Critical Sections
```bash
wc -l docs/tasks/refined-tasks/Epic\ 02/task-049f-sqlite-postgres-sync-engine.md
grep -n "## Implementation Prompt" task-049f*.md
grep -n "## Testing Requirements" task-049f*.md
grep -n "## Acceptance Criteria" task-049f*.md
```

#### Step 2: Read Specification Sections (in order)
1. Description (lines ~1-200) - Architecture, outbox pattern, batch processing
2. Acceptance Criteria (lines ~600-900) - Feature checklist
3. Testing Requirements (lines ~2000-2500) - Complete test implementations
4. Implementation Prompt (lines ~3000-3500) - Complete production code

#### Step 3: Implementation Plan (TDD)

**Phase 1: Domain Models**
- OutboxEntry record (entity_id, operation, idempotency_key, payload, status, retry_count)
- SyncStatus enum (PENDING, IN_PROGRESS, SYNCED, FAILED)
- SyncOperation enum (CREATE, UPDATE, DELETE)
- PrivacyLevel enum (LOCAL_ONLY, REDACTED, FULL)

**Phase 2: Outbox Repository**
- IOutboxRepository interface
- SqliteOutboxRepository implementation
  - InsertAsync(OutboxEntry)
  - GetPendingAsync(int batchSize)
  - MarkSyncedAsync(string[] ids)
  - MarkFailedAsync(string id, string error)
- Tests: 10-15 tests

**Phase 3: Batch Processor**
- IBatchProcessor interface
- BatchProcessor implementation
  - ProcessBatchAsync()
  - ApplyPrivacyFilters()
  - GroupByEntityType()
- Tests: 8-12 tests

**Phase 4: Retry Engine**
- IRetryPolicy interface
- ExponentialBackoffPolicy implementation
  - ShouldRetry(int attemptCount, Exception ex)
  - GetBackoffDelay(int attemptCount)
- Tests: 6-8 tests

**Phase 5: Sync Service**
- ISyncService interface
- SyncService orchestration
  - StartAsync() - background worker
  - StopAsync() - graceful shutdown
  - GetStatusAsync() - queue depth, last sync
- Tests: 10-12 tests

**Phase 6: HTTP Client**
- ISyncHttpClient interface
- SyncHttpClient implementation
  - PostBatchAsync(SyncBatch)
  - Idempotency headers
  - Timeout handling
- Tests: 8-10 tests

**Estimated**: 50-70 tests, 15-20 files, 3000-4000 lines of code

---

### Task 050d: Health Checks + Diagnostics (COMPLEXITY: 8)
**File**: `docs/tasks/refined-tasks/Epic 02/task-050d-health-checks-diagnostics.md`
**Dependencies**: Task 050 (DB Foundation), Task 049f (Sync Engine)

**Following GAP_ANALYSIS_METHODOLOGY.md**

#### Implementation Plan (TDD)

**Phase 1: Health Check Framework**
- IHealthCheck interface
- IHealthCheckRegistry interface
- HealthCheckResult record
- HealthStatus enum (Healthy, Degraded, Unhealthy)
- CompositeHealthResult aggregator
- Tests: 8-10 tests

**Phase 2: Database Health Checks**
- DatabaseConnectivityCheck (SELECT 1)
- DatabaseSchemaCheck (migration version)
- DatabasePoolCheck (connection pool stats)
- DatabaseLockCheck (detect long-running locks)
- Tests: 12-15 tests

**Phase 3: Sync Health Checks**
- SyncQueueDepthCheck (outbox count)
- SyncLagCheck (time since last sync)
- SyncErrorRateCheck (failed sync percentage)
- Tests: 8-10 tests

**Phase 4: Storage Health Checks**
- DiskSpaceCheck (available space threshold)
- DatabaseSizeCheck (current DB size)
- WALSizeCheck (SQLite WAL growth)
- Tests: 8-10 tests

**Phase 5: Health Check Caching**
- HealthCheckCache with TTL
- Cache key per check
- TTL varies by status (Healthy: 30s, Degraded: 10s, Unhealthy: 5s)
- Tests: 6-8 tests

**Phase 6: Registry Implementation**
- HealthCheckRegistry with parallel execution
- Timeout per check (default 5s)
- Exception handling (catch all, return Unhealthy)
- Tests: 10-12 tests

**Estimated**: 50-65 tests, 12-15 files, 2000-2500 lines of code

---

### Task 050e: Backup + Export Hooks (COMPLEXITY: 5)
**File**: `docs/tasks/refined-tasks/Epic 02/task-050e-backup-export-hooks.md`
**Dependencies**: Task 050 (DB Foundation)

**Following GAP_ANALYSIS_METHODOLOGY.md**

#### Implementation Plan (TDD)

**Phase 1: Backup Service**
- IBackupService interface
- BackupResult record
- BackupService implementation
  - CreateBackupAsync(string targetPath)
  - SQLite: VACUUM INTO
  - PostgreSQL: pg_dump
  - Compression (gzip)
  - Timestamp in filename
- Tests: 8-10 tests

**Phase 2: Export Service**
- IExportService interface
- ExportFormat enum (JSON, CSV, SQL)
- ExportService implementation
  - ExportConversationsAsync(format, filter)
  - ExportMessagesAsync(format, filter)
- Tests: 10-12 tests

**Phase 3: Pre-Migration Backup Hook**
- IMigrationHook interface
- PreMigrationBackupHook implementation
  - OnBeforeMigration(MigrationFile)
  - Create backup if configured
  - Skip if backup disabled
- Tests: 6-8 tests

**Phase 4: Retention Policy**
- IRetentionPolicy interface
- BackupRetentionPolicy implementation
  - CleanupOldBackups(int retentionDays)
  - Keep last N backups
- Tests: 6-8 tests

**Estimated**: 30-38 tests, 8-10 files, 1200-1500 lines of code

---

## Total Estimated Remaining Work
- **Tests**: 130-173 tests
- **Files**: 35-45 files
- **Code**: 6200-8000 lines
- **Commits**: 15-20 commits (one per logical unit)

## Execution Strategy

1. **Context Management**: This plan allows resuming at any phase if context runs out
2. **TDD**: RED → GREEN → REFACTOR for every feature
3. **Commit Frequency**: After each completed phase (not waiting for full task)
4. **Test First**: Write tests before implementation for every component
5. **Gap Analysis**: For each task, follow GAP_ANALYSIS_METHODOLOGY.md phases 1-6

## Next Action
Start with task 049f Phase 1 (Domain Models) following TDD approach.
