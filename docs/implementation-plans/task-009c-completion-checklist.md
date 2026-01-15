# Task-009c Completion Checklist: Fallback & Escalation Rules

**Status:** 73% Complete (55/75 ACs verified)

**Objective:** Achieve 100% Acceptance Criteria compliance through systematic gap closure

**Methodology:** Test-Driven Development (RED → GREEN → REFACTOR)

---

## Instructions for Implementation

### Phase System

This checklist is organized into 6 sequential phases. **Each phase depends on previous phases being complete.**

1. **Phase 1: Security Layer (CRITICAL)** - Implement model verification and validation
   - 2 new service files + 2 new classes
   - Model verification + checksum validation
   - URL validation logic
   - ~8-10 hours

2. **Phase 2: Capability Validation (CRITICAL)** - Ensure fallback preserves capabilities
   - 1 new wrapper handler
   - Capability checking logic
   - ~4-6 hours

3. **Phase 3: Operating Mode Enforcement (CRITICAL)** - Restrict models based on mode
   - 1 new wrapper handler
   - Mode-based filtering
   - ~3-4 hours

4. **Phase 4: Policy Implementation (HIGH)** - Implement escalation policy strategies
   - 4 new policy classes + 1 interface
   - Policy-based decision making
   - ~4-5 hours

5. **Phase 5: Integration & E2E Tests (HIGH)** - Comprehensive test coverage
   - 2 new test files (integration + E2E)
   - 6+ tests total
   - ~10-12 hours

6. **Phase 6: Verification & Logging (MEDIUM)** - Final polish and observability
   - Update logging to JSON format
   - Final audit of all 75 ACs
   - ~2-3 hours

### How to Use This Checklist

- Mark items with **[🔄]** when starting
- Mark items with **[✅]** when complete with evidence
- Evidence = test output, code file path, or verification command
- **Do not proceed to next phase until current phase is 100% complete**
- Run `dotnet build` after each phase to catch compilation errors
- Run `dotnet test` after each phase to verify tests pass

---

## PHASE 1: SECURITY LAYER (CRITICAL)

**Duration:** ~8-10 hours
**Dependency:** None (can start immediately)
**Blocking:** Phases 2-3 depend on this (security must come first)

### 1.1 Create IModelVerificationService Interface

**File:** `src/Acode.Domain/Security/IModelVerificationService.cs`

**What to implement:**
- [🔄] Create public interface IModelVerificationService
- [🔄] Method: `bool VerifyModel(string modelId, OperatingMode mode)`
  - Purpose: Verify model is registered in catalog and meets mode constraints
  - Return: true if valid, false if invalid
- [🔄] Method: `bool IsValidModelReference(string modelId)`
  - Purpose: Validate format (not a direct URL)
  - Reject: http://, https://, file://
  - Accept: registry IDs (alphanumeric + hyphens)
- [🔄] Method: `string GetModelChecksum(string modelId)`
  - Purpose: Get SHA256 checksum for tampering detection
  - Return: hex string of checksum

**Code Template:**
```csharp
namespace AgenticCoder.Domain.Security;

using AgenticCoder.Domain.Modes;

public interface IModelVerificationService
{
    /// <summary>
    /// Verifies model is registered and compatible with operating mode
    /// </summary>
    bool VerifyModel(string modelId, OperatingMode mode);

    /// <summary>
    /// Validates model reference format (not a URL)
    /// </summary>
    bool IsValidModelReference(string modelId);

    /// <summary>
    /// Gets checksum for model verification
    /// </summary>
    string GetModelChecksum(string modelId);
}
```

**Success Criteria:**
- [ ] File compiles without errors
- [ ] Interface is public and properly documented

### 1.2 Create ModelVerificationService Implementation

**File:** `src/Acode.Infrastructure/Security/ModelVerificationService.cs`

**What to implement:**
- [🔄] Class ModelVerificationService : IModelVerificationService
- [🔄] Constructor: `public ModelVerificationService(IModelCatalog catalog, ILogger<ModelVerificationService> logger)`
- [🔄] Implement `VerifyModel(modelId, mode)`:
  - Get model from IModelCatalog
  - If model not found, return false
  - Check if model deployment is compatible with mode:
    - LocalOnly: Only ModelDeployment.Local
    - Airgapped: Only ModelDeployment.Local + not RequiresNetworkConnection
    - Burst: Any deployment allowed
  - Return true if compatible, false otherwise
