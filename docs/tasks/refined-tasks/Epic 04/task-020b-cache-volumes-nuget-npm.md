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

### ROI Calculation

**Problem Cost (No Caching):**
- **NuGet Restore Time**: Entity Framework (35MB), ASP.NET Core (120MB), Newtonsoft.Json (2MB) = 157MB downloaded per build
- **npm Install Time**: React (45MB), webpack (25MB), babel (18MB), eslint (12MB) = 100MB downloaded per build
- **Build Frequency**: 50 engineers × 10 builds/day = 500 builds/day
- **Network Cost**: 500 builds × 257MB = 128.5GB/day = 3,855GB/month
- **Time Cost**: 3 minutes restore per build × 500 builds = 1,500 minutes/day = 25 hours/day wasted
- **Developer Cost**: $75/hour average × 25 hours/day × 22 working days = $41,250/month
- **Bandwidth Cost**: 3,855GB × $0.12/GB = $462.60/month (AWS data transfer out pricing)

**Solution Cost (With Caching):**
- **First Build**: Full download (3 minutes)
- **Subsequent Builds**: Cache hit (5 seconds for verification)
- **Cache Storage**: 10GB Docker volumes = $1/month (local storage)
- **Implementation Cost**: 60 hours × $100/hour = $6,000 (one-time)

**ROI Metrics:**
| Metric | Before (No Cache) | After (With Cache) | Savings | ROI % |
|--------|-------------------|--------------------|---------| ------|
| Monthly Developer Time | 550 hours (25/day × 22 days) | 36.7 hours (cache misses only) | 513.3 hours | **93.3%** |
| Monthly Developer Cost | $41,250 | $2,752 | **$38,498/month** | **93.3%** |
| Monthly Bandwidth Cost | $462.60 | $50 (cache misses) | $412.60/month | **89.2%** |
| **Total Monthly Savings** | - | - | **$38,910.60** | **93.1%** |
| **Annual Savings** | - | - | **$466,927** | - |
| **Payback Period** | - | - | **0.46 days** (3.7 hours) | **2,028%** first year |

**Break-Even Analysis:**
- Implementation cost: $6,000
- Monthly savings: $38,910.60
- Break-even: $6,000 / $38,910.60 = **0.154 months = 4.6 days**

After less than 5 days of operation, the cache volumes pay for themselves completely. Every subsequent day generates $1,768 in savings.

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                          Agent CLI Layer                            │
│  Commands: cache list, cache prune, cache stats                    │
└─────────────────────┬───────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────────┐
│                   CacheVolumeManager (Infrastructure)               │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │ CreateVolumeAsync(packageManager: string)                  │    │
│  │  ├─ Check existence: Docker.Volumes.Inspect()             │    │
│  │  ├─ If not exists: Docker.Volumes.Create()               │    │
│  │  └─ Return: VolumeInfo                                    │    │
│  ├────────────────────────────────────────────────────────────┤    │
│  │ GetMountConfig(packageManager: string): Mount[]            │    │
│  │  ├─ NuGet    → /root/.nuget/packages                     │    │
│  │  ├─ npm      → /root/.npm                                │    │
│  │  ├─ yarn     → /usr/local/share/.cache/yarn              │    │
│  │  └─ pnpm     → /root/.local/share/pnpm/store             │    │
│  ├────────────────────────────────────────────────────────────┤    │
│  │ PruneUnusedVolumesAsync()                                 │    │
│  │ GetStatisticsAsync(): CacheStats                          │    │
│  └────────────────────────────────────────────────────────────┘    │
└─────────────────────┬───────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────────┐
│                  Docker.DotNet Client (API)                         │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │ Volumes.CreateVolumeAsync(params)                          │    │
│  │ Volumes.InspectVolumeAsync(name)                          │    │
│  │ Volumes.ListVolumesAsync(filters)                         │    │
│  │ Volumes.RemoveVolumeAsync(name, force: true)              │    │
│  └────────────────────────────────────────────────────────────┘    │
└─────────────────────┬───────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       Docker Daemon (Host)                          │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ Named Volumes (Persistent Storage)                          │   │
│  │                                                             │   │
│  │  acode-cache-nuget/        (Size: 2.3GB)                  │   │
│  │  ├─ microsoft.entityframeworkcore.8.0.0/                  │   │
│  │  ├─ newtonsoft.json.13.0.3/                               │   │
│  │  └─ [1,247 other packages]                                │   │
│  │                                                             │   │
│  │  acode-cache-npm/          (Size: 4.1GB)                  │   │
│  │  ├─ react@18.2.0/                                         │   │
│  │  ├─ webpack@5.88.0/                                       │   │
│  │  └─ [8,392 other packages]                                │   │
│  │                                                             │   │
│  │  acode-cache-yarn/         (Size: 3.7GB)                  │   │
│  │  acode-cache-pnpm/         (Size: 2.1GB)                  │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────┬───────────────────────────────────────────────┘
                      │
                      ▼ (Volume Mount)
┌─────────────────────────────────────────────────────────────────────┐
│            Per-Task Container (Task 020.a)                          │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │ Container ID: acode-abc123-task-build                      │    │
│  │                                                             │    │
│  │ Mounts:                                                     │    │
│  │  - /workspace (bind mount from host repo)                  │    │
│  │  - /root/.nuget/packages (volume: acode-cache-nuget)      │    │
│  │  - /root/.npm (volume: acode-cache-npm)                   │    │
│  │                                                             │    │
│  │ Process: dotnet restore MyProject.csproj                   │    │
│  │  ├─ Checks /root/.nuget/packages first                    │    │
│  │  ├─ Cache HIT: Loads from volume (5 seconds)              │    │
│  │  └─ Cache MISS: Downloads, saves to volume (3 minutes)    │    │
│  └────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘

