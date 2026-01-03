# Task 009.c: Fallback Escalation Rules

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 009, Task 009.a, Task 009.b, Task 004 (Model Provider Interface)  

---

## Description

Task 009.c defines the fallback escalation rules that handle model unavailability and routing failures. When a requested model is unavailable, unreachable, or fails to respond, the escalation system provides alternative models to maintain workflow continuity. This resilience is critical for uninterrupted operation in local environments where model availability may vary.

Fallback chains define ordered lists of alternative models. Each role can have its own fallback chain, or a global chain can apply to all roles. When the primary model is unavailable, the system tries the first fallback. If that fails, it tries the next, continuing until a working model is found or the chain is exhausted.

Escalation triggers define when fallback occurs. Model unavailability (server not running) is the primary trigger. Request timeout (model too slow) is another trigger. Repeated errors (model returning invalid responses) can also trigger escalation. Each trigger has configurable thresholds.

Escalation policy determines behavior. The "immediate" policy tries fallback on first failure. The "retry-then-fallback" policy retries the primary model before falling back. The "circuit-breaker" policy remembers failures and skips problematic models temporarily. Users choose the policy matching their needs.

The circuit breaker pattern prevents repeated failures. If a model fails multiple times in succession, it is temporarily removed from consideration. After a cooling period, the model is retried. This prevents wasting time on consistently failing models while allowing recovery when issues are transient.

Escalation scope controls how far fallback extends. "Role-scoped" fallback stays within the role's model tier. "Global-scoped" fallback can cross tiers. A planner might fall back from a large model to a medium model if allowed. Scope prevents inappropriate downgrades that would harm quality.

Notification and logging make escalation visible. Each fallback event is logged with details—which model failed, why, which fallback was selected. This visibility helps users understand when their preferred models have issues and take corrective action.

User feedback during escalation is important. When the agent falls back to an alternative model, it can inform the user. This transparency sets expectations—a smaller fallback model might produce lower quality output. Users can then decide whether to continue or address the model issue.

Graceful degradation when all fallbacks fail is the last resort. If no models are available, the agent cannot proceed with inference-dependent tasks. It can still perform file operations and other non-model tasks. The error message clearly explains the situation and suggests remediation.

Configuration provides flexible control. The `.agent/config.yml` file defines fallback chains per role or globally. Environment-specific configurations allow different fallback strategies for development versus production. Defaults provide reasonable behavior without configuration.

Integration with the provider registry ensures accurate availability information. Before trying a fallback, the system checks whether the model is actually available. This prevents cascading through the entire chain when all models are down, failing fast with a helpful error.

Escalation respects operating mode constraints. In air-gapped mode, fallback cannot include network-dependent models. In local-only mode, cloud models are never in the fallback chain even if configured. Mode constraints are enforced at every level of the escalation process.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Fallback | Alternative when primary fails |
| Fallback Chain | Ordered list of alternative models |
| Escalation | Process of moving to fallback |
| Escalation Trigger | Condition that initiates fallback |
| Escalation Policy | Strategy for handling failures |
| Immediate Policy | Fallback on first failure |
| Retry Policy | Retry before fallback |
| Circuit Breaker | Temporarily disable failing model |
| Cooling Period | Time before retrying failed model |
| Escalation Scope | How far fallback can go |
| Role-Scoped | Fallback within same tier |
| Global-Scoped | Fallback across tiers |
| Model Unavailable | Model server not responding |
| Request Timeout | Model too slow to respond |
| Graceful Degradation | Behavior when all fail |
| Recovery | Model becoming available again |
| Failure Count | Number of consecutive failures |
| Failure Threshold | Failures before circuit break |

---

## Out of Scope

The following items are explicitly excluded from Task 009.c:

- **Role definitions** - Covered in Task 009.a
- **Heuristics and overrides** - Covered in Task 009.b
- **Model provider logic** - Covered in Tasks 004-006
- **Automatic model healing** - Not in MVP
- **Model health monitoring** - Covered in provider tasks
- **Multi-cluster failover** - Not applicable (local)
- **Cost-based fallback ordering** - Not applicable
- **Performance-based reordering** - Post-MVP
- **Fallback prediction** - Post-MVP
- **Hot-swap model loading** - Future enhancement

---

## Functional Requirements

### IFallbackHandler Interface

- FR-001: Interface MUST be in Application layer
- FR-002: MUST have GetFallback(role, context) method
- FR-003: MUST return FallbackResult
- FR-004: FallbackResult MUST include model if found
- FR-005: FallbackResult MUST include reason
- FR-006: MUST have NotifyFailure(model, error) method
- FR-007: MUST have IsCircuitOpen(model) method

