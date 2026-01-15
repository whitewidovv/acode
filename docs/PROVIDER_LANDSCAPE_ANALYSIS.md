# Provider Landscape Analysis & Roadmap

**Document Version**: 1.0
**Last Updated**: 2026-01-14
**Purpose**: Identify all required providers, their implementation status, and roadmap

---

## Executive Summary

Acode currently supports **2 local providers** with complete lifecycle management. **3-5 cloud providers** are planned for the Cloud Burst phase. This document provides a comprehensive inventory, gap analysis, and roadmap.

### Current Status

| Category | Status | Providers |
|----------|--------|-----------|
| **Local (Fully Implemented)** | ✅ Complete | Ollama, vLLM |
| **Local (Planned)** | 🔄 Designed | Llama.cpp, LM Studio |
| **Cloud (Planned)** | 💡 Future | AWS Bedrock, Azure OpenAI, Google Vertex AI |
| **Intentionally Excluded** | ❌ Deferred | Anthropic API (violates LocalOnly philosophy) |

---

## Provider Matrix

### Dimension 1: Execution Location

```
┌─────────────────────────────────────────────────────┐
│ WHERE DOES INFERENCE RUN?                           │
├─────────────────────────────────────────────────────┤
│ LOCAL (User's machine)                              │
│   ├─ Ollama (Task 005) ✅                           │
│   ├─ vLLM (Task 006) ✅                             │
│   ├─ Llama.cpp (Future) 💡                          │
│   └─ LM Studio (Future) 💡                          │
│                                                     │
│ CLOUD (Managed service)                             │
│   ├─ AWS Bedrock (Task 029+) 🔄                     │
│   ├─ Azure OpenAI (Task 029+) 🔄                    │
│   ├─ Google Vertex AI (Task 029+) 🔄                │
│   └─ Anthropic (EXCLUDED) ❌                        │
└─────────────────────────────────────────────────────┘
```

### Dimension 2: Lifecycle Management Need

```
┌──────────────────────────────────────────────────────┐
│ HOW DOES ACODE MANAGE THE SERVICE?                   │
├──────────────────────────────────────────────────────┤
│ MANAGED (Acode starts/stops/monitors)                │
│   ├─ Ollama (Task 005d) ✅                           │
│   ├─ vLLM (Task 006d) ✅                             │
│   ├─ Llama.cpp (Future - optional) 💡                │
│   └─ LM Studio (Future - optional) 💡                │
│                                                      │
│ UNMANAGED (User manages externally)                  │
│   ├─ AWS Bedrock (cloud-hosted, no startup) 🔄      │
│   ├─ Azure OpenAI (cloud-hosted) 🔄                 │
│   └─ Google Vertex AI (cloud-hosted) 🔄             │
│                                                      │
│ HYBRID (Both options)                                │
│   └─ Any local provider (Monitored or Managed) 🔄    │
└──────────────────────────────────────────────────────┘
```

### Dimension 3: Operating Mode Compatibility

```
┌────────────────────────────────────────────────────────┐
│ WHICH OPERATING MODES SUPPORT THIS PROVIDER?           │
├────────────────────────────────────────────────────────┤
│ LocalOnly Mode (no external network)                   │
│   ├─ Ollama ✅                                        │
│   ├─ vLLM ✅                                          │
│   ├─ Llama.cpp 💡                                     │
│   └─ LM Studio 💡                                     │
│   ❌ NO Cloud providers                               │
│                                                        │
│ Burst Mode (cloud allowed, but NOT external LLM APIs) │
│   ├─ Ollama ✅                                        │
│   ├─ vLLM ✅                                          │
│   ├─ AWS Bedrock 🔄                                   │
│   ├─ Azure OpenAI 🔄                                  │
│   └─ Google Vertex AI 🔄                              │
│   ❌ Anthropic (violates "no external LLM")          │
│                                                        │
│ Airgapped Mode (zero network)                         │
│   ├─ Ollama ✅ (pre-staged models only)              │
│   ├─ vLLM ✅ (pre-staged models only)                │
│   ├─ Llama.cpp 💡 (pre-staged models only)           │
│   └─ LM Studio 💡 (pre-staged models only)           │
│   ❌ NO Cloud providers                               │
└────────────────────────────────────────────────────────┘
```

---

## Current Providers (Fully Implemented ✅)

### 1. Ollama

