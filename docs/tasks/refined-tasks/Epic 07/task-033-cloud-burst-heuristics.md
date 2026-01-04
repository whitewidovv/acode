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

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Task 031 EC2 | IEc2InstanceManager | Burst → Provision | Burst triggers EC2 creation |
| Task 032 Placement | IPlacementEngine | Decision → Placement | Burst informs placement |
| Task 027 Worker Pool | IWorkerPool | Stats → Context | Worker saturation data |
| Task 026 Queue | ITaskQueue | Depth/Wait → Context | Queue metrics for heuristics |
| agent-config.yml | Config parser | Options → Engine | Heuristic configuration |
| Metrics System | IMetrics | Stats → Metrics | Burst decision telemetry |
| Event System | IEventPublisher | Events → Subscribers | Burst/Block events |

### Mode Compliance

| Mode | Burst Behavior |
|------|----------------|
| local-only | DISABLED - Never burst to cloud |
| airgapped | DISABLED - Never burst to cloud |
| burst | ENABLED - Cloud scaling allowed |

Heuristics MUST respect mode constraints. Mode violations MUST be logged.

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Heuristic exception | Catch in engine | Skip heuristic | Reduced accuracy |
| All heuristics fail | No results | Conservative (no burst) | Tasks stay local |
| Budget data unavailable | Null budget | Allow burst with warning | May overspend |
| Queue metrics stale | Age check | Use last known | Slightly delayed burst |
| EC2 provision fails | Provision exception | Retry with backoff | Delayed scaling |
| Cooldown stuck | Time overflow | Reset cooldown | Burst unblocked |
| Mode check fails | Exception | Block burst | Safe behavior |
| Concurrent decisions | Race condition | Single-flight pattern | One decision |

---

## Assumptions

1. Queue depth and wait time metrics are available from Task 026
2. Worker utilization is available from Task 027 worker pool
3. System load metrics (CPU, memory) are collected by infrastructure
4. Cloud pricing data is available for cost estimation
5. Operating mode is accessible globally and consistent
6. Cooldown applies per burst event, not per heuristic
7. Budget tracking is accurate within reasonable tolerance
8. Burst decisions are made synchronously (not async queue)

---

## Security Considerations

1. Burst decisions MUST NOT expose internal metrics to unauthorized users
2. Budget data MUST NOT be logged in plain text
3. Cloud credentials MUST NOT be accessed during heuristic evaluation
4. Cost estimates MUST NOT include pricing details in logs
5. Burst events MUST be logged for audit purposes
6. Mode compliance MUST be enforced at decision time, not deferred
7. Heuristic configuration MUST be validated for reasonable thresholds
8. Denial-of-service via forced bursting MUST be prevented by cooldown

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

### Burst Engine

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033-01 | `IBurstHeuristics` interface MUST exist | P0 |
| FR-033-02 | `EvaluateAsync` MUST return `BurstDecision` | P0 |
| FR-033-03 | Input: `BurstContext` with current system state | P0 |
| FR-033-04 | Output: `BurstDecision` with shouldBurst flag | P0 |
| FR-033-05 | Decision MUST include human-readable reasons | P1 |
| FR-033-06 | Decision MUST include suggested target count | P0 |
| FR-033-07 | Multiple heuristics MUST be combinable | P0 |
| FR-033-08 | Heuristic weights MUST be configurable | P1 |
| FR-033-09 | Any trigger activates burst (OR logic) default | P0 |
| FR-033-10 | All triggers required (AND logic) optional | P2 |
| FR-033-11 | Default logic: OR (any trigger bursts) | P0 |
| FR-033-12 | Heuristics MUST be pluggable via DI | P1 |
| FR-033-13 | Each heuristic MUST be individually configurable | P1 |
| FR-033-14 | Each heuristic MUST be individually disableable | P1 |
| FR-033-15 | Burst MUST respect operating mode | P0 |
| FR-033-16 | local-only mode MUST block all bursting | P0 |
| FR-033-17 | airgapped mode MUST block all bursting | P0 |
| FR-033-18 | Burst decisions MUST be logged | P0 |
| FR-033-19 | Burst decisions MUST emit metrics | P1 |
| FR-033-20 | Burst history MUST be queryable | P2 |

