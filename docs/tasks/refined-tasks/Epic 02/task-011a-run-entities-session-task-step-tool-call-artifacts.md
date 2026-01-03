# Task 011.a: Run Entities (Session/Task/Step/Tool Call/Artifacts)

**Priority:** P0 – Critical Path  
**Tier:** Domain Model  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 011 (State Machine), Task 050 (Workspace DB)  

---

## Description

Task 011.a defines the core domain entities that represent agent execution: Session, Task, Step, ToolCall, and Artifact. These entities form the hierarchical structure that models how the agent plans and executes work. Clear, well-designed entities are foundational—every other component depends on them.

The entity hierarchy reflects the natural structure of agentic work. A Session represents a complete run initiated by a user command. Sessions contain Tasks, which represent high-level goals the agent must accomplish. Tasks contain Steps, which are discrete actions the agent takes. Steps contain ToolCalls, which are atomic operations like reading a file or writing code. This hierarchy enables progress tracking at any granularity.

Entity identity is globally unique and sync-safe. All entities use UUID v7 identifiers, which are sortable by creation time and guaranteed unique across systems. This design supports both local SQLite storage (authoritative offline) and remote PostgreSQL storage (authoritative when connected). Idempotent sync requires that IDs never collide.

Domain entities follow Clean Architecture principles. Entities live in the Domain layer with no dependencies on infrastructure. They encapsulate business rules and invariants. The Application layer orchestrates entities. The Infrastructure layer handles persistence. This separation enables testing entities in isolation and swapping storage implementations.

Sessions are the root aggregate. A Session owns its Tasks, Steps, and ToolCalls. All modifications to child entities go through the Session to maintain invariants. For example, a Task cannot complete until all its Steps complete. The Session enforces these rules.

Artifacts are outputs produced during execution. When the agent reads a file, the content is an artifact. When it writes code, the new content is an artifact. When it produces a diff, that's an artifact. Artifacts are versioned and immutable—once created, they don't change. This immutability supports undo, audit, and debugging.

Entity state derives from execution progress. A Task is "In Progress" if any of its Steps is executing. A Task is "Complete" when all Steps succeed. A Task is "Failed" if any Step fails irrecoverably. This derived state propagates up the hierarchy—Session state derives from Task states.

Timestamps track everything. Every entity has created_at and updated_at timestamps. Every state change has a timestamp. This temporal data enables timeline reconstruction, performance analysis, and debugging. Timestamps use UTC to avoid timezone complications.

Metadata enables extensibility. Entities have a metadata field for additional context. This might include model parameters, tool versions, or custom tags. Metadata is typed as JSON and validated against schema. This flexibility handles unforeseen requirements without schema changes.

Validation enforces data integrity. Entity constructors and setters validate inputs. Invalid data is rejected immediately with descriptive errors. Entities cannot enter invalid states. This fail-fast approach catches bugs early and simplifies debugging.

Serialization supports multiple formats. Entities serialize to JSON for API responses and events. They serialize to database records for persistence. They serialize to diff format for change tracking. The entity design considers all serialization needs.

Testing is comprehensive. Unit tests verify entity behavior in isolation. Each entity has tests for construction, validation, state transitions, and serialization. Test coverage targets 100% for domain entities—these are too important to leave gaps.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Session | Root entity for a complete agent run |
| Task | High-level goal within a session |
| Step | Discrete action within a task |
| ToolCall | Atomic operation within a step |
| Artifact | Output produced during execution |
| Entity | Domain object with identity |
| Aggregate | Cluster of entities with root |
| Aggregate Root | Entry point for aggregate access |
| Value Object | Object without identity |
| UUID v7 | Sortable unique identifier |
| ULID | Universally Unique Lexicographically Sortable ID |
| Invariant | Condition that must always hold |
| Domain Layer | Business logic layer |
| Serialization | Converting to storage format |
| Metadata | Extensible context data |

---

## Out of Scope

The following items are explicitly excluded from Task 011.a:

- **State machine logic** - Task 011 main
- **Persistence implementation** - Task 011.b
- **Resume behavior** - Task 011.c
- **Database schema** - Task 011.b
- **Sync logic** - Task 049/050
- **Entity versioning/migration** - Post-MVP
- **Entity archival** - Post-MVP
- **Entity deletion** - Soft delete only
- **Custom entity types** - Fixed set
- **Entity relationships beyond hierarchy** - Tree only

---

## Functional Requirements

