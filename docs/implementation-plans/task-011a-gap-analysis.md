# Task-011a Gap Analysis: Run Entities (Session, Task, Step, Tool Call, Artifacts)

**Status:** ❌ 0% COMPLETE - NOT STARTED

**Date:** 2026-01-15
**Analyzed By:** Claude Code
**Methodology:** Comprehensive semantic evaluation per CLAUDE.md Section 3.2

---

## Executive Summary

**CRITICAL FINDING:** Task-011a (Run Entities domain model) has **zero implementation** - all 94 Acceptance Criteria are completely unstarted. This is a foundational domain modeling task that requires building the core entity hierarchy: Session → Task → Step → ToolCall → Artifact.

**Key Metrics:**
- **Total Acceptance Criteria:** 94 ACs
- **ACs Complete:** 0 (0%)
- **ACs Partial:** 0 (0%)
- **ACs Missing:** 94 (100%)
- **Overall Semantic Completeness:** 0.0%
- **Production Readiness:** ❌ NOT READY (requires ≥ 95%)
- **Blocking Dependencies:** ✅ NONE - Can proceed immediately
- **Estimated Effort:** 42.75 hours (22.75 hours production code + 20 hours testing)
- **Estimated LOC:** 2,680+ total (880 production + 1,800 tests)

---

## Current Codebase State

### What EXISTS (NOT Applicable - Different Domains)

❌ **Existing but NOT reusable:**
- `src/Acode.Domain/Audit/SessionId.cs` - Different domain (Audit sessions, not Run sessions)
- `src/Acode.Domain/Conversation/ToolCall.cs` - Different domain (LLM tool calls, not Run execution tool calls)

**Why NOT reusable:**
- These are in different bounded contexts with different semantics
- Audit sessions track modification history, not execution flow
- Conversation ToolCalls are LLM requests, not execution artifacts
- Different properties, different lifetime, different validation rules

### What DOES NOT EXIST (Must be Created)

❌ **MISSING - Entire namespace structure:**

**Domain Model Structure:**
```
src/Acode.Domain/
├── Common/
│   ├── EntityBase.cs                    (MISSING)
│   ├── EntityId.cs                      (MISSING)
│   └── SecureIdGenerator.cs             (MISSING)
├── Sessions/
│   ├── Session.cs                       (MISSING)
│   ├── SessionId.cs                     (MISSING)
│   ├── SessionState.cs                  (MISSING)
│   ├── SessionEvent.cs                  (MISSING)
│   └── SessionNotFoundException.cs       (MISSING)
├── Tasks/
│   ├── SessionTask.cs                   (MISSING)
│   ├── TaskId.cs                        (MISSING)
│   └── TaskState.cs                     (MISSING)
├── Steps/
│   ├── Step.cs                          (MISSING)
│   ├── StepId.cs                        (MISSING)
│   └── StepState.cs                     (MISSING)
├── ToolCalls/
│   ├── ToolCall.cs                      (Run execution, MISSING)
│   ├── ToolCallId.cs                    (MISSING)
│   └── ToolCallState.cs                 (MISSING)
└── Artifacts/
    ├── Artifact.cs                      (MISSING)
    ├── ArtifactId.cs                    (MISSING)
    └── ArtifactType.cs                  (MISSING)
```

❌ **MISSING - Test directory structure:**
```
tests/Acode.Domain.Tests/
├── Sessions/SessionTests.cs             (MISSING - 20+ tests)
├── Tasks/SessionTaskTests.cs            (MISSING - 15+ tests)
├── Steps/StepTests.cs                   (MISSING - 12+ tests)
├── ToolCalls/ToolCallTests.cs           (MISSING - 12+ tests)
├── Artifacts/ArtifactTests.cs           (MISSING - 12+ tests)
├── Common/EntityIdTests.cs              (MISSING - 12+ tests)
├── Sessions/StateTransitionTests.cs     (MISSING - 8+ tests)
└── Serialization/RoundTripTests.cs      (MISSING - 10+ tests)
```

---

## Acceptance Criteria Compliance Summary

### By Category

