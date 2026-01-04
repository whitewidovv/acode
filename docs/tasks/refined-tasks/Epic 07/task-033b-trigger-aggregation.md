# Task 033.b: Trigger Aggregation

**Priority:** P1 – High  
**Tier:** L – Cloud Integration  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 033 (Burst Heuristics), Task 033.a (Triggers)  

---

## Description

Task 033.b implements trigger aggregation. Multiple trigger signals MUST be combined into a single burst decision. Aggregation MUST support different strategies.

Aggregation determines how multiple signals combine. OR logic bursts if any trigger fires. AND logic requires all triggers. Weighted voting is also supported.

This task covers aggregation logic. Individual triggers are in 033.a. Rate limiting is in 033.c.

### Business Value

Aggregation provides:
- Flexible decision making
- Reduced false positives
- Configurable sensitivity
- Predictable behavior

### Scope Boundaries

This task covers signal combination. Trigger evaluation is in 033.a. Cooldown is in 033.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Task 033 Burst Engine | ITriggerAggregator | Signals → Decision | Aggregation within engine |
| Task 033.a Triggers | TriggerSignal[] | Signals → Aggregator | Input from all triggers |
| Task 033.c Rate Limiter | AggregatedDecision | Decision → Limiter | Output to cooldown check |
| Configuration | AggregationOptions | Config → Aggregator | Strategy and weights |
| Aggregator Registry | IAggregatorRegistry | Strategy → Aggregator | Lookup by strategy name |
| Metrics System | IMetrics | Stats → Metrics | Aggregation telemetry |
| Event System | IEventPublisher | Events → Subscribers | Completion events |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| No signals | Empty list | Return no burst | Conservative behavior |
| All triggers disabled | No enabled signals | Return no burst | Conservative behavior |
| Aggregator exception | Catch in engine | Default to no burst | Safe fallback |
| Mixed signals | Ambiguous result | Apply strategy strictly | Expected behavior |
| Invalid quorum | Quorum > count | Warn and cap at count | Degraded accuracy |
| Zero total weight | All weights 0 | Return no burst | Conservative behavior |
| Unknown strategy | Lookup fails | Use default (OR) | Fallback behavior |
| Concurrent aggregation | Race condition | Thread-safe design | No issue |

---

## Assumptions

1. All trigger signals are available before aggregation begins
2. Trigger signals include confidence scores between 0.0 and 1.0
3. Trigger signals include suggested scale values
4. Aggregation strategies are mutually exclusive (only one active)
5. Weights can be configured per-trigger in agent-config.yml
6. Quorum can be specified as absolute count or percentage
7. Percentage quorum rounds up (50% of 3 = 2)
8. Aggregation is synchronous and fast (<5ms)

---

## Security Considerations

1. Aggregated decisions MUST NOT expose internal scoring details externally
2. Weight configuration MUST be validated for reasonable ranges
3. Strategy selection MUST NOT allow injection attacks
4. Aggregation logs MUST NOT include sensitive trigger data
5. Custom aggregators MUST be sandboxed
6. Decision events MUST be logged for audit
7. Denial-of-service via weight manipulation MUST be prevented
8. Registry access MUST be thread-safe

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Aggregation | Combining signals |
| OR Logic | Any trigger fires |
| AND Logic | All triggers fire |
| Weighted | Signals have weights |
| Quorum | Minimum triggers |
| Consensus | Agreement level |

---

## Out of Scope

- Machine learning aggregation
- Historical pattern matching
- External voting systems
- Distributed consensus
- Multi-region aggregation

---

## Functional Requirements

### Aggregator Interface

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033B-01 | `ITriggerAggregator` interface MUST exist | P0 |
| FR-033B-02 | `Aggregate` MUST return `AggregatedDecision` | P0 |
| FR-033B-03 | Input: list of `TriggerSignal` from all triggers | P0 |
| FR-033B-04 | Output: combined decision with burst flag | P0 |
| FR-033B-05 | Decision MUST include shouldBurst boolean | P0 |
| FR-033B-06 | Decision MUST include aggregated confidence | P0 |
| FR-033B-07 | Decision MUST include suggested scale | P0 |
| FR-033B-08 | Decision MUST include combined reasons list | P0 |
| FR-033B-09 | Aggregator MUST accept configuration options | P0 |
| FR-033B-10 | Strategy MUST be selectable at runtime | P1 |
| FR-033B-11 | Default strategy: OR (any trigger fires) | P0 |
| FR-033B-12 | Aggregator MUST be pluggable via DI | P1 |
| FR-033B-13 | Custom aggregators MUST be registerable | P2 |
| FR-033B-14 | Aggregator MUST log aggregation decisions | P1 |
| FR-033B-15 | Aggregator MUST emit completion metrics | P1 |