### Session Entity

- FR-001: Session MUST have unique ID (UUID v7)
- FR-002: Session MUST have TaskDescription (string)
- FR-003: Session MUST have State (enum)
- FR-004: Session MUST have CreatedAt (DateTimeOffset)
- FR-005: Session MUST have UpdatedAt (DateTimeOffset)
- FR-006: Session MUST have Tasks collection
- FR-007: Session MUST have Events collection
- FR-008: Session MAY have Metadata (JSON)
- FR-009: Session MUST derive state from Tasks
- FR-010: Session ID MUST be globally unique

### Task Entity

- FR-011: Task MUST have unique ID (UUID v7)
- FR-012: Task MUST have SessionId (foreign key)
- FR-013: Task MUST have Title (string)
- FR-014: Task MUST have Description (string, optional)
- FR-015: Task MUST have State (enum)
- FR-016: Task MUST have Order (integer)
- FR-017: Task MUST have CreatedAt (DateTimeOffset)
- FR-018: Task MUST have UpdatedAt (DateTimeOffset)
- FR-019: Task MUST have Steps collection
- FR-020: Task MAY have Metadata (JSON)

### Step Entity

- FR-021: Step MUST have unique ID (UUID v7)
- FR-022: Step MUST have TaskId (foreign key)
- FR-023: Step MUST have Name (string)
- FR-024: Step MUST have Description (string, optional)
- FR-025: Step MUST have State (enum)
- FR-026: Step MUST have Order (integer)
- FR-027: Step MUST have CreatedAt (DateTimeOffset)
- FR-028: Step MUST have UpdatedAt (DateTimeOffset)
- FR-029: Step MUST have ToolCalls collection
- FR-030: Step MAY have Metadata (JSON)

### ToolCall Entity

- FR-031: ToolCall MUST have unique ID (UUID v7)
- FR-032: ToolCall MUST have StepId (foreign key)
- FR-033: ToolCall MUST have ToolName (string)
- FR-034: ToolCall MUST have Parameters (JSON)
- FR-035: ToolCall MUST have State (enum)
- FR-036: ToolCall MUST have Order (integer)
- FR-037: ToolCall MUST have CreatedAt (DateTimeOffset)
- FR-038: ToolCall MUST have CompletedAt (DateTimeOffset, nullable)
- FR-039: ToolCall MUST have Result (JSON, nullable)
- FR-040: ToolCall MAY have ErrorMessage (string)
- FR-041: ToolCall MUST have Artifacts collection

### Artifact Entity

- FR-042: Artifact MUST have unique ID (UUID v7)
- FR-043: Artifact MUST have ToolCallId (foreign key)
- FR-044: Artifact MUST have Type (enum)
- FR-045: Artifact MUST have Name (string)
- FR-046: Artifact MUST have Content (bytes or string)
- FR-047: Artifact MUST have ContentHash (SHA256)
- FR-048: Artifact MUST have ContentType (MIME type)
- FR-049: Artifact MUST have Size (bytes)
- FR-050: Artifact MUST have CreatedAt (DateTimeOffset)
- FR-051: Artifact MUST be immutable after creation

### Entity States

- FR-052: Session states: Created, Planning, AwaitingApproval, Executing, Paused, Completed, Failed, Cancelled
- FR-053: Task states: Pending, InProgress, Completed, Failed, Skipped
- FR-054: Step states: Pending, InProgress, Completed, Failed, Skipped
- FR-055: ToolCall states: Pending, Executing, Succeeded, Failed, Cancelled

### Artifact Types

- FR-056: FileContent type for file reads
- FR-057: FileWrite type for file writes
- FR-058: FileDiff type for changes
- FR-059: CommandOutput type for command results
- FR-060: ModelResponse type for LLM output
- FR-061: SearchResult type for search output

### State Derivation

- FR-062: Task derives InProgress if any Step is InProgress
- FR-063: Task derives Completed if all Steps Completed
- FR-064: Task derives Failed if any Step Failed
- FR-065: Session derives state from child Tasks similarly

### Validation

- FR-066: IDs MUST be valid UUID v7
- FR-067: Required strings MUST be non-empty
- FR-068: Order MUST be >= 0
- FR-069: Timestamps MUST be valid UTC
- FR-070: Metadata MUST be valid JSON if present
- FR-071: ContentHash MUST match Content

### Identity

