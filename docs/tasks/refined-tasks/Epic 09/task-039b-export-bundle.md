# Task 039.b: Export Bundle

**Priority:** P1 – High  
**Tier:** L – Feature Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 9 – Safety & Compliance  
**Dependencies:** Task 039, Task 050, Task 021.c, Task 038  

---

## Description

Task 039.b implements the export bundle format and generation system. Export bundles are portable archives containing audit events, artifacts, and metadata for sharing, compliance, and debugging.

**Update:** Export bundle generation MUST assemble from:
- DB audit events + run metadata snapshots
- Filesystem artifacts (logs, patches, diffs)
- Configuration snapshots
- Verification data

Bundles are self-contained and include all information needed to understand what the agent did during a session or task. They support filtering by time range, task ID, and event type.

### Business Value

Export bundles provide:
- Portable audit evidence
- Shareable debugging info
- Compliance documentation
- Reproducibility data
- Offline analysis capability

### Scope Boundaries

This task covers bundle format and generation. Core audit is 039. Event recording is 039.a. Secret verification is 039.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Audit System | Task 039 | Event source | Pull events |
| Workspace DB | Task 050 | Query events | Primary source |
| Artifact Store | Task 021.c | Get artifacts | Include blobs |
| Secret Scanner | Task 038 | Verify clean | Pre-export |
| CLI | Task 000 | Export command | User access |
| Compression | `ICompressor` | Archive | Reduce size |
| Signing | `ISigner` | Integrity | Optional |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| DB query fail | Exception | Retry | Error shown |
| Artifact missing | NotFound | Skip + warn | Partial bundle |
| Out of memory | Monitor | Streaming | Slower |
| Disk full | IOException | Error | Must free space |
| Secret in bundle | Verification | Block | Must fix |
| Compression fail | Exception | Uncompressed | Larger file |
| Sign fail | Exception | Unsigned | Warning |
| Timeout | Watchdog | Partial | Warning |

### Assumptions

1. **Events in DB**: Via Task 050
2. **Artifacts accessible**: Via Task 021.c
3. **Format is ZIP-based**: Standard archive
4. **JSON for data**: Human-readable
5. **Streaming supported**: For large exports
6. **Verification required**: Before write
7. **Filtering works**: Time, task, type
8. **Compression optional**: User choice

### Security Considerations

1. **Pre-export verification**: No secrets
2. **Redaction check**: Already applied
3. **Signing optional**: Integrity
4. **Checksum included**: Verification
5. **Encryption optional**: At rest
6. **Access control**: Who can export
7. **Audit export event**: Log who exported
8. **No sensitive metadata**: Config redacted

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Export Bundle | Portable archive |
| Bundle Format | Structure specification |
| Manifest | Index of contents |
| Artifact | Large blob included |
| Checksum | Integrity hash |
| Signature | Cryptographic sign |
| Streaming Export | Incremental generation |
| Filter | Selection criteria |

---

## Out of Scope

- Bundle import
- Bundle diff
- Bundle merge
- Remote bundle storage
- Bundle analytics
- Bundle transformation

---

## Functional Requirements

### FR-001 to FR-015: Bundle Format

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039B-01 | Bundle MUST be ZIP format | P0 |
| FR-039B-02 | Bundle MUST include manifest | P0 |
| FR-039B-03 | Manifest MUST be JSON | P0 |
| FR-039B-04 | Manifest MUST list all files | P0 |
| FR-039B-05 | Manifest MUST include checksums | P0 |
| FR-039B-06 | Events MUST be in events/ dir | P0 |
| FR-039B-07 | Artifacts MUST be in artifacts/ dir | P0 |
| FR-039B-08 | Metadata MUST be in metadata/ dir | P0 |
| FR-039B-09 | Events MUST be JSON files | P0 |
| FR-039B-10 | One event per file MUST be optional | P1 |
| FR-039B-11 | Batched events MUST be supported | P1 |
| FR-039B-12 | Schema version MUST be included | P0 |
| FR-039B-13 | Export timestamp MUST be included | P0 |
| FR-039B-14 | Filter criteria MUST be included | P0 |
| FR-039B-15 | README MUST be included | P1 |

