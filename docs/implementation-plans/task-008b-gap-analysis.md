# Task-008b Gap Analysis: Loader/Validator + Selection via Config

**Task**: task-008b-loader-validator-selection-via-config.md (Epic 01)
**Status**: Semantic Gap Analysis (Comprehensive)
**Date**: 2026-01-14
**Specification**: ~2968 lines, comprehensive loader, validator, registry, and configuration-based pack selection
**Semantic Completeness**: 74% (54 of 73 Acceptance Criteria Met)

---

## Executive Summary

Task-008b is **functionally 74% semantically complete** with all core infrastructure implemented, comprehensive test coverage (49 tests), and fully functional CLI commands. However, there are **4 critical semantic gaps** blocking full completion:

1. **Async/Sync Interface Mismatch (CRITICAL)**: IPromptPackRegistry interface is entirely synchronous but implementation internally calls async loader methods with blocking `.GetAwaiter().GetResult()` (anti-pattern). Interface signatures don't match implementation pattern. This violates async best practices and reduces thread efficiency.

2. **Configuration File Reading Not Implemented (GAP)**: PackConfiguration.cs line 53 has TODO comment. Environment variable override works (`ACODE_PROMPT_PACK`), but config file reading for `prompts.pack_id` is missing. Only 50% of AC-052/AC-053 (configuration precedence) is complete.

3. **Missing Error Code Usage (GAP)**: Validator implements only 4 of 6 defined error codes. ACODE-VAL-003 and ACODE-VAL-005 are defined in spec but not used in implementation. This means some validation scenarios may not be properly reported.

4. **Performance Guarantees Unverified (GAP)**: AC-032 specifies "< 100ms validation" but there are no performance tests (no Stopwatch-based tests) to verify this requirement is met.

**Scope**: 4 gaps requiring remediation to reach 100% semantic completeness

**Impact on Downstream Tasks**: 
- Task-008c (Composition) depends on registry API - async/sync mismatch will cause issues
- Task-010 (CLI) will work fine (CLI layer already async-aware)
- Task-011 (Session State) will work fine

**Recommendation**: Fix async/sync mismatches in IPromptPackRegistry, implement config file reading, add missing error codes, add performance tests. Code is functionally correct but interface contracts are broken.

---

## Current State Analysis

### What Exists and is Complete

#### Application Layer Interfaces (✅ Complete)
- **IPromptPackLoader.cs**: 4 methods (3 async, 1 sync with blocking wrapper)
  - ✅ `Task<PromptPack> LoadPackAsync(string path, CancellationToken ct = default)`
  - ✅ `Task<PromptPack> LoadBuiltInPackAsync(string packId, CancellationToken ct = default)`
  - ✅ `Task<PromptPack> LoadUserPackAsync(string path, CancellationToken ct = default)`
  - ⚠️ `bool TryLoadPack(string path, out PromptPack? pack, out string? errorMessage)` [SYNC wrapper - anti-pattern]

- **IPackValidator.cs**: 2 methods (both sync, but ValidatePath performs file I/O)
  - ✅ `ValidationResult Validate(PromptPack pack)`
  - ⚠️ `ValidationResult ValidatePath(string packPath)` [Does file I/O, should be async]
  - ✅ Support classes: ValidationResult, ValidationError, PromptPackInfo

- **IPromptPackRegistry.cs**: 6 sync methods (but spec says should be async)
  - ❌ Interface entirely sync; implementation internally async but with blocking calls

#### Infrastructure Layer Implementations (✅ Mostly Complete)
- **PromptPackLoader.cs**: All 4 methods fully implemented with:
  - ✅ Manifest parsing with YamlDotNet
  - ✅ Component file reading
  - ✅ Content hash verification
  - ✅ Path traversal blocking (PathNormalizer.EnsurePathSafe)
  - ✅ Symlink rejection
  - ✅ Error codes ACODE-PKL-001 through ACODE-PKL-004

