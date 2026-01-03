# Task 049.e: Retention, Export, Privacy + Redaction Controls

**Priority:** P1 – High Priority  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – CLI + Orchestration Core  
**Dependencies:** Task 049.a (Data Model), Task 049.f (Sync), Task 039 (Security)  

---

## Description

Task 049.e defines retention policies, export formats, privacy models, and redaction rules for conversation data. These controls ensure data lifecycle management, compliance readiness, and user privacy across local and remote storage.

Retention policies govern how long data is kept. By default, conversations are retained for 365 days. Active chats are never auto-deleted. Archived chats follow retention rules. Configurable policies support different compliance requirements.

Export enables data portability. Users can export conversations to JSON, Markdown, or plain text. Export includes messages, tool calls, and metadata. Export supports filtering by chat, date, or tag. Exported data is self-contained and importable.

Privacy controls determine what data leaves the local machine. By default, all conversation data is local-only. Remote sync can be enabled. Privacy settings control what syncs: full content, redacted content, or metadata only.

Redaction removes sensitive content before sync or export. Pattern-based redaction catches secrets (API keys, passwords). Custom patterns support project-specific sensitivity. Redacted content is replaced with placeholders.

The privacy model uses a layered approach. Local storage contains full data. Sync layer applies redaction. Remote storage contains safe data. This separation protects sensitive information.

Retention enforcement runs as a background process. Expired chats are identified. Soft-deleted chats past retention are purged. Indexes are updated. Storage is reclaimed.

Export formats support different use cases. JSON for machine processing. Markdown for human reading. Plain text for simple archives. Each format includes configurable verbosity.

Compliance features support enterprise requirements. Audit logs track deletions. Export provides "right to delete" evidence. Retention reports show policy compliance.

Redaction is reversible locally but permanent remotely. Local redaction masks display. Remote redaction removes data. Users can see their full local history.

The system provides clear feedback. Retention warnings before deletion. Redaction previews before sync. Export confirmation with size estimates.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Retention | How long data is kept |
| Expiry | When data can be deleted |
| Export | Create portable data copy |
| Redaction | Remove sensitive content |
| Privacy | Data visibility controls |
| Compliance | Meeting requirements |
| Purge | Permanent deletion |
| Policy | Retention/privacy rules |
| Pattern | Regex for matching |
| Placeholder | Replacement for redacted |
| Metadata | Data about data |
| Audit | Record of actions |
| Layered | Different levels |
| Portable | Works standalone |
| Enforcement | Applying policies |

---

## Out of Scope

The following items are explicitly excluded from Task 049.e:

- **Data model** - Task 049.a
- **CLI commands** - Task 049.b
- **Concurrency** - Task 049.c
- **Search** - Task 049.d
- **Sync engine** - Task 049.f
- **Encryption** - Task 039
- **Legal hold** - Not supported
- **Cross-tenant** - Single user
- **Audit archival** - Basic only
- **GDPR automation** - Manual process

---

## Functional Requirements

### Retention Policies

- FR-001: Default retention: 365 days
- FR-002: Retention MUST be configurable
- FR-003: Minimum retention: 7 days
- FR-004: Maximum retention: unlimited
- FR-005: Active chats MUST NOT expire
- FR-006: Archived chats MUST follow policy

### Retention Enforcement

- FR-007: Background job MUST run
- FR-008: Expired chats MUST be identified
- FR-009: Expired chats MUST be purged
- FR-010: Purge MUST cascade to runs/messages
- FR-011: Index MUST be updated
- FR-012: Enforcement MUST log actions

### Retention Warnings

- FR-013: Chats near expiry MUST warn
- FR-014: Warning threshold: 7 days
- FR-015: Warnings MUST be visible in list
- FR-016: Warning suppression MUST work

### Export Formats

- FR-017: JSON export MUST work
- FR-018: Markdown export MUST work
- FR-019: Plain text export MUST work
- FR-020: Format MUST be selectable

### Export Content

- FR-021: Messages MUST be included
- FR-022: Tool calls MUST be included
- FR-023: Metadata MUST be included
- FR-024: Timestamps MUST be included
- FR-025: Content MUST be redacted if configured

