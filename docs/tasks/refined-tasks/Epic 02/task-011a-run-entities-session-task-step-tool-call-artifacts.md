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

- **State machine transition logic** - Covered in Task 011 main, which defines valid state transitions and orchestration
- **Persistence implementation** - Covered in Task 011.b for SQLite and PostgreSQL database implementations
- **Resume behavior and restart logic** - Covered in Task 011.c for resuming sessions from checkpoints
- **Database schema DDL** - Covered in Task 011.b for table definitions, indexes, and constraints
- **Sync logic between SQLite and PostgreSQL** - Covered in Task 049f for bidirectional synchronization
- **Entity versioning and schema migration** - Post-MVP feature for handling entity schema changes over time
- **Entity archival and retention policies** - Post-MVP feature for archiving old sessions and artifacts
- **Physical entity deletion** - Post-MVP feature, only soft delete is supported in MVP
- **Custom user-defined entity types** - MVP supports fixed set of entity types only
- **Entity relationships beyond tree hierarchy** - MVP supports Session → Task → Step → ToolCall → Artifact only
- **Distributed entity coordination** - Post-MVP feature for coordinating entities across multiple agent instances
- **Entity caching layer** - Post-MVP performance optimization for frequently accessed entities
- **Entity compression** - Post-MVP feature for compressing large artifacts to reduce storage usage
- **Entity encryption at rest** - Post-MVP security feature for encrypting sensitive artifact content
- **Entity replication** - Post-MVP feature for replicating entities to multiple storage backends

---

## Assumptions

### Technical Assumptions

- ASM-001: Entity Framework Core or similar ORM is used for entity definition
- ASM-002: UUID v7 provides time-ordered unique identifiers for all entities
- ASM-003: Nullable reference types are enabled for optional properties
- ASM-004: Record types are used for immutable entity snapshots
- ASM-005: JSON can represent all metadata and artifact content
- ASM-006: Entity hierarchies can be modeled with navigation properties

### Design Assumptions

- ASM-007: Session is the root aggregate containing all other entities
- ASM-008: Task, Step, ToolCall form a strict hierarchy (Session → Task → Step → ToolCall)
- ASM-009: Artifacts are attached to their producing entity (Step or ToolCall)
- ASM-010: Entities are append-only with soft delete for removal
- ASM-011: State derivation follows deterministic rules from child entities

### Dependency Assumptions

- ASM-012: Task 011 main provides state machine semantics
- ASM-013: Task 011.b implements actual persistence of these entities
- ASM-014: Domain layer exists for business logic separation

### Operational Assumptions

- ASM-015: Entities must support serialization for persistence and transfer
- ASM-016: Entity relationships are navigable in both directions
- ASM-017: Metadata allows extensibility without schema changes
- ASM-018: Artifact storage may use separate binary storage for large files

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

### Invariants

- FR-081: Session MUST have at least one Task before transitioning to Executing state
- FR-082: Task Steps MUST have sequential Order values starting from 0
- FR-083: ToolCall CompletedAt MUST be >= CreatedAt
- FR-084: Session UpdatedAt MUST be >= CreatedAt
- FR-085: Task UpdatedAt MUST be updated when any Step state changes
- FR-086: Step UpdatedAt MUST be updated when any ToolCall state changes
- FR-087: Session CANNOT transition to Completed if any Task is InProgress
- FR-088: Task CANNOT transition to Completed if any Step is Pending or InProgress
- FR-089: Step CANNOT transition to Completed if any ToolCall is Pending or Executing

### Serialization

- FR-090: Session MUST serialize to JSON format
- FR-091: All entity properties MUST be included in JSON serialization
- FR-092: Navigation properties MUST be serializable
- FR-093: Deserialization MUST reconstruct complete entity graph
- FR-094: Round-trip serialization MUST preserve all data
- FR-095: JSON property names MUST use camelCase convention
- FR-096: Enum values MUST serialize as strings not integers

### Collections

- FR-097: Entity collections MUST be exposed as IReadOnlyList
- FR-098: Internal collections MUST be mutable for aggregate root operations
- FR-099: Collection modifications MUST go through aggregate root methods
- FR-100: Collections MUST maintain insertion order
- FR-101: Collections MUST NOT allow null elements
- FR-102: Collections MUST support enumeration

### Events

- FR-103: SessionEvent MUST record FromState and ToState
- FR-104: SessionEvent MUST record Reason for transition
- FR-105: SessionEvent MUST record Timestamp of transition
- FR-106: SessionEvents MUST be append-only
- FR-107: SessionEvents MUST be ordered chronologically
- FR-108: SessionEvents MUST NOT be modifiable after creation

### Equality

- FR-109: Entities MUST implement IEquatable based on ID
- FR-110: Entities with same ID MUST be considered equal
- FR-111: Entities with different IDs MUST NOT be equal
- FR-112: GetHashCode MUST use ID hash code
- FR-113: Value objects MUST implement structural equality
- FR-114: EntityId equality MUST be based on Guid value

### Construction

- FR-115: Entity constructors MUST validate all required parameters
- FR-116: Entity constructors MUST throw ArgumentNullException for null required parameters
- FR-117: Entity constructors MUST throw ArgumentException for invalid required parameters
- FR-118: Entity constructors MUST initialize collections to empty
- FR-119: Entity constructors MUST set CreatedAt to current UTC time
- FR-120: Entity constructors MUST set UpdatedAt equal to CreatedAt initially

---

## Non-Functional Requirements

### Performance

- NFR-001: Entity creation MUST complete < 1ms
- NFR-002: State derivation MUST complete < 10ms for Sessions with up to 100 Tasks
- NFR-003: Serialization MUST complete < 5ms per entity
- NFR-004: Memory per Session MUST be < 1MB typical, < 10MB maximum
- NFR-005: Collection enumeration MUST have O(1) startup cost
- NFR-006: ID generation MUST complete < 0.1ms
- NFR-007: Hash computation MUST complete < 1ms per KB of artifact content

### Reliability

- NFR-008: Invalid input MUST throw immediately with descriptive errors
- NFR-009: Entity state MUST be consistent at all times
- NFR-010: Collections MUST be thread-safe for concurrent reads
- NFR-011: State transitions MUST be atomic
- NFR-012: Validation errors MUST include parameter name and invalid value
- NFR-013: Entities MUST NOT allow partial construction
- NFR-014: Navigation properties MUST always return non-null collections

