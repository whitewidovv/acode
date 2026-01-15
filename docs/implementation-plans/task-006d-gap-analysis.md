# Task 006d: vLLM Lifecycle Management - Comprehensive Gap Analysis

**Date**: January 15, 2026
**Methodology**: CLAUDE.md Section 3.2 + GAP_ANALYSIS_METHODOLOGY.md
**Specification**: docs/tasks/refined-tasks/Epic 01/task-006d-vllm-lifecycle-management.md (717 lines)
**Reference Pattern**: Task 005d (Ollama Lifecycle) - proven pattern to follow

---

## EXECUTIVE SUMMARY

**Spec Requirements**:
- **717 total lines** with 50+ FRs, 19 NFRs, 44 ACs, 21 tests, 15 implementation items
- **File structure**: 20+ files across Domain/Application/Infrastructure/Tests layers
- **Scope**: vLLM service orchestration with startup, health monitoring, model loading, GPU management, crash recovery

**Current Implementation State**:
- ✅ Task 006c COMPLETE: vLLM Health Check endpoints, error handling (64 tests passing)
- ❌ Task 006d MISSING: All lifecycle orchestration, state machine, GPU/model managers
- **Completion**: 0% (no lifecycle files exist yet)

**Gap Breakdown**:
| Category | Required | Complete | Missing | Status |
|----------|----------|----------|---------|--------|
| Domain Layer | 4 files | 0 | 4 | ❌ MISSING |
| Application Layer | 2 files | 0 | 2 | ❌ MISSING |
| Infrastructure | 8 files | 0 | 8 | ❌ MISSING |
| Tests | 7 files | 0 | 7 | ❌ MISSING |
| **TOTAL** | **21 files** | **0** | **21** | **0% COMPLETE** |

**Expected Implementation Scope**:
- **Production Code**: 2000+ lines (enums, configs, orchestrator, helpers)
- **Test Code**: 1500+ lines (100+ tests - unit, integration, performance)
- **Test Count Expected**: 10 unit + 7 integration + 4 performance = 21+ tests minimum

---

## SPECIFICATION ANALYSIS

### Functional Requirements Summary (50 FRs)

**FR-001 to FR-005**: Service Orchestrator Interface
- Must mirror OllamaServiceOrchestrator interface
- Accept model ID in constructor (Huggingface format)
- Support 3 lifecycle modes: Managed/Monitored/External

**FR-006 to FR-015**: vLLM-Specific Startup
- Start via `python -m vllm.entrypoints.openai.api_server` or `vllm serve`
- Pass `--model <model-id>` for Huggingface models
- GPU configuration (tensor parallelism, memory utilization)
- Error detection: GPU unavailable, model not found, port conflict

**FR-016 to FR-020**: Health Monitoring
- Use `/health` endpoint + `/v1/models` for model verification
- 5-second timeout (same as Ollama)
- Auto-restart on failed checks

**FR-021 to FR-027**: Model Management
- Ensure configured model available/loadable
- Lazy loading (default vLLM behavior)
- Configurable load timeout (default 300s)
- Airgapped mode blocks HF registry fetching
- Model ID validation (org/model-name format)
- Model switching restarts service

**FR-028 to FR-031**: GPU Configuration
- Memory utilization configurable (0.0-1.0, default 0.9)
- Tensor parallelism for multi-GPU
- Pipeline parallelism (if supported)

**FR-032 to FR-039**: Restart Policy & Configuration
- Same crash detection as Ollama (PID-based)
- Max 3 restarts per 60 seconds
- 2-second minimum wait before retry
- VllmLifecycleOptions with: Mode, StartTimeout, HealthCheckInterval, MaxRestarts, Port
- Load from `providers.vllm.lifecycle` config section
- Model ID via config, CLI, or env var override

**FR-040 to FR-051**: Error Reporting & Integration
- Detailed error messages for port, model, auth, GPU, timeout issues
- Integration with ProviderRegistry
- EnsureHealthyAsync() calls from registry

