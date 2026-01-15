# Task-011b Semantic Gap Analysis: Persistence Model (SQLite + PostgreSQL)

**Status:** ❌ 0% COMPLETE - SEMANTIC COMPLETENESS: 0/59 ACs (0%)

**Date:** 2026-01-15
**Analyzed By:** Claude Code
**Methodology:** Semantic completeness verification per CLAUDE.md Section 3.2

---

## EXECUTIVE SUMMARY

Task-011b (Persistence Model) is **0% semantically complete**. All 59 Acceptance Criteria are missing or incomplete:

- **Total ACs:** 59
- **ACs Present (fully implemented):** 0
- **ACs Missing:** 59
- **Semantic Completeness:** (0 / 59) × 100 = **0%**
- **Implementation Gaps:** 14 critical production files needed
- **Test Gaps:** 13 test files needed
- **Estimated Effort:** 59 hours (from spec analysis)

**Blocking Dependencies:**
- ✓ Task-011a (Run Entities) - Assumed complete or near-complete
- ❌ Task-050 (Workspace DB Foundation) - NOT STARTED (blocks design patterns)

**Critical Finding:** While the codebase contains migration infrastructure and some SQLite files, NONE of these are configured for session run state persistence per 011b spec. Existing files implement conversation persistence (task-049), not session persistence (task-011b).

**Recommendation:** Full new implementation needed. Existing infrastructure MAY be reusable but requires integration into 011b patterns.

---

## SECTION 1: ACCEPTANCE CRITERIA VERIFICATION

### SQLite Storage (AC-001 through AC-008)

**AC-001: Database in .agent/workspace.db**
- **Status:** ❌ MISSING (session-specific implementation)
- **Evidence:** Config references `.acode/workspace.db`, not `.agent/workspace.db`
- **Spec Reference:** Lines 1780, 2029
- **Required Component:** SqliteConnectionFactory configured for session state
- **Verification:** Codebase has SqliteConnectionFactory but not configured per 011b spec (wrong path, no session-specific setup)
- **AC Complete:** NO

**AC-002: WAL mode enabled**
- **Status:** ❌ MISSING
- **Evidence:** No grep result for WAL mode configuration in SQLite setup
- **Spec Reference:** Line 1781, 2062
- **Required Component:** SQLiteConnectionFactory enables WAL on session database
- **Verification:** Documentation mentions WAL but implementation not found in session persistence code
- **AC Complete:** NO

**AC-003: STRICT tables used**
- **Status:** ❌ MISSING
- **Evidence:** No STRICT table definitions found in session schema
- **Spec Reference:** Line 1782, 2062
- **Required Component:** Migration creates tables with STRICT keyword
- **Verification:** Spec requires `CREATE TABLE ... STRICT;` but no such tables exist for session storage
- **AC Complete:** NO

**AC-004: Auto-created on first access**
- **Status:** ❌ MISSING
- **Evidence:** No auto-creation logic in SqliteConnectionFactory for session database
- **Spec Reference:** Line 1783, 2066-2068
- **Required Component:** Connection factory creates database automatically if not exists
- **Verification:** IRunStateStore.GetAsync() should trigger auto-creation but interface doesn't exist
- **AC Complete:** NO

**AC-005: Writes transactional**
- **Status:** ❌ MISSING (session-specific)
- **Evidence:** No IRunStateStore.SaveAsync() implementation with transaction guarantee
- **Spec Reference:** Lines 1784-1785, 2074-2080
- **Required Component:** SaveAsync() wraps writes in BEGIN TRANSACTION...COMMIT
- **Verification:** Interface not implemented, cannot verify transactional behavior
- **AC Complete:** NO

**AC-006: Transaction timeout 30s**
- **Status:** ❌ MISSING
- **Evidence:** No timeout configuration for session transactions
- **Spec Reference:** Line 1786, 2085-2087
- **Required Component:** SqliteConnectionFactory sets timeout = 30s
- **Verification:** Config mentions timeout but no value set, no session-specific configuration
- **AC Complete:** NO

**AC-007: Concurrent reads work**
- **Status:** ❌ MISSING
- **Evidence:** No concurrent read handling implemented for session store
- **Spec Reference:** Line 1787, 2092
- **Required Component:** SQLiteRunStateStore implements PRAGMA query_only for concurrent reads
- **Verification:** No implementation to verify
- **AC Complete:** NO

