# Audit Event Schema

This document defines the complete audit event schema used by Acode for all audit logging operations.

## Overview

Audit events are immutable records that capture all significant operations performed by the Acode agent. Events are serialized to JSONL (JSON Lines) format for append-only logging with integrity verification via SHA-256 checksums.

**Schema Version**: 1.0.0

## Event Schema Fields

The audit event schema consists of 13 fields, all required unless marked optional:

### 1. SchemaVersion (string, required)
- **Type**: `string`
- **Format**: Semantic versioning (e.g., "1.0.0")
- **Description**: Schema version for forward/backward compatibility
- **Validation**: Must match pattern `^\d+\.\d+\.\d+$`
- **Example**: `"1.0.0"`

### 2. EventId (string, required)
- **Type**: `string`
- **Format**: `evt_` prefix followed by alphanumeric characters
- **Description**: Unique identifier for this event
- **Validation**: Must match pattern `^evt_[a-zA-Z0-9]+$`
- **Example**: `"evt_a3f8c2d1b5e4f6a7"`

### 3. Timestamp (ISO 8601 datetime, required)
- **Type**: `string` (ISO 8601 format with UTC timezone)
- **Description**: When the event occurred (UTC)
- **Validation**: Must be valid ISO 8601 datetime with timezone
- **Example**: `"2026-01-11T15:30:45.1234567Z"`

### 4. SessionId (string, required)
- **Type**: `string`
- **Format**: `sess_` prefix followed by alphanumeric characters
- **Description**: Identifier for the Acode agent session
- **Validation**: Must match pattern `^sess_[a-zA-Z0-9]+$`
- **Example**: `"sess_b1c2d3e4f5a6b7c8"`

### 5. CorrelationId (string, required)
- **Type**: `string`
- **Format**: `corr_` prefix followed by alphanumeric characters
- **Description**: Correlation ID for grouping related events
- **Validation**: Must match pattern `^corr_[a-zA-Z0-9]+$`
- **Example**: `"corr_x9y8z7w6v5u4t3s2"`

### 6. SpanId (string, optional)
- **Type**: `string` or `null`
- **Format**: `span_` prefix followed by alphanumeric characters
- **Description**: Span identifier for hierarchical distributed tracing
- **Validation**: If present, must match pattern `^span_[a-zA-Z0-9]+$`
- **Example**: `"span_abc123def456"` or `null`

### 7. ParentSpanId (string, optional)
- **Type**: `string` or `null`
- **Format**: `span_` prefix followed by alphanumeric characters
- **Description**: Parent span identifier for hierarchical tracing
- **Validation**: If present, must match pattern `^span_[a-zA-Z0-9]+$`
- **Example**: `"span_parent789xyz"` or `null`

### 8. EventType (string enum, required)
- **Type**: `string` (enum)
- **Description**: Type of audit event (see Event Types section)
- **Validation**: Must be one of the 25 defined event types
- **Example**: `"CommandStart"`, `"FileWrite"`, `"SecurityViolation"`

### 9. Severity (string enum, required)
- **Type**: `string` (enum)
- **Description**: Severity level of the event
- **Validation**: Must be one of: `Debug`, `Info`, `Warning`, `Error`, `Critical`
- **Example**: `"Info"`, `"Warning"`, `"Critical"`

### 10. Source (string, required)
- **Type**: `string`
- **Description**: Component that generated the event
- **Validation**: Non-empty string
- **Example**: `"Acode.Cli.Commands.BuildCommand"`, `"Acode.Infrastructure.FileSystem"`

### 11. OperatingMode (string, required)
- **Type**: `string`
- **Description**: Operating mode when event occurred
- **Validation**: Must be one of: `LocalOnly`, `Burst`, `Airgapped`
- **Example**: `"LocalOnly"`, `"Burst"`

### 12. Data (object, required)
- **Type**: `object` (key-value dictionary)
- **Description**: Event-specific data payload
- **Validation**: Must be a valid JSON object (can be empty `{}`)
- **Example**: `{"command": "build", "exitCode": 0, "durationMs": 1234}`

### 13. Context (object, optional)
- **Type**: `object` or `null`
- **Description**: Additional context information
- **Validation**: If present, must be a valid JSON object
- **Example**: `{"userId": "dev123", "repository": "acode"}` or `null`

