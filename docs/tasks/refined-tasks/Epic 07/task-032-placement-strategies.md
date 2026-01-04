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

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Task 029 Compute Interface | IComputeTarget | Available targets | Target registry |
| Task 027 Worker | IWorker | Placement requests | Primary consumer |
| Task 033 Heuristics | IBurstHeuristics | Burst decision input | Advisory |
| Task 025 Task Spec | ITaskSpecification | Requirements declared | Input |
| Capability Discovery | ICapabilityDiscovery | Target capabilities | Matching input |
| Strategy Registry | IStrategyRegistry | Available strategies | Pluggable |
| Cost Service | ICostService | Cost estimates | Optimization |

### Mode Compliance

| Mode | Placement Options |
|------|-------------------|
| local-only | LocalTarget only |
| airgapped | LocalTarget only |
| burst | Any available target |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| No valid target | All targets fail constraints | Return error with reason | Task cannot run |
| Capability discovery fails | Probe timeout | Use cached or configured | Possibly stale data |
| Strategy throws exception | Exception caught | Fallback to auto strategy | Degraded selection |
| Cache stale | TTL expired | Force refresh | Brief delay |
| GPU requirement unsatisfied | No GPU targets | Error with GPU guidance | Configuration needed |
| Cost limit exceeded | Estimate > limit | Block placement | User decision |
| Mode violation attempted | Mode check | Block and log | Enforcement works |
| Target becomes unavailable | Health check | Re-place to alternative | Brief disruption |

---

## Assumptions

1. **Target Registry**: IComputeTarget instances registered in DI container
2. **Capability Format**: Standard capability schema across all target types
3. **Strategy Pluggability**: New strategies registrable via DI
4. **Cost Data**: Cost estimates available for cloud targets
5. **Discovery Commands**: Remote targets support basic probing (uname, nproc, etc.)
6. **Mode Enforcement**: Operating mode available from configuration
7. **Determinism**: Same inputs produce same placement (for testing)
8. **Fallback Chain**: Fallback strategies ordered by reliability

---

## Security Considerations

1. **Capability Probe Safety**: Discovery commands MUST NOT execute untrusted code
2. **Target Authorization**: Placement MUST verify user has access to target
3. **Mode Enforcement**: local-only/airgapped MUST block cloud targets
4. **Cost Limit Privacy**: Cost limits MUST NOT appear in shared logs
5. **Strategy Injection**: Custom strategies MUST be validated
6. **Placement Audit**: All placement decisions MUST be logged
7. **Capability Cache**: Cached capabilities MUST expire
8. **Target Isolation**: Placement MUST NOT leak target details cross-session

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

### Placement Engine

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032-01 | `IPlacementEngine` interface MUST exist | P0 |
| FR-032-02 | `PlaceAsync` MUST return selected target | P0 |
| FR-032-03 | Input MUST include task requirements | P0 |
| FR-032-04 | Input MUST include available targets | P0 |
| FR-032-05 | Output MUST include selected target | P0 |
| FR-032-06 | Output MUST include placement reason | P0 |
| FR-032-07 | No valid target MUST return error with details | P0 |
| FR-032-08 | Multiple strategies MUST be supported | P0 |
| FR-032-09 | Strategy MUST be configurable per-session | P0 |
| FR-032-10 | Default strategy MUST be `auto` | P0 |
| FR-032-11 | Strategies MUST be pluggable via DI | P1 |
| FR-032-12 | `IPlacementStrategy` interface MUST exist | P0 |
| FR-032-13 | Strategy MUST implement `ScoreTargetsAsync` | P0 |
| FR-032-14 | Scoring MUST return score per target | P0 |
| FR-032-15 | Highest score MUST win placement | P0 |
| FR-032-16 | Tie-breaker MUST be defined and consistent | P0 |
| FR-032-17 | Default tie-breaker: first registered target | P1 |
| FR-032-18 | Placement decision MUST be logged | P0 |
| FR-032-19 | Placement MUST emit metrics | P1 |
| FR-032-20 | Placement history MUST be queryable | P2 |

