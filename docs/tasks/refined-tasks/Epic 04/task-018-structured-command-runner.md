# Task 018: Structured Command Runner

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 050 (Workspace Database), Task 002 (Config Contract)  

---

## Description

### Business Value and ROI Analysis

The Structured Command Runner is the execution foundation that enables Agentic Coding Bot to verify, test, and build code changes. This abstraction is critically important because:

1. **Autonomous Verification:** The agent MUST verify its own code changes through compilation, testing, and linting. Without a reliable command execution layer, the agent cannot validate that its modifications work correctly. This is the difference between an agent that "hopes" its code works and one that "knows" it works.

2. **Reproducibility:** Every command execution MUST be reproducible. When debugging agent behavior, developers need to understand exactly what commands were run, with what parameters, in what environment, and what the results were. The structured approach ensures complete reproducibility.

3. **Auditability:** In enterprise environments, all agent actions MUST be auditable. The command runner creates a complete audit trail of every external process invoked—enabling compliance, debugging, and cost tracking.

4. **Security Boundary:** Commands execute external processes that can have side effects. The command runner provides the control point for enforcing timeouts, resource limits, and execution policies that protect the host system.

5. **Error Recovery:** When commands fail, the agent needs actionable feedback. Raw stderr isn't enough—the agent needs structured error information that enables intelligent retry or fallback behavior.

6. **Performance Optimization:** By capturing execution metrics (duration, memory, CPU), the command runner enables performance analysis and optimization of the agent's build/test cycle.

#### ROI Calculation

| Metric | Before (Manual/Ad-hoc) | After (Structured Runner) | Annual Savings |
|--------|------------------------|---------------------------|----------------|
| Debug Time per Failed Build | 45 minutes | 8 minutes | 37 min/failure |
| Build Failures per Developer/Week | 8 | 8 | N/A |
| Time Saved per Developer/Week | N/A | 4.9 hours | $367.50/week |
| Security Incidents from Unvalidated Commands | 2/year | 0/year | $150,000/year |
| Compliance Audit Preparation | 40 hours/quarter | 4 hours/quarter | $14,400/year |
| Root Cause Analysis Time | 4 hours/incident | 30 minutes/incident | $3,500/year |
| Process Leak/Zombie Cleanup | 2 hours/week | 0 | $10,400/year |

**Assumptions:**
- 10-person development team
- $75/hour fully loaded developer cost
- 50 work weeks/year
- Security incident remediation cost: $75,000 per incident

**Total Annual ROI:**
- Developer Time Savings: 10 developers × 4.9 hours × $75 × 50 weeks = **$183,750**
- Security Incident Prevention: 2 incidents × $75,000 = **$150,000**
- Compliance Savings: $14,400
- RCA Time Savings: $3,500
- Process Management: $10,400

**Total Annual Savings: $362,050**

**Implementation Cost:** 40 hours × $100/hour = $4,000
**ROI: 9,051%** | **Payback Period: 4 days**

#### Before/After Comparison

| Aspect | Before (No Structured Runner) | After (With Structured Runner) |
|--------|-------------------------------|--------------------------------|
| Command Logging | Scattered console output | Centralized, queryable audit trail |
| Timeout Handling | Processes hang indefinitely | Automatic timeout with process tree kill |
| Error Diagnosis | Parse raw stderr manually | Structured CommandResult with error codes |
| Reproducibility | "Works on my machine" | Complete command serialization for replay |
| Security | Shell injection vulnerabilities | Argument array prevents injection |
| Resource Management | Zombie processes accumulate | Automatic cleanup in finally blocks |
| Correlation | Guess which command failed | Full tracing with RunId/SessionId/TaskId/StepId |
| Performance Metrics | Unknown execution times | Duration, memory, CPU captured per command |

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                           STRUCTURED COMMAND RUNNER                                  │
│                              Task 018 Architecture                                   │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐                │
│  │   CLI Command   │     │  Agent Actions  │     │ Language Runner │                │
│  │   (acode exec)  │     │  (Task 019+)    │     │   (Task 019)    │                │
│  └────────┬────────┘     └────────┬────────┘     └────────┬────────┘                │
│           │                       │                       │                          │
│           ▼                       ▼                       ▼                          │
│  ┌────────────────────────────────────────────────────────────────────┐             │
│  │                         ICommandExecutor                           │             │
│  │  ┌──────────────────────────────────────────────────────────────┐  │             │
│  │  │  ExecuteAsync(Command, ExecutionOptions?, CancellationToken) │  │             │
│  │  │  → Task<CommandResult>                                        │  │             │
│  │  └──────────────────────────────────────────────────────────────┘  │             │
│  └─────────────────────────────┬──────────────────────────────────────┘             │
│                                │                                                     │
│           ┌────────────────────┼────────────────────┐                               │
│           │                    │                    │                               │
│           ▼                    ▼                    ▼                               │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐                      │
│  │  Mode Selector  │  │  Audit Service  │  │ Concurrency Ctrl│                      │
│  │   (Task 001)    │  │   (Task 003.c)  │  │  (Semaphore)    │                      │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘                      │
│           │                    │                    │                               │
│           │                    ▼                    │                               │
│           │           ┌─────────────────┐           │                               │
│           │           │  Workspace DB   │           │                               │
│           │           │   (Task 050)    │           │                               │
│           │           └─────────────────┘           │                               │
│           │                                         │                               │
│           ▼                                         ▼                               │
│  ┌────────────────────────────────────────────────────────────────────┐             │
│  │                        EXECUTION LAYER                             │             │
│  │  ┌─────────────────────────┐  ┌─────────────────────────────────┐  │             │
│  │  │     ProcessRunner       │  │      DockerExecutor             │  │             │
│  │  │     (Local Mode)        │  │      (Docker Mode - Task 020)   │  │             │
│  │  │  ┌───────────────────┐  │  │  ┌───────────────────────────┐  │  │             │
│  │  │  │ System.Diagnostics│  │  │  │   Container Sandbox       │  │  │             │
│  │  │  │     .Process      │  │  │  │   with Volume Mounts      │  │  │             │
│  │  │  └───────────────────┘  │  │  └───────────────────────────┘  │  │             │
│  │  └─────────────────────────┘  └─────────────────────────────────┘  │             │
│  └────────────────────────────────────────────────────────────────────┘             │
│                                │                                                     │
│                                ▼                                                     │
│  ┌────────────────────────────────────────────────────────────────────┐             │
│  │                         CommandResult                              │             │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐  │             │
│  │  │ Stdout/Stderr│  │  Exit Code   │  │   CorrelationIds         │  │             │
│  │  │  (captured)  │  │  + Duration  │  │ RunId/SessionId/TaskId   │  │             │
│  │  └──────────────┘  └──────────────┘  └──────────────────────────┘  │             │
│  └────────────────────────────────────────────────────────────────────┘             │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘

                           DATA FLOW SEQUENCE

    ┌─────────┐     ┌──────────────────┐     ┌─────────────────┐     ┌─────────────┐
    │ Caller  │     │ CommandExecutor  │     │  ProcessRunner  │     │   Process   │
    └────┬────┘     └────────┬─────────┘     └────────┬────────┘     └──────┬──────┘
         │                   │                        │                     │
         │  ExecuteAsync()   │                        │                     │
         │──────────────────>│                        │                     │
         │                   │                        │                     │
         │                   │  Validate Command      │                     │
         │                   │─────────────┐          │                     │
         │                   │             │          │                     │
         │                   │<────────────┘          │                     │
         │                   │                        │                     │
         │                   │  Acquire Semaphore     │                     │
         │                   │─────────────┐          │                     │
         │                   │             │          │                     │
         │                   │<────────────┘          │                     │
         │                   │                        │                     │
         │                   │  Record Start Audit    │                     │
         │                   │─────────────────────>  │                     │
         │                   │                        │                     │
         │                   │  RunAsync(Command)     │                     │
         │                   │───────────────────────>│                     │
         │                   │                        │                     │
         │                   │                        │  Start Process      │
         │                   │                        │────────────────────>│
         │                   │                        │                     │
         │                   │                        │  Capture Stdout     │
         │                   │                        │<────────────────────│
         │                   │                        │                     │
         │                   │                        │  Capture Stderr     │
         │                   │                        │<────────────────────│
         │                   │                        │                     │
         │                   │                        │  Wait for Exit      │
         │                   │                        │<────────────────────│
         │                   │                        │                     │
         │                   │  CommandResult         │                     │
         │                   │<───────────────────────│                     │
         │                   │                        │                     │
         │                   │  Record End Audit      │                     │
         │                   │─────────────────────>  │                     │
         │                   │                        │                     │
         │                   │  Release Semaphore     │                     │
         │                   │─────────────┐          │                     │
         │                   │             │          │                     │
         │                   │<────────────┘          │                     │
         │                   │                        │                     │
         │  CommandResult    │                        │                     │
         │<──────────────────│                        │                     │
         │                   │                        │                     │
    ┌────┴────┐     ┌────────┴─────────┐     ┌────────┴────────┐     ┌──────┴──────┐
    │ Caller  │     │ CommandExecutor  │     │  ProcessRunner  │     │   Process   │
    └─────────┘     └──────────────────┘     └─────────────────┘     └─────────────┘
