# Task-011a Completion Checklist: Run Entities (Session, Task, Step, Tool Call, Artifacts)

**Status:** 0% COMPLETE - NOT STARTED

**Date:** 2026-01-15
**Created By:** Claude Code
**Purpose:** 6-phase TDD implementation plan to reach 100% AC compliance (42.75 hours, 94 ACs)

---

## INSTRUCTIONS FOR IMPLEMENTATION

This checklist contains **20 major implementation gaps** across **6 sequential phases**. Task-011a is a greenfield domain modeling task with zero external dependencies.

**Total Estimated Effort:** 42.75 hours
- Phase 1-2: 3.5 hours (Foundation + Enums)
- Phase 3-4: 1.75 hours (IDs + Events)
- Phase 5: 16 hours (Entity Classes - most complex)
- Phase 6: 1 hour (Serialization)
- Phase 7: 20 hours (Comprehensive testing)

**Work Order:** Sequential phases (each builds on previous). Follow **strict TDD: RED → GREEN → REFACTOR** for each gap.

**Blocking Dependencies:** ✅ NONE - Proceed immediately

---

## PHASE 1: Foundation Infrastructure (2.5 hours)

### What This Phase Does
Creates the base classes that ALL entities will inherit from. This foundation is critical:
- EntityId: Base class for all ID value objects
- SecureIdGenerator: UUID v7 generation with collision handling
- EntityBase<TId>: Base class for all entities

### Work Item 1.1: EntityId Abstract Base Class

**Status:** 🔄 PENDING

**File:** `src/Acode.Domain/Common/EntityId.cs`
**Spec Reference:** Implementation Prompt lines 3672-3705
**Acceptance Criteria:** AC-058, AC-060, AC-061
**Effort:** 30 min, 35 LOC

**TDD Steps:**

1. **RED: Write failing tests first**
```csharp
// tests/Acode.Domain.Tests/Common/EntityIdTests.cs
[Fact]
public void NewId_GeneratesValidGuid()
{
    var id = new TestEntityId(Guid.NewGuid());
    Assert.NotEqual(Guid.Empty, id.Value);
}

[Fact]
public void EmptyId_ThrowsArgumentException()
{
    Assert.Throws<ArgumentException>(() => new TestEntityId(Guid.Empty));
}

[Fact]
public void EqualsOperator_ReturnsTrueForSameValue()
{
    var guid = Guid.NewGuid();
    var id1 = new TestEntityId(guid);
    var id2 = new TestEntityId(guid);
    Assert.True(id1.Equals(id2));
}

// ... 5 more tests for GetHashCode, IEquatable, immutability
```

2. **GREEN: Implement EntityId**
```csharp
public abstract class EntityId : IEquatable<EntityId>
{
    public Guid Value { get; }

    protected EntityId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("ID cannot be empty", nameof(value));
        Value = value;
    }

    public override bool Equals(object? obj) => obj is EntityId other && Value == other.Value;
    public bool Equals(EntityId? other) => other is not null && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");

    public static bool operator ==(EntityId? left, EntityId? right)
        => (left, right) switch
        {
            (null, null) => true,
            (null, _) => false,
            (_, null) => false,
            _ => left.Value == right.Value
        };

    public static bool operator !=(EntityId? left, EntityId? right) => !(left == right);
}
```

3. **REFACTOR:** Review for clarity, documentation
4. **Run tests:** `dotnet test --filter "FullyQualifiedName~EntityIdTests"`
5. **Verify:** All 8 tests passing ✅

**Mark Complete When:**
- [x] EntityId.cs created
- [x] All 8 tests written and passing
- [x] IEquatable<T> implemented correctly
- [x] Empty GUID validation working

**Commit:** `feat(domain): implement EntityId abstract base class with GUID v7 support`

---

### Work Item 1.2: SecureIdGenerator Static Class

**Status:** 🔄 PENDING

**File:** `src/Acode.Domain/Common/SecureIdGenerator.cs`
**Spec Reference:** Security Threat 2 (UUID Collision Prevention), Implementation Prompt
**Acceptance Criteria:** AC-058, AC-059
**Effort:** 1.5 hours, 120 LOC
**COMPLEXITY:** HIGH - Most complex base component

