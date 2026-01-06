# Task 011: Run Session State Machine + Persistence

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 34 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 050 (Workspace DB), Task 010 (CLI Framework), Task 002 (.agent/config.yml), Task 018 (Undo/Checkpoint), Task 026 (Observability)  

---

## Description

Task 011 implements the Run Session State Machine, the central orchestration mechanism that governs agent execution from initiation through completion. The state machine manages session lifecycle, tracks execution progress, handles interruptions, and ensures recoverability through persistence. This is the backbone of Acode's reliability.

A Run Session represents a complete unit of agentic work. When a user invokes `acode run "Add validation"`, a session is created encompassing all planning, execution, and verification that follows. Sessions have defined states—Created, Planning, Executing, Paused, Completed, Failed, Cancelled. Transitions between states follow strict rules that maintain invariants.

The state machine enforces correctness constraints. Invalid transitions are rejected—a Completed session cannot move to Executing. State-specific behaviors are encapsulated—Planning state allows only planning operations. This rigor prevents subtle bugs where sessions end up in inconsistent states due to unexpected event sequences.

Persistence is fundamental, not optional. Every state transition is durably recorded before the transition completes. If the process crashes mid-execution, the session can be resumed from its last persisted state. This durability uses the Workspace DB Foundation (Task 050) abstractions, writing to local SQLite with optional sync to remote PostgreSQL.

The persistence model follows event sourcing principles. Rather than just storing current state, the system records every transition event. This history enables debugging (what happened?), auditing (who approved what?), and recovery (resume from any point). Events are immutable—corrections are new events, not edits.

Abstraction layers ensure database independence. Run session logic depends on `IRunStateStore` and `IWorkspaceDb` interfaces, not concrete SQLite or PostgreSQL implementations. This enables testing with in-memory stores, development with SQLite, and production with PostgreSQL, all without changing business logic.

Session hierarchy organizes work naturally. A Session contains one or more Tasks (high-level goals). Tasks contain Steps (discrete actions). Steps contain Tool Calls (atomic operations). This hierarchy mirrors how the agent thinks and enables granular progress tracking, targeted rollback, and efficient resumption.

Interruption handling is first-class. Users may Ctrl+C, systems may crash, models may timeout. The state machine handles all interruption modes gracefully. In-progress work is checkpointed. Resources are cleaned up. The session enters a resumable state that clearly indicates what completed and what remains.

Resume behavior reconstructs exactly where execution stopped. The state machine queries persisted state, identifies incomplete work, and restarts from that point. Completed steps are not re-executed. Partially completed steps are rolled back or retried based on configuration. This efficiency is critical for long-running sessions.

Concurrency control prevents corruption. Only one process may actively execute a session. Locks (advisory or table-level) prevent multiple CLI invocations from operating on the same session. Stale locks from crashed processes are detected and released. These mechanisms maintain single-writer semantics.

Integration points connect the state machine to all Acode subsystems. The agent loop (Task 012) requests state transitions. Approval gates (Task 013) pause execution for user decisions. Undo/checkpoint (Task 018) creates recovery points. Observability (Task 026) receives transition events. The state machine is the coordination hub.

Testing requires simulating complex scenarios. Unit tests verify individual transitions. Integration tests verify the persistence layer. Property-based tests explore edge cases like concurrent access and crash recovery. Test coverage must include all state combinations and transition paths.

---

## Use Cases

### Use Case 1: Crash Recovery During Long-Running Refactoring

**Persona:** DevBot (Autonomous Agent), Jordan (Senior Developer, Software Team Lead)

**Context:** Jordan starts a complex refactoring session to migrate a legacy authentication system to a modern OAuth2 implementation. The refactoring involves analyzing 47 files, updating 18 controllers, modifying 9 service classes, and rewriting 31 test files. The session is expected to run for 35-45 minutes.

**Problem:** Midway through the refactoring (23 minutes in), Jordan's laptop battery dies unexpectedly. Without session persistence and state recovery, Jordan would need to start the entire refactoring from scratch, losing 23 minutes of analysis and 14 files already modified. The agent has no memory of what it accomplished, what remains, or which files are in a partially modified state.

**Solution (With Task 011):** The Run Session State Machine persisted every state transition and work unit completion to the Workspace Database. When Jordan restarts the laptop and runs `acode session resume <session-id>`, the system:

1. Queries IRunStateStore to retrieve the last persisted session state (EXECUTING)
2. Reconstructs the session hierarchy showing 14 completed files, 3 partially modified files, and 30 pending files
3. Resumes execution from the last checkpoint, rolling back the 3 partial files and continuing with the 30 pending files
4. Avoids re-analyzing the codebase or re-modifying the 14 completed files
5. Completes the refactoring in the remaining 18 minutes instead of restarting the full 42-minute session

**Business Impact:**
- **Time Saved:** 23 minutes per crash × 2 crashes/month × 15 developers = 11.5 hours/month = **$2,875/month** at $250/hour developer rate
- **Annual Savings:** $34,500/year in prevented rework
- **Reduced Frustration:** Developers trust the agent to handle interruptions gracefully, increasing adoption from 60% to 92%
- **Risk Reduction:** Prevents merge conflicts and inconsistent states from restarted sessions

**Success Metrics:**
- Resume success rate: 99.5% of interrupted sessions resume correctly
- Resume time: < 500ms to reconstruct state and continue execution
- Data loss: 0% completed work lost due to crashes or interruptions

---

### Use Case 2: Concurrent Session Prevention During Team Collaboration

**Persona:** Alex (DevOps Engineer), Jordan (Senior Developer)

**Context:** Alex and Jordan are pair programming on a critical security patch. Jordan starts an agent session to implement input validation across 8 API endpoints. While the session is running, Alex (unaware Jordan already started a session) attempts to run another agent session for the same task from a different terminal window.

**Problem:** Without session locking, both instances would:
- Write to the same session state concurrently, corrupting the database
- Execute duplicate tool calls (reading files twice, writing conflicting changes)
- Create merge conflicts when both instances attempt to commit changes
- Waste compute resources on redundant work (2× model inference costs)
- Risk data inconsistency with race conditions on session state updates

**Solution (With Task 011):** When Jordan's session starts, ISessionLock acquires an exclusive lock via FileSessionLock (advisory lock file in `.acode/locks/<session-id>.lock`). When Alex attempts to start the second session, the system:

1. Attempts to acquire the lock via ISessionLock.TryAcquireAsync(timeout: 60s)
2. Detects the existing lock held by Jordan's process
3. Displays error: "Session already running (PID 47382, started 8 minutes ago). Use `acode session show <id>` to view status or `acode session cancel <id>` to terminate."
4. Alex runs `acode session show <id>` and sees Jordan's session is 45% complete
5. Alex communicates with Jordan to avoid duplication, then monitors progress via `acode session status --watch`

**Business Impact:**
- **Cost Savings:** Prevents 2-3 duplicate sessions/week × 20 minutes/session × $0.15/minute model cost = **$180/month** saved in model inference costs
- **Merge Conflict Prevention:** Avoids 1-2 hours/week resolving conflicts from concurrent sessions = **$2,000/month** saved (8 hours × $250/hour)
- **Data Integrity:** 100% prevention of database corruption from concurrent writes
- **Resource Efficiency:** No wasted compute on redundant analysis and code generation

**Success Metrics:**
- Lock conflicts detected: 100% of concurrent access attempts blocked
- False positives: < 1% (stale locks released properly)
- Lock acquisition time: < 100ms
- User clarity: Clear error messages guide users to correct action

---

### Use Case 3: State History Audit for Compliance and Debugging

**Persona:** Morgan (Security Auditor), Taylor (DevOps Manager)

**Context:** A production incident occurs where a critical configuration file was modified by an agent session, causing a 15-minute outage. Morgan needs to audit exactly what happened: which session made the change, what state transitions occurred, what approval gates were (or were not) triggered, and what the agent's reasoning was at each decision point.

**Problem:** Without event sourcing and state history, the investigation requires:
- Manually reviewing Git commit logs (limited context, no approval data)
- Searching through scattered log files across multiple systems
- Interviewing developers who may not remember details
- Reconstructing timeline from incomplete data, taking 3-4 hours
- Unable to prove whether approval gates were bypassed or properly followed

**Solution (With Task 011):** The Run Session State Machine records every state transition as an immutable SessionEvent with timestamp, reason, and metadata. Morgan runs:

```bash
acode session show <session-id> --verbose
acode session history <session-id> --format json
```

The system provides complete audit trail:

1. **Session Created** at 2025-01-05T14:23:17Z (CREATED → PLANNING)
2. **Planning Completed** at 2025-01-05T14:24:03Z (PLANNING → AWAITING_APPROVAL) - Reason: "High-risk file modification detected: config/production.yml"
3. **Approval Granted** at 2025-01-05T14:26:45Z (AWAITING_APPROVAL → EXECUTING) - Approver: taylor@company.com, Reason: "Reviewed plan, looks safe"
4. **Execution Started** at 2025-01-05T14:26:46Z - Tool calls: read_file, write_file (12 events)
5. **Session Completed** at 2025-01-05T14:28:22Z (EXECUTING → COMPLETED) - Duration: 5 minutes 5 seconds

Morgan discovers:
- Approval gate WAS triggered (high-risk file detected)
- Taylor approved the change after reviewing the plan
- Timeline shows 2m 42s approval delay (within policy)
- Complete tool call history shows exactly what was modified
- No policy violations occurred; outage was due to configuration typo, not process failure

