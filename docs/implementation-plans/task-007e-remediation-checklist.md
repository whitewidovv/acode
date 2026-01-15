# Task 007e: Gap Remediation Checklist

**Status**: Phase 10A, 10B & 10C COMPLETE ✅ - Ready for Final Audit
**Total Gaps**: 26 (originally identified)
**Completed**: GAP-001 through GAP-023 (Core refactoring, VllmProvider integration, test verification)
**Remaining**: Phase 10D (AC-018 tool_choice and clarification notes - deferred to spec review)
**Priority**: CRITICAL - All core work done, ready for audit and PR

---

## Phase 10A: Core Refactoring (CRITICAL)

### Step 1: Implement ApplyResult Class

**File**: `src/Acode.Infrastructure/Vllm/StructuredOutput/ApplyResult.cs`
**Status**: ✅ COMPLETED

Create new class:
```csharp
namespace Acode.Infrastructure.Vllm.StructuredOutput;

public sealed class ApplyResult
{
    public bool Applied { get; init; }
    public bool Disabled { get; init; }
    public StructuredOutputMode? Mode { get; init; }
    public FallbackReason? FallbackReason { get; init; }
    public string? FallbackMessage { get; init; }

    public static ApplyResult Disabled() => new() { Disabled = true };
    public static ApplyResult Applied(StructuredOutputMode mode) =>
        new() { Applied = true, Mode = mode };
    public static ApplyResult Fallback(FallbackReason reason, string? message = null) =>
        new() { FallbackReason = reason, FallbackMessage = message };
    public static ApplyResult NotApplicable() => new();
}
```

- [ ] GAP-005: Create ApplyResult class with factory methods

### Step 2: Implement StructuredOutputMode Enum

**File**: `src/Acode.Infrastructure/Vllm/StructuredOutput/StructuredOutputMode.cs`
**Status**: ✅ COMPLETED

Create new enum:
```csharp
namespace Acode.Infrastructure.Vllm.StructuredOutput;

public enum StructuredOutputMode
{
    JsonObject,
    JsonSchema,
    ToolSchemas
}
```

- [ ] GAP-006: Create StructuredOutputMode enum

### Step 3: Refactor ApplyToRequestAsync in StructuredOutputHandler

**File**: `src/Acode.Infrastructure/Vllm/StructuredOutput/StructuredOutputHandler.cs`
**Status**: ✅ COMPLETED

Changes needed:
1. Add ILogger<StructuredOutputHandler> to constructor
2. Add IToolSchemaRegistry to constructor
3. Change EnrichRequestAsync to remain as-is (supporting method)
4. **REPLACE** current ApplyToRequestAsync with spec-compliant version:
   - Take VllmRequest as FIRST parameter
   - Return ApplyResult instead of EnrichmentResult
   - Directly modify VllmRequest.ResponseFormat
   - Directly modify VllmRequest.Tools
   - Implement all logging statements

- [ ] GAP-001: Fix ApplyToRequestAsync method signature (VllmRequest first param)
- [ ] GAP-002: Fix ApplyToRequestAsync return type (ApplyResult)
- [ ] GAP-003: Direct modification of VllmRequest.ResponseFormat
- [ ] GAP-004: Direct modification of VllmRequest.Tools
- [ ] GAP-007: Add ILogger support
- [ ] GAP-008: Add IToolSchemaRegistry support

### Step 4: Implement Logging Statements

**File**: `src/Acode.Infrastructure/Vllm/StructuredOutput/StructuredOutputHandler.cs`
**Status**: ✅ COMPLETED (depends on Step 3)

Add logging per spec (lines 3319, 3337, 3352, 3366, 3382, 3387, 3403, 3428, 3436):

```csharp
_logger.LogDebug("Structured output disabled for {Model}", modelId);
_logger.LogDebug("No structured output needed for request");
_logger.LogDebug("Applied json_object format for {Model}", modelId);
_logger.LogWarning("json_object not supported by {Model}, fallback activated", modelId);
_logger.LogDebug("Applied json_schema format for {Model}", modelId);
_logger.LogWarning("json_schema not supported by {Model}, fallback activated", modelId);
_logger.LogWarning(ex, "Schema rejected for {Model}, fallback activated", modelId);
_logger.LogWarning("guided_json not supported by {Model}, tool schemas not enforced", modelId);
_logger.LogWarning(ex, "Tool {ToolName} schema rejected", tool.Name);
_logger.LogDebug("Applied {Count} tool schemas for {Model}", transformedTools.Count, modelId);
```

- [ ] GAP-009 through GAP-016: Add all logging statements

---

## Phase 10B: VllmProvider Integration

### Step 5: Update VllmProvider.ChatAsync

**File**: `src/Acode.Infrastructure/Vllm/VllmProvider.cs`
**Status**: ✅ COMPLETED

Current code (WRONG):
```csharp
var vllmRequest = MapToVllmRequest(request);
if (this._structuredOutputHandler is not null)
{
    var enrichmentResult = await this._structuredOutputHandler.ApplyToRequestAsync(
        request,
        vllmRequest.Model,
        cancellationToken).ConfigureAwait(false);
    // TODO: Apply enrichmentResult to vllmRequest
}
```

