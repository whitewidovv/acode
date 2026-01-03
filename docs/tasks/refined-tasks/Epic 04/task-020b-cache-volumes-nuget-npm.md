# Task 020.b: Cache Volumes (NuGet/npm)

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 020 (Docker Sandbox), Task 020.a (Per-Task Containers)  

---

## Description

### Overview

Task 020.b implements persistent cache volumes for package managers within the Docker sandbox infrastructure. Per-task containers from Task 020.a create isolated execution environments, but without shared caches, every container invocation would re-download dependencies from scratch—NuGet packages, npm modules, yarn caches—wasting time and bandwidth. This task introduces named Docker volumes that persist across container lifecycles, enabling package restoration to leverage previously downloaded packages automatically.

### Business Value

1. **Dramatic Build Acceleration**: Second and subsequent builds skip download phase entirely, reducing restore times from minutes to seconds
2. **Bandwidth Conservation**: Large packages (Entity Framework, React, Angular) download once and persist indefinitely
3. **Offline Capability Enhancement**: Cached packages remain available even when network connectivity is limited
4. **Developer Experience Improvement**: Faster feedback loops increase productivity and reduce frustration
5. **Resource Efficiency**: Reduced network I/O and disk writes extend hardware lifespan
6. **Multi-Project Synergy**: Projects sharing dependencies benefit from each other's cache population

### Scope

This task encompasses:

1. **Volume Lifecycle Management**: Creation, mounting, inspection, and cleanup of named Docker volumes
2. **Package Manager Integration**: NuGet, npm, yarn, and pnpm cache path configuration
3. **Mount Path Configuration**: Mapping volumes to correct container paths per package manager
4. **Cache Isolation**: Separate volumes per package manager to prevent conflicts
5. **Cache Statistics**: Commands to report cache sizes and usage metrics
6. **Cache Invalidation**: Commands to clear caches selectively or entirely
7. **Security Documentation**: Trust model for cached packages
8. **Configuration Schema**: YAML configuration for volume names and behavior
9. **Error Handling**: Graceful degradation when volume operations fail

### Integration Points

| Component | Integration Type | Data Flow |
|-----------|------------------|-----------|
| ContainerLifecycleManager | Volume Mounting | CacheVolumeManager → ContainerConfig |
| Docker Client | Volume Operations | CacheVolumeManager → Docker.DotNet |
| AgentConfig.yml | Configuration | Parser → CacheVolumeManager |
| CLI Layer | User Commands | Commands → CacheVolumeManager |
| TaskExecutionService | Build Execution | Executor → Volumes mounted in container |

### Failure Modes

| Failure Mode | Detection | Recovery |
|--------------|-----------|----------|
| Volume creation fails (permissions) | Docker API error | Fall back to no cache, log warning |
| Volume mount fails at runtime | Container start failure | Retry without volume, degrade gracefully |
| Disk space exhausted | Docker volume inspect | Prune old/unused volumes, alert user |
| Corrupted cache packages | Build failures after restore | Clear cache, re-download |
| Volume name collision | Inspect returns unexpected data | Use unique prefix/suffix strategy |

### Assumptions

- Docker daemon is running and accessible
- User has permissions to create and mount volumes
- Package manager cache paths follow standard conventions
- Sufficient disk space exists for cache storage
- Containers run as root (cache paths use `/root/`)

### Security Considerations

Cached packages represent a trust boundary. A malicious package cached during one build could affect subsequent builds. The trust model assumes:
- Packages are fetched from configured/trusted registries
- NuGet package signature verification is enabled where supported
- npm audit/yarn audit should be run periodically
- Cache clearing is available to address compromised packages

---

## Functional Requirements

### Volume Lifecycle Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020B-01 | System MUST create named Docker volumes on first use | MUST |
| FR-020B-02 | System MUST check volume existence before creation | MUST |
| FR-020B-03 | System MUST reuse existing volumes for subsequent runs | MUST |
| FR-020B-04 | System MUST support volume deletion via CLI command | MUST |
| FR-020B-05 | System MUST list all managed cache volumes | MUST |
| FR-020B-06 | System MUST inspect volume metadata (size, created date) | MUST |
| FR-020B-07 | System MUST use naming pattern `acode-cache-{manager}` | MUST |
| FR-020B-08 | System SHOULD support custom volume name prefixes via config | SHOULD |
| FR-020B-09 | System MUST handle Docker daemon unavailable gracefully | MUST |
| FR-020B-10 | System MUST log volume operations for debugging | MUST |

### NuGet Cache Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020B-11 | System MUST create volume `acode-cache-nuget` | MUST |
| FR-020B-12 | System MUST mount at `/root/.nuget/packages` inside container | MUST |
| FR-020B-13 | System MUST set `NUGET_PACKAGES` environment variable to mount path | MUST |
| FR-020B-14 | System MUST support `dotnet restore` with cached packages | MUST |
| FR-020B-15 | System MUST support `dotnet build` with cached packages | MUST |
| FR-020B-16 | System SHOULD support fallback package sources if cache miss | SHOULD |
| FR-020B-17 | System MUST preserve NuGet package metadata and signatures | MUST |
| FR-020B-18 | System MUST handle concurrent access from parallel containers | MUST |
| FR-020B-19 | System MUST support HTTP cache for package metadata | SHOULD |
| FR-020B-20 | System MUST mount volume as read-write | MUST |

### npm Cache Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020B-21 | System MUST create volume `acode-cache-npm` | MUST |
| FR-020B-22 | System MUST mount at `/root/.npm` inside container | MUST |
| FR-020B-23 | System MUST support `npm install` with cached packages | MUST |
| FR-020B-24 | System MUST support `npm ci` with cached packages | MUST |
| FR-020B-25 | System MUST handle package-lock.json hash mismatches | MUST |
| FR-020B-26 | System SHOULD set `npm_config_cache` environment variable | SHOULD |
| FR-020B-27 | System MUST preserve npm cache integrity metadata | MUST |
| FR-020B-28 | System MUST support scoped packages (@org/package) | MUST |
| FR-020B-29 | System MUST handle tarball cache entries | MUST |
| FR-020B-30 | System MUST mount volume as read-write | MUST |

