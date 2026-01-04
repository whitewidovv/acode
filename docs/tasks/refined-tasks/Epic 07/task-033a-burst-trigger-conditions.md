# Task 033.a: Burst Trigger Conditions

**Priority:** P1 – High  
**Tier:** L – Cloud Integration  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 033 (Burst Heuristics)  

---

## Description

Task 033.a implements specific burst trigger conditions. Each trigger MUST be independently configurable. Triggers MUST emit signals for the burst engine.

Triggers are the individual conditions that indicate bursting is needed. Multiple triggers may fire simultaneously. Each trigger has its own threshold and logic.

This task covers trigger implementations. Aggregation is in 033.b. Cooldown is in 033.c.

### Business Value

Configurable triggers provide:
- Fine-grained control
- Environment-specific tuning
- Predictable behavior
- Easy troubleshooting

### Scope Boundaries

This task covers trigger implementations. Aggregation logic is in 033.b. Rate limiting is in 033.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Task 033 Burst Engine | IBurstTrigger | Signal → Engine | Triggers feed into heuristics |
| Task 026 Queue | ITaskQueue | Depth/Wait → Context | Queue metrics for triggers |
| Task 027 Worker Pool | IWorkerPool | Status → Context | Worker saturation data |
| CPU Sampler | ICpuSampler | Samples → Trigger | System CPU metrics |
| Configuration | TriggerOptions | Config → Trigger | Per-trigger settings |
| Metrics System | IMetrics | Stats → Metrics | Trigger telemetry |
| Event System | IEventPublisher | Events → Subscribers | Fire/Reset events |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Metric unavailable | Null/exception | Skip trigger with warning | Reduced accuracy |
| Threshold undefined | Config validation | Use default value | Expected behavior |
| Trigger exception | Catch in evaluation | Return non-triggered signal | Trigger disabled |
| Sampling failure | Sample exception | Use last known value | Slightly stale data |
| All triggers fail | No signals | Conservative (no burst) | Tasks stay local |
| Moving average overflow | Numeric check | Reset window | Brief accuracy loss |
| Concurrent evaluation | Race condition | Thread-safe design | No issue |
| Sustained period stuck | Time overflow | Auto-reset | Trigger re-enabled |

---

## Assumptions

1. Queue depth is accurate and updated in real-time from Task 026
2. Worker status is available from Task 027 worker pool
3. CPU sampling is available via system APIs or monitoring
4. Triggers are evaluated periodically (e.g., every 5 seconds)
5. Moving average uses a sliding window approach
6. Sustained periods require continuous samples above threshold
7. A single dip below threshold resets the sustained counter
8. Triggers are independent and can fire simultaneously

---

## Security Considerations

1. Trigger signals MUST NOT expose internal metric values externally
2. CPU sampling MUST NOT require elevated privileges
3. Queue metrics MUST NOT include sensitive task content
4. Trigger logs MUST NOT include confidential data
5. Configuration MUST be validated for reasonable thresholds
6. Denial-of-service via trigger manipulation MUST be prevented
7. Trigger events MUST be logged for audit purposes
8. Threshold changes MUST be logged

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Trigger | Condition evaluation |
| Threshold | Value to exceed |
| Sustained | Condition duration |
| Signal | Trigger output |
| Metric | Measured value |
| Sample | Single measurement |

---

## Out of Scope

- External metric sources
- Custom trigger plugins
- Webhook triggers
- Time-based triggers
- Calendar-aware triggers

---

## Functional Requirements

### Trigger Interface

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033A-01 | `IBurstTrigger` interface MUST exist | P0 |
| FR-033A-02 | `Name` property MUST return trigger identifier | P0 |
| FR-033A-03 | `Enabled` property MUST be configurable | P0 |
| FR-033A-04 | `EvaluateAsync` MUST return `TriggerSignal` | P0 |
| FR-033A-05 | Signal MUST include triggered boolean | P0 |
| FR-033A-06 | Signal MUST include confidence (0.0-1.0) | P0 |
| FR-033A-07 | Confidence reflects certainty of trigger | P0 |
| FR-033A-08 | Signal MUST include human-readable reason | P1 |
| FR-033A-09 | Signal MUST include suggested scale | P0 |
| FR-033A-10 | Trigger evaluation MUST be stateless | P0 |
| FR-033A-11 | State stored externally (samples, history) | P1 |
| FR-033A-12 | Trigger MUST accept configuration options | P0 |
| FR-033A-13 | Configuration via strongly-typed options record | P0 |
| FR-033A-14 | Trigger MUST be unit testable | P0 |
| FR-033A-15 | Trigger MUST emit metrics on evaluation | P1 |