### OR Aggregation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033B-16 | `OrAggregator` MUST exist | P0 |
| FR-033B-17 | Any single trigger MUST cause burst | P0 |
| FR-033B-18 | One trigger fired = shouldBurst true | P0 |
| FR-033B-19 | Confidence: maximum of all fired | P0 |
| FR-033B-20 | Scale: maximum of all fired | P0 |
| FR-033B-21 | Reasons: all triggered reasons included | P0 |
| FR-033B-22 | No triggers fired = no burst | P0 |
| FR-033B-23 | All triggers disabled = no burst | P0 |
| FR-033B-24 | Confidence threshold MAY filter weak signals | P1 |
| FR-033B-25 | Signals below threshold MUST be skipped | P1 |
| FR-033B-26 | Default threshold: 0.0 (no filtering) | P1 |
| FR-033B-27 | Below threshold signals still counted | P1 |
| FR-033B-28 | OR MAY short-circuit on high confidence | P2 |
| FR-033B-29 | Stop evaluation on first >= 0.9 confidence | P2 |
| FR-033B-30 | High confidence: configurable threshold | P2 |

### AND Aggregation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033B-31 | `AndAggregator` MUST exist | P0 |
| FR-033B-32 | All triggers MUST fire for burst | P0 |
| FR-033B-33 | Any trigger not fired = no burst | P0 |
| FR-033B-34 | Confidence: minimum of all fired | P0 |
| FR-033B-35 | Scale: average of all fired (rounded) | P0 |
| FR-033B-36 | Reasons: all reasons combined | P0 |
| FR-033B-37 | No triggers = no burst | P0 |
| FR-033B-38 | Disabled triggers MUST be excluded | P0 |
| FR-033B-39 | Only enabled triggers count for "all" | P0 |
| FR-033B-40 | Partial AND MAY be supported | P1 |
| FR-033B-41 | Require N of M triggers (configurable) | P1 |
| FR-033B-42 | Default: all enabled required | P0 |
| FR-033B-43 | N MUST be configurable | P1 |
| FR-033B-44 | Example: 3 of 5 triggers required | P1 |
| FR-033B-45 | AND MUST evaluate all triggers (no short-circuit) | P0 |

### Weighted Aggregation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033B-46 | `WeightedAggregator` MUST exist | P0 |
| FR-033B-47 | Each trigger MUST have configurable weight | P0 |
| FR-033B-48 | Weight range: 0.0 to 1.0 | P0 |
| FR-033B-49 | Default weight: 1.0 (equal weighting) | P0 |
| FR-033B-50 | Score = sum(confidence × weight) / sum(weights) | P0 |
| FR-033B-51 | Normalized score between 0.0 and 1.0 | P0 |
| FR-033B-52 | Threshold MUST be configurable | P0 |
| FR-033B-53 | Default threshold: 0.5 | P0 |
| FR-033B-54 | Score >= threshold = shouldBurst true | P0 |
| FR-033B-55 | Confidence = normalized score | P0 |
| FR-033B-56 | Scale = weighted average of suggested scales | P1 |
| FR-033B-57 | Weights MUST be configurable per-trigger | P0 |
| FR-033B-58 | Configuration in agent-config.yml | P0 |
| FR-033B-59 | Disabled triggers MUST have weight 0 | P0 |
| FR-033B-60 | Zero total weight = no burst | P0 |

