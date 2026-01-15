# Task 005d: Ollama Lifecycle Management

**Priority:** P1 – High Priority
**Tier:** Infrastructure
**Complexity:** 8 (Fibonacci points)
**Phase:** Foundation + Operations
**Dependencies:** Task 005, Task 005a, Task 005c, Task 002

---

## Description

### Business Value

Task 005d transforms the Ollama integration from a manual, operator-intensive setup into an automated, self-managed system. Currently, users must manually:
1. Ensure Ollama is running (`ollama serve`)
2. Download required models (`ollama pull model-name`)
3. Monitor for crashes and restart manually
4. Debug cryptic failures when service state is inconsistent

This task eliminates user friction by automating service lifecycle management. When a user selects a model via configuration or CLI, Acode automatically:
- Detects if Ollama is running
- Starts Ollama if absent
- Pulls the requested model if missing
- Monitors Ollama health and restarts if crashed
- Reports clear, actionable status

**Value Propositions:**
1. **Zero-Config Developer Experience**: New users get working setup with `acode ask` without manual Ollama management
2. **Reduced Troubleshooting**: 80% of "it's not working" issues stem from Ollama not running or models missing—automated now
3. **Production Readiness**: Operators can configure Ollama once, system keeps it healthy thereafter
4. **CI/CD Integration**: Automated setup enables Acode in continuous integration pipelines
5. **Cross-Platform Consistency**: Same automated behavior on Windows (WSL), macOS, Linux
6. **Emergency Recovery**: Automatic restart on crashes prevents cascading failures
7. **Observability**: Clear status reporting lets users understand service state
8. **Graceful Degradation**: If Ollama crashes mid-request, automatic restart enables fast recovery

### Technical Approach

Ollama lifecycle management uses a **service orchestration layer** that sits between Acode and the Ollama HTTP API:

```
Acode CLI / Application
        ↓
OllamaServiceOrchestrator (NEW)
  ├── Detection (Is Ollama running?)
  ├── Startup (Start if missing)
  ├── Model Management (Pull if missing)
  ├── Health Monitoring (Periodic checks)
  └── Crash Recovery (Auto-restart)
        ↓
Ollama HTTP API (Task 005a)
```

The orchestrator operates in three modes determined by configuration:

1. **ManagedMode** (Default): Acode fully manages Ollama lifecycle
   - Auto-starts if missing
   - Auto-pulls models
   - Auto-restarts on crash
   - Stops on Acode shutdown (optional, configurable)

2. **MonitoredMode**: Acode monitors but doesn't start
   - Detects when Ollama stops
   - Reports status clearly
   - No auto-start (assumes external management like systemd)
   - Useful for production with service managers

3. **ExternalMode**: Acode assumes Ollama is always running
   - No lifecycle management
   - Connection failure is fatal
   - Minimal overhead (skip health checks)
   - For embedded deployments

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| OllamaProvider (Task 005a) | Decorator Pattern | Orchestrator wraps provider, enforces healthy state before requests |
| Configuration (Task 002) | Config Section | `providers.ollama.lifecycle` settings: mode, start_timeout, health_check_interval |
| CLI Commands | Implicit Management | `acode ask`, `acode code-review` implicitly trigger model availability checks |
| ProviderRegistry (Task 004c) | Lifecycle Hooks | Registry calls `EnsureHealthyAsync()` before returning provider instance |
| Error Handling (Task 005a) | Recovery Strategy | Connection errors trigger health check → auto-restart → retry logic |
| Logging | Structured Events | Log service state transitions, restarts, pull progress |
| Operating Modes (Task 001) | Constraints | Airgapped mode: no external model registry fetching, use local-only models |

### Failure Modes

| Failure | Detection | Impact | Recovery |
|---------|-----------|--------|----------|
| Ollama won't start (port taken, permission denied) | Process start timeout (30 sec) | User gets error "Cannot start Ollama" | User must free port or restart system; no silent failure |
| Model pull fails (network issue, invalid model name) | Ollama API error during `ollama pull` | User gets error "Model pull failed: [reason]" | Retry with `--force-pull` flag or manual `ollama pull model-name` |
| Ollama crashes mid-operation | Health check timeout (3 missed checks = crash detection) | In-flight request fails, then auto-restart triggers | Auto-restart within 10 seconds, request automatically retried (max 3 times) |
| GPU memory exhausted (OOM killer) | Process exit with memory error signal | Ollama killed, attempt to restart fails | User must reduce model size or add more VRAM; clear error message given |
| Model file corruption | Model load fails on startup | Health check fails, auto-restart loops | Detect loop after 3 restarts, pause auto-restart, report to user |
| Stale lock file preventing startup | Ollama refuses to start (lock file exists) | Process start fails, retry eventually succeeds | After 2 startup failures, attempt to remove lock file and retry |
| Network isolation in container | Cannot reach localhost:11434 | Initial health check fails permanently | Fall back to environment variables, try OLLAMA_HOST override |

### Assumptions

