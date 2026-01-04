# Task 042.a: Persist Prompts/Settings (Redacted)

**Priority:** P1 – High  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 10 – Core Reliability  
**Dependencies:** Task 042 (Reproducibility), Task 038 (Secrets), Task 024 (Local LLM)  

---

## Description

Task 042.a implements persistence of prompts and settings with mandatory redaction. Every prompt sent to the local LLM, every response received, and every configuration setting used during a session MUST be captured and stored in redacted form. This data is essential for debugging, auditing, and replay.

Prompt persistence captures the exact input sent to the model, including system prompts, user messages, and any context provided. Response persistence captures the model's output, including structured data and any metadata. Settings persistence captures configuration values that affect behavior.

All persisted data MUST be redacted before storage. The redaction system (Task 038) scans for secrets, PII, and sensitive patterns, replacing them with markers. This ensures captured artifacts can be safely stored, shared, and analyzed without exposing sensitive information.

The persistence layer integrates with the capture service (Task 042) to stream data as it's generated. This minimizes memory usage and ensures data is captured even if the session crashes mid-execution.

### Business Value

Prompt/settings persistence provides:
- Full session history
- Debugging capability
- Audit trail
- Behavior understanding
- Replay foundation

### Scope Boundaries

This task covers prompt and settings persistence. The capture framework is Task 042. Deterministic mode is Task 042.b. Replay tooling is Task 042.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Capture Service | Task 042 | Storage target | Core |
| Secrets | Task 038 | Redaction | Mandatory |
| Local LLM | Task 024 | Prompt source | Input |
| Config | Task 002 | Settings source | Input |
| Event Log | Task 040 | Correlation | ID link |
| Replay | Task 042.c | Data source | Output |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Redaction miss | Post-scan | Alert + delete | Security risk |
| Large prompt | Size check | Truncate + warn | Partial capture |
| Persistence fail | IOException | Retry | Gap in history |
| Corrupt data | Checksum | Error | Lost data |
| Missing metadata | Validation | Add defaults | Degraded |
| Format mismatch | Version check | Migrate | Compatibility |
| Disk full | IOException | Warn | No capture |
| Concurrent write | Lock | Serialize | None |

### Assumptions

1. **Prompts are text**: Or serializable
2. **Responses are text**: Or serializable
3. **Redaction works**: Task 038
4. **Storage available**: Task 042
5. **Size reasonable**: <10MB typical
6. **Streaming works**: For large
7. **Metadata available**: From LLM
8. **Config is serializable**: YAML

### Security Considerations

1. **Redaction mandatory**: Non-bypassable
2. **Post-storage scan**: Defense-in-depth
3. **Sensitive config excluded**: Explicitly
4. **No model credentials**: Never captured
5. **Prompt review**: Before sharing
6. **Encrypted storage**: Optional
7. **Access control**: Restricted
8. **Audit of access**: Tracked

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Prompt | Input to LLM |
| Response | Output from LLM |
| Settings | Configuration values |
| Redaction | Remove secrets |
| Persistence | Store durably |
| Capture Event | Single recorded item |
| Marker | Redaction placeholder |
| Metadata | Data about data |
| Correlation ID | Links to event log |
| Truncation | Shorten large data |

---

## Out of Scope

- Binary model weights
- Model internal state
- Token-level capture
- Attention visualization
- Prompt optimization
- Response analytics

---

## Functional Requirements

### FR-001 to FR-020: Prompt Persistence

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042a-01 | System prompt MUST be captured | P0 |
| FR-042a-02 | User message MUST be captured | P0 |
| FR-042a-03 | Context MUST be captured | P0 |
| FR-042a-04 | Full prompt MUST be assembled | P0 |
| FR-042a-05 | Prompt MUST include timestamp | P0 |
| FR-042a-06 | Prompt MUST include model name | P0 |
| FR-042a-07 | Prompt MUST include parameters | P0 |
| FR-042a-08 | Parameters MUST include temperature | P0 |
| FR-042a-09 | Parameters MUST include max tokens | P0 |
| FR-042a-10 | Prompt MUST be redacted | P0 |
| FR-042a-11 | Redaction MUST be before storage | P0 |
| FR-042a-12 | Redacted areas MUST be marked | P0 |
| FR-042a-13 | Prompt MUST have sequence number | P0 |
| FR-042a-14 | Sequence MUST match event log | P0 |
| FR-042a-15 | Prompt MUST include token count | P1 |
| FR-042a-16 | Large prompt MUST use file ref | P0 |
| FR-042a-17 | Size threshold MUST be configurable | P1 |
| FR-042a-18 | Default threshold MUST be 1MB | P1 |
| FR-042a-19 | Prompt format MUST be JSON | P0 |
| FR-042a-20 | Format MUST be versioned | P0 |

