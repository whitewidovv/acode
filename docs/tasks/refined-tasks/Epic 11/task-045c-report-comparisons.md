# Task 045.c: Report Comparisons

**Priority:** P1 – High  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 11 – Optimization  
**Dependencies:** Task 045 (Harness), Task 045.a (Microbench), Task 045.b (Correctness)  

---

## Description

Task 045.c implements benchmark report comparison—the ability to compare multiple benchmark runs side by side, highlight regressions and improvements, and generate actionable comparison reports. Without comparison, raw numbers are meaningless; you need context.

Report comparison takes two or more benchmark runs (from Task 045, 045.a, 045.b) and produces a unified comparison showing: (1) delta between runs, (2) percentage change, (3) regression/improvement classification, (4) statistical significance, and (5) actionable recommendations.

### Business Value

Report comparison provides:
- Trend visibility
- Regression detection
- Improvement validation
- Decision support
- Historical tracking

### Scope Boundaries

This task covers comparison reports. Core harness is Task 045. Microbenchmarks are Task 045.a. Correctness is Task 045.b.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Harness | Task 045 | Results input | Source |
| Microbench | Task 045.a | Metrics input | Source |
| Correctness | Task 045.b | Accuracy input | Source |
| Storage | File | Results storage | Persistence |
| Export | File | Report output | Output |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Missing run | Check | Error | Cannot compare |
| Schema mismatch | Validate | Error | Upgrade |
| No overlap | Check | Warn | Partial compare |
| Invalid stats | Validate | Skip | Missing data |

### Assumptions

1. **Results exist**: Previous runs saved
2. **Schema stable**: Or versioned
3. **Same tests**: Comparable suites
4. **Stats meaningful**: Sufficient data
5. **Output format**: Defined

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Baseline | Reference run |
| Candidate | Run being compared |
| Delta | Difference |
| Regression | Performance worse |
| Improvement | Performance better |
| Significance | Statistical meaning |
| Variance | Expected fluctuation |
| Threshold | Decision point |
| Trend | Direction over time |
| Report | Comparison output |

---

## Out of Scope

- Automated remediation
- Continuous monitoring dashboards
- Alert generation
- External integrations
- Machine learning predictions
- Anomaly detection

---

## Functional Requirements

### FR-001 to FR-020: Run Selection

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045c-01 | Runs MUST be selectable | P0 |
| FR-045c-02 | By ID MUST work | P0 |
| FR-045c-03 | By date MUST work | P0 |
| FR-045c-04 | By tag MUST work | P1 |
| FR-045c-05 | Latest MUST be available | P0 |
| FR-045c-06 | N latest MUST work | P1 |
| FR-045c-07 | Date range MUST work | P1 |
| FR-045c-08 | Baseline MUST be settable | P0 |
| FR-045c-09 | Multiple candidates MUST work | P0 |
| FR-045c-10 | List runs MUST work | P0 |
| FR-045c-11 | Run metadata MUST show | P0 |
| FR-045c-12 | Filter by suite MUST work | P0 |
| FR-045c-13 | Filter by model MUST work | P1 |
| FR-045c-14 | Filter by config MUST work | P1 |
| FR-045c-15 | Validation MUST occur | P0 |
| FR-045c-16 | Missing run MUST error | P0 |
| FR-045c-17 | Schema MUST be checked | P0 |
| FR-045c-18 | Version compatibility MUST check | P0 |
| FR-045c-19 | Overlap MUST be detected | P0 |
| FR-045c-20 | Warn on no overlap | P0 |

### FR-021 to FR-040: Delta Calculation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045c-21 | Delta MUST be calculated | P0 |
| FR-045c-22 | Absolute delta MUST exist | P0 |
| FR-045c-23 | Percentage delta MUST exist | P0 |
| FR-045c-24 | Direction MUST be indicated | P0 |
| FR-045c-25 | Up/down MUST be shown | P0 |
| FR-045c-26 | Per-metric delta MUST exist | P0 |
| FR-045c-27 | Per-test delta MUST exist | P0 |
| FR-045c-28 | Aggregate delta MUST exist | P0 |
| FR-045c-29 | Weighted delta MAY exist | P2 |
| FR-045c-30 | Missing metrics MUST mark N/A | P0 |
| FR-045c-31 | New metrics MUST mark NEW | P0 |
| FR-045c-32 | Removed metrics MUST mark GONE | P0 |
| FR-045c-33 | Variance MUST be calculated | P0 |
| FR-045c-34 | StdDev MUST be considered | P0 |
| FR-045c-35 | Confidence MUST be shown | P0 |
| FR-045c-36 | P-value MAY be calculated | P2 |
| FR-045c-37 | Effect size MAY be shown | P2 |
| FR-045c-38 | Noise floor MUST be known | P0 |
| FR-045c-39 | Within noise MUST be marked | P0 |
| FR-045c-40 | Outside noise MUST be marked | P0 |

