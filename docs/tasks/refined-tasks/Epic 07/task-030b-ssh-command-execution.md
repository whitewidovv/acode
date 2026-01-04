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