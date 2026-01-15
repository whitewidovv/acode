# Task 005d: Ollama Lifecycle Management - Gap Analysis & Implementation Checklist

**Status**: Ready for implementation
**Specification File**: `docs/tasks/refined-tasks/Epic 01/task-005d-ollama-lifecycle-management.md` (1,143 lines, 53 KB)
**Complexity**: 8 Fibonacci points

---

## What Already Exists (DO NOT RECREATE)

### Ollama Provider Infrastructure (from Task 005a/c)
- ✅ `src/Acode.Infrastructure/Ollama/` directory structure
- ✅ HTTP client layer (`OllamaHttpClient.cs`)
- ✅ Request/response mapping (`OllamaRequestMapper.cs`, etc.)
- ✅ Health checking (`OllamaHealthChecker.cs`) - **Note**: Basic health checks exist, but orchestrator-level health management is new
- ✅ Exception hierarchy (various `OllamaException` classes)
- ✅ Configuration (`OllamaConfiguration.cs`)
- ✅ Smoke testing infrastructure (`SmokeTest/` folder)

**IMPORTANT**: We are NOT recreating these. We are building lifecycle management ON TOP of these existing components.

### Test Directories (Existing)
- ✅ `tests/Acode.Infrastructure.Tests/Ollama/` - existing test structure
- ✅ `tests/Acode.Integration.Tests/Ollama/` - existing integration test structure

---

## GAPS IDENTIFIED (What Must Be Built)

### Phase 1: Domain Layer (Enums & Interfaces)

#### Gap #1: OllamaServiceState Enum ⏳
- **File to Create**: `src/Acode.Domain/Providers/Ollama/OllamaServiceState.cs`
- **Purpose**: Spec section line 173+, FR-001 to FR-005
- **Enum Values Required**:
  - `Running` - Process is healthy and responding
  - `Starting` - Process is starting up
  - `Stopping` - Graceful shutdown in progress
  - `Stopped` - Process stopped cleanly
  - `Failed` - Process failed to start or health check failed
  - `Crashed` - Process exited unexpectedly
  - `Unknown` - State cannot be determined
- **Implementation Pattern**: Simple public enum with XML documentation
- **Success Criteria**: Enum compiles, all 7 values present, XML docs complete
- **Evidence**: [TBD]

#### Gap #2: OllamaLifecycleMode Enum ⏳
- **File to Create**: `src/Acode.Domain/Providers/Ollama/OllamaLifecycleMode.cs`
- **Purpose**: Spec section line 173+, FR-006 to FR-009
- **Enum Values Required**:
  - `Managed` - Acode controls full lifecycle (default)
  - `Monitored` - External service manager (systemd) controls, Acode monitors
  - `External` - Assumes always running, minimal overhead
- **XML Documentation**: Each mode explained in comments
- **Success Criteria**: Enum compiles, all 3 values with docs
- **Evidence**: [TBD]

#### Gap #3: IOllamaServiceOrchestrator Interface ⏳
- **File to Create**: `src/Acode.Application/Providers/Ollama/IOllamaServiceOrchestrator.cs`
- **Purpose**: Spec section "Implementation Prompt", FR-010 to FR-055
- **Methods Required** (from spec):
  ```csharp
  Task<OllamaServiceState> EnsureHealthyAsync(CancellationToken);
  Task<OllamaServiceState> GetStateAsync(CancellationToken);
  Task<OllamaServiceState> StartAsync(CancellationToken);
  Task<OllamaServiceState> StopAsync(CancellationToken);
  Task<ModelPullResult> PullModelAsync(string modelName, CancellationToken);
  Task<ModelPullResult> PullModelAsync(string modelName, Action<ModelPullProgress>? progressCallback, CancellationToken);
  IAsyncEnumerable<ModelPullProgress> PullModelStreamAsync(string modelName, CancellationToken);
  ```
- **XML Documentation**: Full docs for each method per spec
- **Success Criteria**: Interface compiles, all methods present with proper signatures
- **Evidence**: [TBD]

---

### Phase 2: Application Layer (Configuration)

#### Gap #4: OllamaLifecycleOptions Configuration ⏳
- **File to Create**: `src/Acode.Application/Providers/Ollama/OllamaLifecycleOptions.cs`
- **Purpose**: Spec line 320+, FR-055 to FR-087
- **Properties Required** (from spec):
  ```csharp
  OllamaLifecycleMode Mode { get; }
  TimeSpan StartTimeoutDuration { get; }
  TimeSpan HealthCheckInterval { get; }
  int MaxConsecutiveFailures { get; }
  int MaxRestartsPerMinute { get; }
  bool StopOnExit { get; }
  string OllamaBinaryPath { get; }
  int Port { get; }
  ```
