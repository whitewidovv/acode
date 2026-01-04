# Task 039.c: Verify Export Contains No Raw Secrets

**Priority:** P0 – Critical  
**Tier:** L – Feature Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 9 – Safety & Compliance  
**Dependencies:** Task 039.b, Task 038, Task 050  

---

## Description

Task 039.c implements the export verification system that ensures no raw secrets exist in export bundles before they are finalized. This is a critical safety gate that provides a final check before audit data leaves the system.

**Update:** Secret verification MUST scan BOTH:
- The export bundle contents (events, artifacts, metadata)
- The DB-derived metadata snapshots included in the bundle

Even though events and artifacts should already be redacted (via Task 038), this verification provides defense in depth. If any secret is detected, the export is blocked and the user must address the issue.

### Business Value

Export verification provides:
- Final safety gate before distribution
- Defense in depth against leaks
- Confidence in export safety
- Compliance assurance
- Clear remediation path

### Scope Boundaries

This task covers export verification only. Core audit is 039. Recording is 039.a. Bundle format is 039.b.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Bundle Generator | Task 039.b | Pre-finalize | Before write |
| Secret Scanner | Task 038 | Scan logic | Pattern detection |
| Pattern Provider | Task 038.c | Active patterns | Full set |
| Workspace DB | Task 050 | Metadata snapshots | Included data |
| CLI | Task 000 | Error display | User feedback |
| Audit | Task 039 | Log verification | Trail |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Secret found | Detection | Block export | Must fix |
| Scanner error | Exception | Block export | Error shown |
| Timeout | Watchdog | Block export | Must retry |
| Memory issue | Monitor | Stream scan | Slower |
| Pattern miss | Corpus fail | Update pattern | Risk |
| Verification incomplete | Coverage check | Block | Must complete |
| Report generation fail | Exception | Log only | Warning |
| Remediation unclear | User report | Improve message | Feedback |

### Assumptions

1. **Pre-finalize check**: Before bundle written
2. **All content scanned**: No exceptions
3. **Same patterns as 038**: Consistency
4. **Block is absolute**: No bypass
5. **Report generated**: Clear findings
6. **Location identified**: Where in bundle
7. **Performance acceptable**: <1min for typical
8. **Streaming for large**: Memory bounded

### Security Considerations

1. **Non-bypassable**: No skip option
2. **All content scanned**: Complete coverage
3. **Same patterns**: Consistency with 038
4. **Block on detection**: No exceptions
5. **Clear reporting**: What was found
6. **No secret in report**: Location only
7. **Audit verification**: Log the check
8. **Defense in depth**: Last line

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Export Verification | Pre-finalize secret scan |
| Defense in Depth | Multiple safety layers |
| Verification Report | Findings documentation |
| Block | Prevent export completion |
| Location | Where secret was found |
| Coverage | Percentage scanned |
| Remediation | How to fix |
| False Positive | Non-secret flagged |

---

## Out of Scope

- Automatic remediation
- Secret removal
- Pattern updates from findings
- Historical export scanning
- Remote bundle verification

---

## Functional Requirements

### FR-001 to FR-015: Verification Execution

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039C-01 | Verification MUST run before bundle finalize | P0 |
| FR-039C-02 | All event files MUST be scanned | P0 |
| FR-039C-03 | All artifact files MUST be scanned | P0 |
| FR-039C-04 | All metadata files MUST be scanned | P0 |
| FR-039C-05 | Manifest MUST be scanned | P0 |
| FR-039C-06 | README MUST be scanned | P0 |
| FR-039C-07 | Streaming scan MUST be supported | P1 |
| FR-039C-08 | Memory MUST be bounded | P0 |
| FR-039C-09 | Timeout MUST be configurable | P1 |
| FR-039C-10 | Timeout MUST block export | P0 |
| FR-039C-11 | Same patterns as Task 038 | P0 |
| FR-039C-12 | Custom patterns MUST be included | P0 |
| FR-039C-13 | Entropy analysis MUST be included | P1 |
| FR-039C-14 | Coverage MUST be tracked | P0 |
| FR-039C-15 | Incomplete scan MUST block | P0 |

