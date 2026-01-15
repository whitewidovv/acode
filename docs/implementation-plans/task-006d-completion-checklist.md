# Task 006d: vLLM Lifecycle Management - Implementation Checklist

**Status**: Ready for implementation
**Specification**: `docs/tasks/refined-tasks/Epic 01/task-006d-vllm-lifecycle-management.md` (717 lines)
**Gap Analysis**: `docs/implementation-plans/task-006d-gap-analysis.md` (188 lines)
**Reference Pattern**: Task 005d (Ollama Lifecycle) - Completed with 24+ unit tests
**Total Work**: 20 files, 1500+ lines, 100+ tests across 5 implementation phases

---

## INSTRUCTIONS FOR NEXT AGENT

This document is your complete implementation guide for task-006d. Follow these steps:

1. **Read this checklist completely** - Understand all gaps and success criteria
2. **Read the gap analysis** - `task-006d-gap-analysis.md` for context
3. **Reference task-005d** - Use Ollama implementation as pattern for vLLM
4. **Follow TDD strictly** - Write tests RED → GREEN → REFACTOR for each gap
5. **Commit after each gap** - One commit per logical unit of work
6. **Update checklist** - Mark [✅] when gap complete, track evidence
7. **Run tests frequently** - Ensure no regressions as you implement

---

## WHAT ALREADY EXISTS (DO NOT RECREATE)

✅ `src/Acode.Infrastructure/Vllm/` - Full provider infrastructure exists
✅ `VllmHealthChecker.cs` - Health checking implemented (Task 006c)
✅ `VllmHealthConfiguration.cs` - Config with BaseUrl + ModelsEndpoint (Task 006c)
✅ `VllmMetricsClient.cs` + `VllmMetricsParser.cs` - Metrics support (Task 006c)
✅ `VllmProvider.cs` - Main provider implementation
✅ `VllmHttpClient.cs` - HTTP client layer
✅ Exception hierarchy - Various VllmException types exist
✅ Test directory structure - `tests/Acode.Infrastructure.Tests/Vllm/`

**IMPORTANT**: Build ON TOP of existing components. Don't recreate health checking or metrics infrastructure.

---

## IMPLEMENTATION PHASES

### PHASE 1: Domain Layer Enums & Interfaces
**Objective**: Provide foundational types for lifecycle management
**Files**: 3 files (VllmServiceState.cs, VllmLifecycleMode.cs, ModelLoadProgress.cs)
**Dependencies**: None - Pure domain types

#### Gap #1.1: VllmServiceState Enum ⏳ PENDING
**File**: `src/Acode.Domain/Providers/Vllm/VllmServiceState.cs`
**Purpose**: Represent all possible states of vLLM service
**Specification**: lines 173-180 (see Ollama pattern: 7 states)
**Required Enum Values**:
- `Running` - Process healthy and responding
- `Starting` - Startup in progress
- `Stopping` - Graceful shutdown in progress
- `Stopped` - Cleanly stopped
- `Failed` - Failed to start or health check failed
- `Crashed` - Exited unexpectedly
- `Unknown` - Cannot determine state

**Implementation**:
- Public enum (not flags)
- Full XML documentation on enum and each value
- Include use case examples in comments
- Reference spec lines 173-180

**Tests** (`VllmServiceStateTests.cs`):
- [ ] Test enum has 7 values
- [ ] Test values accessible as expected
- [ ] Test ToString() representation
- [ ] 4+ tests minimum

**Success Criteria**:
- ✅ Enum compiles with 0 errors
- ✅ All 7 values present
- ✅ Full XML documentation
- ✅ 4+ unit tests passing

**Evidence**: [TBD - commit hash, test output]

---

#### Gap #1.2: VllmLifecycleMode Enum ⏳ PENDING
**File**: `src/Acode.Domain/Providers/Vllm/VllmLifecycleMode.cs`
**Purpose**: Three operating modes matching Ollama lifecycle
**Specification**: lines 173-180, FR-003, AC-041-044
**Required Enum Values**:
- `Managed` - Acode controls full lifecycle (default)
- `Monitored` - External manager (systemd) controls, Acode monitors
- `External` - Assumes always running, Acode just uses

**Implementation**:
- Public enum
- Document each mode with use case guidance (120+ lines)
- Reference Ollama pattern
- Match OllamaLifecycleMode behavior

**Tests** (`VllmLifecycleModeTests.cs`):
- [ ] Enum has 3 values
- [ ] Default/first value is Managed
- [ ] Mode-specific behavior documented
- [ ] 6+ tests minimum

**Success Criteria**:
- ✅ Compiles with 0 errors
- ✅ 3 values exactly
- ✅ Comprehensive documentation
- ✅ 6+ tests passing

**Evidence**: [TBD]

---