| Category | Total ACs | Complete | Partial | Missing | % |
|----------|-----------|----------|---------|---------|---|
| Session Entity | 8 | 0 | 0 | 8 | 0% |
| Task Entity | 8 | 0 | 0 | 8 | 0% |
| Step Entity | 7 | 0 | 0 | 7 | 0% |
| ToolCall Entity | 9 | 0 | 0 | 9 | 0% |
| Artifact Entity | 9 | 0 | 0 | 9 | 0% |
| States (Enums) | 5 | 0 | 0 | 5 | 0% |
| Artifact Types | 6 | 0 | 0 | 6 | 0% |
| Validation | 5 | 0 | 0 | 5 | 0% |
| Identity & ID Generation | 4 | 0 | 0 | 4 | 0% |
| Entity Hierarchy/Relationships | 5 | 0 | 0 | 5 | 0% |
| Serialization | 4 | 0 | 0 | 4 | 0% |
| State Transitions | 8 | 0 | 0 | 8 | 0% |
| Collections & Encapsulation | 6 | 0 | 0 | 6 | 0% |
| Equality & Hashing | 4 | 0 | 0 | 4 | 0% |
| Events | 6 | 0 | 0 | 6 | 0% |
| **TOTAL** | **94** | **0** | **0** | **94** | **0%** |

### AC Specification List

**AC-001 to AC-008: Session Entity**
- AC-001: UUID v7 ID generated ❌ MISSING
- AC-002: TaskDescription stored ❌ MISSING
- AC-003: State tracked ❌ MISSING
- AC-004: CreatedAt recorded ❌ MISSING
- AC-005: UpdatedAt maintained ❌ MISSING
- AC-006: Tasks collection works ❌ MISSING
- AC-007: Events collection works ❌ MISSING
- AC-008: Metadata optional ❌ MISSING

**AC-009 to AC-016: Task Entity**
- AC-009 through AC-016: All task-specific properties ❌ ALL MISSING

**AC-017 to AC-023: Step Entity**
- AC-017 through AC-023: All step-specific properties ❌ ALL MISSING

**AC-024 to AC-032: ToolCall Entity**
- AC-024 through AC-032: All tool call properties ❌ ALL MISSING

**AC-033 to AC-041: Artifact Entity**
- AC-033 through AC-041: All artifact properties + security validation ❌ ALL MISSING

**AC-042 to AC-046: State Enums**
- AC-042: SessionState enum ❌ MISSING
- AC-043: TaskState enum ❌ MISSING
- AC-044: StepState enum ❌ MISSING
- AC-045: ToolCallState enum ❌ MISSING
- AC-046: State derivation logic ❌ MISSING

**AC-047 to AC-052: Artifact Types**
- AC-047 through AC-052: ArtifactType enum values ❌ ALL MISSING

**AC-053 to AC-057: Validation**
- AC-053: Invalid IDs rejected ❌ MISSING
- AC-054: Empty strings rejected ❌ MISSING
- AC-055: Negative order rejected ❌ MISSING
- AC-056: Invalid JSON rejected ❌ MISSING
- AC-057: Hash mismatch detected ❌ MISSING

**AC-058 to AC-061: Identity**
- AC-058: UUID v7 format used ❌ MISSING
- AC-059: IDs generated on creation ❌ MISSING
- AC-060: IDs immutable ❌ MISSING
- AC-061: IDs database-safe ❌ MISSING

**AC-062 to AC-066: Hierarchy**
- AC-062: Session is aggregate root ❌ MISSING
- AC-063: Tasks belong to Session ❌ MISSING
- AC-064: Steps belong to Task ❌ MISSING
- AC-065: ToolCalls belong to Step ❌ MISSING
- AC-066: Artifacts belong to ToolCall ❌ MISSING

**AC-067 to AC-070: Serialization**
- AC-067: JSON serialization works ❌ MISSING
- AC-068: All fields serialized ❌ MISSING
- AC-069: Deserialization works ❌ MISSING
- AC-070: Round-trip preserves data ❌ MISSING

**AC-071 to AC-078: State Transitions**
- AC-071 through AC-078: All 8 state transition scenarios ❌ ALL MISSING

**AC-079 to AC-084: Collections**
- AC-079 through AC-084: Collection management ❌ ALL MISSING

**AC-085 to AC-088: Equality**
- AC-085 through AC-088: Equality and hashing ❌ ALL MISSING

**AC-089 to AC-094: Events**
- AC-089 through AC-094: State transition events ❌ ALL MISSING

---

## Identified Gaps (20 Major Implementation Tasks)

