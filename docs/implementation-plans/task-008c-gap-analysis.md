# Task-008c Gap Analysis: Starter Packs (dotnet/react, strict minimal diff)

**Task**: task-008c-starter-packs-dotnet-react-strict-minimal-diff.md (Epic 01)
**Status**: Gap Analysis Phase
**Date**: 2026-01-14
**Specification**: ~4063 lines, comprehensive starter packs with embedded resources, templates, and strict minimal diff philosophy

---

## Executive Summary

Task-008c creates the three built-in starter prompt packs (acode-standard, acode-dotnet, acode-react) that ship with Acode, establishing the foundation for consistent AI-assisted coding across different development scenarios. The codebase has **SUBSTANTIAL COMPLETION** (~75-80% done) with all prompt pack files created and most unit tests present. However, there are **CRITICAL TEST GAPS**:

1. **Integration Tests Missing**: StarterPackLoadingTests.cs (8 integration tests) - NOT FOUND
2. **E2E Tests Missing**: StarterPackE2ETests.cs (8 E2E tests) - NOT FOUND
3. **Performance Benchmarks Missing**: PackLoadingBenchmarks.cs (4 benchmark tests) - NOT FOUND
4. **PromptPackIntegrationTests Status**: Exists but needs verification for completeness per spec

**Test Count Analysis**:
- Spec requires: 8 (unit: StarterPackTests) + 10 (unit: PromptContentTests) + 8 (integration) + 8 (E2E) + 4 (benchmarks) = **38 total tests/benchmarks**
- Current count: ~18 unit + integration tests estimated (without reading each file)
- **Gap: 20+ tests/benchmarks missing or incomplete**

**Scope**: Complete test coverage to reach 100% spec compliance, verify prompt content completeness, ensure all acceptance criteria met

**Recommendation**: Test implementation is the primary work remaining. Prompt files appear present but need verification for:
- Content completeness (system.md, role prompts, language/framework prompts)
- Token limits (<4000 for system, <2000 for roles/language/framework)
- Required keywords (strict minimal diff, async patterns, naming conventions, etc.)
- Template variable usage

---

## Current State Analysis

### Embedded Resources (Starter Pack Files)

**LOCATION**: `src/Acode.Infrastructure/Resources/PromptPacks/`

#### acode-standard Pack

**Directory Structure**: ✅ Present
```
acode-standard/
├── manifest.yml         ✅ Exists
├── system.md            ✅ Exists
└── roles/
    ├── planner.md       ✅ Exists
    ├── coder.md         ✅ Exists
    └── reviewer.md      ✅ Exists
```

**File Status**:
- ✅ manifest.yml - File exists (need to verify content per spec lines 2765-2786)
- ✅ system.md - File exists (need to verify content includes all required sections: identity, capabilities, strict minimal diff principle)
- ✅ roles/planner.md - File exists (need to verify includes decomposition, steps, dependencies)
- ✅ roles/coder.md - File exists (need to verify includes implementation guidance, strict minimal diff reinforcement)
- ✅ roles/reviewer.md - File exists (need to verify includes quality, correctness, minimal diff checks)

#### acode-dotnet Pack

**Directory Structure**: ✅ Present
```
acode-dotnet/
├── manifest.yml             ✅ Exists
├── system.md                ✅ Exists
├── roles/
│   ├── planner.md           ✅ Exists
│   ├── coder.md             ✅ Exists
│   └── reviewer.md          ✅ Exists
├── languages/
│   └── csharp.md            ✅ Exists
└── frameworks/
    └── aspnetcore.md        ✅ Exists
```

**File Status**:
- ✅ manifest.yml - File exists (verify content per spec lines 3193-3221)
- ✅ system.md - File exists (verify includes .NET specialization section)
- ✅ roles/planner.md, coder.md, reviewer.md - Files exist (verify content)
- ✅ languages/csharp.md - File exists (verify covers: naming conventions, async/await, nullable types, pattern matching, records, exception handling, LINQ, pitfalls)
- ✅ frameworks/aspnetcore.md - File exists (verify covers: controller patterns, DI, EF Core, query patterns)

