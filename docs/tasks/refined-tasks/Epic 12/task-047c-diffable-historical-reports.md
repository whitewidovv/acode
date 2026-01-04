# Task 047.c: Diffable Historical Reports

**Priority:** P1 – High  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 12 – Hardening  
**Dependencies:** Task 047 (Scoring), Task 047.a (Metrics), Task 046.c (Results)  

---

## Description

Task 047.c implements diffable historical reports—the ability to compare benchmark results over time and visualize trends. Numbers in isolation are less useful than numbers in context. Historical reports show how quality metrics evolve across runs, releases, and time.

Diffable means: (1) structured output that can be compared with standard diff tools, (2) semantic comparison that understands metric relationships, (3) trend visualization that shows direction over time, and (4) regression highlighting that calls attention to degradation.

### Business Value

Historical reports provide:
- Trend visibility
- Pattern recognition
- Regression root cause
- Progress tracking
- Release confidence

### Scope Boundaries

This task covers historical comparison and reports. Results format is Task 046.c. Metrics are Task 047.a. Baseline management is Task 048.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Results | Task 046.c | Historical data | Source |
| Metrics | Task 047.a | Calculated values | Input |
| Scoring | Task 047 | Score history | Input |
| Baseline | Task 048 | Reference points | Reference |
| Export | File | Report output | Output |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Missing history | Check | Warn | Partial report |
| Schema mismatch | Validate | Migrate | May fail |
| Storage full | Check | Prune | Data loss |
| Corrupt data | Validate | Skip | Gaps |

### Assumptions

1. **History stored**: Results persist
2. **Schema versioned**: Or compatible
3. **Runs comparable**: Same suite
4. **Storage sufficient**: For retention
5. **Diff tools**: Standard Unix diff

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Historical | Over time |
| Diff | Difference comparison |
| Trend | Direction over time |
| Report | Formatted output |
| Delta | Change amount |
| Regression | Quality decrease |
| Improvement | Quality increase |
| Baseline | Reference point |
| Retention | How long kept |
| Prune | Remove old data |

---

## Out of Scope

- Real-time monitoring
- Dashboards
- Alerts
- External storage
- Cloud sync
- Prediction/forecasting

---

## Functional Requirements

### FR-001 to FR-020: History Storage

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-047c-01 | Results MUST be stored | P0 |
| FR-047c-02 | Storage MUST be local | P0 |
| FR-047c-03 | Storage format: JSON | P0 |
| FR-047c-04 | Per-run files MUST exist | P0 |
| FR-047c-05 | Filename: timestamp-based | P0 |
| FR-047c-06 | Index file MUST exist | P0 |
| FR-047c-07 | Index: run metadata | P0 |
| FR-047c-08 | Retention MUST be configurable | P0 |
| FR-047c-09 | Default retention: 90 days | P0 |
| FR-047c-10 | Retention by count MAY exist | P1 |
| FR-047c-11 | Pruning MUST be automatic | P0 |
| FR-047c-12 | Pruning MUST log | P0 |
| FR-047c-13 | Manual prune MUST work | P1 |
| FR-047c-14 | Storage path MUST be configurable | P0 |
| FR-047c-15 | Default path: .acode/history | P0 |
| FR-047c-16 | Compression MAY be used | P2 |
| FR-047c-17 | Size limit MAY exist | P2 |
| FR-047c-18 | Backup MAY exist | P2 |
| FR-047c-19 | Import MUST work | P1 |
| FR-047c-20 | Export MUST work | P0 |

