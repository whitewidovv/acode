# Task 008 Gap Analysis Plan

## Executive Summary

Task 008 "Prompt Pack System" and all its subtasks (008a, 008b, 008c) have **NOT BEEN IMPLEMENTED**. The entire Prompt Pack System is missing from the codebase. This gap analysis identifies what needs to be built and provides a structured implementation plan.

## Current State Assessment

**Search Results**: No PromptPack-related code exists in `/src` or `/tests` directories.

| Component | Expected | Actual | Gap |
|-----------|----------|--------|-----|
| Domain Models | 7+ classes | 0 | 100% missing |
| Infrastructure Services | 6+ classes | 0 | 100% missing |
| Tests | 100+ test methods | 0 | 100% missing |
| Built-in Packs | 3 packs | 0 | 100% missing |

## Task Breakdown

### Task 008a: Prompt Pack File Layout, Hashing, and Versioning (3,410 lines)
- **Status**: ❌ NOT STARTED
- **Priority**: First - foundational domain models

**Domain Models Required:**
- `PackManifest.cs` - Pack metadata and components
- `PackComponent.cs` - Component definition
- `ComponentType.cs` - Component type enum
- `ContentHash.cs` - SHA-256 hash value object
- `PackVersion.cs` - SemVer 2.0 value object
- `PackSource.cs` - Pack source enum

**Exceptions Required:**
- `ManifestParseException.cs`
- `PathTraversalException.cs`
- `PackValidationException.cs`

**Infrastructure Services Required:**
- `ContentHasher.cs` - Hash computation
- `HashVerifier.cs` - Hash verification
- `ManifestParser.cs` - YAML parsing
- `PathNormalizer.cs` - Cross-platform paths
- `PackDiscovery.cs` - Pack discovery service
- `PackDiscoveryOptions.cs` - Discovery configuration

**Tests Expected:**
- `PackManifestTests.cs` (~12 tests)
- `ContentHasherTests.cs` (~8 tests)
- `ComponentPathTests.cs` (~8 tests)
- `SemVerTests.cs` (~10 tests)
- `PackDiscoveryTests.cs` (integration, ~3 tests)
- `HashVerificationTests.cs` (integration, ~3 tests)

### Task 008b: Loader, Validator, Selection via Config (2,360 lines)
- **Status**: ❌ NOT STARTED
- **Priority**: Second - depends on 008a

**Expected Components:**
- `IPromptPackLoader` interface
- `PromptPackLoader` implementation
- `IPackValidator` interface
- `PackValidator` implementation
- `IPackRegistry` interface
- `PackRegistry` implementation
- Config integration for pack selection

### Task 008c: Starter Packs (.NET, React, Strict Minimal Diff) (3,299 lines)
- **Status**: ❌ NOT STARTED
- **Priority**: Third - depends on 008a, 008b

**Expected Starter Packs:**
- `acode-standard` - General coding pack
- `acode-dotnet` - .NET specialized pack
- `acode-react` - React/TypeScript pack

**Expected Files Per Pack:**
- `manifest.yml`
- `system.md`
- `roles/coder.md`, `roles/planner.md`, `roles/reviewer.md`
- `languages/*.md` (relevant language files)
- `frameworks/*.md` (relevant framework files)

---

## Implementation Execution Plan

### Phase 1: Task 008a Implementation (Estimated: 2-3 hours)

**Step 1.1**: Create Domain Models
1. Create `src/Acode.Domain/PromptPacks/` directory
2. Implement `PackManifest.cs` with all properties and validation
3. Implement `PackComponent.cs`
4. Implement `ComponentType.cs` enum
5. Implement `ContentHash.cs` value object with SHA-256
6. Implement `PackVersion.cs` value object with SemVer parsing
7. Implement `PackSource.cs` enum
8. Create exception classes in `Exceptions/` subdirectory

**Step 1.2**: Create Infrastructure Services
1. Create `src/Acode.Infrastructure/PromptPacks/` directory
2. Implement `PathNormalizer.cs`
3. Implement `ContentHasher.cs`
4. Implement `ManifestParser.cs` with YamlDotNet
5. Implement `HashVerifier.cs`
6. Implement `PackDiscovery.cs` and `PackDiscoveryOptions.cs`
7. Create DI registration extension method

