# Task 048.c: Regression Triage Workflow

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 12 – Hardening  
**Dependencies:** Task 048 (Baseline), Task 048.a (Recording), Task 048.b (Change Log)  

---

## Description

Task 048.c implements the regression triage workflow—a structured process for investigating, categorizing, and resolving benchmark regressions. When a regression is detected (performance drops, pass rates decrease, or metrics degrade), this workflow guides developers through investigation, root cause analysis, and resolution.

The workflow covers: (1) regression detection and notification, (2) automated diagnostics, (3) change correlation (linking regressions to recent changes via Task 048.b), (4) categorization (true regression vs. flaky test vs. expected change), (5) resolution tracking, and (6) closure with documentation. This ensures regressions are systematically addressed rather than ignored.

### Business Value

Regression triage provides:
- Structured investigation
- Root cause identification
- Resolution tracking
- Knowledge preservation
- Quality gate enforcement

### Scope Boundaries

This task covers the triage workflow. Change logging is Task 048.b. Baseline recording is Task 048.a. Baseline management is Task 048.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Baseline | Task 048 | Comparison | Source |
| Change Log | Task 048.b | Correlation | Analysis |
| Recording | Task 048.a | Resolution | Update |
| Scoring | Task 047 | Thresholds | Trigger |
| History | Task 047.c | Trends | Context |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| No baseline | Check | Create first | Cannot triage |
| Missing changes | Query fail | Manual entry | Incomplete |
| Resolution fail | Retry fail | Manual close | Blocked |
| State corruption | Parse error | Reset | Lost progress |

### Assumptions

1. **Baseline exists**: For comparison
2. **Changes tracked**: Via Task 048.b
3. **Results available**: From benchmark runs
4. **User action**: For resolution
5. **Storage writable**: For state tracking

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Regression | Performance/quality degradation |
| Triage | Investigation and categorization |
| Root cause | Underlying reason for regression |
| Correlation | Linking regression to change |
| Resolution | Fixing or accepting regression |
| Closure | Completing triage process |
| Flaky | Intermittent failure |
| Expected | Intentional change |
| True regression | Unintended degradation |
| Investigation | Analysis process |
| Diagnostics | Automated analysis |

---

## Out of Scope

- Automatic fixes
- Code changes
- PR integration
- External issue trackers
- Team notifications
- Approval workflows

---

## Functional Requirements

### FR-001 to FR-025: Detection

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048c-01 | Detection MUST be automatic | P0 |
| FR-048c-02 | Trigger on baseline comparison | P0 |
| FR-048c-03 | Trigger on gating failure | P0 |
| FR-048c-04 | Pass rate regression MUST detect | P0 |
| FR-048c-05 | Runtime regression MUST detect | P0 |
| FR-048c-06 | Score regression MUST detect | P0 |
| FR-048c-07 | Per-task regression MUST detect | P0 |
| FR-048c-08 | Threshold MUST be configurable | P0 |
| FR-048c-09 | Default pass rate delta: 5% | P0 |
| FR-048c-10 | Default runtime delta: 20% | P0 |
| FR-048c-11 | Regression ID MUST be generated | P0 |
| FR-048c-12 | ID format: reg-{date}-{seq} | P0 |
| FR-048c-13 | Severity MUST be assigned | P0 |
| FR-048c-14 | Severity: critical (>20% drop) | P0 |
| FR-048c-15 | Severity: major (10-20% drop) | P0 |
| FR-048c-16 | Severity: minor (5-10% drop) | P0 |
| FR-048c-17 | Affected tasks MUST list | P0 |
| FR-048c-18 | Delta MUST calculate | P0 |
| FR-048c-19 | Comparison baseline MUST record | P0 |
| FR-048c-20 | Current run MUST record | P0 |
| FR-048c-21 | Timestamp MUST record | P0 |
| FR-048c-22 | Status MUST be set | P0 |
| FR-048c-23 | Initial status: open | P0 |
| FR-048c-24 | Notification MUST be shown | P0 |
| FR-048c-25 | CLI output MUST indicate | P0 |

