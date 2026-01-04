# Task 040.b: Resume Rules – Identify Resume Point After Crash

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 10 – Core Reliability  
**Dependencies:** Task 040 (Event Log), Task 040.a (Append-Only)  

---

## Description

Task 040.b implements resume rules—the logic that determines exactly where to continue after a crash or interruption. When the agent restarts, it MUST identify the last successfully completed action and resume from the next pending step, avoiding both repeated work and skipped steps.

The resume engine analyzes the event log to find the resume point. A resume point is the event that represents the last fully completed operation. Events that were partially written (if any survived crash) are discarded. The resume engine considers event types, completion markers, and transaction boundaries to identify the precise resume point.

Resume rules handle multiple scenarios: clean shutdown (resume from last event), crash mid-operation (resume from last complete event), crash mid-event-write (WAL handles this), and user interruption (resume from checkpoint). Each scenario has specific detection logic and recovery behavior.

The resume engine also validates state consistency. After identifying the resume point, it verifies that the workspace state (files, DB) matches what the event log expects. If there's a mismatch (e.g., file modified but event not written), the engine raises an error requiring manual resolution.

### Business Value

Robust resume rules provide:
- Zero repeated work
- No skipped steps
- Crash resilience
- User confidence
- Debugging capability

### Scope Boundaries

This task covers resume point identification. Event log storage is Task 040. Append-only semantics are Task 040.a. Ordering guarantees are Task 040.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Event Log | Task 040 | Read events | Find last |
| Append-Only | Task 040.a | Guarantees | Immutable |
| Ordering | Task 040.c | Sequence | Gap check |
| Task Executor | `ITaskExecutor` | Resume point | Continue |
| Workspace | File system | State check | Validation |
| CLI | Task 000 | Resume command | User trigger |
| Retry Policy | Task 041 | On failure | Retry count |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| No events | Empty log | Start fresh | None |
| Corrupted event | Parse error | Skip + warn | Manual check |
| Missing completion | No marker | Re-execute | Possible dup |
| State mismatch | Validation | Error | Manual fix |
| Sequence gap | Gap detection | Error | Manual fix |
| Unknown event type | Type check | Skip | Warning |
| Resume loop | Counter | Error | Human needed |
| Incomplete recovery | Timeout | Error | Manual fix |

### Assumptions

1. **Event log intact**: WAL mode ensures
2. **Completion markers used**: By convention
3. **State can be validated**: Hashes stored
4. **Sequence monotonic**: Task 040.c
5. **Resume is deterministic**: Same result
6. **Single resume point**: One only
7. **Resume is fast**: <1s typical
8. **Manual override exists**: Admin option

### Security Considerations

1. **Resume validation logged**: Audit
2. **State mismatch alert**: Security concern
3. **No arbitrary resume**: Event-based only
4. **Secrets not in resume**: Redacted
5. **Resume count tracked**: Loop detection
6. **Human escalation**: On conflict
7. **Backup before resume**: Optional
8. **Rollback capability**: Emergency

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Resume Point | Where to continue |
| Completion Marker | Event indicating done |
| State Validation | Check workspace matches |
| Resume Engine | Logic for resume |
| Resume Loop | Repeated resume failures |
| Transaction Boundary | Atomic operation edge |
| Recovery Mode | After crash startup |
| Checkpoint | User-initiated save point |

---

## Out of Scope

- Distributed resume
- Multi-agent resume
- Time-travel resume
- Speculative resume
- Resume optimization
- Resume caching

---

## Functional Requirements

### FR-001 to FR-015: Resume Point Identification

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040b-01 | Resume engine MUST find last event | P0 |
| FR-040b-02 | Resume engine MUST check completion | P0 |
| FR-040b-03 | Incomplete events MUST be identified | P0 |
| FR-040b-04 | Incomplete events MUST be skipped | P0 |
| FR-040b-05 | Completion marker MUST be defined | P0 |
| FR-040b-06 | Operation boundaries MUST be detected | P0 |
| FR-040b-07 | Resume point MUST be unique | P0 |
| FR-040b-08 | Resume point MUST be returned | P0 |
| FR-040b-09 | No resume point MUST mean fresh start | P0 |
| FR-040b-10 | Resume query MUST be fast | P0 |
| FR-040b-11 | Resume MUST be deterministic | P0 |
| FR-040b-12 | Same log MUST give same point | P0 |
| FR-040b-13 | Resume MUST be logged | P0 |
| FR-040b-14 | Resume point details MUST be shown | P1 |
| FR-040b-15 | Resume MUST include event ID | P0 |

