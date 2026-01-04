# Task 043.b: Attach Full Logs

**Priority:** P1 – High  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 11 – Optimization  
**Dependencies:** Task 043 (Pipeline), Task 043.a (Failures), Task 040 (Event Log)  

---

## Description

Task 043.b implements full log attachment—the mechanism that preserves complete, unmodified output alongside summaries. While summaries provide quick insight, full logs are essential for forensic analysis, debugging complex issues, and verifying that summarization didn't miss critical information.

When the summarization pipeline processes output, it simultaneously stores the complete raw output in the log store. The summary includes a reference (ID) to this full log. Users can retrieve the full log on demand, whether for investigation, sharing with team members, or detailed analysis.

### Business Value

Full log attachment provides:
- Complete audit trail
- Forensic capability
- Debug support
- Verification option
- Data preservation

### Scope Boundaries

This task covers log storage, referencing, and retrieval. Summarization is Task 043. Failure extraction is Task 043.a. Size limits are Task 043.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Pipeline | Task 043 | Store trigger | Parent |
| Event Log | Task 040 | Storage | Persistence |
| CLI | Commands | Retrieval | User interface |
| File System | Write | Overflow | Large logs |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Storage full | Disk check | Overflow | May lose old |
| Write fails | I/O error | Retry | Warning |
| Reference lost | Lookup fail | Log warning | No full log |
| Corruption | Checksum | Re-store | May lose |
| Large log | Size check | File ref | Slower retrieval |

### Assumptions

1. **Storage available**: Disk space
2. **Logs valuable**: Worth storing
3. **Retrieval needed**: User will access
4. **Format stable**: No conversion needed
5. **Retention finite**: Old logs pruned

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Full Log | Complete raw output |
| Reference | ID linking to log |
| Attachment | Log stored with summary |
| Retrieval | Getting full log back |
| Overflow | Write to file |
| Retention | How long kept |
| Pruning | Delete old logs |
| Compression | Reduce size |
| Checksum | Integrity check |
| Store | Persistence layer |

---

## Out of Scope

- Log streaming to external services
- Real-time log analysis
- Log aggregation services
- Distributed log storage
- Log search/indexing
- Log visualization

---

## Functional Requirements

### FR-001 to FR-015: Log Storage

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043b-01 | Full log MUST be stored | P0 |
| FR-043b-02 | Storage MUST be automatic | P0 |
| FR-043b-03 | Storage MUST be with summary | P0 |
| FR-043b-04 | Reference MUST be generated | P0 |
| FR-043b-05 | Reference MUST be unique | P0 |
| FR-043b-06 | Reference MUST be in summary | P0 |
| FR-043b-07 | Storage MUST be SQLite | P0 |
| FR-043b-08 | Large logs MUST use file | P0 |
| FR-043b-09 | Threshold MUST be 100KB | P0 |
| FR-043b-10 | Threshold MUST be configurable | P1 |
| FR-043b-11 | File path MUST be stored | P0 |
| FR-043b-12 | Storage MUST be async | P0 |
| FR-043b-13 | Storage MUST not block | P0 |
| FR-043b-14 | Storage failure MUST warn | P0 |
| FR-043b-15 | Storage failure MUST not fail summary | P0 |

### FR-016 to FR-035: Log Retrieval

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043b-16 | Retrieval MUST work by ID | P0 |
| FR-043b-17 | Retrieval MUST be fast | P0 |
| FR-043b-18 | Not found MUST return null | P0 |
| FR-043b-19 | Expired MUST return null | P0 |
| FR-043b-20 | Retrieval MUST handle file | P0 |
| FR-043b-21 | Retrieval MUST be async | P0 |
| FR-043b-22 | Streaming MUST be supported | P1 |
| FR-043b-23 | Large log MUST stream | P0 |
| FR-043b-24 | CLI command MUST exist | P0 |
| FR-043b-25 | CLI MUST accept ID | P0 |
| FR-043b-26 | CLI MUST output log | P0 |
| FR-043b-27 | CLI MUST support pipe | P1 |
| FR-043b-28 | CLI MUST support file output | P1 |
| FR-043b-29 | Partial retrieval MUST work | P1 |
| FR-043b-30 | Head/tail MUST work | P1 |
| FR-043b-31 | Line range MUST work | P1 |
| FR-043b-32 | Encoding MUST be preserved | P0 |
| FR-043b-33 | Binary MUST be handled | P0 |
| FR-043b-34 | Decompression MUST be automatic | P0 |
| FR-043b-35 | Checksum MUST be verified | P0 |