### Queue Depth Trigger

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033A-16 | `QueueDepthTrigger` MUST exist | P0 |
| FR-033A-17 | Depth threshold MUST be configurable | P0 |
| FR-033A-18 | Default threshold: 10 pending tasks | P0 |
| FR-033A-19 | Depth > threshold MUST fire trigger | P0 |
| FR-033A-20 | Confidence scales with excess depth | P0 |
| FR-033A-21 | 2x threshold = 1.0 confidence | P0 |
| FR-033A-22 | Suggested scale MUST be calculated | P0 |
| FR-033A-23 | Scale = depth / workers (rounded up) | P0 |
| FR-033A-24 | Minimum scale: 1 instance | P0 |
| FR-033A-25 | Max scale MUST be configurable | P1 |
| FR-033A-26 | Default max scale: 5 instances | P1 |
| FR-033A-27 | Depth MUST be sampled periodically | P1 |
| FR-033A-28 | Default sample interval: 5 seconds | P1 |
| FR-033A-29 | Smoothing MUST be applied | P1 |
| FR-033A-30 | Moving average over 3 samples | P1 |

### Queue Wait Trigger

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033A-31 | `QueueWaitTrigger` MUST exist | P0 |
| FR-033A-32 | Wait threshold MUST be configurable | P0 |
| FR-033A-33 | Default threshold: 5 minutes | P0 |
| FR-033A-34 | Oldest task wait MUST be checked by default | P0 |
| FR-033A-35 | Wait > threshold MUST fire trigger | P0 |
| FR-033A-36 | Average wait MAY be used instead | P2 |
| FR-033A-37 | P95 wait MAY be used instead | P2 |
| FR-033A-38 | Default metric: oldest task wait | P0 |
| FR-033A-39 | Confidence scales with wait time | P0 |
| FR-033A-40 | 2x threshold = 1.0 confidence | P0 |
| FR-033A-41 | Priority MUST weight wait time | P1 |
| FR-033A-42 | P0 task: 2x wait weight | P1 |
| FR-033A-43 | Suggested scale MUST be 1 | P0 |
| FR-033A-44 | Wait trigger adds 1 instance | P0 |
| FR-033A-45 | Reason MUST include actual wait time | P1 |

### CPU Utilization Trigger

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033A-46 | `CpuUtilizationTrigger` MUST exist | P0 |
| FR-033A-47 | CPU threshold MUST be configurable | P0 |
| FR-033A-48 | Default threshold: 80% utilization | P0 |
| FR-033A-49 | CPU MUST be sampled periodically | P0 |
| FR-033A-50 | Default sample interval: 10 seconds | P0 |
| FR-033A-51 | Sustained period MUST be enforced | P0 |
| FR-033A-52 | Default sustained period: 2 minutes | P0 |
| FR-033A-53 | All samples in period above threshold | P0 |
| FR-033A-54 | Single sample below resets sustained | P0 |
| FR-033A-55 | Confidence from CPU utilization | P0 |
| FR-033A-56 | 100% utilization = 1.0 confidence | P0 |
| FR-033A-57 | Per-core utilization MAY be available | P2 |
| FR-033A-58 | Default: aggregate CPU utilization | P0 |
| FR-033A-59 | Suggested scale from CPU headroom | P1 |
| FR-033A-60 | ~20% headroom needed per instance | P1 |

