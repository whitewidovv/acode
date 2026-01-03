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

### Configuration

```yaml
# .agent/config.yml
sandbox:
  enabled: true
  
  defaults:
    cpu_limit: 1
    memory_mb: 512
    network: false
    
  images:
    dotnet: mcr.microsoft.com/dotnet/sdk:8.0
    node: node:20-alpine
```

### CLI Commands

```bash
# Run command in sandbox
acode sandbox exec "dotnet build"

# Show running sandboxes
acode sandbox list

# Cleanup orphaned sandboxes
acode sandbox cleanup
```

---

## Acceptance Criteria

- [ ] AC-001: Containers MUST be created for execution
- [ ] AC-002: Containers MUST be cleaned up after use
- [ ] AC-003: Repository MUST be mounted correctly
- [ ] AC-004: Resource limits MUST be enforced
- [ ] AC-005: Network MUST be disabled by default
- [ ] AC-006: Output MUST be captured from container
- [ ] AC-007: Exit codes MUST match container exit

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/Sandbox/
├── DockerSandbox.cs
├── ContainerLifecycle.cs
├── MountManager.cs
├── ResourceLimiter.cs
└── NetworkPolicy.cs
```

### ISandbox Interface

```csharp
public interface ISandbox
{
    Task<SandboxResult> RunAsync(
        Command command,
        SandboxPolicy policy,
        CancellationToken ct = default);
    
    Task CleanupAsync(CancellationToken ct = default);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-SBX-001 | Container creation failed |
| ACODE-SBX-002 | Image pull failed |
| ACODE-SBX-003 | Mount error |
| ACODE-SBX-004 | Resource limit exceeded |

---

**End of Task 020 Specification**