**Business Impact:**
- **Audit Efficiency:** Incident investigation reduced from 3-4 hours to **15 minutes** = **$937.50 saved per incident** ($250/hour × 3.75 hours)
- **Compliance:** Provides cryptographic proof of approval workflow for SOC 2, ISO 27001, HIPAA audits
- **Root Cause Analysis:** Complete event history enables faster debugging (12× faster than manual log analysis)
- **Blame Reduction:** Objective timeline eliminates finger-pointing and focuses on systemic improvements
- **Annual Value:** 8 incidents/year × $937.50/incident = **$7,500/year** + compliance audit pass rate 100% (avoids $50K penalty risk)

**Success Metrics:**
- Audit trail completeness: 100% of state transitions recorded with no gaps
- Query performance: < 10ms to retrieve full session history (500 events)
- Event immutability: 0 cases of tampered or modified historical events
- Retention: Events retained for 90 days minimum, 2 years for compliance mode

---

**Total Business Value Summary:**
- **Crash Recovery:** $34,500/year saved from prevented rework
- **Concurrency Prevention:** $26,160/year saved from duplicate work elimination ($180 + $2,000 × 12)
- **Audit Efficiency:** $7,500/year saved from faster incident investigation
- **Total Annual ROI:** **$68,160/year** for a 10-developer team
- **Intangible Benefits:** Increased agent trust, reduced developer frustration, compliance risk mitigation, data integrity guarantee

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Run Session | Complete unit of agentic work |
| Session State | Current position in lifecycle |
| State Transition | Movement from one state to another |
| State Machine | System governing valid transitions |
| Event Sourcing | Recording all transitions as events |
| Persistence | Durable storage of state |
| IRunStateStore | Abstraction for state persistence |
| IWorkspaceDb | Database abstraction layer |
| Session Hierarchy | Session → Task → Step → Tool Call |
| Checkpoint | Recovery point within execution |
| Resume | Restart from interrupted state |
| Lock | Concurrency control mechanism |
| Invariant | Condition that must always hold |
| Transition Guard | Condition for valid transition |
| Event | Immutable record of state change |

---

## Out of Scope

The following items are explicitly excluded from Task 011:

- **Agent decision logic** - Task 012
- **Approval workflow** - Task 013
- **Undo/rollback mechanics** - Task 018
- **Multi-agent coordination** - Post-MVP
- **Distributed sessions** - Single machine
- **Session migration** - Fixed location
- **Session archival** - Retention policy only
- **External monitoring integration** - Task 026
- **Real-time sync** - Eventual consistency
- **Session templates** - Hardcoded structure

---

## Assumptions

### Technical Assumptions

- ASM-001: UUID v7 generation is available for time-ordered unique identifiers
- ASM-002: State machine patterns can be implemented with finite state automaton semantics
- ASM-003: DateTimeOffset provides sufficient precision for timestamp ordering
- ASM-004: JSON serialization is available for event payloads and metadata
- ASM-005: Event sourcing patterns can be applied for state history
- ASM-006: Lock mechanisms are available for session exclusivity

### Environmental Assumptions

- ASM-007: Single-machine execution is the primary deployment model
- ASM-008: File system or database storage is available for persistence
- ASM-009: Process interruption (SIGINT, crash) can occur at any point
- ASM-010: System clock is reasonably accurate for timestamp ordering

### Dependency Assumptions

- ASM-011: Task 010 CLI provides command infrastructure for session commands
- ASM-012: Task 011.a entity model is defined before implementation
- ASM-013: Task 011.b persistence layer handles actual storage
- ASM-014: Task 011.c resume behavior builds on session state

### Behavioral Assumptions

- ASM-015: State transitions are atomic and observable
- ASM-016: Invalid state transitions are programming errors, not user errors
- ASM-017: Session state must be recoverable after process restart
- ASM-018: Event history provides complete audit trail

---

## Functional Requirements

### Session Lifecycle

- FR-001: Session MUST have unique ID (UUID v7)
- FR-002: Session MUST track creation timestamp
- FR-003: Session MUST track last update timestamp
- FR-004: Session MUST reference original task description
- FR-005: Session MUST track current state
- FR-006: Session MUST track previous states (history)
- FR-007: Session MUST be queryable by ID
- FR-008: Active sessions MUST be listable

### State Definitions

- FR-009: CREATED state for new sessions
- FR-010: PLANNING state during analysis
- FR-011: AWAITING_APPROVAL for approval gates
- FR-012: EXECUTING state during actions
- FR-013: PAUSED state for user interruption
- FR-014: COMPLETED state for success
- FR-015: FAILED state for error termination
- FR-016: CANCELLED state for user cancellation

### State Transitions

- FR-017: CREATED → PLANNING (on start)
- FR-018: PLANNING → AWAITING_APPROVAL (if required)
- FR-019: PLANNING → EXECUTING (if auto-approved)
- FR-020: AWAITING_APPROVAL → EXECUTING (on approve)
- FR-021: AWAITING_APPROVAL → CANCELLED (on reject)
- FR-022: EXECUTING → AWAITING_APPROVAL (for gates)
- FR-023: EXECUTING → COMPLETED (on success)
- FR-024: EXECUTING → FAILED (on error)
- FR-025: Any active → PAUSED (on interrupt)
- FR-026: PAUSED → previous active state (on resume)
- FR-027: Any → CANCELLED (on user cancel)

### Transition Guards

- FR-028: Transition MUST validate preconditions
- FR-029: Invalid transitions MUST be rejected
- FR-030: Guard failures MUST log reason
- FR-031: Guards MUST be synchronous
- FR-032: Guards MUST NOT have side effects

### Event Recording

- FR-033: Every transition MUST emit event
- FR-034: Events MUST be immutable
- FR-035: Events MUST include timestamp
- FR-036: Events MUST include previous state
- FR-037: Events MUST include new state
- FR-038: Events MUST include trigger reason
- FR-039: Events MUST be persistable

### Session Hierarchy

- FR-040: Session contains Tasks
- FR-041: Task contains Steps
- FR-042: Step contains ToolCalls
- FR-043: Each level has own state
- FR-044: Parent state derives from children
- FR-045: Hierarchy is queryable
- FR-046: Hierarchy supports partial completion

### Persistence

- FR-047: State MUST use IRunStateStore abstraction
- FR-048: MUST persist to local SQLite via Task 050
- FR-049: MAY sync to PostgreSQL when configured
- FR-050: Persist MUST occur before transition completes
- FR-051: Persist failure MUST abort transition
- FR-052: Persist MUST be transactional
- FR-053: Recovery MUST restore exact state

### Locking

- FR-054: Active session MUST acquire lock
- FR-055: Lock MUST prevent concurrent access
- FR-056: Lock MUST have timeout (60s default)
- FR-057: Stale locks MUST be detectable
- FR-058: Stale locks MUST be releasable
- FR-059: Lock release MUST occur on exit

### Resume Behavior

- FR-060: Resume MUST find last checkpoint
- FR-061: Resume MUST skip completed steps
- FR-062: Resume MUST retry incomplete steps
- FR-063: Resume MUST restore context
- FR-064: Resume MUST validate environment
- FR-065: Resume MUST log what's being resumed

### Cleanup

- FR-066: COMPLETED sessions MUST cleanup resources
- FR-067: FAILED sessions MUST cleanup partial work
- FR-068: CANCELLED sessions MUST cleanup all
- FR-069: Cleanup MUST respect retention policy
- FR-070: Cleanup MUST not affect other sessions

### Query Operations

- FR-071: Get session by ID
- FR-072: List sessions by state
- FR-073: List sessions by date range
- FR-074: Get session history
- FR-075: Get session hierarchy
- FR-076: Query MUST support pagination

---

## Non-Functional Requirements

### Performance

- NFR-001: State transition MUST complete < 50ms
- NFR-002: Persist MUST complete < 100ms
- NFR-003: Query by ID MUST complete < 10ms
- NFR-004: Resume MUST complete < 500ms
- NFR-005: Lock acquisition MUST complete < 100ms

### Reliability

- NFR-006: Crash recovery MUST preserve state
- NFR-007: Partial transitions MUST NOT corrupt state
- NFR-008: Lock conflicts MUST be handled gracefully
- NFR-009: Database errors MUST be recoverable

### Security

- NFR-010: Session data MUST NOT contain secrets
- NFR-011: Lock files MUST have appropriate permissions
- NFR-012: Queries MUST NOT leak other sessions

### Durability

- NFR-013: Persisted state MUST survive crash
- NFR-014: Events MUST NOT be lost
- NFR-015: Sync failures MUST NOT lose local data

### Observability

- NFR-016: All transitions MUST be logged
- NFR-017: All errors MUST be logged with context
- NFR-018: Metrics MUST track transition latency
- NFR-019: Metrics MUST track session counts by state

---

## Security Considerations

### Threat 1: Session State Injection via Database Manipulation

**Attack Scenario:**
An attacker with filesystem access to `.acode/workspace.db` directly modifies the SQLite database using `sqlite3` CLI or a database editor. The attacker changes a session's state from AWAITING_APPROVAL to EXECUTING, bypassing the approval gate. This allows the session to proceed with high-risk file modifications without authorization.

**Impact Assessment:**
- **Confidentiality:** Low (no data exfiltration)
- **Integrity:** **CRITICAL** - Unauthorized code changes executed without approval
- **Availability:** Low (system remains operational)

**Mitigation Strategy:**

1. **State Checksum Validation:** Compute SHA-256 checksum of session state and validate on load

