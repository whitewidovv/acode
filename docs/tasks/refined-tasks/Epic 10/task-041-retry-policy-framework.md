# Task 041: Retry Policy Framework

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 10 – Core Reliability  
**Dependencies:** Task 040 (Event Log), Task 038 (Secrets), Task 002 (Config)  

---

## Description

Task 041 implements the retry policy framework, a systematic approach to handling transient failures in agent operations. When a tool execution, file operation, or other action fails, the retry framework determines whether to retry, how many times, with what delay, and when to give up and escalate to human intervention.

The retry framework is essential for a reliable agent. Network timeouts, locked files, transient system errors—these failures are common and usually resolve on retry. But unlimited retries waste time and hide real problems. The framework balances persistence with pragmatism, retrying when likely to succeed and escalating when human judgment is needed.

Retry policies are configurable per operation type. Some operations (like network requests) benefit from aggressive retry with exponential backoff. Others (like permission errors) should not retry at all. The framework categorizes failures and applies appropriate policies, all while logging each attempt for debugging and audit.

The framework also implements circuit-breaker patterns. If the same operation fails repeatedly across multiple invocations, the framework can preemptively fail fast rather than waste time on doomed retries. This protects against cascading failures and resource exhaustion.

### Business Value

Retry policy framework provides:
- Automatic transient failure recovery
- Predictable failure handling
- Appropriate human escalation
- Resource protection
- Debugging transparency

### Scope Boundaries

This task covers the core retry framework. Failure categorization is Task 041.a. Retry cap enforcement is Task 041.b. Human escalation rules are Task 041.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Event Log | Task 040 | Log attempts | Audit |
| Config | Task 002 | Policy config | YAML |
| Tool Executor | `IToolExecutor` | Retry wrapper | Core use |
| File Ops | File system | Retry wrapper | Core use |
| Resume Engine | Task 040.b | Retry count | Resume |
| Human Escalation | Task 041.c | Final failure | Escalate |
| CLI | Task 000 | Retry status | Display |
| Secrets | Task 038 | Redact errors | Logging |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Retry exhausted | Counter | Escalate | Human needed |
| Infinite loop | Circuit breaker | Fail fast | Error |
| Policy not found | Lookup failure | Use default | Warning |
| Config invalid | Validation | Error + default | Warning |
| Timeout in retry | Timer | Next attempt | Delay |
| Memory exhaustion | Monitor | Abort | Error |
| Retry storm | Rate limiting | Throttle | Delay |
| State corruption | Validation | Error | Human needed |

### Assumptions

1. **Most failures are transient**: Network, locks
2. **Retry count reasonable**: 3-5 typical
3. **Backoff helps**: Exponential
4. **Categories known**: Per operation type
5. **Config available**: Task 002
6. **Logging works**: Task 040
7. **Human available**: For escalation
8. **Retry is idempotent**: No side effects

### Security Considerations

1. **Error messages redacted**: No secrets
2. **Retry log redacted**: Task 038
3. **No retry on auth failures**: Immediate fail
4. **Retry count in audit**: Full history
5. **Circuit breaker logged**: Security event
6. **Rate limiting prevents abuse**: DoS protection
7. **Escalation is authenticated**: User verified
8. **Retry state not exposed**: Internal only

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Retry Policy | Rules for retry behavior |
| Backoff | Delay between retries |
| Exponential Backoff | Doubling delay |
| Jitter | Random delay component |
| Circuit Breaker | Fail-fast pattern |
| Transient Failure | Temporary error |
| Permanent Failure | Non-retryable error |
| Retry Budget | Limited attempts |
| Escalation | Human intervention |
| Retry Storm | Many concurrent retries |

---

## Out of Scope

- Distributed retry coordination
- Cross-agent retry sharing
- Retry analytics dashboard
- ML-based retry prediction
- Retry cost optimization
- External retry service

---

## Functional Requirements