### FallbackHandler Implementation

- FR-008: Implementation MUST be in Infrastructure layer
- FR-009: MUST read fallback chains from config
- FR-010: MUST support per-role chains
- FR-011: MUST support global chain
- FR-012: Role chain MUST take precedence over global
- FR-013: MUST check model availability before selection

### Fallback Chain Configuration

- FR-014: Config section: models.fallback
- FR-015: fallback.global MUST define global chain
- FR-016: fallback.roles.{role} MUST define role chains
- FR-017: Chain MUST be ordered array of model IDs
- FR-018: Empty chain MUST use global fallback
- FR-019: Chain MUST respect mode constraints

### Escalation Triggers

- FR-020: Model unavailable MUST trigger escalation
- FR-021: Request timeout MUST trigger escalation
- FR-022: Repeated errors MUST trigger escalation
- FR-023: Trigger threshold MUST be configurable
- FR-024: Default timeout MUST be 60 seconds
- FR-025: Default error threshold MUST be 3

### Escalation Policies

- FR-026: MUST support "immediate" policy
- FR-027: MUST support "retry-then-fallback" policy
- FR-028: MUST support "circuit-breaker" policy
- FR-029: Default policy MUST be "retry-then-fallback"
- FR-030: Policy MUST be configurable per role
- FR-031: Retry count MUST be configurable

### Immediate Policy

- FR-032: MUST fall back on first failure
- FR-033: No retries before fallback
- FR-034: Fastest recovery path

### Retry-Then-Fallback Policy

- FR-035: MUST retry primary before fallback
- FR-036: Default retries MUST be 2
- FR-037: Retry delay MUST be configurable
- FR-038: Default delay MUST be 1 second
- FR-039: Exponential backoff MUST be supported

### Circuit Breaker Policy

- FR-040: MUST track failure counts per model
- FR-041: MUST open circuit after threshold
- FR-042: Default threshold MUST be 5 failures
- FR-043: Open circuit MUST skip model
- FR-044: MUST implement half-open state
- FR-045: Cooling period MUST be configurable
- FR-046: Default cooling MUST be 60 seconds
- FR-047: Successful request MUST close circuit

### Escalation Scope

- FR-048: MUST support "role-scoped" scope
- FR-049: MUST support "global-scoped" scope
- FR-050: Default scope MUST be "role-scoped"
- FR-051: Role-scoped MUST stay in tier
- FR-052: Global-scoped MAY cross tiers

### Chain Exhaustion

- FR-053: MUST handle all fallbacks exhausted
- FR-054: Exhausted MUST return failure result
- FR-055: Failure MUST include all tried models
- FR-056: Failure MUST include failure reasons
- FR-057: Graceful degradation MUST be triggered

### Logging

- FR-058: Escalation MUST be logged as WARNING
- FR-059: Log MUST include original model
- FR-060: Log MUST include fallback model
- FR-061: Log MUST include trigger reason
- FR-062: Circuit events MUST be logged
- FR-063: Exhaustion MUST be logged as ERROR

### CLI Integration

- FR-064: `acode fallback status` MUST show state
- FR-065: MUST show configured chains
- FR-066: MUST show circuit breaker state
- FR-067: `acode fallback reset` MUST reset circuits
- FR-068: `acode fallback test` MUST test chain

### User Notification

- FR-069: Fallback MAY notify user
- FR-070: Notification MUST be opt-in
- FR-071: Config: fallback.notify_user
- FR-072: Default MUST be false

---

## Non-Functional Requirements

### Performance

- NFR-001: Fallback selection MUST complete < 10ms
- NFR-002: Circuit check MUST complete < 1ms
- NFR-003: Availability check MUST timeout at 5s
- NFR-004: State MUST be cached in memory

### Reliability

- NFR-005: Fallback MUST not crash on failure
- NFR-006: Circuit state MUST persist in session
- NFR-007: Recovery MUST be automatic
- NFR-008: Concurrent access MUST be thread-safe

### Security

- NFR-009: Mode constraints MUST be enforced
- NFR-010: Chain MUST be validated
- NFR-011: No sensitive data in logs

### Observability

- NFR-012: All escalations MUST be logged
- NFR-013: Circuit state MUST be queryable
- NFR-014: Metrics SHOULD track fallback rate
- NFR-015: Health endpoint SHOULD show state

### Maintainability

- NFR-016: Policies MUST be pluggable
- NFR-017: New triggers MUST be addable
- NFR-018: All public APIs MUST have XML docs
- NFR-019: Tests MUST cover all policies

