# Task 018: Structured Command Runner

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 050 (Workspace Database), Task 002 (Config Contract)  

---

## Description

Task 018 implements the structured command runner. The agent executes commands to verify code changes. Every command must be executed consistently with captured output.

Command execution is fundamental. The agent runs compilers, test frameworks, linters. Every command produces output that guides the agent's next action.

Structure means consistency. Every command has the same input format. Every result has the same output format. This enables reliable processing.

Input structure includes: command string, arguments, working directory, environment variables, timeout, resource limits. All inputs are validated before execution.

Output structure includes: stdout, stderr, exit code, duration, resource usage. All outputs are captured and stored.

The command runner integrates with Task 050's workspace database. Every execution is recorded. Audit trails are complete. Run history is queryable.

Audit events use correlation fields. `run_id` identifies the agent run. `session_id` identifies the session. `task_id` identifies the task. These enable tracing.

The runner respects Task 001 operating modes. Local-only mode executes directly. Docker mode delegates to Task 020. Mode selection is automatic.

Error handling is comprehensive. Commands can fail, timeout, or crash. Each failure mode has a defined response. The agent receives actionable feedback.

Task 018.a handles output capture details. Task 018.b handles working directory and environment. Task 018.c handles artifact logging.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Command | Executable with arguments |
| Working Directory | Command execution path |
| Environment | Variables for command |
| Timeout | Maximum execution time |
| Exit Code | Command return value |
| Stdout | Standard output stream |
| Stderr | Standard error stream |
| Resource Limit | CPU/memory constraint |
| Audit Event | Recorded execution |
| Correlation ID | Tracing identifier |
| Run ID | Agent run identifier |
| Session ID | User session identifier |
| Task ID | Agent task identifier |
| Step ID | Execution step identifier |

---

## Out of Scope

The following items are explicitly excluded from Task 018:

- **Output capture details** - See Task 018.a
- **Working directory details** - See Task 018.b
- **Artifact logging** - See Task 018.c
- **Docker execution** - See Task 020
- **Language-specific runners** - See Task 019
- **Real-time streaming** - Batch capture only
- **Remote execution** - Local only
- **Parallel execution** - Sequential only

---

## Functional Requirements

### Command Model

- FR-001: Define ICommand interface
- FR-002: Define Command record
- FR-003: Store command string
- FR-004: Store arguments list
- FR-005: Store working directory
- FR-006: Store environment variables
- FR-007: Store timeout value
- FR-008: Store resource limits
- FR-009: Validate command before execution

### Executor Interface

- FR-010: Define ICommandExecutor interface
- FR-011: ExecuteAsync method
- FR-012: Accept Command parameter
- FR-013: Accept ExecutionOptions parameter
- FR-014: Accept CancellationToken
- FR-015: Return CommandResult

### Command Result

- FR-016: Define CommandResult record
- FR-017: Store stdout content
- FR-018: Store stderr content
- FR-019: Store exit code
- FR-020: Store start time
- FR-021: Store end time
- FR-022: Store duration
- FR-023: Store success flag
- FR-024: Store timeout flag
- FR-025: Store error message

### Process Execution

- FR-026: Start process
- FR-027: Configure process start info
- FR-028: Redirect stdout
- FR-029: Redirect stderr
- FR-030: Wait for exit
- FR-031: Handle timeout
- FR-032: Kill on timeout
- FR-033: Dispose process

### Execution Options

- FR-034: Default timeout
- FR-035: Override timeout
- FR-036: Environment merge mode
- FR-037: Shell mode option
- FR-038: Capture mode option

### Audit Recording

- FR-039: Record execution start
- FR-040: Record execution end
- FR-041: Store run_id
- FR-042: Store session_id
- FR-043: Store task_id
- FR-044: Store step_id
- FR-045: Store tool_call_id
- FR-046: Store worktree_id
- FR-047: Store repo_sha
- FR-048: Persist to workspace DB

### Error Handling