Data Flow:
1. Agent creates container for build task
2. CacheVolumeManager.CreateVolumeAsync("nuget") ensures volume exists
3. CacheVolumeManager.GetMountConfig("nuget") returns mount specification
4. ContainerLifecycleManager applies mounts during container creation
5. Package manager (dotnet restore / npm install) checks mounted cache first
6. On cache HIT: Restore completes in 5 seconds (95% faster)
7. On cache MISS: Downloads package, writes to mounted volume for future use
8. Volume persists after container removal (Task 020.a ephemeral containers)
9. Next container reuses same volume, sees all previously cached packages
```

### Trade-Offs and Design Decisions

#### Trade-Off 1: Named Volumes vs Bind Mounts

**Decision:** Use Docker named volumes instead of bind mounts from host filesystem.

**Pros:**
- Docker manages volume lifecycle (creation, deletion, disk space)
- Volumes work identically on Windows, macOS, and Linux
- Volume driver abstraction enables future cloud storage backends
- Volumes have better performance on Docker Desktop (Mac/Windows)
- Docker CLI provides native volume inspection commands

**Cons:**
- Volumes are less transparent than filesystem directories
- Cannot `ls` volume contents directly from host (need `docker run` inspection)
- Volume paths are Docker-managed, not user-controlled
- Backup/restore requires Docker commands, not standard file tools

**Rationale:** Portability and Docker-native management outweigh transparency loss. For enterprise users needing direct access, bind mount option can be added via configuration flag.

#### Trade-Off 2: Per-Package-Manager Volumes vs Single Unified Cache

**Decision:** Separate volumes per package manager (`acode-cache-nuget`, `acode-cache-npm`).

**Pros:**
- Isolation prevents package manager conflicts (e.g., NuGet vs npm path collisions)
- Selective cache clearing (clear only npm cache without affecting NuGet)
- Size tracking per package manager (report "npm cache: 4.1GB, NuGet cache: 2.3GB")
- Parallel builds can't race on cache directory locks
- Different retention policies per manager (keep NuGet 90 days, npm 30 days)

**Cons:**
- More volumes to manage (4 volumes vs 1)
- Slight overhead in volume creation (4 API calls vs 1)
- More complex configuration schema
- Higher disk space fragmentation

**Rationale:** Isolation and granular control are more valuable than simplicity. Four volumes are still manageable at scale.

#### Trade-Off 3: Automatic Volume Creation vs Explicit User Command

**Decision:** Automatically create volumes on first use, with CLI commands for manual management.

**Pros:**
- Zero-configuration for 95% of users
- No "cache not initialized" errors
- Volumes appear transparently when needed
- Better onboarding experience (works out-of-box)

**Cons:**
- Surprise disk usage (users don't explicitly opt-in)
- Potential permission issues on first run
- Harder to debug ("where did this volume come from?")

**Rationale:** Ease of use trumps explicitness. Users expect caching to "just work" like their local package managers already do. Power users can disable via configuration.

#### Trade-Off 4: Cache Invalidation Strategy

**Decision:** Manual cache clearing via CLI commands, no automatic expiration.

**Pros:**
- Predictable behavior (cache stays unless user clears it)
- No surprise cache misses from automatic pruning
- Maximum cache hit rate
- No background processes needed for cleanup

**Cons:**
- Disk space can grow unbounded if user never prunes
- Old/unused packages accumulate indefinitely
- No freshness guarantee (packages could be months old)

**Rationale:** Package managers (NuGet, npm) already have their own freshness checks. Docker volume persistence should be dumb storage. Users who need control can run `acode cache prune`.

#### Trade-Off 5: Cache Sharing Across Projects vs Per-Project Caches

**Decision:** Share cache volumes across all projects on the same host.

**Pros:**
- Maximum cache efficiency (React downloaded once, used in 10 projects)
- Lower disk usage (no duplicate packages)
- Faster onboarding for new projects (cache already warm from other projects)
- Matches behavior of global package manager caches (~/.nuget, ~/.npm)

**Cons:**
- Package conflicts if two projects need different versions (rare, package managers handle this)
- One project's corrupted cache affects all projects
- No project-level cache isolation for security

**Rationale:** Efficiency and standard package manager behavior outweigh isolation concerns. If project-specific caches are needed, use custom volume name prefix in configuration.

---

## Use Cases

### Use Case 1: Samantha (Full-Stack Developer) - Eliminating 3-Minute Restore Waits

**Persona:** Samantha is a full-stack developer working on a microservices e-commerce platform. She switches between 5 different services daily (payment-service, inventory-service, user-service, notification-service, reporting-service), each with its own set of NuGet and npm dependencies.

**Problem (Before):**
Every time Samantha runs `dotnet restore` or `npm install` in a fresh container, packages download from scratch. Payment-service pulls 45 NuGet packages (Entity Framework Core, ASP.NET, FluentValidation, MediatR) taking 2.5 minutes. The frontend pulls 320 npm packages (React, Redux, webpack, babel) taking 3.5 minutes. She runs 8 builds per day across services = 8 × 3 minutes = 24 minutes/day waiting for package downloads.

**Annual Cost (Before):**
- **Waiting Time:** 24 minutes/day × 250 working days = 6,000 minutes/year = 100 hours/year
- **Developer Cost:** 100 hours × $85/hour = $8,500/year (Samantha's time wasted)
- **Frustration Impact:** Context switching during waits reduces productivity by 15% = additional $12,750 productivity loss
- **Total Annual Cost:** $21,250/year for one developer

**Solution (After):**
Cache volumes persist NuGet packages in `acode-cache-nuget` and npm packages in `acode-cache-npm`. First build takes 3 minutes (cold cache). All subsequent builds for the same service take 5 seconds (cache hit). Even switching between services takes 5 seconds because packages are shared across all projects on her machine.

**Annual Cost (After):**
- **First Build Per Day:** 1 × 3 minutes = 3 minutes (one cold start)
- **Subsequent Builds:** 7 × 5 seconds = 35 seconds (all cache hits)
- **Total Daily Time:** 3.58 minutes/day × 250 days = 14.9 hours/year
- **Developer Cost:** 14.9 hours × $85/hour = $1,267/year
- **Total Annual Cost:** $1,267/year

**ROI Metrics:**
- **Annual Savings:** $21,250 - $1,267 = $19,983/year
- **Time Savings:** 100 hours - 14.9 hours = 85.1 hours/year (85% reduction)
- **Context Switches Eliminated:** 1,750 waits/year reduced to 250 waits/year (86% reduction)
- **Productivity Gain:** Eliminating 1,500 interruptions = 15% productivity recovery = $12,750/year
- **Total ROI:** $19,983 + $12,750 = $32,733/year for one developer

### Use Case 2: DevOps Team (5 engineers) - CI/CD Pipeline Acceleration

**Persona:** A DevOps team manages CI/CD pipelines for 30 microservices. Each service has its own build pipeline running in ephemeral Docker containers. The team runs 500 builds/day across all services (production deployments, PR builds, nightly builds, manual builds).

**Problem (Before):**
Every CI build creates a fresh container and downloads all packages from scratch. Average build time: 8 minutes (3 min package restore + 5 min compile/test). With 500 builds/day, package restoration alone consumes 500 × 3 = 1,500 minutes/day = 25 hours/day of CI compute time. CI runners are billed at $0.08/minute = 1,500 × $0.08 = $120/day = $2,640/month for package downloads alone.

**Annual Cost (Before):**
- **CI Compute Time:** 25 hours/day × 22 days/month × 12 months = 6,600 hours/year
- **CI Compute Cost:** 1,500 minutes/day × $0.08/min × 22 days × 12 months = **$31,680/year**
- **Developer Waiting Time:** 8 min/build × 100 PR builds/day × 22 days × 12 months = 211,200 minutes = 3,520 hours
- **Developer Waiting Cost:** 3,520 hours × $85/hour = **$299,200/year**
- **Total Annual Cost:** $330,880/year

**Solution (After):**
CI runners use Docker cache volumes mounted at build time. The first build for each service populates the cache (3 minutes). All subsequent builds (99% of builds) hit the cache and restore in 5 seconds. Average build time drops to 5.08 minutes (5 sec restore + 5 min compile/test). CI compute time for packages: 500 × 5 seconds = 2,500 seconds/day = 42 minutes/day.

**Annual Cost (After):**
- **CI Compute Time (Cache):** 42 minutes/day × 22 days × 12 months = 11,088 minutes/year = 185 hours/year
- **CI Compute Cost (Cache):** 42 min/day × $0.08/min × 22 days × 12 months = **$888/year**
- **CI Compute Time (First Builds):** 5 builds/day × 3 min × 22 days × 12 months = 3,960 minutes = 66 hours/year
- **CI Compute Cost (First Builds):** 3,960 min × $0.08 = **$317/year**
- **Developer Waiting Time:** 5.08 min/build × 100 PR builds/day × 22 days × 12 months = 134,112 minutes = 2,235 hours/year
- **Developer Waiting Cost:** 2,235 hours × $85/hour = **$189,975/year**
- **Total Annual Cost:** $191,180/year

**ROI Metrics:**
- **CI Compute Savings:** $31,680 - $1,205 = $30,475/year (96% reduction)
- **Developer Time Savings:** $299,200 - $189,975 = $109,225/year (36% reduction)
- **Total Annual Savings:** **$139,700/year**
- **Build Time Reduction:** 8 min → 5.08 min (36.5% faster)
- **Feedback Loop Improvement:** Developers get PR results 3 minutes faster = 100 PRs/day × 3 min × 250 days = 1,250 hours/year recovered
- **ROI:** ($139,700 / $6,000 implementation cost) = **2,328% first year**

### Use Case 3: Sarah (Open Source Contributor) - Offline Development Enablement

**Persona:** Sarah contributes to open source projects during her daily train commute (2 hours/day). Her commute has spotty internet connectivity. She works on .NET libraries and React applications, frequently cloning new repositories and experimenting with dependencies.

**Problem (Before):**
Without cache volumes, every `git clone` + `dotnet restore` + `npm install` requires full package downloads. On the train, downloads timeout or fail mid-stream. She can only work on projects she's already set up at home with good Wi-Fi. This limits her to 2-3 pre-configured projects, reducing her contribution opportunities by 70%.

**Annual Cost (Before):**
- **Failed Package Restores:** 5 attempts/day × 3 minutes wasted = 15 minutes/day
- **Limited Project Access:** Can only work on 30% of desired projects = 70% contribution opportunity lost
- **Annual Contribution Loss:** 250 days × 2 hours × 70% = 350 hours/year unproductive
- **Value of Lost Contributions:** 350 hours × $75/hour (volunteer equivalent value) = $26,250/year opportunity cost

**Solution (After):**
With cache volumes, Sarah pre-populates caches at home before her commute. On the train, `dotnet restore` and `npm install` complete instantly from cached packages (no network needed). She can work on any project she's previously restored, expanding her project access from 3 to 20+ projects. Even new projects benefit from shared dependencies (React, Entity Framework) already in cache from other projects.

**Annual Cost (After):**
- **Pre-Population Time:** 10 minutes/week at home = 520 minutes/year = 8.67 hours/year
- **Cache Storage:** 15GB local Docker volumes = $0 (local disk)
- **Failed Package Restores:** 0 (everything cached)
- **Project Access:** 100% (no network dependency after initial cache)
- **Total Annual Cost:** 8.67 hours × $75/hour = $650/year

**ROI Metrics:**
- **Annual Savings:** $26,250 - $650 = $25,600/year
- **Productivity Gain:** 350 hours/year × 100% = 350 additional contribution hours
- **Project Diversity:** 3 projects → 20+ projects (566% increase in accessible repositories)
- **Offline Capability:** 0% offline work → 95% offline work (only initial clone needs network)
- **Community Impact:** 350 additional hours of open source contributions = ~70 merged PRs/year = $175,000 community value (at $500/PR average impact)

**Aggregate ROI Summary for Cache Volumes:**
| Stakeholder | Annual Savings | Time Savings | ROI % |
|-------------|----------------|--------------|-------|
| Samantha (Developer) | $32,733 | 85 hours/year | **545%** |
| DevOps Team | $139,700 | 1,285 hours/year | **2,328%** |
| Sarah (OSS Contributor) | $25,600 | 350 hours/year | **4,267%** |
| **Total (3 personas)** | **$198,033** | **1,720 hours** | **3,301% avg** |

---

## Glossary

| Term | Definition |
|------|------------|
| **Cache Volume** | A named Docker volume that persists package manager downloads across container lifecycles, enabling package reuse without re-downloading |
| **Named Volume** | A Docker volume identified by name (e.g., `acode-cache-nuget`) rather than an anonymous hash, managed by Docker daemon |
| **Bind Mount** | A host filesystem directory mounted directly into a container, providing real-time synchronization (NOT used for caches in this task) |
| **Package Manager** | Software tool that automates installing, upgrading, configuring software dependencies (NuGet, npm, yarn, pnpm) |
| **NuGet** | Microsoft's package manager for .NET, stores packages in `~/.nuget/packages` by default |
| **npm** | Node Package Manager for JavaScript, caches downloads in `~/.npm` directory |
| **yarn** | Facebook's alternative to npm, caches in `/usr/local/share/.cache/yarn` or `~/.yarn/cache` |
| **pnpm** | Performant npm, uses content-addressable storage at `~/.local/share/pnpm/store` |
| **Cache Hit** | Successful package restoration from cache without network download (restore time: ~5 seconds) |
| **Cache Miss** | Failed cache lookup requiring full package download from registry (restore time: ~3 minutes) |
| **Cache Invalidation** | Explicitly clearing cached packages to force fresh downloads, useful after corruption or security issues |
| **Cache Warming** | Pre-populating cache volumes with commonly used packages before disconnecting from network |
| **Ephemeral Container** | Short-lived container from Task 020.a that is removed after task completion; caches must persist beyond this |
| **Package Restore** | Operation that downloads and installs dependencies listed in project manifests (packages.config, package.json) |
| **Registry** | Central server hosting packages for download (nuget.org, npmjs.com, private registries) |
| **Tarball** | Compressed archive file format (.tgz) used by npm to distribute packages |
| **Content-Addressable Storage** | Storage system where files are identified by hash of their content, enabling deduplication (used by pnpm) |
| **Scoped Package** | npm package namespaced to an organization (e.g., `@angular/core`, `@types/node`) |
| **Volume Driver** | Docker plugin that implements storage backend for volumes (local, nfs, cloud providers) |
| **Volume Pruning** | Removing unused Docker volumes to reclaim disk space |

---

## Out of Scope

The following features and capabilities are explicitly **NOT** included in this task:

1. **Build Artifact Caching** - Compiled binaries, `.dll` files, intermediate build outputs are NOT cached (separate concern for Task 021)
2. **Source Code Caching** - Repository files are mounted as bind mounts from Task 020.a, not stored in volumes
3. **Custom Registry Authentication** - Private npm/NuGet registry credentials are NOT managed by cache layer (assumes public registries or pre-configured auth)
4. **Cross-Platform Cache Sharing** - Caches are local to the Docker host; no network-shared volumes or distributed cache coordination
5. **Cache Encryption** - Cached packages are stored unencrypted; disk encryption is a host-level concern
6. **Automatic Cache Expiration** - No TTL-based auto-pruning; manual cache clearing only
7. **Cache Analytics Dashboard** - No web UI for cache statistics; CLI commands only
8. **Cache Hit Rate Metrics Collection** - No telemetry tracking how often caches are hit vs missed (future enhancement)
9. **Selective Package Caching** - All packages for a manager are cached or none; no allowlist/denylist per package
10. **Cache Backup/Restore** - No built-in backup mechanism; users must use `docker volume` commands or host-level backup
11. **Multi-Version Package Retention Policies** - Package managers handle multi-version storage internally; we don't impose additional policies
12. **Cache Compression** - Volumes store packages as-is; no additional compression layer
13. **Other Package Managers** - Only NuGet, npm, yarn, pnpm supported; Maven, pip, Cargo, Go modules are OUT OF SCOPE
14. **IDE Integration** - No Visual Studio / VS Code extensions for cache management; CLI only
15. **Cache Preheating from lock files** - No proactive download of all packages from `package-lock.json` before first build (packages are cached lazily on first use)

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

## Assumptions

### Technical Assumptions

1. **Docker Named Volumes Available** - Docker daemon supports named volumes (Docker Engine 1.9+)
2. **Docker.DotNet Compatibility** - Docker.DotNet client library works with target Docker API version (v1.41+)
3. **Volume Driver Default** - Default `local` volume driver is sufficient for cache storage (no exotic drivers required)
4. **Filesystem Support** - Host filesystem supports the volume size requirements (ext4, NTFS, APFS all compatible)
5. **Volume Atomicity** - Docker volume creation/deletion operations are atomic (no partial volumes)
6. **Concurrent Access Safe** - Docker volumes support concurrent read/write from multiple containers without corruption
7. **Package Manager Cache Directories** - Standard cache locations are stable across package manager versions:
   - NuGet: `/root/.nuget/packages` (NuGet 3.0+)
   - npm: `/root/.npm` (npm 5.0+)
   - yarn: `/root/.cache/yarn` (Yarn 1.x and 2.x+)
   - pnpm: `/root/.pnpm-store` (pnpm 4.0+)

### Operational Assumptions

8. **Docker Daemon Running** - Docker daemon is available and running when cache operations are invoked
9. **Sufficient Disk Space** - Host system has adequate disk space for cache volumes (recommend 10GB+ free)
10. **No Manual Volume Deletion** - Users do not manually delete managed volumes outside of `acode cache clear`
11. **Volume Naming Uniqueness** - Volume names `acode-cache-{manager}` do not conflict with existing volumes
12. **Container Lifecycle Ephemeral** - Containers are destroyed after each task per Task 020.a (caches persist)
13. **Single Acode Instance per Host** - Only one acode instance manages cache volumes on a given Docker host (no multi-tenancy)
14. **Network Access for Downloads** - Initial package downloads require network access to registries (NuGet.org, npmjs.com, etc.)

### Integration Assumptions

15. **ContainerLifecycleManager Integration** - Task 020.a's `ContainerLifecycleManager` supports injecting volume mounts before container start
16. **Configuration Provider Available** - `IConfigurationProvider` from Task 002 is available for reading `agent-config.yml`
17. **Project Type Detection** - File system access is available to detect project files (`.csproj`, `package.json`, lock files)
18. **Environment Variable Injection** - Container runtime supports setting environment variables (`NUGET_PACKAGES`, `npm_config_cache`)
19. **Logger Availability** - `ILogger<T>` from Microsoft.Extensions.Logging is available for diagnostic logging
20. **Error Code Registry** - Task 021's error code system is available for volume operation failures (ACODE-VOL-XXX codes)

---

## Security Considerations

### Threat 1: Volume Name Injection

**Risk Description:** An attacker could manipulate configuration or user input to inject malicious Docker volume names containing shell metacharacters, path traversal sequences, or names that conflict with system volumes, potentially leading to unauthorized access or system compromise.

**Attack Scenario:**
1. Attacker modifies `agent-config.yml` to set `volume_name: "acode-cache-nuget; docker exec -it $(docker ps -q) /bin/sh"`
2. System passes unsanitized volume name to Docker API
3. If Docker CLI is used (instead of SDK), shell injection occurs
4. Attacker gains shell access to running containers

**Mitigation:**

```csharp
// VolumeNameValidator.cs
namespace Acode.Infrastructure.Sandbox.Caching;

