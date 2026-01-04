# Task 033.c: Burst Rate Limiting and Cooldown

**Priority:** P1 – High  
**Tier:** L – Cloud Integration  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 033 (Burst Heuristics), Task 033.b (Aggregation)  

---

## Description

Task 033.c implements burst rate limiting and cooldown. Bursting MUST be rate-limited. Cooldown periods MUST prevent thrashing. Gradual scaling MUST be supported.

Rate limiting prevents excessive cloud spending. Cooldown ensures newly provisioned resources stabilize. Gradual scaling avoids over-provisioning.

This task covers burst pacing. Trigger aggregation is in 033.b. Cost controls are in 031.c.

### Business Value

Rate limiting provides:
- Cost protection
- System stability
- Predictable behavior
- Thrashing prevention

### Scope Boundaries

This task covers burst pacing. Aggregation is in 033.b. Cost budgets are in 031.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Burst Aggregator | `IAggregatedDecision` | Receives burst decision | From 033.b |
| Cost Monitor | `ICostMonitor` | Budget limits checked | From 031.c |
| Provisioner | `IProvisioningService` | Pacing instance creation | Gradual scaling |
| State Store | SQLite DB | Persist rate limit state | Survives restarts |
| Event Bus | `IEventPublisher` | Rate limit events published | Async notification |
| CLI Handler | `BurstStatusCommand` | Status display to user | Real-time data |
| Configuration | `IOptionsSnapshot<>` | Dynamic limit changes | Hot reload |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| State store unavailable | SQLite connection error | Use in-memory fallback | Limits may reset on restart |
| Timer drift | Clock skew detection | Resync with system time | Cooldown may vary ±seconds |
| Concurrent burst requests | Lock contention detected | Serialize with semaphore | Slight delay for second request |
| Cooldown state lost | State file missing | Apply default cooldown | Conservative behavior |
| Rate counter overflow | Integer overflow check | Reset to max safe value | Unlikely, logged warning |
| Backoff calculation error | NaN/Infinity check | Use max backoff | Conservative fallback |
| Config reload failure | Validation exception | Keep previous config | Old limits remain active |
| Force bypass abuse | High frequency detection | Rate limit force flag | Admin alerted |

### Mode Compliance

| Operating Mode | Burst Rate Limiting Behavior | Constraints |
|----------------|------------------------------|-------------|
| Local-Only | Always blocked (no cloud) | Rate limiter returns blocked |
| Burst | Rate limits enforced | All checks active |
| Air-Gapped | Not applicable | No external connectivity |

### Assumptions

1. **System clock accurate**: Rate limiting depends on accurate system time for cooldowns
2. **SQLite available**: State persistence requires SQLite database access
3. **Single-node deployment**: Rate limiting is per-instance, not distributed
4. **Burst requests serialized**: Concurrent requests are queued, not parallel
5. **Configuration immutable during burst**: Limits don't change mid-burst
6. **P0 defined in task metadata**: Priority level available for cooldown reduction
7. **Force flag requires elevation**: Only operators can bypass limits
8. **Instance count trackable**: Running instances can be queried from infrastructure

### Security Considerations

1. **Force bypass authorization**: `--force` flag requires operator privilege level
2. **Rate limit config protected**: Configuration changes require admin access
3. **State file permissions**: SQLite database has restricted file permissions
4. **Audit trail for bypasses**: All force bypasses logged with user identity
5. **No secrets in rate limit state**: State file contains only timing data
6. **Rate limit evasion prevention**: Cannot circumvent by restarting process
7. **Backoff reset authorization**: Manual reset requires operator privilege
8. **Cost protection enforcement**: Rate limits cannot be disabled entirely

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Rate Limit | Max bursts per period |
| Cooldown | Wait after burst |
| Thrashing | Rapid scale up/down |
| Gradual | Step-by-step scaling |
| Warmup | New instance stabilization |
| Backoff | Increasing delays |

---

## Out of Scope

- Adaptive rate limiting
- Machine learning prediction
- External rate limit service
- Distributed rate limiting
- Token bucket algorithms

---

## Functional Requirements