- FR-049: Handle process start failure
- FR-050: Handle timeout
- FR-051: Handle exit code non-zero
- FR-052: Handle output too large
- FR-053: Map errors to result
- FR-054: Log errors with context

### Validation

- FR-055: Validate command not empty
- FR-056: Validate working directory exists
- FR-057: Validate timeout positive
- FR-058: Sanitize command string

---

## Non-Functional Requirements

### Performance

- NFR-001: Process start < 50ms overhead
- NFR-002: Output capture < 10ms/MB
- NFR-003: Audit write < 5ms

### Reliability

- NFR-004: Handle process crashes
- NFR-005: Handle zombie processes
- NFR-006: Handle large output
- NFR-007: Consistent state on failure

### Security

- NFR-008: No command injection
- NFR-009: Secrets redacted in logs
- NFR-010: Limited shell access
- NFR-011: Environment sanitization

### Auditability

- NFR-012: All executions logged
- NFR-013: Correlation IDs present
- NFR-014: Timestamps accurate
- NFR-015: Query history works

---

## User Manual Documentation

### Overview

The structured command runner executes commands with consistent input/output handling. Every execution is recorded for audit and debugging.

### Configuration

```yaml
# .agent/config.yml
execution:
  # Default command timeout (seconds)
  default_timeout_seconds: 300
  
  # Maximum output size (KB)
  max_output_kb: 1024
  
  # Shell execution mode
  use_shell: false
  
  # Environment handling
  environment:
    # inherit, replace, merge
    mode: inherit
    
  # Audit settings
  audit:
    # Store in database
    enabled: true
    
    # Retain days
    retention_days: 30
```

### CLI Commands

```bash
# Execute a command
acode exec "dotnet build"

# Execute with timeout
acode exec "npm test" --timeout 60

# Execute in directory
acode exec "make" --cwd /project/src

# Execute with environment
acode exec "node app.js" --env "NODE_ENV=test"

# View execution history
acode runs list

# View execution details
acode runs show <run-id>
```

### Execution Result

```json
{
  "id": "exec-001",
  "command": "dotnet build",
  "workingDirectory": "/project",
  "exitCode": 0,
  "success": true,
  "timedOut": false,
  "startTime": "2024-01-15T10:30:00Z",
  "endTime": "2024-01-15T10:30:05Z",
  "durationMs": 5000,
  "stdout": "Build succeeded...",
  "stderr": "",
  "correlationIds": {
    "runId": "run-123",
    "sessionId": "sess-456",
    "taskId": "task-789"
  }
}
```

### Audit Record

```json
{
  "eventType": "command_execution",
  "timestamp": "2024-01-15T10:30:00Z",
  "runId": "run-123",
  "sessionId": "sess-456",
  "taskId": "task-789",
  "stepId": "step-001",
  "toolCallId": "tool-001",
  "worktreeId": "main",
  "repoSha": "abc123",
  "command": "dotnet build",
  "exitCode": 0,
  "durationMs": 5000
}
```

### Troubleshooting

#### Command Not Found

**Problem:** Command executable not found

**Solutions:**
1. Verify command is in PATH
2. Use absolute path
3. Check shell mode setting
4. Verify working directory

#### Timeout

**Problem:** Command exceeds timeout

**Solutions:**
1. Increase timeout value
2. Check for infinite loops
3. Check for blocking I/O
4. Kill manually if needed

#### Large Output

**Problem:** Output exceeds limit

**Solutions:**
1. Increase max_output_kb
2. Redirect to file
3. Filter output

---

## Acceptance Criteria

### Model

- [ ] AC-001: Command model complete
- [ ] AC-002: Result model complete
- [ ] AC-003: Validation works

### Execution

- [ ] AC-004: Commands execute
- [ ] AC-005: Output captured
- [ ] AC-006: Exit code returned
- [ ] AC-007: Timeout works

### Audit

- [ ] AC-008: Events recorded
- [ ] AC-009: Correlation IDs present
- [ ] AC-010: Database persisted

### Error Handling

