# Task 039.a: Record Tool Calls/Commands/Diffs/Models/Prompts

**Priority:** P0 – Critical  
**Tier:** L – Feature Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 9 – Safety & Compliance  
**Dependencies:** Task 039, Task 050, Task 038, Task 018  

---

## Description

Task 039.a implements the specific audit event recording for tool calls, command executions, file diffs, model interactions, and prompt content. This is the data capture layer that feeds the audit trail system.

**Update:** Recording MUST write structured audit events into the Workspace DB with:
- event_id (ULID/UUID), timestamps, tool name, inputs/outputs references
- redaction status + hash
- correlation with parent task/session

Every operation type has a specific event schema optimized for that category while maintaining compatibility with the common audit event structure.

### Business Value

Detailed recording provides:
- Complete operation traceability
- Debugging capability
- Compliance evidence
- Reproducibility data
- Accountability records

### Scope Boundaries

This task covers specific event schemas and recording. Core audit engine is 039. Export bundle is 039.b. Secret verification is 039.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Audit Recorder | Task 039 | Event write | Core system |
| Workspace DB | Task 050 | Storage | Events table |
| Tool Executor | Task 020 | Tool events | Intercept all |
| Command Runner | Task 021 | Command events | All executions |
| Git Operations | Task 018 | Diff events | All changes |
| Model Adapter | Task 044 | Model events | Prompts/responses |
| Secret Scanner | Task 038 | Redaction | Before record |
| Artifact Store | Task 021.c | Large blobs | Diffs, outputs |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Event write fail | Exception | Retry | Warn user |
| Large output | Size check | Artifact ref | Normal |
| Redaction fail | Exception | Block record | Error |
| Serialization fail | Exception | Log raw | Warn |
| DB timeout | Watchdog | Retry | Delayed |
| Correlation lost | Missing ID | Generate | Warning |
| Schema mismatch | Validation | Log error | Record fails |
| Performance impact | Timer | Log warn | Slower |

### Assumptions

1. **All operations captured**: No exceptions
2. **Event schemas defined**: Per type
3. **Redaction first**: Before any record
4. **Large data → artifacts**: Reference only
5. **Performance acceptable**: <50ms overhead
6. **Correlation maintained**: Trace through
7. **Timestamps accurate**: NTP-synced
8. **IDs unique**: ULID/UUID

### Security Considerations

1. **Pre-record redaction**: Always
2. **No raw secrets**: Ever
3. **Input redaction**: User data
4. **Output redaction**: Tool results
5. **Prompt redaction**: May contain secrets
6. **Diff redaction**: File changes
7. **Hash for verification**: Integrity
8. **Audit of recording**: Meta-events

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Tool Call Event | Record of tool invocation |
| Command Event | Record of shell execution |
| Diff Event | Record of file changes |
| Model Event | Record of LLM interaction |
| Prompt Event | Record of input to model |
| Response Event | Record of model output |
| Artifact Reference | Pointer to large blob |
| Event Hash | Integrity checksum |
| Correlation ID | Links related events |

---

## Out of Scope

- Event analytics
- Event alerting
- Real-time streaming
- External event sinks
- Event transformation

---

## Functional Requirements

### FR-001 to FR-020: Tool Call Recording

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039A-01 | All tool calls MUST be recorded | P0 |
| FR-039A-02 | Tool name MUST be recorded | P0 |
| FR-039A-03 | Tool input MUST be recorded | P0 |
| FR-039A-04 | Tool output MUST be recorded | P0 |
| FR-039A-05 | Tool duration MUST be recorded | P0 |
| FR-039A-06 | Tool outcome MUST be recorded | P0 |
| FR-039A-07 | Tool error MUST be recorded | P0 |
| FR-039A-08 | Tool correlation ID MUST be included | P0 |
| FR-039A-09 | Tool timestamp MUST be recorded | P0 |
| FR-039A-10 | Input MUST be redacted | P0 |
| FR-039A-11 | Output MUST be redacted | P0 |
| FR-039A-12 | Large input MUST use artifact | P0 |
| FR-039A-13 | Large output MUST use artifact | P0 |
| FR-039A-14 | Artifact reference MUST be in event | P0 |
| FR-039A-15 | Tool version MUST be recorded | P1 |
| FR-039A-16 | Tool category MUST be recorded | P1 |
| FR-039A-17 | Retry count MUST be recorded | P1 |
| FR-039A-18 | Parent task MUST be recorded | P0 |
| FR-039A-19 | Session ID MUST be recorded | P0 |
| FR-039A-20 | Hash MUST be computed | P0 |