**Why This Matters:**
Multiple concurrent tool invocations must never generate the same UUID. Spec requires UUID v7 (timestamp-based) with monotonic sequence to guarantee uniqueness even under high concurrency.

**TDD Steps:**

1. **RED: Write comprehensive collision/monotonicity tests**
```csharp
[Fact]
public void GenerateId_ReturnsValidGuid()
{
    var id = SecureIdGenerator.GenerateId();
    Assert.NotEqual(Guid.Empty, id);
}

[Fact]
public void GenerateId_IsUuidV7Format()
{
    var id = SecureIdGenerator.GenerateId();
    // UUID v7 has specific version bits
    var bytes = id.ToByteArray();
    var version = (bytes[6] >> 4) & 0xf;
    Assert.Equal(7, version);
}

[Fact]
public void GenerateId_NoCollisionsUnderConcurrency()
{
    var ids = new System.Collections.Concurrent.ConcurrentBag<Guid>();
    var options = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 10 };

    System.Threading.Tasks.Parallel.For(0, 10000, options, _ =>
    {
        ids.Add(SecureIdGenerator.GenerateId());
    });

    var uniqueCount = ids.Distinct().Count();
    Assert.Equal(10000, uniqueCount); // All unique, zero collisions
}

[Fact]
public void GenerateId_MonotonicIncreasing()
{
    var id1 = SecureIdGenerator.GenerateId();
    System.Threading.Thread.Sleep(1); // Small delay
    var id2 = SecureIdGenerator.GenerateId();
    Assert.True(id2 > id1); // UUID v7 are comparable
}

// ... more tests for clock regression, sequence overflow
```

2. **GREEN: Implement SecureIdGenerator**

This is the most complex base class. Spec shows full implementation for UUID v7 generation with:
- Timestamp extraction from system clock
- Monotonic sequence counter (managed via lock)
- Clock regression handling (reuse previous timestamp if clock goes backwards)
- Sequence overflow (retry with new timestamp when sequence maxes out)
- Thread-safe access via lock

```csharp
public static class SecureIdGenerator
{
    private static readonly object _lock = new();
    private static DateTime _lastTimestamp = DateTime.UtcNow;
    private static ushort _sequence = 0;
    private const int MaxSequenceRetries = 3;

    public static Guid GenerateId()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            // Handle clock regression
            if (now < _lastTimestamp)
            {
                now = _lastTimestamp;
            }
            else if (now > _lastTimestamp)
            {
                _lastTimestamp = now;
                _sequence = 0;
            }

            // Handle sequence overflow
            if (_sequence == ushort.MaxValue)
            {
                // Wait for next millisecond
                _sequence = 0;
            }

            _sequence++;

            return CreateUuidV7(now, _sequence);
        }
    }

    private static Guid CreateUuidV7(DateTime timestamp, ushort sequence)
    {
        // Implement UUID v7 format
        // ... (spec shows exact bit layout)
        throw new NotImplementedException("UUID v7 bit packing");
    }
}
```

3. **REFACTOR:** Optimize for performance, add documentation
4. **Run tests:** `dotnet test --filter "FullyQualifiedName~SecureIdGeneratorTests"`
5. **Performance verify:** Generation < 10µs per ID

**Mark Complete When:**
- [x] SecureIdGenerator.cs created
- [x] All 12 tests written and passing
- [x] UUID v7 format verified
- [x] Zero collisions under 10k concurrent generations
- [x] Monotonic ordering enforced
- [x] Clock regression handled

**Commit:** `feat(domain): implement SecureIdGenerator with UUID v7 collision prevention`

---

### Work Item 1.3: EntityBase<TId> Abstract Class

**Status:** 🔄 PENDING

**File:** `src/Acode.Domain/Common/EntityBase.cs`
**Acceptance Criteria:** AC-004, AC-005, AC-015, AC-022, AC-028, AC-050
**Effort:** 20 min, 20 LOC

**TDD Steps:**

