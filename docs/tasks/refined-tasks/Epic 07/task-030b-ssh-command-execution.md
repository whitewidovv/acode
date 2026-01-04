# Task 030.b: SSH Command Execution

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 030 (SSH Target), Task 030.a (Connection)  

---

## Description

Task 030.b implements SSH command execution. Commands MUST run on remote hosts. Output MUST stream back. Exit codes MUST be captured.

This extends Task 029.b (exec commands) for SSH specifics. PTY handling, shell escaping, and environment variables MUST work correctly.

Interactive commands MUST be supported. Long-running commands MUST stream output. Timeouts MUST be enforced.

### Business Value

Remote execution enables:
- Build on remote hardware
- Access specialized tools
- Utilize existing infrastructure
- Scale without cloud

### Scope Boundaries

This task covers command execution. Connection management is in 030.a. File transfer is in 030.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Task 029.b IExecuteCommands | Implements interface | Commands flow in, results flow out | Core abstraction |
| Task 030.a Connection Pool | ISshConnectionPool | Acquires connections for execution | Reuses connections |
| Task 027 Worker Orchestration | IWorker.Execute | Worker triggers command execution | Primary consumer |
| SSH.NET / Renci Library | SshClient, SshCommand | SSH protocol abstraction | Transport layer |
| PTY Subsystem | PseudoTerminalMode | Interactive sessions | Optional feature |
| Output Streaming | IAsyncEnumerable | Real-time output | Non-blocking |
| Process Control | SSH Channel | Signals, kill commands | Remote control |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Command timeout | Timer expiry | Kill process, return error | Delayed feedback |
| Connection lost mid-command | SSH disconnect event | Reconnect, optionally retry | Possible data loss |
| Non-zero exit code | ExitStatus check | Log and propagate error | Clear failure signal |
| Shell error (syntax) | stderr parsing | Report with context | Debugging needed |
| OOM on remote | Exit code 137 | Report OOM, no retry | Resource constraints |
| PTY allocation failure | Exception | Fallback to non-PTY | Degraded mode |
| Output buffer overflow | Size threshold | Switch to streaming | Memory protection |
| Signal delivery failure | Timeout on kill | Force close channel | Orphan possible |

---

## Assumptions

1. **SSH.NET Library**: Implementation uses SSH.NET (Renci) for SSH protocol handling
2. **Connection Pooling**: Task 030.a provides reliable connection acquisition
3. **Shell Availability**: Remote hosts have standard shell (bash, sh, or zsh)
4. **Encoding Consistency**: UTF-8 encoding used for all command I/O
5. **Signal Support**: Remote OS supports POSIX signals (SIGTERM, SIGKILL)
6. **Process Groups**: Shell supports process group control for child cleanup
7. **PATH Configuration**: Remote PATH includes standard locations (/usr/bin, /bin)
8. **Resource Limits**: Remote host has reasonable ulimits for processes

---

## Security Considerations

1. **Command Injection Prevention**: All user input MUST be shell-escaped using proper quoting
2. **Environment Sanitization**: Environment variables MUST NOT leak secrets to logs
3. **Output Filtering**: Sensitive patterns in output MUST be redacted in logs
4. **Timeout Enforcement**: All commands MUST have timeout to prevent resource exhaustion
5. **Privilege Escalation**: Sudo usage MUST be explicit and logged
6. **Credential Isolation**: SSH credentials MUST NOT appear in command strings
7. **Audit Trail**: All command executions MUST be logged with correlation ID
8. **Kill Authorization**: Only session owner MUST be able to kill commands

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| PTY | Pseudo-terminal |
| Shell | Command interpreter |
| Exit code | Command return value |
| Stream | Real-time output |
| Signal | Process control |
| Escape | Shell character handling |

---

## Out of Scope

- Interactive shell sessions
- Terminal emulation
- Color output processing
- Command history
- Tab completion

---

## Functional Requirements

### Command Execution Core

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030B-01 | `ExecuteAsync` MUST implement `IExecuteCommands` interface for SSH target | P0 |
| FR-030B-02 | Command MUST be passed as string with proper shell escaping | P0 |
| FR-030B-03 | Shell MUST be used for command interpretation (not direct exec) | P0 |
| FR-030B-04 | Default shell MUST be user's login shell from remote `/etc/passwd` | P1 |
| FR-030B-05 | Shell MUST be overridable via `ExecuteOptions.Shell` property | P1 |
| FR-030B-06 | Working directory MUST be settable and validated before execution | P0 |
| FR-030B-07 | Environment variables MUST be settable as `Dictionary<string, string>` | P0 |
| FR-030B-08 | Environment MUST merge with remote environment (local overrides) | P0 |
| FR-030B-09 | Exit code MUST be captured from SSH channel exit status | P0 |
| FR-030B-10 | Exit code MUST accurately reflect remote process exit | P0 |
| FR-030B-11 | Stdout MUST be captured as `string` or `IAsyncEnumerable<string>` | P0 |
| FR-030B-12 | Stderr MUST be captured separately as `string` or `IAsyncEnumerable<string>` | P0 |
| FR-030B-13 | Combined stdout+stderr MUST be optional via `CombineOutput` flag | P1 |
| FR-030B-14 | Real-time streaming MUST work via `IAsyncEnumerable<OutputLine>` | P0 |
| FR-030B-15 | Stream callback MUST be invocable with `Action<OutputLine>` | P1 |
| FR-030B-16 | Buffered output MUST be available via `ExecuteResult.Output` property | P0 |
| FR-030B-17 | Timeout MUST be enforced with automatic kill on expiry | P0 |
| FR-030B-18 | Default timeout MUST be 30 minutes (configurable) | P0 |
| FR-030B-19 | Timeout MUST be configurable per-execution via `ExecuteOptions.Timeout` | P0 |
| FR-030B-20 | Cancelled commands MUST be killed with SIGTERM followed by SIGKILL | P0 |

