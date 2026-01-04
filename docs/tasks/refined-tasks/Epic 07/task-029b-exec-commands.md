# Task 029.b: Exec Commands

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 029 (Interface), Task 029.a (Prepare)  

---

## Description

Task 029.b implements command execution on compute targets. Commands MUST run in the prepared workspace. Output MUST be captured and streamed.

Execution MUST support interactive and batch modes. Timeouts MUST be enforced. Exit codes MUST be captured. Environment variables MUST be configurable.

Execution MUST be cancellable. Long-running commands MUST support cancellation. Cancelled commands MUST cleanup properly. Partial output MUST be preserved.

### Business Value

Command execution enables:
- Build operations
- Test execution
- Code analysis
- Artifact generation

### Scope Boundaries

This task covers command execution. Preparation is in 029.a. Artifacts are in 029.c.

### Integration Points

| Component | Integration Type | Description |
|-----------|-----------------|-------------|
| Task 029.a Workspace | Prerequisite | Provides prepared workspace for command execution |
| Task 027 Workers | Consumer | Workers invoke ExecuteAsync for task operations |
| Task 030-031 Remote Targets | Override | Override process runner for SSH/cloud execution |
| IExecutionService | Interface | Main contract for execution logic |
| IProcessRunner | Strategy | Platform-specific process management |
| IOutputBuffer | Component | Manages output streaming and buffering |
| IOutputHandler | Callback | User-provided output streaming handler |

### Failure Modes

| Failure Type | Detection | Recovery | User Impact |
|--------------|-----------|----------|-------------|
| Command not found | Exit code 127 | Clear error with command name | Fix command path |
| Timeout exceeded | Timer expiration | SIGTERM → SIGKILL, report | Command killed |
| Process crash | Unexpected exit | Capture partial output | Report crash with output |
| SIGTERM/SIGINT | Signal handler | Graceful shutdown | Clean termination |
| Out of memory | Exit code 137 | Report with memory used | Increase memory limit |
| Disk full | Write error | Report space needed | Free disk space |
| Permission denied | Exit code 126 | Clear error with file path | Fix permissions |
| Encoding error | Exception | Replace invalid chars | Log warning |

---

## Assumptions

1. **Workspace Ready**: PrepareWorkspaceAsync has been called and target is in Ready state
2. **Shell Available**: The configured shell (/bin/bash, cmd.exe) is available on the target
3. **Environment Inheritable**: Process environment can be inherited and extended
4. **UTF-8 Output**: Command output is UTF-8 encoded (or convertible)
5. **Process Signals**: Target OS supports SIGTERM/SIGKILL (Unix) or TerminateProcess (Windows)
6. **Resource Limits**: Memory and CPU limits are enforced at the container/process level
7. **Time Synchronization**: System clock is accurate for execution duration measurement
8. **Filesystem Access**: Commands can read/write within the workspace directory

---

## Security Considerations

1. **Command Injection Prevention**: Commands are passed as single strings to shell; user input must be escaped
2. **Secret Protection**: Environment variables marked as secrets MUST NOT appear in logs
3. **Output Redaction**: Sensitive patterns (API keys, passwords) must be redacted in stored output
4. **Privilege Control**: Commands execute as configured user, never as root unless explicitly configured
5. **Path Traversal**: Working directory cannot be set outside workspace without explicit permission
6. **Resource Limits**: CPU/memory limits prevent resource exhaustion attacks
7. **Network Access**: Commands respect mode constraints (no external network in airgapped)
8. **Audit Trail**: All command executions logged with user, command, duration, exit code

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Command | Shell command to run |
| Execution | Running a command |
| Exit Code | Process return value |
| Stdout | Standard output |
| Stderr | Standard error |
| Timeout | Maximum duration |

---

## Out of Scope

- GUI applications
- Interactive shells
- Pseudo-TTY allocation
- Process tree management
- Container exec

---

## Functional Requirements

### Command Execution (FR-029B-01 to FR-029B-30)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029B-01 | `ExecuteAsync(ExecutionCommand, CancellationToken)` MUST be defined | Must Have |
| FR-029B-02 | ExecutionCommand MUST include command string property | Must Have |
| FR-029B-03 | Shell MUST be configurable per command | Should Have |
| FR-029B-04 | Default shell on Unix: `/bin/bash -c` | Must Have |
| FR-029B-05 | Default shell on Windows: `cmd /c` | Must Have |
| FR-029B-06 | Working directory MUST default to workspace root | Must Have |
| FR-029B-07 | Working directory MUST be overridable via ExecutionCommand | Should Have |
| FR-029B-08 | Process MUST inherit parent environment variables | Must Have |
| FR-029B-09 | Additional environment variables MUST be addable via command | Must Have |
| FR-029B-10 | Command environment MUST override inherited values | Must Have |
| FR-029B-11 | Secrets MUST be passed via environment variables | Must Have |
| FR-029B-12 | Secrets MUST NOT appear in logs or stored output | Must Have |
| FR-029B-13 | Timeout MUST be enforced per command | Must Have |
| FR-029B-14 | Timeout MUST be configurable (default from config) | Must Have |
| FR-029B-15 | Timeout expiration MUST kill the process | Must Have |
| FR-029B-16 | Kill sequence: SIGTERM first, wait 10s | Must Have |
| FR-029B-17 | Kill sequence: SIGKILL if process survives | Must Have |
| FR-029B-18 | Process exit code MUST be captured | Must Have |
| FR-029B-19 | Exit code 0 MUST be treated as success | Must Have |
| FR-029B-20 | Exit code non-zero MUST be treated as failure | Must Have |
| FR-029B-21 | Standard output (stdout) MUST be captured | Must Have |
| FR-029B-22 | Standard error (stderr) MUST be captured | Must Have |
| FR-029B-23 | Combined output (stdout+stderr interleaved) MUST be available | Should Have |
| FR-029B-24 | Separate stdout and stderr MUST be available | Must Have |
| FR-029B-25 | Real-time streaming MUST be supported | Must Have |
| FR-029B-26 | Output streaming callback MUST receive lines as generated | Must Have |
| FR-029B-27 | Output buffer MUST have configurable maximum size | Should Have |
| FR-029B-28 | Default buffer size: 10MB | Should Have |
| FR-029B-29 | Buffer overflow MUST truncate oldest content | Should Have |
| FR-029B-30 | Truncation MUST set flag in ExecutionResult | Must Have |