### FR-001 to FR-015: Rate Limiter Interface

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033C-01 | `IBurstRateLimiter` interface MUST exist in Application layer | P0 |
| FR-033C-02 | `AllowBurstAsync` MUST check all rate limits before allowing burst | P0 |
| FR-033C-03 | Input MUST include `BurstRequest` with target type, count, priority | P0 |
| FR-033C-04 | Output MUST return `RateLimitResult` with allowed boolean | P0 |
| FR-033C-05 | Blocked result MUST include `RateLimitReason` enum value | P0 |
| FR-033C-06 | Blocked result MUST include estimated wait time until allowed | P1 |
| FR-033C-07 | `RecordBurstAsync` MUST track successful bursts for rate counting | P0 |
| FR-033C-08 | Record MUST be called after successful burst completion | P0 |
| FR-033C-09 | Rate limit state MUST persist to SQLite database | P0 |
| FR-033C-10 | Process restart MUST NOT reset rate limit counters | P0 |
| FR-033C-11 | State store MUST use SQLite for cross-restart persistence | P1 |
| FR-033C-12 | Rate limiter MUST be configurable via `agent-config.yml` | P1 |
| FR-033C-13 | All limits MUST be tunable without code changes | P1 |
| FR-033C-14 | Rate limiter MUST log all decisions with structured data | P1 |
| FR-033C-15 | Rate limiter MUST emit metrics for monitoring dashboards | P2 |

### FR-016 to FR-030: Cooldown Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033C-16 | Cooldown period MUST exist between consecutive bursts | P0 |
| FR-033C-17 | Cooldown duration MUST be configurable via configuration | P0 |
| FR-033C-18 | Default cooldown MUST be 5 minutes for EC2 targets | P1 |
| FR-033C-19 | Cooldown timer MUST start immediately after burst completion | P0 |
| FR-033C-20 | Burst requests MUST be blocked during active cooldown period | P0 |
| FR-033C-21 | Cooldown MUST support per-target-type configuration | P1 |
| FR-033C-22 | EC2 target type cooldown MUST default to 5 minutes | P1 |
| FR-033C-23 | SSH target type cooldown MUST default to 2 minutes | P1 |
| FR-033C-24 | Cooldown bypass MUST exist via `--force` flag | P1 |
| FR-033C-25 | Force flag MUST bypass cooldown for emergency situations | P1 |
| FR-033C-26 | P0 priority tasks MUST reduce cooldown by configured percentage | P1 |
| FR-033C-27 | P0 cooldown reduction MUST default to 50% | P2 |
| FR-033C-28 | Remaining cooldown time MUST be queryable via API | P1 |
| FR-033C-29 | Cooldown status MUST be visible in CLI output | P1 |
| FR-033C-30 | `acode burst status` MUST display cooldown information | P1 |

### FR-031 to FR-045: Rate Limits

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033C-31 | Bursts per hour MUST be limited to configurable maximum | P0 |
| FR-033C-32 | Default hourly burst limit MUST be 10 bursts | P1 |
| FR-033C-33 | Bursts per day MUST be limited to configurable maximum | P0 |
| FR-033C-34 | Default daily burst limit MUST be 50 bursts | P1 |
| FR-033C-35 | Instances per single burst MUST be limited | P0 |
| FR-033C-36 | Default max instances per burst MUST be 3 | P1 |
| FR-033C-37 | Total concurrent instances MUST be limited globally | P0 |
| FR-033C-38 | Default max concurrent instances MUST be 5 | P1 |
| FR-033C-39 | Limits MUST support per-target-type configuration | P1 |
| FR-033C-40 | EC2 limits MUST be configurable separately from SSH limits | P2 |
| FR-033C-41 | Global limits MUST exist that apply to all target types | P1 |
| FR-033C-42 | Global limits MUST override per-type limits when lower | P1 |
| FR-033C-43 | Limit exceeded MUST block burst with clear reason | P0 |
| FR-033C-44 | Block message MUST include which limit was exceeded | P1 |
| FR-033C-45 | Next allowed burst time MUST be included in block response | P1 |

### FR-046 to FR-060: Gradual Scaling

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033C-46 | Gradual scaling MUST be enabled by default | P1 |
| FR-033C-47 | First scaling step MUST provision only 1 instance | P1 |
| FR-033C-48 | Each step MUST wait for stabilization before next | P0 |
| FR-033C-49 | Default stabilization period MUST be 2 minutes | P1 |
| FR-033C-50 | Scale up MUST continue if demand still exists | P0 |
| FR-033C-51 | Maximum increment per step MUST be 2 instances | P1 |
| FR-033C-52 | Scale factor MUST be configurable for growth rate | P2 |
| FR-033C-53 | Default scale factor MUST be 1.5x current count | P2 |
| FR-033C-54 | Scaling MUST cap at originally requested total | P0 |
| FR-033C-55 | Never provision more instances than originally requested | P0 |
| FR-033C-56 | Rapid scaling MUST be optional alternative to gradual | P2 |
| FR-033C-57 | Rapid scaling MUST provision all instances at once | P2 |
| FR-033C-58 | Rapid scaling MUST require explicit `--rapid-scale` flag | P2 |
| FR-033C-59 | `--rapid-scale` flag MUST override gradual scaling setting | P2 |
| FR-033C-60 | Gradual scaling progress MUST be logged at each step | P1 |