### FR-016 to FR-030: State Validation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040b-16 | State validation MUST run | P0 |
| FR-040b-17 | File state MUST be checked | P0 |
| FR-040b-18 | DB state MUST be checked | P0 |
| FR-040b-19 | Hash comparison MUST work | P0 |
| FR-040b-20 | Mismatch MUST be detected | P0 |
| FR-040b-21 | Mismatch MUST raise error | P0 |
| FR-040b-22 | Mismatch details MUST be logged | P0 |
| FR-040b-23 | Mismatch MUST require human | P0 |
| FR-040b-24 | Validation MUST be configurable | P1 |
| FR-040b-25 | Strict mode MUST exist | P1 |
| FR-040b-26 | Lenient mode MUST exist | P2 |
| FR-040b-27 | Validation skip MUST be flagged | P1 |
| FR-040b-28 | Skip MUST be audited | P0 |
| FR-040b-29 | Validation timeout MUST exist | P1 |
| FR-040b-30 | Timeout MUST be configurable | P2 |

### FR-031 to FR-045: Crash Scenarios

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040b-31 | Clean shutdown MUST be detected | P0 |
| FR-040b-32 | Crash mid-op MUST be detected | P0 |
| FR-040b-33 | Crash mid-write MUST be handled | P0 |
| FR-040b-34 | User interrupt MUST be detected | P0 |
| FR-040b-35 | Each scenario MUST have handler | P0 |
| FR-040b-36 | Clean shutdown MUST resume normal | P0 |
| FR-040b-37 | Mid-op MUST re-execute last | P0 |
| FR-040b-38 | Mid-write MUST use WAL | P0 |
| FR-040b-39 | Interrupt MUST use checkpoint | P0 |
| FR-040b-40 | Scenario type MUST be logged | P0 |
| FR-040b-41 | Unknown scenario MUST error | P0 |
| FR-040b-42 | Recovery action MUST be logged | P0 |
| FR-040b-43 | Recovery success MUST be verified | P0 |
| FR-040b-44 | Recovery failure MUST escalate | P0 |
| FR-040b-45 | Escalation MUST be to human | P0 |

### FR-046 to FR-060: Resume Execution

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040b-46 | Resume MUST continue from point | P0 |
| FR-040b-47 | Next operation MUST be determined | P0 |
| FR-040b-48 | Task context MUST be restored | P0 |
| FR-040b-49 | Variable state MUST be restored | P0 |
| FR-040b-50 | File state MUST be consistent | P0 |
| FR-040b-51 | Resume counter MUST be tracked | P0 |
| FR-040b-52 | Resume limit MUST exist | P0 |
| FR-040b-53 | Resume limit MUST be configurable | P1 |
| FR-040b-54 | Default limit MUST be 3 | P1 |
| FR-040b-55 | Limit exceeded MUST error | P0 |
| FR-040b-56 | Limit error MUST require human | P0 |
| FR-040b-57 | Resume progress MUST be shown | P0 |
| FR-040b-58 | Resume complete MUST be logged | P0 |
| FR-040b-59 | Post-resume validation MUST run | P0 |
| FR-040b-60 | Validation fail MUST error | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-040b-01 | Resume point find | <100ms | P0 |
| NFR-040b-02 | State validation | <1s | P0 |
| NFR-040b-03 | Context restore | <500ms | P0 |
| NFR-040b-04 | Hash comparison | <10ms/file | P1 |
| NFR-040b-05 | Full resume cycle | <5s | P0 |
| NFR-040b-06 | Log scan (10k events) | <500ms | P1 |
| NFR-040b-07 | Memory for resume | <100MB | P2 |
| NFR-040b-08 | Resume query | <50ms | P0 |
| NFR-040b-09 | Scenario detection | <10ms | P0 |
| NFR-040b-10 | Counter update | <5ms | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-040b-11 | Correct resume | 100% | P0 |
| NFR-040b-12 | No repeated work | 100% | P0 |
| NFR-040b-13 | No skipped work | 100% | P0 |
| NFR-040b-14 | Deterministic | 100% | P0 |
| NFR-040b-15 | Mismatch detection | 100% | P0 |
| NFR-040b-16 | Loop detection | 100% | P0 |
| NFR-040b-17 | Escalation works | 100% | P0 |
| NFR-040b-18 | Cross-platform | All OS | P0 |
| NFR-040b-19 | Thread safety | No races | P0 |
| NFR-040b-20 | Graceful failure | Always | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-040b-21 | Resume logged | Info | P0 |
| NFR-040b-22 | Scenario logged | Info | P0 |
| NFR-040b-23 | Mismatch logged | Error | P0 |
| NFR-040b-24 | Limit exceeded logged | Error | P0 |
| NFR-040b-25 | Metrics: resume count | Counter | P2 |
| NFR-040b-26 | Metrics: resume time | Histogram | P2 |
| NFR-040b-27 | Structured logging | JSON | P0 |
| NFR-040b-28 | CLI output | User-friendly | P0 |
| NFR-040b-29 | Debug details | Verbose mode | P1 |
| NFR-040b-30 | Alert on loop | Critical | P0 |