### PTY (Pseudo-Terminal) Handling

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030B-21 | PTY allocation MUST be optional via `ExecuteOptions.UsePty` flag | P0 |
| FR-030B-22 | Default MUST be no PTY (non-interactive mode) | P0 |
| FR-030B-23 | PTY MUST be requestable for interactive commands | P1 |
| FR-030B-24 | PTY MUST be auto-enabled for commands requiring terminal (vim, less) | P2 |
| FR-030B-25 | PTY dimensions MUST be settable via `PtyOptions.Columns` and `Rows` | P1 |
| FR-030B-26 | Default PTY dimensions MUST be 80 columns × 24 rows | P1 |
| FR-030B-27 | PTY MUST support runtime resize via `ResizePtyAsync` method | P2 |
| FR-030B-28 | PTY escape sequences MUST pass through unmodified | P1 |
| FR-030B-29 | SIGWINCH MUST be sent on resize | P2 |
| FR-030B-30 | PTY resources MUST be cleaned up on command completion | P0 |
| FR-030B-31 | Raw mode MUST be available for character-by-character I/O | P2 |
| FR-030B-32 | Cooked mode MUST be default (line-buffered) | P1 |
| FR-030B-33 | Line buffering MUST work for non-PTY output | P0 |
| FR-030B-34 | Character buffering MUST work for PTY output | P1 |
| FR-030B-35 | ANSI escape codes MUST pass through for color support | P1 |
| FR-030B-36 | ANSI stripping MUST be optional via `StripAnsi` flag | P2 |
| FR-030B-37 | Terminal type MUST be settable via `PtyOptions.TerminalType` | P2 |
| FR-030B-38 | Default terminal type MUST be `xterm-256color` | P2 |
| FR-030B-39 | PTY allocation failures MUST be handled gracefully | P0 |
| FR-030B-40 | Fallback to non-PTY MUST work when PTY fails | P1 |

### Process Control

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030B-41 | Running process MUST be killable via `KillAsync` method | P0 |
| FR-030B-42 | Kill MUST send SIGTERM first for graceful shutdown | P0 |
| FR-030B-43 | SIGTERM grace period MUST be 5 seconds (configurable) | P0 |
| FR-030B-44 | SIGKILL MUST follow SIGTERM if process doesn't exit | P0 |
| FR-030B-45 | Kill MUST work remotely via SSH signal mechanism | P0 |
| FR-030B-46 | Kill via SSH channel close MUST be fallback option | P1 |
| FR-030B-47 | Orphan processes MUST be handled via process group kill | P1 |
| FR-030B-48 | Entire process group MUST be killed (not just leader) | P0 |
| FR-030B-49 | Custom signals MUST be sendable via `SendSignalAsync(int signal)` | P2 |
| FR-030B-50 | SIGINT MUST be sendable for Ctrl+C behavior | P1 |
| FR-030B-51 | SIGHUP MUST be sendable for hangup simulation | P2 |
| FR-030B-52 | Background execution MUST work via `&` suffix | P1 |
| FR-030B-53 | Nohup MUST be optional for disconnect-safe execution | P2 |
| FR-030B-54 | Detached mode MUST allow immediate return without output | P2 |
| FR-030B-55 | PID MUST be retrievable via `ExecuteResult.ProcessId` | P1 |
| FR-030B-56 | Process status MUST be queryable via `IsRunningAsync(pid)` | P2 |
| FR-030B-57 | `/proc/{pid}` MUST be checked on Linux for status | P2 |
| FR-030B-58 | Wait for process MUST be available via `WaitAsync` | P1 |
| FR-030B-59 | Wait timeout MUST be supported | P1 |
| FR-030B-60 | Zombie process cleanup MUST happen automatically | P1 |

### Shell Handling and Escaping

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030B-61 | Bash shell MUST be fully supported | P0 |
| FR-030B-62 | Bourne sh MUST be supported as fallback | P0 |
| FR-030B-63 | Zsh MUST be supported | P1 |
| FR-030B-64 | Shell detection MUST work via remote `$SHELL` variable | P1 |
| FR-030B-65 | Shell path MUST be determined from `$SHELL` environment | P1 |
| FR-030B-66 | Fallback to `/bin/sh` MUST work when shell unknown | P0 |
| FR-030B-67 | Shell escaping MUST prevent injection attacks | P0 |
| FR-030B-68 | Single quotes MUST be properly escaped (`'` → `'\''`) | P0 |
| FR-030B-69 | Double quotes MUST be properly escaped (`"` → `\"`) | P0 |
| FR-030B-70 | Backticks MUST be escaped to prevent command substitution | P0 |
| FR-030B-71 | Dollar signs MUST be escaped to prevent variable expansion | P0 |
| FR-030B-72 | Newlines in commands MUST be handled via quoting | P0 |
| FR-030B-73 | Multi-line commands MUST work with proper escaping | P0 |
| FR-030B-74 | Here-doc syntax MUST work for multi-line input | P2 |
| FR-030B-75 | Script execution via `bash -c` MUST work | P0 |

