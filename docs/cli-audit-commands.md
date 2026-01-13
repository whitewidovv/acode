# CLI Audit Commands

This document describes all audit-related CLI commands available in Acode.

## Overview

The `acode audit` command provides comprehensive audit log management capabilities including viewing, searching, exporting, and maintaining audit logs. All audit operations respect the current operating mode and security policies.

## Command Structure

```
acode audit <subcommand> [options]
```

## Exit Codes

All audit commands use standard Unix exit codes:

| Code | Name | Description |
|------|------|-------------|
| 0 | Success | Command completed successfully |
| 1 | RuntimeError | Runtime error occurred during execution |
| 2 | InvalidArguments | Invalid command arguments or options |
| 3 | ConfigurationError | Configuration error or missing configuration |

## Subcommands

### 1. list - List Audit Sessions

Lists all audit sessions, optionally filtered by date range.

**Usage:**
```bash
acode audit list [--from <date>] [--to <date>]
```

**Options:**
- `--from <date>` - Show sessions starting from this date (ISO 8601 format)
- `--to <date>` - Show sessions up to this date (ISO 8601 format)

**Examples:**

List all sessions:
```bash
acode audit list
```

List sessions from January 1, 2026:
```bash
acode audit list --from 2026-01-01
```

List sessions in a specific date range:
```bash
acode audit list --from 2026-01-01 --to 2026-01-31
```

**Output Format:**
```
Found 3 session(s):
  sess_abc123def456 | LocalOnly | 2026-01-11 10:00:00
  sess_xyz789uvw012 | LocalOnly | 2026-01-11 14:30:00
  sess_mno345pqr678 | Burst | 2026-01-12 09:15:00
```

**Exit Codes:**
- `0` - Success, sessions listed
- `1` - Runtime error (e.g., log directory not accessible)
- `3` - Configuration error (handler not configured)

---

### 2. show - Show Session Events

Displays all audit events for a specific session.

**Usage:**
```bash
acode audit show <session-id>
```

**Arguments:**
- `<session-id>` - Session identifier (e.g., `sess_abc123def456`)

**Examples:**

Show events for a session:
```bash
acode audit show sess_abc123def456
```

**Output Format:**
```
Session sess_abc123def456 - 15 event(s):
  [10:00:00] SessionStart | Info | Acode.Cli.Program
  [10:00:01] ConfigLoad | Info | Acode.Application.Config.ConfigLoader
  [10:00:05] CommandStart | Info | Acode.Cli.Commands.BuildCommand
  [10:00:35] CommandEnd | Info | Acode.Cli.Commands.BuildCommand
  [10:01:00] SessionEnd | Info | Acode.Cli.Program
```

**Exit Codes:**
- `0` - Success, events displayed
- `1` - Runtime error (e.g., session log file not found)
- `2` - Invalid arguments (missing session ID)
- `3` - Configuration error (handler not configured)

---

### 3. search - Search Audit Events

Searches audit events across all sessions using multiple filter criteria.

**Usage:**
```bash
acode audit search [--from <date>] [--to <date>] [--type <event-type>] [--severity <severity>] [--text <search-text>]
```

**Options:**
- `--from <date>` - Events starting from this date (ISO 8601 format)
- `--to <date>` - Events up to this date (ISO 8601 format)
- `--type <event-type>` - Filter by event type (e.g., `CommandStart`, `FileWrite`)
- `--severity <severity>` - Filter by minimum severity level (`Debug`, `Info`, `Warning`, `Error`, `Critical`)
- `--text <search-text>` - Search for text in event data

**Event Types:**
`SessionStart`, `SessionEnd`, `ConfigLoad`, `ConfigError`, `ModeSelect`, `CommandStart`, `CommandEnd`, `CommandError`, `FileRead`, `FileWrite`, `FileDelete`, `DirCreate`, `DirDelete`, `ProtectedPathBlocked`, `SecurityViolation`, `TaskStart`, `TaskEnd`, `TaskError`, `ApprovalRequest`, `ApprovalResponse`, `CodeGenerated`, `TestExecution`, `BuildExecution`, `ErrorRecovery`, `Shutdown`

