# Task 003.c: Define Audit Baseline Requirements

**Priority:** P0 (Critical)  
**Tier:** Foundation  
**Complexity:** 8 (Fibonacci)  
**Phase:** 0 — Product Definition, Constraints, Repo Contracts  
**Dependencies:** Task 001 (Operating Modes), Task 002 (Config Contract), Task 003 (Threat Model), Task 003.a (Risk Categories), Task 003.b (Protected Paths)  

---

## Description

### Business Value

The Agentic Coding Bot operates autonomously, executing code, modifying files, and running commands without direct human supervision for each action. This autonomy introduces significant accountability challenges: how can developers, security teams, and auditors verify what the agent did, when it did it, and why? The audit baseline requirements establish the fundamental logging, tracing, and record-keeping infrastructure that provides complete transparency into agent operations.

Without comprehensive audit capabilities, organizations cannot use Acode in regulated environments, cannot investigate security incidents, cannot debug unexpected behaviors, and cannot demonstrate compliance with internal policies. The audit baseline is not optional enhancement—it is foundational infrastructure that enables trustworthy autonomous operation.

### Scope

This task defines the audit baseline requirements including: what events MUST be logged, what data MUST be captured for each event, how audit records MUST be stored and protected, how audit logs MUST be queryable and exportable, and how audit integrity MUST be verified. The scope covers the audit schema, storage requirements, retention policies, and verification mechanisms.

The audit baseline applies to ALL operating modes defined in Task 001. Even in Local-Only mode with maximum privacy, local audit logs MUST be maintained. The depth and destination of audit records may vary by mode, but the fundamental audit capability is non-negotiable.

### Integration Points

- **Task 001 (Operating Modes):** Audit behavior adapts to operating mode. Local-Only mode keeps all audit data local. Burst Mode may batch audit events. Air-gapped Mode has stricter audit requirements.
- **Task 002 (Config Contract):** Audit configuration is specified in `.agent/config.yml` under the `audit` section. This includes log levels, retention periods, and export formats.
- **Task 002.b (Parser/Validator):** The config parser validates audit configuration settings.
- **Task 003 (Threat Model):** Audit requirements derive from threat model. Audit logs are critical evidence for incident response.
- **Task 003.a (Risk Categories):** Audit events reference risk IDs for traceability. Each logged event maps to potential risks it helps mitigate.
- **Task 003.b (Protected Paths):** Audit of protected path access attempts is mandatory and cannot be disabled.

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Audit log corruption | Loss of accountability evidence | Integrity checksums, append-only storage |
| Audit disk full | Operations blocked or audit gaps | Space monitoring, rotation, fail-closed on full |
| Audit disabled by attacker | Stealth attacks possible | Immutable audit settings, log to multiple destinations |
| Incomplete event capture | Gaps in accountability | Comprehensive event enumeration, tests for each event |
| PII in audit logs | Privacy violation | Data classification, automatic redaction |
| Performance impact | User experience degradation | Async logging, batching, sampling for high-volume events |

### Assumptions

1. Local file system is available for audit storage
2. Operating system provides reliable timestamps
3. Disk space can be monitored
4. Log files can be rotated without losing data
5. Users may need to export audit logs for external analysis
6. Audit logs may be subject to legal discovery
7. Audit data may contain sensitive information requiring protection

### Security Model

The audit subsystem operates under the principle of **defense in depth**: even if other security controls fail, comprehensive audit logs enable detection and investigation. Audit logs themselves are protected assets that MUST NOT be modifiable by the agent or by malicious actors.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Audit Event | A discrete, logged occurrence capturing an action, decision, or state change in the system |
| Audit Log | Persistent storage containing chronologically ordered audit events |
| Audit Trail | Complete sequence of audit events related to a specific operation or session |
| Correlation ID | Unique identifier linking related audit events across a session |
| Event Schema | Structured definition of fields captured for each audit event type |
| Retention Period | Duration for which audit logs MUST be preserved before archival or deletion |
| Log Rotation | Process of archiving current logs and starting new log files |
| Integrity Checksum | Cryptographic hash verifying log content has not been modified |
| Redaction | Process of removing or masking sensitive data before logging |
| Structured Logging | Logging with consistent, machine-parseable format (JSON) |
| Log Level | Severity classification: Debug, Info, Warning, Error, Critical |
| Audit Sink | Destination for audit events (file, console, remote) |
| Tamper-Evident | Property ensuring modifications to logs are detectable |
| Session | Bounded period of agent operation from start to termination |
| Span | Unit of work within a session, may contain child spans |
| High-Water Mark | Checkpoint indicating last successfully processed log entry |

---

## Out of Scope

The following items are explicitly NOT part of this task:

- Real-time log streaming to external services (requires network)
- SIEM integration (Security Information and Event Management)
- Log aggregation across multiple machines
- Cloud-based log storage (conflicts with Local-Only mode)
- Machine learning anomaly detection on logs
- Automated incident response based on log patterns
- Compliance certification for specific regulations (SOC2, HIPAA)
- Legal hold and e-discovery procedures
- Log encryption at rest (may be added in future task)
- Remote log collection agents
- Centralized logging infrastructure
- Audit log backup to network locations

---

## Functional Requirements

### Core Audit Infrastructure (FR-003c-01 to FR-003c-20)

| ID | Requirement |
|----|-------------|
| FR-003c-01 | System MUST implement structured audit logging |
| FR-003c-02 | Audit logs MUST use JSON format |
| FR-003c-03 | Each audit event MUST have unique event ID |
| FR-003c-04 | Each audit event MUST have ISO 8601 timestamp |
| FR-003c-05 | Timestamps MUST include timezone (UTC preferred) |
| FR-003c-06 | Each audit event MUST have correlation ID |
| FR-003c-07 | Correlation ID MUST persist across related events |
| FR-003c-08 | Each audit event MUST have event type |
| FR-003c-09 | Event types MUST be from defined enumeration |
| FR-003c-10 | Each audit event MUST have severity level |
| FR-003c-11 | Severity levels MUST be: Debug, Info, Warning, Error, Critical |
| FR-003c-12 | Each audit event MUST have source component |
| FR-003c-13 | Source component MUST identify originating module |
| FR-003c-14 | Each audit event MUST have session ID |
| FR-003c-15 | Session ID MUST be generated at startup |
| FR-003c-16 | Session ID MUST be unique across all sessions |
| FR-003c-17 | Each audit event MAY have parent span ID |
| FR-003c-18 | Span IDs MUST form traceable hierarchy |
| FR-003c-19 | Audit events MUST be ordered chronologically |
| FR-003c-20 | Audit infrastructure MUST be initialized before any operation |

### Mandatory Audit Events (FR-003c-21 to FR-003c-45)