### Output Handling

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030B-76 | Output encoding MUST default to UTF-8 | P0 |
| FR-030B-77 | Output encoding MUST be configurable | P2 |
| FR-030B-78 | Binary output MUST be supported via `byte[]` mode | P2 |
| FR-030B-79 | Output buffer MUST have configurable max size (default 10MB) | P1 |
| FR-030B-80 | Buffer overflow MUST switch to truncation with warning | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-030B-01 | Command execution overhead (SSH setup) | <200ms | P0 |
| NFR-030B-02 | Output stream latency from remote to local | <100ms | P0 |
| NFR-030B-03 | Concurrent command execution | 50 simultaneous | P0 |
| NFR-030B-04 | Output buffering capacity for small commands | 1MB per command | P1 |
| NFR-030B-05 | Large output streaming throughput | 10MB/s sustained | P1 |
| NFR-030B-06 | PTY allocation time | <50ms | P1 |
| NFR-030B-07 | Signal delivery latency | <100ms | P1 |
| NFR-030B-08 | Kill response time | <5s (SIGTERM + SIGKILL) | P0 |
| NFR-030B-09 | Connection reuse benefit | 80% latency reduction | P2 |
| NFR-030B-10 | Memory usage per active command | <5MB | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-030B-11 | No memory leaks on command completion | Zero leaks in 1000 iterations | P0 |
| NFR-030B-12 | Thread-safe concurrent execution | Zero race conditions | P0 |
| NFR-030B-13 | Exit code accuracy | 100% match with remote | P0 |
| NFR-030B-14 | Output completeness (no dropped lines) | 100% fidelity | P0 |
| NFR-030B-15 | Timeout enforcement accuracy | ±1 second | P0 |
| NFR-030B-16 | Recovery from connection loss | Automatic reconnect | P0 |
| NFR-030B-17 | Orphan process prevention | No orphans after cleanup | P0 |
| NFR-030B-18 | PTY fallback success rate | 100% graceful fallback | P1 |
| NFR-030B-19 | Shell escaping correctness | Zero injection vectors | P0 |
| NFR-030B-20 | Cancellation responsiveness | <5s to kill | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-030B-21 | Structured logging for all executions | JSON with correlation ID | P0 |
| NFR-030B-22 | Metrics for execution time | Histogram buckets | P1 |
| NFR-030B-23 | Metrics for exit code distribution | Counter per code | P1 |
| NFR-030B-24 | Metrics for timeout events | Counter | P1 |
| NFR-030B-25 | Metrics for kill events | Counter | P1 |
| NFR-030B-26 | Cross-platform client support | Windows, Linux, macOS | P0 |
| NFR-030B-27 | Distributed tracing via Activity | W3C trace context | P1 |
| NFR-030B-28 | Command sanitization in logs | Secrets redacted | P0 |
| NFR-030B-29 | Output size in metrics | Per-command bytes | P2 |
| NFR-030B-30 | Error categorization | Typed exception codes | P1 |

---

## User Manual Documentation

### Example Usage

```csharp
// Simple command
var result = await sshTarget.ExecuteAsync("echo hello");

// With options
var result = await sshTarget.ExecuteAsync(
    "make build",
    new ExecuteOptions
    {
        WorkingDirectory = "/app",
        Timeout = TimeSpan.FromMinutes(10),
        Environment = new() { ["CC"] = "clang" }
    });

// Streaming
await sshTarget.ExecuteAsync(
    "tail -f /var/log/app.log",
    output: line => Console.WriteLine(line),
    timeout: TimeSpan.FromHours(1));

// With PTY
await sshTarget.ExecuteAsync(
    "vim file.txt",
    new ExecuteOptions { UsePty = true });
```

### Exit Code Handling

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1-125 | Command failure |
| 126 | Permission denied |
| 127 | Command not found |
| 128+ | Signal received |

### Common Issues

| Issue | Resolution |
|-------|------------|
| Command not found | Check PATH |
| Permission denied | Check user/sudo |
| Timeout | Increase limit |
| No output | Check redirect |

---

## Acceptance Criteria / Definition of Done

### Command Execution Core
- [ ] AC-001: `ExecuteAsync` successfully runs simple command (`echo hello`)
- [ ] AC-002: `ExecuteAsync` returns correct exit code (0 for success)
- [ ] AC-003: `ExecuteAsync` returns correct non-zero exit code
- [ ] AC-004: Stdout is captured correctly
- [ ] AC-005: Stderr is captured separately
- [ ] AC-006: Combined output works with `CombineOutput` flag
- [ ] AC-007: Working directory is respected
- [ ] AC-008: Environment variables are set on remote
- [ ] AC-009: Local environment merges with remote
- [ ] AC-010: Local environment overrides remote values

### Streaming Output
- [ ] AC-011: `IAsyncEnumerable<OutputLine>` streams output in real-time
- [ ] AC-012: Stream callback is invoked for each line
- [ ] AC-013: Output type (stdout/stderr) is correctly labeled
- [ ] AC-014: Streaming works for long-running commands
- [ ] AC-015: Buffered output available after streaming completes
- [ ] AC-016: Large output (>1MB) streams without memory issues

### Timeout and Cancellation
- [ ] AC-017: Timeout is enforced (command killed after timeout)
- [ ] AC-018: Default timeout is 30 minutes
- [ ] AC-019: Timeout is configurable per-execution
- [ ] AC-020: `CancellationToken` triggers command kill
- [ ] AC-021: SIGTERM is sent first on cancellation
- [ ] AC-022: SIGKILL follows SIGTERM after grace period
- [ ] AC-023: Cancelled command returns appropriate error

