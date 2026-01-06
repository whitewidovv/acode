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

**Attack Scenario:**
1. Attacker gains filesystem access to workspace
2. Opens SQLite database directly
3. Updates record: `UPDATE approval_records SET decision = 'APPROVED' WHERE decision = 'DENIED'`
4. Malicious operations now appear to have been approved
5. Audit shows no evidence of denial

**Mitigation - Complete C# Implementation:**

```csharp
namespace AgenticCoder.Application.Approvals.Persistence.Security;

/// <summary>
/// Provides tamper detection for approval records using HMAC signatures.
/// Each record is signed at creation time, and signatures are verified on read.
/// </summary>
public sealed class ApprovalRecordIntegrityVerifier
{
    private readonly byte[] _signingKey;
    private readonly ILogger<ApprovalRecordIntegrityVerifier> _logger;

    public ApprovalRecordIntegrityVerifier(
        ISecretProvider secretProvider,
        ILogger<ApprovalRecordIntegrityVerifier> logger)
    {
        _signingKey = secretProvider.GetRecordSigningKey();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Computes HMAC-SHA256 signature for an approval record.
    /// </summary>
    public string ComputeSignature(ApprovalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        // Serialize record fields deterministically
        var dataToSign = SerializeForSigning(record);

        using var hmac = new HMACSHA256(_signingKey);
        var signatureBytes = hmac.ComputeHash(dataToSign);

        return Convert.ToBase64String(signatureBytes);
    }

    /// <summary>
    /// Verifies record integrity by comparing stored vs computed signature.
    /// </summary>
    public RecordIntegrityResult Verify(ApprovalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (string.IsNullOrEmpty(record.Signature))
        {
            _logger.LogCritical(
                "SECURITY: Approval record {RecordId} has no signature",
                record.Id);

            return RecordIntegrityResult.MissingSignature(record.Id);
        }

        try
        {
            var computedSignature = ComputeSignature(record);
            var isValid = CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(computedSignature),
                Convert.FromBase64String(record.Signature));

            if (!isValid)
            {
                _logger.LogCritical(
                    "SECURITY: Approval record {RecordId} signature mismatch. " +
                    "Record may have been tampered. Session={SessionId}, Decision={Decision}",
                    record.Id, record.SessionId, record.Decision);

                return RecordIntegrityResult.Tampered(record.Id, record.SessionId);
            }

            return RecordIntegrityResult.Valid();
        }
        catch (FormatException ex)
        {
            _logger.LogCritical(ex,
                "SECURITY: Invalid signature format for record {RecordId}",
                record.Id);

            return RecordIntegrityResult.InvalidSignature(record.Id);
        }
    }

    /// <summary>
    /// Verifies all records in a batch, returning any integrity violations.
    /// </summary>
    public BatchIntegrityResult VerifyBatch(IEnumerable<ApprovalRecord> records)
    {
        var violations = new List<RecordIntegrityResult>();
        var verifiedCount = 0;

        foreach (var record in records)
        {
            var result = Verify(record);
            verifiedCount++;

            if (!result.IsValid)
            {
                violations.Add(result);
            }
        }

        if (violations.Count > 0)
        {
            _logger.LogCritical(
                "SECURITY: Batch integrity check found {ViolationCount} tampered records " +
                "out of {TotalCount}",
                violations.Count, verifiedCount);
        }

        return new BatchIntegrityResult(verifiedCount, violations.AsReadOnly());
    }

    private static byte[] SerializeForSigning(ApprovalRecord record)
    {
        // Deterministic serialization - field order and format must be consistent
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        writer.Write(record.Id.ToByteArray());
        writer.Write(record.SessionId.ToByteArray());
        writer.Write(record.OperationId.ToByteArray());
        writer.Write((int)record.Category);
        writer.Write((int)record.Decision);
        writer.Write(record.DecidedAt.ToUnixTimeMilliseconds());
        writer.Write(record.MatchedRuleName ?? string.Empty);
        writer.Write(record.OperationDescription ?? string.Empty);

        return ms.ToArray();
    }
}

public sealed record RecordIntegrityResult
{
    public bool IsValid { get; init; }
    public Guid? RecordId { get; init; }
    public Guid? SessionId { get; init; }
    public string? ViolationType { get; init; }

    public static RecordIntegrityResult Valid() => new() { IsValid = true };

    public static RecordIntegrityResult Tampered(Guid recordId, Guid sessionId) => new()
    {
        IsValid = false,
        RecordId = recordId,
        SessionId = sessionId,
        ViolationType = "TAMPERED"
    };

    public static RecordIntegrityResult MissingSignature(Guid recordId) => new()
    {
        IsValid = false,
        RecordId = recordId,
        ViolationType = "MISSING_SIGNATURE"
    };

    public static RecordIntegrityResult InvalidSignature(Guid recordId) => new()
    {
        IsValid = false,
        RecordId = recordId,
        ViolationType = "INVALID_SIGNATURE_FORMAT"
    };
}

public sealed record BatchIntegrityResult(
    int TotalVerified,
    IReadOnlyList<RecordIntegrityResult> Violations)
{
    public bool AllValid => Violations.Count == 0;
    public int TamperedCount => Violations.Count;
}
```

**Testing Strategy:**
- Unit test: Verify signature computation is deterministic
- Unit test: Verify tampered record detection
- Integration test: Modify database directly, verify detection
- E2E test: Query records, verify all pass integrity check

---

### Threat 2: Sensitive Data Exposure in Records

**Risk Level:** High
**CVSS Score:** 7.1 (High)
**Attack Vector:** Data leakage

**Description:**
Approval records contain operation details that may include file paths, command strings, or file contents. If these contain sensitive data (credentials, PII, proprietary code), the audit trail becomes a target for data exfiltration.

**Attack Scenario:**
1. Developer runs Acode with a task that handles database credentials
2. Approval record stores operation description: "Execute: mysql -u admin -p'S3cretP@ss'"
3. Records synced to team PostgreSQL server
4. Attacker compromises team server
5. Extracts database credentials from approval records

**Mitigation - Complete C# Implementation:**

