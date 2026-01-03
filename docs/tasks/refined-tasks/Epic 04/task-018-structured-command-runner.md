# Task 018: Structured Command Runner

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 050 (Workspace Database), Task 002 (Config Contract)  

---

## Description

### Business Value

The Structured Command Runner is the execution foundation that enables Agentic Coding Bot to verify, test, and build code changes. This abstraction is critically important because:

1. **Autonomous Verification:** The agent MUST verify its own code changes through compilation, testing, and linting. Without a reliable command execution layer, the agent cannot validate that its modifications work correctly. This is the difference between an agent that "hopes" its code works and one that "knows" it works.

2. **Reproducibility:** Every command execution MUST be reproducible. When debugging agent behavior, developers need to understand exactly what commands were run, with what parameters, in what environment, and what the results were. The structured approach ensures complete reproducibility.

3. **Auditability:** In enterprise environments, all agent actions MUST be auditable. The command runner creates a complete audit trail of every external process invoked—enabling compliance, debugging, and cost tracking.

4. **Security Boundary:** Commands execute external processes that can have side effects. The command runner provides the control point for enforcing timeouts, resource limits, and execution policies that protect the host system.

5. **Error Recovery:** When commands fail, the agent needs actionable feedback. Raw stderr isn't enough—the agent needs structured error information that enables intelligent retry or fallback behavior.

6. **Performance Optimization:** By capturing execution metrics (duration, memory, CPU), the command runner enables performance analysis and optimization of the agent's build/test cycle.

### Scope

This task defines the complete command execution infrastructure:

1. **Command Model:** The `Command` record defining executable, arguments, working directory, environment variables, timeout, and resource limits. Immutable and validated.

2. **ICommandExecutor Interface:** The primary contract for command execution. Defines `ExecuteAsync` with structured input and output. All execution paths (local, Docker) implement this interface.

3. **CommandResult Model:** The structured output including stdout, stderr, exit code, duration, success flag, timeout flag, and error details. Enables reliable result processing.

4. **Process Execution Engine:** The core implementation that starts processes, captures output streams, handles timeouts, and manages process lifecycle.

5. **Execution Options:** Configurable parameters for timeout override, environment merge mode, shell execution mode, and output capture mode.

6. **Audit Integration:** Every command execution is recorded to the workspace database (Task 050) with full correlation IDs for tracing.

7. **CLI Integration:** `acode exec` command for manual command execution and `acode runs` commands for viewing execution history.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 002 (Config) | Configuration | Execution settings in `.agent/config.yml` under `execution` section |
| Task 003 (DI) | Dependency Injection | ICommandExecutor registered as singleton service |
| Task 001 (Modes) | Mode Selection | Operating mode determines local vs Docker execution path |
| Task 050 (Database) | Audit Persistence | All executions recorded with run_id, session_id, task_id, step_id, tool_call_id |
| Task 020 (Docker) | Sandbox Execution | Docker mode delegates execution to sandbox containers |
| Task 019 (Language) | Build Commands | Language runners use command executor for compilation/testing |
| Task 003.c (Audit) | Event Logging | Execution events published to audit system |
| Task 011 (Session) | Context | Session provides run_id and session_id for correlation |
| Task 009 (CLI) | Commands | `acode exec` and `acode runs` commands exposed |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Executable not found | Command fails immediately | Validate executable exists before execution, clear error message |
| Working directory missing | Command fails immediately | Validate directory exists before execution |
| Permission denied | Cannot start process | Check permissions at startup, report in health check |
| Timeout exceeded | Process killed mid-execution | Graceful timeout with process tree kill, timeout flag in result |
| Process crash | Non-zero exit code | Capture all available output, map to structured error |
| Output buffer overflow | Memory exhaustion | Configurable output limits with truncation |
| Zombie process | Resource leak | Process handle cleanup in finally block, health monitor |
| Concurrent execution limit | Resource contention | Semaphore-based concurrency control |
| Environment variable conflict | Unexpected behavior | Clear merge mode semantics (inherit, replace, merge) |
| Shell injection attempt | Security violation | Input validation, no shell mode by default |
| Long-running command | Resource consumption | Mandatory timeout enforcement, no infinite waits |

### Assumptions

1. The host system has a working process subsystem (Windows Process, Linux fork/exec)
2. Executables are available in PATH or specified with absolute paths
3. The agent has permission to start processes
4. The file system is accessible for working directory resolution
5. Environment variables can be inherited and modified
6. Process output is UTF-8 encoded (or system default encoding)
7. Process trees can be killed on timeout (important for npm, dotnet)
8. The workspace database (Task 050) is available for audit persistence
9. Commands complete in reasonable time (timeout enforced)
10. Output size is bounded (configurable limit with truncation)

### Security Considerations

The command executor is a critical security component. External process execution presents significant risk:

