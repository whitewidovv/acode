# Task 010.b: JSONL Event Stream Mode

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 010 (CLI Framework), Task 010.a (Command Routing)  

---

## Description

Task 010.b implements the JSONL event stream mode, providing machine-readable output for all CLI operations. JSONL (JSON Lines) format enables programmatic integration, automation, and tooling around Acode. This mode is essential for CI/CD pipelines, IDE plugins, monitoring systems, and any tooling that needs to parse CLI output.

JSONL mode transforms all CLI output into a stream of newline-delimited JSON objects. Each line is a self-contained JSON object representing a discrete event: progress updates, status changes, approvals requested, actions taken, errors encountered. Consumers parse line-by-line without buffering the entire output, enabling real-time processing of long-running operations.

The event stream architecture uses typed events. Every JSON object includes a `type` field identifying the event kind. Consumers can filter or route events based on type. Common event types include `progress`, `status`, `approval_request`, `action`, `error`, `completion`. This typing enables sophisticated handling without parsing message content.

Schema stability is paramount for integration reliability. Event schemas are versioned and documented. Breaking changes increment the major version and are communicated with deprecation notices. Consumers can rely on schema contracts—fields don't disappear without warning, and new fields are additive only.

Progress events provide granular visibility into long-running operations. Rather than a single completion message, consumers receive incremental updates: step started, percentage complete, estimated time remaining. This enables progress bars, monitoring dashboards, and timeout handling in automation.

Approval request events enable external approval workflows. When an action requires approval, the event includes all context needed for a decision: action type, affected files, risk level, proposed changes. External systems can display this information and submit responses via stdin or a separate approval endpoint.

Error events include structured information for programmatic handling. Error code, message, affected component, stack trace (if verbose), suggested remediation. Automation can respond appropriately to different error types—retrying transient errors, failing on permanent ones, alerting on unexpected conditions.

The event stream integrates with all CLI subsystems. Model operations emit events for loading, inference, retries. File operations emit events for reads, writes, diffs. Agent orchestration emits events for state transitions, planning, execution. This comprehensive coverage provides complete observability.

Stdout receives event lines; stderr receives logs. This separation enables clean event parsing while preserving diagnostic visibility. Consumers can pipe stdout to parsing logic while stderr provides human-readable context for debugging. The `--quiet` flag suppresses stderr if pure event output is needed.

Enabling JSONL mode is explicit. The `--json` flag activates event stream output. Without this flag, output remains human-readable. This explicit opt-in prevents accidental format changes that break existing workflows.

Buffering behavior is carefully managed. Events are flushed immediately after each line to ensure real-time streaming. This is critical for long-running operations—consumers shouldn't wait for buffer fills. Stdout is line-buffered in JSONL mode regardless of pipe status.

Timestamps use ISO 8601 format with millisecond precision. This standardization enables consistent time handling across time zones and systems. All timestamps are UTC to avoid local timezone complications.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| JSONL | JSON Lines - newline-delimited JSON |
| Event | Single JSON object in the stream |
| Event Type | Category identifier for events |
| Event Schema | Structure definition for event type |
| Schema Version | Version number for event formats |
| Line Buffering | Flushing after each newline |
| Event Consumer | System parsing the event stream |
| Event Producer | Component emitting events |
| Event Router | Component directing events |
| Stdout | Standard output stream (events) |
| Stderr | Standard error stream (logs) |
| ISO 8601 | Standard timestamp format |
| UTC | Coordinated Universal Time |
| Pipe Detection | Determining if output is piped |
| Flush | Force output buffer to stream |

---

## Out of Scope

The following items are explicitly excluded from Task 010.b:

- **Binary output formats** - JSONL only
- **WebSocket streaming** - stdout only
- **GraphQL interface** - CLI only
- **Event persistence** - Consumer responsibility
- **Event replay** - Consumer responsibility
- **Event aggregation** - Consumer responsibility
- **Custom serializers** - Standard JSON only
- **Compression** - Plain text only
- **Encryption** - Plain text output
- **Multi-language messages** - English only

---

## Assumptions

### Technical Assumptions

- ASM-001: System.Text.Json is available for JSON serialization with high performance
- ASM-002: Console output can be reliably flushed after each line for real-time streaming
- ASM-003: Stdout and stderr can be independently controlled and buffered
- ASM-004: Console.IsOutputRedirected accurately detects piped output
- ASM-005: JSON serialization can handle all .NET types used in events
- ASM-006: Timestamps can be generated with millisecond precision

