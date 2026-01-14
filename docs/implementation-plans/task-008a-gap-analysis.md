# Task-008a Gap Analysis: Prompt Pack File Layout + Hashing/Versioning

**Task**: task-008a-prompt-pack-file-layout-hashing-versioning.md (Epic 01)
**Status**: Gap Analysis Phase  
**Date**: 2026-01-13
**Specification**: ~4705 lines, comprehensive file layout, hashing, versioning, manifest schema

---

## Executive Summary

Task-008a defines the foundational file layout, manifest schema, content hashing, and semantic versioning for prompt packs. The codebase has **substantial implementation** (~85-90% complete) with most domain models and infrastructure services present. However, there are **critical gaps**:

1. **Missing Test Files**: Spec requires PackDiscoveryTests.cs and PackCreationE2ETests.cs (brand new, not found)
2. **Test Files Exist But Likely Incomplete**: PackManifestTests, ContentHasherTests, ComponentPathTests, SemVerTests tests exist but may not match all spec test cases
3. **Extra Infrastructure**: Code has PromptPackLoader, PromptComposer, TemplateEngine, PackValidator, PackCache, ComponentMerger which are likely task-008b/008c code mixed in
4. **Missing Built-in Resources**: Spec shows embedded acode-standard pack in Resources folder - implementation status unclear
5. **Application Layer**: Spec mentions Application.ToolSchemas.Retry naming, but code uses Application/Infrastructure directly - not under ToolSchemas directories
6. **Test Naming Mismatch**: Spec shows ComponentPathTests named "ComponentPathTests", code has same - good alignment

**Scope**: Complete test coverage verification, ensure all 8+ test files match spec exactly, verify built-in pack resources, clean up extra code

**Recommendation**: Most implementation appears complete. Audit needed to verify all test methods match spec line-for-line. Remove task-008b code if present. Verify built-in pack resources embedded correctly.

---

## Current State Analysis

### Domain Layer - 13 Files

**Existing & Spec-Required**:
- ✅ ComponentType.cs - enum with System, Role, Language, Framework, Custom
- ✅ ContentHash.cs - SHA-256 value object (verified implementation available)
- ✅ PackComponent.cs - component definition with Path, Type, Metadata
- ✅ PackManifest.cs - manifest domain model with validation
- ✅ PackSource.cs - enum (BuiltIn, User)
- ✅ PackVersion.cs - SemVer 2.0 value object
- ✅ Exception Classes (5 files) - ManifestParseException, PackValidationException, PathTraversalException, PackNotFoundException, PackLoadException

**Extra (Task-008b/008c Code?)**:
- ⚠️ PromptPack.cs - appears to be higher-level composition (task-008b)
- ⚠️ CompositionContext.cs - likely task-008b (templating context)

### Infrastructure Layer - 15 Files

**Spec-Required (Task-008a)**:
- ✅ ContentHasher.cs - hash computation and verification
- ✅ HashVerifier.cs - integrity verification
- ✅ ManifestParser.cs - YAML parsing with YamlDotNet
- ✅ PathNormalizer.cs - cross-platform path handling
- ✅ PackDiscovery.cs - discovers built-in and user packs
- ✅ PackDiscoveryOptions.cs - configuration for discovery
- ✅ PromptPacksServiceExtensions.cs - DI registration (AddPromptPacks method)

**Extra (Task-008b/008c Code)**:
- ⚠️ PromptPackLoader.cs - loader service (likely 008b)
- ⚠️ PromptComposer.cs - composition service (likely 008b)
- ⚠️ TemplateEngine.cs - template rendering (likely 008b)
- ⚠️ PackValidator.cs - advanced validation (likely 008b)
- ⚠️ PackCache.cs - caching layer (likely 008b)
- ⚠️ ComponentMerger.cs - merging components (likely 008b)
- ⚠️ EmbeddedPackProvider.cs - embedded resource loading
- ⚠️ PackConfiguration.cs - configuration model
- ⚠️ HashVerificationResult.cs - verification result type

### Test Files - Many Exist

