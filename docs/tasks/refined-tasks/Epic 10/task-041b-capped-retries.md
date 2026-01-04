# Task 041.b: Capped Retries – Maximum Attempt Limits

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 10 – Core Reliability  
**Dependencies:** Task 041 (Retry Framework), Task 041.a (Categorization)  

---

## Description

Task 041.b implements capped retries—strict maximum limits on retry attempts that prevent infinite loops and wasted resources. Every retryable operation MUST have a maximum attempt count. When the limit is reached, the operation fails permanently and escalates to human intervention (Task 041.c).

The retry cap is the safety net that prevents runaway retries. Even if an error is classified as transient, repeatedly failing operations should not retry forever. The cap ensures predictable failure within bounded time and provides a clear escalation trigger.

Retry caps are configurable per policy, with sensible defaults. Network operations might allow 5 attempts, while file operations might allow 3. The default cap of 3 applies when no specific policy is configured. A minimum cap of 1 means "no retry" (single attempt).

The framework tracks attempt count per operation invocation. Each invocation starts at 1 and increments on each retry. The counter resets between invocations—a successful operation followed by a new invocation starts fresh.

### Business Value

Capped retries provide:
- Bounded failure time
- Resource protection
- Predictable escalation
- No infinite loops
- Debugging clarity

### Scope Boundaries

This task covers retry counting and cap enforcement. Retry execution is Task 041. Failure categorization is Task 041.a. Escalation is Task 041.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Retry Framework | Task 041 | Counter access | Core |
| Categorization | Task 041.a | Retry decision | Input |
| Escalation | Task 041.c | Cap exceeded | Output |
| Config | Task 002 | Cap values | YAML |
| Event Log | Task 040 | Log attempts | Audit |
| Resume | Task 040.b | Retry count | Resume |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Cap exceeded | Counter check | Escalate | Human needed |
| Counter overflow | Check | Error | Bug |
| Counter reset fail | State check | Error | Bug |
| Config missing | Validation | Use default | None |
| Cap too low | Config review | Adjust | Many escalations |
| Cap too high | Config review | Adjust | Slow failures |
| Counter not persisted | Resume check | Error | Lost count |
| Concurrent update | Lock | Serialize | None |

### Assumptions

1. **Caps are reasonable**: 1-10 typical
2. **Counter per invocation**: Not global
3. **Counter fits in int**: <2B attempts
4. **Config available**: Task 002
5. **Escalation works**: Task 041.c
6. **Resume includes count**: Task 040.b
7. **Logging works**: Task 040
8. **Human available**: For escalation

### Security Considerations

1. **Counter not externally modifiable**: Internal only
2. **Cap not bypassable**: Enforced
3. **Escalation logged**: Audit trail
4. **No retry storm**: Cap prevents
5. **Resource exhaustion prevented**: Bounded
6. **Config validated**: Reasonable values
7. **Override audited**: Track changes
8. **Attempt history logged**: Debug

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Retry Cap | Maximum attempt count |
| Attempt | Single operation try |
| Counter | Tracks attempts |
| Cap Exceeded | Counter >= max |
| Escalation | Human intervention |
| Default Cap | Fallback limit |
| Per-Policy Cap | Operation-specific limit |
| Counter Reset | Start at 1 |

---

## Out of Scope

- Dynamic cap adjustment
- Cap learning from history
- Distributed cap coordination
- Cap analytics
- Cap recommendations
- Time-based caps

---

## Functional Requirements

### FR-001 to FR-015: Counter Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041b-01 | Counter MUST start at 1 | P0 |
| FR-041b-02 | Counter MUST increment on retry | P0 |
| FR-041b-03 | Counter MUST be per-invocation | P0 |
| FR-041b-04 | Counter MUST reset between invocations | P0 |
| FR-041b-05 | Counter MUST be queryable | P0 |
| FR-041b-06 | Counter MUST be logged | P0 |
| FR-041b-07 | Counter MUST be thread-safe | P0 |
| FR-041b-08 | Counter MUST fit in int | P0 |
| FR-041b-09 | Counter MUST be non-negative | P0 |
| FR-041b-10 | Counter overflow MUST error | P0 |
| FR-041b-11 | Counter MUST include in event | P0 |
| FR-041b-12 | Event MUST have attempt number | P0 |
| FR-041b-13 | Event MUST have max attempts | P0 |
| FR-041b-14 | Resume MUST restore counter | P0 |
| FR-041b-15 | Restored counter MUST continue | P0 |