```csharp
namespace AgenticCoder.Application.Approvals.Persistence.Security;

/// <summary>
/// Sanitizes approval records before persistence to prevent sensitive data exposure.
/// Applies configurable redaction patterns to operation details.
/// </summary>
public sealed class RecordSanitizer
{
    private readonly ILogger<RecordSanitizer> _logger;
    private readonly IReadOnlyList<Regex> _redactionPatterns;

    private static readonly Regex[] DefaultPatterns = new[]
    {
        // Passwords in command lines
        new Regex(@"(-p|--password[=\s])['""]?([^\s'""]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),

        // API keys
        new Regex(@"(api[_-]?key|apikey)[=:]\s*['""]?[\w-]{20,}", RegexOptions.Compiled | RegexOptions.IgnoreCase),

        // Bearer tokens
        new Regex(@"(bearer\s+)[a-zA-Z0-9_\-\.]{20,}", RegexOptions.Compiled | RegexOptions.IgnoreCase),

        // AWS credentials
        new Regex(@"(AKIA|ABIA|ACCA|ASIA)[A-Z0-9]{16}", RegexOptions.Compiled),

        // Generic secrets
        new Regex(@"(secret|token|credential)[=:]\s*['""]?[^\s'""]{8,}", RegexOptions.Compiled | RegexOptions.IgnoreCase),

        // Connection strings
        new Regex(@"(password|pwd)=[^;]+", RegexOptions.Compiled | RegexOptions.IgnoreCase),

        // Private keys
        new Regex(@"-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----", RegexOptions.Compiled),

        // Environment variable values that look like secrets
        new Regex(@"(DB_PASSWORD|DATABASE_URL|SECRET_KEY|ENCRYPTION_KEY)=\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase)
    };

    public RecordSanitizer(
        ILogger<RecordSanitizer> logger,
        IEnumerable<Regex>? additionalPatterns = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var patterns = new List<Regex>(DefaultPatterns);
        if (additionalPatterns != null)
        {
            patterns.AddRange(additionalPatterns);
        }
        _redactionPatterns = patterns.AsReadOnly();
    }

    /// <summary>
    /// Sanitizes an approval record by redacting sensitive data.
    /// </summary>
    public SanitizedRecord Sanitize(ApprovalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var redactionCount = 0;
        var sanitizedDescription = RedactString(record.OperationDescription, ref redactionCount);
        var sanitizedDetails = SanitizeDetails(record.OperationDetails, ref redactionCount);

        if (redactionCount > 0)
        {
            _logger.LogInformation(
                "Sanitized approval record {RecordId}: {RedactionCount} redactions applied",
                record.Id, redactionCount);
        }

        return new SanitizedRecord(
            OriginalRecord: record,
            SanitizedDescription: sanitizedDescription,
            SanitizedDetails: sanitizedDetails,
            RedactionCount: redactionCount);
    }

    /// <summary>
    /// Validates that a record contains no sensitive data.
    /// Returns true if safe to persist, false if redaction needed.
    /// </summary>
    public bool IsSafe(ApprovalRecord record)
    {
        if (ContainsSensitiveData(record.OperationDescription))
            return false;

        if (record.OperationDetails != null)
        {
            foreach (var value in record.OperationDetails.Values)
            {
                if (value is string str && ContainsSensitiveData(str))
                    return false;
            }
        }

        return true;
    }

    private string RedactString(string? input, ref int redactionCount)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        var result = input;
        foreach (var pattern in _redactionPatterns)
        {
            result = pattern.Replace(result, match =>
            {
                redactionCount++;
                return "[REDACTED]";
            });
        }
        return result;
    }

    private IReadOnlyDictionary<string, object>? SanitizeDetails(
        IReadOnlyDictionary<string, object>? details,
        ref int redactionCount)
    {
        if (details == null || details.Count == 0)
            return details;

        var sanitized = new Dictionary<string, object>();
        foreach (var (key, value) in details)
        {
            // Never store file contents
            if (key.Equals("content", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("file_content", StringComparison.OrdinalIgnoreCase))
            {
                sanitized[key] = "[CONTENT_EXCLUDED]";
                redactionCount++;
                continue;
            }

            sanitized[key] = value switch
            {
                string str => RedactString(str, ref redactionCount),
                _ => value
            };
        }

        return sanitized.AsReadOnly();
    }

    private bool ContainsSensitiveData(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        return _redactionPatterns.Any(p => p.IsMatch(input));
    }
}

public sealed record SanitizedRecord(
    ApprovalRecord OriginalRecord,
    string SanitizedDescription,
    IReadOnlyDictionary<string, object>? SanitizedDetails,
    int RedactionCount)
{
    public bool WasModified => RedactionCount > 0;
}
```

**Testing Strategy:**
- Unit test: Verify each default pattern redacts correctly
- Unit test: Verify file contents are always excluded
- Integration test: Persist record with secrets, verify redacted in storage
- E2E test: Export records, verify no sensitive data in output

---

### Threat 3: Storage Exhaustion via Record Flooding

**Risk Level:** Medium
**CVSS Score:** 5.3 (Medium)
**Attack Vector:** Resource exhaustion

**Description:**
An attacker could craft tasks that generate millions of operations, creating millions of approval records. This could exhaust disk space, slow queries to unusable levels, or cause out-of-memory conditions.

**Attack Scenario:**
1. Attacker creates task that generates 100,000 file operations
2. Each operation creates approval record (even if auto-approved)
3. Database grows to 5GB in single session
4. Disk fills, subsequent writes fail
5. All Acode sessions crash, data potentially corrupted

**Mitigation - Complete C# Implementation:**

