# Task 042: Reproducibility Knobs

**Priority:** P1 – High  
**Tier:** F – Foundation Layer  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 10 – Core Reliability  
**Dependencies:** Task 040 (Event Log), Task 038 (Secrets), Task 002 (Config)  

---

## Description

Task 042 implements reproducibility knobs—configuration options and tooling that enable deterministic replay of agent sessions. Reproducibility is essential for debugging, testing, auditing, and understanding agent behavior. When something goes wrong, developers need to replay the exact sequence of events to diagnose the issue.

The reproducibility system captures everything needed to recreate a session: prompts sent to models, responses received, tool calls made, file states before and after operations, and configuration settings. All captured data is redacted to remove secrets before storage, ensuring reproducibility artifacts can be safely shared.

Deterministic mode switches control sources of non-determinism. When enabled, the agent uses fixed seeds for random operations, predictable timestamps, and cached responses where appropriate. This allows identical inputs to produce identical outputs, essential for regression testing.

Replay tooling reads captured session data and re-executes the session, comparing actual outputs to recorded outputs. This validates that code changes haven't altered behavior, detects regressions, and provides a debugging aid for complex multi-step operations.

### Business Value

Reproducibility knobs provide:
- Debugging capability
- Regression testing
- Audit verification
- Behavior understanding
- Issue reproduction

### Scope Boundaries

This task covers core reproducibility framework. Prompt/settings persistence is Task 042.a. Deterministic switches are Task 042.b. Replay tooling is Task 042.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Event Log | Task 040 | Recording source | Core |
| Secrets | Task 038 | Redaction | Before storage |
| Config | Task 002 | Settings capture | Persistence |
| Local LLM | Task 024 | Prompt/response | Capture |
| Tool Executor | `IToolExecutor` | Tool calls | Capture |
| File System | I/O | State snapshots | Before/after |
| CLI | Task 000 | Replay commands | User interface |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Incomplete capture | Validation | Warn | Limited replay |
| Redaction miss | Scan | Alert | Security risk |
| Large artifacts | Size check | Compress | Storage |
| Replay divergence | Comparison | Log diff | Investigation |
| Non-determinism | Seed failure | Warn | Flaky replay |
| Corrupt recording | Checksum | Error | Lost data |
| Version mismatch | Header check | Error | Upgrade needed |
| Storage full | IOException | Purge old | Space |

### Assumptions

1. **Event log complete**: Task 040 works
2. **Redaction reliable**: Task 038 works
3. **Local LLM deterministic**: Given seed
4. **Tools are idempotent**: Mostly
5. **File system stable**: For comparison
6. **Storage available**: For artifacts
7. **Replay is offline**: No network
8. **Version tracked**: For compatibility

### Security Considerations

1. **Redaction mandatory**: No raw secrets
2. **Artifacts scanned**: Before export
3. **No model creds in capture**: Only prompts
4. **Replay isolated**: Sandbox
5. **Captured data access control**: Restricted
6. **No network in replay**: Offline only
7. **Sensitive config excluded**: Explicitly
8. **Sharing requires review**: Manual

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Reproducibility | Ability to recreate session |
| Replay | Re-execute from recording |
| Capture | Record session data |
| Redaction | Remove secrets |
| Deterministic Mode | Fixed non-determinism |
| Seed | Random number initializer |
| Snapshot | Point-in-time state |
| Divergence | Replay differs from original |
| Artifact | Stored capture data |
| Session | Single agent run |

---

## Out of Scope

- Distributed replay
- Remote session capture
- Real-time replay
- Replay analytics
- Automatic regression suite
- Production replay

---

## Functional Requirements

### FR-001 to FR-020: Capture Framework

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042-01 | Capture MUST record prompts | P0 |
| FR-042-02 | Capture MUST record responses | P0 |
| FR-042-03 | Capture MUST record tool calls | P0 |
| FR-042-04 | Capture MUST record tool outputs | P0 |
| FR-042-05 | Capture MUST record file states | P0 |
| FR-042-06 | Capture MUST record config | P0 |
| FR-042-07 | Capture MUST be redacted | P0 |
| FR-042-08 | Capture MUST have session ID | P0 |
| FR-042-09 | Capture MUST have timestamp | P0 |
| FR-042-10 | Capture MUST have sequence | P0 |
| FR-042-11 | Capture MUST be optional | P0 |
| FR-042-12 | Capture MUST be configurable | P0 |
| FR-042-13 | Capture MUST be toggleable | P0 |
| FR-042-14 | Default capture MUST be off | P1 |
| FR-042-15 | Capture MUST NOT affect performance significantly | P0 |
| FR-042-16 | Capture MUST be streaming | P1 |
| FR-042-17 | Capture MUST handle large data | P0 |
| FR-042-18 | Large data MUST reference file | P0 |
| FR-042-19 | Capture format MUST be versioned | P0 |
| FR-042-20 | Version MUST be in header | P0 |

