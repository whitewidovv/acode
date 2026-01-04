# Task 035: CI Maintenance Mode

**Priority:** P1 – High  
**Tier:** L – Feature Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 8 – CI/CD Integration  
**Dependencies:** Task 034 (CI Template Generator), Epic 05 (Git Automation)  

---

## Description

Task 035 implements CI Maintenance Mode for managing existing CI workflows. Unlike generation (Task 034), maintenance mode analyzes, proposes changes to, and safely updates existing workflows. Changes MUST be proposed as diffs with approval gates.

CI workflows evolve over time. Security patches, dependency updates, and best practice improvements need to be applied systematically. Maintenance mode provides a safe, auditable way to update workflows without regenerating from scratch.

The maintenance engine scans existing workflows, detects outdated patterns, and proposes improvements. Proposals are shown as diffs that require approval before applying. This prevents accidental breaking changes and maintains audit trails.

This task provides the core maintenance infrastructure. Subtasks cover diff generation (035.a), approval gates (035.b), and CI-specific task runner support (035.c).

### Business Value

CI Maintenance Mode provides:
- Safe workflow updates with review
- Automated security patching
- Consistent best practice enforcement
- Audit trail for all changes

### Scope Boundaries

This task covers maintenance infrastructure. Diff proposals are in 035.a. Approval gates are in 035.b. Task runner support is in 035.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Template Generator | Task 034 | Reference patterns | Best practices |
| Git Automation | Epic 05 | Commit changes | After approval |
| Diff Engine | Task 035.a | Generate proposals | Change diffs |
| Approval Gates | Task 035.b | Gate application | User approval |
| Security Module | Task 034.b | Version updates | SHA pinning |
| Audit Events | `IEventPublisher` | Log all changes | Required |
| Workspace DB | Task 050 | Store proposals | Persistence |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Workflow parse fails | YAML parse error | Skip file, log error | Manual review needed |
| Pattern not recognized | Unknown structure | Skip pattern, continue | Partial analysis |
| Proposal generation fails | Exception | Return partial results | Some proposals missing |
| Approval timeout | Deadline exceeded | Proposal expires | Must re-run |
| Git commit fails | Git error | Rollback, retry | Changes not applied |
| Concurrent modification | Git conflict | Abort, re-analyze | Re-run required |
| Network unavailable | Version lookup fails | Use cached versions | Outdated suggestions |
| Invalid workflow after edit | Validation fails | Reject change | Original preserved |

### Mode Compliance

| Operating Mode | Analysis | Proposal | Apply Changes |
|----------------|----------|----------|---------------|
| Local-Only | ALLOWED | ALLOWED | LOCAL ONLY |
| Burst | ALLOWED | ALLOWED | ALLOWED |
| Air-Gapped | ALLOWED | ALLOWED | LOCAL ONLY |

### Assumptions

1. **Workflows are YAML**: All CI workflows are valid YAML files
2. **GitHub Actions format**: Primary focus on GitHub Actions syntax
3. **Git repository**: Changes committed via Git
4. **User approval required**: No automatic application without consent
5. **Existing workflows parseable**: Workflows follow standard structure
6. **Version info accessible**: Can lookup current action versions
7. **Diff tooling available**: Can generate unified diffs
8. **Backup before change**: Original preserved before modification

### Security Considerations

1. **No auto-apply**: Changes require explicit user approval
2. **Audit all proposals**: Every proposal is logged with context
3. **Validate after edit**: Post-edit validation ensures workflow valid
4. **Rollback capability**: Can revert to original on failure
5. **Version verification**: Updated versions verified against registry
6. **No credential exposure**: Diffs never show secret values
7. **Change attribution**: All changes attributed to user/session
8. **Approval timeout**: Unapproved proposals expire

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Maintenance Mode | Mode for updating existing workflows |
| Proposal | Suggested change with diff |
| Approval Gate | Required user confirmation |
| Pattern | Detectable improvement opportunity |
| Diff | Unified diff of proposed change |
| Outdated | Using old version or pattern |
| Best Practice | Recommended configuration |
| Rollback | Revert to previous state |

---

## Out of Scope

- Automatic application without approval
- GitLab CI/CD maintenance
- Azure DevOps pipeline maintenance
- Jenkins pipeline maintenance
- Custom CI platform support
- Semantic workflow analysis (behavior changes)

