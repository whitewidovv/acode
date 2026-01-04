# Task 048: Golden Baseline Maintenance

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 12 – Hardening  
**Dependencies:** Task 047 (Scoring), Task 046 (Benchmark Suite)  

---

## Description

Task 048 implements golden baseline maintenance—the reference standard against which all benchmark runs are compared. A baseline is a known-good state that represents acceptable quality. Without baselines, regression detection is impossible; you need a reference point to detect degradation.

Golden baselines are: (1) recorded snapshots of successful benchmark runs, (2) versioned and tracked for auditability, (3) updated deliberately through a controlled process, and (4) the foundation for regression gates. This task establishes the baseline lifecycle: creation, storage, comparison, and updates.

### Business Value

Golden baselines provide:
- Regression detection foundation
- Quality reference point
- Upgrade confidence
- Change impact measurement
- Historical anchor

### Scope Boundaries

This task establishes baseline framework. Baseline recording is Task 048.a. Change tracking is Task 048.b. Triage workflow is Task 048.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Scoring | Task 047 | Comparison | Consumer |
| Results | Task 046.c | Baseline source | Input |
| History | Task 047.c | Storage | Persistence |
| Gates | Task 047 | Reference | Decision |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Missing baseline | Check | Warn | No comparison |
| Corrupt baseline | Validate | Restore | Use backup |
| Outdated baseline | Age check | Warn | False regressions |
| Conflicting baselines | Version check | Error | Unclear reference |

### Assumptions

1. **Baselines stored**: Local file system
2. **Baselines versioned**: With metadata
3. **Updates deliberate**: Not automatic
4. **Backups exist**: On update
5. **One active**: Single current baseline

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Baseline | Reference standard |
| Golden | Approved/accepted |
| Snapshot | Point-in-time capture |
| Active | Currently used |
| Archive | Historical baseline |
| Update | Replace baseline |
| Restore | Return to previous |
| Regression | Below baseline |
| Delta | Difference from baseline |
| Promotion | Make new baseline |

---

## Out of Scope

- Automatic baseline updates
- ML-based baseline selection
- External baseline storage
- Multi-baseline comparison
- Baseline prediction
- Baseline recommendations

---

## Functional Requirements

### FR-001 to FR-020: Baseline Storage

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048-01 | Baseline MUST be storable | P0 |
| FR-048-02 | Storage MUST be local | P0 |
| FR-048-03 | Storage format: JSON | P0 |
| FR-048-04 | Baseline MUST have ID | P0 |
| FR-048-05 | Baseline MUST have version | P0 |
| FR-048-06 | Baseline MUST have timestamp | P0 |
| FR-048-07 | Baseline MUST have source run | P0 |
| FR-048-08 | Baseline MUST have metrics | P0 |
| FR-048-09 | Baseline MUST have summary | P0 |
| FR-048-10 | Per-task baselines MUST exist | P0 |
| FR-048-11 | Active baseline MUST be marked | P0 |
| FR-048-12 | Only one active MUST be enforced | P0 |
| FR-048-13 | Archive baselines MUST be kept | P0 |
| FR-048-14 | Archive count MUST be configurable | P0 |
| FR-048-15 | Default archive = 10 | P0 |
| FR-048-16 | Storage path MUST be configurable | P0 |
| FR-048-17 | Default path: .acode/baselines | P0 |
| FR-048-18 | Backup on update MUST occur | P0 |
| FR-048-19 | Integrity check MUST exist | P0 |
| FR-048-20 | Checksum MUST be stored | P0 |

