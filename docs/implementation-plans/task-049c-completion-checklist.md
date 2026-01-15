# Task-049c Completion Checklist: Multi-Chat Concurrency Model + Run/Worktree Binding

**Status:** 🔴 0% COMPLETE (Clean Slate - No Code Exists)

**Objective:** Implement 108 acceptance criteria across 18 files using Test-Driven Development

**Methodology:** RED → GREEN → REFACTOR cycle, with per-phase commits

**Effort Estimate:** 55-60 hours total (6 phases, 2 weeks at full capacity)

---

## INSTRUCTIONS FOR FRESH AGENT

This checklist guides implementation from a completely empty state. Follow these steps:

1. **Read this entire document** to understand the full scope
2. **For each gap in order:**
   - Read the spec line numbers provided
   - Write test code FIRST (RED phase)
   - Run test, verify it fails with meaningful error
   - Write minimum production code (GREEN phase)
   - Refactor while keeping tests green
   - Mark gap as [✅] complete
   - Commit with message `feat(task-049c/phase-N): [Gap title]`
3. **Never skip sections** - Each section depends on previous completions
4. **Commit after each gap** - Don't batch multiple gaps per commit
5. **Update this file** as you work - Mark progress with [🔄] when starting, [✅] when done
6. **Performance testing** - Verify latency targets specified for each component
7. **Audit before PR** - Follow docs/AUDIT-GUIDELINES.md checklist

**Success Criteria:** All 108 ACs passing tests, 85%+ code coverage, no warnings/errors on build

---

## WHAT EXISTS (NONE - CLEAN SLATE)

**Already Available (from other tasks):**
- ✅ Domain value types: `ChatId`, `WorktreeId` (Task 049a)
- ✅ Event system: `IEventPublisher` (Task 023)
- ✅ Data access: SQLite integration (Dapper/ADO.NET) (Task 049a)

**NOT Available (Blocking on other tasks):**
- ❌ Git worktree detection: `IGitWorktreeDetector` (Task 022 - can use placeholder interface)

**Completely New (Must Implement):**
- ❌ WorktreeBinding and WorktreeLock domain entities
- ❌ IBindingService, ILockService, IContextResolver interfaces
- ❌ SqliteBindingRepository and related binding infrastructure
- ❌ AtomicFileLockService and related lock infrastructure
- ❌ WorktreeContextResolver with event publishing
- ❌ Database schema for worktree_bindings table
- ❌ CLI commands for binding/locking operations
- ❌ All test files (unit, integration, E2E)

---

## PHASE 0: DOMAIN ENTITIES & APPLICATION INTERFACES (2-3 hours, 4 tests)

**Objective:** Create domain models and service contracts for binding and locking

### Gap 1: WorktreeBinding Domain Entity [⬜]

**Spec Reference:** Lines 2560-2592 (Implementation Prompt - Domain Entities section)

**What to Implement:**
- File: `src/AgenticCoder.Domain/Concurrency/WorktreeBinding.cs`
- Public properties: `WorktreeId`, `ChatId`, `CreatedAt` (read-only)
- Factory method: `static Create(WorktreeId, ChatId)` - sets CreatedAt to UtcNow
- Persistence method: `static Reconstitute(WorktreeId, ChatId, DateTimeOffset)` - for loading from database
- No behavior beyond immutable value storage

**Test File to Write First (RED):**
- Create: `Tests/Unit/Concurrency/BindingTests.cs`
- Test 1: `Should_Create_Binding_With_Current_Timestamp()` - Verify Create() sets CreatedAt near UtcNow
- Test 2: `Should_Reconstitute_Binding_From_Persistence()` - Verify Reconstitute preserves all fields

**Success Criteria:**
- [✅] Entity compiles without errors
- [✅] Both tests pass
- [✅] Properties are read-only (no public setters)
- [✅] DateTimeOffset.UtcNow is within 2 seconds of test assertion

**Effort:** 30 minutes implementation + 30 minutes testing

**Evidence Placeholder:**
```
✅ Test Output: dotnet test Tests/Unit/Concurrency/BindingTests.cs --filter "Create|Reconstitute"
```

---

### Gap 2: WorktreeLock Domain Entity [⬜]

**Spec Reference:** Lines 2594-2643 (Implementation Prompt - Domain Entities section)

**What to Implement:**
- File: `src/AgenticCoder.Domain/Concurrency/WorktreeLock.cs`
- Public properties: `WorktreeId`, `ProcessId`, `LockedAt`, `Hostname`, `Terminal`
- Computed property: `Age` → `DateTimeOffset.UtcNow - LockedAt`
- Method: `IsStale(TimeSpan threshold)` → `Age > threshold`
- Method: `IsOwnedByCurrentProcess()` → `ProcessId == Environment.ProcessId`
- Static factory: `CreateForCurrentProcess(WorktreeId)` - captures current process, hostname, terminal
- Helper: Private static `GetTerminalId()` - returns TTY on Unix, session ID on Windows

**Test File to Write First (RED):**
- Add to: `Tests/Unit/Concurrency/BindingTests.cs` or create `Tests/Unit/Concurrency/LockEntityTests.cs`
- Test 1: `Should_Calculate_Age_From_Locked_At_Timestamp()`
- Test 2: `Should_Detect_Stale_Lock_After_5_Minutes()`
- Test 3: `Should_Identify_Current_Process_Ownership()`

**Success Criteria:**
- [✅] Entity compiles without errors
- [✅] All three tests pass
- [✅] Age calculation accurate to within 100ms
- [✅] Stale detection correctly uses threshold parameter
- [✅] Process ownership check uses Environment.ProcessId

**Effort:** 45 minutes implementation + 45 minutes testing

**Evidence Placeholder:**
```
✅ Test Output: dotnet test Tests/Unit/Concurrency/ --filter "Age|Stale|Ownership"
```

---

### Gap 3: IBindingService Application Interface [⬜]

**Spec Reference:** Lines 2649-2659 (Implementation Prompt - Application Layer Interfaces)

**What to Implement:**
- File: `src/AgenticCoder.Application/Concurrency/IBindingService.cs`
- Methods (async/await):
  - `Task<ChatId?> GetBoundChatAsync(WorktreeId worktreeId, CancellationToken ct)` - Query: find chat bound to worktree
  - `Task<WorktreeId?> GetBoundWorktreeAsync(ChatId chatId, CancellationToken ct)` - Query: find worktree binding chat
  - `Task CreateBindingAsync(WorktreeId worktreeId, ChatId chatId, CancellationToken ct)` - Command: create binding
  - `Task DeleteBindingAsync(WorktreeId worktreeId, CancellationToken ct)` - Command: remove binding
  - `Task<IReadOnlyList<WorktreeBinding>> ListAllBindingsAsync(CancellationToken ct)` - Query: all bindings
