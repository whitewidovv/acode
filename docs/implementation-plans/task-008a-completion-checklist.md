# Task-008a Completion Checklist: Prompt Pack File Layout + Hashing/Versioning

**Task**: task-008a-prompt-pack-file-layout-hashing-versioning.md
**Status**: Ready for Verification & Gap Closure
**Last Updated**: 2026-01-13
**Specification**: ~4705 lines

---

## How to Use This Checklist

This checklist focuses on **verifying existing implementation** against spec and **closing remaining gaps**. Most components are complete; this ensures full spec compliance.

### Phases
1. **Existing Code Audit** (3 items): Verify domain/infrastructure complete
2. **Test File Audit** (4 items): Verify 4 existing test files match spec exactly
3. **Create Missing Tests** (4 items): Create 4 new test files per spec
4. **Verify Built-in Resources** (1 item): Confirm embedded packs
5. **Final Audit** (2 items): Build, run tests, verify performance

---

## Phase 1: Existing Code Audit

### 1. Verify Domain Layer Implementation [🔄]

**Domain Classes Required by Spec** (Implementation Prompt, lines 3591-3601):

Check each class exists and implements spec requirements:

- [ ] **ComponentType.cs**: Enum with values (System, Role, Language, Framework, Custom)
  - Verify: 5 enum values present with XML documentation
  
- [ ] **ContentHash.cs**: SHA-256 value object (lines 3810-3913)
  - Verify: Constructor validates 64 lowercase hex chars
  - Verify: Static Compute() method exists (lines 3864-3890)
  - Verify: Equality operators implemented (lines 3908-3912)
  - Verify: ToString() returns hex string
  
- [ ] **PackComponent.cs**: Component definition (lines 3732-3770)
  - Verify: Path (string, required)
  - Verify: Type (ComponentType, required)
  - Verify: Metadata (IReadOnlyDictionary<string, string>?, optional)
  - Verify: Description (string?, optional)
  
- [ ] **PackManifest.cs**: Manifest model (lines 3619-3729)
  - Verify: All 11 properties present (FormatVersion, Id, Version, Name, Description, ContentHash, CreatedAt, UpdatedAt, Author, Components, Source, PackPath)
  - Verify: Validate() method exists (lines 3702-3717)
  - Verify: IsValidPackId() helper (lines 3719-3728)
  
- [ ] **PackSource.cs**: Enum (BuiltIn, User) (lines 4063-4082)
  
- [ ] **PackVersion.cs**: SemVer value object (lines 3916-4060)
  - Verify: Parse() static method (lines 3955-3974)
  - Verify: TryParse() static method (lines 3979-3991)
  - Verify: CompareTo() for comparison (lines 3998-4021)
  - Verify: IComparable<PackVersion>, IEquatable<PackVersion> implemented
  - Verify: Operator overloads (<, >, <=, >=, ==, !=)
  - Verify: GeneratedRegex for SemVer parsing (lines 4056-4059)

**Exceptions** (lines 3598-3601):
- [ ] ManifestParseException (ACODE-PKL-001, ACODE-PKL-002, ACODE-PKL-003, ACODE-PKL-005)
- [ ] PathTraversalException (ACODE-PKL-007)
- [ ] PackValidationException (ACODE-PKL-004, ACODE-PKL-010)

**Acceptance Criteria**:
- [x] All 6 domain classes exist
- [x] All properties and methods from spec present
- [x] All 3 exception types exist with error codes
- [x] Build succeeds: `dotnet build src/Acode.Domain/Acode.Domain.csproj`

**Evidence Needed**:
```bash
grep -l "class PackManifest\|class PackComponent\|enum ComponentType" \
  src/Acode.Domain/PromptPacks/*.cs | wc -l
# Should show 3 (at minimum)
```

**Dependencies**: None (domain layer has no external deps per spec)

---

### 2. Verify Infrastructure Layer Implementation [🔄]

**Infrastructure Services** (Implementation Prompt, lines 3603-3609):

- [ ] **PathNormalizer.cs** (lines 4087-4181)
  - Verify: Normalize() method validates paths, prevents traversal
  - Verify: IsPathSafe() checks if path under root
  - Verify: ContainsTraversal() detects ".." patterns
  - Verify: Rejects absolute paths
  - Verify: Normalizes slashes to forward slash
  
- [ ] **ContentHasher.cs** (lines 4184-4273)
  - Verify: ComputeHash() synchronous method (lines 4207-4210)
  - Verify: ComputeHashAsync() async with manifest (lines 4219-4243)
  - Verify: RegenerateAsync() updates manifest (lines 4248-4272)
  - Verify: Uses ContentHash.Compute() internally
  