### yarn Cache Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020B-31 | System MUST create volume `acode-cache-yarn` | MUST |
| FR-020B-32 | System MUST mount at `/root/.cache/yarn` inside container | MUST |
| FR-020B-33 | System MUST support `yarn install` with cached packages | MUST |
| FR-020B-34 | System MUST support Yarn Berry (v2+) cache format | SHOULD |
| FR-020B-35 | System SHOULD set `YARN_CACHE_FOLDER` environment variable | SHOULD |
| FR-020B-36 | System MUST preserve yarn.lock integrity | MUST |
| FR-020B-37 | System MUST handle PnP (Plug'n'Play) cache if enabled | SHOULD |
| FR-020B-38 | System MUST support offline mirror mode | SHOULD |
| FR-020B-39 | System MUST mount volume as read-write | MUST |
| FR-020B-40 | System MUST handle Yarn Classic and Yarn Berry differences | SHOULD |

### pnpm Cache Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020B-41 | System MUST create volume `acode-cache-pnpm` | MUST |
| FR-020B-42 | System MUST mount at `/root/.pnpm-store` inside container | MUST |
| FR-020B-43 | System MUST support `pnpm install` with cached packages | MUST |
| FR-020B-44 | System SHOULD set `PNPM_HOME` environment variable | SHOULD |
| FR-020B-45 | System MUST preserve content-addressable store structure | MUST |
| FR-020B-46 | System MUST handle hard links within store | MUST |
| FR-020B-47 | System MUST support pnpm workspace caching | SHOULD |
| FR-020B-48 | System MUST mount volume as read-write | MUST |
| FR-020B-49 | System SHOULD support store pruning command | SHOULD |
| FR-020B-50 | System MUST handle symlinked node_modules with store | MUST |

### Cache Configuration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020B-51 | System MUST read cache configuration from `agent-config.yml` | MUST |
| FR-020B-52 | System MUST support enabling/disabling caching globally | MUST |
| FR-020B-53 | System MUST support enabling/disabling per package manager | MUST |
| FR-020B-54 | System MUST support custom volume names via config | SHOULD |
| FR-020B-55 | System MUST support custom mount paths via config | SHOULD |
| FR-020B-56 | System MUST validate volume names for Docker compatibility | MUST |
| FR-020B-57 | System MUST apply default configuration when not specified | MUST |
| FR-020B-58 | System MUST emit configuration validation errors | MUST |
| FR-020B-59 | System SHOULD support volume driver configuration | SHOULD |
| FR-020B-60 | System MUST reload configuration on config file change | SHOULD |

### Cache Statistics and Reporting

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020B-61 | System MUST report cache size per volume | MUST |
| FR-020B-62 | System MUST report cache creation timestamp | MUST |
| FR-020B-63 | System MUST report last used timestamp | SHOULD |
| FR-020B-64 | System MUST report package count per cache | SHOULD |
| FR-020B-65 | System MUST output stats in human-readable format | MUST |
| FR-020B-66 | System MUST support JSON output for stats | MUST |
| FR-020B-67 | System SHOULD track cache hit/miss ratios | SHOULD |
| FR-020B-68 | System SHOULD report disk space savings estimate | SHOULD |
| FR-020B-69 | System MUST handle volumes with no data gracefully | MUST |
| FR-020B-70 | System MUST display stats in consistent units (MB/GB) | MUST |

### Cache Invalidation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020B-71 | System MUST support clearing all caches | MUST |
| FR-020B-72 | System MUST support clearing specific package manager cache | MUST |
| FR-020B-73 | System MUST confirm before destructive operations | MUST |
| FR-020B-74 | System MUST support `--force` flag to skip confirmation | MUST |
| FR-020B-75 | System MUST report freed disk space after clear | MUST |
| FR-020B-76 | System MUST handle volumes in use by running containers | MUST |
| FR-020B-77 | System MUST recreate volume after clear if needed | MUST |
| FR-020B-78 | System SHOULD support selective package removal | SHOULD |
| FR-020B-79 | System MUST log cache clear operations | MUST |
| FR-020B-80 | System MUST support dry-run mode for clear operations | SHOULD |

### Container Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020B-81 | System MUST add volume mounts to container configuration | MUST |
| FR-020B-82 | System MUST create volumes before container start | MUST |
| FR-020B-83 | System MUST set appropriate environment variables in container | MUST |
| FR-020B-84 | System MUST handle volume mount failures gracefully | MUST |
| FR-020B-85 | System MUST support multiple volumes per container | MUST |
| FR-020B-86 | System MUST detect project type to select appropriate caches | MUST |
| FR-020B-87 | System MUST mount NuGet cache for .NET projects | MUST |
| FR-020B-88 | System MUST mount npm/yarn/pnpm cache for Node.js projects | MUST |
| FR-020B-89 | System MUST mount both caches for mixed projects | MUST |
| FR-020B-90 | System MUST validate mount paths before container start | MUST |

---

## Non-Functional Requirements

### Performance

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-020B-01 | Volume existence check | < 50ms |
| NFR-020B-02 | Volume creation | < 500ms |
| NFR-020B-03 | Volume inspection (metadata) | < 100ms |
| NFR-020B-04 | Adding volume mount to container config | < 1ms |
| NFR-020B-05 | Cache stats collection (all volumes) | < 2s |
| NFR-020B-06 | Cached package restore vs fresh | > 80% faster |
| NFR-020B-07 | Volume mount at container start | < 100ms per volume |
| NFR-020B-08 | Memory overhead per managed volume | < 1MB |

### Reliability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-020B-09 | Volume operations MUST be idempotent | 100% |
| NFR-020B-10 | System MUST handle Docker restarts | Automatic recovery |
| NFR-020B-11 | Corrupted volume MUST be detectable | Via health check |
| NFR-020B-12 | Concurrent container access MUST NOT corrupt cache | 100% |
| NFR-020B-13 | Volume creation MUST be atomic | No partial volumes |
| NFR-020B-14 | System MUST recover from failed volume operations | Retry with backoff |
| NFR-020B-15 | Cache consistency after power loss | Best effort |
| NFR-020B-16 | Graceful degradation without caching | Full functionality |

### Security

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-020B-17 | Volumes MUST be project-isolated where configured | Configurable |
| NFR-020B-18 | Volume permissions MUST restrict access | Container user only |
| NFR-020B-19 | Cached packages MUST NOT execute during mount | No execution |
| NFR-020B-20 | Volume names MUST NOT leak sensitive data | Sanitized names |
| NFR-020B-21 | Clear operations MUST require confirmation | Default enabled |
| NFR-020B-22 | Audit trail for volume operations | Logged |
| NFR-020B-23 | No host filesystem exposure via volumes | Volumes only |
| NFR-020B-24 | Package signature verification support | Passthrough |

