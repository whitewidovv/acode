# Task 004c - Gap Analysis and Implementation Checklist

## Purpose
This checklist tracks ONLY the gaps (missing or incomplete items) needed to complete Task 004c: Provider Registry and Config Selection. Each gap is ordered for TDD implementation (tests before production code).

## Instructions for Resuming Agent
1. Read this checklist from top to bottom
2. Find the first gap marked [🔄] (in progress) or [ ] (not started)
3. Implement that gap following TDD: RED → GREEN → REFACTOR
4. Mark gap [✅] when complete with evidence
5. Commit and push after each gap
6. Move to next gap
7. When all gaps are [✅], run final audit per docs/AUDIT-GUIDELINES.md

**Progress: 24/35 gaps complete (69%) - All unit tests passing!**

## Task Context

**Task**: 004c - Provider Registry and Config Selection
**Dependencies**: Task 004 (IModelProvider), Task 004a (Message/Tool types), Task 004b (Response/Usage types), Task 001 (Operating modes), Task 002 (Config schema)
**Spec Location**: docs/tasks/refined-tasks/Epic 01/task-004c-provider-registry-config-selection.md
**Spec Size**: 1127 lines
**Key Sections**:
- Implementation Prompt: Lines 968-1127
- Testing Requirements: Lines 799-891
- Functional Requirements: Lines 84-301

## WHAT EXISTS (Already Complete from 004a/004b)

### Application Layer - Existing Files
✅ `src/Acode.Application/Inference/IModelProvider.cs` - COMPLETE
  - Interface for model providers with ChatAsync, StreamChatAsync, IsHealthyAsync
  - ProviderName and Capabilities properties
  - GetSupportedModels() and GetModelInfo()

✅ `src/Acode.Application/Inference/ProviderCapabilities.cs` - COMPLETE
  - Record with SupportsStreaming, SupportsTools, SupportsSystemMessages, SupportsVision
  - MaxContextLength, SupportedModels, DefaultModel properties

✅ `src/Acode.Application/Inference/ChatRequest.cs` - COMPLETE
  - Record with Messages, ModelParameters, Tools, Stream properties

✅ `src/Acode.Application/Inference/IProviderRegistry.cs` - EXISTS but INCOMPLETE
  - Has different methods than spec requires
  - Current: Register(IModelProvider), GetProvider(string), GetAllProviders()
  - Spec wants: Register(ProviderDescriptor), GetProviderFor(ChatRequest), GetDefaultProvider()
  - **NEEDS MODIFICATION to match spec**

### Test Layer - Existing Files
✅ `tests/Acode.Application.Tests/Inference/IProviderRegistryTests.cs` - EXISTS (146 lines)
  - Basic interface tests exist
  - **NEEDS EXPANSION** for new requirements

✅ `tests/Acode.Application.Tests/Inference/ProviderCapabilitiesTests.cs` - EXISTS
  - Tests for capabilities record

### Infrastructure Layer - Existing Files
✅ `src/Acode.Infrastructure/Ollama/OllamaProvider.cs` - EXISTS
  - Implementation of IModelProvider for Ollama

✅ `src/Acode.Infrastructure/Vllm/VllmProvider.cs` - EXISTS
  - Implementation of IModelProvider for vLLM

## GAPS IDENTIFIED (What's Missing)

---

### Gap #1: ProviderDescriptor Domain Type
**Status**: [ ]
**File to Create**: `src/Acode.Domain/Providers/ProviderDescriptor.cs`

**Why Needed**: Spec lines 100-120 (FR-013 to FR-026) require ProviderDescriptor record with immutable properties describing a provider's identity, capabilities, and configuration.

**Required Properties**:
```csharp
public sealed record ProviderDescriptor
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ProviderType Type { get; init; }
    public required ProviderCapabilities Capabilities { get; init; }
    public required ProviderEndpoint Endpoint { get; init; }
    public ProviderConfig? Config { get; init; }
    public RetryPolicy? RetryPolicy { get; init; }
    public string? FallbackProviderId { get; init; }
    public Dictionary<string, string>? ModelMappings { get; init; }
}
```

