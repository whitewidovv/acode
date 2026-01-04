# Task 039: Audit Trail + Export

**Priority:** P0 – Critical  
**Tier:** X – Cross-Cutting  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 9 – Safety & Compliance  
**Dependencies:** Task 050, Task 011, Task 018, Task 038, Task 021.c, Task 049.e  

---

## Description

Task 039 implements the comprehensive audit trail and export system. Every agent action, tool call, command execution, file modification, and model interaction is recorded as a structured audit event. This provides complete traceability and accountability for all agent operations.

**Update (DB-backed audit):** Audit records MUST be written to the Workspace DB (Task 050) as structured events. Exports MUST pull audit evidence from DB + artifact store. Redaction MUST occur *before* persistence to remote Postgres and *before* writing export bundles.

The audit system captures: what happened, when, why (task context), who initiated (user or agent), and what the outcome was. Events are immutable once written. The system supports export to portable bundles for sharing, compliance, and debugging.

### Business Value

Audit trail provides:
- Complete operation history
- Compliance evidence
- Debugging capability
- Accountability record
- Export for sharing/review

### Scope Boundaries

This task covers the core audit engine and export system. Recording specific events is 039.a. Bundle format is 039.b. Secret verification is 039.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Workspace DB | Task 050 | Event storage | Primary store |
| Artifact Store | Task 021.c | Large blobs | Diffs, outputs |
| Remote Sync | Task 049.e | Redacted events | Before sync |
| Secret Scanner | Task 038 | Pre-persistence | Redaction |
| Tool Executor | Task 020 | Tool events | Record calls |
| Git Operations | Task 018 | Git events | Record ops |
| Model Adapter | Task 044 | Model events | Record prompts |
| CLI | Task 000 | Export command | User access |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| DB write fail | Exception | Retry + fallback | Warn |
| Event lost | Sequence check | Alert | Gap in trail |
| Export fail | Exception | Partial + error | Retry |
| Secret in export | Verification | Block export | Must fix |
| Disk full | Monitor | Warn + rotate | Old events purged |
| Sync fail | Retry logic | Queue | Eventual |
| Large event | Size check | Artifact store | Split |
| Schema migration | Version check | Migrate | Seamless |

### Assumptions

1. **Workspace DB available**: Via Task 050
2. **Events are immutable**: No edit after write
3. **Events are ordered**: Sequence maintained
4. **Redaction before persist**: No raw secrets
5. **Export is portable**: Standard format
6. **Retention configurable**: Per policy
7. **Performance acceptable**: <50ms per event
8. **Query capability**: Filter, search

### Security Considerations

1. **No raw secrets**: Redacted before write
2. **Immutable events**: Cannot tamper
3. **Signed exports**: Integrity verification
4. **Access control**: Who can export
5. **Retention policy**: Secure deletion
6. **Audit of audit**: Track access
7. **Encryption at rest**: Optional
8. **Secure transport**: For sync
9. **PII handling**: Configurable
10. **Compliance modes**: GDPR, etc.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Audit Event | Single recorded operation |
| Event ID | Unique identifier |
| Event Sequence | Order number |
| Artifact | Large blob (diff, output) |
| Export Bundle | Portable archive |
| Retention | How long to keep |
| Immutable | Cannot change |
| Pre-persistence Redaction | Clean before write |
| Event Schema | Structure definition |
| Audit Trail | Complete history |

---

## Out of Scope

- Real-time audit streaming
- Audit analytics
- Audit alerting
- External SIEM integration
- Audit visualization UI
- Audit-based billing

---

## Functional Requirements

### FR-001 to FR-020: Event Recording

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039-01 | All events MUST be written to Workspace DB | P0 |
| FR-039-02 | Events MUST have unique ID | P0 |
| FR-039-03 | Events MUST have sequence number | P0 |
| FR-039-04 | Events MUST have timestamp | P0 |
| FR-039-05 | Events MUST have event type | P0 |
| FR-039-06 | Events MUST have task context | P0 |
| FR-039-07 | Events MUST have initiator | P0 |
| FR-039-08 | Events MUST have outcome | P0 |
| FR-039-09 | Events MUST be immutable | P0 |
| FR-039-10 | Events MUST be ordered | P0 |
| FR-039-11 | Events MUST be queryable | P0 |
| FR-039-12 | Event schema MUST be versioned | P0 |
| FR-039-13 | Schema migration MUST work | P0 |
| FR-039-14 | Large data MUST use artifact store | P0 |
| FR-039-15 | Artifact reference MUST be in event | P0 |
| FR-039-16 | Events MUST be redacted before write | P0 |
| FR-039-17 | Redaction MUST use Task 038 | P0 |
| FR-039-18 | Failed write MUST retry | P0 |
| FR-039-19 | Retry exhausted MUST fallback | P0 |
| FR-039-20 | Fallback MUST log locally | P0 |

