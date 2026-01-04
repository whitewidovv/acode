# Task 032.c: Placement Strategy Implementations

**Priority:** P1 – High  
**Tier:** L – Cloud Integration  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 032 (Placement), Task 032.b (Matching)  

---

## Description

Task 032.c implements concrete placement strategies. Each strategy encapsulates a decision algorithm. Strategies MUST be pluggable and selectable.

Different strategies suit different scenarios. Local-first minimizes cloud costs. Cloud-first maximizes isolation. Cost-optimized minimizes spending.

Users select strategies based on their priorities.

### Business Value

Multiple strategies provide:
- Flexible decision making
- User-driven optimization
- Scenario-specific placement
- Easy customization

### Scope Boundaries

This task covers strategy implementations. Matching is in 032.b. Burst heuristics are in Task 033.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Task 032 Placement Engine | IPlacementStrategy | Score → Engine | Engine calls strategies |
| Task 032.b Matching | ICapabilityMatcher | Caps → Match → Score | Match result feeds scoring |
| Task 033 Burst Heuristics | IBurstHeuristics | Heuristics → Strategy | Heuristics complement strategy |
| Strategy Registry | IPlacementStrategyRegistry | Name → Strategy | Lookup by name |
| agent-config.yml | Config parser | Options → Strategy | Per-strategy configuration |
| CLI | --placement-strategy | Override → Engine | Runtime strategy selection |
| Metrics System | IMetrics | Stats → Metrics | Per-strategy telemetry |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| No valid target | All scores = 0 | Clear error message | Task cannot run |
| Strategy exception | Catch in engine | Use fallback strategy | Degraded selection |
| All strategies fail | Cascade failure | Try auto as last resort | May still fail |
| Mode violation | Strategy detects | Exclude violating targets | Correct behavior |
| Config parse error | Invalid options | Use default options | Non-optimal selection |
| Timeout during score | Task timeout | Return 0 for target | Target excluded |
| Missing cost data | Null hourly rate | Assume high cost | Conservative selection |
| Concurrent requests | Race condition | Stateless design | No issue |

---

## Assumptions

1. All strategies receive the same inputs (requirements, capabilities, options)
2. Strategies are stateless and can be called concurrently
3. Match results from 032.b are available before strategy scoring
4. Cost data for cloud targets is available in capabilities
5. Local targets are always considered $0 cost unless configured
6. Operating mode is accessible to all strategies for compliance checks
7. Fallback strategies are tried in order: requested → auto → error
8. Strategy selection can be overridden at runtime via CLI

---

## Security Considerations

1. Strategy selection MUST NOT expose internal cost calculations to users
2. Cloud credentials MUST NOT be accessed directly by strategies
3. Strategy configuration MUST be validated for reasonable values
4. Cost optimization MUST NOT bypass budget limits
5. Strategy logs MUST NOT include sensitive target details
6. Custom strategies MUST be sandboxed from system access
7. Strategy fallback MUST be logged for audit
8. Performance strategy MUST respect cost limits when configured

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Strategy | Decision algorithm |
| Local-first | Prefer local execution |
| Cloud-first | Prefer cloud execution |
| Cost-optimized | Minimize spending |
| Performance | Minimize latency |
| Auto | Balanced approach |

---

## Out of Scope

- Machine learning strategies
- Multi-objective optimization
- Pareto frontier analysis
- Strategy composition
- Dynamic strategy switching

---

## Functional Requirements

### Strategy Interface

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032C-01 | `IPlacementStrategy` interface MUST exist | P0 |
| FR-032C-02 | `Name` property MUST return strategy identifier | P0 |
| FR-032C-03 | `ScoreAsync` MUST return double score | P0 |
| FR-032C-04 | Score range: 0.0 to 1.0 | P0 |
| FR-032C-05 | Higher score = better fit for this strategy | P0 |
| FR-032C-06 | Strategy MUST be stateless | P0 |
| FR-032C-07 | Strategy MUST be thread-safe | P0 |
| FR-032C-08 | Strategy MUST accept configuration options | P1 |
| FR-032C-09 | Configuration via strongly-typed options record | P1 |
| FR-032C-10 | Strategy MUST handle any target type | P0 |
| FR-032C-11 | Unknown target type MUST return valid score | P0 |
| FR-032C-12 | Strategy MUST log scoring decisions | P1 |
| FR-032C-13 | Strategy MUST emit metrics on score | P1 |
| FR-032C-14 | Strategy MUST be unit testable | P0 |
| FR-032C-15 | Strategies MUST be composable (chained) | P2 |

