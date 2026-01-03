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
```

#### AuditEventTests.cs

```csharp
namespace AgenticCoder.Tests.Unit.Domain.Audit;

using AgenticCoder.Domain.Audit;
using FluentAssertions;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

public class AuditEventTests
{
    [Fact]
    public void Should_Generate_Unique_EventId()
    {
        // Arrange & Act
        var event1 = CreateTestEvent();
        var event2 = CreateTestEvent();

        // Assert
        event1.EventId.Should().NotBeNull();
        event2.EventId.Should().NotBeNull();
        event1.EventId.Value.Should().NotBe(event2.EventId.Value,
            because: "each event must have unique ID");
        event1.EventId.Value.Should().MatchRegex(@"^evt_[a-zA-Z0-9]+$",
            because: "event ID must follow evt_xxx format");
    }

    [Fact]
    public void Should_Include_ISO8601_Timestamp()
    {
        // Arrange & Act
        var auditEvent = CreateTestEvent();

        // Assert
        auditEvent.Timestamp.Should().BeCloseTo(
            DateTimeOffset.UtcNow, 
            TimeSpan.FromSeconds(5));
        
        // Verify ISO 8601 format when serialized
        var json = JsonSerializer.Serialize(auditEvent);
        var iso8601Pattern = @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}";
        Regex.IsMatch(json, iso8601Pattern).Should().BeTrue(
            because: "timestamp must be ISO 8601 format");
    }

    [Fact]
    public void Should_Include_Timezone()
    {
        // Arrange & Act
        var auditEvent = CreateTestEvent();
        var json = JsonSerializer.Serialize(auditEvent);

        // Assert - timestamp should end with Z (UTC) or offset
        var timezonePattern = @"(\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2}))";
        Regex.IsMatch(json, timezonePattern).Should().BeTrue(
            because: "timestamp must include timezone");
    }

    [Fact]
    public void Should_Include_SessionId()
    {
        // Arrange
        var sessionId = SessionId.New();
        var auditEvent = CreateTestEvent(sessionId: sessionId);

        // Assert
        auditEvent.SessionId.Should().Be(sessionId);
        auditEvent.SessionId.Value.Should().MatchRegex(@"^sess_[a-zA-Z0-9]+$",
            because: "session ID must follow sess_xxx format");
    }

    [Fact]
    public void Should_Include_CorrelationId()
    {
        // Arrange
        var correlationId = CorrelationId.New();
        var auditEvent = CreateTestEvent(correlationId: correlationId);

        // Assert
        auditEvent.CorrelationId.Should().Be(correlationId);
        auditEvent.CorrelationId.Value.Should().MatchRegex(@"^corr_[a-zA-Z0-9]+$",
            because: "correlation ID must follow corr_xxx format");
    }

    [Fact]
    public void Should_Include_EventType()
    {
        // Arrange & Act
        var auditEvent = CreateTestEvent(eventType: AuditEventType.FileWrite);

        // Assert
        auditEvent.EventType.Should().Be(AuditEventType.FileWrite);
        Enum.IsDefined(typeof(AuditEventType), auditEvent.EventType).Should().BeTrue(
            because: "event type must be from defined enumeration");
    }

    [Fact]
    public void Should_Include_Severity()
    {
        // Arrange & Act
        var auditEvent = CreateTestEvent(severity: AuditSeverity.Warning);

        // Assert
        auditEvent.Severity.Should().Be(AuditSeverity.Warning);
        Enum.IsDefined(typeof(AuditSeverity), auditEvent.Severity).Should().BeTrue(
            because: "severity must be from defined enumeration");
    }

    [Theory]
    [InlineData(AuditSeverity.Debug)]
    [InlineData(AuditSeverity.Info)]
    [InlineData(AuditSeverity.Warning)]
    [InlineData(AuditSeverity.Error)]
    [InlineData(AuditSeverity.Critical)]
    public void Should_Support_All_Severity_Levels(AuditSeverity severity)
    {
        // Arrange & Act
        var auditEvent = CreateTestEvent(severity: severity);

        // Assert
        auditEvent.Severity.Should().Be(severity);
    }

    [Fact]
    public void Should_Include_Source()
    {
        // Arrange
        var source = "AgenticCoder.Infrastructure.FileSystem";
        var auditEvent = CreateTestEvent(source: source);

        // Assert
        auditEvent.Source.Should().Be(source);
        auditEvent.Source.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Should_Include_SchemaVersion()
    {
        // Arrange & Act
        var auditEvent = CreateTestEvent();

        // Assert
        auditEvent.SchemaVersion.Should().NotBeNullOrWhiteSpace();
        auditEvent.SchemaVersion.Should().MatchRegex(@"^\d+\.\d+$",
            because: "schema version must be in X.Y format");
    }

    [Fact]
    public void Should_Include_OperatingMode()
    {
        // Arrange & Act
        var auditEvent = CreateTestEvent(operatingMode: "local_only");

        // Assert
        auditEvent.OperatingMode.Should().Be("local_only");
        auditEvent.OperatingMode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Should_Support_SpanId()
    {
        // Arrange
        var spanId = SpanId.New();
        var auditEvent = CreateTestEvent(spanId: spanId);

        // Assert
        auditEvent.SpanId.Should().Be(spanId);
        auditEvent.SpanId!.Value.Should().MatchRegex(@"^span_[a-zA-Z0-9]+$");
    }

    [Fact]
    public void Should_Support_ParentSpanId()
    {
        // Arrange
        var parentSpanId = SpanId.New();
        var spanId = SpanId.New();
        var auditEvent = CreateTestEvent(spanId: spanId, parentSpanId: parentSpanId);

        // Assert
        auditEvent.ParentSpanId.Should().Be(parentSpanId);
        auditEvent.SpanId.Should().Be(spanId);
        auditEvent.ParentSpanId!.Value.Should().NotBe(auditEvent.SpanId!.Value);
    }

    [Fact]
    public void Should_Serialize_To_ValidJson()
    {
        // Arrange
        var auditEvent = CreateTestEvent(
            data: new Dictionary<string, object>
            {
                ["path"] = "src/Program.cs",
                ["bytes_written"] = 1234
            });

        // Act
        var json = JsonSerializer.Serialize(auditEvent);

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        
        // Should be parseable
        var parsed = JsonDocument.Parse(json);
        parsed.RootElement.GetProperty("event_id").GetString()
            .Should().NotBeNullOrWhiteSpace();
        parsed.RootElement.GetProperty("timestamp").GetString()
            .Should().NotBeNullOrWhiteSpace();
        parsed.RootElement.GetProperty("event_type").GetString()
            .Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Should_Serialize_To_Single_Line()
    {
        // Arrange
        var auditEvent = CreateTestEvent(
            data: new Dictionary<string, object>
            {
                ["multiline"] = "line1\nline2\nline3"
            });

        // Act
        var json = JsonSerializer.Serialize(auditEvent, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        // Assert
        json.Should().NotContain("\n",
            because: "JSONL format requires single-line entries");
        json.Should().NotContain("\r",
            because: "JSONL format requires single-line entries");
        
        // Newlines in data should be escaped
        json.Should().Contain("\\n",
            because: "embedded newlines must be escaped");
    }

    private static AuditEvent CreateTestEvent(
        SessionId? sessionId = null,
        CorrelationId? correlationId = null,
        SpanId? spanId = null,
        SpanId? parentSpanId = null,
        AuditEventType eventType = AuditEventType.FileWrite,
        AuditSeverity severity = AuditSeverity.Info,
        string source = "TestSource",
        string operatingMode = "local_only",
        IDictionary<string, object>? data = null)
    {
        return new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = sessionId ?? SessionId.New(),
            CorrelationId = correlationId ?? CorrelationId.New(),
            SpanId = spanId,
            ParentSpanId = parentSpanId,
            EventType = eventType,
            Severity = severity,
            Source = source,
            OperatingMode = operatingMode,
            Data = (data ?? new Dictionary<string, object>()).AsReadOnly()
        };
    }
}
```

```
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
```

#### AuditLoggerTests.cs

```csharp
namespace AgenticCoder.Tests.Unit.Domain.Audit;

using AgenticCoder.Domain.Audit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit;

public class AuditLoggerTests : IAsyncDisposable
{
    private readonly Mock<IAuditWriter> _writerMock;
    private readonly IAuditLogger _logger;
    private readonly ConcurrentBag<AuditEvent> _capturedEvents;

    public AuditLoggerTests()
    {
        _writerMock = new Mock<IAuditWriter>();
        _capturedEvents = new ConcurrentBag<AuditEvent>();
        
        _writerMock
            .Setup(w => w.WriteAsync(It.IsAny<AuditEvent>()))
            .Callback<AuditEvent>(e => _capturedEvents.Add(e))
            .Returns(Task.CompletedTask);

        _logger = new AuditLogger(
            _writerMock.Object,
            SessionId.New(),
            "local_only",
            Mock.Of<ILogger<AuditLogger>>());
    }

    public async ValueTask DisposeAsync()
    {
        await _logger.FlushAsync();
    }

    [Fact]
    public async Task Should_Log_SessionStart()
    {
        // Act
        await _logger.LogAsync(
            AuditEventType.SessionStart,
            AuditSeverity.Info,
            "AgenticCoder.CLI",
            new Dictionary<string, object>
            {
                ["version"] = "1.0.0",
                ["working_directory"] = "/home/user/project"
            });

        await _logger.FlushAsync();

        // Assert
        _capturedEvents.Should().ContainSingle(e => 
            e.EventType == AuditEventType.SessionStart);
        
        var sessionStart = _capturedEvents.First(e => 
            e.EventType == AuditEventType.SessionStart);
        sessionStart.Severity.Should().Be(AuditSeverity.Info);
        sessionStart.Data.Should().ContainKey("version");
    }

    [Fact]
    public async Task Should_Log_SessionEnd()
    {
        // Act
        await _logger.LogAsync(
            AuditEventType.SessionEnd,
            AuditSeverity.Info,
            "AgenticCoder.CLI",
            new Dictionary<string, object>
            {
                ["exit_code"] = 0,
                ["duration_ms"] = 45000,
                ["events_logged"] = 1234
            });

        await _logger.FlushAsync();

        // Assert
        _capturedEvents.Should().ContainSingle(e => 
            e.EventType == AuditEventType.SessionEnd);
        
        var sessionEnd = _capturedEvents.First(e => 
            e.EventType == AuditEventType.SessionEnd);
        sessionEnd.Data["exit_code"].Should().Be(0);
    }

    [Fact]
    public async Task Should_Log_ConfigLoad()
    {
        // Act
        await _logger.LogAsync(
            AuditEventType.ConfigLoad,
            AuditSeverity.Info,
            "AgenticCoder.Infrastructure.Configuration",
            new Dictionary<string, object>
            {
                ["config_path"] = ".agent/config.yml",
                ["operating_mode"] = "local_only"
            });

        await _logger.FlushAsync();

        // Assert
        _capturedEvents.Should().ContainSingle(e => 
            e.EventType == AuditEventType.ConfigLoad);
    }

    [Fact]
    public async Task Should_Log_FileOperations()
    {
        // Act - log read
        await _logger.LogAsync(
            AuditEventType.FileRead,
            AuditSeverity.Info,
            "AgenticCoder.Infrastructure.FileSystem",
            new Dictionary<string, object>
            {
                ["path"] = "src/Program.cs",
                ["bytes_read"] = 2048
            });

        // Act - log write
        await _logger.LogAsync(
            AuditEventType.FileWrite,
            AuditSeverity.Info,
            "AgenticCoder.Infrastructure.FileSystem",
            new Dictionary<string, object>
            {
                ["path"] = "src/NewFile.cs",
                ["bytes_written"] = 1024
            });

        // Act - log delete
        await _logger.LogAsync(
            AuditEventType.FileDelete,
            AuditSeverity.Warning,
            "AgenticCoder.Infrastructure.FileSystem",
            new Dictionary<string, object>
            {
                ["path"] = "src/Obsolete.cs"
            });

        await _logger.FlushAsync();

        // Assert
        _capturedEvents.Should().Contain(e => e.EventType == AuditEventType.FileRead);
        _capturedEvents.Should().Contain(e => e.EventType == AuditEventType.FileWrite);
        _capturedEvents.Should().Contain(e => e.EventType == AuditEventType.FileDelete);

        var writeEvent = _capturedEvents.First(e => e.EventType == AuditEventType.FileWrite);
        writeEvent.Data["path"].Should().Be("src/NewFile.cs");
        writeEvent.Data.Should().NotContainKey("content",
            because: "file contents must never be logged");
    }

    [Fact]
    public async Task Should_Log_CommandExecution()
    {
        // Act - command start
        await _logger.LogAsync(
            AuditEventType.CommandStart,
            AuditSeverity.Info,
            "AgenticCoder.Infrastructure.Process",
            new Dictionary<string, object>
            {
                ["command"] = "dotnet",
                ["arguments"] = "build",
                ["working_directory"] = "/home/user/project"
            });

        // Act - command end
        await _logger.LogAsync(
            AuditEventType.CommandEnd,
            AuditSeverity.Info,
            "AgenticCoder.Infrastructure.Process",
            new Dictionary<string, object>
            {
                ["command"] = "dotnet",
                ["exit_code"] = 0,
                ["duration_ms"] = 5432
            });

        await _logger.FlushAsync();

        // Assert
        _capturedEvents.Should().Contain(e => e.EventType == AuditEventType.CommandStart);
        _capturedEvents.Should().Contain(e => e.EventType == AuditEventType.CommandEnd);
        
        var endEvent = _capturedEvents.First(e => e.EventType == AuditEventType.CommandEnd);
        endEvent.Data["exit_code"].Should().Be(0);
        endEvent.Data["duration_ms"].Should().Be(5432);
    }

    [Fact]
    public async Task Should_Log_SecurityViolations()
    {
        // Act
        await _logger.LogAsync(
            AuditEventType.ProtectedPathBlocked,
            AuditSeverity.Warning,
            "AgenticCoder.Domain.Security.PathProtection",
            new Dictionary<string, object>
            {
                ["attempted_path"] = "[REDACTED]",
                ["matched_pattern"] = "~/.ssh/*",
                ["risk_id"] = "RISK-E-003",
                ["error_code"] = "ACODE-SEC-003-001"
            });

        await _logger.FlushAsync();

        // Assert
        var violation = _capturedEvents.First(e => 
            e.EventType == AuditEventType.ProtectedPathBlocked);
        
        violation.Severity.Should().Be(AuditSeverity.Warning);
        violation.Data["risk_id"].Should().Be("RISK-E-003");
        violation.Data["error_code"].Should().Be("ACODE-SEC-003-001");
    }

    [Fact]
    public async Task Should_Log_TaskEvents()
    {
        // Act
        await _logger.LogAsync(
            AuditEventType.TaskStart,
            AuditSeverity.Info,
            "AgenticCoder.Application.Tasks",
            new Dictionary<string, object>
            {
                ["task_id"] = "task_001",
                ["description"] = "Implement feature X"
            });

        await _logger.LogAsync(
            AuditEventType.TaskEnd,
            AuditSeverity.Info,
            "AgenticCoder.Application.Tasks",
            new Dictionary<string, object>
            {
                ["task_id"] = "task_001",
                ["status"] = "completed",
                ["duration_ms"] = 120000
            });

        await _logger.FlushAsync();

        // Assert
        _capturedEvents.Should().Contain(e => e.EventType == AuditEventType.TaskStart);
        _capturedEvents.Should().Contain(e => e.EventType == AuditEventType.TaskEnd);
    }

    [Fact]
    public async Task Should_Log_ApprovalEvents()
    {
        // Act - request approval
        await _logger.LogAsync(
            AuditEventType.ApprovalRequest,
            AuditSeverity.Info,
            "AgenticCoder.Application.Approval",
            new Dictionary<string, object>
            {
                ["request_id"] = "req_001",
                ["action"] = "delete_file",
                ["target"] = "important_file.cs"
            });

        // Act - user response
        await _logger.LogAsync(
            AuditEventType.ApprovalResponse,
            AuditSeverity.Info,
            "AgenticCoder.Application.Approval",
            new Dictionary<string, object>
            {
                ["request_id"] = "req_001",
                ["decision"] = "approved",
                ["response_time_ms"] = 3500
            });

        await _logger.FlushAsync();

        // Assert
        var request = _capturedEvents.First(e => e.EventType == AuditEventType.ApprovalRequest);
        var response = _capturedEvents.First(e => e.EventType == AuditEventType.ApprovalResponse);
        
        request.Data["action"].Should().Be("delete_file");
        response.Data["decision"].Should().Be("approved");
    }

    [Fact]
    public async Task Should_Maintain_CorrelationId()
    {
        // Arrange
        using var scope = _logger.BeginCorrelation("Test operation");

        // Act
        await _logger.LogAsync(AuditEventType.TaskStart, AuditSeverity.Info,
            "Test", new Dictionary<string, object> { ["step"] = 1 });
        await _logger.LogAsync(AuditEventType.FileRead, AuditSeverity.Info,
            "Test", new Dictionary<string, object> { ["step"] = 2 });
        await _logger.LogAsync(AuditEventType.TaskEnd, AuditSeverity.Info,
            "Test", new Dictionary<string, object> { ["step"] = 3 });

        await _logger.FlushAsync();

        // Assert
        var events = _capturedEvents.ToList();
        events.Should().HaveCount(3);
        
        var correlationId = events[0].CorrelationId;
        events.Should().OnlyContain(e => e.CorrelationId == correlationId,
            because: "all events in scope share correlation ID");
    }

    [Fact]
    public async Task Should_Not_Block_MainThread()
    {
        // Arrange
        var sw = Stopwatch.StartNew();
        
        // Act - log many events rapidly
        for (int i = 0; i < 100; i++)
        {
            await _logger.LogAsync(
                AuditEventType.FileRead,
                AuditSeverity.Debug,
                "Test",
                new Dictionary<string, object> { ["iteration"] = i });
        }
        sw.Stop();

        // Assert - logging 100 events should be fast (async)
        sw.ElapsedMilliseconds.Should().BeLessThan(100,
            because: "logging must not block main thread");
        
        // Flush and verify all logged
        await _logger.FlushAsync();
        _capturedEvents.Count.Should().Be(100);
    }

    [Fact]
    public async Task Should_Handle_HighVolume()
    {
        // Arrange
        const int eventCount = 10000;
        
        // Act
        var tasks = Enumerable.Range(0, eventCount)
            .Select(i => _logger.LogAsync(
                AuditEventType.FileRead,
                AuditSeverity.Debug,
                "Test",
                new Dictionary<string, object> { ["iteration"] = i }))
            .ToList();

        await Task.WhenAll(tasks);
        await _logger.FlushAsync();

        // Assert
        _capturedEvents.Count.Should().Be(eventCount,
            because: "no events should be lost under high volume");
    }
}
```

```
├── RedactionTests.cs
│   ├── Should_Redact_ApiKeys()
│   ├── Should_Redact_Passwords()
│   ├── Should_Redact_Tokens()
│   ├── Should_Redact_SecretPatterns()
│   ├── Should_Not_Log_FileContents()
│   ├── Should_Escape_SpecialCharacters()
│   ├── Should_Escape_Newlines()
│   └── Should_Prevent_LogInjection()
```

#### RedactionTests.cs

```csharp
namespace AgenticCoder.Tests.Unit.Domain.Audit;

using AgenticCoder.Infrastructure.Audit;
using FluentAssertions;
using Xunit;

public class RedactionTests
{
    private readonly AuditRedactor _redactor;

    public RedactionTests()
    {
        _redactor = new AuditRedactor();
    }

    [Theory]
    [InlineData("api_key=abc123secret", "api_key=[REDACTED]")]
    [InlineData("apiKey: xyz789token", "apiKey=[REDACTED]")]
    [InlineData("API_KEY=AKIAIOSFODNN7EXAMPLE", "API_KEY=[REDACTED]")]
    [InlineData("x-api-key: secret123", "x-api-key=[REDACTED]")]
    public void Should_Redact_ApiKeys(string input, string expected)
    {
        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Should().Be(expected);
        result.Should().NotContain("abc123");
        result.Should().NotContain("xyz789");
        result.Should().NotContain("AKIAIOSFODNN7EXAMPLE");
    }

    [Theory]
    [InlineData("password=mysecret123", "password=[REDACTED]")]
    [InlineData("Password: hunter2", "Password=[REDACTED]")]
    [InlineData("db_password=verysecret", "db_password=[REDACTED]")]
    [InlineData("\"password\": \"secret123\"", "\"password\"=[REDACTED]")]
    public void Should_Redact_Passwords(string input, string expected)
    {
        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("mysecret");
        result.Should().NotContain("hunter2");
        result.Should().NotContain("verysecret");
    }

    [Theory]
    [InlineData("token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.xxx", "token=[REDACTED]")]
    [InlineData("access_token: ghp_1234567890abcdef", "access_token=[REDACTED]")]
    [InlineData("Bearer eyJhbGciOiJIUzI1NiJ9.xxx.yyy", "Bearer [REDACTED]")]
    [InlineData("Authorization: Bearer abc123", "Authorization: Bearer [REDACTED]")]
    public void Should_Redact_Tokens(string input, string expected)
    {
        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
        result.Should().NotContain("ghp_1234567890");
    }

    [Theory]
    [InlineData("secret=my_secret_value")]
    [InlineData("client_secret: abcdef123456")]
    [InlineData("aws_secret_access_key=wJalrXUtnFEMI/K7MDENG")]
    [InlineData("private_key=-----BEGIN RSA PRIVATE KEY-----")]
    public void Should_Redact_SecretPatterns(string input)
    {
        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("my_secret_value");
        result.Should().NotContain("abcdef123456");
        result.Should().NotContain("wJalrXUtnFEMI");
        result.Should().NotContain("-----BEGIN RSA PRIVATE KEY-----");
    }

    [Fact]
    public void Should_Not_Log_FileContents()
    {
        // Arrange
        var fileContent = "This is the actual file content with secrets: password=hunter2";
        var data = new Dictionary<string, object>
        {
            ["path"] = "config.json",
            ["content"] = fileContent,  // This should be rejected
            ["file_contents"] = fileContent  // This too
        };

        // Act
        var redacted = _redactor.RedactData(data);

        // Assert
        redacted["path"].Should().Be("config.json");
        redacted.Should().NotContainKey("content",
            because: "file contents must be removed, not just redacted");
        redacted.Should().NotContainKey("file_contents",
            because: "any content field must be removed");
    }

    [Theory]
    [InlineData("normal text without secrets")]
    [InlineData("path=/home/user/project")]
    [InlineData("command=dotnet build")]
    [InlineData("exit_code=0")]
    public void Should_Not_Redact_Normal_Text(string input)
    {
        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Should().Be(input,
            because: "non-sensitive text should not be modified");
    }

    [Theory]
    [InlineData("\t", "\\t")]
    [InlineData("\r", "\\r")]
    [InlineData("\n", "\\n")]
    [InlineData("line1\nline2", "line1\\nline2")]
    public void Should_Escape_SpecialCharacters(string input, string expectedContains)
    {
        // Act
        var result = _redactor.EscapeForJson(input);

        // Assert
        result.Should().Contain(expectedContains);
        result.Should().NotContain("\n");
        result.Should().NotContain("\r");
        result.Should().NotContain("\t");
    }

    [Fact]
    public void Should_Escape_Newlines()
    {
        // Arrange
        var multiline = "line1\nline2\r\nline3";

        // Act
        var result = _redactor.EscapeForJson(multiline);

        // Assert
        result.Should().NotContain("\n");
        result.Should().NotContain("\r");
        result.Should().Contain("\\n");
        result.Should().Contain("\\r");
    }

    [Theory]
    [InlineData("}\n{\"malicious\": true}", "should escape injection attempt")]
    [InlineData("\", \"injected\": \"value", "should escape quote injection")]
    [InlineData("normal\x00null", "should handle null bytes")]
    public void Should_Prevent_LogInjection(string maliciousInput, string because)
    {
        // Act
        var result = _redactor.EscapeForJson(maliciousInput);

        // Assert
        // When properly escaped, parsing the result should not produce extra JSON
        var escaped = $"{{\"value\": \"{result}\"}}";
        var parsed = System.Text.Json.JsonDocument.Parse(escaped);
        
        parsed.RootElement.EnumerateObject().Count().Should().Be(1,
            because);
    }

    [Fact]
    public void Should_Redact_Sensitive_Keys_In_Dictionary()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["username"] = "admin",
            ["password"] = "supersecret",
            ["api_key"] = "sk-12345",
            ["token"] = "jwt.token.here",
            ["credential"] = "some-credential",
            ["normal_field"] = "normal value"
        };

        // Act
        var redacted = _redactor.RedactData(data);

        // Assert
        redacted["username"].Should().Be("admin");
        redacted["password"].Should().Be("[REDACTED]");
        redacted["api_key"].Should().Be("[REDACTED]");
        redacted["token"].Should().Be("[REDACTED]");
        redacted["credential"].Should().Be("[REDACTED]");
        redacted["normal_field"].Should().Be("normal value");
    }

    [Fact]
    public void Should_Handle_Nested_Secrets()
    {
        // Arrange
        var input = "config: {\"password\": \"secret\", \"apiKey\": \"abc123\"}";

        // Act
        var result = _redactor.Redact(input);

        // Assert
        result.Should().NotContain("secret");
        result.Should().NotContain("abc123");
    }
}
```

```
├── IntegrityTests.cs
│   ├── Should_Compute_SHA256_Checksum()
│   ├── Should_Update_Checksum_OnWrite()
│   ├── Should_Detect_Modification()
│   ├── Should_Detect_Truncation()
│   ├── Should_Detect_Insertion()
│   └── Should_Write_ChecksumFile()
```

#### IntegrityTests.cs

```csharp
namespace AgenticCoder.Tests.Unit.Domain.Audit;