### Worker Saturation Trigger

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033A-61 | `WorkerSaturationTrigger` MUST exist | P0 |
| FR-033A-62 | All workers busy MUST fire trigger | P0 |
| FR-033A-63 | Per-worker queue depth MUST be checked | P0 |
| FR-033A-64 | Queue threshold per worker: 3 tasks | P0 |
| FR-033A-65 | Sustained period MUST be enforced | P0 |
| FR-033A-66 | Default sustained period: 30 seconds | P0 |
| FR-033A-67 | Confidence from saturation level | P0 |
| FR-033A-68 | All workers busy = 1.0 confidence | P0 |
| FR-033A-69 | 90% workers busy = 0.9 confidence | P0 |
| FR-033A-70 | Suggested scale from total queue | P0 |
| FR-033A-71 | Scale = total queued / queue threshold | P0 |
| FR-033A-72 | Single idle worker MUST reset sustained | P0 |
| FR-033A-73 | Idle worker prevents trigger firing | P0 |
| FR-033A-74 | Grace period MUST exist before reset | P1 |
| FR-033A-75 | Default grace period: 10 seconds | P1 |

### Priority Task Trigger

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033A-76 | `PriorityTaskTrigger` MUST exist | P1 |
| FR-033A-77 | P0 task waiting MUST fire trigger | P1 |
| FR-033A-78 | P0 wait threshold: 1 minute | P1 |
| FR-033A-79 | P1 wait threshold: 3 minutes | P1 |
| FR-033A-80 | Lower priority tasks: trigger disabled | P1 |
| FR-033A-81 | P0 task confidence: 1.0 | P1 |
| FR-033A-82 | P1 task confidence: 0.8 | P1 |
| FR-033A-83 | Suggested scale: 1 instance | P1 |
| FR-033A-84 | Priority task needs one dedicated instance | P1 |
| FR-033A-85 | Reason MUST include task priority | P1 |

---

## Non-Functional Requirements

### Performance

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-033A-01 | Single trigger evaluation | <10ms | P0 |
| NFR-033A-02 | Sampling overhead | <1% CPU | P0 |
| NFR-033A-03 | Memory per trigger | <1KB | P1 |
| NFR-033A-04 | Sample storage | <100 samples | P1 |
| NFR-033A-05 | Moving average calculation | O(1) | P1 |
| NFR-033A-06 | Concurrent evaluation | Thread-safe | P0 |
| NFR-033A-07 | Sample pruning | Automatic | P1 |
| NFR-033A-08 | Trigger initialization | <10ms | P1 |
| NFR-033A-09 | Configuration reload | <100ms | P2 |
| NFR-033A-10 | Metric emission | <1ms | P1 |

### Reliability

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-033A-11 | False positive rate | <2% | P0 |
| NFR-033A-12 | Trigger isolation | Failure doesn't cascade | P0 |
| NFR-033A-13 | Missing metric handling | Graceful skip | P0 |
| NFR-033A-14 | Sustained period accuracy | Within 1 sample | P1 |
| NFR-033A-15 | Confidence determinism | Same input = same output | P0 |
| NFR-033A-16 | Scale calculation accuracy | Within 1 instance | P1 |
| NFR-033A-17 | Threshold enforcement | Exact | P0 |
| NFR-033A-18 | Default value fallback | Always works | P0 |
| NFR-033A-19 | Sample overflow prevention | Auto-prune | P1 |
| NFR-033A-20 | Recovery from stuck state | Auto-reset | P1 |

### Observability

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-033A-21 | Structured logging | All evaluations | P0 |
| NFR-033A-22 | Trigger fire metric | Counter per trigger | P1 |
| NFR-033A-23 | Trigger reset metric | Counter | P1 |
| NFR-033A-24 | Confidence distribution | Histogram | P2 |
| NFR-033A-25 | TriggerFiredEvent | Published on fire | P1 |
| NFR-033A-26 | TriggerResetEvent | Published on reset | P2 |
| NFR-033A-27 | Sample history logging | Debug level | P2 |
| NFR-033A-28 | Trace correlation | Request ID | P1 |
| NFR-033A-29 | Threshold logging | On change | P1 |
| NFR-033A-30 | Scale suggestion logging | Info level | P1 |

---

## User Manual Documentation

### Trigger Configuration