### PHASE 1: Foundation Infrastructure

#### Gap #1: EntityId Abstract Base Class

**Requirement:** FR-072 to FR-074
**Spec Reference:** Implementation Prompt section, lines 3672-3705
**File:** `src/Acode.Domain/Common/EntityId.cs`
**Status:** ❌ MISSING (0 lines, should be 35 lines)

**What's Missing:**
- Abstract base class for all entity IDs
- UUID v7 generation with validation
- Equality comparison (IEquatable<T>)
- GetHashCode implementation
- Implicit conversion to/from Guid
- Validation: reject empty/default GUIDs

**Code Template from Spec:**
```csharp
public abstract class EntityId : IEquatable<EntityId>
{
    public Guid Value { get; }

    protected EntityId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("ID cannot be empty");
        Value = value;
    }

    public override bool Equals(object? obj) => obj is EntityId other && Value == other.Value;
    public bool Equals(EntityId? other) => other is not null && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
}
```

**Acceptance Criteria:** AC-058, AC-060, AC-061
**Test Count:** 8 tests (construction, empty validation, equality, GetHashCode, immutability)
**Effort:** 30 minutes, 35 LOC

---

#### Gap #2: SecureIdGenerator Static Class

**Requirement:** FR-066, FR-072 (UUID v7 generation), Security Threat 2 (UUID Collision Prevention)
**Spec Reference:** Security Considerations section, lines 665-784; Implementation Prompt
**File:** `src/Acode.Domain/Common/SecureIdGenerator.cs`
**Status:** ❌ MISSING (0 lines, should be 120 lines)

**What's Missing:**
- UUID v7 (timestamp-based) generation
- Collision detection and retry logic
- Monotonic sequence support
- Clock regression handling
- Timestamp extraction from generated IDs
- Thread-safe implementation

**Security Threat Address:** Security Threat 2 (UUID Collisions)
- Multiple tool invocations might generate same UUID
- Solution: UUID v7 with monotonic sequence + collision detection
- Timeout: Fail after 3 collision retries (guarantees forward progress)

**Complexity:** This is the most complex base class component
- Implements RFC 4122 UUID v7 format
- Handles clock regression scenarios
- Manages monotonic sequence overflow

**Code Pattern from Spec:**
```csharp
public static class SecureIdGenerator
{
    private static readonly object _lock = new();
    private static DateTime _lastTimestamp = DateTime.UtcNow;
    private static ushort _sequence = 0;

    public static Guid GenerateId()
    {
        lock (_lock)
        {
            // Implement UUID v7 logic
            // Handle clock regression
            // Manage sequence
        }
    }
}
```

**Acceptance Criteria:** AC-058, AC-059, AC-074 (performance)
**Test Count:** 12 tests (v7format, collision detection, monotonicity, clock regression, performance, thread safety)
**Effort:** 1.5 hours, 120 LOC

---

#### Gap #3: EntityBase<TId> Abstract Class

**Requirement:** FR-001-050 (all entities inherit)
**File:** `src/Acode.Domain/Common/EntityBase.cs`
**Status:** ❌ MISSING (0 lines, should be 20 lines)

**What's Missing:**
- Generic base class for all entities
- ID property (generic TId extends EntityId)
- CreatedAt timestamp (auto-set)
- UpdatedAt timestamp (auto-updated)
- MarkUpdated method (for persistence)

**Code Pattern:**
```csharp
public abstract class EntityBase<TId> where TId : EntityId
{
    public TId Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset UpdatedAt { get; protected set; }

    protected void MarkUpdated() => UpdatedAt = DateTimeOffset.UtcNow;
}
```

**Acceptance Criteria:** AC-004, AC-005, AC-015, AC-022, AC-028, AC-050
**Test Count:** 5 tests (automatic via inheritance by subclasses)
**Effort:** 20 minutes, 20 LOC

---

### PHASE 2: State Enums (5 Required)

#### Gap #4: SessionState Enum

**File:** `src/Acode.Domain/Sessions/SessionState.cs` (15 lines)
**Status:** ❌ MISSING

**Values Required:**
- `Created` - Initial state, just created
- `Planning` - Analyzing task, creating steps
- `AwaitingApproval` - Awaiting user approval before execution
- `Executing` - Actively running steps
- `Paused` - Paused by user
- `Completed` - All tasks completed successfully
- `Failed` - One or more tasks failed
- `Cancelled` - User cancelled execution

