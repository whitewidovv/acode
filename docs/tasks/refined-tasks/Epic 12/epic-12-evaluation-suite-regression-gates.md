# EPIC 12 — Evaluation Suite + Regression Gates

**Priority:** P0 – Critical  
**Phase:** Phase 12 – Hardening  
**Dependencies:** Epic 11 (Performance), Epic 10 (Reliability), Epic 07 (CLI)  

---

## Epic Overview

Epic 12 establishes the evaluation suite and regression gates—the quality assurance framework that ensures Acode doesn't degrade over time. Without systematic evaluation, each change risks introducing subtle regressions that erode user trust and system reliability.

This epic creates: (1) a benchmark task suite that captures real coding scenarios as repeatable test cases, (2) a scoring and gating system that defines pass/fail criteria and blocks regressions from shipping, and (3) a golden baseline maintenance system that tracks historical performance and manages prompt/model upgrades.

### Purpose

Evaluation and regression gates provide:
- Objective quality measurement
- Automated regression prevention
- Upgrade confidence
- Historical tracking
- Continuous improvement

### Boundaries

This epic focuses on evaluation infrastructure. Performance measurement is Epic 11. Reliability is Epic 10. The CLI framework is Epic 07. This epic builds the gates on top of those foundations.

### Cross-Cutting Concerns

| Concern | Resolution |
|---------|------------|
| Performance | Benchmark suite integrates with Task 045 harness |
| Reliability | Evaluation uses crash-safe logging from Task 040 |
| CLI | Runner CLI follows Task 030 patterns |
| Configuration | Gates configured via Task 002 contract |
| Mode compliance | All evaluation respects Task 001 modes |

---

## Outcomes

| ID | Outcome | Measurable Result |
|----|---------|-------------------|
| O-01 | Tasks stored as specs | 100% of benchmark tasks in declarative format |
| O-02 | Runner executes tasks | All tasks runnable via CLI |
| O-03 | Results in JSON | Structured, parseable output |
| O-04 | Pass/fail scoring | Clear binary outcome |
| O-05 | Iteration metrics | Runtime and attempt counts |
| O-06 | Thresholds defined | Configurable gating rules |
| O-07 | Historical tracking | Diffable reports over time |
| O-08 | Golden baselines | Reference runs recorded |
| O-09 | Change tracking | Prompt/model upgrades logged |
| O-10 | Triage workflow | Regression investigation process |
| O-11 | Blocking gates | Regressions block shipping |
| O-12 | Override mechanism | Approved bypass for exceptions |
| O-13 | Audit trail | All gate decisions logged |
| O-14 | CI integration | Gates in pipeline |
| O-15 | Local gates | Developer pre-commit checks |

---

## Non-Goals

| ID | Non-Goal | Rationale |
|----|----------|-----------|
| NG-01 | Production monitoring | Out of scope—this is dev/test |
| NG-02 | External CI systems | Focus on local evaluation |
| NG-03 | ML-based evaluation | Static rules, not learned |
| NG-04 | Human evaluation | Automated only |
| NG-05 | Subjective quality | Objective metrics only |
| NG-06 | A/B testing | Single evaluation path |
| NG-07 | Multi-tenant evaluation | Single user focus |
| NG-08 | Cloud-based baselines | Local storage only |
| NG-09 | Real-time dashboards | Batch evaluation |
| NG-10 | Custom evaluation metrics | Standard metrics only |
| NG-11 | Cross-version comparison | Same version evaluation |
| NG-12 | Performance optimization | Measure, not optimize |

---

## Architecture & Integration Points

### Component Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Evaluation Suite                         │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │  Task Specs     │  │  Runner CLI     │  │  Results    │  │
│  │  (046.a)        │  │  (046.b)        │  │  (046.c)    │  │
│  └────────┬────────┘  └────────┬────────┘  └──────┬──────┘  │
│           │                    │                   │         │
│  ┌────────▼────────────────────▼───────────────────▼──────┐  │
│  │                 Benchmark Task Suite (046)              │  │
│  └─────────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │  Pass/Fail      │  │  Thresholds     │  │  Reports    │  │
│  │  (047.a)        │  │  (047.b)        │  │  (047.c)    │  │
│  └────────┬────────┘  └────────┬────────┘  └──────┬──────┘  │
│           │                    │                   │         │
│  ┌────────▼────────────────────▼───────────────────▼──────┐  │
│  │              Scoring + Promotion Gates (047)            │  │
│  └─────────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │  Baseline Runs  │  │  Change Log     │  │  Triage     │  │
│  │  (048.a)        │  │  (048.b)        │  │  (048.c)    │  │
│  └────────┬────────┘  └────────┬────────┘  └──────┬──────┘  │
│           │                    │                   │         │
│  ┌────────▼────────────────────▼───────────────────▼──────┐  │
│  │            Golden Baseline Maintenance (048)            │  │
│  └─────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Domain Interfaces