```yaml
burst:
  triggers:
    queueDepth:
      enabled: true
      threshold: 10
      maxScale: 5
    queueWait:
      enabled: true
      thresholdMinutes: 5
      metric: oldest  # oldest | average | p95
    cpuUtilization:
      enabled: true
      threshold: 80
      sustainedMinutes: 2
    workerSaturation:
      enabled: true
      queuePerWorker: 3
      sustainedSeconds: 30
    priorityTask:
      enabled: true
      p0ThresholdMinutes: 1
      p1ThresholdMinutes: 3
```

### Trigger Signals

| Trigger | Threshold | Confidence | Scale |
|---------|-----------|------------|-------|
| Queue Depth | depth > 10 | depth/20 | depth/workers |
| Queue Wait | wait > 5m | wait/10m | 1 |
| CPU | cpu > 80% 2m | cpu/100 | headroom |
| Worker Sat | all busy 30s | busy% | queued/3 |
| Priority | P0 wait > 1m | 1.0 | 1 |

### Troubleshooting

| Symptom | Check |
|---------|-------|
| No burst | Verify triggers enabled |
| Too many bursts | Increase thresholds |
| Slow burst | Reduce sustained periods |
| Wrong scale | Adjust max scale |

---

## Acceptance Criteria / Definition of Done

### Trigger Interface
- [ ] AC-001: `IBurstTrigger` interface exists
- [ ] AC-002: `Name` property returns identifier
- [ ] AC-003: `Enabled` property is configurable
- [ ] AC-004: `EvaluateAsync` returns `TriggerSignal`
- [ ] AC-005: Signal includes triggered boolean
- [ ] AC-006: Signal includes confidence 0.0-1.0
- [ ] AC-007: Signal includes reason when triggered
- [ ] AC-008: Signal includes suggested scale
- [ ] AC-009: Triggers registered via DI
- [ ] AC-010: Metrics emitted on evaluation

### Queue Depth Trigger
- [ ] AC-011: Depth > 10 fires trigger
- [ ] AC-012: Threshold is configurable
- [ ] AC-013: Confidence scales: 20 tasks = 1.0
- [ ] AC-014: Scale calculated from depth/workers
- [ ] AC-015: Max scale (5) enforced
- [ ] AC-016: Moving average smoothing works
- [ ] AC-017: Reason includes actual depth

### Queue Wait Trigger
- [ ] AC-018: Wait > 5min fires trigger
- [ ] AC-019: Threshold is configurable
- [ ] AC-020: Oldest task wait used by default
- [ ] AC-021: Confidence scales: 10min = 1.0
- [ ] AC-022: Priority multiplier applied
- [ ] AC-023: P0 task wait weighted 2x
- [ ] AC-024: Scale always 1
- [ ] AC-025: Reason includes wait time

### CPU Utilization Trigger
- [ ] AC-026: CPU > 80% sustained fires
- [ ] AC-027: Threshold is configurable
- [ ] AC-028: Sample interval 10 seconds
- [ ] AC-029: Sustained period 2 minutes
- [ ] AC-030: Single dip resets sustained
- [ ] AC-031: Confidence = CPU utilization
- [ ] AC-032: Scale from headroom calculation
- [ ] AC-033: Reason includes CPU % and duration

### Worker Saturation Trigger
- [ ] AC-034: All workers busy fires
- [ ] AC-035: Per-worker queue threshold 3
- [ ] AC-036: Sustained period 30 seconds
- [ ] AC-037: Confidence = saturation %
- [ ] AC-038: Scale from total queue
- [ ] AC-039: Single idle worker resets
- [ ] AC-040: Grace period 10 seconds

### Priority Task Trigger
- [ ] AC-041: P0 waiting > 1min fires
- [ ] AC-042: P1 waiting > 3min fires
- [ ] AC-043: P0 confidence = 1.0
- [ ] AC-044: P1 confidence = 0.8
- [ ] AC-045: Scale always 1
- [ ] AC-046: Reason includes priority level

### Observability
- [ ] AC-047: TriggerFiredEvent published
- [ ] AC-048: TriggerResetEvent published
- [ ] AC-049: Evaluation logged
- [ ] AC-050: Metrics by trigger type
- [ ] AC-051: Confidence histogram available
- [ ] AC-052: Configuration logged on startup

---

## User Verification Scenarios

