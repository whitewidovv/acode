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

- Task 033: Part of heuristics engine
- Task 033.a: Receives trigger signals
- Task 033.c: Passes to rate limiter

### Failure Modes

- No signals → No burst
- All triggers disabled → No burst
- Aggregator error → Log and default to no burst
- Mixed signals → Apply strategy

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

### FR-001 to FR-015: Aggregator Interface

- FR-001: `ITriggerAggregator` MUST exist
- FR-002: `AggregateAsync` MUST return decision
- FR-003: Input: list of signals
- FR-004: Output: aggregated decision
- FR-005: Decision MUST include burst flag
- FR-006: Decision MUST include confidence
- FR-007: Decision MUST include scale
- FR-008: Decision MUST include reasons
- FR-009: Aggregator MUST be configurable
- FR-010: Strategy MUST be selectable
- FR-011: Default strategy: OR
- FR-012: Aggregator MUST be pluggable
- FR-013: Custom aggregators MUST work
- FR-014: Aggregator MUST log
- FR-015: Aggregator MUST emit metrics

### FR-016 to FR-030: OR Aggregation

- FR-016: `OrAggregator` MUST exist
- FR-017: Any trigger MUST burst
- FR-018: Single fired = burst
- FR-019: Confidence: max of all
- FR-020: Scale: max of all
- FR-021: Reasons: all triggered
- FR-022: No triggers = no burst
- FR-023: All disabled = no burst
- FR-024: Threshold MUST be optional
- FR-025: Confidence threshold
- FR-026: Default threshold: 0.0
- FR-027: Below threshold MUST skip
- FR-028: OR MUST short-circuit
- FR-029: Stop on first high confidence
- FR-030: High confidence: >= 0.9

### FR-031 to FR-045: AND Aggregation

- FR-031: `AndAggregator` MUST exist
- FR-032: All triggers MUST fire
- FR-033: Any not fired = no burst
- FR-034: Confidence: min of all
- FR-035: Scale: average of all
- FR-036: Reasons: all reasons
- FR-037: No triggers = no burst
- FR-038: Disabled triggers MUST skip
- FR-039: Only enabled count
- FR-040: Partial AND MUST be optional
- FR-041: Require N of M triggers
- FR-042: Default: all required
- FR-043: Configurable N
- FR-044: Example: 3 of 5
- FR-045: AND MUST evaluate all

### FR-046 to FR-060: Weighted Aggregation

- FR-046: `WeightedAggregator` MUST exist
- FR-047: Triggers MUST have weights
- FR-048: Weight: 0.0 to 1.0
- FR-049: Default weight: 1.0
- FR-050: Score = sum(confidence × weight)
- FR-051: Normalized by sum of weights
- FR-052: Threshold MUST apply
- FR-053: Default threshold: 0.5
- FR-054: Above threshold = burst
- FR-055: Confidence = score
- FR-056: Scale = weighted average
- FR-057: Weights MUST be configurable
- FR-058: Per-trigger weight config
- FR-059: Disabled triggers weight 0
- FR-060: Zero total weight = no burst

### FR-061 to FR-075: Quorum Aggregation

- FR-061: `QuorumAggregator` MUST exist
- FR-062: Quorum count MUST be configurable
- FR-063: Default quorum: 2
- FR-064: At least N triggers MUST fire
- FR-065: Fired count >= quorum = burst
- FR-066: Confidence: average of fired
- FR-067: Scale: max of fired
- FR-068: Reasons: fired triggers only
- FR-069: Quorum MUST be validated
- FR-070: Quorum <= enabled count
- FR-071: Invalid quorum MUST warn
- FR-072: Fallback to available count
- FR-073: Percentage quorum MUST work
- FR-074: Example: 50% of triggers
- FR-075: Percentage rounds up

---

## Non-Functional Requirements

- NFR-001: Aggregation <5ms
- NFR-002: Memory efficient
- NFR-003: Deterministic
- NFR-004: Thread-safe
- NFR-005: Clear decision trail
- NFR-006: Structured logging
- NFR-007: Metrics on strategy
- NFR-008: Easy to test
- NFR-009: Configurable
- NFR-010: No side effects

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

- [ ] AC-001: OR aggregation works
- [ ] AC-002: AND aggregation works
- [ ] AC-003: Weighted works
- [ ] AC-004: Quorum works
- [ ] AC-005: Confidence calculates
- [ ] AC-006: Scale aggregates
- [ ] AC-007: Reasons collect
- [ ] AC-008: Configuration works
- [ ] AC-009: Metrics emit
- [ ] AC-010: Logging complete

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: OR logic
- [ ] UT-002: AND logic
- [ ] UT-003: Weighted scoring
- [ ] UT-004: Quorum counting

### Integration Tests

- [ ] IT-001: Full aggregation
- [ ] IT-002: Mixed signals
- [ ] IT-003: Edge cases
- [ ] IT-004: Configuration

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