### Auto Strategy

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032C-16 | `AutoPlacementStrategy` MUST exist | P0 |
| FR-032C-17 | Auto MUST be the default strategy | P0 |
| FR-032C-18 | Auto MUST balance multiple factors | P0 |
| FR-032C-19 | Factors: capability, cost, locality | P0 |
| FR-032C-20 | Match score weight default: 0.4 | P1 |
| FR-032C-21 | Cost score weight default: 0.3 | P1 |
| FR-032C-22 | Locality score weight default: 0.3 | P1 |
| FR-032C-23 | Weights MUST be configurable | P1 |
| FR-032C-24 | Local target bonus MUST exist | P1 |
| FR-032C-25 | Local bonus default: +0.1 | P1 |
| FR-032C-26 | Sufficient local capability MUST prefer local | P0 |
| FR-032C-27 | Insufficient local MUST allow cloud burst | P0 |
| FR-032C-28 | Auto MUST respect operating mode | P0 |
| FR-032C-29 | local-only mode MUST exclude cloud | P0 |
| FR-032C-30 | burst mode MUST allow cloud targets | P0 |

### Local-First Strategy

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032C-31 | `LocalFirstStrategy` MUST exist | P0 |
| FR-032C-32 | Local targets MUST score higher than cloud | P0 |
| FR-032C-33 | Local base score: 1.0 | P0 |
| FR-032C-34 | Cloud base score: 0.5 (when fallback allowed) | P0 |
| FR-032C-35 | Capability match MUST still be applied | P0 |
| FR-032C-36 | Insufficient local MUST trigger fallback | P0 |
| FR-032C-37 | Fallback to cloud MUST be configurable | P1 |
| FR-032C-38 | Default: fallback enabled | P1 |
| FR-032C-39 | No fallback + no local = error | P0 |
| FR-032C-40 | Local queue timeout MUST trigger burst | P1 |
| FR-032C-41 | Timeout MUST be configurable | P1 |
| FR-032C-42 | Default timeout: 5 minutes | P1 |
| FR-032C-43 | Queue length MUST trigger burst | P1 |
| FR-032C-44 | Queue threshold MUST be configurable | P1 |
| FR-032C-45 | Default queue threshold: 10 tasks | P1 |

### Cloud-First Strategy

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032C-46 | `CloudFirstStrategy` MUST exist | P0 |
| FR-032C-47 | Cloud targets MUST score higher than local | P0 |
| FR-032C-48 | Cloud base score: 1.0 | P0 |
| FR-032C-49 | Local base score: 0.5 (when fallback allowed) | P0 |
| FR-032C-50 | Use case: isolation/reproducibility | P1 |
| FR-032C-51 | Use case: fresh environment every time | P1 |
| FR-032C-52 | Local fallback MUST work when cloud unavailable | P0 |
| FR-032C-53 | Fallback when cloud timeout | P0 |
| FR-032C-54 | Cloud timeout MUST be configurable | P1 |
| FR-032C-55 | Timeout: fall to local if exceeded | P0 |
| FR-032C-56 | Cost limit MUST be respected | P0 |
| FR-032C-57 | Over budget MUST fall to local | P0 |
| FR-032C-58 | Instance type preference MUST work | P1 |
| FR-032C-59 | Prefer configured instance types list | P1 |
| FR-032C-60 | Region preference MUST work | P1 |