#### Gap #1.3: ModelLoadProgress Supporting Type ⏳ PENDING
**File**: `src/Acode.Domain/Providers/Vllm/ModelLoadProgress.cs`
**Purpose**: Track model loading progress for streaming
**Specification**: FR-022, streaming progress tracking
**Properties** (read-only):
- `string ModelId { get; }` - Model being loaded
- `double ProgressPercent { get; }` - 0-100
- `long? BytesDownloaded { get; }` - Current bytes
- `long? TotalBytes { get; }` - Total bytes (if known)
- `string Status { get; }` - "downloading", "extracting", "loaded"
- `bool IsProgressKnown { get; }` - True if BytesDownloaded/Total valid
- `DateTime StartedAt { get; }` - When load started
- `DateTime? CompletedAt { get; }` - When load completed (if done)

**Implementation**:
- Sealed class, immutable (init-only properties)
- Constructor validation
- Factory methods: `FromDownloading()`, `FromComplete()`, `FromFailed()`

**Tests** (`ModelLoadProgressTests.cs`):
- [ ] Can create downloading progress
- [ ] Can create complete progress
- [ ] IsProgressKnown works correctly
- [ ] CompletedAt null until completed
- [ ] 8+ tests

**Success Criteria**:
- ✅ All properties accessible
- ✅ Immutable (init-only)
- ✅ Factory methods work
- ✅ 8+ tests passing

**Evidence**: [TBD]

---

### PHASE 2: Application Layer Interface & Configuration
**Objective**: Define contract and configuration for lifecycle management
**Files**: 2 files (IVllmServiceOrchestrator.cs, VllmLifecycleOptions.cs)
**Dependencies**: Phase 1 enums

#### Gap #2.1: IVllmServiceOrchestrator Interface ⏳ PENDING
**File**: `src/Acode.Application/Providers/Vllm/IVllmServiceOrchestrator.cs`
**Purpose**: Contract for vLLM lifecycle orchestration
**Specification**: FR-001-009, spec implementation prompt section
**Required Methods** (match IOllamaServiceOrchestrator):
- `Task EnsureHealthyAsync(CancellationToken)` - Ensure service ready/healthy
- `Task<VllmServiceState> GetStateAsync(CancellationToken)` - Get current state
- `Task StartAsync(CancellationToken)` - Start vLLM process
- `Task StopAsync(CancellationToken)` - Stop gracefully
- `Task LoadModelAsync(string modelId, CancellationToken)` - Load HF model
- `IAsyncEnumerable<ModelLoadProgress> LoadModelStreamAsync(string modelId, CancellationToken)` - Stream progress
- `Task<string[]> GetLoadedModelsAsync(CancellationToken)` - List loaded models

**Implementation**:
- Public interface
- Full XML documentation
- Include vLLM-specific notes (Huggingface models, GPU, airgapped)
- Proper async signatures

**Tests** (`IVllmServiceOrchestratorTests.cs`):
- [ ] Interface compiles
- [ ] All 7 methods present
- [ ] Correct signatures
- [ ] Full documentation present
- [ ] 5+ tests

**Success Criteria**:
- ✅ Compiles with 0 errors
- ✅ 7 methods with correct signatures
- ✅ Full XML documentation
- ✅ References Huggingface, GPU, airgapped constraints

**Evidence**: [TBD]

---

#### Gap #2.2: VllmLifecycleOptions Configuration Class ⏳ PENDING
**File**: `src/Acode.Application/Providers/Vllm/VllmLifecycleOptions.cs`
**Purpose**: Configuration for vLLM lifecycle management
**Specification**: FR-036-042, config example lines 668-684
**Required Properties** (with defaults):
- `VllmLifecycleMode Mode { get; set; }` = Managed
- `int StartTimeoutSeconds { get; set; }` = 30
- `int HealthCheckIntervalSeconds { get; set; }` = 60
- `int MaxConsecutiveFailures { get; set; }` = 3
- `int MaxRestartsPerMinute { get; set; }` = 3
- `bool StopOnExit { get; set; }` = false
- `string VllmBinaryPath { get; set; }` = "vllm" or "python -m vllm"
- `int Port { get; set; }` = 8000
- `string? DefaultModel { get; set; }` = null
- `int ModelLoadTimeoutSeconds { get; set; }` = 300
- `int ModelLoadMaxRetries { get; set; }` = 2
- `int ShutdownGracePeriodSeconds { get; set; }` = 10
- `double GpuMemoryUtilization { get; set; }` = 0.9
- `int TensorParallelSize { get; set; }` = 1 (multi-GPU)

**Implementation**:
- Sealed class
- All properties public get/set
- Defaults match spec lines 668-684
- Includes vLLM-specific options (GPU, model timeout, binary path)

**Validation** (method):
- `void Validate()` - Throw ArgumentException if invalid
  - StartTimeoutSeconds > 0
  - HealthCheckIntervalSeconds > 0
  - MaxRestartsPerMinute >= 1
  - Port 1-65535
  - GpuMemoryUtilization 0.0-1.0
  - TensorParallelSize >= 1

**Tests** (`VllmLifecycleOptionsTests.cs`):
- [ ] Defaults set correctly
- [ ] Can deserialize from config
- [ ] Validate() accepts valid config
- [ ] Validate() rejects invalid timeouts
- [ ] Validate() rejects invalid port
- [ ] Validate() rejects invalid GPU utilization
- [ ] 12+ tests