1. **No Shell by Default:** Commands MUST NOT be executed through a shell by default. Shell execution enables injection attacks. When shell mode is required, it MUST be explicitly enabled.

2. **Argument Validation:** Command arguments MUST be passed as an array, not a concatenated string. This prevents argument injection attacks.

3. **Environment Sanitization:** Sensitive environment variables (credentials, tokens) MUST be filtered from inherited environment or redacted in logs.

4. **Working Directory Validation:** The working directory MUST be within the repository root. Commands MUST NOT be able to access parent directories.

5. **Resource Limits:** All commands MUST have a timeout. Runaway processes MUST NOT consume unbounded resources.

6. **Audit Trail:** Every command execution MUST be logged with full context. The audit trail enables forensic analysis of agent behavior.

7. **Process Isolation:** In Docker mode, commands execute in isolated containers. Local mode SHOULD consider process sandboxing where available.

8. **Output Sanitization:** Command output logged to audit MUST have potential secrets redacted (API keys, passwords matching patterns).

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

### Command Model (FR-018-01 to FR-018-20)

| ID | Requirement |
|----|-------------|
| FR-018-01 | System MUST define `ICommand` interface as the command contract |
| FR-018-02 | System MUST define `Command` record implementing `ICommand` |
| FR-018-03 | Command MUST have `Executable` property (required, non-empty string) |
| FR-018-04 | Command MUST have `Arguments` property (list of strings, may be empty) |
| FR-018-05 | Command MUST have `WorkingDirectory` property (optional, defaults to repo root) |
| FR-018-06 | Command MUST have `Environment` property (optional dictionary) |
| FR-018-07 | Command MUST have `Timeout` property (optional TimeSpan, defaults to config value) |
| FR-018-08 | Command MUST have `ResourceLimits` property (optional, for CPU/memory constraints) |
| FR-018-09 | Command MUST be immutable (record type) |
| FR-018-10 | Command MUST be serializable to JSON for audit logging |
| FR-018-11 | Command MUST validate `Executable` is not null or whitespace |
| FR-018-12 | Command MUST validate `WorkingDirectory` exists if specified |
| FR-018-13 | Command MUST validate `Timeout` is positive if specified |
| FR-018-14 | Command MUST validate `Arguments` contains no null values |
| FR-018-15 | Command MUST normalize path separators in `WorkingDirectory` |
| FR-018-16 | Command MUST support fluent builder pattern for construction |
| FR-018-17 | Command builder MUST have `WithArguments(params string[])` method |
| FR-018-18 | Command builder MUST have `WithEnvironment(string key, string value)` method |
| FR-018-19 | Command builder MUST have `WithTimeout(TimeSpan)` method |
| FR-018-20 | Command builder MUST have `WithWorkingDirectory(string)` method |

### Command Executor Interface (FR-018-21 to FR-018-35)

| ID | Requirement |
|----|-------------|
| FR-018-21 | System MUST define `ICommandExecutor` interface |
| FR-018-22 | ICommandExecutor MUST have `ExecuteAsync(Command, ExecutionOptions?, CancellationToken)` method |
| FR-018-23 | ExecuteAsync MUST return `Task<CommandResult>` |
| FR-018-24 | ExecuteAsync MUST accept optional `ExecutionOptions` for execution-time overrides |
| FR-018-25 | ExecuteAsync MUST accept `CancellationToken` for cooperative cancellation |
| FR-018-26 | ExecuteAsync MUST throw `ArgumentNullException` if command is null |
| FR-018-27 | ExecuteAsync MUST throw `CommandValidationException` if command is invalid |
| FR-018-28 | ExecuteAsync MUST NOT throw for command execution failures (return result with error) |
| FR-018-29 | ICommandExecutor MUST be registered as singleton in DI container |
| FR-018-30 | ICommandExecutor MUST support concurrent executions (thread-safe) |
| FR-018-31 | ICommandExecutor MUST enforce global concurrency limits (configurable) |
| FR-018-32 | ICommandExecutor MUST select execution mode (local/Docker) based on operating mode |
| FR-018-33 | ICommandExecutor MUST delegate to ISandbox when in Docker mode |
| FR-018-34 | ICommandExecutor MUST emit telemetry events for execution metrics |
| FR-018-35 | ICommandExecutor MUST integrate with audit system for all executions |

### Command Result Model (FR-018-36 to FR-018-55)