| ID | Requirement |
|----|-------------|
| FR-003c-21 | Session start MUST be audited |
| FR-003c-22 | Session end MUST be audited |
| FR-003c-23 | Configuration load MUST be audited |
| FR-003c-24 | Configuration validation errors MUST be audited |
| FR-003c-25 | Operating mode selection MUST be audited |
| FR-003c-26 | Command execution start MUST be audited |
| FR-003c-27 | Command execution end MUST be audited |
| FR-003c-28 | Command execution failure MUST be audited |
| FR-003c-29 | File read operations MUST be audited |
| FR-003c-30 | File write operations MUST be audited |
| FR-003c-31 | File delete operations MUST be audited |
| FR-003c-32 | Directory creation MUST be audited |
| FR-003c-33 | Directory deletion MUST be audited |
| FR-003c-34 | Protected path access attempts MUST be audited |
| FR-003c-35 | Security policy violations MUST be audited |
| FR-003c-36 | Task execution start MUST be audited |
| FR-003c-37 | Task execution end MUST be audited |
| FR-003c-38 | Task execution failure MUST be audited |
| FR-003c-39 | User approval requests MUST be audited |
| FR-003c-40 | User approval responses MUST be audited |
| FR-003c-41 | Code generation events MUST be audited |
| FR-003c-42 | Test execution MUST be audited |
| FR-003c-43 | Build execution MUST be audited |
| FR-003c-44 | Error recovery attempts MUST be audited |
| FR-003c-45 | Graceful shutdown MUST be audited |

### Audit Event Data Requirements (FR-003c-46 to FR-003c-65)

| ID | Requirement |
|----|-------------|
| FR-003c-46 | File operation events MUST include file path |
| FR-003c-47 | File paths in events MUST be relative to repo root |
| FR-003c-48 | File operation events MUST include operation type |
| FR-003c-49 | File write events MUST include bytes written |
| FR-003c-50 | File write events MUST NOT include file contents |
| FR-003c-51 | Command events MUST include command name |
| FR-003c-52 | Command events MUST include command arguments |
| FR-003c-53 | Command events MUST redact sensitive arguments |
| FR-003c-54 | Command events MUST include exit code |
| FR-003c-55 | Command events MUST include duration |
| FR-003c-56 | Error events MUST include error code |
| FR-003c-57 | Error events MUST include error message |
| FR-003c-58 | Error events MUST include stack trace (debug level) |
| FR-003c-59 | Security events MUST include risk ID |
| FR-003c-60 | Security events MUST include mitigation reference |
| FR-003c-61 | Approval events MUST include what was requested |
| FR-003c-62 | Approval events MUST include user decision |
| FR-003c-63 | Task events MUST include task identifier |
| FR-003c-64 | Task events MUST include task status |
| FR-003c-65 | All events MUST include operating mode |

### Audit Storage (FR-003c-66 to FR-003c-85)

| ID | Requirement |
|----|-------------|
| FR-003c-66 | Audit logs MUST be written to local file system |
| FR-003c-67 | Default audit location MUST be `.agent/logs/audit/` |
| FR-003c-68 | Audit location MUST be configurable |
| FR-003c-69 | Audit files MUST use `.jsonl` extension |
| FR-003c-70 | Each session MUST have separate audit file |
| FR-003c-71 | Audit file name MUST include session start timestamp |
| FR-003c-72 | Audit file name MUST include session ID |
| FR-003c-73 | Audit writes MUST be append-only |
| FR-003c-74 | Audit files MUST NOT be truncated |
| FR-003c-75 | Audit writes MUST be flushed immediately |
| FR-003c-76 | Audit writes MUST be atomic per event |
| FR-003c-77 | Concurrent writes MUST be serialized |
| FR-003c-78 | Write failures MUST trigger fail-safe behavior |
| FR-003c-79 | Disk full MUST trigger configurable response |
| FR-003c-80 | Default disk full response MUST be halt operations |
| FR-003c-81 | Audit files MUST have restricted permissions |
| FR-003c-82 | Unix permissions MUST be 0600 (owner read/write only) |
| FR-003c-83 | Windows ACL MUST restrict to current user |
| FR-003c-84 | Audit directory MUST be created if not exists |
| FR-003c-85 | Audit directory permissions MUST be 0700 |

### Log Rotation (FR-003c-86 to FR-003c-100)

| ID | Requirement |
|----|-------------|
| FR-003c-86 | Audit logs MUST support rotation |
| FR-003c-87 | Rotation MUST be based on file size |
| FR-003c-88 | Default max file size MUST be 100MB |
| FR-003c-89 | Max file size MUST be configurable |
| FR-003c-90 | Rotation MUST create new file with incremented suffix |
| FR-003c-91 | Rotated files MUST retain original permissions |
| FR-003c-92 | Rotation MUST be atomic (no lost events) |
| FR-003c-93 | Rotation MUST preserve chronological order |
| FR-003c-94 | Retention period MUST be configurable |
| FR-003c-95 | Default retention MUST be 90 days |
| FR-003c-96 | Expired logs MUST be deleted automatically |
| FR-003c-97 | Deletion MUST be logged before execution |
| FR-003c-98 | Max total audit storage MUST be configurable |
| FR-003c-99 | Default max storage MUST be 1GB |
| FR-003c-100 | Storage limit MUST trigger oldest-first deletion |

### Audit Integrity (FR-003c-101 to FR-003c-115)

| ID | Requirement |
|----|-------------|
| FR-003c-101 | Audit logs MUST be tamper-evident |
| FR-003c-102 | Each log file MUST have integrity checksum |
| FR-003c-103 | Checksum MUST be SHA-256 |
| FR-003c-104 | Checksum MUST be in separate `.sha256` file |
| FR-003c-105 | Checksum MUST be updated after each write |
| FR-003c-106 | Checksum file MUST have same permissions as log |
| FR-003c-107 | Integrity verification command MUST exist |
| FR-003c-108 | Verification MUST detect any modification |
| FR-003c-109 | Verification MUST detect truncation |
| FR-003c-110 | Verification MUST detect insertion |
| FR-003c-111 | Verification failure MUST be reported clearly |
| FR-003c-112 | Logs MUST NOT be modifiable by agent operations |
| FR-003c-113 | Agent MUST NOT have delete permission on logs |
| FR-003c-114 | Session start MUST record log file path |
| FR-003c-115 | Session end MUST record final checksum |

---

## Non-Functional Requirements

### Performance (NFR-003c-01 to NFR-003c-15)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-003c-01 | Performance | Audit logging MUST NOT block main execution |
| NFR-003c-02 | Performance | Audit write latency MUST be < 10ms (p99) |
| NFR-003c-03 | Performance | Audit MUST use async I/O where possible |
| NFR-003c-04 | Performance | Audit buffer MUST be configurable |
| NFR-003c-05 | Performance | Default buffer MUST be 1000 events |
| NFR-003c-06 | Performance | Buffer overflow MUST not lose events |
| NFR-003c-07 | Performance | Overflow MUST block or spill to disk |
| NFR-003c-08 | Performance | JSON serialization MUST be fast |
| NFR-003c-09 | Performance | Memory per event MUST be < 4KB |
| NFR-003c-10 | Performance | Total audit memory MUST be < 10MB |
| NFR-003c-11 | Performance | Audit CPU usage MUST be < 5% |
| NFR-003c-12 | Performance | Checksum update MUST be incremental |
| NFR-003c-13 | Performance | Log rotation MUST complete in < 100ms |
| NFR-003c-14 | Performance | Log query MUST use indexed access |
| NFR-003c-15 | Performance | Export MUST stream without full load |

