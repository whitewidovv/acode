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