using AgenticCoder.Infrastructure.Audit;
using FluentAssertions;
using System.Security.Cryptography;
using System.Text;
using Xunit;

public class IntegrityTests : IDisposable
{
    private readonly AuditIntegrityVerifier _verifier;
    private readonly string _testDir;

    public IntegrityTests()
    {
        _verifier = new AuditIntegrityVerifier();
        _testDir = Path.Combine(Path.GetTempPath(), $"audit_integrity_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    [Fact]
    public void Should_Compute_SHA256_Checksum()
    {
        // Arrange
        var content = "test log content\n";
        var logPath = Path.Combine(_testDir, "test.jsonl");
        File.WriteAllText(logPath, content);

        // Act
        var checksum = _verifier.ComputeChecksum(logPath);

        // Assert
        checksum.Should().NotBeNullOrWhiteSpace();
        checksum.Should().HaveLength(64, 
            because: "SHA-256 produces 64 hex characters");
        checksum.Should().MatchRegex("^[a-f0-9]{64}$",
            because: "checksum should be lowercase hex");

        // Verify manually
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        var expectedChecksum = Convert.ToHexString(hash).ToLowerInvariant();
        
        checksum.Should().Be(expectedChecksum);
    }

    [Fact]
    public async Task Should_Update_Checksum_OnWrite()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "update_test.jsonl");
        var checksumPath = logPath + ".sha256";
        var writer = new FileAuditWriter(_testDir, new AuditConfiguration());

        // Act - write first event
        await writer.WriteAsync(CreateTestEvent("event1"));
        var checksum1 = await File.ReadAllTextAsync(checksumPath);

        // Act - write second event
        await writer.WriteAsync(CreateTestEvent("event2"));
        var checksum2 = await File.ReadAllTextAsync(checksumPath);

        await writer.DisposeAsync();

        // Assert
        checksum1.Should().NotBeNullOrWhiteSpace();
        checksum2.Should().NotBeNullOrWhiteSpace();
        checksum1.Should().NotBe(checksum2,
            because: "checksum should change after each write");
    }

