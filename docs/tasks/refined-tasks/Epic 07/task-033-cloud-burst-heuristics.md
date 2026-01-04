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