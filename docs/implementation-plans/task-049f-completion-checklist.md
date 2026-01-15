# Task-049f Completion Checklist: SQLite↔PostgreSQL Sync Engine

**Status:** 🔄 READY FOR IMPLEMENTATION - 146 ACs across 10 Implementation Phases
**Date Created:** 2026-01-15
**Semantic Completeness:** 15.7% (23/146 ACs) → Target: 100% (146/146 ACs)
**Gap Analysis Reference:** docs/implementation-plans/task-049f-semantic-gap-analysis.md

---

## HOW TO USE THIS CHECKLIST

This checklist breaks down the entire task-049f implementation into 10 sequential phases. Each phase:
1. Lists the ACs it covers (with spec line numbers)
2. Specifies test files to create (test-first TDD)
3. Specifies production files to create/complete
4. Provides implementation guidance
5. Shows success criteria (tests must pass)
6. Can be completed independently (no cross-phase dependencies)

**Workflow:**
- Start with Phase 1 (create test files first per TDD)
- Implement production code to make tests pass
- Mark [✅] when phase is complete (all tests passing)
- Proceed to Phase 2
- Do NOT skip phases or reorder without understanding dependencies

**Expected Effort:** 32-40 hours total (3-4 hours per phase)

---

## PHASE 1: Domain Entities - InboxEntry & ConflictPolicy

**Acceptance Criteria Covered:** AC-054-065, AC-066-078 (partial domain-level ACs)
**Tests Covered:** InboxEntryTests, ConflictPolicyTests
**Production Files:** InboxEntry.cs, ConflictPolicy.cs, OutboxStatus enum update
**Estimated Effort:** 2-3 hours
**Blocker Dependencies:** NONE

### Phase 1A: Test File - InboxEntryTests.cs [🔄]

**Location:** tests/Acode.Domain.Tests/Sync/InboxEntryTests.cs
**Expected Test Methods:** 6 tests

**Spec Reference:** Testing Requirements section (InboxEntry expected properties/methods from ConflictTests section)

**Tests to Implement (from spec patterns):**
```csharp
// Tests needed based on OutboxEntry pattern:
[Fact] public void Should_Create_Inbox_Entry()
[Fact] public void Should_Include_IdempotencyKey()
[Fact] public void Should_Mark_As_Applied()
[Fact] public void Should_Mark_As_Error()
[Fact] public void Should_Include_All_Properties() // Id, IdempotencyKey, EntityType, EntityId, Operation, Payload, Status, CreatedAt, AppliedAt
[Fact] public async Task Should_Support_Transactions() // Apply changes atomically
```

**Implementation Guidance:**
- InboxEntry should mirror OutboxEntry structure but for remote changes
- Properties needed: Id, IdempotencyKey, EntityType, EntityId, Operation, Payload, Status, CreatedAt, AppliedAt
- Methods: Create(static), MarkAsApplied(), MarkAsError()
- Status enum: Pending, Applied, Error (similar to OutboxStatus)

**Success Criteria:**
- [ ] InboxEntryTests.cs created with 6 test methods
- [ ] All tests compile
- [ ] Tests fail (RED state - no InboxEntry.cs yet)

### Phase 1B: Test File - ConflictPolicyTests.cs [🔄]

**Location:** tests/Acode.Domain.Tests/Sync/ConflictPolicyTests.cs
**Expected Test Methods:** 6 tests

**Spec Reference:** AC-066-078 (Conflict Detection and Resolution)

**Tests to Implement:**
```csharp
[Fact] public void Should_Define_LastWriteWins_Policy()
[Fact] public void Should_Define_FirstWriteWins_Policy()
[Fact] public void Should_Define_ManualResolution_Policy()
[Fact] public void Should_Define_CustomMerge_Policy()
[Fact] public void Should_Serialize_Policy()
[Fact] public void Should_Parse_Policy_From_String()
```

**Implementation Guidance:**
- ConflictPolicy as enum: LastWriteWins, FirstWriteWins, ManualResolution, CustomMerge
- Optional: ConflictPolicyConfig record for serialization
- Spec AC-068-072 shows custom patterns, but policy enum is foundation

**Success Criteria:**
- [ ] ConflictPolicyTests.cs created with 6 test methods
- [ ] All tests compile
- [ ] Tests fail (RED state)

### Phase 1C: Production - InboxEntry.cs [🔄]

**Location:** src/Acode.Domain/Sync/InboxEntry.cs
**Expected Lines:** ~180-220 lines (matching OutboxEntry pattern)
**Spec Reference:** AC-054-065 (Inbox Processing requirements)

**Implementation (from spec patterns):**
```csharp
namespace Acode.Domain.Sync;

public sealed class InboxEntry
{
    // Properties (matching OutboxEntry):
    public string Id { get; private set; }
    public string IdempotencyKey { get; private set; }
    public string EntityType { get; private set; }
    public string EntityId { get; private set; }
    public string Operation { get; private set; }
    public string Payload { get; private set; }
    public InboxStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? AppliedAt { get; private set; }
    
    // Methods:
    public static InboxEntry Create(string entityType, string entityId, string operation, string payload)
    public void MarkAsApplied() 
    public void MarkAsError(string errorMessage)
}

public enum InboxStatus { Pending, Applied, Error }
```

**Key Points:**
- InboxEntry represents incoming changes FROM remote PostgreSQL
- IdempotencyKey prevents duplicate application
- Status tracks processing state
- AppliedAt timestamp shows when change was applied locally

**Success Criteria:**
- [ ] InboxEntry.cs created with all properties and methods
- [ ] No NotImplementedException
- [ ] All InboxEntryTests pass (GREEN state)
- [ ] InboxStatus enum defined

### Phase 1D: Production - ConflictPolicy.cs [🔄]

**Location:** src/Acode.Domain/Sync/ConflictPolicy.cs
**Expected Lines:** ~30-50 lines
**Spec Reference:** AC-068-072 (Custom policies), AC-069-070 (Built-in policies)

**Implementation (simple enum foundation):**
```csharp
namespace Acode.Domain.Sync;

/// <summary>
/// Conflict resolution strategy.
/// </summary>
public enum ConflictPolicy
{
    /// <summary>
    /// Keep the version with the latest timestamp (AC-069)
    /// </summary>
    LastWriteWins = 0,
    
    /// <summary>
    /// Keep the original version (AC-070)
    /// </summary>
    FirstWriteWins = 1,
    
    /// <summary>
    /// Pause sync and prompt user (AC-071)
    /// </summary>
    ManualResolution = 2,
    
    /// <summary>
    /// Execute custom merge function (AC-072)
    /// </summary>
    CustomMerge = 3
}
```

**Success Criteria:**
- [ ] ConflictPolicy.cs created with all enum values
- [ ] All ConflictPolicyTests pass (GREEN state)

### Phase 1 Summary [🔄]

**Completion Checklist:**
- [ ] InboxEntryTests.cs created and all tests passing (6/6) ✅
- [ ] ConflictPolicyTests.cs created and all tests passing (6/6) ✅
- [ ] InboxEntry.cs created with all properties and methods ✅
- [ ] ConflictPolicy.cs created with all enum values ✅
- [ ] InboxStatus enum defined ✅
- [ ] No NotImplementedException in any file ✅
- [ ] No TODO/FIXME markers ✅
- [ ] Build: 0 errors, 0 warnings ✅

**When Phase 1 Complete:**
```bash
git add docs/implementation-plans/task-049f-completion-checklist.md
git commit -m "feat(task-049f): complete Phase 1 - InboxEntry & ConflictPolicy domain entities

Phase 1 Completion:
- InboxEntry.cs - created with full implementation
- ConflictPolicy.cs - created with policy enum
- InboxEntryTests.cs - 6 tests, all passing
- ConflictPolicyTests.cs - 6 tests, all passing

Verified:
- All tests passing (12/12)
- No NotImplementedException
- Build GREEN (0 errors, 0 warnings)

Covers ACs: AC-054-065 (partial), AC-066-078 (partial)"
```