- **Configuration Schema**: Support `.agent/config.yml` format (example in spec)
- **Validation**: Implement validation for timeout values (must be positive)
- **Success Criteria**: Options class compiles, all properties with getters, validation working
- **Evidence**: [TBD]

---

### Phase 3: Infrastructure Layer - Core Implementation

#### Gap #5: OllamaServiceOrchestrator Main Class ⏳
- **File to Create**: `src/Acode.Infrastructure/Providers/Ollama/Lifecycle/OllamaServiceOrchestrator.cs`
- **Purpose**: Main orchestration logic, Spec line 949+, implements IOllamaServiceOrchestrator
- **Responsibilities**:
  - Manage process lifecycle (start/stop/restart)
  - Coordinate health checking
  - Track service state
  - Enforce restart policies
  - Manage model pulling
- **Key Methods** (all from interface):
  - `EnsureHealthyAsync()` - Ensures service ready, handles auto-start in Managed mode
  - `GetStateAsync()` - Returns current service state
  - `StartAsync()` - Starts Ollama process
  - `StopAsync()` - Gracefully stops process
  - `PullModelAsync()` - Pulls model from registry
- **Dependencies to Inject**:
  - `OllamaHttpClient` - For HTTP communication
  - `OllamaHealthChecker` - For health checks
  - `IProcessManager` - For process control (Windows/Unix)
  - `ILogger<OllamaServiceOrchestrator>` - For logging
- **Key Logic**:
  - Detect if process running (check port 11434 or process listing)
  - Handle mode-specific behavior (Managed vs Monitored vs External)
  - Implement restart limiting (max 3 per 60 seconds)
  - Cache state for 5 seconds (FR-044)
  - Support Operating Modes constraints (LocalOnly, Burst, Airgapped)
- **Implementation Pattern**: Use dependency injection, follow async patterns, proper CancellationToken handling
- **Success Criteria**: All public methods implemented (not throwing NotImplementedException), compiles, integrates with dependency injection
- **Evidence**: [TBD]

#### Gap #6: ServiceStateTracker Helper Class ⏳
- **File to Create**: `src/Acode.Infrastructure/Providers/Ollama/Lifecycle/ServiceStateTracker.cs`
- **Purpose**: Internal state machine for lifecycle management, FR-014 to FR-050
- **Responsibility**: Track service state transitions and history
- **Key Features**:
  - Maintain current `OllamaServiceState`
  - Track failed health check count (reset on success)
  - Track restart count and timestamps
  - Detect state transitions (e.g., Running → Crashed)
  - Provide state change events for logging/monitoring
- **Methods**:
  - `UpdateState(OllamaServiceState newState)` - Validate transition, update, log
  - `IncrementFailureCount()` - Increment health check failures
  - `ResetFailureCount()` - Reset on successful health check
  - `CanRestart()` - Check if restart limit allows
  - `RecordRestart()` - Track restart for rate limiting
- **Success Criteria**: State transitions validated, failure counter works, restart limiting enforced
- **Evidence**: [TBD]

#### Gap #7: HealthCheckWorker Background Service ⏳
- **File to Create**: `src/Acode.Infrastructure/Providers/Ollama/Lifecycle/HealthCheckWorker.cs`
- **Purpose**: Periodic health monitoring, FR-010 to FR-020, FR-042
- **Responsibility**: Background task that periodically checks Ollama health
- **Key Features**:
  - Configurable check interval (default 60 seconds)
  - Early exit if service marked as External mode
  - Update service state based on health result
  - Detect crashes (process no longer exists)
  - Handle transient failures (retry logic)
- **Methods**:
  - `StartAsync(CancellationToken)` - Start background health checks
  - `StopAsync(CancellationToken)` - Stop gracefully
  - `HealthCheckAsync(CancellationToken)` - Single health check iteration
- **Integration**: Works with ServiceStateTracker and OllamaHealthChecker
- **Success Criteria**: Background loop runs, detects crashes, respects CancellationToken
- **Evidence**: [TBD]

