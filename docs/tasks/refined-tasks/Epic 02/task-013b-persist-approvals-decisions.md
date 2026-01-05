# Task 013.b: Persist Approvals + Decisions

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 013 (Human Approval Gates), Task 013.a (Rules/Prompts), Task 011.b (Persistence)  

---

## Description

Task 013.b implements durable persistence for human approval decisions—creating an immutable audit trail of every approval, denial, skip, and timeout throughout Acode's operation. This persistence layer serves as the foundation for accountability, compliance, debugging, session resume, and intelligent policy recommendations. Every decision made through the approval gate system is captured, timestamped, and stored for future reference.

### Business Value and ROI

**Quantified Benefits:**

1. **Compliance and Audit Cost Reduction: $85,000/year**
   - Regulated industries (fintech, healthcare, enterprise) require audit trails for automated actions
   - Manual audit log maintenance costs: 20 hours/month × $100/hour = $24,000/year
   - Audit preparation costs: 80 hours/quarter × $150/hour = $48,000/year
   - Legal/compliance consultation for gaps: $15,000/year
   - With approval persistence: Automated, queryable, exportable audit trail
   - Audit preparation reduced to 10 hours/quarter
   - Total savings: **$85,000/year** in compliance costs

2. **Incident Investigation Time Reduction: $45,000/year**
   - Average incident investigation without audit trail: 8 hours
   - Average incident investigation with approval history: 1.5 hours
   - Time savings per incident: 6.5 hours
   - Average incidents requiring investigation: 30/year
   - 30 × 6.5 hours × $230/hour (senior engineer time) = **$44,850/year** saved

3. **Policy Optimization Value: $35,000/year**
   - Approval data enables pattern analysis
   - Identify consistently approved patterns → create auto-approve rules
   - Identify consistently denied patterns → create deny rules
   - Each optimized rule saves 2 seconds × 50 occurrences/day = 100 seconds/day
   - 20 optimized rules × 100 seconds × 250 days × $50/hour = **$34,700/year** productivity gains

4. **Session Resume Reliability: $25,000/year**
   - Without persistence: Interrupted session loses all approval context
   - User must re-approve everything or start over
   - Average re-approval time: 15 minutes per session
   - Interruption frequency: 10% of sessions
   - With persistence: Seamless resume with full approval state
   - 500 sessions/year × 10% × 15 minutes × $50/hour = **$6,250** base savings
   - Plus: Avoided frustration, context switching costs: **$18,750/year** additional value

**Total ROI: $190,000/year for a 10-person development team**

### Technical Architecture

#### Approval Record Data Model

The core data model captures all information needed for audit, query, and resume capabilities:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        ApprovalRecord Entity                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Identity                                                        │   │
│  │  - RecordId: Guid (Primary Key, generated)                       │   │
│  │  - SessionId: Guid (FK to Sessions)                              │   │
│  │  - Sequence: int (Order within session)                          │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Operation Context                                               │   │
│  │  - Category: enum (FILE_READ, FILE_WRITE, FILE_DELETE, etc.)    │   │
│  │  - OperationHash: string (deterministic ID of operation)        │   │
│  │  - Path: string? (for file operations)                          │   │
│  │  - Command: string? (for terminal operations)                   │   │
│  │  - DetailsJson: string (full operation details, JSON)           │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Decision                                                        │   │
│  │  - Decision: enum (APPROVED, DENIED, SKIPPED, TIMEOUT, AUTO)    │   │
│  │  - Reason: string? (user-provided reason)                       │   │
│  │  - MatchedRuleName: string? (which rule determined policy)      │   │
│  │  - PolicyApplied: enum (AUTO, PROMPT, DENY, SKIP)               │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Timing                                                          │   │
│  │  - RequestedAt: DateTimeOffset (when approval was requested)    │   │
│  │  - DecidedAt: DateTimeOffset (when decision was made)           │   │
│  │  - DurationMs: int (time to decide)                             │   │
│  │  - TimeoutConfiguredMs: int? (timeout if applicable)            │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Integrity                                                       │   │
│  │  - Signature: string (HMAC signature for tamper detection)      │   │
│  │  - CreatedAt: DateTimeOffset (immutable record creation time)   │   │
│  │  - Version: int (schema version for migration)                  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Storage Architecture

