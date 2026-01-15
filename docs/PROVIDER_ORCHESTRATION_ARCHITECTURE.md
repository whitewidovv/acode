# Provider Orchestration Architecture

**Document Version**: 1.0
**Last Updated**: 2026-01-14
**Status**: Design Complete (Implementation In Progress)

---

## Executive Summary

Acode implements a **three-tier provider orchestration system** that enables seamless switching between local and cloud inference backends while maintaining consistent developer experience. This document describes the architecture, integration points, and future extensibility.

**Key Principle**: Users select a provider and model via configuration; Acode automatically manages its lifecycle, health, and availability.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│  Acode CLI / Application Layer                                   │
│  ProviderRegistry, ProviderSelector, CLI Commands                │
└────────────────────────┬────────────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        │                │                │
        ↓                ↓                ↓
   ┌─────────────┐  ┌──────────────┐  ┌──────────────┐
   │ Ollama      │  │ vLLM         │  │ Cloud        │
   │ Orchestrator│  │ Orchestrator │  │ Orchestrator │
   │ (005d)      │  │ (006d)       │  │ (Future)     │
   └──────┬──────┘  └──────┬───────┘  └──────┬───────┘
          │                │                │
          │ Manages        │ Manages        │ Manages
          │ Lifecycle      │ Lifecycle      │ Lifecycle
          │                │                │
   ┌──────▼──────┐  ┌──────▼───────┐  ┌──────▼───────┐
   │ OllamaProvider
   │ (Task 005a/c)│  │ VllmProvider  │  │ CloudProvider│
   │ HTTP API     │  │ (Task 006a/c) │  │ (Task 007+)  │
   │ Adapter      │  │ HTTP API      │  │ API Adapter  │
   │              │  │ Adapter       │  │              │
   └──────┬──────┘  └──────┬───────┘  └──────┬───────┘
          │                │                │
   ┌──────▼──────────────────────────────────▼───────┐
   │  Model Provider Interface (IModelProvider)       │
   │  Task 004c: Unified inference abstraction        │
   └─────────────────────────────────────────────────┘
          │
   ┌──────▼──────────────────────────────────────────┐
   │  Actual Inference Services                       │
   │  - Ollama (localhost:11434)                      │
   │  - vLLM (localhost:8000)                         │
   │  - Cloud APIs (AWS, Azure, Google Cloud, etc.)   │
   └──────────────────────────────────────────────────┘
