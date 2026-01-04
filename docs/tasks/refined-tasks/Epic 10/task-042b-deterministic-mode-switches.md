# Task 042.b: Deterministic Mode Switches

**Priority:** P1 – High  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 10 – Core Reliability  
**Dependencies:** Task 042 (Reproducibility), Task 024 (Local LLM)  

---

## Description

Task 042.b implements deterministic mode switches—configuration options that eliminate sources of non-determinism in agent execution. When deterministic mode is enabled, the same inputs MUST produce the same outputs, enabling reliable replay, regression testing, and debugging.

Sources of non-determinism include: random number generation, timestamp-based logic, model sampling with temperature >0, file ordering from directory listings, hash map iteration order, and network timing. Deterministic mode addresses each source with appropriate controls.

For LLM inference, deterministic mode sets temperature to 0 and uses a fixed seed (if the model supports it). For random operations, it uses a seeded PRNG. For timestamps, it provides a configurable "frozen" or "incremental" mode. For file operations, it enforces sorted iteration.

Deterministic mode is opt-in and off by default. Normal operation allows non-determinism for natural behavior. Deterministic mode is enabled for replay, regression testing, and debugging scenarios where reproducibility is more important than natural variation.

### Business Value

Deterministic mode provides:
- Exact replay capability
- Regression test reliability
- Debug reproducibility
- Behavior verification
- Comparison testing

### Scope Boundaries

This task covers deterministic switches. Capture framework is Task 042. Prompt persistence is Task 042.a. Replay tooling is Task 042.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Reproducibility | Task 042 | Mode config | Control |
| Local LLM | Task 024 | Seed control | Temperature |
| Config | Task 002 | Switch config | YAML |
| File System | I/O | Sorted listing | Override |
| CLI | Task 000 | Mode toggle | User control |
| Replay | Task 042.c | Mode activation | Auto-enable |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Non-determinism leak | Replay diff | Identify source | Investigation |
| Seed not supported | Model check | Warn | Partial det. |
| Frozen time break | Validation | Warn | Logic error |
| Sorted list overhead | Performance | Warn | Slower |
| Missing source | Replay diff | Add control | Gap |
| Mode conflict | Validation | Error | Config fix |
| Seed overflow | Check | Wrap or error | Rare |
| Random leak | Audit | Identify | Fix code |

### Assumptions

1. **LLM supports seed**: Or temperature=0
2. **Random is controllable**: System.Random
3. **Time is mockable**: IClock interface
4. **File order controllable**: Sort explicitly
5. **Hash maps ordered**: Use sorted types
6. **Network not used**: Local only mode
7. **Seed is sufficient**: For determinism
8. **Mode is global**: Per-session

### Security Considerations

1. **Seed not secret**: Can be logged
2. **Frozen time safe**: For testing only
3. **No security bypass**: Mode is safe
4. **Production warning**: For non-det
5. **Audit of mode changes**: Tracked
6. **No crypto weakening**: Separate RNG
7. **Testing isolation**: Sandbox
8. **Config validation**: Safe values

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Deterministic | Same input = same output |
| Non-determinism | Variable behavior |
| Seed | Random initializer |
| PRNG | Pseudo-Random Number Generator |
| Temperature | LLM sampling variability |
| Frozen Time | Fixed timestamp |
| Sorted Iteration | Ordered file/collection access |
| Replay Fidelity | Match original behavior |

---

## Out of Scope

- Hardware non-determinism
- Floating point precision
- Thread scheduling
- OS-level randomness
- External service mocking
- Parallelism control

---

## Functional Requirements

### FR-001 to FR-015: Mode Configuration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042b-01 | Deterministic mode MUST be configurable | P0 |
| FR-042b-02 | Mode MUST be in config.yml | P0 |
| FR-042b-03 | Mode MUST be toggleable via CLI | P0 |
| FR-042b-04 | Default MUST be off | P0 |
| FR-042b-05 | Mode MUST apply session-wide | P0 |
| FR-042b-06 | Mode MUST be logged on start | P0 |
| FR-042b-07 | Mode change mid-session MUST error | P0 |
| FR-042b-08 | Seed MUST be configurable | P0 |
| FR-042b-09 | Default seed MUST be fixed value | P0 |
| FR-042b-10 | Seed MUST be logged | P0 |
| FR-042b-11 | Seed MUST be in capture | P0 |
| FR-042b-12 | Mode MUST be queryable | P0 |
| FR-042b-13 | Components MUST check mode | P0 |
| FR-042b-14 | Mode MUST affect all sources | P0 |
| FR-042b-15 | Partial mode MUST NOT exist | P0 |

