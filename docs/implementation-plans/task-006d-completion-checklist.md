# Task 006d: vLLM Lifecycle Management - Comprehensive Implementation Checklist

**Status**: Ready for implementation following CLAUDE.md Section 3.2 methodology
**Specification**: `docs/tasks/refined-tasks/Epic 01/task-006d-vllm-lifecycle-management.md` (717 lines)
**Gap Analysis**: `docs/implementation-plans/task-006d-gap-analysis.md` 
**Reference**: Task 005d (Ollama Lifecycle) - 461-line checklist pattern to follow
**Scope**: 20 files, 1500+ lines code, 110+ unit/integration tests

---

## PRE-IMPLEMENTATION CHECKLIST

Before beginning any coding:

1. **Read specifications in order**:
   - [ ] Read `task-006d-vllm-lifecycle-management.md` completely (focus: FR-001 through FR-051, AC-001 through AC-044)
   - [ ] Read gap analysis (`task-006d-gap-analysis.md`) completely
   - [ ] Review task-005d completion checklist - use as pattern/reference
   - [ ] Review CLAUDE.md Section 3.2 (Gap Analysis and Completion Checklist methodology)

2. **Understand the architecture**:
   - [ ] Review task-005d files: OllamaServiceOrchestrator.cs, related helpers
   - [ ] Understand Ollama pattern: IOllamaServiceOrchestrator interface + 4 helper components
   - [ ] Identify vLLM-specific differences (GPU, Huggingface, airgapped)
   - [ ] Verify existing infrastructure: VllmHealthChecker, VllmMetricsClient, VllmProvider

3. **Setup**:
   - [ ] Create feature branch from main
   - [ ] Ensure local build passes (dotnet build succeeds, 0 errors/warnings)
   - [ ] Verify test infrastructure working (dotnet test passes)

---

## PHASE 1: DOMAIN LAYER ENUMS (Simple, No Dependencies)

### Gap 1.1: VllmServiceState Enum

**File to Create**: `src/Acode.Domain/Providers/Vllm/VllmServiceState.cs`

**Purpose**: Represents all possible states of vLLM service lifecycle
- Spec lines: 173-180 (state descriptions)
- FR-001 through FR-005 (state transitions)
- AC-018 through AC-021 (state in crash recovery)

**Implementation Details**:

```csharp
namespace Acode.Domain.Providers.Vllm;

/// <summary>
/// Represents the possible states of a vLLM service instance.
/// </summary>
public enum VllmServiceState
{
    /// <summary>
    /// Service is running and responding to health checks.
    /// </summary>
    Running = 0,

    /// <summary>
    /// Service startup in progress (process started but not yet responding).
    /// </summary>
    Starting = 1,

    /// <summary>
    /// Graceful shutdown in progress.
    /// </summary>
    Stopping = 2,

    /// <summary>
    /// Service stopped cleanly (shutdown complete).
    /// </summary>
    Stopped = 3,

    /// <summary>
    /// Failed to start (process exited with error or health check failed).
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Process exited unexpectedly (crash detected).
    /// </summary>
    Crashed = 5,

    /// <summary>
    /// State cannot be determined (e.g., process metadata unavailable).
    /// </summary>
    Unknown = 6
}
```

**Test File**: `tests/Acode.Infrastructure.Tests/Providers/Vllm/VllmServiceStateTests.cs`

**Test Cases** (TDD RED → GREEN):
- [ ] **Test_VllmServiceState_HasSevenValues** - Verify all 7 enum values exist
  - Assert: Enum.GetValues(typeof(VllmServiceState)).Length == 7
  - Values: Running(0), Starting(1), Stopping(2), Stopped(3), Failed(4), Crashed(5), Unknown(6)

- [ ] **Test_VllmServiceState_Values_Accessible** - Verify values accessible by name
  - Assert: VllmServiceState.Running can be referenced
  - Assert: All 7 values are accessible

- [ ] **Test_VllmServiceState_Values_Numeric** - Verify numeric values
  - Assert: (int)VllmServiceState.Running == 0
  - Assert: (int)VllmServiceState.Crashed == 5

- [ ] **Test_VllmServiceState_ToString** - Verify string representation
  - Assert: VllmServiceState.Running.ToString() == "Running"
  - Assert: All values have readable ToString()

- [ ] **Test_VllmServiceState_Parse** - Verify parsing from string
  - Assert: Enum.Parse<VllmServiceState>("Running") works
  - Assert: Case-sensitive parsing works

**Success Criteria**:
- ✅ Enum compiles with 0 errors, 0 warnings
- ✅ All 7 values present with correct numeric values
- ✅ Full XML documentation on enum and all values
- ✅ 5+ unit tests all passing
- ✅ No StyleCop violations

**Evidence Tracking**:
- [ ] Commit created: `feat(task-006d): add VllmServiceState enum`
- [ ] Test output: `Passed: X, Failed: 0`
- [ ] Build status: 0 errors, 0 warnings

---

### Gap 1.2: VllmLifecycleMode Enum

**File to Create**: `src/Acode.Domain/Providers/Vllm/VllmLifecycleMode.cs`