### Security (NFR-003c-16 to NFR-003c-30)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-003c-16 | Security | Audit logs MUST have restricted permissions |
| NFR-003c-17 | Security | Logs MUST NOT be world-readable |
| NFR-003c-18 | Security | Agent MUST NOT modify past log entries |
| NFR-003c-19 | Security | Secrets MUST be redacted before logging |
| NFR-003c-20 | Security | Redaction MUST use consistent patterns |
| NFR-003c-21 | Security | API keys MUST be fully redacted |
| NFR-003c-22 | Security | Passwords MUST be fully redacted |
| NFR-003c-23 | Security | Tokens MUST be fully redacted |
| NFR-003c-24 | Security | PII MUST be handled per configuration |
| NFR-003c-25 | Security | File contents MUST NOT be logged |
| NFR-003c-26 | Security | Stack traces MUST be debug-level only |
| NFR-003c-27 | Security | Log injection MUST be prevented |
| NFR-003c-28 | Security | Special characters MUST be escaped |
| NFR-003c-29 | Security | Newlines in data MUST be escaped |
| NFR-003c-30 | Security | Integrity verification MUST be available |

### Reliability (NFR-003c-31 to NFR-003c-45)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-003c-31 | Reliability | Audit MUST NOT lose events |
| NFR-003c-32 | Reliability | Write failures MUST be retried |
| NFR-003c-33 | Reliability | Max retries MUST be 3 |
| NFR-003c-34 | Reliability | Retry backoff MUST be exponential |
| NFR-003c-35 | Reliability | Persistent failure MUST halt operations |
| NFR-003c-36 | Reliability | Crash recovery MUST not corrupt logs |
| NFR-003c-37 | Reliability | Partial writes MUST be detectable |
| NFR-003c-38 | Reliability | Recovery MUST skip incomplete events |
| NFR-003c-39 | Reliability | Log file locking MUST be reliable |
| NFR-003c-40 | Reliability | Concurrent access MUST be safe |
| NFR-003c-41 | Reliability | Disk I/O errors MUST be handled |
| NFR-003c-42 | Reliability | Full disk MUST be handled gracefully |
| NFR-003c-43 | Reliability | Audit init failure MUST prevent startup |
| NFR-003c-44 | Reliability | Shutdown MUST flush pending events |
| NFR-003c-45 | Reliability | Force quit MUST not corrupt logs |

### Maintainability (NFR-003c-46 to NFR-003c-55)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-003c-46 | Maintainability | Event schema MUST be versioned |
| NFR-003c-47 | Maintainability | Schema version MUST be in each event |
| NFR-003c-48 | Maintainability | Schema changes MUST be backward compatible |
| NFR-003c-49 | Maintainability | Old logs MUST remain parseable |
| NFR-003c-50 | Maintainability | Event types MUST be documented |
| NFR-003c-51 | Maintainability | New events MUST be easy to add |
| NFR-003c-52 | Maintainability | Audit code MUST be centralized |
| NFR-003c-53 | Maintainability | Audit tests MUST cover all event types |
| NFR-003c-54 | Maintainability | Log format MUST be documented |
| NFR-003c-55 | Maintainability | Migration tools MUST exist for schema changes |

---

## User Manual Documentation

### Overview

The Agentic Coding Bot maintains comprehensive audit logs of all operations. These logs provide accountability, support debugging, enable security investigations, and demonstrate compliance with organizational policies. Audit logging is always enabled and cannot be disabled.

### Audit Log Location

By default, audit logs are stored in:

```
<repository-root>/.agent/logs/audit/
```

Each session creates a new log file:

```
.agent/logs/audit/
├── 2024-01-15T10-30-00Z_sess_abc123.jsonl
├── 2024-01-15T10-30-00Z_sess_abc123.jsonl.sha256
├── 2024-01-15T14-45-30Z_sess_def456.jsonl
├── 2024-01-15T14-45-30Z_sess_def456.jsonl.sha256
└── ...
```

### Configuration

Configure audit settings in `.agent/config.yml`:

```yaml
audit:
  # Log level: debug, info, warning, error, critical
  level: info
  
  # Custom log directory (relative to repo root)
  directory: .agent/logs/audit
  
  # Maximum file size before rotation (bytes)
  max_file_size: 104857600  # 100MB
  
  # Retention period in days
  retention_days: 90
  
  # Maximum total storage (bytes)
  max_total_storage: 1073741824  # 1GB
  
  # Behavior when disk is full: halt, warn_continue
  on_disk_full: halt
  
  # Enable console output of audit events
  console_echo: false
  
  # Include debug information (stack traces, etc.)
  include_debug: false
```

### Viewing Audit Logs

#### List Sessions

```bash
# List all audit sessions
agentic-coder audit list

# List sessions from specific date
agentic-coder audit list --date 2024-01-15

# List sessions with summary
agentic-coder audit list --verbose
```

**Example Output:**

```
Audit Sessions
==============

Session                           Started              Duration    Events
--------------------------------  -------------------  ----------  ------
sess_abc123                       2024-01-15 10:30:00  45m         1,234
sess_def456                       2024-01-15 14:45:30  12m           456
sess_ghi789                       2024-01-16 09:00:00  2h 30m      3,456

Total: 3 sessions, 5,146 events
```

#### View Session Events

```bash
# View all events in a session
agentic-coder audit show sess_abc123

# View with filtering
agentic-coder audit show sess_abc123 --level warning
agentic-coder audit show sess_abc123 --type file_operation
agentic-coder audit show sess_abc123 --after "2024-01-15T10:35:00Z"

# View as JSON
agentic-coder audit show sess_abc123 --format json

# Tail mode (follow new events in current session)
agentic-coder audit tail
```

#### Search Across Sessions

```bash
# Search for events matching criteria
agentic-coder audit search --query "protected_path"
agentic-coder audit search --risk-id "RISK-E-003"
agentic-coder audit search --error-code "ACODE-SEC-003"

# Search in date range
agentic-coder audit search --from "2024-01-01" --to "2024-01-31" --query "error"
```

### Audit Event Types

The following event types are captured:

| Event Type | Description |
|------------|-------------|
| `session_start` | Agent session began |
| `session_end` | Agent session ended |
| `config_load` | Configuration file loaded |
| `config_error` | Configuration validation error |
| `mode_select` | Operating mode selected |
| `command_start` | External command execution started |
| `command_end` | External command completed |
| `command_error` | External command failed |
| `file_read` | File read operation |
| `file_write` | File write operation |
| `file_delete` | File deletion |
| `dir_create` | Directory creation |
| `dir_delete` | Directory deletion |
| `protected_path_blocked` | Protected path access blocked |
| `security_violation` | Security policy violation |
| `task_start` | Task execution started |
| `task_end` | Task completed |
| `task_error` | Task failed |
| `approval_request` | User approval requested |
| `approval_response` | User provided approval response |
| `code_generated` | Code generation event |
| `test_execution` | Test run |
| `build_execution` | Build run |
| `error_recovery` | Error recovery attempted |
| `shutdown` | Graceful shutdown initiated |