- FR-072: UUIDs MUST be v7 (time-sortable)
- FR-073: IDs MUST be generated on creation
- FR-074: IDs MUST NOT be modifiable after creation
- FR-075: IDs MUST be safe for database keys

### Hierarchy

- FR-076: Session is aggregate root
- FR-077: Tasks belong to exactly one Session
- FR-078: Steps belong to exactly one Task
- FR-079: ToolCalls belong to exactly one Step
- FR-080: Artifacts belong to exactly one ToolCall

---

## Non-Functional Requirements

### Performance

- NFR-001: Entity creation MUST complete < 1ms
- NFR-002: State derivation MUST complete < 10ms
- NFR-003: Serialization MUST complete < 5ms per entity
- NFR-004: Memory per Session MUST be < 1MB typical

### Reliability

- NFR-005: Invalid input MUST throw immediately
- NFR-006: Entity state MUST be consistent
- NFR-007: Collections MUST be thread-safe for reads

### Security

- NFR-008: Artifacts MUST NOT store secrets directly
- NFR-009: Metadata MUST be validated
- NFR-010: ContentHash MUST prevent tampering

### Maintainability

- NFR-011: Entities MUST have no infrastructure dependencies
- NFR-012: Entities MUST be unit testable in isolation
- NFR-013: Entity behavior MUST be deterministic

### Compatibility

- NFR-014: JSON serialization MUST be stable
- NFR-015: Database mapping MUST preserve all fields
- NFR-016: ID format MUST be consistent across stores

---

## User Manual Documentation

### Overview

Run entities represent the structure of agent execution. Understanding these entities helps with debugging, monitoring, and extending Acode.

### Entity Hierarchy

```
Session
├── TaskDescription: "Add input validation"
├── State: Executing
├── Tasks[]
│   ├── Task: "Analyze existing code"
│   │   ├── State: Completed
│   │   └── Steps[]
│   │       ├── Step: "Read login form"
│   │       │   └── ToolCalls[]
│   │       │       └── ToolCall: read_file
│   │       │           └── Artifacts[]
│   │       │               └── Artifact: file content
│   │       └── Step: "Find validation points"
│   │           └── ToolCalls[]
│   │               └── ToolCall: semantic_search
│   │                   └── Artifacts[]
│   │                       └── Artifact: search results
│   └── Task: "Implement validation"
│       ├── State: InProgress
│       └── Steps[]
│           └── Step: "Add validators"
│               └── ToolCalls[]
│                   └── ToolCall: write_file
│                       └── Artifacts[]
│                           ├── Artifact: original content
│                           └── Artifact: new content
└── Events[]
```

### Entity Details

#### Session

The root entity representing a complete agent run:

```csharp
public sealed class Session
{
    public Guid Id { get; }
    public string TaskDescription { get; }
    public SessionState State { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }
    public IReadOnlyList<Task> Tasks { get; }
    public IReadOnlyList<SessionEvent> Events { get; }
    public JsonDocument? Metadata { get; }
}
```

#### Task

A high-level goal within a session:

```csharp
public sealed class SessionTask
{
    public Guid Id { get; }
    public Guid SessionId { get; }
    public string Title { get; }
    public string? Description { get; }
    public TaskState State { get; }
    public int Order { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }
    public IReadOnlyList<Step> Steps { get; }
    public JsonDocument? Metadata { get; }
}
```

#### Step

A discrete action within a task:

```csharp
public sealed class Step
{
    public Guid Id { get; }
    public Guid TaskId { get; }
    public string Name { get; }
    public string? Description { get; }
    public StepState State { get; }
    public int Order { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }
    public IReadOnlyList<ToolCall> ToolCalls { get; }
    public JsonDocument? Metadata { get; }
}
```

#### ToolCall

An atomic operation:

```csharp
public sealed class ToolCall
{
    public Guid Id { get; }
    public Guid StepId { get; }
    public string ToolName { get; }
    public JsonDocument Parameters { get; }
    public ToolCallState State { get; }
    public int Order { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? CompletedAt { get; }
    public JsonDocument? Result { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyList<Artifact> Artifacts { get; }
}
```

#### Artifact

An output from execution:

```csharp
public sealed class Artifact
{
    public Guid Id { get; }
    public Guid ToolCallId { get; }
    public ArtifactType Type { get; }
    public string Name { get; }
    public byte[] Content { get; }
    public string ContentHash { get; }
    public string ContentType { get; }
    public long Size { get; }
    public DateTimeOffset CreatedAt { get; }
}
```