---

## Functional Requirements

### FR-001 to FR-015: Maintenance Engine Interface

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035-01 | `ICiMaintenanceEngine` interface MUST exist | P0 |
| FR-035-02 | `AnalyzeAsync` MUST scan existing workflows | P0 |
| FR-035-03 | Input MUST include workflow directory path | P0 |
| FR-035-04 | Output MUST return list of detected issues | P0 |
| FR-035-05 | `ProposeChangesAsync` MUST generate proposals | P0 |
| FR-035-06 | Proposals MUST include diff for each change | P0 |
| FR-035-07 | `ApplyApprovedAsync` MUST apply approved changes | P0 |
| FR-035-08 | Apply MUST require prior approval | P0 |
| FR-035-09 | Engine MUST be extensible for new patterns | P1 |
| FR-035-10 | Pattern detectors MUST be pluggable | P1 |
| FR-035-11 | Engine MUST log all operations | P1 |
| FR-035-12 | Engine MUST emit metrics | P2 |
| FR-035-13 | Engine MUST support dry-run mode | P1 |
| FR-035-14 | Dry-run MUST show proposals without applying | P1 |
| FR-035-15 | Engine MUST validate workflows after changes | P0 |

### FR-016 to FR-030: Pattern Detection

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035-16 | Outdated action versions MUST be detected | P0 |
| FR-035-17 | Unpinned action versions MUST be flagged | P0 |
| FR-035-18 | Missing permissions block MUST be detected | P0 |
| FR-035-19 | Overly broad permissions MUST be flagged | P0 |
| FR-035-20 | Missing concurrency block MUST be detected | P1 |
| FR-035-21 | Deprecated actions MUST be flagged | P1 |
| FR-035-22 | Security vulnerabilities MUST be detected | P0 |
| FR-035-23 | Missing timeout limits MUST be flagged | P2 |
| FR-035-24 | Inefficient patterns MUST be detected | P2 |
| FR-035-25 | Missing cache configuration MUST be suggested | P2 |
| FR-035-26 | Each pattern MUST have severity level | P1 |
| FR-035-27 | Severity: Critical, High, Medium, Low | P1 |
| FR-035-28 | Patterns MUST include remediation | P0 |
| FR-035-29 | Remediation MUST be actionable | P0 |
| FR-035-30 | Patterns MUST be configurable (enable/disable) | P1 |

### FR-031 to FR-045: Proposal Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035-31 | Proposals MUST have unique IDs | P0 |
| FR-035-32 | Proposals MUST include file path | P0 |
| FR-035-33 | Proposals MUST include line numbers | P1 |
| FR-035-34 | Proposals MUST include before/after | P0 |
| FR-035-35 | Proposals MUST include rationale | P0 |
| FR-035-36 | Proposals MUST include severity | P0 |
| FR-035-37 | Proposals MUST be persisted | P1 |
| FR-035-38 | Proposals MUST have expiration | P2 |
| FR-035-39 | Default expiration: 24 hours | P2 |
| FR-035-40 | Proposals MUST support batch approval | P1 |
| FR-035-41 | Proposals MUST support selective approval | P0 |
| FR-035-42 | Rejected proposals MUST be logged | P1 |
| FR-035-43 | Applied proposals MUST be logged | P0 |
| FR-035-44 | Proposals MUST be viewable in CLI | P0 |
| FR-035-45 | Proposals MUST support JSON export | P2 |