Approval records use the two-tier storage model defined in Task 011.b:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Approval Persistence Architecture                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌────────────────────┐                                                 │
│  │  Approval Gate     │                                                 │
│  │  (Decision Source) │                                                 │
│  └─────────┬──────────┘                                                 │
│            │ Decision Event                                             │
│            ▼                                                            │
│  ┌────────────────────┐                                                 │
│  │  ApprovalRecorder  │                                                 │
│  │  - Creates record  │                                                 │
│  │  - Signs record    │                                                 │
│  │  - Validates data  │                                                 │
│  └─────────┬──────────┘                                                 │
│            │ ApprovalRecord                                             │
│            ▼                                                            │
│  ┌────────────────────┐     ┌────────────────────┐                     │
│  │  SQLite (Local)    │────▶│  Outbox Queue      │                     │
│  │  - Immediate write │     │  - Pending syncs   │                     │
│  │  - Fast queries    │     │  - Retry logic     │                     │
│  │  - WAL mode        │     │  - Batch uploads   │                     │
│  └────────────────────┘     └─────────┬──────────┘                     │
│                                       │ Async Sync                      │
│                                       ▼                                 │
│                             ┌────────────────────┐                      │
│                             │  PostgreSQL        │                      │
│                             │  (Optional Remote) │                      │
│                             │  - Aggregation     │                      │
│                             │  - Team analytics  │                      │
│                             │  - Long-term store │                      │
│                             └────────────────────┘                      │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Query Capabilities

The persistence layer supports various query patterns:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Query Interface Capabilities                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Query by Session:                                                       │
│  ─────────────────                                                       │
│  SELECT * FROM approval_records                                          │
│  WHERE session_id = @sessionId                                           │
│  ORDER BY sequence ASC                                                   │
│                                                                          │
│  Query by Decision Type:                                                 │
│  ────────────────────────                                                │
│  SELECT * FROM approval_records                                          │
│  WHERE decision = @decision                                              │
│  AND decided_at BETWEEN @start AND @end                                  │
│                                                                          │
│  Query by Operation Category:                                            │
│  ────────────────────────────                                            │
│  SELECT * FROM approval_records                                          │
│  WHERE category = @category                                              │
│  AND path LIKE @pathPattern                                              │
│                                                                          │
│  Aggregation by Rule:                                                    │
│  ────────────────────                                                    │
│  SELECT matched_rule_name,                                               │
│         decision,                                                        │
│         COUNT(*) as count,                                               │
│         AVG(duration_ms) as avg_duration                                 │
│  FROM approval_records                                                   │
│  WHERE decided_at >= @since                                              │
│  GROUP BY matched_rule_name, decision                                    │
│                                                                          │
│  Pattern Analysis for Policy Suggestions:                                │
│  ────────────────────────────────────────                                │
│  SELECT path_pattern,                                                    │
│         category,                                                        │
│         SUM(CASE WHEN decision = 'APPROVED' THEN 1 ELSE 0 END) as approved, │
│         SUM(CASE WHEN decision = 'DENIED' THEN 1 ELSE 0 END) as denied   │
│  FROM approval_records                                                   │
│  WHERE policy_applied = 'PROMPT'                                         │
│  GROUP BY path_pattern, category                                         │
│  HAVING COUNT(*) >= 10                                                   │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Integration Points

#### Integration with Task 013 (Human Approval Gates)
- Gate framework emits decision events
- ApprovalRecorder subscribes to events
- Records created synchronously with decisions
- Resume queries existing records for session

#### Integration with Task 011.b (Persistence Model)
- Uses workspace database for SQLite storage
- Uses outbox pattern for PostgreSQL sync
- Follows same transaction patterns
- Shares connection pooling

#### Integration with Task 013.a (Gate Rules/Prompts)
- Records which rule matched each decision
- Supports rule effectiveness analysis
- Enables policy suggestions from patterns

### Design Decisions and Trade-offs

**Decision 1: Synchronous Local Write, Async Remote Sync**
- Local write must complete before operation proceeds
- Remote sync happens in background
- Trade-off: Local storage must always be available, but remote failures don't block

**Decision 2: Immutable Records**
- Records cannot be modified after creation
- Corrections require new records with reference to original
- Trade-off: Storage grows, but audit integrity guaranteed

**Decision 3: Operation Details in JSON**
- Flexible storage for varying operation types
- Queryable with SQLite JSON functions
- Trade-off: Less type safety, but more extensible

**Decision 4: Retention with Soft Delete**
- Expired records marked as deleted, not removed
- Actual removal happens during maintenance window
- Trade-off: Storage overhead, but safer for compliance