- [ ] AC-011: Failures handled
- [ ] AC-012: Timeouts handled
- [ ] AC-013: Errors logged

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Execution/
├── CommandTests.cs
│   ├── Should_Validate_Command()
│   └── Should_Reject_Empty()
│
├── CommandExecutorTests.cs
│   ├── Should_Execute_Command()
│   ├── Should_Capture_Output()
│   ├── Should_Return_Exit_Code()
│   └── Should_Handle_Timeout()
│
├── CommandResultTests.cs
│   ├── Should_Store_Output()
│   └── Should_Calculate_Duration()
│
└── AuditRecordTests.cs
    ├── Should_Include_Correlation_Ids()
    └── Should_Persist_To_Db()
```

### Integration Tests

```
Tests/Integration/Execution/
├── CommandExecutorIntegrationTests.cs
│   ├── Should_Execute_Real_Command()
│   └── Should_Handle_Real_Timeout()
│
└── AuditIntegrationTests.cs
    └── Should_Query_History()
```

### E2E Tests

```
Tests/E2E/Execution/
├── ExecutionE2ETests.cs
│   ├── Should_Execute_Via_CLI()
│   └── Should_Show_History()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Process start overhead | 30ms | 50ms |
| Output capture 1MB | 5ms | 10ms |
| Audit write | 2ms | 5ms |

---

## User Verification Steps

### Scenario 1: Execute Command

1. Run `acode exec "echo hello"`
2. Verify: Output shows "hello"

### Scenario 2: Capture Exit Code

1. Run `acode exec "exit 42"`
2. Verify: Exit code 42 returned

### Scenario 3: Timeout

1. Run `acode exec "sleep 60" --timeout 2`
2. Verify: Timeout after 2 seconds

### Scenario 4: View History

1. Execute several commands
2. Run `acode runs list`
3. Verify: All runs shown

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Execution/
│   ├── ICommand.cs
│   ├── Command.cs
│   └── CommandResult.cs
│
src/AgenticCoder.Application/
├── Execution/
│   ├── ICommandExecutor.cs
│   └── ExecutionOptions.cs
│
src/AgenticCoder.Infrastructure/
├── Execution/
│   ├── CommandExecutor.cs
│   ├── ProcessRunner.cs
│   └── ExecutionAuditRepository.cs
```

### ICommandExecutor Interface

```csharp
namespace AgenticCoder.Application.Execution;

public interface ICommandExecutor
{
    Task<CommandResult> ExecuteAsync(
        Command command,
        ExecutionOptions? options = null,
        CancellationToken ct = default);
}
```

### Command Record

```csharp
namespace AgenticCoder.Domain.Execution;

public record Command
{
    public required string Executable { get; init; }
    public IReadOnlyList<string> Arguments { get; init; } = [];
    public string? WorkingDirectory { get; init; }
    public IReadOnlyDictionary<string, string>? Environment { get; init; }
    public TimeSpan? Timeout { get; init; }
}
```

### CommandResult Record

```csharp
public record CommandResult
{
    public required string Stdout { get; init; }
    public required string Stderr { get; init; }
    public required int ExitCode { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public TimeSpan Duration => EndTime - StartTime;
    public bool Success => ExitCode == 0;
    public bool TimedOut { get; init; }
    public string? Error { get; init; }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-EXE-001 | Command not found |
| ACODE-EXE-002 | Timeout exceeded |
| ACODE-EXE-003 | Process failed |
| ACODE-EXE-004 | Output too large |

### Implementation Checklist

1. [ ] Create command model
2. [ ] Create result model
3. [ ] Create executor interface
4. [ ] Implement process runner
5. [ ] Add output capture
6. [ ] Add timeout handling
7. [ ] Add audit recording
8. [ ] Add CLI commands

### Rollout Plan

1. **Phase 1:** Command model
2. **Phase 2:** Process execution
3. **Phase 3:** Output capture
4. **Phase 4:** Audit recording
5. **Phase 5:** CLI integration

---

**End of Task 018 Specification**