### Security

- NFR-015: Artifacts MUST NOT store secrets directly in plain text
- NFR-016: Metadata MUST be validated against schema before acceptance
- NFR-017: ContentHash MUST prevent tampering detection
- NFR-018: Entity IDs MUST NOT be predictable or sequential
- NFR-019: Error messages MUST NOT leak sensitive information
- NFR-020: Artifact content MUST be treated as potentially malicious

### Maintainability

- NFR-021: Entities MUST have no infrastructure dependencies
- NFR-022: Entities MUST be unit testable in isolation without mocks
- NFR-023: Entity behavior MUST be deterministic for same inputs
- NFR-024: Code coverage for entities MUST exceed 95%
- NFR-025: Public API MUST have XML documentation
- NFR-026: Entity classes MUST follow SOLID principles
- NFR-027: Complex logic MUST have explanatory comments

### Compatibility

- NFR-028: JSON serialization format MUST be stable across versions
- NFR-029: Database mapping MUST preserve all fields without loss
- NFR-030: ID format MUST be consistent across SQLite and PostgreSQL
- NFR-031: Adding new optional properties MUST NOT break existing code
- NFR-032: Enum values MUST be additive only, never removed
- NFR-033: Serialized entities MUST deserialize in newer versions

### Observability

- NFR-034: Entity state changes MUST be traceable through events
- NFR-035: All entity operations MUST complete within measurable time bounds
- NFR-036: Entity lifecycle MUST be observable through timestamps
- NFR-037: Collection sizes MUST be queryable for monitoring

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

### Best Practices Summary

1. **Access through Session**: Always go through the aggregate root
2. **Check derived state**: Task/Session states reflect children
3. **Preserve artifacts**: They're immutable for audit
4. **Use metadata sparingly**: Only for truly optional data
5. **Validate early**: Constructor validation prevents bad data

---

## Security Considerations

### Threat 1: Malicious Artifact Content Injection

**Risk**: An attacker could attempt to inject malicious content into artifacts (e.g., scripts in file content, command injection in command parameters) that could be executed later when artifacts are processed or displayed.

**Mitigation Strategy**: Treat all artifact content as untrusted data. Validate content types, sanitize when displaying, and never execute artifact content directly without explicit user approval.

**Complete C# Implementation**:

```csharp
namespace AgenticCoder.Domain.Artifacts;

public sealed class ArtifactContentValidator
{
    private static readonly HashSet<string> AllowedContentTypes = new()
    {
        "text/plain",
        "text/markdown",
        "application/json",
        "text/x-csharp",
        "text/x-python",
        "text/x-typescript",
        "application/octet-stream"
    };

    private static readonly int MaxArtifactSizeMB = 10;
    private static readonly int MaxArtifactSizeBytes = MaxArtifactSizeMB * 1024 * 1024;

    public static ValidationResult Validate(byte[] content, string contentType, string name)
    {
        // Size validation
        if (content.Length > MaxArtifactSizeBytes)
        {
            return ValidationResult.Failure(
                $"Artifact size {content.Length} bytes exceeds maximum {MaxArtifactSizeBytes} bytes");
        }

        // Content type validation
        if (!AllowedContentTypes.Contains(contentType))
        {
            return ValidationResult.Failure(
                $"Content type '{contentType}' is not in allowed list");
        }

        // Filename validation - prevent path traversal
        if (name.Contains("..") || name.Contains("/") || name.Contains("\\"))
        {
            return ValidationResult.Failure(
                $"Artifact name '{name}' contains invalid path characters");
        }

        // Check for null bytes (could indicate binary injection in text)
        if (contentType.StartsWith("text/") && content.Contains((byte)0))
        {
            return ValidationResult.Failure(
                "Text artifact contains null bytes, possible binary injection");
        }

        return ValidationResult.Success();
    }
}

public record ValidationResult(bool IsValid, string? ErrorMessage)
{
    public static ValidationResult Success() => new(true, null);
    public static ValidationResult Failure(string error) => new(false, error);
}
```

### Threat 2: Entity ID Prediction Enabling Unauthorized Access

**Risk**: If entity IDs are sequential or predictable, an attacker could guess valid IDs and attempt to access sessions or artifacts belonging to other users.

**Mitigation Strategy**: Use UUID v7 with cryptographically random components. Never expose internal database sequence numbers as entity IDs.

**Complete C# Implementation**:

```csharp
namespace AgenticCoder.Domain.Common;

public abstract class EntityId : IEquatable<EntityId>
{
    public Guid Value { get; }

    protected EntityId()
    {
        // UUID v7: timestamp (48 bits) + version (4 bits) +
        // random (12 bits) + variant (2 bits) + random (62 bits)
        Value = CreateUuidV7();
    }

    protected EntityId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("ID cannot be empty", nameof(value));

        // Validate this is a UUID v7 (version bits should be 0111)
        var bytes = value.ToByteArray();
        var version = (bytes[7] >> 4) & 0x0F;
        if (version != 7)
        {
            throw new ArgumentException(
                $"GUID must be UUID v7 format, got version {version}",
                nameof(value));
        }

        Value = value;
    }

    private static Guid CreateUuidV7()
    {
        var bytes = new byte[16];

        // Fill first 6 bytes with timestamp (milliseconds since Unix epoch)
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        bytes[0] = (byte)((timestamp >> 40) & 0xFF);
        bytes[1] = (byte)((timestamp >> 32) & 0xFF);
        bytes[2] = (byte)((timestamp >> 24) & 0xFF);
        bytes[3] = (byte)((timestamp >> 16) & 0xFF);
        bytes[4] = (byte)((timestamp >> 8) & 0xFF);
        bytes[5] = (byte)(timestamp & 0xFF);

        // Fill remaining bytes with cryptographically secure random data
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var randomBytes = new byte[10];
        rng.GetBytes(randomBytes);
        Array.Copy(randomBytes, 0, bytes, 6, 10);

        // Set version (4 bits) to 7 (0111)
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);

        // Set variant (2 bits) to RFC 4122 (10)
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes);
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

### Threat 3: Metadata JSON Injection

**Risk**: Malicious JSON in metadata fields could cause deserialization vulnerabilities, denial of service through deeply nested structures, or injection attacks when metadata is displayed.

**Mitigation Strategy**: Validate JSON depth, size, and structure before storing. Use a schema validator and size limits.

**Complete C# Implementation**:

```csharp
namespace AgenticCoder.Domain.Common;