### Maintainability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-020B-25 | Volume manager code coverage | ≥ 90% |
| NFR-020B-26 | Clear separation from container lifecycle | Interface boundary |
| NFR-020B-27 | Extensible for new package managers | Plugin architecture |
| NFR-020B-28 | Configuration changes without code changes | 100% |
| NFR-020B-29 | Dependency on Docker.DotNet abstracted | Via interface |
| NFR-020B-30 | Documentation for adding new cache types | Developer guide |
| NFR-020B-31 | Cyclomatic complexity per method | ≤ 10 |
| NFR-020B-32 | Maximum method length | ≤ 50 lines |

### Observability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-020B-33 | Log volume create/delete operations | Info level |
| NFR-020B-34 | Log volume mount operations | Debug level |
| NFR-020B-35 | Log cache statistics on request | Info level |
| NFR-020B-36 | Emit metrics for cache size | Metric endpoint |
| NFR-020B-37 | Emit metrics for cache hit ratio | Where measurable |
| NFR-020B-38 | Structured logging with volume name | JSON format |
| NFR-020B-39 | Correlation ID through volume operations | Trace context |
| NFR-020B-40 | Alert on disk space threshold | Configurable |

---

## Acceptance Criteria

### Volume Lifecycle

- [ ] AC-020B-01: Volume `acode-cache-nuget` is created on first .NET task execution
- [ ] AC-020B-02: Volume `acode-cache-npm` is created on first Node.js task execution
- [ ] AC-020B-03: Volume `acode-cache-yarn` is created when yarn detected
- [ ] AC-020B-04: Volume `acode-cache-pnpm` is created when pnpm detected
- [ ] AC-020B-05: Existing volumes are reused without recreation
- [ ] AC-020B-06: Volume inspection returns size and metadata
- [ ] AC-020B-07: `acode cache list` displays all managed volumes
- [ ] AC-020B-08: Volumes persist after container removal

### NuGet Integration

- [ ] AC-020B-09: NuGet volume mounts at `/root/.nuget/packages`
- [ ] AC-020B-10: `NUGET_PACKAGES` environment variable set in container
- [ ] AC-020B-11: `dotnet restore` uses cached packages
- [ ] AC-020B-12: Second restore completes in < 20% of first restore time
- [ ] AC-020B-13: Package signatures preserved in cache
- [ ] AC-020B-14: Parallel containers can access cache simultaneously
- [ ] AC-020B-15: NuGet HTTP cache metadata preserved
- [ ] AC-020B-16: Large packages (EF Core, ASP.NET) cache correctly

### npm Integration

- [ ] AC-020B-17: npm volume mounts at `/root/.npm`
- [ ] AC-020B-18: `npm install` uses cached packages
- [ ] AC-020B-19: `npm ci` uses cached packages
- [ ] AC-020B-20: Scoped packages (@org/package) cache correctly
- [ ] AC-020B-21: Tarball cache entries preserved
- [ ] AC-020B-22: Second install completes in < 20% of first install time
- [ ] AC-020B-23: package-lock.json changes trigger appropriate updates
- [ ] AC-020B-24: npm cache verify passes after multiple runs

### yarn Integration

- [ ] AC-020B-25: yarn volume mounts at `/root/.cache/yarn`
- [ ] AC-020B-26: `yarn install` uses cached packages
- [ ] AC-020B-27: Yarn Classic (v1) cache format supported
- [ ] AC-020B-28: Yarn Berry (v2+) cache format supported
- [ ] AC-020B-29: yarn.lock integrity preserved across runs
- [ ] AC-020B-30: Offline mode works with populated cache
- [ ] AC-020B-31: Second install completes in < 20% of first install time
- [ ] AC-020B-32: Workspace dependencies cache correctly

### pnpm Integration

- [ ] AC-020B-33: pnpm volume mounts at `/root/.pnpm-store`
- [ ] AC-020B-34: `pnpm install` uses cached packages
- [ ] AC-020B-35: Content-addressable store structure preserved
- [ ] AC-020B-36: Hard links function correctly within store
- [ ] AC-020B-37: Second install completes in < 20% of first install time
- [ ] AC-020B-38: Store pruning via `pnpm store prune` works
- [ ] AC-020B-39: Workspace dependencies cached correctly
- [ ] AC-020B-40: Symlinked node_modules work with store

### Configuration

- [ ] AC-020B-41: Caching enabled by default when Docker available
- [ ] AC-020B-42: `sandbox.cache_volumes.enabled: false` disables all caching
- [ ] AC-020B-43: Individual package managers can be disabled
- [ ] AC-020B-44: Custom volume names applied when configured
- [ ] AC-020B-45: Custom mount paths applied when configured
- [ ] AC-020B-46: Invalid volume names produce clear errors
- [ ] AC-020B-47: Missing configuration uses sensible defaults
- [ ] AC-020B-48: Configuration reloaded when file changes

### Statistics and Reporting

- [ ] AC-020B-49: `acode cache stats` shows all cache sizes
- [ ] AC-020B-50: Stats display creation date per volume
- [ ] AC-020B-51: Stats show last used date per volume
- [ ] AC-020B-52: Stats show package count where measurable
- [ ] AC-020B-53: `acode cache stats --json` outputs valid JSON
- [ ] AC-020B-54: Sizes displayed in appropriate units (MB/GB)
- [ ] AC-020B-55: Empty caches display as 0 bytes
- [ ] AC-020B-56: Stats complete within 2 seconds

### Cache Invalidation

- [ ] AC-020B-57: `acode cache clear` prompts for confirmation
- [ ] AC-020B-58: `acode cache clear --force` skips confirmation
- [ ] AC-020B-59: `acode cache clear --nuget` clears only NuGet cache
- [ ] AC-020B-60: `acode cache clear --npm` clears only npm cache
- [ ] AC-020B-61: Freed disk space reported after clear
- [ ] AC-020B-62: Running containers block clear with helpful message
- [ ] AC-020B-63: Cleared volumes recreated on next use
- [ ] AC-020B-64: Clear operations logged with timestamp

### Graceful Degradation

- [ ] AC-020B-65: Volume creation failure logs warning, continues without cache
- [ ] AC-020B-66: Volume mount failure logs warning, starts container without mount
- [ ] AC-020B-67: Docker unavailable falls back to no caching
- [ ] AC-020B-68: Disk full condition handled gracefully
- [ ] AC-020B-69: Corrupted cache detected via build failure, clear recommended
- [ ] AC-020B-70: All degradation scenarios produce actionable error messages

---

## User Manual Documentation

### Overview

