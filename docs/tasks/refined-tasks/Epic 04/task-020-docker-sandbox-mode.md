# Task 020: Docker Sandbox Mode

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 018 (Command Runner), Task 001 (Operating Modes)  

---

## Description

### Business Value

Docker Sandbox Mode provides the security isolation layer that enables Agentic Coding Bot to safely execute untrusted or potentially dangerous code. This is critically important because:

1. **Security Isolation:** When the agent modifies and runs code, that code could contain bugs, malware, or unintended side effects. Container isolation prevents these from affecting the host system, protecting user data and system integrity.

2. **Reproducible Environment:** Containers ensure consistent execution environments. The same code runs the same way regardless of host system configuration, eliminating "works on my machine" issues.

3. **Resource Protection:** Runaway processes could consume unlimited CPU, memory, or disk. Container resource limits prevent denial-of-service conditions on the developer's machine.

4. **Network Control:** Malicious or buggy code might make unwanted network requests. Container network policies control and optionally block all network access.

5. **Clean State:** Each task gets a fresh container, preventing state leakage between agent tasks. File modifications in one task cannot affect another.

6. **Enterprise Security:** Organizations require that AI agents operate within defined security boundaries. Container sandboxing provides an auditable, enforceable security perimeter.

7. **Air-Gapped Compliance:** In air-gapped environments, containers with disabled networking ensure no data exfiltration is possible, even from compromised dependencies.

8. **Rollback Safety:** If code execution causes file corruption, only mounted directories are affected. The container's destruction reverts any internal changes.

### Scope

This task defines the complete Docker sandbox infrastructure:

1. **ISandbox Interface:** The contract for sandboxed command execution, accepting commands and policies, returning structured results.

2. **Container Lifecycle Management:** Create, start, execute, stop, and remove containers. Handle orphaned container cleanup.

3. **Mount Configuration:** Repository mounting with configurable read/write permissions. Additional mount points for caches and artifacts.

4. **Resource Limits:** CPU, memory, disk (tmpfs), and PID limits to prevent resource exhaustion.

5. **Network Policies:** Default network disabled, configurable enablement, DNS control for allowed network mode.

6. **Image Management:** Default images per language, custom image support, image pulling with timeout.

7. **Output Capture:** Capture stdout/stderr from container execution.

8. **Docker API Integration:** Communication with Docker daemon via Docker.DotNet SDK.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 018 (Command) | Execution Delegation | Command executor delegates to sandbox in Docker mode |
| Task 001 (Modes) | Mode Detection | Operating mode determines if sandbox is used |
| Task 002 (Config) | Configuration | Sandbox settings in `.agent/config.yml` |
| Task 020.a (Strategy) | Container Strategy | Per-task container creation strategy |
| Task 020.b (Caches) | Volume Mounts | NuGet/npm cache volume mounting |
| Task 020.c (Policy) | Policy Enforcement | Security policy rules inside container |
| Task 003 (DI) | Dependency Injection | ISandbox registered based on mode |
| Task 050 (Database) | Audit Logging | Container lifecycle events logged |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Docker not installed | Cannot use sandbox mode | Detect at startup, clear error, fall back to local with warning |
| Docker not running | Container operations fail | Health check Docker daemon, prompt to start |
| Image pull fails | Cannot create container | Retry with backoff, offline fallback to cached images |
| Container creation fails | Command cannot execute | Clear error message, check Docker resources |
| Mount permission denied | Cannot access repository | Check Docker permissions, provide fix instructions |
| Resource limit exceeded | Container killed | Capture partial output, report resource issue |
| Container orphaned | Resource leak | Cleanup on startup, periodic health check |
| Network policy violation | Unexpected failures | Clear error explaining network restriction |
| Docker API timeout | Slow operations | Configurable timeouts, retry logic |
| Disk space exhaustion | Container fails | Monitor disk space, cleanup old images |

### Assumptions

1. Docker is installed and running on the host system
2. The user has permission to run Docker commands (docker group or elevated)
3. Docker API is accessible via local socket or TCP
4. Sufficient disk space exists for container images
5. Container images are available from public registries (or cached)
6. The repository path is accessible for mounting
7. Docker version supports required features (API 1.41+)
8. Host kernel supports container isolation (Linux or WSL2)
9. No conflicting container names exist
10. Network policies are enforceable by Docker

### Security Considerations

Docker sandboxing is the primary security boundary for untrusted code execution:

1. **Non-Root Execution:** Containers MUST run as non-root user. Root in container can escape in some configurations.

2. **Capability Dropping:** Containers MUST drop all unnecessary Linux capabilities. Only NET_BIND_SERVICE if networking is allowed.

3. **Read-Only Root:** Container root filesystem SHOULD be read-only where possible. Use tmpfs for writable areas.

4. **No Privileged Mode:** Containers MUST NEVER run in privileged mode. This bypasses all isolation.

5. **Seccomp Profiles:** Apply restrictive seccomp profiles to limit system calls.

6. **User Namespace:** Consider user namespace remapping for additional isolation.

7. **Mount Restrictions:** Only mount necessary paths. Never mount /etc, /var, or system directories.

8. **Network Isolation:** Default to no network. When enabled, restrict to necessary egress only.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Sandbox | Isolated execution environment |
| Container | Docker container instance |
| Image | Container template |
| Mount | Filesystem binding |
| Resource Limit | CPU/memory constraint |
| Network Policy | Allowed connections |
| Lifecycle | Create, run, cleanup phases |

---

## Out of Scope

- **Container orchestration** - Single container only
- **Container building** - Pre-built images only
- **Registry authentication** - Public images only for v1
- **GPU passthrough** - CPU only
- **Windows containers** - Linux containers only

---

## Functional Requirements

### Sandbox Interface (FR-020-01 to FR-020-15)

| ID | Requirement |
|----|-------------|
| FR-020-01 | System MUST define `ISandbox` interface |
| FR-020-02 | ISandbox MUST have `RunAsync(Command, SandboxPolicy, CancellationToken)` method |
| FR-020-03 | RunAsync MUST return `SandboxResult` with output and exit code |
| FR-020-04 | ISandbox MUST have `CleanupAsync()` method for resource cleanup |
| FR-020-05 | ISandbox MUST have `IsAvailable` property checking Docker availability |
| FR-020-06 | ISandbox MUST have `GetContainersAsync()` listing managed containers |
| FR-020-07 | ISandbox MUST implement IAsyncDisposable for cleanup |
| FR-020-08 | ISandbox MUST support cancellation token for abort |
| FR-020-09 | ISandbox MUST log all operations with correlation IDs |
| FR-020-10 | ISandbox MUST emit metrics for container operations |
| FR-020-11 | SandboxResult MUST include stdout string |
| FR-020-12 | SandboxResult MUST include stderr string |
| FR-020-13 | SandboxResult MUST include exit code |
| FR-020-14 | SandboxResult MUST include container ID |
| FR-020-15 | SandboxResult MUST include execution duration |

### Container Lifecycle (FR-020-16 to FR-020-35)