### FR-001 to FR-020: Core Retry Framework

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041-01 | Retry policy MUST be definable | P0 |
| FR-041-02 | Policy MUST specify max attempts | P0 |
| FR-041-03 | Policy MUST specify backoff type | P0 |
| FR-041-04 | Policy MUST specify initial delay | P0 |
| FR-041-05 | Policy MUST specify max delay | P0 |
| FR-041-06 | Policy MUST specify jitter | P1 |
| FR-041-07 | Policy MUST specify retryable errors | P0 |
| FR-041-08 | Policy MUST specify non-retryable errors | P0 |
| FR-041-09 | Default policy MUST exist | P0 |
| FR-041-10 | Default MUST be configurable | P1 |
| FR-041-11 | Per-operation policy MUST override | P0 |
| FR-041-12 | Policy lookup MUST be fast | P0 |
| FR-041-13 | Policy MUST be immutable at runtime | P0 |
| FR-041-14 | Policy validation MUST occur on load | P0 |
| FR-041-15 | Invalid policy MUST error | P0 |
| FR-041-16 | Policy MUST have name | P0 |
| FR-041-17 | Policy MUST be serializable | P1 |
| FR-041-18 | Policy MUST support composition | P2 |
| FR-041-19 | Policy registry MUST exist | P0 |
| FR-041-20 | Registry MUST be queryable | P1 |

### FR-021 to FR-035: Retry Execution

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041-21 | Retry wrapper MUST execute operation | P0 |
| FR-041-22 | Retry wrapper MUST catch exceptions | P0 |
| FR-041-23 | Retry wrapper MUST check retryability | P0 |
| FR-041-24 | Retryable MUST trigger retry | P0 |
| FR-041-25 | Non-retryable MUST throw immediately | P0 |
| FR-041-26 | Retry MUST wait for backoff | P0 |
| FR-041-27 | Backoff MUST apply jitter | P1 |
| FR-041-28 | Retry counter MUST increment | P0 |
| FR-041-29 | Counter MUST check max | P0 |
| FR-041-30 | Max exceeded MUST fail | P0 |
| FR-041-31 | Each attempt MUST be logged | P0 |
| FR-041-32 | Final success MUST be logged | P0 |
| FR-041-33 | Final failure MUST be logged | P0 |
| FR-041-34 | Retry MUST be cancellable | P0 |
| FR-041-35 | Cancellation MUST be immediate | P0 |

### FR-036 to FR-050: Backoff Strategies

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041-36 | Constant backoff MUST be supported | P0 |
| FR-041-37 | Linear backoff MUST be supported | P1 |
| FR-041-38 | Exponential backoff MUST be supported | P0 |
| FR-041-39 | Exponential factor MUST be configurable | P1 |
| FR-041-40 | Default factor MUST be 2.0 | P1 |
| FR-041-41 | Max delay MUST cap backoff | P0 |
| FR-041-42 | Jitter MUST be percentage | P1 |
| FR-041-43 | Default jitter MUST be 10% | P1 |
| FR-041-44 | Full jitter MUST be supported | P2 |
| FR-041-45 | Decorrelated jitter MUST be supported | P2 |
| FR-041-46 | Backoff MUST be computed | P0 |
| FR-041-47 | Computed delay MUST be logged | P1 |
| FR-041-48 | Zero delay MUST be allowed | P1 |
| FR-041-49 | Immediate retry MUST work | P1 |
| FR-041-50 | Backoff MUST be deterministic given seed | P2 |