### Constraints and Limitations

**Technical Constraints:**
- Maximum record size: 64KB (including operation details)
- Maximum records per session: 10,000 (practical limit)
- Query result limit: 1,000 records per page

**Operational Constraints:**
- Retention minimum: 1 day
- Retention maximum: 10 years
- Sync to remote requires network access (optional)

**Security Constraints:**
- Records signed with session key
- Sensitive paths redacted in remote sync
- No PII stored in operation details

### Performance Characteristics

- Record creation: < 10ms (local SQLite)
- Query by session: < 50ms for 1,000 records
- Aggregation query: < 200ms for 100,000 records
- Remote sync: Batched, every 5 minutes or 100 records
- Storage per record: ~500 bytes average

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

## Use Cases

### Use Case 1: David the Compliance Manager

**Persona:** David Park, Compliance Manager at a healthcare software company. His organization must demonstrate audit trails for SOC 2 compliance and HIPAA-related code changes. He needs to prove that all automated actions were explicitly approved and that the approval process is traceable.

**Before Acode with Approval Persistence:**
David's compliance audits are a nightmare. Developers use various AI coding tools, but there's no centralized record of what was approved. When auditors ask "who approved this database migration script?", the team scrambles through Slack messages, git blame, and developer memory. Each audit costs $50,000 in preparation time and inevitably has findings about gaps in authorization records.

**After Acode with Approval Persistence:**
Every approval decision is automatically recorded with full context:

```bash
$ acode approvals export --format csv --since 2024-01-01

session_id,operation,path,decision,decided_at,rule_matched,duration_ms
abc123,FILE_WRITE,src/migrations/001_add_patient_table.sql,APPROVED,2024-01-15T10:23:45Z,prompt-migrations,4523
abc123,TERMINAL,npm run migrate:prod,APPROVED,2024-01-15T10:24:12Z,prompt-prod-commands,8901
def456,FILE_DELETE,src/legacy/old_api.ts,DENIED,2024-01-16T14:32:01Z,deny-source-delete,892
...
```

Auditors get a complete, timestamped, queryable record of every decision. Export to CSV, filter by date range, show decision timing—all automated.

**Measurable Improvement:**
- Audit preparation time: 80 hours → 8 hours (90% reduction)
- Audit findings related to approval gaps: 5 → 0
- Cost per audit: $50,000 → $5,000
- Annual savings: **$180,000** (4 audits × $45,000 saved)

---

### Use Case 2: Priya the Incident Responder

**Persona:** Priya Sharma, Senior SRE at an e-commerce platform. When production incidents occur, she needs to quickly trace what changed, when, and whether it was properly authorized. The difference between a 30-minute outage and a 4-hour outage often depends on how fast she can find the root cause.

**Before Acode with Approval Persistence:**
A production incident occurs—orders are failing. Priya suspects a recent code change. She checks git history and finds 15 commits in the last 2 hours. For each commit, she has to ask developers: "Did you review this change?" "What did the AI generate?" "Did you approve the database query change?" Meanwhile, the incident continues. After 3 hours, she discovers the culprit was an AI-generated SQL query that a developer quickly approved without reading carefully.

**After Acode with Approval Persistence:**
When the incident occurs, Priya queries approval history:

```bash
$ acode approvals history --path "src/database/**" --since "2h ago"

Session  Operation    Path                              Decision  Time     Rule
─────────────────────────────────────────────────────────────────────────────────
gh789    FILE_WRITE   src/database/queries/orders.sql   APPROVED  1h ago   prompt-db
gh789    FILE_WRITE   src/database/queries/inventory.sql APPROVED  1h ago   prompt-db
jk012    FILE_WRITE   src/database/connection.ts        APPROVED  45m ago  prompt-source

$ acode approvals details gh789-op-3

Operation: FILE_WRITE
Path: src/database/queries/orders.sql
Decision: APPROVED
Decided At: 2024-03-15T14:23:01Z
Duration: 2.1 seconds    ← Very short review time!
Rule: prompt-db
Session: gh789 (Developer: alice@company.com)

[View file content at approval time]
```

The 2.1-second decision duration immediately flags a rushed approval. Priya identifies the problematic query in minutes instead of hours.

**Measurable Improvement:**
- Average incident investigation time: 3 hours → 30 minutes (85% reduction)
- MTTR for code-related incidents: 4 hours → 1.5 hours
- Annual incident cost savings: **$150,000** (20 incidents × 2.5 hours × $300/hour)

