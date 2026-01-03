# Task 020.b: Cache Volumes (NuGet/npm)

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 020 (Docker Sandbox), Task 020.a (Per-Task Containers)  

---

## Description

Task 020.b implements cache volumes for package managers. Per-task containers (Task 020.a) MUST NOT re-download packages every time. Shared cache volumes solve this.

Package restoration is slow. NuGet packages can be hundreds of megabytes. npm packages likewise. Downloading repeatedly wastes time and bandwidth.

Docker volumes persist across containers. A named volume for NuGet cache survives container removal. Next container mounts the same volume. Packages are already present.

Cache volumes MUST be language-specific. NuGet cache MUST NOT mix with npm cache. Each package manager MUST have its own volume.

Volume paths MUST match package manager expectations. NuGet expects `~/.nuget/packages`. npm expects `~/.npm`. Volumes MUST mount at correct locations.

Cache invalidation MUST be supported. Users MUST be able to clear caches. Stale packages MUST be removable.

Volume security MUST be considered. Cached packages could be malicious. Trust model MUST be documented.

---

## Functional Requirements

- FR-001: NuGet cache volume MUST be created and mounted
- FR-002: npm cache volume MUST be created and mounted
- FR-003: Volumes MUST persist across container runs
- FR-004: Volume names MUST follow pattern: `acode-cache-{manager}`
- FR-005: Mount paths MUST match package manager defaults
- FR-006: Cache clear command MUST be provided
- FR-007: Multiple projects MUST share the same cache

### Mount Paths

- FR-008: NuGet MUST mount at `/root/.nuget/packages`
- FR-009: npm MUST mount at `/root/.npm`
- FR-010: yarn MUST mount at `/root/.cache/yarn`
- FR-011: pnpm MUST mount at `/root/.pnpm-store`

---

## Acceptance Criteria

- [ ] AC-001: Package restore MUST use cached packages
- [ ] AC-002: Second restore MUST be faster than first
- [ ] AC-003: Cache MUST persist across tasks
- [ ] AC-004: Cache clear MUST remove all cached packages
- [ ] AC-005: Volumes MUST be created automatically

---

## User Manual Documentation

### Configuration

```yaml
# .agent/config.yml
sandbox:
  cache_volumes:
    enabled: true
    nuget: acode-cache-nuget
    npm: acode-cache-npm
```

### CLI Commands

```bash
# Clear all caches
acode cache clear

# Clear specific cache
acode cache clear --nuget
acode cache clear --npm

# Show cache sizes
acode cache stats
```

---

## Implementation Prompt

### Volume Creation

```csharp
public async Task EnsureCacheVolumeAsync(string volumeName)
{
    // Check if volume exists, create if not
    var exists = await _docker.Volumes.InspectAsync(volumeName)
        .ContinueWith(t => !t.IsFaulted);
    
    if (!exists)
    {
        await _docker.Volumes.CreateAsync(new VolumesCreateParameters
        {
            Name = volumeName
        });
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-VOL-001 | Volume creation failed |
| ACODE-VOL-002 | Volume mount failed |

---

**End of Task 020.b Specification**