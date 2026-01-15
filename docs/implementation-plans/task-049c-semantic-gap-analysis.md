# Task-049c Semantic Gap Analysis: Multi-Chat Concurrency Model + Run/Worktree Binding

**Status:** ✅ GAP ANALYSIS COMPLETE - ~0% IMPLEMENTATION (Complete Clean Slate)

**Date:** 2026-01-15

**Analyzed By:** Claude Code (Manual Review + Specification Parsing)

**Methodology:** CLAUDE.md Section 3.2 + GAP_ANALYSIS_METHODOLOGY.md

**Spec Reference:** `docs/tasks/refined-tasks/Epic 02/task-049c-multi-chat-concurrency-worktree-binding.md` (3177 lines)

---

## EXECUTIVE SUMMARY

**Semantic Completeness: ~0% (0/108 ACs) - COMPLETELY UNIMPLEMENTED**

**The Situation:** No production code exists yet for multi-chat concurrency or worktree binding. This is a completely new feature requiring domain entities, application services, infrastructure implementations, database schema, CLI commands, and comprehensive testing.

**Result:** Clean slate implementation. All 108 ACs must be implemented from scratch.

---

## SECTION 1: SPECIFICATION REQUIREMENTS

### Acceptance Criteria Summary

- **Total ACs:** 108 (AC-001 through AC-108)
- **AC Breakdown by Domain:**
  - Binding Commands (AC-001-020): 20 ACs - `acode chat bind`, unbind, bindings, auto-bind
  - Context Resolution (AC-021-035): 15 ACs - Worktree detection, context switching
  - Lock Acquisition/Release (AC-036-055): 20 ACs - Atomic file locking with timeouts
  - Stale Lock Handling (AC-056-065): 10 ACs - Stale detection, automatic cleanup
  - Multi-Session Scenarios (AC-066-080): 15 ACs - Concurrent terminal support
  - Cascade Operations (AC-081-090): 10 ACs - Delete/purge cascading
  - Security (AC-091-098): 8 ACs - Path validation, SQL parameterization, permissions
  - Cross-Cutting (AC-099-108): 10 ACs - Help flags, exit codes, logging, testing coverage

### Error Codes Required

**7 error codes** (ACODE-CONC-001 through ACODE-CONC-007):
1. ACODE-CONC-001: Worktree locked by another process
2. ACODE-CONC-002: Wait timeout exceeded
3. ACODE-CONC-003: Binding already exists
4. ACODE-CONC-004: Worktree not found
5. ACODE-CONC-005: Chat already bound to different worktree
6. ACODE-CONC-006: Lock file corrupted
7. ACODE-CONC-007: Permission denied on lock file

### Expected Production Files (15+ total)

**DOMAIN LAYER (2 files):**
1. `src/AgenticCoder.Domain/Concurrency/WorktreeBinding.cs` - One-to-one chat-worktree binding entity
2. `src/AgenticCoder.Domain/Concurrency/WorktreeLock.cs` - Lock metadata entity (process, timestamp, hostname, terminal)

**APPLICATION LAYER (3 interface files):**
3. `src/AgenticCoder.Application/Concurrency/IBindingService.cs` - Binding CRUD operations
4. `src/AgenticCoder.Application/Concurrency/ILockService.cs` - Lock acquisition/release with timeout support
5. `src/AgenticCoder.Application/Concurrency/IContextResolver.cs` - Worktree detection & context switching

**INFRASTRUCTURE LAYER - Binding (2 files):**
6. `src/AgenticCoder.Infrastructure/Concurrency/SqliteBindingRepository.cs` - Persistent binding storage with one-to-one enforcement
7. `src/AgenticCoder.Infrastructure/Concurrency/ValidatedBindingCache.cs` - In-memory binding cache with staleness detection

**INFRASTRUCTURE LAYER - Locking (5 files):**
8. `src/AgenticCoder.Infrastructure/Concurrency/AtomicFileLockService.cs` - File-based locking in `.agent/locks/`
9. `src/AgenticCoder.Infrastructure/Concurrency/SafeLockPathResolver.cs` - Path traversal prevention
10. `src/AgenticCoder.Infrastructure/Concurrency/LockFileValidator.cs` - Permission & ownership validation
11. `src/AgenticCoder.Infrastructure/Concurrency/WorktreeContextResolver.cs` - Implementation of IContextResolver
12. `src/AgenticCoder.Infrastructure/Concurrency/StaleLocLockCleanupService.cs` - Background stale lock cleanup task