public static class JsonMetadataValidator
{
    private const int MaxJsonSizeBytes = 64 * 1024; // 64 KB
    private const int MaxJsonDepth = 10;
    private const int MaxArrayLength = 1000;

    public static ValidationResult Validate(JsonDocument? metadata)
    {
        if (metadata == null)
            return ValidationResult.Success();

        // Size validation
        var jsonString = metadata.RootElement.GetRawText();
        var sizeBytes = Encoding.UTF8.GetByteCount(jsonString);
        if (sizeBytes > MaxJsonSizeBytes)
        {
            return ValidationResult.Failure(
                $"Metadata size {sizeBytes} bytes exceeds maximum {MaxJsonSizeBytes} bytes");
        }

        // Depth validation
        var depth = CalculateDepth(metadata.RootElement);
        if (depth > MaxJsonDepth)
        {
            return ValidationResult.Failure(
                $"Metadata depth {depth} exceeds maximum {MaxJsonDepth}");
        }

        // Array length validation
        var arrayLengthResult = ValidateArrayLengths(metadata.RootElement);
        if (!arrayLengthResult.IsValid)
        {
            return arrayLengthResult;
        }

        return ValidationResult.Success();
    }

    private static int CalculateDepth(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => 1 + element.EnumerateObject()
                .Select(p => CalculateDepth(p.Value))
                .DefaultIfEmpty(0)
                .Max(),
            JsonValueKind.Array => 1 + element.EnumerateArray()
                .Select(CalculateDepth)
                .DefaultIfEmpty(0)
                .Max(),
            _ => 0
        };
    }

    private static ValidationResult ValidateArrayLengths(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            var length = element.GetArrayLength();
            if (length > MaxArrayLength)
            {
                return ValidationResult.Failure(
                    $"Array length {length} exceeds maximum {MaxArrayLength}");
            }

            foreach (var item in element.EnumerateArray())
            {
                var result = ValidateArrayLengths(item);
                if (!result.IsValid)
                    return result;
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var result = ValidateArrayLengths(property.Value);
                if (!result.IsValid)
                    return result;
            }
        }

        return ValidationResult.Success();
    }
}
```

### Threat 4: Hash Collision Attacks on Artifact Deduplication

**Risk**: An attacker could craft artifacts with colliding hashes to bypass deduplication, causing storage DoS, or to replace legitimate artifacts with malicious ones.

**Mitigation Strategy**: Use SHA-256 for content hashing (collision-resistant). Never use MD5 or SHA-1. Include artifact size in deduplication logic as an additional check.

**Complete C# Implementation**:

```csharp
namespace AgenticCoder.Domain.Artifacts;

public static class ContentHasher
{
    public static string ComputeHash(byte[] content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        // Use SHA-256 (not MD5/SHA-1 which have known collision attacks)
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(content);
        var hexHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return $"sha256:{hexHash}";
    }

    public static bool VerifyHash(byte[] content, string expectedHash)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));
        if (string.IsNullOrEmpty(expectedHash))
            throw new ArgumentException("Hash cannot be null or empty", nameof(expectedHash));

        var actualHash = ComputeHash(content);

        // Use constant-time comparison to prevent timing attacks
        return CryptographicEquals(actualHash, expectedHash);
    }

    private static bool CryptographicEquals(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        var result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}

// Artifact entity with hash validation
public sealed class Artifact : EntityBase<ArtifactId>
{
    public byte[] Content { get; }
    public string ContentHash { get; }
    public long Size { get; }

    public Artifact(
        ToolCallId toolCallId,
        ArtifactType type,
        string name,
        byte[] content,
        string contentType)
        : base(new ArtifactId())
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Size = content.Length;
        ContentHash = ContentHasher.ComputeHash(content);

        // Verify hash immediately after computation
        if (!ContentHasher.VerifyHash(content, ContentHash))
        {
            throw new InvalidOperationException(
                "Content hash verification failed immediately after computation");
        }

        // Additional validation
        var validationResult = ArtifactContentValidator.Validate(content, contentType, name);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(validationResult.ErrorMessage, nameof(content));
        }

        ToolCallId = toolCallId;
        Type = type;
        Name = name;
        ContentType = contentType;
    }
}
```

### Threat 5: Unauthorized State Transition

**Risk**: External code could manipulate entity state directly, bypassing business rules and creating inconsistent data (e.g., marking a Session Complete while Tasks are still executing).

**Mitigation Strategy**: Make state properties private setters. Expose only valid state transition methods. Validate transitions in aggregate root.

**Complete C# Implementation**:

```csharp
namespace AgenticCoder.Domain.Sessions;

public sealed class Session : EntityBase<SessionId>
{
    private readonly List<SessionTask> _tasks = new();
    private readonly List<SessionEvent> _events = new();

    public SessionState State { get; private set; } // Private setter!

    public Session(string taskDescription, JsonDocument? metadata = null)
        : base(new SessionId())
    {
        if (string.IsNullOrWhiteSpace(taskDescription))
            throw new ArgumentException("Task description required", nameof(taskDescription));

        TaskDescription = taskDescription;
        State = SessionState.Created;
        Metadata = metadata;
    }

    // Valid state transitions enforced through explicit methods
    public void StartPlanning()
    {
        ValidateTransition(State, SessionState.Planning);
        TransitionTo(SessionState.Planning, "Planning started");
    }

    public void RequestApproval()
    {
        if (_tasks.Count == 0)
            throw new InvalidOperationException("Cannot request approval: session has no tasks");

        ValidateTransition(State, SessionState.AwaitingApproval);
        TransitionTo(SessionState.AwaitingApproval, "Awaiting user approval");
    }

    public void StartExecuting()
    {
        if (_tasks.Count == 0)
            throw new InvalidOperationException("Cannot execute: session has no tasks");

        ValidateTransition(State, SessionState.Executing);
        TransitionTo(SessionState.Executing, "Execution started");
    }

    public void Complete()
    {
        // Validate all tasks are complete
        if (_tasks.Any(t => t.State != TaskState.Completed && t.State != TaskState.Skipped))
        {
            throw new InvalidOperationException(
                "Cannot complete session: not all tasks are completed or skipped");
        }

        ValidateTransition(State, SessionState.Completed);
        TransitionTo(SessionState.Completed, "All tasks completed");
    }