Cache volumes dramatically improve build performance by persisting downloaded packages across container runs. Instead of downloading NuGet packages or npm modules every time, the agentic coding bot stores them in Docker volumes that survive container lifecycle.

### Configuration

```yaml
# .agent/config.yml
sandbox:
  cache_volumes:
    enabled: true                    # Master switch for all caching
    
    nuget:
      enabled: true                  # Enable NuGet caching
      volume_name: acode-cache-nuget # Volume name (optional)
      mount_path: /root/.nuget/packages # Mount path (optional)
    
    npm:
      enabled: true                  # Enable npm caching
      volume_name: acode-cache-npm   # Volume name (optional)
      mount_path: /root/.npm         # Mount path (optional)
    
    yarn:
      enabled: true                  # Enable yarn caching
      volume_name: acode-cache-yarn  # Volume name (optional)
      mount_path: /root/.cache/yarn  # Mount path (optional)
    
    pnpm:
      enabled: true                  # Enable pnpm caching
      volume_name: acode-cache-pnpm  # Volume name (optional)
      mount_path: /root/.pnpm-store  # Mount path (optional)
```

### Default Volume Names

| Package Manager | Default Volume Name | Default Mount Path |
|-----------------|--------------------|--------------------|
| NuGet | `acode-cache-nuget` | `/root/.nuget/packages` |
| npm | `acode-cache-npm` | `/root/.npm` |
| yarn | `acode-cache-yarn` | `/root/.cache/yarn` |
| pnpm | `acode-cache-pnpm` | `/root/.pnpm-store` |

### CLI Commands

#### List Cache Volumes

```bash
# Show all managed cache volumes
acode cache list

# Output:
# VOLUME                  SIZE      CREATED          LAST USED
# acode-cache-nuget      1.2 GB    2024-01-15 10:00  2024-01-20 14:30
# acode-cache-npm        856 MB    2024-01-15 10:05  2024-01-20 14:25
# acode-cache-yarn       0 bytes   (not created)     (never)
# acode-cache-pnpm       0 bytes   (not created)     (never)
```

#### View Cache Statistics

```bash
# Show detailed cache statistics
acode cache stats

# Output:
# Cache Statistics
# ================
# 
# NuGet Cache (acode-cache-nuget)
#   Size:           1.2 GB
#   Created:        2024-01-15 10:00:00
#   Last Used:      2024-01-20 14:30:00
#   Packages:       ~150 packages
#   
# npm Cache (acode-cache-npm)
#   Size:           856 MB
#   Created:        2024-01-15 10:05:00
#   Last Used:      2024-01-20 14:25:00
#   Packages:       ~2,400 tarballs
#
# Total Cache Size: 2.05 GB

# JSON output for scripting
acode cache stats --json

# Output:
# {
#   "nuget": {
#     "volume_name": "acode-cache-nuget",
#     "size_bytes": 1288490188,
#     "created": "2024-01-15T10:00:00Z",
#     "last_used": "2024-01-20T14:30:00Z"
#   },
#   "npm": { ... }
# }
```

#### Clear Caches

```bash
# Clear all caches (with confirmation)
acode cache clear

# Output:
# This will delete the following cache volumes:
#   - acode-cache-nuget (1.2 GB)
#   - acode-cache-npm (856 MB)
# 
# Total space to be freed: 2.05 GB
# 
# Are you sure? [y/N]: y
# 
# ✓ Cleared acode-cache-nuget (freed 1.2 GB)
# ✓ Cleared acode-cache-npm (freed 856 MB)
# 
# Total freed: 2.05 GB

# Clear without confirmation
acode cache clear --force

# Clear specific cache only
acode cache clear --nuget
acode cache clear --npm
acode cache clear --yarn
acode cache clear --pnpm

# Dry run (show what would be cleared)
acode cache clear --dry-run
```

### Automatic Cache Detection

The bot automatically detects which caches to mount based on project type:

| Project Contains | Caches Mounted |
|------------------|----------------|
| `*.csproj`, `*.fsproj`, `*.sln` | NuGet |
| `package.json` with npm | npm |
| `package.json` with `yarn.lock` | yarn |
| `package.json` with `pnpm-lock.yaml` | pnpm |
| Mixed .NET and Node.js | NuGet + appropriate JS cache |

### Performance Expectations

| Scenario | First Run | Cached Run | Improvement |
|----------|-----------|------------|-------------|
| Large .NET project (50+ packages) | 2-5 minutes | 5-15 seconds | 80-95% |
| React app (node_modules) | 1-3 minutes | 10-30 seconds | 70-90% |
| Monorepo (mixed) | 3-8 minutes | 15-45 seconds | 80-90% |

### Troubleshooting

#### Cache Not Being Used

```bash
# Verify volumes exist
docker volume ls | grep acode-cache

# Check volume is mounted in container
docker inspect <container-id> --format '{{json .Mounts}}' | jq

# Verify environment variables
docker exec <container-id> env | grep -E "(NUGET|npm)"
```

#### Corrupted Cache

If builds fail with cache-related errors:

```bash
# Clear the problematic cache
acode cache clear --nuget --force

# Next build will repopulate cache
acode task run build
```

#### Disk Space Issues

```bash
# Check cache sizes
acode cache stats

# Clear all caches to free space
acode cache clear --force

# Or clear selectively
acode cache clear --npm --force
```

### Security Notes

- Cached packages come from your configured registries
- Enable NuGet package signature verification for production builds
- Run `npm audit` or `yarn audit` periodically on your projects
- Clear caches if you suspect compromised packages

---

## Testing Requirements

### Unit Tests

#### CacheVolumeManagerTests

```csharp
[Fact] EnsureVolumeAsync_WhenVolumeDoesNotExist_CreatesVolume()
[Fact] EnsureVolumeAsync_WhenVolumeExists_DoesNotRecreate()
[Fact] EnsureVolumeAsync_WhenDockerUnavailable_ThrowsDockerNotAvailableException()
[Fact] EnsureVolumeAsync_WithInvalidVolumeName_ThrowsInvalidVolumeNameException()
[Fact] GetVolumeInfoAsync_WhenVolumeExists_ReturnsVolumeInfo()
[Fact] GetVolumeInfoAsync_WhenVolumeDoesNotExist_ReturnsNull()
[Fact] DeleteVolumeAsync_WhenVolumeExists_DeletesVolume()
[Fact] DeleteVolumeAsync_WhenVolumeInUse_ThrowsVolumeInUseException()
[Fact] DeleteVolumeAsync_WhenVolumeDoesNotExist_ReturnsSuccess()
[Fact] ListVolumesAsync_ReturnsOnlyManagedVolumes()
[Fact] ListVolumesAsync_WithNoVolumes_ReturnsEmptyList()
```