### Cost-Optimized Strategy

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032C-61 | `CostOptimizedStrategy` MUST exist | P0 |
| FR-032C-62 | Lowest cost target MUST win | P0 |
| FR-032C-63 | Local cost: $0 (or configured value) | P0 |
| FR-032C-64 | Cloud cost from pricing/capabilities | P0 |
| FR-032C-65 | Spot instances MUST be preferred | P1 |
| FR-032C-66 | Spot discount factored into score | P1 |
| FR-032C-67 | Capability match MUST still apply | P0 |
| FR-032C-68 | Minimum capability threshold enforced | P0 |
| FR-032C-69 | Below threshold: target skipped (score 0) | P0 |
| FR-032C-70 | Cost per capability MUST be calculated | P1 |
| FR-032C-71 | Example: $/CPU-hour metric | P1 |
| FR-032C-72 | Best cost/capability ratio MUST win | P1 |
| FR-032C-73 | Time estimate MUST be used if available | P1 |
| FR-032C-74 | Total cost = hourly rate × estimated time | P1 |
| FR-032C-75 | Shortest execution time factors into score | P2 |

### Performance Strategy

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032C-76 | `PerformanceStrategy` MUST exist | P0 |
| FR-032C-77 | Highest capability target MUST win | P0 |
| FR-032C-78 | More CPU = higher score | P0 |
| FR-032C-79 | More memory = higher score | P0 |
| FR-032C-80 | GPU presence = higher score | P0 |
| FR-032C-81 | Faster network = higher score (optional) | P2 |
| FR-032C-82 | SSD storage = higher score (optional) | P2 |
| FR-032C-83 | Cost MUST be secondary consideration | P1 |
| FR-032C-84 | Performance within budget constraint | P1 |
| FR-032C-85 | Excess capability valuable for parallel tasks | P2 |

---

## Non-Functional Requirements

### Performance

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-032C-01 | Single strategy score call | <50ms | P0 |
| NFR-032C-02 | All strategies deterministic | Same result | P0 |
| NFR-032C-03 | Strategies stateless | No mutable state | P0 |
| NFR-032C-04 | Thread-safe execution | Concurrent calls | P0 |
| NFR-032C-05 | Memory per score call | <1KB | P1 |
| NFR-032C-06 | Score 100 targets | <2 seconds | P1 |
| NFR-032C-07 | Strategy lookup | O(1) | P0 |
| NFR-032C-08 | Parallel target scoring | Supported | P1 |
| NFR-032C-09 | Options parsing | <1ms | P1 |
| NFR-032C-10 | Registry initialization | <100ms | P1 |

### Reliability

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-032C-11 | Exception safety | Catch and return 0 | P0 |
| NFR-032C-12 | Fallback chain | Always tried | P0 |
| NFR-032C-13 | Mode compliance | Enforced | P0 |
| NFR-032C-14 | Invalid options | Use defaults | P1 |
| NFR-032C-15 | Missing cost data | Conservative score | P1 |
| NFR-032C-16 | Null capabilities | Score 0 | P0 |
| NFR-032C-17 | Strategy not found | Use auto | P0 |
| NFR-032C-18 | All targets fail | Clear error | P0 |
| NFR-032C-19 | Concurrent registration | Thread-safe | P1 |
| NFR-032C-20 | Custom strategy errors | Sandboxed | P2 |

### Observability

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-032C-21 | Structured logging | All scoring | P0 |
| NFR-032C-22 | Score duration metric | Per-strategy histogram | P1 |
| NFR-032C-23 | Strategy selection metric | Counter by strategy | P1 |
| NFR-032C-24 | Fallback event metric | Counter | P1 |
| NFR-032C-25 | StrategySelectedEvent | Published | P1 |
| NFR-032C-26 | StrategyFallbackEvent | Published on fallback | P1 |
| NFR-032C-27 | Score breakdown logging | Debug level | P2 |
| NFR-032C-28 | Trace correlation | Request ID | P1 |
| NFR-032C-29 | Strategy registry export | Available strategies | P2 |
| NFR-032C-30 | Configuration audit | Logged on startup | P2 |

---

## User Manual Documentation