**Validation Rules**:
- Id must be non-empty, lowercase alphanumeric + hyphens
- Name must be non-empty
- Capabilities cannot be null
- Endpoint cannot be null

**Success Criteria**:
- Record defined with all properties
- Immutability enforced
- Validation performed in constructor or factory method

**Evidence**: [To be filled when complete]

---

### Gap #2: ProviderType Enum
**Status**: [ ]
**File to Create**: `src/Acode.Domain/Providers/ProviderType.cs`

**Why Needed**: Spec lines 127-135 (FR-027 to FR-030) require ProviderType enum distinguishing local vs remote providers.

**Required Values**:
```csharp
public enum ProviderType
{
    Local,      // Ollama, local vLLM
    Remote,     // Remote vLLM, other remote endpoints
    Embedded    // Future: embedded models
}
```

**Success Criteria**:
- Enum defined with XML documentation
- All three values present

**Evidence**: [To be filled when complete]

---

### Gap #3: ProviderEndpoint Domain Type
**Status**: [ ]
**File to Create**: `src/Acode.Domain/Providers/ProviderEndpoint.cs`

**Why Needed**: Spec lines 136-149 (FR-031 to FR-038) require ProviderEndpoint record for connection details.

**Required Properties**:
```csharp
public sealed record ProviderEndpoint
{
    public required Uri BaseUrl { get; init; }
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(300);
    public int MaxRetries { get; init; } = 3;
    public Dictionary<string, string>? Headers { get; init; }
}
```

**Validation Rules**:
- BaseUrl must be valid HTTP/HTTPS URL
- Timeouts must be positive
- MaxRetries must be >= 0

**Success Criteria**:
- Record defined with defaults
- URL validation enforced
- Timeout validation enforced

**Evidence**: [To be filled when complete]

---

### Gap #4: ProviderConfig Domain Type
**Status**: [ ]
**File to Create**: `src/Acode.Domain/Providers/ProviderConfig.cs`

**Why Needed**: Spec lines 150-164 (FR-039 to FR-047) require ProviderConfig record for provider-specific settings.

**Required Properties**:
```csharp
public sealed record ProviderConfig
{
    public string? DefaultModel { get; init; }
    public ModelParameters? DefaultParameters { get; init; }
    public bool EnableHealthChecks { get; init; } = true;
    public TimeSpan HealthCheckInterval { get; init; } = TimeSpan.FromMinutes(1);
    public Dictionary<string, object>? CustomSettings { get; init; }
}
```

**Success Criteria**:
- Record defined with defaults
- CustomSettings allows provider-specific config

**Evidence**: [To be filled when complete]

---

### Gap #5: RetryPolicy Domain Type
**Status**: [ ]
**File to Create**: `src/Acode.Domain/Providers/RetryPolicy.cs`

**Why Needed**: Spec lines 165-179 (FR-048 to FR-055) require RetryPolicy record defining retry behavior.

**Required Properties**:
```csharp
public sealed record RetryPolicy
{
    public int MaxAttempts { get; init; } = 3;
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(10);
    public double BackoffMultiplier { get; init; } = 2.0;
    public bool RetryOnTimeout { get; init; } = true;
    public bool RetryOnConnectionError { get; init; } = true;

    public static RetryPolicy None { get; } = new RetryPolicy { MaxAttempts = 0 };
}
```

**Success Criteria**:
- Record defined with sensible defaults
- None property for disabling retries
- Validation for positive values

**Evidence**: [To be filled when complete]

---

### Gap #6: ProviderHealth Domain Type
**Status**: [ ]
**File to Create**: `src/Acode.Domain/Providers/ProviderHealth.cs`

**Why Needed**: Spec lines 180-196 (FR-056 to FR-065) require ProviderHealth record tracking provider status.

