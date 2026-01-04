# Task 036.c: Non-Bypassable Approvals (Default)

**Priority:** P0 – Critical  
**Tier:** L – Feature Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 8 – CI/CD Integration  
**Dependencies:** Task 036, Task 036.a, Task 036.b  

---

## Description

Task 036.c implements non-bypassable approval gates for deployment hooks. All deployment hook executions MUST require explicit user approval. There is NO mechanism to bypass this requirement - it is a fundamental safety constraint.

Non-bypassable approvals ensure that no deployment action occurs without human confirmation. Unlike other approval systems that may have admin overrides or automation bypasses, this approval gate is architecturally enforced and cannot be circumvented.

Approval decisions are recorded as structured DB events with who/when/why and full redaction applied. The audit trail is immutable and provides complete traceability.

### Business Value

Non-bypassable approvals provide:
- Guaranteed human-in-the-loop for deployments
- Protection against automation accidents
- Complete audit trail of approval decisions
- Compliance with change management policies

### Scope Boundaries

This task covers approval enforcement and audit. Hook execution is Task 036. Schema is 036.a. Enablement is 036.b.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Hook Engine | Task 036 | Approval check | Before exec |
| Enablement | Task 036.b | Both required | Layered gates |
| Workspace DB | Task 050 | Audit events | Persistence |
| Audit System | Task 039 | Event records | Compliance |
| CLI Prompt | `IApprovalPrompter` | User input | Interactive |
| Event Bus | `IEventPublisher` | Approval events | Async |
| Redaction | `IRedactionService` | Log redaction | Required |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Approval timeout | Timer expires | Abort hook | Must re-initiate |
| Approval denied | User input | Block execution | Hook not run |
| DB audit write fails | Exception | Retry queue | Degraded audit |
| Prompt interrupted | Signal | Treat as deny | Safe default |
| Non-interactive env | Stdin check | Error message | Cannot approve |
| Concurrent approval | Lock check | Queue | Sequential |
| Invalid input | Validation | Re-prompt | Must retry |
| Event publish fails | Exception | Retry | Delayed audit |

### Assumptions

1. **Interactive terminal**: Approval via TTY
2. **User present**: Human available to approve
3. **Audit required**: All decisions logged
4. **No bypass exists**: Architecturally enforced
5. **Single approver**: One person approves
6. **Timeout configured**: Default 5 minutes
7. **Denial is default**: No input = deny
8. **Reason optional**: Approval reason optional

### Security Considerations

1. **No bypass mechanism**: Code has no override path
2. **Audit immutable**: Decisions cannot be deleted
3. **Denial logged**: Rejections are audited
4. **Context captured**: Who/when/why recorded
5. **Redaction applied**: Sensitive data hidden
6. **Timeout enforced**: Cannot wait forever
7. **Non-interactive blocked**: Must have TTY
8. **Prompt tamper-resistant**: Clear Y/N
9. **Confirmation explicit**: Not just Enter
10. **Double-confirm for critical**: High-risk hooks

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Approval | Explicit user consent |
| Denial | Explicit user rejection |
| Non-Bypassable | Cannot be circumvented |
| Approval Gate | Checkpoint requiring approval |
| Timeout | Maximum wait for approval |
| Audit Event | Immutable decision record |
| Context | Who/when/why of decision |
| Double-Confirm | Type word to confirm |

---

## Out of Scope

- Multi-person approval
- Approval delegation
- Remote approval API
- Automated approval rules
- Role-based approval
- Approval workflows

---

## Functional Requirements

### FR-001 to FR-015: Approval Enforcement

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036C-01 | `IApprovalGate` interface MUST exist | P0 |
| FR-036C-02 | `RequireApprovalAsync` MUST block until approved | P0 |
| FR-036C-03 | Approval MUST be required for ALL hooks | P0 |
| FR-036C-04 | NO bypass mechanism MUST exist | P0 |
| FR-036C-05 | Code MUST NOT have admin override | P0 |
| FR-036C-06 | Config MUST NOT allow bypass | P0 |
| FR-036C-07 | CLI MUST NOT have `--force` flag | P0 |
| FR-036C-08 | API MUST NOT have bypass parameter | P0 |
| FR-036C-09 | Approval MUST be interactive (TTY) | P0 |
| FR-036C-10 | Non-interactive MUST error | P0 |
| FR-036C-11 | Error MUST explain why | P0 |
| FR-036C-12 | Timeout MUST be enforced | P0 |
| FR-036C-13 | Default timeout: 5 minutes | P0 |
| FR-036C-14 | Timeout MUST be configurable | P1 |
| FR-036C-15 | Timeout results in denial | P0 |

