# Task 013.b: Persist Approvals + Decisions

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 013 (Human Approval Gates), Task 013.a (Rules/Prompts), Task 011.b (Persistence)  

---

## Description

Task 013.b implements persistence for approval decisions—recording every approval, denial, skip, and timeout in durable storage. This audit trail is essential for accountability, debugging, and policy refinement. Users can review what was approved, when, and by whom.

Approval persistence serves multiple purposes. First, it provides an audit trail for compliance and accountability. When something goes wrong, you can trace back through the approval history to understand what happened. Second, it enables analytics—patterns in approvals inform policy tuning. Third, it supports session resume—interrupted sessions can restore approval state.

Each approval decision is persisted as an ApprovalRecord. The record includes: session ID, operation details, decision (approved/denied/skipped/timeout), timestamp, rule that matched, and any user-provided reason. Records are immutable once created.

Persistence integrates with the two-tier storage model (Task 011.b). Approval records are stored in SQLite for local access and synced to PostgreSQL for centralized analysis. The outbox pattern ensures reliable sync even with network interruptions.

Query capabilities support common needs. List approvals by session. Search by operation type. Filter by decision. Aggregate by rule. These queries enable the CLI commands for viewing approval history.

Privacy considerations apply to approval records. Operation details may include file paths or command strings that could be sensitive. Records are stored locally by default. Sync to remote can be disabled. Sensitive details can be redacted before sync.

Retention policies manage storage growth. By default, records are retained for 90 days. Configurable retention allows longer or shorter periods. Explicit deletion is available for compliance requirements.

The approval history is used for policy suggestions. If a pattern is consistently approved, the system might suggest an auto-approve rule. If a pattern is consistently denied, the system might suggest a deny rule. These suggestions are informational only.

Error handling ensures persistence never blocks execution. If storage fails, the decision still applies—the operation proceeds or is blocked. Persistence failures are logged and retried. No approval is lost due to temporary storage issues.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Approval Record | Persisted decision data |
| Audit Trail | History of decisions |
| Decision | Approved/Denied/Skipped/Timeout |
| Retention | How long records are kept |
| Query | Search/filter of records |
| Sync | Copy to remote storage |
| Redaction | Removing sensitive details |
| Analytics | Pattern analysis |
| Policy Suggestion | Recommended rule change |
| Immutable | Cannot be modified |
| Outbox | Queue for reliable sync |
| Compliance | Meeting regulatory requirements |
| Session History | Approvals for one session |
| Aggregate | Summary statistics |
| Expiry | When record can be deleted |

---

## Out of Scope

The following items are explicitly excluded from Task 013.b:

- **Rule definition** - Task 013.a
- **--yes scoping** - Task 013.c
- **Prompt UI** - Task 013.a
- **Real-time sync** - Async only
- **External audit systems** - Local only
- **Approval signing** - No cryptographic proof
- **Multi-user attribution** - Single user
- **Approval modification** - Immutable only
- **Automatic policy changes** - Suggestions only
- **Long-term archival** - Standard retention

---

## Functional Requirements

### Record Structure

- FR-001: Record MUST have unique ID
- FR-002: Record MUST have session ID
- FR-003: Record MUST have operation category
- FR-004: Record MUST have operation details
- FR-005: Record MUST have decision
- FR-006: Record MUST have timestamp
- FR-007: Record MUST have matched rule name
- FR-008: Record MAY have user reason

### Decision Types

- FR-009: APPROVED decision type
- FR-010: DENIED decision type
- FR-011: SKIPPED decision type
- FR-012: TIMEOUT decision type
- FR-013: AUTO_APPROVED decision type

### Persistence Operations

- FR-014: Create MUST store record
- FR-015: Create MUST be atomic
- FR-016: Create MUST return ID
- FR-017: Records MUST be immutable
- FR-018: Update MUST NOT be allowed

### Query Operations

- FR-019: Query by session ID
- FR-020: Query by decision type
- FR-021: Query by operation category
- FR-022: Query by time range
- FR-023: Query by rule name
- FR-024: Combined filters MUST work

### Pagination

- FR-025: Results MUST be paginated
- FR-026: Default page size: 50
- FR-027: Configurable page size
- FR-028: Total count MUST be available

### Ordering

- FR-029: Default order: newest first
- FR-030: Oldest first option
- FR-031: Order by field option

### Aggregation

- FR-032: Count by decision type
- FR-033: Count by operation category
- FR-034: Count by rule
- FR-035: Time-based grouping

