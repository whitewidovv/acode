# Task 006d: vLLM Lifecycle Management - Fresh Gap Analysis

**Date**: January 15, 2026
**Methodology**: CLAUDE.md Section 3.2 - Fresh Gap Analysis (treating as if none done before)
**Specification**: `docs/tasks/refined-tasks/Epic 01/task-006d-vllm-lifecycle-management.md` (717 lines)
**Related Task (Reference Pattern)**: Task 005d (Ollama Lifecycle Management) - completed with 24+ unit tests and full implementation

---

## Part 1: READ THE SPEC - Key Sections to Verify

### Specification Structure Review
- **Description** (lines 14-95): Business value, technical approach, integration points, assumptions
- **Functional Requirements** (lines 173-271): 50 FRs covering orchestrator, startup, health, model mgmt, GPU config, error handling, integration
- **Non-Functional Requirements** (lines 273-313): 19 NFRs covering performance, reliability, observability, compatibility
- **Acceptance Criteria** (lines 517-590): 44 ACs across auto-start, model mgmt, health monitoring, crash recovery, config, GPU, status, error handling, integration
- **Testing Requirements** (lines 592-630):
  - Unit tests: 10 required (FR validation, startup, health, model management, config, restart limits, airgapped)
  - Integration tests: 7 required (end-to-end startup, model switching, crash recovery, GPU detection, auth, multi-GPU, status reporting)
  - Performance tests: 4 required (startup latency, health check, model lazy load, GPU monitoring overhead)
- **Implementation Prompt** (lines 632-714): File structure, configuration example, error codes, implementation checklist

### Key Technical Requirements
1. **Service Orchestrator**: Parallel to OllamaServiceOrchestrator but vLLM-specific
2. **Operating Modes**: Managed, Monitored, External (same as Ollama)
3. **GPU Support**: NVIDIA/AMD GPU configuration, memory utilization monitoring
4. **Model Loading**: Huggingface models with lazy loading support
5. **Configuration**: From `providers.vllm.lifecycle` section in config.yml
6. **Health Monitoring**: `/health` endpoint + `/v1/models` for model verification
7. **Lifecycle Management**: Start/stop/restart with restart policy enforcement
8. **Error Handling**: GPU not available, model not found, auth required, port conflict, etc.

---

## Part 2: VERIFY CURRENT IMPLEMENTATION STATE

### Existing vLLM Infrastructure (DO NOT RECREATE)
- ✅ `src/Acode.Infrastructure/Vllm/` directory structure
- ✅ `VllmProvider.cs` - vLLM HTTP provider implementation
- ✅ `VllmHttpClient.cs` - HTTP client layer with retry/auth
- ✅ `VllmHealthChecker.cs` - Basic health checking (Task 006c - COMPLETE)
- ✅ `VllmHealthConfiguration.cs` - Health check config (Task 006c - COMPLETE, now includes BaseUrl + ModelsEndpoint)
- ✅ `VllmMetricsClient.cs` - Prometheus metrics client (Task 006c - COMPLETE)
- ✅ `VllmMetricsParser.cs` - Prometheus parser (Task 006c - COMPLETE)
- ✅ Various exception types in `Vllm/Exceptions/`
- ✅ Test structure: `tests/Acode.Infrastructure.Tests/Vllm/`

**IMPORTANT**: We are NOT recreating these. We build lifecycle management ON TOP.

### What MUST BE CREATED FOR TASK 006d (Domain/Application/Infrastructure Layers)

#### Phase 1: Domain Layer (Enums & Interfaces) - MISSING
- ❌ `src/Acode.Domain/Providers/Vllm/VllmServiceState.cs` - Service state enum (7 values: Running, Starting, Stopping, Stopped, Failed, Crashed, Unknown)
- ❌ `src/Acode.Domain/Providers/Vllm/VllmLifecycleMode.cs` - Lifecycle mode enum (3 values: Managed, Monitored, External)
- ❌ `src/Acode.Domain/Providers/Vllm/ModelLoadProgress.cs` - Model loading progress tracking

#### Phase 2: Application Layer (Configuration) - MISSING
- ❌ `src/Acode.Application/Providers/Vllm/IVllmServiceOrchestrator.cs` - Interface definition (7 methods minimum)
- ❌ `src/Acode.Application/Providers/Vllm/VllmLifecycleOptions.cs` - Configuration options (10+ properties)

#### Phase 3: Infrastructure Layer - Core Components - MISSING
- ❌ `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmServiceOrchestrator.cs` - Main orchestrator (300+ lines)
- ❌ `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmServiceStateTracker.cs` - State machine (150+ lines)
- ❌ `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmHealthCheckWorker.cs` - Background health monitoring (150+ lines)
- ❌ `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmRestartPolicyEnforcer.cs` - Restart rate limiting (100+ lines)
- ❌ `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmModelLoader.cs` - Huggingface model loading (150+ lines)
- ❌ `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmGpuMonitor.cs` - GPU monitoring (120+ lines)

#### Phase 4: DI Registration - MISSING
- ❌ `AddVllmLifecycleManagement()` extension method in ServiceCollectionExtensions.cs

