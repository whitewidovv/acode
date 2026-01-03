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

- Task 033: Triggers feed heuristics
- Task 026: Queue provides metrics
- Task 027: Workers provide load

### Failure Modes

- Metric unavailable → Skip trigger
- Threshold undefined → Use default
- Trigger error → Log and skip
- All triggers fail → No burst

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

### FR-001 to FR-015: Trigger Interface

- FR-001: `IBurstTrigger` MUST exist
- FR-002: `Name` property MUST exist
- FR-003: `Enabled` property MUST exist
- FR-004: `EvaluateAsync` MUST return signal
- FR-005: Signal MUST include triggered flag
- FR-006: Signal MUST include confidence
- FR-007: Confidence: 0.0 to 1.0
- FR-008: Signal MUST include reason
- FR-009: Signal MUST include suggested scale
- FR-010: Trigger MUST be stateless
- FR-011: State in external stores
- FR-012: Trigger MUST be configurable
- FR-013: Configuration via options
- FR-014: Trigger MUST be testable
- FR-015: Trigger MUST emit metrics

### FR-016 to FR-030: Queue Depth Trigger

- FR-016: `QueueDepthTrigger` MUST exist
- FR-017: Threshold MUST be configurable
- FR-018: Default threshold: 10
- FR-019: Depth > threshold MUST fire
- FR-020: Confidence scales with excess
- FR-021: 2x threshold = 1.0 confidence
- FR-022: Suggested scale MUST calculate
- FR-023: Scale = depth / workers
- FR-024: Min scale: 1
- FR-025: Max scale MUST be configurable
- FR-026: Default max: 5
- FR-027: Depth MUST be sampled
- FR-028: Sample interval: 5 seconds
- FR-029: Smoothing MUST be applied
- FR-030: Moving average over 3 samples

### FR-031 to FR-045: Queue Wait Trigger

- FR-031: `QueueWaitTrigger` MUST exist
- FR-032: Threshold MUST be configurable
- FR-033: Default threshold: 5 minutes
- FR-034: Oldest task wait MUST be checked
- FR-035: Wait > threshold MUST fire
- FR-036: Average wait MUST be optional
- FR-037: P95 wait MUST be optional
- FR-038: Default: oldest task
- FR-039: Confidence scales with wait
- FR-040: 2x threshold = 1.0 confidence
- FR-041: Priority MUST weight wait
- FR-042: P0 task: 2x weight
- FR-043: Suggested scale MUST be 1
- FR-044: Wait trigger adds 1 instance
- FR-045: Reason MUST include wait time

### FR-046 to FR-060: CPU Utilization Trigger

- FR-046: `CpuUtilizationTrigger` MUST exist
- FR-047: Threshold MUST be configurable
- FR-048: Default threshold: 80%
- FR-049: CPU MUST be sampled
- FR-050: Sample interval: 10 seconds
- FR-051: Sustained period MUST apply
- FR-052: Default sustained: 2 minutes
- FR-053: All samples above threshold
- FR-054: Single dip resets sustained
- FR-055: Confidence from utilization
- FR-056: 100% = 1.0 confidence
- FR-057: Per-core MUST be available
- FR-058: Default: aggregate
- FR-059: Suggested scale from headroom
- FR-060: Need 20% headroom per instance

### FR-061 to FR-075: Worker Saturation Trigger

- FR-061: `WorkerSaturationTrigger` MUST exist
- FR-062: All workers busy MUST fire
- FR-063: Per-worker queue MUST be checked
- FR-064: Queue threshold per worker: 3
- FR-065: Sustained period MUST apply
- FR-066: Default sustained: 30 seconds
- FR-067: Confidence from saturation level
- FR-068: All busy = 1.0
- FR-069: 90% busy = 0.9
- FR-070: Suggested scale from queue
- FR-071: Total queued / queue threshold
- FR-072: Idle worker MUST reset
- FR-073: Single idle prevents trigger
- FR-074: Grace period MUST exist
- FR-075: Default grace: 10 seconds

### FR-076 to FR-085: Priority Trigger

- FR-076: `PriorityTaskTrigger` MUST exist
- FR-077: P0 task waiting MUST fire
- FR-078: P0 wait threshold: 1 minute
- FR-079: P1 wait threshold: 3 minutes
- FR-080: Lower priority: disabled
- FR-081: Confidence: 1.0 for P0
- FR-082: Confidence: 0.8 for P1
- FR-083: Suggested scale: 1
- FR-084: Priority task needs one instance
- FR-085: Reason MUST include priority

---

## Non-Functional Requirements

- NFR-001: Trigger evaluation <10ms
- NFR-002: Sampling overhead <1%
- NFR-003: Memory efficient
- NFR-004: No false positives in tests
- NFR-005: Clear trigger reasons
- NFR-006: Structured logging
- NFR-007: Metrics per trigger
- NFR-008: Configurable thresholds
- NFR-009: Thread-safe
- NFR-010: Deterministic

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

- [ ] AC-001: Queue depth trigger works
- [ ] AC-002: Queue wait trigger works
- [ ] AC-003: CPU trigger works
- [ ] AC-004: Worker saturation works
- [ ] AC-005: Priority trigger works
- [ ] AC-006: Sustained periods work
- [ ] AC-007: Confidence calculates
- [ ] AC-008: Scale calculates
- [ ] AC-009: Configuration works
- [ ] AC-010: Metrics emit

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Queue depth logic
- [ ] UT-002: Wait calculation
- [ ] UT-003: Sustained period
- [ ] UT-004: Confidence scoring

### Integration Tests

- [ ] IT-001: Real CPU sampling
- [ ] IT-002: Worker metrics
- [ ] IT-003: Queue integration
- [ ] IT-004: Priority handling

---

## Implementation Prompt

### Interface

```csharp
public interface IBurstTrigger
{
    string Name { get; }
    bool Enabled { get; }
    
    Task<TriggerSignal> EvaluateAsync(
        TriggerContext context,
        CancellationToken ct);
}

public record TriggerContext(
    int QueueDepth,
    TimeSpan OldestTaskWait,
    TimeSpan AverageTaskWait,
    IReadOnlyList<QueuedTask> QueuedTasks,
    double CpuUtilization,
    double MemoryUtilization,
    int ActiveWorkers,
    int TotalWorkers,
    IReadOnlyList<WorkerStatus> WorkerStatuses);

public record TriggerSignal(
    bool Triggered,
    double Confidence,
    string Reason,
    int SuggestedScale,
    IReadOnlyDictionary<string, object> Metadata);
```

### Implementations

```csharp
public class QueueDepthTrigger : IBurstTrigger
{
    public QueueDepthTrigger(QueueDepthOptions options);
}

public class QueueWaitTrigger : IBurstTrigger
{
    public QueueWaitTrigger(QueueWaitOptions options);
}

public class CpuUtilizationTrigger : IBurstTrigger
{
    public CpuUtilizationTrigger(CpuOptions options);
}

public class WorkerSaturationTrigger : IBurstTrigger
{
    public WorkerSaturationTrigger(SaturationOptions options);
}

public class PriorityTaskTrigger : IBurstTrigger
{
    public PriorityTaskTrigger(PriorityOptions options);
}
```

---

**End of Task 033.a Specification**