### Strategy Selection

```yaml
placement:
  strategy: auto  # Options: auto, local-first, cloud-first, cost-optimized, performance
```

### Strategy Comparison

| Strategy | Best For | Trade-off |
|----------|----------|-----------|
| auto | General use | Balanced |
| local-first | Cost conscious | May be slower |
| cloud-first | Isolation | Costs money |
| cost-optimized | Budget limited | May be slow |
| performance | Speed critical | May be expensive |

### Strategy Weights (Auto)

```yaml
placement:
  strategy: auto
  weights:
    capability: 0.4
    cost: 0.3
    locality: 0.3
```

### CLI Override

```bash
# Use specific strategy
acode run --placement-strategy cost-optimized

# Force local
acode run --placement-strategy local-first --no-fallback
```

---

## Acceptance Criteria / Definition of Done

### Strategy Interface
- [ ] AC-001: `IPlacementStrategy` interface exists
- [ ] AC-002: `Name` property returns strategy identifier
- [ ] AC-003: `ScoreAsync` returns double between 0.0 and 1.0
- [ ] AC-004: Strategies are stateless (no mutable state)
- [ ] AC-005: Strategies are thread-safe
- [ ] AC-006: Strategy accepts options via constructor
- [ ] AC-007: Registry resolves strategies by name

### Auto Strategy
- [ ] AC-008: `AutoPlacementStrategy` is registered as default
- [ ] AC-009: Auto balances capability, cost, locality
- [ ] AC-010: Default weights: capability 0.4, cost 0.3, locality 0.3
- [ ] AC-011: Local target gets +0.1 bonus
- [ ] AC-012: Weights configurable via options
- [ ] AC-013: local-only mode excludes cloud targets
- [ ] AC-014: burst mode allows all targets
- [ ] AC-015: Insufficient local triggers cloud consideration

### Local-First Strategy
- [ ] AC-016: `LocalFirstStrategy` prefers local targets
- [ ] AC-017: Local base score is 1.0
- [ ] AC-018: Cloud base score is 0.5 (with fallback)
- [ ] AC-019: Cloud base score is 0.0 (without fallback)
- [ ] AC-020: Capability match still applied
- [ ] AC-021: Fallback enabled by default
- [ ] AC-022: Queue timeout triggers burst
- [ ] AC-023: Queue length triggers burst

### Cloud-First Strategy
- [ ] AC-024: `CloudFirstStrategy` prefers cloud targets
- [ ] AC-025: Cloud base score is 1.0
- [ ] AC-026: Local base score is 0.5 (with fallback)
- [ ] AC-027: Budget limit enforced
- [ ] AC-028: Over budget falls to local
- [ ] AC-029: Instance type preference works
- [ ] AC-030: Region preference works
- [ ] AC-031: Cloud timeout triggers local fallback

### Cost-Optimized Strategy
- [ ] AC-032: `CostOptimizedStrategy` prefers cheapest
- [ ] AC-033: Local scores 1.0 (free)
- [ ] AC-034: Cloud scored inversely with cost
- [ ] AC-035: Spot instances preferred
- [ ] AC-036: Minimum capability threshold enforced
- [ ] AC-037: Below threshold scores 0.0
- [ ] AC-038: Cost per capability calculated
- [ ] AC-039: Time estimate used if available

### Performance Strategy
- [ ] AC-040: `PerformanceStrategy` prefers highest capability
- [ ] AC-041: More CPU = higher score
- [ ] AC-042: More memory = higher score
- [ ] AC-043: GPU presence = higher score
- [ ] AC-044: Budget constraint respected
- [ ] AC-045: Weights configurable per resource

### Strategy Registry
- [ ] AC-046: Registry resolves by name (case-insensitive)
- [ ] AC-047: GetDefault returns auto
- [ ] AC-048: GetAll returns all registered
- [ ] AC-049: Unknown name returns null
- [ ] AC-050: Fallback to auto when requested not found

