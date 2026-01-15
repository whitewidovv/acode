# Task-011a Completion Checklist: Run Entities (Session/Task/Step/Tool Call/Artifacts)

**Status:** 📋 READY FOR IMPLEMENTATION
**Phases:** 7 phases, 20-28 hours estimated
**Test Coverage:** 27+ test methods expected across 4 test files
**Acceptance Criteria:** 94 total ACs, 0% currently implemented

---

## IMPLEMENTATION INSTRUCTIONS FOR FRESH AGENT

This checklist is a self-contained implementation guide. You can pick up this document and implement task-011a to 100% specification compliance by working through these phases sequentially.

**Critical Rules:**
1. **Work in TDD order:** Tests FIRST (RED), then implementation (GREEN), then refactor (CLEAN)
2. **Reference the spec:** Every gap includes spec line numbers - use them
3. **Commit after each gap:** Each checklist item = one commit (not batched)
4. **No shortcuts:** Mark items ✅ ONLY when tests pass and semantic verification complete
5. **Verify semantic completeness:** Don't count "file exists" - count "no NotImplementedException" AND "all tests passing"

**Progress Tracking:**
- [ ] = Not started
- 🔄 = In progress
- ✅ = Complete and verified

---

## PHASE 1: BASE CLASSES (EntityId & EntityBase) - 2-3 Hours

**Objective:** Create abstract base classes that all entities inherit from. These are critical dependencies for all other phases.

**Files to Create:** 2 (src/Acode.Domain/Common/EntityId.cs, src/Acode.Domain/Common/EntityBase.cs)
**Tests to Create:** 2 test files (EntityIdTests, EntityBaseTests) with 8 test methods
**Effort:** 2-3 hours

### Gap 1.1: Create EntityId Abstract Base Class

**Current State:** ❌ MISSING
**Spec Reference:** lines 3672-3705 (Implementation Prompt)
**ACs Covered:** AC-058 (UUID v7 format), AC-059 (IDs generated on creation), AC-060 (IDs immutable), AC-061 (IDs database-safe)

**What Exists:** Nothing - this class must be created from scratch

**What's Missing:** Complete EntityId abstract class with:
- Guid Value property (read-only)
- Parameterless constructor (generates UUID v7)
- Constructor accepting Guid value (validation)
- IEquatable<EntityId> implementation
- Equals(object) override
- GetHashCode() override
- ToString() override

**Implementation Details (from spec, lines 3672-3705):**

```csharp
namespace Acode.Domain.Common;

public abstract class EntityId : IEquatable<EntityId>
{
    public Guid Value { get; }

    protected EntityId()
    {
        Value = Guid.CreateVersion7();
    }

    protected EntityId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("ID cannot be empty", nameof(value));
        Value = value;
    }

    public bool Equals(EntityId? other) =>
        other is not null && Value == other.Value;

    public override bool Equals(object? obj) =>
        Equals(obj as EntityId);

    public override int GetHashCode() =>
        Value.GetHashCode();

    public override string ToString() =>
        Value.ToString();
}
```

**Acceptance Criteria Covered:**
- AC-058: UUID v7 format used (Guid.CreateVersion7())
- AC-059: IDs generated on creation (parameterless constructor)
- AC-060: IDs immutable (Value is read-only property)
- AC-061: IDs database-safe (standard Guid serialization)

**Test Requirements:** Write 4 unit tests
1. Should_Generate_UUIDv7_On_Parameterless_Construction
2. Should_Accept_Guid_Value_On_Construction
3. Should_Equal_When_Values_Match
4. Should_Not_Equal_When_Values_Differ

**Success Criteria:**
- [ ] EntityId.cs created in src/Acode.Domain/Common/
- [ ] Class is abstract with protected constructors
- [ ] UUID v7 generated on creation (verify version bits = 7)
- [ ] All properties are read-only (immutable)
- [ ] IEquatable<EntityId> implemented correctly
- [ ] 4 unit tests passing
- [ ] No NotImplementedException in code
- [ ] Build: 0 errors, 0 warnings

**Gap Checklist Item:** [ ] 🔄 EntityId abstract base class implementation complete with tests passing

---

### Gap 1.2: Create EntityBase Abstract Base Class

**Current State:** ❌ MISSING
**Spec Reference:** lines 3647-3670 (Implementation Prompt)
**ACs Covered:** AC-004 (CreatedAt recorded), AC-005 (UpdatedAt maintained)

**What Exists:** Nothing - this class must be created from scratch

**What's Missing:** Complete EntityBase abstract class with:
- Generic parameter TId where TId : EntityId
- Id property (read-only, type TId)
- CreatedAt property (read-only, DateTimeOffset)
- UpdatedAt property (protected set, DateTimeOffset)
- Protected constructor accepting TId
- Protected MarkUpdated() method