### Non-Functional Requirements (19 NFRs)

| Category | Target |
|----------|--------|
| EnsureHealthyAsync() when running | <50ms |
| EnsureHealthyAsync() when restarting | 2-5 seconds |
| Health check latency | <100ms |
| Model lazy load | <30 seconds |
| Startup overhead | 5-15 seconds |
| Memory overhead | <20 MB |
| Idle CPU usage | <1% |
| GPU monitoring overhead | <5% CPU |
| Restart success rate | >95% |
| Health check accuracy | >95% |
| Model load success | >95% |
| vLLM version support | 0.4.0+ |

### Acceptance Criteria (44 ACs)

**AC-001 to AC-005**: Auto-Start (Managed Mode)
**AC-006 to AC-013**: Model Management (lazy load, validation, auth, airgapped)
**AC-014 to AC-017**: Health Monitoring (endpoints, model verification, restart triggers)
**AC-018 to AC-021**: Crash Recovery (detection, restart, limits, history)
**AC-022 to AC-027**: Configuration (loading, env override, validation)
**AC-028 to AC-031**: GPU Configuration (memory, tensor parallelism, detection)
**AC-032 to AC-035**: Status Reporting (state, model, GPU, health)
**AC-036 to AC-040**: Error Handling (port, model, GPU, auth, timeout)
**AC-041 to AC-044**: Integration (registry, failures, reconnect, retry)

### Testing Requirements (21+ tests)

**Unit Tests (10)**: UT-001 to UT-010
- Startup with valid model, port detection, GPU detection, model not found
- Health checks (/health and /v1/models endpoints)
- Model ID validation, config loading, restart limits, airgapped mode

**Integration Tests (7)**: IT-001 to IT-007
- End-to-end startup → model load → request
- Model switching with restart
- Crash recovery and auto-restart
- GPU detection
- HF authentication
- Multi-GPU tensor parallelism
- Status reporting

**Performance Tests (4)**: PT-001 to PT-004
- Startup with 7B model (<15 seconds)
- Health check latency (<100ms)
- Model lazy load (<30 seconds)
- GPU monitoring overhead (<5% CPU)

---

## CURRENT IMPLEMENTATION STATE

### VERIFIED: Existing Infrastructure (Do NOT recreate)

✅ **Task 006c COMPLETE** (64 tests passing):
- `src/Acode.Infrastructure/Vllm/Health/VllmHealthChecker.cs` - Health check logic
- `src/Acode.Infrastructure/Vllm/Health/VllmHealthConfiguration.cs` - Health config with BaseUrl, ModelsEndpoint
- `src/Acode.Infrastructure/Vllm/Health/Metrics/VllmMetricsClient.cs` - Prometheus metrics
- `src/Acode.Infrastructure/Vllm/Health/Metrics/VllmMetricsParser.cs` - Metrics parsing
- `src/Acode.Infrastructure/Vllm/Health/VllmLoadStatus.cs` - Load status tracking
- `src/Acode.Infrastructure/Vllm/VllmProvider.cs` - HTTP provider implementation
- `src/Acode.Infrastructure/Vllm/Client/VllmHttpClient.cs` - HTTP client
- Exception types in `src/Acode.Infrastructure/Vllm/Exceptions/`