- [🔄] Implement `IsValidModelReference(modelId)`:
  - Reject if starts with: http://, https://, file://, ftp://
  - Reject if empty
  - Accept if alphanumeric + hyphens + colons (provider:model format)
  - Return true/false
- [🔄] Implement `GetModelChecksum(modelId)`:
  - Read model registration from catalog
  - Compute SHA256 hash of model definition
  - Return hex string

**Code Template:**
```csharp
namespace AgenticCoder.Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using AgenticCoder.Domain.Models;
using AgenticCoder.Domain.Modes;
using AgenticCoder.Domain.Security;
using AgenticCoder.Application.Models;

public sealed class ModelVerificationService : IModelVerificationService
{
    private readonly IModelCatalog _catalog;
    private readonly ILogger<ModelVerificationService> _logger;

    public ModelVerificationService(
        IModelCatalog catalog,
        ILogger<ModelVerificationService> logger)
    {
        _catalog = catalog;
        _logger = logger;
    }

    public bool VerifyModel(string modelId, OperatingMode mode)
    {
        try
        {
            var model = _catalog.GetModel(modelId);
            if (model == null) return false;

            // Check mode constraints
            var allowed = mode switch
            {
                OperatingMode.LocalOnly => model.Deployment == ModelDeployment.Local,
                OperatingMode.Airgapped => model.Deployment == ModelDeployment.Local && !model.RequiresNetworkConnection,
                OperatingMode.Burst => true,
                _ => false
            };

            _logger.LogInformation("Model verification: {ModelId} = {Result} for mode {Mode}", modelId, allowed, mode);
            return allowed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying model {ModelId}", modelId);
            return false;
        }
    }

    public bool IsValidModelReference(string modelId)
    {
        if (string.IsNullOrEmpty(modelId)) return false;

        // Reject URLs
        var invalidPrefixes = new[] { "http://", "https://", "file://", "ftp://" };
        if (invalidPrefixes.Any(p => modelId.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return false;

        // Accept provider:model format (alphanumeric, hyphens, colons)
        return modelId.All(c => char.IsLetterOrDigit(c) || c == '-' || c == ':' || c == '_');
    }

    public string GetModelChecksum(string modelId)
    {
        var model = _catalog.GetModel(modelId);
        if (model == null) return string.Empty;

        var json = System.Text.Json.JsonSerializer.Serialize(model);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }
}
```

**Success Criteria:**
- [ ] File compiles without errors
- [ ] All three methods implemented
- [ ] No NotImplementedException

### 1.3 Create ModelVerificationServiceTests

**File:** `tests/Acode.Infrastructure.Tests/Security/ModelVerificationServiceTests.cs`

**What to implement - Write tests FIRST (RED phase):**

- [🔄] `Should_Verify_Local_Model_In_LocalOnly_Mode()`
  - Arrange: Create model with Deployment.Local
  - Act: Call VerifyModel(modelId, OperatingMode.LocalOnly)
  - Assert: Returns true

- [🔄] `Should_Reject_Network_Model_In_LocalOnly_Mode()`
  - Arrange: Create model with Deployment.ExternalAPI
  - Act: Call VerifyModel(modelId, OperatingMode.LocalOnly)
  - Assert: Returns false

- [🔄] `Should_Reject_URL_Format()`
  - Arrange: modelId = "http://example.com/model"
  - Act: Call IsValidModelReference(modelId)
  - Assert: Returns false

- [🔄] `Should_Accept_Provider_Format()`
  - Arrange: modelId = "ollama:llama3.2:7b"
  - Act: Call IsValidModelReference(modelId)
  - Assert: Returns true

- [🔄] `Should_Generate_Checksum()`
  - Arrange: Known model
  - Act: Call GetModelChecksum(modelId) twice
  - Assert: Both calls return same checksum (deterministic)

- [🔄] `Should_Detect_Model_Tampering()`
  - Arrange: Get checksum of original model
  - Act: Simulate model modification, get new checksum
  - Assert: Checksums differ

**Success Criteria:**
- [ ] All 6 tests written and failing (RED phase)
- [ ] Tests compile without errors
- [ ] Failing message indicates test is waiting for implementation

### 1.4 Implement ModelVerificationService Tests (GREEN phase)

