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

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Task 032 Placement Engine | ICapabilityDiscovery | Caps → Engine | Provides target capabilities |
| Task 029.a Target Prep | OnPrepare hook | Trigger → Discovery | Discovery on first use |
| Task 030.b SSH Execution | ICommandExecutor | Commands → SSH | Remote probe execution |
| Task 030.c File Transfer | IFileTransfer | Scripts → Target | Optional probe scripts |
| agent-config.yml | Config parser | Manual caps → Merge | Override discovered |
| Capability Cache | CapabilityCache | Caps ↔ Cache | TTL-based storage |
| Metrics System | IMetrics | Stats → Metrics | Discovery telemetry |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Discovery timeout | Task timeout | Return partial results | Degraded placement accuracy |
| Probe command fails | Non-zero exit | Mark capability unknown | Missing capability info |
| SSH connection lost | Connection exception | Retry once, then fail | Discovery fails for target |
| nvidia-smi not found | Command not found | GPU marked absent | No GPU capability |
| Parsing error | Format exception | Log and skip probe | Single capability missing |
| Cache corruption | Deserialize fail | Invalidate cache | Re-discovery triggered |
| Concurrent discovery | Race condition | Single-flight pattern | Only one discovery runs |
| Target unreachable | Connect timeout | Mark target unavailable | Target excluded from placement |

---

## Assumptions

1. SSH connection to remote targets is already established before discovery
2. Target systems have standard commands available (nproc, df, free, etc.)
3. GPU discovery only supports NVIDIA GPUs via nvidia-smi initially
4. Tool version commands follow `--version` convention
5. Manual capability overrides take precedence over discovered values
6. Discovery runs synchronously during target preparation
7. Cache invalidation happens on target reconnection
8. Probe commands complete within individual timeout limits

---

## Security Considerations

1. Probe commands MUST NOT require elevated privileges by default
2. Command output MUST be sanitized before parsing
3. Discovered capabilities MUST NOT leak to unauthorized users
4. SSH commands for discovery MUST be read-only (no side effects)
5. nvidia-smi output MUST be validated before parsing
6. Cache storage MUST NOT include sensitive capability data
7. Discovery failures MUST NOT expose internal system paths
8. Custom probe scripts MUST be validated before execution

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

### Discovery Engine

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032A-01 | `ICapabilityDiscovery` interface MUST exist | P0 |
| FR-032A-02 | `DiscoverAsync` MUST return `TargetCapabilities` | P0 |
| FR-032A-03 | Discovery MUST execute probe commands on target | P0 |
| FR-032A-04 | Local target discovery via process execution | P0 |
| FR-032A-05 | Remote target discovery via SSH commands | P0 |
| FR-032A-06 | Overall discovery MUST be timeout-bound | P0 |
| FR-032A-07 | Default discovery timeout: 60 seconds | P1 |
| FR-032A-08 | Partial results MUST be returned on timeout | P1 |
| FR-032A-09 | Unknown capabilities MUST be explicitly marked | P1 |
| FR-032A-10 | Discovery MUST be idempotent (same result) | P0 |
| FR-032A-11 | Discovery results MUST be cached | P0 |
| FR-032A-12 | Cache key MUST include target ID | P0 |
| FR-032A-13 | Cache TTL MUST be configurable | P1 |
| FR-032A-14 | Default cache TTL: 5 minutes | P1 |
| FR-032A-15 | Force refresh option MUST bypass cache | P1 |
| FR-032A-16 | Explicit cache invalidation MUST work | P1 |
| FR-032A-17 | Multiple probes MUST run in parallel | P1 |
| FR-032A-18 | Parallel discovery of multiple targets | P2 |
| FR-032A-19 | Discovery progress MUST be reportable | P2 |
| FR-032A-20 | Discovery completion MUST log all results | P1 |