### State Enums

```csharp
// Session states
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

// Task states
public enum TaskState
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Skipped
}

// Step states
public enum StepState
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Skipped
}

// ToolCall states
public enum ToolCallState
{
    Pending,
    Executing,
    Succeeded,
    Failed,
    Cancelled
}

// Artifact types
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

### CLI Examples

```bash
# View session structure
$ acode session show abc123 --tree
Session abc123
├── Task 1: Analyze existing code [Completed]
│   ├── Step 1: Read login form [Completed]
│   │   └── ToolCall: read_file [Succeeded]
│   └── Step 2: Find validation points [Completed]
│       └── ToolCall: semantic_search [Succeeded]
└── Task 2: Implement validation [InProgress]
    └── Step 1: Add validators [InProgress]
        └── ToolCall: write_file [Executing]

# View specific artifact
$ acode artifact show def456
Artifact: def456
Type: FileDiff
Name: src/validators.ts
Size: 1.2 KB
Created: 2024-01-15T10:35:00Z
Hash: sha256:abc123...

# List all artifacts for session
$ acode session artifacts abc123
ID          TYPE        NAME                    SIZE
art001      FileContent src/login.ts            4.5 KB
art002      SearchResult validation matches     1.2 KB
art003      FileDiff    src/validators.ts       0.8 KB
```

### ID Format

All entities use UUID v7:

```
0190d6a1-7b2c-7def-8a3e-b4c5d6e7f890
│        │    │    │    └── Random
│        │    │    └── Variant
│        │    └── Version (7)
│        └── Time (sortable)
└── Prefix
```

Benefits:
- Time-sortable (newer IDs sort higher)
- Globally unique (no coordination needed)
- Database-friendly (works as primary key)
- URL-safe (no special characters)

### Metadata Schema

Metadata is optional JSON with known shapes:

```json
// Session metadata
{
  "operating_mode": "local-only",
  "model_config": {
    "planner": "llama3.2:70b",
    "coder": "llama3.2:7b"
  }
}

// Task metadata
{
  "estimated_steps": 5,
  "priority": "high"
}

// Step metadata
{
  "retry_count": 0,
  "timeout_ms": 30000
}

// ToolCall metadata
{
  "tokens_used": 1500,
  "model_latency_ms": 2340
}

