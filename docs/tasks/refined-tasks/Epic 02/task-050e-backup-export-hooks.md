# Task 050.e: Backup/Export Hooks for Workspace DB (Safe, Redacted)

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – Persistence + Reliability Core  
**Dependencies:** Task 050 (Database Foundation), Task 049.e (Privacy/Redaction)  

---

## Description

### Overview

Task 050.e implements a comprehensive backup, restore, and export system for the workspace database with integrated redaction capabilities for secure data sharing. This system provides disaster recovery, data portability, and privacy-preserving export functionality essential for production-grade database management. Without proper backup and export infrastructure, teams face catastrophic data loss risks, compliance violations, and inability to safely share or debug data.

### Business Value and ROI

**Annual savings for a 10-developer team: $192,000/year**

| Category | Current Cost | With Backup/Export System | Annual Savings |
|----------|--------------|---------------------------|----------------|
| **Data Loss Prevention** | 1 incident/year @ $120k | 0 incidents with backups | $120,000/year |
| **Recovery Time** | 8 hours per incident | 30 minutes with restore | $36,000/year |
| **Debug Data Sharing** | 4 hours redaction/share | 5 minutes with auto-redact | $24,000/year |
| **Compliance Prep** | 16 hours/audit manual | 2 hours automated export | $12,000/year |

**Quantified Improvements:**
- **Mean Time to Recovery (MTTR):** 8 hours manual → 30 minutes automated (94% reduction)
- **Data Loss Risk:** Unprotected → 99.9% recovery rate with verified backups
- **Redaction Accuracy:** 70% manual → 99.9% automated pattern-based (99% reduction in exposure risk)
- **Backup Verification:** None → 100% verified with integrity checks

### Technical Architecture

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                          BACKUP & EXPORT SYSTEM                               │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                         BACKUP SUBSYSTEM                             │    │
│  │                                                                      │    │
│  │  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐          │    │
│  │  │   Backup     │    │   Backup     │    │   Backup     │          │    │
│  │  │   Service    │───▶│   Provider   │───▶│   Storage    │          │    │
│  │  │              │    │  (SQLite/PG) │    │              │          │    │
│  │  └──────────────┘    └──────────────┘    └──────────────┘          │    │
│  │         │                   │                   │                   │    │
│  │         ▼                   ▼                   ▼                   │    │
│  │  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐          │    │
│  │  │   Manifest   │    │   Checksum   │    │   Rotation   │          │    │
│  │  │   Builder    │    │   Validator  │    │   Policy     │          │    │
│  │  └──────────────┘    └──────────────┘    └──────────────┘          │    │
│  │                                                                      │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                         EXPORT SUBSYSTEM                             │    │
│  │                                                                      │    │
│  │  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐          │    │
│  │  │   Export     │    │   Format     │    │   Redaction  │          │    │
│  │  │   Service    │───▶│   Writers    │───▶│   Pipeline   │          │    │
│  │  │              │    │ JSON/CSV/DB  │    │              │          │    │
│  │  └──────────────┘    └──────────────┘    └──────────────┘          │    │
│  │         │                   │                   │                   │    │
│  │         ▼                   ▼                   ▼                   │    │
│  │  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐          │    │
│  │  │   Table      │    │   Progress   │    │   Redaction  │          │    │
│  │  │   Selector   │    │   Reporter   │    │   Logger     │          │    │
│  │  └──────────────┘    └──────────────┘    └──────────────┘          │    │
│  │                                                                      │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                         HOOK INTEGRATION                             │    │
│  │                                                                      │    │
│  │  ┌────────────────┐        ┌────────────────┐                       │    │
│  │  │ Pre-Migration  │        │ Pre-Purge      │                       │    │
│  │  │ Backup Hook    │        │ Export Hook    │                       │    │
│  │  └───────┬────────┘        └───────┬────────┘                       │    │
│  │          │                         │                                 │    │
│  │          ▼                         ▼                                 │    │
│  │  ┌────────────────────────────────────────────────┐                 │    │
│  │  │              Hook Registry                      │                 │    │
│  │  │    Configurable • Ordered • Cancellable         │                 │    │
│  │  └────────────────────────────────────────────────┘                 │    │
│  │                                                                      │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘

                         BACKUP FILE STRUCTURE
┌──────────────────────────────────────────────────────────────────────────────┐
│ .agent/backups/                                                              │
│   ├── acode_backup_20240120_143045.db       # SQLite backup file            │
│   ├── acode_backup_20240120_143045.json     # Manifest file                 │
│   ├── acode_backup_20240119_100000.db                                       │
│   ├── acode_backup_20240119_100000.json                                     │
│   └── ...                                                                    │
│                                                                              │
│ Manifest Structure:                                                          │
│ {                                                                            │
│   "version": "1.0",                                                          │
│   "created_at": "2024-01-20T14:30:45Z",                                     │
│   "database_type": "sqlite",                                                 │
│   "schema_version": "1.0.5",                                                │
│   "file_size": 47394816,                                                    │
│   "checksum": "sha256:a1b2c3d4...",                                         │
│   "tables": ["conv_chats", "conv_runs", ...],                               │
│   "record_counts": { "conv_chats": 25, ... }                                │
│ }                                                                            │
└──────────────────────────────────────────────────────────────────────────────┘

                         BACKUP LIFECYCLE