### Hardware Discovery

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032A-21 | CPU count MUST be discovered | P0 |
| FR-032A-22 | Linux CPU: parse `/proc/cpuinfo` | P0 |
| FR-032A-23 | Windows CPU: use `wmic cpu` command | P0 |
| FR-032A-24 | macOS CPU: use `sysctl hw.ncpu` | P1 |
| FR-032A-25 | CPU model/name MUST be discovered | P2 |
| FR-032A-26 | Total memory MUST be discovered | P0 |
| FR-032A-27 | Linux memory: parse `/proc/meminfo` | P0 |
| FR-032A-28 | Available memory MUST be discovered | P1 |
| FR-032A-29 | Disk space for workspace MUST be discovered | P0 |
| FR-032A-30 | Disk probe MUST target workspace mount point | P0 |
| FR-032A-31 | GPU presence MUST be detected | P1 |
| FR-032A-32 | NVIDIA GPU: parse nvidia-smi output | P1 |
| FR-032A-33 | GPU count MUST be discovered | P1 |
| FR-032A-34 | GPU VRAM MUST be discovered | P1 |
| FR-032A-35 | GPU model name MUST be discovered | P2 |
| FR-032A-36 | GPU driver version MUST be discovered | P2 |
| FR-032A-37 | CUDA version MUST be discovered if available | P2 |
| FR-032A-38 | Network bandwidth MAY be discovered (optional) | P3 |
| FR-032A-39 | CPU architecture MUST be discovered | P0 |
| FR-032A-40 | Architecture: x64, arm64, x86, etc. | P0 |

### Software Discovery

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032A-41 | Operating system name MUST be discovered | P0 |
| FR-032A-42 | OS version MUST be discovered | P0 |
| FR-032A-43 | Linux: parse `/etc/os-release` | P0 |
| FR-032A-44 | Windows: use `ver` command | P0 |
| FR-032A-45 | macOS: use `sw_vers` command | P1 |
| FR-032A-46 | Default shell MUST be discovered | P2 |
| FR-032A-47 | Common tool availability MUST be probed | P1 |
| FR-032A-48 | Tool probe list MUST be configurable | P1 |
| FR-032A-49 | Default tools: git, docker, python | P1 |
| FR-032A-50 | Tool version MUST be discovered | P1 |
| FR-032A-51 | Version via `tool --version` pattern | P1 |
| FR-032A-52 | Docker availability MUST be checked | P1 |
| FR-032A-53 | Docker daemon running state MUST be checked | P1 |
| FR-032A-54 | Kubernetes availability MAY be checked | P2 |
| FR-032A-55 | Container runtime MUST be detected | P1 |
| FR-032A-56 | Package manager MUST be detected | P2 |
| FR-032A-57 | Detect: apt, yum, brew, choco | P2 |
| FR-032A-58 | Language runtimes MUST be detected | P1 |
| FR-032A-59 | Detect: python, node, dotnet, java | P1 |
| FR-032A-60 | Compiler availability MUST be checked | P2 |

### Capability Schema

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032A-61 | Capability schema MUST be formally defined | P0 |
| FR-032A-62 | Schema MUST be versioned | P1 |
| FR-032A-63 | Schema MUST be extensible for custom caps | P1 |
| FR-032A-64 | Custom capabilities MUST be supported | P1 |
| FR-032A-65 | Capability type enum MUST exist | P0 |
| FR-032A-66 | Types: hardware, software, resource, custom | P0 |
| FR-032A-67 | Capability values MUST be strongly typed | P0 |
| FR-032A-68 | Boolean type for presence flags | P0 |
| FR-032A-69 | Integer type for quantities (CPU, memory) | P0 |
| FR-032A-70 | String type for versions and names | P0 |
| FR-032A-71 | List type for multiple values | P1 |
| FR-032A-72 | Capabilities MUST serialize to disk | P1 |
| FR-032A-73 | JSON serialization MUST work | P0 |
| FR-032A-74 | YAML serialization MUST work | P1 |
| FR-032A-75 | Manual overrides MUST merge with discovered | P1 |

---

## Non-Functional Requirements

### Performance

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-032A-01 | Full discovery time | <30 seconds | P0 |
| NFR-032A-02 | Single probe execution | <5 seconds | P0 |
| NFR-032A-03 | Cache hit retrieval | <10ms | P0 |
| NFR-032A-04 | Parallel probe efficiency | Linear scaling | P1 |
| NFR-032A-05 | Memory for 100 target cache | <50MB | P1 |
| NFR-032A-06 | Discovery startup time | <100ms | P1 |
| NFR-032A-07 | Cache lookup complexity | O(1) | P0 |
| NFR-032A-08 | Probe parallelism | 10 concurrent | P1 |
| NFR-032A-09 | Multi-target discovery | 5 targets parallel | P2 |
| NFR-032A-10 | Cache serialization | <500ms for 100 entries | P2 |

