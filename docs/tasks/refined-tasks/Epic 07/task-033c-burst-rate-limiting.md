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

- Task 033.b: Receives aggregated decision
- Task 031.c: Respects cost limits
- Task 031.a: Paces provisioning

### Failure Modes

- Cooldown state lost → Use default
- Rate limit exceeded → Queue burst
- Timer error → Log and continue
- Concurrent burst → Serialize

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

- FR-001: `IBurstRateLimiter` MUST exist
- FR-002: `AllowBurstAsync` MUST check
- FR-003: Input: burst request
- FR-004: Output: allowed or blocked
- FR-005: Blocked MUST include reason
- FR-006: Blocked MUST include wait time
- FR-007: `RecordBurstAsync` MUST track
- FR-008: Record after successful burst
- FR-009: State MUST persist
- FR-010: Restart MUST not reset
- FR-011: State in SQLite
- FR-012: Rate limiter MUST be configurable
- FR-013: Limits MUST be tunable
- FR-014: Rate limiter MUST log
- FR-015: Rate limiter MUST emit metrics

### FR-016 to FR-030: Cooldown

- FR-016: Cooldown period MUST exist
- FR-017: Cooldown MUST be configurable
- FR-018: Default cooldown: 5 minutes
- FR-019: Cooldown starts on burst
- FR-020: Burst blocked during cooldown
- FR-021: Cooldown per target type MUST work
- FR-022: EC2 cooldown: 5 minutes
- FR-023: SSH cooldown: 2 minutes
- FR-024: Cooldown bypass MUST exist
- FR-025: Force flag bypasses
- FR-026: P0 task MUST reduce cooldown
- FR-027: P0 reduction: 50%
- FR-028: Cooldown remaining MUST report
- FR-029: Cooldown MUST be visible in CLI
- FR-030: `acode burst status`

### FR-031 to FR-045: Rate Limits

- FR-031: Bursts per hour MUST be limited
- FR-032: Default: 10 per hour
- FR-033: Bursts per day MUST be limited
- FR-034: Default: 50 per day
- FR-035: Instances per burst MUST be limited
- FR-036: Default: 3 instances
- FR-037: Total instances MUST be limited
- FR-038: Default: 5 concurrent
- FR-039: Limits MUST be per-target-type
- FR-040: EC2 limits separate from SSH
- FR-041: Global limits MUST exist
- FR-042: Global overrides per-type
- FR-043: Limit exceeded MUST block
- FR-044: Block message MUST include limit
- FR-045: Next allowed time MUST show

### FR-046 to FR-060: Gradual Scaling

- FR-046: Gradual scaling MUST be default
- FR-047: Start with 1 instance
- FR-048: Wait for stabilization
- FR-049: Stabilization period: 2 minutes
- FR-050: Scale up if still needed
- FR-051: Max increment: 2 instances
- FR-052: Scale factor MUST be configurable
- FR-053: Default factor: 1.5x
- FR-054: Cap at requested scale
- FR-055: Never exceed requested
- FR-056: Rapid scale MUST be optional
- FR-057: Rapid: all at once
- FR-058: Rapid requires flag
- FR-059: `--rapid-scale` flag
- FR-060: Gradual MUST log progress

### FR-061 to FR-075: Backoff

- FR-061: Failure backoff MUST exist
- FR-062: Failed burst MUST delay next
- FR-063: Initial backoff: 1 minute
- FR-064: Backoff multiplier: 2x
- FR-065: Max backoff: 30 minutes
- FR-066: Success MUST reset backoff
- FR-067: Partial success MUST reduce
- FR-068: Backoff per target type
- FR-069: Different types independent
- FR-070: Backoff state MUST persist
- FR-071: Backoff MUST be queryable
- FR-072: Manual reset MUST work
- FR-073: `acode burst reset`
- FR-074: Backoff MUST log
- FR-075: Metrics on backoff state

---

## Non-Functional Requirements

- NFR-001: Rate check <5ms
- NFR-002: State persistence reliable
- NFR-003: No race conditions
- NFR-004: Accurate timing
- NFR-005: Restart resilient
- NFR-006: Structured logging
- NFR-007: Metrics on limits
- NFR-008: Clear block reasons
- NFR-009: Configurable limits
- NFR-010: Thread-safe

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

- [ ] AC-001: Cooldown works
- [ ] AC-002: Rate limits work
- [ ] AC-003: Gradual scaling works
- [ ] AC-004: Backoff works
- [ ] AC-005: State persists
- [ ] AC-006: Force bypass works
- [ ] AC-007: P0 reduction works
- [ ] AC-008: CLI status works
- [ ] AC-009: Metrics emit
- [ ] AC-010: Logging complete

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Cooldown timing
- [ ] UT-002: Rate limit counting
- [ ] UT-003: Gradual scaling
- [ ] UT-004: Backoff calculation

### Integration Tests

- [ ] IT-001: Full rate limiting
- [ ] IT-002: State persistence
- [ ] IT-003: Force bypass
- [ ] IT-004: Backoff recovery

---

## Implementation Prompt

### Interface

```csharp
public interface IBurstRateLimiter
{
    Task<RateLimitResult> CheckAsync(
        BurstRequest request,
        CancellationToken ct);
    
    Task RecordBurstAsync(
        BurstRecord record,
        CancellationToken ct);
    
    Task<RateLimitStatus> GetStatusAsync(
        CancellationToken ct);
    
    Task ResetBackoffAsync(
        string targetType = null,
        CancellationToken ct = default);
}

public record BurstRequest(
    string TargetType,
    int RequestedInstances,
    TaskPriority? HighestPriority,
    bool Force);

public record RateLimitResult(
    bool Allowed,
    RateLimitReason? BlockReason,
    TimeSpan? WaitTime,
    DateTime? NextAllowed,
    int AllowedInstances);

public enum RateLimitReason
{
    Cooldown,
    HourlyLimit,
    DailyLimit,
    MaxInstances,
    Backoff
}

public record RateLimitStatus(
    bool InCooldown,
    TimeSpan CooldownRemaining,
    int BurstsThisHour,
    int HourlyLimit,
    int BurstsToday,
    int DailyLimit,
    int RunningInstances,
    int MaxInstances,
    bool InBackoff,
    TimeSpan BackoffRemaining);
```

### Gradual Scaler

```csharp
public interface IGradualScaler
{
    Task<ScaleStep> GetNextStepAsync(
        int targetScale,
        int currentScale,
        CancellationToken ct);
}

public record ScaleStep(
    int InstancestoAdd,
    TimeSpan WaitBeforeNext,
    bool IsFinal);
```

---

**End of Task 033.c Specification**