1. **RED: Write test for base properties**
```csharp
[Fact]
public void Entity_HasIdProperty()
{
    var entity = new TestEntity(new TestEntityId(Guid.NewGuid()));
    Assert.NotNull(entity.Id);
}

[Fact]
public void Entity_HasCreatedAtTimestamp()
{
    var entity = new TestEntity(new TestEntityId(Guid.NewGuid()));
    Assert.True(entity.CreatedAt <= DateTimeOffset.UtcNow);
}

[Fact]
public void Entity_CanUpdateUpdatedAt()
{
    var entity = new TestEntity(new TestEntityId(Guid.NewGuid()));
    var original = entity.UpdatedAt;
    System.Threading.Thread.Sleep(1);
    entity.MarkUpdated();
    Assert.True(entity.UpdatedAt > original);
}
```

2. **GREEN: Implement EntityBase**
```csharp
public abstract class EntityBase<TId> where TId : EntityId
{
    public TId Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset UpdatedAt { get; protected set; }

    protected EntityBase(TId id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    protected void MarkUpdated() => UpdatedAt = DateTimeOffset.UtcNow;
}
```

3. **REFACTOR:** Add XML documentation
4. **Run tests:** `dotnet test --filter "FullyQualifiedName~EntityBaseTests"`

**Mark Complete When:**
- [x] EntityBase<TId> created
- [x] All 5 tests passing
- [x] Timestamps auto-set on creation
- [x] MarkUpdated working correctly

**Commit:** `feat(domain): implement EntityBase<TId> generic base class with timestamp tracking`

---

## PHASE 1 Summary

**When Complete:**
- [x] 3 base classes created (EntityId, SecureIdGenerator, EntityBase)
- [x] 25 unit tests written and passing
- [x] UUID v7 generation working with collision prevention
- [x] All inheritance relationships ready for Phase 2

**Phase 1 Completion Test:**
```bash
dotnet test tests/Acode.Domain.Tests/Common/ --verbosity normal
# Expected: All 25 tests passing, 0 errors
```

**Commit Summary:**
- Commit 1: EntityId abstract base class
- Commit 2: SecureIdGenerator with UUID v7
- Commit 3: EntityBase<TId> generic base

---

## PHASE 2: State Enums & Type Enums (1.5 hours)

### Work Item 2.1-2.5: Five Enums

**Files:**
- `src/Acode.Domain/Sessions/SessionState.cs` (15 lines, 8 values)
- `src/Acode.Domain/Tasks/TaskState.cs` (12 lines, 5 values)
- `src/Acode.Domain/Steps/StepState.cs` (12 lines, 5 values)
- `src/Acode.Domain/ToolCalls/ToolCallState.cs` (12 lines, 5 values)
- `src/Acode.Domain/Artifacts/ArtifactType.cs` (15 lines, 6 values)

**TDD for SessionState:**

1. **RED: Write enum existence test**
```csharp
[Fact]
public void SessionState_HasAllRequiredValues()
{
    Assert.Equal(8, Enum.GetValues(typeof(SessionState)).Length);
    Assert.Contains(SessionState.Created, Enum.GetValues(typeof(SessionState)).Cast<SessionState>());
    Assert.Contains(SessionState.Executing, Enum.GetValues(typeof(SessionState)).Cast<SessionState>());
    Assert.Contains(SessionState.Completed, Enum.GetValues(typeof(SessionState)).Cast<SessionState>());
    // ... test all 8 values
}
```

2. **GREEN: Create SessionState enum**
```csharp
public enum SessionState
{
    Created = 0,
    Planning = 1,
    AwaitingApproval = 2,
    Executing = 3,
    Paused = 4,
    Completed = 5,
    Failed = 6,
    Cancelled = 7
}
```

3. **Repeat for other 4 enums (TaskState, StepState, ToolCallState, ArtifactType)**

**Effort:** 1.5 hours total for all 5 enums
**Tests:** 10 total (2 tests per enum for existence and value counts)

**Mark Complete When:**
- [x] All 5 enums created
- [x] All required values present
- [x] All 10 tests passing

**Commits:**
- Commit 1: State enums (SessionState, TaskState, StepState, ToolCallState)
- Commit 2: ArtifactType enum

---

## PHASE 3: Entity ID Value Objects (1 hour)

### Work Item 3.1-3.5: Five ID Classes

**Pattern:** Each `extends EntityId`

**TDD for SessionId:**