```csharp
namespace AgenticCoder.Domain.Sessions;

public sealed class Session
{
    private string _stateChecksum;
    
    public Session(SessionId id, string taskDescription)
    {
        Id = id;
        TaskDescription = taskDescription;
        State = SessionState.Created;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        _stateChecksum = ComputeChecksum();
    }
    
    public void Transition(SessionState newState, string reason)
    {
        // Validate checksum before transition
        if (!ValidateChecksum())
        {
            throw new SessionTamperedException(
                $"Session {Id} failed checksum validation. Possible database tampering detected.");
        }
        
        // Record transition
        var evt = new SessionEvent(
            Id,
            State,
            newState,
            reason,
            DateTimeOffset.UtcNow);
        
        _events.Add(evt);
        State = newState;
        UpdatedAt = DateTimeOffset.UtcNow;
        
        // Recompute checksum
        _stateChecksum = ComputeChecksum();
    }
    
    private string ComputeChecksum()
    {
        var data = $"{Id}|{State}|{UpdatedAt:O}|{_events.Count}";
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
    
    private bool ValidateChecksum()
    {
        return _stateChecksum == ComputeChecksum();
    }
}

public sealed class SessionTamperedException : Exception
{
    public SessionTamperedException(string message) : base(message) { }
}
```

2. **Database File Permissions:** Restrict `.acode/workspace.db` to owner-only (chmod 600 on Unix, ACLs on Windows)

```bash
# Set restrictive permissions on database file
chmod 600 .acode/workspace.db
chmod 700 .acode/

# Verify permissions
ls -la .acode/workspace.db
# Expected: -rw------- (owner read/write only)
```

3. **Audit Logging:** Log all database access attempts with process ID and user

```csharp
public sealed class SQLiteRunStateStore : IRunStateStore
{
    private readonly IAuditLogger _auditLogger;
    
    public async Task<Session?> GetAsync(SessionId id, CancellationToken ct)
    {
        _auditLogger.LogDatabaseAccess(
            operation: "ReadSession",
            sessionId: id,
            processId: Environment.ProcessId,
            userName: Environment.UserName);
        
        // Retrieve session
        var session = await LoadSessionAsync(id, ct);
        
        // Validate checksum
        if (session != null && !session.ValidateChecksum())
        {
            _auditLogger.LogSecurityEvent(
                severity: "CRITICAL",
                eventType: "ChecksumValidationFailed",
                sessionId: id,
                message: "Session checksum mismatch - possible tampering");
            throw new SessionTamperedException($"Session {id} checksum validation failed");
        }
        
        return session;
    }
}
```

4. **Defense in Depth:**
   - **Application Layer:** Checksum validation on every load
   - **Database Layer:** Foreign key constraints prevent orphaned events
   - **Filesystem Layer:** Owner-only permissions prevent unauthorized access
   - **Audit Layer:** Comprehensive logging enables forensic analysis
   - **Infrastructure Layer:** Filesystem encryption (BitLocker, LUKS) protects data at rest

---

### Threat 2: Session Lock Bypass via Stale Lock Exploitation

**Attack Scenario:**
An attacker discovers the lock file mechanism (`.acode/locks/<session-id>.lock`) and creates a script to rapidly create and delete lock files. By exploiting race conditions in stale lock detection, the attacker causes two concurrent processes to believe they both hold the lock, leading to database corruption from concurrent writes.

**Impact Assessment:**
- **Confidentiality:** Low (no data exfiltration)
- **Integrity:** **HIGH** - Database corruption from concurrent writes
- **Availability:** **HIGH** - Corrupted database may render sessions unrecoverable

**Mitigation Strategy:**

1. **Atomic Lock Acquisition:** Use filesystem atomic operations (`CreateNew` mode) to prevent race conditions

```csharp
namespace AgenticCoder.Infrastructure.Locking;

public sealed class FileSessionLock : ISessionLock
{
    private readonly string _lockFilePath;
    private FileStream? _lockFileStream;
    private bool _isHeld;
    
    public FileSessionLock(SessionId sessionId)
    {
        SessionId = sessionId;
        _lockFilePath = Path.Combine(".acode", "locks", $"{sessionId}.lock");
    }
    
    public async Task<bool> TryAcquireAsync(TimeSpan timeout, CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                // Atomic lock file creation (fails if exists)
                _lockFileStream = new FileStream(
                    _lockFilePath,
                    FileMode.CreateNew,  // CRITICAL: Atomic "create if not exists"
                    FileAccess.Write,
                    FileShare.None);
                
                // Write lock metadata
                var lockData = new LockMetadata(
                    ProcessId: Environment.ProcessId,
                    MachineName: Environment.MachineName,
                    AcquiredAt: DateTimeOffset.UtcNow,
                    ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(5));
                
                var json = JsonSerializer.Serialize(lockData);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _lockFileStream.WriteAsync(bytes, ct);
                await _lockFileStream.FlushAsync(ct);
                
                _isHeld = true;
                AcquiredAt = lockData.AcquiredAt;
                return true;
            }
            catch (IOException) when (File.Exists(_lockFilePath))
            {
                // Lock already exists - check if stale
                if (await IsLockStaleAsync(_lockFilePath, ct))
                {
                    // Break stale lock
                    await ForceReleaseAsync(ct);
                    continue;
                }
                
                // Lock is active - wait and retry
                await Task.Delay(100, ct);
            }
        }
        
        return false; // Timeout
    }
    
    private async Task<bool> IsLockStaleAsync(string lockFilePath, CancellationToken ct)
    {
        try
        {
            var json = await File.ReadAllTextAsync(lockFilePath, ct);
            var lockData = JsonSerializer.Deserialize<LockMetadata>(json);
            
            if (lockData == null)
                return true; // Corrupted lock file
            
            // Check if expired
            if (DateTimeOffset.UtcNow > lockData.ExpiresAt)
                return true;
            
            // Check if process still running
            try
            {
                var process = Process.GetProcessById(lockData.ProcessId);
                return process.HasExited;
            }
            catch (ArgumentException)
            {
                return true; // Process doesn't exist
            }
        }
        catch
        {
            return true; // Can't validate - assume stale
        }
    }
    
    private async Task ForceReleaseAsync(CancellationToken ct)
    {
        try
        {
            // Log forced release
            _logger.LogWarning(
                "Forcing release of stale lock for session {SessionId}",
                SessionId);
            
            // Delete lock file
            File.Delete(_lockFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to force release lock for session {SessionId}",
                SessionId);
        }
    }
}

record LockMetadata(
    int ProcessId,
    string MachineName,
    DateTimeOffset AcquiredAt,
    DateTimeOffset ExpiresAt);
```

2. **Lock File Permissions:** Restrict lock directory to owner-only access

```bash
chmod 700 .acode/locks/
chmod 600 .acode/locks/*.lock
```

3. **Automated Stale Lock Cleanup:** Background job periodically cleans stale locks

```csharp
public sealed class StaleLockCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
            await CleanupStaleLocksAsync(ct);
        }
    }
    
    private async Task CleanupStaleLocksAsync(CancellationToken ct)
    {
        var lockDir = Path.Combine(".acode", "locks");
        if (!Directory.Exists(lockDir))
            return;
        
        var lockFiles = Directory.GetFiles(lockDir, "*.lock");
        
        foreach (var lockFile in lockFiles)
        {
            if (await IsLockStaleAsync(lockFile, ct))
            {
                File.Delete(lockFile);
                _logger.LogInformation(
                    "Cleaned up stale lock file: {LockFile}",
                    Path.GetFileName(lockFile));
            }
        }
    }
}
```

4. **Defense in Depth:**
   - **Atomic Operations:** FileMode.CreateNew ensures no race conditions
   - **Process Validation:** Verify lock-holding process is still alive
   - **Expiration:** Locks auto-expire after 5 minutes (configurable)
   - **Periodic Cleanup:** Background service removes stale locks
   - **Audit Logging:** All lock acquisitions, releases, and force-breaks logged

---

### Threat 3: Event History Injection for Audit Trail Manipulation

**Attack Scenario:**
An attacker with database access inserts fake SessionEvent records to fabricate an approval trail. For example, inserting an event showing "Approval Granted" when no approval was actually given, allowing unauthorized actions to appear legitimate during compliance audits.

**Impact Assessment:**
- **Confidentiality:** Low (no data leaked)
- **Integrity:** **CRITICAL** - Falsified audit trail undermines compliance
- **Availability:** Low (system operational)

**Mitigation Strategy:**

1. **Event Signing:** Cryptographically sign each event using HMAC-SHA256

```csharp
namespace AgenticCoder.Domain.Sessions;

public sealed record SessionEvent
{
    public Guid EventId { get; init; }
    public SessionId SessionId { get; init; }
    public SessionState FromState { get; init; }
    public SessionState ToState { get; init; }
    public string Reason { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string Signature { get; init; }
    
    public static SessionEvent Create(
        SessionId sessionId,
        SessionState fromState,
        SessionState toState,
        string reason,
        IEventSigner signer)
    {
        var eventId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        
        var evt = new SessionEvent
        {
            EventId = eventId,
            SessionId = sessionId,
            FromState = fromState,
            ToState = toState,
            Reason = reason,
            Timestamp = timestamp,
            Signature = string.Empty // Compute next
        };
        
        // Compute signature
        var signature = signer.Sign(evt);
        return evt with { Signature = signature };
    }
    
    public bool ValidateSignature(IEventSigner signer)
    {
        var expectedSignature = signer.Sign(this with { Signature = string.Empty });
        return Signature == expectedSignature;
    }
}

public interface IEventSigner
{
    string Sign(SessionEvent evt);
}

public sealed class HmacEventSigner : IEventSigner
{
    private readonly byte[] _secretKey;
    
    public HmacEventSigner(IConfiguration configuration)
    {
        // Load secret key from secure configuration
        var keyBase64 = configuration["Acode:EventSigningKey"]
            ?? throw new InvalidOperationException("Event signing key not configured");
        _secretKey = Convert.FromBase64String(keyBase64);
    }
    
    public string Sign(SessionEvent evt)
    {
        // Serialize event fields (excluding signature)
        var data = $"{evt.EventId}|{evt.SessionId}|{evt.FromState}|{evt.ToState}|{evt.Reason}|{evt.Timestamp:O}";
        var bytes = Encoding.UTF8.GetBytes(data);
        
        // Compute HMAC-SHA256
        using var hmac = new HMACSHA256(_secretKey);
        var hash = hmac.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
```