### Task Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032-21 | Task requirements MUST be parsed from spec | P0 |
| FR-032-22 | CPU requirement MUST work (core count) | P0 |
| FR-032-23 | Memory requirement MUST work (GB) | P0 |
| FR-032-24 | Disk requirement MUST work (GB) | P1 |
| FR-032-25 | GPU requirement MUST work (boolean) | P0 |
| FR-032-26 | GPU type MUST be specifiable (nvidia, amd) | P1 |
| FR-032-27 | GPU VRAM requirement MUST work (GB) | P1 |
| FR-032-28 | Tool requirements MUST work (array) | P0 |
| FR-032-29 | Example tools: docker, kubectl, python3 | P0 |
| FR-032-30 | OS requirement MUST work (linux, windows) | P0 |
| FR-032-31 | Architecture requirement MUST work (x64, arm64) | P1 |
| FR-032-32 | Soft requirements MUST work (preferred) | P1 |
| FR-032-33 | Soft requirements influence score, don't exclude | P1 |
| FR-032-34 | Hard requirements MUST work (required) | P0 |
| FR-032-35 | Hard requirements exclude non-matching targets | P0 |
| FR-032-36 | Timeout requirement MUST influence placement | P1 |
| FR-032-37 | Max runtime affects spot vs on-demand | P1 |
| FR-032-38 | Cost limit MUST be a requirement | P1 |
| FR-032-39 | Targets exceeding cost limit excluded | P1 |
| FR-032-40 | Custom requirements MUST be extensible | P2 |

### Target Capabilities

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032-41 | Target capabilities MUST be known | P0 |
| FR-032-42 | Capabilities MUST come from config | P0 |
| FR-032-43 | Capabilities MUST be discoverable via probe | P0 |
| FR-032-44 | Discovery via standard probe commands | P0 |
| FR-032-45 | CPU count MUST be discovered (nproc) | P0 |
| FR-032-46 | Memory size MUST be discovered (/proc/meminfo) | P0 |
| FR-032-47 | Disk space MUST be discovered (df) | P1 |
| FR-032-48 | GPU presence MUST be discovered (nvidia-smi) | P0 |
| FR-032-49 | GPU type and VRAM MUST be discovered | P1 |
| FR-032-50 | Tool availability MUST be probed (which/where) | P0 |
| FR-032-51 | OS MUST be detected (uname) | P0 |
| FR-032-52 | Architecture MUST be detected (uname -m) | P0 |
| FR-032-53 | Capabilities MUST cache | P0 |
| FR-032-54 | Cache TTL MUST be configurable (default 5 min) | P1 |
| FR-032-55 | Capability refresh MUST be triggerable | P1 |
| FR-032-56 | Capability change MUST notify observers | P2 |
| FR-032-57 | Capability schema MUST be well-defined | P0 |
| FR-032-58 | Custom capability types MUST be extensible | P2 |
| FR-032-59 | Capability matching MUST support ranges | P1 |
| FR-032-60 | Matching: >= for numeric, contains for lists | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-032-01 | Placement decision time | <100ms | P0 |
| NFR-032-02 | Max targets supported | 100 | P1 |
| NFR-032-03 | Capability discovery time | <5s per target | P0 |
| NFR-032-04 | Cache hit rate | >90% | P1 |
| NFR-032-05 | Strategy scoring time | <50ms for 100 targets | P1 |
| NFR-032-06 | Requirement parsing time | <10ms | P1 |
| NFR-032-07 | Memory per cached capability | <1KB | P2 |
| NFR-032-08 | Placement throughput | 100/second | P2 |
| NFR-032-09 | Discovery parallelization | 10 concurrent | P1 |
| NFR-032-10 | Fallback strategy time | <50ms | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-032-11 | No placement resource leaks | Zero leaks | P0 |
| NFR-032-12 | Deterministic with same inputs | 100% consistent | P0 |
| NFR-032-13 | Graceful degradation on errors | Fallback works | P0 |
| NFR-032-14 | Strategy exception isolation | Caught and logged | P0 |
| NFR-032-15 | Cache recovery on corruption | Auto-rebuild | P1 |
| NFR-032-16 | Mode enforcement reliability | 100% enforced | P0 |
| NFR-032-17 | Hard requirement enforcement | Never violated | P0 |
| NFR-032-18 | Discovery retry on failure | 3 retries | P1 |
| NFR-032-19 | Placement idempotency | Same result | P0 |
| NFR-032-20 | Fallback chain completeness | Always terminates | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-032-21 | Structured logging for all placements | JSON format | P0 |
| NFR-032-22 | Metrics for placement count | Counter by strategy | P1 |
| NFR-032-23 | Metrics for placement latency | Histogram | P1 |
| NFR-032-24 | Audit trail for decisions | Full history | P0 |
| NFR-032-25 | Cache statistics | Hit/miss counters | P1 |
| NFR-032-26 | Discovery failure logging | Per-target | P1 |
| NFR-032-27 | Score breakdown logging | Debug level | P2 |
| NFR-032-28 | Mode violation logging | Warning level | P0 |
| NFR-032-29 | Requirement match logging | Info level | P1 |
| NFR-032-30 | Strategy selection logging | Info level | P0 |

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