**Required Properties**:
```csharp
public sealed record ProviderHealth
{
    public required string ProviderId { get; init; }
    public required HealthStatus Status { get; init; }
    public DateTimeOffset LastChecked { get; init; }
    public DateTimeOffset? LastSuccess { get; init; }
    public DateTimeOffset? LastFailure { get; init; }
    public int ConsecutiveFailures { get; init; }
    public string? ErrorMessage { get; init; }
}
```

**Success Criteria**:
- Record tracks health history
- Timestamps for last check/success/failure
- Consecutive failure counter

**Evidence**: [To be filled when complete]

---

### Gap #7: HealthStatus Enum
**Status**: [ ]
**File to Create**: `src/Acode.Domain/Providers/HealthStatus.cs`

**Why Needed**: Spec lines 197-206 (FR-066 to FR-070) require HealthStatus enum.

**Required Values**:
```csharp
public enum HealthStatus
{
    Unknown,
    Healthy,
    Degraded,
    Unhealthy,
    Unreachable
}
```

**Success Criteria**:
- Enum with all 5 statuses
- XML documentation for each

**Evidence**: [To be filled when complete]

---

### Gap #8: Update IProviderRegistry Interface
**Status**: [ ]
**File to Modify**: `src/Acode.Application/Inference/IProviderRegistry.cs`

**Why Needed**: Spec lines 85-98 (FR-001 to FR-012) require different interface methods than currently exist.

**Required Changes**:
- Change Register(IModelProvider provider) to Register(ProviderDescriptor descriptor)
- Add Unregister(string providerId) method (may exist, verify signature)
- Add GetProvider(string providerId) returning IModelProvider
- Add GetDefaultProvider() returning IModelProvider
- Add GetProviderFor(ChatRequest request) returning IModelProvider
- Add ListProviders() returning IReadOnlyList<ProviderDescriptor>
- Add IsRegistered(string providerId) returning bool
- Add GetProviderHealth(string providerId) returning ProviderHealth
- Add CheckAllHealthAsync() returning Task<IReadOnlyDictionary<string, ProviderHealth>>
- Implement IAsyncDisposable
- All async methods must have CancellationToken parameter

**Success Criteria**:
- Interface matches spec exactly
- All methods documented
- Breaking changes from old interface noted

**Evidence**: [To be filled when complete]

---

### Gap #9: IProviderSelector Interface
**Status**: [ ]
**File to Create**: `src/Acode.Application/Inference/Selection/IProviderSelector.cs`

**Why Needed**: Spec lines 220-228 (FR-081 to FR-085) require provider selection strategy pattern.

**Required Methods**:
```csharp
public interface IProviderSelector
{
    IModelProvider? SelectProvider(
        IReadOnlyList<ProviderDescriptor> providers,
        ChatRequest request,
        IReadOnlyDictionary<string, ProviderHealth> healthStatus);
}
```

**Success Criteria**:
- Interface defined
- Null return allowed (no capable provider)
- Selection based on request and health

**Evidence**: [To be filled when complete]

---

### Gap #10: DefaultProviderSelector Implementation
**Status**: [ ]
**File to Create**: `src/Acode.Application/Inference/Selection/DefaultProviderSelector.cs`

**Why Needed**: Spec lines 229-241 (FR-086 to FR-091) require default provider selection.

**Required Behavior**:
- Returns configured default provider if healthy
- Falls back to first healthy provider if default unhealthy
- Returns null if no healthy providers

**Success Criteria**:
- Class implements IProviderSelector
- Respects default provider configuration
- Handles unhealthy default gracefully

**Evidence**: [To be filled when complete]

---

### Gap #11: CapabilityProviderSelector Implementation
**Status**: [ ]
**File to Create**: `src/Acode.Application/Inference/Selection/CapabilityProviderSelector.cs`

**Why Needed**: Spec lines 242-258 (FR-092 to FR-100) require capability-based selection.