### Result Handling (FR-029B-31 to FR-029B-50)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029B-31 | `ExecutionResult` record MUST be returned | Must Have |
| FR-029B-32 | Result MUST include ExitCode (int) | Must Have |
| FR-029B-33 | Result MUST include Stdout (string) | Must Have |
| FR-029B-34 | Result MUST include Stderr (string) | Must Have |
| FR-029B-35 | Result MUST include Duration (TimeSpan) | Must Have |
| FR-029B-36 | Result MUST include StartedAt (DateTimeOffset) | Must Have |
| FR-029B-37 | Result MUST include CompletedAt (DateTimeOffset) | Must Have |
| FR-029B-38 | Result MUST include OutputTruncated (bool) | Should Have |
| FR-029B-39 | Result MUST include Success computed property | Must Have |
| FR-029B-40 | Success MUST be true when ExitCode == 0 | Must Have |
| FR-029B-41 | Result MUST include TimedOut (bool) | Must Have |
| FR-029B-42 | Result MUST include Cancelled (bool) | Must Have |
| FR-029B-43 | Common error patterns MUST be parsed from output | Should Have |
| FR-029B-44 | Build errors (MSB*, CS*) MUST be extracted | Should Have |
| FR-029B-45 | Test failures MUST be extracted | Should Have |
| FR-029B-46 | Parsed errors MUST be available in Errors collection | Should Have |
| FR-029B-47 | Result MUST be JSON serializable | Must Have |
| FR-029B-48 | Result MUST support structured logging | Must Have |
| FR-029B-49 | Sensitive output MUST support redaction | Should Have |
| FR-029B-50 | Result MUST include Command for debugging | Should Have |

### Streaming (FR-029B-51 to FR-029B-65)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029B-51 | IOutputHandler callback interface MUST be defined | Must Have |
| FR-029B-52 | Handler MUST receive output per-line | Must Have |
| FR-029B-53 | Handler MUST support per-chunk mode | Should Have |
| FR-029B-54 | Chunk size MUST be configurable | Should Have |
| FR-029B-55 | Default chunk size: 4KB | Should Have |
| FR-029B-56 | OutputLine MUST indicate stream type (stdout/stderr) | Must Have |
| FR-029B-57 | OutputLine MUST include timestamp | Should Have |
| FR-029B-58 | Backpressure MUST be handled when consumer is slow | Should Have |
| FR-029B-59 | Slow consumer MUST buffer up to limit | Should Have |
| FR-029B-60 | Buffer full MUST drop oldest chunks | Should Have |
| FR-029B-61 | Streaming MUST NOT block command execution | Must Have |
| FR-029B-62 | Final result MUST include all output (up to buffer) | Must Have |
| FR-029B-63 | Stream end MUST be signaled to handler | Must Have |
| FR-029B-64 | Handler exceptions MUST be caught and logged | Must Have |
| FR-029B-65 | Handler MUST receive output even if execution fails | Must Have |

### Cancellation (FR-029B-66 to FR-029B-75)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029B-66 | CancellationToken MUST be respected | Must Have |
| FR-029B-67 | Cancellation MUST kill running process | Must Have |
| FR-029B-68 | Cancellation MUST use graceful shutdown sequence | Should Have |
| FR-029B-69 | Partial output MUST be captured before kill | Must Have |
| FR-029B-70 | Result.Cancelled MUST be true after cancellation | Must Have |
| FR-029B-71 | Cancellation MUST not throw OperationCanceledException by default | Should Have |
| FR-029B-72 | ThrowOnCancellation option MUST be available | Should Have |
| FR-029B-73 | Child processes MUST be killed on cancellation | Should Have |
| FR-029B-74 | Cancellation response time MUST be <1 second | Must Have |
| FR-029B-75 | Post-cancellation cleanup MUST complete within 5 seconds | Must Have |

---

## Non-Functional Requirements