### FR-021 to FR-040: Baseline Operations

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048-21 | Create baseline MUST work | P0 |
| FR-048-22 | Create from run MUST work | P0 |
| FR-048-23 | Create requires run ID | P0 |
| FR-048-24 | Get active baseline MUST work | P0 |
| FR-048-25 | Get by ID MUST work | P0 |
| FR-048-26 | List baselines MUST work | P0 |
| FR-048-27 | Update baseline MUST work | P0 |
| FR-048-28 | Update MUST require confirmation | P0 |
| FR-048-29 | Update MUST require reason | P0 |
| FR-048-30 | Update MUST backup previous | P0 |
| FR-048-31 | Delete baseline MUST work | P1 |
| FR-048-32 | Delete MUST require confirmation | P1 |
| FR-048-33 | Delete active MUST warn | P1 |
| FR-048-34 | Restore baseline MUST work | P0 |
| FR-048-35 | Restore from archive MUST work | P0 |
| FR-048-36 | Compare to baseline MUST work | P0 |
| FR-048-37 | Export baseline MUST work | P0 |
| FR-048-38 | Import baseline MUST work | P0 |
| FR-048-39 | Validate baseline MUST work | P0 |
| FR-048-40 | Baseline age MUST track | P0 |

### FR-041 to FR-060: Comparison

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048-41 | Compare MUST be automatic | P0 |
| FR-048-42 | Compare after run MUST work | P0 |
| FR-048-43 | Delta calculation MUST exist | P0 |
| FR-048-44 | Pass rate delta MUST show | P0 |
| FR-048-45 | Runtime delta MUST show | P0 |
| FR-048-46 | Score delta MUST show | P0 |
| FR-048-47 | Per-task delta MUST be available | P0 |
| FR-048-48 | Regression threshold MUST apply | P0 |
| FR-048-49 | Regression MUST be flagged | P0 |
| FR-048-50 | Regression MUST list tasks | P0 |
| FR-048-51 | Improvement MUST be flagged | P0 |
| FR-048-52 | No baseline = skip comparison | P0 |
| FR-048-53 | Missing baseline MUST warn | P0 |
| FR-048-54 | Baseline mismatch MUST warn | P0 |
| FR-048-55 | Suite version check MUST occur | P0 |
| FR-048-56 | Incompatible suite MUST error | P0 |
| FR-048-57 | Comparison result MUST persist | P0 |
| FR-048-58 | Comparison MUST log | P0 |
| FR-048-59 | Comparison in results MUST include | P0 |
| FR-048-60 | Comparison CLI output MUST show | P0 |

### FR-061 to FR-075: Lifecycle

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048-61 | Baseline age MUST track | P0 |
| FR-048-62 | Stale warning MUST exist | P0 |
| FR-048-63 | Default stale = 30 days | P0 |
| FR-048-64 | Stale threshold MUST configure | P0 |
| FR-048-65 | Refresh reminder MUST exist | P1 |
| FR-048-66 | Last update MUST be visible | P0 |
| FR-048-67 | Update history MUST be tracked | P0 |
| FR-048-68 | Who updated MUST be tracked | P1 |
| FR-048-69 | Why updated MUST be tracked | P0 |
| FR-048-70 | Rollback MUST be possible | P0 |
| FR-048-71 | Rollback MUST be logged | P0 |
| FR-048-72 | Multiple rollback MUST work | P1 |
| FR-048-73 | Archive cleanup MUST work | P0 |
| FR-048-74 | Oldest archive MUST prune | P0 |
| FR-048-75 | Prune MUST log | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-048-01 | Baseline load | <100ms | P0 |
| NFR-048-02 | Baseline save | <200ms | P0 |
| NFR-048-03 | Comparison | <500ms | P0 |
| NFR-048-04 | List operation | <50ms | P0 |
| NFR-048-05 | Restore | <200ms | P0 |
| NFR-048-06 | Export | <500ms | P0 |
| NFR-048-07 | Import | <500ms | P0 |
| NFR-048-08 | Validation | <100ms | P0 |
| NFR-048-09 | Baseline size | <1MB | P0 |
| NFR-048-10 | Archive total | <10MB | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-048-11 | Data integrity | 100% | P0 |
| NFR-048-12 | Checksum verify | Always | P0 |
| NFR-048-13 | Backup success | 100% | P0 |
| NFR-048-14 | Cross-platform | All OS | P0 |
| NFR-048-15 | Atomic update | Always | P0 |
| NFR-048-16 | Concurrent access | Safe | P0 |
| NFR-048-17 | Corrupt recovery | Graceful | P0 |
| NFR-048-18 | Schema evolution | Handled | P0 |
| NFR-048-19 | Error messages | Clear | P0 |
| NFR-048-20 | Rollback reliability | 100% | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-048-21 | Operations logged | Info | P0 |
| NFR-048-22 | Updates logged | Warning | P0 |
| NFR-048-23 | Rollbacks logged | Warning | P0 |
| NFR-048-24 | Errors logged | Error | P0 |
| NFR-048-25 | Comparison logged | Info | P0 |
| NFR-048-26 | Metrics: age | Gauge | P1 |
| NFR-048-27 | Metrics: updates | Counter | P1 |
| NFR-048-28 | Structured logging | JSON | P0 |
| NFR-048-29 | Audit trail | Complete | P0 |
| NFR-048-30 | Trace ID | Included | P1 |