### FR-016 to FR-030: Bundle Generation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039B-16 | Query DB for events | P0 |
| FR-039B-17 | Include referenced artifacts | P0 |
| FR-039B-18 | Apply date filter | P0 |
| FR-039B-19 | Apply task filter | P0 |
| FR-039B-20 | Apply type filter | P0 |
| FR-039B-21 | Streaming generation | P1 |
| FR-039B-22 | Progress reporting | P1 |
| FR-039B-23 | Memory bounded | P0 |
| FR-039B-24 | Compression configurable | P1 |
| FR-039B-25 | Output path configurable | P0 |
| FR-039B-26 | Overwrite protection | P0 |
| FR-039B-27 | Checksum computed | P0 |
| FR-039B-28 | Signature optional | P1 |
| FR-039B-29 | Encryption optional | P2 |
| FR-039B-30 | Bundle size reported | P1 |

### FR-031 to FR-045: Verification

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039B-31 | Pre-export verification MUST run | P0 |
| FR-039B-32 | All events MUST be scanned | P0 |
| FR-039B-33 | All artifacts MUST be scanned | P0 |
| FR-039B-34 | Secret detected MUST block | P0 |
| FR-039B-35 | Block MUST be non-bypassable | P0 |
| FR-039B-36 | Error MUST identify location | P0 |
| FR-039B-37 | Resolution MUST be suggested | P1 |
| FR-039B-38 | Verification report MUST be included | P1 |
| FR-039B-39 | Verification timestamp MUST be included | P0 |
| FR-039B-40 | Verification method MUST be documented | P0 |
| FR-039B-41 | Partial export MUST be blocked | P0 |
| FR-039B-42 | --skip-verify MUST NOT exist | P0 |
| FR-039B-43 | Verification logged | P0 |
| FR-039B-44 | Verification metrics | P2 |
| FR-039B-45 | Verification timeout | P1 |

### FR-046 to FR-060: CLI Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039B-46 | `acode audit export` MUST work | P0 |
| FR-039B-47 | `--from` date filter | P0 |
| FR-039B-48 | `--to` date filter | P0 |
| FR-039B-49 | `--task` filter | P0 |
| FR-039B-50 | `--type` filter | P1 |
| FR-039B-51 | `-o` output path | P0 |
| FR-039B-52 | `--compress` option | P1 |
| FR-039B-53 | `--sign` option | P2 |
| FR-039B-54 | `--json` output format | P1 |
| FR-039B-55 | Progress bar | P1 |
| FR-039B-56 | Exit code 0 on success | P0 |
| FR-039B-57 | Exit code 1 on error | P0 |
| FR-039B-58 | Exit code 10 on secret found | P0 |
| FR-039B-59 | Help text complete | P0 |
| FR-039B-60 | Error messages clear | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-039B-01 | Small export (100 events) | <5s | P1 |
| NFR-039B-02 | Medium export (1k events) | <30s | P1 |
| NFR-039B-03 | Large export (10k events) | <5min | P2 |
| NFR-039B-04 | Memory bounded | <500MB | P0 |
| NFR-039B-05 | Streaming threshold | >1000 events | P1 |
| NFR-039B-06 | Compression ratio | >50% | P2 |
| NFR-039B-07 | Verification speed | 10k events/min | P1 |
| NFR-039B-08 | Artifact copy | 100MB/s | P2 |
| NFR-039B-09 | Progress update | 1Hz | P2 |
| NFR-039B-10 | Checksum compute | <1s | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-039B-11 | Bundle integrity | 100% | P0 |
| NFR-039B-12 | No data loss | 100% | P0 |
| NFR-039B-13 | Verification complete | 100% | P0 |
| NFR-039B-14 | Graceful on error | Always | P0 |
| NFR-039B-15 | Resume support | Future | P2 |
| NFR-039B-16 | Thread safety | No races | P0 |
| NFR-039B-17 | Cross-platform | All OS | P0 |
| NFR-039B-18 | Encoding support | UTF-8 | P0 |
| NFR-039B-19 | Path normalization | Cross-OS | P0 |
| NFR-039B-20 | Cleanup on fail | Always | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-039B-21 | Export start logged | Info | P0 |
| NFR-039B-22 | Export complete logged | Info | P0 |
| NFR-039B-23 | Error logged | Error | P0 |
| NFR-039B-24 | Secret found logged | Warning | P0 |
| NFR-039B-25 | Metrics: export count | Counter | P2 |
| NFR-039B-26 | Metrics: export size | Histogram | P2 |
| NFR-039B-27 | Events published | EventBus | P1 |
| NFR-039B-28 | Structured logging | JSON | P0 |
| NFR-039B-29 | Progress tracking | Observable | P1 |
| NFR-039B-30 | Export event audited | Required | P0 |