### Performance (NFR-029B-01 to NFR-029B-10)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029B-01 | Execution start latency | <100ms from call | Must Have |
| NFR-029B-02 | Output streaming latency | <100ms from generation | Must Have |
| NFR-029B-03 | High-volume output throughput | 1MB/s minimum | Should Have |
| NFR-029B-04 | Concurrent executions supported | 10 per target | Should Have |
| NFR-029B-05 | Memory per execution (excluding output) | <50MB | Should Have |
| NFR-029B-06 | Output buffer memory | Configurable, default 10MB | Should Have |
| NFR-029B-07 | Process spawn time | <50ms | Should Have |
| NFR-029B-08 | Exit code detection latency | <10ms after exit | Must Have |
| NFR-029B-09 | Cancellation response time | <1 second | Must Have |
| NFR-029B-10 | Cleanup time after termination | <5 seconds | Must Have |

### Reliability (NFR-029B-11 to NFR-029B-20)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029B-11 | No zombie processes | 100% cleanup | Must Have |
| NFR-029B-12 | No orphan processes | Kill process tree | Must Have |
| NFR-029B-13 | Output encoding robustness | Handle invalid UTF-8 | Must Have |
| NFR-029B-14 | Long-running command support | Hours without memory leak | Should Have |
| NFR-029B-15 | Concurrent execution isolation | No cross-talk | Must Have |
| NFR-029B-16 | Resource cleanup on exception | 100% | Must Have |
| NFR-029B-17 | Process handle leak prevention | 0 leaked handles | Must Have |
| NFR-029B-18 | File descriptor cleanup | All closed on exit | Must Have |
| NFR-029B-19 | Graceful timeout handling | SIGTERM → SIGKILL | Must Have |
| NFR-029B-20 | Cross-platform behavior consistency | Same API semantics | Must Have |

### Observability (NFR-029B-21 to NFR-029B-30)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029B-21 | Execution start log | Info level with command | Must Have |
| NFR-029B-22 | Execution complete log | Info level with exit code, duration | Must Have |
| NFR-029B-23 | Timeout log | Warning level | Must Have |
| NFR-029B-24 | Cancellation log | Info level | Must Have |
| NFR-029B-25 | Output streaming log | Debug level | Should Have |
| NFR-029B-26 | Error logs with context | Error level with stderr sample | Must Have |
| NFR-029B-27 | Structured logging format | JSON-compatible | Should Have |
| NFR-029B-28 | TargetId and CommandId in logs | Correlation | Must Have |
| NFR-029B-29 | Metric: execution_duration_seconds | Histogram | Should Have |
| NFR-029B-30 | Metric: execution_exit_code | Counter per code | Should Have |

---

## User Manual Documentation

### Configuration

```yaml
execution:
  shell: /bin/bash -c
  defaultTimeoutSeconds: 3600
  outputBufferMb: 10
  streamChunkBytes: 4096
  
  environment:
    CI: true
    DOTNET_CLI_TELEMETRY_OPTOUT: 1
```

### Example Usage

```csharp
var result = await target.ExecuteAsync(new ExecutionCommand(
    Command: "dotnet build",
    Timeout: TimeSpan.FromMinutes(10),
    Environment: new Dictionary<string, string>
    {
        ["Configuration"] = "Release"
    }
));

if (result.Success)
{
    Console.WriteLine($"Build succeeded in {result.Duration}");
}
else
{
    Console.WriteLine($"Build failed: {result.Stderr}");
}
```

### Streaming Output

```csharp
await target.ExecuteAsync(command, outputHandler: line =>
{
    Console.WriteLine($"[{line.Stream}] {line.Text}");
});
```

---

## Acceptance Criteria / Definition of Done

### Core Execution (AC-029B-01 to AC-029B-15)

- [ ] AC-029B-01: `IExecutionService` interface defined in Application layer
- [ ] AC-029B-02: `ExecuteAsync` method accepts ExecutionCommand and CancellationToken
- [ ] AC-029B-03: ExecutionCommand record includes Command, Timeout, Environment properties
- [ ] AC-029B-04: ExecutionCommand includes optional WorkingDirectory override
- [ ] AC-029B-05: ExecutionCommand includes optional Shell override
- [ ] AC-029B-06: Default shell is /bin/bash on Unix, cmd.exe on Windows
- [ ] AC-029B-07: Working directory defaults to prepared workspace root
- [ ] AC-029B-08: Parent environment inherited by spawned process
- [ ] AC-029B-09: Command environment variables added to process
- [ ] AC-029B-10: Command environment overrides inherited values
- [ ] AC-029B-11: Process spawned within 100ms of call
- [ ] AC-029B-12: Command runs asynchronously with proper awaiting
- [ ] AC-029B-13: Multiple concurrent executions supported
- [ ] AC-029B-14: Each execution isolated (no cross-talk)
- [ ] AC-029B-15: Target state set to Executing during execution

### Output Capture (AC-029B-16 to AC-029B-30)

- [ ] AC-029B-16: Stdout captured to string in result
- [ ] AC-029B-17: Stderr captured to string in result
- [ ] AC-029B-18: Combined output available via property
- [ ] AC-029B-19: Separate stdout/stderr preserved
- [ ] AC-029B-20: Output buffer configurable (default 10MB)
- [ ] AC-029B-21: Buffer overflow truncates oldest content
- [ ] AC-029B-22: OutputTruncated flag set when truncated
- [ ] AC-029B-23: Invalid UTF-8 bytes handled gracefully
- [ ] AC-029B-24: Binary output converted to string representation
- [ ] AC-029B-25: Large output (100MB+) doesn't crash
- [ ] AC-029B-26: Output capture doesn't block command
- [ ] AC-029B-27: Memory usage bounded by buffer size
- [ ] AC-029B-28: Output encoding detection works
- [ ] AC-029B-29: Line endings normalized to \n
- [ ] AC-029B-30: Empty output results in empty string (not null)