### PTY Handling
- [ ] AC-024: PTY is NOT allocated by default
- [ ] AC-025: PTY is allocated when `UsePty=true`
- [ ] AC-026: PTY dimensions default to 80×24
- [ ] AC-027: PTY dimensions are configurable
- [ ] AC-028: PTY resize works at runtime
- [ ] AC-029: ANSI escape codes pass through
- [ ] AC-030: ANSI stripping works when enabled
- [ ] AC-031: Terminal type defaults to xterm-256color
- [ ] AC-032: PTY failure falls back to non-PTY
- [ ] AC-033: PTY resources are cleaned up

### Process Control
- [ ] AC-034: `KillAsync` terminates running command
- [ ] AC-035: Kill sends SIGTERM first
- [ ] AC-036: Kill sends SIGKILL after 5s grace
- [ ] AC-037: Custom signals can be sent
- [ ] AC-038: SIGINT works (Ctrl+C simulation)
- [ ] AC-039: Process group is killed (not just leader)
- [ ] AC-040: Background execution works with `&`
- [ ] AC-041: Nohup prevents hangup issues
- [ ] AC-042: PID is retrievable from result
- [ ] AC-043: Process status is queryable

### Shell Handling
- [ ] AC-044: Bash shell is fully supported
- [ ] AC-045: Bourne sh works as fallback
- [ ] AC-046: Zsh shell works
- [ ] AC-047: Shell is detected from `$SHELL`
- [ ] AC-048: Fallback to `/bin/sh` works
- [ ] AC-049: Shell escaping prevents injection
- [ ] AC-050: Single quotes are escaped correctly
- [ ] AC-051: Double quotes are escaped correctly
- [ ] AC-052: Backticks are escaped (no command substitution)
- [ ] AC-053: Dollar signs are escaped (no variable expansion)
- [ ] AC-054: Multi-line commands work
- [ ] AC-055: Script execution via `bash -c` works

### Error Handling
- [ ] AC-056: Command not found returns exit code 127
- [ ] AC-057: Permission denied returns exit code 126
- [ ] AC-058: Connection loss triggers reconnection attempt
- [ ] AC-059: Timeout exception is typed and descriptive
- [ ] AC-060: Shell syntax errors are reported clearly

### Reliability and Performance
- [ ] AC-061: No memory leaks in 1000-iteration test
- [ ] AC-062: Thread-safe under concurrent execution
- [ ] AC-063: 50 concurrent commands complete successfully
- [ ] AC-064: Execution overhead is <200ms
- [ ] AC-065: Output stream latency is <100ms

---

## User Verification Scenarios

### Scenario 1: Developer Runs Build Command
**Persona:** Developer executing remote build  
**Preconditions:** SSH target connected, workspace prepared  
**Steps:**
1. Execute `make build` with 10-minute timeout
2. Observe streaming output
3. Check exit code on completion
4. Verify artifacts exist

**Verification Checklist:**
- [ ] Command starts within 500ms
- [ ] Output streams in real-time
- [ ] Exit code 0 on success
- [ ] Non-zero exit code on failure
- [ ] Timeout kills command if exceeded

### Scenario 2: Running Interactive Command
**Persona:** Developer needing interactive session  
**Preconditions:** PTY support enabled  
**Steps:**
1. Execute `vim config.yml` with PTY
2. Verify terminal renders correctly
3. Send keystrokes
4. Exit and verify file saved

**Verification Checklist:**
- [ ] PTY allocated successfully
- [ ] Cursor movement works
- [ ] ANSI colors display
- [ ] Exit code 0 on `:wq`

### Scenario 3: Cancelling Long-Running Command
**Persona:** Developer aborting stuck build  
**Preconditions:** Command running for 5+ minutes  
**Steps:**
1. Start long-running command
2. Trigger cancellation after 30 seconds
3. Verify command killed
4. Check no orphan processes

**Verification Checklist:**
- [ ] Cancellation acknowledged
- [ ] SIGTERM sent first
- [ ] SIGKILL follows if needed
- [ ] Command returns cancelled status
- [ ] No orphan processes on remote

### Scenario 4: Command with Environment Variables
**Persona:** CI system setting build env  
**Preconditions:** Build requires specific env vars  
**Steps:**
1. Set CC=clang, BUILD_TYPE=release
2. Execute `make build`
3. Verify env vars used

**Verification Checklist:**
- [ ] Environment variables set on remote
- [ ] Variables available to command
- [ ] Local vars override remote
- [ ] Variables don't leak to logs

### Scenario 5: Handling Command Failure
**Persona:** Developer debugging failed command  
**Preconditions:** Command expected to fail  
**Steps:**
1. Execute command with typo
2. Observe error output
3. Check exit code
4. View stderr content

**Verification Checklist:**
- [ ] Exit code 127 for not found
- [ ] Exit code 126 for permission denied
- [ ] Stderr captured separately
- [ ] Error message is descriptive

### Scenario 6: Concurrent Command Execution
**Persona:** CI system running parallel tests  
**Preconditions:** Multiple workers active  
**Steps:**
1. Start 20 commands simultaneously
2. Each command runs for 30 seconds
3. Verify all complete
4. Check no resource leaks

