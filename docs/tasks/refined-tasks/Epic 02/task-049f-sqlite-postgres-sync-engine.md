# Task 049.f: SQLite→PostgreSQL Sync Engine

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 2 – CLI + Orchestration Core  
**Dependencies:** Task 049.a (Data Model), Task 050 (Workspace DB), Task 039 (Security)  

---

## Description

Task 049.f implements the sync engine that synchronizes conversation data between local SQLite and remote PostgreSQL. The engine uses an outbox pattern for reliability, batching for efficiency, retries for resilience, and conflict resolution for consistency.

The sync engine is the backbone of the offline-first architecture. Local SQLite is fast and always available. Remote PostgreSQL provides durability and cross-device access. The sync engine bridges them reliably.

The outbox pattern ensures no data is lost. Changes are written to an outbox table atomically with the main data. A background process reads the outbox and syncs to remote. On success, outbox entries are marked complete. On failure, they're retried.

Batching improves efficiency. Individual messages are small—syncing one at a time is wasteful. The engine batches multiple changes into single network calls. Batch size is configurable. Too large risks timeout; too small wastes round trips.

Retries handle transient failures. Network blips, server restarts, and timeouts are common. The engine retries failed syncs with exponential backoff. Permanent failures (validation errors) are flagged differently.

Idempotency ensures exactly-once semantics. Each sync operation has a unique idempotency key. If the same operation is attempted twice, the server recognizes the duplicate and returns success without re-applying.

Conflict resolution handles concurrent modifications. The same chat modified on two devices creates conflict. The engine uses "last-write-wins" by default, with configurable policies. Conflicts are logged for visibility.

The inbox pattern handles reverse sync. Changes made on other devices appear in the inbox. The engine pulls these changes and applies them locally. Merge logic handles the integration.

Sync status is visible to users. Each entity has a SyncStatus field: Pending, Synced, Conflict, Failed. CLI commands show sync state. Users can force sync, retry failed items, or work offline.

The engine respects privacy settings. Items marked LOCAL_ONLY never sync. REDACTED items sync with content removed. FULL items sync as-is. Privacy checks happen before outbox insertion.

Health monitoring ensures the engine is working. Metrics track sync lag, failure rates, and queue depth. Alerts fire when sync is stuck. Diagnostics help troubleshoot issues.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Outbox | Queue for pending uploads |
| Inbox | Queue for pending downloads |
| Sync | Synchronize local/remote |
| Batch | Group of items |
| Retry | Try again after failure |
| Backoff | Increasing delay |
| Idempotency | Same result on repeat |
| Idempotency Key | Unique operation ID |
| Conflict | Concurrent modification |
| Last-Write-Wins | Latest change wins |
| Merge | Combine changes |
| Lag | Time behind current |
| Queue Depth | Pending items count |
| Transient | Temporary failure |
| Permanent | Unrecoverable failure |

---

## Out of Scope

The following items are explicitly excluded from Task 049.f:

- **Data model** - Task 049.a
- **CLI commands** - Task 049.b
- **Concurrency** - Task 049.c
- **Search** - Task 049.d
- **Retention** - Task 049.e
- **Real-time sync** - Polling only
- **Conflict resolution UI** - Auto-resolve only
- **Distributed transactions** - Eventual consistency
- **Multi-region** - Single region
- **Peer-to-peer** - Client-server only

---

## Functional Requirements

### Outbox Pattern

- FR-001: Changes MUST write to outbox
- FR-002: Outbox write MUST be atomic
- FR-003: Outbox MUST have idempotency key
- FR-004: Outbox MUST have payload
- FR-005: Outbox MUST have status
- FR-006: Outbox MUST have retry count

### Outbox Processing

- FR-007: Background processor MUST exist
- FR-008: Processor MUST poll outbox
- FR-009: Poll interval MUST be configurable
- FR-010: Default poll: 5 seconds
- FR-011: Batch size MUST be configurable
- FR-012: Default batch: 50 items