```

---

## Three-Tier System

### Tier 1: Service Orchestrators (NEW - Tasks 005d, 006d, Future)

**Responsibility**: Manage service lifecycle independent of application

**Components**:
- **OllamaServiceOrchestrator** (Task 005d)
  - Detects if Ollama is running
  - Auto-starts if missing (Managed Mode)
  - Monitors health via periodic checks
  - Auto-restarts on crash
  - Pulls models on demand

- **VllmServiceOrchestrator** (Task 006d)
  - Detects if vLLM is running on port 8000 (configurable)
  - Auto-starts with specified Huggingface model
  - Monitors GPU availability
  - Auto-loads models on demand (lazy loading)
  - Auto-restarts on crash
  - Configures GPU memory utilization

- **CloudServiceOrchestrator** (Future - Task 029+)
  - No local process management (cloud-hosted)
  - API credential validation
  - Quota and rate limit monitoring
  - Cost tracking and limits
  - Multi-region failover

**Operating Modes** (All three support):
1. **Managed Mode** (Default): Acode fully controls lifecycle
2. **Monitored Mode**: External service manager (systemd) in control
3. **External Mode**: Assumes always running (minimal overhead)

**Key Methods**:
```csharp
Task<ServiceState> EnsureHealthyAsync(CancellationToken)  // Ensures service ready
Task<ServiceState> GetStateAsync(CancellationToken)       // Current state
Task<ServiceState> StartAsync(CancellationToken)          // Manual start
Task<ServiceState> StopAsync(CancellationToken)           // Graceful shutdown
Task<PullResult> PullModelAsync(string model, ...)        // Load/pull model
```

---

### Tier 2: Provider Adapters (Existing - Tasks 005a/c, 006a/c)

**Responsibility**: Translate between Acode's inference interface and provider's HTTP API

**Components**:
- **OllamaProvider** (Task 005a/c)
  - HTTP client to Ollama API (localhost:11434)
  - Request/response translation
  - Streaming support
  - Tool-call parsing
  - Retry on invalid JSON
  - Setup documentation
  - Smoke test scripts (PowerShell + Bash)

- **VllmProvider** (Task 006a/c)
  - HTTP client to vLLM API (localhost:8000)
  - OpenAI-compatible API support
  - Structured outputs enforcement
  - Request/response translation
  - Health check integration
  - Load endpoint verification
  - Error handling

- **CloudProvider** (Future - Tasks 007+)
  - HTTP/gRPC clients to cloud APIs
  - Request/response translation
  - Token budget management
  - Rate limiting
  - Cost tracking

**Key Interface** (IModelProvider, from Task 004c):
```csharp
Task<CompletionResponse> CompleteAsync(CompletionRequest request, CancellationToken)
Task<StreamingCompletionResponse> CompleteStreamingAsync(CompletionRequest, ...)
Task<HealthStatus> GetHealthAsync(CancellationToken)
ProviderCapabilities Capabilities { get; }
```

---

### Tier 3: Unified Provider Interface (Task 004c)

**Responsibility**: Abstraction layer enabling provider swapping

**Components**:
- **IModelProvider Interface**: Defines complete provider contract
- **ProviderRegistry**: Manages all providers, caching, selection
- **ProviderSelector**: Intelligently chooses provider based on:
  - Model requirements
  - Capability matching
  - Fallback priorities
  - Operating mode constraints

**Key Methods**:
```csharp
Task<IModelProvider> GetProviderAsync(string providerId)
Task<IModelProvider> GetDefaultProviderAsync()
Task<IModelProvider> GetProviderForCapabilitiesAsync(CapabilityRequirement)
```

---

## Integration Flow

### When User Runs: `acode ask --provider vllm "write code"`

```
1. CLI receives request
   ↓
2. ProviderRegistry.GetProviderAsync("vllm")
   ↓
3. VllmServiceOrchestrator.EnsureHealthyAsync()
   │   ├─ Check if vLLM process running
   │   ├─ If not (Managed Mode): Start it
   │   ├─ Wait for health endpoint response
   │   ├─ Check if model loaded
   │   └─ If not: Pull model from Huggingface
   ↓
4. VllmProvider ready (decorated with orchestrator)
   ↓
5. Application makes inference request
   ↓
6. VllmProvider translates to OpenAI-compatible API
   ↓
7. vLLM HTTP server responds
   ↓
8. VllmProvider translates back to Acode format
   ↓
9. Application receives response
   ↓
10. Done!
```

---

## Provider Landscape

### Current Providers (Implemented or In-Progress)

| Provider | Task | Tier | Status | Local | Auto-Start | Model Pull | Notes |
|----------|------|------|--------|-------|------------|-----------|-------|
| **Ollama** | 005/005d | 1-2 | ✅ Complete | ✅ Yes | ✅ (005d) | ✅ | Simplest setup, CPU/GPU |
| **vLLM** | 006/006d | 1-2 | ✅ Complete | ✅ Yes | ✅ (006d) | ✅ HF | GPU-optimized, fast |

### Future Providers (Planning/Design Phase)

| Provider | Task | Tier | Status | Local | Notes |
|----------|------|------|--------|-------|-------|
| **AWS Bedrock** | 029+ | 1-2 | 🔄 Planned | ❌ | Managed service, pay-per-inference |
| **Azure OpenAI** | 029+ | 1-2 | 🔄 Planned | ❌ | Enterprise, cost tracking |
| **Google Vertex AI** | 029+ | 1-2 | 🔄 Planned | ❌ | Multimodal models |
| **Anthropic API** | 029+ | 1-2 | ❌ Deferred | ❌ | Conflicts with "no external LLM" philosophy |
| **Local Llama.cpp** | Future | 1-2 | 💡 Optional | ✅ Yes | CPU-only, single file |
| **LM Studio** | Future | 1-2 | 💡 Optional | ✅ Yes | GUI tool, simple API |

### Why Anthropic API Deferred

Task specification explicitly states: **"NO external LLM API calls by default"**

Anthropic API violates this constraint. The system is designed for:
- **LocalOnly Mode**: No external APIs (Ollama/vLLM only)
- **Burst Mode**: Can use cloud, but NOT external LLM APIs
- **Airgapped Mode**: Complete network isolation

The architecture supports extensibility to add it in future if requirements change.

---

## Configuration

### User's View (`.agent/config.yml`)

```yaml
# Select provider and model
providers:
  # Active provider
  active: vllm

  # Ollama configuration
  ollama:
    model: "llama3.2:latest"
    lifecycle:
      mode: managed
      start_timeout_seconds: 30
      health_check_interval_seconds: 60
      stop_on_exit: false

  # vLLM configuration
  vllm:
    model: "meta-llama/Llama-2-7b-hf"
    port: 8000
    lifecycle:
      mode: managed
      start_timeout_seconds: 30
      health_check_interval_seconds: 60
      model_load_timeout_seconds: 300
      stop_on_exit: false
    gpu:
      memory_utilization: 0.9
      tensor_parallel_size: 1