### Queue-Based Heuristics

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033-21 | Queue depth trigger MUST exist | P0 |
| FR-033-22 | Depth threshold MUST be configurable | P0 |
| FR-033-23 | Default depth threshold: 10 pending tasks | P0 |
| FR-033-24 | Queue wait time trigger MUST exist | P0 |
| FR-033-25 | Wait threshold MUST be configurable | P0 |
| FR-033-26 | Default wait threshold: 5 minutes | P0 |
| FR-033-27 | Queue growth rate trigger MUST exist | P1 |
| FR-033-28 | Fast queue growth MUST trigger burst | P1 |
| FR-033-29 | Growth rate: tasks added per minute | P1 |
| FR-033-30 | Default growth threshold: 5 tasks/min | P1 |
| FR-033-31 | Priority queue trigger MUST exist | P1 |
| FR-033-32 | High priority tasks MUST trigger faster | P1 |
| FR-033-33 | Priority multiplier MUST exist | P1 |
| FR-033-34 | Default priority multiplier: 2x | P1 |
| FR-033-35 | Task starvation trigger MUST exist | P1 |
| FR-033-36 | Starvation: task waiting beyond threshold | P1 |
| FR-033-37 | Default starvation threshold: 15 minutes | P1 |
| FR-033-38 | Queue composition MUST influence decision | P2 |
| FR-033-39 | Large tasks MUST weight more heavily | P2 |
| FR-033-40 | Estimated runtime MUST factor into decision | P2 |

### Load-Based Heuristics

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033-41 | CPU utilization trigger MUST exist | P0 |
| FR-033-42 | CPU threshold MUST be configurable | P0 |
| FR-033-43 | Default CPU threshold: 80% utilization | P0 |
| FR-033-44 | Sustained high CPU MUST trigger burst | P0 |
| FR-033-45 | Sustained period: 2 minutes minimum | P0 |
| FR-033-46 | Memory utilization trigger MUST exist | P1 |
| FR-033-47 | Memory threshold MUST be configurable | P1 |
| FR-033-48 | Default memory threshold: 80% utilization | P1 |
| FR-033-49 | Disk utilization trigger MUST exist | P2 |
| FR-033-50 | Default disk threshold: 90% utilization | P2 |
| FR-033-51 | Worker saturation trigger MUST exist | P0 |
| FR-033-52 | All workers busy MUST trigger burst | P0 |
| FR-033-53 | Per-worker queue depth MUST matter | P1 |
| FR-033-54 | Default per-worker queue threshold: 3 | P1 |
| FR-033-55 | Response time trigger MUST exist | P2 |
| FR-033-56 | Slow response (vs baseline) MUST trigger | P2 |
| FR-033-57 | Response threshold: 2x baseline | P2 |
| FR-033-58 | Baseline MUST be learned from history | P2 |
| FR-033-59 | Learning period: first 10 tasks | P2 |
| FR-033-60 | Load average MUST be sampled periodically | P1 |

### Cost Controls

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033-61 | Budget check MUST precede burst decision | P0 |
| FR-033-62 | Over daily budget MUST block burst | P0 |
| FR-033-63 | Near budget (>80%) MUST log warning | P1 |
| FR-033-64 | Cost per burst MUST be estimated | P1 |
| FR-033-65 | ROI calculation MUST exist | P1 |
| FR-033-66 | ROI: time saved vs cost incurred | P1 |
| FR-033-67 | Minimum ROI threshold MUST exist | P1 |
| FR-033-68 | Default minimum ROI: 2.0 (save 2hr/$1) | P1 |
| FR-033-69 | Max concurrent instances MUST be enforced | P0 |
| FR-033-70 | Default max instances: 5 | P0 |
| FR-033-71 | Cooldown period MUST exist | P0 |
| FR-033-72 | Cooldown prevents burst thrashing | P0 |
| FR-033-73 | Default cooldown: 5 minutes | P0 |
| FR-033-74 | Burst ramp MUST be gradual | P1 |
| FR-033-75 | Start with 1 instance, scale up | P1 |

---

## Non-Functional Requirements