### Batching

- FR-013: Multiple items MUST batch
- FR-014: Batch MUST respect size limit
- FR-015: Batch MUST respect byte limit
- FR-016: Partial batch MUST be allowed

### Upload Flow

- FR-017: Read pending from outbox
- FR-018: Apply privacy/redaction
- FR-019: Send batch to server
- FR-020: Mark successful as Synced
- FR-021: Mark failed for retry

### Retries

- FR-022: Transient failures MUST retry
- FR-023: Exponential backoff MUST apply
- FR-024: Max retries MUST be configurable
- FR-025: Default max: 5 retries
- FR-026: Base delay: 1 second
- FR-027: Max delay: 5 minutes

### Idempotency

- FR-028: Each change MUST have key
- FR-029: Key MUST be ULID
- FR-030: Server MUST detect duplicate
- FR-031: Duplicate MUST return success

### Inbox Pattern

- FR-032: Inbox table MUST exist
- FR-033: Poll server for changes
- FR-034: Download to inbox
- FR-035: Apply to local storage
- FR-036: Mark as applied

### Download Flow

- FR-037: Query server for changes since
- FR-038: Download new/updated items
- FR-039: Apply to local DB
- FR-040: Update last sync timestamp

### Conflict Detection

- FR-041: Version field MUST be checked
- FR-042: Concurrent edit MUST be detected
- FR-043: Conflict MUST be flagged
- FR-044: Conflict MUST be logged

### Conflict Resolution

- FR-045: Last-write-wins MUST be default
- FR-046: Policy MUST be configurable
- FR-047: Conflict MUST update local
- FR-048: Original MUST be preserved

### Status Tracking

- FR-049: SyncStatus MUST be per-entity
- FR-050: Pending status MUST exist
- FR-051: Synced status MUST exist
- FR-052: Conflict status MUST exist
- FR-053: Failed status MUST exist

### CLI Integration

- FR-054: `acode sync status` MUST work
- FR-055: `acode sync now` MUST force sync
- FR-056: `acode sync retry` MUST retry failed
- FR-057: `acode sync pause/resume` MUST work

### Health Monitoring

- FR-058: Queue depth MUST be tracked
- FR-059: Sync lag MUST be tracked
- FR-060: Failure rate MUST be tracked
- FR-061: Metrics MUST be exposed

---

## Non-Functional Requirements

### Performance

- NFR-001: Sync lag < 30 seconds normal
- NFR-002: Batch process < 5s
- NFR-003: No blocking main thread

### Reliability

- NFR-004: No lost data
- NFR-005: Eventual consistency
- NFR-006: Survives restart

### Scalability

- NFR-007: Handle 10k pending
- NFR-008: Handle 100 items/second
- NFR-009: Efficient batching

### Resilience

- NFR-010: Handle network loss
- NFR-011: Handle server downtime
- NFR-012: Graceful degradation

---

## User Manual Documentation

### Overview

The sync engine keeps local and remote data in sync. Work offline, and changes sync when connected. Cross-device access happens automatically.

### Quick Start

```bash
# Check sync status
$ acode sync status

Sync Status
────────────────────────────────────
Mode: Online
Last Sync: 30s ago
Queue: 0 pending

Local → Remote:
  Pending: 0
  Synced today: 47
  Failed: 0

Remote → Local:
  Last pull: 1m ago
  Downloaded today: 12
```

### Forcing Sync

```bash
# Force immediate sync
$ acode sync now
Syncing...
  ↑ Uploaded: 3 items
  ↓ Downloaded: 1 item
Sync complete.

# Retry failed items
$ acode sync retry
Retrying 2 failed items...
  ✓ chat_abc123: synced
  ✗ chat_def456: still failing (invalid data)
```

### Pausing Sync

```bash
# Pause sync (offline mode)
$ acode sync pause
Sync paused. Changes will queue locally.

# Resume sync
$ acode sync resume
Sync resumed. Processing queue...
```

