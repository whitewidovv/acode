# Task 035.b: Approval Gates

**Priority:** P0 – Critical  
**Tier:** L – Feature Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 8 – CI/CD Integration  
**Dependencies:** Task 035, Task 035.a  

---

## Description

Task 035.b implements the approval gate system for CI maintenance proposals. Proposals MUST NOT be applied without explicit user approval. Approval gates ensure no CI changes are auto-committed without human review.

Every proposal requires explicit approval before changes are applied. Approvals MUST be recorded with timestamp and context. Rejections MUST include a reason. Bulk approval with review MUST be supported for efficiency.

The approval system integrates with the proposal store (035.a) to track status transitions. Applied changes MUST be atomic and reversible through git.

### Business Value

Approval gates provide:
- Human-in-the-loop for CI changes
- Safety against unwanted modifications
- Audit trail for all decisions
- Confidence in automated suggestions

### Scope Boundaries

This task covers approval/rejection flow. Proposal generation is Task 035. Diff display is 035.a. Task runner integration is 035.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Proposal Store | `IProposalStore` | Status updates | From 035.a |
| Change Applier | `IChangeApplier` | Apply approved | Git operations |
| CLI Commands | `ApproveCommand` | User input | Interactive |
| Audit Log | `IAuditLogger` | Decision log | Required |
| Event Bus | `IEventPublisher` | Approval events | Async |
| Git Operations | `IGitService` | Commit changes | Atomic |
| Backup System | `IBackupService` | Pre-change backup | Optional |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Proposal not found | ID lookup fails | Error message | Must verify ID |
| Already applied | Status check | Skip with message | No action needed |
| Already rejected | Status check | Cannot re-approve | Must re-analyze |
| File changed | Hash mismatch | Regenerate proposal | Must re-review |
| Git commit fails | Exception | Rollback | Changes not applied |
| Concurrent approval | Lock check | Error message | Retry |
| Invalid transition | State machine | Error message | Action blocked |
| Audit log fails | Write error | Block operation | Safety check |

### Assumptions

1. **Interactive approval**: User manually approves
2. **Proposal valid**: Proposal exists and not expired
3. **File unchanged**: Hash matches proposal
4. **Git available**: Git CLI accessible
5. **Write access**: Can modify workflow files
6. **Audit required**: All decisions logged
7. **Atomic apply**: All or nothing
8. **Reason required**: Rejections need reason

### Security Considerations

1. **Human required**: No auto-approval possible
2. **Audit immutable**: Decisions cannot be deleted
3. **Hash verification**: Prevents stale apply
4. **Reason mandatory**: Rejections documented
5. **Rollback available**: Undo via git
6. **No force mode**: Cannot bypass approval
7. **Session context**: Who approved recorded
8. **Timeout enforced**: Expired proposals blocked

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Approval | Explicit user consent to apply |
| Rejection | Explicit user decline |
| Gate | Checkpoint requiring approval |
| Bulk Approval | Approve multiple proposals |
| Hash Check | Verify file unchanged |
| Atomic Apply | All changes or none |
| Rollback | Undo applied changes |
| Audit Entry | Immutable decision record |

---

## Out of Scope

- Automated approval rules
- Role-based approval
- Multi-person approval
- Approval via UI (CLI only)
- Scheduled auto-apply
- Approval delegation

---

## Functional Requirements

### FR-001 to FR-015: Approval Interface

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035B-01 | `IApprovalGate` interface MUST exist | P0 |
| FR-035B-02 | `ApproveAsync` MUST approve proposal | P0 |
| FR-035B-03 | `RejectAsync` MUST reject proposal | P0 |
| FR-035B-04 | Approval MUST verify proposal exists | P0 |
| FR-035B-05 | Approval MUST verify not expired | P0 |
| FR-035B-06 | Approval MUST verify status is Pending | P0 |
| FR-035B-07 | Rejection MUST require reason | P0 |
| FR-035B-08 | Approval MUST update status to Approved | P0 |
| FR-035B-09 | Rejection MUST update status to Rejected | P0 |
| FR-035B-10 | Status change MUST emit event | P0 |
| FR-035B-11 | Approval timestamp MUST be recorded | P0 |
| FR-035B-12 | Approver context MUST be recorded | P1 |
| FR-035B-13 | Invalid transitions MUST error | P0 |
| FR-035B-14 | Concurrent approval MUST be prevented | P0 |
| FR-035B-15 | Approval MUST be logged to audit | P0 |