**Acceptance Criteria:** AC-042, AC-071-078
**Effort:** 15 minutes, 15 LOC

**Gap #5-8:** TaskState, StepState, ToolCallState, ArtifactType
- Similar pattern to SessionState
- TaskState: Pending, InProgress, Completed, Failed, Skipped (5 values)
- StepState: Pending, InProgress, Completed, Failed, Skipped (5 values)
- ToolCallState: Pending, Executing, Succeeded, Failed, Cancelled (5 values)
- ArtifactType: FileContent, FileWrite, FileDiff, CommandOutput, ModelResponse, SearchResult (6 values)
- **Total Effort:** 1 hour, 65 LOC for all 5 enums

---

### PHASE 3: Value Objects - Entity IDs (5 Required)

#### Gap #9-13: SessionId, TaskId, StepId, ToolCallId, ArtifactId

**Pattern:** Each extends EntityId

**Code Template:**
```csharp
public sealed class SessionId : EntityId
{
    public SessionId(Guid value) : base(value) { }
    public static SessionId Create() => new(SecureIdGenerator.GenerateId());
}
```

**Per-ID Effort:** 10 minutes, 8 LOC
**Total Effort:** 50 minutes, 40 LOC for all 5 IDs
**Test Count:** 20 tests total (4 tests per ID)

---

### PHASE 4: SessionEvent Class

#### Gap #14: SessionEvent Class

**File:** `src/Acode.Domain/Sessions/SessionEvent.cs` (40 lines)
**Status:** ❌ MISSING

**What's Missing:**
- Event class for state transitions (audit trail)
- FromState property
- ToState property
- Reason property (why transition occurred)
- Timestamp property

**Use:** Records every Session state change for audit and resumability

**Code Pattern:**
```csharp
public sealed record SessionEvent(
    SessionState FromState,
    SessionState ToState,
    string Reason,
    DateTimeOffset Timestamp
);
```

**Acceptance Criteria:** AC-089-094
**Effort:** 45 minutes, 40 LOC
**Test Count:** 6 tests (properties, immutability, timestamp, ordering)

---

### PHASE 5: Core Entity Classes (5 Entities, Most Complex)

#### Gap #15: Session Entity Class

**File:** `src/Acode.Domain/Sessions/Session.cs` (100+ lines)
**Status:** ❌ MISSING

**What's Missing:**
- Aggregate root for entire execution hierarchy
- Manages Tasks collection
- Tracks state and state transitions
- Emits SessionEvent on each transition
- Derives own state from Tasks
- JSON serialization support

**Key Methods:**
- `SessionTask AddTask(string title, string? description)` - Add new task
- `void Transition(SessionState newState, string reason)` - Change state with validation
- `SessionState DeriveState()` - Calculate state from Tasks
- Validation: Cannot transition to invalid states

**Acceptance Criteria:** AC-001-008, AC-042, AC-062-063, AC-067-070, AC-071-078, AC-079-084, AC-085-088, AC-089-094
**Effort:** 4 hours, 120 LOC
**Test Count:** 20 tests
- Construction validation
- AddTask behavior
- State transitions (created→planning→executing→completed)
- Invalid transition handling
- Event recording
- Serialization round-trip
- Equality based on ID

#### Gap #16-19: SessionTask, Step, ToolCall, Artifact Classes

**Pattern:** Similar to Session but at different hierarchy levels

**SessionTask (Task):**
- Effort: 3.5 hours, 100 LOC, 15 tests
- Key: Derives state from Steps, tracks order

**Step:**
- Effort: 3 hours, 90 LOC, 12 tests
- Key: Manages ToolCalls collection, order tracking

**ToolCall (Run-specific, NOT Conversation):**
- Effort: 4 hours, 110 LOC, 12 tests
- Key: Tracks execution state, captures result/error
- **Critical:** Different from Conversation.ToolCall (different domain)

**Artifact (with security validation):**
- Effort: 5 hours, 150 LOC, 12 tests
- Key: Content storage + hash verification + injection prevention
- **Critical:** Security Threat 3 (Injection) mitigation
  - SQL injection detection regex
  - Script injection detection regex
  - File path traversal prevention
  - MIME type whitelist validation
  - Size limit enforcement (10 MB)