public sealed class VolumeNameValidator
{
    private static readonly Regex ValidVolumeNamePattern =
        new(@"^[a-zA-Z0-9][a-zA-Z0-9_.-]{0,127}$", RegexOptions.Compiled);

    private static readonly HashSet<string> ReservedPrefixes = new()
    {
        "docker-", "sys-", "dev-", "tmp-", "proc-", "root-"
    };

    private const int MaxVolumeNameLength = 128;

    public static ValidationResult Validate(string volumeName)
    {
        if (string.IsNullOrWhiteSpace(volumeName))
        {
            return ValidationResult.Failure(
                "ACODE-VOL-005",
                "Volume name cannot be null or whitespace");
        }

        if (volumeName.Length > MaxVolumeNameLength)
        {
            return ValidationResult.Failure(
                "ACODE-VOL-005",
                $"Volume name exceeds maximum length of {MaxVolumeNameLength} characters");
        }

        if (!ValidVolumeNamePattern.IsMatch(volumeName))
        {
            return ValidationResult.Failure(
                "ACODE-VOL-005",
                "Volume name must contain only alphanumeric characters, underscores, " +
                "hyphens, and periods. Must start with alphanumeric character.");
        }

        var lowerName = volumeName.ToLowerInvariant();
        if (ReservedPrefixes.Any(prefix => lowerName.StartsWith(prefix)))
        {
            return ValidationResult.Failure(
                "ACODE-VOL-005",
                $"Volume name cannot start with reserved prefix: {string.Join(", ", ReservedPrefixes)}");
        }

        // Check for path traversal attempts
        if (volumeName.Contains("..") || volumeName.Contains("/") || volumeName.Contains("\\"))
        {
            return ValidationResult.Failure(
                "ACODE-VOL-005",
                "Volume name cannot contain path traversal sequences or path separators");
        }

        return ValidationResult.Success();
    }
}

public sealed record ValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static ValidationResult Success() => new() { IsValid = true };

    public static ValidationResult Failure(string errorCode, string errorMessage) =>
        new() { IsValid = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}
```

### Threat 2: Cache Poisoning via Malicious Packages

**Risk Description:** An attacker with access to the host system or a compromised container could inject malicious packages into cache volumes. Subsequent builds would use poisoned packages without verification, executing attacker-controlled code.

**Attack Scenario:**
1. Attacker gains access to host filesystem or privileged container
2. Attacker mounts cache volume: `docker volume inspect acode-cache-nuget` → find mount point
3. Attacker replaces legitimate package DLL with malicious version at `/var/lib/docker/volumes/acode-cache-nuget/_data/newtonsoft.json/13.0.1/`
4. Next build uses poisoned Newtonsoft.Json package
5. Malicious code executes during build or runtime

**Mitigation:**

```csharp
// CacheIntegrityVerifier.cs
namespace Acode.Infrastructure.Sandbox.Caching;

public interface ICacheIntegrityVerifier
{
    Task<IntegrityCheckResult> VerifyNuGetPackageAsync(
        string packagePath,
        string expectedHash,
        CancellationToken cancellationToken = default);

    Task<IntegrityCheckResult> VerifyNpmPackageAsync(
        string packagePath,
        string expectedIntegrity,
        CancellationToken cancellationToken = default);
}

public sealed class CacheIntegrityVerifier : ICacheIntegrityVerifier
{
    private readonly ILogger<CacheIntegrityVerifier> _logger;

    public CacheIntegrityVerifier(ILogger<CacheIntegrityVerifier> logger)
    {
        _logger = logger;
    }

    public async Task<IntegrityCheckResult> VerifyNuGetPackageAsync(
        string packagePath,
        string expectedHash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(packagePath))
            {
                return IntegrityCheckResult.Missing(packagePath);
            }

            using var sha512 = SHA512.Create();
            using var stream = File.OpenRead(packagePath);

            var hashBytes = await sha512.ComputeHashAsync(stream, cancellationToken);
            var actualHash = Convert.ToBase64String(hashBytes);