**Success Criteria**:
- ✅ All properties present
- ✅ Defaults correct
- ✅ Validate() method works
- ✅ Throws on invalid config
- ✅ 12+ tests passing

**Evidence**: [TBD]

---

### PHASE 3: Infrastructure Helper Components
**Objective**: Build reusable helper components for lifecycle orchestration
**Files**: 5+ helper classes + tests
**Dependencies**: Phase 1 + Phase 2

[Note: Detailed Phase 3 continues in next section - helpers for state tracking, restart policy, health monitoring, GPU monitoring, model loading]

---

### PHASE 4: Main Orchestrator
**Objective**: Integrate all helpers into main VllmServiceOrchestrator
**Files**: Main orchestrator class + tests
**Dependencies**: All phases 1-3

---

### PHASE 5: Integration & Deployment
**Objective**: Register in DI, wire up to provider registry, integration tests
**Files**: DI registration, integration tests, performance tests
**Dependencies**: All phases 1-4

---

## CURRENT PROGRESS

| Phase | Component | Status | Tests | Evidence |
|-------|-----------|--------|-------|----------|
| 1.1 | VllmServiceState | ⏳ | 0/4 | [TBD] |
| 1.2 | VllmLifecycleMode | ⏳ | 0/6 | [TBD] |
| 1.3 | ModelLoadProgress | ⏳ | 0/8 | [TBD] |
| 2.1 | IVllmServiceOrchestrator | ⏳ | 0/5 | [TBD] |
| 2.2 | VllmLifecycleOptions | ⏳ | 0/12 | [TBD] |
| **PHASE 1+2** | **Domain+App** | **⏳** | **0/35** | **[TBD]** |

---

## TEST SUMMARY TARGETS

- **Phase 1 Tests**: 18 tests (enums, supporting types)
- **Phase 2 Tests**: 17 tests (interface, configuration)
- **Phase 3 Tests**: 45 tests (helpers - state, restart, health, GPU, model loader)
- **Phase 4 Tests**: 20 tests (main orchestrator)
- **Phase 5 Tests**: 10 tests (integration, performance)
- **TOTAL**: 110+ tests

---

## SUCCESS CRITERIA FOR TASK COMPLETION

- ✅ All 20 files created with 0 compilation errors
- ✅ All 110+ unit tests passing (100%)
- ✅ All 44 acceptance criteria validated
- ✅ All 50 functional requirements verified
- ✅ Build succeeds in both Debug and Release
- ✅ No StyleCop violations (SA1202, SA1214, etc.)
- ✅ Full XML documentation on all public members
- ✅ Comprehensive integration tests (end-to-end, crash recovery, GPU detection, model switching)
- ✅ Performance tests show startup <15s, health check <100ms, GPU overhead <5%
- ✅ Audit report documents all requirements coverage

---

## REFERENCE MATERIALS

**Specification File**:
- `docs/tasks/refined-tasks/Epic 01/task-006d-vllm-lifecycle-management.md` - Complete specification (717 lines)

**Gap Analysis**:
- `docs/implementation-plans/task-006d-gap-analysis.md` - Fresh gap analysis (188 lines)

**Reference Implementation** (Task 005d - Ollama):
- `src/Acode.Domain/Providers/Ollama/OllamaServiceState.cs` - State enum pattern
- `src/Acode.Domain/Providers/Ollama/OllamaLifecycleMode.cs` - Mode enum pattern
- `src/Acode.Application/Providers/Ollama/IOllamaServiceOrchestrator.cs` - Interface pattern
- `src/Acode.Application/Providers/Ollama/OllamaLifecycleOptions.cs` - Config pattern
- `src/Acode.Infrastructure/Providers/Ollama/Lifecycle/OllamaServiceOrchestrator.cs` - Orchestrator pattern
- `docs/implementation-plans/task-005d-completion-checklist.md` - Template for this document

**Related Task 006c Files** (vLLM Health Checking):
- `src/Acode.Infrastructure/Vllm/Health/VllmHealthChecker.cs` - Use for health monitoring
- `src/Acode.Infrastructure/Vllm/Health/VllmHealthConfiguration.cs` - Extend for lifecycle config
- `src/Acode.Infrastructure/Vllm/Health/Metrics/VllmMetricsClient.cs` - Use for GPU monitoring

---

## GETTING STARTED

1. **Read this checklist completely** - You're reading it now ✓
2. **Read the gap analysis** - `task-006d-gap-analysis.md`
3. **Review task-005d pattern** - See how Ollama lifecycle was built
4. **Start with Phase 1** - Enums are simplest, no dependencies
5. **Write tests FIRST** - Red → Green → Refactor for each gap
6. **Commit after each gap** - One logical unit per commit
7. **Update this checklist** - Mark [✅] and add evidence links
8. **Run full test suite** - Ensure no regressions
9. **When all phases complete** - Create comprehensive audit report
10. **Create PR** - With detailed description of implementation

---

**Status**: ⏳ Ready for implementation (waiting for next agent to begin Phase 1)