### FR-021 to FR-035: Storage

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042-21 | Artifacts MUST be stored locally | P0 |
| FR-042-22 | Storage location MUST be configurable | P0 |
| FR-042-23 | Default location MUST be .agent/recordings | P0 |
| FR-042-24 | Artifact MUST be single file | P1 |
| FR-042-25 | Artifact MUST be compressed | P1 |
| FR-042-26 | Artifact MUST have checksum | P0 |
| FR-042-27 | Checksum MUST be SHA-256 | P0 |
| FR-042-28 | Artifact MUST be portable | P0 |
| FR-042-29 | Artifact MUST include metadata | P0 |
| FR-042-30 | Metadata MUST include version | P0 |
| FR-042-31 | Metadata MUST include platform | P1 |
| FR-042-32 | Purge MUST be available | P1 |
| FR-042-33 | Purge MUST respect retention | P1 |
| FR-042-34 | Retention MUST be configurable | P2 |
| FR-042-35 | Default retention MUST be 30 days | P2 |

### FR-036 to FR-050: Redaction

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042-36 | Prompts MUST be redacted | P0 |
| FR-042-37 | Responses MUST be redacted | P0 |
| FR-042-38 | Tool outputs MUST be redacted | P0 |
| FR-042-39 | Config MUST be redacted | P0 |
| FR-042-40 | File contents MUST be redacted | P0 |
| FR-042-41 | Redaction MUST use Task 038 | P0 |
| FR-042-42 | Redaction MUST be before storage | P0 |
| FR-042-43 | Redacted marker MUST be clear | P0 |
| FR-042-44 | Redaction MUST be logged | P0 |
| FR-042-45 | Post-storage scan MUST occur | P0 |
| FR-042-46 | Scan failure MUST block | P0 |
| FR-042-47 | Sensitive config MUST be excluded | P0 |
| FR-042-48 | Excluded fields MUST be configurable | P1 |
| FR-042-49 | Default exclusions MUST exist | P0 |
| FR-042-50 | Environment vars MUST be excluded | P0 |

### FR-051 to FR-065: Session Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042-51 | Session ID MUST be unique | P0 |
| FR-042-52 | Session ID MUST be ULID | P0 |
| FR-042-53 | Session MUST have start time | P0 |
| FR-042-54 | Session MUST have end time | P0 |
| FR-042-55 | Session MUST have status | P0 |
| FR-042-56 | Status MUST include success/fail | P0 |
| FR-042-57 | Session MUST track event count | P0 |
| FR-042-58 | Session MUST be listable | P0 |
| FR-042-59 | List MUST show metadata | P0 |
| FR-042-60 | Session MUST be deletable | P0 |
| FR-042-61 | Delete MUST be confirmed | P1 |
| FR-042-62 | Session MUST be exportable | P1 |
| FR-042-63 | Export MUST be portable | P1 |
| FR-042-64 | Import MUST validate | P1 |
| FR-042-65 | Import MUST check version | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-042-01 | Capture overhead | <5% | P0 |
| NFR-042-02 | Single event capture | <10ms | P0 |
| NFR-042-03 | Redaction | <50ms | P0 |
| NFR-042-04 | Compression | <100ms/MB | P1 |
| NFR-042-05 | Checksum | <10ms/MB | P1 |
| NFR-042-06 | Session list | <100ms | P1 |
| NFR-042-07 | Artifact size | <10x original | P2 |
| NFR-042-08 | Memory usage | <50MB extra | P2 |
| NFR-042-09 | Disk I/O | Batched | P1 |
| NFR-042-10 | Concurrent capture | Supported | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-042-11 | Capture complete | 100% | P0 |
| NFR-042-12 | No data loss | 100% | P0 |
| NFR-042-13 | Redaction complete | 100% | P0 |
| NFR-042-14 | Checksum valid | 100% | P0 |
| NFR-042-15 | Cross-platform | All OS | P0 |
| NFR-042-16 | Unicode support | Full | P0 |
| NFR-042-17 | Large file support | >1GB | P1 |
| NFR-042-18 | Crash recovery | Partial save | P1 |
| NFR-042-19 | Thread safety | No races | P0 |
| NFR-042-20 | Version compat | Documented | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-042-21 | Capture start logged | Info | P0 |
| NFR-042-22 | Capture end logged | Info | P0 |
| NFR-042-23 | Redaction logged | Debug | P1 |
| NFR-042-24 | Storage logged | Debug | P1 |
| NFR-042-25 | Metrics: captures | Counter | P2 |
| NFR-042-26 | Metrics: size | Gauge | P2 |
| NFR-042-27 | Metrics: events | Counter | P2 |
| NFR-042-28 | Structured logging | JSON | P0 |
| NFR-042-29 | Session metadata | Exported | P1 |
| NFR-042-30 | Health check | Storage test | P2 |

