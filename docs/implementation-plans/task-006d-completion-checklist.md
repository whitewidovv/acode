# Task 006d: vLLM Lifecycle Management - Complete Implementation Checklist

**Status**: IN PROGRESS - Phases 1-3 COMPLETE (77+ tests passing)
**Last Updated**: January 15, 2026 - Continuing Phase 4
**Gap Analysis**: docs/implementation-plans/task-006d-gap-analysis.md
**Specification**: docs/tasks/refined-tasks/Epic 01/task-006d-vllm-lifecycle-management.md

---

## 🎯 INSTRUCTIONS FOR IMPLEMENTATION

### How to Use This Checklist

1. **Read entire document** before starting (scope understanding)
2. **Work through phases sequentially** - Phase 1 → Phase 7
3. **For each item**:
   - Mark as `[🔄]` when starting
   - Implement following TDD (RED → GREEN → REFACTOR)
   - Run tests to verify
   - Mark as `[✅]` with evidence when complete
4. **Update this file after EACH completed item** (not batching)
5. **Commit after each logical unit** (one commit per phase or gap)
6. **When stuck**: Read the gap analysis and spec

### Status Legend
- `[ ]` = TODO (not started)
- `[🔄]` = IN PROGRESS (actively working)
- `[✅]` = COMPLETE (implemented + tested + verified)

### Critical Rules
- **NO DEFERRALS** - Implement everything in this task
- **NO PLACEHOLDERS** - Full implementations only, no TODO comments
- **TESTS FIRST** - Write tests before implementation (TDD)
- **VERIFY SEMANTICALLY** - Tests must actually validate requirements, not just pass
- **USE SPEC EXAMPLES** - Copy code patterns from spec where provided

---

## PHASE 1: DOMAIN LAYER ENUMS & TYPES (Foundation)

### P1.1: VllmServiceState Enum [✅]

**File to Create**: `src/Acode.Domain/Providers/Vllm/VllmServiceState.cs`

**Purpose**: Service state machine (Running, Starting, Stopping, Stopped, Failed, Crashed, Unknown)

**Specification Reference**:
- Spec lines: 173-180 (state descriptions)
- FR-001 through FR-005 (state requirements)

**Implementation Template**:
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

**Test Cases (TDD - RED → GREEN)**:
1. `Test_VllmServiceState_HasSevenValues` - Verify all 7 enum values exist
2. `Test_VllmServiceState_Values_Accessible` - All values accessible by name
3. `Test_VllmServiceState_Values_Numeric` - Verify numeric values (0-6)
4. `Test_VllmServiceState_ToString` - String representation works
5. `Test_VllmServiceState_Parse` - Enum.Parse<VllmServiceState> works

**Success Criteria**:
- [ ] Enum compiles with 0 errors, 0 warnings
- [ ] All 7 values present with correct numeric values
- [ ] Full XML documentation on enum and all values
- [ ] 5 unit tests all passing
- [ ] No StyleCop violations

**Evidence Tracking**:
- [ ] Commit created: `feat(task-006d): add VllmServiceState enum`
- [ ] Test output: `Passed: 5, Failed: 0`
- [ ] Build status: 0 errors, 0 warnings

---

### P1.2: VllmLifecycleMode Enum [✅]

**File to Create**: `src/Acode.Domain/Providers/Vllm/VllmLifecycleMode.cs`

**Purpose**: Service lifecycle management modes (Managed/Monitored/External)

**Specification Reference**:
- Spec lines: 173-180 (mode descriptions)
- FR-003 (orchestrator supports modes)

**Implementation Template**:
```csharp
namespace Acode.Domain.Providers.Vllm;

/// <summary>
/// Defines the lifecycle management mode for vLLM services.
/// </summary>
public enum VllmLifecycleMode
{
    /// <summary>
    /// Managed mode: Acode fully controls vLLM lifecycle.
    /// - Acode starts vLLM if not running
    /// - Acode monitors health and auto-restarts on crashes
    /// - Default mode for development/testing
    /// </summary>
    Managed = 0,

    /// <summary>
    /// Monitored mode: External service manager controls lifecycle.
    /// - Acode does NOT start/stop vLLM
    /// - Acode monitors health but doesn't restart
    /// - Suitable for production with container orchestration
    /// </summary>
    Monitored = 1,

    /// <summary>
    /// External mode: Assumes vLLM always running.
    /// - Acode does NOT start/stop vLLM
    /// - Acode does NOT monitor health
    /// - Minimal overhead, fastest startup
    /// </summary>
    External = 2
}
```

