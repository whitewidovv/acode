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

### ROI Calculation

**Aggregate Value Across Three Representative Use Cases:**

| Use Case | Annual Cost (Before) | Annual Cost (After) | Annual Savings | Payback Period |
|----------|---------------------|---------------------|----------------|----------------|
| Security Testing (Teresa) | $33,280 | $2,600 | $30,680 | 1.5 days |
| Build Reproducibility (Marcus) | $120,000 | $10,000 | $110,000 | 2 days |
| Air-Gapped Compliance (Dr. Priya) | $126,400 | $6,400 | $120,000 | 1 day |
| **Total** | **$279,680** | **$19,000** | **$260,680** | **1.6 days avg** |

**ROI Metrics:**
- **Total Annual Savings:** $260,680 (93% cost reduction)
- **Implementation Cost:** $150/hour × 80 hours (2 weeks) = $12,000
- **ROI:** 2,172% first year
- **Payback Period:** 1.6 days average (0.6 weeks)
- **Break-even:** After 17 days of operation

**Qualitative Benefits (Not Monetized):**
- **Zero security incidents** from code execution (prev. 24/year across use cases)
- **100% environment reproducibility** between CI and local
- **Perfect compliance** for air-gapped requirements (provable at kernel level)
- **90x faster** security testing workflows (45min → 30sec per test)
- **96% faster** developer onboarding (2 days → 30 minutes)

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          Docker Sandbox Architecture                             │
└─────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────┐
│  Host System                                                                     │
│                                                                                  │
│  ┌─────────────────┐                                                            │
│  │  Acode CLI      │                                                            │
│  │  Command Entry  │                                                            │
│  └────────┬────────┘                                                            │
│           │                                                                      │
│           │ 1. Execute Command                                                  │
│           ▼                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐           │
│  │  Command Executor (Task 018)                                     │           │
│  │  ┌─────────────────────────────────────────────────────────┐   │           │
│  │  │  If Sandbox Enabled:                                     │   │           │
│  │  │    Delegate to ISandbox.RunAsync()                       │   │           │
│  │  │  Else:                                                   │   │           │
│  │  │    Execute directly on host                              │   │           │
│  │  └─────────────────────────────────────────────────────────┘   │           │
│  └──────────────────────────┬──────────────────────────────────────┘           │
│                             │                                                   │
│                             │ 2. RunAsync(command, policy)                      │
│                             ▼                                                   │
│  ┌─────────────────────────────────────────────────────────────────────────┐  │
│  │  DockerSandbox (ISandbox Implementation)                                 │  │
│  │                                                                           │  │
│  │  ┌──────────────┐  ┌─────────────┐  ┌──────────────┐  ┌─────────────┐ │  │
│  │  │  Mount       │  │  Resource   │  │  Network     │  │  Image      │ │  │
│  │  │  Manager     │  │  Limiter    │  │  Policy      │  │  Manager    │ │  │
│  │  └──────┬───────┘  └──────┬──────┘  └──────┬───────┘  └──────┬──────┘ │  │
│  │         │                 │                 │                 │         │  │
│  │         │ Validate Paths  │ Set CPU/Mem    │ Network Mode   │ Pull    │  │
│  │         └─────────────────┴─────────────────┴─────────────────┘ Image  │  │
│  │                             │                                            │  │
│  │                             │ 3. CreateContainerAsync                    │  │
│  │                             ▼                                            │  │
│  │  ┌────────────────────────────────────────────────────────────────┐    │  │
│  │  │  ContainerLifecycle                                             │    │  │
│  │  │                                                                 │    │  │
│  │  │  • CreateContainerAsync(CreateContainerParameters)             │    │  │
│  │  │  • StartContainerAsync(containerId)                            │    │  │
│  │  │  • WaitForCompletionAsync(containerId, timeout)                │    │  │
│  │  │  • GetLogsAsync(containerId) → stdout/stderr                   │    │  │
│  │  │  • RemoveContainerAsync(containerId, force: true)              │    │  │
│  │  └────────────────────────┬───────────────────────────────────────┘    │  │
│  └───────────────────────────┼────────────────────────────────────────────┘  │
│                              │                                                │
│                              │ 4. Docker API Calls (Docker.DotNet)            │
│                              ▼                                                │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │  Docker Daemon (dockerd)                                                │  │
│  │  • Listens on /var/run/docker.sock (Linux) or npipe (Windows)          │  │
│  │  • Creates container from image                                         │  │
│  │  • Enforces resource limits via cgroups                                 │  │
│  │  • Enforces network isolation via namespaces                            │  │
│  │  • Enforces security constraints (capabilities, seccomp)                │  │
│  └────────────────────────────┬───────────────────────────────────────────┘  │
└─────────────────────────────────┼──────────────────────────────────────────────┘
                                  │ 5. Container Lifecycle
                                  ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  Docker Container (Isolated Namespace)                                          │
│                                                                                  │
│  ┌────────────────────────────────────────────────────────────────────────┐   │
│  │  Container Filesystem (Overlay2)                                        │   │
│  │  ┌──────────────────────────────────────────────────────────────────┐ │   │
│  │  │  /                                  (Read-Only, from image)       │ │   │
│  │  │  ├── bin/, lib/, usr/               Security: non-root (uid 1000) │ │   │
│  │  │  ├── etc/                           Capabilities: NONE             │ │   │
│  │  │  └── tmp/  (tmpfs, writable)        Seccomp: default profile      │ │   │
│  │  └──────────────────────────────────────────────────────────────────┘ │   │
│  │  ┌──────────────────────────────────────────────────────────────────┐ │   │
│  │  │  /workspace                         (Bind Mount from Host)        │ │   │
│  │  │  └── Repository files (rw or ro)    Source: /path/to/repo        │ │   │
│  │  └──────────────────────────────────────────────────────────────────┘ │   │
│  │  ┌──────────────────────────────────────────────────────────────────┐ │   │
│  │  │  /root/.nuget/packages              (Volume Mount, cache)        │ │   │
│  │  │  /root/.npm/_cache                  Persistent across containers │ │   │
│  │  └──────────────────────────────────────────────────────────────────┘ │   │
│  └────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│  ┌────────────────────────────────────────────────────────────────────────┐   │
│  │  Resource Limits (cgroups)                                             │   │
│  │  • CPU: 1.0 core (100% of single core)                                 │   │
│  │  • Memory: 512MB hard limit (OOM kill at 512MB)                        │   │
│  │  • PIDs: 256 max processes (fork bomb prevention)                      │   │
│  │  • Disk I/O: Best-effort (no hard limit)                               │   │
│  └────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│  ┌────────────────────────────────────────────────────────────────────────┐   │
│  │  Network Namespace                                                      │   │
│  │  • Default: network=none (no interfaces except lo)                      │   │
│  │  • Enabled: network=bridge (veth pair to docker0)                       │   │
│  │  • DNS: Disabled if network=none                                        │   │
│  └────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│  ┌────────────────────────────────────────────────────────────────────────┐   │
│  │  Command Execution                                                      │   │
│  │  ┌─────────────────────────────────────────────────────────────────┐  │   │
│  │  │  $ dotnet build                     (or npm test, python run.py) │  │   │
│  │  │  └──> stdout: Build succeeded.                                   │  │   │
│  │  │  └──> stderr: (warnings)                                         │  │   │
│  │  │  └──> exit code: 0                                               │  │   │
│  │  └─────────────────────────────────────────────────────────────────┘  │   │
│  └────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│  6. Container Completes → Logs Captured → Container Removed                     │
└─────────────────────────────────────────────────────────────────────────────────┘
                                  │
                                  │ 7. Return SandboxResult
                                  ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  SandboxResult                                                                   │
│  {                                                                               │
│    Stdout: "Build succeeded. 0 Warning(s). 0 Error(s).",                       │
│    Stderr: "",                                                                   │
│    ExitCode: 0,                                                                  │
│    ContainerId: "abc123def456",                                                  │
│    Duration: TimeSpan(00:00:05.234),                                             │
│    ResourceStats: { PeakMemoryMB: 387, CpuPercent: 72 }                         │
│  }                                                                               │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Architectural Decisions and Trade-offs

#### 1. Docker Containers vs. Virtual Machines

**Decision:** Use Docker containers (namespaces + cgroups) instead of full VMs (QEMU/VirtualBox).

**Trade-offs:**
- **Pro:** 10-50x faster startup (1s vs. 30-60s for VM boot)
- **Pro:** 10x less memory overhead per instance (50MB vs. 512MB+ for VM)
- **Pro:** Native performance (no virtualization overhead)
- **Pro:** Simpler API (Docker.DotNet vs. libvirt/VBoxManage)
- **Con:** Weaker isolation boundary (shared kernel, not separate OS)
- **Con:** Windows-only code requires Windows containers (less common, larger images)
- **Con:** Privilege escalation vulnerabilities can affect host (e.g., CVE-2019-5736)

**Justification:** For agentic coding workloads, the performance and ergonomics benefits outweigh the slightly weaker isolation. Containers provide "good enough" isolation for most threat models (buggy code, accidental damage), while extreme threats (APT, targeted malware) are out of scope. Organizations with high-security requirements can layer containers inside VMs.

---

#### 2. Persistent Containers vs. Per-Execution Containers

**Decision:** Create a new container for each command execution, then destroy it.

**Trade-offs:**
- **Pro:** Perfect state isolation between tasks (no state leakage)
- **Pro:** Automatic cleanup (no manual garbage collection)
- **Pro:** Simplified debugging (container ID maps 1:1 to execution)
- **Con:** 500ms-1s overhead per execution (create + destroy)
- **Con:** Cache volumes required for acceptable performance (NuGet, npm)
- **Con:** Cannot reuse running services (e.g., database container for tests)