**INFRASTRUCTURE LAYER - Data (1 file):**
13. `src/AgenticCoder.Infrastructure/Persistence/Migrations/Migration_YYYYMM_AddWorktreeBindings.cs` - Database schema for `worktree_bindings` table

**CLI LAYER (4 command files + 1 integration file):**
14. `src/AgenticCoder.Cli/Commands/Concurrency/ChatBindCommand.cs` - `acode chat bind <id>`
15. `src/AgenticCoder.Cli/Commands/Concurrency/ChatUnbindCommand.cs` - `acode chat unbind [--force]`
16. `src/AgenticCoder.Cli/Commands/Concurrency/ChatBindingsCommand.cs` - `acode chat bindings [--json] [--cleanup]`
17. `src/AgenticCoder.Cli/Commands/Concurrency/UnlockCommand.cs` - `acode unlock --force`
18. Enhancement to `RunCommand.cs` - Lock acquisition on run start, release on completion

### Testing Requirements Extraction

**Unit Test Files (3 files, 12 tests total):**
- `Tests/Unit/Concurrency/BindingTests.cs` - 4 tests (bind, unbind, one-to-one enforcement, persistence)
- `Tests/Unit/Concurrency/LockTests.cs` - 5 tests (acquire, release, stale detection, concurrent blocking, wait timeout)
- `Tests/Unit/Concurrency/ContextTests.cs` - 3 tests (context switching, run isolation by chat, worktree recording)

**Integration Test Files (2 files, 5 tests total):**
- `Tests/Integration/Concurrency/MultiSessionTests.cs` - 3 tests (different worktrees parallel, wait queue, timeout)
- `Tests/Integration/Concurrency/BindingPersistenceTests.cs` - 2 tests (application restart survival, cascade delete on purge)

**E2E Test Files (1 file, 3 tests total):**
- `Tests/E2E/Concurrency/WorktreeWorkflowE2ETests.cs` - 3 tests (auto-bind on chat creation, context switch on directory change, lock conflict graceful handling)

**Performance Benchmarks:**
- Context switch latency: Target 25ms, Max 50ms
- Lock acquire latency: Target 5ms, Max 10ms
- Lock release latency: Target 5ms, Max 10ms
- Binding query latency: Target 2ms, Max 5ms

**Total Test Methods Expected: 20+ tests** (all with complete Arrange-Act-Assert implementations in spec)

---

## SECTION 2: CURRENT IMPLEMENTATION STATE (VERIFIED)

### Status: ❌ ZERO IMPLEMENTATION - CLEAN SLATE

**Verification Results:**
- ✅ Verified: No `AgenticCoder.Domain/Concurrency/` directory exists
- ✅ Verified: No `AgenticCoder.Application/Concurrency/` interfaces exist
- ✅ Verified: No `AgenticCoder.Infrastructure/Concurrency/` implementations exist
- ✅ Verified: No worktree binding test files exist
- ✅ Verified: Database migration for `worktree_bindings` table does not exist

**Partial Dependencies Found:**
- ✅ Domain entities exist: `ChatId`, `WorktreeId` (from 049a)
- ✅ Event system exists: `IEventPublisher` (Task 023)
- ⚠️ Git worktree detection: Not yet implemented (Task 022 - out of scope for 049c)

---

## SECTION 3: PRODUCTION FILES NEEDED (15 files)

### Domain Layer (2 files - NEW)

**File 1: `src/AgenticCoder.Domain/Concurrency/WorktreeBinding.cs`**
- Entity representing one-to-one chat-to-worktree association
- Properties: WorktreeId, ChatId, CreatedAt
- Methods: Create(), Reconstitute() (for persistence)
- Constraints: One-to-one relationship enforced in repository