| ID | Requirement |
|----|-------------|
| FR-020-16 | System MUST create container before command execution |
| FR-020-17 | Container creation MUST use specified image |
| FR-020-18 | Container creation MUST configure mounts |
| FR-020-19 | Container creation MUST configure resource limits |
| FR-020-20 | Container creation MUST configure network mode |
| FR-020-21 | System MUST start container after creation |
| FR-020-22 | System MUST wait for command completion with timeout |
| FR-020-23 | System MUST stop container after completion |
| FR-020-24 | System MUST remove container after stop |
| FR-020-25 | Container removal MUST force remove if needed |
| FR-020-26 | System MUST handle orphaned containers on startup |
| FR-020-27 | Orphaned container detection MUST use label filter |
| FR-020-28 | Containers MUST be labeled with `acode.managed=true` |
| FR-020-29 | Containers MUST be labeled with session ID |
| FR-020-30 | Containers MUST be labeled with task ID |
| FR-020-31 | Container names MUST follow pattern `acode-{session}-{task}` |
| FR-020-32 | System MUST handle concurrent container operations |
| FR-020-33 | System MUST retry failed container operations |
| FR-020-34 | Retry MUST have configurable attempts and backoff |
| FR-020-35 | System MUST timeout container creation at configurable limit |

### Mount Configuration (FR-020-36 to FR-020-50)

| ID | Requirement |
|----|-------------|
| FR-020-36 | Repository MUST be mounted at configurable path (default `/workspace`) |
| FR-020-37 | Repository mount MUST be read-write by default |
| FR-020-38 | Repository mount MUST support read-only option |
| FR-020-39 | Additional mounts MUST be configurable |
| FR-020-40 | Host paths outside repository MUST be rejected by default |
| FR-020-41 | Mount path validation MUST prevent escape attempts |
| FR-020-42 | Mounts MUST use bind mount type |
| FR-020-43 | Mount propagation MUST be `rprivate` |
| FR-020-44 | System MUST support volume mounts for caches |
| FR-020-45 | Cache volumes MUST be named with prefix `acode-cache-` |
| FR-020-46 | Cache volumes MUST persist across container restarts |
| FR-020-47 | System MUST support tmpfs mounts for temporary data |
| FR-020-48 | tmpfs size MUST be configurable |
| FR-020-49 | Mount errors MUST be reported clearly |
| FR-020-50 | System MUST validate mount sources exist before mounting |

### Resource Limits (FR-020-51 to FR-020-65)

| ID | Requirement |
|----|-------------|
| FR-020-51 | CPU limit MUST be configurable (default: 1 core equivalent) |
| FR-020-52 | CPU limit MUST use `NanoCPUs` Docker setting |
| FR-020-53 | Memory limit MUST be configurable (default: 512MB) |
| FR-020-54 | Memory limit MUST use hard limit (OOM kill) |
| FR-020-55 | Memory swap MUST be disabled by default |
| FR-020-56 | PID limit MUST be enforced (default: 256) |
| FR-020-57 | PID limit MUST prevent fork bombs |
| FR-020-58 | Disk limit MUST use tmpfs with size limit |
| FR-020-59 | ulimit MUST be set for open files (default: 1024) |
| FR-020-60 | ulimit MUST be set for processes (matches PID limit) |
| FR-020-61 | Resource limits MUST be overridable per command |
| FR-020-62 | Resource limit exceeded MUST be detectable |
| FR-020-63 | OOM kill MUST be reported in result |
| FR-020-64 | CPU throttling MUST be logged |
| FR-020-65 | Resource usage MUST be captured if available |

### Network Policy (FR-020-66 to FR-020-80)

| ID | Requirement |
|----|-------------|
| FR-020-66 | Network MUST be disabled by default (`none` mode) |
| FR-020-67 | Network enable option MUST exist in policy |
| FR-020-68 | Enabled network MUST use `bridge` mode |
| FR-020-69 | Air-gapped mode MUST force network disabled |
| FR-020-70 | Air-gapped mode MUST override enable option |
| FR-020-71 | DNS resolution MUST respect network policy |
| FR-020-72 | DNS MUST be blocked when network is disabled |
| FR-020-73 | Custom DNS servers MUST be configurable |
| FR-020-74 | Network policy violation MUST be logged |
| FR-020-75 | System MUST support custom network for service communication |
| FR-020-76 | Container-to-container network MUST be opt-in |
| FR-020-77 | Published ports MUST be configurable |
| FR-020-78 | Port conflicts MUST be handled gracefully |
| FR-020-79 | Network timeout MUST be enforced |
| FR-020-80 | Network statistics MUST be available in result |

### Image Management (FR-020-81 to FR-020-95)

| ID | Requirement |
|----|-------------|
| FR-020-81 | Default images MUST be defined per language |
| FR-020-82 | Default .NET image MUST be `mcr.microsoft.com/dotnet/sdk:8.0` |
| FR-020-83 | Default Node image MUST be `node:20-alpine` |
| FR-020-84 | Custom image MUST be configurable via contract |
| FR-020-85 | Image MUST be pulled if not present locally |
| FR-020-86 | Image pull MUST have configurable timeout (default: 5 minutes) |
| FR-020-87 | Image pull MUST show progress |
| FR-020-88 | Image pull failure MUST be handled gracefully |
| FR-020-89 | Offline mode MUST use only cached images |
| FR-020-90 | Image verification MUST check image exists after pull |
| FR-020-91 | Image tag MUST be configurable |
| FR-020-92 | System MUST support image digest pinning |
| FR-020-93 | System MUST prune old images on command |
| FR-020-94 | Image list MUST be queryable |
| FR-020-95 | Image size MUST be reported |

### Output Capture (FR-020-96 to FR-020-105)

| ID | Requirement |
|----|-------------|
| FR-020-96 | Stdout MUST be captured from container |
| FR-020-97 | Stderr MUST be captured from container |
| FR-020-98 | Exit code MUST be captured from container |
| FR-020-99 | Output MUST support streaming mode |
| FR-020-100 | Output MUST support buffered mode |
| FR-020-101 | Output size MUST be limited (configurable) |
| FR-020-102 | Output truncation MUST be indicated |
| FR-020-103 | Output encoding MUST be handled (UTF-8) |
| FR-020-104 | Binary output MUST be handled gracefully |
| FR-020-105 | Container logs MUST be retrievable after exit |

---

## Non-Functional Requirements

### Performance (NFR-020-01 to NFR-020-12)

| ID | Requirement | Target | Maximum |
|----|-------------|--------|---------|
| NFR-020-01 | Container creation MUST complete quickly | 1s | 2s |
| NFR-020-02 | Container cleanup MUST complete quickly | 500ms | 1s |
| NFR-020-03 | Overhead vs direct execution MUST be minimal | 300ms | 500ms |
| NFR-020-04 | Image pull MUST show progress | N/A | 5 minutes |
| NFR-020-05 | Output capture latency MUST be low | 10ms | 50ms |
| NFR-020-06 | Mount setup MUST be fast | 100ms | 500ms |
| NFR-020-07 | Container start MUST be fast | 200ms | 500ms |
| NFR-020-08 | Container stop MUST be fast | 500ms | 2s |
| NFR-020-09 | Docker API call latency MUST be acceptable | 50ms | 200ms |
| NFR-020-10 | Concurrent container limit MUST be configurable | 4 | 16 |
| NFR-020-11 | Memory usage per container tracking MUST be minimal | 5MB | 20MB |
| NFR-020-12 | Cleanup of 10 orphaned containers MUST complete | 5s | 15s |

### Reliability (NFR-020-13 to NFR-020-24)