---

## PHASE 2: Application Layer Interfaces

**Acceptance Criteria Covered:** AC-017-030, AC-054-065, AC-066-078 (interface-level contracts)
**Tests Covered:** N/A (interfaces only)
**Production Files:** IOutboxProcessor.cs, IInboxProcessor.cs, IConflictResolver.cs
**Estimated Effort:** 1 hour
**Blocker Dependencies:** NONE

### Phase 2A: IOutboxProcessor.cs [🔄]

**Location:** src/Acode.Application/Sync/IOutboxProcessor.cs
**Spec Reference:** AC-017-030 (Outbox Processing)

**Implementation:**
```csharp
namespace Acode.Application.Sync;

public interface IOutboxProcessor
{
    /// <summary>
    /// Process a batch of outbox entries.
    /// Sync to remote PostgreSQL and mark as completed on success.
    /// </summary>
    Task ProcessBatchAsync(IReadOnlyList<OutboxEntry> batch, CancellationToken ct);
    
    /// <summary>
    /// Process a single outbox entry.
    /// </summary>
    Task ProcessSingleAsync(OutboxEntry entry, CancellationToken ct);
}
```

**Success Criteria:**
- [ ] IOutboxProcessor.cs created with 2 methods
- [ ] Full documentation
- [ ] Compiles without errors

### Phase 2B: IInboxProcessor.cs [🔄]

**Location:** src/Acode.Application/Sync/IInboxProcessor.cs
**Spec Reference:** AC-054-065 (Inbox Processing)

**Implementation:**
```csharp
namespace Acode.Application.Sync;

public interface IInboxProcessor
{
    /// <summary>
    /// Fetch changes from remote PostgreSQL since last sync.
    /// </summary>
    Task<IReadOnlyList<InboxEntry>> FetchRemoteChangesAsync(DateTimeOffset lastSyncAt, CancellationToken ct);
    
    /// <summary>
    /// Process inbox entries and apply to local database.
    /// </summary>
    Task ProcessInboxAsync(IReadOnlyList<InboxEntry> entries, CancellationToken ct);
}
```

**Success Criteria:**
- [ ] IInboxProcessor.cs created with 2 methods
- [ ] Full documentation
- [ ] Compiles without errors

### Phase 2C: IConflictResolver.cs [🔄]

**Location:** src/Acode.Application/Sync/IConflictResolver.cs
**Spec Reference:** AC-066-078 (Conflict Detection and Resolution)

**Implementation:**
```csharp
namespace Acode.Application.Sync;

public interface IConflictResolver
{
    /// <summary>
    /// Detect if local and remote versions conflict.
    /// </summary>
    ConflictInfo? Detect(object local, object remote);
    
    /// <summary>
    /// Resolve conflict using configured policy.
    /// </summary>
    Task<object> ResolveAsync(ConflictInfo conflict, ConflictPolicy policy, CancellationToken ct);
}

public sealed record ConflictInfo(
    string EntityId,
    ConflictType Type,      // ModifyModify, DeleteModify, ModifyDelete
    int LocalVersion,
    int RemoteVersion,
    List<string> ConflictingFields,
    object LocalVersion,
    object RemoteVersion);

public enum ConflictType { ModifyModify, DeleteModify, ModifyDelete }
```

**Success Criteria:**
- [ ] IConflictResolver.cs created with 2 methods
- [ ] ConflictInfo record defined
- [ ] ConflictType enum defined
- [ ] Full documentation
- [ ] Compiles without errors

### Phase 2 Summary [🔄]

**Completion Checklist:**
- [ ] IOutboxProcessor.cs created ✅
- [ ] IInboxProcessor.cs created ✅
- [ ] IConflictResolver.cs created ✅
- [ ] ConflictInfo record defined ✅
- [ ] ConflictType enum defined ✅
- [ ] All files compile without errors ✅
- [ ] Full documentation on all interfaces ✅

**When Phase 2 Complete:**
```bash
git add src/Acode.Application/Sync/*
git commit -m "feat(task-049f): complete Phase 2 - application layer interfaces

Phase 2 Completion:
- IOutboxProcessor.cs - batch processing interface
- IInboxProcessor.cs - inbox processing interface
- IConflictResolver.cs - conflict resolution interface
- ConflictInfo record - conflict data structure
- ConflictType enum - conflict type categories

Verified:
- All interfaces compile (0 errors)
- Full documentation present
- Ready for Phase 3 implementations"
```

---

## PHASE 3: OutboxProcessor Implementation

**Acceptance Criteria Covered:** AC-017-030 (14 ACs) + AC-031-045 (partial - error handling)
**Tests Covered:** OutboxProcessorTests.cs
**Production Files:** OutboxProcessor.cs (Infrastructure)
**Estimated Effort:** 4-5 hours
**Blocker Dependencies:** Phase 2, Phase 7 (PostgreSQL repositories)

### Phase 3A: Test File - OutboxProcessorTests.cs [🔄]

**Location:** tests/Acode.Infrastructure.Tests/Sync/OutboxProcessorTests.cs
**Expected Test Methods:** 8 tests (from spec patterns)

**Tests to Implement (from Testing Requirements):**
```csharp
[Fact] public async Task Should_Process_Batch()           // AC-020
[Fact] public async Task Should_Mark_As_Completed()       // AC-021-022
[Fact] public async Task Should_Apply_Privacy_Filters()   // AC-064 integration
[Fact] public async Task Should_Respect_Rate_Limits()     // AC-030
[Fact] public async Task Should_Handle_Network_Loss()     // AC-025
[Fact] public async Task Should_Resume_On_Connectivity()  // AC-026
[Fact] public async Task Should_Batch_Multiple_Entries()  // AC-029
[Fact] public async Task Should_Return_Result_Metadata()  // Coverage
```

**Success Criteria:**
- [ ] OutboxProcessorTests.cs created with 8 tests
- [ ] All tests compile
- [ ] Tests fail (RED state)

### Phase 3B: Production - OutboxProcessor.cs [🔄]

**Location:** src/Acode.Infrastructure/Sync/OutboxProcessor.cs
**Expected Lines:** 250-350 lines
**Spec Reference:** AC-017-030 (14 ACs covering batch processing)

**Implementation Structure:**
```csharp
namespace Acode.Infrastructure.Sync;

public sealed class OutboxProcessor : IOutboxProcessor
{
    private readonly IOutboxBatcher _batcher;
    private readonly IRetryPolicy _retryPolicy;
    private readonly HttpClient _httpClient;      // For PostgreSQL API
    private readonly IOutboxRepository _outboxRepository;
    
    public async Task ProcessBatchAsync(IReadOnlyList<OutboxEntry> batch, CancellationToken ct)
    {
        // AC-017: Run on configurable polling interval
        // AC-018: Retrieve in creation order (FIFO)
        // AC-019: Batch respects size limit
        // AC-020: Send single HTTP request
        // AC-021-022: Mark completed + timestamp
        // AC-030: Respect rate limits
    }
    
    public async Task ProcessSingleAsync(OutboxEntry entry, CancellationToken ct)
    {
        // Single entry processing
    }
}
```

**Key Implementation Points (from spec):**
- Use OutboxBatcher to create batches from pending entries
- Send to PostgreSQL API endpoint (TBD - AC-130 configurable URL)
- On success: Mark entries as Completed (AC-021), set CompletedAt (AC-022)
- On transient errors: Increment retry count (AC-031), schedule retry (AC-033)
- On permanent errors: Move to dead letter (AC-036-037)
- Respect rate limits from remote server (AC-030)

