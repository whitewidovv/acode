# Task 020.a: Per-Task Container Strategy

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 020 (Docker Sandbox)  

---

## Description

Task 020.a defines the per-task container strategy. Each agent task MUST execute in a fresh container. Container reuse MUST NOT occur between tasks. This ensures isolation.

Fresh containers prevent state leakage. One task MUST NOT affect another. File modifications in one task MUST NOT persist to the next. Environment changes MUST NOT carry over.

Container naming MUST be consistent. Names MUST include session ID and task ID. This enables tracking and cleanup.

Parallel tasks MUST have separate containers. If multiple tasks run concurrently, each MUST have its own container. No sharing.

Container images MUST match the task requirements. .NET tasks MUST use .NET images. Node tasks MUST use Node images. Multi-language tasks MUST use appropriate images.

Cleanup MUST be automatic. When a task completes, its container MUST be removed. On agent exit, all containers MUST be cleaned up.

---

## Functional Requirements

- FR-001: Each task MUST get a fresh container
- FR-002: Container names MUST follow pattern: `acode-{session}-{task}`
- FR-003: Previous task containers MUST be removed before new task
- FR-004: Parallel tasks MUST have separate containers
- FR-005: Container cleanup MUST occur on task completion
- FR-006: Agent exit MUST trigger cleanup of all containers
- FR-007: Container creation failure MUST abort the task

---

## Acceptance Criteria

- [ ] AC-001: Each task MUST start with clean container
- [ ] AC-002: Container names MUST be traceable
- [ ] AC-003: No state MUST persist between tasks
- [ ] AC-004: Cleanup MUST occur automatically
- [ ] AC-005: Orphaned containers MUST be cleaned on startup

---

## Implementation Prompt

### Container Naming

```csharp
public string GenerateContainerName(string sessionId, string taskId)
{
    return $"acode-{sessionId}-{taskId}";
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-CTN-001 | Container already exists |
| ACODE-CTN-002 | Cleanup failed |

---

**End of Task 020.a Specification**