## Event Types

Acode defines 25 event types covering all significant operations:

### Session Lifecycle
1. **SessionStart** - Agent session begins
2. **SessionEnd** - Agent session terminates
3. **Shutdown** - Graceful shutdown initiated

### Configuration
4. **ConfigLoad** - Configuration successfully loaded
5. **ConfigError** - Configuration validation failed
6. **ModeSelect** - Operating mode selected

### Command Execution
7. **CommandStart** - Command execution begins
8. **CommandEnd** - Command execution completes
9. **CommandError** - Command execution fails

### File Operations
10. **FileRead** - File read from disk
11. **FileWrite** - File written to disk
12. **FileDelete** - File deleted
13. **DirCreate** - Directory created
14. **DirDelete** - Directory deleted

### Security
15. **ProtectedPathBlocked** - Protected path access denied
16. **SecurityViolation** - Security policy violation

### Task Execution
17. **TaskStart** - Agent task begins
18. **TaskEnd** - Agent task completes
19. **TaskError** - Agent task fails

### User Interaction
20. **ApprovalRequest** - User approval requested
21. **ApprovalResponse** - User approval received

### Code Generation
22. **CodeGenerated** - LLM generated code

### Build & Test
23. **TestExecution** - Tests executed
24. **BuildExecution** - Build process executed

### Error Handling
25. **ErrorRecovery** - Error recovery attempted

## JSON Serialization Format

Events are serialized to JSONL (JSON Lines) format - one JSON object per line with no pretty printing.

**Example Event (SessionStart)**:
```json
{"schemaVersion":"1.0.0","eventId":"evt_a1b2c3d4e5f6","timestamp":"2026-01-11T15:30:45.1234567Z","sessionId":"sess_x1y2z3","correlationId":"corr_abc123","spanId":null,"parentSpanId":null,"eventType":"SessionStart","severity":"Info","source":"Acode.Cli.Program","operatingMode":"LocalOnly","data":{"agentVersion":"1.0.0","cliVersion":"1.0.0","platform":"linux"},"context":{"workingDirectory":"/home/user/project"}}
```

**Example Event (CommandStart)**:
```json
{"schemaVersion":"1.0.0","eventId":"evt_f6e5d4c3b2a1","timestamp":"2026-01-11T15:31:00.7654321Z","sessionId":"sess_x1y2z3","correlationId":"corr_def456","spanId":"span_cmd001","parentSpanId":null,"eventType":"CommandStart","severity":"Info","source":"Acode.Cli.Commands.BuildCommand","operatingMode":"LocalOnly","data":{"command":"build","args":["--configuration","Release"]},"context":{"projectPath":"/home/user/project"}}
```

**Example Event (FileWrite)**:
```json
{"schemaVersion":"1.0.0","eventId":"evt_1a2b3c4d5e6f","timestamp":"2026-01-11T15:31:05.1111111Z","sessionId":"sess_x1y2z3","correlationId":"corr_def456","spanId":"span_file001","parentSpanId":"span_cmd001","eventType":"FileWrite","severity":"Info","source":"Acode.Infrastructure.FileSystem","operatingMode":"LocalOnly","data":{"path":"src/Program.cs","sizeBytes":1024,"operation":"create"},"context":null}
```

**Example Event (SecurityViolation)**:
```json
{"schemaVersion":"1.0.0","eventId":"evt_9z8y7x6w5v4u","timestamp":"2026-01-11T15:32:00.9999999Z","sessionId":"sess_x1y2z3","correlationId":"corr_ghi789","spanId":"span_sec001","parentSpanId":null,"eventType":"SecurityViolation","severity":"Critical","source":"Acode.Infrastructure.Security","operatingMode":"LocalOnly","data":{"violationType":"ProtectedPathAccess","attemptedPath":".git/config","deniedReason":"Path on denylist"},"context":{"requestedBy":"Acode.Cli.Commands.FileCommand"}}
```

**Example Event (CommandEnd)**:
```json
{"schemaVersion":"1.0.0","eventId":"evt_4e3d2c1b0a9f","timestamp":"2026-01-11T15:31:30.5555555Z","sessionId":"sess_x1y2z3","correlationId":"corr_def456","spanId":"span_cmd001","parentSpanId":null,"eventType":"CommandEnd","severity":"Info","source":"Acode.Cli.Commands.BuildCommand","operatingMode":"LocalOnly","data":{"command":"build","exitCode":0,"durationMs":30000,"result":"success"},"context":null}
```