### FR-026 to FR-045: Investigation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048c-26 | Investigation MUST be supported | P0 |
| FR-048c-27 | Auto-diagnostics MUST run | P0 |
| FR-048c-28 | Change correlation MUST occur | P0 |
| FR-048c-29 | Query changes since baseline | P0 |
| FR-048c-30 | Link changes to regression | P0 |
| FR-048c-31 | Per-task comparison MUST show | P0 |
| FR-048c-32 | Before/after metrics MUST show | P0 |
| FR-048c-33 | Trend analysis MUST allow | P1 |
| FR-048c-34 | Historical context MUST show | P0 |
| FR-048c-35 | Flakiness check MUST run | P0 |
| FR-048c-36 | Previous failures MUST query | P0 |
| FR-048c-37 | Similar regressions MUST query | P1 |
| FR-048c-38 | Affected prompts MUST list | P0 |
| FR-048c-39 | Affected configs MUST list | P0 |
| FR-048c-40 | Model changes MUST list | P0 |
| FR-048c-41 | Investigation notes MUST allow | P0 |
| FR-048c-42 | Notes MUST be timestamped | P0 |
| FR-048c-43 | Notes MUST have author | P0 |
| FR-048c-44 | Attachments MUST allow | P1 |
| FR-048c-45 | Investigation report MUST generate | P0 |

### FR-046 to FR-060: Categorization

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048c-46 | Categorization MUST be required | P0 |
| FR-048c-47 | Category: true_regression | P0 |
| FR-048c-48 | Category: flaky_test | P0 |
| FR-048c-49 | Category: expected_change | P0 |
| FR-048c-50 | Category: environment_issue | P0 |
| FR-048c-51 | Category: data_issue | P0 |
| FR-048c-52 | Category: timeout_issue | P0 |
| FR-048c-53 | Category MUST have reason | P0 |
| FR-048c-54 | Category MUST be audited | P0 |
| FR-048c-55 | Re-categorization MUST allow | P0 |
| FR-048c-56 | Category history MUST track | P0 |
| FR-048c-57 | Confidence MUST be optional | P1 |
| FR-048c-58 | Evidence MUST link | P0 |
| FR-048c-59 | Root cause MUST describe | P0 |
| FR-048c-60 | Blame assignment MUST allow | P1 |

### FR-061 to FR-080: Resolution

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048c-61 | Resolution MUST be tracked | P0 |
| FR-048c-62 | Resolution: fixed | P0 |
| FR-048c-63 | Resolution: accepted | P0 |
| FR-048c-64 | Resolution: deferred | P0 |
| FR-048c-65 | Resolution: wont_fix | P0 |
| FR-048c-66 | Resolution: duplicate | P0 |
| FR-048c-67 | Fixed MUST require verification | P0 |
| FR-048c-68 | Verification run MUST link | P0 |
| FR-048c-69 | Verification pass MUST confirm | P0 |
| FR-048c-70 | Accepted MUST require reason | P0 |
| FR-048c-71 | Accepted MUST update baseline | P0 |
| FR-048c-72 | Deferred MUST set reminder | P1 |
| FR-048c-73 | Wont_fix MUST justify | P0 |
| FR-048c-74 | Duplicate MUST link original | P0 |
| FR-048c-75 | Resolution timestamp MUST record | P0 |
| FR-048c-76 | Resolution author MUST record | P0 |
| FR-048c-77 | Resolution notes MUST allow | P0 |
| FR-048c-78 | Reopen MUST be allowed | P0 |
| FR-048c-79 | Reopen reason MUST record | P0 |
| FR-048c-80 | State transitions MUST audit | P0 |