### Configuration

```yaml
# .agent/config.yml
sync:
  enabled: true
  
  # Remote connection
  remote:
    url: postgres://...
    
  # Upload settings
  upload:
    poll_interval_seconds: 5
    batch_size: 50
    batch_bytes_max: 1048576  # 1MB
    
  # Retry settings
  retry:
    max_attempts: 5
    base_delay_seconds: 1
    max_delay_seconds: 300
    
  # Download settings
  download:
    poll_interval_seconds: 30
    
  # Conflict resolution
  conflict:
    policy: last_write_wins  # or 'local_wins', 'remote_wins'
    preserve_original: true
```

### Viewing Queue

```bash
$ acode sync queue

Sync Queue
────────────────────────────────────
Pending Upload: 3 items

Item                    Status    Retries  Last Error
chat_abc123 (msg)       Pending   0        -
chat_def456 (msg)       Pending   0        -
chat_ghi789 (chat)      Failed    3        Connection timeout

Failed items will retry with backoff.
Use 'acode sync retry --force' to retry now.
```

### Conflict Handling

```bash
$ acode sync conflicts

Conflicts
────────────────────────────────────
1 conflict found

Chat: chat_abc123 "Feature: Auth"
  Local version: 5 (modified 10m ago)
  Remote version: 6 (modified 8m ago)
  Resolution: Remote wins (last-write)
  
  Local changes preserved in: .agent/conflicts/chat_abc123_v5.json
```

### Troubleshooting

#### Sync Stuck

**Problem:** Queue not draining

**Solutions:**
1. Check network: `acode sync status`
2. Check errors: `acode sync queue --errors`
3. Force retry: `acode sync retry --force`

#### High Lag

**Problem:** Sync lag increasing

**Solutions:**
1. Check batch size (increase if small)
2. Check network speed
3. Check server health

#### Conflicts

**Problem:** Frequent conflicts

**Solutions:**
1. Work in separate chats
2. Reduce concurrent edits
3. Increase sync frequency

---

## Acceptance Criteria

### Outbox

- [ ] AC-001: Outbox writes work
- [ ] AC-002: Atomic with data
- [ ] AC-003: Idempotency key set
- [ ] AC-004: Status tracked

### Processing

- [ ] AC-005: Background processor runs
- [ ] AC-006: Polling works
- [ ] AC-007: Batching works
- [ ] AC-008: Items marked synced

### Retries

- [ ] AC-009: Failed items retry
- [ ] AC-010: Backoff applies
- [ ] AC-011: Max retries honored

### Idempotency

- [ ] AC-012: Duplicate detected
- [ ] AC-013: Duplicate succeeds

### Inbox

- [ ] AC-014: Downloads work
- [ ] AC-015: Applied locally
- [ ] AC-016: Timestamp updated

### Conflicts

- [ ] AC-017: Detection works
- [ ] AC-018: Resolution applies
- [ ] AC-019: Original preserved

### CLI

- [ ] AC-020: Status works
- [ ] AC-021: Now works
- [ ] AC-022: Retry works
- [ ] AC-023: Pause/resume works

### Health

- [ ] AC-024: Queue depth tracked
- [ ] AC-025: Lag tracked

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Sync/
├── OutboxTests.cs
│   ├── Should_Write_Atomically()
│   ├── Should_Include_IdempotencyKey()
│   └── Should_Track_Status()
│
├── BatcherTests.cs
│   ├── Should_Batch_Items()
│   ├── Should_Respect_Size_Limit()
│   └── Should_Respect_Byte_Limit()
│
├── RetryTests.cs
│   ├── Should_Retry_Transient()
│   ├── Should_Apply_Backoff()
│   └── Should_Honor_Max_Retries()
│
└── ConflictTests.cs
    ├── Should_Detect_Conflict()
    └── Should_Apply_Policy()