| ID | Requirement |
|----|-------------|
| FR-018-36 | System MUST define `CommandResult` record |
| FR-018-37 | CommandResult MUST have `Stdout` property (string, may be empty) |
| FR-018-38 | CommandResult MUST have `Stderr` property (string, may be empty) |
| FR-018-39 | CommandResult MUST have `ExitCode` property (int) |
| FR-018-40 | CommandResult MUST have `StartTime` property (DateTimeOffset) |
| FR-018-41 | CommandResult MUST have `EndTime` property (DateTimeOffset) |
| FR-018-42 | CommandResult MUST have computed `Duration` property (TimeSpan) |
| FR-018-43 | CommandResult MUST have `Success` property (true if ExitCode == 0) |
| FR-018-44 | CommandResult MUST have `TimedOut` property (bool) |
| FR-018-45 | CommandResult MUST have `Error` property (optional error message) |
| FR-018-46 | CommandResult MUST have `CorrelationIds` property (structured IDs) |
| FR-018-47 | CorrelationIds MUST include `RunId` (agent run identifier) |
| FR-018-48 | CorrelationIds MUST include `SessionId` (user session identifier) |
| FR-018-49 | CorrelationIds MUST include `TaskId` (agent task identifier) |
| FR-018-50 | CorrelationIds MUST include `StepId` (execution step identifier) |
| FR-018-51 | CorrelationIds MUST include `ToolCallId` (tool invocation identifier) |
| FR-018-52 | CommandResult MUST have `TruncationInfo` if output was truncated |
| FR-018-53 | TruncationInfo MUST include original size and truncated size |
| FR-018-54 | CommandResult MUST be immutable (record type) |
| FR-018-55 | CommandResult MUST be serializable to JSON for audit logging |

### Process Execution Engine (FR-018-56 to FR-018-75)

| ID | Requirement |
|----|-------------|
| FR-018-56 | System MUST implement `ProcessRunner` class for native process execution |
| FR-018-57 | ProcessRunner MUST use `System.Diagnostics.Process` for execution |
| FR-018-58 | ProcessRunner MUST configure `ProcessStartInfo` with executable and arguments |
| FR-018-59 | ProcessRunner MUST set `RedirectStandardOutput = true` |
| FR-018-60 | ProcessRunner MUST set `RedirectStandardError = true` |
| FR-018-61 | ProcessRunner MUST set `UseShellExecute = false` by default |
| FR-018-62 | ProcessRunner MUST set `CreateNoWindow = true` |
| FR-018-63 | ProcessRunner MUST set working directory if specified |
| FR-018-64 | ProcessRunner MUST merge environment variables according to mode |
| FR-018-65 | ProcessRunner MUST capture stdout asynchronously to StringBuilder |
| FR-018-66 | ProcessRunner MUST capture stderr asynchronously to StringBuilder |
| FR-018-67 | ProcessRunner MUST wait for process exit with timeout |
| FR-018-68 | ProcessRunner MUST kill process tree on timeout (not just parent) |
| FR-018-69 | ProcessRunner MUST dispose process resources in finally block |
| FR-018-70 | ProcessRunner MUST handle OutputDataReceived events |
| FR-018-71 | ProcessRunner MUST handle ErrorDataReceived events |
| FR-018-72 | ProcessRunner MUST call BeginOutputReadLine() for async capture |
| FR-018-73 | ProcessRunner MUST call BeginErrorReadLine() for async capture |
| FR-018-74 | ProcessRunner MUST handle process start failure gracefully |
| FR-018-75 | ProcessRunner MUST record process memory and CPU usage (best effort) |

### Execution Options (FR-018-76 to FR-018-90)

| ID | Requirement |
|----|-------------|
| FR-018-76 | System MUST define `ExecutionOptions` record |
| FR-018-77 | ExecutionOptions MUST have `TimeoutOverride` property (optional TimeSpan) |
| FR-018-78 | ExecutionOptions MUST have `EnvironmentMergeMode` property (enum) |
| FR-018-79 | EnvironmentMergeMode MUST support `Inherit` (default - inherit host env) |
| FR-018-80 | EnvironmentMergeMode MUST support `Replace` (use only specified env) |
| FR-018-81 | EnvironmentMergeMode MUST support `Merge` (host env + specified overrides) |
| FR-018-82 | ExecutionOptions MUST have `UseShell` property (bool, default false) |
| FR-018-83 | ExecutionOptions MUST have `CaptureMode` property (enum) |
| FR-018-84 | CaptureMode MUST support `All` (capture stdout and stderr) |
| FR-018-85 | CaptureMode MUST support `StdoutOnly` (capture stdout only) |
| FR-018-86 | CaptureMode MUST support `StderrOnly` (capture stderr only) |
| FR-018-87 | CaptureMode MUST support `None` (discard output) |
| FR-018-88 | ExecutionOptions MUST have `MaxOutputSizeBytes` property (optional) |
| FR-018-89 | ExecutionOptions MUST have `RedactSecrets` property (bool, default true) |
| FR-018-90 | ExecutionOptions MUST be immutable (record type) |

### Audit Recording (FR-018-91 to FR-018-110)