---

### Use Case 3: Alex the Team Lead Optimizing Workflow

**Persona:** Alex Thompson, Engineering Team Lead who wants to improve the team's productivity without compromising safety. They notice developers complaining about too many approval prompts, but aren't sure which patterns are safe to auto-approve.

**Before Acode with Approval Persistence:**
Alex tries to optimize approval rules based on developer complaints. "Tests always get approved, let's auto-approve them." But this is anecdotal—maybe 5% of test file writes actually get denied for good reasons. Without data, optimizations are guesses that might introduce risk or miss opportunities.

**After Acode with Approval Persistence:**
Alex queries the approval data for insights:

```bash
$ acode approvals stats --since "90 days" --group-by pattern

Pattern Analysis (Last 90 Days)
═══════════════════════════════════════════════════════════════════════════

Pattern: **/*.test.ts
  Total: 4,521 decisions
  Approved: 4,519 (99.96%)
  Denied: 2 (0.04%)
  Avg Decision Time: 1.2s
  → RECOMMENDATION: Consider auto-approve (98%+ approval rate)

Pattern: src/config/**
  Total: 234 decisions
  Approved: 156 (66.7%)
  Denied: 78 (33.3%)
  Avg Decision Time: 45.3s
  → RECOMMENDATION: Keep as prompt (significant denial rate)

Pattern: **/.env*
  Total: 12 decisions
  Approved: 0 (0%)
  Denied: 12 (100%)
  Avg Decision Time: 8.2s
  → RECOMMENDATION: Consider deny rule (100% denial rate)

$ acode approvals suggest-rules

Suggested Rule Changes:
1. ADD: auto-approve **/*.test.ts (saves ~75 prompts/day)
2. ADD: deny **/.env* (prevents 12 mistakes/month)
3. KEEP: prompt src/config/** (meaningful review)
```

Data-driven rule optimization improves productivity while maintaining safety.

**Measurable Improvement:**
- Prompts reduced via optimized rules: 75/day → 18,750/year
- Time saved: 18,750 × 3 seconds = 15.6 hours/year/developer
- Team of 8: **125 hours/year** productivity recovered
- Prevented accidents via data-informed deny rules: ~12/year × $2,500 = **$30,000/year**

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

## Assumptions

### Technical Assumptions

- ASM-001: Approval records are stored in workspace database
- ASM-002: Records are immutable once created
- ASM-003: Record timestamps use UTC for consistency
- ASM-004: Query interface supports filtering by date, operation, decision
- ASM-005: Storage format supports efficient aggregation queries

### Behavioral Assumptions

- ASM-006: Every approval decision creates a record
- ASM-007: Records include full operation context for audit
- ASM-008: Aggregations inform policy recommendations
- ASM-009: Users can export approval history
- ASM-010: Retention policy configurable (default: session lifetime)

### Dependency Assumptions

- ASM-011: Task 013 gate framework provides decision data
- ASM-012: Task 050 workspace database provides storage
- ASM-013: Task 011.b persistence layer provides database access

### Audit Assumptions

- ASM-014: Audit trail is complete - no gaps in recording
- ASM-015: Records support compliance requirements
- ASM-016: Query performance is acceptable for reporting

---

## Security Considerations

### Threat 1: Audit Record Tampering

**Risk Level:** Critical
**CVSS Score:** 8.6 (High)
**Attack Vector:** Database manipulation

**Description:**
An attacker who gains access to the SQLite database could modify approval records to hide malicious activity. By changing a DENIED record to APPROVED, or deleting records entirely, the audit trail becomes unreliable.

**Mitigation:** Each record includes an HMAC signature computed over all fields using a session-derived key. The `ApprovalRecordIntegrityVerifier` class (from Task 013 Security Considerations) validates signatures on read. Any tampered record fails verification and triggers a security alert.

---

### Threat 2: Sensitive Data Exposure in Records

**Risk Level:** High
**CVSS Score:** 7.1 (High)
**Attack Vector:** Data leakage

**Description:**
Approval records contain operation details that may include file paths, command strings, or file contents. If these contain sensitive data (credentials, PII, proprietary code), the audit trail becomes a target for data exfiltration.

**Mitigation:** The `RecordSanitizer` class filters sensitive patterns before persistence. File paths are stored, but file contents are never stored in records. Command strings are redacted for known secret patterns. Sync to remote storage applies additional redaction configured in `.agent/config.yml`.