```csharp
namespace AgenticCoder.Application.Approvals.Persistence.Security;

/// <summary>
/// Guards against storage exhaustion by enforcing record limits.
/// Monitors storage usage and blocks new records when limits exceeded.
/// </summary>
public sealed class ApprovalStorageGuard
{
    private readonly IApprovalRecordRepository _repository;
    private readonly ILogger<ApprovalStorageGuard> _logger;
    private readonly StorageLimits _limits;

    // Track session record counts in memory for fast checking
    private readonly ConcurrentDictionary<Guid, int> _sessionCounts = new();
    private int _dailyCount;
    private DateOnly _currentDay;
    private readonly object _dailyLock = new();

    public ApprovalStorageGuard(
        IApprovalRecordRepository repository,
        IOptions<StorageLimits> limits,
        ILogger<ApprovalStorageGuard> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _limits = limits?.Value ?? throw new ArgumentNullException(nameof(limits));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentDay = DateOnly.FromDateTime(DateTime.UtcNow);
    }

    /// <summary>
    /// Checks if a new record can be stored for the given session.
    /// </summary>
    public StorageCheckResult CanStore(Guid sessionId)
    {
        // Check session limit
        var sessionCount = _sessionCounts.GetOrAdd(sessionId, 0);
        if (sessionCount >= _limits.MaxRecordsPerSession)
        {
            _logger.LogWarning(
                "Storage guard: Session {SessionId} exceeded limit of {Limit} records",
                sessionId, _limits.MaxRecordsPerSession);

            return StorageCheckResult.SessionLimitExceeded(sessionId, sessionCount, _limits.MaxRecordsPerSession);
        }

        // Check daily limit
        lock (_dailyLock)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (today != _currentDay)
            {
                _currentDay = today;
                _dailyCount = 0;
            }

            if (_dailyCount >= _limits.MaxRecordsPerDay)
            {
                _logger.LogWarning(
                    "Storage guard: Daily limit of {Limit} records exceeded",
                    _limits.MaxRecordsPerDay);

                return StorageCheckResult.DailyLimitExceeded(_dailyCount, _limits.MaxRecordsPerDay);
            }
        }

        return StorageCheckResult.Allowed();
    }

    /// <summary>
    /// Records that a new record was stored, updating counters.
    /// </summary>
    public void RecordStored(Guid sessionId)
    {
        _sessionCounts.AddOrUpdate(sessionId, 1, (_, count) => count + 1);

        lock (_dailyLock)
        {
            _dailyCount++;
        }
    }

    /// <summary>
    /// Cleanup when session ends, removing from session tracking.
    /// </summary>
    public void SessionEnded(Guid sessionId)
    {
        _sessionCounts.TryRemove(sessionId, out _);
    }

    /// <summary>
    /// Gets current storage statistics.
    /// </summary>
    public async Task<StorageStatistics> GetStatisticsAsync(CancellationToken ct)
    {
        var totalRecords = await _repository.GetTotalCountAsync(ct);
        var oldestRecord = await _repository.GetOldestRecordDateAsync(ct);
        var estimatedSizeBytes = totalRecords * 500; // ~500 bytes per record estimate

        return new StorageStatistics(
            TotalRecords: totalRecords,
            OldestRecord: oldestRecord,
            EstimatedSizeBytes: estimatedSizeBytes,
            ActiveSessions: _sessionCounts.Count,
            DailyCount: _dailyCount);
    }
}

public sealed record StorageCheckResult
{
    public bool IsAllowed { get; init; }
    public string? RejectionReason { get; init; }
    public Guid? SessionId { get; init; }
    public int? CurrentCount { get; init; }
    public int? Limit { get; init; }

    public static StorageCheckResult Allowed() => new() { IsAllowed = true };

    public static StorageCheckResult SessionLimitExceeded(Guid sessionId, int current, int limit) => new()
    {
        IsAllowed = false,
        RejectionReason = $"Session record limit exceeded ({current}/{limit})",
        SessionId = sessionId,
        CurrentCount = current,
        Limit = limit
    };

    public static StorageCheckResult DailyLimitExceeded(int current, int limit) => new()
    {
        IsAllowed = false,
        RejectionReason = $"Daily record limit exceeded ({current}/{limit})",
        CurrentCount = current,
        Limit = limit
    };
}

public sealed record StorageLimits
{
    public int MaxRecordsPerSession { get; init; } = 10_000;
    public int MaxRecordsPerDay { get; init; } = 100_000;
    public long MaxStorageBytes { get; init; } = 1_073_741_824; // 1GB
}

public sealed record StorageStatistics(
    long TotalRecords,
    DateTimeOffset? OldestRecord,
    long EstimatedSizeBytes,
    int ActiveSessions,
    int DailyCount);
```

**Testing Strategy:**
- Unit test: Verify session limit enforcement
- Unit test: Verify daily limit enforcement
- Unit test: Verify day rollover resets daily count
- Integration test: Create records until limit, verify rejection
- Performance test: Verify overhead of limit checking < 1ms

---

### Threat 4: Query Injection via Malformed Filters

**Risk Level:** Medium
**CVSS Score:** 5.9 (Medium)
**Attack Vector:** SQL injection

**Description:**
Query interfaces accept user-provided filters (path patterns, rule names, etc.). Malformed input could be crafted to inject SQL, bypassing query constraints or extracting unauthorized data.

**Attack Scenario:**
1. Attacker calls: `acode approvals list --path "'; DROP TABLE approval_records; --"`
2. Vulnerable code builds: `SELECT * FROM approval_records WHERE path LIKE '; DROP TABLE...`
3. Database executes injected SQL
4. Approval records deleted, audit trail destroyed

**Mitigation - Complete C# Implementation:**

