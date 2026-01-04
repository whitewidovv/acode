# Task 042.c: Replay Tooling

**Priority:** P1 – High  
**Tier:** F – Foundation Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 10 – Core Reliability  
**Dependencies:** Task 042 (Reproducibility), Task 042.a (Persistence), Task 042.b (Determinism)  

---

## Description

Task 042.c implements replay tooling—the capability to re-execute a captured session and compare results with the original run. Replay is essential for debugging, regression testing, and verifying that code changes haven't altered agent behavior.

The replay engine reads captured session artifacts (Task 042.a), enables deterministic mode (Task 042.b), and re-executes each operation in sequence. After each step, it compares the actual output with the recorded output, logging any divergences.

Replay operates in isolation. It uses the captured prompts and inputs, but re-executes tool calls and LLM inference. This tests the current codebase against recorded behavior. If the code is unchanged, outputs should match exactly. If there are divergences, the replay engine provides detailed diffs.

Replay supports multiple modes: full replay (re-execute everything), mock replay (use recorded outputs for external calls), and validation replay (compare only, no side effects). Each mode serves different debugging and testing scenarios.

### Business Value

Replay tooling provides:
- Regression detection
- Debug reproduction
- Behavior verification
- Change impact analysis
- Test automation foundation

### Scope Boundaries

This task covers replay execution and comparison. Capture framework is Task 042. Persistence is Task 042.a. Deterministic mode is Task 042.b.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Capture | Task 042 | Artifact source | Input |
| Persistence | Task 042.a | Prompt/response | Input |
| Determinism | Task 042.b | Mode activation | Control |
| Local LLM | Task 024 | Re-inference | Execution |
| Tool Executor | `IToolExecutor` | Re-execution | Execution |
| CLI | Task 000 | Replay commands | User interface |
| Event Log | Task 040 | Replay events | Audit |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Artifact corrupt | Checksum | Error | Cannot replay |
| Version mismatch | Header check | Warn | May diverge |
| Divergence | Comparison | Log diff | Investigation |
| Missing artifact | File check | Error | Cannot replay |
| Out of memory | Monitor | Stream | Adjust settings |
| LLM mismatch | Response diff | Log | Expected with changes |
| Tool failure | Execution error | Log + continue | May cascade |
| Timeout | Timer | Extend or abort | Long replay |

### Assumptions

1. **Artifacts available**: Captured session
2. **Determinism works**: Task 042.b
3. **LLM deterministic**: With seed
4. **Tools idempotent**: For full replay
5. **Comparison possible**: Serializable
6. **Sandbox available**: For isolation
7. **Resources sufficient**: Memory, disk
8. **Version tracked**: For compatibility

### Security Considerations

1. **Replay isolated**: Sandbox
2. **No network access**: Local only
3. **No persistent changes**: Option
4. **Captured data safe**: Redacted
5. **Replay logged**: Audit trail
6. **No credential use**: Redacted
7. **Temp files cleaned**: After replay
8. **Abort safe**: No partial state

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Replay | Re-execute captured session |
| Divergence | Difference from original |
| Full Replay | Execute all operations |
| Mock Replay | Use recorded outputs |
| Validation Replay | Compare only |
| Artifact | Captured session data |
| Diff | Detailed comparison |
| Regression | Behavior change |
| Sandbox | Isolated environment |
| Comparison | Check for match |

---

## Out of Scope

- Distributed replay
- Real-time streaming replay
- Replay acceleration
- Replay analytics dashboard
- Automatic fix suggestions
- Replay optimization

---

## Functional Requirements

### FR-001 to FR-015: Replay Initialization

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042c-01 | Replay MUST load artifact | P0 |
| FR-042c-02 | Artifact MUST be validated | P0 |
| FR-042c-03 | Checksum MUST be verified | P0 |
| FR-042c-04 | Version MUST be checked | P0 |
| FR-042c-05 | Incompatible version MUST warn | P0 |
| FR-042c-06 | Deterministic mode MUST be enabled | P0 |
| FR-042c-07 | Seed MUST be from artifact | P0 |
| FR-042c-08 | Config MUST be from artifact | P0 |
| FR-042c-09 | Start time MUST be from artifact | P0 |
| FR-042c-10 | Session metadata MUST be logged | P0 |
| FR-042c-11 | Event count MUST be known | P0 |
| FR-042c-12 | Progress MUST be trackable | P0 |
| FR-042c-13 | Initialization MUST be fast | P0 |
| FR-042c-14 | Missing artifact MUST error | P0 |
| FR-042c-15 | Corrupt artifact MUST error | P0 |