### Audit Event Schema

Each audit event follows this JSON schema:

```json
{
  "schema_version": "1.0",
  "event_id": "evt_a1b2c3d4e5f6",
  "timestamp": "2024-01-15T10:30:45.123Z",
  "session_id": "sess_abc123",
  "correlation_id": "corr_xyz789",
  "span_id": "span_001",
  "parent_span_id": "span_000",
  "event_type": "file_write",
  "severity": "info",
  "source": "AgenticCoder.Infrastructure.FileSystem",
  "operating_mode": "local_only",
  "data": {
    "path": "src/Program.cs",
    "bytes_written": 1234,
    "operation": "write"
  },
  "context": {
    "task_id": "task_001",
    "command": "implement feature"
  }
}
```

### Verifying Log Integrity

```bash
# Verify a specific session's log integrity
agentic-coder audit verify sess_abc123

# Verify all logs
agentic-coder audit verify --all

# Verify with detailed output
agentic-coder audit verify --verbose
```

**Example Output:**

```
Audit Log Integrity Verification
================================

Session: sess_abc123
  File: 2024-01-15T10-30-00Z_sess_abc123.jsonl
  Size: 1,234,567 bytes
  Events: 1,234
  Expected SHA-256: a1b2c3d4...
  Computed SHA-256: a1b2c3d4...
  Status: ✓ VALID

Session: sess_def456
  File: 2024-01-15T14-45-30Z_sess_def456.jsonl
  Size: 456,789 bytes
  Events: 456
  Expected SHA-256: e5f6g7h8...
  Computed SHA-256: e5f6g7h8...
  Status: ✓ VALID

Summary: 2/2 sessions verified successfully
```

### Exporting Audit Logs

```bash
# Export session to file
agentic-coder audit export sess_abc123 --output audit-export.json

# Export with filtering
agentic-coder audit export sess_abc123 --level warning --output warnings.json

# Export date range
agentic-coder audit export --from "2024-01-01" --to "2024-01-31" --output january.json

# Export as CSV (for spreadsheet analysis)
agentic-coder audit export sess_abc123 --format csv --output audit.csv
```

### Log Rotation and Retention

Logs are automatically rotated when they reach the configured size limit:

```
original.jsonl          →  original.jsonl.1
original.jsonl.1        →  original.jsonl.2
(new) original.jsonl
```

Logs older than the retention period are automatically deleted during startup and periodically during operation. Deletion events are themselves logged before the deletion occurs.

### Troubleshooting

#### No Audit Logs Found

1. Check the audit directory exists: `.agent/logs/audit/`
2. Check permissions on the directory
3. Verify configuration in `.agent/config.yml`

#### Disk Full Errors

```bash
# Check audit storage usage
agentic-coder audit stats

# Force cleanup of old logs
agentic-coder audit cleanup --force
```

#### Integrity Verification Failed

If integrity verification fails, the log may have been modified:

```
Session: sess_abc123
  Status: ✗ INVALID - Checksum mismatch
  Expected: a1b2c3d4...
  Computed: x9y8z7w6...
  Action: Log may have been tampered with. Investigate immediately.
```

**Actions:**
1. Preserve the log file for forensic analysis
2. Check system logs for unauthorized access
3. Report to security team if tampering is suspected

### Best Practices

1. **Regular Verification:** Run `audit verify --all` weekly
2. **Monitor Storage:** Check `audit stats` to avoid disk full
3. **Retain Important Logs:** Export logs before retention expiry
4. **Review Security Events:** Regularly search for security violations
5. **Protect Log Directory:** Ensure no unauthorized access to `.agent/logs/`

---

## Acceptance Criteria

### Core Infrastructure

- [ ] AC-001: Audit logging is implemented
- [ ] AC-002: Audit uses JSON format
- [ ] AC-003: Each event has unique event_id
- [ ] AC-004: Each event has ISO 8601 timestamp
- [ ] AC-005: Timestamp includes timezone
- [ ] AC-006: Each event has correlation_id
- [ ] AC-007: Correlation_id persists across related events
- [ ] AC-008: Each event has event_type
- [ ] AC-009: Event types are from defined enum
- [ ] AC-010: Each event has severity level
- [ ] AC-011: Severity levels match specification
- [ ] AC-012: Each event has source component
- [ ] AC-013: Each event has session_id
- [ ] AC-014: Session_id is unique
- [ ] AC-015: Events support parent_span_id
- [ ] AC-016: Span hierarchy is traceable
- [ ] AC-017: Events are chronologically ordered
- [ ] AC-018: Audit initializes before any operation
- [ ] AC-019: Schema version is in each event
- [ ] AC-020: Operating mode is in each event

### Mandatory Events

- [ ] AC-021: session_start is logged
- [ ] AC-022: session_end is logged
- [ ] AC-023: config_load is logged
- [ ] AC-024: config_error is logged
- [ ] AC-025: mode_select is logged
- [ ] AC-026: command_start is logged
- [ ] AC-027: command_end is logged
- [ ] AC-028: command_error is logged
- [ ] AC-029: file_read is logged
- [ ] AC-030: file_write is logged
- [ ] AC-031: file_delete is logged
- [ ] AC-032: dir_create is logged
- [ ] AC-033: dir_delete is logged
- [ ] AC-034: protected_path_blocked is logged
- [ ] AC-035: security_violation is logged
- [ ] AC-036: task_start is logged
- [ ] AC-037: task_end is logged
- [ ] AC-038: task_error is logged
- [ ] AC-039: approval_request is logged
- [ ] AC-040: approval_response is logged
- [ ] AC-041: code_generated is logged
- [ ] AC-042: test_execution is logged
- [ ] AC-043: build_execution is logged
- [ ] AC-044: error_recovery is logged
- [ ] AC-045: shutdown is logged

### Event Data

- [ ] AC-046: File events include path
- [ ] AC-047: File paths are relative to repo root
- [ ] AC-048: File events include operation type
- [ ] AC-049: File write events include bytes_written
- [ ] AC-050: File contents are NOT logged
- [ ] AC-051: Command events include command name
- [ ] AC-052: Command events include arguments
- [ ] AC-053: Sensitive arguments are redacted
- [ ] AC-054: Command events include exit_code
- [ ] AC-055: Command events include duration
- [ ] AC-056: Error events include error_code
- [ ] AC-057: Error events include message
- [ ] AC-058: Stack traces are debug-level only
- [ ] AC-059: Security events include risk_id
- [ ] AC-060: Security events include mitigation reference
- [ ] AC-061: Approval events include request details
- [ ] AC-062: Approval events include user decision
- [ ] AC-063: Task events include task_id
- [ ] AC-064: Task events include status
- [ ] AC-065: All events include operating_mode

### Storage

