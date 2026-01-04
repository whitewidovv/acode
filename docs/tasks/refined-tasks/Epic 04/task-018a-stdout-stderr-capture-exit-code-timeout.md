# Task 018.a: Stdout/Stderr Capture + Exit Code + Timeout

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 018 (Structured Command Runner), Task 050 (Workspace Database)  

---

## Description

### Overview

Task 018.a implements the foundational output capture mechanics for Agentic Coding Bot's command execution system. This subtask delivers reliable stdout/stderr capture, exit code handling, and timeout enforcement—the core primitives upon which all command execution depends.

Robust output capture is non-trivial. Both stdout and stderr must be read concurrently and asynchronously to prevent deadlocks when process buffers fill. The implementation must handle edge cases including large outputs, mixed encodings, binary content, process crashes, and hung processes that never terminate.

### Business Value

Reliable output capture provides essential value:

1. **Agent Intelligence** — The agent needs command output to understand results, detect errors, and make decisions
2. **Debugging Support** — Captured output enables developers to diagnose issues when builds or tests fail
3. **Audit Compliance** — Complete capture history supports security review and compliance requirements
4. **User Visibility** — Users can review what commands produced, building trust in agent operations
5. **Error Recovery** — Timeout handling prevents hung processes from blocking agent progress indefinitely

### Scope

This subtask delivers:

1. **Stdout Capture** — Asynchronous capture of standard output with encoding detection and size limiting
2. **Stderr Capture** — Parallel capture of standard error with the same robustness guarantees
3. **Exit Code Handling** — Correct capture of process exit codes including crash and signal codes
4. **Timeout Enforcement** — Graceful timeout with SIGINT followed by SIGKILL if process doesn't respond
5. **Output Limiting** — Configurable maximum output sizes with truncation when exceeded
6. **Encoding Detection** — BOM detection and UTF-8 default with graceful handling of invalid sequences
7. **Binary Detection** — Identification of binary output to prevent corruption from text interpretation
8. **Audit Recording** — Capture metadata persisted for debugging and compliance

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 018 Command Runner | Parent | Provides the structured command execution framework |
| Task 018.b Working Directory | Sibling | Environment setup before capture begins |
| Task 018.c Artifact Logging | Sibling | Consumes capture results for artifact storage |
| Task 021 Run Inspection | Downstream | Stores capture results as run artifacts |
| Process Abstraction | Internal | Wraps .NET Process for testability |

### Failure Modes

| Failure | Detection | Impact | Recovery |
|---------|-----------|--------|----------|
| Stream buffer deadlock | Task hangs indefinitely | Capture never completes | Prevented by async parallel reads |
| Encoding exception | DecoderFallbackException | Text corruption | Use replacement character fallback |
| Process crash before output | ExitCode is non-zero, output empty | Partial data | Return what was captured |
| Memory exhaustion from large output | OutOfMemoryException | Process crash | Enforce size limits with truncation |
| Graceful kill ignored | Process still running after grace period | Resource leak | Force kill with SIGKILL |

### Assumptions

1. Commands execute as child processes of the agent
2. .NET Process class provides stdout/stderr streams
3. Process.Kill() is available on all platforms
4. UTF-8 is the default encoding for most command output
5. Cancellation tokens are respected for async operations

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

### Stdout Capture (FR-018A-01 through FR-018A-15)

| ID | Requirement |
|----|-------------|
| FR-018A-01 | System MUST redirect stdout from child process |
| FR-018A-02 | System MUST read stdout asynchronously |
| FR-018A-03 | System MUST buffer stdout content in memory |
| FR-018A-04 | System MUST handle stdout up to configured max size |
| FR-018A-05 | System MUST detect stdout encoding via BOM |
| FR-018A-06 | System MUST default to UTF-8 if no BOM detected |
| FR-018A-07 | System MUST convert stdout bytes to string |
| FR-018A-08 | System MUST replace invalid encoding bytes with U+FFFD |
| FR-018A-09 | System MUST track stdout byte count |
| FR-018A-10 | System MUST mark stdout as truncated when limit exceeded |
| FR-018A-11 | System MUST preserve stdout head when truncating |
| FR-018A-12 | System MUST handle stdout stream close gracefully |
| FR-018A-13 | System MUST handle stdout containing null bytes |
| FR-018A-14 | System MUST support cancellation during stdout read |
| FR-018A-15 | System MUST return stdout even if process crashes |

### Stderr Capture (FR-018A-16 through FR-018A-25)

| ID | Requirement |
|----|-------------|
| FR-018A-16 | System MUST redirect stderr from child process |
| FR-018A-17 | System MUST read stderr asynchronously |
| FR-018A-18 | System MUST buffer stderr content in memory |
| FR-018A-19 | System MUST handle stderr up to configured max size |
| FR-018A-20 | System MUST detect stderr encoding via BOM |
| FR-018A-21 | System MUST convert stderr bytes to string |
| FR-018A-22 | System MUST track stderr byte count |
| FR-018A-23 | System MUST mark stderr as truncated when limit exceeded |
| FR-018A-24 | System MUST handle stderr stream close gracefully |
| FR-018A-25 | System MUST return stderr even if process crashes |

### Parallel Capture (FR-018A-26 through FR-018A-35)

