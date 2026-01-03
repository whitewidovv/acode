# Task 018.a: Stdout/Stderr Capture + Exit Code + Timeout

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 018 (Structured Command Runner), Task 050 (Workspace Database)  

---

## Description

Task 018.a implements stdout/stderr capture, exit code handling, and timeout enforcement. These are the core mechanics of command execution.

Output capture is essential. The agent needs command output to understand results. Stdout contains normal output. Stderr contains errors and warnings.

Capturing both streams simultaneously is non-trivial. Streams must be read asynchronously. Deadlocks occur if buffers fill. Proper async patterns prevent this.

Exit codes indicate success or failure. Zero means success. Non-zero means failure. The specific code often indicates error type.

Timeout prevents hung processes. Commands can hang indefinitely. The agent cannot wait forever. Timeout kills stuck processes after a limit.

Timeout handling is graceful. First, send interrupt signal. Wait briefly. If still running, force kill. Capture whatever output was produced.

Large output handling is critical. Commands can produce megabytes of output. Memory must be managed. Truncation may be necessary.

Stream encoding matters. Output may be UTF-8, UTF-16, or platform default. Encoding detection ensures correct text interpretation.

Binary output detection prevents corruption. Some commands produce binary. Attempting to interpret as text corrupts data. Detection prevents this.

Audit events capture execution metadata. Start time, end time, exit code, output size are recorded. Correlation IDs enable tracing.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Stdout | Standard output stream |
| Stderr | Standard error stream |
| Exit Code | Process return value |
| Timeout | Maximum execution time |
| Async Read | Non-blocking stream read |
| Buffer | In-memory data storage |
| Deadlock | Mutual blocking condition |
| SIGINT | Interrupt signal |
| SIGKILL | Force kill signal |
| Encoding | Text byte interpretation |
| UTF-8 | Unicode encoding |
| Binary | Non-text output |
| Truncation | Cutting excess output |
| Graceful Kill | Orderly process termination |

---

## Out of Scope

The following items are explicitly excluded from Task 018.a:

- **Working directory** - See Task 018.b
- **Environment variables** - See Task 018.b
- **Artifact logging** - See Task 018.c
- **Real-time streaming** - Batch capture only
- **Interactive input** - No stdin support
- **Pseudo-terminal** - No PTY support
- **Signal handling** - Platform default only

---

## Functional Requirements

### Stdout Capture

- FR-001: Redirect stdout
- FR-002: Async read stdout
- FR-003: Buffer stdout content
- FR-004: Handle large stdout
- FR-005: Detect stdout encoding
- FR-006: Convert to string

### Stderr Capture

- FR-007: Redirect stderr
- FR-008: Async read stderr
- FR-009: Buffer stderr content
- FR-010: Handle large stderr
- FR-011: Detect stderr encoding
- FR-012: Convert to string

### Parallel Capture

- FR-013: Read both streams concurrently
- FR-014: Prevent deadlock
- FR-015: Wait for both completion
- FR-016: Handle partial reads
- FR-017: Combine results

### Exit Code

- FR-018: Wait for process exit
- FR-019: Capture exit code
- FR-020: Handle -1 (not started)
- FR-021: Handle crash codes
- FR-022: Map to success/failure

### Timeout

- FR-023: Track elapsed time
- FR-024: Check against limit
- FR-025: Interrupt on timeout
- FR-026: Wait for graceful exit
- FR-027: Force kill if needed
- FR-028: Mark result as timed out
- FR-029: Capture partial output

### Output Limits

- FR-030: Configurable max size
- FR-031: Track output size
- FR-032: Truncate when exceeded
- FR-033: Mark as truncated
- FR-034: Preserve important content

### Encoding

- FR-035: Detect BOM
- FR-036: Use UTF-8 default
- FR-037: Handle invalid bytes
- FR-038: Replace invalid chars
- FR-039: Preserve original bytes option

### Binary Detection

- FR-040: Check for null bytes
- FR-041: Check for control chars
- FR-042: Flag as binary
- FR-043: Return byte summary