```csharp
namespace AgenticCoder.Application.Approvals.Persistence.Queries;

/// <summary>
/// Builds safe, parameterized queries for approval record retrieval.
/// Never concatenates user input into SQL strings.
/// </summary>
public sealed class SafeQueryBuilder
{
    private readonly ILogger<SafeQueryBuilder> _logger;

    // Allowed characters in path patterns (for LIKE queries)
    private static readonly Regex SafePathPatternRegex = new(
        @"^[\w\.\-\/\*\?\[\]]+$",
        RegexOptions.Compiled);

    // Allowed characters in rule names
    private static readonly Regex SafeRuleNameRegex = new(
        @"^[\w\-]{1,100}$",
        RegexOptions.Compiled);

    public SafeQueryBuilder(ILogger<SafeQueryBuilder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Builds a parameterized query for approval records.
    /// </summary>
    public QueryDefinition Build(ApprovalRecordQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sql = new StringBuilder("SELECT * FROM approval_records WHERE 1=1");
        var parameters = new DynamicParameters();

        // Session ID filter (exact match, parameterized)
        if (query.SessionId.HasValue)
        {
            sql.Append(" AND session_id = @SessionId");
            parameters.Add("SessionId", query.SessionId.Value.ToString());
        }

        // Decision filter (enum, validated)
        if (query.Decision.HasValue)
        {
            if (!Enum.IsDefined(query.Decision.Value))
            {
                throw new QueryValidationException($"Invalid decision value: {query.Decision.Value}");
            }
            sql.Append(" AND decision = @Decision");
            parameters.Add("Decision", query.Decision.Value.ToString());
        }

        // Operation category filter (enum, validated)
        if (query.Category.HasValue)
        {
            if (!Enum.IsDefined(query.Category.Value))
            {
                throw new QueryValidationException($"Invalid category value: {query.Category.Value}");
            }
            sql.Append(" AND operation_category = @Category");
            parameters.Add("Category", query.Category.Value.ToString());
        }

        // Path pattern filter (validated, parameterized with escape)
        if (!string.IsNullOrEmpty(query.PathPattern))
        {
            ValidatePathPattern(query.PathPattern);
            var likePattern = ConvertGlobToLike(query.PathPattern);
            sql.Append(" AND operation_path LIKE @PathPattern ESCAPE '\\'");
            parameters.Add("PathPattern", likePattern);
        }

        // Rule name filter (validated, parameterized)
        if (!string.IsNullOrEmpty(query.RuleName))
        {
            ValidateRuleName(query.RuleName);
            sql.Append(" AND matched_rule_name = @RuleName");
            parameters.Add("RuleName", query.RuleName);
        }

        // Time range filters (parameterized)
        if (query.StartTime.HasValue)
        {
            sql.Append(" AND decided_at >= @StartTime");
            parameters.Add("StartTime", query.StartTime.Value.ToString("O"));
        }

        if (query.EndTime.HasValue)
        {
            sql.Append(" AND decided_at <= @EndTime");
            parameters.Add("EndTime", query.EndTime.Value.ToString("O"));
        }

        // Ordering (validated enum, not user string)
        var orderColumn = query.OrderBy switch
        {
            ApprovalRecordOrderBy.DecidedAt => "decided_at",
            ApprovalRecordOrderBy.SessionId => "session_id",
            ApprovalRecordOrderBy.Decision => "decision",
            _ => "decided_at"
        };
        var orderDir = query.OrderDescending ? "DESC" : "ASC";
        sql.Append($" ORDER BY {orderColumn} {orderDir}");

        // Pagination (validated integers)
        if (query.PageSize <= 0 || query.PageSize > 1000)
        {
            throw new QueryValidationException($"Page size must be between 1 and 1000");
        }
        if (query.Page < 1)
        {
            throw new QueryValidationException($"Page must be >= 1");
        }

        sql.Append(" LIMIT @Limit OFFSET @Offset");
        parameters.Add("Limit", query.PageSize);
        parameters.Add("Offset", (query.Page - 1) * query.PageSize);

        return new QueryDefinition(sql.ToString(), parameters);
    }

    private void ValidatePathPattern(string pattern)
    {
        if (!SafePathPatternRegex.IsMatch(pattern))
        {
            _logger.LogWarning(
                "SECURITY: Rejected invalid path pattern: {Pattern}",
                pattern);
            throw new QueryValidationException(
                $"Path pattern contains invalid characters. Allowed: alphanumeric, . - / * ? [ ]");
        }
    }

    private void ValidateRuleName(string ruleName)
    {
        if (!SafeRuleNameRegex.IsMatch(ruleName))
        {
            _logger.LogWarning(
                "SECURITY: Rejected invalid rule name: {RuleName}",
                ruleName);
            throw new QueryValidationException(
                $"Rule name contains invalid characters. Allowed: alphanumeric, - (max 100 chars)");
        }
    }

    private static string ConvertGlobToLike(string globPattern)
    {
        // Escape SQL LIKE special characters
        var escaped = globPattern
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

        // Convert glob wildcards to SQL LIKE wildcards
        return escaped
            .Replace("*", "%")
            .Replace("?", "_");
    }
}

public sealed record QueryDefinition(string Sql, DynamicParameters Parameters);

public sealed record ApprovalRecordQuery
{
    public Guid? SessionId { get; init; }
    public ApprovalDecision? Decision { get; init; }
    public OperationCategory? Category { get; init; }
    public string? PathPattern { get; init; }
    public string? RuleName { get; init; }
    public DateTimeOffset? StartTime { get; init; }
    public DateTimeOffset? EndTime { get; init; }
    public ApprovalRecordOrderBy OrderBy { get; init; } = ApprovalRecordOrderBy.DecidedAt;
    public bool OrderDescending { get; init; } = true;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public enum ApprovalRecordOrderBy { DecidedAt, SessionId, Decision }

public sealed class QueryValidationException : Exception
{
    public QueryValidationException(string message) : base(message) { }
}
```

**Testing Strategy:**
- Unit test: Verify SQL injection attempts are rejected
- Unit test: Verify path pattern validation
- Unit test: Verify glob-to-LIKE conversion
- Integration test: Execute queries with edge-case patterns
- Security test: Fuzz testing with malicious inputs

---

### Threat 5: Timing Attacks on Decision Duration

**Risk Level:** Low
**CVSS Score:** 3.1 (Low)
**Attack Vector:** Information disclosure

**Description:**
Decision duration is recorded for analytics. An attacker analyzing this data might infer information about approval patterns—e.g., very short durations suggest auto-approval or inattentive review.

**Attack Scenario:**
1. Attacker exports approval records for analysis
2. Notices pattern: durations < 100ms are always AUTO_APPROVED
3. Durations 5-30s are APPROVED (human review)
4. Durations > 60s are typically DENIED (careful consideration)
5. Attacker tailors future attacks to avoid operations that trigger long review times

**Mitigation - Complete C# Implementation:**