### FR-021 to FR-035: Command Recording

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039A-21 | All commands MUST be recorded | P0 |
| FR-039A-22 | Command line MUST be recorded | P0 |
| FR-039A-23 | Working directory MUST be recorded | P0 |
| FR-039A-24 | Environment MUST NOT be fully recorded | P0 |
| FR-039A-25 | Stdout MUST be recorded | P0 |
| FR-039A-26 | Stderr MUST be recorded | P0 |
| FR-039A-27 | Exit code MUST be recorded | P0 |
| FR-039A-28 | Duration MUST be recorded | P0 |
| FR-039A-29 | Stdout MUST be redacted | P0 |
| FR-039A-30 | Stderr MUST be redacted | P0 |
| FR-039A-31 | Command MUST be redacted | P0 |
| FR-039A-32 | Large stdout MUST use artifact | P0 |
| FR-039A-33 | Large stderr MUST use artifact | P0 |
| FR-039A-34 | PID MUST be recorded | P2 |
| FR-039A-35 | Timeout status MUST be recorded | P1 |

### FR-036 to FR-050: Diff Recording

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039A-36 | All file changes MUST be recorded | P0 |
| FR-039A-37 | File path MUST be recorded | P0 |
| FR-039A-38 | Change type MUST be recorded | P0 |
| FR-039A-39 | Diff content MUST be recorded | P0 |
| FR-039A-40 | Diff MUST be redacted | P0 |
| FR-039A-41 | Large diff MUST use artifact | P0 |
| FR-039A-42 | Before hash MUST be recorded | P1 |
| FR-039A-43 | After hash MUST be recorded | P1 |
| FR-039A-44 | Line count MUST be recorded | P1 |
| FR-039A-45 | Additions MUST be counted | P1 |
| FR-039A-46 | Deletions MUST be counted | P1 |
| FR-039A-47 | Binary flag MUST be recorded | P0 |
| FR-039A-48 | Git commit SHA MUST be recorded if applicable | P1 |
| FR-039A-49 | File mode MUST be recorded | P2 |
| FR-039A-50 | Encoding MUST be recorded | P2 |

### FR-051 to FR-070: Model/Prompt Recording

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-039A-51 | All model calls MUST be recorded | P0 |
| FR-039A-52 | Model ID MUST be recorded | P0 |
| FR-039A-53 | Prompt MUST be recorded | P0 |
| FR-039A-54 | Response MUST be recorded | P0 |
| FR-039A-55 | Token count input MUST be recorded | P1 |
| FR-039A-56 | Token count output MUST be recorded | P1 |
| FR-039A-57 | Duration MUST be recorded | P0 |
| FR-039A-58 | Prompt MUST be redacted | P0 |
| FR-039A-59 | Response MUST be redacted | P0 |
| FR-039A-60 | Large prompt MUST use artifact | P0 |
| FR-039A-61 | Large response MUST use artifact | P0 |
| FR-039A-62 | Temperature MUST be recorded | P2 |
| FR-039A-63 | Max tokens MUST be recorded | P2 |
| FR-039A-64 | Stop reason MUST be recorded | P1 |
| FR-039A-65 | Streaming flag MUST be recorded | P1 |
| FR-039A-66 | Tool use MUST be recorded | P0 |
| FR-039A-67 | System prompt hash MUST be recorded | P0 |
| FR-039A-68 | Context window used MUST be recorded | P1 |
| FR-039A-69 | Cost estimate MUST be recorded | P2 |
| FR-039A-70 | Local vs remote MUST be recorded | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-039A-01 | Tool event write | <20ms | P1 |
| NFR-039A-02 | Command event write | <20ms | P1 |
| NFR-039A-03 | Diff event write | <50ms | P1 |
| NFR-039A-04 | Model event write | <50ms | P1 |
| NFR-039A-05 | Artifact write | <200ms | P2 |
| NFR-039A-06 | Redaction overhead | <10ms | P1 |
| NFR-039A-07 | Total overhead | <100ms | P0 |
| NFR-039A-08 | Batch write | Supported | P2 |
| NFR-039A-09 | Concurrent writes | 50+ | P2 |
| NFR-039A-10 | Memory per event | <1MB | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-039A-11 | All ops recorded | 100% | P0 |
| NFR-039A-12 | No event loss | 100% | P0 |
| NFR-039A-13 | Redaction complete | 100% | P0 |
| NFR-039A-14 | Correlation maintained | 100% | P0 |
| NFR-039A-15 | Schema valid | 100% | P0 |
| NFR-039A-16 | Graceful on error | Always | P0 |
| NFR-039A-17 | Thread safety | No races | P0 |
| NFR-039A-18 | Cross-platform | All OS | P0 |
| NFR-039A-19 | Encoding support | UTF-8 | P0 |
| NFR-039A-20 | Retry on fail | 3 times | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-039A-21 | Event logged | Debug | P1 |
| NFR-039A-22 | Error logged | Error | P0 |
| NFR-039A-23 | Metrics: events | Counter | P2 |
| NFR-039A-24 | Metrics: by type | Counter | P2 |
| NFR-039A-25 | Metrics: size | Histogram | P2 |
| NFR-039A-26 | Events published | EventBus | P1 |
| NFR-039A-27 | Structured logging | JSON | P0 |
| NFR-039A-28 | Correlation in logs | Required | P0 |
| NFR-039A-29 | Tracing support | OpenTelemetry | P2 |
| NFR-039A-30 | Health check | Event write | P1 |

