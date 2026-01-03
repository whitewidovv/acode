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

### Interface

```csharp
public interface IPlacementEngine
{
    Task<PlacementResult> PlaceAsync(
        TaskRequirements requirements,
        IReadOnlyList<IComputeTarget> targets,
        PlacementOptions options = null,
        CancellationToken ct = default);
}

public record TaskRequirements(
    int? MinCpu = null,
    int? MinMemoryGb = null,
    int? MinDiskGb = null,
    GpuRequirement? Gpu = null,
    IReadOnlyList<string> RequiredTools = null,
    string RequiredOs = null,
    string RequiredArch = null,
    TimeSpan? MaxRuntime = null,
    decimal? MaxCost = null,
    IReadOnlyDictionary<string, string> CustomRequirements = null);

public record GpuRequirement(
    bool Required,
    string Type = null,
    int? MinVramGb = null,
    int? MinCount = null);

public record PlacementResult(
    bool Success,
    IComputeTarget SelectedTarget,
    PlacementReason Reason,
    IReadOnlyList<TargetScore> AllScores);

public record TargetScore(
    IComputeTarget Target,
    double Score,
    IReadOnlyList<string> Matches,
    IReadOnlyList<string> Mismatches);

public enum PlacementReason
{
    OnlyOption,
    HighestScore,
    CostOptimal,
    LocalPreferred,
    CapabilityMatch,
    NoValidTarget
}
```

### Strategy Interface

```csharp
public interface IPlacementStrategy
{
    string Name { get; }
    Task<double> ScoreAsync(
        IComputeTarget target,
        TaskRequirements requirements,
        TargetCapabilities capabilities,
        CancellationToken ct);
}
```

---

**End of Task 032 Specification**