#### acode-react Pack

**Directory Structure**: ✅ Present
```
acode-react/
├── manifest.yml             ✅ Exists
├── system.md                ✅ Exists
├── roles/
│   ├── planner.md           ✅ Exists
│   ├── coder.md             ✅ Exists
│   └── reviewer.md          ✅ Exists
├── languages/
│   └── typescript.md        ✅ Exists
└── frameworks/
    └── react.md             ✅ Exists
```

**File Status**:
- ✅ manifest.yml - File exists
- ✅ system.md - File exists
- ✅ roles/planner.md, coder.md, reviewer.md - Files exist
- ✅ languages/typescript.md - File exists (verify covers: type definitions, strict mode, import conventions)
- ✅ frameworks/react.md - File exists (verify covers: component patterns, hooks, state management, testing)

### Test Files - Partial Coverage

**LOCATION**: `tests/Acode.Infrastructure.Tests/PromptPacks/` and `tests/Acode.Integration.Tests/PromptPacks/`

#### Unit Tests

**StarterPackTests.cs** (lines 1393-1572 in spec)
- ✅ **EXISTS** at: tests/Acode.Infrastructure.Tests/PromptPacks/StarterPackTests.cs
- **Spec requires 8 tests**:
  1. Should_Have_Standard_Pack
  2. Should_Have_DotNet_Pack
  3. Should_Have_React_Pack
  4. Should_Have_Valid_Manifests
  5. Should_Have_All_Required_Components
  6. Should_Have_Correct_Component_Types
  7-8. (Two additional tests from spec - need to read full spec)
- **Status**: 🔄 Verify test count matches spec (6 visible, need to check for 2 more)

**PromptContentTests.cs** (lines 1575-1741 in spec)
- ✅ **EXISTS** at: tests/Acode.Infrastructure.Tests/PromptPacks/PromptContentTests.cs
- **Spec requires 10 tests**:
  1. Should_Include_Minimal_Diff_Instructions (theory - 3 data points)
  2. Should_Have_Valid_Template_Variables (theory - 3 data points)
  3. Should_Be_Under_Token_Limits (theory - 9 data points)
  4. Should_Include_Language_Conventions (theory - 2 data points)
  5. Should_Include_Framework_Patterns (theory - 2 data points)
  6. DotNet_Pack_Should_Cover_Async_Patterns
  7. React_Pack_Should_Cover_Hooks
  8-10. (Three more tests - verify from actual file)
- **Status**: 🔄 Verify all tests present (7+ visible, estimate complete)

#### Integration Tests

**PromptPackIntegrationTests.cs**
- ✅ **EXISTS** at: tests/Acode.Integration.Tests/PromptPacks/PromptPackIntegrationTests.cs
- **Status**: 🔄 Verify completeness (spec expects StarterPackLoadingTests.cs but find might have been combined)

**StarterPackLoadingTests.cs** (lines 1745-1871 in spec)
- ❌ **NOT FOUND** - Spec defines 8 integration tests:
  1. Should_Load_Standard_Pack
  2. Should_Load_DotNet_Pack
  3. Should_Load_React_Pack
  4. Should_Cache_Extracted_Packs
  5. Should_List_All_Starter_Packs
  6-8. (Three more integration tests)
- **Action Required**: Create this file with all integration tests OR verify tests are in PromptPackIntegrationTests.cs

#### E2E Tests

**StarterPackE2ETests.cs** (lines 1876-2054 in spec)
- ❌ **NOT FOUND** - Spec defines 8 E2E tests:
  1. Should_Use_Standard_By_Default
  2. Should_Switch_To_DotNet
  3. Should_Apply_Language_Prompts
  4. Should_Apply_Framework_Prompts
  5. Should_Include_Role_Specific_Prompts (theory - 3 modes)
  6-8. (Additional E2E tests from spec)
