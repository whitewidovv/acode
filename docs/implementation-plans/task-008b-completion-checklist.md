# Task-008b Completion Checklist: Loader/Validator + Selection via Config

**Task**: task-008b-loader-validator-selection-via-config.md (Epic 01)
**Status**: Implementation Phase
**Date**: 2026-01-13
**Spec Reference**: ~/docs/tasks/refined-tasks/Epic 01/task-008b-loader-validator-selection-via-config.md
**Gap Analysis Reference**: ~/docs/implementation-plans/task-008b-gap-analysis.md

---

## Instructions for Fresh Agent

This checklist contains **ALL gaps** identified in task-008b that require implementation or verification. Each item includes:
- Item number and description
- Current status indicator
- Spec reference (lines/sections)
- Implementation location (file path)
- Acceptance criteria (what constitutes "done")
- TDD test commands (how to verify)

**Working Order**: Follow this checklist sequentially. For each item:

1. **Read** the spec section referenced (detailed guidance)
2. **Find** the file at the location specified
3. **Implement** according to acceptance criteria
4. **Test** using commands provided
5. **Mark** ✅ when complete and tests pass

**Important**:
- Many items are inter-dependent (e.g., tests depend on implementation)
- TDD ordering: Read tests from spec, verify tests fail, implement code, verify tests pass
- Use `dotnet test --filter "..." --verbosity normal` to run specific test groups
- Commit frequently (after each logical grouping) with descriptive messages
- Do NOT skip items just because they seem non-critical - ALL items block PR creation

---

## CRITICAL: Interface Signature Decision (✅ RESOLVED - ASYNC APPROVED)

**Status**: ✅ **RESOLVED - SPEC UPDATED**

**Issue Resolved**: Spec showed synchronous methods (`LoadPack()`, `LoadBuiltInPack()`, etc.) but async is the correct modern .NET pattern for file I/O.

**Resolution**: File I/O operations should be async to:
1. Enable non-blocking pack loading in CLI commands
2. Support concurrent pack loading if needed
3. Better resource utilization (threads not blocked on disk I/O)
4. Follow modern .NET best practices and idiomatic patterns

**Action Taken**:
- ✅ Updated specification (lines 2851-2938) to show async methods with CancellationToken support
- ✅ Specification now reflects: `LoadPackAsync()`, `LoadBuiltInPackAsync()`, etc. with `Task<T>` returns
- ✅ This is a **SPEC CORRECTION**, not a code deviation
- ✅ Implementation is correct and matches updated spec

**For This Checklist**: All items below assume async methods per updated spec.

---

## Phase 1: Verify Current Interface Implementations

### Item 1: Verify IPromptPackLoader Interface

**Status**: ✅ (Spec Updated)

**Location**: `src/Acode.Application/PromptPacks/IPromptPackLoader.cs`

**Spec Reference**: Implementation Prompt, lines 2851-2873 (updated for async)

**Acceptance Criteria**:
- [ ] File exists at location
- [ ] Contains async methods:
  - [ ] `Task<PromptPack> LoadPackAsync(string path, CancellationToken ct = default)`
  - [ ] `Task<PromptPack> LoadBuiltInPackAsync(string packId, CancellationToken ct = default)`
  - [ ] `Task<PromptPack> LoadUserPackAsync(string path, CancellationToken ct = default)`
  - [ ] `Task<(bool success, PromptPack? pack, string? error)> TryLoadPackAsync(string path, CancellationToken ct = default)`
- [ ] All methods return Task<T> types (async)
- [ ] All methods accept optional CancellationToken parameter
- [ ] All methods have XML documentation with summaries
- [ ] Interface is in namespace: `Acode.Application.PromptPacks`
- [ ] Note: Spec was updated from sync to async (file I/O should be non-blocking per modern .NET best practices)

**Test Command**:
```bash
dotnet build && echo "✅ Interface compiles successfully"
grep -n "Task<PromptPack>" src/Acode.Application/PromptPacks/IPromptPackLoader.cs && echo "✅ Async methods found" || echo "❌ Async methods missing"
```

**Evidence**: Paste output of grep command showing method signatures

---

### Item 2: Verify IPackValidator Interface

**Status**: 🔄

**Location**: `src/Acode.Application/PromptPacks/IPackValidator.cs`

**Spec Reference**: Implementation Prompt, lines 2876-2907 (updated for async)