**AC-008: Write locks exclusive**
- **Status:** ❌ MISSING
- **Evidence:** No exclusive lock handling in session write operations
- **Spec Reference:** Line 1788, 2093-2098
- **Required Component:** SaveAsync() uses exclusive locks via PRAGMA locking_mode
- **Verification:** Not implemented
- **AC Complete:** NO

**Subtotal SQLite Storage: 0/8 ACs complete (0%)**

---

### Schema Management (AC-009 through AC-014)

**AC-009: Schema version tracked**
- **Status:** ❌ MISSING (for session domain)
- **Evidence:** Migration system exists but not integrated with session schema
- **Spec Reference:** Line 1789, 2103-2108
- **Required Component:** schema_version table in session database
- **Verification:** Version tracking exists in migration infrastructure but not for session storage
- **AC Complete:** NO

**AC-010: Migrations versioned**
- **Status:** ❌ MISSING (session-specific migrations)
- **Evidence:** No migrations for session tables exist
- **Spec Reference:** Line 1790, 2111-2115
- **Required Component:** Migrations: V1_0_0_InitialSessionSchema, V1_1_0_AddArtifactTables, etc.
- **Verification:** Spec calls for versioned migrations (V1.0.0, V1.1.0) for session domain
- **AC Complete:** NO

**AC-011: Auto-migrate on startup**
- **Status:** ❌ MISSING (for session domain)
- **Evidence:** MigrationBootstrapper exists but not wired for session database
- **Spec Reference:** Line 1791, 2119-2122
- **Required Component:** IMigrationBootstrapper runs before first IRunStateStore access
- **Verification:** Application startup doesn't include session migration bootstrap
- **AC Complete:** NO

**AC-012: Migrations idempotent**
- **Status:** ⚠️ PARTIAL (for conversation domain, not sessions)
- **Evidence:** MigrationValidator exists but not applied to session migrations
- **Spec Reference:** Line 1792, 2126-2135
- **Required Component:** Each migration can be run multiple times without error
- **Verification:** Session migrations don't exist yet to test idempotency
- **AC Complete:** NO

**AC-013: Migration failure halts startup**
- **Status:** ❌ MISSING (for session domain)
- **Evidence:** No session migration failure handling
- **Spec Reference:** Line 1793, 2139-2143
- **Required Component:** IMigrationBootstrapper throws if any session migration fails
- **Verification:** Not implemented
- **AC Complete:** NO

**AC-014: Version queryable**
- **Status:** ❌ MISSING
- **Evidence:** No `acode db schema version` command
- **Spec Reference:** Line 1794, 2147-2153
- **Required Component:** CLI command queries session schema version
- **Verification:** Command not implemented
- **AC Complete:** NO

**Subtotal Schema Management: 0/6 ACs complete (0%)**

---

### Tables (AC-015 through AC-022)

**AC-015: sessions table exists**
- **Status:** ❌ MISSING
- **Evidence:** No migration creates sessions table with session_id, state, created_at, updated_at columns
- **Spec Reference:** Line 1795, 2157-2163
- **Required Component:** Migration V1_0_0 creates sessions table as STRICT
- **Verification:** Table not found in codebase
- **AC Complete:** NO

**AC-016: session_events table exists**
- **Status:** ❌ MISSING
- **Evidence:** No table for session domain events
- **Spec Reference:** Line 1796, 2167-2173
- **Required Component:** table with event_id, session_id, event_type, payload columns
- **Verification:** Not found
- **AC Complete:** NO

**AC-017: session_tasks table exists**
- **Status:** ❌ MISSING
- **Evidence:** No table for session tasks
- **Spec Reference:** Line 1797, 2177-2182
- **Required Component:** table with task_id, session_id, status, created_at columns
- **Verification:** Not found
- **AC Complete:** NO

**AC-018: steps table exists**
- **Status:** ❌ MISSING
- **Evidence:** No steps table for session steps
- **Spec Reference:** Line 1798, 2186-2191
- **Required Component:** table with step_id, task_id, status columns
- **Verification:** Not found
- **AC Complete:** NO

**AC-019: tool_calls table exists**
- **Status:** ❌ MISSING
- **Evidence:** No table for tool calls within steps
- **Spec Reference:** Line 1799, 2195-2200
- **Required Component:** table with tool_call_id, step_id, name, arguments, result columns
- **Verification:** Not found
- **AC Complete:** NO

**AC-020: artifacts table exists**
- **Status:** ❌ MISSING
- **Evidence:** No table for artifacts
- **Spec Reference:** Line 1800, 2204-2209
- **Required Component:** table with artifact_id, session_id, content, type columns
- **Verification:** Not found
- **AC Complete:** NO