// Artifact metadata
{
  "encoding": "utf-8",
  "original_path": "src/login.ts"
}
```

### Best Practices

1. **Access through Session**: Always go through the aggregate root
2. **Check derived state**: Task/Session states reflect children
3. **Preserve artifacts**: They're immutable for audit
4. **Use metadata sparingly**: Only for truly optional data
5. **Validate early**: Constructor validation prevents bad data

---

## Acceptance Criteria

### Session Entity

- [ ] AC-001: UUID v7 ID generated
- [ ] AC-002: TaskDescription stored
- [ ] AC-003: State tracked
- [ ] AC-004: CreatedAt recorded
- [ ] AC-005: UpdatedAt maintained
- [ ] AC-006: Tasks collection works
- [ ] AC-007: Events collection works
- [ ] AC-008: Metadata optional

### Task Entity

- [ ] AC-009: UUID v7 ID generated
- [ ] AC-010: SessionId required
- [ ] AC-011: Title required
- [ ] AC-012: Description optional
- [ ] AC-013: State tracked
- [ ] AC-014: Order maintained
- [ ] AC-015: Timestamps recorded
- [ ] AC-016: Steps collection works

### Step Entity

- [ ] AC-017: UUID v7 ID generated
- [ ] AC-018: TaskId required
- [ ] AC-019: Name required
- [ ] AC-020: State tracked
- [ ] AC-021: Order maintained
- [ ] AC-022: Timestamps recorded
- [ ] AC-023: ToolCalls collection works

### ToolCall Entity

- [ ] AC-024: UUID v7 ID generated
- [ ] AC-025: StepId required
- [ ] AC-026: ToolName required
- [ ] AC-027: Parameters JSON valid
- [ ] AC-028: State tracked
- [ ] AC-029: CompletedAt nullable
- [ ] AC-030: Result nullable
- [ ] AC-031: ErrorMessage captured
- [ ] AC-032: Artifacts collection works

### Artifact Entity

- [ ] AC-033: UUID v7 ID generated
- [ ] AC-034: ToolCallId required
- [ ] AC-035: Type required
- [ ] AC-036: Name required
- [ ] AC-037: Content stored
- [ ] AC-038: ContentHash computed
- [ ] AC-039: ContentType set
- [ ] AC-040: Size calculated
- [ ] AC-041: Immutable after creation

### States

- [ ] AC-042: All Session states work
- [ ] AC-043: All Task states work
- [ ] AC-044: All Step states work
- [ ] AC-045: All ToolCall states work
- [ ] AC-046: State derivation correct

### Artifact Types

- [ ] AC-047: FileContent type works
- [ ] AC-048: FileWrite type works
- [ ] AC-049: FileDiff type works
- [ ] AC-050: CommandOutput type works
- [ ] AC-051: ModelResponse type works
- [ ] AC-052: SearchResult type works

### Validation

- [ ] AC-053: Invalid IDs rejected
- [ ] AC-054: Empty strings rejected
- [ ] AC-055: Negative order rejected
- [ ] AC-056: Invalid JSON rejected
- [ ] AC-057: Hash mismatch detected

### Identity

- [ ] AC-058: UUID v7 format used
- [ ] AC-059: IDs generated on creation
- [ ] AC-060: IDs immutable
- [ ] AC-061: IDs database-safe

### Hierarchy

- [ ] AC-062: Session is aggregate root
- [ ] AC-063: Tasks belong to Session
- [ ] AC-064: Steps belong to Task
- [ ] AC-065: ToolCalls belong to Step
- [ ] AC-066: Artifacts belong to ToolCall

### Serialization

- [ ] AC-067: JSON serialization works
- [ ] AC-068: All fields serialized
- [ ] AC-069: Deserialization works
- [ ] AC-070: Round-trip preserves data

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Domain/Entities/
├── SessionTests.cs
│   ├── Should_Generate_UUIDv7_Id()
│   ├── Should_Require_TaskDescription()
│   ├── Should_Track_State()
│   ├── Should_Record_Timestamps()
│   ├── Should_Derive_State_From_Tasks()
│   └── Should_Serialize_To_JSON()
│
├── SessionTaskTests.cs
│   ├── Should_Generate_UUIDv7_Id()
│   ├── Should_Require_SessionId()
│   ├── Should_Require_Title()
│   ├── Should_Derive_State_From_Steps()
│   └── Should_Maintain_Order()
│
├── StepTests.cs
│   ├── Should_Generate_UUIDv7_Id()
│   ├── Should_Require_TaskId()
│   ├── Should_Require_Name()
│   ├── Should_Derive_State_From_ToolCalls()
│   └── Should_Maintain_Order()
│
├── ToolCallTests.cs
│   ├── Should_Generate_UUIDv7_Id()
│   ├── Should_Require_StepId()
│   ├── Should_Require_ToolName()
│   ├── Should_Track_Completion()
│   └── Should_Capture_Error()
│
└── ArtifactTests.cs
    ├── Should_Generate_UUIDv7_Id()
    ├── Should_Compute_ContentHash()
    ├── Should_Calculate_Size()
    ├── Should_Be_Immutable()
    └── Should_Validate_Hash_Matches()
```

### Integration Tests

```
Tests/Integration/Domain/
├── HierarchyTests.cs
│   ├── Should_Navigate_Session_To_Artifacts()
│   ├── Should_Derive_States_Correctly()
│   └── Should_Maintain_Referential_Integrity()
```

### E2E Tests