2. **Event Chain Validation:** Link events using cryptographic hashing (blockchain-style)

```csharp
public sealed class Session
{
    private readonly List<SessionEvent> _events = new();
    
    public void Transition(SessionState newState, string reason, IEventSigner signer)
    {
        // Create signed event
        var evt = SessionEvent.Create(
            SessionId: Id,
            FromState: State,
            ToState: newState,
            Reason: reason,
            Signer: signer);
        
        // Link to previous event (chain validation)
        if (_events.Count > 0)
        {
            var previousEvent = _events[^1];
            evt = evt with { PreviousEventId = previousEvent.EventId };
        }
        
        _events.Add(evt);
        State = newState;
    }
    
    public bool ValidateEventChain(IEventSigner signer)
    {
        for (int i = 0; i < _events.Count; i++)
        {
            var evt = _events[i];
            
            // Validate signature
            if (!evt.ValidateSignature(signer))
            {
                _logger.LogError(
                    "Event {EventId} signature validation failed",
                    evt.EventId);
                return false;
            }
            
            // Validate chain link
            if (i > 0 && evt.PreviousEventId != _events[i - 1].EventId)
            {
                _logger.LogError(
                    "Event chain broken at event {EventId}",
                    evt.EventId);
                return false;
            }
        }
        
        return true;
    }
}
```

3. **Write-Once Event Storage:** Store events in append-only log with immutable records

```sql
-- SQLite schema with append-only semantics
CREATE TABLE session_events (
    event_id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    from_state TEXT NOT NULL,
    to_state TEXT NOT NULL,
    reason TEXT NOT NULL,
    timestamp TEXT NOT NULL,
    signature TEXT NOT NULL,
    previous_event_id TEXT,
    created_at TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (session_id) REFERENCES sessions(id),
    FOREIGN KEY (previous_event_id) REFERENCES session_events(event_id)
) STRICT;

-- Prevent updates and deletes (append-only)
CREATE TRIGGER prevent_event_update
BEFORE UPDATE ON session_events
BEGIN
    SELECT RAISE(FAIL, 'Events are immutable - updates not allowed');
END;

CREATE TRIGGER prevent_event_delete
BEFORE DELETE ON session_events
BEGIN
    SELECT RAISE(FAIL, 'Events are immutable - deletes not allowed');
END;
```

4. **Defense in Depth:**
   - **Cryptographic Signatures:** HMAC-SHA256 prevents forgery
   - **Event Chaining:** Blockchain-style linking detects insertion/deletion
   - **Database Triggers:** Prevent UPDATE/DELETE on event table
   - **Audit Logging:** All database writes logged with caller identity
   - **Secret Key Management:** Signing key stored in secure configuration (environment variable, not committed to Git)

---

### Threat 4: State Transition Logic Bypass via Direct State Manipulation

**Attack Scenario:**
A compromised dependency or malicious code within the codebase directly sets `Session.State` property, bypassing the state machine's transition guards and validation logic. This allows invalid state transitions (e.g., COMPLETED → EXECUTING) that violate invariants.

**Impact Assessment:**
- **Confidentiality:** Low
- **Integrity:** **HIGH** - Invalid state transitions corrupt session semantics
- **Availability:** Medium (may cause crashes from unexpected states)

**Mitigation Strategy:**

1. **Immutable State Property:** Make `State` property private with controlled mutation

```csharp
public sealed class Session
{
    private SessionState _state;
    
    public SessionState State
    {
        get => _state;
        private set => _state = value; // Private setter prevents external mutation
    }
    
    // Only way to change state is through Transition method
    public void Transition(SessionState newState, string reason, ISessionStateMachine stateMachine)
    {
        // Validate transition
        if (!stateMachine.CanTransition(_state, newState))
        {
            throw new InvalidStateTransitionException(
                $"Cannot transition from {_state} to {newState}");
        }
        
        // Apply transition
        _state = newState;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

2. **Transition Guard Enforcement:** Centralize all transition logic in ISessionStateMachine

```csharp
public interface ISessionStateMachine
{
    bool CanTransition(SessionState from, SessionState to);
    void ValidateTransition(SessionState from, SessionState to); // Throws on invalid
    IReadOnlyList<SessionState> GetValidTransitions(SessionState from);
}

public sealed class SessionStateMachine : ISessionStateMachine
{
    private static readonly IReadOnlyDictionary<SessionState, HashSet<SessionState>> _transitionMatrix = new Dictionary<SessionState, HashSet<SessionState>>
    {
        [SessionState.Created] = new() { SessionState.Planning, SessionState.Cancelled },
        [SessionState.Planning] = new() { SessionState.AwaitingApproval, SessionState.Executing, SessionState.Paused, SessionState.Failed, SessionState.Cancelled },
        [SessionState.AwaitingApproval] = new() { SessionState.Executing, SessionState.Paused, SessionState.Cancelled },
        [SessionState.Executing] = new() { SessionState.AwaitingApproval, SessionState.Paused, SessionState.Completed, SessionState.Failed, SessionState.Cancelled },
        [SessionState.Paused] = new() { SessionState.Planning, SessionState.AwaitingApproval, SessionState.Executing, SessionState.Cancelled },
        [SessionState.Completed] = new(), // Terminal state
        [SessionState.Failed] = new(), // Terminal state
        [SessionState.Cancelled] = new(), // Terminal state
    };
    
    public bool CanTransition(SessionState from, SessionState to)
    {
        return _transitionMatrix[from].Contains(to);
    }
    
    public void ValidateTransition(SessionState from, SessionState to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidStateTransitionException(
                $"Invalid state transition: {from} -> {to}. Valid transitions from {from}: {string.Join(", ", _transitionMatrix[from])}");
        }
    }
}
```

3. **Unit Test Coverage:** 100% coverage of invalid transition attempts

```csharp
[Fact]
public void Transition_FromCompleted_ToAnyState_ThrowsInvalidStateTransitionException()
{
    // Arrange
    var session = SessionTestHelpers.CreateCompletedSession();
    var stateMachine = new SessionStateMachine();
    var invalidTargets = new[] { SessionState.Planning, SessionState.Executing, SessionState.Paused };
    
    // Act & Assert
    foreach (var target in invalidTargets)
    {
        var ex = Assert.Throws<InvalidStateTransitionException>(
            () => session.Transition(target, "Should fail", stateMachine));
        
        Assert.Contains("Invalid state transition", ex.Message);
        Assert.Contains(SessionState.Completed.ToString(), ex.Message);
    }
}
```

4. **Defense in Depth:**
   - **Private Setters:** Prevent direct state mutation from outside class
   - **Transition Matrix:** Hardcoded valid transitions (no dynamic logic)
   - **Guard Validation:** Centralized validation in ISessionStateMachine
   - **Unit Tests:** 100% coverage of invalid transitions
   - **Static Analysis:** Use Roslyn analyzers to detect direct state assignments

---

### Threat 5: Denial of Service via Session Exhaustion

**Attack Scenario:**
A malicious script or runaway process creates thousands of sessions rapidly, exhausting database storage and degrading performance. Each session persists state to SQLite, consuming disk space and IOPS. Legitimate sessions experience slow queries and lock contention.

**Impact Assessment:**
- **Confidentiality:** Low
- **Integrity:** Low
- **Availability:** **HIGH** - System becomes unusable due to resource exhaustion

**Mitigation Strategy:**

1. **Session Rate Limiting:** Enforce maximum session creation rate per user/process

```csharp
public sealed class RateLimitedSessionService : ISessionService
{
    private readonly ISessionService _inner;
    private readonly IRateLimiter _rateLimiter;
    
    public async Task<Session> CreateSessionAsync(
        string taskDescription,
        CancellationToken ct)
    {
        // Check rate limit (max 10 sessions per minute per user)
        var userId = _userContext.CurrentUserId;
        var allowed = await _rateLimiter.TryAcquireAsync(
            key: $"session-create:{userId}",
            maxRequests: 10,
            window: TimeSpan.FromMinutes(1),
            ct);
        
        if (!allowed)
        {
            throw new RateLimitExceededException(
                "Session creation rate limit exceeded. Max 10 sessions per minute.");
        }
        
        return await _inner.CreateSessionAsync(taskDescription, ct);
    }
}
```

2. **Session Quota Enforcement:** Limit maximum concurrent sessions per user

```csharp
public sealed class SessionQuotaService : ISessionQuotaService
{
    private readonly IRunStateStore _store;
    
