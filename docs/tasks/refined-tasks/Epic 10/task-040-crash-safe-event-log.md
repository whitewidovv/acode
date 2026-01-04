# Task 040: Crash-Safe Event Log

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 10 – Core Reliability  
**Dependencies:** Task 050 (Workspace DB), Task 038 (Secrets)  

---

## Description

Task 040 implements the crash-safe event log, the foundational persistence layer for agent state. Every significant operation is recorded as an immutable event. If the agent crashes, it can resume from the last persisted event, avoiding repeated work and maintaining consistency.

The event log is append-only: events are never modified or deleted during normal operation. This provides a complete, ordered history of all agent actions. The log uses SQLite with WAL mode for crash safety—even if the process terminates unexpectedly, committed events are preserved.

Events are sequenced monotonically, providing a strict total order. This enables deterministic replay and unambiguous resume point identification. The sequence generator is also crash-safe, ensuring no gaps or duplicates even across restarts.

### Business Value

Crash-safe event logging provides:
- Zero data loss on crash
- Deterministic resume point
- Complete audit trail
- Reproducibility foundation
- Debugging capability

### Scope Boundaries

This task covers core event log infrastructure. Append-only semantics are 040.a. Resume rules are 040.b. Ordering guarantees are 040.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Workspace DB | Task 050 | Storage | SQLite table |
| Audit | Task 039 | Event source | Overlap |
| Secret Scanner | Task 038 | Pre-persist | Redaction |
| Task Executor | `ITaskExecutor` | Event publish | On action |
| Resume Engine | Task 040.b | Read log | Find point |
| Replay | Task 042.c | Read log | Replay |
| CLI | Task 000 | Log query | Debug |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Disk full | IOException | Warn + pause | Cannot proceed |
| DB corruption | Integrity check | Backup restore | Data loss |
| Crash mid-write | WAL recovery | Automatic | None |
| Sequence collision | Unique constraint | Retry | None |
| Large event | Size check | Reject | Error |
| Slow write | Timer | Log warning | Performance |
| Lock contention | Timeout | Retry | Delay |
| Power loss | fsync | WAL recovery | Minimal loss |

### Assumptions

1. **SQLite WAL mode**: Crash-safe by default
2. **Local disk reliable**: SSD recommended
3. **Events are small**: <1MB typical
4. **Sequence monotonic**: Never decreasing
5. **Redaction before persist**: No raw secrets
6. **Performance acceptable**: <10ms append
7. **Concurrent reads safe**: Multiple readers
8. **Single writer**: Serialized appends

### Security Considerations

1. **Pre-persist redaction**: Always
2. **No raw secrets in events**: Verified
3. **Checksums for integrity**: Optional
4. **Audit log access**: Track reads
5. **Encryption at rest**: Optional
6. **File permissions**: Restricted
7. **Backup security**: Same as primary
8. **No remote access**: Local only

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Event Log | Append-only persistence |
| WAL | Write-Ahead Logging |
| Sequence Number | Monotonic event order |
| Append-Only | No updates or deletes |
| Crash-Safe | Survives unexpected termination |
| Event | Single recorded operation |
| Resume Point | Where to continue |
| Checkpoint | Sync to disk |

---

## Out of Scope

- Distributed event log
- Event streaming
- Remote replication
- Event compaction
- Event archival
- Event analytics

---

## Functional Requirements

### FR-001 to FR-020: Event Persistence

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040-01 | Events MUST be persisted to SQLite | P0 |
| FR-040-02 | Events MUST survive process crash | P0 |
| FR-040-03 | Events MUST survive power loss (WAL) | P0 |
| FR-040-04 | WAL mode MUST be enabled | P0 |
| FR-040-05 | Events MUST have unique ID | P0 |
| FR-040-06 | Event ID MUST be ULID or UUID | P0 |
| FR-040-07 | Events MUST have sequence number | P0 |
| FR-040-08 | Sequence MUST be monotonically increasing | P0 |
| FR-040-09 | Events MUST have timestamp | P0 |
| FR-040-10 | Events MUST have type | P0 |
| FR-040-11 | Events MUST have payload | P0 |
| FR-040-12 | Payload MUST be redacted | P0 |
| FR-040-13 | Events MUST have optional hash | P1 |
| FR-040-14 | Hash MUST use SHA-256 | P1 |
| FR-040-15 | Events MUST be immutable after write | P0 |
| FR-040-16 | No UPDATE on events table | P0 |
| FR-040-17 | No DELETE on events table (normal) | P0 |
| FR-040-18 | Append MUST be atomic | P0 |
| FR-040-19 | Append MUST return ID | P0 |
| FR-040-20 | Append MUST return sequence | P0 |