### Environmental Assumptions

- ASM-007: Consumers can parse JSONL format (one JSON object per line)
- ASM-008: Pipe buffers are large enough for typical event sizes (< 4KB per event)
- ASM-009: Consumers process events in real-time or have sufficient buffering
- ASM-010: UTF-8 encoding is supported by all consumers
- ASM-011: Newline character is LF (\n) for cross-platform compatibility

### Dependency Assumptions

- ASM-012: Task 010 CLI Framework provides the --json flag handling
- ASM-013: Task 010.a command routing is complete for command identification
- ASM-014: All subsystems (model, file, orchestrator) emit events through a central event bus
- ASM-015: Event schemas are defined and versioned before implementation

### Consumer Assumptions

- ASM-016: Consumers understand JSON Lines format (vs. JSON array)
- ASM-017: Consumers can handle events arriving out of order in edge cases
- ASM-018: Consumers implement appropriate timeout handling for long operations
- ASM-019: Consumers filter events by type rather than parsing all content
- ASM-020: Consumers expect UTC timestamps and handle timezone conversion

---

## Functional Requirements

### JSONL Output Mode

- FR-001: --json flag MUST enable JSONL mode
- FR-002: ACODE_JSON=1 env MUST enable JSONL mode
- FR-003: Events MUST be written to stdout
- FR-004: Each event MUST be one line
- FR-005: Each line MUST be valid JSON
- FR-006: Lines MUST end with newline
- FR-007: Logs MUST go to stderr in JSONL mode
- FR-008: No non-JSON output on stdout in JSONL mode

### Event Structure

- FR-009: Events MUST have "type" field
- FR-010: Events MUST have "timestamp" field
- FR-011: Timestamps MUST be ISO 8601 UTC
- FR-012: Events MUST have "event_id" field
- FR-013: Event IDs MUST be unique per session
- FR-014: Events MAY have "correlation_id" field
- FR-015: Events MUST include schema version
- FR-016: Schema version MUST use semver

### Event Types

- FR-017: "session_start" for session begin
- FR-018: "session_end" for session complete
- FR-019: "progress" for incremental updates
- FR-020: "status" for state changes
- FR-021: "approval_request" for approval needed
- FR-022: "approval_response" for approval given
- FR-023: "action" for actions taken
- FR-024: "error" for error conditions
- FR-025: "warning" for warning conditions
- FR-026: "model_event" for model operations
- FR-027: "file_event" for file operations

### Session Events

- FR-028: session_start MUST include run_id
- FR-029: session_start MUST include command
- FR-030: session_start MUST include schema_version
- FR-031: session_end MUST include exit_code
- FR-032: session_end MUST include duration_ms
- FR-033: session_end MUST include summary

### Progress Events

- FR-034: progress MUST include current step
- FR-035: progress MUST include total steps if known
- FR-036: progress MAY include percentage
- FR-037: progress MAY include eta_seconds
- FR-038: progress MUST include message

### Status Events

- FR-039: status MUST include previous_state
- FR-040: status MUST include new_state
- FR-041: status MUST include reason

### Approval Events

- FR-042: approval_request MUST include action_type
- FR-043: approval_request MUST include context
- FR-044: approval_request MUST include risk_level
- FR-045: approval_request MUST include options
- FR-046: approval_response MUST include decision
- FR-047: approval_response MUST include source

### Action Events

- FR-048: action MUST include action_type
- FR-049: action MUST include parameters
- FR-050: action MUST include result
- FR-051: action MUST include duration_ms

### Error Events

- FR-052: error MUST include code
- FR-053: error MUST include message
- FR-054: error MUST include component
- FR-055: error MAY include stack_trace (if verbose)
- FR-056: error MAY include remediation

### Model Events

- FR-057: model_event MUST include model_id
- FR-058: model_event MUST include operation
- FR-059: model_event MAY include tokens_used
- FR-060: model_event MAY include latency_ms

### File Events

- FR-061: file_event MUST include operation
- FR-062: file_event MUST include path
- FR-063: file_event MAY include diff (if write)
- FR-064: file_event MUST include result

### Streaming Behavior

- FR-065: Events MUST be flushed immediately
- FR-066: No buffering across events
- FR-067: Stdout MUST be line-buffered
- FR-068: Long-running ops MUST emit progress