**Success Criteria:**
- [ ] OutboxProcessor.cs created with ProcessBatchAsync() and ProcessSingleAsync()
- [ ] All OutboxProcessorTests pass (8/8) ✅
- [ ] Integrates with OutboxBatcher ✅
- [ ] Integrates with RetryPolicy ✅
- [ ] HTTP client properly configured ✅
- [ ] No NotImplementedException ✅

### Phase 3 Summary [🔄]

**Completion Checklist:**
- [ ] OutboxProcessorTests.cs created and all passing (8/8) ✅
- [ ] OutboxProcessor.cs created and fully implemented ✅
- [ ] Integration with OutboxBatcher verified ✅
- [ ] Integration with RetryPolicy verified ✅
- [ ] HTTP communication working ✅
- [ ] Build GREEN (0 errors, 0 warnings) ✅

**When Phase 3 Complete:**
```bash
git commit -m "feat(task-049f): complete Phase 3 - OutboxProcessor batch processing

Phase 3 Completion:
- OutboxProcessor.cs - batch sync to PostgreSQL
- OutboxProcessorTests.cs - 8 tests, all passing

Covers ACs: AC-017-030 (14 ACs - outbox processing)"
```

---

## PHASE 4: RetryPolicy & Circuit Breaker Completion

**Acceptance Criteria Covered:** AC-031-045 (15 ACs - retry mechanism)
**Tests Covered:** RetryPolicyTests (existing - enhance), CircuitBreakerTests (new)
**Production Files:** RetryPolicy.cs (enhance), CircuitBreaker.cs (new)
**Estimated Effort:** 2-3 hours
**Blocker Dependencies:** NONE (can implement independently)

### Phase 4A: Enhance RetryPolicy.cs [🔄]

**Missing from current implementation:**
- AC-034: Jitter of ±10% on retry delay
- AC-041-043: Circuit breaker integration
- AC-050: 409 Conflict response handling

**Implementation Enhancement:**
```csharp
// In RetryPolicy.cs, update CalculateBackoff():
private TimeSpan CalculateBackoffWithJitter(int retryCount)
{
    var baseDelay = GetExponentialBackoff(retryCount);  // 1s, 2s, 4s, ...
    
    // AC-034: Apply ±10% jitter
    var jitterPercent = (Random.Shared.NextDouble() - 0.5) * 0.2; // ±10%
    var jitterMs = (int)(baseDelay.TotalMilliseconds * jitterPercent);
    
    return TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds + jitterMs);
}

// Add circuit breaker field
private readonly CircuitBreaker _circuitBreaker;

public async Task<bool> ExecuteAsync(Func<Task<bool>> operation, CancellationToken ct)
{
    // AC-041: Check circuit breaker state
    if (_circuitBreaker.IsOpen)
        throw new CircuitBreakerOpenException("Circuit breaker is open");
    
    // Existing retry logic with jitter
}
```

**Success Criteria:**
- [ ] Jitter implementation added to RetryPolicy.cs ✅
- [ ] Circuit breaker integration added ✅
- [ ] All existing RetryPolicyTests still pass ✅
- [ ] Jitter tested with new test ✅

### Phase 4B: Production - CircuitBreaker.cs [🔄]

**Location:** src/Acode.Infrastructure/Sync/CircuitBreaker.cs
**Spec Reference:** AC-041-043 (Circuit breaker behavior)

**Implementation:**
```csharp
namespace Acode.Infrastructure.Sync;

public sealed class CircuitBreaker
{
    public bool IsOpen => _state == CircuitBreakerState.Open && DateTime.UtcNow < _openUntil;
    public CircuitBreakerState State => _state;
    
    public void RecordSuccess()  // AC-042: Reset after success
    {
        _consecutiveFailures = 0;
        _state = CircuitBreakerState.Closed;
    }
    
    public void RecordFailure()  // AC-041: Open after 5 consecutive failures
    {
        _consecutiveFailures++;
        if (_consecutiveFailures >= 5)
        {
            _state = CircuitBreakerState.Open;
            _openUntil = DateTime.UtcNow.AddSeconds(30);  // AC-042: 30 second timeout
        }
    }
    
    public void Reset()  // AC-043: Manual reset via CLI
    {
        _state = CircuitBreakerState.Closed;
        _consecutiveFailures = 0;
    }
}

public enum CircuitBreakerState { Closed, Open, HalfOpen }
```

**Success Criteria:**
- [ ] CircuitBreaker.cs created with Open/Closed/HalfOpen states ✅
- [ ] 5 consecutive failures trigger Open (AC-041) ✅
- [ ] 30 second timeout on Open (AC-042) ✅
- [ ] Manual reset (AC-043) ✅
- [ ] Tests passing ✅

### Phase 4 Summary [🔄]

**Completion Checklist:**
- [ ] RetryPolicy.cs enhanced with jitter (±10% per AC-034) ✅
- [ ] CircuitBreaker.cs created with all states ✅
- [ ] Integration between RetryPolicy and CircuitBreaker ✅
- [ ] All RetryPolicyTests still passing ✅
- [ ] All CircuitBreakerTests passing (new tests) ✅
- [ ] AC-031-045 now 100% covered ✅
- [ ] Build GREEN ✅

**When Phase 4 Complete:**
```bash
git commit -m "feat(task-049f): complete Phase 4 - retry mechanism with circuit breaker

Phase 4 Completion:
- RetryPolicy.cs - enhanced with jitter (±10%)
- CircuitBreaker.cs - new implementation with states
- Integration tested

Covers ACs: AC-031-045 (15 ACs - complete retry mechanism)"
```

---

## PHASE 5: InboxProcessor Implementation

**Acceptance Criteria Covered:** AC-054-065 (12 ACs - inbox processing)
**Tests Covered:** InboxProcessorTests.cs (5 tests)
**Production Files:** InboxProcessor.cs
**Estimated Effort:** 4-5 hours
**Blocker Dependencies:** Phase 1 (InboxEntry), Phase 2 (IInboxProcessor)

### Phase 5A: Test File - InboxProcessorTests.cs [🔄]

**Location:** tests/Acode.Infrastructure.Tests/Sync/InboxProcessorTests.cs
**Expected Test Methods:** 5 tests

**Tests to Implement:**
```csharp
[Fact] public async Task Should_Fetch_Remote_Changes()     // AC-055
[Fact] public async Task Should_Apply_Changes_To_Local_Db() // AC-058
[Fact] public async Task Should_Handle_Insert_Operations()  // AC-061
[Fact] public async Task Should_Handle_Update_Operations()  // AC-062
[Fact] public async Task Should_Handle_Delete_Operations()  // AC-063
```

**Success Criteria:**
- [ ] InboxProcessorTests.cs created with 5 tests
- [ ] All tests compile
- [ ] Tests fail (RED state)

### Phase 5B: Production - InboxProcessor.cs [🔄]

**Location:** src/Acode.Infrastructure/Sync/InboxProcessor.cs
**Expected Lines:** 250-300 lines
**Spec Reference:** AC-054-065 (12 ACs)

**Implementation Structure:**
```csharp
namespace Acode.Infrastructure.Sync;

public sealed class InboxProcessor : IInboxProcessor
{
    private readonly HttpClient _httpClient;      // Fetch from PostgreSQL
    private readonly IOutboxRepository _outboxRepository;
    
    public async Task<IReadOnlyList<InboxEntry>> FetchRemoteChangesAsync(
        DateTimeOffset lastSyncAt, CancellationToken ct)
    {
        // AC-054: Poll remote on configurable interval
        // AC-055: Download changes since lastSyncAt
        // AC-056: Write entries to local inbox table
    }
    
    public async Task ProcessInboxAsync(IReadOnlyList<InboxEntry> entries, CancellationToken ct)
    {
        // AC-057: Process in timestamp order
        // AC-058-063: Handle insert/update/delete
        // AC-064: Validate against local schema
        // AC-065: Move to error queue if invalid
    }
}
```

