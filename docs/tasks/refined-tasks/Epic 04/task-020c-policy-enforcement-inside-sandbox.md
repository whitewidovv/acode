# Task 020.c: Policy Enforcement Inside Sandbox

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 020 (Docker Sandbox), Task 001 (Operating Modes)  

---

## Description

### Overview

Task 020.c implements comprehensive security policy enforcement within Docker sandboxes. Containers executing user code MUST operate under strict security constraints that align with the operating modes defined in Task 001. This task creates the policy enforcement layer that translates high-level operating mode selections (local-only, docker, air-gapped) into concrete Docker security configurations, ensuring that sandboxed code cannot escape isolation, access unauthorized resources, or compromise the host system.

### Business Value

1. **Security Guarantee**: Hardened container policies prevent malicious or buggy code from affecting host system
2. **Compliance Enablement**: Documented security controls support audit and compliance requirements
3. **Trust Foundation**: Users can confidently execute untrusted code knowing policies are enforced
4. **Mode Alignment**: Security policies automatically match operating mode constraints
5. **Defense in Depth**: Multiple overlapping controls (network, filesystem, process) provide layered security
6. **Audit Trail**: Logged policy violations support incident investigation and security monitoring

### Scope

This task encompasses:

1. **Network Policy Enforcement**: Control network access based on operating mode (none, bridge, custom)
2. **Filesystem Policy Enforcement**: Read-only root, restricted mounts, symlink escape prevention
3. **Process Policy Enforcement**: Non-root execution, capability dropping, seccomp profiles
4. **Resource Policy Enforcement**: Memory/CPU limits, OOM handling, fork bomb prevention
5. **Policy Configuration**: YAML schema for policy customization where permitted
6. **Violation Detection**: Identify and log policy violation attempts
7. **Audit Logging**: Security event recording for compliance and debugging
8. **Policy Validation**: Verify policies are correctly applied before execution

### Integration Points

| Component | Integration Type | Data Flow |
|-----------|------------------|-----------|
| ContainerLifecycleManager | Policy Application | PolicyEnforcer → CreateContainerParameters |
| OperatingModeService | Mode Detection | ModeService → PolicyEnforcer |
| AgentConfig.yml | Policy Customization | Parser → PolicyEnforcer |
| Docker Client | Container Config | PolicyEnforcer → Docker.DotNet |
| AuditLogger | Security Events | PolicyEnforcer → AuditLog |
| TaskExecutionService | Execution Context | TaskExecutor → PolicyEnforcer |

### Failure Modes

| Failure Mode | Detection | Recovery |
|--------------|-----------|----------|
| Policy application fails | Container start error | Fail fast with clear error message |
| Network escape attempt | Docker networking logs | Log violation, container continues isolated |
| Filesystem escape attempt | Seccomp/AppArmor blocks | Log violation, operation denied |
| Resource limit exceeded | OOM killer / throttling | Container killed or throttled |
| Capability escalation attempt | no-new-privileges blocks | Log violation, operation denied |
| Symlink escape attempt | Mount propagation rules | Operation denied, logged |

### Assumptions

- Docker daemon supports security options (seccomp, capabilities, user namespaces)
- Host kernel supports required security features
- Operating mode is determined before container creation
- Audit logging infrastructure is available
- Resource limits are supported by Docker and host kernel

### Security Philosophy

This implementation follows the principle of least privilege:
- Start with maximum restrictions, selectively relax as needed
- Default deny for network, capabilities, and filesystem access
- All relaxations must be explicit and auditable
- Prefer throttling over termination for resource limits
- Log all security-relevant events

---

## Functional Requirements

### Network Policy

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020C-01 | System MUST set default network mode to `none` | MUST |
| FR-020C-02 | Air-gapped mode MUST use `none` network unconditionally | MUST |
| FR-020C-03 | Local-only mode MUST use `none` network by default | MUST |
| FR-020C-04 | Docker mode MAY enable `bridge` network when explicitly configured | MAY |
| FR-020C-05 | Burst mode MAY enable `bridge` network for LLM API calls only | MAY |
| FR-020C-06 | Network enable MUST be explicit opt-in via configuration | MUST |
| FR-020C-07 | DNS resolution MUST be blocked when network is `none` | MUST |
| FR-020C-08 | System MUST NOT allow `host` network mode | MUST |
| FR-020C-09 | Custom bridge networks MUST be isolated from host network | MUST |
| FR-020C-10 | Network policy MUST be applied before container start | MUST |
| FR-020C-11 | System MUST log network mode applied to container | MUST |
| FR-020C-12 | System MUST block outbound connections when network is `none` | MUST |

### Filesystem Policy

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020C-13 | Container root filesystem MUST be read-only | MUST |
| FR-020C-14 | /workspace MUST be writable for repository access | MUST |
| FR-020C-15 | /tmp MUST be tmpfs with size limit | MUST |
| FR-020C-16 | /var/tmp MUST be tmpfs with size limit | SHOULD |
| FR-020C-17 | Cache volumes MUST be writable when mounted | MUST |
| FR-020C-18 | Artifact output directories MUST be writable | MUST |
| FR-020C-19 | System MUST NOT allow bind mounts outside repository | MUST |
| FR-020C-20 | System MUST prevent symlink escape from mount boundaries | MUST |
| FR-020C-21 | System MUST block access to Docker socket | MUST |
| FR-020C-22 | System MUST block access to /etc/shadow, /etc/passwd | MUST |
| FR-020C-23 | Mount propagation MUST be `private` or `slave` | MUST |
| FR-020C-24 | System MUST validate all mount paths before container start | MUST |
| FR-020C-25 | System MUST log all mounts applied to container | MUST |

### Process Policy

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020C-26 | Containers MUST NOT run as UID 0 (root) | MUST |
| FR-020C-27 | Containers MUST run as configurable non-root user (default 1000:1000) | MUST |
| FR-020C-28 | Containers MUST drop ALL Linux capabilities | MUST |
| FR-020C-29 | Containers MUST set `--security-opt no-new-privileges` | MUST |
| FR-020C-30 | Containers MUST have isolated PID namespace | MUST |
| FR-020C-31 | Containers MUST have isolated IPC namespace | MUST |
| FR-020C-32 | Containers MUST have isolated UTS namespace | MUST |
| FR-020C-33 | Containers MUST NOT run in privileged mode | MUST |
| FR-020C-34 | System MUST apply seccomp profile (default or custom) | MUST |
| FR-020C-35 | System SHOULD apply AppArmor profile when available | SHOULD |
| FR-020C-36 | System MUST block ptrace capability | MUST |
| FR-020C-37 | System MUST block SYS_ADMIN capability | MUST |
| FR-020C-38 | System MUST log user/group applied to container | MUST |