### FR-016 to FR-030: LLM Control

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042b-16 | Temperature MUST be 0 in det mode | P0 |
| FR-042b-17 | Seed MUST be passed to model | P0 |
| FR-042b-18 | Model seed support MUST be checked | P0 |
| FR-042b-19 | No seed support MUST warn | P0 |
| FR-042b-20 | Warning MUST be logged | P0 |
| FR-042b-21 | Model params MUST be logged | P0 |
| FR-042b-22 | Same prompt MUST give same response | P0 |
| FR-042b-23 | Response MUST be verified on replay | P0 |
| FR-042b-24 | Divergence MUST be logged | P0 |
| FR-042b-25 | Top-k MUST be 1 in det mode | P1 |
| FR-042b-26 | Top-p MUST be 1.0 in det mode | P1 |
| FR-042b-27 | Repetition penalty MUST be fixed | P2 |
| FR-042b-28 | Model version MUST be captured | P0 |
| FR-042b-29 | Version mismatch MUST warn on replay | P0 |
| FR-042b-30 | Quantization MUST be logged | P1 |

### FR-031 to FR-045: Random Control

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042b-31 | Random MUST use seeded PRNG | P0 |
| FR-042b-32 | PRNG MUST be deterministic | P0 |
| FR-042b-33 | Seed MUST be configurable | P0 |
| FR-042b-34 | Default seed MUST be 42 | P0 |
| FR-042b-35 | PRNG MUST be injectable | P0 |
| FR-042b-36 | IRandom interface MUST exist | P0 |
| FR-042b-37 | Direct Random MUST NOT be used | P0 |
| FR-042b-38 | GUID generation MUST be seeded | P0 |
| FR-042b-39 | ULID generation MUST be seeded | P0 |
| FR-042b-40 | Shuffle MUST be deterministic | P0 |
| FR-042b-41 | Sample MUST be deterministic | P0 |
| FR-042b-42 | Jitter MUST be deterministic | P0 |
| FR-042b-43 | Retry delay MUST be deterministic | P0 |
| FR-042b-44 | Hash seed MUST be controlled | P1 |
| FR-042b-45 | Crypto RNG MUST be separate | P0 |

### FR-046 to FR-060: Time Control

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042b-46 | Time MUST use IClock interface | P0 |
| FR-042b-47 | IClock MUST be injectable | P0 |
| FR-042b-48 | Frozen mode MUST exist | P0 |
| FR-042b-49 | Frozen time MUST be configurable | P0 |
| FR-042b-50 | Incremental mode MUST exist | P1 |
| FR-042b-51 | Increment MUST be configurable | P1 |
| FR-042b-52 | Default increment MUST be 1s | P1 |
| FR-042b-53 | DateTime.Now MUST NOT be used | P0 |
| FR-042b-54 | DateTime.UtcNow MUST NOT be used | P0 |
| FR-042b-55 | Stopwatch MUST be controllable | P1 |
| FR-042b-56 | Timeout MUST use IClock | P0 |
| FR-042b-57 | Delay MUST use IClock | P0 |
| FR-042b-58 | Time in logs MUST be consistent | P0 |
| FR-042b-59 | Start time MUST be captured | P0 |
| FR-042b-60 | Time drift MUST be detectable | P1 |

### FR-061 to FR-070: Collection Control

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-042b-61 | Directory listing MUST be sorted | P0 |
| FR-042b-62 | Sort MUST be lexicographic | P0 |
| FR-042b-63 | Hash iteration MUST be ordered | P0 |
| FR-042b-64 | SortedDictionary MUST be used | P0 |
| FR-042b-65 | Parallel MUST be serialized | P1 |
| FR-042b-66 | Async MUST be deterministic | P1 |
| FR-042b-67 | Task order MUST be controlled | P1 |
| FR-042b-68 | Enumerable order MUST be stable | P0 |
| FR-042b-69 | Globbing MUST be sorted | P0 |
| FR-042b-70 | File metadata MUST be consistent | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-042b-01 | Mode check | <0.1ms | P0 |
| NFR-042b-02 | Seeded random | <0.1ms | P0 |
| NFR-042b-03 | Clock access | <0.1ms | P0 |
| NFR-042b-04 | Sorted listing | <2x normal | P1 |
| NFR-042b-05 | Sorted dict overhead | <20% | P1 |
| NFR-042b-06 | LLM params | No overhead | P0 |
| NFR-042b-07 | Memory overhead | <10MB | P2 |
| NFR-042b-08 | Startup impact | <100ms | P1 |
| NFR-042b-09 | Mode switching | N/A (once) | P0 |
| NFR-042b-10 | Determinism check | <1ms | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-042b-11 | Determinism | 100% | P0 |
| NFR-042b-12 | Same seed = same | 100% | P0 |
| NFR-042b-13 | Mode enforced | 100% | P0 |
| NFR-042b-14 | No leak | 100% | P0 |
| NFR-042b-15 | Cross-platform | Same behavior | P0 |
| NFR-042b-16 | Thread safety | No races | P0 |
| NFR-042b-17 | Replay match | 99.9% | P0 |
| NFR-042b-18 | Detection of leaks | Logged | P0 |
| NFR-042b-19 | Graceful fallback | Warn + continue | P0 |
| NFR-042b-20 | Version compat | Documented | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-042b-21 | Mode logged | Info | P0 |
| NFR-042b-22 | Seed logged | Info | P0 |
| NFR-042b-23 | LLM params logged | Debug | P1 |
| NFR-042b-24 | Warning logged | Warning | P0 |
| NFR-042b-25 | Metrics: mode | Gauge | P2 |
| NFR-042b-26 | Metrics: seed | Label | P2 |
| NFR-042b-27 | Divergence logged | Error | P0 |
| NFR-042b-28 | Structured logging | JSON | P0 |
| NFR-042b-29 | Config in capture | Full | P0 |
| NFR-042b-30 | Health check | Mode query | P2 |