    [Fact]
    public void Should_Detect_Modification()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "modify_test.jsonl");
        var checksumPath = logPath + ".sha256";
        
        var originalContent = "{\"event\":\"test1\"}\n{\"event\":\"test2\"}\n";
        File.WriteAllText(logPath, originalContent);
        
        var checksum = _verifier.ComputeChecksum(logPath);
        File.WriteAllText(checksumPath, checksum);

        // Verify initial state is valid
        _verifier.Verify(logPath).Should().BeTrue();

        // Act - modify content
        File.WriteAllText(logPath, "{\"event\":\"MODIFIED\"}\n{\"event\":\"test2\"}\n");

        // Assert
        var result = _verifier.Verify(logPath);
        result.Should().BeFalse(
            because: "modification should be detected");
    }

    [Fact]
    public void Should_Detect_Truncation()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "truncate_test.jsonl");
        var checksumPath = logPath + ".sha256";
        
        var originalContent = "{\"event\":\"test1\"}\n{\"event\":\"test2\"}\n{\"event\":\"test3\"}\n";
        File.WriteAllText(logPath, originalContent);
        
        var checksum = _verifier.ComputeChecksum(logPath);
        File.WriteAllText(checksumPath, checksum);

        // Verify initial state
        _verifier.Verify(logPath).Should().BeTrue();

        // Act - truncate file
        File.WriteAllText(logPath, "{\"event\":\"test1\"}\n");

        // Assert
        _verifier.Verify(logPath).Should().BeFalse(
            because: "truncation should be detected");
    }

    [Fact]
    public void Should_Detect_Insertion()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "insert_test.jsonl");
        var checksumPath = logPath + ".sha256";
        
        var originalContent = "{\"event\":\"test1\"}\n{\"event\":\"test2\"}\n";
        File.WriteAllText(logPath, originalContent);
        
        var checksum = _verifier.ComputeChecksum(logPath);
        File.WriteAllText(checksumPath, checksum);

        // Verify initial state
        _verifier.Verify(logPath).Should().BeTrue();

        // Act - insert content in middle
        var modifiedContent = "{\"event\":\"test1\"}\n{\"event\":\"INSERTED\"}\n{\"event\":\"test2\"}\n";
        File.WriteAllText(logPath, modifiedContent);

        // Assert
        _verifier.Verify(logPath).Should().BeFalse(
            because: "insertion should be detected");
    }

    [Fact]
    public void Should_Write_ChecksumFile()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "checksum_file_test.jsonl");
        var checksumPath = logPath + ".sha256";
        
        File.WriteAllText(logPath, "{\"event\":\"test\"}\n");

        // Act
        _verifier.WriteChecksumFile(logPath);

        // Assert
        File.Exists(checksumPath).Should().BeTrue();
        
        var savedChecksum = File.ReadAllText(checksumPath).Trim();
        var computedChecksum = _verifier.ComputeChecksum(logPath);
        
        savedChecksum.Should().Be(computedChecksum);
    }

    [Fact]
    public void Should_Verify_Valid_Log()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "valid_test.jsonl");
        var content = "{\"event\":\"test1\"}\n{\"event\":\"test2\"}\n";
        File.WriteAllText(logPath, content);
        _verifier.WriteChecksumFile(logPath);

        // Act
        var result = _verifier.Verify(logPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_Detailed_VerificationResult()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "detailed_test.jsonl");
        File.WriteAllText(logPath, "{\"event\":\"test\"}\n");
        _verifier.WriteChecksumFile(logPath);

        // Tamper with file
        File.AppendAllText(logPath, "{\"event\":\"tampered\"}\n");

        // Act
        var result = _verifier.VerifyWithDetails(logPath);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ExpectedChecksum.Should().NotBeNullOrWhiteSpace();
        result.ActualChecksum.Should().NotBeNullOrWhiteSpace();
        result.ExpectedChecksum.Should().NotBe(result.ActualChecksum);
        result.ErrorMessage.Should().Contain("mismatch");
    }

    private static AuditEvent CreateTestEvent(string id)
    {
        return new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = new EventId($"evt_{id}"),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.FileRead,
            Severity = AuditSeverity.Info,
            Source = "Test",
            OperatingMode = "local_only",
            Data = new Dictionary<string, object> { ["id"] = id }.AsReadOnly()
        };
    }
}
```

```
└── RotationTests.cs
    ├── Should_Rotate_OnSizeLimit()
    ├── Should_Create_NewFile()
    ├── Should_Preserve_Permissions()
    ├── Should_Be_Atomic()
    ├── Should_Delete_ExpiredLogs()
    └── Should_Respect_StorageLimit()
```

#### RotationTests.cs

```csharp
namespace AgenticCoder.Tests.Unit.Domain.Audit;

using AgenticCoder.Infrastructure.Audit;
using FluentAssertions;
using Xunit;

public class RotationTests : IDisposable
{
    private readonly string _testDir;
    private readonly AuditLogRotator _rotator;

    public RotationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"audit_rotation_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _rotator = new AuditLogRotator(new AuditConfiguration
        {
            MaxFileSize = 1024, // 1KB for testing
            RetentionDays = 90,
            MaxTotalStorage = 10 * 1024 // 10KB
        });
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    [Fact]
    public async Task Should_Rotate_OnSizeLimit()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "session_001.jsonl");
        
        // Create file larger than limit
        var largeContent = string.Join("\n", 
            Enumerable.Range(0, 100).Select(i => $"{{\"event\":{i}}}"));
        await File.WriteAllTextAsync(logPath, largeContent);

        var originalSize = new FileInfo(logPath).Length;
        originalSize.Should().BeGreaterThan(1024);

        // Act
        var result = await _rotator.RotateIfNeededAsync(logPath);

        // Assert
        result.RotationOccurred.Should().BeTrue();
        File.Exists(logPath + ".1").Should().BeTrue(
            because: "rotated file should exist with .1 suffix");
    }

    [Fact]
    public async Task Should_Create_NewFile()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "session_002.jsonl");
        var largeContent = new string('x', 2000); // Larger than 1KB limit
        await File.WriteAllTextAsync(logPath, largeContent);

        // Act
        var writer = new FileAuditWriter(_testDir, new AuditConfiguration { MaxFileSize = 1024 });
        await writer.WriteAsync(CreateTestEvent());

        // Assert - new file should be created for new writes
        // (Implementation detail: rotation creates new current file)
    }

    [Fact]
    public async Task Should_Preserve_Permissions()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "session_perm.jsonl");
        await File.WriteAllTextAsync(logPath, new string('x', 2000));

        // Set permissions
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(logPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        // Act
        await _rotator.RotateIfNeededAsync(logPath);

        // Assert
        var rotatedPath = logPath + ".1";
        File.Exists(rotatedPath).Should().BeTrue();

        if (!OperatingSystem.IsWindows())
        {
            var mode = File.GetUnixFileMode(rotatedPath);
            mode.Should().HaveFlag(UnixFileMode.UserRead);
            mode.Should().HaveFlag(UnixFileMode.UserWrite);
            mode.Should().NotHaveFlag(UnixFileMode.OtherRead);
            mode.Should().NotHaveFlag(UnixFileMode.OtherWrite);
        }
    }

    [Fact]
    public async Task Should_Be_Atomic()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "session_atomic.jsonl");
        var events = Enumerable.Range(0, 50)
            .Select(i => $"{{\"event\":{i}}}\n")
            .ToList();
        
        await File.WriteAllTextAsync(logPath, string.Join("", events));

        // Count lines before
        var linesBefore = File.ReadAllLines(logPath).Length;

        // Act
        await _rotator.RotateIfNeededAsync(logPath);

        // Assert - rotated file should have all original lines
        var rotatedPath = logPath + ".1";
        if (File.Exists(rotatedPath))
        {
            var linesAfter = File.ReadAllLines(rotatedPath).Length;
            linesAfter.Should().Be(linesBefore,
                because: "rotation must not lose events");
        }
    }

    [Fact]
    public async Task Should_Delete_ExpiredLogs()
    {
        // Arrange - create old log files
        var oldLogPath = Path.Combine(_testDir, "2023-01-01T00-00-00Z_sess_old.jsonl");
        await File.WriteAllTextAsync(oldLogPath, "{\"event\":\"old\"}");
        
        // Make file appear old
        File.SetLastWriteTime(oldLogPath, DateTime.Now.AddDays(-100));

        var recentLogPath = Path.Combine(_testDir, "2024-01-01T00-00-00Z_sess_recent.jsonl");
        await File.WriteAllTextAsync(recentLogPath, "{\"event\":\"recent\"}");

        // Act
        var deleted = await _rotator.CleanupExpiredLogsAsync(_testDir, retentionDays: 90);

        // Assert
        deleted.Should().Contain(oldLogPath);
        File.Exists(oldLogPath).Should().BeFalse(
            because: "logs older than 90 days should be deleted");
        File.Exists(recentLogPath).Should().BeTrue(
            because: "recent logs should be kept");
    }

    [Fact]
    public async Task Should_Respect_StorageLimit()
    {
        // Arrange - create files exceeding limit
        for (int i = 0; i < 5; i++)
        {
            var path = Path.Combine(_testDir, $"session_{i:D3}.jsonl");
            await File.WriteAllTextAsync(path, new string('x', 3000)); // 3KB each = 15KB total
            File.SetLastWriteTime(path, DateTime.Now.AddDays(-i)); // Older files first
        }

        // Act - enforce 10KB limit
        var deleted = await _rotator.EnforceStorageLimitAsync(_testDir, maxBytes: 10 * 1024);

        // Assert
        deleted.Should().NotBeEmpty(
            because: "oldest files should be deleted to meet storage limit");
        
        var remainingSize = Directory.GetFiles(_testDir, "*.jsonl")
            .Sum(f => new FileInfo(f).Length);
        
        remainingSize.Should().BeLessThanOrEqualTo(10 * 1024,
            because: "total storage should not exceed limit");
    }

    [Fact]
    public async Task Should_Number_RotatedFiles_Sequentially()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "session_seq.jsonl");
        
        // Create and rotate multiple times
        for (int i = 0; i < 3; i++)
        {
            await File.WriteAllTextAsync(logPath, new string('x', 2000));
            await _rotator.RotateIfNeededAsync(logPath);
        }

        // Assert
        File.Exists(logPath + ".1").Should().BeTrue();
        File.Exists(logPath + ".2").Should().BeTrue();
        File.Exists(logPath + ".3").Should().BeTrue();
    }

    [Fact]
    public async Task Should_Log_Deletion_Before_Delete()
    {
        // Arrange
        var oldLogPath = Path.Combine(_testDir, "old_session.jsonl");
        await File.WriteAllTextAsync(oldLogPath, "{\"event\":\"old\"}");
        File.SetLastWriteTime(oldLogPath, DateTime.Now.AddDays(-100));

        var deletionLog = new List<string>();
        _rotator.OnBeforeDelete += (path) => deletionLog.Add(path);

        // Act
        await _rotator.CleanupExpiredLogsAsync(_testDir, retentionDays: 90);

        // Assert
        deletionLog.Should().Contain(oldLogPath,
            because: "deletion should be logged before execution");
    }

    private static AuditEvent CreateTestEvent()
    {
        return new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.FileRead,
            Severity = AuditSeverity.Info,
            Source = "Test",
            OperatingMode = "local_only",
            Data = new Dictionary<string, object>().AsReadOnly()
        };
    }
}
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
```

#### AuditStorageTests.cs

```csharp
namespace AgenticCoder.Tests.Integration.Audit;

using AgenticCoder.Infrastructure.Audit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;