| ID | Requirement |
|----|-------------|
| NFR-020-13 | System MUST handle Docker daemon restart |
| NFR-020-14 | System MUST handle container crash |
| NFR-020-15 | System MUST handle image pull interruption |
| NFR-020-16 | System MUST handle mount failures gracefully |
| NFR-020-17 | System MUST handle resource exhaustion |
| NFR-020-18 | System MUST handle network failures |
| NFR-020-19 | System MUST cleanup on unexpected termination |
| NFR-020-20 | System MUST retry transient Docker API failures |
| NFR-020-21 | System MUST handle concurrent cleanup requests |
| NFR-020-22 | System MUST survive partial container state |
| NFR-020-23 | System MUST handle timeout during any operation |
| NFR-020-24 | System MUST never leave containers running on exit |

### Security (NFR-020-25 to NFR-020-38)

| ID | Requirement |
|----|-------------|
| NFR-020-25 | Containers MUST run as non-root user |
| NFR-020-26 | Containers MUST NOT run in privileged mode |
| NFR-020-27 | Containers MUST drop unnecessary capabilities |
| NFR-020-28 | Containers MUST use seccomp profile |
| NFR-020-29 | Mount paths MUST be validated against escape |
| NFR-020-30 | Network MUST be disabled unless explicitly enabled |
| NFR-020-31 | Air-gapped mode MUST be enforced at container level |
| NFR-020-32 | Container images MUST be verified |
| NFR-020-33 | Sensitive host paths MUST never be mounted |
| NFR-020-34 | Environment variables MUST be sanitized |
| NFR-020-35 | Audit logs MUST capture all container operations |
| NFR-020-36 | Process isolation MUST prevent container escape |
| NFR-020-37 | Resource limits MUST prevent host impact |
| NFR-020-38 | Container names MUST not leak sensitive info |

### Maintainability (NFR-020-39 to NFR-020-48)

| ID | Requirement |
|----|-------------|
| NFR-020-39 | Code MUST follow SOLID principles |
| NFR-020-40 | ISandbox MUST be mockable for testing |
| NFR-020-41 | Docker API calls MUST be isolated for mocking |
| NFR-020-42 | All public APIs MUST have XML documentation |
| NFR-020-43 | Configuration MUST be externalizable |
| NFR-020-44 | Error codes MUST be documented |
| NFR-020-45 | Code coverage MUST exceed 80% |
| NFR-020-46 | Integration tests MUST use real Docker |
| NFR-020-47 | Unit tests MUST mock Docker API |
| NFR-020-48 | Container lifecycle MUST be clearly logged |

### Observability (NFR-020-49 to NFR-020-60)

| ID | Requirement |
|----|-------------|
| NFR-020-49 | All container operations MUST be logged |
| NFR-020-50 | Container creation duration MUST be metric |
| NFR-020-51 | Container execution duration MUST be metric |
| NFR-020-52 | Container cleanup duration MUST be metric |
| NFR-020-53 | Resource limit violations MUST be logged |
| NFR-020-54 | OOM kills MUST be logged |
| NFR-020-55 | Network policy violations MUST be logged |
| NFR-020-56 | Image pull progress MUST be observable |
| NFR-020-57 | Active container count MUST be metric |
| NFR-020-58 | Container exit codes MUST be logged |
| NFR-020-59 | Health check MUST report Docker status |
| NFR-020-60 | Orphaned container count MUST be metric |

---

## User Manual Documentation

### Overview

Docker Sandbox Mode provides isolated command execution for Agentic Coding Bot. When enabled, all commands execute inside Docker containers, providing security isolation, reproducible environments, and resource control.

Sandbox mode is recommended for:
- Executing untrusted or AI-generated code
- Ensuring reproducible build environments
- Protecting host system from runaway processes
- Enterprise security compliance requirements
- Air-gapped environments requiring network isolation

### Prerequisites

**Docker Installation:**
- Docker Desktop (Windows/macOS) or Docker Engine (Linux)
- Docker version 20.10 or later (API 1.41+)
- User must be in `docker` group (Linux) or have Docker Desktop running

**Verify Docker:**
```bash
# Check Docker is available
docker --version
# Docker version 24.0.0, build ...

# Check Docker is running
docker info
# Should show Docker daemon information
```

### Configuration

Configure sandbox behavior in `.agent/config.yml`:

```yaml
# .agent/config.yml
sandbox:
  # Enable Docker sandbox mode
  # When enabled, all commands execute in containers
  enabled: true
  
  # Docker connection settings
  docker:
    # Docker host (default: local socket)
    # Linux: unix:///var/run/docker.sock
    # Windows: npipe:////./pipe/docker_engine
    host: null
    
    # API version (null = auto-detect)
    api_version: null
  
  # Default resource limits
  defaults:
    # CPU limit (1.0 = 1 core)
    cpu_limit: 1.0
    
    # Memory limit in megabytes
    memory_mb: 512
    
    # Memory swap (-1 = same as memory, 0 = disabled)
    memory_swap_mb: 0
    
    # Maximum number of processes/threads
    pids_limit: 256
    
    # Network access (true/false)
    network: false
    
    # User to run as inside container
    # null = image default (usually root, not recommended)
    user: "1000:1000"
  
  # Default images per language
  images:
    dotnet: mcr.microsoft.com/dotnet/sdk:8.0
    node: node:20-alpine
    python: python:3.12-slim
    default: ubuntu:22.04
  
  # Mount configuration
  mounts:
    # Path inside container for repository
    workspace_path: /workspace
    
    # Repository mount mode (rw/ro)
    workspace_mode: rw
    
    # Additional mounts
    additional: []
    # - source: /path/on/host
    #   target: /path/in/container
    #   readonly: true
  
  # Cache volumes for package managers
  cache_volumes:
    enabled: true
    nuget: acode-cache-nuget
    npm: acode-cache-npm
    yarn: acode-cache-yarn
  
  # Container management
  containers:
    # Prefix for container names
    name_prefix: acode
    
    # Labels applied to all containers
    labels:
      acode.managed: "true"
    
    # Auto-cleanup orphaned containers on startup
    cleanup_orphans: true
    
    # Maximum container creation time
    create_timeout_seconds: 30
    
    # Maximum image pull time
    pull_timeout_seconds: 300
  
  # Security settings
  security:
    # Drop all capabilities except these
    capabilities_add: []
    capabilities_drop:
      - ALL
    
    # Read-only root filesystem
    readonly_rootfs: false
    
    # No new privileges flag
    no_new_privileges: true
    
    # Seccomp profile (default/unconfined/path)
    seccomp_profile: default
```

### CLI Commands

#### Check Sandbox Status

```bash
# Check if sandbox is available and configured
acode sandbox status

Output:
Sandbox Status: Available
Docker Version: 24.0.0
Docker API: 1.43
Containers Running: 0
Orphaned Containers: 0
Cache Volumes: 3 (nuget, npm, yarn)
Default Image: mcr.microsoft.com/dotnet/sdk:8.0
```

#### Execute Command in Sandbox

```bash
# Run command in sandbox
acode sandbox exec "dotnet build"

# Run with custom image
acode sandbox exec "npm test" --image node:18-alpine

# Run with network enabled
acode sandbox exec "npm install" --network

# Run with increased memory
acode sandbox exec "npm run build" --memory 1024

# Run in read-only mode
acode sandbox exec "dotnet test" --readonly
```