### Resource Policy

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020C-39 | Memory limit MUST be enforced via cgroups | MUST |
| FR-020C-40 | OOM killer MUST be enabled for memory limit breach | MUST |
| FR-020C-41 | CPU limit MUST use CPU quota (throttling, not hard kill) | MUST |
| FR-020C-42 | CPU shares MUST be configurable for relative priority | SHOULD |
| FR-020C-43 | PIDs limit MUST be set to prevent fork bombs | MUST |
| FR-020C-44 | Default PIDs limit MUST be 512 | MUST |
| FR-020C-45 | Ulimits MUST be configured for nofile, nproc | MUST |
| FR-020C-46 | Ulimit nofile default MUST be 65536 | SHOULD |
| FR-020C-47 | Memory swap limit MUST be set to prevent swap abuse | MUST |
| FR-020C-48 | Disk I/O limits SHOULD be configurable | SHOULD |
| FR-020C-49 | System MUST log resource limits applied | MUST |
| FR-020C-50 | System MUST log OOM events | MUST |

### Operating Mode Alignment

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020C-51 | System MUST detect operating mode before applying policies | MUST |
| FR-020C-52 | Air-gapped mode MUST apply maximum security restrictions | MUST |
| FR-020C-53 | Local-only mode MUST apply default security restrictions | MUST |
| FR-020C-54 | Docker mode MUST apply configurable security restrictions | MUST |
| FR-020C-55 | Burst mode MUST apply relaxed network with other restrictions | MUST |
| FR-020C-56 | System MUST validate mode-policy compatibility | MUST |
| FR-020C-57 | System MUST fail if policy violates mode constraints | MUST |
| FR-020C-58 | Policy overrides MUST NOT relax mode-enforced restrictions | MUST |
| FR-020C-59 | System MUST log operating mode and policy applied | MUST |
| FR-020C-60 | Mode constraints MUST take precedence over config overrides | MUST |

### Policy Configuration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020C-61 | Policies MUST be configurable via agent-config.yml | MUST |
| FR-020C-62 | Custom seccomp profiles MUST be loadable from file | SHOULD |
| FR-020C-63 | User/group IDs MUST be configurable | MUST |
| FR-020C-64 | Resource limits MUST be configurable per task | SHOULD |
| FR-020C-65 | tmpfs size limits MUST be configurable | SHOULD |
| FR-020C-66 | PIDs limit MUST be configurable | SHOULD |
| FR-020C-67 | Invalid configurations MUST produce clear errors | MUST |
| FR-020C-68 | Default configurations MUST apply when not specified | MUST |
| FR-020C-69 | Configuration validation MUST occur at startup | MUST |
| FR-020C-70 | Hot reload of policies SHOULD be supported | SHOULD |

### Audit and Logging

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020C-71 | All policy applications MUST be logged | MUST |
| FR-020C-72 | Policy violation attempts MUST be logged at Warning level | MUST |
| FR-020C-73 | Security-relevant events MUST include correlation ID | MUST |
| FR-020C-74 | Audit logs MUST include timestamp, event type, details | MUST |
| FR-020C-75 | OOM events MUST be logged with process details | MUST |
| FR-020C-76 | Network block attempts MUST be logged | MUST |
| FR-020C-77 | Filesystem access denials MUST be logged | MUST |
| FR-020C-78 | Capability escalation attempts MUST be logged | MUST |
| FR-020C-79 | Audit logs MUST be queryable by task ID | MUST |
| FR-020C-80 | Audit log retention MUST be configurable | SHOULD |

### Policy Validation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020C-81 | System MUST validate policy before container start | MUST |
| FR-020C-82 | System MUST verify Docker supports required features | MUST |
| FR-020C-83 | System MUST verify kernel supports seccomp | MUST |
| FR-020C-84 | System MUST verify cgroups are available | MUST |
| FR-020C-85 | Invalid policies MUST prevent container start | MUST |
| FR-020C-86 | Validation errors MUST include remediation guidance | MUST |
| FR-020C-87 | System SHOULD support policy dry-run mode | SHOULD |
| FR-020C-88 | System MUST expose policy inspection command | MUST |
| FR-020C-89 | Policy inspection MUST show effective policy for mode | MUST |
| FR-020C-90 | System MUST log validation results | MUST |

---

## Non-Functional Requirements

### Performance

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-020C-01 | Policy application overhead | < 50ms |
| NFR-020C-02 | Policy validation | < 100ms |
| NFR-020C-03 | Mode detection | < 10ms |
| NFR-020C-04 | Audit log write | < 5ms |
| NFR-020C-05 | Seccomp profile load | < 100ms |
| NFR-020C-06 | Policy inspection command | < 200ms |
| NFR-020C-07 | Memory overhead per container | < 1MB |
| NFR-020C-08 | CPU overhead for policy enforcement | < 1% |

### Reliability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-020C-09 | Policy application MUST be atomic | All or nothing |
| NFR-020C-10 | Failed policy MUST prevent container start | 100% |
| NFR-020C-11 | Partial policy application MUST NOT occur | 100% |
| NFR-020C-12 | Policy enforcement MUST survive container restart | Consistent |
| NFR-020C-13 | Audit logging MUST be reliable | No lost events |
| NFR-020C-14 | OOM handling MUST be deterministic | Immediate kill |
| NFR-020C-15 | Policy MUST be applied before ANY code execution | 100% |
| NFR-020C-16 | Seccomp MUST block syscalls before execution | Kernel level |

### Security

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-020C-17 | No privilege escalation possible | Verified |
| NFR-020C-18 | No container escape possible | Defense in depth |
| NFR-020C-19 | No host filesystem access | Verified |
| NFR-020C-20 | No network access in air-gapped mode | Verified |
| NFR-020C-21 | All capabilities dropped | Verified |
| NFR-020C-22 | Seccomp blocks dangerous syscalls | Default profile |
| NFR-020C-23 | No Docker socket access | Blocked |
| NFR-020C-24 | No /proc or /sys write access | Read-only |
| NFR-020C-25 | Audit trail tamper-resistant | Append-only |
| NFR-020C-26 | Policy configuration validates input | Injection prevention |