---

## Acceptance Criteria / Definition of Done

### Resume Point
- [ ] AC-001: Last event found
- [ ] AC-002: Completion checked
- [ ] AC-003: Incomplete identified
- [ ] AC-004: Unique point returned
- [ ] AC-005: Fresh start on empty
- [ ] AC-006: Deterministic result
- [ ] AC-007: Event ID included
- [ ] AC-008: Logged

### State Validation
- [ ] AC-009: Validation runs
- [ ] AC-010: File state checked
- [ ] AC-011: DB state checked
- [ ] AC-012: Mismatch detected
- [ ] AC-013: Mismatch errors
- [ ] AC-014: Human required on mismatch
- [ ] AC-015: Skip audited
- [ ] AC-016: Timeout works

### Crash Scenarios
- [ ] AC-017: Clean shutdown detected
- [ ] AC-018: Mid-op detected
- [ ] AC-019: Mid-write handled
- [ ] AC-020: Interrupt detected
- [ ] AC-021: Each scenario handled
- [ ] AC-022: Recovery logged
- [ ] AC-023: Recovery verified
- [ ] AC-024: Failure escalates

### Resume Execution
- [ ] AC-025: Continues from point
- [ ] AC-026: Next op determined
- [ ] AC-027: Context restored
- [ ] AC-028: Counter tracked
- [ ] AC-029: Limit enforced
- [ ] AC-030: Limit error escalates
- [ ] AC-031: Progress shown
- [ ] AC-032: Post-validation runs

---

## User Verification Scenarios

### Scenario 1: Clean Shutdown Resume
**Persona:** Agent restarting  
**Preconditions:** Clean shutdown occurred  
**Steps:**
1. Start agent
2. Event log read
3. Last event found
4. Resume continues

**Verification Checklist:**
- [ ] Last event correct
- [ ] Resume from next
- [ ] No repeat
- [ ] Logged

### Scenario 2: Crash Mid-Operation
**Persona:** Agent after crash  
**Preconditions:** Crashed during task  
**Steps:**
1. Start agent
2. Incomplete detected
3. Re-execute last op
4. Continue normally

**Verification Checklist:**
- [ ] Incomplete identified
- [ ] Re-executed correctly
- [ ] State consistent
- [ ] Logged

### Scenario 3: State Mismatch
**Persona:** Agent finding inconsistency  
**Preconditions:** File modified externally  
**Steps:**
1. Start agent
2. Resume attempted
3. Mismatch detected
4. Human required

**Verification Checklist:**
- [ ] Mismatch found
- [ ] Details logged
- [ ] Error raised
- [ ] Human needed

### Scenario 4: Resume Loop Detection
**Persona:** Agent repeatedly failing  
**Preconditions:** Same failure 3+ times  
**Steps:**
1. Resume attempted
2. Failure occurs
3. Counter increments
4. Limit exceeded

**Verification Checklist:**
- [ ] Counter tracked
- [ ] Limit enforced
- [ ] Error raised
- [ ] Human escalation

### Scenario 5: Fresh Start
**Persona:** Agent first run  
**Preconditions:** Empty event log  
**Steps:**
1. Start agent
2. No events found
3. Fresh start mode
4. Begin from scratch