### Schema Management

- FR-069: Schema version in every event
- FR-070: Breaking changes = major version bump
- FR-071: New fields = minor version bump
- FR-072: Current schema version: 1.0.0

### Secret Redaction

- FR-073: Secrets MUST be redacted in events
- FR-074: API keys MUST show only last 4 chars
- FR-075: Passwords MUST be replaced with ***
- FR-076: Paths MUST be preserved (not secrets)

---

## Non-Functional Requirements

### Performance

- NFR-001: Event emission MUST NOT block operations
- NFR-002: Serialization MUST complete < 1ms per event
- NFR-003: Memory per event MUST be < 10KB
- NFR-004: Event throughput MUST support 1000/sec

### Reliability

- NFR-005: Partial output MUST be parseable
- NFR-006: Truncated lines MUST be detectable
- NFR-007: Stream interruption MUST NOT corrupt

### Security

- NFR-008: No secrets in event content
- NFR-009: File content MUST be optional (config)
- NFR-010: Stack traces MUST require verbose flag

### Compatibility

- NFR-011: JSON MUST be RFC 8259 compliant
- NFR-012: Unicode MUST be properly escaped
- NFR-013: All platforms MUST emit identical format

### Observability

- NFR-014: Event emission MUST be logged
- NFR-015: Serialization errors MUST be logged
- NFR-016: Performance metrics MUST be available

---

## User Manual Documentation

### Overview

JSONL event stream mode provides machine-readable output for all Acode CLI operations. Enable this mode for automation, CI/CD integration, and programmatic control.

### Quick Start

```bash
# Enable JSONL mode
$ acode run --json "Add validation"

# Output example (one event per line):
{"type":"session_start","timestamp":"2024-01-15T10:30:00.123Z","event_id":"evt_001","run_id":"abc123","command":"run","schema_version":"1.0.0"}
{"type":"progress","timestamp":"2024-01-15T10:30:01.456Z","event_id":"evt_002","step":1,"total":5,"message":"Analyzing task"}
{"type":"action","timestamp":"2024-01-15T10:30:05.789Z","event_id":"evt_003","action_type":"file_write","path":"src/validation.ts","result":"success"}
{"type":"session_end","timestamp":"2024-01-15T10:30:10.012Z","event_id":"evt_004","exit_code":0,"duration_ms":9889}
```

### Enabling JSONL Mode

```bash
# Via command-line flag
$ acode run --json "task"

# Via environment variable
$ export ACODE_JSON=1
$ acode run "task"

# Combine with other options
$ acode run --json --verbose "task"
```

### Event Types

#### session_start

Emitted when a command begins:

```json
{
  "type": "session_start",
  "timestamp": "2024-01-15T10:30:00.123Z",
  "event_id": "evt_001",
  "run_id": "abc123",
  "command": "run",
  "args": ["Add validation"],
  "schema_version": "1.0.0"
}
```

#### session_end

Emitted when a command completes:

```json
{
  "type": "session_end",
  "timestamp": "2024-01-15T10:30:10.012Z",
  "event_id": "evt_099",
  "exit_code": 0,
  "duration_ms": 9889,
  "summary": {
    "actions_taken": 5,
    "files_modified": 3,
    "approvals_requested": 2
  }
}
```

#### progress

Emitted for long-running operations:

```json
{
  "type": "progress",
  "timestamp": "2024-01-15T10:30:01.456Z",
  "event_id": "evt_002",
  "step": 2,
  "total": 5,
  "percentage": 40,
  "message": "Planning actions",
  "eta_seconds": 15
}
```

#### status

Emitted on state changes:

```json
{
  "type": "status",
  "timestamp": "2024-01-15T10:30:02.789Z",
  "event_id": "evt_003",
  "previous_state": "PLANNING",
  "new_state": "EXECUTING",
  "reason": "Plan approved"
}
```

#### approval_request

Emitted when approval is needed:

```json
{
  "type": "approval_request",
  "timestamp": "2024-01-15T10:30:03.012Z",
  "event_id": "evt_004",
  "action_type": "file_write",
  "context": {
    "file": "src/config.ts",
    "changes": "+15 -3 lines"
  },
  "risk_level": "medium",
  "options": ["approve", "reject", "modify"]
}
```

#### approval_response

Emitted when approval is given:

```json
{
  "type": "approval_response",
  "timestamp": "2024-01-15T10:30:05.345Z",
  "event_id": "evt_005",
  "correlation_id": "evt_004",
  "decision": "approve",
  "source": "cli_prompt"
}
```

#### action

Emitted for each action taken:

```json
{
  "type": "action",
  "timestamp": "2024-01-15T10:30:06.678Z",
  "event_id": "evt_006",
  "action_type": "file_write",
  "parameters": {
    "path": "src/validation.ts"
  },
  "result": "success",
  "duration_ms": 45
}
```

#### error

Emitted on errors:

```json
{
  "type": "error",
  "timestamp": "2024-01-15T10:30:07.901Z",
  "event_id": "evt_007",
  "code": "ACODE-FILE-001",
  "message": "File not found: src/missing.ts",
  "component": "FileSystem",
  "remediation": "Check file path and permissions"
}
```

#### warning

Emitted on warnings:

```json
{
  "type": "warning",
  "timestamp": "2024-01-15T10:30:08.234Z",
  "event_id": "evt_008",
  "code": "ACODE-WARN-001",
  "message": "Large file detected, may be slow",
  "component": "FileSystem"
}
```

#### model_event

Emitted for model operations:

```json
{
  "type": "model_event",
  "timestamp": "2024-01-15T10:30:09.567Z",
  "event_id": "evt_009",
  "model_id": "llama3.2:7b",
  "operation": "inference",
  "tokens_used": 1500,
  "latency_ms": 2340
}
```

#### file_event

Emitted for file operations:

```json
{
  "type": "file_event",
  "timestamp": "2024-01-15T10:30:10.890Z",
  "event_id": "evt_010",
  "operation": "write",
  "path": "src/validation.ts",
  "result": "success",
  "diff": {
    "lines_added": 15,
    "lines_removed": 3
  }
}
```

### Parsing JSONL

#### Bash with jq

```bash
# Filter specific event types
$ acode run --json "task" | jq -c 'select(.type == "progress")'

# Extract error messages
$ acode run --json "task" | jq -c 'select(.type == "error") | .message'

# Get final exit code
$ acode run --json "task" | jq -c 'select(.type == "session_end") | .exit_code'
```

#### Python

```python
import json
import subprocess
import sys

proc = subprocess.Popen(
    ["acode", "run", "--json", "Add validation"],
    stdout=subprocess.PIPE,
    text=True
)

for line in proc.stdout:
    event = json.loads(line)
    
    if event["type"] == "progress":
        print(f"Progress: {event['percentage']}%")
    
    elif event["type"] == "error":
        print(f"Error: {event['message']}", file=sys.stderr)
    
    elif event["type"] == "session_end":
        sys.exit(event["exit_code"])
```

#### Node.js

```javascript
const { spawn } = require('child_process');
const readline = require('readline');

const proc = spawn('acode', ['run', '--json', 'Add validation']);

const rl = readline.createInterface({
  input: proc.stdout,
  crlfDelay: Infinity
});

rl.on('line', (line) => {
  const event = JSON.parse(line);
  
  switch (event.type) {
    case 'progress':
      console.log(`Progress: ${event.percentage}%`);
      break;
    case 'error':
      console.error(`Error: ${event.message}`);
      break;
    case 'session_end':
      process.exit(event.exit_code);
  }
});
```

### Correlation IDs

Events can be correlated using `correlation_id`:

```bash
# Find approval response for a specific request
$ acode run --json "task" | jq -c '
  select(.type == "approval_request" or .type == "approval_response")
  | {type, event_id, correlation_id}
'
```

### Schema Versioning

Event schemas are versioned. Check `schema_version` for compatibility:

```bash
$ acode run --json "task" | jq -c 'select(.type == "session_start") | .schema_version'
# Output: "1.0.0"
```

### Stdout vs Stderr

In JSONL mode:
- **stdout**: JSONL events only
- **stderr**: Human-readable logs

```bash
# Events to file, logs to terminal
$ acode run --json "task" > events.jsonl 2>&1

# Events to parser, suppress logs
$ acode run --json --quiet "task" | my-parser

# Events to parser, logs to file
$ acode run --json "task" 2>debug.log | my-parser
```

### Configuration

#### Config File

```yaml
# .agent/config.yml
output:
  jsonl:
    include_file_content: false   # Omit file content from events
    include_stack_traces: false   # Omit stack traces
    pretty_print: false           # Single-line events (default)
```