### FR-061 to FR-075: Failure Backoff

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033C-61 | Failure backoff MUST exist for failed burst attempts | P0 |
| FR-033C-62 | Failed burst MUST trigger delay before next attempt | P0 |
| FR-033C-63 | Initial backoff duration MUST be 1 minute | P1 |
| FR-033C-64 | Backoff multiplier MUST be 2x for exponential growth | P1 |
| FR-033C-65 | Maximum backoff MUST cap at 30 minutes | P1 |
| FR-033C-66 | Successful burst MUST reset backoff to zero | P0 |
| FR-033C-67 | Partial success (some instances) MUST reduce backoff | P2 |
| FR-033C-68 | Backoff MUST be tracked per target type | P1 |
| FR-033C-69 | Different target types MUST have independent backoff | P1 |
| FR-033C-70 | Backoff state MUST persist across process restarts | P1 |
| FR-033C-71 | Current backoff status MUST be queryable via API | P1 |
| FR-033C-72 | Manual backoff reset MUST be supported for operators | P2 |
| FR-033C-73 | `acode burst reset` MUST clear backoff state | P2 |
| FR-033C-74 | Backoff events MUST be logged with full context | P1 |
| FR-033C-75 | Metrics on backoff state MUST be emitted | P2 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-033C-01 | Rate limit check latency | <5ms p99 | P0 |
| NFR-033C-02 | State store read latency | <10ms p99 | P1 |
| NFR-033C-03 | State store write latency | <20ms p99 | P1 |
| NFR-033C-04 | Memory for rate limit state | <5MB | P2 |
| NFR-033C-05 | Cooldown timer accuracy | ±1 second | P1 |
| NFR-033C-06 | Backoff calculation time | <1ms | P2 |
| NFR-033C-07 | Status query response time | <50ms | P1 |
| NFR-033C-08 | Concurrent request handling | 100 req/sec | P2 |
| NFR-033C-09 | SQLite connection pool | 5 connections | P2 |
| NFR-033C-10 | Gradual scale step calculation | <1ms | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-033C-11 | State persistence durability | 99.99% | P0 |
| NFR-033C-12 | No rate limit bypass on error | 100% | P0 |
| NFR-033C-13 | Thread safety for concurrent access | Zero race conditions | P0 |
| NFR-033C-14 | Graceful degradation on store failure | In-memory fallback | P1 |
| NFR-033C-15 | Clock skew tolerance | ±5 seconds | P1 |
| NFR-033C-16 | Restart recovery time | <1 second | P1 |
| NFR-033C-17 | Duplicate burst prevention | Zero duplicates | P0 |
| NFR-033C-18 | Force bypass audit trail | 100% logged | P0 |
| NFR-033C-19 | Configuration validation | Fail-fast on invalid | P1 |
| NFR-033C-20 | Backoff state consistency | Survives crashes | P1 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-033C-21 | Structured logging for all decisions | JSON format | P1 |
| NFR-033C-22 | Metrics on burst blocked count | Per-reason breakdown | P1 |
| NFR-033C-23 | Metrics on cooldown active time | Histogram | P2 |
| NFR-033C-24 | Metrics on backoff occurrences | Counter per type | P1 |
| NFR-033C-25 | Event emission for rate limit hits | Async publish | P1 |
| NFR-033C-26 | Clear block reason in user messages | Human-readable | P0 |
| NFR-033C-27 | Trace correlation for rate checks | Request ID propagated | P2 |
| NFR-033C-28 | Dashboard integration support | Prometheus compatible | P2 |
| NFR-033C-29 | Alert threshold configuration | Configurable | P2 |
| NFR-033C-30 | Audit log for config changes | Full history | P1 |

---

## User Manual Documentation

### Configuration