**Verification Checklist:**
- [ ] All 20 commands start
- [ ] No deadlocks occur
- [ ] All complete successfully
- [ ] Memory stable throughout
- [ ] Connections reused

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-030B-01 | Command building with simple string | FR-030B-02 |
| UT-030B-02 | Shell escaping for single quotes | FR-030B-68 |
| UT-030B-03 | Shell escaping for double quotes | FR-030B-69 |
| UT-030B-04 | Shell escaping for backticks | FR-030B-70 |
| UT-030B-05 | Shell escaping for dollar signs | FR-030B-71 |
| UT-030B-06 | Exit code parsing from result | FR-030B-09 |
| UT-030B-07 | Timeout logic triggers kill | FR-030B-17 |
| UT-030B-08 | Environment variable merging | FR-030B-08 |
| UT-030B-09 | Working directory injection | FR-030B-06 |
| UT-030B-10 | PTY options configuration | FR-030B-25 |
| UT-030B-11 | Output stream splitting | FR-030B-12 |
| UT-030B-12 | Combined output mode | FR-030B-13 |
| UT-030B-13 | SIGTERM grace period logic | FR-030B-43 |
| UT-030B-14 | SIGKILL fallback | FR-030B-44 |
| UT-030B-15 | Shell detection logic | FR-030B-64 |
| UT-030B-16 | ANSI stripping | FR-030B-36 |
| UT-030B-17 | Output buffer size limits | FR-030B-79 |
| UT-030B-18 | Multi-line command handling | FR-030B-73 |
| UT-030B-19 | Process group kill command | FR-030B-48 |
| UT-030B-20 | Nohup command wrapping | FR-030B-53 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-030B-01 | Real SSH command execution | E2E basic |
| IT-030B-02 | Streaming output real-time | FR-030B-14 |
| IT-030B-03 | PTY interactive command | FR-030B-23 |
| IT-030B-04 | Process kill and cleanup | FR-030B-41 |
| IT-030B-05 | 50 concurrent commands | NFR-030B-03 |
| IT-030B-06 | Large output streaming | NFR-030B-05 |
| IT-030B-07 | Timeout enforcement | FR-030B-17 |
| IT-030B-08 | Environment variable passing | FR-030B-07 |
| IT-030B-09 | Working directory switching | FR-030B-06 |
| IT-030B-10 | Shell fallback to /bin/sh | FR-030B-66 |
| IT-030B-11 | Connection reuse benefit | NFR-030B-09 |
| IT-030B-12 | Cancellation responsiveness | NFR-030B-20 |
| IT-030B-13 | No orphan processes | NFR-030B-17 |
| IT-030B-14 | Exit code 127/126 mapping | FR-030B-09 |
| IT-030B-15 | Memory stability under load | NFR-030B-11 |

---

## Implementation Prompt

### Part 1: File Structure and Domain Models

**Target Directory Structure:**
```
src/
├── Acode.Domain/
│   └── Compute/
│       └── Ssh/
│           └── Execution/
│               ├── SshCommandResult.cs
│               ├── PtyOptions.cs
│               ├── ShellType.cs
│               └── Events/
│                   ├── CommandStartedEvent.cs
│                   ├── CommandOutputEvent.cs
│                   ├── CommandCompletedEvent.cs
│                   └── CommandKilledEvent.cs
├── Acode.Application/
│   └── Compute/
│       └── Ssh/
│           └── Execution/
│               ├── SshExecuteOptions.cs
│               ├── ISshCommandExecutor.cs
│               ├── IShellEscaper.cs
│               ├── IPtyHandler.cs
│               └── IProcessSignaler.cs
└── Acode.Infrastructure/
    └── Compute/
        └── Ssh/
            └── Execution/
                ├── SshCommandExecutor.cs
                ├── ShellEscaper.cs
                ├── PtyHandler.cs
                ├── ProcessSignaler.cs
                ├── StreamingOutputHandler.cs
                └── ShellProviders/
                    ├── BashShellProvider.cs
                    ├── ShShellProvider.cs
                    └── ZshShellProvider.cs
```

**Domain Models:**

```csharp
// src/Acode.Domain/Compute/Ssh/Execution/SshCommandResult.cs
namespace Acode.Domain.Compute.Ssh.Execution;

public sealed record SshCommandResult
{
    public int ExitCode { get; init; }
    public string Stdout { get; init; } = string.Empty;
    public string Stderr { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public bool TimedOut { get; init; }
    public bool Killed { get; init; }
    public string? KillReason { get; init; }
    public int? ProcessId { get; init; }
    
    public bool IsSuccess => ExitCode == 0 && !TimedOut && !Killed;
}

// src/Acode.Domain/Compute/Ssh/Execution/PtyOptions.cs
namespace Acode.Domain.Compute.Ssh.Execution;

public sealed record PtyOptions
{
    public int Columns { get; init; } = 80;
    public int Rows { get; init; } = 24;
    public string TerminalType { get; init; } = "xterm-256color";
    public bool StripAnsi { get; init; } = false;
    public bool RawMode { get; init; } = false;
    
    public static PtyOptions Default => new();
}

// src/Acode.Domain/Compute/Ssh/Execution/ShellType.cs
namespace Acode.Domain.Compute.Ssh.Execution;

public enum ShellType
{
    Auto,   // Detect from $SHELL
    Bash,
    Sh,
    Zsh
}

// src/Acode.Domain/Compute/Ssh/Execution/Events/CommandStartedEvent.cs
namespace Acode.Domain.Compute.Ssh.Execution.Events;

public sealed record CommandStartedEvent(
    string CommandId,
    string ConnectionId,
    string Command,
    DateTimeOffset StartedAt);

// src/Acode.Domain/Compute/Ssh/Execution/Events/CommandOutputEvent.cs
namespace Acode.Domain.Compute.Ssh.Execution.Events;

public sealed record CommandOutputEvent(
    string CommandId,
    OutputType Type,
    string Line,
    DateTimeOffset Timestamp);

public enum OutputType { Stdout, Stderr }

// src/Acode.Domain/Compute/Ssh/Execution/Events/CommandCompletedEvent.cs
namespace Acode.Domain.Compute.Ssh.Execution.Events;

public sealed record CommandCompletedEvent(
    string CommandId,
    int ExitCode,
    TimeSpan Duration,
    long BytesOutput,
    DateTimeOffset CompletedAt);

// src/Acode.Domain/Compute/Ssh/Execution/Events/CommandKilledEvent.cs
namespace Acode.Domain.Compute.Ssh.Execution.Events;

public sealed record CommandKilledEvent(
    string CommandId,
    string Reason,
    int SignalSent,
    DateTimeOffset KilledAt);
```