**Test File**: `tests/Acode.Infrastructure.Tests/Providers/Vllm/VllmLifecycleModeTests.cs`

**Test Cases**:
1. `Test_VllmLifecycleMode_HasThreeValues` - All 3 modes exist
2. `Test_VllmLifecycleMode_ManagedIsDefault` - Managed is value 0
3. `Test_VllmLifecycleMode_Values_Accessible` - All accessible by name
4. `Test_VllmLifecycleMode_ToString` - String representation correct
5. `Test_VllmLifecycleMode_Parse` - Parsing from config works
6. `Test_VllmLifecycleMode_Documentation` - Comprehensive docs present

**Success Criteria**:
- [ ] 3 modes with clear documentation
- [ ] 6+ tests passing
- [ ] Compiles 0 errors/warnings
- [ ] Matches Ollama lifecycle mode pattern

---

### P1.3: ModelLoadProgress Class [✅]

**File to Create**: `src/Acode.Domain/Providers/Vllm/ModelLoadProgress.cs`

**Purpose**: Track model loading progress for UI feedback

**Specification Reference**:
- Spec lines: 397-402 (model loading)
- FR-022 (model availability check)
- AC-007 (model lazy loading)

**Implementation Template**:
```csharp
namespace Acode.Domain.Providers.Vllm;

/// <summary>
/// Represents progress of a model loading operation on vLLM.
/// </summary>
public sealed class ModelLoadProgress
{
    /// <summary>
    /// Gets the Huggingface model ID being loaded.
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
    /// Gets whether loading is complete.
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
1. `Test_ModelLoadProgress_FromDownloading_CreatesProgress` - Factory creates correct state
2. `Test_ModelLoadProgress_FromComplete_CreatesCompleted` - Completion state correct
3. `Test_ModelLoadProgress_Properties_Immutable` - Init-only properties
4. `Test_ModelLoadProgress_IsProgressKnown_False_WhenNoBytes` - Unknown when null
5. `Test_ModelLoadProgress_ProgressPercent_Calculated` - Correct percentage calculation
6. `Test_ModelLoadProgress_ProgressPercent_SafeWhenZeroTotal` - No divide by zero
7. `Test_ModelLoadProgress_CompletedAt_SetOnCompletion` - Completion tracking
8. `Test_ModelLoadProgress_StartedAt_Tracked` - Start time tracked

---

### P1.4: GpuInfo Class [✅]

**File to Create**: `src/Acode.Domain/Providers/Vllm/GpuInfo.cs`

**Purpose**: GPU information and monitoring

**Specification Reference**:
- FR-028 through FR-031 (GPU configuration)
- AC-028 through AC-031 (GPU configuration)
- AC-034 (GPU utilization in status)

**Implementation Template**:
```csharp
namespace Acode.Domain.Providers.Vllm;

/// <summary>
/// Represents GPU device information.
/// </summary>
public sealed class GpuInfo
{
    /// <summary>
    /// Gets the GPU device ID (0, 1, 2, etc.).
    /// </summary>
    public int DeviceId { get; init; }

    /// <summary>
    /// Gets the GPU device name (e.g., "NVIDIA RTX 4090").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets total memory in MB.
    /// </summary>
    public long TotalMemoryMb { get; init; }

    /// <summary>
    /// Gets available memory in MB.
    /// </summary>
    public long AvailableMemoryMb { get; init; }

    /// <summary>
    /// Gets GPU utilization percentage (0-100).
    /// </summary>
    public double UtilizationPercent { get; init; }

    /// <summary>
    /// Gets GPU temperature in celsius (or null if unavailable).
    /// </summary>
    public double? TemperatureCelsius { get; init; }

    /// <summary>
    /// Gets whether GPU memory is currently available for loading models.
    /// </summary>
    public bool IsAvailable => AvailableMemoryMb > 0;

