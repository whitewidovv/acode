# Task 011: Run Session State Machine + Persistence - Implementation Plan

**Status:** In Progress
**Created:** 2026-01-06
**Task Suite:** 011 (parent) + 011a, 011b, 011c (subtasks)
**Complexity:** 34 (parent) + 21 + 21 + 21 = 97 Fibonacci points
**Estimated Duration:** Multi-session (very large task)

---

## Task Suite Overview

### Dependencies Analysis
- Task 010 (CLI Framework) - ✅ COMPLETE (PR #14)
- Task 002 (.agent/config.yml) - ✅ COMPLETE
- Task 050 (Workspace DB) - ❌ NOT YET IMPLEMENTED
- Task 018 (Undo/Checkpoint) - ❌ NOT YET IMPLEMENTED
- Task 026 (Observability) - ❌ NOT YET IMPLEMENTED

**BLOCKER IDENTIFIED:** Task 050 (Workspace DB) is a dependency but not yet implemented. Task 011.b requires IWorkspaceDb abstraction from Task 050.

**DECISION REQUIRED:** Clarify with user whether to:
1. Implement Task 050 first (out of order)
2. Stub/mock IWorkspaceDb for now and integrate later
3. Skip Task 011.b persistence implementation temporarily

---

## Subtask Breakdown

| Subtask | Description | Lines | Status |
|---------|-------------|-------|--------|
| 011a | Run Entities (Domain Model) | 4,249 | ⏸️ Pending decision |
| 011b | Persistence Model (SQLite + Postgres) | 2,224 | ⏸️ Blocked by Task 050 |
| 011c | Resume Behavior + Invariants | 1,700 | ⏸️ Depends on 011a, 011b |
| 011 (parent) | State Machine + Orchestration | 2,335 | ⏸️ Depends on 011a |

**Total:** 10,508 lines of specification

---

## Strategic Approach

### Option A: Implement Task 011a (Domain Model) Only
- Implement Session, Task, Step, ToolCall, Artifact entities
- Pure domain logic, no dependencies on unimplemented tasks
- Provides foundation for other tasks
- Defer 011b (persistence) until Task 050 exists
- Defer 011c (resume) until 011b exists

### Option B: Wait for Task 050
- Block all Task 011 work until Task 050 (Workspace DB) is implemented
- Ensures proper architecture with real abstractions
- Avoids technical debt from mocking

### Option C: Stub Task 050 Interfaces
- Create minimal IWorkspaceDb, ISessionRepository interfaces
- Implement in-memory versions for testing
- Implement full Task 011 suite
- Replace stubs when Task 050 is available

**RECOMMENDATION:** Option A - Implement Task 011a only, defer rest.
- Reason: Domain entities are self-contained and valuable on their own
- Reason: Avoid creating fake abstractions that will be replaced
- Reason: Provides foundation for when Task 050 is available
- Reason: Follows "implement what you can, block on what you can't" principle

---

## Detailed Implementation Plan for Task 011a (Domain Model)

### Phase 1: Project Structure (15 min)
- Create Acode.Domain.RunSessions namespace
- Create Acode.Domain.Tests.RunSessions namespace
- Set up test project references

### Phase 2: Value Objects (30 min)
- SessionId (UUID v7)
- TaskId (UUID v7)
- StepId (UUID v7)
- ToolCallId (UUID v7)
- ArtifactId (UUID v7)
- SessionState enum
- TaskState enum
- StepState enum

### Phase 3: Session Entity (2 hours)
- Session aggregate root
- Session creation
- Session state transitions
- Session hierarchy (Tasks collection)
- Session events
- Session validation
- Session serialization

### Phase 4: Task Entity (1.5 hours)
- Task entity
- Task-Session relationship
- Task state transitions
- Task hierarchy (Steps collection)
- Task validation

### Phase 5: Step Entity (1.5 hours)
- Step entity
- Step-Task relationship
- Step state transitions
- Step hierarchy (ToolCalls collection)
- Step validation

### Phase 6: ToolCall Entity (1 hour)
- ToolCall entity
- ToolCall-Step relationship
- ToolCall metadata
- ToolCall validation

### Phase 7: Artifact Entity (1 hour)
- Artifact value object
- Artifact versioning
- Artifact immutability
- Artifact serialization

### Phase 8: Domain Events (1 hour)
- SessionEvent base
- SessionCreated, SessionStateChanged, SessionCompleted events
- TaskEvent, StepEvent, ToolCallEvent
- Event metadata

### Phase 9: Invariants & Guards (1 hour)
- State transition guards
- Hierarchy invariants
- Validation rules
- Business logic constraints

### Phase 10: Comprehensive Testing (3 hours)
- Unit tests for each entity
- Property-based tests for invariants
- State transition tests
- Serialization tests
- Edge case tests

### Phase 11: Audit & Documentation (1 hour)
- Verify all FR requirements met
- Update audit checklist
- Verify test coverage
- Document any deviations

**Estimated Total for 011a:** ~13-14 hours

---

## Deferred Work (Requires Task 050)

### Task 011b: Persistence Model
- ISessionRepository interface (defined in Application layer)
- SQLite implementation (Infrastructure layer)
- PostgreSQL implementation (Infrastructure layer)
- Outbox pattern
- Migration system
- **BLOCKED:** Needs IWorkspaceDb from Task 050

### Task 011c: Resume Behavior
- Resume command
- Checkpoint loading
- Idempotent replay
- Environment validation
- Session locks
- **BLOCKED:** Needs persistence from 011b

### Task 011 (parent): State Machine
- State machine orchestrator
- Transition executor
- Event dispatcher
- Session manager
- **BLOCKED:** Needs persistence from 011b

---

## Testing Strategy for 011a

### Unit Tests (Domain Layer Only)
- ✅ Test entity construction
- ✅ Test validation rules
- ✅ Test state transitions
- ✅ Test hierarchy navigation
- ✅ Test invariant enforcement
- ✅ Test serialization/deserialization
- ✅ No mocks needed (pure domain logic)

### Property-Based Tests
- ✅ State machine properties
- ✅ Hierarchy invariants
- ✅ Event ordering properties

### Integration Tests
- ⏸️ Defer until 011b (requires persistence)

---

## Success Criteria for 011a

Before marking Task 011a complete:
- [ ] All entity classes implemented (Session, Task, Step, ToolCall, Artifact)
- [ ] All value objects implemented (IDs, states, events)
- [ ] State transition logic implemented
- [ ] Hierarchy relationships implemented
- [ ] Validation rules enforced
- [ ] Domain events defined
- [ ] Unit tests: 100+ tests, all passing
- [ ] Property tests verify invariants
- [ ] Build: 0 warnings, 0 errors
- [ ] XML documentation complete
- [ ] Clean Architecture: Domain layer has no external dependencies
- [ ] Audit checklist complete

---

## Current Status

**Decision Point:** Waiting for user guidance on dependency blocker.

**Question for User:**
Task 011 depends on Task 050 (Workspace DB Foundation) which is not yet implemented. Should I:

1. **Implement Task 011a only** (domain entities - no blockers) and defer 011b/011c/parent until Task 050 exists?
2. **Implement Task 050 first** (out of sequence) to unblock all of Task 011?
3. **Create stub interfaces** for Task 050 and implement full Task 011, replacing stubs later?

**Recommendation:** Option 1 - Implement Task 011a (domain entities) now, defer persistence until Task 050 is available.

---

## Progress Tracking

### Completed
- [x] Read all task specifications
- [x] Identify dependency blocker (Task 050)
- [x] Create implementation plan
- [x] Document decision point for user

### In Progress
- [ ] Awaiting user decision on blocker resolution

### Pending
- [ ] Implement chosen scope
- [ ] Test implementation
- [ ] Audit implementation
- [ ] Create PR

---

## Notes

- This is the largest task suite so far (10,500+ lines of spec vs 3,000 for Task 010)
- Breaking into phases is critical for manageable progress
- Domain model (011a) is self-contained and can proceed independently
- Persistence (011b) and resume (011c) genuinely require Task 050
- Will update this plan as work progresses