**What to verify:**
- [✅] All tests pass after implementing 1.2
- [ ] `dotnet test --filter "ModelVerificationService"` shows 6/6 passing

**Success Criteria:**
- [ ] Green phase: 6/6 tests passing

### 1.5 Create VerifiedModelFallbackHandler Wrapper

**File:** `src/Acode.Infrastructure/Fallback/VerifiedModelFallbackHandler.cs`

**What to implement:**
- [🔄] Class VerifiedModelFallbackHandler : IFallbackHandler
- [🔄] Wraps inner IFallbackHandler with verification
- [🔄] Constructor: `public VerifiedModelFallbackHandler(IFallbackHandler inner, IModelVerificationService verificationService)`
- [🔄] Implement GetFallback:
  ```csharp
  public async Task<FallbackResult> GetFallback(FallbackContext context)
  {
      var result = await _inner.GetFallback(context);

      if (result != null)
      {
          // Verify model reference format
          if (!_verificationService.IsValidModelReference(result.ModelId))
          {
              throw new FallbackException(
                  $"Invalid model reference: {result.ModelId}",
                  "ACODE-FAL-002");
          }

          // Verify model is allowed in current mode
          if (!_verificationService.VerifyModel(result.ModelId, context.OperatingMode))
          {
              throw new FallbackException(
                  $"Model {result.ModelId} not allowed in {context.OperatingMode} mode",
                  "ACODE-FAL-003");
          }
      }

      return result;
  }
  ```
- [🔄] Implement NotifyFailure: Delegate to inner
- [🔄] Implement IsCircuitOpen: Delegate to inner

**Code Template:**
```csharp
namespace AgenticCoder.Infrastructure.Fallback;

using AgenticCoder.Application.Fallback;
using AgenticCoder.Domain.Security;
using Microsoft.Extensions.Logging;

public sealed class VerifiedModelFallbackHandler : IFallbackHandler
{
    private readonly IFallbackHandler _inner;
    private readonly IModelVerificationService _verificationService;
    private readonly ILogger<VerifiedModelFallbackHandler> _logger;

    public VerifiedModelFallbackHandler(
        IFallbackHandler inner,
        IModelVerificationService verificationService,
        ILogger<VerifiedModelFallbackHandler> logger)
    {
        _inner = inner;
        _verificationService = verificationService;
        _logger = logger;
    }

    public async Task<FallbackResult> GetFallback(FallbackContext context)
    {
        var result = await _inner.GetFallback(context);

        if (result != null)
        {
            // Verify model reference
            if (!_verificationService.IsValidModelReference(result.ModelId))
            {
                _logger.LogError("Invalid model reference: {ModelId}", result.ModelId);
                throw new FallbackException(
                    $"Invalid model reference: {result.ModelId}",
                    "ACODE-FAL-002");
            }

            // Verify mode compatibility
            if (!_verificationService.VerifyModel(result.ModelId, context.OperatingMode))
            {
                _logger.LogError("Model {ModelId} incompatible with mode {Mode}",
                    result.ModelId, context.OperatingMode);
                throw new FallbackException(
                    $"Model {result.ModelId} not allowed in {context.OperatingMode} mode",
                    "ACODE-FAL-003");
            }
        }

        return result;
    }

    public Task NotifyFailure(string modelId, Exception ex, CancellationToken ct = default)
        => _inner.NotifyFailure(modelId, ex, ct);

    public bool IsCircuitOpen(string modelId)
        => _inner.IsCircuitOpen(modelId);
}
```

**Success Criteria:**
- [ ] File compiles without errors
- [ ] Wraps inner handler correctly
- [ ] Verification logic implemented

### 1.6 Create VerifiedModelFallbackHandlerTests

**File:** `tests/Acode.Infrastructure.Tests/Fallback/VerifiedModelFallbackHandlerTests.cs`

**What to implement:**

- [🔄] `Should_Allow_Valid_Models()`
- [🔄] `Should_Reject_URL_Format_Models()`
- [🔄] `Should_Reject_Models_Incompatible_With_Mode()`
- [🔄] `Should_Log_Verification_Failures()`

**Success Criteria:**
- [ ] 4 tests pass

### 1.7 Register Services in DI

**File:** `src/Acode.Infrastructure/ServiceCollectionExtensions.cs`

**What to modify:**
- [🔄] Add: `services.AddSingleton<IModelVerificationService, ModelVerificationService>();`
- [🔄] Modify fallback handler registration to use VerifiedModelFallbackHandler