### FR-051 to FR-065: Circuit Breaker

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041-51 | Circuit breaker MUST be optional | P1 |
| FR-041-52 | Breaker MUST track failure count | P1 |
| FR-041-53 | Breaker MUST track success count | P1 |
| FR-041-54 | Failure threshold MUST trigger open | P1 |
| FR-041-55 | Open circuit MUST fail fast | P1 |
| FR-041-56 | Open duration MUST be configurable | P1 |
| FR-041-57 | Half-open MUST allow probe | P1 |
| FR-041-58 | Probe success MUST close circuit | P1 |
| FR-041-59 | Probe failure MUST re-open | P1 |
| FR-041-60 | Circuit state MUST be logged | P1 |
| FR-041-61 | Circuit reset MUST be possible | P1 |
| FR-041-62 | Per-operation circuit MUST be supported | P2 |
| FR-041-63 | Global circuit MUST be supported | P2 |
| FR-041-64 | Circuit MUST be queryable | P1 |
| FR-041-65 | Circuit MUST persist across restart | P2 |

### FR-066 to FR-075: Configuration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-041-66 | Config MUST be in agent/config.yml | P0 |
| FR-041-67 | Retry section MUST exist | P0 |
| FR-041-68 | Named policies MUST be defined | P0 |
| FR-041-69 | Operation-policy mapping MUST exist | P0 |
| FR-041-70 | Validation MUST occur on startup | P0 |
| FR-041-71 | Validation errors MUST be clear | P0 |
| FR-041-72 | Hot reload MUST NOT be supported | P1 |
| FR-041-73 | Env var override MUST be supported | P2 |
| FR-041-74 | CLI override MUST be supported | P2 |
| FR-041-75 | Config schema MUST be documented | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-041-01 | Policy lookup | <1ms | P0 |
| NFR-041-02 | Retry overhead | <5ms | P0 |
| NFR-041-03 | Backoff compute | <1ms | P0 |
| NFR-041-04 | Circuit check | <1ms | P1 |
| NFR-041-05 | Log write | <5ms | P0 |
| NFR-041-06 | Config load | <100ms | P0 |
| NFR-041-07 | Memory per policy | <1KB | P2 |
| NFR-041-08 | Memory per circuit | <10KB | P2 |
| NFR-041-09 | Concurrent retries | 100+ | P1 |
| NFR-041-10 | Registry size | 1000 policies | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-041-11 | Retry logic correct | 100% | P0 |
| NFR-041-12 | Backoff correct | 100% | P0 |
| NFR-041-13 | Circuit correct | 100% | P1 |
| NFR-041-14 | No retry leak | 100% | P0 |
| NFR-041-15 | Thread safety | No races | P0 |
| NFR-041-16 | Exception propagation | Correct | P0 |
| NFR-041-17 | Cancellation works | 100% | P0 |
| NFR-041-18 | Cross-platform | All OS | P0 |
| NFR-041-19 | Deterministic | Given seed | P2 |
| NFR-041-20 | Graceful degradation | On config error | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-041-21 | Each attempt logged | Debug | P0 |
| NFR-041-22 | Final result logged | Info | P0 |
| NFR-041-23 | Circuit state logged | Info | P1 |
| NFR-041-24 | Config logged | Debug | P1 |
| NFR-041-25 | Metrics: retry count | Counter | P2 |
| NFR-041-26 | Metrics: success rate | Gauge | P2 |
| NFR-041-27 | Metrics: circuit state | Gauge | P2 |
| NFR-041-28 | Structured logging | JSON | P0 |
| NFR-041-29 | Error redaction | Task 038 | P0 |
| NFR-041-30 | Health check | Circuit state | P1 |

---

## Acceptance Criteria / Definition of Done

### Core Framework
- [ ] AC-001: Policy definable
- [ ] AC-002: Max attempts configurable
- [ ] AC-003: Backoff type configurable
- [ ] AC-004: Initial delay configurable
- [ ] AC-005: Max delay configurable
- [ ] AC-006: Jitter configurable
- [ ] AC-007: Default policy exists
- [ ] AC-008: Per-operation override

### Retry Execution
- [ ] AC-009: Operation executed
- [ ] AC-010: Exception caught
- [ ] AC-011: Retryability checked
- [ ] AC-012: Retry triggered
- [ ] AC-013: Non-retryable throws
- [ ] AC-014: Backoff applied
- [ ] AC-015: Counter incremented
- [ ] AC-016: Max enforced