1. **RED: Write test**
```csharp
[Fact]
public void SessionId_CanBeCreated()
{
    var id = SessionId.Create();
    Assert.NotEqual(Guid.Empty, id.Value);
}

[Fact]
public void SessionId_AreEqual_WhenValuesMatch()
{
    var guid = Guid.NewGuid();
    var id1 = new SessionId(guid);
    var id2 = new SessionId(guid);
    Assert.Equal(id1, id2);
}
```

2. **GREEN: Implement SessionId**
```csharp
public sealed class SessionId : EntityId
{
    public SessionId(Guid value) : base(value) { }
    public static SessionId Create() => new(SecureIdGenerator.GenerateId());
}
```

3. **Repeat for TaskId, StepId, ToolCallId, ArtifactId**

**Effort:** 1 hour total
**Tests:** 20 total (4 tests per ID)

**Mark Complete When:**
- [x] All 5 ID classes created
- [x] All 20 tests passing
- [x] Can use `[ID].Create()` for new entities

**Commit:** `feat(domain): implement entity ID value objects (SessionId, TaskId, StepId, ToolCallId, ArtifactId)`

---

## PHASE 4: SessionEvent Class (45 min)

### Work Item 4.1: SessionEvent Event Record

**File:** `src/Acode.Domain/Sessions/SessionEvent.cs`
**Spec Reference:** FR-103-108
**Acceptance Criteria:** AC-089-094
**Effort:** 45 min, 40 LOC

**TDD Steps:**

1. **RED: Write event property tests**
```csharp
[Fact]
public void SessionEvent_RecordsFromState()
{
    var evt = new SessionEvent(SessionState.Planning, SessionState.Executing, "User started", DateTimeOffset.UtcNow);
    Assert.Equal(SessionState.Planning, evt.FromState);
}

[Fact]
public void SessionEvent_RecordsToState()
{
    var evt = new SessionEvent(SessionState.Planning, SessionState.Executing, "User started", DateTimeOffset.UtcNow);
    Assert.Equal(SessionState.Executing, evt.ToState);
}

[Fact]
public void SessionEvent_RecordsReason()
{
    const string reason = "User initiated execution";
    var evt = new SessionEvent(SessionState.Planning, SessionState.Executing, reason, DateTimeOffset.UtcNow);
    Assert.Equal(reason, evt.Reason);
}

[Fact]
public void SessionEvent_RecordsTimestamp()
{
    var now = DateTimeOffset.UtcNow;
    var evt = new SessionEvent(SessionState.Planning, SessionState.Executing, "Test", now);
    Assert.Equal(now, evt.Timestamp);
}
```

2. **GREEN: Implement SessionEvent**
```csharp
public sealed record SessionEvent(
    SessionState FromState,
    SessionState ToState,
    string Reason,
    DateTimeOffset Timestamp
)
{
    public SessionEvent(SessionState fromState, SessionState toState, string reason, DateTimeOffset timestamp)
        : this(fromState, toState, reason, timestamp)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be empty", nameof(reason));
    }
}
```

3. **REFACTOR:** Add validation

**Mark Complete When:**
- [x] SessionEvent.cs created
- [x] All 6 tests passing
- [x] Properties immutable (record type)
- [x] Validation working (non-empty reason)

**Commit:** `feat(domain): implement SessionEvent for state transition audit trail`

---

## PHASE 5: Core Entity Classes (16 hours) - MOST COMPLEX

This is the heart of task-011a. Each entity is TDD'd completely before moving to the next.

### Work Item 5.1: Session Entity (4 hours)

**File:** `src/Acode.Domain/Sessions/Session.cs` (120 LOC)
**Spec Reference:** Implementation Prompt lines 3707-3760
**Acceptance Criteria:** AC-001-008, AC-042, AC-062-063, AC-067-070, AC-071-078, AC-079-084, AC-085-088, AC-089-094
**Effort:** 4 hours, 120 LOC
**Tests:** 20 comprehensive tests

**TDD Steps (Abbreviated - each step is full RED-GREEN-REFACTOR cycle):**