### FR-081 to FR-095: Reporting

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048c-81 | List open regressions MUST work | P0 |
| FR-048c-82 | List by severity MUST work | P0 |
| FR-048c-83 | List by category MUST work | P0 |
| FR-048c-84 | List by age MUST work | P0 |
| FR-048c-85 | Summary report MUST generate | P0 |
| FR-048c-86 | Detail report MUST generate | P0 |
| FR-048c-87 | Timeline MUST show | P0 |
| FR-048c-88 | Resolution rate MUST show | P1 |
| FR-048c-89 | Mean time to resolve MUST show | P1 |
| FR-048c-90 | Export MUST work | P0 |
| FR-048c-91 | JSON format MUST work | P0 |
| FR-048c-92 | Markdown format MUST work | P0 |
| FR-048c-93 | Text format MUST work | P0 |
| FR-048c-94 | Historical reports MUST query | P0 |
| FR-048c-95 | Trend reports MUST generate | P1 |

### FR-096 to FR-105: CLI

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048c-96 | CLI commands MUST exist | P0 |
| FR-048c-97 | `acode triage list` MUST work | P0 |
| FR-048c-98 | `acode triage show <id>` MUST work | P0 |
| FR-048c-99 | `acode triage investigate <id>` MUST work | P0 |
| FR-048c-100 | `acode triage categorize <id>` MUST work | P0 |
| FR-048c-101 | `acode triage resolve <id>` MUST work | P0 |
| FR-048c-102 | `acode triage note <id>` MUST work | P0 |
| FR-048c-103 | `acode triage report` MUST work | P0 |
| FR-048c-104 | `--severity` filter MUST work | P0 |
| FR-048c-105 | `--status` filter MUST work | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-048c-01 | Detection | <200ms | P0 |
| NFR-048c-02 | Diagnostics | <1s | P0 |
| NFR-048c-03 | Change correlation | <500ms | P0 |
| NFR-048c-04 | List query | <200ms | P0 |
| NFR-048c-05 | Detail query | <100ms | P0 |
| NFR-048c-06 | Report generation | <500ms | P0 |
| NFR-048c-07 | State update | <100ms | P0 |
| NFR-048c-08 | Memory usage | <50MB | P0 |
| NFR-048c-09 | Storage per regression | <100KB | P0 |
| NFR-048c-10 | Concurrent queries | 10 | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-048c-11 | Detection accuracy | 100% | P0 |
| NFR-048c-12 | State integrity | 100% | P0 |
| NFR-048c-13 | No data loss | Guaranteed | P0 |
| NFR-048c-14 | Atomic updates | Always | P0 |
| NFR-048c-15 | Cross-platform | All OS | P0 |
| NFR-048c-16 | Concurrent access | Safe | P0 |
| NFR-048c-17 | Backup | Automatic | P0 |
| NFR-048c-18 | Recovery | Graceful | P0 |
| NFR-048c-19 | Correlation accuracy | 100% | P0 |
| NFR-048c-20 | Audit completeness | 100% | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-048c-21 | Detection logged | Info | P0 |
| NFR-048c-22 | State changes logged | Info | P0 |
| NFR-048c-23 | Resolution logged | Info | P0 |
| NFR-048c-24 | Errors logged | Error | P0 |
| NFR-048c-25 | Structured logging | JSON | P0 |
| NFR-048c-26 | Metrics: open count | Gauge | P1 |
| NFR-048c-27 | Metrics: by severity | Histogram | P1 |
| NFR-048c-28 | Trace ID | Included | P1 |
| NFR-048c-29 | Audit events | Complete | P0 |
| NFR-048c-30 | User actions logged | Always | P0 |

---

## Acceptance Criteria / Definition of Done

### Detection
- [ ] AC-001: Automatic detection
- [ ] AC-002: Pass rate regression
- [ ] AC-003: Runtime regression
- [ ] AC-004: Score regression
- [ ] AC-005: Per-task regression
- [ ] AC-006: Threshold configurable
- [ ] AC-007: Severity assignment
- [ ] AC-008: Notification

### Investigation
- [ ] AC-009: Auto-diagnostics
- [ ] AC-010: Change correlation
- [ ] AC-011: Before/after metrics
- [ ] AC-012: Historical context
- [ ] AC-013: Flakiness check
- [ ] AC-014: Notes support
- [ ] AC-015: Report generation

