# Task 036: Deployment Hook Tool

**Priority:** P1 – High  
**Tier:** L – Feature Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 8 – CI/CD Integration  
**Dependencies:** Task 034, Task 035, Task 039, Task 050 (Workspace DB)  

---

## Description

Task 036 implements the Deployment Hook Tool for post-release cleanup, notifications, and automated actions. Deployment hooks execute AFTER CI/CD workflows complete, enabling cleanup, artifact management, and notification dispatch. All execution MUST be auditable with complete provenance.

Deployment hooks are a sensitive operation that can modify production environments. By default, deployment hooks are DISABLED and require explicit enablement (Task 036.b). All deployments require non-bypassable approvals (Task 036.c) to prevent accidental or malicious execution.

The tool provides a schema-driven approach (Task 036.a) where hook definitions are validated against a strict schema before execution. This ensures type safety, parameter validation, and clear documentation of hook behavior.

Every deployment hook execution is recorded as a structured audit event in the Workspace DB (Task 050). Audit events include inputs (redacted), approvals, targets, artifacts produced, final status, and correlation fields (run_id, worktree_id, repo_sha).

### Business Value

Deployment hooks provide:
- Automated post-release cleanup and notifications
- Auditable deployment actions with provenance
- Secure-by-default operation (disabled until enabled)
- Non-bypassable approval workflow for safety

### Scope Boundaries

This task covers the core deployment hook engine and execution framework. The schema definition is in 036.a. Disabled-by-default logic is in 036.b. Approval gates are in 036.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Workspace DB | Task 050 | Audit events | Persistence |
| Audit System | Task 039 | Event records | Compliance |
| Export System | Task 021.c | Provenance | Bundles |
| CI Templates | Task 034 | Hook triggers | From workflows |
| Schema Validator | Task 036.a | Validation | Before exec |
| Approval Gate | Task 036.c | Approval | Required |
| Event Bus | `IEventPublisher` | Events | Async |
| Notification | `INotificationSender` | Alerts | Optional |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Hook execution fails | Exception | Retry or abort | Partial deployment |
| Approval timeout | Timer expires | Abort hook | Must re-initiate |
| Schema validation fails | Validator error | Reject hook | Hook not executed |
| DB audit write fails | Exception | Retry queue | Degraded audit |
| Notification fails | Service error | Log and continue | No alert |
| Provenance missing | Validation check | Block export | Must add provenance |
| Concurrent execution | Lock check | Queue or reject | Wait or retry |
| Redaction fails | Regex error | Fallback redact all | Over-redacted |

### Assumptions

1. **Workspace DB available**: Task 050 complete
2. **Approval system ready**: Task 036.c implemented
3. **Schema defined**: Task 036.a complete
4. **Audit system active**: Task 039 implemented
5. **Hooks disabled by default**: Task 036.b logic applied
6. **Git context available**: Repo SHA, worktree ID known
7. **Run ID assigned**: Unique execution identifier
8. **Network optional**: Notifications may fail in air-gapped

### Security Considerations

1. **Disabled by default**: Hooks require explicit enablement
2. **Non-bypassable approvals**: All hooks require approval
3. **Input redaction**: Secrets never in audit logs
4. **Provenance mandatory**: All artifacts tracked
5. **Audit immutable**: Events cannot be deleted
6. **Role-based access**: Future extension point
7. **Execution isolation**: Hooks run in sandbox
8. **No remote execution**: Hooks run locally only
9. **Hash verification**: Hook definitions versioned
10. **Timeout enforcement**: Hooks cannot run forever

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Deployment Hook | Action executed after deployment |
| Provenance | Origin tracking (SHA, run_id, etc.) |
| Audit Event | Immutable record of action |
| Redaction | Removing secrets from logs |
| Approval | Explicit user consent |
| Hook Schema | Definition structure |
| Run ID | Unique execution identifier |
| Worktree ID | Git worktree identifier |
| Sync State | Pending/Acked/Failed |
| Artifact | Output from hook execution |

---

## Out of Scope

- Remote/cloud hook execution
- Multi-tenant hook management
- Hook marketplace/sharing
- Visual hook builder
- Rollback automation
- Blue/green deployment management

---

## Functional Requirements