#### Phase 5: Tests - MISSING
- ❌ `tests/Acode.Infrastructure.Tests/Providers/Vllm/Lifecycle/VllmServiceOrchestratorTests.cs` - 20+ unit tests
- ❌ `tests/Acode.Infrastructure.Tests/Providers/Vllm/Lifecycle/VllmServiceStateTrackerTests.cs` - 15+ unit tests
- ❌ `tests/Acode.Infrastructure.Tests/Providers/Vllm/Lifecycle/VllmHealthCheckWorkerTests.cs` - 10+ unit tests
- ❌ `tests/Acode.Infrastructure.Tests/Providers/Vllm/Lifecycle/VllmRestartPolicyEnforcerTests.cs` - 12+ unit tests
- ❌ `tests/Acode.Infrastructure.Tests/Providers/Vllm/Lifecycle/VllmGpuMonitorTests.cs` - 8+ unit tests
- ❌ `tests/Acode.Infrastructure.Tests/Providers/Vllm/Lifecycle/VllmModelLoaderTests.cs` - 10+ unit tests
- ❌ `tests/Acode.Integration.Tests/Providers/Vllm/Lifecycle/VllmLifecycleIntegrationTests.cs` - 7+ integration tests

---

## Part 3: GAP SUMMARY

### Total Implementation Gaps: 20 files / 1500+ lines of code

#### By Layer:
- **Domain Layer**: 3 files (enums + supporting types)
- **Application Layer**: 2 files (interface + configuration)
- **Infrastructure Layer**: 8 files (orchestrator + 6 helper components + DI)
- **Test Layer**: 7 test files (100+ tests total)

#### By Complexity:
- **Simple** (enums, config): 5 files - 400 lines
- **Medium** (helpers, GPU monitor): 5 files - 600 lines
- **Complex** (main orchestrator): 1 file - 300+ lines
- **Tests**: 7 files - 1000+ lines

---

## Part 4: IMPLEMENTATION ORDER (For TDD)

### Recommended Sequence (Tests First)

**Phase 1: Domain Enums** (Foundation - no dependencies)
1. VllmServiceState enum + tests
2. VllmLifecycleMode enum + tests

**Phase 2: Application Config** (Depends on Phase 1)
3. IVllmServiceOrchestrator interface + tests
4. VllmLifecycleOptions config + tests

**Phase 3: Helper Components** (Depends on Phase 2)
5. VllmServiceStateTracker + tests
6. VllmRestartPolicyEnforcer + tests
7. VllmGpuMonitor + tests
8. VllmModelLoader + tests
9. VllmHealthCheckWorker + tests

**Phase 4: Main Orchestrator** (Depends on all above)
10. VllmServiceOrchestrator + tests (integrates all helpers)

**Phase 5: Integration & DI** (Depends on Phase 4)
11. DI registration (AddVllmLifecycleManagement)
12. Integration tests (end-to-end scenarios)
13. Performance tests (startup latency, GPU overhead)

---

## Part 5: KEY DIFFERENCES FROM OLLAMA (vLLM-Specific)

1. **GPU Support**: Additional GPU monitoring and configuration
2. **Model Format**: Huggingface model IDs instead of Ollama model names
3. **Lazy Loading**: vLLM loads models on first request (default behavior)
4. **Port**: Default 8000 instead of 11434
5. **Multi-GPU**: Tensor parallelism configuration for multi-GPU setups
6. **Authentication**: HF_TOKEN support for private/gated models
7. **Airgapped Mode**: Special handling to prevent HF model registry access
8. **Startup Command**: `python -m vllm.entrypoints.openai.api_server` or `vllm serve`

---

## Part 6: SPECIFICATION COMPLIANCE CHECKLIST

### Functional Requirements (50 FRs)
- [ ] FR-001 to FR-009: Service Orchestrator interface (same as Ollama)
- [ ] FR-010 to FR-015: vLLM-specific startup with GPU, model loading
- [ ] FR-016 to FR-021: Health monitoring with /health and /v1/models endpoints
- [ ] FR-022 to FR-027: Model management with Huggingface format validation
- [ ] FR-028 to FR-031: GPU configuration (utilization, tensor parallelism)
- [ ] FR-032 to FR-035: Crash detection and restart policy (same as Ollama)
- [ ] FR-036 to FR-042: Configuration loading and validation
- [ ] FR-043 to FR-048: Error reporting with helpful messages
- [ ] FR-049 to FR-051: Integration with ProviderRegistry

### Acceptance Criteria (44 ACs)
- [ ] AC-001 to AC-005: Auto-start functionality
- [ ] AC-006 to AC-013: Model management (lazy loading, validation, auth, airgapped)
- [ ] AC-014 to AC-017: Health monitoring
- [ ] AC-018 to AC-021: Crash recovery
- [ ] AC-022 to AC-027: Configuration
- [ ] AC-028 to AC-031: GPU configuration
- [ ] AC-032 to AC-035: Status reporting
- [ ] AC-036 to AC-040: Error handling
- [ ] AC-041 to AC-044: Integration

### Testing Requirements
- [ ] 10 unit tests: Startup, health check, model management, config, restart, airgapped
- [ ] 7 integration tests: End-to-end, model switching, crash recovery, GPU, auth, multi-GPU, status
- [ ] 4 performance tests: Startup latency, health check, model lazy load, GPU overhead

---

## Next Steps

This gap analysis documents everything that must be built for task-006d. The recommended approach:

1. **Use task-005d as pattern reference** - Mirror Ollama's orchestrator design
2. **Follow TDD methodology** - Write tests before implementation
3. **Implement in phases** - Domain → Config → Helpers → Orchestrator → Tests
4. **Focus on vLLM-specific differences** - GPU support, Huggingface models, airgapped constraints
5. **Ensure spec compliance** - All 50 FRs and 44 ACs must be validated

Total estimated scope: 20 files, 1500+ lines of code, 100+ tests

---

**Status**: Ready for detailed implementation planning in completion checklist