**Implementation Details (from spec, lines 3647-3670):**

```csharp
namespace Acode.Domain.Common;

public abstract class EntityBase<TId> where TId : EntityId
{
    public TId Id { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; protected set; }

    protected EntityBase(TId id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    protected void MarkUpdated()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

**Acceptance Criteria Covered:**
- AC-004: CreatedAt recorded (set in constructor to DateTimeOffset.UtcNow)
- AC-005: UpdatedAt maintained (can be updated via MarkUpdated())

**Test Requirements:** Write 4 unit tests
1. Should_Require_Non_Null_Id
2. Should_Record_CreatedAt_On_Construction
3. Should_Initialize_UpdatedAt_Equal_To_CreatedAt
4. Should_Update_UpdatedAt_When_MarkUpdated_Called

**Success Criteria:**
- [ ] EntityBase.cs created in src/Acode.Domain/Common/
- [ ] Class is abstract with generic TId parameter
- [ ] Id is read-only and required in constructor
- [ ] CreatedAt is read-only, set to UtcNow
- [ ] UpdatedAt is protected-set, initialized to CreatedAt
- [ ] MarkUpdated() method exists and updates UpdatedAt
- [ ] 4 unit tests passing
- [ ] No NotImplementedException in code
- [ ] Build: 0 errors, 0 warnings

**Gap Checklist Item:** [ ] 🔄 EntityBase abstract base class implementation complete with tests passing

---

**Phase 1 Summary:**
- [ ] Gap 1.1: EntityId class - ✅ COMPLETE
- [ ] Gap 1.2: EntityBase class - ✅ COMPLETE
- [ ] Build: 0 errors, 0 warnings
- [ ] Tests: 8/8 passing
- [ ] ACs verified: AC-004, AC-005, AC-058, AC-059, AC-060, AC-061 (6/94)

---

## PHASE 2: SESSION DOMAIN (Session, SessionId, SessionState, SessionEvent) - 4-6 Hours

**Objective:** Implement the root aggregate and session state management. Session is the entry point for all entity access.

**Files to Create:** 4 (SessionId.cs, SessionState.cs, SessionEvent.cs, Session.cs)
**Tests to Create:** SessionTests.cs with 14 test methods
**Effort:** 4-6 hours

### Gap 2.1: Create SessionId Value Object

**Current State:** ❌ MISSING
**Spec Reference:** lines 4169-4173 (Implementation Prompt)
**ACs Covered:** AC-001 (UUID v7 ID generated)

**What Exists:** Nothing - inherits from EntityId created in Phase 1

**What's Missing:** SessionId sealed class that:
- Inherits from EntityId
- Provides public parameterless constructor
- Provides public constructor accepting Guid value
- Is sealed (cannot be subclassed)

**Implementation Details (from spec, lines 4169-4173):**

```csharp
namespace Acode.Domain.Sessions;

public sealed class SessionId : EntityId
{
    public SessionId() : base() { }
    public SessionId(Guid value) : base(value) { }
}
```

**Acceptance Criteria Covered:**
- AC-001: UUID v7 ID generated (via EntityId base)

**Test Requirements:** 1 unit test
1. Should_Create_SessionId_With_Valid_UUIDv7

**Success Criteria:**
- [ ] SessionId.cs created in src/Acode.Domain/Sessions/
- [ ] Class is sealed and inherits from EntityId
- [ ] Both constructors present and functional
- [ ] 1 unit test passing
- [ ] Build: 0 errors, 0 warnings

**Gap Checklist Item:** [ ] 🔄 SessionId value object implementation complete with tests passing

---

### Gap 2.2: Create SessionState Enum

**Current State:** ❌ MISSING
**Spec Reference:** lines 4151-4161 (Implementation Prompt)
**ACs Covered:** AC-003 (State tracked), AC-042 (All Session states work), AC-071-078 (State transitions)

**What Exists:** Nothing - enum must be created from scratch

**What's Missing:** SessionState enum with all valid states:
- Created
- Planning
- AwaitingApproval
- Executing
- Paused
- Completed
- Failed
- Cancelled

**Implementation Details (from spec, lines 4151-4161):**

```csharp
namespace Acode.Domain.Sessions;