```

### Technical Scope

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

### Architectural Trade-offs

#### Trade-off 1: Single Interface vs Specialized Executors

| Approach | Pros | Cons |
|----------|------|------|
| **Single ICommandExecutor (Chosen)** | Uniform API, mode switching transparent to callers, simpler DI | Internal complexity in mode selection |
| Separate ILocalExecutor/IDockerExecutor | Clear separation, easier testing | Callers must choose executor, leaky abstraction |
| Strategy Pattern with IExecutionStrategy | Maximum flexibility | Over-engineering for current needs |

**Decision:** Single interface with internal mode selection. Callers should not care about execution mode.

#### Trade-off 2: Synchronous vs Asynchronous Output Capture

| Approach | Pros | Cons |
|----------|------|------|
| **Async with Events (Chosen)** | Non-blocking, handles large output, real-time streaming possible | More complex implementation |
| Synchronous ReadToEnd() | Simple implementation | Deadlock risk if output exceeds buffer |
| Memory-mapped files | No buffer limits | Complex, platform-specific |

**Decision:** Asynchronous capture using `BeginOutputReadLine()` and events. Prevents deadlocks.

#### Trade-off 3: Process Tree Kill vs Single Process Kill

| Approach | Pros | Cons |
|----------|------|------|
| **Process Tree Kill (Chosen)** | Cleans up child processes (npm, dotnet), no zombies | Platform-specific implementation |
| Single Process Kill | Simple, portable | Orphaned child processes |
| Job Objects (Windows) | OS-level isolation | Windows-only |

**Decision:** Process tree kill with platform-specific implementations. Critical for npm, dotnet.

#### Trade-off 4: Correlation ID Generation

| Approach | Pros | Cons |
|----------|------|------|
| **Ambient Context (Chosen)** | No parameter threading, works with existing code | Relies on AsyncLocal<T>, testing requires setup |
| Explicit Parameter Passing | Clear dependencies, easy to test | Viral parameter changes across codebase |
| Message-based (Correlation Header) | Standard pattern for distributed systems | Over-engineering for single-process |

**Decision:** Ambient context via `ICorrelationContext` with AsyncLocal<T> storage.

#### Trade-off 5: Output Size Limits

| Approach | Pros | Cons |
|----------|------|------|
| **Truncation with Indicator (Chosen)** | Bounded memory, preserves head/tail | May lose middle content |
| No Limits | Complete output | Memory exhaustion risk |
| Streaming to File | Unlimited size, bounded memory | Slower, file cleanup needed |
| Ring Buffer | Bounded, keeps recent | Loses beginning of output |

**Decision:** Configurable limit (default 1MB) with head/tail truncation strategy.

### Failure Modes and Mitigations

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

---

## Use Cases

### Scenario 1: DevBot Verifies Code Changes

**Persona:** DevBot, an AI developer assistant working on a .NET web API project

**Before (No Structured Command Runner):**
1. DevBot modifies `CustomerController.cs` to add a new endpoint
2. DevBot calls `Process.Start("dotnet", "build")` directly
3. Build fails due to a typo in the code
4. DevBot receives raw stderr: `error CS1002: ; expected`
5. DevBot doesn't know which file failed (no correlation)
6. DevBot cannot retry because no command history exists
7. DevBot's next attempt leaves orphaned dotnet processes
8. System memory slowly leaks with each failed attempt
9. After 10 attempts, system becomes unresponsive
10. User must manually kill processes and restart

**After (With Structured Command Runner):**
1. DevBot modifies `CustomerController.cs` to add a new endpoint
2. DevBot calls `await executor.ExecuteAsync(new Command("dotnet", "build"))`
3. Build fails due to a typo in the code
4. DevBot receives structured `CommandResult`:
   - `ExitCode: 1`
   - `Stderr: "CustomerController.cs(45,12): error CS1002: ; expected"`
   - `Duration: 2.3s`
   - `CorrelationIds: { RunId: "r-123", TaskId: "t-456", StepId: "s-789" }`
5. DevBot queries audit: "Show me all commands for step s-789"
6. DevBot sees full command context and can retry with same parameters
7. On timeout, process tree is killed automatically
8. System remains stable after many build attempts
9. DevBot fixes the typo and verifies fix with same command
10. All executions are auditable for debugging

**Metrics:**
- Debug time reduced from 45 minutes to 5 minutes per failure
- Zero orphaned processes vs 3-5 per failed session
- Complete audit trail vs no visibility
- Improvement: **9x faster diagnosis**

---

### Scenario 2: Sarah Debugs a Failing Test

**Persona:** Sarah, a senior developer debugging why tests pass locally but fail in the agent

**Before (No Structured Command Runner):**
1. Agent reports "tests failed" with no details
2. Sarah asks "what command did you run?"
3. Agent cannot answer—no execution history
4. Sarah guesses environment differences
5. Sarah manually runs tests to reproduce
6. 4 hours later, Sarah discovers agent used different working directory
7. Sarah has no way to know if this was the actual issue
8. Sarah patches randomly, hoping it works
9. Issue recurs next week with different environment variable

**After (With Structured Command Runner):**
1. Agent reports "tests failed" with CommandResult details
2. Sarah queries: `acode runs show r-123`
3. Sarah sees exact command:
   ```
   Command: dotnet test
   WorkingDirectory: /repo/tests
   Environment: { "ASPNETCORE_ENVIRONMENT": "Development" }
   Timeout: 300s
   ExitCode: 1
   Duration: 45.2s
   Stderr: [truncated, 1.2MB]
   ```
4. Sarah immediately sees working directory is wrong
5. Sarah fixes agent configuration in 10 minutes
6. Sarah verifies fix by comparing audit trails
7. Issue never recurs because root cause is known
8. Sarah documents pattern for team

**Metrics:**
- Root cause analysis: 4 hours → 10 minutes
- Recurrence rate: 100% → 0%
- Documentation quality: None → Complete
- Improvement: **24x faster RCA**

---

### Scenario 3: Compliance Team Audits Agent Activity

**Persona:** Marcus, a compliance officer reviewing agent actions for SOC2 audit

**Before (No Structured Command Runner):**
1. Auditor requests: "Show all commands run by agents in Q3"
2. DevOps team scrambles to collect scattered logs
3. Logs are incomplete—some commands not logged
4. Logs lack correlation—cannot trace command to task
5. Audit preparation takes 40 hours
6. Audit findings: "Insufficient command logging"
7. Remediation required before certification
8. Certification delayed 3 months
9. Business impact: Cannot close enterprise deals

**After (With Structured Command Runner):**
1. Auditor requests: "Show all commands run by agents in Q3"
2. Marcus runs: `acode runs list --from 2024-07-01 --to 2024-09-30 --format json`
3. Complete command history with correlation:
   ```json
   {
     "commands": [
       {
         "timestamp": "2024-07-15T10:23:45Z",
         "command": "dotnet build",
         "exitCode": 0,
         "duration": "12.3s",
         "runId": "r-456",
         "sessionId": "s-789",
         "taskId": "t-012",
         "userId": "agent@company.com"
       }
     ],
     "total": 45892
   }
   ```
4. Complete chain of custody for every command
5. Audit preparation takes 4 hours
6. Audit findings: "Complete command audit trail"
7. Certification approved
8. Enterprise deals proceed

**Metrics:**
- Audit preparation: 40 hours → 4 hours
- Audit findings: Non-compliant → Compliant
- Certification timeline: +3 months → On schedule
- Improvement: **10x faster compliance, certification achieved**

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
- **Shell scripting support** - No .sh/.ps1 file execution, only direct commands
- **Interactive commands** - No stdin support, batch mode only

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

---

## Assumptions

### Technical Assumptions

1. **Process Subsystem Available:** The host system has a working process subsystem (Windows Process API, Linux fork/exec, macOS posix_spawn). Process creation, execution, and termination work reliably.

2. **Executable Resolution:** Executables are available in the system PATH or specified with absolute paths. The command executor does not perform PATH searching beyond what the OS provides.

3. **Process Permissions:** The agent process has permission to start child processes. Security policies (AppArmor, SELinux, Windows UAC) do not block process creation.

4. **File System Access:** The file system is accessible for working directory resolution and validation. Network file systems may have latency but are supported.

5. **UTF-8 Encoding:** Process output is UTF-8 encoded or uses the system default encoding. The executor does not perform charset detection or conversion.

6. **Process Tree Kill:** Process trees can be killed on timeout. This is critical for build tools like npm/yarn that spawn child processes. Platform-specific APIs (Windows Job Objects, Linux cgroups, macOS process groups) are available.

7. **Environment Variable Size:** Environment variable blocks are within OS limits (typically 32KB on Windows, 128KB on Linux). Large environments are not supported.

8. **Memory Availability:** Sufficient memory exists to buffer command output up to configured limits (default 1MB per stream).

### Operational Assumptions

9. **Timeout Configuration:** Users configure appropriate timeouts for their workloads. The default 5-minute timeout is suitable for most build/test operations.

10. **Concurrency Limits:** The configured concurrency limit (default 4) is appropriate for the host system. Users adjust based on CPU cores and memory.

11. **Audit Retention:** Audit events are retained for the configured period (default 30 days). Users manage storage capacity for long-running systems.

12. **Log Aggregation:** Structured logs are collected and aggregated by external systems (ELK, Datadog, etc.) for analysis.

### Integration Assumptions

13. **Database Available:** The workspace database (Task 050) is available and writable for audit persistence. Audit logging degrades gracefully if database is unavailable.

14. **Correlation Context:** The correlation context (ICorrelationContext) is properly initialized before command execution. Missing context results in null correlation IDs.

15. **DI Container:** The dependency injection container (Task 003) is configured before command executor use. The singleton lifecycle is respected.

16. **Config Loaded:** Configuration (Task 002) is loaded before command executor initialization. Configuration changes require service restart.

17. **Docker Available:** When in Docker mode (Task 020), Docker daemon is running and accessible. Container images are pre-pulled or pullable.

18. **Session Context:** The session context (Task 011) provides run_id and session_id for correlation. Missing session context results in null correlation IDs.

---

## Security Threats and Mitigations

### Threat 1: Command Injection via Shell Execution

**Risk:** HIGH - Attackers could inject malicious commands through user-controlled input when shell mode is enabled.

**Attack Scenario:**
```
User provides filename: "file.txt; rm -rf /"
Agent executes: sh -c "process file.txt; rm -rf /"
Result: System destruction
```

**Complete Mitigation Code:**

```csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Acode.Infrastructure.CommandExecution;