### FR-021 to FR-035: Event Reading

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040-21 | Read from sequence MUST work | P0 |
| FR-040-22 | Read range MUST work | P0 |
| FR-040-23 | Read by ID MUST work | P0 |
| FR-040-24 | Read by type MUST work | P0 |
| FR-040-25 | Read MUST be concurrent-safe | P0 |
| FR-040-26 | Read MUST not block writes | P1 |
| FR-040-27 | Pagination MUST work | P0 |
| FR-040-28 | Limit MUST be enforced | P0 |
| FR-040-29 | Streaming read MUST work | P1 |
| FR-040-30 | Last sequence MUST be queryable | P0 |
| FR-040-31 | Event count MUST be queryable | P1 |
| FR-040-32 | Read performance MUST be indexed | P0 |
| FR-040-33 | Index on sequence MUST exist | P0 |
| FR-040-34 | Index on timestamp MUST exist | P1 |
| FR-040-35 | Index on type MUST exist | P2 |

### FR-036 to FR-050: Write Safety

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040-36 | Write MUST use transactions | P0 |
| FR-040-37 | Transaction MUST be serializable | P0 |
| FR-040-38 | Concurrent writes MUST serialize | P0 |
| FR-040-39 | Write timeout MUST be configurable | P1 |
| FR-040-40 | Write retry MUST be automatic | P0 |
| FR-040-41 | Retry count MUST be limited | P0 |
| FR-040-42 | Checkpoint MUST be periodic | P1 |
| FR-040-43 | Checkpoint MUST be configurable | P2 |
| FR-040-44 | fsync MUST be used for durability | P0 |
| FR-040-45 | Batch write MUST be supported | P1 |
| FR-040-46 | Batch MUST be atomic | P1 |
| FR-040-47 | Size limit MUST be enforced | P0 |
| FR-040-48 | Size limit MUST be configurable | P1 |
| FR-040-49 | Over-size MUST reject | P0 |
| FR-040-50 | Write error MUST be logged | P0 |

### FR-051 to FR-065: Sequence Generation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040-51 | Sequence MUST be unique | P0 |
| FR-040-52 | Sequence MUST be ordered | P0 |
| FR-040-53 | Sequence MUST survive crash | P0 |
| FR-040-54 | No gaps in sequence (normal) | P0 |
| FR-040-55 | Gap detection MUST work | P0 |
| FR-040-56 | Gap recovery MUST be possible | P1 |
| FR-040-57 | Sequence MUST be 64-bit | P0 |
| FR-040-58 | Overflow handling MUST exist | P2 |
| FR-040-59 | Initial sequence MUST be 1 | P0 |
| FR-040-60 | Sequence MUST be atomic increment | P0 |
| FR-040-61 | Counter MUST be persisted | P0 |
| FR-040-62 | Counter MUST survive crash | P0 |
| FR-040-63 | Counter recovery MUST be automatic | P0 |
| FR-040-64 | Counter read MUST be fast | P0 |
| FR-040-65 | Counter write MUST be safe | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-040-01 | Single event append | <10ms | P0 |
| NFR-040-02 | Batch append (100) | <100ms | P1 |
| NFR-040-03 | Read single event | <5ms | P0 |
| NFR-040-04 | Read range (100) | <50ms | P1 |
| NFR-040-05 | Last sequence query | <1ms | P0 |
| NFR-040-06 | Checkpoint | <100ms | P1 |
| NFR-040-07 | Concurrent reads | 10+ | P1 |
| NFR-040-08 | Memory per event | <1MB | P0 |
| NFR-040-09 | DB file size | Unbounded | P2 |
| NFR-040-10 | Index overhead | <20% | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-040-11 | Crash recovery | 100% | P0 |
| NFR-040-12 | No data loss | 100% | P0 |
| NFR-040-13 | Sequence integrity | 100% | P0 |
| NFR-040-14 | Immutability | 100% | P0 |
| NFR-040-15 | Atomicity | 100% | P0 |
| NFR-040-16 | Durability | fsync | P0 |
| NFR-040-17 | Thread safety | No races | P0 |
| NFR-040-18 | Cross-platform | All OS | P0 |
| NFR-040-19 | Unicode support | Full | P0 |
| NFR-040-20 | Graceful degradation | On disk full | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-040-21 | Append logged | Debug | P1 |
| NFR-040-22 | Error logged | Error | P0 |
| NFR-040-23 | Checkpoint logged | Info | P1 |
| NFR-040-24 | Recovery logged | Info | P0 |
| NFR-040-25 | Metrics: events | Counter | P2 |
| NFR-040-26 | Metrics: size | Gauge | P2 |
| NFR-040-27 | Events published | EventBus | P1 |
| NFR-040-28 | Structured logging | JSON | P0 |
| NFR-040-29 | Health check | Query test | P1 |
| NFR-040-30 | Dashboard data | Exported | P2 |

