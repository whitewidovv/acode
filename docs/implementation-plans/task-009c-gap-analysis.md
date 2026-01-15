# Task-009c Gap Analysis: Fallback & Escalation Rules

**Status:** 73% Semantic Completeness (55 of 75 Acceptance Criteria met)

**Date:** 2026-01-15
**Analyzed By:** Claude Code
**Methodology:** Comprehensive semantic evaluation per CLAUDE.md Section 3.2

---

## Executive Summary

Task-009c implementation has strong foundational work: 65 unit tests passing, core fallback logic fully functional, circuit breaker state machine complete, and CLI interface working. However, **critical security, capability validation, and operating mode enforcement features are missing**. The core fallback and circuit breaker system is production-ready, but security-critical features must be implemented before the task can be considered complete.

**Key Metric:** 55 of 75 Acceptance Criteria verified complete. 20 ACs require work (primarily security, capability validation, operating modes, and test coverage).

---

## What Exists: Complete Components

### ✅ Domain Layer (100% Complete - 8/8 ACs)

**Files:** `src/Acode.Domain/Fallback/CircuitState.cs`, `src/Acode.Domain/Fallback/EscalationTrigger.cs`, `src/Acode.Domain/Fallback/EscalationPolicy.cs`

**Implemented:**
- AC-001: CircuitState enum (Closed, Open, HalfOpen) ✅
- AC-002: EscalationTrigger enum (Unavailable, Timeout, RepeatedErrors) ✅
- AC-003: EscalationPolicy enum (Immediate, RetryThenFallback, CircuitBreaker) ✅
- AC-004-008: Supporting value objects ✅

**Evidence:** 3 enums fully defined with proper documentation

### ✅ Application Layer - Interfaces (100% Complete - 6/6 ACs)

**Files:** `src/Acode.Application/Fallback/IFallbackHandler.cs`, `src/Acode.Application/Fallback/IFallbackConfiguration.cs`

**Implemented:**
- AC-009: IFallbackHandler interface with GetFallback, NotifyFailure, IsCircuitOpen ✅
- AC-010: IFallbackConfiguration interface with GetGlobalChain, GetRoleChain ✅
- AC-011-014: Supporting context and result classes ✅

**Evidence:** Interfaces fully specified with complete method signatures

### ✅ Application Layer - Models (100% Complete - 8/8 ACs)

**Files:** `src/Acode.Application/Fallback/FallbackContext.cs`, `src/Acode.Application/Fallback/FallbackResult.cs`, `src/Acode.Application/Fallback/CircuitStateInfo.cs`

**Implemented:**
- AC-015: FallbackContext with TaskDescription, OperatingMode, SessionId ✅
- AC-016: FallbackResult with ModelId, Reason, TriedModels, FailureReasons ✅
- AC-017: CircuitStateInfo with state tracking ✅
- AC-018-022: Complete immutable properties ✅

**Evidence:** All classes defined with init-only properties, no setters

### ✅ Configuration System (100% Complete - 6/6 ACs)

**File:** `src/Acode.Infrastructure/Fallback/FallbackConfiguration.cs`

**Implemented:**
- AC-023: Global fallback chain configuration ✅
- AC-024: Role-specific chain configuration ✅
- AC-025: Default values (timeout 60s, threshold 5, policy RetryThenFallback) ✅
- AC-026: Configuration validation (ranges, constraints) ✅
- AC-027-028: Chain ordering and precedence ✅

**Evidence:** Constructor shows default values, Getters return IReadOnlyList<string>

### ✅ Circuit Breaker Implementation (100% Complete - 8/8 ACs)

**File:** `src/Acode.Infrastructure/Fallback/CircuitBreaker.cs`

**Implemented:**
- AC-029: CircuitBreaker tracks failures with FailureCount ✅
- AC-030: Opens after threshold (RecordFailure at line 116) ✅
- AC-031: HalfOpen state for recovery attempts ✅
- AC-032: Cooling period configurable (default 60s) ✅
- AC-033: Success closes circuit (RecordSuccess at line 126) ✅
- AC-034: Thread-safe with lock statements ✅
- AC-035-036: Proper state transitions ✅

**Evidence:** State machine fully implemented with proper guards and transitions

### ✅ Main Handler Implementation (100% Complete - 13/13 ACs)

**File:** `src/Acode.Infrastructure/Fallback/FallbackHandlerWithCircuitBreaker.cs`

