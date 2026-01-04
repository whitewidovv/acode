# Task 040.a: Append-Only Event Log – Immutable Writes, WAL Mode

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 10 – Core Reliability  
**Dependencies:** Task 040 (Event Log), Task 050 (Workspace DB)  

---

## Description

Task 040.a implements append-only semantics for the crash-safe event log. Once an event is written, it MUST NOT be modified or deleted. This immutability guarantee is foundational to system integrity—it enables deterministic replay, reliable resume, complete audit trails, and prevents accidental or malicious history rewriting.

The append-only constraint is enforced at multiple layers: database triggers prevent UPDATE and DELETE operations on the events table, the application layer exposes only append methods, and the API surface provides no modification capabilities. This defense-in-depth approach ensures immutability even if one layer is bypassed.

SQLite WAL (Write-Ahead Logging) mode provides crash safety. In WAL mode, changes are written to a separate log file before being applied to the main database. If a crash occurs mid-write, the database can recover by replaying or discarding the incomplete transaction. This ensures no partial writes corrupt the event log.

WAL mode also enables concurrent readers during writes. Multiple components can read the event log while new events are being appended, improving throughput and reducing contention. The checkpoint process periodically transfers changes from the WAL file to the main database, configurable for performance tuning.

### Business Value

Append-only semantics provide:
- Audit trail integrity
- Deterministic replay capability
- Tamper evidence
- Simplified reasoning about state
- Regulatory compliance support

### Scope Boundaries

This task covers append-only enforcement and WAL mode configuration. Core event log structure is Task 040. Resume logic is Task 040.b. Ordering guarantees are Task 040.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Event Log | Task 040 | Provides storage | Core dependency |
| Workspace DB | Task 050 | DB connection | WAL config |
| Resume Engine | Task 040.b | Reads events | Append-only critical |
| Replay Engine | Task 042.c | Reads events | Immutability required |
| Audit Export | Task 039.b | Reads events | Integrity guaranteed |
| Secrets | Task 038 | Pre-write scan | Redaction |
| CLI | Task 000 | Query events | Read-only |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| WAL file corrupt | Checksum | Automatic | Possible loss |
| Checkpoint fail | Error log | Retry | Performance |
| Trigger bypass | Audit log | Alert | Security issue |
| Disk full (WAL) | IOException | Pause + warn | Cannot append |
| WAL too large | Size monitor | Force checkpoint | Performance |
| Concurrent checkpoint | Lock detection | Queue | Delay |
| Power loss mid-write | WAL recovery | Automatic | None |
| fsync failure | Error code | Retry | Durability risk |

### Assumptions

1. **SQLite supports WAL**: All platforms
2. **Triggers enforceable**: No bypass
3. **Application layer correct**: No raw SQL
4. **Checkpointing automatic**: By default
5. **WAL file persists**: Same directory
6. **Concurrent readers safe**: WAL mode
7. **Single writer**: Serialized
8. **fsync reliable**: Hardware support

### Security Considerations

1. **Immutability enforced**: Triggers + API
2. **No raw SQL access**: Application only
3. **Trigger tampering**: File permissions
4. **WAL file security**: Same as DB
5. **Checkpoint safety**: Atomic
6. **No admin bypass**: Except maintenance
7. **Audit of reads**: Optional
8. **Backup immutability**: Preserved

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Append-Only | Only inserts allowed |
| Immutable | Cannot be changed |
| WAL | Write-Ahead Logging |
| Checkpoint | Sync WAL to main DB |
| WAL Mode | SQLite journal mode |
| Trigger | DB-level constraint |
| Defense-in-Depth | Multiple protection layers |
| fsync | Flush to physical disk |
| PRAGMA | SQLite configuration |
| Rollback | Undo incomplete transaction |

---

## Out of Scope

- Event modification features
- Event deletion features
- Alternative journal modes
- Distributed WAL
- WAL replication
- Event compaction
- Event archival

---

## Functional Requirements

### FR-001 to FR-015: Append-Only Enforcement

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040a-01 | UPDATE on events MUST be blocked | P0 |
| FR-040a-02 | DELETE on events MUST be blocked | P0 |
| FR-040a-03 | INSERT MUST be allowed | P0 |
| FR-040a-04 | Trigger MUST block UPDATE | P0 |
| FR-040a-05 | Trigger MUST block DELETE | P0 |
| FR-040a-06 | Trigger MUST raise error | P0 |
| FR-040a-07 | Error MUST be descriptive | P0 |
| FR-040a-08 | Application layer MUST NOT expose update | P0 |
| FR-040a-09 | Application layer MUST NOT expose delete | P0 |
| FR-040a-10 | IEventLog interface MUST be append-only | P0 |
| FR-040a-11 | No bypass method MUST exist | P0 |
| FR-040a-12 | Raw SQL MUST NOT be used | P0 |
| FR-040a-13 | Maintenance mode MUST be documented | P1 |
| FR-040a-14 | Maintenance MUST require explicit flag | P1 |
| FR-040a-15 | Maintenance operations MUST be audited | P0 |

