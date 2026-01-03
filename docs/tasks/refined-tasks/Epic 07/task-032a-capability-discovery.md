# Task 032.a: Capability Discovery

**Priority:** P1 – High  
**Tier:** L – Cloud Integration  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 032 (Placement Strategies)  

---

## Description

Task 032.a implements capability discovery for compute targets. Target capabilities MUST be detected automatically. Hardware, software, and resources MUST be probed.

Discovery enables intelligent placement. Targets report their capabilities. Placement matches requirements to capabilities.

Discovery runs on target preparation. Results are cached. Manual override is supported.

### Business Value

Capability discovery enables:
- Automatic target profiling
- Accurate placement decisions
- Reduced misconfiguration
- Dynamic capability updates

### Scope Boundaries

This task covers discovery. Capability matching is in 032.b. Placement engine is in Task 032.

### Integration Points

- Task 032: Provides capabilities to engine
- Task 029.a: Runs during preparation
- Task 030.b: Uses SSH for remote

### Failure Modes

- Discovery timeout → Use defaults
- Probe fails → Mark capability unknown
- Partial discovery → Return partial
- Cache stale → Re-discover

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Discovery | Automatic detection |
| Probe | Detection command |
| Capability | Target feature |
| Profile | Capability snapshot |
| Cache | Stored results |
| Refresh | Re-run discovery |

---

## Out of Scope

- Performance benchmarking
- Stress testing
- Network topology discovery
- Storage performance testing
- Security capability scanning

---

## Functional Requirements

### FR-001 to FR-020: Discovery Engine

- FR-001: `ICapabilityDiscovery` MUST exist
- FR-002: `DiscoverAsync` MUST return capabilities
- FR-003: Discovery MUST run commands
- FR-004: Local discovery MUST work
- FR-005: Remote discovery via SSH MUST work
- FR-006: Discovery MUST be timeout-bound
- FR-007: Default timeout: 60 seconds
- FR-008: Partial results MUST be returned
- FR-009: Unknown capabilities MUST be marked
- FR-010: Discovery MUST be idempotent
- FR-011: Results MUST be cached
- FR-012: Cache key: target ID
- FR-013: Cache TTL MUST be configurable
- FR-014: Default TTL: 5 minutes
- FR-015: Force refresh MUST work
- FR-016: Cache invalidation MUST work
- FR-017: Discovery MUST be parallel
- FR-018: Multiple targets simultaneously
- FR-019: Discovery progress MUST report
- FR-020: Discovery MUST log results

### FR-021 to FR-040: Hardware Discovery

- FR-021: CPU count MUST be discovered
- FR-022: Linux: `/proc/cpuinfo`
- FR-023: Windows: `wmic cpu`
- FR-024: macOS: `sysctl hw.ncpu`
- FR-025: CPU model MUST be discovered
- FR-026: Memory total MUST be discovered
- FR-027: Linux: `/proc/meminfo`
- FR-028: Memory available MUST be discovered
- FR-029: Disk space MUST be discovered
- FR-030: Disk for workspace path
- FR-031: GPU presence MUST be discovered
- FR-032: nvidia-smi for NVIDIA
- FR-033: GPU count MUST be discovered
- FR-034: GPU memory MUST be discovered
- FR-035: GPU model MUST be discovered
- FR-036: GPU driver MUST be discovered
- FR-037: CUDA version MUST be discovered
- FR-038: Network bandwidth MUST be optional
- FR-039: Architecture MUST be discovered
- FR-040: x64, arm64, etc.

### FR-041 to FR-060: Software Discovery

- FR-041: OS MUST be discovered
- FR-042: OS version MUST be discovered
- FR-043: Linux: `cat /etc/os-release`
- FR-044: Windows: `ver`
- FR-045: macOS: `sw_vers`
- FR-046: Shell MUST be discovered
- FR-047: Common tools MUST be probed
- FR-048: Tool list MUST be configurable
- FR-049: Default tools: git, docker, python
- FR-050: Tool version MUST be discovered
- FR-051: `tool --version` pattern
- FR-052: Docker availability MUST check
- FR-053: Docker running MUST check
- FR-054: Kubernetes MUST be optional
- FR-055: Container runtime MUST detect
- FR-056: Package managers MUST detect
- FR-057: apt, yum, brew, choco
- FR-058: Language runtimes MUST detect
- FR-059: python, node, dotnet, java
- FR-060: Compiler availability MUST check

