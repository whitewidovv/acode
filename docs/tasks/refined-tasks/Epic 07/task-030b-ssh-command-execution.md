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

- Task 029.b: Implements exec interface
- Task 030.a: Uses connection pool
- Task 027: Workers execute commands

### Failure Modes

- Command timeout → Kill and error
- Connection lost → Reconnect and retry
- Exit code non-zero → Report failure
- Shell error → Parse and report

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

### FR-001 to FR-020: Command Execution

- FR-001: `ExecuteAsync` MUST work over SSH
- FR-002: Command MUST be string
- FR-003: Shell MUST be used
- FR-004: Default shell: user's default
- FR-005: Shell MUST be overridable
- FR-006: Working directory MUST be settable
- FR-007: Environment MUST be settable
- FR-008: Environment MUST merge with remote
- FR-009: Exit code MUST be captured
- FR-010: Exit code MUST be accurate
- FR-011: Stdout MUST be captured
- FR-012: Stderr MUST be captured
- FR-013: Combined output MUST be optional
- FR-014: Streaming MUST work
- FR-015: Stream callback MUST be invocable
- FR-016: Buffered output MUST be available
- FR-017: Timeout MUST be enforced
- FR-018: Default timeout: 30 minutes
- FR-019: Timeout MUST be configurable
- FR-020: Cancelled commands MUST be killed

### FR-021 to FR-040: PTY Handling

- FR-021: PTY MUST be optional
- FR-022: Default: no PTY
- FR-023: PTY MUST be requestable
- FR-024: PTY needed for some commands
- FR-025: PTY dimensions MUST be settable
- FR-026: Default: 80x24
- FR-027: PTY MUST support resize
- FR-028: PTY escape sequences MUST work
- FR-029: SIGWINCH MUST be sent
- FR-030: PTY MUST be cleaned up
- FR-031: Raw mode MUST be available
- FR-032: Cooked mode MUST be default
- FR-033: Line buffering MUST work
- FR-034: Character buffering for PTY
- FR-035: ANSI codes MUST pass through
- FR-036: ANSI stripping MUST be optional
- FR-037: Terminal type MUST be settable
- FR-038: Default: xterm-256color
- FR-039: PTY errors MUST be handled
- FR-040: Fallback to non-PTY MUST work

### FR-041 to FR-060: Process Control

- FR-041: Process MUST be killable
- FR-042: Kill MUST use SIGTERM first
- FR-043: SIGTERM grace: 5 seconds
- FR-044: SIGKILL MUST follow
- FR-045: Kill MUST work remotely
- FR-046: Kill via SSH channel close
- FR-047: Orphan processes MUST be handled
- FR-048: Process group MUST be killed
- FR-049: Signals MUST be sendable
- FR-050: SIGINT MUST work
- FR-051: SIGHUP MUST work
- FR-052: Custom signals MUST work
- FR-053: Background execution MUST work
- FR-054: Nohup MUST be optional
- FR-055: Detached MUST work
- FR-056: PID MUST be retrievable
- FR-057: Process status MUST be queryable
- FR-058: /proc MUST be checked (Linux)
- FR-059: Wait MUST be available
- FR-060: Wait timeout MUST work

### FR-061 to FR-075: Shell Handling

- FR-061: Bash MUST be supported
- FR-062: Sh MUST be supported
- FR-063: Zsh MUST be supported
- FR-064: Shell detection MUST work
- FR-065: Shell via $SHELL
- FR-066: Fallback to /bin/sh
- FR-067: Shell escaping MUST work
- FR-068: Single quotes MUST be escaped
- FR-069: Double quotes MUST be escaped
- FR-070: Backticks MUST be escaped
- FR-071: Dollar signs MUST be escaped
- FR-072: Newlines MUST be handled
- FR-073: Multi-line commands MUST work
- FR-074: Here-doc MUST work
- FR-075: Script execution MUST work

---

## Non-Functional Requirements

- NFR-001: Exec latency <200ms overhead
- NFR-002: Stream latency <100ms
- NFR-003: 50 concurrent commands
- NFR-004: 1MB output buffered
- NFR-005: Larger output streamed
- NFR-006: No memory leaks
- NFR-007: Thread-safe
- NFR-008: Structured logging
- NFR-009: Metrics on execution
- NFR-010: Cross-platform client

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

- [ ] AC-001: Simple commands work
- [ ] AC-002: Exit codes captured
- [ ] AC-003: Stdout captured
- [ ] AC-004: Stderr captured
- [ ] AC-005: Streaming works
- [ ] AC-006: Timeout works
- [ ] AC-007: PTY works
- [ ] AC-008: Kill works
- [ ] AC-009: Environment works
- [ ] AC-010: Shell escaping works

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Command building
- [ ] UT-002: Shell escaping
- [ ] UT-003: Exit code parsing
- [ ] UT-004: Timeout logic

### Integration Tests

- [ ] IT-001: Real SSH execution
- [ ] IT-002: Streaming output
- [ ] IT-003: PTY interaction
- [ ] IT-004: Process control

---

## Implementation Prompt

### Interface

```csharp
public record SshExecuteOptions(
    string WorkingDirectory = null,
    IReadOnlyDictionary<string, string> Environment = null,
    TimeSpan? Timeout = null,
    bool UsePty = false,
    PtyOptions PtyOptions = null,
    string Shell = null,
    bool CombineOutput = false);

public record PtyOptions(
    int Columns = 80,
    int Rows = 24,
    string TerminalType = "xterm-256color",
    bool StripAnsi = false);

public record SshCommandResult(
    int ExitCode,
    string Stdout,
    string Stderr,
    TimeSpan Duration,
    bool TimedOut,
    bool Killed);
```

### Shell Escaper

```csharp
public static class ShellEscaper
{
    public static string Escape(string value, ShellType shell);
    public static string Quote(string value);
    public static string BuildCommand(
        string command,
        string workingDir,
        IReadOnlyDictionary<string, string> env);
}

public enum ShellType { Bash, Sh, Zsh }
```

---

**End of Task 030.b Specification**