### FR-016 to FR-030: Detection and Blocking

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039C-16 | Secret detected MUST block export | P0 |
| FR-039C-17 | Block MUST be non-bypassable | P0 |
| FR-039C-18 | No --force or --skip option | P0 |
| FR-039C-19 | All findings MUST be reported | P0 |
| FR-039C-20 | File path MUST be in report | P0 |
| FR-039C-21 | Line/offset MUST be in report | P1 |
| FR-039C-22 | Secret type MUST be in report | P0 |
| FR-039C-23 | Secret value MUST NOT be in report | P0 |
| FR-039C-24 | Pattern name MUST be in report | P1 |
| FR-039C-25 | Multiple findings MUST all report | P0 |
| FR-039C-26 | Summary count MUST be shown | P0 |
| FR-039C-27 | Exit code 10 on secret | P0 |
| FR-039C-28 | Error message MUST be clear | P0 |
| FR-039C-29 | Remediation steps MUST be suggested | P1 |
| FR-039C-30 | Re-run guidance MUST be shown | P1 |

### FR-031 to FR-045: Verification Report

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039C-31 | Report MUST be generated | P0 |
| FR-039C-32 | Report MUST be JSON format | P0 |
| FR-039C-33 | Report MUST include timestamp | P0 |
| FR-039C-34 | Report MUST include status | P0 |
| FR-039C-35 | Report MUST include coverage | P0 |
| FR-039C-36 | Report MUST include findings | P0 |
| FR-039C-37 | Report MUST include patterns used | P1 |
| FR-039C-38 | Report MUST include duration | P1 |
| FR-039C-39 | Report MUST be included in bundle if pass | P1 |
| FR-039C-40 | Report MUST be standalone on fail | P0 |
| FR-039C-41 | Report file name MUST be clear | P0 |
| FR-039C-42 | Report MUST be human-readable | P0 |
| FR-039C-43 | CLI MUST display report summary | P0 |
| FR-039C-44 | `--json` output MUST include report | P1 |
| FR-039C-45 | Report MUST be audited | P0 |

### FR-046 to FR-055: Audit Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039C-46 | Verification event MUST be logged | P0 |
| FR-039C-47 | Log MUST include bundle ID | P0 |
| FR-039C-48 | Log MUST include status | P0 |
| FR-039C-49 | Log MUST include finding count | P0 |
| FR-039C-50 | Log MUST include duration | P1 |
| FR-039C-51 | Log MUST NOT include secret values | P0 |
| FR-039C-52 | Structured log format | P0 |
| FR-039C-53 | Events published | P1 |
| FR-039C-54 | Metrics tracked | P2 |
| FR-039C-55 | DB event recorded | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-039C-01 | Small bundle verify | <10s | P1 |
| NFR-039C-02 | Medium bundle verify | <30s | P1 |
| NFR-039C-03 | Large bundle verify | <2min | P2 |
| NFR-039C-04 | Memory bounded | <200MB | P0 |
| NFR-039C-05 | Streaming threshold | >100MB | P1 |
| NFR-039C-06 | Pattern match speed | 10MB/s | P1 |
| NFR-039C-07 | Report generation | <1s | P1 |
| NFR-039C-08 | Concurrent files | Supported | P2 |
| NFR-039C-09 | Progress update | 1Hz | P2 |
| NFR-039C-10 | Timeout default | 5min | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-039C-11 | Complete scan | 100% | P0 |
| NFR-039C-12 | No bypass | 100% | P0 |
| NFR-039C-13 | Detection rate | >99.9% | P0 |
| NFR-039C-14 | Block on detection | 100% | P0 |
| NFR-039C-15 | Report accuracy | 100% | P0 |
| NFR-039C-16 | Graceful on error | Block | P0 |
| NFR-039C-17 | Thread safety | No races | P0 |
| NFR-039C-18 | Cross-platform | All OS | P0 |
| NFR-039C-19 | Encoding support | UTF-8 | P0 |
| NFR-039C-20 | Deterministic | Same result | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-039C-21 | Verification logged | Info | P0 |
| NFR-039C-22 | Finding logged | Warning | P0 |
| NFR-039C-23 | Block logged | Warning | P0 |
| NFR-039C-24 | Error logged | Error | P0 |
| NFR-039C-25 | Metrics: verifications | Counter | P2 |
| NFR-039C-26 | Metrics: blocks | Counter | P2 |
| NFR-039C-27 | Events published | EventBus | P1 |
| NFR-039C-28 | Structured logging | JSON | P0 |
| NFR-039C-29 | Progress tracking | Observable | P1 |
| NFR-039C-30 | Dashboard data | Exported | P2 |

