# Task 049c Gap Analysis and Implementation

## INSTRUCTIONS FOR RESUMPTION AFTER CONTEXT COMPACTION

**Current Status**: Phases 0-3 COMPLETE, starting Phase 4 (AtomicFileLockService).

**What to do next**:
1. Read this entire file to understand completed work and remaining tasks
2. Continue from the last completed phase marker
3. Implement remaining components following TDD (Red-Green-Refactor)
4. Update this document as you complete each phase
5. Commit after each logical unit of work
6. Mark phase complete with ✅ when ALL verification criteria met

**Key Architecture Notes**:
- **Worktree Binding Model**: One-to-one relationship between Git worktrees and chats
- **File-Based Locking**: Locks stored in `.agent/locks/` within each worktree
- **Atomic Lock Acquisition**: Write temp file, rename to final (atomic operation)
- **Stale Lock Detection**: Locks >5 minutes old are automatically cleaned up
- **Context Resolution**: Automatic chat switching when changing directories between worktrees

**File Locations**:
- Task spec: `docs/tasks/refined-tasks/Epic 02/task-049c-multi-chat-concurrency-worktree-binding.md`
- Domain entities: `src/Acode.Domain/Concurrency/` (to be created)
- Application interfaces: `src/Acode.Application/Concurrency/` (to be created)
- Infrastructure implementations: `src/Acode.Infrastructure/Concurrency/` (to be created)
- CLI commands: `src/Acode.Cli/Commands/` (extend existing ChatCommand)
- Tests: `tests/Acode.*.Tests/Concurrency/` (to be created)

**Current Branch**: `feature/task-050-workspace-database-foundation` (will likely need new branch for 049c)

---

## Specification Requirements Summary

**From Acceptance Criteria** (lines 1341-1647):
- Total acceptance criteria items: 108 (AC-001 to AC-108)
- Binding commands: 20 ACs
- Context resolution: 15 ACs
- Lock acquisition/release: 20 ACs
- Stale lock handling: 10 ACs
- Multi-session scenarios: 15 ACs
- Cascade operations: 10 ACs
- Security: 8 ACs
- Cross-cutting: 10 ACs

**From Testing Requirements** (lines 1648-2555):
- BindingTests.cs: ~10 unit tests
- LockTests.cs: ~8 unit tests
- ContextTests.cs: ~6 unit tests
- MultiSessionTests.cs: ~5 integration tests
- BindingPersistenceTests.cs: ~8 integration tests
- **Total: ~37 tests expected**

**From Implementation Prompt** (lines 2556-3177):
- Domain entities: 2 files (WorktreeBinding.cs, WorktreeLock.cs)
- Application interfaces: 3 interfaces (IBindingService, ILockService, IContextResolver)
- Infrastructure implementations: 2 files (SqliteBindingRepository.cs, AtomicFileLockService.cs)
- CLI commands: 5 subcommands (bind, unbind, bindings, lock status, unlock)
- Migration: 1 SQL migration for worktree_bindings table
- **Total production files: ~10 files**

---

## Current Implementation State (VERIFIED)

### Existing Components

#### ✅ REUSABLE: src/Acode.Domain/Worktree/WorktreeId.cs
**Status**: Exists but needs modification
- ✅ File exists (59 lines)
- ✅ Uses ULID format (26-character string)
- ⚠️ **Issue**: Spec requires deterministic hash from worktree path (AC-034, AC-035)
- ⚠️ **Issue**: Current implementation uses ULID (random), not deterministic

**Evidence**:
```bash
$ grep "public static WorktreeId From" src/Acode.Domain/Worktree/WorktreeId.cs
public static WorktreeId From(string value) => new(value);

# Current: From(ulid_string) - accepts any 26-char string
# Required: From(worktree_path) - generates deterministic hash from path
```

**Required Work**:
- Modify WorktreeId.From() to accept worktree paths
- Add WorktreeId.FromPath(string path) that generates deterministic ID from path hash
- Keep compatibility with existing ULID format for database storage
- Verify AC-034, AC-035 requirements met

#### ✅ REUSABLE: src/Acode.Domain/Conversation/ChatId.cs
**Status**: Exists and compatible
- ✅ File exists
- ✅ Used in binding relationship
- ✅ No modifications required