---

## Acceptance Criteria / Definition of Done

### Persistence
- [ ] AC-001: SQLite storage works
- [ ] AC-002: WAL mode enabled
- [ ] AC-003: Crash recovery works
- [ ] AC-004: Power loss recovery
- [ ] AC-005: Unique IDs
- [ ] AC-006: Sequence numbers
- [ ] AC-007: Timestamps
- [ ] AC-008: Immutable events

### Reading
- [ ] AC-009: Read from sequence
- [ ] AC-010: Read range
- [ ] AC-011: Read by ID
- [ ] AC-012: Concurrent reads
- [ ] AC-013: Pagination
- [ ] AC-014: Last sequence query
- [ ] AC-015: Indexed queries
- [ ] AC-016: Streaming read

### Writing
- [ ] AC-017: Atomic append
- [ ] AC-018: Transactions
- [ ] AC-019: Serialized writes
- [ ] AC-020: Retry logic
- [ ] AC-021: Checkpointing
- [ ] AC-022: Size limits
- [ ] AC-023: Batch append
- [ ] AC-024: Error handling

### Sequence
- [ ] AC-025: Unique sequences
- [ ] AC-026: Ordered sequences
- [ ] AC-027: Crash-safe counter
- [ ] AC-028: No gaps
- [ ] AC-029: Gap detection
- [ ] AC-030: Atomic increment
- [ ] AC-031: 64-bit capacity
- [ ] AC-032: Auto recovery

---

## User Verification Scenarios

### Scenario 1: Normal Append
**Persona:** Agent executing task  
**Preconditions:** Log initialized  
**Steps:**
1. Execute action
2. Append event
3. Verify persisted
4. Check sequence

**Verification Checklist:**
- [ ] Event stored
- [ ] ID assigned
- [ ] Sequence correct
- [ ] Queryable

### Scenario 2: Crash Recovery
**Persona:** Agent restarting  
**Preconditions:** Crash occurred  
**Steps:**
1. Start agent
2. Recovery runs
3. Last event found
4. Resume ready

**Verification Checklist:**
- [ ] DB intact
- [ ] Events preserved
- [ ] Sequence correct
- [ ] No corruption

### Scenario 3: Concurrent Reads
**Persona:** Multiple components  
**Preconditions:** Events exist  
**Steps:**
1. Multiple readers
2. All read correctly
3. No blocking
4. Consistent data

**Verification Checklist:**
- [ ] Reads succeed
- [ ] Data consistent
- [ ] No races
- [ ] Performance ok

### Scenario 4: Batch Append
**Persona:** Agent with many events  
**Preconditions:** Batch ready  
**Steps:**
1. Batch 100 events
2. Atomic append
3. All or nothing
4. Verify all

**Verification Checklist:**
- [ ] Atomic commit
- [ ] All events stored
- [ ] Sequences correct
- [ ] Performance ok