### Maintainability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-020C-27 | Policy enforcer code coverage | ≥ 95% |
| NFR-020C-28 | Clear separation of policy types | Modular |
| NFR-020C-29 | Extensible for new policy types | Plugin ready |
| NFR-020C-30 | Configuration-driven policies | No hardcoding |
| NFR-020C-31 | Policy logic cyclomatic complexity | ≤ 8 |
| NFR-020C-32 | Comprehensive policy documentation | Complete |
| NFR-020C-33 | Seccomp profile externalized | JSON file |
| NFR-020C-34 | Easy policy debugging | Inspection command |

### Observability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-020C-35 | Log all security events | Structured |
| NFR-020C-36 | Metrics for policy violations | Prometheus |
| NFR-020C-37 | Alert on repeated violations | Threshold |
| NFR-020C-38 | Trace context through enforcement | Correlation ID |
| NFR-020C-39 | Audit log searchable | By task, time, type |
| NFR-020C-40 | Policy inspection human-readable | CLI output |
| NFR-020C-41 | Health check for policy subsystem | Endpoint |
| NFR-020C-42 | Violation count per task | Tracked |

---

## Acceptance Criteria

### Network Policy

- [ ] AC-020C-01: Default network mode is `none` for new containers
- [ ] AC-020C-02: Air-gapped mode containers have no network access
- [ ] AC-020C-03: Local-only mode containers have no network by default
- [ ] AC-020C-04: Docker mode can enable bridge network via config
- [ ] AC-020C-05: Burst mode enables network for LLM API calls only
- [ ] AC-020C-06: DNS resolution fails when network is `none`
- [ ] AC-020C-07: `host` network mode is rejected with error
- [ ] AC-020C-08: Network mode is logged at container start
- [ ] AC-020C-09: Outbound connections blocked in `none` mode
- [ ] AC-020C-10: Network attempts in `none` mode are logged

### Filesystem Policy

- [ ] AC-020C-11: Root filesystem is mounted read-only
- [ ] AC-020C-12: Write to / fails with permission denied
- [ ] AC-020C-13: /workspace is writable
- [ ] AC-020C-14: Files created in /workspace persist to host
- [ ] AC-020C-15: /tmp is tmpfs with size limit
- [ ] AC-020C-16: /tmp data does NOT persist after container exit
- [ ] AC-020C-17: Bind mounts outside repository are rejected
- [ ] AC-020C-18: Symlinks cannot escape /workspace
- [ ] AC-020C-19: Docker socket (/var/run/docker.sock) is not accessible
- [ ] AC-020C-20: Mount propagation is private
- [ ] AC-020C-21: All mounts logged at container start

### Process Policy

- [ ] AC-020C-22: `id` command shows UID 1000, not 0
- [ ] AC-020C-23: `cat /proc/1/status | grep Cap` shows no capabilities
- [ ] AC-020C-24: `sudo` fails (not installed or permission denied)
- [ ] AC-020C-25: Privileged operations fail (mount, raw socket, etc.)
- [ ] AC-020C-26: `--privileged` flag rejected by policy enforcer
- [ ] AC-020C-27: PID namespace isolated (only container processes visible)
- [ ] AC-020C-28: Seccomp blocks dangerous syscalls (mount, reboot)
- [ ] AC-020C-29: no-new-privileges prevents setuid escalation
- [ ] AC-020C-30: User/group logged at container start

### Resource Policy

- [ ] AC-020C-31: Memory-intensive process triggers OOM killer at limit
- [ ] AC-020C-32: CPU-intensive process is throttled, not killed
- [ ] AC-020C-33: Fork bomb is stopped by PIDs limit
- [ ] AC-020C-34: File descriptor exhaustion blocked by ulimit
- [ ] AC-020C-35: OOM event is logged with process details
- [ ] AC-020C-36: Resource limits logged at container start
- [ ] AC-020C-37: Swap usage limited
- [ ] AC-020C-38: Default PIDs limit is 512

### Operating Mode Alignment

- [ ] AC-020C-39: Air-gapped mode applies maximum restrictions
- [ ] AC-020C-40: Local-only mode applies default restrictions
- [ ] AC-020C-41: Docker mode allows configured relaxations
- [ ] AC-020C-42: Burst mode enables network only
- [ ] AC-020C-43: Config cannot override mode-enforced restrictions
- [ ] AC-020C-44: Mode mismatch produces clear error
- [ ] AC-020C-45: Operating mode logged with policy
- [ ] AC-020C-46: `acode policy show` displays effective policy

### Configuration

- [ ] AC-020C-47: Policies configurable via agent-config.yml
- [ ] AC-020C-48: Custom user/group applied when configured
- [ ] AC-020C-49: Custom resource limits applied when configured
- [ ] AC-020C-50: Custom seccomp profile loaded when specified
- [ ] AC-020C-51: Invalid config produces clear validation error
- [ ] AC-020C-52: Missing config uses sensible defaults
- [ ] AC-020C-53: Policy inspection shows config source

### Audit and Logging

- [ ] AC-020C-54: Policy application logged at Info level
- [ ] AC-020C-55: Violations logged at Warning level
- [ ] AC-020C-56: OOM events logged at Error level
- [ ] AC-020C-57: Logs include correlation ID
- [ ] AC-020C-58: Logs include timestamp
- [ ] AC-020C-59: Audit trail queryable by task ID
- [ ] AC-020C-60: Security events structured (JSON)

### Validation

- [ ] AC-020C-61: Policy validated before container start
- [ ] AC-020C-62: Docker feature support verified
- [ ] AC-020C-63: Kernel support verified (seccomp, cgroups)
- [ ] AC-020C-64: Invalid policy prevents container start
- [ ] AC-020C-65: Validation errors include remediation hints
- [ ] AC-020C-66: `acode policy validate` checks current config

---

## User Manual Documentation

### Overview

The agentic coding bot enforces strict security policies inside Docker sandboxes to protect your host system from untrusted code. These policies are automatically applied based on your operating mode and cannot be bypassed by code running inside containers.

### Security Policies by Operating Mode