### FR-021 to FR-040: Event Types

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039-21 | Tool call events MUST be recorded | P0 |
| FR-039-22 | Command execution MUST be recorded | P0 |
| FR-039-23 | File modification MUST be recorded | P0 |
| FR-039-24 | Git operations MUST be recorded | P0 |
| FR-039-25 | Model prompts MUST be recorded | P0 |
| FR-039-26 | Model responses MUST be recorded | P0 |
| FR-039-27 | Policy decisions MUST be recorded | P0 |
| FR-039-28 | Approval events MUST be recorded | P0 |
| FR-039-29 | Error events MUST be recorded | P0 |
| FR-039-30 | Task lifecycle MUST be recorded | P0 |
| FR-039-31 | Session events MUST be recorded | P0 |
| FR-039-32 | Config changes MUST be recorded | P1 |
| FR-039-33 | Secret redactions MUST be recorded | P0 |
| FR-039-34 | Block events MUST be recorded | P0 |
| FR-039-35 | Export events MUST be recorded | P0 |
| FR-039-36 | Sync events MUST be recorded | P1 |
| FR-039-37 | Custom events MUST be supported | P2 |
| FR-039-38 | Event metadata MUST be extensible | P1 |
| FR-039-39 | Event correlation ID MUST work | P0 |
| FR-039-40 | Parent-child events MUST link | P1 |

### FR-041 to FR-055: Export System

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039-41 | Export MUST pull from DB | P0 |
| FR-039-42 | Export MUST include artifacts | P0 |
| FR-039-43 | Export format MUST be documented | P0 |
| FR-039-44 | Export MUST be verifiable | P0 |
| FR-039-45 | Export MUST be portable | P0 |
| FR-039-46 | Date range filter MUST work | P0 |
| FR-039-47 | Event type filter MUST work | P0 |
| FR-039-48 | Task filter MUST work | P0 |
| FR-039-49 | Export MUST be incremental | P1 |
| FR-039-50 | Export MUST support streaming | P1 |
| FR-039-51 | Large export MUST not OOM | P0 |
| FR-039-52 | Export checksum MUST be included | P0 |
| FR-039-53 | Export signature MUST be optional | P1 |
| FR-039-54 | Export compression MUST work | P1 |
| FR-039-55 | Export CLI MUST be simple | P0 |

### FR-056 to FR-070: Query and Retention

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039-56 | Query by time range MUST work | P0 |
| FR-039-57 | Query by event type MUST work | P0 |
| FR-039-58 | Query by task ID MUST work | P0 |
| FR-039-59 | Query by correlation ID MUST work | P0 |
| FR-039-60 | Query pagination MUST work | P0 |
| FR-039-61 | Query result limit MUST work | P0 |
| FR-039-62 | Retention policy MUST be configurable | P0 |
| FR-039-63 | Auto-purge MUST respect retention | P1 |
| FR-039-64 | Purge MUST be secure | P1 |
| FR-039-65 | Purge MUST be logged | P0 |
| FR-039-66 | Manual purge MUST be supported | P1 |
| FR-039-67 | Archive before purge MUST work | P2 |
| FR-039-68 | Retention per event type MUST work | P2 |
| FR-039-69 | Compliance retention MUST be enforced | P1 |
| FR-039-70 | Retention warnings MUST be shown | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-039-01 | Event write | <50ms | P1 |
| NFR-039-02 | Artifact write | <500ms | P2 |
| NFR-039-03 | Query (100 events) | <200ms | P1 |
| NFR-039-04 | Export (1000 events) | <10s | P1 |
| NFR-039-05 | Large export (10k) | <60s | P2 |
| NFR-039-06 | Memory per export | <200MB | P1 |
| NFR-039-07 | Concurrent writes | 100+ | P2 |
| NFR-039-08 | DB size per day | <100MB | P2 |
| NFR-039-09 | Purge speed | 1k/s | P2 |
| NFR-039-10 | Index query | <50ms | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-039-11 | Event durability | 99.99% | P0 |
| NFR-039-12 | No event loss | 100% | P0 |
| NFR-039-13 | Order preservation | 100% | P0 |
| NFR-039-14 | Immutability | 100% | P0 |
| NFR-039-15 | Export integrity | 100% | P0 |
| NFR-039-16 | Graceful on error | Always | P0 |
| NFR-039-17 | Thread safety | No races | P0 |
| NFR-039-18 | Cross-platform | All OS | P0 |
| NFR-039-19 | Schema compat | Versioned | P0 |
| NFR-039-20 | Recovery on crash | Consistent | P0 |