#### List Running Containers

```bash
# Show containers managed by acode
acode sandbox list

Output:
CONTAINER ID    NAME                    IMAGE                   STATUS      CREATED
abc123def456    acode-sess1-task1       dotnet/sdk:8.0         Running     2m ago
```

#### Cleanup Containers

```bash
# Remove all stopped acode containers
acode sandbox cleanup

# Force remove all acode containers (including running)
acode sandbox cleanup --force

# Prune old images
acode sandbox prune-images

# Remove cache volumes
acode sandbox cleanup --volumes
```

#### Manage Images

```bash
# Pull default images
acode sandbox pull

# Pull specific image
acode sandbox pull node:20-alpine

# List cached images
acode sandbox images

# Remove unused images
acode sandbox prune-images
```

### Container Naming Convention

Containers are named following the pattern:
```
acode-{session_id}-{task_id}
```

For example:
- `acode-abc123-task001` - Session abc123, Task 001
- `acode-xyz789-build-01` - Session xyz789, build step 1

### Resource Limit Examples

**Memory-intensive build:**
```yaml
# For large .NET solutions
sandbox:
  defaults:
    memory_mb: 2048
    cpu_limit: 2.0
```

**CPU-intensive tests:**
```yaml
# For parallel test execution
sandbox:
  defaults:
    cpu_limit: 4.0
    pids_limit: 512
```

**Minimal for simple commands:**
```yaml
# For lightweight operations
sandbox:
  defaults:
    memory_mb: 256
    cpu_limit: 0.5
```

### Troubleshooting

#### Docker Not Found

**Symptoms:**
- "Docker is not installed or not running"
- Sandbox status shows unavailable

**Solutions:**
1. Install Docker Desktop or Docker Engine
2. Start Docker daemon/service
3. Verify with `docker info`
4. On Linux, ensure user is in docker group: `sudo usermod -aG docker $USER`

#### Permission Denied

**Symptoms:**
- "Permission denied while trying to connect to Docker"
- Mount failures

**Solutions:**
1. Check Docker socket permissions
2. Add user to docker group (Linux)
3. Restart Docker Desktop (Windows/macOS)
4. Check SELinux/AppArmor policies

#### Image Pull Fails

**Symptoms:**
- "Failed to pull image"
- Timeout during image download

**Solutions:**
1. Check network connectivity
2. Verify registry is accessible
3. Use `docker pull` directly to diagnose
4. Configure Docker registry mirrors
5. Pre-pull images manually

#### Container Creation Fails

**Symptoms:**
- "Failed to create container"
- Resource allocation errors

**Solutions:**
1. Check Docker has sufficient resources
2. Reduce resource limits in config
3. Clean up unused containers: `docker system prune`
4. Check disk space for images

#### Slow Container Start

**Symptoms:**
- Container creation takes > 2 seconds
- Noticeable delay on each command

**Solutions:**
1. Use smaller base images (alpine variants)
2. Pre-pull images
3. Enable cache volumes
4. Check Docker daemon performance

#### Network Not Available

**Symptoms:**
- "Network is disabled" errors
- `npm install` fails in sandbox

**Solutions:**
1. Enable network: `--network` flag
2. Check air-gapped mode is not forced
3. Configure network in sandbox settings
4. Use cache volumes for offline packages

---

## Acceptance Criteria

### Sandbox Interface (AC-020-01 to AC-020-10)

- [ ] AC-020-01: ISandbox interface MUST be defined with RunAsync method
- [ ] AC-020-02: ISandbox.IsAvailable MUST correctly detect Docker availability
- [ ] AC-020-03: ISandbox.RunAsync MUST execute command in container
- [ ] AC-020-04: ISandbox.RunAsync MUST return structured SandboxResult
- [ ] AC-020-05: ISandbox.CleanupAsync MUST remove managed containers
- [ ] AC-020-06: ISandbox MUST support CancellationToken
- [ ] AC-020-07: ISandbox MUST implement IAsyncDisposable
- [ ] AC-020-08: SandboxResult MUST contain stdout, stderr, exit code
- [ ] AC-020-09: SandboxResult MUST contain container ID
- [ ] AC-020-10: SandboxResult MUST contain execution duration

### Container Lifecycle (AC-020-11 to AC-020-25)

- [ ] AC-020-11: Containers MUST be created before execution
- [ ] AC-020-12: Containers MUST be started for command execution
- [ ] AC-020-13: Containers MUST be stopped after completion
- [ ] AC-020-14: Containers MUST be removed after stop
- [ ] AC-020-15: Container creation MUST complete within timeout
- [ ] AC-020-16: Container names MUST follow naming convention
- [ ] AC-020-17: Containers MUST have management labels
- [ ] AC-020-18: Orphaned containers MUST be detected on startup
- [ ] AC-020-19: Orphaned containers MUST be cleaned up automatically
- [ ] AC-020-20: Concurrent container operations MUST be thread-safe
- [ ] AC-020-21: Container creation failure MUST return structured error
- [ ] AC-020-22: Container timeout MUST kill container
- [ ] AC-020-23: Container exit code MUST be captured correctly
- [ ] AC-020-24: Partial output MUST be captured on crash
- [ ] AC-020-25: Resources MUST be released on failure

### Mount Configuration (AC-020-26 to AC-020-35)

- [ ] AC-020-26: Repository MUST be mounted at configured path
- [ ] AC-020-27: Repository mount MUST be read-write by default
- [ ] AC-020-28: Read-only mount option MUST work
- [ ] AC-020-29: Additional mounts MUST be configurable
- [ ] AC-020-30: Host paths outside repository MUST be rejected
- [ ] AC-020-31: Mount path traversal MUST be prevented
- [ ] AC-020-32: Cache volumes MUST be created and mounted
- [ ] AC-020-33: Cache volumes MUST persist across containers
- [ ] AC-020-34: Mount errors MUST be reported clearly
- [ ] AC-020-35: Mount sources MUST be validated

### Resource Limits (AC-020-36 to AC-020-45)

- [ ] AC-020-36: CPU limit MUST be enforced
- [ ] AC-020-37: Memory limit MUST be enforced
- [ ] AC-020-38: Memory swap MUST be controllable
- [ ] AC-020-39: PID limit MUST be enforced
- [ ] AC-020-40: OOM kill MUST be detectable
- [ ] AC-020-41: Resource limits MUST be configurable
- [ ] AC-020-42: Per-command limit overrides MUST work
- [ ] AC-020-43: Resource violation MUST be logged
- [ ] AC-020-44: Container MUST be killed on resource violation
- [ ] AC-020-45: Resource usage MUST be reported if available

### Network Policy (AC-020-46 to AC-020-55)

- [ ] AC-020-46: Network MUST be disabled by default
- [ ] AC-020-47: Network enable option MUST work
- [ ] AC-020-48: Air-gapped mode MUST force disable network
- [ ] AC-020-49: DNS MUST be blocked when network disabled
- [ ] AC-020-50: Enabled network MUST allow DNS resolution
- [ ] AC-020-51: Network mode MUST be configurable
- [ ] AC-020-52: Published ports MUST work when configured
- [ ] AC-020-53: Network policy MUST be logged
- [ ] AC-020-54: Network timeout MUST be enforced
- [ ] AC-020-55: Container-to-container network MUST be opt-in

### Image Management (AC-020-56 to AC-020-65)