### Performance

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-033-01 | Burst decision evaluation | <100ms | P0 |
| NFR-033-02 | Heuristic evaluation overhead | <10ms each | P0 |
| NFR-033-03 | Context construction | <20ms | P1 |
| NFR-033-04 | Metrics collection overhead | <5% CPU | P1 |
| NFR-033-05 | Memory for history tracking | <10MB | P1 |
| NFR-033-06 | Concurrent evaluations | Thread-safe | P0 |
| NFR-033-07 | Single-flight decisions | One at a time | P1 |
| NFR-033-08 | Heuristic parallelization | Supported | P2 |
| NFR-033-09 | Cooldown lookup | O(1) | P0 |
| NFR-033-10 | History query | <50ms | P2 |

### Reliability

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-033-11 | False positive rate | <5% | P0 |
| NFR-033-12 | False negative rate | <10% | P1 |
| NFR-033-13 | Mode compliance | Always enforced | P0 |
| NFR-033-14 | Budget compliance | Always enforced | P0 |
| NFR-033-15 | Heuristic isolation | Failure doesn't cascade | P0 |
| NFR-033-16 | Graceful degradation | Partial heuristics OK | P1 |
| NFR-033-17 | Cooldown persistence | Survive restart | P2 |
| NFR-033-18 | Decision determinism | Repeatable given state | P1 |
| NFR-033-19 | Concurrent burst prevention | Single-flight | P0 |
| NFR-033-20 | Recovery from stuck state | Auto-reset | P1 |

### Observability

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-033-21 | Structured logging | All decisions | P0 |
| NFR-033-22 | Burst trigger metric | Counter by trigger type | P1 |
| NFR-033-23 | Burst blocked metric | Counter by reason | P1 |
| NFR-033-24 | Cooldown remaining metric | Gauge | P2 |
| NFR-033-25 | BurstTriggeredEvent | Published on burst | P0 |
| NFR-033-26 | BurstBlockedEvent | Published on block | P0 |
| NFR-033-27 | BurstCooldownEvent | Published when in cooldown | P1 |
| NFR-033-28 | Heuristic evaluation trace | Debug level | P2 |
| NFR-033-29 | Cost estimation logging | Info level | P1 |
| NFR-033-30 | Decision audit trail | Queryable history | P2 |

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

### Burst Engine Core
- [ ] AC-001: `IBurstHeuristics` interface exists
- [ ] AC-002: `EvaluateAsync` returns `BurstDecision`
- [ ] AC-003: Decision includes shouldBurst flag
- [ ] AC-004: Decision includes target instance count
- [ ] AC-005: Decision includes human-readable reasons
- [ ] AC-006: Decision includes active triggers
- [ ] AC-007: Multiple heuristics evaluated
- [ ] AC-008: OR logic triggers on any heuristic
- [ ] AC-009: AND logic option works
- [ ] AC-010: Heuristics pluggable via DI

### Queue-Based Triggers
- [ ] AC-011: Queue depth > 10 triggers burst
- [ ] AC-012: Queue depth threshold configurable
- [ ] AC-013: Queue wait > 5min triggers burst
- [ ] AC-014: Queue wait threshold configurable
- [ ] AC-015: Queue growth rate tracked
- [ ] AC-016: Fast growth triggers burst
- [ ] AC-017: Priority tasks trigger faster
- [ ] AC-018: Priority multiplier works
- [ ] AC-019: Starvation (15min wait) triggers
- [ ] AC-020: Large tasks weighted more

### Load-Based Triggers
- [ ] AC-021: CPU > 80% sustained triggers
- [ ] AC-022: CPU threshold configurable
- [ ] AC-023: Sustained period (2min) enforced
- [ ] AC-024: Memory > 80% triggers
- [ ] AC-025: Memory threshold configurable
- [ ] AC-026: Worker saturation triggers
- [ ] AC-027: All workers busy detected
- [ ] AC-028: Per-worker queue depth tracked
- [ ] AC-029: Load samples collected periodically
- [ ] AC-030: Disk > 90% triggers (optional)

### Cost Controls
- [ ] AC-031: Budget checked before burst
- [ ] AC-032: Over budget blocks burst
- [ ] AC-033: Near budget (80%) logs warning
- [ ] AC-034: Cost per burst estimated
- [ ] AC-035: ROI calculated (time saved / cost)
- [ ] AC-036: Below ROI threshold blocks burst
- [ ] AC-037: Max instances (5) enforced
- [ ] AC-038: Cooldown (5min) prevents thrashing
- [ ] AC-039: Gradual ramp (start with 1)
- [ ] AC-040: Running instance count tracked

