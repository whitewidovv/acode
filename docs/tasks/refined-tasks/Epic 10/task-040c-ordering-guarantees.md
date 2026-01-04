# Task 040.c: Ordering Guarantees – Sequence Numbers, Gap Detection

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 10 – Core Reliability  
**Dependencies:** Task 040 (Event Log), Task 040.a (Append-Only)  

---

## Description

Task 040.c implements ordering guarantees for the event log through monotonically increasing sequence numbers and gap detection. Every event receives a unique sequence number that establishes a strict total order across all events. This ordering is foundational to deterministic replay, correct resume, and consistent audit trails.

Sequence numbers are 64-bit integers starting from 1, incremented atomically for each event. The sequence counter is crash-safe—stored in the database and recovered automatically on restart. No two events can have the same sequence number, and sequences MUST NOT decrease.

Gap detection identifies missing sequence numbers in the event log. Gaps can occur due to rollback, corruption, or manual tampering. When a gap is detected, the system logs a warning and provides diagnostic information. Gaps in normal operation indicate a serious problem requiring investigation.

The ordering system also supports querying events in sequence order, range queries by sequence, and finding the boundary events (first/last). These capabilities enable efficient resume point identification and replay.

### Business Value

Ordering guarantees provide:
- Deterministic replay
- Unambiguous event order
- Corruption detection
- Audit trail integrity
- Debugging capability

### Scope Boundaries

This task covers sequence generation and gap detection. Event storage is Task 040. Append-only semantics are Task 040.a. Resume logic is Task 040.b.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Event Log | Task 040 | Sequence storage | Core |
| Append-Only | Task 040.a | Immutability | Guarantees |
| Resume | Task 040.b | Sequence query | Resume point |
| Replay | Task 042.c | Order guarantee | Determinism |
| Audit | Task 039 | Sequence in export | Integrity |
| CLI | Task 000 | Gap check command | Diagnostics |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Sequence collision | Unique constraint | Retry | None |
| Counter corruption | Validation | Recover from log | Warning |
| Gap detected | Scan | Log warning | Investigation |
| Overflow (64-bit) | Check | Error | System limit |
| Concurrent increment | Lock | Serialize | None |
| Counter not found | Check | Initialize | None |
| Non-monotonic | Validation | Error | Corruption |
| Missing counter table | Check | Create | None |

### Assumptions

1. **64-bit sufficient**: 9 quintillion events
2. **Atomic increment works**: SQLite transaction
3. **Counter persisted**: Same DB as events
4. **Gaps are rare**: Indicates problem
5. **Sequential access fast**: Indexed
6. **No distributed counters**: Single node
7. **Recovery deterministic**: Always same
8. **Rollback causes gaps**: By design

### Security Considerations

1. **Sequence tampering detectable**: Gap scan
2. **Counter protected**: DB access only
3. **No arbitrary sequence set**: Increment only
4. **Gaps logged**: Security alert
5. **Audit includes sequence**: Integrity
6. **No external access**: Local only
7. **Backup includes counter**: Recovery
8. **Overflow handled**: Error, not wrap

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Sequence Number | Monotonic event order |
| Gap | Missing sequence in log |
| Monotonic | Never decreasing |
| Total Order | All events comparable |
| Atomic Increment | Thread-safe +1 |
| Counter Recovery | Restore from log |
| Boundary Events | First/last in range |
| Sequence Scan | Find gaps |

---

## Out of Scope

- Distributed sequence
- Vector clocks
- Lamport timestamps
- Sequence compaction
- Sequence partitioning
- Multi-node ordering

---

## Functional Requirements

### FR-001 to FR-015: Sequence Generation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040c-01 | Sequence MUST be 64-bit integer | P0 |
| FR-040c-02 | Sequence MUST start at 1 | P0 |
| FR-040c-03 | Sequence MUST be monotonically increasing | P0 |
| FR-040c-04 | Sequence MUST be unique | P0 |
| FR-040c-05 | Increment MUST be atomic | P0 |
| FR-040c-06 | Increment MUST be transactional | P0 |
| FR-040c-07 | Counter MUST be persisted | P0 |
| FR-040c-08 | Counter MUST survive crash | P0 |
| FR-040c-09 | Counter recovery MUST be automatic | P0 |
| FR-040c-10 | Recovery MUST use max(sequence) | P0 |
| FR-040c-11 | Overflow MUST be detected | P0 |
| FR-040c-12 | Overflow MUST raise error | P0 |
| FR-040c-13 | Overflow MUST NOT wrap | P0 |
| FR-040c-14 | Concurrent requests MUST serialize | P0 |
| FR-040c-15 | Sequence MUST be returned to caller | P0 |