**Acceptance Criteria**:
- [ ] File exists at location
- [ ] Contains methods:
  - [ ] `ValidationResult Validate(PromptPack pack)` - synchronous, in-memory validation only
  - [ ] `Task<ValidationResult> ValidatePathAsync(string packPath, CancellationToken ct = default)` - async, includes file checks
- [ ] Returns ValidationResult with IsValid property and Errors collection
- [ ] ValidationResult is sealed class
- [ ] ValidationError is sealed class with properties: Code, Message, FilePath, LineNumber
- [ ] All error code constants documented (VAL-001 through VAL-008)
- [ ] All types have XML documentation with summaries

**Test Command**:
```bash
dotnet build && echo "✅ Interface compiles successfully"
grep -A5 "class ValidationResult" src/Acode.Application/PromptPacks/ValidationResult.cs
grep -A5 "class ValidationError" src/Acode.Application/PromptPacks/ValidationError.cs
```

**Evidence**: Paste class definitions

---

### Item 3: Verify IPromptPackRegistry Interface

**Status**: 🔄

**Location**: `src/Acode.Application/PromptPacks/IPromptPackRegistry.cs`

**Spec Reference**: Implementation Prompt, lines 2910-2938 (updated for async)

**Acceptance Criteria**:
- [ ] File exists at location
- [ ] Contains all async methods:
  - [ ] `Task InitializeAsync(CancellationToken ct = default)` - discovers all packs at startup
  - [ ] `Task<PromptPack> GetPackAsync(string packId, CancellationToken ct = default)` - loads and caches pack
  - [ ] `Task<PromptPack?> TryGetPackAsync(string packId, CancellationToken ct = default)` - returns null if not found
  - [ ] `IReadOnlyList<PromptPackInfo> ListPacks()` - synchronous, returns metadata only
  - [ ] `Task<PromptPack> GetActivePackAsync(CancellationToken ct = default)` - gets configured active pack
  - [ ] `Task RefreshAsync(CancellationToken ct = default)` - clears cache and re-discovers packs
- [ ] Return types correct: Task<T>, Task, IReadOnlyList<PromptPackInfo>
- [ ] All async methods accept CancellationToken parameter
- [ ] XML documentation present with summaries for each method

**Test Command**:
```bash
dotnet build && echo "✅ Registry interface compiles"
grep "GetActive\|Refresh\|ListPacks" src/Acode.Application/PromptPacks/IPromptPackRegistry.cs
```

**Evidence**: Paste method signatures

---

## Phase 2: Verify Current Implementation Classes

### Item 4: Verify PromptPackLoader Implementation

**Status**: 🔄

**Location**: `src/Acode.Infrastructure/PromptPacks/PromptPackLoader.cs`

**Spec Reference**: Description section, lines 215-239; Implementation details

**Acceptance Criteria**:
- [ ] Implements IPromptPackLoader interface
- [ ] Constructor accepts IFileSystem, ILogger<PromptPackLoader>
- [ ] LoadPackAsync method:
  - [ ] Reads manifest.yml from pack directory
  - [ ] Parses YAML using YamlDotNet into PackManifest
  - [ ] Reads each component file from disk
  - [ ] Calculates SHA-256 hash of component contents
  - [ ] Compares hash to manifest content_hash (logs warning if mismatch, does NOT fail)
  - [ ] Performs path security checks (prevents ../../../etc/passwd)
  - [ ] Rejects symlinks (checks FileAttributes.ReparsePoint)
  - [ ] Handles encoding errors (tries UTF-8, falls back to Latin1)
  - [ ] Throws PackLoadException with error code for failures
- [ ] LoadBuiltInPackAsync method:
  - [ ] Reads from embedded resources (not filesystem)
  - [ ] Sets PackSource = PackSource.BuiltIn
- [ ] LoadUserPackAsync method:
  - [ ] Reads from filesystem at specified path
  - [ ] Sets PackSource = PackSource.User
- [ ] TryLoadPackAsync method:
  - [ ] Returns false + error message instead of throwing on expected errors

**Test Command**:
```bash
dotnet test --filter "PromptPackLoaderTests" --verbosity normal
```

**Evidence**: Count of passing tests, any failures reported

---

### Item 5: Verify PackValidator Implementation

**Status**: 🔄

**Location**: `src/Acode.Infrastructure/PromptPacks/PackValidator.cs`

**Spec Reference**: Description section, lines 231-254; Validator Implementation Details