#### CacheConfigurationTests

```csharp
[Fact] GetCacheConfig_WhenNotConfigured_ReturnsDefaults()
[Fact] GetCacheConfig_WhenDisabled_ReturnsDisabledConfig()
[Fact] GetCacheConfig_WithCustomVolumeNames_ReturnsCustomNames()
[Fact] GetCacheConfig_WithCustomMountPaths_ReturnsCustomPaths()
[Fact] GetCacheConfig_WithInvalidVolumeName_ThrowsValidationException()
[Fact] GetMountPath_ForNuGet_ReturnsCorrectPath()
[Fact] GetMountPath_ForNpm_ReturnsCorrectPath()
[Fact] GetMountPath_ForYarn_ReturnsCorrectPath()
[Fact] GetMountPath_ForPnpm_ReturnsCorrectPath()
```

#### PackageManagerDetectorTests

```csharp
[Fact] DetectPackageManagers_WithCsproj_ReturnsNuGet()
[Fact] DetectPackageManagers_WithPackageJson_ReturnsNpm()
[Fact] DetectPackageManagers_WithYarnLock_ReturnsYarn()
[Fact] DetectPackageManagers_WithPnpmLock_ReturnsPnpm()
[Fact] DetectPackageManagers_WithMixedProject_ReturnsBoth()
[Fact] DetectPackageManagers_WithNoProjectFiles_ReturnsEmpty()
[Fact] GetRequiredMounts_ForNuGet_ReturnsNuGetMount()
[Fact] GetRequiredMounts_ForNpm_ReturnsNpmMount()
[Fact] GetRequiredMounts_ForMixedProject_ReturnsBothMounts()
```

#### CacheStatsCollectorTests

```csharp
[Fact] GetStatsAsync_WithPopulatedCache_ReturnsCorrectSize()
[Fact] GetStatsAsync_WithEmptyCache_ReturnsZeroSize()
[Fact] GetStatsAsync_WithNonexistentVolume_ReturnsNotCreatedStatus()
[Fact] GetAllStatsAsync_ReturnsAllManagedCaches()
[Fact] FormatStats_WithBytes_FormatsAsBytes()
[Fact] FormatStats_WithMegabytes_FormatsAsMB()
[Fact] FormatStats_WithGigabytes_FormatsAsGB()
[Fact] ToJson_ReturnsValidJson()
```

#### CacheClearServiceTests

```csharp
[Fact] ClearAllAsync_DeletesAllManagedVolumes()
[Fact] ClearAllAsync_WithRunningContainers_ThrowsVolumeInUseException()
[Fact] ClearAsync_ForSpecificManager_DeletesOnlyThatVolume()
[Fact] ClearAsync_WithForce_SkipsConfirmation()
[Fact] ClearAsync_ReturnsFreedSpaceAmount()
[Fact] ClearAsync_WithDryRun_DoesNotDelete()
[Fact] ClearAsync_RecreatesVolumeIfConfigured()
```

### Integration Tests

#### VolumeLifecycleIntegrationTests

```csharp
[Fact] VolumeLifecycle_CreateMountDelete_CompletesSuccessfully()
[Fact] VolumeLifecycle_DataPersistsAcrossContainers()
[Fact] VolumeLifecycle_ParallelContainerAccess_NoCorruption()
[Fact] VolumeLifecycle_ClearAndRecreate_WorksCorrectly()
```

#### NuGetCacheIntegrationTests

```csharp
[Fact] NuGetCache_FirstRestore_PopulatesCache()
[Fact] NuGetCache_SecondRestore_UsesCachedPackages()
[Fact] NuGetCache_SecondRestore_IsFasterThanFirst()
[Fact] NuGetCache_PackageSignatures_PreservedInCache()
[Fact] NuGetCache_LargePackages_CacheCorrectly()
```

#### NpmCacheIntegrationTests

```csharp
[Fact] NpmCache_FirstInstall_PopulatesCache()
[Fact] NpmCache_SecondInstall_UsesCachedPackages()
[Fact] NpmCache_SecondInstall_IsFasterThanFirst()
[Fact] NpmCache_ScopedPackages_CacheCorrectly()
[Fact] NpmCache_NpmCi_UsesCachedPackages()
```

### E2E Tests

#### CacheCLIE2ETests

```csharp
[Fact] CacheList_ShowsAllManagedVolumes()
[Fact] CacheStats_ShowsSizesAndDates()
[Fact] CacheStats_JsonOutput_IsValidJson()
[Fact] CacheClear_WithConfirmation_ClearsVolumes()
[Fact] CacheClear_WithForce_SkipsConfirmation()
[Fact] CacheClear_Specific_ClearsOnlySpecified()
```

### Performance Benchmarks

| Benchmark | Target | Threshold |
|-----------|--------|-----------|
| VolumeExistsCheck | < 50ms | P95 |
| VolumeCreation | < 500ms | P95 |
| VolumeInspection | < 100ms | P95 |
| CacheStatsCollection | < 2s | P95 |
| DotNetRestoreCached vs Fresh | > 80% improvement | Mean |
| NpmInstallCached vs Fresh | > 80% improvement | Mean |

### Coverage Requirements

| Component | Minimum Coverage |
|-----------|------------------|
| CacheVolumeManager | 95% |
| CacheConfiguration | 90% |
| PackageManagerDetector | 95% |
| CacheStatsCollector | 90% |
| CacheClearService | 90% |
| CLI Commands | 85% |
| **Overall** | **90%** |

---

## User Verification Steps

### Scenario 1: First .NET Build Populates Cache

```bash
# Ensure no existing cache
acode cache clear --nuget --force

# Run a .NET build
acode task run build

# Verify cache was created
acode cache stats

# Expected: NuGet cache shows non-zero size
```

### Scenario 2: Second .NET Build Uses Cache

```bash
# Run build again
time acode task run build

# Compare timing
# Expected: Restore phase completes significantly faster
# Verify packages came from cache (no download messages in output)
```

### Scenario 3: npm Install Uses Cache

```bash
# Clear npm cache
acode cache clear --npm --force

# Run npm install
acode task run install

# Check cache
acode cache stats

# Expected: npm cache shows non-zero size

# Run again
acode task run install

# Expected: Install completes much faster
```

### Scenario 4: Cache Persists Across Sessions

```bash
# Run a build
acode task run build

# Close and reopen terminal
# Run build again

acode task run build

# Expected: Cache still used, fast restore
```