## Field Validation Rules

### Required Fields
All events MUST include these fields:
- `schemaVersion`
- `eventId`
- `timestamp`
- `sessionId`
- `correlationId`
- `eventType`
- `severity`
- `source`
- `operatingMode`
- `data`

### Optional Fields
These fields are optional:
- `spanId` (for hierarchical tracing)
- `parentSpanId` (for hierarchical tracing)
- `context` (for additional metadata)

### Format Validation
- **EventId**: Must match `^evt_[a-zA-Z0-9]+$`
- **SessionId**: Must match `^sess_[a-zA-Z0-9]+$`
- **CorrelationId**: Must match `^corr_[a-zA-Z0-9]+$`
- **SpanId/ParentSpanId**: If present, must match `^span_[a-zA-Z0-9]+$`
- **Timestamp**: Must be valid ISO 8601 format with UTC timezone
- **SchemaVersion**: Must be valid semver (e.g., "1.0.0")

### Enum Validation
- **EventType**: Must be one of the 25 defined event types
- **Severity**: Must be one of: Debug, Info, Warning, Error, Critical
- **OperatingMode**: Must be one of: LocalOnly, Burst, Airgapped

### Data Constraints
- **Data**: Must be a valid JSON object (can be empty `{}`)
- **Context**: If present, must be a valid JSON object
- **Source**: Non-empty string representing component name
- **EventId/SessionId/CorrelationId**: Globally unique within audit logs

## Integrity Verification

Each JSONL log file includes integrity metadata:
- **Checksum File**: Companion `.checksum` file with SHA-256 hash per line
- **Verification**: Line N in log file has checksum on line N in checksum file
- **Tamper Detection**: Any modification to event breaks checksum verification

## Storage Format

Events are stored in session-based log files:
```
.acode/logs/
├── session_sess_x1y2z3_20260111_153045.jsonl
├── session_sess_x1y2z3_20260111_153045.jsonl.checksum
├── session_sess_a4b5c6_20260111_140000.jsonl
└── session_sess_a4b5c6_20260111_140000.jsonl.checksum
```

**File Naming Convention**:
```
session_{sessionId}_{timestamp}.jsonl
```

Where:
- `{sessionId}` is the session identifier
- `{timestamp}` is the session start time in `yyyyMMdd_HHmmss` format

## Event Examples by Type

### SessionStart
```json
{
  "schemaVersion": "1.0.0",
  "eventId": "evt_session_start_001",
  "timestamp": "2026-01-11T10:00:00.0000000Z",
  "sessionId": "sess_20260111_100000",
  "correlationId": "corr_session_001",
  "spanId": null,
  "parentSpanId": null,
  "eventType": "SessionStart",
  "severity": "Info",
  "source": "Acode.Cli.Program",
  "operatingMode": "LocalOnly",
  "data": {
    "agentVersion": "1.0.0",
    "cliVersion": "1.0.0",
    "platform": "linux",
    "workingDirectory": "/home/user/acode"
  },
  "context": null
}
```

### ConfigLoad
```json
{
  "schemaVersion": "1.0.0",
  "eventId": "evt_config_load_001",
  "timestamp": "2026-01-11T10:00:01.0000000Z",
  "sessionId": "sess_20260111_100000",
  "correlationId": "corr_config_001",
  "spanId": null,
  "parentSpanId": null,
  "eventType": "ConfigLoad",
  "severity": "Info",
  "source": "Acode.Application.Config.ConfigLoader",
  "operatingMode": "LocalOnly",
  "data": {
    "configPath": ".agent/config.yml",
    "schemaVersion": "1.0.0",
    "validationResult": "success"
  },
  "context": null
}
```