### Audit Recording

- FR-044: Record capture start
- FR-045: Record capture end
- FR-046: Record output sizes
- FR-047: Record truncation
- FR-048: Store correlation IDs
- FR-049: Persist to workspace DB

---

## Non-Functional Requirements

### Performance

- NFR-001: Stream read latency < 10ms
- NFR-002: Buffer allocation efficient
- NFR-003: No memory leaks
- NFR-004: Handle 10MB output

### Reliability

- NFR-005: No deadlocks
- NFR-006: Handle process crash
- NFR-007: Handle stream close
- NFR-008: Consistent state

### Accuracy

- NFR-009: Complete output capture
- NFR-010: Correct exit code
- NFR-011: Accurate timing
- NFR-012: Correct encoding

---

## User Manual Documentation

### Overview

Output capture handles stdout, stderr, exit code, and timeout for command execution. Reliable capture enables the agent to understand command results.

### Configuration

```yaml
# .agent/config.yml
execution:
  capture:
    # Maximum stdout size (KB)
    max_stdout_kb: 1024
    
    # Maximum stderr size (KB)
    max_stderr_kb: 256
    
    # Default encoding
    encoding: utf-8
    
    # Handle invalid encoding
    invalid_char_handling: replace
    
  timeout:
    # Default timeout (seconds)
    default_seconds: 300
    
    # Grace period before kill (ms)
    grace_period_ms: 5000
    
    # Use SIGINT first
    graceful_interrupt: true
```

### Output Capture Modes

| Mode | Description |
|------|-------------|
| Full | Capture all output |
| Truncate | Capture up to limit |
| Tail | Keep last N bytes |
| Head | Keep first N bytes |

### Exit Code Interpretation

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1-127 | Application error |
| 128+N | Signal N received |
| -1 | Process not started |
| 137 | Killed (128+9) |
| 143 | Terminated (128+15) |

### Timeout Handling

```
Time    0s     T-5s    T      T+5s
        │       │      │       │
        ├───────┼──────┼───────┤
        │       │SIGINT│SIGKILL│
        │       │      │       │
      Start   Grace  Timeout  Kill
```

### Captured Result

```json
{
  "stdout": "Build succeeded.\n8 Warning(s)\n0 Error(s)",
  "stderr": "MSBuild version 17.0.0\n",
  "exitCode": 0,
  "success": true,
  "timedOut": false,
  "stdoutTruncated": false,
  "stderrTruncated": false,
  "stdoutBytes": 47,
  "stderrBytes": 25,
  "encoding": "utf-8",
  "isBinary": false
}
```

### Troubleshooting

#### Missing Output

**Problem:** Output not captured

**Solutions:**
1. Check output redirection
2. Verify process started
3. Check for early exit
4. Increase timeout

#### Truncated Output

**Problem:** Output cut off

**Solutions:**
1. Increase max_stdout_kb
2. Use tail mode for logs
3. Redirect to file instead

#### Encoding Issues

**Problem:** Garbled text

**Solutions:**
1. Specify correct encoding
2. Use replace mode for invalid chars
3. Check for binary output

---

## Acceptance Criteria

### Stdout

- [ ] AC-001: Stdout captured
- [ ] AC-002: Large stdout handled
- [ ] AC-003: Encoding correct

### Stderr

- [ ] AC-004: Stderr captured
- [ ] AC-005: Large stderr handled
- [ ] AC-006: Encoding correct

### Exit Code

- [ ] AC-007: Exit code captured
- [ ] AC-008: Zero = success
- [ ] AC-009: Non-zero = failure

### Timeout

- [ ] AC-010: Timeout detected
- [ ] AC-011: Process killed
- [ ] AC-012: Partial output captured
- [ ] AC-013: Result marked timed out

### Limits