### Scenario 5: Clear Specific Cache

```bash
# Check current state
acode cache stats

# Clear only npm
acode cache clear --npm

# Confirm when prompted

# Verify
acode cache stats

# Expected: npm cache shows 0 bytes, NuGet unchanged
```

### Scenario 6: JSON Stats Output

```bash
# Get JSON output
acode cache stats --json

# Parse with jq
acode cache stats --json | jq '.nuget.size_bytes'

# Expected: Valid JSON, correct structure
```

### Scenario 7: Disabled Caching

```yaml
# Edit .agent/config.yml
sandbox:
  cache_volumes:
    enabled: false
```

```bash
# Run build
acode task run build

# Expected: No cache volumes created, packages downloaded fresh
```

### Scenario 8: Mixed Project Caching

```bash
# In a project with both .csproj and package.json

# Clear all caches
acode cache clear --force

# Run build
acode task run build

# Check caches
acode cache stats

# Expected: Both NuGet and npm/yarn/pnpm caches populated
```

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Sandbox/
│       ├── Caching/
│       │   ├── ICacheVolumeManager.cs
│       │   ├── CacheVolumeConfig.cs
│       │   ├── VolumeInfo.cs
│       │   ├── CacheStats.cs
│       │   └── PackageManagerType.cs
│       └── Detection/
│           └── IPackageManagerDetector.cs
├── Acode.Infrastructure/
│   └── Sandbox/
│       ├── Caching/
│       │   ├── CacheVolumeManager.cs
│       │   ├── CacheConfigurationProvider.cs
│       │   ├── CacheStatsCollector.cs
│       │   └── CacheClearService.cs
│       └── Detection/
│           └── PackageManagerDetector.cs
├── Acode.Cli/
│   └── Commands/
│       └── CacheCommands.cs
└── tests/
    ├── Acode.Domain.Tests/
    │   └── Sandbox/
    │       └── Caching/
    │           └── CacheVolumeConfigTests.cs
    ├── Acode.Infrastructure.Tests/
    │   └── Sandbox/
    │       └── Caching/
    │           ├── CacheVolumeManagerTests.cs
    │           ├── PackageManagerDetectorTests.cs
    │           └── CacheStatsCollectorTests.cs
    └── Acode.Integration.Tests/
        └── Sandbox/
            └── Caching/
                ├── VolumeLifecycleTests.cs
                ├── NuGetCacheTests.cs
                └── NpmCacheTests.cs
```

### Domain Models

```csharp
// ICacheVolumeManager.cs
namespace Acode.Domain.Sandbox.Caching;

public interface ICacheVolumeManager
{
    Task<VolumeInfo> EnsureVolumeAsync(
        PackageManagerType packageManager,
        CancellationToken cancellationToken = default);
    
