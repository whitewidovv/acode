# Task 041.c: Needs-Human Transition Rules – Escalation Policies

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 10 – Core Reliability  
**Dependencies:** Task 041 (Retry Framework), Task 041.b (Capped Retries)  

---

## Description

Task 041.c implements needs-human transition rules—the escalation policies that determine when the agent should stop retrying and require human intervention. When retries are exhausted, when certain error types occur, or when the agent detects it's in an irrecoverable state, it MUST escalate to a human rather than continue failing.

The escalation system transitions the agent to a "needs-human" state. In this state, the agent pauses execution, logs the issue with full context, and waits for human resolution. The human can investigate, fix the problem, and instruct the agent to resume—or abort the operation entirely.

Escalation triggers include: retry cap exceeded (Task 041.b), permanent failures (Task 041.a), state validation failures (Task 040.b), security violations, and explicitly configured escalation conditions. Each trigger has specific context requirements to help the human diagnose and resolve the issue.

The escalation is non-bypassable in normal operation. The agent cannot retry or continue past an escalation without human acknowledgment. This ensures safety—the agent doesn't make the same mistake repeatedly or proceed with corrupted state.

### Business Value

Needs-human escalation provides:
- Safe failure handling
- Human-in-the-loop for complex issues
- No silent failures
- Clear resolution path
- Audit trail for issues

### Scope Boundaries

This task covers escalation rules and state. Retry framework is Task 041. Cap enforcement is Task 041.b. Categorization is Task 041.a.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Retry Framework | Task 041 | Escalation trigger | Input |
| Cap Enforcement | Task 041.b | Cap exceeded | Trigger |
| Categorization | Task 041.a | Permanent failure | Trigger |
| Resume Engine | Task 040.b | Validation failure | Trigger |
| Event Log | Task 040 | Log escalation | Audit |
| CLI | Task 000 | User notification | Output |
| Config | Task 002 | Escalation rules | Config |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Escalation not shown | UI check | Log fallback | User unaware |
| Escalation bypassed | Audit | Alert | Security |
| Human unavailable | Timeout | Keep waiting | Blocked |
| Resolution rejected | Validation | Re-escalate | Retry needed |
| Context lost | Log check | Recover from log | Degraded |
| Multiple escalations | Queue | Serialize | User overwhelmed |
| Escalation loop | Counter | Error | Bug |
| Notification failure | Retry notify | Persist | Delayed |

### Assumptions

1. **Human available eventually**: Will respond
2. **CLI visible**: User sees output
3. **Log accessible**: For context
4. **Resolution is explicit**: Human command
5. **Abort is allowed**: Human can cancel
6. **Context is sufficient**: For diagnosis
7. **Escalation is rare**: Well-designed system
8. **Resume is possible**: After fix

### Security Considerations

1. **Escalation logged**: Full audit
2. **No bypass without audit**: Tracked
3. **Human identity verified**: For resolution
4. **Context redacted**: No secrets
5. **Resolution logged**: Who, when, what
6. **Abort logged**: Track cancellations
7. **Escalation not exploitable**: No DoS
8. **Rate limiting**: Prevent spam

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Needs-Human | Escalation state |
| Escalation | Transition to needs-human |
| Resolution | Human acknowledgment |
| Abort | Human cancellation |
| Resume | Continue after resolution |
| Trigger | Event causing escalation |
| Context | Information for diagnosis |
| Escalation Policy | Rules for when to escalate |

---

## Out of Scope

- External notification (email, SMS)
- Escalation routing to teams
- SLA enforcement
- Escalation analytics
- Automatic resolution suggestions
- Multi-level escalation

---

## Functional Requirements

### FR-001 to FR-015: Escalation Triggers

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041c-01 | Retry cap exceeded MUST trigger | P0 |
| FR-041c-02 | Permanent failure MUST trigger | P0 |
| FR-041c-03 | State validation failure MUST trigger | P0 |
| FR-041c-04 | Security violation MUST trigger | P0 |
| FR-041c-05 | Configuration error MUST trigger | P0 |
| FR-041c-06 | Explicit escalation MUST trigger | P0 |
| FR-041c-07 | Custom triggers MUST be configurable | P1 |
| FR-041c-08 | Trigger type MUST be logged | P0 |
| FR-041c-09 | Trigger context MUST be captured | P0 |
| FR-041c-10 | Trigger timestamp MUST be recorded | P0 |
| FR-041c-11 | Trigger source MUST be identified | P0 |
| FR-041c-12 | Multiple triggers MUST be handled | P0 |
| FR-041c-13 | Duplicate trigger MUST be deduplicated | P1 |
| FR-041c-14 | Trigger priority MUST exist | P1 |
| FR-041c-15 | High priority MUST escalate faster | P2 |

