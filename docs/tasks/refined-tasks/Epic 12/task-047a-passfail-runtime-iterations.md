# Task 047.a: Pass/Fail + Runtime + Iterations

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 12 – Hardening  
**Dependencies:** Task 047 (Scoring), Task 046.c (Results)  

---

## Description

Task 047.a implements the core metrics for scoring: pass/fail determination, runtime measurement, and iteration counting. These three metrics form the foundation of quality assessment—did the task succeed, how long did it take, and how many attempts were needed?

Pass/fail is the binary quality gate: the task either achieved its expected outcome or it didn't. Runtime measures efficiency: faster is better (within limits). Iterations count retries: fewer attempts indicates more reliable model behavior. Together, these metrics create a comprehensive quality picture.

### Business Value

Core metrics provide:
- Success measurement
- Efficiency tracking
- Reliability assessment
- Quality scoring inputs
- Trend visibility

### Scope Boundaries

This task covers pass/fail, runtime, and iterations. Threshold configuration is Task 047.b. Historical reports are Task 047.c. Scoring aggregation is Task 047.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Results | Task 046.c | Metric source | Input |
| Scoring | Task 047 | Aggregation | Consumer |
| Thresholds | Task 047.b | Comparison | Rules |
| Reports | Task 047.c | Visualization | Output |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Missing result | Check | Error | Cannot score |
| Invalid runtime | Validation | Default | Inaccurate |
| Overflow | Range check | Cap | Data loss |
| Clock skew | Validation | Warn | Inaccurate |

### Assumptions

1. **Results complete**: From Task 046.c
2. **Timing accurate**: Millisecond precision
3. **Iteration tracked**: By runner
4. **Status definitive**: Clear outcome
5. **Metrics independent**: No coupling

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Pass | Task succeeded |
| Fail | Task did not succeed |
| Runtime | Execution duration |
| Iteration | Single attempt |
| Retry | Repeated attempt |
| Metric | Measured value |
| Aggregate | Combined metric |
| Rate | Percentage metric |
| Latency | Time measurement |
| Attempt | Iteration synonym |

---

## Out of Scope

- Threshold configuration (Task 047.b)
- Historical comparison (Task 047.c)
- Score aggregation (Task 047)
- Token metrics (separate)
- Memory metrics (separate)
- Custom metrics

---

## Functional Requirements

### FR-001 to FR-025: Pass/Fail Metrics

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-047a-01 | Pass/fail MUST be determined | P0 |
| FR-047a-02 | Status from result MUST map | P0 |
| FR-047a-03 | passed → Pass | P0 |
| FR-047a-04 | failed → Fail | P0 |
| FR-047a-05 | timeout → Fail | P0 |
| FR-047a-06 | error → Fail | P0 |
| FR-047a-07 | skipped → Excluded | P0 |
| FR-047a-08 | Pass count MUST calculate | P0 |
| FR-047a-09 | Fail count MUST calculate | P0 |
| FR-047a-10 | Total count MUST calculate | P0 |
| FR-047a-11 | Pass rate MUST calculate | P0 |
| FR-047a-12 | Pass rate = pass / total | P0 |
| FR-047a-13 | Pass rate as percentage | P0 |
| FR-047a-14 | Pass rate range: 0-100 | P0 |
| FR-047a-15 | Zero total = undefined | P0 |
| FR-047a-16 | By-category pass rate MUST work | P0 |
| FR-047a-17 | By-difficulty pass rate MAY work | P2 |
| FR-047a-18 | Critical tasks MUST track | P0 |
| FR-047a-19 | Critical pass rate MUST exist | P0 |
| FR-047a-20 | Critical fail = gate fail | P0 |
| FR-047a-21 | Partial pass MAY exist | P2 |
| FR-047a-22 | Partial = weighted pass | P2 |
| FR-047a-23 | All pass MUST be flag | P0 |
| FR-047a-24 | Any fail MUST be flag | P0 |
| FR-047a-25 | Fail list MUST be available | P0 |

