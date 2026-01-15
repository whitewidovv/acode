# Task-008c Completion Checklist: Starter Packs Implementation

**Task**: task-008c-starter-packs-dotnet-react-strict-minimal-diff.md (Epic 01)
**Current Status**: 57% Semantically Complete (Comprehensive Blocking Issues)
**Date**: 2026-01-14
**Instructions for Next Agent**: This checklist contains ONLY what's missing to reach 100% semantic completeness. Each item includes enough context and specifics that a fresh-context agent can pick it up and implement it without confusion. Follow items in order (dependencies exist). Mark [🔄] when starting, [✅] when complete.

---

## Critical Findings Summary

**What Exists** (DO NOT recreate - just use):
- ✅ 19 prompt pack resource files (acode-standard, acode-dotnet, acode-react) with valid YAML and correct content
- ✅ 162 unit tests written across 14 test files (all passing when not blocked by compilation)
- ✅ Core domain classes: PromptPack, LoadedComponent, PackVersion, etc.
- ✅ Core infrastructure: EmbeddedPackProvider, PackCache, PromptComposer, PromptPackLoader, PromptPackRegistry

**What's Missing** (Fix in this order):
1. Fix 5 integration tests blocked by compilation errors (API mismatch in test code)
2. Create 8 E2E tests file
3. Create 4 performance benchmark tests
4. Rename PackPath property → Directory (spec compliance)
5. Verify all 50+ Acceptance Criteria

---

## PHASE 1: Fix Blocked Integration Tests (CRITICAL - BLOCKING BUILD)

### 1.1 - Fix StarterPackLoadingTests.cs Compilation Errors [🔄 → ✅]

**Status**: ❌ BLOCKED - 16 compile errors preventing test execution

**File**: `tests/Acode.Integration.Tests/PromptPacks/StarterPackLoadingTests.cs`

**Problem**: Test was written against spec assumptions that differ from actual implementation

**Errors to Fix** (16 total):

1. **Error CS1739**: Constructor parameter mismatch
   - **Current code**: `new PromptPackRegistry(packDirectory: tempDir, ...)`
   - **Reality**: Check constructor signature with `grep -n "public PromptPackRegistry" src/Acode.Infrastructure/PromptPacks/PromptPackRegistry.cs`
   - **Fix**: Update constructor call to match actual parameters

2. **Error CS1061** (appears 13 times): `PromptPack.Manifest` property doesn't exist
   - **Current code**: `pack.Manifest.Id`, `pack.Manifest.Version`, `pack.Manifest.Components`, etc.
   - **Reality**: Properties are flattened directly on PromptPack
   - **Correct code**: `pack.Id`, `pack.Version`, `pack.Components` (no `.Manifest.` prefix)
   - **Locations in file**: Search for all instances of `pack.Manifest.` and replace with direct property access
   - **Commands to run**:
     ```bash
     grep -n "pack.Manifest" tests/Acode.Integration.Tests/PromptPacks/StarterPackLoadingTests.cs
     # Then edit each one to remove .Manifest
     ```

3. **Error CS1061**: `IReadOnlyList<LoadedComponent>.Keys` property doesn't exist
   - **Current code**: `components.Keys` (treating as dictionary)
   - **Reality**: `components` is IReadOnlyList<LoadedComponent>, not Dictionary
   - **Correct code**: Iterate with `components` directly or use `components.GetComponent(path)`
   - **Examples of correct patterns**:
     ```csharp
     // Instead of: components.Keys.Contains(...)
     // Use: components.Any(c => c.Path == ...)
     
     // Instead of: components["system.md"]
     // Use: components.GetComponent("system.md") or components.FirstOrDefault(c => c.Path == "system.md")
     ```

4. **Error CS1061**: `IPromptPackRegistry.ListPacksAsync()` method doesn't exist
   - **Current code**: `await registry.ListPacksAsync()`
   - **Reality**: Method is synchronous: `registry.ListPacks()`
   - **Correct code**: `var packs = registry.ListPacks();` (no await)