```csharp
namespace AgenticCoder.Application.Approvals.Persistence.Analytics;

/// <summary>
/// Provides privacy-preserving analytics for approval decision durations.
/// Applies differential privacy and aggregation to prevent timing analysis.
/// </summary>
public sealed class DurationAnalyzer
{
    private readonly ILogger<DurationAnalyzer> _logger;
    private readonly Random _random = new();

    // Minimum bucket size to prevent individual identification
    private const int MinBucketSize = 10;

    // Noise scale for differential privacy (epsilon = 1.0)
    private const double NoiseScale = 1.0;

    public DurationAnalyzer(ILogger<DurationAnalyzer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates privacy-preserving duration statistics.
    /// Individual durations are never exposed.
    /// </summary>
    public DurationStatistics ComputeStatistics(IEnumerable<ApprovalRecord> records)
    {
        var durations = records
            .Where(r => r.DecisionDuration.HasValue)
            .Select(r => r.DecisionDuration!.Value.TotalMilliseconds)
            .ToList();

        if (durations.Count < MinBucketSize)
        {
            _logger.LogInformation(
                "Insufficient data for duration statistics ({Count} < {Min})",
                durations.Count, MinBucketSize);

            return DurationStatistics.InsufficientData();
        }

        // Compute aggregates with added noise
        var mean = AddNoise(durations.Average());
        var median = AddNoise(ComputeMedian(durations));
        var stdDev = AddNoise(ComputeStdDev(durations, mean));

        // Bucket into ranges (rounded to prevent exact duration inference)
        var buckets = BucketDurations(durations);

        return new DurationStatistics(
            SampleCount: durations.Count,
            MeanMs: RoundToNearest(mean, 100), // Round to nearest 100ms
            MedianMs: RoundToNearest(median, 100),
            StdDevMs: RoundToNearest(stdDev, 50),
            Buckets: buckets);
    }

    /// <summary>
    /// Gets aggregate duration stats by decision type.
    /// Does not expose individual decision durations.
    /// </summary>
    public IReadOnlyDictionary<ApprovalDecision, DurationStatistics> ComputeByDecision(
        IEnumerable<ApprovalRecord> records)
    {
        return records
            .GroupBy(r => r.Decision)
            .Where(g => g.Count() >= MinBucketSize)
            .ToDictionary(
                g => g.Key,
                g => ComputeStatistics(g));
    }

    /// <summary>
    /// Determines if a duration can be safely exposed.
    /// Only aggregate data passes this check.
    /// </summary>
    public bool CanExposeDuration(ExposureContext context)
    {
        // Individual durations are never exposed
        if (context.IsIndividualRecord)
            return false;

        // Aggregates over minimum size are ok
        if (context.AggregateSize >= MinBucketSize)
            return true;

        return false;
    }

    private double AddNoise(double value)
    {
        // Laplace noise for differential privacy
        var u = _random.NextDouble() - 0.5;
        var noise = -NoiseScale * Math.Sign(u) * Math.Log(1 - 2 * Math.Abs(u));
        return value + noise;
    }

    private static double ComputeMedian(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }

    private static double ComputeStdDev(List<double> values, double mean)
    {
        var sumSquares = values.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumSquares / values.Count);
    }

    private static double RoundToNearest(double value, double nearest)
    {
        return Math.Round(value / nearest) * nearest;
    }

    private IReadOnlyDictionary<string, int> BucketDurations(List<double> durations)
    {
        var buckets = new Dictionary<string, int>
        {
            { "< 1s", 0 },
            { "1-5s", 0 },
            { "5-15s", 0 },
            { "15-30s", 0 },
            { "30-60s", 0 },
            { "> 60s", 0 }
        };

        foreach (var ms in durations)
        {
            var bucket = ms switch
            {
                < 1000 => "< 1s",
                < 5000 => "1-5s",
                < 15000 => "5-15s",
                < 30000 => "15-30s",
                < 60000 => "30-60s",
                _ => "> 60s"
            };
            buckets[bucket]++;
        }

        // Add noise to bucket counts
        foreach (var key in buckets.Keys.ToList())
        {
            buckets[key] = Math.Max(0, (int)AddNoise(buckets[key]));
        }

        return buckets.AsReadOnly();
    }
}

public sealed record DurationStatistics
{
    public int SampleCount { get; init; }
    public double MeanMs { get; init; }
    public double MedianMs { get; init; }
    public double StdDevMs { get; init; }
    public IReadOnlyDictionary<string, int>? Buckets { get; init; }
    public bool HasSufficientData { get; init; } = true;

    public static DurationStatistics InsufficientData() => new()
    {
        HasSufficientData = false,
        SampleCount = 0
    };
}

public sealed record ExposureContext(bool IsIndividualRecord, int AggregateSize);
```

**Testing Strategy:**
- Unit test: Verify noise is added to aggregates
- Unit test: Verify individual durations are never exposed
- Unit test: Verify minimum bucket size enforcement
- Privacy test: Verify cannot reconstruct individual durations from aggregates
- Statistical test: Verify noise distribution is correct

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

- [ ] AC-001: ApprovalRecord has unique ULID identifier generated on creation
- [ ] AC-002: Record stores SessionId as GUID linking to originating session
- [ ] AC-003: Record stores OperationCategory enum (FileRead, FileWrite, FileDelete, TerminalCommand, etc.)
- [ ] AC-004: Record stores OperationDescription as sanitized string (max 1000 chars)
- [ ] AC-005: Record stores OperationPath when applicable (file operations)
- [ ] AC-006: Record stores OperationDetails as JSON dictionary for extensibility
- [ ] AC-007: Record stores Decision enum (Approved, Denied, Skipped, Timeout, AutoApproved)
- [ ] AC-008: Record stores DecidedAt as UTC DateTimeOffset with millisecond precision
- [ ] AC-009: Record stores MatchedRuleName identifying which rule determined decision
- [ ] AC-010: Record stores UserReason when user provides denial/skip reason
- [ ] AC-011: Record stores DecisionDuration as TimeSpan for analytics
- [ ] AC-012: Record stores HMAC signature for tamper detection

### Decision Type Persistence

- [ ] AC-013: APPROVED decision type persists with correct enum value
- [ ] AC-014: DENIED decision type persists with user reason when provided
- [ ] AC-015: SKIPPED decision type persists when user skips operation
- [ ] AC-016: TIMEOUT decision type persists with duration before timeout
- [ ] AC-017: AUTO_APPROVED decision type persists with rule name that triggered auto-approval