---

## User Manual Documentation

### Overview

Fallback escalation ensures the agent continues working when preferred models are unavailable. This guide covers fallback configuration, policies, and troubleshooting.

### Quick Start

Configure a global fallback chain:

```yaml
# .agent/config.yml
models:
  fallback:
    global:
      - llama3.2:70b
      - llama3.2:7b
      - mistral:7b
```

### How Fallback Works

1. Agent requests model for a role
2. Primary model is checked for availability
3. If unavailable, first fallback is tried
4. Process continues until working model found
5. If all fail, error is returned

### Fallback Configuration

#### Global Chain

Applies to all roles:

```yaml
models:
  fallback:
    global:
      - llama3.2:70b   # First fallback
      - llama3.2:7b    # Second fallback
      - mistral:7b     # Last resort
```

#### Per-Role Chains

Different chains for different roles:

```yaml
models:
  fallback:
    roles:
      planner:
        - llama3.2:70b
        - mistral:7b
      coder:
        - llama3.2:7b
        - qwen2:7b
      reviewer:
        - llama3.2:70b
        - llama3.2:7b
```

#### Complete Configuration

```yaml
models:
  fallback:
    # Escalation policy
    policy: retry-then-fallback
    
    # Policy settings
    retries: 2
    retry_delay_ms: 1000
    timeout_ms: 60000
    
    # Circuit breaker
    circuit_breaker:
      enabled: true
      failure_threshold: 5
      cooling_period_ms: 60000
    
    # Notification
    notify_user: false
    
    # Scope
    scope: role-scoped
    
    # Chains
    global:
      - llama3.2:7b
    
    roles:
      planner:
        - llama3.2:70b
        - llama3.2:7b
```

### Escalation Policies

#### Immediate Policy

Fastest recovery—fallback on first failure:

```yaml
models:
  fallback:
    policy: immediate
```

**Use when:** Speed matters more than persistence.

#### Retry-Then-Fallback Policy

Try primary model several times before falling back:

```yaml
models:
  fallback:
    policy: retry-then-fallback
    retries: 2
    retry_delay_ms: 1000
```

**Use when:** Transient failures are common (default).

#### Circuit Breaker Policy

Skip consistently failing models temporarily:

```yaml
models:
  fallback:
    policy: circuit-breaker
    circuit_breaker:
      failure_threshold: 5    # Failures before opening
      cooling_period_ms: 60000  # Time before retry
```

**Use when:** Models may fail persistently then recover.

### Escalation Triggers

| Trigger | Description | Default Threshold |
|---------|-------------|-------------------|
| Model Unavailable | Server not responding | Immediate |
| Request Timeout | Response too slow | 60 seconds |
| Repeated Errors | Invalid responses | 3 errors |

Configure thresholds:

```yaml
models:
  fallback:
    timeout_ms: 30000          # 30s timeout
    error_threshold: 5         # 5 errors before escalate
```

### Circuit Breaker

The circuit breaker prevents repeatedly trying failing models:

**States:**
- **Closed** - Normal operation, requests go through
- **Open** - Model is skipped, using fallback
- **Half-Open** - Testing if model recovered

```
Failure 1 → Failure 2 → ... → Failure 5 → CIRCUIT OPENS
                                              ↓
                                         (60s cooling)
                                              ↓
                                        HALF-OPEN
                                              ↓
                                   Success → CLOSED
                                   Failure → OPEN
```

### CLI Commands

```bash
# Show fallback status
$ acode fallback status
Fallback Configuration:
  Policy: retry-then-fallback
  Scope: role-scoped

Global Chain:
  1. llama3.2:7b (available)

Role Chains:
  planner:
    1. llama3.2:70b (available)
    2. llama3.2:7b (available)

Circuit Breaker State:
  llama3.2:70b: CLOSED (0 failures)
  llama3.2:7b: CLOSED (0 failures)

# Reset circuit breakers
$ acode fallback reset
Circuit breakers reset.

# Test fallback chain
$ acode fallback test planner
Testing fallback chain for 'planner':
  llama3.2:70b: OK (45ms)
  llama3.2:7b: OK (32ms)
Chain is healthy.
```

### Logs

Fallback events in logs:

```
[WARN] Model escalation triggered
  Original: llama3.2:70b
  Fallback: llama3.2:7b
  Reason: request_timeout (65s > 60s limit)
  Role: planner

[WARN] Circuit opened for model
  Model: llama3.2:70b
  Failures: 5
  Cooling: 60s

[ERROR] All fallbacks exhausted
  Role: planner
  Tried: llama3.2:70b, llama3.2:7b, mistral:7b
  Error: No available models
```