**Severity Levels:**
`Debug`, `Info`, `Warning`, `Error`, `Critical`

**Examples:**

Search for all CommandStart events:
```bash
acode audit search --type CommandStart
```

Search for Warning and above events:
```bash
acode audit search --severity Warning
```

Search for security violations:
```bash
acode audit search --type SecurityViolation
```

Search for events containing specific text:
```bash
acode audit search --text "build failed"
```

Combined search (CommandStart events from specific date):
```bash
acode audit search --type CommandStart --from 2026-01-11
```

**Output Format:**
```
Found 5 matching event(s):
  [2026-01-11 10:00:05] CommandStart | Info | Session: sess_abc123def456
  [2026-01-11 10:05:10] CommandStart | Info | Session: sess_abc123def456
  [2026-01-11 14:30:00] CommandStart | Info | Session: sess_xyz789uvw012
  [2026-01-12 09:15:30] CommandStart | Info | Session: sess_mno345pqr678
  [2026-01-12 11:20:45] CommandStart | Info | Session: sess_mno345pqr678
```

**Exit Codes:**
- `0` - Success, matching events displayed (even if 0 results)
- `1` - Runtime error (e.g., log directory not accessible)
- `3` - Configuration error (handler not configured)

---

### 4. verify - Verify Audit Log Integrity

**⚠️ IMPORTANT:** `acode audit verify` is currently a **no-op placeholder**. It does **not** perform any checksum or integrity validation and must **not** be relied on as an integrity control in production or automation. The current implementation only prints an error message and exits with failure.

This subcommand is reserved for a future implementation that may perform log integrity verification. Until that implementation is available and documented, treat this command as informational only and do not use it to make security decisions.

**Usage:**
```bash
acode audit verify
```

**Current Behavior:**
```bash
acode audit verify
```

**Output:**
```
ERROR: Audit log verification is not yet implemented.
This command performs NO integrity validation and MUST NOT be used as a security control.
Logs are NOT verified for tampering or corruption.
```

**Exit Codes (current behavior):**
- `3` - RuntimeError (placeholder executed; **does not imply logs are valid or untampered**)

**Planned behavior (subject to change and not yet implemented):**
- Verify checksum files exist for all JSONL log files
- Validate each event line against its checksum
- Report any tampering or corruption detected
- Exit with code 0 if all integrity checks pass
- Exit with code 1 if integrity violations found
- Exit with code 3 for configuration errors

---

### 5. export - Export Audit Logs

Exports audit logs to various formats for analysis or archival.

**Usage:**
```bash
acode audit export --output <path> [--format <format>] [--log-dir <directory>]
```

**Options:**
- `--output <path>` - **Required.** Output file path for exported logs
- `--format <format>` - Export format: `json`, `csv`, or `text` (default: `json`)
- `--log-dir <directory>` - Log directory to export from (default: `.acode/logs`)

**Formats:**
- `json` - JSON array of all events
- `csv` - Comma-separated values (spreadsheet-compatible)
- `text` - Human-readable text format

**Examples:**

Export to JSON:
```bash
acode audit export --output audit-export.json
```

Export to CSV:
```bash
acode audit export --output audit-export.csv --format csv
```

Export from custom log directory:
```bash
acode audit export --output archive.json --log-dir /mnt/backup/logs
```

**Output:**
```
Exported audit logs to audit-export.json (json format)
```

**Exit Codes:**
- `0` - Success, logs exported
- `1` - Runtime error (e.g., cannot write to output file)
- `2` - Invalid arguments (missing --output)
- `3` - Configuration error (exporter not configured)

---

### 6. stats - Show Audit Statistics

Displays statistical summary of audit logs.

**Usage:**
```bash
acode audit stats
```

**Examples:**