### Create Operations

- [ ] AC-018: CreateAsync persists record to SQLite and returns record ID
- [ ] AC-019: CreateAsync is atomic - record fully persisted or not at all
- [ ] AC-020: CreateAsync computes and stores HMAC signature before persistence
- [ ] AC-021: CreateAsync sanitizes sensitive data via RecordSanitizer before persistence
- [ ] AC-022: CreateAsync validates storage limits via ApprovalStorageGuard
- [ ] AC-023: CreateAsync rejects if session limit (10,000) exceeded
- [ ] AC-024: CreateAsync rejects if daily limit (100,000) exceeded
- [ ] AC-025: CreateAsync logs record creation with record ID and session ID

### Immutability

- [ ] AC-026: No UpdateAsync method exists - records are immutable
- [ ] AC-027: Attempting to modify record throws ImmutableRecordException
- [ ] AC-028: Soft delete marks IsDeleted=true, does not modify content
- [ ] AC-029: Correction appends new record, does not modify original

### Query Operations

- [ ] AC-030: GetByIdAsync returns single record by ULID
- [ ] AC-031: GetBySessionIdAsync returns all records for session in chronological order
- [ ] AC-032: QueryAsync supports filtering by Decision type
- [ ] AC-033: QueryAsync supports filtering by OperationCategory
- [ ] AC-034: QueryAsync supports filtering by time range (StartTime, EndTime)
- [ ] AC-035: QueryAsync supports filtering by MatchedRuleName
- [ ] AC-036: QueryAsync supports filtering by path pattern (glob syntax)
- [ ] AC-037: QueryAsync supports combining multiple filters with AND logic
- [ ] AC-038: QueryAsync uses SafeQueryBuilder for all filter construction

### Pagination

- [ ] AC-039: Query results are paginated with default page size of 50
- [ ] AC-040: Page size is configurable from 1 to 1000
- [ ] AC-041: Page numbers start at 1 (not 0-indexed)
- [ ] AC-042: Response includes TotalCount for UI pagination controls
- [ ] AC-043: Response includes HasNextPage boolean

### Ordering

- [ ] AC-044: Default ordering is DecidedAt descending (newest first)
- [ ] AC-045: Ordering can be changed to ascending (oldest first)
- [ ] AC-046: Ordering supports DecidedAt, SessionId, Decision columns
- [ ] AC-047: Order column comes from validated enum, not user string

### Aggregation

- [ ] AC-048: CountByDecisionAsync returns counts grouped by Decision type
- [ ] AC-049: CountByCategoryAsync returns counts grouped by OperationCategory
- [ ] AC-050: CountByRuleAsync returns counts grouped by MatchedRuleName
- [ ] AC-051: CountByDayAsync returns counts grouped by calendar day
- [ ] AC-052: Aggregations use DurationAnalyzer for privacy-preserving duration stats

### Storage Integration

- [ ] AC-053: SQLite repository implements IApprovalRecordRepository
- [ ] AC-054: SQLite uses WAL mode for concurrent access
- [ ] AC-055: PostgreSQL repository implements IApprovalRecordRepository
- [ ] AC-056: PostgreSQL uses connection pooling
- [ ] AC-057: Both repositories use identical interface with storage-specific implementations

### Sync Operations

- [ ] AC-058: Outbox table stores pending sync records
- [ ] AC-059: Sync service polls outbox and pushes to remote
- [ ] AC-060: Failed sync records are retried with exponential backoff
- [ ] AC-061: Max retry attempts configurable (default 5)
- [ ] AC-062: Conflict resolution uses "latest wins" based on DecidedAt

### Security

- [ ] AC-063: ApprovalRecordIntegrityVerifier validates HMAC on record read
- [ ] AC-064: Tampered records are flagged and logged
- [ ] AC-065: RecordSanitizer redacts passwords, API keys, tokens before persistence
- [ ] AC-066: ApprovalStorageGuard enforces session and daily limits
- [ ] AC-067: SafeQueryBuilder prevents SQL injection in all queries
- [ ] AC-068: DurationAnalyzer applies differential privacy to duration stats

### Privacy and Retention

- [ ] AC-069: Default retention period is 90 days
- [ ] AC-070: Retention period is configurable in .agent/config.yml
- [ ] AC-071: Cleanup job runs daily to delete expired records
- [ ] AC-072: Deletion is permanent (not soft delete) for retention cleanup
- [ ] AC-073: Sync respects local redaction settings
- [ ] AC-074: Remote storage may apply additional redaction

### CLI Commands

- [ ] AC-075: `acode approvals list` shows paginated records with filters
- [ ] AC-076: `acode approvals show <id>` shows single record details
- [ ] AC-077: `acode approvals delete <id>` soft-deletes single record
- [ ] AC-078: `acode approvals export` exports to JSON or CSV format
- [ ] AC-079: `acode approvals stats` shows aggregated statistics

### Performance

- [ ] AC-080: Record creation completes in < 10ms
- [ ] AC-081: Query with single filter completes in < 50ms for 10,000 records
- [ ] AC-082: Query with pagination completes in < 100ms for any page
- [ ] AC-083: Storage guard check overhead < 1ms

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