    public async Task<bool> CanCreateSessionAsync(string userId, CancellationToken ct)
    {
        var activeSessions = await _store.ListAsync(
            new SessionFilter(State: SessionState.Executing),
            ct);
        
        var userSessions = activeSessions.Count(s => s.UserId == userId);
        const int MaxConcurrentSessions = 5;
        
        return userSessions < MaxConcurrentSessions;
    }
}
```

3. **Automatic Session Cleanup:** Background job removes abandoned sessions

```csharp
public sealed class SessionCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(1), ct);
            await CleanupAbandonedSessionsAsync(ct);
        }
    }
    
    private async Task CleanupAbandonedSessionsAsync(CancellationToken ct)
    {
        var threshold = DateTimeOffset.UtcNow.AddDays(-7);
        
        // Find sessions not updated in 7 days
        var abandoned = await _store.ListAsync(
            new SessionFilter(
                State: SessionState.Paused,
                Before: threshold),
            ct);
        
        foreach (var session in abandoned)
        {
            _logger.LogInformation(
                "Cleaning up abandoned session {SessionId} (last updated: {UpdatedAt})",
                session.Id, session.UpdatedAt);
            
            await _store.DeleteAsync(session.Id, ct);
        }
    }
}
```

4. **Defense in Depth:**
   - **Rate Limiting:** Max 10 sessions/minute per user
   - **Quota Enforcement:** Max 5 concurrent sessions per user
   - **Automatic Cleanup:** Remove sessions idle > 7 days
   - **Monitoring:** Alert on session count > 1000 or creation rate > 100/min
   - **Database Limits:** SQLite max_page_count prevents unbounded growth

---

## Best Practices

### State Machine Design

**BP-001: Use Finite State Automaton Semantics**
- **Reason:** Formal state machine semantics prevent ambiguous behavior and enable verification
- **Example:** Define explicit transition matrix with all valid (from → to) pairs
- **Anti-pattern:** Using boolean flags (`isRunning`, `isPaused`) instead of explicit state enum

**BP-002: Enforce Immutability of State Transitions**
- **Reason:** Immutable state transitions prevent race conditions and simplify debugging
- **Example:** State can only change via `Transition()` method, never direct assignment
- **Anti-pattern:** Public setter allowing `session.State = SessionState.Completed` from any caller

**BP-003: Make Terminal States Truly Terminal**
- **Reason:** Prevents logical errors like resuming completed sessions
- **Example:** COMPLETED, FAILED, CANCELLED states have no valid outbound transitions
- **Anti-pattern:** Allowing COMPLETED → EXECUTING transition for "session replay"

### Event Sourcing

**BP-004: Record Every State Transition as Immutable Event**
- **Reason:** Complete audit trail enables debugging, compliance, and recovery
- **Example:** Every `Transition()` call appends to immutable event log
- **Anti-pattern:** Only storing current state, losing transition history

**BP-005: Include Reason in Every Event**
- **Reason:** Human-readable reasons enable faster debugging and audit comprehension
- **Example:** `"High-risk file modification detected: config/production.yml"`
- **Anti-pattern:** Generic reasons like `"Transition occurred"` or empty strings

**BP-006: Sign Events Cryptographically**
- **Reason:** Prevents audit trail tampering and ensures compliance integrity
- **Example:** HMAC-SHA256 signature computed over event fields using secret key
- **Anti-pattern:** Unsigned events vulnerable to database manipulation

### Persistence Strategy

**BP-007: Abstract Persistence Behind IRunStateStore Interface**
- **Reason:** Enables testing with in-memory stores and production database swaps
- **Example:** SessionStateMachine depends on IRunStateStore, not SQLiteRunStateStore
- **Anti-pattern:** Direct SQLiteConnection usage in domain/application layers

**BP-008: Persist Before Completing Transition**
- **Reason:** Prevents data loss if process crashes between state change and persistence
- **Example:** Call `await _store.SaveAsync(session)` before returning from `Transition()`
- **Anti-pattern:** Fire-and-forget persistence (e.g., background queue)

**BP-009: Use Transactions for Multi-Step Persistence**
- **Reason:** Maintains consistency between session state and event log
- **Example:** Update sessions table and insert into session_events within single transaction
- **Anti-pattern:** Separate non-transactional updates that can diverge

### Concurrency Control

**BP-010: Acquire Lock Before Modifying Session**
- **Reason:** Prevents data corruption from concurrent writes
- **Example:** `await lock.TryAcquireAsync(timeout: 60s)` before loading session
- **Anti-pattern:** Optimistic concurrency without conflict detection

**BP-011: Release Lock in Finally Block**
- **Reason:** Ensures lock release even if exception occurs
- **Example:** Use `try/finally` or `using` statement for lock lifecycle
- **Anti-pattern:** Lock release in normal flow only, leaked on exception

**BP-012: Implement Stale Lock Detection**
- **Reason:** Prevents indefinite blocking when lock-holding process crashes
- **Example:** Check if lock-holding process ID still exists before waiting
- **Anti-pattern:** Infinite wait for lock held by dead process

### Resume Behavior

**BP-013: Validate Environment Before Resume**
- **Reason:** Prevents cryptic errors from changed environment (missing files, etc.)
- **Example:** Check Git branch, verify .acode directory exists, validate dependencies
- **Anti-pattern:** Blindly resuming without environment validation

**BP-014: Skip Completed Work on Resume**
- **Reason:** Avoids wasted compute and potential side effects from re-execution
- **Example:** Query session hierarchy, filter to incomplete tasks/steps only
- **Anti-pattern:** Re-executing completed steps "for safety"

**BP-015: Log Resume Context**
- **Reason:** Helps users understand what's being resumed and from where
- **Example:** Log: "Resuming session {id} from PAUSED state (14/30 steps completed)"
- **Anti-pattern:** Silent resume with no user feedback

### Error Handling

**BP-016: Use Domain-Specific Exceptions**
- **Reason:** Enables precise error handling and user-friendly messages
- **Example:** `InvalidStateTransitionException`, `SessionLockedException`, `SessionTamperedException`
- **Anti-pattern:** Generic `InvalidOperationException` or `Exception` for domain errors

**BP-017: Include Context in Exception Messages**
- **Reason:** Faster debugging with actionable error details
- **Example:** `"Cannot transition from {from} to {to}. Valid transitions: {valid}"`
- **Anti-pattern:** `"Invalid transition"` with no details

**BP-018: Log All State Machine Errors**
- **Reason:** Enables post-mortem analysis of failed sessions
- **Example:** Log transition failures with session ID, from/to states, reason, stack trace
- **Anti-pattern:** Swallowing exceptions with empty catch blocks

### Testing Strategy

**BP-019: Test Every Valid Transition**
- **Reason:** Ensures state machine implements specification correctly
- **Example:** 8 states × average 3 valid transitions = 24+ test cases
- **Anti-pattern:** Testing only "happy path" transitions

**BP-020: Test Every Invalid Transition**
- **Reason:** Verifies guards properly reject forbidden transitions
- **Example:** Test COMPLETED → EXECUTING throws InvalidStateTransitionException
- **Anti-pattern:** Assuming invalid transitions "just won't happen"

**BP-021: Use Property-Based Testing for Invariants**
- **Reason:** Discovers edge cases that example-based tests miss
- **Example:** Generate random transition sequences, verify state invariants hold
- **Anti-pattern:** Only example-based unit tests

### Observability

**BP-022: Emit Structured Log for Every Transition**
- **Reason:** Enables querying and alerting on state machine behavior
- **Example:** `{ "event": "session_transition", "session_id": "abc", "from": "Planning", "to": "Executing", "duration_ms": 15 }`
- **Anti-pattern:** Unstructured log messages like "Transitioned session"

**BP-023: Track Transition Latency Metrics**
- **Reason:** Detects performance regressions in state machine or persistence
- **Example:** Histogram of `session_transition_duration_ms` by (from, to) pair
- **Anti-pattern:** No metrics, relying only on logs

**BP-024: Monitor Session Count by State**
- **Reason:** Detects anomalies like session leaks or stuck sessions
- **Example:** Gauge of `active_sessions{state="Executing"}` updated every minute
- **Anti-pattern:** No visibility into session distribution

---

## Troubleshooting

### Problem 1: "Invalid State Transition" Exception

**Symptoms:**
- Application throws `InvalidStateTransitionException` during session operation
- Error message shows attempted transition (e.g., "Cannot transition from Completed to Executing")
- Session appears stuck in terminal state

**Possible Causes:**
1. Code attempting invalid transition (logic bug)
2. Session state corrupted in database
3. Race condition with concurrent process
4. Resume logic attempting to replay completed session

**Diagnosis:**

```bash
# View session current state and history
acode session show <session-id> --verbose

# Check event log for unexpected transitions
acode session history <session-id> --format json | jq '.events[] | select(.to_state == "Completed")'

# Verify transition matrix allows this transition
acode session transitions --from Completed
```

**Solutions:**

1. **Fix Application Logic:**
   - Review code calling `session.Transition()`
   - Verify transition guards implemented correctly
   - Add unit test covering this specific transition

2. **Repair Corrupted Session State:**
   ```bash
   # If database corruption suspected, validate checksums
   sqlite3 .acode/workspace.db "SELECT id, state FROM sessions WHERE id = '<session-id>';"
   
   # Manually correct state if needed (DANGEROUS - breaks audit trail)
   sqlite3 .acode/workspace.db "UPDATE sessions SET state = 'Failed' WHERE id = '<session-id>';"
   ```

3. **Prevent Race Conditions:**
   - Ensure session lock is acquired before state transitions
   - Check for multiple processes operating on same session
   - Review lock acquisition logs for conflicts

4. **Fix Resume Logic:**
   - Verify resume only targets non-terminal states (Created, Planning, Paused)
   - Add validation: `if (session.State.IsTerminal()) throw new InvalidOperationException("Cannot resume terminal session")`

**Prevention:**
- Always validate transition before attempting: `if (!stateMachine.CanTransition(from, to)) return;`
- Use terminal state check: `if (state == SessionState.Completed || state == SessionState.Failed) return;`
- Implement transition logging to audit trail

---

### Problem 2: Session Lock Acquisition Timeout

**Symptoms:**
- `acode run` command fails with "Session lock acquisition timeout"
- Error mentions lock held by PID XXXXX
- Session appears to be running but no activity observed

**Possible Causes:**
1. Concurrent session attempt by different process
2. Stale lock from crashed process
3. Lock file permissions preventing cleanup
4. Deadlock between two sessions

**Diagnosis:**

```bash
# Check if lock-holding process exists
ps aux | grep <pid-from-error>