| ID | Requirement |
|----|-------------|
| FR-018-91 | System MUST record execution start event to audit log |
| FR-018-92 | System MUST record execution end event to audit log |
| FR-018-93 | Audit event MUST include `EventType` (command_execution_start/end) |
| FR-018-94 | Audit event MUST include `Timestamp` (DateTimeOffset, UTC) |
| FR-018-95 | Audit event MUST include `RunId` from correlation context |
| FR-018-96 | Audit event MUST include `SessionId` from correlation context |
| FR-018-97 | Audit event MUST include `TaskId` from correlation context |
| FR-018-98 | Audit event MUST include `StepId` from correlation context |
| FR-018-99 | Audit event MUST include `ToolCallId` from correlation context |
| FR-018-100 | Audit event MUST include `WorktreeId` from session context |
| FR-018-101 | Audit event MUST include `RepoSha` from session context |
| FR-018-102 | Audit event MUST include command executable and arguments |
| FR-018-103 | Audit event MUST include working directory |
| FR-018-104 | Audit event MUST include exit code on completion |
| FR-018-105 | Audit event MUST include duration on completion |
| FR-018-106 | Audit event MUST include timeout flag if applicable |
| FR-018-107 | Audit events MUST be persisted to workspace database (Task 050) |
| FR-018-108 | Audit events MUST be queryable by run_id, session_id, task_id |
| FR-018-109 | Audit events MUST support retention policy (configurable days) |
| FR-018-110 | Audit events MUST redact sensitive environment variables |

### Error Handling (FR-018-111 to FR-018-125)

| ID | Requirement |
|----|-------------|
| FR-018-111 | System MUST handle process start failure (executable not found) |
| FR-018-112 | System MUST handle permission denied errors |
| FR-018-113 | System MUST handle working directory not found errors |
| FR-018-114 | System MUST handle timeout expiration gracefully |
| FR-018-115 | System MUST handle process crash (access violation, etc.) |
| FR-018-116 | System MUST handle output buffer overflow |
| FR-018-117 | System MUST handle cancellation token cancellation |
| FR-018-118 | System MUST map all errors to structured `CommandError` records |
| FR-018-119 | CommandError MUST have `Code` property (error code string) |
| FR-018-120 | CommandError MUST have `Message` property (human-readable) |
| FR-018-121 | CommandError MUST have `Details` property (optional additional context) |
| FR-018-122 | System MUST log errors with full context (command, working dir, etc.) |
| FR-018-123 | System MUST NOT expose sensitive information in error messages |
| FR-018-124 | System MUST return CommandResult with error info (not throw) |
| FR-018-125 | System MUST increment failure metrics for observability |

### Input Validation (FR-018-126 to FR-018-140)

| ID | Requirement |
|----|-------------|
| FR-018-126 | System MUST validate command executable is not null |
| FR-018-127 | System MUST validate command executable is not empty |
| FR-018-128 | System MUST validate command executable is not whitespace only |
| FR-018-129 | System MUST validate working directory exists if specified |
| FR-018-130 | System MUST validate working directory is within repository root |
| FR-018-131 | System MUST validate timeout is positive if specified |
| FR-018-132 | System MUST validate arguments contain no null values |
| FR-018-133 | System MUST sanitize command strings for logging (redact patterns) |
| FR-018-134 | System MUST reject commands with shell metacharacters when UseShell=false |
| FR-018-135 | System MUST provide clear validation error messages |
| FR-018-136 | System MUST throw `CommandValidationException` for validation failures |
| FR-018-137 | CommandValidationException MUST include property name and message |
| FR-018-138 | CommandValidationException MUST be serializable |
| FR-018-139 | System MUST validate resource limits are within allowed bounds |
| FR-018-140 | System MUST validate environment variable names are valid |

---

## Non-Functional Requirements

### Performance (NFR-018-01 to NFR-018-10)

| ID | Requirement | Target | Maximum |
|----|-------------|--------|---------|
| NFR-018-01 | Process start overhead MUST be minimal | 30ms | 50ms |
| NFR-018-02 | Output capture overhead per MB MUST be efficient | 5ms | 10ms |
| NFR-018-03 | Audit write latency MUST be non-blocking | 2ms | 5ms |
| NFR-018-04 | Command validation overhead MUST be minimal | 1ms | 5ms |
| NFR-018-05 | Memory usage per command MUST be bounded | 10MB | 50MB |
| NFR-018-06 | Concurrent command limit MUST be configurable | 4 | 16 |
| NFR-018-07 | Process tree kill MUST complete quickly | 500ms | 2s |
| NFR-018-08 | Environment variable merge MUST be O(n) | 1ms | 10ms |
| NFR-018-09 | Command result serialization MUST be efficient | 5ms | 20ms |
| NFR-018-10 | Output truncation MUST be constant time | 1ms | 5ms |

### Reliability (NFR-018-11 to NFR-018-20)