---

## Acceptance Criteria / Definition of Done

### Storage
- [ ] AC-001: Baseline stored
- [ ] AC-002: JSON format
- [ ] AC-003: Version tracked
- [ ] AC-004: Active marked
- [ ] AC-005: Archive kept
- [ ] AC-006: Backup works
- [ ] AC-007: Checksum
- [ ] AC-008: Path config

### Operations
- [ ] AC-009: Create works
- [ ] AC-010: Get active
- [ ] AC-011: List works
- [ ] AC-012: Update works
- [ ] AC-013: Reason required
- [ ] AC-014: Restore works
- [ ] AC-015: Export works
- [ ] AC-016: Import works

### Comparison
- [ ] AC-017: Auto compare
- [ ] AC-018: Delta shown
- [ ] AC-019: Regression flagged
- [ ] AC-020: Tasks listed
- [ ] AC-021: Threshold works
- [ ] AC-022: In results
- [ ] AC-023: CLI shows
- [ ] AC-024: Logged

### Lifecycle
- [ ] AC-025: Age tracked
- [ ] AC-026: Stale warning
- [ ] AC-027: History tracked
- [ ] AC-028: Rollback works
- [ ] AC-029: Archive prune
- [ ] AC-030: Tests pass
- [ ] AC-031: Documented
- [ ] AC-032: Cross-platform

---

## User Verification Scenarios

### Scenario 1: Create Baseline
**Persona:** Tech Lead  
**Preconditions:** Successful run  
**Steps:**
1. Run benchmarks
2. Results pass gate
3. Create baseline
4. Becomes active

**Verification Checklist:**
- [ ] Baseline created
- [ ] Active marked
- [ ] Metrics captured
- [ ] Logged

### Scenario 2: Compare to Baseline
**Persona:** Developer  
**Preconditions:** Baseline exists  
**Steps:**
1. Run benchmarks
2. Auto compare
3. See delta
4. Know status

**Verification Checklist:**
- [ ] Comparison auto
- [ ] Delta shown
- [ ] Status clear
- [ ] Actionable

### Scenario 3: Update Baseline
**Persona:** Tech Lead  
**Preconditions:** New baseline needed  
**Steps:**
1. Run new benchmark
2. Decide to update
3. Provide reason
4. New baseline active

**Verification Checklist:**
- [ ] Confirmation required
- [ ] Reason captured
- [ ] Previous backed up
- [ ] New active

### Scenario 4: Rollback Baseline
**Persona:** Tech Lead  
**Preconditions:** Bad update  
**Steps:**
1. Realize issue
2. List archives
3. Select previous
4. Restore

**Verification Checklist:**
- [ ] Archives listed
- [ ] Selection works
- [ ] Restore works
- [ ] Logged

### Scenario 5: Stale Warning
**Persona:** Developer  
**Preconditions:** Old baseline  
**Steps:**
1. Run benchmarks
2. Baseline > 30 days
3. Warning shown
4. Consider refresh

**Verification Checklist:**
- [ ] Age checked
- [ ] Warning shown
- [ ] Threshold works
- [ ] Actionable