---

## Acceptance Criteria / Definition of Done

### Format
- [ ] AC-001: ZIP format
- [ ] AC-002: Manifest included
- [ ] AC-003: Manifest JSON
- [ ] AC-004: Files listed
- [ ] AC-005: Checksums included
- [ ] AC-006: Events in events/
- [ ] AC-007: Artifacts in artifacts/
- [ ] AC-008: Schema versioned

### Generation
- [ ] AC-009: DB queried
- [ ] AC-010: Artifacts included
- [ ] AC-011: Date filter works
- [ ] AC-012: Task filter works
- [ ] AC-013: Type filter works
- [ ] AC-014: Streaming works
- [ ] AC-015: Memory bounded
- [ ] AC-016: Checksum computed

### Verification
- [ ] AC-017: Pre-export runs
- [ ] AC-018: All scanned
- [ ] AC-019: Secret blocks
- [ ] AC-020: Non-bypassable
- [ ] AC-021: Location shown
- [ ] AC-022: Report included
- [ ] AC-023: Logged
- [ ] AC-024: No skip option

### CLI
- [ ] AC-025: Command works
- [ ] AC-026: Filters work
- [ ] AC-027: Output path
- [ ] AC-028: Progress shown
- [ ] AC-029: Exit codes
- [ ] AC-030: Help complete
- [ ] AC-031: Errors clear
- [ ] AC-032: JSON output

---

## User Verification Scenarios

### Scenario 1: Basic Export
**Persona:** Developer exporting trail  
**Preconditions:** Events exist  
**Steps:**
1. Run export command
2. Specify date range
3. Bundle generated
4. Verify contents

**Verification Checklist:**
- [ ] Command works
- [ ] Filter applied
- [ ] Bundle created
- [ ] Contents correct

### Scenario 2: Task-Filtered Export
**Persona:** Developer debugging task  
**Preconditions:** Task events exist  
**Steps:**
1. Run with --task filter
2. Only task events included
3. Artifacts included
4. Manifest correct

**Verification Checklist:**
- [ ] Filter works
- [ ] Only task events
- [ ] Artifacts present
- [ ] Manifest accurate

### Scenario 3: Large Export
**Persona:** Admin exporting month  
**Preconditions:** Many events  
**Steps:**
1. Export large range
2. Streaming mode active
3. Progress shown
4. Complete without OOM

**Verification Checklist:**
- [ ] Streaming works
- [ ] Progress updates
- [ ] Memory bounded
- [ ] Complete bundle

### Scenario 4: Secret Blocked
**Persona:** Developer with secret  
**Preconditions:** Secret in event  
**Steps:**
1. Attempt export
2. Verification runs
3. Secret found
4. Export blocked

**Verification Checklist:**
- [ ] Verification runs
- [ ] Secret found
- [ ] Export blocked
- [ ] Location shown

### Scenario 5: Verify Bundle
**Persona:** Recipient checking bundle  
**Preconditions:** Bundle received  
**Steps:**
1. Check manifest
2. Verify checksums
3. Read events
4. View artifacts

**Verification Checklist:**
- [ ] Manifest readable
- [ ] Checksums match
- [ ] Events accessible
- [ ] Artifacts present