**Step 1.3**: Create Unit Tests
1. Create `tests/Acode.Domain.Tests/PromptPacks/` directory
2. Write `PackManifestTests.cs`
3. Write `SemVerTests.cs`
4. Write `ContentHashTests.cs`

**Step 1.4**: Create Infrastructure Tests
1. Create `tests/Acode.Infrastructure.Tests/PromptPacks/` directory
2. Write `ContentHasherTests.cs`
3. Write `ComponentPathTests.cs`
4. Write `ManifestParserTests.cs`

**Step 1.5**: Create Integration Tests
1. Create `tests/Acode.Integration.Tests/PromptPacks/` directory
2. Write `PackDiscoveryTests.cs`
3. Write `HashVerificationTests.cs`

**Step 1.6**: Verify All Tests Pass
- Run `dotnet test --filter "FullyQualifiedName~PromptPacks"`
- Ensure 100% pass rate

### Phase 2: Task 008b Implementation

**Step 2.1**: Create Application Interfaces
1. Create `src/Acode.Application/PromptPacks/` directory
2. Implement `IPromptPackLoader.cs`
3. Implement `IPackValidator.cs`
4. Implement `IPackRegistry.cs`

**Step 2.2**: Create Infrastructure Implementations
1. Implement `PromptPackLoader.cs`
2. Implement `PackValidator.cs`
3. Implement `PackRegistry.cs`
4. Add configuration integration

**Step 2.3**: Create Tests
1. Write `PromptPackLoaderTests.cs`
2. Write `PackValidatorTests.cs`
3. Write `PackRegistryTests.cs`

### Phase 3: Task 008c Implementation

**Step 3.1**: Create Starter Pack Directory Structure
1. Create `src/Acode.Infrastructure/Resources/PromptPacks/`
2. Create `acode-standard/`, `acode-dotnet/`, `acode-react/` subdirectories

**Step 3.2**: Create Standard Pack
1. Write `manifest.yml`
2. Write `system.md`
3. Write role prompts
4. Compute content hash

**Step 3.3**: Create .NET Pack
1. Write `manifest.yml`
2. Write `languages/csharp.md`
3. Write `frameworks/aspnetcore.md`
4. Compute content hash

**Step 3.4**: Create React Pack
1. Write `manifest.yml`
2. Write `languages/typescript.md`
3. Write `frameworks/react.md`
4. Compute content hash

**Step 3.5**: Update .csproj for Embedded Resources
1. Add `<EmbeddedResource Include="Resources\PromptPacks\**\*.md" />`
2. Add `<EmbeddedResource Include="Resources\PromptPacks\**\*.yml" />`

**Step 3.6**: Create Tests
1. Write starter pack integration tests
2. Verify all packs load correctly

---

## Acceptance Criteria Verification

After implementation, verify ALL acceptance criteria from:
- Task 008a: 63 acceptance criteria items (AC-001 through AC-063)
- Task 008b: TBD after reading specification
- Task 008c: TBD after reading specification

---

## Progress Tracking

### Task 008a Progress
- [ ] Domain Models created
- [ ] Exceptions created
- [ ] Infrastructure services created
- [ ] Unit tests created and passing
- [ ] Integration tests created and passing
- [ ] All 63 acceptance criteria verified

### Task 008b Progress
- [ ] Application interfaces created
- [ ] Infrastructure implementations created
- [ ] Tests created and passing
- [ ] Acceptance criteria verified

### Task 008c Progress
- [ ] acode-standard pack created
- [ ] acode-dotnet pack created
- [ ] acode-react pack created
- [ ] Embedded resources configured
- [ ] Tests created and passing
- [ ] Acceptance criteria verified

---

## Final Verification

Before creating PR:
- [ ] All tests pass: `dotnet test`
- [ ] Build succeeds with no warnings: `dotnet build`
- [ ] All acceptance criteria verified
- [ ] Documentation complete
- [ ] Commits follow convention