---

## Acceptance Criteria / Definition of Done

### Mode Configuration
- [ ] AC-001: Mode configurable
- [ ] AC-002: In config.yml
- [ ] AC-003: CLI toggle
- [ ] AC-004: Default off
- [ ] AC-005: Session-wide
- [ ] AC-006: Logged on start
- [ ] AC-007: Seed configurable
- [ ] AC-008: Seed logged

### LLM Control
- [ ] AC-009: Temperature = 0
- [ ] AC-010: Seed passed
- [ ] AC-011: Support checked
- [ ] AC-012: Warning on no support
- [ ] AC-013: Same prompt = same response
- [ ] AC-014: Top-k = 1
- [ ] AC-015: Version captured
- [ ] AC-016: Divergence logged

### Random Control
- [ ] AC-017: Seeded PRNG
- [ ] AC-018: Deterministic
- [ ] AC-019: IRandom interface
- [ ] AC-020: No direct Random
- [ ] AC-021: GUID seeded
- [ ] AC-022: Shuffle deterministic
- [ ] AC-023: Jitter deterministic
- [ ] AC-024: Crypto separate

### Time Control
- [ ] AC-025: IClock interface
- [ ] AC-026: Injectable
- [ ] AC-027: Frozen mode
- [ ] AC-028: Configurable time
- [ ] AC-029: No DateTime.Now
- [ ] AC-030: Timeout uses IClock
- [ ] AC-031: Delay uses IClock
- [ ] AC-032: Consistent in logs

---

## User Verification Scenarios

### Scenario 1: Enable Deterministic Mode
**Persona:** Developer testing  
**Preconditions:** Mode disabled  
**Steps:**
1. Set deterministic=true
2. Set seed=12345
3. Run agent
4. Note outputs

**Verification Checklist:**
- [ ] Mode enabled
- [ ] Seed logged
- [ ] LLM deterministic
- [ ] Random deterministic

### Scenario 2: Verify Same Output
**Persona:** Developer testing  
**Preconditions:** Det mode enabled  
**Steps:**
1. Run with seed 42
2. Record outputs
3. Run again with seed 42
4. Compare

**Verification Checklist:**
- [ ] Outputs match
- [ ] No divergence
- [ ] All sources controlled
- [ ] Logged

### Scenario 3: LLM Determinism
**Persona:** Developer testing LLM  
**Preconditions:** Det mode enabled  
**Steps:**
1. Send prompt
2. Get response
3. Send same prompt
4. Get same response

**Verification Checklist:**
- [ ] Temperature = 0
- [ ] Seed set
- [ ] Responses match
- [ ] Params logged

### Scenario 4: File Order Determinism
**Persona:** Developer testing files  
**Preconditions:** Det mode enabled  
**Steps:**
1. List directory
2. Note order
3. List again
4. Same order

**Verification Checklist:**
- [ ] Sorted listing
- [ ] Consistent order
- [ ] Cross-platform same
- [ ] Logged

### Scenario 5: Time Control
**Persona:** Developer testing time  
**Preconditions:** Det mode + frozen  
**Steps:**
1. Enable frozen time
2. Check timestamps
3. All same value
4. Increment mode

**Verification Checklist:**
- [ ] Frozen works
- [ ] All same
- [ ] Increment works
- [ ] Configurable

### Scenario 6: Non-Determinism Warning
**Persona:** Developer with unsupported model  
**Preconditions:** Model no seed support  
**Steps:**
1. Enable det mode
2. Start agent
3. Warning shown
4. Continues with temp=0

