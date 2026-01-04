# Task 047: Scoring + Promotion Gates

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 12 – Hardening  
**Dependencies:** Task 046 (Benchmark Suite), Task 046.c (Results)  

---

## Description

Task 047 implements scoring and promotion gates—the quality control system that determines whether a build can ship. Raw benchmark results (Task 046.c) become actionable decisions: pass the gate and proceed, or fail and block. This is the enforcement mechanism for quality standards.

The scoring system: (1) assigns scores to benchmark results using configurable weights, (2) applies pass/fail thresholds, (3) compares against baselines for regression detection, and (4) produces gate verdicts. Promotion gates are the quality checkpoints that prevent regressions from reaching users.

### Business Value

Scoring and gates provide:
- Regression prevention
- Quality enforcement
- Release confidence
- Automated decisions
- Consistent standards

### Scope Boundaries

This task establishes scoring framework. Pass/fail metrics are Task 047.a. Threshold rules are Task 047.b. Historical reports are Task 047.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Results | Task 046.c | Input | Source |
| Baseline | Task 048 | Comparison | Reference |
| CLI | Task 046.b | Commands | Interface |
| CI Pipeline | External | Exit codes | Integration |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Missing results | Check | Error | Cannot score |
| Invalid score | Validation | Error | Bad decision |
| Baseline missing | Check | Warn, proceed | No comparison |
| Threshold error | Validation | Default | May pass incorrectly |
| Gate crash | Exception | Fail closed | Block release |

### Assumptions

1. **Results exist**: From Task 046
2. **Thresholds defined**: Or defaults
3. **Baseline optional**: But recommended
4. **Fail closed**: On error, block
5. **Override exists**: For exceptions

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Score | Numeric quality measure |
| Weight | Score importance |
| Threshold | Pass/fail boundary |
| Gate | Quality checkpoint |
| Verdict | Gate decision |
| Regression | Quality decrease |
| Baseline | Reference standard |
| Override | Manual bypass |
| Promotion | Advance to next stage |
| Block | Prevent advancement |

---

## Out of Scope

- Result generation (Task 046)
- Baseline management (Task 048)
- Automated remediation
- External notifications
- Dashboard UI
- Trend prediction

---

## Functional Requirements

### FR-001 to FR-020: Scoring

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-047-01 | Scoring MUST exist | P0 |
| FR-047-02 | Score MUST be numeric | P0 |
| FR-047-03 | Score range: 0.0 to 1.0 | P0 |
| FR-047-04 | Pass rate MUST contribute | P0 |
| FR-047-05 | Runtime MUST contribute | P1 |
| FR-047-06 | Token usage MUST contribute | P1 |
| FR-047-07 | Weights MUST be configurable | P0 |
| FR-047-08 | Default weights MUST exist | P0 |
| FR-047-09 | Per-category weights MAY exist | P1 |
| FR-047-10 | Per-difficulty weights MAY exist | P2 |
| FR-047-11 | Critical tasks MUST have weight | P0 |
| FR-047-12 | Critical fail = overall fail | P0 |
| FR-047-13 | Score MUST be deterministic | P0 |
| FR-047-14 | Score calculation MUST log | P0 |
| FR-047-15 | Breakdown MUST be available | P0 |
| FR-047-16 | Component scores MUST show | P0 |
| FR-047-17 | Weighted sum MUST be used | P0 |
| FR-047-18 | Normalization MUST occur | P0 |
| FR-047-19 | Edge cases MUST handle | P0 |
| FR-047-20 | Zero tasks = undefined | P0 |

### FR-021 to FR-040: Thresholds

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-047-21 | Thresholds MUST exist | P0 |
| FR-047-22 | Pass threshold MUST be set | P0 |
| FR-047-23 | Default pass = 0.80 | P0 |
| FR-047-24 | Warn threshold MAY exist | P1 |
| FR-047-25 | Default warn = 0.90 | P1 |
| FR-047-26 | Per-metric thresholds MAY exist | P1 |
| FR-047-27 | Per-category thresholds MAY exist | P1 |
| FR-047-28 | Regression threshold MUST exist | P0 |
| FR-047-29 | Default regression = 5% drop | P0 |
| FR-047-30 | Absolute thresholds MUST work | P0 |
| FR-047-31 | Relative thresholds MUST work | P0 |
| FR-047-32 | Threshold source: config file | P0 |
| FR-047-33 | Threshold source: CLI override | P0 |
| FR-047-34 | Threshold source: environment | P1 |
| FR-047-35 | Precedence: CLI > env > config | P0 |
| FR-047-36 | Invalid threshold MUST error | P0 |
| FR-047-37 | Threshold validation MUST occur | P0 |
| FR-047-38 | Thresholds MUST be logged | P0 |
| FR-047-39 | Threshold changes MUST audit | P0 |
| FR-047-40 | Thresholds MUST be documented | P0 |