### Quorum Aggregation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-033B-61 | `QuorumAggregator` MUST exist | P1 |
| FR-033B-62 | Quorum count MUST be configurable | P1 |
| FR-033B-63 | Default quorum: 2 triggers | P1 |
| FR-033B-64 | At least N triggers MUST fire for burst | P1 |
| FR-033B-65 | Fired count >= quorum = shouldBurst true | P1 |
| FR-033B-66 | Confidence: average of fired triggers | P1 |
| FR-033B-67 | Scale: maximum of fired triggers | P1 |
| FR-033B-68 | Reasons: only fired triggers included | P1 |
| FR-033B-69 | Quorum MUST be validated against count | P1 |
| FR-033B-70 | Quorum cannot exceed enabled trigger count | P1 |
| FR-033B-71 | Invalid quorum MUST log warning | P1 |
| FR-033B-72 | Fallback: cap quorum at available count | P1 |
| FR-033B-73 | Percentage quorum MUST be supported | P1 |
| FR-033B-74 | Example: 50% of triggers = burst | P1 |
| FR-033B-75 | Percentage rounds up (50% of 3 = 2) | P1 |

---

## Non-Functional Requirements

### Performance

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-033B-01 | Aggregation time | <5ms | P0 |
| NFR-033B-02 | Memory per aggregation | <1KB | P1 |
| NFR-033B-03 | Signal iteration | O(n) | P0 |
| NFR-033B-04 | Concurrent aggregation | Thread-safe | P0 |
| NFR-033B-05 | Registry lookup | O(1) | P0 |
| NFR-033B-06 | Weight calculation | O(n) | P0 |
| NFR-033B-07 | Short-circuit optimization | Optional | P2 |
| NFR-033B-08 | Strategy initialization | <10ms | P1 |
| NFR-033B-09 | Config parsing | <5ms | P1 |
| NFR-033B-10 | Metric emission | <1ms | P1 |

### Reliability

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-033B-11 | Deterministic results | Same input = same output | P0 |
| NFR-033B-12 | No side effects | Pure function | P0 |
| NFR-033B-13 | Exception safety | Catch and handle | P0 |
| NFR-033B-14 | Empty signal handling | Return no burst | P0 |
| NFR-033B-15 | Invalid quorum handling | Cap at max | P1 |
| NFR-033B-16 | Zero weight handling | Return no burst | P0 |
| NFR-033B-17 | Null signal filtering | Skip nulls | P0 |
| NFR-033B-18 | Strategy not found | Use default | P0 |
| NFR-033B-19 | Concurrent registration | Thread-safe | P1 |
| NFR-033B-20 | Recovery from errors | Log and continue | P0 |

### Observability

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-033B-21 | Structured logging | All aggregations | P0 |
| NFR-033B-22 | Strategy usage metric | Counter by strategy | P1 |
| NFR-033B-23 | Triggers fired metric | Counter | P1 |
| NFR-033B-24 | Confidence distribution | Histogram | P2 |
| NFR-033B-25 | AggregationCompletedEvent | Published | P1 |
| NFR-033B-26 | Decision trace | Debug level | P2 |
| NFR-033B-27 | Weight configuration logging | On startup | P1 |
| NFR-033B-28 | Trace correlation | Request ID | P1 |
| NFR-033B-29 | Strategy selection logging | Info level | P1 |
| NFR-033B-30 | Performance trace | Duration logged | P1 |

---

## User Manual Documentation

### Aggregation Strategies

| Strategy | Behavior | Use Case |
|----------|----------|----------|
| or | Any trigger fires | Sensitive |
| and | All triggers fire | Conservative |
| weighted | Score threshold | Balanced |
| quorum | N of M triggers | Flexible |

### Configuration

```yaml
burst:
  aggregation:
    strategy: weighted  # or | and | weighted | quorum
    confidenceThreshold: 0.5
    quorum: 2
    weights:
      queueDepth: 1.0
      queueWait: 0.8
      cpuUtilization: 0.6
      workerSaturation: 0.9
      priorityTask: 1.0
```

### Decision Example

```json
{
  "shouldBurst": true,
  "confidence": 0.75,
  "suggestedScale": 3,
  "strategy": "weighted",
  "triggersEvaluated": 5,
  "triggersFired": 3,
  "reasons": [
    "Queue depth 15 (weight 1.0, confidence 0.75)",
    "Worker saturation 100% (weight 0.9, confidence 1.0)",
    "Priority P0 waiting 2m (weight 1.0, confidence 1.0)"
  ]
}
```

---

## Acceptance Criteria / Definition of Done