**End of Task 030.b Specification - Part 1/3**

### Part 2: Application Interfaces and Shell Escaping

```csharp
// src/Acode.Application/Compute/Ssh/Execution/SshExecuteOptions.cs
namespace Acode.Application.Compute.Ssh.Execution;

public sealed record SshExecuteOptions
{
    public string? WorkingDirectory { get; init; }
    public IReadOnlyDictionary<string, string>? Environment { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(30);
    public bool UsePty { get; init; } = false;
    public PtyOptions? PtyOptions { get; init; }
    public ShellType Shell { get; init; } = ShellType.Auto;
    public bool CombineOutput { get; init; } = false;
    public Action<OutputType, string>? OutputCallback { get; init; }
    public bool ThrowOnNonZero { get; init; } = false;
    
    public static SshExecuteOptions Default => new();
}

// src/Acode.Application/Compute/Ssh/Execution/ISshCommandExecutor.cs
namespace Acode.Application.Compute.Ssh.Execution;

public interface ISshCommandExecutor
{
    Task<SshCommandResult> ExecuteAsync(
        ISshConnection connection,
        string command,
        SshExecuteOptions? options = null,
        CancellationToken ct = default);
    
    Task<SshCommandResult> ExecuteWithStreamingAsync(
        ISshConnection connection,
        string command,
        Action<OutputType, string> outputHandler,
        SshExecuteOptions? options = null,
        CancellationToken ct = default);
    
    Task KillAsync(string commandId, CancellationToken ct = default);
    Task SendSignalAsync(string commandId, int signal, CancellationToken ct = default);
}

// src/Acode.Application/Compute/Ssh/Execution/IShellEscaper.cs
namespace Acode.Application.Compute.Ssh.Execution;

public interface IShellEscaper
{
    string Escape(string value);
    string Quote(string value);
    string EscapeForEnvironment(string name, string value);
    
    string BuildCommand(
        string command,
        string? workingDir,
        IReadOnlyDictionary<string, string>? environment);
}

// src/Acode.Application/Compute/Ssh/Execution/IPtyHandler.cs
namespace Acode.Application.Compute.Ssh.Execution;

public interface IPtyHandler
{
    Task<ISshChannelPty> CreatePtyAsync(
        ISshConnection connection,
        PtyOptions options,
        CancellationToken ct = default);
    
    Task ResizeAsync(ISshChannelPty pty, int columns, int rows, CancellationToken ct = default);
    string StripAnsiCodes(string input);
}

public interface ISshChannelPty : IAsyncDisposable
{
    Stream InputStream { get; }
    Stream OutputStream { get; }
    int Columns { get; }
    int Rows { get; }
    
    Task WriteAsync(string data, CancellationToken ct = default);
    Task<string> ReadAsync(CancellationToken ct = default);
    Task SendSignalAsync(int signal, CancellationToken ct = default);
}

// src/Acode.Application/Compute/Ssh/Execution/IProcessSignaler.cs
namespace Acode.Application.Compute.Ssh.Execution;

public interface IProcessSignaler
{
    Task<bool> SendSignalAsync(
        ISshConnection connection,
        int processId,
        int signal,
        CancellationToken ct = default);
    
    Task<bool> KillProcessAsync(
        ISshConnection connection,
        int processId,
        bool force = false,
        CancellationToken ct = default);
    
    Task<bool> KillProcessGroupAsync(
        ISshConnection connection,
        int processGroupId,
        bool force = false,
        CancellationToken ct = default);
    
    Task<bool> IsProcessRunningAsync(
        ISshConnection connection,
        int processId,
        CancellationToken ct = default);
}

// src/Acode.Infrastructure/Compute/Ssh/Execution/ShellEscaper.cs
namespace Acode.Infrastructure.Compute.Ssh.Execution;

public sealed class ShellEscaper : IShellEscaper
{
    private readonly ShellType _shellType;
    
    public ShellEscaper(ShellType shellType = ShellType.Bash)
    {
        _shellType = shellType;
    }
    
    public string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "''";
        
        // Check if needs escaping
        if (!NeedsEscaping(value))
            return value;
        
        // Single-quote escaping is safest for all shells
        return "'" + value.Replace("'", "'\"'\"'") + "'";
    }
    
    public string Quote(string value) => $"\"{EscapeDoubleQuotes(value)}\"";
    
    public string EscapeForEnvironment(string name, string value) =>
        $"export {name}={Escape(value)}";
    
    public string BuildCommand(
        string command,
        string? workingDir,
        IReadOnlyDictionary<string, string>? environment)
    {
        var parts = new List<string>();
        
        // Add environment exports
        if (environment != null)
        {
            foreach (var (name, value) in environment)
            {
                parts.Add(EscapeForEnvironment(name, value));
            }
        }
        
        // Add working directory change
        if (!string.IsNullOrEmpty(workingDir))
        {
            parts.Add($"cd {Escape(workingDir)}");
        }
        
        // Add the actual command
        parts.Add(command);
        
        return string.Join(" && ", parts);
    }
    
    private static bool NeedsEscaping(string value) =>
        value.Any(c => char.IsWhiteSpace(c) || 
                       c == '\'' || c == '"' || c == '\\' || 
                       c == '$' || c == '`' || c == '!' ||
                       c == '*' || c == '?' || c == '[' ||
                       c == ']' || c == '(' || c == ')' ||
                       c == '{' || c == '}' || c == '|' ||
                       c == '&' || c == ';' || c == '<' ||
                       c == '>' || c == '~' || c == '#');
    
    private static string EscapeDoubleQuotes(string value) =>
        value.Replace("\\", "\\\\")
             .Replace("\"", "\\\"")
             .Replace("$", "\\$")
             .Replace("`", "\\`")
             .Replace("!", "\\!");
}
```

**End of Task 030.b Specification - Part 2/3**

### Part 3: Infrastructure Implementation and Checklist

```csharp
// src/Acode.Infrastructure/Compute/Ssh/Execution/SshCommandExecutor.cs
namespace Acode.Infrastructure.Compute.Ssh.Execution;