### FR-016 to FR-030: Needs-Human State

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041c-16 | Agent MUST pause on escalation | P0 |
| FR-041c-17 | Pause MUST be immediate | P0 |
| FR-041c-18 | No retry MUST occur in pause | P0 |
| FR-041c-19 | No progress MUST occur in pause | P0 |
| FR-041c-20 | State MUST be preserved | P0 |
| FR-041c-21 | State MUST be queryable | P0 |
| FR-041c-22 | CLI MUST show escalation | P0 |
| FR-041c-23 | CLI MUST show context | P0 |
| FR-041c-24 | CLI MUST show options | P0 |
| FR-041c-25 | Options MUST include resume | P0 |
| FR-041c-26 | Options MUST include abort | P0 |
| FR-041c-27 | Options MUST include retry | P1 |
| FR-041c-28 | Timeout MUST be configurable | P2 |
| FR-041c-29 | Default timeout MUST be infinite | P1 |
| FR-041c-30 | Timeout MUST escalate further | P2 |

### FR-031 to FR-045: Context Capture

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041c-31 | Context MUST include trigger type | P0 |
| FR-041c-32 | Context MUST include error details | P0 |
| FR-041c-33 | Context MUST include stack trace | P1 |
| FR-041c-34 | Context MUST include retry history | P0 |
| FR-041c-35 | Context MUST include task state | P0 |
| FR-041c-36 | Context MUST include event log ref | P0 |
| FR-041c-37 | Context MUST include timestamp | P0 |
| FR-041c-38 | Context MUST be redacted | P0 |
| FR-041c-39 | Context MUST be serializable | P0 |
| FR-041c-40 | Context MUST be human-readable | P0 |
| FR-041c-41 | Context MUST include suggestions | P1 |
| FR-041c-42 | Suggestions MUST be actionable | P1 |
| FR-041c-43 | Context size MUST be bounded | P0 |
| FR-041c-44 | Context MUST include operation | P0 |
| FR-041c-45 | Context MUST include affected files | P1 |

### FR-046 to FR-060: Resolution Handling

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041c-46 | Resolution MUST be explicit | P0 |
| FR-041c-47 | Resolution MUST be logged | P0 |
| FR-041c-48 | Log MUST include resolution type | P0 |
| FR-041c-49 | Log MUST include timestamp | P0 |
| FR-041c-50 | Log MUST include identifier | P0 |
| FR-041c-51 | Resume MUST continue from pause | P0 |
| FR-041c-52 | Resume MUST reset retry counter | P0 |
| FR-041c-53 | Abort MUST stop execution | P0 |
| FR-041c-54 | Abort MUST log reason | P0 |
| FR-041c-55 | Abort MUST cleanup state | P0 |
| FR-041c-56 | Retry MUST re-attempt operation | P1 |
| FR-041c-57 | Retry MUST increment counter | P1 |
| FR-041c-58 | Force-continue MUST be audited | P0 |
| FR-041c-59 | Force-continue MUST require flag | P0 |
| FR-041c-60 | Invalid resolution MUST reject | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-041c-01 | Escalation transition | <100ms | P0 |
| NFR-041c-02 | Context capture | <500ms | P0 |
| NFR-041c-03 | CLI display | <200ms | P0 |
| NFR-041c-04 | Resolution processing | <100ms | P0 |
| NFR-041c-05 | Resume transition | <200ms | P0 |
| NFR-041c-06 | Abort processing | <500ms | P0 |
| NFR-041c-07 | Memory in pause | <100MB | P2 |
| NFR-041c-08 | Context size | <1MB | P1 |
| NFR-041c-09 | Log write | <50ms | P0 |
| NFR-041c-10 | State query | <10ms | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-041c-11 | Escalation works | 100% | P0 |
| NFR-041c-12 | State preserved | 100% | P0 |
| NFR-041c-13 | Resolution works | 100% | P0 |
| NFR-041c-14 | No bypass | 100% | P0 |
| NFR-041c-15 | Context complete | 99% | P0 |
| NFR-041c-16 | CLI display works | 100% | P0 |
| NFR-041c-17 | Cross-platform | All OS | P0 |
| NFR-041c-18 | Crash recovery | Resume | P0 |
| NFR-041c-19 | Concurrent safe | No races | P0 |
| NFR-041c-20 | Graceful degradation | On error | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-041c-21 | Escalation logged | Info | P0 |
| NFR-041c-22 | Resolution logged | Info | P0 |
| NFR-041c-23 | Context logged | Debug | P0 |
| NFR-041c-24 | Abort logged | Warning | P0 |
| NFR-041c-25 | Metrics: escalations | Counter | P2 |
| NFR-041c-26 | Metrics: resolutions | Counter | P2 |
| NFR-041c-27 | Metrics: duration | Histogram | P2 |
| NFR-041c-28 | Structured logging | JSON | P0 |
| NFR-041c-29 | Alert on escalation | Optional | P2 |
| NFR-041c-30 | Dashboard data | Exported | P2 |