### FR-001 to FR-015: Hook Engine Interface

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036-01 | `IDeploymentHookEngine` interface MUST exist | P0 |
| FR-036-02 | `ExecuteHookAsync` MUST run a hook definition | P0 |
| FR-036-03 | Hook MUST NOT execute if disabled (036.b) | P0 |
| FR-036-04 | Hook MUST require approval (036.c) | P0 |
| FR-036-05 | Hook MUST validate against schema (036.a) | P0 |
| FR-036-06 | Hook execution MUST be atomic | P0 |
| FR-036-07 | Hook timeout MUST be enforced | P0 |
| FR-036-08 | Default timeout MUST be 5 minutes | P1 |
| FR-036-09 | Timeout MUST be configurable | P1 |
| FR-036-10 | Hook MUST emit events on start | P0 |
| FR-036-11 | Hook MUST emit events on complete | P0 |
| FR-036-12 | Hook MUST emit events on failure | P0 |
| FR-036-13 | Hook execution MUST be logged | P0 |
| FR-036-14 | Hook MUST support cancellation | P1 |
| FR-036-15 | Concurrent hooks MUST be serialized | P1 |

### FR-016 to FR-030: Audit Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036-16 | Hook execution MUST create audit event | P0 |
| FR-036-17 | Audit event MUST include run_id | P0 |
| FR-036-18 | Audit event MUST include worktree_id | P0 |
| FR-036-19 | Audit event MUST include repo_sha | P0 |
| FR-036-20 | Audit event MUST include timestamp | P0 |
| FR-036-21 | Audit event MUST include hook name | P0 |
| FR-036-22 | Audit event MUST include inputs (redacted) | P0 |
| FR-036-23 | Audit event MUST include approvals | P0 |
| FR-036-24 | Audit event MUST include targets | P0 |
| FR-036-25 | Audit event MUST include artifacts | P0 |
| FR-036-26 | Audit event MUST include status | P0 |
| FR-036-27 | Audit event MUST include duration | P1 |
| FR-036-28 | Audit event MUST have sync_state | P0 |
| FR-036-29 | Sync states: Pending, Acked, Failed | P0 |
| FR-036-30 | Audit write failure MUST NOT block hook | P1 |

### FR-031 to FR-045: Provenance Tracking

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036-31 | All artifacts MUST have provenance | P0 |
| FR-036-32 | Provenance MUST include repo_sha | P0 |
| FR-036-33 | Provenance MUST include run_id | P0 |
| FR-036-34 | Provenance MUST include worktree_id | P0 |
| FR-036-35 | Provenance MUST include session_id | P1 |
| FR-036-36 | Provenance MUST include timestamps | P0 |
| FR-036-37 | Provenance MUST be exportable | P0 |
| FR-036-38 | Export bundles MUST include provenance | P0 |
| FR-036-39 | Artifacts MUST be redacted before export | P0 |
| FR-036-40 | Redaction MUST use pattern matching | P0 |
| FR-036-41 | Redaction patterns MUST be configurable | P1 |
| FR-036-42 | Provenance MUST be queryable | P1 |
| FR-036-43 | Provenance MUST support search by SHA | P1 |
| FR-036-44 | Provenance MUST support search by date | P2 |
| FR-036-45 | Provenance MUST be tamper-evident | P1 |

### FR-046 to FR-060: Hook Types

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036-46 | Post-release cleanup hook MUST be supported | P0 |
| FR-036-47 | Notification hook MUST be supported | P0 |
| FR-036-48 | Artifact upload hook MUST be supported | P1 |
| FR-036-49 | Cache invalidation hook MUST be supported | P1 |
| FR-036-50 | Metric recording hook MUST be supported | P2 |
| FR-036-51 | Custom script hook MUST be supported | P1 |
| FR-036-52 | Cleanup hook MUST delete temp files | P0 |
| FR-036-53 | Notification hook MUST support multiple channels | P1 |
| FR-036-54 | Hook chaining MUST be supported | P2 |
| FR-036-55 | Hook dependencies MUST be resolvable | P2 |
| FR-036-56 | Hook templates MUST be available | P1 |
| FR-036-57 | Hooks MUST be version-controlled | P0 |
| FR-036-58 | Hook versioning MUST use semantic versioning | P2 |
| FR-036-59 | Hook rollback MUST be supported | P2 |
| FR-036-60 | Hooks MUST support dry-run | P0 |