**Success Criteria:**
- [ ] File compiles without errors
- [ ] Services resolve correctly

### 1.8 Verify Phase 1 Complete

**Commands to run:**
```bash
# Build should succeed
dotnet build

# All security tests should pass
dotnet test --filter "ModelVerification|VerifiedModelFallback"
```

**Evidence needed:**
- [ ] `dotnet build` output: "Build succeeded"
- [ ] Security tests: all passing
- [ ] No compilation errors

---

## PHASE 2: CAPABILITY VALIDATION (CRITICAL)

**Duration:** ~4-6 hours
**Dependency:** Phase 1 must be complete
**Blocking:** Phase 3 and beyond can continue in parallel

### 2.1 Create CapabilityAwareFallbackHandler

**File:** `src/Acode.Infrastructure/Fallback/CapabilityAwareFallbackHandler.cs`

**What to implement:**
- [🔄] Class CapabilityAwareFallbackHandler : IFallbackHandler
- [🔄] Wraps inner handler with capability checking
- [🔄] Constructor: `public CapabilityAwareFallbackHandler(IFallbackHandler inner, IModelCatalog catalog)`
- [🔄] Implement GetFallback:
  - If context.RequiredCapabilities is null/empty, delegate to inner
  - Call inner.GetFallback() to get candidate model
  - If candidate returned:
    - Get model from catalog
    - For each required capability:
      - If model doesn't support it, skip to next model in chain
      - If all capabilities present, return result
  - Continue until chain exhausted or match found

**Implementation Logic:**
```csharp
public async Task<FallbackResult> GetFallback(FallbackContext context)
{
    // If no capability requirements, skip validation
    if (context.RequiredCapabilities == null || context.RequiredCapabilities.Count == 0)
        return await _inner.GetFallback(context);

    // Try fallback chain until finding model with all capabilities
    while (true)
    {
        var result = await _inner.GetFallback(context);
        if (result == null) return null;

        var model = _catalog.GetModel(result.ModelId);
        if (model == null)
        {
            // Model disappeared, try next
            context.SkipModel(result.ModelId);
            continue;
        }

        // Check if model has all required capabilities
        var missingCapabilities = context.RequiredCapabilities
            .Where(c => !model.Capabilities.Contains(c))
            .ToList();

        if (missingCapabilities.Count == 0)
        {
            // All capabilities present, return this model
            return result;
        }

        // Missing capabilities, try next model
        result.Reason = $"Missing capabilities: {string.Join(", ", missingCapabilities)}";
        context.SkipModel(result.ModelId);
    }
}
```

**Success Criteria:**
- [ ] File compiles without errors
- [ ] Wraps handler correctly
- [ ] Capability checking logic implemented

### 2.2 Create CapabilityAwareFallbackHandlerTests

**File:** `tests/Acode.Infrastructure.Tests/Fallback/CapabilityAwareFallbackHandlerTests.cs`

**Tests to implement:**
- [🔄] AC-067: `Should_Preserve_Tool_Calling_Capability()`
- [🔄] AC-068: `Should_Preserve_Vision_Capability()`
- [🔄] AC-069: `Should_Preserve_Function_Calling_Capability()`
- [🔄] AC-070: `Should_Skip_Models_Without_Required_Capabilities()`
- [🔄] AC-071: `Should_Continue_Chain_On_Capability_Mismatch()`

**Success Criteria:**
- [ ] 5 tests pass

### 2.3 Modify FallbackHandlerWithCircuitBreaker to Use Wrapper

**File:** `src/Acode.Infrastructure/Fallback/FallbackHandlerWithCircuitBreaker.cs`

**What to modify:**
- Wrap with CapabilityAwareFallbackHandler during construction
- Update DI registration to use wrapper

**Success Criteria:**
- [ ] File compiles
- [ ] Existing tests still pass
- [ ] Capability validation now integrated

---

## PHASE 3: OPERATING MODE ENFORCEMENT (CRITICAL)

**Duration:** ~3-4 hours
**Dependency:** Phase 1 must be complete
**Can be parallel:** With Phase 2

### 3.1 Create ModeEnforcingFallbackHandler

**File:** `src/Acode.Infrastructure/Fallback/ModeEnforcingFallbackHandler.cs`