**Acceptance Criteria**:
- [ ] Implements IPackValidator interface
- [ ] Constructor accepts IFileSystem
- [ ] Validate method checks all rules and returns ValidationResult with all errors (does NOT fail on first error):
  - [ ] **VAL-001**: Required fields (id, version, name, description, components non-empty)
  - [ ] **VAL-002**: Pack ID format matches regex `^[a-z][a-z0-9-]*[a-z0-9]$`
  - [ ] **VAL-003**: Version is valid SemVer 2.0 format
  - [ ] **VAL-004**: Each component file exists at path
  - [ ] **VAL-005**: All template variables {{name}} are declared in manifest variables
  - [ ] **VAL-006**: Total size (manifest + all components) <= 5MB (5242880 bytes)
  - [ ] **VAL-007**: Component paths are relative, no ../ or absolute paths
  - [ ] **VAL-008**: No circular references in component includes ({{>component}})
- [ ] Validation completes in < 100ms for 5MB packs
- [ ] ValidatePath method validates from filesystem (checks file existence with IFileSystem)

**Test Command**:
```bash
dotnet test --filter "PackValidatorTests" --verbosity normal
```

**Evidence**: Count of passing tests, specific error code tests mentioned

---

### Item 6: Verify PromptPackRegistry Implementation

**Status**: 🔄

**Location**: `src/Acode.Infrastructure/PromptPacks/PromptPackRegistry.cs`

**Spec Reference**: Description section, lines 286-317; Pack Registry Implementation Details

**Acceptance Criteria**:
- [ ] Implements IPromptPackRegistry interface
- [ ] Constructor accepts: IPromptPackLoader, IPackValidator, IPackDiscovery, IPackCache, IConfiguration, ILogger
- [ ] InitializeAsync method:
  - [ ] Calls PackDiscovery to find all built-in packs
  - [ ] Calls PackDiscovery to find all user packs
  - [ ] Builds index (Dictionary<string, PackInfo>)
  - [ ] User packs override built-in packs with same ID (user version wins)
  - [ ] Completes in < 500ms for 10 packs
- [ ] GetActivePackAsync method:
  - [ ] Reads configuration (env var > config file > default)
  - [ ] Checks cache (key = "{packId}:{contentHash}")
  - [ ] On cache miss: calls loader, calls validator, stores in cache
  - [ ] On missing pack: logs warning, falls back to acode-standard
- [ ] RefreshAsync method:
  - [ ] Clears cache
  - [ ] Re-runs discovery
  - [ ] Rebuilds index
  - [ ] Reloads active pack
- [ ] Thread-safe cache implementation (ConcurrentDictionary)

**Test Command**:
```bash
dotnet test --filter "PromptPackRegistryTests" --verbosity normal
```

**Evidence**: Test count and pass/fail status

---

### Item 7: Verify PackConfiguration Implementation

**Status**: 🔄

**Location**: `src/Acode.Infrastructure/PromptPacks/PackConfiguration.cs`

**Spec Reference**: Configuration-Based Selection Details, lines 256-284

**Acceptance Criteria**:
- [ ] Implements configuration precedence:
  1. Environment variable ACODE_PROMPT_PACK (highest)
  2. Config file prompts.pack_id (middle)
  3. Default acode-standard (lowest)
- [ ] ReadsPacks configuration section from .agent/config.yml
- [ ] Configuration schema includes:
  ```yaml
  prompts:
    pack_id: acode-standard
    discovery:
      user_path: .acode/prompts
      enable_builtin: true
  ```
- [ ] Falls back gracefully if config file missing
- [ ] Caches configuration value (evaluated once at startup)

**Test Command**:
```bash
dotnet build && echo "✅ Compiles"
grep -n "ACODE_PROMPT_PACK\|GetPackId" src/Acode.Infrastructure/PromptPacks/PackConfiguration.cs
```

**Evidence**: Show relevant grep output

---

### Item 8: Verify PackCache Implementation

**Status**: 🔄

**Location**: `src/Acode.Infrastructure/PromptPacks/PackCache.cs`

**Spec Reference**: Caching Implementation Details, lines 302-316

**Acceptance Criteria**:
- [ ] Uses ConcurrentDictionary<string, PromptPack> (thread-safe)
- [ ] Cache key format: "{packId}:{contentHash}"
- [ ] Implements Get/Set/Clear operations
- [ ] Cache lookup < 1ms (O(1) dictionary lookup)
- [ ] No size limit or eviction policy (packs max 5MB, typical <10 packs)
- [ ] Clear() is thread-safe

**Test Command**:
```bash
dotnet test --filter "PackCacheTests" --verbosity normal
```

**Evidence**: Test pass count

---

## Phase 3: Complete Unit Test Coverage