### Reliability

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-032A-11 | Probe failure isolation | No cascade | P0 |
| NFR-032A-12 | Partial results on timeout | Always | P0 |
| NFR-032A-13 | Discovery success rate | >95% on valid targets | P1 |
| NFR-032A-14 | Cache persistence | Survive restart | P2 |
| NFR-032A-15 | Graceful degradation | Continue on probe fail | P0 |
| NFR-032A-16 | Retry failed probes | Once with backoff | P1 |
| NFR-032A-17 | Single-flight discovery | No duplicate runs | P1 |
| NFR-032A-18 | Cross-platform support | Linux, Windows, macOS | P0 |
| NFR-032A-19 | No target side effects | Read-only operations | P0 |
| NFR-032A-20 | Recovery from cache corruption | Auto-invalidate | P1 |

### Observability

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-032A-21 | Structured logging | All operations | P0 |
| NFR-032A-22 | Probe duration metrics | Per-probe histogram | P1 |
| NFR-032A-23 | Cache hit/miss metrics | Counter | P1 |
| NFR-032A-24 | Discovery event publishing | Completion event | P0 |
| NFR-032A-25 | Failed probe tracking | Event per failure | P1 |
| NFR-032A-26 | Capability change detection | Compare to previous | P2 |
| NFR-032A-27 | Target capability summary | Log on completion | P1 |
| NFR-032A-28 | Health check integration | Exportable | P2 |
| NFR-032A-29 | Trace correlation | Request ID | P1 |
| NFR-032A-30 | Discovery audit log | All discoveries | P2 |

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

### Discovery Engine
- [ ] AC-001: `ICapabilityDiscovery` interface exists with `DiscoverAsync`
- [ ] AC-002: `DiscoverAsync` returns complete `TargetCapabilities`
- [ ] AC-003: Local target discovery executes shell commands
- [ ] AC-004: Remote target discovery uses SSH execution
- [ ] AC-005: Discovery completes within 60 second timeout
- [ ] AC-006: Partial results returned on timeout
- [ ] AC-007: Cache stores results by target ID
- [ ] AC-008: Cache TTL defaults to 5 minutes
- [ ] AC-009: Force refresh bypasses cache
- [ ] AC-010: Cache invalidation removes entry

### Hardware Discovery
- [ ] AC-011: CPU count discovered on Linux
- [ ] AC-012: CPU count discovered on Windows
- [ ] AC-013: CPU count discovered on macOS
- [ ] AC-014: Memory total discovered correctly
- [ ] AC-015: Memory available discovered
- [ ] AC-016: Disk space for workspace discovered
- [ ] AC-017: GPU presence detected via nvidia-smi
- [ ] AC-018: GPU count returned for multi-GPU
- [ ] AC-019: GPU VRAM discovered in bytes
- [ ] AC-020: GPU absent marked when nvidia-smi fails
- [ ] AC-021: Architecture (x64/arm64) detected

### Software Discovery
- [ ] AC-022: OS name discovered (Ubuntu, Windows, macOS)
- [ ] AC-023: OS version discovered
- [ ] AC-024: Default shell discovered
- [ ] AC-025: git availability probed
- [ ] AC-026: docker availability probed
- [ ] AC-027: Docker daemon running state checked
- [ ] AC-028: python version discovered
- [ ] AC-029: node version discovered
- [ ] AC-030: dotnet version discovered
- [ ] AC-031: Tool list configurable in options
- [ ] AC-032: Package manager detected (apt, yum, brew)

### Capability Schema
- [ ] AC-033: CapabilityType enum has 4 values
- [ ] AC-034: Boolean capabilities serialize correctly
- [ ] AC-035: Integer capabilities serialize correctly
- [ ] AC-036: String capabilities serialize correctly
- [ ] AC-037: Custom capabilities supported
- [ ] AC-038: Manual override merges with discovered
- [ ] AC-039: JSON serialization works
- [ ] AC-040: YAML serialization works

### Parallel Execution
- [ ] AC-041: Multiple probes run in parallel
- [ ] AC-042: Single probe failure doesn't block others
- [ ] AC-043: Parallel probe results aggregated
- [ ] AC-044: Probe timeout honored per-probe