### FR-016 to FR-030: WAL Mode Configuration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040a-16 | WAL mode MUST be enabled | P0 |
| FR-040a-17 | PRAGMA journal_mode=WAL MUST execute | P0 |
| FR-040a-18 | WAL mode MUST be verified | P0 |
| FR-040a-19 | Non-WAL MUST fail startup | P0 |
| FR-040a-20 | WAL file MUST be created | P0 |
| FR-040a-21 | WAL file MUST be same directory | P0 |
| FR-040a-22 | WAL size MUST be monitored | P1 |
| FR-040a-23 | WAL size threshold MUST be configurable | P2 |
| FR-040a-24 | Sync mode MUST be NORMAL or FULL | P0 |
| FR-040a-25 | FULL sync MUST be default | P0 |
| FR-040a-26 | Sync mode MUST be configurable | P1 |
| FR-040a-27 | WAL persists across restart | P0 |
| FR-040a-28 | Incomplete WAL MUST be recovered | P0 |
| FR-040a-29 | Recovery MUST be automatic | P0 |
| FR-040a-30 | Recovery MUST be logged | P0 |

### FR-031 to FR-045: Checkpointing

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040a-31 | Auto checkpoint MUST be enabled | P0 |
| FR-040a-32 | Checkpoint threshold MUST be configurable | P1 |
| FR-040a-33 | Default threshold MUST be 1000 pages | P1 |
| FR-040a-34 | Manual checkpoint MUST be possible | P1 |
| FR-040a-35 | Checkpoint MUST use PASSIVE mode | P1 |
| FR-040a-36 | FULL checkpoint MUST be available | P2 |
| FR-040a-37 | RESTART checkpoint MUST be available | P2 |
| FR-040a-38 | TRUNCATE checkpoint MUST be available | P2 |
| FR-040a-39 | Checkpoint MUST be atomic | P0 |
| FR-040a-40 | Checkpoint MUST not block reads | P0 |
| FR-040a-41 | Checkpoint result MUST be logged | P1 |
| FR-040a-42 | Checkpoint failure MUST retry | P0 |
| FR-040a-43 | Retry count MUST be limited | P0 |
| FR-040a-44 | Checkpoint timeout MUST exist | P1 |
| FR-040a-45 | Timeout MUST be configurable | P2 |

### FR-046 to FR-060: Concurrent Access

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-040a-46 | Multiple readers MUST work | P0 |
| FR-040a-47 | Readers MUST NOT block writer | P0 |
| FR-040a-48 | Writer MUST NOT block readers | P0 |
| FR-040a-49 | Reader isolation MUST be snapshot | P0 |
| FR-040a-50 | Snapshot MUST be consistent | P0 |
| FR-040a-51 | Reader connection pool MUST exist | P1 |
| FR-040a-52 | Writer MUST be singleton | P0 |
| FR-040a-53 | Write lock MUST serialize | P0 |
| FR-040a-54 | Lock timeout MUST be configurable | P1 |
| FR-040a-55 | Lock timeout default MUST be 5s | P1 |
| FR-040a-56 | Deadlock MUST NOT occur | P0 |
| FR-040a-57 | Stale readers MUST timeout | P1 |
| FR-040a-58 | Connection cleanup MUST work | P0 |
| FR-040a-59 | Connection leaks MUST be detected | P1 |
| FR-040a-60 | Busy timeout MUST be set | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-040a-01 | Append latency | <10ms | P0 |
| NFR-040a-02 | Concurrent reads | 10+ | P1 |
| NFR-040a-03 | Checkpoint latency | <100ms | P1 |
| NFR-040a-04 | WAL recovery | <1s | P0 |
| NFR-040a-05 | Trigger overhead | <1ms | P0 |
| NFR-040a-06 | Connection acquire | <10ms | P1 |
| NFR-040a-07 | WAL file size | <100MB | P2 |
| NFR-040a-08 | Memory per reader | <10MB | P2 |
| NFR-040a-09 | Writer memory | <50MB | P2 |
| NFR-040a-10 | fsync latency | <50ms | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-040a-11 | Crash safety | 100% | P0 |
| NFR-040a-12 | No data loss | 100% | P0 |
| NFR-040a-13 | Immutability | 100% | P0 |
| NFR-040a-14 | Trigger reliability | 100% | P0 |
| NFR-040a-15 | Recovery success | 100% | P0 |
| NFR-040a-16 | Checkpoint success | 99.9% | P0 |
| NFR-040a-17 | No corruption | 100% | P0 |
| NFR-040a-18 | Cross-platform | All OS | P0 |
| NFR-040a-19 | Thread safety | No races | P0 |
| NFR-040a-20 | Durability | fsync | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-040a-21 | WAL mode logged | Info | P0 |
| NFR-040a-22 | Checkpoint logged | Debug | P1 |
| NFR-040a-23 | Recovery logged | Info | P0 |
| NFR-040a-24 | Trigger violations logged | Error | P0 |
| NFR-040a-25 | WAL size metric | Gauge | P2 |
| NFR-040a-26 | Checkpoint count | Counter | P2 |
| NFR-040a-27 | Connection count | Gauge | P2 |
| NFR-040a-28 | Structured logging | JSON | P0 |
| NFR-040a-29 | Health check | Checkpoint test | P1 |
| NFR-040a-30 | Alerts on trigger bypass | Critical | P0 |