### FR-016 to FR-035: Replay Execution

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042c-16 | Events MUST be replayed in order | P0 |
| FR-042c-17 | Sequence MUST match original | P0 |
| FR-042c-18 | Prompts MUST be re-sent | P0 |
| FR-042c-19 | Tool calls MUST be re-executed | P0 |
| FR-042c-20 | Full replay MUST execute tools | P0 |
| FR-042c-21 | Mock replay MUST use recorded | P0 |
| FR-042c-22 | Validation replay MUST compare only | P0 |
| FR-042c-23 | Mode MUST be configurable | P0 |
| FR-042c-24 | Default mode MUST be validation | P0 |
| FR-042c-25 | Each step MUST be logged | P0 |
| FR-042c-26 | Progress MUST be shown | P0 |
| FR-042c-27 | Pause MUST be possible | P1 |
| FR-042c-28 | Resume MUST be possible | P1 |
| FR-042c-29 | Abort MUST be possible | P0 |
| FR-042c-30 | Abort MUST cleanup | P0 |
| FR-042c-31 | Timeout MUST be configurable | P0 |
| FR-042c-32 | Default timeout MUST be 10x original | P1 |
| FR-042c-33 | Timeout MUST be per-step | P0 |
| FR-042c-34 | Overall timeout MUST exist | P1 |
| FR-042c-35 | Timeout MUST be logged | P0 |

### FR-036 to FR-055: Comparison

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042c-36 | Response MUST be compared | P0 |
| FR-042c-37 | Tool output MUST be compared | P0 |
| FR-042c-38 | File state MUST be compared | P0 |
| FR-042c-39 | Comparison MUST be exact by default | P0 |
| FR-042c-40 | Fuzzy comparison MUST be optional | P1 |
| FR-042c-41 | Ignore list MUST be configurable | P1 |
| FR-042c-42 | Timestamps MUST be ignorable | P1 |
| FR-042c-43 | Divergence MUST be logged | P0 |
| FR-042c-44 | Divergence MUST include diff | P0 |
| FR-042c-45 | Diff MUST be human-readable | P0 |
| FR-042c-46 | Diff MUST be machine-parseable | P1 |
| FR-042c-47 | Divergence count MUST be tracked | P0 |
| FR-042c-48 | First divergence MUST be noted | P0 |
| FR-042c-49 | Continue after divergence MUST be option | P0 |
| FR-042c-50 | Stop on first MUST be option | P0 |
| FR-042c-51 | Divergence severity MUST be categorized | P1 |
| FR-042c-52 | Minor/major MUST be distinguished | P1 |
| FR-042c-53 | Summary MUST be generated | P0 |
| FR-042c-54 | Summary MUST include statistics | P0 |
| FR-042c-55 | Summary MUST be exportable | P1 |

### FR-056 to FR-070: Isolation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042c-56 | Replay MUST be isolated | P0 |
| FR-042c-57 | Sandbox MUST be created | P0 |
| FR-042c-58 | Original workspace MUST NOT change | P0 |
| FR-042c-59 | Temp directory MUST be used | P0 |
| FR-042c-60 | Temp MUST be cleaned after | P0 |
| FR-042c-61 | Network MUST be disabled | P0 |
| FR-042c-62 | External tools MUST be mocked | P0 |
| FR-042c-63 | Database MUST be copied | P0 |
| FR-042c-64 | Copy MUST be temp | P0 |
| FR-042c-65 | File writes MUST be to sandbox | P0 |
| FR-042c-66 | File reads MUST use snapshot | P0 |
| FR-042c-67 | Snapshot MUST be from artifact | P0 |
| FR-042c-68 | Cleanup on abort MUST work | P0 |
| FR-042c-69 | Cleanup on error MUST work | P0 |
| FR-042c-70 | Cleanup on success MUST work | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-042c-01 | Artifact load | <1s | P0 |
| NFR-042c-02 | Per-event overhead | <10ms | P0 |
| NFR-042c-03 | Comparison | <50ms | P0 |
| NFR-042c-04 | Diff generation | <100ms | P1 |
| NFR-042c-05 | Sandbox creation | <1s | P0 |
| NFR-042c-06 | Cleanup | <5s | P0 |
| NFR-042c-07 | Memory usage | <2x original | P1 |
| NFR-042c-08 | Disk usage (temp) | <1GB | P2 |
| NFR-042c-09 | Progress update | 1/s | P1 |
| NFR-042c-10 | Streaming load | For large | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-042c-11 | Sequence match | 100% | P0 |
| NFR-042c-12 | Comparison correct | 100% | P0 |
| NFR-042c-13 | Isolation complete | 100% | P0 |
| NFR-042c-14 | Cleanup complete | 100% | P0 |
| NFR-042c-15 | Abort safe | 100% | P0 |
| NFR-042c-16 | Cross-platform | All OS | P0 |
| NFR-042c-17 | Version compat | Documented | P0 |
| NFR-042c-18 | Error recovery | Cleanup + log | P0 |
| NFR-042c-19 | Crash recovery | Cleanup on next | P1 |
| NFR-042c-20 | Concurrent replay | Supported | P2 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-042c-21 | Replay start logged | Info | P0 |
| NFR-042c-22 | Replay end logged | Info | P0 |
| NFR-042c-23 | Divergence logged | Warning | P0 |
| NFR-042c-24 | Progress logged | Debug | P1 |
| NFR-042c-25 | Metrics: replays | Counter | P2 |
| NFR-042c-26 | Metrics: divergences | Counter | P2 |
| NFR-042c-27 | Metrics: duration | Histogram | P2 |
| NFR-042c-28 | Structured logging | JSON | P0 |
| NFR-042c-29 | Summary report | Exportable | P1 |
| NFR-042c-30 | Alert on divergence | Optional | P2 |