```csharp
namespace AgenticCoder.Application.Tests.Unit.Approvals.Persistence;

public class ApprovalRecordTests
{
    [Fact]
    public void Should_Create_Valid_Record()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var category = OperationCategory.FileWrite;
        var description = "Write src/test.ts";
        var decision = ApprovalDecision.Approved;

        // Act
        var record = new ApprovalRecord(
            sessionId: sessionId,
            category: category,
            description: description,
            decision: decision,
            matchedRuleName: "allow-tests");

        // Assert
        Assert.NotEqual(Ulid.Empty, record.Id);
        Assert.Equal(sessionId, record.SessionId);
        Assert.Equal(category, record.Category);
        Assert.Equal(description, record.OperationDescription);
        Assert.Equal(decision, record.Decision);
        Assert.NotEqual(default, record.DecidedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Null_Or_Empty_Description(string? description)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ApprovalRecord(
            sessionId: Guid.NewGuid(),
            category: OperationCategory.FileWrite,
            description: description!,
            decision: ApprovalDecision.Approved,
            matchedRuleName: "rule"));
    }

    [Fact]
    public void Should_Truncate_Description_Over_1000_Chars()
    {
        // Arrange
        var longDescription = new string('x', 2000);

        // Act
        var record = new ApprovalRecord(
            sessionId: Guid.NewGuid(),
            category: OperationCategory.FileWrite,
            description: longDescription,
            decision: ApprovalDecision.Approved,
            matchedRuleName: "rule");

        // Assert
        Assert.Equal(1000, record.OperationDescription.Length);
    }

    [Fact]
    public void Should_Generate_Unique_Ids()
    {
        // Arrange & Act
        var ids = Enumerable.Range(0, 1000)
            .Select(_ => new ApprovalRecord(
                sessionId: Guid.NewGuid(),
                category: OperationCategory.FileRead,
                description: "test",
                decision: ApprovalDecision.Approved,
                matchedRuleName: "rule").Id)
            .ToList();

        // Assert
        Assert.Equal(1000, ids.Distinct().Count());
    }
}

public class ApprovalRecordIntegrityVerifierTests
{
    private readonly ApprovalRecordIntegrityVerifier _verifier;

    public ApprovalRecordIntegrityVerifierTests()
    {
        var mockSecretProvider = new Mock<ISecretProvider>();
        mockSecretProvider.Setup(p => p.GetRecordSigningKey())
            .Returns(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });

        _verifier = new ApprovalRecordIntegrityVerifier(
            mockSecretProvider.Object,
            NullLogger<ApprovalRecordIntegrityVerifier>.Instance);
    }

    [Fact]
    public void Should_Compute_Deterministic_Signature()
    {
        // Arrange
        var record = CreateTestRecord();

        // Act
        var sig1 = _verifier.ComputeSignature(record);
        var sig2 = _verifier.ComputeSignature(record);

        // Assert
        Assert.Equal(sig1, sig2);
    }

    [Fact]
    public void Should_Detect_Tampered_Decision()
    {
        // Arrange
        var record = CreateTestRecord();
        record.Signature = _verifier.ComputeSignature(record);

        // Tamper with decision
        var tamperedRecord = record with { Decision = ApprovalDecision.Denied };
        tamperedRecord.Signature = record.Signature; // Keep original signature

        // Act
        var result = _verifier.Verify(tamperedRecord);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("TAMPERED", result.ViolationType);
    }

    [Fact]
    public void Should_Accept_Valid_Record()
    {
        // Arrange
        var record = CreateTestRecord();
        record.Signature = _verifier.ComputeSignature(record);

        // Act
        var result = _verifier.Verify(record);

        // Assert
        Assert.True(result.IsValid);
    }

    private static ApprovalRecord CreateTestRecord() => new(
        sessionId: Guid.Parse("12345678-1234-1234-1234-123456789012"),
        category: OperationCategory.FileWrite,
        description: "Write test.txt",
        decision: ApprovalDecision.Approved,
        matchedRuleName: "test-rule");
}

public class RecordSanitizerTests
{
    private readonly RecordSanitizer _sanitizer;

    public RecordSanitizerTests()
    {
        _sanitizer = new RecordSanitizer(NullLogger<RecordSanitizer>.Instance);
    }

    [Theory]
    [InlineData("mysql -u admin -pS3cret", "[REDACTED]")]
    [InlineData("api_key=sk_live_abc123def456", "[REDACTED]")]
    [InlineData("Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", "[REDACTED]")]
    [InlineData("password=hunter2", "[REDACTED]")]
    public void Should_Redact_Sensitive_Patterns(string input, string expectedContains)
    {
        // Arrange
        var record = new ApprovalRecord(
            sessionId: Guid.NewGuid(),
            category: OperationCategory.TerminalCommand,
            description: input,
            decision: ApprovalDecision.Approved,
            matchedRuleName: "rule");

        // Act
        var sanitized = _sanitizer.Sanitize(record);

        // Assert
        Assert.Contains(expectedContains, sanitized.SanitizedDescription);
        Assert.True(sanitized.WasModified);
    }

    [Fact]
    public void Should_Not_Modify_Safe_Content()
    {
        // Arrange
        var record = new ApprovalRecord(
            sessionId: Guid.NewGuid(),
            category: OperationCategory.FileWrite,
            description: "Write src/components/Button.tsx",
            decision: ApprovalDecision.Approved,
            matchedRuleName: "rule");

        // Act
        var sanitized = _sanitizer.Sanitize(record);

        // Assert
        Assert.False(sanitized.WasModified);
        Assert.Equal(record.OperationDescription, sanitized.SanitizedDescription);
    }
}

public class ApprovalStorageGuardTests
{
    [Fact]
    public void Should_Allow_Within_Session_Limit()
    {
        // Arrange
        var guard = CreateGuard(maxPerSession: 100);
        var sessionId = Guid.NewGuid();

        // Act
        var result = guard.CanStore(sessionId);

        // Assert
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Should_Block_At_Session_Limit()
    {
        // Arrange
        var guard = CreateGuard(maxPerSession: 3);
        var sessionId = Guid.NewGuid();

        // Record 3 stores
        guard.RecordStored(sessionId);
        guard.RecordStored(sessionId);
        guard.RecordStored(sessionId);

        // Act
        var result = guard.CanStore(sessionId);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Contains("limit exceeded", result.RejectionReason);
    }

    private static ApprovalStorageGuard CreateGuard(int maxPerSession = 10000)
    {
        var mockRepo = new Mock<IApprovalRecordRepository>();
        var limits = Options.Create(new StorageLimits { MaxRecordsPerSession = maxPerSession });
        return new ApprovalStorageGuard(mockRepo.Object, limits,
            NullLogger<ApprovalStorageGuard>.Instance);
    }
}

public class SafeQueryBuilderTests
{
    private readonly SafeQueryBuilder _builder;

    public SafeQueryBuilderTests()
    {
        _builder = new SafeQueryBuilder(NullLogger<SafeQueryBuilder>.Instance);
    }

    [Fact]
    public void Should_Build_Parameterized_Query()
    {
        // Arrange
        var query = new ApprovalRecordQuery
        {
            SessionId = Guid.NewGuid(),
            Decision = ApprovalDecision.Approved
        };

        // Act
        var result = _builder.Build(query);

        // Assert
        Assert.Contains("@SessionId", result.Sql);
        Assert.Contains("@Decision", result.Sql);
        Assert.DoesNotContain(query.SessionId.ToString(), result.Sql);
    }

    [Theory]
    [InlineData("'; DROP TABLE --")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("${malicious}")]
    public void Should_Reject_Malicious_Path_Pattern(string pattern)
    {
        // Arrange
        var query = new ApprovalRecordQuery { PathPattern = pattern };

        // Act & Assert
        Assert.Throws<QueryValidationException>(() => _builder.Build(query));
    }

    [Fact]
    public void Should_Convert_Glob_To_Like()
    {
        // Arrange
        var query = new ApprovalRecordQuery { PathPattern = "src/**/*.ts" };

        // Act
        var result = _builder.Build(query);

        // Assert
        Assert.Contains("LIKE", result.Sql);
        Assert.Contains("@PathPattern", result.Sql);
    }
}
```

