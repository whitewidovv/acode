# Task-011b Fresh Gap Analysis: Persistence Model (SQLite + PostgreSQL)

**Status:** ✅ GAP ANALYSIS COMPLETE - ~5% COMPLETE (3 files / 28+ expected, 0/59 ACs verified)
**Date:** 2026-01-15
**Analyzed By:** Claude Code (Established 050b Pattern)
**Methodology:** CLAUDE.md Section 3.2 + GAP_ANALYSIS_METHODOLOGY.md
**Spec Reference:** docs/tasks/refined-tasks/Epic 02/task-011b-persistence-model-sqlite-postgres.md (2223 lines)

---

## EXECUTIVE SUMMARY

**Semantic Completeness: ~5% (3 production files / 28+ expected, 0/59 ACs verified)**

**Current State:**
- ✅ Connection factories partially implemented (3 files: SqliteConnectionFactory, PostgresConnectionFactory, ConnectionFactorySelector)
- ❌ Core persistence tier missing: RunStateStore implementations, Outbox infrastructure per spec
- ❌ Sync service infrastructure missing: SyncService, OutboxProcessor, ConflictResolver
- ❌ All migration infrastructure either not implemented or in wrong locations
- ❌ No IRunStateStore interface found (spec required)
- ❌ IOutboxRepository exists but IOutbox interface not found (spec required)
- ❌ Test infrastructure mostly missing (0-5 tests for persistence persistence layer per spec)

**Result:** Task-011b is ~95% incomplete with only low-level connection factories implemented. All 59 ACs remain unverified. Most infrastructure needs to be built from specification.

---

## SECTION 1: SPECIFICATION SUMMARY

### Acceptance Criteria (59 total ACs)

**SQLite Storage (AC-001-008):** 8 ACs ✅ Requirements
- Database in .agent/workspace.db, WAL mode, STRICT tables, auto-create, transactional writes, 30s timeout, concurrent reads, exclusive write locks

**Schema Management (AC-009-014):** 6 ACs ✅ Requirements
- Schema version tracking, versioned migrations, auto-migrate on startup, idempotent migrations, failure halts startup, queryable version

**Tables (AC-015-022):** 8 ACs ✅ Requirements
- sessions, session_events, session_tasks, steps, tool_calls, artifacts, outbox tables with timestamps