### FR-016 to FR-030: Gap Detection

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040c-16 | Gap detection MUST be available | P0 |
| FR-040c-17 | Gap scan MUST check full range | P0 |
| FR-040c-18 | Gap scan MUST be efficient | P0 |
| FR-040c-19 | Gap MUST return missing sequences | P0 |
| FR-040c-20 | Gap MUST be logged | P0 |
| FR-040c-21 | Gap log MUST include details | P0 |
| FR-040c-22 | Gap MUST NOT block writes | P0 |
| FR-040c-23 | Gap scan MUST be on-demand | P0 |
| FR-040c-24 | Periodic gap scan MUST be optional | P2 |
| FR-040c-25 | Gap scan MUST return count | P0 |
| FR-040c-26 | No gaps MUST return 0 | P0 |
| FR-040c-27 | Gap scan MUST be pageable | P1 |
| FR-040c-28 | Large gap MUST be summarized | P1 |
| FR-040c-29 | Gap severity MUST be categorized | P1 |
| FR-040c-30 | Critical gap MUST alert | P0 |

### FR-031 to FR-045: Sequence Queries

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040c-31 | Query by sequence MUST work | P0 |
| FR-040c-32 | Range query MUST work | P0 |
| FR-040c-33 | First event MUST be queryable | P0 |
| FR-040c-34 | Last event MUST be queryable | P0 |
| FR-040c-35 | Next after sequence MUST work | P0 |
| FR-040c-36 | Previous before sequence MUST work | P1 |
| FR-040c-37 | Count in range MUST work | P1 |
| FR-040c-38 | Sequence index MUST exist | P0 |
| FR-040c-39 | Index MUST be unique | P0 |
| FR-040c-40 | Index MUST be ascending | P0 |
| FR-040c-41 | Query performance MUST be O(log n) | P0 |
| FR-040c-42 | Range scan MUST be O(k) | P0 |
| FR-040c-43 | Current sequence MUST be queryable | P0 |
| FR-040c-44 | Next sequence MUST be predictable | P0 |
| FR-040c-45 | Sequence statistics MUST be available | P2 |

### FR-046 to FR-055: Ordering Validation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040c-46 | Ordering validation MUST be available | P0 |
| FR-040c-47 | Non-monotonic MUST be detected | P0 |
| FR-040c-48 | Non-monotonic MUST raise error | P0 |
| FR-040c-49 | Duplicate MUST be detected | P0 |
| FR-040c-50 | Duplicate MUST raise error | P0 |
| FR-040c-51 | Validation MUST be on insert | P0 |
| FR-040c-52 | Validation MUST be in transaction | P0 |
| FR-040c-53 | Validation failure MUST rollback | P0 |
| FR-040c-54 | Validation error MUST be logged | P0 |
| FR-040c-55 | Validation MUST be non-bypassable | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-040c-01 | Sequence increment | <1ms | P0 |
| NFR-040c-02 | Counter read | <1ms | P0 |
| NFR-040c-03 | Query by sequence | <5ms | P0 |
| NFR-040c-04 | Range query (100) | <20ms | P0 |
| NFR-040c-05 | Gap scan (10k) | <500ms | P1 |
| NFR-040c-06 | First/last query | <5ms | P0 |
| NFR-040c-07 | Counter recovery | <100ms | P0 |
| NFR-040c-08 | Index memory | <10MB | P2 |
| NFR-040c-09 | Validation overhead | <1ms | P0 |
| NFR-040c-10 | Concurrent increment | <5ms | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-040c-11 | Sequence unique | 100% | P0 |
| NFR-040c-12 | Sequence monotonic | 100% | P0 |
| NFR-040c-13 | Gap detection | 100% | P0 |
| NFR-040c-14 | Crash recovery | 100% | P0 |
| NFR-040c-15 | No overflow crash | 100% | P0 |
| NFR-040c-16 | Atomic increment | 100% | P0 |
| NFR-040c-17 | Cross-platform | All OS | P0 |
| NFR-040c-18 | Thread safety | No races | P0 |
| NFR-040c-19 | Deterministic | 100% | P0 |
| NFR-040c-20 | Index integrity | 100% | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-040c-21 | Increment logged | Debug | P1 |
| NFR-040c-22 | Gap logged | Warning | P0 |
| NFR-040c-23 | Recovery logged | Info | P0 |
| NFR-040c-24 | Validation error logged | Error | P0 |
| NFR-040c-25 | Metrics: current seq | Gauge | P2 |
| NFR-040c-26 | Metrics: increment rate | Counter | P2 |
| NFR-040c-27 | Metrics: gap count | Gauge | P2 |
| NFR-040c-28 | Structured logging | JSON | P0 |
| NFR-040c-29 | CLI gap report | User-friendly | P1 |
| NFR-040c-30 | Alert on gaps | Critical | P0 |