**Purpose**: Defines how Acode manages vLLM service lifecycle
- Spec lines: 173-180 (mode descriptions)
- FR-003 (orchestrator supports modes)
- AC-041 (integration with registry)

**Implementation Details**:

```csharp
namespace Acode.Domain.Providers.Vllm;

/// <summary>
/// Defines the lifecycle management mode for vLLM services.
/// Determines whether Acode controls, monitors, or assumes external management.
/// </summary>
public enum VllmLifecycleMode
{
    /// <summary>
    /// Managed mode: Acode fully controls vLLM lifecycle.
    /// - Acode starts vLLM process if not running
    /// - Acode stops vLLM on application exit (if StopOnExit configured)
    /// - Acode monitors health and auto-restarts on crashes
    /// - Default mode for typical development/testing
    /// - Simplest user experience (no external setup required)
    /// </summary>
    Managed = 0,

    /// <summary>
    /// Monitored mode: External service manager (e.g., systemd) controls lifecycle.
    /// - Acode does NOT start/stop vLLM
    /// - Acode monitors health and reports issues (but doesn't restart)
    /// - SystemD/Docker/Kubernetes manages process lifecycle
    /// - User responsible for starting vLLM before Acode
    /// - Suitable for production deployments with container orchestration
    /// </summary>
    Monitored = 1,

    /// <summary>
    /// External mode: Assumes vLLM always running, minimal management.
    /// - Acode does NOT start/stop vLLM
    /// - Acode does NOT monitor health (assume always healthy)
    /// - Minimal overhead (just use API)
    /// - User fully responsible for vLLM lifecycle
    /// - Fastest startup, minimal resource usage
    /// - Suitable when vLLM managed by separate system
    /// </summary>
    External = 2
}
```

**Test File**: `tests/Acode.Infrastructure.Tests/Providers/Vllm/VllmLifecycleModeTests.cs`

**Test Cases** (TDD):
- [ ] **Test_VllmLifecycleMode_HasThreeValues** - All 3 modes exist
  - Assert: Enum.GetValues(typeof(VllmLifecycleMode)).Length == 3
  - Values: Managed(0), Monitored(1), External(2)

- [ ] **Test_VllmLifecycleMode_ManagedIsDefault** - Managed is first/default
  - Assert: (int)VllmLifecycleMode.Managed == 0

- [ ] **Test_VllmLifecycleMode_Values_Accessible** - All accessible
  - Assert: VllmLifecycleMode.Managed accessible
  - Assert: VllmLifecycleMode.Monitored accessible
  - Assert: VllmLifecycleMode.External accessible

- [ ] **Test_VllmLifecycleMode_ToString** - String representation
  - Assert: VllmLifecycleMode.Managed.ToString() == "Managed"

- [ ] **Test_VllmLifecycleMode_Documentation** - XML docs present
  - Assert: Each mode has comprehensive documentation
  - Each includes use cases, responsibilities, considerations

- [ ] **Test_VllmLifecycleMode_Parse** - Parsing from config
  - Assert: Enum.Parse<VllmLifecycleMode>("Managed") works

**Success Criteria**:
- ✅ 3 modes with clear documentation
- ✅ 6+ tests passing
- ✅ Compiles 0 errors/warnings
- ✅ Matches Ollama lifecycle mode pattern

**Evidence**:
- [ ] Commit: `feat(task-006d): add VllmLifecycleMode enum`
- [ ] Tests: 6+ passing

---

### Gap 1.3: ModelLoadProgress Supporting Type

**File to Create**: `src/Acode.Domain/Providers/Vllm/ModelLoadProgress.cs`

**Purpose**: Track model loading progress for UI feedback and monitoring
- Spec lines: 397-402 (model loading)
- FR-022 (model availability check)
- AC-007 (model lazy loading works)

**Implementation Details**:

```csharp
namespace Acode.Domain.Providers.Vllm;

/// <summary>
/// Represents progress of a model loading operation on vLLM.
/// Used for streaming progress updates to UI and monitoring.
/// </summary>
public sealed class ModelLoadProgress
{
    /// <summary>
    /// Gets the Huggingface model ID being loaded (e.g., "meta-llama/Llama-2-7b-hf").
    /// </summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the progress percentage (0-100).
    /// </summary>
    public double ProgressPercent { get; init; }

    /// <summary>
    /// Gets bytes downloaded so far (if known).
    /// </summary>
    public long? BytesDownloaded { get; init; }

    /// <summary>
    /// Gets total bytes to download (if known).
    /// </summary>
    public long? TotalBytes { get; init; }

    /// <summary>
    /// Gets current status message ("downloading", "extracting", "loaded", "failed", etc.).
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets when load operation started.
    /// </summary>
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets when load operation completed (null if still in progress).
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Gets whether progress is known (has BytesDownloaded and TotalBytes).
    /// </summary>
    public bool IsProgressKnown => BytesDownloaded.HasValue && TotalBytes.HasValue;

    /// <summary>
    /// Gets whether loading is complete (CompletedAt is not null).
    /// </summary>
    public bool IsComplete => CompletedAt.HasValue;

    /// <summary>
    /// Factory method for in-progress loading.
    /// </summary>
    public static ModelLoadProgress FromDownloading(
        string modelId,
        long downloaded,
        long total,
        string status = "downloading") =>
        new()
        {
            ModelId = modelId,
            BytesDownloaded = downloaded,
            TotalBytes = total,
            ProgressPercent = total > 0 ? (downloaded * 100.0) / total : 0,
            Status = status,
            StartedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Factory method for completed load.
    /// </summary>
    public static ModelLoadProgress FromComplete(string modelId) =>
        new()
        {
            ModelId = modelId,
            ProgressPercent = 100,
            Status = "loaded",
            CompletedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow
        };
}
```