- No implementation required - interface only
- Add XML comments explaining each method (what AC it implements)

**Test File:** No unit tests for interface itself (interfaces are just contracts)

**Success Criteria:**
- [✅] Interface compiles without errors
- [✅] All 5 methods declared with correct signatures
- [✅] Uses nullable ChatId?/WorktreeId? for optional returns
- [✅] Cancellation tokens passed correctly

**Effort:** 15 minutes

**Evidence Placeholder:**
```
✅ Compilation: dotnet build src/AgenticCoder.Application/
```

---

### Gap 4: ILockService Application Interface [⬜]

**Spec Reference:** Lines 2661-2688 (Implementation Prompt - Application Layer Interfaces)

**What to Implement:**
- File: `src/AgenticCoder.Application/Concurrency/ILockService.cs`
- Methods (async/await):
  - `Task<IAsyncDisposable> AcquireAsync(WorktreeId worktreeId, TimeSpan? timeout, CancellationToken ct)` - Returns lock handle that releases on dispose
  - `Task<LockStatus> GetStatusAsync(WorktreeId worktreeId, CancellationToken ct)` - Query current lock status
  - `Task ReleaseStaleLocksAsync(TimeSpan threshold, CancellationToken ct)` - Cleanup: delete locks older than threshold
  - `Task ForceUnlockAsync(WorktreeId worktreeId, CancellationToken ct)` - Emergency: force delete lock regardless of owner
- Record: `LockStatus(bool IsLocked, bool IsStale, TimeSpan Age, int? ProcessId, string? Hostname, string? Terminal)`

**Test File:** No unit tests for interface itself

**Success Criteria:**
- [✅] Interface compiles
- [✅] AcquireAsync returns IAsyncDisposable for using statement
- [✅] Timeout parameter is optional (TimeSpan?)
- [✅] LockStatus record includes all fields
- [✅] XML comments document timeout=null behavior (no wait, fail fast)

**Effort:** 15 minutes

**Evidence Placeholder:**
```
✅ Compilation: dotnet build src/AgenticCoder.Application/
```

---

### Gap 5: IContextResolver Application Interface [⬜]

**Spec Reference:** Lines 2690-2705 (Implementation Prompt - Application Layer Interfaces)

**What to Implement:**
- File: `src/AgenticCoder.Application/Concurrency/IContextResolver.cs`
- Methods (async/await):
  - `Task<ChatId?> ResolveActiveChatAsync(WorktreeId currentWorktree, CancellationToken ct)` - Find active chat for worktree
  - `Task<WorktreeId?> DetectCurrentWorktreeAsync(string currentDirectory, CancellationToken ct)` - Detect worktree from path
  - `Task NotifyContextSwitchAsync(WorktreeId from, WorktreeId to, CancellationToken ct)` - Publish context switch event

**Test File:** No unit tests for interface itself

**Success Criteria:**
- [✅] Interface compiles
- [✅] All methods are async Task
- [✅] Nullable returns where appropriate

**Effort:** 10 minutes

**Evidence Placeholder:**
```
✅ Compilation: dotnet build src/AgenticCoder.Application/
```

---

## PHASE 1: BINDING INFRASTRUCTURE (6-8 hours, 8 tests)

**Objective:** Implement persistent worktree-chat bindings with one-to-one enforcement and caching

### Gap 6: Database Migration for worktree_bindings Table [⬜]

**Spec Reference:** Lines 17 (AC-017) mentions database storage with foreign key and unique constraint

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Persistence/Migrations/Migration_YYYYMM_AddWorktreeBindings.cs`
- Create table `worktree_bindings`:
  ```sql
  CREATE TABLE worktree_bindings (
      worktree_id TEXT PRIMARY KEY,
      chat_id TEXT NOT NULL,
      created_at TEXT NOT NULL,
      FOREIGN KEY (chat_id) REFERENCES chats(id),
      UNIQUE (worktree_id)  -- One-to-one enforcement (AC-018)
  );
  CREATE INDEX idx_worktree_bindings_chat_id ON worktree_bindings(chat_id);
  ```
- Migration should be idempotent (check if table exists)
- On chat purge: cascade delete binding via foreign key ON DELETE CASCADE

**Test File:** No unit test for migration itself (infrastructure test)

**Success Criteria:**
- [✅] Migration file created with timestamp
- [✅] dotnet ef database update applies migration without errors
- [✅] Table created with all columns
- [✅] Primary key on worktree_id enforces one-to-one
- [✅] Foreign key on chat_id with cascade delete
- [✅] Index on chat_id for reverse lookup (GetBoundWorktree)

**Effort:** 30 minutes

**Evidence Placeholder:**
```
✅ Migration applied: dotnet ef database update
✅ Schema verified: sqlite3 .agent/data/workspace.db ".schema worktree_bindings"
```

---

### Gap 7: SqliteBindingRepository Implementation [⬜]

**Spec Reference:** Lines 2711-2830 (Implementation Prompt - Infrastructure Implementations)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Concurrency/SqliteBindingRepository.cs`
- Implements: IBindingRepository (internal interface, or directly implement IBindingService)
- Constructor: Takes IDbConnection, ILogger<SqliteBindingRepository>
- Methods with parameterized SQL (AC-093 security):
  - `GetByWorktreeAsync(WorktreeId, ct)` - SELECT with @WorktreeId parameter
  - `GetByChatAsync(ChatId, ct)` - SELECT with @ChatId parameter
  - `CreateAsync(WorktreeBinding, ct)` - INSERT, validates no existing binding (AC-003)
  - `DeleteAsync(WorktreeId, ct)` - DELETE by worktree_id
  - `DeleteByChatAsync(ChatId, ct)` - DELETE by chat_id (for cascade)
  - `ListAllAsync(ct)` - SELECT all, ordered by created_at DESC
- Logging:
  - INFO on successful create/delete (AC-104)
  - WARNING on validation failures
- Performance: Query results < 5ms (AC-019, verified with Dapper)
- One-to-one validation: CreateAsync checks no existing binding first (AC-003)

