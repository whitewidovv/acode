# Task 020.c: Policy Enforcement Inside Sandbox

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 020 (Docker Sandbox), Task 001 (Operating Modes)  

---

## Description

Task 020.c implements policy enforcement inside sandboxes. Containers MUST enforce security policies. Policies MUST align with Task 001 operating modes.

Network policy MUST match the operating mode. Local-only mode MUST block external network. Docker mode MAY allow controlled access. Air-gapped mode MUST block all network.

Filesystem policy MUST restrict access. Only the mounted repository MUST be writable. System directories MUST be read-only. Sensitive host paths MUST NOT be accessible.

Process policy MUST limit capabilities. Root access MUST be disabled. Privileged mode MUST be disabled. Capability drops MUST be applied.

Resource policies MUST be enforced continuously. Memory limits MUST trigger OOM if exceeded. CPU limits MUST throttle, not kill.

Policy violations MUST be logged. Attempts to exceed limits MUST be recorded. Security audit trail MUST be maintained.

---

## Functional Requirements

### Network Policy

- FR-001: Default network mode MUST be `none`
- FR-002: Air-gapped mode MUST use `none` network
- FR-003: Burst mode MAY enable `bridge` network
- FR-004: Network enable MUST be explicit opt-in
- FR-005: DNS MUST be blocked when network is disabled

### Filesystem Policy

- FR-006: Containers MUST run with read-only root filesystem
- FR-007: /workspace MUST be the only writable mount
- FR-008: /tmp MUST be tmpfs (ephemeral)
- FR-009: No bind mounts outside repository allowed
- FR-010: Symlinks MUST NOT escape mount boundary

### Process Policy

- FR-011: Containers MUST NOT run as root (UID 0)
- FR-012: Containers MUST drop all capabilities
- FR-013: Containers MUST have `--security-opt no-new-privileges`
- FR-014: PID namespace MUST be isolated
- FR-015: Seccomp profile MUST be applied

### Resource Enforcement

- FR-016: OOM killer MUST be enabled
- FR-017: CPU throttling MUST be used (not hard limit)
- FR-018: Pids limit MUST prevent fork bombs
- FR-019: Ulimits MUST be configured

### Audit

- FR-020: Policy violations MUST be logged
- FR-021: Resource limit hits MUST be logged
- FR-022: Network block attempts MUST be logged

---

## Acceptance Criteria

- [ ] AC-001: Network MUST be blocked by default
- [ ] AC-002: Root access MUST be denied
- [ ] AC-003: Only /workspace MUST be writable
- [ ] AC-004: Capabilities MUST be dropped
- [ ] AC-005: Policy violations MUST be logged
- [ ] AC-006: Air-gapped mode MUST block all network

---

## User Manual Documentation

### Security Profile

```yaml
# Applied automatically based on operating mode
security:
  air_gapped:
    network: none
    capabilities: []
    read_only_root: true
    
  docker:
    network: none  # Can be overridden
    capabilities: []
    read_only_root: true
```

---

## Implementation Prompt

### Container Security Options

```csharp
public CreateContainerParameters ApplySecurityPolicy(
    CreateContainerParameters parameters,
    SandboxPolicy policy)
{
    parameters.HostConfig.ReadonlyRootfs = true;
    parameters.HostConfig.SecurityOpt = new[] { "no-new-privileges" };
    parameters.HostConfig.CapDrop = new[] { "ALL" };
    parameters.HostConfig.NetworkMode = policy.AllowNetwork ? "bridge" : "none";
    parameters.User = "1000:1000";  // Non-root
    
    return parameters;
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-POL-001 | Policy violation detected |
| ACODE-POL-002 | Network access denied |
| ACODE-POL-003 | Filesystem access denied |

---

**End of Task 020.c Specification**