#### Environment Variables

```bash
# Enable JSONL mode
export ACODE_JSON=1

# Include file content in events
export ACODE_JSONL_INCLUDE_CONTENT=1
```

### Best Practices

1. **Use jq for exploration**: Learn event structure interactively
2. **Filter early**: Process only needed event types
3. **Handle errors gracefully**: Check for error events
4. **Check schema version**: Ensure compatibility
5. **Use correlation IDs**: Track related events
6. **Preserve stderr**: Don't discard diagnostic logs

### Troubleshooting

#### Invalid JSON

**Problem:** Parser fails on a line.

**Possible causes:**
1. Mixed mode output (JSONL and human-readable)
2. Truncated event
3. Interleaved stderr

**Solution:**
```bash
# Ensure pure JSONL
$ acode run --json --quiet "task" 2>/dev/null | jq -c .
```

#### Missing Events

**Problem:** Expected events not appearing.

**Possible causes:**
1. Events going to stderr
2. Buffering issues
3. Filtered by verbosity level

**Solution:**
```bash
# Enable verbose for more events
$ acode run --json --verbose "task"
```

#### Events Out of Order

**Problem:** Timestamps not sequential.

**Explanation:** Events are emitted from concurrent operations. Use `event_id` for ordering.

---

## Acceptance Criteria

### JSONL Mode Activation

- [ ] AC-001: --json flag enables JSONL mode
- [ ] AC-002: ACODE_JSON=1 enables JSONL mode
- [ ] AC-003: Events go to stdout
- [ ] AC-004: One event per line
- [ ] AC-005: Each line is valid JSON
- [ ] AC-006: Lines end with newline
- [ ] AC-007: Logs go to stderr
- [ ] AC-008: No non-JSON on stdout

### Event Structure

- [ ] AC-009: "type" field present
- [ ] AC-010: "timestamp" field present
- [ ] AC-011: ISO 8601 UTC format
- [ ] AC-012: "event_id" field present
- [ ] AC-013: Event IDs unique per session
- [ ] AC-014: Schema version present

### Session Events

- [ ] AC-015: session_start includes run_id
- [ ] AC-016: session_start includes command
- [ ] AC-017: session_start includes schema_version
- [ ] AC-018: session_end includes exit_code
- [ ] AC-019: session_end includes duration_ms
- [ ] AC-020: session_end includes summary

### Progress Events

- [ ] AC-021: Includes current step
- [ ] AC-022: Includes total if known
- [ ] AC-023: Includes message
- [ ] AC-024: Emitted for long operations

### Status Events

- [ ] AC-025: Includes previous_state
- [ ] AC-026: Includes new_state
- [ ] AC-027: Includes reason

### Approval Events

- [ ] AC-028: Request includes action_type
- [ ] AC-029: Request includes context
- [ ] AC-030: Request includes risk_level
- [ ] AC-031: Request includes options
- [ ] AC-032: Response includes decision
- [ ] AC-033: Response includes source

### Action Events

- [ ] AC-034: Includes action_type
- [ ] AC-035: Includes parameters
- [ ] AC-036: Includes result
- [ ] AC-037: Includes duration_ms

### Error Events

- [ ] AC-038: Includes code
- [ ] AC-039: Includes message
- [ ] AC-040: Includes component
- [ ] AC-041: Stack trace only if verbose

### Model Events

- [ ] AC-042: Includes model_id
- [ ] AC-043: Includes operation
- [ ] AC-044: Includes tokens if applicable
- [ ] AC-045: Includes latency_ms

### File Events

- [ ] AC-046: Includes operation
- [ ] AC-047: Includes path
- [ ] AC-048: Includes result
- [ ] AC-049: Diff optional

### Streaming

- [ ] AC-050: Events flushed immediately
- [ ] AC-051: No cross-event buffering
- [ ] AC-052: Line-buffered stdout
- [ ] AC-053: Progress for long ops

### Schema

- [ ] AC-054: Version in every event
- [ ] AC-055: Semver format
- [ ] AC-056: Version 1.0.0 current

### Secret Redaction

- [ ] AC-057: Secrets redacted
- [ ] AC-058: API keys last 4 chars only
- [ ] AC-059: Passwords replaced

### Performance

- [ ] AC-060: Emission non-blocking
- [ ] AC-061: Serialization < 1ms
- [ ] AC-062: Memory < 10KB per event
- [ ] AC-063: 1000 events/sec supported