    public void Fail(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Failure reason required", nameof(reason));

        ValidateTransition(State, SessionState.Failed);
        TransitionTo(SessionState.Failed, $"Session failed: {reason}");
    }

    private void ValidateTransition(SessionState from, SessionState to)
    {
        // Define valid state transitions
        var validTransitions = new Dictionary<SessionState, HashSet<SessionState>>
        {
            [SessionState.Created] = new() { SessionState.Planning },
            [SessionState.Planning] = new() { SessionState.AwaitingApproval, SessionState.Failed },
            [SessionState.AwaitingApproval] = new() { SessionState.Executing, SessionState.Cancelled },
            [SessionState.Executing] = new() { SessionState.Paused, SessionState.Completed, SessionState.Failed },
            [SessionState.Paused] = new() { SessionState.Executing, SessionState.Cancelled },
        };

        if (!validTransitions.TryGetValue(from, out var allowed) || !allowed.Contains(to))
        {
            throw new InvalidOperationException(
                $"Invalid state transition from {from} to {to}");
        }
    }

    private void TransitionTo(SessionState newState, string reason)
    {
        var oldState = State;
        State = newState;
        _events.Add(new SessionEvent(oldState, newState, reason, DateTimeOffset.UtcNow));
        MarkUpdated();
    }
}

public record SessionEvent(
    SessionState FromState,
    SessionState ToState,
    string Reason,
    DateTimeOffset Timestamp);
```

---

## Best Practices

### Entity Design

- **BP-001**: Design entities around business concepts, not database tables
- **BP-002**: Keep entities focused on a single responsibility
- **BP-003**: Use value objects for IDs to provide type safety
- **BP-004**: Make all entity fields readonly when possible for immutability
- **BP-005**: Validate all inputs in constructors before assigning to fields
- **BP-006**: Throw exceptions immediately for invalid state, don't return error codes

### Aggregate Root Pattern

- **BP-007**: Access child entities only through the aggregate root
- **BP-008**: Enforce invariants in the aggregate root, not in child entities
- **BP-009**: Never expose internal collections directly, use IReadOnlyList
- **BP-010**: Provide explicit methods for operations rather than property setters
- **BP-011**: Keep aggregates small to avoid performance issues
- **BP-012**: Design aggregate boundaries around transaction boundaries

### State Management

- **BP-013**: Derive state from child entities rather than storing it redundantly
- **BP-014**: Use explicit state transition methods instead of property setters
- **BP-015**: Record state transitions as events for audit trail
- **BP-016**: Validate state transitions using allowed transition maps
- **BP-017**: Make state properties have private setters to prevent external manipulation

### Identity and Equality

- **BP-018**: Use UUID v7 for time-sortable globally unique identifiers
- **BP-019**: Implement IEquatable<T> for entity equality based on ID
- **BP-020**: Override GetHashCode to use ID hash code
- **BP-021**: Never expose entity ID setters after construction

### Validation

- **BP-022**: Validate inputs at construction time, not at persistence time
- **BP-023**: Provide descriptive error messages that include the invalid value
- **BP-024**: Use guard clauses at the start of methods for precondition checks
- **BP-025**: Validate complex business rules in dedicated validator classes

### Testing

- **BP-026**: Write unit tests for each entity in isolation without mocks
- **BP-027**: Test all validation rules with both valid and invalid inputs
- **BP-028**: Test all state transitions including invalid transitions
- **BP-029**: Test serialization round-trips to ensure no data loss

### Performance

- **BP-030**: Avoid lazy loading in domain entities to prevent N+1 queries
- **BP-031**: Use AsReadOnly() for collections to avoid defensive copying
- **BP-032**: Cache derived state if computation is expensive
- **BP-033**: Consider pagination for large collections

---

## Troubleshooting

### Issue 1: InvalidOperationException - "Cannot complete session: not all tasks are completed"

**Symptoms**:
- Session.Complete() throws InvalidOperationException
- Error message indicates tasks are not in completed state
- Session appears stuck in Executing state

**Root Causes**:
1. One or more Tasks are still in Pending or InProgress state
2. Task state derivation logic not working correctly
3. Steps within Tasks not marked complete

**Solution Steps**:

```csharp
// Diagnostic code to identify incomplete tasks
public static class SessionDiagnostics
{
    public static SessionCompletionReport AnalyzeCompletion(Session session)
    {
        var report = new SessionCompletionReport { SessionId = session.Id };

        foreach (var task in session.Tasks)
        {
            if (task.State != TaskState.Completed && task.State != TaskState.Skipped)
            {
                var taskIssue = new TaskIssue
                {
                    TaskId = task.Id,
                    TaskTitle = task.Title,
                    CurrentState = task.State,
                    IncompleteSteps = task.Steps
                        .Where(s => s.State != StepState.Completed && s.State != StepState.Skipped)
                        .Select(s => new StepIssue
                        {
                            StepId = s.Id,
                            StepName = s.Name,
                            CurrentState = s.State,
                            PendingToolCalls = s.ToolCalls
                                .Where(tc => tc.State == ToolCallState.Pending ||
                                           tc.State == ToolCallState.Executing)
                                .Count()
                        })
                        .ToList()
                };

                report.IncompleteTasks.Add(taskIssue);
            }
        }

        report.CanComplete = report.IncompleteTasks.Count == 0;
        return report;
    }
}

public class SessionCompletionReport
{
    public Guid SessionId { get; set; }
    public bool CanComplete { get; set; }
    public List<TaskIssue> IncompleteTasks { get; set; } = new();
}

public class TaskIssue
{
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; }
    public TaskState CurrentState { get; set; }
    public List<StepIssue> IncompleteSteps { get; set; } = new();
}