### Observability
- [ ] AC-045: DiscoveryCompletedEvent published
- [ ] AC-046: ProbeFailedEvent published on failure
- [ ] AC-047: Structured logging for all probes
- [ ] AC-048: Cache hit/miss logged
- [ ] AC-049: Discovery duration logged
- [ ] AC-050: Failed probes listed in result
- [ ] AC-051: Metrics emitted for discovery
- [ ] AC-052: Probe durations tracked

---

## User Verification Scenarios

### Scenario 1: Local Target Discovery
**Persona:** Developer on local machine  
**Preconditions:** Local target configured, no prior discovery  
**Steps:**
1. Configure local target in agent-config.yml
2. Start session that requires placement
3. Observe discovery running
4. Check discovered capabilities

**Verification Checklist:**
- [ ] CPU count matches actual cores
- [ ] Memory matches system RAM
- [ ] OS correctly identified
- [ ] Tools found that are installed

### Scenario 2: Remote Target Discovery
**Persona:** Developer with SSH target  
**Preconditions:** SSH target configured and accessible  
**Steps:**
1. Add remote target with SSH credentials
2. Trigger discovery via placement
3. View discovered capabilities
4. Verify accuracy against target

**Verification Checklist:**
- [ ] Discovery uses SSH connection
- [ ] Remote hardware correctly discovered
- [ ] Remote OS correctly identified
- [ ] Discovery completes within timeout

### Scenario 3: GPU Detection
**Persona:** ML developer with GPU server  
**Preconditions:** Target has NVIDIA GPU with nvidia-smi  
**Steps:**
1. Configure target with GPU
2. Run discovery
3. Check GPU capabilities
4. Submit GPU-requiring task

**Verification Checklist:**
- [ ] GPU present = true
- [ ] GPU count accurate
- [ ] GPU VRAM in bytes
- [ ] GPU model name correct

### Scenario 4: Cache Behavior
**Persona:** Developer running multiple tasks  
**Preconditions:** Discovery already completed once  
**Steps:**
1. Complete first discovery
2. Immediately request second discovery
3. Observe cache hit
4. Wait 5 minutes, request again
5. Observe cache miss and re-discovery

**Verification Checklist:**
- [ ] Second request uses cache
- [ ] Cache hit logged
- [ ] After TTL, re-discovery runs
- [ ] Force refresh bypasses cache

### Scenario 5: Manual Override
**Persona:** Developer with known target specs  
**Preconditions:** Target has incomplete auto-discovery  
**Steps:**
1. Add manual capabilities in config
2. Run discovery
3. Check merged result
4. Manual overrides present

**Verification Checklist:**
- [ ] Manual values take precedence
- [ ] Discovered values fill gaps
- [ ] Merged result is complete
- [ ] Override logged

### Scenario 6: Partial Discovery Failure
**Persona:** Developer with partially accessible target  
**Preconditions:** Some probes will timeout  
**Steps:**
1. Configure target with limited access
2. Run discovery with some failing probes
3. Check partial results returned
4. Verify failed probes listed

**Verification Checklist:**
- [ ] Successful probes in result
- [ ] Failed probes listed explicitly
- [ ] IsPartial flag set
- [ ] ProbeFailedEvent published

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-032A-01 | CPU probe parsing for Linux | FR-032A-22 |
| UT-032A-02 | CPU probe parsing for Windows | FR-032A-23 |
| UT-032A-03 | Memory probe parsing | FR-032A-26 |
| UT-032A-04 | Disk probe parsing df output | FR-032A-29 |
| UT-032A-05 | GPU probe nvidia-smi parsing | FR-032A-32 |
| UT-032A-06 | OS probe /etc/os-release | FR-032A-43 |
| UT-032A-07 | Tool version extraction | FR-032A-51 |
| UT-032A-08 | Cache TTL expiry logic | FR-032A-14 |
| UT-032A-09 | Cache hit retrieval | NFR-032A-03 |
| UT-032A-10 | Manual override merging | FR-032A-75 |
| UT-032A-11 | Capability JSON serialization | FR-032A-73 |
| UT-032A-12 | Capability YAML serialization | FR-032A-74 |
| UT-032A-13 | Probe timeout handling | FR-032A-06 |
| UT-032A-14 | Partial result construction | FR-032A-08 |
| UT-032A-15 | CapabilityType enum values | FR-032A-66 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-032A-01 | Local discovery end-to-end | E2E |
| IT-032A-02 | Remote discovery via SSH | FR-032A-05 |
| IT-032A-03 | Parallel probe execution | FR-032A-17 |
| IT-032A-04 | GPU detection with nvidia-smi | FR-032A-31 |
| IT-032A-05 | Tool availability detection | FR-032A-47 |
| IT-032A-06 | Docker running state check | FR-032A-53 |
| IT-032A-07 | Cache persistence across sessions | NFR-032A-14 |
| IT-032A-08 | Force refresh bypasses cache | FR-032A-15 |
| IT-032A-09 | Discovery completion event | NFR-032A-24 |
| IT-032A-10 | Cross-platform discovery | NFR-032A-18 |
| IT-032A-11 | Multi-target parallel discovery | FR-032A-18 |
| IT-032A-12 | Graceful degradation on failures | NFR-032A-15 |
| IT-032A-13 | Probe metrics emission | NFR-032A-22 |
| IT-032A-14 | Cache hit/miss metrics | NFR-032A-23 |
| IT-032A-15 | Discovery timeout handling | NFR-032A-12 |