public enum SessionState
{
    Created,
    Planning,
    AwaitingApproval,
    Executing,
    Paused,
    Completed,
    Failed,
    Cancelled
}
```

**Acceptance Criteria Covered:**
- AC-003: State tracked (SessionState enum)
- AC-042: All Session states work (8 states defined)
- AC-071-078: State transitions supported

**Test Requirements:** 1 unit test
1. Should_Contain_All_Expected_Session_States

**Success Criteria:**
- [ ] SessionState.cs created in src/Acode.Domain/Sessions/
- [ ] Enum contains all 8 required states
- [ ] 1 unit test passing (verify all states enumerable)
- [ ] Build: 0 errors, 0 warnings

**Gap Checklist Item:** [ ] 🔄 SessionState enum implementation complete with tests passing

---

### Gap 2.3: Create SessionEvent Record

**Current State:** ❌ MISSING
**Spec Reference:** lines 4163-4167 (Implementation Prompt)
**ACs Covered:** AC-007 (Events collection works), AC-089-094 (SessionEvent records state transitions)

**What Exists:** Nothing - record must be created from scratch

**What's Missing:** SessionEvent record with:
- FromState property (SessionState)
- ToState property (SessionState)
- Reason property (string)
- Timestamp property (DateTimeOffset)

**Implementation Details (from spec, lines 4163-4167):**

```csharp
namespace Acode.Domain.Sessions;

public record SessionEvent(
    SessionState FromState,
    SessionState ToState,
    string Reason,
    DateTimeOffset Timestamp);
```

**Acceptance Criteria Covered:**
- AC-007: Events collection works
- AC-089: SessionEvent records FromState
- AC-090: SessionEvent records ToState
- AC-091: SessionEvent records Reason
- AC-092: SessionEvent records Timestamp
- AC-093: Events append-only
- AC-094: Events chronologically ordered

**Test Requirements:** 2 unit tests
1. Should_Create_SessionEvent_With_All_Fields
2. Should_Support_Equality_When_All_Fields_Match

**Success Criteria:**
- [ ] SessionEvent.cs created in src/Acode.Domain/Sessions/
- [ ] Record has all 4 properties
- [ ] Record is immutable (record type)
- [ ] 2 unit tests passing
- [ ] Build: 0 errors, 0 warnings

**Gap Checklist Item:** [ ] 🔄 SessionEvent record implementation complete with tests passing

---

### Gap 2.4: Create Session Entity (Main Entity)

**Current State:** ❌ MISSING
**Spec Reference:** lines 3707-3759 (Implementation Prompt)
**ACs Covered:** AC-001-008 (all Session properties), AC-046 (State derivation), AC-071-078 (State transitions)

**What Exists:** Nothing - entity must be created from scratch

**What's Missing:** Session sealed entity class with:
- Inherits from EntityBase<SessionId>
- Private readonly List<SessionTask> _tasks
- Private readonly List<SessionEvent> _events
- string TaskDescription property (read-only)
- SessionState State property (private set)
- JsonDocument? Metadata property (read-only)
- IReadOnlyList<SessionTask> Tasks property
- IReadOnlyList<SessionEvent> Events property
- Constructor: Session(string taskDescription, JsonDocument? metadata = null)
- Method: SessionTask AddTask(string title, string? description = null)
- Method: void Transition(SessionState newState, string reason)
- Method: SessionState DeriveState()
- Validation for non-empty TaskDescription

**Implementation Details (from spec, lines 3707-3759):**

```csharp
namespace Acode.Domain.Sessions;

public sealed class Session : EntityBase<SessionId>
{
    private readonly List<SessionTask> _tasks = new();
    private readonly List<SessionEvent> _events = new();

    public string TaskDescription { get; }
    public SessionState State { get; private set; }
    public JsonDocument? Metadata { get; }

    public IReadOnlyList<SessionTask> Tasks => _tasks.AsReadOnly();
    public IReadOnlyList<SessionEvent> Events => _events.AsReadOnly();

    public Session(string taskDescription, JsonDocument? metadata = null)
        : base(new SessionId())
    {
        if (string.IsNullOrWhiteSpace(taskDescription))
            throw new ArgumentException("Task description required", nameof(taskDescription));

        TaskDescription = taskDescription;
        State = SessionState.Created;
        Metadata = metadata;
    }

    public SessionTask AddTask(string title, string? description = null)
    {
        var task = new SessionTask(Id, title, description, _tasks.Count);
        _tasks.Add(task);
        MarkUpdated();
        return task;
    }

    public void Transition(SessionState newState, string reason)
    {
        var @event = new SessionEvent(State, newState, reason);
        _events.Add(@event);
        State = newState;
        MarkUpdated();
    }