### FR-026 to FR-045: Runtime Metrics

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-047a-26 | Runtime MUST be captured | P0 |
| FR-047a-27 | Runtime in milliseconds | P0 |
| FR-047a-28 | Per-task runtime MUST exist | P0 |
| FR-047a-29 | Total runtime MUST calculate | P0 |
| FR-047a-30 | Average runtime MUST calculate | P0 |
| FR-047a-31 | Median runtime MUST calculate | P0 |
| FR-047a-32 | P95 runtime MAY calculate | P1 |
| FR-047a-33 | P99 runtime MAY calculate | P1 |
| FR-047a-34 | Min runtime MUST calculate | P0 |
| FR-047a-35 | Max runtime MUST calculate | P0 |
| FR-047a-36 | Std dev MAY calculate | P1 |
| FR-047a-37 | By-category runtime MUST work | P0 |
| FR-047a-38 | Slowest tasks MUST list | P0 |
| FR-047a-39 | Fastest tasks MAY list | P2 |
| FR-047a-40 | Runtime score MUST calculate | P0 |
| FR-047a-41 | Faster = higher score | P0 |
| FR-047a-42 | Reference runtime MUST exist | P0 |
| FR-047a-43 | Score = reference / actual | P0 |
| FR-047a-44 | Score cap at 1.0 | P0 |
| FR-047a-45 | Timeout = 0.0 score | P0 |

### FR-047a-46 to FR-060: Iteration Metrics

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-047a-46 | Iterations MUST be captured | P0 |
| FR-047a-47 | Per-task iterations MUST exist | P0 |
| FR-047a-48 | Total iterations MUST calculate | P0 |
| FR-047a-49 | Average iterations MUST calculate | P0 |
| FR-047a-50 | First-attempt pass MUST track | P0 |
| FR-047a-51 | First-attempt rate MUST calculate | P0 |
| FR-047a-52 | Multi-attempt tasks MUST list | P0 |
| FR-047a-53 | Max iterations MUST track | P0 |
| FR-047a-54 | Iteration score MUST calculate | P0 |
| FR-047a-55 | Fewer = higher score | P0 |
| FR-047a-56 | 1 iteration = 1.0 score | P0 |
| FR-047a-57 | Score = 1 / iterations | P0 |
| FR-047a-58 | Score min at 0.0 | P0 |
| FR-047a-59 | By-category iterations MUST work | P0 |
| FR-047a-60 | Retry patterns MAY analyze | P2 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-047a-01 | Pass/fail calc | <10ms | P0 |
| NFR-047a-02 | Runtime calc | <10ms | P0 |
| NFR-047a-03 | Iteration calc | <5ms | P0 |
| NFR-047a-04 | Stats calc | <50ms | P0 |
| NFR-047a-05 | 1000 tasks | <100ms | P0 |
| NFR-047a-06 | Memory usage | <10MB | P0 |
| NFR-047a-07 | Percentile calc | <20ms | P0 |
| NFR-047a-08 | Score calc | <10ms | P0 |
| NFR-047a-09 | By-category | <20ms | P0 |
| NFR-047a-10 | List generation | <10ms | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-047a-11 | Calculation accuracy | 100% | P0 |
| NFR-047a-12 | Determinism | 100% | P0 |
| NFR-047a-13 | Overflow handling | Always | P0 |
| NFR-047a-14 | Division by zero | Handled | P0 |
| NFR-047a-15 | Missing data | Handled | P0 |
| NFR-047a-16 | Cross-platform | All OS | P0 |
| NFR-047a-17 | Floating point | Consistent | P0 |
| NFR-047a-18 | Edge cases | Tested | P0 |
| NFR-047a-19 | Invalid input | Rejected | P0 |
| NFR-047a-20 | Precision | 2 decimal | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-047a-21 | Calc logged | Debug | P0 |
| NFR-047a-22 | Results logged | Info | P0 |
| NFR-047a-23 | Errors logged | Error | P0 |
| NFR-047a-24 | Metrics: pass rate | Gauge | P0 |
| NFR-047a-25 | Metrics: runtime | Histogram | P0 |
| NFR-047a-26 | Metrics: iterations | Histogram | P0 |
| NFR-047a-27 | Structured logging | JSON | P0 |
| NFR-047a-28 | Component scores | Logged | P0 |
| NFR-047a-29 | Breakdown | Available | P0 |
| NFR-047a-30 | Trace ID | Included | P1 |