### FR-016 to FR-030: Cap Enforcement

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041b-16 | Cap MUST be enforced | P0 |
| FR-041b-17 | Counter >= max MUST fail | P0 |
| FR-041b-18 | Failure MUST be permanent | P0 |
| FR-041b-19 | Failure MUST trigger escalation | P0 |
| FR-041b-20 | Escalation MUST include details | P0 |
| FR-041b-21 | Details MUST include attempt count | P0 |
| FR-041b-22 | Details MUST include error history | P0 |
| FR-041b-23 | Cap check MUST occur before retry | P0 |
| FR-041b-24 | Cap check MUST be first | P0 |
| FR-041b-25 | Cap exceeded MUST be logged | P0 |
| FR-041b-26 | Log MUST include final error | P0 |
| FR-041b-27 | Log MUST include all attempts | P0 |
| FR-041b-28 | Cap MUST NOT be bypassable | P0 |
| FR-041b-29 | No API for cap bypass | P0 |
| FR-041b-30 | Admin override MUST be audited | P1 |

### FR-031 to FR-045: Configuration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041b-31 | Default cap MUST exist | P0 |
| FR-041b-32 | Default cap MUST be 3 | P0 |
| FR-041b-33 | Default cap MUST be configurable | P0 |
| FR-041b-34 | Per-policy cap MUST exist | P0 |
| FR-041b-35 | Per-policy MUST override default | P0 |
| FR-041b-36 | Cap MUST be positive integer | P0 |
| FR-041b-37 | Cap MUST be >= 1 | P0 |
| FR-041b-38 | Cap = 1 MUST mean no retry | P0 |
| FR-041b-39 | Cap validation MUST occur on load | P0 |
| FR-041b-40 | Invalid cap MUST error | P0 |
| FR-041b-41 | Cap MUST be in config.yml | P0 |
| FR-041b-42 | Cap MUST be in policy section | P0 |
| FR-041b-43 | Missing cap MUST use default | P0 |
| FR-041b-44 | Cap range MUST be 1-100 | P1 |
| FR-041b-45 | Cap > 100 MUST warn | P1 |

### FR-046 to FR-055: Error History

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041b-46 | Error history MUST be tracked | P0 |
| FR-041b-47 | History MUST include all attempts | P0 |
| FR-041b-48 | History MUST include error type | P0 |
| FR-041b-49 | History MUST include timestamp | P0 |
| FR-041b-50 | History MUST include duration | P1 |
| FR-041b-51 | History MUST be redacted | P0 |
| FR-041b-52 | History MUST be in escalation | P0 |
| FR-041b-53 | History MUST be in event log | P0 |
| FR-041b-54 | History size MUST be bounded | P0 |
| FR-041b-55 | Max history MUST be cap * 2 | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-041b-01 | Counter increment | <0.1ms | P0 |
| NFR-041b-02 | Cap check | <0.1ms | P0 |
| NFR-041b-03 | Counter reset | <0.1ms | P0 |
| NFR-041b-04 | History add | <1ms | P0 |
| NFR-041b-05 | History query | <5ms | P1 |
| NFR-041b-06 | Config lookup | <1ms | P0 |
| NFR-041b-07 | Memory per counter | <1KB | P2 |
| NFR-041b-08 | Memory per history | <10KB | P2 |
| NFR-041b-09 | Concurrent counters | 1000+ | P2 |
| NFR-041b-10 | Counter operations | No allocation | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-041b-11 | Cap enforced | 100% | P0 |
| NFR-041b-12 | No bypass | 100% | P0 |
| NFR-041b-13 | Counter correct | 100% | P0 |
| NFR-041b-14 | Thread safety | No races | P0 |
| NFR-041b-15 | Escalation works | 100% | P0 |
| NFR-041b-16 | Resume restore | 100% | P0 |
| NFR-041b-17 | Cross-platform | All OS | P0 |
| NFR-041b-18 | History preserved | 100% | P0 |
| NFR-041b-19 | Overflow handled | 100% | P0 |
| NFR-041b-20 | Graceful config error | Falls back | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-041b-21 | Attempt logged | Debug | P0 |
| NFR-041b-22 | Cap exceeded logged | Warning | P0 |
| NFR-041b-23 | Escalation logged | Info | P0 |
| NFR-041b-24 | Config logged | Debug | P1 |
| NFR-041b-25 | Metrics: attempts | Counter | P2 |
| NFR-041b-26 | Metrics: cap hits | Counter | P2 |
| NFR-041b-27 | Metrics: escalations | Counter | P2 |
| NFR-041b-28 | Structured logging | JSON | P0 |
| NFR-041b-29 | History in log | Redacted | P0 |
| NFR-041b-30 | Alert on high cap hits | Warning | P2 |