- [ ] AC-066: Logs are written to local file system
- [ ] AC-067: Default location is .agent/logs/audit/
- [ ] AC-068: Location is configurable
- [ ] AC-069: Files use .jsonl extension
- [ ] AC-070: Each session has separate file
- [ ] AC-071: Filename includes timestamp
- [ ] AC-072: Filename includes session_id
- [ ] AC-073: Writes are append-only
- [ ] AC-074: Files are not truncated
- [ ] AC-075: Writes are flushed immediately
- [ ] AC-076: Writes are atomic per event
- [ ] AC-077: Concurrent writes are serialized
- [ ] AC-078: Write failures trigger fail-safe
- [ ] AC-079: Disk full triggers configured response
- [ ] AC-080: Default disk full halts operations
- [ ] AC-081: Files have restricted permissions
- [ ] AC-082: Unix permissions are 0600
- [ ] AC-083: Windows ACL restricts to user
- [ ] AC-084: Directory is created if missing
- [ ] AC-085: Directory permissions are 0700

### Log Rotation

- [ ] AC-086: Rotation is supported
- [ ] AC-087: Rotation is based on file size
- [ ] AC-088: Default max size is 100MB
- [ ] AC-089: Max size is configurable
- [ ] AC-090: Rotation creates new file
- [ ] AC-091: Rotated files retain permissions
- [ ] AC-092: Rotation is atomic
- [ ] AC-093: Rotation preserves order
- [ ] AC-094: Retention period is configurable
- [ ] AC-095: Default retention is 90 days
- [ ] AC-096: Expired logs are deleted
- [ ] AC-097: Deletion is logged first
- [ ] AC-098: Max storage is configurable
- [ ] AC-099: Default max storage is 1GB
- [ ] AC-100: Storage limit deletes oldest first

### Integrity

- [ ] AC-101: Logs are tamper-evident
- [ ] AC-102: Each log has integrity checksum
- [ ] AC-103: Checksum uses SHA-256
- [ ] AC-104: Checksum is in .sha256 file
- [ ] AC-105: Checksum updates after each write
- [ ] AC-106: Checksum file has same permissions
- [ ] AC-107: Verification command exists
- [ ] AC-108: Verification detects modification
- [ ] AC-109: Verification detects truncation
- [ ] AC-110: Verification detects insertion
- [ ] AC-111: Verification failure is reported
- [ ] AC-112: Agent cannot modify past entries
- [ ] AC-113: Agent cannot delete logs
- [ ] AC-114: Session start records log path
- [ ] AC-115: Session end records final checksum

### CLI Commands

- [ ] AC-116: `audit list` shows sessions
- [ ] AC-117: `audit list --date` filters by date
- [ ] AC-118: `audit list --verbose` shows summary
- [ ] AC-119: `audit show` displays events
- [ ] AC-120: `--level` filters by severity
- [ ] AC-121: `--type` filters by event type
- [ ] AC-122: `--after` filters by time
- [ ] AC-123: `--format json` outputs JSON
- [ ] AC-124: `audit tail` follows current session
- [ ] AC-125: `audit search` searches across sessions
- [ ] AC-126: `--query` text search works
- [ ] AC-127: `--risk-id` filter works
- [ ] AC-128: `--error-code` filter works
- [ ] AC-129: `audit verify` verifies integrity
- [ ] AC-130: `--all` verifies all sessions
- [ ] AC-131: `audit export` exports to file
- [ ] AC-132: `--format csv` exports CSV
- [ ] AC-133: `audit stats` shows usage
- [ ] AC-134: `audit cleanup` removes old logs

### Performance

- [ ] AC-135: Logging does not block main execution
- [ ] AC-136: Write latency < 10ms (p99)
- [ ] AC-137: Async I/O is used
- [ ] AC-138: Buffer is configurable
- [ ] AC-139: Buffer overflow is handled
- [ ] AC-140: Memory per event < 4KB
- [ ] AC-141: Total memory < 10MB
- [ ] AC-142: CPU usage < 5%
- [ ] AC-143: Checksum update is efficient
- [ ] AC-144: Rotation completes in < 100ms
- [ ] AC-145: Export streams without full load

### Security

- [ ] AC-146: Logs have restricted permissions
- [ ] AC-147: Logs are not world-readable
- [ ] AC-148: Past entries cannot be modified
- [ ] AC-149: Secrets are redacted
- [ ] AC-150: API keys are redacted
- [ ] AC-151: Passwords are redacted
- [ ] AC-152: Tokens are redacted
- [ ] AC-153: File contents are not logged
- [ ] AC-154: Log injection is prevented
- [ ] AC-155: Special characters are escaped

### Reliability

- [ ] AC-156: Events are not lost
- [ ] AC-157: Write failures are retried
- [ ] AC-158: Crash recovery works
- [ ] AC-159: Partial writes are detected
- [ ] AC-160: File locking is reliable
- [ ] AC-161: Disk I/O errors are handled
- [ ] AC-162: Full disk is handled
- [ ] AC-163: Shutdown flushes pending events

### Documentation

- [ ] AC-164: Event schema is documented
- [ ] AC-165: All event types are documented
- [ ] AC-166: Configuration options are documented
- [ ] AC-167: CLI commands are documented
- [ ] AC-168: Troubleshooting guide exists
- [ ] AC-169: Best practices are documented
- [ ] AC-170: Schema version history exists

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Domain/Audit/
├── AuditEventTests.cs
│   ├── Should_Generate_Unique_EventId()
│   ├── Should_Include_ISO8601_Timestamp()
│   ├── Should_Include_Timezone()
│   ├── Should_Include_SessionId()
│   ├── Should_Include_CorrelationId()
│   ├── Should_Include_EventType()
│   ├── Should_Include_Severity()
│   ├── Should_Include_Source()
│   ├── Should_Include_SchemaVersion()
│   ├── Should_Include_OperatingMode()
│   ├── Should_Support_SpanId()
│   ├── Should_Support_ParentSpanId()
│   └── Should_Serialize_To_ValidJson()
│
├── AuditLoggerTests.cs
│   ├── Should_Log_SessionStart()
│   ├── Should_Log_SessionEnd()
│   ├── Should_Log_ConfigLoad()
│   ├── Should_Log_FileOperations()
│   ├── Should_Log_CommandExecution()
│   ├── Should_Log_SecurityViolations()
│   ├── Should_Log_TaskEvents()
│   ├── Should_Log_ApprovalEvents()
│   ├── Should_Maintain_CorrelationId()
│   ├── Should_Not_Block_MainThread()
│   └── Should_Handle_HighVolume()
│
├── RedactionTests.cs
│   ├── Should_Redact_ApiKeys()
│   ├── Should_Redact_Passwords()
│   ├── Should_Redact_Tokens()
│   ├── Should_Redact_SecretPatterns()
│   ├── Should_Not_Log_FileContents()
│   ├── Should_Escape_SpecialCharacters()
│   ├── Should_Escape_Newlines()
│   └── Should_Prevent_LogInjection()
│
├── IntegrityTests.cs
│   ├── Should_Compute_SHA256_Checksum()
│   ├── Should_Update_Checksum_OnWrite()
│   ├── Should_Detect_Modification()
│   ├── Should_Detect_Truncation()
│   ├── Should_Detect_Insertion()
│   └── Should_Write_ChecksumFile()
│
└── RotationTests.cs
    ├── Should_Rotate_OnSizeLimit()
    ├── Should_Create_NewFile()
    ├── Should_Preserve_Permissions()
    ├── Should_Be_Atomic()
    ├── Should_Delete_ExpiredLogs()
    └── Should_Respect_StorageLimit()
