# Task 006d: vLLM Lifecycle Management

**Priority:** P1 – High Priority
**Tier:** Infrastructure
**Complexity:** 8 (Fibonacci points)
**Phase:** Foundation + Operations
**Dependencies:** Task 006, Task 006a, Task 006c, Task 005d (Ollama Lifecycle)

---

## Description

### Business Value

Task 006d applies the same lifecycle automation from Task 005d (Ollama) to vLLM, enabling users to work with vLLM exactly as easily as Ollama. Currently, users must manually manage vLLM processes just as they do with Ollama—start it in another terminal, manage model loading, monitor for crashes.

By implementing vLLM lifecycle management parallel to Ollama, Acode provides consistent developer experience across multiple inference backends. The same Managed/Monitored/External modes apply to vLLM, allowing users to switch between providers without changing their workflow.

**Value Propositions:**
1. **Consistent Multi-Provider Experience**: Users switch providers (Ollama → vLLM) without workflow change
2. **Automatic vLLM Startup**: Running `acode ask --provider vllm` auto-starts vLLM (if not running)
3. **Model Load Automation**: vLLM auto-loads models on demand (faster than Ollama, useful for benchmarking)
4. **Production-Ready Monitoring**: vLLM health checks and auto-restart like Ollama
5. **Performance Comparison**: Users can easily A/B test providers with automatic lifecycle handling
6. **Burst Compute Strategy**: Foundation for Task 007 (Cloud Burst) to swap vLLM ↔ cloud based on load
7. **Enterprise Deployment**: Monitored Mode enables vLLM management by external orchestrators
8. **Distributed Development**: Multiple developers use same vLLM instance via Monitored/External modes

### Technical Approach

vLLM lifecycle mirrors Ollama's orchestrator design but accounts for vLLM-specific details:

```
Acode CLI / Application
        ↓
VllmServiceOrchestrator (NEW)
  ├── Detection (Is vLLM running on port 8000?)
  ├── Startup (Start if missing, with model --load-format)
  ├── Model Management (Load models dynamically)
  ├── Health Monitoring (Check /health endpoint)
  ├── Crash Recovery (Auto-restart)
  └── Performance Monitoring (GPU utilization)
        ↓
vLLM HTTP API (Task 006c)
```

**Key Differences from Ollama:**

1. **Model Loading**: vLLM loads models on first request (lazy), not pre-pulled
   - Ollama: `ollama pull model` (separate step)
   - vLLM: `vllm serve --model model-name` (loads on startup)