### FR-016 to FR-030: Change Application

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035B-16 | `IChangeApplier` interface MUST exist | P0 |
| FR-035B-17 | `ApplyAsync` MUST apply approved proposal | P0 |
| FR-035B-18 | Apply MUST verify status is Approved | P0 |
| FR-035B-19 | Apply MUST verify file hash matches | P0 |
| FR-035B-20 | Apply MUST modify file atomically | P0 |
| FR-035B-21 | Apply MUST update status to Applied | P0 |
| FR-035B-22 | Apply failure MUST rollback | P0 |
| FR-035B-23 | Apply MUST create git commit | P1 |
| FR-035B-24 | Commit message MUST reference proposal ID | P1 |
| FR-035B-25 | Commit MUST include rationale | P1 |
| FR-035B-26 | Apply MUST emit event | P0 |
| FR-035B-27 | Applied timestamp MUST be recorded | P0 |
| FR-035B-28 | Backup before apply MUST be optional | P2 |
| FR-035B-29 | Apply error MUST preserve original | P0 |
| FR-035B-30 | Apply MUST be logged to audit | P0 |

### FR-031 to FR-045: Bulk Operations

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035B-31 | Bulk approve MUST be supported | P1 |
| FR-035B-32 | Bulk approve MUST require confirmation | P0 |
| FR-035B-33 | Bulk approve MUST show all proposals first | P0 |
| FR-035B-34 | Bulk approve MUST be atomic (all or none) | P1 |
| FR-035B-35 | Bulk reject MUST be supported | P1 |
| FR-035B-36 | Bulk reject MUST require reason | P0 |
| FR-035B-37 | Bulk operations MUST log each decision | P0 |
| FR-035B-38 | Bulk apply MUST process sequentially | P0 |
| FR-035B-39 | Bulk failure MUST report which failed | P0 |
| FR-035B-40 | Bulk MUST support filter by severity | P2 |
| FR-035B-41 | Bulk MUST support filter by file | P2 |
| FR-035B-42 | Bulk count MUST be shown before confirm | P0 |
| FR-035B-43 | Bulk abort MUST be cancellable | P1 |
| FR-035B-44 | Bulk progress MUST be displayed | P1 |
| FR-035B-45 | Bulk events MUST be emitted per proposal | P1 |

### FR-046 to FR-060: CLI Commands

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035B-46 | `acode ci maintain approve <id>` MUST work | P0 |
| FR-035B-47 | `acode ci maintain reject <id> --reason` MUST work | P0 |
| FR-035B-48 | `acode ci maintain apply <id>` MUST work | P0 |
| FR-035B-49 | `acode ci maintain approve --all` for bulk | P1 |
| FR-035B-50 | `acode ci maintain reject --all --reason` for bulk | P1 |
| FR-035B-51 | `acode ci maintain apply --all` for bulk | P1 |
| FR-035B-52 | Interactive confirmation for bulk | P0 |
| FR-035B-53 | `--yes` flag to skip confirmation | P2 |
| FR-035B-54 | `--dry-run` to preview apply | P1 |
| FR-035B-55 | Exit code 0 on success | P0 |
| FR-035B-56 | Exit code 1 on failure | P0 |
| FR-035B-57 | Clear error messages | P0 |
| FR-035B-58 | JSON output with `--json` | P1 |
| FR-035B-59 | Verbose mode with `-v` | P2 |
| FR-035B-60 | Help text for each command | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-035B-01 | Single approval latency | <50ms | P1 |
| NFR-035B-02 | Single rejection latency | <50ms | P1 |
| NFR-035B-03 | Apply latency (excluding git) | <100ms | P1 |
| NFR-035B-04 | Bulk approve (10 proposals) | <500ms | P2 |
| NFR-035B-05 | Hash verification | <20ms | P1 |
| NFR-035B-06 | Audit log write | <10ms | P1 |
| NFR-035B-07 | Event emission | <5ms | P2 |
| NFR-035B-08 | Git commit | <2s | P2 |
| NFR-035B-09 | Concurrent lock acquisition | <100ms | P2 |
| NFR-035B-10 | CLI response | <200ms | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-035B-11 | Status transition correctness | 100% | P0 |
| NFR-035B-12 | Hash verification accuracy | 100% | P0 |
| NFR-035B-13 | Audit log durability | 99.99% | P0 |
| NFR-035B-14 | Atomic apply guarantee | 100% | P0 |
| NFR-035B-15 | Rollback on failure | 100% | P0 |
| NFR-035B-16 | No data loss on crash | Required | P0 |
| NFR-035B-17 | Concurrent safety | No corruption | P0 |
| NFR-035B-18 | Idempotent operations | Where applicable | P1 |
| NFR-035B-19 | Graceful error recovery | Always | P1 |
| NFR-035B-20 | Valid state after any operation | Required | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-035B-21 | Approval events logged | All | P0 |
| NFR-035B-22 | Rejection events logged | All | P0 |
| NFR-035B-23 | Apply events logged | All | P0 |
| NFR-035B-24 | Metrics: approvals count | Counter | P1 |
| NFR-035B-25 | Metrics: rejections count | Counter | P1 |
| NFR-035B-26 | Metrics: applications count | Counter | P1 |
| NFR-035B-27 | Audit trail complete | Required | P0 |
| NFR-035B-28 | Error logging | Structured | P0 |
| NFR-035B-29 | Performance metrics | Histogram | P2 |
| NFR-035B-30 | Decision reasons captured | Required | P0 |