| Policy | Air-Gapped | Local-Only | Docker | Burst |
|--------|------------|------------|--------|-------|
| Network | none | none | configurable | bridge (API only) |
| Root Access | blocked | blocked | blocked | blocked |
| Capabilities | all dropped | all dropped | all dropped | all dropped |
| Read-only Root | yes | yes | yes | yes |
| Seccomp | enabled | enabled | enabled | enabled |
| PIDs Limit | 512 | 512 | configurable | 512 |
| Memory Limit | configurable | configurable | configurable | configurable |

### Configuration

```yaml
# .agent/config.yml
sandbox:
  security:
    # User/group for container processes (non-root)
    user: "1000:1000"
    
    # Network policy (overridable only in docker mode)
    network:
      mode: none           # none, bridge
      allow_dns: false     # Only when mode: bridge
    
    # Resource limits
    resources:
      memory: "2g"         # Memory limit
      cpu_quota: 100000    # CPU quota (100000 = 1 CPU)
      pids_limit: 512      # Max processes
      nofile_limit: 65536  # Max open files
    
    # Filesystem
    filesystem:
      tmpfs_size: "256m"   # Size of /tmp
      read_only_root: true # Root filesystem read-only
    
    # Advanced
    seccomp_profile: null  # null=default, path to custom profile
    apparmor_profile: null # null=default, profile name
```

### Default Security Profile

When no configuration is specified, the following defaults apply:

```yaml
# Defaults (applied automatically)
sandbox:
  security:
    user: "1000:1000"
    network:
      mode: none
      allow_dns: false
    resources:
      memory: "2g"
      cpu_quota: 100000
      pids_limit: 512
      nofile_limit: 65536
    filesystem:
      tmpfs_size: "256m"
      read_only_root: true
    seccomp_profile: null  # Uses Docker default seccomp
```

### CLI Commands

#### Inspect Current Policy

```bash
# Show effective policy for current mode
acode policy show

# Output:
# Security Policy (Operating Mode: local-only)
# ============================================
# 
# Network:
#   Mode:        none
#   DNS:         blocked
# 
# Process:
#   User:        1000:1000
#   Capabilities: none (all dropped)
#   Seccomp:     docker-default
#   Privileged:  blocked
# 
# Filesystem:
#   Root FS:     read-only
#   /workspace:  read-write (repository mount)
#   /tmp:        tmpfs (256MB limit)
# 
# Resources:
#   Memory:      2GB (OOM enabled)
#   CPU:         100% of 1 core (throttled)
#   PIDs:        512 max
#   Open Files:  65536 max
```

#### Validate Configuration

```bash
# Validate policy configuration
acode policy validate

# Output:
# ✓ Network policy valid
# ✓ Process policy valid
# ✓ Filesystem policy valid
# ✓ Resource limits valid
# ✓ Seccomp profile valid
# ✓ Operating mode compatibility verified
# 
# Policy validation: PASSED

# With errors:
# ✗ Network policy invalid: mode 'host' not allowed
# ✗ Process policy invalid: user '0' (root) not allowed
# 
# Policy validation: FAILED
```

#### View Audit Log

```bash
# Show security events
acode policy audit

# Output:
# TIMESTAMP            TASK    EVENT                DETAILS
# 2024-01-20 10:15:32  task-1  policy-applied       network=none, user=1000:1000
# 2024-01-20 10:16:05  task-1  network-blocked      destination=8.8.8.8:53
# 2024-01-20 10:16:10  task-1  oom-killed           process=node, memory=2.1GB

# Filter by task
acode policy audit --task task-1

# JSON output
acode policy audit --json
```

### What Is Blocked

The following actions are blocked inside containers:

| Action | Blocked By | Error You'll See |
|--------|------------|------------------|
| Running as root | User policy | Permission denied |
| Network access (air-gapped) | Network policy | Network unreachable |
| Mounting filesystems | Seccomp + capabilities | Operation not permitted |
| Loading kernel modules | Seccomp + capabilities | Operation not permitted |
| Accessing Docker socket | Filesystem policy | No such file |
| Reading /etc/shadow | Filesystem policy | Permission denied |
| Forking > 512 processes | PIDs limit | Resource limit exceeded |
| Using > 2GB memory | Memory limit | Container killed (OOM) |

### Troubleshooting

#### "Operation not permitted" Errors

This usually means you're trying to do something that requires root or elevated capabilities. In the sandbox, this is intentional. If you need this operation:

1. Check if there's a non-privileged alternative
2. Consider running outside the sandbox (not recommended)
3. File a feature request if this is a common need

#### Container Killed (OOM)

Your process exceeded the memory limit. Solutions:

```yaml
# Increase memory limit in config
sandbox:
  security:
    resources:
      memory: "4g"  # Increase from 2g
```

#### Network Connection Failed

In local-only or air-gapped mode, network is intentionally blocked.

```bash
# Check your operating mode
acode config show

# If you need network access, use burst mode (if allowed)
# or switch to docker mode with network enabled
```

### Security Best Practices

1. **Don't disable security features** - They protect your host system
2. **Use the least permissive mode** - Air-gapped > Local-only > Docker > Burst
3. **Review audit logs** - Check for unexpected security events
4. **Keep memory limits reasonable** - Prevents runaway processes
5. **Don't run untrusted code outside sandbox** - Always use the sandbox

---

## Testing Requirements

### Unit Tests

#### PolicyEnforcerTests

```csharp
[Fact] ApplyPolicy_WithAirGappedMode_SetsNetworkNone()
[Fact] ApplyPolicy_WithLocalOnlyMode_SetsNetworkNone()
[Fact] ApplyPolicy_WithDockerMode_SetsConfiguredNetwork()
[Fact] ApplyPolicy_WithBurstMode_SetsBridgeNetwork()
[Fact] ApplyPolicy_Always_DropsAllCapabilities()
[Fact] ApplyPolicy_Always_SetsNoNewPrivileges()
[Fact] ApplyPolicy_Always_SetsNonRootUser()
[Fact] ApplyPolicy_Always_SetsReadOnlyRootFs()
[Fact] ApplyPolicy_Always_SetsPidsLimit()
[Fact] ApplyPolicy_WithCustomUser_AppliesCustomUser()
[Fact] ApplyPolicy_WithCustomMemory_AppliesCustomMemory()
[Fact] ApplyPolicy_RejectsHostNetwork()
[Fact] ApplyPolicy_RejectsPrivilegedMode()
[Fact] ApplyPolicy_RejectsRootUser()
```