### Storage Integration

- FR-036: Use SQLite for local
- FR-037: Use PostgreSQL for remote
- FR-038: Outbox for sync
- FR-039: Conflict resolution: latest wins

### Sync Behavior

- FR-040: Sync MUST be async
- FR-041: Sync MUST use outbox
- FR-042: Failed sync MUST retry
- FR-043: Sync MUST be configurable

### Privacy

- FR-044: Details MUST be redactable
- FR-045: Redaction MUST be configurable
- FR-046: Sync MUST respect redaction
- FR-047: Local MUST retain full details

### Retention

- FR-048: Default retention: 90 days
- FR-049: Retention MUST be configurable
- FR-050: Expired records MUST be deletable
- FR-051: Manual deletion MUST work

### CLI Commands

- FR-052: List approvals command
- FR-053: Show approval command
- FR-054: Delete approvals command
- FR-055: Export approvals command

### Logging

- FR-056: Creation MUST be logged
- FR-057: Queries MUST be logged
- FR-058: Deletions MUST be logged
- FR-059: Sync events MUST be logged

---

## Non-Functional Requirements

### Performance

- NFR-001: Record creation < 50ms
- NFR-002: Query < 100ms for 1000 records
- NFR-003: No blocking on sync

### Reliability

- NFR-004: No lost records
- NFR-005: Crash-safe storage
- NFR-006: Eventual sync

### Security

- NFR-007: No secrets in records
- NFR-008: Redaction before sync
- NFR-009: Access control

### Scalability

- NFR-010: Handle 10K+ records per session
- NFR-011: Handle 1M+ total records
- NFR-012: Efficient queries

### Compliance

- NFR-013: Complete audit trail
- NFR-014: Deletion support
- NFR-015: Export support

---

## User Manual Documentation

### Overview

Approval persistence creates an audit trail of all approval decisions. Review history, analyze patterns, and maintain compliance records.

### Viewing History

```bash
# List recent approvals
$ acode approvals list

ID        Session   Operation     Decision   Time
apr_001   abc123    FILE_WRITE    APPROVED   2m ago
apr_002   abc123    TERMINAL      APPROVED   1m ago
apr_003   def456    FILE_DELETE   DENIED     1h ago
apr_004   def456    FILE_WRITE    TIMEOUT    1h ago

Showing 4 of 127 records
```

### Filtering

```bash
# Filter by session
$ acode approvals list --session abc123

# Filter by decision
$ acode approvals list --decision denied

# Filter by operation
$ acode approvals list --operation file_delete

# Filter by date
$ acode approvals list --since 2024-01-01 --until 2024-01-31

# Combined filters
$ acode approvals list --session abc123 --decision approved
```

### Viewing Details

```bash
$ acode approvals show apr_001

Approval Record: apr_001
────────────────────────────────────
Session: abc123
Time: 2024-01-15 14:32:15 UTC

Operation: FILE_WRITE
Path: src/components/Button.tsx
Size: 45 lines

Decision: APPROVED
Rule: default-file-write
Response Time: 2.3s
```

### Aggregation

```bash
# Summary statistics
$ acode approvals stats

Approval Statistics (last 30 days)
────────────────────────────────────
Total Records: 1,247

By Decision:
  APPROVED:      892 (71.5%)
  DENIED:         45 (3.6%)
  SKIPPED:        28 (2.2%)
  AUTO_APPROVED: 282 (22.6%)

By Operation:
  FILE_WRITE:    756 (60.6%)
  TERMINAL:      298 (23.9%)
  FILE_DELETE:    89 (7.1%)
  OTHER:         104 (8.3%)

Most Used Rules:
  1. auto-test-files     (412 matches)
  2. default-file-write  (389 matches)
  3. prompt-config       (187 matches)
```

### Export

```bash
# Export to JSON
$ acode approvals export --format json > approvals.json

# Export to CSV
$ acode approvals export --format csv > approvals.csv

# Export with filters
$ acode approvals export --session abc123 --format json
```

### Deletion

```bash
# Delete old records
$ acode approvals cleanup --older-than 90d

Deleting approval records older than 90 days...
Found 342 records to delete.

Continue? [y/N] y

✓ Deleted 342 records

# Delete specific session
$ acode approvals delete --session abc123

Delete all approvals for session abc123? [y/N] y

✓ Deleted 15 records
```

### Configuration