### Scenario 6: Compressed Export
**Persona:** Developer saving space  
**Preconditions:** Many artifacts  
**Steps:**
1. Run with --compress
2. Compression applied
3. Size reduced
4. Still readable

**Verification Checklist:**
- [ ] Compression works
- [ ] Size smaller
- [ ] Valid ZIP
- [ ] Contents intact

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-039B-01 | Manifest generation | FR-039B-02 |
| UT-039B-02 | Event serialization | FR-039B-09 |
| UT-039B-03 | Checksum computation | FR-039B-27 |
| UT-039B-04 | Date filter | FR-039B-18 |
| UT-039B-05 | Task filter | FR-039B-19 |
| UT-039B-06 | Type filter | FR-039B-20 |
| UT-039B-07 | ZIP creation | FR-039B-01 |
| UT-039B-08 | Artifact inclusion | FR-039B-17 |
| UT-039B-09 | Verification block | FR-039B-34 |
| UT-039B-10 | Schema version | FR-039B-12 |
| UT-039B-11 | Compression | FR-039B-24 |
| UT-039B-12 | Memory limit | NFR-039B-04 |
| UT-039B-13 | Path normalization | NFR-039B-19 |
| UT-039B-14 | Exit codes | FR-039B-56-58 |
| UT-039B-15 | Thread safety | NFR-039B-16 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-039B-01 | Full export flow | E2E |
| IT-039B-02 | DB integration | Task 050 |
| IT-039B-03 | Artifact store | Task 021.c |
| IT-039B-04 | Secret scanner | Task 038 |
| IT-039B-05 | Large export | NFR-039B-03 |
| IT-039B-06 | Streaming mode | FR-039B-21 |
| IT-039B-07 | CLI commands | FR-039B-46 |
| IT-039B-08 | Filters combined | FR-039B-18-20 |
| IT-039B-09 | Verification | FR-039B-31 |
| IT-039B-10 | Cross-platform | NFR-039B-17 |
| IT-039B-11 | Progress reporting | FR-039B-22 |
| IT-039B-12 | Error handling | NFR-039B-14 |
| IT-039B-13 | Cleanup on fail | NFR-039B-20 |
| IT-039B-14 | Bundle validation | FR-039B-04 |
| IT-039B-15 | Audit event | NFR-039B-30 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Audit/
│       └── Export/
│           ├── ExportBundle.cs
│           ├── BundleManifest.cs
│           └── ExportFilter.cs
├── Acode.Application/
│   └── Audit/
│       └── Export/
│           ├── IBundleGenerator.cs
│           ├── IBundleVerifier.cs
│           └── IManifestBuilder.cs
├── Acode.Infrastructure/
│   └── Audit/
│       └── Export/
│           ├── BundleGenerator.cs
│           ├── BundleVerifier.cs
│           ├── ManifestBuilder.cs
│           └── ZipBundleWriter.cs
└── Acode.Cli/
    └── Commands/
        └── Audit/
            └── ExportCommand.cs
```

### Bundle Structure

```
audit-export-2024-01-15.zip
├── manifest.json
├── README.md
├── metadata/
│   ├── export-info.json
│   ├── filter-criteria.json
│   └── verification-report.json
├── events/
│   ├── events-001.json  (batched)
│   ├── events-002.json
│   └── ...
└── artifacts/
    ├── <artifact-id-1>.bin
    ├── <artifact-id-2>.txt
    └── ...
```

### Manifest Schema

```json
{
  "version": "1.0",
  "exportTimestamp": "2024-01-15T10:30:00Z",
  "filter": {
    "from": "2024-01-01T00:00:00Z",
    "to": "2024-01-15T00:00:00Z",
    "taskId": null,
    "eventTypes": null
  },
  "counts": {
    "events": 1234,
    "artifacts": 56
  },
  "files": [
    {
      "path": "events/events-001.json",
      "checksum": "sha256:abc123...",
      "size": 102400
    }
  ],
  "totalChecksum": "sha256:xyz789...",
  "verificationStatus": "passed"
}
```

**End of Task 039.b Specification**