1. **RED-GREEN Cycle 1: Construction**
```csharp
[Fact]
public void Session_CanBeCreated()
{
    var session = Session.Create("Implement feature X");
    Assert.NotEqual(SessionId.Empty, session.Id);
    Assert.Equal(SessionState.Created, session.State);
    Assert.Equal("Implement feature X", session.TaskDescription);
}
```

2. **RED-GREEN Cycle 2: AddTask**
```csharp
[Fact]
public void AddTask_AddsTaskToCollection()
{
    var session = Session.Create("Implement feature");
    var task = session.AddTask("Step 1", "Do X");
    Assert.Contains(task, session.Tasks);
    Assert.Equal(1, session.Tasks.Count);
}

[Fact]
public void AddTask_AssignsCorrectOrder()
{
    var session = Session.Create("Implement feature");
    var task1 = session.AddTask("Step 1", null);
    var task2 = session.AddTask("Step 2", null);
    Assert.Equal(0, task1.Order);
    Assert.Equal(1, task2.Order);
}
```

3. **RED-GREEN Cycle 3: State Transitions**
```csharp
[Fact]
public void Transition_FromCreatedToPlanning_Works()
{
    var session = Session.Create("Implement feature");
    session.Transition(SessionState.Planning, "User analyzed task");
    Assert.Equal(SessionState.Planning, session.State);
}

[Fact]
public void Transition_InvalidPath_Throws()
{
    var session = Session.Create("Implement feature");
    Assert.Throws<InvalidOperationException>(() =>
        session.Transition(SessionState.Completed, "Invalid transition")
    );
}

[Fact]
public void Transition_RecordsEvent()
{
    var session = Session.Create("Implement feature");
    session.Transition(SessionState.Planning, "Analyze");
    Assert.Equal(1, session.Events.Count);
    Assert.Equal(SessionState.Created, session.Events[0].FromState);
    Assert.Equal(SessionState.Planning, session.Events[0].ToState);
}
```

4. **RED-GREEN Cycle 4: State Derivation**
```csharp
[Fact]
public void State_DerivesFromTasks()
{
    var session = Session.Create("Implement");
    session.Transition(SessionState.Planning, "Start planning");
    session.Transition(SessionState.AwaitingApproval, "Ready for review");
    session.Transition(SessionState.Executing, "User approved");

    var task = session.AddTask("Task 1", null);
    task.Start(); // Task now InProgress

    var derivedState = session.DeriveState();
    Assert.Equal(SessionState.Executing, derivedState);
    // If all tasks complete, should be Completed
}
```

5. **RED-GREEN Cycle 5: Serialization**
```csharp
[Fact]
public void Session_SerializesAndDeserializes()
{
    var original = Session.Create("Implement feature");
    original.Transition(SessionState.Planning, "User analyzed");

    var json = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<Session>(json);

    Assert.NotNull(deserialized);
    Assert.Equal(original.Id, deserialized.Id);
    Assert.Equal(original.TaskDescription, deserialized.TaskDescription);
    Assert.Equal(original.State, deserialized.State);
    Assert.Equal(original.Events.Count, deserialized.Events.Count);
}
```

6. **RED-GREEN Cycle 6: Equality**
```csharp
[Fact]
public void Session_Equal_WhenIdsMatch()
{
    var session1 = new Session(new SessionId(Guid.NewGuid()), "Test", SessionState.Created, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    var session2 = new Session(session1.Id, "Different desc", SessionState.Planning, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    Assert.Equal(session1, session2); // Equal by ID only
}
```