---

## Acceptance Criteria / Definition of Done

### Replay Initialization
- [ ] AC-001: Artifact loaded
- [ ] AC-002: Validated
- [ ] AC-003: Checksum verified
- [ ] AC-004: Version checked
- [ ] AC-005: Det mode enabled
- [ ] AC-006: Seed from artifact
- [ ] AC-007: Config from artifact
- [ ] AC-008: Metadata logged

### Replay Execution
- [ ] AC-009: Events in order
- [ ] AC-010: Sequence matches
- [ ] AC-011: Prompts re-sent
- [ ] AC-012: Full replay works
- [ ] AC-013: Mock replay works
- [ ] AC-014: Validation works
- [ ] AC-015: Progress shown
- [ ] AC-016: Abort works

### Comparison
- [ ] AC-017: Response compared
- [ ] AC-018: Tool output compared
- [ ] AC-019: File state compared
- [ ] AC-020: Exact by default
- [ ] AC-021: Fuzzy optional
- [ ] AC-022: Divergence logged
- [ ] AC-023: Diff included
- [ ] AC-024: Summary generated

### Isolation
- [ ] AC-025: Sandbox created
- [ ] AC-026: Original unchanged
- [ ] AC-027: Temp directory used
- [ ] AC-028: Network disabled
- [ ] AC-029: External mocked
- [ ] AC-030: Cleanup works
- [ ] AC-031: Cleanup on abort
- [ ] AC-032: Cleanup on error

---

## User Verification Scenarios

### Scenario 1: Basic Replay
**Persona:** Developer debugging  
**Preconditions:** Captured session exists  
**Steps:**
1. Run replay command
2. Artifact loaded
3. Events replayed
4. Summary shown

**Verification Checklist:**
- [ ] Replay starts
- [ ] Progress shown
- [ ] Completes
- [ ] Summary accurate

### Scenario 2: Detect Divergence
**Persona:** Developer after code change  
**Preconditions:** Code modified  
**Steps:**
1. Replay previous session
2. Divergence detected
3. Diff shown
4. Investigate

**Verification Checklist:**
- [ ] Divergence found
- [ ] Diff clear
- [ ] Location identified
- [ ] Actionable

### Scenario 3: Mock Replay
**Persona:** Developer testing logic  
**Preconditions:** Session exists  
**Steps:**
1. Use mock mode
2. Recorded outputs used
3. Logic tested
4. Fast execution

**Verification Checklist:**
- [ ] Mock works
- [ ] No real calls
- [ ] Fast
- [ ] Logic tested

### Scenario 4: Isolated Execution
**Persona:** Developer testing safety  
**Preconditions:** Session with writes  
**Steps:**
1. Full replay
2. Files written to sandbox
3. Original unchanged
4. Cleanup occurs

**Verification Checklist:**
- [ ] Sandbox used
- [ ] Original safe
- [ ] Cleanup complete
- [ ] No side effects

### Scenario 5: Abort Replay
**Persona:** Developer canceling  
**Preconditions:** Replay in progress  
**Steps:**
1. Start replay
2. Press Ctrl+C
3. Abort handled
4. Cleanup runs

**Verification Checklist:**
- [ ] Abort detected
- [ ] Safe stop
- [ ] Cleanup runs
- [ ] No mess

### Scenario 6: Version Mismatch
**Persona:** Developer with old capture  
**Preconditions:** Older artifact version  
**Steps:**
1. Load old artifact
2. Warning shown
3. Continue offered
4. Replay attempts