```

### Integration Tests

```
Tests/Integration/Audit/
├── AuditStorageTests.cs
│   ├── Should_CreateAuditDirectory()
│   ├── Should_WriteToCorrectLocation()
│   ├── Should_UseJsonlFormat()
│   ├── Should_CreateFilePerSession()
│   ├── Should_IncludeTimestampInFilename()
│   ├── Should_SetCorrectPermissions_Unix()
│   ├── Should_SetCorrectPermissions_Windows()
│   ├── Should_HandleConcurrentWrites()
│   └── Should_FlushImmediately()
│
├── AuditConfigTests.cs
│   ├── Should_LoadFromConfig()
│   ├── Should_UseDefaultValues()
│   ├── Should_ValidateConfiguration()
│   ├── Should_ApplyLogLevel()
│   └── Should_ApplyRetentionSettings()
│
├── AuditRecoveryTests.cs
│   ├── Should_RecoverFromCrash()
│   ├── Should_DetectPartialWrite()
│   ├── Should_HandleDiskFull()
│   └── Should_RetryOnFailure()
│
└── CLIIntegrationTests.cs
    ├── AuditList_ShouldShowSessions()
    ├── AuditShow_ShouldDisplayEvents()
    ├── AuditSearch_ShouldFindEvents()
    ├── AuditVerify_ShouldValidateIntegrity()
    ├── AuditExport_ShouldCreateFile()
    └── AuditStats_ShouldShowUsage()
```

### End-to-End Tests

```
Tests/E2E/Audit/
├── AuditScenarios.cs
│   ├── Scenario_CompleteSession_AllEventsLogged()
│   ├── Scenario_FileOperations_CorrectlyAudited()
│   ├── Scenario_SecurityViolation_CapturedWithDetails()
│   ├── Scenario_ErrorRecovery_TrackedInAudit()
│   ├── Scenario_GracefulShutdown_AuditComplete()
│   ├── Scenario_ForcedShutdown_NoDataLoss()
│   ├── Scenario_LogRotation_DuringSession()
│   ├── Scenario_IntegrityVerification_AfterSession()
│   └── Scenario_ExportAndAnalysis()
```

### Performance Tests

```
Tests/Performance/Audit/
├── AuditBenchmarks.cs
│   ├── Benchmark_SingleEventWrite()
│   ├── Benchmark_1000EventsPerSecond()
│   ├── Benchmark_ChecksumUpdate()
│   ├── Benchmark_LogRotation()
│   ├── Benchmark_SearchQuery()
│   └── Benchmark_Export()
│
└── AuditLoadTests.cs
    ├── Should_Handle_HighEventRate()
    ├── Should_NotExceedMemoryLimit()
    ├── Should_NotExceedCPULimit()
    └── Should_MaintainLatency_UnderLoad()
```

### Regression Tests

```
Tests/Regression/Audit/
├── EventLossTests.cs
│   ├── Should_NotLose_Events_OnCrash()
│   ├── Should_NotLose_Events_OnDiskFull()
│   ├── Should_NotLose_Events_OnHighLoad()
│   └── Should_NotLose_Events_OnRotation()
│
└── SecurityRegressionTests.cs
    ├── Should_AlwaysRedact_Secrets()
    ├── Should_NeverLog_FileContents()
    ├── Should_MaintainPermissions()
    └── Should_PreventTampering()