### Security Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-039-21 | Pre-persistence redaction | 100% | P0 |
| NFR-039-22 | No raw secrets | Never | P0 |
| NFR-039-23 | Export verified clean | 100% | P0 |
| NFR-039-24 | Immutable storage | Required | P0 |
| NFR-039-25 | Access logged | Required | P0 |
| NFR-039-26 | Encryption optional | Supported | P1 |
| NFR-039-27 | Signature optional | Supported | P1 |
| NFR-039-28 | Secure deletion | Required | P1 |
| NFR-039-29 | PII handling | Configurable | P1 |
| NFR-039-30 | Compliance modes | Supported | P2 |

---

## Acceptance Criteria / Definition of Done

### Recording
- [ ] AC-001: Events to Workspace DB
- [ ] AC-002: Unique IDs
- [ ] AC-003: Sequence numbers
- [ ] AC-004: Timestamps
- [ ] AC-005: Event types
- [ ] AC-006: Task context
- [ ] AC-007: Initiator recorded
- [ ] AC-008: Outcome recorded
- [ ] AC-009: Immutable
- [ ] AC-010: Ordered

### Event Types
- [ ] AC-011: Tool calls recorded
- [ ] AC-012: Commands recorded
- [ ] AC-013: Files recorded
- [ ] AC-014: Git recorded
- [ ] AC-015: Prompts recorded
- [ ] AC-016: Responses recorded
- [ ] AC-017: Policies recorded
- [ ] AC-018: Errors recorded

### Export
- [ ] AC-019: Pull from DB
- [ ] AC-020: Include artifacts
- [ ] AC-021: Format documented
- [ ] AC-022: Verifiable
- [ ] AC-023: Portable
- [ ] AC-024: Filters work
- [ ] AC-025: Checksum included
- [ ] AC-026: CLI simple

### Security
- [ ] AC-027: Pre-redaction
- [ ] AC-028: No raw secrets
- [ ] AC-029: Export verified
- [ ] AC-030: Immutable storage
- [ ] AC-031: Access logged
- [ ] AC-032: Secure deletion

---

## User Verification Scenarios

### Scenario 1: Record Tool Call
**Persona:** Agent executing tool  
**Preconditions:** Tool invoked  
**Steps:**
1. Tool executes
2. Event recorded
3. Check DB
4. Verify fields

**Verification Checklist:**
- [ ] Event exists
- [ ] Fields correct
- [ ] Redacted
- [ ] Queryable

### Scenario 2: Export Trail
**Persona:** Developer debugging  
**Preconditions:** Events exist  
**Steps:**
1. Run export command
2. Specify date range
3. Bundle created
4. Verify contents

**Verification Checklist:**
- [ ] Export works
- [ ] Filter applied
- [ ] Contents correct
- [ ] Checksum valid

### Scenario 3: Query Events
**Persona:** Admin investigating  
**Preconditions:** Events exist  
**Steps:**
1. Query by task ID
2. Results returned
3. Paginate through
4. Verify completeness

**Verification Checklist:**
- [ ] Query works
- [ ] Results correct
- [ ] Pagination works
- [ ] All events found

### Scenario 4: Pre-Redaction
**Persona:** System recording  
**Preconditions:** Secret in output  
**Steps:**
1. Tool returns secret
2. Event created
3. Secret redacted
4. Write to DB