- [ ] AC-020-56: Default images MUST be configured per language
- [ ] AC-020-57: Custom images MUST be usable
- [ ] AC-020-58: Missing images MUST be pulled automatically
- [ ] AC-020-59: Image pull MUST show progress
- [ ] AC-020-60: Image pull timeout MUST be enforced
- [ ] AC-020-61: Image pull failure MUST be handled gracefully
- [ ] AC-020-62: Offline mode MUST use only cached images
- [ ] AC-020-63: Image list MUST be queryable
- [ ] AC-020-64: Image prune MUST remove unused images
- [ ] AC-020-65: Image verification MUST confirm existence

### Security (AC-020-66 to AC-020-75)

- [ ] AC-020-66: Containers MUST run as non-root by default
- [ ] AC-020-67: Containers MUST NOT run privileged
- [ ] AC-020-68: Capabilities MUST be dropped
- [ ] AC-020-69: Seccomp profile MUST be applied
- [ ] AC-020-70: No new privileges MUST be set
- [ ] AC-020-71: Sensitive paths MUST NOT be mountable
- [ ] AC-020-72: Container escape attempts MUST be blocked
- [ ] AC-020-73: All operations MUST be audit logged
- [ ] AC-020-74: Container labels MUST NOT leak secrets
- [ ] AC-020-75: Environment MUST be sanitized

### CLI Integration (AC-020-76 to AC-020-85)

- [ ] AC-020-76: `acode sandbox status` MUST show availability
- [ ] AC-020-77: `acode sandbox exec` MUST execute in container
- [ ] AC-020-78: `acode sandbox list` MUST show managed containers
- [ ] AC-020-79: `acode sandbox cleanup` MUST remove containers
- [ ] AC-020-80: `acode sandbox pull` MUST pull images
- [ ] AC-020-81: `acode sandbox images` MUST list images
- [ ] AC-020-82: CLI flags MUST override defaults
- [ ] AC-020-83: CLI MUST show progress for long operations
- [ ] AC-020-84: CLI MUST handle errors gracefully
- [ ] AC-020-85: CLI MUST support --json output

---

## Testing Requirements

### Unit Tests

#### DockerSandboxTests
- DockerSandbox_Constructor_ValidatesDockerClient
- DockerSandbox_Constructor_AcceptsNullLoggerGracefully
- DockerSandbox_IsAvailable_ReturnsTrueWhenDockerResponds
- DockerSandbox_IsAvailable_ReturnsFalseWhenDockerUnreachable
- DockerSandbox_IsAvailable_CachesResultForConfiguredDuration
- DockerSandbox_RunAsync_ThrowsWhenNotAvailable
- DockerSandbox_RunAsync_CreatesContainerWithCorrectImage
- DockerSandbox_RunAsync_AppliesResourceLimits
- DockerSandbox_RunAsync_DisablesNetworkByDefault
- DockerSandbox_RunAsync_MountsWorkspaceCorrectly
- DockerSandbox_RunAsync_SetsWorkingDirectory
- DockerSandbox_RunAsync_ReturnsStdoutContent
- DockerSandbox_RunAsync_ReturnsStderrContent
- DockerSandbox_RunAsync_ReturnsExitCode
- DockerSandbox_RunAsync_ReturnsExecutionDuration
- DockerSandbox_RunAsync_RemovesContainerAfterExecution
- DockerSandbox_RunAsync_HandlesTimeoutGracefully
- DockerSandbox_RunAsync_KillsContainerOnTimeout
- DockerSandbox_RunAsync_CleansUpOnCancellation
- DockerSandbox_RunAsync_PropagatesDockerExceptions
- DockerSandbox_CleanupAsync_RemovesAllManagedContainers
- DockerSandbox_CleanupAsync_HandlesAlreadyRemovedContainers

#### ContainerLifecycleTests
- ContainerLifecycle_Create_GeneratesUniqueContainerId
- ContainerLifecycle_Create_AppliesLabelsForTracking
- ContainerLifecycle_Create_SetsRestartPolicyToNo
- ContainerLifecycle_Create_SetsAutoRemoveToFalse
- ContainerLifecycle_Create_ConfiguresEntrypoint
- ContainerLifecycle_Create_SetsEnvironmentVariables
- ContainerLifecycle_Create_HandlesCreateContainerException
- ContainerLifecycle_Start_StartsCreatedContainer
- ContainerLifecycle_Start_WaitsForContainerRunning
- ContainerLifecycle_Start_ThrowsOnStartFailure
- ContainerLifecycle_Wait_ReturnsExitCode
- ContainerLifecycle_Wait_RespectsTimeout
- ContainerLifecycle_Wait_ReturnsCancelledOnCancellation
- ContainerLifecycle_Logs_ReturnsStdout
- ContainerLifecycle_Logs_ReturnsStderr
- ContainerLifecycle_Logs_HandlesMissingLogs
- ContainerLifecycle_Remove_RemovesContainer
- ContainerLifecycle_Remove_ForcesRemovalIfRunning
- ContainerLifecycle_Remove_RemovesAssociatedVolumes
- ContainerLifecycle_Remove_IgnoresNotFoundError

#### MountManagerTests
- MountManager_ValidatePath_AllowsWorkspaceSubpaths
- MountManager_ValidatePath_RejectsParentTraversal
- MountManager_ValidatePath_RejectsSymlinksOutsideWorkspace
- MountManager_ValidatePath_RejectsAbsolutePathsOutsideWorkspace
- MountManager_ValidatePath_RejectsSensitivePaths
- MountManager_CreateBind_CreatesReadOnlyBindByDefault
- MountManager_CreateBind_SupportsReadWriteOption
- MountManager_CreateBind_NormalizesPathsCorrectly
- MountManager_CreateBind_HandlesSpacesInPaths
- MountManager_CreateBind_ConvertsWindowsPathsToLinux
- MountManager_ResolveOutputPath_CreatesOutputDirectory
- MountManager_ResolveOutputPath_CalculatesContainerPath
- MountManager_SensitivePaths_IncludesHomeDirectory
- MountManager_SensitivePaths_IncludesCredentialStores
- MountManager_SensitivePaths_IncludesDockerSocket

#### ResourceLimiterTests
- ResourceLimiter_Configure_SetsCpuLimit
- ResourceLimiter_Configure_SetsCpuPeriod
- ResourceLimiter_Configure_SetsCpuQuota
- ResourceLimiter_Configure_SetsMemoryLimit
- ResourceLimiter_Configure_SetsMemorySwap
- ResourceLimiter_Configure_SetsMemorySwappiness
- ResourceLimiter_Configure_SetsPidsLimit
- ResourceLimiter_Configure_DisablesOOMKillByDefault
- ResourceLimiter_Configure_SetsUlimits
- ResourceLimiter_MergeOverrides_AppliesPerCommandLimits
- ResourceLimiter_MergeOverrides_PreservesUnspecifiedDefaults
- ResourceLimiter_Validate_RejectsNegativeLimits
- ResourceLimiter_Validate_RejectsZeroCpu
- ResourceLimiter_Validate_RejectsUnreasonableMemory
- ResourceLimiter_GetResourceStats_ReturnsContainerStats
- ResourceLimiter_GetResourceStats_CalculatesCpuPercentage
- ResourceLimiter_GetResourceStats_CalculatesMemoryUsage