**Test File to Write First (RED):**
- File: `Tests/Integration/Concurrency/BindingRepositoryTests.cs`
- Test 1: `Should_Create_Binding_In_Database()` - Insert, verify persisted
- Test 2: `Should_Retrieve_Binding_By_Worktree_Id()` - GetByWorktreeAsync returns correct binding
- Test 3: `Should_Retrieve_Binding_By_Chat_Id()` - GetByChatAsync returns correct binding
- Test 4: `Should_Enforce_One_To_One_Constraint()` - Second binding to same worktree throws InvalidOperationException
- Test 5: `Should_Delete_Binding_By_Worktree()` - DeleteAsync removes binding
- Test 6: `Should_List_All_Bindings_In_Order()` - ListAllAsync returns all in created_at DESC order
- Test 7: `Should_Use_Parameterized_Queries()` - Verify no string concatenation in SQL (inspect SQL strings)
- Test 8: `Should_Handle_Query_Timeout()` - Verify timeout handling on slow query

**Success Criteria:**
- [✅] All 8 tests pass
- [✅] One-to-one validation enforced
- [✅] SQL queries use parameterized statements (no string concat)
- [✅] Logging at correct levels (INFO for success, WARN for errors)
- [✅] Query latency < 5ms (measure with Stopwatch in test)
- [✅] Handles NULL returns correctly

**Effort:** 2 hours implementation + 2 hours testing

**Evidence Placeholder:**
```
✅ Tests Pass: dotnet test Tests/Integration/Concurrency/BindingRepositoryTests.cs
✅ SQL Inspection: Grep for "SELECT * FROM" (should have parameters)
✅ Latency Check: Benchmark query execution time
```

---

### Gap 8: ValidatedBindingCache Implementation [⬜]

**Spec Reference:** Lines AC-020 (>95% hit rate), AC-095 (validate chat exists), AC-096 (remove invalid)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Concurrency/ValidatedBindingCache.cs`
- Wraps IBindingRepository with in-memory caching
- Constructor: Takes IBindingRepository, ILogger
- Caching strategy:
  - Cache layer: ConcurrentDictionary<WorktreeId, WorktreeBinding?> for worktree lookups
  - TTL: No fixed TTL (cache invalidated on explicit removal or validation failure)
  - Hit rate tracking: Track cache hits/misses for metrics
- Validation on read:
  - Before returning cached binding, verify chat still exists (AC-095)
  - If chat deleted, remove binding from cache AND repository (AC-096)
  - Log validation failures at WARNING level
- Size limit: No explicit limit (can grow to number of worktrees)
- Thread-safe: Use ConcurrentDictionary

**Test File to Write First (RED):**
- Add to: `Tests/Unit/Concurrency/BindingTests.cs`
- Test 1: `Should_Return_Cached_Binding_On_Hit()` - Verify second query hits cache
- Test 2: `Should_Detect_Cache_Hit_Rate()` - Track hit rate, verify >95% possible
- Test 3: `Should_Validate_Chat_Still_Exists()` - Delete chat, re-query binding, verify cache removed
- Test 4: `Should_Remove_Invalid_Binding_From_Repository()` - Invalid binding deleted from DB

**Success Criteria:**
- [✅] All 4 tests pass
- [✅] Cache hit rate >95% for repeated queries
- [✅] Stale entries removed when chat deleted
- [✅] Thread-safe (concurrent access works correctly)
- [✅] Logging at WARN level for validation failures

**Effort:** 1.5 hours implementation + 1.5 hours testing

**Evidence Placeholder:**
```
✅ Tests Pass: dotnet test Tests/Unit/Concurrency/BindingTests.cs --filter "Cache"
✅ Hit Rate: >95% in benchmark test
```

---

## PHASE 2: LOCK SERVICE (10-12 hours, 8 tests)

**Objective:** Implement file-based worktree locking with atomic operations, stale detection, and wait support

### Gap 9: SafeLockPathResolver Implementation [⬜]

**Spec Reference:** Lines AC-091 (path traversal prevention), AC-092 (sanitize worktree ID)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Concurrency/SafeLockPathResolver.cs`
- Constructor: Takes `string workspaceRoot` and `ILogger`
- Method: `GetLockFilePath(WorktreeId worktreeId) → string`
  - Constructs: `Path.Combine(workspaceRoot, ".agent", "locks", $"{worktreeId.Value}.lock")`
  - Validates: worktreeId.Value contains no `../`, `/..`, or other traversal attempts
  - Returns: Absolute path to lock file
- Security: Throws InvalidOperationException if path traversal detected

**Test File to Write First (RED):**
- File: `Tests/Unit/Concurrency/LockPathResolverTests.cs`
- Test 1: `Should_Generate_Safe_Lock_Path()` - Normal worktree ID produces correct path
- Test 2: `Should_Prevent_Path_Traversal_Attacks()` - Path with `../` throws InvalidOperationException
- Test 3: `Should_Sanitize_Worktree_Id()` - Special characters sanitized or rejected

**Success Criteria:**
- [✅] All 3 tests pass
- [✅] Path construction uses Path.Combine (not string concat)
- [✅] Traversal attempts rejected
- [✅] Path is absolute (full filesystem path)

**Effort:** 45 minutes implementation + 45 minutes testing

**Evidence Placeholder:**
```
✅ Tests Pass: dotnet test Tests/Unit/Concurrency/LockPathResolverTests.cs
```

---

### Gap 10: AtomicFileLockService Implementation [⬜]