### FR-021 to FR-040: Historical Query

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-047c-21 | Query by date MUST work | P0 |
| FR-047c-22 | Query by range MUST work | P0 |
| FR-047c-23 | Query by run ID MUST work | P0 |
| FR-047c-24 | Query latest N MUST work | P0 |
| FR-047c-25 | Query by tag MUST work | P1 |
| FR-047c-26 | Query by suite MUST work | P0 |
| FR-047c-27 | Query by model MUST work | P1 |
| FR-047c-28 | List all runs MUST work | P0 |
| FR-047c-29 | Run summary MUST be available | P0 |
| FR-047c-30 | Filter by pass rate MUST work | P1 |
| FR-047c-31 | Filter by status MUST work | P1 |
| FR-047c-32 | Sort by date MUST work | P0 |
| FR-047c-33 | Sort by score MUST work | P1 |
| FR-047c-34 | Pagination MAY exist | P2 |
| FR-047c-35 | Search MUST work | P1 |
| FR-047c-36 | Full results MUST be loadable | P0 |
| FR-047c-37 | Lazy loading MAY exist | P2 |
| FR-047c-38 | Cache MAY exist | P2 |
| FR-047c-39 | Query logging MUST occur | P0 |
| FR-047c-40 | Query errors MUST handle | P0 |

### FR-041 to FR-060: Comparison

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-047c-41 | Two-run comparison MUST work | P0 |
| FR-047c-42 | Multi-run comparison MAY work | P1 |
| FR-047c-43 | Delta calculation MUST exist | P0 |
| FR-047c-44 | Absolute delta MUST show | P0 |
| FR-047c-45 | Percentage delta MUST show | P0 |
| FR-047c-46 | Direction MUST indicate | P0 |
| FR-047c-47 | Regression MUST highlight | P0 |
| FR-047c-48 | Improvement MUST highlight | P0 |
| FR-047c-49 | No change MUST indicate | P0 |
| FR-047c-50 | Per-task comparison MUST work | P0 |
| FR-047c-51 | Per-category comparison MUST work | P0 |
| FR-047c-52 | Summary comparison MUST work | P0 |
| FR-047c-53 | Missing tasks MUST handle | P0 |
| FR-047c-54 | New tasks MUST handle | P0 |
| FR-047c-55 | Removed tasks MUST handle | P0 |
| FR-047c-56 | Semantic diff MUST work | P0 |
| FR-047c-57 | Text diff MUST work | P0 |
| FR-047c-58 | Side-by-side MUST work | P0 |
| FR-047c-59 | Unified diff MAY work | P1 |
| FR-047c-60 | Threshold for "change" MUST exist | P0 |

### FR-061 to FR-075: Report Generation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-047c-61 | Report MUST be generatable | P0 |
| FR-047c-62 | Format: text MUST work | P0 |
| FR-047c-63 | Format: JSON MUST work | P0 |
| FR-047c-64 | Format: Markdown MUST work | P0 |
| FR-047c-65 | Format: HTML MAY work | P2 |
| FR-047c-66 | Trend report MUST exist | P0 |
| FR-047c-67 | Trend: pass rate over time | P0 |
| FR-047c-68 | Trend: runtime over time | P0 |
| FR-047c-69 | Trend: score over time | P0 |
| FR-047c-70 | Trend: ASCII chart MUST work | P0 |
| FR-047c-71 | Trend: data export MUST work | P0 |
| FR-047c-72 | Report title MUST exist | P0 |
| FR-047c-73 | Report timestamp MUST exist | P0 |
| FR-047c-74 | Report metadata MUST exist | P0 |
| FR-047c-75 | Report output path MUST configure | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-047c-01 | History load | <500ms | P0 |
| NFR-047c-02 | Query response | <100ms | P0 |
| NFR-047c-03 | Comparison calc | <200ms | P0 |
| NFR-047c-04 | Report generation | <500ms | P0 |
| NFR-047c-05 | 100 run history | <1s load | P0 |
| NFR-047c-06 | Storage per run | <100KB | P0 |
| NFR-047c-07 | Index size | <1MB | P0 |
| NFR-047c-08 | Memory usage | <100MB | P0 |
| NFR-047c-09 | Prune operation | <5s | P0 |
| NFR-047c-10 | Export time | <1s | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-047c-11 | Data integrity | 100% | P0 |
| NFR-047c-12 | Comparison accuracy | 100% | P0 |
| NFR-047c-13 | Cross-platform | All OS | P0 |
| NFR-047c-14 | Schema evolution | Handled | P0 |
| NFR-047c-15 | Corrupt detection | 100% | P0 |
| NFR-047c-16 | Graceful degradation | Partial | P0 |
| NFR-047c-17 | Backup on prune | Optional | P1 |
| NFR-047c-18 | Atomic writes | Always | P0 |
| NFR-047c-19 | Error recovery | Graceful | P0 |
| NFR-047c-20 | Locking | Proper | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-047c-21 | Storage logged | Info | P0 |
| NFR-047c-22 | Query logged | Debug | P0 |
| NFR-047c-23 | Prune logged | Info | P0 |
| NFR-047c-24 | Errors logged | Error | P0 |
| NFR-047c-25 | Report generated | Info | P0 |
| NFR-047c-26 | Metrics: run count | Gauge | P1 |
| NFR-047c-27 | Metrics: storage size | Gauge | P1 |
| NFR-047c-28 | Structured logging | JSON | P0 |
| NFR-047c-29 | Comparison events | Logged | P0 |
| NFR-047c-30 | Trace ID | Included | P1 |