### Streaming (AC-029B-31 to AC-029B-40)

- [ ] AC-029B-31: IOutputHandler interface defined
- [ ] AC-029B-32: Handler receives lines as generated
- [ ] AC-029B-33: Handler receives within 100ms of generation
- [ ] AC-029B-34: OutputLine includes stream type (stdout/stderr)
- [ ] AC-029B-35: OutputLine includes timestamp
- [ ] AC-029B-36: Chunk mode option for binary-like output
- [ ] AC-029B-37: Slow handler buffered up to limit
- [ ] AC-029B-38: Handler exceptions caught and logged
- [ ] AC-029B-39: Stream end signaled to handler
- [ ] AC-029B-40: Handler failures don't affect command execution

### Exit Code and Result (AC-029B-41 to AC-029B-50)

- [ ] AC-029B-41: Exit code captured accurately
- [ ] AC-029B-42: Exit code 0 sets Success = true
- [ ] AC-029B-43: Exit code non-zero sets Success = false
- [ ] AC-029B-44: Duration calculated accurately
- [ ] AC-029B-45: StartedAt and CompletedAt timestamps accurate
- [ ] AC-029B-46: TimedOut flag set on timeout
- [ ] AC-029B-47: Cancelled flag set on cancellation
- [ ] AC-029B-48: Result serializable to JSON
- [ ] AC-029B-49: Result loggable in structured format
- [ ] AC-029B-50: Sensitive content redactable

### Timeout and Cancellation (AC-029B-51 to AC-029B-60)

- [ ] AC-029B-51: Timeout enforced per command
- [ ] AC-029B-52: Default timeout from configuration
- [ ] AC-029B-53: Per-command timeout override works
- [ ] AC-029B-54: Timeout triggers SIGTERM first
- [ ] AC-029B-55: SIGKILL sent after 10 seconds if needed
- [ ] AC-029B-56: CancellationToken respected
- [ ] AC-029B-57: Cancellation kills running process
- [ ] AC-029B-58: Partial output preserved on cancellation
- [ ] AC-029B-59: Cancellation response <1 second
- [ ] AC-029B-60: Child processes killed on cancellation

### Error Parsing (AC-029B-61 to AC-029B-70)

- [ ] AC-029B-61: MSBuild errors (MSB*) extracted
- [ ] AC-029B-62: C# compiler errors (CS*) extracted
- [ ] AC-029B-63: Test failures extracted
- [ ] AC-029B-64: Common error patterns recognized
- [ ] AC-029B-65: Errors collection populated in result
- [ ] AC-029B-66: Error includes file, line, message
- [ ] AC-029B-67: Error extraction doesn't fail on unexpected format
- [ ] AC-029B-68: Custom error patterns configurable
- [ ] AC-029B-69: Error parsing optional (can be disabled)
- [ ] AC-029B-70: Error count reported in logs

### Security and Cleanup (AC-029B-71 to AC-029B-80)

- [ ] AC-029B-71: Secrets in environment not logged
- [ ] AC-029B-72: Secrets not included in stored output
- [ ] AC-029B-73: No zombie processes after execution
- [ ] AC-029B-74: No orphan processes after timeout
- [ ] AC-029B-75: Process handles closed properly
- [ ] AC-029B-76: File descriptors cleaned up
- [ ] AC-029B-77: Temporary files removed
- [ ] AC-029B-78: Memory released after execution
- [ ] AC-029B-79: Cross-platform works (Windows, macOS, Linux)
- [ ] AC-029B-80: Target state returned to Ready after execution

---

## User Verification Scenarios

### Scenario 1: Successful Build Execution

**Persona:** Developer building .NET project

**Steps:**
1. Prepare workspace with .NET project
2. Execute `dotnet build -c Release`
3. Observe streaming output in console
4. Observe: "Build succeeded"
5. Check result.Success == true
6. Check result.ExitCode == 0
7. Check result.Duration is reasonable

**Verification:**
- [ ] Build output streams in real-time
- [ ] Success property is true
- [ ] Duration is accurate
- [ ] No orphan processes

### Scenario 2: Failed Build with Error Extraction

**Persona:** Developer with compile errors

**Steps:**
1. Introduce syntax error in code
2. Execute `dotnet build`
3. Observe build fails
4. Check result.Success == false
5. Check result.Errors contains parsed error
6. Error includes file name and line number

**Verification:**
- [ ] Failure detected correctly
- [ ] Errors extracted with location
- [ ] Stderr captured with full output

### Scenario 3: Timeout on Long-Running Command

**Persona:** Developer with stuck command

**Steps:**
1. Execute command with 10-second timeout: `sleep 300`
2. Wait for timeout
3. Observe: "Command timed out after 10s"
4. Check result.TimedOut == true
5. Check process is killed
6. Check no zombie processes

**Verification:**
- [ ] Timeout triggers at correct time
- [ ] Process killed gracefully
- [ ] TimedOut flag set correctly
- [ ] Partial output preserved

### Scenario 4: Cancellation During Execution

**Persona:** Developer cancelling command