### Categorization
- [ ] AC-016: All categories
- [ ] AC-017: Reason required
- [ ] AC-018: Re-categorization
- [ ] AC-019: History tracking
- [ ] AC-020: Evidence linking
- [ ] AC-021: Root cause

### Resolution
- [ ] AC-022: All resolutions
- [ ] AC-023: Fixed verification
- [ ] AC-024: Accepted baseline update
- [ ] AC-025: Reopen support
- [ ] AC-026: State auditing
- [ ] AC-027: Resolution notes

### Reporting
- [ ] AC-028: List queries
- [ ] AC-029: Filter support
- [ ] AC-030: Summary report
- [ ] AC-031: Detail report
- [ ] AC-032: Export formats
- [ ] AC-033: Historical

### CLI
- [ ] AC-034: All commands
- [ ] AC-035: Filters work
- [ ] AC-036: Interactive mode
- [ ] AC-037: Tests pass
- [ ] AC-038: Documented
- [ ] AC-039: Cross-platform
- [ ] AC-040: Reviewed

---

## User Verification Scenarios

### Scenario 1: Regression Detected
**Persona:** Developer  
**Preconditions:** Run below baseline  
**Steps:**
1. Run benchmark
2. Regression detected
3. Notification shown
4. Triage opened

**Verification Checklist:**
- [ ] Detection works
- [ ] Severity assigned
- [ ] Tasks listed
- [ ] Delta shown

### Scenario 2: Investigate Regression
**Persona:** Developer  
**Preconditions:** Open regression exists  
**Steps:**
1. Run investigate command
2. Diagnostics run
3. Changes shown
4. Report generated

**Verification Checklist:**
- [ ] Diagnostics complete
- [ ] Changes correlated
- [ ] Before/after shown
- [ ] Report available

### Scenario 3: Categorize as Flaky
**Persona:** Developer  
**Preconditions:** Investigation complete  
**Steps:**
1. Run categorize command
2. Select flaky_test
3. Provide reason
4. Category saved

**Verification Checklist:**
- [ ] Category set
- [ ] Reason saved
- [ ] History updated
- [ ] Audit logged

### Scenario 4: Resolve as Fixed
**Persona:** Developer  
**Preconditions:** Fix deployed  
**Steps:**
1. Run new benchmark
2. Run resolve command
3. Link verification run
4. Confirm fixed

**Verification Checklist:**
- [ ] Resolution set
- [ ] Verification linked
- [ ] Pass confirmed
- [ ] Closed

### Scenario 5: Accept Regression
**Persona:** Tech Lead  
**Preconditions:** Expected change  
**Steps:**
1. Run resolve command
2. Select accepted
3. Provide justification
4. Baseline updated

**Verification Checklist:**
- [ ] Accepted
- [ ] Reason saved
- [ ] Baseline updated
- [ ] Audit logged

### Scenario 6: Generate Summary Report
**Persona:** Manager  
**Preconditions:** Regressions exist  
**Steps:**
1. Run report command
2. View summary
3. Filter by severity
4. Export to markdown