### FR-046 to FR-060: Change Application

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035-46 | Changes MUST be applied atomically | P0 |
| FR-035-47 | Partial failure MUST rollback all | P0 |
| FR-035-48 | Original file MUST be backed up | P0 |
| FR-035-49 | Backup location configurable | P2 |
| FR-035-50 | Post-apply validation MUST run | P0 |
| FR-035-51 | Invalid result MUST trigger rollback | P0 |
| FR-035-52 | Git commit MUST be optional | P1 |
| FR-035-53 | Commit message MUST be descriptive | P1 |
| FR-035-54 | Commit MUST reference proposal IDs | P2 |
| FR-035-55 | Changes MUST emit events | P0 |
| FR-035-56 | Events MUST include before/after hashes | P1 |
| FR-035-57 | Concurrent changes MUST be prevented | P0 |
| FR-035-58 | Lock file MUST prevent races | P1 |
| FR-035-59 | Lock timeout: 5 minutes | P2 |
| FR-035-60 | Manual unlock MUST be available | P2 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-035-01 | Workflow analysis latency | <2 seconds per file | P1 |
| NFR-035-02 | Pattern detection latency | <500ms per pattern | P1 |
| NFR-035-03 | Proposal generation latency | <1 second per issue | P1 |
| NFR-035-04 | Change application latency | <500ms per file | P1 |
| NFR-035-05 | Memory usage during analysis | <100MB | P2 |
| NFR-035-06 | Concurrent file analysis | 10 parallel | P2 |
| NFR-035-07 | Proposal storage size | <1KB per proposal | P2 |
| NFR-035-08 | Validation latency | <200ms | P1 |
| NFR-035-09 | Backup operation latency | <100ms | P2 |
| NFR-035-10 | Lock acquisition latency | <50ms | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-035-11 | No data loss on failure | 100% | P0 |
| NFR-035-12 | Rollback success rate | 100% | P0 |
| NFR-035-13 | Valid output after changes | 100% | P0 |
| NFR-035-14 | Pattern detection accuracy | >95% | P0 |
| NFR-035-15 | Proposal persistence durability | 99.99% | P1 |
| NFR-035-16 | Atomic change application | Always | P0 |
| NFR-035-17 | Backup availability | Always | P0 |
| NFR-035-18 | Lock correctness | No races | P0 |
| NFR-035-19 | Graceful degradation | Partial results | P1 |
| NFR-035-20 | Crash recovery | Resume from backup | P1 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-035-21 | Structured logging for all ops | JSON format | P1 |
| NFR-035-22 | Metrics on issues detected | Counter | P1 |
| NFR-035-23 | Metrics on proposals generated | Counter | P1 |
| NFR-035-24 | Metrics on changes applied | Counter | P1 |
| NFR-035-25 | Events on analysis complete | Async publish | P1 |
| NFR-035-26 | Events on proposal approved | Async publish | P1 |
| NFR-035-27 | Events on changes applied | Async publish | P0 |
| NFR-035-28 | Audit trail for all changes | Required | P0 |
| NFR-035-29 | Dashboard metrics support | Prometheus | P2 |
| NFR-035-30 | Alert on critical issues | Configurable | P2 |

---

## User Manual Documentation

### Configuration

```yaml
ciMaintenance:
  enabled: true
  workflowPath: .github/workflows
  patterns:
    outdatedVersions: true
    unpinnedActions: true
    missingPermissions: true
    broadPermissions: true
    missingConcurrency: true
    missingTimeout: true
  proposalExpiration: 24h
  autoCommit: false
  backupPath: .acode/backups/workflows
```

### CLI Usage

```bash
# Analyze workflows for issues
acode ci maintain analyze

# Show current proposals
acode ci maintain proposals

# Approve specific proposal
acode ci maintain approve PROP-001

# Approve all proposals
acode ci maintain approve --all

# Apply approved changes
acode ci maintain apply

# Dry-run (preview only)
acode ci maintain apply --dry-run

# Reject proposal
acode ci maintain reject PROP-002

# Show analysis summary
acode ci maintain status
```

### Example Output

```
CI Workflow Analysis Results
============================

Analyzed: 3 workflows
Issues Found: 7

Critical (2):
  - ci.yml: Action 'actions/checkout' uses unpinned version @v4
  - ci.yml: Missing 'permissions' block

High (3):
  - ci.yml: Action 'actions/setup-dotnet' outdated (v3 → v4)
  - deploy.yml: Overly broad permissions (write-all)
  - test.yml: Missing concurrency block

Medium (2):
  - ci.yml: Missing timeout-minutes
  - deploy.yml: Missing cache configuration

Run 'acode ci maintain proposals' to view change proposals.
```

---

## Acceptance Criteria / Definition of Done

### Maintenance Engine Interface
- [ ] AC-001: `ICiMaintenanceEngine` interface exists
- [ ] AC-002: `AnalyzeAsync` scans workflows
- [ ] AC-003: `ProposeChangesAsync` generates proposals
- [ ] AC-004: `ApplyApprovedAsync` applies changes
- [ ] AC-005: Engine is extensible
- [ ] AC-006: Pattern detectors pluggable
- [ ] AC-007: Dry-run mode works