### Graceful Degradation

When all models fail:

```
[ERROR] No models available for inference

The agent cannot perform inference tasks because no models are available.

Tried:
  - llama3.2:70b: unavailable (circuit open)
  - llama3.2:7b: timeout
  - mistral:7b: not loaded

Suggestions:
  1. Start a model: ollama run llama3.2:7b
  2. Reset circuit breakers: acode fallback reset
  3. Check model server: ollama list
```

### Best Practices

1. **Always configure fallback** - Never rely on single model
2. **Order by preference** - Best models first
3. **Include smaller models** - Fast fallbacks when needed
4. **Monitor circuit state** - Check `acode fallback status`
5. **Keep models loaded** - Pre-load fallback models

### Troubleshooting

#### Constant Fallback

```
Every request uses fallback model
```

**Cause:** Primary model consistently unavailable.  
**Solution:** Check model server, reset circuit breakers.

#### Chain Exhausted

```
All fallbacks exhausted, no model available
```

**Cause:** All configured models are unavailable.  
**Solution:** Start at least one model from the chain.

#### Circuit Stuck Open

```
Model available but circuit still open
```

**Cause:** Cooling period not elapsed.  
**Solution:** Wait for cooling, or `acode fallback reset`.

---

## Acceptance Criteria

### Interface

- [ ] AC-001: IFallbackHandler in Application
- [ ] AC-002: GetFallback method exists
- [ ] AC-003: Returns FallbackResult
- [ ] AC-004: Result has model
- [ ] AC-005: Result has reason
- [ ] AC-006: NotifyFailure method exists
- [ ] AC-007: IsCircuitOpen method exists

### Implementation

- [ ] AC-008: FallbackHandler in Infrastructure
- [ ] AC-009: Reads from config
- [ ] AC-010: Supports per-role chains
- [ ] AC-011: Supports global chain
- [ ] AC-012: Role precedence over global
- [ ] AC-013: Checks availability

### Configuration

- [ ] AC-014: models.fallback section
- [ ] AC-015: fallback.global works
- [ ] AC-016: fallback.roles works
- [ ] AC-017: Chain is ordered array
- [ ] AC-018: Empty chain uses global
- [ ] AC-019: Mode constraints respected

### Triggers

- [ ] AC-020: Unavailable triggers
- [ ] AC-021: Timeout triggers
- [ ] AC-022: Errors trigger
- [ ] AC-023: Thresholds configurable
- [ ] AC-024: Default timeout 60s
- [ ] AC-025: Default errors 3

### Policies

- [ ] AC-026: Immediate policy works
- [ ] AC-027: Retry policy works
- [ ] AC-028: Circuit breaker works
- [ ] AC-029: Default is retry
- [ ] AC-030: Per-role configurable

### Circuit Breaker

- [ ] AC-031: Tracks failures
- [ ] AC-032: Opens after threshold
- [ ] AC-033: Default threshold 5
- [ ] AC-034: Skips open circuits
- [ ] AC-035: Half-open state works
- [ ] AC-036: Cooling configurable
- [ ] AC-037: Default cooling 60s
- [ ] AC-038: Success closes circuit

### Chain Exhaustion

- [ ] AC-039: Handles exhaustion
- [ ] AC-040: Returns failure result
- [ ] AC-041: Includes tried models
- [ ] AC-042: Includes reasons
- [ ] AC-043: Graceful degradation

### CLI

- [ ] AC-044: status command works
- [ ] AC-045: Shows chains
- [ ] AC-046: Shows circuit state
- [ ] AC-047: reset command works
- [ ] AC-048: test command works

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Application/Fallback/
├── FallbackHandlerTests.cs
│   ├── Should_Return_First_Available_Fallback()
│   ├── Should_Skip_Unavailable_Models()
│   ├── Should_Use_Role_Chain_First()
│   └── Should_Fall_To_Global_Chain()
│
├── CircuitBreakerTests.cs
│   ├── Should_Track_Failures()
│   ├── Should_Open_After_Threshold()
│   ├── Should_Skip_Open_Circuit()
│   ├── Should_Enter_HalfOpen()
│   └── Should_Close_On_Success()
│
└── EscalationPolicyTests.cs
    ├── Should_Apply_Immediate_Policy()
    ├── Should_Retry_Then_Fallback()
    └── Should_Use_Circuit_Breaker_Policy()