```
Tests/E2E/Domain/
├── EntityLifecycleTests.cs
│   ├── Should_Create_Full_Hierarchy()
│   └── Should_Persist_And_Retrieve()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Entity creation | 0.5ms | 1ms |
| State derivation | 5ms | 10ms |
| JSON serialization | 2ms | 5ms |
| Hash computation | 1ms/KB | 5ms/KB |

### Regression Tests

- Entity fields after schema change
- Serialization after format update
- State derivation after new states added

---

## User Verification Steps

### Scenario 1: Create Session

1. Create new Session via code
2. Verify: UUID v7 generated
3. Verify: CreatedAt set

### Scenario 2: Add Task

1. Add Task to Session
2. Verify: Task has SessionId
3. Verify: Session.Tasks contains Task

### Scenario 3: Add Step

1. Add Step to Task
2. Verify: Step has TaskId
3. Verify: Task.Steps contains Step

### Scenario 4: Add ToolCall

1. Add ToolCall to Step
2. Verify: ToolCall has StepId
3. Verify: Step.ToolCalls contains ToolCall

### Scenario 5: Create Artifact

1. Create Artifact from ToolCall
2. Verify: ContentHash computed
3. Verify: Size calculated

### Scenario 6: State Derivation

1. Complete all Steps in Task
2. Verify: Task.State is Completed
3. Complete all Tasks in Session
4. Verify: Session.State reflects completion

### Scenario 7: Validation Rejection

1. Try to create Session without TaskDescription
2. Verify: Exception thrown
3. Verify: Descriptive message

### Scenario 8: Artifact Immutability

1. Create Artifact
2. Try to modify Content
3. Verify: Modification prevented

### Scenario 9: JSON Serialization

1. Create full hierarchy
2. Serialize to JSON
3. Deserialize from JSON
4. Verify: All data preserved

### Scenario 10: Order Maintenance

1. Add multiple Tasks to Session
2. Verify: Order property maintained
3. Verify: Retrieval in order

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Sessions/
│   ├── Session.cs
│   ├── SessionId.cs
│   ├── SessionState.cs
│   └── SessionEvent.cs
│
├── Tasks/
│   ├── SessionTask.cs
│   ├── TaskId.cs
│   └── TaskState.cs
│
├── Steps/
│   ├── Step.cs
│   ├── StepId.cs
│   └── StepState.cs
│
├── ToolCalls/
│   ├── ToolCall.cs
│   ├── ToolCallId.cs
│   └── ToolCallState.cs
│
├── Artifacts/
│   ├── Artifact.cs
│   ├── ArtifactId.cs
│   └── ArtifactType.cs
│
└── Common/
    ├── EntityId.cs
    ├── EntityBase.cs
    └── JsonMetadata.cs
```

### EntityBase Class

```csharp
namespace AgenticCoder.Domain.Common;

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

### EntityId Base Class

```csharp
namespace AgenticCoder.Domain.Common;

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

### Session Entity

```csharp
namespace AgenticCoder.Domain.Sessions;

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

### Artifact Entity

```csharp
namespace AgenticCoder.Domain.Artifacts;

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

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-ENT-001 | Invalid entity ID |
| ACODE-ENT-002 | Required field missing |
| ACODE-ENT-003 | Invalid state transition |
| ACODE-ENT-004 | Invalid metadata JSON |
| ACODE-ENT-005 | Content hash mismatch |
| ACODE-ENT-006 | Invalid order value |

### Logging Fields

```json
{
  "event": "entity_created",
  "entity_type": "Session",
  "entity_id": "0190d6a1-7b2c-7def-8a3e-b4c5d6e7f890",
  "timestamp": "2024-01-15T10:30:00.123Z"
}
```

### Implementation Checklist

1. [ ] Create EntityBase abstract class
2. [ ] Create EntityId abstract class
3. [ ] Create SessionId value object
4. [ ] Create Session entity
5. [ ] Create SessionState enum
6. [ ] Create SessionEvent record
7. [ ] Create TaskId value object
8. [ ] Create SessionTask entity
9. [ ] Create TaskState enum
10. [ ] Create StepId value object
11. [ ] Create Step entity
12. [ ] Create StepState enum
13. [ ] Create ToolCallId value object
14. [ ] Create ToolCall entity
15. [ ] Create ToolCallState enum
16. [ ] Create ArtifactId value object
17. [ ] Create Artifact entity
18. [ ] Create ArtifactType enum
19. [ ] Implement state derivation
20. [ ] Implement validation
21. [ ] Implement JSON serialization
22. [ ] Write unit tests for all entities
23. [ ] Write hierarchy tests
24. [ ] Add XML documentation

### Validation Checklist Before Merge

- [ ] All entities have UUID v7 IDs
- [ ] All required fields validated
- [ ] All states enumerated
- [ ] State derivation works correctly
- [ ] Artifacts are immutable
- [ ] ContentHash matches Content
- [ ] JSON serialization round-trips
- [ ] No infrastructure dependencies
- [ ] Unit test coverage > 95%
- [ ] XML documentation complete

### Rollout Plan

1. **Phase 1:** Base classes (EntityBase, EntityId)
2. **Phase 2:** Session and Task entities
3. **Phase 3:** Step and ToolCall entities
4. **Phase 4:** Artifact entity
5. **Phase 5:** State derivation logic
6. **Phase 6:** Validation and serialization
7. **Phase 7:** Comprehensive tests

---

**End of Task 011.a Specification**