Show audit statistics:
```bash
acode audit stats
```

**Output Format:**
```
Audit Statistics:
  Total Sessions: 3
  Total Events: 847
  Events by Type:
    CommandStart: 125
    CommandEnd: 123
    FileWrite: 89
    FileRead: 201
    ConfigLoad: 3
    SessionStart: 3
    SessionEnd: 3
  Events by Severity:
    Info: 795
    Warning: 42
    Error: 8
    Critical: 2
  Oldest Event: 2026-01-01 10:00:00
  Newest Event: 2026-01-12 18:30:00
```

**Exit Codes:**
- `0` - Success, statistics displayed
- `1` - Runtime error (e.g., log directory not accessible)
- `3` - Configuration error (handler not configured)

---

### 7. tail - Follow Audit Log in Real-Time

Follows the active audit log, displaying new events as they occur (similar to `tail -f`).

**Usage:**
```bash
acode audit tail
```

**Examples:**

Follow audit log in real-time:
```bash
acode audit tail
```

**Output Format:**
```
Real-time audit log tail not yet implemented.
```

**Note:** This subcommand is currently a placeholder and will be implemented in a future release. When complete, it will:
- Monitor the current session's log file
- Display new events as they are written
- Update continuously until interrupted (Ctrl+C)
- Support color-coding by severity level

**Exit Codes:**
- `0` - Success (currently always returns success)
- `1` - Runtime error (future)
- `3` - Configuration error

---

### 8. cleanup - Clean Up Old Audit Logs

Deletes audit logs older than the specified retention period.

**Usage:**
```bash
acode audit cleanup [--retention-days <days>] [--log-dir <directory>]
```

**Options:**
- `--retention-days <days>` - Number of days to retain logs (default: `90`)
- `--log-dir <directory>` - Log directory to clean up (default: `.acode/logs`)

**Examples:**

Clean up logs older than 90 days (default):
```bash
acode audit cleanup
```

Clean up logs older than 30 days:
```bash
acode audit cleanup --retention-days 30
```

Clean up logs in custom directory:
```bash
acode audit cleanup --retention-days 60 --log-dir /mnt/archive/logs
```

**Output Format:**
```
Cleanup complete:
  Files deleted: 15
  Bytes freed: 2,457,600
```

**Exit Codes:**
- `0` - Success, cleanup completed
- `1` - Runtime error (e.g., permission denied)
- `3` - Configuration error (handler not configured)

---

## Common Usage Patterns

### Daily Audit Review

Review today's audit events:
```bash
acode audit list --from $(date +%Y-%m-%d)
```

### Security Monitoring

Check for security violations in the last 7 days:
```bash
acode audit search --type SecurityViolation --from $(date -d '7 days ago' +%Y-%m-%d)
```

### Error Investigation

Find all error events:
```bash
acode audit search --severity Error
```

### Session Forensics

Investigate a specific session:
```bash
# List sessions to find session ID
acode audit list

# Show all events for that session
acode audit show sess_abc123def456

# Export session for detailed analysis
acode audit export --output investigation.json
```

### Regular Maintenance

Monthly cleanup and export:
```bash
# Export logs before cleanup
acode audit export --output monthly-archive-$(date +%Y-%m).json

# Clean up logs older than 90 days
acode audit cleanup --retention-days 90
```

### Compliance Reporting

Generate compliance report:
```bash
# Export all audit logs
acode audit export --output compliance-report.csv --format csv

# Show statistics
acode audit stats
```

## Integration with Other Commands

Audit commands can be combined with standard Unix tools for advanced analysis:

### Count Events by Type
```bash
acode audit search --type CommandStart | wc -l
```

### Extract Specific Session
```bash
acode audit show sess_abc123def456 > session-report.txt
```

### Filter Export with jq
```bash
acode audit export --output all-events.json
cat all-events.json | jq '.[] | select(.severity == "Critical")'
```