---

## Acceptance Criteria / Definition of Done

### Counter Management
- [ ] AC-001: Starts at 1
- [ ] AC-002: Increments on retry
- [ ] AC-003: Per-invocation
- [ ] AC-004: Resets between
- [ ] AC-005: Queryable
- [ ] AC-006: Logged
- [ ] AC-007: Thread-safe
- [ ] AC-008: In event

### Cap Enforcement
- [ ] AC-009: Cap enforced
- [ ] AC-010: Counter >= max fails
- [ ] AC-011: Triggers escalation
- [ ] AC-012: Details included
- [ ] AC-013: Check before retry
- [ ] AC-014: Logged
- [ ] AC-015: Not bypassable
- [ ] AC-016: Error history included

### Configuration
- [ ] AC-017: Default cap = 3
- [ ] AC-018: Default configurable
- [ ] AC-019: Per-policy cap
- [ ] AC-020: Override works
- [ ] AC-021: Positive integer
- [ ] AC-022: >= 1 required
- [ ] AC-023: Validation on load
- [ ] AC-024: Invalid errors

### Error History
- [ ] AC-025: History tracked
- [ ] AC-026: All attempts included
- [ ] AC-027: Error type included
- [ ] AC-028: Timestamp included
- [ ] AC-029: Redacted
- [ ] AC-030: In escalation
- [ ] AC-031: In event log
- [ ] AC-032: Bounded size

---

## User Verification Scenarios

### Scenario 1: Normal Retry Success
**Persona:** Agent with transient failure  
**Preconditions:** Cap = 3, fails twice  
**Steps:**
1. First attempt fails
2. Counter = 2
3. Second attempt fails
4. Counter = 3, succeeds

**Verification Checklist:**
- [ ] Counter incremented
- [ ] Under cap
- [ ] Retry occurred
- [ ] Eventually succeeded

### Scenario 2: Cap Exceeded
**Persona:** Agent with persistent failure  
**Preconditions:** Cap = 3  
**Steps:**
1. Three attempts fail
2. Counter = 3
3. Cap exceeded
4. Escalation triggered

**Verification Checklist:**
- [ ] All attempts logged
- [ ] Cap reached
- [ ] Escalation works
- [ ] History included

### Scenario 3: No Retry Policy
**Persona:** Agent with cap = 1  
**Preconditions:** No retry configured  
**Steps:**
1. First attempt fails
2. Counter = 1 = cap
3. Immediate escalation
4. No retry

**Verification Checklist:**
- [ ] No retry occurred
- [ ] Immediate failure
- [ ] Escalation works
- [ ] Logged

### Scenario 4: Resume After Crash
**Persona:** Agent resuming  
**Preconditions:** Crashed after 2 attempts  
**Steps:**
1. Agent restarts
2. Counter restored = 2
3. Next attempt = 3
4. If fails, cap exceeded

**Verification Checklist:**
- [ ] Counter restored
- [ ] Continues counting
- [ ] Cap still enforced
- [ ] History preserved

### Scenario 5: Per-Policy Override
**Persona:** Agent with custom policy  
**Preconditions:** Default = 3, policy = 5  
**Steps:**
1. Use custom policy
2. Five attempts allowed
3. Cap = 5
4. Escalates on 5th fail

**Verification Checklist:**
- [ ] Override applied
- [ ] 5 attempts allowed
- [ ] Escalation at 5
- [ ] Logged

