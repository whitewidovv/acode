# Task 029.a: Prepare Workspace

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 029 (Interface), Task 005 (Git)  

---

## Description

Task 029.a implements workspace preparation for compute targets. Before execution, the target MUST have the code and dependencies ready. Preparation MUST be idempotent.

Workspace preparation MUST sync the repository. The correct branch or commit MUST be checked out. Dependencies MUST be installed. Build tools MUST be available.

Preparation MUST be target-specific. Local targets use the filesystem. Remote targets use rsync or similar. Cloud targets bootstrap from scratch.

### Business Value

Workspace preparation enables:
- Consistent execution environment
- Correct code version
- Required dependencies
- Reproducible builds

### Scope Boundaries

This task covers preparation. Execution is in 029.b. Artifacts are in 029.c. Teardown is in 029.d.

### Integration Points

| Component | Integration Type | Description |
|-----------|-----------------|-------------|
| Task 029 IComputeTarget | Primary | Provides target abstraction for workspace operations |
| Task 005 Git Operations | Data Source | Clones/fetches repository content |
| Task 030-031 Remote Targets | Extension | Override sync methods for SSH/EC2 specifics |
| IWorkspacePreparation | Interface | Main contract for preparation logic |
| ISourceSyncer | Strategy | Pluggable sync implementations (local/git/rsync) |
| IDependencyInstaller | Strategy | Ecosystem-specific dependency installation |
| ICacheManager | Optimization | Manages dependency and artifact caches |

### Failure Modes

| Failure Type | Detection | Recovery | User Impact |
|--------------|-----------|----------|-------------|
| Network timeout during sync | Connection error | Retry with backoff (3x) | Delayed preparation |
| Disk space exhausted | IOException | Cleanup temp files, report space needed | Must free space |
| Dependency install failure | Non-zero exit | Log details, report packages | Manual intervention |
| Permission denied | UnauthorizedAccess | Clear error message | Fix permissions |
| Invalid ref (branch/commit) | Git exit code | Report available refs | Fix configuration |
| Corrupted cache | Hash mismatch | Invalidate cache, re-sync | Automatic recovery |
| Submodule failure | Git exit code | Report submodule name | Fix submodule config |
| Custom command failure | Non-zero exit | Log output, fail preparation | Fix command |

---

## Assumptions

1. **Target Ready State**: The compute target is in a state where filesystem operations are possible
2. **Network Access**: For remote sources, network connectivity is available (respecting mode constraints)
3. **Git Availability**: Git is installed on all target types that need repository sync
4. **Package Manager Access**: Package managers (dotnet, npm, pip) are available for dependency installation
5. **Credential Access**: SSH keys or tokens for private repos are properly configured
6. **Disk Space**: Sufficient disk space exists on target (validated before sync)
7. **Time Synchronization**: Target system clocks are reasonably synchronized for cache invalidation
8. **UTF-8 Support**: File systems support UTF-8 encoded paths and content

---

## Security Considerations

1. **Source Validation**: Only sync from configured/whitelisted sources - never execute arbitrary URLs
2. **Credential Protection**: Never log SSH keys, tokens, or credentials in preparation output
3. **Path Traversal Prevention**: Sanitize paths to prevent `../` escapes during sync
4. **Executable Permissions**: prepareCommands have explicit permissions, logged for audit
5. **Environment Isolation**: Preparation commands run in sandboxed environment where possible
6. **Dependency Integrity**: Verify package checksums when lockfiles are present
7. **Submodule Security**: Submodule URLs validated against allowed sources
8. **Cache Poisoning Prevention**: Cache entries keyed by content hash, not just path

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Workspace | Directory for execution |
| Sync | Transfer code to target |
| Dependencies | Required packages |
| Bootstrap | Initial setup |
| Idempotent | Same result on repeat |

---

## Out of Scope

- Custom build systems
- Monorepo handling
- Submodule resolution
- LFS file handling
- Pre-built artifacts

---

## Functional Requirements

### Preparation Steps (FR-029A-01 to FR-029A-30)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029A-01 | `PrepareWorkspaceAsync(WorkspaceConfig, CancellationToken)` method MUST be defined | Must Have |
| FR-029A-02 | WorkspaceConfig MUST specify source (repository path or URL) | Must Have |
| FR-029A-03 | Source MUST support local filesystem paths | Must Have |
| FR-029A-04 | Source MUST support git repository URLs (https, ssh) | Must Have |
| FR-029A-05 | WorkspaceConfig MUST specify ref (branch, tag, or commit SHA) | Must Have |
| FR-029A-06 | Ref MUST support branch names | Must Have |
| FR-029A-07 | Ref MUST support tag names | Must Have |
| FR-029A-08 | Ref MUST support full commit SHAs | Must Have |
| FR-029A-09 | WorkspaceConfig MUST specify target workspace path | Must Have |
| FR-029A-10 | Workspace directory MUST be created if not exists | Must Have |
| FR-029A-11 | Existing workspace files MAY be cleaned before sync | Should Have |
| FR-029A-12 | Clean option MUST be configurable (default: true) | Should Have |
| FR-029A-13 | Source code MUST be synced to workspace | Must Have |
| FR-029A-14 | Local source MUST use efficient copy or symlink | Should Have |
| FR-029A-15 | Remote source MUST use rsync or git clone | Must Have |
| FR-029A-16 | Specified ref MUST be checked out after sync | Must Have |
| FR-029A-17 | Checkout MUST use detached HEAD for consistency | Should Have |
| FR-029A-18 | Git submodules MUST be updated if present | Should Have |
| FR-029A-19 | Submodule depth MUST be configurable (default: 1) | Could Have |
| FR-029A-20 | Dependencies MUST be installed after checkout | Must Have |
| FR-029A-21 | .NET projects MUST run `dotnet restore` | Must Have |
| FR-029A-22 | Node projects MUST run `npm ci` or `yarn install` | Must Have |
| FR-029A-23 | Python projects MUST run `pip install -r requirements.txt` | Must Have |
| FR-029A-24 | Ecosystem detection MUST be automatic based on project files | Must Have |
| FR-029A-25 | Multiple ecosystems in same project MUST be supported | Should Have |
| FR-029A-26 | Custom preparation commands MUST be supported via config | Should Have |
| FR-029A-27 | Custom commands MUST be defined in `prepareCommands` array | Should Have |
| FR-029A-28 | Custom commands MUST run in order (sequential execution) | Must Have |
| FR-029A-29 | Any command failure MUST stop preparation and report error | Must Have |
| FR-029A-30 | Successful preparation MUST transition target to Ready state | Must Have |