### Placement Engine
- [ ] AC-001: `IPlacementEngine` interface exists
- [ ] AC-002: `PlaceAsync` returns selected target
- [ ] AC-003: Task requirements parsed correctly
- [ ] AC-004: Available targets considered
- [ ] AC-005: Selected target returned
- [ ] AC-006: Placement reason included
- [ ] AC-007: No valid target returns error
- [ ] AC-008: Multiple strategies supported
- [ ] AC-009: Strategy configurable per-session
- [ ] AC-010: Default strategy is `auto`
- [ ] AC-011: Strategies pluggable via DI
- [ ] AC-012: Strategy interface works
- [ ] AC-013: Scoring returns per-target scores
- [ ] AC-014: Highest score wins
- [ ] AC-015: Tie-breaker is consistent

### Task Requirements
- [ ] AC-016: CPU requirement works
- [ ] AC-017: Memory requirement works
- [ ] AC-018: Disk requirement works
- [ ] AC-019: GPU requirement works
- [ ] AC-020: GPU type specifiable
- [ ] AC-021: Tool requirements work
- [ ] AC-022: OS requirement works
- [ ] AC-023: Arch requirement works
- [ ] AC-024: Soft requirements influence score
- [ ] AC-025: Hard requirements exclude targets
- [ ] AC-026: Timeout requirement considered
- [ ] AC-027: Cost limit excludes expensive targets

### Target Capabilities
- [ ] AC-028: Capabilities from config work
- [ ] AC-029: Capability discovery works
- [ ] AC-030: CPU count discovered
- [ ] AC-031: Memory size discovered
- [ ] AC-032: Disk space discovered
- [ ] AC-033: GPU presence discovered
- [ ] AC-034: Tool availability probed
- [ ] AC-035: OS detected
- [ ] AC-036: Arch detected
- [ ] AC-037: Capabilities cached
- [ ] AC-038: Cache TTL respected
- [ ] AC-039: Capability refresh works

### Mode Compliance
- [ ] AC-040: local-only blocks cloud targets
- [ ] AC-041: airgapped blocks cloud targets
- [ ] AC-042: burst allows all targets
- [ ] AC-043: Mode violation logged

### Observability
- [ ] AC-044: Placement decisions logged
- [ ] AC-045: Metrics emitted
- [ ] AC-046: Placement history queryable
- [ ] AC-047: Cache statistics available

### Performance
- [ ] AC-048: Placement decision <100ms
- [ ] AC-049: 100 targets supported
- [ ] AC-050: Capability discovery <5s
- [ ] AC-051: Deterministic results
- [ ] AC-052: Graceful degradation on errors

---

## User Verification Scenarios

### Scenario 1: GPU Task Placed on GPU Target
**Persona:** ML developer running training  
**Preconditions:** GPU and non-GPU targets available  
**Steps:**
1. Submit task requiring GPU
2. Observe placement decision
3. Verify GPU target selected
4. Check placement reason

**Verification Checklist:**
- [ ] GPU requirement parsed
- [ ] Non-GPU targets excluded
- [ ] GPU target selected
- [ ] Reason shows "GPU requirement satisfied"