---

## Acceptance Criteria / Definition of Done

### Append-Only
- [ ] AC-001: UPDATE blocked by trigger
- [ ] AC-002: DELETE blocked by trigger
- [ ] AC-003: INSERT works
- [ ] AC-004: Trigger error is descriptive
- [ ] AC-005: No update method in interface
- [ ] AC-006: No delete method in interface
- [ ] AC-007: Raw SQL prevented
- [ ] AC-008: Maintenance mode documented

### WAL Mode
- [ ] AC-009: WAL mode enabled
- [ ] AC-010: WAL verified on startup
- [ ] AC-011: Non-WAL fails startup
- [ ] AC-012: WAL file created
- [ ] AC-013: Sync mode configured
- [ ] AC-014: FULL sync default
- [ ] AC-015: Recovery automatic
- [ ] AC-016: Recovery logged

### Checkpointing
- [ ] AC-017: Auto checkpoint works
- [ ] AC-018: Threshold configurable
- [ ] AC-019: Manual checkpoint works
- [ ] AC-020: PASSIVE mode default
- [ ] AC-021: Checkpoint atomic
- [ ] AC-022: Does not block reads
- [ ] AC-023: Result logged
- [ ] AC-024: Retry on failure

### Concurrent Access
- [ ] AC-025: Multiple readers work
- [ ] AC-026: Readers don't block writer
- [ ] AC-027: Writer doesn't block readers
- [ ] AC-028: Snapshot isolation
- [ ] AC-029: Connection pool works
- [ ] AC-030: Writer singleton
- [ ] AC-031: Serialized writes
- [ ] AC-032: No deadlocks

---

## User Verification Scenarios

### Scenario 1: Immutability Enforcement
**Persona:** Malicious or buggy code  
**Preconditions:** Events exist  
**Steps:**
1. Attempt UPDATE via raw SQL
2. Trigger fires
3. Error returned
4. Event unchanged

**Verification Checklist:**
- [ ] UPDATE blocked
- [ ] Error message clear
- [ ] Event intact
- [ ] Logged

### Scenario 2: WAL Recovery
**Persona:** Agent after crash  
**Preconditions:** Crash mid-write  
**Steps:**
1. Start agent
2. WAL detected
3. Recovery runs
4. State consistent

**Verification Checklist:**
- [ ] WAL recovered
- [ ] No partial events
- [ ] Counter correct
- [ ] Logged

### Scenario 3: Concurrent Reading
**Persona:** Multiple components  
**Preconditions:** Events exist  
**Steps:**
1. Start 5 readers
2. Start 1 writer
3. All proceed
4. No blocking

**Verification Checklist:**
- [ ] All readers succeed
- [ ] Writer succeeds
- [ ] No contention
- [ ] Performance ok

### Scenario 4: Checkpoint Cycle
**Persona:** Long-running agent  
**Preconditions:** Many events written  
**Steps:**
1. Write 1000+ events
2. Checkpoint triggers
3. WAL reduced
4. Performance stable

**Verification Checklist:**
- [ ] Checkpoint triggers
- [ ] WAL size reduced
- [ ] No interruption
- [ ] Logged

### Scenario 5: Maintenance Mode
**Persona:** Admin  
**Preconditions:** Need to fix data  
**Steps:**
1. Enable maintenance flag
2. Disable triggers
3. Make fix
4. Re-enable + audit

**Verification Checklist:**
- [ ] Flag required
- [ ] Action audited
- [ ] Triggers restored
- [ ] Documented