    public SessionState DeriveState()
    {
        if (_tasks.Count == 0) return State;
        if (_tasks.All(t => t.State == TaskState.Completed)) return SessionState.Completed;
        if (_tasks.Any(t => t.State == TaskState.Failed)) return SessionState.Failed;
        if (_tasks.Any(t => t.State == TaskState.InProgress)) return SessionState.Executing;
        return State;
    }
}
```

**Acceptance Criteria Covered:**
- AC-001: UUID v7 ID generated (SessionId)
- AC-002: TaskDescription stored
- AC-003: State tracked (SessionState)
- AC-004: CreatedAt recorded (via EntityBase)
- AC-005: UpdatedAt maintained (via EntityBase)
- AC-006: Tasks collection works
- AC-007: Events collection works
- AC-008: Metadata optional
- AC-046: State derivation correct
- AC-071-078: State transitions

**Test Requirements:** 14 unit tests (see spec lines 3030-3242)
1. Should_Generate_UUIDv7_Id
2. Should_Require_TaskDescription
3. Should_Require_Non_Whitespace_TaskDescription
4. Should_Initialize_With_Created_State
5. Should_Record_CreatedAt_Timestamp
6. Should_Initialize_UpdatedAt_Equal_To_CreatedAt
7. Should_Initialize_Empty_Tasks_Collection
8. Should_Add_Task_To_Session
9. Should_Derive_Completed_State_When_All_Tasks_Completed
10. Should_Transition_To_Planning_State
11. Should_Record_State_Transition_Event
12. Should_Update_UpdatedAt_On_State_Transition
13. Should_Throw_When_Completing_Session_Without_Tasks
14. Should_Serialize_To_JSON / Should_Deserialize_From_JSON

**Success Criteria:**
- [ ] Session.cs created in src/Acode.Domain/Sessions/
- [ ] Inherits from EntityBase<SessionId>
- [ ] All properties implemented with correct visibility
- [ ] Collections are IReadOnlyList (external immutability)
- [ ] TaskDescription validation enforced
- [ ] AddTask() method works and updates sessions
- [ ] Transition() method creates event and updates state
- [ ] DeriveState() implements state derivation logic
- [ ] 14 unit tests passing (100% coverage)
- [ ] No NotImplementedException in code
- [ ] Build: 0 errors, 0 warnings

**Gap Checklist Item:** [ ] 🔄 Session entity implementation complete with tests passing

---

**Phase 2 Summary:**
- [ ] Gap 2.1: SessionId - ✅ COMPLETE
- [ ] Gap 2.2: SessionState enum - ✅ COMPLETE
- [ ] Gap 2.3: SessionEvent record - ✅ COMPLETE
- [ ] Gap 2.4: Session entity - ✅ COMPLETE
- [ ] Build: 0 errors, 0 warnings
- [ ] Tests: 18/18 passing (SessionTests + supporting)
- [ ] ACs verified: AC-001-008, AC-046, AC-071-078, AC-089-094 (22/94)

---

## PHASE 3: TASK DOMAIN (TaskId, TaskState, SessionTask) - 3-4 Hours

**Objective:** Implement task entity within sessions. Tasks represent high-level goals.

**Files to Create:** 3 (TaskId.cs, TaskState.cs, SessionTask.cs)
**Tests to Create:** TaskTests.cs with tests for all task functionality
**Effort:** 3-4 hours

### Gap 3.1: Create TaskId Value Object

**Current State:** ❌ MISSING
**Spec Reference:** lines 3863-3867 (Implementation Prompt)
**ACs Covered:** AC-009 (UUID v7 ID generated)

**What Exists:** Nothing

**What's Missing:** TaskId sealed class (like SessionId, inherits EntityId)

**Implementation Details (from spec, lines 3863-3867):**

```csharp
namespace Acode.Domain.Tasks;

public sealed class TaskId : EntityId
{
    public TaskId() : base() { }
    public TaskId(Guid value) : base(value) { }
}
```

**Gap Checklist Item:** [ ] 🔄 TaskId value object implementation complete

---

### Gap 3.2: Create TaskState Enum

**Current State:** ❌ MISSING
**Spec Reference:** lines 3854-3861 (Implementation Prompt)
**ACs Covered:** AC-013 (State tracked)

**What Exists:** Nothing

**What's Missing:** TaskState enum with states: Pending, InProgress, Completed, Failed, Skipped

**Implementation Details (from spec, lines 3854-3861):**

```csharp
public enum TaskState
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Skipped
}
```

**Gap Checklist Item:** [ ] 🔄 TaskState enum implementation complete

---

### Gap 3.3: Create SessionTask Entity

**Current State:** ❌ MISSING
**Spec Reference:** lines 3764-3852 (Implementation Prompt)
**ACs Covered:** AC-009-016 (all Task properties), AC-043 (Task states work)

**What Exists:** Nothing

**What's Missing:** SessionTask sealed entity with:
- SessionId, Title, Description, State, Order properties
- Steps collection
- Methods: AddStep, Start, Complete, Fail, Skip, DeriveState
- Validation for required fields

**Implementation Details (from spec, lines 3764-3852):**

```csharp
namespace Acode.Domain.Tasks;