---

## Acceptance Criteria / Definition of Done

### Pass/Fail
- [ ] AC-001: Status mapped
- [ ] AC-002: Pass count
- [ ] AC-003: Fail count
- [ ] AC-004: Pass rate
- [ ] AC-005: By-category
- [ ] AC-006: Critical track
- [ ] AC-007: Fail list
- [ ] AC-008: All pass flag

### Runtime
- [ ] AC-009: Per-task
- [ ] AC-010: Total
- [ ] AC-011: Average
- [ ] AC-012: Median
- [ ] AC-013: Percentiles
- [ ] AC-014: By-category
- [ ] AC-015: Slowest list
- [ ] AC-016: Score calc

### Iterations
- [ ] AC-017: Per-task
- [ ] AC-018: Total
- [ ] AC-019: Average
- [ ] AC-020: First-attempt
- [ ] AC-021: Multi-attempt
- [ ] AC-022: Score calc
- [ ] AC-023: By-category
- [ ] AC-024: Max track

### Quality
- [ ] AC-025: Accurate
- [ ] AC-026: Deterministic
- [ ] AC-027: Cross-platform
- [ ] AC-028: Edge cases
- [ ] AC-029: Logged
- [ ] AC-030: Tests pass
- [ ] AC-031: Documented
- [ ] AC-032: Reviewed

---

## User Verification Scenarios

### Scenario 1: View Pass Rate
**Persona:** Developer  
**Preconditions:** Run complete  
**Steps:**
1. Run benchmarks
2. View summary
3. See pass rate
4. Understand quality

**Verification Checklist:**
- [ ] Pass rate shown
- [ ] Percentage format
- [ ] Accurate count
- [ ] Clear display

### Scenario 2: Analyze Runtime
**Persona:** Developer  
**Preconditions:** Run complete  
**Steps:**
1. View runtime stats
2. See average
3. See slowest
4. Identify issues

**Verification Checklist:**
- [ ] Stats shown
- [ ] Average correct
- [ ] Slowest listed
- [ ] Actionable

### Scenario 3: Track Iterations
**Persona:** Developer  
**Preconditions:** Retries occurred  
**Steps:**
1. View iteration stats
2. See first-attempt rate
3. See multi-attempt tasks
4. Understand reliability

**Verification Checklist:**
- [ ] Iterations shown
- [ ] First-attempt rate
- [ ] Multi-attempt list
- [ ] Reliability clear

### Scenario 4: Category Breakdown
**Persona:** Developer  
**Preconditions:** Multiple categories  
**Steps:**
1. View by-category
2. See pass rates
3. See runtimes
4. Compare

**Verification Checklist:**
- [ ] Categories shown
- [ ] Metrics per category
- [ ] Comparison possible
- [ ] Weak areas visible

### Scenario 5: Critical Tasks
**Persona:** Tech Lead  
**Preconditions:** Critical defined  
**Steps:**
1. Run benchmarks
2. Check critical tasks
3. See critical pass rate
4. Gate decision

**Verification Checklist:**
- [ ] Critical tracked
- [ ] Separate rate
- [ ] Gate impact
- [ ] Clear status

### Scenario 6: Score Calculation
**Persona:** Developer  
**Preconditions:** Run complete  
**Steps:**
1. View scores
2. See pass score
3. See runtime score
4. See iteration score