**AC-021: outbox table exists**
- **Status:** ❌ MISSING (for session outbox)
- **Evidence:** IOutboxRepository exists but for conversation sync, not session outbox
- **Spec Reference:** Line 1801, 2213-2219
- **Required Component:** SQLite outbox table with outbox_id, session_id, payload, processed_at columns
- **Verification:** OutboxEntry domain model exists but for conversation, not session persistence
- **AC Complete:** NO

**AC-022: All have timestamps**
- **Status:** ❌ MISSING (for session tables)
- **Evidence:** Tables don't exist yet to verify timestamp columns
- **Spec Reference:** Line 1802, 2223-2225
- **Required Component:** All tables include created_at (TIMESTAMP NOT NULL) and updated_at (TIMESTAMP)
- **Verification:** Cannot verify until tables created
- **AC Complete:** NO

**Subtotal Tables: 0/8 ACs complete (0%)**

---

### PostgreSQL Configuration (AC-023 through AC-028)

**AC-023: Configurable via config**
- **Status:** ❌ MISSING
- **Evidence:** No PostgreSQL configuration in persistence section of config
- **Spec Reference:** Line 1803, 2231-2235
- **Required Component:** persistence.postgres section in config schema
- **Verification:** Config schema doesn't include postgres configuration for session storage
- **AC Complete:** NO

**AC-024: Connection string from env**
- **Status:** ❌ MISSING
- **Evidence:** No ACODE_POSTGRES_URL environment variable handling
- **Spec Reference:** Line 1804, 2239-2243
- **Required Component:** PostgresConnectionFactory reads from env var
- **Verification:** PostgresConnectionFactory exists but not configured for session storage
- **AC Complete:** NO

**AC-025: Missing doesn't fail startup**
- **Status:** ❌ MISSING
- **Evidence:** No graceful fallback to SQLite-only when PostgreSQL unavailable
- **Spec Reference:** Line 1805, 2247-2250
- **Required Component:** Application starts with SQLite even if PostgreSQL unavailable
- **Verification:** Not implemented
- **AC Complete:** NO

**AC-026: Schema mirrors SQLite**
- **Status:** ❌ MISSING
- **Evidence:** No PostgreSQL schema defined for session tables
- **Spec Reference:** Line 1806, 2254-2258
- **Required Component:** PostgreSQL schema matches SQLite tables exactly
- **Verification:** Not implemented
- **AC Complete:** NO

**AC-027: Indexes optimized**
- **Status:** ❌ MISSING
- **Evidence:** No PostgreSQL indexes defined
- **Spec Reference:** Line 1807, 2262-2275
- **Required Component:** CREATE INDEX on session_id, entity_id, created_at for query performance
- **Verification:** Not implemented
- **AC Complete:** NO

**AC-028: Pool size configurable**
- **Status:** ❌ MISSING
- **Evidence:** No pool size configuration for session PostgreSQL connections
- **Spec Reference:** Line 1808, 2279-2283
- **Required Component:** Connection pool configured with pool_size: 10 (configurable)
- **Verification:** Not found in code
- **AC Complete:** NO

**Subtotal PostgreSQL: 0/6 ACs complete (0%)**

---

### Outbox Pattern (AC-029 through AC-035)

**AC-029: Records have idempotency_key**
- **Status:** ❌ MISSING (for session outbox)
- **Evidence:** Session outbox not implemented
- **Spec Reference:** Line 1809, 2287-2291
- **Required Component:** Outbox records include unique idempotency_key field
- **Verification:** Not found
- **AC Complete:** NO

**AC-030: Records have entity_type**
- **Status:** ❌ MISSING
- **Evidence:** Session outbox table not created
- **Spec Reference:** Line 1810, 2295-2299
- **Required Component:** Outbox records track entity_type (Session, Task, Step, etc.)
- **Verification:** Not implemented
- **AC Complete:** NO

**AC-031: Records have entity_id**
- **Status:** ❌ MISSING
- **Evidence:** No outbox field for entity_id
- **Spec Reference:** Line 1811, 2303-2307
- **Required Component:** Outbox records identify specific entity being synced
- **Verification:** Not found
- **AC Complete:** NO

**AC-032: Records have payload**
- **Status:** ❌ MISSING
- **Evidence:** No outbox payload field
- **Spec Reference:** Line 1812, 2311-2315
- **Required Component:** JSON payload with full entity state
- **Verification:** Not implemented
- **AC Complete:** NO