```yaml
burst:
  rateLimit:
    perHour: 10
    perDay: 50
    maxInstancesPerBurst: 3
    maxConcurrentInstances: 5
  cooldown:
    defaultMinutes: 5
    ec2Minutes: 5
    sshMinutes: 2
    p0ReductionPercent: 50
  gradualScaling:
    enabled: true
    stabilizationMinutes: 2
    scaleFactor: 1.5
  backoff:
    initialMinutes: 1
    multiplier: 2
    maxMinutes: 30
```

### CLI Commands

```bash
# Check burst status
acode burst status

# Output:
# Cooldown: 3m 24s remaining
# Rate Limit: 7/10 bursts this hour
# Backoff: none
# Instances: 2/5 running

# Force bypass cooldown (for P0)
acode burst --force

# Reset backoff
acode burst reset

# View history
acode burst history --last 24h
```

### Rate Limit Messages

| Scenario | Message |
|----------|---------|
| Cooldown | "Cooldown: 3m remaining. Next burst at 14:23" |
| Hourly limit | "Hourly limit reached (10/10). Resets at 15:00" |
| Max instances | "Max instances (5/5). Wait for termination" |
| Backoff | "In backoff after failure. Next attempt at 14:30" |

---

## Acceptance Criteria / Definition of Done

### Rate Limiter Interface
- [ ] AC-001: `IBurstRateLimiter` interface exists in Application layer
- [ ] AC-002: `CheckAsync` accepts `BurstRequest` and returns `RateLimitResult`
- [ ] AC-003: `RecordBurstAsync` tracks burst for rate counting
- [ ] AC-004: `GetStatusAsync` returns current rate limit status
- [ ] AC-005: `ResetBackoffAsync` clears backoff for target type
- [ ] AC-006: Result includes allowed boolean
- [ ] AC-007: Result includes block reason enum
- [ ] AC-008: Result includes wait time estimate

### Cooldown Management
- [ ] AC-009: Cooldown starts after burst completion
- [ ] AC-010: Bursts blocked during active cooldown
- [ ] AC-011: Default cooldown is 5 minutes
- [ ] AC-012: EC2 cooldown configurable separately
- [ ] AC-013: SSH cooldown configurable separately
- [ ] AC-014: Cooldown remaining queryable via status
- [ ] AC-015: `--force` flag bypasses cooldown
- [ ] AC-016: P0 priority reduces cooldown by 50%

### Rate Limits
- [ ] AC-017: Hourly limit enforced (default 10)
- [ ] AC-018: Daily limit enforced (default 50)
- [ ] AC-019: Per-burst instance limit (default 3)
- [ ] AC-020: Concurrent instance limit (default 5)
- [ ] AC-021: Per-target-type limits work
- [ ] AC-022: Global limits override type limits
- [ ] AC-023: Block message includes limit type
- [ ] AC-024: Next allowed time calculated

### Gradual Scaling
- [ ] AC-025: Gradual scaling enabled by default
- [ ] AC-026: First step provisions 1 instance
- [ ] AC-027: Stabilization wait between steps
- [ ] AC-028: Default stabilization 2 minutes
- [ ] AC-029: Max increment 2 instances per step
- [ ] AC-030: Scale factor 1.5x applied
- [ ] AC-031: Never exceeds requested total
- [ ] AC-032: `--rapid-scale` overrides gradual

### Failure Backoff
- [ ] AC-033: Failed burst triggers backoff
- [ ] AC-034: Initial backoff 1 minute
- [ ] AC-035: Backoff multiplier 2x applied
- [ ] AC-036: Max backoff capped at 30 minutes
- [ ] AC-037: Success resets backoff to zero
- [ ] AC-038: Backoff per target type
- [ ] AC-039: Backoff state persists
- [ ] AC-040: `acode burst reset` clears backoff

### State Persistence
- [ ] AC-041: State stored in SQLite database
- [ ] AC-042: State survives process restart
- [ ] AC-043: Rate counters persist
- [ ] AC-044: Cooldown timers persist
- [ ] AC-045: Backoff state persists

### CLI Commands
- [ ] AC-046: `acode burst status` shows cooldown
- [ ] AC-047: Status shows hourly usage
- [ ] AC-048: Status shows running instances
- [ ] AC-049: Status shows backoff state
- [ ] AC-050: `acode burst reset` available
- [ ] AC-051: `acode burst history` shows recent

### Observability
- [ ] AC-052: All decisions logged structured
- [ ] AC-053: Metrics emitted for blocks
- [ ] AC-054: Events published for rate limits
- [ ] AC-055: Force bypasses audited

---

## User Verification Scenarios