```yaml
# .agent/config.yml
approvals:
  persistence:
    # Enable/disable persistence
    enabled: true
    
    # Retention period
    retention_days: 90
    
    # Sync to remote
    sync_enabled: true
    
    # Redact details before sync
    redact_for_sync: false
    
    # Fields to redact
    redact_fields:
      - operation_details
```

### Privacy & Redaction

Local records contain full details:
```json
{
  "id": "apr_001",
  "operation": "file_write",
  "details": {
    "path": "src/secrets/config.ts",
    "content_hash": "sha256:abc..."
  }
}
```

Synced records (when redacted):
```json
{
  "id": "apr_001",
  "operation": "file_write",
  "details": "[REDACTED]"
}
```

### Troubleshooting

#### Records Not Syncing

**Problem:** Records stay in local only

**Solution:**
1. Check sync_enabled: `acode config get approvals.persistence.sync_enabled`
2. Check network connectivity
3. View sync status: `acode sync status`

#### Storage Growing Large

**Problem:** Approval database is large

**Solution:**
1. Run cleanup: `acode approvals cleanup --older-than 30d`
2. Reduce retention: `approvals.persistence.retention_days: 30`

#### Missing Records

**Problem:** Expected records not found

**Solution:**
1. Check persistence enabled
2. Verify session ID
3. Check time range filters

---

## Acceptance Criteria

### Record Structure

- [ ] AC-001: Has unique ID
- [ ] AC-002: Has session ID
- [ ] AC-003: Has operation category
- [ ] AC-004: Has operation details
- [ ] AC-005: Has decision
- [ ] AC-006: Has timestamp
- [ ] AC-007: Has rule name

### Decision Types

- [ ] AC-008: APPROVED works
- [ ] AC-009: DENIED works
- [ ] AC-010: SKIPPED works
- [ ] AC-011: TIMEOUT works
- [ ] AC-012: AUTO_APPROVED works

### Persistence

- [ ] AC-013: Create works
- [ ] AC-014: Atomic creation
- [ ] AC-015: Returns ID
- [ ] AC-016: Immutable

### Queries

- [ ] AC-017: By session
- [ ] AC-018: By decision
- [ ] AC-019: By operation
- [ ] AC-020: By time range
- [ ] AC-021: Combined

### Pagination

- [ ] AC-022: Paginated
- [ ] AC-023: Configurable size
- [ ] AC-024: Total count

### Storage

- [ ] AC-025: SQLite works
- [ ] AC-026: PostgreSQL works
- [ ] AC-027: Outbox works
- [ ] AC-028: Sync works

### Privacy

- [ ] AC-029: Redaction works
- [ ] AC-030: Sync respects redaction

### Retention

- [ ] AC-031: Default 90 days
- [ ] AC-032: Configurable
- [ ] AC-033: Cleanup works

### CLI

- [ ] AC-034: List works
- [ ] AC-035: Show works
- [ ] AC-036: Delete works
- [ ] AC-037: Export works

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Approvals/Persistence/
├── ApprovalRecordTests.cs
│   ├── Should_Create_Valid_Record()
│   ├── Should_Reject_Invalid_Record()
│   └── Should_Be_Immutable()
│
├── ApprovalRepositoryTests.cs
│   ├── Should_Create_Record()
│   ├── Should_Query_By_Session()
│   ├── Should_Query_By_Decision()
│   └── Should_Paginate_Results()
│
└── RetentionTests.cs
    ├── Should_Identify_Expired()
    └── Should_Delete_Expired()
```

### Integration Tests

```
Tests/Integration/Approvals/Persistence/
├── StorageIntegrationTests.cs
│   ├── Should_Store_In_SQLite()
│   ├── Should_Sync_To_PostgreSQL()
│   └── Should_Handle_Sync_Failure()
│
└── QueryIntegrationTests.cs
    ├── Should_Filter_Correctly()
    └── Should_Aggregate_Correctly()
```

### E2E Tests

```
Tests/E2E/Approvals/Persistence/
├── PersistenceE2ETests.cs
│   ├── Should_Persist_During_Session()
│   ├── Should_Query_After_Session()
│   └── Should_Export_Records()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Record creation | 25ms | 50ms |
| Query 1000 | 50ms | 100ms |
| Aggregation | 100ms | 500ms |

---

## User Verification Steps

### Scenario 1: Record Created

1. Trigger approval prompt
2. Approve operation
3. Run `acode approvals list`
4. Verify: Record exists

### Scenario 2: Query by Session