**File 2: `src/AgenticCoder.Domain/Concurrency/WorktreeLock.cs`**
- Entity representing lock metadata
- Properties: WorktreeId, ProcessId, LockedAt, Hostname, Terminal
- Methods: Age (property), IsStale(threshold), IsOwnedByCurrentProcess(), CreateForCurrentProcess()
- Serializable to JSON for file-based locking

### Application Layer (3 files - NEW)

**File 3: `src/AgenticCoder.Application/Concurrency/IBindingService.cs`**
- Interface with methods:
  - GetBoundChatAsync(WorktreeId) → ChatId?
  - GetBoundWorktreeAsync(ChatId) → WorktreeId?
  - CreateBindingAsync(WorktreeId, ChatId)
  - DeleteBindingAsync(WorktreeId)
  - ListAllBindingsAsync() → IReadOnlyList<WorktreeBinding>

**File 4: `src/AgenticCoder.Application/Concurrency/ILockService.cs`**
- Interface with methods:
  - AcquireAsync(WorktreeId, timeout?, ct) → IAsyncDisposable
  - GetStatusAsync(WorktreeId, ct) → LockStatus
  - ReleaseStaleLocksAsync(threshold, ct)
  - ForceUnlockAsync(WorktreeId, ct)
- Record: LockStatus(IsLocked, IsStale, Age, ProcessId?, Hostname?, Terminal?)

**File 5: `src/AgenticCoder.Application/Concurrency/IContextResolver.cs`**
- Interface with methods:
  - ResolveActiveChatAsync(WorktreeId, ct) → ChatId?
  - DetectCurrentWorktreeAsync(currentDirectory, ct) → WorktreeId?
  - NotifyContextSwitchAsync(from, to, ct)

### Infrastructure Layer - Binding (2 files - NEW)

**File 6: `src/AgenticCoder.Infrastructure/Concurrency/SqliteBindingRepository.cs`**
- Implements: IBindingRepository (internal interface) and optionally IBindingService
- Uses parameterized SQL queries (AC-093 security requirement)
- Database table: `worktree_bindings` with unique constraint on `worktree_id` column
- Methods: GetByWorktreeAsync(), GetByChatAsync(), CreateAsync() with one-to-one validation, DeleteAsync(), DeleteByChatAsync(), ListAllAsync()
- Logging: INFO on create/delete, WARNING on validation failures
- Required: Query latency < 5ms (AC-019), Cache hit rate > 95% (AC-020)

**File 7: `src/AgenticCoder.Infrastructure/Concurrency/ValidatedBindingCache.cs`**
- Decorator/wrapper around binding repository with in-memory caching
- Validates cached chat still exists before returning (AC-095)
- Removes invalid bindings automatically (AC-096)
- Achieves >95% hit rate for repeated queries (AC-020)

### Infrastructure Layer - Locking (5 files - NEW)

**File 8: `src/AgenticCoder.Infrastructure/Concurrency/AtomicFileLockService.cs`**
- Implements: ILockService
- Location: `.agent/locks/` directory (AC-037)
- Lock file format: JSON with ProcessId, LockedAt, Hostname, Terminal (AC-038)
- Permissions (Unix): 600 (owner read/write only) (AC-039)
- Atomic write: temp file → rename (AC-047) (AC-082)
- Verification: re-read file to confirm ownership (AC-048)
- Stale threshold: 5 minutes configurable (AC-056, AC-057)
- Wait polling: 2-second intervals (AC-050, AC-074)
- Latency targets: Acquire <10ms (AC-045), Release <5ms (AC-046)
- Exceptions: LockBusyException (AC-041), TimeoutException (AC-053), LockCorruptedException

**File 9: `src/AgenticCoder.Infrastructure/Concurrency/SafeLockPathResolver.cs`**
- Prevents path traversal attacks (AC-091)
- Validates worktree IDs before file path construction
- Returns safe lock file path within `.agent/locks/`

**File 10: `src/AgenticCoder.Infrastructure/Concurrency/LockFileValidator.cs`**
- Validates lock file permissions (AC-094, AC-098)
- Checks process ID still running (AC-061)
- Validates hostname (AC-062) - prevents cross-machine tampering
- Used by AtomicFileLockService to verify lock integrity