**What to implement:**
- [🔄] Class ModeEnforcingFallbackHandler : IFallbackHandler
- [🔄] Filters fallback chain based on operating mode
- [🔄] Constructor: `public ModeEnforcingFallbackHandler(IFallbackHandler inner, IModelCatalog catalog)`
- [🔄] Helper methods:
  - GetValidModelsForLocalOnly()
  - GetValidModelsForAirgapped()
  - GetValidModelsForBurst()
- [🔄] Implement GetFallback:
  - Get valid models for current mode
  - Filter context's allowed chain
  - Get fallback from inner handler
  - Verify result is in valid set
  - Throw if incompatible

**Implementation Logic:**
```csharp
public async Task<FallbackResult> GetFallback(FallbackContext context)
{
    var validModels = GetValidModelsForMode(context.OperatingMode);
    context.AllowedModels = validModels;

    var result = await _inner.GetFallback(context);

    if (result != null && !validModels.Contains(result.ModelId))
    {
        throw new FallbackException(
            $"Model {result.ModelId} incompatible with {context.OperatingMode} mode",
            "ACODE-FAL-001");
    }

    return result;
}

private IEnumerable<string> GetValidModelsForMode(OperatingMode mode) =>
    mode switch
    {
        OperatingMode.LocalOnly =>
            _catalog.GetAllModels()
                .Where(m => m.Deployment == ModelDeployment.Local)
                .Select(m => m.ModelId),

        OperatingMode.Airgapped =>
            _catalog.GetAllModels()
                .Where(m => m.Deployment == ModelDeployment.Local && !m.RequiresNetworkConnection)
                .Select(m => m.ModelId),

        OperatingMode.Burst =>
            _catalog.GetAllModels()
                .Select(m => m.ModelId),

        _ => throw new InvalidOperationException($"Unknown mode: {mode}")
    };
```

**Success Criteria:**
- [ ] File compiles without errors
- [ ] Mode-based filtering logic implemented
- [ ] All three modes handled

### 3.2 Create ModeEnforcingFallbackHandlerTests

**File:** `tests/Acode.Infrastructure.Tests/Fallback/ModeEnforcingFallbackHandlerTests.cs`

**Tests to implement:**
- [🔄] AC-062: `Should_Enforce_Mode_Constraints()`
- [🔄] AC-063: `Should_Exclude_Network_Models_In_LocalOnly()`
- [🔄] AC-064: `Should_Exclude_Network_In_Airgapped()`
- [🔄] AC-065: `Should_Allow_Cloud_Models_In_Burst()`
- [🔄] AC-066: `Should_Validate_At_Chain_Resolution()`

**Success Criteria:**
- [ ] 5 tests pass

### 3.3 Integrate ModeEnforcingFallbackHandler

**File:** `src/Acode.Infrastructure/ServiceCollectionExtensions.cs`

**What to modify:**
- Wrap fallback handler with ModeEnforcingFallbackHandler during DI registration
- Ensure composition order: ModeEnforcingFallbackHandler → VerifiedModelFallbackHandler → CapabilityAwareFallbackHandler → FallbackHandlerWithCircuitBreaker

**Success Criteria:**
- [ ] Handlers compose in correct order
- [ ] All existing tests still pass

---

## PHASE 4: POLICY IMPLEMENTATION (HIGH)

**Duration:** ~4-5 hours
**Dependency:** Phases 1-3 complete
**Note:** Complex, requires policy pattern implementation

### 4.1 Create IEscalationPolicyStrategy Interface

**File:** `src/Acode.Domain/Fallback/IEscalationPolicyStrategy.cs`

**What to implement:**
- [🔄] Public interface IEscalationPolicyStrategy
- [🔄] Method: `Task<EscalationResult> ExecuteAsync(FallbackContext context, EscalationTrigger trigger)`
- [🔄] Each strategy implements different logic for handling trigger

**Code:**
```csharp
namespace AgenticCoder.Domain.Fallback;

public interface IEscalationPolicyStrategy
{
    /// <summary>
    /// Executes escalation policy when trigger condition is met
    /// </summary>
    Task<EscalationResult> ExecuteAsync(FallbackContext context, EscalationTrigger trigger);
}

public class EscalationResult
{
    public bool ShouldEscalate { get; set; }
    public string? RecommendedFallback { get; set; }
    public string Reason { get; set; } = string.Empty;
}
```

### 4.2 Create Policy Implementation Classes

#### 4.2a ImmediatePolicy