### Scenario 1: Queue Depth Trigger
**Persona:** Developer with large batch  
**Preconditions:** Queue empty, 15 tasks submitted  
**Steps:**
1. Submit 15 tasks to queue
2. Observe trigger evaluation
3. Check trigger fired
4. Verify scale calculation

**Verification Checklist:**
- [ ] Depth = 15 > threshold 10
- [ ] Trigger fired = true
- [ ] Confidence = 15/20 = 0.75
- [ ] Scale = 15/workers, max 5

### Scenario 2: CPU Sustained High
**Persona:** Developer with CPU workload  
**Preconditions:** CPU idle, then 90% for 3 min  
**Steps:**
1. Start CPU-intensive work
2. Monitor samples over 3 minutes
3. Check sustained period reached
4. Verify trigger fires

**Verification Checklist:**
- [ ] Samples recorded every 10s
- [ ] All samples > 80%
- [ ] After 2min, trigger fires
- [ ] Reason: "CPU 90% for 2 minutes"

### Scenario 3: Single Dip Resets Sustained
**Persona:** Developer with variable load  
**Preconditions:** CPU at 85% for 90 seconds  
**Steps:**
1. CPU high for 90 seconds
2. Brief dip to 70% (one sample)
3. CPU returns to 85%
4. Sustained counter reset

**Verification Checklist:**
- [ ] 90 seconds accumulated
- [ ] Dip to 70% detected
- [ ] Sustained counter reset to 0
- [ ] Trigger not fired

### Scenario 4: Worker Saturation
**Persona:** Developer with busy workers  
**Preconditions:** All 4 workers have 5+ queued tasks  
**Steps:**
1. All workers processing
2. Each has 5 queued tasks
3. Sustained 30 seconds
4. Trigger fires

**Verification Checklist:**
- [ ] All workers busy detected
- [ ] Queue per worker > 3
- [ ] 30 second sustained met
- [ ] Scale = 20/3 ≈ 7, capped

### Scenario 5: Priority Task Fast Track
**Persona:** Developer with urgent P0 task  
**Preconditions:** P0 task queued, waiting 2 minutes  
**Steps:**
1. Submit P0 task
2. Wait 2 minutes (> 1 min threshold)
3. Trigger fires
4. Confidence = 1.0

**Verification Checklist:**
- [ ] P0 task identified
- [ ] Wait > 1 min threshold
- [ ] Trigger fires with confidence 1.0
- [ ] Scale = 1

### Scenario 6: Trigger Configuration Change
**Persona:** Operations adjusting thresholds  
**Preconditions:** Default thresholds active  
**Steps:**
1. Increase queue depth threshold to 20
2. Queue 15 tasks
3. Trigger does NOT fire
4. Queue 25 tasks, trigger fires

**Verification Checklist:**
- [ ] New threshold 20 applied
- [ ] 15 tasks: no trigger
- [ ] 25 tasks: trigger fires
- [ ] Config change logged

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-033A-01 | Queue depth > threshold fires | FR-033A-19 |
| UT-033A-02 | Queue depth confidence scaling | FR-033A-20-21 |
| UT-033A-03 | Queue depth scale calculation | FR-033A-22-24 |
| UT-033A-04 | Queue depth max scale enforcement | FR-033A-26 |
| UT-033A-05 | Queue wait > threshold fires | FR-033A-35 |
| UT-033A-06 | Queue wait priority weighting | FR-033A-41-42 |
| UT-033A-07 | CPU sustained period tracking | FR-033A-51-53 |
| UT-033A-08 | CPU single dip resets | FR-033A-54 |
| UT-033A-09 | Worker saturation detection | FR-033A-62 |
| UT-033A-10 | Worker idle resets sustained | FR-033A-72 |
| UT-033A-11 | Priority P0 threshold | FR-033A-78 |
| UT-033A-12 | Priority P1 threshold | FR-033A-79 |
| UT-033A-13 | Moving average smoothing | FR-033A-30 |
| UT-033A-14 | Disabled trigger returns false | FR-033A-03 |
| UT-033A-15 | Confidence determinism | NFR-033A-15 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-033A-01 | Real CPU sampling | E2E |
| IT-033A-02 | Worker pool integration | FR-033A-63 |
| IT-033A-03 | Queue metrics integration | FR-033A-16 |
| IT-033A-04 | Priority task detection | FR-033A-77 |
| IT-033A-05 | Multiple triggers simultaneously | Multiple |
| IT-033A-06 | Sustained period over time | FR-033A-51 |
| IT-033A-07 | Configuration reload | NFR-033A-09 |
| IT-033A-08 | Event publishing | NFR-033A-25 |
| IT-033A-09 | Metrics emission | NFR-033A-22 |
| IT-033A-10 | Performance <10ms | NFR-033A-01 |
| IT-033A-11 | Thread safety | NFR-033A-06 |
| IT-033A-12 | Grace period behavior | FR-033A-75 |
| IT-033A-13 | Sample pruning | NFR-033A-19 |
| IT-033A-14 | False positive testing | NFR-033A-11 |
| IT-033A-15 | Recovery from stuck state | NFR-033A-20 |