| ID | Requirement |
|----|-------------|
| FR-018A-26 | System MUST read stdout and stderr concurrently |
| FR-018A-27 | System MUST prevent deadlock from buffer exhaustion |
| FR-018A-28 | System MUST wait for both streams to complete |
| FR-018A-29 | System MUST handle one stream finishing before other |
| FR-018A-30 | System MUST combine results into single response |
| FR-018A-31 | System MUST handle interleaved output correctly |
| FR-018A-32 | System MUST not lose data from either stream |
| FR-018A-33 | System MUST handle empty streams |
| FR-018A-34 | System MUST handle process exit before stream EOF |
| FR-018A-35 | System MUST handle stream error on one stream while other continues |

### Exit Code Handling (FR-018A-36 through FR-018A-48)

| ID | Requirement |
|----|-------------|
| FR-018A-36 | System MUST wait for process exit |
| FR-018A-37 | System MUST capture process exit code |
| FR-018A-38 | System MUST return exit code in result |
| FR-018A-39 | System MUST interpret exit code 0 as success |
| FR-018A-40 | System MUST interpret non-zero exit code as failure |
| FR-018A-41 | System MUST handle exit code -1 (process not started) |
| FR-018A-42 | System MUST handle exit code 137 (SIGKILL on Linux) |
| FR-018A-43 | System MUST handle exit code 143 (SIGTERM on Linux) |
| FR-018A-44 | System MUST handle access violation codes on Windows |
| FR-018A-45 | System MUST map platform exit codes to standard meanings |
| FR-018A-46 | System MUST capture exit code even after timeout |
| FR-018A-47 | System MUST not throw when exit code is negative |
| FR-018A-48 | System MUST provide success/failure boolean from exit code |

### Timeout Enforcement (FR-018A-49 through FR-018A-65)

| ID | Requirement |
|----|-------------|
| FR-018A-49 | System MUST track elapsed time during execution |
| FR-018A-50 | System MUST enforce configured timeout limit |
| FR-018A-51 | System MUST send SIGINT/Ctrl+C first on timeout |
| FR-018A-52 | System MUST wait grace period after SIGINT |
| FR-018A-53 | System MUST send SIGKILL if process survives grace period |
| FR-018A-54 | System MUST mark result as timed out |
| FR-018A-55 | System MUST capture partial output before kill |
| FR-018A-56 | System MUST not throw exception on timeout |
| FR-018A-57 | System MUST handle process exiting during grace period |
| FR-018A-58 | System MUST kill entire process tree on timeout |
| FR-018A-59 | System MUST release resources after kill |
| FR-018A-60 | System MUST report actual duration in result |
| FR-018A-61 | System MUST support CancellationToken for external cancellation |
| FR-018A-62 | System MUST treat cancellation differently from timeout |
| FR-018A-63 | System MUST cleanup on cancellation |
| FR-018A-64 | System MUST support infinite timeout (TimeSpan.MaxValue) |
| FR-018A-65 | System MUST support zero timeout (immediate) |

### Output Limits (FR-018A-66 through FR-018A-75)

| ID | Requirement |
|----|-------------|
| FR-018A-66 | System MUST support configurable max stdout size |
| FR-018A-67 | System MUST support configurable max stderr size |
| FR-018A-68 | System MUST track output size during capture |
| FR-018A-69 | System MUST stop capturing when limit exceeded |
| FR-018A-70 | System MUST mark output as truncated |
| FR-018A-71 | System MUST report original size if known |
| FR-018A-72 | System MUST support head truncation mode (keep first N bytes) |
| FR-018A-73 | System MUST support tail truncation mode (keep last N bytes) |
| FR-018A-74 | System MUST default to head truncation |
| FR-018A-75 | System MUST handle exactly-at-limit output |

### Encoding Detection (FR-018A-76 through FR-018A-85)

| ID | Requirement |
|----|-------------|
| FR-018A-76 | System MUST check for UTF-8 BOM (EF BB BF) |
| FR-018A-77 | System MUST check for UTF-16 LE BOM (FF FE) |
| FR-018A-78 | System MUST check for UTF-16 BE BOM (FE FF) |
| FR-018A-79 | System MUST default to UTF-8 if no BOM |
| FR-018A-80 | System MUST support override encoding via configuration |
| FR-018A-81 | System MUST handle invalid UTF-8 sequences |
| FR-018A-82 | System MUST use replacement character for invalid bytes |
| FR-018A-83 | System MUST preserve original bytes in binary mode |
| FR-018A-84 | System MUST report detected encoding in result |
| FR-018A-85 | System MUST handle mixed encoding gracefully |

### Binary Detection (FR-018A-86 through FR-018A-92)

| ID | Requirement |
|----|-------------|
| FR-018A-86 | System MUST check for null bytes in output |
| FR-018A-87 | System MUST check for non-printable control characters |
| FR-018A-88 | System MUST flag output as binary when detected |
| FR-018A-89 | System MUST return byte count for binary output |
| FR-018A-90 | System MUST return hex preview for binary output |
| FR-018A-91 | System MUST not attempt text conversion for binary |
| FR-018A-92 | System MUST support force-text mode override |

### Audit Recording (FR-018A-93 through FR-018A-100)