5. **IDE0005**: Unnecessary using directive
   - **Fix**: Remove any imports that are no longer used after fixing above errors

**Verification Steps**:
- [ ] Run: `dotnet build tests/Acode.Integration.Tests/`
- [ ] Expected: No CS errors remaining
- [ ] Verify output contains: "Build succeeded. - 0 Error(s)"

**Test Execution After Fix**:
```bash
dotnet test tests/Acode.Integration.Tests/PromptPacks/StarterPackLoadingTests.cs --verbosity normal
# Should see: 5 tests passing (or at least 0 failures if some are skipped)
```

**AC-Coverage**: Fixes verification of AC-030 through AC-035 (integration loading tests)

---

## PHASE 2: Create Missing E2E Tests

### 2.1 - Create StarterPackE2ETests.cs [🔄 → ✅]

**Status**: ❌ MISSING - File does not exist

**File Location**: `tests/Acode.E2E.Tests/PromptPacks/StarterPackE2ETests.cs`

**Spec Reference**: Lines 1876-2054 in task-008c-starter-packs-dotnet-react-strict-minimal-diff.md

**Required Test Count**: 8 test methods

**Test Methods to Implement** (in order):

1. **Test: `Should_Use_Standard_Pack_By_Default`**
   - **Arrange**: Initialize PromptPackRegistry with default configuration (no env var, no config file)
   - **Act**: Call `registry.GetActivePack()`
   - **Assert**: 
     - Pack ID should be "acode-standard"
     - Pack should contain system.md component
     - Should contain roles/planner.md, roles/coder.md, roles/reviewer.md
   - **AC-Coverage**: AC-036 (default pack selection)

2. **Test: `Should_Switch_To_DotNet_Pack_Via_Environment_Variable`**
   - **Arrange**: Set environment variable `ACODE_PROMPT_PACK=acode-dotnet`, initialize registry
   - **Act**: Call `registry.GetActivePack()`
   - **Assert**:
     - Pack ID should be "acode-dotnet"
     - Pack should include system.md + roles/ + languages/csharp.md + frameworks/aspnetcore.md
   - **AC-Coverage**: AC-037 (env var override)

3. **Test: `Should_Switch_To_React_Pack_Via_Environment_Variable`**
   - **Arrange**: Set environment variable `ACODE_PROMPT_PACK=acode-react`
   - **Act**: Call `registry.GetActivePack()`
   - **Assert**: Pack ID "acode-react" with typescript.md and react.md components
   - **AC-Coverage**: AC-037 (env var override)

4. **Test: `Should_Apply_Language_Specific_Prompts_In_Composition`**
   - **Arrange**: Load acode-dotnet pack, use PromptComposer
   - **Act**: Compose prompts for language context "csharp"
   - **Assert**: Output includes C# guidance (naming conventions, async patterns, nullable refs)
   - **AC-Coverage**: AC-038 (language composition)

5. **Test: `Should_Apply_Framework_Specific_Prompts_In_Composition`**
   - **Arrange**: Load acode-dotnet pack, use PromptComposer
   - **Act**: Compose prompts for framework context "aspnetcore"
   - **Assert**: Output includes ASP.NET Core guidance (DI, controllers, EF Core)
   - **AC-Coverage**: AC-039 (framework composition)

6. **Test: `Should_Include_Planner_Role_Specific_Prompts_In_Composition`**
   - **Arrange**: Load standard pack, use PromptComposer
   - **Act**: Compose prompts for role "planner"
   - **Assert**: Output includes planning guidance (decomposition, dependencies, task breakdown)
   - **AC-Coverage**: AC-040 (role composition - planner)

7. **Test: `Should_Include_Coder_Role_Specific_Prompts_In_Composition`**
   - **Arrange**: Load standard pack, use PromptComposer
   - **Act**: Compose prompts for role "coder"
   - **Assert**: Output includes "Strict Minimal Diff" principle, correct/wrong code examples
   - **AC-Coverage**: AC-041 (role composition - coder)