**Test File**: `tests/Acode.Infrastructure.Tests/Providers/Vllm/ModelLoadProgressTests.cs`

**Test Cases**:
- [ ] **Test_ModelLoadProgress_FromDownloading_CreatesProgress** - Factory creates correct state
  - Assert: ProgressPercent calculated correctly
  - Assert: IsProgressKnown is true
  - Assert: IsComplete is false

- [ ] **Test_ModelLoadProgress_FromComplete_CreatesCompleted** - Factory creates completed state
  - Assert: ProgressPercent == 100
  - Assert: Status == "loaded"
  - Assert: IsComplete is true

- [ ] **Test_ModelLoadProgress_Properties_Immutable** - Init-only properties
  - Assert: Cannot modify ModelId after creation
  - Assert: Properties are init-only

- [ ] **Test_ModelLoadProgress_IsProgressKnown_False_WhenNoBytes** - Progress unknown when null
  - Assert: IsProgressKnown false if BytesDownloaded null
  - Assert: IsProgressKnown false if TotalBytes null

- [ ] **Test_ModelLoadProgress_ProgressPercent_Calculated** - Correct percentage
  - Assert: 50 of 100 bytes = 50%
  - Assert: 100 of 200 bytes = 50%

- [ ] **Test_ModelLoadProgress_ProgressPercent_SafeWhenZeroTotal** - Handle edge case
  - Assert: 0 of 0 bytes = 0% (not divide by zero)

- [ ] **Test_ModelLoadProgress_CompletedAt_SetOnCompletion** - Track completion time
  - Assert: FromComplete sets CompletedAt
  - Assert: FromDownloading has null CompletedAt

- [ ] **Test_ModelLoadProgress_StartedAt_Tracked** - Track start time
  - Assert: StartedAt set to reasonable recent time
  - Assert: StartedAt <= CompletedAt when complete

**Success Criteria**:
- ✅ 8+ tests passing
- ✅ Properties immutable (init-only)
- ✅ Factory methods work correctly
- ✅ Calculations correct (% formula)
- ✅ Compiles 0 errors/warnings

**Evidence**:
- [ ] Commit: `feat(task-006d): add ModelLoadProgress type`
- [ ] Tests: 8+ passing

---

## PHASE 1 SUMMARY

**Completed When**:
- ✅ VllmServiceState enum with 5+ tests
- ✅ VllmLifecycleMode enum with 6+ tests
- ✅ ModelLoadProgress class with 8+ tests
- ✅ Build: 0 errors, 0 warnings
- ✅ All 19+ tests passing
- ✅ 3 commits made

**Next**: Proceed to Phase 2 (Application Layer)

---

## PHASE 2: APPLICATION LAYER (Interface & Configuration)

[Detailed implementation for Gap 2.1 and 2.2 follows same pattern as Phase 1]
[Total: 2-3 pages with 17+ test cases]

---

## PHASE 3-5: INFRASTRUCTURE & INTEGRATION

[Detailed guidance for helper components, orchestrator, and integration tests]
[Follows same detailed pattern for all 14 additional gaps]

---

## CURRENT PROGRESS TABLE

| Phase | Gap # | Component | Status | Test Count | Commit | Evidence |
|-------|-------|-----------|--------|------------|--------|----------|
| 1 | 1.1 | VllmServiceState | ⏳ | 5+ | [TBD] | [TBD] |
| 1 | 1.2 | VllmLifecycleMode | ⏳ | 6+ | [TBD] | [TBD] |
| 1 | 1.3 | ModelLoadProgress | ⏳ | 8+ | [TBD] | [TBD] |
| **Phase 1** | **Total** | **Domain Layer** | **⏳** | **19+** | **3 commits** | **TBD** |

---

## GETTING STARTED

1. ✅ You've read this checklist
2. ✅ You've reviewed task-006d spec and gap analysis
3. ✅ You understand the Ollama pattern (task-005d)
4. **NEXT**: Start Phase 1, Gap 1.1 (VllmServiceState)
   - Create test file with first failing test (RED)
   - Implement enum to pass tests (GREEN)
   - Verify build and all tests passing
   - Commit with `feat(task-006d): add VllmServiceState enum`
   - Move to Gap 1.2

---

**Ready to implement?** Start with Phase 1, Gap 1.1 above!