### FR-021 to FR-040: Response Persistence

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042a-21 | Response text MUST be captured | P0 |
| FR-042a-22 | Structured output MUST be captured | P0 |
| FR-042a-23 | Response MUST include timestamp | P0 |
| FR-042a-24 | Response MUST include duration | P0 |
| FR-042a-25 | Duration MUST be in milliseconds | P0 |
| FR-042a-26 | Response MUST include token count | P1 |
| FR-042a-27 | Response MUST be linked to prompt | P0 |
| FR-042a-28 | Link MUST use sequence number | P0 |
| FR-042a-29 | Response MUST be redacted | P0 |
| FR-042a-30 | Redaction MUST be before storage | P0 |
| FR-042a-31 | Response MUST include model version | P1 |
| FR-042a-32 | Response MUST include finish reason | P0 |
| FR-042a-33 | Finish reason MUST be captured | P0 |
| FR-042a-34 | Large response MUST use file ref | P0 |
| FR-042a-35 | Response format MUST match prompt | P0 |
| FR-042a-36 | Partial response MUST be marked | P0 |
| FR-042a-37 | Streaming response MUST be captured | P1 |
| FR-042a-38 | Streaming MUST be reconstructed | P1 |
| FR-042a-39 | Error response MUST be captured | P0 |
| FR-042a-40 | Error MUST include details | P0 |

### FR-041 to FR-055: Settings Persistence

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042a-41 | Config values MUST be captured | P0 |
| FR-042a-42 | Capture MUST be at session start | P0 |
| FR-042a-43 | Changes MUST be captured | P1 |
| FR-042a-44 | Change timestamp MUST be recorded | P1 |
| FR-042a-45 | Sensitive settings MUST be excluded | P0 |
| FR-042a-46 | Exclusion list MUST be configurable | P0 |
| FR-042a-47 | Default exclusions MUST exist | P0 |
| FR-042a-48 | Settings MUST be redacted | P0 |
| FR-042a-49 | Redacted settings MUST show marker | P0 |
| FR-042a-50 | Settings format MUST be JSON | P0 |
| FR-042a-51 | Settings MUST include source | P0 |
| FR-042a-52 | Source MUST indicate file/env/default | P0 |
| FR-042a-53 | Settings MUST include version | P0 |
| FR-042a-54 | Version MUST be app version | P0 |
| FR-042a-55 | Environment MUST be summarized | P1 |

### FR-056 to FR-065: Storage Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042a-56 | Data MUST be streamed to storage | P0 |
| FR-042a-57 | Storage MUST use Task 042 | P0 |
| FR-042a-58 | Each item MUST have unique ID | P0 |
| FR-042a-59 | ID MUST correlate to event log | P0 |
| FR-042a-60 | Batch write MUST be supported | P1 |
| FR-042a-61 | Batch MUST be atomic | P1 |
| FR-042a-62 | Write failure MUST be logged | P0 |
| FR-042a-63 | Failure MUST NOT block execution | P0 |
| FR-042a-64 | Gap MUST be marked in capture | P0 |
| FR-042a-65 | Recovery MUST be attempted | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-042a-01 | Prompt capture | <5ms | P0 |
| NFR-042a-02 | Response capture | <5ms | P0 |
| NFR-042a-03 | Settings capture | <10ms | P0 |
| NFR-042a-04 | Redaction (1KB) | <10ms | P0 |
| NFR-042a-05 | Redaction (1MB) | <100ms | P1 |
| NFR-042a-06 | File ref write | <50ms | P1 |
| NFR-042a-07 | Memory overhead | <20MB | P2 |
| NFR-042a-08 | Streaming latency | <10ms | P1 |
| NFR-042a-09 | Batch write (100) | <100ms | P2 |
| NFR-042a-10 | No blocking | Main thread | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-042a-11 | Capture complete | 99.9% | P0 |
| NFR-042a-12 | Redaction complete | 100% | P0 |
| NFR-042a-13 | No data loss | 99.9% | P0 |
| NFR-042a-14 | Correlation correct | 100% | P0 |
| NFR-042a-15 | Format valid | 100% | P0 |
| NFR-042a-16 | Cross-platform | All OS | P0 |
| NFR-042a-17 | Unicode support | Full | P0 |
| NFR-042a-18 | Large data support | >100MB | P1 |
| NFR-042a-19 | Thread safety | No races | P0 |
| NFR-042a-20 | Non-blocking | Always | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-042a-21 | Capture logged | Debug | P1 |
| NFR-042a-22 | Redaction logged | Debug | P1 |
| NFR-042a-23 | Error logged | Error | P0 |
| NFR-042a-24 | Size logged | Debug | P2 |
| NFR-042a-25 | Metrics: prompts | Counter | P2 |
| NFR-042a-26 | Metrics: responses | Counter | P2 |
| NFR-042a-27 | Metrics: bytes | Counter | P2 |
| NFR-042a-28 | Structured logging | JSON | P0 |
| NFR-042a-29 | Correlation ID | In logs | P0 |
| NFR-042a-30 | Duration logged | Debug | P1 |