### FR-061 to FR-075: CLI Commands

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036-61 | `acode deploy hooks list` MUST work | P0 |
| FR-036-62 | `acode deploy hooks show <name>` MUST work | P0 |
| FR-036-63 | `acode deploy hooks run <name>` MUST work | P0 |
| FR-036-64 | `acode deploy hooks run --dry-run` MUST work | P0 |
| FR-036-65 | `acode deploy hooks validate` MUST work | P0 |
| FR-036-66 | `acode deploy hooks history` MUST work | P1 |
| FR-036-67 | `acode deploy hooks audit <run_id>` MUST work | P1 |
| FR-036-68 | Exit code 0 on success | P0 |
| FR-036-69 | Exit code 1 on failure | P0 |
| FR-036-70 | Exit code 2 on validation error | P0 |
| FR-036-71 | Exit code 3 on approval denied | P0 |
| FR-036-72 | `--json` output MUST work | P1 |
| FR-036-73 | `--verbose` MUST show details | P2 |
| FR-036-74 | Help text MUST be complete | P1 |
| FR-036-75 | Progress indicator MUST show | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-036-01 | Hook validation latency | <100ms | P1 |
| NFR-036-02 | Hook startup latency | <500ms | P1 |
| NFR-036-03 | Audit event write | <50ms | P1 |
| NFR-036-04 | Provenance lookup | <20ms | P1 |
| NFR-036-05 | History query (last 100) | <200ms | P2 |
| NFR-036-06 | Concurrent hook queuing | <10ms | P2 |
| NFR-036-07 | Redaction processing | <100ms/MB | P1 |
| NFR-036-08 | CLI response time | <200ms | P1 |
| NFR-036-09 | Memory per hook | <100MB | P2 |
| NFR-036-10 | Default timeout | 5 minutes | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-036-11 | Audit event persistence | 99.99% | P0 |
| NFR-036-12 | Hook execution success | >95% | P1 |
| NFR-036-13 | Provenance accuracy | 100% | P0 |
| NFR-036-14 | Redaction completeness | 100% | P0 |
| NFR-036-15 | Approval enforcement | 100% | P0 |
| NFR-036-16 | Disabled-by-default enforcement | 100% | P0 |
| NFR-036-17 | Graceful error recovery | Always | P1 |
| NFR-036-18 | Crash recovery | Resume or report | P1 |
| NFR-036-19 | Concurrent access safety | No corruption | P0 |
| NFR-036-20 | Idempotent where applicable | Yes | P1 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-036-21 | Structured logging | JSON format | P0 |
| NFR-036-22 | Metrics: hooks executed | Counter | P1 |
| NFR-036-23 | Metrics: hooks failed | Counter | P1 |
| NFR-036-24 | Metrics: execution duration | Histogram | P1 |
| NFR-036-25 | Events: hook started | Required | P0 |
| NFR-036-26 | Events: hook completed | Required | P0 |
| NFR-036-27 | Events: hook failed | Required | P0 |
| NFR-036-28 | Events: approval requested | Required | P0 |
| NFR-036-29 | Events: approval granted | Required | P0 |
| NFR-036-30 | Audit trail completeness | 100% | P0 |

---

## Mode Compliance

| Mode | Deployment Hook Behavior |
|------|--------------------------|
| Local-Only | Full hook support, no network notifications |
| Burst | Same as Local-Only |
| Air-Gapped | Full support, network hooks disabled |

---

## User Manual Documentation

### Configuration Example

```yaml
# .agent/config.yml
deployment:
  hooks:
    enabled: true  # Must be explicitly enabled
    timeout: 300   # 5 minutes
    require_approval: true
    
    definitions:
      - name: post-release-cleanup
        type: cleanup
        on: release
        actions:
          - delete: "tmp/**/*"
          - delete: "build/cache/**"
        
      - name: notify-team
        type: notification
        on: release
        channels:
          - type: file
            path: ".agent/notifications.log"
```

### CLI Usage

```bash
# List available hooks
acode deploy hooks list

# Show hook details
acode deploy hooks show post-release-cleanup

# Validate hook definitions
acode deploy hooks validate

# Run hook (requires approval)
acode deploy hooks run post-release-cleanup

# Dry run (no approval needed)
acode deploy hooks run post-release-cleanup --dry-run

# View hook execution history
acode deploy hooks history

# View specific execution audit
acode deploy hooks audit RUN-abc123
```

### Output Example