**Spec Reference:** Lines 2832-3024 (Complete AtomicFileLockService implementation in spec)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Concurrency/AtomicFileLockService.cs`
- Implements: ILockService
- Constructor: Takes `string workspaceRoot`, `ILogger<AtomicFileLockService>`
- Properties: `_locksDirectory`, `_logger`, `_pathResolver`
- Complex method `AcquireAsync()`:
  - Lock file path: `.agent/locks/{worktreeId}.lock`
  - Lock file content: JSON with ProcessId, LockedAt, Hostname, Terminal
  - Atomic write: Write to temp file, move with File.Move(overwrite: false)
  - Unix permissions: Set 600 (user read/write only) via File.SetUnixFileMode
  - Verification: Re-read file, verify ProcessId matches (AC-048)
  - Polling: On conflict, check stale (>5 min old, AC-058), retry if stale, else:
    - If no timeout: throw LockBusyException immediately (AC-041)
    - If timeout: wait 2 seconds, check again (AC-050), repeat until timeout or acquired
  - Timeout calculation: Track elapsed time, throw TimeoutException with duration (AC-053)
  - Logging: INFO on acquisition, WARNING on stale removal, DEBUG on poll attempts
- Method `GetStatusAsync()`:
  - Read lock file if exists, deserialize JSON
  - Calculate age: DateTimeOffset.UtcNow - data.LockedAt
  - Determine if stale: age > 5 minutes (configurable via IOptions)
  - Return: LockStatus with all fields populated
- Method `ReleaseStaleLocksAsync()`:
  - Scan `.agent/locks/` for all .lock files
  - For each: deserialize, check age against threshold
  - Delete files older than threshold (AC-032 - atomic rename)
  - Log each removal at WARNING level
- Method `ForceUnlockAsync()`:
  - Delete lock file if exists
  - Log at WARNING level with lock details (AC-055)
- Inner class `FileLock : IAsyncDisposable`:
  - Dispose: Delete lock file, log (AC-044 - handles exceptions without re-throw)

**Test File to Write First (RED):**
- File: `Tests/Unit/Concurrency/LockServiceTests.cs`
- Test 1: `Should_Acquire_Lock_And_Create_File()` - AcquireAsync creates lock file with correct JSON
- Test 2: `Should_Release_Lock_On_Dispose()` - Using(IAsyncDisposable) deletes lock file
- Test 3: `Should_Detect_Stale_Lock_After_5_Minutes()` - Lock with timestamp 10min old marked stale
- Test 4: `Should_Block_Concurrent_Acquisition()` - Second AcquireAsync on same worktree throws LockBusyException
- Test 5: `Should_Wait_Until_Lock_Available_With_Timeout()` - AcquireAsync(timeout:2s) polls 2-second intervals
- Test 6: `Should_Verify_Lock_Ownership()` - After acquire, re-read verifies ProcessId matches
- Test 7: `Should_Remove_Stale_Locks_Cleanup()` - ReleaseStaleLocksAsync deletes old locks
- Test 8: `Should_Force_Unlock_Any_Lock()` - ForceUnlockAsync deletes lock regardless of owner

**Success Criteria:**
- [✅] All 8 tests pass
- [✅] Lock file created with JSON content (ProcessId, LockedAt, Hostname, Terminal)
- [✅] Atomic rename used (File.Move with overwrite:false)
- [✅] Unix permissions set to 600 (owner read/write only)
- [✅] Ownership verification works (re-read and check ProcessId)
- [✅] Polling interval 2 seconds, timeout honored
- [✅] Latency targets met: acquire <10ms, release <5ms
- [✅] Stale detection accurate for 5-minute threshold
- [✅] All logs at correct level (INFO, WARNING, DEBUG)

**Effort:** 3.5 hours implementation + 2 hours testing

**Evidence Placeholder:**
```
✅ Tests Pass: dotnet test Tests/Unit/Concurrency/LockServiceTests.cs
✅ Latency Benchmark: Verify acquire <10ms, release <5ms
✅ File Permissions: ls -la .agent/locks/*.lock (should show 600)
```

---

### Gap 11: LockFileValidator Implementation [⬜]

**Spec Reference:** Lines 453-500 (Security Considerations - LockFileValidator 60 lines)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Concurrency/LockFileValidator.cs`
- Constructor: Takes `ILogger<LockFileValidator>`, `IProcessChecker` (interface for process existence checks)
- Method `ValidateAsync(string lockFilePath, CancellationToken ct) → ValidationResult`:
  - Check file exists (AC-478)
  - Check permissions (Unix only, must be 600) (AC-486-494)
  - Parse JSON lock data
  - Verify process still running (PID check) (AC-061)
  - Verify hostname matches (AC-062)
  - Return: ValidationResult with IsValid, ErrorMessage
- ValidationResult record: Immutable result with Is Valid, Reason

**Test File to Write First (RED):**
- File: `Tests/Unit/Concurrency/LockFileValidatorTests.cs`
- Test 1: `Should_Validate_Correct_Lock_File()` - Valid file passes
- Test 2: `Should_Reject_File_With_Wrong_Permissions()` - Non-600 permissions rejected (Unix)
- Test 3: `Should_Reject_Lock_From_Dead_Process()` - PID check fails for non-existent process
- Test 4: `Should_Reject_Lock_From_Different_Hostname()` - Hostname mismatch detected

**Success Criteria:**
- [✅] All 4 tests pass
- [✅] Permission check works on Unix (mocked on Windows tests)
- [✅] Process existence check works
- [✅] Hostname validation prevents cross-machine tampering
- [✅] Logs WARN on failed validation

**Effort:** 1 hour implementation + 1 hour testing

**Evidence Placeholder:**
```
✅ Tests Pass: dotnet test Tests/Unit/Concurrency/LockFileValidatorTests.cs
```

---

### Gap 12: StaleLockCleanupService Implementation [⬜]

**Spec Reference:** Lines AC-056-065 (Stale Lock Handling section)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Concurrency/StaleLockCleanupService.cs`
- Implements: IHostedService (for background task)
- Constructor: Takes `ILockService`, `ILogger`, `IOptions<WorkspaceOptions>` (for threshold config)
- Startup: Start background task on application start
- Background task:
  - Runs periodically (every 60 seconds)
  - Calls `ILockService.ReleaseStaleLocksAsync(threshold: 5 minutes)`
  - Logs start/completion at DEBUG level
  - On exception: Log ERROR, continue running
- Configuration: `workspace.lock_timeout_seconds` = 300 (configurable)

**Test File to Write First (RED):**
- File: `Tests/Integration/Concurrency/StaleLockCleanupTests.cs`
- Test 1: `Should_Run_Cleanup_On_Startup()` - Background task starts with application
- Test 2: `Should_Remove_Stale_Locks_Periodically()` - Cleanup task runs at regular interval

**Success Criteria:**
- [✅] All 2 tests pass
- [✅] Cleanup runs without blocking application startup
- [✅] Stale locks removed with threshold configured
- [✅] Exceptions handled gracefully (don't kill background task)

**Effort:** 1 hour implementation + 1 hour testing

**Evidence Placeholder:**
```
✅ Tests Pass: dotnet test Tests/Integration/Concurrency/StaleLockCleanupTests.cs
```

---

## PHASE 3: CONTEXT RESOLUTION (5-6 hours, 4 tests)

**Objective:** Implement automatic context switching based on worktree binding

### Gap 13: WorktreeContextResolver Implementation [⬜]

**Spec Reference:** Lines 3026-3094 (Implementation Prompt - WorktreeContextResolver)

**What to Implement:**
- File: `src/AgenticCoder.Infrastructure/Concurrency/WorktreeContextResolver.cs`
- Implements: IContextResolver
- Constructor: Takes `IBindingService`, `IGitWorktreeDetector`, `IEventPublisher`, `ILogger<WorktreeContextResolver>`
- Method `ResolveActiveChatAsync()`:
  - Call `IBindingService.GetBoundChatAsync(worktreeId)`
  - If found: Log DEBUG, return ChatId
  - If not found: Log DEBUG "no bound chat, using global/manual", return null
- Method `DetectCurrentWorktreeAsync()`:
  - Call `IGitWorktreeDetector.DetectAsync(currentDirectory)`
  - If found: Log DEBUG with path, return WorktreeId
  - If not found: Log DEBUG, return null
  - **Placeholder for Task 022:** If IGitWorktreeDetector not available, create IWorkTreeDetector interface with mock implementation that returns placeholder worktree
- Method `NotifyContextSwitchAsync()`:
  - Create `ContextSwitchedEvent(from, to, DateTimeOffset.UtcNow)`
  - Publish via `IEventPublisher.PublishAsync()`
  - Log INFO "Context switch: from → to"
- Performance: Entire resolve should be <50ms (AC-023)

**Test File to Write First (RED):**
- File: `Tests/Unit/Concurrency/ContextResolverTests.cs`
- Test 1: `Should_Resolve_Bound_Chat_For_Worktree()` - Binding exists, returns correct ChatId
- Test 2: `Should_Return_Null_When_No_Binding()` - No binding, returns null
- Test 3: `Should_Detect_Current_Worktree()` - Mock git detector, verify worktree detected
- Test 4: `Should_Publish_Context_Switch_Event()` - NotifyContextSwitchAsync publishes event

**Success Criteria:**
- [✅] All 4 tests pass
- [✅] Resolve latency <50ms (measure with Stopwatch)
- [✅] Logging at correct levels (DEBUG for context info, INFO for switches)
- [✅] Event published with correct payload
- [✅] Works with placeholder git detector (mock interface)

**Effort:** 2 hours implementation + 1.5 hours testing

**Evidence Placeholder:**
```
✅ Tests Pass: dotnet test Tests/Unit/Concurrency/ContextResolverTests.cs
✅ Latency Check: <50ms per spec
```

---

## PHASE 4: INTEGRATION & CLI COMMANDS (8-10 hours, 8 tests)

**Objective:** Create CLI commands for binding/locking operations and integrate lock/context into run lifecycle

### Gap 14: ChatBindCommand Implementation [⬜]

**Spec Reference:** Lines 1345-1348 (AC-001-004, Binding Commands section)

**What to Implement:**
- File: `src/AgenticCoder.Cli/Commands/Concurrency/ChatBindCommand.cs` (or `ChatCommand.cs` subcommand)
- Command: `acode chat bind <chat-id>`
- Parameters: ChatId (required)
- Logic:
  1. Get current worktree via `IContextResolver.DetectCurrentWorktreeAsync()`
  2. Verify chat exists via `IChatRepository.GetAsync(chatId)`
  3. Verify worktree not already bound via `IBindingService.GetBoundChatAsync(worktreeId)`
  4. Verify chat not bound elsewhere via `IBindingService.GetBoundWorktreeAsync(chatId)`
  5. Create binding via `IBindingService.CreateBindingAsync()`
  6. Return success message with binding details
- Error handling:
  - Chat not found → Error ACODE-CONC-004
  - Worktree already bound → Error ACODE-CONC-003
  - Chat bound elsewhere → Error ACODE-CONC-005
  - Not in worktree → Error ACODE-CONC-004 (worktree not found)
- Exit codes: 0 success, 1 error
- Help flag: Supported (AC-099)

**Test File to Write First (RED):**
- File: `Tests/Integration/Concurrency/ChatBindCommandTests.cs`
- Test 1: `Should_Bind_Chat_To_Current_Worktree()` - Binding created successfully
- Test 2: `Should_Error_If_Chat_Not_Found()` - Non-existent chat ID returns error
- Test 3: `Should_Error_If_Worktree_Already_Bound()` - Existing binding prevents new one
- Test 4: `Should_Error_If_Chat_Bound_Elsewhere()` - Chat already bound to different worktree

**Success Criteria:**
- [✅] All 4 tests pass
- [✅] Binding created in database
- [✅] All three error cases handled with correct error codes
- [✅] Exit codes: 0 on success, 1 on error
- [✅] Help flag works: `acode chat bind --help`

**Effort:** 1.5 hours implementation + 1 hour testing

**Evidence Placeholder:**
```
✅ Tests Pass: dotnet test Tests/Integration/Concurrency/ChatBindCommandTests.cs
✅ Integration: acode chat bind <existing-chat-id> succeeds
```

---

### Gap 15: ChatUnbindCommand Implementation [⬜]

**Spec Reference:** Lines 1349-1352 (AC-005-008, Unbind Commands)

**What to Implement:**
- File: `src/AgenticCoder.Cli/Commands/Concurrency/ChatUnbindCommand.cs`
- Command: `acode chat unbind [--force]`
- Logic:
  1. Get current worktree
  2. Get binding via `IBindingService.GetBoundChatAsync()`
  3. If no binding: Return success silently (idempotent, AC-008)
  4. If binding exists:
     - If --force flag: Delete without prompt
     - Else: Prompt "Unbind <ChatTitle> from this worktree? (y/n)" (AC-006)
     - If user confirms: Delete via `IBindingService.DeleteBindingAsync()`
  5. Return success message
- Help flag: Supported (AC-099)

**Test File to Write First (RED):**
- File: `Tests/Integration/Concurrency/ChatUnbindCommandTests.cs`
- Test 1: `Should_Unbind_Existing_Binding()` - Binding deleted
- Test 2: `Should_Prompt_For_Confirmation()` - User prompted before delete
- Test 3: `Should_Skip_Confirmation_With_Force_Flag()` - --force bypasses prompt
- Test 4: `Should_Return_Success_When_No_Binding()` - Idempotent, no error if not bound

**Success Criteria:**
- [✅] All 4 tests pass
- [✅] Binding deleted from database
- [✅] Confirmation prompt appears (can mock in test)
- [✅] Idempotent (no error if no binding)

**Effort:** 1 hour implementation + 1 hour testing

**Evidence Placeholder:**
```
✅ Tests Pass: dotnet test Tests/Integration/Concurrency/ChatUnbindCommandTests.cs
```

---

### Gap 16: ChatBindingsCommand Implementation [⬜]

**Spec Reference:** Lines 1353-1356 (AC-009-011, Listing bindings)

**What to Implement:**
- File: `src/AgenticCoder.Cli/Commands/Concurrency/ChatBindingsCommand.cs`
- Command: `acode chat bindings [--json] [--cleanup]`
- Without flags: List all bindings in table format
  - Columns: Worktree Path, Chat ID, Chat Title, Created At
  - Format: ASCII table or similar (AC-010)
- With --json: Output as valid JSON array (AC-011)
- With --cleanup: Detect and remove orphaned bindings
  - Orphaned = binding exists but worktree directory doesn't
  - Prompt for confirmation (AC-088)
  - Delete via repository
  - Log at INFO level (AC-089)
  - Return count of cleaned bindings

**Test File to Write First (RED):**
- File: `Tests/Integration/Concurrency/ChatBindingsCommandTests.cs`
- Test 1: `Should_List_All_Bindings_In_Table()` - All bindings displayed
- Test 2: `Should_Output_Valid_Json_With_Flag()` - JSON output is valid
- Test 3: `Should_Detect_Orphaned_Bindings()` - Bindings to non-existent worktrees detected
- Test 4: `Should_Cleanup_Orphaned_Bindings()` - Orphaned bindings removed

**Success Criteria:**
- [✅] All 4 tests pass
- [✅] All bindings listed
- [✅] JSON output parses correctly
- [✅] Orphaned detection works
- [✅] Cleanup removes orphaned entries

**Effort:** 1.5 hours implementation + 1 hour testing

**Evidence Placeholder:**
```
✅ Tests Pass: dotnet test Tests/Integration/Concurrency/ChatBindingsCommandTests.cs
```

---

### Gap 17: UnlockCommand Implementation [⬜]

**Spec Reference:** Lines 1404-1405 (AC-054-055, Force Unlock)

**What to Implement:**
- File: `src/AgenticCoder.Cli/Commands/Concurrency/UnlockCommand.cs`
- Command: `acode unlock --force [<worktree>]`
- If no worktree specified: Detect current worktree
- Call `ILockService.ForceUnlockAsync(worktreeId)`
- Return success message with lock details (holder PID, timestamp, hostname)
- Log at WARNING level (AC-055)
- Help flag: Supported (AC-099)

**Test File to Write First (RED):**
- File: `Tests/Integration/Concurrency/UnlockCommandTests.cs`
- Test 1: `Should_Force_Unlock_Current_Worktree()` - Lock file deleted
- Test 2: `Should_Force_Unlock_Specified_Worktree()` - Can unlock specific worktree
- Test 3: `Should_Display_Holder_Details()` - Output includes PID, hostname, terminal

**Success Criteria:**
- [✅] All 3 tests pass
- [✅] Lock file deleted
- [✅] Log entry at WARNING level
- [✅] Message shows lock details

**Effort:** 1 hour implementation + 30 minutes testing

**Evidence Placeholder:**
```
✅ Tests Pass: dotnet test Tests/Integration/Concurrency/UnlockCommandTests.cs
```

---

### Gap 18: LockStatusCommand Implementation [⬜]

**Spec Reference:** Lines 1424-1426 (AC-063-064, Lock Status)

**What to Implement:**
- File: `src/AgenticCoder.Cli/Commands/Concurrency/LockStatusCommand.cs`
- Command: `acode lock status [<worktree>]`
- If no worktree: Detect current
- Call `ILockService.GetStatusAsync(worktreeId)`
- Display:
  - IsLocked: true/false
  - Age: "2 minutes 34 seconds" or "no lock"
  - IsStale: "[STALE]" indicator if true (AC-064)
  - Holder info: PID, hostname, terminal (if locked)
- Color output: Locked/stale in red, available in green

**Test File:** Add to ChatBindingsCommandTests (simple command)

**Success Criteria:**
- [✅] Tests pass
- [✅] Status displayed clearly
- [✅] Stale indicator shown
- [✅] Holder details included if locked

**Effort:** 45 minutes implementation

**Evidence Placeholder:**
```
✅ Command works: acode lock status
```

---

### Gap 19: RunCommand Enhancement - Lock Acquisition & Context Resolution [⬜]

**Spec Reference:** Lines 1385-1397 (AC-036-048, Lock Acquisition), Lines 1368-1382 (AC-021-029, Context Resolution)

**What to Implement:**
- File: Modify `src/AgenticCoder.Cli/Commands/RunCommand.cs` (EXISTING)
- Add to command execution:

**Step 1: Detect Worktree (BEFORE executing chat command)**
```csharp
var worktreeId = await _contextResolver.DetectCurrentWorktreeAsync(
    Environment.CurrentDirectory,
    cancellationToken);

// If not in worktree and not using --no-context flag, warn user
if (worktreeId is null && !command.NoContext)
{
    _logger.LogWarning("Not in a Git worktree. Using global chat selection.");
}
```

**Step 2: Resolve Active Chat**
```csharp
ChatId? activeChatId = null;
if (worktreeId is not null)
{
    activeChatId = await _contextResolver.ResolveActiveChatAsync(worktreeId, cancellationToken);
    if (activeChatId is not null)
    {
        _logger.LogInformation("Auto-selected bound chat for worktree");
    }
}

// Use activeChatId if present, else use user-provided or interactive selection
var chatId = activeChatId ?? command.ChatId ?? (await InteractiveChatSelection());
```

**Step 3: Acquire Lock (BEFORE running)**
```csharp
IAsyncDisposable? lockHandle = null;

if (worktreeId is not null)
{
    try
    {
        var timeout = command.Wait ? TimeSpan.FromMinutes(5) : (TimeSpan?)null;
        lockHandle = await _lockService.AcquireAsync(worktreeId, timeout, cancellationToken);
        _logger.LogInformation("Lock acquired for worktree {Worktree}", worktreeId);
    }
    catch (LockBusyException ex)
    {
        _console.WriteError($"Worktree locked by {ex.Holder.ProcessId} since {ex.Holder.LockedAt}");
        _console.WriteError("Use --wait to queue, or unlock with: acode unlock --force");
        return ExitCodes.LockBusy;
    }
    catch (TimeoutException)
    {
        _console.WriteError("Lock acquisition timeout. Use: acode unlock --force");
        return ExitCodes.Timeout;
    }
}
```

**Step 4: Execute Run (keeping lock held)**
```csharp
try
{
    // ... actual run execution with chatId, worktreeId ...
    await _runService.ExecuteRunAsync(chatId, worktreeId, command.Prompt, cancellationToken);
}
finally
{
    // Step 5: Release Lock (ALWAYS, even on error)
    if (lockHandle is not null)
    {
        await lockHandle.DisposeAsync();
        _logger.LogInformation("Lock released for worktree {Worktree}", worktreeId);
    }
}
```

**Command-line flags to support:**
- `acode run "prompt"` - Auto-detect worktree & chat
- `acode run "prompt" --no-context` - Skip auto-detection
- `acode run "prompt" --wait` - Queue if locked (wait up to 5 minutes)
- `acode run "prompt" --chat <id>` - Override auto-detected chat

**Test File to Write First (RED):**
- File: `Tests/Integration/Concurrency/RunCommandWithLockTests.cs`
- Test 1: `Should_Auto_Detect_Worktree_And_Resolve_Bound_Chat()` - Worktree detected, chat resolved
- Test 2: `Should_Acquire_Lock_Before_Running()` - Lock file created before execution
- Test 3: `Should_Release_Lock_After_Run_Completes()` - Lock deleted on completion
- Test 4: `Should_Error_If_Worktree_Locked()` - LockBusyException returned if already locked
- Test 5: `Should_Wait_For_Lock_With_Wait_Flag()` - Poll and retry with --wait
- Test 6: `Should_Timeout_After_5_Minutes()` - Timeout error on --wait after 5min
- Test 7: `Should_Use_Auto_Detected_Chat_When_Bound()` - Run uses bound chat without explicit flag
- Test 8: `Should_Override_Auto_Chat_With_Explicit_Flag()` - --chat flag overrides auto-detection

**Success Criteria:**
- [✅] All 8 tests pass
- [✅] Worktree detected automatically
- [✅] Context (chat) resolved automatically
- [✅] Lock acquired on start, released on end
- [✅] Lock prevents concurrent execution same worktree
- [✅] --wait flag queues execution
- [✅] Timeout honored (5 minutes default)
- [✅] Error codes returned correctly
- [✅] Lock released even on exception (finally block)

**Effort:** 2.5 hours implementation + 2 hours testing

**Evidence Placeholder:**
```
✅ Tests Pass: dotnet test Tests/Integration/Concurrency/RunCommandWithLockTests.cs
✅ Lock file created/deleted: Verified in test
✅ Concurrent execution prevented: Second terminal gets BUSY error
```

---

## PHASE 5: AUTOBING ON CHAT CREATION & CLI STATUS (2-3 hours, 2 tests)

**Objective:** Auto-bind new chats to worktree by default

### Gap 20: ChatNewCommand Enhancement - Auto-Bind [⬜]

**Spec Reference:** Lines 1356-1359 (AC-012-014, Auto-bind on chat new)

**What to Implement:**
- File: Modify `src/AgenticCoder.Cli/Commands/ChatNewCommand.cs` (EXISTING)
- Add logic:

**On chat creation (before saving):**
```csharp
// Detect current worktree
var worktreeId = await _contextResolver.DetectCurrentWorktreeAsync(
    Environment.CurrentDirectory,
    cancellationToken);

// Parse --no-bind flag
bool autoBind = !command.NoBind;

// If in worktree and auto-bind enabled: create binding
if (worktreeId is not null && autoBind)
{
    // After chat is created, bind it
    try
    {
        await _bindingService.CreateBindingAsync(worktreeId, newChat.Id, cancellationToken);
        _console.WriteLine($"✓ Bound to worktree (auto-bind enabled)");
        _logger.LogInformation("Auto-bound chat {ChatId} to worktree {Worktree}",
            newChat.Id, worktreeId);
    }
    catch (InvalidOperationException)
    {
        // Worktree already has binding
        _console.WriteWarning("Note: Worktree already bound to another chat");
        _logger.LogWarning("Auto-bind failed: worktree already bound");
    }
}
```

**Flags:**
- `acode chat new "Title"` - Auto-binds (default)
- `acode chat new --no-bind "Title"` - Creates unbound chat (AC-013)

**Success Criteria:**
- [✅] Binding created automatically
- [✅] Confirmation message shown (AC-014)
- [✅] --no-bind flag skips binding
- [✅] Logs at INFO level

**Effort:** 1 hour implementation

**Evidence Placeholder:**
```
✅ Auto-bind works: acode chat new "Test" creates binding
✅ No-bind works: acode chat new --no-bind "Test" skips binding
```

---

### Gap 21: StatusCommand Enhancement - Show Worktree & Lock Status [⬜]

**Spec Reference:** Lines 1372-1376 (AC-024-026, Status display)

**What to Implement:**
- File: Modify `src/AgenticCoder.Cli/Commands/StatusCommand.cs` (EXISTING)
- Add to status output:

```
Worktree:
  Current: feature-auth
  Bound Chat: Chat-ABC123 ("Auth Implementation")

Lock Status:
  Status: AVAILABLE (not locked)
  Last Checked: 2026-01-15 14:30:00Z
```

Or if locked:

```
Lock Status:
  Status: ⚠️  LOCKED
  Holder: PID 12345 (acode process)
  Terminal: /dev/ttys001
  Since: 2 minutes 34 seconds ago
  Hostname: dev-machine-01
  Suggestion: Use --wait to queue, or force-unlock if stuck
```

**Test File:** Add to existing status tests

**Success Criteria:**
- [✅] Worktree ID displayed
- [✅] Bound chat shown (or "unbound")
- [✅] Lock status shown (available/locked, with age if locked)
- [✅] Holder details displayed if locked

**Effort:** 30 minutes implementation

**Evidence Placeholder:**
```
✅ Status command shows worktree/lock: acode status
```

---

## PHASE 6: FINAL VERIFICATION & DOCUMENTATION (3-5 hours)

**Objective:** Comprehensive testing, performance validation, audit, and documentation

### Gap 22: MultiSessionIntegrationTests [⬜]

**Spec Reference:** Lines 1947-2030 (Testing Requirements - MultiSessionTests.cs)

**What to Implement:**
- File: Create `Tests/Integration/Concurrency/MultiSessionTests.cs` with all examples from spec
- Test 1: `Should_Handle_Multiple_Terminals_Different_Worktrees()` - 2 terminals, 2 worktrees, both succeed
- Test 2: `Should_Queue_With_Wait_Flag()` - Terminal 1 holds lock, Terminal 2 waits and acquires
- Test 3: `Should_Timeout_If_Lock_Not_Released()` - Wait timeout respected

**Success Criteria:**
- [✅] All 3 tests pass
- [✅] No race conditions (both locks held simultaneously if different worktrees)
- [✅] Polling interval 2 seconds verified
- [✅] Timeout honored

**Effort:** 1 hour

**Evidence Placeholder:**
```
✅ Tests Pass: dotnet test Tests/Integration/Concurrency/MultiSessionTests.cs
```

---

### Gap 23: BindingPersistenceIntegrationTests [⬜]

**Spec Reference:** Lines 2032-2108 (Testing Requirements - BindingPersistenceTests.cs)

**What to Implement:**
- File: Create `Tests/Integration/Concurrency/BindingPersistenceTests.cs`
- Test 1: `Should_Survive_Application_Restart()` - Binding persists across process restart
- Test 2: `Should_Cascade_Delete_On_Chat_Purge()` - Binding deleted when chat purged

**Success Criteria:**
- [✅] Both tests pass
- [✅] Persistence across restart verified
- [✅] Cascade delete works correctly

**Effort:** 1 hour

**Evidence Placeholder:**
```
✅ Tests Pass: dotnet test Tests/Integration/Concurrency/BindingPersistenceTests.cs
```

---

### Gap 24: E2E WorktreeWorkflowTests [⬜]

**Spec Reference:** Lines 2111-2189 (Testing Requirements - WorktreeWorkflowE2ETests.cs)

**What to Implement:**
- File: Create `Tests/E2E/Concurrency/WorktreeWorkflowE2ETests.cs`
- Test 1: `Should_Auto_Bind_New_Chat_In_Worktree()` - Full end-to-end workflow
- Test 2: `Should_Switch_Context_On_Directory_Change()` - Multi-worktree context switching
- Test 3: `Should_Handle_Lock_Conflict_Gracefully()` - Lock prevents concurrent execution

**Success Criteria:**
- [✅] All 3 tests pass
- [✅] Full workflows tested end-to-end
- [✅] Realistic scenarios verified

**Effort:** 1.5 hours

**Evidence Placeholder:**
```
✅ Tests Pass: dotnet test Tests/E2E/Concurrency/WorktreeWorkflowE2ETests.cs
```

---

### Gap 25: Performance Benchmarks [⬜]

**Spec Reference:** Lines 2192-2199 (Performance Benchmarks)

**What to Implement:**
- File: Create `Tests/Performance/Concurrency/ConcurrencyBenchmarks.cs`
- Benchmarks (using BenchmarkDotNet or manual Stopwatch):
  - Context switch: Target 25ms, Max 50ms
  - Lock acquire: Target 5ms, Max 10ms
  - Lock release: Target 5ms, Max 10ms
  - Binding query: Target 2ms, Max 5ms (with cache, >95% hits)

**Test File:** Benchmark class with [Benchmark] methods

**Success Criteria:**
- [✅] All latencies within spec ranges
- [✅] Cache hit rate >95% for binding queries
- [✅] Results documented in comment

**Effort:** 1.5 hours

**Evidence Placeholder:**
```
✅ Benchmarks Pass: dotnet run -c Release -- ConcurrencyBenchmarks
Context switch:        24.5ms ✓
Lock acquire:           4.8ms ✓
Lock release:           3.2ms ✓
Binding query (cached):  1.2ms ✓
```

---

### Gap 26: Code Coverage Report [⬜]

**What to Verify:**
- Run: `dotnet test --collect:"XPlat Code Coverage"`
- Generate report: `ReportGenerator`
- Verify: >85% coverage for Concurrency components (AC-105)

**Success Criteria:**
- [✅] >85% coverage overall
- [✅] No untested critical paths
- [✅] Report generated and reviewed

**Effort:** 30 minutes

**Evidence Placeholder:**
```
✅ Coverage Report: 89% coverage (186/209 lines)
```

---

## FINAL AUDIT & COMPLETION

### Pre-PR Verification

1. [✅] All 108 ACs verified complete (manually or with test mapping)
2. [✅] All 18 files implemented
3. [✅] All 20+ tests passing
4. [✅] Build clean (no errors, warnings)
5. [✅] Code coverage >85%
6. [✅] Performance benchmarks within spec
7. [✅] Security validations working (path traversal, SQL params, file perms)
8. [✅] Logging at correct levels (INFO/WARNING/DEBUG)
9. [✅] Error codes defined and used (ACODE-CONC-001 through 007)
10. [✅] Documentation complete

### Commit Strategy

After each gap complete:
```bash
git add .
git commit -m "feat(task-049c/phase-N): [Gap title]"
git push origin feature/task-049c-concurrency
```

After all phases:
```bash
git log --oneline | head -26  # Verify one commit per gap
```

### PR Submission

```bash
gh pr create --title "Implement Task-049c: Multi-Chat Concurrency + Worktree Binding" \
  --body "Implements 108 ACs across domain, application, infrastructure, and CLI layers..."
```

---

## EFFORT SUMMARY

| Phase | Duration | Tests | Status |
|-------|----------|-------|--------|
| Phase 0: Domain & Interfaces | 2-3h | 4 | 🔴 Not started |
| Phase 1: Binding Infra | 6-8h | 8 | 🔴 Not started |
| Phase 2: Lock Service | 10-12h | 8 | 🔴 Not started |
| Phase 3: Context Resolution | 5-6h | 4 | 🔴 Not started |
| Phase 4: CLI Commands | 8-10h | 8 | 🔴 Not started |
| Phase 5: Auto-bind & Status | 2-3h | 2 | 🔴 Not started |
| Phase 6: Final Verification | 3-5h | - | 🔴 Not started |
| **TOTAL** | **55-60h** | **20+** | 🔴 Not started |

---

**Status:** 🔴 READY FOR IMPLEMENTATION

**Next Steps:** Begin Phase 0 with WorktreeBinding entity and BindingTests.cs

---