---

## Acceptance Criteria / Definition of Done

### Capture Framework
- [ ] AC-001: Prompts captured
- [ ] AC-002: Responses captured
- [ ] AC-003: Tool calls captured
- [ ] AC-004: File states captured
- [ ] AC-005: Config captured
- [ ] AC-006: Redacted
- [ ] AC-007: Session ID assigned
- [ ] AC-008: Configurable

### Storage
- [ ] AC-009: Local storage works
- [ ] AC-010: Location configurable
- [ ] AC-011: Compressed
- [ ] AC-012: Checksum included
- [ ] AC-013: Portable format
- [ ] AC-014: Versioned
- [ ] AC-015: Purge works
- [ ] AC-016: Retention honored

### Redaction
- [ ] AC-017: Prompts redacted
- [ ] AC-018: Responses redacted
- [ ] AC-019: Tool outputs redacted
- [ ] AC-020: Config redacted
- [ ] AC-021: Files redacted
- [ ] AC-022: Uses Task 038
- [ ] AC-023: Before storage
- [ ] AC-024: Post-scan works

### Session Management
- [ ] AC-025: Unique session ID
- [ ] AC-026: Start/end time
- [ ] AC-027: Status tracked
- [ ] AC-028: Event count
- [ ] AC-029: Listable
- [ ] AC-030: Deletable
- [ ] AC-031: Exportable
- [ ] AC-032: Importable

---

## User Verification Scenarios

### Scenario 1: Enable Capture
**Persona:** Developer debugging  
**Preconditions:** Capture disabled  
**Steps:**
1. Set capture=true
2. Run agent
3. Session recorded
4. Artifact created

**Verification Checklist:**
- [ ] Capture enabled
- [ ] Session recorded
- [ ] Artifact exists
- [ ] Metadata correct

### Scenario 2: Verify Redaction
**Persona:** Developer checking security  
**Preconditions:** Session with secrets  
**Steps:**
1. Run with secrets in env
2. Complete capture
3. Search artifact
4. No secrets found

**Verification Checklist:**
- [ ] Secrets in input
- [ ] Artifact created
- [ ] Search clean
- [ ] Markers present

### Scenario 3: List Sessions
**Persona:** Developer finding session  
**Preconditions:** Multiple sessions  
**Steps:**
1. Run list command
2. View sessions
3. See metadata
4. Find target

**Verification Checklist:**
- [ ] All sessions listed
- [ ] Metadata shown
- [ ] Sorted correctly
- [ ] ID visible

### Scenario 4: Export Session
**Persona:** Developer sharing  
**Preconditions:** Session exists  
**Steps:**
1. Export session
2. Get portable file
3. Verify checksum
4. Share safely

**Verification Checklist:**
- [ ] Export works
- [ ] File portable
- [ ] Checksum valid
- [ ] Redacted

### Scenario 5: Purge Old Sessions
**Persona:** Admin managing storage  
**Preconditions:** Old sessions exist  
**Steps:**
1. Set retention
2. Run purge
3. Old deleted
4. New preserved

**Verification Checklist:**
- [ ] Retention applied
- [ ] Old removed
- [ ] New kept
- [ ] Logged

### Scenario 6: Large Session
**Persona:** Developer with big run  
**Preconditions:** Many events  
**Steps:**
1. Run large task
2. Capture streams
3. File refs used
4. Artifact manageable

