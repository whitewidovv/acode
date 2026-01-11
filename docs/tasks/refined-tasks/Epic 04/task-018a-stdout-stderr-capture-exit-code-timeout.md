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

### Business Value and ROI Analysis

Reliable output capture provides essential value:

1. **Agent Intelligence** — The agent needs command output to understand results, detect errors, and make decisions
2. **Debugging Support** — Captured output enables developers to diagnose issues when builds or tests fail
3. **Audit Compliance** — Complete capture history supports security review and compliance requirements
4. **User Visibility** — Users can review what commands produced, building trust in agent operations
5. **Error Recovery** — Timeout handling prevents hung processes from blocking agent progress indefinitely

#### ROI Calculation

| Metric | Before (Basic Capture) | After (Robust Capture) | Annual Savings |
|--------|------------------------|------------------------|----------------|
| Deadlock Recovery Time | 30 min/incident | 0 (prevented) | $1,875/incident |
| Memory Crash Recovery | 2 hours/incident | 0 (prevented) | $150/incident |
| Incidents per Year | 50 deadlocks, 20 crashes | 0 | N/A |
| Debug Time Saved | N/A | 1.5 hours/failure | $112.50/failure |
| Build Failures Analyzed | 200/year | 200/year | N/A |
| Hung Process Investigation | 1 hour/incident | 5 min/incident | $68.75/incident |
| Hung Incidents per Year | 100 | 100 | $6,875 |

**Assumptions:**
- $75/hour fully loaded developer cost
- 10-person development team
- 50 work weeks/year

**Total Annual ROI:**
- Deadlock Prevention: 50 × $1,875 = **$93,750**
- Memory Crash Prevention: 20 × $150 = **$3,000**
- Debug Time Savings: 200 × $112.50 = **$22,500**
- Hung Process Savings: 100 × $68.75 = **$6,875**

**Total Annual Savings: $126,125**

**Implementation Cost:** 24 hours × $100/hour = $2,400
**ROI: 5,155%** | **Payback Period: 7 days**

#### Before/After Comparison

| Aspect | Before (Naive Capture) | After (Robust Capture) |
|--------|------------------------|------------------------|
| Stdout/Stderr | Sequential reads | Parallel async reads |
| Deadlock Risk | High (buffer fills) | Zero (async draining) |
| Large Output | Memory exhaustion | Bounded with truncation |
| Timeout | Process hangs indefinitely | Graceful SIGINT → SIGKILL |
| Encoding | Assumed UTF-8, crashes on invalid | BOM detection + fallback |
| Binary Output | Corrupted as text | Detected and flagged |
| Crash Handling | Lost output | Partial capture preserved |
| Exit Codes | Only checked for 0 | Full signal mapping |

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                   OUTPUT CAPTURE ARCHITECTURE                                │
│                        Task 018.a                                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                        Process                                       │   │
│   │  ┌─────────────────┐              ┌─────────────────┐               │   │
│   │  │     STDOUT      │              │     STDERR      │               │   │
│   │  │  (pipe buffer)  │              │  (pipe buffer)  │               │   │
│   │  └────────┬────────┘              └────────┬────────┘               │   │
│   └───────────┼────────────────────────────────┼────────────────────────┘   │
│               │                                │                             │
│               ▼                                ▼                             │
│   ┌─────────────────────┐          ┌─────────────────────┐                  │
│   │  AsyncStreamReader  │          │  AsyncStreamReader  │                  │
│   │   (OutputDataRcvd)  │          │   (ErrorDataRcvd)   │                  │
│   │                     │          │                     │                  │
│   │  ┌───────────────┐  │          │  ┌───────────────┐  │                  │
│   │  │ StringBuilder │  │          │  │ StringBuilder │  │                  │
│   │  │ (with limit)  │  │          │  │ (with limit)  │  │                  │
│   │  └───────────────┘  │          │  └───────────────┘  │                  │
│   └──────────┬──────────┘          └──────────┬──────────┘                  │
│              │                                │                              │
│              └────────────┬───────────────────┘                              │
│                           │                                                  │
│                           ▼                                                  │
│              ┌─────────────────────────┐                                     │
│              │    OutputAggregator     │                                     │
│              │  ┌─────────────────────┐│                                     │
│              │  │ Wait for both reads ││                                     │
│              │  │ + process exit      ││                                     │
│              │  └─────────────────────┘│                                     │
│              └────────────┬────────────┘                                     │
│                           │                                                  │
│                           ▼                                                  │
│              ┌─────────────────────────┐                                     │
│              │    TimeoutEnforcer      │                                     │
│              │  ┌─────────────────────┐│                                     │
│              │  │ Timer → SIGINT      ││                                     │
│              │  │ Grace → SIGKILL     ││                                     │
│              │  └─────────────────────┘│                                     │
│              └────────────┬────────────┘                                     │
│                           │                                                  │
│                           ▼                                                  │
│              ┌─────────────────────────┐                                     │
│              │    CaptureResult        │                                     │
│              │  ┌─────────────────────┐│                                     │
│              │  │ Stdout (string)     ││                                     │
│              │  │ Stderr (string)     ││                                     │
│              │  │ ExitCode (int)      ││                                     │
│              │  │ TimedOut (bool)     ││                                     │
│              │  │ Truncation (info)   ││                                     │
│              │  └─────────────────────┘│                                     │
│              └─────────────────────────┘                                     │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

                    DEADLOCK PREVENTION SEQUENCE

    ┌──────────┐   ┌──────────┐   ┌────────────┐   ┌────────────┐
    │ Process  │   │ Stdout   │   │  Stderr    │   │ Aggregator │
    │          │   │ Reader   │   │  Reader    │   │            │
    └────┬─────┘   └────┬─────┘   └─────┬──────┘   └─────┬──────┘
         │              │               │                │
         │  Start async read            │                │
         │──────────────>               │                │
         │              │  Start async read              │
         │              │───────────────>               │
         │              │               │                │
         │  Data chunk  │               │                │
         │──────────────>               │                │
         │              │  Buffer data  │                │
         │              │────────┐      │                │
         │              │        │      │                │
         │              │<───────┘      │                │
         │              │               │                │
         │  Data chunk  │               │                │
         │──────────────────────────────>               │
         │              │               │  Buffer data   │
         │              │               │─────────┐      │
         │              │               │         │      │
         │              │               │<────────┘      │
         │              │               │                │
         │  EOF stdout  │               │                │
         │──────────────>               │                │
         │              │  Signal done  │                │
         │              │───────────────────────────────>│
         │              │               │                │
         │  EOF stderr  │               │                │
         │──────────────────────────────>               │
         │              │               │  Signal done   │
         │              │               │───────────────>│
         │              │               │                │
         │  Exit(0)     │               │                │
         │──────────────────────────────────────────────>│
         │              │               │                │
         │              │               │    Combine     │
         │              │               │    Results     │
         │              │               │       │        │
         │              │               │       ▼        │
         │              │               │  CaptureResult │
    ┌────┴─────┐   ┌────┴─────┐   ┌─────┴──────┐   ┌─────┴──────┐
    │ Process  │   │ Stdout   │   │  Stderr    │   │ Aggregator │
    │          │   │ Reader   │   │  Reader    │   │            │
    └──────────┘   └──────────┘   └────────────┘   └────────────┘