            if (!string.Equals(actualHash, expectedHash, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "NuGet package integrity check failed for {PackagePath}. " +
                    "Expected: {ExpectedHash}, Actual: {ActualHash}",
                    packagePath, expectedHash, actualHash);

                return IntegrityCheckResult.Corrupted(packagePath, expectedHash, actualHash);
            }

            return IntegrityCheckResult.Valid(packagePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying NuGet package integrity: {PackagePath}", packagePath);
            return IntegrityCheckResult.Error(packagePath, ex.Message);
        }
    }

    public async Task<IntegrityCheckResult> VerifyNpmPackageAsync(
        string packagePath,
        string expectedIntegrity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(packagePath))
            {
                return IntegrityCheckResult.Missing(packagePath);
            }

            // npm uses SRI (Subresource Integrity) format: "sha512-base64hash"
            var parts = expectedIntegrity.Split('-');
            if (parts.Length != 2)
            {
                return IntegrityCheckResult.Error(packagePath, "Invalid SRI format");
            }

            var algorithm = parts[0].ToLowerInvariant();
            var expectedHash = parts[1];

            HashAlgorithm hashAlgorithm = algorithm switch
            {
                "sha512" => SHA512.Create(),
                "sha384" => SHA384.Create(),
                "sha256" => SHA256.Create(),
                _ => throw new NotSupportedException($"Hash algorithm {algorithm} not supported")
            };

            using (hashAlgorithm)
            using (var stream = File.OpenRead(packagePath))
            {
                var hashBytes = await hashAlgorithm.ComputeHashAsync(stream, cancellationToken);
                var actualHash = Convert.ToBase64String(hashBytes);

                if (!string.Equals(actualHash, expectedHash, StringComparison.Ordinal))
                {
                    _logger.LogWarning(
                        "npm package integrity check failed for {PackagePath}. " +
                        "Expected: {ExpectedIntegrity}, Actual: {Algorithm}-{ActualHash}",
                        packagePath, expectedIntegrity, $"{algorithm}-{actualHash}");

                    return IntegrityCheckResult.Corrupted(
                        packagePath, expectedIntegrity, $"{algorithm}-{actualHash}");
                }
            }

            return IntegrityCheckResult.Valid(packagePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying npm package integrity: {PackagePath}", packagePath);
            return IntegrityCheckResult.Error(packagePath, ex.Message);
        }
    }
}

public sealed record IntegrityCheckResult
{
    public IntegrityStatus Status { get; init; }
    public string PackagePath { get; init; } = string.Empty;
    public string? ExpectedHash { get; init; }
    public string? ActualHash { get; init; }
    public string? ErrorMessage { get; init; }

    public static IntegrityCheckResult Valid(string packagePath) =>
        new() { Status = IntegrityStatus.Valid, PackagePath = packagePath };

    public static IntegrityCheckResult Corrupted(string packagePath, string expected, string actual) =>
        new() { Status = IntegrityStatus.Corrupted, PackagePath = packagePath,
                ExpectedHash = expected, ActualHash = actual };

    public static IntegrityCheckResult Missing(string packagePath) =>
        new() { Status = IntegrityStatus.Missing, PackagePath = packagePath };

    public static IntegrityCheckResult Error(string packagePath, string errorMessage) =>
        new() { Status = IntegrityStatus.Error, PackagePath = packagePath, ErrorMessage = errorMessage };
}

public enum IntegrityStatus
{
    Valid,
    Corrupted,
    Missing,
    Error
}
```

### Threat 3: Unauthorized Container Access to Cache Volumes

**Risk Description:** A malicious or compromised container could access cache volumes intended for different projects or package managers, potentially reading sensitive data from cached packages or poisoning caches for other projects.

**Attack Scenario:**
1. Attacker compromises Container A running Node.js project
2. Container A is configured to mount only npm cache
3. Attacker exploits misconfiguration to mount NuGet cache: `docker run -v acode-cache-nuget:/mnt/stolen`
4. Attacker reads proprietary .NET packages or injects malicious NuGet packages
5. Victim's .NET build uses poisoned packages

**Mitigation:**

```csharp
// VolumeMountAuthorizationService.cs
namespace Acode.Infrastructure.Sandbox.Caching;

public sealed class VolumeMountAuthorizationService
{
    private readonly IPackageManagerDetector _detector;
    private readonly ICacheConfigurationProvider _configProvider;
    private readonly ILogger<VolumeMountAuthorizationService> _logger;

    public VolumeMountAuthorizationService(
        IPackageManagerDetector detector,
        ICacheConfigurationProvider configProvider,
        ILogger<VolumeMountAuthorizationService> logger)
    {
        _detector = detector;
        _configProvider = configProvider;
        _logger = logger;
    }

    public async Task<AuthorizationResult> AuthorizeVolumeMountsAsync(
        string projectPath,
        IReadOnlyList<VolumeMount> requestedMounts,
        CancellationToken cancellationToken = default)
    {
        var config = _configProvider.GetConfig();
        var detectedManagers = await _detector.DetectAsync(projectPath, cancellationToken);
        var authorizedVolumeNames = GetAuthorizedVolumeNames(detectedManagers, config);

        var unauthorizedMounts = requestedMounts
            .Where(mount => !authorizedVolumeNames.Contains(mount.VolumeName))
            .ToList();

        if (unauthorizedMounts.Any())
        {
            var unauthorizedNames = string.Join(", ", unauthorizedMounts.Select(m => m.VolumeName));

            _logger.LogWarning(
                "Unauthorized volume mounts requested for project {ProjectPath}. " +
                "Detected package managers: {DetectedManagers}. " +
                "Unauthorized volumes: {UnauthorizedVolumes}",
                projectPath,
                string.Join(", ", detectedManagers),
                unauthorizedNames);

            return AuthorizationResult.Denied(
                $"Volumes {unauthorizedNames} are not authorized for this project type. " +
                $"Detected package managers: {string.Join(", ", detectedManagers)}");
        }

        // Additional check: ensure only managed volumes are mounted (no arbitrary volumes)
        var nonManagedMounts = requestedMounts
            .Where(mount => !mount.VolumeName.StartsWith("acode-cache-"))
            .ToList();

        if (nonManagedMounts.Any())
        {
            var nonManagedNames = string.Join(", ", nonManagedMounts.Select(m => m.VolumeName));

            _logger.LogError(
                "Attempt to mount non-managed volumes: {NonManagedVolumes}",
                nonManagedNames);

            return AuthorizationResult.Denied(
                $"Only acode-managed cache volumes can be mounted. Rejected: {nonManagedNames}");
        }

        return AuthorizationResult.Allowed(authorizedVolumeNames);
    }

    private static HashSet<string> GetAuthorizedVolumeNames(
        IReadOnlyList<PackageManagerType> detectedManagers,
        CacheVolumeConfig config)
    {
        var authorized = new HashSet<string>(StringComparer.Ordinal);

        foreach (var manager in detectedManagers)
        {
            var volumeName = manager switch
            {
                PackageManagerType.NuGet when config.NuGet.Enabled => config.NuGet.VolumeName,
                PackageManagerType.Npm when config.Npm.Enabled => config.Npm.VolumeName,
                PackageManagerType.Yarn when config.Yarn.Enabled => config.Yarn.VolumeName,
                PackageManagerType.Pnpm when config.Pnpm.Enabled => config.Pnpm.VolumeName,
                _ => null
            };

            if (volumeName is not null)
            {
                authorized.Add(volumeName);
            }
        }

        return authorized;
    }
}

public sealed record AuthorizationResult
{
    public bool IsAllowed { get; init; }
    public IReadOnlySet<string> AuthorizedVolumes { get; init; } = new HashSet<string>();
    public string? DenialReason { get; init; }

    public static AuthorizationResult Allowed(IReadOnlySet<string> authorizedVolumes) =>
        new() { IsAllowed = true, AuthorizedVolumes = authorizedVolumes };

    public static AuthorizationResult Denied(string reason) =>
        new() { IsAllowed = false, DenialReason = reason };
}
```

### Threat 4: Volume Exhaustion Denial of Service

**Risk Description:** An attacker or buggy build process could fill cache volumes to capacity, exhausting disk space and preventing legitimate builds from completing. This could cascade to system-wide disk exhaustion if volumes reside on the root partition.

**Attack Scenario:**
1. Attacker submits build task with malicious package.json containing 10,000 dependencies
2. npm install downloads 50GB of packages to cache volume
3. Cache volume fills host disk partition
4. Subsequent builds fail with "no space left on device"
5. System logs and Docker operations also fail due to disk exhaustion

**Mitigation:**

```csharp
// VolumeQuotaEnforcer.cs
namespace Acode.Infrastructure.Sandbox.Caching;

public sealed class VolumeQuotaEnforcer
{
    private readonly IDockerClientFactory _dockerClientFactory;
    private readonly ICacheConfigurationProvider _configProvider;
    private readonly ILogger<VolumeQuotaEnforcer> _logger;

    private const long DefaultMaxVolumeSizeBytes = 10L * 1024 * 1024 * 1024; // 10 GB
    private const long DefaultDiskReserveBytes = 5L * 1024 * 1024 * 1024;    // 5 GB

    public VolumeQuotaEnforcer(
        IDockerClientFactory dockerClientFactory,
        ICacheConfigurationProvider configProvider,
        ILogger<VolumeQuotaEnforcer> logger)
    {
        _dockerClientFactory = dockerClientFactory;
        _configProvider = configProvider;
        _logger = logger;
    }