8. **Test: `Should_Include_Reviewer_Role_Specific_Prompts_In_Composition`**
   - **Arrange**: Load standard pack, use PromptComposer
   - **Act**: Compose prompts for role "reviewer"
   - **Assert**: Output includes review checklist with "Minimal Diff Validation" section
   - **AC-Coverage**: AC-042 (role composition - reviewer)

**Template Code Structure** (use as base):
```csharp
using Xunit;
using Acode.Application.PromptPacks;
using Acode.Infrastructure.PromptPacks;

namespace Acode.E2E.Tests.PromptPacks;

public class StarterPackE2ETests : IDisposable
{
    private readonly PromptPackRegistry _registry;
    private readonly IPromptComposer _composer;
    
    public StarterPackE2ETests()
    {
        // Initialize registry with test configuration
        _registry = new PromptPackRegistry(/* constructor params */);
        // Initialize composer
        _composer = new PromptComposer();
    }

    [Fact]
    public void Should_Use_Standard_Pack_By_Default()
    {
        // ARRANGE
        // (no special setup - default is acode-standard)
        
        // ACT
        var activePack = _registry.GetActivePack();
        
        // ASSERT
        Assert.NotNull(activePack);
        Assert.Equal("acode-standard", activePack.Id);
        Assert.NotNull(activePack.GetComponent("system.md"));
        // ... more assertions
    }

    // ... implement remaining 7 tests following pattern above

    public void Dispose()
    {
        _registry?.Dispose();
    }
}
```

**Verification Steps**:
- [ ] File created at: `tests/Acode.E2E.Tests/PromptPacks/StarterPackE2ETests.cs`
- [ ] Run: `dotnet test tests/Acode.E2E.Tests/PromptPacks/StarterPackE2ETests.cs --verbosity normal`
- [ ] Expected: 8 tests passing

**AC-Coverage**: AC-036 through AC-042 (7 ACs for E2E tests)

---

## PHASE 3: Create Performance Benchmarks

### 3.1 - Create PackLoadingBenchmarks.cs [🔄 → ✅]

**Status**: ❌ MISSING - File does not exist

**File Location**: `tests/Acode.Performance.Tests/PromptPacks/PackLoadingBenchmarks.cs`

**Spec Reference**: Lines 2060-2111 in task-008c-starter-packs-dotnet-react-strict-minimal-diff.md

**Required Benchmark Count**: 4 benchmarks

**Performance Targets** (from spec):
- First load: 100-150ms (includes extraction from embedded resources)
- Cached load: < 5ms (subsequent calls from cache)

**Benchmarks to Implement**:

1. **Benchmark: `Benchmark_Load_Standard_Pack_First_Time`**
   - **Setup**: Clear cache before each iteration
   - **Operation**: `registry.GetActivePack()` with standard pack
   - **Expected**: 100-150ms
   - **Measurement**: Total elapsed time
   - **Iterations**: 5 (small number due to high latency)

2. **Benchmark: `Benchmark_Load_Standard_Pack_From_Cache`**
   - **Setup**: Load once, then warm cache
   - **Operation**: `registry.GetActivePack()` (repeated 10x)
   - **Expected**: < 5ms per call
   - **Measurement**: Average time
   - **Iterations**: 10 (higher iterations for low-latency operation)

3. **Benchmark: `Benchmark_Load_DotNet_Pack_First_Time`**
   - **Setup**: Clear cache, switch config to acode-dotnet
   - **Operation**: `registry.GetActivePack()`
   - **Expected**: 100-150ms
   - **Iterations**: 5

4. **Benchmark: `Benchmark_Load_React_Pack_First_Time`**
   - **Setup**: Clear cache, switch config to acode-react
   - **Operation**: `registry.GetActivePack()`
   - **Expected**: 100-150ms
   - **Iterations**: 5