### FR-016 to FR-030: Approval Prompt

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036C-16 | `IApprovalPrompter` interface MUST exist | P0 |
| FR-036C-17 | Prompt MUST show hook name | P0 |
| FR-036C-18 | Prompt MUST show hook actions | P0 |
| FR-036C-19 | Prompt MUST show affected targets | P0 |
| FR-036C-20 | Prompt MUST show approval timeout | P1 |
| FR-036C-21 | Prompt MUST require explicit Y/N | P0 |
| FR-036C-22 | Empty input MUST be treated as N | P0 |
| FR-036C-23 | Invalid input MUST re-prompt | P0 |
| FR-036C-24 | Case-insensitive Y/N | P1 |
| FR-036C-25 | `yes`/`no` words accepted | P1 |
| FR-036C-26 | High-risk hooks MUST require double-confirm | P0 |
| FR-036C-27 | Double-confirm: type hook name | P0 |
| FR-036C-28 | Prompt MUST be clear and unambiguous | P0 |
| FR-036C-29 | Prompt MUST show consequences | P0 |
| FR-036C-30 | Prompt MUST support optional reason | P1 |

### FR-031 to FR-045: Audit Events

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036C-31 | Approval MUST create audit event | P0 |
| FR-036C-32 | Denial MUST create audit event | P0 |
| FR-036C-33 | Timeout MUST create audit event | P0 |
| FR-036C-34 | Audit event MUST include decision | P0 |
| FR-036C-35 | Audit event MUST include hook name | P0 |
| FR-036C-36 | Audit event MUST include timestamp | P0 |
| FR-036C-37 | Audit event MUST include user context | P0 |
| FR-036C-38 | Audit event MUST include reason (if provided) | P1 |
| FR-036C-39 | Audit event MUST include timeout duration | P1 |
| FR-036C-40 | Audit events MUST be persisted to DB | P0 |
| FR-036C-41 | Audit events MUST be immutable | P0 |
| FR-036C-42 | Audit events MUST have sync_state | P1 |
| FR-036C-43 | Sensitive context MUST be redacted | P0 |
| FR-036C-44 | Audit history MUST be queryable | P1 |
| FR-036C-45 | Approval/denial ratio MUST be trackable | P2 |

### FR-046 to FR-060: Risk Classification

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036C-46 | Hooks MUST have risk level | P0 |
| FR-036C-47 | Risk levels: low, medium, high, critical | P0 |
| FR-036C-48 | Low risk: single Y confirm | P0 |
| FR-036C-49 | Medium risk: single Y confirm + warning | P0 |
| FR-036C-50 | High risk: double confirm (type hook name) | P0 |
| FR-036C-51 | Critical risk: double confirm + explicit acknowledgment | P0 |
| FR-036C-52 | Risk level MUST be in schema | P0 |
| FR-036C-53 | Default risk level: medium | P0 |
| FR-036C-54 | Risk level MUST be displayed | P0 |
| FR-036C-55 | Risk level MUST be logged | P0 |
| FR-036C-56 | Risk can be overridden in config | P1 |
| FR-036C-57 | Override cannot reduce risk below schema | P0 |
| FR-036C-58 | Critical hooks MUST show countdown | P1 |
| FR-036C-59 | Countdown: 10 second delay before confirm | P2 |
| FR-036C-60 | User can cancel during countdown | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-036C-01 | Prompt display latency | <50ms | P1 |
| NFR-036C-02 | Input processing | <10ms | P1 |
| NFR-036C-03 | Audit write latency | <50ms | P1 |
| NFR-036C-04 | Event emission | <10ms | P2 |
| NFR-036C-05 | Validation latency | <10ms | P1 |
| NFR-036C-06 | Countdown timer accuracy | ±100ms | P2 |
| NFR-036C-07 | TTY detection | <10ms | P1 |
| NFR-036C-08 | Memory overhead | <1MB | P2 |
| NFR-036C-09 | Concurrent approval gates | 1 at a time | P0 |
| NFR-036C-10 | Lock acquisition | <100ms | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-036C-11 | No bypass guarantee | 100% | P0 |
| NFR-036C-12 | Audit persistence | 99.99% | P0 |
| NFR-036C-13 | Timeout enforcement | 100% | P0 |
| NFR-036C-14 | Input validation | 100% | P0 |
| NFR-036C-15 | Denial on interrupt | 100% | P0 |
| NFR-036C-16 | Denial on timeout | 100% | P0 |
| NFR-036C-17 | TTY detection accuracy | 100% | P0 |
| NFR-036C-18 | Graceful error handling | Always | P1 |
| NFR-036C-19 | Safe default (deny) | Always | P0 |
| NFR-036C-20 | Audit completeness | 100% | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-036C-21 | Approval logged | Info level | P0 |
| NFR-036C-22 | Denial logged | Warning level | P0 |
| NFR-036C-23 | Timeout logged | Warning level | P0 |
| NFR-036C-24 | Metrics: approvals count | Counter | P1 |
| NFR-036C-25 | Metrics: denials count | Counter | P1 |
| NFR-036C-26 | Metrics: timeouts count | Counter | P1 |
| NFR-036C-27 | Metrics: approval latency | Histogram | P2 |
| NFR-036C-28 | Events: approval requested | Published | P0 |
| NFR-036C-29 | Events: decision made | Published | P0 |
| NFR-036C-30 | Structured log output | JSON | P0 |