---

## Acceptance Criteria / Definition of Done

### Storage
- [ ] AC-001: Results stored
- [ ] AC-002: Local storage
- [ ] AC-003: JSON format
- [ ] AC-004: Index exists
- [ ] AC-005: Retention works
- [ ] AC-006: Pruning works
- [ ] AC-007: Path configurable
- [ ] AC-008: Export works

### Query
- [ ] AC-009: By date
- [ ] AC-010: By range
- [ ] AC-011: By ID
- [ ] AC-012: Latest N
- [ ] AC-013: List all
- [ ] AC-014: Summary
- [ ] AC-015: Filter
- [ ] AC-016: Sort

### Comparison
- [ ] AC-017: Two-run
- [ ] AC-018: Delta calc
- [ ] AC-019: Direction
- [ ] AC-020: Regression
- [ ] AC-021: Per-task
- [ ] AC-022: Per-category
- [ ] AC-023: Missing handled
- [ ] AC-024: Text diff

### Report
- [ ] AC-025: Generated
- [ ] AC-026: Text format
- [ ] AC-027: JSON format
- [ ] AC-028: Markdown
- [ ] AC-029: Trend
- [ ] AC-030: Tests pass
- [ ] AC-031: Documented
- [ ] AC-032: Cross-platform

---

## User Verification Scenarios

### Scenario 1: View History
**Persona:** Developer  
**Preconditions:** Multiple runs exist  
**Steps:**
1. List historical runs
2. See run list
3. Select run
4. View details

**Verification Checklist:**
- [ ] List works
- [ ] Runs shown
- [ ] Selection works
- [ ] Details available

### Scenario 2: Compare Runs
**Persona:** Developer  
**Preconditions:** Two runs exist  
**Steps:**
1. Select baseline
2. Select candidate
3. Generate comparison
4. Review diff

**Verification Checklist:**
- [ ] Comparison works
- [ ] Delta shown
- [ ] Direction clear
- [ ] Actionable

### Scenario 3: View Trend
**Persona:** Tech Lead  
**Preconditions:** History exists  
**Steps:**
1. Request trend report
2. See pass rate trend
3. See runtime trend
4. Identify patterns

**Verification Checklist:**
- [ ] Trend generated
- [ ] Chart readable
- [ ] Data accurate
- [ ] Patterns visible

### Scenario 4: Regression Investigation
**Persona:** Developer  
**Preconditions:** Regression detected  
**Steps:**
1. Find regression point
2. Compare before/after
3. Identify changed tasks
4. Root cause

**Verification Checklist:**
- [ ] Point found
- [ ] Comparison works
- [ ] Changes visible
- [ ] Actionable

### Scenario 5: Export Report
**Persona:** Developer  
**Preconditions:** Comparison done  
**Steps:**
1. Generate report
2. Export to Markdown
3. Include in docs
4. Share

**Verification Checklist:**
- [ ] Export works
- [ ] Markdown valid
- [ ] Readable
- [ ] Complete

### Scenario 6: Prune History
**Persona:** Developer  
**Preconditions:** Old runs exist  
**Steps:**
1. Check storage size
2. Run prune
3. Old runs removed
4. Space freed