### Aggregator Interface
- [ ] AC-001: `ITriggerAggregator` interface exists
- [ ] AC-002: `Aggregate` returns `AggregatedDecision`
- [ ] AC-003: Decision includes shouldBurst boolean
- [ ] AC-004: Decision includes confidence 0.0-1.0
- [ ] AC-005: Decision includes suggested scale
- [ ] AC-006: Decision includes combined reasons
- [ ] AC-007: Registry resolves aggregators by strategy
- [ ] AC-008: Default strategy is OR

### OR Aggregation
- [ ] AC-009: One trigger fired = burst
- [ ] AC-010: Multiple triggers fired = burst
- [ ] AC-011: No triggers fired = no burst
- [ ] AC-012: Confidence = max of fired
- [ ] AC-013: Scale = max of fired
- [ ] AC-014: All fired reasons collected
- [ ] AC-015: Confidence threshold filtering works

### AND Aggregation
- [ ] AC-016: All triggers fired = burst
- [ ] AC-017: Any trigger not fired = no burst
- [ ] AC-018: Disabled triggers excluded
- [ ] AC-019: Confidence = min of all
- [ ] AC-020: Scale = average (rounded)
- [ ] AC-021: All reasons combined
- [ ] AC-022: Partial AND (N of M) works

### Weighted Aggregation
- [ ] AC-023: Weights per trigger applied
- [ ] AC-024: Default weight = 1.0
- [ ] AC-025: Score normalized 0.0-1.0
- [ ] AC-026: Threshold 0.5 default
- [ ] AC-027: Above threshold = burst
- [ ] AC-028: Disabled triggers weight 0
- [ ] AC-029: Zero total weight = no burst
- [ ] AC-030: Reasons include weights

### Quorum Aggregation
- [ ] AC-031: Quorum count configurable
- [ ] AC-032: Default quorum = 2
- [ ] AC-033: Fired >= quorum = burst
- [ ] AC-034: Confidence = average of fired
- [ ] AC-035: Scale = max of fired
- [ ] AC-036: Percentage quorum works
- [ ] AC-037: 50% of 3 = 2 (rounds up)
- [ ] AC-038: Invalid quorum logged

### Observability
- [ ] AC-039: AggregationCompletedEvent published
- [ ] AC-040: Strategy usage logged
- [ ] AC-041: Triggers fired counted
- [ ] AC-042: Decision trace available
- [ ] AC-043: Metrics by strategy
- [ ] AC-044: Weight config logged on startup

---

## User Verification Scenarios

### Scenario 1: OR Aggregation Single Trigger
**Persona:** Developer with queue spike  
**Preconditions:** OR strategy, only queue depth fires  
**Steps:**
1. Queue 15 tasks (depth trigger fires)
2. Other triggers not firing
3. Check aggregated decision
4. Verify burst occurs

**Verification Checklist:**
- [ ] Single trigger detected
- [ ] OR logic returns burst=true
- [ ] Confidence = depth trigger confidence
- [ ] Scale = depth trigger scale

### Scenario 2: AND Aggregation All Required
**Persona:** Operations with conservative settings  
**Preconditions:** AND strategy, 3 of 4 triggers fire  
**Steps:**
1. Queue depth fires
2. CPU fires
3. Worker saturation fires
4. Priority does NOT fire
5. Check decision

**Verification Checklist:**
- [ ] 3 of 4 triggers fired
- [ ] AND requires all 4
- [ ] Decision = no burst
- [ ] Reason: "Priority trigger not fired"

### Scenario 3: Weighted Score Threshold
**Persona:** Developer with balanced approach  
**Preconditions:** Weighted strategy, threshold 0.5  
**Steps:**
1. Queue depth fires (conf 0.7, weight 1.0)
2. CPU does not fire
3. Worker fires (conf 0.9, weight 0.9)
4. Calculate score
5. Check if >= 0.5

**Verification Checklist:**
- [ ] Score = (0.7×1.0 + 0.9×0.9) / (1.0+0.9) = 0.79
- [ ] Score 0.79 >= 0.5
- [ ] Burst = true
- [ ] Confidence = 0.79

### Scenario 4: Quorum 2 of 5
**Persona:** Operations with flexible policy  
**Preconditions:** Quorum strategy, quorum=2  
**Steps:**
1. 5 triggers enabled
2. Queue depth fires
3. CPU fires
4. Others do not fire
5. Check decision