**Verification Checklist:**
- [ ] Report generated
- [ ] Summary accurate
- [ ] Filter works
- [ ] Export works

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-048c-01 | Detection logic | FR-048c-01 |
| UT-048c-02 | Threshold calculation | FR-048c-08 |
| UT-048c-03 | Severity assignment | FR-048c-13 |
| UT-048c-04 | ID generation | FR-048c-11 |
| UT-048c-05 | State transitions | FR-048c-80 |
| UT-048c-06 | Categorization | FR-048c-46 |
| UT-048c-07 | Resolution validation | FR-048c-61 |
| UT-048c-08 | Change correlation | FR-048c-28 |
| UT-048c-09 | Note handling | FR-048c-41 |
| UT-048c-10 | Report generation | FR-048c-85 |
| UT-048c-11 | Filter logic | FR-048c-82 |
| UT-048c-12 | Export formatting | FR-048c-91 |
| UT-048c-13 | Reopen logic | FR-048c-78 |
| UT-048c-14 | Verification check | FR-048c-67 |
| UT-048c-15 | Timeline building | FR-048c-87 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-048c-01 | Full workflow E2E | E2E |
| IT-048c-02 | Baseline integration | Task 048 |
| IT-048c-03 | Change log integration | Task 048.b |
| IT-048c-04 | Recording integration | Task 048.a |
| IT-048c-05 | CLI integration | FR-048c-96 |
| IT-048c-06 | Cross-platform | NFR-048c-15 |
| IT-048c-07 | Concurrent access | NFR-048c-16 |
| IT-048c-08 | Logging | NFR-048c-21 |
| IT-048c-09 | Large history | NFR-048c-09 |
| IT-048c-10 | Report export | FR-048c-90 |
| IT-048c-11 | State recovery | NFR-048c-18 |
| IT-048c-12 | Audit trail | NFR-048c-29 |
| IT-048c-13 | Baseline update | FR-048c-71 |
| IT-048c-14 | Verification flow | FR-048c-68 |
| IT-048c-15 | Query performance | NFR-048c-04 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Triage/
│       ├── Regression.cs
│       ├── RegressionStatus.cs
│       ├── RegressionCategory.cs
│       ├── Resolution.cs
│       ├── InvestigationNote.cs
│       └── TriageTimeline.cs
├── Acode.Application/
│   └── Triage/
│       ├── IRegressionDetector.cs
│       ├── ITriageService.cs
│       ├── IInvestigator.cs
│       ├── TriageConfig.cs
│       └── DiagnosticsRunner.cs
├── Acode.Infrastructure/
│   └── Triage/
│       ├── RegressionDetector.cs
│       ├── TriageService.cs
│       ├── TriageStore.cs
│       ├── ChangeCorrelator.cs
│       └── TriageReporter.cs
├── Acode.Cli/
│   └── Commands/
│       └── Triage/
│           ├── TriageListCommand.cs
│           ├── TriageShowCommand.cs
│           ├── TriageInvestigateCommand.cs
│           ├── TriageCategorizeCommand.cs
│           ├── TriageResolveCommand.cs
│           └── TriageReportCommand.cs
```

### Regression Record Schema

```json
{
  "id": "reg-2025-01-15-001",
  "detectedAt": "2025-01-15T10:30:00Z",
  "status": "open",
  "severity": "major",
  "type": "pass_rate",
  "delta": {
    "baseline": 95.0,
    "current": 82.0,
    "change": -13.0
  },
  "affectedTasks": ["task-001", "task-005", "task-012"],
  "baselineId": "baseline-2025-01-01-001",
  "runId": "run-2025-01-15-001",
  "correlatedChanges": ["change-2025-01-14-001", "change-2025-01-15-001"],
  "category": null,
  "resolution": null,
  "notes": [],
  "timeline": []
}
```

### State Machine

```
                    ┌─────────────────────────────────────────┐
                    │                                         │
                    v                                         │
[OPEN] ──> [INVESTIGATING] ──> [CATEGORIZED] ──> [RESOLVED] ──┘
  │              │                   │               │ (reopen)
  │              │                   │               │
  └──────────────┴───────────────────┴───────────────┘
                     (can skip steps)
```

### CLI Examples

```bash
# List open regressions
acode triage list

# List by severity
acode triage list --severity critical

# Show regression details
acode triage show reg-2025-01-15-001

# Investigate (run diagnostics)
acode triage investigate reg-2025-01-15-001

# Add investigation note
acode triage note reg-2025-01-15-001 "Checked model logs, no errors"

# Categorize
acode triage categorize reg-2025-01-15-001 --category true_regression --reason "Prompt change broke edge case"

# Resolve as fixed
acode triage resolve reg-2025-01-15-001 --resolution fixed --verification run-2025-01-16-001

# Resolve as accepted
acode triage resolve reg-2025-01-15-001 --resolution accepted --reason "Expected after model upgrade" --update-baseline

# Generate report
acode triage report --format markdown --output triage-report.md
```

**End of Task 048.c Specification**