✅ **Pattern Reference - Ollama Lifecycle** (Task 005d - working reference):
- `src/Acode.Domain/Providers/Ollama/OllamaServiceState.cs` - Enum: Running/Starting/Stopping/Stopped/Failed/Crashed/Unknown
- `src/Acode.Domain/Providers/Ollama/OllamaLifecycleMode.cs` - Enum: Managed/Monitored/External
- `src/Acode.Domain/Providers/Ollama/ModelPullProgress.cs` - Progress tracking
- `src/Acode.Domain/Providers/Ollama/ModelPullResult.cs` - Pull result
- `src/Acode.Application/Providers/Ollama/OllamaLifecycleOptions.cs` - Configuration class
- `src/Acode.Infrastructure/Providers/Ollama/Lifecycle/OllamaServiceOrchestrator.cs` - Main orchestrator
- `src/Acode.Infrastructure/Providers/Ollama/Lifecycle/ServiceStateTracker.cs` - State machine
- `src/Acode.Infrastructure/Providers/Ollama/Lifecycle/HealthCheckWorker.cs` - Background health checks
- `src/Acode.Infrastructure/Providers/Ollama/Lifecycle/RestartPolicyEnforcer.cs` - Restart rate limiting
- `src/Acode.Infrastructure/Providers/Ollama/Lifecycle/ModelPullManager.cs` - Model pull logic

### MISSING: Task 006d Implementation (20 files)

#### DOMAIN LAYER (4 files)

**❌ Missing: `src/Acode.Domain/Providers/Vllm/VllmServiceState.cs`**
- Type: Enum
- Size: ~50 lines
- Expected values: Running, Starting, Stopping, Stopped, Failed, Crashed, Unknown (7 values)
- Mirrors: OllamaServiceState.cs (same pattern)
- Tests: VllmServiceStateTests (5+ test methods)

**❌ Missing: `src/Acode.Domain/Providers/Vllm/VllmLifecycleMode.cs`**
- Type: Enum
- Size: ~40 lines
- Expected values: Managed, Monitored, External (3 values)
- Mirrors: OllamaLifecycleMode.cs (same pattern)
- Tests: VllmLifecycleModeTests (6+ test methods)

**❌ Missing: `src/Acode.Domain/Providers/Vllm/ModelLoadProgress.cs`**
- Type: Class (sealed record-like)
- Size: ~80 lines
- Properties: ModelId, ProgressPercent, BytesDownloaded, TotalBytes, Status, StartedAt, CompletedAt
- Factory methods: FromDownloading(), FromComplete()
- Tests: ModelLoadProgressTests (8+ test methods)

**❌ Missing: `src/Acode.Domain/Providers/Vllm/GpuInfo.cs`**
- Type: Class (sealed record-like)
- Size: ~60 lines
- Properties: DeviceId, Name, TotalMemory, AvailableMemory, UtilizationPercent, Temperature
- Tests: GpuInfoTests (6+ test methods)

#### APPLICATION LAYER (2 files)

**❌ Missing: `src/Acode.Application/Providers/Vllm/IVllmServiceOrchestrator.cs`**
- Type: Interface
- Size: ~80 lines
- Methods: EnsureHealthyAsync(model?), StartAsync(model), StopAsync(), GetStatusAsync(), RestartAsync()
- Returns: IDisposable, Task<VllmStatus>, Task<IReadOnlyList<GpuInfo>>, etc.
- Mirrors: IOllamaServiceOrchestrator pattern

**❌ Missing: `src/Acode.Application/Providers/Vllm/VllmLifecycleOptions.cs`**
- Type: Class (configuration)
- Size: ~100 lines
- Properties: Mode, StartTimeoutSeconds, HealthCheckIntervalSeconds, MaxRestartsPerMinute, ModelLoadTimeoutSeconds, Port, StopOnExit, GpuMemoryUtilization, TensorParallelSize
- Methods: Validate() with exception on invalid config
- Mirrors: OllamaLifecycleOptions.cs

#### INFRASTRUCTURE LAYER (8 files)

**❌ Missing: `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmServiceOrchestrator.cs`**
- Type: Class (main orchestrator)
- Size: 350-400 lines
- Implements: IVllmServiceOrchestrator, IDisposable
- Dependencies: IVllmProvider, IProcessRunner, ILogger, VllmHealthChecker, GpuMonitor, ModelLoader, StateTracker, RestartPolicyEnforcer
- Key methods:
  - StartAsync(): Start vLLM process with model
  - StopAsync(): Graceful shutdown
  - EnsureHealthyAsync(): Check health, restart if failed
  - GetStatusAsync(): Current service state
  - RestartAsync(): Forceful restart