**Verification Checklist:**
- [ ] Streaming works
- [ ] Refs used
- [ ] Size reasonable
- [ ] Performance ok

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-042-01 | Prompt capture | FR-042-01 |
| UT-042-02 | Response capture | FR-042-02 |
| UT-042-03 | Tool call capture | FR-042-03 |
| UT-042-04 | Redaction | FR-042-36 |
| UT-042-05 | Session ID generation | FR-042-51 |
| UT-042-06 | Compression | FR-042-25 |
| UT-042-07 | Checksum | FR-042-26 |
| UT-042-08 | Metadata | FR-042-29 |
| UT-042-09 | Purge logic | FR-042-32 |
| UT-042-10 | Version header | FR-042-19 |
| UT-042-11 | Large data refs | FR-042-18 |
| UT-042-12 | Config exclusion | FR-042-47 |
| UT-042-13 | Toggle capture | FR-042-13 |
| UT-042-14 | Status tracking | FR-042-55 |
| UT-042-15 | Thread safety | NFR-042-19 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-042-01 | Full capture flow | E2E |
| IT-042-02 | Event log integration | Task 040 |
| IT-042-03 | Redaction integration | Task 038 |
| IT-042-04 | Config integration | Task 002 |
| IT-042-05 | Large session | FR-042-17 |
| IT-042-06 | Export/import | FR-042-62 |
| IT-042-07 | Cross-platform | NFR-042-15 |
| IT-042-08 | Performance overhead | NFR-042-01 |
| IT-042-09 | CLI integration | Task 000 |
| IT-042-10 | Crash recovery | NFR-042-18 |
| IT-042-11 | Concurrent capture | NFR-042-10 |
| IT-042-12 | Purge integration | FR-042-32 |
| IT-042-13 | Logging | NFR-042-21 |
| IT-042-14 | Streaming | FR-042-16 |
| IT-042-15 | Version compat | NFR-042-20 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Reproducibility/
│       ├── Session.cs
│       ├── CaptureEvent.cs
│       ├── SessionMetadata.cs
│       └── CaptureFormat.cs
├── Acode.Application/
│   └── Reproducibility/
│       ├── ICaptureService.cs
│       ├── ISessionStore.cs
│       └── CaptureOptions.cs
├── Acode.Infrastructure/
│   └── Reproducibility/
│       ├── CaptureService.cs
│       ├── FileSessionStore.cs
│       └── ArtifactWriter.cs
├── Acode.Cli/
│   └── Commands/
│       └── SessionCommand.cs
```

### Configuration Schema

```yaml
reproducibility:
  capture:
    enabled: false
    location: .agent/recordings
    compress: true
    retention:
      days: 30
      maxCount: 100
  exclude:
    - env.*
    - secrets.*
    - apiKey
    - password
    - token
```

### Key Implementation

```csharp
public class CaptureService : ICaptureService
{
    private Session? _currentSession;
    
    public async Task StartSessionAsync()
    {
        if (!_options.Enabled)
            return;
        
        _currentSession = new Session
        {
            Id = Ulid.NewUlid().ToString(),
            StartTime = DateTime.UtcNow,
            Status = SessionStatus.InProgress
        };
        
        _logger.LogInformation("Capture session started: {SessionId}", _currentSession.Id);
    }
    
    public async Task CaptureEventAsync(CaptureEvent evt)
    {
        if (_currentSession == null)
            return;
        
        // Redact before capture
        var redacted = await _redactor.RedactAsync(evt);
        
        // Add sequence
        redacted.Sequence = _currentSession.EventCount++;
        
        // Stream to storage
        await _store.AppendEventAsync(_currentSession.Id, redacted);
    }
    
    public async Task EndSessionAsync(bool success)
    {
        if (_currentSession == null)
            return;
        
        _currentSession.EndTime = DateTime.UtcNow;
        _currentSession.Status = success ? SessionStatus.Success : SessionStatus.Failed;
        
        // Finalize artifact
        await _store.FinalizeAsync(_currentSession);
        
        // Post-storage scan
        await _scanner.ScanAsync(_store.GetArtifactPath(_currentSession.Id));
        
        _logger.LogInformation("Capture session ended: {SessionId}, events: {Count}",
            _currentSession.Id, _currentSession.EventCount);
        
        _currentSession = null;
    }
}
```

**End of Task 042 Specification**