**Steps:**
1. Start long-running command
2. Press Ctrl+C or cancel programmatically
3. Observe: "Execution cancelled"
4. Check result.Cancelled == true
5. Check process killed within 1 second
6. Check partial output available

**Verification:**
- [ ] Cancellation responsive
- [ ] Process killed cleanly
- [ ] Partial output preserved
- [ ] No orphan processes

### Scenario 5: High-Volume Output Streaming

**Persona:** Developer running verbose tests

**Steps:**
1. Execute command generating 100MB output
2. Observe streaming doesn't lag
3. Check output buffer limits respected
4. Check OutputTruncated flag if applicable
5. Check memory usage stays bounded

**Verification:**
- [ ] Streaming keeps up (1MB/s)
- [ ] Memory doesn't explode
- [ ] Buffer limit enforced
- [ ] Truncation flagged correctly

### Scenario 6: Secret Environment Variable Protection

**Persona:** Developer with API keys

**Steps:**
1. Configure secret: `API_KEY=secret123`
2. Execute command that echoes all env vars
3. Check logs do NOT contain `secret123`
4. Check result.Stdout does NOT contain `secret123` (redacted)
5. Command still receives the secret

**Verification:**
- [ ] Secret not in logs
- [ ] Secret redacted in stored output
- [ ] Command receives actual secret value

---

## Testing Requirements

### Unit Tests (UT-029B-01 to UT-029B-25)

- [ ] UT-029B-01: ExecutionCommand validates command is not empty
- [ ] UT-029B-02: ExecutionCommand defaults timeout from config
- [ ] UT-029B-03: ExecutionResult calculates Duration correctly
- [ ] UT-029B-04: ExecutionResult Success is true when ExitCode == 0
- [ ] UT-029B-05: ExecutionResult serializes to JSON
- [ ] UT-029B-06: OutputBuffer respects size limit
- [ ] UT-029B-07: OutputBuffer truncates oldest on overflow
- [ ] UT-029B-08: OutputBuffer sets truncated flag
- [ ] UT-029B-09: ProcessRunner constructs correct shell command (Unix)
- [ ] UT-029B-10: ProcessRunner constructs correct shell command (Windows)
- [ ] UT-029B-11: ProcessRunner merges environment correctly
- [ ] UT-029B-12: Timeout triggers kill after duration
- [ ] UT-029B-13: Cancellation triggers kill immediately
- [ ] UT-029B-14: OutputHandler receives lines in order
- [ ] UT-029B-15: OutputHandler exception is caught
- [ ] UT-029B-16: MSBuild error pattern matched
- [ ] UT-029B-17: CS compiler error pattern matched
- [ ] UT-029B-18: Test failure pattern matched
- [ ] UT-029B-19: Invalid UTF-8 handled gracefully
- [ ] UT-029B-20: Empty output handled correctly
- [ ] UT-029B-21: Secret redaction works
- [ ] UT-029B-22: Events emitted for start/complete
- [ ] UT-029B-23: Metrics recorded correctly
- [ ] UT-029B-24: Concurrent execution isolation
- [ ] UT-029B-25: Resource cleanup on exception

### Integration Tests (IT-029B-01 to IT-029B-15)

- [ ] IT-029B-01: Execute `echo hello` returns hello
- [ ] IT-029B-02: Execute failing command captures exit code
- [ ] IT-029B-03: Execute with environment variable works
- [ ] IT-029B-04: Execute with working directory override
- [ ] IT-029B-05: Execute with timeout kills process
- [ ] IT-029B-06: Execute with cancellation kills process
- [ ] IT-029B-07: Streaming output works end-to-end
- [ ] IT-029B-08: Large output captured correctly
- [ ] IT-029B-09: Concurrent executions work
- [ ] IT-029B-10: Long-running command with cancellation
- [ ] IT-029B-11: Cross-platform execution
- [ ] IT-029B-12: Build command with error extraction
- [ ] IT-029B-13: Test command with failure extraction
- [ ] IT-029B-14: No zombie processes after 100 executions
- [ ] IT-029B-15: Memory stable after 100 executions

---

## Implementation Prompt

You are implementing command execution for compute targets. This handles running commands, capturing output, and enforcing timeouts. Follow Clean Architecture and TDD.

### Part 1: File Structure and Domain Models

#### File Structure

```
src/Acode.Domain/
├── Compute/
│   └── Execution/
│       ├── ExecutionCommand.cs
│       ├── ExecutionResult.cs
│       ├── OutputLine.cs
│       ├── OutputStream.cs
│       ├── ExecutionOptions.cs
│       └── Events/
│           ├── ExecutionStartedEvent.cs
│           ├── OutputReceivedEvent.cs
│           ├── ExecutionCompletedEvent.cs
│           └── ExecutionTimedOutEvent.cs

src/Acode.Application/
├── Compute/
│   └── Execution/
│       ├── IExecutionService.cs
│       ├── IOutputHandler.cs
│       ├── IOutputBuffer.cs
│       └── IProcessRunner.cs

src/Acode.Infrastructure/
├── Compute/
│   └── Execution/
│       ├── ExecutionService.cs
│       ├── ProcessRunner.cs
│       ├── OutputBuffer.cs
│       ├── OutputStreamHandler.cs
│       └── Shell/
│           ├── IShellProvider.cs
│           ├── BashShellProvider.cs
│           ├── CmdShellProvider.cs
│           └── PowerShellProvider.cs

tests/Acode.Domain.Tests/
├── Compute/
│   └── Execution/
│       ├── ExecutionCommandTests.cs
│       └── ExecutionResultTests.cs

tests/Acode.Infrastructure.Tests/
├── Compute/
│   └── Execution/
│       ├── ExecutionServiceTests.cs
│       ├── ProcessRunnerTests.cs
│       ├── OutputBufferTests.cs
│       └── Shell/
│           └── ShellProviderTests.cs
```