public sealed class SessionTask : EntityBase<TaskId>
{
    private readonly List<Step> _steps = new();

    public SessionId SessionId { get; }
    public string Title { get; }
    public string? Description { get; }
    public TaskState State { get; private set; }
    public int Order { get; }
    public JsonDocument? Metadata { get; }

    public IReadOnlyList<Step> Steps => _steps.AsReadOnly();

    public SessionTask(
        SessionId sessionId,
        string title,
        string? description,
        int order,
        JsonDocument? metadata = null)
        : base(new TaskId())
    {
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title required", nameof(title));

        if (order < 0)
            throw new ArgumentException("Order must be >= 0", nameof(order));

        Title = title;
        Description = description;
        Order = order;
        State = TaskState.Pending;
        Metadata = metadata;
    }

    public Step AddStep(string name, string? description = null)
    {
        var step = new Step(Id, name, description, _steps.Count);
        _steps.Add(step);
        MarkUpdated();
        return step;
    }

    public void Start()
    {
        if (State != TaskState.Pending)
            throw new InvalidOperationException($"Cannot start task in {State} state");

        State = TaskState.InProgress;
        MarkUpdated();
    }

    public void Complete()
    {
        if (_steps.Any(s => s.State != StepState.Completed && s.State != StepState.Skipped))
            throw new InvalidOperationException("Cannot complete task: not all steps completed");

        State = TaskState.Completed;
        MarkUpdated();
    }

    public void Fail()
    {
        State = TaskState.Failed;
        MarkUpdated();
    }

    public void Skip()
    {
        State = TaskState.Skipped;
        MarkUpdated();
    }

    public TaskState DeriveState()
    {
        if (_steps.Count == 0) return State;
        if (_steps.All(s => s.State == StepState.Completed || s.State == StepState.Skipped))
            return TaskState.Completed;
        if (_steps.Any(s => s.State == StepState.Failed))
            return TaskState.Failed;
        if (_steps.Any(s => s.State == StepState.InProgress))
            return TaskState.InProgress;
        return State;
    }
}
```

**Gap Checklist Item:** [ ] 🔄 SessionTask entity implementation complete with tests passing

---

**Phase 3 Summary:**
- [ ] Gap 3.1: TaskId - ✅ COMPLETE
- [ ] Gap 3.2: TaskState enum - ✅ COMPLETE
- [ ] Gap 3.3: SessionTask entity - ✅ COMPLETE
- [ ] ACs verified: AC-009-016, AC-043 (8/94, cumulative 30/94)

---

## PHASE 4: STEP DOMAIN (StepId, StepState, Step) - 3-4 Hours

**Objective:** Implement step entity within tasks. Steps are discrete actions.

**Files to Create:** 3 (StepId.cs, StepState.cs, Step.cs)
**Effort:** 3-4 hours

### Gap 4.1-4.3: Create StepId, StepState, Step

**Current State:** ❌ MISSING (all 3 files)
**Spec Reference:** lines 3870-3976 (Implementation Prompt)
**ACs Covered:** AC-017-023 (all Step properties), AC-044 (Step states work)

**Implementation Details (from spec, lines 3971-3975 for StepId):**

```csharp
public sealed class StepId : EntityId
{
    public StepId() : base() { }
    public StepId(Guid value) : base(value) { }
}
```

**StepState enum (lines 3962-3969):**

```csharp
public enum StepState
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Skipped
}
```

**Step entity (lines 3875-3960):** Full implementation with ToolCalls collection, state transitions, and derivation

**Gap Checklist Item:** [ ] 🔄 Step domain (StepId, StepState, Step) implementation complete with tests passing

---

## PHASE 5: TOOLCALL DOMAIN (ToolCallId, ToolCallState, ToolCall) - 3-4 Hours

**Objective:** Implement tool call entity within steps. ToolCalls are atomic operations.

**Files to Create:** 3 (ToolCallId.cs, ToolCallState.cs, ToolCall.cs)
**Effort:** 3-4 hours

### Gap 5.1-5.3: Create ToolCallId, ToolCallState, ToolCall

**Current State:** ❌ MISSING (all 3 files)
**Spec Reference:** lines 3978-4083 (Implementation Prompt)
**ACs Covered:** AC-024-032 (all ToolCall properties), AC-045 (ToolCall states work)

**Implementation Details (from spec):**
- ToolCallId sealed class (lines 4079-4083)
- ToolCallState enum (lines 4070-4077): Pending, Executing, Succeeded, Failed, Cancelled
- ToolCall entity (lines 3983-4068): Full implementation with Artifacts collection, state transitions (Start, Succeed, Fail, Cancel)

**Gap Checklist Item:** [ ] 🔄 ToolCall domain implementation complete with tests passing

---

## PHASE 6: ARTIFACT DOMAIN (ArtifactId, ArtifactType, Artifact) - 2-3 Hours

**Objective:** Implement artifact entity for outputs produced during execution.

**Files to Create:** 3 (ArtifactId.cs, ArtifactType.cs, Artifact.cs)
**Tests to Create:** ArtifactTests.cs with 10 test methods
**Effort:** 2-3 hours

### Gap 6.1: Create ArtifactId Value Object

**Current State:** ❌ MISSING
**Spec Reference:** lines 4139-4143 (Implementation Prompt)

**Implementation Details (from spec):**

```csharp
public sealed class ArtifactId : EntityId
{
    public ArtifactId() : base() { }
    public ArtifactId(Guid value) : base(value) { }
}
```

**Gap Checklist Item:** [ ] 🔄 ArtifactId value object implementation complete

---

### Gap 6.2: Create ArtifactType Enum

**Current State:** ❌ MISSING
**Spec Reference:** lines 4129-4137 (Implementation Prompt)
**ACs Covered:** AC-047-052 (all artifact types)

**Implementation Details (from spec, lines 4129-4137):**

```csharp
public enum ArtifactType
{
    FileContent,
    FileWrite,
    FileDiff,
    CommandOutput,
    ModelResponse,
    SearchResult
}
```

**Gap Checklist Item:** [ ] 🔄 ArtifactType enum implementation complete

---

### Gap 6.3: Create Artifact Entity

**Current State:** ❌ MISSING
**Spec Reference:** lines 4088-4127 (Implementation Prompt)
**ACs Covered:** AC-033-041 (all Artifact properties), AC-047-052 (artifact types)

**What Exists:** Nothing

**What's Missing:** Artifact sealed entity with:
- ToolCallId, Type, Name, Content, ContentHash, ContentType, Size properties (all read-only)
- Constructor validates inputs
- ContentHash computed via SHA256
- Size calculated from content length
- Immutable after creation

**Implementation Details (from spec, lines 4088-4127):**

```csharp
namespace Acode.Domain.Artifacts;