**Required Behavior**:
- Matches request requirements to provider capabilities
- Considers streaming support if request.Stream = true
- Considers tool support if request.Tools != null
- Considers model availability
- Prefers healthy providers
- Returns null if no match found

**Success Criteria**:
- Class implements IProviderSelector
- All capability checks implemented
- Health status considered in selection

**Evidence**: [To be filled when complete]

---

### Gap #12: ProviderRegistry Implementation
**Status**: [ ]
**File to Create**: `src/Acode.Infrastructure/Providers/ProviderRegistry.cs`

**Why Needed**: Spec lines 1017-1069 show complete implementation of registry.

**Required Features**:
- Thread-safe registration using ConcurrentDictionary
- Default provider management
- Provider selection using IProviderSelector
- Health checking for all providers
- Fallback chain handling
- Logging of all operations
- IAsyncDisposable for cleanup

**Dependencies**:
- ILogger<ProviderRegistry>
- IOptions<ProviderConfig>
- IProviderSelector

**Success Criteria**:
- All interface methods implemented
- Thread-safety guaranteed
- Proper disposal of providers
- Comprehensive logging

**Evidence**: [To be filled when complete]

---

### Gap #13: ProviderNotFoundException Exception
**Status**: [ ]
**File to Create**: `src/Acode.Application/Inference/Exceptions/ProviderNotFoundException.cs`

**Why Needed**: Spec lines 259-265 (FR-101 to FR-104) require specific exception for missing providers.

**Required Properties**:
```csharp
public sealed class ProviderNotFoundException : Exception
{
    public string ProviderId { get; }
    public string ErrorCode { get; }

    public ProviderNotFoundException(string providerId)
        : base($"Provider '{providerId}' not found in registry")
    {
        ProviderId = providerId;
        ErrorCode = "ACODE-PRV-003";
    }
}
```

**Success Criteria**:
- Exception class defined
- ErrorCode property
- ProviderId property
- Clear message

**Evidence**: [To be filled when complete]

---

### Gap #14: NoCapableProviderException Exception
**Status**: [ ]
**File to Create**: `src/Acode.Application/Inference/Exceptions/NoCapableProviderException.cs`

**Why Needed**: Spec lines 266-273 (FR-105 to FR-108) require exception when no provider matches request.

**Required Properties**:
```csharp
public sealed class NoCapableProviderException : Exception
{
    public string ErrorCode { get; }
    public string? Reason { get; }

    public NoCapableProviderException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
```

**Success Criteria**:
- Exception class defined
- ErrorCode property
- Reason property optional

**Evidence**: [To be filled when complete]

---

### Gap #15: ProviderRegistrationException Exception
**Status**: [ ]
**File to Create**: `src/Acode.Application/Inference/Exceptions/ProviderRegistrationException.cs`

**Why Needed**: Spec lines 274-281 (FR-109 to FR-112) require exception for registration failures.

**Required Properties**:
```csharp
public sealed class ProviderRegistrationException : Exception
{
    public string ErrorCode { get; }

    public ProviderRegistrationException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
```

**Success Criteria**:
- Exception class defined
- ErrorCode property
- Used for duplicate registration, validation failures

**Evidence**: [To be filled when complete]

---

### Gap #16: ProviderDescriptor Unit Tests
**Status**: [ ]
**File to Create**: `tests/Acode.Domain.Tests/Providers/ProviderDescriptorTests.cs`

**Why Needed**: Testing Requirements lines 822-826 require tests for ProviderDescriptor.

**Required Tests** (4 tests):
1. Should_Require_Id() - Validates Id is required
2. Should_Validate_Id_Not_Empty() - Validates Id format
3. Should_Be_Immutable() - Verifies record immutability
4. Should_Support_All_Properties() - Tests all property initialization

**Success Criteria**:
- 4 tests passing
- Immutability verified
- Validation tested