| ID | Requirement |
|----|-------------|
| FR-018A-93 | System MUST record capture start timestamp |
| FR-018A-94 | System MUST record capture end timestamp |
| FR-018A-95 | System MUST record stdout size in bytes |
| FR-018A-96 | System MUST record stderr size in bytes |
| FR-018A-97 | System MUST record whether truncation occurred |
| FR-018A-98 | System MUST include correlation IDs in audit |
| FR-018A-99 | System MUST persist audit to workspace database |
| FR-018A-100 | System MUST emit structured log events for capture |

---

## Non-Functional Requirements

### Performance (NFR-018A-01 through NFR-018A-12)

| ID | Requirement |
|----|-------------|
| NFR-018A-01 | Stream read latency MUST be under 10ms per chunk |
| NFR-018A-02 | Buffer allocation MUST use pooled memory |
| NFR-018A-03 | System MUST handle 10MB output without OOM |
| NFR-018A-04 | System MUST handle 100MB output with truncation |
| NFR-018A-05 | Parallel read overhead MUST be under 10ms |
| NFR-018A-06 | Timeout detection accuracy MUST be within 50ms |
| NFR-018A-07 | Graceful kill MUST complete within grace period + 1s |
| NFR-018A-08 | Memory usage MUST not exceed 2x captured output size |
| NFR-018A-09 | Encoding detection MUST complete within 1ms |
| NFR-018A-10 | Binary detection MUST complete within 1ms |
| NFR-018A-11 | Capture of 1MB output MUST complete within 100ms |
| NFR-018A-12 | No memory leaks during capture |

### Reliability (NFR-018A-13 through NFR-018A-22)

| ID | Requirement |
|----|-------------|
| NFR-018A-13 | System MUST NOT deadlock under any circumstances |
| NFR-018A-14 | System MUST handle process crash gracefully |
| NFR-018A-15 | System MUST handle stream close gracefully |
| NFR-018A-16 | System MUST maintain consistent state after timeout |
| NFR-018A-17 | System MUST release all resources after capture |
| NFR-018A-18 | System MUST handle concurrent captures |
| NFR-018A-19 | System MUST be reentrant |
| NFR-018A-20 | System MUST handle disk full during audit write |
| NFR-018A-21 | System MUST continue after failed audit write |
| NFR-018A-22 | System MUST log all exceptions with context |

### Accuracy (NFR-018A-23 through NFR-018A-30)

| ID | Requirement |
|----|-------------|
| NFR-018A-23 | System MUST capture 100% of output up to limit |
| NFR-018A-24 | System MUST report correct exit code |
| NFR-018A-25 | System MUST report accurate timing to 1ms resolution |
| NFR-018A-26 | System MUST correctly identify encoding |
| NFR-018A-27 | System MUST correctly detect binary content |
| NFR-018A-28 | System MUST correctly report truncation |
| NFR-018A-29 | System MUST preserve output byte order |
| NFR-018A-30 | System MUST not corrupt output data |

### Maintainability (NFR-018A-31 through NFR-018A-38)

| ID | Requirement |
|----|-------------|
| NFR-018A-31 | All classes MUST have interfaces for mocking |
| NFR-018A-32 | Configuration MUST be injectable |
| NFR-018A-33 | Logging MUST use ILogger abstraction |
| NFR-018A-34 | Unit test coverage MUST exceed 90% |
| NFR-018A-35 | Code MUST follow C# coding conventions |
| NFR-018A-36 | All public methods MUST have XML documentation |
| NFR-018A-37 | Complex logic MUST have inline comments |
| NFR-018A-38 | Error messages MUST be actionable |

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

### Stdout Capture (AC-018A-01 to AC-018A-12)

- [ ] AC-018A-01: Stdout from command is captured completely
- [ ] AC-018A-02: Large stdout (1MB) is captured without error
- [ ] AC-018A-03: Very large stdout (10MB) triggers truncation
- [ ] AC-018A-04: Truncated stdout is marked as truncated
- [ ] AC-018A-05: Stdout encoding detected correctly (UTF-8)
- [ ] AC-018A-06: Stdout encoding detected correctly (UTF-16)
- [ ] AC-018A-07: Invalid encoding bytes replaced with U+FFFD
- [ ] AC-018A-08: Empty stdout returns empty string
- [ ] AC-018A-09: Stdout with null bytes handled correctly
- [ ] AC-018A-10: Stdout captured even if process crashes
- [ ] AC-018A-11: Stdout byte count is accurate
- [ ] AC-018A-12: Stdout capture supports cancellation

### Stderr Capture (AC-018A-13 to AC-018A-20)

- [ ] AC-018A-13: Stderr from command is captured completely
- [ ] AC-018A-14: Large stderr is captured without error
- [ ] AC-018A-15: Truncated stderr is marked as truncated
- [ ] AC-018A-16: Empty stderr returns empty string
- [ ] AC-018A-17: Stderr captured independently of stdout
- [ ] AC-018A-18: Interleaved stdout/stderr handled correctly
- [ ] AC-018A-19: Stderr byte count is accurate
- [ ] AC-018A-20: Stderr capture supports cancellation

### Parallel Capture (AC-018A-21 to AC-018A-28)