```
╔══════════════════════════════════════════════════════════════╗
║ Deployment Hook: post-release-cleanup                        ║
╠══════════════════════════════════════════════════════════════╣
║ Status: PENDING APPROVAL                                     ║
║ Run ID: RUN-abc123                                           ║
║ Repo SHA: 7a8b9c...                                          ║
║ Worktree: main                                               ║
╠══════════════════════════════════════════════════════════════╣
║ Actions to execute:                                          ║
║   1. Delete tmp/**/* (12 files)                              ║
║   2. Delete build/cache/** (45 files)                        ║
╠══════════════════════════════════════════════════════════════╣
║ [?] Approve execution? (y/N):                                ║
╚══════════════════════════════════════════════════════════════╝
```

---

## Acceptance Criteria / Definition of Done

### Core Engine
- [ ] AC-001: `IDeploymentHookEngine` interface exists
- [ ] AC-002: `ExecuteHookAsync` runs hooks
- [ ] AC-003: Disabled hooks blocked
- [ ] AC-004: Unapproved hooks blocked
- [ ] AC-005: Schema validation required
- [ ] AC-006: Atomic execution
- [ ] AC-007: Timeout enforced
- [ ] AC-008: Events emitted
- [ ] AC-009: Cancellation supported
- [ ] AC-010: Serialized execution

### Audit Integration
- [ ] AC-011: Audit events created
- [ ] AC-012: run_id included
- [ ] AC-013: worktree_id included
- [ ] AC-014: repo_sha included
- [ ] AC-015: Inputs redacted
- [ ] AC-016: Approvals recorded
- [ ] AC-017: Status tracked
- [ ] AC-018: Sync state managed
- [ ] AC-019: Duration captured
- [ ] AC-020: Write failures handled

### Provenance
- [ ] AC-021: All artifacts tracked
- [ ] AC-022: Provenance fields complete
- [ ] AC-023: Export includes provenance
- [ ] AC-024: Redaction before export
- [ ] AC-025: Provenance queryable
- [ ] AC-026: Tamper-evident
- [ ] AC-027: Search by SHA works
- [ ] AC-028: Search by date works

### Hook Types
- [ ] AC-029: Cleanup hook works
- [ ] AC-030: Notification hook works
- [ ] AC-031: Custom script hook works
- [ ] AC-032: Dry-run supported
- [ ] AC-033: Hook versioning works
- [ ] AC-034: Templates available

### CLI
- [ ] AC-035: `hooks list` works
- [ ] AC-036: `hooks show` works
- [ ] AC-037: `hooks run` works
- [ ] AC-038: `hooks validate` works
- [ ] AC-039: `hooks history` works
- [ ] AC-040: `hooks audit` works
- [ ] AC-041: Exit codes correct
- [ ] AC-042: JSON output works
- [ ] AC-043: Help text complete
- [ ] AC-044: Progress shown

### Safety
- [ ] AC-045: Disabled by default
- [ ] AC-046: Approval required
- [ ] AC-047: No bypass possible
- [ ] AC-048: Secrets redacted
- [ ] AC-049: Audit immutable
- [ ] AC-050: Execution isolated

---

## User Verification Scenarios

### Scenario 1: Run Cleanup Hook
**Persona:** Developer after release  
**Preconditions:** Hooks enabled, cleanup hook defined  
**Steps:**
1. Run `acode deploy hooks run post-release-cleanup`
2. Approval prompt appears
3. Approve execution
4. Hook runs
5. Cleanup completed

**Verification Checklist:**
- [ ] Approval required
- [ ] Hook executes
- [ ] Files cleaned
- [ ] Audit logged

### Scenario 2: Hook Disabled by Default
**Persona:** New user with fresh config  
**Preconditions:** Default configuration  
**Steps:**
1. Try to run any hook
2. Error: hooks disabled
3. Enable in config
4. Hook now runnable

**Verification Checklist:**
- [ ] Disabled by default
- [ ] Clear error message
- [ ] Enable works
- [ ] Then runs

### Scenario 3: Dry Run
**Persona:** Developer testing hook  
**Preconditions:** Hook defined  
**Steps:**
1. Run `acode deploy hooks run cleanup --dry-run`
2. No approval needed
3. See what would happen
4. No actual changes

**Verification Checklist:**
- [ ] No approval needed
- [ ] Preview shown
- [ ] No files changed
- [ ] Clear output

