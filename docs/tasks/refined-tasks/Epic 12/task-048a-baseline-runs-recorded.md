# Task 048.a: Baseline Runs Recorded

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 12 – Hardening  
**Dependencies:** Task 048 (Baseline), Task 046.c (Results)  

---

## Description

Task 048.a implements baseline run recording—the process of capturing a benchmark run as a golden baseline. Not every run becomes a baseline; only runs that represent acceptable quality are promoted. Recording captures the complete run state so future comparisons are accurate.

Recording involves: (1) capturing all metrics and per-task results, (2) capturing environment and configuration, (3) creating metadata (who, when, why), (4) validating completeness, and (5) storing in the baseline format. The recorded baseline becomes the regression detection reference.

### Business Value

Baseline recording provides:
- Complete state capture
- Accurate comparison foundation
- Audit trail
- Reproducibility
- Quality reference

### Scope Boundaries

This task covers recording baselines from runs. Change tracking is Task 048.b. Triage workflow is Task 048.c. Baseline management is Task 048.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Results | Task 046.c | Source data | Input |
| Baseline | Task 048 | Storage | Output |
| Scoring | Task 047 | Validation | Check |
| CLI | Task 046.b | Commands | Interface |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Incomplete run | Validation | Reject | Cannot record |
| Failed run | Status check | Reject | Quality gate |
| Missing data | Field check | Reject | Cannot record |
| Storage error | IO check | Retry | Not recorded |

### Assumptions

1. **Run complete**: All tasks finished
2. **Run passed**: Meets quality bar
3. **Data available**: Results accessible
4. **Storage writable**: Permissions OK
5. **Unique ID**: Run identifiable

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Recording | Capturing as baseline |
| Promotion | Making run a baseline |
| Capture | Saving state |
| Complete | All data present |
| Validate | Check requirements |
| Metadata | Who/when/why |
| Snapshot | Point-in-time capture |
| Environment | System context |
| Configuration | Settings state |
| Checksum | Integrity verification |

---

## Out of Scope

- Automatic recording
- Recording failed runs
- Partial recordings
- Incremental baselines
- Baseline merging
- Cross-suite baselines

---

## Functional Requirements

### FR-001 to FR-025: Recording Process

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048a-01 | Recording MUST be explicit | P0 |
| FR-048a-02 | Record from run ID MUST work | P0 |
| FR-048a-03 | Record from latest MUST work | P0 |
| FR-048a-04 | Run MUST be complete | P0 |
| FR-048a-05 | Incomplete MUST be rejected | P0 |
| FR-048a-06 | Run MUST pass quality bar | P0 |
| FR-048a-07 | Quality bar MUST be configurable | P0 |
| FR-048a-08 | Default bar: pass rate >= 80% | P0 |
| FR-048a-09 | Failed run MUST warn | P0 |
| FR-048a-10 | Force record MUST be option | P1 |
| FR-048a-11 | Force MUST require reason | P1 |
| FR-048a-12 | Record MUST capture summary | P0 |
| FR-048a-13 | Record MUST capture per-task | P0 |
| FR-048a-14 | Record MUST capture environment | P0 |
| FR-048a-15 | Record MUST capture config | P0 |
| FR-048a-16 | Record MUST capture suite info | P0 |
| FR-048a-17 | Record MUST capture model info | P0 |
| FR-048a-18 | Record MUST have timestamp | P0 |
| FR-048a-19 | Record MUST have reason | P0 |
| FR-048a-20 | Record MUST have author | P1 |
| FR-048a-21 | Author from git MUST work | P1 |
| FR-048a-22 | Author from env MUST work | P1 |
| FR-048a-23 | Author from CLI MUST work | P1 |
| FR-048a-24 | Validation MUST occur | P0 |
| FR-048a-25 | Invalid MUST reject | P0 |

### FR-026 to FR-040: Data Capture

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048a-26 | Summary metrics MUST capture | P0 |
| FR-048a-27 | Pass rate MUST capture | P0 |
| FR-048a-28 | Total runtime MUST capture | P0 |
| FR-048a-29 | Average runtime MUST capture | P0 |
| FR-048a-30 | Score MUST capture | P0 |
| FR-048a-31 | Per-task status MUST capture | P0 |
| FR-048a-32 | Per-task runtime MUST capture | P0 |
| FR-048a-33 | Per-task iterations MUST capture | P0 |
| FR-048a-34 | Per-task tokens MUST capture | P0 |
| FR-048a-35 | Environment OS MUST capture | P0 |
| FR-048a-36 | Environment runtime MUST capture | P0 |
| FR-048a-37 | Model name MUST capture | P0 |
| FR-048a-38 | Model version MUST capture | P0 |
| FR-048a-39 | Suite ID MUST capture | P0 |
| FR-048a-40 | Suite version MUST capture | P0 |