public sealed class Artifact : EntityBase<ArtifactId>
{
    public ToolCallId ToolCallId { get; }
    public ArtifactType Type { get; }
    public string Name { get; }
    public byte[] Content { get; }
    public string ContentHash { get; }
    public string ContentType { get; }
    public long Size { get; }

    public Artifact(
        ToolCallId toolCallId,
        ArtifactType type,
        string name,
        byte[] content,
        string contentType)
        : base(new ArtifactId())
    {
        ToolCallId = toolCallId ?? throw new ArgumentNullException(nameof(toolCallId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name required", nameof(name));

        Type = type;
        Name = name;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ContentType = contentType ?? "application/octet-stream";
        Size = content.Length;
        ContentHash = ComputeHash(content);
    }

    private static string ComputeHash(byte[] content)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(content);
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
```

**Acceptance Criteria Covered:**
- AC-033: UUID v7 ID generated (ArtifactId)
- AC-034: ToolCallId required (constructor parameter)
- AC-035: Type required (ArtifactType enum)
- AC-036: Name required (validation in constructor)
- AC-037: Content stored (byte[] property)
- AC-038: ContentHash computed (SHA256 computation)
- AC-039: ContentType set (with default "application/octet-stream")
- AC-040: Size calculated (from content.Length)
- AC-041: Immutable after creation (all properties read-only)

**Test Requirements:** 10 unit tests (see spec lines 3255-3441)
1. Should_Generate_UUIDv7_Id
2. Should_Compute_ContentHash
3. Should_Calculate_Size
4. Should_Be_Immutable_After_Creation
5. Should_Validate_Hash_Matches_Content
6. Should_Reject_Null_Content
7. Should_Reject_Null_ToolCallId
8. Should_Set_ContentType
9. Should_Support_All_Artifact_Types (test all 6 types)
10. Additional validation tests

**Success Criteria:**
- [ ] Artifact.cs created in src/Acode.Domain/Artifacts/
- [ ] All properties are read-only (immutable)
- [ ] ContentHash computed correctly using SHA256
- [ ] Size calculated from content length
- [ ] Validation rejects null ToolCallId, null content, empty name
- [ ] 10 unit tests passing
- [ ] No NotImplementedException in code
- [ ] Build: 0 errors, 0 warnings

**Gap Checklist Item:** [ ] 🔄 Artifact entity implementation complete with tests passing

---

**Phase 6 Summary:**
- [ ] Gap 6.1: ArtifactId - ✅ COMPLETE
- [ ] Gap 6.2: ArtifactType enum - ✅ COMPLETE
- [ ] Gap 6.3: Artifact entity - ✅ COMPLETE
- [ ] ACs verified: AC-033-052 (20/94, cumulative 50/94)

---

## PHASE 7: INTEGRATION & PERFORMANCE - 2-3 Hours

**Objective:** Write hierarchy tests to verify entity relationships and performance benchmarks.

**Files to Create:** 2 (HierarchyTests.cs, Performance benchmarks)
**Tests to Create:** 3 integration tests + 4 performance benchmarks
**Effort:** 2-3 hours

### Gap 7.1: Create Hierarchy Integration Tests

**Current State:** ❌ MISSING
**Spec Reference:** lines 3447-3524 (Testing Requirements)
**ACs Covered:** AC-062-066 (hierarchy relationships), AC-067-070 (serialization), AC-079-084 (collections)

**What Exists:** Nothing

**What's Missing:** 3 integration test methods that verify the full entity hierarchy works together:

**Test Requirements:** 3 integration tests
1. Should_Navigate_From_Session_To_Artifacts (verify full hierarchy traversal)
2. Should_Derive_States_Correctly_Through_Hierarchy (verify state derivation propagates up)
3. Should_Maintain_Referential_Integrity (verify foreign keys set correctly)

**Implementation Details (from spec, lines 3447-3524):**

```csharp
// File: tests/Acode.Domain.Tests/Integration/HierarchyTests.cs
namespace Acode.Domain.Tests.Integration;

using Acode.Domain.Sessions;
using Acode.Domain.Artifacts;
using FluentAssertions;
using System.Text;
using Xunit;

public class HierarchyTests
{
    [Fact]
    public void Should_Navigate_From_Session_To_Artifacts()
    {
        // Arrange & Act
        var session = new Session("Implement feature X");
        var task = session.AddTask("Write code");
        var step = task.AddStep("Read file");
        var toolCall = step.AddToolCall("read_file", JsonDocument.Parse("{\"path\":\"test.cs\"}"));
        var content = Encoding.UTF8.GetBytes("file content");
        var artifact = toolCall.AddArtifact(
            ArtifactType.FileContent,
            "test.cs",
            content,
            "text/x-csharp");

        // Assert - verify hierarchy navigation
        session.Tasks.Should().ContainSingle();
        session.Tasks.First().Should().Be(task);

        task.Steps.Should().ContainSingle();
        task.Steps.First().Should().Be(step);

        step.ToolCalls.Should().ContainSingle();
        step.ToolCalls.First().Should().Be(toolCall);

        toolCall.Artifacts.Should().ContainSingle();
        toolCall.Artifacts.First().Should().Be(artifact);
    }

    [Fact]
    public void Should_Derive_States_Correctly_Through_Hierarchy()
    {
        // Arrange
        var session = new Session("Implement feature X");
        var task1 = session.AddTask("Task 1");
        var task2 = session.AddTask("Task 2");

        var step1 = task1.AddStep("Step 1");
        var step2 = task1.AddStep("Step 2");

        // Act - complete all steps
        step1.Complete();
        step2.Complete();

        // Assert - task should derive Completed state
        var task1DerivedState = task1.DeriveState();
        task1DerivedState.Should().Be(TaskState.Completed);
    }

    [Fact]
    public void Should_Maintain_Referential_Integrity()
    {
        // Arrange & Act
        var session = new Session("Implement feature X");
        var task = session.AddTask("Task 1");
        var step = task.AddStep("Step 1");

        // Assert - verify foreign keys
        task.SessionId.Should().Be(session.Id);
        step.TaskId.Should().Be(task.Id);
    }
}
```

**Success Criteria:**
- [ ] HierarchyTests.cs created in tests/Acode.Domain.Tests/Integration/
- [ ] 3 integration tests present and passing
- [ ] Full entity hierarchy can be created and navigated
- [ ] State derivation works through hierarchy
- [ ] Referential integrity verified
- [ ] Build: 0 errors, 0 warnings

**Gap Checklist Item:** [ ] 🔄 Hierarchy integration tests complete with all tests passing

---

### Gap 7.2: Create Performance Benchmarks

**Current State:** ❌ MISSING
**Spec Reference:** lines 3526-3533 (Performance Benchmarks section)
**ACs Covered:** Implicit performance validation

**What Exists:** Nothing

**What's Missing:** 4 performance benchmark scenarios that measure:
1. Entity creation performance (target: < 0.5ms per entity)
2. State derivation performance (target: < 5ms)
3. JSON serialization performance (target: < 2ms)
4. Hash computation performance (target: < 1ms per KB)

**Performance Targets (from spec, lines 3526-3533):**

```
| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Entity creation | 0.5ms | 1ms |
| State derivation | 5ms | 10ms |
| JSON serialization | 2ms | 5ms |
| Hash computation | 1ms/KB | 5ms/KB |
```

**Success Criteria:**
- [ ] Performance benchmark file created
- [ ] 4 benchmark scenarios implemented
- [ ] Entity creation: < 0.5ms average (< 1ms maximum)
- [ ] State derivation: < 5ms average (< 10ms maximum)
- [ ] JSON serialization: < 2ms average (< 5ms maximum)
- [ ] Hash computation: < 1ms/KB average
- [ ] Benchmarks can be run with: `dotnet run -c Release --project tests/Acode.Performance.Tests`

**Gap Checklist Item:** [ ] 🔄 Performance benchmarks implementation complete and passing targets

---

**Phase 7 Summary:**
- [ ] Gap 7.1: Hierarchy integration tests - ✅ COMPLETE
- [ ] Gap 7.2: Performance benchmarks - ✅ COMPLETE
- [ ] Build: 0 errors, 0 warnings
- [ ] All 27+ tests passing (14 SessionTests + 10 ArtifactTests + 3 HierarchyTests + implicit Task/Step/ToolCall tests)
- [ ] ACs verified: All 94/94 ACs complete

---

## FINAL VERIFICATION CHECKLIST (BEFORE MARKING COMPLETE)

### File Count Verification
- [ ] All 25 production files created (6 directories)
  - [ ] 2 base classes (Common/)
  - [ ] 4 Session files (Sessions/)
  - [ ] 3 Task files (Tasks/)
  - [ ] 3 Step files (Steps/)
  - [ ] 3 ToolCall files (ToolCalls/)
  - [ ] 3 Artifact files (Artifacts/)
- [ ] All 4 test files created
  - [ ] SessionTests.cs (14 tests)
  - [ ] ArtifactTests.cs (10 tests)
  - [ ] HierarchyTests.cs (3 tests)
  - [ ] Performance benchmarks

### Semantic Completeness Verification
- [ ] NO NotImplementedException found ANYWHERE in production code
- [ ] NO TODO/FIXME comments indicating incomplete work
- [ ] All methods from spec present in production files
- [ ] All property signatures match spec exactly

### Test Verification
- [ ] 27+ test methods total
- [ ] All tests passing (100% pass rate)
- [ ] SessionTests: 14/14 passing
- [ ] ArtifactTests: 10/10 passing
- [ ] HierarchyTests: 3/3 passing
- [ ] Performance benchmarks meeting targets

### Build & Compilation
- [ ] `dotnet build` → 0 errors, 0 warnings
- [ ] All 25 production files compile successfully
- [ ] All test files compile successfully

### Acceptance Criteria Verification
- [ ] AC-001-094: All 94 ACs verified implemented with evidence
- [ ] AC mapping updated in gap analysis showing 100% completion

### Final Git Status
- [ ] All commits pushed to feature branch
- [ ] One commit per phase (7 commits minimum)
- [ ] Commit messages follow Conventional Commits format
- [ ] Ready for PR creation

---

## SUMMARY TABLE

| Phase | Description | Files | Tests | Hours | Status | AC Coverage |
|-------|-------------|-------|-------|-------|--------|-------------|
| 1 | Base Classes | 2 | 8 | 2-3 | [ ] PENDING | 6/94 |
| 2 | Session Domain | 4 | 18 | 4-6 | [ ] PENDING | 22/94 |
| 3 | Task Domain | 3 | - | 3-4 | [ ] PENDING | 8/94 |
| 4 | Step Domain | 3 | - | 3-4 | [ ] PENDING | 7/94 |
| 5 | ToolCall Domain | 3 | - | 3-4 | [ ] PENDING | 9/94 |
| 6 | Artifact Domain | 3 | 10 | 2-3 | [ ] PENDING | 20/94 |
| 7 | Integration & Perf | 2 | 7 | 2-3 | [ ] PENDING | 20/94 |
| **TOTAL** | **Run Entities** | **25** | **27+** | **20-28** | [ ] PENDING | **94/94** |

---

**Completion Status:** Ready for implementation. Follow phases 1-7 sequentially using TDD (RED → GREEN → CLEAN). Mark each phase complete only when all tests pass and semantic verification succeeds. Good luck!

---