**AC-033: Records have created_at**
- **Status:** ❌ MISSING
- **Evidence:** No outbox created_at timestamp
- **Spec Reference:** Line 1813, 2319-2323
- **Required Component:** Timestamp when record created
- **Verification:** Not found
- **AC Complete:** NO

**AC-034: Records track processed_at**
- **Status:** ❌ MISSING
- **Evidence:** No processed_at tracking in session outbox
- **Spec Reference:** Line 1814, 2327-2331
- **Required Component:** Timestamp when synced to PostgreSQL (nullable)
- **Verification:** Not implemented
- **AC Complete:** NO

**AC-035: Records track attempts**
- **Status:** ❌ MISSING
- **Evidence:** No attempt counter in outbox records
- **Spec Reference:** Line 1815, 2335-2339
- **Required Component:** Retry attempt count for failed syncs
- **Verification:** Not found
- **AC Complete:** NO

**Subtotal Outbox Pattern: 0/7 ACs complete (0%)**

---

### Sync Service (AC-036 through AC-042)

**AC-036: Runs in background**
- **Status:** ❌ MISSING
- **Evidence:** No background sync worker for session outbox
- **Spec Reference:** Line 1816, 2343-2347
- **Required Component:** ISyncService with BackgroundSyncWorker
- **Verification:** Not found in session persistence code
- **AC Complete:** NO

**AC-037: Non-blocking**
- **Status:** ❌ MISSING
- **Evidence:** No non-blocking sync design
- **Spec Reference:** Line 1817, 2351-2355
- **Required Component:** SaveAsync() returns immediately after SQLite write, before sync
- **Verification:** Not implemented
- **AC Complete:** NO

**AC-038: Oldest first**
- **Status:** ❌ MISSING
- **Evidence:** No outbox processing order logic
- **Spec Reference:** Line 1818, 2359-2363
- **Required Component:** SyncService processes outbox ORDER BY created_at ASC
- **Verification:** Not found
- **AC Complete:** NO

**AC-039: Retries failed**
- **Status:** ❌ MISSING
- **Evidence:** No retry logic for failed syncs
- **Spec Reference:** Line 1819, 2367-2371
- **Required Component:** Failed records marked for retry
- **Verification:** Not implemented
- **AC Complete:** NO

**AC-040: Exponential backoff**
- **Status:** ❌ MISSING
- **Evidence:** No exponential backoff implementation
- **Spec Reference:** Line 1820, 2375-2383
- **Required Component:** Retry delays: 5s, 10s, 20s, 40s...3600s (max)
- **Verification:** Not found
- **AC Complete:** NO

**AC-041: Max 10 attempts**
- **Status:** ❌ MISSING
- **Evidence:** No max attempts limit
- **Spec Reference:** Line 1821, 2387-2391
- **Required Component:** Record marked failed after 10 attempts
- **Verification:** Not implemented
- **AC Complete:** NO

**AC-042: Resumable after restart**
- **Status:** ❌ MISSING
- **Evidence:** No persistent outbox processing state
- **Spec Reference:** Line 1822, 2395-2401
- **Required Component:** In-flight syncs survive application restart
- **Verification:** Not found
- **AC Complete:** NO

**Subtotal Sync Service: 0/7 ACs complete (0%)**

---

### Idempotency (AC-043 through AC-047)

**AC-043: Unique keys generated**
- **Status:** ❌ MISSING
- **Evidence:** No idempotency key generation for session outbox
- **Spec Reference:** Line 1823, 2405-2409
- **Required Component:** IdempotencyKeyGenerator class
- **Verification:** Not found
- **AC Complete:** NO

**AC-044: Key format correct**
- **Status:** ❌ MISSING
- **Evidence:** No implementation of format: entity_type:entity_id:timestamp
- **Spec Reference:** Line 1824, 2413-2421
- **Required Component:** Keys like "Session:sess_abc123:2026-01-15T10:30:00Z"
- **Verification:** Not found
- **AC Complete:** NO

**AC-045: Duplicates rejected**
- **Status:** ❌ MISSING
- **Evidence:** No duplicate detection in PostgreSQL
- **Spec Reference:** Line 1825, 2425-2429
- **Required Component:** UNIQUE constraint on idempotency_key
- **Verification:** Not implemented
- **AC Complete:** NO

**AC-046: Rejection marks processed**
- **Status:** ❌ MISSING
- **Evidence:** No handling for duplicate key rejection
- **Spec Reference:** Line 1826, 2433-2437
- **Required Component:** Duplicate inserts mark outbox record as processed
- **Verification:** Not found
- **AC Complete:** NO