```

### CLI Commands

```bash
# Select provider
acode provider set vllm

# View current provider
acode provider status

# List available providers
acode provider list

# Manage service lifecycle
acode providers start ollama
acode providers stop ollama
acode providers restart ollama
acode providers status ollama

# View model configuration
acode models list --provider vllm
acode models info llama3.2:latest --provider ollama
```

---

## Operating Modes Integration

### LocalOnly Mode
- **Allowed Providers**: Ollama, vLLM (local GPU)
- **Lifecycle**: Managed Mode (auto-start enabled)
- **Model Sources**: Local disk only (no downloads during airgapped runs)
- **Network**: None required

### Burst Mode
- **Allowed Providers**: Ollama, vLLM, AWS Bedrock, Azure OpenAI
- **Lifecycle**: Managed/Monitored/External (user choice)
- **Model Sources**: Local + cloud registry
- **Network**: Required for cloud providers
- **Cost Tracking**: Enabled for cloud providers

### Airgapped Mode
- **Allowed Providers**: Ollama, vLLM (with pre-staged models)
- **Lifecycle**: Monitored/External (no downloads)
- **Model Sources**: Local disk ONLY (no cloud model registry)
- **Network**: Prohibited
- **Model Pull**: Rejected with clear error

---

## Failure Scenarios & Recovery

### Scenario 1: Ollama Crashes Mid-Request

```
1. Request sent to Ollama
   ↓ (Ollama crashes)
2. Connection timeout (5 seconds)
   ↓
3. HealthCheckWorker detects process exit
   ↓
4. ServiceState → Crashed
   ↓
5. Error handling code calls EnsureHealthyAsync()
   ↓
6. OllamaServiceOrchestrator detects crash
   ↓
7. StartAsync() restarts Ollama
   ↓
8. Model re-loads (if needed)
   ↓
9. Request automatically retried (max 3 retries)
   ↓
10. Success!
```

**User Experience**: Transparent recovery, no manual intervention needed

---

### Scenario 2: Model Not Found on Huggingface

```
1. User configures: model: "invalid-model-id"
   ↓
2. ProviderRegistry.GetProviderAsync("vllm")
   ↓
3. VllmServiceOrchestrator.EnsureHealthyAsync()
   ↓
4. Tries to pull model from Huggingface
   ↓
5. HTTP 404: Model not found
   ↓
6. Clear error: "Model 'invalid-model-id' not found on Huggingface"
   ↓
7. User corrects config or provides HF_TOKEN for private models
```

---

### Scenario 3: Port Conflict (vLLM can't start)

```
1. vLLM startup command: vllm serve --model llama2 --port 8000
   ↓
2. Process fails: "Address already in use: [::]:8000"
   ↓
3. VllmServiceOrchestrator detects process didn't start
   ↓
4. Error: "Port 8000 already in use"
   ↓