**Spec-Required Task-008a Tests**:
- ✅ PackManifestTests.cs - tests for manifest parsing and validation
- ✅ ContentHasherTests.cs - tests for hash computation
- ✅ ComponentPathTests.cs - tests for path normalization (found as ComponentPathTests)
- ✅ SemVerTests.cs - tests for version parsing
- ❌ PackDiscoveryTests.cs - MISSING (spec defines integration test class for pack discovery)
- ❌ HashVerificationTests.cs - Marked as Integration, but spec shows it should test hash verification
- ❌ PackCreationE2ETests.cs - MISSING (E2E tests for pack creation)
- ❌ PackLayoutBenchmarks.cs - Performance benchmarks (missing)

**Extra Test Files (Task-008b/008c)**:
- ⚠️ ComponentMergerTests.cs
- ⚠️ EmbeddedPackProviderTests.cs
- ⚠️ PackCacheTests.cs
- ⚠️ PackConfigurationTests.cs
- ⚠️ PackValidatorTests.cs
- ⚠️ PromptComposerTests.cs
- ⚠️ PromptPackLoaderTests.cs
- ⚠️ PromptPackRegistryTests.cs
- ⚠️ StarterPackTests.cs
- ⚠️ TemplateEngineTests.cs
- ⚠️ CompositionContextTests.cs
- ⚠️ PromptPackIntegrationTests.cs
- ⚠️ PromptPackPerformanceTests.cs

### Built-in Pack Resources

**Spec Shows** (Implementation Prompt, lines 3610-3615):
```
src/Acode.Infrastructure/Resources/PromptPacks/
└── acode-standard/
    ├── manifest.yml
    ├── system.md
    └── roles/
```

**Current State**: Directory exists but contents not verified

---

## Critical Findings

### 1. Possible Code Organization Issue

**Spec Intends**: Task-008a focuses on file layout, hashing, versioning, discovery
- Domain models (PackManifest, ContentHash, PackVersion, etc.)
- Infrastructure parsing/discovery (ManifestParser, ContentHasher, PathNormalizer, PackDiscovery)
- Tests for above components

**Current Codebase**: Contains substantial extra code (PromptPackLoader, PromptComposer, TemplateEngine, etc.) that belongs to tasks-008b or 008c

**Recommendation**: Audit and potentially move task-008b code to separate classes/namespaces

### 2. Test File Verification Needed

Spec defines exact test methods (e.g., lines 2092-2120 for PackManifestTests):
- Should_Parse_Valid_Manifest
- Should_Reject_Invalid_Format_Version  
- Should_Validate_Pack_Id_Format
- Should_Parse_SemVer_Version
- Should_Parse_Components_With_Metadata
- Should_Require_Created_At_Field

Current PackManifestTests.cs needs audit to verify ALL spec test methods are present and complete

### 3. Missing or Incomplete Tests

Per spec Testing Requirements (lines 2075-3011):
- [ ] PackDiscoveryTests.cs - Should_Find_BuiltIn_Packs, Should_Find_User_Packs, Should_Prioritize_User_Packs (3 tests, lines 2662-2696)
- [ ] HashVerificationTests.cs - Should_Verify_Valid_Hash, Should_Detect_Modified_Content, Should_Regenerate_Hash (3 tests, lines 2744-2791)
- [ ] PackCreationE2ETests.cs - Should_Create_Pack_Structure, Should_Generate_Valid_Manifest, Should_Compute_Initial_Hash (3 tests, lines 2852-2926)
- [ ] PackLayoutBenchmarks.cs - Benchmark_Hash_Small_Pack, Benchmark_Hash_Large_Pack, Benchmark_Parse_Manifest (3 benchmarks, lines 2993-3009)

### 4. Namespace Structure

**Current**: Acode.Domain.PromptPacks, Acode.Infrastructure.PromptPacks
**Spec**: Same (no ToolSchemas mentioned, so current is correct)

---

## Gap Summary

### Complete Components (Can be verified, not changed)
- ✅ All 6 domain model classes (ComponentType, ContentHash, PackComponent, PackManifest, PackSource, PackVersion)
- ✅ All 5 exception classes
- ✅ All 7 infrastructure services (ContentHasher, HashVerifier, ManifestParser, PathNormalizer, PackDiscovery, PackDiscoveryOptions, ServiceExtensions)
- ✅ 4 test classes (PackManifestTests, ContentHasherTests, ComponentPathTests, SemVerTests)

### Verification Needed (Audit tests match spec exactly)
- 🔄 PackManifestTests.cs - Verify all 9 test methods from spec exist and are complete
- 🔄 ContentHasherTests.cs - Verify all 9 test methods from spec exist
- 🔄 ComponentPathTests.cs - Verify all 6 test methods from spec exist
- 🔄 SemVerTests.cs - Verify all 5 test methods from spec exist

