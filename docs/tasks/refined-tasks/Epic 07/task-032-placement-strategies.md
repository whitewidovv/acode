# Task 032: Inference/Execution Placement Strategies

**Priority:** P1 – High  
**Tier:** L – Cloud Integration  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 029 (Interface), Task 030 (SSH), Task 031 (EC2)  

---

## Description

Task 032 implements placement strategies for task execution. Tasks MUST be assigned to appropriate compute targets. Placement MUST consider constraints, capabilities, and cost.

Placement decides where to run tasks. Local execution is default. Cloud burst when local is insufficient. GPU tasks need GPU targets.

This task provides the placement decision engine. Subtasks cover specific strategies and optimization.

### Business Value

Placement strategies enable:
- Optimal resource utilization
- Cost-efficient execution
- Capability matching
- Performance optimization

### Scope Boundaries

This task covers placement decisions. Compute targets are in Tasks 029-031. Burst heuristics are in Task 033.

### Integration Points

- Task 029: Available targets
- Task 027: Worker requests placement
- Task 033: Heuristics inform placement
- Task 025: Task spec declares requirements

### Mode Compliance

| Mode | Placement Options |
|------|-------------------|
| local-only | LocalTarget only |
| airgapped | LocalTarget only |
| burst | Any available target |

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Placement | Target selection |
| Strategy | Selection algorithm |
| Capability | Target feature |
| Constraint | Task requirement |
| Affinity | Preference for target |
| Anti-affinity | Avoid target |

---

## Out of Scope

- Kubernetes scheduling
- Service mesh routing
- Load balancer integration
- Geographic placement
- Multi-region optimization

---

## Functional Requirements

### FR-001 to FR-020: Placement Engine

- FR-001: `IPlacementEngine` MUST exist
- FR-002: `PlaceAsync` MUST return target
- FR-003: Input: task requirements
- FR-004: Input: available targets
- FR-005: Output: selected target
- FR-006: Output: placement reason
- FR-007: No valid target MUST error
- FR-008: Multiple strategies MUST work
- FR-009: Strategy MUST be configurable
- FR-010: Default strategy: auto
- FR-011: Strategies MUST be pluggable
- FR-012: Strategy interface MUST exist
- FR-013: `IPlacementStrategy`
- FR-014: Strategy MUST score targets
- FR-015: Highest score wins
- FR-016: Tie-breaker MUST be defined
- FR-017: Default tie-breaker: first
- FR-018: Placement MUST be logged
- FR-019: Placement MUST emit metrics
- FR-020: Placement history MUST be tracked

### FR-021 to FR-040: Task Requirements

- FR-021: Task requirements MUST be parsed
- FR-022: CPU requirement MUST work
- FR-023: Memory requirement MUST work
- FR-024: Disk requirement MUST work
- FR-025: GPU requirement MUST work
- FR-026: GPU type MUST be specifiable
- FR-027: Network requirement MUST work
- FR-028: Tool requirements MUST work
- FR-029: Example: docker, kubectl
- FR-030: OS requirement MUST work
- FR-031: Example: linux, windows
- FR-032: Arch requirement MUST work
- FR-033: Example: x64, arm64
- FR-034: Soft requirements MUST work
- FR-035: Soft: prefer but not required
- FR-036: Hard requirements MUST work
- FR-037: Hard: must satisfy or fail
- FR-038: Timeout requirement MUST work
- FR-039: Max runtime influences target
- FR-040: Cost limit MUST be requirement

### FR-041 to FR-060: Target Capabilities

- FR-041: Target capabilities MUST be known
- FR-042: Capabilities from config
- FR-043: Capabilities from discovery
- FR-044: Discovery via probe command
- FR-045: CPU count MUST be discovered
- FR-046: Memory size MUST be discovered
- FR-047: Disk space MUST be discovered
- FR-048: GPU presence MUST be discovered
- FR-049: GPU type MUST be discovered
- FR-050: Tool availability MUST be probed
- FR-051: OS MUST be detected
- FR-052: Arch MUST be detected
- FR-053: Capabilities MUST cache
- FR-054: Cache TTL: 5 minutes
- FR-055: Capability refresh MUST work
- FR-056: Capability change MUST notify
- FR-057: Capability schema MUST be defined
- FR-058: Extensible capability types
- FR-059: Custom capabilities MUST work
- FR-060: Capability matching MUST be flexible

---

## Non-Functional Requirements