---

## Acceptance Criteria / Definition of Done

### Sequence Generation
- [ ] AC-001: 64-bit integer
- [ ] AC-002: Starts at 1
- [ ] AC-003: Monotonically increasing
- [ ] AC-004: Unique
- [ ] AC-005: Atomic increment
- [ ] AC-006: Persisted counter
- [ ] AC-007: Crash-safe
- [ ] AC-008: Auto recovery

### Gap Detection
- [ ] AC-009: Gap scan works
- [ ] AC-010: Full range check
- [ ] AC-011: Missing sequences returned
- [ ] AC-012: Gap logged
- [ ] AC-013: Count returned
- [ ] AC-014: Zero when no gaps
- [ ] AC-015: Severity categorized
- [ ] AC-016: Critical alerts

### Sequence Queries
- [ ] AC-017: Query by sequence
- [ ] AC-018: Range query
- [ ] AC-019: First event query
- [ ] AC-020: Last event query
- [ ] AC-021: Next after
- [ ] AC-022: Index exists
- [ ] AC-023: Index unique
- [ ] AC-024: Performance O(log n)

### Ordering Validation
- [ ] AC-025: Non-monotonic detected
- [ ] AC-026: Duplicate detected
- [ ] AC-027: Validation on insert
- [ ] AC-028: Failure rollback
- [ ] AC-029: Error logged
- [ ] AC-030: Non-bypassable
- [ ] AC-031: Overflow detected
- [ ] AC-032: Overflow errors

---

## User Verification Scenarios

### Scenario 1: Normal Sequence
**Persona:** Agent appending events  
**Preconditions:** Log exists  
**Steps:**
1. Append event
2. Get sequence
3. Append another
4. Verify increment

**Verification Checklist:**
- [ ] Sequence assigned
- [ ] Incremented by 1
- [ ] Persisted
- [ ] Logged

### Scenario 2: Counter Recovery
**Persona:** Agent after crash  
**Preconditions:** Events exist  
**Steps:**
1. Crash occurs
2. Restart agent
3. Counter recovered
4. Next sequence correct

**Verification Checklist:**
- [ ] Counter recovered
- [ ] From max(sequence)
- [ ] Next correct
- [ ] Logged

### Scenario 3: Gap Detection
**Persona:** Admin checking integrity  
**Preconditions:** Events exist  
**Steps:**
1. Run gap scan
2. Check results
3. Review gaps
4. Investigate

**Verification Checklist:**
- [ ] Scan completes
- [ ] Gaps found (if any)
- [ ] Details logged
- [ ] Count returned

### Scenario 4: Duplicate Prevention
**Persona:** Bug causing duplicate  
**Preconditions:** Events exist  
**Steps:**
1. Attempt duplicate sequence
2. Validation fails
3. Error raised
4. Rollback

**Verification Checklist:**
- [ ] Duplicate detected
- [ ] Error raised
- [ ] Rollback occurs
- [ ] Logged

### Scenario 5: Range Query
**Persona:** Replay engine  
**Preconditions:** Events exist  
**Steps:**
1. Query range 1-100
2. Get events
3. Verify order
4. Verify count

**Verification Checklist:**
- [ ] Events returned
- [ ] In order
- [ ] Correct count
- [ ] Performance ok

### Scenario 6: Concurrent Increment
**Persona:** Multiple writers  
**Preconditions:** Events exist  
**Steps:**
1. Two concurrent appends
2. Both serialize
3. Both succeed
4. Sequences unique