**PostgreSQL (AC-023-028):** 6 ACs ✅ Requirements
- Configurable via config, connection string from env, optional (doesn't fail startup), schema mirrors SQLite, indexes optimized, pool size configurable

**Outbox Pattern (AC-029-035):** 7 ACs ✅ Requirements
- idempotency_key, entity_type, entity_id, payload, created_at, processed_at, attempts tracking

**Sync Process (AC-036-042):** 7 ACs ✅ Requirements
- Background sync, non-blocking, oldest first, retries failed, exponential backoff, max 10 attempts, resumable after restart

**Idempotency (AC-043-047):** 5 ACs ✅ Requirements
- Unique keys generated, key format correct, duplicates rejected, rejection marks processed, logged

**Conflict Resolution (AC-048-052):** 5 ACs ✅ Requirements
- Conflicts detected, latest timestamp wins, logged, counted, details preserved

**Performance (AC-053-056):** 4 ACs ✅ Requirements
- SQLite write < 50ms, read < 10ms, sync 100 records/sec, pagination query < 100ms

**Security (AC-057-059):** 3 ACs ✅ Requirements
- File permissions 600, connection string not logged, SQL injection prevented

### Expected Production Files (28+ total)

**MISSING - Application Layer (2 files):**
- src/Acode.Application/Sessions/IRunStateStore.cs (interface) - 20 lines
- src/Acode.Application/Sessions/IOutbox.cs (interface) - 30 lines

**MISSING/INCOMPLETE - Infrastructure Persistence Layer (13 files):**
- src/Acode.Infrastructure/Persistence/SQLite/SQLiteConnectionFactory.cs - ✅ EXISTS (113 lines) - **VERIFIED COMPLETE**
- src/Acode.Infrastructure/Persistence/SQLite/SQLiteRunStateStore.cs - ❌ MISSING (100 lines)
- src/Acode.Infrastructure/Persistence/SQLite/SQLiteOutbox.cs - ❌ MISSING (80 lines)
- src/Acode.Infrastructure/Persistence/SQLite/Migrations/IMigration.cs - ❌ MISSING (15 lines)
- src/Acode.Infrastructure/Persistence/SQLite/Migrations/MigrationRunner.cs - ❌ MISSING (120 lines)
- src/Acode.Infrastructure/Persistence/SQLite/Migrations/V1_0_0_InitialSchema.cs - ❌ MISSING (150 lines)
- src/Acode.Infrastructure/Persistence/SQLite/Migrations/V1_1_0_AddArtifacts.cs - ❌ MISSING (50 lines)
- src/Acode.Infrastructure/Persistence/PostgreSQL/PostgresConnectionFactory.cs - ✅ EXISTS (169 lines) - **VERIFIED COMPLETE**
- src/Acode.Infrastructure/Persistence/PostgreSQL/PostgresRunStateStore.cs - ❌ MISSING (110 lines)
- src/Acode.Infrastructure/Persistence/PostgreSQL/PostgresSyncTarget.cs - ❌ MISSING (90 lines)
- src/Acode.Infrastructure/Persistence/Sync/ISyncService.cs - ❌ MISSING (interface, 20 lines)
- src/Acode.Infrastructure/Persistence/Sync/SyncService.cs - ❌ MISSING (200 lines)
- src/Acode.Infrastructure/Persistence/Sync/ConflictResolver.cs - ❌ MISSING (80 lines)
- src/Acode.Infrastructure/Persistence/ConnectionFactorySelector.cs - ✅ EXISTS (50 lines) - **VERIFIED PARTIAL** (wiring only)

**Outbox Infrastructure:**
- Existing: src/Acode.Infrastructure/Sync/SqliteOutboxRepository.cs (partial)
- Missing from spec structure: OutboxProcessor, proper outbox implementation per two-tier model spec

### Expected Test Files (5 files with 24+ test methods)

**MISSING - Unit Tests:**
- tests/Acode.Infrastructure.Tests/Persistence/SQLiteConnectionTests.cs - ❌ MISSING (3 test methods)
- tests/Acode.Infrastructure.Tests/Persistence/MigrationRunnerTests.cs - ❌ MISSING (3 test methods)
- tests/Acode.Infrastructure.Tests/Persistence/SessionRepositoryTests.cs - ❌ MISSING (4 test methods)
- tests/Acode.Infrastructure.Tests/Persistence/OutboxTests.cs - ❌ MISSING (3 test methods)
- tests/Acode.Infrastructure.Tests/Persistence/SyncServiceTests.cs - ❌ MISSING (3 test methods)

**MISSING - Integration Tests (nested under Persistence):**
- tests/Acode.Infrastructure.Tests/Persistence/SQLiteIntegrationTests.cs - ❌ MISSING (3 test methods)
- tests/Acode.Infrastructure.Tests/Persistence/PostgreSQLIntegrationTests.cs - ❌ MISSING (3 test methods)
- tests/Acode.Infrastructure.Tests/Persistence/MigrationIntegrationTests.cs - ❌ MISSING (2 test methods)

**MISSING - E2E Tests:**
- tests/Acode.Infrastructure.Tests/Persistence/OfflineOperationTests.cs - ❌ MISSING (3 test methods)

**MISSING - Performance Benchmarks:**
- Benchmarks/ folder with 5 benchmark scenarios (SQLite write, read, sync, query, migration)

**Test Status:**
- Expected total: 24+ test methods + 5 benchmarks
- Currently existing for persistence: 0 tests dedicated to 011b persistence model
- Missing: 100% of dedicated persistence test coverage

---

## SECTION 2: CURRENT IMPLEMENTATION STATE (VERIFIED)

### ✅ COMPLETE Files (1 file - 3% of production code)

**SqliteConnectionFactory.cs** (113 lines)
- ✅ File exists at: src/Acode.Infrastructure/Persistence/Connections/SqliteConnectionFactory.cs
- ✅ No NotImplementedException
- ✅ Implements connection factory pattern
- ✅ Methods: Create(), ConfigureWAL(), (but spec requires more comprehensive implementation)
- **Status**: COMPLETE for connection factory, but limited to connection creation only

**PostgresConnectionFactory.cs** (169 lines)
- ✅ File exists at: src/Acode.Infrastructure/Persistence/Connections/PostgresConnectionFactory.cs
- ✅ No NotImplementedException
- ✅ Implements connection factory pattern
- **Status**: COMPLETE for connection factory pattern, but incomplete per spec (needs pool management, SSL handling)

### ⚠️ INCOMPLETE Files (1 file - 2% partial implementation)

**ConnectionFactorySelector.cs** (50 lines)
- ✅ File exists at: src/Acode.Infrastructure/Persistence/Connections/ConnectionFactorySelector.cs
- ✅ No NotImplementException
- ❌ Very minimal implementation - just wiring layer
- ❌ Spec requires full IRunStateStore abstraction with factory pattern
- **Status**: PARTIAL - provides selector but not full RunStateStore abstraction

### ❌ MISSING Files (25+ files - 97% of production code)

**Application Layer (2 files):**
- IRunStateStore.cs - MISSING - **CRITICAL BLOCKER** - all infrastructure depends on this
- IOutbox.cs - MISSING - **CRITICAL** - outbox pattern requires this interface

**SQLite Implementation (7 files):**
- SQLiteRunStateStore.cs - MISSING - Must implement IRunStateStore for SQLite
- SQLiteOutbox.cs - MISSING - Must implement IOutbox for SQLite
- IMigration.cs - MISSING - Interface for migration system
- MigrationRunner.cs - MISSING - Executes migrations systematically
- V1_0_0_InitialSchema.cs - MISSING - Initial database schema
- V1_1_0_AddArtifacts.cs - MISSING - Schema migration for artifacts

**PostgreSQL Implementation (3 files):**
- PostgresRunStateStore.cs - MISSING - Must implement IRunStateStore for PostgreSQL
- PostgresSyncTarget.cs - MISSING - Receives sync data from outbox
- (Schema files - need matching PostgreSQL schema)

**Sync Infrastructure (3+ files):**
- ISyncService.cs - MISSING - **CRITICAL** - sync orchestration interface
- SyncService.cs - MISSING - **CRITICAL** - background sync worker
- ConflictResolver.cs - MISSING - Conflict resolution logic
- OutboxProcessor.cs - MISSING - Processes outbox records with backoff

**Migration/Infrastructure (rest of ~8-10 files):**
- Schema SQL files for SQLite
- Schema SQL files for PostgreSQL
- Error handling infrastructure
- CLI command implementations (db status, sync, migrate)

### Test Files Status

**ALL TEST FILES MISSING:**
- SQLiteConnectionTests - 0 tests (expected 3)
- MigrationRunnerTests - 0 tests (expected 3)
- SessionRepositoryTests - 0 tests (expected 4)
- OutboxTests - 0 tests (expected 3)
- SyncServiceTests - 0 tests (expected 3)
- SQLiteIntegrationTests - 0 tests (expected 3)
- PostgreSQLIntegrationTests - 0 tests (expected 3)
- MigrationIntegrationTests - 0 tests (expected 2)
- OfflineOperationTests - 0 tests (expected 3)

**Summary**: 0/24+ expected test methods implemented

---

## SECTION 3: SEMANTIC COMPLETENESS VERIFICATION

### AC-by-AC Mapping (0/59 verified - 0% completion)

**BLOCKED** - Cannot verify any ACs because:
1. IRunStateStore interface missing (required by all AC verification)
2. IOutbox interface missing (required for outbox ACs)
3. SQLiteRunStateStore not implemented (required for SQLite ACs)
4. SyncService not implemented (required for sync ACs)

**Unverifiable Without Implementation:**
- AC-001 to AC-008: SQLite storage - Cannot verify without SQLiteRunStateStore
- AC-009 to AC-014: Schema management - Cannot verify without MigrationRunner
- AC-015 to AC-022: Tables - Cannot verify without schema files
- AC-023 to AC-028: PostgreSQL - Cannot verify without PostgresRunStateStore
- AC-029 to AC-035: Outbox - Cannot verify without IOutbox implementation
- AC-036 to AC-042: Sync - Cannot verify without SyncService
- AC-043 to AC-047: Idempotency - Cannot verify without SyncService
- AC-048 to AC-052: Conflicts - Cannot verify without ConflictResolver
- AC-053 to AC-056: Performance - Cannot verify without implementations
- AC-057 to AC-059: Security - Cannot verify without implementations

---

## SECTION 4: BUILD & TEST STATUS

**Build Status:**
```
✅ Build succeeds (0 errors, 0 warnings)
Note: Build passes because connection factories exist, but core persistence tier is missing
```

**Test Status:**
```
❌ Tests for task-011b persistence model: 0/24+ expected
- Passing: 0
- Failing: 0 (no tests exist)
- Total expected: 24+ test methods + 5 benchmarks

Note: Existing tests in codebase for other persistence work are passing
```

**Production Code Status:**
```
❌ Task-011b production implementation: ~3% complete
- Files expected per spec: 28+
- Files found: 3 (2 connection factories + 1 selector)
- Files missing: 25+ (87%)
- Tests expected: 24+
- Tests found: 0
```

---

## CRITICAL GAPS & BLOCKING DEPENDENCIES

### **CRITICAL BLOCKERS - Must implement FIRST:**

1. **Missing IRunStateStore Interface (Application Layer)** - **BLOCKS EVERYTHING**
   - Impact: All 28+ infrastructure files depend on this interface
   - Must be defined before any persistence implementation can proceed
   - Estimated effort: 1-2 hours (including tests)
   - Dependency: Task-011a entities must exist (SessionId, Session, etc.)

2. **Missing IOutbox Interface (Application Layer)** - **BLOCKS OUTBOX PATTERN**
   - Impact: Cannot implement outbox processor, sync service
   - Required for: outbox tests, outbox implementations
   - Estimated effort: 1-2 hours (including tests)

3. **Missing SQLiteRunStateStore (Critical Persistence)** - **BLOCKS SQLite PERSISTENCE**
   - Impact: Cannot persist sessions, tasks, steps, artifacts to SQLite
   - Required for: All persistence unit/integration tests
   - Depends on: IRunStateStore, Session entities from task-011a
   - Estimated effort: 6-8 hours (including tests)

4. **Missing SyncService (Critical Sync)** - **BLOCKS SYNC FUNCTIONALITY**
   - Impact: Cannot sync data to PostgreSQL, cannot handle offline
   - Required for: All sync tests, all offline operation tests
   - Depends on: IRunStateStore, IOutbox, SQLiteRunStateStore
   - Estimated effort: 8-10 hours (including tests)

### **Major Gaps by Category:**

| Category | Complete | Incomplete | Missing | Total | Status |
|----------|----------|-----------|---------|-------|--------|
| Interfaces | 0 | 0 | 2 | 2 | ❌ CRITICAL |
| SQLite Persistence | 1 | 0 | 7 | 8 | ❌ CRITICAL |
| PostgreSQL Persistence | 0 | 0 | 3 | 3 | ❌ CRITICAL |
| Sync Infrastructure | 0 | 0 | 4 | 4 | ❌ CRITICAL |
| Migrations | 0 | 0 | 4 | 4 | ❌ CRITICAL |
| Unit Tests | 0 | 0 | 5 | 5 | ❌ CRITICAL |
| Integration Tests | 0 | 0 | 4 | 4 | ❌ CRITICAL |
| E2E Tests | 0 | 0 | 1 | 1 | ❌ CRITICAL |
| **TOTAL** | **1** | **1** | **26+** | **28+** | **~5%** |

---

## RECOMMENDED IMPLEMENTATION ORDER (8 Phases)

### Phase 1: Application Layer Interfaces (1-2 hours)
- Create IRunStateStore interface with all required methods
- Create IOutbox interface with all required methods
- Write unit tests for interfaces (verify contract)
- Result: Foundation for all persistence implementations

### Phase 2: SQLite Persistence Layer (6-8 hours)
- Create SQLiteRunStateStore implementing IRunStateStore
- Create SQLiteOutbox implementing IOutbox
- Implement all CRUD operations
- Write SessionRepositoryTests (4 methods)
- Result: Full SQLite persistence working

### Phase 3: PostgreSQL Persistence Layer (5-6 hours)
- Create PostgresRunStateStore implementing IRunStateStore
- Create PostgresSyncTarget for receiving sync data
- Implement PostgreSQL connection management
- Write PostgreSQLIntegrationTests (3 methods)
- Result: PostgreSQL connections ready for sync

### Phase 4: Migration Infrastructure (3-4 hours)
- Create IMigration interface for version management
- Create MigrationRunner orchestrating migrations
- Create V1_0_0_InitialSchema with all table schemas
- Create V1_1_0_AddArtifacts migration
- Write MigrationRunnerTests (3 methods) and MigrationIntegrationTests (2 methods)
- Result: Automatic schema management working

### Phase 5: Sync Service Core (6-8 hours)
- Create ISyncService interface
- Create SyncService background worker
- Implement outbox processing with batching
- Implement exponential backoff retry logic
- Write SyncServiceTests (3 methods)
- Result: Background sync engine ready

### Phase 6: Conflict Resolution (2-3 hours)
- Create ConflictResolver
- Implement "latest timestamp wins" logic
- Implement conflict logging
- Result: Conflicts handled automatically

### Phase 7: Integration & Performance (4-5 hours)
- Write OutboxTests (3 methods)
- Write OfflineOperationTests (3 methods)
- Write SQLiteIntegrationTests (3 methods)
- Implement performance benchmarks (5 scenarios)
- Verify all performance targets met
- Result: All 24+ tests passing, performance verified

### Phase 8: CLI Commands & Final Polish (2-3 hours)
- Implement `acode db status` command
- Implement `acode db sync` command
- Implement `acode db migrate` command
- Write documentation
- Final audit
- Result: Complete task ready for merge

**Total Estimated Effort: 28-40 hours for complete 100% implementation**

---

## DEPENDENCY ANALYSIS

### **Hard Blockers (Must wait for):**
- Task-011a (Run Entities): Must provide Session, SessionTask, Step, ToolCall, Artifact entities
- **Status**: Analyzed separately - 0% complete, major work required

### **Soft Dependencies (Can work in parallel):**
- None - persistence layer can proceed independently once interfaces defined

---

**Status:** ✅ GAP ANALYSIS COMPLETE - READY FOR IMPLEMENTATION

**Next Steps:**
1. Use task-011b-completion-checklist.md for detailed implementation roadmap
2. Execute Phase 1: Application Interfaces (1-2 hours)
3. Execute Phases 2-8 sequentially with TDD
4. Verify all 59 ACs implemented and 24+ tests passing
5. Create PR and merge

---