### Pattern Detection
- [ ] AC-008: Outdated versions detected
- [ ] AC-009: Unpinned actions flagged
- [ ] AC-010: Missing permissions detected
- [ ] AC-011: Broad permissions flagged
- [ ] AC-012: Missing concurrency detected
- [ ] AC-013: Deprecated actions flagged
- [ ] AC-014: Security issues detected
- [ ] AC-015: Severity levels assigned

### Proposal Management
- [ ] AC-016: Proposals have unique IDs
- [ ] AC-017: Proposals include file path
- [ ] AC-018: Proposals include diff
- [ ] AC-019: Proposals include rationale
- [ ] AC-020: Proposals persisted
- [ ] AC-021: Proposals expire
- [ ] AC-022: Batch approval works
- [ ] AC-023: Selective approval works
- [ ] AC-024: CLI shows proposals

### Change Application
- [ ] AC-025: Atomic application
- [ ] AC-026: Rollback on failure
- [ ] AC-027: Backup created
- [ ] AC-028: Validation after apply
- [ ] AC-029: Invalid triggers rollback
- [ ] AC-030: Git commit optional
- [ ] AC-031: Descriptive commit message
- [ ] AC-032: Events emitted

### Concurrency Control
- [ ] AC-033: Lock prevents races
- [ ] AC-034: Lock timeout works
- [ ] AC-035: Manual unlock available

### Analysis Output
- [ ] AC-036: Summary displayed
- [ ] AC-037: Issues categorized by severity
- [ ] AC-038: File paths shown
- [ ] AC-039: Remediation suggested

### CLI Commands
- [ ] AC-040: `analyze` command works
- [ ] AC-041: `proposals` command works
- [ ] AC-042: `approve` command works
- [ ] AC-043: `reject` command works
- [ ] AC-044: `apply` command works
- [ ] AC-045: `status` command works

### Audit and Logging
- [ ] AC-046: All operations logged
- [ ] AC-047: Proposals logged
- [ ] AC-048: Approvals logged
- [ ] AC-049: Applications logged
- [ ] AC-050: Rejections logged

---

## User Verification Scenarios

### Scenario 1: Detect Outdated Action
**Persona:** Developer with old workflow  
**Preconditions:** Workflow uses actions/checkout@v3  
**Steps:**
1. Run `acode ci maintain analyze`
2. See outdated version issue
3. View proposal
4. Approve and apply

**Verification Checklist:**
- [ ] v3 detected as outdated
- [ ] Proposal shows v4 update
- [ ] Diff is accurate
- [ ] Apply updates version

### Scenario 2: Fix Missing Permissions
**Persona:** Security-conscious developer  
**Preconditions:** Workflow lacks permissions block  
**Steps:**
1. Run `acode ci maintain analyze`
2. See critical issue
3. View proposed fix
4. Approve and apply

**Verification Checklist:**
- [ ] Missing permissions detected
- [ ] Proposal adds block
- [ ] Default is contents: read
- [ ] Workflow valid after

### Scenario 3: Batch Approve All
**Persona:** Developer with many issues  
**Preconditions:** Multiple proposals pending  
**Steps:**
1. Run `acode ci maintain analyze`
2. View all proposals
3. Run `approve --all`
4. Apply changes

**Verification Checklist:**
- [ ] All proposals approved
- [ ] All changes applied
- [ ] Backup created
- [ ] Workflows valid

### Scenario 4: Selective Approval
**Persona:** Careful developer  
**Preconditions:** Mix of proposals  
**Steps:**
1. View proposals
2. Approve only PROP-001
3. Reject PROP-002
4. Apply approved only

**Verification Checklist:**
- [ ] Only selected applied
- [ ] Rejected logged
- [ ] Partial update works
- [ ] Remaining proposals kept

### Scenario 5: Rollback on Invalid
**Persona:** Developer with complex workflow  
**Preconditions:** Proposal would break workflow  
**Steps:**
1. Approve proposal
2. Apply (would create invalid YAML)
3. Validation fails
4. Rollback occurs

**Verification Checklist:**
- [ ] Validation catches error
- [ ] Rollback triggered
- [ ] Original restored
- [ ] Error logged