### Caching and Optimization (FR-029A-31 to FR-029A-50)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029A-31 | Dependency caching MUST be supported | Should Have |
| FR-029A-32 | .NET MUST cache NuGet packages | Should Have |
| FR-029A-33 | Node MUST cache node_modules | Should Have |
| FR-029A-34 | Python MUST cache virtualenv | Should Have |
| FR-029A-35 | Cache location MUST be configurable | Should Have |
| FR-029A-36 | Cache MUST be invalidated on lockfile change | Must Have |
| FR-029A-37 | Lockfile detection: packages.lock.json, package-lock.json, requirements.txt | Must Have |
| FR-029A-38 | Incremental sync MUST be supported for repeated preparation | Should Have |
| FR-029A-39 | Only changed files MUST transfer on incremental sync | Should Have |
| FR-029A-40 | rsync MUST use delta transfer for remote targets | Should Have |
| FR-029A-41 | git fetch MUST be incremental (not full clone) | Should Have |
| FR-029A-42 | Parallel file transfer MAY be used for large files | Could Have |
| FR-029A-43 | Progress MUST be reported during sync | Must Have |
| FR-029A-44 | Progress events MUST include bytes transferred and total | Must Have |
| FR-029A-45 | Progress MUST estimate remaining time (ETA) | Should Have |
| FR-029A-46 | Cancellation token MUST be respected at all phases | Must Have |
| FR-029A-47 | Partial state MUST be cleaned up on cancellation | Must Have |
| FR-029A-48 | Retry count MUST be configurable (default: 3) | Should Have |
| FR-029A-49 | Retry MUST use exponential backoff | Should Have |
| FR-029A-50 | Preparation timeout MUST be configurable (default: 10min) | Should Have |

### Ecosystem Detection (FR-029A-51 to FR-029A-65)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029A-51 | `IEcosystemDetector` interface MUST be defined | Must Have |
| FR-029A-52 | Detector MUST identify .NET by *.csproj, *.sln, *.fsproj | Must Have |
| FR-029A-53 | Detector MUST identify Node by package.json | Must Have |
| FR-029A-54 | Detector MUST identify Python by requirements.txt, setup.py, pyproject.toml | Must Have |
| FR-029A-55 | Detector MUST identify Go by go.mod | Should Have |
| FR-029A-56 | Detector MUST identify Rust by Cargo.toml | Should Have |
| FR-029A-57 | Detector MUST return `EcosystemType` flags enum | Must Have |
| FR-029A-58 | Multiple ecosystems MUST be detected simultaneously | Should Have |
| FR-029A-59 | Detection MUST search workspace root and immediate subdirectories | Must Have |
| FR-029A-60 | Detection MUST be cached per workspace path | Should Have |
| FR-029A-61 | Cache MUST be invalidated on workspace changes | Should Have |
| FR-029A-62 | Detection result MUST be logged for debugging | Should Have |
| FR-029A-63 | Unknown ecosystems MUST NOT fail (just skip dependency install) | Must Have |
| FR-029A-64 | Custom ecosystem detection MUST be extensible | Could Have |
| FR-029A-65 | Detection timeout MUST be <5 seconds | Should Have |

### Dependency Installation (FR-029A-66 to FR-029A-80)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029A-66 | `IDependencyInstaller` interface MUST be defined | Must Have |
| FR-029A-67 | Installer MUST accept ecosystem type and workspace path | Must Have |
| FR-029A-68 | Installer MUST run appropriate package manager command | Must Have |
| FR-029A-69 | Installer MUST capture stdout/stderr for logging | Must Have |
| FR-029A-70 | Installer MUST report progress via events | Should Have |
| FR-029A-71 | Installer MUST respect cancellation token | Must Have |
| FR-029A-72 | Installer MUST use --frozen-lockfile equivalents where available | Should Have |
| FR-029A-73 | Installer MUST fail on missing lockfile if configured | Should Have |
| FR-029A-74 | Installer MUST support offline mode for airgapped | Must Have |
| FR-029A-75 | Offline mode MUST use pre-populated cache | Must Have |
| FR-029A-76 | Installer MUST validate installation success | Must Have |
| FR-029A-77 | Validation: check for expected artifacts (bin, lib, etc.) | Should Have |
| FR-029A-78 | Installer MUST support timeout (default: 5min per ecosystem) | Should Have |
| FR-029A-79 | Installer MUST log package manager version used | Should Have |
| FR-029A-80 | Installer errors MUST include package manager output | Must Have |

---

## Non-Functional Requirements

### Performance (NFR-029A-01 to NFR-029A-10)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029A-01 | Small repo preparation (<100 files) | <30 seconds | Must Have |
| NFR-029A-02 | Medium repo preparation (100-1000 files) | <2 minutes | Must Have |
| NFR-029A-03 | Large repo preparation (1GB+) | <5 minutes | Should Have |
| NFR-029A-04 | Cached dependency restore | <10 seconds | Should Have |
| NFR-029A-05 | Incremental sync (10% changed files) | <30 seconds | Should Have |
| NFR-029A-06 | Ecosystem detection time | <5 seconds | Must Have |
| NFR-029A-07 | Memory usage during sync | <500MB | Should Have |
| NFR-029A-08 | Parallel operation throughput | 10 files/second | Should Have |
| NFR-029A-09 | Progress update frequency | Every 1 second | Should Have |
| NFR-029A-10 | Cancellation response time | <1 second | Must Have |

### Reliability (NFR-029A-11 to NFR-029A-20)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029A-11 | Network interruption recovery | 3 retries with backoff | Must Have |
| NFR-029A-12 | Disk space check before sync | Fail if <500MB available | Must Have |
| NFR-029A-13 | Permission error reporting | Clear message with path | Must Have |
| NFR-029A-14 | Idempotent preparation | Same result on repeat | Must Have |
| NFR-029A-15 | Parallel preparation safety | No race conditions | Must Have |
| NFR-029A-16 | Resource cleanup on failure | 100% temporary files removed | Must Have |
| NFR-029A-17 | Corrupted file detection | Hash verification | Should Have |
| NFR-029A-18 | Atomic directory operations | Rename-based commits | Should Have |
| NFR-029A-19 | Lock file for concurrent access | Prevent corruption | Must Have |
| NFR-029A-20 | Graceful timeout handling | Cleanup and report | Must Have |

### Observability (NFR-029A-21 to NFR-029A-30)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029A-21 | Preparation start log | Info level with config summary | Must Have |
| NFR-029A-22 | Preparation complete log | Info level with duration | Must Have |
| NFR-029A-23 | Phase transition logs | Debug level | Should Have |
| NFR-029A-24 | Sync progress logs | Debug level, every 10% | Should Have |
| NFR-029A-25 | Dependency install logs | Debug level with package manager output | Should Have |
| NFR-029A-26 | Error logs with context | Error level with exception chain | Must Have |
| NFR-029A-27 | Structured logging format | JSON-compatible fields | Should Have |
| NFR-029A-28 | TargetId in all logs | Correlation | Must Have |
| NFR-029A-29 | Metric: preparation_duration_seconds | Histogram | Should Have |
| NFR-029A-30 | Metric: preparation_phase_duration_seconds | Histogram per phase | Could Have |

---