#### Domain Models

```csharp
// src/Acode.Domain/Compute/Execution/ExecutionCommand.cs
namespace Acode.Domain.Compute.Execution;

public sealed record ExecutionCommand
{
    public required string Command { get; init; }
    public IReadOnlyList<string>? Arguments { get; init; }
    public TimeSpan? Timeout { get; init; }
    public string? WorkingDirectory { get; init; }
    public IReadOnlyDictionary<string, string>? Environment { get; init; }
    public bool CaptureOutput { get; init; } = true;
    public bool StreamOutput { get; init; } = false;
    public int? OutputBufferSizeBytes { get; init; }
    public string? Shell { get; init; }
}

// src/Acode.Domain/Compute/Execution/ExecutionResult.cs
namespace Acode.Domain.Compute.Execution;

public sealed record ExecutionResult
{
    public required int ExitCode { get; init; }
    public string? StandardOutput { get; init; }
    public string? StandardError { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
    public required TimeSpan Duration { get; init; }
    public bool Truncated { get; init; }
    public bool TimedOut { get; init; }
    public bool Cancelled { get; init; }
    public string? Signal { get; init; }
    
    public bool Success => ExitCode == 0 && !TimedOut && !Cancelled;
    
    public static ExecutionResult FromTimeout(DateTimeOffset started) => new()
    {
        ExitCode = -1,
        StartedAt = started,
        CompletedAt = DateTimeOffset.UtcNow,
        Duration = DateTimeOffset.UtcNow - started,
        TimedOut = true
    };
}

// src/Acode.Domain/Compute/Execution/OutputLine.cs
namespace Acode.Domain.Compute.Execution;

public sealed record OutputLine(
    OutputStream Stream,
    string Text,
    DateTimeOffset Timestamp);

public enum OutputStream { Stdout, Stderr, Combined }

// src/Acode.Domain/Compute/Execution/Events/ExecutionStartedEvent.cs
namespace Acode.Domain.Compute.Execution.Events;

public sealed record ExecutionStartedEvent(
    ComputeTargetId TargetId,
    string Command,
    string? WorkingDirectory,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Execution/Events/ExecutionCompletedEvent.cs
namespace Acode.Domain.Compute.Execution.Events;

public sealed record ExecutionCompletedEvent(
    ComputeTargetId TargetId,
    int ExitCode,
    TimeSpan Duration,
    bool TimedOut,
    bool Cancelled,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 029.b Specification - Part 1/3**

### Part 2: Application Interfaces and Infrastructure Implementation

```csharp
// src/Acode.Application/Compute/Execution/IExecutionService.cs
namespace Acode.Application.Compute.Execution;

public interface IExecutionService
{
    Task<ExecutionResult> ExecuteAsync(
        IComputeTarget target,
        ExecutionCommand command,
        Action<OutputLine>? outputHandler = null,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Execution/IProcessRunner.cs
namespace Acode.Application.Compute.Execution;

public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(
        string command,
        IEnumerable<string> arguments,
        string? workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        TimeSpan? timeout,
        CancellationToken ct = default);
    
    Task<ProcessRunResult> RunWithStreamingAsync(
        string command,
        IEnumerable<string> arguments,
        string? workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        TimeSpan? timeout,
        Action<OutputLine> outputHandler,
        CancellationToken ct = default);
}

public sealed record ProcessRunResult(
    int ExitCode,
    string StdOut,
    string StdErr,
    bool TimedOut,
    TimeSpan Duration);

// src/Acode.Application/Compute/Execution/IOutputBuffer.cs
namespace Acode.Application.Compute.Execution;

public interface IOutputBuffer
{
    void Append(OutputStream stream, string text);
    void AppendLine(OutputStream stream, string line);
    string GetOutput(OutputStream stream);
    string GetCombinedOutput();
    bool IsTruncated { get; }
    int TotalBytes { get; }
    void Clear();
}

// src/Acode.Infrastructure/Compute/Execution/ExecutionService.cs
namespace Acode.Infrastructure.Compute.Execution;

public sealed class ExecutionService : IExecutionService
{
    private readonly IProcessRunner _processRunner;
    private readonly IShellProvider _shellProvider;
    private readonly IEventPublisher _events;
    private readonly ExecutionOptions _options;
    private readonly ILogger<ExecutionService> _logger;
    