#### NetworkPolicyTests

```csharp
[Fact] BuildNetworkConfig_NoneMode_ReturnsNoneNetwork()
[Fact] BuildNetworkConfig_BridgeMode_ReturnsBridgeNetwork()
[Fact] BuildNetworkConfig_HostMode_ThrowsPolicyViolation()
[Fact] BuildNetworkConfig_WithDns_IncludesDnsConfig()
[Fact] BuildNetworkConfig_WithoutDns_ExcludesDnsConfig()
[Fact] ValidateNetwork_AirGappedWithBridge_ThrowsModeViolation()
[Fact] ValidateNetwork_LocalOnlyWithBridge_ThrowsModeViolation()
```

#### FilesystemPolicyTests

```csharp
[Fact] BuildMounts_IncludesWorkspaceMount()
[Fact] BuildMounts_WorkspaceMountIsReadWrite()
[Fact] BuildMounts_IncludesTmpfsMounts()
[Fact] BuildMounts_TmpfsHasSizeLimit()
[Fact] BuildMounts_RootFsIsReadOnly()
[Fact] ValidateMounts_OutsideRepo_ThrowsPolicyViolation()
[Fact] ValidateMounts_DockerSocket_ThrowsPolicyViolation()
[Fact] ValidateMounts_PropagationIsPrivate()
```

#### ProcessPolicyTests

```csharp
[Fact] BuildUserConfig_DefaultsTo1000()
[Fact] BuildUserConfig_CustomUserApplied()
[Fact] BuildUserConfig_RejectsRoot()
[Fact] BuildCapabilities_DropsAll()
[Fact] BuildSecurityOpts_IncludesNoNewPrivileges()
[Fact] BuildSeccompConfig_DefaultProfile()
[Fact] BuildSeccompConfig_CustomProfile()
[Fact] BuildNamespaceConfig_IsolatesPid()
[Fact] BuildNamespaceConfig_IsolatesIpc()
```

#### ResourcePolicyTests

```csharp
[Fact] BuildResourceConfig_AppliesMemoryLimit()
[Fact] BuildResourceConfig_AppliesCpuQuota()
[Fact] BuildResourceConfig_AppliesPidsLimit()
[Fact] BuildResourceConfig_AppliesUlimits()
[Fact] BuildResourceConfig_EnablesOomKiller()
[Fact] BuildResourceConfig_LimitsSwap()
[Fact] BuildResourceConfig_CustomValues()
```

#### PolicyValidatorTests

```csharp
[Fact] Validate_ValidPolicy_ReturnsSuccess()
[Fact] Validate_RootUser_ReturnsError()
[Fact] Validate_HostNetwork_ReturnsError()
[Fact] Validate_Privileged_ReturnsError()
[Fact] Validate_NegativeMemory_ReturnsError()
[Fact] Validate_ZeroPids_ReturnsError()
[Fact] Validate_ModeMismatch_ReturnsError()
[Fact] Validate_MissingSeccomp_ReturnsWarning()
```

#### AuditLoggerTests

```csharp
[Fact] LogPolicyApplied_IncludesAllDetails()
[Fact] LogViolation_IncludesViolationType()
[Fact] LogOomEvent_IncludesProcessDetails()
[Fact] LogNetworkBlocked_IncludesDestination()
[Fact] GetAuditLog_FiltersByTask_ReturnsFiltered()
[Fact] GetAuditLog_ToJson_ReturnsValidJson()
```

### Integration Tests

#### PolicyEnforcementIntegrationTests

```csharp
[Fact] Container_WithNoneNetwork_CannotReachInternet()
[Fact] Container_WithNonRootUser_CannotEscalate()
[Fact] Container_WithReadOnlyRoot_CannotWriteToRoot()
[Fact] Container_WithPidsLimit_BlocksForkBomb()
[Fact] Container_WithMemoryLimit_TriggersOom()
[Fact] Container_WithSeccomp_BlocksDangerousSyscalls()
```

#### ModeEnforcementIntegrationTests

```csharp
[Fact] AirGappedMode_EnforcesMaxSecurity()
[Fact] LocalOnlyMode_EnforcesDefaultSecurity()
[Fact] DockerMode_AllowsConfiguredRelaxations()
[Fact] BurstMode_AllowsNetworkOnly()
[Fact] ModeOverride_CannotRelaxEnforcedRestrictions()
```

#### AuditIntegrationTests

```csharp
[Fact] PolicyViolation_IsLoggedWithDetails()
[Fact] OomEvent_IsLoggedWithProcess()
[Fact] NetworkBlock_IsLoggedWithDestination()
[Fact] AuditLog_QueryByTaskId_ReturnsCorrect()
```

### E2E Tests

#### PolicyCLIE2ETests

```csharp
[Fact] PolicyShow_DisplaysEffectivePolicy()
[Fact] PolicyValidate_WithValidConfig_Passes()
[Fact] PolicyValidate_WithInvalidConfig_Fails()
[Fact] PolicyAudit_ShowsSecurityEvents()
[Fact] PolicyAudit_JsonOutput_IsValidJson()
```

### Performance Benchmarks

| Benchmark | Target | Threshold |
|-----------|--------|-----------|
| PolicyApplication | < 50ms | P95 |
| PolicyValidation | < 100ms | P95 |
| ModeDetection | < 10ms | P95 |
| AuditLogWrite | < 5ms | P95 |
| SeccompProfileLoad | < 100ms | P95 |
| PolicyInspection | < 200ms | P95 |

### Coverage Requirements

| Component | Minimum Coverage |
|-----------|------------------|
| PolicyEnforcer | 95% |
| NetworkPolicy | 95% |
| FilesystemPolicy | 95% |
| ProcessPolicy | 95% |
| ResourcePolicy | 95% |
| PolicyValidator | 95% |
| AuditLogger | 90% |
| CLI Commands | 85% |
| **Overall** | **92%** |

---

## User Verification Steps

### Scenario 1: Verify Non-Root Execution

```bash
# Run a task
acode task run build

# Inside container, check user
docker exec <container-id> id

# Expected output:
# uid=1000(acode) gid=1000(acode) groups=1000(acode)
# NOT uid=0(root)
```

