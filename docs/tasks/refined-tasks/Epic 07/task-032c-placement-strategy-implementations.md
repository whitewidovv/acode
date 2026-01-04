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

- Task 032: Strategies used by engine
- Task 032.b: Uses match results
- Task 033: Heuristics complement strategies

### Failure Modes

- No valid target → Fall through strategies
- Strategy error → Log and use fallback
- All strategies fail → Clear error

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

### FR-001 to FR-015: Strategy Interface

- FR-001: `IPlacementStrategy` MUST exist
- FR-002: `Name` property MUST exist
- FR-003: `ScoreAsync` MUST return double
- FR-004: Score range: 0.0 to 1.0
- FR-005: Higher score = better fit
- FR-006: Strategy MUST be stateless
- FR-007: Strategy MUST be thread-safe
- FR-008: Strategy MUST be configurable
- FR-009: Configuration via options
- FR-010: Strategy MUST handle any target
- FR-011: Unknown target type MUST work
- FR-012: Strategy MUST log decisions
- FR-013: Strategy MUST emit metrics
- FR-014: Strategy MUST be testable
- FR-015: Strategy MUST be composable

### FR-016 to FR-030: Auto Strategy

- FR-016: `AutoPlacementStrategy` MUST exist
- FR-017: Auto MUST be default
- FR-018: Auto MUST balance factors
- FR-019: Factors: capability, cost, locality
- FR-020: Match score weight: 0.4
- FR-021: Cost score weight: 0.3
- FR-022: Locality score weight: 0.3
- FR-023: Weights MUST be configurable
- FR-024: Local target bonus MUST exist
- FR-025: Local bonus: +0.1
- FR-026: Sufficient local MUST prefer local
- FR-027: Insufficient local MUST burst
- FR-028: Auto MUST consider mode
- FR-029: local-only MUST stay local
- FR-030: Burst MUST allow cloud

### FR-031 to FR-045: Local-First Strategy

- FR-031: `LocalFirstStrategy` MUST exist
- FR-032: Local targets MUST score higher
- FR-033: Local base score: 1.0
- FR-034: Cloud base score: 0.5
- FR-035: Capability match MUST still apply
- FR-036: Insufficient local MUST fallback
- FR-037: Fallback to cloud MUST be optional
- FR-038: Default: fallback enabled
- FR-039: No fallback MUST error
- FR-040: Local timeout MUST trigger burst
- FR-041: Timeout MUST be configurable
- FR-042: Default timeout: 5 minutes
- FR-043: Queue length MUST trigger burst
- FR-044: Queue threshold MUST be configurable
- FR-045: Default queue threshold: 10

### FR-046 to FR-060: Cloud-First Strategy

- FR-046: `CloudFirstStrategy` MUST exist
- FR-047: Cloud targets MUST score higher
- FR-048: Cloud base score: 1.0
- FR-049: Local base score: 0.5
- FR-050: Use case: isolation
- FR-051: Use case: reproducibility
- FR-052: Local fallback MUST work
- FR-053: Fallback when cloud unavailable
- FR-054: Cloud timeout MUST exist
- FR-055: Timeout: fall to local
- FR-056: Cost limit MUST be respected
- FR-057: Over budget MUST fall to local
- FR-058: Instance type preference MUST work
- FR-059: Prefer configured instance types
- FR-060: Region preference MUST work

### FR-061 to FR-075: Cost-Optimized Strategy

- FR-061: `CostOptimizedStrategy` MUST exist
- FR-062: Lowest cost MUST win
- FR-063: Local cost: $0 (or configured)
- FR-064: Cloud cost from pricing
- FR-065: Spot MUST be preferred
- FR-066: Spot discount factored
- FR-067: Capability match MUST still apply
- FR-068: Minimum capability threshold
- FR-069: Below threshold: skip
- FR-070: Cost per capability MUST calculate
- FR-071: Example: $/CPU-hour
- FR-072: Best ratio MUST win
- FR-073: Time estimate MUST be used
- FR-074: Total cost = rate × time
- FR-075: Shortest time MUST factor

### FR-076 to FR-085: Performance Strategy

- FR-076: `PerformanceStrategy` MUST exist
- FR-077: Highest capability MUST win
- FR-078: More CPU = higher score
- FR-079: More memory = higher score
- FR-080: GPU presence = higher score
- FR-081: Faster network = higher score
- FR-082: SSD storage = higher score
- FR-083: Cost MUST be secondary
- FR-084: Performance within budget
- FR-085: Excess capability valuable

---

## Non-Functional Requirements

- NFR-001: Strategy score <50ms
- NFR-002: All strategies deterministic
- NFR-003: Strategies stateless
- NFR-004: Thread-safe
- NFR-005: Memory efficient
- NFR-006: Structured logging
- NFR-007: Metrics per strategy
- NFR-008: Easy to add strategies
- NFR-009: Clear documentation
- NFR-010: Full test coverage

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

- [ ] AC-001: Auto strategy works
- [ ] AC-002: Local-first works
- [ ] AC-003: Cloud-first works
- [ ] AC-004: Cost-optimized works
- [ ] AC-005: Performance works
- [ ] AC-006: Strategy selection works
- [ ] AC-007: Fallback works
- [ ] AC-008: Mode compliance works
- [ ] AC-009: Metrics emitted
- [ ] AC-010: Logging complete

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Auto scoring
- [ ] UT-002: Local-first behavior
- [ ] UT-003: Cost calculation
- [ ] UT-004: Performance scoring

### Integration Tests

- [ ] IT-001: Strategy selection
- [ ] IT-002: Fallback behavior
- [ ] IT-003: Mode compliance
- [ ] IT-004: Multi-target ranking

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