#### Gap #8: RestartPolicyEnforcer Helper Class ⏳
- **File to Create**: `src/Acode.Infrastructure/Providers/Ollama/Lifecycle/RestartPolicyEnforcer.cs`
- **Purpose**: Prevent restart loops, FR-045 to FR-051
- **Responsibility**: Implement restart rate limiting
- **Key Logic**:
  - Track restart timestamps in last 60 seconds
  - Allow max 3 restarts per 60-second window
  - Exponential backoff for repeated failures
  - Backoff durations: 1s, 2s, 4s, 8s, etc.
- **Methods**:
  - `CanRestart()` - Check if restart allowed
  - `RecordRestart()` - Record restart timestamp
  - `GetNextBackoffDuration()` - Calculate backoff time
  - `Reset()` - Reset state after success
- **Success Criteria**: Prevents >3 restarts per minute, backoff increases
- **Evidence**: [TBD]

#### Gap #9: ModelPullManager Helper Class ⏳
- **File to Create**: `src/Acode.Infrastructure/Providers/Ollama/Lifecycle/ModelPullManager.cs`
- **Purpose**: Manage model pulling/downloading, FR-025 to FR-037
- **Responsibility**: Handle model pulling with retries and error handling
- **Key Features**:
  - Pull model from Ollama registry (/api/pull endpoint)
  - Retry on network errors (max 3 times)
  - Detect specific error conditions:
    - Invalid model name (HTTP 404)
    - Insufficient disk space
    - Model already pulled
  - Non-blocking failures (log warning but don't crash)
  - Progress reporting (for streaming UI)
  - Airgapped mode support (reject pulls with clear error)
- **Methods**:
  ```csharp
  Task<ModelPullResult> PullAsync(string modelName, CancellationToken);
  Task<ModelPullResult> PullAsync(string modelName, Action<ModelPullProgress>? callback, CancellationToken);
  IAsyncEnumerable<ModelPullProgress> PullStreamAsync(string modelName, CancellationToken);
  ```
- **Models to Create**:
  - `ModelPullResult` - Success/failure with message
  - `ModelPullProgress` - Progress event (current bytes, total bytes, status)
- **Success Criteria**: Pulls models, retries on failure, respects airgapped mode
- **Evidence**: [TBD]

---

### Phase 4: Testing Layer

#### Gap #10: OllamaServiceOrchestrator Unit Tests ⏳
- **File to Create**: `tests/Acode.Infrastructure.Tests/Providers/Ollama/Lifecycle/OllamaServiceOrchestratorTests.cs`
- **Purpose**: Spec Testing Requirements section, UT-001 to UT-020
- **Test Count Required**: 20+ unit tests minimum
- **Test Categories** (from spec):
  - Construction & initialization (UT-001)
  - GetStateAsync behavior (UT-002 to UT-005)
  - StartAsync behavior (UT-006 to UT-008)
  - Mode-specific behavior (UT-009, UT-010)
  - PullModelAsync behavior (UT-011 to UT-015)
  - Health check logic (UT-016)
  - Restart policies (UT-017)
  - StopAsync behavior (UT-018, UT-019)
  - Configuration handling (UT-020)
- **Mocking Strategy**:
  - Mock `OllamaHttpClient` for API calls
  - Mock `IProcessManager` for process control
  - Mock `ILogger` for logging
  - Fake/stub `OllamaHealthChecker`
  - Mock `IOperatingModeDetector` for mode checks
- **Arrange-Act-Assert Pattern**: All tests follow AAA
- **Test Data**: Use realistic Ollama configurations and responses
- **Success Criteria**: All 20+ tests pass, no mocking of internals (only boundaries)
- **Evidence**: [TBD]

#### Gap #11: ServiceStateTracker Unit Tests ⏳
- **File to Create**: `tests/Acode.Infrastructure.Tests/Providers/Ollama/Lifecycle/ServiceStateTrackerTests.cs`
- **Purpose**: State machine validation, FR-014 to FR-050
- **Test Count**: 12+ tests
- **Test Cases**:
  - Valid state transitions (Running → Crashed, etc.)
  - Invalid state transitions rejected (if applicable)
  - Failure counter increments and resets
  - Restart counter respects rate limiting
  - State change events fired correctly
- **Success Criteria**: State transitions validated, counters work correctly
- **Evidence**: [TBD]

#### Gap #12: HealthCheckWorker Unit Tests ⏳
- **File to Create**: `tests/Acode.Infrastructure.Tests/Providers/Ollama/Lifecycle/HealthCheckWorkerTests.cs`
- **Purpose**: Background health check validation, FR-010 to FR-020
- **Test Count**: 10+ tests
- **Test Cases**:
  - Health check loop runs at correct interval
  - Detects process crash
  - Respects External mode (skips checks)
  - Handles transient failures
  - Graceful shutdown
- **Mocking**: Mock `OllamaHealthChecker` and clock
- **Timing Tests**: Use fake clock to test intervals without actual delays
- **Success Criteria**: Health loop validates, crash detection works
- **Evidence**: [TBD]

#### Gap #13: RestartPolicyEnforcer Unit Tests ⏳
- **File to Create**: `tests/Acode.Infrastructure.Tests/Providers/Ollama/Lifecycle/RestartPolicyEnforcerTests.cs`
- **Purpose**: Restart rate limiting validation, FR-045 to FR-051
- **Test Count**: 8+ tests
- **Test Cases**:
  - Allows up to 3 restarts per 60 seconds
  - Rejects 4th restart in window
  - Backoff duration increases correctly (1s → 2s → 4s → 8s)
  - Old restart timestamps pruned from window
  - Reset clears state
- **Fake Clock**: Use injectable clock for testing
- **Success Criteria**: Rate limiting enforced, backoff calculated correctly
- **Evidence**: [TBD]

#### Gap #14: ModelPullManager Unit Tests ⏳
- **File to Create**: `tests/Acode.Infrastructure.Tests/Providers/Ollama/Lifecycle/ModelPullManagerTests.cs`
- **Purpose**: Model pulling validation, FR-025 to FR-037
- **Test Count**: 12+ tests
- **Test Cases**:
  - Successful model pull
  - Retry on network error (up to 3 times)
  - Detect invalid model (404 response)
  - Detect insufficient disk space
  - Detect already-pulled model
  - Airgapped mode rejects pulls
  - Progress events fire correctly
  - Non-blocking failure (logs warning)
- **Mocking**: Mock HTTP responses for various scenarios
- **Success Criteria**: Retries work, errors detected, airgapped respected
- **Evidence**: [TBD]

#### Gap #15: Integration Tests ⏳
- **File to Create**: `tests/Acode.Integration.Tests/Providers/Ollama/Lifecycle/OllamaLifecycleIntegrationTests.cs`
- **Purpose**: End-to-end workflow testing, Spec section "Testing Requirements"
- **Test Count**: 10+ integration tests (some marked `[Skip]` if require live Ollama)
- **Test Scenarios**:
  - Full startup workflow (EnsureHealthy → process starts → health passes)
  - Crash detection and recovery
  - Model pulling in context
  - Mode switching (Managed → Monitored behavior differs)
  - Clean shutdown
  - Config reload behavior
  - Multi-provider coexistence
  - State transitions through full lifecycle
- **Real Services**: Use real `OllamaHttpClient` but mock actual Ollama process
- **Skip Markers**: Tests requiring live Ollama marked `[Fact(Skip = "Requires live Ollama")]`
- **Success Criteria**: Integration workflows verified, all real-world scenarios covered
- **Evidence**: [TBD]

---

## IMPLEMENTATION ORDER (TDD: RED → GREEN → REFACTOR)

### Step 1: Domain Layer Enums ⏳
1. Create OllamaServiceState enum (GREEN fast)
2. Create OllamaLifecycleMode enum (GREEN fast)
3. Write tests for enum values (Unit tests)
4. **Commit**: `feat(task-005d): add OllamaServiceState and OllamaLifecycleMode enums`

### Step 2: Domain Layer Interfaces ⏳
1. Create IOllamaServiceOrchestrator interface
2. Create supporting types (ModelPullResult, ModelPullProgress)
3. **Commit**: `feat(task-005d): add IOllamaServiceOrchestrator interface`

### Step 3: Application Configuration ⏳
1. Write failing tests for OllamaLifecycleOptions (RED)
2. Implement OllamaLifecycleOptions class (GREEN)
3. Implement configuration validation (GREEN)
4. **Commit**: `feat(task-005d): add OllamaLifecycleOptions configuration`

### Step 4: Infrastructure Helpers (Order: Small to Large) ⏳
1. **ServiceStateTracker** (smallest, easiest):
   - Write tests for state transitions (RED)
   - Implement state machine (GREEN)
   - **Commit**: `feat(task-005d): add ServiceStateTracker`

2. **RestartPolicyEnforcer** (straightforward logic):
   - Write tests for rate limiting (RED)
   - Implement backoff calculation (GREEN)
   - **Commit**: `feat(task-005d): add RestartPolicyEnforcer`

3. **ModelPullManager** (depends on HTTP client):
   - Write tests for pull scenarios (RED)
   - Implement pull logic with retries (GREEN)
   - **Commit**: `feat(task-005d): add ModelPullManager`

4. **HealthCheckWorker** (requires background service setup):
   - Write tests for health loop (RED)
   - Implement background worker (GREEN)
   - **Commit**: `feat(task-005d): add HealthCheckWorker`

### Step 5: Core OllamaServiceOrchestrator (Largest) ⏳
1. Write failing tests for orchestrator (RED) - start with one method
2. Implement EnsureHealthyAsync (GREEN)
3. Implement GetStateAsync (GREEN)
4. Implement StartAsync (GREEN)
5. Implement StopAsync (GREEN)
6. Implement PullModelAsync variants (GREEN)
7. **Commit after each major method**: `feat(task-005d): implement OllamaServiceOrchestrator.[MethodName]`

### Step 6: Integration Testing ⏳
1. Write integration tests for full workflows (RED)
2. Implement integration test scenarios (GREEN)
3. Verify end-to-end with mock Ollama
4. **Commit**: `test(task-005d): add integration tests`

### Step 7: Documentation & Final Verification ⏳
1. Update XML documentation on all public APIs
2. Create user manual examples in CLI help
3. Verify all 87 FRs satisfied
4. Run full test suite (should be 3900+ → 3920+)
5. **Commit**: `docs(task-005d): add user documentation`

---

## SUCCESS CRITERIA (Task Complete When All Met)

- [ ] All domain layer enums and interfaces created and compiling
- [ ] All application configuration classes created with validation
- [ ] All infrastructure helper classes implemented (ServiceStateTracker, RestartPolicyEnforcer, ModelPullManager, HealthCheckWorker)
- [ ] OllamaServiceOrchestrator fully implements IOllamaServiceOrchestrator interface
- [ ] 20+ unit tests for OllamaServiceOrchestrator (all passing)
- [ ] 12+ unit tests for ServiceStateTracker (all passing)
- [ ] 10+ unit tests for HealthCheckWorker (all passing)
- [ ] 8+ unit tests for RestartPolicyEnforcer (all passing)
- [ ] 12+ unit tests for ModelPullManager (all passing)
- [ ] 10+ integration tests covering full lifecycle (all passing)
- [ ] Build succeeds with 0 warnings, 0 errors
- [ ] All 87 Functional Requirements addressed in code (per spec FR-001 to FR-087)
- [ ] All 37 Non-Functional Requirements met (performance, reliability, etc.)
- [ ] All 72 Acceptance Criteria verified
- [ ] XML documentation complete on all public APIs
- [ ] User Manual section implemented with CLI examples
- [ ] Audit passes (see docs/AUDIT-GUIDELINES.md)
- [ ] PR created with comprehensive summary

---

## CURRENT PROGRESS

| Phase | Component | Status | Tests Written | Implementation | Evidence |
|-------|-----------|--------|----------------|-----------------|----------|
| 1 | OllamaServiceState | ⏳ Pending | [ ] | [ ] | |
| 1 | OllamaLifecycleMode | ⏳ Pending | [ ] | [ ] | |
| 1 | IOllamaServiceOrchestrator | ⏳ Pending | [ ] | [ ] | |
| 2 | OllamaLifecycleOptions | ⏳ Pending | [ ] | [ ] | |
| 3 | ServiceStateTracker | ⏳ Pending | [ ] | [ ] | |
| 3 | RestartPolicyEnforcer | ⏳ Pending | [ ] | [ ] | |
| 3 | ModelPullManager | ⏳ Pending | [ ] | [ ] | |
| 3 | HealthCheckWorker | ⏳ Pending | [ ] | [ ] | |
| 3 | OllamaServiceOrchestrator | ⏳ Pending | [ ] | [ ] | |
| 4 | Unit Tests | ⏳ Pending | [ ] | [ ] | |
| 4 | Integration Tests | ⏳ Pending | [ ] | [ ] | |

---

## TASK DEPENDENCIES

- ✅ Task 005, 005a, 005c: Ollama provider adapter (exists, already built)
- ✅ Task 002: Repo contract file (needed for config loading)
- ✅ Task 004c: Provider registry (will consume this orchestrator)
- 🔄 Task 006d: vLLM lifecycle (parallel, same architecture pattern)
- 🔄 Task 029a-e: Cloud provider orchestrators (future, will follow same pattern)

---

**NEXT ACTION**: Begin Phase 1 - Domain Layer by writing and implementing OllamaServiceState enum with tests.