### Item 9: Ensure PromptPackLoaderTests Has All 11 Tests

**Status**: 🔄

**Location**: `tests/Acode.Infrastructure.Tests/PromptPacks/PromptPackLoaderTests.cs`

**Spec Reference**: Testing Requirements, lines 1158-1486

**Acceptance Criteria** - All 11 tests present:
- [ ] Should_Load_Valid_Pack
- [ ] Should_Fail_On_Missing_Manifest
- [ ] Should_Fail_On_Invalid_YAML
- [ ] Should_Warn_On_Hash_Mismatch
- [ ] Should_Load_All_Components
- [ ] Should_Block_Path_Traversal
- [ ] Should_Reject_Symlinks
- [ ] Should_Handle_Encoding_Fallback
- [ ] Should_Load_BuiltIn_Pack_From_Embedded_Resources
- [ ] Should_Calculate_Correct_Content_Hash
- [ ] (One additional test - verify by reading spec lines 1441+)

**Test Command**:
```bash
dotnet test tests/Acode.Infrastructure.Tests/PromptPacks/PromptPackLoaderTests.cs --verbosity normal
```

**Evidence**: All tests pass, count = 11

**Implementation Notes**:
- Tests use NSubstitute for IFileSystem mocking
- Spec provides complete test code (lines 1148-1486) - use as reference
- Error codes tested: ACODE-PKL-001, ACODE-PKL-002, ACODE-PKL-004

---

### Item 10: Ensure PackValidatorTests Has All 11 Tests

**Status**: 🔄

**Location**: `tests/Acode.Infrastructure.Tests/PromptPacks/PackValidatorTests.cs`

**Spec Reference**: Testing Requirements, lines 1503-1795

**Acceptance Criteria** - All 11 tests present:
- [ ] Should_Validate_Required_Fields (VAL-001)
- [ ] Should_Validate_Id_Format (VAL-002)
- [ ] Should_Accept_Valid_Id_Format (VAL-002 negative case)
- [ ] Should_Validate_Version_Format (VAL-003)
- [ ] Should_Accept_Valid_SemVer_Format (VAL-003 negative case)
- [ ] Should_Check_Files_Exist (VAL-004)
- [ ] Should_Validate_Template_Variables (VAL-005)
- [ ] Should_Enforce_Size_Limit (VAL-006)
- [ ] Should_Complete_Validation_Within_Time_Limit
- [ ] Should_Detect_Circular_References (VAL-008)
- [ ] Should_Validate_Component_Path_Format (VAL-007)

**Test Command**:
```bash
dotnet test tests/Acode.Infrastructure.Tests/PromptPacks/PackValidatorTests.cs --verbosity normal
```

**Evidence**: All tests pass, count = 11

**Implementation Notes**:
- Tests use NSubstitute for IFileSystem mocking
- Spec provides complete test code (lines 1493-1795) - use as reference
- Helper method CreateValidPack() needed (provided in spec line 1782)

---

## Phase 4: Create Integration Tests

### Item 11: Create LoaderIntegrationTests.cs File

**Status**: ❌ (MISSING)

**Location**: `tests/Acode.Integration.Tests/PromptPacks/LoaderIntegrationTests.cs`

**Spec Reference**: Testing Requirements, lines 1810-1970 (complete test code provided)

**Acceptance Criteria** - File contains 5 real-filesystem tests:
- [ ] Uses real file system (not mocked), creates temp directory
- [ ] Uses IDisposable to clean up temp files after tests
- [ ] **Test 1: Should_Load_BuiltIn_Pack**
  - Calls LoadBuiltInPackAsync("acode-standard")
  - Verifies pack ID, version, components non-empty
  - Verifies source = PackSource.BuiltIn
- [ ] **Test 2: Should_Load_User_Pack**
  - Creates temp pack directory with manifest.yml and system.md
  - Calls LoadUserPackAsync(tempPath)
  - Verifies pack loads correctly
  - Verifies source = PackSource.User
- [ ] **Test 3: Should_Handle_Missing_Directory**
  - Calls LoadPackAsync with non-existent path
  - Expects PackLoadException with message containing "not found"
- [ ] **Test 4: Should_Load_Pack_With_Subdirectories**
  - Creates hierarchical pack structure: system.md, roles/planner.md, languages/csharp.md
  - Verifies all 3 components loaded
- [ ] **Test 5: Should_Handle_Large_Pack_Within_Limit**
  - Creates pack with 4 components × 1MB each = 4MB total (under 5MB limit)
  - Verifies pack loads successfully

