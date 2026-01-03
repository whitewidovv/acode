# Task 021: Artifact Collection + Run Inspection

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 018 (Command Runner), Task 050 (Workspace Database)  

---

## Description

Task 021 implements artifact collection and run inspection. The agent produces artifacts during execution. These artifacts MUST be collected, stored, and inspectable.

Artifacts include build outputs, test results, logs, and diffs. Each run produces a set of artifacts. Runs MUST be queryable by time, status, and type.

Run records MUST be persisted. Each execution MUST create a run record. Records MUST include command, result, duration, and artifact references.

Inspection enables debugging. When something fails, users MUST see what happened. Logs, outputs, and diffs MUST be accessible.

Historical runs MUST be retained per policy. Disk space MUST be managed. Old runs MUST be cleaned up automatically.

Task 021.a defines artifact directory standards. Task 021.b implements run inspection CLI commands. Task 021.c defines the export bundle format.

---

## Functional Requirements

### Run Record

- FR-001: Define RunRecord model with unique ID
- FR-002: Store command executed
- FR-003: Store execution result (success/failure)
- FR-004: Store start time and end time
- FR-005: Store exit code
- FR-006: Store correlation IDs (session, task)
- FR-007: Store artifact references

### Run Store

- FR-008: Define IRunStore interface
- FR-009: Create run record method
- FR-010: Get run by ID method
- FR-011: List runs with filtering method
- FR-012: Delete run method
- FR-013: Persist to workspace database

### Artifact Collection

- FR-014: Collect stdout artifact
- FR-015: Collect stderr artifact
- FR-016: Collect log file artifacts
- FR-017: Collect test result artifacts
- FR-018: Associate artifacts with run

### Run Queries

- FR-019: Query by time range
- FR-020: Query by status (success/failure)
- FR-021: Query by command pattern
- FR-022: Query by session ID
- FR-023: Pagination support

---

## Acceptance Criteria

- [ ] AC-001: Run records MUST be created for each execution
- [ ] AC-002: Artifacts MUST be associated with runs
- [ ] AC-003: Runs MUST be queryable by filters
- [ ] AC-004: Run history MUST persist across restarts
- [ ] AC-005: Artifact content MUST be retrievable

---

## User Manual Documentation

### CLI Commands

```bash
# List recent runs
acode runs list

# List failed runs
acode runs list --status failed

# Show run details
acode runs show <run-id>

# Show run artifacts
acode runs artifacts <run-id>
```

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/Runs/
├── RunRecord.cs
├── IRunStore.cs

src/AgenticCoder.Infrastructure/Runs/
├── RunStore.cs
├── RunRepository.cs
```

### RunRecord Model

```csharp
public record RunRecord
{
    public required Guid Id { get; init; }
    public required string Command { get; init; }
    public required int ExitCode { get; init; }
    public required bool Success { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public required string SessionId { get; init; }
    public required string? TaskId { get; init; }
    public IReadOnlyList<Guid> ArtifactIds { get; init; } = [];
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-RUN-001 | Run not found |
| ACODE-RUN-002 | Artifact not found |

---

**End of Task 021 Specification**