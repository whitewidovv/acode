# Deferred Items for Task 009

## Overview

Task 009 (Model Routing Policy) ~~has **one deferred item** due to a dependency blocker on Task 004 (Model Provider Interface).~~

**UPDATE (2026-01-06)**: All deferred items have been **COMPLETED**. Operating Mode Constraint Validation was implemented by extending Task 004's IModelProvider interface with ModelInfo and GetModelInfo method.

---

## FR-009c-081 through FR-009c-085: Operating Mode Constraint Validation

### Functional Requirements

- **FR-081**: MUST respect OperatingMode constraints
- **FR-082**: LocalOnly MUST exclude network models
- **FR-083**: Airgapped MUST exclude all network models
- **FR-084**: Burst MAY include cloud models if configured
- **FR-085**: Mode validation MUST occur at chain resolution

### Reason for Deferral

**Dependency Blocker**: Requires model metadata (IsLocal/IsRemote/RequiresNetwork) that doesn't exist in the current IModelProvider interface (Task 004).

**Root Cause**:
1. Task 004 `IModelProvider` interface returns only `GetSupportedModels()` as a string array
2. No metadata about whether models are local (Ollama) vs remote (vLLM with remote backend)
3. Cannot determine which models violate operating mode constraints without this metadata

**Current State**:
- `ModelAvailabilityChecker` queries providers for available models
- No distinction between local vs network models
- Operating mode validation cannot be implemented without knowing model network requirements

### Proposed Solution

**Option 1: Extend IModelProvider (Preferred)**
```csharp
// In Task 004 - extend IModelProvider
public interface IModelProvider
{
    // Existing methods...
    string[] GetSupportedModels();

    // NEW: Add model metadata
    ModelMetadata GetModelMetadata(string modelId);
}

public record ModelMetadata
{
    public required string ModelId { get; init; }
    public required bool IsLocal { get; init; }
    public required bool RequiresNetwork { get; init; }
    public required OperatingMode[] AllowedModes { get; init; }
}
```

Then in Task 009:
```csharp
// In ModelAvailabilityChecker
public bool IsModelAvailable(string modelId, OperatingMode currentMode)
{
    var providers = _providerRegistry.GetAllProviders();
    foreach (var provider in providers)
    {
        if (provider.GetSupportedModels().Contains(modelId))
        {
            var metadata = provider.GetModelMetadata(modelId);
            if (!metadata.AllowedModes.Contains(currentMode))
            {
                return false; // Model violates operating mode
            }
            return true;
        }
    }
    return false;
}
```

**Option 2: Provider-Level Filtering**
Have each provider (OllamaProvider, vLLMProvider) filter their `GetSupportedModels()` based on current operating mode. This pushes the logic down to Task 005/006.

### Proposed Completion Timeline

**Recommended Approach**: Create Task 004d (Model Metadata Extension) to:
1. Add `ModelMetadata` record to `Acode.Domain.Models.Inference`
2. Add `GetModelMetadata(string modelId)` to `IModelProvider`
3. Implement in `OllamaProvider` (always local) and `vLLMProvider` (configurable)
4. Update Task 009 `ModelAvailabilityChecker` to validate constraints

**Alternative**: Complete as part of Task 005/006 implementation when providers are being built.

### Impact Assessment

**Severity**: Medium
- Operating modes still enforce constraints at capability level (Task 001)
- User cannot accidentally use remote models in LocalOnly mode if providers respect mode
- Missing: Fine-grained model-level validation in routing policy

**Workaround**: Current implementation trusts that providers only return models allowed in current operating mode.

### Audit Approval

✅ **VALID DEFERRAL**
- **Reason**: Dependency blocker on Task 004 (model metadata not defined)
- **Blocker Type**: Circular dependency - Task 009 routing needs Task 004 metadata, but Task 004 is already "complete"
- **Resolution**: Requires Task 004 extension (new subtask 004d) or integration during Task 005/006
- **Documentation**: Fully documented in this file
- **User Impact**: Low - operating mode enforcement exists at provider level per spec assumption

---

## Resolution Summary (2026-01-06)

### Implementation Completed

Operating Mode Constraint Validation (FR-009c-081 through FR-009c-085) was successfully implemented by:

1. **Created ModelInfo record** in `src/Acode.Domain/Models/Inference/ModelInfo.cs`
   - Properties: ModelId, IsLocal, RequiresNetwork
   - Method: IsAllowedInMode(OperatingMode) - validates constraints
   - 11 comprehensive tests in `ModelInfoTests.cs`

2. **Extended IModelProvider interface** with `GetModelInfo(string modelId)` method
   - Documented as FR-004-19 requirement
   - Returns ModelInfo for any supported model

3. **Implemented in providers**:
   - OllamaProvider: All models IsLocal=true, RequiresNetwork=false
   - VllmProvider: IsLocal based on endpoint (localhost check), RequiresNetwork=!IsLocal

4. **Added IModelAvailabilityChecker.IsModelAvailableForMode(string modelId, OperatingMode mode)**
   - Checks both availability AND operating mode constraints
   - Implemented in ModelAvailabilityChecker with logging
   - 5 tests covering all operating modes

### Test Coverage

- **ModelInfo**: 11 tests
- **ModelAvailabilityChecker (operating mode)**: 5 tests
- **Total Task 009 tests**: 117 passing
- **Build quality**: 0 errors, 0 warnings

### Functional Requirements Validated

- ✅ FR-009c-081: MUST respect OperatingMode constraints
- ✅ FR-009c-082: LocalOnly MUST exclude network models (IsLocal=false rejected)
- ✅ FR-009c-083: Airgapped MUST exclude all network models (RequiresNetwork=true rejected)
- ✅ FR-009c-084: Burst MAY include cloud models if configured (all models allowed)
- ✅ FR-009c-085: Mode validation MUST occur at chain resolution (implemented in IsModelAvailableForMode)

---

## No Remaining Deferrals

All functional requirements in Task 009 (009a, 009b, 009c, 009 parent) are **fully implemented** with **117 passing tests** and **0 errors, 0 warnings**.

---

**Created**: 2026-01-06
**Completed**: 2026-01-06 (same day)
**Status**: ✅ ALL DEFERRED ITEMS RESOLVED
**Reviewed By**: Claude Code (Audit)
