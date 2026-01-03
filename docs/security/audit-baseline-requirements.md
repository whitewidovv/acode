# Audit Baseline Requirements

**Version:** 1.0.0
**Last Updated:** 2026-01-03
**Status:** Approved
**Owner:** Security Team

---

## Overview

This document defines the baseline audit logging requirements for Acode. These requirements establish what MUST be logged, how audit events MUST be structured, how audit logs MUST be stored and protected, and how audit data MUST be queryable and exportable.

Audit logging is **mandatory and non-negotiable**. Even in LocalOnly mode with maximum privacy, local audit logs MUST be maintained. The audit baseline provides complete transparency into agent operations, enabling accountability, security incident investigation, debugging, and compliance.

---

## Mandatory Audit Events

The following events MUST be logged to the audit trail. Each event includes the required fields specified in the Event Schema section.

### Session Management Events

| Event Type | When Logged | Required Data |
|------------|-------------|---------------|
| `SessionStart` | Acode process starts | SessionId, Timestamp, OperatingMode, Version, User, WorkingDirectory |
| `SessionEnd` | Acode process terminates | SessionId, Timestamp, Duration, ExitCode, Reason |
| `ModeChange` | Operating mode changes | OldMode, NewMode, Reason, UserConsent, Timestamp |

### Configuration Events

| Event Type | When Logged | Required Data |
|------------|-------------|---------------|
| `ConfigLoad` | Config file loaded | FilePath (redacted), SchemaVersion, ValidationResult, Timestamp |
| `ConfigChange` | Config modified | FilePath, ChangedKeys, OldValues (redacted), NewValues (redacted), User |
| `ConfigError` | Config validation fails | FilePath, Errors, Timestamp |

### File Operation Events

| Event Type | When Logged | Required Data |
|------------|-------------|---------------|
| `FileRead` | File read attempted | Path (normalized), Size, Success, DeniedReason, Timestamp |
| `FileWrite` | File write attempted | Path (normalized), Size, Success, DeniedReason, Timestamp |
| `FileDelete` | File delete attempted | Path (normalized), Success, DeniedReason, Timestamp |
| `PathBlocked` | Protected path access denied | Path (normalized), Operation, MatchedPattern, RiskId, Timestamp |

### Command Execution Events

| Event Type | When Logged | Required Data |
|------------|-------------|---------------|
| `CommandStart` | Command execution begins | Executable, Arguments (redacted), WorkingDir, Environment (redacted), Timeout |
| `CommandComplete` | Command execution ends | ExitCode, Duration, StdoutSize, StderrSize, Success, TimedOut |
| `CommandTimeout` | Command exceeds timeout | Executable, Timeout, Duration, KilledPids |
| `CommandError` | Command fails to start | Executable, Error, Reason |

### LLM Interaction Events (Burst Mode)

| Event Type | When Logged | Required Data |
|------------|-------------|---------------|
| `PromptSent` | Prompt sent to external LLM | Provider, Model, PromptHash, TokenCount, ContextSizeBytes |
| `ResponseReceived` | Response from external LLM | Provider, ResponseHash, TokenCount, Duration, Success |
| `LlmError` | LLM API call fails | Provider, Error, Reason, Timestamp |

### Security Events

| Event Type | When Logged | Required Data |
|------------|-------------|---------------|
| `SecretRedacted` | Secret detected and redacted | SecretType, Location, Pattern, Timestamp |
| `SecurityBlock` | Security control blocks operation | Control, Operation, Reason, RiskId, Timestamp |
| `AuditLogRotation` | Audit log rotated | OldFile, NewFile, Size, Timestamp |
| `AuditError` | Audit logging fails | Error, Reason, Timestamp |

---

## Event Schema

All audit events MUST conform to this schema. Events are stored in JSON Lines format (one JSON object per line).

### Core Fields (Required for ALL Events)