    public async Task<QuotaCheckResult> CheckVolumeQuotaAsync(
        string volumeName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _dockerClientFactory.Create();

            var volumeInfo = await client.Volumes.InspectAsync(volumeName, cancellationToken);
            var mountpoint = volumeInfo.Mountpoint;

            // Get disk usage for volume mount point
            var driveInfo = new DriveInfo(Path.GetPathRoot(mountpoint) ?? "/");

            var volumeSize = await GetVolumeSizeAsync(mountpoint, cancellationToken);
            var availableSpace = driveInfo.AvailableFreeSpace;
            var totalSpace = driveInfo.TotalSize;

            var config = _configProvider.GetConfig();
            var maxVolumeSize = GetMaxVolumeSize(volumeName, config);

            // Check 1: Volume size exceeds configured limit
            if (volumeSize > maxVolumeSize)
            {
                _logger.LogWarning(
                    "Volume {VolumeName} size {VolumeSize} exceeds limit {MaxSize}",
                    volumeName, FormatBytes(volumeSize), FormatBytes(maxVolumeSize));

                return QuotaCheckResult.ExceededLimit(
                    volumeName, volumeSize, maxVolumeSize, availableSpace);
            }

            // Check 2: Insufficient disk space reserve
            if (availableSpace < DefaultDiskReserveBytes)
            {
                _logger.LogWarning(
                    "Insufficient disk space. Available: {Available}, Reserve: {Reserve}",
                    FormatBytes(availableSpace), FormatBytes(DefaultDiskReserveBytes));

                return QuotaCheckResult.InsufficientDiskSpace(
                    volumeName, volumeSize, availableSpace, DefaultDiskReserveBytes);
            }

            // Check 3: Volume consumes > 50% of total disk
            var volumePercentage = (double)volumeSize / totalSpace * 100;
            if (volumePercentage > 50)
            {
                _logger.LogWarning(
                    "Volume {VolumeName} consumes {Percentage}% of total disk space",
                    volumeName, volumePercentage);

                return QuotaCheckResult.ExcessiveDiskUsage(
                    volumeName, volumeSize, totalSpace, volumePercentage);
            }

            return QuotaCheckResult.WithinLimits(volumeName, volumeSize, maxVolumeSize, availableSpace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking volume quota for {VolumeName}", volumeName);
            return QuotaCheckResult.CheckFailed(volumeName, ex.Message);
        }
    }