### Scenario 1: Cooldown Blocking
**Persona:** Developer who just burst  
**Preconditions:** Burst completed 2 minutes ago, 5 minute cooldown  
**Steps:**
1. Run another burst request
2. Request blocked with cooldown reason
3. Check `acode burst status`
4. Wait for cooldown expiry
5. Retry burst request

**Verification Checklist:**
- [ ] Burst blocked during cooldown
- [ ] Block message shows "3m remaining"
- [ ] Status shows cooldown timer
- [ ] Burst succeeds after cooldown

### Scenario 2: Hourly Limit Reached
**Persona:** Developer with heavy workload  
**Preconditions:** 10 bursts already this hour  
**Steps:**
1. Request 11th burst
2. Request blocked with hourly limit
3. Check status shows 10/10
4. Wait for hour rollover
5. Retry succeeds

**Verification Checklist:**
- [ ] 11th burst blocked
- [ ] Message: "Hourly limit (10/10)"
- [ ] Status shows limit
- [ ] Rollover resets counter

### Scenario 3: Force Bypass for Emergency
**Persona:** Operator with P0 incident  
**Preconditions:** In cooldown, urgent need  
**Steps:**
1. Run `acode burst --force`
2. Cooldown bypassed
3. Audit log entry created
4. Burst proceeds

**Verification Checklist:**
- [ ] Force bypass works
- [ ] Audit log contains bypass
- [ ] User identity recorded
- [ ] Burst completes

### Scenario 4: Gradual Scaling Steps
**Persona:** Developer requesting 5 instances  
**Preconditions:** Gradual scaling enabled  
**Steps:**
1. Request burst scale=5
2. Step 1: 1 instance provisioned
3. Wait 2 minutes stabilization
4. Step 2: 2 more instances
5. Step 3: 2 more instances

**Verification Checklist:**
- [ ] Step 1: 1 instance
- [ ] 2 minute wait observed
- [ ] Step 2: 2 instances (total 3)
- [ ] Step 3: 2 instances (total 5)

### Scenario 5: Failure Backoff Escalation
**Persona:** Developer with infrastructure issues  
**Preconditions:** Clean state, no backoff  
**Steps:**
1. Burst attempt fails
2. Check backoff: 1 minute
3. Wait, retry, fails again
4. Check backoff: 2 minutes
5. Wait, retry, fails again
6. Check backoff: 4 minutes

**Verification Checklist:**
- [ ] Initial backoff 1m
- [ ] Second failure 2m
- [ ] Third failure 4m
- [ ] Exponential growth verified

### Scenario 6: State Persistence Across Restart
**Persona:** Operations restarting service  
**Preconditions:** 5 bursts this hour, in cooldown  
**Steps:**
1. Check `acode burst status`
2. Restart acode process
3. Check status again
4. State preserved

**Verification Checklist:**
- [ ] Pre-restart: 5/10 bursts
- [ ] Pre-restart: cooldown active
- [ ] Post-restart: 5/10 bursts
- [ ] Post-restart: cooldown preserved

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-033C-01 | Cooldown blocks burst during active period | FR-033C-20 |
| UT-033C-02 | Cooldown expires after configured duration | FR-033C-18 |
| UT-033C-03 | Force flag bypasses cooldown | FR-033C-25 |
| UT-033C-04 | P0 priority reduces cooldown by 50% | FR-033C-27 |
| UT-033C-05 | Hourly limit blocks at threshold | FR-033C-31 |
| UT-033C-06 | Daily limit blocks at threshold | FR-033C-33 |
| UT-033C-07 | Per-burst instance limit enforced | FR-033C-35 |
| UT-033C-08 | Concurrent instance limit enforced | FR-033C-37 |
| UT-033C-09 | Gradual scaling first step is 1 | FR-033C-47 |
| UT-033C-10 | Gradual scaling respects max increment | FR-033C-51 |
| UT-033C-11 | Backoff initial value 1 minute | FR-033C-63 |
| UT-033C-12 | Backoff multiplier 2x applied | FR-033C-64 |
| UT-033C-13 | Backoff capped at 30 minutes | FR-033C-65 |
| UT-033C-14 | Success resets backoff | FR-033C-66 |
| UT-033C-15 | Rate check under 5ms | NFR-033C-01 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-033C-01 | Full rate limiting pipeline | E2E |
| IT-033C-02 | State persistence across restart | FR-033C-10 |
| IT-033C-03 | SQLite state store operations | FR-033C-11 |
| IT-033C-04 | Force bypass with audit trail | NFR-033C-18 |
| IT-033C-05 | Gradual scaling full sequence | Scenario 4 |
| IT-033C-06 | Backoff recovery sequence | Scenario 5 |
| IT-033C-07 | CLI status command accuracy | AC-046 |
| IT-033C-08 | CLI reset command functionality | AC-050 |
| IT-033C-09 | Metrics emission verification | NFR-033C-22 |
| IT-033C-10 | Event publishing on rate limit | NFR-033C-25 |
| IT-033C-11 | Thread safety under load | NFR-033C-13 |
| IT-033C-12 | Clock skew handling | NFR-033C-15 |
| IT-033C-13 | Configuration reload | Dynamic limits |
| IT-033C-14 | Per-target-type limits | FR-033C-39 |
| IT-033C-15 | Global limit override | FR-033C-42 |