---

## Mode Compliance

| Mode | Approval Gate Behavior |
|------|------------------------|
| Local-Only | Full approval flow, no network |
| Burst | Same as Local-Only |
| Air-Gapped | Full approval flow, strict audit |

---

## Acceptance Criteria / Definition of Done

### Approval Flow
- [ ] AC-001: `IApprovalGate` interface exists
- [ ] AC-002: `ApproveAsync` approves proposal
- [ ] AC-003: `RejectAsync` rejects with reason
- [ ] AC-004: Expired proposals blocked
- [ ] AC-005: Already applied proposals blocked
- [ ] AC-006: Status transitions validated
- [ ] AC-007: Events emitted on approval
- [ ] AC-008: Events emitted on rejection

### Application
- [ ] AC-009: `IChangeApplier` interface exists
- [ ] AC-010: `ApplyAsync` applies changes
- [ ] AC-011: Hash verification required
- [ ] AC-012: Atomic file modification
- [ ] AC-013: Git commit created
- [ ] AC-014: Rollback on failure
- [ ] AC-015: Status updated to Applied
- [ ] AC-016: Original preserved on error

### Bulk Operations
- [ ] AC-017: Bulk approve works
- [ ] AC-018: Bulk reject works
- [ ] AC-019: Confirmation required
- [ ] AC-020: All proposals shown first
- [ ] AC-021: Each logged individually
- [ ] AC-022: Progress displayed
- [ ] AC-023: Failure reporting clear
- [ ] AC-024: Cancellation supported

### CLI
- [ ] AC-025: `approve <id>` command works
- [ ] AC-026: `reject <id> --reason` works
- [ ] AC-027: `apply <id>` command works
- [ ] AC-028: Bulk commands work
- [ ] AC-029: `--dry-run` shows preview
- [ ] AC-030: Exit codes correct
- [ ] AC-031: Error messages clear
- [ ] AC-032: JSON output works

### Audit
- [ ] AC-033: All approvals logged
- [ ] AC-034: All rejections logged
- [ ] AC-035: All applications logged
- [ ] AC-036: Timestamp recorded
- [ ] AC-037: Context recorded
- [ ] AC-038: Reason captured for rejections
- [ ] AC-039: Proposal ID in all entries
- [ ] AC-040: Audit entries immutable

### Validation
- [ ] AC-041: Concurrent approval prevented
- [ ] AC-042: Stale proposal detected
- [ ] AC-043: Invalid ID handled
- [ ] AC-044: Missing reason blocked

---

## User Verification Scenarios

### Scenario 1: Approve Single Proposal
**Persona:** Developer reviewing change  
**Preconditions:** Proposal pending  
**Steps:**
1. View proposal details
2. Run `acode ci maintain approve PROP-001`
3. Confirmation prompt appears
4. Confirm approval
5. Status updated

**Verification Checklist:**
- [ ] Proposal approved
- [ ] Status is Approved
- [ ] Event emitted
- [ ] Audit logged

### Scenario 2: Reject with Reason
**Persona:** Developer declining change  
**Preconditions:** Proposal pending  
**Steps:**
1. View proposal details
2. Run `acode ci maintain reject PROP-001 --reason "Not needed"`
3. Status updated to Rejected
4. Reason stored

**Verification Checklist:**
- [ ] Rejection processed
- [ ] Status is Rejected
- [ ] Reason recorded
- [ ] Event emitted