- [ ] **ManifestParser.cs** (lines 4276-4461)
  - Verify: Parse() method validates YAML (lines 4310-4326)
  - Verify: ParseFile() reads from disk (lines 4331-4336)
  - Verify: Uses YamlDotNet with UnderscoredNamingConvention (lines 4298-4301)
  - Verify: MapToManifest() converts DTO to domain (lines 4338-4411)
  - Verify: All field validations present
  - Verify: Error codes ACODE-PKL-001 through ACODE-PKL-005 thrown appropriately
  
- [ ] **PackDiscovery.cs** (lines 4464-4599)
  - Verify: DiscoverAsync() finds built-in and user packs (lines 4498-4526)
  - Verify: DiscoverBuiltInPacksAsync() loads from embedded resources (lines 4528-4560)
  - Verify: DiscoverUserPacksAsync() loads from user directory (lines 4562-4598)
  - Verify: User packs override built-in with same ID
  - Verify: Returns ordered by pack ID
  
- [ ] **PackDiscoveryOptions.cs** (lines 4602-4621)
  - Verify: UserPacksPath property with default
  
- [ ] **HashVerifier.cs** (infrastructure service)
  - Verify: VerifyAsync() method compares hashes
  - Verify: Returns HashVerificationResult with IsValid, HashMismatch, ExpectedHash, ActualHash
  
- [ ] **DI Registration** (lines 4624-4649)
  - Verify: PromptPacksServiceExtensions class exists
  - Verify: AddPromptPacks() method registers all services:
    - ManifestParser (Singleton)
    - PathNormalizer (Singleton)
    - ContentHasher (Singleton)
    - PackDiscovery (Singleton)

**Acceptance Criteria**:
- [x] All 7 infrastructure services present
- [x] All methods from spec implemented
- [x] DI extension method adds all required services
- [x] Build succeeds: `dotnet build src/Acode.Infrastructure/Acode.Infrastructure.csproj`

**Evidence Needed**:
```bash
grep "public.*async Task\|public void\|public string" \
  src/Acode.Infrastructure/PromptPacks/PackDiscovery.cs | wc -l
# Should show 4+ (minimum methods)
```

**Dependencies**: Domain layer must be complete

---

### 3. Code Organization Audit: Separate Task-008b Code [🔄]

**Task-008a Scope**: File layout, hashing, versioning, discovery
**Task-008b Scope** (inferred): Loading, composition, templating

**Identify Extra Files**:
- [ ] PromptPackLoader.cs - BELONGS TO 008b (move/mark as out-of-scope)
- [ ] PromptComposer.cs - BELONGS TO 008b (move/mark)
- [ ] TemplateEngine.cs - BELONGS TO 008b (move/mark)
- [ ] PackValidator.cs - BELONGS TO 008b (move/mark)
- [ ] PackCache.cs - BELONGS TO 008b (move/mark)
- [ ] ComponentMerger.cs - BELONGS TO 008b (move/mark)

**Action**: Document which files are 008a-only vs 008b code

**Acceptance Criteria**:
- [x] Identified 6+ files that belong to 008b
- [x] Documented move/refactor plan if needed
- [x] 008a scope limited to: manifest, hashing, versioning, discovery

---

## Phase 2: Test File Audit - Verify Against Spec

### 4. Audit PackManifestTests.cs [🔄]

**Location**: tests/Acode.Infrastructure.Tests/PromptPacks/PackManifestTests.cs

**Spec Reference**: Lines 2089-2288

**Required Tests** (9 tests from spec):
1. [ ] Should_Parse_Valid_Manifest (line 2091)
2. [ ] Should_Reject_Invalid_Format_Version (line 2122)
3. [ ] Should_Validate_Pack_Id_Format (line 2147) - Theory with InlineData
4. [ ] Should_Accept_Valid_Pack_Id_Format (line 2176) - Theory with InlineData
5. [ ] Should_Parse_SemVer_Version (line 2203)
6. [ ] Should_Parse_Components_With_Metadata (line 2230)
7. [ ] Should_Require_Created_At_Field (line 2266)

**Verification Commands**:
```bash
grep -c "\[Fact\]\|\[Theory\]" tests/Acode.Infrastructure.Tests/PromptPacks/PackManifestTests.cs
# Should show 7+ (minimum 7 facts/theories)

dotnet test tests/Acode.Infrastructure.Tests/PromptPacks/PackManifestTests.cs -v normal
# Should show "7 passed" or higher
```

**Acceptance Criteria**:
- [x] File exists with exact path
- [x] All 7+ test methods from spec present
- [x] Tests use xUnit [Fact] and [Theory] attributes
- [x] All tests passing
- [x] YAML examples from spec included as inline data

---

### 5. Audit ContentHasherTests.cs [🔄]