- Mirrors: OllamaServiceOrchestrator.cs pattern

**❌ Missing: `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmServiceStateTracker.cs`**
- Type: Class (state machine)
- Size: 150-180 lines
- Properties: CurrentState (VllmServiceState), ProcessId, UpSinceUtc, LastHealthCheckUtc, LastHealthCheckStatus
- Methods: Transition(newState), MarkHealthy(), MarkUnhealthy(), GetDiagnostics()
- Thread-safe: Yes (lock-based)
- Mirrors: ServiceStateTracker.cs (Ollama pattern)

**❌ Missing: `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmHealthCheckWorker.cs`**
- Type: Class (background worker)
- Size: 150-180 lines
- Implements: IDisposable, BackgroundService (or Timer-based)
- Responsibility: Periodic health checks, failure counting, auto-restart trigger
- Methods: StartAsync(), StopAsync(), ExecuteAsync() (worker loop)
- Mirrors: HealthCheckWorker.cs (Ollama pattern)

**❌ Missing: `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmRestartPolicyEnforcer.cs`**
- Type: Class (rate limiter)
- Size: 120-150 lines
- Responsibility: Track restart history, enforce max 3 restarts per 60 seconds
- Methods: CanRestart(), RecordRestart(), Reset(), GetHistory()
- Mirrors: RestartPolicyEnforcer.cs (Ollama pattern)

**❌ Missing: `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmGpuMonitor.cs`**
- Type: Class (GPU-specific health)
- Size: 200-250 lines
- Responsibility: Detect GPU availability, monitor utilization, report errors
- Methods: GetAvailableGpuAsync(), GetUtilizationAsync(), GetTemperatureAsync(), DetectGpuError()
- GPU detection: nvidia-smi or rocm-smi commands
- vLLM-specific: Reports as GpuInfo[] with device IDs, memory, utilization

**❌ Missing: `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmModelLoader.cs`**
- Type: Class (Huggingface model management)
- Size: 200-250 lines
- Responsibility: Validate model IDs, check HF token, detect airgapped mode, handle loading errors
- Methods: ValidateModelIdAsync(modelId), CanLoadModelAsync(modelId), GetModelInfoAsync(modelId), LoadModelAsync(modelId)
- Format validation: Ensure org/model-name format
- Airgapped handling: Check local cache only, reject HF registry fetches
- Error handling: 401 (auth), 404 (not found), timeout, etc.

**❌ Missing: `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmProcessRunner.cs`** (or reuse existing)
- Type: Class or interface
- Responsibility: Start/stop vLLM process, handle stdout/stderr
- Methods: StartProcessAsync(command, args), TerminateAsync(processId), GetProcessStatusAsync(processId)
- Command format: `python -m vllm.entrypoints.openai.api_server --model <model> --port <port>`

#### TEST LAYER (7 files)

**❌ Missing: `tests/Acode.Infrastructure.Tests/Providers/Vllm/Lifecycle/VllmServiceStateTests.cs`**
- Test count: 5+ tests
- Coverage: Enum values exist, accessible, correct numeric values, ToString(), Parse()

**❌ Missing: `tests/Acode.Infrastructure.Tests/Providers/Vllm/Lifecycle/VllmLifecycleModeTests.cs`**
- Test count: 6+ tests
- Coverage: All 3 modes, default, accessible, ToString(), Parse(), documentation

**❌ Missing: `tests/Acode.Infrastructure.Tests/Providers/Vllm/Lifecycle/ModelLoadProgressTests.cs`**
- Test count: 8+ tests
- Coverage: Factory methods, progress calculation, immutability, completion state

**❌ Missing: `tests/Acode.Infrastructure.Tests/Providers/Vllm/Lifecycle/GpuInfoTests.cs`**
- Test count: 6+ tests
- Coverage: Construction, property access, immutability, error states