---

## Acceptance Criteria / Definition of Done

### Verification
- [ ] AC-001: Pre-finalize check
- [ ] AC-002: All events scanned
- [ ] AC-003: All artifacts scanned
- [ ] AC-004: All metadata scanned
- [ ] AC-005: Manifest scanned
- [ ] AC-006: Streaming works
- [ ] AC-007: Memory bounded
- [ ] AC-008: Same patterns as 038

### Detection
- [ ] AC-009: Secret blocks export
- [ ] AC-010: Non-bypassable
- [ ] AC-011: No skip option
- [ ] AC-012: All findings reported
- [ ] AC-013: File path shown
- [ ] AC-014: Type shown
- [ ] AC-015: Value never shown
- [ ] AC-016: Exit code correct

### Report
- [ ] AC-017: Report generated
- [ ] AC-018: JSON format
- [ ] AC-019: Status included
- [ ] AC-020: Coverage included
- [ ] AC-021: Findings included
- [ ] AC-022: Human-readable
- [ ] AC-023: CLI displays summary
- [ ] AC-024: In bundle if pass

### Audit
- [ ] AC-025: Event logged
- [ ] AC-026: Bundle ID included
- [ ] AC-027: Status included
- [ ] AC-028: Count included
- [ ] AC-029: No values
- [ ] AC-030: Structured
- [ ] AC-031: DB recorded
- [ ] AC-032: Events published

---

## User Verification Scenarios

### Scenario 1: Clean Export
**Persona:** Developer exporting  
**Preconditions:** All redacted  
**Steps:**
1. Generate export
2. Verification runs
3. No secrets found
4. Export completes

**Verification Checklist:**
- [ ] Scan runs
- [ ] All scanned
- [ ] Status pass
- [ ] Bundle created

### Scenario 2: Secret Found
**Persona:** Developer with leak  
**Preconditions:** Secret in event  
**Steps:**
1. Attempt export
2. Verification runs
3. Secret detected
4. Export blocked

**Verification Checklist:**
- [ ] Scan runs
- [ ] Secret found
- [ ] Export blocked
- [ ] Location shown

### Scenario 3: Multiple Secrets
**Persona:** Developer with issues  
**Preconditions:** 3 secrets  
**Steps:**
1. Attempt export
2. All found
3. All reported
4. Summary shown

**Verification Checklist:**
- [ ] All detected
- [ ] All in report
- [ ] Count correct
- [ ] Remediation suggested

### Scenario 4: Large Bundle
**Persona:** Admin with big export  
**Preconditions:** 10k events  
**Steps:**
1. Generate large export
2. Streaming verification
3. Progress shown
4. Complete scan

**Verification Checklist:**
- [ ] Streaming active
- [ ] Memory bounded
- [ ] Progress updates
- [ ] Complete coverage

### Scenario 5: View Report
**Persona:** Developer checking  
**Preconditions:** Verification complete  
**Steps:**
1. Check report file
2. See status
3. See coverage
4. See any findings

**Verification Checklist:**
- [ ] Report exists
- [ ] Status clear
- [ ] Coverage shown
- [ ] Findings listed