```json
{
  "schema_version": "1.0.0",
  "event_id": "evt_01234567890abcdef",
  "timestamp": "2026-01-03T10:30:00.000Z",
  "session_id": "sess_abcdef123456",
  "correlation_id": "corr_xyz789",
  "event_type": "SessionStart|ConfigLoad|FileRead|CommandStart|...",
  "severity": "Info|Warning|Error|Critical",
  "source": "Acode.Core|Acode.Security|Acode.Execution|...",
  "operating_mode": "LocalOnly|Burst|Airgapped",
  "data": {}
}
```

### Field Definitions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `schema_version` | string | Yes | Schema version (semver): "1.0.0" |
| `event_id` | string | Yes | Unique event identifier (evt_*) |
| `timestamp` | ISO 8601 | Yes | Event timestamp in UTC |
| `session_id` | string | Yes | Session identifier (links related events) |
| `correlation_id` | string | Yes | Correlation identifier for distributed tracing |
| `event_type` | enum | Yes | Type of event (see Mandatory Audit Events) |
| `severity` | enum | Yes | Info, Warning, Error, Critical |
| `source` | string | Yes | Component that generated the event |
| `operating_mode` | enum | Yes | Operating mode when event occurred |
| `data` | object | Yes | Event-specific data (structure varies by event_type) |

### Event-Specific Data Examples

**SessionStart:**
```json
{
  "version": "0.1.0-alpha",
  "user": "neilo",
  "working_directory": "/mnt/c/Users/neilo/source/project",
  "operating_mode": "LocalOnly",
  "process_id": 12345,
  "parent_process_id": 11111
}
```

**PathBlocked:**
```json
{
  "path": "~/.ssh/id_rsa",
  "normalized_path": "/home/neilo/.ssh/id_rsa",
  "operation": "Read",
  "matched_pattern": "~/.ssh/id_*",
  "risk_id": "RISK-I-003",
  "category": "SshKeys",
  "reason": "SSH private key files"
}
```

**CommandStart:**
```json
{
  "executable": "dotnet",
  "arguments": ["build", "--configuration", "Release"],
  "working_directory": "/mnt/c/Users/neilo/source/project",
  "timeout_seconds": 300,
  "environment": {
    "PATH": "[REDACTED]",
    "API_KEY": "[REDACTED]"
  }
}
```

**SecretRedacted:**
```json
{
  "secret_type": "api_key",
  "location": "environment_variable",
  "pattern": "api[_-]?key",
  "redaction_count": 1,
  "context": "CommandStart environment variables"
}
```

---

## Storage Requirements

### File Format

- **Format:** JSON Lines (.jsonl)
- **Encoding:** UTF-8
- **Line Separator:** LF (\\n)
- **One Event Per Line:** Each line is a complete, valid JSON object
- **No Pretty Printing:** Compact JSON (no whitespace except in strings)

### File Location

- **Default Path:** `.agent/audit.jsonl` (relative to repository root)
- **Configurable:** Via `.agent/config.yml` → `audit.log_path`
- **Permissions:** 600 (owner read/write only)
- **Directory Permissions:** 700 (owner only)

### File Rotation

| Setting | Default | Configurable | Description |
|---------|---------|--------------|-------------|
| Max Size | 100 MB | Yes | Rotate when file exceeds size |
| Max Age | 7 days | Yes | Rotate daily at midnight UTC |
| Retention | 90 days | Yes | Delete rotated logs older than retention period |
| Compression | gzip | Yes | Compress rotated logs (.jsonl.gz) |
| Naming | `audit-YYYYMMDD-HHMMSS.jsonl.gz` | No | Rotated file naming scheme |

### Tamper Protection

- **Append-Only:** Log files opened in append mode only
- **Integrity Checksums:** SHA-256 hash computed per rotation
- **Checksum File:** `.agent/audit.jsonl.sha256` contains hash of current log
- **Verification:** `acode security verify-audit` command validates integrity
- **Deletion Detection:** Missing log files logged to new file immediately

---

## Retention and Pruning

### Retention Policy

| Log Type | Minimum Retention | Default Retention | Configurable |
|----------|-------------------|-------------------|--------------|
| Active Session | Until session ends | N/A | No |
| Completed Session | 30 days | 90 days | Yes (7-365 days) |
| Security Events | 90 days | 365 days | Yes (30-730 days) |
| Error Events | 60 days | 180 days | Yes (30-365 days) |