**AC-047: Logged**
- **Status:** ❌ MISSING
- **Evidence:** No logging for idempotency key usage
- **Spec Reference:** Line 1827, 2441-2445
- **Required Component:** Audit log includes idempotency_key for all operations
- **Verification:** Not implemented
- **AC Complete:** NO

**Subtotal Idempotency: 0/5 ACs complete (0%)**

---

### Conflict Resolution (AC-048 through AC-052)

**AC-048: Detected**
- **Status:** ❌ MISSING
- **Evidence:** No conflict detection in sync
- **Spec Reference:** Line 1828, 2449-2453
- **Required Component:** ConflictResolver detects version mismatches
- **Verification:** Not found
- **AC Complete:** NO

**AC-049: Latest wins**
- **Status:** ❌ MISSING
- **Evidence:** No last-write-wins conflict resolution
- **Spec Reference:** Line 1829, 2457-2461
- **Required Component:** Higher updated_at timestamp wins
- **Verification:** Not implemented
- **AC Complete:** NO

**AC-050: Logged**
- **Status:** ❌ MISSING
- **Evidence:** No conflict logging
- **Spec Reference:** Line 1830, 2465-2471
- **Required Component:** Conflict log with before/after state
- **Verification:** Not found
- **AC Complete:** NO

**AC-051: Counted**
- **Status:** ❌ MISSING
- **Evidence:** No conflict count tracking
- **Spec Reference:** Line 1831, 2475-2479
- **Required Component:** Metrics on conflict frequency
- **Verification:** Not found
- **AC Complete:** NO

**AC-052: Details preserved**
- **Status:** ❌ MISSING
- **Evidence:** No conflict detail preservation
- **Spec Reference:** Line 1832, 2483-2487
- **Required Component:** Both versions stored for audit
- **Verification:** Not implemented
- **AC Complete:** NO

**Subtotal Conflict Resolution: 0/5 ACs complete (0%)**

---

### Performance Requirements (AC-053 through AC-056)

**AC-053: SQLite write < 50ms**
- **Status:** ❌ MISSING (unverified - no implementation)
- **Evidence:** No SQLite session write performance test
- **Spec Reference:** Line 1833, 2491-2495
- **Required Component:** SaveAsync() completes in < 50ms
- **Verification:** Cannot test without implementation
- **AC Complete:** NO

**AC-054: SQLite read < 10ms**
- **Status:** ❌ MISSING
- **Evidence:** No SQLite session read performance test
- **Spec Reference:** Line 1834, 2499-2503
- **Required Component:** GetAsync() completes in < 10ms
- **Verification:** Cannot test without implementation
- **AC Complete:** NO

**AC-055: Sync 100 records/sec**
- **Status:** ❌ MISSING
- **Evidence:** No sync throughput benchmark
- **Spec Reference:** Line 1835, 2507-2511
- **Required Component:** SyncService processes 100+ records/second
- **Verification:** Not implemented
- **AC Complete:** NO

**AC-056: Query with pagination < 100ms**
- **Status:** ❌ MISSING
- **Evidence:** No query pagination performance requirement
- **Spec Reference:** Line 1836, 2515-2519
- **Required Component:** ListAsync() with pagination < 100ms
- **Verification:** Not implemented
- **AC Complete:** NO

**Subtotal Performance: 0/4 ACs complete (0%)**

---

### Security (AC-057 through AC-059)

**AC-057: File permissions 600**
- **Status:** ❌ MISSING
- **Evidence:** No file permission enforcement on workspace.db
- **Spec Reference:** Line 1837, 2523-2527
- **Required Component:** Database file created with mode 0600 (owner read/write only)
- **Verification:** Not found
- **AC Complete:** NO

**AC-058: Connection string not logged**
- **Status:** ❌ MISSING
- **Evidence:** No redaction of connection strings in logs
- **Spec Reference:** Line 1838, 2531-2535
- **Required Component:** PostgreSQL connection string redacted in logs
- **Verification:** Not implemented
- **AC Complete:** NO

**AC-059: SQL injection prevented**
- **Status:** ❌ MISSING (unverified - no implementation)
- **Evidence:** No session persistence code uses parameterized queries yet
- **Spec Reference:** Line 1839, 2539-2545
- **Required Component:** All queries use parameterized statements (no string concatenation)
- **Verification:** Cannot verify without implementation
- **AC Complete:** NO