```

### Integration Tests

```
Tests/Integration/Sync/
├── SyncEngineTests.cs
│   ├── Should_Upload_To_Postgres()
│   ├── Should_Download_From_Postgres()
│   └── Should_Handle_Network_Loss()
│
└── IdempotencyTests.cs
    └── Should_Deduplicate()
```

### E2E Tests

```
Tests/E2E/Sync/
├── SyncE2ETests.cs
│   ├── Should_Sync_Full_Workflow()
│   ├── Should_Handle_Offline_To_Online()
│   └── Should_Resolve_Conflicts()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Batch 50 items | 2s | 5s |
| Outbox write | 5ms | 10ms |
| Conflict detect | 1ms | 5ms |

---

## User Verification Steps

### Scenario 1: Basic Sync

1. Create message offline
2. Go online
3. Wait for sync
4. Verify: Message in remote

### Scenario 2: Retry

1. Create message
2. Block network
3. Wait for failure
4. Restore network
5. Verify: Retries succeed

### Scenario 3: Batching

1. Create 100 messages quickly
2. Check queue
3. Verify: Batched uploads

### Scenario 4: Conflict

1. Modify chat locally
2. Modify same chat on server
3. Sync
4. Verify: Conflict resolved

### Scenario 5: Pause/Resume

1. Pause sync
2. Create messages
3. Resume sync
4. Verify: Queue drains

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Sync/
│   ├── OutboxEntry.cs
│   ├── InboxEntry.cs
│   ├── SyncStatus.cs
│   └── ConflictPolicy.cs
│
src/AgenticCoder.Application/
├── Sync/
│   ├── ISyncEngine.cs
│   ├── IOutboxProcessor.cs
│   ├── IInboxProcessor.cs
│   └── IConflictResolver.cs
│
src/AgenticCoder.Infrastructure/
├── Sync/
│   ├── SyncEngine.cs
│   ├── OutboxProcessor.cs
│   ├── InboxProcessor.cs
│   ├── ConflictResolver.cs
│   ├── Batcher.cs
│   └── RetryPolicy.cs
```

### OutboxEntry Entity

```csharp
namespace AgenticCoder.Domain.Sync;

public sealed class OutboxEntry
{
    public OutboxEntryId Id { get; }
    public string IdempotencyKey { get; }
    public string EntityType { get; }
    public string EntityId { get; }
    public string Payload { get; }
    public OutboxStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public DateTimeOffset? NextRetryAt { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    
    public void MarkSynced();
    public void MarkFailed(string error);
    public void ScheduleRetry(TimeSpan delay);
}
```

### ISyncEngine Interface

```csharp
namespace AgenticCoder.Application.Sync;

public interface ISyncEngine
{
    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    Task SyncNowAsync(CancellationToken ct);
    Task<SyncStatus> GetStatusAsync(CancellationToken ct);
    Task PauseAsync(CancellationToken ct);
    Task ResumeAsync(CancellationToken ct);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-SYNC-001 | Connection failed |
| ACODE-SYNC-002 | Batch failed |
| ACODE-SYNC-003 | Conflict detected |
| ACODE-SYNC-004 | Max retries exceeded |
| ACODE-SYNC-005 | Invalid payload |

### Implementation Checklist

1. [ ] Create domain entities
2. [ ] Create service interfaces
3. [ ] Implement outbox writer
4. [ ] Implement outbox processor
5. [ ] Implement batcher
6. [ ] Implement retry policy
7. [ ] Implement inbox processor
8. [ ] Implement conflict resolver
9. [ ] Create sync engine
10. [ ] Add CLI commands
11. [ ] Add health monitoring
12. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Domain entities
2. **Phase 2:** Outbox
3. **Phase 3:** Processing
4. **Phase 4:** Batching
5. **Phase 5:** Retries
6. **Phase 6:** Inbox
7. **Phase 7:** Conflicts
8. **Phase 8:** CLI

---

**End of Task 049.f Specification**