---

## Acceptance Criteria / Definition of Done

### Prompt Persistence
- [ ] AC-001: System prompt captured
- [ ] AC-002: User message captured
- [ ] AC-003: Context captured
- [ ] AC-004: Timestamp included
- [ ] AC-005: Model name included
- [ ] AC-006: Parameters included
- [ ] AC-007: Redacted
- [ ] AC-008: Sequence number

### Response Persistence
- [ ] AC-009: Text captured
- [ ] AC-010: Structured output captured
- [ ] AC-011: Duration included
- [ ] AC-012: Linked to prompt
- [ ] AC-013: Redacted
- [ ] AC-014: Finish reason included
- [ ] AC-015: Large uses file ref
- [ ] AC-016: Error captured

### Settings Persistence
- [ ] AC-017: Config captured
- [ ] AC-018: At session start
- [ ] AC-019: Sensitive excluded
- [ ] AC-020: Redacted
- [ ] AC-021: Source indicated
- [ ] AC-022: Version included
- [ ] AC-023: Changes tracked
- [ ] AC-024: JSON format

### Storage Integration
- [ ] AC-025: Streamed
- [ ] AC-026: Uses Task 042
- [ ] AC-027: Unique ID
- [ ] AC-028: Correlated
- [ ] AC-029: Non-blocking
- [ ] AC-030: Errors logged
- [ ] AC-031: Gaps marked
- [ ] AC-032: Recovery attempted

---

## User Verification Scenarios

### Scenario 1: Prompt Capture
**Persona:** Developer debugging  
**Preconditions:** Capture enabled  
**Steps:**
1. Send prompt to LLM
2. Prompt captured
3. View in artifact
4. Verify content

**Verification Checklist:**
- [ ] Prompt visible
- [ ] Metadata correct
- [ ] Redacted
- [ ] Sequence matches

### Scenario 2: Response Capture
**Persona:** Developer debugging  
**Preconditions:** Capture enabled  
**Steps:**
1. Receive response
2. Response captured
3. View in artifact
4. Linked to prompt

**Verification Checklist:**
- [ ] Response visible
- [ ] Duration correct
- [ ] Link works
- [ ] Redacted

### Scenario 3: Settings Capture
**Persona:** Developer understanding config  
**Preconditions:** Capture enabled  
**Steps:**
1. Start session
2. Settings captured
3. View in artifact
4. Sources shown

**Verification Checklist:**
- [ ] Config visible
- [ ] Sources correct
- [ ] Sensitive excluded
- [ ] Version present

### Scenario 4: Large Prompt Handling
**Persona:** Developer with big context  
**Preconditions:** Prompt > 1MB  
**Steps:**
1. Send large prompt
2. File ref created
3. Reference stored
4. Data accessible

**Verification Checklist:**
- [ ] File ref used
- [ ] Reference works
- [ ] Size reasonable
- [ ] Performance ok

### Scenario 5: Redaction Verification
**Persona:** Developer checking security  
**Preconditions:** Secrets in prompt  
**Steps:**
1. Include secret in prompt
2. Capture occurs
3. Search artifact
4. No secrets

**Verification Checklist:**
- [ ] Secret in input
- [ ] Marker in output
- [ ] Search clean
- [ ] Logged

### Scenario 6: Error Response Capture
**Persona:** Developer debugging failure  
**Preconditions:** LLM error occurs  
**Steps:**
1. LLM returns error
2. Error captured
3. View in artifact
4. Details present