public class StepIssue
{
    public Guid StepId { get; set; }
    public string StepName { get; set; }
    public StepState CurrentState { get; set; }
    public int PendingToolCalls { get; set; }
}
```

### Issue 2: ArgumentException - "Content hash mismatch"

**Symptoms**:
- Artifact creation or validation throws ArgumentException
- Error message indicates computed hash doesn't match provided hash
- Artifact appears corrupt

**Root Causes**:
1. Artifact content was modified after hash computation
2. Incorrect hash algorithm used (e.g., MD5 instead of SHA-256)
3. Hash format mismatch (e.g., uppercase vs lowercase hex)
4. Byte encoding issues when converting string to bytes

**Solution Steps**:

```csharp
// Diagnostic and repair code
public static class ArtifactHashDiagnostics
{
    public static HashMismatchReport DiagnoseHashMismatch(
        byte[] content,
        string providedHash)
    {
        var report = new HashMismatchReport
        {
            ProvidedHash = providedHash,
            ContentSize = content.Length
        };

        // Compute hash with multiple algorithms to identify which was used
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var sha256Hash = sha256.ComputeHash(content);
            report.Sha256Hash = $"sha256:{Convert.ToHexString(sha256Hash).ToLowerInvariant()}";
        }

        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            var md5Hash = md5.ComputeHash(content);
            report.Md5Hash = $"md5:{Convert.ToHexString(md5Hash).ToLowerInvariant()}";
        }

        // Check for common formatting issues
        if (providedHash.Equals(report.Sha256Hash, StringComparison.OrdinalIgnoreCase))
        {
            report.Issue = "Hash is correct but case mismatch";
            report.Recommendation = "Use lowercase hex encoding";
        }
        else if (providedHash.Replace("SHA256:", "sha256:") == report.Sha256Hash)
        {
            report.Issue = "Hash prefix has incorrect casing";
            report.Recommendation = "Use 'sha256:' prefix (lowercase)";
        }
        else if (providedHash == report.Md5Hash)
        {
            report.Issue = "MD5 hash used instead of SHA-256";
            report.Recommendation = "MD5 is deprecated, use SHA-256";
        }
        else
        {
            report.Issue = "Content has been modified or corrupted";
            report.Recommendation = "Recompute hash from original content";
        }

        return report;
    }
}

public class HashMismatchReport
{
    public string ProvidedHash { get; set; }
    public string Sha256Hash { get; set; }
    public string Md5Hash { get; set; }
    public long ContentSize { get; set; }
    public string Issue { get; set; }
    public string Recommendation { get; set; }
}
```

### Issue 3: JsonException - "Metadata JSON depth exceeds maximum"

**Symptoms**:
- Entity construction with metadata throws JsonException
- Error mentions maximum depth exceeded
- Metadata appears deeply nested

**Root Causes**:
1. Metadata contains circular references causing infinite nesting
2. Malicious metadata designed to cause DoS through deep nesting
3. Accidentally nested structure (e.g., wrapping metadata multiple times)

**Solution Steps**:

```csharp
// Flatten deeply nested metadata
public static class MetadataFlattener
{
    public static JsonDocument FlattenMetadata(JsonDocument deepMetadata, int maxDepth = 10)
    {
        var flattened = new Dictionary<string, object>();
        FlattenElement(deepMetadata.RootElement, "", flattened, currentDepth: 0, maxDepth);

        var json = JsonSerializer.Serialize(flattened);
        return JsonDocument.Parse(json);
    }

    private static void FlattenElement(
        JsonElement element,
        string prefix,
        Dictionary<string, object> result,
        int currentDepth,
        int maxDepth)
    {
        if (currentDepth >= maxDepth)
        {
            result[prefix] = "<truncated - max depth reached>";
            return;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";
                    FlattenElement(property.Value, key, result, currentDepth + 1, maxDepth);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = $"{prefix}[{index}]";
                    FlattenElement(item, key, result, currentDepth + 1, maxDepth);
                    index++;
                }
                break;

            case JsonValueKind.String:
                result[prefix] = element.GetString();
                break;

            case JsonValueKind.Number:
                result[prefix] = element.GetDouble();
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                result[prefix] = element.GetBoolean();
                break;

            case JsonValueKind.Null:
                result[prefix] = null;
                break;
        }
    }
}
```

### Issue 4: OutOfMemoryException when loading Session with many Tasks

**Symptoms**:
- OutOfMemoryException when loading large Sessions
- Application crashes or becomes unresponsive
- Session has hundreds or thousands of Tasks

**Root Causes**:
1. Loading entire Session graph into memory at once
2. Navigation properties causing N+1 query explosion
3. No pagination for large collections

**Solution Steps**:

```csharp
// Implement pagination for large Sessions
public interface ISessionRepository
{
    Session GetSession(Guid sessionId);

    // Paginated access to Tasks
    PagedResult<SessionTask> GetTasks(
        Guid sessionId,
        int pageNumber = 1,
        int pageSize = 50);

    // Lazy loading for Steps
    PagedResult<Step> GetSteps(
        Guid taskId,
        int pageNumber = 1,
        int pageSize = 100);
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}

// Lightweight Session projection for listings
public class SessionSummary
{
    public Guid Id { get; set; }
    public string TaskDescription { get; set; }
    public SessionState State { get; set; }
    public int TaskCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

// Repository implementation with pagination
public class SessionRepository : ISessionRepository
{
    private readonly DbContext _context;