### FR-061 to FR-075: Capability Schema

- FR-061: Capability schema MUST be defined
- FR-062: Schema MUST be versioned
- FR-063: Schema MUST be extensible
- FR-064: Custom capabilities MUST work
- FR-065: Capability types MUST exist
- FR-066: Type: hardware, software, resource
- FR-067: Capability values MUST be typed
- FR-068: Boolean for presence
- FR-069: Integer for quantities
- FR-070: String for versions
- FR-071: List for multiple values
- FR-072: Capabilities MUST serialize
- FR-073: JSON serialization MUST work
- FR-074: YAML serialization MUST work
- FR-075: Manual override MUST merge

---

## Non-Functional Requirements

- NFR-001: Discovery in <30 seconds
- NFR-002: Single probe <5 seconds
- NFR-003: Cache hit <10ms
- NFR-004: Parallel discovery efficient
- NFR-005: No side effects
- NFR-006: Structured logging
- NFR-007: Metrics on discovery
- NFR-008: Cross-platform
- NFR-009: Graceful fallback
- NFR-010: Minimal target impact

---

## User Manual Documentation

### Discovered Capabilities

| Category | Capability | How Discovered |
|----------|------------|----------------|
| Hardware | cpu.count | /proc/cpuinfo |
| Hardware | memory.total | /proc/meminfo |
| Hardware | disk.available | df command |
| Hardware | gpu.present | nvidia-smi |
| Hardware | gpu.vram | nvidia-smi |
| Software | os.name | /etc/os-release |
| Software | docker.available | which docker |
| Software | python.version | python --version |

### Configuration

```yaml
discovery:
  enabled: true
  cacheMinutes: 5
  timeoutSeconds: 60
  tools:
    - git
    - docker
    - python3
    - node
    - dotnet
```

### Manual Override

```yaml
targets:
  my-server:
    host: build-server.example.com
    capabilities:
      gpu.present: true
      gpu.type: nvidia
      gpu.vram: 16
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: CPU discovery works
- [ ] AC-002: Memory discovery works
- [ ] AC-003: Disk discovery works
- [ ] AC-004: GPU discovery works
- [ ] AC-005: OS discovery works
- [ ] AC-006: Tool discovery works
- [ ] AC-007: Cache works
- [ ] AC-008: Timeout handled
- [ ] AC-009: Manual override works
- [ ] AC-010: Cross-platform works

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Probe parsing
- [ ] UT-002: Cache logic
- [ ] UT-003: Schema validation
- [ ] UT-004: Override merging

### Integration Tests

- [ ] IT-001: Local discovery
- [ ] IT-002: Remote discovery
- [ ] IT-003: GPU detection
- [ ] IT-004: Tool detection

---

## Implementation Prompt

### Interface

```csharp
public interface ICapabilityDiscovery
{
    Task<TargetCapabilities> DiscoverAsync(
        IComputeTarget target,
        DiscoveryOptions options = null,
        CancellationToken ct = default);
    
    Task InvalidateCacheAsync(string targetId);
    TargetCapabilities GetCached(string targetId);
}

public record DiscoveryOptions(
    bool ForceRefresh = false,
    TimeSpan? Timeout = null,
    IReadOnlyList<string> ToolsToProbe = null);

public record TargetCapabilities(
    string TargetId,
    DateTime DiscoveredAt,
    HardwareCapabilities Hardware,
    SoftwareCapabilities Software,
    IReadOnlyDictionary<string, object> Custom);

public record HardwareCapabilities(
    int CpuCount,
    string CpuModel,
    long MemoryBytes,
    long DiskBytes,
    string Architecture,
    GpuCapabilities Gpu = null);

public record GpuCapabilities(
    bool Present,
    int Count,
    string Type,
    string Model,
    long VramBytes,
    string DriverVersion,
    string CudaVersion);

public record SoftwareCapabilities(
    string OsName,
    string OsVersion,
    string Shell,
    IReadOnlyDictionary<string, string> ToolVersions);
```

### Probe Registry

```csharp
public interface ICapabilityProbe
{
    string CapabilityName { get; }
    Task<object> ProbeAsync(
        IComputeTarget target,
        CancellationToken ct);
}

public class CpuCountProbe : ICapabilityProbe { }
public class MemoryProbe : ICapabilityProbe { }
public class GpuProbe : ICapabilityProbe { }
public class ToolVersionProbe : ICapabilityProbe { }
```

---

**End of Task 032.a Specification**