**❌ Missing: `tests/Acode.Infrastructure.Tests/Providers/Vllm/Lifecycle/VllmServiceOrchestratorTests.cs`**
- Test count: 20+ tests
- Coverage: StartAsync (valid model, port conflict, GPU error), StopAsync, EnsureHealthyAsync, GetStatusAsync, RestartAsync, failure handling, mode behavior (Managed/Monitored/External)

**❌ Missing: `tests/Acode.Infrastructure.Tests/Providers/Vllm/Lifecycle/VllmGpuMonitorTests.cs`**
- Test count: 10+ tests
- Coverage: GPU detection, nvidia-smi/rocm-smi parsing, error reporting, availability states

**❌ Missing: `tests/Acode.Integration.Tests/Providers/Vllm/Lifecycle/VllmLifecycleIntegrationTests.cs`**
- Test count: 7+ integration tests
- Coverage: E2E startup, model switching, crash recovery, GPU detection, HF auth, multi-GPU, status reporting

---

## IMPLEMENTATION STRATEGY

### Phase 1: Domain Layer (Foundation - No Dependencies)

**Estimated Effort**: 1-2 hours
**Output**: 4 enum/class files, 25+ unit tests

1. Create VllmServiceState enum + tests (5 tests)
2. Create VllmLifecycleMode enum + tests (6 tests)
3. Create ModelLoadProgress class + tests (8 tests)
4. Create GpuInfo class + tests (6 tests)

**Acceptance**: All files compile, no NotImplementedException, 25+ tests passing, 0 warnings

### Phase 2: Application Layer (Configuration - Depends on Phase 1)

**Estimated Effort**: 1-1.5 hours
**Output**: 2 files, 12+ unit tests

1. Create IVllmServiceOrchestrator interface (no tests needed for interface)
2. Create VllmLifecycleOptions class + tests (12+ tests for config validation, property access, defaults)

**Acceptance**: Files compile, VllmLifecycleOptions validates properly, 12+ tests passing

### Phase 3: Helper Components (Infrastructure - Depends on Phase 2)

**Estimated Effort**: 3-4 hours
**Output**: 3 helper files, 25+ unit tests

1. Create VllmServiceStateTracker + tests (8 tests for state transitions, thread safety)
2. Create VllmRestartPolicyEnforcer + tests (8 tests for rate limiting, history)
3. Create VllmGpuMonitor + tests (10+ tests for GPU detection, parsing)

**Acceptance**: 25+ tests passing, no stubs

### Phase 4: Complex Components (Infrastructure - Depends on Phase 3)

**Estimated Effort**: 2-3 hours
**Output**: 2 files, 15+ unit tests

1. Create VllmModelLoader + tests (10+ tests for validation, error handling, airgapped)
2. Create VllmHealthCheckWorker + tests (5+ tests for background monitoring)

**Acceptance**: 15+ tests passing

### Phase 5: Main Orchestrator (Infrastructure - Depends on All Above)

**Estimated Effort**: 2-3 hours
**Output**: 1 file, 20+ unit tests

1. Create VllmServiceOrchestrator + tests (20+ tests for startup, stop, ensure healthy, restart, modes)

**Acceptance**: 20+ tests passing, implements IVllmServiceOrchestrator fully

### Phase 6: Integration Tests (Depends on Phase 5)

**Estimated Effort**: 1.5-2 hours
**Output**: 1 file, 7+ integration tests

1. Create VllmLifecycleIntegrationTests (7+ E2E scenarios)

**Acceptance**: 7+ integration tests passing

### Phase 7: DI Wiring & Final Integration

**Estimated Effort**: 0.5-1 hour
**Output**: Updates to ServiceCollectionExtensions, ProviderRegistry

1. Update ServiceCollectionExtensions.cs to register VllmServiceOrchestrator and dependencies
2. Update ProviderRegistry to call EnsureHealthyAsync() for vLLM
3. Update CLI commands if needed
4. Verify build: 0 errors, 0 warnings