```

### Technical Scope

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

### Architectural Trade-offs

#### Trade-off 1: Event-Based vs Manual Stream Reading

| Approach | Pros | Cons |
|----------|------|------|
| **Event-Based (Chosen)** | Built-in async, handles buffering | Less control over timing |
| Manual StreamReader | Full control, custom buffer size | Must manage threading manually |
| Memory-mapped | Highest performance | Complex, platform-specific |

**Decision:** Event-based using OutputDataReceived/ErrorDataReceived for simplicity and reliability.

#### Trade-off 2: String vs Byte Buffer

| Approach | Pros | Cons |
|----------|------|------|
| **String Buffer (Chosen)** | Simple API, handles encoding | Memory overhead for large output |
| Byte Buffer | Efficient, exact size control | Must handle encoding separately |
| Ring Buffer | Bounded memory, keeps recent | Loses beginning of output |

**Decision:** String buffer with StringBuilder for most use cases. Byte buffer available for binary mode.

#### Trade-off 3: Timeout Implementation

| Approach | Pros | Cons |
|----------|------|------|
| **CancellationToken + Kill (Chosen)** | Standard .NET pattern | Requires careful cleanup |
| Process.WaitForExit with timeout | Simple API | No cancellation support |
| External watchdog process | Isolated, reliable | Complexity, cross-process IPC |

**Decision:** CancellationToken with linked timeout CTS for idiomatic .NET async patterns.

#### Trade-off 4: Graceful vs Immediate Kill

| Approach | Pros | Cons |
|----------|------|------|
| **SIGINT → SIGKILL (Chosen)** | Allows cleanup, then forces | More complex timing |
| Immediate SIGKILL | Fast, guaranteed termination | No cleanup opportunity |
| SIGTERM only | Standard Unix convention | May be ignored |

**Decision:** Two-phase: SIGINT/Ctrl+C first with 2s grace period, then SIGKILL if still running.

### Failure Modes and Mitigations

| Failure | Detection | Impact | Recovery |
|---------|-----------|--------|----------|
| Stream buffer deadlock | Task hangs indefinitely | Capture never completes | Prevented by async parallel reads |
| Encoding exception | DecoderFallbackException | Text corruption | Use replacement character fallback |
| Process crash before output | ExitCode is non-zero, output empty | Partial data | Return what was captured |
| Memory exhaustion from large output | OutOfMemoryException | Process crash | Enforce size limits with truncation |
| Graceful kill ignored | Process still running after grace period | Resource leak | Force kill with SIGKILL |

---

## Use Cases

### Scenario 1: DevBot Captures Build Output

**Persona:** DevBot, an AI developer assistant working on a complex .NET solution

**Before (Naive Capture):**
1. DevBot runs `dotnet build` on a solution with 50 projects
2. Build produces 10MB of output including warnings
3. Stdout and stderr read sequentially
4. Process blocks writing to stderr (buffer full)
5. Agent hangs waiting for stdout EOF
6. Timeout never triggers (stdout read blocking)
7. User must manually kill agent
8. Build state unknown, no output captured

**After (Robust Capture):**
1. DevBot runs `dotnet build` on a solution with 50 projects
2. Build produces 10MB of output including warnings
3. AsyncStreamReader drains stdout and stderr in parallel
4. No deadlock, buffers never fill
5. Output captured (truncated at 1MB with indicator)
6. ExitCode = 0, Success = true
7. Truncation info shows: "Original: 10MB, Captured: 1MB"
8. Agent continues to next task

**Metrics:**
- Deadlock incidents: 100% eliminated
- Capture reliability: 99.9% (from ~70%)
- Improvement: **Eliminates blocking entirely**

---

### Scenario 2: Marcus Investigates Test Failure

**Persona:** Marcus, a developer debugging a failing test in CI

**Before (Basic Exit Code):**
1. Agent reports "Tests failed, exit code: 1"
2. Marcus asks "Which test failed?"
3. Agent can only report exit code was non-zero
4. Marcus downloads CI logs manually
5. Logs show test output with stack trace
6. 30 minutes wasted finding obvious error

**After (Complete Capture with Exit Code Mapping):**
1. Agent reports:
   ```
   Tests failed: exit code 1
   STDOUT (last 100 lines):
     [FAIL] CustomerService.GetById_NotFound_ReturnsNull
       Expected: null
       Actual: Exception("Customer not found")
       at CustomerServiceTests.cs:45
   
   STDERR:
     Build succeeded.
     Test run failed.
     Failed: 1, Passed: 247, Skipped: 3
   ```
2. Marcus immediately sees the failing test and line number
3. Fix applied in 5 minutes
4. No log hunting required

**Metrics:**
- Debug time: 30 min → 5 min
- Context switches: 5 → 0
- Improvement: **6x faster diagnosis**

---

### Scenario 3: Operations Team Handles Hung Build

**Persona:** Taylor, a DevOps engineer monitoring agent builds overnight

**Before (No Timeout):**
1. Overnight build runs `npm install` on corrupted package
2. npm hangs waiting for user input (package-lock conflict)
3. Agent hangs indefinitely
4. Morning: 8 hours wasted, no builds completed
5. Taylor must SSH in and kill processes manually
6. Zombie npm processes consuming memory
7. Build server needs restart

**After (Timeout with Graceful Kill):**
1. Overnight build runs `npm install` on corrupted package
2. npm hangs waiting for user input
3. After 5 minutes (configured timeout): SIGINT sent
4. npm gracefully shuts down, writes partial lockfile
5. After 2s grace: still not exited → SIGKILL
6. Process tree killed, resources freed
7. Agent reports:
   ```
   Command timed out after 5m
   TimedOut: true
   Partial stdout captured: "Installing dependencies..."
   ExitCode: -1 (killed)
   ```
8. Agent moves to next task, emails Taylor
9. Morning: Taylor sees one build timed out, 20 succeeded

**Metrics:**
- Hung build impact: 8 hours → 5 minutes
- Manual intervention: Required → None
- Recovery: Restart server → Automatic
- Improvement: **96x faster recovery**

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

---

## Assumptions

### Technical Assumptions

1. **Process Stream Availability:** The .NET Process class provides accessible StandardOutput and StandardError streams when RedirectStandard* properties are true.

2. **Asynchronous Read Support:** OutputDataReceived and ErrorDataReceived events fire on background threads and can be handled concurrently.

3. **UTF-8 Default:** Most command-line tools output UTF-8 encoded text. Non-UTF-8 output is the exception, not the rule.

4. **BOM Detection:** When a Byte Order Mark is present, it indicates the correct encoding. UTF-8 BOM (EF BB BF) is rare but supported.

5. **Process Kill Capability:** Process.Kill() is available and functional on all supported platforms (Windows, Linux, macOS).

6. **Signal Delivery:** On Unix-like systems, SIGINT (2) and SIGKILL (9) are deliverable to child processes. On Windows, Ctrl+C events are delivered.

7. **Exit Code Range:** Exit codes are 8-bit values (0-255) on Unix, 32-bit signed integers on Windows.

### Operational Assumptions

8. **Buffer Sizes:** Default OS pipe buffer sizes (typically 4KB-64KB) are adequate when async reading drains continuously.

9. **Memory Availability:** Sufficient memory exists to buffer up to the configured maximum output size (default 1MB per stream).

10. **Timeout Values:** Users configure appropriate timeouts. Default 5-minute timeout is sufficient for typical build/test commands.

11. **Cancellation Respected:** Async operations honor CancellationToken cancellation within reasonable time (< 100ms).

12. **Process Trees:** Child processes can be killed when parent is killed, though this requires platform-specific handling.

### Integration Assumptions

13. **Audit System Ready:** The audit service (Task 003.c) is available to persist capture metadata.

14. **Database Available:** The workspace database (Task 050) is writable for storing capture results.

15. **Correlation Context:** Correlation IDs are available via ambient context when capture begins.

16. **Configuration Loaded:** Capture configuration (max sizes, timeouts) is loaded before capture operations begin.

17. **Encoding Fallback:** When encoding detection fails or invalid sequences encountered, replacement character (U+FFFD) is acceptable.

18. **Binary Output Rare:** Binary output (executables, images) to stdout/stderr is uncommon. When detected, raw bytes can be discarded or hex-encoded.

---

## Security Threats and Mitigations

### Threat 1: Output Size Denial of Service

**Risk:** HIGH - Malicious or buggy commands could produce unlimited output, exhausting memory.

**Attack Scenario:**
```bash
# Attacker crafts a command that produces infinite output
yes "Attack payload" | head -n 100000000000
# Or a compile error that produces gigabytes of template instantiation errors
```

**Complete Mitigation Code:**

```csharp
using System;
using System.Text;
using System.Threading;