**Implementation Template:**
```csharp
public sealed class Session : EntityBase<SessionId>
{
    private readonly List<SessionTask> _tasks = new();
    private readonly List<SessionEvent> _events = new();

    public string TaskDescription { get; }
    public SessionState State { get; private set; }
    public IReadOnlyList<SessionTask> Tasks => _tasks.AsReadOnly();
    public IReadOnlyList<SessionEvent> Events => _events.AsReadOnly();
    public JsonDocument? Metadata { get; set; }

    private Session(SessionId id, string taskDescription, SessionState state,
        DateTimeOffset createdAt, DateTimeOffset updatedAt)
        : base(id)
    {
        TaskDescription = taskDescription ?? throw new ArgumentNullException(nameof(taskDescription));
        State = state;
    }

    public static Session Create(string taskDescription)
        => new(SessionId.Create(), taskDescription, SessionState.Created, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    public SessionTask AddTask(string title, string? description)
    {
        var task = new SessionTask(TaskId.Create(), Id, title, description, TaskState.Pending, _tasks.Count);
        _tasks.Add(task);
        MarkUpdated();
        return task;
    }

    public void Transition(SessionState newState, string reason)
    {
        if (!IsValidTransition(State, newState))
            throw new InvalidOperationException($"Cannot transition from {State} to {newState}");

        _events.Add(new SessionEvent(State, newState, reason, DateTimeOffset.UtcNow));
        State = newState;
        MarkUpdated();
    }

    public SessionState DeriveState()
    {
        if (_tasks.Count == 0) return State;

        var taskStates = _tasks.Select(t => t.DeriveState()).ToList();
        if (taskStates.Any(s => s == TaskState.InProgress)) return SessionState.Executing;
        if (taskStates.All(s => s == TaskState.Completed)) return SessionState.Completed;
        if (taskStates.Any(s => s == TaskState.Failed)) return SessionState.Failed;
        return State;
    }

    private static bool IsValidTransition(SessionState from, SessionState to)
        => (from, to) switch
        {
            (SessionState.Created, SessionState.Planning) => true,
            (SessionState.Planning, SessionState.AwaitingApproval) => true,
            (SessionState.AwaitingApproval, SessionState.Executing) => true,
            (SessionState.Executing, SessionState.Completed) => true,
            (SessionState.Executing, SessionState.Failed) => true,
            (_, SessionState.Cancelled) => true,
            _ => false
        };

    public override bool Equals(object? obj) => obj is Session other && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}
```

**Tests (20 total):**
- Construction (2 tests)
- AddTask (3 tests)
- State transitions (4 tests)
- State derivation (2 tests)
- Events recording (2 tests)
- Serialization (2 tests)
- Equality (2 tests)
- Collections (1 test)

**Mark Complete When:**
- [x] Session.cs created (120 LOC)
- [x] All 20 tests written and passing
- [x] Proper encapsulation (internal _tasks, public IReadOnlyList)
- [x] State transitions validated
- [x] Events recorded on transition
- [x] Serialization round-trip working

**Commit:** `feat(domain): implement Session aggregate root entity`

---

### Work Item 5.2-5.5: SessionTask, Step, ToolCall, Artifact

**Similar TDD process to Session, but each simpler:**

**SessionTask (Task) - 3.5 hours, 100 LOC, 15 tests**
- Manages Steps collection
- Derives state from Steps
- Properties: Id, SessionId, Title, Description, State, Order, Steps collection
- Key methods: AddStep, Start, Complete, Fail, Skip, DeriveState

**Step - 3 hours, 90 LOC, 12 tests**
- Manages ToolCalls collection
- Properties: Id, TaskId, Name, Description, State, Order, ToolCalls collection
- Key methods: AddToolCall, Start, Complete, Fail, Skip, DeriveState

**ToolCall - 4 hours, 110 LOC, 12 tests**
- **CRITICAL:** This is Run execution ToolCall, NOT Conversation ToolCall
- Manages Artifacts collection
- Properties: Id, StepId, ToolName, Parameters (JSON), State, Result (JSON), ErrorMessage, CompletedAt (nullable)
- Key methods: Execute, Succeed, Fail, Cancel, AddArtifact
- Security: JSON parameter validation

**Artifact - 5 hours, 150 LOC, 12 tests**
- **CRITICAL SECURITY:** Implements Security Threat 3 (Injection Prevention)
- Immutable after creation (sealed class)
- Properties: Id, ToolCallId, Type, Name, Content (bytes), ContentHash (SHA256), ContentType (MIME), Size
- Static factory: Create(...) with full validation
- Validation rules:
  - Content size ≤ 10 MB (FR-080)
  - MIME type whitelist (FR-079)
  - SQL injection detection regex (Security Threat 3)
  - Script injection detection regex (Security Threat 3)
  - File path traversal prevention (Security Threat 3)
  - Unicode normalization

**Total Phase 5:**
- 16 hours
- 500 LOC
- 71 tests (20 + 15 + 12 + 12 + 12 for entities)