- [ ] AC-014: Truncation works
- [ ] AC-015: Size tracked
- [ ] AC-016: Truncation marked

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Execution/Capture/
├── StdoutCaptureTests.cs
│   ├── Should_Capture_All_Output()
│   ├── Should_Handle_Large_Output()
│   └── Should_Detect_Encoding()
│
├── StderrCaptureTests.cs
│   ├── Should_Capture_All_Errors()
│   └── Should_Handle_Mixed_Streams()
│
├── ExitCodeTests.cs
│   ├── Should_Capture_Zero()
│   ├── Should_Capture_NonZero()
│   └── Should_Handle_Crash()
│
├── TimeoutTests.cs
│   ├── Should_Kill_On_Timeout()
│   ├── Should_Graceful_First()
│   └── Should_Capture_Partial()
│
└── TruncationTests.cs
    ├── Should_Truncate_Large()
    └── Should_Mark_Truncated()
```

### Integration Tests

```
Tests/Integration/Execution/Capture/
├── CaptureIntegrationTests.cs
│   ├── Should_Capture_Real_Command()
│   └── Should_Handle_Real_Timeout()
```

### E2E Tests

```
Tests/E2E/Execution/Capture/
├── CaptureE2ETests.cs
│   └── Should_Capture_Via_CLI()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Capture 1MB stdout | 50ms | 100ms |
| Parallel stream read | 10ms overhead | 20ms |
| Timeout detection | 50ms accuracy | 100ms |

---

## User Verification Steps

### Scenario 1: Capture Stdout

1. Run command with output
2. Verify: All stdout captured

### Scenario 2: Capture Stderr

1. Run command with errors
2. Verify: All stderr captured

### Scenario 3: Exit Code

1. Run command that fails
2. Verify: Exit code correct

### Scenario 4: Timeout

1. Run command that hangs
2. Set short timeout
3. Verify: Killed after timeout

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/
├── Execution/
│   └── Capture/
│       ├── OutputCapture.cs
│       ├── StreamReader.cs
│       ├── TimeoutHandler.cs
│       ├── EncodingDetector.cs
│       └── BinaryDetector.cs
```

### OutputCapture Class

```csharp
namespace AgenticCoder.Infrastructure.Execution.Capture;

public class OutputCapture
{
    private readonly OutputCaptureOptions _options;
    
    public async Task<CaptureResult> CaptureAsync(
        Process process,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        
        var stdoutTask = ReadStreamAsync(process.StandardOutput, _options.MaxStdoutBytes, cts.Token);
        var stderrTask = ReadStreamAsync(process.StandardError, _options.MaxStderrBytes, cts.Token);
        
        try
        {
            await Task.WhenAll(stdoutTask, stderrTask);
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            await KillGracefullyAsync(process);
        }
        
        return new CaptureResult
        {
            Stdout = stdoutTask.Result,
            Stderr = stderrTask.Result,
            ExitCode = process.ExitCode,
            TimedOut = cts.IsCancellationRequested
        };
    }
}
```

### TimeoutHandler Class

```csharp
public class TimeoutHandler
{
    public async Task KillGracefullyAsync(
        Process process,
        TimeSpan gracePeriod,
        CancellationToken ct = default)
    {
        if (process.HasExited) return;
        
        // Try interrupt first
        try
        {
            process.Kill(entireProcessTree: false);
        }
        catch { }
        
        // Wait for graceful exit
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(gracePeriod);
        
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Force kill
            process.Kill(entireProcessTree: true);
        }
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-CAP-001 | Capture failed |
| ACODE-CAP-002 | Timeout exceeded |
| ACODE-CAP-003 | Stream error |
| ACODE-CAP-004 | Encoding error |

### Implementation Checklist

1. [ ] Create output capture
2. [ ] Implement async stream reading
3. [ ] Add timeout handling
4. [ ] Add graceful kill
5. [ ] Add encoding detection
6. [ ] Add truncation
7. [ ] Add binary detection
8. [ ] Add unit tests

### Rollout Plan

1. **Phase 1:** Basic capture
2. **Phase 2:** Parallel streams
3. **Phase 3:** Timeout handling
4. **Phase 4:** Encoding/truncation
5. **Phase 5:** Integration

---

**End of Task 018.a Specification**