---

### Threat 3: Storage Exhaustion via Record Flooding

**Risk Level:** Medium
**CVSS Score:** 5.3 (Medium)
**Attack Vector:** Resource exhaustion

**Description:**
An attacker could craft tasks that generate millions of operations, creating millions of approval records. This could exhaust disk space, slow queries to unusable levels, or cause out-of-memory conditions.

**Mitigation:** Per-session record limits (10,000 max), per-day limits (100,000 max), and automatic cleanup of old records via retention policy. The `ApprovalStorageGuard` monitors storage usage and blocks new records if limits are exceeded.

---

### Threat 4: Query Injection via Malformed Filters

**Risk Level:** Medium
**CVSS Score:** 5.9 (Medium)
**Attack Vector:** SQL injection

**Description:**
Query interfaces accept user-provided filters (path patterns, rule names, etc.). Malformed input could be crafted to inject SQL, bypassing query constraints or extracting unauthorized data.

**Mitigation:** All queries use parameterized statements. Path patterns are validated against allowed characters before use in LIKE clauses. The `SafeQueryBuilder` constructs all SQL with strict input validation.

---

### Threat 5: Timing Attacks on Decision Duration

**Risk Level:** Low
**CVSS Score:** 3.1 (Low)
**Attack Vector:** Information disclosure

**Description:**
Decision duration is recorded for analytics. An attacker analyzing this data might infer information about approval patterns—e.g., very short durations suggest auto-approval or inattentive review.

**Mitigation:** Duration data is used for aggregate analytics only. Individual duration values are not exposed in standard queries. The `DurationAnalyzer` applies differential privacy techniques when generating reports.

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

## Best Practices

### Record Design

- **BP-001: Immutable records** - Never modify approval records after creation; append corrections as new records
- **BP-002: Complete context capture** - Store all information needed to understand the decision without external lookups
- **BP-003: Consistent timestamps** - Use UTC timestamps for all records to avoid timezone confusion
- **BP-004: Unique identifiers** - Use UUID v7 for time-ordered, collision-free record IDs

### Storage Management

- **BP-005: Efficient indexing** - Index records by session, date, and operation type for common queries
- **BP-006: Retention policies** - Define clear retention rules and implement automatic cleanup
- **BP-007: Query optimization** - Aggregate statistics during write time, not query time
- **BP-008: Export formats** - Support both human-readable (CSV) and machine-readable (JSON) exports

### Audit Trail Integrity

- **BP-009: No gaps in recording** - Every approval decision must be persisted, even in error conditions
- **BP-010: Chronological ordering** - Records should be retrievable in decision order
- **BP-011: Session correlation** - All records link to their originating session
- **BP-012: Failure recording** - Record failed operations alongside their decisions

### Privacy and Security

- **BP-013: Minimal sensitive data** - Avoid storing file contents in approval records
- **BP-014: Access control consideration** - Record access should be limited to session owner
- **BP-015: Secure deletion** - Implement proper data deletion when retention expires
- **BP-016: Export security** - Warn users about sensitive data in exports

---

## Troubleshooting

### Record Storage Issues

#### Approval Not Recorded

**Symptom:** Approval was given but doesn't appear in history.

**Cause:** Database write failure or transaction rollback.

**Solution:**
1. Check database connectivity
2. Review logs for write errors
3. Verify disk space is available
4. Check for transaction deadlocks

#### Query Returns No Results

**Symptom:** `acode approvals list` shows no records when approvals were given.

**Cause:** Incorrect filter criteria or date range.

**Solution:**
1. Check date range parameters
2. Verify session ID is correct
3. Use broader filters to confirm records exist
4. Check if retention policy deleted old records

### Export Issues

#### Export File Empty

**Symptom:** Export command creates empty or minimal file.

**Cause:** No records match export criteria.

**Solution:**
1. Verify records exist with list command
2. Check export filter parameters
3. Expand date range for export

#### Export Format Invalid

**Symptom:** Exported JSON/CSV is malformed.

**Cause:** Special characters in operation descriptions.

**Solution:**
1. Verify encoding is UTF-8
2. Check for unescaped characters
3. Report issue if persists

### Statistics Issues

#### Statistics Don't Match Records

**Symptom:** Aggregate counts differ from individual record count.

**Cause:** Statistics calculated before recent records indexed.

**Solution:**
1. Wait for index update
2. Force reindex if available
3. Recalculate statistics from raw records

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