**Justification:** Clean state per execution aligns with agentic workflow goals (reproducibility, predictability). The 1s overhead is acceptable for typical commands (builds: 10s-5min, tests: 5s-5min). Future optimization: implement container pooling for hot-path commands if profiling shows overhead dominates.

---

#### 3. Bind Mounts vs. Copy-In/Copy-Out

**Decision:** Bind mount the repository from host into container at `/workspace`.

**Trade-offs:**
- **Pro:** Zero-copy (instant availability of repo files)
- **Pro:** Changes visible on host immediately (build artifacts, generated code)
- **Pro:** Supports large repositories (100GB+ monorepos)
- **Con:** Host filesystem performance (especially on macOS/Windows Docker Desktop)
- **Con:** File permission mismatches (container uid 1000 vs. host uid)
- **Con:** Path translation complexity (Windows paths → Linux paths)

**Justification:** Copy-in would require copying entire repository (10s-60s for large repos), making sandbox unusable. Bind mount is the only practical option. Permission mismatches are handled by configuring container user to match host user (configurable uid:gid). macOS/Windows filesystem performance limitations are inherent to Docker Desktop and cannot be avoided without alternative architectures (VM-based Docker Engine on Linux host).

---

#### 4. Docker.DotNet SDK vs. Docker CLI Wrapper

**Decision:** Use Docker.DotNet library (native API client) instead of wrapping `docker` CLI commands.

**Trade-offs:**
- **Pro:** Type-safe API (compile-time checking)
- **Pro:** Structured responses (no parsing stdout)
- **Pro:** Better error handling (exceptions vs. exit codes)
- **Pro:** 2-3x faster (no process spawn overhead)
- **Con:** Dependency on third-party library (Docker.DotNet)
- **Con:** Slightly lagging Docker feature parity (API client updated after CLI)

**Justification:** Docker.DotNet is the official library from Docker, well-maintained, and battle-tested. Type safety and performance benefits outweigh the minor dependency risk. For unsupported features, can fall back to CLI wrapper.

---

#### 5. Default Network Disabled vs. Default Network Enabled

**Decision:** Network disabled by default (`network: none`), opt-in to enable.

**Trade-offs:**
- **Pro:** Security by default (prevents accidental data exfiltration)
- **Pro:** Compliance-friendly (air-gapped provable at config level)
- **Pro:** Fails loudly (npm install fails immediately, not silently)
- **Con:** Breaks common workflows (npm install, NuGet restore)
- **Con:** User must explicitly enable network for legitimate needs
- **Con:** Documentation burden (explain why network disabled)

**Justification:** Aligns with security-first philosophy and operating mode design (LocalOnly, Burst, Airgapped). Network-enabled mode is trivial to configure (`network: true` in config or `--network` CLI flag), making this a low-friction default. Errors are actionable: "Network disabled by policy. Enable with --network flag."

---

## Use Cases

### Use Case 1: Security Researcher Tests Exploit Detection (Teresa)

**Persona:** Teresa is a security engineer at FinanceCorp implementing agentic code analysis for vulnerability detection. The bot needs to execute potentially malicious code samples to test detection rules without compromising the developer workstation.

**Before (No Sandbox):**
- Manual VM setup for each test run: 45 minutes per test cycle
- Risk of accidental execution on host system: 2 incidents per month requiring clean reinstalls
- Limited test coverage due to setup overhead: Only 20% of vulnerability database tested
- Manual cleanup of test artifacts: 15 minutes per run
- **Cost:** 8 hours/week for manual testing + 2 workstation rebuilds/month = $640/week ($33,280/year at $80/hr)

**After (Docker Sandbox):**
- Automated sandbox execution: 30 seconds per test (90x faster)
- Zero host contamination incidents: Perfect isolation
- Full vulnerability database coverage: 100% of 5,000 test cases
- Automatic cleanup: 0 manual intervention required
- Parallel test execution: 10 simultaneous containers, 10 test suites/hour vs. 0.13/hour
- **Savings:** $30,680/year (92% reduction), payback period: 1.5 days
- **Metrics:** 90x faster execution, 0 security incidents (down from 24/year), 5x test coverage

**Workflow Difference:**
```
Before (45 min):
1. Launch VirtualBox VM (5 min)
2. Snapshot clean state (2 min)
3. Copy test code to VM (1 min)
4. Run exploit sample manually (10 min)
5. Observe behavior, take notes (15 min)
6. Revert to snapshot (2 min)
7. Shutdown VM (1 min)
8. Repeat for next test case (9 min setup)

After (30 sec):
1. Run: acode sandbox exec --image security-tools:latest "python exploit.py" (30 sec)
2. Container auto-creates, runs, captures output, auto-destroys
3. Immediately start next test (no setup)
```

---

### Use Case 2: DevOps Lead Ensures Build Reproducibility (Marcus)

**Persona:** Marcus leads DevOps for a 50-person engineering team building a multi-language SaaS platform (.NET backend, React frontend). CI builds succeed but local builds fail due to environment differences. The agentic bot needs to execute builds in the exact CI environment.

**Before (No Sandbox):**
- "Works in CI, fails locally" debugging: 3 hours per incident, 8 incidents/week
- Developer environment drift: 25% of team has mismatched SDK versions
- Onboarding new developers: 2 days to configure local environment
- CI config changes break local workflows: 2 times/month, 6 hours each to fix
- **Cost:** 24 hours/week troubleshooting + 16 hours/month CI breakage = 1.5 FTE = $120,000/year (at $80k/year average)

**After (Docker Sandbox):**
- Identical CI and local environments: 0 "works in CI" incidents
- Instant environment replication: Pull `ci-build:latest` image
- Onboarding time reduced: 30 minutes (pull image, run sandbox)
- CI changes tested locally first: 0 workflow breakages
- **Savings:** $110,000/year (92% reduction), payback period: 2 days
- **Metrics:** 0 environment-related incidents (down from 32/month), 96% faster onboarding (30min vs 16hr), 100% CI/local parity

**Workflow Difference:**
```
Before (3 hour debugging session):
1. CI build succeeds, local build fails (30 min to discover)
2. Compare CI logs vs local output (45 min)
3. Identify SDK version mismatch (.NET 8.0.2 vs 8.0.1)
4. Download and install matching SDK (20 min)
5. Discover NuGet cache corruption (30 min)
6. Clear NuGet cache, rebuild (15 min)
7. New error: missing system library (30 min research)
8. Install library, rebuild, finally succeeds (10 min)

After (5 min):
1. Run: acode sandbox exec --image ghcr.io/company/ci-build:latest "dotnet build"
2. Build runs in exact CI image, succeeds immediately
3. If fails, failure is real bug (not environment drift)
```

---

### Use Case 3: AI Safety Team Tests Agent in Air-Gapped Mode (Dr. Priya)

**Persona:** Dr. Priya leads AI safety at a defense contractor building an agentic coding system for classified networks. The agent must execute code with zero possibility of network exfiltration, even from compromised dependencies.

**Before (No Sandbox):**
- Manual network monitoring: 40 hours/week, 1 dedicated security engineer
- Firewall rule management: 10 hours/week updating allow/deny lists
- Incident investigation: 6 hours/month for suspicious connections (false positives)
- Compliance audit overhead: 80 hours/quarter proving network isolation
- **Cost:** 1 FTE security engineer ($120k/year) + 80 hours/quarter audits ($6,400/year) = $126,400/year

**After (Docker Sandbox with Network Disabled):**
- Enforced at kernel level: 0 network access, 0 monitoring needed
- No firewall rule management: Network mode "none" blocks everything
- Zero false positive investigations: Impossible for container to connect
- Compliance trivial: "Docker network=none" proves isolation
- **Savings:** $120,000/year (95% reduction), payback period: 1 day
- **Metrics:** 0 network connections (down from 150 allowed/week), 0 investigation hours (down from 6/month), 95% faster compliance audits