---

## Acceptance Criteria / Definition of Done

### Escalation Triggers
- [ ] AC-001: Cap exceeded triggers
- [ ] AC-002: Permanent failure triggers
- [ ] AC-003: Validation failure triggers
- [ ] AC-004: Security violation triggers
- [ ] AC-005: Custom triggers work
- [ ] AC-006: Type logged
- [ ] AC-007: Context captured
- [ ] AC-008: Timestamp recorded

### Needs-Human State
- [ ] AC-009: Agent pauses
- [ ] AC-010: Pause immediate
- [ ] AC-011: No retry in pause
- [ ] AC-012: State preserved
- [ ] AC-013: CLI shows escalation
- [ ] AC-014: CLI shows context
- [ ] AC-015: CLI shows options
- [ ] AC-016: State queryable

### Context Capture
- [ ] AC-017: Trigger type included
- [ ] AC-018: Error details included
- [ ] AC-019: Retry history included
- [ ] AC-020: Task state included
- [ ] AC-021: Redacted
- [ ] AC-022: Human-readable
- [ ] AC-023: Size bounded
- [ ] AC-024: Suggestions included

### Resolution Handling
- [ ] AC-025: Explicit resolution
- [ ] AC-026: Logged
- [ ] AC-027: Resume works
- [ ] AC-028: Counter reset
- [ ] AC-029: Abort works
- [ ] AC-030: Cleanup on abort
- [ ] AC-031: Force-continue audited
- [ ] AC-032: Invalid rejected

---

## User Verification Scenarios

### Scenario 1: Retry Cap Escalation
**Persona:** Agent with exhausted retries  
**Preconditions:** 3 retries failed  
**Steps:**
1. Cap exceeded
2. Escalation triggered
3. Agent pauses
4. User sees message

**Verification Checklist:**
- [ ] Pause occurred
- [ ] Context shown
- [ ] Options available
- [ ] Logged

### Scenario 2: Human Resolves with Resume
**Persona:** User fixing issue  
**Preconditions:** Agent escalated  
**Steps:**
1. View context
2. Fix external issue
3. Issue resume command
4. Agent continues

**Verification Checklist:**
- [ ] Context helpful
- [ ] Resume works
- [ ] Counter reset
- [ ] Logged

### Scenario 3: Human Aborts
**Persona:** User canceling operation  
**Preconditions:** Agent escalated  
**Steps:**
1. View context
2. Decide to abort
3. Issue abort command
4. Agent stops cleanly

**Verification Checklist:**
- [ ] Abort works
- [ ] Cleanup occurs
- [ ] Logged
- [ ] Clean exit

### Scenario 4: Security Escalation
**Persona:** Agent detecting violation  
**Preconditions:** Secret in output  
**Steps:**
1. Violation detected
2. Immediate escalation
3. No retry attempted
4. User notified

**Verification Checklist:**
- [ ] No retry
- [ ] Immediate pause
- [ ] Security context
- [ ] Alert logged

### Scenario 5: Context Review
**Persona:** User debugging  
**Preconditions:** Escalation occurred  
**Steps:**
1. View escalation
2. Check retry history
3. Check error details
4. Diagnose issue

**Verification Checklist:**
- [ ] Full history shown
- [ ] Details helpful
- [ ] Redacted properly
- [ ] Suggestions present

### Scenario 6: Force Continue
**Persona:** Admin with override  
**Preconditions:** Issue understood  
**Steps:**
1. Review context
2. Use force flag
3. Continue despite error
4. Audit logged