# Verify lock file exists and check permissions
ls -la .acode/locks/<session-id>.lock
cat .acode/locks/<session-id>.lock

# Check lock expiration timestamp
jq '.expires_at' .acode/locks/<session-id>.lock

# List all active locks
ls -lh .acode/locks/
```

**Solutions:**

1. **Wait for Concurrent Session to Complete:**
   - If lock-holding process is alive, wait or use `acode session status --watch`
   - If urgent, cancel existing session: `acode session cancel <session-id>`

2. **Break Stale Lock:**
   ```bash
   # Verify process is dead
   ps aux | grep <pid> || echo "Process dead"
   
   # Force release lock
   rm .acode/locks/<session-id>.lock
   
   # Verify lock removed
   ls .acode/locks/<session-id>.lock 2>&1 | grep "No such file"
   ```

3. **Fix Lock File Permissions:**
   ```bash
   # Set correct permissions on lock directory
   chmod 700 .acode/locks/
   
   # Remove locks with incorrect permissions
   find .acode/locks/ -type f ! -perm 600 -delete
   ```

4. **Enable Automated Stale Lock Cleanup:**
   ```yaml
   # .acode/config.yml
   session:
     lock_timeout: 300s  # 5 minutes
     stale_lock_cleanup: true
     cleanup_interval: 60s
   ```

**Prevention:**
- Enable stale lock cleanup background service
- Set reasonable lock timeout (default 60s)
- Use `acode session list` before starting new session to check for conflicts
- Implement graceful shutdown handling to release locks

---

### Problem 3: Session Checksum Validation Failed

**Symptoms:**
- Application throws `SessionTamperedException` when loading session
- Error message: "Session {id} failed checksum validation"
- Session appears in database but cannot be loaded

**Possible Causes:**
1. Direct database modification by user or external tool
2. Database corruption from disk error
3. Checksum algorithm changed between versions
4. Concurrent write without proper locking

**Diagnosis:**

```bash
# Query session directly from database
sqlite3 .acode/workspace.db "SELECT id, state, updated_at FROM sessions WHERE id = '<session-id>';"

# Check for database corruption
sqlite3 .acode/workspace.db "PRAGMA integrity_check;"

# Review audit logs for suspicious database access
grep "DatabaseAccess" .acode/logs/audit.log | grep "<session-id>"

# Check if checksum column exists (migration issue)
sqlite3 .acode/workspace.db "PRAGMA table_info(sessions);" | grep checksum
```

**Solutions:**

1. **Recompute Checksum (If Legitimate Change):**
   ```bash
   # Temporarily disable checksum validation
   # In appsettings.json:
   # "Acode:Session:ChecksumValidation": false
   
   acode session repair-checksums
   
   # Re-enable validation
   ```

2. **Restore from Backup:**
   ```bash
   # Restore database from backup
   cp .acode/backups/workspace.db.2025-01-05T14-30-00Z .acode/workspace.db
   
   # Verify restoration
   acode session show <session-id>
   ```

3. **Delete Corrupted Session:**
   ```bash
   # If session unrecoverable, delete
   sqlite3 .acode/workspace.db "DELETE FROM sessions WHERE id = '<session-id>';"
   sqlite3 .acode/workspace.db "DELETE FROM session_events WHERE session_id = '<session-id>';"
   ```

4. **Fix Database Corruption:**
   ```bash
   # Dump and restore database
   sqlite3 .acode/workspace.db ".dump" > dump.sql
   rm .acode/workspace.db
   sqlite3 .acode/workspace.db < dump.sql
   rm dump.sql
   ```

**Prevention:**
- Never modify `.acode/workspace.db` directly - use CLI commands only
- Enable automatic database backups: `session.backup_enabled: true`
- Use database-level checksums: `PRAGMA data_version` before/after operations
- Monitor audit logs for unexpected database access

---

### Problem 4: Session Resume Fails with "Environment Validation Error"

**Symptoms:**
- `acode session resume <id>` fails with environment validation error
- Error mentions missing file, incorrect Git branch, or configuration mismatch
- Session was paused successfully but cannot be resumed

**Possible Causes:**
1. Git branch changed since session started
2. Files deleted or moved after session paused
3. `.acode/config.yml` modified incompatibly
4. Dependencies updated (e.g., npm install, dotnet restore)

**Diagnosis:**

```bash
# Check session metadata for environment expectations
acode session show <session-id> --format json | jq '.metadata.environment'

# Compare current Git branch to session start
echo "Current: $(git branch --show-current)"
acode session show <session-id> --format json | jq -r '.metadata.git_branch'

# Verify files referenced by session still exist
acode session show <session-id> --format json | jq -r '.tasks[].steps[].tool_calls[] | select(.tool=="read_file") | .args.path' | xargs -I{} test -f {} && echo "Exists: {}" || echo "MISSING: {}"

# Check configuration compatibility
diff <(acode config show --format json | jq -S) <(acode session show <session-id> --format json | jq -S '.metadata.config')
```

**Solutions:**

1. **Restore Git Branch:**
   ```bash
   # Switch to branch where session started
   ORIGINAL_BRANCH=$(acode session show <session-id> --format json | jq -r '.metadata.git_branch')
   git checkout "$ORIGINAL_BRANCH"
   
   # Resume session
   acode session resume <session-id>
   ```

2. **Skip Environment Validation (Force Resume):**
   ```bash
   # Resume with --force flag (bypasses validation)
   acode session resume <session-id> --force
   
   # Warning: May cause unexpected behavior if environment diverged
   ```

3. **Recreate Missing Files:**
   - Restore deleted files from Git history: `git checkout HEAD~1 -- path/to/file`
   - If files intentionally deleted, cancel session and start fresh

4. **Update Session Metadata:**
   ```bash
   # Update session to current environment (dangerous)
   acode session update-environment <session-id>
   
   # This updates Git branch, config snapshot, file checksums
   ```

**Prevention:**
- Avoid changing Git branches during active session
- Commit or stash changes before pausing session
- Document environment requirements in session metadata
- Use `acode session pause --checkpoint` to save environment snapshot

---

### Problem 5: High Session Transition Latency (> 500ms)

**Symptoms:**
- Session state transitions take > 500ms (target: < 50ms)
- `acode session list` command slow (> 2 seconds)
- Database file size large (> 1 GB)

**Possible Causes:**
1. Database file fragmentation
2. Missing indexes on session queries
3. Large number of events per session (> 10,000)
4. Slow disk I/O (network drive, spinning disk)

**Diagnosis:**

```bash
# Check database file size
ls -lh .acode/workspace.db

# Analyze table sizes
sqlite3 .acode/workspace.db "SELECT name, SUM(pgsize) as size FROM dbstat GROUP BY name ORDER BY size DESC;"

# Check index usage
sqlite3 .acode/workspace.db "EXPLAIN QUERY PLAN SELECT * FROM sessions WHERE state = 'Executing';"

# Count events per session
sqlite3 .acode/workspace.db "SELECT session_id, COUNT(*) as event_count FROM session_events GROUP BY session_id ORDER BY event_count DESC LIMIT 10;"

# Measure query performance
time sqlite3 .acode/workspace.db "SELECT * FROM sessions WHERE id = '<session-id>';"
```

**Solutions:**

1. **Vacuum Database:**
   ```bash
   # Reclaim unused space and defragment
   sqlite3 .acode/workspace.db "VACUUM;"
   
   # Verify improvement
   ls -lh .acode/workspace.db
   ```

2. **Add Missing Indexes:**
   ```sql
   -- Create indexes for common queries
   CREATE INDEX IF NOT EXISTS idx_sessions_state_updated 
       ON sessions(state, updated_at);
   
   CREATE INDEX IF NOT EXISTS idx_events_timestamp 
       ON session_events(timestamp);
   
   -- Verify index usage
   EXPLAIN QUERY PLAN SELECT * FROM sessions WHERE state = 'Executing' ORDER BY updated_at DESC;
   ```

3. **Archive Old Sessions:**
   ```bash
   # Export sessions older than 90 days
   acode session export --before 90d --output archive-2025-01.jsonl
   
   # Delete archived sessions
   acode session cleanup --before 90d --confirm
   
   # Vacuum after deletion
   sqlite3 .acode/workspace.db "VACUUM;"
   ```

4. **Migrate to PostgreSQL:**
   ```bash
   # For large installations (> 1000 sessions), use PostgreSQL
   # Update .acode/config.yml:
   database:
     provider: postgres
     connection_string: "Host=localhost;Database=acode;Username=acode;Password=***"
   
   # Run migration
   acode db migrate --from sqlite --to postgres
   ```

**Prevention:**
- Enable automatic session cleanup: `session.retention_days: 90`
- Monitor database size and set alerts at 500 MB
- Use write-ahead logging: `PRAGMA journal_mode=WAL;`
- Schedule weekly `VACUUM` during off-hours
- Consider PostgreSQL for high-throughput installations

---

## User Manual Documentation

### Overview

The Run Session State Machine manages the lifecycle of agent runs. Understanding session states helps with debugging, resuming interrupted work, and monitoring progress.

### Session States

```
CREATED → PLANNING → AWAITING_APPROVAL → EXECUTING → COMPLETED
                   ↘                    ↗
                    → ─────────────────→
                    