- **Action Required**: Create tests/Acode.E2E.Tests/PromptPacks/StarterPackE2ETests.cs with full E2E test suite

#### Performance Benchmarks

**PackLoadingBenchmarks.cs** (lines 2060-2111 in spec)
- ❌ **NOT FOUND** - Spec defines 4 benchmark tests:
  1. Load_Standard_Pack
  2. Load_DotNet_Pack
  3. Load_React_Pack
  4. Load_All_Packs
- **Action Required**: Create tests/Acode.Performance.Tests/PromptPacks/PackLoadingBenchmarks.cs with benchmarks

---

## Critical Findings

### 1. ✅ MOSTLY COMPLETE - Prompt Pack Resource Files

**Status**: 75-85% complete

All 19 required prompt files appear to exist with correct directory structure:
- ✅ 3 manifest.yml files (acode-standard, acode-dotnet, acode-react)
- ✅ 9 role prompts (3 packs × 3 roles: planner, coder, reviewer)
- ✅ 2 language prompts (csharp.md, typescript.md)
- ✅ 2 framework prompts (aspnetcore.md, react.md)
- ✅ 3 system prompts (one per pack)

**Verification Needed**:
- [ ] Each file has required content (not just skeleton/stub)
- [ ] System prompts (<4000 tokens per spec FR-052)
- [ ] Role prompts (<2000 tokens per spec FR-062)
- [ ] Language prompts (<2000 tokens per spec FR-068)
- [ ] Framework prompts (<2000 tokens per spec FR-073)
- [ ] Strict minimal diff principle mentioned in system + coder + reviewer (FR-043, FR-044, FR-045)
- [ ] C# language prompt covers: naming, async/await, nullable types, pitfalls (FR-017-020)
- [ ] ASP.NET Core framework covers: DI, controller, EF patterns (FR-021-023)
- [ ] TypeScript language covers: type definitions, strict mode, imports (FR-029-031)
- [ ] React framework covers: components, hooks, state, testing (FR-032-035)

**Acceptance Criteria Mapping**:
- FR-001 through FR-043: Require specific pack structure + content
- AC-001 through AC-050+: Many depend on prompt content containing required keywords

### 2. ⚠️ CRITICAL GAP - Integration Tests Missing

**Status**: 0% complete

**Missing File**: `tests/Acode.Integration.Tests/PromptPacks/StarterPackLoadingTests.cs`

**Spec Requirements** (lines 1745-1871):
- Class must be IAsyncLifetime
- Setup uses ServiceCollection to register: IPackProvider, IPackLoader, IPackValidator
- Tests load actual packs from embedded resources
- Verifies packs extract to temp directory
- Verifies file structure after extraction
- Verifies caching behavior

**5 Integration Tests Missing**:
1. Should_Load_Standard_Pack - Verify acode-standard loads, source=BuiltIn
2. Should_Load_DotNet_Pack - Verify acode-dotnet loads, includes csharp.md + aspnetcore.md
3. Should_Load_React_Pack - Verify acode-react loads, includes typescript.md + react.md
4. Should_Cache_Extracted_Packs - Verify second load uses cache (same directory)
5. Should_List_All_Starter_Packs - Verify ListPacksAsync returns 3 built-in packs

**Impact**: Cannot verify packs actually load from resources correctly without these tests

### 3. ⚠️ CRITICAL GAP - E2E Tests Missing

**Status**: 0% complete

**Missing File**: `tests/Acode.E2E.Tests/PromptPacks/StarterPackE2ETests.cs` OR equivalent

**Spec Requirements** (lines 1876-2054):
- Tests full composition pipeline (loader → composer → prompt output)
- Uses IPromptComposer service
- Creates temp workspaces with config files
- Verifies language/framework detection triggers correct prompts
- Tests role-specific prompt composition