    private static async Task<long> GetVolumeSizeAsync(string mountpoint, CancellationToken cancellationToken)
    {
        // Calculate directory size recursively
        var directory = new DirectoryInfo(mountpoint);
        if (!directory.Exists)
        {
            return 0;
        }

        return await Task.Run(() =>
        {
            long size = 0;
            try
            {
                var files = directory.EnumerateFiles("*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    size += file.Length;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip inaccessible directories
            }

            return size;
        }, cancellationToken);
    }

    private static long GetMaxVolumeSize(string volumeName, CacheVolumeConfig config)
    {
        // Future: read from config per package manager
        return DefaultMaxVolumeSizeBytes;
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

public sealed record QuotaCheckResult
{
    public QuotaStatus Status { get; init; }
    public string VolumeName { get; init; } = string.Empty;
    public long CurrentSize { get; init; }
    public long? MaxSize { get; init; }
    public long? AvailableSpace { get; init; }
    public string? Message { get; init; }

    public static QuotaCheckResult WithinLimits(string volumeName, long currentSize,
        long maxSize, long availableSpace) =>
        new()
        {
            Status = QuotaStatus.WithinLimits,
            VolumeName = volumeName,
            CurrentSize = currentSize,
            MaxSize = maxSize,
            AvailableSpace = availableSpace
        };

    public static QuotaCheckResult ExceededLimit(string volumeName, long currentSize,
        long maxSize, long availableSpace) =>
        new()
        {
            Status = QuotaStatus.ExceededLimit,
            VolumeName = volumeName,
            CurrentSize = currentSize,
            MaxSize = maxSize,
            AvailableSpace = availableSpace,
            Message = $"Volume size {currentSize} exceeds limit {maxSize}"
        };

    public static QuotaCheckResult InsufficientDiskSpace(string volumeName, long currentSize,
        long availableSpace, long requiredReserve) =>
        new()
        {
            Status = QuotaStatus.InsufficientDiskSpace,
            VolumeName = volumeName,
            CurrentSize = currentSize,
            AvailableSpace = availableSpace,
            Message = $"Available space {availableSpace} below reserve {requiredReserve}"
        };

    public static QuotaCheckResult ExcessiveDiskUsage(string volumeName, long currentSize,
        long totalSpace, double percentage) =>
        new()
        {
            Status = QuotaStatus.ExcessiveDiskUsage,
            VolumeName = volumeName,
            CurrentSize = currentSize,
            Message = $"Volume consumes {percentage:F1}% of total disk ({currentSize}/{totalSpace})"
        };

    public static QuotaCheckResult CheckFailed(string volumeName, string errorMessage) =>
        new()
        {
            Status = QuotaStatus.CheckFailed,
            VolumeName = volumeName,
            Message = errorMessage
        };
}

public enum QuotaStatus
{
    WithinLimits,
    ExceededLimit,
    InsufficientDiskSpace,
    ExcessiveDiskUsage,
    CheckFailed
}
```

### Threat 5: Package Signature Bypass via Caching

**Risk Description:** Cached packages bypass package manager signature verification on subsequent restores. If the initial download was compromised or verification was disabled, malicious packages persist in cache and are used without re-verification.

**Attack Scenario:**
1. Attacker compromises NuGet.org or performs MITM attack during initial package download
2. Malicious package with forged signature downloads to cache
3. NuGet's signature verification passes during first restore (weak validation) or is disabled
4. Subsequent builds use cached package WITHOUT re-verification
5. Malicious code executes in all future builds

**Mitigation:**

```csharp
// SignatureVerificationEnforcer.cs
namespace Acode.Infrastructure.Sandbox.Caching;

public sealed class SignatureVerificationEnforcer
{
    private readonly ICacheConfigurationProvider _configProvider;
    private readonly ILogger<SignatureVerificationEnforcer> _logger;

    public SignatureVerificationEnforcer(
        ICacheConfigurationProvider configProvider,
        ILogger<SignatureVerificationEnforcer> logger)
    {
        _configProvider = configProvider;
        _logger = logger;
    }

    public async Task<VerificationResult> VerifyNuGetPackageSignatureAsync(
        string packagePath,
        CancellationToken cancellationToken = default)
    {
        var config = _configProvider.GetConfig();
        if (!config.RequireSignatureVerification)
        {
            _logger.LogDebug("Signature verification disabled in configuration");
            return VerificationResult.Skipped(packagePath);
        }

        try
        {
            // Use NuGet.Packaging APIs to verify signature
            var packageReader = new PackageArchiveReader(packagePath);

            // Check if package is signed
            var isSigned = await packageReader.IsSignedAsync(cancellationToken);
            if (!isSigned && config.RequireSignedPackages)
            {
                _logger.LogWarning("Package {PackagePath} is not signed", packagePath);
                return VerificationResult.NotSigned(packagePath);
            }

            if (!isSigned)
            {
                // Unsigned but not required - allow
                return VerificationResult.UnsignedAllowed(packagePath);
            }

            // Verify primary signature
            var primarySignature = await packageReader.GetPrimarySignatureAsync(cancellationToken);
            if (primarySignature is null)
            {
                return VerificationResult.Invalid(packagePath, "No primary signature found");
            }

            // Verify signature validity
            var signedCms = primarySignature.SignedCms;
            try
            {
                signedCms.CheckSignature(verifySignatureOnly: false);
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Signature verification failed for {PackagePath}", packagePath);
                return VerificationResult.Invalid(packagePath, ex.Message);
            }

            // Verify certificate chain
            var signerInfo = signedCms.SignerInfos[0];
            var certificate = signerInfo.Certificate;

            if (certificate is null)
            {
                return VerificationResult.Invalid(packagePath, "No certificate in signature");
            }

            var chain = new X509Chain
            {
                ChainPolicy =
                {
                    RevocationMode = X509RevocationMode.Online,
                    RevocationFlag = X509RevocationFlag.EntireChain,
                    VerificationFlags = X509VerificationFlags.NoFlag
                }
            };

            var chainValid = chain.Build(certificate);
            if (!chainValid)
            {
                var errors = string.Join(", ",
                    chain.ChainStatus.Select(s => $"{s.Status}: {s.StatusInformation}"));

                _logger.LogError(
                    "Certificate chain validation failed for {PackagePath}: {Errors}",
                    packagePath, errors);

                return VerificationResult.Invalid(packagePath, $"Certificate chain invalid: {errors}");
            }

            // Check if certificate is trusted (matches configured trusted signers)
            var trustedSigners = config.TrustedSigners ?? new List<string>();
            var thumbprint = certificate.Thumbprint;

            if (trustedSigners.Any() && !trustedSigners.Contains(thumbprint, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Package {PackagePath} signed by untrusted certificate {Thumbprint}",
                    packagePath, thumbprint);

                return VerificationResult.UntrustedSigner(packagePath, thumbprint);
            }

            _logger.LogInformation(
                "Package signature verified successfully: {PackagePath}, Signer: {SubjectName}",
                packagePath, certificate.SubjectName.Name);

            return VerificationResult.Valid(packagePath, certificate.SubjectName.Name ?? "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying package signature: {PackagePath}", packagePath);
            return VerificationResult.Error(packagePath, ex.Message);
        }
    }
}

public sealed record VerificationResult
{
    public SignatureStatus Status { get; init; }
    public string PackagePath { get; init; } = string.Empty;
    public string? SignerName { get; init; }
    public string? ErrorMessage { get; init; }

    public static VerificationResult Valid(string packagePath, string signerName) =>
        new() { Status = SignatureStatus.Valid, PackagePath = packagePath, SignerName = signerName };

    public static VerificationResult Invalid(string packagePath, string errorMessage) =>
        new() { Status = SignatureStatus.Invalid, PackagePath = packagePath, ErrorMessage = errorMessage };

    public static VerificationResult NotSigned(string packagePath) =>
        new() { Status = SignatureStatus.NotSigned, PackagePath = packagePath };

    public static VerificationResult UnsignedAllowed(string packagePath) =>
        new() { Status = SignatureStatus.UnsignedAllowed, PackagePath = packagePath };

    public static VerificationResult UntrustedSigner(string packagePath, string thumbprint) =>
        new()
        {
            Status = SignatureStatus.UntrustedSigner,
            PackagePath = packagePath,
            ErrorMessage = $"Certificate thumbprint {thumbprint} not in trusted signers list"
        };

    public static VerificationResult Skipped(string packagePath) =>
        new() { Status = SignatureStatus.Skipped, PackagePath = packagePath };

    public static VerificationResult Error(string packagePath, string errorMessage) =>
        new() { Status = SignatureStatus.Error, PackagePath = packagePath, ErrorMessage = errorMessage };
}

public enum SignatureStatus
{
    Valid,
    Invalid,
    NotSigned,
    UnsignedAllowed,
    UntrustedSigner,
    Skipped,
    Error
}
```

---

## Best Practices

### Cache Volume Design

1. **Separate caches by type** - NuGet, npm, yarn in different volumes
2. **Named volumes preferred** - Easier to manage than bind mounts
3. **Regular cleanup** - Schedule cache pruning to manage size
4. **Verify cache integrity** - Detect corrupted cached packages

### Security

5. **Read-only when possible** - Mount caches read-only for restore
6. **Scan cached packages** - Periodic vulnerability scanning
7. **Clear on security alerts** - Remove caches if compromise suspected
8. **Hash verification** - Verify package hashes on restore

### Performance

9. **Warm cache for common packages** - Pre-populate frequently used packages
10. **Local cache priority** - Check local cache before remote
11. **Parallel downloads** - Download multiple packages concurrently
12. **Monitor cache hit rates** - Track effectiveness of caching

---

## Troubleshooting

### Issue 1: Cache Not Being Used (Packages Downloaded Every Build)

**Symptoms:**
- Build logs show "Downloading package X from NuGet.org"  every build
- `acode cache stats` shows volume size is 0 bytes or very small
- Build times remain slow (2-3 minutes) instead of fast (5-15 seconds)

**Causes:**
1. Cache volumes not mounted to container
2. Environment variables not set correctly
3. Package manager looking in wrong directory
4. Cache disabled in configuration

**Solutions:**

```bash
# Step 1: Verify volume exists
docker volume ls | grep acode-cache

# Expected output:
# acode-cache-nuget
# acode-cache-npm

# Step 2: Check if volume is mounted in container
acode task run build
# While container is running, in another terminal:
docker ps
docker inspect <container-id> --format '{{json .Mounts}}' | jq

# Expected: Should show mounts like:
# {
#   "Type": "volume",
#   "Name": "acode-cache-nuget",
#   "Source": "/var/lib/docker/volumes/acode-cache-nuget/_data",
#   "Destination": "/root/.nuget/packages",
#   "Driver": "local",
#   "Mode": "",
#   "RW": true,
#   "Propagation": ""
# }

# Step 3: Verify environment variables inside container
docker exec <container-id> env | grep -E "(NUGET_PACKAGES|npm_config_cache|YARN_CACHE_FOLDER|PNPM_HOME)"

# Expected:
# NUGET_PACKAGES=/root/.nuget/packages
# npm_config_cache=/root/.npm

# Step 4: Check configuration
cat .agent/config.yml | grep -A 10 "cache_volumes"

# Ensure enabled: true

# Step 5: If still not working, enable debug logging
# Edit .agent/config.yml:
# logging:
#   level: debug
#   components:
#     - Acode.Infrastructure.Sandbox.Caching

# Run build again and check logs for cache operations
```

### Issue 2: Volume Creation Fails with "Permission Denied"

**Symptoms:**
- Error: `ACODE-VOL-001: Volume creation failed`
- Docker logs show: `Error response from daemon: error while mounting volume: permission denied`
- Build fails before starting

**Causes:**
1. Docker daemon running as non-root user without proper permissions
2. Host filesystem permissions restrict Docker's volume directory
3. SELinux or AppArmor blocking Docker operations
4. Disk quota exceeded for Docker user

**Solutions:**

```bash
# Step 1: Check Docker permissions
docker info | grep -A 5 "Server Version"
groups | grep docker

# If current user not in docker group:
sudo usermod -aG docker $USER
newgrp docker

# Step 2: Verify Docker volume directory permissions
sudo ls -la /var/lib/docker/volumes/
# Should be owned by root:root with 700 or 755 permissions

# Step 3: Check SELinux status (if on RHEL/CentOS/Fedora)
getenforce
# If "Enforcing", check for denials:
sudo ausearch -m avc -ts recent | grep docker

# Temporarily set to permissive for testing:
sudo setenforce 0

# If that fixes it, create custom policy:
sudo audit2allow -a -M docker-volume
sudo semodule -i docker-volume.pp

# Step 4: Check disk space and quotas
df -h /var/lib/docker
sudo quota -v

# Step 5: Try creating volume manually
docker volume create test-volume
docker volume inspect test-volume
docker volume rm test-volume

# If manual creation fails, restart Docker daemon:
sudo systemctl restart docker
```

### Issue 3: Corrupted Cache Causing Build Failures

**Symptoms:**
- Builds fail with errors like: "Package X is corrupted" or "Invalid package signature"
- `dotnet restore` exits with non-zero code
- `npm install` reports cache integrity errors
- Builds worked previously, started failing suddenly

**Causes:**
1. Incomplete package download interrupted mid-write
2. Disk I/O errors or filesystem corruption
3. Cache poisoning (malicious package injection)
4. Docker volume ran out of space during write
5. Concurrent access from multiple containers caused race condition

**Solutions:**

```bash
# Step 1: Identify which package manager cache is corrupted
# For NuGet:
docker run --rm -v acode-cache-nuget:/cache alpine sh -c "ls -lah /cache"

# For npm:
docker run --rm -v acode-cache-npm:/cache alpine sh -c "ls -lah /cache"

# Step 2: Clear the corrupted cache
acode cache clear --nuget --force
# Or:
acode cache clear --npm --force

# Step 3: Verify volume is actually cleared
acode cache stats

# Expected: Volume should show 0 bytes

# Step 4: Run build again to repopulate cache
acode task run build

# Step 5: If corruption persists, check for disk errors
sudo dmesg | grep -i error
sudo smartctl -a /dev/sda  # Replace with your disk

# Step 6: If disk is healthy, enable integrity verification
# Edit .agent/config.yml:
# sandbox:
#   cache_volumes:
#     integrity_verification:
#       enabled: true
#       hash_algorithm: sha512

# Step 7: Check Docker volume health
docker volume inspect acode-cache-nuget
# Look for errors in Mountpoint or Labels

# Step 8: If all else fails, recreate volume from scratch
docker volume rm acode-cache-nuget
acode task run build
```

### Issue 4: Disk Space Exhausted by Cache Volumes

**Symptoms:**
- Error: `ACODE-VOL-007: Disk space exhausted`
- Builds fail with "no space left on device"
- System becomes unresponsive
- `df -h` shows 100% usage on Docker partition

**Causes:**
1. Cache volumes grew beyond expected size (10GB+ per cache)
2. Many old/unused packages accumulated in cache
3. Monorepo with thousands of dependencies
4. No cache pruning configured
5. Multiple projects sharing host, each with own caches

**Solutions:**

```bash
# Step 1: Check cache sizes
acode cache stats

# Expected output will show size per cache:
# NuGet: 8.5 GB
# npm: 12.3 GB  <-- Problem: npm cache too large

# Step 2: Check overall disk usage
df -h /var/lib/docker
du -sh /var/lib/docker/volumes/*

# Step 3: Prune unused Docker resources
docker system prune -a --volumes
# Warning: This removes ALL unused volumes, not just cache

# Step 4: Clear specific large cache
acode cache clear --npm --force

# Step 5: Repopulate with only current dependencies
acode task run "npm ci"

# Step 6: Set up cache size limits (prevent future exhaustion)
# Edit .agent/config.yml:
# sandbox:
#   cache_volumes:
#     nuget:
#       max_size_gb: 5
#     npm:
#       max_size_gb: 5
#       auto_prune_threshold_gb: 4

# Step 7: Schedule regular cache pruning
crontab -e
# Add:
# 0 2 * * * /usr/local/bin/acode cache prune --older-than 30d

# Step 8: Monitor cache growth over time
acode cache stats --json | jq '.[] | "\(.package_manager): \(.size_bytes)"'

# Step 9: If caches grow rapidly, investigate project dependencies
cd /path/to/project
npm list --depth=0 | wc -l  # Count direct dependencies
du -sh node_modules  # Check installed size
```

### Issue 5: Cache Sharing Conflicts Across Projects

**Symptoms:**
- Project A builds successfully
- Project B builds using Project A's cached packages, but fails at runtime
- Version conflicts reported: "Expected version X.1.0, got X.2.0"
- Intermittent failures depending on which project ran first

**Causes:**
1. Multiple projects depend on different versions of same package
2. Cache stores multiple versions, but package manager picks wrong one
3. Global cache shared across projects with conflicting dependencies
4. Lock files (package-lock.json, yarn.lock) not in sync with cache

**Solutions:**

```bash
# Step 1: Verify which projects are affected
cd /path/to/project-a
npm list | grep problematic-package
# Output: problematic-package@1.0.0

cd /path/to/project-b
npm list | grep problematic-package
# Output: problematic-package@2.0.0

# Step 2: Check what's in cache
docker run --rm -v acode-cache-npm:/cache alpine \
  sh -c "find /cache -name 'problematic-package*' -type d"

# Output might show both versions cached:
# /cache/_cacache/content-v2/.../problematic-package-1.0.0-abc123.tgz
# /cache/_cacache/content-v2/.../problematic-package-2.0.0-def456.tgz

# Step 3: Ensure lock files are present and committed
cd /path/to/project-b
ls -la package-lock.json  # For npm
ls -la yarn.lock          # For yarn
ls -la pnpm-lock.yaml     # For pnpm

# If missing, generate:
npm install --package-lock-only

# Step 4: Use --frozen-lockfile to enforce exact versions
# Edit .agent/config.yml or build script:
# For npm:
npm ci --frozen-lockfile

# For yarn:
yarn install --frozen-lockfile

# For pnpm:
pnpm install --frozen-lockfile

# Step 5: If conflicts persist, use per-project caches
# Edit .agent/config.yml:
# sandbox:
#   cache_volumes:
#     npm:
#       enabled: true
#       per_project_isolation: true
#       volume_name: "acode-cache-npm-${PROJECT_NAME}"

# This creates separate volumes:
# - acode-cache-npm-project-a
# - acode-cache-npm-project-b

# Step 6: Clear cache and rebuild from clean state
acode cache clear --npm --force
cd /path/to/project-a && acode task run "npm ci"
cd /path/to/project-b && acode task run "npm ci"

# Step 7: Verify correct versions installed
docker exec <project-a-container> npm list problematic-package
docker exec <project-b-container> npm list problematic-package

# Step 8: Long-term solution - use lockfile-based caching
# Ensures cache key includes lockfile hash:
# .agent/config.yml:
# sandbox:
#   cache_volumes:
#     cache_key_includes_lockfile_hash: true
```

### Issue 6: Volume Mount Fails on Windows with WSL2

**Symptoms:**
- Error: `ACODE-VOL-002: Volume mount failed`
- Windows + WSL2 + Docker Desktop environment
- Volumes created successfully but containers fail to start
- Error mentions: "invalid mount config for type bind"

**Causes:**
1. Docker Desktop WSL2 backend has different volume mount semantics
2. Volume mount paths incompatible between Windows and WSL2
3. Docker daemon running in Windows mode, but acode running in WSL2
4. File system permissions mismatch between WSL2 and Windows

**Solutions:**

```bash
# Step 1: Verify Docker context (must be using Docker Desktop WSL2 backend)
docker context ls
# Should show: desktop-linux (active)

# Step 2: Check Docker daemon location
docker version --format '{{.Server.Os}}'
# Should show: linux (not windows)

# Step 3: Verify WSL2 integration enabled
# In Docker Desktop -> Settings -> Resources -> WSL Integration
# Ensure your WSL2 distro is enabled

# Step 4: Check volume mount syntax (should use Linux paths)
docker inspect <container-id> --format '{{json .HostConfig.Mounts}}'

# Should show Linux-style paths:
# "Destination": "/root/.nuget/packages"  (Correct)
# NOT: "Destination": "C:\\root\\.nuget\\packages"  (Incorrect)

# Step 5: Recreate volumes using WSL2-aware Docker client
wsl -d Ubuntu
cd /path/to/project
docker volume rm acode-cache-nuget
acode task run build

# Step 6: If still failing, use named volumes (not bind mounts)
# Ensure acode configuration uses named volumes:
# sandbox:
#   cache_volumes:
#     type: named  # NOT bind

# Step 7: Check Docker Desktop version (needs 4.0.0+)
docker version

# Update if needed from https://www.docker.com/products/docker-desktop/

# Step 8: Verify WSL2 kernel version
wsl --status
# Should show WSL 2 and kernel version 5.10+

uname -r
# Should show something like: 5.15.90.1-microsoft-standard-WSL2
```

---

## Testing Requirements

Complete test implementations using xUnit, FluentAssertions, and NSubstitute.

### Unit Tests

```csharp
// CacheVolumeManagerTests.cs
namespace Acode.Infrastructure.Tests.Sandbox.Caching;

public sealed class CacheVolumeManagerTests
{
    [Fact]
    public async Task EnsureVolumeAsync_WhenVolumeDoesNotExist_CreatesVolume()
    {
        // Arrange
        var mockDockerClient = Substitute.For<IDockerClient>();
        var mockVolumesOperations = Substitute.For<IVolumeOperations>();
        var mockDockerClientFactory = Substitute.For<IDockerClientFactory>();
        var mockConfigProvider = Substitute.For<ICacheConfigurationProvider>();
        var mockDetector = Substitute.For<IPackageManagerDetector>();
        var mockLogger = Substitute.For<ILogger<CacheVolumeManager>>();

        mockDockerClientFactory.Create().Returns(mockDockerClient);
        mockDockerClient.Volumes.Returns(mockVolumesOperations);

        mockConfigProvider.GetConfig().Returns(new CacheVolumeConfig
        {
            Enabled = true,
            NuGet = new NuGetCacheConfig { VolumeName = "acode-cache-nuget" }
        });

        mockVolumesOperations
            .InspectAsync("acode-cache-nuget", Arg.Any<CancellationToken>())
            .Returns<VolumeResponse>(_ => throw new DockerContainerNotFoundException());

        mockVolumesOperations
            .CreateAsync(Arg.Any<VolumesCreateParameters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new VolumeResponse { Name = "acode-cache-nuget" }));

        var sut = new CacheVolumeManager(
            mockDockerClientFactory,
            mockConfigProvider,
            mockDetector,
            mockLogger);

        // Act
        var result = await sut.EnsureVolumeAsync(PackageManagerType.NuGet);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("acode-cache-nuget");
        result.PackageManager.Should().Be(PackageManagerType.NuGet);
        result.Exists.Should().BeTrue();

        await mockVolumesOperations.Received(1).CreateAsync(
            Arg.Is<VolumesCreateParameters>(p =>
                p.Name == "acode-cache-nuget" &&
                p.Labels["managed-by"] == "acode" &&
                p.Labels["package-manager"] == "nuget"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureVolumeAsync_WhenVolumeExists_DoesNotRecreate()
    {
        // Arrange
        var mockDockerClient = Substitute.For<IDockerClient>();
        var mockVolumesOperations = Substitute.For<IVolumeOperations>();
        var mockDockerClientFactory = Substitute.For<IDockerClientFactory>();
        var mockConfigProvider = Substitute.For<ICacheConfigurationProvider>();
        var mockDetector = Substitute.For<IPackageManagerDetector>();
        var mockLogger = Substitute.For<ILogger<CacheVolumeManager>>();

        mockDockerClientFactory.Create().Returns(mockDockerClient);
        mockDockerClient.Volumes.Returns(mockVolumesOperations);

        mockConfigProvider.GetConfig().Returns(new CacheVolumeConfig
        {
            Enabled = true,
            Npm = new NpmCacheConfig { VolumeName = "acode-cache-npm" }
        });

        var existingVolume = new VolumeResponse
        {
            Name = "acode-cache-npm",
            CreatedAt = "2024-01-15T10:00:00Z",
            Mountpoint = "/var/lib/docker/volumes/acode-cache-npm/_data"
        };

        mockVolumesOperations
            .InspectAsync("acode-cache-npm", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(existingVolume));

        var sut = new CacheVolumeManager(
            mockDockerClientFactory,
            mockConfigProvider,
            mockDetector,
            mockLogger);

        // Act
        var result = await sut.EnsureVolumeAsync(PackageManagerType.Npm);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("acode-cache-npm");
        result.Exists.Should().BeTrue();

        await mockVolumesOperations.DidNotReceive().CreateAsync(
            Arg.Any<VolumesCreateParameters>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRequiredMountsAsync_ForDotNetProject_ReturnsNuGetMount()
    {
        // Arrange
        var mockDockerClientFactory = Substitute.For<IDockerClientFactory>();
        var mockConfigProvider = Substitute.For<ICacheConfigurationProvider>();
        var mockDetector = Substitute.For<IPackageManagerDetector>();
        var mockLogger = Substitute.For<ILogger<CacheVolumeManager>>();
        var mockDockerClient = Substitute.For<IDockerClient>();
        var mockVolumesOperations = Substitute.For<IVolumeOperations>();

        mockDockerClientFactory.Create().Returns(mockDockerClient);
        mockDockerClient.Volumes.Returns(mockVolumesOperations);

        mockConfigProvider.GetConfig().Returns(new CacheVolumeConfig
        {
            Enabled = true,
            NuGet = new NuGetCacheConfig
            {
                Enabled = true,
                VolumeName = "acode-cache-nuget",
                MountPath = "/root/.nuget/packages"
            }
        });

        mockDetector
            .DetectAsync("/projects/my-dotnet-app", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<PackageManagerType>>(
                new List<PackageManagerType> { PackageManagerType.NuGet }));

        mockVolumesOperations
            .InspectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new VolumeResponse
            {
                Name = "acode-cache-nuget",
                CreatedAt = "2024-01-15T10:00:00Z"
            }));

        var sut = new CacheVolumeManager(
            mockDockerClientFactory,
            mockConfigProvider,
            mockDetector,
            mockLogger);

        // Act
        var mounts = await sut.GetRequiredMountsAsync("/projects/my-dotnet-app");

        // Assert
        mounts.Should().HaveCount(1);
        var mount = mounts.First();
        mount.VolumeName.Should().Be("acode-cache-nuget");
        mount.ContainerPath.Should().Be("/root/.nuget/packages");
        mount.ReadOnly.Should().BeFalse();
        mount.EnvironmentVariables.Should().ContainKey("NUGET_PACKAGES");
        mount.EnvironmentVariables["NUGET_PACKAGES"].Should().Be("/root/.nuget/packages");
    }
}

// PackageManagerDetectorTests.cs
public sealed class PackageManagerDetectorTests
{
    [Fact]
    public async Task DetectAsync_WithCsprojFile_ReturnsNuGet()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystem>();
        var mockDirectory = Substitute.For<IDirectory>();
        var mockLogger = Substitute.For<ILogger<PackageManagerDetector>>();

        mockFileSystem.Directory.Returns(mockDirectory);

        mockDirectory
            .EnumerateFiles("/projects/dotnet-app", "*.*", SearchOption.AllDirectories)
            .Returns(new[]
            {
                "/projects/dotnet-app/MyApp.csproj",
                "/projects/dotnet-app/Program.cs"
            });

        var sut = new PackageManagerDetector(mockFileSystem, mockLogger);

        // Act
        var result = await sut.DetectAsync("/projects/dotnet-app");

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(PackageManagerType.NuGet);
    }

    [Fact]
    public async Task DetectAsync_WithMixedProject_ReturnsBothNuGetAndNpm()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystem>();
        var mockDirectory = Substitute.For<IDirectory>();
        var mockFile = Substitute.For<IFile>();
        var mockLogger = Substitute.For<ILogger<PackageManagerDetector>>();

        mockFileSystem.Directory.Returns(mockDirectory);
        mockFileSystem.File.Returns(mockFile);

        mockDirectory
            .EnumerateFiles("/projects/mixed-app", "*.*", SearchOption.AllDirectories)
            .Returns(new[]
            {
                "/projects/mixed-app/Backend.csproj",
                "/projects/mixed-app/package.json",
                "/projects/mixed-app/ClientApp/app.tsx"
            });

        mockFile.Exists("/projects/mixed-app/package.json").Returns(true);
        mockFile.Exists("/projects/mixed-app/yarn.lock").Returns(false);
        mockFile.Exists("/projects/mixed-app/pnpm-lock.yaml").Returns(false);

        var sut = new PackageManagerDetector(mockFileSystem, mockLogger);

        // Act
        var result = await sut.DetectAsync("/projects/mixed-app");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(PackageManagerType.NuGet);
        result.Should().Contain(PackageManagerType.Npm);
    }

    [Fact]
    public async Task DetectAsync_WithNoProjectFiles_ReturnsEmpty()
    {
        // Arrange
        var mockFileSystem = Substitute.For<IFileSystem>();
        var mockDirectory = Substitute.For<IDirectory>();
        var mockFile = Substitute.For<IFile>();
        var mockLogger = Substitute.For<ILogger<PackageManagerDetector>>();

        mockFileSystem.Directory.Returns(mockDirectory);
        mockFileSystem.File.Returns(mockFile);

        mockDirectory
            .EnumerateFiles("/projects/empty-dir", "*.*", SearchOption.AllDirectories)
            .Returns(new[] { "/projects/empty-dir/README.md" });

        mockFile.Exists(Arg.Any<string>()).Returns(false);

        var sut = new PackageManagerDetector(mockFileSystem, mockLogger);

        // Act
        var result = await sut.DetectAsync("/projects/empty-dir");

        // Assert
        result.Should().BeEmpty();
    }
}

// VolumeNameValidatorTests.cs
public sealed class VolumeNameValidatorTests
{
    [Theory]
    [InlineData("acode-cache-nuget", true)]
    [InlineData("acode-cache-npm", true)]
    [InlineData("my_volume.123", true)]
    [InlineData("volume-with-dashes_and_underscores.dots", true)]
    public void Validate_WithValidNames_ReturnsSuccess(string volumeName, bool expected)
    {
        // Act
        var result = VolumeNameValidator.Validate(volumeName);

        // Assert
        result.IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData("", "Volume name cannot be null or whitespace")]
    [InlineData("   ", "Volume name cannot be null or whitespace")]
    [InlineData("docker-forbidden", "cannot start with reserved prefix")]
    [InlineData("sys-reserved", "cannot start with reserved prefix")]
    [InlineData("volume/with/slashes", "cannot contain path traversal")]
    [InlineData("volume..traversal", "cannot contain path traversal")]
    [InlineData("invalid@chars!", "must contain only alphanumeric")]
    [InlineData("-starts-with-dash", "must contain only alphanumeric")]
    public void Validate_WithInvalidNames_ReturnsFailure(string volumeName, string expectedMessagePart)
    {
        // Act
        var result = VolumeNameValidator.Validate(volumeName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("ACODE-VOL-005");
        result.ErrorMessage.Should().Contain(expectedMessagePart);
    }
}

// VolumeMountAuthorizationServiceTests.cs
public sealed class VolumeMountAuthorizationServiceTests
{
    [Fact]
    public async Task AuthorizeVolumeMountsAsync_WithAuthorizedMounts_ReturnsAllowed()
    {
        // Arrange
        var mockDetector = Substitute.For<IPackageManagerDetector>();
        var mockConfigProvider = Substitute.For<ICacheConfigurationProvider>();
        var mockLogger = Substitute.For<ILogger<VolumeMountAuthorizationService>>();

        mockDetector
            .DetectAsync("/projects/app", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<PackageManagerType>>(
                new List<PackageManagerType> { PackageManagerType.NuGet }));

        mockConfigProvider.GetConfig().Returns(new CacheVolumeConfig
        {
            Enabled = true,
            NuGet = new NuGetCacheConfig { Enabled = true, VolumeName = "acode-cache-nuget" }
        });

        var requestedMounts = new List<VolumeMount>
        {
            new() { VolumeName = "acode-cache-nuget", ContainerPath = "/root/.nuget/packages" }
        };

        var sut = new VolumeMountAuthorizationService(mockDetector, mockConfigProvider, mockLogger);

        // Act
        var result = await sut.AuthorizeVolumeMountsAsync("/projects/app", requestedMounts);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.AuthorizedVolumes.Should().Contain("acode-cache-nuget");
        result.DenialReason.Should().BeNull();
    }

    [Fact]
    public async Task AuthorizeVolumeMountsAsync_WithUnauthorizedMounts_ReturnsDenied()
    {
        // Arrange
        var mockDetector = Substitute.For<IPackageManagerDetector>();
        var mockConfigProvider = Substitute.For<ICacheConfigurationProvider>();
        var mockLogger = Substitute.For<ILogger<VolumeMountAuthorizationService>>();

        mockDetector
            .DetectAsync("/projects/nodejs-app", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<PackageManagerType>>(
                new List<PackageManagerType> { PackageManagerType.Npm }));

        mockConfigProvider.GetConfig().Returns(new CacheVolumeConfig
        {
            Enabled = true,
            Npm = new NpmCacheConfig { Enabled = true, VolumeName = "acode-cache-npm" }
        });

        // Attacker tries to mount NuGet cache in Node.js project
        var requestedMounts = new List<VolumeMount>
        {
            new() { VolumeName = "acode-cache-nuget", ContainerPath = "/mnt/stolen" }
        };

        var sut = new VolumeMountAuthorizationService(mockDetector, mockConfigProvider, mockLogger);

        // Act
        var result = await sut.AuthorizeVolumeMountsAsync("/projects/nodejs-app", requestedMounts);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.DenialReason.Should().Contain("not authorized for this project type");
    }
}
```

### Integration Tests

```csharp
// VolumeLifecycleIntegrationTests.cs (Docker required)
[Collection("DockerCollection")]
public sealed class VolumeLifecycleIntegrationTests : IAsyncLifetime
{
    private readonly DockerClient _dockerClient;
    private const string TestVolumeName = "acode-test-volume";

    public VolumeLifecycleIntegrationTests()
    {
        _dockerClient = new DockerClientConfiguration().CreateClient();
    }

    [Fact]
    public async Task VolumeLifecycle_CreateMountDelete_CompletesSuccessfully()
    {
        // Arrange - Create volume
        var volumeParams = new VolumesCreateParameters
        {
            Name = TestVolumeName,
            Labels = new Dictionary<string, string>
            {
                ["managed-by"] = "acode",
                ["test"] = "true"
            }
        };

        await _dockerClient.Volumes.CreateAsync(volumeParams);

        try
        {
            // Act 1 - Inspect volume
            var inspected = await _dockerClient.Volumes.InspectAsync(TestVolumeName);

            // Assert 1
            inspected.Should().NotBeNull();
            inspected.Name.Should().Be(TestVolumeName);
            inspected.Labels["managed-by"].Should().Be("acode");

            // Act 2 - Mount volume in container
            var containerParams = new CreateContainerParameters
            {
                Image = "alpine:latest",
                Cmd = new[] { "sh", "-c", "echo 'test data' > /cache/test.txt && cat /cache/test.txt" },
                HostConfig = new HostConfig
                {
                    Mounts = new[]
                    {
                        new Mount
                        {
                            Type = "volume",
                            Source = TestVolumeName,
                            Target = "/cache"
                        }
                    }
                }
            };

            var container = await _dockerClient.Containers.CreateContainerAsync(containerParams);
            await _dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters());
            var waitResult = await _dockerClient.Containers.WaitContainerAsync(container.ID);

            // Assert 2 - Container executed successfully
            waitResult.StatusCode.Should().Be(0);

            // Cleanup container
            await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true });
        }
        finally
        {
            // Act 3 - Delete volume
            await _dockerClient.Volumes.RemoveAsync(TestVolumeName);

            // Assert 3 - Volume deleted
            await Assert.ThrowsAsync<DockerVolumeNotFoundException>(
                async () => await _dockerClient.Volumes.InspectAsync(TestVolumeName));
        }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        try
        {
            await _dockerClient.Volumes.RemoveAsync(TestVolumeName);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
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
        var hasDotNetProject = _fileSystem.Directory
            .EnumerateFiles(projectPath, "*.*", SearchOption.AllDirectories)
            .Any(filePath =>
                filePath.EndsWith(".csproj") ||
                filePath.EndsWith(".fsproj") ||
                filePath.EndsWith(".sln"));

        if (hasDotNetProject)
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