namespace Acode.Infrastructure.Execution;

/// <summary>
/// Bounded output capture that enforces size limits to prevent memory exhaustion.
/// </summary>
public sealed class BoundedOutputCapture : IDisposable
{
    private readonly StringBuilder _buffer;
    private readonly int _maxBytes;
    private readonly object _lock = new();
    private int _currentBytes;
    private bool _truncated;
    private int _droppedBytes;
    private bool _disposed;
    
    public BoundedOutputCapture(int maxBytes = 1024 * 1024)
    {
        if (maxBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxBytes), "Max bytes must be positive");
        
        _maxBytes = maxBytes;
        _buffer = new StringBuilder(Math.Min(maxBytes / 2, 64 * 1024));
    }
    
    /// <summary>
    /// Appends data with automatic truncation at limit.
    /// </summary>
    public void Append(string? data)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BoundedOutputCapture));
        
        if (string.IsNullOrEmpty(data))
            return;
        
        lock (_lock)
        {
            if (_currentBytes >= _maxBytes)
            {
                // Already at limit, just count dropped bytes
                _truncated = true;
                _droppedBytes += Encoding.UTF8.GetByteCount(data);
                return;
            }
            
            var dataBytes = Encoding.UTF8.GetByteCount(data);
            var remainingBytes = _maxBytes - _currentBytes;
            
            if (dataBytes <= remainingBytes)
            {
                // Fits completely
                _buffer.Append(data);
                _currentBytes += dataBytes;
            }
            else
            {
                // Partial fit - truncate at character boundary
                var charCount = TruncateToByteLimit(data, remainingBytes);
                _buffer.Append(data, 0, charCount);
                _currentBytes = _maxBytes;
                _truncated = true;
                _droppedBytes = dataBytes - remainingBytes;
            }
        }
    }
    
    /// <summary>
    /// Gets the captured output with truncation information.
    /// </summary>
    public CapturedOutput GetResult()
    {
        lock (_lock)
        {
            return new CapturedOutput(
                Content: _buffer.ToString(),
                IsTruncated: _truncated,
                TruncationInfo: _truncated 
                    ? new TruncationInfo(_currentBytes + _droppedBytes, _currentBytes, _droppedBytes)
                    : null);
        }
    }
    
    private int TruncateToByteLimit(string data, int maxBytes)
    {
        int byteCount = 0;
        for (int i = 0; i < data.Length; i++)
        {
            int charBytes = Encoding.UTF8.GetByteCount(data, i, 1);
            if (byteCount + charBytes > maxBytes)
                return i;
            byteCount += charBytes;
        }
        return data.Length;
    }
    
    public void Dispose()
    {
        _disposed = true;
    }
}
```

---

### Threat 2: Timeout Bypass via Process Tree

**Risk:** MEDIUM - Child processes may continue running after parent is killed.

**Attack Scenario:**
```bash
# Parent process spawns child that ignores SIGTERM
bash -c 'nohup sleep 3600 &'
# Killing bash doesn't kill the sleep process
```

**Complete Mitigation Code:**

```csharp
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Acode.Infrastructure.Execution;

/// <summary>
/// Kills process trees to prevent orphaned child processes.
/// </summary>
public static class ProcessTreeKiller
{
    /// <summary>
    /// Kills a process and all its descendants.
    /// </summary>
    public static void KillTree(int processId, TimeSpan gracePeriod)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                KillTreeWindows(processId, gracePeriod);
            }
            else
            {
                KillTreeUnix(processId, gracePeriod);
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - best effort cleanup
            Console.Error.WriteLine($"Warning: Failed to kill process tree {processId}: {ex.Message}");
        }
    }
    
    private static void KillTreeWindows(int processId, TimeSpan gracePeriod)
    {
        // First, send Ctrl+C to allow graceful shutdown
        try
        {
            var gracefulKill = Process.Start(new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/PID {processId}",
                CreateNoWindow = true,
                UseShellExecute = false
            });
            gracefulKill?.WaitForExit((int)gracePeriod.TotalMilliseconds);
        }
        catch { }
        
        // Check if still running
        try
        {
            var process = Process.GetProcessById(processId);
            if (!process.HasExited)
            {
                // Force kill the entire tree
                var forceKill = Process.Start(new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/T /F /PID {processId}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                forceKill?.WaitForExit(5000);
            }
        }
        catch (ArgumentException)
        {
            // Process already exited
        }
    }
    
    private static void KillTreeUnix(int processId, TimeSpan gracePeriod)
    {
        // Send SIGTERM to process group
        try
        {
            Process.Start("kill", $"-TERM -{processId}")?.WaitForExit(1000);
        }
        catch { }
        
        // Wait for grace period
        System.Threading.Thread.Sleep(gracePeriod);
        
        // Check if still running and force kill
        try
        {
            var process = Process.GetProcessById(processId);
            if (!process.HasExited)
            {
                // Send SIGKILL to process group
                Process.Start("kill", $"-9 -{processId}")?.WaitForExit(1000);
                
                // Also kill any children by PPID
                Process.Start("pkill", $"-9 -P {processId}")?.WaitForExit(1000);
            }
        }
        catch (ArgumentException)
        {
            // Process already exited
        }
    }
}
```

---

### Threat 3: Encoding Attack (Invalid UTF-8 Sequences)

**Risk:** MEDIUM - Malformed encoding could cause parsing errors or incorrect string handling.

**Attack Scenario:**
```
# Output contains invalid UTF-8 byte sequences
printf '\x80\x81\x82' # Invalid continuation bytes
# Could cause DecoderFallbackException or produce incorrect strings
```

**Complete Mitigation Code:**

```csharp
using System;
using System.Text;

namespace Acode.Infrastructure.Execution;

/// <summary>
/// Safe text decoder that handles invalid encoding sequences gracefully.
/// </summary>
public sealed class SafeTextDecoder
{
    private readonly Encoding _encoding;
    private readonly StringBuilder _buffer;
    private readonly byte[] _incomplete;
    private int _incompleteCount;
    
    public SafeTextDecoder(Encoding? encoding = null)
    {
        // Create encoding with replacement fallback (never throws)
        _encoding = encoding ?? Encoding.UTF8;
        _buffer = new StringBuilder();
        _incomplete = new byte[4]; // Max UTF-8 sequence length
        _incompleteCount = 0;
    }
    