**Subtotal Security: 0/3 ACs complete (0%)**

---

## SECTION 2: ACCEPTANCE CRITERIA SUMMARY

**Final Count:**
- SQLite Storage: 0/8
- Schema Management: 0/6
- Tables: 0/8
- PostgreSQL: 0/6
- Outbox Pattern: 0/7
- Sync Service: 0/7
- Idempotency: 0/5
- Conflict Resolution: 0/5
- Performance: 0/4
- Security: 0/3

**TOTAL: 0/59 ACs COMPLETE (0% Semantic Completeness)**

---

## SECTION 3: PRODUCTION FILES NEEDED

**Application Layer (2 files):**
- [ ] `src/Acode.Application/Sessions/IRunStateStore.cs` - Core persistence interface
- [ ] `src/Acode.Application/Sessions/IOutbox.cs` - Outbox abstraction

**Infrastructure - SQLite (3 files):**
- [ ] `src/Acode.Infrastructure/Persistence/SQLite/SqliteConnectionFactory.cs` - Connection management with WAL/STRICT
- [ ] `src/Acode.Infrastructure/Persistence/SQLite/SqliteRunStateStore.cs` - IRunStateStore implementation
- [ ] `src/Acode.Infrastructure/Persistence/SQLite/SqliteOutbox.cs` - Outbox implementation

**Infrastructure - Migrations (3 files):**
- [ ] `src/Acode.Infrastructure/Persistence/Migrations/SessionMigrations/V1_0_0_InitialSchema.cs` - Session tables
- [ ] `src/Acode.Infrastructure/Persistence/Migrations/SessionMigrations/V1_1_0_AddArtifacts.cs` - Artifact tables
- [ ] `src/Acode.Infrastructure/Persistence/SessionMigrationRunner.cs` - Migration orchestration for sessions

**Infrastructure - PostgreSQL (2 files):**
- [ ] `src/Acode.Infrastructure/Persistence/PostgreSQL/PostgresRunStateStore.cs` - PostgreSQL implementation
- [ ] `src/Acode.Infrastructure/Persistence/PostgreSQL/PostgresSyncTarget.cs` - Sync target for outbox

**Infrastructure - Sync (4 files):**
- [ ] `src/Acode.Infrastructure/Persistence/Sync/SyncService.cs` - Background sync orchestration
- [ ] `src/Acode.Infrastructure/Persistence/Sync/OutboxProcessor.cs` - Outbox batch processing
- [ ] `src/Acode.Infrastructure/Persistence/Sync/ConflictResolver.cs` - Last-write-wins resolution
- [ ] `src/Acode.Infrastructure/Persistence/Sync/IdempotencyKeyGenerator.cs` - Key generation

**Total: 14 production files**

---

## SECTION 4: TEST FILES NEEDED

**Unit Tests (6 files):**
- [ ] `tests/Acode.Application.Tests/Sessions/IRunStateStoreTests.cs` - 12 tests
- [ ] `tests/Acode.Infrastructure.Tests/Persistence/SQLite/SqliteConnectionFactoryTests.cs` - 5 tests
- [ ] `tests/Acode.Infrastructure.Tests/Persistence/SQLite/SqliteRunStateStoreTests.cs` - 8 tests
- [ ] `tests/Acode.Infrastructure.Tests/Persistence/Sync/SyncServiceTests.cs` - 9 tests
- [ ] `tests/Acode.Infrastructure.Tests/Persistence/Sync/OutboxProcessorTests.cs` - 8 tests
- [ ] `tests/Acode.Infrastructure.Tests/Persistence/Sync/ConflictResolverTests.cs` - 6 tests

**Integration Tests (4 files):**
- [ ] `tests/Acode.Infrastructure.Tests/Persistence/SQLiteIntegrationTests.cs` - 5 tests
- [ ] `tests/Acode.Infrastructure.Tests/Persistence/PostgreSQLIntegrationTests.cs` - 4 tests
- [ ] `tests/Acode.Infrastructure.Tests/Persistence/MigrationIntegrationTests.cs` - 3 tests
- [ ] `tests/Acode.Infrastructure.Tests/Persistence/SyncIntegrationTests.cs` - 6 tests

**E2E Tests (2 files):**
- [ ] `tests/Acode.Infrastructure.Tests/Persistence/OfflineOperationTests.cs` - 3 tests
- [ ] `tests/Acode.Infrastructure.Tests/Persistence/PerformanceBenchmarkTests.cs` - 5 benchmark tests

