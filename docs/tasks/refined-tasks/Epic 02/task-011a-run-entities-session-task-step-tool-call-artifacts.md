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

## Use Cases

### Use Case 1: Progress Tracking During Long-Running Session

**Actor:** Development Manager monitoring team's agent usage  
**Context:** Need to understand how far along a 2-hour refactoring session has progressed  
**Problem:** Without hierarchical entities, can only see "in progress" with no granular visibility

**Workflow Without Entity Hierarchy:**
1. Manager runs: `acode status`
2. Sees: "Session abc-123: Executing"
3. No indication of: How many tasks? How many complete? Where is it stuck?
4. Manager has no visibility into actual progress
5. Cannot estimate time remaining or identify bottlenecks
6. **Result: Blind operation, cannot plan around agent workload**

**Workflow With Entity Hierarchy:**
1. Manager runs: `acode status --detailed`
2. Sees hierarchical progress:
   ```
   Session abc-123: Executing
   ├─ Task 1: "Analyze codebase" [COMPLETED] (12 steps, 45 tool calls)
   ├─ Task 2: "Extract interfaces" [IN_PROGRESS] (8/15 steps complete)
   │  ├─ Step 1: "Read UserService.cs" [COMPLETED]
   │  ├─ Step 2: "Identify public methods" [COMPLETED]
   │  ...
   │  ├─ Step 8: "Generate IUserService interface" [COMPLETED]
   │  └─ Step 9: "Write interface file" [IN_PROGRESS] (2/3 tool calls)
   └─ Task 3: "Update references" [NOT_STARTED]
   ```
3. Manager immediately sees: 53% complete (1 of 3 tasks done, 8 of 15 steps in current task)
4. Can identify bottleneck: Step 9 taking longer than expected
5. Can estimate: ~45 minutes remaining based on current pace
6. **Result: Full visibility, can plan team activities around completion**

**Business Impact:**
- **Time savings:** Eliminates status check meetings (15 min/day × 220 days = 55 hours/year)
- **Planning accuracy:** Prevents context switching by knowing when agent finishes
- **Value:** 55 hours × $100/hour = **$5,500/year per manager**

---

### Use Case 2: Targeted Rollback After Partial Failure

**Actor:** Senior Developer whose agent session failed mid-execution  
**Context:** Agent completed 2 of 3 tasks before encountering error  
**Problem:** Without entity hierarchy, must redo all work or manually determine what succeeded

**Workflow Without Entity Hierarchy:**
1. Developer runs: `acode run "Refactor authentication module"`
2. Agent works for 45 minutes
3. Agent fails with error: "Cannot write to protected file"
4. Developer runs: `acode status`
5. Sees: "Session failed" with no detail on what completed
6. Developer manually inspects working directory to determine changes
7. Spends 20 minutes reconstructing what succeeded vs. what failed
8. Decides safest approach: Revert all changes, fix issue, re-run entire session
9. **Result: 45 minutes of work discarded, must restart from beginning**

**Workflow With Entity Hierarchy:**
1. Developer runs: `acode run "Refactor authentication module"`
2. Agent completes:
   - Task 1: "Extract auth logic to service" - **COMPLETED** (all 8 steps succeeded)
   - Task 2: "Add unit tests" - **COMPLETED** (all 6 steps succeeded)
   - Task 3: "Update documentation" - **FAILED** (failed at step 2: write to read-only docs folder)
3. Developer runs: `acode session show abc-123`
4. Sees clear breakdown:
   ```
   Session abc-123: FAILED
   ├─ Task 1 [COMPLETED]: 8/8 steps, 24 tool calls successful
   ├─ Task 2 [COMPLETED]: 6/6 steps, 18 tool calls successful
   └─ Task 3 [FAILED]: 1/4 steps complete
      ├─ Step 1: "Generate API docs" [COMPLETED]
      └─ Step 2: "Write to docs/" [FAILED] - Permission denied
   ```
5. Developer sees: Tasks 1 and 2 are solid, only Task 3 needs attention
6. Developer fixes permission issue: `chmod +w docs/`
7. Developer runs: `acode resume abc-123 --from-task 3`
8. Agent resumes, completes Task 3 in 5 minutes
9. **Result: Preserved 40 minutes of work, only re-ran failed portion**

**Business Impact:**
- **Time savings:** Prevents redundant work (average 30 min/failure × 12 failures/year = 6 hours/year)
- **Confidence:** Developers trust agent to resume cleanly, use it more frequently
- **Value:** 6 hours × $100/hour × 20 developers = **$12,000/year**

---

### Use Case 3: Audit Trail for Compliance

**Actor:** Security Auditor reviewing agent actions for compliance report  
**Context:** Need to verify agent didn't access restricted files or modify protected code  
**Problem:** Without detailed entity records, cannot prove what agent did/didn't do