**Verification Checklist:**
- [ ] 2 triggers fired
- [ ] 2 >= quorum 2
- [ ] Burst = true
- [ ] Confidence = average of 2

### Scenario 5: Percentage Quorum
**Persona:** Developer with dynamic triggers  
**Preconditions:** Quorum 50%, 5 triggers enabled  
**Steps:**
1. 50% of 5 = 3 (rounded up)
2. 2 triggers fire
3. Decision = no burst
4. 3 triggers fire
5. Decision = burst

**Verification Checklist:**
- [ ] 50% of 5 = 2.5 → 3
- [ ] 2 < 3: no burst
- [ ] 3 >= 3: burst
- [ ] Rounding up verified

### Scenario 6: Strategy Selection at Runtime
**Persona:** Developer switching strategies  
**Preconditions:** Config has weighted, CLI overrides to OR  
**Steps:**
1. Config file: strategy=weighted
2. Run with --burst-strategy or
3. Check OR strategy used
4. Verify override logged

**Verification Checklist:**
- [ ] CLI overrides config
- [ ] OR strategy selected
- [ ] Override logged
- [ ] Correct aggregation behavior

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-033B-01 | OR single trigger fires | FR-033B-18 |
| UT-033B-02 | OR multiple triggers fires | FR-033B-17 |
| UT-033B-03 | OR no triggers = no burst | FR-033B-22 |
| UT-033B-04 | OR confidence = max | FR-033B-19 |
| UT-033B-05 | AND all triggers required | FR-033B-32 |
| UT-033B-06 | AND any missing = no burst | FR-033B-33 |
| UT-033B-07 | AND disabled excluded | FR-033B-38 |
| UT-033B-08 | Weighted score calculation | FR-033B-50 |
| UT-033B-09 | Weighted threshold enforcement | FR-033B-54 |
| UT-033B-10 | Weighted zero weight handling | FR-033B-60 |
| UT-033B-11 | Quorum count enforcement | FR-033B-65 |
| UT-033B-12 | Quorum percentage rounding | FR-033B-75 |
| UT-033B-13 | Quorum invalid validation | FR-033B-70 |
| UT-033B-14 | Registry lookup by strategy | NFR-033B-05 |
| UT-033B-15 | Deterministic results | NFR-033B-11 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-033B-01 | Full aggregation pipeline | E2E |
| IT-033B-02 | Mixed signals all strategies | Multiple |
| IT-033B-03 | Edge case: empty signals | NFR-033B-14 |
| IT-033B-04 | Edge case: all disabled | FR-033B-23 |
| IT-033B-05 | Configuration from yaml | FR-033B-58 |
| IT-033B-06 | Runtime strategy override | Scenario 6 |
| IT-033B-07 | Event publishing | NFR-033B-25 |
| IT-033B-08 | Metrics emission | NFR-033B-22 |
| IT-033B-09 | Performance <5ms | NFR-033B-01 |
| IT-033B-10 | Thread safety | NFR-033B-04 |
| IT-033B-11 | Custom aggregator registration | FR-033B-13 |
| IT-033B-12 | Logging completeness | NFR-033B-21 |
| IT-033B-13 | Short-circuit optimization | FR-033B-28 |
| IT-033B-14 | Partial AND (N of M) | FR-033B-41 |
| IT-033B-15 | Weight configuration reload | Dynamic |

---

## Implementation Prompt

### Part 1: File Structure + Domain Models

```
src/
├── Acode.Domain/
│   └── Compute/
│       └── Burst/
│           └── Aggregation/
│               ├── AggregationStrategy.cs
│               └── Events/
│                   └── AggregationCompletedEvent.cs
├── Acode.Application/
│   └── Compute/
│       └── Burst/
│           └── Aggregation/
│               ├── ITriggerAggregator.cs
│               ├── IAggregatorRegistry.cs
│               ├── AggregatedDecision.cs
│               └── AggregationOptions.cs
└── Acode.Infrastructure/
    └── Compute/
        └── Burst/
            └── Aggregation/
                ├── AggregatorRegistry.cs
                ├── OrAggregator.cs
                ├── AndAggregator.cs
                ├── WeightedAggregator.cs
                └── QuorumAggregator.cs
```