### Pruning Behavior

- **Automatic Pruning:** Runs daily at 02:00 local time
- **Manual Pruning:** `acode security prune-audit --older-than 90d`
- **Prune Log Event:** Pruning itself is logged as `AuditPruned` event
- **Safety Check:** Never prune logs less than 7 days old
- **Fail-Closed:** If pruning fails, log error and halt (do not delete partial data)

---

## Query and Export

### Query Capabilities

Users MUST be able to query audit logs by:

| Filter | Example | Implementation |
|--------|---------|----------------|
| Time Range | `--after 2026-01-01 --before 2026-01-31` | Filter by timestamp field |
| Event Type | `--event-type CommandStart` | Filter by event_type field |
| Severity | `--severity Error,Critical` | Filter by severity field |
| Session ID | `--session sess_abc123` | Filter by session_id field |
| Correlation ID | `--correlation corr_xyz789` | Filter by correlation_id field |
| Source Component | `--source Acode.Security` | Filter by source field |
| Full-Text Search | `--search "ssh"` | Search across all string fields |

### Export Formats

| Format | Extension | Use Case |
|--------|-----------|----------|
| JSON Lines | `.jsonl` | Machine processing, re-import |
| JSON Array | `.json` | API consumption, single file |
| CSV | `.csv` | Spreadsheet analysis |
| Markdown | `.md` | Human-readable reports |
| HTML | `.html` | Browser viewing, sharing |

### Export Command

```bash
acode security export-audit \
  --after 2026-01-01 \
  --before 2026-01-31 \
  --event-type PathBlocked,SecurityBlock \
  --format json \
  --output security-events-jan2026.json
```

---

## Security and Privacy

### Secret Redaction

ALL audit events MUST redact secrets before logging:

| Data Type | Redaction Rule |
|-----------|----------------|
| Environment Variables | Redact values for keys matching `*KEY*`, `*SECRET*`, `*TOKEN*`, `*PASSWORD*`, `*CREDENTIAL*` |
| Command Arguments | Redact arguments matching secret patterns (regex-based) |
| File Paths | Normalize paths (replace home with `~`), redact usernames in paths |
| Configuration Values | Redact values for sensitive config keys |
| LLM Prompts | Hash prompts (SHA-256), do not log full content |
| LLM Responses | Hash responses (SHA-256), log metadata only |

### Access Control

- **File Permissions:** 600 (owner read/write only)
- **Directory Permissions:** 700 (owner access only)
- **No World Readable:** Audit logs MUST NOT be world-readable
- **No Group Readable:** Audit logs MUST NOT be group-readable
- **Verification:** `acode security check-audit-permissions` validates permissions

### Data Minimization

- **Principle:** Log only what is necessary for accountability and debugging
- **No Sensitive Data:** Never log secrets, credentials, or PII
- **Hashing:** Use SHA-256 hashes for large content (prompts, responses)
- **Truncation:** Truncate large fields (e.g., command output limited to 10 KB in audit)

---

## Performance Requirements

| Requirement | Target | Maximum | Notes |
|-------------|--------|---------|-------|
| Audit Write Latency | < 2ms | < 5ms | Async write, should not block operations |
| Audit Write Throughput | > 1000 events/sec | > 500 events/sec | Buffered writes |
| Query Response Time | < 100ms for 1k events | < 500ms | Indexed search recommended |
| Export Time | < 5s for 10k events | < 30s | Streaming export |
| Disk Space Impact | < 100 MB/day typical | < 500 MB/day worst case | With rotation and compression |

---

## Failure Handling

### Audit Write Failure

| Failure | Behavior | Rationale |
|---------|----------|-----------|
| Disk Full | **Fail-Closed:** Halt operations, log to stderr, exit with error | Cannot operate without audit trail |
| Permission Denied | **Fail-Closed:** Halt operations, log to stderr, exit with error | Audit integrity compromised |
| Log Corruption | **Fail-Closed:** Halt operations, log to stderr, create new log | Cannot trust corrupted audit data |
| Temporary I/O Error | **Retry 3x:** Exponential backoff, then fail-closed | Transient errors may resolve |