### FR-041 to FR-055: Classification

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045c-41 | Classification MUST occur | P0 |
| FR-045c-42 | Regression MUST be detected | P0 |
| FR-045c-43 | Improvement MUST be detected | P0 |
| FR-045c-44 | No change MUST be detected | P0 |
| FR-045c-45 | Threshold MUST be configurable | P0 |
| FR-045c-46 | Default threshold = 5% | P0 |
| FR-045c-47 | Per-metric thresholds MUST work | P1 |
| FR-045c-48 | Time: faster = improvement | P0 |
| FR-045c-49 | Memory: lower = improvement | P0 |
| FR-045c-50 | Accuracy: higher = improvement | P0 |
| FR-045c-51 | Severity MUST be rated | P0 |
| FR-045c-52 | Minor < 10% | P0 |
| FR-045c-53 | Moderate 10-25% | P0 |
| FR-045c-54 | Major > 25% | P0 |
| FR-045c-55 | Critical > 50% | P0 |

### FR-056 to FR-070: Report Generation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045c-56 | Report MUST be generated | P0 |
| FR-045c-57 | Summary MUST be included | P0 |
| FR-045c-58 | Details MUST be included | P0 |
| FR-045c-59 | Tables MUST be formatted | P0 |
| FR-045c-60 | Color coding MUST work | P1 |
| FR-045c-61 | Red = regression | P1 |
| FR-045c-62 | Green = improvement | P1 |
| FR-045c-63 | Yellow = within noise | P1 |
| FR-045c-64 | Sorting MUST be available | P0 |
| FR-045c-65 | By severity MUST work | P0 |
| FR-045c-66 | By delta MUST work | P0 |
| FR-045c-67 | By name MUST work | P0 |
| FR-045c-68 | Filtering MUST work | P0 |
| FR-045c-69 | Regressions only MUST work | P0 |
| FR-045c-70 | Improvements only MUST work | P0 |

### FR-071 to FR-080: Export

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-045c-71 | Text export MUST work | P0 |
| FR-045c-72 | JSON export MUST work | P0 |
| FR-045c-73 | Markdown export MUST work | P0 |
| FR-045c-74 | HTML export MAY work | P2 |
| FR-045c-75 | CSV export MAY work | P2 |
| FR-045c-76 | Output path MUST be settable | P0 |
| FR-045c-77 | Stdout MUST work | P0 |
| FR-045c-78 | Append MUST work | P1 |
| FR-045c-79 | Overwrite MUST confirm | P1 |
| FR-045c-80 | Format MUST be parseable | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-045c-01 | Run load time | <100ms | P0 |
| NFR-045c-02 | Delta calculation | <50ms | P0 |
| NFR-045c-03 | Classification | <10ms | P0 |
| NFR-045c-04 | Report generation | <200ms | P0 |
| NFR-045c-05 | Multi-run compare | <500ms | P0 |
| NFR-045c-06 | Export time | <100ms | P0 |
| NFR-045c-07 | Memory for 100 runs | <100MB | P0 |
| NFR-045c-08 | Large suite compare | <2s | P0 |
| NFR-045c-09 | Trend over 50 runs | <5s | P1 |
| NFR-045c-10 | Report size | <1MB | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-045c-11 | Delta accuracy | 100% | P0 |
| NFR-045c-12 | Classification accuracy | 100% | P0 |
| NFR-045c-13 | Cross-platform | All OS | P0 |
| NFR-045c-14 | Version upgrade | Graceful | P0 |
| NFR-045c-15 | Schema migration | Supported | P0 |
| NFR-045c-16 | Missing data | Handled | P0 |
| NFR-045c-17 | Corrupt data | Detected | P0 |
| NFR-045c-18 | Partial compare | Supported | P0 |
| NFR-045c-19 | Error messages | Clear | P0 |
| NFR-045c-20 | Recovery | Graceful | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-045c-21 | Compare logged | Info | P0 |
| NFR-045c-22 | Runs loaded | Debug | P0 |
| NFR-045c-23 | Regressions | Warning | P0 |
| NFR-045c-24 | Summary logged | Info | P0 |
| NFR-045c-25 | Metrics: compare count | Counter | P1 |
| NFR-045c-26 | Metrics: regressions | Counter | P0 |
| NFR-045c-27 | Metrics: improvements | Counter | P1 |
| NFR-045c-28 | Structured logging | JSON | P0 |
| NFR-045c-29 | Trace ID | Per compare | P1 |
| NFR-045c-30 | Export logged | Info | P0 |