```csharp
// src/Acode.Domain/Compute/Burst/Aggregation/AggregationStrategy.cs
namespace Acode.Domain.Compute.Burst.Aggregation;

public enum AggregationStrategy
{
    Or,       // Any trigger fires
    And,      // All triggers fire
    Weighted, // Score threshold
    Quorum    // N of M triggers
}

// src/Acode.Domain/Compute/Burst/Aggregation/Events/AggregationCompletedEvent.cs
namespace Acode.Domain.Compute.Burst.Aggregation.Events;

public sealed record AggregationCompletedEvent(
    AggregationStrategy Strategy,
    bool ShouldBurst,
    double Confidence,
    int TriggersEvaluated,
    int TriggersFired,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 033.b Specification - Part 1/3**

### Part 2: Application Interfaces

```csharp
// src/Acode.Application/Compute/Burst/Aggregation/AggregationOptions.cs
namespace Acode.Application.Compute.Burst.Aggregation;

public sealed record AggregationOptions
{
    public double ConfidenceThreshold { get; init; } = 0.0;
    public int? Quorum { get; init; }
    public double? QuorumPercentage { get; init; }
    public IReadOnlyDictionary<string, double> Weights { get; init; } = new Dictionary<string, double>();
}

// src/Acode.Application/Compute/Burst/Aggregation/AggregatedDecision.cs
namespace Acode.Application.Compute.Burst.Aggregation;

public sealed record AggregatedDecision
{
    public bool ShouldBurst { get; init; }
    public double Confidence { get; init; }
    public int SuggestedScale { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
    public int TriggersEvaluated { get; init; }
    public int TriggersFired { get; init; }
    public AggregationStrategy Strategy { get; init; }
}

// src/Acode.Application/Compute/Burst/Aggregation/ITriggerAggregator.cs
namespace Acode.Application.Compute.Burst.Aggregation;

public interface ITriggerAggregator
{
    string Name { get; }
    AggregationStrategy Strategy { get; }
    
    AggregatedDecision Aggregate(
        IReadOnlyList<TriggerSignal> signals,
        AggregationOptions? options = null);
}

// src/Acode.Application/Compute/Burst/Aggregation/IAggregatorRegistry.cs
namespace Acode.Application.Compute.Burst.Aggregation;

public interface IAggregatorRegistry
{
    void Register(ITriggerAggregator aggregator);
    ITriggerAggregator? Get(string name);
    ITriggerAggregator? Get(AggregationStrategy strategy);
    ITriggerAggregator GetDefault();
    IReadOnlyList<ITriggerAggregator> GetAll();
}
```

**End of Task 033.b Specification - Part 2/3**

### Part 3: Infrastructure Implementation + Checklist

```csharp
// src/Acode.Infrastructure/Compute/Burst/Aggregation/OrAggregator.cs
namespace Acode.Infrastructure.Compute.Burst.Aggregation;

public sealed class OrAggregator : ITriggerAggregator
{
    public string Name => "or";
    public AggregationStrategy Strategy => AggregationStrategy.Or;
    
    public AggregatedDecision Aggregate(IReadOnlyList<TriggerSignal> signals, AggregationOptions? options = null)
    {
        options ??= new AggregationOptions();
        var fired = signals.Where(s => s.Triggered && s.Confidence >= options.ConfidenceThreshold).ToList();
        
        return new AggregatedDecision
        {
            ShouldBurst = fired.Count > 0,
            Confidence = fired.Count > 0 ? fired.Max(s => s.Confidence) : 0.0,
            SuggestedScale = fired.Count > 0 ? fired.Max(s => s.SuggestedScale) : 0,
            Reasons = fired.Select(s => s.Reason!).ToList(),
            TriggersEvaluated = signals.Count,
            TriggersFired = fired.Count,
            Strategy = AggregationStrategy.Or
        };
    }
}

// src/Acode.Infrastructure/Compute/Burst/Aggregation/WeightedAggregator.cs
namespace Acode.Infrastructure.Compute.Burst.Aggregation;

public sealed class WeightedAggregator : ITriggerAggregator
{
    public string Name => "weighted";
    public AggregationStrategy Strategy => AggregationStrategy.Weighted;
    