### Audit Read Failure

| Failure | Behavior | Rationale |
|---------|----------|-----------|
| File Not Found | Return empty result set, log warning | Logs may have been rotated/pruned |
| Corrupted Entry | Skip corrupted entry, log error, continue | Partial data better than none |
| Permission Denied | Fail with clear error message | User lacks necessary access |

---

## Configuration

### Audit Configuration Schema

```yaml
# .agent/config.yml
audit:
  enabled: true  # Cannot be disabled (security invariant)
  log_path: .agent/audit.jsonl
  rotation:
    max_size_mb: 100
    max_age_days: 7
    compression: gzip
  retention:
    default_days: 90
    security_events_days: 365
    error_events_days: 180
  performance:
    async_write: true
    buffer_size: 1000
    flush_interval_ms: 100
  redaction:
    enabled: true  # Cannot be disabled
    secret_patterns:
      - "(?i)password"
      - "(?i)api[_-]?key"
      - "(?i)token"
      - "(?i)secret"
  export:
    default_format: jsonl
    include_redacted_placeholders: true
```

### Security Invariants (Cannot Be Disabled)

1. **`audit.enabled` MUST be true** — Cannot be set to false via config
2. **`audit.redaction.enabled` MUST be true** — Secrets always redacted
3. **File permissions MUST be 600** — Not configurable
4. **Tamper-evident logging MUST be enabled** — Checksums always computed

---

## CLI Commands

### Audit Query

```bash
# View recent audit events
acode security audit --last 24h

# Filter by event type
acode security audit --event-type PathBlocked,SecurityBlock

# Search for specific pattern
acode security audit --search "ssh" --last 7d

# View specific session
acode security audit --session sess_abc123
```

### Audit Export

```bash
# Export to JSON
acode security export-audit --after 2026-01-01 --format json --output audit-jan2026.json

# Export to CSV for spreadsheet analysis
acode security export-audit --last 30d --format csv --output audit-30days.csv

# Export security events only
acode security export-audit --event-type SecurityBlock,SecretRedacted --format md --output security-report.md
```

### Audit Verification

```bash
# Verify audit log integrity
acode security verify-audit

# Check audit log permissions
acode security check-audit-permissions

# View audit statistics
acode security audit-stats
```

### Audit Maintenance

```bash
# Manually rotate logs
acode security rotate-audit

# Manually prune old logs
acode security prune-audit --older-than 180d --confirm

# Repair corrupted logs (creates backup first)
acode security repair-audit --backup
```

---

## Compliance Mapping

| Framework | Requirement | How Audit Baseline Meets It |
|-----------|-------------|------------------------------|
| **SOC2 Type II** | CC7.2: System activities logged and monitored | All operations logged with timestamp, user, action |
| **SOC2 Type II** | CC7.3: Logging integrity protected | Append-only logs, integrity checksums, tamper detection |
| **ISO 27001** | A.12.4.1: Event logging | Comprehensive event logging per schema |
| **ISO 27001** | A.12.4.2: Protection of log information | File permissions, encryption (future), access control |
| **ISO 27001** | A.12.4.3: Administrator and operator logs | All privileged operations logged |
| **GDPR** | Article 30: Records of processing activities | Audit trail provides record of all data processing |
| **GDPR** | Article 32: Security of processing | Tamper-evident logs, encryption, access control |
| **NIST CSF** | DE.AE-3: Event data aggregated and correlated | Session ID and correlation ID for event correlation |
| **NIST CSF** | PR.PT-1: Audit/log records determined and documented | This document defines audit baseline |

---

## Future Enhancements (Out of Scope for v1.0)

The following features are planned for future versions but are NOT required for the baseline:

- **Remote log shipping** (SIEM integration)
- **Encryption at rest** (AES-256 for log files)
- **Real-time alerting** (based on audit events)
- **Anomaly detection** (ML-based pattern recognition)
- **Compliance report generation** (automated SOC2/ISO reports)
- **Log aggregation** (multi-machine log collection)
- **Blockchain-based tamper evidence** (immutable audit trail)

---

## Appendix A: Event Type Catalog

