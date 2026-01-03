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

### Interface

```csharp
public record ExecutionCommand(
    string Command,
    TimeSpan? Timeout = null,
    string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string>? Environment = null);

public record ExecutionResult(
    int ExitCode,
    string Stdout,
    string Stderr,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    TimeSpan Duration,
    bool Truncated,
    bool TimedOut,
    bool Cancelled)
{
    public bool Success => ExitCode == 0 && !TimedOut && !Cancelled;
}

public record OutputLine(
    OutputStream Stream,
    string Text,
    DateTimeOffset Timestamp);

public enum OutputStream { Stdout, Stderr }

public delegate Task OutputHandler(OutputLine line);

// Extended interface
public interface IComputeTargetExecution
{
    Task<ExecutionResult> ExecuteAsync(
        ExecutionCommand command,
        OutputHandler? outputHandler = null,
        CancellationToken ct = default);
}
```

---

**End of Task 029.b Specification**