/// <summary>
/// Validates and sanitizes command arguments to prevent injection attacks.
/// </summary>
public sealed class CommandArgumentValidator
{
    private static readonly Regex DangerousShellChars = new(
        @"[;&|`$(){}[\]<>!#*?\\'\""~]",
        RegexOptions.Compiled);
    
    private static readonly Regex PathTraversalPattern = new(
        @"\.\.[/\\]",
        RegexOptions.Compiled);
    
    private static readonly HashSet<string> DangerousCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "rm", "del", "rmdir", "rd", "format", "fdisk",
        "dd", "mkfs", "shutdown", "reboot", "halt",
        "chmod", "chown", "sudo", "su", "passwd",
        "curl", "wget", "nc", "netcat", "ncat"
    };

    /// <summary>
    /// Validates command for safety before execution.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <param name="options">Execution options including shell mode.</param>
    /// <returns>Validation result with any errors.</returns>
    public ValidationResult Validate(Command command, ExecutionOptions? options = null)
    {
        var errors = new List<string>();
        
        // Block dangerous executables
        var executableName = Path.GetFileNameWithoutExtension(command.Executable);
        if (DangerousCommands.Contains(executableName))
        {
            errors.Add($"Dangerous command blocked: {executableName}");
        }
        
        // When shell mode is enabled, validate arguments strictly
        if (options?.UseShell == true)
        {
            foreach (var arg in command.Arguments)
            {
                if (DangerousShellChars.IsMatch(arg))
                {
                    errors.Add($"Shell-unsafe character in argument: {arg}");
                }
                
                if (PathTraversalPattern.IsMatch(arg))
                {
                    errors.Add($"Path traversal attempt in argument: {arg}");
                }
            }
        }
        
        // Validate working directory is within repository
        if (!string.IsNullOrEmpty(command.WorkingDirectory))
        {
            var normalizedPath = Path.GetFullPath(command.WorkingDirectory);
            var repoRoot = GetRepositoryRoot();
            
            if (!normalizedPath.StartsWith(repoRoot, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Working directory outside repository: {command.WorkingDirectory}");
            }
        }
        
        return new ValidationResult(errors.Count == 0, errors);
    }
    
    private string GetRepositoryRoot()
    {
        // Implementation retrieves repository root from session context
        return Environment.GetEnvironmentVariable("ACODE_REPO_ROOT") 
            ?? Environment.CurrentDirectory;
    }
}

public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);
```

---

### Threat 2: Environment Variable Credential Leakage

**Risk:** HIGH - Sensitive credentials in environment variables could be logged or exposed in audit trails.

**Attack Scenario:**
```
Environment contains: DATABASE_PASSWORD=secret123
Agent logs full environment to audit trail
Attacker gains access to audit database
Result: Credential exposure
```

**Complete Mitigation Code:**

```csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Acode.Infrastructure.CommandExecution;