**Commits (one per entity):**
- Commit 1: SessionTask entity
- Commit 2: Step entity
- Commit 3: ToolCall entity (note: Run execution, not Conversation)
- Commit 4: Artifact entity with security validation

---

## PHASE 6: JSON Serialization Configuration (1 hour)

### Work Item 6.1: Serialization Context

**File:** `src/Acode.Domain/Sessions/RunSessionJsonSerializerContext.cs`
**Effort:** 1 hour, 50 LOC
**Acceptance Criteria:** AC-067-070
**Tests:** 5 (serialization, deserialization, round-trip, camelCase, enum-as-string)

**TDD:**

1. **RED: Test camelCase serialization**
```csharp
[Fact]
public void Session_SerializesWithCamelCasePropertyNames()
{
    var session = Session.Create("Test");
    var json = JsonSerializer.Serialize(session, GetSerializerOptions());
    Assert.Contains("\"taskDescription\"", json);
    Assert.DoesNotContain("\"TaskDescription\"", json);
}

[Fact]
public void SessionState_SerializesAsString()
{
    var session = Session.Create("Test");
    var json = JsonSerializer.Serialize(session, GetSerializerOptions());
    Assert.Contains("\"state\": \"Created\"", json);
    Assert.DoesNotContain("\"state\": 0", json);
}
```

2. **GREEN: Implement serialization options**
```csharp
[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(SessionTask))]
[JsonSerializable(typeof(Step))]
[JsonSerializable(typeof(ToolCall))]
[JsonSerializable(typeof(Artifact))]
[JsonSerializable(typeof(SessionEvent))]
[JsonSerializable(typeof(SessionState))]
[JsonSerializable(typeof(TaskState))]
[JsonSerializable(typeof(StepState))]
[JsonSerializable(typeof(ToolCallState))]
[JsonSerializable(typeof(ArtifactType))]
public partial class RunSessionJsonSerializerContext : JsonSerializerContext
{
    public static JsonSerializerOptions GetOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            },
            WriteIndented = false
        };
        return options;
    }
}
```

**Mark Complete When:**
- [x] RunSessionJsonSerializerContext created
- [x] All 5 serialization tests passing
- [x] camelCase property naming verified
- [x] Enums serialize as strings
- [x] Round-trip (serialize → deserialize) preserves all data

**Commit:** `feat(domain): implement JSON serialization for run session entities`

---

## PHASE 7: Comprehensive Test Coverage (20 hours)

After all entities implemented, add comprehensive test suites:

### Test Suites to Create/Complete

**SessionTests.cs (20+ tests)**
- Construction validation
- AddTask behavior and ordering
- State transitions (all valid paths)
- Invalid transition rejection
- Event recording
- State derivation from tasks
- Serialization round-trip
- Equality semantics
- Collection immutability

**SessionTaskTests.cs (15+ tests)**
- Similar patterns to Session
- Derives state from Steps
- AddStep behavior

**StepTests.cs (12+ tests)**
- Derives state from ToolCalls
- AddToolCall behavior

**ToolCallTests.cs (12+ tests)**
- Execute → Succeed flow
- Execute → Fail flow
- Timestamp tracking (CompletedAt)
- Result/ErrorMessage handling

**ArtifactTests.cs (12+ tests)**
- **CRITICAL SECURITY:** Injection prevention tests
- SQL injection patterns rejected
- Script injection patterns rejected
- File path traversal rejected
- MIME type validation
- Size limits enforced
- Hash computation verified
- Content immutability

**ValueObjectTests.cs (12+ tests)**
- All ID types tested
- Equality verified
- Hash codes correct

**SerializationTests.cs (10+ tests)**
- Round-trip for each entity
- camelCase properties verified
- Enum serialization verified
- Nested object serialization

**StateTransitionTests.cs (8+ tests)**
- Session state machine
- Task state machine
- Step state machine
- ToolCall state machine
- Invalid transitions rejected

**Total:** 95+ comprehensive unit tests covering all 94 ACs