**Implemented:**
- AC-037: GetFallback reads configuration ✅
- AC-038: Supports per-role fallback chains ✅
- AC-039: Supports global fallback chain ✅
- AC-040: Role precedence over global (lines 256-260) ✅
- AC-041: Availability checks via registry ✅
- AC-042: Skips open circuits (lines 72-78) ✅
- AC-043: Chain exhaustion handling (lines 109-123) ✅
- AC-044: Returns failure result with reasons ✅
- AC-045: TriedModels included in result ✅
- AC-046: FailureReasons dict populated ✅
- AC-047: Graceful degradation with messages ✅
- AC-048-049: Comprehensive logging ✅

**Evidence:** Method implementations fully functional with proper error handling

### ✅ CLI Interface (100% Complete - 11/11 ACs)

**File:** `src/Acode.CLI/Commands/Fallback/FallbackCommand.cs`

**Subcommands Implemented:**
- AC-050: `status` command shows chains ✅
- AC-051: Shows circuit state (lines 174-227) ✅
- AC-052: Shows per-model state (lines 184-226) ✅
- AC-053: Shows last failure time (lines 203-209) ✅
- AC-054: `reset` command with --model filter (line 259) ✅
- AC-055: `reset` with --all flag (line 260) ✅
- AC-056: `test` command for chain availability ✅
- AC-057: Test shows availability (lines 345-365) ✅
- AC-058: All commands have --help (GetHelp method) ✅
- AC-059-060: Output formatted clearly ✅

**Evidence:** Three subcommands fully implemented with proper CLI infrastructure

### ✅ Logging & Observability (100% Complete - 5/5 ACs)

**Evidence in code:**
- AC-061: All escalations logged (lines 92-100) ✅
- AC-062: Circuit events logged (lines 140-158) ✅
- AC-063: Session IDs included (line 99: context.SessionId) ✅
- AC-064: Tried models included (line 114: TriedModels) ✅
- AC-065: Failure reasons included (line 115: failureReasons) ✅

**Evidence:** Comprehensive logging at decision points

### ✅ Unit Tests (100% Complete - 65/65 tests)

**Test Files Present:**
- FallbackResultTests.cs: 3 tests ✅
- CircuitStateInfoTests.cs: 4 tests ✅
- CircuitBreakerTests.cs: 11 tests ✅
- FallbackConfigurationTests.cs: 6 tests ✅
- FallbackHandlerWithCircuitBreakerTests.cs: 11 tests ✅
- FallbackCommandTests.cs: 19 tests ✅
- FallbackHandlerTests.cs (Models): 11 tests ✅

**Evidence:** All unit tests passing, comprehensive coverage of normal and edge cases

---

## Critical Gaps: Missing Implementation

### Gap 1: Security Layer - CRITICAL (AC-072 through AC-075 BLOCKED)

**Specification Reference:** Implementation Prompt lines 3090-3140

**Issue:** Security-critical features completely missing. No model verification, no tamper detection, no URL validation.

#### 1a. Model Verification Service (AC-072, AC-073)

**Spec Location:** Lines 1815-1826 (interface spec)

**What's Missing:** `IModelVerificationService` interface and `VerifiedModelFallbackHandler` wrapper

**What to Implement:**

```csharp
// src/Acode.Domain/Security/IModelVerificationService.cs
public interface IModelVerificationService
{
    /// <summary>
    /// Verifies model is registered in catalog and meets constraints
    /// </summary>
    bool VerifyModel(string modelId, OperatingMode mode);

    /// <summary>
    /// Validates model URL format and origin (no direct URLs)
    /// </summary>
    bool IsValidModelReference(string modelId);

    /// <summary>
    /// Gets checksum for model to detect tampering
    /// </summary>
    string GetModelChecksum(string modelId);
}
```

**Integration Points:**
- Call in FallbackHandlerWithCircuitBreaker.GetFallback() before selecting model
- Reject network URLs (not registry IDs)
- Verify against OperatingMode constraints

**Files to Create:**
- `src/Acode.Domain/Security/IModelVerificationService.cs`
- `src/Acode.Infrastructure/Security/ModelVerificationService.cs`
- `tests/Acode.Infrastructure.Tests/Security/ModelVerificationServiceTests.cs`