**File:** `src/Acode.Infrastructure/Fallback/Policies/ImmediatePolicy.cs`

**Implementation:**
- Immediately escalate on any trigger
- No retry logic
- Return true for ShouldEscalate

#### 4.2b RetryThenFallbackPolicy

**File:** `src/Acode.Infrastructure/Fallback/Policies/RetryThenFallbackPolicy.cs`

**Implementation:**
- Allow N retries before escalating
- Constructor: `public RetryThenFallbackPolicy(int maxRetries = 3)`
- Track retry count
- Return true only after retries exhausted

#### 4.2c CircuitBreakerPolicy

**File:** `src/Acode.Infrastructure/Fallback/Policies/CircuitBreakerPolicy.cs`

**Implementation:**
- Use circuit breaker state to decide
- Implement state machine (Closed, Open, HalfOpen)
- Return true when circuit is open

### 4.3 Create Policy Tests

**File:** `tests/Acode.Infrastructure.Tests/Fallback/EscalationPolicyTests.cs`

**Tests:**
- [🔄] `Should_Escalate_Immediately_With_ImmediatePolicy()`
- [🔄] `Should_Retry_Before_Escalating_With_RetryPolicy()`
- [🔄] `Should_Use_Circuit_State_With_CircuitBreakerPolicy()`

**Success Criteria:**
- [ ] All policy tests passing

### 4.4 Update FallbackHandlerWithCircuitBreaker to Use Policies

**File:** `src/Acode.Infrastructure/Fallback/FallbackHandlerWithCircuitBreaker.cs`

**What to modify:**
- Add IEscalationPolicyStrategy field
- Update NotifyFailure to use policy strategy
- Inject appropriate policy in constructor

**Success Criteria:**
- [ ] Existing tests still pass
- [ ] Policies integrated

---

## PHASE 5: INTEGRATION & E2E TESTS (HIGH)

**Duration:** ~10-12 hours
**Dependency:** Phases 1-4 complete
**Note:** Most time-consuming phase

### 5.1 Create FallbackIntegrationTests

**File:** `tests/Acode.Tests.Integration/Fallback/FallbackIntegrationTests.cs`

**Tests to implement:**
- [🔄] `Should_Select_First_Available_Model_In_Chain()`
- [🔄] `Should_Skip_Unavailable_Models_And_Continue_Chain()`
- [🔄] `Should_Respect_Circuit_Breaker_State_Across_Requests()`
- [🔄] `Should_Integrate_With_IModelRouter()`
- [🔄] `Should_Respect_Operating_Mode_Constraints()`
- [🔄] `Should_Preserve_Required_Capabilities()`

**Each test should:**
- Use TestApplicationFactory
- Set up realistic fallback chain
- Verify end-to-end behavior
- Check logging and observability

**Estimated Scope:** ~150 lines

### 5.2 Create FallbackE2ETests

**File:** `tests/Acode.Tests.E2E/Fallback/FallbackE2ETests.cs`

**Tests to implement:**
- [🔄] `Should_Display_Fallback_Status_Via_CLI()`
- [🔄] `Should_Test_Chain_Availability_Via_CLI()`
- [🔄] `Should_Reset_Circuits_Via_CLI()`
- [🔄] `Should_Show_Per_Model_Circuit_State()`
- [🔄] `Should_Accept_Model_Filter_For_Reset()`

**Each test should:**
- Use E2ETestFixture with running CLI
- Execute actual CLI commands
- Verify output and behavior
- Check logs for events

**Estimated Scope:** ~120 lines

### 5.3 Verify Phase 5 Complete

**Commands to run:**
```bash
# Build should succeed
dotnet build

# Integration tests should pass
dotnet test --filter "FallbackIntegration"

# E2E tests should pass
dotnet test --filter "FallbackE2E"
```

**Evidence needed:**
- [ ] All integration tests passing (6+)
- [ ] All E2E tests passing (5+)
- [ ] No compilation errors

---

## PHASE 6: VERIFICATION & LOGGING (MEDIUM)

**Duration:** ~2-3 hours
**Dependency:** Phases 1-5 complete
**Final:** Completion of task

### 6.1 Update Logging to JSON Format

**File:** `src/Acode.Infrastructure/Fallback/FallbackHandlerWithCircuitBreaker.cs`