**Verification Checklist:**
- [ ] Version detected
- [ ] Warning clear
- [ ] User chooses
- [ ] Best effort

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-042c-01 | Artifact loading | FR-042c-01 |
| UT-042c-02 | Checksum verification | FR-042c-03 |
| UT-042c-03 | Version check | FR-042c-04 |
| UT-042c-04 | Event ordering | FR-042c-16 |
| UT-042c-05 | Full replay mode | FR-042c-20 |
| UT-042c-06 | Mock replay mode | FR-042c-21 |
| UT-042c-07 | Validation mode | FR-042c-22 |
| UT-042c-08 | Response comparison | FR-042c-36 |
| UT-042c-09 | Divergence detection | FR-042c-43 |
| UT-042c-10 | Diff generation | FR-042c-44 |
| UT-042c-11 | Sandbox creation | FR-042c-57 |
| UT-042c-12 | Cleanup | FR-042c-60 |
| UT-042c-13 | Abort handling | FR-042c-29 |
| UT-042c-14 | Timeout | FR-042c-31 |
| UT-042c-15 | Summary generation | FR-042c-53 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-042c-01 | Full replay flow | E2E |
| IT-042c-02 | Capture integration | Task 042 |
| IT-042c-03 | Persistence integration | Task 042.a |
| IT-042c-04 | Determinism integration | Task 042.b |
| IT-042c-05 | LLM replay | Task 024 |
| IT-042c-06 | Tool replay | IToolExecutor |
| IT-042c-07 | CLI integration | Task 000 |
| IT-042c-08 | Isolation | FR-042c-56 |
| IT-042c-09 | Large artifact | NFR-042c-10 |
| IT-042c-10 | Cross-platform | NFR-042c-16 |
| IT-042c-11 | Concurrent | NFR-042c-20 |
| IT-042c-12 | Logging | NFR-042c-21 |
| IT-042c-13 | Crash recovery | NFR-042c-19 |
| IT-042c-14 | Version compat | NFR-042c-17 |
| IT-042c-15 | Summary export | FR-042c-55 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Replay/
│       ├── ReplaySession.cs
│       ├── ReplayResult.cs
│       ├── Divergence.cs
│       └── ReplayMode.cs
├── Acode.Application/
│   └── Replay/
│       ├── IReplayEngine.cs
│       ├── IComparer.cs
│       └── ReplayOptions.cs
├── Acode.Infrastructure/
│   └── Replay/
│       ├── ReplayEngine.cs
│       ├── ArtifactLoader.cs
│       ├── EventComparer.cs
│       ├── DiffGenerator.cs
│       └── SandboxManager.cs
├── Acode.Cli/
│   └── Commands/
│       └── ReplayCommand.cs
```

### CLI Commands

```bash
# Basic replay
acode replay <session-id>

# Replay with mode
acode replay <session-id> --mode validation
acode replay <session-id> --mode mock
acode replay <session-id> --mode full

# Replay options
acode replay <session-id> --stop-on-divergence
acode replay <session-id> --timeout 300
acode replay <session-id> --ignore timestamps,durations

# Export results
acode replay <session-id> --export-report report.json
```

### Key Implementation

```csharp
public class ReplayEngine : IReplayEngine
{
    public async Task<ReplayResult> ReplayAsync(
        string sessionId, 
        ReplayOptions options,
        CancellationToken ct = default)
    {
        // Load artifact
        var artifact = await _loader.LoadAsync(sessionId);
        ValidateArtifact(artifact);
        
        // Setup sandbox
        using var sandbox = await _sandboxManager.CreateAsync();
        
        // Enable deterministic mode with captured seed
        _deterministicContext.Enable(artifact.Seed);
        
        var result = new ReplayResult(sessionId);
        
        try
        {
            foreach (var evt in artifact.Events)
            {
                ct.ThrowIfCancellationRequested();
                
                var replayedOutput = options.Mode switch
                {
                    ReplayMode.Full => await ExecuteEventAsync(evt, sandbox),
                    ReplayMode.Mock => evt.RecordedOutput,
                    ReplayMode.Validation => evt.RecordedOutput
                };
                
                // Compare
                var comparison = await _comparer.CompareAsync(
                    evt.RecordedOutput, replayedOutput, options);
                
                if (comparison.HasDivergence)
                {
                    var divergence = new Divergence(
                        evt.Sequence,
                        comparison.Diff,
                        _diffGenerator.Generate(evt.RecordedOutput, replayedOutput));
                    
                    result.AddDivergence(divergence);
                    _logger.LogWarning("Divergence at {Seq}: {Summary}",
                        evt.Sequence, comparison.Summary);
                    
                    if (options.StopOnDivergence)
                        break;
                }
                
                result.IncrementProcessed();
                OnProgress?.Invoke(result.Processed, artifact.EventCount);
            }
        }
        finally
        {
            await sandbox.CleanupAsync();
            _deterministicContext.Disable();
        }
        
        result.Complete();
        
        _logger.LogInformation("Replay complete: {Processed} events, {Divergences} divergences",
            result.Processed, result.DivergenceCount);
        
        return result;
    }
}
```

**End of Task 042.c Specification**