1. Ollama is installed at system PATH (`ollama` command available) or OLLAMA_HOME is set
2. Port 11434 is available for Ollama (or user configures alternate OLLAMA_HOST)
3. System has sufficient disk space for model download (~15 GB for llama3.2:latest)
4. System has sufficient RAM for model loading (8 GB minimum, more recommended)
5. User has permissions to create `.ollama/` directory in home directory
6. On Windows, WSL2 or equivalent container backend is available (Ollama doesn't run on Windows natively)
7. Ollama API is HTTP (not secured with TLS by default)
8. Model downloads are resumable if interrupted
9. Process supervision on Unix is via simple restart, not systemd integration (initially)
10. Airgapped mode users will pre-stage models, not rely on dynamic pulling
11. "Healthy" state means: process running + health endpoint responding + at least one model available
12. User tolerates 30-60 second startup delay for first Acode invocation (Ollama starting + model loading)

### Security Considerations

1. **Process Execution**: Starting Ollama via `Process.Start()` respects system PATH and OLLAMA_HOME, cannot be hijacked by malicious PATH entries (validation needed)
2. **File Permissions**: Model downloads written to `.ollama/models/`, must verify directory permissions are user-only (0700)
3. **Network Binding**: Ollama binds to localhost:11434 by default (not exposed externally), but verify if user configures OLLAMA_HOST with external IP
4. **Model Integrity**: Downloaded models are verified via SHA256 (Ollama handles this), Acode must not bypass validation
5. **Privilege Escalation**: Acode never escalates to root/admin to start Ollama (users must have PATH access to `ollama` binary)
6. **Resource Exhaustion**: Uncontrolled model pulling could fill disk. Implement quota checks (warn if <5 GB free space remains)
7. **Denial of Service**: Prevent rapid restart loops (max 3 restarts in 60 seconds) to avoid CPU/memory exhaustion
8. **Signal Handling**: On SIGTERM, gracefully shut down Ollama (don't kill immediately, allow cleanup)
9. **Credential Exposure**: No credentials for model registry initially (Ollama doesn't require auth), but prepare for future huggingface token support
10. **Airgapped Constraints**: Enforce that airgapped mode cannot trigger model pulls from external sources, must use pre-staged models

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Lifecycle Management | Automated control of service startup, shutdown, health monitoring, and recovery |
| Service Orchestrator | Component that manages Ollama process state and availability |
| Auto-Start | Automatic process startup when detected as not running |
| Model Pulling | Downloading a model from remote registry (e.g., ollama.ai) to local storage |
| Health Check | Periodic verification that service is running and responsive |
| Auto-Restart | Automatic process restart when crash is detected |
| Managed Mode | Orchestrator fully controls Ollama lifecycle (default) |
| Monitored Mode | Orchestrator watches but doesn't start Ollama |
| External Mode | Orchestrator assumes Ollama is externally managed |
| Process Supervisor | Component that monitors and restarts processes on failure |
| Service State | Current condition: Running, Starting, Stopping, Stopped, Failed, Crashed |
| Graceful Shutdown | Shutdown allowing cleanup: stop accepting requests, finish in-flight work, exit cleanly |
| Force Kill | Abrupt process termination, may leave resources locked |
| Model Registry | Remote source for downloading models (ollama.ai for public models) |
| Local Cache | Models stored in `.ollama/models/` directory on local disk |
| OLLAMA_HOME | Environment variable specifying Ollama data directory (default: `~/.ollama`) |
| OLLAMA_HOST | Environment variable specifying HTTP bind address (default: `localhost:11434`) |
| Lock File | File preventing multiple Ollama processes from starting simultaneously |

---

## Out of Scope

The following items are explicitly excluded from Task 005d:

- **Ollama Installation**: Users must install Ollama beforehand (handled by docs/setup, not automated)
- **Systemd/Service Manager Integration**: No systemd unit files or Windows service wrapper (use Managed Mode instead)
- **Model Auto-Selection**: Model selection is user responsibility via config; lifecycle only ensures available models are loaded
- **Multi-Instance Orchestration**: Managing multiple Ollama instances on different ports (single instance per Acode assumed)
- **GPU Driver Management**: No GPU driver installation or updates (user responsibility)
- **Container/Docker Orchestration**: Ollama in Docker assumed to be pre-started externally (lifecycle manages process, not containers)
- **Model Caching Strategy**: No intelligent cache management (keep all models, don't auto-delete)
- **Bandwidth Throttling**: Model pulls run at full network speed (user-configurable if critical)
- **Fallback Providers**: If Ollama fails, don't fall back to vLLM or other providers (that's provider selection, not lifecycle)
- **Upgrade Management**: Acode doesn't upgrade Ollama itself (users must do `ollama upgrade` manually)
- **Scheduled Maintenance**: No automatic model updates or defragmentation
- **Network Troubleshooting**: Focus on Acode's perspective; deep network diagnostics are user responsibility
- **Performance Tuning**: No automatic VRAM allocation or process priority adjustment

---

## Functional Requirements

### Service Orchestrator Interface

| ID | Requirement |
|----|-------------|
| FR-001 | OllamaServiceOrchestrator interface MUST define `EnsureHealthyAsync(CancellationToken)` method returning Task<ServiceState> |
| FR-002 | OllamaServiceOrchestrator interface MUST define `GetStateAsync()` method returning current ServiceState |
| FR-003 | OllamaServiceOrchestrator interface MUST define `StartAsync(CancellationToken)` method to manually start service |
| FR-004 | OllamaServiceOrchestrator interface MUST define `StopAsync(CancellationToken)` method to gracefully shut down |
| FR-005 | OllamaServiceOrchestrator interface MUST define `PullModelAsync(string modelName, CancellationToken)` method |
| FR-006 | Orchestrator MUST accept LifecycleMode (Managed, Monitored, External) at construction |
| FR-007 | Orchestrator MUST accept IOllamaProvider as dependency for HTTP operations |
| FR-008 | Orchestrator MUST accept ILogger for structured logging |
| FR-009 | Orchestrator MUST accept OllamaLifecycleOptions for configuration |

### Service State Management

| ID | Requirement |
|----|-------------|
| FR-010 | ServiceState enum MUST define: Running, Starting, Stopping, Stopped, Failed, Crashed, Unknown |
| FR-011 | `GetStateAsync()` MUST return Running only if: process exists AND health endpoint responds within 5 seconds |
| FR-012 | `GetStateAsync()` MUST return Crashed if: process ran but no longer exists (detected via PID check) |
| FR-013 | `GetStateAsync()` MUST return Failed if: process exists but health endpoint unreachable for >3 consecutive checks |
| FR-014 | `GetStateAsync()` MUST return Stopped if: process doesn't exist and wasn't recently running |
| FR-015 | State transitions MUST be logged with timestamp, old state, new state, and reason |

### Auto-Start Functionality

| ID | Requirement |
|----|-------------|
| FR-016 | In Managed Mode, `EnsureHealthyAsync()` MUST call `StartAsync()` if state is Stopped or Crashed |
| FR-017 | `StartAsync()` MUST start Ollama process via `ollama serve` command |
| FR-018 | `StartAsync()` MUST wait up to 30 seconds for process to start and bind to port |
| FR-019 | `StartAsync()` MUST return failure if process starts but health check still fails after 30 seconds |
| FR-020 | `StartAsync()` MUST detect if port is already in use and report specific error |
| FR-021 | `StartAsync()` MUST detect if `ollama` binary not found in PATH and report specific error with guidance |
| FR-022 | In Monitored Mode, `EnsureHealthyAsync()` MUST NOT auto-start, only report state |
| FR-023 | In External Mode, `EnsureHealthyAsync()` MUST skip health checks entirely, assume Running |
| FR-024 | Startup process MUST be logged with PID, command line, and start timestamp |

### Model Pulling

| ID | Requirement |
|----|-------------|
| FR-025 | `EnsureHealthyAsync()` MUST ensure configured model is available locally before returning Success |
| FR-026 | If configured model not available, MUST call `PullModelAsync(modelName, cancellationToken)` automatically |
| FR-027 | `PullModelAsync()` MUST use Ollama API `POST /api/pull` endpoint to pull model |
| FR-028 | Pull progress MUST be streamed and logged at Debug level with percentage completion |
| FR-029 | Pull operation MUST respect configured timeout (default: 5 minutes for models < 10 GB) |
| FR-030 | Pull timeout MUST be configurable per model via `model_pull_timeout_minutes` setting |
| FR-031 | If pull fails due to network error, MUST retry up to 3 times with exponential backoff |
| FR-032 | If pull fails after 3 retries, MUST report error and not fail service health (pull attempted but unavailable) |
| FR-033 | Pull MUST detect and report if model name is invalid (Ollama API 404 error) |
| FR-034 | Pull MUST detect and report if insufficient disk space (<5 GB free) before attempting pull |
| FR-035 | In Airgapped Mode, pull operations MUST be rejected with error: "Cannot pull models in airgapped mode" |
| FR-036 | Pull operations MUST log model name, size, download time, and completion status |

### Health Monitoring

| ID | Requirement |
|----|-------------|
| FR-037 | Orchestrator MUST perform periodic health checks at configured interval (default: 60 seconds) |
| FR-038 | Health check interval MUST be configurable via `health_check_interval_seconds` |
| FR-039 | Health check MUST call `/api/tags` endpoint to verify connectivity and model availability |
| FR-040 | Health check MUST timeout after 5 seconds (fail fast if unresponsive) |
| FR-041 | Three consecutive failed health checks MUST trigger state transition to Failed |
| FR-042 | Successful health check MUST reset failure counter to 0 |
| FR-043 | Health check failures MUST be logged at Warn level with endpoint, error, and timestamp |
| FR-044 | Health check successes MUST NOT be logged (too noisy) but available at Debug level if requested |
| FR-045 | Health check MUST detect if Ollama process exited unexpectedly (process no longer exists) and report Crashed state |

### Crash Detection and Recovery

| ID | Requirement |
|----|-------------|
| FR-046 | If ServiceState becomes Crashed, MUST NOT immediately auto-restart (wait for explicit `EnsureHealthyAsync()` call) |
| FR-047 | Auto-restart MUST only occur if: (a) Managed Mode enabled, (b) crash detected, (c) restart limit not exceeded |
| FR-048 | Restart limit MUST be: max 3 restarts per 60-second window (prevent restart loops) |
| FR-049 | If restart limit exceeded, MUST stop attempting restart and set state to Failed, logging critical error |
| FR-050 | Each restart attempt MUST log: attempt number, time since last crash, command, and result |
| FR-051 | Automatic restart MUST wait minimum 2 seconds before retry (don't thrash) |
| FR-052 | Manual `StartAsync()` call MUST reset restart counter, allowing fresh restart attempts |

### Graceful Shutdown

| ID | Requirement |
|----|-------------|
| FR-053 | `StopAsync()` MUST send SIGTERM signal to Ollama process (Unix) or equivalent (Windows) |
| FR-054 | `StopAsync()` MUST wait up to 10 seconds for graceful shutdown before force-killing |
| FR-055 | `StopAsync()` MUST force-kill with SIGKILL if process doesn't exit within 10 seconds |
| FR-056 | `StopAsync()` MUST log shutdown: old state, method (graceful/forced), duration, result |
| FR-057 | In Managed Mode, `StopAsync()` MUST be called automatically on Acode shutdown (configurable via `stop_on_exit` setting) |
| FR-058 | `stop_on_exit` MUST default to false (don't stop Ollama on Acode exit, keep it running for other clients) |
| FR-059 | If `stop_on_exit` true, Acode MUST ensure Ollama is stopped before process exits (register with app lifecycle) |

### Configuration

| ID | Requirement |
|----|-------------|
| FR-060 | OllamaLifecycleOptions MUST include: Mode (Managed/Monitored/External), StartTimeout, HealthCheckInterval, MaxRestarts |
| FR-061 | Configuration MUST load from `providers.ollama.lifecycle` section in .agent/config.yml |
| FR-062 | Configuration MUST support environment variable overrides: ACODE_OLLAMA_LIFECYCLE_MODE, ACODE_OLLAMA_START_TIMEOUT_SECONDS |
| FR-063 | Default configuration MUST be: Mode=Managed, StartTimeout=30s, HealthCheckInterval=60s, MaxRestarts=3, StopOnExit=false |
| FR-064 | Model name to pull MUST be configurable via: (a) config file `model` setting, (b) CLI `--model` flag |
| FR-065 | If model not configured, MUST default to `llama3.2:latest` |
| FR-066 | Configuration MUST be validated at startup (invalid mode, negative timeouts, etc.) with clear error messages |

### Integration with Provider Registry

| ID | Requirement |
|----|-------------|
| FR-067 | ProviderRegistry.GetProviderAsync() MUST call orchestrator.EnsureHealthyAsync() before returning OllamaProvider |
| FR-068 | If EnsureHealthyAsync() fails, MUST return error with status and guidance (not silent failure) |
| FR-069 | Orchestrator lifecycle MUST be transparent to provider clients (orchestrator wraps provider) |
| FR-070 | Provider requests MUST NOT fail due to lifecycle state changes (health checks run in background) |
| FR-071 | If Ollama crashes mid-request, error handling MUST trigger auto-restart and request retry (max 3 retries) |

### Error Reporting

| ID | Requirement |
|----|-------------|
| FR-072 | All lifecycle failures MUST produce clear, actionable error messages with root cause |
| FR-073 | Error messages MUST include specific guidance, not generic descriptions |
| FR-074 | Error message for "port in use" MUST show: which port, command to find process using it, suggestion to change OLLAMA_HOST |
| FR-075 | Error message for "model pull failed" MUST show: model name, downloaded size so far, network error if applicable |
| FR-076 | Error message for "Ollama won't start" MUST show: timeout occurred, suggestion to check logs, manual start command |
| FR-077 | Error message for "restart limit exceeded" MUST show: restart history, suggestion to check Ollama logs, restart attempts |
| FR-078 | Errors MUST be logged to structured logs with LogLevel.Error or higher |

### Status Reporting

| ID | Requirement |
|----|-------------|
| FR-079 | `GetStateAsync()` MUST return complete status including: state, PID, uptime, last health check time, model list |
| FR-080 | Status MUST be consumable by CLI for `acode status` command to display |
| FR-081 | Status MUST be cacheable (don't poll on every request, use 5-second cache minimum) |
| FR-082 | Status report MUST include timestamp for cache expiration detection |

---

## Non-Functional Requirements

### Performance

| ID | Requirement | Target | Maximum |
|----|-------------|--------|---------|
| NFR-001 | EnsureHealthyAsync() latency when already running | <50ms | 200ms |
| NFR-002 | EnsureHealthyAsync() latency when needs restart | 2-5s | 30s |
| NFR-003 | GetStateAsync() latency | <100ms | 500ms |
| NFR-004 | Health check latency (HTTP call to /api/tags) | <100ms | 5s (timeout) |
| NFR-005 | PullModelAsync() throughput for 7GB model | >50 Mbps | (network dependent) |
| NFR-006 | Memory overhead of orchestrator | <20 MB | 50 MB |
| NFR-007 | CPU usage in idle (no requests) | <1% | 5% |
| NFR-008 | Background health check overhead | <5% CPU | 10% CPU |
| NFR-009 | Model pull memory overhead | <50 MB | 200 MB |

### Reliability

| ID | Requirement | Target | Maximum |
|----|-------------|--------|---------|
| NFR-010 | Service restart success rate | >99% | (aim for 100%) |
| NFR-011 | Health check accuracy (true positive rate for detecting crashes) | >95% | <1 minute detection lag |
| NFR-012 | Model pull resumability after network interruption | >90% | retry within 60 seconds |
| NFR-013 | Process startup success rate on first attempt | >95% | after 3 retries: 99% |
| NFR-014 | Graceful shutdown success rate (within 10 seconds) | >99% | force-kill within 15 seconds |
| NFR-015 | No orphaned Ollama processes after Acode exit | 100% | automatic cleanup |

### Observability

| ID | Requirement | Target | Maximum |
|----|-------------|--------|---------|
| NFR-016 | All state transitions logged with structured fields | 100% | no missing transitions |
| NFR-017 | All errors logged with LogLevel.Error or higher | 100% | no silent failures |
| NFR-018 | Health check results logged (failures at Warn, successes at Debug) | 100% | <5KB per day |
| NFR-019 | Model pull progress logged in real-time | 100% | updates every 5-10 seconds |
| NFR-020 | Restart history retained for diagnostics | 10 restarts minimum | previous 24 hours |

### Maintainability

| ID | Requirement | Target | Maximum |
|----|-------------|--------|---------|
| NFR-021 | Code coverage for OllamaServiceOrchestrator | >90% | not applicable |
| NFR-022 | Interface complexity (cyclomatic complexity per method) | <10 | 20 maximum |
| NFR-023 | Dependency count for orchestrator | <5 | 10 maximum |
| NFR-024 | Test-to-code ratio | >1.0 | not applicable |

### Compatibility

| ID | Requirement | Target | Maximum |
|----|-------------|--------|---------|
| NFR-025 | Ollama version compatibility | 0.1.23+ | backwards 12 months |
| NFR-026 | Operating system support | Windows (WSL), macOS, Linux | 95% of modern systems |
| NFR-027 | .NET version requirement | .NET 8.0+ | no .NET 6 or earlier |

---

## User Manual Documentation

### Overview

The Ollama Lifecycle Manager automatically handles starting Ollama, downloading models, monitoring health, and recovering from crashes. Once configured, Acode manages Ollama in the background—users just select a model and get to work.

For most users, no manual configuration is required. Set your desired model in `.agent/config.yml` and Acode takes care of the rest:
- Starts Ollama if not running
- Downloads the model if missing
- Monitors health in the background
- Restarts automatically if it crashes

### Quick Start

**Default configuration (no setup required):**
```bash
# Just run Acode—it handles everything
acode ask "write a hello world program"

# Acode will:
# 1. Check if Ollama is running
# 2. If not, start it (takes ~10 seconds)
# 3. Check if llama3.2:latest is downloaded
# 4. If not, pull it (takes ~5-15 minutes first time)
# 5. Run your request
```

**Monitor service status:**
```bash
acode providers status ollama
# Output:
# Service: Running (PID 12345)
# Uptime: 2 hours 34 minutes
# Model: llama3.2:latest (7.2 GB)
# Last health check: 45 seconds ago (healthy)
```

### Configuration

Configure lifecycle behavior in `.agent/config.yml`:

```yaml
providers:
  ollama:
    lifecycle:
      # Lifecycle mode: "managed" (default), "monitored", or "external"
      # managed: Acode fully controls Ollama (recommended)
      # monitored: Acode watches but doesn't start (assumes systemd/external management)
      # external: Acode assumes Ollama is always running (minimal overhead)
      mode: managed

      # Timeout for Ollama startup (seconds)
      start_timeout_seconds: 30

      # Health check interval (seconds)
      health_check_interval_seconds: 60

      # Max consecutive restart attempts per minute (prevents restart loops)
      max_restarts_per_minute: 3

      # Stop Ollama when Acode exits
      # Set to true if Ollama should be exclusive to Acode
      # Set to false (default) if other apps also use Ollama
      stop_on_exit: false

      # Model pull timeout (minutes) for downloading models
      model_pull_timeout_minutes: 5
```

**Environment variable overrides:**
```bash
# Override mode via environment
export ACODE_OLLAMA_LIFECYCLE_MODE=monitored
acode ask "hello"

# Override model pull timeout
export ACODE_OLLAMA_MODEL_PULL_TIMEOUT_MINUTES=10
acode ask "hello"
```

### CLI Commands

```bash
# Check Ollama service status
acode providers status ollama
# Output: Running, PID, uptime, model, health check status

# Manually start Ollama
acode providers start ollama
# Output: Starting Ollama... [done]
# PID: 12345, listening on http://localhost:11434

# Manually stop Ollama
acode providers stop ollama
# Output: Stopping Ollama... [done]
# Process exited cleanly

# Manually pull a model (if not auto-pulling)
acode providers pull-model ollama --model "llama3.2:8b"
# Output: Pulling model...
# Downloaded 7.2 GB [████████████░░░░░░░░░░░░░░░░░░░░] 45%
# [done] 4m 32s

# View lifecycle history (crashes, restarts)
acode providers history ollama
# Output:
# 2026-01-13 14:22:01 Started (manual)
# 2026-01-13 14:22:45 Pulled llama3.2:latest (7.2 GB)
# 2026-01-13 14:24:30 Health check passed
# 2026-01-13 14:25:47 Detected crash (auto-restarting)
# 2026-01-13 14:25:50 Started (auto-restart)

# Clear restart history (after fixing underlying issue)
acode providers reset-restarts ollama
# Output: Reset restart counter
```

### Best Practices

1. **Let It Auto-Start**: Don't manually `ollama serve` in another terminal. Let Acode manage it. (Exception: shared Ollama server accessible to multiple tools)

2. **Pre-Stage Models in Shared Environments**: If multiple tools share Ollama, pre-pull large models in advance so Acode doesn't block on download: `ollama pull llama3.2:latest && acode ...`

3. **Monitor Memory Usage**: Large models (13B+) need 16+ GB RAM. Monitor with `acode providers history ollama` to catch OOM crashes.

4. **Use Monitored Mode for Production**: For servers with systemd managing Ollama, set mode to `monitored` so Acode doesn't try to restart if systemd is handling it.

5. **Check Logs After Crashes**: If Ollama keeps crashing, check logs: `cat ~/.ollama/logs/` (if available) or `journalctl -u ollama` (if systemd-managed).

6. **Disk Space**: Large models need 30+ GB disk space. Acode will warn if <5 GB free and refuse to pull.

7. **Network Resilience**: Model pulls automatically retry 3 times. For flaky networks, increase `model_pull_timeout_minutes`.

8. **CI/CD**: Set mode to `managed` so CI jobs don't require Ollama pre-installed. Acode will set it up.

### Troubleshooting

**Problem: "Cannot start Ollama - process timeout after 30 seconds"**
- **Causes:**
  1. `ollama` binary not in PATH
  2. Port 11434 already in use by another process
  3. Insufficient permissions to create `~/.ollama` directory
  4. System resources too low (RAM, disk)

- **Solutions:**
  1. Verify Ollama installed: `ollama --version`
  2. Check if port in use: `lsof -i :11434` (Unix) or `netstat -ano | findstr :11434` (Windows)
  3. Free port: `kill $(lsof -ti :11434)` or configure `OLLAMA_HOST=localhost:11435`
  4. Check disk space: `df -h` (need >5 GB free)
  5. Increase timeout: `start_timeout_seconds: 60` in config

- **Example Fix:**
  ```bash
  # Port in use? Kill it and restart
  lsof -ti :11434 | xargs kill -9
  acode providers restart ollama
  ```

**Problem: "Model pull timeout after 5 minutes"**
- **Causes:**
  1. Network too slow (model is large, needs more time)
  2. Network interrupted during download
  3. Model name invalid
  4. Insufficient disk space (fills up during download)

- **Solutions:**
  1. Increase timeout: `model_pull_timeout_minutes: 15` for slow connections
  2. Check network: `ping ollama.ai` or try manual pull: `ollama pull llama3.2:latest`
  3. Verify model name: `ollama list` to see available models
  4. Free disk space: `du -sh ~/.ollama/models/` to check usage

- **Example Fix:**
  ```bash
  # Increase timeout and retry
  acode providers pull-model ollama --model "llama3.2:8b" --timeout 20
  ```

**Problem: "Ollama keeps crashing (restart limit exceeded)"**
- **Causes:**
  1. Out of Memory (OOM killer terminating process)
  2. Corrupted model file (won't load)
  3. Incompatible GPU driver
  4. Disk I/O errors

- **Solutions:**
  1. Check free memory: `free -h` (need >8 GB for llama3.2:latest)
  2. Try smaller model: `model: "llama3.2:1b"` in config
  3. Check GPU: `ollama list` should show models
  4. Check logs: `dmesg | grep -i ollama` for kernel messages
  5. Verify disk: `fsck` (if suspected I/O errors)

- **Example Fix:**
  ```bash
  # Switch to smaller model
  echo "model: llama3.2:1b" >> .agent/config.yml
  # Reset restart counter
  acode providers reset-restarts ollama
  # Retry
  acode ask "hello"
  ```

**Problem: "Ollama running but health check fails (disconnected from service)"**
- **Causes:**
  1. Firewall blocking localhost:11434
  2. Ollama listening on different port (OLLAMA_HOST misconfigured)
  3. Network namespace issue (Docker/WSL isolation)
  4. Socket file permissions

- **Solutions:**
  1. Check if listening: `curl http://localhost:11434/api/tags`
  2. Check OLLAMA_HOST: `echo $OLLAMA_HOST`
  3. If using Docker: ensure port mapping and network mode correct
  4. If using WSL: verify localhost accessible from Windows (usually OK)

- **Example Fix:**
  ```bash
  # Verify connectivity
  curl http://localhost:11434/api/tags
  # If times out, check firewall or port
  ```

---

## Acceptance Criteria

### Auto-Start Functionality

- [ ] AC-001: When Managed Mode enabled and Ollama not running, EnsureHealthyAsync() starts process automatically
- [ ] AC-002: Startup waits up to configured timeout (default 30s) for process to bind to port
- [ ] AC-003: If startup times out, clear error message reported with port and troubleshooting guidance
- [ ] AC-004: If `ollama` binary not in PATH, clear error with `ollama --version` command to verify install
- [ ] AC-005: If port already in use, clear error showing which process is using port
- [ ] AC-006: Startup is logged with PID, command line, start time, and result

### Model Pulling

- [ ] AC-007: When configured model missing, EnsureHealthyAsync() automatically pulls model
- [ ] AC-008: Pull progress streamed and logged at Debug level with percentage
- [ ] AC-009: Pull respects timeout (default 5 minutes, configurable)
- [ ] AC-010: Pull retries up to 3 times on network errors with exponential backoff
- [ ] AC-011: If pull fails after retries, error reported but health check doesn't fail (degraded state)
- [ ] AC-012: Invalid model names detected (404 from Ollama API) with clear error
- [ ] AC-013: Insufficient disk space detected (>5 GB free required) with clear error before attempting pull
- [ ] AC-014: In Airgapped Mode, pull operations rejected with "Cannot pull in airgapped mode" message
- [ ] AC-015: Pull operations logged with model name, size, duration, and completion status

### Health Monitoring

- [ ] AC-016: Periodic health checks run at configured interval (default 60s)
- [ ] AC-017: Health check calls /api/tags endpoint with 5-second timeout
- [ ] AC-018: Three consecutive failed checks transition to Failed state
- [ ] AC-019: Successful check resets failure counter
- [ ] AC-020: Health check failures logged at Warn level with error details
- [ ] AC-021: Crash detected when process exits unexpectedly (PID check)
- [ ] AC-022: Crash sets state to Crashed and logs critical error

### Crash Recovery

- [ ] AC-023: In Managed Mode, crashed process automatically restarts
- [ ] AC-024: Restart limit prevents loops (max 3 per 60 seconds)
- [ ] AC-025: Restart limit exceeded stops attempts and sets state to Failed
- [ ] AC-026: Each restart logged with attempt number, time since crash, result
- [ ] AC-027: Minimum 2-second delay between restart attempts (no thrashing)
- [ ] AC-028: Manual StartAsync() call resets restart counter

### Graceful Shutdown

- [ ] AC-029: StopAsync() sends SIGTERM to process (Unix/Linux)
- [ ] AC-030: StopAsync() waits up to 10 seconds for graceful exit
- [ ] AC-031: Force-kill applied after 10 seconds if process doesn't exit
- [ ] AC-032: Shutdown logged with method (graceful/forced), duration, result
- [ ] AC-033: In Managed Mode with stop_on_exit=true, Acode stops Ollama on shutdown

### Configuration

- [ ] AC-034: Configuration loads from `providers.ollama.lifecycle` section in .agent/config.yml
- [ ] AC-035: Environment variables override config file settings
- [ ] AC-036: Invalid configuration detected at startup with clear error messages
- [ ] AC-037: Default configuration applies if section missing from config.yml
- [ ] AC-038: Mode parameter accepts "managed", "monitored", "external" (case-insensitive)
- [ ] AC-039: Timeouts accept positive integers, reject negative/zero values
- [ ] AC-040: Max restarts parameter rejects values <1 or >10

### Status Reporting

- [ ] AC-041: GetStateAsync() returns state, PID, uptime, model list, last health check time
- [ ] AC-042: Status formatted for CLI consumption by `acode providers status` command
- [ ] AC-043: Status cached (5-second minimum) to avoid polling overhead
- [ ] AC-044: Cache includes expiration timestamp for staleness detection
- [ ] AC-045: Status includes health check result (Healthy/Degraded/Unhealthy)
- [ ] AC-046: Status includes restart history (last 10 restarts)

### Error Handling

- [ ] AC-047: All errors include root cause (not generic descriptions)
- [ ] AC-048: Error messages include actionable remediation steps
- [ ] AC-049: Port-in-use error shows command to find process using port
- [ ] AC-050: Model-pull-failed error shows downloaded size, network error if applicable
- [ ] AC-051: Startup-failed error suggests checking logs and manual start command
- [ ] AC-052: Restart-limit-exceeded error shows restart history and suggestions
- [ ] AC-053: All errors logged to structured logs at Error level or higher
- [ ] AC-054: No silent failures—all issues reported to user

### Integration

- [ ] AC-055: ProviderRegistry calls EnsureHealthyAsync() before returning provider
- [ ] AC-056: Provider requests fail with clear message if EnsureHealthyAsync() fails
- [ ] AC-057: Provider fails are not silent (status and guidance provided)
- [ ] AC-058: Connection errors trigger health check and auto-restart
- [ ] AC-059: Requests automatically retried (max 3) after auto-restart
- [ ] AC-060: Orchestrator wrapper is transparent to provider clients

### Mode-Specific Behavior

- [ ] AC-061: Managed Mode: auto-starts, auto-pulls, auto-restarts (all enabled)
- [ ] AC-062: Monitored Mode: watches but doesn't start
- [ ] AC-063: External Mode: skips health checks, assumes Running
- [ ] AC-064: Mode can be changed via config without restart
- [ ] AC-065: Mode mismatch with actual Ollama state handled gracefully

### Logging

- [ ] AC-066: All state transitions logged with structured fields (old state, new state, reason, timestamp)
- [ ] AC-067: All lifecycle operations logged at appropriate level (Debug/Info/Warn/Error)
- [ ] AC-068: Health check failures logged at Warn (not Error)
- [ ] AC-069: Health check successes not logged (Debug level if enabled)
- [ ] AC-070: Model pull progress logged every 10-20% with size, speed, ETA
- [ ] AC-071: Restart history retained for at least 24 hours
- [ ] AC-072: Logs parseable by log aggregation tools (structured logging format)

---

## User Verification Steps

### Scenario 1: First-Time Setup with Auto-Start

**Objective:** Verify that Acode automatically starts Ollama and pulls model on first use

**Preconditions:**
- Ollama installed but not currently running
- `.agent/config.yml` has Managed Mode enabled (or default)
- Network connectivity for model pull

**Steps:**
1. Open terminal and verify Ollama not running: `pgrep ollama` (should return empty)
2. Run Acode request: `acode ask "what is 2+2?"`
3. Observe startup messages: "Starting Ollama..." "Pulling llama3.2:latest..." (first time takes 5-15 min)
4. Observe request completes successfully with response "2+2 = 4"
5. Verify Ollama still running: `pgrep ollama` (should show PID)
6. Check service status: `acode providers status ollama` (should show Running)

**Expected Results:**
- ✓ No manual Ollama startup required
- ✓ Model automatically downloaded
- ✓ Request succeeds after automatic startup
- ✓ Ollama remains running for next request

---

### Scenario 2: Health Monitoring and Crash Recovery

**Objective:** Verify health checks detect crashes and auto-restart

**Preconditions:**
- Ollama running (from Scenario 1)
- Managed Mode enabled

**Steps:**
1. Verify Ollama running: `acode providers status ollama`
2. Manually crash Ollama: `pkill ollama` or `kill <PID>` from previous status
3. Wait 60+ seconds (one health check interval)
4. Run request: `acode ask "hello"`
5. Observe in logs: health check detects crash, auto-restart triggered
6. Request succeeds (indicates Ollama restarted)
7. Verify restart in history: `acode providers history ollama`

**Expected Results:**
- ✓ Crash detected within 60 seconds
- ✓ Auto-restart triggered automatically
- ✓ Request doesn't fail permanently (transparent recovery)
- ✓ Restart logged in history

---

### Scenario 3: Model Selection and Auto-Pull

**Objective:** Verify different models can be selected and pulled automatically

**Preconditions:**
- Ollama running
- Network connectivity

**Steps:**
1. Edit `.agent/config.yml`, change model: `model: "llama3.2:8b"`
2. Run request: `acode ask "hello"`
3. Observe: "Pulling llama3.2:8b..." if not already cached
4. If 8b model already exists locally, request should use cached version (no pull)
5. Change model again: `model: "neural-chat"`
6. Run request: `acode ask "hello"`
7. Observe pull of new model (if not cached) or use cached version
8. Check available models: `acode providers status ollama` (should list pulled models)

**Expected Results:**
- ✓ Model switching works
- ✓ Auto-pull triggered only when model not cached
- ✓ Subsequent requests use cached models (no re-download)
- ✓ All models show in status

---

### Scenario 4: Configuration Reload and Mode Changes

**Objective:** Verify configuration changes take effect without restart

**Preconditions:**
- Ollama running in Managed Mode

**Steps:**
1. View current status: `acode providers status ollama` (should show Managed Mode)
2. Edit `.agent/config.yml`, change: `mode: monitored`
3. Run request: `acode ask "hello"`
4. Verify health checks stop running (no background restarts)
5. Manually stop Ollama: `pkill ollama`
6. Run request: `acode ask "hello"`
7. Observe: request fails (Monitored Mode doesn't auto-start)
8. Change mode back to Managed: `mode: managed`
9. Run request: `acode ask "hello"`
10. Observe: auto-restart and request succeeds

**Expected Results:**
- ✓ Mode changes take effect without restarting Acode
- ✓ Monitored Mode doesn't auto-start
- ✓ Managed Mode auto-starts when crashed
- ✓ Transparent mode switching

---

### Scenario 5: Disk Space and Model Pull Timeout

**Objective:** Verify disk space check and pull timeout

**Preconditions:**
- Ollama running
- Disk with <5 GB free space (or simulate)

**Steps:**
1. Reduce available disk space to <5 GB (or use test scenario)
2. Try to pull a model: `acode providers pull-model ollama --model "llama3.2:13b"`
3. Observe error: "Insufficient disk space: 3.2 GB free, need >5 GB"
4. Free up disk space
5. Increase pull timeout in config: `model_pull_timeout_minutes: 15`
6. Retry pull: `acode providers pull-model ollama --model "llama3.2:13b"`
7. Observe pull succeeds with timeout respected

**Expected Results:**
- ✓ Disk space check prevents failed pulls
- ✓ Clear error message includes disk usage
- ✓ Pull timeout configurable and respected
- ✓ Retry succeeds after freeing space

---

### Scenario 6: Monitoring Mode with External Service Manager

**Objective:** Verify Monitored Mode works with external systemd/service manager

**Preconditions:**
- Ollama managed by systemd (or other external service manager)
- Acode in Monitored Mode

**Steps:**
1. Set mode to Monitored: `mode: monitored` in config
2. Stop Ollama via systemd: `systemctl stop ollama`
3. Run request: `acode ask "hello"`
4. Observe: error "Ollama not running. Use 'systemctl start ollama' to start service"
5. Start Ollama via systemd: `systemctl start ollama`
6. Run request: `acode ask "hello"`
7. Observe: request succeeds
8. Check Acode didn't start Ollama itself (didn't interfere with systemd)

**Expected Results:**
- ✓ Monitored Mode doesn't auto-start (respects systemd)
- ✓ Clear error message instructs user to use systemd
- ✓ After external start, request succeeds
- ✓ No Acode interference with service manager

---

### Scenario 7: Status History and Restart Tracking

**Objective:** Verify status history and restart tracking work correctly

**Preconditions:**
- Ollama running
- Managed Mode enabled

**Steps:**
1. View history: `acode providers history ollama`
2. Manually crash Ollama: `pkill ollama`
3. Run request to trigger auto-restart: `acode ask "hello"`
4. View history again: `acode providers history ollama`
5. Observe new entry for crash and restart
6. Reset restart counter: `acode providers reset-restarts ollama`
7. Verify counter reset in history

**Expected Results:**
- ✓ History shows all state transitions
- ✓ Crash and restart logged
- ✓ Reset counter clears restart count
- ✓ History readable and actionable

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-001 | OllamaServiceOrchestrator construction with all modes | FR-006 |
| UT-002 | GetStateAsync returns Running when process exists and health check passes | FR-011 |
| UT-003 | GetStateAsync returns Crashed when process no longer exists | FR-012 |
| UT-004 | GetStateAsync returns Failed on 3 consecutive health check failures | FR-013 |
| UT-005 | GetStateAsync caches result for 5 seconds | FR-044 |
| UT-006 | StartAsync times out after configured duration | FR-018 |
| UT-007 | StartAsync detects port in use and reports specific error | FR-020 |
| UT-008 | StartAsync detects ollama binary not found | FR-021 |
| UT-009 | In Monitored Mode, StartAsync not called by EnsureHealthyAsync | FR-022 |
| UT-010 | In External Mode, health checks skipped | FR-023 |
| UT-011 | PullModelAsync calls /api/pull endpoint | FR-027 |
| UT-012 | Model pull retries 3 times on network errors | FR-031 |
| UT-013 | Model pull detects invalid model name (404) | FR-033 |
| UT-014 | Model pull detects insufficient disk space | FR-034 |
| UT-015 | In Airgapped Mode, pull rejected with specific error | FR-035 |
| UT-016 | Health check failure counter resets on success | FR-042 |
| UT-017 | Restart limit prevents >3 restarts per 60 seconds | FR-048 |
| UT-018 | StopAsync sends SIGTERM (Unix) to process | FR-053 |
| UT-019 | StopAsync force-kills after timeout | FR-054 |
| UT-020 | Configuration loaded from .agent/config.yml | FR-061 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-001 | End-to-end auto-start: Ollama not running → EnsureHealthyAsync → process starts → health check passes | FR-016, FR-018, FR-037 |
| IT-002 | Model pull integration: model missing → EnsureHealthyAsync → pull starts → progress logged → model available | FR-025, FR-027, FR-028 |
| IT-003 | Crash recovery: Ollama running → manual kill → health check detects → auto-restart → process restarted | FR-046, FR-047 |
| IT-004 | Monitored Mode doesn't interfere: Ollama crash → Monitored Mode → no auto-restart | FR-022 |
| IT-005 | External Mode skips health checks: Ollama running → External Mode → no health check calls | FR-023 |
| IT-006 | Configuration reload: mode change → new behavior takes effect | FR-062 |
| IT-007 | Graceful shutdown: running process → StopAsync → clean exit logged | FR-053, FR-054, FR-056 |
| IT-008 | Restart limit enforcement: repeated crashes → restart count increments → limit exceeded → stops attempting | FR-047, FR-048, FR-049 |
| IT-009 | Provider integration: ProviderRegistry.GetProvider() → EnsureHealthyAsync() called → provider returned | FR-067 |
| IT-010 | Error reporting: various failure modes → error includes root cause and remediation | FR-072, FR-073 |
| IT-011 | Environment variable override: config file setting → env var overrides it | FR-062 |
| IT-012 | Health check recovery: 2 failures → 1 success → counter resets | FR-042 |

### E2E Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| E2E-001 | User runs `acode ask` with Ollama not running → sees startup progress → request completes | FR-016, FR-026 |
| E2E-002 | User requests different model → pulls new model → switches to it | FR-025, FR-027 |
| E2E-003 | User manually crashes Ollama → next request triggers auto-restart → request completes | FR-046, FR-047 |
| E2E-004 | User changes mode in config → mode takes effect without restart | FR-062 |
| E2E-005 | User views status: `acode providers status ollama` → shows state, PID, uptime, model | FR-079 |

### Performance Tests

| ID | Test Case | Target |
|----|-----------|--------|
| PT-001 | EnsureHealthyAsync when already running | <50ms latency |
| PT-002 | Health check call to /api/tags | <100ms latency |
| PT-003 | Background health checks don't exceed 5% CPU | <5% CPU usage |
| PT-004 | Model pull for 7GB model via network | >50 Mbps throughput |
| PT-005 | State caching reduces API calls by >90% | cache hit ratio >90% |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Providers/Ollama/
│       ├── OllamaServiceState.cs (enum: Running, Crashed, etc.)
│       ├── OllamaLifecycleMode.cs (enum: Managed, Monitored, External)
│       └── IOllamaServiceOrchestrator.cs (interface)
│
├── Acode.Application/
│   └── Providers/Ollama/
│       ├── OllamaLifecycleOptions.cs (configuration)
│       └── IOllamaServiceOrchestrator.cs (copy or shared)
│
├── Acode.Infrastructure/
│   └── Providers/Ollama/Lifecycle/
│       ├── OllamaServiceOrchestrator.cs (main implementation)
│       ├── ServiceStateTracker.cs (state management)
│       ├── HealthCheckWorker.cs (background health checks)
│       ├── RestartPolicyEnforcer.cs (restart limit logic)
│       └── ModelPullManager.cs (model download)
│
└── tests/
    ├── Acode.Domain.Tests/
    │   └── Providers/Ollama/
    │       └── OllamaLifecycleModeTests.cs
    ├── Acode.Application.Tests/
    │   └── Providers/Ollama/
    │       └── OllamaLifecycleOptionsTests.cs
    ├── Acode.Infrastructure.Tests/
    │   └── Providers/Ollama/Lifecycle/
    │       ├── OllamaServiceOrchestratorTests.cs
    │       ├── HealthCheckWorkerTests.cs
    │       ├── RestartPolicyEnforcerTests.cs
    │       └── ModelPullManagerTests.cs
    └── Acode.Integration.Tests/
        └── Providers/Ollama/Lifecycle/
            └── OllamaLifecycleIntegrationTests.cs
```

### Key Interfaces

```csharp
namespace Acode.Application.Providers.Ollama;

/// <summary>
/// Manages Ollama service lifecycle: startup, monitoring, health checks, crash recovery.
/// </summary>
public interface IOllamaServiceOrchestrator
{
    /// <summary>
    /// Ensures Ollama service is in a healthy state, auto-starting or auto-restarting if configured.
    /// </summary>
    Task<ServiceState> EnsureHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current service state without modification.
    /// </summary>
    Task<ServiceState> GetStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually starts Ollama service. Returns Running on success or error state on failure.
    /// </summary>
    Task<ServiceState> StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gracefully stops Ollama service. Sends SIGTERM, force-kills after timeout.
    /// </summary>
    Task<ServiceState> StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls a model from registry. Non-blocking if model already cached.
    /// </summary>
    Task<PullResult> PullModelAsync(string modelName, IProgress<PullProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public enum ServiceState
{
    Unknown = 0,
    Running = 1,
    Starting = 2,
    Stopping = 3,
    Stopped = 4,
    Failed = 5,
    Crashed = 6,
}

public enum OllamaLifecycleMode
{
    Managed = 0,      // Acode fully controls lifecycle
    Monitored = 1,    // Acode watches but doesn't start
    External = 2,     // Acode assumes always running
}

public record ServiceStatus(
    ServiceState State,
    int? ProcessId,
    TimeSpan? Uptime,
    DateTime LastHealthCheck,
    IReadOnlyList<string> AvailableModels,
    int RestartCount,
    DateTime? LastRestartTime);

public record PullResult(
    bool Success,
    string ModelName,
    long BytesDownloaded,
    TimeSpan Duration,
    string? ErrorMessage);

public record PullProgress(
    long BytesDownloaded,
    long? TotalBytes,
    double PercentComplete,
    TimeSpan ElapsedTime,
    TimeSpan? EstimatedTimeRemaining);

public class OllamaLifecycleOptions
{
    public OllamaLifecycleMode Mode { get; set; } = OllamaLifecycleMode.Managed;
    public TimeSpan StartTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(60);
    public int MaxRestartsPerMinute { get; set; } = 3;
    public int ModelPullTimeoutMinutes { get; set; } = 5;
    public bool StopOnExit { get; set; } = false;
}
```

### Configuration Example

```yaml
providers:
  ollama:
    lifecycle:
      mode: managed
      start_timeout_seconds: 30
      health_check_interval_seconds: 60
      max_restarts_per_minute: 3
      model_pull_timeout_minutes: 5
      stop_on_exit: false
```

### Error Codes Table

| Code | Meaning | Resolution |
|------|---------|------------|
| ACODE-OLM-001 | Cannot start Ollama (binary not found) | Install Ollama: `ollama --version` |
| ACODE-OLM-002 | Cannot start Ollama (port in use) | Free port or set OLLAMA_HOST env var |
| ACODE-OLM-003 | Cannot start Ollama (permission denied) | Check file permissions, try different directory |
| ACODE-OLM-004 | Model pull failed (network error) | Check network, retry with increased timeout |
| ACODE-OLM-005 | Model pull failed (invalid model) | Verify model name with `ollama list` |
| ACODE-OLM-006 | Model pull failed (insufficient disk) | Free up disk space (need >5 GB) |
| ACODE-OLM-007 | Health check failed (connection refused) | Verify Ollama running: `ollama serve` |
| ACODE-OLM-008 | Restart limit exceeded (crash loop) | Check Ollama logs, may need smaller model |

### Implementation Checklist

1. Create OllamaServiceState and OllamaLifecycleMode enums (Domain)
2. Create IOllamaServiceOrchestrator interface (Application)
3. Create OllamaLifecycleOptions class (Application)
4. Implement ServiceStateTracker (maintain current state in-memory)
5. Implement HealthCheckWorker (background task monitoring)
6. Implement RestartPolicyEnforcer (restart limit logic)
7. Implement ModelPullManager (orchestrate `ollama pull`)
8. Implement OllamaServiceOrchestrator (main orchestration)
9. Create structured logging fields
10. Wire orchestrator into ProviderRegistry
11. Create configuration schema updates
12. Add CLI commands (start, stop, status, history)
13. Write unit tests for all components
14. Write integration tests for end-to-end flows
15. Write E2E tests with actual Ollama (skipped by default)
16. Update .agent/config.yml schema
17. Document in user manual
18. Document troubleshooting
19. Performance test health check overhead
20. Verify all error messages clear and actionable

### Rollout Plan

| Phase | Description | Duration | Success Criteria |
|-------|-------------|----------|------------------|
| Alpha | Internal testing with single developer | 1 week | No crashes, auto-start works, health checks pass |
| Beta | Testing with 3-5 developers | 2 weeks | Survives crashes, mode switching works, no memory leaks |
| RC | Pre-release with feedback incorporation | 1 week | User feedback positive, no critical bugs, performance acceptable |
| GA | General availability | - | All acceptance criteria met, tests passing, docs complete |

---

**End of Task 005d Specification**