**File 11: `src/AgenticCoder.Infrastructure/Concurrency/WorktreeContextResolver.cs`**
- Implements: IContextResolver
- Resolves active chat for current worktree via binding lookup
- Detects worktree via Git integration (depends on Task 022)
- Publishes ContextSwitchedEvent on context change (AC-174)
- Latency target: < 50ms (AC-023)

**File 12: `src/AgenticCoder.Infrastructure/Concurrency/StaleLockCleanupService.cs`**
- Background service or scheduled task
- Periodically scans `.agent/locks/` directory
- Removes stale locks (> 5 minutes old) (AC-059)
- Validates process ID not running (AC-061)
- Validates hostname matches (AC-062)
- Logs removal at WARNING level (AC-060)
- Triggered by: Application startup, manual `acode lock cleanup` command (AC-065)

### Infrastructure Layer - Data (1 file - NEW)

**File 13: `src/AgenticCoder.Infrastructure/Persistence/Migrations/Migration_YYYYMM_AddWorktreeBindings.cs`**
- Creates `worktree_bindings` table:
  - `worktree_id TEXT PRIMARY KEY`
  - `chat_id TEXT NOT NULL`
  - `created_at TEXT NOT NULL`
  - Foreign key to `chats(id)` for referential integrity
  - Unique constraint on `worktree_id` (one-to-one) (AC-018)
- Cascade delete when chat is purged (AC-083, AC-090)
- Soft delete handling: `acode chat delete` does NOT affect binding (AC-084), `acode chat purge` DOES (AC-083)

### CLI Layer (5 modifications/files - NEW)

**File 14: `src/AgenticCoder.Cli/Commands/Concurrency/ChatBindCommand.cs`**
- Command: `acode chat bind <chat-id>`
- Validates chat exists (AC-002)
- Validates worktree has no existing binding (AC-003)
- Validates chat not bound to different worktree (AC-004)
- Error ACODE-CONC-001/003/005 with clear message
- Exit codes: 0 success, 1 error

**File 15: `src/AgenticCoder.Cli/Commands/Concurrency/ChatUnbindCommand.cs`**
- Command: `acode chat unbind` with `--force` flag
- Prompts for confirmation by default (AC-006)
- Bypasses confirmation with `--force` (AC-007)
- Idempotent: succeeds silently if no binding exists (AC-008)
- Logs action at INFO level

**File 16: `src/AgenticCoder.Cli/Commands/Concurrency/ChatBindingsCommand.cs`**
- Command: `acode chat bindings [--json] [--cleanup]`
- Lists all bindings: worktree path, chat ID, chat title, creation timestamp (AC-010)
- JSON output: valid JSON array (AC-011)
- `--cleanup` flag: detects and removes orphaned bindings (AC-086/087)
- Cleanup prompts for confirmation, logs at INFO level (AC-088/089)

**File 17: `src/AgenticCoder.Cli/Commands/Concurrency/UnlockCommand.cs`**
- Command: `acode unlock --force`
- Deletes lock file regardless of owner (AC-054)
- Logs at WARNING level with lock details (AC-055)
- Used for emergency unlock

**File 18: Modification to `RunCommand.cs`** (EXISTING - ENHANCE)
- On command start: Acquire lock via ILockService (AC-036)
- On command end (success/error): Release lock in finally block (AC-042)
- Support `--wait` flag: queue with timeout (AC-049, AC-050, AC-052)
- Display progress: elapsed time, holder info (AC-051)
- Error codes: ACODE-CONC-001 (locked), ACODE-CONC-002 (timeout)
- Integrate context resolution: detect worktree, resolve bound chat, execute with correct context
- Auto-bind on `acode chat new`: unless `--no-bind` (AC-012/013)
- Confirmation message on auto-bind (AC-014)

---

## SECTION 4: TEST FILES NEEDED (6 files, 20+ tests)

### Unit Tests

**File 1: `Tests/Unit/Concurrency/BindingTests.cs` - 4 tests**
1. Should_Bind_Chat_To_Worktree - Create binding, verify stored
2. Should_Unbind_Chat - Create then delete, verify gone
3. Should_Enforce_OneToOne_Binding - Second binding to same worktree throws InvalidOperationException
4. Should_Persist_Binding - Survives process restart

