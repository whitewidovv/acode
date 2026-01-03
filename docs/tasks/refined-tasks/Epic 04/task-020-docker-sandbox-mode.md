# Task 020: Docker Sandbox Mode

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 018 (Command Runner), Task 001 (Operating Modes)  

---

## Description

Task 020 implements Docker sandbox mode for isolated command execution. When operating in Docker mode (per Task 001), all commands MUST execute inside containers. This provides security isolation.

Sandbox execution is critical for safety. Untrusted code MUST NOT access the host directly. Containers provide process, filesystem, and network isolation. The agent MUST enforce this boundary.

Container lifecycle MUST be managed. Containers MUST be created before execution. Containers MUST be cleaned up after execution. Orphaned containers MUST NOT remain.

File system mounting MUST be controlled. The repository MUST be mounted read-write. Other paths MUST NOT be accessible. Mount points MUST be configurable.

Resource limits MUST be enforced. CPU limits MUST prevent runaway processes. Memory limits MUST prevent OOM conditions. Disk limits MUST prevent fill attacks.

Network access MUST be policy-controlled. By default, network MUST be disabled. When enabled, egress rules MUST apply. Air-gapped mode MUST have no network.

Image selection MUST be configurable. Default images MUST be provided per language. Custom images MUST be supported via contract.

Task 020.a defines per-task container strategy. Task 020.b implements cache volumes. Task 020.c handles policy enforcement.

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

### Sandbox Interface

- FR-001: Define ISandbox interface with RunAsync method
- FR-002: Accept Command and SandboxPolicy parameters
- FR-003: Return SandboxResult with output and exit code
- FR-004: Support CancellationToken for abort

### Container Lifecycle

- FR-005: Create container before command execution
- FR-006: Start container for execution
- FR-007: Wait for command completion or timeout
- FR-008: Stop container after completion
- FR-009: Remove container after stop
- FR-010: Handle orphaned containers on startup

### Mounts

- FR-011: Mount repository at configurable path (default: /workspace)
- FR-012: Repository mount MUST be read-write by default
- FR-013: Read-only mount option MUST be supported
- FR-014: Additional mounts MUST be configurable
- FR-015: Host paths outside repo MUST be rejected by default

### Resource Limits

- FR-016: CPU limit MUST be configurable (default: 1 core)
- FR-017: Memory limit MUST be configurable (default: 512MB)
- FR-018: Disk limit via tmpfs MUST be supported
- FR-019: PID limit MUST be enforced (default: 100)

### Network Policy

- FR-020: Network MUST be disabled by default
- FR-021: Network enable option MUST exist
- FR-022: Air-gapped mode MUST block all network
- FR-023: DNS resolution MUST respect policy

### Image Management

- FR-024: Default images MUST be defined per language
- FR-025: Custom image MUST be configurable via contract
- FR-026: Image pull MUST occur if not present
- FR-027: Image pull timeout MUST be enforced

### Output Capture

- FR-028: Stdout MUST be captured from container
- FR-029: Stderr MUST be captured from container
- FR-030: Exit code MUST be captured

---

## Non-Functional Requirements

- NFR-001: Container creation MUST complete < 2 seconds
- NFR-002: Container cleanup MUST complete < 1 second
- NFR-003: Overhead vs direct execution MUST be < 500ms
- NFR-004: Containers MUST run as non-root user

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