### Backoff
- [ ] AC-017: Constant works
- [ ] AC-018: Exponential works
- [ ] AC-019: Factor configurable
- [ ] AC-020: Max caps delay
- [ ] AC-021: Jitter applied
- [ ] AC-022: Delay logged
- [ ] AC-023: Zero allowed
- [ ] AC-024: Deterministic

### Circuit Breaker
- [ ] AC-025: Failure tracked
- [ ] AC-026: Threshold triggers open
- [ ] AC-027: Open fails fast
- [ ] AC-028: Half-open probes
- [ ] AC-029: Success closes
- [ ] AC-030: State logged
- [ ] AC-031: Queryable
- [ ] AC-032: Resetable

---

## User Verification Scenarios

### Scenario 1: Transient Failure Recovery
**Persona:** Agent executing tool  
**Preconditions:** Network unstable  
**Steps:**
1. Tool fails first attempt
2. Retry triggered
3. Backoff applied
4. Second attempt succeeds

**Verification Checklist:**
- [ ] Failure detected
- [ ] Retry triggered
- [ ] Delay applied
- [ ] Success logged

### Scenario 2: Max Retries Exceeded
**Persona:** Agent with persistent failure  
**Preconditions:** Operation broken  
**Steps:**
1. All attempts fail
2. Counter reaches max
3. Final failure raised
4. Escalation triggered

**Verification Checklist:**
- [ ] All attempts logged
- [ ] Max reached
- [ ] Clear error
- [ ] Escalation works

### Scenario 3: Non-Retryable Error
**Persona:** Agent with permission error  
**Preconditions:** Access denied  
**Steps:**
1. Operation fails
2. Error categorized
3. Non-retryable detected
4. Immediate failure

**Verification Checklist:**
- [ ] No retry attempted
- [ ] Immediate throw
- [ ] Category logged
- [ ] Clear error

### Scenario 4: Circuit Breaker Opens
**Persona:** Agent with repeated failures  
**Preconditions:** Service down  
**Steps:**
1. Multiple operations fail
2. Threshold reached
3. Circuit opens
4. Fail fast begins

**Verification Checklist:**
- [ ] Failures counted
- [ ] Circuit opens
- [ ] Fast failures
- [ ] State logged

### Scenario 5: Circuit Recovery
**Persona:** Agent after service restored  
**Preconditions:** Circuit open  
**Steps:**
1. Timeout expires
2. Half-open state
3. Probe succeeds
4. Circuit closes

**Verification Checklist:**
- [ ] Half-open triggered
- [ ] Probe executed
- [ ] Circuit closes
- [ ] Normal operation

### Scenario 6: Cancellation
**Persona:** User canceling operation  
**Preconditions:** Retry in progress  
**Steps:**
1. User sends Ctrl+C
2. Retry cancels
3. Resources cleaned
4. Clean exit