| ID | Requirement |
|----|-------------|
| NFR-018-11 | System MUST handle process crashes without crashing the agent |
| NFR-018-12 | System MUST handle zombie processes (orphaned children) |
| NFR-018-13 | System MUST handle output larger than configured limit (truncate) |
| NFR-018-14 | System MUST maintain consistent state after timeout |
| NFR-018-15 | System MUST recover from process handle leaks |
| NFR-018-16 | System MUST handle concurrent execution failures independently |
| NFR-018-17 | System MUST survive malformed command input |
| NFR-018-18 | System MUST handle stdout/stderr interleaving correctly |
| NFR-018-19 | System MUST handle partial output on crash |
| NFR-018-20 | System MUST handle process exit before output complete |

### Security (NFR-018-21 to NFR-018-30)

| ID | Requirement |
|----|-------------|
| NFR-018-21 | System MUST NOT allow command injection via arguments |
| NFR-018-22 | System MUST redact secrets in logs (API keys, passwords) |
| NFR-018-23 | System MUST NOT execute commands through shell by default |
| NFR-018-24 | System MUST validate working directory is within repository |
| NFR-018-25 | System MUST sanitize environment variables in audit logs |
| NFR-018-26 | System MUST enforce timeout to prevent denial of service |
| NFR-018-27 | System MUST enforce output limits to prevent memory exhaustion |
| NFR-018-28 | System MUST run processes with minimum necessary privileges |
| NFR-018-29 | System MUST NOT expose host system paths in error messages |
| NFR-018-30 | System MUST support credential-free audit log queries |

### Maintainability (NFR-018-31 to NFR-018-40)

| ID | Requirement |
|----|-------------|
| NFR-018-31 | Code MUST follow SOLID principles |
| NFR-018-32 | ICommandExecutor MUST be mockable for testing |
| NFR-018-33 | ProcessRunner MUST be replaceable via DI |
| NFR-018-34 | All public APIs MUST have XML documentation |
| NFR-018-35 | All configuration MUST be externalizable |
| NFR-018-36 | Error codes MUST be documented |
| NFR-018-37 | Logging MUST use structured logging |
| NFR-018-38 | Code coverage MUST exceed 80% |
| NFR-018-39 | All async methods MUST use ConfigureAwait(false) |
| NFR-018-40 | Disposable resources MUST be properly disposed |

### Observability (NFR-018-41 to NFR-018-50)

| ID | Requirement |
|----|-------------|
| NFR-018-41 | All executions MUST be logged with correlation IDs |
| NFR-018-42 | Execution duration MUST be emitted as metric |
| NFR-018-43 | Execution success/failure MUST be emitted as metric |
| NFR-018-44 | Timeout events MUST be emitted as metric |
| NFR-018-45 | Output truncation events MUST be logged |
| NFR-018-46 | Concurrent execution count MUST be observable |
| NFR-018-47 | Audit queries MUST support time range filtering |
| NFR-018-48 | Audit queries MUST support correlation ID filtering |
| NFR-018-49 | Audit queries MUST support command pattern filtering |
| NFR-018-50 | Health check MUST report executor status |

---

## User Manual Documentation

### Overview

The Structured Command Runner is the execution engine that enables Agentic Coding Bot to run external commands (compilers, test frameworks, linters) and capture their results. Every command execution is structured, audited, and traceable.

This component is essential for the agent's autonomous verification loop. After making code changes, the agent MUST verify those changes work by running build and test commands. The command runner provides the reliable execution infrastructure for this verification.

### Key Concepts

**Command:** A structured representation of an external process to execute, including the executable, arguments, working directory, environment variables, and timeout.

**CommandResult:** The structured output from command execution, including stdout, stderr, exit code, duration, and correlation IDs.

**Audit Trail:** Every command execution is recorded to the workspace database with full correlation IDs, enabling traceability from agent decision through execution to result.

**Execution Modes:** Commands can execute locally (default) or in Docker containers (when operating in Docker mode per Task 001).

### Configuration

Configure command execution behavior in `.agent/config.yml`:

```yaml
# .agent/config.yml
execution:
  # Default command timeout in seconds
  # Commands exceeding this timeout are killed
  default_timeout_seconds: 300
  
  # Maximum output size in kilobytes
  # Output exceeding this limit is truncated
  max_output_kb: 1024
  
  # Whether to use shell execution
  # WARNING: Shell mode enables shell features but increases injection risk
  use_shell: false
  
  # Maximum concurrent command executions
  # Prevents resource exhaustion from parallel commands
  max_concurrent: 4
  
  # Environment variable handling
  environment:
    # inherit: Start with host environment (default)
    # replace: Use only specified variables
    # merge: Host environment plus specified overrides
    mode: inherit
    
    # Variables to exclude from inheritance
    # Use patterns like *_TOKEN, *_KEY, *_SECRET
    exclude_patterns:
      - "*_TOKEN"
      - "*_KEY"
      - "*_SECRET"
      - "*_PASSWORD"
    
    # Variables to always include (overrides exclude)
    include:
      - PATH
      - HOME
      - TEMP
      - TMP
  
  # Audit settings
  audit:
    # Whether to record executions to database
    enabled: true
    
    # Days to retain audit records
    retention_days: 30
    
    # Whether to include stdout/stderr in audit
    include_output: true
    
    # Maximum output size to store in audit (bytes)
    max_output_bytes: 10240
  
  # Secret redaction patterns
  redaction:
    enabled: true
    patterns:
      - "(?i)(api[_-]?key|apikey)[\\s:=]+['\"]?[a-zA-Z0-9_-]{20,}['\"]?"
      - "(?i)(secret|password|token)[\\s:=]+['\"]?[^\\s'\"]+['\"]?"
      - "sk-[a-zA-Z0-9]{20,}"
      - "ghp_[a-zA-Z0-9]{36}"
```

### CLI Commands

#### Execute a Command

Run a command and capture its output:

```bash
# Simple command execution
acode exec "dotnet build"

# Command with arguments (preferred - avoids shell parsing)
acode exec dotnet build --configuration Release

# Command with timeout override
acode exec "npm test" --timeout 60

# Command in specific directory
acode exec "make" --cwd /project/src

# Command with environment variables
acode exec "node app.js" --env "NODE_ENV=test" --env "DEBUG=true"

# Command in shell mode (use with caution)
acode exec "echo $PATH" --shell

# Command with verbose output
acode exec "dotnet test" --verbose
```

#### View Execution History

Query the audit trail of command executions:

```bash
# List recent executions
acode runs list

# List with count limit
acode runs list --limit 20

# List executions from specific run
acode runs list --run-id run-123

# List executions from specific session
acode runs list --session-id sess-456

# List executions for specific task
acode runs list --task-id task-789

# List failed executions only
acode runs list --failed

# List executions matching command pattern
acode runs list --command "dotnet*"

# List executions in time range
acode runs list --since "2024-01-15T00:00:00Z" --until "2024-01-16T00:00:00Z"
```

#### View Execution Details

Inspect a specific execution:

```bash
# Show execution details
acode runs show exec-001

# Show with full output
acode runs show exec-001 --full-output

# Show as JSON
acode runs show exec-001 --json

# Show with correlation context
acode runs show exec-001 --context
```

#### Cleanup Audit Records

Manage audit storage:

```bash
# Prune old audit records
acode runs prune --older-than 30d

# Prune specific run's records
acode runs prune --run-id run-123

# Show audit storage statistics
acode runs stats
```

### Execution Result Format

Every command execution produces a structured result:

```json
{
  "id": "exec-001",
  "command": {
    "executable": "dotnet",
    "arguments": ["build", "--configuration", "Release"],
    "workingDirectory": "/project",
    "timeout": "00:05:00"
  },
  "result": {
    "exitCode": 0,
    "success": true,
    "timedOut": false,
    "startTime": "2024-01-15T10:30:00.000Z",
    "endTime": "2024-01-15T10:30:05.123Z",
    "durationMs": 5123,
    "stdout": "MSBuild version 17.8.0+...\n  Determining projects to restore...\n  All projects are up-to-date for restore.\n  AgenticCoder -> /project/bin/Release/net8.0/AgenticCoder.dll\n\nBuild succeeded.\n    0 Warning(s)\n    0 Error(s)\n\nTime Elapsed 00:00:04.52",
    "stderr": "",
    "truncation": null
  },
  "correlationIds": {
    "runId": "run-123",
    "sessionId": "sess-456",
    "taskId": "task-789",
    "stepId": "step-001",
    "toolCallId": "tool-001",
    "worktreeId": "main",
    "repoSha": "abc123def"
  }
}
```

### Audit Record Format

Audit records stored in the workspace database:

```json
{
  "eventType": "command_execution_complete",
  "timestamp": "2024-01-15T10:30:05.123Z",
  "correlationIds": {
    "runId": "run-123",
    "sessionId": "sess-456",
    "taskId": "task-789",
    "stepId": "step-001",
    "toolCallId": "tool-001"
  },
  "context": {
    "worktreeId": "main",
    "repoSha": "abc123def456"
  },
  "command": {
    "executable": "dotnet",
    "arguments": ["build"],
    "workingDirectory": "/project"
  },
  "result": {
    "exitCode": 0,
    "durationMs": 5123,
    "timedOut": false,
    "outputTruncated": false
  }
}
```

### Troubleshooting

#### Command Not Found

**Symptoms:**
- Error message: "Command not found" or "The system cannot find the file specified"
- Exit code: 127 (Unix) or 9009 (Windows)

**Causes:**
1. Executable not in PATH
2. Executable name misspelled
3. Executable requires shell expansion