    /// <summary>
    /// Creates a UTF-8 decoder with safe fallback handling.
    /// </summary>
    public static SafeTextDecoder CreateUtf8()
    {
        var encoding = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: false);
        return new SafeTextDecoder(encoding);
    }
    
    /// <summary>
    /// Decodes bytes to string, handling invalid sequences with replacement.
    /// </summary>
    public string Decode(byte[] bytes, int offset, int count)
    {
        if (bytes == null || count == 0)
            return string.Empty;
        
        // Combine with any incomplete sequence from previous call
        byte[] toProcess;
        int toProcessCount;
        
        if (_incompleteCount > 0)
        {
            toProcess = new byte[_incompleteCount + count];
            Array.Copy(_incomplete, 0, toProcess, 0, _incompleteCount);
            Array.Copy(bytes, offset, toProcess, _incompleteCount, count);
            toProcessCount = _incompleteCount + count;
            _incompleteCount = 0;
        }
        else
        {
            toProcess = bytes;
            toProcessCount = count;
        }
        
        // Check for incomplete sequence at end
        int validEnd = FindValidEnd(toProcess, 0, toProcessCount);
        
        if (validEnd < toProcessCount)
        {
            // Save incomplete bytes for next call
            _incompleteCount = toProcessCount - validEnd;
            Array.Copy(toProcess, validEnd, _incomplete, 0, _incompleteCount);
            toProcessCount = validEnd;
        }
        
        // Decode with fallback
        try
        {
            return _encoding.GetString(toProcess, 0, toProcessCount);
        }
        catch
        {
            // Ultimate fallback: replace all non-ASCII with ?
            var result = new StringBuilder(toProcessCount);
            for (int i = 0; i < toProcessCount; i++)
            {
                result.Append(toProcess[i] < 128 ? (char)toProcess[i] : '\uFFFD');
            }
            return result.ToString();
        }
    }
    
    /// <summary>
    /// Flushes any remaining incomplete sequence.
    /// </summary>
    public string Flush()
    {
        if (_incompleteCount == 0)
            return string.Empty;
        
        // Incomplete sequence at end - emit replacement characters
        var result = new string('\uFFFD', _incompleteCount);
        _incompleteCount = 0;
        return result;
    }
    
    private int FindValidEnd(byte[] bytes, int offset, int count)
    {
        int end = offset + count;
        
        // Check if last 1-3 bytes could be incomplete UTF-8 sequence
        for (int i = 1; i <= 3 && end - i >= offset; i++)
        {
            byte b = bytes[end - i];
            
            // Check for sequence start byte
            if ((b & 0xC0) == 0xC0) // 11xxxxxx = start byte
            {
                int expectedLength = 
                    (b & 0xF8) == 0xF0 ? 4 :
                    (b & 0xF0) == 0xE0 ? 3 :
                    (b & 0xE0) == 0xC0 ? 2 : 1;
                
                if (i < expectedLength)
                    return end - i; // Incomplete sequence
            }
        }
        
        return end;
    }
}
```

---

### Threat 4: Sensitive Data in Output

**Risk:** MEDIUM - Command output may contain passwords, tokens, or secrets that get logged.

**Attack Scenario:**
```bash
# Build output includes environment dump with secrets
printenv  # Dumps all environment variables including AWS_SECRET_KEY
# Or error message reveals connection string
dotnet run  # Error: Connection string 'Server=prod;Password=secret123'
```

**Complete Mitigation Code:**

```csharp
using System;
using System.Text.RegularExpressions;

namespace Acode.Infrastructure.Execution;

/// <summary>
/// Redacts sensitive information from captured command output.
/// </summary>
public sealed class OutputRedactor
{
    private static readonly Regex[] SensitivePatterns = new[]
    {
        // Environment variable patterns
        new Regex(@"(PASSWORD|SECRET|TOKEN|KEY|CREDENTIAL|AUTH)=([^\s]+)", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase),
        
        // Connection string patterns
        new Regex(@"(password|pwd)=([^;""'\s]+)", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase),
        
        // Bearer tokens
        new Regex(@"Bearer\s+[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+", 
            RegexOptions.Compiled),
        
        // AWS keys
        new Regex(@"AKIA[0-9A-Z]{16}", RegexOptions.Compiled),
        new Regex(@"aws_secret_access_key\s*=\s*([A-Za-z0-9/+=]{40})", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase),
        
        // API keys (long alphanumeric strings in key context)
        new Regex(@"(api[_-]?key|apikey)\s*[:=]\s*['""]?([A-Za-z0-9]{20,})", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase),
        
        // Private keys
        new Regex(@"-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----[\s\S]*?-----END\s+(RSA\s+)?PRIVATE\s+KEY-----", 
            RegexOptions.Compiled),
        
        // GitHub tokens
        new Regex(@"gh[pousr]_[A-Za-z0-9]{36}", RegexOptions.Compiled),
        
        // npm tokens  
        new Regex(@"npm_[A-Za-z0-9]{36}", RegexOptions.Compiled)
    };
    
    /// <summary>
    /// Redacts sensitive information from output string.
    /// </summary>
    public string Redact(string output)
    {
        if (string.IsNullOrEmpty(output))
            return output;
        
        var result = output;
        
        foreach (var pattern in SensitivePatterns)
        {
            result = pattern.Replace(result, match =>
            {
                // For patterns with groups, redact just the value
                if (match.Groups.Count > 2)
                {
                    return match.Value.Replace(
                        match.Groups[2].Value, 
                        "[REDACTED]");
                }
                
                // For full-match patterns
                if (match.Value.Length > 20)
                {
                    // Show first/last few chars for identification
                    return $"{match.Value[..4]}...[REDACTED]...{match.Value[^4..]}";
                }
                
                return "[REDACTED]";
            });
        }
        
        return result;
    }
    
    /// <summary>
    /// Checks if output likely contains secrets (for warnings).
    /// </summary>
    public bool ContainsSensitiveData(string output)
    {
        if (string.IsNullOrEmpty(output))
            return false;
        
        foreach (var pattern in SensitivePatterns)
        {
            if (pattern.IsMatch(output))
                return true;
        }
        
        return false;
    }
}
```

---

### Threat 5: Binary Output Injection

**Risk:** LOW - Binary data interpreted as text could cause log corruption or injection.

**Attack Scenario:**
```bash
# Command outputs binary that contains ANSI escape sequences
cat /bin/ls | strings  # May contain terminal escape codes
# Or intentionally crafted binary with log forging bytes
```

**Complete Mitigation Code:**

```csharp
using System;
using System.Linq;

namespace Acode.Infrastructure.Execution;

/// <summary>
/// Detects and sanitizes binary content in command output.
/// </summary>
public sealed class BinaryOutputDetector
{
    // Characters that indicate binary content
    private static readonly char[] BinaryIndicators = { '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06' };
    
    // ANSI escape sequence pattern
    private const char EscapeChar = '\x1B';
    
    /// <summary>
    /// Checks if content appears to be binary (non-text).
    /// </summary>
    public bool IsBinary(string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;
        
        // Sample first 8KB for binary detection
        var sampleLength = Math.Min(content.Length, 8192);
        var sample = content.AsSpan(0, sampleLength);
        
        int nullCount = 0;
        int controlCount = 0;
        
        foreach (char c in sample)
        {
            if (c == '\x00') nullCount++;
            if (c < 32 && c != '\n' && c != '\r' && c != '\t') controlCount++;
        }
        
        // More than 1% null bytes = definitely binary
        if (nullCount > sampleLength / 100)
            return true;
        
        // More than 10% control characters = likely binary
        if (controlCount > sampleLength / 10)
            return true;
        
        return false;
    }
    
    /// <summary>
    /// Sanitizes output by removing dangerous control sequences.
    /// </summary>
    public string Sanitize(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;
        
        var result = new char[content.Length];
        int writeIndex = 0;
        int i = 0;
        
        while (i < content.Length)
        {
            char c = content[i];
            
            // Skip ANSI escape sequences
            if (c == EscapeChar && i + 1 < content.Length && content[i + 1] == '[')
            {
                // Find end of escape sequence
                i += 2;
                while (i < content.Length && content[i] >= 0x20 && content[i] <= 0x3F)
                    i++;
                if (i < content.Length)
                    i++; // Skip final byte
                continue;
            }
            
            // Replace null bytes with visible placeholder
            if (c == '\x00')
            {
                result[writeIndex++] = '␀';
                i++;
                continue;
            }
            
            // Keep allowed control characters
            if (c == '\n' || c == '\r' || c == '\t' || c >= 32)
            {
                result[writeIndex++] = c;
            }
            else
            {
                // Replace other control chars with placeholder
                result[writeIndex++] = '�';
            }
            
            i++;
        }
        
        return new string(result, 0, writeIndex);
    }
    