- [ ] AC-018A-21: Both stdout and stderr captured concurrently
- [ ] AC-018A-22: No deadlock with large output on both streams
- [ ] AC-018A-23: Both streams complete before result returned
- [ ] AC-018A-24: One stream empty, other full - both handled
- [ ] AC-018A-25: Both streams full - both captured to limit
- [ ] AC-018A-26: Process exit before stream EOF handled
- [ ] AC-018A-27: Stream error on one doesn't break other
- [ ] AC-018A-28: Combined result contains both outputs

### Exit Code (AC-018A-29 to AC-018A-38)

- [ ] AC-018A-29: Exit code 0 captured and returned
- [ ] AC-018A-30: Exit code 1 captured and returned
- [ ] AC-018A-31: Exit code 127 (command not found) captured
- [ ] AC-018A-32: Exit code 137 (SIGKILL) captured
- [ ] AC-018A-33: Exit code 143 (SIGTERM) captured
- [ ] AC-018A-34: Negative exit code handled without exception
- [ ] AC-018A-35: Exit code 0 maps to Success=true
- [ ] AC-018A-36: Non-zero exit code maps to Success=false
- [ ] AC-018A-37: Exit code available after timeout
- [ ] AC-018A-38: Exit code available after cancellation

### Timeout (AC-018A-39 to AC-018A-52)

- [ ] AC-018A-39: Command completes before timeout - no interruption
- [ ] AC-018A-40: Command exceeds timeout - SIGINT sent
- [ ] AC-018A-41: Command exits during grace period - captured
- [ ] AC-018A-42: Command ignores SIGINT - SIGKILL sent after grace
- [ ] AC-018A-43: Partial output captured before kill
- [ ] AC-018A-44: Result marked as TimedOut=true
- [ ] AC-018A-45: Actual duration reported correctly
- [ ] AC-018A-46: Process tree killed on timeout
- [ ] AC-018A-47: Resources released after timeout
- [ ] AC-018A-48: External cancellation handled differently
- [ ] AC-018A-49: Cancelled result marked as Cancelled=true
- [ ] AC-018A-50: Infinite timeout works correctly
- [ ] AC-018A-51: Very short timeout (100ms) works
- [ ] AC-018A-52: Zero timeout kills immediately

### Output Limits (AC-018A-53 to AC-018A-60)

- [ ] AC-018A-53: Max stdout size configurable
- [ ] AC-018A-54: Max stderr size configurable
- [ ] AC-018A-55: Output at exactly max size - not truncated
- [ ] AC-018A-56: Output 1 byte over max - truncated
- [ ] AC-018A-57: Truncation preserves head by default
- [ ] AC-018A-58: Tail truncation mode works
- [ ] AC-018A-59: Original size reported when truncated
- [ ] AC-018A-60: Truncation flag accurate

### Encoding (AC-018A-61 to AC-018A-68)

- [ ] AC-018A-61: UTF-8 BOM detected correctly
- [ ] AC-018A-62: UTF-16 LE BOM detected correctly
- [ ] AC-018A-63: UTF-16 BE BOM detected correctly
- [ ] AC-018A-64: No BOM defaults to UTF-8
- [ ] AC-018A-65: Invalid UTF-8 sequences handled
- [ ] AC-018A-66: Replacement character used for invalid bytes
- [ ] AC-018A-67: Detected encoding reported in result
- [ ] AC-018A-68: Override encoding configuration works

### Binary Detection (AC-018A-69 to AC-018A-75)

- [ ] AC-018A-69: Null bytes trigger binary detection
- [ ] AC-018A-70: Control characters trigger binary detection
- [ ] AC-018A-71: Binary flag set correctly
- [ ] AC-018A-72: Byte count returned for binary output
- [ ] AC-018A-73: Hex preview generated for binary
- [ ] AC-018A-74: Text output not marked as binary
- [ ] AC-018A-75: Force-text mode overrides detection

### Audit (AC-018A-76 to AC-018A-82)

- [ ] AC-018A-76: Capture start timestamp recorded
- [ ] AC-018A-77: Capture end timestamp recorded
- [ ] AC-018A-78: Stdout size recorded
- [ ] AC-018A-79: Stderr size recorded
- [ ] AC-018A-80: Truncation status recorded
- [ ] AC-018A-81: Correlation IDs included
- [ ] AC-018A-82: Audit persisted to workspace database

---

## Testing Requirements

### Unit Tests

#### OutputCaptureTests
- OutputCapture_CapturesStdout_WhenCommandProducesOutput
- OutputCapture_CapturesStderr_WhenCommandProducesErrors
- OutputCapture_CapturesBothStreams_Concurrently
- OutputCapture_PreventsDeadlock_WhenBothStreamsProduceLargeOutput
- OutputCapture_ReturnsEmptyStrings_WhenNoOutput
- OutputCapture_ReturnsPartialOutput_WhenProcessCrashes
- OutputCapture_RespectsMaxStdoutSize_WhenConfigured
- OutputCapture_RespectsMaxStderrSize_WhenConfigured
- OutputCapture_MarksTruncated_WhenOutputExceedsLimit
- OutputCapture_PreservesHead_ByDefault
- OutputCapture_PreservesTail_WhenTailModeConfigured
- OutputCapture_HandlesCancellation_Gracefully
- OutputCapture_ReportsAccurateByteCount_ForStdout
- OutputCapture_ReportsAccurateByteCount_ForStderr