**Acceptance**: Build clean, all 80+ tests passing, no regressions

---

## VERIFICATION CHECKLIST (Final Audit)

**File Count Verification**:
- [ ] Domain: 4 files (VllmServiceState, VllmLifecycleMode, ModelLoadProgress, GpuInfo)
- [ ] Application: 2 files (IVllmServiceOrchestrator, VllmLifecycleOptions)
- [ ] Infrastructure: 8 files (Orchestrator + 6 helpers + ProcessRunner/wiring)
- [ ] Tests: 7 files with 80+ test methods
- [ ] **Total: 21 files created**

**NotImplementedException Scan**:
- [ ] `grep -r "NotImplementedException" src/Acode.*/Providers/Vllm/Lifecycle/` → **NO MATCHES**
- [ ] `grep -r "NotImplementedException" tests/Acode.*.Tests/Providers/Vllm/Lifecycle/` → **NO MATCHES**

**Test Execution Verification**:
- [ ] `dotnet test --filter "VllmServiceState"` → 5/5 passing
- [ ] `dotnet test --filter "VllmLifecycleMode"` → 6/6 passing
- [ ] `dotnet test --filter "ModelLoadProgress"` → 8/8 passing
- [ ] `dotnet test --filter "GpuInfo"` → 6/6 passing
- [ ] `dotnet test --filter "VllmLifecycleOptions"` → 12+ passing
- [ ] `dotnet test --filter "VllmServiceStateTracker"` → 8+ passing
- [ ] `dotnet test --filter "VllmRestartPolicyEnforcer"` → 8+ passing
- [ ] `dotnet test --filter "VllmGpuMonitor"` → 10+ passing
- [ ] `dotnet test --filter "VllmModelLoader"` → 10+ passing
- [ ] `dotnet test --filter "VllmHealthCheckWorker"` → 5+ passing
- [ ] `dotnet test --filter "VllmServiceOrchestrator"` → 20+ passing
- [ ] `dotnet test --filter "VllmLifecycleIntegration"` → 7+ passing
- [ ] **Total: 80+ tests passing (100%)**

**Build Verification**:
- [ ] `dotnet build` → 0 errors, 0 warnings
- [ ] `dotnet build --configuration Release` → 0 errors, 0 warnings

**Spec Compliance Verification**:
- [ ] All 50 FRs implemented (grep verification per file)
- [ ] All 44 ACs semantically complete (test coverage verification)
- [ ] All 21 unit/integration/perf tests from Testing Requirements exist

**No Regression Verification**:
- [ ] Task 006c tests still passing (64 tests)
- [ ] Ollama lifecycle tests still passing (no changes to Ollama)
- [ ] Build still clean

---

## SUCCESS CRITERIA

Task 006d is COMPLETE when:
1. ✅ All 21 files created with real implementations (no stubs)
2. ✅ NO NotImplementedException found anywhere
3. ✅ 80+ tests passing (unit + integration + performance)
4. ✅ Build: 0 errors, 0 warnings (Debug and Release)
5. ✅ All 50 FRs mapped to implementation
6. ✅ All 44 ACs verified with tests
7. ✅ ProviderRegistry integration complete
8. ✅ Config schema updated
9. ✅ All commits pushed to feature branch

**If ANY criterion fails, task is INCOMPLETE.**

---

## REFERENCES

- **Specification**: docs/tasks/refined-tasks/Epic 01/task-006d-vllm-lifecycle-management.md
- **Reference Pattern**: docs/implementation-plans/task-005d-completion-checklist.md (Ollama - 461 lines)
- **Methodology**: docs/GAP_ANALYSIS_METHODOLOGY.md (1325 lines)
- **CLAUDE.md Section 3.2**: Gap Analysis and Completion Checklist

---

**Status**: READY FOR DETAILED COMPLETION CHECKLIST + IMPLEMENTATION