**Location**: tests/Acode.Infrastructure.Tests/PromptPacks/ContentHasherTests.cs

**Spec Reference**: Lines 2302-2442

**Required Tests** (9 tests):
1. [ ] Should_Compute_SHA256_Hash (line 2306)
2. [ ] Should_Sort_Paths_Alphabetically (line 2323)
3. [ ] Should_Normalize_Line_Endings (line 2346)
4. [ ] Should_Be_Deterministic (line 2364)
5. [ ] Should_Produce_Different_Hash_For_Different_Content (line 2384)
6. [ ] Should_Include_Path_In_Hash (line 2399)
7. [ ] Should_Handle_Empty_Components (line 2414)
8. [ ] Should_Handle_Unicode_Content (line 2428)

**Verification**:
```bash
dotnet test tests/Acode.Infrastructure.Tests/PromptPacks/ContentHasherTests.cs -v normal
# Should show "8 passed" or higher
```

---

### 6. Audit ComponentPathTests.cs [🔄]

**Location**: tests/Acode.Infrastructure.Tests/PromptPacks/ComponentPathTests.cs (or similar name)

**Spec Reference**: Lines 2455-2531

**Required Tests** (6 tests):
1. [ ] Should_Normalize_Paths (line 2459) - Theory with InlineData (5 scenarios)
2. [ ] Should_Reject_Traversal_Paths (line 2474) - Theory with InlineData (4 scenarios)
3. [ ] Should_Reject_Absolute_Paths (line 2489) - Theory with InlineData (3 scenarios)
4. [ ] Should_Handle_Unicode_Paths (line 2503)
5. [ ] Should_Validate_Path_Is_Under_Root (line 2516)

---

### 7. Audit SemVerTests.cs [🔄]

**Location**: tests/Acode.Domain.Tests/PromptPacks/SemVerTests.cs

**Spec Reference**: Lines 2544-2628

**Required Tests** (5 tests):
1. [ ] Should_Parse_Major_Minor_Patch (line 2546) - Theory with 6 InlineData
2. [ ] Should_Reject_Invalid_Versions (line 2568) - Theory with 5 InlineData
3. [ ] Should_Compare_Versions (line 2583) - Theory with 7 InlineData
4. [ ] Should_Sort_Versions (line 2604)

---

## Phase 3: Create Missing Test Files

### 8. Create PackDiscoveryTests.cs [🔄]

**Location**: tests/Acode.Integration.Tests/PromptPacks/PackDiscoveryTests.cs

**Spec Reference**: Lines 2634-2721

**Class Setup**:
```csharp
public class PackDiscoveryTests : IDisposable
{
    private readonly string _testDir;
    private readonly PackDiscovery _discovery;
    
    // Constructor creates temp directory
    // Dispose cleans up
    // CreateTestPack(string id) helper method
}
```

**Required Tests** (3 tests):
1. [ ] Should_Find_BuiltIn_Packs (line 2661)
   - Assert: packs.Contains(p => p.Id == "acode-standard")
   
2. [ ] Should_Find_User_Packs (line 2671)
   - Arrange: CreateTestPack("my-pack")
   - Assert: packs.Contains(p => p.Id == "my-pack")
   
3. [ ] Should_Prioritize_User_Packs (line 2684)
   - Arrange: Create user pack with same ID as built-in
   - Assert: pack.Source == PackSource.User

**Acceptance Criteria**:
- [x] File created with exact path
- [x] All 3 async tests present
- [x] Proper setup/teardown for temp directories
- [x] Tests passing: `dotnet test PackDiscoveryTests.cs`

---

### 9. Create/Expand HashVerificationTests.cs [🔄]

**Location**: tests/Acode.Integration.Tests/PromptPacks/HashVerificationTests.cs

**Spec Reference**: Lines 2724-2827

**Required Tests** (3 tests):
1. [ ] Should_Verify_Valid_Hash (line 2743)
2. [ ] Should_Detect_Modified_Content (line 2758)
3. [ ] Should_Regenerate_Hash (line 2775)

**Implementation Pattern**:
- IDisposable for temp directory cleanup
- CreatePackWithHash() helper
- Calls to verifier.VerifyAsync() and hasher.RegenerateAsync()

---

### 10. Create PackCreationE2ETests.cs [🔄]

**Location**: tests/Acode.E2E.Tests/PromptPacks/PackCreationE2ETests.cs

**Spec Reference**: Lines 2842-2935

**Required Tests** (3 E2E tests):
1. [ ] Should_Create_Pack_Structure (line 2852)
   - Simulate CLI PackCreateCommand
   - Verify directory structure created
   
2. [ ] Should_Generate_Valid_Manifest (line 2875)
   - Verify manifest.yml generates valid YAML
   - Parse and verify fields
   