---

## Implementation Prompt

### Part 1: File Structure + Domain Models

```
src/
├── Acode.Domain/
│   └── Compute/
│       └── Burst/
│           └── RateLimit/
│               ├── RateLimitReason.cs
│               └── Events/
│                   ├── BurstRateLimitedEvent.cs
│                   ├── CooldownStartedEvent.cs
│                   └── BackoffAppliedEvent.cs
├── Acode.Application/
│   └── Compute/
│       └── Burst/
│           └── RateLimit/
│               ├── IBurstRateLimiter.cs
│               ├── IGradualScaler.cs
│               ├── BurstRequest.cs
│               ├── RateLimitResult.cs
│               ├── RateLimitStatus.cs
│               ├── ScaleStep.cs
│               └── RateLimitOptions.cs
└── Acode.Infrastructure/
    └── Compute/
        └── Burst/
            └── RateLimit/
                ├── BurstRateLimiter.cs
                ├── RateLimitStateStore.cs
                ├── CooldownTracker.cs
                ├── BackoffCalculator.cs
                └── GradualScaler.cs
```

```csharp
// src/Acode.Domain/Compute/Burst/RateLimit/RateLimitReason.cs
namespace Acode.Domain.Compute.Burst.RateLimit;

public enum RateLimitReason
{
    Cooldown,
    HourlyLimit,
    DailyLimit,
    MaxInstances,
    Backoff
}

// src/Acode.Domain/Compute/Burst/RateLimit/Events/BurstRateLimitedEvent.cs
namespace Acode.Domain.Compute.Burst.RateLimit.Events;

public sealed record BurstRateLimitedEvent(
    RateLimitReason Reason,
    TimeSpan WaitTime,
    string TargetType,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Burst/RateLimit/Events/CooldownStartedEvent.cs
namespace Acode.Domain.Compute.Burst.RateLimit.Events;

public sealed record CooldownStartedEvent(
    string TargetType,
    TimeSpan Duration,
    DateTimeOffset EndsAt,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Burst/RateLimit/Events/BackoffAppliedEvent.cs
namespace Acode.Domain.Compute.Burst.RateLimit.Events;

public sealed record BackoffAppliedEvent(
    string TargetType,
    int FailureCount,
    TimeSpan BackoffDuration,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 033.c Specification - Part 1/3**

### Part 2: Application Interfaces

```csharp
// src/Acode.Application/Compute/Burst/RateLimit/BurstRequest.cs
namespace Acode.Application.Compute.Burst.RateLimit;

public sealed record BurstRequest
{
    public required string TargetType { get; init; }
    public int RequestedInstances { get; init; } = 1;
    public int? HighestPriority { get; init; }
    public bool Force { get; init; } = false;
}

// src/Acode.Application/Compute/Burst/RateLimit/RateLimitResult.cs
namespace Acode.Application.Compute.Burst.RateLimit;

public sealed record RateLimitResult
{
    public bool Allowed { get; init; }
    public RateLimitReason? BlockReason { get; init; }
    public TimeSpan? WaitTime { get; init; }
    public DateTimeOffset? NextAllowed { get; init; }
    public int AllowedInstances { get; init; }
}

// src/Acode.Application/Compute/Burst/RateLimit/RateLimitStatus.cs
namespace Acode.Application.Compute.Burst.RateLimit;

public sealed record RateLimitStatus
{
    public bool InCooldown { get; init; }
    public TimeSpan CooldownRemaining { get; init; }
    public int BurstsThisHour { get; init; }
    public int HourlyLimit { get; init; }
    public int BurstsToday { get; init; }
    public int DailyLimit { get; init; }
    public int RunningInstances { get; init; }
    public int MaxInstances { get; init; }
    public bool InBackoff { get; init; }
    public TimeSpan BackoffRemaining { get; init; }
}