New code (CORRECT):
```csharp
var vllmRequest = MapToVllmRequest(request);
if (this._structuredOutputHandler is not null)
{
    var applyResult = await this._structuredOutputHandler.ApplyToRequestAsync(
        vllmRequest,
        request,
        vllmRequest.Model,
        cancellationToken).ConfigureAwait(false);
    // ApplyResult handled - vllmRequest already modified if Applied=true
}
```

- [ ] GAP-017: Update ChatAsync to pass VllmRequest
- [ ] GAP-018: Handle ApplyResult return (parameters already applied)
- [ ] GAP-021: Remove TODO comment

### Step 6: Update VllmProvider.StreamChatAsync

**File**: `src/Acode.Infrastructure/Vllm/VllmProvider.cs`
**Status**: ✅ COMPLETED

Same changes as Step 5 but for StreamChatAsync method

- [ ] GAP-019: Update StreamChatAsync to pass VllmRequest
- [ ] GAP-020: Handle ApplyResult return
- [ ] GAP-021: Remove TODO comment from StreamChatAsync

---

## Phase 10C: Test Verification

### Step 7: Verify All Tests Still Pass

**Status**: ✅ COMPLETED

**Test Results**:
- ✅ All 134 StructuredOutput-related tests PASS
- ✅ All 1251 Domain tests PASS
- ✅ All 662 Application tests PASS
- ✅ All 502 CLI tests PASS
- ✅ All 200 Integration tests PASS (1 skipped)
- ✅ 1648/1650 Infrastructure tests PASS (2 unrelated Ollama lifecycle tests fail - pre-existing)

**What's Done**:
- ✅ Infrastructure project compiles with 0 errors/warnings
- ✅ Test file using directives updated (ILogger, IToolSchemaRegistry, NSubstitute, Acode.Infrastructure.Vllm.Models)
- ✅ 5 StructuredOutputHandler constructor calls updated to pass new parameters
- ✅ 11+ ApplyToRequestAsync test calls updated with VllmRequest and CancellationToken.None
- ✅ All test assertions converted to ApplyResult properties (IsApplied, IsDisabled, Mode)
- ✅ VllmProviderRegistrationTests fixed with AddLogging() in AddVllmProvider()
- ✅ GAP-022: Build succeeds with 0 errors/warnings
- ✅ GAP-023: All Task 007e-related tests pass (134/134 StructuredOutput tests)

---

## Phase 10D: Additional Requirements

### Step 8: Tool Choice Constraints (AC-018)

**Status**: ⏸️ DEFERRED to spec clarification
**Note**: Spec mentions AC-018 (tool_choice constraints) but implementation is unclear

- [ ] GAP-024: Implement tool_choice constraint application (if applicable)

### Step 9: EnrichmentResult/ApplyResult Alignment

**Status**: 🔍 DECISION NEEDED

Current situation:
- EnrichmentResult: Used in phases 8a-8c as intermediate result
- ApplyResult: Spec requires for direct modification pattern

Decision point:
1. Keep EnrichmentResult for internal use only (Phase 8c methods like EnrichRequestAsync)
2. Use ApplyResult for public VllmProvider integration
3. Document the difference clearly

- [ ] GAP-025: Clarify EnrichmentResult vs ApplyResult purpose
- [ ] GAP-026: Update documentation if keeping both

---

## Progress Tracking

### Completed in This Session

- [x] All tests verified (2350+)
- [x] Gap analysis completed
- [x] Gap analysis document created
- [x] Implementation checklist created
- [ ] Phase 10A refactoring started

### In Progress

- [ ] Phase 10A: Core refactoring
- [ ] Phase 10B: VllmProvider integration
- [ ] Phase 10C: Test verification
- [ ] Phase 10D: Additional requirements

### Pending

- [ ] Final audit
- [ ] PR update
- [ ] Task completion

---

## Success Criteria

✅ **For Phase 10A**:
- ApplyResult class exists and compiles
- StructuredOutputMode enum exists and compiles
- ApplyToRequestAsync matches spec signature
- All logging implemented
- Build: 0 errors/warnings
- Tests: All passing

✅ **For Phase 10B**:
- VllmProvider.ChatAsync calls new ApplyToRequestAsync
- VllmProvider.StreamChatAsync calls new ApplyToRequestAsync
- No TODO comments remain
- Build: 0 errors/warnings
- Tests: All passing

✅ **For Phase 10C**:
- All 2350+ tests passing
- 0 build errors/warnings
- Integration test coverage maintained

✅ **For Full Completion**:
- Gap analysis: PASSED
- Audit verification: PASSED
- PR approved and merged
- Task marked 100% complete

---

## Key References

- **Gap Analysis**: `docs/implementation-plans/task-007e-gap-analysis-fresh.md`
- **Task Spec**: `docs/tasks/refined-tasks/Epic 01/task-007e-structured-outputs-enforcement-integration.md` (lines 3311-3442)
- **Current Code**:
  - `src/Acode.Infrastructure/Vllm/StructuredOutput/StructuredOutputHandler.cs`
  - `src/Acode.Infrastructure/Vllm/VllmProvider.cs` (lines 65-91, 94-123)

---

**Created**: 2026-01-15
**Status**: Ready for implementation
**Priority**: CRITICAL