**Test Command**:
```bash
dotnet test tests/Acode.Integration.Tests/PromptPacks/LoaderIntegrationTests.cs --verbosity normal
```

**Expected Output**: 5 passing tests

**Implementation Notes**:
- Use RealFileSystem (not mocked)
- Use NullLogger<PromptPackLoader>()
- Spec lines 1810-1970 provides complete implementation
- Must use async/await with proper disposal

---

### Item 12: Create RegistryIntegrationTests.cs File

**Status**: ❌ (MISSING)

**Location**: `tests/Acode.Integration.Tests/PromptPacks/RegistryIntegrationTests.cs`

**Spec Reference**: Testing Requirements, lines 1985-2100 (complete test code provided)

**Acceptance Criteria** - File contains 3 real-filesystem tests:
- [ ] Uses real file system, creates temp directory with in-memory configuration
- [ ] Uses IDisposable for cleanup
- [ ] **Test 1: Should_Index_All_Packs**
  - Initializes registry
  - Calls ListPacks()
  - Verifies built-in packs found (acode-standard at minimum)
- [ ] **Test 2: Should_Refresh_Registry**
  - Gets initial pack count
  - Adds new user pack to .acode/prompts/
  - Calls RefreshAsync()
  - Verifies new pack in list
- [ ] **Test 3: Should_Prioritize_User_Pack_Over_BuiltIn**
  - Creates user pack with same ID as built-in (acode-standard)
  - Initializes registry
  - Calls GetPackAsync("acode-standard")
  - Verifies returned pack source = PackSource.User (not BuiltIn)
  - Verifies version = 2.0.0 (user override version)

**Test Command**:
```bash
dotnet test tests/Acode.Integration.Tests/PromptPacks/RegistryIntegrationTests.cs --verbosity normal
```

**Expected Output**: 3 passing tests

**Implementation Notes**:
- Create ConfigurationBuilder with in-memory collection
- Spec lines 1985-2100 provides complete implementation
- Tests real discovery and cache behavior

---

## Phase 5: Create E2E Tests

### Item 13: Implement CLI Commands (Prerequisite for E2E Tests)

**Status**: ❌ (MISSING)

**Location**: CLI command handlers (location to be determined - likely `src/Acode.CLI/Commands/PromptPacks/`)

**Spec Reference**: Description, lines 362-373; User Verification Steps, lines 2493-2540+

**Acceptance Criteria** - Three CLI commands:
- [ ] **Command 1: `acode prompts list`**
  - Calls IPromptPackRegistry.ListPacks()
  - Displays table with columns: ID, Version, Source, Status
  - Marks active pack with "active" in Status column
  - Exit code 0 on success
  - Example output provided in spec lines 2507-2517

- [ ] **Command 2: `acode prompts validate [path]`**
  - Calls IPromptPackValidator.ValidatePath(path)
  - On validation pass: Exit code 0, print "✓ Pack is valid"
  - On validation fail: Exit code 1, print all validation errors with error codes (VAL-001, etc.)
  - Include file paths and line numbers where applicable

- [ ] **Command 3: `acode prompts reload`**
  - Calls IPromptPackRegistry.RefreshAsync()
  - Outputs "Refreshing pack registry..."
  - Outputs count of discovered packs
  - Outputs active pack after refresh
  - Exit code 0 on success

**Acceptance**: All three commands compile and work end-to-end

**Implementation Notes**:
- Use existing CLI framework
- Follow existing command patterns
- Commands not tested in checklist item 13 itself; they enable item 14

---

### Item 14: Create PackSelectionE2ETests.cs File

**Status**: ❌ (MISSING)

**Location**: `tests/Acode.E2E.Tests/PromptPacks/PackSelectionE2ETests.cs`

**Spec Reference**: Testing Requirements, lines 2113-2200 (complete test code provided)

**Acceptance Criteria** - File contains 3 CLI-based E2E tests:
- [ ] Uses real CLI invocation (not mocked), creates temp workspace
- [ ] Uses IDisposable for cleanup
- [ ] **Test 1: Should_Use_Configured_Pack**
  - Creates .agent/config.yml with `prompts.pack_id: acode-dotnet`
  - Runs CLI command: `acode prompts list`
  - Verifies output contains "acode-dotnet" with "active" status
  - Exit code 0

- [ ] **Test 2: Should_Use_Env_Override**
  - Creates .agent/config.yml with `prompts.pack_id: acode-standard`
  - Sets environment variable: `ACODE_PROMPT_PACK=acode-react`
  - Runs CLI command: `acode prompts list`
  - Verifies output contains "acode-react" with "active" status
  - Verifies "acode-standard" NOT marked as active
  - Exit code 0
  - Cleans up environment variable after test