---

## Implementation Prompt

### Part 1: File Structure + Domain Models

```
src/
├── Acode.Domain/
│   └── Compute/
│       └── Burst/
│           └── Triggers/
│               └── Events/
│                   ├── TriggerFiredEvent.cs
│                   └── TriggerResetEvent.cs
├── Acode.Application/
│   └── Compute/
│       └── Burst/
│           └── Triggers/
│               ├── IBurstTrigger.cs
│               ├── TriggerContext.cs
│               ├── TriggerSignal.cs
│               ├── QueueDepthOptions.cs
│               ├── QueueWaitOptions.cs
│               ├── CpuOptions.cs
│               ├── SaturationOptions.cs
│               └── PriorityOptions.cs
└── Acode.Infrastructure/
    └── Compute/
        └── Burst/
            └── Triggers/
                ├── QueueDepthTrigger.cs
                ├── QueueWaitTrigger.cs
                ├── CpuUtilizationTrigger.cs
                ├── WorkerSaturationTrigger.cs
                ├── PriorityTaskTrigger.cs
                └── Sampling/
                    ├── CpuSampler.cs
                    └── MovingAverageCalculator.cs
```

```csharp
// src/Acode.Domain/Compute/Burst/Triggers/Events/TriggerFiredEvent.cs
namespace Acode.Domain.Compute.Burst.Triggers.Events;

public sealed record TriggerFiredEvent(
    string TriggerName,
    double Confidence,
    string Reason,
    int SuggestedScale,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Burst/Triggers/Events/TriggerResetEvent.cs
namespace Acode.Domain.Compute.Burst.Triggers.Events;

public sealed record TriggerResetEvent(
    string TriggerName,
    string ResetReason,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 033.a Specification - Part 1/3**

### Part 2: Application Interfaces + Options

```csharp
// src/Acode.Application/Compute/Burst/Triggers/TriggerContext.cs
namespace Acode.Application.Compute.Burst.Triggers;

public sealed record TriggerContext
{
    public int QueueDepth { get; init; }
    public TimeSpan OldestTaskWait { get; init; }
    public TimeSpan AverageTaskWait { get; init; }
    public TimeSpan P95TaskWait { get; init; }
    public IReadOnlyList<QueuedTask> QueuedTasks { get; init; } = [];
    public double CpuUtilization { get; init; }
    public double MemoryUtilization { get; init; }
    public int ActiveWorkers { get; init; }
    public int TotalWorkers { get; init; }
    public IReadOnlyList<WorkerStatus> WorkerStatuses { get; init; } = [];
}

// src/Acode.Application/Compute/Burst/Triggers/TriggerSignal.cs
namespace Acode.Application.Compute.Burst.Triggers;

public sealed record TriggerSignal
{
    public bool Triggered { get; init; }
    public double Confidence { get; init; }
    public string? Reason { get; init; }
    public int SuggestedScale { get; init; } = 1;
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}

// src/Acode.Application/Compute/Burst/Triggers/IBurstTrigger.cs
namespace Acode.Application.Compute.Burst.Triggers;

public interface IBurstTrigger
{
    string Name { get; }
    bool Enabled { get; }
    