```csharp
// Task specs
interface IBenchmarkTaskSpec {
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string Category { get; }
    TaskInput Input { get; }
    ExpectedOutput Expected { get; }
    TimeSpan Timeout { get; }
}

// Runner
interface IEvaluationRunner {
    Task<EvaluationResult> RunAsync(IBenchmarkTaskSpec spec, CancellationToken ct);
    Task<EvaluationBatch> RunBatchAsync(IEnumerable<IBenchmarkTaskSpec> specs, CancellationToken ct);
}

// Scoring
interface IScorer {
    ScoreResult Score(EvaluationResult result, IBenchmarkTaskSpec spec);
    bool PassesGate(ScoreResult score, GatingRules rules);
}

// Baseline
interface IBaselineManager {
    Task<Baseline> GetCurrentAsync();
    Task SetCurrentAsync(Baseline baseline);
    Task<bool> IsRegressionAsync(EvaluationBatch current, Baseline baseline);
}
```

### Event Contracts

| Event | Producer | Consumer | Payload |
|-------|----------|----------|---------|
| EvaluationStarted | Runner | Logger | TaskId, Timestamp |
| EvaluationCompleted | Runner | Scorer | TaskId, Result |
| GatePassed | Scorer | Pipeline | Score, Threshold |
| GateFailed | Scorer | Pipeline | Score, Threshold |
| RegressionDetected | Baseline | Triage | Delta, Severity |
| BaselineUpdated | Manager | Logger | OldBaseline, NewBaseline |

### Data Contracts

```json
// Task spec format
{
  "id": "BENCH-001",
  "name": "Simple file read",
  "category": "file-ops",
  "input": {
    "prompt": "Read the contents of README.md",
    "files": ["README.md"]
  },
  "expected": {
    "toolCall": "read_file",
    "outcome": "success"
  },
  "timeout": "PT30S"
}

// Evaluation result format
{
  "taskId": "BENCH-001",
  "status": "passed",
  "runtime": 1250,
  "iterations": 1,
  "score": 1.0,
  "details": {}
}

// Gate result format
{
  "passed": false,
  "score": 0.85,
  "threshold": 0.90,
  "regressions": ["BENCH-015", "BENCH-023"]
}
```

---

## Operational Considerations

### Mode Compliance

| Mode | Evaluation Impact |
|------|-------------------|
| Local-Only | All evaluation local |
| Burst | No impact |
| Airgapped | Evaluation works |

### Safety Considerations

| Concern | Mitigation |
|---------|------------|
| Benchmark task escape | Sandbox execution |
| Result tampering | Signed results |
| Baseline corruption | Backup on update |
| False positives | Configurable thresholds |
| False negatives | Multiple validation passes |

### Audit Requirements

| Event | Logged Data | Retention |
|-------|-------------|-----------|
| Evaluation run | TaskId, Result, Duration | 90 days |
| Gate decision | Pass/Fail, Score, Threshold | 90 days |
| Baseline change | Old, New, Reason | Permanent |
| Override | Who, Why, Approval | Permanent |

---

## Acceptance Criteria / Definition of Done

### Benchmark Task Suite (Task 046)
- [ ] AC-E12-001: Tasks defined as specs
- [ ] AC-E12-002: Spec format validated
- [ ] AC-E12-003: Categories supported
- [ ] AC-E12-004: Runner CLI works
- [ ] AC-E12-005: Single task execution
- [ ] AC-E12-006: Batch execution
- [ ] AC-E12-007: Results in JSON
- [ ] AC-E12-008: Results in text
- [ ] AC-E12-009: Timeout handling
- [ ] AC-E12-010: Error handling

### Scoring + Gates (Task 047)
- [ ] AC-E12-011: Pass/fail scoring
- [ ] AC-E12-012: Runtime tracked
- [ ] AC-E12-013: Iterations tracked
- [ ] AC-E12-014: Thresholds configurable
- [ ] AC-E12-015: Per-category thresholds
- [ ] AC-E12-016: Gating rules defined
- [ ] AC-E12-017: Historical reports
- [ ] AC-E12-018: Reports diffable
- [ ] AC-E12-019: Gate blocks on fail
- [ ] AC-E12-020: Override mechanism