**Verification Checklist:**
- [ ] Both succeed
- [ ] No collision
- [ ] Serialized
- [ ] Logged

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-040c-01 | Sequence is 64-bit | FR-040c-01 |
| UT-040c-02 | Starts at 1 | FR-040c-02 |
| UT-040c-03 | Monotonic increment | FR-040c-03 |
| UT-040c-04 | Unique sequence | FR-040c-04 |
| UT-040c-05 | Atomic increment | FR-040c-05 |
| UT-040c-06 | Counter persisted | FR-040c-07 |
| UT-040c-07 | Overflow detection | FR-040c-11 |
| UT-040c-08 | Gap detection | FR-040c-16 |
| UT-040c-09 | Gap scan | FR-040c-17 |
| UT-040c-10 | Query by sequence | FR-040c-31 |
| UT-040c-11 | Range query | FR-040c-32 |
| UT-040c-12 | First/last | FR-040c-33 |
| UT-040c-13 | Non-monotonic error | FR-040c-47 |
| UT-040c-14 | Duplicate error | FR-040c-49 |
| UT-040c-15 | Validation rollback | FR-040c-53 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-040c-01 | Full increment flow | E2E |
| IT-040c-02 | Crash recovery | FR-040c-08 |
| IT-040c-03 | Counter from max | FR-040c-10 |
| IT-040c-04 | Concurrent increment | FR-040c-14 |
| IT-040c-05 | Event log integration | Task 040 |
| IT-040c-06 | Gap scan full | FR-040c-17 |
| IT-040c-07 | Range query perf | FR-040c-42 |
| IT-040c-08 | Index performance | FR-040c-41 |
| IT-040c-09 | Cross-platform | NFR-040c-17 |
| IT-040c-10 | Logging | NFR-040c-21 |
| IT-040c-11 | CLI gap report | FR-040c-16 |
| IT-040c-12 | Large volume | 100k sequences |
| IT-040c-13 | Validation | FR-040c-46 |
| IT-040c-14 | Resume integration | Task 040.b |
| IT-040c-15 | Replay integration | Task 042.c |

---

## Implementation Prompt

### Database Schema

```sql
-- Sequence counter table
CREATE TABLE IF NOT EXISTS sequence_counter (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    last_sequence INTEGER NOT NULL DEFAULT 0
);

-- Initialize counter
INSERT OR IGNORE INTO sequence_counter (id, last_sequence) VALUES (1, 0);

-- Unique index on events.sequence
CREATE UNIQUE INDEX IF NOT EXISTS idx_events_sequence ON events(sequence);
```

### File Structure

```
src/
├── Acode.Domain/
│   └── EventLog/
│       ├── SequenceNumber.cs
│       └── GapInfo.cs
├── Acode.Application/
│   └── EventLog/
│       ├── ISequenceGenerator.cs
│       └── IGapDetector.cs
├── Acode.Infrastructure/
│   └── EventLog/
│       ├── SequenceGenerator.cs
│       └── GapDetector.cs
```

### Key Implementation

```csharp
public class SequenceGenerator : ISequenceGenerator
{
    public async Task<SequenceNumber> NextAsync(IDbTransaction tx)
    {
        // Atomic increment within transaction
        var result = await _db.ExecuteScalarAsync<long>(tx, @"
            UPDATE sequence_counter 
            SET last_sequence = last_sequence + 1 
            WHERE id = 1
            RETURNING last_sequence");
        
        if (result > long.MaxValue - 1000)
        {
            throw new SequenceOverflowException(result);
        }
        
        return new SequenceNumber(result);
    }
    
    public async Task RecoverAsync()
    {
        var maxSeq = await _db.ExecuteScalarAsync<long?>(
            "SELECT MAX(sequence) FROM events");
        
        var currentCounter = await _db.ExecuteScalarAsync<long>(
            "SELECT last_sequence FROM sequence_counter WHERE id = 1");
        
        if (maxSeq.HasValue && maxSeq.Value > currentCounter)
        {
            await _db.ExecuteAsync(
                "UPDATE sequence_counter SET last_sequence = @max WHERE id = 1",
                new { max = maxSeq.Value });
            
            _logger.LogInformation("Recovered sequence counter from {Old} to {New}",
                currentCounter, maxSeq.Value);
        }
    }
}

public class GapDetector : IGapDetector
{
    public async Task<GapInfo> ScanAsync()
    {
        var gaps = await _db.QueryAsync<long>(@"
            WITH RECURSIVE seq AS (
                SELECT 1 AS n
                UNION ALL
                SELECT n + 1 FROM seq 
                WHERE n < (SELECT MAX(sequence) FROM events)
            )
            SELECT n FROM seq
            WHERE n NOT IN (SELECT sequence FROM events)
            LIMIT 1000");
        
        var gapList = gaps.ToList();
        
        if (gapList.Any())
        {
            _logger.LogWarning("Gap detected: {Count} missing sequences, first: {First}",
                gapList.Count, gapList.First());
        }
        
        return new GapInfo(gapList);
    }
}
```

**End of Task 040.c Specification**