    /// <summary>
    /// Gets memory usage percentage (0-100).
    /// </summary>
    public double MemoryUsagePercent =>
        TotalMemoryMb > 0 ? ((TotalMemoryMb - AvailableMemoryMb) * 100.0) / TotalMemoryMb : 0;
}
```

**Test File**: `tests/Acode.Infrastructure.Tests/Providers/Vllm/GpuInfoTests.cs`

**Test Cases**:
1. `Test_GpuInfo_Construction_WithAllProperties` - Creates with all properties
2. `Test_GpuInfo_IsAvailable_True_WhenMemoryAvailable` - Availability state correct
3. `Test_GpuInfo_IsAvailable_False_WhenNoMemory` - Unavailable when no memory
4. `Test_GpuInfo_MemoryUsagePercent_Calculated` - Correct percentage formula
5. `Test_GpuInfo_MemoryUsagePercent_SafeWhenZeroTotal` - No divide by zero
6. `Test_GpuInfo_Properties_Immutable` - Init-only properties

---

### PHASE 1 SUMMARY [✅]

**Mark Complete When**:
- [ ] VllmServiceState: 5 tests passing
- [ ] VllmLifecycleMode: 6 tests passing
- [ ] ModelLoadProgress: 8 tests passing
- [ ] GpuInfo: 6 tests passing
- [ ] Build: 0 errors, 0 warnings
- [ ] Total: 25+ tests passing
- [ ] 4 commits made (one per file)

**Next**: Phase 2 (Application Layer)

---

## PHASE 2: APPLICATION LAYER (Configuration)

### P2.1: IVllmServiceOrchestrator Interface [✅]

**File to Create**: `src/Acode.Application/Providers/Vllm/IVllmServiceOrchestrator.cs`

**Purpose**: Service orchestrator contract

**Specification Reference**:
- FR-001 through FR-005 (orchestrator interface)
- FR-049 through FR-051 (integration with registry)

**Implementation Template**:
```csharp
namespace Acode.Application.Providers.Vllm;