#### StreamReaderTests
- StreamReader_ReadsAllContent_FromStream
- StreamReader_ReadsAsync_WithoutBlocking
- StreamReader_HandlesLargeContent_WithBuffering
- StreamReader_StopsReading_AtMaxSize
- StreamReader_HandlesPrematureClose_Gracefully
- StreamReader_SupportsInterruption_ViaCancellation
- StreamReader_ReturnsPartialContent_OnInterruption
- StreamReader_HandlesEmptyStream_WithoutError
- StreamReader_HandlesNullBytes_InContent

#### ExitCodeHandlerTests
- ExitCodeHandler_ReturnsZero_ForSuccessfulCommand
- ExitCodeHandler_ReturnsNonZero_ForFailedCommand
- ExitCodeHandler_Returns127_ForCommandNotFound
- ExitCodeHandler_Returns137_ForKilledProcess
- ExitCodeHandler_Returns143_ForTerminatedProcess
- ExitCodeHandler_HandlesNegative_WithoutException
- ExitCodeHandler_MapsToSuccess_WhenZero
- ExitCodeHandler_MapsToFailure_WhenNonZero
- ExitCodeHandler_Available_AfterTimeout
- ExitCodeHandler_Available_AfterCancellation

#### TimeoutHandlerTests
- TimeoutHandler_AllowsCompletion_BeforeTimeout
- TimeoutHandler_SendsSigint_OnTimeout
- TimeoutHandler_WaitsGracePeriod_AfterSigint
- TimeoutHandler_SendsSigkill_AfterGracePeriod
- TimeoutHandler_CapturesPartialOutput_BeforeKill
- TimeoutHandler_MarksResultAsTimedOut_Correctly
- TimeoutHandler_ReportsActualDuration_Accurately
- TimeoutHandler_KillsProcessTree_NotJustParent
- TimeoutHandler_ReleasesResources_AfterKill
- TimeoutHandler_HandlesZeroTimeout_Correctly
- TimeoutHandler_HandlesInfiniteTimeout_Correctly
- TimeoutHandler_HandlesExitDuringGrace_Correctly

#### GracefulKillTests
- GracefulKill_SendsSigint_First
- GracefulKill_WaitsConfiguredPeriod_BeforeSigkill
- GracefulKill_SkipsToSigkill_IfConfigured
- GracefulKill_HandlesAlreadyExited_Gracefully
- GracefulKill_KillsEntireTree_OnLinux
- GracefulKill_KillsEntireTree_OnWindows

#### EncodingDetectorTests
- EncodingDetector_DetectsUtf8Bom_Correctly
- EncodingDetector_DetectsUtf16LeBom_Correctly
- EncodingDetector_DetectsUtf16BeBom_Correctly
- EncodingDetector_DefaultsToUtf8_WhenNoBom
- EncodingDetector_HandlesInvalidSequences_WithReplacement
- EncodingDetector_ReportsDetectedEncoding_InResult
- EncodingDetector_SupportsOverride_ViaConfiguration
- EncodingDetector_HandlesMixedEncoding_Gracefully

#### BinaryDetectorTests
- BinaryDetector_DetectsNullBytes_AsBinary
- BinaryDetector_DetectsControlChars_AsBinary
- BinaryDetector_FlagsAsBinary_WhenDetected
- BinaryDetector_ReturnsByteCount_ForBinary
- BinaryDetector_ReturnsHexPreview_ForBinary
- BinaryDetector_DoesNotFlagText_AsBinary
- BinaryDetector_SupportsForceText_Override

#### AuditRecorderTests
- AuditRecorder_RecordsStartTimestamp_Correctly
- AuditRecorder_RecordsEndTimestamp_Correctly
- AuditRecorder_RecordsStdoutSize_Accurately
- AuditRecorder_RecordsStderrSize_Accurately
- AuditRecorder_RecordsTruncation_WhenOccurs
- AuditRecorder_IncludesCorrelationIds_InRecord
- AuditRecorder_PersistsToDatabase_Successfully
- AuditRecorder_HandlesDbError_Gracefully

### Integration Tests

#### CaptureIntegrationTests
- Capture_RealCommand_ReturnsCorrectOutput
- Capture_CommandWithLargeOutput_HandlesCorrectly
- Capture_CommandThatFails_CapturesStderr
- Capture_CommandThatHangs_TimesOutCorrectly
- Capture_ConcurrentCommands_AllCapturedCorrectly
- Capture_CommandWithBinaryOutput_DetectedCorrectly
- Capture_CrossPlatform_WorksOnWindows
- Capture_CrossPlatform_WorksOnLinux

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Capture 1KB stdout | 5ms | 10ms |
| Capture 100KB stdout | 20ms | 50ms |
| Capture 1MB stdout | 50ms | 100ms |
| Capture 10MB stdout | 200ms | 500ms |
| Parallel stream overhead | 5ms | 10ms |
| Timeout detection accuracy | 20ms | 50ms |
| Graceful kill completion | grace + 100ms | grace + 500ms |
| Encoding detection | 0.5ms | 1ms |
| Binary detection | 0.5ms | 1ms |

### Coverage Requirements

| Component | Minimum Coverage |
|-----------|-----------------|
| OutputCapture | 95% |
| StreamReader | 95% |
| ExitCodeHandler | 90% |
| TimeoutHandler | 95% |
| GracefulKill | 90% |
| EncodingDetector | 90% |
| BinaryDetector | 90% |
| AuditRecorder | 85% |