public sealed class SshCommandExecutor : ISshCommandExecutor
{
    private readonly IShellEscaper _escaper;
    private readonly IPtyHandler _ptyHandler;
    private readonly IProcessSignaler _signaler;
    private readonly IEventPublisher _events;
    private readonly ILogger<SshCommandExecutor> _logger;
    private readonly ConcurrentDictionary<string, RunningCommand> _running = new();
    
    public async Task<SshCommandResult> ExecuteAsync(
        ISshConnection connection,
        string command,
        SshExecuteOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= SshExecuteOptions.Default;
        var commandId = Ulid.NewUlid().ToString();
        var stopwatch = Stopwatch.StartNew();
        
        var fullCommand = _escaper.BuildCommand(
            command, options.WorkingDirectory, options.Environment);
        
        await _events.PublishAsync(new CommandStartedEvent(
            commandId, connection.ConnectionId, command, DateTimeOffset.UtcNow));
        
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(options.Timeout);
            
            SshCommandResult result;
            
            if (options.UsePty)
            {
                result = await ExecuteWithPtyAsync(
                    connection, fullCommand, commandId, options, cts.Token);
            }
            else
            {
                result = await ExecuteStandardAsync(
                    connection, fullCommand, commandId, options, cts.Token);
            }
            
            stopwatch.Stop();
            
            await _events.PublishAsync(new CommandCompletedEvent(
                commandId, result.ExitCode, stopwatch.Elapsed,
                result.Stdout.Length + result.Stderr.Length, DateTimeOffset.UtcNow));
            
            if (options.ThrowOnNonZero && result.ExitCode != 0)
            {
                throw new SshCommandException(result);
            }
            
            return result with { Duration = stopwatch.Elapsed };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await KillAsync(commandId, CancellationToken.None);
            throw;
        }
        catch (OperationCanceledException)
        {
            // Timeout
            await KillAsync(commandId, CancellationToken.None);
            return new SshCommandResult { TimedOut = true, Duration = stopwatch.Elapsed };
        }
    }
    
    private async Task<SshCommandResult> ExecuteStandardAsync(
        ISshConnection connection,
        string command,
        string commandId,
        SshExecuteOptions options,
        CancellationToken ct)
    {
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();
        
        var sshResult = await connection.ExecuteAsync(command, options.Timeout, ct);
        
        if (options.OutputCallback != null)
        {
            foreach (var line in sshResult.Output.Split('\n'))
            {
                options.OutputCallback(OutputType.Stdout, line);
            }
        }
        
        return new SshCommandResult
        {
            ExitCode = sshResult.ExitCode,
            Stdout = sshResult.Output,
            Stderr = sshResult.Error ?? string.Empty,
            ProcessId = sshResult.ProcessId
        };
    }
    
    public async Task KillAsync(string commandId, CancellationToken ct = default)
    {
        if (!_running.TryRemove(commandId, out var running))
            return;
        
        // SIGTERM first
        await _signaler.KillProcessAsync(
            running.Connection, running.ProcessId, force: false, ct);
        
        // Wait grace period
        await Task.Delay(TimeSpan.FromSeconds(5), ct);
        
        // Check if still running, then SIGKILL
        if (await _signaler.IsProcessRunningAsync(running.Connection, running.ProcessId, ct))
        {
            await _signaler.KillProcessGroupAsync(
                running.Connection, running.ProcessId, force: true, ct);
        }
        
        await _events.PublishAsync(new CommandKilledEvent(
            commandId, "Cancelled", 15, DateTimeOffset.UtcNow));
    }
    
    public async Task SendSignalAsync(string commandId, int signal, CancellationToken ct = default)
    {
        if (!_running.TryGetValue(commandId, out var running))
            return;
        
        await _signaler.SendSignalAsync(running.Connection, running.ProcessId, signal, ct);
    }
    
    private record RunningCommand(
        ISshConnection Connection,
        int ProcessId,
        CancellationTokenSource Cts);
}