### Scenario 6: Export/Import
**Persona:** Developer  
**Preconditions:** Baseline exists  
**Steps:**
1. Export baseline
2. Transfer to new machine
3. Import baseline
4. Works same

**Verification Checklist:**
- [ ] Export works
- [ ] File portable
- [ ] Import works
- [ ] Same behavior

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-048-01 | Baseline storage | FR-048-01 |
| UT-048-02 | JSON format | FR-048-03 |
| UT-048-03 | Version tracking | FR-048-05 |
| UT-048-04 | Active marking | FR-048-11 |
| UT-048-05 | Checksum | FR-048-20 |
| UT-048-06 | Create operation | FR-048-21 |
| UT-048-07 | Get active | FR-048-24 |
| UT-048-08 | Update operation | FR-048-27 |
| UT-048-09 | Restore operation | FR-048-34 |
| UT-048-10 | Delta calculation | FR-048-43 |
| UT-048-11 | Regression flag | FR-048-49 |
| UT-048-12 | Age tracking | FR-048-61 |
| UT-048-13 | Stale check | FR-048-62 |
| UT-048-14 | Archive prune | FR-048-73 |
| UT-048-15 | Validation | FR-048-39 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-048-01 | Full lifecycle E2E | E2E |
| IT-048-02 | Results integration | Task 046.c |
| IT-048-03 | Scoring integration | Task 047 |
| IT-048-04 | Gates integration | Task 047 |
| IT-048-05 | Cross-platform | NFR-048-14 |
| IT-048-06 | Concurrent access | NFR-048-16 |
| IT-048-07 | Corrupt recovery | NFR-048-17 |
| IT-048-08 | Logging | NFR-048-21 |
| IT-048-09 | Export/import | FR-048-37 |
| IT-048-10 | Rollback chain | FR-048-72 |
| IT-048-11 | Archive management | FR-048-13 |
| IT-048-12 | Suite version check | FR-048-55 |
| IT-048-13 | Backup on update | FR-048-30 |
| IT-048-14 | Comparison in CLI | FR-048-60 |
| IT-048-15 | Audit trail | NFR-048-29 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Baselines/
│       ├── Baseline.cs
│       ├── BaselineMetrics.cs
│       ├── BaselineVersion.cs
│       └── BaselineComparison.cs
├── Acode.Application/
│   └── Baselines/
│       ├── IBaselineManager.cs
│       ├── IBaselineComparer.cs
│       └── BaselineOptions.cs
├── Acode.Infrastructure/
│   └── Baselines/
│       ├── FileBaselineManager.cs
│       ├── BaselineComparer.cs
│       ├── BaselineValidator.cs
│       └── BaselineArchive.cs
├── data/
│   └── baselines/
│       ├── active.json
│       └── archive/
```

### Baseline Format

```json
{
  "id": "baseline-2025-01-15-001",
  "version": "1.0.0",
  "createdAt": "2025-01-15T10:30:00Z",
  "updatedAt": "2025-01-15T10:30:00Z",
  "sourceRun": "run-2025-01-15-001",
  "reason": "Initial baseline after v1.0 release",
  "suite": {
    "id": "default-suite-v1",
    "version": "1.0.0"
  },
  "summary": {
    "passRate": 85.0,
    "avgRuntimeMs": 2500,
    "score": 0.82
  },
  "tasks": {
    "BENCH-001": { "status": "passed", "runtimeMs": 1250 },
    "BENCH-002": { "status": "passed", "runtimeMs": 2100 }
  },
  "checksum": "sha256:abc123..."
}
```

### CLI Commands

```bash
# Create baseline from run
acode baseline create --run run-2025-01-15-001 --reason "Initial baseline"

# Show active baseline
acode baseline show

# Compare current run to baseline
acode bench run --compare-baseline

# Update baseline
acode baseline update --run run-2025-01-16-001 --reason "Updated after fixes"

# List archives
acode baseline list --archive

# Restore previous
acode baseline restore --id baseline-2025-01-10-001
```

**End of Task 048 Specification**