**Key Implementation Points (from spec AC-054-065):**
- Poll remote PostgreSQL API for changes since last sync timestamp (AC-054-055)
- Process entries in timestamp order (FIFO) (AC-057)
- Handle insert operations: INSERT into local SQLite (AC-061)
- Handle update operations: UPDATE existing records (AC-062)
- Handle delete operations: soft-delete with deleted_at timestamp (AC-063)
- Validate incoming data against local schema (AC-064)
- Move invalid entries to error queue (AC-065)
- Mark processed entries as Applied (AC-059)
- Update last sync timestamp atomically (AC-060)

**Success Criteria:**
- [ ] InboxProcessor.cs created with FetchRemoteChangesAsync() and ProcessInboxAsync() ✅
- [ ] All InboxProcessorTests pass (5/5) ✅
- [ ] Handles insert/update/delete operations ✅
- [ ] Validates schema and error handling ✅
- [ ] Integrates with InboxEntry ✅
- [ ] Build GREEN ✅

### Phase 5 Summary [🔄]

**Completion Checklist:**
- [ ] InboxProcessorTests.cs created and all passing (5/5) ✅
- [ ] InboxProcessor.cs created and fully implemented ✅
- [ ] Insert/Update/Delete operations working ✅
- [ ] Schema validation working ✅
- [ ] Error queue handling working ✅
- [ ] AC-054-065 now 100% covered ✅
- [ ] Build GREEN ✅

**When Phase 5 Complete:**
```bash
git commit -m "feat(task-049f): complete Phase 5 - InboxProcessor polling and application

Phase 5 Completion:
- InboxProcessor.cs - fetch remote changes and apply locally
- InboxProcessorTests.cs - 5 tests, all passing

Covers ACs: AC-054-065 (12 ACs - inbox processing)"
```

---

## PHASE 6: ConflictResolver Implementation

**Acceptance Criteria Covered:** AC-066-078 (13 ACs - conflict detection and resolution)
**Tests Covered:** ConflictResolverTests.cs (6 tests)
**Production Files:** ConflictResolver.cs
**Estimated Effort:** 3-4 hours
**Blocker Dependencies:** Phase 1 (ConflictPolicy), Phase 2 (IConflictResolver)

### Phase 6A: Test File - ConflictResolverTests.cs [🔄]

**Location:** tests/Acode.Infrastructure.Tests/Sync/ConflictResolverTests.cs
**Expected Test Methods:** 6 tests (from spec)

**Tests to Implement (from spec Testing Requirements):**
```csharp
[Fact] public void Should_Detect_Conflict()                // AC-066
[Fact] public void Should_Apply_LastWriteWins_Policy()     // AC-069
[Fact] public void Should_Apply_LocalWins_Policy()         // AC-070
[Fact] public void Should_Apply_RemoteWins_Policy()        // Alternative
[Fact] public void Should_Detect_DeleteModify_Conflict()   // AC-076
[Fact] public void Should_Apply_ThreeWayMerge()            // AC-076-077
```

**Success Criteria:**
- [ ] ConflictResolverTests.cs created with 6 tests
- [ ] All tests compile
- [ ] Tests fail (RED state)

### Phase 6B: Production - ConflictResolver.cs [🔄]

**Location:** src/Acode.Infrastructure/Sync/ConflictResolver.cs
**Expected Lines:** 300-350 lines
**Spec Reference:** AC-066-078 (13 ACs)

**Implementation Structure:**
```csharp
namespace Acode.Infrastructure.Sync;

public sealed class ConflictResolver : IConflictResolver
{
    public ConflictInfo? Detect(object local, object remote)
    {
        // AC-066: Detect when same entity modified locally and remotely
        // AC-067: Use version vectors to detect conflict
        // Compare version fields
    }
    
    public async Task<object> ResolveAsync(
        ConflictInfo conflict, ConflictPolicy policy, CancellationToken ct)
    {
        return policy switch
        {
            ConflictPolicy.LastWriteWins => ResolveLastWriteWins(conflict),     // AC-069
            ConflictPolicy.FirstWriteWins => ResolveFirstWriteWins(conflict),   // AC-070
            ConflictPolicy.ManualResolution => PauseAndPrompt(conflict),        // AC-071
            ConflictPolicy.CustomMerge => await RunCustomMerge(conflict, ct),   // AC-072
            _ => throw new ArgumentException($"Unknown policy: {policy}")
        };
    }
    
    private object ResolveLastWriteWins(ConflictInfo conflict)
    {
        // AC-069: Select entry with latest timestamp
        return conflict.LocalVersion.UpdatedAt > conflict.RemoteVersion.UpdatedAt
            ? conflict.LocalVersion
            : conflict.RemoteVersion;
    }
}
```

**Key Implementation Points (from spec AC-066-078):**
- Detect conflicts by comparing version numbers (AC-067)
- Support LastWriteWins policy: keep entry with latest UpdatedAt timestamp (AC-069)
- Support FirstWriteWins policy: keep original entry (AC-070)
- Support ManualResolution policy: pause and prompt user (AC-071)
- Support CustomMerge policy: execute registered merge function (AC-072)
- Preserve original conflicting versions in conflict table (AC-073)
- Log conflict resolution for audit (AC-074)
- Update both local database and outbox (AC-075)
- Implement three-way merge for non-conflicting changes (AC-076-077)
- Make conflict list viewable via CLI (AC-078)

**Success Criteria:**
- [ ] ConflictResolver.cs created with Detect() and ResolveAsync() ✅
- [ ] All ConflictResolverTests pass (6/6) ✅
- [ ] All policies implemented (LastWriteWins, FirstWriteWins, ManualResolution, CustomMerge) ✅
- [ ] Three-way merge working ✅
- [ ] Conflict table storing/auditing ✅
- [ ] AC-066-078 now 100% covered ✅
- [ ] Build GREEN ✅

### Phase 6 Summary [🔄]

**Completion Checklist:**
- [ ] ConflictResolverTests.cs created and all passing (6/6) ✅
- [ ] ConflictResolver.cs created with all policies ✅
- [ ] LastWriteWins, FirstWriteWins, ManualResolution, CustomMerge implemented ✅
- [ ] Three-way merge implemented ✅
- [ ] Conflict audit logging working ✅
- [ ] AC-066-078 now 100% covered ✅
- [ ] Build GREEN ✅

**When Phase 6 Complete:**
```bash
git commit -m "feat(task-049f): complete Phase 6 - ConflictResolver with policies

Phase 6 Completion:
- ConflictResolver.cs - conflict detection and resolution
- ConflictResolverTests.cs - 6 tests, all passing
- All conflict policies implemented

Covers ACs: AC-066-078 (13 ACs - conflict detection/resolution)"
```

---

## PHASE 7: PostgreSQL Repository Implementations

**Acceptance Criteria Covered:** AC-133-146 (14 ACs - PostgreSQL repositories)
**Tests Covered:** Postgres*RepositoryTests.cs (3 test files)
**Production Files:** PostgresChatRepository.cs, PostgresRunRepository.cs, PostgresMessageRepository.cs
**Estimated Effort:** 5-6 hours
**Blocker Dependencies:** PostgreSQL infrastructure/connection setup

### Phase 7A-C: Test Files (3 files) [🔄]

**Locations:**
- tests/Acode.Infrastructure.Tests/Repositories/PostgresChatRepositoryTests.cs (5 tests)
- tests/Acode.Infrastructure.Tests/Repositories/PostgresRunRepositoryTests.cs (5 tests)
- tests/Acode.Infrastructure.Tests/Repositories/PostgresMessageRepositoryTests.cs (5 tests)

**Tests to Implement (sample for PostgresChatRepositoryTests):**
```csharp
[Fact] public async Task Should_Create_Chat()              // AC-133 CRUD
[Fact] public async Task Should_Read_Chat()                // AC-133 CRUD
[Fact] public async Task Should_Update_Chat()              // AC-133 CRUD
[Fact] public async Task Should_Soft_Delete_Chat()         // AC-133 CRUD
[Fact] public async Task Should_Support_Transactions()     // AC-136
```