### Observability
- [ ] AC-051: StrategySelectedEvent published
- [ ] AC-052: StrategyFallbackEvent published
- [ ] AC-053: Scoring decisions logged
- [ ] AC-054: Metrics emitted per strategy
- [ ] AC-055: Score breakdown available in debug

---

## User Verification Scenarios

### Scenario 1: Auto Strategy Selection
**Persona:** Developer with default configuration  
**Preconditions:** Auto is default, local and cloud targets available  
**Steps:**
1. Submit task without specifying strategy
2. Observe auto strategy used
3. Check balanced scoring
4. Verify target selection

**Verification Checklist:**
- [ ] Auto strategy selected by default
- [ ] Capability, cost, locality all factored
- [ ] Local target gets bonus
- [ ] Reasonable target selected

### Scenario 2: Local-First with Fallback
**Persona:** Cost-conscious developer  
**Preconditions:** Local target insufficient, cloud available  
**Steps:**
1. Set strategy to local-first
2. Submit GPU task (local has no GPU)
3. Observe fallback to cloud
4. Check fallback logged

**Verification Checklist:**
- [ ] Local tried first (score 1.0)
- [ ] Local fails capability match
- [ ] Cloud used as fallback (score 0.5)
- [ ] StrategyFallbackEvent published

### Scenario 3: Local-First without Fallback
**Persona:** Airgapped environment  
**Preconditions:** Fallback disabled, local insufficient  
**Steps:**
1. Set strategy to local-first --no-fallback
2. Submit task local can't handle
3. Observe error returned
4. No cloud attempted

**Verification Checklist:**
- [ ] Cloud score = 0.0 (fallback disabled)
- [ ] No valid target found
- [ ] Clear error message
- [ ] Guidance to enable fallback

### Scenario 4: Cost-Optimized with Spot
**Persona:** Budget-limited developer  
**Preconditions:** Spot and on-demand instances available  
**Steps:**
1. Set strategy to cost-optimized
2. Submit task
3. Observe spot instance preferred
4. Check cost calculation

**Verification Checklist:**
- [ ] Spot instance scored higher
- [ ] Cost discount factored
- [ ] Cheapest viable target selected
- [ ] Cost shown in logs

### Scenario 5: Performance Strategy for ML
**Persona:** ML developer needing GPU  
**Preconditions:** Multiple GPU instances available  
**Steps:**
1. Set strategy to performance
2. Submit GPU training task
3. Observe highest GPU target selected
4. Budget respected

**Verification Checklist:**
- [ ] GPU presence weighted highly
- [ ] More VRAM = higher score
- [ ] Budget limit not exceeded
- [ ] Performance target selected

### Scenario 6: CLI Strategy Override
**Persona:** Developer testing different strategies  
**Preconditions:** Config has auto, CLI overrides  
**Steps:**
1. Config file sets auto strategy
2. Run with --placement-strategy cloud-first
3. Observe cloud-first used
4. Override logged