#### ⚠️ PARTIALLY REUSABLE: Migration-Related Locking (NOT FOR WORKTREE LOCKING)
**Files**:
- src/Acode.Infrastructure/Persistence/Migrations/FileMigrationLock.cs
- src/Acode.Infrastructure/Persistence/Migrations/PostgreSqlAdvisoryLock.cs
- src/Acode.Application/Database/IMigrationLock.cs

**Status**: Exists but NOT usable for worktree locking
- These are for database migration locking (different purpose)
- Cannot be reused for worktree concurrency
- Need separate AtomicFileLockService per spec

---

### Missing Components (NOT STARTED)

#### ❌ MISSING: src/Acode.Domain/Concurrency/WorktreeBinding.cs
**Status**: File does not exist
- ❌ No domain entity for worktree-chat binding
- Spec lines 2561-2592 show complete implementation (32 lines)
- Methods required: Create(), Reconstitute()
- Properties: WorktreeId, ChatId, CreatedAt

**Required Work**:
- Create domain entity from spec
- Write tests first (BindingTests.cs)
- Verify immutability

#### ❌ MISSING: src/Acode.Domain/Concurrency/WorktreeLock.cs
**Status**: File does not exist
- ❌ No domain entity for lock metadata
- Spec lines 2594-2643 show complete implementation (50 lines)
- Methods required: CreateForCurrentProcess(), IsStale(), IsOwnedByCurrentProcess()
- Properties: ProcessId, LockedAt, Hostname, Terminal, Age

**Required Work**:
- Create domain entity from spec
- Write tests for Age, IsStale(), GetTerminalId()
- Handle Unix vs Windows terminal detection

#### ❌ MISSING: src/Acode.Application/Concurrency/IBindingService.cs
**Status**: File does not exist
- ❌ Interface for binding management not defined
- Spec lines 2649-2659 show interface definition
- Methods required: GetBoundChatAsync, GetBoundWorktreeAsync, CreateBindingAsync, DeleteBindingAsync, ListAllBindingsAsync

**Required Work**:
- Create interface from spec
- Will be implemented by BindingService in Infrastructure layer

#### ❌ MISSING: src/Acode.Application/Concurrency/ILockService.cs
**Status**: File does not exist
- ❌ Interface for lock management not defined
- Spec lines 2661-2688 show interface and LockStatus record
- Methods required: AcquireAsync, GetStatusAsync, ReleaseStaleLocksAsync, ForceUnlockAsync

**Required Work**:
- Create interface from spec
- Create LockStatus record type
- Will be implemented by AtomicFileLockService

#### ❌ MISSING: src/Acode.Application/Concurrency/IContextResolver.cs
**Status**: File does not exist
- ❌ Interface for context resolution not defined
- Spec lines 2690-2705 show interface definition
- Methods required: ResolveActiveChatAsync, DetectCurrentWorktreeAsync, NotifyContextSwitchAsync

**Required Work**:
- Create interface from spec
- Will be implemented by WorktreeContextResolver

#### ❌ MISSING: src/Acode.Infrastructure/Concurrency/SqliteBindingRepository.cs
**Status**: File does not exist
- ❌ Persistence layer for bindings not implemented
- Spec lines 2711-2830 show complete implementation (120 lines)
- Methods required: GetByWorktreeAsync, GetByChatAsync, CreateAsync, DeleteAsync, DeleteByChatAsync, ListAllAsync

**Required Work**:
- Create repository from spec
- Implement one-to-one constraint checking (AC-018)
- Write integration tests (BindingPersistenceTests.cs)

#### ❌ MISSING: src/Acode.Infrastructure/Concurrency/AtomicFileLockService.cs
**Status**: File does not exist
- ❌ File-based locking not implemented
- Spec lines 2832-2955+ show implementation (~200+ lines)
- Methods required: AcquireAsync, GetStatusAsync, ReleaseStaleLocksAsync, ForceUnlockAsync
- Features: Atomic rename, stale detection, wait/timeout, process verification

**Required Work**:
- Create lock service from spec
- Implement atomic write-rename pattern
- Implement stale lock cleanup (>5 minutes)
- Handle Unix permissions (600)
- Write unit tests (LockTests.cs)
- Write integration tests (MultiSessionTests.cs)