### Scenario 6: Error History Review
**Persona:** Developer debugging  
**Preconditions:** Cap exceeded  
**Steps:**
1. View escalation
2. Check history
3. See all attempts
4. Diagnose issue

**Verification Checklist:**
- [ ] History present
- [ ] All attempts shown
- [ ] Errors redacted
- [ ] Timestamps correct

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-041b-01 | Counter starts at 1 | FR-041b-01 |
| UT-041b-02 | Counter increments | FR-041b-02 |
| UT-041b-03 | Counter resets | FR-041b-04 |
| UT-041b-04 | Cap enforcement | FR-041b-16 |
| UT-041b-05 | Counter >= max fails | FR-041b-17 |
| UT-041b-06 | Escalation triggered | FR-041b-19 |
| UT-041b-07 | Default cap = 3 | FR-041b-32 |
| UT-041b-08 | Per-policy cap | FR-041b-34 |
| UT-041b-09 | Cap = 1 no retry | FR-041b-38 |
| UT-041b-10 | Invalid cap error | FR-041b-40 |
| UT-041b-11 | Error history tracked | FR-041b-46 |
| UT-041b-12 | History redacted | FR-041b-51 |
| UT-041b-13 | Thread safety | NFR-041b-14 |
| UT-041b-14 | Overflow handling | FR-041b-10 |
| UT-041b-15 | Resume restore | FR-041b-14 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-041b-01 | Full cap flow | E2E |
| IT-041b-02 | Config loading | FR-041b-41 |
| IT-041b-03 | Retry integration | Task 041 |
| IT-041b-04 | Escalation integration | Task 041.c |
| IT-041b-05 | Event log integration | Task 040 |
| IT-041b-06 | Resume integration | Task 040.b |
| IT-041b-07 | Multiple policies | FR-041b-35 |
| IT-041b-08 | Logging | NFR-041b-21 |
| IT-041b-09 | Cross-platform | NFR-041b-17 |
| IT-041b-10 | Performance | NFR-041b-01 |
| IT-041b-11 | Concurrent operations | NFR-041b-09 |
| IT-041b-12 | Config error fallback | NFR-041b-20 |
| IT-041b-13 | History in escalation | FR-041b-52 |
| IT-041b-14 | Secret redaction | Task 038 |
| IT-041b-15 | High cap warning | FR-041b-45 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Retry/
│       ├── RetryCounter.cs
│       ├── AttemptHistory.cs
│       └── CapExceededException.cs
├── Acode.Application/
│   └── Retry/
│       └── IRetryCounter.cs
├── Acode.Infrastructure/
│   └── Retry/
│       └── RetryCounter.cs
```

### Configuration Schema

```yaml
retry:
  defaultMaxAttempts: 3
  policies:
    standard:
      maxAttempts: 3
    aggressive:
      maxAttempts: 5
    noRetry:
      maxAttempts: 1
    highReliability:
      maxAttempts: 10
```

### Key Implementation

```csharp
public class RetryCounter : IRetryCounter
{
    private int _count = 1;
    private readonly int _max;
    private readonly List<AttemptRecord> _history = new();
    
    public RetryCounter(int maxAttempts)
    {
        if (maxAttempts < 1)
            throw new ArgumentException("Max attempts must be >= 1");
        if (maxAttempts > 100)
            _logger.LogWarning("High retry cap: {Max}", maxAttempts);
        
        _max = maxAttempts;
    }
    
    public int Current => _count;
    public int Max => _max;
    public bool CanRetry => _count < _max;
    
    public void Increment(Exception error)
    {
        if (_count >= int.MaxValue)
            throw new OverflowException("Retry counter overflow");
        
        _history.Add(new AttemptRecord(
            Attempt: _count,
            ErrorType: error.GetType().Name,
            Message: Redact(error.Message),
            Timestamp: DateTime.UtcNow));
        
        _count++;
        
        _logger.LogDebug("Retry attempt {Count}/{Max}", _count, _max);
    }
    
    public void CheckCap()
    {
        if (_count > _max)
        {
            _logger.LogWarning("Retry cap exceeded: {Count}/{Max}", _count, _max);
            throw new CapExceededException(_count, _max, _history);
        }
    }
    
    public AttemptHistory GetHistory() => new(_history.ToList());
}
```

**End of Task 041.b Specification**