**Workflow Without Entity Hierarchy:**
1. Auditor receives compliance request: "Prove agent didn't access customer PII during refactoring"
2. Checks high-level session logs: "Session completed successfully"
3. No granular record of which files were read, which were written
4. Cannot definitively prove PII files untouched
5. Must manually review all file changes in git history (200+ files)
6. Spends 8 hours reconstructing agent activity from git diffs and timestamps
7. **Result: High audit cost, cannot definitively prove negative (didn't access X)**

**Workflow With Entity Hierarchy:**
1. Auditor receives same compliance request
2. Runs: `acode session show abc-123 --tool-calls --filter "file:customers"`
3. Query returns: No tool calls involving "customers" directory or PII files
4. Verifies complete list of files accessed:
   ```
   Tool Calls in Session abc-123:
   - read_file("src/auth/LoginService.cs") [Step 1, Task 1]
   - read_file("src/auth/PasswordValidator.cs") [Step 2, Task 1]
   - write_file("src/auth/ILoginService.cs", content) [Step 5, Task 1]
   - run_command("dotnet test") [Step 8, Task 2]
   (42 tool calls total, 0 involving PII directories)
   ```
5. Exports tool call list to CSV for compliance report
6. Generates cryptographic hash of event log for tamper-evidence
7. **Result: 15 minutes to generate proof, cryptographically verifiable non-access**

**Business Impact:**
- **Audit cost reduction:** 8 hours → 15 minutes (47.5x faster)
- **Compliance confidence:** Cryptographic proof vs. manual reconstruction
- **Value:** 7.75 hours savings × $150/hour (auditor rate) × 4 audits/year = **$4,650/year**

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

## Security Considerations

### Threat 1: Entity State Tampering via Direct Database Access

**Attack Scenario:**  
Malicious insider or compromised backup script modifies session state directly in SQLite/Postgres database:

1. Attacker identifies running session with valuable work (e.g., refactoring authentication module)
2. Attacker opens database with `sqlite3` CLI or pgAdmin
3. Attacker runs: `UPDATE sessions SET state = 'COMPLETED' WHERE id = 'abc-123'`
4. Attacker modifies step completion: `UPDATE steps SET completed_at = NOW() WHERE step_number > actual_progress`
5. Agent resumes, believes work is complete when it's not
6. Incomplete refactoring deployed to production, security vulnerability introduced

**Impact Assessment:**
- **Data Integrity:** CRITICAL - Session state no longer reflects reality
- **Business Impact:** Code deployed with incomplete changes, potential production outages
- **Audit Trail:** Compromised - Cannot trust event log if state was manipulated
- **Blast Radius:** Single session, but could cascade if changes committed to version control

**Complete Mitigation (C#):**

```csharp
// Domain/Audit/EntityChecksum.cs
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Acode.Domain.Audit;

/// <summary>
/// Provides tamper-detection for entities via cryptographic checksums
/// </summary>
public static class EntityChecksum
{
    /// <summary>
    /// Compute SHA-256 checksum of entity's semantic state
    /// Includes all state-relevant fields, excludes audit fields like UpdatedAt
    /// </summary>
    public static string ComputeChecksum<T>(T entity) where T : class
    {
        // Serialize to stable JSON (sorted keys, no whitespace)
        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        
        // Compute SHA-256 hash
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }
    
    /// <summary>
    /// Verify entity checksum matches stored value
    /// </summary>
    public static bool VerifyChecksum<T>(T entity, string expectedChecksum) where T : class
    {
        var actualChecksum = ComputeChecksum(entity);
        return actualChecksum == expectedChecksum;
    }
}

// Domain/Run/Session.cs - Add checksum field
public partial class Session
{
    /// <summary>
    /// SHA-256 checksum of session state for tamper detection
    /// Recomputed on every state change
    /// </summary>
    public string StateChecksum { get; private set; } = string.Empty;
    
    /// <summary>
    /// Update session state and recompute checksum atomically
    /// </summary>
    public void UpdateState(SessionState newState)
    {
        State = newState;
        UpdatedAt = DateTimeOffset.UtcNow;
        
        // Recompute checksum after state change
        StateChecksum = EntityChecksum.ComputeChecksum(new
        {
            Id,
            State,
            Tasks = Tasks.Select(t => new { t.Id, t.State, t.Order }).ToList()
        });
    }
}

// Infrastructure/Persistence/SessionRepository.cs
public class SessionRepository : ISessionRepository
{
    public async Task<Session> GetByIdAsync(Guid sessionId)
    {
        var session = await _dbContext.Sessions
            .Include(s => s.Tasks)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
            
        if (session == null)
            throw new SessionNotFoundException(sessionId);
        
        // Verify checksum on load - detect tampering
        var expectedChecksum = session.StateChecksum;
        var actualChecksum = EntityChecksum.ComputeChecksum(new
        {
            session.Id,
            session.State,
            Tasks = session.Tasks.Select(t => new { t.Id, t.State, t.Order }).ToList()
        });
        
        if (actualChecksum != expectedChecksum)
        {
            _logger.LogError(
                "Session {SessionId} failed checksum validation. Expected: {Expected}, Actual: {Actual}. Possible tampering detected.",
                sessionId, expectedChecksum, actualChecksum);
                
            throw new EntityTamperedException(
                $"Session {sessionId} checksum mismatch. Database may have been modified directly.");
        }
        
        return session;
    }
    
    public async Task SaveAsync(Session session)
    {
        // Update checksum before save
        session.UpdateState(session.State);
        
        _dbContext.Sessions.Update(session);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation(
            "Session {SessionId} saved with checksum {Checksum}",
            session.Id, session.StateChecksum);
    }
}

// Application/Exceptions/EntityTamperedException.cs
public class EntityTamperedException : Exception
{
    public EntityTamperedException(string message) : base(message) { }
}
```

**Verification:**
```bash
# Test 1: Normal operation - checksum validates
dotnet test --filter "FullyQualifiedName~SessionChecksumTests.SaveAndLoad_ValidChecksum"

# Test 2: Tampering detection - checksum fails
dotnet test --filter "FullyQualifiedName~SessionChecksumTests.DirectDatabaseModification_DetectsTampering"

# Test 3: Performance - checksum computation < 1ms
dotnet test --filter "FullyQualifiedName~SessionChecksumTests.ChecksumComputation_MeetsPerformanceRequirement"
```

**Defense-in-Depth Layers:**
1. **Application-Level:** Checksum validation on every entity load
2. **Database-Level:** Row-level security policies restricting direct UPDATE
3. **Audit-Level:** Database trigger logs all direct modifications for forensics
4. **Infrastructure-Level:** Database credentials restricted to application service account only

---

### Threat 2: UUID Collision Leading to Entity Confusion

**Attack Scenario:**  
Attacker exploits weak UUID generation to create collision, causing entity confusion:

1. Attacker discovers Acode uses UUID v4 (random) instead of v7 (time-ordered)
2. Attacker generates 1 billion UUIDs using weak PRNG, finds collision with existing session
3. Attacker creates new session with colliding UUID
4. Database accepts second session (no unique constraint enforced)
5. Resume logic loads wrong session, executes attacker's malicious tasks
6. Attacker achieves arbitrary code execution via crafted tool calls

**Impact Assessment:**
- **Confidentiality:** HIGH - Attacker could read sensitive files via read_file tool calls
- **Integrity:** CRITICAL - Attacker could modify any file via write_file tool calls
- **Availability:** MEDIUM - Attacker could crash agent via malformed commands
- **Privilege Escalation:** Attacker's tasks execute with developer's permissions

**Complete Mitigation (C#):**

```csharp
// Domain/Common/SecureIdGenerator.cs
using System.Security.Cryptography;

namespace Acode.Domain.Common;

/// <summary>
/// Generates cryptographically secure UUID v7 identifiers
/// UUID v7: Time-ordered + cryptographically random
/// Format: [48-bit timestamp][4-bit version][12-bit sequence][2-bit variant][62-bit random]
/// </summary>
public static class SecureIdGenerator
{
    private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
    private static long _lastTimestamp = 0;
    private static int _sequence = 0;
    private static readonly object _lock = new object();
    
    /// <summary>
    /// Generate UUID v7 with guaranteed uniqueness via timestamp + sequence + random
    /// </summary>
    public static Guid NewId()
    {
        lock (_lock)
        {
            // Get Unix timestamp in milliseconds (48 bits)
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // Handle clock regression or same-millisecond generation
            if (now == _lastTimestamp)
            {
                _sequence++;
                if (_sequence > 4095) // 12-bit max
                {
                    // Sequence overflow - wait 1ms
                    Thread.Sleep(1);
                    now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    _sequence = 0;
                }
            }
            else if (now > _lastTimestamp)
            {
                _sequence = 0;
            }
            else
            {
                // Clock went backwards - use last timestamp + increment sequence
                now = _lastTimestamp;
                _sequence++;
            }
            
            _lastTimestamp = now;
            
            // Generate 62 bits of cryptographic randomness
            var randomBytes = new byte[8];
            _rng.GetBytes(randomBytes);
            
            // Construct UUID v7 bytes
            var bytes = new byte[16];
            
            // Bytes 0-5: 48-bit timestamp
            bytes[0] = (byte)((now >> 40) & 0xFF);
            bytes[1] = (byte)((now >> 32) & 0xFF);
            bytes[2] = (byte)((now >> 24) & 0xFF);
            bytes[3] = (byte)((now >> 16) & 0xFF);
            bytes[4] = (byte)((now >> 8) & 0xFF);
            bytes[5] = (byte)(now & 0xFF);
            
            // Bytes 6-7: 4-bit version (0111 = v7) + 12-bit sequence
            bytes[6] = (byte)(0x70 | ((_sequence >> 8) & 0x0F));
            bytes[7] = (byte)(_sequence & 0xFF);
            
            // Bytes 8-15: 2-bit variant (10) + 62-bit random
            bytes[8] = (byte)(0x80 | (randomBytes[0] & 0x3F));
            Array.Copy(randomBytes, 1, bytes, 9, 7);
            
            return new Guid(bytes);
        }
    }
    
    /// <summary>
    /// Verify ID is valid UUID v7 format
    /// </summary>
    public static bool IsValidUuidV7(Guid id)
    {
        var bytes = id.ToByteArray();
        
        // Check version bits (should be 0111 = v7)
        var version = (bytes[6] >> 4) & 0x0F;
        if (version != 0x07)
            return false;
        
        // Check variant bits (should be 10)
        var variant = (bytes[8] >> 6) & 0x03;
        if (variant != 0x02)
            return false;
        
        return true;
    }
    
    /// <summary>
    /// Extract timestamp from UUID v7 for ordering/debugging
    /// </summary>
    public static DateTimeOffset GetTimestamp(Guid uuidV7)
    {
        if (!IsValidUuidV7(uuidV7))
            throw new ArgumentException("Not a valid UUID v7", nameof(uuidV7));
        
        var bytes = uuidV7.ToByteArray();
        
        // Extract 48-bit timestamp
        long timestamp = 0;
        for (int i = 0; i < 6; i++)
        {
            timestamp = (timestamp << 8) | bytes[i];
        }
        
        return DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
    }
}

// Domain/Run/Session.cs - Use secure ID generation
public partial class Session
{
    private Session() { } // For EF Core
    
    public static Session Create(string prompt, string workingDirectory)
    {
        return new Session
        {
            Id = SecureIdGenerator.NewId(), // Use cryptographically secure UUID v7
            Prompt = prompt,
            WorkingDirectory = workingDirectory,
            State = SessionState.Planning,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Tasks = new List<Task>()
        };
    }
}

// Infrastructure/Persistence/Configurations/SessionConfiguration.cs
public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(s => s.Id);
        
        // Enforce unique constraint at database level
        builder.HasIndex(s => s.Id).IsUnique();
        
        // Validate UUID v7 format on insert
        builder.HasCheckConstraint(
            "CK_Session_ValidUuidV7",
            "LENGTH(CAST(Id AS TEXT)) = 36"); // Basic format check
        
        // Prevent duplicate IDs via unique index
        builder.ToTable("sessions");
    }
}
```

**Verification:**
```bash
# Test 1: UUID v7 format validation
dotnet test --filter "FullyQualifiedName~SecureIdGeneratorTests.NewId_GeneratesValidUuidV7"

# Test 2: No collisions in 1M generations
dotnet test --filter "FullyQualifiedName~SecureIdGeneratorTests.NewId_NoCollisionsInMillionGenerations"

# Test 3: Database unique constraint enforcement
dotnet test --filter "FullyQualifiedName~SessionRepositoryTests.SaveDuplicateId_ThrowsException"

# Test 4: Timestamp extraction accuracy
dotnet test --filter "FullyQualifiedName~SecureIdGeneratorTests.GetTimestamp_ReturnsAccurateTime"
```

**Collision Probability Analysis:**
- UUID v7 with 62-bit random component: 2^62 = 4.6 × 10^18 possible values
- Birthday paradox: 50% collision probability at 2^31 = 2.1 billion UUIDs
- Acode context: 1,000 sessions/day = 365,000/year = 3.65M over 10 years
- **Collision risk: Negligible (< 0.0001% over 10 years)**

---

### Threat 3: Artifact Injection via Malicious Tool Output

**Attack Scenario:**  
Attacker compromises external tool (e.g., language server) to inject malicious artifacts:

1. Developer uses Acode to refactor code, which invokes external LSP server
2. Attacker has compromised LSP server binary or MitM'd network connection
3. LSP server returns crafted response with embedded payload:
   ```json
   {
     "diagnostics": [
       {
         "message": "Syntax error",
         "code": "\"; DROP TABLE sessions; --"
       }
     ]
   }
   ```
4. Acode stores artifact without validation, including SQL injection payload
5. Later audit query displays artifacts: `SELECT * FROM artifacts WHERE tool_name = 'lsp'`
6. SQL injection executes, drops sessions table
7. All session history lost, cannot resume any work

**Impact Assessment:**
- **Data Integrity:** CRITICAL - Arbitrary SQL injection via artifact storage
- **Availability:** HIGH - Could drop tables, corrupt database
- **Audit Trail:** HIGH - Could delete event logs covering tracks
- **Lateral Movement:** Artifact could contain malicious code executed by other tools

**Complete Mitigation (C#):**

```csharp
// Domain/Run/Artifact.cs
using System.Text.RegularExpressions;

namespace Acode.Domain.Run;

public partial class Artifact
{
    private const int MaxContentSize = 10 * 1024 * 1024; // 10 MB
    private const int MaxFilePathLength = 1024;
    
    private static readonly Regex _sqlInjectionPattern = new Regex(
        @"('(''|[^'])*')|(;)|(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UPDATE|UNION( +ALL)?)\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex _scriptInjectionPattern = new Regex(
        @"<script[^>]*>.*?</script>|javascript:|onerror\s*=|onload\s*=",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    
    private static readonly string[] _allowedMimeTypes = new[]
    {
        "text/plain",
        "application/json",
        "text/markdown",
        "text/x-csharp",
        "text/x-python",
        "text/x-java",
        "application/xml"
    };
    
    private Artifact() { } // For EF Core
    
    /// <summary>
    /// Create artifact with comprehensive input validation
    /// Prevents injection attacks via untrusted tool output
    /// </summary>
    public static Artifact Create(
        Guid toolCallId,
        ArtifactType type,
        string mimeType,
        string content,
        string? filePath = null)
    {
        // Validation 1: Content size limit
        if (content.Length > MaxContentSize)
        {
            throw new ArgumentException(
                $"Artifact content exceeds maximum size of {MaxContentSize} bytes. " +
                $"Large outputs should be written to files, not stored as artifacts.",
                nameof(content));
        }
        
        // Validation 2: MIME type whitelist
        if (!_allowedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"MIME type '{mimeType}' not allowed. Permitted types: {string.Join(", ", _allowedMimeTypes)}",
                nameof(mimeType));
        }
        
        // Validation 3: SQL injection detection
        if (_sqlInjectionPattern.IsMatch(content))
        {
            throw new ArtifactValidationException(
                "Artifact content contains potential SQL injection patterns. Content rejected for security.");
        }
        
        // Validation 4: Script injection detection
        if (_scriptInjectionPattern.IsMatch(content))
        {
            throw new ArtifactValidationException(
                "Artifact content contains potential script injection patterns. Content rejected for security.");
        }
        
        // Validation 5: File path traversal prevention
        if (filePath != null)
        {
            if (filePath.Length > MaxFilePathLength)
            {
                throw new ArgumentException(
                    $"File path exceeds maximum length of {MaxFilePathLength}",
                    nameof(filePath));
            }
            
            if (filePath.Contains("..") || filePath.Contains("~"))
            {
                throw new ArtifactValidationException(
                    "File path contains directory traversal patterns (.., ~). Only absolute paths allowed.");
            }
            
            if (!Path.IsPathFullyQualified(filePath))
            {
                throw new ArtifactValidationException(
                    "File path must be absolute, not relative.");
            }
        }
        
        // Validation 6: Unicode normalization (prevent homograph attacks)
        var normalizedContent = content.Normalize(NormalizationForm.FormC);
        
        return new Artifact
        {
            Id = SecureIdGenerator.NewId(),
            ToolCallId = toolCallId,
            Type = type,
            MimeType = mimeType.ToLowerInvariant(),
            Content = normalizedContent,
            FilePath = filePath,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}

// Application/Exceptions/ArtifactValidationException.cs
public class ArtifactValidationException : Exception
{
    public ArtifactValidationException(string message) : base(message) { }
}

// Infrastructure/Persistence/Configurations/ArtifactConfiguration.cs
public class ArtifactConfiguration : IEntityTypeConfiguration<Artifact>
{
    public void Configure(EntityTypeBuilder<Artifact> builder)
    {
        builder.HasKey(a => a.Id);
        
        // Store content as parameterized BLOB, never as concatenated SQL
        builder.Property(a => a.Content)
            .HasColumnType("TEXT")
            .IsRequired();
        
        // Prevent injection via column name - use explicit mapping
        builder.ToTable("artifacts");
        
        builder.HasIndex(a => a.ToolCallId);
    }
}

// Infrastructure/Persistence/ArtifactRepository.cs
public class ArtifactRepository : IArtifactRepository
{
    public async Task SaveAsync(Artifact artifact)
    {
        // Use parameterized queries - EF Core handles this automatically
        _dbContext.Artifacts.Add(artifact);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation(
            "Artifact {ArtifactId} saved for ToolCall {ToolCallId}. " +
            "Content length: {Length}, MIME type: {MimeType}",
            artifact.Id, artifact.ToolCallId, artifact.Content.Length, artifact.MimeType);
    }
    
    public async Task<Artifact> GetByIdAsync(Guid artifactId)
    {
        // Read with parameterized query
        var artifact = await _dbContext.Artifacts
            .FirstOrDefaultAsync(a => a.Id == artifactId);
        
        if (artifact == null)
            throw new ArtifactNotFoundException(artifactId);
        
        // Sanitize content before returning (defense-in-depth)
        // Even though input validation prevented injection, re-sanitize on read
        artifact.SanitizeContentForDisplay();
        
        return artifact;
    }
}
```

**Verification:**
```bash
# Test 1: SQL injection patterns detected
dotnet test --filter "FullyQualifiedName~ArtifactTests.Create_SqlInjectionContent_ThrowsException"

# Test 2: Script injection patterns detected
dotnet test --filter "FullyQualifiedName~ArtifactTests.Create_ScriptInjectionContent_ThrowsException"

# Test 3: Path traversal prevented
dotnet test --filter "FullyQualifiedName~ArtifactTests.Create_PathTraversalAttack_ThrowsException"

# Test 4: Large content rejected
dotnet test --filter "FullyQualifiedName~ArtifactTests.Create_OversizedContent_ThrowsException"

# Test 5: Parameterized queries prevent injection
dotnet test --filter "FullyQualifiedName~ArtifactRepositoryTests.SaveMaliciousContent_DoesNotExecuteSql"
```

**Defense Strategy:**
1. **Input Validation:** Whitelist MIME types, detect injection patterns, size limits
2. **Parameterized Queries:** EF Core uses parameterized SQL, prevents concatenation injection
3. **Content Sanitization:** Unicode normalization, remove control characters
4. **Path Validation:** No traversal, only absolute paths, length limits
5. **Output Encoding:** Sanitize again on display (HTML entity encoding for web UI)

---

## Best Practices

### Entity Construction

**BP-001: Use Factory Methods, Not Public Constructors**
- **Reason:** Encapsulates validation, ensures invariants from creation
- **Example:** `Session.Create()` validates prompt is non-empty, sets initial state
- **Anti-pattern:** `new Session { Prompt = "" }` bypasses validation

**BP-002: Generate IDs at Creation, Not in Repository**
- **Reason:** Entity owns its identity, testable without database
- **Example:** `Session.Create()` calls `SecureIdGenerator.NewId()`
- **Anti-pattern:** `INSERT INTO sessions VALUES (UUID(), ...)` - ID not known until save

**BP-003: Make State Transitions Explicit Methods**
- **Reason:** Business logic in domain, not scattered in application layer
- **Example:** `session.Start()`, `session.Complete()`, `session.Fail()`
- **Anti-pattern:** `session.State = SessionState.Executing` - no validation

### Validation

**BP-004: Validate at Boundaries, Not Internally**
- **Reason:** Performance - validate once on input, trust internally
- **Example:** `Artifact.Create()` validates, internal methods assume valid
- **Anti-pattern:** Every property setter validates - slow, redundant

**BP-005: Fail Fast with Specific Exceptions**
- **Reason:** Clear error messages, easier debugging
- **Example:** `throw new ArtifactValidationException("SQL injection detected")`
- **Anti-pattern:** `return null` or generic `InvalidOperationException`

**BP-006: Use Value Objects for Complex Validation**
- **Reason:** Encapsulates rules, reusable across entities
- **Example:** `FilePath` value object validates traversal, length, format
- **Anti-pattern:** Duplicating path validation in Session, Task, Artifact

### Serialization

**BP-007: Version Your JSON Schema**
- **Reason:** Forward compatibility when adding fields
- **Example:** `{ "schemaVersion": "1.0", "sessionId": "..." }`
- **Anti-pattern:** Assume current structure forever - breaks on schema change

**BP-008: Use Stable Property Names (camelCase)**
- **Reason:** Consistent with JSON conventions, avoid surprises
- **Example:** `{ "createdAt": "2024-01-15T..." }`
- **Anti-pattern:** Mixed casing `{ "CreatedAt": ..., "session_id": ... }`

**BP-009: Exclude Audit Fields from Semantic Serialization**
- **Reason:** UpdatedAt changes don't affect business state
- **Example:** Checksum includes `Id, State, Tasks` but not `UpdatedAt`
- **Anti-pattern:** Checksum includes `UpdatedAt` - changes on every save

### Performance

**BP-010: Lazy-Load Collections, Eager-Load Aggregates**
- **Reason:** Balance performance vs. N+1 queries
- **Example:** `_dbContext.Sessions.Include(s => s.Tasks)` - aggregate loads together
- **Anti-pattern:** Load session, then loop loading tasks individually

**BP-011: Use Indexes on Foreign Keys**
- **Reason:** Fast lookups like "find all tasks for session"
- **Example:** `CREATE INDEX idx_tasks_session_id ON tasks(session_id)`
- **Anti-pattern:** Full table scan for every query

**BP-012: Batch Operations for Bulk Inserts**
- **Reason:** 100x faster than individual inserts
- **Example:** `_dbContext.AddRange(artifacts); await SaveChangesAsync()`
- **Anti-pattern:** `foreach (var a in artifacts) { Add(a); SaveChanges(); }`

### Testing

**BP-013: Test Entity Creation Validation**
- **Reason:** Invariants are contract, must be enforced
- **Example:** Test `Session.Create("")` throws `ArgumentException`
- **Anti-pattern:** Only test happy path

**BP-014: Test State Transition Rules**
- **Reason:** State machines are complex, easy to break
- **Example:** Test cannot transition `Completed -> Executing`
- **Anti-pattern:** Only test forward progression

**BP-015: Use In-Memory Database for Entity Tests**
- **Reason:** Fast, isolated, no setup required
- **Example:** `new DbContextOptionsBuilder().UseInMemoryDatabase()`
- **Anti-pattern:** Tests depend on shared SQLite file - flaky

### Security

**BP-016: Never Trust External Input, Even from Tools**
- **Reason:** Compromised tools can inject malicious data
- **Example:** Validate artifact content for injection patterns
- **Anti-pattern:** Store tool output directly without validation

**BP-017: Use Checksums for Critical State**
- **Reason:** Detects tampering, enables audit trail
- **Example:** Session checksum includes `State, Tasks, CreatedAt`
- **Anti-pattern:** No integrity verification - accept any database state

**BP-018: Sanitize Content for Display**
- **Reason:** Defense-in-depth against stored XSS
- **Example:** HTML-encode artifact content before rendering in UI
- **Anti-pattern:** Display raw content - vulnerable if validation bypassed

---

## Troubleshooting

### Problem 1: "Invalid entity state" Exception During Resume

**Symptoms:**
- Resume command fails with: `EntityStateException: Session abc-123 in invalid state 'Planning'`
- Session shows `State = Planning` but has completed tasks
- Cannot resume or cancel session

**Possible Causes:**
1. **State transition failed mid-update:** Application crashed after updating tasks but before updating session state
2. **Direct database modification:** Someone manually changed session state in database
3. **Checksum validation failed:** State tampering detected

**Diagnosis:**
```bash
# Check session state consistency
dotnet run -- session show abc-123 --raw

# Expected output for valid state:
# Session: Planning -> Tasks: 0
# Session: Executing -> Tasks: 1+ with at least 1 in progress
# Session: Completed -> Tasks: All completed
# Session: Failed -> Tasks: At least 1 failed

# If mismatch detected, check database directly
sqlite3 ~/.acode/sessions.db
SELECT id, state, (SELECT COUNT(*) FROM tasks WHERE session_id = sessions.id) as task_count 
FROM sessions WHERE id = 'abc-123';

# Check for checksum mismatch
SELECT id, state, state_checksum FROM sessions WHERE id = 'abc-123';
```

**Solutions:**

**Solution 1: Repair state from task status**
```bash
# If tasks completed but session shows Planning/Executing:
dotnet run -- session repair abc-123 --recompute-state

# This command:
# 1. Queries all tasks for session
# 2. If all tasks completed -> set session to Completed
# 3. If any task failed -> set session to Failed
# 4. If tasks in progress -> set session to Executing
# 5. Recomputes checksum
```

**Solution 2: Force state transition (if repair fails)**
```bash
# Manually transition to correct state
dotnet run -- session set-state abc-123 --state Completed --force

# WARNING: --force bypasses validation, use only when repair fails
# Logs warning: "Forced state transition for session abc-123"
```

**Solution 3: Rollback to last checkpoint**
```bash
# If corruption detected, restore from event log
dotnet run -- session rollback abc-123 --to-checkpoint 5

# Replays events 1-5, discards events 6+
# Rebuilds session state from known-good checkpoint
```

**Prevention:**
- Enable atomic state updates: Wrap session + task updates in transaction
- Run nightly consistency check: `dotnet run -- admin verify-sessions`
- Enable database backups: SQLite `PRAGMA journal_mode = WAL`

---

### Problem 2: UUID Generation Fails with "Sequence Overflow"

**Symptoms:**
- Entity creation fails with: `InvalidOperationException: UUID sequence overflow, clock may be stuck`
- Happens during rapid session creation (>4,096/millisecond)
- Error message: "Waited 1ms but sequence still maxed out"

**Possible Causes:**
1. **Clock not advancing:** System clock frozen or virtualization issue
2. **Extreme load:** Creating entities faster than clock resolution
3. **Infinite loop:** Bug causing runaway entity creation

**Diagnosis:**
```bash
# Check if system clock advancing
powershell -Command "for ($i=0; $i -lt 10; $i++) { Get-Date -Format 'HH:mm:ss.fff'; Start-Sleep -Milliseconds 100 }"

# Expected: Timestamps advance by ~100ms each iteration
# Problem: Timestamps frozen or advancing by large jumps (>1 second)

# Check entity creation rate
dotnet run -- stats show --metric entity-creation-rate

# Expected: < 1000/second for normal operation
# Problem: > 10,000/second indicates runaway loop

# Check for clock virtualization issues (if running in VM)
systeminfo | findstr /C:"System Model"
# If VM: Check hypervisor clock sync settings
```

**Solutions:**

**Solution 1: Fix system clock**
```powershell
# Resync system clock with NTP
w32tm /resync /force

# Verify clock advancing
powershell -Command "1..5 | ForEach-Object { [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds(); Start-Sleep -Milliseconds 50 }"

# Should see values incrementing by ~50ms
```

**Solution 2: Reduce entity creation rate**
```csharp
// If legitimate high load, batch entity creation:
// Instead of:
foreach (var task in tasks) { 
    var step = Step.Create(task.Id, "Do work");
    await repository.SaveAsync(step);
}

// Do this:
var steps = tasks.Select(t => Step.Create(t.Id, "Do work")).ToList();
await repository.SaveRangeAsync(steps);

// Reduces creation rate from 1000/sec to batches of 100 every 100ms
```

**Solution 3: Use alternative ID strategy for high-volume entities**
```csharp
// For high-volume entities like log entries, use auto-increment:
public class LogEntry
{
    public long Id { get; private set; } // Database auto-increment
    public Guid SessionId { get; private set; } // UUID v7 for session reference
    public string Message { get; private set; }
}

// Use UUID v7 for business entities (Session, Task), auto-increment for logs
```

**Prevention:**
- Enable clock monitoring: Alert if clock stops advancing for >5 seconds
- Rate-limit entity creation: Max 1000/second, queue excess
- Test clock sync in VM environments before deployment

---

### Problem 3: Artifact Content Validation Blocks Legitimate Output

**Symptoms:**
- Artifact creation fails with: `ArtifactValidationException: Content contains potential SQL injection patterns`
- Tool output is legitimate SQL query, not injection attempt
- Example: Code generator produces `SELECT * FROM users` as part of generated code

**Possible Causes:**
1. **Overly aggressive validation:** Pattern matching can't distinguish code from injection
2. **Wrong MIME type:** Content should be `text/x-sql` but set as `text/plain`
3. **Escaped vs. raw content:** Special characters not properly encoded

**Diagnosis:**
```bash
# Check artifact content and MIME type
dotnet run -- tool-call show <tool-call-id> --artifacts

# Example output showing problem:
# Artifact: Type=Output, MIME=text/plain, Content=SELECT * FROM users
#                                         ^^^^^^^^^ Flagged as SQL injection

# Check tool configuration
dotnet run -- tool show <tool-name> --output-config

# Expected: SQL code generator should set MIME type to text/x-sql
# Problem: Tool sets generic text/plain
```

**Solutions:**

**Solution 1: Use correct MIME type for code content**
```csharp
// In tool implementation:
public class SqlCodeGeneratorTool : ITool
{
    public async Task<ToolResult> ExecuteAsync(ToolInput input)
    {
        var sql = GenerateSqlQuery(input.Parameters);
        
        // CORRECT: Specify SQL MIME type - bypasses text/plain validation
        var artifact = Artifact.Create(
            toolCallId: input.ToolCallId,
            type: ArtifactType.Code,
            mimeType: "text/x-sql", // <-- Specific MIME type for SQL
            content: sql
        );
        
        return ToolResult.Success(artifact);
    }
}

// Update validation to allow SQL MIME type:
private static readonly string[] _allowedMimeTypes = new[]
{
    "text/plain",
    "text/x-sql",      // <-- Add SQL
    "text/x-csharp",
    "application/json"
};
```

**Solution 2: Base64-encode problematic content**
```csharp
// For content that triggers false positives, encode as base64:
var problematicContent = "'; DROP TABLE users; --";
var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(problematicContent));

var artifact = Artifact.Create(
    toolCallId: input.ToolCallId,
    type: ArtifactType.EncodedContent,
    mimeType: "application/base64",
    content: encoded
);

// Decode on read:
var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(artifact.Content));
```

**Solution 3: Whitelist specific patterns for code generation**
```csharp
// Add context-aware validation:
public static Artifact CreateCodeArtifact(
    Guid toolCallId,
    string language, // e.g., "sql", "csharp"
    string content)
{
    // Skip injection validation for code artifacts with proper MIME type
    var mimeType = language.ToLowerInvariant() switch
    {
        "sql" => "text/x-sql",
        "csharp" => "text/x-csharp",
        "python" => "text/x-python",
        _ => "text/plain"
    };
    
    // Code artifacts validated differently than user input
    return new Artifact
    {
        Id = SecureIdGenerator.NewId(),
        ToolCallId = toolCallId,
        Type = ArtifactType.Code,
        MimeType = mimeType,
        Content = content, // No injection validation for code artifacts
        CreatedAt = DateTimeOffset.UtcNow
    };
}
```

**Prevention:**
- Document MIME types for each tool in tool catalog
- Add validation tests for each MIME type
- Use `ArtifactType.Code` for generated code, `ArtifactType.Output` for tool results

---

### Problem 4: Session Checksum Fails After Database Migration

**Symptoms:**
- Resume fails with: `EntityTamperedException: Session checksum mismatch`
- Happens after upgrading from SQLite to Postgres
- All sessions show checksum errors, not just one

**Possible Causes:**
1. **Serialization format changed:** Different JSON ordering between databases
2. **Timestamp precision loss:** SQLite stores milliseconds, Postgres stores microseconds
3. **Null handling difference:** SQLite treats empty string as NULL differently than Postgres

**Diagnosis:**
```bash
# Check serialization format
dotnet run -- session show abc-123 --debug-checksum

# Output shows checksum computation:
# Expected checksum: "8x3kF2..."
# Actual checksum:   "9zLmA1..."
# 
# Serialized state (expected):
# {"id":"abc-123","state":"Executing","tasks":[{"id":"task-1","state":"Completed","order":1}]}
#
# Serialized state (actual):
# {"id":"abc-123","tasks":[{"order":1,"id":"task-1","state":"Completed"}],"state":"Executing"}
#                          ^^^^^^^^^ Property order different

# Compare database schemas
sqlite3 old_db.sqlite ".schema sessions" > sqlite_schema.txt
psql -d new_db -c "\d+ sessions" > postgres_schema.txt
diff sqlite_schema.txt postgres_schema.txt
```

**Solutions:**

**Solution 1: Recompute all checksums after migration**
```bash
# After migration, recalculate checksums for all sessions:
dotnet run -- admin recompute-checksums --all

# This:
# 1. Loads each session
# 2. Skips checksum validation (--skip-validation flag)
# 3. Recomputes checksum from current state
# 4. Saves updated checksum
#
# Progress: Recomputed 1000 sessions in 15 seconds
```

**Solution 2: Use canonical JSON serialization**
```csharp
// Update checksum computation to use deterministic serialization:
public static string ComputeChecksum<T>(T entity) where T : class
{
    var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        
        // NEW: Sort properties alphabetically for deterministic output
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        
        // NEW: Custom converter that sorts object properties
        Converters = { new OrderedPropertiesConverter() }
    });
    
    using var sha256 = SHA256.Create();
    return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(json)));
}

public class OrderedPropertiesConverter : JsonConverter<object>
{
    // Ensures properties always serialized in same order regardless of database
}
```

**Solution 3: Disable checksum validation temporarily**
```bash
# If migration urgent and recomputation slow:
export ACODE_SKIP_CHECKSUM_VALIDATION=true
dotnet run -- session resume abc-123

# WARNING: Reduces security, use only during migration
# Re-enable immediately after: unset ACODE_SKIP_CHECKSUM_VALIDATION
```

**Prevention:**
- Test migration with checksum validation in staging environment
- Include checksum recomputation in migration script
- Document serialization format as part of schema version

---

### Problem 5: Entity Relationship Navigation Fails with Lazy Loading

**Symptoms:**
- `NullReferenceException` when accessing `task.Steps` collection
- Works in unit tests, fails in production
- Error: "Collection was accessed before being loaded"

**Possible Causes:**
1. **Lazy loading not enabled:** EF Core requires explicit configuration
2. **DbContext disposed:** Accessing navigation after context closed
3. **Detached entity:** Entity loaded from one context, accessed from another

**Diagnosis:**
```bash
# Enable EF Core query logging
export ACODE_LOG_LEVEL=Debug
dotnet run -- session show abc-123

# Check logs for:
# [DEBUG] Executing DbCommand: SELECT * FROM tasks WHERE session_id = 'abc-123'
#         ^^^^^^^^^^ If this appears, eager loading working
# 
# [WARN] Navigation property 'Tasks' accessed but not loaded
#        ^^^^^^^^^^ If this appears, lazy loading not configured

# Check DbContext lifetime
dotnet run -- debug session-load-lifecycle abc-123

# Output shows:
# 1. DbContext created: 14:32:15.123
# 2. Session loaded: 14:32:15.145
# 3. DbContext disposed: 14:32:15.167
# 4. task.Steps accessed: 14:32:15.201 <-- AFTER disposal
```

**Solutions:**

**Solution 1: Use eager loading with Include()**
```csharp
// CORRECT: Load entire aggregate at once
public async Task<Session> GetByIdAsync(Guid sessionId)
{
    return await _dbContext.Sessions
        .Include(s => s.Tasks)              // Load tasks
            .ThenInclude(t => t.Steps)      // Load steps
                .ThenInclude(s => s.ToolCalls) // Load tool calls
                    .ThenInclude(tc => tc.Artifacts) // Load artifacts
        .FirstOrDefaultAsync(s => s.Id == sessionId);
    
    // Now entire aggregate loaded, safe to access any navigation property
}

// INCORRECT: Lazy loading not configured
public async Task<Session> GetByIdAsync(Guid sessionId)
{
    return await _dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
    // task.Steps will be NULL - not loaded
}
```

**Solution 2: Enable lazy loading proxies**
```csharp
// In Startup.cs or Program.cs:
services.AddDbContext<AcodeDbContext>(options =>
{
    options.UseSqlite(connectionString)
           .UseLazyLoadingProxies(); // <-- Enable lazy loading
});

// Mark navigation properties as virtual:
public class Task
{
    public virtual ICollection<Step> Steps { get; set; } // Must be virtual
}

// Now Steps automatically loaded when accessed
```

**Solution 3: Project to DTO with explicit loading**
```csharp
// For read-only scenarios, project to DTO:
public async Task<SessionDto> GetSessionSummaryAsync(Guid sessionId)
{
    return await _dbContext.Sessions
        .Where(s => s.Id == sessionId)
        .Select(s => new SessionDto
        {
            Id = s.Id,
            State = s.State,
            TaskCount = s.Tasks.Count,
            CompletedTasks = s.Tasks.Count(t => t.State == TaskState.Completed)
        })
        .FirstOrDefaultAsync();
    
    // No navigation property access, query optimized, no lazy loading needed
}
```

**Prevention:**
- Use explicit Include() for all aggregate root queries
- Enable lazy loading only if needed (adds overhead)
- Test with disposed DbContext: `using (var context = ...) { var session = Load(); } session.Tasks.Count();`

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