### Mode Compliance
- [ ] AC-041: local-only mode blocks all bursts
- [ ] AC-042: airgapped mode blocks all bursts
- [ ] AC-043: burst mode allows bursting
- [ ] AC-044: Mode violation logged
- [ ] AC-045: BurstBlockedEvent published for mode

### Observability
- [ ] AC-046: BurstTriggeredEvent published
- [ ] AC-047: BurstBlockedEvent published
- [ ] AC-048: BurstCooldownEvent published
- [ ] AC-049: Decision logging complete
- [ ] AC-050: Metrics emitted by trigger type
- [ ] AC-051: Cooldown remaining queryable
- [ ] AC-052: Decision history queryable

---

## User Verification Scenarios

### Scenario 1: Queue Depth Triggers Burst
**Persona:** Developer with many pending tasks  
**Preconditions:** Mode is burst, 15 tasks queued  
**Steps:**
1. Queue 15 tasks
2. Observe heuristics evaluation
3. Check burst decision
4. Verify EC2 instance provisioned

**Verification Checklist:**
- [ ] Queue depth heuristic triggered
- [ ] Decision shows "15 > 10"
- [ ] targetInstanceCount > 0
- [ ] EC2 provisioning started

### Scenario 2: CPU Sustained High Triggers Burst
**Persona:** Developer with CPU-intensive workload  
**Preconditions:** Local CPU at 90% for 3 minutes  
**Steps:**
1. Run CPU-intensive tasks locally
2. Wait for sustained period
3. Check burst decision
4. Verify CPU trigger reason

**Verification Checklist:**
- [ ] CPU heuristic tracked samples
- [ ] Sustained period exceeded
- [ ] Burst triggered
- [ ] Reason includes "CPU 90% for 3 minutes"

### Scenario 3: Budget Blocks Burst
**Persona:** Developer near daily budget  
**Preconditions:** Daily budget $10, spent $10.50  
**Steps:**
1. Queue many tasks to trigger burst
2. Observe budget check
3. Burst blocked
4. Check blocked event

**Verification Checklist:**
- [ ] Budget checked first
- [ ] Burst blocked (blockedByBudget=true)
- [ ] BurstBlockedEvent published
- [ ] Clear reason: "Budget exceeded"

### Scenario 4: Cooldown Prevents Thrashing
**Persona:** Developer with fluctuating load  
**Preconditions:** Just burst 2 minutes ago  
**Steps:**
1. Previous burst completed
2. Load spikes again within cooldown
3. Burst requested
4. Cooldown enforced

**Verification Checklist:**
- [ ] Cooldown remaining calculated
- [ ] Burst blocked (blockedByCooldown=true)
- [ ] CooldownRemaining in decision
- [ ] BurstCooldownEvent published

### Scenario 5: Mode Compliance Enforced
**Persona:** Developer in airgapped environment  
**Preconditions:** Mode is airgapped, heavy queue  
**Steps:**
1. Set mode to airgapped
2. Queue 20 tasks
3. Heuristics would normally trigger
4. Burst blocked by mode

**Verification Checklist:**
- [ ] Mode checked first
- [ ] Burst blocked (blockedByMode=true)
- [ ] No EC2 attempted
- [ ] Clear error message

### Scenario 6: Multi-Heuristic Combination
**Persona:** Developer with multiple triggers active  
**Preconditions:** Queue depth 12, CPU 85%, all workers busy  
**Steps:**
1. All three conditions true
2. Evaluate heuristics
3. Check all triggers in decision
4. Instance count reflects severity