### Monitor for Errors
```bash
# Poll for new errors every 10 seconds
watch -n 10 'acode audit search --severity Error --from $(date -d "1 hour ago" +%Y-%m-%dT%H:%M:%S)'
```

## Error Handling

### Common Errors

**Handler Not Configured**
```
Error: List handler not configured.
```
**Resolution:** Ensure the audit system is properly initialized in the application configuration.

**Missing Session ID**
```
Error: Missing session ID. Usage: acode audit show <session-id>
```
**Resolution:** Provide a valid session ID from `acode audit list`.

**Missing Output Path**
```
Error: Missing --output path.
```
**Resolution:** Specify `--output` parameter with a file path for export commands.

**Unknown Subcommand**
```
Error: Unknown subcommand 'invalid'. Use 'acode audit help' for usage.
```
**Resolution:** Use one of the 8 valid subcommands or run `acode audit help`.

### Troubleshooting

**Log directory not accessible:**
- Check that `.acode/logs` directory exists and has read permissions
- Verify disk space is available
- Check file system is not read-only

**Session not found:**
- Verify session ID format (e.g., `sess_abc123def456`)
- Use `acode audit list` to see available sessions
- Check if logs have been cleaned up or archived

**Export fails:**
- Ensure output directory exists and is writable
- Check available disk space
- Verify log files are not corrupted

## Configuration

Audit logging is configured in `.agent/config.yml`:

```yaml
audit:
  enabled: true
  log_level: info
  log_directory: .acode/logs
  retention_days: 90
  rotation_size_mb: 10
  rotation_interval: daily
  export_formats:
    - json
    - csv
    - text
```

**Configuration Options:**
- `enabled` - Enable/disable audit logging (default: `true`)
- `log_level` - Minimum severity to log: `debug`, `info`, `warning`, `error`, `critical` (default: `info`)
- `log_directory` - Directory for log files (default: `.acode/logs`)
- `retention_days` - Days to retain logs (default: `90`)
- `rotation_size_mb` - Max log file size before rotation (default: `10`)
- `rotation_interval` - Rotation interval: `hourly`, `daily`, `weekly` (default: `daily`)
- `export_formats` - Supported export formats (default: `["json", "csv", "text"]`)

## Security Considerations

### Access Control
- Audit logs contain sensitive operational data
- Restrict read access to `.acode/logs` directory
- Use appropriate file permissions (e.g., `chmod 600`)

### Integrity Protection
- Audit logs include SHA-256 checksum files (`.sha256` sidecar files)
- ⚠️ **WARNING:** `acode audit verify` is **not yet implemented** and cannot detect tampering
- Manual verification: Compare `.sha256` file contents with actual file hash (`sha256sum audit-*.jsonl`)
- Store checksums separately for enhanced security when verification is implemented

### Data Retention
- Balance audit visibility with storage costs
- Set appropriate `retention_days` based on compliance requirements
- Archive old logs before cleanup if needed

### Export Security
- Exported files may contain sensitive data
- Encrypt exports for transmission or archival
- Sanitize exports before sharing externally

## Performance Considerations

### Large Log Files
- Use date filters (`--from`, `--to`) to narrow searches
- Export and analyze offline for very large datasets
- Consider rotating logs more frequently

### Search Performance
- Specific filters (--type, --severity) are faster than text search
- Session-specific queries (`show`) are fastest
- Index exports for repeated queries

### Cleanup Impact
- Cleanup scans all log files - may be slow for large volumes
- Run cleanup during low-activity periods
- Monitor disk I/O during cleanup operations

## Related Documentation

- [Audit Event Schema](audit-event-schema.md) - Complete event schema reference
- [Audit Configuration](../data/config-schema.json) - Configuration schema
- [AUDIT-GUIDELINES.md](AUDIT-GUIDELINES.md) - Audit requirements and compliance

## Getting Help

For detailed usage of a specific subcommand:
```bash
acode audit help
```

For general Acode help:
```bash
acode --help
```