#### NetworkPolicyTests
- NetworkPolicy_DisabledByDefault_SetsNetworkModeNone
- NetworkPolicy_Enabled_SetsNetworkModeBridge
- NetworkPolicy_AirGapped_ForcesDisabledNetwork
- NetworkPolicy_AirGapped_OverridesEnabledSetting
- NetworkPolicy_Configure_SetsPortBindings
- NetworkPolicy_Configure_SetsDnsServers
- NetworkPolicy_Configure_SetsExtraHosts
- NetworkPolicy_Configure_SetsNetworkAliases
- NetworkPolicy_Validate_RejectsPrivilegedPorts
- NetworkPolicy_Validate_RejectsConflictingPorts

#### ImageManagerTests
- ImageManager_GetImage_ReturnsConfiguredImageForLanguage
- ImageManager_GetImage_ReturnsFallbackForUnknownLanguage
- ImageManager_GetImage_SupportsCustomImageOverride
- ImageManager_Exists_ReturnsTrueForExistingImage
- ImageManager_Exists_ReturnsFalseForMissingImage
- ImageManager_Pull_PullsImageFromRegistry
- ImageManager_Pull_ReportsProgress
- ImageManager_Pull_RespectsTimeout
- ImageManager_Pull_ThrowsInOfflineMode
- ImageManager_Pull_HandlesAuthenticationErrors
- ImageManager_List_ReturnsAllManagedImages
- ImageManager_Prune_RemovesUnusedImages
- ImageManager_Prune_PreservesRecentlyUsedImages

### Integration Tests

#### DockerSandboxIntegrationTests
- DockerSandbox_RunsSimpleCommand_ReturnsCorrectOutput
- DockerSandbox_RunsDotNetCommand_CompilesAndExecutes
- DockerSandbox_RunsNodeCommand_ExecutesJavaScript
- DockerSandbox_RunsPythonCommand_ExecutesPythonScript
- DockerSandbox_MountsWorkspace_SeesMountedFiles
- DockerSandbox_WritesOutput_OutputVisibleOnHost
- DockerSandbox_RespectsTimeout_KillsLongRunningProcess
- DockerSandbox_RespectsMemoryLimit_OOMKillsExcessiveProcess
- DockerSandbox_NetworkDisabled_CannotReachInternet
- DockerSandbox_NetworkEnabled_CanReachInternet
- DockerSandbox_CleanupRemovesContainers_NoOrphansRemain
- DockerSandbox_MultipleParallel_AllExecuteSuccessfully
- DockerSandbox_RecoverFromFailure_SubsequentCallsSucceed
- DockerSandbox_NonZeroExit_ReturnsCorrectExitCode
- DockerSandbox_LargeOutput_StreamsCorrectly
- DockerSandbox_EnvironmentVariables_PassedToContainer

#### ImageManagementIntegrationTests
- ImageManagement_PullImage_DownloadsFromDockerHub
- ImageManagement_ListImages_ReturnsDownloadedImages
- ImageManagement_PruneImages_RemovesDanglingImages
- ImageManagement_CustomImage_UsedForExecution

#### SecurityIntegrationTests
- Security_ContainerRunsAsNonRoot_UidIsNotZero
- Security_PrivilegedDisabled_CannotAccessDevices
- Security_CapabilitiesDropped_CannotChangePerms
- Security_SensitivePaths_NotMountable
- Security_DockerSocket_NotMountable
- Security_Seccomp_BlocksDangerousSyscalls

### Benchmark Tests

| Benchmark | Target | Description |
|-----------|--------|-------------|
| ContainerCreation_Latency | <500ms | Time to create and start container |
| ContainerExecution_HelloWorld | <1s | Total time for minimal command |
| ContainerCleanup_Latency | <200ms | Time to remove container |
| ImagePull_Cached | <100ms | Time when image already exists |
| MountSetup_Latency | <50ms | Time to configure mounts |
| ParallelExecution_10Containers | <10s | 10 concurrent hello-world commands |

### Coverage Requirements

| Component | Minimum Coverage |
|-----------|-----------------|
| DockerSandbox | 90% |
| ContainerLifecycle | 95% |
| MountManager | 95% |
| ResourceLimiter | 90% |
| NetworkPolicy | 90% |
| ImageManager | 85% |

---

## User Verification Steps

### Scenario 1: Verify Sandbox Availability Check

**Objective:** Confirm the sandbox correctly detects Docker availability

**Steps:**
1. Ensure Docker Desktop/Engine is running
2. Run `acode sandbox status`
3. Observe output showing Docker is available
4. Stop Docker Desktop/Engine
5. Run `acode sandbox status` again
6. Observe output showing Docker is unavailable

**Expected Results:**
- When Docker running: "Sandbox Available: Docker version X.Y.Z"
- When Docker stopped: "Sandbox Unavailable: Docker daemon not responding"
- Status command completes in under 2 seconds
- No error stack traces shown to user

### Scenario 2: Verify Basic Command Execution in Sandbox

**Objective:** Confirm commands execute correctly inside container

**Steps:**
1. Navigate to a test project directory
2. Enable sandbox mode: Set `sandbox.enabled: true` in agent-config.yml
3. Run a simple command: `acode sandbox exec -- echo "Hello from container"`
4. Observe the output

**Expected Results:**
- Output shows "Hello from container"
- Log indicates container was created and removed
- Exit code is 0
- Execution completes within timeout
- No container remains after execution (`docker ps -a` shows no acode containers)

### Scenario 3: Verify Workspace Mounting

**Objective:** Confirm workspace files are accessible inside container

**Steps:**
1. Create a test file: `echo "test content" > testfile.txt`
2. Run: `acode sandbox exec -- cat /workspace/testfile.txt`
3. Run: `acode sandbox exec -- ls -la /workspace`
4. Verify directory listing matches host workspace

**Expected Results:**
- Cat command outputs "test content"
- Directory listing shows all workspace files
- File permissions are appropriate
- Only workspace directory is mounted (not parent directories)

### Scenario 4: Verify Network Isolation

**Objective:** Confirm network is disabled by default

**Steps:**
1. Ensure sandbox.network.enabled is false (default)
2. Run: `acode sandbox exec -- ping -c 1 google.com`
3. Observe the error
4. Set sandbox.network.enabled to true in config
5. Run ping command again

**Expected Results:**
- With network disabled: "ping: google.com: Temporary failure in name resolution" or similar
- With network enabled: Ping succeeds with response from google.com
- Network state is logged in agent logs

### Scenario 5: Verify Resource Limits

**Objective:** Confirm resource limits prevent runaway processes

**Steps:**
1. Configure memory limit: `sandbox.limits.memory: 256m`
2. Create a script that allocates excessive memory:
   ```python
   # memory_hog.py
   data = []
   while True:
       data.append(' ' * 1024 * 1024)  # 1MB per iteration
   ```
3. Run: `acode sandbox exec -- python memory_hog.py`
4. Observe the result

**Expected Results:**
- Process is killed when memory limit exceeded
- Exit code indicates OOM kill (137)
- Log message indicates resource limit violation
- Container is cleaned up after OOM

### Scenario 6: Verify Image Management

**Objective:** Confirm image pulling and listing works

**Steps:**
1. Run: `acode sandbox images`
2. Observe current images
3. Run: `acode sandbox pull node:20-slim`
4. Observe pull progress
5. Run: `acode sandbox images` again
6. Verify new image appears