### ProtectedPathBlocked
```json
{
  "schemaVersion": "1.0.0",
  "eventId": "evt_protected_path_001",
  "timestamp": "2026-01-11T10:05:00.0000000Z",
  "sessionId": "sess_20260111_100000",
  "correlationId": "corr_file_001",
  "spanId": "span_file_access_001",
  "parentSpanId": null,
  "eventType": "ProtectedPathBlocked",
  "severity": "Warning",
  "source": "Acode.Infrastructure.FileSystem.PathValidator",
  "operatingMode": "LocalOnly",
  "data": {
    "attemptedPath": ".git/config",
    "operation": "write",
    "deniedReason": "Path on denylist",
    "denylistRule": "^\.git/"
  },
  "context": {
    "requestedBy": "Acode.Cli.Commands.FileCommand"
  }
}
```

### ApprovalRequest
```json
{
  "schemaVersion": "1.0.0",
  "eventId": "evt_approval_req_001",
  "timestamp": "2026-01-11T10:10:00.0000000Z",
  "sessionId": "sess_20260111_100000",
  "correlationId": "corr_approval_001",
  "spanId": "span_approval_001",
  "parentSpanId": null,
  "eventType": "ApprovalRequest",
  "severity": "Info",
  "source": "Acode.Cli.Commands.ExecuteCommand",
  "operatingMode": "LocalOnly",
  "data": {
    "approvalType": "CommandExecution",
    "requestedOperation": "dotnet build",
    "reason": "User consent required for build execution"
  },
  "context": null
}
```

### ApprovalResponse
```json
{
  "schemaVersion": "1.0.0",
  "eventId": "evt_approval_resp_001",
  "timestamp": "2026-01-11T10:10:05.0000000Z",
  "sessionId": "sess_20260111_100000",
  "correlationId": "corr_approval_001",
  "spanId": "span_approval_001",
  "parentSpanId": null,
  "eventType": "ApprovalResponse",
  "severity": "Info",
  "source": "Acode.Cli.UserInteraction",
  "operatingMode": "LocalOnly",
  "data": {
    "approvalType": "CommandExecution",
    "decision": "approved",
    "timestamp": "2026-01-11T10:10:05Z"
  },
  "context": null
}
```

### CodeGenerated
```json
{
  "schemaVersion": "1.0.0",
  "eventId": "evt_codegen_001",
  "timestamp": "2026-01-11T10:15:00.0000000Z",
  "sessionId": "sess_20260111_100000",
  "correlationId": "corr_codegen_001",
  "spanId": "span_codegen_001",
  "parentSpanId": "span_task_001",
  "eventType": "CodeGenerated",
  "severity": "Info",
  "source": "Acode.Application.CodeGen.Generator",
  "operatingMode": "LocalOnly",
  "data": {
    "targetFile": "src/NewFeature.cs",
    "linesGenerated": 150,
    "modelProvider": "ollama",
    "modelName": "codellama:7b"
  },
  "context": {
    "taskDescription": "Implement authentication service"
  }
}
```

### TestExecution
```json
{
  "schemaVersion": "1.0.0",
  "eventId": "evt_test_exec_001",
  "timestamp": "2026-01-11T10:20:00.0000000Z",
  "sessionId": "sess_20260111_100000",
  "correlationId": "corr_test_001",
  "spanId": "span_test_001",
  "parentSpanId": null,
  "eventType": "TestExecution",
  "severity": "Info",
  "source": "Acode.Cli.Commands.TestCommand",
  "operatingMode": "LocalOnly",
  "data": {
    "command": "dotnet test",
    "totalTests": 125,
    "passedTests": 123,
    "failedTests": 2,
    "durationMs": 5000
  },
  "context": null
}
```

### SessionEnd
```json
{
  "schemaVersion": "1.0.0",
  "eventId": "evt_session_end_001",
  "timestamp": "2026-01-11T11:00:00.0000000Z",
  "sessionId": "sess_20260111_100000",
  "correlationId": "corr_session_001",
  "spanId": null,
  "parentSpanId": null,
  "eventType": "SessionEnd",
  "severity": "Info",
  "source": "Acode.Cli.Program",
  "operatingMode": "LocalOnly",
  "data": {
    "sessionDurationMs": 3600000,
    "totalEvents": 847,
    "exitReason": "user_initiated"
  },
  "context": null
}
```

## Related Documentation

- [CLI Audit Commands](cli-audit-commands.md) - Using the audit CLI
- [Audit Configuration](../data/config-schema.json) - Configuration schema
- [AUDIT-GUIDELINES.md](AUDIT-GUIDELINES.md) - Audit requirements and compliance