### Integration Tests

```csharp
namespace AgenticCoder.Application.Tests.Integration.Approvals.Persistence;

public class SqliteApprovalRepositoryTests : IClassFixture<SqliteTestFixture>
{
    private readonly SqliteTestFixture _fixture;
    private readonly IApprovalRecordRepository _repository;

    public SqliteApprovalRepositoryTests(SqliteTestFixture fixture)
    {
        _fixture = fixture;
        _repository = fixture.GetService<IApprovalRecordRepository>();
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Record()
    {
        // Arrange
        var record = CreateTestRecord();

        // Act
        var id = await _repository.CreateAsync(record, CancellationToken.None);
        var retrieved = await _repository.GetByIdAsync(id, CancellationToken.None);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(record.SessionId, retrieved.SessionId);
        Assert.Equal(record.Decision, retrieved.Decision);
    }

    [Fact]
    public async Task Should_Query_By_Session()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var record1 = CreateTestRecord(sessionId);
        var record2 = CreateTestRecord(sessionId);
        var otherRecord = CreateTestRecord(Guid.NewGuid());

        await _repository.CreateAsync(record1, CancellationToken.None);
        await _repository.CreateAsync(record2, CancellationToken.None);
        await _repository.CreateAsync(otherRecord, CancellationToken.None);

        // Act
        var results = await _repository.GetBySessionIdAsync(sessionId, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(sessionId, r.SessionId));
    }

    [Fact]
    public async Task Should_Paginate_Results()
    {
        // Arrange - Create 25 records
        var sessionId = Guid.NewGuid();
        for (int i = 0; i < 25; i++)
        {
            await _repository.CreateAsync(CreateTestRecord(sessionId), CancellationToken.None);
        }

        var query = new ApprovalRecordQuery
        {
            SessionId = sessionId,
            PageSize = 10,
            Page = 1
        };

        // Act
        var page1 = await _repository.QueryAsync(query, CancellationToken.None);
        query = query with { Page = 2 };
        var page2 = await _repository.QueryAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(10, page2.Items.Count);
        Assert.True(page1.HasNextPage);
        Assert.Equal(25, page1.TotalCount);
    }

    private static ApprovalRecord CreateTestRecord(Guid? sessionId = null) => new(
        sessionId: sessionId ?? Guid.NewGuid(),
        category: OperationCategory.FileWrite,
        description: $"Write test-{Guid.NewGuid():N}.txt",
        decision: ApprovalDecision.Approved,
        matchedRuleName: "test-rule");
}

public class SyncIntegrationTests : IClassFixture<SyncTestFixture>
{
    private readonly SyncTestFixture _fixture;

    public SyncIntegrationTests(SyncTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_Sync_Record_To_Remote()
    {
        // Arrange
        var localRepo = _fixture.GetLocalRepository();
        var remoteRepo = _fixture.GetRemoteRepository();
        var syncService = _fixture.GetSyncService();

        var record = CreateTestRecord();
        await localRepo.CreateAsync(record, CancellationToken.None);

        // Act
        await syncService.SyncPendingAsync(CancellationToken.None);

        // Assert
        var remoteRecord = await remoteRepo.GetByIdAsync(record.Id, CancellationToken.None);
        Assert.NotNull(remoteRecord);
        Assert.Equal(record.SessionId, remoteRecord.SessionId);
    }
}
```

### E2E Tests

```csharp
namespace AgenticCoder.Application.Tests.E2E.Approvals.Persistence;

public class ApprovalPersistenceE2ETests : IClassFixture<E2ETestFixture>
{
    private readonly E2ETestFixture _fixture;

    public ApprovalPersistenceE2ETests(E2ETestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_Persist_During_Session_And_Query_After()
    {
        // Arrange
        var cli = _fixture.CreateCLI();

        // Act - Run task that creates approval record
        var exitCode = await cli.RunAsync(new[] { "run", "write test.txt", "--yes" });

        // Assert - Query records
        var output = await cli.RunAndCaptureAsync(new[] { "approvals", "list" });
        Assert.Contains("APPROVED", output);
        Assert.Contains("test.txt", output);
    }

    [Fact]
    public async Task Should_Export_To_Json()
    {
        // Arrange
        var cli = _fixture.CreateCLI();
        await cli.RunAsync(new[] { "run", "write test.txt", "--yes" });

        // Act
        var output = await cli.RunAndCaptureAsync(new[] { "approvals", "export", "--format", "json" });

        // Assert - Valid JSON
        var records = JsonSerializer.Deserialize<ApprovalRecord[]>(output);
        Assert.NotEmpty(records!);
    }
}
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Record creation | 10ms | 25ms |
| Signature computation | 1ms | 5ms |
| Query with 1 filter (10K records) | 50ms | 100ms |
| Query with pagination | 50ms | 100ms |
| Aggregation | 100ms | 500ms |
| Storage guard check | 0.1ms | 1ms |

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