## User Manual Documentation

### Configuration

```yaml
workspace:
  source: /path/to/repo  # or git URL
  ref: main
  worktree: /tmp/acode-work
  clean: true
  
  cache:
    enabled: true
    path: ~/.acode/cache
    
  dependencies:
    autoDetect: true
    commands:
      - dotnet restore
      - npm ci
      
  prepareCommands:
    - chmod +x scripts/*.sh
```

### Lifecycle

```
Target.PrepareWorkspaceAsync(config)
  ├── Create workspace directory
  ├── Clean existing files (if configured)
  ├── Sync source code
  ├── Checkout specified ref
  ├── Update submodules
  ├── Install dependencies
  ├── Run custom commands
  └── Mark target as Ready
```

### Progress Events

| Event | Data |
|-------|------|
| PreparationStarted | targetId, config |
| SyncProgress | bytesTransferred, total |
| DependencyProgress | ecosystem, status |
| PreparationCompleted | targetId, duration |
| PreparationFailed | targetId, error |

---

## Acceptance Criteria / Definition of Done

### Workspace Creation (AC-029A-01 to AC-029A-10)

- [ ] AC-029A-01: `IWorkspacePreparation` interface defined in Application layer
- [ ] AC-029A-02: `PrepareWorkspaceAsync` method accepts target, config, and cancellation token
- [ ] AC-029A-03: Method creates workspace directory if not exists
- [ ] AC-029A-04: Method cleans existing files when clean=true
- [ ] AC-029A-05: Method preserves existing files when clean=false
- [ ] AC-029A-06: Directory creation logged at Debug level
- [ ] AC-029A-07: Clean operation logged at Debug level with file count
- [ ] AC-029A-08: Invalid path throws `ArgumentException` with path value
- [ ] AC-029A-09: Permission denied throws `UnauthorizedAccessException`
- [ ] AC-029A-10: Disk full throws `IOException` with required space

### Source Synchronization (AC-029A-11 to AC-029A-20)

- [ ] AC-029A-11: `ISourceSyncer` interface defined with sync strategy pattern
- [ ] AC-029A-12: `LocalSourceSyncer` copies files from local path
- [ ] AC-029A-13: `GitSourceSyncer` clones/fetches from git URL
- [ ] AC-029A-14: `RsyncSourceSyncer` syncs using rsync protocol
- [ ] AC-029A-15: Syncer selection automatic based on source URL scheme
- [ ] AC-029A-16: Progress events emitted during sync
- [ ] AC-029A-17: Sync respects cancellation token
- [ ] AC-029A-18: Incremental sync detects unchanged files
- [ ] AC-029A-19: Failed sync cleans up partial files
- [ ] AC-029A-20: Sync duration logged with file count and bytes

### Ref Checkout (AC-029A-21 to AC-029A-30)