### Scenario 2: Verify Network Blocked

```bash
# In local-only or air-gapped mode
acode task run test

# Inside container, try network
docker exec <container-id> curl https://google.com

# Expected: Connection fails
# curl: (6) Could not resolve host: google.com
```

### Scenario 3: Verify Read-Only Root

```bash
# Inside container, try to write to root
docker exec <container-id> touch /test-file

# Expected:
# touch: cannot touch '/test-file': Read-only file system
```

### Scenario 4: Verify /workspace Writable

```bash
# Inside container, write to workspace
docker exec <container-id> touch /workspace/test-file

# Expected: Success (no error)

# Verify on host
ls -la /path/to/repo/test-file
# File should exist
```

### Scenario 5: Verify Capabilities Dropped

```bash
# Inside container, check capabilities
docker exec <container-id> cat /proc/1/status | grep Cap

# Expected: All zeros
# CapInh: 0000000000000000
# CapPrm: 0000000000000000
# CapEff: 0000000000000000
```

### Scenario 6: Verify PIDs Limit

```bash
# Inside container, try fork bomb (DON'T DO THIS ON HOST)
docker exec <container-id> bash -c ':(){ :|:& };:'

# Expected: Quickly hits PIDs limit, doesn't crash host
# bash: fork: Resource temporarily unavailable
```

### Scenario 7: Verify OOM Behavior

```bash
# Run memory-intensive task with low limit
acode task run memory-hog

# Check logs
acode policy audit

# Expected: OOM event logged
# EVENT: oom-killed, process=memory-hog, memory=2.1GB
```

### Scenario 8: Verify Policy Inspection

```bash
# Show current policy
acode policy show

# Expected: Full policy displayed with mode, network, resources

# Validate policy
acode policy validate

# Expected: All checks pass or clear errors shown
```

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Sandbox/
│       ├── Policy/
│       │   ├── ISandboxPolicyEnforcer.cs
│       │   ├── SandboxPolicy.cs
│       │   ├── NetworkPolicy.cs
│       │   ├── FilesystemPolicy.cs
│       │   ├── ProcessPolicy.cs
│       │   ├── ResourcePolicy.cs
│       │   └── PolicyViolation.cs
│       └── Audit/
│           ├── IAuditLogger.cs
│           ├── AuditEvent.cs
│           └── AuditEventType.cs
├── Acode.Infrastructure/
│   └── Sandbox/
│       ├── Policy/
│       │   ├── SandboxPolicyEnforcer.cs
│       │   ├── NetworkPolicyBuilder.cs
│       │   ├── FilesystemPolicyBuilder.cs
│       │   ├── ProcessPolicyBuilder.cs
│       │   ├── ResourcePolicyBuilder.cs
│       │   ├── PolicyValidator.cs
│       │   └── SeccompProfileLoader.cs
│       └── Audit/
│           └── AuditLogger.cs
├── Acode.Cli/
│   └── Commands/
│       └── PolicyCommands.cs
└── tests/
    ├── Acode.Domain.Tests/
    │   └── Sandbox/
    │       └── Policy/
    │           ├── SandboxPolicyTests.cs
    │           └── PolicyViolationTests.cs
    ├── Acode.Infrastructure.Tests/
    │   └── Sandbox/
    │       └── Policy/
    │           ├── PolicyEnforcerTests.cs
    │           ├── NetworkPolicyTests.cs
    │           ├── FilesystemPolicyTests.cs
    │           ├── ProcessPolicyTests.cs
    │           ├── ResourcePolicyTests.cs
    │           └── PolicyValidatorTests.cs
    └── Acode.Integration.Tests/
        └── Sandbox/
            └── Policy/
                ├── PolicyEnforcementTests.cs
                ├── ModeEnforcementTests.cs
                └── AuditIntegrationTests.cs
```

### Domain Models

```csharp
// ISandboxPolicyEnforcer.cs
namespace Acode.Domain.Sandbox.Policy;

public interface ISandboxPolicyEnforcer
{
    Task<SandboxPolicy> BuildPolicyAsync(
        OperatingMode mode,
        PolicyConfiguration? configuration = null,
        CancellationToken cancellationToken = default);
    
    void ApplyToContainer(
        SandboxPolicy policy,
        CreateContainerParameters parameters);
    
    Task<PolicyValidationResult> ValidateAsync(
        SandboxPolicy policy,
        CancellationToken cancellationToken = default);
}

// SandboxPolicy.cs
namespace Acode.Domain.Sandbox.Policy;