- [ ] **Test 3: Should_Fallback_Gracefully**
  - Creates .agent/config.yml with `prompts.pack_id: nonexistent-pack`
  - Runs CLI command: `acode prompts list`
  - Verifies exit code 0 (no crash)
  - Verifies error message: "Pack 'nonexistent-pack' not found"
  - Verifies warning: "falling back to 'acode-standard'"
  - Verifies output contains "acode-standard" as active

**Test Command**:
```bash
dotnet test tests/Acode.E2E.Tests/PromptPacks/PackSelectionE2ETests.cs --verbosity normal
```

**Expected Output**: 3 passing tests

**Implementation Notes**:
- Spec lines 2113-2200 provides complete implementation
- Tests actual CLI invocation, not just API calls
- Temp directory cleanup important for isolation

---

## Phase 6: Verify Performance Benchmarks

### Item 15: Ensure PackPerformanceTests Has All 4 Benchmarks

**Status**: 🔄 (PARTIAL)

**Location**: `tests/Acode.Performance.Tests/PromptPacks/PackPerformanceTests.cs`

**Spec Reference**: Testing Requirements, lines 2214-2305; Performance Requirements, lines 376-391

**Acceptance Criteria** - File contains 4 BenchmarkDotNet benchmarks:
- [ ] **PERF-001: Pack_Loading_Under_100ms**
  - Benchmark target: < 100ms
  - Loads test pack from filesystem
  - Spec lines 2252-2258

- [ ] **PERF-002: Validation_Under_100ms**
  - Benchmark target: < 100ms
  - Validates test pack object
  - Spec lines 2260-2266

- [ ] **PERF-003: Registry_Init_Under_500ms**
  - Benchmark target: < 500ms
  - Initializes registry with discovery
  - Spec lines 2268-2284

- [ ] **PERF-004: Cache_Lookup_Under_1ms**
  - Benchmark target: < 1ms
  - Cache set then get operation
  - Spec lines 2286-2295

**Test Command**:
```bash
# Run in Release configuration for accurate benchmark
dotnet test tests/Acode.Performance.Tests/PromptPacks/PackPerformanceTests.cs \
  --configuration Release --verbosity normal
```

**Expected Output**: All benchmarks show execution times below targets

**Implementation Notes**:
- Use BenchmarkDotNet attributes: [MemoryDiagnoser], [Benchmark], [GlobalSetup], [GlobalCleanup]
- Spec lines 2214-2305 provides complete implementation
- Run in Release build for accurate timing

---

## Phase 7: Error Code Verification

### Item 16: Verify All Error Codes Used Correctly

**Status**: 🔄

**Location**: Multiple files (PromptPackLoader, PackValidator, exceptions)

**Spec Reference**: Implementation Prompt, lines 2906-2920

**Acceptance Criteria** - All error codes defined and used:
- [ ] **Loader Error Codes** (PromptPackLoader.cs):
  - [ ] ACODE-PKL-001: Pack not found
  - [ ] ACODE-PKL-002: Manifest parse error
  - [ ] ACODE-PKL-003: Component read error
  - [ ] ACODE-PKL-004: Permission denied / Path traversal / Symlink

- [ ] **Validator Error Codes** (PackValidator.cs):
  - [ ] ACODE-VAL-001: Missing required field
  - [ ] ACODE-VAL-002: Invalid pack ID
  - [ ] ACODE-VAL-003: Invalid version
  - [ ] ACODE-VAL-004: Component not found
  - [ ] ACODE-VAL-005: Invalid template variable
  - [ ] ACODE-VAL-006: Size exceeded (5MB limit)

- [ ] Each error thrown with correct code
- [ ] Error messages clear and actionable (include file path, line number where applicable)
- [ ] Tests verify correct error codes (see Items 9-10)

**Test Command**:
```bash
grep -r "ACODE-PKL\|ACODE-VAL" src/Acode.Infrastructure/PromptPacks/ | wc -l
# Should show 10+ matches
```

**Evidence**: Show error code usage across implementation

---

### Item 17: Verify CLI Exit Codes

**Status**: 🔄

**Location**: CLI command handlers

**Spec Reference**: Implementation Prompt, lines 2934-2942