**Running Full Test Suite:**
```bash
dotnet test tests/Acode.Domain.Tests/Sessions/ --verbosity normal
dotnet test tests/Acode.Domain.Tests/Tasks/ --verbosity normal
dotnet test tests/Acode.Domain.Tests/Steps/ --verbosity normal
dotnet test tests/Acode.Domain.Tests/ToolCalls/ --verbosity normal
dotnet test tests/Acode.Domain.Tests/Artifacts/ --verbosity normal
dotnet test tests/Acode.Domain.Tests/Common/ --verbosity normal
dotnet test tests/Acode.Domain.Tests/Serialization/ --verbosity normal

# Combined
dotnet test tests/Acode.Domain.Tests/ --filter "FullyQualifiedName~*Session*|*Task*|*Step*|*ToolCall*|*Artifact*" --verbosity normal
```

**Expected Results:**
- ✅ 95+ tests passing
- ✅ 0 errors, 0 warnings
- ✅ 94 Acceptance Criteria verified

---

## FINAL VERIFICATION CHECKLIST

When ALL phases complete, verify:

- [ ] All 880 LOC of production code written (Phase 1-6)
- [ ] All 95+ unit tests written and passing (Phase 7)
- [ ] `dotnet build` → 0 errors, 0 warnings
- [ ] `dotnet test` → All tests passing, 0 failures
- [ ] 94/94 Acceptance Criteria implemented
- [ ] All entities have proper validation
- [ ] All state transitions guarded with InvalidOperationException
- [ ] All collections exposed as IReadOnlyList<T>
- [ ] All entities implement IEquatable<T> correctly
- [ ] All entities implement GetHashCode() correctly
- [ ] JSON serialization uses camelCase property names
- [ ] JSON serialization uses string for enums
- [ ] Round-trip serialization is lossless
- [ ] UUID v7 generation works with collision prevention
- [ ] Artifact security validation prevents SQL injection
- [ ] Artifact security validation prevents script injection
- [ ] Artifact security validation prevents path traversal
- [ ] SessionEvent is append-only (immutable record)
- [ ] State derivation logic is correct
- [ ] All public APIs have XML documentation
- [ ] Design follows Clean Architecture
- [ ] No external dependencies (pure domain)

---

## GIT WORKFLOW

**Branch:** `feature/task-011-validator-system` (same branch as task-010)

**Commits (in order):**

**Phase 1: Foundation**
1. `feat(domain): implement EntityId abstract base class`
2. `feat(domain): implement SecureIdGenerator with UUID v7 collision prevention`
3. `feat(domain): implement EntityBase<TId> generic base class`

**Phase 2: Enums**
4. `feat(domain): implement state enums (SessionState, TaskState, StepState, ToolCallState)`
5. `feat(domain): implement ArtifactType enum`

**Phase 3: Value Objects**
6. `feat(domain): implement entity ID value objects (SessionId, TaskId, StepId, ToolCallId, ArtifactId)`

**Phase 4: Events**
7. `feat(domain): implement SessionEvent for state transition audit trail`

**Phase 5: Entities**
8. `feat(domain): implement Session aggregate root entity`
9. `feat(domain): implement SessionTask entity`
10. `feat(domain): implement Step entity`
11. `feat(domain): implement ToolCall entity (run execution, NOT conversation)`
12. `feat(domain): implement Artifact entity with security validation`

**Phase 6: Serialization**
13. `feat(domain): implement JSON serialization for run session entities`

**Phase 7: Tests**
14. `test(domain): add comprehensive unit tests for all entities (95+ tests)`

**Final**
15. `docs(task-011a): update completion checklist with verification evidence`

---

## SUCCESS CRITERIA

Task-011a is COMPLETE when:

- [x] All 94 Acceptance Criteria implemented and tested
- [x] 880+ LOC of production code
- [x] 95+ unit tests, all passing
- [x] Build clean (0 errors, 0 warnings)
- [x] 100% semantic AC compliance verified
- [x] All security requirements implemented (artifact injection prevention)
- [x] All state transitions validated
- [x] All serialization tested with round-trip
- [x] Clean Architecture boundaries respected
- [x] No external dependencies
- [x] Commit created with descriptive message
- [x] Ready for code review

---

**Checklist Created:** 2026-01-15
**Total Estimated Time:** 42.75 hours
**Status:** Ready for Phase 1 implementation (foundation classes)
**Blocking Dependencies:** NONE - Can start immediately