---

## Acceptance Criteria / Definition of Done

### Tool Calls
- [ ] AC-001: All tools recorded
- [ ] AC-002: Name recorded
- [ ] AC-003: Input recorded
- [ ] AC-004: Output recorded
- [ ] AC-005: Duration recorded
- [ ] AC-006: Outcome recorded
- [ ] AC-007: Input redacted
- [ ] AC-008: Output redacted

### Commands
- [ ] AC-009: All commands recorded
- [ ] AC-010: Command line recorded
- [ ] AC-011: Working dir recorded
- [ ] AC-012: Stdout recorded
- [ ] AC-013: Stderr recorded
- [ ] AC-014: Exit code recorded
- [ ] AC-015: Output redacted
- [ ] AC-016: Large uses artifact

### Diffs
- [ ] AC-017: All diffs recorded
- [ ] AC-018: File path recorded
- [ ] AC-019: Change type recorded
- [ ] AC-020: Diff content recorded
- [ ] AC-021: Diff redacted
- [ ] AC-022: Hashes recorded
- [ ] AC-023: Stats recorded
- [ ] AC-024: Large uses artifact

### Model/Prompts
- [ ] AC-025: All model calls recorded
- [ ] AC-026: Model ID recorded
- [ ] AC-027: Prompt recorded
- [ ] AC-028: Response recorded
- [ ] AC-029: Tokens recorded
- [ ] AC-030: Prompt redacted
- [ ] AC-031: Response redacted
- [ ] AC-032: Local/remote recorded

---

## User Verification Scenarios

### Scenario 1: Tool Call Recording
**Persona:** Agent executing tool  
**Preconditions:** Tool invoked  
**Steps:**
1. Tool executes
2. Event created
3. Check DB
4. Verify all fields

**Verification Checklist:**
- [ ] Event exists
- [ ] All fields present
- [ ] Redacted properly
- [ ] Hash computed

### Scenario 2: Command Recording
**Persona:** Agent running command  
**Preconditions:** Command invoked  
**Steps:**
1. Command runs
2. Event created
3. Check output
4. Verify redaction

**Verification Checklist:**
- [ ] Event exists
- [ ] Command recorded
- [ ] Output redacted
- [ ] Exit code present

### Scenario 3: Diff Recording
**Persona:** Agent editing file  
**Preconditions:** File modified  
**Steps:**
1. File changed
2. Diff captured
3. Event created
4. Check content

**Verification Checklist:**
- [ ] Event exists
- [ ] Diff present
- [ ] Secrets redacted
- [ ] Hashes computed

### Scenario 4: Model Recording
**Persona:** Agent using LLM  
**Preconditions:** Model call  
**Steps:**
1. Prompt sent
2. Response received
3. Event created
4. Verify recording