**File 2: `Tests/Unit/Concurrency/LockTests.cs` - 5 tests**
1. Should_Acquire_Lock - Lock file created with process ID
2. Should_Release_Lock_On_Dispose - Lock file deleted on IAsyncDisposable.DisposeAsync()
3. Should_Detect_Stale_Lock - Lock > 5 minutes old marked stale
4. Should_Block_Concurrent_Acquisition - Second acquisition throws LockBusyException
5. Should_Queue_With_Wait_Timeout - Polls 2-second intervals until timeout

**File 3: `Tests/Unit/Concurrency/ContextTests.cs` - 3 tests**
1. Should_Switch_Context_On_Directory_Change - Different worktrees resolve different chats
2. Should_Isolate_Runs_By_Chat - Runs from different chats don't appear in each other's lists
3. Should_Record_Worktree_In_Run - Run.WorktreeId populated correctly

### Integration Tests

**File 4: `Tests/Integration/Concurrency/MultiSessionTests.cs` - 3 tests**
1. Should_Handle_Multiple_Terminals_Different_Worktrees - Two locks on different worktrees succeed simultaneously
2. Should_Queue_With_Wait_Flag - Terminal 1 holds lock, Terminal 2 waits and acquires after release
3. Should_Timeout_If_Lock_Not_Released - Wait times out if lock not released

**File 5: `Tests/Integration/Concurrency/BindingPersistenceTests.cs` - 2 tests**
1. Should_Survive_Application_Restart - Binding persists across application restarts
2. Should_Cascade_Delete_On_Chat_Purge - Binding deleted when chat is purged

### E2E Tests

**File 6: `Tests/E2E/Concurrency/WorktreeWorkflowE2ETests.cs` - 3 tests**
1. Should_Auto_Bind_New_Chat_In_Worktree - `acode chat new` creates binding automatically
2. Should_Switch_Context_On_Directory_Change - Changing directories switches active chat
3. Should_Handle_Lock_Conflict_Gracefully - Second terminal gets lock error, waits, then succeeds

---

## SECTION 5: EFFORT BREAKDOWN

| Component | ACs | Files | Impl Hours | Test Hours | Total |
|-----------|-----|-------|-----------|-----------|--------|
| Domain Entities | 2 | 2 | 1 | 1 | 2 |
| Application Interfaces | 3 | 3 | 1 | 0 | 1 |
| Binding Repository | 10 | 2 | 4 | 2 | 6 |
| Lock Service | 30 | 5 | 8 | 4 | 12 |
| Context Resolver | 15 | 2 | 3 | 2 | 5 |
| Database Migration | 2 | 1 | 1 | 1 | 2 |
| CLI Commands | 25 | 5 | 6 | 3 | 9 |
| Unit Tests | 12 | 3 | 0 | 5 | 5 |
| Integration Tests | 5 | 2 | 0 | 4 | 4 |
| E2E Tests | 3 | 1 | 0 | 3 | 3 |
| Documentation | 4 | - | 2 | 0 | 2 |
| **TOTAL** | **108** | **18** | **28-32** | **25-28** | **53-60** |

---

## SECTION 6: SEMANTIC COMPLETENESS

```
Task-049c Semantic Completeness = (ACs fully implemented / Total ACs) × 100

ACs Fully Implemented: 0
Total ACs: 108
Worktree Binding & Locking Coverage: 0%

Semantic Completeness: (0 / 108) × 100 = 0%
```

**Breakdown by Domain:**
- Binding Commands: 0/20 (0%)
- Context Resolution: 0/15 (0%)
- Lock Acquisition/Release: 0/20 (0%)
- Stale Lock Handling: 0/10 (0%)
- Multi-Session Scenarios: 0/15 (0%)
- Cascade Operations: 0/10 (0%)
- Security: 0/8 (0%)
- Cross-Cutting: 0/10 (0%)

---

## SECTION 7: CRITICAL ANALYSIS