- **PackValidator.cs**: All validation logic implemented but with gaps:
  - ✅ Pack ID format validation
  - ✅ Component existence check
  - ✅ Size limits (5MB pack, 1MB per component)
  - ✅ Error codes: ACODE-VAL-001, ACODE-VAL-002, ACODE-VAL-004, ACODE-VAL-006
  - ❌ Missing error codes: ACODE-VAL-003, ACODE-VAL-005
  - ⚠️ Template variable validation lenient (logs but doesn't error)

- **PromptPackRegistry.cs**: Full implementation but with async/sync mismatch:
  - ✅ Pack discovery and indexing
  - ✅ Thread-safe caching (ConcurrentDictionary)
  - ✅ Fallback to default pack
  - ✅ ListPacks(), GetActivePack(), Refresh() methods
  - ❌ Internal blocking async calls (GetAwaiter().GetResult())
  - ⚠️ InitializeAsync() exists but not in interface

- **PackConfiguration.cs**: Partial implementation
  - ✅ Environment variable reading (`ACODE_PROMPT_PACK`)
  - ❌ Config file reading NOT implemented (TODO at line 53)
  - ✅ Default fallback to acode-standard

- **PackCache.cs**: Complete thread-safe caching implementation
  - ✅ Get/Set/Remove/Clear operations
  - ✅ Cache key strategy with PackId + ContentHash

#### Tests (✅ 49 Tests Total, All Passing)
- **PromptPackLoaderTests.cs**: 10 unit tests - complete coverage of loader functionality
- **PackValidatorTests.cs**: 11 unit tests - comprehensive validator testing
- **PromptPackRegistryTests.cs**: 11 unit tests - registry behavior testing
- **PromptsCommandTests.cs**: 17 tests - full CLI command testing

#### CLI Commands (✅ All Implemented)
- **PromptsCommand.cs**: All 3 commands working:
  - ✅ `acode prompts list` - Lists packs with metadata
  - ✅ `acode prompts validate [path]` - Validates pack structure
  - ✅ `acode prompts reload` - Refreshes registry cache

### What Does NOT Exist or is Incomplete

#### 1. Async/Sync Interface Mismatches (BLOCKING)

**IPromptPackRegistry Interface Issue:**
```csharp
// CURRENT (sync interface, blocking implementation)
public PromptPack GetPack(string packId)           // Line 25
public PromptPack? TryGetPack(string packId)       // Line 30
public PromptPack GetActivePack()                  // Line 35
public void Refresh()                              // Line 40
```

**IMPLEMENTATION (async with blocking calls):**
```csharp
// In PromptPackRegistry.cs - actual implementation
public PromptPack GetPack(string packId)
{
    return GetPackAsync(packId).GetAwaiter().GetResult(); // ❌ BLOCKING ANTI-PATTERN
}

public void Refresh()
{
    InitializeAsync().GetAwaiter().GetResult();    // ❌ BLOCKING ANTI-PATTERN
}
```

**Problem**: Using `.GetAwaiter().GetResult()` to block on async operations is an anti-pattern that:
- Reduces thread pool efficiency
- Can cause deadlocks in certain contexts
- Violates async best practices

**Fix Required**: Make IPromptPackRegistry methods async:
```csharp
Task<PromptPack> GetPackAsync(string packId)
Task<PromptPack?> TryGetPackAsync(string packId)
Task<PromptPack> GetActivePackAsync()
Task RefreshAsync()
```

#### 2. IPackValidator.ValidatePath Async Issue (BLOCKING)

**Current**: `ValidationResult ValidatePath(string packPath)` [sync method, does file I/O]

**Problem**: File I/O should be async in modern .NET for responsiveness

**Fix Required**: Change to `Task<ValidationResult> ValidatePathAsync(string packPath)`

#### 3. Configuration File Reading Not Implemented (AC-052, AC-053 Incomplete)

**File**: PackConfiguration.cs, line 53
```csharp
// TODO: Read from .agent/config.yml when config system is available
var configPackId = GetFromConfigFile();  // Returns null
```

**Current Behavior**:
- ✅ Environment variable: `ACODE_PROMPT_PACK` works
- ❌ Config file: `prompts.pack_id` NOT read (TODO)
- ✅ Default: `acode-standard` works

**Configuration Precedence (spec AC-051)**:
```
1. ACODE_PROMPT_PACK environment variable (highest)
2. prompts.pack_id in .agent/config.yml (middle)  ❌ NOT IMPLEMENTED
3. acode-standard default (lowest)
```

**Acceptance Criteria Not Met**: AC-052 (reads config file), AC-053 (applies config correctly)

**Fix Required**: Implement config file reading using IConfiguration from Task-002

#### 4. Missing Error Codes in Validator (AC-030 Incomplete)

**Spec Defines 6 Error Codes**:
- ✅ ACODE-VAL-001: Missing required field / Missing manifest / Empty paths
- ✅ ACODE-VAL-002: Invalid pack ID format
- ❌ ACODE-VAL-003: **MISSING** - Invalid version (mentioned in spec, not implemented)
- ✅ ACODE-VAL-004: Component not found
- ❌ ACODE-VAL-005: **MISSING** - Invalid template (mentioned in spec, not implemented)
- ✅ ACODE-VAL-006: Size limits exceeded

**Where VAL-003 Should Be Used**: When pack version doesn't follow SemVer format
```csharp
// Currently missing - should add to PackValidator
if (!ValidateSemVer(pack.Version))
{
    errors.Add(new ValidationError
    {
        Code = "ACODE-VAL-003",
        Message = "Invalid version format. Must follow SemVer (e.g., 1.0.0)",
        FilePath = "manifest.yml"
    });
}
```

**Where VAL-005 Should Be Used**: When pack has invalid template variable syntax
```csharp
// Currently missing - should add to PackValidator
if (HasInvalidTemplateVariables(pack))
{
    errors.Add(new ValidationError
    {
        Code = "ACODE-VAL-005",
        Message = "Invalid template variable syntax. Must match {{variable_name}}",
        FilePath = component.Path
    });
}
```

#### 5. Performance Guarantees Not Tested (AC-032)

**Spec Requirement**: Validation must complete in < 100ms for packs up to 5MB

**Current Testing**: No performance tests
- ✅ Functional tests pass
- ❌ No `[Fact]` test with Stopwatch to verify < 100ms
- ❌ No performance benchmark for validation

**Fix Required**: Add PackValidationPerformanceTests.cs with test:
```csharp
[Fact]
public void ValidatePath_ShouldCompleteLessThan100ms()
{
    var stopwatch = Stopwatch.StartNew();
    var result = _validator.ValidatePath(_largePackPath);
    stopwatch.Stop();
    
    Assert.True(result.IsValid);
    Assert.True(stopwatch.ElapsedMilliseconds < 100);
}
```

---

## Acceptance Criteria Coverage Scorecard

| Category | Met | Partial | Gap | Coverage |
|----------|-----|---------|-----|----------|
| Loader Interface (5 AC) | 5 | 0 | 0 | 100% |
| Loader Implementation (10 AC) | 10 | 0 | 0 | 100% |
| Validator Interface (8 AC) | 8 | 0 | 0 | 100% |
| Validator Implementation (9 AC) | 8 | 1 | 0 | 89% |
| Registry Interface (6 AC) | 0 | 6 | 0 | 0% ❌ BLOCKING |
| Registry Implementation (9 AC) | 9 | 0 | 0 | 100% |
| Configuration (9 AC) | 4 | 3 | 2 | 44% ❌ GAP |
| Caching (7 AC) | 7 | 0 | 0 | 100% |
| CLI (9 AC) | 9 | 0 | 0 | 100% |
| Performance (1 AC) | 0 | 1 | 0 | 0% ❌ UNVERIFIED |
| Error Codes (1 AC) | 0 | 0 | 1 | 0% ❌ GAP |

**Total: 54/73 = 74% Semantic Completeness**

---

## Remediation Strategy

### Phase 1: Fix IPromptPackRegistry Async/Sync Mismatch (BLOCKING)

**Impact**: High - affects downstream Task-008c usage

**Files to Change**:
1. `src/Acode.Application/PromptPacks/IPromptPackRegistry.cs`
   - [ ] Change all 6 methods to async: `GetPackAsync`, `TryGetPackAsync`, `GetActivePackAsync`, `RefreshAsync`
   - [ ] Keep `GetActivePackIdAsync` (already sync for configuration)
   - [ ] Add `ListPacksAsync` for consistency

2. `src/Acode.Infrastructure/PromptPacks/PromptPackRegistry.cs`
   - [ ] Remove `.GetAwaiter().GetResult()` blocking calls
   - [ ] Implement async properly throughout
   - [ ] Update method signatures to match async interface

3. Update all consumer code:
   - [ ] CLI Commands (PromptsCommand.cs)
   - [ ] Tests (PromptPackRegistryTests.cs - already use async internally?)

4. Update tests:
   - [ ] PromptPackRegistryTests.cs - ensure tests properly await async methods

### Phase 2: Implement Configuration File Reading (HIGH)

**File**: `src/Acode.Infrastructure/PromptPacks/PackConfiguration.cs`

- [ ] Remove TODO comment at line 53
- [ ] Implement `GetFromConfigFile()` method:
  - [ ] Read `.agent/config.yml`
  - [ ] Extract `prompts.pack_id` value
  - [ ] Return null if not found (fallback to default)
- [ ] Test precedence: env var > config file > default
- [ ] Update tests: PackConfigurationTests.cs to verify config file reading

### Phase 3: Add Missing Error Codes to Validator (HIGH)

**File**: `src/Acode.Infrastructure/PromptPacks/PackValidator.cs`

- [ ] Add ACODE-VAL-003 for version format validation
- [ ] Add ACODE-VAL-005 for template variable validation
- [ ] Update PackValidatorTests.cs with tests for new error codes:
  - [ ] Test: `Should_Fail_With_VAL_003_On_Invalid_Version`
  - [ ] Test: `Should_Fail_With_VAL_005_On_Invalid_Templates`

### Phase 4: Add Performance Tests (MEDIUM)

**New File**: `tests/Acode.Infrastructure.Tests/PromptPacks/PackValidationPerformanceTests.cs`

- [ ] Add test: `ValidatePath_ShouldCompleteLessThan100ms`
- [ ] Add test: `Validate_ShouldCompleteLessThan10ms` (in-memory)
- [ ] Create test data: large 5MB pack for performance testing

### Phase 5: Verify Build and Tests (FINAL)

- [ ] Run: `dotnet build --configuration Debug`
- [ ] Verify no compiler warnings
- [ ] Run: `dotnet test --filter "FullyQualifiedName~PromptPacks" --verbosity normal`
- [ ] Verify all 49+ tests pass
- [ ] Run CLI verification: `acode prompts list`, `acode prompts validate`, `acode prompts reload`

---

## Test Coverage Summary

**Current**: 49 tests total (all passing)
- PromptPackLoaderTests: 10 tests
- PackValidatorTests: 11 tests
- PromptPackRegistryTests: 11 tests
- PromptsCommandTests: 17 tests

**After Fixes**: 53+ tests (adding ~4 new tests)
- Add: Should_Fail_With_VAL_003_On_Invalid_Version
- Add: Should_Fail_With_VAL_005_On_Invalid_Templates
- Add: ValidatePath_ShouldCompleteLessThan100ms
- Add: PackConfiguration_Should_Read_From_ConfigFile

---

## Implementation Dependencies

- **Task-008a** (File Layout & Hashing): PromptPack domain model - ✅ Used by all components
- **Task-002** (Configuration System): IConfiguration interface - ✅ Used by PackConfiguration (but config file reading TODO)
- **Task-008c** (Composition): Depends on registry - ⚠️ Will need async API once fixed
- **Task-010** (CLI): Uses registry - ✅ Already working, will adapt to async

---

## References

- **Gap Analysis Methodology**: CLAUDE.md Section 3.2
- **Spec File**: docs/tasks/refined-tasks/Epic 01/task-008b-loader-validator-selection-via-config.md
- **Implementation Commit**: Last commit to task-008b files
- **Related Gap Analyses**: task-008a (65 of 73 AC - 92% semantic completeness)