| Aspect | Details |
|--------|---------|
| **Status** | ✅ Complete (Tasks 005, 005a, 005c, 005d) |
| **Website** | https://ollama.ai |
| **Type** | Local service, CPU/GPU |
| **Models Supported** | 100+ (Llama, Mistral, Neural Chat, etc.) |
| **Inference API** | HTTP REST API (custom format) |
| **Task Assignments** | |
| - Core Adapter | Task 005a - Request/Response + Streaming |
| - Smoke Tests | Task 005c - Setup Docs + Test Scripts |
| - Lifecycle | Task 005d - Auto-start, Health, Recovery |
| **Lifecycle Management** | ✅ Full implementation in 005d |
| **Operating Modes** | LocalOnly, Burst, Airgapped |
| **GPU Support** | ✅ NVIDIA, AMD (optional) |
| **Model Pull** | ✅ Automatic via lifecycle manager |
| **CLI Commands** | `acode providers start/stop/status ollama` |
| **Configuration** | `.agent/config.yml` → `providers.ollama.lifecycle` |

**Strengths**:
- Simple installation (single binary)
- Works CPU-only (great for testing)
- No GPU drivers required for basic use
- Excellent documentation

**Limitations**:
- Slower than vLLM
- CPU-only quite slow
- Limited to Ollama's curated models

---

### 2. vLLM

| Aspect | Details |
|--------|---------|
| **Status** | ✅ Complete (Tasks 006, 006a, 006c, 006d) |
| **Website** | https://github.com/lm-sys/vllm |
| **Type** | Local service, GPU-optimized |
| **Models Supported** | 1000+ Huggingface models |
| **Inference API** | OpenAI-compatible REST API |
| **Task Assignments** | |
| - Core Adapter | Task 006a - Serving + Client Adapter |
| - Health Check | Task 006c - Load/Health Endpoints |
| - Lifecycle | Task 006d - Auto-start, GPU Mgmt, Recovery |
| **Lifecycle Management** | ✅ Full implementation in 006d |
| **Operating Modes** | LocalOnly, Burst, Airgapped |
| **GPU Support** | ✅ NVIDIA (primary), AMD (secondary) |
| **Model Pull** | ✅ Automatic via Huggingface |
| **CLI Commands** | `acode providers start/stop/status vllm` |
| **Configuration** | `.agent/config.yml` → `providers.vllm.lifecycle` |

**Strengths**:
- 10x faster than Ollama (GPU-optimized)
- Access to latest Huggingface models
- Production-ready inference serving
- Structured outputs support

**Limitations**:
- GPU required for reasonable performance
- More complex setup (Python + CUDA)
- Higher memory overhead

---

## Planned Providers (Cloud Burst Phase - Task 029+)

### 3. AWS Bedrock

| Aspect | Details |
|--------|---------|
| **Status** | 🔄 Planned (Task 029+) |
| **Website** | https://aws.amazon.com/bedrock |
| **Type** | Managed cloud service |
| **Models** | Claude, Llama 2, Mistral, etc. |
| **API** | AWS SDK (boto3 + Bedrock Runtime) |
| **Lifecycle** | No process management (cloud-hosted) |
| **Operating Modes** | Burst mode only |
| **Cost Model** | Pay-per-1K tokens |
| **Task Assignment** | Task 029+ (Cloud Burst Orchestrators) |
| **Estimated Tasks** | 1-2 subtasks (adapter + cost tracking) |
| **Priority** | HIGH (AWS most popular for burst) |

**Rationale for Selection**:
- ✅ No "external LLM API" violation (Bedrock hosts AWS models like Llama)
- ✅ Enterprise support
- ✅ No rate limiting concerns (AWS managed)
- ✅ Cost tracking/quota support

**Implementation Needs**:
1. CloudServiceOrchestrator wrapper
2. BedrockProvider adapter
3. Cost tracking integration
4. Quota enforcement

---

### 4. Azure OpenAI

| Aspect | Details |
|--------|---------|
| **Status** | 🔄 Planned (Task 029+) |
| **Website** | https://azure.microsoft.com/en-us/products/cognitive-services/openai-service |
| **Type** | Managed cloud service |
| **Models** | GPT-4, GPT-3.5 (Azure-hosted) |
| **API** | Azure SDK + OpenAI compatibility |
| **Lifecycle** | No process management (cloud-hosted) |
| **Operating Modes** | Burst mode only |
| **Cost Model** | Pay-per-1K tokens (with quotas) |
| **Task Assignment** | Task 029+ (Cloud Burst Orchestrators) |
| **Estimated Tasks** | 1-2 subtasks |
| **Priority** | MEDIUM (Enterprise) |