```

---

## User Verification Steps

### Scenario 1: Verify Session Logging

**Objective:** Confirm session start and end are logged

1. Start agentic-coder: `agentic-coder start`
2. Perform a simple operation
3. Exit gracefully
4. Run: `agentic-coder audit list`
5. Verify new session appears in list
6. Run: `agentic-coder audit show <session_id>`
7. Verify `session_start` event at beginning
8. Verify `session_end` event at end
9. Verify both have correct timestamps

**Expected Result:**
- Session is listed with correct timestamp
- Start and end events present
- Events have all required fields

### Scenario 2: Verify File Operation Logging

**Objective:** Confirm file operations are audited

1. Start agentic-coder
2. Execute command that reads a file
3. Execute command that writes a file
4. Exit gracefully
5. Run: `agentic-coder audit show <session_id> --type file_read`
6. Verify file_read event logged
7. Verify path is relative to repo root
8. Verify file contents are NOT logged
9. Run: `agentic-coder audit show <session_id> --type file_write`
10. Verify file_write event logged
11. Verify bytes_written is recorded

**Expected Result:**
- All file operations captured
- Paths are relative
- No file contents in logs

### Scenario 3: Verify Security Event Logging

**Objective:** Confirm security violations are captured

1. Start agentic-coder
2. Attempt to access protected path (e.g., ~/.ssh/)
3. Verify operation is blocked
4. Exit gracefully
5. Run: `agentic-coder audit show <session_id> --type protected_path_blocked`
6. Verify event contains risk_id
7. Verify event contains pattern matched
8. Verify event has WARNING severity

**Expected Result:**
- Security violation captured
- Risk ID referenced
- Appropriate severity

### Scenario 4: Verify Command Logging with Redaction

**Objective:** Confirm commands are logged with secrets redacted

1. Configure a command with sensitive parameter
2. Execute command that includes password or token
3. Exit gracefully
4. Check audit log
5. Verify command_start and command_end events
6. Verify sensitive arguments are redacted (shown as [REDACTED])
7. Verify exit code is captured
8. Verify duration is captured

**Expected Result:**
- Commands logged with arguments
- Secrets properly redacted
- Exit code and duration present

### Scenario 5: Verify Correlation ID Tracking

**Objective:** Confirm related events share correlation ID

1. Start agentic-coder
2. Execute a multi-step task
3. Exit gracefully
4. Run: `agentic-coder audit show <session_id>`
5. Note correlation_id on first event
6. Verify subsequent related events have same correlation_id
7. Verify span_id hierarchy is correct

**Expected Result:**
- Related events share correlation_id
- Span hierarchy is traceable
- Events can be grouped by correlation

### Scenario 6: Verify Audit Log Location

**Objective:** Confirm logs are in correct location

1. Check `.agent/logs/audit/` directory exists
2. Start and complete a session
3. Verify new .jsonl file created
4. Verify .sha256 file created alongside
5. Verify filename includes timestamp and session_id

**Expected Result:**
- Logs in correct directory
- Proper file naming
- Checksum file present

### Scenario 7: Verify Integrity Verification

**Objective:** Confirm integrity check works

1. Complete a session
2. Run: `agentic-coder audit verify <session_id>`
3. Verify output shows VALID
4. Manually modify the log file (add a character)
5. Run: `agentic-coder audit verify <session_id>`
6. Verify output shows INVALID - Checksum mismatch
7. Restore the log file

**Expected Result:**
- Valid logs pass verification
- Modified logs detected
- Clear error message on mismatch

### Scenario 8: Verify Log Rotation

**Objective:** Confirm logs rotate at size limit

1. Configure max_file_size to small value (1MB)
2. Generate many audit events
3. Observe log rotation occurs
4. Verify rotated file has .1 suffix
5. Verify new events go to fresh file
6. Verify no events lost during rotation

**Expected Result:**
- Rotation occurs at size limit
- Files properly numbered
- No event loss

### Scenario 9: Verify Retention Policy

**Objective:** Confirm old logs are cleaned up

1. Configure retention_days to 1
2. Create a fake old log file with old timestamp
3. Run: `agentic-coder audit cleanup`
4. Verify old log is deleted
5. Verify recent logs are retained
6. Verify deletion was logged

**Expected Result:**
- Old logs deleted
- Recent logs kept
- Cleanup logged

### Scenario 10: Verify CLI Search

**Objective:** Confirm search across sessions works

1. Complete multiple sessions with various events
2. Run: `agentic-coder audit search --type file_write`
3. Verify results from multiple sessions
4. Run: `agentic-coder audit search --level warning`
5. Verify only warning/error events shown
6. Run: `agentic-coder audit search --query "protected"`
7. Verify text search finds relevant events

**Expected Result:**
- Search works across sessions
- Filters apply correctly
- Text search functional

### Scenario 11: Verify Export Functionality

**Objective:** Confirm audit export works

1. Complete a session
2. Run: `agentic-coder audit export <session_id> --output export.json`
3. Verify export.json created
4. Verify JSON is valid
5. Run: `agentic-coder audit export <session_id> --format csv --output export.csv`
6. Verify CSV is valid
7. Open in spreadsheet application

**Expected Result:**
- JSON export valid
- CSV export valid
- Data matches original

### Scenario 12: Verify Disk Full Handling

**Objective:** Confirm graceful handling of disk full

1. Simulate disk full scenario (if safe to do)
2. Attempt operations
3. Verify configured behavior (halt or warn)
4. Verify no data corruption
5. Clear space and verify recovery

**Expected Result:**
- Configured behavior followed
- No data loss or corruption
- Graceful recovery

---

## Implementation Prompt

### File Structure

```
src/
├── AgenticCoder.Domain/
│   ├── Audit/
│   │   ├── AuditEvent.cs
│   │   ├── AuditEventType.cs
│   │   ├── AuditSeverity.cs
│   │   ├── IAuditLogger.cs
│   │   ├── AuditSession.cs
│   │   ├── CorrelationContext.cs
│   │   ├── SpanContext.cs
│   │   └── AuditConfiguration.cs
│   └── ValueObjects/
│       ├── EventId.cs
│       ├── SessionId.cs
│       ├── CorrelationId.cs
│       └── SpanId.cs
│
├── AgenticCoder.Application/
│   ├── Audit/
│   │   ├── Commands/
│   │   │   ├── StartAuditSessionCommand.cs
│   │   │   ├── EndAuditSessionCommand.cs
│   │   │   ├── LogEventCommand.cs
│   │   │   └── CleanupLogsCommand.cs
│   │   ├── Queries/
│   │   │   ├── ListSessionsQuery.cs
│   │   │   ├── GetSessionEventsQuery.cs
│   │   │   ├── SearchEventsQuery.cs
│   │   │   └── GetAuditStatsQuery.cs
│   │   └── Services/
│   │       ├── AuditService.cs
│   │       └── CorrelationService.cs
│
├── AgenticCoder.Infrastructure/
│   ├── Audit/
│   │   ├── FileAuditWriter.cs
│   │   ├── AuditLogRotator.cs
│   │   ├── AuditIntegrityVerifier.cs
│   │   ├── AuditRedactor.cs
│   │   ├── AuditExporter.cs
│   │   └── AuditConfigurationLoader.cs
│
└── AgenticCoder.CLI/
    └── Commands/
        └── Audit/
            ├── AuditListCommand.cs
            ├── AuditShowCommand.cs
            ├── AuditSearchCommand.cs
            ├── AuditVerifyCommand.cs
            ├── AuditExportCommand.cs
            ├── AuditStatsCommand.cs
            ├── AuditTailCommand.cs
            └── AuditCleanupCommand.cs
```

### Core Interfaces

```csharp
namespace AgenticCoder.Domain.Audit;

/// <summary>
/// Represents a single audit event.
/// Immutable and serializable to JSON.
/// </summary>
public sealed record AuditEvent
{
    public required string SchemaVersion { get; init; }
    public required EventId EventId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required SessionId SessionId { get; init; }
    public required CorrelationId CorrelationId { get; init; }
    public SpanId? SpanId { get; init; }
    public SpanId? ParentSpanId { get; init; }
    public required AuditEventType EventType { get; init; }
    public required AuditSeverity Severity { get; init; }
    public required string Source { get; init; }
    public required string OperatingMode { get; init; }
    public required IReadOnlyDictionary<string, object> Data { get; init; }
    public IReadOnlyDictionary<string, object>? Context { get; init; }
}

public enum AuditEventType
{
    SessionStart,
    SessionEnd,
    ConfigLoad,
    ConfigError,
    ModeSelect,
    CommandStart,
    CommandEnd,
    CommandError,
    FileRead,
    FileWrite,
    FileDelete,
    DirCreate,
    DirDelete,
    ProtectedPathBlocked,
    SecurityViolation,
    TaskStart,
    TaskEnd,
    TaskError,
    ApprovalRequest,
    ApprovalResponse,
    CodeGenerated,
    TestExecution,
    BuildExecution,
    ErrorRecovery,
    Shutdown
}

public enum AuditSeverity
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Interface for audit logging operations.
/// All operations MUST be non-blocking.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs an audit event. MUST NOT block the calling thread.
    /// </summary>
    Task LogAsync(AuditEvent auditEvent);
    
    /// <summary>
    /// Logs an audit event with automatic timestamp and session context.
    /// </summary>
    Task LogAsync(
        AuditEventType eventType,
        AuditSeverity severity,
        string source,
        IDictionary<string, object> data,
        IDictionary<string, object>? context = null);
    
    /// <summary>
    /// Starts a new correlation scope. Events within scope share correlation ID.
    /// </summary>
    IDisposable BeginCorrelation(string description);
    
    /// <summary>
    /// Starts a new span within current correlation.
    /// </summary>
    IDisposable BeginSpan(string operation);
    
    /// <summary>
    /// Flushes all pending audit events.
    /// MUST be called during graceful shutdown.
    /// </summary>
    Task FlushAsync();
}
```

### Audit Writer Implementation

```csharp
namespace AgenticCoder.Infrastructure.Audit;

/// <summary>
/// Writes audit events to JSONL files with integrity checksums.
/// SECURITY CRITICAL: Append-only, tamper-evident logging.
/// </summary>
public sealed class FileAuditWriter : IAuditWriter, IAsyncDisposable
{
    private readonly string _auditDirectory;
    private readonly AuditConfiguration _config;
    private readonly ConcurrentQueue<AuditEvent> _buffer;
    private readonly SemaphoreSlim _writeLock;
    private StreamWriter? _currentWriter;
    private string? _currentFilePath;
    private IncrementalHash? _runningHash;
    