**Total Test Files: 12**
**Total Test Methods: ~74 tests**

---

## SECTION 5: BLOCKING DEPENDENCIES

### Task-011a (Run Entities) - Status: ASSUMED COMPLETE

**What 011a Provides:**
- `Session` aggregate root with state machine
- `SessionId`, `TaskId`, `StepId`, `ToolCallId`, `ArtifactId` value objects
- `SessionState` enum (CREATED, PLANNING, AWAITING_APPROVAL, EXECUTING, PAUSED, COMPLETED, FAILED, CANCELLED)
- `SessionEvent` and event subtypes for replay

**Impact on 011b:**
- IRunStateStore.SaveAsync(Session) depends on Session entity
- Migrations store/retrieve Session serialized to JsonDocument
- All domain objects serialized to Outbox.Payload

**Verification Required:** Task-011a must complete before 011b testing can fully begin.

### Task-050 (Workspace DB Foundation) - Status: NOT STARTED

**What 050 Provides (Inferred):**
- `IDbContext` - Base database context interface
- `IRepository<T>` - Generic repository pattern
- `IUnitOfWork` - Transaction abstraction
- Connection pooling configuration
- Database migration framework abstractions

**Impact on 011b:**
- Session persistence COULD use repository pattern from 050
- Transaction management defined in 050
- Migration framework patterns defined in 050

**Current Approach:** Implement 011b independently with direct SQLite/PostgreSQL access; integrate with 050 patterns if/when 050 delivered.

**Recommendation:** Proceed with implementation without blocking on 050, but design for easy integration.

---

## SECTION 6: IMPLEMENTATION EFFORT BREAKDOWN

| Category | Count | Hours | Notes |
|----------|-------|-------|-------|
| Production Files | 14 | 35 | Core persistence logic, sync coordination |
| Unit Tests | 6 | 12 | Isolation tests with mocked dependencies |
| Integration Tests | 4 | 8 | Real SQLite/PostgreSQL, no network |
| E2E Tests | 2 | 4 | End-to-end scenarios with full stack |
| Documentation | - | 5 | Configuration, troubleshooting, examples |
| Code Review/Refactor | - | 5 | Final cleanup, optimize, verify |
| **TOTAL** | - | **59 hours** | 7-8 days full-time equivalent |

**Effort Breakdown by Phase:**
1. **Interfaces & SQLite Core** (12 hrs) - IRunStateStore, SqliteConnectionFactory, CRUD operations
2. **Migration System** (8 hrs) - Schema migrations, auto-run on startup, versioning
3. **Outbox & Sync** (16 hrs) - Outbox pattern, background sync, idempotency
4. **PostgreSQL Sync** (10 hrs) - PostgreSQL implementation, conflict resolution
5. **Testing & Performance** (10 hrs) - Unit/integration/E2E tests, benchmarks
6. **Documentation & Polish** (3 hrs) - Docs, error messages, logging

---

## SECTION 7: SEMANTIC COMPLETENESS CALCULATION

```
Task-011b Semantic Completeness = (ACs fully implemented / Total ACs) × 100

ACs Fully Implemented: 0
  - SQLite Storage: 0/8
  - Schema Management: 0/6
  - Tables: 0/8
  - PostgreSQL: 0/6
  - Outbox: 0/7
  - Sync: 0/7
  - Idempotency: 0/5
  - Conflict: 0/5
  - Performance: 0/4
  - Security: 0/3

Total ACs: 59

Semantic Completeness: (0 / 59) × 100 = 0%

Conservative Estimate: 0% (NO acceptable assumptions)
```

---

## SECTION 8: QUALITY STANDARDS

All 011b implementations must meet:
- ✅ 100% AC compliance (all 59 ACs verified)
- ✅ Unit test coverage > 90% for all components
- ✅ Integration test coverage for critical paths (SQLite CRUD, outbox processing, sync)
- ✅ Zero build errors/warnings
- ✅ No NotImplementedException in production code
- ✅ Clean Architecture layer boundaries (Domain → Application → Infrastructure)
- ✅ Async/await best practices (no blocking calls)
- ✅ Performance benchmarks passing (write <50ms, read <10ms, sync >100/sec)
- ✅ Comprehensive error handling with error codes (ACODE-DB-001 through ACODE-DB-007)
- ✅ Security requirements met (file permissions, connection string redaction, SQL injection prevention)

---

## SECTION 9: GIT WORKFLOW

**Current Branch:** `feature/task-049-prompt-pack-loader` (per user instruction)