**Verification Checklist:**
- [ ] Flag required
- [ ] Audited
- [ ] Continues
- [ ] Risk acknowledged

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-041c-01 | Cap exceeded trigger | FR-041c-01 |
| UT-041c-02 | Permanent failure trigger | FR-041c-02 |
| UT-041c-03 | Security trigger | FR-041c-04 |
| UT-041c-04 | Agent pause | FR-041c-16 |
| UT-041c-05 | State preservation | FR-041c-20 |
| UT-041c-06 | Context capture | FR-041c-31 |
| UT-041c-07 | Context redaction | FR-041c-38 |
| UT-041c-08 | Resume works | FR-041c-51 |
| UT-041c-09 | Counter reset | FR-041c-52 |
| UT-041c-10 | Abort works | FR-041c-53 |
| UT-041c-11 | Cleanup on abort | FR-041c-55 |
| UT-041c-12 | Force-continue audit | FR-041c-58 |
| UT-041c-13 | Invalid resolution | FR-041c-60 |
| UT-041c-14 | Custom triggers | FR-041c-07 |
| UT-041c-15 | Context size bound | FR-041c-43 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-041c-01 | Full escalation flow | E2E |
| IT-041c-02 | Cap integration | Task 041.b |
| IT-041c-03 | Categorization integration | Task 041.a |
| IT-041c-04 | Event log integration | Task 040 |
| IT-041c-05 | CLI display | Task 000 |
| IT-041c-06 | Resume flow | FR-041c-51 |
| IT-041c-07 | Abort flow | FR-041c-53 |
| IT-041c-08 | Crash recovery | NFR-041c-18 |
| IT-041c-09 | Logging | NFR-041c-21 |
| IT-041c-10 | Cross-platform | NFR-041c-17 |
| IT-041c-11 | Secret redaction | Task 038 |
| IT-041c-12 | Config integration | Task 002 |
| IT-041c-13 | Multiple escalations | FR-041c-12 |
| IT-041c-14 | Timeout handling | FR-041c-28 |
| IT-041c-15 | Performance | NFR-041c-01 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Escalation/
│       ├── EscalationTrigger.cs
│       ├── EscalationContext.cs
│       ├── Resolution.cs
│       └── EscalationState.cs
├── Acode.Application/
│   └── Escalation/
│       ├── IEscalationManager.cs
│       └── EscalationOptions.cs
├── Acode.Infrastructure/
│   └── Escalation/
│       ├── EscalationManager.cs
│       ├── ContextBuilder.cs
│       └── ResolutionHandler.cs
├── Acode.Cli/
│   └── Commands/
│       └── ResolveCommand.cs
```

### CLI Commands

```bash
# View current escalation
acode escalation show

# Resume after fixing issue
acode escalation resolve --resume

# Abort the operation
acode escalation resolve --abort --reason "Cannot fix"

# Force continue (requires explicit flag)
acode escalation resolve --force-continue --acknowledge-risk

# Retry the failed operation
acode escalation resolve --retry
```

### Key Implementation

```csharp
public class EscalationManager : IEscalationManager
{
    public async Task EscalateAsync(EscalationTrigger trigger, Exception? error = null)
    {
        var context = await _contextBuilder.BuildAsync(trigger, error);
        
        _logger.LogInformation("Escalation triggered: {Type} - {Summary}",
            trigger.Type, context.Summary);
        
        // Record escalation event
        await _eventLog.AppendAsync(new EscalationEvent(trigger, context));
        
        // Transition to needs-human state
        _state.TransitionTo(EscalationState.NeedsHuman);
        
        // Display to user
        await _cli.DisplayEscalationAsync(context);
        
        // Wait for resolution
        var resolution = await WaitForResolutionAsync();
        
        await HandleResolutionAsync(resolution);
    }
    
    public async Task HandleResolutionAsync(Resolution resolution)
    {
        _logger.LogInformation("Resolution received: {Type}", resolution.Type);
        
        await _eventLog.AppendAsync(new ResolutionEvent(resolution));
        
        switch (resolution.Type)
        {
            case ResolutionType.Resume:
                _retryCounter.Reset();
                _state.TransitionTo(EscalationState.Running);
                break;
                
            case ResolutionType.Abort:
                await CleanupAsync();
                _state.TransitionTo(EscalationState.Aborted);
                throw new OperationAbortedException(resolution.Reason);
                
            case ResolutionType.ForceContinue:
                if (!resolution.AcknowledgedRisk)
                    throw new InvalidResolutionException("Risk not acknowledged");
                _logger.LogWarning("Force-continue used: {Reason}", resolution.Reason);
                _state.TransitionTo(EscalationState.Running);
                break;
                
            case ResolutionType.Retry:
                _state.TransitionTo(EscalationState.Running);
                // Don't reset counter - this is an additional attempt
                break;
        }
    }
}
```

**End of Task 041.c Specification**