**Verification Checklist:**
- [ ] Warning logged
- [ ] Temperature = 0
- [ ] Best effort
- [ ] Documented

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-042b-01 | Mode configuration | FR-042b-01 |
| UT-042b-02 | Default off | FR-042b-04 |
| UT-042b-03 | Seed configuration | FR-042b-08 |
| UT-042b-04 | LLM temperature | FR-042b-16 |
| UT-042b-05 | LLM seed | FR-042b-17 |
| UT-042b-06 | Seeded PRNG | FR-042b-31 |
| UT-042b-07 | IRandom interface | FR-042b-36 |
| UT-042b-08 | GUID seeded | FR-042b-38 |
| UT-042b-09 | IClock interface | FR-042b-46 |
| UT-042b-10 | Frozen time | FR-042b-48 |
| UT-042b-11 | Sorted listing | FR-042b-61 |
| UT-042b-12 | Ordered dict | FR-042b-64 |
| UT-042b-13 | Same seed same | NFR-042b-12 |
| UT-042b-14 | Cross-platform | NFR-042b-15 |
| UT-042b-15 | Thread safety | NFR-042b-16 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-042b-01 | Full det mode flow | E2E |
| IT-042b-02 | LLM integration | Task 024 |
| IT-042b-03 | Config integration | Task 002 |
| IT-042b-04 | Capture integration | Task 042 |
| IT-042b-05 | Replay integration | Task 042.c |
| IT-042b-06 | Same run twice | FR-042b-22 |
| IT-042b-07 | All sources controlled | FR-042b-14 |
| IT-042b-08 | CLI toggle | FR-042b-03 |
| IT-042b-09 | Logging | NFR-042b-21 |
| IT-042b-10 | Performance | NFR-042b-01 |
| IT-042b-11 | Cross-platform | NFR-042b-15 |
| IT-042b-12 | No seed warning | FR-042b-19 |
| IT-042b-13 | Divergence detection | FR-042b-24 |
| IT-042b-14 | Mode mid-session | FR-042b-07 |
| IT-042b-15 | Crypto separate | FR-042b-45 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Determinism/
│       ├── DeterministicMode.cs
│       ├── IRandom.cs
│       ├── IClock.cs
│       └── DeterministicSeed.cs
├── Acode.Application/
│   └── Determinism/
│       ├── IDeterministicContext.cs
│       └── DeterministicOptions.cs
├── Acode.Infrastructure/
│   └── Determinism/
│       ├── SeededRandom.cs
│       ├── FrozenClock.cs
│       ├── IncrementalClock.cs
│       └── DeterministicFileSystem.cs
```

### Configuration Schema

```yaml
reproducibility:
  deterministic:
    enabled: false
    seed: 42
    llm:
      temperature: 0
      topK: 1
      topP: 1.0
    time:
      mode: frozen  # frozen | incremental
      startTime: "2024-01-01T00:00:00Z"
      increment: 1000  # ms
```

### Key Implementation

```csharp
public class DeterministicContext : IDeterministicContext
{
    private readonly int _seed;
    private readonly IRandom _random;
    private readonly IClock _clock;
    
    public DeterministicContext(DeterministicOptions options)
    {
        if (!options.Enabled)
        {
            _random = new SystemRandom();
            _clock = new SystemClock();
            return;
        }
        
        _seed = options.Seed ?? 42;
        _random = new SeededRandom(_seed);
        _clock = options.Time.Mode == TimeMode.Frozen
            ? new FrozenClock(options.Time.StartTime)
            : new IncrementalClock(options.Time.StartTime, options.Time.Increment);
        
        _logger.LogInformation("Deterministic mode enabled, seed={Seed}", _seed);
    }
    
    public IRandom Random => _random;
    public IClock Clock => _clock;
    public bool IsDeterministic => _seed != null;
    
    public LlmParameters GetLlmParameters()
    {
        if (!IsDeterministic)
            return LlmParameters.Default;
        
        return new LlmParameters
        {
            Temperature = 0,
            TopK = 1,
            TopP = 1.0f,
            Seed = _seed
        };
    }
}

public class SeededRandom : IRandom
{
    private readonly Random _rng;
    
    public SeededRandom(int seed)
    {
        _rng = new Random(seed);
    }
    
    public int Next() => _rng.Next();
    public int Next(int max) => _rng.Next(max);
    public double NextDouble() => _rng.NextDouble();
    
    public Guid NewGuid()
    {
        var bytes = new byte[16];
        _rng.NextBytes(bytes);
        return new Guid(bytes);
    }
}
```

**End of Task 042.b Specification**