---

## Acceptance Criteria / Definition of Done

### Run Selection
- [ ] AC-001: Runs selectable
- [ ] AC-002: By ID works
- [ ] AC-003: By date works
- [ ] AC-004: Latest works
- [ ] AC-005: Baseline settable
- [ ] AC-006: Multiple candidates
- [ ] AC-007: List works
- [ ] AC-008: Validation works

### Delta Calculation
- [ ] AC-009: Delta calculated
- [ ] AC-010: Absolute delta
- [ ] AC-011: Percentage delta
- [ ] AC-012: Direction shown
- [ ] AC-013: Per-metric
- [ ] AC-014: Variance shown
- [ ] AC-015: Confidence shown
- [ ] AC-016: Noise marked

### Classification
- [ ] AC-017: Classification works
- [ ] AC-018: Regression detected
- [ ] AC-019: Improvement detected
- [ ] AC-020: No change detected
- [ ] AC-021: Threshold works
- [ ] AC-022: Severity rated
- [ ] AC-023: Polarity correct
- [ ] AC-024: Configurable

### Report
- [ ] AC-025: Report generated
- [ ] AC-026: Summary included
- [ ] AC-027: Details included
- [ ] AC-028: Sorting works
- [ ] AC-029: Filtering works
- [ ] AC-030: Export works
- [ ] AC-031: Tests pass
- [ ] AC-032: Documented

---

## User Verification Scenarios

### Scenario 1: Compare Two Runs
**Persona:** Developer after changes  
**Preconditions:** Two runs exist  
**Steps:**
1. Select baseline run
2. Select candidate run
3. Generate comparison
4. Review deltas

**Verification Checklist:**
- [ ] Both load
- [ ] Deltas shown
- [ ] Direction clear
- [ ] Actionable

### Scenario 2: Detect Regression
**Persona:** Developer in CI  
**Preconditions:** Recent run  
**Steps:**
1. Compare to baseline
2. Regression detected
3. Review severity
4. Investigate

**Verification Checklist:**
- [ ] Regression found
- [ ] Severity shown
- [ ] Detail available
- [ ] Clear action

### Scenario 3: Validate Improvement
**Persona:** Developer optimizing  
**Preconditions:** Optimization done  
**Steps:**
1. Run benchmark
2. Compare to before
3. Improvement confirmed
4. Document

**Verification Checklist:**
- [ ] Improvement shown
- [ ] Percentage clear
- [ ] Significant
- [ ] Documented

### Scenario 4: Multi-Run Trend
**Persona:** Developer tracking  
**Preconditions:** 10+ runs  
**Steps:**
1. Select run range
2. View trend
3. Identify pattern
4. Take action

**Verification Checklist:**
- [ ] Multiple runs
- [ ] Trend visible
- [ ] Pattern clear
- [ ] Actionable

### Scenario 5: Export Comparison
**Persona:** Developer reporting  
**Preconditions:** Comparison done  
**Steps:**
1. Generate comparison
2. Export to Markdown
3. Include in PR
4. Review approved

**Verification Checklist:**
- [ ] Export works
- [ ] Markdown valid
- [ ] Readable
- [ ] Useful

### Scenario 6: Filter Regressions Only
**Persona:** Developer triaging  
**Preconditions:** Mixed results  
**Steps:**
1. Generate comparison
2. Filter regressions only
3. Focus on issues
4. Fix critical first