### FR-041 to FR-060: Gate Logic

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-047-41 | Gate MUST produce verdict | P0 |
| FR-047-42 | Verdict: pass | P0 |
| FR-047-43 | Verdict: fail | P0 |
| FR-047-44 | Verdict: warn | P1 |
| FR-047-45 | Pass = score >= threshold | P0 |
| FR-047-46 | Fail = score < threshold | P0 |
| FR-047-47 | Regression check MUST run | P0 |
| FR-047-48 | Regression = fail (configurable) | P0 |
| FR-047-49 | Multiple conditions MUST AND | P0 |
| FR-047-50 | All pass = overall pass | P0 |
| FR-047-51 | Any fail = overall fail | P0 |
| FR-047-52 | Reasons MUST be captured | P0 |
| FR-047-53 | Detailed breakdown MUST exist | P0 |
| FR-047-54 | Failed conditions MUST list | P0 |
| FR-047-55 | Exit code MUST reflect verdict | P0 |
| FR-047-56 | Exit 0 = pass | P0 |
| FR-047-57 | Exit 1 = fail | P0 |
| FR-047-58 | Gate timing MUST be captured | P0 |
| FR-047-59 | Gate run MUST be logged | P0 |
| FR-047-60 | Gate result MUST be persisted | P0 |

### FR-061 to FR-075: Override

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-047-61 | Override MUST be possible | P0 |
| FR-047-62 | Override MUST require reason | P0 |
| FR-047-63 | Override MUST be logged | P0 |
| FR-047-64 | Override MUST be auditable | P0 |
| FR-047-65 | Override CLI flag MUST exist | P0 |
| FR-047-66 | `--force` MUST bypass gate | P0 |
| FR-047-67 | Force MUST still run checks | P0 |
| FR-047-68 | Force MUST log verdict | P0 |
| FR-047-69 | Force reason MUST be captured | P0 |
| FR-047-70 | Override approval MAY require | P2 |
| FR-047-71 | Override count MUST track | P0 |
| FR-047-72 | Frequent override MUST warn | P1 |
| FR-047-73 | Override expiry MAY exist | P2 |
| FR-047-74 | Override scope MUST be limited | P0 |
| FR-047-75 | Override MUST NOT be default | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-047-01 | Score calculation | <100ms | P0 |
| NFR-047-02 | Threshold check | <10ms | P0 |
| NFR-047-03 | Gate evaluation | <200ms | P0 |
| NFR-047-04 | Baseline comparison | <500ms | P0 |
| NFR-047-05 | Result persistence | <100ms | P0 |
| NFR-047-06 | Memory usage | <50MB | P0 |
| NFR-047-07 | Large suite scoring | <1s | P0 |
| NFR-047-08 | Config load | <50ms | P0 |
| NFR-047-09 | Override processing | <100ms | P0 |
| NFR-047-10 | Audit write | <50ms | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-047-11 | Score determinism | 100% | P0 |
| NFR-047-12 | Threshold accuracy | 100% | P0 |
| NFR-047-13 | Gate consistency | 100% | P0 |
| NFR-047-14 | Fail closed | Always | P0 |
| NFR-047-15 | Cross-platform | All OS | P0 |
| NFR-047-16 | Config validation | 100% | P0 |
| NFR-047-17 | Error recovery | Graceful | P0 |
| NFR-047-18 | Partial results | Handled | P0 |
| NFR-047-19 | Override integrity | Verified | P0 |
| NFR-047-20 | Audit completeness | 100% | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-047-21 | Gate start logged | Info | P0 |
| NFR-047-22 | Score logged | Info | P0 |
| NFR-047-23 | Verdict logged | Info | P0 |
| NFR-047-24 | Override logged | Warning | P0 |
| NFR-047-25 | Failure reason | Error | P0 |
| NFR-047-26 | Metrics: pass rate | Gauge | P0 |
| NFR-047-27 | Metrics: score | Gauge | P0 |
| NFR-047-28 | Metrics: overrides | Counter | P0 |
| NFR-047-29 | Structured logging | JSON | P0 |
| NFR-047-30 | Trace ID | Included | P1 |

---

## Acceptance Criteria / Definition of Done

### Scoring
- [ ] AC-001: Score calculated
- [ ] AC-002: Range 0-1
- [ ] AC-003: Weights work
- [ ] AC-004: Defaults exist
- [ ] AC-005: Breakdown available
- [ ] AC-006: Deterministic
- [ ] AC-007: Critical tasks
- [ ] AC-008: Logged

### Thresholds
- [ ] AC-009: Thresholds work
- [ ] AC-010: Default 0.80
- [ ] AC-011: Configurable
- [ ] AC-012: Per-metric
- [ ] AC-013: Regression threshold
- [ ] AC-014: Validation
- [ ] AC-015: Precedence
- [ ] AC-016: Logged

### Gate
- [ ] AC-017: Verdict produced
- [ ] AC-018: Pass/fail/warn
- [ ] AC-019: Exit codes
- [ ] AC-020: Reasons
- [ ] AC-021: Breakdown
- [ ] AC-022: Regression check
- [ ] AC-023: Persisted
- [ ] AC-024: Logged