---

## User Verification Steps

### Scenario 1: Capture Stdout

**Objective:** Verify stdout is captured completely

**Steps:**
1. Run a command that produces stdout: `acode exec -- echo "Hello World"`
2. Observe the captured output
3. Verify the output matches exactly

**Expected Results:**
- Output shows "Hello World"
- No data is missing
- Encoding is correct (no garbled characters)

### Scenario 2: Capture Stderr

**Objective:** Verify stderr is captured independently

**Steps:**
1. Run a command that produces stderr: `acode exec -- bash -c 'echo error >&2'`
2. Observe the captured output
3. Verify stderr is separate from stdout

**Expected Results:**
- Stderr shows "error"
- Stdout is empty
- Both streams are accessible separately

### Scenario 3: Exit Code Success

**Objective:** Verify exit code 0 is captured

**Steps:**
1. Run a command that succeeds: `acode exec -- true`
2. Observe the result
3. Verify exit code is 0

**Expected Results:**
- Exit code is 0
- Success flag is true
- No error in result

### Scenario 4: Exit Code Failure

**Objective:** Verify non-zero exit code is captured

**Steps:**
1. Run a command that fails: `acode exec -- false`
2. Observe the result
3. Verify exit code is 1

**Expected Results:**
- Exit code is 1
- Success flag is false
- Result indicates failure

### Scenario 5: Timeout Enforcement

**Objective:** Verify timeout kills hung process

**Steps:**
1. Run a command that hangs: `acode exec --timeout 2s -- sleep 60`
2. Wait for timeout
3. Observe the result

**Expected Results:**
- Command killed after ~2 seconds
- Result marked as TimedOut
- Partial output captured if any
- Process tree terminated

### Scenario 6: Graceful Kill

**Objective:** Verify graceful kill sequence

**Steps:**
1. Run a command with signal handler: `acode exec --timeout 5s -- bash -c 'trap "echo caught" SIGINT; sleep 60'`
2. Wait for timeout and grace period
3. Observe the output

**Expected Results:**
- SIGINT sent first
- Process has chance to respond
- If not exited, SIGKILL sent
- Output shows "caught" if trap worked

### Scenario 7: Large Output Truncation

**Objective:** Verify large output is truncated

**Steps:**
1. Configure max stdout to 1KB
2. Run: `acode exec -- yes | head -n 10000`
3. Observe the captured output

**Expected Results:**
- Output is approximately 1KB
- Truncation flag is true
- Original size reported
- Head of output preserved

### Scenario 8: Binary Detection

**Objective:** Verify binary output is detected

**Steps:**
1. Run: `acode exec -- cat /bin/ls` (or equivalent binary)
2. Observe the result

**Expected Results:**
- Binary flag is true
- Byte count reported
- Hex preview available
- No text corruption

### Scenario 9: Encoding Detection

**Objective:** Verify encoding is detected correctly

**Steps:**
1. Run command producing UTF-8 with special chars: `acode exec -- echo "résumé"`
2. Observe the output
3. Verify encoding is correct

**Expected Results:**
- Accented characters display correctly
- Encoding reported as UTF-8
- No replacement characters (unless invalid)

### Scenario 10: Concurrent Capture

**Objective:** Verify both streams captured without deadlock

**Steps:**
1. Run command producing large output on both streams
2. Wait for completion
3. Verify both are captured

**Expected Results:**
- Both stdout and stderr captured
- No deadlock
- No data loss
- Reasonable completion time

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/Execution/Capture/
├── OutputCapture.cs              # Main capture orchestrator
├── IOutputCapture.cs             # Interface for testing
├── StreamReader.cs               # Async stream reader
├── TimeoutHandler.cs             # Timeout and kill logic
├── GracefulKill.cs               # Graceful process termination
├── EncodingDetector.cs           # BOM and encoding detection
├── BinaryDetector.cs             # Binary content detection
├── AuditRecorder.cs              # Capture audit logging
├── CaptureResult.cs              # Result model
├── CaptureOptions.cs             # Configuration model
└── TruncationMode.cs             # Enum: Head, Tail

tests/AgenticCoder.Infrastructure.Tests/Execution/Capture/
├── OutputCaptureTests.cs
├── StreamReaderTests.cs
├── TimeoutHandlerTests.cs
├── GracefulKillTests.cs
├── EncodingDetectorTests.cs
├── BinaryDetectorTests.cs
├── AuditRecorderTests.cs
└── Integration/
    └── CaptureIntegrationTests.cs
```

### CaptureResult Model

```csharp
namespace AgenticCoder.Infrastructure.Execution.Capture;

public sealed record CaptureResult
{
    public required string Stdout { get; init; }
    public required string Stderr { get; init; }
    public required int ExitCode { get; init; }
    public required bool Success { get; init; }
    public required bool TimedOut { get; init; }
    public required bool Cancelled { get; init; }
    public required TimeSpan Duration { get; init; }
    
    // Truncation info
    public bool StdoutTruncated { get; init; }
    public bool StderrTruncated { get; init; }
    public long StdoutBytes { get; init; }
    public long StderrBytes { get; init; }
    public long? OriginalStdoutBytes { get; init; }
    public long? OriginalStderrBytes { get; init; }
    