**Verification Checklist:**
- [ ] All three heuristics triggered
- [ ] Decision.ActiveTriggers has 3 entries
- [ ] Reasons list all three
- [ ] Higher instance count suggested

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-033-01 | Queue depth threshold triggers | FR-033-21 |
| UT-033-02 | Queue wait threshold triggers | FR-033-24 |
| UT-033-03 | Queue growth rate calculation | FR-033-27 |
| UT-033-04 | Priority task multiplier | FR-033-33 |
| UT-033-05 | CPU sustained period tracking | FR-033-45 |
| UT-033-06 | Memory threshold triggers | FR-033-46 |
| UT-033-07 | Worker saturation detection | FR-033-51 |
| UT-033-08 | OR logic any trigger | FR-033-09 |
| UT-033-09 | AND logic all triggers | FR-033-10 |
| UT-033-10 | Budget block | FR-033-62 |
| UT-033-11 | Max instances limit | FR-033-69 |
| UT-033-12 | Cooldown calculation | FR-033-71 |
| UT-033-13 | Mode compliance local-only | FR-033-16 |
| UT-033-14 | Mode compliance airgapped | FR-033-17 |
| UT-033-15 | ROI threshold enforcement | FR-033-67 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-033-01 | End-to-end burst trigger | E2E |
| IT-033-02 | Queue depth + EC2 provision | Integration |
| IT-033-03 | Cooldown behavior over time | FR-033-72 |
| IT-033-04 | Budget enforcement | FR-033-61 |
| IT-033-05 | Mode compliance e2e | Mode table |
| IT-033-06 | Multi-heuristic combination | FR-033-07 |
| IT-033-07 | Gradual ramp 1→N | FR-033-74 |
| IT-033-08 | Event publishing | NFR-033-25-27 |
| IT-033-09 | Metrics emission | NFR-033-22-23 |
| IT-033-10 | Performance <100ms | NFR-033-01 |
| IT-033-11 | Concurrent decision prevention | NFR-033-19 |
| IT-033-12 | History query | FR-033-20 |
| IT-033-13 | Heuristic disable | FR-033-14 |
| IT-033-14 | False positive rate | NFR-033-11 |
| IT-033-15 | Recovery from stuck cooldown | NFR-033-20 |

---

## Implementation Prompt

### Part 1: File Structure + Domain Models

```
src/
├── Acode.Domain/
│   └── Compute/
│       └── Burst/
│           ├── BurstTrigger.cs
│           ├── HeuristicType.cs
│           └── Events/
│               ├── BurstTriggeredEvent.cs
│               ├── BurstBlockedEvent.cs
│               └── BurstCooldownEvent.cs
├── Acode.Application/
│   └── Compute/
│       └── Burst/
│           ├── IBurstHeuristics.cs
│           ├── IBurstHeuristic.cs
│           ├── BurstContext.cs
│           ├── BurstDecision.cs
│           ├── HeuristicResult.cs
│           └── BurstOptions.cs
└── Acode.Infrastructure/
    └── Compute/
        └── Burst/
            ├── BurstHeuristicsEngine.cs
            ├── BurstCooldownTracker.cs
            ├── BurstHistoryRecorder.cs
            └── Heuristics/
                ├── QueueDepthHeuristic.cs
                ├── QueueWaitHeuristic.cs
                ├── QueueGrowthHeuristic.cs
                ├── CpuUtilizationHeuristic.cs
                ├── MemoryUtilizationHeuristic.cs
                ├── WorkerSaturationHeuristic.cs
                └── PriorityTaskHeuristic.cs
```

```csharp
// src/Acode.Domain/Compute/Burst/BurstTrigger.cs
namespace Acode.Domain.Compute.Burst;

public enum BurstTrigger
{
    QueueDepth,
    QueueWait,
    QueueGrowth,
    CpuUtilization,
    MemoryUtilization,
    WorkerSaturation,
    PriorityTask,
    TaskStarvation
}

// src/Acode.Domain/Compute/Burst/HeuristicType.cs
namespace Acode.Domain.Compute.Burst;

public enum HeuristicType
{
    QueueBased,
    LoadBased,
    TaskBased
}

// src/Acode.Domain/Compute/Burst/Events/BurstTriggeredEvent.cs
namespace Acode.Domain.Compute.Burst.Events;

public sealed record BurstTriggeredEvent(
    IReadOnlyList<BurstTrigger> Triggers,
    int TargetInstanceCount,
    decimal EstimatedCost,
    TimeSpan EstimatedTimeSaved,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Burst/Events/BurstBlockedEvent.cs
namespace Acode.Domain.Compute.Burst.Events;

public sealed record BurstBlockedEvent(
    string Reason,
    bool BlockedByMode,
    bool BlockedByBudget,
    bool BlockedByCooldown,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Burst/Events/BurstCooldownEvent.cs
namespace Acode.Domain.Compute.Burst.Events;

public sealed record BurstCooldownEvent(
    TimeSpan CooldownRemaining,
    DateTimeOffset CooldownEnds,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 033 Specification - Part 1/4**

### Part 2: Application Interfaces

```csharp
// src/Acode.Application/Compute/Burst/BurstContext.cs
namespace Acode.Application.Compute.Burst;