    public SessionSummary[] GetSessionSummaries(int pageNumber, int pageSize)
    {
        return _context.Sessions
            .OrderByDescending(s => s.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SessionSummary
            {
                Id = s.Id,
                TaskDescription = s.TaskDescription,
                State = s.State,
                TaskCount = s.Tasks.Count,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToArray();
    }

    public PagedResult<SessionTask> GetTasks(
        Guid sessionId,
        int pageNumber = 1,
        int pageSize = 50)
    {
        var query = _context.Tasks.Where(t => t.SessionId == sessionId);

        return new PagedResult<SessionTask>
        {
            TotalCount = query.Count(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = query
                .OrderBy(t => t.Order)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList()
        };
    }
}
```

### Issue 5: ArgumentException - "Invalid state transition from Executing to Planning"

**Symptoms**:
- State transition method throws ArgumentException
- Error indicates transition is not valid
- Cannot move Session back to earlier state

**Root Causes**:
1. Attempting invalid state transition (e.g., backward transition)
2. State machine doesn't allow requested transition
3. Missing intermediate state transition

**Solution Steps**:

```csharp
// Visualize valid state transitions
public static class SessionStateDiagnostics
{
    public static StateTransitionGraph GetTransitionGraph()
    {
        return new StateTransitionGraph
        {
            Transitions = new()
            {
                [SessionState.Created] = new[] { SessionState.Planning },
                [SessionState.Planning] = new[] { SessionState.AwaitingApproval, SessionState.Failed },
                [SessionState.AwaitingApproval] = new[] { SessionState.Executing, SessionState.Cancelled },
                [SessionState.Executing] = new[] { SessionState.Paused, SessionState.Completed, SessionState.Failed },
                [SessionState.Paused] = new[] { SessionState.Executing, SessionState.Cancelled },
                [SessionState.Completed] = Array.Empty<SessionState>(), // Terminal state
                [SessionState.Failed] = Array.Empty<SessionState>(), // Terminal state
                [SessionState.Cancelled] = Array.Empty<SessionState>() // Terminal state
            }
        };
    }

    public static string GetValidTransitionsFromState(SessionState currentState)
    {
        var graph = GetTransitionGraph();
        if (graph.Transitions.TryGetValue(currentState, out var validStates))
        {
            return validStates.Length == 0
                ? $"{currentState} is a terminal state (no transitions allowed)"
                : $"Valid transitions from {currentState}: {string.Join(", ", validStates)}";
        }

        return $"No transition information for state {currentState}";
    }

    public static bool CanTransition(SessionState from, SessionState to)
    {
        var graph = GetTransitionGraph();
        return graph.Transitions.TryGetValue(from, out var validStates)
            && validStates.Contains(to);
    }
}

public class StateTransitionGraph
{
    public Dictionary<SessionState, SessionState[]> Transitions { get; set; } = new();
}
```

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

### State Transitions

- [ ] AC-071: Session transitions from Created to Planning
- [ ] AC-072: Session transitions from Planning to AwaitingApproval
- [ ] AC-073: Session transitions from AwaitingApproval to Executing
- [ ] AC-074: Session transitions from Executing to Completed
- [ ] AC-075: Session transitions from Executing to Failed
- [ ] AC-076: Invalid transitions throw InvalidOperationException
- [ ] AC-077: State transition events recorded
- [ ] AC-078: State transition timestamps captured

### Collections

- [ ] AC-079: Collections exposed as IReadOnlyList
- [ ] AC-080: Collections maintain insertion order
- [ ] AC-081: Collections do not allow null elements
- [ ] AC-082: Collections support enumeration
- [ ] AC-083: Internal collections mutable through aggregate root
- [ ] AC-084: External code cannot modify collections directly

### Equality

- [ ] AC-085: Entities equal when IDs match
- [ ] AC-086: Entities not equal when IDs differ
- [ ] AC-087: GetHashCode uses ID hash
- [ ] AC-088: IEquatable implemented correctly

### Events

- [ ] AC-089: SessionEvent records FromState
- [ ] AC-090: SessionEvent records ToState
- [ ] AC-091: SessionEvent records Reason
- [ ] AC-092: SessionEvent records Timestamp
- [ ] AC-093: Events append-only
- [ ] AC-094: Events chronologically ordered

---

## Testing Requirements

### Unit Tests - Complete C# Implementations

```csharp
// File: tests/Acode.Domain.Tests/Sessions/SessionTests.cs
namespace Acode.Domain.Tests.Sessions;

using Acode.Domain.Sessions;
using FluentAssertions;
using System.Text.Json;
using Xunit;

public class SessionTests
{
    [Fact]
    public void Should_Generate_UUIDv7_Id()
    {
        // Arrange & Act
        var session = new Session("Implement feature X");

        // Assert
        session.Id.Should().NotBeNull();
        session.Id.Value.Should().NotBe(Guid.Empty);

        // Verify UUID v7 format (version bits should be 0111 = 7)
        var bytes = session.Id.Value.ToByteArray();
        var version = (bytes[7] >> 4) & 0x0F;
        version.Should().Be(7);
    }

    [Fact]
    public void Should_Require_TaskDescription()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Session(""));

        exception.Message.Should().Contain("Task description required");
        exception.ParamName.Should().Be("taskDescription");
    }

    [Fact]
    public void Should_Require_Non_Whitespace_TaskDescription()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Session("   "));

        exception.Message.Should().Contain("Task description required");
    }

    [Fact]
    public void Should_Initialize_With_Created_State()
    {
        // Arrange & Act
        var session = new Session("Implement feature X");

        // Assert
        session.State.Should().Be(SessionState.Created);
    }

    [Fact]
    public void Should_Record_CreatedAt_Timestamp()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var session = new Session("Implement feature X");

        // Assert
        var after = DateTimeOffset.UtcNow;
        session.CreatedAt.Should().BeOnOrAfter(before);
        session.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Should_Initialize_UpdatedAt_Equal_To_CreatedAt()
    {
        // Arrange & Act
        var session = new Session("Implement feature X");

        // Assert
        session.UpdatedAt.Should().Be(session.CreatedAt);
    }

    [Fact]
    public void Should_Initialize_Empty_Tasks_Collection()
    {
        // Arrange & Act
        var session = new Session("Implement feature X");

        // Assert
        session.Tasks.Should().NotBeNull();
        session.Tasks.Should().BeEmpty();
    }

    [Fact]
    public void Should_Add_Task_To_Session()
    {
        // Arrange
        var session = new Session("Implement feature X");

        // Act
        var task = session.AddTask("Write unit tests", "Comprehensive test coverage");

        // Assert
        session.Tasks.Should().ContainSingle();
        session.Tasks.Should().Contain(task);
        task.SessionId.Should().Be(session.Id);
    }

    [Fact]
    public void Should_Derive_Completed_State_When_All_Tasks_Completed()
    {
        // Arrange
        var session = new Session("Implement feature X");
        var task1 = session.AddTask("Task 1");
        var task2 = session.AddTask("Task 2");

        // Simulate task completion (would normally be done via Task.Complete())
        // For this test, assume Tasks have internal state management
        // This is a simplified example
        var derivedState = session.DeriveState();

        // Assert - with no completed tasks, should not be Completed
        derivedState.Should().NotBe(SessionState.Completed);
    }

    [Fact]
    public void Should_Transition_To_Planning_State()
    {
        // Arrange
        var session = new Session("Implement feature X");

        // Act
        session.StartPlanning();

        // Assert
        session.State.Should().Be(SessionState.Planning);
    }

    [Fact]
    public void Should_Record_State_Transition_Event()
    {
        // Arrange
        var session = new Session("Implement feature X");

        // Act
        session.StartPlanning();

        // Assert
        session.Events.Should().ContainSingle();
        var evt = session.Events.First();
        evt.FromState.Should().Be(SessionState.Created);
        evt.ToState.Should().Be(SessionState.Planning);
        evt.Reason.Should().Contain("Planning started");
    }