**Expected Results:**
- Image list shows available images with tags and sizes
- Pull shows download progress with percentage
- After pull, new image appears in list
- Images are tagged appropriately for acode management

### Scenario 7: Verify .NET Execution in Sandbox

**Objective:** Confirm .NET commands work in sandbox

**Steps:**
1. Create a new .NET project: `dotnet new console -n SandboxTest`
2. Navigate to project: `cd SandboxTest`
3. Run: `acode sandbox exec -- dotnet build`
4. Run: `acode sandbox exec -- dotnet run`
5. Verify build output in /workspace/bin

**Expected Results:**
- Build succeeds with output shown
- Run produces "Hello, World!" output
- Build artifacts are visible on host (bin/Debug folder)
- NuGet restore works (requires network enabled)

### Scenario 8: Verify Container Cleanup

**Objective:** Confirm no orphaned containers remain

**Steps:**
1. Run several sandbox commands in sequence
2. Run: `docker ps -a --filter label=acode.managed=true`
3. Verify no containers shown
4. Manually interrupt a running sandbox command (Ctrl+C)
5. Run docker ps command again
6. Run: `acode sandbox cleanup`
7. Verify cleanup output

**Expected Results:**
- Normal execution leaves no containers
- Interrupted execution might leave container temporarily
- Cleanup command removes any orphaned containers
- Cleanup reports number of containers removed
- Final docker ps shows no managed containers

### Scenario 9: Verify Security Configuration

**Objective:** Confirm security hardening is applied

**Steps:**
1. Run: `acode sandbox exec -- id`
2. Verify non-root user
3. Run: `acode sandbox exec -- cat /proc/1/status | grep Cap`
4. Verify capabilities are minimal
5. Attempt to mount sensitive path via configuration
6. Verify mount is rejected

**Expected Results:**
- id shows non-root UID (e.g., uid=1000)
- Capabilities show minimal set (not full 0000003fffffffff)
- Sensitive path mount fails with security error
- Log shows security policy enforcement

### Scenario 10: Verify CLI Error Handling

**Objective:** Confirm errors are handled gracefully

**Steps:**
1. Run: `acode sandbox exec -- /nonexistent/command`
2. Observe error message
3. Run: `acode sandbox exec --image nonexistent:tag -- echo test`
4. Observe error about missing image
5. Run: `acode sandbox status --json`
6. Verify JSON output format

**Expected Results:**
- Command not found: Clear error, suggests checking command
- Missing image: Offers to pull or suggests correct image name
- JSON output: Valid JSON with status, version, capabilities
- All errors have error codes (ACODE-SBX-XXX)
- No stack traces shown to user

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/Sandbox/
├── DockerSandbox.cs              # Main sandbox implementation
├── DockerSandboxFactory.cs       # Factory for creating sandbox instances
├── ContainerLifecycle.cs         # Container create/start/stop/remove
├── MountManager.cs               # Path validation and mount configuration
├── ResourceLimiter.cs            # CPU, memory, PID limits
├── NetworkPolicy.cs              # Network mode and port configuration
├── ImageManager.cs               # Image pull, list, prune operations
├── SandboxConfiguration.cs       # Configuration model
├── SandboxResult.cs              # Execution result model
├── SandboxPolicy.cs              # Security policy model
├── SandboxException.cs           # Domain-specific exceptions
└── SandboxErrorCodes.cs          # Error code constants

src/AgenticCoder.Domain/Abstractions/
└── ISandbox.cs                   # Sandbox abstraction interface

src/AgenticCoder.CLI/Commands/
└── SandboxCommand.cs             # CLI subcommands for sandbox

tests/AgenticCoder.Infrastructure.Tests/Sandbox/
├── DockerSandboxTests.cs
├── ContainerLifecycleTests.cs
├── MountManagerTests.cs
├── ResourceLimiterTests.cs
├── NetworkPolicyTests.cs
├── ImageManagerTests.cs
└── Integration/
    ├── DockerSandboxIntegrationTests.cs
    ├── ImageManagementIntegrationTests.cs
    └── SecurityIntegrationTests.cs
```

### ISandbox Interface

```csharp
namespace AgenticCoder.Domain.Abstractions;

/// <summary>
/// Abstraction for sandboxed command execution.
/// Implementations may use Docker, VMs, or other isolation mechanisms.
/// </summary>
public interface ISandbox
{
    /// <summary>
    /// Gets whether the sandbox is available for use.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets detailed information about sandbox availability.
    /// </summary>
    Task<SandboxStatus> GetStatusAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Executes a command within the sandbox.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="policy">Security and resource policy.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing output, exit code, and metrics.</returns>
    Task<SandboxResult> RunAsync(
        SandboxCommand command,
        SandboxPolicy policy,
        CancellationToken ct = default);
    
    /// <summary>
    /// Cleans up any orphaned containers or resources.
    /// </summary>
    Task<CleanupResult> CleanupAsync(CancellationToken ct = default);
}
```

### SandboxConfiguration Model

```csharp
namespace AgenticCoder.Infrastructure.Sandbox;

/// <summary>
/// Configuration for Docker sandbox mode.
/// Maps to sandbox section in agent-config.yml.
/// </summary>
public sealed record SandboxConfiguration
{
    public bool Enabled { get; init; } = false;
    
    public string DefaultImage { get; init; } = "mcr.microsoft.com/dotnet/sdk:8.0";
    
    public ResourceLimitsConfig Limits { get; init; } = new();
    
    public NetworkConfig Network { get; init; } = new();
    
    public SecurityConfig Security { get; init; } = new();
    
    public Dictionary<string, string> LanguageImages { get; init; } = new()
    {
        ["csharp"] = "mcr.microsoft.com/dotnet/sdk:8.0",
        ["fsharp"] = "mcr.microsoft.com/dotnet/sdk:8.0",
        ["javascript"] = "node:20-slim",
        ["typescript"] = "node:20-slim",
        ["python"] = "python:3.12-slim"
    };
}

public sealed record ResourceLimitsConfig
{
    public string Memory { get; init; } = "512m";
    public float CpuLimit { get; init; } = 1.0f;
    public int PidsLimit { get; init; } = 100;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
}

public sealed record NetworkConfig
{
    public bool Enabled { get; init; } = false;
    public List<string> DnsServers { get; init; } = new();
    public List<PortBinding> Ports { get; init; } = new();
}

public sealed record SecurityConfig
{
    public bool RunAsNonRoot { get; init; } = true;
    public bool ReadOnlyRootFilesystem { get; init; } = false;
    public bool NoNewPrivileges { get; init; } = true;
    public List<string> DropCapabilities { get; init; } = new() { "ALL" };
}
```

### ContainerLifecycle Implementation Pattern

```csharp
namespace AgenticCoder.Infrastructure.Sandbox;

public sealed class ContainerLifecycle : IDisposable
{
    private readonly DockerClient _client;
    private readonly ILogger<ContainerLifecycle> _logger;
    private readonly List<string> _managedContainerIds = new();
    