    // Encoding info
    public string Encoding { get; init; } = "utf-8";
    public bool IsBinary { get; init; }
    public string? BinaryHexPreview { get; init; }
}
```

### CaptureOptions Model

```csharp
namespace AgenticCoder.Infrastructure.Execution.Capture;

public sealed record CaptureOptions
{
    public long MaxStdoutBytes { get; init; } = 1024 * 1024;  // 1MB
    public long MaxStderrBytes { get; init; } = 256 * 1024;   // 256KB
    public TruncationMode TruncationMode { get; init; } = TruncationMode.Head;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan GracePeriod { get; init; } = TimeSpan.FromSeconds(5);
    public bool GracefulInterrupt { get; init; } = true;
    public Encoding? ForcedEncoding { get; init; }
    public bool ForceTextMode { get; init; }
}

public enum TruncationMode { Head, Tail }
```

### IOutputCapture Interface

```csharp
namespace AgenticCoder.Infrastructure.Execution.Capture;

public interface IOutputCapture
{
    Task<CaptureResult> CaptureAsync(
        Process process,
        CaptureOptions options,
        CancellationToken ct = default);
}
```

### OutputCapture Implementation

```csharp
namespace AgenticCoder.Infrastructure.Execution.Capture;

public sealed class OutputCapture : IOutputCapture
{
    private readonly ILogger<OutputCapture> _logger;
    private readonly ITimeoutHandler _timeoutHandler;
    private readonly IEncodingDetector _encodingDetector;
    private readonly IBinaryDetector _binaryDetector;
    private readonly IAuditRecorder _auditRecorder;
    
    public OutputCapture(
        ILogger<OutputCapture> logger,
        ITimeoutHandler timeoutHandler,
        IEncodingDetector encodingDetector,
        IBinaryDetector binaryDetector,
        IAuditRecorder auditRecorder)
    {
        _logger = logger;
        _timeoutHandler = timeoutHandler;
        _encodingDetector = encodingDetector;
        _binaryDetector = binaryDetector;
        _auditRecorder = auditRecorder;
    }
    
    public async Task<CaptureResult> CaptureAsync(
        Process process,
        CaptureOptions options,
        CancellationToken ct = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        await _auditRecorder.RecordStartAsync(process.Id, startTime);
        
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(options.Timeout);
        
        var stdoutTask = ReadStreamAsync(
            process.StandardOutput.BaseStream, 
            options.MaxStdoutBytes, 
            options.TruncationMode,
            timeoutCts.Token);
            
        var stderrTask = ReadStreamAsync(
            process.StandardError.BaseStream, 
            options.MaxStderrBytes, 
            options.TruncationMode,
            timeoutCts.Token);
        
        bool timedOut = false;
        bool cancelled = false;
        
        try
        {
            await Task.WhenAll(stdoutTask, stderrTask);
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            if (ct.IsCancellationRequested)
            {
                cancelled = true;
                _logger.LogInformation("Capture cancelled by request");
            }
            else
            {
                timedOut = true;
                _logger.LogWarning("Capture timed out after {Timeout}", options.Timeout);
            }
            
            await _timeoutHandler.KillGracefullyAsync(
                process, 
                options.GracePeriod, 
                options.GracefulInterrupt);
        }
        
        var stdoutResult = await stdoutTask;
        var stderrResult = await stderrTask;
        
        var encoding = _encodingDetector.Detect(stdoutResult.Bytes, options.ForcedEncoding);
        var isBinary = !options.ForceTextMode && _binaryDetector.IsBinary(stdoutResult.Bytes);
        
        var stdout = isBinary 
            ? $"[Binary content: {stdoutResult.Bytes.Length} bytes]"
            : encoding.GetString(stdoutResult.Bytes);
            
        var stderr = encoding.GetString(stderrResult.Bytes);
        
        var endTime = DateTimeOffset.UtcNow;
        var duration = endTime - startTime;
        
        var result = new CaptureResult
        {
            Stdout = stdout,
            Stderr = stderr,
            ExitCode = process.HasExited ? process.ExitCode : -1,
            Success = process.HasExited && process.ExitCode == 0,
            TimedOut = timedOut,
            Cancelled = cancelled,
            Duration = duration,
            StdoutTruncated = stdoutResult.Truncated,
            StderrTruncated = stderrResult.Truncated,
            StdoutBytes = stdoutResult.Bytes.Length,
            StderrBytes = stderrResult.Bytes.Length,
            OriginalStdoutBytes = stdoutResult.OriginalSize,
            OriginalStderrBytes = stderrResult.OriginalSize,
            Encoding = encoding.WebName,
            IsBinary = isBinary,
            BinaryHexPreview = isBinary ? GetHexPreview(stdoutResult.Bytes) : null
        };
        
        await _auditRecorder.RecordEndAsync(process.Id, endTime, result);
        
        return result;
    }
    