[Collection("Integration")]
public class AuditStorageTests : IClassFixture<IntegrationTestFixture>, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly string _testDir;

    public AuditStorageTests(IntegrationTestFixture fixture)
    {
        _services = fixture.Services;
        _testDir = Path.Combine(Path.GetTempPath(), $"audit_storage_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    [Fact]
    public async Task Should_CreateAuditDirectory()
    {
        // Arrange
        var auditDir = Path.Combine(_testDir, ".agent", "logs", "audit");
        Directory.Exists(auditDir).Should().BeFalse();

        // Act
        var writer = new FileAuditWriter(auditDir, new AuditConfiguration());
        await writer.WriteAsync(CreateTestEvent());
        await writer.DisposeAsync();

        // Assert
        Directory.Exists(auditDir).Should().BeTrue(
            because: "audit directory should be created automatically");
    }

    [Fact]
    public async Task Should_WriteToCorrectLocation()
    {
        // Arrange
        var auditDir = Path.Combine(_testDir, "custom_audit");
        var config = new AuditConfiguration { Directory = auditDir };
        
        // Act
        var writer = new FileAuditWriter(auditDir, config);
        await writer.WriteAsync(CreateTestEvent());
        await writer.DisposeAsync();

        // Assert
        var files = Directory.GetFiles(auditDir, "*.jsonl");
        files.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_UseJsonlFormat()
    {
        // Arrange
        var auditDir = Path.Combine(_testDir, "jsonl_test");
        Directory.CreateDirectory(auditDir);

        var writer = new FileAuditWriter(auditDir, new AuditConfiguration());
        
        // Act
        await writer.WriteAsync(CreateTestEvent());
        await writer.WriteAsync(CreateTestEvent());
        await writer.WriteAsync(CreateTestEvent());
        await writer.DisposeAsync();

        // Assert
        var logFile = Directory.GetFiles(auditDir, "*.jsonl").First();
        var lines = await File.ReadAllLinesAsync(logFile);
        
        lines.Should().HaveCount(3);
        
        foreach (var line in lines)
        {
            // Each line should be valid JSON
            var action = () => JsonDocument.Parse(line);
            action.Should().NotThrow(
                because: "each line must be valid JSON (JSONL format)");
            
            // No multi-line JSON
            line.Should().NotContain("\n");
        }
    }

    [Fact]
    public async Task Should_CreateFilePerSession()
    {
        // Arrange
        var auditDir = Path.Combine(_testDir, "session_test");
        Directory.CreateDirectory(auditDir);

        // Act - create two sessions
        var session1 = SessionId.New();
        var writer1 = new FileAuditWriter(auditDir, new AuditConfiguration(), session1);
        await writer1.WriteAsync(CreateTestEvent(session1));
        await writer1.DisposeAsync();

        var session2 = SessionId.New();
        var writer2 = new FileAuditWriter(auditDir, new AuditConfiguration(), session2);
        await writer2.WriteAsync(CreateTestEvent(session2));
        await writer2.DisposeAsync();

        // Assert
        var files = Directory.GetFiles(auditDir, "*.jsonl");
        files.Should().HaveCount(2,
            because: "each session should have its own log file");
    }

    [Fact]
    public async Task Should_IncludeTimestampInFilename()
    {
        // Arrange
        var auditDir = Path.Combine(_testDir, "filename_test");
        Directory.CreateDirectory(auditDir);

        // Act
        var writer = new FileAuditWriter(auditDir, new AuditConfiguration());
        await writer.WriteAsync(CreateTestEvent());
        await writer.DisposeAsync();

        // Assert
        var logFile = Directory.GetFiles(auditDir, "*.jsonl").First();
        var filename = Path.GetFileName(logFile);
        
        // Should match pattern: 2024-01-15T10-30-00Z_sess_xxx.jsonl
        filename.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}-\d{2}-\d{2}Z_sess_[a-zA-Z0-9]+\.jsonl$",
            because: "filename must include ISO timestamp and session ID");
    }

    [SkippableFact]
    public async Task Should_SetCorrectPermissions_Unix()
    {
        Skip.If(OperatingSystem.IsWindows());

        // Arrange
        var auditDir = Path.Combine(_testDir, "perms_unix");
        Directory.CreateDirectory(auditDir);

        // Act
        var writer = new FileAuditWriter(auditDir, new AuditConfiguration());
        await writer.WriteAsync(CreateTestEvent());
        await writer.DisposeAsync();

        // Assert
        var logFile = Directory.GetFiles(auditDir, "*.jsonl").First();
        var mode = File.GetUnixFileMode(logFile);
        
        // Should be 0600 (owner read/write only)
        mode.Should().HaveFlag(UnixFileMode.UserRead);
        mode.Should().HaveFlag(UnixFileMode.UserWrite);
        mode.Should().NotHaveFlag(UnixFileMode.GroupRead);
        mode.Should().NotHaveFlag(UnixFileMode.GroupWrite);
        mode.Should().NotHaveFlag(UnixFileMode.OtherRead);
        mode.Should().NotHaveFlag(UnixFileMode.OtherWrite);
    }

    [SkippableFact]
    public async Task Should_SetCorrectPermissions_Windows()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        // Arrange
        var auditDir = Path.Combine(_testDir, "perms_windows");
        Directory.CreateDirectory(auditDir);

        // Act
        var writer = new FileAuditWriter(auditDir, new AuditConfiguration());
        await writer.WriteAsync(CreateTestEvent());
        await writer.DisposeAsync();

        // Assert - Windows uses ACLs, verify current user has access
        var logFile = Directory.GetFiles(auditDir, "*.jsonl").First();
        var fi = new FileInfo(logFile);
        
        // File should be accessible to current user
        fi.Exists.Should().BeTrue();
        
        // ACL verification would require System.Security.AccessControl
    }

    [Fact]
    public async Task Should_HandleConcurrentWrites()
    {
        // Arrange
        var auditDir = Path.Combine(_testDir, "concurrent_test");
        Directory.CreateDirectory(auditDir);
        var writer = new FileAuditWriter(auditDir, new AuditConfiguration());

        // Act - write concurrently from multiple tasks
        var tasks = Enumerable.Range(0, 100)
            .Select(i => writer.WriteAsync(CreateTestEvent()))
            .ToList();

        await Task.WhenAll(tasks);
        await writer.DisposeAsync();

        // Assert
        var logFile = Directory.GetFiles(auditDir, "*.jsonl").First();
        var lines = await File.ReadAllLinesAsync(logFile);
        
        lines.Should().HaveCount(100,
            because: "all concurrent writes should succeed");
        
        // Each line should be valid JSON (no corruption from concurrent access)
        foreach (var line in lines)
        {
            var action = () => JsonDocument.Parse(line);
            action.Should().NotThrow();
        }
    }

    [Fact]
    public async Task Should_FlushImmediately()
    {
        // Arrange
        var auditDir = Path.Combine(_testDir, "flush_test");
        Directory.CreateDirectory(auditDir);
        var writer = new FileAuditWriter(auditDir, new AuditConfiguration());

        // Act
        await writer.WriteAsync(CreateTestEvent());
        // Don't dispose yet - check file immediately

        // Assert
        var logFile = Directory.GetFiles(auditDir, "*.jsonl").First();
        var content = await File.ReadAllTextAsync(logFile);
        
        content.Should().NotBeEmpty(
            because: "events should be flushed immediately, not buffered");

        await writer.DisposeAsync();
    }

    private static AuditEvent CreateTestEvent(SessionId? sessionId = null)
    {
        return new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = sessionId ?? SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.FileRead,
            Severity = AuditSeverity.Info,
            Source = "Test",
            OperatingMode = "local_only",
            Data = new Dictionary<string, object>().AsReadOnly()
        };
    }
}
```

```
├── AuditConfigTests.cs
│   ├── Should_LoadFromConfig()
│   ├── Should_UseDefaultValues()
│   ├── Should_ValidateConfiguration()
│   ├── Should_ApplyLogLevel()
│   └── Should_ApplyRetentionSettings()
```

#### AuditConfigTests.cs

```csharp
namespace AgenticCoder.Tests.Integration.Audit;

using AgenticCoder.Application.Configuration;
using AgenticCoder.Infrastructure.Audit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Collection("Integration")]
public class AuditConfigTests : IClassFixture<IntegrationTestFixture>, IDisposable
{
    private readonly string _testDir;

    public AuditConfigTests(IntegrationTestFixture fixture)
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"audit_config_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    [Fact]
    public void Should_LoadFromConfig()
    {
        // Arrange
        var agentConfigPath = Path.Combine(_testDir, "agent-config.yml");
        File.WriteAllText(agentConfigPath, @"
audit:
  enabled: true
  directory: custom_audit_dir
  rotation:
    size_mb: 25
  retention:
    days: 30
    max_storage_mb: 1000
  integrity:
    enabled: true
    algorithm: SHA512
");

        var config = new ConfigurationBuilder()
            .AddAgentConfig(agentConfigPath)
            .Build();

        // Act
        var services = new ServiceCollection();
        services.AddAuditServices(config);
        var provider = services.BuildServiceProvider();
        var auditConfig = provider.GetRequiredService<IAuditConfiguration>();

        // Assert
        auditConfig.Directory.Should().Contain("custom_audit_dir");
        auditConfig.RotationSizeMb.Should().Be(25);
        auditConfig.RetentionDays.Should().Be(30);
        auditConfig.MaxStorageMb.Should().Be(1000);
        auditConfig.IntegrityAlgorithm.Should().Be("SHA512");
    }

    [Fact]
    public void Should_UseDefaultValues()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddAuditServices(config);
        var provider = services.BuildServiceProvider();
        var auditConfig = provider.GetRequiredService<IAuditConfiguration>();

        // Assert
        auditConfig.Enabled.Should().BeTrue(
            because: "audit is enabled by default");
        auditConfig.RotationSizeMb.Should().Be(10,
            because: "default rotation size is 10MB");
        auditConfig.RetentionDays.Should().Be(90,
            because: "default retention is 90 days");
        auditConfig.MaxStorageMb.Should().Be(500,
            because: "default max storage is 500MB");
        auditConfig.EnableIntegrityChecks.Should().BeTrue(
            because: "integrity checks are enabled by default");
        auditConfig.IntegrityAlgorithm.Should().Be("SHA256",
            because: "SHA256 is the default algorithm");
    }

    [Fact]
    public void Should_ValidateConfiguration()
    {
        // Arrange - invalid configuration
        var agentConfigPath = Path.Combine(_testDir, "invalid-config.yml");
        File.WriteAllText(agentConfigPath, @"
audit:
  rotation:
    size_mb: -5
  retention:
    days: 0
    max_storage_mb: -100
");

        var config = new ConfigurationBuilder()
            .AddAgentConfig(agentConfigPath)
            .Build();

        // Act & Assert
        var services = new ServiceCollection();
        var action = () => services.AddAuditServices(config);

        action.Should().Throw<ConfigurationValidationException>(
            because: "invalid values should be rejected during configuration");
    }

    [Fact]
    public async Task Should_ApplyLogLevel()
    {
        // Arrange
        var auditDir = Path.Combine(_testDir, "loglevel_test");
        Directory.CreateDirectory(auditDir);

        var config = new AuditConfiguration
        {
            Directory = auditDir,
            MinimumSeverity = AuditSeverity.Warning
        };

        // Act
        var logger = new AuditLogger(
            new FileAuditWriter(auditDir, config),
            config
        );

        await logger.LogInfoAsync("session_start", new { });       // Should be filtered
        await logger.LogWarningAsync("constraint_violation", new { });  // Should be logged
        await logger.LogErrorAsync("file_error", new { });         // Should be logged
        await logger.DisposeAsync();

        // Assert
        var logFile = Directory.GetFiles(auditDir, "*.jsonl").First();
        var lines = await File.ReadAllLinesAsync(logFile);

        lines.Should().HaveCount(2,
            because: "info events should be filtered when minimum is Warning");
    }

    [Fact]
    public async Task Should_ApplyRetentionSettings()
    {
        // Arrange
        var auditDir = Path.Combine(_testDir, "retention_test");
        Directory.CreateDirectory(auditDir);

        // Create files with old timestamps
        var oldFile = Path.Combine(auditDir, "2023-01-01T00-00-00Z_sess_old.jsonl");
        var recentFile = Path.Combine(auditDir, $"{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ssZ}_sess_new.jsonl");

        await File.WriteAllTextAsync(oldFile, "{\"test\":true}");
        await File.WriteAllTextAsync(recentFile, "{\"test\":true}");

        // Set old file's timestamp
        File.SetCreationTimeUtc(oldFile, DateTime.UtcNow.AddDays(-100));

        var config = new AuditConfiguration
        {
            Directory = auditDir,
            RetentionDays = 90
        };

        // Act
        var retentionManager = new AuditRetentionManager(auditDir, config);
        await retentionManager.CleanupExpiredLogsAsync();

        // Assert
        File.Exists(oldFile).Should().BeFalse(
            because: "files older than retention period should be deleted");
        File.Exists(recentFile).Should().BeTrue(
            because: "recent files should be kept");
    }
}
```

```
├── AuditRecoveryTests.cs
│   ├── Should_RecoverFromCrash()
│   ├── Should_DetectPartialWrite()
│   ├── Should_HandleDiskFull()
│   └── Should_RetryOnFailure()
```

#### AuditRecoveryTests.cs

```csharp
namespace AgenticCoder.Tests.Integration.Audit;

using AgenticCoder.Infrastructure.Audit;
using FluentAssertions;
using System.IO;
using System.Text.Json;
using Xunit;

[Collection("Integration")]
public class AuditRecoveryTests : IClassFixture<IntegrationTestFixture>, IDisposable
{
    private readonly string _testDir;

    public AuditRecoveryTests(IntegrationTestFixture fixture)
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"audit_recovery_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    [Fact]
    public async Task Should_RecoverFromCrash()
    {
        // Arrange - simulate a crash by leaving a partial file
        var auditDir = Path.Combine(_testDir, "crash_recovery");
        Directory.CreateDirectory(auditDir);

        var crashedFile = Path.Combine(auditDir, "2024-01-15T10-00-00Z_sess_crashed.jsonl");
        
        // Write some valid events, then simulate incomplete write
        var validEvent1 = CreateValidEventJson(1);
        var validEvent2 = CreateValidEventJson(2);
        var partialEvent = "{\"eventId\":\"partial\",\"timestamp\":\"2024-01-15T10:00:03Z\"";  // Incomplete
        
        await File.WriteAllTextAsync(crashedFile, 
            $"{validEvent1}\n{validEvent2}\n{partialEvent}");

        // Act
        var recoveryManager = new AuditRecoveryManager(auditDir, new AuditConfiguration());
        var result = await recoveryManager.RecoverAsync();

        // Assert
        result.RecoveredFiles.Should().Be(1);
        result.CorruptedEntriesFound.Should().Be(1);
        result.ValidEntriesRecovered.Should().Be(2);

        // Corrupted data should be moved to a .corrupt file
        File.Exists(crashedFile + ".corrupt").Should().BeTrue();
        
        // Original file should only contain valid entries
        var recoveredContent = await File.ReadAllLinesAsync(crashedFile);
        recoveredContent.Should().HaveCount(2);
        
        foreach (var line in recoveredContent)
        {
            var action = () => JsonDocument.Parse(line);
            action.Should().NotThrow();
        }
    }

    [Fact]
    public async Task Should_DetectPartialWrite()
    {
        // Arrange
        var auditDir = Path.Combine(_testDir, "partial_write");
        Directory.CreateDirectory(auditDir);

        var logFile = Path.Combine(auditDir, "2024-01-15T10-00-00Z_sess_partial.jsonl");
        
        // Various types of partial writes
        var testCases = new[]
        {
            "{\"eventId\":\"1\",\"incomplete",                    // Truncated mid-field
            "{\"eventId\":\"2\"}garbage",                          // Extra garbage after valid JSON
            "not json at all",                                     // Not JSON
            "{\"eventId\":\"3\",\"data\":{\"nested\":\"incomplete", // Deeply nested incomplete
            ""                                                     // Empty line
        };

        await File.WriteAllLinesAsync(logFile, testCases);

        // Act
        var validator = new AuditLogValidator(new AuditConfiguration());
        var result = await validator.ValidateFileAsync(logFile);

        // Assert
        result.IsValid.Should().BeFalse();
        result.TotalLines.Should().Be(5);
        result.InvalidLines.Should().Be(4);  // 4 invalid, 1 valid (the second one, minus garbage)
        result.Errors.Should().Contain(e => e.Contains("line 1"));
        result.Errors.Should().Contain(e => e.Contains("line 3"));
        result.Errors.Should().Contain(e => e.Contains("line 4"));
    }