### Scenario 6: Dry-Run Preview
**Persona:** Developer previewing changes  
**Preconditions:** Proposals pending  
**Steps:**
1. Run `acode ci maintain apply --dry-run`
2. See what would change
3. No files modified
4. Decide to proceed or not

**Verification Checklist:**
- [ ] Changes displayed
- [ ] No files modified
- [ ] Can review safely
- [ ] Then apply if desired

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-035-01 | YAML workflow parsing | FR-035-02 |
| UT-035-02 | Outdated version detection | FR-035-16 |
| UT-035-03 | Unpinned action detection | FR-035-17 |
| UT-035-04 | Missing permissions detection | FR-035-18 |
| UT-035-05 | Broad permissions detection | FR-035-19 |
| UT-035-06 | Proposal ID generation | FR-035-31 |
| UT-035-07 | Diff generation | FR-035-06 |
| UT-035-08 | Proposal persistence | FR-035-37 |
| UT-035-09 | Proposal expiration | FR-035-38 |
| UT-035-10 | Change application | FR-035-46 |
| UT-035-11 | Rollback mechanism | FR-035-47 |
| UT-035-12 | Backup creation | FR-035-48 |
| UT-035-13 | Post-apply validation | FR-035-50 |
| UT-035-14 | Lock acquisition | FR-035-58 |
| UT-035-15 | Analysis < 2s per file | NFR-035-01 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-035-01 | Full analysis pipeline | E2E |
| IT-035-02 | Proposal generation flow | E2E |
| IT-035-03 | Approve and apply flow | E2E |
| IT-035-04 | Rollback on validation fail | NFR-035-12 |
| IT-035-05 | Dry-run mode | FR-035-13 |
| IT-035-06 | Git commit integration | FR-035-52 |
| IT-035-07 | Concurrent modification lock | FR-035-57 |
| IT-035-08 | Event emission | NFR-035-27 |
| IT-035-09 | Batch approval | FR-035-40 |
| IT-035-10 | Selective approval | FR-035-41 |
| IT-035-11 | Proposal persistence | FR-035-37 |
| IT-035-12 | CLI commands | AC-040 |
| IT-035-13 | Multiple workflows | Complex |
| IT-035-14 | Audit logging | NFR-035-28 |
| IT-035-15 | Backup restoration | FR-035-48 |

---

## Implementation Prompt

### Part 1: File Structure

```
src/
├── Acode.Domain/
│   └── CiCd/
│       └── Maintenance/
│           ├── MaintenanceIssue.cs
│           ├── IssueSeverity.cs
│           ├── Proposal.cs
│           ├── ProposalStatus.cs
│           └── Events/
│               ├── WorkflowAnalyzedEvent.cs
│               ├── ProposalCreatedEvent.cs
│               ├── ProposalApprovedEvent.cs
│               └── ChangesAppliedEvent.cs
├── Acode.Application/
│   └── CiCd/
│       └── Maintenance/
│           ├── ICiMaintenanceEngine.cs
│           ├── IPatternDetector.cs
│           ├── IPatternRegistry.cs
│           ├── IProposalStore.cs
│           ├── AnalysisResult.cs
│           └── ApplyResult.cs
└── Acode.Infrastructure/
    └── CiCd/
        └── Maintenance/
            ├── CiMaintenanceEngine.cs
            ├── PatternRegistry.cs
            ├── ProposalStore.cs
            ├── WorkflowBackupService.cs
            └── Patterns/
                ├── OutdatedVersionPattern.cs
                ├── UnpinnedActionPattern.cs
                ├── MissingPermissionsPattern.cs
                └── BroadPermissionsPattern.cs
```

### Implementation Checklist

| Step | Action | Verification |
|------|--------|--------------|
| 1 | Define MaintenanceIssue and Proposal | Models compile |
| 2 | Create ICiMaintenanceEngine interface | Contract clear |
| 3 | Implement pattern detectors | Patterns detected |
| 4 | Implement proposal generation | Diffs generated |
| 5 | Implement proposal storage | Persistence works |
| 6 | Implement change application | Atomic apply |
| 7 | Implement rollback | Restore on fail |
| 8 | Add validation | Invalid blocked |
| 9 | Add locking | No races |
| 10 | Add CLI commands | Commands work |
| 11 | Add event publishing | Events emitted |
| 12 | Add Git integration | Commits work |

**End of Task 035 Specification**