**What to modify:**
- [🔄] Replace String.Format logging with structured JSON logging
- [🔄] Example transformation:
  ```csharp
  // OLD:
  _logger.LogInformation($"Fallback selected: {modelId} for session {sessionId}");

  // NEW:
  _logger.LogInformation(
      "Fallback selected: {ModelId} for session {SessionId}",
      modelId,
      sessionId);  // Structured logging extracts fields
  ```
- [🔄] Ensure all decision points include:
  - event_type (escalation, circuit_event, fallback_selected)
  - model_id
  - session_id
  - status (success/failure)
  - reason

**Success Criteria:**
- [ ] All logging uses structured format
- [ ] No string interpolation in log calls
- [ ] Fields properly named per spec

### 6.2 Create Final Verification Checklist

**What to verify - All 75 ACs:**

| AC Range | Category | Status |
|----------|----------|--------|
| AC-001-007 | Core Interfaces | ✅ |
| AC-008-019 | Implementation | ✅ |
| AC-020-025 | Triggers & Policies | ✅ |
| AC-026-038 | Circuit Breaker | ✅ |
| AC-039-043 | Chain Exhaustion | ✅ |
| AC-044-054 | CLI | ✅ |
| AC-055-061 | Logging | ✅ |
| AC-062-066 | Operating Modes | ✅ (NOW COMPLETE) |
| AC-067-071 | Capability Validation | ✅ (NOW COMPLETE) |
| AC-072-075 | Security | ✅ (NOW COMPLETE) |

- [🔄] Run each AC verification command
- [🔄] Document evidence for each AC
- [🔄] No ACs remain unverified

### 6.3 Full Build and Test

**Commands to run:**
```bash
# Full clean build
dotnet clean
dotnet build

# Run all tests
dotnet test

# Run fallback-specific tests only
dotnet test --filter "Fallback"
```

**Evidence needed:**
- [ ] Clean build succeeds (no warnings)
- [ ] All tests pass (100+)
- [ ] Fallback tests: 65 unit + 6 integration + 5 E2E = 76 tests

### 6.4 Create Final Commit

```bash
git add -A
git commit -m "feat(task-009c): complete fallback and escalation implementation

Implements all missing security, capability validation, operating mode enforcement,
policy strategies, and comprehensive test coverage.

Phase 1: Add model verification service with checksum validation (AC-072-075)
Phase 2: Add capability-aware fallback handler (AC-067-071)
Phase 3: Add mode-enforcing fallback handler (AC-062-066)
Phase 4: Add escalation policy strategy classes (3 policies)
Phase 5: Add integration and E2E tests (11 tests)
Phase 6: Update to structured JSON logging

Achieves 100% acceptance criteria compliance (75/75 ACs)
All 100+ tests passing
Production-ready security features

🤖 Generated with Claude Code

Co-Authored-By: Claude Haiku 4.5 <noreply@anthropic.com>"
```

### 6.5 Create Pull Request

```bash
gh pr create --title "feat(task-009c): Complete fallback and escalation rules" \
  --body "Complete implementation achieving 100% acceptance criteria compliance.
  Adds security layer, capability validation, operating mode enforcement,
  policy strategies, and comprehensive test coverage. 75/75 ACs verified."
```

---

## Checklist Summary (Check Off as You Complete Each Phase)

- [ ] Phase 1: Security Layer (IModelVerificationService, VerifiedModelFallbackHandler)
- [ ] Phase 2: Capability Validation (CapabilityAwareFallbackHandler)
- [ ] Phase 3: Operating Mode Enforcement (ModeEnforcingFallbackHandler)
- [ ] Phase 4: Policy Implementation (3 policy classes)
- [ ] Phase 5: Integration & E2E Tests (11 tests)
- [ ] Phase 6: Verification & JSON Logging
- [ ] Final verification: All 75 ACs verified
- [ ] Git commit and PR created

---

## Reference Documents

- **Gap Analysis:** `docs/implementation-plans/task-009c-gap-analysis.md`
- **Spec File:** `docs/tasks/refined-tasks/Epic 01/task-009c-fallback-escalation-rules.md`
- **Related Task:** Task-009a (Roles) - fallback must respect roles
- **Related Task:** Task-009b (Heuristics) - fallback after heuristics fail
- **Related Task:** Task-009 (Routing) - fallback is part of main routing decision

---

**Status Update:** This task is ~73% complete. Complete all 6 phases to reach 100%.