    [Fact]
    public async Task Should_HandleDiskFull()
    {
        // Arrange
        var auditDir = Path.Combine(_testDir, "disk_full");
        Directory.CreateDirectory(auditDir);

        // Create a configuration with very low storage limit
        var config = new AuditConfiguration
        {
            Directory = auditDir,
            MaxStorageMb = 1  // 1MB limit
        };

        var writer = new FileAuditWriter(auditDir, config);

        // Act - try to write more than the limit
        var eventsWritten = 0;
        AuditStorageException? storageException = null;

        try
        {
            for (int i = 0; i < 100000; i++)
            {
                await writer.WriteAsync(CreateLargeTestEvent(i));
                eventsWritten++;
            }
        }
        catch (AuditStorageException ex)
        {
            storageException = ex;
        }
        finally
        {
            await writer.DisposeAsync();
        }

        // Assert
        storageException.Should().NotBeNull(
            because: "storage limit should be enforced");
        storageException!.Message.Should().Contain("storage limit",
            because: "error message should be descriptive");
        eventsWritten.Should().BeGreaterThan(0,
            because: "some events should have been written before limit");

        // Verify graceful degradation - existing events preserved
        var files = Directory.GetFiles(auditDir, "*.jsonl");
        files.Should().NotBeEmpty();
        
        foreach (var file in files)
        {
            var lines = await File.ReadAllLinesAsync(file);
            foreach (var line in lines)
            {
                var action = () => JsonDocument.Parse(line);
                action.Should().NotThrow(
                    because: "all written events should be valid");
            }
        }
    }

    [Fact]
    public async Task Should_RetryOnFailure()
    {
        // Arrange
        var auditDir = Path.Combine(_testDir, "retry_test");
        Directory.CreateDirectory(auditDir);

        var config = new AuditConfiguration
        {
            Directory = auditDir,
            RetryAttempts = 3,
            RetryDelayMs = 100
        };

        // Create a writer with a flaky underlying stream
        var flakyStream = new FlakyStreamWrapper(
            new FileStream(
                Path.Combine(auditDir, "test.jsonl"),
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read
            ),
            failCount: 2  // Fail first 2 attempts, succeed on 3rd
        );

        var writer = new FileAuditWriter(flakyStream, config);

        // Act
        var result = await writer.WriteAsync(CreateTestEvent());
        await writer.DisposeAsync();

        // Assert
        result.Success.Should().BeTrue(
            because: "write should succeed after retries");
        result.AttemptCount.Should().Be(3,
            because: "it should have taken 3 attempts");

        // Verify event was actually written
        var content = await File.ReadAllTextAsync(Path.Combine(auditDir, "test.jsonl"));
        content.Should().NotBeEmpty();
    }

    private static string CreateValidEventJson(int index)
    {
        var evt = new
        {
            schemaVersion = "1.0",
            eventId = $"evt_{index}",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            sessionId = "sess_test",
            correlationId = "corr_test",
            eventType = "file_read",
            severity = "info",
            source = "Test",
            operatingMode = "local_only",
            data = new { }
        };
        return JsonSerializer.Serialize(evt);
    }

    private static AuditEvent CreateTestEvent()
    {
        return new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.FileRead,
            Severity = AuditSeverity.Info,
            Source = "Test",
            OperatingMode = "local_only",
            Data = new Dictionary<string, object>().AsReadOnly()
        };
    }

    private static AuditEvent CreateLargeTestEvent(int index)
    {
        return new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.FileRead,
            Severity = AuditSeverity.Info,
            Source = "Test",
            OperatingMode = "local_only",
            Data = new Dictionary<string, object>
            {
                ["index"] = index,
                ["padding"] = new string('x', 500)
            }.AsReadOnly()
        };
    }
}

/// <summary>
/// Test helper that simulates I/O failures.
/// </summary>
internal class FlakyStreamWrapper : Stream
{
    private readonly Stream _inner;
    private int _failCount;
    private int _writeAttempts = 0;

    public FlakyStreamWrapper(Stream inner, int failCount)
    {
        _inner = inner;
        _failCount = failCount;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _writeAttempts++;
        if (_writeAttempts <= _failCount)
        {
            throw new IOException($"Simulated failure {_writeAttempts} of {_failCount}");
        }
        _inner.Write(buffer, offset, count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        _writeAttempts++;
        if (_writeAttempts <= _failCount)
        {
            throw new IOException($"Simulated failure {_writeAttempts} of {_failCount}");
        }
        await _inner.WriteAsync(buffer, offset, count, cancellationToken);
    }

    // Required Stream overrides
    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => _inner.CanWrite;
    public override long Length => _inner.Length;
    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }
    public override void Flush() => _inner.Flush();
    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
    public override void SetLength(long value) => _inner.SetLength(value);
    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();
        base.Dispose(disposing);
    }
}
```

```
└── CLIIntegrationTests.cs
    ├── AuditList_ShouldShowSessions()
    ├── AuditShow_ShouldDisplayEvents()
    ├── AuditSearch_ShouldFindEvents()
    ├── AuditVerify_ShouldValidateIntegrity()
    ├── AuditExport_ShouldCreateFile()
    └── AuditStats_ShouldShowUsage()
```

#### CLIIntegrationTests.cs

```csharp
namespace AgenticCoder.Tests.Integration.Audit;

using AgenticCoder.CLI;
using AgenticCoder.Infrastructure.Audit;
using FluentAssertions;
using System.Text.Json;
using Xunit;

[Collection("Integration")]
public class CLIIntegrationTests : IClassFixture<IntegrationTestFixture>, IDisposable
{
    private readonly string _testDir;
    private readonly CLITestHarness _cli;

    public CLIIntegrationTests(IntegrationTestFixture fixture)
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"audit_cli_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _cli = new CLITestHarness(_testDir);
        
        // Seed with test audit data
        SeedTestData().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _cli.Dispose();
        try { Directory.Delete(_testDir, true); } catch { }
    }

    private async Task SeedTestData()
    {
        var auditDir = Path.Combine(_testDir, ".agent", "logs", "audit");
        Directory.CreateDirectory(auditDir);

        // Create two sessions with events
        var session1 = "sess_abc123";
        var session2 = "sess_def456";

        var file1 = Path.Combine(auditDir, $"2024-01-15T10-00-00Z_{session1}.jsonl");
        var file2 = Path.Combine(auditDir, $"2024-01-15T11-00-00Z_{session2}.jsonl");

        var events1 = new[]
        {
            CreateEventJson(session1, "session_start", "info"),
            CreateEventJson(session1, "file_read", "info", "/src/main.cs"),
            CreateEventJson(session1, "file_write", "info", "/src/main.cs"),
            CreateEventJson(session1, "session_end", "info")
        };

        var events2 = new[]
        {
            CreateEventJson(session2, "session_start", "info"),
            CreateEventJson(session2, "constraint_violation", "warning", "/system/hosts"),
            CreateEventJson(session2, "operation_blocked", "error", "/etc/passwd"),
            CreateEventJson(session2, "session_end", "info")
        };

        await File.WriteAllLinesAsync(file1, events1);
        await File.WriteAllLinesAsync(file2, events2);
    }

    [Fact]
    public async Task AuditList_ShouldShowSessions()
    {
        // Act
        var result = await _cli.RunAsync("audit", "list");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("sess_abc123");
        result.Output.Should().Contain("sess_def456");
        result.Output.Should().Contain("2024-01-15");
        result.Output.Should().Contain("Events:");
    }

    [Fact]
    public async Task AuditList_WithDateFilter_ShouldFilterResults()
    {
        // Act
        var result = await _cli.RunAsync("audit", "list", "--after", "2024-01-15T10:30:00Z");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("sess_def456");
        result.Output.Should().NotContain("sess_abc123");
    }

    [Fact]
    public async Task AuditShow_ShouldDisplayEvents()
    {
        // Act
        var result = await _cli.RunAsync("audit", "show", "sess_abc123");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("session_start");
        result.Output.Should().Contain("file_read");
        result.Output.Should().Contain("file_write");
        result.Output.Should().Contain("session_end");
        result.Output.Should().Contain("/src/main.cs");
    }

    [Fact]
    public async Task AuditShow_WithEventFilter_ShouldFilterResults()
    {
        // Act
        var result = await _cli.RunAsync("audit", "show", "sess_abc123", "--type", "file_read");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("file_read");
        result.Output.Should().NotContain("session_start");
        result.Output.Should().NotContain("file_write");
    }

    [Fact]
    public async Task AuditSearch_ShouldFindEvents()
    {
        // Act - search for constraint violations
        var result = await _cli.RunAsync("audit", "search", "--type", "constraint_violation");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("sess_def456");
        result.Output.Should().Contain("constraint_violation");
        result.Output.Should().Contain("/system/hosts");
    }

    [Fact]
    public async Task AuditSearch_ByPath_ShouldFindEvents()
    {
        // Act - search for operations on /etc/passwd
        var result = await _cli.RunAsync("audit", "search", "--path", "/etc/passwd");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("operation_blocked");
        result.Output.Should().Contain("/etc/passwd");
    }

    [Fact]
    public async Task AuditSearch_BySeverity_ShouldFindEvents()
    {
        // Act - search for errors only
        var result = await _cli.RunAsync("audit", "search", "--severity", "error");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("operation_blocked");
        result.Output.Should().NotContain("session_start");
    }

    [Fact]
    public async Task AuditVerify_ShouldValidateIntegrity()
    {
        // Act
        var result = await _cli.RunAsync("audit", "verify");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("verified");
        result.Output.Should().Contain("2 sessions");
        result.Output.Should().Contain("8 events");
        result.Output.Should().NotContain("corrupted");
    }

    [Fact]
    public async Task AuditVerify_WithCorruptedData_ShouldReportErrors()
    {
        // Arrange - corrupt one of the files
        var auditDir = Path.Combine(_testDir, ".agent", "logs", "audit");
        var corruptFile = Path.Combine(auditDir, "2024-01-15T12-00-00Z_sess_corrupt.jsonl");
        await File.WriteAllTextAsync(corruptFile, "{\"incomplete");

        // Act
        var result = await _cli.RunAsync("audit", "verify");

        // Assert
        result.ExitCode.Should().Be(1,
            because: "verification should fail with corrupted data");
        result.Output.Should().Contain("corrupted");
        result.Output.Should().Contain("sess_corrupt");
    }

    [Fact]
    public async Task AuditExport_ShouldCreateFile()
    {
        // Arrange
        var exportPath = Path.Combine(_testDir, "export.json");

        // Act
        var result = await _cli.RunAsync("audit", "export", "--output", exportPath);

        // Assert
        result.ExitCode.Should().Be(0);
        File.Exists(exportPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(exportPath);
        var action = () => JsonDocument.Parse(content);
        action.Should().NotThrow();

        using var doc = JsonDocument.Parse(content);
        doc.RootElement.GetProperty("sessions").GetArrayLength().Should().Be(2);
        doc.RootElement.GetProperty("totalEvents").GetInt32().Should().Be(8);
    }

    [Fact]
    public async Task AuditExport_WithSessionFilter_ShouldExportSelected()
    {
        // Arrange
        var exportPath = Path.Combine(_testDir, "export_single.json");

        // Act
        var result = await _cli.RunAsync("audit", "export", 
            "--session", "sess_abc123", 
            "--output", exportPath);

        // Assert
        result.ExitCode.Should().Be(0);

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(exportPath));
        doc.RootElement.GetProperty("sessions").GetArrayLength().Should().Be(1);
        doc.RootElement.GetProperty("totalEvents").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task AuditStats_ShouldShowUsage()
    {
        // Act
        var result = await _cli.RunAsync("audit", "stats");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Total sessions: 2");
        result.Output.Should().Contain("Total events: 8");
        result.Output.Should().Contain("Storage used:");
        result.Output.Should().Contain("Event types:");
        result.Output.Should().Contain("file_read");
        result.Output.Should().Contain("constraint_violation");
    }

    [Fact]
    public async Task AuditStats_WithVerbose_ShouldShowDetails()
    {
        // Act
        var result = await _cli.RunAsync("audit", "stats", "--verbose");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("session_start: 2");
        result.Output.Should().Contain("file_read: 1");
        result.Output.Should().Contain("file_write: 1");
        result.Output.Should().Contain("constraint_violation: 1");
        result.Output.Should().Contain("operation_blocked: 1");
        result.Output.Should().Contain("session_end: 2");
    }

    [Fact]
    public async Task AuditClean_ShouldRemoveOldLogs()
    {
        // Arrange - add an old file
        var auditDir = Path.Combine(_testDir, ".agent", "logs", "audit");
        var oldFile = Path.Combine(auditDir, "2023-01-01T00-00-00Z_sess_old.jsonl");
        await File.WriteAllTextAsync(oldFile, "{}");
        File.SetCreationTimeUtc(oldFile, DateTime.UtcNow.AddDays(-100));

        // Act
        var result = await _cli.RunAsync("audit", "clean", "--older-than", "90");

        // Assert
        result.ExitCode.Should().Be(0);
        File.Exists(oldFile).Should().BeFalse();
        result.Output.Should().Contain("Removed 1 log file");
    }

    private static string CreateEventJson(
        string sessionId, 
        string eventType, 
        string severity,
        string? path = null)
    {
        var data = path != null 
            ? new { path } 
            : (object)new { };

        var evt = new
        {
            schemaVersion = "1.0",
            eventId = $"evt_{Guid.NewGuid():N}",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            sessionId,
            correlationId = $"corr_{Guid.NewGuid():N}",
            eventType,
            severity,
            source = "Test",
            operatingMode = "local_only",
            data
        };
        return JsonSerializer.Serialize(evt);
    }
}

/// <summary>
/// Test harness for running CLI commands in isolation.
/// </summary>
internal class CLITestHarness : IDisposable
{
    private readonly string _workDir;

    public CLITestHarness(string workDir)
    {
        _workDir = workDir;
    }

    public async Task<CLIResult> RunAsync(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();

        var app = new AgentCLI(output, error, _workDir);
        var exitCode = await app.RunAsync(args);

        return new CLIResult
        {
            ExitCode = exitCode,
            Output = output.ToString(),
            Error = error.ToString()
        };
    }

    public void Dispose() { }
}