### Export Filtering

- FR-026: Filter by chat MUST work
- FR-027: Filter by date MUST work
- FR-028: Filter by tag MUST work
- FR-029: All chats export MUST work

### Export CLI

- FR-030: `acode export` MUST work
- FR-031: Output file MUST be configurable
- FR-032: Stdout MUST be supported
- FR-033: Progress MUST be shown

### Privacy Levels

- FR-034: LOCAL_ONLY MUST prevent sync
- FR-035: REDACTED MUST sync cleaned content
- FR-036: FULL MUST sync everything
- FR-037: Default MUST be LOCAL_ONLY

### Privacy Controls

- FR-038: Per-chat privacy MUST work
- FR-039: Global default MUST work
- FR-040: Privacy MUST be changeable
- FR-041: Privacy change MUST log

### Redaction Patterns

- FR-042: API key patterns MUST exist
- FR-043: Password patterns MUST exist
- FR-044: Custom patterns MUST work
- FR-045: Patterns MUST be configurable

### Redaction Behavior

- FR-046: Match MUST be replaced
- FR-047: Placeholder MUST be used
- FR-048: Original MUST be preserved locally
- FR-049: Redaction MUST be logged

### Redaction Preview

- FR-050: `acode redact preview` MUST work
- FR-051: Preview MUST show matches
- FR-052: Preview MUST NOT modify

### Compliance

- FR-053: Deletion audit MUST exist
- FR-054: Export audit MUST exist
- FR-055: Retention report MUST exist
- FR-056: Compliance status MUST be queryable

---

## Non-Functional Requirements

### Performance

- NFR-001: Export < 10s for 1000 messages
- NFR-002: Redaction < 1ms per message
- NFR-003: Retention check < 1s

### Reliability

- NFR-004: No data loss from retention
- NFR-005: Export MUST be complete
- NFR-006: Redaction MUST be consistent

### Security

- NFR-007: Redaction MUST catch secrets
- NFR-008: Export MUST respect privacy
- NFR-009: Audit MUST be tamper-evident

### Compliance

- NFR-010: Retention MUST be enforced
- NFR-011: Deletion MUST be provable
- NFR-012: Export MUST be portable

---

## User Manual Documentation

### Overview

Data lifecycle controls manage retention, export, and privacy. Keep data as long as needed, export when required, and protect sensitive content.

### Retention Configuration

```yaml
# .agent/config.yml
retention:
  # Default retention period
  default_days: 365
  
  # Per-status overrides
  overrides:
    archived: 90   # Archived chats: 90 days
    active: -1     # Active chats: never expire
    
  # Warning before expiry
  warn_days_before: 7
  
  # Enforcement schedule
  enforce:
    enabled: true
    schedule: "0 2 * * *"  # Daily at 2 AM
```

### Viewing Retention Status

```bash
$ acode retention status

Retention Status
────────────────────────────────────
Active Policy: 365 days (archived: 90 days)
Last Enforcement: 2024-01-15 02:00 UTC

Statistics:
  Total Chats: 156
  Active: 12 (never expire)
  Archived: 144
  Expiring Soon (7d): 3
  
Expiring Soon:
  chat_old001  "Old Feature"      Expires: 2024-01-20
  chat_old002  "Archived Bug"     Expires: 2024-01-21
  chat_old003  "Legacy Work"      Expires: 2024-01-22
```

### Export Commands

```bash
# Export single chat to JSON
$ acode export chat_abc123 --format json > chat.json

# Export to Markdown
$ acode export chat_abc123 --format markdown > chat.md

# Export all chats
$ acode export --all --format json > all_chats.json

# Export with date filter
$ acode export --since 2024-01-01 --format json > recent.json

# Export with redaction
$ acode export chat_abc123 --redact --format json > redacted.json
```

### Export Formats

**JSON Format:**
```json
{
  "exported_at": "2024-01-15T10:00:00Z",
  "chats": [
    {
      "id": "chat_abc123",
      "title": "Feature: Auth",
      "runs": [
        {
          "id": "run_001",
          "messages": [
            {
              "role": "user",
              "content": "Design login flow",
              "created_at": "2024-01-15T09:00:00Z"
            }
          ]
        }
      ]
    }
  ]
}
```