**Template Code Structure** (use BenchmarkDotNet):
```csharp
using BenchmarkDotNet.Attributes;
using Acode.Application.PromptPacks;
using Acode.Infrastructure.PromptPacks;

namespace Acode.Performance.Tests.PromptPacks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, targetCount: 5)]
public class PackLoadingBenchmarks
{
    private PromptPackRegistry _registry;
    
    [GlobalSetup]
    public void Setup()
    {
        _registry = new PromptPackRegistry(/* constructor params */);
    }

    [Benchmark(Description = "Load Standard Pack (First Time)")]
    public PromptPack LoadStandardPackFirstTime()
    {
        // Clear cache
        _registry.Refresh();
        
        // Load pack
        return _registry.GetActivePack();
    }

    [Benchmark(Description = "Load Standard Pack (From Cache)")]
    public PromptPack LoadStandardPackFromCache()
    {
        // Pack should already be cached from previous call
        return _registry.GetActivePack();
    }

    // ... implement remaining 2 benchmarks following pattern above

    [GlobalCleanup]
    public void Cleanup()
    {
        _registry?.Dispose();
    }
}
```

**Verification Steps**:
- [ ] File created at: `tests/Acode.Performance.Tests/PromptPacks/PackLoadingBenchmarks.cs`
- [ ] Run: `dotnet test tests/Acode.Performance.Tests/PromptPacks/PackLoadingBenchmarks.cs --configuration Release --verbosity normal`
- [ ] Expected output should show all benchmarks completed with timings:
  - First load: 100-150ms ✓
  - Cached load: < 5ms ✓

**AC-Coverage**: AC-043 (performance verification)

---

## PHASE 4: Fix Property Naming

### 4.1 - Rename PackPath → Directory Property [🔄 → ✅]

**Status**: ⚠️ NAMING INCONSISTENCY - Property name differs from spec

**Spec Expectation**: `pack.Directory`
**Current Implementation**: `pack.PackPath`

**Files to Update**:

1. **File**: `src/Acode.Domain/PromptPacks/PromptPack.cs`
   - **Current**: `public string PackPath { get; init; }`
   - **Change to**: `public string Directory { get; init; }`
   - **Command**: `grep -n "PackPath" src/Acode.Domain/PromptPacks/PromptPack.cs`

2. **Update all references** (search and replace):
   ```bash
   grep -r "pack\.PackPath" src/ tests/ --include="*.cs"
   # Replace all with: pack.Directory
   ```

3. **Update test code**:
   - Search: `AssertUtils.AreEqual(pack.PackPath, ...)`
   - Replace with: `AssertUtils.AreEqual(pack.Directory, ...)`

4. **Update documentation**:
   - If PackPath is mentioned in XML comments, change to Directory

**Verification Steps**:
- [ ] Run: `dotnet build --configuration Debug`
- [ ] Expected: No CS1061 errors related to "PackPath" not being found
- [ ] Verify compilation succeeds with 0 warnings

**AC-Coverage**: AC-001 through AC-010 (all file existence/content ACs now verified)

---

## PHASE 5: Comprehensive Test Execution and Verification

### 5.1 - Run All PromptPacks Tests [🔄 → ✅]

**Status**: PENDING - All previous phases must complete first

**Commands to Run** (in order):

1. **Clean build** (remove any cached artifacts):
   ```bash
   dotnet clean
   dotnet build --configuration Debug
   # Verify output: "Build succeeded. - 0 Error(s), 0 Warning(s)"
   ```

2. **Run all PromptPacks unit tests**:
   ```bash
   dotnet test tests/Acode.*.Tests/ --filter "FullyQualifiedName~PromptPacks" --verbosity normal --configuration Debug
   # Expected: All tests passing (162+)
   ```

3. **Run integration tests separately**:
   ```bash
   dotnet test tests/Acode.Integration.Tests/PromptPacks/ --verbosity normal --configuration Debug
   # Expected: All 5 integration tests passing (after Phase 1 fixes)
   ```

4. **Run E2E tests**:
   ```bash
   dotnet test tests/Acode.E2E.Tests/PromptPacks/ --verbosity normal --configuration Debug
   # Expected: All 8 E2E tests passing (after Phase 2)
   ```