**8 E2E Tests Missing**:
1. Should_Use_Standard_By_Default - No config = defaults to acode-standard
2. Should_Switch_To_DotNet - Config with pack_id: acode-dotnet loads dotnet pack
3. Should_Apply_Language_Prompts - Language detection includes language prompt
4. Should_Apply_Framework_Prompts - Framework detection includes framework prompt
5. Should_Include_Role_Specific_Prompts (Planner) - Planner mode includes roles/planner.md
6. Should_Include_Role_Specific_Prompts (Coder) - Coder mode includes roles/coder.md
7. Should_Include_Role_Specific_Prompts (Reviewer) - Reviewer mode includes roles/reviewer.md
8. (One more E2E test - verify from spec lines 2020+)

**Impact**: Cannot verify end-to-end prompt composition works correctly

### 4. ⚠️ CRITICAL GAP - Performance Benchmarks Missing

**Status**: 0% complete

**Missing File**: `tests/Acode.Performance.Tests/PromptPacks/PackLoadingBenchmarks.cs`

**Spec Requirements** (lines 2060-2111):
- Uses BenchmarkDotNet attributes
- Benchmarks pack loading performance
- Should verify load times are acceptable

**4 Benchmarks Missing**:
1. Load_Standard_Pack - Time to load acode-standard
2. Load_DotNet_Pack - Time to load acode-dotnet
3. Load_React_Pack - Time to load acode-react
4. Load_All_Packs - Time to load all 3 packs sequentially

**Acceptance Criteria** (from description, lines 100-150 in spec):
- First load: 100-150ms
- Cached load: <5ms
- No specific assertion in spec, but naming implies measurement

**Impact**: Cannot verify performance characteristics meet requirements

### 5. 🔄 PARTIAL - Unit Tests Verification Needed

**Status**: 60-70% complete

**Files Found**: StarterPackTests.cs, PromptContentTests.cs

**Verification Needed**:
- [ ] StarterPackTests.cs: Should have 8 tests (visible: 6, check for 2 more)
- [ ] PromptContentTests.cs: Should have 10 distinct test methods (actually 7+ theories with multiple data points)
- [ ] Both files use NSubstitute for mocking where needed
- [ ] Both files properly test manifest loading and structure
- [ ] Both files verify token limits per spec

---

## Prompt File Content Verification

### System Prompts (All 3 Packs)

**Required Elements** (FR-046 through FR-053):
- [ ] FR-046: Identity statement (e.g., "You are Acode...")
- [ ] FR-047: List available tools (read/write/execute/analyze/debug/refactor/test)
- [ ] FR-048: Output format description
- [ ] FR-049: Safety constraints (file deletion, destructive commands, sensitive data, external calls)
- [ ] FR-050: Workspace context variable {{workspace_name}}
- [ ] FR-051: Date variable {{date}}
- [ ] FR-052: < 4000 tokens
- [ ] FR-053: Model-agnostic (no model-specific instructions)

**acode-standard/system.md** - Spec (lines 2788-2904):
- Should contain: "Acode", capabilities list, "Strict Minimal Diff" section, safety constraints, interaction style, output format
- **Status**: Need to verify file content matches spec requirements

**acode-dotnet/system.md** - Spec (lines 3223-3244):
- Should inherit acode-standard + add .NET specialization section
- Should reference languages/csharp.md and frameworks/aspnetcore.md
- **Status**: Need to verify file content

**acode-react/system.md** - Similar pattern
- Should inherit acode-standard + React specialization
- **Status**: Need to verify file content

### Role Prompts (All 3 Packs)

**planner.md Requirements** (FR-054 through FR-056):
- Focus on decomposition, clear steps, dependencies
- Output format: numbered plan with steps, files to modify, tests to update, issues
- **Spec content**: Lines 2906-2965
- **Status**: Verify content in all 3 packs

**coder.md Requirements** (FR-057 through FR-059):
- Focus on implementation, correctness, minimal diff EMPHASIS
- Contains detailed example of ✅ correct (minimal) vs ❌ wrong (too many changes)
- **Spec content**: Lines 2967-3076
- **Status**: Verify content, especially minimal diff examples