Complete list of all event types with descriptions:

| Event Type | Category | Severity | Description |
|------------|----------|----------|-------------|
| `SessionStart` | Session | Info | Acode process started |
| `SessionEnd` | Session | Info | Acode process terminated |
| `ModeChange` | Session | Warning | Operating mode changed |
| `ConfigLoad` | Configuration | Info | Configuration file loaded |
| `ConfigChange` | Configuration | Warning | Configuration modified |
| `ConfigError` | Configuration | Error | Configuration validation failed |
| `FileRead` | FileOperation | Info | File read attempted |
| `FileWrite` | FileOperation | Info | File write attempted |
| `FileDelete` | FileOperation | Warning | File delete attempted |
| `PathBlocked` | Security | Warning | Protected path access denied |
| `CommandStart` | Execution | Info | Command execution started |
| `CommandComplete` | Execution | Info | Command execution completed |
| `CommandTimeout` | Execution | Warning | Command exceeded timeout |
| `CommandError` | Execution | Error | Command failed to start |
| `PromptSent` | LLM | Info | Prompt sent to external LLM |
| `ResponseReceived` | LLM | Info | Response received from LLM |
| `LlmError` | LLM | Error | LLM API call failed |
| `SecretRedacted` | Security | Info | Secret detected and redacted |
| `SecurityBlock` | Security | Warning | Security control blocked operation |
| `AuditLogRotation` | Audit | Info | Audit log rotated |
| `AuditError` | Audit | Critical | Audit logging failed |

---

## Appendix B: Sample Audit Log

```jsonl
{"schema_version":"1.0.0","event_id":"evt_01HJQZ9X8F2YGN6VW3KPMT4ERC","timestamp":"2026-01-03T10:30:00.000Z","session_id":"sess_01HJQZ9X7DMJR5NQVX3YBKP8QZ","correlation_id":"corr_01HJQZ9X7DMJR5NQVX3YBKP8QZ","event_type":"SessionStart","severity":"Info","source":"Acode.Core","operating_mode":"LocalOnly","data":{"version":"0.1.0-alpha","user":"neilo","working_directory":"/mnt/c/Users/neilo/source/acode","process_id":12345}}
{"schema_version":"1.0.0","event_id":"evt_01HJQZ9XA3T8HKNVQW9XPMF6RY","timestamp":"2026-01-03T10:30:05.123Z","session_id":"sess_01HJQZ9X7DMJR5NQVX3YBKP8QZ","correlation_id":"corr_01HJQZ9XA3T8HKNVQW9XPMF6RY","event_type":"ConfigLoad","severity":"Info","source":"Acode.Configuration","operating_mode":"LocalOnly","data":{"file_path":".agent/config.yml","schema_version":"1.0.0","validation_result":"valid"}}
{"schema_version":"1.0.0","event_id":"evt_01HJQZ9XC7WQMNRVX4YPZK3BTE","timestamp":"2026-01-03T10:30:10.456Z","session_id":"sess_01HJQZ9X7DMJR5NQVX3YBKP8QZ","correlation_id":"corr_01HJQZ9XC7WQMNRVX4YPZK3BTE","event_type":"PathBlocked","severity":"Warning","source":"Acode.Security","operating_mode":"LocalOnly","data":{"path":"~/.ssh/id_rsa","normalized_path":"/home/neilo/.ssh/id_rsa","operation":"Read","matched_pattern":"~/.ssh/id_*","risk_id":"RISK-I-003","category":"SshKeys","reason":"SSH private key files"}}
{"schema_version":"1.0.0","event_id":"evt_01HJQZ9XE2FQNPRVW5XPMZ4CUG","timestamp":"2026-01-03T10:35:00.789Z","session_id":"sess_01HJQZ9X7DMJR5NQVX3YBKP8QZ","correlation_id":"corr_01HJQZ9X7DMJR5NQVX3YBKP8QZ","event_type":"SessionEnd","severity":"Info","source":"Acode.Core","operating_mode":"LocalOnly","data":{"duration_seconds":300,"exit_code":0,"reason":"user_termination"}}
```

---

**End of Audit Baseline Requirements v1.0.0**