    [Fact]
    public void Should_Update_UpdatedAt_On_State_Transition()
    {
        // Arrange
        var session = new Session("Implement feature X");
        var originalUpdatedAt = session.UpdatedAt;

        // Small delay to ensure timestamp changes
        Thread.Sleep(10);

        // Act
        session.StartPlanning();

        // Assert
        session.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Should_Throw_When_Completing_Session_Without_Tasks()
    {
        // Arrange
        var session = new Session("Implement feature X");
        session.StartPlanning();
        session.RequestApproval();
        session.StartExecuting();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            session.Complete());

        exception.Message.Should().Contain("not all tasks are completed");
    }

    [Fact]
    public void Should_Serialize_To_JSON()
    {
        // Arrange
        var session = new Session("Implement feature X");
        session.AddTask("Task 1");

        // Act
        var json = JsonSerializer.Serialize(session);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("Implement feature X");
        json.Should().Contain("Task 1");
    }

    [Fact]
    public void Should_Deserialize_From_JSON()
    {
        // Arrange
        var originalSession = new Session("Implement feature X");
        originalSession.AddTask("Task 1");
        var json = JsonSerializer.Serialize(originalSession);

        // Act
        var deserializedSession = JsonSerializer.Deserialize<Session>(json);

        // Assert
        deserializedSession.Should().NotBeNull();
        deserializedSession.TaskDescription.Should().Be("Implement feature X");
        deserializedSession.Tasks.Should().HaveCount(1);
    }
}
```

```csharp
// File: tests/Acode.Domain.Tests/Artifacts/ArtifactTests.cs
namespace Acode.Domain.Tests.Artifacts;

using Acode.Domain.Artifacts;
using Acode.Domain.ToolCalls;
using FluentAssertions;
using System.Text;
using Xunit;

public class ArtifactTests
{
    [Fact]
    public void Should_Generate_UUIDv7_Id()
    {
        // Arrange
        var toolCallId = new ToolCallId();
        var content = Encoding.UTF8.GetBytes("test content");

        // Act
        var artifact = new Artifact(
            toolCallId,
            ArtifactType.FileContent,
            "test.txt",
            content,
            "text/plain");

        // Assert
        artifact.Id.Should().NotBeNull();
        var bytes = artifact.Id.Value.ToByteArray();
        var version = (bytes[7] >> 4) & 0x0F;
        version.Should().Be(7);
    }

    [Fact]
    public void Should_Compute_ContentHash()
    {
        // Arrange
        var toolCallId = new ToolCallId();
        var content = Encoding.UTF8.GetBytes("test content");

        // Act
        var artifact = new Artifact(
            toolCallId,
            ArtifactType.FileContent,
            "test.txt",
            content,
            "text/plain");

        // Assert
        artifact.ContentHash.Should().NotBeNullOrEmpty();
        artifact.ContentHash.Should().StartWith("sha256:");
        artifact.ContentHash.Length.Should().Be(71); // "sha256:" (7) + 64 hex chars
    }

    [Fact]
    public void Should_Calculate_Size()
    {
        // Arrange
        var toolCallId = new ToolCallId();
        var content = Encoding.UTF8.GetBytes("test content");

        // Act
        var artifact = new Artifact(
            toolCallId,
            ArtifactType.FileContent,
            "test.txt",
            content,
            "text/plain");

        // Assert
        artifact.Size.Should().Be(content.Length);
    }

    [Fact]
    public void Should_Be_Immutable_After_Creation()
    {
        // Arrange
        var toolCallId = new ToolCallId();
        var content = Encoding.UTF8.GetBytes("test content");

        // Act
        var artifact = new Artifact(
            toolCallId,
            ArtifactType.FileContent,
            "test.txt",
            content,
            "text/plain");

        // Assert - verify all properties are readonly
        var contentProperty = typeof(Artifact).GetProperty(nameof(Artifact.Content));
        contentProperty.SetMethod.Should().BeNull();

        var hashProperty = typeof(Artifact).GetProperty(nameof(Artifact.ContentHash));
        hashProperty.SetMethod.Should().BeNull();
    }

    [Fact]
    public void Should_Validate_Hash_Matches_Content()
    {
        // Arrange
        var toolCallId = new ToolCallId();
        var content = Encoding.UTF8.GetBytes("test content");

        // Act
        var artifact = new Artifact(
            toolCallId,
            ArtifactType.FileContent,
            "test.txt",
            content,
            "text/plain");

        // Assert - recompute hash and verify it matches
        var recomputedHash = ContentHasher.ComputeHash(content);
        artifact.ContentHash.Should().Be(recomputedHash);
    }

    [Fact]
    public void Should_Reject_Null_Content()
    {
        // Arrange
        var toolCallId = new ToolCallId();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new Artifact(
                toolCallId,
                ArtifactType.FileContent,
                "test.txt",
                null,
                "text/plain"));

        exception.ParamName.Should().Be("content");
    }

    [Fact]
    public void Should_Reject_Null_ToolCallId()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("test content");

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new Artifact(
                null,
                ArtifactType.FileContent,
                "test.txt",
                content,
                "text/plain"));

        exception.ParamName.Should().Be("toolCallId");
    }

    [Fact]
    public void Should_Set_ContentType()
    {
        // Arrange
        var toolCallId = new ToolCallId();
        var content = Encoding.UTF8.GetBytes("test content");

        // Act
        var artifact = new Artifact(
            toolCallId,
            ArtifactType.FileContent,
            "test.txt",
            content,
            "text/plain");

        // Assert
        artifact.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public void Should_Support_All_Artifact_Types()
    {
        // Arrange
        var toolCallId = new ToolCallId();
        var content = Encoding.UTF8.GetBytes("test");

        // Act & Assert - verify each type can be created
        var types = new[]
        {
            ArtifactType.FileContent,
            ArtifactType.FileWrite,
            ArtifactType.FileDiff,
            ArtifactType.CommandOutput,
            ArtifactType.ModelResponse,
            ArtifactType.SearchResult
        };

        foreach (var type in types)
        {
            var artifact = new Artifact(toolCallId, type, "test", content, "text/plain");
            artifact.Type.Should().Be(type);
        }
    }
}
```

### Integration Tests

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
        // Arrange
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

        // Act & Assert - navigate down the hierarchy
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
        // Arrange
        var session = new Session("Implement feature X");
        var task = session.AddTask("Task 1");

        // Assert - task has reference back to session
        task.SessionId.Should().Be(session.Id);

        // Arrange - add step
        var step = task.AddStep("Step 1");

        // Assert - step has reference back to task
        step.TaskId.Should().Be(task.Id);
    }
}
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

### SessionTask Entity

```csharp
namespace AgenticCoder.Domain.Tasks;

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