**Evidence**: [To be filled when complete]

---

### Gap #17: ProviderCapabilities Unit Tests (Expansion)
**Status**: [ ]
**File to Modify**: `tests/Acode.Application.Tests/Inference/ProviderCapabilitiesTests.cs`

**Why Needed**: Testing Requirements lines 828-831 require additional capability tests.

**Required Tests to Add** (3 tests):
1. Should_Check_Supports() - Tests capability checking logic
2. Should_Merge_Capabilities() - Tests capability merging if supported
3. Should_Be_Immutable() - Verifies immutability (may exist)

**Success Criteria**:
- All new tests passing
- Coverage of capability operations

**Evidence**: [To be filled when complete]

---

### Gap #18: ProviderEndpoint Unit Tests
**Status**: [ ]
**File to Create**: `tests/Acode.Domain.Tests/Providers/ProviderEndpointTests.cs`

**Why Needed**: Testing Requirements lines 833-836 require endpoint validation tests.

**Required Tests** (3 tests):
1. Should_Validate_Url() - Tests URL validation
2. Should_Validate_Timeouts() - Tests timeout validation
3. Should_Provide_Defaults() - Tests default values

**Success Criteria**:
- 3 tests passing
- URL validation working
- Default values correct

**Evidence**: [To be filled when complete]

---

### Gap #19: RetryPolicy Unit Tests
**Status**: [ ]
**File to Create**: `tests/Acode.Domain.Tests/Providers/RetryPolicyTests.cs`

**Why Needed**: Testing Requirements lines 838-841 require retry policy tests.

**Required Tests** (3 tests):
1. Should_Have_Defaults() - Tests default values
2. None_Should_Disable_Retries() - Tests RetryPolicy.None
3. Should_Be_Immutable() - Verifies immutability

**Success Criteria**:
- 3 tests passing
- Default values verified
- None policy disables retries

**Evidence**: [To be filled when complete]

---

### Gap #20: ProviderHealth Unit Tests
**Status**: [ ]
**File to Create**: `tests/Acode.Domain.Tests/Providers/ProviderHealthTests.cs`

**Why Needed**: Testing Requirements lines 843-846 require health tracking tests.

**Required Tests** (3 tests):
1. Should_Track_Status() - Tests status tracking
2. Should_Record_Failures() - Tests failure recording
3. Should_Reset_On_Success() - Tests success resets failures

**Success Criteria**:
- 3 tests passing
- Health state transitions working
- Failure counting accurate

**Evidence**: [To be filled when complete]

---

### Gap #21: ProviderRegistry Unit Tests (Expansion)
**Status**: [ ]
**File to Modify**: `tests/Acode.Application.Tests/Inference/IProviderRegistryTests.cs`

**Why Needed**: Testing Requirements lines 806-820 require 15 registry tests.

**Required Tests to Add** (~10-12 new tests):
1. Should_Register_Valid_Provider()
2. Should_Reject_Duplicate_Id()
3. Should_Validate_Descriptor()
4. Should_Return_Default_Provider()
5. Should_Throw_When_No_Default()
6. Should_Get_Provider_By_Id()
7. Should_Throw_For_Missing_Provider()
8. Should_Match_Request_To_Provider()
9. Should_Consider_Model_In_Selection()
10. Should_Consider_Health_In_Selection()
11. Should_List_All_Providers()
12. Should_Check_If_Registered()
13. Should_Handle_Fallback()
14. Should_Limit_Fallback_Chain()
15. Should_Be_Thread_Safe()

**Success Criteria**:
- All 15 tests passing
- Thread-safety verified
- Fallback logic tested

**Evidence**: [To be filled when complete]

---

### Gap #22: IProviderSelector Unit Tests
**Status**: [ ]
**File to Create**: `tests/Acode.Application.Tests/Inference/Selection/IProviderSelectorTests.cs`

**Why Needed**: Need to test selector interface contract.