**Verification Checklist:**
- [ ] Error captured
- [ ] Details included
- [ ] Linked correctly
- [ ] Useful for debug

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-042a-01 | Prompt capture | FR-042a-01 |
| UT-042a-02 | Response capture | FR-042a-21 |
| UT-042a-03 | Settings capture | FR-042a-41 |
| UT-042a-04 | Prompt redaction | FR-042a-10 |
| UT-042a-05 | Response redaction | FR-042a-29 |
| UT-042a-06 | Settings redaction | FR-042a-48 |
| UT-042a-07 | Large prompt ref | FR-042a-16 |
| UT-042a-08 | Prompt-response link | FR-042a-27 |
| UT-042a-09 | Sequence number | FR-042a-13 |
| UT-042a-10 | Settings exclusion | FR-042a-45 |
| UT-042a-11 | JSON format | FR-042a-19 |
| UT-042a-12 | Timestamp | FR-042a-05 |
| UT-042a-13 | Duration | FR-042a-24 |
| UT-042a-14 | Error capture | FR-042a-39 |
| UT-042a-15 | Thread safety | NFR-042a-19 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-042a-01 | Full capture flow | E2E |
| IT-042a-02 | Task 042 integration | FR-042a-57 |
| IT-042a-03 | Task 038 integration | FR-042a-11 |
| IT-042a-04 | LLM integration | Task 024 |
| IT-042a-05 | Config integration | Task 002 |
| IT-042a-06 | Event log correlation | FR-042a-59 |
| IT-042a-07 | Large prompt | FR-042a-16 |
| IT-042a-08 | Large response | FR-042a-34 |
| IT-042a-09 | Streaming | FR-042a-37 |
| IT-042a-10 | Performance | NFR-042a-01 |
| IT-042a-11 | Cross-platform | NFR-042a-16 |
| IT-042a-12 | Non-blocking | NFR-042a-20 |
| IT-042a-13 | Logging | NFR-042a-21 |
| IT-042a-14 | Batch write | FR-042a-60 |
| IT-042a-15 | Error recovery | FR-042a-65 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Reproducibility/
│       ├── PromptCapture.cs
│       ├── ResponseCapture.cs
│       └── SettingsCapture.cs
├── Acode.Application/
│   └── Reproducibility/
│       ├── IPromptCaptureService.cs
│       └── CaptureContext.cs
├── Acode.Infrastructure/
│   └── Reproducibility/
│       ├── PromptCaptureService.cs
│       └── SettingsCaptureService.cs
```

### Capture Schema

```json
{
  "type": "prompt",
  "version": "1.0",
  "id": "01HX...",
  "sessionId": "01HX...",
  "sequence": 42,
  "timestamp": "2024-01-15T10:30:00Z",
  "model": "mistral-7b",
  "parameters": {
    "temperature": 0.7,
    "maxTokens": 2048
  },
  "content": {
    "system": "You are a coding assistant...",
    "user": "Write a function that [REDACTED]",
    "context": ["file1.cs", "file2.cs"]
  },
  "tokenCount": 1234,
  "redactionCount": 3
}
```

### Key Implementation

```csharp
public class PromptCaptureService : IPromptCaptureService
{
    public async Task CapturePromptAsync(LlmPrompt prompt, CaptureContext ctx)
    {
        if (!_options.Enabled)
            return;
        
        var capture = new PromptCapture
        {
            Id = Ulid.NewUlid().ToString(),
            SessionId = ctx.SessionId,
            Sequence = ctx.NextSequence(),
            Timestamp = DateTime.UtcNow,
            Model = prompt.ModelName,
            Parameters = prompt.Parameters,
            Content = await RedactContentAsync(prompt)
        };
        
        if (capture.GetSize() > _options.FileSizeThreshold)
        {
            capture = await WriteToFileRefAsync(capture);
        }
        
        await _store.AppendAsync(capture);
        
        _logger.LogDebug("Captured prompt {Id}, sequence {Seq}", 
            capture.Id, capture.Sequence);
    }
    
    private async Task<PromptContent> RedactContentAsync(LlmPrompt prompt)
    {
        return new PromptContent
        {
            System = await _redactor.RedactAsync(prompt.SystemPrompt),
            User = await _redactor.RedactAsync(prompt.UserMessage),
            Context = prompt.ContextFiles // File names only, not content
        };
    }
}
```

**End of Task 042.a Specification**