### Compatibility

- [ ] AC-064: RFC 8259 compliant JSON
- [ ] AC-065: Unicode properly escaped
- [ ] AC-066: Cross-platform identical

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/CLI/JSONL/
├── EventSerializerTests.cs
│   ├── Should_Serialize_All_Event_Types()
│   ├── Should_Include_Required_Fields()
│   ├── Should_Generate_Unique_EventIds()
│   ├── Should_Format_Timestamps_ISO8601()
│   └── Should_Redact_Secrets()
│
├── EventEmitterTests.cs
│   ├── Should_Write_To_Stdout()
│   ├── Should_Flush_After_Each_Event()
│   ├── Should_Emit_Newline()
│   └── Should_Handle_Concurrent_Events()
│
├── JSONLModeTests.cs
│   ├── Should_Enable_Via_Flag()
│   ├── Should_Enable_Via_EnvVar()
│   └── Should_Not_Affect_Stderr()
│
└── SecretRedactionTests.cs
    ├── Should_Redact_API_Keys()
    ├── Should_Redact_Passwords()
    └── Should_Preserve_Paths()
```

### Integration Tests

```
Tests/Integration/CLI/JSONL/
├── EventStreamTests.cs
│   ├── Should_Emit_Session_Events()
│   ├── Should_Emit_Progress_Events()
│   ├── Should_Emit_Error_Events()
│   └── Should_Correlate_Approval_Events()
│
└── ParsingTests.cs
    ├── Should_Be_Parseable_With_jq()
    ├── Should_Be_Parseable_With_Python()
    └── Should_Handle_Concurrent_Events()
```

### E2E Tests

```
Tests/E2E/CLI/JSONL/
├── FullRunTests.cs
│   ├── Should_Produce_Complete_Stream()
│   ├── Should_Handle_Errors_Gracefully()
│   └── Should_Work_With_Pipes()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Event serialization | 0.5ms | 1ms |
| Event emission | 0.1ms | 0.5ms |
| Memory per event | 5KB | 10KB |
| Events per second | 2000 | 1000 min |

### Regression Tests

- Event format after schema update
- Performance after field additions
- Compatibility after serializer change

---

## User Verification Steps

### Scenario 1: Enable JSONL

1. Run `acode run --json "test"`
2. Verify: Output is JSONL
3. Verify: Each line parses as JSON

### Scenario 2: Event Types

1. Run `acode run --json "task" | jq -c .type`
2. Verify: session_start first
3. Verify: session_end last

### Scenario 3: Parse with jq

1. Run `acode run --json "task" | jq -c 'select(.type=="progress")'`
2. Verify: Only progress events
3. Verify: Valid JSON

### Scenario 4: Session Events

1. Run `acode run --json "task"`
2. Verify: session_start has run_id
3. Verify: session_end has exit_code

### Scenario 5: Error Event

1. Cause an error
2. Verify: error event emitted
3. Verify: Contains code and message

### Scenario 6: Progress Events

1. Run long operation with --json
2. Verify: Progress events emitted
3. Verify: Step/total included

### Scenario 7: Secret Redaction

1. Run with API key in config
2. Verify: Key not in events
3. Verify: Redacted form only

### Scenario 8: Stderr Separation

1. Run `acode run --json "task" 2>log.txt`
2. Verify: stdout is pure JSONL
3. Verify: stderr has logs

### Scenario 9: Environment Variable

1. Set ACODE_JSON=1
2. Run `acode run "task"`
3. Verify: JSONL output

### Scenario 10: Schema Version

1. Run `acode run --json "task"`
2. Verify: schema_version in session_start
3. Verify: Format is semver

### Scenario 11: Correlation IDs

1. Run operation requiring approval
2. Find approval_request event_id
3. Verify: approval_response has matching correlation_id

### Scenario 12: Concurrent Events

1. Run operation with parallel work
2. Parse all events
3. Verify: All events valid JSON

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.CLI/
├── JSONL/
│   ├── IEventEmitter.cs
│   ├── EventEmitter.cs
│   ├── IEventSerializer.cs
│   ├── EventSerializer.cs
│   ├── EventIdGenerator.cs
│   └── SecretRedactor.cs
│
├── Events/
│   ├── BaseEvent.cs
│   ├── SessionStartEvent.cs
│   ├── SessionEndEvent.cs
│   ├── ProgressEvent.cs
│   ├── StatusEvent.cs
│   ├── ApprovalRequestEvent.cs
│   ├── ApprovalResponseEvent.cs
│   ├── ActionEvent.cs
│   ├── ErrorEvent.cs
│   ├── WarningEvent.cs
│   ├── ModelEvent.cs
│   └── FileEvent.cs
│
└── Output/
    ├── JSONLOutputFormatter.cs
    └── OutputStreamManager.cs