**Verification Checklist:**
- [ ] Secret detected
- [ ] Redaction applied
- [ ] DB clean
- [ ] Marker in event

### Scenario 5: Retention Purge
**Persona:** Admin managing storage  
**Preconditions:** Old events  
**Steps:**
1. Check retention policy
2. Trigger purge
3. Old events removed
4. Recent preserved

**Verification Checklist:**
- [ ] Policy respected
- [ ] Old removed
- [ ] Recent kept
- [ ] Purge logged

### Scenario 6: Large Export
**Persona:** Compliance export  
**Preconditions:** 10k events  
**Steps:**
1. Export full range
2. Streaming mode
3. No OOM
4. Complete bundle

**Verification Checklist:**
- [ ] Memory bounded
- [ ] Streaming works
- [ ] Complete export
- [ ] Valid bundle

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-039-01 | Event creation | FR-039-02 |
| UT-039-02 | Sequence gen | FR-039-03 |
| UT-039-03 | Timestamp | FR-039-04 |
| UT-039-04 | Immutability | FR-039-09 |
| UT-039-05 | Artifact ref | FR-039-15 |
| UT-039-06 | Pre-redaction | FR-039-16 |
| UT-039-07 | Event types | FR-039-21-35 |
| UT-039-08 | Query filters | FR-039-56-59 |
| UT-039-09 | Pagination | FR-039-60 |
| UT-039-10 | Export format | FR-039-43 |
| UT-039-11 | Checksum | FR-039-52 |
| UT-039-12 | Retention | FR-039-62 |
| UT-039-13 | Correlation | FR-039-39 |
| UT-039-14 | Schema version | FR-039-12 |
| UT-039-15 | Thread safety | NFR-039-17 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-039-01 | Full record flow | E2E |
| IT-039-02 | DB integration | Task 050 |
| IT-039-03 | Artifact store | Task 021.c |
| IT-039-04 | Redaction | Task 038 |
| IT-039-05 | Export flow | FR-039-41 |
| IT-039-06 | Query flow | FR-039-56 |
| IT-039-07 | Retention purge | FR-039-63 |
| IT-039-08 | Large export | NFR-039-05 |
| IT-039-09 | Performance | NFR-039-01 |
| IT-039-10 | Durability | NFR-039-11 |
| IT-039-11 | Cross-platform | NFR-039-18 |
| IT-039-12 | Schema migration | FR-039-13 |
| IT-039-13 | Remote sync | Task 049.e |
| IT-039-14 | Secret verify | NFR-039-22 |
| IT-039-15 | CLI commands | FR-039-55 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Audit/
│       ├── AuditEvent.cs
│       ├── EventId.cs
│       ├── EventType.cs
│       ├── EventSequence.cs
│       └── ExportBundle.cs
├── Acode.Application/
│   └── Audit/
│       ├── IAuditRecorder.cs
│       ├── IAuditQuery.cs
│       ├── IAuditExporter.cs
│       └── IRetentionManager.cs
├── Acode.Infrastructure/
│   └── Audit/
│       ├── AuditRecorder.cs
│       ├── AuditQuery.cs
│       ├── AuditExporter.cs
│       ├── RetentionManager.cs
│       └── Db/
│           └── AuditEventRepository.cs
└── Acode.Cli/
    └── Commands/
        └── Audit/
            ├── ExportCommand.cs
            ├── QueryCommand.cs
            └── PurgeCommand.cs
```

### Event Schema

```csharp
public record AuditEvent
{
    public EventId Id { get; init; }
    public long Sequence { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public EventType Type { get; init; }
    public string TaskId { get; init; }
    public string CorrelationId { get; init; }
    public string Initiator { get; init; }
    public string Outcome { get; init; }
    public Dictionary<string, object> Metadata { get; init; }
    public string? ArtifactRef { get; init; }
    public int SchemaVersion { get; init; }
}
```

### CLI Commands

```
acode audit export --from 2024-01-01 --to 2024-01-31 -o audit-jan.zip
acode audit query --task <task-id> --limit 100
acode audit purge --older-than 90d --dry-run
acode audit stats
```

**End of Task 039 Specification**