- NFR-001: Placement decision <100ms
- NFR-002: 100 targets supported
- NFR-003: Capability discovery <5s
- NFR-004: Cache hit rate >90%
- NFR-005: No placement leaks
- NFR-006: Structured logging
- NFR-007: Metrics on placements
- NFR-008: Audit trail
- NFR-009: Deterministic with same input
- NFR-010: Graceful degradation

---

## User Manual Documentation

### Configuration

```yaml
placement:
  strategy: auto  # auto | local-first | cloud-first | cost-optimized
  fallbackEnabled: true
  discoveryEnabled: true
  cacheMinutes: 5
```

### Task Requirements Example

```yaml
task:
  name: build-ml-model
  requirements:
    cpu: 4
    memoryGb: 16
    gpu:
      required: true
      type: nvidia
      minVram: 8
    tools:
      - python3
      - nvidia-smi
    os: linux
    maxRuntimeMinutes: 60
    maxCost: 5.00
```

### Placement Strategies

| Strategy | Description |
|----------|-------------|
| auto | Balance local and cloud |
| local-first | Prefer local, burst if needed |
| cloud-first | Prefer cloud for isolation |
| cost-optimized | Minimize cost |
| performance | Minimize latency |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Placement engine works
- [ ] AC-002: Requirements parsed
- [ ] AC-003: Capabilities discovered
- [ ] AC-004: Strategy selects target
- [ ] AC-005: Mode compliance enforced
- [ ] AC-006: Fallback works
- [ ] AC-007: GPU placement works
- [ ] AC-008: Cost limit works
- [ ] AC-009: Metrics emitted
- [ ] AC-010: Logging complete

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Requirement parsing
- [ ] UT-002: Capability matching
- [ ] UT-003: Strategy scoring
- [ ] UT-004: Mode validation

### Integration Tests

- [ ] IT-001: Real placement
- [ ] IT-002: Capability discovery
- [ ] IT-003: Multi-target selection
- [ ] IT-004: Fallback behavior

---

## Implementation Prompt

### Part 1: File Structure and Domain Models

**Target Directory Structure:**
```
src/
├── Acode.Domain/
│   └── Compute/
│       └── Placement/
│           ├── PlacementReason.cs
│           ├── TargetScore.cs
│           ├── GpuRequirement.cs
│           └── Events/
│               ├── PlacementStartedEvent.cs
│               ├── PlacementCompletedEvent.cs
│               └── PlacementFailedEvent.cs
├── Acode.Application/
│   └── Compute/
│       └── Placement/
│           ├── IPlacementEngine.cs
│           ├── IPlacementStrategy.cs
│           ├── TaskRequirements.cs
│           ├── PlacementResult.cs
│           └── PlacementOptions.cs
└── Acode.Infrastructure/
    └── Compute/
        └── Placement/
            ├── PlacementEngine.cs
            ├── Strategies/
            │   ├── AutoStrategy.cs
            │   ├── LocalFirstStrategy.cs
            │   ├── CloudFirstStrategy.cs
            │   ├── CostOptimizedStrategy.cs
            │   └── PerformanceStrategy.cs
            └── Matching/
                ├── RequirementMatcher.cs
                └── CapabilityComparer.cs
```

**Domain Models:**

```csharp
// src/Acode.Domain/Compute/Placement/PlacementReason.cs
namespace Acode.Domain.Compute.Placement;

public enum PlacementReason
{
    OnlyOption,
    HighestScore,
    CostOptimal,
    LocalPreferred,
    CapabilityMatch,
    ModeRestricted,
    NoValidTarget
}

// src/Acode.Domain/Compute/Placement/TargetScore.cs
namespace Acode.Domain.Compute.Placement;

public sealed record TargetScore
{
    public required string TargetId { get; init; }
    public required string TargetType { get; init; }
    public required double Score { get; init; }
    public IReadOnlyList<string> Matches { get; init; } = [];
    public IReadOnlyList<string> Mismatches { get; init; } = [];
    public bool IsEligible => Mismatches.Count == 0;
}

// src/Acode.Domain/Compute/Placement/GpuRequirement.cs
namespace Acode.Domain.Compute.Placement;

public sealed record GpuRequirement
{
    public bool Required { get; init; }
    public string? Type { get; init; } // nvidia, amd
    public int? MinVramGb { get; init; }
    public int? MinCount { get; init; } = 1;
}

// src/Acode.Domain/Compute/Placement/Events/PlacementCompletedEvent.cs
namespace Acode.Domain.Compute.Placement.Events;

public sealed record PlacementCompletedEvent(
    string RequestId,
    string SelectedTargetId,
    string SelectedTargetType,
    PlacementReason Reason,
    double Score,
    int CandidatesEvaluated,
    TimeSpan Duration,
    DateTimeOffset CompletedAt);

// src/Acode.Domain/Compute/Placement/Events/PlacementFailedEvent.cs
namespace Acode.Domain.Compute.Placement.Events;

public sealed record PlacementFailedEvent(
    string RequestId,
    string FailureReason,
    int CandidatesEvaluated,
    IReadOnlyList<string> RequirementsNotMet,
    DateTimeOffset FailedAt);
```

