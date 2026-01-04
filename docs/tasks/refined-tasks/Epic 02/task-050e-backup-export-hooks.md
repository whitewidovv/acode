# Task 050.e: Backup/Export Hooks for Workspace DB (Safe, Redacted)

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – Persistence + Reliability Core  
**Dependencies:** Task 050 (Database Foundation), Task 049.e (Privacy/Redaction)  

---

## Description

Task 050.e implements backup and export functionality for the workspace database. Backups enable disaster recovery. Exports enable data portability. Both support redaction for safety.

Backups are point-in-time copies of the database. They capture the full state. They are for disaster recovery. They are for rollback after failed migrations. They are not human-readable.

Exports are data extractions for sharing. They can be JSON, CSV, or SQLite. They are human-readable. They are for portability. They may include redaction for privacy.

Redaction removes sensitive data before export. API keys are removed. Personal identifiers are masked. File paths are normalized. Secrets are replaced with placeholders.

Backup hooks integrate with migrations. Before each migration, a backup is optionally created. If migration fails, the backup enables recovery. This is configurable.

Export hooks integrate with retention policies. When data is purged, an export can be created first. This preserves data that would otherwise be lost. This is configurable.

The backup format is raw database copy for SQLite. This is the fastest method. It uses SQLite's backup API. It is atomic. It handles WAL mode correctly.

For PostgreSQL, backups use pg_dump format. This is the standard PostgreSQL backup format. It can be restored with pg_restore. It supports compression.

Export formats include JSON for readability, CSV for spreadsheet compatibility, and SQLite for self-contained archives. Each format has trade-offs.

Redaction is rule-based. Patterns define what to redact. Patterns can match column names, content patterns, or both. Redaction is not reversible.

Backup locations are configurable. Local directory. Network share. Cloud storage is out of scope for MVP. Rotation policies delete old backups.

Backup verification ensures integrity. After backup, the file is validated. Checksums are computed. Restoration is tested in isolated mode.

Scheduled backups are optional. Daily backups at configurable time. Weekly backups with longer retention. Manual backups on demand.

The CLI provides backup commands. Create backup. List backups. Restore from backup. Verify backup. Delete old backups.

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

- NFR-001: Backup < 10s for 100MB
- NFR-002: Export < 30s for 100MB
- NFR-003: Verification < 5s

### Reliability

- NFR-004: Atomic backup
- NFR-005: No partial backups
- NFR-006: Checksum verification

### Safety

- NFR-007: Redaction is complete
- NFR-008: No partial redaction
- NFR-009: Verification before share

### Usability

- NFR-010: Clear progress output
- NFR-011: Helpful error messages
- NFR-012: Dry-run for exports

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

### Backup

- [ ] AC-001: SQLite backup works
- [ ] AC-002: PostgreSQL backup works
- [ ] AC-003: Manifest created
- [ ] AC-004: Checksum computed

### Rotation

- [ ] AC-005: Max backups enforced
- [ ] AC-006: Oldest deleted first
- [ ] AC-007: Rotation after success

### Restore

- [ ] AC-008: SQLite restore works
- [ ] AC-009: PostgreSQL restore works
- [ ] AC-010: Pre-restore backup
- [ ] AC-011: Verification works

### Export

- [ ] AC-012: JSON export works
- [ ] AC-013: CSV export works
- [ ] AC-014: SQLite export works
- [ ] AC-015: Table selection works

### Redaction

- [ ] AC-016: Pattern redaction works
- [ ] AC-017: Column redaction works
- [ ] AC-018: Complete redaction
- [ ] AC-019: Logged redaction

### CLI

- [ ] AC-020: Backup command works
- [ ] AC-021: List command works
- [ ] AC-022: Restore command works
- [ ] AC-023: Export command works

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

### Issue: Backup file corrupted

**Symptoms:** Restore fails with "malformed database" or integrity errors

**Causes:**
- Backup taken during active write (non-atomic method used)
- Disk error during backup write
- File transfer corruption (binary mode not used)

**Solutions:**
1. Verify source database integrity before backup
2. Use SQLite backup API or pg_dump, not file copy
3. Check file checksums after transfer

### Issue: Restore changes not visible

**Symptoms:** Restored data not appearing in application

**Causes:**
- Restored to wrong database file/instance
- Connection caching showing stale data
- Application not restarted after restore

**Solutions:**
1. Verify restore target path matches config
2. Restart application to clear any cached connections
3. Run `agent db health` to confirm database state

### Issue: Backup takes too long