/// <summary>
/// Redacts sensitive values from environment variables and command output.
/// </summary>
public sealed class SecretRedactor
{
    private static readonly HashSet<string> SensitiveKeyPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "PASSWORD", "SECRET", "TOKEN", "KEY", "CREDENTIAL",
        "API_KEY", "APIKEY", "AUTH", "BEARER", "JWT",
        "PRIVATE", "CERT", "PFX", "PEM"
    };
    
    private static readonly Regex SecretValuePatterns = new(
        @"(password|secret|token|key|auth|bearer)[:=]\s*['""]?([^'""&\s]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    private static readonly Regex JwtPattern = new(
        @"eyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+",
        RegexOptions.Compiled);
    
    private static readonly Regex ApiKeyPattern = new(
        @"[a-zA-Z0-9]{32,}",
        RegexOptions.Compiled);

    private const string RedactedPlaceholder = "[REDACTED]";

    /// <summary>
    /// Redacts sensitive values from environment dictionary for logging.
    /// </summary>
    public IDictionary<string, string> RedactEnvironment(
        IDictionary<string, string>? environment)
    {
        if (environment == null)
            return new Dictionary<string, string>();
        
        var redacted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var kvp in environment)
        {
            var isSensitive = IsSensitiveKey(kvp.Key);
            redacted[kvp.Key] = isSensitive ? RedactedPlaceholder : kvp.Value;
        }
        
        return redacted;
    }
    
    /// <summary>
    /// Redacts sensitive values from command output for logging.
    /// </summary>
    public string RedactOutput(string output)
    {
        if (string.IsNullOrEmpty(output))
            return output;
        
        var result = output;
        
        // Redact password=value patterns
        result = SecretValuePatterns.Replace(result, "$1=[REDACTED]");
        
        // Redact JWT tokens
        result = JwtPattern.Replace(result, RedactedPlaceholder);
        
        // Redact long alphanumeric strings (potential API keys)
        // Only if they look like keys (not file paths, GUIDs)
        result = ApiKeyPattern.Replace(result, match =>
        {
            var value = match.Value;
            // Skip GUIDs and hex strings that might be commit SHAs
            if (value.Length == 32 || value.Length == 40 || value.Length == 64)
            {
                if (Regex.IsMatch(value, @"^[0-9a-f]+$", RegexOptions.IgnoreCase))
                    return value; // Likely a hash, not a secret
            }
            return RedactedPlaceholder;
        });
        
        return result;
    }
    
    private bool IsSensitiveKey(string key)
    {
        foreach (var pattern in SensitiveKeyPatterns)
        {
            if (key.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
```

---

### Threat 3: Resource Exhaustion via Runaway Processes

**Risk:** MEDIUM - Long-running or resource-intensive commands could exhaust system resources.

**Attack Scenario:**
```
Agent executes: while(true) { allocate_memory(); }
Process consumes all available RAM
System becomes unresponsive
Result: Denial of service
```

**Complete Mitigation Code:**

```csharp
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Acode.Infrastructure.CommandExecution;

/// <summary>
/// Enforces resource limits on command execution to prevent resource exhaustion.
/// </summary>
public sealed class ResourceLimitEnforcer : IDisposable
{
    private readonly TimeSpan _timeout;
    private readonly long _maxMemoryBytes;
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly Timer? _resourceMonitor;
    private Process? _monitoredProcess;
    
    public ResourceLimitEnforcer(
        TimeSpan timeout,
        long maxMemoryBytes = 512 * 1024 * 1024, // 512 MB default
        int maxConcurrency = 4)
    {
        _timeout = timeout;
        _maxMemoryBytes = maxMemoryBytes;
        _concurrencySemaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        
        // Monitor resource usage every 5 seconds
        _resourceMonitor = new Timer(CheckResourceUsage, null, 
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }
    
    /// <summary>
    /// Acquires execution slot and sets up resource monitoring.
    /// </summary>
    public async Task<ExecutionGuard> AcquireAsync(
        CancellationToken cancellationToken = default)
    {
        // Wait for available execution slot
        if (!await _concurrencySemaphore.WaitAsync(_timeout, cancellationToken))
        {
            throw new TimeoutException(
                $"Timed out waiting for execution slot after {_timeout}");
        }
        
        return new ExecutionGuard(this);
    }
    
    /// <summary>
    /// Begins monitoring a process for resource limits.
    /// </summary>
    public void MonitorProcess(Process process)
    {
        _monitoredProcess = process;
    }
    
    private void CheckResourceUsage(object? state)
    {
        if (_monitoredProcess == null || _monitoredProcess.HasExited)
            return;
        
        try
        {
            _monitoredProcess.Refresh();
            var memoryUsage = _monitoredProcess.WorkingSet64;
            
            if (memoryUsage > _maxMemoryBytes)
            {
                KillProcessTree(_monitoredProcess);
                throw new ResourceLimitExceededException(
                    $"Process exceeded memory limit: {memoryUsage / (1024 * 1024)}MB > {_maxMemoryBytes / (1024 * 1024)}MB");
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
    }
    
    private void KillProcessTree(Process process)
    {
        try
        {
            // Kill entire process tree, not just parent
            if (OperatingSystem.IsWindows())
            {
                // taskkill /T kills the entire tree
                Process.Start("taskkill", $"/T /F /PID {process.Id}")?.WaitForExit(5000);
            }
            else
            {
                // pkill -P kills children, then kill parent
                Process.Start("pkill", $"-P {process.Id}")?.WaitForExit(1000);
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort - process may have already exited
            try { process.Kill(); } catch { }
        }
    }
    
    internal void Release()
    {
        _monitoredProcess = null;
        _concurrencySemaphore.Release();
    }
    
    public void Dispose()
    {
        _resourceMonitor?.Dispose();
        _concurrencySemaphore.Dispose();
    }
    
    public sealed class ExecutionGuard : IDisposable
    {
        private readonly ResourceLimitEnforcer _enforcer;
        private bool _disposed;
        
        internal ExecutionGuard(ResourceLimitEnforcer enforcer)
        {
            _enforcer = enforcer;
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _enforcer.Release();
                _disposed = true;
            }
        }
    }
}

public class ResourceLimitExceededException : Exception
{
    public ResourceLimitExceededException(string message) : base(message) { }
}
```

---

### Threat 4: Output Buffer Overflow

**Risk:** MEDIUM - Commands producing large output could exhaust memory.

**Attack Scenario:**
```
Command outputs: for i in $(seq 1 1000000); do echo $RANDOM; done
Agent buffers all output in memory
Out of memory exception
Result: Agent crash
```

**Complete Mitigation Code:**

```csharp
using System;
using System.Text;
using System.Threading;

namespace Acode.Infrastructure.CommandExecution;

/// <summary>
/// Captures process output with bounded buffer and truncation.
/// </summary>
public sealed class BoundedOutputCapture
{
    private readonly StringBuilder _buffer;
    private readonly int _maxBytes;
    private readonly object _lock = new();
    private int _currentBytes;
    private bool _truncated;
    private int _droppedBytes;
    
    public BoundedOutputCapture(int maxBytes = 1024 * 1024) // 1 MB default
    {
        _maxBytes = maxBytes;
        _buffer = new StringBuilder(Math.Min(maxBytes, 64 * 1024));
    }
    
    /// <summary>
    /// Appends data to the buffer with truncation if limit exceeded.
    /// </summary>
    public void Append(string? data)
    {
        if (string.IsNullOrEmpty(data))
            return;
        
        lock (_lock)
        {
            var dataBytes = Encoding.UTF8.GetByteCount(data);
            
            if (_currentBytes + dataBytes <= _maxBytes)
            {
                // Fits within limit
                _buffer.Append(data);
                _currentBytes += dataBytes;
            }
            else if (_currentBytes < _maxBytes)
            {
                // Partial fit - take what we can
                var remainingBytes = _maxBytes - _currentBytes;
                var chars = TruncateToBytes(data, remainingBytes);
                _buffer.Append(chars);
                _currentBytes = _maxBytes;
                _truncated = true;
                _droppedBytes = dataBytes - remainingBytes;
            }
            else
            {
                // Already at limit - count dropped bytes
                _truncated = true;
                _droppedBytes += dataBytes;
            }
        }
    }
    
    /// <summary>
    /// Gets the captured output with truncation indicator if applicable.
    /// </summary>
    public CapturedOutput GetOutput()
    {
        lock (_lock)
        {
            var content = _buffer.ToString();
            
            if (_truncated)
            {
                var truncationInfo = new TruncationInfo(
                    OriginalBytes: _currentBytes + _droppedBytes,
                    CapturedBytes: _currentBytes,
                    DroppedBytes: _droppedBytes);
                
                return new CapturedOutput(
                    Content: content + $"\n\n[OUTPUT TRUNCATED: {_droppedBytes:N0} bytes dropped]",
                    IsTruncated: true,
                    TruncationInfo: truncationInfo);
            }
            
            return new CapturedOutput(
                Content: content,
                IsTruncated: false,
                TruncationInfo: null);
        }
    }
    
    private string TruncateToBytes(string data, int maxBytes)
    {
        // Binary search for the right character count
        var low = 0;
        var high = data.Length;
        
        while (low < high)
        {
            var mid = (low + high + 1) / 2;
            var bytes = Encoding.UTF8.GetByteCount(data.AsSpan(0, mid));
            
            if (bytes <= maxBytes)
                low = mid;
            else
                high = mid - 1;
        }
        
        return data.Substring(0, low);
    }
}

public record CapturedOutput(
    string Content,
    bool IsTruncated,
    TruncationInfo? TruncationInfo);

public record TruncationInfo(
    long OriginalBytes,
    long CapturedBytes,
    long DroppedBytes);
```

---

### Threat 5: Audit Log Tampering

**Risk:** MEDIUM - Attackers could modify audit logs to hide malicious activity.

**Attack Scenario:**
```
Attacker gains write access to audit database
Attacker deletes records of malicious commands
Forensic analysis misses attack
Result: Attack goes undetected
```

**Complete Mitigation Code:**

```csharp
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Acode.Infrastructure.CommandExecution;

/// <summary>
/// Provides tamper-evident audit logging with hash chains.
/// </summary>
public sealed class TamperEvidentAuditLogger
{
    private readonly IAuditStore _auditStore;
    private readonly object _hashLock = new();
    private string _previousHash = "GENESIS";
    
    public TamperEvidentAuditLogger(IAuditStore auditStore)
    {
        _auditStore = auditStore;
        
        // Load the last hash from persistent storage
        var lastEntry = _auditStore.GetLastEntry();
        if (lastEntry != null)
        {
            _previousHash = lastEntry.Hash;
        }
    }
    
    /// <summary>
    /// Records an audit event with hash chain for tamper detection.
    /// </summary>
    public void RecordEvent(CommandAuditEvent auditEvent)
    {
        lock (_hashLock)
        {
            // Create hash chain entry
            var entry = new AuditEntry
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                Event = auditEvent,
                PreviousHash = _previousHash,
                SequenceNumber = GetNextSequenceNumber()
            };
            
            // Calculate hash of this entry (including previous hash)
            entry.Hash = CalculateHash(entry);
            
            // Store the entry
            _auditStore.Save(entry);
            
            // Update chain for next entry
            _previousHash = entry.Hash;
        }
    }
    
    /// <summary>
    /// Verifies the integrity of the audit log hash chain.
    /// </summary>
    public AuditVerificationResult VerifyIntegrity()
    {
        var entries = _auditStore.GetAllEntriesOrdered();
        
        string expectedPreviousHash = "GENESIS";
        long expectedSequence = 0;
        
        foreach (var entry in entries)
        {
            // Verify sequence
            if (entry.SequenceNumber != expectedSequence)
            {
                return new AuditVerificationResult(
                    IsValid: false,
                    Error: $"Sequence gap at {entry.SequenceNumber}, expected {expectedSequence}");
            }
            
            // Verify hash chain
            if (entry.PreviousHash != expectedPreviousHash)
            {
                return new AuditVerificationResult(
                    IsValid: false,
                    Error: $"Hash chain broken at {entry.Id}");
            }
            
            // Verify entry hash
            var calculatedHash = CalculateHash(entry);
            if (entry.Hash != calculatedHash)
            {
                return new AuditVerificationResult(
                    IsValid: false,
                    Error: $"Entry hash mismatch at {entry.Id}");
            }
            
            expectedPreviousHash = entry.Hash;
            expectedSequence++;
        }
        
        return new AuditVerificationResult(IsValid: true, Error: null);
    }
    
    private string CalculateHash(AuditEntry entry)
    {
        var content = JsonSerializer.Serialize(new
        {
            entry.Id,
            entry.Timestamp,
            entry.Event,
            entry.PreviousHash,
            entry.SequenceNumber
        });
        
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(bytes);
    }
    
    private long GetNextSequenceNumber()
    {
        return _auditStore.GetMaxSequenceNumber() + 1;
    }
}

public record AuditEntry
{
    public Guid Id { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public CommandAuditEvent Event { get; init; } = null!;
    public string PreviousHash { get; init; } = null!;
    public string Hash { get; set; } = null!;
    public long SequenceNumber { get; init; }
}

public record CommandAuditEvent(
    string EventType,
    string Executable,
    string[] Arguments,
    string? WorkingDirectory,
    int? ExitCode,
    TimeSpan? Duration,
    CorrelationIds CorrelationIds);

public record AuditVerificationResult(bool IsValid, string? Error);

public interface IAuditStore
{
    void Save(AuditEntry entry);
    AuditEntry? GetLastEntry();
    IEnumerable<AuditEntry> GetAllEntriesOrdered();
    long GetMaxSequenceNumber();
}
```

---

## Best Practices

### Coding Practices

1. **Use Command Builder Pattern:** Always construct commands using the fluent builder pattern rather than direct property assignment. This ensures validation is applied consistently.

2. **Prefer Argument Arrays:** Pass arguments as string arrays, not concatenated strings. This prevents injection vulnerabilities and ensures proper escaping.

3. **Set Explicit Timeouts:** Always specify a timeout for command execution. Never rely on defaults for production workloads.

4. **Handle Cancellation:** Pass `CancellationToken` to `ExecuteAsync` and respect cancellation in long-running operations.

### Security Practices

5. **Disable Shell Mode:** Keep `UseShell = false` (default) unless absolutely necessary. Document why shell mode is needed when enabled.

6. **Validate Working Directories:** Ensure working directories are within the repository root. Never allow parent directory traversal.

7. **Redact Secrets:** Enable secret redaction in execution options. Review redaction patterns for your environment.

8. **Audit All Executions:** Never disable audit logging in production. Audit logs are critical for security forensics.

### Performance Practices

9. **Configure Output Limits:** Set appropriate output size limits based on expected command output. 1MB is suitable for most build commands.

10. **Tune Concurrency:** Set concurrency limits based on available CPU cores and memory. Monitor system resources under load.

11. **Use Appropriate Timeouts:** Short timeouts for quick commands (10s for `git status`), longer for builds (5m), maximum for test suites (30m).

### Operational Practices

12. **Monitor Execution Metrics:** Track execution duration, success rate, and timeout frequency. Alert on anomalies.

13. **Rotate Audit Logs:** Configure audit retention policy. Archive old logs before deletion for compliance.

14. **Test Failure Scenarios:** Verify timeout handling, process cleanup, and error reporting during development.

15. **Document Commands:** Maintain a catalog of expected commands with their typical behavior, resource usage, and timeouts.

---

## Troubleshooting

### Issue 1: Command Executable Not Found

**Symptoms:**
- CommandResult with ExitCode = -1
- Error message: "The system cannot find the file specified" (Windows) or "No such file or directory" (Linux)
- Command fails immediately without any stdout/stderr

**Causes:**
- Executable not in PATH environment variable
- Typo in executable name
- Executable requires absolute path
- Shell mode disabled when needed for shell builtins
- Working directory changed PATH resolution

**Solutions:**
1. Verify executable exists:
   ```powershell
   # Windows
   Get-Command dotnet
   
   # Linux/macOS
   which dotnet
   ```

2. Use absolute path:
   ```csharp
   var command = new CommandBuilder("dotnet")
       .WithExecutable("/usr/local/share/dotnet/dotnet")
       .Build();
   ```

3. Check PATH in execution environment:
   ```csharp
   var result = await executor.ExecuteAsync(
       new CommandBuilder("echo").WithArguments("%PATH%")
           .Build(),
       new ExecutionOptions { UseShell = true });
   ```

4. For shell builtins (cd, echo), enable shell mode:
   ```csharp
   new ExecutionOptions { UseShell = true }
   ```

---

### Issue 2: Command Times Out

**Symptoms:**
- CommandResult with `TimedOut = true`
- Partial stdout/stderr captured
- ExitCode may be -1 or signal number
- Duration equals timeout value

**Causes:**
- Command genuinely takes longer than configured timeout
- Infinite loop in command/script
- Waiting for user input (stdin blocked)
- Network operation blocking
- Deadlock in spawned process

**Solutions:**
1. Increase timeout for legitimate long-running commands:
   ```csharp
   var command = new CommandBuilder("dotnet")
       .WithArguments("test", "--no-build")
       .WithTimeout(TimeSpan.FromMinutes(30))
       .Build();
   ```

2. Check for interactive prompts and disable:
   ```csharp
   var command = new CommandBuilder("dotnet")
       .WithArguments("build", "--nologo", "--no-restore")
       .WithEnvironment("DOTNET_CLI_TELEMETRY_OPTOUT", "1")
       .Build();
   ```

3. Add progress output to detect hangs:
   ```csharp
   var command = new CommandBuilder("dotnet")
       .WithArguments("test", "--logger", "console;verbosity=normal")
       .Build();
   ```

4. Monitor process tree for orphaned children:
   ```powershell
   # Check for orphaned dotnet processes
   Get-Process dotnet | Select-Object Id, ProcessName, StartTime
   ```

---

### Issue 3: Output Truncated

**Symptoms:**
- CommandResult contains `[OUTPUT TRUNCATED: X bytes dropped]`
- TruncationInfo is non-null
- Missing end of expected output
- Test/build results incomplete

**Causes:**
- Verbose logging enabled
- Large test suite output
- Debug/trace level logging
- Binary content in output
- Default 1MB limit exceeded

**Solutions:**
1. Increase output limit:
   ```csharp
   var options = new ExecutionOptions
   {
       MaxOutputSizeBytes = 10 * 1024 * 1024 // 10 MB
   };
   ```

2. Reduce verbosity:
   ```csharp
   var command = new CommandBuilder("dotnet")
       .WithArguments("build", "--verbosity", "minimal")
       .Build();
   ```

3. Redirect to file for very large output:
   ```csharp
   var command = new CommandBuilder("dotnet")
       .WithArguments("test", "--logger", "trx;LogFileName=results.trx")
       .Build();
   // Then read results.trx file
   ```

4. Filter output at source:
   ```csharp
   var command = new CommandBuilder("dotnet")
       .WithArguments("test", "--filter", "Category=UnitTest")
       .Build();
   ```

---

### Issue 4: Environment Variables Not Applied

**Symptoms:**
- Command behaves as if environment variables not set
- Works in terminal but fails through executor
- Different behavior than expected

**Causes:**
- EnvironmentMergeMode set incorrectly
- Variable name case sensitivity (Linux)
- Parent process environment not inherited
- Variable contains special characters

**Solutions:**
1. Check merge mode:
   ```csharp
   var options = new ExecutionOptions
   {
       EnvironmentMergeMode = EnvironmentMergeMode.Merge // Default
   };
   ```

2. Verify variable application:
   ```csharp
   var command = new CommandBuilder("printenv")
       .WithEnvironment("MY_VAR", "my_value")
       .Build();
   var result = await executor.ExecuteAsync(command);
   Console.WriteLine(result.Stdout); // Should contain MY_VAR=my_value
   ```

3. For Replace mode, include required system variables:
   ```csharp
   var command = new CommandBuilder("dotnet")
       .WithEnvironment("PATH", Environment.GetEnvironmentVariable("PATH")!)
       .WithEnvironment("HOME", Environment.GetEnvironmentVariable("HOME")!)
       .WithEnvironment("MY_VAR", "my_value")
       .Build();
   ```

---

### Issue 5: Zombie Processes Accumulating

**Symptoms:**
- Process count grows over time
- Memory usage increases
- System becomes sluggish
- `acode exec` commands start timing out

**Causes:**
- Timeout killing parent but not children
- Process tree kill failing
- Exceptions before cleanup runs
- Docker container processes not cleaned

**Solutions:**
1. Verify process tree kill is working:
   ```powershell
   # Windows - after timeout
   Get-Process | Where-Object { $_.ProcessName -like "dotnet*" }
   
   # Linux - check for orphans
   ps aux | grep defunct
   ```

2. Manual cleanup:
   ```powershell
   # Windows
   Get-Process dotnet | Stop-Process -Force
   
   # Linux
   pkill -9 dotnet
   ```

3. Check for platform-specific kill issues:
   ```csharp
   // Enable verbose logging for process management
   var logger = LoggerFactory.Create(builder =>
       builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
   ```

4. Implement periodic cleanup job:
   ```csharp
   // In background service
   private async Task CleanupOrphanedProcesses()
   {
       var ageThreshold = TimeSpan.FromHours(1);
       var processes = Process.GetProcessesByName("dotnet");
       foreach (var p in processes)
       {
           if (DateTime.Now - p.StartTime > ageThreshold)
           {
               p.Kill(entireProcessTree: true);
           }
       }
   }
   ```

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

### Complete Unit Test Implementation

#### CommandTests.cs - Complete Implementation

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using AgenticCoder.Domain.Execution;

namespace AgenticCoder.Tests.Unit.Execution;

public class CommandTests
{
    [Fact]
    public void Command_Constructor_SetsAllProperties()
    {
        // Arrange
        var arguments = new List<string> { "build", "--no-restore" };
        var environment = new Dictionary<string, string> { { "CI", "true" } };
        var timeout = TimeSpan.FromMinutes(5);
        
        // Act
        var command = new Command
        {
            Executable = "dotnet",
            Arguments = arguments,
            WorkingDirectory = "/repo",
            Environment = environment,
            Timeout = timeout
        };
        
        // Assert
        command.Executable.Should().Be("dotnet");
        command.Arguments.Should().BeEquivalentTo(arguments);
        command.WorkingDirectory.Should().Be("/repo");
        command.Environment.Should().BeEquivalentTo(environment);
        command.Timeout.Should().Be(timeout);
    }
    
    [Fact]
    public void Command_Builder_ChainsMethodsCorrectly()
    {
        // Arrange & Act
        var command = Command.Create("dotnet")
            .WithArguments("build", "--no-restore")
            .WithWorkingDirectory("/repo")
            .WithEnvironment("CI", "true")
            .WithEnvironment("DOTNET_CLI_TELEMETRY_OPTOUT", "1")
            .WithTimeout(TimeSpan.FromMinutes(10))
            .Build();
        
        // Assert
        command.Executable.Should().Be("dotnet");
        command.Arguments.Should().HaveCount(2);
        command.Arguments[0].Should().Be("build");
        command.Arguments[1].Should().Be("--no-restore");
        command.WorkingDirectory.Should().Be("/repo");
        command.Environment.Should().ContainKey("CI");
        command.Environment.Should().ContainKey("DOTNET_CLI_TELEMETRY_OPTOUT");
        command.Timeout.Should().Be(TimeSpan.FromMinutes(10));
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Command_Builder_RejectsInvalidExecutable(string? executable)
    {
        // Arrange & Act
        var act = () => new CommandBuilder(executable!);
        
        // Assert
        act.Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void Command_Serialization_RoundTripsCorrectly()
    {
        // Arrange
        var original = Command.Create("dotnet")
            .WithArguments("test")
            .WithWorkingDirectory("/repo/tests")
            .WithTimeout(TimeSpan.FromMinutes(5))
            .Build();
        
        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Command>(json);
        
        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Executable.Should().Be(original.Executable);
        deserialized.Arguments.Should().BeEquivalentTo(original.Arguments);
        deserialized.WorkingDirectory.Should().Be(original.WorkingDirectory);
    }
    
    [Fact]
    public void Command_Equality_WorksCorrectly()
    {
        // Arrange
        var command1 = Command.Create("dotnet").WithArguments("build").Build();
        var command2 = Command.Create("dotnet").WithArguments("build").Build();
        var command3 = Command.Create("dotnet").WithArguments("test").Build();
        
        // Assert
        command1.Should().Be(command2);
        command1.Should().NotBe(command3);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }
}
```

#### CommandResultTests.cs - Complete Implementation

```csharp
using System;
using FluentAssertions;
using Xunit;
using AgenticCoder.Domain.Execution;

namespace AgenticCoder.Tests.Unit.Execution;

public class CommandResultTests
{
    private readonly CorrelationIds _defaultCorrelation = new()
    {
        RunId = "run-123",
        SessionId = "session-456",
        TaskId = "task-789",
        StepId = "step-012",
        ToolCallId = "tool-345"
    };
    
    [Fact]
    public void CommandResult_Duration_CalculatesCorrectly()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow;
        var endTime = startTime.AddSeconds(5);
        
        // Act
        var result = new CommandResult
        {
            Stdout = "output",
            Stderr = "",
            ExitCode = 0,
            StartTime = startTime,
            EndTime = endTime,
            CorrelationIds = _defaultCorrelation
        };
        
        // Assert
        result.Duration.Should().Be(TimeSpan.FromSeconds(5));
    }
    
    [Fact]
    public void CommandResult_Success_TrueWhenExitCodeZero()
    {
        // Arrange & Act
        var result = new CommandResult
        {
            Stdout = "",
            Stderr = "",
            ExitCode = 0,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow,
            TimedOut = false,
            CorrelationIds = _defaultCorrelation
        };
        
        // Assert
        result.Success.Should().BeTrue();
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(255)]
    public void CommandResult_Success_FalseWhenExitCodeNonZero(int exitCode)
    {
        // Arrange & Act
        var result = new CommandResult
        {
            Stdout = "",
            Stderr = "",
            ExitCode = exitCode,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow,
            CorrelationIds = _defaultCorrelation
        };
        
        // Assert
        result.Success.Should().BeFalse();
    }
    
    [Fact]
    public void CommandResult_Success_FalseWhenTimedOut()
    {
        // Arrange & Act
        var result = new CommandResult
        {
            Stdout = "",
            Stderr = "",
            ExitCode = 0, // Even with exit code 0
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow,
            TimedOut = true,
            CorrelationIds = _defaultCorrelation
        };
        
        // Assert
        result.Success.Should().BeFalse();
    }
    
    [Fact]
    public void CommandResult_TruncationInfo_SetWhenTruncated()
    {
        // Arrange & Act
        var result = new CommandResult
        {
            Stdout = "truncated...",
            Stderr = "",
            ExitCode = 0,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow,
            CorrelationIds = _defaultCorrelation,
            Truncation = new TruncationInfo(
                OriginalBytes: 2_000_000,
                TruncatedBytes: 1_000_000,
                Stream: "stdout")
        };
        
        // Assert
        result.Truncation.Should().NotBeNull();
        result.Truncation!.OriginalBytes.Should().Be(2_000_000);
        result.Truncation.TruncatedBytes.Should().Be(1_000_000);
    }
}
```

#### CommandExecutorTests.cs - Complete Implementation

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using AgenticCoder.Application.Execution;
using AgenticCoder.Domain.Execution;

namespace AgenticCoder.Tests.Unit.Execution;

public class CommandExecutorTests
{
    private readonly Mock<IProcessRunner> _processRunnerMock;
    private readonly Mock<ICorrelationContext> _correlationMock;
    private readonly Mock<IExecutionAuditService> _auditMock;
    private readonly CommandExecutor _sut;
    
    public CommandExecutorTests()
    {
        _processRunnerMock = new Mock<IProcessRunner>();
        _correlationMock = new Mock<ICorrelationContext>();
        _auditMock = new Mock<IExecutionAuditService>();
        
        _correlationMock.Setup(c => c.Current).Returns(new CorrelationIds
        {
            RunId = "run-123",
            SessionId = "session-456",
            TaskId = "task-789",
            StepId = "step-012",
            ToolCallId = "tool-345"
        });
        
        _sut = new CommandExecutor(
            _processRunnerMock.Object,
            _correlationMock.Object,
            _auditMock.Object);
    }
    
    [Fact]
    public async Task ExecuteAsync_ReturnsResult()
    {
        // Arrange
        var command = Command.Create("echo").WithArguments("hello").Build();
        _processRunnerMock
            .Setup(p => p.RunAsync(command, It.IsAny<ExecutionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("hello\n", "", 0));
        
        // Act
        var result = await _sut.ExecuteAsync(command);
        
        // Assert
        result.Should().NotBeNull();
        result.ExitCode.Should().Be(0);
        result.Stdout.Should().Contain("hello");
    }
    
    [Fact]
    public async Task ExecuteAsync_CapturesStdoutAndStderr()
    {
        // Arrange
        var command = Command.Create("test").Build();
        _processRunnerMock
            .Setup(p => p.RunAsync(command, It.IsAny<ExecutionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("stdout content", "stderr content", 0));
        
        // Act
        var result = await _sut.ExecuteAsync(command);
        
        // Assert
        result.Stdout.Should().Be("stdout content");
        result.Stderr.Should().Be("stderr content");
    }
    
    [Fact]
    public async Task ExecuteAsync_ThrowsForNullCommand()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.ExecuteAsync(null!));
    }
    
    [Fact]
    public async Task ExecuteAsync_HandlesCancellation()
    {
        // Arrange
        var command = Command.Create("long-running").Build();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.ExecuteAsync(command, ct: cts.Token));
    }
    
    [Fact]
    public async Task ExecuteAsync_SetsCorrelationIds()
    {
        // Arrange
        var command = Command.Create("echo").Build();
        _processRunnerMock
            .Setup(p => p.RunAsync(command, It.IsAny<ExecutionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("", "", 0));
        
        // Act
        var result = await _sut.ExecuteAsync(command);
        
        // Assert
        result.CorrelationIds.Should().NotBeNull();
        result.CorrelationIds.RunId.Should().Be("run-123");
        result.CorrelationIds.SessionId.Should().Be("session-456");
        result.CorrelationIds.TaskId.Should().Be("task-789");
    }
    
    [Fact]
    public async Task ExecuteAsync_RecordsToAudit()
    {
        // Arrange
        var command = Command.Create("echo").Build();
        _processRunnerMock
            .Setup(p => p.RunAsync(command, It.IsAny<ExecutionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("", "", 0));
        
        // Act
        await _sut.ExecuteAsync(command);
        
        // Assert
        _auditMock.Verify(a => a.RecordStart(It.IsAny<CommandAuditEvent>()), Times.Once);
        _auditMock.Verify(a => a.RecordComplete(It.IsAny<CommandAuditEvent>()), Times.Once);
    }
    
    [Fact]
    public async Task ExecuteAsync_SetsTimedOutFlag_WhenTimeout()
    {
        // Arrange
        var command = Command.Create("slow").WithTimeout(TimeSpan.FromMilliseconds(1)).Build();
        _processRunnerMock
            .Setup(p => p.RunAsync(command, It.IsAny<ExecutionOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());
        
        // Act
        var result = await _sut.ExecuteAsync(command);
        
        // Assert
        result.TimedOut.Should().BeTrue();
        result.Success.Should().BeFalse();
    }
}
```

#### ProcessRunnerTests.cs - Complete Implementation

```csharp
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using AgenticCoder.Infrastructure.Execution;
using AgenticCoder.Domain.Execution;

namespace AgenticCoder.Tests.Unit.Execution;

public class ProcessRunnerTests
{
    [Fact]
    public async Task RunAsync_ExecutesSimpleCommand()
    {
        // Arrange
        await using var runner = new ProcessRunner();
        var command = Command.Create("dotnet").WithArguments("--version").Build();
        var options = new ExecutionOptions();
        
        // Act
        var (stdout, stderr, exitCode) = await runner.RunAsync(command, options, CancellationToken.None);
        
        // Assert
        exitCode.Should().Be(0);
        stdout.Should().NotBeNullOrEmpty();
        stdout.Should().Contain(".");  // Version contains periods
    }
    
    [Fact]
    public async Task RunAsync_CapturesStderr()
    {
        // Arrange
        await using var runner = new ProcessRunner();
        var command = Command.Create("dotnet")
            .WithArguments("nonexistent-command-xyz")
            .Build();
        var options = new ExecutionOptions();
        
        // Act
        var (stdout, stderr, exitCode) = await runner.RunAsync(command, options, CancellationToken.None);
        
        // Assert
        exitCode.Should().NotBe(0);
        stderr.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public async Task RunAsync_RespectsWorkingDirectory()
    {
        // Arrange
        await using var runner = new ProcessRunner();
        var tempDir = Path.GetTempPath();
        var command = OperatingSystem.IsWindows()
            ? Command.Create("cmd").WithArguments("/c", "cd").WithWorkingDirectory(tempDir).Build()
            : Command.Create("pwd").WithWorkingDirectory(tempDir).Build();
        var options = new ExecutionOptions();
        
        // Act
        var (stdout, stderr, exitCode) = await runner.RunAsync(command, options, CancellationToken.None);
        
        // Assert
        exitCode.Should().Be(0);
        stdout.Trim().Should().StartWith(tempDir.TrimEnd(Path.DirectorySeparatorChar));
    }
    
    [Fact]
    public async Task RunAsync_PassesEnvironmentVariables()
    {
        // Arrange
        await using var runner = new ProcessRunner();
        var command = OperatingSystem.IsWindows()
            ? Command.Create("cmd").WithArguments("/c", "echo %TEST_VAR%")
                .WithEnvironment("TEST_VAR", "test_value").Build()
            : Command.Create("printenv").WithArguments("TEST_VAR")
                .WithEnvironment("TEST_VAR", "test_value").Build();
        var options = new ExecutionOptions { EnvironmentMergeMode = EnvironmentMergeMode.Merge };
        
        // Act
        var (stdout, stderr, exitCode) = await runner.RunAsync(command, options, CancellationToken.None);
        
        // Assert
        exitCode.Should().Be(0);
        stdout.Trim().Should().Be("test_value");
    }
    
    [Fact]
    public async Task RunAsync_TimesOut_KillsProcess()
    {
        // Arrange
        await using var runner = new ProcessRunner();
        var command = OperatingSystem.IsWindows()
            ? Command.Create("ping").WithArguments("-n", "60", "localhost").WithTimeout(TimeSpan.FromSeconds(1)).Build()
            : Command.Create("sleep").WithArguments("60").WithTimeout(TimeSpan.FromSeconds(1)).Build();
        var options = new ExecutionOptions();
        
        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(
            () => runner.RunAsync(command, options, CancellationToken.None));
    }
}
```

### Integration Tests - Complete Implementation

#### CommandExecutorIntegrationTests.cs

```csharp
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using AgenticCoder.Application.Execution;
using AgenticCoder.Domain.Execution;

namespace AgenticCoder.Tests.Integration.Execution;

public class CommandExecutorIntegrationTests : IClassFixture<ExecutionTestFixture>
{
    private readonly ICommandExecutor _executor;
    
    public CommandExecutorIntegrationTests(ExecutionTestFixture fixture)
    {
        _executor = fixture.Services.GetRequiredService<ICommandExecutor>();
    }
    
    [Fact]
    public async Task ExecuteAsync_RealEchoCommand_CapturesOutput()
    {
        // Arrange
        var command = OperatingSystem.IsWindows()
            ? Command.Create("cmd").WithArguments("/c", "echo", "integration test").Build()
            : Command.Create("echo").WithArguments("integration test").Build();
        
        // Act
        var result = await _executor.ExecuteAsync(command);
        
        // Assert
        result.Success.Should().BeTrue();
        result.ExitCode.Should().Be(0);
        result.Stdout.Should().Contain("integration test");
        result.Duration.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }
    
    [Fact]
    public async Task ExecuteAsync_NonZeroExitCode_ReportsFailure()
    {
        // Arrange
        var command = OperatingSystem.IsWindows()
            ? Command.Create("cmd").WithArguments("/c", "exit 42").Build()
            : Command.Create("sh").WithArguments("-c", "exit 42").Build();
        var options = new ExecutionOptions { UseShell = true };
        
        // Act
        var result = await _executor.ExecuteAsync(command, options);
        
        // Assert
        result.Success.Should().BeFalse();
        result.ExitCode.Should().Be(42);
    }
    
    [Fact]
    public async Task ExecuteAsync_WithTimeout_KillsLongRunningProcess()
    {
        // Arrange
        var command = OperatingSystem.IsWindows()
            ? Command.Create("ping").WithArguments("-n", "60", "localhost").WithTimeout(TimeSpan.FromSeconds(2)).Build()
            : Command.Create("sleep").WithArguments("60").WithTimeout(TimeSpan.FromSeconds(2)).Build();
        
        // Act
        var result = await _executor.ExecuteAsync(command);
        
        // Assert
        result.TimedOut.Should().BeTrue();
        result.Success.Should().BeFalse();
        result.Duration.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }
    
    [Fact]
    public async Task ExecuteAsync_AuditRecorded_InDatabase()
    {
        // Arrange
        var command = Command.Create("dotnet").WithArguments("--version").Build();
        
        // Act
        var result = await _executor.ExecuteAsync(command);
        
        // Assert
        result.CorrelationIds.Should().NotBeNull();
        // Query audit database to verify recording
        // This would use the audit repository to verify the event was persisted
    }
}
```

### Performance Benchmarks

| Benchmark | Method | Target | Maximum | Notes |
|-----------|--------|--------|---------|-------|
| ProcessStartOverhead | `Benchmark_ProcessStart` | 30ms | 50ms | Measure from call to process running |
| OutputCapture1MB | `Benchmark_OutputCapture_1MB` | 5ms | 10ms | 1MB stdout capture time |
| OutputCapture10MB | `Benchmark_OutputCapture_10MB` | 50ms | 100ms | 10MB stdout capture with truncation |
| AuditWrite | `Benchmark_AuditWrite` | 2ms | 5ms | Single audit event write |
| CommandValidation | `Benchmark_Validation` | 0.5ms | 1ms | Full command validation |
| ConcurrentExecution | `Benchmark_Concurrent_4` | N/A | N/A | 4 parallel commands throughput |

### Test Coverage Requirements

| Component | Minimum Coverage |
|-----------|------------------|
| Command.cs | 95% |
| CommandResult.cs | 95% |
| CommandExecutor.cs | 90% |
| ProcessRunner.cs | 85% |
| ExecutionAuditRepository.cs | 85% |
| CommandValidation.cs | 95% |
| Overall | 85% |

---

## User Verification Steps

### Scenario 1: Execute Simple Command

**Objective:** Verify basic command execution works

**Steps:**
1. Open terminal in repository directory
2. Run `acode exec "echo hello world"`
3. Observe output

**Expected Results:**
- Output shows "hello world"
- Exit code is 0
- Success is true
- Duration is recorded

### Scenario 2: Execute Command with Arguments

**Objective:** Verify argument passing works correctly

**Steps:**
1. Run `acode exec dotnet --version`
2. Observe version output

**Expected Results:**
- .NET version displayed (e.g., "8.0.100")
- Exit code is 0
- Arguments passed correctly

### Scenario 3: Capture Exit Code

**Objective:** Verify non-zero exit codes are captured

**Steps:**
1. Run `acode exec "exit 42"` (with --shell flag on Windows)
2. Observe result

**Expected Results:**
- Exit code is 42
- Success is false
- Error message indicates non-zero exit

### Scenario 4: Command Timeout

**Objective:** Verify timeout enforcement works

**Steps:**
1. Run `acode exec "ping -n 60 localhost" --timeout 2` (Windows)
2. Or `acode exec "sleep 60" --timeout 2` (Unix)
3. Observe timeout behavior

**Expected Results:**
- Command killed after ~2 seconds
- TimedOut is true
- Partial output captured
- Duration approximately 2 seconds

### Scenario 5: Working Directory

**Objective:** Verify working directory is respected

**Steps:**
1. Create subdirectory `test-dir` with file `marker.txt`
2. Run `acode exec "ls" --cwd ./test-dir` (Unix) or `acode exec "dir" --cwd ./test-dir` (Windows)
3. Observe output

**Expected Results:**
- Output shows `marker.txt`
- Command executed in specified directory
- Working directory validated before execution

### Scenario 6: Environment Variables

**Objective:** Verify environment variable passing

**Steps:**
1. Run `acode exec "printenv MY_VAR" --env "MY_VAR=test123"` (Unix)
2. Or `acode exec "echo %MY_VAR%" --env "MY_VAR=test123" --shell` (Windows)
3. Observe output

**Expected Results:**
- Output shows "test123"
- Environment variable was passed to command
- Environment merge mode respected

### Scenario 7: View Execution History

**Objective:** Verify audit trail is maintained

**Steps:**
1. Execute several commands: `acode exec "echo one"`, `acode exec "echo two"`, `acode exec "echo three"`
2. Run `acode runs list`
3. Observe history

**Expected Results:**
- All three executions listed
- Each has unique execution ID
- Timestamps are correct
- Commands are shown

### Scenario 8: View Execution Details

**Objective:** Verify detailed execution info is available

**Steps:**
1. Execute `acode exec "dotnet build"`
2. Note the execution ID from output
3. Run `acode runs show <exec-id>`
4. Observe details

**Expected Results:**
- Full command details shown
- Complete stdout/stderr captured
- Correlation IDs present
- Duration and timestamps accurate

### Scenario 9: Large Output Handling

**Objective:** Verify output truncation works

**Steps:**
1. Create or find command that produces large output (> 1MB)
2. Run with default max_output_kb
3. Observe truncation

**Expected Results:**
- Output is truncated
- Truncation info shows original and truncated sizes
- "[OUTPUT TRUNCATED]" marker present
- No memory exhaustion

### Scenario 10: Concurrent Execution Limit

**Objective:** Verify concurrency limit is enforced

**Steps:**
1. Configure `max_concurrent: 2` in config
2. Start 4 concurrent long-running commands
3. Observe behavior

**Expected Results:**
- Only 2 commands run simultaneously
- Additional commands queue and wait
- All commands eventually complete
- No deadlock occurs

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Execution/
│   ├── ICommand.cs              # Command interface
│   ├── Command.cs               # Command record
│   ├── CommandBuilder.cs        # Fluent builder
│   ├── CommandResult.cs         # Execution result
│   ├── CommandError.cs          # Structured error
│   ├── CorrelationIds.cs        # Correlation context
│   ├── TruncationInfo.cs        # Output truncation info
│   └── ExecutionOptions.cs      # Execution options
│
src/AgenticCoder.Application/
├── Execution/
│   ├── ICommandExecutor.cs      # Executor interface
│   ├── CommandExecutor.cs       # Main implementation
│   └── IExecutionAuditService.cs # Audit service interface
│
src/AgenticCoder.Infrastructure/
├── Execution/
│   ├── ProcessRunner.cs         # Process execution
│   ├── ProcessKiller.cs         # Process tree killer
│   ├── OutputCapture.cs         # Async output capture
│   ├── EnvironmentMerger.cs     # Environment handling
│   ├── SecretRedactor.cs        # Secret redaction
│   ├── ExecutionAuditRepository.cs # Audit persistence
│   └── ExecutionConfiguration.cs # Config binding
│
src/AgenticCoder.CLI/
├── Commands/
│   ├── ExecCommand.cs           # acode exec
│   ├── RunsListCommand.cs       # acode runs list
│   └── RunsShowCommand.cs       # acode runs show
│
Tests/Unit/Execution/
├── CommandTests.cs
├── CommandResultTests.cs
├── CommandExecutorTests.cs
├── ProcessRunnerTests.cs
├── ExecutionAuditTests.cs
└── CommandValidationTests.cs
│
Tests/Integration/Execution/
├── CommandExecutorIntegrationTests.cs
├── AuditIntegrationTests.cs
└── CLIIntegrationTests.cs
```

### ICommand Interface

```csharp
namespace AgenticCoder.Domain.Execution;

/// <summary>
/// Represents a command to be executed by the command runner.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// The executable to run (e.g., "dotnet", "npm", "/usr/bin/make").
    /// </summary>
    string Executable { get; }
    
    /// <summary>
    /// Arguments to pass to the executable.
    /// </summary>
    IReadOnlyList<string> Arguments { get; }
    
    /// <summary>
    /// Working directory for execution. Null means repository root.
    /// </summary>
    string? WorkingDirectory { get; }
    
    /// <summary>
    /// Environment variables to set. Null means inherit only.
    /// </summary>
    IReadOnlyDictionary<string, string>? Environment { get; }
    
    /// <summary>
    /// Timeout for execution. Null means use default.
    /// </summary>
    TimeSpan? Timeout { get; }
}
```

### Command Record

```csharp
namespace AgenticCoder.Domain.Execution;

/// <summary>
/// Immutable command definition.
/// </summary>
public sealed record Command : ICommand
{
    public required string Executable { get; init; }
    public IReadOnlyList<string> Arguments { get; init; } = [];
    public string? WorkingDirectory { get; init; }
    public IReadOnlyDictionary<string, string>? Environment { get; init; }
    public TimeSpan? Timeout { get; init; }
    public ResourceLimits? ResourceLimits { get; init; }
    
    /// <summary>
    /// Creates a fluent builder for command construction.
    /// </summary>
    public static CommandBuilder Create(string executable) => 
        new CommandBuilder(executable);
}

/// <summary>
/// Fluent builder for Command construction.
/// </summary>
public sealed class CommandBuilder
{
    private readonly string _executable;
    private readonly List<string> _arguments = [];
    private string? _workingDirectory;
    private readonly Dictionary<string, string> _environment = [];
    private TimeSpan? _timeout;
    
    public CommandBuilder(string executable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        _executable = executable;
    }
    
    public CommandBuilder WithArguments(params string[] args)
    {
        _arguments.AddRange(args);
        return this;
    }
    
    public CommandBuilder WithWorkingDirectory(string directory)
    {
        _workingDirectory = directory;
        return this;
    }
    
    public CommandBuilder WithEnvironment(string key, string value)
    {
        _environment[key] = value;
        return this;
    }
    
    public CommandBuilder WithTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }
    
    public Command Build() => new Command
    {
        Executable = _executable,
        Arguments = _arguments.AsReadOnly(),
        WorkingDirectory = _workingDirectory,
        Environment = _environment.Count > 0 
            ? _environment.AsReadOnly() 
            : null,
        Timeout = _timeout
    };
}
```

### CommandResult Record

```csharp
namespace AgenticCoder.Domain.Execution;

/// <summary>
/// Immutable result from command execution.
/// </summary>
public sealed record CommandResult
{
    public required string Stdout { get; init; }
    public required string Stderr { get; init; }
    public required int ExitCode { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public TimeSpan Duration => EndTime - StartTime;
    public bool Success => ExitCode == 0 && !TimedOut;
    public bool TimedOut { get; init; }
    public CommandError? Error { get; init; }
    public required CorrelationIds CorrelationIds { get; init; }
    public TruncationInfo? Truncation { get; init; }
}

/// <summary>
/// Structured error information.
/// </summary>
public sealed record CommandError(
    string Code,
    string Message,
    string? Details = null
);

/// <summary>
/// Correlation IDs for tracing.
/// </summary>
public sealed record CorrelationIds
{
    public required string RunId { get; init; }
    public required string SessionId { get; init; }
    public required string TaskId { get; init; }
    public required string StepId { get; init; }
    public required string ToolCallId { get; init; }
    public string? WorktreeId { get; init; }
    public string? RepoSha { get; init; }
}

/// <summary>
/// Information about output truncation.
/// </summary>
public sealed record TruncationInfo(
    long OriginalBytes,
    long TruncatedBytes,
    string Stream // "stdout", "stderr", or "both"
);
```

### ICommandExecutor Interface

```csharp
namespace AgenticCoder.Application.Execution;

/// <summary>
/// Executes commands and returns structured results.
/// </summary>
public interface ICommandExecutor
{
    /// <summary>
    /// Executes a command and returns the result.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="options">Optional execution options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Structured execution result.</returns>
    /// <exception cref="ArgumentNullException">Command is null.</exception>
    /// <exception cref="CommandValidationException">Command is invalid.</exception>
    Task<CommandResult> ExecuteAsync(
        Command command,
        ExecutionOptions? options = null,
        CancellationToken ct = default);
}
```

### ProcessRunner Core Logic

```csharp
namespace AgenticCoder.Infrastructure.Execution;

/// <summary>
/// Executes processes and captures output.
/// </summary>
internal sealed class ProcessRunner : IAsyncDisposable
{
    private readonly ILogger<ProcessRunner> _logger;
    private readonly Process _process;
    private readonly StringBuilder _stdout = new();
    private readonly StringBuilder _stderr = new();
    
    public async Task<(string stdout, string stderr, int exitCode)> RunAsync(
        Command command,
        ExecutionOptions options,
        CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command.Executable,
            UseShellExecute = options.UseShell,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = command.WorkingDirectory ?? Environment.CurrentDirectory
        };
        
        // Add arguments
        foreach (var arg in command.Arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }
        
        // Merge environment
        MergeEnvironment(startInfo, command.Environment, options.EnvironmentMergeMode);
        
        _process = new Process { StartInfo = startInfo };
        
        // Set up async output capture
        _process.OutputDataReceived += (_, e) => 
        {
            if (e.Data != null) _stdout.AppendLine(e.Data);
        };
        _process.ErrorDataReceived += (_, e) => 
        {
            if (e.Data != null) _stderr.AppendLine(e.Data);
        };
        
        // Start process
        if (!_process.Start())
        {
            throw new InvalidOperationException("Failed to start process");
        }
        
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        
        // Wait with timeout
        var timeout = options.TimeoutOverride ?? 
                     command.Timeout ?? 
                     TimeSpan.FromSeconds(300);
        
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        
        try
        {
            await _process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            await KillProcessTreeAsync();
            throw new TimeoutException($"Command timed out after {timeout}");
        }
        
        return (_stdout.ToString(), _stderr.ToString(), _process.ExitCode);
    }
    
    private async Task KillProcessTreeAsync()
    {
        try
        {
            // Kill entire process tree
            ProcessKiller.KillTree(_process.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to kill process tree");
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        _process?.Dispose();
    }
}
```

### Error Codes

| Code | Meaning | Resolution |
|------|---------|------------|
| ACODE-EXE-001 | Command not found | Verify executable path and PATH |
| ACODE-EXE-002 | Permission denied | Check file permissions |
| ACODE-EXE-003 | Working directory not found | Verify directory exists |
| ACODE-EXE-004 | Timeout exceeded | Increase timeout or investigate command |
| ACODE-EXE-005 | Process crashed | Check command and environment |
| ACODE-EXE-006 | Output too large | Reduce verbosity or increase limit |
| ACODE-EXE-007 | Validation failed | Check command parameters |
| ACODE-EXE-008 | Concurrency limit | Wait for other commands to complete |

### Configuration Schema

```yaml
# JSON Schema for execution configuration
$schema: http://json-schema.org/draft-07/schema#
type: object
properties:
  execution:
    type: object
    properties:
      default_timeout_seconds:
        type: integer
        minimum: 1
        maximum: 3600
        default: 300
      max_output_kb:
        type: integer
        minimum: 1
        maximum: 102400
        default: 1024
      use_shell:
        type: boolean
        default: false
      max_concurrent:
        type: integer
        minimum: 1
        maximum: 32
        default: 4
      environment:
        type: object
        properties:
          mode:
            type: string
            enum: [inherit, replace, merge]
            default: inherit
      audit:
        type: object
        properties:
          enabled:
            type: boolean
            default: true
          retention_days:
            type: integer
            minimum: 1
            default: 30
```

### Implementation Checklist

1. [ ] Create Domain models (Command, CommandResult, etc.)
2. [ ] Create CommandBuilder with fluent API
3. [ ] Implement command validation
4. [ ] Create ICommandExecutor interface
5. [ ] Implement ProcessRunner for native execution
6. [ ] Implement ProcessKiller for process tree termination
7. [ ] Implement OutputCapture with truncation
8. [ ] Implement EnvironmentMerger
9. [ ] Implement SecretRedactor
10. [ ] Create ExecutionAuditRepository
11. [ ] Register services in DI container
12. [ ] Create acode exec CLI command
13. [ ] Create acode runs list CLI command
14. [ ] Create acode runs show CLI command
15. [ ] Write unit tests for all components
16. [ ] Write integration tests
17. [ ] Write E2E tests
18. [ ] Create performance benchmarks
19. [ ] Document configuration options
20. [ ] Document error codes

### Rollout Plan

| Phase | Description | Duration | Success Criteria |
|-------|-------------|----------|------------------|
| 1 | Domain models | 2 days | Command, CommandResult defined, validated, serializable |
| 2 | Process execution | 3 days | Commands execute, output captured, timeout works |
| 3 | Error handling | 2 days | All failure modes handled, structured errors |
| 4 | Audit recording | 2 days | All executions logged, queryable |
| 5 | CLI commands | 2 days | acode exec and runs commands work |
| 6 | Testing & docs | 3 days | 85%+ coverage, docs complete |

---

**End of Task 018 Specification**