### Scenario 6: Remediation
**Persona:** Developer fixing  
**Preconditions:** Secret found  
**Steps:**
1. See error
2. See location
3. Follow guidance
4. Re-run export

**Verification Checklist:**
- [ ] Location clear
- [ ] Guidance helpful
- [ ] Fix possible
- [ ] Re-run succeeds

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-039C-01 | Pre-finalize hook | FR-039C-01 |
| UT-039C-02 | Event scan | FR-039C-02 |
| UT-039C-03 | Artifact scan | FR-039C-03 |
| UT-039C-04 | Metadata scan | FR-039C-04 |
| UT-039C-05 | Detection blocks | FR-039C-16 |
| UT-039C-06 | No bypass | FR-039C-17 |
| UT-039C-07 | Report generation | FR-039C-31 |
| UT-039C-08 | Location in report | FR-039C-20 |
| UT-039C-09 | No value in report | FR-039C-23 |
| UT-039C-10 | Exit code | FR-039C-27 |
| UT-039C-11 | Coverage tracking | FR-039C-14 |
| UT-039C-12 | Streaming | FR-039C-07 |
| UT-039C-13 | Memory bounded | FR-039C-08 |
| UT-039C-14 | Audit logging | FR-039C-46 |
| UT-039C-15 | Thread safety | NFR-039C-17 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-039C-01 | Full verification flow | E2E |
| IT-039C-02 | Bundle generator integration | Task 039.b |
| IT-039C-03 | Pattern provider integration | Task 038.c |
| IT-039C-04 | Secret scanner integration | Task 038 |
| IT-039C-05 | Large bundle | NFR-039C-03 |
| IT-039C-06 | Streaming mode | FR-039C-07 |
| IT-039C-07 | Multiple secrets | FR-039C-25 |
| IT-039C-08 | Report in bundle | FR-039C-39 |
| IT-039C-09 | CLI output | FR-039C-43 |
| IT-039C-10 | Cross-platform | NFR-039C-18 |
| IT-039C-11 | Error handling | NFR-039C-16 |
| IT-039C-12 | Timeout handling | FR-039C-10 |
| IT-039C-13 | DB event | FR-039C-55 |
| IT-039C-14 | Event publish | FR-039C-53 |
| IT-039C-15 | Deterministic | NFR-039C-20 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Audit/
│       └── Verification/
│           ├── VerificationResult.cs
│           ├── SecretFinding.cs
│           └── VerificationReport.cs
├── Acode.Application/
│   └── Audit/
│       └── Verification/
│           ├── IExportVerifier.cs
│           ├── IVerificationReporter.cs
│           └── VerificationOptions.cs
├── Acode.Infrastructure/
│   └── Audit/
│       └── Verification/
│           ├── ExportVerifier.cs
│           ├── StreamingScanner.cs
│           └── VerificationReporter.cs
```

### Verification Report Schema

```json
{
  "timestamp": "2024-01-15T10:35:00Z",
  "bundleId": "export-2024-01-15-abc123",
  "status": "blocked",
  "coverage": {
    "filesScanned": 156,
    "totalFiles": 156,
    "bytesScanned": 10485760,
    "patterns": 45
  },
  "duration": "PT25S",
  "findings": [
    {
      "file": "events/events-042.json",
      "line": 127,
      "type": "AWS_ACCESS_KEY",
      "pattern": "aws-access-key",
      "severity": "critical"
    }
  ],
  "summary": {
    "total": 1,
    "critical": 1,
    "high": 0
  },
  "remediation": [
    "Check event source for pre-persistence redaction gap",
    "Verify Task 038 is running on all tool outputs",
    "Re-record affected events with proper redaction"
  ]
}
```

### Exit Codes

```
EXIT_SUCCESS = 0           # Verification passed
EXIT_ERROR = 1             # Verification error
EXIT_SECRET_FOUND = 10     # Secret detected (blocked)
EXIT_INCOMPLETE = 11       # Coverage incomplete
EXIT_TIMEOUT = 12          # Timeout reached
```

**End of Task 039.c Specification**