**Success Criteria:**
- [ ] 3 test files created with 5 tests each (15 tests total)
- [ ] All tests compile
- [ ] Tests fail (RED state - repos don't exist yet)

### Phase 7D-F: Production - PostgreSQL Repositories (3 files) [🔄]

**Locations:**
- src/Acode.Infrastructure/Repositories/PostgresChatRepository.cs
- src/Acode.Infrastructure/Repositories/PostgresRunRepository.cs
- src/Acode.Infrastructure/Repositories/PostgresMessageRepository.cs

**Expected Lines:** ~200-250 lines each

**Spec Reference:** AC-133-146 (14 ACs)

**Implementation Requirements (from spec):**

AC-133: CRUD operations (create, read, update, soft delete)
AC-134: Connection pooling with configurable pool size (default 10)
AC-135: Command timeout configured (default 30 seconds)
AC-136: Transactions support commit and rollback
AC-137: Statement caching for prepared statements
AC-138: TLS encryption required (min TLS 1.2)
AC-139: PostgresChatRepository implements IChatRepository
AC-140: PostgresRunRepository implements IRunRepository
AC-141: PostgresMessageRepository implements IMessageRepository
AC-142: Use Dapper for data access (consistent with SQLite)
AC-143: Handle optimistic concurrency with version field
AC-144: Throw ConcurrencyException on version conflicts
AC-145: Connection string supports environment variable override
AC-146: PostgreSQL schema matches SQLite schema (conv_chats, conv_runs, conv_messages)

**Implementation Pattern (PostgresChatRepository example):**
```csharp
namespace Acode.Infrastructure.Repositories;

public sealed class PostgresChatRepository : IChatRepository
{
    private readonly string _connectionString;  // AC-145: env override
    private static readonly DataSource _dataSource;  // AC-134: connection pooling
    
    static PostgresChatRepository()
    {
        // AC-134: Pool size 10
        // AC-135: 30s timeout
        // AC-138: TLS 1.2+
        _dataSource = new NpgsqlDataSourceBuilder(GetConnectionString())
            .DefaultCommandTimeout(TimeSpan.FromSeconds(30))
            .UseLoggerFactory(LoggerFactory.Create(b => b.AddConsole()))
            .Build();
    }
    
    public async Task<ChatId> CreateAsync(Chat chat, CancellationToken ct)
    {
        // AC-133: INSERT
        // AC-136: Transaction support
        // AC-137: Prepared statement
        using var connection = _dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        
        var sql = "INSERT INTO conv_chats (id, title, worktree_id, version, created_at, updated_at) " +
                  "VALUES (@id, @title, @worktree, @version, @createdAt, @updatedAt)";
        
        await connection.ExecuteAsync(sql, new
        {
            id = chat.Id.Value,
            title = chat.Title,
            worktree = chat.WorktreeBinding.Value,
            version = 1,
            createdAt = chat.CreatedAt,
            updatedAt = chat.UpdatedAt
        });
        
        await transaction.CommitAsync(ct);
        return chat.Id;
    }
    
    public async Task<Chat?> GetByIdAsync(ChatId id, CancellationToken ct)
    {
        // AC-133: SELECT
        using var connection = _dataSource.OpenConnection();
        var sql = "SELECT * FROM conv_chats WHERE id = @id AND deleted_at IS NULL";
        
        var row = await connection.QuerySingleOrDefaultAsync(sql, new { id = id.Value });
        return row == null ? null : MapToDomain(row);
    }
    
    public async Task UpdateAsync(Chat chat, CancellationToken ct)
    {
        // AC-143: Optimistic concurrency with version
        using var connection = _dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        
        var sql = @"UPDATE conv_chats SET title = @title, version = version + 1, updated_at = @updatedAt 
                   WHERE id = @id AND version = @currentVersion AND deleted_at IS NULL";
        
        var result = await connection.ExecuteAsync(sql, new
        {
            id = chat.Id.Value,
            title = chat.Title,
            updatedAt = chat.UpdatedAt,
            currentVersion = chat.Version
        });
        
        // AC-144: Throw on version conflict
        if (result == 0)
            throw new ConcurrencyException($"Chat {chat.Id} was modified by another process");
        
        await transaction.CommitAsync(ct);
    }
    
    // Similar pattern for delete (soft-delete with deleted_at)
}
```

**Success Criteria:**
- [ ] PostgresChatRepository.cs created and all tests passing ✅
- [ ] PostgresRunRepository.cs created and all tests passing ✅
- [ ] PostgresMessageRepository.cs created and all tests passing ✅
- [ ] All implement respective IChatRepository, IRunRepository, IMessageRepository interfaces ✅
- [ ] Connection pooling working (AC-134) ✅
- [ ] Timeout configured (AC-135) ✅
- [ ] TLS encryption required (AC-138) ✅
- [ ] Dapper used for queries (AC-142) ✅
- [ ] Optimistic concurrency with version (AC-143) ✅
- [ ] ConcurrencyException thrown (AC-144) ✅
- [ ] AC-133-146 now 100% covered ✅
- [ ] Build GREEN ✅

### Phase 7 Summary [🔄]

**Completion Checklist:**
- [ ] 3 test files created with 5 tests each (15/15 tests) ✅
- [ ] PostgresChatRepository.cs created and all tests passing ✅
- [ ] PostgresRunRepository.cs created and all tests passing ✅
- [ ] PostgresMessageRepository.cs created and all tests passing ✅
- [ ] All implement correct repository interfaces ✅
- [ ] Connection pooling, timeouts, TLS configured ✅
- [ ] Dapper statements cached ✅
- [ ] Transaction support working ✅
- [ ] Optimistic concurrency working ✅
- [ ] AC-133-146 now 100% covered ✅
- [ ] Build GREEN ✅

**When Phase 7 Complete:**
```bash
git commit -m "feat(task-049f): complete Phase 7 - PostgreSQL repositories

Phase 7 Completion:
- PostgresChatRepository.cs - CRUD, pooling, TLS, transactions
- PostgresRunRepository.cs - CRUD, pooling, TLS, transactions
- PostgresMessageRepository.cs - CRUD, pooling, TLS, transactions
- 3 test files with 15 tests, all passing

Covers ACs: AC-133-146 (14 ACs - PostgreSQL repositories)"
```

---

## PHASE 8: SyncEngine Integration & Completion

**Acceptance Criteria Covered:** AC-017-030 (integration), AC-046-053 (idempotency), performance tests
**Tests Covered:** Enhanced SyncEngineTests.cs, IdempotencyTests.cs, SyncE2ETests.cs, SyncBenchmarks.cs
**Production Files:** SyncEngine.cs (complete remaining), add health check/metrics
**Estimated Effort:** 3-4 hours
**Blocker Dependencies:** Phases 1-7 (all components)

### Phase 8A: Complete SyncEngine.cs [🔄]

**Location:** src/Acode.Infrastructure/Sync/SyncEngine.cs (currently 218 lines, 55% complete)

**Current Issues:**
- Line 187: TODO about structured logging
- Line 203: TODO about domain models
- ProcessPendingEntriesAsync() is stub (lines 193-216)

**Completion Requirements:**
```csharp
private async Task ProcessPendingEntriesAsync(CancellationToken cancellationToken)
{
    // AC-017: Run on configurable polling interval
    // Step 1: Get pending entries
    var pendingEntries = await _outboxRepository.GetPendingAsync(limit: 50, cancellationToken);
    
    if (pendingEntries.Count == 0)
        return;
    
    // Step 2: Batch using OutboxBatcher
    var batches = _batcher.CreateBatches(pendingEntries);
    
    // Step 3: Process each batch
    foreach (var batch in batches)
    {
        try
        {
            // AC-020: Send single HTTP request for batch
            await _outboxProcessor.ProcessBatchAsync(batch, cancellationToken);
            
            // AC-021-022: Mark as completed with timestamp
            foreach (var entry in batch)
            {
                entry.MarkAsCompleted();
                await _outboxRepository.UpdateAsync(entry, cancellationToken);
                _totalProcessed++;
            }
        }
        catch (HttpRequestException ex) when (IsTransientError(ex))
        {
            // AC-031-033: Retry on transient errors
            await _retryPolicy.ExecuteAsync(
                async () => await _outboxProcessor.ProcessBatchAsync(batch, cancellationToken),
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // AC-036-037: Move to dead letter on permanent errors
            foreach (var entry in batch)
            {
                entry.MarkAsDeadLetter(ex.Message);
                await _outboxRepository.UpdateAsync(entry, cancellationToken);
            }
            _totalFailed++;
        }
    }
    
    // Step 4: Update last sync timestamp
    _lastSyncAt = DateTimeOffset.UtcNow;
    
    // AC-106: Emit metric for queue depth
    _metrics.RecordQueueDepth(pendingEntries.Count);
}
```

**Success Criteria:**
- [ ] ProcessPendingEntriesAsync() fully implemented (no more TODOs) ✅
- [ ] Integrates with OutboxProcessor ✅
- [ ] Integrates with OutboxBatcher ✅
- [ ] Integrates with RetryPolicy ✅
- [ ] All error paths tested ✅
- [ ] Metrics collection integrated ✅
- [ ] No NotImplementedException ✅
- [ ] No TODO/FIXME markers ✅

### Phase 8B: Test File - IdempotencyTests.cs [🔄]

**Location:** tests/Acode.Infrastructure.Tests/Sync/IdempotencyTests.cs
**Expected Test Methods:** 3 tests

**Tests to Implement (from spec):**
```csharp
[Fact] public async Task Should_Deduplicate_Same_Request()      // AC-046-047
[Fact] public async Task Should_Cache_Idempotency_Keys()        // AC-051-052
[Fact] public async Task Should_Prevent_Concurrent_Duplicate()  // AC-053
```

**Success Criteria:**
- [ ] IdempotencyTests.cs created with 3 tests
- [ ] All tests pass ✅

### Phase 8C: Test File - SyncE2ETests.cs [🔄]

**Location:** tests/Acode.Infrastructure.Tests/Sync/SyncE2ETests.cs
**Expected Test Methods:** 3 tests (from spec Testing Requirements)

**Tests to Implement:**
```csharp
[Fact] public async Task Should_Sync_Full_Workflow()        // Create, sync, verify
[Fact] public async Task Should_Handle_Offline_To_Online()  // Batch offline changes
[Fact] public async Task Should_Resolve_Conflicts()         // End-to-end conflict resolution
```

**Success Criteria:**
- [ ] SyncE2ETests.cs created with 3 tests
- [ ] All tests pass ✅

### Phase 8D: Performance Benchmarks [🔄]

**Location:** tests/Acode.Infrastructure.Tests/Performance/SyncBenchmarks.cs
**Expected Benchmarks:** 3 BenchmarkDotNet methods

**Benchmarks to Implement (from spec):**
```csharp
[Benchmark] public async Task Batch_50_Items()      // Target: 2s, Max: 5s (AC-120)
[Benchmark] public async Task Outbox_Write()        // Target: 5ms, Max: 10ms (AC-119)
[Benchmark] public void Conflict_Detect()           // Target: 1ms, Max: 5ms
```

**Success Criteria:**
- [ ] SyncBenchmarks.cs created with all 3 benchmarks ✅
- [ ] All benchmarks meet performance targets (AC-119-124) ✅

### Phase 8 Summary [🔄]

**Completion Checklist:**
- [ ] SyncEngine.cs completed (no TODOs, fully implemented) ✅
- [ ] IdempotencyTests.cs created and passing (3/3) ✅
- [ ] SyncE2ETests.cs created and passing (3/3) ✅
- [ ] SyncBenchmarks.cs created with target performance ✅
- [ ] All integration tests passing ✅
- [ ] Metrics collection working ✅
- [ ] AC-017-030, AC-046-053, AC-119-124 covered ✅
- [ ] Build GREEN ✅

**When Phase 8 Complete:**
```bash
git commit -m "feat(task-049f): complete Phase 8 - SyncEngine integration & E2E testing

Phase 8 Completion:
- SyncEngine.cs - completed (removed all TODOs)
- IdempotencyTests.cs - 3 tests, all passing
- SyncE2ETests.cs - 3 tests, all passing
- SyncBenchmarks.cs - 3 benchmarks, target performance met

Covers ACs: AC-017-030 (integration), AC-046-053 (idempotency), AC-119-124 (performance)"
```

---

## PHASE 9: CLI Commands Implementation

**Acceptance Criteria Covered:** AC-079-100 (22 ACs - CLI commands)
**Tests Covered:** CLI command tests (10+ test files)
**Production Files:** SyncCommand.cs (router), + 9 command classes
**Estimated Effort:** 6-8 hours
**Blocker Dependencies:** Phases 1-8

### Phase 9A-J: CLI Commands (10+ classes) [🔄]

**Base Router:**
- src/Acode.CLI/Commands/SyncCommand.cs (dispatcher for sync subcommands)

**Subcommands:**
1. **SyncStatusCommand.cs** (AC-079-083)
   - `acode sync status` - Show sync state, queue depths, last sync time
   - Output: Outbox/Inbox pending counts, sync lag, is running/paused

2. **SyncNowCommand.cs** (AC-084-085)
   - `acode sync now` - Trigger immediate sync, wait for completion

3. **SyncRetryCommand.cs** (AC-086-087)
   - `acode sync retry <id>` - Retry specific failed entry
   - `acode sync retry --all` - Retry all failed entries

4. **SyncPauseResumeCommand.cs** (AC-088-089)
   - `acode sync pause` - Pause background processor
   - `acode sync resume` - Resume background processor

5. **SyncFullCommand.cs** (AC-090-092)
   - `acode sync full` - Full resync of all data
   - `acode sync full --direction push` - Only local to remote
   - `acode sync full --direction pull` - Only remote to local

6. **SyncConflictCommand.cs** (AC-093-094)
   - `acode sync conflicts list` - Show pending conflicts
   - `acode sync resolve <id> --strategy <strategy>` - Resolve specific conflict

7. **SyncHealthCommand.cs** (AC-095)
   - `acode sync health` - Show sync subsystem health metrics

8. **SyncLogsCommand.cs** (AC-096)
   - `acode sync logs` - Show recent sync activity

9. **SyncDlqCommand.cs** (AC-097-100)
   - `acode sync dlq list` - Show dead letter queue entries
   - `acode sync dlq retry <id>` - Retry dead letter entry
   - `acode sync dlq archive` / `acode sync dlq delete` - Manage DLQ

### Implementation Pattern (SyncStatusCommand example)

```csharp
namespace Acode.CLI.Commands;

[Command("sync status")]
public sealed class SyncStatusCommand : Command
{
    private readonly ISyncEngine _syncEngine;
    
    [Option("-v|--verbose", Description = "Show per-entity breakdown")]
    public bool Verbose { get; set; }
    
    public async Task<int> OnExecuteAsync()
    {
        var status = await _syncEngine.GetStatusAsync(CancellationToken.None);
        
        Console.WriteLine($"Sync Status:");
        Console.WriteLine($"  Running: {status.IsRunning}");
        Console.WriteLine($"  Paused: {status.IsPaused}");
        Console.WriteLine($"  Pending Outbox Entries: {status.PendingOutboxCount}");
        Console.WriteLine($"  Last Sync: {status.LastSyncAt:O}");
        Console.WriteLine($"  Sync Lag: {status.SyncLag}");
        
        if (Verbose)
        {
            Console.WriteLine($"  Total Processed: {status.TotalProcessed}");
            Console.WriteLine($"  Total Failed: {status.TotalFailed}");
        }
        
        return 0;
    }
}
```

**Success Criteria:**
- [ ] 10+ CLI command classes created ✅
- [ ] SyncCommand router implemented ✅
- [ ] All commands compile and work ✅
- [ ] Help text for all commands ✅
- [ ] Option parsing working (--verbose, --direction, etc.) ✅
- [ ] AC-079-100 (22 ACs) all covered ✅
- [ ] Integration tests for commands ✅
- [ ] Build GREEN ✅

### Phase 9 Summary [🔄]

**Completion Checklist:**
- [ ] SyncCommand.cs (router) created ✅
- [ ] SyncStatusCommand.cs created (AC-079-083) ✅
- [ ] SyncNowCommand.cs created (AC-084-085) ✅
- [ ] SyncRetryCommand.cs created (AC-086-087) ✅
- [ ] SyncPauseResumeCommand.cs created (AC-088-089) ✅
- [ ] SyncFullCommand.cs created (AC-090-092) ✅
- [ ] SyncConflictCommand.cs created (AC-093-094) ✅
- [ ] SyncHealthCommand.cs created (AC-095) ✅
- [ ] SyncLogsCommand.cs created (AC-096) ✅
- [ ] SyncDlqCommand.cs created (AC-097-100) ✅
- [ ] All commands have help/documentation ✅
- [ ] All commands tested and working ✅
- [ ] AC-079-100 fully covered ✅
- [ ] Build GREEN ✅

**When Phase 9 Complete:**
```bash
git commit -m "feat(task-049f): complete Phase 9 - CLI commands for sync management

Phase 9 Completion:
- SyncCommand.cs - router for sync subcommands
- SyncStatusCommand.cs - display sync status (AC-079-083)
- SyncNowCommand.cs - trigger immediate sync (AC-084-085)
- SyncRetryCommand.cs - retry failed entries (AC-086-087)
- SyncPauseResumeCommand.cs - pause/resume processor (AC-088-089)
- SyncFullCommand.cs - full resync (AC-090-092)
- SyncConflictCommand.cs - manage conflicts (AC-093-094)
- SyncHealthCommand.cs - health metrics (AC-095)
- SyncLogsCommand.cs - recent activity (AC-096)
- SyncDlqCommand.cs - dead letter queue (AC-097-100)

Covers ACs: AC-079-100 (22 ACs - CLI commands)"
```

---

## PHASE 10: Health Monitoring, Metrics & Configuration

**Acceptance Criteria Covered:** AC-101-132 (19 ACs - health, monitoring, configuration)
**Tests Covered:** Health/metrics/config tests
**Production Files:** HealthCheckService.cs, MetricsCollector.cs, SyncConfiguration.cs
**Estimated Effort:** 4-5 hours
**Blocker Dependencies:** Phases 1-9

### Phase 10A: Health Monitoring (AC-101-111) [🔄]

**Production Files:**
1. **HealthCheckService.cs** - Health check endpoint
2. **MetricsCollector.cs** - Metrics collection
3. **CircuitBreakerMonitor.cs** - CB state exposure

**Implementation Requirements:**
- AC-101: Queue depth metric exposed for outbox entries
- AC-102: Queue depth metric exposed for inbox entries
- AC-103: Sync lag metric (oldest pending entry age)
- AC-104: Throughput metric (entries/second)
- AC-105: Error rate metric (failed/total ratio)
- AC-106: Circuit breaker state exposed as metric
- AC-107-108: Health check endpoint returns status in <100ms
- AC-109: Structured logs with correlation IDs
- AC-110: Prometheus format export
- AC-111: Alert triggers when sync lag exceeds 5 minutes

**Implementation Pattern:**

```csharp
namespace Acode.Infrastructure.Sync;

public sealed class SyncHealthCheck : IHealthCheck
{
    private readonly ISyncEngine _syncEngine;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct)
    {
        var status = await _syncEngine.GetStatusAsync(ct);
        
        // AC-108: Complete within 100ms
        var data = new Dictionary<string, object>
        {
            // AC-101-102: Queue depths
            { "OutboxPendingCount", status.PendingOutboxCount },
            { "InboxPendingCount", status.PendingInboxCount },
            
            // AC-103: Sync lag
            { "SyncLagSeconds", status.SyncLag?.TotalSeconds },
            
            // AC-105: Error rate
            { "ErrorRate", status.ErrorRate },
            
            // AC-106: Circuit breaker state
            { "CircuitBreakerState", status.CircuitBreakerState }
        };
        
        // AC-111: Alert if lag > 5 minutes
        if (status.SyncLag?.TotalMinutes > 5)
        {
            return HealthCheckResult.Unhealthy("Sync lag exceeds 5 minutes", data);
        }
        
        return HealthCheckResult.Healthy("Sync is healthy", data);
    }
}

public sealed class MetricsCollector
{
    // AC-104: Throughput tracking
    public void RecordProcessed(int count) => _entriesProcessed.Add(count);
    
    // AC-105: Error tracking
    public void RecordError() => _entriesFailed.Inc();
    
    // Prometheus export (AC-110)
    public string ExportPrometheus()
    {
        return $@"
# HELP acode_sync_outbox_pending Number of pending outbox entries
# TYPE acode_sync_outbox_pending gauge
acode_sync_outbox_pending {_outboxCount}

# HELP acode_sync_lag_seconds Sync lag in seconds
# TYPE acode_sync_lag_seconds gauge
acode_sync_lag_seconds {_lagSeconds}

# HELP acode_sync_throughput_eps Entries processed per second
# TYPE acode_sync_throughput_eps gauge
acode_sync_throughput_eps {_throughputEps}
";
    }
}
```

**Success Criteria:**
- [ ] HealthCheckService.cs created with health check endpoint ✅
- [ ] MetricsCollector.cs created with all metrics (AC-101-106) ✅
- [ ] Prometheus export working (AC-110) ✅
- [ ] Alert logic for lag > 5 minutes (AC-111) ✅
- [ ] Structured logs with correlation IDs (AC-109) ✅
- [ ] Health check <100ms (AC-108) ✅
- [ ] AC-101-111 fully covered ✅

### Phase 10B: Configuration System (AC-125-132) [🔄]

**Production Files:**
1. **SyncConfiguration.cs** - Configuration record
2. **SyncConfigurationValidator.cs** - Validation

**Implementation Requirements:**
- AC-125: Sync enabled/disabled via configuration
- AC-126: Polling interval configurable (default 5 seconds)
- AC-127: Batch size configurable (default 100)
- AC-128: Max retry count configurable (default 10)
- AC-129: Conflict resolution policy configurable
- AC-130: Remote endpoint URL configurable
- AC-131: Authentication method configurable
- AC-132: Idempotency key TTL configurable

**Implementation:**

```csharp
namespace Acode.Infrastructure.Sync;

public sealed record SyncConfiguration
{
    // AC-125: Enabled/Disabled
    public bool Enabled { get; init; } = true;
    
    // AC-126: Polling interval (default 5s)
    public int PollingIntervalMs { get; init; } = 5000;
    
    // AC-127: Batch size (default 100)
    public int BatchSize { get; init; } = 100;
    
    // AC-128: Max retries (default 10)
    public int MaxRetries { get; init; } = 10;
    
    // AC-129: Conflict policy
    public ConflictPolicy ConflictPolicy { get; init; } = ConflictPolicy.LastWriteWins;
    
    // AC-130: Remote endpoint
    public string RemoteEndpoint { get; init; } = string.Empty;
    
    // AC-131: Auth method
    public string AuthMethod { get; init; } = "Bearer";
    
    // AC-132: Idempotency TTL
    public int IdempotencyTtlDays { get; init; } = 7;
    
    public static SyncConfiguration LoadFromEnvironment()
    {
        // AC-130, AC-131, AC-132: Load from environment
        return new SyncConfiguration
        {
            Enabled = bool.Parse(Environment.GetEnvironmentVariable("ACODE_SYNC_ENABLED") ?? "true"),
            PollingIntervalMs = int.Parse(Environment.GetEnvironmentVariable("ACODE_SYNC_POLLING_MS") ?? "5000"),
            RemoteEndpoint = Environment.GetEnvironmentVariable("ACODE_SYNC_ENDPOINT") ?? throw new InvalidOperationException("ACODE_SYNC_ENDPOINT required")
        };
    }
}
```

**Success Criteria:**
- [ ] SyncConfiguration.cs created with all properties ✅
- [ ] SyncConfigurationValidator.cs validates all settings ✅
- [ ] Environment variable loading (AC-130-132) ✅
- [ ] Default values correct per spec ✅
- [ ] AC-125-132 fully covered ✅

### Phase 10 Summary [🔄]

**Completion Checklist:**
- [ ] HealthCheckService.cs created ✅
- [ ] MetricsCollector.cs created (AC-101-106) ✅
- [ ] Prometheus export working (AC-110) ✅
- [ ] Alert triggers for lag > 5 min (AC-111) ✅
- [ ] SyncConfiguration.cs created (AC-125-132) ✅
- [ ] SyncConfigurationValidator.cs created ✅
- [ ] Structured logs with correlation IDs (AC-109) ✅
- [ ] Health check endpoint <100ms (AC-108) ✅
- [ ] Configuration loading from environment ✅
- [ ] All tests passing ✅
- [ ] AC-101-132 fully covered (19 ACs) ✅
- [ ] Build GREEN ✅

**When Phase 10 Complete:**
```bash
git commit -m "feat(task-049f): complete Phase 10 - health monitoring & configuration

Phase 10 Completion:
- HealthCheckService.cs - health check endpoint
- MetricsCollector.cs - metrics collection (queue, lag, throughput, error rate)
- SyncConfiguration.cs - configuration record with env loading
- SyncConfigurationValidator.cs - configuration validation
- Prometheus export for metrics
- Alert system for sync lag > 5 minutes
- Structured logging with correlation IDs

Covers ACs: AC-101-132 (19 ACs - health, monitoring, configuration)"
```

---

## COMPLETION VERIFICATION CHECKLIST

When ALL 10 phases complete, verify 100% implementation:

### Production Files (31 total)
- [ ] Domain Layer (4):
  - [x] OutboxEntry.cs ✅
  - [ ] InboxEntry.cs ✅
  - [x] SyncStatus.cs ✅
  - [ ] ConflictPolicy.cs ✅

- [ ] Application Layer (5):
  - [x] ISyncEngine.cs ✅
  - [x] IOutboxRepository.cs ✅
  - [ ] IOutboxProcessor.cs ✅
  - [ ] IInboxProcessor.cs ✅
  - [ ] IConflictResolver.cs ✅

- [ ] Infrastructure Services (7):
  - [ ] OutboxBatcher.cs ✅
  - [ ] RetryPolicy.cs ✅ (with jitter + CB)
  - [ ] CircuitBreaker.cs ✅
  - [ ] SqliteOutboxRepository.cs ✅
  - [ ] OutboxProcessor.cs ✅
  - [ ] InboxProcessor.cs ✅
  - [ ] ConflictResolver.cs ✅
  - [x] SyncEngine.cs ✅ (completed)

- [ ] PostgreSQL Repositories (3):
  - [ ] PostgresChatRepository.cs ✅
  - [ ] PostgresRunRepository.cs ✅
  - [ ] PostgresMessageRepository.cs ✅

- [ ] CLI Commands (10):
  - [ ] SyncCommand.cs (router) ✅
  - [ ] SyncStatusCommand.cs ✅
  - [ ] SyncNowCommand.cs ✅
  - [ ] SyncRetryCommand.cs ✅
  - [ ] SyncPauseResumeCommand.cs ✅
  - [ ] SyncFullCommand.cs ✅
  - [ ] SyncConflictCommand.cs ✅
  - [ ] SyncHealthCommand.cs ✅
  - [ ] SyncLogsCommand.cs ✅
  - [ ] SyncDlqCommand.cs ✅

- [ ] Health & Config (3):
  - [ ] HealthCheckService.cs ✅
  - [ ] MetricsCollector.cs ✅
  - [ ] SyncConfiguration.cs ✅
  - [ ] SyncConfigurationValidator.cs ✅

### Test Files (15+ total)
- [ ] Domain Tests (3):
  - [x] OutboxEntryTests.cs ✅
  - [ ] InboxEntryTests.cs ✅
  - [ ] ConflictPolicyTests.cs ✅

- [ ] Infrastructure Tests (7):
  - [ ] OutboxBatcherTests.cs ✅
  - [ ] RetryPolicyTests.cs ✅
  - [ ] CircuitBreakerTests.cs ✅
  - [ ] SqliteOutboxRepositoryTests.cs ✅
  - [ ] OutboxProcessorTests.cs ✅
  - [ ] InboxProcessorTests.cs ✅
  - [ ] ConflictResolverTests.cs ✅

- [ ] Integration Tests (5):
  - [x] SyncEngineTests.cs ✅
  - [ ] IdempotencyTests.cs ✅
  - [ ] SyncE2ETests.cs ✅
  - [ ] PostgresChatRepositoryTests.cs ✅
  - [ ] PostgresRunRepositoryTests.cs ✅
  - [ ] PostgresMessageRepositoryTests.cs ✅

- [ ] Performance Tests (1):
  - [ ] SyncBenchmarks.cs ✅

### Acceptance Criteria (146 total)
- [ ] AC-001-016: Outbox Entry Creation (16 ACs) ✅
- [ ] AC-017-030: Outbox Processing (14 ACs) ✅
- [ ] AC-031-045: Retry Mechanism (15 ACs) ✅
- [ ] AC-046-053: Idempotency Enforcement (8 ACs) ✅
- [ ] AC-054-065: Inbox Processing (12 ACs) ✅
- [ ] AC-066-078: Conflict Detection/Resolution (13 ACs) ✅
- [ ] AC-079-100: CLI Commands (22 ACs) ✅
- [ ] AC-101-111: Health and Monitoring (11 ACs) ✅
- [ ] AC-112-118: Data Integrity (7 ACs) - covered by above
- [ ] AC-119-124: Performance (6 ACs) ✅
- [ ] AC-125-132: Configuration (8 ACs) ✅
- [ ] AC-133-146: PostgreSQL Repositories (14 ACs) ✅

### Build & Test Verification
- [ ] `dotnet build` → 0 errors, 0 warnings ✅
- [ ] `dotnet test` → ALL tests passing (40+ tests total) ✅
- [ ] Grep for NotImplementedException → ZERO matches ✅
- [ ] Grep for TODO/FIXME → ZERO matches (or only acceptable TODOs) ✅
- [ ] Grep for "throw new NotImplementedException" → ZERO matches ✅

### Final Steps (when 100% verified complete)
1. [ ] Update task-049f-completion-checklist.md → 100% COMPLETE
2. [ ] Commit final changes with comprehensive message
3. [ ] Create PR with detailed description of all 146 ACs
4. [ ] Pass final audit (AUDIT-GUIDELINES.md)
5. [ ] Merge to main

---

**Task Status:** 🔄 Ready for Phase 1 Implementation (0% → 100%)

**Total Estimated Effort:** 32-40 hours
**Effort per Phase:** 2-8 hours
**Parallel Work:** Phases can be worked on sequentially only (each depends on previous)

**Next Action:** Proceed to Phase 1 - Domain Entities (InboxEntry & ConflictPolicy)