**Verification Checklist:**
- [ ] Cancellation detected
- [ ] Immediate stop
- [ ] No resource leak
- [ ] Logged

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-041-01 | Policy creation | FR-041-01 |
| UT-041-02 | Max attempts | FR-041-02 |
| UT-041-03 | Backoff types | FR-041-36 |
| UT-041-04 | Exponential factor | FR-041-39 |
| UT-041-05 | Max delay cap | FR-041-41 |
| UT-041-06 | Jitter calculation | FR-041-42 |
| UT-041-07 | Retry wrapper | FR-041-21 |
| UT-041-08 | Retryability check | FR-041-23 |
| UT-041-09 | Counter increment | FR-041-28 |
| UT-041-10 | Max exceeded | FR-041-30 |
| UT-041-11 | Circuit failure count | FR-041-52 |
| UT-041-12 | Circuit open | FR-041-54 |
| UT-041-13 | Circuit half-open | FR-041-57 |
| UT-041-14 | Cancellation | FR-041-34 |
| UT-041-15 | Policy registry | FR-041-19 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-041-01 | Full retry flow | E2E |
| IT-041-02 | Config loading | FR-041-66 |
| IT-041-03 | Event log integration | Task 040 |
| IT-041-04 | Multiple policies | FR-041-11 |
| IT-041-05 | Circuit persistence | FR-041-65 |
| IT-041-06 | Concurrent retries | NFR-041-09 |
| IT-041-07 | Backoff timing | FR-041-26 |
| IT-041-08 | Error redaction | Task 038 |
| IT-041-09 | CLI integration | Task 000 |
| IT-041-10 | Resume integration | Task 040.b |
| IT-041-11 | Escalation integration | Task 041.c |
| IT-041-12 | Performance | NFR-041-01 |
| IT-041-13 | Cross-platform | NFR-041-18 |
| IT-041-14 | Logging | NFR-041-21 |
| IT-041-15 | Cancellation | FR-041-34 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Retry/
│       ├── RetryPolicy.cs
│       ├── BackoffStrategy.cs
│       ├── CircuitState.cs
│       └── RetryResult.cs
├── Acode.Application/
│   └── Retry/
│       ├── IRetryExecutor.cs
│       ├── ICircuitBreaker.cs
│       ├── IPolicyRegistry.cs
│       └── RetryOptions.cs
├── Acode.Infrastructure/
│   └── Retry/
│       ├── RetryExecutor.cs
│       ├── CircuitBreaker.cs
│       ├── PolicyRegistry.cs
│       └── BackoffCalculator.cs
```

### Configuration Schema

```yaml
retry:
  defaultPolicy: standard
  policies:
    standard:
      maxAttempts: 3
      backoff: exponential
      initialDelayMs: 100
      maxDelayMs: 5000
      factor: 2.0
      jitterPercent: 10
    aggressive:
      maxAttempts: 5
      backoff: exponential
      initialDelayMs: 50
      maxDelayMs: 30000
      factor: 2.0
      jitterPercent: 20
    noRetry:
      maxAttempts: 1
  
  operationPolicies:
    network: aggressive
    file: standard
    permission: noRetry
  
  circuitBreaker:
    enabled: true
    failureThreshold: 5
    openDurationMs: 30000
    halfOpenProbes: 1
```

### Key Implementation

```csharp
public class RetryExecutor : IRetryExecutor
{
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        string operationType,
        CancellationToken ct = default)
    {
        var policy = _registry.GetPolicy(operationType);
        var attempt = 0;
        
        while (true)
        {
            attempt++;
            
            if (_circuitBreaker?.IsOpen(operationType) == true)
            {
                throw new CircuitOpenException(operationType);
            }
            
            try
            {
                var result = await operation(ct);
                _circuitBreaker?.RecordSuccess(operationType);
                _logger.LogDebug("Operation {Op} succeeded on attempt {Attempt}",
                    operationType, attempt);
                return result;
            }
            catch (Exception ex) when (ShouldRetry(ex, policy, attempt))
            {
                _circuitBreaker?.RecordFailure(operationType);
                
                var delay = _backoff.Calculate(policy, attempt);
                _logger.LogDebug("Retry {Attempt}/{Max} for {Op}, delay {Delay}ms",
                    attempt, policy.MaxAttempts, operationType, delay);
                
                await Task.Delay(delay, ct);
            }
            catch (Exception ex)
            {
                _circuitBreaker?.RecordFailure(operationType);
                _logger.LogError(ex, "Operation {Op} failed permanently after {Attempt} attempts",
                    operationType, attempt);
                throw;
            }
        }
    }
    
    private bool ShouldRetry(Exception ex, RetryPolicy policy, int attempt)
    {
        if (attempt >= policy.MaxAttempts)
            return false;
        
        return _classifier.IsRetryable(ex, policy);
    }
}
```

**End of Task 041 Specification**