**Total Phase 5 Effort:** 16 hours, 500 LOC, 71 tests

---

### PHASE 6: JSON Serialization Configuration

#### Gap #20: Serialization Context

**File:** `src/Acode.Domain/Sessions/RunSessionJsonSerializerContext.cs` (50 lines)
**Status:** ❌ MISSING (optional but recommended)

**What's Missing:**
- JsonSerializerOptions configuration
- camelCase property name policy (FR-095)
- Enum serialization as strings (FR-096)
- Consistent serialization across all entities

**Code Pattern:**
```csharp
[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(SessionTask))]
// ... etc
public partial class RunSessionJsonSerializerContext : JsonSerializerContext
{
}
```

**Acceptance Criteria:** AC-067-070
**Effort:** 1 hour, 50 LOC
**Test Count:** 5 tests (camelCase serialization, enum as string, round-trip)

---

## Effort Summary

| Phase | Component | LOC | Hours | Tests |
|-------|-----------|-----|-------|-------|
| 1 | Foundation (EntityBase, EntityId, SecureIdGenerator) | 175 | 2.5 | 25 |
| 2 | Enums (5 state/type enums) | 75 | 1.5 | 10 |
| 3 | Value Objects (5 IDs) | 40 | 1 | 20 |
| 4 | SessionEvent | 40 | 0.75 | 6 |
| 5 | Entity Classes (Session, Task, Step, ToolCall, Artifact) | 500 | 16 | 71 |
| 6 | Serialization Configuration | 50 | 1 | 5 |
| **Total Production Code** | | **880** | **22.75** | **137** |
| | | | | |
| **Test Code (95+ unit tests)** | | **1,800+** | **20** | **95+** |
| **GRAND TOTAL** | | **2,680+** | **42.75** | **232** |

---

## Blocking Dependencies Analysis

### Current Task: 011a (Run Entities)

**External Dependencies:** ✅ NONE
- Does NOT require Task 050 (Workspace DB Foundation)
- Does NOT require Task 018 (Undo/Checkpoint)
- Does NOT require Task 026 (Observability)
- Does NOT require persistence layer
- **Decision:** CAN PROCEED IMMEDIATELY ✅

**Downstream Tasks Blocked:**
- Task-011b (Persistence) - WILL BE BLOCKED by Task 050
- Task-011c (Resume Behavior) - BLOCKED by 011b
- Task-011 (State Machine) - BLOCKED by 011b

**Rationale:** 011a provides pure domain logic with no external dependencies. Other tasks can wait for Task 050. Implementing 011a now provides foundation.

---

## Production Readiness Assessment

**Current Status:** ❌ NOT READY (0% complete)
**Minimum for Production:** ≥ 95% AC compliance

**Blockers to Production Ready:**
1. ❌ EntityId abstract base not implemented (0% → 5%)
2. ❌ SecureIdGenerator not implemented (5% → 9%)
3. ❌ All 5 state enums missing (9% → 14%)
4. ❌ All 5 entity ID classes missing (14% → 19%)
5. ❌ SessionEvent class missing (19% → 20%)
6. ❌ All 5 entity classes missing (20% → 95%)
7. ❌ Serialization configuration missing (95% → 100%)

**To Reach Production Ready:** Must complete ALL 20 gaps = 42.75 hours work

---

## Next Steps

1. **Immediate:** Create task-011a-completion-checklist.md with phase-by-phase implementation guide
2. **Then:** Analyze task-011b and task-011c using same methodology
3. **Create PR #52:** Document entire task-011 suite analysis

---

## Summary

**Task-011a is a greenfield implementation with zero current work.**

- ✅ No external dependencies - can start immediately
- ✅ Clear specification with code examples
- ✅ Well-defined acceptance criteria (94 ACs)
- ✅ Strong security requirements (injection prevention)
- ❌ Significant effort: 42.75 hours to complete
- ❌ Not ready for production: 0.0% complete

**Recommendation:** Proceed with Phase 1 (Foundation) using strict TDD. Each phase builds on the previous. Commit after each phase. Estimated completion: 40-50 hours.

---

**Status:** ❌ NOT STARTED - 0% COMPLETE
**Estimated Time to 100%:** 42.75 hours
**Blocking Dependencies:** NONE - Can proceed immediately