    Task<TriggerSignal> EvaluateAsync(
        TriggerContext context,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Burst/Triggers/QueueDepthOptions.cs
namespace Acode.Application.Compute.Burst.Triggers;

public sealed record QueueDepthOptions
{
    public bool Enabled { get; init; } = true;
    public int Threshold { get; init; } = 10;
    public int MaxScale { get; init; } = 5;
    public int SampleCount { get; init; } = 3;
}

// src/Acode.Application/Compute/Burst/Triggers/QueueWaitOptions.cs
namespace Acode.Application.Compute.Burst.Triggers;

public sealed record QueueWaitOptions
{
    public bool Enabled { get; init; } = true;
    public TimeSpan Threshold { get; init; } = TimeSpan.FromMinutes(5);
    public string Metric { get; init; } = "oldest"; // oldest | average | p95
    public double PriorityMultiplier { get; init; } = 2.0;
}

// src/Acode.Application/Compute/Burst/Triggers/CpuOptions.cs
namespace Acode.Application.Compute.Burst.Triggers;

public sealed record CpuOptions
{
    public bool Enabled { get; init; } = true;
    public double Threshold { get; init; } = 0.80;
    public TimeSpan SustainedPeriod { get; init; } = TimeSpan.FromMinutes(2);
    public TimeSpan SampleInterval { get; init; } = TimeSpan.FromSeconds(10);
}

// src/Acode.Application/Compute/Burst/Triggers/SaturationOptions.cs
namespace Acode.Application.Compute.Burst.Triggers;

public sealed record SaturationOptions
{
    public bool Enabled { get; init; } = true;
    public int QueuePerWorker { get; init; } = 3;
    public TimeSpan SustainedPeriod { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan GracePeriod { get; init; } = TimeSpan.FromSeconds(10);
}

// src/Acode.Application/Compute/Burst/Triggers/PriorityOptions.cs
namespace Acode.Application.Compute.Burst.Triggers;

public sealed record PriorityOptions
{
    public bool Enabled { get; init; } = true;
    public TimeSpan P0Threshold { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan P1Threshold { get; init; } = TimeSpan.FromMinutes(3);
}
```

**End of Task 033.a Specification - Part 2/3**

### Part 3: Infrastructure Implementation + Checklist

```csharp
// src/Acode.Infrastructure/Compute/Burst/Triggers/QueueDepthTrigger.cs
namespace Acode.Infrastructure.Compute.Burst.Triggers;

public sealed class QueueDepthTrigger : IBurstTrigger
{
    private readonly QueueDepthOptions _options;
    private readonly MovingAverageCalculator _smoother;
    
    public string Name => "queue-depth";
    public bool Enabled => _options.Enabled;
    
    public QueueDepthTrigger(QueueDepthOptions options)
    {
        _options = options;
        _smoother = new MovingAverageCalculator(_options.SampleCount);
    }
    
    public Task<TriggerSignal> EvaluateAsync(TriggerContext context, CancellationToken ct)
    {
        var smoothedDepth = _smoother.Add(context.QueueDepth);
        var triggered = smoothedDepth > _options.Threshold;
        var confidence = Math.Min(smoothedDepth / (_options.Threshold * 2.0), 1.0);
        var scale = Math.Min((int)Math.Ceiling(smoothedDepth / context.TotalWorkers), _options.MaxScale);
        
        return Task.FromResult(new TriggerSignal
        {
            Triggered = triggered,
            Confidence = confidence,
            Reason = triggered ? $"Queue depth {smoothedDepth:F0} > threshold {_options.Threshold}" : null,
            SuggestedScale = scale,
            Metadata = new Dictionary<string, object> { ["smoothedDepth"] = smoothedDepth }
        });
    }
}

// src/Acode.Infrastructure/Compute/Burst/Triggers/CpuUtilizationTrigger.cs
namespace Acode.Infrastructure.Compute.Burst.Triggers;

public sealed class CpuUtilizationTrigger : IBurstTrigger
{
    private readonly CpuOptions _options;
    private readonly Queue<(DateTimeOffset Time, double Value)> _samples = new();
    
    public string Name => "cpu-utilization";
    public bool Enabled => _options.Enabled;
    
    public Task<TriggerSignal> EvaluateAsync(TriggerContext context, CancellationToken ct)
    {
        _samples.Enqueue((DateTimeOffset.UtcNow, context.CpuUtilization));
        PruneSamples();
        
        var sustained = IsSustainedAboveThreshold();
        return Task.FromResult(new TriggerSignal
        {
            Triggered = sustained,
            Confidence = sustained ? context.CpuUtilization : 0.0,
            Reason = sustained 
                ? $"CPU at {context.CpuUtilization:P0} for {_options.SustainedPeriod.TotalMinutes} minutes" 
                : null,
            SuggestedScale = sustained ? (int)Math.Ceiling((context.CpuUtilization - 0.6) / 0.2) : 0
        });
    }
    
    private bool IsSustainedAboveThreshold()
    {
        if (_samples.Count < 2) return false;
        var window = DateTimeOffset.UtcNow - _options.SustainedPeriod;
        return _samples.Where(s => s.Time >= window).All(s => s.Value >= _options.Threshold);
    }
    
    private void PruneSamples()
    {
        var cutoff = DateTimeOffset.UtcNow - _options.SustainedPeriod - TimeSpan.FromMinutes(1);
        while (_samples.Count > 0 && _samples.Peek().Time < cutoff)
            _samples.Dequeue();
    }
}

// src/Acode.Infrastructure/Compute/Burst/Triggers/PriorityTaskTrigger.cs
namespace Acode.Infrastructure.Compute.Burst.Triggers;

public sealed class PriorityTaskTrigger : IBurstTrigger
{
    private readonly PriorityOptions _options;
    
    public string Name => "priority-task";
    public bool Enabled => _options.Enabled;
    
    public Task<TriggerSignal> EvaluateAsync(TriggerContext context, CancellationToken ct)
    {
        var p0Tasks = context.QueuedTasks.Where(t => t.Priority == 0).ToList();
        var p1Tasks = context.QueuedTasks.Where(t => t.Priority == 1).ToList();
        
        var p0Waiting = p0Tasks.Any(t => t.WaitTime >= _options.P0Threshold);
        var p1Waiting = p1Tasks.Any(t => t.WaitTime >= _options.P1Threshold);
        
        return Task.FromResult(new TriggerSignal
        {
            Triggered = p0Waiting || p1Waiting,
            Confidence = p0Waiting ? 1.0 : (p1Waiting ? 0.8 : 0.0),
            Reason = p0Waiting 
                ? $"P0 task waiting {p0Tasks.Max(t => t.WaitTime).TotalMinutes:F1} minutes"
                : (p1Waiting ? $"P1 task waiting {p1Tasks.Max(t => t.WaitTime).TotalMinutes:F1} minutes" : null),
            SuggestedScale = 1
        });
    }
}
```

### Implementation Checklist

| Step | Action | Verification |
|------|--------|--------------|
| 1 | Create trigger events | Event serialization verified |
| 2 | Define TriggerContext, TriggerSignal | Records compile |
| 3 | Define all options records | Options compile |
| 4 | Create IBurstTrigger interface | Interface contract clear |
| 5 | Implement MovingAverageCalculator | Smoothing verified |
| 6 | Implement QueueDepthTrigger | Threshold fires correctly |
| 7 | Implement QueueWaitTrigger | Wait time calculated |
| 8 | Implement CpuUtilizationTrigger | Sustained period works |
| 9 | Implement WorkerSaturationTrigger | All-busy detection works |
| 10 | Implement PriorityTaskTrigger | P0/P1 thresholds work |
| 11 | Add confidence scoring | Confidence 0.0-1.0 |
| 12 | Add scale calculation | Scale within limits |
| 13 | Register triggers in DI | All triggers resolved |
| 14 | Performance verify <10ms | Benchmark passes |

### Rollout Plan

1. **Phase 1**: Implement trigger interface and sampling utilities
2. **Phase 2**: Add queue-based triggers (depth, wait)
3. **Phase 3**: Add load-based triggers (CPU, memory)
4. **Phase 4**: Add WorkerSaturationTrigger
5. **Phase 5**: Add PriorityTaskTrigger and integration tests

**End of Task 033.a Specification**