internal class CLIResult
{
    public int ExitCode { get; init; }
    public string Output { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
}
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

#### AuditScenarios.cs

```csharp
namespace AgenticCoder.Tests.E2E.Audit;

using AgenticCoder.CLI;
using AgenticCoder.Application.Sessions;
using AgenticCoder.Infrastructure.Audit;
using FluentAssertions;
using System.Text.Json;
using Xunit;

[Collection("E2E")]
[Trait("Category", "E2E")]
public class AuditScenarios : IClassFixture<E2ETestFixture>, IDisposable
{
    private readonly E2ETestFixture _fixture;
    private readonly string _testDir;
    private readonly string _auditDir;

    public AuditScenarios(E2ETestFixture fixture)
    {
        _fixture = fixture;
        _testDir = Path.Combine(Path.GetTempPath(), $"audit_e2e_{Guid.NewGuid():N}");
        _auditDir = Path.Combine(_testDir, ".agent", "logs", "audit");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    [Fact]
    public async Task Scenario_CompleteSession_AllEventsLogged()
    {
        // Arrange
        var app = _fixture.CreateApplication(_testDir);

        // Act - execute a complete session
        await app.StartSessionAsync();
        await app.ExecuteTaskAsync("Create a simple class");
        await app.EndSessionAsync();

        // Assert - verify all required events present
        var events = await ReadAllAuditEvents();
        
        events.Should().Contain(e => e.EventType == "session_start",
            because: "session start must be logged");
        events.Should().Contain(e => e.EventType == "task_start",
            because: "task execution must be logged");
        events.Should().Contain(e => e.EventType == "task_end",
            because: "task completion must be logged");
        events.Should().Contain(e => e.EventType == "session_end",
            because: "session end must be logged");

        // Verify session continuity
        var sessionId = events.First().SessionId;
        events.Should().OnlyContain(e => e.SessionId == sessionId,
            because: "all events should belong to the same session");

        // Verify ordering
        var timestamps = events.Select(e => e.Timestamp).ToList();
        timestamps.Should().BeInAscendingOrder(
            because: "events should be chronologically ordered");

        // Verify correlation chain
        var correlationIds = events.Select(e => e.CorrelationId).Distinct();
        correlationIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Scenario_FileOperations_CorrectlyAudited()
    {
        // Arrange
        var app = _fixture.CreateApplication(_testDir);
        var targetFile = Path.Combine(_testDir, "test.cs");

        // Act - perform file operations
        await app.StartSessionAsync();
        await app.ReadFileAsync(targetFile);  // Should log file_read
        await app.WriteFileAsync(targetFile, "// Test content");  // Should log file_write
        await app.EndSessionAsync();

        // Assert
        var events = await ReadAllAuditEvents();

        // Find file read event
        var readEvent = events.First(e => e.EventType == "file_read");
        readEvent.Data.Should().ContainKey("path");
        readEvent.Data["path"].ToString().Should().Contain("test.cs");
        readEvent.Data.Should().ContainKey("success");
        readEvent.Data.Should().NotContainKey("content",
            because: "file content should never be logged");

        // Find file write event
        var writeEvent = events.First(e => e.EventType == "file_write");
        writeEvent.Data.Should().ContainKey("path");
        writeEvent.Data.Should().ContainKey("bytes_written");
        writeEvent.Data.Should().NotContainKey("content",
            because: "file content should never be logged");
    }

    [Fact]
    public async Task Scenario_SecurityViolation_CapturedWithDetails()
    {
        // Arrange
        var app = _fixture.CreateApplication(_testDir);

        // Act - attempt to access protected path
        await app.StartSessionAsync();
        var result = await app.ReadFileAsync("/etc/passwd");  // Should be blocked
        await app.EndSessionAsync();

        // Assert
        result.Success.Should().BeFalse();

        var events = await ReadAllAuditEvents();

        // Find security violation event
        var violationEvent = events.First(e => 
            e.EventType == "constraint_violation" || 
            e.EventType == "operation_blocked");

        violationEvent.Severity.Should().BeOneOf("warning", "error");
        violationEvent.Data.Should().ContainKey("requested_path");
        violationEvent.Data.Should().ContainKey("reason");
        violationEvent.Data.Should().ContainKey("constraint_type");
        violationEvent.Data["constraint_type"].ToString().Should().Be("protected_path");
    }

    [Fact]
    public async Task Scenario_ErrorRecovery_TrackedInAudit()
    {
        // Arrange
        var app = _fixture.CreateApplication(_testDir);

        // Act - simulate an error that's recovered from
        await app.StartSessionAsync();
        
        try
        {
            await app.ExecuteTaskAsync("intentionally_failing_task");
        }
        catch { /* Expected */ }

        await app.ExecuteTaskAsync("recovery_task");
        await app.EndSessionAsync();

        // Assert
        var events = await ReadAllAuditEvents();

        // Verify error event
        var errorEvent = events.FirstOrDefault(e => e.EventType == "task_error");
        errorEvent.Should().NotBeNull();
        errorEvent!.Severity.Should().Be("error");

        // Verify recovery - subsequent task succeeded
        var recoveryEvents = events.Where(e => 
            e.Timestamp > errorEvent.Timestamp && 
            e.EventType == "task_end");
        recoveryEvents.Should().NotBeEmpty(
            because: "recovery should be tracked after error");
    }

    [Fact]
    public async Task Scenario_GracefulShutdown_AuditComplete()
    {
        // Arrange
        var app = _fixture.CreateApplication(_testDir);

        // Act - normal shutdown
        await app.StartSessionAsync();
        await app.ExecuteTaskAsync("test task");
        await app.EndSessionAsync();
        await app.DisposeAsync();

        // Assert
        var events = await ReadAllAuditEvents();

        // Last event should be session_end
        events.Last().EventType.Should().Be("session_end");

        // All events should have integrity checksums
        var logFiles = Directory.GetFiles(_auditDir, "*.jsonl");
        foreach (var file in logFiles)
        {
            var validator = new AuditLogValidator(new AuditConfiguration());
            var result = await validator.ValidateFileAsync(file);
            result.IsValid.Should().BeTrue(
                because: $"log file {Path.GetFileName(file)} should be valid");
        }
    }

    [Fact]
    public async Task Scenario_ForcedShutdown_NoDataLoss()
    {
        // Arrange
        var app = _fixture.CreateApplication(_testDir);

        // Act - simulate crash (no graceful shutdown)
        await app.StartSessionAsync();
        await app.ExecuteTaskAsync("task 1");
        await app.ExecuteTaskAsync("task 2");
        
        // Force abort without EndSessionAsync
        app.ForceAbort();

        // Assert - verify events still persisted
        var events = await ReadAllAuditEvents();

        events.Should().Contain(e => e.EventType == "session_start");
        events.Where(e => e.EventType == "task_start").Should().HaveCount(2,
            because: "both task starts should be persisted immediately");
        
        // No data loss - all written events preserved
        events.Count.Should().BeGreaterThanOrEqualTo(3,
            because: "session_start + 2 task_start at minimum");
    }

    [Fact]
    public async Task Scenario_LogRotation_DuringSession()
    {
        // Arrange - configure for small rotation size
        var config = new Dictionary<string, string>
        {
            ["audit:rotation:size_mb"] = "0.1"  // 100KB for testing
        };
        var app = _fixture.CreateApplication(_testDir, config);

        // Act - generate enough events to trigger rotation
        await app.StartSessionAsync();
        
        for (int i = 0; i < 1000; i++)
        {
            await app.ExecuteTaskAsync($"task_{i}");
        }
        
        await app.EndSessionAsync();

        // Assert
        var logFiles = Directory.GetFiles(_auditDir, "*.jsonl");
        logFiles.Length.Should().BeGreaterThan(1,
            because: "rotation should have created multiple files");

        // Verify no events lost across rotation
        var allEvents = await ReadAllAuditEvents();
        allEvents.Where(e => e.EventType == "task_start").Should().HaveCount(1000);

        // Verify each file is valid
        foreach (var file in logFiles)
        {
            var validator = new AuditLogValidator(new AuditConfiguration());
            var result = await validator.ValidateFileAsync(file);
            result.IsValid.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Scenario_IntegrityVerification_AfterSession()
    {
        // Arrange
        var app = _fixture.CreateApplication(_testDir);

        // Act - complete a session
        await app.StartSessionAsync();
        await app.ExecuteTaskAsync("create file");
        await app.EndSessionAsync();
        await app.DisposeAsync();

        // Assert - use CLI to verify
        var cli = new CLITestHarness(_testDir);
        var result = await cli.RunAsync("audit", "verify");

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("verified");
        result.Output.Should().NotContain("corrupted");
        result.Output.Should().NotContain("tampered");
    }

    [Fact]
    public async Task Scenario_IntegrityVerification_DetectsTampering()
    {
        // Arrange
        var app = _fixture.CreateApplication(_testDir);

        await app.StartSessionAsync();
        await app.ExecuteTaskAsync("create file");
        await app.EndSessionAsync();
        await app.DisposeAsync();

        // Tamper with the log file
        var logFile = Directory.GetFiles(_auditDir, "*.jsonl").First();
        var lines = await File.ReadAllLinesAsync(logFile);
        lines[1] = lines[1].Replace("file", "FILE");  // Subtle change
        await File.WriteAllLinesAsync(logFile, lines);

        // Act - verify with CLI
        var cli = new CLITestHarness(_testDir);
        var result = await cli.RunAsync("audit", "verify");

        // Assert
        result.ExitCode.Should().Be(1,
            because: "tampering should be detected");
        result.Output.Should().Contain("integrity");
    }

    [Fact]
    public async Task Scenario_ExportAndAnalysis()
    {
        // Arrange
        var app = _fixture.CreateApplication(_testDir);

        await app.StartSessionAsync();
        await app.ExecuteTaskAsync("task 1");
        await app.ExecuteTaskAsync("task 2");
        await app.EndSessionAsync();
        await app.DisposeAsync();

        var exportPath = Path.Combine(_testDir, "audit_export.json");

        // Act - export and analyze
        var cli = new CLITestHarness(_testDir);
        var exportResult = await cli.RunAsync("audit", "export", "--output", exportPath);
        var statsResult = await cli.RunAsync("audit", "stats");

        // Assert - export succeeded
        exportResult.ExitCode.Should().Be(0);
        File.Exists(exportPath).Should().BeTrue();

        // Verify export format
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(exportPath));
        doc.RootElement.GetProperty("schemaVersion").GetString().Should().NotBeEmpty();
        doc.RootElement.GetProperty("exportTimestamp").GetString().Should().NotBeEmpty();
        doc.RootElement.GetProperty("sessions").GetArrayLength().Should().Be(1);
        doc.RootElement.GetProperty("totalEvents").GetInt32().Should().BeGreaterThan(0);

        // Stats should show meaningful data
        statsResult.ExitCode.Should().Be(0);
        statsResult.Output.Should().Contain("task_start: 2");
    }

    private async Task<List<AuditEventData>> ReadAllAuditEvents()
    {
        var events = new List<AuditEventData>();

        if (!Directory.Exists(_auditDir))
            return events;

        foreach (var file in Directory.GetFiles(_auditDir, "*.jsonl").OrderBy(f => f))
        {
            var lines = await File.ReadAllLinesAsync(file);
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                try
                {
                    var evt = JsonSerializer.Deserialize<AuditEventData>(line);
                    if (evt != null) events.Add(evt);
                }
                catch { /* Skip invalid lines */ }
            }
        }

        return events.OrderBy(e => e.Timestamp).ToList();
    }
}

internal class AuditEventData
{
    public string SchemaVersion { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string OperatingMode { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
}
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
```

#### AuditBenchmarks.cs

```csharp
namespace AgenticCoder.Tests.Performance.Audit;

using AgenticCoder.Infrastructure.Audit;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Security.Cryptography;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[RPlotExporter]
public class AuditBenchmarks
{
    private FileAuditWriter _writer = null!;
    private AuditEvent _testEvent = null!;
    private string _testDir = null!;
    private string _searchDir = null!;

    [GlobalSetup]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"audit_bench_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);

        _writer = new FileAuditWriter(_testDir, new AuditConfiguration());

        _testEvent = new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.FileRead,
            Severity = AuditSeverity.Info,
            Source = "Benchmark",
            OperatingMode = "local_only",
            Data = new Dictionary<string, object>
            {
                ["path"] = "/src/test.cs",
                ["bytes"] = 1024,
                ["duration_ms"] = 5.2
            }.AsReadOnly()
        };

        // Setup search directory with sample data
        _searchDir = Path.Combine(Path.GetTempPath(), $"audit_search_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_searchDir);
        SeedSearchData().GetAwaiter().GetResult();
    }

    private async Task SeedSearchData()
    {
        var writer = new FileAuditWriter(_searchDir, new AuditConfiguration());
        for (int i = 0; i < 10000; i++)
        {
            await writer.WriteAsync(CreateVariedEvent(i));
        }
        await writer.DisposeAsync();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _writer.DisposeAsync().GetAwaiter().GetResult();
        try { Directory.Delete(_testDir, true); } catch { }
        try { Directory.Delete(_searchDir, true); } catch { }
    }

    [Benchmark(Description = "Write single audit event")]
    public async Task Benchmark_SingleEventWrite()
    {
        await _writer.WriteAsync(_testEvent);
    }

    [Benchmark(Description = "Write 1000 events per second target")]
    [Arguments(1000)]
    public async Task Benchmark_1000EventsPerSecond(int count)
    {
        var tasks = new List<Task>(count);
        for (int i = 0; i < count; i++)
        {
            tasks.Add(_writer.WriteAsync(_testEvent));
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark(Description = "SHA-256 checksum calculation")]
    public byte[] Benchmark_ChecksumUpdate()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_testEvent);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return SHA256.HashData(bytes);
    }

    [Benchmark(Description = "Incremental checksum with chain")]
    public string Benchmark_IncrementalChecksum()
    {
        var prevHash = "0".PadLeft(64, '0');
        var json = System.Text.Json.JsonSerializer.Serialize(_testEvent);
        var combined = prevHash + json;
        var bytes = System.Text.Encoding.UTF8.GetBytes(combined);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Benchmark(Description = "Log rotation trigger and create")]
    public async Task Benchmark_LogRotation()
    {
        var rotationDir = Path.Combine(_testDir, $"rotation_{Guid.NewGuid():N}");
        Directory.CreateDirectory(rotationDir);

        var config = new AuditConfiguration
        {
            Directory = rotationDir,
            RotationSizeMb = 0.001  // 1KB for fast rotation
        };

        var writer = new FileAuditWriter(rotationDir, config);
        
        // Write enough to trigger rotation
        for (int i = 0; i < 100; i++)
        {
            await writer.WriteAsync(_testEvent);
        }

        await writer.DisposeAsync();
        Directory.Delete(rotationDir, true);
    }

    [Benchmark(Description = "Search 10K events by event type")]
    public async Task<int> Benchmark_SearchQuery()
    {
        var searcher = new AuditSearcher(_searchDir);
        var results = await searcher.SearchAsync(new AuditSearchQuery
        {
            EventType = "file_write"
        });
        return results.Count;
    }

    [Benchmark(Description = "Search 10K events by path pattern")]
    public async Task<int> Benchmark_SearchByPath()
    {
        var searcher = new AuditSearcher(_searchDir);
        var results = await searcher.SearchAsync(new AuditSearchQuery
        {
            PathPattern = "/src/*.cs"
        });
        return results.Count;
    }

    [Benchmark(Description = "Search 10K events by date range")]
    public async Task<int> Benchmark_SearchByDateRange()
    {
        var searcher = new AuditSearcher(_searchDir);
        var results = await searcher.SearchAsync(new AuditSearchQuery
        {
            After = DateTimeOffset.UtcNow.AddHours(-1),
            Before = DateTimeOffset.UtcNow
        });
        return results.Count;
    }

    [Benchmark(Description = "Export 10K events to JSON")]
    public async Task Benchmark_Export()
    {
        var exporter = new AuditExporter(_searchDir);
        var exportPath = Path.Combine(_testDir, $"export_{Guid.NewGuid():N}.json");
        
        await exporter.ExportAsync(exportPath, new AuditExportOptions());
        
        File.Delete(exportPath);
    }

    private static AuditEvent CreateVariedEvent(int index)
    {
        var eventTypes = new[] { "file_read", "file_write", "session_start", "session_end", "constraint_violation" };
        var severities = new[] { AuditSeverity.Info, AuditSeverity.Warning, AuditSeverity.Error };

        return new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow.AddSeconds(-index),
            SessionId = SessionId.Parse($"sess_{index % 10}"),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.Parse(eventTypes[index % eventTypes.Length]),
            Severity = severities[index % severities.Length],
            Source = "Benchmark",
            OperatingMode = "local_only",
            Data = new Dictionary<string, object>
            {
                ["path"] = $"/src/file_{index % 100}.cs",
                ["index"] = index
            }.AsReadOnly()
        };
    }
}
```

```
└── AuditLoadTests.cs
    ├── Should_Handle_HighEventRate()
    ├── Should_NotExceedMemoryLimit()
    ├── Should_NotExceedCPULimit()
    └── Should_MaintainLatency_UnderLoad()
```

#### AuditLoadTests.cs

```csharp
namespace AgenticCoder.Tests.Performance.Audit;

using AgenticCoder.Infrastructure.Audit;
using FluentAssertions;
using System.Diagnostics;
using Xunit;

[Trait("Category", "Performance")]
[Collection("Performance")]
public class AuditLoadTests : IDisposable
{
    private readonly string _testDir;

    public AuditLoadTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"audit_load_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    [Fact]
    [Trait("Performance", "HighRate")]
    public async Task Should_Handle_HighEventRate()
    {
        // Arrange
        var config = new AuditConfiguration { Directory = _testDir };
        var writer = new FileAuditWriter(_testDir, config);
        
        const int targetEventsPerSecond = 1000;
        const int durationSeconds = 5;
        const int totalEvents = targetEventsPerSecond * durationSeconds;

        var events = Enumerable.Range(0, totalEvents)
            .Select(CreateTestEvent)
            .ToList();

        // Act
        var sw = Stopwatch.StartNew();
        
        var tasks = events.Select(e => writer.WriteAsync(e));
        await Task.WhenAll(tasks);
        
        sw.Stop();
        await writer.DisposeAsync();

        // Assert
        var actualRate = totalEvents / sw.Elapsed.TotalSeconds;
        actualRate.Should().BeGreaterThan(targetEventsPerSecond,
            because: $"should handle at least {targetEventsPerSecond} events/sec, " +
                     $"achieved {actualRate:F0} events/sec");

        // Verify all events written
        var logFiles = Directory.GetFiles(_testDir, "*.jsonl");
        var totalLines = logFiles.Sum(f => File.ReadLines(f).Count());
        totalLines.Should().Be(totalEvents);
    }

    [Fact]
    [Trait("Performance", "Memory")]
    public async Task Should_NotExceedMemoryLimit()
    {
        // Arrange
        const long maxMemoryIncreaseMb = 50;
        var config = new AuditConfiguration { Directory = _testDir };
        var writer = new FileAuditWriter(_testDir, config);

        // Force GC and get baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var baselineMemory = GC.GetTotalMemory(true);

        // Act - write 100K events
        for (int i = 0; i < 100_000; i++)
        {
            await writer.WriteAsync(CreateTestEvent(i));
            
            // Periodically check memory
            if (i % 10_000 == 0)
            {
                var currentMemory = GC.GetTotalMemory(false);
                var increase = (currentMemory - baselineMemory) / (1024.0 * 1024.0);
                
                increase.Should().BeLessThan(maxMemoryIncreaseMb,
                    because: $"memory should not increase more than {maxMemoryIncreaseMb}MB " +
                             $"during sustained writes (at event {i}, increase: {increase:F1}MB)");
            }
        }

        await writer.DisposeAsync();

        // Final memory check
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(true);
        var finalIncrease = (finalMemory - baselineMemory) / (1024.0 * 1024.0);

        finalIncrease.Should().BeLessThan(maxMemoryIncreaseMb,
            because: "memory should be released after disposal");
    }

    [Fact]
    [Trait("Performance", "CPU")]
    public async Task Should_NotExceedCPULimit()
    {
        // Arrange
        var config = new AuditConfiguration { Directory = _testDir };
        var writer = new FileAuditWriter(_testDir, config);
        var process = Process.GetCurrentProcess();

        var startCpuTime = process.TotalProcessorTime;
        var sw = Stopwatch.StartNew();

        // Act - write 10K events
        for (int i = 0; i < 10_000; i++)
        {
            await writer.WriteAsync(CreateTestEvent(i));
        }

        sw.Stop();
        var endCpuTime = process.TotalProcessorTime;
        await writer.DisposeAsync();

        // Assert
        var cpuUsed = (endCpuTime - startCpuTime).TotalMilliseconds;
        var wallTime = sw.Elapsed.TotalMilliseconds;
        var cpuPercent = (cpuUsed / wallTime) * 100 / Environment.ProcessorCount;

        cpuPercent.Should().BeLessThan(25,
            because: $"audit logging should use less than 25% CPU, " +
                     $"used {cpuPercent:F1}% ({cpuUsed:F0}ms CPU / {wallTime:F0}ms wall)");
    }

    [Fact]
    [Trait("Performance", "Latency")]
    public async Task Should_MaintainLatency_UnderLoad()
    {
        // Arrange
        var config = new AuditConfiguration { Directory = _testDir };
        var writer = new FileAuditWriter(_testDir, config);
        var latencies = new List<double>();

        // Act - measure latency for each write
        for (int i = 0; i < 1000; i++)
        {
            var sw = Stopwatch.StartNew();
            await writer.WriteAsync(CreateTestEvent(i));
            sw.Stop();
            latencies.Add(sw.Elapsed.TotalMilliseconds);
        }

        await writer.DisposeAsync();

        // Assert
        var avgLatency = latencies.Average();
        var p50 = Percentile(latencies, 50);
        var p95 = Percentile(latencies, 95);
        var p99 = Percentile(latencies, 99);
        var maxLatency = latencies.Max();

        avgLatency.Should().BeLessThan(5,
            because: $"average latency should be <5ms, was {avgLatency:F2}ms");
        p95.Should().BeLessThan(10,
            because: $"P95 latency should be <10ms, was {p95:F2}ms");
        p99.Should().BeLessThan(50,
            because: $"P99 latency should be <50ms, was {p99:F2}ms");
    }

    [Fact]
    [Trait("Performance", "Concurrent")]
    public async Task Should_HandleConcurrentSessions()
    {
        // Arrange - simulate 10 concurrent sessions
        const int sessionCount = 10;
        const int eventsPerSession = 1000;
        var config = new AuditConfiguration { Directory = _testDir };

        // Act
        var sw = Stopwatch.StartNew();
        
        var tasks = Enumerable.Range(0, sessionCount)
            .Select(async sessionIndex =>
            {
                var sessionId = SessionId.Parse($"sess_{sessionIndex}");
                var writer = new FileAuditWriter(_testDir, config, sessionId);
                
                for (int i = 0; i < eventsPerSession; i++)
                {
                    await writer.WriteAsync(CreateTestEvent(i, sessionId));
                }
                
                await writer.DisposeAsync();
            });

        await Task.WhenAll(tasks);
        sw.Stop();

        // Assert
        var totalEvents = sessionCount * eventsPerSession;
        var eventsPerSecond = totalEvents / sw.Elapsed.TotalSeconds;

        eventsPerSecond.Should().BeGreaterThan(500,
            because: $"concurrent sessions should maintain good throughput, " +
                     $"achieved {eventsPerSecond:F0} events/sec");

        // Verify all events written
        var logFiles = Directory.GetFiles(_testDir, "*.jsonl");
        logFiles.Should().HaveCount(sessionCount,
            because: "each session should have its own log file");
    }

    private static double Percentile(List<double> values, int percentile)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }

    private static AuditEvent CreateTestEvent(int index, SessionId? sessionId = null)
    {
        return new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = sessionId ?? SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.FileRead,
            Severity = AuditSeverity.Info,
            Source = "LoadTest",
            OperatingMode = "local_only",
            Data = new Dictionary<string, object>
            {
                ["index"] = index,
                ["path"] = $"/test/file_{index}.cs"
            }.AsReadOnly()
        };
    }
}
```

### Regression Tests

```
Tests/Regression/Audit/
├── EventLossTests.cs
│   ├── Should_NotLose_Events_OnCrash()
│   ├── Should_NotLose_Events_OnDiskFull()
│   ├── Should_NotLose_Events_OnHighLoad()
│   └── Should_NotLose_Events_OnRotation()
```

#### EventLossTests.cs

```csharp
namespace AgenticCoder.Tests.Regression.Audit;

using AgenticCoder.Infrastructure.Audit;
using FluentAssertions;
using System.Text.Json;
using Xunit;

/// <summary>
/// Regression tests to ensure audit events are never lost under various failure conditions.
/// These tests verify the durability guarantees of the audit system.
/// </summary>
[Trait("Category", "Regression")]
[Collection("Regression")]
public class EventLossTests : IDisposable
{
    private readonly string _testDir;

    public EventLossTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"audit_regression_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    [Fact]
    [Trait("Regression", "DataLoss")]
    public async Task Should_NotLose_Events_OnCrash()
    {
        // Arrange
        var config = new AuditConfiguration { Directory = _testDir };
        var expectedEvents = new List<string>();

        // Act - simulate crash by not disposing properly
        var writer = new FileAuditWriter(_testDir, config);
        
        for (int i = 0; i < 100; i++)
        {
            var evt = CreateTestEvent(i);
            expectedEvents.Add(evt.EventId.ToString());
            await writer.WriteAsync(evt);
        }
        
        // Simulate crash - abandon without dispose
        // In real crash, the FileAuditWriter would be lost without cleanup
        // But events should already be flushed to disk

        // Assert - verify events are on disk despite no dispose
        var actualEvents = await ReadAllEventIds();
        
        actualEvents.Should().BeEquivalentTo(expectedEvents,
            because: "all events should be persisted immediately, not buffered");

        // Cleanup for test
        await writer.DisposeAsync();
    }

    [Fact]
    [Trait("Regression", "DataLoss")]
    public async Task Should_NotLose_Events_OnDiskFull()
    {
        // Arrange - very small storage limit
        var config = new AuditConfiguration
        {
            Directory = _testDir,
            MaxStorageMb = 1  // 1MB limit
        };

        var writer = new FileAuditWriter(_testDir, config);
        var eventsBeforeLimit = new List<string>();

        // Act - write until we hit storage limit
        try
        {
            for (int i = 0; i < 100_000; i++)
            {
                var evt = CreateLargeTestEvent(i);
                eventsBeforeLimit.Add(evt.EventId.ToString());
                await writer.WriteAsync(evt);
            }
        }
        catch (AuditStorageException)
        {
            // Expected - storage limit reached
        }

        await writer.DisposeAsync();

        // Assert - all events written before exception should be preserved
        var actualEvents = await ReadAllEventIds();
        
        // We don't know exactly how many were written, but none should be lost
        foreach (var eventId in actualEvents)
        {
            eventsBeforeLimit.Should().Contain(eventId,
                because: "every event on disk should be one we tried to write");
        }

        // Every event on disk should be valid
        var logFiles = Directory.GetFiles(_testDir, "*.jsonl");
        foreach (var file in logFiles)
        {
            var lines = await File.ReadAllLinesAsync(file);
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var action = () => JsonDocument.Parse(line);
                action.Should().NotThrow(
                    because: "all persisted events should be valid JSON");
            }
        }
    }

    [Fact]
    [Trait("Regression", "DataLoss")]
    public async Task Should_NotLose_Events_OnHighLoad()
    {
        // Arrange
        var config = new AuditConfiguration { Directory = _testDir };
        var writer = new FileAuditWriter(_testDir, config);
        
        const int totalEvents = 10_000;
        var expectedEventIds = new ConcurrentBag<string>();

        // Act - high concurrency stress test
        var tasks = Enumerable.Range(0, totalEvents)
            .Select(async i =>
            {
                var evt = CreateTestEvent(i);
                expectedEventIds.Add(evt.EventId.ToString());
                await writer.WriteAsync(evt);
            });

        await Task.WhenAll(tasks);
        await writer.DisposeAsync();

        // Assert - no events lost
        var actualEvents = await ReadAllEventIds();
        
        actualEvents.Should().HaveCount(totalEvents,
            because: $"all {totalEvents} events should be persisted");
        
        actualEvents.Should().BeEquivalentTo(expectedEventIds,
            because: "exactly the events we wrote should be on disk");
    }

    [Fact]
    [Trait("Regression", "DataLoss")]
    public async Task Should_NotLose_Events_OnRotation()
    {
        // Arrange - small rotation size to force many rotations
        var config = new AuditConfiguration
        {
            Directory = _testDir,
            RotationSizeMb = 0.01  // 10KB for frequent rotation
        };

        var writer = new FileAuditWriter(_testDir, config);
        var expectedEventIds = new List<string>();

        // Act - write enough to cause multiple rotations
        for (int i = 0; i < 1000; i++)
        {
            var evt = CreateTestEvent(i);
            expectedEventIds.Add(evt.EventId.ToString());
            await writer.WriteAsync(evt);
        }

        await writer.DisposeAsync();

        // Assert - multiple files created
        var logFiles = Directory.GetFiles(_testDir, "*.jsonl");
        logFiles.Length.Should().BeGreaterThan(1,
            because: "multiple rotations should have occurred");

        // No events lost across rotations
        var actualEvents = await ReadAllEventIds();
        actualEvents.Should().BeEquivalentTo(expectedEventIds,
            because: "no events should be lost during rotation");
    }

    [Fact]
    [Trait("Regression", "DataLoss")]
    public async Task Should_NotLose_Events_OnConcurrentRotation()
    {
        // Arrange - stress test rotation under concurrent writes
        var config = new AuditConfiguration
        {
            Directory = _testDir,
            RotationSizeMb = 0.05  // 50KB
        };

        var writer = new FileAuditWriter(_testDir, config);
        var expectedEventIds = new ConcurrentBag<string>();

        // Act - concurrent writes forcing rotation
        var tasks = Enumerable.Range(0, 100)
            .Select(async batch =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var evt = CreateTestEvent(batch * 100 + i);
                    expectedEventIds.Add(evt.EventId.ToString());
                    await writer.WriteAsync(evt);
                }
            });

        await Task.WhenAll(tasks);
        await writer.DisposeAsync();

        // Assert
        var actualEvents = await ReadAllEventIds();
        actualEvents.Should().HaveCount(10_000,
            because: "all 10,000 events should be persisted despite concurrent rotation");
    }

    private async Task<List<string>> ReadAllEventIds()
    {
        var eventIds = new List<string>();

        foreach (var file in Directory.GetFiles(_testDir, "*.jsonl"))
        {
            var lines = await File.ReadAllLinesAsync(file);
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    if (doc.RootElement.TryGetProperty("eventId", out var eventIdProp))
                    {
                        eventIds.Add(eventIdProp.GetString()!);
                    }
                }
                catch { /* Skip invalid lines */ }
            }
        }

        return eventIds;
    }

    private static AuditEvent CreateTestEvent(int index)
    {
        return new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.FileRead,
            Severity = AuditSeverity.Info,
            Source = "RegressionTest",
            OperatingMode = "local_only",
            Data = new Dictionary<string, object>
            {
                ["index"] = index
            }.AsReadOnly()
        };
    }

    private static AuditEvent CreateLargeTestEvent(int index)
    {
        return new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.FileRead,
            Severity = AuditSeverity.Info,
            Source = "RegressionTest",
            OperatingMode = "local_only",
            Data = new Dictionary<string, object>
            {
                ["index"] = index,
                ["padding"] = new string('x', 500)  // Large payload
            }.AsReadOnly()
        };
    }
}
```

```
└── SecurityRegressionTests.cs
    ├── Should_AlwaysRedact_Secrets()
    ├── Should_NeverLog_FileContents()
    ├── Should_MaintainPermissions()
    └── Should_PreventTampering()
```

#### SecurityRegressionTests.cs

```csharp
namespace AgenticCoder.Tests.Regression.Audit;

using AgenticCoder.Infrastructure.Audit;
using FluentAssertions;
using System.Runtime.InteropServices;
using System.Text.Json;
using Xunit;

/// <summary>
/// Security regression tests to verify audit system doesn't leak sensitive data.
/// These tests ensure the security properties of the audit system are maintained.
/// </summary>
[Trait("Category", "Regression")]
[Trait("Category", "Security")]
[Collection("Regression")]
public class SecurityRegressionTests : IDisposable
{
    private readonly string _testDir;

    public SecurityRegressionTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"audit_security_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    [Theory]
    [Trait("Security", "Redaction")]
    [InlineData("GITHUB_TOKEN=ghp_abc123xyz789")]
    [InlineData("api_key: sk-proj-1234567890abcdef")]
    [InlineData("password=SuperSecret123!")]
    [InlineData("AWS_SECRET_ACCESS_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY")]
    [InlineData("Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9")]
    [InlineData("-----BEGIN RSA PRIVATE KEY-----")]
    [InlineData("-----BEGIN OPENSSH PRIVATE KEY-----")]
    public async Task Should_AlwaysRedact_Secrets(string sensitiveData)
    {
        // Arrange
        var config = new AuditConfiguration { Directory = _testDir };
        var writer = new FileAuditWriter(_testDir, config);

        var evt = new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.FileRead,
            Severity = AuditSeverity.Info,
            Source = "SecurityTest",
            OperatingMode = "local_only",
            Data = new Dictionary<string, object>
            {
                ["output"] = $"Command output: {sensitiveData}",
                ["error"] = $"Error: {sensitiveData}",
                ["metadata"] = sensitiveData
            }.AsReadOnly()
        };

        // Act
        await writer.WriteAsync(evt);
        await writer.DisposeAsync();

        // Assert
        var logContent = await File.ReadAllTextAsync(
            Directory.GetFiles(_testDir, "*.jsonl").First());

        // Extract actual sensitive values (not redaction markers)
        var sensitivePatterns = new[]
        {
            "ghp_", "sk-proj-", "SuperSecret", "wJalrXUtn",
            "eyJhbGciOi", "BEGIN RSA PRIVATE KEY", "BEGIN OPENSSH"
        };

        foreach (var pattern in sensitivePatterns)
        {
            if (sensitiveData.Contains(pattern))
            {
                logContent.Should().NotContain(pattern,
                    because: $"sensitive data '{pattern}...' should be redacted");
            }
        }

        // Should contain redaction marker instead
        logContent.Should().Contain("[REDACTED",
            because: "redacted content should show a marker");
    }

    [Fact]
    [Trait("Security", "FileContents")]
    public async Task Should_NeverLog_FileContents()
    {
        // Arrange
        var config = new AuditConfiguration { Directory = _testDir };
        var writer = new FileAuditWriter(_testDir, config);

        var fileContent = "This is file content that should never appear in logs!";

        // Various events that might try to include file content
        var events = new[]
        {
            CreateFileEvent("file_read", fileContent),
            CreateFileEvent("file_write", fileContent),
            CreateFileEvent("file_create", fileContent),
            CreateFileEvent("file_modify", fileContent)
        };

        // Act
        foreach (var evt in events)
        {
            await writer.WriteAsync(evt);
        }
        await writer.DisposeAsync();

        // Assert
        var logContent = await File.ReadAllTextAsync(
            Directory.GetFiles(_testDir, "*.jsonl").First());

        logContent.Should().NotContain(fileContent,
            because: "file content should never be logged");
        logContent.Should().NotContain("never appear in logs",
            because: "file content should be completely excluded");

        // Should contain metadata about the file operation
        logContent.Should().Contain("path",
            because: "file path metadata should be logged");
    }

    [Fact]
    [Trait("Security", "ContentFiltering")]
    public async Task Should_FilterContent_EvenInErrorMessages()
    {
        // Arrange
        var config = new AuditConfiguration { Directory = _testDir };
        var writer = new FileAuditWriter(_testDir, config);

        var sensitiveContent = "password=secret123";
        var evt = new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.Error,
            Severity = AuditSeverity.Error,
            Source = "SecurityTest",
            OperatingMode = "local_only",
            Data = new Dictionary<string, object>
            {
                ["error_message"] = $"Failed to parse config: {sensitiveContent}",
                ["stack_trace"] = $"at Config.Parse(\"{sensitiveContent}\")"
            }.AsReadOnly()
        };

        // Act
        await writer.WriteAsync(evt);
        await writer.DisposeAsync();

        // Assert
        var logContent = await File.ReadAllTextAsync(
            Directory.GetFiles(_testDir, "*.jsonl").First());

        logContent.Should().NotContain("secret123",
            because: "secrets in error messages should be redacted");
    }

    [SkippableFact]
    [Trait("Security", "Permissions")]
    public async Task Should_MaintainPermissions_Unix()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        // Arrange
        var config = new AuditConfiguration { Directory = _testDir };
        var writer = new FileAuditWriter(_testDir, config);

        // Act
        await writer.WriteAsync(CreateSimpleEvent());
        await writer.DisposeAsync();

        // Assert
        var logFile = Directory.GetFiles(_testDir, "*.jsonl").First();
        var mode = File.GetUnixFileMode(logFile);

        // Should be 0600 - owner read/write only
        mode.Should().HaveFlag(UnixFileMode.UserRead);
        mode.Should().HaveFlag(UnixFileMode.UserWrite);
        mode.Should().NotHaveFlag(UnixFileMode.GroupRead);
        mode.Should().NotHaveFlag(UnixFileMode.GroupWrite);
        mode.Should().NotHaveFlag(UnixFileMode.OtherRead);
        mode.Should().NotHaveFlag(UnixFileMode.OtherWrite);

        // Directory should be 0700
        var dirMode = new DirectoryInfo(_testDir).UnixFileMode;
        dirMode.Should().HaveFlag(UnixFileMode.UserRead);
        dirMode.Should().HaveFlag(UnixFileMode.UserWrite);
        dirMode.Should().HaveFlag(UnixFileMode.UserExecute);
        dirMode.Should().NotHaveFlag(UnixFileMode.GroupRead);
        dirMode.Should().NotHaveFlag(UnixFileMode.OtherRead);
    }

    [Fact]
    [Trait("Security", "Integrity")]
    public async Task Should_PreventTampering()
    {
        // Arrange
        var config = new AuditConfiguration
        {
            Directory = _testDir,
            EnableIntegrityChecks = true
        };
        var writer = new FileAuditWriter(_testDir, config);

        // Write some events
        for (int i = 0; i < 10; i++)
        {
            await writer.WriteAsync(CreateSimpleEvent());
        }
        await writer.DisposeAsync();

        // Verify integrity before tampering
        var validator = new AuditLogValidator(config);
        var logFile = Directory.GetFiles(_testDir, "*.jsonl").First();
        var beforeResult = await validator.ValidateFileAsync(logFile);
        beforeResult.IsValid.Should().BeTrue();

        // Act - tamper with the file
        var lines = await File.ReadAllLinesAsync(logFile);
        lines[5] = lines[5].Replace("info", "error");  // Subtle change
        await File.WriteAllLinesAsync(logFile, lines);

        // Assert - tampering detected
        var afterResult = await validator.ValidateFileAsync(logFile);
        afterResult.IsValid.Should().BeFalse(
            because: "integrity check should detect modification");
        afterResult.Errors.Should().Contain(e => e.Contains("line 6") || e.Contains("checksum"),
            because: "error should indicate which line was tampered");
    }

    [Fact]
    [Trait("Security", "Integrity")]
    public async Task Should_DetectDeletion()
    {
        // Arrange
        var config = new AuditConfiguration
        {
            Directory = _testDir,
            EnableIntegrityChecks = true
        };
        var writer = new FileAuditWriter(_testDir, config);

        for (int i = 0; i < 10; i++)
        {
            await writer.WriteAsync(CreateSimpleEvent());
        }
        await writer.DisposeAsync();

        var logFile = Directory.GetFiles(_testDir, "*.jsonl").First();

        // Act - delete a line
        var lines = (await File.ReadAllLinesAsync(logFile)).ToList();
        lines.RemoveAt(5);
        await File.WriteAllLinesAsync(logFile, lines);

        // Assert
        var validator = new AuditLogValidator(config);
        var result = await validator.ValidateFileAsync(logFile);
        
        result.IsValid.Should().BeFalse(
            because: "deletion should break the integrity chain");
    }

    [Fact]
    [Trait("Security", "Integrity")]
    public async Task Should_DetectInsertion()
    {
        // Arrange
        var config = new AuditConfiguration
        {
            Directory = _testDir,
            EnableIntegrityChecks = true
        };
        var writer = new FileAuditWriter(_testDir, config);

        for (int i = 0; i < 10; i++)
        {
            await writer.WriteAsync(CreateSimpleEvent());
        }
        await writer.DisposeAsync();

        var logFile = Directory.GetFiles(_testDir, "*.jsonl").First();

        // Act - insert a fake event
        var lines = (await File.ReadAllLinesAsync(logFile)).ToList();
        var fakeEvent = "{\"eventId\":\"fake\",\"timestamp\":\"2024-01-01T00:00:00Z\"}";
        lines.Insert(5, fakeEvent);
        await File.WriteAllLinesAsync(logFile, lines);

        // Assert
        var validator = new AuditLogValidator(config);
        var result = await validator.ValidateFileAsync(logFile);
        
        result.IsValid.Should().BeFalse(
            because: "insertion should break the integrity chain");
    }

    private static AuditEvent CreateFileEvent(string eventType, string content)
    {
        return new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.Parse(eventType),
            Severity = AuditSeverity.Info,
            Source = "SecurityTest",
            OperatingMode = "local_only",
            Data = new Dictionary<string, object>
            {
                ["path"] = "/test/file.txt",
                // NOTE: Content should be filtered out by the writer
                ["content"] = content,
                ["bytes"] = content.Length
            }.AsReadOnly()
        };
    }

    private static AuditEvent CreateSimpleEvent()
    {
        return new AuditEvent
        {
            SchemaVersion = "1.0",
            EventId = EventId.New(),
            Timestamp = DateTimeOffset.UtcNow,
            SessionId = SessionId.New(),
            CorrelationId = CorrelationId.New(),
            EventType = AuditEventType.FileRead,
            Severity = AuditSeverity.Info,
            Source = "SecurityTest",
            OperatingMode = "local_only",
            Data = new Dictionary<string, object>
            {
                ["path"] = "/test/file.cs"
            }.AsReadOnly()
        };
    }
}
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