    /// <summary>
    /// Converts binary content to safe hex representation.
    /// </summary>
    public string ToHexDump(byte[] bytes, int maxBytes = 512)
    {
        var length = Math.Min(bytes.Length, maxBytes);
        var hex = BitConverter.ToString(bytes, 0, length).Replace("-", " ");
        
        if (bytes.Length > maxBytes)
            hex += $"... ({bytes.Length - maxBytes} more bytes)";
        
        return $"[BINARY OUTPUT - {bytes.Length} bytes]\n{hex}";
    }
}
```

---

## Troubleshooting

### Issue 1: Output Not Captured (Empty Stdout/Stderr)

**Symptoms:**
- CommandResult.Stdout and Stderr are both empty strings
- ExitCode is 0 (command succeeded)
- Duration shows command ran for expected time
- Process started and exited normally

**Causes:**
- Output redirection not enabled in ProcessStartInfo
- Process writes to a different stream (stdout to stderr or vice versa)
- Process writes to file instead of streams
- Very fast process exits before read handlers attach
- Output captured to different encoding/codepage

**Solutions:**
1. Verify ProcessStartInfo configuration:
   ```csharp
   // Ensure redirection is enabled
   startInfo.RedirectStandardOutput = true;
   startInfo.RedirectStandardError = true;
   startInfo.UseShellExecute = false; // Required for redirection
   ```

2. Check if output goes to alternate stream:
   ```bash
   # Some commands output to stderr by default
   acode exec "curl -v https://example.com" 2>&1  # Merge streams
   ```

3. Start async readers before process exits:
   ```csharp
   process.Start();
   process.BeginOutputReadLine();  // Must call immediately after Start
   process.BeginErrorReadLine();
   ```

---

### Issue 2: Deadlock - Capture Hangs Indefinitely

**Symptoms:**
- ExecuteAsync never returns
- Process shows as running in task manager
- Memory usage slowly increases
- CPU usage is low (not spinning)
- Cancellation token doesn't help

**Causes:**
- Synchronous stream reads blocking each other
- Process waiting for stdin (expecting input)
- Process wrote to full buffer and blocked
- Async read handlers not draining buffers fast enough
- WaitForExit called before streams fully read

**Solutions:**
1. Ensure async parallel reads (correct pattern):
   ```csharp
   // WRONG: Sequential reads can deadlock
   var stdout = process.StandardOutput.ReadToEnd();  // Blocks if stderr buffer full
   var stderr = process.StandardError.ReadToEnd();
   
   // RIGHT: Parallel async reads
   var stdoutTask = process.StandardOutput.ReadToEndAsync();
   var stderrTask = process.StandardError.ReadToEndAsync();
   await Task.WhenAll(stdoutTask, stderrTask);
   ```

2. Use event-based capture:
   ```csharp
   process.OutputDataReceived += (s, e) => stdout.AppendLine(e.Data);
   process.ErrorDataReceived += (s, e) => stderr.AppendLine(e.Data);
   process.BeginOutputReadLine();
   process.BeginErrorReadLine();
   ```

3. Close stdin if not needed:
   ```csharp
   process.StandardInput.Close();  // Signal no more input
   ```

---

### Issue 3: Output Truncated Unexpectedly

**Symptoms:**
- TruncationInfo shows output was cut off
- Expected full output but got partial
- "[OUTPUT TRUNCATED]" marker in result
- DroppedBytes is significant

**Causes:**
- max_stdout_kb config too low for output size
- Command produces more output than expected
- Binary output inflated byte count
- Encoding caused byte expansion

**Solutions:**
1. Increase output limits in config:
   ```yaml
   execution:
     capture:
       max_stdout_kb: 10240  # 10MB
       max_stderr_kb: 1024   # 1MB
   ```

2. Use tail mode to keep end of output:
   ```csharp
   var options = new ExecutionOptions 
   { 
       TruncationMode = TruncationMode.KeepTail 
   };
   ```

3. Redirect large output to file:
   ```bash
   acode exec "dotnet build > build.log 2>&1"
   # Then read build.log separately
   ```

---

### Issue 4: Wrong Exit Code Reported

**Symptoms:**
- ExitCode doesn't match expected value
- ExitCode is -1 when process should have succeeded
- ExitCode is 137/143 unexpectedly
- Success is false but command seemed to work

**Causes:**
- Process killed by timeout before normal exit
- Process crashed (segfault, access violation)
- Shell wrapper changed exit code
- Exit code not captured before process disposed
- Signal translated to different exit code on different platforms

**Solutions:**
1. Check if timeout occurred:
   ```csharp
   if (result.TimedOut)
   {
       // ExitCode may be -1 or signal number
       Console.WriteLine("Command was killed due to timeout");
   }
   ```

2. Increase timeout for long commands:
   ```csharp
   var command = Command.Create("npm")
       .WithArguments("install")
       .WithTimeout(TimeSpan.FromMinutes(30))
       .Build();
   ```

3. Map signal exit codes:
   ```csharp
   // Linux signal exits are 128 + signal number
   var wasKilled = result.ExitCode == 137; // 128 + SIGKILL(9)
   var wasTermed = result.ExitCode == 143; // 128 + SIGTERM(15)
   ```

---

### Issue 5: Encoding Corruption (Garbled Text)

**Symptoms:**
- Output contains replacement characters (�)
- Non-ASCII characters display incorrectly
- Mixed encoding in output
- BOM markers visible in output

**Causes:**
- Wrong encoding detected or specified
- Process uses different codepage than UTF-8
- Binary content mixed with text
- Multi-byte sequences split across buffer reads

**Solutions:**
1. Specify correct encoding explicitly:
   ```csharp
   var options = new ExecutionOptions
   {
       OutputEncoding = Encoding.GetEncoding("windows-1252")
   };
   ```

2. Set environment to force UTF-8:
   ```csharp
   var command = Command.Create("git")
       .WithEnvironment("LC_ALL", "en_US.UTF-8")
       .WithEnvironment("LANG", "en_US.UTF-8")
       .Build();
   ```

3. Check for and skip BOM:
   ```csharp
   var output = result.Stdout;
   if (output.StartsWith("\uFEFF")) // UTF-8 BOM
       output = output[1..];
   ```

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

## Best Practices

### Output Capture

1. **Stream, don't buffer entirely** - Process output incrementally to handle large outputs
2. **Separate stdout and stderr** - Keep streams distinct for proper error handling
3. **Use async readers** - Non-blocking I/O prevents deadlocks with child processes
4. **Handle encoding properly** - Detect and handle UTF-8, UTF-16, and console encodings

### Timeout Management

5. **Kill process tree** - Terminate child processes when parent times out
6. **Grace period before force kill** - Give processes time to clean up
7. **Log timeout context** - Record what was running when timeout occurred
8. **Make timeouts configurable** - Different operations need different limits

### Exit Code Handling

9. **Preserve original exit code** - Don't mask real exit codes with wrapper errors
10. **Handle special codes** - Exit codes >128 often indicate signals
11. **Log exit codes consistently** - Include in all command execution logs
12. **Define expected codes** - Document what exit codes mean for each command

---

## Testing Requirements

### Complete Test Implementations

#### OutputCaptureTests.cs

```csharp
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Acode.Infrastructure.Execution;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for stdout/stderr capture functionality.
/// </summary>
public class OutputCaptureTests
{
    [Fact]
    public async Task OutputCapture_CapturesStdout_WhenCommandProducesOutput()
    {
        // Arrange
        var capture = new OutputCapture();
        var process = CreateMockProcess(stdout: "Hello, World!\n");
        
        // Act
        var result = await capture.CaptureAsync(process);
        
        // Assert
        result.Stdout.Should().Be("Hello, World!\n");
        result.StdoutBytes.Should().Be(14);
        result.StdoutTruncated.Should().BeFalse();
    }
    