**Workflow Difference:**
```
Before (40 hours/week monitoring):
1. Agent executes code
2. Network monitor captures all packets (tcpdump running 24/7)
3. Analyze 50GB/day of traffic logs for anomalies (8 hours/day)
4. Investigate 5-10 suspicious connections/day (4 hours/day)
5. Update firewall allow list when false positive found (2 hours/day)
6. Weekly report to compliance team (6 hours/week)

After (0 hours monitoring):
1. Agent executes code in sandbox with network=none
2. Kernel enforces network isolation (impossible to bypass)
3. Compliance report: "All executions used network=none" (5 min/week)
```

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **Sandbox** | An isolated execution environment that prevents code from affecting the host system. In this context, Docker containers provide sandboxing through Linux namespaces, cgroups, and seccomp. |
| **Container** | A Docker container instance - a running process with isolated filesystem, network, and process namespaces. Containers are created from images and destroyed after execution. |
| **Image** | A read-only template used to create containers. Images contain the filesystem snapshot (OS, runtimes, dependencies). Examples: `mcr.microsoft.com/dotnet/sdk:8.0`, `node:20-alpine`. |
| **Mount** | A filesystem binding that makes host directories accessible inside the container. Bind mounts attach host paths (e.g., `/home/user/repo` → `/workspace`), while volume mounts use Docker-managed storage. |
| **Bind Mount** | A type of mount that directly maps a host directory path into the container. Changes in either location are immediately visible in both. Used for repository mounting. |
| **Volume Mount** | A type of mount using Docker-managed storage volumes. Persists across container deletions. Used for caches (NuGet, npm) that should survive container lifecycle. |
| **Resource Limit** | Constraints on CPU, memory, PIDs, and I/O enforced by Linux cgroups. Prevents runaway processes from exhausting host resources. Example: `memory: 512m` limits RAM to 512MB. |
| **Network Policy** | Rules governing container network access. `network: none` disables all networking. `network: bridge` enables connectivity through Docker's bridge network. Air-gapped mode enforces `none`. |
| **Container Lifecycle** | The sequence of states: create → start → run → stop → remove. Each command execution creates a fresh container and removes it after completion. |
| **Namespace** | A Linux kernel feature that isolates system resources (PID, network, mount, IPC, UTS, user). Containers use namespaces to create isolated views of the system. |
| **cgroups** | Control Groups - Linux kernel feature limiting and accounting resource usage (CPU, memory, I/O). Docker uses cgroups to enforce container resource limits. |
| **Seccomp** | Secure Computing mode - Linux kernel feature restricting system calls available to a process. Docker applies seccomp profiles to limit dangerous syscalls in containers. |
| **Capabilities** | Linux security feature dividing root privileges into distinct units (e.g., CAP_NET_BIND_SERVICE for binding privileged ports). Containers drop all capabilities by default for security. |
| **Overlay Filesystem (Overlay2)** | Docker's storage driver using layered filesystem. Image layers are read-only, container layer is writable. Changes are Copy-on-Write, making containers fast to create. |
| **Docker Daemon (dockerd)** | The background service managing containers on the host. Listens on `/var/run/docker.sock` (Linux) or named pipe (Windows). Acode communicates with daemon via Docker API. |
| **Docker.DotNet** | Official .NET library for Docker API communication. Provides type-safe API client for creating, managing, and monitoring containers programmatically. |
| **ISandbox** | Domain interface abstracting sandboxed execution. Implementations can use Docker, VMs, or other isolation mechanisms. Enables mocking for tests and alternative sandbox backends. |
| **Orphaned Container** | A container left running after unexpected termination of the parent process. Acode detects orphans using labels (`acode.managed=true`) and cleans them up on startup. |
| **Air-Gapped Mode** | Execution environment with zero network access, preventing all external communication. Enforced by setting container network mode to "none". Required for classified/sensitive environments. |
| **OOM Kill** | Out-Of-Memory Kill - when a container exceeds its memory limit, the kernel kills it with signal SIGKILL (exit code 137). Detected by checking container exit status. |

---

## Out of Scope

1. **Container Orchestration (Kubernetes, Docker Swarm)** - This task focuses on single-container execution per command. Multi-container orchestration, service meshes, and cluster management are beyond scope. Rationale: Agentic coding tasks are inherently sequential and single-threaded per execution.

2. **Custom Image Building (Dockerfile, BuildKit)** - Only pre-built images from registries are supported. Acode will not dynamically build images from Dockerfiles. Rationale: Image building adds 30s-10min overhead per execution, violating performance requirements. Users must pre-build and push images.

3. **Private Registry Authentication (Docker login)** - Initial version supports only public images from Docker Hub and other public registries. No support for authenticated registries (GitHub Container Registry with token, AWS ECR, Azure ACR). Rationale: Credential management adds security complexity deferred to future iteration.

4. **GPU Passthrough (NVIDIA CUDA, AMD ROCm)** - Only CPU and memory resources are managed. No support for GPU allocation, CUDA toolkit, or ML/AI workloads requiring GPU acceleration. Rationale: GPU passthrough requires privileged mode and host driver dependencies, violating security constraints.