### What's Done Well
- Specification is extremely detailed with all ACs explicitly listed
- Implementation examples provided (100+ lines of complete code)
- Test implementations provided (200+ lines with Arrange-Act-Assert)
- Clear architecture with domain entities, application services, infrastructure implementations
- Performance targets specified (latency, cache hit rates)
- Security considerations well-documented (threat models with code examples)

### What's Missing (COMPLETE IMPLEMENTATION REQUIRED)

**Critical Path Dependencies:**
1. **Domain Layer** (1 hour) - WorktreeBinding and WorktreeLock entities
2. **Application Interfaces** (1 hour) - IBindingService, ILockService, IContextResolver
3. **Binding Infrastructure** (4 hours) - SqliteBindingRepository + ValidatedBindingCache
4. **Lock Service** (8 hours) - AtomicFileLockService with all edge cases
5. **Context Resolution** (3 hours) - WorktreeContextResolver integration
6. **Database Schema** (1 hour) - Migration for worktree_bindings table
7. **CLI Commands** (6 hours) - bind, unbind, bindings, unlock commands
8. **Comprehensive Testing** (20+ hours) - Unit, integration, E2E tests

### Recommended Implementation Order

1. **Phase 1: Domain & Application (2-3 hours)**
   - Create WorktreeBinding entity with persistence methods
   - Create WorktreeLock entity with stale detection
   - Define IBindingService, ILockService, IContextResolver interfaces
   - Write unit tests for entities

2. **Phase 2: Binding Infrastructure (6-8 hours)**
   - Implement SqliteBindingRepository with one-to-one validation
   - Create database migration for worktree_bindings table
   - Implement ValidatedBindingCache for performance
   - Write integration tests for binding persistence and cascade delete

3. **Phase 3: Lock Service (10-12 hours)**
   - Implement AtomicFileLockService with atomic file operations
   - Implement SafeLockPathResolver for path traversal prevention
   - Implement LockFileValidator for ownership/permission checks
   - Implement StaleLockCleanupService for background cleanup
   - Write comprehensive lock service tests (stale detection, concurrent acquisition, wait timeouts)

4. **Phase 4: Context Resolution (5-6 hours)**
   - Implement WorktreeContextResolver with binding lookup
   - Integrate worktree detection (placeholder until Task 022)
   - Publish ContextSwitchedEvent
   - Write E2E tests for context switching

5. **Phase 5: CLI Integration (8-10 hours)**
   - Implement ChatBindCommand, ChatUnbindCommand, ChatBindingsCommand, UnlockCommand
   - Enhance RunCommand with lock acquisition/release and context resolution
   - Enhance ChatNewCommand with auto-bind and --no-bind flag
   - Write CLI integration tests

6. **Phase 6: Testing & Polish (5-8 hours)**
   - Complete all unit tests (85%+ coverage)
   - Complete all integration tests (multi-session scenarios)
   - Complete E2E tests (full workflows)
   - Performance benchmarking

---

## IMPLEMENTATION COMPLETION CHECKLIST

The detailed implementation checklist with all gaps will be created in a separate file: `task-049c-completion-checklist.md`

Key items to address:
1. [ ] Domain entities (2 files, 1 hour)
2. [ ] Application interfaces (3 files, 1 hour)
3. [ ] Binding repository (2 files, 4 hours)
4. [ ] Lock service (5 files, 8 hours)
5. [ ] Context resolver (2 files, 3 hours)
6. [ ] Database migration (1 file, 1 hour)
7. [ ] CLI commands (5 files, 6 hours)
8. [ ] Unit tests (3 files, 5 hours)
9. [ ] Integration tests (2 files, 4 hours)
10. [ ] E2E tests (1 file, 3 hours)

---

## STATUS & RECOMMENDATIONS

**Status:** 🔴 COMPLETELY UNIMPLEMENTED - Clean slate, no production code exists

**Blocking Dependencies:**
- Task 022 (Git Worktree Integration) - Needed for worktree detection; can use placeholder/mock for now
- Task 023 (Event System) - Already exists, no blocker

**Recommendation:** Create completion checklist in 6 phases (Domain → Binding → Locking → Context → CLI → Testing) with clear success criteria and test-driven development approach. Estimated 55-60 hours total effort.

---