### Scenario 2: Local-Only Mode Enforced
**Persona:** Developer in airgapped environment  
**Preconditions:** Mode set to local-only, cloud target available  
**Steps:**
1. Submit task with high CPU requirement
2. Local target insufficient
3. Observe cloud target blocked
4. Error returned

**Verification Checklist:**
- [ ] Cloud target not considered
- [ ] Mode violation logged
- [ ] Clear error message
- [ ] Guidance to change mode if needed

### Scenario 3: Cost-Optimized Placement
**Persona:** Cost-conscious developer  
**Preconditions:** Multiple cloud targets with different costs  
**Steps:**
1. Set strategy to cost-optimized
2. Submit task
3. Observe cheapest target selected
4. Check cost in placement reason

**Verification Checklist:**
- [ ] All targets scored
- [ ] Cheapest target wins
- [ ] Cost shown in reason
- [ ] More expensive alternatives noted

### Scenario 4: Tool Requirement Matching
**Persona:** Developer needing Docker  
**Preconditions:** Some targets have Docker, some don't  
**Steps:**
1. Submit task requiring docker
2. Observe discovery probes
3. Verify Docker target selected
4. Check tool availability in capabilities

**Verification Checklist:**
- [ ] Tool requirement parsed
- [ ] Discovery probes `which docker`
- [ ] Targets without Docker excluded
- [ ] Docker target selected

### Scenario 5: Capability Caching
**Persona:** Developer running multiple tasks  
**Preconditions:** Discovery already run once  
**Steps:**
1. Submit first task (discovery runs)
2. Submit second task within 5 minutes
3. Observe cache hit
4. Verify faster placement

**Verification Checklist:**
- [ ] First task triggers discovery
- [ ] Second task uses cache
- [ ] Cache hit logged
- [ ] Placement faster on second task

### Scenario 6: Fallback on Strategy Failure
**Persona:** Developer with custom strategy  
**Preconditions:** Custom strategy throws exception  
**Steps:**
1. Register faulty strategy
2. Submit task
3. Observe fallback to auto
4. Placement succeeds

**Verification Checklist:**
- [ ] Exception caught
- [ ] Fallback to auto logged
- [ ] Placement completes
- [ ] Task runs successfully

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-032-01 | Requirement parsing for CPU | FR-032-22 |
| UT-032-02 | Requirement parsing for GPU | FR-032-25 |
| UT-032-03 | Capability matching numeric (>=) | FR-032-60 |
| UT-032-04 | Capability matching list (contains) | FR-032-60 |
| UT-032-05 | Strategy scoring logic | FR-032-14 |
| UT-032-06 | Tie-breaker consistency | FR-032-16 |
| UT-032-07 | Mode validation local-only | Mode table |
| UT-032-08 | Mode validation burst | Mode table |
| UT-032-09 | Hard requirement exclusion | FR-032-35 |
| UT-032-10 | Soft requirement scoring | FR-032-33 |
| UT-032-11 | Cost limit exclusion | FR-032-39 |
| UT-032-12 | Cache TTL expiry | FR-032-54 |
| UT-032-13 | No valid target error | FR-032-07 |
| UT-032-14 | Placement reason generation | FR-032-06 |
| UT-032-15 | Deterministic placement | NFR-032-12 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-032-01 | Real placement with local target | E2E |
| IT-032-02 | Capability discovery via SSH | FR-032-43 |
| IT-032-03 | Multi-target scoring | FR-032-14 |
| IT-032-04 | Fallback on strategy failure | NFR-032-13 |
| IT-032-05 | GPU target selection | FR-032-25 |
| IT-032-06 | Tool availability discovery | FR-032-50 |
| IT-032-07 | Cost-optimized strategy | Strategy table |
| IT-032-08 | Local-first strategy | Strategy table |
| IT-032-09 | Capability caching | FR-032-53 |
| IT-032-10 | Cache refresh | FR-032-55 |
| IT-032-11 | Mode enforcement | Mode table |
| IT-032-12 | Placement metrics | NFR-032-22 |
| IT-032-13 | 100 target performance | NFR-032-02 |
| IT-032-14 | Discovery parallelization | NFR-032-09 |
| IT-032-15 | Placement history query | FR-032-20 |

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