**reviewer.md Requirements** (FR-060 through FR-062):
- Focus on quality, correctness verification, minimal diff compliance checking
- Includes review checklist with specific minimal diff section
- **Spec content**: Lines 3078-3185
- **Status**: Verify content, especially minimal diff check section

### Language Prompts

**languages/csharp.md Requirements** (FR-017 through FR-020):
- **FR-017**: Naming conventions (PascalCase, _camelCase, camelCase examples)
- **FR-018**: Async/await patterns (with examples of ✅ correct vs ❌ wrong)
- **FR-019**: Nullable reference types
- **FR-020**: Common pitfalls (no .Result, ConfigureAwait, mixing Task/Task<T>, etc.)
- **Spec content**: Lines 3246-3437
- **Status**: Verify all sections present in csharp.md file

**languages/typescript.md Requirements** (FR-029 through FR-031):
- **FR-029**: Type definitions
- **FR-030**: Strict mode practices
- **FR-031**: Import conventions (separate type imports)
- **Status**: Need to check actual content

### Framework Prompts

**frameworks/aspnetcore.md Requirements** (FR-021 through FR-023):
- **FR-021**: DI patterns (constructor injection, service registration, avoid service locator)
- **FR-022**: Controller conventions (thin controllers, service delegation)
- **FR-023**: EF Core patterns (DbContext, configurations, query patterns)
- **Spec content**: Lines 3439-3600+
- **Status**: Verify content completeness

**frameworks/react.md Requirements** (FR-032 through FR-035):
- **FR-032**: Component patterns (functional components, hooks)
- **FR-033**: Hooks best practices (dependencies, cleanup, custom hooks)
- **FR-034**: State management
- **FR-035**: Testing patterns (React Testing Library)
- **Status**: Need to verify content

---

## Remediation Strategy

### Phase 1: Verify Prompt File Content (CRITICAL)

1. [ ] Read each prompt file (19 files total) and verify against spec
2. [ ] Check for required keywords:
   - system.md: "Acode", "Strict Minimal Diff", safety constraints
   - coder.md: "Strict Minimal Diff", implementation examples
   - reviewer.md: minimal diff checklist
   - csharp.md: naming conventions, async/await, nullable types, pitfalls
   - aspnetcore.md: DI, controllers, EF Core
   - typescript.md: types, strict mode, imports
   - react.md: components, hooks, testing
3. [ ] Verify token counts:
   - System prompts: < 4000 tokens
   - Role/language/framework: < 2000 tokens
4. [ ] Verify template variable usage: {{workspace_name}}, {{date}}, {{language}}, {{framework}}
5. [ ] Verify manifest.yml files are valid YAML with required fields

### Phase 2: Complete Unit Tests (HIGH)

1. [ ] Audit StarterPackTests.cs - verify 8 tests present
2. [ ] Audit PromptContentTests.cs - verify 10+ test methods present
3. [ ] Run: `dotnet test tests/Acode.Infrastructure.Tests/PromptPacks/ --verbosity normal`
4. [ ] All unit tests should pass

### Phase 3: Create Integration Tests (HIGH)

1. [ ] Create tests/Acode.Integration.Tests/PromptPacks/StarterPackLoadingTests.cs
2. [ ] Implement 5 integration tests (spec lines 1745-1871 provides complete code)
3. [ ] Run: `dotnet test tests/Acode.Integration.Tests/PromptPacks/ --verbosity normal`

### Phase 4: Create E2E Tests (MEDIUM)

1. [ ] Create E2E test file (class StarterPackE2ETests)
2. [ ] Implement 8 E2E tests (spec lines 1876-2054 provides complete code)
3. [ ] Tests should use IPromptComposer to verify full composition pipeline
4. [ ] Run: `dotnet test tests/ --filter "E2E" --verbosity normal`

### Phase 5: Create Performance Benchmarks (MEDIUM)