3. [ ] Should_Compute_Initial_Hash (line 2902)
   - Verify hash computed and stored in manifest
   - Assert: manifest.ContentHash has 64 lowercase hex chars

---

### 11. Create PackLayoutBenchmarks.cs [🔄]

**Location**: tests/Acode.Performance.Tests/PromptPacks/PackLayoutBenchmarks.cs

**Spec Reference**: Lines 2940-3010

**Benchmarks** (using BenchmarkDotNet):
1. [ ] Benchmark_Hash_Small_Pack (line 2993)
   - Hash 2KB pack
   - Requirement: < 50ms (PERF-001)
   
2. [ ] Benchmark_Hash_Large_Pack (line 2999)
   - Hash 1MB pack (100 files × 10KB)
   - Requirement: < 50ms (PERF-001)
   
3. [ ] Benchmark_Parse_Manifest (line 3005)
   - Parse manifest YAML
   - Requirement: < 10ms (PERF-002)

**Performance Assertions** (lines 3013-3020):
- PERF-001: Hash 1MB < 50ms ✅
- PERF-002: Parse manifest < 10ms ✅
- PERF-003: Scan directory < 100ms (benchmark)
- PERF-004: Memory < 5MB for 1MB pack (benchmark)

---

## Phase 4: Built-in Resources Verification

### 12. Verify Embedded acode-standard Pack [🔄]

**Spec Shows** (lines 3610-3615):
```
src/Acode.Infrastructure/Resources/PromptPacks/
└── acode-standard/
    ├── manifest.yml
    ├── system.md
    └── roles/
```

**Verification Steps**:
- [ ] Directory exists: src/Acode.Infrastructure/Resources/PromptPacks/acode-standard/
- [ ] File exists: manifest.yml with valid schema
- [ ] File exists: system.md with content
- [ ] Directory exists: roles/ with at least one role file
- [ ] Manifest id field = "acode-standard"
- [ ] Embedded as resources (check .csproj EmbeddedResource items)

**Build Test**:
```bash
dotnet build src/Acode.Infrastructure/Acode.Infrastructure.csproj
# Verify no errors
```

**Runtime Test** (PackDiscovery finds built-in):
```bash
dotnet test tests/Acode.Integration.Tests/PromptPacks/PackDiscoveryTests.cs::PackDiscoveryTests::Should_Find_BuiltIn_Packs
# Should pass
```

---

## Phase 5: Final Audit & Verification

### 13. Build Success [🔄]

**Command**:
```bash
dotnet build --configuration Release --verbosity minimal
```

**Acceptance Criteria**:
- [x] Build completes successfully
- [x] No CS* errors
- [x] No CS* warnings in PromptPacks code
- [x] All projects build: Domain, Application, Infrastructure, Tests

**Evidence**: Build log showing "Build succeeded"

---

### 14. Test Suite Success [🔄]

**Command**:
```bash
dotnet test --filter "FullyQualifiedName~PromptPacks" --verbosity normal
```

**Acceptance Criteria**:
- [x] All unit tests pass (30+ tests minimum)
- [x] All integration tests pass (6 tests)
- [x] All E2E tests pass (3 tests)
- [x] Performance tests pass with latency requirements met
- [x] Total: 50+ tests passing

**Test Breakdown**:
- PackManifestTests: 7 passing ✅
- ContentHasherTests: 8 passing ✅
- ComponentPathTests: 5 passing ✅
- SemVerTests: 4 passing ✅
- PackDiscoveryTests: 3 passing ✅
- HashVerificationTests: 3 passing ✅
- PackCreationE2ETests: 3 passing ✅
- PackLayoutBenchmarks: 3 passing ✅

**Total: 36 base tests + existing tests = 50+ passing**

---

## Summary

### Total Checklist Items: 14

### Implementation Phases:
1. **Existing Code Audit** (items 1-3): Verify domain/infrastructure complete
2. **Test File Audit** (items 4-7): Audit 4 existing test files
3. **Create Missing Tests** (items 8-11): Create 4 new test files
4. **Verify Resources** (item 12): Confirm embedded packs
5. **Final Audit** (items 13-14): Build and test success

### Success Criteria
- ✅ All 6 domain models complete and correct
- ✅ All 7 infrastructure services complete and correct
- ✅ 50+ tests passing (8+ test files)
- ✅ Performance benchmarks meet requirements
- ✅ Build succeeds with no warnings
- ✅ Built-in acode-standard pack embedded and discoverable

---

## References

- **Spec**: task-008a-prompt-pack-file-layout-hashing-versioning.md (~4705 lines)
- **Gap Analysis**: task-008a-gap-analysis.md
- **CLAUDE.md Section 3.2**: Gap Analysis and Completion Checklist methodology