---

## Implementation Prompt

### Part 1: File Structure + Domain Models

```
src/
├── Acode.Domain/
│   └── Compute/
│       └── Discovery/
│           ├── CapabilityType.cs
│           ├── ProbeResult.cs
│           └── Events/
│               ├── DiscoveryCompletedEvent.cs
│               └── ProbeFailedEvent.cs
├── Acode.Application/
│   └── Compute/
│       └── Discovery/
│           ├── ICapabilityDiscovery.cs
│           ├── ICapabilityProbe.cs
│           ├── DiscoveryOptions.cs
│           ├── TargetCapabilities.cs
│           ├── HardwareCapabilities.cs
│           ├── SoftwareCapabilities.cs
│           └── GpuCapabilities.cs
└── Acode.Infrastructure/
    └── Compute/
        └── Discovery/
            ├── CapabilityDiscoveryService.cs
            ├── CapabilityCache.cs
            ├── ProbeRegistry.cs
            └── Probes/
                ├── CpuCountProbe.cs
                ├── MemoryProbe.cs
                ├── DiskProbe.cs
                ├── GpuProbe.cs
                ├── OsProbe.cs
                └── ToolVersionProbe.cs
```

```csharp
// src/Acode.Domain/Compute/Discovery/CapabilityType.cs
namespace Acode.Domain.Compute.Discovery;

public enum CapabilityType
{
    Hardware,
    Software,
    Resource,
    Custom
}

// src/Acode.Domain/Compute/Discovery/ProbeResult.cs
namespace Acode.Domain.Compute.Discovery;

public sealed record ProbeResult
{
    public required string ProbeName { get; init; }
    public required bool Success { get; init; }
    public object? Value { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
    public CapabilityType Type { get; init; }
}

// src/Acode.Domain/Compute/Discovery/Events/DiscoveryCompletedEvent.cs
namespace Acode.Domain.Compute.Discovery.Events;

public sealed record DiscoveryCompletedEvent(
    string TargetId,
    int ProbesRun,
    int ProbesSucceeded,
    TimeSpan TotalDuration,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Discovery/Events/ProbeFailedEvent.cs
namespace Acode.Domain.Compute.Discovery.Events;

public sealed record ProbeFailedEvent(
    string TargetId,
    string ProbeName,
    string ErrorMessage,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 032.a Specification - Part 1/3**

### Part 2: Application Interfaces

```csharp
// src/Acode.Application/Compute/Discovery/DiscoveryOptions.cs
namespace Acode.Application.Compute.Discovery;

public sealed record DiscoveryOptions
{
    public bool ForceRefresh { get; init; } = false;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);
    public IReadOnlyList<string> ToolsToProbe { get; init; } = ["git", "docker", "python3", "node", "dotnet"];
    public bool IncludeGpu { get; init; } = true;
    public bool ParallelProbes { get; init; } = true;
}

// src/Acode.Application/Compute/Discovery/GpuCapabilities.cs
namespace Acode.Application.Compute.Discovery;

public sealed record GpuCapabilities
{
    public bool Present { get; init; }
    public int Count { get; init; }
    public string? Type { get; init; } // nvidia, amd, intel
    public string? Model { get; init; }
    public long VramBytes { get; init; }
    public string? DriverVersion { get; init; }
    public string? CudaVersion { get; init; }
}