    Task<VolumeInfo?> GetVolumeInfoAsync(
        string volumeName,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<VolumeInfo>> ListManagedVolumesAsync(
        CancellationToken cancellationToken = default);
    
    Task<long> DeleteVolumeAsync(
        string volumeName,
        bool force = false,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<VolumeMount>> GetRequiredMountsAsync(
        string projectPath,
        CancellationToken cancellationToken = default);
}

// CacheVolumeConfig.cs
namespace Acode.Domain.Sandbox.Caching;

public sealed record CacheVolumeConfig
{
    public bool Enabled { get; init; } = true;
    public NuGetCacheConfig NuGet { get; init; } = new();
    public NpmCacheConfig Npm { get; init; } = new();
    public YarnCacheConfig Yarn { get; init; } = new();
    public PnpmCacheConfig Pnpm { get; init; } = new();
}

public sealed record NuGetCacheConfig
{
    public bool Enabled { get; init; } = true;
    public string VolumeName { get; init; } = "acode-cache-nuget";
    public string MountPath { get; init; } = "/root/.nuget/packages";
}

public sealed record NpmCacheConfig
{
    public bool Enabled { get; init; } = true;
    public string VolumeName { get; init; } = "acode-cache-npm";
    public string MountPath { get; init; } = "/root/.npm";
}

public sealed record YarnCacheConfig
{
    public bool Enabled { get; init; } = true;
    public string VolumeName { get; init; } = "acode-cache-yarn";
    public string MountPath { get; init; } = "/root/.cache/yarn";
}

public sealed record PnpmCacheConfig
{
    public bool Enabled { get; init; } = true;
    public string VolumeName { get; init; } = "acode-cache-pnpm";
    public string MountPath { get; init; } = "/root/.pnpm-store";
}

// VolumeInfo.cs
namespace Acode.Domain.Sandbox.Caching;

public sealed record VolumeInfo
{
    public required string Name { get; init; }
    public required PackageManagerType PackageManager { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public bool Exists { get; init; } = true;
    
    public string FormattedSize => FormatSize(SizeBytes);
    
    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} bytes",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

// VolumeMount.cs
namespace Acode.Domain.Sandbox.Caching;

public sealed record VolumeMount
{
    public required string VolumeName { get; init; }
    public required string ContainerPath { get; init; }
    public bool ReadOnly { get; init; } = false;
    public Dictionary<string, string> EnvironmentVariables { get; init; } = new();
}

// PackageManagerType.cs
namespace Acode.Domain.Sandbox.Caching;

public enum PackageManagerType
{
    NuGet,
    Npm,
    Yarn,
    Pnpm
}

// IPackageManagerDetector.cs
namespace Acode.Domain.Sandbox.Detection;

public interface IPackageManagerDetector
{
    Task<IReadOnlyList<PackageManagerType>> DetectAsync(
        string projectPath,
        CancellationToken cancellationToken = default);
}
```

### Infrastructure Implementation

```csharp
// CacheVolumeManager.cs
namespace Acode.Infrastructure.Sandbox.Caching;

public sealed class CacheVolumeManager : ICacheVolumeManager
{
    private readonly IDockerClientFactory _dockerClientFactory;
    private readonly ICacheConfigurationProvider _configProvider;
    private readonly IPackageManagerDetector _detector;
    private readonly ILogger<CacheVolumeManager> _logger;
    
    private const string VolumePrefix = "acode-cache-";
    
    public CacheVolumeManager(
        IDockerClientFactory dockerClientFactory,
        ICacheConfigurationProvider configProvider,
        IPackageManagerDetector detector,
        ILogger<CacheVolumeManager> logger)
    {
        _dockerClientFactory = dockerClientFactory;
        _configProvider = configProvider;
        _detector = detector;
        _logger = logger;
    }
    
    public async Task<VolumeInfo> EnsureVolumeAsync(
        PackageManagerType packageManager,
        CancellationToken cancellationToken = default)
    {
        var config = _configProvider.GetConfig();
        var volumeName = GetVolumeName(packageManager, config);
        
        using var client = _dockerClientFactory.Create();
        
        try
        {
            var existing = await client.Volumes.InspectAsync(volumeName, cancellationToken);
            _logger.LogDebug("Volume {VolumeName} already exists", volumeName);
            
            return new VolumeInfo
            {
                Name = volumeName,
                PackageManager = packageManager,
                SizeBytes = await GetVolumeSizeAsync(client, volumeName, cancellationToken),
                CreatedAt = DateTimeOffset.Parse(existing.CreatedAt),
                Exists = true
            };
        }
        catch (DockerContainerNotFoundException)
        {
            _logger.LogInformation("Creating cache volume {VolumeName}", volumeName);
            
            await client.Volumes.CreateAsync(new VolumesCreateParameters
            {
                Name = volumeName,
                Labels = new Dictionary<string, string>
                {
                    ["managed-by"] = "acode",
                    ["package-manager"] = packageManager.ToString().ToLowerInvariant()
                }
            }, cancellationToken);
            
            return new VolumeInfo
            {
                Name = volumeName,
                PackageManager = packageManager,
                SizeBytes = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                Exists = true
            };
        }
    }
    
    public async Task<IReadOnlyList<VolumeMount>> GetRequiredMountsAsync(
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        var config = _configProvider.GetConfig();
        if (!config.Enabled)
        {
            _logger.LogDebug("Cache volumes disabled globally");
            return Array.Empty<VolumeMount>();
        }
        
        var packageManagers = await _detector.DetectAsync(projectPath, cancellationToken);
        var mounts = new List<VolumeMount>();
        
        foreach (var pm in packageManagers)
        {
            var mount = CreateMount(pm, config);
            if (mount is not null)
            {
                await EnsureVolumeAsync(pm, cancellationToken);
                mounts.Add(mount);
            }
        }
        
        return mounts;
    }
    
    private VolumeMount? CreateMount(PackageManagerType pm, CacheVolumeConfig config)
    {
        return pm switch
        {
            PackageManagerType.NuGet when config.NuGet.Enabled => new VolumeMount
            {
                VolumeName = config.NuGet.VolumeName,
                ContainerPath = config.NuGet.MountPath,
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["NUGET_PACKAGES"] = config.NuGet.MountPath
                }
            },
            PackageManagerType.Npm when config.Npm.Enabled => new VolumeMount
            {
                VolumeName = config.Npm.VolumeName,
                ContainerPath = config.Npm.MountPath,
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["npm_config_cache"] = config.Npm.MountPath
                }
            },
            PackageManagerType.Yarn when config.Yarn.Enabled => new VolumeMount
            {
                VolumeName = config.Yarn.VolumeName,
                ContainerPath = config.Yarn.MountPath,
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["YARN_CACHE_FOLDER"] = config.Yarn.MountPath
                }
            },
            PackageManagerType.Pnpm when config.Pnpm.Enabled => new VolumeMount
            {
                VolumeName = config.Pnpm.VolumeName,
                ContainerPath = config.Pnpm.MountPath,
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["PNPM_HOME"] = "/root/.local/share/pnpm"
                }
            },
            _ => null
        };
    }
    
    private static string GetVolumeName(PackageManagerType pm, CacheVolumeConfig config)
    {
        return pm switch
        {
            PackageManagerType.NuGet => config.NuGet.VolumeName,
            PackageManagerType.Npm => config.Npm.VolumeName,
            PackageManagerType.Yarn => config.Yarn.VolumeName,
            PackageManagerType.Pnpm => config.Pnpm.VolumeName,
            _ => throw new ArgumentOutOfRangeException(nameof(pm))
        };
    }
    
    private async Task<long> GetVolumeSizeAsync(
        DockerClient client,
        string volumeName,
        CancellationToken cancellationToken)
    {
        // Docker doesn't provide volume size directly
        // Use system df or estimate from container inspection
        try
        {
            var df = await client.System.GetSystemInfoAsync(cancellationToken);
            // Parse volume usage from system info
            return 0; // Placeholder - actual implementation varies
        }
        catch
        {
            return 0;
        }
    }
}

// PackageManagerDetector.cs
namespace Acode.Infrastructure.Sandbox.Detection;

public sealed class PackageManagerDetector : IPackageManagerDetector
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<PackageManagerDetector> _logger;
    
    public PackageManagerDetector(
        IFileSystem fileSystem,
        ILogger<PackageManagerDetector> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }
    
    public Task<IReadOnlyList<PackageManagerType>> DetectAsync(
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        var detected = new List<PackageManagerType>();
        
        // Detect .NET projects
        if (_fileSystem.Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories).Any() ||
            _fileSystem.Directory.GetFiles(projectPath, "*.fsproj", SearchOption.AllDirectories).Any() ||
            _fileSystem.Directory.GetFiles(projectPath, "*.sln", SearchOption.AllDirectories).Any())
        {
            detected.Add(PackageManagerType.NuGet);
            _logger.LogDebug("Detected .NET project, will mount NuGet cache");
        }
        
        // Detect Node.js projects
        var packageJsonPath = Path.Combine(projectPath, "package.json");
        if (_fileSystem.File.Exists(packageJsonPath))
        {
            if (_fileSystem.File.Exists(Path.Combine(projectPath, "pnpm-lock.yaml")))
            {
                detected.Add(PackageManagerType.Pnpm);
                _logger.LogDebug("Detected pnpm project");
            }
            else if (_fileSystem.File.Exists(Path.Combine(projectPath, "yarn.lock")))
            {
                detected.Add(PackageManagerType.Yarn);
                _logger.LogDebug("Detected yarn project");
            }
            else
            {
                detected.Add(PackageManagerType.Npm);
                _logger.LogDebug("Detected npm project");
            }
        }
        
        return Task.FromResult<IReadOnlyList<PackageManagerType>>(detected);
    }
}
```

### CLI Commands

```csharp
// CacheCommands.cs
namespace Acode.Cli.Commands;