**Verification Checklist:**
- [ ] CLI overrides config
- [ ] cloud-first strategy used
- [ ] Override logged
- [ ] Correct target selected

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-032C-01 | Auto strategy weight calculation | FR-032C-20-22 |
| UT-032C-02 | Auto local bonus application | FR-032C-25 |
| UT-032C-03 | Auto mode compliance | FR-032C-29-30 |
| UT-032C-04 | Local-first base scores | FR-032C-33-34 |
| UT-032C-05 | Local-first fallback enabled | FR-032C-38 |
| UT-032C-06 | Local-first fallback disabled | FR-032C-39 |
| UT-032C-07 | Cloud-first base scores | FR-032C-48-49 |
| UT-032C-08 | Cloud-first budget limit | FR-032C-57 |
| UT-032C-09 | Cost-optimized local = free | FR-032C-63 |
| UT-032C-10 | Cost-optimized spot preference | FR-032C-65 |
| UT-032C-11 | Cost-optimized min threshold | FR-032C-68 |
| UT-032C-12 | Performance CPU weighting | FR-032C-78 |
| UT-032C-13 | Performance GPU weighting | FR-032C-80 |
| UT-032C-14 | Performance budget constraint | FR-032C-84 |
| UT-032C-15 | Registry name lookup | NFR-032C-07 |
| UT-032C-16 | Registry default = auto | AC-047 |
| UT-032C-17 | Strategy determinism | NFR-032C-02 |
| UT-032C-18 | Strategy statelessness | NFR-032C-03 |
| UT-032C-19 | Invalid options uses defaults | NFR-032C-14 |
| UT-032C-20 | Null capabilities = score 0 | NFR-032C-16 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-032C-01 | Auto strategy end-to-end | E2E |
| IT-032C-02 | Local-first with fallback | FR-032C-36 |
| IT-032C-03 | Cloud-first with timeout | FR-032C-55 |
| IT-032C-04 | Cost-optimized multi-target | FR-032C-72 |
| IT-032C-05 | Performance multi-target | FR-032C-77 |
| IT-032C-06 | Strategy fallback chain | NFR-032C-12 |
| IT-032C-07 | Mode compliance enforcement | NFR-032C-13 |
| IT-032C-08 | CLI strategy override | Scenario 6 |
| IT-032C-09 | Registry all strategies | AC-048 |
| IT-032C-10 | Event publishing | NFR-032C-25 |
| IT-032C-11 | Metrics emission | NFR-032C-22 |
| IT-032C-12 | Parallel target scoring | NFR-032C-08 |
| IT-032C-13 | Performance <50ms | NFR-032C-01 |
| IT-032C-14 | Thread safety | NFR-032C-04 |
| IT-032C-15 | 100 targets performance | NFR-032C-06 |

---

## Implementation Prompt

### Part 1: File Structure + Domain Models

```
src/
├── Acode.Domain/
│   └── Compute/
│       └── Placement/
│           └── Strategies/
│               └── Events/
│                   ├── StrategySelectedEvent.cs
│                   └── StrategyFallbackEvent.cs
├── Acode.Application/
│   └── Compute/
│       └── Placement/
│           └── Strategies/
│               ├── IPlacementStrategyRegistry.cs
│               ├── AutoStrategyOptions.cs
│               ├── LocalFirstOptions.cs
│               ├── CloudFirstOptions.cs
│               ├── CostOptimizedOptions.cs
│               └── PerformanceOptions.cs
└── Acode.Infrastructure/
    └── Compute/
        └── Placement/
            └── Strategies/
                ├── PlacementStrategyRegistry.cs
                ├── AutoPlacementStrategy.cs
                ├── LocalFirstStrategy.cs
                ├── CloudFirstStrategy.cs
                ├── CostOptimizedStrategy.cs
                └── PerformanceStrategy.cs
```

```csharp
// src/Acode.Domain/Compute/Placement/Strategies/Events/StrategySelectedEvent.cs
namespace Acode.Domain.Compute.Placement.Strategies.Events;

public sealed record StrategySelectedEvent(
    string StrategyName,
    string SelectedTargetId,
    double Score,
    int CandidatesEvaluated,
    TimeSpan Duration,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Placement/Strategies/Events/StrategyFallbackEvent.cs
namespace Acode.Domain.Compute.Placement.Strategies.Events;

public sealed record StrategyFallbackEvent(
    string OriginalStrategy,
    string FallbackStrategy,
    string Reason,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 032.c Specification - Part 1/3**

### Part 2: Application Interfaces + Options

```csharp
// src/Acode.Application/Compute/Placement/Strategies/AutoStrategyOptions.cs
namespace Acode.Application.Compute.Placement.Strategies;

public sealed record AutoStrategyOptions
{
    public double CapabilityWeight { get; init; } = 0.4;
    public double CostWeight { get; init; } = 0.3;
    public double LocalityWeight { get; init; } = 0.3;
    public double LocalBonus { get; init; } = 0.1;
}

// src/Acode.Application/Compute/Placement/Strategies/LocalFirstOptions.cs
namespace Acode.Application.Compute.Placement.Strategies;