---

## Mode Compliance

| Mode | Non-Bypassable Approval Behavior |
|------|----------------------------------|
| Local-Only | Full enforcement, TTY required |
| Burst | Same as Local-Only |
| Air-Gapped | Full enforcement, TTY required |

---

## Acceptance Criteria / Definition of Done

### Enforcement
- [ ] AC-001: `IApprovalGate` interface exists
- [ ] AC-002: All hooks require approval
- [ ] AC-003: No bypass mechanism exists
- [ ] AC-004: No `--force` flag
- [ ] AC-005: No admin override
- [ ] AC-006: No config bypass
- [ ] AC-007: TTY required
- [ ] AC-008: Non-interactive errors
- [ ] AC-009: Timeout enforced
- [ ] AC-010: Timeout = denial

### Prompt
- [ ] AC-011: `IApprovalPrompter` exists
- [ ] AC-012: Hook name shown
- [ ] AC-013: Actions shown
- [ ] AC-014: Targets shown
- [ ] AC-015: Explicit Y/N required
- [ ] AC-016: Empty = N
- [ ] AC-017: Invalid re-prompts
- [ ] AC-018: High-risk double-confirm
- [ ] AC-019: Consequences shown
- [ ] AC-020: Reason supported

### Audit
- [ ] AC-021: Approval creates event
- [ ] AC-022: Denial creates event
- [ ] AC-023: Timeout creates event
- [ ] AC-024: Decision in event
- [ ] AC-025: Timestamp in event
- [ ] AC-026: Context in event
- [ ] AC-027: Reason captured
- [ ] AC-028: Events persisted
- [ ] AC-029: Events immutable
- [ ] AC-030: Redaction applied
- [ ] AC-031: History queryable

### Risk Levels
- [ ] AC-032: Risk levels defined
- [ ] AC-033: Low: single Y
- [ ] AC-034: Medium: Y + warning
- [ ] AC-035: High: double confirm
- [ ] AC-036: Critical: countdown
- [ ] AC-037: Risk displayed
- [ ] AC-038: Risk logged
- [ ] AC-039: Override cannot reduce

---

## User Verification Scenarios

### Scenario 1: Standard Approval
**Persona:** Developer running hook  
**Preconditions:** Hook enabled, medium risk  
**Steps:**
1. Run `acode deploy hooks run cleanup`
2. Approval prompt appears
3. Type 'y' and press Enter
4. Hook executes

**Verification Checklist:**
- [ ] Prompt shown
- [ ] Details visible
- [ ] Y accepted
- [ ] Hook runs

### Scenario 2: Denial
**Persona:** Developer changing mind  
**Preconditions:** Approval prompt  
**Steps:**
1. See approval prompt
2. Type 'n' and Enter
3. Hook not executed
4. Denial logged

**Verification Checklist:**
- [ ] N accepted
- [ ] Hook blocked
- [ ] Audit logged
- [ ] Clear message

### Scenario 3: Timeout
**Persona:** Developer away from keyboard  
**Preconditions:** Approval prompt  
**Steps:**
1. See approval prompt
2. Wait 5 minutes
3. Timeout occurs
4. Treated as denial

**Verification Checklist:**
- [ ] Timeout enforced
- [ ] Denial result
- [ ] Audit logged
- [ ] Message shown

### Scenario 4: High-Risk Double Confirm
**Persona:** Developer with critical hook  
**Preconditions:** High-risk hook  
**Steps:**
1. Run high-risk hook
2. Prompt shows warning
3. Must type hook name
4. Then confirms

**Verification Checklist:**
- [ ] Warning shown
- [ ] Name required
- [ ] Typo rejected
- [ ] Correct works

### Scenario 5: Non-Interactive Blocked
**Persona:** CI pipeline attempt  
**Preconditions:** No TTY  
**Steps:**
1. Script runs hook command
2. No TTY detected
3. Error: approval requires interactive terminal
4. Hook not executed