**Commits for Task-011b Implementation:**

```bash
# Phase 1: Interfaces & SQLite Core
git commit -m "feat(task-011b): create IRunStateStore persistence interface"
git commit -m "feat(task-011b): implement SqliteConnectionFactory with WAL and STRICT"
git commit -m "feat(task-011b): implement SqliteRunStateStore CRUD operations"
git commit -m "test(task-011b): add unit tests for SQLite persistence layer"

# Phase 2: Schema & Migrations
git commit -m "feat(task-011b): create session schema migration (V1_0_0)"
git commit -m "feat(task-011b): create artifact tables migration (V1_1_0)"
git commit -m "feat(task-011b): implement migration runner with auto-bootstrap"
git commit -m "test(task-011b): add migration integration tests"

# Phase 3: Outbox & Sync
git commit -m "feat(task-011b): implement outbox pattern for reliable sync"
git commit -m "feat(task-011b): implement idempotency key generation"
git commit -m "feat(task-011b): implement SyncService with background processing"
git commit -m "test(task-011b): add outbox and sync tests"

# Phase 4: PostgreSQL & Conflict Resolution
git commit -m "feat(task-011b): implement PostgreSQL sync target"
git commit -m "feat(task-011b): implement conflict resolver with last-write-wins"
git commit -m "test(task-011b): add PostgreSQL and conflict resolution tests"

# Phase 5: CLI & Documentation
git commit -m "feat(task-011b): add db status, sync, migrate CLI commands"
git commit -m "docs(task-011b): add persistence configuration guide"

# Final: Complete task-011b
git commit -m "feat(task-011b): complete persistence model - all 59 ACs implemented

- SQLite core with WAL and STRICT tables
- Schema migrations with auto-bootstrap
- Outbox pattern with idempotency
- PostgreSQL sync with conflict resolution
- 74 tests (unit + integration + E2E)
- CLI commands for database management
- Performance benchmarks passing
- Security hardening (file perms, SQL injection prevention)

Total: 14 production files, 12 test files, 59/59 ACs complete

🤖 Generated with Claude Code
"
```

---

## SECTION 10: RECOMMENDED NEXT STEPS

### Phase 1: Accept this gap analysis

1. Review this document
2. Verify assessment accuracy
3. Confirm effort estimate (59 hours)
4. Identify any blockers or constraints

### Phase 2: Begin implementation

**Follow this sequence (Phase 1, then 2, then 3, etc.):**

1. Create `docs/implementation-plans/task-011b-completion-checklist.md` based on these gaps
2. Implement Phase 1 (Interfaces & SQLite) - 12 hours
3. Implement Phase 2 (Migrations) - 8 hours
4. Implement Phase 3 (Outbox) - 16 hours
5. Implement Phase 4 (PostgreSQL) - 10 hours
6. Implement Phase 5 (Testing & Polish) - 13 hours

### Phase 3: Verification

- Run full test suite: `dotnet test` (all 74+ tests passing)
- Run performance benchmarks: verify AC-053 through AC-056
- Manual testing: all 10 user verification scenarios from spec
- Security audit: verify AC-057 through AC-059

---

## AUDIT CHECKLIST FOR TASK-011b COMPLETION

**Before declaring task-011b complete:**

- [ ] All 59 ACs verified implemented (AC-001 through AC-059)
- [ ] All 14 production files created with real implementations
- [ ] All 12 test files created with 74+ test methods
- [ ] Zero NotImplementedException in production code
- [ ] Zero build errors, zero warnings
- [ ] All tests passing (dotnet test)
- [ ] Performance benchmarks passing (AC-053 through AC-056)
- [ ] Security requirements verified (AC-057 through AC-059)
- [ ] Code coverage > 90% (SonarQube or similar)
- [ ] Clean Architecture boundaries maintained
- [ ] Async/await best practices followed
- [ ] Error codes implemented (ACODE-DB-001 through ACODE-DB-007)
- [ ] Logging implemented per spec format
- [ ] Configuration schema validated
- [ ] User manual verified against implementation
- [ ] PR created and ready for review

---

**Status:** READY FOR COMPLETION CHECKLIST CREATION

**Current Semantic Completeness:** 0/59 ACs (0%)

**Estimated Time to 100%:** 59 hours (7-8 full-time days)

**Blocking Dependencies:** None - can proceed with Task-050 as parallel work

**Recommendation:** Create completion checklist based on this gap analysis, then begin Phase 1 (Interfaces & SQLite Core) implementation following TDD methodology.