// src/Acode.Application/Compute/Burst/RateLimit/ScaleStep.cs
namespace Acode.Application.Compute.Burst.RateLimit;

public sealed record ScaleStep
{
    public int InstancesToAdd { get; init; }
    public TimeSpan WaitBeforeNext { get; init; }
    public bool IsFinal { get; init; }
}

// src/Acode.Application/Compute/Burst/RateLimit/RateLimitOptions.cs
namespace Acode.Application.Compute.Burst.RateLimit;

public sealed record RateLimitOptions
{
    public int PerHour { get; init; } = 10;
    public int PerDay { get; init; } = 50;
    public int MaxInstancesPerBurst { get; init; } = 3;
    public int MaxConcurrentInstances { get; init; } = 5;
    public TimeSpan DefaultCooldown { get; init; } = TimeSpan.FromMinutes(5);
    public double P0CooldownReduction { get; init; } = 0.5;
    public TimeSpan InitialBackoff { get; init; } = TimeSpan.FromMinutes(1);
    public double BackoffMultiplier { get; init; } = 2.0;
    public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromMinutes(30);
    public bool GradualScalingEnabled { get; init; } = true;
    public TimeSpan StabilizationPeriod { get; init; } = TimeSpan.FromMinutes(2);
    public double ScaleFactor { get; init; } = 1.5;
}

// src/Acode.Application/Compute/Burst/RateLimit/IBurstRateLimiter.cs
namespace Acode.Application.Compute.Burst.RateLimit;

public interface IBurstRateLimiter
{
    Task<RateLimitResult> CheckAsync(BurstRequest request, CancellationToken ct = default);
    Task RecordBurstAsync(string targetType, int instanceCount, bool success, CancellationToken ct = default);
    Task<RateLimitStatus> GetStatusAsync(CancellationToken ct = default);
    Task ResetBackoffAsync(string? targetType = null, CancellationToken ct = default);
}

// src/Acode.Application/Compute/Burst/RateLimit/IGradualScaler.cs
namespace Acode.Application.Compute.Burst.RateLimit;

public interface IGradualScaler
{
    Task<ScaleStep> GetNextStepAsync(int targetScale, int currentScale, CancellationToken ct = default);
    bool IsScalingComplete(int targetScale, int currentScale);
}
```

**End of Task 033.c Specification - Part 2/3**

### Part 3: Infrastructure Implementation + Checklist

```csharp
// src/Acode.Infrastructure/Compute/Burst/RateLimit/BackoffCalculator.cs
namespace Acode.Infrastructure.Compute.Burst.RateLimit;

public sealed class BackoffCalculator
{
    private readonly RateLimitOptions _options;
    private readonly Dictionary<string, (int FailureCount, DateTimeOffset LastFailure)> _state = new();
    
    public TimeSpan GetBackoff(string targetType)
    {
        if (!_state.TryGetValue(targetType, out var state))
            return TimeSpan.Zero;
        
        var backoff = _options.InitialBackoff * Math.Pow(_options.BackoffMultiplier, state.FailureCount - 1);
        backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks, _options.MaxBackoff.Ticks));
        
        var elapsed = DateTimeOffset.UtcNow - state.LastFailure;
        return elapsed >= backoff ? TimeSpan.Zero : backoff - elapsed;
    }
    
    public void RecordFailure(string targetType)
    {
        var count = _state.TryGetValue(targetType, out var s) ? s.FailureCount + 1 : 1;
        _state[targetType] = (count, DateTimeOffset.UtcNow);
    }
    
    public void RecordSuccess(string targetType) => _state.Remove(targetType);
    
    public void Reset(string? targetType = null)
    {
        if (targetType != null) _state.Remove(targetType);
        else _state.Clear();
    }
}

// src/Acode.Infrastructure/Compute/Burst/RateLimit/GradualScaler.cs
namespace Acode.Infrastructure.Compute.Burst.RateLimit;

public sealed class GradualScaler : IGradualScaler
{
    private readonly RateLimitOptions _options;
    