**End of Task 032 Specification - Part 1/3**

### Part 2: Application Interfaces

```csharp
// src/Acode.Application/Compute/Placement/TaskRequirements.cs
namespace Acode.Application.Compute.Placement;

public sealed record TaskRequirements
{
    public int? MinCpu { get; init; }
    public int? MinMemoryGb { get; init; }
    public int? MinDiskGb { get; init; }
    public GpuRequirement? Gpu { get; init; }
    public IReadOnlyList<string> RequiredTools { get; init; } = [];
    public string? RequiredOs { get; init; }
    public string? RequiredArch { get; init; }
    public TimeSpan? MaxRuntime { get; init; }
    public decimal? MaxCost { get; init; }
    public IReadOnlyDictionary<string, string> CustomRequirements { get; init; } = new Dictionary<string, string>();
    public bool AllowCloud { get; init; } = true;
}

// src/Acode.Application/Compute/Placement/PlacementResult.cs
namespace Acode.Application.Compute.Placement;

public sealed record PlacementResult
{
    public bool Success { get; init; }
    public IComputeTarget? SelectedTarget { get; init; }
    public PlacementReason Reason { get; init; }
    public IReadOnlyList<TargetScore> AllScores { get; init; } = [];
    public string? FailureMessage { get; init; }
    public TimeSpan DecisionDuration { get; init; }
}

// src/Acode.Application/Compute/Placement/PlacementOptions.cs
namespace Acode.Application.Compute.Placement;

public sealed record PlacementOptions
{
    public string Strategy { get; init; } = "auto";
    public bool FallbackEnabled { get; init; } = true;
    public bool IncludeLocalTarget { get; init; } = true;
    public bool IncludeCloudTargets { get; init; } = true;
    public int MaxCandidates { get; init; } = 10;
    public TimeSpan DecisionTimeout { get; init; } = TimeSpan.FromMilliseconds(100);
}

// src/Acode.Application/Compute/Placement/IPlacementEngine.cs
namespace Acode.Application.Compute.Placement;

public interface IPlacementEngine
{
    Task<PlacementResult> PlaceAsync(
        TaskRequirements requirements,
        IReadOnlyList<IComputeTarget> targets,
        PlacementOptions? options = null,
        CancellationToken ct = default);
    
    Task<IReadOnlyList<TargetScore>> EvaluateAllAsync(
        TaskRequirements requirements,
        IReadOnlyList<IComputeTarget> targets,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Placement/IPlacementStrategy.cs
namespace Acode.Application.Compute.Placement;

public interface IPlacementStrategy
{
    string Name { get; }
    
    Task<double> ScoreAsync(
        IComputeTarget target,
        TaskRequirements requirements,
        TargetCapabilities capabilities,
        CancellationToken ct = default);
    
    bool CanHandle(TaskRequirements requirements);
}

// src/Acode.Application/Compute/Placement/TargetCapabilities.cs
namespace Acode.Application.Compute.Placement;

public sealed record TargetCapabilities
{
    public required string TargetId { get; init; }
    public int CpuCount { get; init; }
    public int MemoryGb { get; init; }
    public int DiskGb { get; init; }
    public bool HasGpu { get; init; }
    public string? GpuType { get; init; }
    public int? GpuVramGb { get; init; }
    public int GpuCount { get; init; }
    public string Os { get; init; } = "linux";
    public string Arch { get; init; } = "x64";
    public IReadOnlyList<string> AvailableTools { get; init; } = [];
    public decimal? HourlyRate { get; init; }
    public bool IsLocal { get; init; }
    public DateTimeOffset DiscoveredAt { get; init; }
}
```

**End of Task 032 Specification - Part 2/3**

### Part 3: Infrastructure Implementation + Checklist