**Verification Checklist:**
- [ ] TTY checked
- [ ] Error shown
- [ ] Hook blocked
- [ ] Clear message

### Scenario 6: No Force Flag
**Persona:** Developer trying bypass  
**Preconditions:** Looking for shortcuts  
**Steps:**
1. Try `acode deploy hooks run --force`
2. Error: unknown flag --force
3. No bypass exists
4. Must go through approval

**Verification Checklist:**
- [ ] Flag rejected
- [ ] No bypass
- [ ] Must approve
- [ ] Safety maintained

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-036C-01 | IApprovalGate interface | FR-036C-01 |
| UT-036C-02 | Approval blocks until input | FR-036C-02 |
| UT-036C-03 | All hooks require approval | FR-036C-03 |
| UT-036C-04 | No bypass code path exists | FR-036C-04 |
| UT-036C-05 | TTY detection | FR-036C-09 |
| UT-036C-06 | Non-interactive error | FR-036C-10 |
| UT-036C-07 | Timeout enforcement | FR-036C-12 |
| UT-036C-08 | Y input accepted | FR-036C-21 |
| UT-036C-09 | N input accepted | FR-036C-21 |
| UT-036C-10 | Empty = N | FR-036C-22 |
| UT-036C-11 | Invalid re-prompts | FR-036C-23 |
| UT-036C-12 | High-risk double-confirm | FR-036C-26 |
| UT-036C-13 | Audit event creation | FR-036C-31 |
| UT-036C-14 | Risk level check | FR-036C-46 |
| UT-036C-15 | Redaction applied | FR-036C-43 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-036C-01 | Full approval flow | E2E |
| IT-036C-02 | Full denial flow | E2E |
| IT-036C-03 | Timeout flow | FR-036C-12 |
| IT-036C-04 | Audit persistence | FR-036C-40 |
| IT-036C-05 | Risk level enforcement | FR-036C-46 |
| IT-036C-06 | Double-confirm | FR-036C-26 |
| IT-036C-07 | Non-interactive rejection | FR-036C-10 |
| IT-036C-08 | No force flag | FR-036C-07 |
| IT-036C-09 | Workspace DB integration | Task 050 |
| IT-036C-10 | Event emission | NFR-036C-28 |
| IT-036C-11 | Concurrent gate serialization | NFR-036C-09 |
| IT-036C-12 | Countdown for critical | FR-036C-58 |
| IT-036C-13 | History query | FR-036C-44 |
| IT-036C-14 | Metrics emission | NFR-036C-24 |
| IT-036C-15 | Performance benchmark | NFR-036C-01 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Deployment/
│       └── Hooks/
│           └── Approvals/
│               ├── ApprovalDecision.cs
│               ├── RiskLevel.cs
│               └── Events/
│                   ├── ApprovalRequestedEvent.cs
│                   ├── ApprovalGrantedEvent.cs
│                   ├── ApprovalDeniedEvent.cs
│                   └── ApprovalTimedOutEvent.cs
├── Acode.Application/
│   └── Deployment/
│       └── Hooks/
│           └── Approvals/
│               ├── IApprovalGate.cs
│               └── IApprovalPrompter.cs
├── Acode.Infrastructure/
│   └── Deployment/
│       └── Hooks/
│           └── Approvals/
│               ├── ApprovalGate.cs
│               ├── ConsoleApprovalPrompter.cs
│               └── RiskLevelEvaluator.cs
└── Acode.Cli/
    └── Commands/
        └── Deploy/
            └── Hooks/
                └── (approval integrated into RunHookCommand)
```

### Architectural Note

The non-bypassable nature is enforced by architecture:
1. `IApprovalGate.RequireApprovalAsync()` is called in `IDeploymentHookEngine.ExecuteHookAsync()` BEFORE any hook action
2. There is NO code path that skips this call
3. The interface has NO bypass parameter
4. Unit tests verify no bypass exists

```csharp
// This is the ONLY way to execute a hook
public async Task<HookResult> ExecuteHookAsync(DeploymentHook hook, CancellationToken ct)
{
    // Step 1: Check enabled (036.b) - cannot be skipped
    if (!await _enabledChecker.IsEnabledAsync(hook.Name, ct))
        throw new HookDisabledException(hook.Name);
    
    // Step 2: Require approval (036.c) - cannot be skipped
    var approval = await _approvalGate.RequireApprovalAsync(hook, ct);
    if (!approval.Granted)
        throw new ApprovalDeniedException(hook.Name, approval.Reason);
    
    // Step 3: Execute (only if above passed)
    return await ExecuteInternalAsync(hook, ct);
}
```

**End of Task 036.c Specification**