5. **Windows Containers (mcr.microsoft.com/windows/*)** - Only Linux containers are supported, even when running on Windows hosts (via WSL2 or Hyper-V). Windows containers require Windows Server host or special licensing. Rationale: Linux containers are ubiquitous, faster, and smaller. Windows-specific code can be cross-compiled or built in CI.

6. **Container Composition (docker-compose, multi-container stacks)** - No support for defining multi-container applications with dependencies (e.g., web + database + redis). Each execution uses exactly one container. Rationale: Agentic coding tasks execute atomically without external service dependencies. If needed, services can be mocked or started in test code.

7. **Host Network Mode (--network=host)** - Containers cannot use host network stack directly. Only `none` (default) and `bridge` modes are supported. Rationale: Host network mode bypasses network isolation, violating security requirements. Port publishing on bridge network provides necessary connectivity.

8. **Privileged Mode (--privileged)** - Containers MUST NOT run in privileged mode, which disables all security constraints. This is a hard security requirement with no exceptions. Rationale: Privileged containers can trivially escape to host, rendering sandboxing pointless.

9. **Docker-in-Docker (DinD, mounting Docker socket)** - Containers cannot build or run other containers. The Docker socket (`/var/run/docker.sock`) is never mounted into sandbox containers. Rationale: Docker socket access grants full host control, equivalent to privileged mode. If Docker is needed, use kaniko for building or `docker run` before sandbox execution.

10. **Persistent Container State Across Executions** - Containers are ephemeral. File changes inside container (outside mounted `/workspace`) are lost after execution. No support for "resuming" a container. Rationale: Persistent state violates reproducibility and complicates cleanup. Use volume mounts for data that must persist.

11. **Interactive Container Sessions (TTY, stdin)** - Containers run in non-interactive mode only. No support for `docker exec -it` style interactive shells or stdin redirection. Rationale: Agentic execution is batch-mode, not interactive. If debugging is needed, use `acode sandbox exec --image <img> -- bash -c "commands"` for one-shot execution.

12. **Custom Seccomp/AppArmor Profiles** - Only the default Docker seccomp profile is supported. Users cannot provide custom seccomp JSON or AppArmor policies. Rationale: Custom profiles require deep Linux security expertise and could accidentally weaken isolation. Default profile is sufficient for 99% of workloads.

13. **Container Snapshotting/Checkpointing (CRIU)** - No support for checkpoint/restore of running containers to save and resume execution state. Rationale: Adds complexity and CRIU has limited Docker integration. If long-running tasks need resumability, implement application-level checkpointing.

14. **Multi-Architecture Images (ARM64, s390x, ppc64le)** - Only x86_64 (amd64) images are tested and officially supported. Other architectures may work but are not guaranteed. Rationale: Developer workstations are predominantly x86_64. ARM support (Apple Silicon) is possible via Rosetta emulation but performance is degraded.

15. **Real-Time Container Monitoring (Stats API streaming)** - Resource usage stats are captured at end of execution only, not streamed during. No live dashboard of CPU/memory graphs during build. Rationale: Streaming stats API adds complexity and overhead. Post-execution summary (peak memory, average CPU) is sufficient for resource planning.

---

## Assumptions

### Technical Assumptions

1. **Docker Installed and Running** - Docker Engine (Linux) or Docker Desktop (Windows/macOS) is installed on the host system and the daemon is running at the time of execution.

2. **Docker API Version 1.41+** - The Docker daemon supports API version 1.41 or later (Docker 20.10+). Older versions may lack required features (seccomp, PID limits, etc.).

3. **User Has Docker Permissions** - The user running Acode is a member of the `docker` group (Linux) or has Docker Desktop running with appropriate permissions (Windows/macOS). No elevation required.

4. **Sufficient Disk Space for Images** - The host has at least 10GB free disk space for Docker images. Base images (dotnet SDK, node) range from 200MB to 2GB each.

5. **Sufficient Disk Space for Containers** - The host has at least 5GB free space for container filesystem layers and logs. Containers themselves use minimal space (10-50MB) but build outputs can be large.

6. **Linux Kernel with Namespace Support** - The host kernel supports required Linux namespaces (PID, network, mount, IPC, UTS, user). Kernel 4.4+ on Linux, WSL2 on Windows.

7. **cgroups v1 or v2 Available** - The system has cgroups (control groups) enabled for resource limiting. Most modern Linux distributions enable this by default.

8. **Overlay2 Storage Driver** - Docker is configured to use overlay2 storage driver (default on most systems). Other drivers (aufs, btrfs) may have different performance characteristics.

9. **No Conflicting Container Names** - Container names are generated with GUIDs (`acode-{guid}`), making conflicts extremely unlikely (< 1 in 10^36).

10. **Host Clock Synchronized** - The host system clock is reasonably accurate. Container timestamps and timeout calculations rely on system time.

### Operational Assumptions

11. **Repository Path is Accessible** - The repository being mounted exists on the local filesystem and is readable by the Docker daemon. No network mounts (NFS, SMB) are used for repository source.

12. **Repository Fits in Memory** - The repository size is reasonable (<10GB). While bind mounts don't copy data, extremely large repos (100GB+) may cause filesystem performance issues on macOS/Windows.

13. **Commands Complete Within Timeout** - Build and test commands complete within configured timeouts (default 5 minutes). Commands exceeding timeout are killed, which may leave incomplete outputs.

14. **Network Bandwidth for Image Pulls** - Adequate internet bandwidth exists for pulling Docker images (typically 200MB-2GB per image). Initial image pull may take 1-10 minutes.

15. **No Antivirus/EDR Interference** - Antivirus or Endpoint Detection and Response (EDR) software does not block Docker operations. On Windows, Defender may scan container filesystems, causing slowdowns.

16. **User Understands Docker Basics** - Users have basic Docker knowledge (images vs containers, volumes, networking). This is not a Docker tutorial; familiarity is assumed for troubleshooting.

17. **Cleanup Runs on Startup** - Orphaned containers from previous crashes are cleaned up on Acode startup. Users should not manually manage acode-labeled containers.

### Integration Assumptions

18. **Task 018 Command Executor Exists** - The structured command runner (Task 018) is implemented and provides the interface for delegating to ISandbox when sandbox mode is enabled.

19. **Task 001 Operating Modes Configured** - The OperatingMode enum (LocalOnly, Burst, Airgapped) is implemented and air-gapped mode correctly disables network at the policy level.

20. **Task 002 Configuration Loader Available** - The YAML configuration loader parses `.agent/config.yml` and provides SandboxConfiguration with validated settings (image names, resource limits, etc.).

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

## Security Considerations

This section provides detailed threat analysis and complete mitigation code for the five primary security risks in Docker sandbox implementation.

### Threat 1: Container Escape via Privileged Mode or Excessive Capabilities

**Risk Description:**
Running containers in privileged mode or with unnecessary Linux capabilities grants processes inside the container broad access to host resources. Privileged containers can load kernel modules, access all devices, and bypass most isolation mechanisms. Even non-privileged containers with capabilities like `CAP_SYS_ADMIN` can potentially escape to the host.

**Attack Scenario:**
An attacker who achieves code execution inside a privileged container can:
1. Mount the host's root filesystem: `mount /dev/sda1 /mnt/host`
2. Chroot into host filesystem: `chroot /mnt/host`
3. Execute arbitrary code on host with root privileges
4. Install backdoors, exfiltrate data, pivot to other systems

**Mitigation (Complete C# Code):**

```csharp
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Sandbox.Security;

/// <summary>
/// Enforces capability dropping and prevents privileged mode.
/// CRITICAL: This validator MUST be called before every container creation.
/// </summary>
public sealed class CapabilityEnforcer
{
    private readonly ILogger<CapabilityEnforcer> _logger;
    private readonly SandboxSecurityConfig _config;

    // Capabilities that should NEVER be granted
    private static readonly HashSet<string> BlacklistedCapabilities = new()
    {
        "CAP_SYS_ADMIN",      // Can mount filesystems, load modules
        "CAP_SYS_MODULE",     // Can load kernel modules
        "CAP_SYS_RAWIO",      // Can access /dev/mem, /dev/kmem
        "CAP_SYS_PTRACE",     // Can attach to arbitrary processes
        "CAP_SYS_BOOT",       // Can reboot the system
        "CAP_MAC_ADMIN",      // Can change SELinux/AppArmor policies
        "CAP_MAC_OVERRIDE",   // Can bypass SELinux/AppArmor
        "CAP_DAC_OVERRIDE",   // Can bypass file permission checks
        "CAP_DAC_READ_SEARCH" // Can bypass file read permission checks
    };

    public CapabilityEnforcer(
        ILogger<CapabilityEnforcer> logger,
        SandboxSecurityConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Validates that HostConfig does not grant excessive privileges.
    /// Throws SecurityPolicyViolationException if validation fails.
    /// </summary>
    public void ValidateAndEnforceCapabilities(HostConfig hostConfig, string containerId)
    {
        ArgumentNullException.ThrowIfNull(hostConfig);

        // CRITICAL CHECK 1: Privileged mode MUST be false
        if (hostConfig.Privileged)
        {
            var error = $"Container {containerId}: Privileged mode is FORBIDDEN. " +
                       "This would grant full host access. Set Privileged = false.";
            _logger.LogError(error);
            throw new SecurityPolicyViolationException(
                SecurityViolationCode.PrivilegedModeEnabled, error);
        }

        // CRITICAL CHECK 2: Drop ALL capabilities by default
        if (hostConfig.CapDrop == null || !hostConfig.CapDrop.Contains("ALL"))
        {
            _logger.LogWarning(
                "Container {ContainerId}: CapDrop does not include ALL. Enforcing.", containerId);
            hostConfig.CapDrop = new List<string> { "ALL" };
        }

        // CRITICAL CHECK 3: Validate any added capabilities
        if (hostConfig.CapAdd != null && hostConfig.CapAdd.Any())
        {
            var forbidden = hostConfig.CapAdd.Intersect(BlacklistedCapabilities).ToList();
            if (forbidden.Any())
            {
                var error = $"Container {containerId}: Blacklisted capabilities detected: " +
                           $"{string.Join(", ", forbidden)}. These MUST NOT be granted.";
                _logger.LogError(error);
                throw new SecurityPolicyViolationException(
                    SecurityViolationCode.BlacklistedCapability, error);
            }

            // Log approved capabilities (should be minimal or none)
            _logger.LogInformation(
                "Container {ContainerId}: Approved capabilities added: {Caps}",
                containerId, string.Join(", ", hostConfig.CapAdd));
        }

        // CRITICAL CHECK 4: NoNewPrivileges must be set
        if (hostConfig.SecurityOpt == null ||
            !hostConfig.SecurityOpt.Any(opt => opt == "no-new-privileges"))
        {
            _logger.LogWarning(
                "Container {ContainerId}: no-new-privileges not set. Enforcing.", containerId);
            hostConfig.SecurityOpt ??= new List<string>();
            hostConfig.SecurityOpt.Add("no-new-privileges");
        }

        _logger.LogInformation(
            "Container {ContainerId}: Capability enforcement passed. " +
            "Privileged=false, CapDrop=ALL, NoNewPrivileges=true", containerId);
    }
}

/// <summary>
/// Exception thrown when security policy is violated.
/// </summary>
public sealed class SecurityPolicyViolationException : Exception
{
    public SecurityViolationCode Code { get; }

    public SecurityPolicyViolationException(SecurityViolationCode code, string message)
        : base(message)
    {
        Code = code;
    }
}

public enum SecurityViolationCode
{
    PrivilegedModeEnabled,
    BlacklistedCapability,
    HostPathEscape,
    DockerSocketMounted,
    UnverifiedImage
}
```

---

### Threat 2: Host Path Traversal via Malicious Mount Paths

**Risk Description:**
If mount source paths are not validated, an attacker could specify paths like `/`, `/etc`, `/var/run/docker.sock`, or use path traversal sequences (`../`) to escape the intended mount restriction. This could expose sensitive host files (SSH keys, credentials, Docker socket) or allow modification of system configuration.

**Attack Scenario:**
Malicious repository contract (`.agent/config.yml`) specifies:
```yaml
sandbox:
  mounts:
    additional:
      - source: ../../../../etc
        target: /container-etc
        readonly: false
```
Without validation, this mounts `/etc` as writable, allowing attacker to:
1. Modify `/etc/passwd`, `/etc/shadow` (add root user)
2. Modify `/etc/crontab` (install backdoor)
3. Read `/etc/machine-id`, `/etc/ssh/ssh_host_*` keys

**Mitigation (Complete C# Code):**

```csharp
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Sandbox.Security;

/// <summary>
/// Validates mount paths to prevent path traversal and access to sensitive host directories.
/// </summary>
public sealed class MountPathValidator
{
    private readonly ILogger<MountPathValidator> _logger;
    private readonly string _repositoryRoot;

    // Sensitive paths that MUST NEVER be mounted (even read-only)
    private static readonly HashSet<string> ForbiddenPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",                    // Root filesystem
        "/etc",                 // System configuration
        "/var",                 // System state
        "/usr",                 // System binaries
        "/bin",                 // Critical binaries
        "/sbin",                // System binaries
        "/boot",                // Boot files
        "/dev",                 // Device files
        "/proc",                // Process information
        "/sys",                 // Kernel/system information
        "/run",                 // Runtime data
        "/var/run",             // Runtime sockets
        "/var/run/docker.sock", // Docker socket (CRITICAL)
        "/root",                // Root user home
        "/home",                // All user homes (too broad)
        "C:\\",                 // Windows root
        "C:\\Windows",          // Windows system
        "C:\\Program Files"     // Windows programs
    };

    public MountPathValidator(ILogger<MountPathValidator> logger, string repositoryRoot)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repositoryRoot = Path.GetFullPath(repositoryRoot ?? throw new ArgumentNullException(nameof(repositoryRoot)));
    }

    /// <summary>
    /// Validates and canonicalizes a mount source path.
    /// Returns the canonical path if valid, throws if invalid.
    /// </summary>
    public string ValidateAndCanonicalizePath(string sourcePath, bool isWorkspaceMount)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new SecurityPolicyViolationException(
                SecurityViolationCode.HostPathEscape,
                "Mount source path cannot be null or empty");
        }

        // Step 1: Resolve to absolute path
        string absolutePath;
        try
        {
            absolutePath = Path.IsPathRooted(sourcePath)
                ? Path.GetFullPath(sourcePath)
                : Path.GetFullPath(Path.Combine(_repositoryRoot, sourcePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve path: {Path}", sourcePath);
            throw new SecurityPolicyViolationException(
                SecurityViolationCode.HostPathEscape,
                $"Invalid path: {sourcePath}");
        }

        // Step 2: Check against forbidden paths
        var canonicalPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? absolutePath.Replace('/', '\\')
            : absolutePath.Replace('\\', '/');

        foreach (var forbidden in ForbiddenPaths)
        {
            var forbiddenCanonical = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? forbidden.Replace('/', '\\')
                : forbidden;

            if (canonicalPath.Equals(forbiddenCanonical, StringComparison.OrdinalIgnoreCase) ||
                canonicalPath.StartsWith(forbiddenCanonical + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                var error = $"FORBIDDEN: Cannot mount sensitive path '{sourcePath}' " +
                           $"(resolved to '{absolutePath}'). Path is in restricted list.";
                _logger.LogError(error);
                throw new SecurityPolicyViolationException(
                    SecurityViolationCode.HostPathEscape, error);
            }
        }

        // Step 3: Workspace mounts MUST be within repository root
        if (isWorkspaceMount)
        {
            if (!absolutePath.StartsWith(_repositoryRoot, StringComparison.OrdinalIgnoreCase))
            {
                var error = $"FORBIDDEN: Workspace mount '{sourcePath}' (resolved to '{absolutePath}') " +
                           $"is outside repository root '{_repositoryRoot}'";
                _logger.LogError(error);
                throw new SecurityPolicyViolationException(
                    SecurityViolationCode.HostPathEscape, error);
            }
        }

        // Step 4: Check for excessive parent directory traversals (potential obfuscation)
        var segments = sourcePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        var parentTraversals = segments.Count(s => s == "..");
        if (parentTraversals > 5)
        {
            _logger.LogWarning(
                "Path '{Path}' has {Count} parent traversals (suspicious)", sourcePath, parentTraversals);
        }

        // Step 5: Verify path exists (prevents typos leading to security issues)
        if (!Directory.Exists(absolutePath) && !File.Exists(absolutePath))
        {
            _logger.LogWarning("Path does not exist: {Path}", absolutePath);
            // Don't fail here - Docker will handle non-existent paths
            // But log for visibility
        }

        _logger.LogDebug(
            "Mount path validated: '{Source}' -> '{Canonical}'", sourcePath, canonicalPath);

        return canonicalPath;
    }

    /// <summary>
    /// Checks if a path points to the Docker socket (CRITICAL security check).
    /// </summary>
    public bool IsDockerSocket(string path)
    {
        var canonical = Path.GetFullPath(path);
        var dockerSockets = new[]
        {
            "/var/run/docker.sock",
            "/run/docker.sock",
            "\\\\.\\pipe\\docker_engine",
            "npipe:////./pipe/docker_engine"
        };

        return dockerSockets.Any(socket =>
            canonical.Equals(socket, StringComparison.OrdinalIgnoreCase));
    }
}
```

---

### Threat 3: Resource Exhaustion via Missing or Insufficient Limits

**Risk Description:**
Without proper resource limits, a container can consume unlimited CPU, memory, and PIDs, causing host system degradation or complete denial of service. A fork bomb can create thousands of processes. A memory leak can trigger host OOM killer, potentially killing critical system services.

**Attack Scenario:**
Malicious code executes inside container:
```bash
# Fork bomb
:(){ :|:& };:

# Memory bomb
while true; do
  malloc_bomb=$(cat /dev/zero | head -c 100M | base64)
done
```
Without limits, this exhausts host resources in seconds, affecting all other containers and host processes.

**Mitigation (Complete C# Code):**

```csharp
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Sandbox.Security;

/// <summary>
/// Enforces resource limits to prevent denial-of-service attacks.
/// </summary>
public sealed class ResourceLimitEnforcer
{
    private readonly ILogger<ResourceLimitEnforcer> _logger;
    private readonly SandboxConfiguration _config;

    // Absolute maximums (cannot be exceeded even by config)
    private const long MaxMemoryBytes = 8L * 1024 * 1024 * 1024; // 8GB
    private const long MaxCpuQuota = 400_000; // 4 cores
    private const long MaxPids = 2048;
    private const int MaxUlimitNoFile = 10000;

    public ResourceLimitEnforcer(
        ILogger<ResourceLimitEnforcer> logger,
        SandboxConfiguration config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Applies and validates resource limits on HostConfig.
    /// Clamps values to safe maximums if configured limits exceed safety thresholds.
    /// </summary>
    public void EnforceResourceLimits(HostConfig hostConfig, string containerId)
    {
        ArgumentNullException.ThrowIfNull(hostConfig);

        // Memory limit (CRITICAL: prevents OOM bomb)
        var requestedMemory = ParseMemoryString(_config.Defaults.Memory);
        var enforcedMemory = Math.Min(requestedMemory, MaxMemoryBytes);

        if (enforcedMemory < requestedMemory)
        {
            _logger.LogWarning(
                "Container {ContainerId}: Requested memory {Requested}MB exceeds maximum {Max}MB. Clamping.",
                containerId, requestedMemory / (1024 * 1024), MaxMemoryBytes / (1024 * 1024));
        }

        hostConfig.Memory = enforcedMemory;
        hostConfig.MemorySwap = 0; // Disable swap (prevents swap thrashing)
        hostConfig.MemorySwappiness = 0;
        hostConfig.OomKillDisable = false; // CRITICAL: Allow OOM kill

        _logger.LogInformation(
            "Container {ContainerId}: Memory limit set to {Memory}MB",
            containerId, enforcedMemory / (1024 * 1024));

        // CPU limit (CRITICAL: prevents CPU exhaustion)
        var requestedCpu = _config.Defaults.CpuLimit;
        var enforcedCpu = Math.Min(requestedCpu, MaxCpuQuota / 100_000.0);

        if (enforcedCpu < requestedCpu)
        {
            _logger.LogWarning(
                "Container {ContainerId}: Requested CPU {Requested} cores exceeds maximum {Max}. Clamping.",
                containerId, requestedCpu, enforcedCpu);
        }

        hostConfig.CPUQuota = (long)(enforcedCpu * 100_000);
        hostConfig.CPUPeriod = 100_000; // Standard 100ms period
        hostConfig.CPUShares = 1024; // Default weight

        _logger.LogInformation(
            "Container {ContainerId}: CPU limit set to {Cpu} cores",
            containerId, enforcedCpu);

        // PID limit (CRITICAL: prevents fork bomb)
        var requestedPids = _config.Defaults.PidsLimit;
        var enforcedPids = Math.Min(requestedPids, MaxPids);

        if (enforcedPids < requestedPids)
        {
            _logger.LogWarning(
                "Container {ContainerId}: Requested PID limit {Requested} exceeds maximum {Max}. Clamping.",
                containerId, requestedPids, MaxPids);
        }

        hostConfig.PidsLimit = enforcedPids;

        _logger.LogInformation(
            "Container {ContainerId}: PID limit set to {Pids}",
            containerId, enforcedPids);

        // Ulimits (file descriptors, processes)
        hostConfig.Ulimits = new List<Ulimit>
        {
            new Ulimit
            {
                Name = "nofile", // Max open files
                Soft = 1024,
                Hard = Math.Min(_config.Defaults.PidsLimit * 2, MaxUlimitNoFile)
            },
            new Ulimit
            {
                Name = "nproc", // Max processes (redundant with PidsLimit but good defense-in-depth)
                Soft = enforcedPids,
                Hard = enforcedPids
            }
        };

        _logger.LogInformation(
            "Container {ContainerId}: Resource limits enforced successfully", containerId);
    }

    private static long ParseMemoryString(string memory)
    {
        if (string.IsNullOrWhiteSpace(memory))
            return 512L * 1024 * 1024; // Default 512MB

        var trimmed = memory.Trim().ToUpperInvariant();
        var value = double.Parse(new string(trimmed.TakeWhile(char.IsDigit).ToArray()));

        if (trimmed.EndsWith("GB") || trimmed.EndsWith("G"))
            return (long)(value * 1024 * 1024 * 1024);
        if (trimmed.EndsWith("MB") || trimmed.EndsWith("M"))
            return (long)(value * 1024 * 1024);
        if (trimmed.EndsWith("KB") || trimmed.EndsWith("K"))
            return (long)(value * 1024);

        // Assume bytes if no unit
        return (long)value;
    }
}
```

---

### Threat 4: Docker Socket Exposure Leading to Full Host Compromise

**Risk Description:**
Mounting the Docker socket (`/var/run/docker.sock`) into a container is equivalent to granting root access to the host. Any process inside the container can create privileged containers, mount arbitrary host paths, execute commands on the host, and completely bypass all isolation.

**Attack Scenario:**
If Docker socket is mounted:
```yaml
mounts:
  - source: /var/run/docker.sock
    target: /var/run/docker.sock
```
Attacker inside container runs:
```bash
# Install Docker CLI inside container
apk add docker-cli

# Create privileged container with host root mounted
docker run -v /:/host --privileged alpine chroot /host

# Now has full root access to host
```

**Mitigation (Complete C# Code):**

```csharp
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Sandbox.Security;

/// <summary>
/// Prevents Docker socket exposure which would grant full host access.
/// </summary>
public sealed class DockerSocketGuard
{
    private readonly ILogger<DockerSocketGuard> _logger;
    private readonly MountPathValidator _pathValidator;

    // All variations of Docker socket path across platforms
    private static readonly HashSet<string> DockerSocketPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/var/run/docker.sock",
        "/run/docker.sock",
        "//./pipe/docker_engine",          // Windows named pipe
        "\\\\.\\pipe\\docker_engine",      // Windows named pipe (escaped)
        "npipe:////./pipe/docker_engine",  // Docker.DotNet Windows format
        "unix:///var/run/docker.sock",     // Docker.DotNet Unix format
        "tcp://localhost:2375",            // Unencrypted TCP (also dangerous)
        "tcp://127.0.0.1:2375"
    };

    public DockerSocketGuard(
        ILogger<DockerSocketGuard> logger,
        MountPathValidator pathValidator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pathValidator = pathValidator ?? throw new ArgumentNullException(nameof(pathValidator));
    }

    /// <summary>
    /// Scans all mounts for Docker socket and throws if found.
    /// CRITICAL: This check MUST pass before container creation.
    /// </summary>
    public void ValidateNoDockerSocketMounted(IList<string> binds, string containerId)
    {
        if (binds == null || !binds.Any())
            return;

        foreach (var bind in binds)
        {
            // Parse bind format: "source:target:mode" or "source:target"
            var parts = bind.Split(':');
            if (parts.Length < 2)
            {
                _logger.LogWarning(
                    "Container {ContainerId}: Invalid bind format: {Bind}", containerId, bind);
                continue;
            }

            var sourcePath = parts[0];

            // Check if source is Docker socket
            if (IsDockerSocketPath(sourcePath))
            {
                var error = $"CRITICAL SECURITY VIOLATION: Container {containerId} " +
                           $"attempts to mount Docker socket '{sourcePath}'. " +
                           "This would grant full host access and is STRICTLY FORBIDDEN.";
                _logger.LogCritical(error);
                throw new SecurityPolicyViolationException(
                    SecurityViolationCode.DockerSocketMounted, error);
            }

            // Also check canonical path (resolves symlinks)
            try
            {
                var canonical = _pathValidator.ValidateAndCanonicalizePath(sourcePath, false);
                if (IsDockerSocketPath(canonical))
                {
                    var error = $"CRITICAL SECURITY VIOLATION: Container {containerId} " +
                               $"attempts to mount path '{sourcePath}' which resolves to Docker socket '{canonical}'. " +
                               "Symlink or relative path to Docker socket is STRICTLY FORBIDDEN.";
                    _logger.LogCritical(error);
                    throw new SecurityPolicyViolationException(
                        SecurityViolationCode.DockerSocketMounted, error);
                }
            }
            catch (SecurityPolicyViolationException)
            {
                // Re-throw security violations
                throw;
            }
            catch (Exception ex)
            {
                // Path validation failed for other reason - log but don't fail
                // (Docker will reject invalid paths later)
                _logger.LogWarning(ex,
                    "Container {ContainerId}: Could not validate mount path: {Path}",
                    containerId, sourcePath);
            }
        }

        _logger.LogDebug(
            "Container {ContainerId}: Docker socket mount check passed ({Count} mounts scanned)",
            containerId, binds.Count);
    }

    private static bool IsDockerSocketPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // Normalize path for comparison
        var normalized = path.Replace('\\', '/').TrimEnd('/');

        return DockerSocketPaths.Any(socketPath =>
        {
            var normalizedSocket = socketPath.Replace('\\', '/').TrimEnd('/');
            return normalized.Equals(normalizedSocket, StringComparison.OrdinalIgnoreCase) ||
                   normalized.EndsWith(normalizedSocket, StringComparison.OrdinalIgnoreCase);
        });
    }
}
```

---

### Threat 5: Malicious Container Images (Supply Chain Attack)

**Risk Description:**
Container images pulled from registries can contain malware, backdoors, or vulnerable software. A compromised or malicious image could exfiltrate data, mine cryptocurrency, or establish persistence on the host. Even official-looking images can be typosquatted (e.g., `dotnet/sdk` vs `d0tnet/sdk`).

**Attack Scenario:**
User configures custom image:
```yaml
sandbox:
  images:
    dotnet: malicious-user/fake-dotnet-sdk:latest
```
Image contains:
1. Backdoored compiler that injects malware into built binaries
2. Cryptocurrency miner consuming CPU in background
3. Data exfiltration tool sending code to attacker's server

**Mitigation (Complete C# Code):**

```csharp
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Acode.Infrastructure.Sandbox.Security;

/// <summary>
/// Validates and verifies container images before use.
/// </summary>
public sealed class ImageVerifier
{
    private readonly IDockerClient _dockerClient;
    private readonly ILogger<ImageVerifier> _logger;
    private readonly SandboxConfiguration _config;

    // Trusted registries (configurable, defaults to official sources)
    private static readonly HashSet<string> TrustedRegistries = new(StringComparer.OrdinalIgnoreCase)
    {
        "mcr.microsoft.com",      // Microsoft Container Registry
        "docker.io",              // Docker Hub (official)
        "registry.hub.docker.com", // Docker Hub (full URL)
        "ghcr.io",                // GitHub Container Registry (if org-owned)
        "gcr.io"                  // Google Container Registry (if project-owned)
    };

    // Official image patterns (configurable)
    private static readonly Dictionary<string, string[]> OfficialImagePrefixes = new()
    {
        ["dotnet"] = new[] { "mcr.microsoft.com/dotnet/sdk", "mcr.microsoft.com/dotnet/runtime" },
        ["node"] = new[] { "docker.io/library/node", "node" }, // "node" = docker.io/library/node
        ["python"] = new[] { "docker.io/library/python", "python" },
        ["ubuntu"] = new[] { "docker.io/library/ubuntu", "ubuntu" }
    };

    public ImageVerifier(
        IDockerClient dockerClient,
        ILogger<ImageVerifier> logger,
        SandboxConfiguration config)
    {
        _dockerClient = dockerClient ?? throw new ArgumentNullException(nameof(dockerClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Verifies image before use. Checks:
    /// 1. Image comes from trusted registry
    /// 2. Image tag is not 'latest' (pinned version required in production)
    /// 3. Image exists locally or can be pulled
    /// 4. Image digest matches expected (if pinned)
    /// </summary>
    public async Task<ImageInspectResponse> VerifyImageAsync(
        string imageName,
        bool allowPull,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(imageName))
        {
            throw new ArgumentException("Image name cannot be null or empty", nameof(imageName));
        }

        // Parse image name into components
        var (registry, repository, tag, digest) = ParseImageName(imageName);

        // Check 1: Verify registry is trusted (unless explicitly allowed)
        if (!string.IsNullOrEmpty(registry) && !TrustedRegistries.Contains(registry))
        {
            if (!_config.Security.AllowUntrustedRegistries)
            {
                var error = $"Image '{imageName}' is from untrusted registry '{registry}'. " +
                           $"Trusted registries: {string.Join(", ", TrustedRegistries)}. " +
                           "Set sandbox.security.allow_untrusted_registries = true to override (NOT RECOMMENDED).";
                _logger.LogError(error);
                throw new SecurityPolicyViolationException(
                    SecurityViolationCode.UnverifiedImage, error);
            }
            else
            {
                _logger.LogWarning(
                    "SECURITY WARNING: Using image from untrusted registry: {Registry}", registry);
            }
        }

        // Check 2: Warn if using :latest tag (non-reproducible)
        if (tag == "latest")
        {
            _logger.LogWarning(
                "Image '{Image}' uses ':latest' tag which is non-reproducible. " +
                "Consider pinning to specific version (e.g., 'node:20.10.0') or digest.", imageName);
        }

        // Check 3: Verify image exists locally
        ImageInspectResponse? imageInfo = null;
        try
        {
            imageInfo = await _dockerClient.Images.InspectImageAsync(imageName, ct);
            _logger.LogInformation(
                "Image '{Image}' found locally. Digest: {Digest}",
                imageName, imageInfo.RepoDigests?.FirstOrDefault() ?? "unknown");
        }
        catch (DockerImageNotFoundException)
        {
            _logger.LogInformation("Image '{Image}' not found locally", imageName);

            if (!allowPull)
            {
                var error = $"Image '{imageName}' not found locally and pulling is disabled. " +
                           "Pre-pull image with 'docker pull {imageName}' or enable auto-pull.";
                _logger.LogError(error);
                throw new InvalidOperationException(error);
            }

            // Pull image
            _logger.LogInformation("Pulling image '{Image}'...", imageName);
            await PullImageWithProgressAsync(imageName, ct);

            // Inspect after pull
            imageInfo = await _dockerClient.Images.InspectImageAsync(imageName, ct);
        }

        // Check 4: If digest was specified, verify it matches
        if (!string.IsNullOrEmpty(digest))
        {
            var actualDigest = imageInfo.RepoDigests?.FirstOrDefault()?.Split('@').LastOrDefault();
            if (actualDigest != null && !actualDigest.Equals(digest, StringComparison.OrdinalIgnoreCase))
            {
                var error = $"Image '{imageName}' digest mismatch. " +
                           $"Expected: {digest}, Actual: {actualDigest}. " +
                           "This could indicate image tampering or registry compromise.";
                _logger.LogError(error);
                throw new SecurityPolicyViolationException(
                    SecurityViolationCode.UnverifiedImage, error);
            }
        }

        _logger.LogInformation(
            "Image verification passed: {Image} (Size: {Size}MB, Created: {Created})",
            imageName,
            imageInfo.Size / (1024.0 * 1024.0),
            imageInfo.Created);

        return imageInfo;
    }

    private static (string Registry, string Repository, string Tag, string Digest) ParseImageName(string imageName)
    {
        // Format: [registry/]repository[:tag][@digest]
        // Examples:
        //   node:20                        -> ("", "node", "20", "")
        //   mcr.microsoft.com/dotnet/sdk:8 -> ("mcr.microsoft.com", "dotnet/sdk", "8", "")
        //   ubuntu@sha256:abc123...        -> ("", "ubuntu", "", "sha256:abc123...")

        string registry = "";
        string repository = imageName;
        string tag = "latest";
        string digest = "";

        // Extract digest if present
        var digestSplit = repository.Split('@');
        if (digestSplit.Length == 2)
        {
            repository = digestSplit[0];
            digest = digestSplit[1];
        }

        // Extract tag if present
        var tagSplit = repository.Split(':');
        if (tagSplit.Length == 2)
        {
            repository = tagSplit[0];
            tag = tagSplit[1];
        }

        // Extract registry if present (contains '/')
        var registrySplit = repository.Split('/');
        if (registrySplit.Length > 1 && registrySplit[0].Contains('.'))
        {
            registry = registrySplit[0];
            repository = string.Join("/", registrySplit.Skip(1));
        }

        return (registry, repository, tag, digest);
    }

    private async Task PullImageWithProgressAsync(string imageName, CancellationToken ct)
    {
        var progress = new Progress<JSONMessage>(msg =>
        {
            if (!string.IsNullOrEmpty(msg.Status))
            {
                _logger.LogDebug("Pull progress: {Status} {Progress}", msg.Status, msg.ProgressMessage);
            }
        });

        await _dockerClient.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = imageName },
            null, // No auth for public images
            progress,
            ct);
    }
}
```

---

## Best Practices

### Container Management

1. **Use official base images** - Prefer official Docker Hub images
2. **Pin image versions** - Use specific tags, not :latest in production
3. **Clean up containers** - Remove containers after task completion
4. **Limit container resources** - Set memory and CPU limits

### Security

5. **Run as non-root** - Create unprivileged user in container
6. **Read-only root filesystem** - Mount specific writeable locations
7. **Drop capabilities** - Remove unnecessary Linux capabilities
8. **No privileged mode** - Never use --privileged flag

### Networking

9. **Network isolation by default** - No network unless explicitly needed
10. **Control outbound access** - Restrict to known endpoints if network enabled
11. **No port publishing** - Don't expose container ports to host
12. **Use bridge networks** - Isolate sandbox network from host

---

## Testing Requirements

**File:** `tests/Acode.Infrastructure.Tests/Sandbox/DockerSandboxTests.cs`

```csharp
using Acode.Domain.Execution;
using Acode.Infrastructure.Sandbox;
using Docker.DotNet;
using Docker.DotNet.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Acode.Infrastructure.Tests.Sandbox;

public sealed class DockerSandboxTests
{
    private readonly IDockerClient _mockDockerClient;
    private readonly DockerSandbox _sandbox;

    public DockerSandboxTests()
    {
        _mockDockerClient = Substitute.For<IDockerClient>();
        _sandbox = new DockerSandbox(_mockDockerClient, NullLogger<DockerSandbox>.Instance);
    }

    [Fact]
    public void Constructor_WithNullDockerClient_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new DockerSandbox(null!, NullLogger<DockerSandbox>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("dockerClient");
    }

    [Fact]
    public async Task IsAvailable_WhenDockerResponds_ReturnsTrue()
    {
        // Arrange
        _mockDockerClient.System.PingAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sandbox.IsAvailableAsync(CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        await _mockDockerClient.System.Received(1).PingAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsAvailable_WhenDockerUnreachable_ReturnsFalse()
    {
        // Arrange
        _mockDockerClient.System.PingAsync(Arg.Any<CancellationToken>())
            .Throws(new DockerApiException(System.Net.HttpStatusCode.ServiceUnavailable, "Docker daemon not responding"));

        // Act
        var result = await _sandbox.IsAvailableAsync(CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_WhenNotAvailable_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockDockerClient.System.PingAsync(Arg.Any<CancellationToken>())
            .Throws(new DockerApiException(System.Net.HttpStatusCode.ServiceUnavailable, "Docker unavailable"));

        var request = new SandboxRequest
        {
            Image = "alpine:latest",
            Command = new[] { "echo", "test" },
            WorkingDirectory = "/workspace"
        };

        // Act
        var act = async () => await _sandbox.RunAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Docker is not available*");
    }

    [Fact]
    public async Task RunAsync_CreatesContainerWithCorrectConfiguration()
    {
        // Arrange
        _mockDockerClient.System.PingAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var createResponse = new CreateContainerResponse { ID = "container-abc123" };
        _mockDockerClient.Containers.CreateContainerAsync(
            Arg.Any<CreateContainerParameters>(),
            Arg.Any<CancellationToken>())
            .Returns(createResponse);

        _mockDockerClient.Containers.StartContainerAsync(
            Arg.Any<string>(),
            Arg.Any<ContainerStartParameters>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        _mockDockerClient.Containers.WaitContainerAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new ContainerWaitResponse { StatusCode = 0 });

        _mockDockerClient.Containers.GetContainerLogsAsync(
            Arg.Any<string>(),
            Arg.Any<ContainerLogsParameters>(),
            Arg.Any<CancellationToken>())
            .Returns(new MultiplexedStream(Stream.Null));

        var request = new SandboxRequest
        {
            Image = "mcr.microsoft.com/dotnet/sdk:8.0",
            Command = new[] { "dotnet", "--version" },
            WorkingDirectory = "/workspace",
            EnvironmentVariables = new Dictionary<string, string> { ["HOME"] = "/tmp" }
        };

        // Act
        var result = await _sandbox.RunAsync(request, CancellationToken.None);

        // Assert
        await _mockDockerClient.Containers.Received(1).CreateContainerAsync(
            Arg.Is<CreateContainerParameters>(p =>
                p.Image == "mcr.microsoft.com/dotnet/sdk:8.0" &&
                p.WorkingDir == "/workspace" &&
                p.Cmd.SequenceEqual(new[] { "dotnet", "--version" }) &&
                p.Env.Contains("HOME=/tmp") &&
                p.HostConfig.NetworkMode == "none" &&
                p.HostConfig.AutoRemove == false &&
                p.Labels.ContainsKey("acode.managed")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ReturnsStdoutStderrAndExitCode()
    {
        // Arrange
        _mockDockerClient.System.PingAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var createResponse = new CreateContainerResponse { ID = "container-xyz789" };
        _mockDockerClient.Containers.CreateContainerAsync(Arg.Any<CreateContainerParameters>(), Arg.Any<CancellationToken>())
            .Returns(createResponse);

        _mockDockerClient.Containers.StartContainerAsync(Arg.Any<string>(), Arg.Any<ContainerStartParameters>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _mockDockerClient.Containers.WaitContainerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ContainerWaitResponse { StatusCode = 42 });

        var stdoutStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("stdout output"));
        var stderrStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("stderr output"));
        _mockDockerClient.Containers.GetContainerLogsAsync(Arg.Any<string>(), Arg.Is<ContainerLogsParameters>(p => p.ShowStdout), Arg.Any<CancellationToken>())
            .Returns(new MultiplexedStream(stdoutStream));
        _mockDockerClient.Containers.GetContainerLogsAsync(Arg.Any<string>(), Arg.Is<ContainerLogsParameters>(p => p.ShowStderr), Arg.Any<CancellationToken>())
            .Returns(new MultiplexedStream(stderrStream));

        var request = new SandboxRequest { Image = "alpine", Command = new[] { "test" } };

        // Act
        var result = await _sandbox.RunAsync(request, CancellationToken.None);

        // Assert
        result.ExitCode.Should().Be(42);
        result.Stdout.Should().Contain("stdout output");
        result.Stderr.Should().Contain("stderr output");
        result.ContainerId.Should().Be("container-xyz789");
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task RunAsync_RemovesContainerAfterExecution()
    {
        // Arrange
        _mockDockerClient.System.PingAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var containerId = "container-cleanup-test";
        _mockDockerClient.Containers.CreateContainerAsync(Arg.Any<CreateContainerParameters>(), Arg.Any<CancellationToken>())
            .Returns(new CreateContainerResponse { ID = containerId });
        _mockDockerClient.Containers.StartContainerAsync(Arg.Any<string>(), Arg.Any<ContainerStartParameters>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _mockDockerClient.Containers.WaitContainerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ContainerWaitResponse { StatusCode = 0 });
        _mockDockerClient.Containers.GetContainerLogsAsync(Arg.Any<string>(), Arg.Any<ContainerLogsParameters>(), Arg.Any<CancellationToken>())
            .Returns(new MultiplexedStream(Stream.Null));

        var request = new SandboxRequest { Image = "alpine", Command = new[] { "echo", "test" } };

        // Act
        await _sandbox.RunAsync(request, CancellationToken.None);

        // Assert
        await _mockDockerClient.Containers.Received(1).RemoveContainerAsync(
            containerId,
            Arg.Is<ContainerRemoveParameters>(p => p.Force == true && p.RemoveVolumes == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CleanupAsync_RemovesAllManagedContainers()
    {
        // Arrange
        var managedContainers = new List<ContainerListResponse>
        {
            new ContainerListResponse { ID = "container-1", Labels = new Dictionary<string, string> { ["acode.managed"] = "true" } },
            new ContainerListResponse { ID = "container-2", Labels = new Dictionary<string, string> { ["acode.managed"] = "true" } }
        };

        _mockDockerClient.Containers.ListContainersAsync(
            Arg.Is<ContainersListParameters>(p => p.All == true),
            Arg.Any<CancellationToken>())
            .Returns(managedContainers);

        // Act
        await _sandbox.CleanupAsync(CancellationToken.None);

        // Assert
        await _mockDockerClient.Containers.Received(1).RemoveContainerAsync("container-1", Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
        await _mockDockerClient.Containers.Received(1).RemoveContainerAsync("container-2", Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
    }
}
```

**File:** `tests/Acode.Infrastructure.Tests/Sandbox/MountManagerTests.cs`

```csharp
using Acode.Domain.Security;
using Acode.Infrastructure.Sandbox;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Sandbox;

public sealed class MountManagerTests
{
    private readonly string _repositoryRoot = "/home/user/repos/myproject";
    private readonly MountManager _mountManager;

    public MountManagerTests()
    {
        _mountManager = new MountManager(_repositoryRoot);
    }

    [Theory]
    [InlineData("src/Program.cs")]
    [InlineData("./docs/README.md")]
    [InlineData("tests/")]
    public void ValidatePath_AllowsWorkspaceSubpaths(string relativePath)
    {
        // Act
        var result = _mountManager.ValidatePath(relativePath, isWorkspaceMount: true);

        // Assert
        result.IsValid.Should().BeTrue();
        result.CanonicalPath.Should().StartWith(_repositoryRoot);
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\Windows\\System32")]
    [InlineData("src/../../outside")]
    public void ValidatePath_RejectsParentTraversal(string traversalPath)
    {
        // Act
        var result = _mountManager.ValidatePath(traversalPath, isWorkspaceMount: true);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violation.Should().Be(SecurityViolationCode.HostPathEscape);
        result.ErrorMessage.Should().Contain("outside repository root");
    }

    [Theory]
    [InlineData("/etc/passwd")]
    [InlineData("/var/run/docker.sock")]
    [InlineData("/root/.ssh/id_rsa")]
    [InlineData("C:\\Windows\\System32")]
    public void ValidatePath_RejectsSensitivePaths(string sensitivePath)
    {
        // Act
        var result = _mountManager.ValidatePath(sensitivePath, isWorkspaceMount: false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violation.Should().Be(SecurityViolationCode.SensitivePathAccess);
        result.ErrorMessage.Should().Contain("sensitive");
    }

    [Fact]
    public void CreateBind_CreatesReadOnlyBindByDefault()
    {
        // Act
        var bind = _mountManager.CreateBind("src", "/workspace/src");

        // Assert
        bind.Should().Be("/home/user/repos/myproject/src:/workspace/src:ro");
    }

    [Fact]
    public void CreateBind_SupportsReadWriteOption()
    {
        // Act
        var bind = _mountManager.CreateBind("output", "/workspace/output", readOnly: false);

        // Assert
        bind.Should().Be("/home/user/repos/myproject/output:/workspace/output:rw");
    }

    [Fact]
    public void CreateBind_HandlesSpacesInPaths()
    {
        // Arrange
        var mountManager = new MountManager("/home/user/my project");

        // Act
        var bind = mountManager.CreateBind("src folder", "/workspace/src");

        // Assert
        bind.Should().Contain("/home/user/my project/src folder");
    }

    [Fact]
    public void SensitivePaths_IncludesDockerSocket()
    {
        // Arrange
        var sensitivePaths = MountManager.GetSensitivePaths();

        // Assert
        sensitivePaths.Should().Contain(p =>
            p.Contains("/var/run/docker.sock") ||
            p.Contains("docker_engine"));
    }

    [Fact]
    public void SensitivePaths_IncludesCredentialStores()
    {
        // Arrange
        var sensitivePaths = MountManager.GetSensitivePaths();

        // Assert
        sensitivePaths.Should().Contain(p => p.Contains(".ssh") || p.Contains(".aws") || p.Contains(".kube"));
    }
}
```

**File:** `tests/Acode.Infrastructure.Tests/Sandbox/ResourceLimiterTests.cs`

```csharp
using Acode.Infrastructure.Sandbox;
using Docker.DotNet.Models;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Sandbox;

public sealed class ResourceLimiterTests
{
    [Fact]
    public void Configure_SetsCpuLimitCorrectly()
    {
        // Arrange
        var limiter = new ResourceLimiter();
        var hostConfig = new HostConfig();
        var limits = new ResourceLimits { CpuCores = 2.0 };

        // Act
        limiter.Configure(hostConfig, limits);

        // Assert
        hostConfig.CPUQuota.Should().Be(200_000); // 2.0 * 100_000
        hostConfig.CPUPeriod.Should().Be(100_000);
        hostConfig.NanoCPUs.Should().Be(2_000_000_000); // 2.0 * 1e9
    }

    [Fact]
    public void Configure_SetsMemoryLimitCorrectly()
    {
        // Arrange
        var limiter = new ResourceLimiter();
        var hostConfig = new HostConfig();
        var limits = new ResourceLimits { MemoryMB = 512 };

        // Act
        limiter.Configure(hostConfig, limits);

        // Assert
        hostConfig.Memory.Should().Be(512 * 1024 * 1024); // 512MB in bytes
        hostConfig.MemorySwap.Should().Be(0); // Swap disabled
        hostConfig.MemorySwappiness.Should().Be(0);
        hostConfig.OomKillDisable.Should().BeFalse();
    }

    [Fact]
    public void Configure_SetsPidsLimitCorrectly()
    {
        // Arrange
        var limiter = new ResourceLimiter();
        var hostConfig = new HostConfig();
        var limits = new ResourceLimits { MaxPids = 256 };

        // Act
        limiter.Configure(hostConfig, limits);

        // Assert
        hostConfig.PidsLimit.Should().Be(256);
    }

    [Fact]
    public void Configure_SetsUlimitsForNofileAndNproc()
    {
        // Arrange
        var limiter = new ResourceLimiter();
        var hostConfig = new HostConfig();
        var limits = new ResourceLimits { MaxOpenFiles = 1024, MaxPids = 128 };

        // Act
        limiter.Configure(hostConfig, limits);

        // Assert
        hostConfig.Ulimits.Should().ContainSingle(u => u.Name == "nofile" && u.Soft == 1024 && u.Hard == 10000);
        hostConfig.Ulimits.Should().ContainSingle(u => u.Name == "nproc" && u.Soft == 128 && u.Hard == 128);
    }

    [Fact]
    public void MergeOverrides_AppliesPerCommandLimits()
    {
        // Arrange
        var limiter = new ResourceLimiter();
        var baseLimits = new ResourceLimits { CpuCores = 1.0, MemoryMB = 512 };
        var overrides = new ResourceLimits { CpuCores = 4.0 }; // Override only CPU

        // Act
        var merged = limiter.MergeOverrides(baseLimits, overrides);

        // Assert
        merged.CpuCores.Should().Be(4.0); // Overridden
        merged.MemoryMB.Should().Be(512); // Preserved from base
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(-512)]
    public void Validate_RejectsNegativeLimits(double invalidValue)
    {
        // Arrange
        var limiter = new ResourceLimiter();
        var limits = new ResourceLimits { CpuCores = invalidValue };

        // Act
        var act = () => limiter.Validate(limits);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*negative*");
    }

    [Fact]
    public void Validate_RejectsZeroCpu()
    {
        // Arrange
        var limiter = new ResourceLimiter();
        var limits = new ResourceLimits { CpuCores = 0 };

        // Act
        var act = () => limiter.Validate(limits);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*CPU*greater than zero*");
    }

    [Theory]
    [InlineData(32 * 1024 * 1024)] // 32TB
    [InlineData(1)] // 1MB (too small)
    public void Validate_RejectsUnreasonableMemory(long unreasonableMemoryMB)
    {
        // Arrange
        var limiter = new ResourceLimiter();
        var limits = new ResourceLimits { MemoryMB = unreasonableMemoryMB };

        // Act
        var act = () => limiter.Validate(limits);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*memory*range*");
    }
}
```

### Integration Tests

**File:** `tests/Acode.Infrastructure.IntegrationTests/Sandbox/DockerSandboxIntegrationTests.cs`

```csharp
using Acode.Domain.Execution;
using Acode.Infrastructure.Sandbox;
using Docker.DotNet;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.IntegrationTests.Sandbox;

[Collection("Docker")]
public sealed class DockerSandboxIntegrationTests : IAsyncLifetime
{
    private readonly IDockerClient _dockerClient;
    private readonly DockerSandbox _sandbox;

    public DockerSandboxIntegrationTests()
    {
        _dockerClient = new DockerClientConfiguration().CreateClient();
        _sandbox = new DockerSandbox(_dockerClient, NullLogger<DockerSandbox>.Instance);
    }

    [Fact]
    public async Task RunsSimpleCommand_ReturnsCorrectOutput()
    {
        // Arrange
        var request = new SandboxRequest
        {
            Image = "alpine:latest",
            Command = new[] { "echo", "Hello from Docker" },
            WorkingDirectory = "/workspace"
        };

        // Act
        var result = await _sandbox.RunAsync(request, CancellationToken.None);

        // Assert
        result.ExitCode.Should().Be(0);
        result.Stdout.Trim().Should().Be("Hello from Docker");
        result.Stderr.Should().BeEmpty();
        result.Duration.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task NetworkDisabled_CannotReachInternet()
    {
        // Arrange
        var request = new SandboxRequest
        {
            Image = "alpine:latest",
            Command = new[] { "wget", "-T", "2", "https://example.com", "-O", "-" },
            WorkingDirectory = "/workspace",
            NetworkEnabled = false
        };

        // Act
        var result = await _sandbox.RunAsync(request, CancellationToken.None);

        // Assert
        result.ExitCode.Should().NotBe(0);
        result.Stderr.Should().Contain("Network");
    }

    [Fact]
    public async Task RespectsMemoryLimit_OOMKillsExcessiveProcess()
    {
        // Arrange
        var request = new SandboxRequest
        {
            Image = "alpine:latest",
            Command = new[] { "sh", "-c", "head -c 1G </dev/zero | tail" },
            WorkingDirectory = "/workspace",
            ResourceLimits = new ResourceLimits { MemoryMB = 64 }
        };

        // Act
        var result = await _sandbox.RunAsync(request, CancellationToken.None);

        // Assert
        result.ExitCode.Should().Be(137); // OOM killed
        result.WasOOMKilled.Should().BeTrue();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _sandbox.CleanupAsync(CancellationToken.None);
        _dockerClient.Dispose();
    }
}
```

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