    private async Task<StreamReadResult> ReadStreamAsync(
        Stream stream,
        long maxBytes,
        TruncationMode mode,
        CancellationToken ct)
    {
        var buffer = new MemoryStream();
        var readBuffer = ArrayPool<byte>.Shared.Rent(8192);
        long totalRead = 0;
        bool truncated = false;
        
        try
        {
            while (true)
            {
                int bytesRead;
                try
                {
                    bytesRead = await stream.ReadAsync(readBuffer, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                
                if (bytesRead == 0) break;
                
                totalRead += bytesRead;
                
                if (buffer.Length + bytesRead <= maxBytes)
                {
                    buffer.Write(readBuffer, 0, bytesRead);
                }
                else
                {
                    truncated = true;
                    if (mode == TruncationMode.Head)
                    {
                        var remaining = (int)(maxBytes - buffer.Length);
                        if (remaining > 0)
                        {
                            buffer.Write(readBuffer, 0, remaining);
                        }
                    }
                    else // Tail mode
                    {
                        // Keep reading but only keep last maxBytes
                        buffer.Write(readBuffer, 0, bytesRead);
                        if (buffer.Length > maxBytes)
                        {
                            var newBuffer = new MemoryStream();
                            buffer.Seek(buffer.Length - maxBytes, SeekOrigin.Begin);
                            buffer.CopyTo(newBuffer);
                            buffer = newBuffer;
                        }
                    }
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
        }
        
        return new StreamReadResult
        {
            Bytes = buffer.ToArray(),
            Truncated = truncated,
            OriginalSize = truncated ? totalRead : null
        };
    }
    
    private static string GetHexPreview(byte[] bytes, int maxBytes = 64)
    {
        var preview = bytes.Take(maxBytes).ToArray();
        return BitConverter.ToString(preview).Replace("-", " ");
    }
}
```

### TimeoutHandler Implementation

```csharp
namespace AgenticCoder.Infrastructure.Execution.Capture;

public interface ITimeoutHandler
{
    Task KillGracefullyAsync(
        Process process,
        TimeSpan gracePeriod,
        bool gracefulFirst,
        CancellationToken ct = default);
}

public sealed class TimeoutHandler : ITimeoutHandler
{
    private readonly ILogger<TimeoutHandler> _logger;
    
    public TimeoutHandler(ILogger<TimeoutHandler> logger)
    {
        _logger = logger;
    }
    
    public async Task KillGracefullyAsync(
        Process process,
        TimeSpan gracePeriod,
        bool gracefulFirst,
        CancellationToken ct = default)
    {
        if (process.HasExited)
        {
            _logger.LogDebug("Process {Pid} already exited", process.Id);
            return;
        }
        
        if (gracefulFirst)
        {
            _logger.LogInformation("Sending interrupt to process {Pid}", process.Id);
            try
            {
                // On Unix: SIGINT, On Windows: GenerateConsoleCtrlEvent
                if (OperatingSystem.IsWindows())
                {
                    // Windows doesn't have clean SIGINT for child processes
                    // Fall through to kill
                }
                else
                {
                    Process.Start("kill", $"-INT {process.Id}")?.WaitForExit(1000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to send interrupt, will force kill");
            }
            
            // Wait for graceful exit
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(gracePeriod);
            
            try
            {
                await process.WaitForExitAsync(cts.Token);
                _logger.LogInformation("Process {Pid} exited gracefully", process.Id);
                return;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Process {Pid} did not exit within grace period", process.Id);
            }
        }
        
        // Force kill
        _logger.LogWarning("Force killing process {Pid}", process.Id);
        try
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to kill process {Pid}", process.Id);
        }
    }
}
```

### Error Codes

| Code | Meaning | User Message |
|------|---------|--------------|
| ACODE-CAP-001 | Capture failed | "Failed to capture command output: {0}" |
| ACODE-CAP-002 | Timeout exceeded | "Command timed out after {0} seconds" |
| ACODE-CAP-003 | Stream error | "Error reading command output stream: {0}" |
| ACODE-CAP-004 | Encoding error | "Failed to decode output with {0} encoding" |
| ACODE-CAP-005 | Kill failed | "Failed to terminate process {0}" |

### Implementation Checklist

| Step | Task | Verification |
|------|------|--------------|
| 1 | Create CaptureResult and CaptureOptions models | Models compile |
| 2 | Create IOutputCapture interface | Interface compiles |
| 3 | Implement async stream reading | Unit tests pass |
| 4 | Implement parallel stdout/stderr capture | No deadlocks in tests |
| 5 | Implement TimeoutHandler | Timeout tests pass |
| 6 | Implement graceful kill sequence | Kill tests pass |
| 7 | Implement EncodingDetector | Encoding tests pass |
| 8 | Implement BinaryDetector | Binary detection works |
| 9 | Implement truncation logic | Truncation tests pass |
| 10 | Implement AuditRecorder | Audit persisted |
| 11 | Write integration tests | Real commands captured |
| 12 | Performance benchmarks | Targets met |

### Rollout Plan

| Phase | Action | Success Criteria |
|-------|--------|------------------|
| 1 | Implement stream reading | Can read stdout/stderr |
| 2 | Implement parallel capture | No deadlocks |
| 3 | Implement timeout handling | Hung processes killed |
| 4 | Implement encoding/binary detection | Correct detection |
| 5 | Implement truncation | Large output handled |
| 6 | Implement audit | Capture logged |
| 7 | Integration testing | All scenarios pass |

### Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| System.Buffers | Built-in | ArrayPool for efficient buffering |
| System.Text.Encoding | Built-in | Encoding detection and conversion |

---

**End of Task 018.a Specification**