    public Task<ScaleStep> GetNextStepAsync(int targetScale, int currentScale, CancellationToken ct)
    {
        if (!_options.GradualScalingEnabled || currentScale >= targetScale)
            return Task.FromResult(new ScaleStep { InstancesToAdd = 0, IsFinal = true });
        
        var remaining = targetScale - currentScale;
        var toAdd = currentScale == 0 
            ? 1 
            : Math.Min((int)Math.Ceiling(currentScale * (_options.ScaleFactor - 1)), remaining);
        
        toAdd = Math.Min(toAdd, 2); // Max increment
        
        return Task.FromResult(new ScaleStep
        {
            InstancesToAdd = toAdd,
            WaitBeforeNext = _options.StabilizationPeriod,
            IsFinal = currentScale + toAdd >= targetScale
        });
    }
    
    public bool IsScalingComplete(int targetScale, int currentScale) => currentScale >= targetScale;
}

// src/Acode.Infrastructure/Compute/Burst/RateLimit/BurstRateLimiter.cs
namespace Acode.Infrastructure.Compute.Burst.RateLimit;

public sealed class BurstRateLimiter : IBurstRateLimiter
{
    private readonly RateLimitOptions _options;
    private readonly RateLimitStateStore _store;
    private readonly BackoffCalculator _backoff;
    private readonly IEventPublisher _events;
    
    public async Task<RateLimitResult> CheckAsync(BurstRequest request, CancellationToken ct)
    {
        // Force bypass for P0 (with reduced cooldown)
        var cooldown = _options.DefaultCooldown;
        if (request.HighestPriority == 0)
            cooldown = TimeSpan.FromTicks((long)(cooldown.Ticks * (1 - _options.P0CooldownReduction)));
        
        // Check cooldown
        var cooldownRemaining = await _store.GetCooldownRemainingAsync(request.TargetType, ct);
        if (!request.Force && cooldownRemaining > TimeSpan.Zero)
        {
            await _events.PublishAsync(new BurstRateLimitedEvent(RateLimitReason.Cooldown, cooldownRemaining, request.TargetType, DateTimeOffset.UtcNow), ct);
            return new RateLimitResult { Allowed = false, BlockReason = RateLimitReason.Cooldown, WaitTime = cooldownRemaining };
        }
        
        // Check backoff
        var backoffRemaining = _backoff.GetBackoff(request.TargetType);
        if (!request.Force && backoffRemaining > TimeSpan.Zero)
            return new RateLimitResult { Allowed = false, BlockReason = RateLimitReason.Backoff, WaitTime = backoffRemaining };
        
        // Check hourly/daily limits
        var status = await GetStatusAsync(ct);
        if (status.BurstsThisHour >= _options.PerHour)
            return new RateLimitResult { Allowed = false, BlockReason = RateLimitReason.HourlyLimit };
        
        if (status.RunningInstances >= _options.MaxConcurrentInstances)
            return new RateLimitResult { Allowed = false, BlockReason = RateLimitReason.MaxInstances };
        
        var allowedInstances = Math.Min(request.RequestedInstances, 
            Math.Min(_options.MaxInstancesPerBurst, _options.MaxConcurrentInstances - status.RunningInstances));
        
        return new RateLimitResult { Allowed = true, AllowedInstances = allowedInstances };
    }
}
```

### Implementation Checklist

| Step | Action | Verification |
|------|--------|--------------|
| 1 | Create RateLimitReason enum | Enum compiles |
| 2 | Add rate limit events | Event serialization verified |
| 3 | Define all records (BurstRequest, RateLimitResult, etc.) | Records compile |
| 4 | Create IBurstRateLimiter, IGradualScaler interfaces | Interface contracts clear |
| 5 | Implement RateLimitStateStore with SQLite | State persists across restarts |
| 6 | Implement CooldownTracker | Cooldown timing accurate |
| 7 | Implement BackoffCalculator | Exponential backoff verified |
| 8 | Implement GradualScaler | Step-by-step scaling works |
| 9 | Implement BurstRateLimiter | All rate checks work |
| 10 | Add P0 cooldown reduction | 50% reduction verified |
| 11 | Add force bypass | Force flag works |
| 12 | Implement CLI status command | `acode burst status` works |
| 13 | Add metrics and logging | Rate limit metrics emitted |
| 14 | Performance verify <5ms | Benchmark passes |

### Rollout Plan

1. **Phase 1**: Implement state store and cooldown tracker
2. **Phase 2**: Add backoff calculator with exponential delays
3. **Phase 3**: Build rate limiter with all limit checks
4. **Phase 4**: Add gradual scaler with stabilization
5. **Phase 5**: CLI integration and end-to-end tests

**End of Task 033.c Specification**