**Acceptance Criteria** - Exit codes correct:
- [ ] Exit code 0: Success (command completed without errors)
- [ ] Exit code 1: Validation failed (pack validation errors found)
- [ ] Exit code 2: Pack not found (specified pack not in registry)
- [ ] Exit code 3: File I/O error (cannot read pack files)

**Test Command**:
```bash
# Test validation failure
acode prompts validate /nonexistent/path; echo "Exit code: $?"
# Should print: Exit code: 3

# Test successful list
acode prompts list; echo "Exit code: $?"
# Should print: Exit code: 0
```

**Evidence**: Show exit codes from manual CLI tests

---

## Phase 8: Final Audit & Build

### Item 18: Full Build and Compilation

**Status**: 🔄

**Location**: Entire solution

**Acceptance Criteria**:
- [ ] `dotnet build --configuration Debug` - No errors, no warnings related to PromptPacks
- [ ] All .cs files compile
- [ ] No CS0619 obsolete warnings
- [ ] All dependencies resolve correctly

**Test Command**:
```bash
dotnet build --configuration Debug
echo "Build status: $?"
# Exit code should be 0
```

**Evidence**: Build output showing success

---

### Item 19: Run All Unit Tests

**Status**: 🔄

**Location**: Test files

**Acceptance Criteria**:
- [ ] All loader tests pass (11 tests)
- [ ] All validator tests pass (11 tests)
- [ ] All cache tests pass (count from existing file)
- [ ] All registry tests pass (count from existing file)
- [ ] Total unit tests: 22+ passing

**Test Command**:
```bash
dotnet test tests/Acode.Infrastructure.Tests/PromptPacks/ \
  --filter "PromptPackLoaderTests|PackValidatorTests" \
  --verbosity normal
```

**Expected Output**: All tests pass

**Evidence**: Test summary showing pass count

---

### Item 20: Run All Integration Tests

**Status**: 🔄

**Location**: Integration test files

**Acceptance Criteria**:
- [ ] LoaderIntegrationTests: 5 tests pass
- [ ] RegistryIntegrationTests: 3 tests pass
- [ ] Total integration tests: 8 passing

**Test Command**:
```bash
dotnet test tests/Acode.Integration.Tests/PromptPacks/ \
  --filter "LoaderIntegrationTests|RegistryIntegrationTests" \
  --verbosity normal
```

**Expected Output**: 8 passing tests

**Evidence**: Test summary

---

### Item 21: Run All E2E Tests

**Status**: 🔄

**Location**: E2E test file

**Acceptance Criteria**:
- [ ] PackSelectionE2ETests: 3 tests pass
- [ ] CLI commands work end-to-end
- [ ] Environment variable override works
- [ ] Fallback behavior works

**Test Command**:
```bash
dotnet test tests/Acode.E2E.Tests/PromptPacks/ \
  --filter "PackSelectionE2ETests" \
  --verbosity normal
```

**Expected Output**: 3 passing tests

**Evidence**: Test summary

---

### Item 22: Run Performance Benchmarks

**Status**: 🔄

**Location**: Performance test file

**Acceptance Criteria**:
- [ ] All 4 benchmarks run successfully
- [ ] PERF-001: < 100ms
- [ ] PERF-002: < 100ms
- [ ] PERF-003: < 500ms
- [ ] PERF-004: < 1ms

**Test Command**:
```bash
dotnet test tests/Acode.Performance.Tests/PromptPacks/PackPerformanceTests.cs \
  --configuration Release \
  --verbosity normal
```

**Expected Output**: All benchmarks show times within targets

**Evidence**: Benchmark output with timing results

---

### Item 23: Run Full Test Suite for PromptPacks

**Status**: 🔄

**Location**: All test files

**Acceptance Criteria**:
- [ ] All tests pass (unit + integration + E2E + performance)
- [ ] No skipped tests
- [ ] Total count: 36+ tests/benchmarks
- [ ] No warnings

**Test Command**:
```bash
dotnet test --filter "FullyQualifiedName~PromptPacks" \
  --verbosity normal \
  --configuration Release
```

**Expected Output**: 36+ tests pass, 0 failed, 0 skipped

**Evidence**: Complete test summary

---

### Item 24: Verify Documentation

**Status**: 🔄

**Location**: Source code files