### FR-041 to FR-055: Storage

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048a-41 | Storage MUST occur | P0 |
| FR-048a-42 | Storage format: JSON | P0 |
| FR-048a-43 | Baseline ID MUST be generated | P0 |
| FR-048a-44 | ID format: baseline-{date}-{seq} | P0 |
| FR-048a-45 | Version MUST be set | P0 |
| FR-048a-46 | Checksum MUST be calculated | P0 |
| FR-048a-47 | Checksum algorithm: SHA-256 | P0 |
| FR-048a-48 | Atomic write MUST occur | P0 |
| FR-048a-49 | Backup previous MUST occur | P0 |
| FR-048a-50 | Active marker MUST update | P0 |
| FR-048a-51 | Archive previous MUST occur | P0 |
| FR-048a-52 | Storage path MUST be used | P0 |
| FR-048a-53 | Write error MUST handle | P0 |
| FR-048a-54 | Rollback on error MUST work | P0 |
| FR-048a-55 | Confirmation MUST be shown | P0 |

### FR-056 to FR-065: CLI

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048a-56 | CLI command MUST exist | P0 |
| FR-048a-57 | `acode baseline create` MUST work | P0 |
| FR-048a-58 | `--run <id>` MUST work | P0 |
| FR-048a-59 | `--reason <text>` MUST work | P0 |
| FR-048a-60 | `--force` MUST work | P1 |
| FR-048a-61 | `--author <name>` MUST work | P1 |
| FR-048a-62 | Interactive reason MUST work | P0 |
| FR-048a-63 | Confirmation prompt MUST work | P0 |
| FR-048a-64 | `--yes` MUST skip confirmation | P0 |
| FR-048a-65 | Success message MUST show | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-048a-01 | Run loading | <200ms | P0 |
| NFR-048a-02 | Validation | <100ms | P0 |
| NFR-048a-03 | Data capture | <100ms | P0 |
| NFR-048a-04 | Checksum calc | <50ms | P0 |
| NFR-048a-05 | Storage write | <200ms | P0 |
| NFR-048a-06 | Backup | <100ms | P0 |
| NFR-048a-07 | Total recording | <1s | P0 |
| NFR-048a-08 | Memory usage | <50MB | P0 |
| NFR-048a-09 | Baseline size | <1MB | P0 |
| NFR-048a-10 | Archive size | <10MB | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-048a-11 | Data integrity | 100% | P0 |
| NFR-048a-12 | Capture completeness | 100% | P0 |
| NFR-048a-13 | Atomic write | Always | P0 |
| NFR-048a-14 | Rollback reliability | 100% | P0 |
| NFR-048a-15 | Cross-platform | All OS | P0 |
| NFR-048a-16 | Validation accuracy | 100% | P0 |
| NFR-048a-17 | Checksum accuracy | 100% | P0 |
| NFR-048a-18 | Error recovery | Graceful | P0 |
| NFR-048a-19 | Backup reliability | 100% | P0 |
| NFR-048a-20 | ID uniqueness | 100% | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-048a-21 | Recording logged | Info | P0 |
| NFR-048a-22 | Validation logged | Debug | P0 |
| NFR-048a-23 | Storage logged | Info | P0 |
| NFR-048a-24 | Errors logged | Error | P0 |
| NFR-048a-25 | Author logged | Info | P0 |
| NFR-048a-26 | Reason logged | Info | P0 |
| NFR-048a-27 | Structured logging | JSON | P0 |
| NFR-048a-28 | Metrics: recordings | Counter | P1 |
| NFR-048a-29 | Trace ID | Included | P1 |
| NFR-048a-30 | Audit trail | Complete | P0 |

---

## Acceptance Criteria / Definition of Done

### Process
- [ ] AC-001: Explicit recording
- [ ] AC-002: From run ID
- [ ] AC-003: From latest
- [ ] AC-004: Complete check
- [ ] AC-005: Quality bar
- [ ] AC-006: Reason required
- [ ] AC-007: Validation
- [ ] AC-008: Force option

### Capture
- [ ] AC-009: Summary
- [ ] AC-010: Per-task
- [ ] AC-011: Environment
- [ ] AC-012: Config
- [ ] AC-013: Model info
- [ ] AC-014: Suite info
- [ ] AC-015: Timestamp
- [ ] AC-016: Author

### Storage
- [ ] AC-017: JSON format
- [ ] AC-018: ID generated
- [ ] AC-019: Checksum
- [ ] AC-020: Atomic write
- [ ] AC-021: Backup
- [ ] AC-022: Archive
- [ ] AC-023: Active marker
- [ ] AC-024: Error handling

### CLI
- [ ] AC-025: Command works
- [ ] AC-026: Options work
- [ ] AC-027: Confirmation
- [ ] AC-028: Success message
- [ ] AC-029: Tests pass
- [ ] AC-030: Documented
- [ ] AC-031: Cross-platform
- [ ] AC-032: Reviewed

---

## User Verification Scenarios

### Scenario 1: Record from Run
**Persona:** Tech Lead  
**Preconditions:** Successful run exists  
**Steps:**
1. Identify run ID
2. Run create command
3. Provide reason
4. Baseline created

**Verification Checklist:**
- [ ] Run found
- [ ] Reason captured
- [ ] Baseline created
- [ ] Active set

### Scenario 2: Record Latest
**Persona:** Tech Lead  
**Preconditions:** Recent run passed  
**Steps:**
1. Run create --latest
2. Confirm
3. Provide reason
4. Baseline created