**Symptoms:** Large database backup times out or impacts operations

**Causes:**
- Full backup of very large database
- Disk I/O contention during backup
- Network transfer bottleneck for remote backup

**Solutions:**
1. Schedule backups during off-peak hours
2. Use faster storage for backup destination
3. Consider incremental backup strategy for large databases

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Backup/
├── BackupServiceTests.cs
│   ├── Should_Create_Backup()
│   ├── Should_Compute_Checksum()
│   └── Should_Create_Manifest()
│
├── BackupRotationTests.cs
│   ├── Should_Keep_Max_Backups()
│   └── Should_Delete_Oldest()
│
├── RedactionServiceTests.cs
│   ├── Should_Redact_Columns()
│   ├── Should_Redact_Patterns()
│   └── Should_Log_Redactions()
│
└── ExportServiceTests.cs
    ├── Should_Export_Json()
    ├── Should_Export_Csv()
    └── Should_Select_Tables()
```

### Integration Tests

```
Tests/Integration/Backup/
├── BackupIntegrationTests.cs
│   ├── Should_Backup_Real_Database()
│   └── Should_Restore_Real_Database()
│
└── ExportIntegrationTests.cs
    └── Should_Export_With_Redaction()
```

### E2E Tests

```
Tests/E2E/Backup/
├── BackupE2ETests.cs
│   ├── Should_Create_And_Restore()
│   └── Should_Rotate_Backups()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| 100MB backup | 5s | 10s |
| 100MB export | 15s | 30s |
| Verification | 2s | 5s |

---

## User Verification Steps

### Scenario 1: Create Backup

1. Run `acode db backup`
2. Verify: Backup file created
3. Verify: Manifest exists

### Scenario 2: List Backups

1. Create multiple backups
2. Run `acode db backup list`
3. Verify: All listed

### Scenario 3: Restore

1. Create backup
2. Make changes
3. Restore backup
4. Verify: Changes reverted

### Scenario 4: Export JSON

1. Run `acode db export --format json`
2. Verify: JSON file valid
3. Verify: Data correct

### Scenario 5: Redacted Export

1. Run `acode db export --redact`
2. Verify: Sensitive data removed
3. Verify: Placeholders present

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/
├── Backup/
│   ├── IBackupService.cs
│   ├── IExportService.cs
│   └── IRedactionService.cs
│
src/AgenticCoder.Infrastructure/
├── Backup/
│   ├── BackupService.cs
│   ├── BackupManifest.cs
│   ├── BackupRotation.cs
│   ├── SQLite/
│   │   └── SqliteBackupProvider.cs
│   └── PostgreSQL/
│       └── PostgresBackupProvider.cs
│
├── Export/
│   ├── ExportService.cs
│   ├── Exporters/
│   │   ├── JsonExporter.cs
│   │   ├── CsvExporter.cs
│   │   └── SqliteExporter.cs
│   └── Redaction/
│       ├── RedactionService.cs
│       └── RedactionPattern.cs
│
src/AgenticCoder.CLI/
└── Commands/
    ├── DbBackupCommand.cs
    └── DbExportCommand.cs
```

### IBackupService Interface

```csharp
namespace AgenticCoder.Application.Backup;

public interface IBackupService
{
    Task<BackupResult> CreateAsync(BackupOptions options, CancellationToken ct);
    Task<IReadOnlyList<BackupInfo>> ListAsync(CancellationToken ct);
    Task<RestoreResult> RestoreAsync(string backupPath, CancellationToken ct);
    Task<VerifyResult> VerifyAsync(string backupPath, CancellationToken ct);
    Task DeleteAsync(string backupPath, CancellationToken ct);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-BAK-001 | Backup failed |
| ACODE-BAK-002 | Restore failed |
| ACODE-BAK-003 | Verification failed |
| ACODE-BAK-004 | Checksum mismatch |
| ACODE-EXP-001 | Export failed |
| ACODE-EXP-002 | Redaction failed |

### Implementation Checklist

1. [ ] Create backup service interface
2. [ ] Implement SQLite backup
3. [ ] Implement PostgreSQL backup
4. [ ] Create manifest handling
5. [ ] Implement rotation
6. [ ] Create restore functionality
7. [ ] Create export service
8. [ ] Implement redaction
9. [ ] Add CLI commands
10. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Backup service
2. **Phase 2:** Restore service
3. **Phase 3:** Rotation
4. **Phase 4:** Export service
5. **Phase 5:** Redaction
6. **Phase 6:** CLI commands

---

**End of Task 050.e Specification**