**Solutions:**
1. Verify the command exists: `which dotnet` (Unix) or `where dotnet` (Windows)
2. Use absolute path: `/usr/bin/dotnet` or `C:\Program Files\dotnet\dotnet.exe`
3. Check shell mode setting if using aliases
4. Verify working directory contains expected tools

**Example Fix:**
```bash
# Instead of
acode exec "dotnet build"

# Try absolute path
acode exec "/usr/share/dotnet/dotnet" build
```

#### Command Timeout

**Symptoms:**
- Result shows `timedOut: true`
- Partial output captured
- Process killed

**Causes:**
1. Command takes longer than timeout
2. Command is waiting for input
3. Command is in infinite loop
4. Command is blocked on I/O

**Solutions:**
1. Increase timeout: `--timeout 600`
2. Check if command requires input (add --no-input flags)
3. Review command for infinite loops
4. Check for disk or network bottlenecks

**Example Fix:**
```bash
# Increase timeout for long builds
acode exec "npm run build" --timeout 600

# Add non-interactive flags
acode exec "apt-get install -y nodejs" --timeout 300
```

#### Output Truncated

**Symptoms:**
- Result shows `truncation` object with original and truncated sizes
- Output ends with `[OUTPUT TRUNCATED]`

**Causes:**
1. Command produces output larger than `max_output_kb`
2. Verbose logging enabled in build

**Solutions:**
1. Increase `max_output_kb` in configuration
2. Reduce verbosity: `dotnet build --verbosity quiet`
3. Redirect output to file if full log needed

**Example Fix:**
```yaml
# Increase limit in config
execution:
  max_output_kb: 4096
```

#### Permission Denied

**Symptoms:**
- Error message: "Access denied" or "Permission denied"
- Exit code: 126 (Unix)

**Causes:**
1. Executable lacks execute permission
2. Working directory not accessible
3. Output file location not writable

**Solutions:**
1. Check executable permissions: `ls -la /path/to/executable`
2. Check directory permissions: `ls -la /path/to/workdir`
3. Run with appropriate user context

#### Shell Features Not Working

**Symptoms:**
- Environment variables not expanded ($PATH shows literal)
- Pipes and redirects not working
- Glob patterns not expanded

**Causes:**
1. Shell mode disabled (default)

**Solutions:**
1. Enable shell mode: `--shell`
2. Use explicit command array form instead of shell features
3. Pass pre-expanded values

**Example Fix:**
```bash
# Enable shell mode for shell features
acode exec "echo $PATH | head -1" --shell

# Or avoid shell features
acode exec printenv PATH
```

### Best Practices

1. **Prefer Argument Arrays:** Pass arguments separately rather than as a single string to avoid shell parsing issues.

2. **Set Appropriate Timeouts:** Set timeouts based on expected command duration. Test builds might need 10+ minutes.

3. **Use Non-Interactive Flags:** Add flags like `--yes`, `--non-interactive`, `--no-input` to prevent commands waiting for input.

4. **Check Exit Codes:** Always check `success` or `exitCode` in results. Non-zero doesn't always mean failure for some commands.

5. **Limit Output:** For verbose commands, use quiet modes or redirect to file to stay within output limits.

6. **Use Correlation IDs:** When debugging, filter audit records by run_id or session_id to see related executions.

7. **Test Commands Manually First:** Before using commands in automation, verify they work manually in the target environment.
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

### Command Model (AC-018-01 to AC-018-12)

- [ ] AC-018-01: Command record MUST be defined with all required properties
- [ ] AC-018-02: Command.Executable MUST be required and validated non-empty
- [ ] AC-018-03: Command.Arguments MUST default to empty list
- [ ] AC-018-04: Command.WorkingDirectory MUST be optional with repo root default
- [ ] AC-018-05: Command.Environment MUST be optional dictionary
- [ ] AC-018-06: Command.Timeout MUST be optional with config default
- [ ] AC-018-07: Command MUST be immutable (record type)
- [ ] AC-018-08: Command MUST serialize to JSON correctly
- [ ] AC-018-09: Command builder MUST create valid commands fluently
- [ ] AC-018-10: Command validation MUST reject null executable
- [ ] AC-018-11: Command validation MUST reject empty executable
- [ ] AC-018-12: Command validation MUST reject missing working directory

### Executor Interface (AC-018-13 to AC-018-22)

- [ ] AC-018-13: ICommandExecutor interface MUST be defined
- [ ] AC-018-14: ExecuteAsync MUST accept Command parameter
- [ ] AC-018-15: ExecuteAsync MUST accept optional ExecutionOptions
- [ ] AC-018-16: ExecuteAsync MUST accept CancellationToken
- [ ] AC-018-17: ExecuteAsync MUST return Task<CommandResult>
- [ ] AC-018-18: Executor MUST be registered in DI container
- [ ] AC-018-19: Executor MUST be thread-safe for concurrent use
- [ ] AC-018-20: Executor MUST select local vs Docker mode correctly
- [ ] AC-018-21: Executor MUST enforce concurrency limits
- [ ] AC-018-22: Executor MUST integrate with audit system