```

### BaseEvent

```csharp
namespace AgenticCoder.CLI.Events;

public abstract record BaseEvent
{
    public required string Type { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string EventId { get; init; }
    public string? CorrelationId { get; init; }
    public string SchemaVersion => "1.0.0";
}
```

### Event Types

```csharp
public sealed record SessionStartEvent : BaseEvent
{
    public required string RunId { get; init; }
    public required string Command { get; init; }
    public IReadOnlyList<string>? Args { get; init; }
}

public sealed record SessionEndEvent : BaseEvent
{
    public required int ExitCode { get; init; }
    public required long DurationMs { get; init; }
    public required SessionSummary Summary { get; init; }
}

public sealed record ProgressEvent : BaseEvent
{
    public required int Step { get; init; }
    public int? Total { get; init; }
    public int? Percentage { get; init; }
    public int? EtaSeconds { get; init; }
    public required string Message { get; init; }
}

public sealed record ErrorEvent : BaseEvent
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public required string Component { get; init; }
    public string? StackTrace { get; init; }
    public string? Remediation { get; init; }
}
```

### IEventEmitter

```csharp
namespace AgenticCoder.CLI.JSONL;

public interface IEventEmitter
{
    void Emit(BaseEvent @event);
    void Configure(EventEmitterOptions options);
}

public sealed record EventEmitterOptions(
    bool IncludeFileContent = false,
    bool IncludeStackTraces = false,
    bool PrettyPrint = false);
```

### IEventSerializer

```csharp
namespace AgenticCoder.CLI.JSONL;

public interface IEventSerializer
{
    string Serialize(BaseEvent @event);
}
```

### EventIdGenerator

```csharp
namespace AgenticCoder.CLI.JSONL;

public sealed class EventIdGenerator
{
    private int _counter;
    private readonly string _prefix;
    
    public EventIdGenerator(string? prefix = "evt")
    {
        _prefix = prefix ?? "evt";
    }
    
    public string Next()
    {
        var count = Interlocked.Increment(ref _counter);
        return $"{_prefix}_{count:D3}";
    }
}
```

### SecretRedactor

```csharp
namespace AgenticCoder.CLI.JSONL;

public sealed class SecretRedactor
{
    public string Redact(string value, string type);
    public bool IsSecret(string key);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-JSONL-001 | Serialization failed |
| ACODE-JSONL-002 | Event emission failed |
| ACODE-JSONL-003 | Invalid event type |

### Logging Fields

```json
{
  "event": "event_emitted",
  "event_type": "progress",
  "event_id": "evt_001",
  "serialization_ms": 0.5,
  "size_bytes": 256
}
```

### Implementation Checklist

1. [ ] Create BaseEvent abstract record
2. [ ] Create all event type records
3. [ ] Implement EventIdGenerator
4. [ ] Implement EventSerializer
5. [ ] Implement SecretRedactor
6. [ ] Implement EventEmitter
7. [ ] Add stdout line buffering
8. [ ] Implement OutputStreamManager
9. [ ] Add --json flag handling
10. [ ] Add ACODE_JSON env handling
11. [ ] Write serialization unit tests
12. [ ] Write emission unit tests
13. [ ] Write redaction tests
14. [ ] Write integration tests
15. [ ] Add performance benchmarks

### Validation Checklist Before Merge

- [ ] All event types serialize correctly
- [ ] Event IDs unique per session
- [ ] Timestamps are ISO 8601 UTC
- [ ] Schema version in all events
- [ ] Secrets properly redacted
- [ ] Events flush immediately
- [ ] Serialization < 1ms
- [ ] Parseable by jq
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** Event types and serialization
2. **Phase 2:** Event emitter and buffering
3. **Phase 3:** Secret redaction
4. **Phase 4:** Integration with commands
5. **Phase 5:** Performance tuning
6. **Phase 6:** Documentation and examples

---

**End of Task 010.b Specification**