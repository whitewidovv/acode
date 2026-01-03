# Task 033: Cloud Burst Heuristics

**Priority:** P1 – High  
**Tier:** L – Cloud Integration  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 031 (EC2), Task 032 (Placement)  

---

## Description

Task 033 implements cloud burst heuristics. The system MUST automatically decide when to burst to cloud. Heuristics MUST consider load, queue depth, and task characteristics.

Bursting is the automatic scaling to cloud compute. Heuristics determine when local resources are insufficient. The goal is optimal resource utilization.

This task provides the burst decision engine. Subtasks cover specific heuristics and configuration.

### Business Value

Burst heuristics enable:
- Automatic scaling
- Optimal resource usage
- Reduced wait times
- Cost-efficient bursting

### Scope Boundaries

This task covers burst decisions. EC2 target is in Task 031. Placement is in Task 032.

### Integration Points

- Task 031: Bursts to EC2
- Task 032: Informs placement
- Task 027: Worker pool triggers burst
- Task 026: Queue depth signals burst

### Mode Compliance

| Mode | Burst Behavior |
|------|----------------|
| local-only | DISABLED |
| airgapped | DISABLED |
| burst | ENABLED |

Heuristics MUST respect mode constraints.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Burst | Scale to cloud |
| Heuristic | Decision rule |
| Trigger | Condition that causes burst |
| Threshold | Value that triggers |
| Cooldown | Wait after burst |
| Backpressure | Queue-based signal |

---

## Out of Scope

- Machine learning predictions
- Historical workload analysis
- Multi-cloud burst coordination
- Container orchestration burst
- Serverless (Lambda) burst

---

## Functional Requirements

### FR-001 to FR-020: Burst Engine

- FR-001: `IBurstHeuristics` MUST exist
- FR-002: `ShouldBurstAsync` MUST return decision
- FR-003: Input: current state
- FR-004: Output: burst decision
- FR-005: Decision MUST include reason
- FR-006: Decision MUST include target count
- FR-007: Multiple heuristics MUST combine
- FR-008: Heuristics MUST be weighted
- FR-009: Any trigger MUST burst (OR)
- FR-010: All triggers MUST burst (AND) optional
- FR-011: Default: OR logic
- FR-012: Heuristics MUST be pluggable
- FR-013: Heuristics MUST be configurable
- FR-014: Heuristics MUST be disableable
- FR-015: Burst MUST respect mode
- FR-016: local-only MUST never burst
- FR-017: airgapped MUST never burst
- FR-018: Burst MUST log decision
- FR-019: Burst MUST emit metrics
- FR-020: Burst history MUST be tracked

### FR-021 to FR-040: Queue-Based Heuristics

- FR-021: Queue depth trigger MUST exist
- FR-022: Depth threshold MUST be configurable
- FR-023: Default threshold: 10 tasks
- FR-024: Queue wait time trigger MUST exist
- FR-025: Wait threshold MUST be configurable
- FR-026: Default wait threshold: 5 minutes
- FR-027: Queue growth rate trigger MUST exist
- FR-028: Fast growth MUST trigger burst
- FR-029: Growth rate: tasks per minute
- FR-030: Default growth threshold: 5/min
- FR-031: Priority queue trigger MUST exist
- FR-032: High priority MUST trigger faster
- FR-033: Priority multiplier MUST exist
- FR-034: Default multiplier: 2x
- FR-035: Starvation trigger MUST exist
- FR-036: Task waiting too long
- FR-037: Starvation threshold: 15 minutes
- FR-038: Queue composition MUST matter
- FR-039: Large tasks MUST weight more
- FR-040: Estimated runtime MUST factor

### FR-041 to FR-060: Load-Based Heuristics

- FR-041: CPU utilization trigger MUST exist
- FR-042: CPU threshold MUST be configurable
- FR-043: Default CPU threshold: 80%
- FR-044: Sustained high CPU MUST trigger
- FR-045: Sustained period: 2 minutes
- FR-046: Memory utilization trigger MUST exist
- FR-047: Memory threshold MUST be configurable
- FR-048: Default memory threshold: 80%
- FR-049: Disk utilization trigger MUST exist
- FR-050: Disk threshold: 90%
- FR-051: Worker saturation trigger MUST exist
- FR-052: All workers busy MUST trigger
- FR-053: Worker queue depth MUST matter
- FR-054: Per-worker queue threshold: 3
- FR-055: Response time trigger MUST exist
- FR-056: Slow response MUST trigger
- FR-057: Response threshold: 2x baseline
- FR-058: Baseline MUST be learned
- FR-059: Learning period: 10 tasks
- FR-060: Load average MUST be sampled

### FR-061 to FR-075: Cost Controls