### FR-036 to FR-050: Retention

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043b-36 | Retention policy MUST exist | P0 |
| FR-043b-37 | Default retention MUST be 7 days | P0 |
| FR-043b-38 | Retention MUST be configurable | P0 |
| FR-043b-39 | Expired logs MUST be pruned | P0 |
| FR-043b-40 | Pruning MUST be automatic | P0 |
| FR-043b-41 | Pruning MUST run daily | P0 |
| FR-043b-42 | Pruning MUST be configurable | P1 |
| FR-043b-43 | Manual prune MUST work | P0 |
| FR-043b-44 | Prune command MUST exist | P0 |
| FR-043b-45 | Prune MUST report count | P0 |
| FR-043b-46 | Prune MUST free disk | P0 |
| FR-043b-47 | File cleanup MUST work | P0 |
| FR-043b-48 | Orphan files MUST be cleaned | P0 |
| FR-043b-49 | Size limit MUST be enforced | P1 |
| FR-043b-50 | Size limit MUST be configurable | P1 |

### FR-051 to FR-060: Compression

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-043b-51 | Compression MUST be supported | P0 |
| FR-043b-52 | Default MUST compress | P0 |
| FR-043b-53 | Compression MUST be gzip | P0 |
| FR-043b-54 | Compression MUST be optional | P1 |
| FR-043b-55 | Compression level MUST be configurable | P2 |
| FR-043b-56 | Default level MUST be 6 | P1 |
| FR-043b-57 | Metadata MUST indicate compression | P0 |
| FR-043b-58 | Decompression MUST be transparent | P0 |
| FR-043b-59 | Small logs MUST skip compression | P1 |
| FR-043b-60 | Threshold MUST be 1KB | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-043b-01 | Storage time | <50ms typical | P0 |
| NFR-043b-02 | Large storage | <500ms for 1MB | P0 |
| NFR-043b-03 | Retrieval time | <50ms typical | P0 |
| NFR-043b-04 | Large retrieval | <200ms for 1MB | P0 |
| NFR-043b-05 | Compression ratio | >2:1 typical | P1 |
| NFR-043b-06 | Streaming latency | <20ms/chunk | P1 |
| NFR-043b-07 | Pruning time | <5s/1000 logs | P1 |
| NFR-043b-08 | Memory usage | <2x log size | P0 |
| NFR-043b-09 | Concurrent ops | 10+ | P1 |
| NFR-043b-10 | Throughput | 100 ops/s | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-043b-11 | Storage success | 99.9% | P0 |
| NFR-043b-12 | Data integrity | 100% | P0 |
| NFR-043b-13 | Checksum valid | 100% | P0 |
| NFR-043b-14 | Retrieval accurate | 100% | P0 |
| NFR-043b-15 | Cross-platform | All OS | P0 |
| NFR-043b-16 | Encoding preserve | UTF-8 | P0 |
| NFR-043b-17 | Binary preserve | Exact | P0 |
| NFR-043b-18 | Crash recovery | Consistent | P0 |
| NFR-043b-19 | Concurrent safe | Yes | P0 |
| NFR-043b-20 | Transaction safe | Yes | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-043b-21 | Storage logged | Debug | P0 |
| NFR-043b-22 | Retrieval logged | Debug | P0 |
| NFR-043b-23 | Pruning logged | Info | P0 |
| NFR-043b-24 | Size logged | Debug | P0 |
| NFR-043b-25 | Metrics: count | Counter | P1 |
| NFR-043b-26 | Metrics: size | Histogram | P1 |
| NFR-043b-27 | Metrics: pruned | Counter | P1 |
| NFR-043b-28 | Structured logging | JSON | P0 |
| NFR-043b-29 | Error logged | Warning | P0 |
| NFR-043b-30 | Disk usage logged | Info | P1 |

---

## Acceptance Criteria / Definition of Done

### Storage
- [ ] AC-001: Full log stored
- [ ] AC-002: Storage automatic
- [ ] AC-003: Reference generated
- [ ] AC-004: Reference unique
- [ ] AC-005: Large uses file
- [ ] AC-006: Async works
- [ ] AC-007: Non-blocking
- [ ] AC-008: Failure warns

### Retrieval
- [ ] AC-009: By ID works
- [ ] AC-010: Fast retrieval
- [ ] AC-011: Not found handled
- [ ] AC-012: File retrieval works
- [ ] AC-013: Streaming works
- [ ] AC-014: CLI command works
- [ ] AC-015: Pipe works
- [ ] AC-016: File output works

### Retention
- [ ] AC-017: Policy works
- [ ] AC-018: Default 7 days
- [ ] AC-019: Configurable
- [ ] AC-020: Auto prune works
- [ ] AC-021: Manual prune works
- [ ] AC-022: Files cleaned
- [ ] AC-023: Disk freed
- [ ] AC-024: Orphans cleaned

### Compression
- [ ] AC-025: Compression works
- [ ] AC-026: Gzip used
- [ ] AC-027: Decompression transparent
- [ ] AC-028: Small skipped
- [ ] AC-029: Checksum valid
- [ ] AC-030: Cross-platform
- [ ] AC-031: Tests pass
- [ ] AC-032: Documented

---

## User Verification Scenarios

### Scenario 1: View Full Log
**Persona:** Developer investigating  
**Preconditions:** Summary generated  
**Steps:**
1. Note log ID
2. Run log command
3. Full log shown
4. Investigate

**Verification Checklist:**
- [ ] ID works
- [ ] Full log shown
- [ ] Encoding correct
- [ ] Complete