[Command("cache", Description = "Manage package manager caches")]
public class CacheCommand
{
    [Command("list", Description = "List all managed cache volumes")]
    public async Task<int> ListAsync(
        ICacheVolumeManager cacheManager,
        IConsole console)
    {
        var volumes = await cacheManager.ListManagedVolumesAsync();
        
        console.WriteLine("VOLUME                  SIZE      CREATED          LAST USED");
        
        foreach (var vol in volumes)
        {
            var created = vol.Exists ? vol.CreatedAt.ToString("yyyy-MM-dd HH:mm") : "(not created)";
            var lastUsed = vol.LastUsedAt?.ToString("yyyy-MM-dd HH:mm") ?? "(never)";
            var size = vol.Exists ? vol.FormattedSize : "0 bytes";
            
            console.WriteLine($"{vol.Name,-23} {size,-9} {created,-16} {lastUsed}");
        }
        
        return 0;
    }
    
    [Command("stats", Description = "Show cache statistics")]
    public async Task<int> StatsAsync(
        ICacheStatsCollector statsCollector,
        IConsole console,
        [Option("json")] bool json = false)
    {
        var stats = await statsCollector.GetAllStatsAsync();
        
        if (json)
        {
            console.WriteLine(JsonSerializer.Serialize(stats, JsonOptions.Pretty));
            return 0;
        }
        
        console.WriteLine("Cache Statistics");
        console.WriteLine("================");
        console.WriteLine();
        
        foreach (var stat in stats)
        {
            console.WriteLine($"{stat.PackageManager} Cache ({stat.VolumeName})");
            console.WriteLine($"  Size:           {stat.FormattedSize}");
            console.WriteLine($"  Created:        {stat.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            console.WriteLine($"  Last Used:      {stat.LastUsedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(never)"}");
            console.WriteLine();
        }
        
        var total = stats.Sum(s => s.SizeBytes);
        console.WriteLine($"Total Cache Size: {FormatSize(total)}");
        
        return 0;
    }
    
    [Command("clear", Description = "Clear package manager caches")]
    public async Task<int> ClearAsync(
        ICacheClearService clearService,
        IConsole console,
        [Option("force")] bool force = false,
        [Option("nuget")] bool nuget = false,
        [Option("npm")] bool npm = false,
        [Option("yarn")] bool yarn = false,
        [Option("pnpm")] bool pnpm = false,
        [Option("dry-run")] bool dryRun = false)
    {
        var targets = new List<PackageManagerType>();
        
        if (nuget) targets.Add(PackageManagerType.NuGet);
        if (npm) targets.Add(PackageManagerType.Npm);
        if (yarn) targets.Add(PackageManagerType.Yarn);
        if (pnpm) targets.Add(PackageManagerType.Pnpm);
        
        // If none specified, clear all
        if (targets.Count == 0)
        {
            targets.AddRange(Enum.GetValues<PackageManagerType>());
        }
        
        var preview = await clearService.PreviewClearAsync(targets);
        
        console.WriteLine("This will delete the following cache volumes:");
        foreach (var item in preview)
        {
            console.WriteLine($"  - {item.VolumeName} ({item.FormattedSize})");
        }
        console.WriteLine();
        console.WriteLine($"Total space to be freed: {FormatSize(preview.Sum(p => p.SizeBytes))}");
        
        if (dryRun)
        {
            console.WriteLine();
            console.WriteLine("(Dry run - no changes made)");
            return 0;
        }
        
        if (!force)
        {
            console.Write("Are you sure? [y/N]: ");
            var response = Console.ReadLine();
            if (!response?.Equals("y", StringComparison.OrdinalIgnoreCase) ?? true)
            {
                console.WriteLine("Cancelled.");
                return 0;
            }
        }
        
        var result = await clearService.ClearAsync(targets);
        
        console.WriteLine();
        foreach (var item in result)
        {
            console.WriteLine($"✓ Cleared {item.VolumeName} (freed {item.FormattedSize})");
        }
        console.WriteLine();
        console.WriteLine($"Total freed: {FormatSize(result.Sum(r => r.FreedBytes))}");
        
        return 0;
    }
}
```

### Error Codes

| Code | Meaning | Recovery |
|------|---------|----------|
| ACODE-VOL-001 | Volume creation failed | Check Docker permissions, disk space |
| ACODE-VOL-002 | Volume mount failed | Verify volume exists, check container config |
| ACODE-VOL-003 | Volume deletion failed | Check if volume in use by containers |
| ACODE-VOL-004 | Volume in use by running container | Stop containers first, or use --force |
| ACODE-VOL-005 | Invalid volume name | Use alphanumeric, dash, underscore only |
| ACODE-VOL-006 | Docker daemon unavailable | Start Docker daemon |
| ACODE-VOL-007 | Disk space exhausted | Clear caches, free disk space |
| ACODE-VOL-008 | Volume inspection failed | Volume may be corrupted, clear and recreate |
| ACODE-VOL-009 | Cache disabled in configuration | Enable in agent-config.yml if desired |
| ACODE-VOL-010 | Package manager not detected | Ensure project files exist in path |

### Implementation Checklist

- [ ] Create `ICacheVolumeManager` interface in Domain layer
- [ ] Create `CacheVolumeConfig` and related config records
- [ ] Create `VolumeInfo` and `VolumeMount` domain models
- [ ] Create `PackageManagerType` enum
- [ ] Create `IPackageManagerDetector` interface
- [ ] Implement `CacheVolumeManager` in Infrastructure layer
- [ ] Implement `PackageManagerDetector` for project type detection
- [ ] Implement `CacheConfigurationProvider` for config loading
- [ ] Implement `CacheStatsCollector` for statistics
- [ ] Implement `CacheClearService` for cache clearing
- [ ] Integrate volume mounts with `ContainerLifecycleManager`
- [ ] Create CLI `cache` command group
- [ ] Implement `cache list` command
- [ ] Implement `cache stats` command with JSON support
- [ ] Implement `cache clear` command with confirmation
- [ ] Add unit tests for all components
- [ ] Add integration tests for volume lifecycle
- [ ] Add integration tests for NuGet and npm caching
- [ ] Add E2E tests for CLI commands
- [ ] Document cache configuration in user manual

### Rollout Plan

| Phase | Action | Validation |
|-------|--------|------------|
| 1 | Implement domain models and interfaces | Unit tests pass |
| 2 | Implement PackageManagerDetector | Detection tests pass |
| 3 | Implement CacheVolumeManager | Volume lifecycle tests pass |
| 4 | Integrate with ContainerLifecycleManager | Containers mount volumes |
| 5 | Implement stats and clear services | Stats/clear tests pass |
| 6 | Add CLI commands | E2E tests pass |
| 7 | Performance validation | Cached builds are faster |
| 8 | Documentation and release | User manual complete |

---

**End of Task 020.b Specification**