### Scenario 4: View Audit Trail
**Persona:** Compliance reviewer  
**Preconditions:** Hooks have been run  
**Steps:**
1. Run `acode deploy hooks history`
2. See all executions
3. Run `acode deploy hooks audit RUN-123`
4. See full details

**Verification Checklist:**
- [ ] History shows all
- [ ] Audit shows details
- [ ] Provenance complete
- [ ] Inputs redacted

### Scenario 5: Approval Denied
**Persona:** Developer who changes mind  
**Preconditions:** Hook ready to run  
**Steps:**
1. Run `acode deploy hooks run cleanup`
2. Approval prompt
3. Enter N (no)
4. Hook not executed

**Verification Checklist:**
- [ ] Prompt appears
- [ ] Denial accepted
- [ ] Hook not run
- [ ] Audit shows denial

### Scenario 6: Export with Provenance
**Persona:** Developer exporting results  
**Preconditions:** Hooks executed with artifacts  
**Steps:**
1. Export results bundle
2. Check provenance metadata
3. Verify redaction
4. Confirm traceability

**Verification Checklist:**
- [ ] Provenance in export
- [ ] All fields present
- [ ] Secrets redacted
- [ ] Traceable to SHA

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-036-01 | Hook engine interface | FR-036-01 |
| UT-036-02 | Disabled hook blocked | FR-036-03 |
| UT-036-03 | Unapproved hook blocked | FR-036-04 |
| UT-036-04 | Schema validation | FR-036-05 |
| UT-036-05 | Timeout enforcement | FR-036-07 |
| UT-036-06 | Event emission | FR-036-10 |
| UT-036-07 | Audit event creation | FR-036-16 |
| UT-036-08 | Provenance fields | FR-036-31 |
| UT-036-09 | Redaction patterns | FR-036-40 |
| UT-036-10 | Cleanup hook | FR-036-46 |
| UT-036-11 | Notification hook | FR-036-47 |
| UT-036-12 | Dry-run mode | FR-036-60 |
| UT-036-13 | Exit codes | FR-036-68 |
| UT-036-14 | Serialized execution | FR-036-15 |
| UT-036-15 | Validation < 100ms | NFR-036-01 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-036-01 | Full hook execution flow | E2E |
| IT-036-02 | Audit persistence | FR-036-16 |
| IT-036-03 | Provenance export | FR-036-38 |
| IT-036-04 | Approval workflow | FR-036-04 |
| IT-036-05 | CLI commands | FR-036-61 |
| IT-036-06 | History query | FR-036-66 |
| IT-036-07 | Redaction completeness | NFR-036-14 |
| IT-036-08 | Concurrent execution | FR-036-15 |
| IT-036-09 | Timeout handling | FR-036-07 |
| IT-036-10 | Cancellation | FR-036-14 |
| IT-036-11 | Workspace DB integration | Task 050 |
| IT-036-12 | Audit system integration | Task 039 |
| IT-036-13 | Export system integration | Task 021.c |
| IT-036-14 | Error recovery | NFR-036-17 |
| IT-036-15 | Performance benchmarks | NFR-036-01 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Deployment/
│       └── Hooks/
│           ├── DeploymentHook.cs
│           ├── HookExecution.cs
│           ├── HookResult.cs
│           ├── Provenance.cs
│           └── Events/
│               ├── HookStartedEvent.cs
│               ├── HookCompletedEvent.cs
│               └── HookFailedEvent.cs
├── Acode.Application/
│   └── Deployment/
│       └── Hooks/
│           ├── IDeploymentHookEngine.cs
│           ├── IHookValidator.cs
│           └── IProvenanceTracker.cs
├── Acode.Infrastructure/
│   └── Deployment/
│       └── Hooks/
│           ├── DeploymentHookEngine.cs
│           ├── HookValidator.cs
│           ├── ProvenanceTracker.cs
│           └── Executors/
│               ├── CleanupHookExecutor.cs
│               ├── NotificationHookExecutor.cs
│               └── ScriptHookExecutor.cs
└── Acode.Cli/
    └── Commands/
        └── Deploy/
            └── Hooks/
                ├── ListHooksCommand.cs
                ├── ShowHookCommand.cs
                ├── RunHookCommand.cs
                ├── ValidateHooksCommand.cs
                ├── HistoryCommand.cs
                └── AuditCommand.cs
```

**End of Task 036 Specification**