### Scenario 2: Large Log
**Persona:** Developer with big output  
**Preconditions:** Large log generated  
**Steps:**
1. Large output
2. Stored to file
3. Retrieve
4. Streams back

**Verification Checklist:**
- [ ] File used
- [ ] Retrieval works
- [ ] Streaming works
- [ ] Complete

### Scenario 3: Prune Old
**Persona:** Developer clearing space  
**Preconditions:** Old logs exist  
**Steps:**
1. Run prune
2. Old removed
3. Count shown
4. Space freed

**Verification Checklist:**
- [ ] Prune works
- [ ] Count accurate
- [ ] Files cleaned
- [ ] Space freed

### Scenario 4: Export Log
**Persona:** Developer sharing  
**Preconditions:** Log exists  
**Steps:**
1. Get log ID
2. Export to file
3. Share file
4. Verify complete

**Verification Checklist:**
- [ ] Export works
- [ ] File created
- [ ] Content complete
- [ ] Encoding correct

### Scenario 5: Compressed Log
**Persona:** Developer with normal log  
**Preconditions:** Log stored  
**Steps:**
1. Log stored
2. Compressed
3. Retrieve
4. Decompressed

**Verification Checklist:**
- [ ] Compression applied
- [ ] Transparent retrieval
- [ ] Content exact
- [ ] Size reduced

### Scenario 6: Expired Log
**Persona:** Developer with old reference  
**Preconditions:** Log expired  
**Steps:**
1. Old log ID
2. Attempt retrieve
3. Not found
4. Clear message

**Verification Checklist:**
- [ ] Not found
- [ ] Clear message
- [ ] No error
- [ ] Graceful

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-043b-01 | Store log | FR-043b-01 |
| UT-043b-02 | Generate reference | FR-043b-04 |
| UT-043b-03 | Large to file | FR-043b-08 |
| UT-043b-04 | Retrieve by ID | FR-043b-16 |
| UT-043b-05 | Not found | FR-043b-18 |
| UT-043b-06 | Streaming | FR-043b-22 |
| UT-043b-07 | Compression | FR-043b-51 |
| UT-043b-08 | Decompression | FR-043b-58 |
| UT-043b-09 | Checksum | FR-043b-35 |
| UT-043b-10 | Retention policy | FR-043b-36 |
| UT-043b-11 | Prune expired | FR-043b-39 |
| UT-043b-12 | File cleanup | FR-043b-47 |
| UT-043b-13 | Encoding | FR-043b-32 |
| UT-043b-14 | Binary | FR-043b-33 |
| UT-043b-15 | Concurrent | NFR-043b-19 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-043b-01 | Pipeline integration | Task 043 |
| IT-043b-02 | Event log integration | Task 040 |
| IT-043b-03 | CLI integration | FR-043b-24 |
| IT-043b-04 | Large log E2E | NFR-043b-02 |
| IT-043b-05 | Compression E2E | FR-043b-51 |
| IT-043b-06 | Prune E2E | FR-043b-39 |
| IT-043b-07 | Cross-platform | NFR-043b-15 |
| IT-043b-08 | Performance | NFR-043b-01 |
| IT-043b-09 | Concurrent | NFR-043b-09 |
| IT-043b-10 | Crash recovery | NFR-043b-18 |
| IT-043b-11 | File output | FR-043b-28 |
| IT-043b-12 | Pipe output | FR-043b-27 |
| IT-043b-13 | Partial | FR-043b-29 |
| IT-043b-14 | Head/tail | FR-043b-30 |
| IT-043b-15 | Logging | NFR-043b-21 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Summary/
│       ├── LogReference.cs
│       └── LogMetadata.cs
├── Acode.Application/
│   └── Summary/
│       ├── ILogStore.cs
│       ├── ILogRetriever.cs
│       └── RetentionPolicy.cs
├── Acode.Infrastructure/
│   └── Summary/
│       ├── LogStore.cs
│       ├── LogRetriever.cs
│       ├── LogCompressor.cs
│       └── LogPruner.cs
├── Acode.Cli/
│   └── Commands/
│       ├── LogCommand.cs
│       └── PruneLogsCommand.cs
```

### CLI Commands

```bash
# Get full log
acode log <log-id>

# Export to file
acode log <log-id> --output full.log

# Head/tail
acode log <log-id> --head 100
acode log <log-id> --tail 100

# Prune old logs
acode log prune
acode log prune --days 3
acode log prune --dry-run
```

### Storage Schema

```sql
CREATE TABLE FullLogs (
    Id TEXT PRIMARY KEY,
    SummaryId TEXT NOT NULL,
    Content BLOB,             -- Compressed, or NULL if file
    FilePath TEXT,            -- Path if large
    Checksum TEXT NOT NULL,
    Size INTEGER NOT NULL,
    Compressed INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    ExpiresAt TEXT NOT NULL,
    FOREIGN KEY (SummaryId) REFERENCES Summaries(Id)
);
```

**End of Task 043.b Specification**