    public async Task<string> CreateAsync(
        CreateContainerRequest request,
        CancellationToken ct)
    {
        var containerName = $"acode-{Guid.NewGuid():N}";
        
        var createParams = new CreateContainerParameters
        {
            Image = request.Image,
            Name = containerName,
            Cmd = request.Command,
            WorkingDir = request.WorkingDirectory,
            Env = request.EnvironmentVariables.Select(kv => $"{kv.Key}={kv.Value}").ToList(),
            Labels = new Dictionary<string, string>
            {
                ["acode.managed"] = "true",
                ["acode.created"] = DateTimeOffset.UtcNow.ToString("O"),
                ["acode.purpose"] = "sandbox-execution"
            },
            HostConfig = new HostConfig
            {
                AutoRemove = false,  // We manage removal
                RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.No },
                Binds = request.Mounts.Select(m => $"{m.Source}:{m.Target}:{(m.ReadOnly ? "ro" : "rw")}").ToList(),
                Memory = request.ResourceLimits.MemoryBytes,
                CPUQuota = (long)(request.ResourceLimits.CpuLimit * 100000),
                CPUPeriod = 100000,
                PidsLimit = request.ResourceLimits.PidsLimit,
                NetworkMode = request.NetworkEnabled ? "bridge" : "none",
                SecurityOpt = new List<string> { "no-new-privileges" },
                CapDrop = new List<string> { "ALL" }
            },
            User = request.RunAsNonRoot ? "1000:1000" : null
        };
        
        var response = await _client.Containers.CreateContainerAsync(createParams, ct);
        _managedContainerIds.Add(response.ID);
        
        _logger.LogInformation(
            "Created container {ContainerId} from image {Image}",
            response.ID[..12], request.Image);
        
        return response.ID;
    }
    
    public async Task<ContainerExecutionResult> RunToCompletionAsync(
        string containerId,
        TimeSpan timeout,
        CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);
        
        try
        {
            await _client.Containers.StartContainerAsync(containerId, null, timeoutCts.Token);
            
            var waitResponse = await _client.Containers.WaitContainerAsync(containerId, timeoutCts.Token);
            
            var logs = await GetLogsAsync(containerId, timeoutCts.Token);
            
            return new ContainerExecutionResult
            {
                ExitCode = (int)waitResponse.StatusCode,
                Stdout = logs.Stdout,
                Stderr = logs.Stderr,
                TimedOut = false
            };
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            _logger.LogWarning("Container {ContainerId} timed out, killing", containerId[..12]);
            await _client.Containers.KillContainerAsync(containerId, new ContainerKillParameters(), CancellationToken.None);
            
            return new ContainerExecutionResult
            {
                ExitCode = -1,
                TimedOut = true,
                ErrorMessage = $"Container execution timed out after {timeout.TotalSeconds}s"
            };
        }
    }
}
```

### Error Codes

| Code | Meaning | User Message |
|------|---------|--------------|
| ACODE-SBX-001 | Container creation failed | "Failed to create sandbox container. Check Docker is running." |
| ACODE-SBX-002 | Image pull failed | "Failed to pull image {0}. Check network and image name." |
| ACODE-SBX-003 | Mount validation error | "Cannot mount path {0}. Path is outside workspace or restricted." |
| ACODE-SBX-004 | Resource limit exceeded | "Container exceeded resource limits and was terminated." |
| ACODE-SBX-005 | Network policy violation | "Network access denied by sandbox policy." |
| ACODE-SBX-006 | Container start failed | "Container failed to start. Check image and command." |
| ACODE-SBX-007 | Container timeout | "Container execution timed out after {0}s." |
| ACODE-SBX-008 | Docker unavailable | "Docker is not available. Install Docker or disable sandbox mode." |
| ACODE-SBX-009 | Image not found | "Image {0} not found locally. Use --pull to download." |
| ACODE-SBX-010 | Security policy violation | "Operation blocked by security policy: {0}" |

### CLI Implementation Pattern

```csharp
namespace AgenticCoder.CLI.Commands;

[Command("sandbox", Description = "Manage Docker sandbox for isolated execution")]
public sealed class SandboxCommand
{
    [Command("status", Description = "Check sandbox availability")]
    public async Task<int> StatusAsync(
        [Option("json", Description = "Output as JSON")] bool json,
        ISandbox sandbox)
    {
        var status = await sandbox.GetStatusAsync();
        
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(status, JsonOptions.Pretty));
        }
        else
        {
            Console.WriteLine($"Sandbox Available: {(status.Available ? "Yes" : "No")}");
            if (status.Available)
            {
                Console.WriteLine($"Docker Version: {status.DockerVersion}");
                Console.WriteLine($"Default Image: {status.DefaultImage}");
            }
            else
            {
                Console.WriteLine($"Reason: {status.UnavailableReason}");
            }
        }
        
        return status.Available ? 0 : 1;
    }
    
    [Command("exec", Description = "Execute command in sandbox")]
    public async Task<int> ExecAsync(
        [Argument] string[] command,
        [Option("image", Description = "Override container image")] string? image,
        [Option("network", Description = "Enable network access")] bool network,
        [Option("timeout", Description = "Execution timeout in seconds")] int timeout = 300,
        ISandbox sandbox)
    {
        var policy = new SandboxPolicy
        {
            Image = image,
            NetworkEnabled = network,
            Timeout = TimeSpan.FromSeconds(timeout)
        };
        
        var result = await sandbox.RunAsync(
            new SandboxCommand { Args = command },
            policy);
        
        Console.Write(result.Stdout);
        Console.Error.Write(result.Stderr);
        
        return result.ExitCode;
    }
    
    [Command("cleanup", Description = "Remove orphaned containers")]
    public async Task<int> CleanupAsync(ISandbox sandbox)
    {
        var result = await sandbox.CleanupAsync();
        Console.WriteLine($"Removed {result.ContainersRemoved} container(s)");
        return 0;
    }
}
```

### Implementation Checklist

| Step | Task | Verification |
|------|------|--------------|
| 1 | Create ISandbox interface in Domain | Interface compiles, no dependencies on Infrastructure |
| 2 | Implement SandboxConfiguration | Configuration loads from agent-config.yml |
| 3 | Implement MountManager | Unit tests pass for path validation |
| 4 | Implement ResourceLimiter | Unit tests pass for limit configuration |
| 5 | Implement NetworkPolicy | Unit tests pass for network modes |
| 6 | Implement ContainerLifecycle | Integration test creates/runs/removes container |
| 7 | Implement ImageManager | Can list and pull images |
| 8 | Implement DockerSandbox | Full integration test passes |
| 9 | Add CLI commands | All sandbox subcommands functional |
| 10 | Add to DI container | ISandbox resolves correctly |
| 11 | Write all unit tests | 90% coverage achieved |
| 12 | Write integration tests | All scenarios pass with real Docker |
| 13 | Document configuration | User manual complete in docs |
| 14 | Update CHANGELOG | Changes documented |

### Rollout Plan

| Phase | Action | Success Criteria |
|-------|--------|------------------|
| 1 | Implement core sandbox | Container create/run/remove works |
| 2 | Add resource limits | Memory/CPU limits enforced |
| 3 | Add security hardening | Non-root, capabilities dropped |
| 4 | Add network policy | Network isolation works |
| 5 | Add CLI commands | All commands functional |
| 6 | Integration testing | All scenarios pass |
| 7 | Documentation | User manual complete |
| 8 | Release | Feature flag enabled by default |

### Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Docker.DotNet | 3.125.* | Docker API client |
| Docker.DotNet.X509 | 3.125.* | Docker TLS authentication (optional) |

---

**End of Task 020 Specification**