    public ExecutionService(
        IProcessRunner processRunner,
        IShellProvider shellProvider,
        IEventPublisher events,
        IOptions<ExecutionOptions> options,
        ILogger<ExecutionService> logger)
    {
        _processRunner = processRunner;
        _shellProvider = shellProvider;
        _events = events;
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<ExecutionResult> ExecuteAsync(
        IComputeTarget target,
        ExecutionCommand command,
        Action<OutputLine>? outputHandler = null,
        CancellationToken ct = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        
        await _events.PublishAsync(new ExecutionStartedEvent(
            target.Id, command.Command, command.WorkingDirectory, startedAt));
        
        var (shell, args) = _shellProvider.GetShellCommand(
            command.Command, command.Shell);
        
        var env = BuildEnvironment(command.Environment);
        var timeout = command.Timeout ?? _options.DefaultTimeout;
        var workDir = command.WorkingDirectory ?? Environment.CurrentDirectory;
        
        ProcessRunResult result;
        
        if (command.StreamOutput && outputHandler != null)
        {
            result = await _processRunner.RunWithStreamingAsync(
                shell, args, workDir, env, timeout, outputHandler, ct);
        }
        else
        {
            result = await _processRunner.RunAsync(
                shell, args, workDir, env, timeout, ct);
        }
        
        var completedAt = DateTimeOffset.UtcNow;
        
        var execResult = new ExecutionResult
        {
            ExitCode = result.ExitCode,
            StandardOutput = result.StdOut,
            StandardError = result.StdErr,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            Duration = result.Duration,
            TimedOut = result.TimedOut,
            Cancelled = ct.IsCancellationRequested
        };
        
        await _events.PublishAsync(new ExecutionCompletedEvent(
            target.Id, result.ExitCode, result.Duration, 
            result.TimedOut, ct.IsCancellationRequested, completedAt));
        
        _logger.LogInformation(
            "Execution completed on {TargetId}: exit={ExitCode}, duration={Duration}",
            target.Id, result.ExitCode, result.Duration);
        
        return execResult;
    }
    
    private Dictionary<string, string> BuildEnvironment(
        IReadOnlyDictionary<string, string>? additional)
    {
        var env = new Dictionary<string, string>(_options.DefaultEnvironment);
        
        if (additional != null)
        {
            foreach (var (key, value) in additional)
                env[key] = value;
        }
        
        return env;
    }
}

// src/Acode.Infrastructure/Compute/Execution/ProcessRunner.cs
namespace Acode.Infrastructure.Compute.Execution;

public sealed class ProcessRunner : IProcessRunner
{
    private readonly ILogger<ProcessRunner> _logger;
    
    public ProcessRunner(ILogger<ProcessRunner> logger) => _logger = logger;
    
    public async Task<ProcessRunResult> RunAsync(
        string command,
        IEnumerable<string> arguments,
        string? workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        TimeSpan? timeout,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        using var process = CreateProcess(command, arguments, workingDirectory, environment);
        
        var outputBuffer = new StringBuilder();
        var errorBuffer = new StringBuilder();
        
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) outputBuffer.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) errorBuffer.AppendLine(e.Data);
        };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        var timedOut = false;
        
        if (timeout.HasValue)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout.Value);
            
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                timedOut = true;
                await TerminateProcessAsync(process);
            }
        }
        else
        {
            await process.WaitForExitAsync(ct);
        }
        
        stopwatch.Stop();
        
        return new ProcessRunResult(
            timedOut ? -1 : process.ExitCode,
            outputBuffer.ToString(),
            errorBuffer.ToString(),
            timedOut,
            stopwatch.Elapsed);
    }
    
    private static Process CreateProcess(
        string command,
        IEnumerable<string> arguments,
        string? workingDirectory,
        IReadOnlyDictionary<string, string>? environment)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            WorkingDirectory = workingDirectory ?? "",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);
        
        if (environment != null)
        {
            foreach (var (key, value) in environment)
                psi.Environment[key] = value;
        }
        
        return new Process { StartInfo = psi };
    }
    
    private async Task TerminateProcessAsync(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to terminate process");
        }
    }
}
```

**End of Task 029.b Specification - Part 2/3**

### Part 3: Output Buffer, Shell Providers, and Rollout

```csharp
// src/Acode.Infrastructure/Compute/Execution/OutputBuffer.cs
namespace Acode.Infrastructure.Compute.Execution;

public sealed class OutputBuffer : IOutputBuffer
{
    private readonly StringBuilder _stdout = new();
    private readonly StringBuilder _stderr = new();
    private readonly int _maxSizeBytes;
    private readonly object _lock = new();
    
    public OutputBuffer(int maxSizeBytes = 10 * 1024 * 1024)
    {
        _maxSizeBytes = maxSizeBytes;
    }
    
    public bool IsTruncated { get; private set; }
    public int TotalBytes => _stdout.Length + _stderr.Length;
    
    public void Append(OutputStream stream, string text)
    {
        lock (_lock)
        {
            var buffer = stream == OutputStream.Stdout ? _stdout : _stderr;
            
            if (TotalBytes + text.Length > _maxSizeBytes)
            {
                IsTruncated = true;
                var available = _maxSizeBytes - TotalBytes;
                if (available > 0)
                    buffer.Append(text.AsSpan(0, Math.Min(available, text.Length)));
            }
            else
            {
                buffer.Append(text);
            }
        }
    }
    
    public void AppendLine(OutputStream stream, string line)
    {
        Append(stream, line + Environment.NewLine);
    }
    
    public string GetOutput(OutputStream stream)
    {
        lock (_lock)
        {
            return stream switch
            {
                OutputStream.Stdout => _stdout.ToString(),
                OutputStream.Stderr => _stderr.ToString(),
                OutputStream.Combined => GetCombinedOutput(),
                _ => throw new ArgumentException($"Unknown stream: {stream}")
            };
        }
    }
    