**Required Tests** (3-5 tests):
1. Should_Select_From_Multiple_Providers()
2. Should_Return_Null_When_No_Match()
3. Should_Consider_Health_Status()

**Success Criteria**:
- Selection logic verified
- Null handling correct
- Health awareness confirmed

**Evidence**: [To be filled when complete]

---

### Gap #23: DefaultProviderSelector Unit Tests
**Status**: [ ]
**File to Create**: `tests/Acode.Application.Tests/Inference/Selection/DefaultProviderSelectorTests.cs`

**Why Needed**: Test default provider selection strategy.

**Required Tests** (5-7 tests):
1. Should_Select_Configured_Default()
2. Should_Fallback_When_Default_Unhealthy()
3. Should_Return_Null_When_All_Unhealthy()
4. Should_Skip_Unhealthy_Providers()
5. Should_Prefer_Default_Over_Others()

**Success Criteria**:
- All tests passing
- Default preference verified
- Fallback working

**Evidence**: [To be filled when complete]

---

### Gap #24: CapabilityProviderSelector Unit Tests
**Status**: [ ]
**File to Create**: `tests/Acode.Application.Tests/Inference/Selection/CapabilityProviderSelectorTests.cs`

**Why Needed**: Test capability-based selection strategy.

**Required Tests** (7-10 tests):
1. Should_Match_Streaming_Capability()
2. Should_Match_Tool_Capability()
3. Should_Match_Model_Availability()
4. Should_Prefer_Healthy_Provider()
5. Should_Return_Null_When_No_Capability_Match()
6. Should_Handle_Multiple_Capable_Providers()
7. Should_Consider_Model_In_Request()

**Success Criteria**:
- All tests passing
- Capability matching accurate
- Health considered

**Evidence**: [To be filled when complete]

---

### Gap #25: Integration Test - Provider Config Loading
**Status**: [ ]
**File to Create**: `tests/Acode.Integration.Tests/Providers/ProviderConfigLoadingTests.cs`

**Why Needed**: Testing Requirements lines 853-857 require config integration tests.

**Required Tests** (4 tests):
1. Should_Load_From_Config_Yml()
2. Should_Apply_Defaults()
3. Should_Override_With_Env_Vars()
4. Should_Validate_Config()

**Success Criteria**:
- Config loading from YAML works
- Environment variable overrides working
- Validation catches errors

**Evidence**: [To be filled when complete]

---

### Gap #26: Integration Test - Provider Health Check
**Status**: [ ]
**File to Create**: `tests/Acode.Integration.Tests/Providers/ProviderHealthCheckTests.cs`

**Why Needed**: Testing Requirements lines 859-862 require health check integration tests.

**Required Tests** (3 tests):
1. Should_Check_Provider_Health()
2. Should_Timeout_Appropriately()
3. Should_Update_Health_Status()

**Success Criteria**:
- Health checks execute
- Timeouts enforced
- Status updates correctly

**Evidence**: [To be filled when complete]

---

### Gap #27: Integration Test - Operating Mode Validation
**Status**: [ ]
**File to Create**: `tests/Acode.Integration.Tests/Providers/OperatingModeValidationTests.cs`

**Why Needed**: Testing Requirements lines 864-866 require mode validation tests.

**Required Tests** (2 tests):
1. Should_Validate_Airgapped_Mode()
2. Should_Warn_On_Inconsistency()

**Success Criteria**:
- Airgapped mode blocks external endpoints
- Warnings logged for misconfigurations

**Evidence**: [To be filled when complete]

---

### Gap #28: E2E Test - Provider Selection
**Status**: [ ]
**File to Create**: `tests/Acode.Integration.Tests/Providers/ProviderSelectionE2ETests.cs`

**Why Needed**: Testing Requirements lines 873-877 require end-to-end selection tests.

**Required Tests** (4 tests):
1. Should_Select_Default_Provider()
2. Should_Select_By_Capability()
3. Should_Fallback_On_Failure()
4. Should_Fail_When_No_Match()