**Markdown Format:**
```markdown
# Feature: Auth

## Run 1 - 2024-01-15 09:00

**User:** Design login flow

**Assistant:** I'll help design the login flow...
```

### Privacy Configuration

```yaml
# .agent/config.yml
privacy:
  # Default privacy level
  default: local_only  # local_only | redacted | full
  
  # Sync settings
  sync:
    enabled: true
    level: redacted  # What to sync
    
  # Per-chat overrides
  overrides:
    - pattern: "*secret*"
      level: local_only
```

### Privacy Levels

| Level | Local | Synced | Description |
|-------|-------|--------|-------------|
| local_only | Full | None | Never sync |
| redacted | Full | Cleaned | Sync with redaction |
| full | Full | Full | Sync everything |

### Setting Chat Privacy

```bash
# Set chat to local-only
$ acode chat privacy chat_abc123 local_only
Privacy set to: local_only (never syncs)

# Set chat to sync redacted
$ acode chat privacy chat_abc123 redacted
Privacy set to: redacted (syncs cleaned content)

# View privacy
$ acode chat show chat_abc123
...
Privacy: local_only
Sync Status: Not synced
```

### Redaction Patterns

```yaml
# .agent/config.yml
redaction:
  # Built-in patterns (enabled by default)
  builtin:
    api_keys: true
    passwords: true
    tokens: true
    
  # Custom patterns
  custom:
    - name: internal_urls
      pattern: "https?://internal\\..+"
      replacement: "[INTERNAL_URL]"
      
    - name: employee_ids
      pattern: "EMP-\\d{6}"
      replacement: "[EMPLOYEE_ID]"
```

### Previewing Redaction

```bash
$ acode redact preview chat_abc123

Redaction Preview
────────────────────────────────────
Chat: Feature: Auth (chat_abc123)
Messages to scan: 47

Matches Found: 3
  Line 12: "API key: sk-abc...xyz" → "[API_KEY]"
  Line 34: "password = 'secret'" → "password = '[PASSWORD]'"
  Line 45: "https://internal.company.com" → "[INTERNAL_URL]"

No changes made. Use 'acode export --redact' to apply.
```

### Compliance Reports

```bash
$ acode compliance report

Compliance Report
────────────────────────────────────
Generated: 2024-01-15 10:00 UTC

Retention Compliance:
  ✓ Policy enforced: 2024-01-15 02:00 UTC
  ✓ No overdue purges
  ✓ 3 chats expiring within warning period

Privacy Compliance:
  ✓ 144 chats: local_only
  ✓ 10 chats: redacted
  ✓ 2 chats: full

Recent Deletions (30 days):
  2024-01-10: 5 chats purged (retention)
  2024-01-05: 2 chats purged (user request)

Export Log:
  2024-01-12: 3 exports (JSON)
```

---

## Acceptance Criteria

### Retention

- [ ] AC-001: Default 365 days
- [ ] AC-002: Configurable
- [ ] AC-003: Enforcement runs
- [ ] AC-004: Cascade purge
- [ ] AC-005: Warnings shown

### Export

- [ ] AC-006: JSON format
- [ ] AC-007: Markdown format
- [ ] AC-008: Plain text format
- [ ] AC-009: Filtering works
- [ ] AC-010: Redaction applies

### Privacy

- [ ] AC-011: LOCAL_ONLY works
- [ ] AC-012: REDACTED works
- [ ] AC-013: FULL works
- [ ] AC-014: Per-chat works

### Redaction

- [ ] AC-015: Patterns match
- [ ] AC-016: Custom patterns
- [ ] AC-017: Preview works
- [ ] AC-018: Replacement correct

### Compliance