**Verification Checklist:**
- [ ] Empty detected
- [ ] Fresh start
- [ ] No error
- [ ] Logged

### Scenario 6: User Interrupt Resume
**Persona:** User resuming after Ctrl+C  
**Preconditions:** Clean interrupt  
**Steps:**
1. Restart agent
2. Checkpoint found
3. Resume from checkpoint
4. Continue

**Verification Checklist:**
- [ ] Interrupt detected
- [ ] Checkpoint used
- [ ] Resume correct
- [ ] No loss

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-040b-01 | Find last event | FR-040b-01 |
| UT-040b-02 | Check completion | FR-040b-02 |
| UT-040b-03 | Identify incomplete | FR-040b-03 |
| UT-040b-04 | Return resume point | FR-040b-08 |
| UT-040b-05 | Empty log | FR-040b-09 |
| UT-040b-06 | Deterministic | FR-040b-11 |
| UT-040b-07 | State validation | FR-040b-16 |
| UT-040b-08 | Hash comparison | FR-040b-19 |
| UT-040b-09 | Mismatch detection | FR-040b-20 |
| UT-040b-10 | Scenario detection | FR-040b-31 |
| UT-040b-11 | Resume counter | FR-040b-51 |
| UT-040b-12 | Limit check | FR-040b-52 |
| UT-040b-13 | Context restore | FR-040b-48 |
| UT-040b-14 | Post-validation | FR-040b-59 |
| UT-040b-15 | Error escalation | FR-040b-44 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-040b-01 | Full resume flow | E2E |
| IT-040b-02 | Crash simulation | FR-040b-32 |
| IT-040b-03 | WAL interaction | FR-040b-38 |
| IT-040b-04 | Event log integration | Task 040 |
| IT-040b-05 | State validation files | FR-040b-17 |
| IT-040b-06 | State validation DB | FR-040b-18 |
| IT-040b-07 | Multiple scenarios | All |
| IT-040b-08 | Resume limit | FR-040b-52 |
| IT-040b-09 | CLI integration | Task 000 |
| IT-040b-10 | Performance | NFR-040b-01 |
| IT-040b-11 | Cross-platform | NFR-040b-18 |
| IT-040b-12 | Logging | NFR-040b-21 |
| IT-040b-13 | Retry integration | Task 041 |
| IT-040b-14 | User interrupt | FR-040b-34 |
| IT-040b-15 | Human escalation | FR-040b-45 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Resume/
│       ├── ResumePoint.cs
│       ├── ResumeScenario.cs
│       └── StateMismatch.cs
├── Acode.Application/
│   └── Resume/
│       ├── IResumeEngine.cs
│       ├── IStateValidator.cs
│       └── ResumeOptions.cs
├── Acode.Infrastructure/
│   └── Resume/
│       ├── ResumeEngine.cs
│       ├── StateValidator.cs
│       └── ScenarioDetector.cs
```

### Key Implementation

```csharp
public class ResumeEngine : IResumeEngine
{
    public async Task<ResumePoint> FindResumePointAsync()
    {
        var lastEvent = await _eventLog.GetLastEventAsync();
        
        if (lastEvent == null)
        {
            _logger.LogInformation("No events found, fresh start");
            return ResumePoint.FreshStart();
        }
        
        var scenario = _scenarioDetector.Detect(lastEvent);
        _logger.LogInformation("Resume scenario: {Scenario}", scenario);
        
        var resumePoint = scenario switch
        {
            ResumeScenario.CleanShutdown => ResumePoint.After(lastEvent),
            ResumeScenario.MidOperation => await FindLastCompleteAsync(),
            ResumeScenario.UserInterrupt => await FindCheckpointAsync(),
            _ => throw new UnknownScenarioException(scenario)
        };
        
        await ValidateStateAsync(resumePoint);
        
        return resumePoint;
    }
    
    private async Task ValidateStateAsync(ResumePoint point)
    {
        var mismatches = await _stateValidator.ValidateAsync(point);
        
        if (mismatches.Any())
        {
            _logger.LogError("State mismatch detected: {Mismatches}", mismatches);
            throw new StateMismatchException(mismatches);
        }
    }
}
```

**End of Task 040.b Specification**