```csharp
// src/Acode.Infrastructure/Compute/Placement/PlacementEngine.cs
namespace Acode.Infrastructure.Compute.Placement;

public sealed class PlacementEngine : IPlacementEngine
{
    private readonly IReadOnlyList<IPlacementStrategy> _strategies;
    private readonly ICapabilityDiscovery _discovery;
    private readonly IEventPublisher _events;
    private readonly ILogger<PlacementEngine> _logger;
    
    public PlacementEngine(
        IEnumerable<IPlacementStrategy> strategies,
        ICapabilityDiscovery discovery,
        IEventPublisher events,
        ILogger<PlacementEngine> logger)
    {
        _strategies = strategies.ToList();
        _discovery = discovery;
        _events = events;
        _logger = logger;
    }
    
    public async Task<PlacementResult> PlaceAsync(
        TaskRequirements requirements,
        IReadOnlyList<IComputeTarget> targets,
        PlacementOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new PlacementOptions();
        var sw = Stopwatch.StartNew();
        
        var strategy = SelectStrategy(options.Strategy, requirements);
        var scores = await EvaluateWithStrategyAsync(
            strategy, requirements, targets, ct);
        
        var best = scores
            .Where(s => s.Score > 0)
            .OrderByDescending(s => s.Score)
            .FirstOrDefault();
        
        var result = best != null
            ? new PlacementResult
            {
                Success = true,
                SelectedTarget = best.Target,
                Reason = PlacementReason.StrategyMatch,
                AllScores = scores,
                DecisionDuration = sw.Elapsed
            }
            : new PlacementResult
            {
                Success = false,
                Reason = PlacementReason.NoViableTarget,
                AllScores = scores,
                FailureMessage = "No target met minimum requirements",
                DecisionDuration = sw.Elapsed
            };
        
        await PublishEventAsync(result, requirements, ct);
        return result;
    }
    
    private IPlacementStrategy SelectStrategy(string name, TaskRequirements req)
    {
        return _strategies.FirstOrDefault(s => 
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && s.CanHandle(req))
            ?? _strategies.First(s => s.Name == "auto");
    }
}

// src/Acode.Infrastructure/Compute/Placement/Strategies/AutoStrategy.cs
namespace Acode.Infrastructure.Compute.Placement.Strategies;

public sealed class AutoStrategy : IPlacementStrategy
{
    public string Name => "auto";
    
    public Task<double> ScoreAsync(
        IComputeTarget target,
        TaskRequirements requirements,
        TargetCapabilities capabilities,
        CancellationToken ct)
    {
        if (!MeetsMinimums(requirements, capabilities))
            return Task.FromResult(0.0);
        
        var score = capabilities.IsLocal ? 0.8 : 0.5; // Prefer local
        score += CalculateResourceScore(requirements, capabilities);
        return Task.FromResult(Math.Min(score, 1.0));
    }
    
    public bool CanHandle(TaskRequirements requirements) => true;
    
    private static bool MeetsMinimums(TaskRequirements req, TargetCapabilities cap)
    {
        if (req.MinCpu.HasValue && cap.CpuCount < req.MinCpu) return false;
        if (req.MinMemoryGb.HasValue && cap.MemoryGb < req.MinMemoryGb) return false;
        if (req.Gpu != null && !cap.HasGpu) return false;
        return true;
    }
}

// Additional strategies: LocalFirstStrategy, CloudFirstStrategy, CostOptimizedStrategy, PerformanceStrategy
// Follow same pattern with different scoring weights
```

### Implementation Checklist

| Step | Action | Verification |
|------|--------|--------------|
| 1 | Create Domain models (PlacementReason, TargetScore, GpuRequirement) | Unit tests pass |
| 2 | Add events (PlacementCompletedEvent, PlacementFailedEvent) | Event serialization verified |
| 3 | Define TaskRequirements, PlacementResult, PlacementOptions | Records compile |
| 4 | Create IPlacementEngine, IPlacementStrategy interfaces | Interface contracts documented |
| 5 | Implement PlacementEngine | Integration tests with mock targets pass |
| 6 | Implement AutoStrategy | Default scoring verified |
| 7 | Implement LocalFirstStrategy | Local targets scored higher |
| 8 | Implement CloudFirstStrategy | Cloud targets scored higher |
| 9 | Implement CostOptimizedStrategy | Cheapest viable selected |
| 10 | Implement PerformanceStrategy | Highest-spec selected |
| 11 | Register strategies in DI | All 5 strategies resolved |
| 12 | Add mode compliance checks | local-only rejects cloud |

### Rollout Plan

1. **Phase 1**: Implement domain models and interfaces
2. **Phase 2**: Build PlacementEngine with AutoStrategy only
3. **Phase 3**: Add remaining 4 strategies
4. **Phase 4**: Integration tests with local + SSH targets
5. **Phase 5**: Performance benchmark (<5ms decision time)

**End of Task 032 Specification**
        CancellationToken ct);
}
```

---

**End of Task 032 Specification**