// src/Acode.Infrastructure/Compute/Ssh/Execution/ProcessSignaler.cs
namespace Acode.Infrastructure.Compute.Ssh.Execution;

public sealed class ProcessSignaler : IProcessSignaler
{
    public async Task<bool> SendSignalAsync(
        ISshConnection connection,
        int processId,
        int signal,
        CancellationToken ct = default)
    {
        var result = await connection.ExecuteAsync($"kill -{signal} {processId}", ct: ct);
        return result.ExitCode == 0;
    }
    
    public async Task<bool> KillProcessAsync(
        ISshConnection connection,
        int processId,
        bool force = false,
        CancellationToken ct = default)
    {
        var signal = force ? 9 : 15; // SIGKILL or SIGTERM
        return await SendSignalAsync(connection, processId, signal, ct);
    }
    
    public async Task<bool> KillProcessGroupAsync(
        ISshConnection connection,
        int processGroupId,
        bool force = false,
        CancellationToken ct = default)
    {
        var signal = force ? 9 : 15;
        var result = await connection.ExecuteAsync($"kill -{signal} -{processGroupId}", ct: ct);
        return result.ExitCode == 0;
    }
    
    public async Task<bool> IsProcessRunningAsync(
        ISshConnection connection,
        int processId,
        CancellationToken ct = default)
    {
        var result = await connection.ExecuteAsync($"kill -0 {processId}", ct: ct);
        return result.ExitCode == 0;
    }
}

// src/Acode.Infrastructure/Compute/Ssh/Execution/StreamingOutputHandler.cs
namespace Acode.Infrastructure.Compute.Ssh.Execution;

public sealed class StreamingOutputHandler
{
    private readonly Action<OutputType, string> _callback;
    private readonly StringBuilder _stdoutBuffer = new();
    private readonly StringBuilder _stderrBuffer = new();
    
    public StreamingOutputHandler(Action<OutputType, string> callback)
    {
        _callback = callback;
    }
    
    public void AppendStdout(string data)
    {
        _stdoutBuffer.Append(data);
        FlushLines(_stdoutBuffer, OutputType.Stdout);
    }
    
    public void AppendStderr(string data)
    {
        _stderrBuffer.Append(data);
        FlushLines(_stderrBuffer, OutputType.Stderr);
    }
    
    private void FlushLines(StringBuilder buffer, OutputType type)
    {
        var content = buffer.ToString();
        var lastNewline = content.LastIndexOf('\n');
        
        if (lastNewline >= 0)
        {
            var lines = content[..lastNewline].Split('\n');
            foreach (var line in lines)
            {
                _callback(type, line);
            }
            buffer.Clear();
            buffer.Append(content[(lastNewline + 1)..]);
        }
    }
    
    public void Flush()
    {
        if (_stdoutBuffer.Length > 0)
            _callback(OutputType.Stdout, _stdoutBuffer.ToString());
        if (_stderrBuffer.Length > 0)
            _callback(OutputType.Stderr, _stderrBuffer.ToString());
    }
}
```

### Implementation Checklist

| # | Requirement | Test | Impl |
|---|-------------|------|------|
| 1 | ExecuteAsync runs command over SSH | ⬜ | ⬜ |
| 2 | Working directory changes with cd | ⬜ | ⬜ |
| 3 | Environment variables exported | ⬜ | ⬜ |
| 4 | Exit code captured accurately | ⬜ | ⬜ |
| 5 | Stdout captured | ⬜ | ⬜ |
| 6 | Stderr captured | ⬜ | ⬜ |
| 7 | Streaming output via callback | ⬜ | ⬜ |
| 8 | Timeout enforced (default 30 min) | ⬜ | ⬜ |
| 9 | Cancelled commands killed | ⬜ | ⬜ |
| 10 | PTY mode works | ⬜ | ⬜ |
| 11 | PTY dimensions configurable | ⬜ | ⬜ |
| 12 | ANSI stripping optional | ⬜ | ⬜ |
| 13 | Kill uses SIGTERM then SIGKILL | ⬜ | ⬜ |
| 14 | Process group killed | ⬜ | ⬜ |
| 15 | Custom signals sendable | ⬜ | ⬜ |
| 16 | Shell escaping works for all chars | ⬜ | ⬜ |
| 17 | Multi-line commands work | ⬜ | ⬜ |
| 18 | CommandStartedEvent published | ⬜ | ⬜ |
| 19 | CommandCompletedEvent published | ⬜ | ⬜ |
| 20 | CommandKilledEvent published | ⬜ | ⬜ |

### Rollout Plan

1. **Tests first**: Unit tests for ShellEscaper, ProcessSignaler, StreamingOutputHandler
2. **Domain models**: Events and result types
3. **Application interfaces**: ISshCommandExecutor, IShellEscaper, IPtyHandler, IProcessSignaler
4. **Infrastructure impl**: SshCommandExecutor, ShellEscaper, ProcessSignaler
5. **PTY support**: PtyHandler implementation
6. **Integration tests**: Real SSH command execution, streaming, timeout behavior
7. **DI registration**: Register executor as scoped, escaper as singleton per shell type

**End of Task 030.b Specification**