    public AggregatedDecision Aggregate(IReadOnlyList<TriggerSignal> signals, AggregationOptions? options = null)
    {
        options ??= new AggregationOptions();
        var threshold = options.ConfidenceThreshold > 0 ? options.ConfidenceThreshold : 0.5;
        
        var totalWeight = 0.0;
        var weightedScore = 0.0;
        var reasons = new List<string>();
        var fired = 0;
        
        foreach (var signal in signals)
        {
            var weight = options.Weights.GetValueOrDefault(signal.TriggerName, 1.0);
            totalWeight += weight;
            if (signal.Triggered)
            {
                weightedScore += signal.Confidence * weight;
                fired++;
                reasons.Add($"{signal.Reason} (weight {weight:F1}, confidence {signal.Confidence:F2})");
            }
        }
        
        var normalizedScore = totalWeight > 0 ? weightedScore / totalWeight : 0.0;
        
        return new AggregatedDecision
        {
            ShouldBurst = normalizedScore >= threshold,
            Confidence = normalizedScore,
            SuggestedScale = signals.Where(s => s.Triggered).Select(s => s.SuggestedScale).DefaultIfEmpty(0).Max(),
            Reasons = reasons,
            TriggersEvaluated = signals.Count,
            TriggersFired = fired,
            Strategy = AggregationStrategy.Weighted
        };
    }
}

// src/Acode.Infrastructure/Compute/Burst/Aggregation/QuorumAggregator.cs
namespace Acode.Infrastructure.Compute.Burst.Aggregation;

public sealed class QuorumAggregator : ITriggerAggregator
{
    public string Name => "quorum";
    public AggregationStrategy Strategy => AggregationStrategy.Quorum;
    
    public AggregatedDecision Aggregate(IReadOnlyList<TriggerSignal> signals, AggregationOptions? options = null)
    {
        options ??= new AggregationOptions();
        var fired = signals.Where(s => s.Triggered).ToList();
        
        var requiredQuorum = options.Quorum 
            ?? (options.QuorumPercentage.HasValue 
                ? (int)Math.Ceiling(signals.Count * options.QuorumPercentage.Value) 
                : 2);
        
        requiredQuorum = Math.Min(requiredQuorum, signals.Count);
        var meetsQuorum = fired.Count >= requiredQuorum;
        
        return new AggregatedDecision
        {
            ShouldBurst = meetsQuorum,
            Confidence = fired.Count > 0 ? fired.Average(s => s.Confidence) : 0.0,
            SuggestedScale = fired.Count > 0 ? fired.Max(s => s.SuggestedScale) : 0,
            Reasons = fired.Select(s => s.Reason!).ToList(),
            TriggersEvaluated = signals.Count,
            TriggersFired = fired.Count,
            Strategy = AggregationStrategy.Quorum
        };
    }
}

// AndAggregator follows similar pattern with All() logic
```

### Implementation Checklist

| Step | Action | Verification |
|------|--------|--------------|
| 1 | Create AggregationStrategy enum | Enum compiles |
| 2 | Add aggregation events | Event serialization verified |
| 3 | Define AggregationOptions, AggregatedDecision | Records compile |
| 4 | Create ITriggerAggregator, IAggregatorRegistry | Interface contracts clear |
| 5 | Implement OrAggregator | Any-fired logic works |
| 6 | Implement AndAggregator | All-fired logic works |
| 7 | Implement WeightedAggregator | Weighted scoring verified |
| 8 | Implement QuorumAggregator | N-of-M logic works |
| 9 | Implement AggregatorRegistry | Strategy lookup works |
| 10 | Add confidence threshold | Below threshold filtered |
| 11 | Add percentage quorum | Percentage rounds up |
| 12 | Register aggregators in DI | All aggregators resolved |
| 13 | Add metrics per strategy | Strategy metrics emitted |
| 14 | Performance verify <5ms | Benchmark passes |

### Rollout Plan

1. **Phase 1**: Implement interfaces and OrAggregator
2. **Phase 2**: Add AndAggregator
3. **Phase 3**: Add WeightedAggregator with configurable weights
4. **Phase 4**: Add QuorumAggregator with percentage support
5. **Phase 5**: Implement registry and integration tests

**End of Task 033.b Specification**