#### ❌ MISSING: Database Migration for worktree_bindings Table
**Status**: Migration not created
- ❌ No SQL migration for worktree_bindings table
- Required schema (from spec):
  ```sql
  CREATE TABLE worktree_bindings (
    worktree_id TEXT PRIMARY KEY,
    chat_id TEXT NOT NULL UNIQUE,
    created_at TEXT NOT NULL,
    FOREIGN KEY (chat_id) REFERENCES chats(id) ON DELETE CASCADE
  );
  ```

**Required Work**:
- Create migration file in `src/Acode.Infrastructure/Persistence/Migrations/`
- Follow existing migration pattern (XXX_Description.sql)
- Enforce one-to-one relationship via UNIQUE constraint on chat_id
- Add foreign key to chats table (AC-017)

#### ❌ MISSING: CLI Commands for Binding Management
**Status**: Commands not implemented
- ❌ `acode chat bind <chat-id>` (AC-001)
- ❌ `acode chat unbind` (AC-005)
- ❌ `acode chat bindings` (AC-009)

**Required Work**:
- Extend ChatCommand.cs with new subcommands
- Implement bind/unbind/bindings routing
- Add --force, --json flags
- Write CLI tests

#### ❌ MISSING: CLI Commands for Lock Management
**Status**: Commands not implemented
- ❌ `acode lock status` (AC-063)
- ❌ `acode unlock --force` (AC-054)
- ❌ `acode lock cleanup` (AC-065)

**Required Work**:
- Create LockCommand.cs
- Implement status/unlock/cleanup subcommands
- Add to command router
- Write CLI tests

#### ❌ MISSING: All Tests
**Status**: No tests exist for task-049c components
- ❌ BindingTests.cs (10 unit tests)
- ❌ LockTests.cs (8 unit tests)
- ❌ ContextTests.cs (6 unit tests)
- ❌ MultiSessionTests.cs (5 integration tests)
- ❌ BindingPersistenceTests.cs (8 integration tests)

**Required Work**:
- Create all test files from Testing Requirements section
- Follow exact test names and patterns from spec
- Verify all tests passing

---

## Gap Summary

### Files Requiring Work

| Category | Complete | Reusable (Modify) | Missing | Total |
|----------|----------|------------------|---------|-------|
| Domain | 0 | 1 (WorktreeId) | 2 | 3 |
| Application | 0 | 0 | 3 | 3 |
| Infrastructure | 0 | 0 | 2 | 2 |
| CLI Commands | 0 | 0 | 2 | 2 |
| Migrations | 0 | 0 | 1 | 1 |
| Tests | 0 | 0 | 5 | 5 |
| **TOTAL** | **0** | **1** | **15** | **16** |

**Completion Percentage**: 0% (0 complete / 16 total)

### Test Coverage Gap

| Component | Tests Expected | Tests Passing | Gap |
|-----------|---------------|---------------|-----|
| BindingTests | 10 | 0 | ❌ 10 missing |
| LockTests | 8 | 0 | ❌ 8 missing |
| ContextTests | 6 | 0 | ❌ 6 missing |
| MultiSessionTests | 5 | 0 | ❌ 5 missing |
| BindingPersistenceTests | 8 | 0 | ❌ 8 missing |
| **TOTAL** | **37** | **0** | **❌ 37 tests missing** |

**Test Completion Percentage**: 0% (0 passing / 37 expected)

---

## Strategic Implementation Plan

### Phase 0: Setup and Preparation
**Objective**: Create project structure, migration, and modify existing components.

**Files to Create/Modify**:
1. Create `src/Acode.Domain/Concurrency/` directory
2. Create `src/Acode.Application/Concurrency/` directory
3. Create `src/Acode.Infrastructure/Concurrency/` directory
4. Create `tests/Acode.Domain.Tests/Concurrency/` directory
5. Create `tests/Acode.Application.Tests/Concurrency/` directory
6. Create `tests/Acode.Infrastructure.Tests/Concurrency/` directory
7. Modify `src/Acode.Domain/Worktree/WorktreeId.cs` to support deterministic IDs
8. Create migration `004_WorktreeBindings.sql`