Any State → PAUSED (interrupt)
Any State → CANCELLED (user cancel)
Any State → FAILED (error)
```

### State Descriptions

| State | Description | Duration |
|-------|-------------|----------|
| CREATED | Session initialized | < 1s |
| PLANNING | Agent analyzing task | Seconds-minutes |
| AWAITING_APPROVAL | Waiting for user | Until response |
| EXECUTING | Agent performing actions | Varies |
| PAUSED | User interrupted | Until resume |
| COMPLETED | Successfully finished | Final |
| FAILED | Error occurred | Final |
| CANCELLED | User cancelled | Final |

### Session Hierarchy

```
Session: "Add input validation"
├── Task: "Analyze existing code"
│   ├── Step: "Read login form"
│   │   └── ToolCall: read_file("src/login.ts")
│   └── Step: "Identify validation points"
│       └── ToolCall: semantic_search("validation")
├── Task: "Implement validation"
│   ├── Step: "Add email validator"
│   │   ├── ToolCall: read_file("src/validators.ts")
│   │   └── ToolCall: write_file("src/validators.ts")
│   └── Step: "Add to form"
│       └── ToolCall: write_file("src/login.ts")
└── Task: "Test changes"
    └── Step: "Run tests"
        └── ToolCall: run_command("npm test")
```

### CLI Commands

```bash
# View current session status
$ acode status
Session: abc123
State: EXECUTING
Task: 2/3 "Implement validation"
Step: 1/2 "Add email validator"
Progress: 45%

# List all sessions
$ acode session list
ID          STATE       CREATED              TASK
abc123      EXECUTING   2024-01-15 10:30    Add input validation
def456      COMPLETED   2024-01-14 15:45    Fix login bug
ghi789      FAILED      2024-01-14 09:00    Refactor module

# View session details
$ acode session show abc123
Session: abc123
State: EXECUTING
Created: 2024-01-15T10:30:00Z
Updated: 2024-01-15T10:35:00Z
Task: Add input validation

History:
  10:30:00 CREATED
  10:30:01 PLANNING
  10:30:15 AWAITING_APPROVAL
  10:30:45 EXECUTING

Tasks:
  ✓ Analyze existing code
  ● Implement validation (in progress)
  ○ Test changes

# Resume interrupted session
$ acode resume abc123
Resuming session abc123...
Last state: EXECUTING
Resuming from: Task 2, Step 1
```

### Interruption and Resume

#### Graceful Interruption

Press Ctrl+C during execution:

```
$ acode run "Add validation"
[Planning] Analyzing codebase...
[Executing] Step 1/5: Reading files...
^C
Interrupted. Saving state...
Session abc123 paused at Step 1.
Resume with: acode resume abc123
```

#### Crash Recovery

If the process crashes, state is preserved:

```
$ acode resume
Found interrupted session: abc123
Last state: EXECUTING at Step 2
Resume? [Y/n] y
Continuing from Step 2...
```

### Configuration

```yaml
# .agent/config.yml
session:
  # Lock timeout in seconds
  lock_timeout_seconds: 60
  
  # Session retention in days
  retention_days: 30
  
  # Maximum concurrent sessions
  max_concurrent: 1
  
  # Checkpoint interval (steps)
  checkpoint_interval: 5
  
  # Stale lock detection (seconds)
  stale_lock_seconds: 300
```

### Persistence

Sessions are stored in the workspace database:

```
.agent/
├── workspace.db          # SQLite database
└── config.yml
```

Database tables:
- `sessions` - Session metadata and current state
- `session_events` - Transition history
- `tasks` - Task records
- `steps` - Step records  
- `tool_calls` - Tool call records

### Troubleshooting

#### Stale Lock

**Problem:** "Session locked by another process"

**Solutions:**
1. Check for running Acode processes
2. Wait for lock timeout (60s)
3. Force release: `acode session unlock abc123`

#### Corrupted State

**Problem:** Session in inconsistent state

**Solutions:**
1. Check event history: `acode session history abc123`
2. Try repair: `acode session repair abc123`
3. Archive and start fresh: `acode session archive abc123`

#### Resume Failures

**Problem:** Cannot resume session

**Possible causes:**
1. Session already completed/failed
2. Environment changed since pause
3. Database corruption

**Solutions:**
1. Check state: `acode session show abc123`
2. Verify environment
3. Run database check: `acode db check`

---

## Acceptance Criteria

### Session Lifecycle

- [ ] AC-001: UUID v7 IDs generated
- [ ] AC-002: Creation timestamp recorded
- [ ] AC-003: Update timestamp maintained
- [ ] AC-004: Task description stored
- [ ] AC-005: Current state tracked
- [ ] AC-006: History maintained
- [ ] AC-007: Queryable by ID
- [ ] AC-008: Active sessions listable

### States

- [ ] AC-009: CREATED state works
- [ ] AC-010: PLANNING state works
- [ ] AC-011: AWAITING_APPROVAL state works
- [ ] AC-012: EXECUTING state works
- [ ] AC-013: PAUSED state works
- [ ] AC-014: COMPLETED state works
- [ ] AC-015: FAILED state works
- [ ] AC-016: CANCELLED state works

### Transitions

- [ ] AC-017: CREATED → PLANNING works
- [ ] AC-018: PLANNING → AWAITING_APPROVAL works
- [ ] AC-019: PLANNING → EXECUTING works
- [ ] AC-020: AWAITING_APPROVAL → EXECUTING works
- [ ] AC-021: AWAITING_APPROVAL → CANCELLED works
- [ ] AC-022: EXECUTING → AWAITING_APPROVAL works
- [ ] AC-023: EXECUTING → COMPLETED works
- [ ] AC-024: EXECUTING → FAILED works
- [ ] AC-025: Any → PAUSED works
- [ ] AC-026: PAUSED → resume works
- [ ] AC-027: Any → CANCELLED works
- [ ] AC-028: Invalid transitions rejected

### Guards

- [ ] AC-029: Preconditions validated
- [ ] AC-030: Invalid rejected
- [ ] AC-031: Failures logged
- [ ] AC-032: Guards synchronous
- [ ] AC-033: No side effects

### Events

- [ ] AC-034: Transitions emit events
- [ ] AC-035: Events immutable
- [ ] AC-036: Events timestamped
- [ ] AC-037: Previous state included
- [ ] AC-038: New state included
- [ ] AC-039: Reason included
- [ ] AC-040: Events persistable

### Hierarchy

- [ ] AC-041: Session contains Tasks
- [ ] AC-042: Task contains Steps
- [ ] AC-043: Step contains ToolCalls
- [ ] AC-044: Each level has state
- [ ] AC-045: Parent derives from children
- [ ] AC-046: Hierarchy queryable

### Persistence

- [ ] AC-047: Uses IRunStateStore
- [ ] AC-048: Persists to SQLite
- [ ] AC-049: Syncs to PostgreSQL if configured
- [ ] AC-050: Persist before transition completes
- [ ] AC-051: Persist failure aborts transition
- [ ] AC-052: Transactional persist
- [ ] AC-053: Recovery restores state

### Locking

- [ ] AC-054: Active sessions locked
- [ ] AC-055: Concurrent access prevented
- [ ] AC-056: Lock timeout configurable
- [ ] AC-057: Stale locks detected
- [ ] AC-058: Stale locks releasable
- [ ] AC-059: Locks released on exit

### Resume

- [ ] AC-060: Finds last checkpoint
- [ ] AC-061: Skips completed steps
- [ ] AC-062: Retries incomplete steps
- [ ] AC-063: Restores context
- [ ] AC-064: Validates environment
- [ ] AC-065: Logs what's resumed

### Performance

- [ ] AC-066: Transition < 50ms
- [ ] AC-067: Persist < 100ms
- [ ] AC-068: Query by ID < 10ms
- [ ] AC-069: Resume < 500ms
- [ ] AC-070: Lock acquisition < 100ms

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Session/
├── SessionTests.cs
│   ├── Should_Generate_UUID7_Id()
│   ├── Should_Track_Timestamps()
│   └── Should_Store_Task_Description()
│
├── StateMachineTests.cs
│   ├── Should_Start_In_Created_State()
│   ├── Should_Transition_Created_To_Planning()
│   ├── Should_Reject_Invalid_Transition()
│   ├── Should_Transition_To_Paused_From_Any_Active()
│   └── Should_Resume_To_Previous_State()
│
├── TransitionGuardTests.cs
│   ├── Should_Validate_Preconditions()
│   ├── Should_Reject_Without_Preconditions()
│   └── Should_Log_Guard_Failures()
│
├── EventTests.cs
│   ├── Should_Emit_Event_On_Transition()
│   ├── Should_Include_All_Fields()
│   └── Should_Be_Immutable()
│
└── HierarchyTests.cs
    ├── Should_Nest_Tasks_In_Session()
    ├── Should_Nest_Steps_In_Task()
    └── Should_Derive_Parent_State()
```

### Integration Tests