### Override
- [ ] AC-025: Override works
- [ ] AC-026: Reason required
- [ ] AC-027: Logged
- [ ] AC-028: Auditable
- [ ] AC-029: Count tracked
- [ ] AC-030: Tests pass
- [ ] AC-031: Documented
- [ ] AC-032: Reviewed

---

## User Verification Scenarios

### Scenario 1: Pass Gate
**Persona:** Developer  
**Preconditions:** Good results  
**Steps:**
1. Run benchmarks
2. Score calculated
3. Above threshold
4. Gate passes

**Verification Checklist:**
- [ ] Score shown
- [ ] Threshold shown
- [ ] Pass verdict
- [ ] Exit 0

### Scenario 2: Fail Gate
**Persona:** Developer  
**Preconditions:** Poor results  
**Steps:**
1. Run benchmarks
2. Score calculated
3. Below threshold
4. Gate fails

**Verification Checklist:**
- [ ] Score shown
- [ ] Threshold shown
- [ ] Fail verdict
- [ ] Exit 1

### Scenario 3: Regression Detected
**Persona:** Developer  
**Preconditions:** Baseline exists  
**Steps:**
1. Run benchmarks
2. Compare to baseline
3. Regression found
4. Gate fails

**Verification Checklist:**
- [ ] Comparison works
- [ ] Regression detected
- [ ] Reason clear
- [ ] Actionable

### Scenario 4: Override Gate
**Persona:** Developer with justification  
**Preconditions:** Gate failed  
**Steps:**
1. Gate fails
2. Use --force with reason
3. Override logged
4. Proceed

**Verification Checklist:**
- [ ] Override works
- [ ] Reason captured
- [ ] Audit logged
- [ ] Proceeds

### Scenario 5: Configure Threshold
**Persona:** Tech Lead  
**Preconditions:** Default too strict  
**Steps:**
1. Modify config
2. Set new threshold
3. Run gate
4. New threshold used

**Verification Checklist:**
- [ ] Config works
- [ ] Threshold changed
- [ ] Applied correctly
- [ ] Logged

### Scenario 6: View Breakdown
**Persona:** Developer  
**Preconditions:** Gate complete  
**Steps:**
1. View results
2. See breakdown
3. Identify weak areas
4. Improve

**Verification Checklist:**
- [ ] Breakdown shown
- [ ] Components visible
- [ ] Weights shown
- [ ] Actionable

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-047-01 | Score calculation | FR-047-01 |
| UT-047-02 | Score range | FR-047-03 |
| UT-047-03 | Weight application | FR-047-07 |
| UT-047-04 | Default weights | FR-047-08 |
| UT-047-05 | Threshold check | FR-047-21 |
| UT-047-06 | Default threshold | FR-047-23 |
| UT-047-07 | Gate pass | FR-047-45 |
| UT-047-08 | Gate fail | FR-047-46 |
| UT-047-09 | Regression check | FR-047-47 |
| UT-047-10 | Override | FR-047-61 |
| UT-047-11 | Exit codes | FR-047-55 |
| UT-047-12 | Breakdown | FR-047-53 |
| UT-047-13 | Critical tasks | FR-047-12 |
| UT-047-14 | Config precedence | FR-047-35 |
| UT-047-15 | Reason capture | FR-047-52 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-047-01 | Full gate E2E | E2E |
| IT-047-02 | Results integration | Task 046.c |
| IT-047-03 | Baseline comparison | Task 048 |
| IT-047-04 | CLI integration | Task 046.b |
| IT-047-05 | Config loading | FR-047-32 |
| IT-047-06 | Override logging | FR-047-63 |
| IT-047-07 | Cross-platform | NFR-047-15 |
| IT-047-08 | Fail closed | NFR-047-14 |
| IT-047-09 | Audit trail | FR-047-64 |
| IT-047-10 | Logging | NFR-047-21 |
| IT-047-11 | Large suite | NFR-047-07 |
| IT-047-12 | Partial results | NFR-047-18 |
| IT-047-13 | Per-category | FR-047-27 |
| IT-047-14 | Regression block | FR-047-48 |
| IT-047-15 | Override count | FR-047-71 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Gates/
│       ├── Score.cs
│       ├── ScoreBreakdown.cs
│       ├── Threshold.cs
│       ├── GateVerdict.cs
│       └── Override.cs
├── Acode.Application/
│   └── Gates/
│       ├── IScorer.cs
│       ├── IGate.cs
│       ├── IScoringConfig.cs
│       └── GateOptions.cs
├── Acode.Infrastructure/
│   └── Gates/
│       ├── WeightedScorer.cs
│       ├── ThresholdGate.cs
│       ├── RegressionDetector.cs
│       ├── OverrideManager.cs
│       └── GateAuditLogger.cs
```

### Gate Configuration

```yaml
# .agent/gates.yml
scoring:
  weights:
    passRate: 0.6
    runtime: 0.2
    tokenUsage: 0.2
  critical:
    - BENCH-001
    - BENCH-015

thresholds:
  pass: 0.80
  warn: 0.90
  regression: 0.05

override:
  requireReason: true
  maxCount: 3
  warnAfter: 1
```

**End of Task 047 Specification**