**Verification Checklist:**
- [ ] Prune works
- [ ] Old removed
- [ ] Recent kept
- [ ] Logged

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-047c-01 | Storage write | FR-047c-01 |
| UT-047c-02 | Index update | FR-047c-06 |
| UT-047c-03 | Query by date | FR-047c-21 |
| UT-047c-04 | Query by range | FR-047c-22 |
| UT-047c-05 | Latest N | FR-047c-24 |
| UT-047c-06 | Delta calculation | FR-047c-43 |
| UT-047c-07 | Direction detection | FR-047c-46 |
| UT-047c-08 | Regression highlight | FR-047c-47 |
| UT-047c-09 | Report generation | FR-047c-61 |
| UT-047c-10 | Text format | FR-047c-62 |
| UT-047c-11 | JSON format | FR-047c-63 |
| UT-047c-12 | Markdown format | FR-047c-64 |
| UT-047c-13 | Trend calculation | FR-047c-66 |
| UT-047c-14 | Pruning | FR-047c-11 |
| UT-047c-15 | Retention check | FR-047c-08 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-047c-01 | Full history E2E | E2E |
| IT-047c-02 | Results integration | Task 046.c |
| IT-047c-03 | Metrics integration | Task 047.a |
| IT-047c-04 | Large history | NFR-047c-05 |
| IT-047c-05 | Cross-platform | NFR-047c-13 |
| IT-047c-06 | Schema evolution | NFR-047c-14 |
| IT-047c-07 | Concurrent access | NFR-047c-20 |
| IT-047c-08 | Logging | NFR-047c-21 |
| IT-047c-09 | Export round-trip | FR-047c-20 |
| IT-047c-10 | Missing tasks | FR-047c-53 |
| IT-047c-11 | New tasks | FR-047c-54 |
| IT-047c-12 | Trend report | FR-047c-66 |
| IT-047c-13 | Text diff | FR-047c-57 |
| IT-047c-14 | Semantic diff | FR-047c-56 |
| IT-047c-15 | Prune + backup | NFR-047c-17 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── History/
│       ├── HistoricalRun.cs
│       ├── RunIndex.cs
│       ├── RunComparison.cs
│       └── Trend.cs
├── Acode.Application/
│   └── History/
│       ├── IHistoryStore.cs
│       ├── IHistoryQuery.cs
│       ├── IComparer.cs
│       ├── IReportGenerator.cs
│       └── HistoryOptions.cs
├── Acode.Infrastructure/
│   └── History/
│       ├── FileHistoryStore.cs
│       ├── HistoryQuery.cs
│       ├── RunComparer.cs
│       ├── TrendCalculator.cs
│       └── Reports/
│           ├── TextReportGenerator.cs
│           ├── JsonReportGenerator.cs
│           └── MarkdownReportGenerator.cs
```

### Comparison Report

```markdown
# Benchmark Comparison Report

Generated: 2025-01-16T10:30:00Z

## Runs Compared
| Property | Baseline | Candidate |
|----------|----------|-----------|
| Run ID | run-2025-01-15-001 | run-2025-01-16-001 |
| Date | 2025-01-15 | 2025-01-16 |
| Suite | default-v1 | default-v1 |

## Summary
| Metric | Baseline | Candidate | Delta | Status |
|--------|----------|-----------|-------|--------|
| Pass Rate | 82.0% | 84.0% | +2.0% | ✅ Improved |
| Avg Runtime | 2500ms | 2300ms | -8.0% | ✅ Improved |
| Score | 0.78 | 0.82 | +0.04 | ✅ Improved |

## Regressions (0)
None detected.

## Improvements (3)
- BENCH-015: Now passing (was failing)
- BENCH-023: Runtime -25% (4000ms → 3000ms)
- BENCH-042: Runtime -15% (8000ms → 6800ms)

## Trend (Last 10 Runs)
```
Pass Rate:
100% |
 90% |        ●     ●
 80% | ● ●  ●   ● ●   ● ●
 70% |   ●
     +-------------------
       1 2 3 4 5 6 7 8 9 10
```

**End of Task 047.c Specification**