### Scenario 6: Delete Attempt
**Persona:** Code bug  
**Preconditions:** Events exist  
**Steps:**
1. Attempt DELETE
2. Trigger fires
3. Error returned
4. Events preserved

**Verification Checklist:**
- [ ] DELETE blocked
- [ ] Error clear
- [ ] Events intact
- [ ] Violation logged

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-040a-01 | Trigger blocks UPDATE | FR-040a-01 |
| UT-040a-02 | Trigger blocks DELETE | FR-040a-02 |
| UT-040a-03 | INSERT allowed | FR-040a-03 |
| UT-040a-04 | Error message | FR-040a-07 |
| UT-040a-05 | Interface is append-only | FR-040a-10 |
| UT-040a-06 | WAL mode set | FR-040a-16 |
| UT-040a-07 | WAL verification | FR-040a-18 |
| UT-040a-08 | Non-WAL fails | FR-040a-19 |
| UT-040a-09 | Sync mode | FR-040a-24 |
| UT-040a-10 | Checkpoint config | FR-040a-32 |
| UT-040a-11 | Lock timeout | FR-040a-54 |
| UT-040a-12 | Busy timeout | FR-040a-60 |
| UT-040a-13 | Reader isolation | FR-040a-49 |
| UT-040a-14 | Writer serialization | FR-040a-53 |
| UT-040a-15 | Connection cleanup | FR-040a-58 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-040a-01 | Full append flow | E2E |
| IT-040a-02 | Crash simulation | NFR-040a-11 |
| IT-040a-03 | WAL recovery | FR-040a-28 |
| IT-040a-04 | Concurrent readers | FR-040a-46 |
| IT-040a-05 | Reader + writer | FR-040a-47 |
| IT-040a-06 | Checkpoint trigger | FR-040a-31 |
| IT-040a-07 | Checkpoint modes | FR-040a-35 |
| IT-040a-08 | Trigger bypass attempt | Security |
| IT-040a-09 | Maintenance mode | FR-040a-13 |
| IT-040a-10 | Cross-platform | NFR-040a-18 |
| IT-040a-11 | Large WAL | FR-040a-22 |
| IT-040a-12 | Connection pool | FR-040a-51 |
| IT-040a-13 | Lock contention | FR-040a-53 |
| IT-040a-14 | Logging | NFR-040a-21 |
| IT-040a-15 | Performance | NFR-040a-01 |

---

## Implementation Prompt

### Database Triggers

```sql
-- Prevent UPDATE on events
CREATE TRIGGER prevent_event_update
BEFORE UPDATE ON events
BEGIN
    SELECT RAISE(ABORT, 'Events are immutable. UPDATE is not allowed.');
END;

-- Prevent DELETE on events
CREATE TRIGGER prevent_event_delete
BEFORE DELETE ON events
BEGIN
    SELECT RAISE(ABORT, 'Events are immutable. DELETE is not allowed.');
END;
```

### WAL Configuration

```sql
-- Enable WAL mode (must succeed)
PRAGMA journal_mode = WAL;

-- Set synchronous mode (FULL for durability, NORMAL for performance)
PRAGMA synchronous = FULL;

-- Auto-checkpoint threshold (pages)
PRAGMA wal_autocheckpoint = 1000;

-- Busy timeout (milliseconds)
PRAGMA busy_timeout = 5000;
```

### File Structure

```
src/
├── Acode.Infrastructure/
│   └── EventLog/
│       ├── WalConfiguration.cs
│       ├── ImmutabilityTriggers.cs
│       └── CheckpointManager.cs
```

### Key Implementation

```csharp
public class WalConfiguration
{
    public async Task ConfigureAsync(SqliteConnection conn)
    {
        // Enable WAL mode
        await conn.ExecuteAsync("PRAGMA journal_mode = WAL");
        
        // Verify WAL mode
        var mode = await conn.ExecuteScalarAsync<string>("PRAGMA journal_mode");
        if (mode != "wal")
            throw new InvalidOperationException("WAL mode required");
        
        // Configure durability
        await conn.ExecuteAsync($"PRAGMA synchronous = {_options.SyncMode}");
        
        // Configure checkpoint threshold
        await conn.ExecuteAsync($"PRAGMA wal_autocheckpoint = {_options.CheckpointThreshold}");
        
        // Configure busy timeout
        await conn.ExecuteAsync($"PRAGMA busy_timeout = {_options.BusyTimeoutMs}");
        
        _logger.LogInformation("WAL mode configured: sync={Sync}, checkpoint={Threshold}",
            _options.SyncMode, _options.CheckpointThreshold);
    }
}
```

**End of Task 040.a Specification**