// src/Acode.Application/Compute/Discovery/HardwareCapabilities.cs
namespace Acode.Application.Compute.Discovery;

public sealed record HardwareCapabilities
{
    public int CpuCount { get; init; }
    public string? CpuModel { get; init; }
    public long MemoryBytes { get; init; }
    public long MemoryAvailableBytes { get; init; }
    public long DiskBytes { get; init; }
    public long DiskAvailableBytes { get; init; }
    public string Architecture { get; init; } = "x64";
    public GpuCapabilities? Gpu { get; init; }
}

// src/Acode.Application/Compute/Discovery/SoftwareCapabilities.cs
namespace Acode.Application.Compute.Discovery;

public sealed record SoftwareCapabilities
{
    public required string OsName { get; init; }
    public required string OsVersion { get; init; }
    public string? Shell { get; init; }
    public IReadOnlyDictionary<string, string> ToolVersions { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<string> PackageManagers { get; init; } = [];
    public bool DockerAvailable { get; init; }
    public bool DockerRunning { get; init; }
}

// src/Acode.Application/Compute/Discovery/TargetCapabilities.cs
namespace Acode.Application.Compute.Discovery;

public sealed record TargetCapabilities
{
    public required string TargetId { get; init; }
    public DateTimeOffset DiscoveredAt { get; init; }
    public required HardwareCapabilities Hardware { get; init; }
    public required SoftwareCapabilities Software { get; init; }
    public IReadOnlyDictionary<string, object> Custom { get; init; } = new Dictionary<string, object>();
    public bool IsPartial { get; init; }
    public IReadOnlyList<string> FailedProbes { get; init; } = [];
}

// src/Acode.Application/Compute/Discovery/ICapabilityProbe.cs
namespace Acode.Application.Compute.Discovery;

public interface ICapabilityProbe
{
    string Name { get; }
    CapabilityType Type { get; }
    
    Task<ProbeResult> ProbeAsync(
        IComputeTarget target,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Discovery/ICapabilityDiscovery.cs
namespace Acode.Application.Compute.Discovery;

public interface ICapabilityDiscovery
{
    Task<TargetCapabilities> DiscoverAsync(
        IComputeTarget target,
        DiscoveryOptions? options = null,
        CancellationToken ct = default);
    
    Task InvalidateCacheAsync(string targetId, CancellationToken ct = default);
    
    TargetCapabilities? GetCached(string targetId);
    
    Task<IReadOnlyList<ProbeResult>> RunProbesAsync(
        IComputeTarget target,
        IReadOnlyList<string> probeNames,
        CancellationToken ct = default);
}
```

**End of Task 032.a Specification - Part 2/3**

### Part 3: Infrastructure Implementation + Checklist

```csharp
// src/Acode.Infrastructure/Compute/Discovery/CapabilityCache.cs
namespace Acode.Infrastructure.Compute.Discovery;

public sealed class CapabilityCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _defaultTtl;
    
    public CapabilityCache(TimeSpan? defaultTtl = null)
    {
        _defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(5);
    }
    
    public TargetCapabilities? Get(string targetId)
    {
        if (_cache.TryGetValue(targetId, out var entry) && !entry.IsExpired)
            return entry.Capabilities;
        return null;
    }
    
    public void Set(string targetId, TargetCapabilities caps, TimeSpan? ttl = null)
    {
        _cache[targetId] = new CacheEntry(caps, ttl ?? _defaultTtl);
    }
    
    public void Invalidate(string targetId) => _cache.TryRemove(targetId, out _);
    
    private sealed record CacheEntry(TargetCapabilities Capabilities, TimeSpan Ttl)
    {
        private readonly DateTimeOffset _created = DateTimeOffset.UtcNow;
        public bool IsExpired => DateTimeOffset.UtcNow - _created > Ttl;
    }
}

// src/Acode.Infrastructure/Compute/Discovery/Probes/CpuCountProbe.cs
namespace Acode.Infrastructure.Compute.Discovery.Probes;

public sealed class CpuCountProbe : ICapabilityProbe
{
    public string Name => "cpu.count";
    public CapabilityType Type => CapabilityType.Hardware;
    