**TDD Process**:
1. RED: Write tests for WorktreeId.FromPath() (deterministic hash)
2. GREEN: Implement WorktreeId.FromPath() method
3. REFACTOR: Clean up, ensure backward compatibility
4. VERIFY: Run tests, verify AC-034 and AC-035

**Acceptance**:
- [ ] All directories created
- [ ] Migration file created with correct schema
- [ ] WorktreeId supports deterministic ID generation
- [ ] Tests passing for WorktreeId modifications
- [ ] Build GREEN (0 errors, 0 warnings)

---

### Phase 1: Domain Entities (WorktreeBinding, WorktreeLock)
**Objective**: Implement core domain entities for binding and locking.

**Files to Create**:
1. `src/Acode.Domain/Concurrency/WorktreeBinding.cs` (~32 lines from spec lines 2561-2592)
2. `src/Acode.Domain/Concurrency/WorktreeLock.cs` (~50 lines from spec lines 2594-2643)
3. `tests/Acode.Domain.Tests/Concurrency/WorktreeBindingTests.cs`
4. `tests/Acode.Domain.Tests/Concurrency/WorktreeLockTests.cs`

**TDD Process**:
1. RED: Create WorktreeBindingTests.cs with tests for Create, Reconstitute
2. GREEN: Implement WorktreeBinding entity
3. RED: Create WorktreeLockTests.cs with tests for Age, IsStale, GetTerminalId
4. GREEN: Implement WorktreeLock entity
5. REFACTOR: Ensure immutability, clean code
6. VERIFY: All domain entity tests passing

**Acceptance**:
- [ ] WorktreeBinding.cs created with immutable properties
- [ ] WorktreeLock.cs created with Age, IsStale methods
- [ ] Tests passing for both entities
- [ ] Build GREEN

---

### Phase 2: Application Interfaces ✅
**Objective**: Define application layer contracts for binding, locking, and context resolution.

**Files to Create**:
1. `src/Acode.Application/Concurrency/IBindingService.cs`
2. `src/Acode.Application/Concurrency/ILockService.cs`
3. `src/Acode.Application/Concurrency/IContextResolver.cs`
4. `src/Acode.Application/Concurrency/LockStatus.cs` (record type)
5. `src/Acode.Application/Concurrency/LockBusyException.cs`
6. `src/Acode.Application/Concurrency/LockCorruptedException.cs`

**TDD Process**:
1. Define interfaces from spec (no tests needed for interfaces)
2. Create exception types
3. VERIFY: Compilation succeeds

**Acceptance**:
- [x] All 3 interfaces created matching spec (IBindingService, ILockService, IContextResolver)
- [x] LockStatus record created
- [x] Exception types created (LockBusyException, LockCorruptedException)
- [x] Build GREEN (0 errors, 0 warnings, 1803 tests passing)

---

### Phase 3: Infrastructure - SqliteBindingRepository ✅
**Objective**: Implement binding persistence layer.

**Files to Create**:
1. `src/Acode.Infrastructure/Concurrency/SqliteBindingRepository.cs` (~120 lines from spec)
2. `tests/Acode.Infrastructure.Tests/Concurrency/SqliteBindingRepositoryTests.cs`

**TDD Process**:
1. RED: Create SqliteBindingRepositoryTests.cs (integration tests)
   - Test GetByWorktreeAsync returns null for non-existent binding
   - Test CreateAsync stores binding
   - Test CreateAsync enforces one-to-one constraint
   - Test DeleteAsync removes binding
   - Test ListAllAsync returns all bindings
2. GREEN: Implement SqliteBindingRepository
3. REFACTOR: Optimize queries, add logging
4. VERIFY: All repository tests passing

**Acceptance**:
- [x] SqliteBindingRepository.cs implemented (188 lines)
- [x] IBindingRepository.cs interface created
- [x] One-to-one constraint enforced (AC-018) via InvalidOperationException
- [x] All repository methods tested (9 tests)
- [x] Tests passing (9/9 SqliteBindingRepositoryTests)
- [x] Build GREEN (0 errors, 0 warnings, 1812 tests passing)

---

### Phase 4: Infrastructure - AtomicFileLockService
**Objective**: Implement file-based locking with atomic operations and stale detection.