### Command Result (AC-018-23 to AC-018-35)

- [ ] AC-018-23: CommandResult record MUST be defined
- [ ] AC-018-24: CommandResult.Stdout MUST contain captured stdout
- [ ] AC-018-25: CommandResult.Stderr MUST contain captured stderr
- [ ] AC-018-26: CommandResult.ExitCode MUST match process exit code
- [ ] AC-018-27: CommandResult.StartTime MUST be accurate
- [ ] AC-018-28: CommandResult.EndTime MUST be accurate
- [ ] AC-018-29: CommandResult.Duration MUST be calculated correctly
- [ ] AC-018-30: CommandResult.Success MUST be true when ExitCode=0
- [ ] AC-018-31: CommandResult.TimedOut MUST be true when timeout occurred
- [ ] AC-018-32: CommandResult.Error MUST contain error message on failure
- [ ] AC-018-33: CommandResult.CorrelationIds MUST be populated
- [ ] AC-018-34: CommandResult.TruncationInfo MUST be set when output truncated
- [ ] AC-018-35: CommandResult MUST serialize to JSON correctly

### Process Execution (AC-018-36 to AC-018-50)

- [ ] AC-018-36: ProcessRunner MUST start processes correctly
- [ ] AC-018-37: ProcessRunner MUST capture stdout asynchronously
- [ ] AC-018-38: ProcessRunner MUST capture stderr asynchronously
- [ ] AC-018-39: ProcessRunner MUST wait for process completion
- [ ] AC-018-40: ProcessRunner MUST handle process timeout
- [ ] AC-018-41: ProcessRunner MUST kill process tree on timeout
- [ ] AC-018-42: ProcessRunner MUST dispose process resources
- [ ] AC-018-43: ProcessRunner MUST handle process start failure
- [ ] AC-018-44: ProcessRunner MUST handle process crash
- [ ] AC-018-45: ProcessRunner MUST set working directory correctly
- [ ] AC-018-46: ProcessRunner MUST merge environment variables
- [ ] AC-018-47: ProcessRunner MUST respect shell mode setting
- [ ] AC-018-48: ProcessRunner MUST handle large output (truncation)
- [ ] AC-018-49: ProcessRunner MUST handle interleaved stdout/stderr
- [ ] AC-018-50: ProcessRunner MUST handle cancellation token

### Execution Options (AC-018-51 to AC-018-58)

- [ ] AC-018-51: ExecutionOptions MUST support timeout override
- [ ] AC-018-52: ExecutionOptions MUST support environment merge modes
- [ ] AC-018-53: ExecutionOptions MUST support shell mode toggle
- [ ] AC-018-54: ExecutionOptions MUST support capture mode selection
- [ ] AC-018-55: ExecutionOptions MUST support max output size override
- [ ] AC-018-56: ExecutionOptions MUST support secret redaction toggle
- [ ] AC-018-57: Default options MUST use config values
- [ ] AC-018-58: Option overrides MUST take precedence

### Audit Recording (AC-018-59 to AC-018-70)

- [ ] AC-018-59: Execution start MUST be recorded to audit
- [ ] AC-018-60: Execution complete MUST be recorded to audit
- [ ] AC-018-61: Audit events MUST include all correlation IDs
- [ ] AC-018-62: Audit events MUST include command details
- [ ] AC-018-63: Audit events MUST include result details
- [ ] AC-018-64: Audit events MUST be queryable by run_id
- [ ] AC-018-65: Audit events MUST be queryable by session_id
- [ ] AC-018-66: Audit events MUST be queryable by task_id
- [ ] AC-018-67: Audit events MUST be queryable by time range
- [ ] AC-018-68: Audit events MUST be queryable by command pattern
- [ ] AC-018-69: Audit retention MUST honor configured days
- [ ] AC-018-70: Audit MUST redact sensitive environment variables

### Error Handling (AC-018-71 to AC-018-80)

- [ ] AC-018-71: Command not found MUST return structured error
- [ ] AC-018-72: Permission denied MUST return structured error
- [ ] AC-018-73: Timeout MUST return result with TimedOut=true
- [ ] AC-018-74: Process crash MUST return partial output captured
- [ ] AC-018-75: Output overflow MUST truncate with info
- [ ] AC-018-76: Cancellation MUST abort cleanly
- [ ] AC-018-77: All errors MUST have error codes
- [ ] AC-018-78: All errors MUST be logged
- [ ] AC-018-79: Errors MUST NOT expose sensitive paths
- [ ] AC-018-80: Failure metrics MUST be emitted

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