    public async Task<ProbeResult> ProbeAsync(IComputeTarget target, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var cmd = target.Os == "windows" 
                ? "wmic cpu get NumberOfCores" 
                : "nproc";
            var result = await target.ExecuteAsync(cmd, ct);
            var count = ParseCpuCount(result.Output, target.Os);
            return new ProbeResult { ProbeName = Name, Success = true, Value = count, Duration = sw.Elapsed, Type = Type };
        }
        catch (Exception ex)
        {
            return new ProbeResult { ProbeName = Name, Success = false, ErrorMessage = ex.Message, Duration = sw.Elapsed, Type = Type };
        }
    }
}

// src/Acode.Infrastructure/Compute/Discovery/Probes/GpuProbe.cs
namespace Acode.Infrastructure.Compute.Discovery.Probes;

public sealed class GpuProbe : ICapabilityProbe
{
    public string Name => "gpu";
    public CapabilityType Type => CapabilityType.Hardware;
    
    public async Task<ProbeResult> ProbeAsync(IComputeTarget target, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await target.ExecuteAsync("nvidia-smi --query-gpu=name,memory.total,driver_version --format=csv,noheader", ct);
            if (result.ExitCode != 0)
                return new ProbeResult { ProbeName = Name, Success = true, Value = new GpuCapabilities { Present = false }, Duration = sw.Elapsed, Type = Type };
            
            var gpu = ParseNvidiaSmi(result.Output);
            return new ProbeResult { ProbeName = Name, Success = true, Value = gpu, Duration = sw.Elapsed, Type = Type };
        }
        catch
        {
            return new ProbeResult { ProbeName = Name, Success = true, Value = new GpuCapabilities { Present = false }, Duration = sw.Elapsed, Type = Type };
        }
    }
}

// src/Acode.Infrastructure/Compute/Discovery/CapabilityDiscoveryService.cs
namespace Acode.Infrastructure.Compute.Discovery;

public sealed class CapabilityDiscoveryService : ICapabilityDiscovery
{
    private readonly IEnumerable<ICapabilityProbe> _probes;
    private readonly CapabilityCache _cache;
    private readonly IEventPublisher _events;
    private readonly ILogger<CapabilityDiscoveryService> _logger;
    
    public async Task<TargetCapabilities> DiscoverAsync(
        IComputeTarget target,
        DiscoveryOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new DiscoveryOptions();
        
        if (!options.ForceRefresh)
        {
            var cached = _cache.Get(target.Id);
            if (cached != null) return cached;
        }
        
        var sw = Stopwatch.StartNew();
        var results = options.ParallelProbes
            ? await RunParallelAsync(target, ct)
            : await RunSequentialAsync(target, ct);
        
        var caps = BuildCapabilities(target.Id, results);
        _cache.Set(target.Id, caps);
        
        await _events.PublishAsync(new DiscoveryCompletedEvent(
            target.Id, results.Count, results.Count(r => r.Success), sw.Elapsed, DateTimeOffset.UtcNow), ct);
        
        return caps;
    }
}
```

### Implementation Checklist

| Step | Action | Verification |
|------|--------|--------------|
| 1 | Create domain models (CapabilityType, ProbeResult) | Unit tests pass |
| 2 | Add discovery events | Event serialization verified |
| 3 | Define DiscoveryOptions, capability records | Records compile |
| 4 | Create ICapabilityProbe, ICapabilityDiscovery | Interface contracts clear |
| 5 | Implement CapabilityCache with TTL | Cache expiry verified |
| 6 | Implement CpuCountProbe (Linux/Windows/macOS) | Cross-platform tested |
| 7 | Implement MemoryProbe | Memory parsing verified |
| 8 | Implement DiskProbe | Disk space accurate |
| 9 | Implement GpuProbe | nvidia-smi parsing verified |
| 10 | Implement OsProbe | OS detection works |
| 11 | Implement ToolVersionProbe | Tool versions extracted |
| 12 | Implement CapabilityDiscoveryService | End-to-end discovery works |
| 13 | Add parallel probe execution | Concurrent probes run |
| 14 | Register probes in DI | All probes resolved |

### Rollout Plan

1. **Phase 1**: Implement cache and probe infrastructure
2. **Phase 2**: Add hardware probes (CPU, memory, disk)
3. **Phase 3**: Add GPU probe with nvidia-smi
4. **Phase 4**: Add software probes (OS, tools)
5. **Phase 5**: Integration test with local and SSH targets

**End of Task 032.a Specification**