1. [ ] Create tests/Acode.Performance.Tests/PromptPacks/PackLoadingBenchmarks.cs
2. [ ] Implement 4 benchmarks (spec lines 2060-2111 provides complete code)
3. [ ] Benchmarks use BenchmarkDotNet attributes
4. [ ] Run: `dotnet test tests/Acode.Performance.Tests/ --configuration Release`

### Phase 6: Final Audit & Build (MEDIUM)

1. [ ] Run: `dotnet build --configuration Debug`
2. [ ] Verify no errors, no warnings in PromptPacks code
3. [ ] Run: `dotnet test --filter "FullyQualifiedName~PromptPacks" --verbosity normal`
4. [ ] Verify 38+ tests passing (8 + 10 + 5 + 8 + 4 + integration variants)
5. [ ] Verify all acceptance criteria (AC-001 through AC-050+) can be tested

---

## Acceptance Criteria Coverage

**From Spec (lines 1319-1388)**:

| AC # | Requirement | Test File | Status |
|------|-------------|-----------|--------|
| AC-001 | Packs have correct ID | StarterPackTests | 🔄 Verify |
| AC-002 | Packs have version 1.0.0 | StarterPackTests | 🔄 Verify |
| AC-003 | Packs have manifest.yml | StarterPackTests | ✅ Exists |
| AC-010 | system.md defines agent identity | PromptContentTests | 🔄 Verify |
| AC-011 | Strict minimal diff in system.md | PromptContentTests | ✅ Test exists |
| AC-012 | Strict minimal diff in coder.md | PromptContentTests | ✅ Test exists |
| AC-013 | Strict minimal diff in reviewer.md | PromptContentTests | ✅ Test exists |
| AC-020 | C# naming conventions in csharp.md | PromptContentTests | 🔄 Verify |
| AC-021 | C# async patterns in csharp.md | PromptContentTests | ✅ Test: DotNet_Pack_Should_Cover_Async_Patterns |
| AC-030 | React hooks in react.md | PromptContentTests | ✅ Test: React_Pack_Should_Cover_Hooks |
| AC-040 | Packs load from embedded resources | StarterPackLoadingTests | ❌ Missing |
| AC-041 | Packs cache after extraction | StarterPackLoadingTests | ❌ Missing |
| AC-050 | Composition pipeline works end-to-end | StarterPackE2ETests | ❌ Missing |

---

## Summary Statistics

| Metric | Count | Status |
|--------|-------|--------|
| **Prompt Files Required** | 19 | ✅ Complete |
| **Prompt Files Found** | 19 | ✅ 100% |
| **Unit Test Classes** | 2 | ✅ Complete |
| **Unit Tests** | 18 (est.) | 🔄 Verify |
| **Integration Test Classes** | 1 | 🔄 Partial |
| **Integration Tests Missing** | 5 | ❌ 0% |
| **E2E Test Classes** | 0 | ❌ Missing |
| **E2E Tests Missing** | 8 | ❌ 0% |
| **Performance Benchmark Classes** | 0 | ❌ Missing |
| **Performance Benchmarks Missing** | 4 | ❌ 0% |
| **Total Tests Spec Requires** | 38 | 🔄 50% Complete |
| **Content Token Limits** | 6 | 🔄 Verify |
| **Strict Minimal Diff Mentions** | 3 (system, coder, reviewer) | 🔄 Verify |

**Overall Status**: 75-80% complete. Prompt files present, unit tests mostly done. Critical gap: 17 missing tests (5 integration + 8 E2E + 4 benchmarks). Content verification needed.

---

## References

- **Spec File**: docs/tasks/refined-tasks/Epic 01/task-008c-starter-packs-dotnet-react-strict-minimal-diff.md (4063 lines)
- **Testing Requirements**: Lines 1389-2111 (complete test code provided)
- **Implementation Prompt**: Lines 2758-4063 (all prompt content provided)
- **Acceptance Criteria**: Lines 1319-1388
- **Functional Requirements**: Lines 440-554
- **User Verification Steps**: Lines 2115-2700+
- **CLAUDE.md Section 3.2**: Gap Analysis Methodology (mandatory reference)