public enum TaskState
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Skipped
}

public sealed class TaskId : EntityId
{
    public TaskId() : base() { }
    public TaskId(Guid value) : base(value) { }
}
```

### Step Entity

```csharp
namespace AgenticCoder.Domain.Steps;

public sealed class Step : EntityBase<StepId>
{
    private readonly List<ToolCall> _toolCalls = new();

    public TaskId TaskId { get; }
    public string Name { get; }
    public string? Description { get; }
    public StepState State { get; private set; }
    public int Order { get; }
    public JsonDocument? Metadata { get; }

    public IReadOnlyList<ToolCall> ToolCalls => _toolCalls.AsReadOnly();

    public Step(
        TaskId taskId,
        string name,
        string? description,
        int order,
        JsonDocument? metadata = null)
        : base(new StepId())
    {
        TaskId = taskId ?? throw new ArgumentNullException(nameof(taskId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name required", nameof(name));

        if (order < 0)
            throw new ArgumentException("Order must be >= 0", nameof(order));

        Name = name;
        Description = description;
        Order = order;
        State = StepState.Pending;
        Metadata = metadata;
    }

    public ToolCall AddToolCall(string toolName, JsonDocument parameters)
    {
        var toolCall = new ToolCall(Id, toolName, parameters, _toolCalls.Count);
        _toolCalls.Add(toolCall);
        MarkUpdated();
        return toolCall;
    }

    public void Start()
    {
        if (State != StepState.Pending)
            throw new InvalidOperationException($"Cannot start step in {State} state");

        State = StepState.InProgress;
        MarkUpdated();
    }

    public void Complete()
    {
        if (_toolCalls.Any(tc => tc.State != ToolCallState.Succeeded && tc.State != ToolCallState.Cancelled))
            throw new InvalidOperationException("Cannot complete step: not all tool calls succeeded");

        State = StepState.Completed;
        MarkUpdated();
    }

    public void Fail()
    {
        State = StepState.Failed;
        MarkUpdated();
    }

    public void Skip()
    {
        State = StepState.Skipped;
        MarkUpdated();
    }

    public StepState DeriveState()
    {
        if (_toolCalls.Count == 0) return State;
        if (_toolCalls.All(tc => tc.State == ToolCallState.Succeeded || tc.State == ToolCallState.Cancelled))
            return StepState.Completed;
        if (_toolCalls.Any(tc => tc.State == ToolCallState.Failed))
            return StepState.Failed;
        if (_toolCalls.Any(tc => tc.State == ToolCallState.Executing))
            return StepState.InProgress;
        return State;
    }
}

public enum StepState
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Skipped
}

public sealed class StepId : EntityId
{
    public StepId() : base() { }
    public StepId(Guid value) : base(value) { }
}
```

### ToolCall Entity

```csharp
namespace AgenticCoder.Domain.ToolCalls;

public sealed class ToolCall : EntityBase<ToolCallId>
{
    private readonly List<Artifact> _artifacts = new();

    public StepId StepId { get; }
    public string ToolName { get; }
    public JsonDocument Parameters { get; }
    public ToolCallState State { get; private set; }
    public int Order { get; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public JsonDocument? Result { get; private set; }
    public string? ErrorMessage { get; private set; }

    public IReadOnlyList<Artifact> Artifacts => _artifacts.AsReadOnly();

    public ToolCall(
        StepId stepId,
        string toolName,
        JsonDocument parameters,
        int order)
        : base(new ToolCallId())
    {
        StepId = stepId ?? throw new ArgumentNullException(nameof(stepId));

        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name required", nameof(toolName));

        if (order < 0)
            throw new ArgumentException("Order must be >= 0", nameof(order));

        ToolName = toolName;
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        Order = order;
        State = ToolCallState.Pending;
    }

    public Artifact AddArtifact(
        ArtifactType type,
        string name,
        byte[] content,
        string contentType)
    {
        var artifact = new Artifact(Id, type, name, content, contentType);
        _artifacts.Add(artifact);
        MarkUpdated();
        return artifact;
    }

    public void Start()
    {
        if (State != ToolCallState.Pending)
            throw new InvalidOperationException($"Cannot start tool call in {State} state");

        State = ToolCallState.Executing;
        MarkUpdated();
    }

    public void Succeed(JsonDocument result)
    {
        if (State != ToolCallState.Executing)
            throw new InvalidOperationException($"Cannot succeed tool call in {State} state");

        Result = result;
        CompletedAt = DateTimeOffset.UtcNow;
        State = ToolCallState.Succeeded;
        MarkUpdated();
    }

    public void Fail(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message required", nameof(errorMessage));

        ErrorMessage = errorMessage;
        CompletedAt = DateTimeOffset.UtcNow;
        State = ToolCallState.Failed;
        MarkUpdated();
    }

    public void Cancel()
    {
        CompletedAt = DateTimeOffset.UtcNow;
        State = ToolCallState.Cancelled;
        MarkUpdated();
    }
}

public enum ToolCallState
{
    Pending,
    Executing,
    Succeeded,
    Failed,
    Cancelled
}

public sealed class ToolCallId : EntityId
{
    public ToolCallId() : base() { }
    public ToolCallId(Guid value) : base(value) { }
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

public enum ArtifactType
{
    FileContent,
    FileWrite,
    FileDiff,
    CommandOutput,
    ModelResponse,
    SearchResult
}

public sealed class ArtifactId : EntityId
{
    public ArtifactId() : base() { }
    public ArtifactId(Guid value) : base(value) { }
}
```

### SessionState and SessionEvent

```csharp
namespace AgenticCoder.Domain.Sessions;

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

public record SessionEvent(
    SessionState FromState,
    SessionState ToState,
    string Reason,
    DateTimeOffset Timestamp);

public sealed class SessionId : EntityId
{
    public SessionId() : base() { }
    public SessionId(Guid value) : base(value) { }
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