**Verification Checklist:**
- [ ] Filter works
- [ ] Only regressions
- [ ] Sorted by severity
- [ ] Prioritized

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-045c-01 | Run loading | FR-045c-01 |
| UT-045c-02 | By ID selection | FR-045c-02 |
| UT-045c-03 | Latest selection | FR-045c-05 |
| UT-045c-04 | Absolute delta | FR-045c-22 |
| UT-045c-05 | Percentage delta | FR-045c-23 |
| UT-045c-06 | Direction | FR-045c-24 |
| UT-045c-07 | Regression detection | FR-045c-42 |
| UT-045c-08 | Improvement detection | FR-045c-43 |
| UT-045c-09 | No change | FR-045c-44 |
| UT-045c-10 | Threshold | FR-045c-45 |
| UT-045c-11 | Severity | FR-045c-51 |
| UT-045c-12 | Report generation | FR-045c-56 |
| UT-045c-13 | Sorting | FR-045c-64 |
| UT-045c-14 | Filtering | FR-045c-68 |
| UT-045c-15 | Export | FR-045c-71 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-045c-01 | Harness integration | Task 045 |
| IT-045c-02 | Microbench integration | Task 045.a |
| IT-045c-03 | Correctness integration | Task 045.b |
| IT-045c-04 | Full comparison | E2E |
| IT-045c-05 | Large comparison | NFR-045c-08 |
| IT-045c-06 | Cross-platform | NFR-045c-13 |
| IT-045c-07 | Missing data | NFR-045c-16 |
| IT-045c-08 | Trend analysis | FR-045c-09 |
| IT-045c-09 | JSON export | FR-045c-72 |
| IT-045c-10 | Markdown export | FR-045c-73 |
| IT-045c-11 | Logging | NFR-045c-21 |
| IT-045c-12 | Schema upgrade | NFR-045c-15 |
| IT-045c-13 | Multiple candidates | FR-045c-09 |
| IT-045c-14 | Filter by suite | FR-045c-12 |
| IT-045c-15 | Severity sort | FR-045c-65 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Performance/
│       ├── ComparisonResult.cs
│       ├── DeltaClassification.cs
│       └── Severity.cs
├── Acode.Application/
│   └── Performance/
│       ├── IBenchmarkComparer.cs
│       ├── IReportExporter.cs
│       └── ComparisonOptions.cs
├── Acode.Infrastructure/
│   └── Performance/
│       ├── BenchmarkComparer.cs
│       ├── DeltaCalculator.cs
│       ├── RegressionClassifier.cs
│       ├── ReportGenerator.cs
│       └── Exporters/
│           ├── TextReportExporter.cs
│           ├── JsonReportExporter.cs
│           └── MarkdownReportExporter.cs
```

### Comparison Report

```
Benchmark Comparison Report
===========================
Baseline: run-2025-01-15-001 (v1.2.0)
Candidate: run-2025-01-16-001 (v1.3.0)

Summary:
  Total Metrics: 25
  Regressions: 2 (1 major, 1 minor)
  Improvements: 4 (2 moderate, 2 minor)
  No Change: 19

Regressions:
┌─────────────────────┬──────────┬──────────┬────────┬──────────┐
│ Metric              │ Baseline │ Candidate│ Delta  │ Severity │
├─────────────────────┼──────────┼──────────┼────────┼──────────┤
│ TTFT (p50)          │ 245ms    │ 312ms    │ +27%   │ MAJOR    │
│ Memory (peak)       │ 512MB    │ 538MB    │ +5%    │ MINOR    │
└─────────────────────┴──────────┴──────────┴────────┴──────────┘

Improvements:
┌─────────────────────┬──────────┬──────────┬────────┬──────────┐
│ Metric              │ Baseline │ Candidate│ Delta  │ Severity │
├─────────────────────┼──────────┼──────────┼────────┼──────────┤
│ TPS                 │ 45/s     │ 58/s     │ +29%   │ MODERATE │
│ Correctness         │ 84%      │ 91%      │ +8%    │ MODERATE │
│ Cache Hit Rate      │ 72%      │ 78%      │ +8%    │ MINOR    │
│ Startup Time        │ 1.2s     │ 1.1s     │ -8%    │ MINOR    │
└─────────────────────┴──────────┴──────────┴────────┴──────────┘

Recommendation:
  INVESTIGATE: TTFT regression (27%) exceeds threshold
  ACCEPT: Other changes within acceptable bounds
```

**End of Task 045.c Specification**