1. Complete session with approvals
2. Run `acode approvals list --session <id>`
3. Verify: Only session records shown

### Scenario 3: Query by Decision

1. Create approved and denied records
2. Run `acode approvals list --decision denied`
3. Verify: Only denied shown

### Scenario 4: View Details

1. Create approval record
2. Run `acode approvals show <id>`
3. Verify: Full details shown

### Scenario 5: Export

1. Create several records
2. Run `acode approvals export --format json`
3. Verify: Valid JSON output

### Scenario 6: Cleanup

1. Create old records
2. Run `acode approvals cleanup --older-than 0d`
3. Verify: Records deleted

### Scenario 7: Sync

1. Enable sync
2. Create approval
3. Check remote storage
4. Verify: Record synced

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Approvals/
│   └── ApprovalRecord.cs
│
src/AgenticCoder.Application/
├── Approvals/
│   └── Persistence/
│       ├── IApprovalRepository.cs
│       ├── ApprovalQueryParams.cs
│       └── ApprovalAggregation.cs
│
src/AgenticCoder.Infrastructure/
├── Persistence/
│   └── Approvals/
│       ├── SqliteApprovalRepository.cs
│       └── PostgresApprovalRepository.cs
│
src/AgenticCoder.CLI/
└── Commands/
    └── ApprovalsCommand.cs
```

### ApprovalRecord Entity

```csharp
namespace AgenticCoder.Domain.Approvals;

public sealed class ApprovalRecord
{
    public ApprovalRecordId Id { get; }
    public SessionId SessionId { get; }
    public OperationCategory Category { get; }
    public JsonDocument Details { get; }
    public ApprovalDecision Decision { get; }
    public string MatchedRule { get; }
    public string? UserReason { get; }
    public DateTimeOffset CreatedAt { get; }
    
    public static ApprovalRecord Create(
        SessionId sessionId,
        Operation operation,
        ApprovalDecision decision,
        string matchedRule,
        string? reason = null);
}
```

### IApprovalRepository Interface

```csharp
namespace AgenticCoder.Application.Approvals.Persistence;

public interface IApprovalRepository
{
    Task<ApprovalRecordId> CreateAsync(ApprovalRecord record, CancellationToken ct);
    Task<ApprovalRecord?> GetByIdAsync(ApprovalRecordId id, CancellationToken ct);
    Task<PagedResult<ApprovalRecord>> QueryAsync(ApprovalQueryParams query, CancellationToken ct);
    Task<ApprovalAggregation> AggregateAsync(ApprovalQueryParams query, CancellationToken ct);
    Task<int> DeleteExpiredAsync(DateTimeOffset before, CancellationToken ct);
    Task<int> DeleteBySessionAsync(SessionId sessionId, CancellationToken ct);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-APPR-PERS-001 | Record creation failed |
| ACODE-APPR-PERS-002 | Query failed |
| ACODE-APPR-PERS-003 | Sync failed |
| ACODE-APPR-PERS-004 | Record not found |
| ACODE-APPR-PERS-005 | Deletion failed |

### Logging Fields

```json
{
  "event": "approval_record_created",
  "record_id": "apr_001",
  "session_id": "abc123",
  "operation_category": "file_write",
  "decision": "approved",
  "rule": "default-file-write",
  "creation_ms": 23
}
```

### Implementation Checklist

1. [ ] Create ApprovalRecord entity
2. [ ] Create IApprovalRepository interface
3. [ ] Implement SQLite repository
4. [ ] Implement PostgreSQL repository
5. [ ] Create query params
6. [ ] Implement filtering
7. [ ] Implement pagination
8. [ ] Implement aggregation
9. [ ] Create outbox integration
10. [ ] Implement sync
11. [ ] Create CLI commands
12. [ ] Implement retention
13. [ ] Write unit tests
14. [ ] Write integration tests
15. [ ] Write E2E tests

### Validation Checklist Before Merge

- [ ] Records created correctly
- [ ] Queries work
- [ ] Pagination works
- [ ] Aggregation works
- [ ] Sync works
- [ ] Retention works
- [ ] CLI commands work
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** Domain entity
2. **Phase 2:** Repository interface
3. **Phase 3:** SQLite implementation
4. **Phase 4:** Query/filter
5. **Phase 5:** Aggregation
6. **Phase 6:** Sync
7. **Phase 7:** CLI commands
8. **Phase 8:** Retention

---

**End of Task 013.b Specification**