public sealed record LocalFirstOptions
{
    public bool AllowFallback { get; init; } = true;
    public TimeSpan LocalTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public int QueueThreshold { get; init; } = 10;
}

// src/Acode.Application/Compute/Placement/Strategies/CloudFirstOptions.cs
namespace Acode.Application.Compute.Placement.Strategies;

public sealed record CloudFirstOptions
{
    public bool AllowLocalFallback { get; init; } = true;
    public TimeSpan CloudTimeout { get; init; } = TimeSpan.FromMinutes(10);
    public decimal? MaxBudget { get; init; }
    public IReadOnlyList<string> PreferredInstanceTypes { get; init; } = [];
    public IReadOnlyList<string> PreferredRegions { get; init; } = [];
}

// src/Acode.Application/Compute/Placement/Strategies/CostOptimizedOptions.cs
namespace Acode.Application.Compute.Placement.Strategies;

public sealed record CostOptimizedOptions
{
    public bool PreferSpot { get; init; } = true;
    public double MinCapabilityScore { get; init; } = 0.5;
    public decimal? MaxHourlyRate { get; init; }
    public bool ConsiderTimeEstimate { get; init; } = true;
}

// src/Acode.Application/Compute/Placement/Strategies/PerformanceOptions.cs
namespace Acode.Application.Compute.Placement.Strategies;

public sealed record PerformanceOptions
{
    public double CpuWeight { get; init; } = 1.0;
    public double MemoryWeight { get; init; } = 1.0;
    public double GpuWeight { get; init; } = 2.0;
    public double StorageWeight { get; init; } = 0.5;
    public decimal? MaxBudget { get; init; }
}

// src/Acode.Application/Compute/Placement/Strategies/IPlacementStrategyRegistry.cs
namespace Acode.Application.Compute.Placement.Strategies;

public interface IPlacementStrategyRegistry
{
    void Register(IPlacementStrategy strategy);
    IPlacementStrategy? Get(string name);
    IReadOnlyList<IPlacementStrategy> GetAll();
    IPlacementStrategy GetDefault();
}
```

**End of Task 032.c Specification - Part 2/3**

### Part 3: Infrastructure Implementation + Checklist

```csharp
// src/Acode.Infrastructure/Compute/Placement/Strategies/AutoPlacementStrategy.cs
namespace Acode.Infrastructure.Compute.Placement.Strategies;

public sealed class AutoPlacementStrategy : IPlacementStrategy
{
    private readonly AutoStrategyOptions _options;
    private readonly ICapabilityMatcher _matcher;
    
    public string Name => "auto";
    
    public Task<double> ScoreAsync(
        IComputeTarget target,
        TaskRequirements requirements,
        TargetCapabilities capabilities,
        CancellationToken ct)
    {
        var matchResult = _matcher.Match(requirements, capabilities);
        if (!matchResult.Passed) return Task.FromResult(0.0);
        
        var capScore = matchResult.Score * _options.CapabilityWeight;
        var costScore = CalculateCostScore(target, capabilities) * _options.CostWeight;
        var localScore = (capabilities.IsLocal ? 1.0 : 0.5) * _options.LocalityWeight;
        var bonus = capabilities.IsLocal ? _options.LocalBonus : 0.0;
        
        return Task.FromResult(Math.Min(capScore + costScore + localScore + bonus, 1.0));
    }
    
    public bool CanHandle(TaskRequirements requirements) => true;
}

// src/Acode.Infrastructure/Compute/Placement/Strategies/LocalFirstStrategy.cs
namespace Acode.Infrastructure.Compute.Placement.Strategies;

public sealed class LocalFirstStrategy : IPlacementStrategy
{
    private readonly LocalFirstOptions _options;
    private readonly ICapabilityMatcher _matcher;
    
    public string Name => "local-first";
    
    public Task<double> ScoreAsync(
        IComputeTarget target,
        TaskRequirements requirements,
        TargetCapabilities capabilities,
        CancellationToken ct)
    {
        var matchResult = _matcher.Match(requirements, capabilities);
        if (!matchResult.Passed) return Task.FromResult(0.0);
        
        var baseScore = capabilities.IsLocal ? 1.0 : (_options.AllowFallback ? 0.5 : 0.0);
        return Task.FromResult(baseScore * matchResult.Score);
    }
    