**Verification Checklist:**
- [ ] Latest found
- [ ] Confirmation works
- [ ] Baseline created
- [ ] Logged

### Scenario 3: Quality Bar Rejection
**Persona:** Developer  
**Preconditions:** Run below 80%  
**Steps:**
1. Try to record
2. Quality check fails
3. Rejection shown
4. Force option available

**Verification Checklist:**
- [ ] Quality checked
- [ ] Rejection clear
- [ ] Force offered
- [ ] Logged

### Scenario 4: Force Record
**Persona:** Tech Lead  
**Preconditions:** Exception needed  
**Steps:**
1. Use --force
2. Provide reason
3. Warning shown
4. Baseline created

**Verification Checklist:**
- [ ] Force works
- [ ] Reason required
- [ ] Warning shown
- [ ] Audit logged

### Scenario 5: View Captured Data
**Persona:** Developer  
**Preconditions:** Baseline exists  
**Steps:**
1. Open baseline file
2. See all metrics
3. See environment
4. Verify complete

**Verification Checklist:**
- [ ] All metrics
- [ ] Environment
- [ ] Model info
- [ ] Complete

### Scenario 6: Storage Error
**Persona:** Developer  
**Preconditions:** Permission issue  
**Steps:**
1. Try to record
2. Write fails
3. Error shown
4. Rollback occurs

**Verification Checklist:**
- [ ] Error detected
- [ ] Message clear
- [ ] Rollback works
- [ ] No corruption

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-048a-01 | Run loading | FR-048a-02 |
| UT-048a-02 | Completeness check | FR-048a-04 |
| UT-048a-03 | Quality bar | FR-048a-06 |
| UT-048a-04 | Summary capture | FR-048a-26 |
| UT-048a-05 | Per-task capture | FR-048a-31 |
| UT-048a-06 | Environment capture | FR-048a-35 |
| UT-048a-07 | ID generation | FR-048a-43 |
| UT-048a-08 | Checksum calc | FR-048a-46 |
| UT-048a-09 | JSON serialization | FR-048a-42 |
| UT-048a-10 | Validation | FR-048a-24 |
| UT-048a-11 | Reason requirement | FR-048a-19 |
| UT-048a-12 | Force option | FR-048a-10 |
| UT-048a-13 | Author detection | FR-048a-21 |
| UT-048a-14 | Backup logic | FR-048a-49 |
| UT-048a-15 | Active marker | FR-048a-50 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-048a-01 | Full recording E2E | E2E |
| IT-048a-02 | Results integration | Task 046.c |
| IT-048a-03 | Baseline integration | Task 048 |
| IT-048a-04 | CLI integration | FR-048a-56 |
| IT-048a-05 | Cross-platform | NFR-048a-15 |
| IT-048a-06 | Atomic write | NFR-048a-13 |
| IT-048a-07 | Rollback | FR-048a-54 |
| IT-048a-08 | Logging | NFR-048a-21 |
| IT-048a-09 | Git author | FR-048a-21 |
| IT-048a-10 | Large run | NFR-048a-09 |
| IT-048a-11 | Confirmation | FR-048a-63 |
| IT-048a-12 | Yes flag | FR-048a-64 |
| IT-048a-13 | Quality rejection | FR-048a-06 |
| IT-048a-14 | Archive | FR-048a-51 |
| IT-048a-15 | Audit trail | NFR-048a-30 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Baselines/
│       ├── BaselineRecord.cs
│       ├── RecordingValidation.cs
│       └── QualityBar.cs
├── Acode.Application/
│   └── Baselines/
│       ├── IBaselineRecorder.cs
│       └── RecordingOptions.cs
├── Acode.Infrastructure/
│   └── Baselines/
│       ├── BaselineRecorder.cs
│       ├── RunCapturer.cs
│       ├── AuthorResolver.cs
│       └── ChecksumCalculator.cs
├── Acode.Cli/
│   └── Commands/
│       └── Baseline/
│           ├── BaselineCreateCommand.cs
│           └── CreateOptions.cs
```

### Recording Flow

```
1. Load run by ID or latest
   ↓
2. Validate run complete
   ↓
3. Check quality bar (pass rate >= 80%)
   ↓
4. Prompt for reason (if interactive)
   ↓
5. Capture summary metrics
   ↓
6. Capture per-task results
   ↓
7. Capture environment & config
   ↓
8. Generate baseline ID
   ↓
9. Calculate checksum
   ↓
10. Backup current active
    ↓
11. Write new baseline
    ↓
12. Update active marker
    ↓
13. Confirm success
```

### CLI Example

```bash
# Record specific run
acode baseline create --run run-2025-01-15-001 --reason "v1.0 release baseline"

# Record latest passing run
acode baseline create --latest --reason "Post-fix baseline"

# Force record below quality bar
acode baseline create --run run-2025-01-15-002 --force --reason "Emergency baseline"

# Non-interactive mode
acode baseline create --run run-2025-01-15-001 --reason "CI baseline" --yes
```

**End of Task 048.a Specification**