public sealed record BurstContext
{
    public int QueueDepth { get; init; }
    public TimeSpan OldestTaskWait { get; init; }
    public double QueueGrowthRate { get; init; } // tasks per minute
    public double CpuUtilization { get; init; }
    public double MemoryUtilization { get; init; }
    public double DiskUtilization { get; init; }
    public int ActiveWorkers { get; init; }
    public int TotalWorkers { get; init; }
    public int RunningCloudInstances { get; init; }
    public decimal TodaySpend { get; init; }
    public decimal? DailyBudget { get; init; }
    public OperatingMode Mode { get; init; }
    public bool HasHighPriorityTasks { get; init; }
    public DateTimeOffset? LastBurstTime { get; init; }
}

// src/Acode.Application/Compute/Burst/HeuristicResult.cs
namespace Acode.Application.Compute.Burst;

public sealed record HeuristicResult
{
    public required BurstTrigger Trigger { get; init; }
    public bool ShouldTrigger { get; init; }
    public double Confidence { get; init; } // 0.0 to 1.0
    public string? Reason { get; init; }
    public int SuggestedInstances { get; init; } = 1;
}

// src/Acode.Application/Compute/Burst/BurstDecision.cs
namespace Acode.Application.Compute.Burst;

public sealed record BurstDecision
{
    public bool ShouldBurst { get; init; }
    public int TargetInstanceCount { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
    public IReadOnlyList<BurstTrigger> ActiveTriggers { get; init; } = [];
    public decimal EstimatedCost { get; init; }
    public TimeSpan EstimatedTimeSaved { get; init; }
    public bool BlockedByMode { get; init; }
    public bool BlockedByBudget { get; init; }
    public bool BlockedByCooldown { get; init; }
    public TimeSpan? CooldownRemaining { get; init; }
}

// src/Acode.Application/Compute/Burst/BurstOptions.cs
namespace Acode.Application.Compute.Burst;

public sealed record BurstOptions
{
    public bool Enabled { get; init; } = true;
    public int MaxInstances { get; init; } = 5;
    public TimeSpan Cooldown { get; init; } = TimeSpan.FromMinutes(5);
    public decimal? MaxHourlyBudget { get; init; }
    public double MinRoi { get; init; } = 2.0;
    public bool UseOrLogic { get; init; } = true; // OR vs AND triggers
}

// src/Acode.Application/Compute/Burst/IBurstHeuristic.cs
namespace Acode.Application.Compute.Burst;

public interface IBurstHeuristic
{
    string Name { get; }
    HeuristicType Type { get; }
    bool Enabled { get; }
    