    [Fact]
    public async Task OutputCapture_CapturesStderr_WhenCommandProducesErrors()
    {
        // Arrange
        var capture = new OutputCapture();
        var process = CreateMockProcess(stderr: "Error: File not found\n");
        
        // Act
        var result = await capture.CaptureAsync(process);
        
        // Assert
        result.Stderr.Should().Be("Error: File not found\n");
        result.StderrBytes.Should().Be(22);
        result.StderrTruncated.Should().BeFalse();
    }
    
    [Fact]
    public async Task OutputCapture_CapturesBothStreams_Concurrently()
    {
        // Arrange
        var capture = new OutputCapture();
        var stdout = new string('O', 100000); // 100KB stdout
        var stderr = new string('E', 100000); // 100KB stderr
        var process = CreateMockProcess(stdout: stdout, stderr: stderr);
        
        // Act
        var result = await capture.CaptureAsync(process);
        
        // Assert - Both should be captured without deadlock
        result.Stdout.Length.Should().Be(100000);
        result.Stderr.Length.Should().Be(100000);
    }
    
    [Fact]
    public async Task OutputCapture_PreventsDeadlock_WhenBothStreamsProduceLargeOutput()
    {
        // Arrange
        var capture = new OutputCapture();
        var largeOutput = new string('X', 1024 * 1024); // 1MB each
        var process = CreateMockProcess(stdout: largeOutput, stderr: largeOutput);
        
        // Act - Should complete, not deadlock
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result = await capture.CaptureAsync(process, cts.Token);
        
        // Assert
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task OutputCapture_ReturnsEmptyStrings_WhenNoOutput()
    {
        // Arrange
        var capture = new OutputCapture();
        var process = CreateMockProcess(stdout: "", stderr: "");
        
        // Act
        var result = await capture.CaptureAsync(process);
        
        // Assert
        result.Stdout.Should().BeEmpty();
        result.Stderr.Should().BeEmpty();
        result.StdoutBytes.Should().Be(0);
        result.StderrBytes.Should().Be(0);
    }
    
    [Fact]
    public async Task OutputCapture_RespectsMaxStdoutSize_WhenConfigured()
    {
        // Arrange
        var options = new CaptureOptions { MaxStdoutBytes = 1024 };
        var capture = new OutputCapture(options);
        var largeOutput = new string('X', 10000);
        var process = CreateMockProcess(stdout: largeOutput);
        
        // Act
        var result = await capture.CaptureAsync(process);
        
        // Assert
        result.Stdout.Length.Should().BeLessOrEqualTo(1024);
        result.StdoutTruncated.Should().BeTrue();
        result.TruncationInfo.DroppedStdoutBytes.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task OutputCapture_MarksTruncated_WhenOutputExceedsLimit()
    {
        // Arrange
        var options = new CaptureOptions { MaxStdoutBytes = 100 };
        var capture = new OutputCapture(options);
        var process = CreateMockProcess(stdout: new string('A', 500));
        
        // Act
        var result = await capture.CaptureAsync(process);
        
        // Assert
        result.StdoutTruncated.Should().BeTrue();
        result.TruncationInfo.Should().NotBeNull();
        result.TruncationInfo.OriginalStdoutBytes.Should().Be(500);
        result.TruncationInfo.CapturedStdoutBytes.Should().BeLessOrEqualTo(100);
    }
    
    [Theory]
    [InlineData(TruncationMode.Head)]
    [InlineData(TruncationMode.Tail)]
    [InlineData(TruncationMode.HeadAndTail)]
    public async Task OutputCapture_RespectsTruncationMode(TruncationMode mode)
    {
        // Arrange
        var options = new CaptureOptions 
        { 
            MaxStdoutBytes = 100,
            TruncationMode = mode
        };
        var capture = new OutputCapture(options);
        var output = "HEAD" + new string('X', 500) + "TAIL";
        var process = CreateMockProcess(stdout: output);
        
        // Act
        var result = await capture.CaptureAsync(process);
        
        // Assert
        result.StdoutTruncated.Should().BeTrue();
        result.TruncationInfo.Mode.Should().Be(mode);
        
        switch (mode)
        {
            case TruncationMode.Head:
                result.Stdout.Should().StartWith("HEAD");
                break;
            case TruncationMode.Tail:
                result.Stdout.Should().EndWith("TAIL");
                break;
            case TruncationMode.HeadAndTail:
                result.Stdout.Should().Contain("...");
                break;
        }
    }
    
    [Fact]
    public async Task OutputCapture_HandlesCancellation_Gracefully()
    {
        // Arrange
        var capture = new OutputCapture();
        var slowProcess = CreateSlowMockProcess(delayMs: 5000);
        var cts = new CancellationTokenSource(100);
        
        // Act
        var act = () => capture.CaptureAsync(slowProcess, cts.Token);
        
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    
    private IProcess CreateMockProcess(string stdout = "", string stderr = "")
    {
        return new MockProcess(stdout, stderr);
    }
    
    private IProcess CreateSlowMockProcess(int delayMs)
    {
        return new SlowMockProcess(delayMs);
    }
}
```

#### ExitCodeHandlerTests.cs

```csharp
using System;
using System.Threading.Tasks;
using Acode.Infrastructure.Execution;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for exit code handling and interpretation.
/// </summary>
public class ExitCodeHandlerTests
{
    [Fact]
    public async Task ExitCodeHandler_ReturnsZero_ForSuccessfulCommand()
    {
        // Arrange
        var handler = new ExitCodeHandler();
        var process = MockProcess.WithExitCode(0);
        
        // Act
        var exitCode = await handler.GetExitCodeAsync(process);
        
        // Assert
        exitCode.Should().Be(0);
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(127)]
    [InlineData(255)]
    public async Task ExitCodeHandler_ReturnsNonZero_ForFailedCommand(int expectedExitCode)
    {
        // Arrange
        var handler = new ExitCodeHandler();
        var process = MockProcess.WithExitCode(expectedExitCode);
        
        // Act
        var exitCode = await handler.GetExitCodeAsync(process);
        
        // Assert
        exitCode.Should().Be(expectedExitCode);
    }
    
    [Fact]
    public async Task ExitCodeHandler_Returns127_ForCommandNotFound()
    {
        // Arrange - 127 is standard "command not found"
        var handler = new ExitCodeHandler();
        var process = MockProcess.WithExitCode(127);
        
        // Act
        var exitCode = await handler.GetExitCodeAsync(process);
        var interpretation = handler.Interpret(exitCode);
        
        // Assert
        exitCode.Should().Be(127);
        interpretation.Category.Should().Be(ExitCodeCategory.CommandNotFound);
    }
    
    [Fact]
    public async Task ExitCodeHandler_Returns137_ForKilledProcess()
    {
        // Arrange - 137 = 128 + 9 (SIGKILL)
        var handler = new ExitCodeHandler();
        var process = MockProcess.WithExitCode(137);
        
        // Act
        var exitCode = await handler.GetExitCodeAsync(process);
        var interpretation = handler.Interpret(exitCode);
        
        // Assert
        exitCode.Should().Be(137);
        interpretation.WasKilled.Should().BeTrue();
        interpretation.Signal.Should().Be(9);
    }
    
    [Fact]
    public async Task ExitCodeHandler_Returns143_ForTerminatedProcess()
    {
        // Arrange - 143 = 128 + 15 (SIGTERM)
        var handler = new ExitCodeHandler();
        var process = MockProcess.WithExitCode(143);
        
        // Act
        var exitCode = await handler.GetExitCodeAsync(process);
        var interpretation = handler.Interpret(exitCode);
        
        // Assert
        exitCode.Should().Be(143);
        interpretation.WasTerminated.Should().BeTrue();
        interpretation.Signal.Should().Be(15);
    }
    
    [Fact]
    public void ExitCodeHandler_MapsToSuccess_WhenZero()
    {
        // Arrange
        var handler = new ExitCodeHandler();
        
        // Act
        var result = handler.IsSuccess(0);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(255)]
    public void ExitCodeHandler_MapsToFailure_WhenNonZero(int exitCode)
    {
        // Arrange
        var handler = new ExitCodeHandler();
        
        // Act
        var result = handler.IsSuccess(exitCode);
        
        // Assert
        result.Should().BeFalse();
    }
}
```

#### TimeoutHandlerTests.cs

```csharp
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Acode.Infrastructure.Execution;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for command timeout handling.
/// </summary>
public class TimeoutHandlerTests
{
    [Fact]
    public async Task TimeoutHandler_AllowsCompletion_BeforeTimeout()
    {
        // Arrange
        var handler = new TimeoutHandler(timeout: TimeSpan.FromSeconds(10));
        var fastProcess = MockProcess.ThatExitsAfter(TimeSpan.FromMilliseconds(100));
        
        // Act
        var result = await handler.WaitWithTimeoutAsync(fastProcess);
        
        // Assert
        result.TimedOut.Should().BeFalse();
        result.CompletedNormally.Should().BeTrue();
    }
    
    [Fact]
    public async Task TimeoutHandler_SendsSigint_OnTimeout()
    {
        // Arrange
        var handler = new TimeoutHandler(
            timeout: TimeSpan.FromMilliseconds(100),
            gracePeriod: TimeSpan.FromSeconds(1));
        var slowProcess = MockProcess.ThatNeverExits();
        
        // Act
        await handler.WaitWithTimeoutAsync(slowProcess);
        
        // Assert
        slowProcess.ReceivedInterrupt.Should().BeTrue();
    }
    
    [Fact]
    public async Task TimeoutHandler_WaitsGracePeriod_AfterSigint()
    {
        // Arrange
        var gracePeriod = TimeSpan.FromMilliseconds(200);
        var handler = new TimeoutHandler(
            timeout: TimeSpan.FromMilliseconds(50),
            gracePeriod: gracePeriod);
        var slowProcess = MockProcess.ThatExitsAfterSignal(TimeSpan.FromMilliseconds(100));
        
        // Act
        var sw = Stopwatch.StartNew();
        await handler.WaitWithTimeoutAsync(slowProcess);
        sw.Stop();
        
        // Assert - Should have waited for signal response
        sw.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(100));
        slowProcess.WasKilled.Should().BeFalse(); // Exited gracefully
    }
    
    [Fact]
    public async Task TimeoutHandler_SendsSigkill_AfterGracePeriod()
    {
        // Arrange
        var handler = new TimeoutHandler(
            timeout: TimeSpan.FromMilliseconds(50),
            gracePeriod: TimeSpan.FromMilliseconds(100));
        var stubbornProcess = MockProcess.ThatIgnoresSignals();
        
        // Act
        await handler.WaitWithTimeoutAsync(stubbornProcess);
        
        // Assert
        stubbornProcess.ReceivedKill.Should().BeTrue();
    }
    
    [Fact]
    public async Task TimeoutHandler_CapturesPartialOutput_BeforeKill()
    {
        // Arrange
        var handler = new TimeoutHandler(timeout: TimeSpan.FromMilliseconds(100));
        var capture = new OutputCapture();
        var slowProcess = MockProcess.ThatProducesOutputThenHangs("Partial output\n");
        
        // Act
        var result = await handler.ExecuteWithCaptureAsync(slowProcess, capture);
        
        // Assert
        result.Stdout.Should().Contain("Partial output");
        result.TimedOut.Should().BeTrue();
    }
    
    [Fact]
    public async Task TimeoutHandler_MarksResultAsTimedOut_Correctly()
    {
        // Arrange
        var handler = new TimeoutHandler(timeout: TimeSpan.FromMilliseconds(50));
        var slowProcess = MockProcess.ThatNeverExits();
        
        // Act
        var result = await handler.WaitWithTimeoutAsync(slowProcess);
        
        // Assert
        result.TimedOut.Should().BeTrue();
        result.TimeoutInfo.Should().NotBeNull();
        result.TimeoutInfo.ConfiguredTimeout.Should().Be(TimeSpan.FromMilliseconds(50));
    }
    
    [Fact]
    public async Task TimeoutHandler_ReportsActualDuration_Accurately()
    {
        // Arrange
        var handler = new TimeoutHandler(timeout: TimeSpan.FromSeconds(10));
        var process = MockProcess.ThatExitsAfter(TimeSpan.FromMilliseconds(200));
        
        // Act
        var result = await handler.WaitWithTimeoutAsync(process);
        
        // Assert
        result.Duration.Should().BeCloseTo(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(100));
    }
    
    [Fact]
    public async Task TimeoutHandler_KillsProcessTree_NotJustParent()
    {
        // Arrange
        var handler = new TimeoutHandler(timeout: TimeSpan.FromMilliseconds(50));
        var processWithChildren = MockProcess.WithChildProcesses(3);
        
        // Act
        await handler.WaitWithTimeoutAsync(processWithChildren);
        
        // Assert
        processWithChildren.AllChildrenKilled.Should().BeTrue();
    }
    
    [Fact]
    public async Task TimeoutHandler_HandlesZeroTimeout_Correctly()
    {
        // Arrange - Zero timeout means kill immediately
        var handler = new TimeoutHandler(timeout: TimeSpan.Zero);
        var process = MockProcess.ThatNeverExits();
        
        // Act
        var result = await handler.WaitWithTimeoutAsync(process);
        
        // Assert
        result.TimedOut.Should().BeTrue();
        result.Duration.Should().BeLessThan(TimeSpan.FromMilliseconds(100));
    }
    
    [Fact]
    public async Task TimeoutHandler_HandlesInfiniteTimeout_Correctly()
    {
        // Arrange
        var handler = new TimeoutHandler(timeout: Timeout.InfiniteTimeSpan);
        var process = MockProcess.ThatExitsAfter(TimeSpan.FromMilliseconds(100));
        
        // Act
        var result = await handler.WaitWithTimeoutAsync(process);
        
        // Assert
        result.TimedOut.Should().BeFalse();
    }
}
```

#### EncodingDetectorTests.cs

```csharp
using System;
using System.Text;
using Acode.Infrastructure.Execution;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for output encoding detection and handling.
/// </summary>
public class EncodingDetectorTests
{
    [Fact]
    public void EncodingDetector_DetectsUtf8Bom_Correctly()
    {
        // Arrange
        var detector = new EncodingDetector();
        var data = new byte[] { 0xEF, 0xBB, 0xBF, 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // BOM + "Hello"
        
        // Act
        var result = detector.Detect(data);
        
        // Assert
        result.Encoding.Should().Be(Encoding.UTF8);
        result.HasBom.Should().BeTrue();
    }
    
    [Fact]
    public void EncodingDetector_DetectsUtf16LeBom_Correctly()
    {
        // Arrange
        var detector = new EncodingDetector();
        var data = new byte[] { 0xFF, 0xFE, 0x48, 0x00, 0x65, 0x00 }; // LE BOM + "He"
        
        // Act
        var result = detector.Detect(data);
        
        // Assert
        result.Encoding.CodePage.Should().Be(1200); // UTF-16LE
        result.HasBom.Should().BeTrue();
    }
    
    [Fact]
    public void EncodingDetector_DetectsUtf16BeBom_Correctly()
    {
        // Arrange
        var detector = new EncodingDetector();
        var data = new byte[] { 0xFE, 0xFF, 0x00, 0x48, 0x00, 0x65 }; // BE BOM + "He"
        
        // Act
        var result = detector.Detect(data);
        
        // Assert
        result.Encoding.CodePage.Should().Be(1201); // UTF-16BE
        result.HasBom.Should().BeTrue();
    }
    
    [Fact]
    public void EncodingDetector_DefaultsToUtf8_WhenNoBom()
    {
        // Arrange
        var detector = new EncodingDetector();
        var data = Encoding.UTF8.GetBytes("Hello, World!");
        
        // Act
        var result = detector.Detect(data);
        
        // Assert
        result.Encoding.Should().Be(Encoding.UTF8);
        result.HasBom.Should().BeFalse();
    }
    
    [Fact]
    public void EncodingDetector_HandlesInvalidSequences_WithReplacement()
    {
        // Arrange
        var detector = new EncodingDetector();
        var data = new byte[] { 0x48, 0x65, 0x80, 0x81, 0x6C, 0x6F }; // "He" + invalid + "lo"
        
        // Act
        var result = detector.DecodeWithFallback(data);
        
        // Assert
        result.Should().Contain("He");
        result.Should().Contain("lo");
        result.Should().Contain("\uFFFD"); // Replacement character
    }
    
    [Fact]
    public void EncodingDetector_ReportsDetectedEncoding_InResult()
    {
        // Arrange
        var detector = new EncodingDetector();
        var data = Encoding.UTF8.GetBytes("Test");
        
        // Act
        var result = detector.Detect(data);
        
        // Assert
        result.EncodingName.Should().Be("utf-8");
        result.Confidence.Should().Be(EncodingConfidence.High);
    }
    
    [Fact]
    public void EncodingDetector_SupportsOverride_ViaConfiguration()
    {
        // Arrange
        var detector = new EncodingDetector(forcedEncoding: Encoding.Latin1);
        var data = new byte[] { 0x48, 0xE9, 0x6C, 0x6C, 0x6F }; // "Héllo" in Latin1
        
        // Act
        var result = detector.Decode(data);
        
        // Assert
        result.Should().Be("Héllo");
    }
}
```

#### BinaryDetectorTests.cs

```csharp
using System;
using Acode.Infrastructure.Execution;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for binary content detection in command output.
/// </summary>
public class BinaryDetectorTests
{
    [Fact]
    public void BinaryDetector_DetectsNullBytes_AsBinary()
    {
        // Arrange
        var detector = new BinaryOutputDetector();
        var data = "Hello\x00World";
        
        // Act
        var result = detector.IsBinary(data);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public void BinaryDetector_DetectsControlChars_AsBinary()
    {
        // Arrange
        var detector = new BinaryOutputDetector();
        var data = "Hello\x01\x02\x03World";
        
        // Act
        var result = detector.IsBinary(data);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public void BinaryDetector_FlagsAsBinary_WhenDetected()
    {
        // Arrange
        var detector = new BinaryOutputDetector();
        var binaryData = new string('\x00', 100);
        
        // Act
        var info = detector.Analyze(binaryData);
        
        // Assert
        info.IsBinary.Should().BeTrue();
        info.NullByteCount.Should().Be(100);
    }
    
    [Fact]
    public void BinaryDetector_ReturnsHexPreview_ForBinary()
    {
        // Arrange
        var detector = new BinaryOutputDetector();
        var bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0x01 };
        
        // Act
        var hexDump = detector.ToHexDump(bytes);
        
        // Assert
        hexDump.Should().Contain("48 65 6C 6C 6F 00 01");
        hexDump.Should().StartWith("[BINARY OUTPUT");
    }
    
    [Fact]
    public void BinaryDetector_DoesNotFlagText_AsBinary()
    {
        // Arrange
        var detector = new BinaryOutputDetector();
        var textData = "Hello, World!\nThis is text with newlines\tand tabs.";
        
        // Act
        var result = detector.IsBinary(textData);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public void BinaryDetector_Sanitizes_RemovesDangerousSequences()
    {
        // Arrange
        var detector = new BinaryOutputDetector();
        var dataWithEscape = "Hello\x1B[31mRed\x1B[0mWorld";
        
        // Act
        var sanitized = detector.Sanitize(dataWithEscape);
        
        // Assert
        sanitized.Should().Contain("Hello");
        sanitized.Should().Contain("World");
        sanitized.Should().NotContain("\x1B");
    }
}
```

#### Integration Tests

```csharp
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Acode.Infrastructure.Execution;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Execution;

/// <summary>
/// Integration tests that run real commands.
/// </summary>
[Collection("IntegrationTests")]
public class CaptureIntegrationTests
{
    [Fact]
    public async Task Capture_RealCommand_ReturnsCorrectOutput()
    {
        // Arrange
        var executor = new CommandExecutor();
        var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Command.Create("cmd", "/c", "echo", "Hello")
            : Command.Create("echo", "Hello");
        
        // Act
        var result = await executor.ExecuteAsync(command);
        
        // Assert
        result.Stdout.Trim().Should().Be("Hello");
        result.ExitCode.Should().Be(0);
    }
    
    [Fact]
    public async Task Capture_CommandWithLargeOutput_HandlesCorrectly()
    {
        // Arrange
        var executor = new CommandExecutor();
        var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Command.Create("cmd", "/c", "dir", "/s", "c:\\windows\\system32")
            : Command.Create("find", "/usr", "-type", "f");
        
        // Act
        var result = await executor.ExecuteAsync(command, TimeSpan.FromMinutes(1));
        
        // Assert
        result.StdoutBytes.Should().BeGreaterThan(10000);
        result.Stdout.Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task Capture_CommandThatFails_CapturesStderr()
    {
        // Arrange
        var executor = new CommandExecutor();
        var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Command.Create("cmd", "/c", "dir", "nonexistent_path_12345")
            : Command.Create("ls", "nonexistent_path_12345");
        
        // Act
        var result = await executor.ExecuteAsync(command);
        
        // Assert
        result.ExitCode.Should().NotBe(0);
        (result.Stderr.Length > 0 || result.Stdout.Length > 0).Should().BeTrue();
    }
    
    [SkippableFact]
    public async Task Capture_CommandThatHangs_TimesOutCorrectly()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Linux), "Linux-specific test");
        
        // Arrange
        var executor = new CommandExecutor();
        var command = Command.Create("sleep", "60");
        
        // Act
        var result = await executor.ExecuteAsync(command, TimeSpan.FromMilliseconds(500));
        
        // Assert
        result.TimedOut.Should().BeTrue();
        result.Duration.Should().BeCloseTo(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(200));
    }
    
    [Fact]
    public async Task Capture_ConcurrentCommands_AllCapturedCorrectly()
    {
        // Arrange
        var executor = new CommandExecutor();
        var commands = new[]
        {
            Command.Create("echo", "One"),
            Command.Create("echo", "Two"),
            Command.Create("echo", "Three")
        };
        
        // Act
        var tasks = Array.ConvertAll(commands, c => executor.ExecuteAsync(c));
        var results = await Task.WhenAll(tasks);
        
        // Assert
        results.Should().AllSatisfy(r => r.ExitCode.Should().Be(0));
        results[0].Stdout.Should().Contain("One");
        results[1].Stdout.Should().Contain("Two");
        results[2].Stdout.Should().Contain("Three");
    }
}
```

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