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

### Interface

```csharp
public interface ITriggerAggregator
{
    string Name { get; }
    
    AggregatedDecision Aggregate(
        IReadOnlyList<TriggerSignal> signals,
        AggregationOptions options = null);
}

public record AggregatedDecision(
    bool ShouldBurst,
    double Confidence,
    int SuggestedScale,
    IReadOnlyList<string> Reasons,
    int TriggersEvaluated,
    int TriggersFired);

public record AggregationOptions(
    double ConfidenceThreshold = 0.0,
    int? Quorum = null,
    IReadOnlyDictionary<string, double> Weights = null);
```

### Implementations

```csharp
public class OrAggregator : ITriggerAggregator
{
    public string Name => "or";
}

public class AndAggregator : ITriggerAggregator
{
    public string Name => "and";
}

public class WeightedAggregator : ITriggerAggregator
{
    public string Name => "weighted";
}

public class QuorumAggregator : ITriggerAggregator
{
    public string Name => "quorum";
}
```

### Registry

```csharp
public interface IAggregatorRegistry
{
    void Register(ITriggerAggregator aggregator);
    ITriggerAggregator Get(string name);
    ITriggerAggregator GetDefault();
}
```

---

**End of Task 033.b Specification**