**Rationale for Selection**:
- ✅ Enterprise availability/SLA
- ✅ Azure ecosystem integration
- ✅ Regional availability
- ⚠️ More expensive than Bedrock for same models

---

### 5. Google Vertex AI

| Aspect | Details |
|--------|---------|
| **Status** | 🔄 Planned (Task 029+) |
| **Website** | https://cloud.google.com/vertex-ai |
| **Type** | Managed cloud service |
| **Models** | PaLM 2, Gemini, custom models |
| **API** | Google Cloud SDK |
| **Lifecycle** | No process management (cloud-hosted) |
| **Operating Modes** | Burst mode only |
| **Cost Model** | Pay-per-1K tokens |
| **Task Assignment** | Task 029+ (Cloud Burst Orchestrators) |
| **Estimated Tasks** | 1-2 subtasks |
| **Priority** | LOW (Smaller market share) |

**Rationale for Selection**:
- ✅ Multi-modal models (Gemini)
- ✅ Good for research/enterprise
- ⚠️ Smaller user base

---

## Optional Future Providers (Lower Priority 💡)

### 6. Llama.cpp

| Aspect | Details |
|--------|---------|
| **Status** | 💡 Optional (not assigned task) |
| **Website** | https://github.com/ggerganov/llama.cpp |
| **Type** | Local binary, CPU-optimized |
| **Characteristics** | Single executable, minimal dependencies |
| **Advantages** | Lightweight, great for mobile/embedded |
| **Disadvantages** | Single quantized model at a time |
| **When to Add** | If mobile/embedded support needed |

---

### 7. LM Studio

| Aspect | Details |
|--------|---------|
| **Status** | 💡 Optional (not assigned task) |
| **Website** | https://lmstudio.ai |
| **Type** | Local GUI application |
| **Characteristics** | User-friendly, model management UI |
| **Advantages** | No CLI knowledge needed |
| **Disadvantages** | Harder to automate than CLI |
| **When to Add** | If GUI-first users important |

---

## Excluded Providers (❌ By Design)

### Anthropic API

| Reason for Exclusion | Details |
|-----|---|
| **Philosophy Violation** | Acode's core principle: "NO external LLM API by default" |
| **Design Intent** | LocalOnly mode should work entirely offline with Ollama/vLLM |
| **Operating Modes** | Would force Burst mode (violates LocalOnly philosophy) |
| **Alternative** | Use AWS Bedrock or Azure (which host open models, not Anthropic) |
| **Future Consideration** | Could add as explicit "Anthropic Mode" if requirements change |

**Key Point**: The system architecture intentionally prevents accidental use of external APIs. Anthropic would require explicit opt-in and architectural changes.

---

## Task Assignment Map

### Implemented Tasks

```
Task 004: Model Provider Interface (Foundation)
├── Task 004a: Message/Tool-Call Types
├── Task 004b: Response Format + Usage
└── Task 004c: Provider Registry + Selection

Task 005: Ollama Provider Adapter
├── Task 005a: Request/Response + Streaming ✅
├── Task 005c: Setup Docs + Smoke Tests ✅
└── Task 005d: Ollama Lifecycle Management ✅ (NEW)

Task 006: vLLM Provider Adapter
├── Task 006a: Serving + Client Adapter ✅
├── Task 006c: Load/Health Endpoints ✅
└── Task 006d: vLLM Lifecycle Management ✅ (NEW)
```

### Missing Subtask Clarification

**Why is 005b and 006b missing?**

- **005b (Tool-call parsing)**: Moved to Task 007d (Tool Schema Registry)
  - Tool calling is cross-provider concern
  - Better to centralize in Tool Registry
  - Both Ollama and vLLM use same parsing logic

- **006b (Structured outputs)**: Assigned to Task 006c
  - Merged with health check implementation
  - vLLM API includes structured outputs
  - Not separate concern like Ollama has

### Planned Cloud Provider Tasks

```
Task 029: Cloud Burst Compute (Parent Task)
├── Task 029a: AWS Bedrock Adapter
├── Task 029b: Azure OpenAI Adapter
├── Task 029c: Google Vertex AI Adapter
├── Task 029d: Cost Tracking + Quotas
└── Task 029e: Multi-region Failover
```

---

## Lifecycle Management Companion Tasks

### Pattern Recognition