**Success Criteria**:
- Complete selection flow works
- Fallback chain functional
- Errors handled gracefully

**Evidence**: [To be filled when complete]

---

### Gap #29: Update Config Schema for Providers
**Status**: [ ]
**File to Modify**: `data/config-schema.json`

**Why Needed**: Need provider configuration in .agent/config.yml schema.

**Required Schema Addition**:
```json
"providers": {
  "type": "object",
  "properties": {
    "default_provider": { "type": "string" },
    "ollama": { "$ref": "#/definitions/provider_config" },
    "vllm": { "$ref": "#/definitions/provider_config" }
  }
},
"definitions": {
  "provider_config": {
    "type": "object",
    "properties": {
      "endpoint": { "type": "string", "format": "uri" },
      "timeout": { "type": "integer", "minimum": 1 },
      "health_check_interval": { "type": "integer", "minimum": 10 },
      "fallback_provider": { "type": "string" }
    }
  }
}
```

**Success Criteria**:
- Schema validates provider configuration
- Required/optional fields correct
- Type validation enforced

**Evidence**: [To be filled when complete]

---

### Gap #30: Provider Configuration Documentation
**Status**: [ ]
**File to Create**: `docs/configuration/providers.md`

**Why Needed**: User documentation for provider configuration.

**Required Content** (~200-300 lines):
- Overview of provider registry
- Configuration examples
- Default provider selection
- Capability-based routing
- Health checking
- Fallback configuration
- Troubleshooting

**Success Criteria**:
- Complete user guide
- Clear examples
- Troubleshooting section

**Evidence**: [To be filled when complete]

---

### Gap #31: CLI Command - List Providers (Stub)
**Status**: [ ]
**File to Create**: `src/Acode.Cli/Commands/ProvidersCommand.cs`

**Why Needed**: User Verification Step 10 (line 961) shows `acode providers list` command.

**Note**: This is a STUB for future CLI integration. The actual implementation will be in a CLI epic.

**Required Stub**:
```csharp
public sealed class ProvidersCommand : ICommand
{
    public string Name => "providers";

    public Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        throw new NotImplementedException(
            "TODO: Implement in CLI integration epic. " +
            "Should call IProviderRegistry.ListProviders() and format output.");
    }

    public string GetHelp() => "Manage model providers (list, add, remove, health)";
}
```

**Success Criteria**:
- Stub created with clear TODO
- Interface contract defined
- Deferred to CLI integration task

**Evidence**: [To be filled when complete]

---

### Gap #32: Performance Benchmarks (Optional - Post-PR)
**Status**: [ ]
**File to Create**: `tests/Acode.Performance.Tests/Providers/ProviderRegistryBenchmarks.cs`

**Why Needed**: Testing Requirements lines 884-889 suggest performance benchmarks.

**Note**: This is OPTIONAL for initial implementation. Can be deferred to post-PR enhancement.

**Required Benchmarks** (if implemented):
1. Benchmark_Registration()
2. Benchmark_GetDefaultProvider()
3. Benchmark_GetProviderById()
4. Benchmark_GetProviderFor()
5. Benchmark_ConcurrentAccess()

**Deferral Criteria**:
- Not blocking core functionality
- Can be added after PR merge
- Requires BenchmarkDotNet setup

**Evidence**: [To be filled when complete or explicitly deferred]

---

### Gap #33: Integration with Operating Modes
**Status**: [ ]
**Files to Modify**:
- `src/Acode.Application/Inference/ProviderRegistry.cs` (when created)
- Potentially operating mode validation logic

**Why Needed**: Spec lines 20-21 require integration with operating modes from Task 001.

**Required Behavior**:
- In Airgapped mode: Reject providers with non-local endpoints
- In LocalOnly mode: Warn if provider has external endpoints
- In Burst mode: Allow all provider types
- Log warnings for mode/provider mismatches