### Baseline Maintenance (Task 048)
- [ ] AC-E12-021: Baselines recorded
- [ ] AC-E12-022: Baseline versioned
- [ ] AC-E12-023: Change log maintained
- [ ] AC-E12-024: Prompt changes tracked
- [ ] AC-E12-025: Model changes tracked
- [ ] AC-E12-026: Triage workflow defined
- [ ] AC-E12-027: Regression investigation
- [ ] AC-E12-028: Regression resolution
- [ ] AC-E12-029: Baseline update process
- [ ] AC-E12-030: Rollback supported

### Integration
- [ ] AC-E12-031: CLI commands work
- [ ] AC-E12-032: Exit codes correct
- [ ] AC-E12-033: Logging complete
- [ ] AC-E12-034: Metrics exported
- [ ] AC-E12-035: Cross-platform works
- [ ] AC-E12-036: Performance acceptable
- [ ] AC-E12-037: Documentation complete
- [ ] AC-E12-038: Tests pass
- [ ] AC-E12-039: Review completed
- [ ] AC-E12-040: Mode compliance verified

### Operational
- [ ] AC-E12-041: Sandbox isolation
- [ ] AC-E12-042: Result integrity
- [ ] AC-E12-043: Audit trail complete
- [ ] AC-E12-044: Override logged
- [ ] AC-E12-045: Backup on baseline change

---

## Risks & Mitigations

| ID | Risk | Probability | Impact | Mitigation |
|----|------|-------------|--------|------------|
| R-01 | Flaky benchmarks | High | High | Multiple runs, statistical analysis |
| R-02 | Threshold gaming | Medium | Medium | Multiple metrics, anti-gaming rules |
| R-03 | Baseline drift | Medium | High | Regular baseline refresh |
| R-04 | False regressions | High | Medium | Configurable thresholds, override |
| R-05 | Evaluation cost | Medium | Low | Efficient test selection |
| R-06 | Task obsolescence | Medium | Medium | Regular task review |
| R-07 | Environment variance | High | Medium | Environment capture, normalization |
| R-08 | Coverage gaps | Medium | High | Coverage metrics, gap analysis |
| R-09 | Slow feedback | Medium | Medium | Incremental evaluation, parallelism |
| R-10 | Override abuse | Low | High | Approval workflow, audit |
| R-11 | Baseline corruption | Low | High | Backups, validation |
| R-12 | Result tampering | Low | High | Signed results, audit |

---

## Milestone Plan

### Milestone 1: Benchmark Task Suite (Week 1)
- Task 046: Core benchmark suite
- Task 046.a: Task spec format
- Task 046.b: Runner CLI
- Task 046.c: JSON results

**Exit Criteria:**
- [ ] Tasks stored as specs
- [ ] Runner executes all tasks
- [ ] Results in JSON format

### Milestone 2: Scoring + Gates (Week 2)
- Task 047: Scoring framework
- Task 047.a: Pass/fail + metrics
- Task 047.b: Thresholds + rules
- Task 047.c: Historical reports

**Exit Criteria:**
- [ ] Scoring works
- [ ] Gates block regressions
- [ ] Reports are diffable

### Milestone 3: Baseline Maintenance (Week 3)
- Task 048: Baseline management
- Task 048.a: Baseline recording
- Task 048.b: Change log
- Task 048.c: Triage workflow

**Exit Criteria:**
- [ ] Baselines recorded
- [ ] Changes tracked
- [ ] Triage workflow complete

---

## Definition of Epic Complete

### Functionality
- [ ] DEC-01: All tasks stored as specs
- [ ] DEC-02: Runner CLI executes all tasks
- [ ] DEC-03: Results in JSON format
- [ ] DEC-04: Pass/fail scoring works
- [ ] DEC-05: Thresholds configurable
- [ ] DEC-06: Gates block regressions
- [ ] DEC-07: Historical reports diffable
- [ ] DEC-08: Baselines recorded
- [ ] DEC-09: Change log maintained
- [ ] DEC-10: Triage workflow defined

### Quality
- [ ] DEC-11: All tests pass
- [ ] DEC-12: Performance acceptable
- [ ] DEC-13: Cross-platform works
- [ ] DEC-14: No critical bugs

### Documentation
- [ ] DEC-15: API documented
- [ ] DEC-16: CLI documented
- [ ] DEC-17: Workflow documented
- [ ] DEC-18: Examples provided

### Operational
- [ ] DEC-19: Logging complete
- [ ] DEC-20: Metrics exported
- [ ] DEC-21: Audit trail works
- [ ] DEC-22: Override mechanism works
- [ ] DEC-23: Rollback supported

### Compliance
- [ ] DEC-24: Mode compliance verified
- [ ] DEC-25: Config contract followed
- [ ] DEC-26: Security reviewed

---

**END OF EPIC 12**