2. **Port Configuration**: vLLM defaults to 8000 (vs Ollama's 11434)
   - Configurable via `--port` flag and `VLLM_PORT` env var

3. **GPU Requirements**: vLLM optimized for GPU, requires CUDA/ROCm
   - Ollama: works CPU-only (slow)
   - vLLM: CPU fallback but primarily GPU-targeted

4. **Startup Speed**: vLLM startup faster than Ollama
   - Model load on first request (lazy)
   - Allows quicker startup

5. **Model Format**: vLLM uses Huggingface model identifiers
   - Ollama: custom model name mapping (llama3.2:latest)
   - vLLM: direct Huggingface IDs (meta-llama/Llama-2-7b-hf)

6. **Multi-Model Support**: vLLM can load multiple models with pooling
   - Ollama: single model at a time
   - vLLM: multiple models via `--enable-lora` and pooling

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| VllmProvider (Task 006a) | Decorator Pattern | Orchestrator wraps provider, enforces healthy state |
| Configuration (Task 002) | Config Section | `providers.vllm.lifecycle` with vLLM-specific settings |
| CLI Commands | Implicit Management | `acode --provider vllm ask` triggers model availability checks |
| ProviderRegistry (Task 004c) | Lifecycle Hooks | Registry calls `EnsureHealthyAsync()` for vLLM |
| Error Handling (Task 006c) | Recovery Strategy | Connection errors trigger health check → auto-restart |
| Logging | Structured Events | Log vLLM startup, model loading, restart events |
| Operating Modes (Task 001) | Constraints | Airgapped mode: no HF model registry fetching, pre-staged only |
| Ollama Orchestrator (Task 005d) | Peer Component | Parallel implementation, share error patterns |

### Failure Modes

| Failure | Detection | Impact | Recovery |
|---------|-----------|--------|----------|
| vLLM won't start (GPU driver missing) | Process start timeout + CUDA error | User gets "GPU not available" error | User must install CUDA/ROCm or switch to Ollama |
| Model download fails (HF token required) | HTTP 401/403 during model load | User gets "Authentication required" error | User provides HF token via env var |
| VRAM exhausted (model too large) | OOM killer or vLLM memory error | vLLM crashes, auto-restart attempts repeatedly | Detect restart loop, suggest smaller model |
| Port conflict (8000 already in use) | Bind error on startup | Process start fails | User configures alternate `VLLM_PORT` |
| Model not found on Huggingface | HTTP 404 on model load | Request fails with "Model not found" | User verifies model ID is valid |
| GPU memory fragmentation | Degraded latency over time | vLLM continues running but slow | Suggest vLLM restart to defrag memory |
| Network unavailable (model download) | Timeout on HF model download | Initial model load fails | Retry when network available or use pre-staged model |

### Assumptions

1. vLLM is installed at system PATH (`vllm` command available) or `python -m vllm`
2. GPU available (CUDA 11.8+ or ROCm) for optimal performance (CPU fallback available but slow)
3. Port 8000 available or user configures alternate via `VLLM_PORT`
4. Sufficient VRAM for model (varies: 13B model ≈ 26GB VRAM, 7B ≈ 14GB)
5. Sufficient disk space for model download (~13-30 GB typical)
6. Huggingface model identifiers used directly (e.g., `meta-llama/Llama-2-7b-hf`)
7. Optional HF_TOKEN env var provided if private models or gated models needed
8. vLLM API compatible with OpenAI format (Task 006a ensures this)
9. Process supervision on Unix via simple restart (not systemd initially)
10. Airgapped mode assumes pre-staged models in local cache (no HF registry access)
11. "Healthy" state means: process running + `/health` endpoint responding + model loaded
12. Health check uses `/v1/models` endpoint (OpenAI-compatible) to verify model availability

### Security Considerations

1. **Process Execution**: vLLM started via `python -m vllm` or `vllm` binary, validated from PATH
2. **GPU Access**: vLLM inherits user's GPU access (no privilege escalation for GPUs)
3. **Model Integrity**: Huggingface models verified via checksums (HF handles this)
4. **HF Token**: Stored in environment variable, not config file (never log token)
5. **Network Exposure**: vLLM binds to localhost:8000 by default (not exposed externally)
6. **Resource Exhaustion**: Monitor GPU memory usage to prevent DoS (avoid quadratic prompts)
7. **Model Scanning**: vLLM itself scans models for safety (Acode defers to vLLM)
8. **Private Models**: Users must provide HF_TOKEN for gated models, not Acode's responsibility
9. **Airgapped Constraints**: Enforce that airgapped mode cannot fetch models from HF (pre-staged only)
10. **No Credential Logging**: Ensure HF_TOKEN never appears in logs

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| vLLM | Large Language Model serving engine optimized for GPU inference |
| Model Serving | HTTP server exposing LLM inference via REST API |
| Model Loading | Loading model weights into GPU/CPU memory for inference |
| Lazy Loading | Deferring model load until first request (vLLM's default) |
| Eager Loading | Loading model on startup (explicit `--load-format`) |
| Huggingface Model ID | Unique identifier for models on Huggingface: org/model-name |
| VRAM | Video Random Access Memory on GPU |
| GPU Driver | CUDA/ROCm software enabling GPU access |
| Model Weights | Learned parameters of neural network (downloaded from HF) |
| Token | Authentication credential for Huggingface API (HF_TOKEN) |
| Gated Model | Model requiring explicit permission or authentication to download |
| Quantization | Model compression technique reducing VRAM requirements |
| Model Cache | Location where downloaded models stored locally |
| OpenAI-Compatible API | API following OpenAI's format (/v1/chat/completions) |
| Structured Outputs | Models returning JSON in guaranteed format |
| vLLM Process | Running vLLM server instance bound to port |
| Service Orchestrator | Component managing vLLM process lifecycle |
| Health Check | Periodic verification vLLM is running and model loaded |

---

## Out of Scope

The following items are explicitly excluded from Task 006d:

- **vLLM Installation**: Users install vLLM beforehand; Acode doesn't install dependencies
- **CUDA/GPU Driver Installation**: GPU setup is user responsibility before vLLM
- **Model Caching Strategy**: vLLM manages HF model cache via `~/.cache/huggingface/`
- **Quantization Management**: No automatic model quantization (user chooses format)
- **LoRA/Adapter Loading**: vLLM's LoRA support not orchestrated initially
- **Distributed vLLM**: Multi-instance vLLM (not single-instance focus of this task)
- **Throughput Optimization**: No automatic batching tuning (vLLM handles internally)
- **Memory Optimization**: No automatic model-to-device placement (vLLM handles)
- **Fallback to CPU**: No auto-fallback if GPU unavailable (user configures)
- **Model Fine-Tuning**: Fine-tuning not orchestrated (inference only)
- **Private Model Management**: No Acode-level credential management (HF_TOKEN env var only)
- **Performance Profiling**: Detailed performance analysis deferred to separate task
- **Container Orchestration**: vLLM in Docker assumed pre-started externally
- **Upgrade Management**: vLLM version management by users (Acode doesn't upgrade)

---

## Functional Requirements

### Service Orchestrator Interface

| ID | Requirement |
|----|-------------|
| FR-001 | VllmServiceOrchestrator interface MUST define same methods as OllamaServiceOrchestrator |
| FR-002 | VllmServiceOrchestrator MUST accept model ID in constructor (e.g., "meta-llama/Llama-2-7b-hf") |
| FR-003 | VllmServiceOrchestrator MUST support LifecycleMode (Managed, Monitored, External) |
| FR-004 | VllmServiceOrchestrator MUST accept VllmLifecycleOptions for configuration |
| FR-005 | EnsureHealthyAsync() MUST accept optional model ID override parameter |
| FR-006 | StartAsync() MUST support GPU configuration (device_map, gpu_memory_utilization) |

### vLLM-Specific Startup

| ID | Requirement |
|----|-------------|
| FR-007 | StartAsync() MUST start vLLM via `python -m vllm.entrypoints.openai.api_server` or `vllm serve` command |
| FR-008 | StartAsync() MUST pass `--model <model-id>` to load specified Huggingface model |
| FR-009 | StartAsync() MUST pass `--port <port>` (default 8000) to bind to configured port |
| FR-010 | StartAsync() MUST respect `VLLM_PORT` environment variable for port override |
| FR-011 | StartAsync() MUST pass `--tensor-parallel-size` if configured for multi-GPU |
| FR-012 | StartAsync() MUST detect if GPU unavailable and report specific error |
| FR-013 | StartAsync() MUST detect if model not found on Huggingface and report specific error |
| FR-014 | StartAsync() MUST pass HF_TOKEN if user configured for private/gated models |
| FR-015 | StartAsync() MUST use lazy model loading (vLLM default behavior) |

### Health Monitoring

| ID | Requirement |
|----|-------------|
| FR-016 | Health check MUST call `/health` endpoint (vLLM-specific health) |
| FR-017 | Health check MUST also call `/v1/models` to verify model is loaded |
| FR-018 | If model not in `/v1/models` response, MUST retry model load or fail gracefully |
| FR-019 | Health check timeout MUST be 5 seconds (same as Ollama) |
| FR-020 | Failed health check MUST trigger auto-restart per restart policy |

### Model Management

| ID | Requirement |
|----|-------------|
| FR-021 | EnsureHealthyAsync() MUST ensure configured model is available (loaded or loadable) |
| FR-022 | If model not loaded on first request, lazy loading MUST auto-trigger |
| FR-023 | Model load timeout MUST be configurable via `model_load_timeout_seconds` (default 300s) |
| FR-024 | In Airgapped Mode, model load from HF MUST be rejected if model not in local cache |
| FR-025 | Model load failure MUST report Huggingface error details (401 auth required, 404 not found, etc.) |
| FR-026 | Model ID format MUST be validated (org/model-name format) before passing to vLLM |
| FR-027 | Model switching (changing model ID) MUST restart vLLM with new model |

### GPU Configuration

| ID | Requirement |
|----|-------------|
| FR-028 | GPU utilization MUST be configurable via `gpu_memory_utilization` (0.0-1.0, default 0.9) |
| FR-029 | Tensor parallelism MUST be configurable via `tensor_parallel_size` for multi-GPU |
| FR-030 | Pipeline parallelism (if supported) configurable via configuration |
| FR-031 | Quantization format NOT auto-selected (user specifies via model ID or vLLM config) |

### Crash Detection and Recovery

| ID | Requirement |
|----|-------------|
| FR-032 | Crash detection same as Ollama: process exit detected via PID |
| FR-033 | Restart limit enforced: max 3 restarts per 60 seconds (same as Ollama) |
| FR-034 | Auto-restart MUST wait minimum 2 seconds before retry |
| FR-035 | Manual StartAsync() resets restart counter |

### Configuration

| ID | Requirement |
|----|-------------|
| FR-036 | VllmLifecycleOptions MUST include: Mode, StartTimeout, HealthCheckInterval, MaxRestarts, Port |
| FR-037 | Configuration MUST load from `providers.vllm.lifecycle` section in .agent/config.yml |
| FR-038 | Model ID MUST be configurable via: (a) config file, (b) CLI `--model` flag, (c) VLLM_MODEL env var |
| FR-039 | Default configuration: Mode=Managed, StartTimeout=30s, HealthCheckInterval=60s, Port=8000 |
| FR-040 | Port configuration MUST support custom ports (8001, 8002, etc.) to avoid conflicts |
| FR-041 | Configuration validation MUST check: mode valid, timeouts positive, port in valid range |
| FR-042 | GPU memory utilization MUST be validated (0.0 <= value <= 1.0) |

### Error Reporting

| ID | Requirement |
|----|-------------|
| FR-043 | Port in use error MUST show which process is using port |
| FR-044 | Model not found error MUST show model ID and suggest checking Huggingface |
| FR-045 | Authentication error MUST suggest setting HF_TOKEN environment variable |
| FR-046 | GPU unavailable error MUST show GPU detection command: `nvidia-smi` or `rocm-smi` |
| FR-047 | Model load timeout error MUST show duration and suggest increasing timeout |
| FR-048 | All errors logged to structured logs at Error level |

### Integration with Provider Registry

| ID | Requirement |
|----|-------------|
| FR-049 | ProviderRegistry.GetProviderAsync() MUST call VllmServiceOrchestrator.EnsureHealthyAsync() |
| FR-050 | If EnsureHealthyAsync() fails, MUST return error with clear guidance |
| FR-051 | Orchestrator MUST be transparent to provider clients |

---

## Non-Functional Requirements

### Performance

| ID | Requirement | Target | Maximum |
|----|-------------|--------|---------|
| NFR-001 | EnsureHealthyAsync() when already running | <50ms | 200ms |
| NFR-002 | EnsureHealthyAsync() when needs restart | 2-5s | 30s |
| NFR-003 | Health check latency | <100ms | 5s (timeout) |
| NFR-004 | Model lazy load on first request | <30s | (varies by model/GPU) |
| NFR-005 | Startup overhead (process + model load) | 5-15s | 60s maximum |
| NFR-006 | Memory overhead of orchestrator | <20 MB | 50 MB |
| NFR-007 | CPU usage in idle | <1% | 5% |
| NFR-008 | GPU monitoring overhead | <5% | 10% |

### Reliability

| ID | Requirement | Target | Maximum |
|----|-------------|--------|---------|
| NFR-009 | Service restart success rate | >95% | after retry: 99% |
| NFR-010 | Health check accuracy (crash detection) | >95% | <1 minute detection lag |
| NFR-011 | No orphaned vLLM processes after Acode exit | 100% | automatic cleanup |
| NFR-012 | Model load success rate | >95% | on retry: 99% |

### Observability

| ID | Requirement | Target | Maximum |
|----|-------------|--------|---------|
| NFR-013 | All state transitions logged | 100% | no missing transitions |
| NFR-014 | All errors logged | 100% | no silent failures |
| NFR-015 | GPU utilization monitored | 100% (if available) | optional metric |
| NFR-016 | Model loading progress logged | 100% | every 10-20 seconds |

### Compatibility

| ID | Requirement | Target | Maximum |
|----|-------------|--------|---------|
| NFR-017 | vLLM version compatibility | 0.4.0+ | support last 12 months |
| NFR-018 | GPU support: NVIDIA, AMD | >95% coverage | modern hardware |
| NFR-019 | Huggingface model compatibility | >99% of models | standard HF format |

---

## User Manual Documentation

### Overview

The vLLM Lifecycle Manager automatically handles starting vLLM, loading models, monitoring health, and recovering from crashes—just like Ollama. Select vLLM as your provider and Acode takes care of the rest.

For GPU-accelerated inference, vLLM is faster than Ollama. Perfect for performance-critical workloads or benchmarking different models.

### Quick Start

**Default configuration:**
```bash
# Run with vLLM (instead of default Ollama)
acode ask --provider vllm "write a hello world program"

# Acode will:
# 1. Check if vLLM is running
# 2. If not, start it (takes ~10 seconds + GPU warmup)
# 3. Load meta-llama/Llama-2-7b-hf model (or configured model)
# 4. Run your request
```

**Check vLLM status:**
```bash
acode providers status vllm
# Output:
# Service: Running (PID 56789)
# Uptime: 1 hour 23 minutes
# Model: meta-llama/Llama-2-7b-hf
# GPU 0: 23.4 GB / 24.0 GB (97% utilization)
# Last health check: 12 seconds ago (healthy)
```

### Configuration

Configure vLLM in `.agent/config.yml`:

```yaml
providers:
  vllm:
    # Model ID from Huggingface
    model: "meta-llama/Llama-2-7b-hf"

    # HTTP port for vLLM API
    port: 8000

    lifecycle:
      mode: managed                          # managed, monitored, or external
      start_timeout_seconds: 30
      health_check_interval_seconds: 60
      max_restarts_per_minute: 3
      model_load_timeout_seconds: 300        # 5 minutes for model lazy load
      stop_on_exit: false

    gpu:
      # GPU memory utilization (0.0 = none, 1.0 = all)
      memory_utilization: 0.9

      # Tensor parallelism for multi-GPU (1 = single GPU)
      tensor_parallel_size: 1
```

**Environment variable overrides:**
```bash
# Override model
export VLLM_MODEL="meta-llama/Llama-2-13b-hf"

# Override port
export VLLM_PORT=8001

# Huggingface token for private/gated models
export HF_TOKEN="hf_your_token_here"

# Run with overrides
acode ask "hello"
```

### CLI Commands

```bash
# Start vLLM manually
acode providers start vllm
# Output: Starting vLLM with meta-llama/Llama-2-7b-hf...
# GPU: NVIDIA RTX 4090 (24GB VRAM)
# Model loading... [████████░░░░░░░░░░] 45%
# [done] Model loaded successfully
# Listening on http://localhost:8000

# Check status
acode providers status vllm
# Shows service state, GPU usage, model info

# Stop vLLM
acode providers stop vllm

# View restart history
acode providers history vllm

# Switch models (restarts vLLM with new model)
acode providers set-model vllm "meta-llama/Llama-2-13b-hf"
# Output: Stopping current model...
# Starting with meta-llama/Llama-2-13b-hf...
# Model loading... (may take 30+ seconds)
```

### Best Practices

1. **Choose Right Model for Hardware**:
   - 7B model: needs 14 GB VRAM
   - 13B model: needs 26 GB VRAM
   - Use smaller models if VRAM limited

2. **Pre-Stage Large Models**:
   - First model download takes time (13B = ~26 GB)
   - Pull in advance: `huggingface-cli download meta-llama/Llama-2-7b-hf`

3. **Use Private Models**:
   - Get HF token from https://huggingface.co/settings/tokens
   - Export: `export HF_TOKEN="hf_..."`

4. **Monitor GPU Memory**:
   - Check with `acode providers status vllm`
   - Adjust `memory_utilization` if running other GPU tasks

5. **Benchmark Providers**:
   - Compare Ollama vs vLLM performance
   - Same workflow: just change `--provider` flag

### Troubleshooting

**Problem: "GPU not available (CUDA not found)"**
- **Causes:**
  1. NVIDIA drivers not installed
  2. CUDA toolkit not installed
  3. vLLM can't detect GPU

- **Solutions:**
  1. Check GPU: `nvidia-smi` (should show GPU info)
  2. Install NVIDIA drivers from nvidia.com
  3. Install CUDA 11.8+: https://developer.nvidia.com/cuda-downloads
  4. Or switch to Ollama (no GPU required)

- **Example Fix:**
  ```bash
  # Verify GPU available
  nvidia-smi
  # If no output, install drivers
  ```

**Problem: "Model not found on Huggingface"**
- **Causes:**
  1. Typo in model ID (should be org/model-name)
  2. Model doesn't exist
  3. Private model and no HF_TOKEN provided
  4. Gated model and permission not granted

- **Solutions:**
  1. Verify model ID: https://huggingface.co/models
  2. Check for typos: "meta-llama/Llama-2-7b-hf" (correct) vs "meta-llama/llama-2-7b" (wrong)
  3. For private models: `export HF_TOKEN="hf_..."`
  4. For gated models: accept terms on HF website first

- **Example Fix:**
  ```bash
  # Get correct model ID
  export VLLM_MODEL="meta-llama/Llama-2-7b-chat-hf"
  acode ask "hello"
  ```

**Problem: "Model load timeout (took >300 seconds)"**
- **Causes:**
  1. Model too large for GPU (not enough VRAM)
  2. Downloading first time (slow network)
  3. GPU driver issues causing slow load

- **Solutions:**
  1. Use smaller model (7B instead of 13B)
  2. Increase timeout: `model_load_timeout_seconds: 600`
  3. Pre-download model: `huggingface-cli download <model-id>`
  4. Check GPU: `nvidia-smi` (should show activity)

- **Example Fix:**
  ```bash
  # Increase timeout to 10 minutes
  # Then run command that triggered timeout
  ```

**Problem: "VRAM exhausted (OOM killer)"**
- **Causes:**
  1. Model too large for GPU VRAM
  2. Running multiple apps on GPU simultaneously
  3. Memory fragmentation over time

- **Solutions:**
  1. Use smaller model
  2. Close other GPU apps
  3. Reduce batch size or context length
  4. Decrease memory_utilization setting

---

## Acceptance Criteria

### Auto-Start Functionality

- [ ] AC-001: Managed Mode starts vLLM if not running
- [ ] AC-002: Startup loads configured Huggingface model
- [ ] AC-003: GPU detection works and reports errors if unavailable
- [ ] AC-004: Port configuration respected (default 8000, or configured)
- [ ] AC-005: Port conflict detected with helpful error message

### Model Management

- [ ] AC-006: Configured model loaded on startup
- [ ] AC-007: Model lazy loading works (load on first request)
- [ ] AC-008: Invalid model ID rejected with helpful error
- [ ] AC-009: Huggingface authentication errors show clear guidance
- [ ] AC-010: Model switching (changing model ID) works (restarts vLLM)
- [ ] AC-011: Private models work with HF_TOKEN
- [ ] AC-012: Gated models require user acceptance (Acode shows guidance)
- [ ] AC-013: Airgapped mode rejects HF model loading if not pre-cached

### Health Monitoring

- [ ] AC-014: Health checks use `/health` endpoint
- [ ] AC-015: Model availability checked via `/v1/models`
- [ ] AC-016: Three failed checks trigger auto-restart
- [ ] AC-017: Successful checks reset failure counter

### Crash Recovery

- [ ] AC-018: Process crash detected and logged
- [ ] AC-019: Auto-restart triggered in Managed Mode
- [ ] AC-020: Restart limit prevents loops (max 3 per 60s)
- [ ] AC-021: Restart history maintained for diagnostics

### Configuration

- [ ] AC-022: Configuration loads from `providers.vllm.lifecycle` section
- [ ] AC-023: Environment variables override config file
- [ ] AC-024: Invalid config detected with error message
- [ ] AC-025: Model ID configured and validated (org/model-name format)
- [ ] AC-026: Port configuration supported (8000-65535)
- [ ] AC-027: GPU memory utilization 0.0-1.0 validated

### GPU Configuration

- [ ] AC-028: GPU memory utilization configurable (0.0-1.0)
- [ ] AC-029: Tensor parallelism configurable for multi-GPU
- [ ] AC-030: GPU unavailable detected with helpful error
- [ ] AC-031: GPU monitoring available in status output

### Status Reporting

- [ ] AC-032: Status shows service state, PID, uptime
- [ ] AC-033: Status shows current model loaded
- [ ] AC-034: Status shows GPU utilization (if available)
- [ ] AC-035: Status shows last health check and result

### Error Handling

- [ ] AC-036: Port-in-use error shows process using port
- [ ] AC-037: Model-not-found error shows model ID and HF link
- [ ] AC-038: GPU-unavailable error shows detection command
- [ ] AC-039: Auth-required error suggests HF_TOKEN
- [ ] AC-040: Timeout error shows duration and suggests increasing timeout

### Integration

- [ ] AC-041: ProviderRegistry calls EnsureHealthyAsync()
- [ ] AC-042: Provider requests fail with clear message if orchestrator fails
- [ ] AC-043: Connection errors trigger auto-restart
- [ ] AC-044: Requests retried after auto-restart (max 3 retries)

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-001 | VllmServiceOrchestrator startup with valid model ID | FR-007, FR-009 |
| UT-002 | Startup detects port in use | FR-009 |
| UT-003 | Startup detects GPU unavailable | FR-012 |
| UT-004 | Startup detects invalid model on HF | FR-013 |
| UT-005 | Health check calls `/health` endpoint | FR-016 |
| UT-006 | Health check verifies model loaded via `/v1/models` | FR-017 |
| UT-007 | Model ID format validation | FR-026 |
| UT-008 | Configuration loading and validation | FR-036, FR-041 |
| UT-009 | Restart limit enforcement | FR-033 |
| UT-010 | Airgapped mode blocks HF model loading | FR-024 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-001 | End-to-end startup: not running → startup → model load → request succeeds | FR-007, FR-021 |
| IT-002 | Model switching: change model ID → restart → new model loads | FR-027 |
| IT-003 | Crash recovery: vLLM running → manual kill → auto-restart → request succeeds | FR-032, FR-033 |
| IT-004 | GPU detection: GPU available/unavailable → reported correctly | FR-012 |
| IT-005 | HF authentication: with/without token → private models work/fail appropriately | FR-014 |
| IT-006 | Multi-GPU support: tensor parallelism configured → applies correctly | FR-011 |
| IT-007 | Status reporting: provides state, model, GPU utilization | AC-032 to AC-035 |

### Performance Tests

| ID | Test Case | Target |
|----|-----------|--------|
| PT-001 | Startup with 7B model | <15 seconds (GPU) |
| PT-002 | Health check latency | <100ms |
| PT-003 | Model lazy load on first request | <30 seconds |
| PT-004 | GPU memory utilization monitoring overhead | <5% CPU |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Providers/VLLm/
│       ├── VllmServiceState.cs (reuse from Ollama pattern)
│       ├── VllmLifecycleMode.cs (reuse pattern)
│       └── IVllmServiceOrchestrator.cs (interface)
│
├── Acode.Application/
│   └── Providers/VLLm/
│       ├── VllmLifecycleOptions.cs (configuration)
│       └── IVllmServiceOrchestrator.cs (copy or shared)
│
├── Acode.Infrastructure/
│   └── Providers/VLLm/Lifecycle/
│       ├── VllmServiceOrchestrator.cs (main, mirrors Ollama)
│       ├── VllmHealthCheckWorker.cs
│       ├── VllmGpuMonitor.cs (NEW: GPU-specific)
│       └── VllmModelLoader.cs (NEW: HF-specific)
│
└── tests/
    ├── Acode.Infrastructure.Tests/
    │   └── Providers/VLLm/Lifecycle/
    │       ├── VllmServiceOrchestratorTests.cs
    │       ├── VllmHealthCheckWorkerTests.cs
    │       └── VllmGpuMonitorTests.cs
    └── Acode.Integration.Tests/
        └── Providers/VLLm/Lifecycle/
            └── VllmLifecycleIntegrationTests.cs
```

### Configuration Example

```yaml
providers:
  vllm:
    model: "meta-llama/Llama-2-7b-hf"
    port: 8000
    lifecycle:
      mode: managed
      start_timeout_seconds: 30
      health_check_interval_seconds: 60
      max_restarts_per_minute: 3
      model_load_timeout_seconds: 300
      stop_on_exit: false
    gpu:
      memory_utilization: 0.9
      tensor_parallel_size: 1
```

### Error Codes Table

| Code | Meaning | Resolution |
|------|---------|------------|
| ACODE-VLM-001 | GPU not available | Install NVIDIA drivers and CUDA |
| ACODE-VLM-002 | Model not found on Huggingface | Verify model ID (org/model-name format) |
| ACODE-VLM-003 | Authentication required | Set HF_TOKEN for private models |
| ACODE-VLM-004 | Model load timeout | Increase timeout or use smaller model |
| ACODE-VLM-005 | Port in use | Use different port via VLLM_PORT |
| ACODE-VLM-006 | Insufficient VRAM | Use smaller model or reduce batch size |

### Implementation Checklist

1. Create VllmServiceState enum (mirror from OllamaServiceState)
2. Create IVllmServiceOrchestrator interface
3. Create VllmLifecycleOptions class
4. Implement VllmServiceOrchestrator (mirror Ollama implementation)
5. Implement VllmGpuMonitor (GPU-specific health metrics)
6. Implement VllmModelLoader (Huggingface model loading)
7. Add configuration schema updates
8. Wire into ProviderRegistry
9. Add CLI commands (start, stop, status, set-model)
10. Write unit tests (20+ tests)
11. Write integration tests (8-10 tests)
12. Performance test GPU overhead
13. Update user manual
14. Test with various Huggingface models
15. Verify GPU error handling

---

**End of Task 006d Specification**