    public string GetCombinedOutput()
    {
        lock (_lock)
        {
            return _stdout.ToString() + _stderr.ToString();
        }
    }
    
    public void Clear()
    {
        lock (_lock)
        {
            _stdout.Clear();
            _stderr.Clear();
            IsTruncated = false;
        }
    }
}

// src/Acode.Infrastructure/Compute/Execution/Shell/IShellProvider.cs
namespace Acode.Infrastructure.Compute.Execution.Shell;

public interface IShellProvider
{
    (string Shell, string[] Arguments) GetShellCommand(string command, string? preferredShell = null);
    bool IsAvailable(string shell);
    string DefaultShell { get; }
}

// src/Acode.Infrastructure/Compute/Execution/Shell/BashShellProvider.cs
namespace Acode.Infrastructure.Compute.Execution.Shell;

public sealed class BashShellProvider : IShellProvider
{
    public string DefaultShell => "/bin/bash";
    
    public (string Shell, string[] Arguments) GetShellCommand(string command, string? preferredShell = null)
    {
        var shell = preferredShell ?? DefaultShell;
        return (shell, ["-c", command]);
    }
    
    public bool IsAvailable(string shell)
    {
        return File.Exists(shell) || File.Exists($"/usr/bin/{shell}");
    }
}

// src/Acode.Infrastructure/Compute/Execution/Shell/CmdShellProvider.cs
namespace Acode.Infrastructure.Compute.Execution.Shell;

public sealed class CmdShellProvider : IShellProvider
{
    public string DefaultShell => "cmd.exe";
    
    public (string Shell, string[] Arguments) GetShellCommand(string command, string? preferredShell = null)
    {
        var shell = preferredShell ?? DefaultShell;
        return (shell, ["/c", command]);
    }
    
    public bool IsAvailable(string shell) => true; // Always available on Windows
}

// src/Acode.Infrastructure/Compute/Execution/Shell/PowerShellProvider.cs
namespace Acode.Infrastructure.Compute.Execution.Shell;

public sealed class PowerShellProvider : IShellProvider
{
    public string DefaultShell => "pwsh";
    
    public (string Shell, string[] Arguments) GetShellCommand(string command, string? preferredShell = null)
    {
        var shell = preferredShell ?? DefaultShell;
        return (shell, ["-NoProfile", "-NonInteractive", "-Command", command]);
    }
    
    public bool IsAvailable(string shell)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = shell,
                Arguments = "-Version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            return proc?.WaitForExit(1000) == true && proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

// src/Acode.Infrastructure/Compute/Execution/OutputStreamHandler.cs
namespace Acode.Infrastructure.Compute.Execution;

public sealed class OutputStreamHandler
{
    private readonly Action<OutputLine> _handler;
    private readonly IOutputBuffer _buffer;
    
    public OutputStreamHandler(Action<OutputLine> handler, IOutputBuffer buffer)
    {
        _handler = handler;
        _buffer = buffer;
    }
    
    public void HandleOutput(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null) return;
        
        var line = new OutputLine(OutputStream.Stdout, e.Data, DateTimeOffset.UtcNow);
        _buffer.AppendLine(OutputStream.Stdout, e.Data);
        _handler(line);
    }
    
    public void HandleError(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null) return;
        
        var line = new OutputLine(OutputStream.Stderr, e.Data, DateTimeOffset.UtcNow);
        _buffer.AppendLine(OutputStream.Stderr, e.Data);
        _handler(line);
    }
}

// src/Acode.Infrastructure/Compute/Execution/ExecutionOptions.cs
namespace Acode.Infrastructure.Compute.Execution;

public sealed class ExecutionOptions
{
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromHours(1);
    public int OutputBufferSizeBytes { get; set; } = 10 * 1024 * 1024;
    public int StreamChunkSizeBytes { get; set; } = 4096;
    public TimeSpan GracefulKillTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public Dictionary<string, string> DefaultEnvironment { get; set; } = new()
    {
        ["CI"] = "true",
        ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    };
}
```

---

## Implementation Checklist

- [ ] Create ExecutionCommand and ExecutionResult records
- [ ] Define OutputLine and OutputStream types
- [ ] Implement domain events for execution lifecycle
- [ ] Create IExecutionService interface
- [ ] Implement IProcessRunner with timeout handling
- [ ] Build OutputBuffer with size limits and truncation
- [ ] Create shell providers (Bash, Cmd, PowerShell)
- [ ] Implement streaming output handler
- [ ] Add graceful process termination (SIGTERM → SIGKILL)
- [ ] Write unit tests for all components (TDD)
- [ ] Write integration tests for real process execution
- [ ] Test timeout scenarios thoroughly
- [ ] Test cancellation handling
- [ ] Verify cross-platform behavior
- [ ] Test large output handling

---

## Rollout Plan

1. **Phase 1**: Domain models (command, result, events)
2. **Phase 2**: Application interfaces
3. **Phase 3**: ProcessRunner implementation
4. **Phase 4**: OutputBuffer with size management
5. **Phase 5**: Shell providers per platform
6. **Phase 6**: ExecutionService orchestrator
7. **Phase 7**: Integration testing

---

**End of Task 029.b Specification**