- [ ] AC-029A-21: Branch names checked out correctly (main, develop, feature/*)
- [ ] AC-029A-22: Tag names checked out correctly (v1.0.0, release-*)
- [ ] AC-029A-23: Full commit SHAs checked out correctly
- [ ] AC-029A-24: Short commit SHAs rejected with helpful error
- [ ] AC-029A-25: Checkout uses detached HEAD
- [ ] AC-029A-26: Invalid ref throws with available refs listed
- [ ] AC-029A-27: Submodules updated after checkout
- [ ] AC-029A-28: Submodule depth configurable (default 1)
- [ ] AC-029A-29: Submodule failure logged with module name
- [ ] AC-029A-30: Checkout duration logged

### Ecosystem Detection (AC-029A-31 to AC-029A-40)

- [ ] AC-029A-31: `IEcosystemDetector` interface defined
- [ ] AC-029A-32: .NET detected by *.csproj, *.sln, *.fsproj
- [ ] AC-029A-33: Node detected by package.json
- [ ] AC-029A-34: Python detected by requirements.txt, pyproject.toml
- [ ] AC-029A-35: Go detected by go.mod
- [ ] AC-029A-36: Rust detected by Cargo.toml
- [ ] AC-029A-37: Multiple ecosystems detected simultaneously
- [ ] AC-029A-38: Detection returns `EcosystemType` flags enum
- [ ] AC-029A-39: Detection cached per workspace path
- [ ] AC-029A-40: Unknown project files do not cause failure

### Dependency Installation (AC-029A-41 to AC-029A-55)

- [ ] AC-029A-41: `IDependencyInstaller` interface defined
- [ ] AC-029A-42: .NET installer runs `dotnet restore`
- [ ] AC-029A-43: Node installer runs `npm ci` (prefers over `npm install`)
- [ ] AC-029A-44: Python installer runs `pip install -r requirements.txt`
- [ ] AC-029A-45: Go installer runs `go mod download`
- [ ] AC-029A-46: Rust installer runs `cargo fetch`
- [ ] AC-029A-47: Installer output captured to logs
- [ ] AC-029A-48: Non-zero exit code throws with output
- [ ] AC-029A-49: Timeout (5min default) throws `TimeoutException`
- [ ] AC-029A-50: Installation respects cancellation token
- [ ] AC-029A-51: Each ecosystem installed in detected order
- [ ] AC-029A-52: Installation events emitted per ecosystem
- [ ] AC-029A-53: Offline mode uses cache only
- [ ] AC-029A-54: Missing lockfile in strict mode throws
- [ ] AC-029A-55: Successful install logged with duration

### Caching (AC-029A-56 to AC-029A-65)

- [ ] AC-029A-56: `ICacheManager` interface defined
- [ ] AC-029A-57: Cache location configurable (default ~/.acode/cache)
- [ ] AC-029A-58: Cache keyed by lockfile hash
- [ ] AC-029A-59: Cache hit restores from cache
- [ ] AC-029A-60: Cache miss performs full install
- [ ] AC-029A-61: Cache populated after successful install
- [ ] AC-029A-62: Lockfile change invalidates cache
- [ ] AC-029A-63: Cache corruption detected and recovered
- [ ] AC-029A-64: Cache size reported in metrics
- [ ] AC-029A-65: Cache eviction respects max size setting

### Custom Commands (AC-029A-66 to AC-029A-70)

- [ ] AC-029A-66: `prepareCommands` array executed in order
- [ ] AC-029A-67: Each command runs in workspace directory
- [ ] AC-029A-68: Command output captured to logs
- [ ] AC-029A-69: Non-zero exit stops preparation with error
- [ ] AC-029A-70: Commands respect cancellation token

### Error Handling and Cleanup (AC-029A-71 to AC-029A-80)

- [ ] AC-029A-71: Any failure cleans up partial workspace
- [ ] AC-029A-72: Cleanup removes temporary files
- [ ] AC-029A-73: Cleanup logs what was removed
- [ ] AC-029A-74: Target state set to Failed on preparation failure
- [ ] AC-029A-75: Exception includes preparation phase that failed
- [ ] AC-029A-76: Exception includes original error cause
- [ ] AC-029A-77: Retry logic attempts configurable times
- [ ] AC-029A-78: Retry uses exponential backoff
- [ ] AC-029A-79: Final failure logs all retry attempts
- [ ] AC-029A-80: Success transitions target to Ready state

---

## User Verification Scenarios

### Scenario 1: Basic .NET Project Preparation

**Persona:** Developer preparing a .NET solution on local target

**Steps:**
1. Configure workspace with local .NET repo path
2. Specify ref as `main` branch
3. Run preparation
4. Observe ecosystem detection log: ".NET detected via Acode.sln"
5. Observe dependency install: "Running dotnet restore..."
6. Observe completion: "Preparation complete in 45s"
7. Verify workspace contains built dependencies

**Verification:**
- [ ] Workspace created at configured path
- [ ] Correct branch checked out
- [ ] dotnet restore executed successfully
- [ ] Target state is Ready

### Scenario 2: Multi-Ecosystem Project

**Persona:** Developer with frontend + backend project

**Steps:**
1. Configure workspace with repo containing both .NET backend and React frontend
2. Run preparation
3. Observe: "Multiple ecosystems detected: DotNet, Node"
4. Observe: "Running dotnet restore..."
5. Observe: "Running npm ci..."
6. Both ecosystems installed

**Verification:**
- [ ] Both ecosystems detected
- [ ] Both package managers executed
- [ ] All dependencies available

### Scenario 3: Incremental Sync After Code Change

**Persona:** Developer making small code changes

**Steps:**
1. Run initial preparation (full sync)
2. Make small change to one file
3. Run preparation again
4. Observe: "Incremental sync: 1 file changed"
5. Observe preparation completes in <10 seconds

**Verification:**
- [ ] Only changed files transferred
- [ ] Dependencies not reinstalled (cache hit)
- [ ] Significant time savings

### Scenario 4: Network Failure Recovery

**Persona:** Developer on unstable network

**Steps:**
1. Configure workspace with remote git URL
2. Start preparation
3. Simulate network interruption during sync
4. Observe: "Sync failed, retrying (1/3)..."
5. Network recovers
6. Observe: "Retry successful, continuing"
7. Preparation completes

**Verification:**
- [ ] Retry occurs automatically
- [ ] Backoff delay between retries
- [ ] Success after network recovery

### Scenario 5: Preparation Cancellation

**Persona:** Developer who starts wrong preparation

**Steps:**
1. Start preparation with large repository
2. Press Ctrl+C during sync phase
3. Observe: "Preparation cancelled"
4. Observe: "Cleaning up partial workspace..."
5. Verify no partial files remain

**Verification:**
- [ ] Cancellation responsive (<1s)
- [ ] Partial files cleaned up
- [ ] Target state set appropriately

### Scenario 6: Custom Preparation Commands

**Persona:** Developer with special build requirements

**Steps:**
1. Configure prepareCommands with custom scripts
2. Run preparation
3. Observe standard phases complete
4. Observe: "Running custom command: chmod +x scripts/*.sh"
5. Observe: "Running custom command: ./scripts/setup-env.sh"
6. All commands complete

**Verification:**
- [ ] Commands run in specified order
- [ ] Commands run in workspace directory
- [ ] Command failure stops preparation

---

## Testing Requirements

### Unit Tests (UT-029A-01 to UT-029A-25)

- [ ] UT-029A-01: WorkspaceConfig validates source is required
- [ ] UT-029A-02: WorkspaceConfig validates ref is required
- [ ] UT-029A-03: WorkspaceConfig validates workspace path is required
- [ ] UT-029A-04: PreparationPhase enum has all required values
- [ ] UT-029A-05: PreparationProgress calculates percent correctly
- [ ] UT-029A-06: EcosystemDetector finds .csproj files
- [ ] UT-029A-07: EcosystemDetector finds package.json
- [ ] UT-029A-08: EcosystemDetector finds requirements.txt
- [ ] UT-029A-09: EcosystemDetector returns flags for multiple ecosystems
- [ ] UT-029A-10: LocalSourceSyncer copies files correctly
- [ ] UT-029A-11: LocalSourceSyncer handles missing source
- [ ] UT-029A-12: GitSourceSyncer constructs correct git commands
- [ ] UT-029A-13: CacheManager generates key from lockfile hash
- [ ] UT-029A-14: CacheManager detects cache hit
- [ ] UT-029A-15: CacheManager detects lockfile change
- [ ] UT-029A-16: DotNetInstaller runs correct restore command
- [ ] UT-029A-17: NodeInstaller prefers npm ci over npm install
- [ ] UT-029A-18: PythonInstaller handles missing requirements.txt
- [ ] UT-029A-19: Preparation cancellation cleans up
- [ ] UT-029A-20: Preparation timeout triggers after configured time
- [ ] UT-029A-21: Events contain correct data
- [ ] UT-029A-22: Retry logic uses exponential backoff
- [ ] UT-029A-23: Custom commands execute in order
- [ ] UT-029A-24: Failed command stops execution
- [ ] UT-029A-25: Success transitions target to Ready

### Integration Tests (IT-029A-01 to IT-029A-15)

- [ ] IT-029A-01: Full preparation of small .NET project
- [ ] IT-029A-02: Full preparation of Node.js project
- [ ] IT-029A-03: Full preparation of Python project
- [ ] IT-029A-04: Multi-ecosystem project preparation
- [ ] IT-029A-05: Incremental sync with changed files
- [ ] IT-029A-06: Cache hit restores quickly
- [ ] IT-029A-07: Cache invalidation on lockfile change
- [ ] IT-029A-08: Large repo (1GB+) preparation
- [ ] IT-029A-09: Cancellation during each phase
- [ ] IT-029A-10: Network failure retry
- [ ] IT-029A-11: Disk space check prevents start
- [ ] IT-029A-12: Custom commands execute correctly
- [ ] IT-029A-13: Parallel preparation of multiple targets
- [ ] IT-029A-14: Progress events emitted correctly
- [ ] IT-029A-15: Cross-platform execution (Windows, macOS, Linux)

## Implementation Prompt

You are implementing workspace preparation for compute targets. This handles code sync, dependency installation, and environment setup. Follow Clean Architecture and TDD.

### Part 1: File Structure and Domain Models

#### File Structure

```
src/Acode.Domain/
├── Compute/
│   └── Workspace/
│       ├── PreparationPhase.cs
│       ├── PreparationProgress.cs
│       ├── WorkspaceConfig.cs
│       ├── EcosystemType.cs
│       └── Events/
│           ├── PreparationStartedEvent.cs
│           ├── SyncProgressEvent.cs
│           ├── DependencyInstalledEvent.cs
│           └── PreparationCompletedEvent.cs

src/Acode.Application/
├── Compute/
│   └── Workspace/
│       ├── IWorkspacePreparation.cs
│       ├── IEcosystemDetector.cs
│       ├── IDependencyInstaller.cs
│       ├── ISourceSyncer.cs
│       └── ICacheManager.cs

src/Acode.Infrastructure/
├── Compute/
│   └── Workspace/
│       ├── WorkspacePreparation.cs
│       ├── EcosystemDetector.cs
│       ├── SourceSyncer/
│       │   ├── LocalSourceSyncer.cs
│       │   ├── GitSourceSyncer.cs
│       │   └── RsyncSourceSyncer.cs
│       ├── DependencyInstaller/
│       │   ├── DotNetDependencyInstaller.cs
│       │   ├── NodeDependencyInstaller.cs
│       │   └── PythonDependencyInstaller.cs
│       └── Cache/
│           ├── DependencyCacheManager.cs
│           └── CacheInvalidator.cs

tests/Acode.Domain.Tests/
├── Compute/
│   └── Workspace/
│       ├── WorkspaceConfigTests.cs
│       └── PreparationProgressTests.cs

tests/Acode.Infrastructure.Tests/
├── Compute/
│   └── Workspace/
│       ├── WorkspacePreparationTests.cs
│       ├── EcosystemDetectorTests.cs
│       ├── SourceSyncer/
│       │   ├── LocalSourceSyncerTests.cs
│       │   └── GitSourceSyncerTests.cs
│       └── DependencyInstaller/
│           ├── DotNetDependencyInstallerTests.cs
│           ├── NodeDependencyInstallerTests.cs
│           └── PythonDependencyInstallerTests.cs

tests/Acode.Integration.Tests/
├── Compute/
│   └── Workspace/
│       ├── FullPreparationTests.cs
│       ├── LargeRepoSyncTests.cs
│       └── MultiEcosystemTests.cs
```

#### Domain Models

```csharp
// src/Acode.Domain/Compute/Workspace/PreparationPhase.cs
namespace Acode.Domain.Compute.Workspace;

public enum PreparationPhase
{
    NotStarted = 0,
    Creating = 1,
    Cleaning = 2,
    Syncing = 3,
    CheckingOut = 4,
    UpdatingSubmodules = 5,
    DetectingEcosystems = 6,
    InstallingDependencies = 7,
    RunningCommands = 8,
    Completed = 9,
    Failed = 10
}

// src/Acode.Domain/Compute/Workspace/PreparationProgress.cs
namespace Acode.Domain.Compute.Workspace;

public sealed record PreparationProgress
{
    public required PreparationPhase Phase { get; init; }
    public required double PercentComplete { get; init; }
    public required string Message { get; init; }
    public long? BytesTransferred { get; init; }
    public long? TotalBytes { get; init; }
    public TimeSpan? EstimatedRemaining { get; init; }
    public string? CurrentFile { get; init; }
}

// src/Acode.Domain/Compute/Workspace/EcosystemType.cs
namespace Acode.Domain.Compute.Workspace;

[Flags]
public enum EcosystemType
{
    None = 0,
    DotNet = 1,
    Node = 2,
    Python = 4,
    Go = 8,
    Rust = 16,
    Java = 32
}

// src/Acode.Domain/Compute/Workspace/Events/PreparationStartedEvent.cs
namespace Acode.Domain.Compute.Workspace.Events;

public sealed record PreparationStartedEvent(
    ComputeTargetId TargetId,
    WorkspaceConfig Config,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Workspace/Events/SyncProgressEvent.cs
namespace Acode.Domain.Compute.Workspace.Events;

public sealed record SyncProgressEvent(
    ComputeTargetId TargetId,
    long BytesTransferred,
    long TotalBytes,
    int FilesTransferred,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Workspace/Events/PreparationCompletedEvent.cs
namespace Acode.Domain.Compute.Workspace.Events;

public sealed record PreparationCompletedEvent(
    ComputeTargetId TargetId,
    TimeSpan Duration,
    EcosystemType DetectedEcosystems,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 029.a Specification - Part 1/4**

### Part 2: Application Layer Interfaces

```csharp
// src/Acode.Application/Compute/Workspace/IWorkspacePreparation.cs
namespace Acode.Application.Compute.Workspace;

public interface IWorkspacePreparation
{
    Task PrepareAsync(
        IComputeTarget target,
        WorkspaceConfig config,
        IProgress<PreparationProgress>? progress = null,
        CancellationToken ct = default);
    
    Task<bool> ValidateWorkspaceAsync(
        string workspacePath,
        CancellationToken ct = default);
    
    Task CleanupAsync(
        string workspacePath,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Workspace/IEcosystemDetector.cs
namespace Acode.Application.Compute.Workspace;

public interface IEcosystemDetector
{
    EcosystemType Detect(string workspacePath);
    IReadOnlyList<EcosystemInfo> GetDetailedInfo(string workspacePath);
}

public sealed record EcosystemInfo(
    EcosystemType Type,
    string RootPath,
    string? LockFile,
    string? ConfigFile);

// src/Acode.Application/Compute/Workspace/IDependencyInstaller.cs
namespace Acode.Application.Compute.Workspace;

public interface IDependencyInstaller
{
    EcosystemType SupportedEcosystem { get; }
    
    Task InstallAsync(
        string workspacePath,
        DependencyConfig? config,
        IProgress<DependencyProgress>? progress = null,
        CancellationToken ct = default);
    
    Task<bool> IsInstalledAsync(string workspacePath, CancellationToken ct = default);
}

public sealed record DependencyProgress(
    string Message,
    double PercentComplete,
    int PackagesInstalled,
    int TotalPackages);

// src/Acode.Application/Compute/Workspace/ISourceSyncer.cs
namespace Acode.Application.Compute.Workspace;

public interface ISourceSyncer
{
    bool CanHandle(string sourcePath);
    
    Task SyncAsync(
        string source,
        string destination,
        string? gitRef,
        SyncOptions options,
        IProgress<SyncProgress>? progress = null,
        CancellationToken ct = default);
}

public sealed record SyncOptions(
    bool Incremental = true,
    bool IncludeSubmodules = true,
    int SubmoduleDepth = 1,
    IReadOnlyList<string>? ExcludePatterns = null);

public sealed record SyncProgress(
    long BytesTransferred,
    long TotalBytes,
    int FilesTransferred,
    int TotalFiles,
    string? CurrentFile);

// src/Acode.Application/Compute/Workspace/ICacheManager.cs
namespace Acode.Application.Compute.Workspace;

public interface ICacheManager
{
    Task<string?> GetCachePathAsync(
        EcosystemType ecosystem,
        string lockFileHash,
        CancellationToken ct = default);
    
    Task StoreCacheAsync(
        EcosystemType ecosystem,
        string dependencyPath,
        string lockFileHash,
        CancellationToken ct = default);
    
    Task<bool> RestoreCacheAsync(
        string cachePath,
        string destinationPath,
        CancellationToken ct = default);
    
    Task InvalidateCacheAsync(
        EcosystemType ecosystem,
        CancellationToken ct = default);
    
    Task<CacheStats> GetStatsAsync(CancellationToken ct = default);
}

public sealed record CacheStats(
    long TotalSize,
    int EntryCount,
    DateTimeOffset OldestEntry,
    IReadOnlyDictionary<EcosystemType, long> SizeByEcosystem);
```

**End of Task 029.a Specification - Part 2/4**

### Part 3: Infrastructure - Source Syncers and Ecosystem Detection

```csharp
// src/Acode.Infrastructure/Compute/Workspace/WorkspacePreparation.cs
namespace Acode.Infrastructure.Compute.Workspace;

public sealed class WorkspacePreparation : IWorkspacePreparation
{
    private readonly IEnumerable<ISourceSyncer> _syncers;
    private readonly IEcosystemDetector _ecosystemDetector;
    private readonly IEnumerable<IDependencyInstaller> _installers;
    private readonly ICacheManager _cacheManager;
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly IEventPublisher _events;
    private readonly ILogger<WorkspacePreparation> _logger;
    
    public WorkspacePreparation(
        IEnumerable<ISourceSyncer> syncers,
        IEcosystemDetector ecosystemDetector,
        IEnumerable<IDependencyInstaller> installers,
        ICacheManager cacheManager,
        IProcessRunner processRunner,
        IFileSystem fileSystem,
        IEventPublisher events,
        ILogger<WorkspacePreparation> logger)
    {
        _syncers = syncers;
        _ecosystemDetector = ecosystemDetector;
        _installers = installers;
        _cacheManager = cacheManager;
        _processRunner = processRunner;
        _fileSystem = fileSystem;
        _events = events;
        _logger = logger;
    }
    
    public async Task PrepareAsync(
        IComputeTarget target,
        WorkspaceConfig config,
        IProgress<PreparationProgress>? progress = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await _events.PublishAsync(new PreparationStartedEvent(
            target.Id, config, DateTimeOffset.UtcNow));
        
        try
        {
            // Step 1: Create workspace directory
            Report(progress, PreparationPhase.Creating, 5, "Creating workspace directory");
            _fileSystem.Directory.CreateDirectory(config.WorktreePath);
            
            // Step 2: Clean if configured
            if (config.CleanBeforeSync)
            {
                Report(progress, PreparationPhase.Cleaning, 10, "Cleaning existing files");
                await CleanupAsync(config.WorktreePath, ct);
            }
            
            // Step 3: Sync source code
            Report(progress, PreparationPhase.Syncing, 15, "Syncing source code");
            var syncer = _syncers.FirstOrDefault(s => s.CanHandle(config.SourcePath))
                ?? throw new InvalidOperationException($"No syncer for: {config.SourcePath}");
            
            var syncProgress = new Progress<SyncProgress>(p =>
                Report(progress, PreparationPhase.Syncing, 
                    15 + (p.BytesTransferred * 35 / Math.Max(p.TotalBytes, 1)),
                    $"Syncing: {p.CurrentFile}",
                    p.BytesTransferred, p.TotalBytes));
            
            await syncer.SyncAsync(
                config.SourcePath,
                config.WorktreePath,
                config.Ref,
                new SyncOptions(),
                syncProgress,
                ct);
            
            // Step 4: Detect ecosystems
            Report(progress, PreparationPhase.DetectingEcosystems, 55, "Detecting ecosystems");
            var ecosystems = _ecosystemDetector.Detect(config.WorktreePath);
            
            // Step 5: Install dependencies
            if (config.Dependencies?.AutoDetect ?? true)
            {
                Report(progress, PreparationPhase.InstallingDependencies, 60, "Installing dependencies");
                await InstallDependenciesAsync(
                    config.WorktreePath, ecosystems, config, progress, ct);
            }
            
            // Step 6: Run custom commands
            if (config.PrepareCommands?.Count > 0)
            {
                Report(progress, PreparationPhase.RunningCommands, 90, "Running prepare commands");
                await RunPrepareCommandsAsync(
                    config.WorktreePath, config.PrepareCommands, ct);
            }
            
            stopwatch.Stop();
            Report(progress, PreparationPhase.Completed, 100, "Workspace ready");
            
            await _events.PublishAsync(new PreparationCompletedEvent(
                target.Id, stopwatch.Elapsed, ecosystems, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workspace preparation failed for {TargetId}", target.Id);
            Report(progress, PreparationPhase.Failed, 0, ex.Message);
            throw;
        }
    }
    
    private async Task InstallDependenciesAsync(
        string workspacePath,
        EcosystemType ecosystems,
        WorkspaceConfig config,
        IProgress<PreparationProgress>? progress,
        CancellationToken ct)
    {
        foreach (var installer in _installers)
        {
            if (!ecosystems.HasFlag(installer.SupportedEcosystem))
                continue;
            
            // Check cache first
            if (config.Cache?.Enabled ?? false)
            {
                var lockHash = ComputeLockFileHash(workspacePath, installer.SupportedEcosystem);
                var cachePath = await _cacheManager.GetCachePathAsync(
                    installer.SupportedEcosystem, lockHash, ct);
                
                if (cachePath != null)
                {
                    var depPath = GetDependencyPath(workspacePath, installer.SupportedEcosystem);
                    if (await _cacheManager.RestoreCacheAsync(cachePath, depPath, ct))
                    {
                        _logger.LogInformation("Restored {Ecosystem} from cache", 
                            installer.SupportedEcosystem);
                        continue;
                    }
                }
            }
            
            await installer.InstallAsync(workspacePath, config.Dependencies, null, ct);
            
            // Store in cache
            if (config.Cache?.Enabled ?? false)
            {
                var lockHash = ComputeLockFileHash(workspacePath, installer.SupportedEcosystem);
                var depPath = GetDependencyPath(workspacePath, installer.SupportedEcosystem);
                await _cacheManager.StoreCacheAsync(
                    installer.SupportedEcosystem, depPath, lockHash, ct);
            }
        }
    }
    
    private async Task RunPrepareCommandsAsync(
        string workspacePath,
        IReadOnlyList<string> commands,
        CancellationToken ct)
    {
        foreach (var cmd in commands)
        {
            var result = await _processRunner.RunAsync(
                cmd, [], workspacePath, null, TimeSpan.FromMinutes(5), ct);
            
            if (result.ExitCode != 0)
                throw new InvalidOperationException($"Prepare command failed: {cmd}\n{result.StdErr}");
        }
    }
    
    private static void Report(
        IProgress<PreparationProgress>? progress,
        PreparationPhase phase, double pct, string msg,
        long? bytesTransferred = null, long? totalBytes = null)
    {
        progress?.Report(new PreparationProgress
        {
            Phase = phase,
            PercentComplete = pct,
            Message = msg,
            BytesTransferred = bytesTransferred,
            TotalBytes = totalBytes
        });
    }
}

// src/Acode.Infrastructure/Compute/Workspace/EcosystemDetector.cs
namespace Acode.Infrastructure.Compute.Workspace;

public sealed class EcosystemDetector : IEcosystemDetector
{
    private readonly IFileSystem _fileSystem;
    
    private static readonly (string File, EcosystemType Type)[] Indicators =
    [
        ("*.csproj", EcosystemType.DotNet),
        ("*.fsproj", EcosystemType.DotNet),
        ("*.sln", EcosystemType.DotNet),
        ("package.json", EcosystemType.Node),
        ("requirements.txt", EcosystemType.Python),
        ("pyproject.toml", EcosystemType.Python),
        ("setup.py", EcosystemType.Python),
        ("go.mod", EcosystemType.Go),
        ("Cargo.toml", EcosystemType.Rust),
        ("pom.xml", EcosystemType.Java),
        ("build.gradle", EcosystemType.Java)
    ];
    
    public EcosystemDetector(IFileSystem fileSystem) => _fileSystem = fileSystem;
    
    public EcosystemType Detect(string workspacePath)
    {
        var result = EcosystemType.None;
        
        foreach (var (pattern, type) in Indicators)
        {
            var files = _fileSystem.Directory.GetFiles(workspacePath, pattern, SearchOption.AllDirectories);
            if (files.Length > 0)
                result |= type;
        }
        
        return result;
    }
    
    public IReadOnlyList<EcosystemInfo> GetDetailedInfo(string workspacePath)
    {
        var results = new List<EcosystemInfo>();
        
        foreach (var (pattern, type) in Indicators)
        {
            foreach (var file in _fileSystem.Directory.GetFiles(workspacePath, pattern, SearchOption.AllDirectories))
            {
                results.Add(new EcosystemInfo(
                    type,
                    Path.GetDirectoryName(file)!,
                    GetLockFile(Path.GetDirectoryName(file)!, type),
                    file));
            }
        }
        
        return results;
    }
    
    private string? GetLockFile(string dir, EcosystemType type) => type switch
    {
        EcosystemType.Node => FindFile(dir, "package-lock.json", "yarn.lock", "pnpm-lock.yaml"),
        EcosystemType.Python => FindFile(dir, "requirements.txt", "poetry.lock", "Pipfile.lock"),
        EcosystemType.DotNet => FindFile(dir, "packages.lock.json"),
        _ => null
    };
    
    private string? FindFile(string dir, params string[] names)
    {
        foreach (var name in names)
        {
            var path = Path.Combine(dir, name);
            if (_fileSystem.File.Exists(path))
                return path;
        }
        return null;
    }
}
```

**End of Task 029.a Specification - Part 3/4**

### Part 4: Dependency Installers, Cache Manager, and Rollout

```csharp
// src/Acode.Infrastructure/Compute/Workspace/DependencyInstaller/DotNetDependencyInstaller.cs
namespace Acode.Infrastructure.Compute.Workspace.DependencyInstaller;

public sealed class DotNetDependencyInstaller : IDependencyInstaller
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<DotNetDependencyInstaller> _logger;
    
    public EcosystemType SupportedEcosystem => EcosystemType.DotNet;
    
    public DotNetDependencyInstaller(
        IProcessRunner processRunner,
        ILogger<DotNetDependencyInstaller> logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }
    
    public async Task InstallAsync(
        string workspacePath,
        DependencyConfig? config,
        IProgress<DependencyProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new DependencyProgress("Running dotnet restore", 0, 0, 0));
        
        var result = await _processRunner.RunAsync(
            "dotnet", ["restore", "--verbosity", "minimal"],
            workspacePath, null, TimeSpan.FromMinutes(10), ct);
        
        if (result.ExitCode != 0)
        {
            _logger.LogError("dotnet restore failed: {Error}", result.StdErr);
            throw new InvalidOperationException($"dotnet restore failed: {result.StdErr}");
        }
        
        progress?.Report(new DependencyProgress("Restore complete", 100, 0, 0));
    }
    
    public async Task<bool> IsInstalledAsync(string workspacePath, CancellationToken ct)
    {
        // Check if obj folders exist with project.assets.json
        var objDirs = Directory.GetDirectories(workspacePath, "obj", SearchOption.AllDirectories);
        return objDirs.Any(d => File.Exists(Path.Combine(d, "project.assets.json")));
    }
}

// src/Acode.Infrastructure/Compute/Workspace/DependencyInstaller/NodeDependencyInstaller.cs
namespace Acode.Infrastructure.Compute.Workspace.DependencyInstaller;

public sealed class NodeDependencyInstaller : IDependencyInstaller
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<NodeDependencyInstaller> _logger;
    
    public EcosystemType SupportedEcosystem => EcosystemType.Node;
    
    public NodeDependencyInstaller(
        IProcessRunner processRunner,
        IFileSystem fileSystem,
        ILogger<NodeDependencyInstaller> logger)
    {
        _processRunner = processRunner;
        _fileSystem = fileSystem;
        _logger = logger;
    }
    
    public async Task InstallAsync(
        string workspacePath,
        DependencyConfig? config,
        IProgress<DependencyProgress>? progress = null,
        CancellationToken ct = default)
    {
        var (cmd, args) = DeterminePackageManager(workspacePath);
        progress?.Report(new DependencyProgress($"Running {cmd} {string.Join(" ", args)}", 0, 0, 0));
        
        var result = await _processRunner.RunAsync(
            cmd, args, workspacePath, null, TimeSpan.FromMinutes(15), ct);
        
        if (result.ExitCode != 0)
        {
            _logger.LogError("{Cmd} failed: {Error}", cmd, result.StdErr);
            throw new InvalidOperationException($"{cmd} install failed: {result.StdErr}");
        }
        
        progress?.Report(new DependencyProgress("Install complete", 100, 0, 0));
    }
    
    private (string cmd, string[] args) DeterminePackageManager(string workspacePath)
    {
        if (_fileSystem.File.Exists(Path.Combine(workspacePath, "pnpm-lock.yaml")))
            return ("pnpm", ["install", "--frozen-lockfile"]);
        if (_fileSystem.File.Exists(Path.Combine(workspacePath, "yarn.lock")))
            return ("yarn", ["install", "--frozen-lockfile"]);
        return ("npm", ["ci"]);
    }
    
    public Task<bool> IsInstalledAsync(string workspacePath, CancellationToken ct)
    {
        var nodeModules = Path.Combine(workspacePath, "node_modules");
        return Task.FromResult(_fileSystem.Directory.Exists(nodeModules));
    }
}

// src/Acode.Infrastructure/Compute/Workspace/DependencyInstaller/PythonDependencyInstaller.cs
namespace Acode.Infrastructure.Compute.Workspace.DependencyInstaller;

public sealed class PythonDependencyInstaller : IDependencyInstaller
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<PythonDependencyInstaller> _logger;
    
    public EcosystemType SupportedEcosystem => EcosystemType.Python;
    
    public async Task InstallAsync(
        string workspacePath,
        DependencyConfig? config,
        IProgress<DependencyProgress>? progress = null,
        CancellationToken ct = default)
    {
        var (cmd, args) = DetermineInstallCommand(workspacePath);
        progress?.Report(new DependencyProgress($"Running {cmd}", 0, 0, 0));
        
        var result = await _processRunner.RunAsync(
            cmd, args, workspacePath, null, TimeSpan.FromMinutes(15), ct);
        
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Python install failed: {result.StdErr}");
        
        progress?.Report(new DependencyProgress("Install complete", 100, 0, 0));
    }
    
    private (string cmd, string[] args) DetermineInstallCommand(string path)
    {
        if (_fileSystem.File.Exists(Path.Combine(path, "pyproject.toml")))
            return ("pip", ["install", "-e", "."]);
        if (_fileSystem.File.Exists(Path.Combine(path, "requirements.txt")))
            return ("pip", ["install", "-r", "requirements.txt"]);
        return ("pip", ["install", "."]);
    }
}

// src/Acode.Infrastructure/Compute/Workspace/Cache/DependencyCacheManager.cs
namespace Acode.Infrastructure.Compute.Workspace.Cache;

public sealed class DependencyCacheManager : ICacheManager
{
    private readonly string _cacheRoot;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<DependencyCacheManager> _logger;
    
    public DependencyCacheManager(
        IOptions<CacheOptions> options,
        IFileSystem fileSystem,
        ILogger<DependencyCacheManager> logger)
    {
        _cacheRoot = options.Value.CachePath;
        _fileSystem = fileSystem;
        _logger = logger;
    }
    
    public Task<string?> GetCachePathAsync(
        EcosystemType ecosystem,
        string lockFileHash,
        CancellationToken ct = default)
    {
        var path = GetCacheEntryPath(ecosystem, lockFileHash);
        return Task.FromResult(_fileSystem.Directory.Exists(path) ? path : null);
    }
    
    public async Task StoreCacheAsync(
        EcosystemType ecosystem,
        string dependencyPath,
        string lockFileHash,
        CancellationToken ct = default)
    {
        var cachePath = GetCacheEntryPath(ecosystem, lockFileHash);
        
        if (_fileSystem.Directory.Exists(cachePath))
            _fileSystem.Directory.Delete(cachePath, true);
        
        await CopyDirectoryAsync(dependencyPath, cachePath, ct);
        
        _logger.LogInformation(
            "Cached {Ecosystem} dependencies at {Path}", ecosystem, cachePath);
    }
    
    public async Task<bool> RestoreCacheAsync(
        string cachePath,
        string destinationPath,
        CancellationToken ct = default)
    {
        if (!_fileSystem.Directory.Exists(cachePath))
            return false;
        
        await CopyDirectoryAsync(cachePath, destinationPath, ct);
        return true;
    }
    
    public Task InvalidateCacheAsync(EcosystemType ecosystem, CancellationToken ct)
    {
        var ecosystemPath = Path.Combine(_cacheRoot, ecosystem.ToString().ToLowerInvariant());
        if (_fileSystem.Directory.Exists(ecosystemPath))
            _fileSystem.Directory.Delete(ecosystemPath, true);
        return Task.CompletedTask;
    }
    
    private string GetCacheEntryPath(EcosystemType ecosystem, string hash) =>
        Path.Combine(_cacheRoot, ecosystem.ToString().ToLowerInvariant(), hash[..16]);
    
    private async Task CopyDirectoryAsync(string source, string dest, CancellationToken ct)
    {
        _fileSystem.Directory.CreateDirectory(dest);
        await Task.Run(() =>
        {
            foreach (var file in _fileSystem.Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                var relative = Path.GetRelativePath(source, file);
                var destFile = Path.Combine(dest, relative);
                _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                _fileSystem.File.Copy(file, destFile, true);
            }
        }, ct);
    }
}

// src/Acode.Infrastructure/Compute/Workspace/SourceSyncer/LocalSourceSyncer.cs
namespace Acode.Infrastructure.Compute.Workspace.SourceSyncer;

public sealed class LocalSourceSyncer : ISourceSyncer
{
    private readonly IFileSystem _fileSystem;
    
    public LocalSourceSyncer(IFileSystem fileSystem) => _fileSystem = fileSystem;
    
    public bool CanHandle(string sourcePath) =>
        _fileSystem.Directory.Exists(sourcePath) && !sourcePath.StartsWith("git://");
    
    public async Task SyncAsync(
        string source,
        string destination,
        string? gitRef,
        SyncOptions options,
        IProgress<SyncProgress>? progress = null,
        CancellationToken ct = default)
    {
        var files = _fileSystem.Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)
            .Where(f => !ShouldExclude(f, source, options.ExcludePatterns))
            .ToList();
        
        long totalSize = files.Sum(f => new FileInfo(f).Length);
        long transferred = 0;
        int fileCount = 0;
        
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            
            var relative = Path.GetRelativePath(source, file);
            var destFile = Path.Combine(destination, relative);
            
            _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            _fileSystem.File.Copy(file, destFile, true);
            
            var size = new FileInfo(file).Length;
            transferred += size;
            fileCount++;
            
            progress?.Report(new SyncProgress(transferred, totalSize, fileCount, files.Count, relative));
        }
    }
    
    private static bool ShouldExclude(string file, string root, IReadOnlyList<string>? patterns)
    {
        if (patterns is null) return false;
        var relative = Path.GetRelativePath(root, file);
        return patterns.Any(p => Glob.IsMatch(relative, p));
    }
}
```

---

## Implementation Checklist

- [ ] Create PreparationPhase and PreparationProgress records
- [ ] Define EcosystemType flags enum
- [ ] Create preparation events for audit trail
- [ ] Implement IWorkspacePreparation with full lifecycle
- [ ] Build EcosystemDetector with file-pattern matching
- [ ] Implement DotNetDependencyInstaller (dotnet restore)
- [ ] Implement NodeDependencyInstaller (npm/yarn/pnpm)
- [ ] Implement PythonDependencyInstaller (pip)
- [ ] Create DependencyCacheManager with hash-based lookup
- [ ] Build LocalSourceSyncer for filesystem copies
- [ ] Add progress reporting throughout pipeline
- [ ] Write unit tests for each component (TDD)
- [ ] Write integration tests for full preparation
- [ ] Test cache hit/miss scenarios
- [ ] Test multi-ecosystem projects
- [ ] Verify cleanup on failure

---

## Rollout Plan

1. **Phase 1**: Domain models (phases, progress, events)
2. **Phase 2**: Application interfaces
3. **Phase 3**: EcosystemDetector implementation
4. **Phase 4**: Source syncers (local, git, rsync)
5. **Phase 5**: Dependency installers per ecosystem
6. **Phase 6**: Cache manager
7. **Phase 7**: WorkspacePreparation orchestrator
8. **Phase 8**: Integration testing

---

**End of Task 029.a Specification**