    Task<HeuristicResult> EvaluateAsync(
        BurstContext context,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Burst/IBurstHeuristics.cs
namespace Acode.Application.Compute.Burst;

public interface IBurstHeuristics
{
    Task<BurstDecision> EvaluateAsync(
        BurstContext context,
        CancellationToken ct = default);
    
    IReadOnlyList<IBurstHeuristic> GetEnabledHeuristics();
}
```

**End of Task 033 Specification - Part 2/4**

### Part 3: Infrastructure Implementation

```csharp
// src/Acode.Infrastructure/Compute/Burst/BurstCooldownTracker.cs
namespace Acode.Infrastructure.Compute.Burst;

public sealed class BurstCooldownTracker
{
    private DateTimeOffset? _lastBurstTime;
    private readonly TimeSpan _cooldownPeriod;
    
    public BurstCooldownTracker(TimeSpan cooldownPeriod)
    {
        _cooldownPeriod = cooldownPeriod;
    }
    
    public void RecordBurst() => _lastBurstTime = DateTimeOffset.UtcNow;
    
    public bool IsInCooldown(out TimeSpan remaining)
    {
        if (_lastBurstTime == null)
        {
            remaining = TimeSpan.Zero;
            return false;
        }
        
        var elapsed = DateTimeOffset.UtcNow - _lastBurstTime.Value;
        remaining = _cooldownPeriod - elapsed;
        return remaining > TimeSpan.Zero;
    }
}

// src/Acode.Infrastructure/Compute/Burst/Heuristics/QueueDepthHeuristic.cs
namespace Acode.Infrastructure.Compute.Burst.Heuristics;

public sealed class QueueDepthHeuristic : IBurstHeuristic
{
    private readonly int _threshold;
    
    public string Name => "queue-depth";
    public HeuristicType Type => HeuristicType.QueueBased;
    public bool Enabled { get; }
    
    public QueueDepthHeuristic(int threshold = 10, bool enabled = true)
    {
        _threshold = threshold;
        Enabled = enabled;
    }
    
    public Task<HeuristicResult> EvaluateAsync(BurstContext context, CancellationToken ct)
    {
        var shouldTrigger = context.QueueDepth > _threshold;
        var confidence = Math.Min((double)context.QueueDepth / _threshold / 2, 1.0);
        var instances = (context.QueueDepth / _threshold) + 1;
        
        return Task.FromResult(new HeuristicResult
        {
            Trigger = BurstTrigger.QueueDepth,
            ShouldTrigger = shouldTrigger,
            Confidence = confidence,
            Reason = shouldTrigger ? $"Queue depth {context.QueueDepth} > threshold {_threshold}" : null,
            SuggestedInstances = instances
        });
    }
}

// src/Acode.Infrastructure/Compute/Burst/Heuristics/CpuUtilizationHeuristic.cs
namespace Acode.Infrastructure.Compute.Burst.Heuristics;

public sealed class CpuUtilizationHeuristic : IBurstHeuristic
{
    private readonly double _threshold;
    private readonly TimeSpan _sustainedPeriod;
    private readonly Queue<(DateTimeOffset Time, double Value)> _samples = new();
    
    public string Name => "cpu-utilization";
    public HeuristicType Type => HeuristicType.LoadBased;
    public bool Enabled { get; }
    
    public CpuUtilizationHeuristic(double threshold = 0.80, TimeSpan? sustainedPeriod = null, bool enabled = true)
    {
        _threshold = threshold;
        _sustainedPeriod = sustainedPeriod ?? TimeSpan.FromMinutes(2);
        Enabled = enabled;
    }
    
    public Task<HeuristicResult> EvaluateAsync(BurstContext context, CancellationToken ct)
    {
        _samples.Enqueue((DateTimeOffset.UtcNow, context.CpuUtilization));
        PruneSamples();
        
        var sustained = IsSustainedHigh();
        return Task.FromResult(new HeuristicResult
        {
            Trigger = BurstTrigger.CpuUtilization,
            ShouldTrigger = sustained,
            Confidence = sustained ? context.CpuUtilization : 0.0,
            Reason = sustained ? $"CPU sustained at {context.CpuUtilization:P0} for {_sustainedPeriod.TotalMinutes} minutes" : null,
            SuggestedInstances = 1
        });
    }
    
    private bool IsSustainedHigh() =>
        _samples.Count >= 2 && _samples.All(s => s.Value >= _threshold);
}
```

**End of Task 033 Specification - Part 3/4**

### Part 4: Burst Engine + Checklist

```csharp
// src/Acode.Infrastructure/Compute/Burst/BurstHeuristicsEngine.cs
namespace Acode.Infrastructure.Compute.Burst;

public sealed class BurstHeuristicsEngine : IBurstHeuristics
{
    private readonly IEnumerable<IBurstHeuristic> _heuristics;
    private readonly BurstCooldownTracker _cooldown;
    private readonly BurstOptions _options;
    private readonly IEventPublisher _events;
    private readonly ILogger<BurstHeuristicsEngine> _logger;
    
    public async Task<BurstDecision> EvaluateAsync(
        BurstContext context,
        CancellationToken ct = default)
    {
        // Mode check first
        if (context.Mode != OperatingMode.Burst)
        {
            await _events.PublishAsync(new BurstBlockedEvent("Mode does not allow burst", true, false, false, DateTimeOffset.UtcNow), ct);
            return new BurstDecision { BlockedByMode = true };
        }
        
        // Cooldown check
        if (_cooldown.IsInCooldown(out var remaining))
        {
            await _events.PublishAsync(new BurstCooldownEvent(remaining, DateTimeOffset.UtcNow + remaining, DateTimeOffset.UtcNow), ct);
            return new BurstDecision { BlockedByCooldown = true, CooldownRemaining = remaining };
        }
        
        // Budget check
        if (_options.MaxHourlyBudget.HasValue && context.TodaySpend >= _options.MaxHourlyBudget * 24)
        {
            await _events.PublishAsync(new BurstBlockedEvent("Budget exceeded", false, true, false, DateTimeOffset.UtcNow), ct);
            return new BurstDecision { BlockedByBudget = true };
        }
        
        // Evaluate all heuristics
        var results = await EvaluateHeuristicsAsync(context, ct);
        var triggers = results.Where(r => r.ShouldTrigger).ToList();
        
        var shouldBurst = _options.UseOrLogic 
            ? triggers.Any()
            : triggers.Count == results.Count;
        
        if (!shouldBurst)
            return new BurstDecision { ShouldBurst = false };
        
        var instanceCount = Math.Min(
            triggers.Max(t => t.SuggestedInstances),
            _options.MaxInstances - context.RunningCloudInstances);
        
        var decision = new BurstDecision
        {
            ShouldBurst = instanceCount > 0,
            TargetInstanceCount = instanceCount,
            Reasons = triggers.Select(t => t.Reason!).ToList(),
            ActiveTriggers = triggers.Select(t => t.Trigger).ToList(),
            EstimatedCost = EstimateCost(instanceCount),
            EstimatedTimeSaved = EstimateTimeSaved(context, instanceCount)
        };
        
        if (decision.ShouldBurst)
        {
            _cooldown.RecordBurst();
            await _events.PublishAsync(new BurstTriggeredEvent(
                decision.ActiveTriggers, decision.TargetInstanceCount,
                decision.EstimatedCost, decision.EstimatedTimeSaved, DateTimeOffset.UtcNow), ct);
        }
        
        return decision;
    }
    
    public IReadOnlyList<IBurstHeuristic> GetEnabledHeuristics() =>
        _heuristics.Where(h => h.Enabled).ToList();
}
```

### Implementation Checklist

| Step | Action | Verification |
|------|--------|--------------|
| 1 | Create domain enums (BurstTrigger, HeuristicType) | Enums compile |
| 2 | Add burst events | Event serialization verified |
| 3 | Define BurstContext, BurstDecision, HeuristicResult | Records compile |
| 4 | Create IBurstHeuristic, IBurstHeuristics interfaces | Interface contracts clear |
| 5 | Implement BurstCooldownTracker | Cooldown logic verified |
| 6 | Implement QueueDepthHeuristic | Threshold triggers correctly |
| 7 | Implement QueueWaitHeuristic | Wait time triggers correctly |
| 8 | Implement QueueGrowthHeuristic | Growth rate calculated |
| 9 | Implement CpuUtilizationHeuristic | Sustained high CPU triggers |
| 10 | Implement MemoryUtilizationHeuristic | Memory triggers work |
| 11 | Implement WorkerSaturationHeuristic | All workers busy triggers |
| 12 | Implement PriorityTaskHeuristic | High priority triggers faster |
| 13 | Implement BurstHeuristicsEngine | OR/AND logic works |
| 14 | Add mode compliance | local-only/airgapped block burst |
| 15 | Add budget enforcement | Over budget blocks burst |
| 16 | Register all heuristics in DI | All heuristics resolved |

### Rollout Plan

1. **Phase 1**: Implement cooldown tracker and base infrastructure
2. **Phase 2**: Add queue-based heuristics (depth, wait, growth)
3. **Phase 3**: Add load-based heuristics (CPU, memory, worker saturation)
4. **Phase 4**: Build BurstHeuristicsEngine with OR/AND logic
5. **Phase 5**: Add budget and mode enforcement, integration tests

**End of Task 033 Specification**