**Files to Create**:
1. `src/Acode.Infrastructure/Concurrency/AtomicFileLockService.cs` (~200 lines)
2. `src/Acode.Infrastructure/Concurrency/FileLock.cs` (IAsyncDisposable implementation)
3. `src/Acode.Infrastructure/Concurrency/SafeLockPathResolver.cs` (path traversal prevention)
4. `tests/Acode.Infrastructure.Tests/Concurrency/AtomicFileLockServiceTests.cs`

**TDD Process**:
1. RED: Create AtomicFileLockServiceTests.cs
   - Test AcquireAsync creates lock file
   - Test AcquireAsync blocks concurrent acquisition
   - Test DisposeAsync releases lock
   - Test stale lock detection (>5 minutes)
   - Test lock wait with timeout
2. GREEN: Implement AtomicFileLockService
3. RED: Add MultiSessionTests.cs (integration tests)
   - Test two terminals, different worktrees
   - Test wait queue behavior
   - Test timeout behavior
4. GREEN: Ensure multi-session tests pass
5. REFACTOR: Optimize file I/O, add error handling
6. VERIFY: All lock tests passing

**Acceptance**:
- [ ] AtomicFileLockService.cs implemented
- [ ] Atomic write-rename pattern working (AC-047)
- [ ] Stale lock detection working (AC-056-065)
- [ ] Wait/timeout logic working (AC-049-053)
- [ ] Unix file permissions set (AC-039)
- [ ] All lock tests passing
- [ ] Build GREEN

---

### Phase 5: CLI Commands - Binding Management
**Objective**: Implement bind, unbind, bindings subcommands in ChatCommand.

**Files to Modify**:
1. `src/Acode.Cli/Commands/ChatCommand.cs` (add BindAsync, UnbindAsync, BindingsAsync methods)
2. `tests/Acode.Cli.Tests/Commands/ChatCommandTests.cs` (add binding tests)

**TDD Process**:
1. RED: Add tests for bind subcommand
   - Test bind creates binding
   - Test bind validates chat exists (AC-002)
   - Test bind fails if already bound (AC-003)
2. GREEN: Implement BindAsync method
3. RED: Add tests for unbind subcommand
   - Test unbind removes binding
   - Test unbind prompts for confirmation (AC-006)
   - Test --force bypasses confirmation (AC-007)
4. GREEN: Implement UnbindAsync method
5. RED: Add tests for bindings subcommand
   - Test bindings lists all
   - Test --json output (AC-011)
6. GREEN: Implement BindingsAsync method
7. VERIFY: All binding CLI tests passing

**Acceptance**:
- [ ] bind, unbind, bindings subcommands implemented
- [ ] Auto-bind in `new` command (AC-012)
- [ ] --no-bind flag supported (AC-013)
- [ ] Tests passing for all binding commands
- [ ] Build GREEN

---

### Phase 6: CLI Commands - Lock Management
**Objective**: Implement lock status, unlock, cleanup commands.

**Files to Create/Modify**:
1. `src/Acode.Cli/Commands/LockCommand.cs` (new command)
2. `tests/Acode.Cli.Tests/Commands/LockCommandTests.cs`
3. `src/Acode.Cli/Program.cs` (register LockCommand in router)

**TDD Process**:
1. RED: Create LockCommandTests.cs
   - Test status shows lock details (AC-063)
   - Test unlock removes lock (AC-054)
   - Test cleanup removes stale locks (AC-065)
2. GREEN: Implement LockCommand
3. REFACTOR: Add formatting, error messages
4. VERIFY: All lock command tests passing

**Acceptance**:
- [ ] LockCommand.cs created
- [ ] status, unlock, cleanup subcommands implemented
- [ ] --force flag for unlock (AC-054)
- [ ] Tests passing
- [ ] Build GREEN

---

### Phase 7: Context Resolution and Integration
**Objective**: Implement context resolution logic and integrate with run command.

**Files to Create/Modify**:
1. `src/Acode.Application/Concurrency/WorktreeContextResolver.cs`
2. `tests/Acode.Application.Tests/Concurrency/WorktreeContextResolverTests.cs`
3. Modify run command to use context resolver