- [ ] AC-019: Audit logs
- [ ] AC-020: Reports work

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Privacy/
├── RetentionTests.cs
│   ├── Should_Identify_Expired()
│   ├── Should_Respect_Active()
│   └── Should_Cascade_Purge()
│
├── ExportTests.cs
│   ├── Should_Export_Json()
│   ├── Should_Export_Markdown()
│   └── Should_Apply_Filters()
│
├── RedactionTests.cs
│   ├── Should_Match_ApiKey()
│   ├── Should_Match_Password()
│   └── Should_Use_Custom_Pattern()
│
└── PrivacyTests.cs
    ├── Should_Block_LocalOnly()
    └── Should_Redact_For_Sync()
```

### Integration Tests

```
Tests/Integration/Privacy/
├── RetentionEnforcementTests.cs
│   ├── Should_Run_Background()
│   └── Should_Log_Deletions()
│
└── ExportIntegrationTests.cs
    └── Should_Export_Large_Chat()
```

### E2E Tests

```
Tests/E2E/Privacy/
├── PrivacyE2ETests.cs
│   ├── Should_Enforce_Retention()
│   ├── Should_Export_With_Redaction()
│   └── Should_Generate_Report()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Export 1000 msgs | 5s | 10s |
| Redact message | 0.5ms | 1ms |
| Retention check | 500ms | 1s |

---

## User Verification Steps

### Scenario 1: Retention Enforcement

1. Create chat with old dates
2. Run retention enforcement
3. Verify: Old chat purged

### Scenario 2: Export JSON

1. Create chat with messages
2. Export to JSON
3. Verify: Valid JSON, all messages

### Scenario 3: Export Markdown

1. Create chat with messages
2. Export to Markdown
3. Verify: Readable Markdown

### Scenario 4: Privacy Local Only

1. Set chat privacy to local_only
2. Enable sync
3. Verify: Chat not synced

### Scenario 5: Redaction

1. Add message with API key
2. Preview redaction
3. Verify: Key matched

### Scenario 6: Compliance Report

1. Perform various operations
2. Generate report
3. Verify: All actions logged

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Privacy/
│   ├── RetentionPolicy.cs
│   ├── PrivacyLevel.cs
│   └── RedactionPattern.cs
│
src/AgenticCoder.Application/
├── Privacy/
│   ├── IRetentionService.cs
│   ├── IExportService.cs
│   ├── IRedactionService.cs
│   └── IComplianceService.cs
│
src/AgenticCoder.Infrastructure/
├── Privacy/
│   ├── RetentionEnforcer.cs
│   ├── JsonExporter.cs
│   ├── MarkdownExporter.cs
│   ├── PatternRedactor.cs
│   └── ComplianceReporter.cs
```

### RetentionPolicy Value Object

```csharp
namespace AgenticCoder.Domain.Privacy;

public sealed record RetentionPolicy
{
    public int DefaultDays { get; init; } = 365;
    public int ArchivedDays { get; init; } = 90;
    public int ActiveDays { get; init; } = -1; // Never
    public int WarnDaysBefore { get; init; } = 7;
}
```

### IExportService Interface

```csharp
namespace AgenticCoder.Application.Privacy;

public interface IExportService
{
    Task<Stream> ExportAsync(
        ExportRequest request,
        CancellationToken ct);
        
    Task<ExportPreview> PreviewAsync(
        ExportRequest request,
        CancellationToken ct);
}

public sealed record ExportRequest(
    ExportFormat Format,
    ChatId? ChatFilter,
    DateTimeOffset? Since,
    DateTimeOffset? Until,
    bool ApplyRedaction);
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-PRIV-001 | Retention error |
| ACODE-PRIV-002 | Export failed |
| ACODE-PRIV-003 | Redaction error |
| ACODE-PRIV-004 | Invalid pattern |
| ACODE-PRIV-005 | Compliance error |

### Implementation Checklist

1. [ ] Create domain types
2. [ ] Implement retention service
3. [ ] Implement exporters
4. [ ] Implement redaction
5. [ ] Implement compliance
6. [ ] Add CLI commands
7. [ ] Add background job
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Domain types
2. **Phase 2:** Retention
3. **Phase 3:** Export
4. **Phase 4:** Redaction
5. **Phase 5:** Privacy
6. **Phase 6:** Compliance
7. **Phase 7:** CLI

---

**End of Task 049.e Specification**