┌───────────────────────────────────────────────────────────────────────────────┐
│                                                                               │
│  CREATE                                                                       │
│    │                                                                          │
│    ├───▶ Acquire lock on source database                                     │
│    │                                                                          │
│    ├───▶ For SQLite: Use sqlite3_backup_* API (atomic, WAL-safe)             │
│    │     For PostgreSQL: Execute pg_dump with --format=custom                │
│    │                                                                          │
│    ├───▶ Compute SHA-256 checksum of backup file                             │
│    │                                                                          │
│    ├───▶ Create manifest with metadata                                       │
│    │                                                                          │
│    ├───▶ Apply rotation policy (delete oldest if > max)                      │
│    │                                                                          │
│    └───▶ Return BackupResult with path and checksum                          │
│                                                                               │
│  RESTORE                                                                      │
│    │                                                                          │
│    ├───▶ Verify backup checksum matches manifest                             │
│    │                                                                          │
│    ├───▶ Create pre-restore backup of current database                       │
│    │                                                                          │
│    ├───▶ Stop all database operations (exclusive lock)                       │
│    │                                                                          │
│    ├───▶ For SQLite: Copy backup file to database location                   │
│    │     For PostgreSQL: Execute pg_restore with --clean                     │
│    │                                                                          │
│    ├───▶ Verify restored database integrity                                  │
│    │                                                                          │
│    └───▶ Return RestoreResult with recovery details                          │
│                                                                               │
└───────────────────────────────────────────────────────────────────────────────┘
```

### Constraints

1. **Backup Format Compatibility** - SQLite backups are raw database files; PostgreSQL uses pg_dump format; cross-database restore not supported
2. **Storage Location** - Local filesystem only for MVP; cloud storage integration out of scope
3. **Backup Size Limits** - Single backup must fit in available disk space; no incremental backups
4. **Encryption Scope** - No backup encryption; relies on filesystem-level security
5. **Concurrent Access** - Backup may briefly impact read performance; WAL mode mitigates for SQLite
6. **pg_dump Dependency** - PostgreSQL backup requires pg_dump tool installed and accessible
7. **Redaction Irreversibility** - Redacted exports cannot be restored to original; one-way transformation

### Trade-offs and Decisions

| Decision | Options Considered | Choice | Rationale |
|----------|-------------------|--------|-----------|
| **Backup Method** | File copy vs SQLite Backup API | SQLite Backup API | Atomic, WAL-safe, consistent even during writes |
| **Checksum Algorithm** | MD5 vs SHA-256 | SHA-256 | Cryptographically secure, no collision risk |
| **Rotation Strategy** | Time-based vs count-based | Count-based (7 backups) | Simpler, predictable storage, easy to understand |
| **Export Format Default** | JSON vs CSV vs SQLite | JSON | Human-readable, preserves types, easy debugging |
| **Redaction Approach** | Static patterns vs ML-based | Static patterns | Deterministic, auditable, no false negatives |

### Use Cases

---

#### Use Case 1: DevBot - Pre-Migration Safety Net

**Persona:** DevBot, an AI coding assistant running automated deployments

**Scenario:** DevBot is deploying a new version that includes database schema migrations. Before applying migrations, it needs to ensure rollback capability.

**Before (Without Pre-Migration Backup):**
- Migration fails partway through
- Database in inconsistent state
- Manual intervention required: 4 hours
- Data loss risk: 15% of failed migrations

**After (With Pre-Migration Backup Hook):**
```
┌────────────────────────────────────────────────────────────────────────────┐
│ DevBot: Starting deployment v2.3.0 with 3 migrations                       │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│ Step 1: Pre-migration backup                                               │
│   ✓ Creating backup: acode_backup_20240120_pre_migrate_v230.db             │
│   ✓ Checksum: sha256:a1b2c3d4e5f6...                                       │
│   ✓ Manifest created                                                       │
│                                                                            │
│ Step 2: Applying migrations                                                │
│   ✓ 001_add_user_preferences.sql                                           │
│   ✓ 002_create_audit_log.sql                                               │
│   ✗ 003_migrate_legacy_data.sql (FAILED: constraint violation)             │
│                                                                            │
│ Step 3: Auto-recovery                                                      │
│   ⚠ Migration failed - initiating rollback                                 │
│   ✓ Restoring from: acode_backup_20240120_pre_migrate_v230.db              │
│   ✓ Database restored to pre-migration state                               │
│   ✓ No data loss                                                           │
│                                                                            │
│ Result: Deployment failed safely. Database intact.                         │
│         Fix migration script and retry.                                    │
└────────────────────────────────────────────────────────────────────────────┘
```

**Quantified Impact:**
- Recovery time: 4 hours → 30 seconds (99.8% reduction)
- Data loss risk: 15% → 0%
- Manual intervention: Always → Never (for recoverable failures)
- Annual savings: $120,000/year in prevented data loss + recovery time

---

#### Use Case 2: Jordan - Secure Debug Data Sharing

**Persona:** Jordan, a senior developer troubleshooting a production issue

**Scenario:** Jordan needs to share database state with an external consultant for debugging, but the database contains API keys, tokens, and customer file paths that must not be exposed.

**Before (Without Automated Redaction):**
- Manual review of 50,000 records: 4 hours
- Miss 3% of sensitive data on average
- Compliance risk if data shared improperly
- External consultant waits 2 days for data

**After (With Redacted Export):**
```
┌────────────────────────────────────────────────────────────────────────────┐
│ Jordan: Creating redacted export for consultant                            │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│ $ acode db export --format json --redact --output debug_export.json        │
│                                                                            │
│ Exporting database with redaction...                                       │
│   Format: JSON (human-readable)                                            │
│   Redaction: ENABLED                                                       │
│                                                                            │
│   Processing tables...                                                     │
│     conv_chats: 25 records                                                 │
│     conv_runs: 150 records                                                 │
│     conv_messages: 1,234 records (contains API keys)                       │
│     sess_sessions: 50 records (contains tokens)                            │
│     appr_approvals: 200 records                                            │
│                                                                            │
│   Applying redaction patterns...                                           │
│     ✓ Column patterns: *_key, *_secret, *_token                            │
│     ✓ Content patterns: sk-*, ghp_*, xoxb-*                                │
│     ✓ Path patterns: C:\Users\*, /home/*                                   │
│                                                                            │
│   Redaction summary:                                                       │
│     • 15 API keys → [REDACTED-API-KEY]                                     │
│     • 3 access tokens → [REDACTED-TOKEN]                                   │
│     • 45 file paths → /[REDACTED-PATH]/...                                 │
│     • 2 email addresses → user@[REDACTED]                                  │
│                                                                            │
│   Output: debug_export.json (2.3 MB)                                       │
│   Redaction log: debug_export.redaction.log                                │
│                                                                            │
│ ✓ Export complete. Safe to share externally.                               │
└────────────────────────────────────────────────────────────────────────────┘
```

**Quantified Impact:**
- Preparation time: 4 hours → 5 minutes (98% reduction)
- Missed sensitive data: 3% → 0.1% (97% improvement)
- Sharing delay: 2 days → 10 minutes
- Compliance risk: High → Minimal with audit log
- Annual savings: $24,000/year in redaction effort

---

#### Use Case 3: Alex - Compliance Audit Preparation

**Persona:** Alex, a DevOps engineer preparing for SOC2 audit

**Scenario:** Alex needs to demonstrate backup and recovery capabilities for compliance audit, including proof of tested restore procedures.

**Before (Without Documented Backup System):**
- Manual backup documentation: 16 hours
- Untested restore procedures
- Auditor requests: "Show me a tested restore"
- Scramble to prove backup integrity

**After (With Verified Backup System):**
```
┌────────────────────────────────────────────────────────────────────────────┐
│ Alex: Preparing backup compliance evidence for auditor                     │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│ $ acode db backup list --verbose --format json > backup_inventory.json     │
│                                                                            │
│ Backup Inventory (last 7 days):                                            │
│   Total backups: 7                                                         │
│   Total size: 315 MB                                                       │
│   Oldest: 2024-01-13T10:00:00Z                                             │
│   Newest: 2024-01-20T10:00:00Z                                             │
│   All checksums: VALID                                                     │
│                                                                            │
│ $ acode db backup verify --all                                             │
│                                                                            │
│ Verifying all backups...                                                   │
│   ✓ acode_backup_20240120_100000.db - checksum valid, structure valid      │
│   ✓ acode_backup_20240119_100000.db - checksum valid, structure valid      │
│   ✓ acode_backup_20240118_100000.db - checksum valid, structure valid      │
│   ... (7/7 verified)                                                       │
│                                                                            │
│ $ acode db restore --test acode_backup_20240118_100000.db                  │
│                                                                            │
│ Test Restore (isolated environment):                                       │
│   ✓ Restored to temp location                                              │
│   ✓ Integrity verified                                                     │
│   ✓ All tables present                                                     │
│   ✓ Record counts match manifest                                           │
│   ✓ Test restore successful                                                │
│   Cleaned up temp files                                                    │
│                                                                            │
│ Audit evidence generated:                                                  │
│   • backup_inventory.json                                                  │
│   • backup_verification_report.json                                        │
│   • restore_test_log.json                                                  │
│                                                                            │
│ ✓ Ready for SOC2 auditor review                                            │
└────────────────────────────────────────────────────────────────────────────┘
```

**Quantified Impact:**
- Audit prep time: 16 hours → 2 hours (88% reduction)
- Restore testing: Untested → Verified with evidence
- Auditor confidence: Low → High with automated proof
- Compliance risk: Significant → Minimal
- Annual savings: $12,000/year in audit preparation

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Backup | Full database copy |
| Export | Data extraction |
| Redaction | Sensitive data removal |
| Hook | Integration point |
| Rotation | Delete old backups |
| Retention | How long to keep |
| Verification | Integrity check |
| Restore | Recover from backup |
| WAL | Write-Ahead Log |
| pg_dump | PostgreSQL backup tool |
| Checksum | Integrity hash |
| Atomic | All or nothing |
| Safe | Redacted for sharing |
| Archive | Compressed backup |
| Manifest | Backup metadata |

---

## Out of Scope

The following items are explicitly excluded from Task 050.e:

- **Cloud storage** - Local only for MVP
- **Encrypted backups** - No encryption layer
- **Incremental backups** - Full only
- **Point-in-time recovery** - Snapshot only
- **Cross-database restore** - Same type only
- **Remote backup servers** - Local only
- **Backup scheduling service** - Manual or cron

---

## Assumptions

### Technical Assumptions

1. **SQLite Backup API** - Uses sqlite3_backup_* API for consistent online backups
2. **PostgreSQL pg_dump** - External pg_dump tool available for PostgreSQL backups
3. **Atomic Backups** - Backup process produces consistent snapshot; no partial states
4. **Local Storage** - Backup files written to local filesystem (.agent/backups/)
5. **Timestamped Files** - Backup filenames include ISO8601 timestamp for ordering
6. **Compression Optional** - gzip compression available but not required
7. **No Encryption** - Backup files not encrypted; filesystem security assumed

### Operational Assumptions

8. **Manual Trigger** - Primary backup method is `agent db backup` CLI command
9. **Pre-Migration Hook** - Migrations optionally trigger backup before schema changes
10. **Retention Policy** - Configurable retention: keep N backups or backups from last N days
11. **Pruning Command** - `agent db backup prune` removes old backups per retention policy
12. **Verification** - `agent db backup verify <file>` checks backup integrity
13. **Restore Command** - `agent db restore <file>` restores from backup with confirmation

### Export Assumptions

14. **JSON Export** - `agent db export --format json` exports data as JSON files
15. **CSV Export** - `agent db export --format csv` exports tables as CSV files
16. **Selective Export** - --tables flag limits export to specific tables
17. **Schema Export** - --schema-only exports DDL without data
18. **Import Support** - `agent db import` restores from JSON/CSV export format

---

## Functional Requirements

### Backup Creation

- FR-001: SQLite backup via API MUST work
- FR-002: PostgreSQL backup via pg_dump MUST work
- FR-003: Backup MUST be atomic
- FR-004: Backup MUST handle WAL
- FR-005: Backup MUST include checksum

### Backup Metadata

- FR-006: Manifest file MUST exist
- FR-007: Manifest includes timestamp
- FR-008: Manifest includes version
- FR-009: Manifest includes checksum
- FR-010: Manifest includes size

### Backup Location

- FR-011: Default: .agent/backups/
- FR-012: Configurable location
- FR-013: Auto-create directory
- FR-014: Backup naming: acode_backup_YYYYMMDD_HHMMSS

### Backup Rotation

- FR-015: Max backups configurable
- FR-016: Default: 7 backups
- FR-017: Oldest deleted first
- FR-018: Rotation after successful backup

### Migration Hooks

- FR-019: Pre-migration backup optional
- FR-020: Default: enabled
- FR-021: Backup before each migration
- FR-022: Failed backup aborts migration

### Restore

- FR-023: SQLite restore via copy MUST work
- FR-024: PostgreSQL restore via pg_restore MUST work
- FR-025: Restore MUST verify checksum
- FR-026: Restore MUST backup current first

### Backup Verification

- FR-027: Checksum validation MUST work
- FR-028: Structure validation MUST work
- FR-029: Optional test restore
- FR-030: Report: valid/invalid

### Export Framework

- FR-031: JSON export MUST work
- FR-032: CSV export MUST work
- FR-033: SQLite export MUST work
- FR-034: Table selection MUST work
- FR-035: Date range filter MUST work

### Redaction

- FR-036: Pattern-based redaction MUST work
- FR-037: Column-based redaction MUST work
- FR-038: Content-based redaction MUST work
- FR-039: Redaction is irreversible
- FR-040: Redaction logged

### Default Redaction Patterns

- FR-041: Redact api_key columns
- FR-042: Redact password columns
- FR-043: Redact token columns
- FR-044: Redact secret columns
- FR-045: Redact content matching API key patterns

### Export Metadata

- FR-046: Export includes timestamp
- FR-047: Export includes version
- FR-048: Export includes redaction applied
- FR-049: Export includes source info

### CLI Backup Commands

- FR-050: `acode db backup` MUST work
- FR-051: `acode db backup list` MUST work
- FR-052: `acode db restore` MUST work
- FR-053: `acode db backup verify` MUST work
- FR-054: `acode db backup delete` MUST work

### CLI Export Commands

- FR-055: `acode db export` MUST work
- FR-056: `--format json|csv|sqlite`
- FR-057: `--tables` table selection
- FR-058: `--redact` enable redaction
- FR-059: `--output` file path

---

## Non-Functional Requirements

### Performance

- NFR-001: Backup < 10s for 100MB database
- NFR-002: Export < 30s for 100MB database
- NFR-003: Verification < 5s for any backup
- NFR-004: Redaction adds < 20% overhead to export
- NFR-005: SHA-256 checksum computed in streaming mode (constant memory)
- NFR-006: Rotation cleanup < 1s even with 100 backups
- NFR-007: Progress updates at least every 2 seconds during backup/export

### Reliability

- NFR-008: Atomic backup - success or no change
- NFR-009: No partial backups left on failure
- NFR-010: Checksum verification on every restore
- NFR-011: Pre-restore backup ensures recovery from failed restore
- NFR-012: Backup file locked during write to prevent corruption
- NFR-013: Manifest written atomically after backup completes
- NFR-014: Database connection maintained during backup for SQLite

### Safety

- NFR-015: Redaction is complete - no partial sensitive data
- NFR-016: Redaction patterns cannot be disabled silently
- NFR-017: Verification required before external sharing
- NFR-018: Restore requires explicit confirmation unless --force
- NFR-019: Delete requires backup name, no wildcards
- NFR-020: Test restore available in isolated mode
- NFR-021: Backup files have restrictive permissions (0600)

### Usability

- NFR-022: Clear progress output with percentage
- NFR-023: Helpful error messages with recovery suggestions
- NFR-024: Dry-run for exports shows what would be redacted
- NFR-025: List command shows backup metadata summary
- NFR-026: JSON output option for all commands (--output json)
- NFR-027: Colored output for success/warning/error states

### Scalability

- NFR-028: Handles databases up to 10GB for SQLite
- NFR-029: No memory constraints - streaming backup
- NFR-030: Handles tables with 10M+ rows for export

### Maintainability

- NFR-031: Backup format versioned in manifest
- NFR-032: Redaction patterns extensible via configuration
- NFR-033: Export formats use strategy pattern
- NFR-034: All operations logged for debugging
- NFR-035: Clear separation between backup/restore/export/redaction

---

## Security Considerations

### Threat Model

| ID | Threat | Impact | Likelihood | Mitigation |
|----|--------|--------|------------|------------|
| SEC-001 | Incomplete redaction exposes secrets in exports | Critical | Medium | Pattern validation, mandatory review |
| SEC-002 | Backup files readable by unauthorized users | High | Medium | Restrictive file permissions |
| SEC-003 | Restore from tampered backup corrupts data | High | Low | Mandatory checksum verification |
| SEC-004 | Sensitive data in backup filenames or paths | Medium | Low | Sanitize metadata, generic naming |
| SEC-005 | Denial of service via large backup requests | Medium | Medium | Size limits, rate limiting |

### SEC-001: Incomplete Redaction Mitigation

**Risk:** Redaction patterns may not catch all sensitive data, exposing API keys, tokens, or personal information when exports are shared externally.

**Mitigation:** Multi-layer redaction with validation, audit logging, and dry-run capability.

```csharp
// Acode.Infrastructure/Backup/Redaction/SecureRedactionPipeline.cs
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Acode.Domain.Backup;

namespace Acode.Infrastructure.Backup.Redaction;

/// <summary>
/// Multi-stage redaction pipeline with validation and audit logging.
/// Ensures complete redaction before allowing export sharing.
/// </summary>
public sealed class SecureRedactionPipeline : IRedactionPipeline
{
    private readonly IReadOnlyList<IRedactionStage> _stages;
    private readonly IRedactionAuditLogger _auditLogger;
    private readonly RedactionValidator _validator;

    public SecureRedactionPipeline(
        IEnumerable<IRedactionStage> stages,
        IRedactionAuditLogger auditLogger,
        RedactionValidator validator)
    {
        _stages = stages?.ToList() ?? throw new ArgumentNullException(nameof(stages));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public RedactionResult Redact(ExportRecord record, RedactionOptions options)
    {
        var result = new RedactionResult
        {
            OriginalRecord = record,
            RedactedRecord = record.Clone(),
            RedactedFields = new List<RedactedField>()
        };

        // Apply all redaction stages in order
        foreach (var stage in _stages)
        {
            var stageResult = stage.Apply(result.RedactedRecord, options);
            result.RedactedFields.AddRange(stageResult.RedactedFields);
        }

        // Validate redaction completeness
        var validationResult = _validator.Validate(result.RedactedRecord);
        if (!validationResult.IsValid)
        {
            // Log warning and apply additional redaction
            _auditLogger.LogValidationWarning(record.Id, validationResult.SuspiciousFields);
            
            foreach (var field in validationResult.SuspiciousFields)
            {
                result.RedactedRecord.SetField(field, "[REDACTED-VALIDATED]");
                result.RedactedFields.Add(new RedactedField
                {
                    FieldName = field,
                    RedactionType = RedactionType.ValidationCatchAll,
                    Reason = "Detected by post-redaction validation"
                });
            }
        }

        // Audit log all redactions
        _auditLogger.LogRedaction(record.Id, result.RedactedFields);

        return result;
    }

    public DryRunResult DryRun(IEnumerable<ExportRecord> records, RedactionOptions options)
    {
        var dryRunResult = new DryRunResult();

        foreach (var record in records)
        {
            var result = Redact(record, options);
            if (result.RedactedFields.Any())
            {
                dryRunResult.AffectedRecords.Add(new DryRunRecord
                {
                    RecordId = record.Id,
                    TableName = record.TableName,
                    FieldsToRedact = result.RedactedFields
                        .Select(f => f.FieldName)
                        .ToList()
                });
            }
        }

        dryRunResult.TotalRecordsScanned = records.Count();
        dryRunResult.TotalFieldsToRedact = dryRunResult.AffectedRecords
            .Sum(r => r.FieldsToRedact.Count);

        return dryRunResult;
    }
}

/// <summary>
/// Validates that redaction is complete by scanning for suspicious patterns.
/// Acts as a safety net for patterns that may have been missed.
/// </summary>
public sealed class RedactionValidator
{
    private static readonly Regex[] SuspiciousPatterns = new[]
    {
        new Regex(@"sk-[a-zA-Z0-9]{48}", RegexOptions.Compiled),     // OpenAI keys
        new Regex(@"ghp_[a-zA-Z0-9]{36}", RegexOptions.Compiled),    // GitHub PATs
        new Regex(@"gho_[a-zA-Z0-9]{36}", RegexOptions.Compiled),    // GitHub OAuth
        new Regex(@"xoxb-[a-zA-Z0-9-]+", RegexOptions.Compiled),     // Slack tokens
        new Regex(@"xoxp-[a-zA-Z0-9-]+", RegexOptions.Compiled),     // Slack user tokens
        new Regex(@"AKIA[A-Z0-9]{16}", RegexOptions.Compiled),       // AWS Access Keys
        new Regex(@"AIza[a-zA-Z0-9_-]{35}", RegexOptions.Compiled),  // Google API keys
        new Regex(@"-----BEGIN (RSA |EC |)PRIVATE KEY-----", RegexOptions.Compiled)
    };

    public ValidationResult Validate(ExportRecord record)
    {
        var suspiciousFields = new List<string>();

        foreach (var field in record.Fields)
        {
            var value = field.Value?.ToString() ?? string.Empty;
            foreach (var pattern in SuspiciousPatterns)
            {
                if (pattern.IsMatch(value))
                {
                    suspiciousFields.Add(field.Key);
                    break;
                }
            }
        }

        return new ValidationResult
        {
            IsValid = suspiciousFields.Count == 0,
            SuspiciousFields = suspiciousFields
        };
    }
}
```

### SEC-002: Backup File Permissions Mitigation

**Risk:** Backup files may be readable by other users on shared systems, exposing sensitive database contents.

**Mitigation:** Restrictive file permissions and secure directory creation.

```csharp
// Acode.Infrastructure/Backup/Storage/SecureBackupStorage.cs
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Acode.Domain.Backup;

namespace Acode.Infrastructure.Backup.Storage;

/// <summary>
/// Secure backup storage with restrictive file permissions.
/// Ensures backup files are only readable by the creating user.
/// </summary>
public sealed class SecureBackupStorage : IBackupStorage
{
    private readonly string _backupDirectory;
    private readonly ILogger<SecureBackupStorage> _logger;

    public SecureBackupStorage(BackupOptions options, ILogger<SecureBackupStorage> logger)
    {
        _backupDirectory = options.BackupDirectory 
            ?? Path.Combine(Environment.CurrentDirectory, ".agent", "backups");
        _logger = logger;
        
        EnsureSecureDirectory();
    }

    private void EnsureSecureDirectory()
    {
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
            SetDirectoryPermissions(_backupDirectory);
        }
    }

    public string CreateBackupPath(string backupName)
    {
        var path = Path.Combine(_backupDirectory, backupName);
        return path;
    }

    public void SecureBackupFile(string filePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            SecureFileWindows(filePath);
        }
        else
        {
            SecureFileUnix(filePath);
        }
        
        _logger.LogDebug("Secured backup file with restrictive permissions: {FilePath}", filePath);
    }

    private void SetDirectoryPermissions(string directoryPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var dirInfo = new DirectoryInfo(directoryPath);
            var security = dirInfo.GetAccessControl();
            
            // Remove inherited permissions
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            
            // Grant access only to current user
            var currentUser = WindowsIdentity.GetCurrent();
            var userRule = new FileSystemAccessRule(
                currentUser.Name,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow);
            
            security.AddAccessRule(userRule);
            dirInfo.SetAccessControl(security);
        }
        else
        {
            // Unix: chmod 700
            File.SetUnixFileMode(directoryPath, 
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private void SecureFileWindows(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var security = fileInfo.GetAccessControl();
        
        // Remove inherited permissions
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        
        // Grant access only to current user
        var currentUser = WindowsIdentity.GetCurrent();
        var userRule = new FileSystemAccessRule(
            currentUser.Name,
            FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Delete,
            AccessControlType.Allow);
        
        security.AddAccessRule(userRule);
        fileInfo.SetAccessControl(security);
    }

    private void SecureFileUnix(string filePath)
    {
        // chmod 600 - owner read/write only
        File.SetUnixFileMode(filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}
```

### SEC-003: Tampered Backup Detection

**Risk:** A modified backup file could be restored, corrupting the database with malicious or incorrect data.

**Mitigation:** Mandatory checksum verification before any restore operation.

```csharp
// Acode.Infrastructure/Backup/Verification/BackupIntegrityVerifier.cs
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using Acode.Domain.Backup;

namespace Acode.Infrastructure.Backup.Verification;

/// <summary>
/// Verifies backup integrity using SHA-256 checksums.
/// Prevents restoration of tampered or corrupted backups.
/// </summary>
public sealed class BackupIntegrityVerifier : IBackupVerifier
{
    private readonly ILogger<BackupIntegrityVerifier> _logger;

    public BackupIntegrityVerifier(ILogger<BackupIntegrityVerifier> logger)
    {
        _logger = logger;
    }

    public BackupVerificationResult Verify(string backupPath)
    {
        var manifestPath = GetManifestPath(backupPath);
        
        // Step 1: Ensure manifest exists
        if (!File.Exists(manifestPath))
        {
            return BackupVerificationResult.Failed(
                BackupVerificationError.ManifestMissing,
                $"Manifest file not found: {manifestPath}");
        }

        // Step 2: Parse manifest
        BackupManifest manifest;
        try
        {
            var manifestJson = File.ReadAllText(manifestPath);
            manifest = JsonSerializer.Deserialize<BackupManifest>(manifestJson)
                ?? throw new InvalidOperationException("Manifest deserialized to null");
        }
        catch (Exception ex)
        {
            return BackupVerificationResult.Failed(
                BackupVerificationError.ManifestCorrupted,
                $"Failed to parse manifest: {ex.Message}");
        }

        // Step 3: Verify backup file exists
        if (!File.Exists(backupPath))
        {
            return BackupVerificationResult.Failed(
                BackupVerificationError.BackupFileMissing,
                $"Backup file not found: {backupPath}");
        }

        // Step 4: Compute current checksum
        string computedChecksum;
        try
        {
            computedChecksum = ComputeChecksum(backupPath);
        }
        catch (Exception ex)
        {
            return BackupVerificationResult.Failed(
                BackupVerificationError.ChecksumComputationFailed,
                $"Failed to compute checksum: {ex.Message}");
        }

        // Step 5: Compare checksums using constant-time comparison
        var expectedChecksum = manifest.Checksum.Replace("sha256:", "");
        if (!CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(computedChecksum),
            Convert.FromHexString(expectedChecksum)))
        {
            _logger.LogWarning(
                "Checksum mismatch for backup {BackupPath}. Expected: {Expected}, Computed: {Computed}",
                backupPath, expectedChecksum, computedChecksum);
                
            return BackupVerificationResult.Failed(
                BackupVerificationError.ChecksumMismatch,
                "Backup file has been modified since creation. " +
                "This could indicate corruption or tampering. " +
                "Do NOT restore this backup.");
        }

        // Step 6: Verify file size
        var fileInfo = new FileInfo(backupPath);
        if (fileInfo.Length != manifest.FileSize)
        {
            return BackupVerificationResult.Failed(
                BackupVerificationError.SizeMismatch,
                $"File size mismatch. Expected: {manifest.FileSize}, Actual: {fileInfo.Length}");
        }

        _logger.LogInformation(
            "Backup verification passed: {BackupPath}, Checksum: {Checksum}",
            backupPath, computedChecksum);

        return BackupVerificationResult.Success(manifest);
    }

    public string ComputeChecksum(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        
        var hashBytes = sha256.ComputeHash(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public bool MustVerifyBeforeRestore => true;

    private static string GetManifestPath(string backupPath)
    {
        return Path.ChangeExtension(backupPath, ".json");
    }
}
```

### SEC-004: Metadata Sanitization

**Risk:** Sensitive information (usernames, paths, hostnames) may leak through backup filenames or manifest metadata.

**Mitigation:** Sanitize all metadata to remove identifying information.

```csharp
// Acode.Infrastructure/Backup/Metadata/MetadataSanitizer.cs
using System;
using System.IO;
using System.Text.RegularExpressions;
using Acode.Domain.Backup;

namespace Acode.Infrastructure.Backup.Metadata;

/// <summary>
/// Sanitizes backup metadata to prevent information leakage.
/// Removes usernames, machine names, and absolute paths from manifests.
/// </summary>
public sealed class MetadataSanitizer : IMetadataSanitizer
{
    private static readonly Regex WindowsPathRegex = new(
        @"[A-Za-z]:\\[^""<>|]*", 
        RegexOptions.Compiled);
    
    private static readonly Regex UnixPathRegex = new(
        @"/(?:home|Users)/[a-zA-Z0-9_-]+", 
        RegexOptions.Compiled);
    
    private static readonly Regex UsernamePatterns = new(
        @"(?:username|user|owner|created_by)["":\s]+[a-zA-Z0-9_@.-]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public BackupManifest SanitizeManifest(BackupManifest manifest)
    {
        return new BackupManifest
        {
            Version = manifest.Version,
            CreatedAt = manifest.CreatedAt,
            DatabaseType = manifest.DatabaseType,
            SchemaVersion = manifest.SchemaVersion,
            FileSize = manifest.FileSize,
            Checksum = manifest.Checksum,
            Tables = manifest.Tables,
            RecordCounts = manifest.RecordCounts,
            // Sanitized fields
            SourcePath = SanitizePath(manifest.SourcePath),
            MachineName = "[REDACTED]",
            Username = "[REDACTED]",
            WorkingDirectory = SanitizePath(manifest.WorkingDirectory)
        };
    }

    public string SanitizeBackupName(string suggestedName)
    {
        // Ensure backup name only contains safe characters
        var safeName = Regex.Replace(suggestedName, @"[^a-zA-Z0-9_.-]", "_");
        
        // Ensure no path components leak
        safeName = Path.GetFileName(safeName);
        
        return safeName;
    }

    public string SanitizePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return "[PATH]";

        // Replace Windows paths
        var sanitized = WindowsPathRegex.Replace(path, "[PATH]");
        
        // Replace Unix home directories
        sanitized = UnixPathRegex.Replace(sanitized, "/[USER]");
        
        return sanitized;
    }

    public string SanitizeErrorMessage(string errorMessage)
    {
        var sanitized = errorMessage;
        
        // Remove absolute paths
        sanitized = WindowsPathRegex.Replace(sanitized, "[PATH]");
        sanitized = UnixPathRegex.Replace(sanitized, "/[USER]");
        
        // Remove username references
        sanitized = UsernamePatterns.Replace(sanitized, "[USER-INFO]");
        
        return sanitized;
    }
}
```

### SEC-005: Resource Exhaustion Prevention

**Risk:** Malicious or accidental large backup requests could exhaust disk space or memory.

**Mitigation:** Size limits, disk space checks, and streaming operations.

```csharp
// Acode.Infrastructure/Backup/Protection/BackupResourceGuard.cs
using System;
using System.IO;
using Acode.Domain.Backup;

namespace Acode.Infrastructure.Backup.Protection;

/// <summary>
/// Prevents resource exhaustion during backup operations.
/// Implements size limits and disk space verification.
/// </summary>
public sealed class BackupResourceGuard : IBackupResourceGuard
{
    private readonly BackupOptions _options;
    private readonly ILogger<BackupResourceGuard> _logger;

    public const long DefaultMaxBackupSize = 10L * 1024 * 1024 * 1024; // 10 GB
    public const long MinimumFreeSpace = 500L * 1024 * 1024; // 500 MB minimum free

    public BackupResourceGuard(BackupOptions options, ILogger<BackupResourceGuard> logger)
    {
        _options = options;
        _logger = logger;
    }

    public ResourceCheckResult CheckBeforeBackup(string sourceDatabasePath, string backupDirectory)
    {
        // Check 1: Source database size
        var sourceInfo = new FileInfo(sourceDatabasePath);
        if (!sourceInfo.Exists)
        {
            return ResourceCheckResult.Failed("Source database does not exist");
        }

        var sourceSize = sourceInfo.Length;
        var maxSize = _options.MaxBackupSizeBytes ?? DefaultMaxBackupSize;

        if (sourceSize > maxSize)
        {
            return ResourceCheckResult.Failed(
                $"Database size ({FormatSize(sourceSize)}) exceeds maximum backup size ({FormatSize(maxSize)}). " +
                "Consider using incremental backups or increasing the limit.");
        }

        // Check 2: Available disk space (need source size + 500MB buffer)
        var requiredSpace = sourceSize + MinimumFreeSpace;
        var driveInfo = new DriveInfo(Path.GetPathRoot(backupDirectory) ?? "C:");
        
        if (driveInfo.AvailableFreeSpace < requiredSpace)
        {
            return ResourceCheckResult.Failed(
                $"Insufficient disk space. Required: {FormatSize(requiredSpace)}, " +
                $"Available: {FormatSize(driveInfo.AvailableFreeSpace)}. " +
                "Free up disk space or change backup location.");
        }

        // Check 3: Backup directory exists and is writable
        if (!Directory.Exists(backupDirectory))
        {
            try
            {
                Directory.CreateDirectory(backupDirectory);
            }
            catch (Exception ex)
            {
                return ResourceCheckResult.Failed(
                    $"Cannot create backup directory: {ex.Message}");
            }
        }

        // Check 4: Write permission test
        var testFile = Path.Combine(backupDirectory, ".write_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch (Exception ex)
        {
            return ResourceCheckResult.Failed(
                $"Backup directory is not writable: {ex.Message}");
        }

        // Check 5: Existing backup count (warn if approaching limit)
        var existingBackups = Directory.GetFiles(backupDirectory, "acode_backup_*.db").Length;
        var maxBackups = _options.MaxBackups ?? 7;
        
        if (existingBackups >= maxBackups)
        {
            _logger.LogInformation(
                "Backup count ({Count}) at maximum ({Max}). Oldest backup will be deleted.",
                existingBackups, maxBackups);
        }

        return ResourceCheckResult.Success(new ResourceInfo
        {
            SourceSize = sourceSize,
            AvailableSpace = driveInfo.AvailableFreeSpace,
            ExistingBackupCount = existingBackups,
            EstimatedBackupSize = sourceSize // Full backup, same size
        });
    }

    public void EnforceRotationPolicy(string backupDirectory)
    {
        var maxBackups = _options.MaxBackups ?? 7;
        var backupFiles = Directory.GetFiles(backupDirectory, "acode_backup_*.db")
            .Select(f => new FileInfo(f))
            .OrderBy(f => f.CreationTimeUtc)
            .ToList();

        // Delete oldest backups if over limit (after adding new one)
        while (backupFiles.Count >= maxBackups)
        {
            var oldest = backupFiles.First();
            var manifestPath = Path.ChangeExtension(oldest.FullName, ".json");
            
            try
            {
                File.Delete(oldest.FullName);
                if (File.Exists(manifestPath))
                {
                    File.Delete(manifestPath);
                }
                
                _logger.LogInformation(
                    "Deleted old backup: {BackupName} (rotation policy: max {Max})",
                    oldest.Name, maxBackups);
                    
                backupFiles.RemoveAt(0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete old backup: {BackupName}", oldest.Name);
                break;
            }
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}
```

---

## User Manual Documentation

### Overview

Backup and export protect your data. Backups enable recovery. Exports enable sharing. Redaction protects sensitive information.

### Quick Backup

```bash
$ acode db backup

Creating backup...
  Source: .agent/acode.db (45.2 MB)
  Target: .agent/backups/acode_backup_20240120_143045.db

  ✓ Database locked
  ✓ Copying data...
  ✓ Checksum computed
  ✓ Manifest created

Backup complete: acode_backup_20240120_143045.db
  Size: 45.2 MB
  Checksum: sha256:a1b2c3d4e5...
```

### List Backups

```bash
$ acode db backup list

Available Backups
═══════════════════════════════════
  1. acode_backup_20240120_143045.db
       Size: 45.2 MB
       Created: 2024-01-20 14:30:45
       Checksum: ✓ valid
       
  2. acode_backup_20240119_100000.db
       Size: 44.8 MB
       Created: 2024-01-19 10:00:00
       Checksum: ✓ valid
       
  3. acode_backup_20240118_100000.db
       Size: 44.1 MB
       Created: 2024-01-18 10:00:00
       Checksum: ✓ valid

Total: 3 backups (134.1 MB)
```

### Restore from Backup

```bash
$ acode db restore acode_backup_20240119_100000.db

Restoring from backup...
  Source: .agent/backups/acode_backup_20240119_100000.db
  Target: .agent/acode.db

  ⚠ Current database will be replaced
  
  Creating safety backup of current...
    ✓ acode_backup_20240120_143500_pre_restore.db
    
  Verifying source backup...
    ✓ Checksum valid
    ✓ Structure valid
    
  Restoring...
    ✓ Database replaced
    ✓ Integrity verified

Restore complete.
Previous database saved as: acode_backup_20240120_143500_pre_restore.db
```

### Verify Backup

```bash
$ acode db backup verify acode_backup_20240119_100000.db

Verifying backup...
  File: acode_backup_20240119_100000.db
  
  ✓ File exists
  ✓ Checksum matches manifest
  ✓ SQLite structure valid
  ✓ Tables present: 12
  ✓ Records: 15,432
  
Backup is valid.
```

### Export with Redaction

```bash
$ acode db export --format json --redact --output export.json

Exporting database...
  Format: JSON
  Redaction: enabled
  
  Processing tables...
    conv_chats: 25 records
    conv_runs: 150 records
    conv_messages: 1,234 records
    sess_sessions: 50 records
    appr_approvals: 200 records
    
  Applying redaction...
    ✓ Redacted 15 API keys
    ✓ Redacted 3 tokens
    ✓ Redacted 45 file paths
    
  Writing output...
    ✓ export.json (2.3 MB)

Export complete.
Redaction summary:
  - 15 API keys replaced with [REDACTED]
  - 3 tokens replaced with [REDACTED]
  - 45 file paths normalized
```

### Selective Export

```bash
$ acode db export --format csv --tables conv_chats,conv_runs --output chats.csv

Exporting selected tables...
  Tables: conv_chats, conv_runs
  Format: CSV
  
  Processing...
    conv_chats: 25 records → conv_chats.csv
    conv_runs: 150 records → conv_runs.csv
    
Export complete: 2 files created
```

### Configuration

```yaml
# .agent/config.yml
backup:
  # Backup location
  path: .agent/backups/
  
  # Maximum backups to keep
  max_backups: 7
  
  # Backup before migrations
  pre_migration: true
  
  # Verification after backup
  verify: true

export:
  # Default format
  default_format: json
  
  # Default redaction
  redact_by_default: true
  
  # Custom redaction patterns
  redaction_patterns:
    - column: "*_key"
    - column: "*_secret"
    - column: "*_token"
    - pattern: "sk-[a-zA-Z0-9]+"
    - pattern: "ghp_[a-zA-Z0-9]+"
```

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Backup/export failed |
| 2 | Verification failed |
| 3 | Restore failed |
| 4 | Invalid backup |

### Troubleshooting

#### Backup Failed

**Problem:** Cannot create backup

**Solutions:**
1. Check disk space
2. Check write permissions
3. Ensure database not locked

#### Checksum Mismatch

**Problem:** Backup verification failed

**Solutions:**
1. Backup may be corrupted
2. Create new backup
3. Check disk errors

#### Restore Failed

**Problem:** Cannot restore from backup

**Solutions:**
1. Verify backup first
2. Check backup format matches database type
3. Ensure database not in use

---

## Acceptance Criteria

### Backup Core Functionality

- [ ] AC-001: SQLite backup creates valid database file using sqlite3_backup_* API
- [ ] AC-002: PostgreSQL backup executes pg_dump with --format=custom
- [ ] AC-003: Backup manifest JSON file created alongside backup file
- [ ] AC-004: SHA-256 checksum computed and stored in manifest
- [ ] AC-005: Backup completes atomically (no partial files on failure)
- [ ] AC-006: Backup handles WAL mode SQLite databases correctly
- [ ] AC-007: Backup file named with ISO8601 timestamp: acode_backup_YYYYMMDD_HHMMSS.db
- [ ] AC-008: Backup works while database has active read operations
- [ ] AC-009: Backup reports progress during long-running operations
- [ ] AC-010: Backup fails gracefully if source database is corrupted

### Backup Manifest

- [ ] AC-011: Manifest includes backup version for forward compatibility
- [ ] AC-012: Manifest includes creation timestamp in ISO8601 format
- [ ] AC-013: Manifest includes database type (sqlite/postgresql)
- [ ] AC-014: Manifest includes schema version from meta table
- [ ] AC-015: Manifest includes file size in bytes
- [ ] AC-016: Manifest includes SHA-256 checksum with sha256: prefix
- [ ] AC-017: Manifest includes list of tables included
- [ ] AC-018: Manifest includes record count per table
- [ ] AC-019: Manifest written atomically after backup completes
- [ ] AC-020: Manifest file has same base name as backup with .json extension

### Backup Location & Storage

- [ ] AC-021: Default backup directory is .agent/backups/ relative to workspace
- [ ] AC-022: Backup directory configurable via database.backup_dir config
- [ ] AC-023: Backup directory auto-created if not exists
- [ ] AC-024: Backup files have restrictive permissions (600/owner-only)
- [ ] AC-025: Backup directory has restrictive permissions (700/owner-only)
- [ ] AC-026: Insufficient disk space detected before backup starts
- [ ] AC-027: Required space calculated as database size + 500MB buffer

### Backup Rotation

- [ ] AC-028: Maximum backup count configurable (default 7)
- [ ] AC-029: Oldest backup deleted when count exceeds maximum
- [ ] AC-030: Rotation deletes both backup file and manifest
- [ ] AC-031: Rotation occurs only after successful backup completion
- [ ] AC-032: Rotation logs which backup was deleted
- [ ] AC-033: Backup list command shows all backups with metadata

### Migration Hooks

- [ ] AC-034: Pre-migration backup hook enabled by default
- [ ] AC-035: Pre-migration backup hook configurable via database.pre_migration_backup
- [ ] AC-036: Pre-migration backup created before first migration runs
- [ ] AC-037: Migration aborted if pre-migration backup fails
- [ ] AC-038: Pre-migration backup named with _pre_migrate suffix
- [ ] AC-039: Automatic restore available if migration fails

### Restore Core Functionality

- [ ] AC-040: SQLite restore copies backup file to database location
- [ ] AC-041: PostgreSQL restore executes pg_restore with --clean
- [ ] AC-042: Restore verifies backup checksum before proceeding
- [ ] AC-043: Restore creates backup of current database before overwriting
- [ ] AC-044: Restore requires explicit confirmation (--force to skip)
- [ ] AC-045: Restore clears connection pool after completion
- [ ] AC-046: Restore verifies database integrity after completion
- [ ] AC-047: Restore reports success/failure with clear message
- [ ] AC-048: Restore to test mode available (--test flag, isolated copy)
- [ ] AC-049: Restore handles missing WAL files gracefully

### Backup Verification

- [ ] AC-050: Verify command checks backup file exists
- [ ] AC-051: Verify command checks manifest exists
- [ ] AC-052: Verify command recomputes SHA-256 checksum
- [ ] AC-053: Verify command compares checksum to manifest
- [ ] AC-054: Verify command reports pass/fail clearly
- [ ] AC-055: Verify --all option checks all backups in directory
- [ ] AC-056: Verify detects truncated or incomplete backup files
- [ ] AC-057: Constant-time comparison used for checksum validation

### Export Core Functionality

- [ ] AC-058: JSON export creates valid JSON file
- [ ] AC-059: CSV export creates valid CSV with header row
- [ ] AC-060: SQLite export creates self-contained database file
- [ ] AC-061: Export selects all tables by default
- [ ] AC-062: Export --tables flag limits to specified tables
- [ ] AC-063: Export --exclude-tables flag excludes specified tables
- [ ] AC-064: Export handles null values appropriately per format
- [ ] AC-065: Export handles datetime values in ISO8601 format
- [ ] AC-066: Export handles binary data (base64 for JSON/CSV)
- [ ] AC-067: Export reports progress during long-running operations

### Export Metadata

- [ ] AC-068: Export includes creation timestamp in file
- [ ] AC-069: Export includes schema version in file
- [ ] AC-070: Export indicates if redaction was applied
- [ ] AC-071: Export includes record counts per table
- [ ] AC-072: JSON export uses UTF-8 encoding without BOM

### Redaction Core Functionality

- [ ] AC-073: Redaction applies to columns matching *_key, *_secret, *_token patterns
- [ ] AC-074: Redaction applies to content matching sk-*, ghp_*, xoxb-* patterns
- [ ] AC-075: Redaction replaces matched content with [REDACTED-*] placeholder
- [ ] AC-076: Redaction is irreversible (original data not recoverable)
- [ ] AC-077: Redaction applies to all selected tables
- [ ] AC-078: Custom redaction patterns configurable via config
- [ ] AC-079: Redaction validates completeness post-processing

### Redaction Logging

- [ ] AC-080: Redaction log file created for each redacted export
- [ ] AC-081: Redaction log includes count of redacted items per type
- [ ] AC-082: Redaction log includes field names redacted (not values)
- [ ] AC-083: Redaction log does NOT include original sensitive values

### Redaction Dry-Run

- [ ] AC-084: --dry-run flag shows what would be redacted without exporting
- [ ] AC-085: Dry-run output includes table name, field name, redaction type
- [ ] AC-086: Dry-run output includes total counts per redaction pattern
- [ ] AC-087: Dry-run does not create any files

### CLI Backup Commands

- [ ] AC-088: `acode db backup` creates new backup with default settings
- [ ] AC-089: `acode db backup --name <name>` uses custom backup name
- [ ] AC-090: `acode db backup list` shows all backups with metadata
- [ ] AC-091: `acode db backup list --format json` outputs JSON for scripting
- [ ] AC-092: `acode db restore <backup>` restores from specified backup
- [ ] AC-093: `acode db restore --test <backup>` tests restore without overwriting
- [ ] AC-094: `acode db backup verify <backup>` verifies backup integrity
- [ ] AC-095: `acode db backup verify --all` verifies all backups
- [ ] AC-096: `acode db backup delete <backup>` deletes specified backup
- [ ] AC-097: Delete command requires confirmation unless --force

### CLI Export Commands

- [ ] AC-098: `acode db export` exports all tables to JSON
- [ ] AC-099: `acode db export --format csv` exports as CSV
- [ ] AC-100: `acode db export --format sqlite` exports as SQLite database
- [ ] AC-101: `acode db export --tables t1,t2` exports only specified tables
- [ ] AC-102: `acode db export --redact` applies redaction patterns
- [ ] AC-103: `acode db export --redact --dry-run` shows what would be redacted
- [ ] AC-104: `acode db export --output <path>` specifies output file path
- [ ] AC-105: Export to stdout available with `--output -`

### Error Handling

- [ ] AC-106: ACODE-BAK-001 returned for backup creation failures
- [ ] AC-107: ACODE-BAK-002 returned for restore failures
- [ ] AC-108: ACODE-BAK-003 returned for verification failures
- [ ] AC-109: ACODE-BAK-004 returned for checksum mismatch
- [ ] AC-110: ACODE-EXP-001 returned for export failures
- [ ] AC-111: ACODE-EXP-002 returned for redaction failures
- [ ] AC-112: All errors include actionable recovery suggestions

### Security

- [ ] AC-113: Backup file permissions are owner-only (0600)
- [ ] AC-114: Backup directory permissions are owner-only (0700)
- [ ] AC-115: Metadata sanitized (no usernames, paths in manifest)
- [ ] AC-116: Checksum comparison uses constant-time algorithm
- [ ] AC-117: Resource limits prevent disk space exhaustion
- [ ] AC-118: Redaction validated before export marked complete

---

## Best Practices

### Backup Strategy

1. **Backup before migrations** - Always create backup before schema changes
2. **Test restore regularly** - Untested backups are not backups; verify restoration works
3. **Rotate backups** - Keep multiple generations; single backup is single point of failure
4. **Store offsite** - Copy critical backups to different location/medium

### Backup Operations

5. **Use atomic APIs** - SQLite backup API, pg_dump ensure consistent snapshots
6. **Verify after backup** - Run integrity check on backup file before considering complete
7. **Compress for storage** - gzip reduces backup size 5-10x for typical databases
8. **Timestamp filenames** - Include ISO8601 timestamp for sorting and identification

### Restore Safety

9. **Restore to staging first** - Verify backup on non-production before depending on it
10. **Confirm before overwrite** - Always prompt when restoring over existing database
11. **Keep current as backup** - Backup current state before restoring old version
12. **Document restore procedure** - Write runbook for restore under pressure

---

## Troubleshooting

### Issue 1: Backup File Corrupted (ACODE-BAK-003)

**Symptoms:** 
- Restore fails with "malformed database" or integrity errors
- Verification reports checksum mismatch
- Cannot open backup file in SQLite browser

**Root Causes:**
- Backup taken during active write using file copy instead of SQLite Backup API
- Disk error during backup write
- File transfer corruption (binary mode not used)
- Incomplete backup due to disk space exhaustion

**Diagnostic Steps:**
```powershell
# Check backup integrity
acode db backup verify acode_backup_20240120_143045.db --verbose

# Compare file sizes
$backup = Get-Item ".agent\backups\acode_backup_20240120_143045.db"
$manifest = Get-Content ".agent\backups\acode_backup_20240120_143045.json" | ConvertFrom-Json
Write-Host "Expected: $($manifest.file_size), Actual: $($backup.Length)"

# Check SQLite file header
$header = [System.IO.File]::ReadAllBytes($backup.FullName)[0..15]
[System.Text.Encoding]::ASCII.GetString($header) # Should be "SQLite format 3"
```

**Solutions:**

```csharp
// Acode.Infrastructure/Backup/Diagnostics/BackupDiagnostics.cs
using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Acode.Domain.Backup;

namespace Acode.Infrastructure.Backup.Diagnostics;

/// <summary>
/// Diagnostics for backup corruption issues.
/// </summary>
public sealed class BackupDiagnostics
{
    public BackupDiagnosticResult DiagnoseCorruption(string backupPath)
    {
        var result = new BackupDiagnosticResult { BackupPath = backupPath };
        
        // Check 1: File exists and has content
        if (!File.Exists(backupPath))
        {
            result.AddError("Backup file does not exist");
            return result;
        }
        
        var fileInfo = new FileInfo(backupPath);
        if (fileInfo.Length == 0)
        {
            result.AddError("Backup file is empty - likely disk space issue during backup");
            result.SuggestedAction = "Re-run backup after freeing disk space";
            return result;
        }
        
        // Check 2: SQLite header magic
        var header = new byte[16];
        using (var fs = File.OpenRead(backupPath))
        {
            fs.Read(header, 0, 16);
        }
        
        var headerString = System.Text.Encoding.ASCII.GetString(header);
        if (!headerString.StartsWith("SQLite format 3"))
        {
            result.AddError($"Invalid SQLite header: '{headerString}'. File may be corrupted or not a SQLite database.");
            result.SuggestedAction = "Backup file is not a valid SQLite database. Use a different backup.";
            return result;
        }
        
        // Check 3: SQLite integrity check
        try
        {
            using var connection = new SqliteConnection($"Data Source={backupPath};Mode=ReadOnly");
            connection.Open();
            
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            var integrityResult = cmd.ExecuteScalar()?.ToString();
            
            if (integrityResult != "ok")
            {
                result.AddError($"SQLite integrity check failed: {integrityResult}");
                result.SuggestedAction = "Database is corrupted. Try an older backup.";
            }
            else
            {
                result.AddInfo("SQLite integrity check passed");
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Cannot open backup database: {ex.Message}");
            result.SuggestedAction = "Backup file is corrupted or incomplete";
        }
        
        return result;
    }
}
```

---

### Issue 2: Restore Changes Not Visible (ACODE-BAK-002)

**Symptoms:** 
- Restored data not appearing in application
- Application shows old data after restore
- Database file shows new timestamp but old content

**Root Causes:**
- Restored to wrong database file/instance
- Connection pooling caching stale connections
- Application not restarted after restore
- WAL journal not included in restore

**Diagnostic Steps:**
```powershell
# Check which database file is in use
acode config get database.path

# Check database file modification time
Get-Item "C:\path\to\acode.db" | Select-Object LastWriteTime

# Check for WAL files
Get-ChildItem "C:\path\to\acode.db*" | Select-Object Name, Length, LastWriteTime
```

**Solutions:**

```csharp
// Acode.Infrastructure/Backup/Restore/RestoreConnectionManager.cs
using System;
using Microsoft.Data.Sqlite;
using Acode.Domain.Backup;

namespace Acode.Infrastructure.Backup.Restore;

/// <summary>
/// Manages connection pool clearing after database restore.
/// </summary>
public sealed class RestoreConnectionManager
{
    private readonly ILogger<RestoreConnectionManager> _logger;

    public RestoreConnectionManager(ILogger<RestoreConnectionManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Clears all cached database connections to ensure fresh reads after restore.
    /// </summary>
    public void ClearConnectionPool()
    {
        // Clear the SQLite connection pool
        SqliteConnection.ClearAllPools();
        
        _logger.LogInformation("Database connection pool cleared after restore");
        
        // Force garbage collection to release file handles
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// Handles WAL checkpoint before restore to ensure all data is in main database.
    /// </summary>
    public void CheckpointWal(string databasePath)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={databasePath}");
            connection.Open();
            
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            cmd.ExecuteNonQuery();
            
            _logger.LogInformation("WAL checkpoint completed before restore");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WAL checkpoint failed (may not be using WAL mode)");
        }
    }

    /// <summary>
    /// Verifies restore was successful by checking a simple query.
    /// </summary>
    public bool VerifyRestoreComplete(string databasePath, string expectedSchemaVersion)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
            connection.Open();
            
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT value FROM meta_schema WHERE key = 'schema_version';";
            var version = cmd.ExecuteScalar()?.ToString();
            
            if (version == expectedSchemaVersion)
            {
                _logger.LogInformation(
                    "Restore verification passed. Schema version: {Version}", version);
                return true;
            }
            
            _logger.LogWarning(
                "Restore verification failed. Expected version: {Expected}, Got: {Actual}",
                expectedSchemaVersion, version);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore verification failed");
            return false;
        }
    }
}
```

---

### Issue 3: Backup Takes Too Long (Performance)

**Symptoms:** 
- Large database backup times out
- Backup impacts application performance
- Progress appears stuck at certain percentage

**Root Causes:**
- Full backup of very large database (> 1GB)
- Disk I/O contention during backup
- Backup destination on slow storage
- Many concurrent reads/writes during backup

**Diagnostic Steps:**
```powershell
# Check database size
$db = Get-Item "C:\path\to\acode.db"
Write-Host "Database size: $([math]::Round($db.Length / 1MB, 2)) MB"

# Check disk I/O during backup
Get-Counter '\PhysicalDisk(*)\% Disk Time' -SampleInterval 1 -MaxSamples 10

# Check backup destination write speed
$testFile = ".agent\backups\speedtest.tmp"
$data = [byte[]](,0 * 100MB)
Measure-Command { [System.IO.File]::WriteAllBytes($testFile, $data) }
Remove-Item $testFile
```

**Solutions:**

```csharp
// Acode.Infrastructure/Backup/Performance/OptimizedBackupService.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Acode.Domain.Backup;

namespace Acode.Infrastructure.Backup.Performance;

/// <summary>
/// Optimized backup service with progress reporting and cancellation.
/// Uses SQLite backup API with configurable page step size.
/// </summary>
public sealed class OptimizedBackupService
{
    private readonly BackupOptions _options;
    private readonly ILogger<OptimizedBackupService> _logger;

    // Larger step = faster backup, but more impact on concurrent operations
    private const int DefaultPagesPerStep = 100;
    private const int SleepMillisecondsBetweenSteps = 10;

    public OptimizedBackupService(BackupOptions options, ILogger<OptimizedBackupService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<BackupResult> CreateBackupAsync(
        string sourcePath,
        string destinationPath,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        using var sourceConnection = new SqliteConnection($"Data Source={sourcePath};Mode=ReadOnly");
        await sourceConnection.OpenAsync(cancellationToken);
        
        using var destConnection = new SqliteConnection($"Data Source={destinationPath}");
        await destConnection.OpenAsync(cancellationToken);

        // Use SQLite backup API for consistent, atomic backup
        var pagesPerStep = _options.BackupPagesPerStep ?? DefaultPagesPerStep;
        var sleepMs = _options.BackupSleepBetweenStepsMs ?? SleepMillisecondsBetweenSteps;

        var totalPages = 0;
        var pagesCompleted = 0;

        sourceConnection.BackupDatabase(destConnection, "main", "main",
            (remaining, pageCount) =>
            {
                if (totalPages == 0) totalPages = pageCount;
                pagesCompleted = totalPages - remaining;
                
                var percentage = totalPages > 0 
                    ? (int)((pagesCompleted * 100.0) / totalPages) 
                    : 0;
                    
                progress?.Report(new BackupProgress
                {
                    PercentComplete = percentage,
                    PagesCompleted = pagesCompleted,
                    TotalPages = totalPages,
                    ElapsedTime = stopwatch.Elapsed
                });

                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    return false; // Abort backup
                }

                // Sleep to reduce impact on concurrent operations
                if (sleepMs > 0)
                {
                    Thread.Sleep(sleepMs);
                }

                return true; // Continue backup
            }, pagesPerStep);

        stopwatch.Stop();

        var result = new BackupResult
        {
            Success = true,
            BackupPath = destinationPath,
            Duration = stopwatch.Elapsed,
            TotalPages = totalPages
        };

        _logger.LogInformation(
            "Backup completed in {Duration:F2}s. Pages: {Pages}",
            stopwatch.Elapsed.TotalSeconds, totalPages);

        return result;
    }
}
```

---

### Issue 4: Export Redaction Missing Sensitive Data (ACODE-EXP-002)

**Symptoms:**
- Exported file contains API keys or tokens after redaction
- Sensitive data in custom columns not redacted
- Regex patterns not matching expected content

**Root Causes:**
- Custom column names not in default redaction patterns
- New secret format not recognized by patterns
- Redaction patterns have regex errors
- Content encoding issues (base64 encoded secrets)

**Diagnostic Steps:**
```powershell
# Run dry-run to see what would be redacted
acode db export --format json --redact --dry-run

# Check redaction patterns configuration
acode config get export.redaction_patterns

# Search for known secret patterns in export
Select-String -Path "export.json" -Pattern "sk-|ghp_|xoxb-" -AllMatches
```

**Solutions:**

```csharp
// Acode.Infrastructure/Backup/Redaction/RedactionPatternValidator.cs
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Acode.Domain.Backup;

namespace Acode.Infrastructure.Backup.Redaction;

/// <summary>
/// Validates redaction patterns are correctly configured and comprehensive.
/// </summary>
public sealed class RedactionPatternValidator
{
    private readonly ILogger<RedactionPatternValidator> _logger;

    // Known test patterns that should always be caught
    private static readonly Dictionary<string, string> KnownSecretPatterns = new()
    {
        ["OpenAI"] = "sk-proj-test123456789012345678901234567890123456789012",
        ["GitHub PAT"] = "ghp_test1234567890123456789012345678901234",
        ["GitHub OAuth"] = "gho_test1234567890123456789012345678901234",
        ["Slack Bot"] = "xoxb-123456789-123456789-abcdefghijklmnop",
        ["AWS Key"] = "AKIAIOSFODNN7EXAMPLE",
        ["Google API"] = "AIzaSyTest1234567890123456789012345678",
    };

    public RedactionPatternValidator(ILogger<RedactionPatternValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates that all configured patterns are syntactically correct.
    /// </summary>
    public PatternValidationResult ValidatePatterns(IEnumerable<string> patterns)
    {
        var result = new PatternValidationResult { IsValid = true };
        
        foreach (var pattern in patterns)
        {
            try
            {
                var regex = new Regex(pattern, RegexOptions.Compiled);
                result.ValidPatterns.Add(pattern);
            }
            catch (ArgumentException ex)
            {
                result.IsValid = false;
                result.InvalidPatterns.Add(new PatternError
                {
                    Pattern = pattern,
                    Error = ex.Message
                });
            }
        }
        
        return result;
    }

    /// <summary>
    /// Tests that redaction catches all known secret patterns.
    /// </summary>
    public RedactionTestResult TestRedactionCompleteness(
        IRedactionPipeline pipeline, 
        RedactionOptions options)
    {
        var result = new RedactionTestResult();
        
        foreach (var (name, testValue) in KnownSecretPatterns)
        {
            var testRecord = new ExportRecord
            {
                Id = "test",
                TableName = "test",
                Fields = new Dictionary<string, object>
                {
                    ["test_field"] = testValue
                }
            };
            
            var redactedResult = pipeline.Redact(testRecord, options);
            var redactedValue = redactedResult.RedactedRecord.Fields["test_field"]?.ToString();
            
            if (redactedValue == testValue)
            {
                result.MissedPatterns.Add(new MissedPattern
                {
                    Name = name,
                    SampleValue = testValue.Substring(0, Math.Min(20, testValue.Length)) + "..."
                });
                
                _logger.LogWarning(
                    "Redaction missed {PatternName} pattern. Value not redacted.",
                    name);
            }
            else
            {
                result.CaughtPatterns.Add(name);
            }
        }
        
        result.IsComplete = result.MissedPatterns.Count == 0;
        return result;
    }

    /// <summary>
    /// Adds custom patterns for columns that should be redacted.
    /// </summary>
    public void AddColumnPattern(string columnPattern, RedactionOptions options)
    {
        options.ColumnPatterns.Add(columnPattern);
        _logger.LogInformation("Added column redaction pattern: {Pattern}", columnPattern);
    }
}
```

---

### Issue 5: Checksum Mismatch on Valid Backup (ACODE-BAK-004)

**Symptoms:**
- Verification fails with checksum mismatch error
- Backup appears intact but won't restore
- Error occurs only on transferred backups

**Root Causes:**
- File transferred in text mode (newline conversion)
- Manifest file modified after backup creation
- Filesystem with checksum-altering features (compression, dedup)
- Backup file modified by antivirus or backup software

**Diagnostic Steps:**
```powershell
# Recalculate checksum locally
$hash = Get-FileHash ".agent\backups\acode_backup_20240120_143045.db" -Algorithm SHA256
$manifest = Get-Content ".agent\backups\acode_backup_20240120_143045.json" | ConvertFrom-Json
Write-Host "Computed: $($hash.Hash)"
Write-Host "Expected: $($manifest.checksum -replace 'sha256:', '')"

# Check file attributes for modification
Get-Item ".agent\backups\acode_backup_20240120_143045.db" | Select-Object Attributes, LastWriteTime
```

**Solutions:**

```csharp
// Acode.Infrastructure/Backup/Verification/ChecksumMismatchDiagnostics.cs
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using Acode.Domain.Backup;

namespace Acode.Infrastructure.Backup.Verification;

/// <summary>
/// Diagnoses checksum mismatch issues and provides recovery options.
/// </summary>
public sealed class ChecksumMismatchDiagnostics
{
    private readonly ILogger<ChecksumMismatchDiagnostics> _logger;

    public ChecksumMismatchDiagnostics(ILogger<ChecksumMismatchDiagnostics> logger)
    {
        _logger = logger;
    }

    public ChecksumDiagnosticResult Diagnose(string backupPath)
    {
        var result = new ChecksumDiagnosticResult { BackupPath = backupPath };
        var manifestPath = Path.ChangeExtension(backupPath, ".json");
        
        // Load manifest
        if (!File.Exists(manifestPath))
        {
            result.Issue = ChecksumIssue.ManifestMissing;
            result.SuggestedAction = "Regenerate manifest with --recompute-checksum";
            return result;
        }
        
        var manifest = JsonSerializer.Deserialize<BackupManifest>(
            File.ReadAllText(manifestPath));
        
        // Compute current checksum
        using var stream = File.OpenRead(backupPath);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
        var computedChecksum = Convert.ToHexString(hashBytes).ToLowerInvariant();
        var expectedChecksum = manifest!.Checksum.Replace("sha256:", "").ToLowerInvariant();
        
        result.ComputedChecksum = computedChecksum;
        result.ExpectedChecksum = expectedChecksum;
        
        if (computedChecksum == expectedChecksum)
        {
            result.Issue = ChecksumIssue.None;
            return result;
        }
        
        // Check for common issues
        var fileInfo = new FileInfo(backupPath);
        var manifestInfo = new FileInfo(manifestPath);
        
        // Issue 1: File size mismatch
        if (fileInfo.Length != manifest.FileSize)
        {
            result.Issue = ChecksumIssue.FileSizeDifference;
            result.Details = $"Expected {manifest.FileSize} bytes, got {fileInfo.Length} bytes";
            result.SuggestedAction = "File was modified or truncated. Re-download or use different backup.";
            return result;
        }
        
        // Issue 2: Manifest newer than backup (manifest modified)
        if (manifestInfo.LastWriteTimeUtc > fileInfo.LastWriteTimeUtc)
        {
            result.Issue = ChecksumIssue.ManifestModified;
            result.Details = "Manifest was modified after backup creation";
            result.SuggestedAction = "Use --recompute-checksum to regenerate manifest from backup file";
            return result;
        }
        
        // Issue 3: Possible text-mode transfer
        // Check first few KB for CRLF/LF differences
        var headerBytes = new byte[4096];
        stream.Seek(0, SeekOrigin.Begin);
        stream.Read(headerBytes, 0, headerBytes.Length);
        
        var crlfCount = 0;
        for (int i = 0; i < headerBytes.Length - 1; i++)
        {
            if (headerBytes[i] == 0x0D && headerBytes[i + 1] == 0x0A) crlfCount++;
        }
        
        if (crlfCount > 10)
        {
            result.Issue = ChecksumIssue.TextModeTransfer;
            result.Details = "File may have been transferred in text mode causing newline conversion";
            result.SuggestedAction = "Re-transfer file in binary mode (FTP: TYPE I, SCP: binary)";
            return result;
        }
        
        result.Issue = ChecksumIssue.Unknown;
        result.SuggestedAction = "Try restoring with --skip-checksum (WARNING: data integrity not guaranteed)";
        
        return result;
    }

    /// <summary>
    /// Recomputes and updates the manifest checksum to match actual file.
    /// Use only when you trust the backup file content.
    /// </summary>
    public void RecomputeManifestChecksum(string backupPath)
    {
        var manifestPath = Path.ChangeExtension(backupPath, ".json");
        
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Manifest file not found", manifestPath);
        }
        
        var manifest = JsonSerializer.Deserialize<BackupManifest>(
            File.ReadAllText(manifestPath))!;
        
        // Compute new checksum
        using var stream = File.OpenRead(backupPath);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
        var newChecksum = "sha256:" + Convert.ToHexString(hashBytes).ToLowerInvariant();
        
        _logger.LogWarning(
            "Recomputing checksum. Old: {Old}, New: {New}",
            manifest.Checksum, newChecksum);
        
        manifest.Checksum = newChecksum;
        manifest.FileSize = new FileInfo(backupPath).Length;
        
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, options));
        
        _logger.LogInformation("Manifest checksum updated");
    }
}

public enum ChecksumIssue
{
    None,
    ManifestMissing,
    FileSizeDifference,
    ManifestModified,
    TextModeTransfer,
    Unknown
}
```

---

### Issue 6: Pre-Migration Backup Hook Failing (ACODE-BAK-001)

**Symptoms:**
- Migrations fail at "Creating pre-migration backup" step
- Error message: "Failed to create backup: [reason]"
- Migrations abort even though database is healthy

**Root Causes:**
- Insufficient disk space for backup
- Backup directory permissions issue
- Database locked by another process
- SQLite WAL file preventing atomic backup

**Diagnostic Steps:**
```powershell
# Check disk space
Get-PSDrive -PSProvider FileSystem | Select-Object Name, @{N="Free(GB)";E={[math]::Round($_.Free/1GB,2)}}

# Check backup directory permissions
$backupDir = ".agent\backups"
Get-Acl $backupDir | Format-List

# Check for locks on database
$dbPath = "C:\path\to\acode.db"
Get-Process | Where-Object { $_.Modules.FileName -contains $dbPath }

# Check WAL files
Get-ChildItem (Split-Path $dbPath) -Filter "*.db*" | Select-Object Name, Length
```

**Solutions:**

```csharp
// Acode.Infrastructure/Backup/Hooks/PreMigrationBackupHook.cs
using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Acode.Domain.Backup;
using Acode.Domain.Migration;

namespace Acode.Infrastructure.Backup.Hooks;

/// <summary>
/// Pre-migration backup hook with comprehensive error handling.
/// </summary>
public sealed class PreMigrationBackupHook : IMigrationHook
{
    private readonly IBackupService _backupService;
    private readonly BackupOptions _options;
    private readonly ILogger<PreMigrationBackupHook> _logger;

    public PreMigrationBackupHook(
        IBackupService backupService,
        BackupOptions options,
        ILogger<PreMigrationBackupHook> logger)
    {
        _backupService = backupService;
        _options = options;
        _logger = logger;
    }

    public int Order => 1; // Run first before any migrations

    public async Task<HookResult> ExecuteAsync(MigrationContext context)
    {
        if (!_options.PreMigrationBackupEnabled)
        {
            _logger.LogDebug("Pre-migration backup disabled in configuration");
            return HookResult.Success();
        }

        try
        {
            // Pre-check 1: Disk space
            var backupDir = _options.BackupDirectory ?? 
                Path.Combine(context.WorkspacePath, ".agent", "backups");
            var drive = new DriveInfo(Path.GetPathRoot(backupDir)!);
            var requiredSpace = new FileInfo(context.DatabasePath).Length * 1.2; // 20% buffer
            
            if (drive.AvailableFreeSpace < requiredSpace)
            {
                return HookResult.Failed(
                    "ACODE-BAK-001",
                    $"Insufficient disk space for backup. " +
                    $"Required: {requiredSpace / 1024 / 1024:F1} MB, " +
                    $"Available: {drive.AvailableFreeSpace / 1024 / 1024:F1} MB");
            }

            // Pre-check 2: WAL checkpoint (ensure all data in main file)
            try
            {
                await CheckpointWalAsync(context.DatabasePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WAL checkpoint failed, continuing with backup");
            }

            // Pre-check 3: Close any local connections
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Create backup
            var backupName = $"acode_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}_pre_migrate";
            var result = await _backupService.CreateBackupAsync(
                context.DatabasePath,
                backupName,
                context.CancellationToken);

            if (!result.Success)
            {
                return HookResult.Failed("ACODE-BAK-001", result.ErrorMessage);
            }

            context.Metadata["PreMigrationBackupPath"] = result.BackupPath;
            
            _logger.LogInformation(
                "Pre-migration backup created: {BackupPath}", result.BackupPath);

            return HookResult.Success();
        }
        catch (IOException ex) when (ex.Message.Contains("being used by another process"))
        {
            return HookResult.Failed(
                "ACODE-BAK-001",
                "Database is locked by another process. " +
                "Close all applications using the database and retry.");
        }
        catch (UnauthorizedAccessException ex)
        {
            return HookResult.Failed(
                "ACODE-BAK-001",
                $"Permission denied creating backup: {ex.Message}. " +
                "Check backup directory permissions.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pre-migration backup failed");
            return HookResult.Failed("ACODE-BAK-001", ex.Message);
        }
    }

    private async Task CheckpointWalAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
        await cmd.ExecuteNonQueryAsync();
    }
}
```

---

### Issue 7: Export Format Incompatibility (ACODE-EXP-001)

**Symptoms:**
- JSON export not parsing in target application
- CSV export has encoding issues or wrong delimiters
- SQLite export cannot be opened by external tools

**Root Causes:**
- Incorrect character encoding (UTF-8 BOM issues)
- Non-standard JSON date formats
- CSV delimiter conflicts with data content
- SQLite version incompatibility

**Diagnostic Steps:**
```powershell
# Check JSON validity
python -m json.tool export.json

# Check CSV encoding
$bytes = [System.IO.File]::ReadAllBytes("export.csv")[0..2]
if ($bytes -eq @(0xEF, 0xBB, 0xBF)) { "UTF-8 with BOM" } else { "No BOM" }

# Check SQLite version
sqlite3 export.db "SELECT sqlite_version();"
```

**Solutions:**

```csharp
// Acode.Infrastructure/Backup/Export/ExportFormatHandler.cs
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Acode.Domain.Backup;

namespace Acode.Infrastructure.Backup.Export;

/// <summary>
/// Handles export format compatibility issues.
/// </summary>
public sealed class ExportFormatHandler
{
    private readonly ExportOptions _options;
    private readonly ILogger<ExportFormatHandler> _logger;

    public ExportFormatHandler(ExportOptions options, ILogger<ExportFormatHandler> logger)
    {
        _options = options;
        _logger = logger;
    }

    public ExportWriterConfig GetJsonConfig()
    {
        return new ExportWriterConfig
        {
            // Use UTF-8 without BOM for maximum compatibility
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            JsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                // ISO 8601 dates for universal compatibility
                Converters = { new Iso8601DateTimeConverter() },
                // Escape non-ASCII for compatibility with legacy parsers
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                // Handle circular references
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            }
        };
    }

    public ExportWriterConfig GetCsvConfig()
    {
        var delimiter = _options.CsvDelimiter ?? ",";
        
        // Auto-detect if data contains delimiter and use alternative
        return new ExportWriterConfig
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: _options.IncludeBom),
            CsvOptions = new CsvExportOptions
            {
                Delimiter = delimiter,
                QuoteChar = '"',
                EscapeChar = '"', // RFC 4180 standard
                NewLine = _options.UseCrLf ? "\r\n" : "\n",
                QuoteAllFields = _options.AlwaysQuoteCsv,
                DateFormat = "yyyy-MM-dd HH:mm:ss" // ISO-like for spreadsheets
            }
        };
    }

    public ExportWriterConfig GetSqliteConfig()
    {
        return new ExportWriterConfig
        {
            SqliteOptions = new SqliteExportOptions
            {
                // Use legacy-compatible schema syntax
                UseLegacySyntax = _options.SqliteLegacyMode,
                // Include table creation statements
                IncludeSchema = true,
                // Use INSERT OR REPLACE for idempotent imports
                UseInsertOrReplace = true,
                // Disable WAL in export for maximum compatibility
                JournalMode = "DELETE"
            }
        };
    }

    /// <summary>
    /// Validates exported file is readable by common tools.
    /// </summary>
    public ValidationResult ValidateExport(string exportPath, ExportFormat format)
    {
        try
        {
            switch (format)
            {
                case ExportFormat.Json:
                    var json = File.ReadAllText(exportPath);
                    JsonDocument.Parse(json);
                    break;
                    
                case ExportFormat.Csv:
                    var lines = File.ReadLines(exportPath).Take(10);
                    foreach (var line in lines)
                    {
                        // Basic CSV structure validation
                        if (string.IsNullOrEmpty(line)) continue;
                    }
                    break;
                    
                case ExportFormat.Sqlite:
                    using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={exportPath};Mode=ReadOnly"))
                    {
                        conn.Open();
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT sqlite_version();";
                        var version = cmd.ExecuteScalar();
                        _logger.LogDebug("Export SQLite version: {Version}", version);
                    }
                    break;
            }
            
            return new ValidationResult { IsValid = true };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                IsValid = false,
                Error = ex.Message,
                SuggestedAction = format switch
                {
                    ExportFormat.Json => "Check for invalid UTF-8 sequences or control characters",
                    ExportFormat.Csv => "Try different delimiter with --csv-delimiter",
                    ExportFormat.Sqlite => "Try --sqlite-legacy-mode for older SQLite versions",
                    _ => "Retry export with different options"
                }
            };
        }
    }
}
```

---

## Testing Requirements

### Unit Tests

```csharp
// Acode.Application.Tests/Backup/BackupServiceTests.cs
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Acode.Application.Backup;
using Acode.Domain.Backup;

namespace Acode.Application.Tests.Backup;

public class BackupServiceTests : IDisposable
{
    private readonly Mock<IBackupProvider> _backupProviderMock;
    private readonly Mock<IManifestBuilder> _manifestBuilderMock;
    private readonly Mock<IBackupStorage> _storageMock;
    private readonly Mock<ILogger<BackupService>> _loggerMock;
    private readonly BackupService _sut;
    private readonly string _tempDir;

    public BackupServiceTests()
    {
        _backupProviderMock = new Mock<IBackupProvider>();
        _manifestBuilderMock = new Mock<IManifestBuilder>();
        _storageMock = new Mock<IBackupStorage>();
        _loggerMock = new Mock<ILogger<BackupService>>();
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var options = new BackupOptions
        {
            BackupDirectory = _tempDir,
            MaxBackups = 7
        };

        _sut = new BackupService(
            _backupProviderMock.Object,
            _manifestBuilderMock.Object,
            _storageMock.Object,
            options,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldCreateBackupFile_WhenDatabaseExists()
    {
        // Arrange
        var sourcePath = Path.Combine(_tempDir, "source.db");
        File.WriteAllBytes(sourcePath, new byte[1024]);
        
        var backupPath = Path.Combine(_tempDir, "backup.db");
        _storageMock.Setup(s => s.CreateBackupPath(It.IsAny<string>()))
            .Returns(backupPath);
        
        _backupProviderMock.Setup(p => p.CreateBackupAsync(
                sourcePath, backupPath, It.IsAny<IProgress<BackupProgress>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ProviderBackupResult { Success = true, BytesWritten = 1024 }));

        // Act
        var result = await _sut.CreateBackupAsync(sourcePath);

        // Assert
        Assert.True(result.Success);
        _manifestBuilderMock.Verify(m => m.CreateManifest(It.IsAny<BackupInfo>()), Times.Once);
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldComputeChecksum_WhenBackupCompletes()
    {
        // Arrange
        var sourcePath = Path.Combine(_tempDir, "source.db");
        File.WriteAllBytes(sourcePath, new byte[1024]);
        
        var backupPath = Path.Combine(_tempDir, "backup.db");
        File.WriteAllBytes(backupPath, new byte[1024]);
        
        _storageMock.Setup(s => s.CreateBackupPath(It.IsAny<string>()))
            .Returns(backupPath);
        
        _backupProviderMock.Setup(p => p.CreateBackupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProgress<BackupProgress>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ProviderBackupResult { Success = true, BytesWritten = 1024 }));

        // Act
        var result = await _sut.CreateBackupAsync(sourcePath);

        // Assert
        Assert.NotNull(result.Checksum);
        Assert.StartsWith("sha256:", result.Checksum);
        Assert.Equal(64 + 7, result.Checksum.Length); // sha256: prefix + 64 hex chars
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldFail_WhenSourceDoesNotExist()
    {
        // Arrange
        var sourcePath = Path.Combine(_tempDir, "nonexistent.db");

        // Act
        var result = await _sut.CreateBackupAsync(sourcePath);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ACODE-BAK-001", result.ErrorCode);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldFail_WhenDiskSpaceInsufficient()
    {
        // Arrange
        var sourcePath = Path.Combine(_tempDir, "source.db");
        File.WriteAllBytes(sourcePath, new byte[1024]);
        
        _storageMock.Setup(s => s.CheckDiskSpace(It.IsAny<string>(), It.IsAny<long>()))
            .Returns(new DiskSpaceCheckResult { Sufficient = false, Available = 100, Required = 1500 });

        // Act
        var result = await _sut.CreateBackupAsync(sourcePath);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("disk space", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}

// Acode.Application.Tests/Backup/BackupRotationTests.cs
public class BackupRotationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly BackupRotationService _sut;
    private readonly Mock<ILogger<BackupRotationService>> _loggerMock;

    public BackupRotationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _loggerMock = new Mock<ILogger<BackupRotationService>>();

        var options = new BackupOptions { BackupDirectory = _tempDir, MaxBackups = 3 };
        _sut = new BackupRotationService(options, _loggerMock.Object);
    }

    [Fact]
    public void ApplyRotation_ShouldKeepMaxBackups_WhenMoreExist()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var backupPath = Path.Combine(_tempDir, $"acode_backup_2024010{i}_120000.db");
            File.WriteAllBytes(backupPath, new byte[100]);
            File.WriteAllText(Path.ChangeExtension(backupPath, ".json"), "{}");
            Thread.Sleep(10); // Ensure different timestamps
        }

        // Act
        _sut.ApplyRotation();

        // Assert
        var remaining = Directory.GetFiles(_tempDir, "*.db").Length;
        Assert.Equal(3, remaining);
    }

    [Fact]
    public void ApplyRotation_ShouldDeleteOldestFirst()
    {
        // Arrange
        var oldest = Path.Combine(_tempDir, "acode_backup_20240101_120000.db");
        var middle = Path.Combine(_tempDir, "acode_backup_20240102_120000.db");
        var newest = Path.Combine(_tempDir, "acode_backup_20240103_120000.db");
        
        File.WriteAllBytes(oldest, new byte[100]);
        File.WriteAllBytes(middle, new byte[100]);
        File.WriteAllBytes(newest, new byte[100]);
        
        // Set creation times explicitly
        File.SetCreationTimeUtc(oldest, DateTime.UtcNow.AddDays(-3));
        File.SetCreationTimeUtc(middle, DateTime.UtcNow.AddDays(-2));
        File.SetCreationTimeUtc(newest, DateTime.UtcNow.AddDays(-1));

        // Act
        _sut.ApplyRotation();

        // Assert
        Assert.False(File.Exists(oldest)); // Oldest should be deleted
        Assert.True(File.Exists(middle));
        Assert.True(File.Exists(newest));
    }

    [Fact]
    public void ApplyRotation_ShouldDeleteManifestWithBackup()
    {
        // Arrange
        for (int i = 0; i < 4; i++)
        {
            var backupPath = Path.Combine(_tempDir, $"acode_backup_2024010{i}_120000.db");
            File.WriteAllBytes(backupPath, new byte[100]);
            File.WriteAllText(Path.ChangeExtension(backupPath, ".json"), "{}");
            File.SetCreationTimeUtc(backupPath, DateTime.UtcNow.AddDays(-4 + i));
        }

        // Act
        _sut.ApplyRotation();

        // Assert
        var manifests = Directory.GetFiles(_tempDir, "*.json").Length;
        Assert.Equal(3, manifests); // Oldest manifest should be deleted
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}

// Acode.Application.Tests/Backup/RedactionServiceTests.cs
public class RedactionServiceTests
{
    private readonly RedactionService _sut;
    private readonly Mock<IRedactionAuditLogger> _auditLoggerMock;

    public RedactionServiceTests()
    {
        _auditLoggerMock = new Mock<IRedactionAuditLogger>();
        var options = new RedactionOptions
        {
            ColumnPatterns = new List<string> { "*_key", "*_secret", "*_token" },
            ContentPatterns = new List<string> { @"sk-[a-zA-Z0-9]+", @"ghp_[a-zA-Z0-9]+" }
        };
        _sut = new RedactionService(options, _auditLoggerMock.Object);
    }

    [Theory]
    [InlineData("api_key", "sk-12345", "[REDACTED-API-KEY]")]
    [InlineData("access_secret", "mysecret", "[REDACTED-SECRET]")]
    [InlineData("auth_token", "bearer123", "[REDACTED-TOKEN]")]
    public void Redact_ShouldRedactMatchingColumns(string columnName, string value, string expectedContains)
    {
        // Arrange
        var record = new ExportRecord
        {
            Id = "1",
            TableName = "test",
            Fields = new Dictionary<string, object> { [columnName] = value }
        };

        // Act
        var result = _sut.Redact(record);

        // Assert
        Assert.Contains("[REDACTED", result.Fields[columnName]?.ToString());
    }

    [Fact]
    public void Redact_ShouldRedactContentPatterns_EvenInNonSensitiveColumns()
    {
        // Arrange
        var record = new ExportRecord
        {
            Id = "1",
            TableName = "test",
            Fields = new Dictionary<string, object>
            {
                ["message"] = "Use your API key: sk-1234567890abcdef for access"
            }
        };

        // Act
        var result = _sut.Redact(record);

        // Assert
        Assert.DoesNotContain("sk-", result.Fields["message"]?.ToString());
        Assert.Contains("[REDACTED", result.Fields["message"]?.ToString());
    }

    [Fact]
    public void Redact_ShouldLogAllRedactions()
    {
        // Arrange
        var record = new ExportRecord
        {
            Id = "1",
            TableName = "test",
            Fields = new Dictionary<string, object>
            {
                ["api_key"] = "sk-12345",
                ["name"] = "safe value"
            }
        };

        // Act
        _sut.Redact(record);

        // Assert
        _auditLoggerMock.Verify(l => l.LogRedaction(
            It.Is<string>(id => id == "1"),
            It.Is<IEnumerable<RedactedField>>(fields => fields.Any(f => f.FieldName == "api_key"))),
            Times.Once);
    }

    [Fact]
    public void Redact_ShouldNotModifyNonSensitiveData()
    {
        // Arrange
        var record = new ExportRecord
        {
            Id = "1",
            TableName = "test",
            Fields = new Dictionary<string, object>
            {
                ["name"] = "John Doe",
                ["email"] = "john@example.com",
                ["count"] = 42
            }
        };

        // Act
        var result = _sut.Redact(record);

        // Assert
        Assert.Equal("John Doe", result.Fields["name"]);
        Assert.Equal("john@example.com", result.Fields["email"]);
        Assert.Equal(42, result.Fields["count"]);
    }
}

// Acode.Application.Tests/Backup/ExportServiceTests.cs
public class ExportServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mock<IExportWriter> _jsonWriterMock;
    private readonly Mock<IExportWriter> _csvWriterMock;
    private readonly Mock<ITableReader> _tableReaderMock;
    private readonly ExportService _sut;

    public ExportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _jsonWriterMock = new Mock<IExportWriter>();
        _csvWriterMock = new Mock<IExportWriter>();
        _tableReaderMock = new Mock<ITableReader>();

        var writerFactory = new Mock<IExportWriterFactory>();
        writerFactory.Setup(f => f.Create(ExportFormat.Json)).Returns(_jsonWriterMock.Object);
        writerFactory.Setup(f => f.Create(ExportFormat.Csv)).Returns(_csvWriterMock.Object);

        _sut = new ExportService(writerFactory.Object, _tableReaderMock.Object, Mock.Of<ILogger<ExportService>>());
    }

    [Fact]
    public async Task ExportAsync_ShouldWriteAllTables_WhenNoTablesSpecified()
    {
        // Arrange
        var tables = new[] { "table1", "table2", "table3" };
        _tableReaderMock.Setup(r => r.GetTableNames()).Returns(tables);
        _tableReaderMock.Setup(r => r.ReadTableAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<ExportRecord>());

        var options = new ExportOptions { Format = ExportFormat.Json, OutputPath = Path.Combine(_tempDir, "export.json") };

        // Act
        await _sut.ExportAsync(options);

        // Assert
        foreach (var table in tables)
        {
            _tableReaderMock.Verify(r => r.ReadTableAsync(table), Times.Once);
        }
    }

    [Fact]
    public async Task ExportAsync_ShouldFilterTables_WhenTablesSpecified()
    {
        // Arrange
        var tables = new[] { "table1", "table2", "table3" };
        _tableReaderMock.Setup(r => r.GetTableNames()).Returns(tables);
        _tableReaderMock.Setup(r => r.ReadTableAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<ExportRecord>());

        var options = new ExportOptions
        {
            Format = ExportFormat.Json,
            OutputPath = Path.Combine(_tempDir, "export.json"),
            Tables = new[] { "table1", "table3" }
        };

        // Act
        await _sut.ExportAsync(options);

        // Assert
        _tableReaderMock.Verify(r => r.ReadTableAsync("table1"), Times.Once);
        _tableReaderMock.Verify(r => r.ReadTableAsync("table2"), Times.Never);
        _tableReaderMock.Verify(r => r.ReadTableAsync("table3"), Times.Once);
    }

    [Theory]
    [InlineData(ExportFormat.Json)]
    [InlineData(ExportFormat.Csv)]
    public async Task ExportAsync_ShouldUseCorrectWriter(ExportFormat format)
    {
        // Arrange
        _tableReaderMock.Setup(r => r.GetTableNames()).Returns(new[] { "test" });
        _tableReaderMock.Setup(r => r.ReadTableAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<ExportRecord>());

        var options = new ExportOptions
        {
            Format = format,
            OutputPath = Path.Combine(_tempDir, $"export.{format.ToString().ToLower()}")
        };

        // Act
        await _sut.ExportAsync(options);

        // Assert
        var expectedMock = format == ExportFormat.Json ? _jsonWriterMock : _csvWriterMock;
        expectedMock.Verify(w => w.WriteAsync(It.IsAny<IEnumerable<ExportRecord>>(), It.IsAny<string>()), Times.Once);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}

// Acode.Application.Tests/Backup/BackupVerifierTests.cs
public class BackupVerifierTests : IDisposable
{
    private readonly string _tempDir;
    private readonly BackupIntegrityVerifier _sut;

    public BackupVerifierTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _sut = new BackupIntegrityVerifier(Mock.Of<ILogger<BackupIntegrityVerifier>>());
    }

    [Fact]
    public void Verify_ShouldPass_WhenChecksumMatches()
    {
        // Arrange
        var backupPath = CreateTestBackup("valid_backup");
        
        // Act
        var result = _sut.Verify(backupPath);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Verify_ShouldFail_WhenChecksumMismatch()
    {
        // Arrange
        var backupPath = CreateTestBackup("tampered_backup");
        File.WriteAllBytes(backupPath, new byte[200]); // Modify after manifest created

        // Act
        var result = _sut.Verify(backupPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(BackupVerificationError.ChecksumMismatch, result.ErrorType);
    }

    [Fact]
    public void Verify_ShouldFail_WhenManifestMissing()
    {
        // Arrange
        var backupPath = Path.Combine(_tempDir, "no_manifest.db");
        File.WriteAllBytes(backupPath, new byte[100]);

        // Act
        var result = _sut.Verify(backupPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(BackupVerificationError.ManifestMissing, result.ErrorType);
    }

    private string CreateTestBackup(string name)
    {
        var backupPath = Path.Combine(_tempDir, $"{name}.db");
        var content = new byte[100];
        new Random(42).NextBytes(content);
        File.WriteAllBytes(backupPath, content);

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(content);
        var checksum = "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();

        var manifest = new { version = "1.0", checksum, file_size = content.Length };
        var manifestPath = Path.ChangeExtension(backupPath, ".json");
        File.WriteAllText(manifestPath, System.Text.Json.JsonSerializer.Serialize(manifest));

        return backupPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
```

### Integration Tests

```csharp
// Acode.Integration.Tests/Backup/BackupIntegrationTests.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Acode.Infrastructure.Backup;
using Acode.Domain.Backup;

namespace Acode.Integration.Tests.Backup;

[Collection("Database")]
public class BackupIntegrationTests : IAsyncLifetime
{
    private readonly string _tempDir;
    private readonly string _databasePath;
    private readonly IServiceProvider _services;

    public BackupIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _databasePath = Path.Combine(_tempDir, "test.db");

        var services = new ServiceCollection();
        services.AddBackupServices(new BackupOptions
        {
            BackupDirectory = Path.Combine(_tempDir, "backups"),
            MaxBackups = 5
        });
        _services = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        // Create test database with sample data
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE test_data (id INTEGER PRIMARY KEY, name TEXT, secret_key TEXT);
            INSERT INTO test_data (name, secret_key) VALUES ('Item1', 'sk-secret123');
            INSERT INTO test_data (name, secret_key) VALUES ('Item2', 'ghp_token456');
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task BackupAndRestore_ShouldPreserveData()
    {
        // Arrange
        var backupService = _services.GetRequiredService<IBackupService>();
        var restoreService = _services.GetRequiredService<IRestoreService>();

        // Act - Create backup
        var backupResult = await backupService.CreateBackupAsync(_databasePath);
        Assert.True(backupResult.Success);

        // Modify database
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM test_data;";
        await cmd.ExecuteNonQueryAsync();
        connection.Close();
        SqliteConnection.ClearAllPools();

        // Act - Restore
        var restoreResult = await restoreService.RestoreAsync(backupResult.BackupPath, _databasePath, force: true);
        Assert.True(restoreResult.Success);

        // Assert - Data should be restored
        await using var verifyConnection = new SqliteConnection($"Data Source={_databasePath}");
        await verifyConnection.OpenAsync();
        await using var verifyCmd = verifyConnection.CreateCommand();
        verifyCmd.CommandText = "SELECT COUNT(*) FROM test_data;";
        var count = Convert.ToInt32(await verifyCmd.ExecuteScalarAsync());
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Backup_ShouldCreateValidManifest()
    {
        // Arrange
        var backupService = _services.GetRequiredService<IBackupService>();

        // Act
        var result = await backupService.CreateBackupAsync(_databasePath);

        // Assert
        Assert.True(result.Success);
        var manifestPath = Path.ChangeExtension(result.BackupPath, ".json");
        Assert.True(File.Exists(manifestPath));

        var manifest = System.Text.Json.JsonSerializer.Deserialize<BackupManifest>(
            await File.ReadAllTextAsync(manifestPath));
        Assert.NotNull(manifest);
        Assert.Equal("sqlite", manifest.DatabaseType);
        Assert.StartsWith("sha256:", manifest.Checksum);
    }

    [Fact]
    public async Task BackupVerify_ShouldDetectTamperedBackup()
    {
        // Arrange
        var backupService = _services.GetRequiredService<IBackupService>();
        var verifier = _services.GetRequiredService<IBackupVerifier>();

        var result = await backupService.CreateBackupAsync(_databasePath);
        Assert.True(result.Success);

        // Tamper with backup
        await File.WriteAllBytesAsync(result.BackupPath, new byte[100]);

        // Act
        var verifyResult = verifier.Verify(result.BackupPath);

        // Assert
        Assert.False(verifyResult.IsValid);
        Assert.Equal(BackupVerificationError.ChecksumMismatch, verifyResult.ErrorType);
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
        return Task.CompletedTask;
    }
}

// Acode.Integration.Tests/Backup/ExportIntegrationTests.cs
[Collection("Database")]
public class ExportIntegrationTests : IAsyncLifetime
{
    private readonly string _tempDir;
    private readonly string _databasePath;
    private readonly IServiceProvider _services;

    public ExportIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _databasePath = Path.Combine(_tempDir, "test.db");

        var services = new ServiceCollection();
        services.AddExportServices(new ExportOptions());
        services.AddRedactionServices(new RedactionOptions
        {
            ColumnPatterns = new List<string> { "*_key", "*_secret" },
            ContentPatterns = new List<string> { @"sk-[a-zA-Z0-9]+", @"ghp_[a-zA-Z0-9]+" }
        });
        _services = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT, api_key TEXT);
            INSERT INTO users (name, api_key) VALUES ('Alice', 'sk-alice12345678901234567890');
            INSERT INTO users (name, api_key) VALUES ('Bob', 'ghp_bob12345678901234567890');
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task ExportJson_ShouldCreateValidJson()
    {
        // Arrange
        var exportService = _services.GetRequiredService<IExportService>();
        var outputPath = Path.Combine(_tempDir, "export.json");

        // Act
        var result = await exportService.ExportAsync(new ExportOptions
        {
            DatabasePath = _databasePath,
            Format = ExportFormat.Json,
            OutputPath = outputPath
        });

        // Assert
        Assert.True(result.Success);
        Assert.True(File.Exists(outputPath));
        
        var json = await File.ReadAllTextAsync(outputPath);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.NotNull(doc);
    }

    [Fact]
    public async Task ExportWithRedaction_ShouldRemoveSensitiveData()
    {
        // Arrange
        var exportService = _services.GetRequiredService<IExportService>();
        var outputPath = Path.Combine(_tempDir, "redacted_export.json");

        // Act
        var result = await exportService.ExportAsync(new ExportOptions
        {
            DatabasePath = _databasePath,
            Format = ExportFormat.Json,
            OutputPath = outputPath,
            EnableRedaction = true
        });

        // Assert
        Assert.True(result.Success);
        var content = await File.ReadAllTextAsync(outputPath);
        
        Assert.DoesNotContain("sk-alice", content);
        Assert.DoesNotContain("ghp_bob", content);
        Assert.Contains("[REDACTED", content);
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
        return Task.CompletedTask;
    }
}
```

### E2E Tests

```csharp
// Acode.E2E.Tests/Backup/BackupE2ETests.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Acode.Cli.Commands;
using Acode.Cli.Testing;

namespace Acode.E2E.Tests.Backup;

[Collection("E2E")]
public class BackupE2ETests : E2ETestBase
{
    [Fact]
    public async Task BackupCommand_ShouldCreateBackupAndManifest()
    {
        // Arrange
        await InitializeDatabaseAsync();

        // Act
        var result = await RunCommandAsync("acode db backup");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Backup created", result.Output);
        
        var backups = Directory.GetFiles(BackupDirectory, "*.db");
        Assert.Single(backups);
        
        var manifests = Directory.GetFiles(BackupDirectory, "*.json");
        Assert.Single(manifests);
    }

    [Fact]
    public async Task BackupListCommand_ShouldShowAllBackups()
    {
        // Arrange
        await InitializeDatabaseAsync();
        await RunCommandAsync("acode db backup");
        await Task.Delay(1100); // Ensure different timestamp
        await RunCommandAsync("acode db backup");

        // Act
        var result = await RunCommandAsync("acode db backup list");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("acode_backup_", result.Output);
        Assert.Matches(@"\d{8}_\d{6}\.db", result.Output);
    }

    [Fact]
    public async Task RestoreCommand_ShouldRequireConfirmation()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var backupResult = await RunCommandAsync("acode db backup");
        var backupPath = ExtractBackupPath(backupResult.Output);

        // Act - without --force
        var result = await RunCommandAsync($"acode db restore {backupPath}");

        // Assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("confirmation required", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RestoreCommand_ShouldWorkWithForce()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var backupResult = await RunCommandAsync("acode db backup");
        var backupPath = ExtractBackupPath(backupResult.Output);

        // Modify database
        await ModifyDatabaseAsync();

        // Act
        var result = await RunCommandAsync($"acode db restore {backupPath} --force");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Restore completed", result.Output);
    }

    [Fact]
    public async Task ExportCommand_ShouldSupportAllFormats()
    {
        // Arrange
        await InitializeDatabaseAsync();

        // Act & Assert - JSON
        var jsonResult = await RunCommandAsync("acode db export --format json --output export.json");
        Assert.Equal(0, jsonResult.ExitCode);
        Assert.True(File.Exists(Path.Combine(WorkspaceDirectory, "export.json")));

        // Act & Assert - CSV
        var csvResult = await RunCommandAsync("acode db export --format csv --output export.csv");
        Assert.Equal(0, csvResult.ExitCode);

        // Act & Assert - SQLite
        var sqliteResult = await RunCommandAsync("acode db export --format sqlite --output export.db");
        Assert.Equal(0, sqliteResult.ExitCode);
    }

    [Fact]
    public async Task ExportRedactCommand_ShouldRedactSensitiveData()
    {
        // Arrange
        await InitializeDatabaseWithSecretsAsync();

        // Act
        var result = await RunCommandAsync("acode db export --format json --redact --output redacted.json");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        var content = await File.ReadAllTextAsync(Path.Combine(WorkspaceDirectory, "redacted.json"));
        Assert.DoesNotContain("sk-", content);
        Assert.DoesNotContain("ghp_", content);
        Assert.Contains("[REDACTED", content);
    }
}
```

### Performance Benchmarks

```csharp
// Acode.Benchmarks/Backup/BackupBenchmarks.cs
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Data.Sqlite;

namespace Acode.Benchmarks.Backup;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class BackupBenchmarks
{
    private string _sourcePath = null!;
    private string _destPath = null!;
    private byte[] _testData = null!;

    [Params(1, 10, 100)]
    public int DatabaseSizeMB { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _sourcePath = Path.Combine(Path.GetTempPath(), $"benchmark_source_{Guid.NewGuid():N}.db");
        _destPath = Path.Combine(Path.GetTempPath(), $"benchmark_dest_{Guid.NewGuid():N}.db");
        
        // Create database of specified size
        using var connection = new SqliteConnection($"Data Source={_sourcePath}");
        connection.Open();
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE data (id INTEGER PRIMARY KEY, content BLOB);";
        cmd.ExecuteNonQuery();
        
        _testData = new byte[1024 * 1024]; // 1MB chunks
        new Random(42).NextBytes(_testData);
        
        for (int i = 0; i < DatabaseSizeMB; i++)
        {
            cmd.CommandText = "INSERT INTO data (content) VALUES (@content);";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@content", _testData);
            cmd.ExecuteNonQuery();
        }
    }

    [Benchmark]
    public void SqliteBackupApi()
    {
        if (File.Exists(_destPath)) File.Delete(_destPath);
        
        using var source = new SqliteConnection($"Data Source={_sourcePath}");
        using var dest = new SqliteConnection($"Data Source={_destPath}");
        
        source.Open();
        dest.Open();
        
        source.BackupDatabase(dest);
    }

    [Benchmark]
    public void FileCopy()
    {
        if (File.Exists(_destPath)) File.Delete(_destPath);
        File.Copy(_sourcePath, _destPath);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_sourcePath)) File.Delete(_sourcePath);
        if (File.Exists(_destPath)) File.Delete(_destPath);
    }
}

// Expected Results:
// | Method          | DatabaseSizeMB | Mean      | Allocated |
// |-----------------|----------------|-----------|-----------|
// | SqliteBackupApi | 1              | 12.5 ms   | 2.1 KB    |
// | SqliteBackupApi | 10             | 125 ms    | 2.1 KB    |
// | SqliteBackupApi | 100            | 1,250 ms  | 2.1 KB    |
// | FileCopy        | 1              | 8.2 ms    | 4.2 KB    |
// | FileCopy        | 10             | 82 ms     | 4.2 KB    |
// | FileCopy        | 100            | 820 ms    | 4.2 KB    |

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class RedactionBenchmarks
{
    private List<ExportRecord> _records = null!;
    private RedactionService _sut = null!;

    [Params(100, 1000, 10000)]
    public int RecordCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _records = Enumerable.Range(1, RecordCount).Select(i => new ExportRecord
        {
            Id = i.ToString(),
            TableName = "test",
            Fields = new Dictionary<string, object>
            {
                ["name"] = $"Item {i}",
                ["api_key"] = $"sk-{Guid.NewGuid():N}",
                ["description"] = "Some description with ghp_token123 embedded"
            }
        }).ToList();

        _sut = new RedactionService(new RedactionOptions
        {
            ColumnPatterns = new List<string> { "*_key" },
            ContentPatterns = new List<string> { @"sk-[a-zA-Z0-9]+", @"ghp_[a-zA-Z0-9]+" }
        }, Mock.Of<IRedactionAuditLogger>());
    }

    [Benchmark]
    public void RedactRecords()
    {
        foreach (var record in _records)
        {
            _sut.Redact(record);
        }
    }
}
```

### Performance Targets

| Benchmark | Target | Maximum | NFR Reference |
|-----------|--------|---------|---------------|
| 100MB backup | 5s | 10s | NFR-001 |
| 100MB export | 15s | 30s | NFR-002 |
| Backup verification | 2s | 5s | NFR-003 |
| Redaction (10K records) | 1s | 3s | NFR-004 |
| Checksum computation | 500ms/100MB | 1s/100MB | NFR-005 |
| Rotation cleanup | 200ms | 1s | NFR-006 |

---

## User Verification Steps

### Scenario 1: Create Backup with Default Settings

**Objective:** Verify that the backup command creates a valid backup file and manifest.

**Prerequisites:**
- Database exists with at least one table containing data
- .agent/backups/ directory is writable (or will be created)

**Steps:**
```powershell
# Step 1: Run backup command
acode db backup

# Expected Output:
# Creating backup...
#   Source: C:\project\.agent\acode.db
#   Destination: .agent\backups\acode_backup_20240120_143045.db
#   Progress: [████████████████████] 100%
#   Checksum: sha256:a1b2c3d4e5f6...
#
# ✓ Backup created successfully
#   Size: 4.5 MB
#   Duration: 1.2s

# Step 2: Verify backup file exists
Get-Item ".agent\backups\acode_backup_*.db" | Select-Object Name, Length

# Expected: One .db file with size similar to source database

# Step 3: Verify manifest exists
Get-Item ".agent\backups\acode_backup_*.json" | Select-Object Name

# Expected: One .json manifest file with same base name

# Step 4: Verify manifest content
Get-Content ".agent\backups\acode_backup_20240120_143045.json" | ConvertFrom-Json

# Expected:
# {
#   "version": "1.0",
#   "created_at": "2024-01-20T14:30:45Z",
#   "database_type": "sqlite",
#   "schema_version": "1.0.5",
#   "file_size": 4718592,
#   "checksum": "sha256:a1b2c3d4e5f6...",
#   "tables": ["conv_chats", "conv_runs", ...],
#   "record_counts": { "conv_chats": 25, ... }
# }
```

---

### Scenario 2: List All Backups with Metadata

**Objective:** Verify that the backup list command shows all backups with useful metadata.

**Prerequisites:**
- At least 3 backups exist

**Steps:**
```powershell
# Step 1: Create multiple backups with small delay
acode db backup
Start-Sleep -Seconds 2
acode db backup
Start-Sleep -Seconds 2
acode db backup

# Step 2: List backups
acode db backup list

# Expected Output:
# Backups in .agent\backups\:
#
# NAME                              SIZE      CREATED              SCHEMA    STATUS
# ────────────────────────────────────────────────────────────────────────────────
# acode_backup_20240120_143045.db   4.5 MB    2024-01-20 14:30:45  1.0.5     ✓ Valid
# acode_backup_20240120_143047.db   4.5 MB    2024-01-20 14:30:47  1.0.5     ✓ Valid
# acode_backup_20240120_143049.db   4.5 MB    2024-01-20 14:30:49  1.0.5     ✓ Valid
#
# Total: 3 backups, 13.5 MB

# Step 3: List backups as JSON for scripting
acode db backup list --format json | ConvertFrom-Json

# Expected: JSON array with backup objects
```

---

### Scenario 3: Restore from Backup (Full Cycle)

**Objective:** Verify that restore correctly reverts database to backup state.

**Prerequisites:**
- Database has data
- Backup created

**Steps:**
```powershell
# Step 1: Check current data count
acode db query "SELECT COUNT(*) FROM conv_chats"
# Expected: count: 25

# Step 2: Create backup
acode db backup
# Note the backup filename

# Step 3: Modify data
acode db query "DELETE FROM conv_chats WHERE id > 10"
acode db query "SELECT COUNT(*) FROM conv_chats"
# Expected: count: 10

# Step 4: Restore without force (should prompt)
acode db restore acode_backup_20240120_143045.db

# Expected Output:
# ⚠ Warning: This will overwrite the current database.
#   Backup will be created before restore.
#
# Current database:
#   Tables: 5
#   Records: 10
#
# Backup being restored:
#   Created: 2024-01-20 14:30:45
#   Tables: 5
#   Records: 25
#
# Type 'yes' to confirm, or use --force to skip: _

# Step 5: Restore with force
acode db restore acode_backup_20240120_143045.db --force

# Expected Output:
# Restoring from: acode_backup_20240120_143045.db
#   ✓ Backup checksum verified
#   ✓ Pre-restore backup created: acode_backup_20240120_143100_pre_restore.db
#   ✓ Database restored
#   ✓ Integrity verified
#
# ✓ Restore completed successfully

# Step 6: Verify data is restored
acode db query "SELECT COUNT(*) FROM conv_chats"
# Expected: count: 25
```

---

### Scenario 4: Backup Verification (Detect Tampering)

**Objective:** Verify that backup verification detects modified or corrupted backups.

**Prerequisites:**
- Valid backup exists

**Steps:**
```powershell
# Step 1: Create backup
acode db backup

# Step 2: Verify valid backup
acode db backup verify acode_backup_20240120_143045.db

# Expected Output:
# Verifying backup: acode_backup_20240120_143045.db
#   ✓ Manifest found
#   ✓ File size matches: 4,718,592 bytes
#   ✓ Checksum verified: sha256:a1b2c3d4...
#   ✓ SQLite integrity check: ok
#
# ✓ Backup is valid and restorable

# Step 3: Tamper with backup file
$bytes = [System.IO.File]::ReadAllBytes(".agent\backups\acode_backup_20240120_143045.db")
$bytes[100] = 0xFF
[System.IO.File]::WriteAllBytes(".agent\backups\acode_backup_20240120_143045.db", $bytes)

# Step 4: Verify tampered backup
acode db backup verify acode_backup_20240120_143045.db

# Expected Output:
# Verifying backup: acode_backup_20240120_143045.db
#   ✓ Manifest found
#   ✓ File size matches: 4,718,592 bytes
#   ✗ Checksum mismatch!
#       Expected: sha256:a1b2c3d4...
#       Computed: sha256:9f8e7d6c...
#
# ✗ Backup verification FAILED (ACODE-BAK-004)
#   The backup file has been modified since creation.
#   This could indicate corruption or tampering.
#   Do NOT restore this backup.
```

---

### Scenario 5: Backup Rotation (Max Backups Enforced)

**Objective:** Verify that backup rotation deletes oldest backups when limit exceeded.

**Prerequisites:**
- Empty backup directory
- Max backups set to 3

**Steps:**
```powershell
# Step 1: Configure max backups to 3 for testing
acode config set database.max_backups 3

# Step 2: Create 5 backups
for ($i = 1; $i -le 5; $i++) {
    acode db backup
    Start-Sleep -Seconds 2
}

# Expected: Each backup after 3rd should trigger rotation
# Backup 4 output should include:
#   Rotation: Deleted acode_backup_20240120_143045.db (oldest)
# Backup 5 output should include:
#   Rotation: Deleted acode_backup_20240120_143047.db (oldest)

# Step 3: List backups
acode db backup list

# Expected: Only 3 backups remain (newest 3)
# Total: 3 backups

# Step 4: Verify oldest were deleted
$backups = Get-ChildItem ".agent\backups\*.db" | Sort-Object CreationTime
$backups.Count
# Expected: 3
```

---

### Scenario 6: Export to JSON with Table Selection

**Objective:** Verify that export creates valid JSON with optional table filtering.

**Prerequisites:**
- Database has multiple tables with data

**Steps:**
```powershell
# Step 1: Export all tables
acode db export --format json --output full_export.json

# Expected Output:
# Exporting database...
#   Format: JSON
#   Tables: conv_chats, conv_runs, conv_messages, sess_sessions, appr_approvals
#   Total records: 450
#   Progress: [████████████████████] 100%
#
# ✓ Export completed: full_export.json (2.3 MB)

# Step 2: Verify JSON is valid
python -c "import json; json.load(open('full_export.json'))"
# Expected: No errors

# Step 3: Export specific tables
acode db export --format json --tables conv_chats,conv_runs --output partial_export.json

# Expected Output:
# Exporting database...
#   Format: JSON
#   Tables: conv_chats, conv_runs
#   Total records: 175
#   Progress: [████████████████████] 100%
#
# ✓ Export completed: partial_export.json (0.8 MB)

# Step 4: Verify only selected tables in export
(Get-Content partial_export.json | ConvertFrom-Json).PSObject.Properties.Name
# Expected: conv_chats, conv_runs (only these two tables)
```

---

### Scenario 7: Redacted Export with Dry-Run Preview

**Objective:** Verify that redaction preview shows what will be redacted without creating files.

**Prerequisites:**
- Database has records with sensitive data (API keys, tokens)

**Steps:**
```powershell
# Step 1: Add test data with secrets
acode db query "INSERT INTO conv_messages (content) VALUES ('Use key: sk-1234567890abcdef')"
acode db query "INSERT INTO conv_messages (content) VALUES ('Token: ghp_abcdef123456')"

# Step 2: Run dry-run to preview redaction
acode db export --format json --redact --dry-run

# Expected Output:
# Redaction dry-run (no files will be created)
#
# REDACTION PREVIEW:
#
# Table: conv_messages
#   Field: content
#     - sk-1234567890ab... → [REDACTED-API-KEY] (Pattern: OpenAI Key)
#     - ghp_abcdef12345... → [REDACTED-TOKEN]   (Pattern: GitHub PAT)
#
# Table: sess_sessions
#   Field: access_token
#     - xoxb-12345-6789... → [REDACTED-TOKEN]   (Pattern: Slack Token)
#
# SUMMARY:
#   Records to scan: 450
#   Fields to redact: 23
#   Patterns matched:
#     - OpenAI Key: 15
#     - GitHub PAT: 5
#     - Slack Token: 3
#
# No files created (dry-run mode)

# Step 3: Verify no files created
Test-Path "export.json"
# Expected: False
```

---

### Scenario 8: Redacted Export with Audit Log

**Objective:** Verify that redacted export creates audit log documenting all redactions.

**Prerequisites:**
- Database has sensitive data

**Steps:**
```powershell
# Step 1: Create redacted export
acode db export --format json --redact --output redacted_export.json

# Expected Output:
# Exporting database with redaction...
#   Format: JSON
#   Redaction: ENABLED
#   Progress: [████████████████████] 100%
#
# Redaction summary:
#   • API keys: 15 redacted
#   • Tokens: 8 redacted
#   • File paths: 12 redacted
#
# ✓ Export completed: redacted_export.json (2.1 MB)
# ✓ Redaction log: redacted_export.redaction.log

# Step 2: Verify no sensitive data in export
Select-String -Path redacted_export.json -Pattern "sk-|ghp_|xoxb-" -AllMatches
# Expected: No matches

# Step 3: Verify placeholders present
Select-String -Path redacted_export.json -Pattern "\[REDACTED-" -AllMatches
# Expected: Multiple matches for [REDACTED-API-KEY], [REDACTED-TOKEN], etc.

# Step 4: Review redaction log (should NOT contain original values)
Get-Content redacted_export.redaction.log

# Expected:
# Redaction Log - 2024-01-20T14:30:45Z
#
# Record: conv_messages/123
#   Field: content
#   Type: PATTERN (OpenAI Key)
#
# Record: sess_sessions/45
#   Field: access_token
#   Type: COLUMN (*_token)
#
# ... (NO original values shown)
```

---

### Scenario 9: Pre-Migration Backup Hook

**Objective:** Verify that migrations automatically create backup before running.

**Prerequisites:**
- Pre-migration backup enabled (default)
- Pending migrations exist

**Steps:**
```powershell
# Step 1: Check current schema version
acode db info

# Expected: Schema version: 1.0.5

# Step 2: Run migrations (with pending migration 1.0.6)
acode db migrate

# Expected Output:
# Database migration
#
# Pre-migration backup...
#   ✓ Creating backup: acode_backup_20240120_143045_pre_migrate_1.0.6.db
#   ✓ Checksum: sha256:a1b2c3d4...
#
# Applying migrations...
#   1/1: 1.0.6_add_metrics_table.sql
#   ✓ Migration applied
#
# ✓ Migration completed successfully
#   Previous version: 1.0.5
#   Current version: 1.0.6
#   Backup available: acode_backup_20240120_143045_pre_migrate_1.0.6.db

# Step 3: Verify pre-migration backup exists
Get-ChildItem ".agent\backups\*pre_migrate*"
# Expected: Backup file with pre_migrate suffix
```

---

### Scenario 10: Migration Rollback Using Backup

**Objective:** Verify that failed migration can be rolled back using pre-migration backup.

**Prerequisites:**
- Pre-migration backup enabled
- Migration script that will fail

**Steps:**
```powershell
# Step 1: Create a migration that will fail
# (Assume migration 1.0.7 has a bug that causes error)

# Step 2: Attempt migration
acode db migrate

# Expected Output:
# Database migration
#
# Pre-migration backup...
#   ✓ Creating backup: acode_backup_20240120_150000_pre_migrate_1.0.7.db
#   ✓ Checksum: sha256:e5f6a7b8...
#
# Applying migrations...
#   1/1: 1.0.7_bad_migration.sql
#   ✗ Migration FAILED
#     Error: no such column: nonexistent
#
# ⚠ Migration failed. Database may be in inconsistent state.
#   Pre-migration backup: acode_backup_20240120_150000_pre_migrate_1.0.7.db
#
# To restore to previous state:
#   acode db restore acode_backup_20240120_150000_pre_migrate_1.0.7.db --force

# Step 3: Restore from backup
acode db restore acode_backup_20240120_150000_pre_migrate_1.0.7.db --force

# Expected Output:
# Restoring from: acode_backup_20240120_150000_pre_migrate_1.0.7.db
#   ✓ Backup checksum verified
#   ✓ Pre-restore backup created
#   ✓ Database restored
#   ✓ Integrity verified
#
# ✓ Restore completed successfully

# Step 4: Verify database is back to previous state
acode db info
# Expected: Schema version: 1.0.6 (not 1.0.7)
```

---

## Implementation Prompt

### File Structure

```
src/Acode.Domain/
├── Backup/
│   ├── BackupResult.cs
│   ├── RestoreResult.cs
│   ├── BackupManifest.cs
│   ├── BackupInfo.cs
│   ├── VerificationResult.cs
│   ├── ExportRecord.cs
│   ├── RedactedField.cs
│   └── Enums/
│       ├── BackupVerificationError.cs
│       └── ExportFormat.cs
│
src/Acode.Application/
├── Backup/
│   ├── IBackupService.cs
│   ├── IRestoreService.cs
│   ├── IBackupVerifier.cs
│   ├── IExportService.cs
│   ├── IRedactionService.cs
│   ├── IBackupProvider.cs
│   ├── IManifestBuilder.cs
│   └── IBackupStorage.cs
│
src/Acode.Infrastructure/
├── Backup/
│   ├── BackupService.cs
│   ├── RestoreService.cs
│   ├── BackupVerifier.cs
│   ├── ManifestBuilder.cs
│   ├── BackupRotationService.cs
│   ├── SecureBackupStorage.cs
│   ├── Providers/
│   │   ├── SqliteBackupProvider.cs
│   │   └── PostgresBackupProvider.cs
│   ├── Hooks/
│   │   ├── PreMigrationBackupHook.cs
│   │   └── PrePurgeExportHook.cs
│   └── DependencyInjection/
│       └── BackupServiceExtensions.cs
│
├── Export/
│   ├── ExportService.cs
│   ├── Writers/
│   │   ├── IExportWriter.cs
│   │   ├── JsonExportWriter.cs
│   │   ├── CsvExportWriter.cs
│   │   └── SqliteExportWriter.cs
│   ├── Redaction/
│   │   ├── RedactionService.cs
│   │   ├── RedactionPipeline.cs
│   │   ├── RedactionValidator.cs
│   │   └── RedactionAuditLogger.cs
│   └── DependencyInjection/
│       └── ExportServiceExtensions.cs
│
src/Acode.Cli/
└── Commands/
    ├── Db/
    │   ├── BackupCommand.cs
    │   ├── BackupListCommand.cs
    │   ├── BackupVerifyCommand.cs
    │   ├── BackupDeleteCommand.cs
    │   ├── RestoreCommand.cs
    │   └── ExportCommand.cs
```

### Domain Models

```csharp
// Acode.Domain/Backup/BackupResult.cs
namespace Acode.Domain.Backup;

public sealed record BackupResult
{
    public bool Success { get; init; }
    public string? BackupPath { get; init; }
    public string? Checksum { get; init; }
    public long FileSize { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static BackupResult Succeeded(string backupPath, string checksum, long fileSize, TimeSpan duration)
        => new()
        {
            Success = true,
            BackupPath = backupPath,
            Checksum = checksum,
            FileSize = fileSize,
            Duration = duration
        };

    public static BackupResult Failed(string errorCode, string errorMessage)
        => new()
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
}

// Acode.Domain/Backup/RestoreResult.cs
public sealed record RestoreResult
{
    public bool Success { get; init; }
    public string? RestoredFrom { get; init; }
    public string? PreRestoreBackupPath { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

// Acode.Domain/Backup/BackupManifest.cs
public sealed class BackupManifest
{
    public string Version { get; set; } = "1.0";
    public DateTime CreatedAt { get; set; }
    public string DatabaseType { get; set; } = "sqlite";
    public string? SchemaVersion { get; set; }
    public long FileSize { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public List<string> Tables { get; set; } = new();
    public Dictionary<string, int> RecordCounts { get; set; } = new();
    public string? SourcePath { get; set; }
    public string? MachineName { get; set; }
    public string? Username { get; set; }
    public string? WorkingDirectory { get; set; }
}

// Acode.Domain/Backup/BackupInfo.cs
public sealed record BackupInfo
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required long FileSize { get; init; }
    public required string? SchemaVersion { get; init; }
    public required bool IsValid { get; init; }
    public string? Checksum { get; init; }
}

// Acode.Domain/Backup/ExportRecord.cs
public sealed class ExportRecord
{
    public required string Id { get; init; }
    public required string TableName { get; init; }
    public required Dictionary<string, object?> Fields { get; init; }

    public ExportRecord Clone()
    {
        return new ExportRecord
        {
            Id = Id,
            TableName = TableName,
            Fields = new Dictionary<string, object?>(Fields)
        };
    }

    public void SetField(string fieldName, object? value)
    {
        Fields[fieldName] = value;
    }
}

// Acode.Domain/Backup/RedactedField.cs
public sealed record RedactedField
{
    public required string FieldName { get; init; }
    public required RedactionType RedactionType { get; init; }
    public string? Reason { get; init; }
    public string? PatternMatched { get; init; }
}

public enum RedactionType
{
    ColumnPattern,
    ContentPattern,
    ValidationCatchAll
}

// Acode.Domain/Backup/Enums/BackupVerificationError.cs
public enum BackupVerificationError
{
    None,
    ManifestMissing,
    ManifestCorrupted,
    BackupFileMissing,
    ChecksumComputationFailed,
    ChecksumMismatch,
    SizeMismatch,
    IntegrityCheckFailed
}

// Acode.Domain/Backup/Enums/ExportFormat.cs
public enum ExportFormat
{
    Json,
    Csv,
    Sqlite
}
```

### Application Interfaces

```csharp
// Acode.Application/Backup/IBackupService.cs
namespace Acode.Application.Backup;

public interface IBackupService
{
    Task<BackupResult> CreateBackupAsync(
        string databasePath,
        string? customName = null,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackupInfo>> ListBackupsAsync(
        CancellationToken cancellationToken = default);
}

// Acode.Application/Backup/IRestoreService.cs
public interface IRestoreService
{
    Task<RestoreResult> RestoreAsync(
        string backupPath,
        string databasePath,
        bool force = false,
        CancellationToken cancellationToken = default);

    Task<RestoreResult> TestRestoreAsync(
        string backupPath,
        CancellationToken cancellationToken = default);
}

// Acode.Application/Backup/IBackupVerifier.cs
public interface IBackupVerifier
{
    VerificationResult Verify(string backupPath);
    Task<IReadOnlyList<VerificationResult>> VerifyAllAsync(CancellationToken cancellationToken = default);
    string ComputeChecksum(string filePath);
    bool MustVerifyBeforeRestore { get; }
}

// Acode.Application/Backup/IExportService.cs
public interface IExportService
{
    Task<ExportResult> ExportAsync(
        ExportOptions options,
        CancellationToken cancellationToken = default);

    Task<DryRunResult> DryRunAsync(
        ExportOptions options,
        CancellationToken cancellationToken = default);
}

// Acode.Application/Backup/IRedactionService.cs
public interface IRedactionService
{
    ExportRecord Redact(ExportRecord record);
    DryRunResult PreviewRedaction(IEnumerable<ExportRecord> records);
}

// Acode.Application/Backup/IBackupProvider.cs
public interface IBackupProvider
{
    string DatabaseType { get; }
    bool CanHandle(string databasePath);

    Task<ProviderBackupResult> CreateBackupAsync(
        string sourcePath,
        string destinationPath,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<ProviderRestoreResult> RestoreAsync(
        string backupPath,
        string destinationPath,
        CancellationToken cancellationToken = default);
}
```

### Infrastructure Implementation

```csharp
// Acode.Infrastructure/Backup/BackupService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Acode.Application.Backup;
using Acode.Domain.Backup;

namespace Acode.Infrastructure.Backup;

public sealed class BackupService : IBackupService
{
    private readonly IBackupProvider _provider;
    private readonly IManifestBuilder _manifestBuilder;
    private readonly IBackupStorage _storage;
    private readonly BackupRotationService _rotationService;
    private readonly BackupOptions _options;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        IBackupProvider provider,
        IManifestBuilder manifestBuilder,
        IBackupStorage storage,
        BackupRotationService rotationService,
        BackupOptions options,
        ILogger<BackupService> logger)
    {
        _provider = provider;
        _manifestBuilder = manifestBuilder;
        _storage = storage;
        _rotationService = rotationService;
        _options = options;
        _logger = logger;
    }

    public async Task<BackupResult> CreateBackupAsync(
        string databasePath,
        string? customName = null,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Validate source exists
        if (!File.Exists(databasePath))
        {
            return BackupResult.Failed("ACODE-BAK-001", $"Source database not found: {databasePath}");
        }

        // Check disk space
        var sourceSize = new FileInfo(databasePath).Length;
        var spaceCheck = _storage.CheckDiskSpace(_options.BackupDirectory, sourceSize);
        if (!spaceCheck.Sufficient)
        {
            return BackupResult.Failed("ACODE-BAK-001",
                $"Insufficient disk space. Required: {spaceCheck.Required / 1024 / 1024} MB, " +
                $"Available: {spaceCheck.Available / 1024 / 1024} MB");
        }

        // Generate backup path
        var backupName = customName ?? $"acode_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.db";
        var backupPath = _storage.CreateBackupPath(backupName);

        try
        {
            // Create backup using provider
            _logger.LogInformation("Creating backup from {Source} to {Destination}",
                databasePath, backupPath);

            var providerResult = await _provider.CreateBackupAsync(
                databasePath, backupPath, progress, cancellationToken);

            if (!providerResult.Success)
            {
                return BackupResult.Failed("ACODE-BAK-001", providerResult.ErrorMessage ?? "Backup failed");
            }

            // Compute checksum
            var checksum = ComputeChecksum(backupPath);
            var fileSize = new FileInfo(backupPath).Length;

            // Create and write manifest
            var manifest = await _manifestBuilder.CreateManifestAsync(new BackupInfo
            {
                Name = backupName,
                FullPath = backupPath,
                CreatedAt = DateTime.UtcNow,
                FileSize = fileSize,
                SchemaVersion = await GetSchemaVersionAsync(databasePath),
                IsValid = true,
                Checksum = checksum
            }, databasePath, cancellationToken);

            await _manifestBuilder.WriteManifestAsync(manifest, backupPath, cancellationToken);

            // Secure the backup file
            _storage.SecureBackupFile(backupPath);

            // Apply rotation policy
            _rotationService.ApplyRotation();

            stopwatch.Stop();

            _logger.LogInformation(
                "Backup completed: {BackupPath}, Size: {Size} bytes, Duration: {Duration}s",
                backupPath, fileSize, stopwatch.Elapsed.TotalSeconds);

            return BackupResult.Succeeded(backupPath, $"sha256:{checksum}", fileSize, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed: {Message}", ex.Message);
            
            // Clean up partial backup
            if (File.Exists(backupPath))
            {
                try { File.Delete(backupPath); } catch { }
            }

            return BackupResult.Failed("ACODE-BAK-001", ex.Message);
        }
    }

    public async Task<IReadOnlyList<BackupInfo>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        var backups = new List<BackupInfo>();
        var backupDir = _options.BackupDirectory;

        if (!Directory.Exists(backupDir))
            return backups;

        var files = Directory.GetFiles(backupDir, "*.db");

        foreach (var file in files.OrderByDescending(f => new FileInfo(f).CreationTimeUtc))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var manifest = await _manifestBuilder.ReadManifestAsync(file, cancellationToken);
            var fileInfo = new FileInfo(file);

            backups.Add(new BackupInfo
            {
                Name = Path.GetFileName(file),
                FullPath = file,
                CreatedAt = manifest?.CreatedAt ?? fileInfo.CreationTimeUtc,
                FileSize = fileInfo.Length,
                SchemaVersion = manifest?.SchemaVersion,
                IsValid = manifest != null,
                Checksum = manifest?.Checksum
            });
        }

        return backups;
    }

    private string ComputeChecksum(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private async Task<string?> GetSchemaVersionAsync(string databasePath)
    {
        // Implementation would query the meta_schema table
        await Task.CompletedTask;
        return "1.0.0"; // Placeholder
    }
}

// Acode.Infrastructure/Backup/Providers/SqliteBackupProvider.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Acode.Application.Backup;
using Acode.Domain.Backup;

namespace Acode.Infrastructure.Backup.Providers;

public sealed class SqliteBackupProvider : IBackupProvider
{
    private readonly ILogger<SqliteBackupProvider> _logger;

    public string DatabaseType => "sqlite";

    public SqliteBackupProvider(ILogger<SqliteBackupProvider> logger)
    {
        _logger = logger;
    }

    public bool CanHandle(string databasePath)
    {
        return databasePath.EndsWith(".db", StringComparison.OrdinalIgnoreCase) ||
               databasePath.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ProviderBackupResult> CreateBackupAsync(
        string sourcePath,
        string destinationPath,
        IProgress<BackupProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var sourceConnection = new SqliteConnection($"Data Source={sourcePath};Mode=ReadOnly");
            await sourceConnection.OpenAsync(cancellationToken);

            await using var destConnection = new SqliteConnection($"Data Source={destinationPath}");
            await destConnection.OpenAsync(cancellationToken);

            var totalPages = 0;
            var pagesCompleted = 0;

            sourceConnection.BackupDatabase(destConnection, "main", "main",
                (remaining, pageCount) =>
                {
                    if (totalPages == 0) totalPages = pageCount;
                    pagesCompleted = totalPages - remaining;

                    var percentage = totalPages > 0
                        ? (int)((pagesCompleted * 100.0) / totalPages)
                        : 0;

                    progress?.Report(new BackupProgress
                    {
                        PercentComplete = percentage,
                        PagesCompleted = pagesCompleted,
                        TotalPages = totalPages
                    });

                    return !cancellationToken.IsCancellationRequested;
                });

            _logger.LogDebug("SQLite backup completed: {Pages} pages", totalPages);

            return new ProviderBackupResult
            {
                Success = true,
                BytesWritten = new FileInfo(destinationPath).Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQLite backup failed");
            return new ProviderBackupResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ProviderRestoreResult> RestoreAsync(
        string backupPath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Clear connection pool
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Simple file copy for SQLite restore
            File.Copy(backupPath, destinationPath, overwrite: true);

            // Verify restored database
            await using var connection = new SqliteConnection($"Data Source={destinationPath}");
            await connection.OpenAsync(cancellationToken);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            var result = await cmd.ExecuteScalarAsync(cancellationToken);

            if (result?.ToString() != "ok")
            {
                return new ProviderRestoreResult
                {
                    Success = false,
                    ErrorMessage = $"Restored database failed integrity check: {result}"
                };
            }

            return new ProviderRestoreResult { Success = true };
        }
        catch (Exception ex)
        {
            return new ProviderRestoreResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

// Acode.Infrastructure/Export/ExportService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Acode.Application.Backup;
using Acode.Domain.Backup;

namespace Acode.Infrastructure.Export;

public sealed class ExportService : IExportService
{
    private readonly IExportWriterFactory _writerFactory;
    private readonly ITableReader _tableReader;
    private readonly IRedactionService _redactionService;
    private readonly ExportOptions _defaultOptions;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        IExportWriterFactory writerFactory,
        ITableReader tableReader,
        IRedactionService redactionService,
        ExportOptions defaultOptions,
        ILogger<ExportService> logger)
    {
        _writerFactory = writerFactory;
        _tableReader = tableReader;
        _redactionService = redactionService;
        _defaultOptions = defaultOptions;
        _logger = logger;
    }

    public async Task<ExportResult> ExportAsync(
        ExportOptions options,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var opts = options ?? _defaultOptions;

        try
        {
            // Get tables to export
            var allTables = _tableReader.GetTableNames();
            var tablesToExport = opts.Tables?.Any() == true
                ? allTables.Where(t => opts.Tables.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList()
                : allTables.ToList();

            if (opts.ExcludeTables?.Any() == true)
            {
                tablesToExport = tablesToExport
                    .Where(t => !opts.ExcludeTables.Contains(t, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }

            _logger.LogInformation("Exporting {Count} tables: {Tables}",
                tablesToExport.Count, string.Join(", ", tablesToExport));

            // Get appropriate writer
            var writer = _writerFactory.Create(opts.Format);

            // Collect all records
            var allRecords = new List<ExportRecord>();
            var redactionLog = new List<RedactionLogEntry>();

            foreach (var table in tablesToExport)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var records = await _tableReader.ReadTableAsync(table);

                if (opts.EnableRedaction)
                {
                    foreach (var record in records)
                    {
                        var redacted = _redactionService.Redact(record);
                        allRecords.Add(redacted);
                    }
                }
                else
                {
                    allRecords.AddRange(records);
                }
            }

            // Write export
            await writer.WriteAsync(allRecords, opts.OutputPath!);

            // Write redaction log if redaction was enabled
            if (opts.EnableRedaction)
            {
                var logPath = Path.ChangeExtension(opts.OutputPath!, ".redaction.log");
                await WriteRedactionLogAsync(redactionLog, logPath);
            }

            stopwatch.Stop();

            return new ExportResult
            {
                Success = true,
                OutputPath = opts.OutputPath,
                RecordCount = allRecords.Count,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed");
            return new ExportResult
            {
                Success = false,
                ErrorCode = "ACODE-EXP-001",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<DryRunResult> DryRunAsync(
        ExportOptions options,
        CancellationToken cancellationToken = default)
    {
        var result = new DryRunResult();

        var allTables = _tableReader.GetTableNames();
        var tablesToExport = options.Tables?.Any() == true
            ? allTables.Where(t => options.Tables.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList()
            : allTables.ToList();

        foreach (var table in tablesToExport)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var records = await _tableReader.ReadTableAsync(table);
            var preview = _redactionService.PreviewRedaction(records);

            result.AffectedRecords.AddRange(preview.AffectedRecords);
            result.TotalRecordsScanned += preview.TotalRecordsScanned;
            result.TotalFieldsToRedact += preview.TotalFieldsToRedact;
        }

        return result;
    }

    private async Task WriteRedactionLogAsync(
        IEnumerable<RedactionLogEntry> entries,
        string logPath)
    {
        var lines = new List<string>
        {
            $"Redaction Log - {DateTime.UtcNow:O}",
            ""
        };

        foreach (var entry in entries)
        {
            lines.Add($"Record: {entry.TableName}/{entry.RecordId}");
            lines.Add($"  Field: {entry.FieldName}");
            lines.Add($"  Type: {entry.RedactionType}");
            lines.Add("");
        }

        await File.WriteAllLinesAsync(logPath, lines);
    }
}

// Acode.Infrastructure/Export/Redaction/RedactionService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Acode.Application.Backup;
using Acode.Domain.Backup;

namespace Acode.Infrastructure.Export.Redaction;

public sealed class RedactionService : IRedactionService
{
    private readonly RedactionOptions _options;
    private readonly IRedactionAuditLogger _auditLogger;
    private readonly List<Regex> _columnPatterns;
    private readonly List<Regex> _contentPatterns;

    private static readonly Dictionary<string, string> PlaceholderMap = new()
    {
        ["api_key"] = "[REDACTED-API-KEY]",
        ["secret"] = "[REDACTED-SECRET]",
        ["token"] = "[REDACTED-TOKEN]",
        ["password"] = "[REDACTED-PASSWORD]",
        ["sk-"] = "[REDACTED-API-KEY]",
        ["ghp_"] = "[REDACTED-TOKEN]",
        ["gho_"] = "[REDACTED-TOKEN]",
        ["xoxb-"] = "[REDACTED-TOKEN]",
        ["AKIA"] = "[REDACTED-AWS-KEY]",
    };

    public RedactionService(RedactionOptions options, IRedactionAuditLogger auditLogger)
    {
        _options = options;
        _auditLogger = auditLogger;

        _columnPatterns = options.ColumnPatterns
            .Select(p => new Regex(WildcardToRegex(p), RegexOptions.Compiled | RegexOptions.IgnoreCase))
            .ToList();

        _contentPatterns = options.ContentPatterns
            .Select(p => new Regex(p, RegexOptions.Compiled))
            .ToList();
    }

    public ExportRecord Redact(ExportRecord record)
    {
        var redacted = record.Clone();
        var redactedFields = new List<RedactedField>();

        foreach (var field in record.Fields)
        {
            var value = field.Value?.ToString();
            if (string.IsNullOrEmpty(value)) continue;

            // Check column patterns
            foreach (var pattern in _columnPatterns)
            {
                if (pattern.IsMatch(field.Key))
                {
                    var placeholder = GetPlaceholder(field.Key);
                    redacted.SetField(field.Key, placeholder);
                    redactedFields.Add(new RedactedField
                    {
                        FieldName = field.Key,
                        RedactionType = RedactionType.ColumnPattern,
                        PatternMatched = pattern.ToString()
                    });
                    break;
                }
            }

            // Check content patterns (only if not already redacted)
            if (redacted.Fields[field.Key]?.ToString() == value)
            {
                foreach (var pattern in _contentPatterns)
                {
                    if (pattern.IsMatch(value))
                    {
                        var placeholder = GetContentPlaceholder(value);
                        var newValue = pattern.Replace(value, placeholder);
                        redacted.SetField(field.Key, newValue);
                        redactedFields.Add(new RedactedField
                        {
                            FieldName = field.Key,
                            RedactionType = RedactionType.ContentPattern,
                            PatternMatched = pattern.ToString()
                        });
                        break;
                    }
                }
            }
        }

        if (redactedFields.Any())
        {
            _auditLogger.LogRedaction(record.Id, redactedFields);
        }

        return redacted;
    }

    public DryRunResult PreviewRedaction(IEnumerable<ExportRecord> records)
    {
        var result = new DryRunResult();

        foreach (var record in records)
        {
            var affected = new DryRunRecord
            {
                RecordId = record.Id,
                TableName = record.TableName,
                FieldsToRedact = new List<string>()
            };

            foreach (var field in record.Fields)
            {
                var value = field.Value?.ToString();
                if (string.IsNullOrEmpty(value)) continue;

                var wouldRedact = _columnPatterns.Any(p => p.IsMatch(field.Key)) ||
                                  _contentPatterns.Any(p => p.IsMatch(value));

                if (wouldRedact)
                {
                    affected.FieldsToRedact.Add(field.Key);
                }
            }

            if (affected.FieldsToRedact.Any())
            {
                result.AffectedRecords.Add(affected);
            }

            result.TotalRecordsScanned++;
        }

        result.TotalFieldsToRedact = result.AffectedRecords.Sum(r => r.FieldsToRedact.Count);
        return result;
    }

    private static string WildcardToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
    }

    private static string GetPlaceholder(string fieldName)
    {
        foreach (var (key, placeholder) in PlaceholderMap)
        {
            if (fieldName.Contains(key, StringComparison.OrdinalIgnoreCase))
                return placeholder;
        }
        return "[REDACTED]";
    }

    private static string GetContentPlaceholder(string value)
    {
        foreach (var (key, placeholder) in PlaceholderMap)
        {
            if (value.Contains(key, StringComparison.OrdinalIgnoreCase))
                return placeholder;
        }
        return "[REDACTED]";
    }
}
```

### CLI Commands

```csharp
// Acode.Cli/Commands/Db/BackupCommand.cs
using System.CommandLine;
using System.CommandLine.Invocation;
using Acode.Application.Backup;

namespace Acode.Cli.Commands.Db;

public sealed class BackupCommand : Command
{
    public BackupCommand() : base("backup", "Create a database backup")
    {
        var nameOption = new Option<string?>(
            "--name",
            "Custom name for the backup file");

        AddOption(nameOption);

        this.SetHandler(async (context) =>
        {
            var name = context.ParseResult.GetValueForOption(nameOption);
            var backupService = context.GetService<IBackupService>();
            var databasePath = context.GetService<DatabaseConfiguration>().Path;
            var console = context.Console;

            console.WriteLine("Creating backup...");

            var progress = new Progress<BackupProgress>(p =>
            {
                console.Write($"\r  Progress: [{new string('█', p.PercentComplete / 5)}{new string(' ', 20 - p.PercentComplete / 5)}] {p.PercentComplete}%");
            });

            var result = await backupService.CreateBackupAsync(databasePath, name, progress);

            console.WriteLine();

            if (result.Success)
            {
                console.WriteLine($"\n✓ Backup created successfully");
                console.WriteLine($"  Path: {result.BackupPath}");
                console.WriteLine($"  Size: {result.FileSize / 1024.0 / 1024.0:F2} MB");
                console.WriteLine($"  Duration: {result.Duration.TotalSeconds:F1}s");
                console.WriteLine($"  Checksum: {result.Checksum}");
                context.ExitCode = 0;
            }
            else
            {
                console.WriteError($"\n✗ Backup failed ({result.ErrorCode})");
                console.WriteError($"  {result.ErrorMessage}");
                context.ExitCode = 1;
            }
        });
    }
}

// Acode.Cli/Commands/Db/ExportCommand.cs
public sealed class ExportCommand : Command
{
    public ExportCommand() : base("export", "Export database to file")
    {
        var formatOption = new Option<ExportFormat>(
            "--format",
            () => ExportFormat.Json,
            "Export format (json, csv, sqlite)");

        var tablesOption = new Option<string[]?>(
            "--tables",
            "Specific tables to export (comma-separated)");

        var redactOption = new Option<bool>(
            "--redact",
            "Apply redaction to sensitive data");

        var dryRunOption = new Option<bool>(
            "--dry-run",
            "Show what would be redacted without exporting");

        var outputOption = new Option<string?>(
            "--output",
            "Output file path");

        AddOption(formatOption);
        AddOption(tablesOption);
        AddOption(redactOption);
        AddOption(dryRunOption);
        AddOption(outputOption);

        this.SetHandler(async (context) =>
        {
            var format = context.ParseResult.GetValueForOption(formatOption);
            var tables = context.ParseResult.GetValueForOption(tablesOption);
            var redact = context.ParseResult.GetValueForOption(redactOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var output = context.ParseResult.GetValueForOption(outputOption);
            var exportService = context.GetService<IExportService>();
            var console = context.Console;

            var options = new ExportOptions
            {
                Format = format,
                Tables = tables?.ToList(),
                EnableRedaction = redact,
                OutputPath = output ?? $"export.{format.ToString().ToLower()}"
            };

            if (dryRun)
            {
                var result = await exportService.DryRunAsync(options);
                console.WriteLine("Redaction dry-run (no files will be created)\n");
                console.WriteLine("REDACTION PREVIEW:\n");

                foreach (var record in result.AffectedRecords.Take(20))
                {
                    console.WriteLine($"Table: {record.TableName}");
                    foreach (var field in record.FieldsToRedact)
                    {
                        console.WriteLine($"  Field: {field}");
                    }
                }

                console.WriteLine($"\nSUMMARY:");
                console.WriteLine($"  Records to scan: {result.TotalRecordsScanned}");
                console.WriteLine($"  Fields to redact: {result.TotalFieldsToRedact}");
                context.ExitCode = 0;
            }
            else
            {
                var result = await exportService.ExportAsync(options);

                if (result.Success)
                {
                    console.WriteLine($"✓ Export completed: {result.OutputPath}");
                    console.WriteLine($"  Records: {result.RecordCount}");
                    console.WriteLine($"  Duration: {result.Duration.TotalSeconds:F1}s");
                    context.ExitCode = 0;
                }
                else
                {
                    console.WriteError($"✗ Export failed ({result.ErrorCode})");
                    console.WriteError($"  {result.ErrorMessage}");
                    context.ExitCode = 1;
                }
            }
        });
    }
}
```

### Dependency Injection

```csharp
// Acode.Infrastructure/Backup/DependencyInjection/BackupServiceExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Acode.Application.Backup;
using Acode.Infrastructure.Backup.Providers;
using Acode.Infrastructure.Export;
using Acode.Infrastructure.Export.Redaction;

namespace Acode.Infrastructure.Backup.DependencyInjection;

public static class BackupServiceExtensions
{
    public static IServiceCollection AddBackupServices(
        this IServiceCollection services,
        BackupOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IRestoreService, RestoreService>();
        services.AddSingleton<IBackupVerifier, BackupVerifier>();
        services.AddSingleton<IManifestBuilder, ManifestBuilder>();
        services.AddSingleton<IBackupStorage, SecureBackupStorage>();
        services.AddSingleton<BackupRotationService>();

        // Register providers
        services.AddSingleton<IBackupProvider, SqliteBackupProvider>();

        return services;
    }

    public static IServiceCollection AddExportServices(
        this IServiceCollection services,
        ExportOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IExportWriterFactory, ExportWriterFactory>();
        services.AddSingleton<ITableReader, SqliteTableReader>();

        return services;
    }

    public static IServiceCollection AddRedactionServices(
        this IServiceCollection services,
        RedactionOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IRedactionService, RedactionService>();
        services.AddSingleton<IRedactionAuditLogger, RedactionAuditLogger>();
        services.AddSingleton<RedactionValidator>();

        return services;
    }
}
```

### Error Codes

| Code | Meaning | Recovery Action |
|------|---------|-----------------|
| ACODE-BAK-001 | Backup creation failed | Check disk space, verify source database exists and is readable |
| ACODE-BAK-002 | Restore failed | Verify backup file exists and is not corrupted, check database permissions |
| ACODE-BAK-003 | Verification failed | Check manifest exists and is valid JSON |
| ACODE-BAK-004 | Checksum mismatch | Backup may be corrupted or tampered. Use different backup. |
| ACODE-EXP-001 | Export failed | Check output path is writable, verify database is accessible |
| ACODE-EXP-002 | Redaction failed | Check redaction patterns are valid regex, verify record fields |

### Implementation Checklist

1. [x] Define domain models (BackupResult, RestoreResult, ExportRecord, etc.)
2. [x] Create application interfaces (IBackupService, IRestoreService, IExportService)
3. [x] Implement SQLite backup provider with backup API
4. [ ] Implement PostgreSQL backup provider with pg_dump
5. [x] Create manifest builder with JSON serialization
6. [x] Implement backup rotation service
7. [x] Create secure backup storage with file permissions
8. [x] Implement restore service with verification
9. [x] Create export service with format writers
10. [x] Implement redaction service with patterns
11. [x] Add CLI commands (backup, list, verify, restore, export)
12. [ ] Implement pre-migration backup hook
13. [ ] Write unit tests for all services
14. [ ] Write integration tests
15. [ ] Write E2E tests

### Rollout Plan

| Phase | Components | Duration | Dependencies |
|-------|------------|----------|--------------|
| 1 | Domain models, Interfaces | 2 days | None |
| 2 | SQLite backup provider | 2 days | Phase 1 |
| 3 | Manifest, Rotation, Storage | 2 days | Phase 2 |
| 4 | Restore service | 2 days | Phase 3 |
| 5 | Export writers (JSON/CSV/SQLite) | 3 days | Phase 1 |
| 6 | Redaction pipeline | 2 days | Phase 5 |
| 7 | CLI commands | 2 days | Phase 4, 6 |
| 8 | Pre-migration hook | 1 day | Phase 4 |
| 9 | Testing & Documentation | 3 days | All phases |

---

**End of Task 050.e Specification**