public sealed record SandboxPolicy
{
    public required OperatingMode Mode { get; init; }
    public required NetworkPolicy Network { get; init; }
    public required FilesystemPolicy Filesystem { get; init; }
    public required ProcessPolicy Process { get; init; }
    public required ResourcePolicy Resources { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

// NetworkPolicy.cs
namespace Acode.Domain.Sandbox.Policy;

public sealed record NetworkPolicy
{
    public NetworkMode Mode { get; init; } = NetworkMode.None;
    public bool AllowDns { get; init; } = false;
    public string? CustomNetwork { get; init; }
}

public enum NetworkMode
{
    None,
    Bridge,
    Custom
    // Host intentionally excluded
}

// FilesystemPolicy.cs
namespace Acode.Domain.Sandbox.Policy;

public sealed record FilesystemPolicy
{
    public bool ReadOnlyRoot { get; init; } = true;
    public string WorkspacePath { get; init; } = "/workspace";
    public string TmpfsSize { get; init; } = "256m";
    public MountPropagation Propagation { get; init; } = MountPropagation.Private;
    public IReadOnlyList<string> BlockedPaths { get; init; } = new[]
    {
        "/var/run/docker.sock",
        "/etc/shadow",
        "/etc/passwd"
    };
}

// ProcessPolicy.cs
namespace Acode.Domain.Sandbox.Policy;

public sealed record ProcessPolicy
{
    public string User { get; init; } = "1000:1000";
    public IReadOnlyList<string> CapabilitiesToDrop { get; init; } = new[] { "ALL" };
    public IReadOnlyList<string> CapabilitiesToAdd { get; init; } = Array.Empty<string>();
    public bool NoNewPrivileges { get; init; } = true;
    public bool Privileged { get; init; } = false;
    public string? SeccompProfile { get; init; }  // null = default
    public string? AppArmorProfile { get; init; }
    public bool IsolatePid { get; init; } = true;
    public bool IsolateIpc { get; init; } = true;
    public bool IsolateUts { get; init; } = true;
}

// ResourcePolicy.cs
namespace Acode.Domain.Sandbox.Policy;

public sealed record ResourcePolicy
{
    public long MemoryBytes { get; init; } = 2L * 1024 * 1024 * 1024; // 2GB
    public long MemorySwapBytes { get; init; } = 2L * 1024 * 1024 * 1024; // Same as memory (no swap)
    public long CpuQuota { get; init; } = 100000; // 100% of 1 core
    public long CpuPeriod { get; init; } = 100000;
    public int PidsLimit { get; init; } = 512;
    public long NofileLimit { get; init; } = 65536;
    public long NprocLimit { get; init; } = 512;
    public bool OomKillEnabled { get; init; } = true;
}

// PolicyViolation.cs
namespace Acode.Domain.Sandbox.Policy;

public sealed record PolicyViolation
{
    public required PolicyViolationType Type { get; init; }
    public required string Message { get; init; }
    public required string Detail { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public enum PolicyViolationType
{
    NetworkAccess,
    FilesystemAccess,
    PrivilegeEscalation,
    ResourceExceeded,
    ConfigurationInvalid,
    ModeViolation
}

// IAuditLogger.cs
namespace Acode.Domain.Sandbox.Audit;

public interface IAuditLogger
{
    void LogPolicyApplied(string taskId, SandboxPolicy policy);
    void LogViolation(string taskId, PolicyViolation violation);
    void LogOomEvent(string taskId, string processName, long memoryBytes);
    void LogNetworkBlocked(string taskId, string destination);
    void LogFilesystemBlocked(string taskId, string path);
    Task<IReadOnlyList<AuditEvent>> GetEventsAsync(
        string? taskId = null,
        DateTimeOffset? since = null,
        CancellationToken cancellationToken = default);
}

// AuditEvent.cs
namespace Acode.Domain.Sandbox.Audit;

public sealed record AuditEvent
{
    public required string Id { get; init; }
    public required string TaskId { get; init; }
    public required AuditEventType Type { get; init; }
    public required string Message { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public IReadOnlyDictionary<string, string> Details { get; init; } = 
        new Dictionary<string, string>();
}

public enum AuditEventType
{
    PolicyApplied,
    NetworkBlocked,
    FilesystemBlocked,
    PrivilegeBlocked,
    OomKilled,
    ResourceThrottled
}
```

### Infrastructure Implementation

```csharp
// SandboxPolicyEnforcer.cs
namespace Acode.Infrastructure.Sandbox.Policy;

public sealed class SandboxPolicyEnforcer : ISandboxPolicyEnforcer
{
    private readonly IOperatingModeService _modeService;
    private readonly IPolicyConfigurationProvider _configProvider;
    private readonly ISeccompProfileLoader _seccompLoader;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<SandboxPolicyEnforcer> _logger;
    
    public SandboxPolicyEnforcer(
        IOperatingModeService modeService,
        IPolicyConfigurationProvider configProvider,
        ISeccompProfileLoader seccompLoader,
        IAuditLogger auditLogger,
        ILogger<SandboxPolicyEnforcer> logger)
    {
        _modeService = modeService;
        _configProvider = configProvider;
        _seccompLoader = seccompLoader;
        _auditLogger = auditLogger;
        _logger = logger;
    }
    
    public async Task<SandboxPolicy> BuildPolicyAsync(
        OperatingMode mode,
        PolicyConfiguration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        var config = configuration ?? _configProvider.GetDefault();
        
        // Mode-enforced constraints cannot be overridden
        var networkPolicy = BuildNetworkPolicy(mode, config);
        var filesystemPolicy = BuildFilesystemPolicy(mode, config);
        var processPolicy = BuildProcessPolicy(mode, config);
        var resourcePolicy = BuildResourcePolicy(mode, config);
        
        var policy = new SandboxPolicy
        {
            Mode = mode,
            Network = networkPolicy,
            Filesystem = filesystemPolicy,
            Process = processPolicy,
            Resources = resourcePolicy
        };
        
        _logger.LogDebug("Built policy for mode {Mode}", mode);
        return policy;
    }
    
    public void ApplyToContainer(
        SandboxPolicy policy,
        CreateContainerParameters parameters)
    {
        // Network
        parameters.HostConfig.NetworkMode = policy.Network.Mode switch
        {
            NetworkMode.None => "none",
            NetworkMode.Bridge => "bridge",
            NetworkMode.Custom => policy.Network.CustomNetwork,
            _ => "none"
        };
        
        // Filesystem
        parameters.HostConfig.ReadonlyRootfs = policy.Filesystem.ReadOnlyRoot;
        parameters.HostConfig.Tmpfs = new Dictionary<string, string>
        {
            ["/tmp"] = $"size={policy.Filesystem.TmpfsSize},mode=1777",
            ["/var/tmp"] = $"size={policy.Filesystem.TmpfsSize},mode=1777"
        };
        
        // Process
        parameters.User = policy.Process.User;
        parameters.HostConfig.CapDrop = policy.Process.CapabilitiesToDrop.ToList();
        parameters.HostConfig.CapAdd = policy.Process.CapabilitiesToAdd.ToList();
        parameters.HostConfig.SecurityOpt = new List<string>();
        
        if (policy.Process.NoNewPrivileges)
        {
            parameters.HostConfig.SecurityOpt.Add("no-new-privileges");
        }
        
        if (policy.Process.SeccompProfile is not null)
        {
            parameters.HostConfig.SecurityOpt.Add(
                $"seccomp={policy.Process.SeccompProfile}");
        }
        
        parameters.HostConfig.PidMode = policy.Process.IsolatePid ? "" : "host";
        parameters.HostConfig.IpcMode = policy.Process.IsolateIpc ? "" : "host";
        
        // Resources
        parameters.HostConfig.Memory = policy.Resources.MemoryBytes;
        parameters.HostConfig.MemorySwap = policy.Resources.MemorySwapBytes;
        parameters.HostConfig.CpuQuota = policy.Resources.CpuQuota;
        parameters.HostConfig.CpuPeriod = policy.Resources.CpuPeriod;
        parameters.HostConfig.PidsLimit = policy.Resources.PidsLimit;
        parameters.HostConfig.OomKillDisable = !policy.Resources.OomKillEnabled;
        
        parameters.HostConfig.Ulimits = new List<Ulimit>
        {
            new() { Name = "nofile", Soft = policy.Resources.NofileLimit, Hard = policy.Resources.NofileLimit },
            new() { Name = "nproc", Soft = policy.Resources.NprocLimit, Hard = policy.Resources.NprocLimit }
        };
        
        _logger.LogInformation(
            "Applied security policy: network={Network}, user={User}, memory={Memory}",
            policy.Network.Mode,
            policy.Process.User,
            policy.Resources.MemoryBytes);
    }
    
    private NetworkPolicy BuildNetworkPolicy(OperatingMode mode, PolicyConfiguration config)
    {
        // Mode-enforced restrictions
        return mode switch
        {
            OperatingMode.AirGapped => new NetworkPolicy { Mode = NetworkMode.None },
            OperatingMode.LocalOnly => new NetworkPolicy { Mode = NetworkMode.None },
            OperatingMode.Docker => new NetworkPolicy 
            { 
                Mode = config.Network?.Mode ?? NetworkMode.None,
                AllowDns = config.Network?.AllowDns ?? false
            },
            OperatingMode.Burst => new NetworkPolicy 
            { 
                Mode = NetworkMode.Bridge,
                AllowDns = true
            },
            _ => new NetworkPolicy { Mode = NetworkMode.None }
        };
    }
    
    private ProcessPolicy BuildProcessPolicy(OperatingMode mode, PolicyConfiguration config)
    {
        var user = config.User ?? "1000:1000";
        
        // Validate not root
        if (user.StartsWith("0:") || user == "root")
        {
            throw new PolicyViolationException(new PolicyViolation
            {
                Type = PolicyViolationType.ConfigurationInvalid,
                Message = "Root user is not allowed",
                Detail = $"User '{user}' resolves to root"
            });
        }
        
        return new ProcessPolicy
        {
            User = user,
            CapabilitiesToDrop = new[] { "ALL" },
            CapabilitiesToAdd = Array.Empty<string>(),
            NoNewPrivileges = true,
            Privileged = false,
            SeccompProfile = config.SeccompProfile,
            IsolatePid = true,
            IsolateIpc = true,
            IsolateUts = true
        };
    }
    
    private FilesystemPolicy BuildFilesystemPolicy(OperatingMode mode, PolicyConfiguration config)
    {
        return new FilesystemPolicy
        {
            ReadOnlyRoot = true,  // Always enforced
            WorkspacePath = "/workspace",
            TmpfsSize = config.TmpfsSize ?? "256m",
            Propagation = MountPropagation.Private
        };
    }
    
    private ResourcePolicy BuildResourcePolicy(OperatingMode mode, PolicyConfiguration config)
    {
        return new ResourcePolicy
        {
            MemoryBytes = ParseMemory(config.Memory ?? "2g"),
            MemorySwapBytes = ParseMemory(config.Memory ?? "2g"), // No swap
            CpuQuota = config.CpuQuota ?? 100000,
            CpuPeriod = 100000,
            PidsLimit = config.PidsLimit ?? 512,
            NofileLimit = config.NofileLimit ?? 65536,
            NprocLimit = config.PidsLimit ?? 512,
            OomKillEnabled = true
        };
    }
    
    private static long ParseMemory(string memory)
    {
        var value = long.Parse(memory[..^1]);
        return memory[^1] switch
        {
            'g' or 'G' => value * 1024 * 1024 * 1024,
            'm' or 'M' => value * 1024 * 1024,
            'k' or 'K' => value * 1024,
            _ => value
        };
    }
}
```

### Error Codes

| Code | Meaning | Recovery |
|------|---------|----------|
| ACODE-POL-001 | Policy violation detected | Review and correct policy configuration |
| ACODE-POL-002 | Network access denied | Expected in local-only/air-gapped mode |
| ACODE-POL-003 | Filesystem access denied | Cannot write to read-only paths |
| ACODE-POL-004 | Privilege escalation blocked | Cannot run as root or gain capabilities |
| ACODE-POL-005 | Resource limit exceeded | Increase limits in configuration |
| ACODE-POL-006 | Mode violation | Configuration incompatible with mode |
| ACODE-POL-007 | Seccomp profile invalid | Check profile JSON syntax |
| ACODE-POL-008 | Docker feature not supported | Update Docker or use different config |
| ACODE-POL-009 | OOM killed | Process exceeded memory limit |
| ACODE-POL-010 | PIDs limit reached | Fork bomb or too many processes |

### Implementation Checklist

- [ ] Create domain models for all policy types
- [ ] Implement `ISandboxPolicyEnforcer` interface
- [ ] Implement `SandboxPolicyEnforcer` with mode-aware policy building
- [ ] Implement `NetworkPolicyBuilder` for network configuration
- [ ] Implement `FilesystemPolicyBuilder` for mount configuration
- [ ] Implement `ProcessPolicyBuilder` for user/capability configuration
- [ ] Implement `ResourcePolicyBuilder` for limits configuration
- [ ] Implement `PolicyValidator` for pre-start validation
- [ ] Implement `SeccompProfileLoader` for custom profiles
- [ ] Implement `AuditLogger` for security event logging
- [ ] Integrate policy enforcer with `ContainerLifecycleManager`
- [ ] Create CLI `policy show` command
- [ ] Create CLI `policy validate` command
- [ ] Create CLI `policy audit` command
- [ ] Add unit tests for all policy builders
- [ ] Add integration tests for policy enforcement
- [ ] Add E2E tests for CLI commands
- [ ] Document policy configuration in user manual
- [ ] Create default seccomp profile

### Rollout Plan

| Phase | Action | Validation |
|-------|--------|------------|
| 1 | Implement domain models | Unit tests pass |
| 2 | Implement policy builders | Builder tests pass |
| 3 | Implement policy enforcer | Enforcer tests pass |
| 4 | Implement policy validator | Validation tests pass |
| 5 | Integrate with ContainerLifecycleManager | Integration tests pass |
| 6 | Implement audit logging | Audit tests pass |
| 7 | Add CLI commands | E2E tests pass |
| 8 | Security validation | Penetration testing |
| 9 | Documentation and release | User manual complete |

---

**End of Task 020.c Specification**