**When a provider needs lifecycle management** (local service running on user's machine):
- Create `Lifecycle Management` subtask (d, e, f, etc.)
- Implement service start/stop/health/recovery
- Add configuration for operating modes
- Integrate with ProviderRegistry

### Current Lifecycle Tasks

| Provider | Adapter Task | Lifecycle Task | Status |
|----------|--------------|----------------|--------|
| Ollama | 005a | **005d** | ✅ Complete |
| vLLM | 006a | **006d** | ✅ Complete |
| Llama.cpp | TBD | TBD | 💡 Future |
| LM Studio | TBD | TBD | 💡 Future |
| AWS Bedrock | 029a | (N/A - cloud) | 🔄 Planned |
| Azure OpenAI | 029b | (N/A - cloud) | 🔄 Planned |
| Google Vertex | 029c | (N/A - cloud) | 🔄 Planned |

---

## Selection Criteria

### Why These 2+3 Providers?

**Local Providers (Ollama + vLLM)**
1. ✅ Enable LocalOnly mode (no external APIs)
2. ✅ Privacy-first architecture
3. ✅ Zero infrastructure cost
4. ✅ Works offline/airgapped
5. ✅ Complementary: Ollama simple, vLLM fast

**Cloud Providers (AWS, Azure, GCP)**
1. ✅ Enable Burst mode scaling
2. ✅ Latest models without GPU
3. ✅ Enterprise SLA/support
4. ✅ Cost-effective for occasional use
5. ✅ Don't violate "no external LLM API" (they host open models)

---

## Provider Coverage Map

### By Use Case

| Use Case | Best Provider | Alternative | Avoid |
|----------|---------------|-------------|-------|
| **Hobby/Learning** | Ollama | vLLM (if GPU) | - |
| **Quick Testing** | Ollama (CPU) | - | - |
| **Performance** | vLLM | - | Ollama (slow) |
| **Enterprise** | Azure OpenAI | AWS Bedrock | - |
| **Cost-Conscious** | Ollama/vLLM | - | Cloud |
| **Privacy-Critical** | Ollama/vLLM | - | Any cloud |
| **Offline/Airgapped** | Ollama/vLLM | - | Cloud |
| **GPU-Limited** | AWS Bedrock | Azure | vLLM |

### By Operating Mode

| Mode | Supported Providers | Requirement |
|------|---------------------|-------------|
| **LocalOnly** | Ollama, vLLM | No network |
| **Burst** | Ollama, vLLM, Bedrock, Azure, GCP | Can use cloud |
| **Airgapped** | Ollama, vLLM (pre-staged) | Zero network |

---

## Why No Ollama 006b, vLLM 006b?

### Investigation Results

Reading Task 005 and 006 specifications:

**Task 005 (Ollama)**:
```
## Out of Scope
- Ollama process management
- Model downloading
- Ollama version upgrades
- Multi-Ollama routing
```

↓ This is exactly what Task **005d** (Ollama Lifecycle) addresses!

**Task 006 (vLLM)**:
```
## Out of Scope
- vLLM installation
- GPU driver management
- Model caching strategy
```

↓ Task **006d** (vLLM Lifecycle) handles the process, not installation/drivers.

**Missing subtasks 005b, 006b** are intentional:
- **005b (Tool calling)** → Moved to Task 007d (cross-provider concern)
- **006b (Structured outputs)** → Integrated into Task 006c (health check endpoint)

---

## Roadmap Summary

### Q1 2026 (Current)
- ✅ Ollama Lifecycle (005d) - COMPLETE
- ✅ vLLM Lifecycle (006d) - COMPLETE
- 🔄 Ecosystem stabilization, testing

### Q2 2026 (Planned)
- 🔄 AWS Bedrock (Task 029a)
- 🔄 Azure OpenAI (Task 029b)
- 🔄 Cost tracking (Task 029d)

### Q3 2026 (Planned)
- 🔄 Google Vertex AI (Task 029c)
- 🔄 Multi-region failover (Task 029e)
- 💡 Optional local providers (Llama.cpp, LM Studio)

### Future (Backlog)
- 💡 Anthropic (if philosophy changes)
- 💡 Distributed inference
- 💡 Model recommendation engine

---

## Conclusion

Acode's provider strategy:

1. **Start local**: Ollama and vLLM cover all local use cases
2. **Add cloud**: AWS, Azure, GCP for burst computing
3. **Maintain privacy**: No external LLM APIs by default
4. **Enable extensibility**: Pattern-based architecture for future providers

This balanced approach ensures users can choose: **fast & simple (Ollama)**, **GPU-optimized (vLLM)**, or **cloud scalable (AWS/Azure/GCP)** — all with consistent experience.

---

**End of Document**