5. Guidance: "Set VLLM_PORT=8001 or kill: lsof -ti :8000 | xargs kill"
```

---

## Extensibility Pattern

### Adding a New Provider (Example: Llama.cpp)

1. **Create Orchestrator** (if local service)
   - `LlamaCppServiceOrchestrator` (optional, only for managed services)
   - Implement `IServiceOrchestrator` interface
   - Add lifecycle management methods

2. **Create Provider Adapter**
   - `LlamaCppProvider : IModelProvider`
   - Implement `CompleteAsync()`, `CompleteStreamingAsync()`
   - Handle request/response translation

3. **Register in Registry**
   - Add to `ProviderRegistry` configuration
   - Define capabilities
   - Add to `.agent/config.yml` schema

4. **Add CLI Commands**
   - `acode providers start llama.cpp`
   - `acode providers status llama.cpp`

---

## Performance Implications

### Health Check Overhead
- **Interval**: 60 seconds (default, configurable)
- **Latency**: <100ms per check
- **CPU Impact**: <1% idle
- **Total Overhead**: <10 KB memory per orchestrator

### Orchestrator Latency (When Service Running)
- `EnsureHealthyAsync()`: <50ms
- `GetStateAsync()`: <100ms
- First request after startup: +30-60 seconds (service startup + model load)

### Memory Usage
- OllamaServiceOrchestrator: ~15 MB
- VllmServiceOrchestrator: ~20 MB
- Combined: <50 MB for orchestration layer alone

---

## Security Considerations

### Process Management
- ✅ vLLM/Ollama started from validated PATH (no injection)
- ✅ Localhost binding only (not exposed externally)
- ✅ No privilege escalation (runs as current user)
- ✅ Clean shutdown (graceful SIGTERM before force-kill)

### Model Integrity
- ✅ Huggingface models verified via SHA-256
- ✅ Local models checked for accessibility
- ✅ Model URLs validated (no arbitrary remote code)

### Secrets Management
- ✅ HF_TOKEN in environment variable (never logged)
- ✅ API keys not stored in config files
- ✅ Redaction applied to logs containing credentials

### Network Isolation (Airgapped Mode)
- ✅ Cloud provider URLs blocked
- ✅ Model registry access denied
- ✅ Clear error messages guide user to pre-stage models

---

## Testing Strategy

### Unit Tests
- **OllamaServiceOrchestrator**: 25+ tests
  - Startup success/failure scenarios
  - Health check logic
  - Restart policies
  - Model pulling

- **VllmServiceOrchestrator**: 20+ tests
  - GPU detection
  - Model loading
  - Port configuration
  - Environment variables

- **ProviderRegistry**: 40+ tests
  - Provider registration
  - Health checks
  - Selection logic
  - Operating mode constraints

### Integration Tests
- **Multi-provider workflows**: Switch between Ollama ↔ vLLM
- **Crash recovery**: Kill service, verify auto-restart
- **Configuration reload**: Change config, verify immediate effect
- **Cloud provider simulation**: Mock API for testing

### E2E Tests
- Real Ollama instance running locally
- Real vLLM instance running locally
- Actual model inference (skipped in CI by default)
- Performance benchmarks

---

## Future Roadmap

### Phase 1 (Current)
- ✅ Ollama Orchestrator (Task 005d)
- ✅ vLLM Orchestrator (Task 006d)
- ✅ Operating mode integration

### Phase 2 (Planned - Task 007+)
- 🔄 Cloud provider orchestrators (AWS, Azure, GCP)
- 🔄 Cost tracking and quotas
- 🔄 Multi-region failover

### Phase 3 (Future)
- 💡 Model quantization selection
- 💡 Automatic provider selection based on cost
- 💡 Distributed inference across multiple services
- 💡 Provider benchmarking and comparison

---

## Conclusion

The provider orchestration architecture enables Acode to:
1. **Simplify user experience**: "Just select a provider, it works"
2. **Support multiple backends**: Swap between providers seamlessly
3. **Maintain consistency**: Same interface regardless of provider
4. **Enable offline-first**: LocalOnly mode with no external APIs
5. **Scale to cloud**: Burst mode adds cloud providers when needed

This design embodies Acode's philosophy: **privacy-first, locally-hosted by default, with optional cloud burst when needed.**

---

**End of Document**