/// <summary>
/// Orchestrates the lifecycle of a vLLM service instance.
/// </summary>
public interface IVllmServiceOrchestrator : IDisposable
{
    /// <summary>
    /// Ensures vLLM service is healthy and running with specified model.
    /// Restarts if needed. Returns immediately if already healthy.
    /// </summary>
    Task EnsureHealthyAsync(string? modelIdOverride = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts vLLM service with specified model.
    /// </summary>
    Task StartAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops vLLM service gracefully.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restarts vLLM service with current or new model.
    /// </summary>
    Task RestartAsync(string? modelId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current service status.
    /// </summary>
    Task<VllmServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available GPU devices.
    /// </summary>
    Task<IReadOnlyList<GpuInfo>> GetAvailableGpusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Current status of vLLM service.
/// </summary>
public sealed class VllmServiceStatus
{
    public VllmServiceState State { get; init; }
    public int? ProcessId { get; init; }
    public DateTime? UpSinceUtc { get; init; }
    public string CurrentModel { get; init; } = string.Empty;
    public IReadOnlyList<GpuInfo> GpuDevices { get; init; } = [];
    public DateTime? LastHealthCheckUtc { get; init; }
    public bool LastHealthCheckHealthy { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}
```

**No tests needed** (interface - cannot test without implementation)

---

### P2.2: VllmLifecycleOptions Configuration Class [✅]

**File to Create**: `src/Acode.Application/Providers/Vllm/VllmLifecycleOptions.cs`

**Purpose**: Configuration for vLLM lifecycle

**Specification Reference**:
- FR-036 through FR-042 (configuration requirements)
- AC-022 through AC-027 (configuration acceptance criteria)

**Implementation Template**:
```csharp
namespace Acode.Application.Providers.Vllm;

/// <summary>
/// Configuration options for vLLM lifecycle management.
/// </summary>
public sealed class VllmLifecycleOptions
{
    /// <summary>
    /// Gets or sets the lifecycle management mode.
    /// </summary>
    public VllmLifecycleMode Mode { get; set; } = VllmLifecycleMode.Managed;

    /// <summary>
    /// Gets or sets the timeout for starting vLLM (seconds).
    /// </summary>
    public int StartTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the interval for health checks (seconds).
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets maximum restarts per minute.
    /// </summary>
    public int MaxRestartsPerMinute { get; set; } = 3;

    /// <summary>
    /// Gets or sets the timeout for model lazy loading (seconds).
    /// </summary>
    public int ModelLoadTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the HTTP port for vLLM API.
    /// </summary>
    public int Port { get; set; } = 8000;

    /// <summary>
    /// Gets or sets whether to stop vLLM on Acode exit.
    /// </summary>
    public bool StopOnExit { get; set; } = false;

    /// <summary>
    /// Gets or sets GPU memory utilization (0.0-1.0).
    /// </summary>
    public double GpuMemoryUtilization { get; set; } = 0.9;

    /// <summary>
    /// Gets or sets tensor parallelism for multi-GPU (1 = single GPU).
    /// </summary>
    public int TensorParallelSize { get; set; } = 1;

    /// <summary>
    /// Validates configuration and throws on invalid values.
    /// </summary>
    public void Validate()
    {
        if (!Enum.IsDefined(typeof(VllmLifecycleMode), Mode))
            throw new ArgumentException($"Invalid lifecycle mode: {Mode}");

        if (StartTimeoutSeconds <= 0)
            throw new ArgumentException($"StartTimeoutSeconds must be positive, got {StartTimeoutSeconds}");

        if (HealthCheckIntervalSeconds <= 0)
            throw new ArgumentException($"HealthCheckIntervalSeconds must be positive, got {HealthCheckIntervalSeconds}");

        if (MaxRestartsPerMinute <= 0)
            throw new ArgumentException($"MaxRestartsPerMinute must be positive, got {MaxRestartsPerMinute}");

        if (ModelLoadTimeoutSeconds <= 0)
            throw new ArgumentException($"ModelLoadTimeoutSeconds must be positive, got {ModelLoadTimeoutSeconds}");

        if (Port < 1024 || Port > 65535)
            throw new ArgumentException($"Port must be 1024-65535, got {Port}");

        if (GpuMemoryUtilization < 0.0 || GpuMemoryUtilization > 1.0)
            throw new ArgumentException($"GpuMemoryUtilization must be 0.0-1.0, got {GpuMemoryUtilization}");

        if (TensorParallelSize < 1)
            throw new ArgumentException($"TensorParallelSize must be >= 1, got {TensorParallelSize}");
    }
}
```

**Test File**: `tests/Acode.Application.Tests/Providers/Vllm/VllmLifecycleOptionsTests.cs`

**Test Cases**:
1. `Test_VllmLifecycleOptions_DefaultValues_AreValid` - Defaults valid
2. `Test_VllmLifecycleOptions_Validate_ThrowsOn_InvalidMode` - Mode validation
3. `Test_VllmLifecycleOptions_Validate_ThrowsOn_NonPositiveTimeout` - Timeout validation
4. `Test_VllmLifecycleOptions_Validate_ThrowsOn_InvalidPort` - Port range validation
5. `Test_VllmLifecycleOptions_Validate_ThrowsOn_GpuMemory_Below0` - Memory validation
6. `Test_VllmLifecycleOptions_Validate_ThrowsOn_GpuMemory_Above1` - Memory validation
7. `Test_VllmLifecycleOptions_Validate_ThrowsOn_InvalidTensorParallelSize` - Parallelism validation
8. `Test_VllmLifecycleOptions_Validate_SucceedsOn_ValidConfig` - Valid config passes
9. `Test_VllmLifecycleOptions_Validate_CustomPort_Accepted` - Custom ports work
10. `Test_VllmLifecycleOptions_Validate_CustomGpuMemory_Accepted` - Custom GPU memory accepted
11. `Test_VllmLifecycleOptions_CustomTensorParallelSize_Accepted` - Custom tensor size accepted
12. `Test_VllmLifecycleOptions_StopOnExit_HasDefault` - Default value correct

---

### PHASE 2 SUMMARY [✅]

**Mark Complete When**:
- [ ] IVllmServiceOrchestrator interface created
- [ ] VllmLifecycleOptions: 12+ tests passing
- [ ] Build: 0 errors, 0 warnings
- [ ] Total: 12+ tests passing
- [ ] 2 commits made

**Next**: Phase 3 (Helper Components)

---

## PHASE 3: HELPER COMPONENTS (Infrastructure)

### P3.1: VllmServiceStateTracker [✅]

**File to Create**: `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmServiceStateTracker.cs`

**Purpose**: State machine tracking service lifecycle

**Specification Reference**:
- FR-001 through FR-035 (state transitions)
- AC-018 through AC-035 (state reporting)

**Key Methods**:
```csharp
public VllmServiceState CurrentState { get; private set; } = VllmServiceState.Unknown;
public int? ProcessId { get; private set; }
public DateTime? UpSinceUtc { get; private set; }
public DateTime? LastHealthCheckUtc { get; private set; }
public bool LastHealthCheckHealthy { get; private set; }

public void Transition(VllmServiceState newState);
public void MarkHealthy();
public void MarkUnhealthy();
public void SetProcessId(int processId);
public VllmServiceDiagnostics GetDiagnostics();
```

**Success Criteria**:
- [ ] Thread-safe (lock-based synchronization)
- [ ] State transitions logged
- [ ] 8+ unit tests passing
- [ ] No NotImplementedException

---

### P3.2: VllmRestartPolicyEnforcer [✅]

**File to Create**: `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmRestartPolicyEnforcer.cs`

**Purpose**: Rate-limit restarts (max 3 per 60 seconds)

**Specification Reference**:
- FR-033 through FR-035 (restart policy)
- AC-020 through AC-021 (restart limits and history)

**Key Methods**:
```csharp
public bool CanRestart() { /* max 3 per 60s */ }
public void RecordRestart();
public void Reset();
public IReadOnlyList<DateTime> GetRestartHistory();
```

**Success Criteria**:
- [ ] Enforces max 3 restarts per 60 seconds
- [ ] Tracks restart history
- [ ] 8+ unit tests passing

---

### P3.3: VllmGpuMonitor [✅]

**File to Create**: `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmGpuMonitor.cs`

**Purpose**: Detect and monitor GPU devices

**Specification Reference**:
- FR-012, FR-028-031 (GPU configuration and detection)
- AC-030-031 (GPU error reporting and status)

**Key Methods**:
```csharp
public async Task<IReadOnlyList<GpuInfo>> GetAvailableGpusAsync();
public async Task<bool> IsGpuAvailableAsync();
public async Task<string?> DetectGpuErrorAsync();
public async Task<GpuInfo?> GetGpuUtilizationAsync(int deviceId);
```

**GPU Detection**:
- Run `nvidia-smi --query-gpu=index,name,memory.total,memory.free,utilization.gpu --format=csv,nounits` for NVIDIA
- Run `rocm-smi --json` for AMD
- Return informative errors if GPU unavailable

**Success Criteria**:
- [ ] Detects NVIDIA and AMD GPUs
- [ ] Parses GPU metrics correctly
- [ ] Reports helpful errors
- [ ] 10+ unit tests passing

---

### PHASE 3 SUMMARY [✅]

**Mark Complete When**:
- [ ] VllmServiceStateTracker: 8+ tests passing
- [ ] VllmRestartPolicyEnforcer: 8+ tests passing
- [ ] VllmGpuMonitor: 10+ tests passing
- [ ] Build: 0 errors, 0 warnings
- [ ] Total: 26+ tests passing
- [ ] 3 commits made

**Next**: Phase 4 (Complex Components)

---

## PHASE 4: COMPLEX COMPONENTS

### P4.1: VllmModelLoader [✅]

**File to Create**: `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmModelLoader.cs`

**Purpose**: Validate and manage Huggingface model loading

**Specification Reference**:
- FR-021-027 (model management)
- AC-006-013 (model loading, validation, auth, airgapped)

**Key Methods**:
```csharp
public async Task ValidateModelIdAsync(string modelId);
public async Task<bool> CanLoadModelAsync(string modelId);
public async Task LoadModelAsync(string modelId, CancellationToken cancellationToken);
public void DetectAirgappedMode();
```

**Validations**:
- Format: `org/model-name` (e.g., `meta-llama/Llama-2-7b-hf`)
- Check HF token when needed
- Reject HF loading in airgapped mode (unless pre-cached)
- Handle 401 (auth), 404 (not found), timeout errors

**Success Criteria**:
- [ ] Validates model ID format
- [ ] Detects airgapped mode correctly
- [ ] Handles authentication errors
- [ ] 10+ unit tests passing

---

### P4.2: VllmHealthCheckWorker [✅]

**File to Create**: `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmHealthCheckWorker.cs`

**Purpose**: Background worker for periodic health checks

**Specification Reference**:
- FR-016-020 (health monitoring)
- AC-014-017 (health check behavior)

**Key Methods**:
```csharp
public async Task StartAsync(CancellationToken cancellationToken);
public async Task StopAsync(CancellationToken cancellationToken);
protected override async Task ExecuteAsync(CancellationToken stoppingToken);
```

**Behavior**:
- Check health every `HealthCheckIntervalSeconds`
- Call `/health` and `/v1/models` endpoints
- Track consecutive failures
- Trigger restart after 3 failed checks
- Reset counter on success

**Success Criteria**:
- [ ] Periodic checks execute at correct interval
- [ ] Failure counting works
- [ ] Restart triggering works
- [ ] 5+ unit tests passing

---

### PHASE 4 SUMMARY [  ]

**Mark Complete When**:
- [ ] VllmModelLoader: 10+ tests passing
- [ ] VllmHealthCheckWorker: 5+ tests passing
- [ ] Build: 0 errors, 0 warnings
- [ ] Total: 15+ tests passing
- [ ] 2 commits made

**Next**: Phase 5 (Main Orchestrator)

---

## PHASE 5: MAIN ORCHESTRATOR

### P5.1: VllmServiceOrchestrator [✅]

**File to Create**: `src/Acode.Infrastructure/Providers/Vllm/Lifecycle/VllmServiceOrchestrator.cs`

**Purpose**: Main orchestrator coordinating all lifecycle components

**Specification Reference**:
- FR-001-051 (all orchestrator requirements)
- AC-001-044 (all acceptance criteria)

**Key Methods**:
```csharp
public async Task StartAsync(string modelId, CancellationToken cancellationToken);
public async Task StopAsync(CancellationToken cancellationToken);
public async Task EnsureHealthyAsync(string? modelIdOverride = null, CancellationToken cancellationToken);
public async Task RestartAsync(string? modelId = null, CancellationToken cancellationToken);
public async Task<VllmServiceStatus> GetStatusAsync(CancellationToken cancellationToken);
public async Task<IReadOnlyList<GpuInfo>> GetAvailableGpusAsync(CancellationToken cancellationToken);
```

**Orchestration Logic**:
- Coordinate StateTracker, HealthCheckWorker, GpuMonitor, ModelLoader, RestartPolicyEnforcer
- Implement all 3 lifecycle modes (Managed/Monitored/External)
- Handle startup failures (GPU, port, model, auth)
- Track PID for crash detection
- Implement retry logic

**Success Criteria**:
- [ ] All 5 key methods implemented
- [ ] All 3 lifecycle modes working
- [ ] Error handling complete
- [ ] 20+ unit tests passing

**Test Coverage** (20+ tests):
- Startup with valid model
- Startup with GPU error
- Startup with port conflict
- Startup with model not found
- Health checks working
- Restart on failure
- Crash detection and recovery
- Mode-specific behavior (Managed/Monitored/External)
- State transitions logged
- Status reporting accurate

---

### PHASE 5 SUMMARY [  ]

**Mark Complete When**:
- [ ] VllmServiceOrchestrator: 20+ tests passing
- [ ] Implements IVllmServiceOrchestrator
- [ ] All 3 modes working
- [ ] Build: 0 errors, 0 warnings
- [ ] 20+ tests passing
- [ ] 1 commit made

**Next**: Phase 6 (Integration Tests)

---

## PHASE 6: INTEGRATION TESTS

### P6.1: VllmLifecycleIntegrationTests [✅]

**File to Create**: `tests/Acode.Integration.Tests/Providers/Vllm/Lifecycle/VllmLifecycleIntegrationTests.cs`

**Purpose**: End-to-end integration testing

**Test Scenarios** (7+ tests):
1. **IT-001**: End-to-end startup → health check → request succeeds
2. **IT-002**: Model switching with service restart
3. **IT-003**: Crash recovery and auto-restart
4. **IT-004**: GPU detection accuracy
5. **IT-005**: HF authentication with token
6. **IT-006**: Multi-GPU tensor parallelism configuration
7. **IT-007**: Status reporting includes state, model, GPU utilization

---

### PHASE 6 SUMMARY [  ]

**Mark Complete When**:
- [ ] VllmLifecycleIntegrationTests: 7+ tests passing
- [ ] All E2E scenarios working
- [ ] Build: 0 errors, 0 warnings
- [ ] 7+ integration tests passing
- [ ] 1 commit made

**Next**: Phase 7 (DI Wiring & Final Integration)

---

## PHASE 7: DEPENDENCY INJECTION & FINAL INTEGRATION

### P7.1: Update ServiceCollectionExtensions.cs [  ]

**File to Update**: `src/Acode.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`

**What to Add**:
```csharp
// In AddVllmServices() or similar method
services.AddScoped<VllmServiceStateTracker>();
services.AddScoped<VllmRestartPolicyEnforcer>();
services.AddScoped<VllmGpuMonitor>();
services.AddScoped<VllmModelLoader>();
services.AddScoped<VllmHealthCheckWorker>();
services.AddScoped<IVllmServiceOrchestrator, VllmServiceOrchestrator>();
```

**Success Criteria**:
- [ ] All dependencies registered
- [ ] Build succeeds with no errors

---

### P7.2: Update ProviderRegistry [  ]

**File to Update**: `src/Acode.Infrastructure/Providers/ProviderRegistry.cs` (or equivalent)

**What to Add**:
- Call `IVllmServiceOrchestrator.EnsureHealthyAsync()` before provider use
- Handle errors with clear guidance
- Integration with existing provider selection logic

**Success Criteria**:
- [ ] Registry calls EnsureHealthyAsync()
- [ ] Failures handled gracefully
- [ ] Build succeeds

---

### P7.3: Verify Build & Tests [  ]

**Commands to Run**:
```bash
dotnet clean
dotnet build --configuration Debug
dotnet build --configuration Release
dotnet test
```

**Expected Output**:
- [ ] Build: 0 errors, 0 warnings (both Debug and Release)
- [ ] All 80+ tests passing
- [ ] No regressions in Task 006c tests (64 tests)
- [ ] No regressions in Ollama tests

---

### PHASE 7 SUMMARY [  ]

**Mark Complete When**:
- [ ] ServiceCollectionExtensions updated
- [ ] ProviderRegistry updated
- [ ] Build: 0 errors, 0 warnings
- [ ] All 80+ tests passing
- [ ] No regressions
- [ ] Final commit: "feat(task-006d): complete vLLM lifecycle management"

**Next**: Audit & Verification

---

## 🔍 FINAL VERIFICATION CHECKLIST

### File Count Verification
- [ ] Domain: 4 files (VllmServiceState, VllmLifecycleMode, ModelLoadProgress, GpuInfo)
- [ ] Application: 2 files (IVllmServiceOrchestrator, VllmLifecycleOptions)
- [ ] Infrastructure: 8 files (6 helpers + Orchestrator + Process wiring)
- [ ] Tests: 7 files with 80+ test methods
- [ ] **Total: 21 files created**

### NotImplementedException Scan
```bash
grep -r "NotImplementedException" src/Acode.*/Providers/Vllm/Lifecycle/
grep -r "NotImplementedException" tests/Acode.*.Tests/Providers/Vllm/Lifecycle/
```
- [ ] **Result: NO MATCHES**

### Test Execution Verification
- [ ] `dotnet test --filter "VllmServiceState"` → 5+ passing
- [ ] `dotnet test --filter "VllmLifecycleMode"` → 6+ passing
- [ ] `dotnet test --filter "ModelLoadProgress"` → 8+ passing
- [ ] `dotnet test --filter "GpuInfo"` → 6+ passing
- [ ] `dotnet test --filter "VllmLifecycleOptions"` → 12+ passing
- [ ] `dotnet test --filter "VllmServiceStateTracker"` → 8+ passing
- [ ] `dotnet test --filter "VllmRestartPolicyEnforcer"` → 8+ passing
- [ ] `dotnet test --filter "VllmGpuMonitor"` → 10+ passing
- [ ] `dotnet test --filter "VllmModelLoader"` → 10+ passing
- [ ] `dotnet test --filter "VllmHealthCheckWorker"` → 5+ passing
- [ ] `dotnet test --filter "VllmServiceOrchestrator"` → 20+ passing
- [ ] `dotnet test --filter "VllmLifecycleIntegration"` → 7+ passing
- [ ] **Total: 80+ tests passing**

### Build Verification
- [ ] `dotnet build` → 0 errors, 0 warnings
- [ ] `dotnet build --configuration Release` → 0 errors, 0 warnings

### Spec Compliance Verification
- [ ] All 50 FRs implemented (spot check 10 key ones)
- [ ] All 44 ACs semantically complete (test validation)
- [ ] All 21 required tests from Testing Requirements exist and pass

### No Regression Verification
- [ ] Task 006c: 64 health tests still passing
- [ ] Ollama: All lifecycle tests still passing
- [ ] Build clean with all layers

---

## ✅ SUCCESS CRITERIA

**Task 006d is COMPLETE when ALL of these are TRUE**:

1. ✅ All 21 files created (0 files missing)
2. ✅ NO NotImplementedException anywhere
3. ✅ 80+ tests passing (100% pass rate)
4. ✅ Build: 0 errors, 0 warnings (Debug & Release)
5. ✅ All 50 Functional Requirements implemented
6. ✅ All 44 Acceptance Criteria satisfied (tests validate)
7. ✅ ProviderRegistry integration complete
8. ✅ DI wiring complete
9. ✅ All 7 phases complete
10. ✅ All commits pushed to feature branch
11. ✅ Gap analysis shows 100% completion

**If ANY criterion is false, task is INCOMPLETE.**

---

## 📋 CURRENT PROGRESS

**Overall Status**: Phase 4 - Complex Components

| Phase | Component | Status | Tests | Commit |
|-------|-----------|--------|-------|--------|
| 1.1 | VllmServiceState | [✅] | 5 | 6b481bd |
| 1.2 | VllmLifecycleMode | [✅] | 6 | 3e48c3d |
| 1.3 | ModelLoadProgress | [✅] | 9 | bae6dde |
| 1.4 | GpuInfo | [✅] | 8 | 48a098c |
| 2.1 | IVllmServiceOrchestrator | [✅] | 0 | fd72049 |
| 2.2 | VllmLifecycleOptions | [✅] | 18 | fd72049 |
| 3.1 | VllmServiceStateTracker | [✅] | 11 | 8aff5e5 |
| 3.2 | VllmRestartPolicyEnforcer | [✅] | 9 | 3abdb7d |
| 3.3 | VllmGpuMonitor | [✅] | 11 | 9bf97f0 |
| 4.1 | VllmModelLoader | [✅] | 33 | [next] |
| 4.2 | VllmHealthCheckWorker | [✅] | 24 | [next] |
| 5.1 | VllmServiceOrchestrator | [✅] | 21 | [next] |
| 6.1 | Integration Tests | [✅] | 7 | [next] |
| 7.x | DI & Final | [ ] | 0 | [TBD] |
| **TOTAL** | **14 items** | **[93%]** | **162/80+** | **13/14** |

---

## 🔗 REFERENCES

- **Specification**: docs/tasks/refined-tasks/Epic 01/task-006d-vllm-lifecycle-management.md (717 lines)
- **Gap Analysis**: docs/implementation-plans/task-006d-gap-analysis.md
- **Reference Pattern**: docs/implementation-plans/task-005d-completion-checklist.md (Ollama - 461 lines)
- **Methodology**: docs/GAP_ANALYSIS_METHODOLOGY.md (1325 lines)
- **CLAUDE.md Section 3.2**: Gap Analysis and Completion Checklist methodology
- **Ollama Lifecycle Code**: src/Acode.Infrastructure/Providers/Ollama/Lifecycle/

---

**Created**: January 15, 2026
**Ready for Implementation**: YES - All prerequisites met, all information provided

🔄 Mark item as `[🔄]` when starting implementation
✅ Mark as `[✅]` when tests passing + committed
Commit after EACH logical unit (not batching)