### Missing or Incomplete (NEW tests to create)
- ❌ PackDiscoveryTests.cs - Integration tests for pack discovery (3 tests)
- ❌ HashVerificationTests.cs - EXPAND existing if present (3 tests minimum)
- ❌ PackCreationE2ETests.cs - E2E tests for pack creation (3 tests)
- ❌ PackLayoutBenchmarks.cs - Performance benchmarks (3+ benchmarks)

### Extra Code to Review
- ⚠️ PromptPackLoader.cs - belongs to task-008b
- ⚠️ PromptComposer.cs - belongs to task-008b
- ⚠️ TemplateEngine.cs - belongs to task-008b
- ⚠️ 10+ other task-008b files mixed into 008a codebase

### Built-in Resources to Verify
- 🔄 src/Acode.Infrastructure/Resources/PromptPacks/acode-standard/ structure
- 🔄 Embedded resource manifest.yml properly formatted
- 🔄 System.md and roles/ content present

---

## Test Requirements Breakdown

**From Spec Testing Requirements (lines 2075-3011)**:

### Unit Tests: 4 classes, ~30 tests total

1. PackManifestTests (9 tests, lines 2089-2288)
2. ContentHasherTests (9 tests, lines 2302-2442)
3. ComponentPathTests (6 tests, lines 2455-2531)
4. SemVerTests (5 tests, lines 2544-2628)

### Integration Tests: 2 classes, ~6 tests total

5. PackDiscoveryTests (3 tests, lines 2644-2721)
6. HashVerificationTests (3 tests, lines 2733-2827)

### E2E Tests: 1 class, 3 tests

7. PackCreationE2ETests (3 tests, lines 2842-2935)

### Performance Tests: 1 class, 3+ benchmarks

8. PackLayoutBenchmarks (3+ benchmarks, lines 2948-3010)

**Total: 8+ test files, 50+ tests/benchmarks**

---

## Performance Requirements (Spec lines 3013-3020)

| Metric | Requirement | Status |
|--------|-------------|--------|
| PERF-001 | Hash 1MB content < 50ms | Testable in PackLayoutBenchmarks |
| PERF-002 | Parse manifest < 10ms | Testable in PackLayoutBenchmarks |
| PERF-003 | Scan directory < 100ms | Testable in PackLayoutBenchmarks |
| PERF-004 | Memory < 5MB for 1MB pack | Needs verification |

---

## Remediation Approach

### Phase 1: Code Organization Audit (CRITICAL)
- [ ] Review PromptPackLoader, PromptComposer, TemplateEngine, etc.
- [ ] Determine if these belong to task-008b or 008c
- [ ] If so, document move/refactoring needed

### Phase 2: Test File Verification (HIGH)
- [ ] Audit PackManifestTests.cs against spec (lines 2089-2288)
- [ ] Audit ContentHasherTests.cs against spec (lines 2302-2442)
- [ ] Audit ComponentPathTests.cs against spec (lines 2455-2531)
- [ ] Audit SemVerTests.cs against spec (lines 2544-2628)

### Phase 3: Create Missing Test Files (HIGH)
- [ ] Create PackDiscoveryTests.cs (3 tests from spec lines 2644-2721)
- [ ] Create/Expand HashVerificationTests.cs (3 tests from spec lines 2733-2827)
- [ ] Create PackCreationE2ETests.cs (3 tests from spec lines 2842-2935)
- [ ] Create PackLayoutBenchmarks.cs (3+ benchmarks from spec lines 2948-3010)

### Phase 4: Built-in Resources (MEDIUM)
- [ ] Verify acode-standard pack embedded in resources
- [ ] Verify manifest.yml, system.md, roles/ structure
- [ ] Verify packaging as embedded resource (not loose files)

### Phase 5: Final Audit
- [ ] Run `dotnet test --filter "FullyQualifiedName~PromptPacks"` (should pass)
- [ ] Verify build succeeds
- [ ] Verify performance benchmarks meet requirements

---

## References

- **Spec**: task-008a-prompt-pack-file-layout-hashing-versioning.md
- **Testing Requirements**: Lines 2075-3011
- **Implementation Prompt**: Lines 3575-4705
- **Performance Requirements**: Lines 3013-3020
- **Error Codes**: Lines 4652-4665
