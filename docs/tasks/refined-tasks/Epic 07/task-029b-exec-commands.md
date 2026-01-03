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

- Task 029.a: Uses prepared workspace
- Task 027: Workers invoke execution
- Task 030-031: Override for remotes

### Failure Modes

- Command not found → Clear error
- Timeout → Kill and report
- Crash → Capture output
- Signal → Handle gracefully

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

### FR-001 to FR-030: Command Execution

- FR-001: `ExecuteAsync` MUST run command
- FR-002: Command MUST be string
- FR-003: Shell MUST be configurable
- FR-004: Default: `/bin/bash -c` (Unix)
- FR-005: Default: `cmd /c` (Windows)
- FR-006: Working directory MUST be workspace
- FR-007: Working directory MUST be overridable
- FR-008: Environment MUST be inherited
- FR-009: Additional env MUST be addable
- FR-010: Env values MUST override
- FR-011: Secrets MUST be passed via env
- FR-012: Secrets MUST NOT be logged
- FR-013: Timeout MUST be enforced
- FR-014: Default timeout: task-specific
- FR-015: Timeout MUST kill process
- FR-016: Kill MUST use SIGTERM first
- FR-017: SIGKILL after 10 seconds
- FR-018: Exit code MUST be captured
- FR-019: Success: exit code 0
- FR-020: Failure: exit code non-zero
- FR-021: Stdout MUST be captured
- FR-022: Stderr MUST be captured
- FR-023: Combined output MUST be available
- FR-024: Separate streams MUST be available
- FR-025: Streaming MUST be supported
- FR-026: Real-time output MUST stream
- FR-027: Buffer limit MUST be configurable
- FR-028: Default buffer: 10MB
- FR-029: Overflow MUST truncate oldest
- FR-030: Truncation MUST be indicated

### FR-031 to FR-050: Result Handling

- FR-031: `ExecutionResult` MUST be returned
- FR-032: Result MUST include exit code
- FR-033: Result MUST include stdout
- FR-034: Result MUST include stderr
- FR-035: Result MUST include duration
- FR-036: Result MUST include started at
- FR-037: Result MUST include completed at
- FR-038: Result MUST include truncated flag
- FR-039: Success property MUST exist
- FR-040: Success = exit code 0
- FR-041: Timeout property MUST exist
- FR-042: Cancelled property MUST exist
- FR-043: Error message MUST be parsed
- FR-044: Common patterns MUST be recognized
- FR-045: Build errors MUST be extracted
- FR-046: Test failures MUST be extracted
- FR-047: Result MUST be serializable
- FR-048: Result MUST be loggable
- FR-049: Sensitive output MUST be redactable
- FR-050: Result MUST support JSON export

### FR-051 to FR-065: Streaming

- FR-051: Streaming callback MUST work
- FR-052: Callback per line MUST work
- FR-053: Callback per chunk MUST work
- FR-054: Chunk size MUST be configurable
- FR-055: Default chunk: 4KB
- FR-056: Stream type MUST be indicated
- FR-057: Types: stdout, stderr
- FR-058: Timestamp MUST be per-chunk
- FR-059: Backpressure MUST be handled
- FR-060: Slow consumer MUST buffer
- FR-061: Buffer full MUST drop or block
- FR-062: Default: drop oldest
- FR-063: Streaming MUST not block execution
- FR-064: Final result MUST include all
- FR-065: Stream end MUST be signaled

---

## Non-Functional Requirements

- NFR-001: Execution start MUST be <100ms
- NFR-002: Output latency MUST be <100ms
- NFR-003: 1MB/s output MUST handle
- NFR-004: Concurrent executions MUST work
- NFR-005: Memory bounded per execution
- NFR-006: Clean cancellation
- NFR-007: No zombie processes
- NFR-008: Resource cleanup on failure
- NFR-009: Cross-platform support
- NFR-010: Structured logging

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

- [ ] AC-001: Commands execute
- [ ] AC-002: Exit code captured
- [ ] AC-003: Stdout captured
- [ ] AC-004: Stderr captured
- [ ] AC-005: Timeout enforced
- [ ] AC-006: Cancellation works
- [ ] AC-007: Streaming works
- [ ] AC-008: Environment works
- [ ] AC-009: Errors handled
- [ ] AC-010: Cross-platform works

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Command parsing
- [ ] UT-002: Timeout logic
- [ ] UT-003: Result building
- [ ] UT-004: Stream handling

### Integration Tests

- [ ] IT-001: Full execution
- [ ] IT-002: Long-running command
- [ ] IT-003: Output streaming
- [ ] IT-004: Error scenarios

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