```

### Integration Tests

```
Tests/Integration/Fallback/
├── FallbackIntegrationTests.cs
│   ├── Should_Fallback_On_Unavailable()
│   ├── Should_Fallback_On_Timeout()
│   └── Should_Recover_When_Available()
```

### E2E Tests

```
Tests/E2E/Fallback/
├── FallbackE2ETests.cs
│   ├── Should_Continue_With_Fallback()
│   └── Should_Fail_Gracefully_When_Exhausted()
```

### Performance Tests

- PERF-001: Fallback selection < 10ms
- PERF-002: Circuit check < 1ms
- PERF-003: Availability timeout 5s

---

## User Verification Steps

### Scenario 1: View Status

1. Run `acode fallback status`
2. Verify: Chains shown
3. Verify: Circuit state shown

### Scenario 2: Fallback Trigger

1. Stop primary model
2. Make request
3. Verify: Fallback used

### Scenario 3: Circuit Opens

1. Configure circuit breaker
2. Cause multiple failures
3. Verify: Circuit opens

### Scenario 4: Circuit Recovery

1. Wait for cooling period
2. Start model
3. Make request
4. Verify: Circuit closes

### Scenario 5: Reset Circuits

1. Open a circuit
2. Run `acode fallback reset`
3. Verify: Circuit closed

### Scenario 6: Chain Exhausted

1. Stop all models
2. Make request
3. Verify: Graceful error

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/Fallback/
├── IFallbackHandler.cs
├── FallbackResult.cs
├── EscalationTrigger.cs
└── FallbackConfiguration.cs

src/AgenticCoder.Infrastructure/Fallback/
├── FallbackHandler.cs
├── CircuitBreaker.cs
├── ImmediatePolicy.cs
├── RetryPolicy.cs
├── CircuitBreakerPolicy.cs
└── FallbackChainResolver.cs
```

### IFallbackHandler Interface

```csharp
namespace AgenticCoder.Application.Fallback;

public interface IFallbackHandler
{
    FallbackResult GetFallback(AgentRole role, FallbackContext context);
    void NotifyFailure(string modelId, Exception error);
    bool IsCircuitOpen(string modelId);
    void ResetCircuit(string modelId);
    void ResetAllCircuits();
}

public sealed class FallbackResult
{
    public required bool Success { get; init; }
    public string? ModelId { get; init; }
    public required string Reason { get; init; }
    public IReadOnlyList<string>? TriedModels { get; init; }
}
```

### CircuitBreaker Class

```csharp
namespace AgenticCoder.Infrastructure.Fallback;

public sealed class CircuitBreaker
{
    private int _failureCount;
    private DateTimeOffset _lastFailure;
    private CircuitState _state;
    
    public CircuitState State => _state;
    
    public void RecordFailure()
    {
        _failureCount++;
        _lastFailure = DateTimeOffset.UtcNow;
        if (_failureCount >= _threshold)
            _state = CircuitState.Open;
    }
    
    public void RecordSuccess()
    {
        _failureCount = 0;
        _state = CircuitState.Closed;
    }
    
    public bool ShouldAllow()
    {
        if (_state == CircuitState.Closed)
            return true;
        if (_state == CircuitState.Open && 
            DateTimeOffset.UtcNow - _lastFailure > _coolingPeriod)
        {
            _state = CircuitState.HalfOpen;
            return true;
        }
        return false;
    }
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-FBK-001 | All fallbacks exhausted |
| ACODE-FBK-002 | Circuit breaker open |
| ACODE-FBK-003 | Model timeout |
| ACODE-FBK-004 | Invalid fallback chain |
| ACODE-FBK-005 | Mode constraint violation |

### Logging Fields

```json
{
  "event": "fallback_escalation",
  "role": "planner",
  "original_model": "llama3.2:70b",
  "fallback_model": "llama3.2:7b",
  "trigger": "request_timeout",
  "trigger_detail": "65000ms > 60000ms limit",
  "circuit_state": "closed"
}
```

### Implementation Checklist

1. [ ] Create IFallbackHandler interface
2. [ ] Create FallbackResult class
3. [ ] Create EscalationTrigger enum
4. [ ] Create FallbackConfiguration class
5. [ ] Implement CircuitBreaker
6. [ ] Implement ImmediatePolicy
7. [ ] Implement RetryPolicy
8. [ ] Implement CircuitBreakerPolicy
9. [ ] Implement FallbackHandler
10. [ ] Implement FallbackChainResolver
11. [ ] Add CLI commands
12. [ ] Write unit tests
13. [ ] Write integration tests
14. [ ] Add XML documentation

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~Fallback"
```

---

**End of Task 009.c Specification**