5. **Run performance benchmarks** (Release configuration):
   ```bash
   dotnet test tests/Acode.Performance.Tests/PromptPacks/ --verbosity normal --configuration Release
   # Expected: Benchmarks showing:
   #   - First load: 100-150ms ✓
   #   - Cached load: < 5ms ✓
   ```

6. **Final verification** (all PromptPacks tests):
   ```bash
   dotnet test --filter "FullyQualifiedName~PromptPacks" --verbosity normal
   # Expected output:
   #   Test Run Successful.
   #   Total tests: 190+
   #   Passed: 190+
   #   Failed: 0
   #   Skipped: 0
   ```

**Success Criteria**:
- [ ] Build succeeds with 0 errors, 0 warnings
- [ ] All unit tests pass (162+)
- [ ] All integration tests pass (5)
- [ ] All E2E tests pass (8)
- [ ] All performance benchmarks complete (4)
- [ ] Total: 179+ tests passing with 0 failures

**AC-Coverage**: AC-001 through AC-050 (all 50+ acceptance criteria verified by test execution)

---

## PHASE 6: Final Audit

### 6.1 - Semantic Completeness Verification [🔄 → ✅]

**Checklist**:
- [ ] All 19 prompt files exist with correct content
- [ ] All 50+ acceptance criteria have corresponding passing tests
- [ ] Build succeeds: `dotnet build` shows 0 errors, 0 warnings
- [ ] All 179+ tests pass: `dotnet test --filter "FullyQualifiedName~PromptPacks"` shows 0 failures
- [ ] Property renamed: `pack.Directory` (not `pack.PackPath`)
- [ ] Integration tests fixed: StarterPackLoadingTests.cs has 0 compilation errors
- [ ] E2E tests created: StarterPackE2ETests.cs has 8 passing tests
- [ ] Performance benchmarks created: PackLoadingBenchmarks.cs has 4 benchmarks passing
- [ ] Documentation updated: All references to PackPath changed to Directory
- [ ] No NotImplementedException or TODO comments in production code

**Success = all 10 checkboxes checked ✅**

---

## Summary of Work Required

| Phase | Task | Est. Time | Blocker |
|-------|------|-----------|---------|
| 1 | Fix 16 compile errors in StarterPackLoadingTests.cs | 1-2h | ✅ YES - blocks build |
| 2 | Create 8 E2E tests in new file | 2-3h | ❌ No |
| 3 | Create 4 benchmarks in new file | 1h | ❌ No |
| 4 | Rename PackPath → Directory property | 30min | ❌ No |
| 5 | Run all tests and verify 179+ passing | 30min | ❌ No |
| 6 | Final audit and semantic completeness check | 30min | ❌ No |
| **TOTAL** | | **6-7 hours** | |

---

## Dependencies Between Phases

```
Phase 1 (Fix compile errors)
    ↓
Phase 4 (Rename property)
    ↓
Phase 5 (Run all tests - prerequisites met)
    ↓
Phase 6 (Final audit - all tests passing)

Phase 2 (Create E2E tests) - can run in parallel with 1, 4, 5
Phase 3 (Create benchmarks) - can run in parallel with 1, 4, 5
```

**Recommended execution order**:
1. Start with Phase 1 (blocks everything)
2. Parallel: Phase 4 (rename property)
3. Parallel: Phase 2 (E2E tests) and Phase 3 (benchmarks)
4. Phase 5 (run tests - once 1, 2, 3, 4 complete)
5. Phase 6 (audit - final verification)

---

## References

- **Task Spec**: docs/tasks/refined-tasks/Epic 01/task-008c-starter-packs-dotnet-react-strict-minimal-diff.md
- **Previous Gap Analysis**: docs/implementation-plans/task-008c-gap-analysis.md
- **Current Status**: 57% semantic completeness (19/19 files + 162 unit tests + 5 blockers)
- **Blocking Issues**: 16 compile errors, 2 missing test files, 1 naming issue
- **Acceptance Criteria**: 50+ total; ~35 verified by existing tests; 15 need E2E/perf verification