### Scenario 5: Disk Full
**Persona:** Agent on low disk  
**Preconditions:** Disk nearly full  
**Steps:**
1. Attempt append
2. Disk full error
3. Agent pauses
4. User notified

**Verification Checklist:**
- [ ] Error detected
- [ ] Agent safe
- [ ] User warned
- [ ] No corruption

### Scenario 6: Large Event
**Persona:** Agent with big payload  
**Preconditions:** Over size limit  
**Steps:**
1. Create large event
2. Size check fails
3. Event rejected
4. Error logged

**Verification Checklist:**
- [ ] Size checked
- [ ] Rejected
- [ ] Clear error
- [ ] Alternative suggested

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-040-01 | Basic append | FR-040-01 |
| UT-040-02 | Event ID generation | FR-040-05 |
| UT-040-03 | Sequence increment | FR-040-08 |
| UT-040-04 | Immutability | FR-040-15 |
| UT-040-05 | Read by sequence | FR-040-21 |
| UT-040-06 | Read range | FR-040-22 |
| UT-040-07 | Pagination | FR-040-27 |
| UT-040-08 | Last sequence | FR-040-30 |
| UT-040-09 | Transaction | FR-040-36 |
| UT-040-10 | Size limit | FR-040-47 |
| UT-040-11 | Batch append | FR-040-45 |
| UT-040-12 | Gap detection | FR-040-55 |
| UT-040-13 | Counter persist | FR-040-61 |
| UT-040-14 | Hash compute | FR-040-13 |
| UT-040-15 | Thread safety | NFR-040-17 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-040-01 | Full write/read flow | E2E |
| IT-040-02 | Crash simulation | FR-040-02 |
| IT-040-03 | WAL recovery | FR-040-03 |
| IT-040-04 | Concurrent access | FR-040-25 |
| IT-040-05 | DB integration | Task 050 |
| IT-040-06 | Redaction | Task 038 |
| IT-040-07 | Checkpoint | FR-040-42 |
| IT-040-08 | Counter recovery | FR-040-63 |
| IT-040-09 | Performance | NFR-040-01 |
| IT-040-10 | Cross-platform | NFR-040-18 |
| IT-040-11 | Large volume | 10k events |
| IT-040-12 | Disk full handling | NFR-040-20 |
| IT-040-13 | Streaming read | FR-040-29 |
| IT-040-14 | Retry logic | FR-040-40 |
| IT-040-15 | Logging | NFR-040-21 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── EventLog/
│       ├── Event.cs
│       ├── EventId.cs
│       ├── SequenceNumber.cs
│       └── EventType.cs
├── Acode.Application/
│   └── EventLog/
│       ├── IEventLog.cs
│       ├── ISequenceGenerator.cs
│       └── IEventLogQuery.cs
├── Acode.Infrastructure/
│   └── EventLog/
│       ├── SqliteEventLog.cs
│       ├── SequenceGenerator.cs
│       └── EventLogQuery.cs
```

### Database Schema

```sql
CREATE TABLE events (
    id TEXT PRIMARY KEY,
    sequence INTEGER NOT NULL UNIQUE,
    timestamp TEXT NOT NULL,
    type TEXT NOT NULL,
    payload BLOB NOT NULL,
    hash TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_events_sequence ON events(sequence);
CREATE INDEX idx_events_timestamp ON events(timestamp);
CREATE INDEX idx_events_type ON events(type);

CREATE TABLE sequence_counter (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    last_sequence INTEGER NOT NULL DEFAULT 0
);

INSERT INTO sequence_counter (id, last_sequence) VALUES (1, 0);
```

### Key Implementation

```csharp
public class SqliteEventLog : IEventLog
{
    public async Task<(EventId, SequenceNumber)> AppendAsync(Event @event)
    {
        using var tx = await _db.BeginTransactionAsync();
        try
        {
            var seq = await _sequenceGen.NextAsync(tx);
            var id = EventId.New();
            
            await _db.ExecuteAsync(tx, @"
                INSERT INTO events (id, sequence, timestamp, type, payload, hash)
                VALUES (@id, @seq, @timestamp, @type, @payload, @hash)",
                new { id, seq, ... });
            
            await tx.CommitAsync();
            return (id, seq);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
```

**End of Task 040 Specification**