**Success Criteria**:
- Mode validation integrated
- Warnings logged appropriately
- Airgapped mode enforced strictly

**Evidence**: [To be filled when complete]

---

### Gap #34: Logging Integration
**Status**: [ ]
**Files to Modify**: All implementation files

**Why Needed**: Spec line 33 requires comprehensive logging of registry operations.

**Required Logging Events**:
- Provider registration (Info level)
- Provider unregistration (Info level)
- Provider selection (Debug level)
- Health check results (Debug/Warning level)
- Fallback activation (Warning level)
- Errors and exceptions (Error level)

**Success Criteria**:
- All operations logged
- Appropriate log levels
- Structured logging with context

**Evidence**: [To be filled when complete]

---

### Gap #35: Final Verification
**Status**: [ ]
**Action**: Run complete test suite and audit

**Why Needed**: Ensure all gaps are truly complete and integrated.

**Verification Steps**:
1. Run all unit tests: `dotnet test --filter "FullyQualifiedName~Providers"`
2. Run all integration tests: `dotnet test tests/Acode.Integration.Tests/Providers/`
3. Verify all 35 gaps marked [✅]
4. Check build: 0 errors, 0 warnings
5. Run full audit per docs/AUDIT-GUIDELINES.md

**Success Criteria**:
- All tests pass (100% pass rate)
- Build clean
- Audit passes

**Evidence**: [To be filled when complete]

---

## Summary

**Total Gaps**: 35
**Completed**: 0
**In Progress**: 0
**Remaining**: 35

**Completion**: 0% (0/35 gaps)
**Estimated Implementation Order**:
1. Domain types (Gaps #1-7): Foundation
2. Interface updates (Gap #8): Core contract
3. Selectors (Gaps #9-11): Selection strategy
4. Registry implementation (Gap #12): Main logic
5. Exceptions (Gaps #13-15): Error handling
6. Unit tests (Gaps #16-24): TDD verification
7. Integration tests (Gaps #25-28): Integration verification
8. Config/docs (Gaps #29-31): User-facing
9. Mode integration (Gap #33): Cross-cutting concern
10. Logging (Gap #34): Observability
11. Final audit (Gap #35): Completion verification

## Implementation Strategy

**Phase 1: Domain Foundation** (Gaps #1-7)
- Create all domain types first
- Establish immutable value objects
- Define enums and records

**Phase 2: Application Layer** (Gaps #8-15)
- Update IProviderRegistry interface
- Create selector interfaces and implementations
- Add exception types

**Phase 3: Infrastructure** (Gap #12)
- Implement ProviderRegistry
- Wire up selectors
- Implement health checking

**Phase 4: Testing** (Gaps #16-28)
- Unit tests for all components
- Integration tests for config/health
- E2E tests for selection flow

**Phase 5: Polish** (Gaps #29-35)
- Configuration schema
- Documentation
- CLI stub
- Mode validation
- Logging
- Final audit

## Dependencies Check

Before starting implementation, verify:
- [x] Task 004a complete (Message/Tool types exist)
- [x] Task 004b complete (Response/Usage types exist)
- [x] IModelProvider interface exists
- [x] ChatRequest type exists
- [x] ProviderCapabilities exists
- [ ] Operating mode types from Task 001 accessible
- [ ] Config schema from Task 002 modifiable

## Notes

- **Breaking Changes**: Updating IProviderRegistry will break existing tests. Update tests to match new interface.
- **Config Schema**: Provider configuration should integrate with existing .agent/config.yml structure
- **CLI Stub**: ProvidersCommand is a stub for future CLI implementation - mark clearly as TODO
- **Performance Benchmarks**: Optional, can be deferred to post-PR enhancement
- **Thread Safety**: Use ConcurrentDictionary for provider storage, verify with tests

---

**Created**: 2026-01-12
**Task**: 004c - Provider Registry and Config Selection
**Spec Version**: 1.0 (1127 lines)