**Verification Checklist:**
- [ ] Scores shown
- [ ] Range 0-1
- [ ] Components visible
- [ ] Weights applied

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-047a-01 | Status mapping | FR-047a-02 |
| UT-047a-02 | Pass count | FR-047a-08 |
| UT-047a-03 | Fail count | FR-047a-09 |
| UT-047a-04 | Pass rate | FR-047a-11 |
| UT-047a-05 | Zero total | FR-047a-15 |
| UT-047a-06 | Total runtime | FR-047a-29 |
| UT-047a-07 | Average runtime | FR-047a-30 |
| UT-047a-08 | Median runtime | FR-047a-31 |
| UT-047a-09 | Percentiles | FR-047a-32 |
| UT-047a-10 | Runtime score | FR-047a-40 |
| UT-047a-11 | Iteration count | FR-047a-48 |
| UT-047a-12 | First-attempt | FR-047a-50 |
| UT-047a-13 | Iteration score | FR-047a-54 |
| UT-047a-14 | By-category | FR-047a-16 |
| UT-047a-15 | Critical tasks | FR-047a-18 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-047a-01 | Results integration | Task 046.c |
| IT-047a-02 | Scoring integration | Task 047 |
| IT-047a-03 | Full calculation | E2E |
| IT-047a-04 | Large suite | NFR-047a-05 |
| IT-047a-05 | Cross-platform | NFR-047a-16 |
| IT-047a-06 | Edge cases | NFR-047a-18 |
| IT-047a-07 | Precision | NFR-047a-20 |
| IT-047a-08 | Logging | NFR-047a-21 |
| IT-047a-09 | Breakdown | FR-047a-25 |
| IT-047a-10 | Score components | All |
| IT-047a-11 | Timeout handling | FR-047a-45 |
| IT-047a-12 | Slowest list | FR-047a-38 |
| IT-047a-13 | Multi-attempt | FR-047a-52 |
| IT-047a-14 | Category stats | FR-047a-37 |
| IT-047a-15 | Critical gate | FR-047a-20 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Gates/
│       └── Metrics/
│           ├── PassFailMetrics.cs
│           ├── RuntimeMetrics.cs
│           ├── IterationMetrics.cs
│           └── MetricScore.cs
├── Acode.Application/
│   └── Gates/
│       ├── IMetricsCalculator.cs
│       └── MetricsOptions.cs
├── Acode.Infrastructure/
│   └── Gates/
│       ├── PassFailCalculator.cs
│       ├── RuntimeCalculator.cs
│       ├── IterationCalculator.cs
│       └── StatisticsHelper.cs
```

### Metrics Output

```json
{
  "passFail": {
    "total": 50,
    "passed": 42,
    "failed": 8,
    "passRate": 84.0,
    "allPassed": false,
    "anyFailed": true,
    "criticalPassRate": 100.0,
    "failedTasks": ["BENCH-015", "BENCH-023"],
    "byCategory": {
      "file-ops": { "passed": 12, "failed": 1, "rate": 92.3 },
      "code-gen": { "passed": 15, "failed": 3, "rate": 83.3 }
    },
    "score": 0.84
  },
  "runtime": {
    "totalMs": 125000,
    "averageMs": 2500,
    "medianMs": 2100,
    "minMs": 450,
    "maxMs": 15000,
    "p95Ms": 8500,
    "slowestTasks": [
      { "id": "BENCH-042", "runtimeMs": 15000 }
    ],
    "byCategory": {
      "file-ops": { "averageMs": 1200 },
      "code-gen": { "averageMs": 4500 }
    },
    "score": 0.78
  },
  "iterations": {
    "total": 58,
    "average": 1.16,
    "firstAttemptCount": 44,
    "firstAttemptRate": 88.0,
    "multiAttemptTasks": [
      { "id": "BENCH-015", "iterations": 3 }
    ],
    "maxIterations": 3,
    "score": 0.86
  }
}
```

**End of Task 047.a Specification**