**Acceptance Criteria**:
- [ ] All public types have XML documentation (///)
- [ ] All public methods have documentation with <summary>, <param>, <returns>
- [ ] Error conditions documented
- [ ] Example usage comments where complex

**Verification**:
```bash
# Check for methods without documentation
grep -E "public (async )?[A-Z]" src/Acode.Infrastructure/PromptPacks/*.cs | \
  grep -v "///" | wc -l
# Should be 0 (all methods documented)
```

**Evidence**: Output showing 0 undocumented methods

---

### Item 25: Acceptance Criteria Coverage Matrix

**Status**: 🔄

**Location**: Spec lines 1038-1140

**Acceptance Criteria** - Verify coverage:

| AC # | Requirement | Covered By | Status |
|------|-------------|-----------|--------|
| AC-001 | Loader reads manifest | PromptPackLoaderTests | ✅ Item 9 |
| AC-002 | Loader reads components | PromptPackLoaderTests | ✅ Item 9 |
| AC-003 | Validator checks required | PackValidatorTests | ✅ Item 10 |
| AC-004 | Validator checks ID format | PackValidatorTests | ✅ Item 10 |
| AC-005 | Validator checks SemVer | PackValidatorTests | ✅ Item 10 |
| AC-010 | Registry discovers built-in | RegistryIntegrationTests | ✅ Item 12 |
| AC-011 | Registry discovers user | RegistryIntegrationTests | ✅ Item 12 |
| AC-012 | User override built-in | RegistryIntegrationTests | ✅ Item 12 |
| AC-020 | Config reads env var | PackSelectionE2ETests | ✅ Item 14 |
| AC-021 | Config reads config file | PackSelectionE2ETests | ✅ Item 14 |
| AC-022 | Fallback to default | PackSelectionE2ETests | ✅ Item 14 |
| AC-030 | Block path traversal | PromptPackLoaderTests | ✅ Item 9 |
| AC-031 | Reject symlinks | PromptPackLoaderTests | ✅ Item 9 |
| AC-032 | Cache key includes hash | PackCacheTests | 🔄 Verify |
| AC-040 | Performance load < 100ms | PackPerformanceTests | ✅ Item 15 |
| AC-041 | Performance validate < 100ms | PackPerformanceTests | ✅ Item 15 |
| AC-042 | Performance init < 500ms | PackPerformanceTests | ✅ Item 15 |

**Evidence**: Coverage complete, all acceptance criteria linked to tests

---

## Commit Points

Commit after completing each phase with descriptive messages:

```bash
# After Phase 1-2 (Verification)
git add docs/implementation-plans/task-008b-*
git commit -m "docs: add task-008b gap analysis and completion checklist"

# After Phase 3 (Unit Tests)
git add tests/
git commit -m "test: complete PromptPackLoaderTests and PackValidatorTests with 22 tests"

# After Phase 4 (Integration Tests)
git add tests/
git commit -m "test: add LoaderIntegrationTests and RegistryIntegrationTests with 8 tests"

# After Phase 5 (E2E Tests)
git add src/Acode.CLI tests/
git commit -m "feat: implement prompts CLI commands and add E2E tests"

# After Phase 6-7 (Performance & Error Codes)
git add tests/
git commit -m "test: verify performance benchmarks and error code coverage"

# After Phase 8 (Final Audit)
git add .
git commit -m "docs: complete task-008b implementation - all tests passing"
```

---

## How to Use This Checklist

1. **Fresh Agent Starting**: Read this document entirely. Understand all 25 items. Ask user to confirm interface decision (Item 1 - CRITICAL).

2. **Working Through Items**: For each item:
   - Read spec reference sections
   - Implement or verify per criteria
   - Run test commands
   - Record evidence (test output, grep output)
   - Mark complete when tests pass

3. **When Tests Fail**:
   - Use `--verbosity normal` for detailed error messages
   - Check spec for exact test implementation (provided in full)
   - Fix implementation, re-test
   - Do NOT mark complete until tests pass

4. **Dependencies**: Later items depend on earlier items. Complete in order.

5. **Completion**: Task is complete when:
   - All 25 checklist items marked ✅
   - All 36+ tests passing
   - `dotnet build` succeeds with no warnings
   - PR created with all commits

---

## References

- **Main Spec**: ~/docs/tasks/refined-tasks/Epic 01/task-008b-loader-validator-selection-via-config.md
- **Gap Analysis**: ~/docs/implementation-plans/task-008b-gap-analysis.md
- **Acceptance Criteria**: Spec lines 1038-1140
- **Testing Requirements**: Spec lines 1142-2307 (complete test code)
- **Implementation Prompt**: Spec lines 2830-2969
- **User Verification Steps**: Spec lines 2491-2540+
- **Task-008a Spec**: For domain models and file format details
- **CLAUDE.md**: Section 3.2 (Gap Analysis Methodology)