- FR-061: Budget check MUST precede burst
- FR-062: Over budget MUST block burst
- FR-063: Near budget MUST warn
- FR-064: Cost per burst MUST estimate
- FR-065: ROI calculation MUST exist
- FR-066: Time saved vs cost
- FR-067: Minimum ROI threshold MUST exist
- FR-068: Default ROI: 2x (save 2hr/$1)
- FR-069: Max concurrent instances MUST limit
- FR-070: Default max instances: 5
- FR-071: Cooldown period MUST exist
- FR-072: Cooldown prevents thrashing
- FR-073: Default cooldown: 5 minutes
- FR-074: Burst ramp MUST be gradual
- FR-075: Start with 1, scale up

---

## Non-Functional Requirements

- NFR-001: Decision in <100ms
- NFR-002: Low overhead monitoring
- NFR-003: No false positive bursts
- NFR-004: Minimal false negatives
- NFR-005: Cost-aware decisions
- NFR-006: Structured logging
- NFR-007: Metrics on decisions
- NFR-008: Audit trail
- NFR-009: Configurable thresholds
- NFR-010: Graceful degradation

---

## User Manual Documentation

### Configuration

```yaml
burst:
  enabled: true
  heuristics:
    queueDepth:
      enabled: true
      threshold: 10
    queueWait:
      enabled: true
      thresholdMinutes: 5
    cpuUtilization:
      enabled: true
      threshold: 80
      sustainedMinutes: 2
    workerSaturation:
      enabled: true
  controls:
    maxInstances: 5
    cooldownMinutes: 5
    maxHourlyBudget: 10.00
    minRoi: 2.0
```

### Burst Triggers

| Trigger | Default | Description |
|---------|---------|-------------|
| Queue Depth | 10 tasks | Too many waiting |
| Queue Wait | 5 min | Tasks waiting too long |
| CPU High | 80% | Local CPU exhausted |
| All Workers Busy | true | No idle workers |
| Priority Task | P0 | High priority waiting |

### Burst Decision Output

```json
{
  "shouldBurst": true,
  "targetCount": 2,
  "reasons": [
    "Queue depth 15 > threshold 10",
    "CPU utilization 85% for 3 minutes"
  ],
  "estimatedCost": 4.20,
  "estimatedTimeSaved": "45 minutes"
}
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Queue depth triggers
- [ ] AC-002: Queue wait triggers
- [ ] AC-003: CPU triggers
- [ ] AC-004: Worker saturation triggers
- [ ] AC-005: Budget blocks burst
- [ ] AC-006: Cooldown prevents thrash
- [ ] AC-007: Mode compliance works
- [ ] AC-008: Metrics emitted
- [ ] AC-009: Logging complete
- [ ] AC-010: ROI calculated

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Queue heuristics
- [ ] UT-002: Load heuristics
- [ ] UT-003: Cost controls
- [ ] UT-004: Mode compliance

### Integration Tests

- [ ] IT-001: End-to-end burst
- [ ] IT-002: Cooldown behavior
- [ ] IT-003: Budget enforcement
- [ ] IT-004: Multi-heuristic

---

## Implementation Prompt

### Interface

```csharp
public interface IBurstHeuristics
{
    Task<BurstDecision> EvaluateAsync(
        BurstContext context,
        CancellationToken ct = default);
}

public record BurstContext(
    int QueueDepth,
    TimeSpan OldestTaskWait,
    double CpuUtilization,
    double MemoryUtilization,
    int ActiveWorkers,
    int TotalWorkers,
    int RunningCloudInstances,
    decimal TodaySpend,
    OperatingMode Mode);

public record BurstDecision(
    bool ShouldBurst,
    int TargetInstanceCount,
    IReadOnlyList<string> Reasons,
    decimal EstimatedCost,
    TimeSpan EstimatedTimeSaved,
    bool BlockedByMode,
    bool BlockedByBudget,
    bool BlockedByCooldown);
```

### Heuristic Interface

```csharp
public interface IBurstHeuristic
{
    string Name { get; }
    bool Enabled { get; }
    Task<HeuristicResult> EvaluateAsync(
        BurstContext context,
        CancellationToken ct);
}

public record HeuristicResult(
    bool ShouldTrigger,
    double Confidence,
    string Reason,
    int SuggestedInstances);
```

### Built-in Heuristics

```csharp
public class QueueDepthHeuristic : IBurstHeuristic { }
public class QueueWaitHeuristic : IBurstHeuristic { }
public class CpuUtilizationHeuristic : IBurstHeuristic { }
public class WorkerSaturationHeuristic : IBurstHeuristic { }
public class PriorityTaskHeuristic : IBurstHeuristic { }
```

---

**End of Task 033 Specification**