**Test Requirements:**
- Test rejects URLs (http://, https://, file://)
- Test verifies catalog registration
- Test validates mode constraints
- Test extracts and compares checksums

#### 1b. Capability-Aware Fallback Wrapper (AC-067 through AC-071)

**What's Missing:** Capability validation when selecting fallback models

**What to Implement:**

```csharp
// Validate fallback model has required capabilities
// from original routing context

public sealed class CapabilityAwareFallbackHandler : IFallbackHandler
{
    private readonly IFallbackHandler _inner;
    private readonly IModelCatalog _catalog;

    public async Task<FallbackResult> GetFallback(FallbackContext context)
    {
        var result = await _inner.GetFallback(context);

        if (result != null && context.RequiredCapabilities != null)
        {
            // Verify fallback model has required capabilities
            var model = _catalog.GetModel(result.ModelId);

            foreach (var cap in context.RequiredCapabilities)
            {
                if (!model.Capabilities.Contains(cap))
                {
                    // Skip this model, try next in chain
                    context.SkipModel(result.ModelId);
                    return await GetFallback(context); // Retry
                }
            }
        }

        return result;
    }
}
```

**Missing Files:**
- `src/Acode.Infrastructure/Fallback/CapabilityAwareFallbackHandler.cs`
- `tests/Acode.Infrastructure.Tests/Fallback/CapabilityAwareFallbackHandlerTests.cs`

**Test Requirements:**
- AC-067: Preserves tool-calling capability
- AC-068: Preserves vision capability
- AC-069: Preserves function-calling capability
- AC-070: Skips models without required capabilities
- AC-071: Continues to next in chain on capability mismatch

---

### Gap 2: Operating Mode Enforcement - CRITICAL (AC-062 through AC-066 PARTIAL)

**Specification Reference:** Implementation Prompt lines 3050-3090

**Issue:** FallbackContext.OperatingMode exists but no validation logic enforces mode constraints.

**What's Missing:** Validation that prevents LocalOnly from accessing network models, prevents Airgapped from any network access, etc.

**What to Implement:**

```csharp
// src/Acode.Infrastructure/Fallback/ModeEnforcingFallbackHandler.cs
public sealed class ModeEnforcingFallbackHandler : IFallbackHandler
{
    private readonly IFallbackHandler _inner;
    private readonly IModelCatalog _catalog;

    public async Task<FallbackResult> GetFallback(FallbackContext context)
    {
        // Filter fallback chain based on operating mode
        var validModels = context.OperatingMode switch
        {
            OperatingMode.LocalOnly =>
                // Only local models (not ExternalAPI deployment)
                GetValidModelsForLocalOnly(context.SessionId),

            OperatingMode.Airgapped =>
                // Only local models, offline only
                GetValidModelsForAirgapped(context.SessionId),

            OperatingMode.Burst =>
                // Local + cloud models allowed
                GetValidModelsForBurst(context.SessionId),

            _ => throw new InvalidOperationException("Unknown mode")
        };

        // Filter context's fallback chain to valid models only
        context.AllowedModels = validModels;

        var result = await _inner.GetFallback(context);

        // Verify selected model is valid for mode
        if (result != null && !validModels.Contains(result.ModelId))
        {
            throw new FallbackException(
                $"Model {result.ModelId} incompatible with {context.OperatingMode} mode",
                "ACODE-FAL-001");
        }

        return result;
    }

    private IEnumerable<string> GetValidModelsForLocalOnly(string sessionId)
    {
        // Return only local models from catalog
        return _catalog.GetAllModels()
            .Where(m => m.Deployment == ModelDeployment.Local)
            .Select(m => m.ModelId);
    }

    private IEnumerable<string> GetValidModelsForAirgapped(string sessionId)
    {
        // Return only local models without network connectivity
        return _catalog.GetAllModels()
            .Where(m => m.Deployment == ModelDeployment.Local && !m.RequiresNetworkConnection)
            .Select(m => m.ModelId);
    }

    private IEnumerable<string> GetValidModelsForBurst(string sessionId)
    {
        // All models allowed in Burst mode
        return _catalog.GetAllModels()
            .Select(m => m.ModelId);
    }
}
```

**Missing Files:**
- `src/Acode.Infrastructure/Fallback/ModeEnforcingFallbackHandler.cs`
- `tests/Acode.Infrastructure.Tests/Fallback/ModeEnforcingFallbackHandlerTests.cs`

**Test Requirements:**
- AC-062: Mode constraints enforced
- AC-063: LocalOnly excludes network models
- AC-064: Airgapped excludes network access
- AC-065: Burst allows cloud models
- AC-066: Validation at chain resolution

---

### Gap 3: Policy Implementation Classes - HIGH PRIORITY (Spec refs but missing)

**Specification Reference:** Spec shows EscalationPolicy enum but no implementation classes

**Issue:** Enum exists (Immediate, RetryThenFallback, CircuitBreaker) but no strategy classes implement the policies.

**What's Missing:**

```csharp
// src/Acode.Infrastructure/Fallback/Policies/IEscalationPolicyStrategy.cs
public interface IEscalationPolicyStrategy
{
    Task<FallbackResult> ExecuteAsync(FallbackContext context);
}

// ImmediatePolicy: Immediately switch to fallback
// RetryThenFallbackPolicy: Retry N times, then fallback
// CircuitBreakerPolicy: Use circuit breaker to decide
```

**Missing Files:**
- `src/Acode.Infrastructure/Fallback/Policies/IEscalationPolicyStrategy.cs`
- `src/Acode.Infrastructure/Fallback/Policies/ImmediatePolicy.cs`
- `src/Acode.Infrastructure/Fallback/Policies/RetryThenFallbackPolicy.cs`
- `src/Acode.Infrastructure/Fallback/Policies/CircuitBreakerPolicy.cs`
- `tests/Acode.Infrastructure.Tests/Fallback/EscalationPolicyTests.cs`

---

### Gap 4: Integration Tests - CRITICAL (3+ tests required)

**Specification Reference:** Testing Requirements section

**What's Missing:** `FallbackIntegrationTests.cs` - no integration tests verify end-to-end behavior

**Required Tests:**
1. Should select first available model in chain
2. Should skip unavailable models and continue chain
3. Should respect circuit breaker state across requests
4. Should integrate with IModelRouter

**File to Create:**
- `tests/Acode.Tests.Integration/Fallback/FallbackIntegrationTests.cs`

**Estimated Scope:** ~150 lines with TestApplicationFactory

---

### Gap 5: E2E Tests - HIGH PRIORITY (3+ tests required)

**Specification Reference:** Testing Requirements section

**What's Missing:** `FallbackE2ETests.cs` - no E2E tests verify CLI and routing together

**Required Tests:**
1. Should run `acode fallback status` and show chains
2. Should run `acode fallback test` and show availability
3. Should run `acode fallback reset` and clear circuits

**File to Create:**
- `tests/Acode.Tests.E2E/Fallback/FallbackE2ETests.cs`

**Estimated Scope:** ~120 lines with E2E fixture

---

### Gap 6: Structured JSON Logging - MEDIUM PRIORITY (AC-060 partial)

**Specification Reference:** AC-060 "Structured logging"

**Issue:** Current logging uses String.Format; spec requires JSON-structured logs

**What's Missing:**
- Structured logging with JSON format
- Machine-parseable fields (event_type, model_id, failure_reason, etc.)
- Proper field extraction for log aggregation

**Files to Modify:**
- `src/Acode.Infrastructure/Fallback/FallbackHandlerWithCircuitBreaker.cs` - Update logging calls

---

## Summary: What Must Be Done to Reach 100% Compliance

### Critical Gaps (MUST DO - blocks task completion)

1. **Security Layer (AC-072 through AC-075)**
   - IModelVerificationService interface + implementation
   - VerifiedModelFallbackHandler wrapper
   - Model URL validation
   - Checksum validation
   - ~8-10 hours

2. **Capability Validation (AC-067 through AC-071)**
   - CapabilityAwareFallbackHandler
   - ModelCapability integration
   - Tool-calling/vision/function-calling checks
   - ~4-6 hours

3. **Operating Mode Enforcement (AC-062 through AC-066)**
   - ModeEnforcingFallbackHandler
   - LocalOnly/Airgapped/Burst filtering
   - Deployment type validation
   - ~3-4 hours

### High Priority Gaps

4. **Policy Implementation Classes**
   - IEscalationPolicyStrategy interface
   - ImmediatePolicy, RetryThenFallbackPolicy, CircuitBreakerPolicy
   - Policy-based decision making
   - ~4-5 hours

5. **Integration Tests**
   - FallbackIntegrationTests.cs (3+ tests)
   - ~5-6 hours

6. **E2E Tests**
   - FallbackE2ETests.cs (3+ tests)
   - ~5-6 hours

### Medium Priority Gaps

7. **Structured JSON Logging**
   - Update logging to JSON format
   - ~2-3 hours

---

## Acceptance Criteria Summary

| Category | Met | Total | Status |
|----------|-----|-------|--------|
| Core Interfaces | 6 | 6 | ✅ 100% |
| Implementation | 13 | 13 | ✅ 100% |
| Configuration | 6 | 6 | ✅ 100% |
| Circuit Breaker | 8 | 8 | ✅ 100% |
| CLI Interface | 11 | 11 | ✅ 100% |
| Logging | 5 | 5 | ✅ 100% |
| Security | 0 | 4 | ❌ 0% |
| Capability Validation | 0 | 5 | ❌ 0% |
| Operating Modes | 1 | 5 | ⚠️ 20% |
| **TOTAL** | **55** | **75** | **73%** |

---

## Next Steps

See `task-009c-completion-checklist.md` for 6-phase implementation plan with detailed steps, code templates, and verification criteria.