**Verification Checklist:**
- [ ] Event exists
- [ ] Prompt recorded
- [ ] Response recorded
- [ ] Redaction applied

### Scenario 5: Large Output
**Persona:** Agent with big result  
**Preconditions:** Large output  
**Steps:**
1. Tool returns large data
2. Artifact created
3. Reference in event
4. Artifact accessible

**Verification Checklist:**
- [ ] Artifact stored
- [ ] Reference correct
- [ ] Event small
- [ ] Artifact redacted

### Scenario 6: Error Recording
**Persona:** Agent with failure  
**Preconditions:** Tool fails  
**Steps:**
1. Tool throws error
2. Error event created
3. Stack trace recorded
4. Redaction applied

**Verification Checklist:**
- [ ] Error recorded
- [ ] Stack trace present
- [ ] Secrets redacted
- [ ] Context maintained

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-039A-01 | Tool event schema | FR-039A-01 |
| UT-039A-02 | Command event schema | FR-039A-21 |
| UT-039A-03 | Diff event schema | FR-039A-36 |
| UT-039A-04 | Model event schema | FR-039A-51 |
| UT-039A-05 | Input redaction | FR-039A-10 |
| UT-039A-06 | Output redaction | FR-039A-11 |
| UT-039A-07 | Large to artifact | FR-039A-12 |
| UT-039A-08 | Hash computation | FR-039A-20 |
| UT-039A-09 | Correlation ID | FR-039A-08 |
| UT-039A-10 | Timestamp accuracy | FR-039A-09 |
| UT-039A-11 | Error recording | FR-039A-07 |
| UT-039A-12 | Binary flag | FR-039A-47 |
| UT-039A-13 | Token counts | FR-039A-55 |
| UT-039A-14 | Duration recording | FR-039A-05 |
| UT-039A-15 | Thread safety | NFR-039A-17 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-039A-01 | Full tool flow | E2E |
| IT-039A-02 | Full command flow | E2E |
| IT-039A-03 | Full diff flow | E2E |
| IT-039A-04 | Full model flow | E2E |
| IT-039A-05 | Artifact integration | FR-039A-12 |
| IT-039A-06 | DB integration | Task 050 |
| IT-039A-07 | Redaction integration | Task 038 |
| IT-039A-08 | Correlation chain | FR-039A-08 |
| IT-039A-09 | Error handling | NFR-039A-16 |
| IT-039A-10 | Performance | NFR-039A-07 |
| IT-039A-11 | Concurrent writes | NFR-039A-09 |
| IT-039A-12 | Large data handling | FR-039A-12 |
| IT-039A-13 | Retry logic | NFR-039A-20 |
| IT-039A-14 | Cross-platform | NFR-039A-18 |
| IT-039A-15 | Schema validation | NFR-039A-15 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Audit/
│       └── Events/
│           ├── ToolCallEvent.cs
│           ├── CommandEvent.cs
│           ├── DiffEvent.cs
│           └── ModelEvent.cs
├── Acode.Application/
│   └── Audit/
│       └── Recording/
│           ├── IToolCallRecorder.cs
│           ├── ICommandRecorder.cs
│           ├── IDiffRecorder.cs
│           └── IModelRecorder.cs
├── Acode.Infrastructure/
│   └── Audit/
│       └── Recording/
│           ├── ToolCallRecorder.cs
│           ├── CommandRecorder.cs
│           ├── DiffRecorder.cs
│           └── ModelRecorder.cs
```

### Event Schemas

```csharp
public record ToolCallEvent : AuditEvent
{
    public string ToolName { get; init; }
    public string InputHash { get; init; }
    public string? InputArtifactRef { get; init; }
    public string OutputHash { get; init; }
    public string? OutputArtifactRef { get; init; }
    public TimeSpan Duration { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public bool InputRedacted { get; init; }
    public bool OutputRedacted { get; init; }
}

public record CommandEvent : AuditEvent
{
    public string CommandLine { get; init; }  // Redacted
    public string WorkingDirectory { get; init; }
    public int ExitCode { get; init; }
    public string StdoutHash { get; init; }
    public string? StdoutArtifactRef { get; init; }
    public string StderrHash { get; init; }
    public string? StderrArtifactRef { get; init; }
    public TimeSpan Duration { get; init; }
}
```

**End of Task 039.a Specification**