    public bool CanHandle(TaskRequirements requirements) => true;
}

// src/Acode.Infrastructure/Compute/Placement/Strategies/CostOptimizedStrategy.cs
namespace Acode.Infrastructure.Compute.Placement.Strategies;

public sealed class CostOptimizedStrategy : IPlacementStrategy
{
    private readonly CostOptimizedOptions _options;
    private readonly ICapabilityMatcher _matcher;
    
    public string Name => "cost-optimized";
    
    public Task<double> ScoreAsync(
        IComputeTarget target,
        TaskRequirements requirements,
        TargetCapabilities capabilities,
        CancellationToken ct)
    {
        var matchResult = _matcher.Match(requirements, capabilities);
        if (matchResult.Score < _options.MinCapabilityScore) return Task.FromResult(0.0);
        
        // Local is free
        if (capabilities.IsLocal) return Task.FromResult(1.0);
        
        // Score inversely with cost
        var hourlyRate = capabilities.HourlyRate ?? 1.0m;
        if (_options.MaxHourlyRate.HasValue && hourlyRate > _options.MaxHourlyRate)
            return Task.FromResult(0.0);
        
        var costScore = 1.0 - (double)(hourlyRate / 10m);
        return Task.FromResult(Math.Max(costScore, 0.1));
    }
    
    public bool CanHandle(TaskRequirements requirements) => true;
}

// Additional: CloudFirstStrategy, PerformanceStrategy follow similar patterns
```

### Strategy Registry

```csharp
// src/Acode.Infrastructure/Compute/Placement/Strategies/PlacementStrategyRegistry.cs
namespace Acode.Infrastructure.Compute.Placement.Strategies;

public sealed class PlacementStrategyRegistry : IPlacementStrategyRegistry
{
    private readonly Dictionary<string, IPlacementStrategy> _strategies = new(StringComparer.OrdinalIgnoreCase);
    
    public PlacementStrategyRegistry(IEnumerable<IPlacementStrategy> strategies)
    {
        foreach (var s in strategies)
            _strategies[s.Name] = s;
    }
    
    public IPlacementStrategy? Get(string name) => 
        _strategies.GetValueOrDefault(name);
    
    public IReadOnlyList<IPlacementStrategy> GetAll() => 
        _strategies.Values.ToList();
    
    public IPlacementStrategy GetDefault() => 
        _strategies["auto"];
}
```

### Implementation Checklist

| Step | Action | Verification |
|------|--------|--------------|
| 1 | Create strategy events | Event serialization verified |
| 2 | Define all options records | Records compile |
| 3 | Create IPlacementStrategyRegistry | Interface contract clear |
| 4 | Implement AutoPlacementStrategy | Balanced scoring verified |
| 5 | Implement LocalFirstStrategy | Local targets preferred |
| 6 | Implement CloudFirstStrategy | Cloud targets preferred |
| 7 | Implement CostOptimizedStrategy | Cheapest viable selected |
| 8 | Implement PerformanceStrategy | Highest spec selected |
| 9 | Implement PlacementStrategyRegistry | All strategies registered |
| 10 | Add mode compliance checks | local-only blocks cloud |
| 11 | Add fallback logic | Graceful degradation works |
| 12 | Register in DI | All strategies resolved |
| 13 | Add metrics per strategy | Strategy metrics emitted |
| 14 | Performance verify <50ms | Benchmark passes |

### Rollout Plan

1. **Phase 1**: Implement registry and AutoPlacementStrategy
2. **Phase 2**: Add LocalFirstStrategy with fallback
3. **Phase 3**: Add CloudFirstStrategy with budget limits
4. **Phase 4**: Add CostOptimizedStrategy with spot preference
5. **Phase 5**: Add PerformanceStrategy and integration tests

**End of Task 032.c Specification**