    public async Task WriteAsync(AuditEvent auditEvent)
    {
        // Serialize event to JSON (single line)
        var json = JsonSerializer.Serialize(auditEvent, _jsonOptions);
        
        // Ensure no newlines in output (prevent log injection)
        if (json.Contains('\n') || json.Contains('\r'))
        {
            throw new InvalidOperationException("Event contains invalid characters");
        }
        
        await _writeLock.WaitAsync();
        try
        {
            // Check rotation needed
            if (await NeedsRotationAsync())
            {
                await RotateAsync();
            }
            
            // Write event
            await _currentWriter!.WriteLineAsync(json);
            await _currentWriter.FlushAsync();
            
            // Update checksum
            UpdateChecksum(json);
        }
        finally
        {
            _writeLock.Release();
        }
    }
    
    private void UpdateChecksum(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json + "\n");
        _runningHash!.AppendData(bytes);
        
        // Write updated checksum
        var checksumPath = _currentFilePath + ".sha256";
        var hash = _runningHash.GetHashAndReset();
        File.WriteAllText(checksumPath, Convert.ToHexString(hash).ToLowerInvariant());
    }
}
```

### Redaction Implementation

```csharp
namespace AgenticCoder.Infrastructure.Audit;

/// <summary>
/// Redacts sensitive data from audit events.
/// SECURITY CRITICAL: MUST catch all sensitive patterns.
/// </summary>
public sealed class AuditRedactor
{
    private static readonly Regex[] SensitivePatterns = new[]
    {
        new Regex(@"password[""']?\s*[:=]\s*[""']?[^""'\s,}]+", RegexOptions.IgnoreCase),
        new Regex(@"token[""']?\s*[:=]\s*[""']?[^""'\s,}]+", RegexOptions.IgnoreCase),
        new Regex(@"api[_-]?key[""']?\s*[:=]\s*[""']?[^""'\s,}]+", RegexOptions.IgnoreCase),
        new Regex(@"secret[""']?\s*[:=]\s*[""']?[^""'\s,}]+", RegexOptions.IgnoreCase),
        new Regex(@"bearer\s+[a-zA-Z0-9\-._~+/]+=*", RegexOptions.IgnoreCase),
        new Regex(@"-----BEGIN\s+[A-Z\s]+-----", RegexOptions.IgnoreCase),
    };
    
    private const string RedactedMarker = "[REDACTED]";
    
    public string Redact(string input)
    {
        var result = input;
        foreach (var pattern in SensitivePatterns)
        {
            result = pattern.Replace(result, match =>
            {
                var prefix = match.Value.Split(new[] { ':', '=' }, 2)[0];
                return $"{prefix}={RedactedMarker}";
            });
        }
        return result;
    }
    
    public IDictionary<string, object> RedactData(IDictionary<string, object> data)
    {
        var redacted = new Dictionary<string, object>();
        foreach (var (key, value) in data)
        {
            if (IsSensitiveKey(key))
            {
                redacted[key] = RedactedMarker;
            }
            else if (value is string strValue)
            {
                redacted[key] = Redact(strValue);
            }
            else
            {
                redacted[key] = value;
            }
        }
        return redacted;
    }
    
    private bool IsSensitiveKey(string key)
    {
        var lower = key.ToLowerInvariant();
        return lower.Contains("password") ||
               lower.Contains("secret") ||
               lower.Contains("token") ||
               lower.Contains("api_key") ||
               lower.Contains("apikey") ||
               lower.Contains("credential");
    }
}
```

### Error Codes

| Code | Description |
|------|-------------|
| ACODE-AUD-001 | Audit initialization failed |
| ACODE-AUD-002 | Audit write failed |
| ACODE-AUD-003 | Audit directory not writable |
| ACODE-AUD-004 | Disk full - audit halted |
| ACODE-AUD-005 | Log rotation failed |
| ACODE-AUD-006 | Integrity verification failed |
| ACODE-AUD-007 | Checksum mismatch detected |
| ACODE-AUD-008 | Session not found |
| ACODE-AUD-009 | Export failed |
| ACODE-AUD-010 | Invalid query parameters |

### CLI Exit Codes

| Exit Code | Meaning |
|-----------|---------|
| 0 | Success |
| 1 | Verification failed (integrity issue) |
| 2 | Invalid arguments |
| 3 | Audit system error |
| 4 | Session not found |

### Logging Schema Fields

| Field | Type | Description |
|-------|------|-------------|
| schema_version | string | Event schema version (e.g., "1.0") |
| event_id | string | Unique event identifier |
| timestamp | string | ISO 8601 timestamp with timezone |
| session_id | string | Session identifier |
| correlation_id | string | Correlation identifier |
| span_id | string? | Span identifier (optional) |
| parent_span_id | string? | Parent span identifier (optional) |
| event_type | string | Event type from enumeration |
| severity | string | debug, info, warning, error, critical |
| source | string | Component that generated event |
| operating_mode | string | Current operating mode |
| data | object | Event-specific data |
| context | object? | Additional context (optional) |

### Implementation Checklist

1. [ ] Implement `AuditEvent` record with all fields
2. [ ] Implement `AuditEventType` enumeration
3. [ ] Implement `AuditSeverity` enumeration
4. [ ] Implement `IAuditLogger` interface
5. [ ] Implement `FileAuditWriter` with append-only writes
6. [ ] Implement checksum generation and update
7. [ ] Implement log rotation logic
8. [ ] Implement retention policy enforcement
9. [ ] Implement `AuditRedactor` for secret redaction
10. [ ] Implement correlation ID tracking
11. [ ] Implement span hierarchy
12. [ ] Implement CLI `audit list` command
13. [ ] Implement CLI `audit show` command
14. [ ] Implement CLI `audit search` command
15. [ ] Implement CLI `audit verify` command
16. [ ] Implement CLI `audit export` command
17. [ ] Implement CLI `audit stats` command
18. [ ] Implement CLI `audit tail` command
19. [ ] Implement CLI `audit cleanup` command
20. [ ] Add audit events for all mandatory event types
21. [ ] Integrate with file operations
22. [ ] Integrate with command execution
23. [ ] Integrate with security violations
24. [ ] Write unit tests for all event types
25. [ ] Write integration tests for storage
26. [ ] Write performance benchmarks
27. [ ] Document event schema
28. [ ] Document CLI commands
29. [ ] Conduct security review

### Dependencies

- Task 001 (Operating Modes) - for mode-aware logging
- Task 002 (Config Contract) - for audit configuration
- Task 002.b (Parser/Validator) - for config loading
- Task 003.a (Risk Categories) - for risk ID references
- Task 003.b (Protected Paths) - for security event logging

### Verification Command

```bash
# Run all audit tests
dotnet test --filter "FullyQualifiedName~Audit"

# Run security tests
dotnet test --filter "Category=Security&FullyQualifiedName~Audit"

# Run performance benchmarks
dotnet run --project Tests/Performance -- --filter "*Audit*"

# Verify audit functionality
agentic-coder audit verify --all
```

---

**End of Task 003.c Specification**