```
Tests/Integration/Session/
├── PersistenceTests.cs
│   ├── Should_Persist_Session_To_SQLite()
│   ├── Should_Recover_After_Crash()
│   └── Should_Sync_To_PostgreSQL()
│
├── LockingTests.cs
│   ├── Should_Acquire_Lock()
│   ├── Should_Block_Concurrent_Access()
│   └── Should_Detect_Stale_Lock()
│
└── ResumeTests.cs
    ├── Should_Resume_From_Checkpoint()
    ├── Should_Skip_Completed_Steps()
    └── Should_Restore_Context()
```

### E2E Tests

```
Tests/E2E/Session/
├── FullLifecycleTests.cs
│   ├── Should_Complete_Full_Session()
│   ├── Should_Handle_Interruption()
│   └── Should_Resume_Successfully()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| State transition | 25ms | 50ms |
| Persist operation | 50ms | 100ms |
| Query by ID | 5ms | 10ms |
| Resume session | 250ms | 500ms |
| Lock acquisition | 50ms | 100ms |

### Regression Tests

- State machine after new state added
- Persistence after schema migration
- Resume after checkpoint format change

---

## User Verification Steps

### Scenario 1: Create Session

1. Run `acode run "task"`
2. Run `acode status`
3. Verify: Session ID shown
4. Verify: State is active

### Scenario 2: View Session

1. Run `acode session show <id>`
2. Verify: Details displayed
3. Verify: History shown

### Scenario 3: List Sessions

1. Run `acode session list`
2. Verify: All sessions listed
3. Verify: States shown

### Scenario 4: Interrupt and Resume

1. Start `acode run "task"`
2. Press Ctrl+C
3. Verify: Session paused
4. Run `acode resume`
5. Verify: Resumes from checkpoint

### Scenario 5: Crash Recovery

1. Start `acode run "task"`
2. Kill process (kill -9)
3. Run `acode resume`
4. Verify: Session resumed

### Scenario 6: Concurrent Access

1. Start `acode run "task"` in terminal 1
2. Try `acode status` in terminal 2
3. Verify: Status works (read-only)
4. Try `acode run "task2"` in terminal 2
5. Verify: Blocked or queued

### Scenario 7: Stale Lock

1. Start session, kill -9 process
2. Wait for lock timeout
3. Run `acode resume`
4. Verify: Stale lock detected and released

### Scenario 8: Session History

1. Complete a session
2. Run `acode session history <id>`
3. Verify: All transitions shown
4. Verify: Timestamps correct

### Scenario 9: Persistence

1. Complete a session
2. Run `acode db check`
3. Verify: Session in database
4. Verify: Events in database

### Scenario 10: Cancel Session

1. Start `acode run "task"`
2. Press Ctrl+C then confirm cancel
3. Verify: State is CANCELLED
4. Verify: Cleanup performed

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Sessions/
│   ├── Session.cs
│   ├── SessionId.cs
│   ├── SessionState.cs
│   ├── SessionTask.cs
│   ├── SessionStep.cs
│   ├── ToolCall.cs
│   └── SessionEvent.cs
│
src/AgenticCoder.Application/
├── Sessions/
│   ├── IRunStateStore.cs
│   ├── ISessionStateMachine.cs
│   ├── SessionStateMachine.cs
│   ├── TransitionGuard.cs
│   ├── SessionService.cs
│   └── ResumeService.cs
│
src/AgenticCoder.Infrastructure/
├── Persistence/
│   ├── SQLite/
│   │   └── SQLiteRunStateStore.cs
│   └── PostgreSQL/
│       └── PostgreSQLRunStateStore.cs
└── Locking/
    ├── ISessionLock.cs
    └── FileSessionLock.cs
```

### Session Entity

```csharp
namespace AgenticCoder.Domain.Sessions;

public sealed class Session
{
    public SessionId Id { get; }
    public string TaskDescription { get; }
    public SessionState State { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    
    private readonly List<SessionTask> _tasks = new();
    private readonly List<SessionEvent> _events = new();
    
    public IReadOnlyList<SessionTask> Tasks => _tasks;
    public IReadOnlyList<SessionEvent> Events => _events;
    
    public void Transition(SessionState newState, string reason);
}
```

### SessionState Enum

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
```

### ISessionStateMachine

```csharp
namespace AgenticCoder.Application.Sessions;

public interface ISessionStateMachine
{
    bool CanTransition(SessionState from, SessionState to);
    void Transition(Session session, SessionState to, string reason);
    IReadOnlyList<SessionState> GetValidTransitions(SessionState from);
}
```

### IRunStateStore

```csharp
namespace AgenticCoder.Application.Sessions;

public interface IRunStateStore
{
    Task<Session?> GetAsync(SessionId id, CancellationToken ct);
    Task SaveAsync(Session session, CancellationToken ct);
    Task<IReadOnlyList<Session>> ListAsync(SessionFilter filter, CancellationToken ct);
    Task<IReadOnlyList<SessionEvent>> GetEventsAsync(SessionId id, CancellationToken ct);
}

public sealed record SessionFilter(
    SessionState? State = null,
    DateTimeOffset? After = null,
    DateTimeOffset? Before = null,
    int? Limit = null,
    int? Offset = null);
```

### ISessionLock

```csharp
namespace AgenticCoder.Infrastructure.Locking;

public interface ISessionLock : IAsyncDisposable
{
    SessionId SessionId { get; }
    bool IsHeld { get; }
    DateTimeOffset AcquiredAt { get; }
    
    Task<bool> TryAcquireAsync(TimeSpan timeout, CancellationToken ct);
    Task ReleaseAsync(CancellationToken ct);
}
```

### Transition Matrix

```
From \ To       | Created | Planning | AwaitingApproval | Executing | Paused | Completed | Failed | Cancelled
----------------|---------|----------|------------------|-----------|--------|-----------|--------|----------
Created         |    -    |    ✓     |        -         |     -     |   ✓    |     -     |   ✓    |    ✓
Planning        |    -    |    -     |        ✓         |     ✓     |   ✓    |     -     |   ✓    |    ✓
AwaitingApproval|    -    |    -     |        -         |     ✓     |   ✓    |     -     |   ✓    |    ✓
Executing       |    -    |    -     |        ✓         |     -     |   ✓    |     ✓     |   ✓    |    ✓
Paused          |    -    |    ✓     |        ✓         |     ✓     |   -    |     -     |   -    |    ✓
Completed       |    -    |    -     |        -         |     -     |   -    |     -     |   -    |    -
Failed          |    -    |    -     |        -         |     -     |   -    |     -     |   -    |    -
Cancelled       |    -    |    -     |        -         |     -     |   -    |     -     |   -    |    -
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-SESSION-001 | Invalid state transition |
| ACODE-SESSION-002 | Session not found |
| ACODE-SESSION-003 | Session locked |
| ACODE-SESSION-004 | Persist failed |
| ACODE-SESSION-005 | Resume failed |
| ACODE-SESSION-006 | Lock acquisition failed |

### Logging Fields

```json
{
  "event": "session_transition",
  "session_id": "abc123",
  "from_state": "Planning",
  "to_state": "Executing",
  "reason": "Plan approved",
  "duration_ms": 15
}
```

### Database Schema

```sql
-- Sessions table
CREATE TABLE sessions (
    id TEXT PRIMARY KEY,
    task_description TEXT NOT NULL,
    state TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    metadata TEXT
);

-- Session events table
CREATE TABLE session_events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT NOT NULL,
    from_state TEXT NOT NULL,
    to_state TEXT NOT NULL,
    reason TEXT NOT NULL,
    timestamp TEXT NOT NULL,
    FOREIGN KEY (session_id) REFERENCES sessions(id)
);

-- Session locks table
CREATE TABLE session_locks (
    session_id TEXT PRIMARY KEY,
    process_id INTEGER NOT NULL,
    acquired_at TEXT NOT NULL,
    expires_at TEXT NOT NULL
);

CREATE INDEX idx_sessions_state ON sessions(state);
CREATE INDEX idx_sessions_created ON sessions(created_at);
CREATE INDEX idx_events_session ON session_events(session_id);
```

### Implementation Checklist

1. [ ] Create Session domain entity
2. [ ] Create SessionState enum
3. [ ] Create SessionEvent record
4. [ ] Create session hierarchy (Task, Step, ToolCall)
5. [ ] Implement ISessionStateMachine
6. [ ] Implement transition guards
7. [ ] Create IRunStateStore interface
8. [ ] Implement SQLiteRunStateStore
9. [ ] Implement PostgreSQLRunStateStore
10. [ ] Create ISessionLock interface
11. [ ] Implement FileSessionLock
12. [ ] Implement ResumeService
13. [ ] Add CLI commands (session list, show, etc.)
14. [ ] Write state machine unit tests
15. [ ] Write persistence integration tests
16. [ ] Write locking tests
17. [ ] Add performance benchmarks

### Validation Checklist Before Merge

- [ ] All states implemented and tested
- [ ] All valid transitions work
- [ ] Invalid transitions rejected
- [ ] Events recorded for all transitions
- [ ] Persistence to SQLite works
- [ ] Resume from checkpoint works
- [ ] Locking prevents concurrent access
- [ ] Stale lock detection works
- [ ] Performance targets met
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** Session entity and state machine
2. **Phase 2:** Event recording
3. **Phase 3:** SQLite persistence
4. **Phase 4:** Locking mechanism
5. **Phase 5:** Resume capability
6. **Phase 6:** CLI integration
7. **Phase 7:** PostgreSQL sync

---

**End of Task 011 Specification**