### Scenario 3: Apply Approved Change
**Persona:** Developer applying fix  
**Preconditions:** Proposal approved  
**Steps:**
1. Run `acode ci maintain apply PROP-001`
2. File modified
3. Git commit created
4. Status updated to Applied

**Verification Checklist:**
- [ ] File changed
- [ ] Diff applied correctly
- [ ] Commit created
- [ ] Status is Applied

### Scenario 4: Stale Proposal Blocked
**Persona:** Developer after file change  
**Preconditions:** File modified since proposal  
**Steps:**
1. Approve proposal
2. Edit workflow file manually
3. Try to apply
4. Hash mismatch detected
5. Apply blocked

**Verification Checklist:**
- [ ] Hash check performed
- [ ] Mismatch detected
- [ ] Apply blocked
- [ ] Error message clear

### Scenario 5: Bulk Approve
**Persona:** Developer with many proposals  
**Preconditions:** Multiple pending proposals  
**Steps:**
1. Run `acode ci maintain approve --all`
2. List of proposals shown
3. Confirm bulk approval
4. All approved

**Verification Checklist:**
- [ ] All proposals listed
- [ ] Count shown
- [ ] Confirmation required
- [ ] All approved

### Scenario 6: Expired Proposal Rejected
**Persona:** Developer after delay  
**Preconditions:** Proposal > 24 hours old  
**Steps:**
1. Try to approve old proposal
2. Error: proposal expired
3. Must re-analyze
4. Action blocked

**Verification Checklist:**
- [ ] Expiration detected
- [ ] Error message clear
- [ ] Action blocked
- [ ] Guidance provided

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-035B-01 | Approve updates status | FR-035B-08 |
| UT-035B-02 | Reject requires reason | FR-035B-07 |
| UT-035B-03 | Expired proposal blocked | FR-035B-05 |
| UT-035B-04 | Already applied blocked | FR-035B-18 |
| UT-035B-05 | Status transition validation | FR-035B-13 |
| UT-035B-06 | Approval emits event | FR-035B-10 |
| UT-035B-07 | Rejection emits event | FR-035B-10 |
| UT-035B-08 | Hash verification | FR-035B-19 |
| UT-035B-09 | Atomic file write | FR-035B-20 |
| UT-035B-10 | Rollback on failure | FR-035B-22 |
| UT-035B-11 | Bulk approve logic | FR-035B-31 |
| UT-035B-12 | Bulk reject logic | FR-035B-35 |
| UT-035B-13 | Audit entry creation | FR-035B-15 |
| UT-035B-14 | Concurrent lock | FR-035B-14 |
| UT-035B-15 | Commit message format | FR-035B-24 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-035B-01 | Full approval-apply flow | E2E |
| IT-035B-02 | Rejection persistence | FR-035B-09 |
| IT-035B-03 | Git commit creation | FR-035B-23 |
| IT-035B-04 | Hash mismatch handling | FR-035B-19 |
| IT-035B-05 | Bulk approval flow | FR-035B-31 |
| IT-035B-06 | Concurrent approval prevention | FR-035B-14 |
| IT-035B-07 | Audit log completeness | NFR-035B-27 |
| IT-035B-08 | Event emission | FR-035B-10 |
| IT-035B-09 | CLI approve command | FR-035B-46 |
| IT-035B-10 | CLI reject command | FR-035B-47 |
| IT-035B-11 | CLI apply command | FR-035B-48 |
| IT-035B-12 | Dry run preview | FR-035B-54 |
| IT-035B-13 | Exit codes | FR-035B-55 |
| IT-035B-14 | Rollback on git failure | FR-035B-22 |
| IT-035B-15 | Metrics emission | NFR-035B-24 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── CiCd/
│       └── Maintenance/
│           └── Approvals/
│               ├── ApprovalDecision.cs
│               └── Events/
│                   ├── ProposalApprovedEvent.cs
│                   ├── ProposalRejectedEvent.cs
│                   └── ChangeAppliedEvent.cs
├── Acode.Application/
│   └── CiCd/
│       └── Maintenance/
│           └── Approvals/
│               ├── IApprovalGate.cs
│               └── IChangeApplier.cs
├── Acode.Infrastructure/
│   └── CiCd/
│       └── Maintenance/
│           └── Approvals/
│               ├── ApprovalGate.cs
│               └── GitChangeApplier.cs
└── Acode.Cli/
    └── Commands/
        └── CiCd/
            └── Maintain/
                ├── ApproveCommand.cs
                ├── RejectCommand.cs
                └── ApplyCommand.cs
```

**End of Task 035.b Specification**