**TDD Process**:
1. RED: Create WorktreeContextResolverTests.cs
   - Test ResolveActiveChatAsync returns bound chat
   - Test DetectCurrentWorktreeAsync finds worktree from path
   - Test fallback to global chat if unbound (AC-027)
2. GREEN: Implement WorktreeContextResolver
3. REFACTOR: Add caching, logging
4. VERIFY: All context resolution tests passing

**Acceptance**:
- [ ] WorktreeContextResolver.cs implemented
- [ ] Context resolution < 50ms (AC-023)
- [ ] Worktree detection working (AC-030-033)
- [ ] Tests passing
- [ ] Build GREEN

---

### Phase 8: E2E Integration Tests
**Objective**: Test full bind-run-unbind lifecycle.

**Files to Create**:
1. `tests/Acode.Cli.Tests/Integration/BindingLifecycleTests.cs`
2. `tests/Acode.Cli.Tests/Integration/LockConcurrencyTests.cs`

**TDD Process**:
1. Write E2E test: Create worktree, bind chat, run command, verify context switch
2. Write E2E test: Two terminals, different worktrees, verify isolation
3. Write E2E test: Two terminals, same worktree, verify locking
4. VERIFY: All E2E tests passing

**Acceptance**:
- [ ] E2E tests covering AC-106, AC-107
- [ ] Multi-session scenarios tested (AC-066-080)
- [ ] Tests passing
- [ ] Build GREEN

---

### Phase 9: Documentation and Audit
**Objective**: Create user documentation and run full audit.

**Files to Create/Modify**:
1. Update `docs/user-manual/concurrency.md` (if exists) or create it
2. Update `README.md` with binding/locking usage examples
3. Run audit per `docs/AUDIT-GUIDELINES.md`

**Audit Checklist**:
- [ ] All 108 acceptance criteria verified
- [ ] All 37 tests passing
- [ ] Build: 0 errors, 0 warnings
- [ ] No NotImplementedException in any file
- [ ] Performance targets met (AC-023, AC-045, AC-046)
- [ ] Security requirements met (AC-091-098)
- [ ] Documentation complete

**Acceptance**:
- [ ] Audit report created in `docs/audits/task-049c-audit.md`
- [ ] All audit criteria passed
- [ ] Documentation complete

---

## Execution Checklist

- [x] Phase 0: Setup and Preparation (COMPLETE - commit e7003b1)
  - [x] Created Concurrency directories in all layers
  - [x] Created migration 001_WorktreeBindings.sql
  - [x] Modified WorktreeId.FromPath() with deterministic hashing
  - [x] Tests: 18/18 passing (6 new FromPath tests)
  - [x] Build GREEN
- [ ] Phase 1: Domain Entities
- [ ] Phase 2: Application Interfaces
- [ ] Phase 3: Infrastructure - SqliteBindingRepository
- [ ] Phase 4: Infrastructure - AtomicFileLockService
- [ ] Phase 5: CLI Commands - Binding Management
- [ ] Phase 6: CLI Commands - Lock Management
- [ ] Phase 7: Context Resolution and Integration
- [ ] Phase 8: E2E Integration Tests
- [ ] Phase 9: Documentation and Audit
- [ ] All verification checks passed
- [ ] All 108 acceptance criteria verified
- [ ] All 37 tests passing (6 FromPath tests complete, 31 remaining)
- [ ] Build GREEN (0 errors, 0 warnings)

**Task Status**: ~11% complete (Phase 0 of 9 complete)

---

## Critical Reminders from Gap Analysis Methodology

1. **File Existence ≠ Feature Implemented**
   - Must verify no NotImplementedException
   - Must verify all methods from spec exist
   - Must verify tests passing

2. **Read Implementation Prompt Completely**
   - Contains complete production code
   - Shows exact method signatures
   - Prevents guesswork

3. **Read Testing Requirements Completely**
   - Contains complete test code
   - Shows expected test count
   - Defines test patterns

4. **No Self-Approved Deferrals**
   - If blocked, STOP and ask user
   - Do not assume anything is "future work"
   - All 108 ACs are in scope

5. **TDD is Mandatory**
   - RED: Write failing tests first
   - GREEN: Implement to pass
   - REFACTOR: Clean up
   - VERIFY: No NotImplementedException, all tests passing

---

**End of Gap Analysis Document**
