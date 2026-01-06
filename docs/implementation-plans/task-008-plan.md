# Task 008 Implementation Plan: Prompt Pack System

## Status: In Progress

**Started:** 2026-01-05
**Epic:** Epic 01 - Model Runtime, Inference, Tool-Calling Contract
**Priority:** P1 (High)
**Complexity:** 21 Fibonacci Points

---

## Executive Summary

Task 008 implements the Prompt Pack System, a modular architecture for managing system prompts, coding guidelines, and behavioral configurations. This is foundational infrastructure that shapes all model interactions across the Acode agent.

**Scope Breakdown:**
- Task 008 (Parent): Overall architecture, domain models, composition engine
- Task 008a: File layout, hashing, versioning
- Task 008b: Loader, validator, selection via config
- Task 008c: Starter packs (acode-standard, acode-dotnet, acode-react)

**Total Specification:** 14,852 lines
**Total Functional Requirements:** 162+
**Total Acceptance Criteria:** 218+

---

## Implementation Strategy

### Core Principles

1. **Strict TDD** - Red → Green → Refactor for every behavior
2. **Incremental Commits** - One logical unit per commit
3. **Subtask Completion** - Task 008 NOT complete until ALL subtasks (008a, 008b, 008c) complete
4. **Layer Boundaries** - Domain → Application → Infrastructure → CLI
5. **No Shortcuts** - All 162+ FRs, all 218+ ACs must be implemented

### Implementation Order

The subtasks must be implemented in dependency order:

```
Task 008a (Foundation: Domain Models)
    ↓
Task 008b (Infrastructure: Loader/Validator/Registry)
    ↓
Task 008c (Content: Starter Packs)
```

**Rationale:**
- 008a defines domain models (PackManifest, ContentHash, PromptPack) needed by 008b
- 008b implements loader/validator needed to validate packs in 008c
- 008c creates actual pack content using formats from 008a and tooling from 008b

---

## Phase 1: Task 008a - File Layout, Hashing, Versioning

### Phase 1.1: Domain Value Objects

#### 1.1.1: ContentHash Value Object
- [ ] **Test:** ContentHashTests - Constructor validates format (64 hex chars)
- [ ] **Impl:** ContentHash record with validation
- [ ] **Test:** ContentHashTests - Equality comparison works
- [ ] **Impl:** Equality methods
- [ ] **Test:** ContentHashTests - ToString returns lowercase hex
- [ ] **Impl:** ToString
- [ ] **Commit:** "feat(task-008a): implement ContentHash value object"

#### 1.1.2: PackVersion Value Object
- [ ] **Test:** PackVersionTests - Parse SemVer 2.0 format
- [ ] **Impl:** PackVersion.Parse(string)
- [ ] **Test:** PackVersionTests - Comparison operators work
- [ ] **Impl:** IComparable<PackVersion>
- [ ] **Test:** PackVersionTests - Pre-release versions supported
- [ ] **Impl:** Pre-release parsing
- [ ] **Test:** PackVersionTests - Build metadata supported
- [ ] **Impl:** Build metadata parsing
- [ ] **Commit:** "feat(task-008a): implement PackVersion value object with SemVer 2.0"

#### 1.1.3: ComponentType Enum
- [ ] **Test:** ComponentTypeTests - All types defined
- [ ] **Impl:** ComponentType enum (System, Role, Language, Framework, Custom)
- [ ] **Test:** ComponentTypeTests - Validation works
- [ ] **Impl:** Validation extension methods
- [ ] **Commit:** "feat(task-008a): implement ComponentType enum"

#### 1.1.4: PackSource Enum
- [ ] **Test:** PackSourceTests - BuiltIn and User types defined
- [ ] **Impl:** PackSource enum
- [ ] **Commit:** "feat(task-008a): implement PackSource enum"

### Phase 1.2: Domain Models

#### 1.2.1: PackComponent Record
- [ ] **Test:** PackComponentTests - Constructor requires path and type
- [ ] **Impl:** PackComponent record (Path, Type, Role?, Language?, Framework?, Content)
- [ ] **Test:** PackComponentTests - Immutability verified
- [ ] **Impl:** Init-only properties
- [ ] **Test:** PackComponentTests - Metadata fields work
- [ ] **Impl:** Role, Language, Framework properties
- [ ] **Commit:** "feat(task-008a): implement PackComponent domain model"

#### 1.2.2: PackManifest Record
- [ ] **Test:** PackManifestTests - All required fields present
- [ ] **Impl:** PackManifest record (FormatVersion, Id, Version, Name, Description, ContentHash, CreatedAt, UpdatedAt?, Author?, Components)
- [ ] **Test:** PackManifestTests - Immutability verified
- [ ] **Impl:** Init-only properties
- [ ] **Test:** PackManifestTests - Id format validation (lowercase, hyphens)
- [ ] **Impl:** Id validation in constructor or validator
- [ ] **Test:** PackManifestTests - FormatVersion must be "1.0"
- [ ] **Impl:** FormatVersion validation
- [ ] **Commit:** "feat(task-008a): implement PackManifest domain model"

#### 1.2.3: PromptPack Record
- [ ] **Test:** PromptPackTests - Constructor requires manifest
- [ ] **Impl:** PromptPack record (Manifest, Components, Source)
- [ ] **Test:** PromptPackTests - Components dictionary keyed by path
- [ ] **Impl:** IReadOnlyDictionary<string, PackComponent>
- [ ] **Test:** PromptPackTests - Source defaults to User
- [ ] **Impl:** Source property with default
- [ ] **Commit:** "feat(task-008a): implement PromptPack domain model"

### Phase 1.3: Path Handling and Security

#### 1.3.1: PathNormalizer Utility
- [ ] **Test:** PathNormalizerTests - Normalize converts backslash to forward slash
- [ ] **Impl:** PathNormalizer.Normalize(string path)
- [ ] **Test:** PathNormalizerTests - Detects "../" traversal
- [ ] **Impl:** Traversal detection
- [ ] **Test:** PathNormalizerTests - Detects absolute paths
- [ ] **Impl:** Absolute path detection
- [ ] **Test:** PathNormalizerTests - Cross-platform stable
- [ ] **Impl:** Cross-platform normalization
- [ ] **Commit:** "feat(task-008a): implement PathNormalizer with traversal detection"

#### 1.3.2: PathTraversalException
- [ ] **Test:** PathTraversalExceptionTests - Constructor accepts attempted path
- [ ] **Impl:** PathTraversalException with message and attempted path
- [ ] **Test:** PathTraversalExceptionTests - Message includes attempted path
- [ ] **Impl:** Override Message property
- [ ] **Commit:** "feat(task-008a): implement PathTraversalException"

### Phase 1.4: Content Hashing

#### 1.4.1: IContentHasher Interface
- [ ] **Test:** (Will test via fake implementation initially)
- [ ] **Impl:** IContentHasher interface in Application layer
  ```csharp
  public interface IContentHasher
  {
      ContentHash Compute(IReadOnlyDictionary<string, string> components);
      bool Verify(IReadOnlyDictionary<string, string> components, ContentHash expectedHash);
  }
  ```
- [ ] **Commit:** "feat(task-008a): define IContentHasher interface"

#### 1.4.2: ContentHasher Implementation
- [ ] **Test:** ContentHasherTests - Compute returns SHA-256 hash
- [ ] **Impl:** ContentHasher.Compute using SHA256
- [ ] **Test:** ContentHasherTests - Deterministic (same input = same hash)
- [ ] **Impl:** Deterministic computation (sorted paths, LF line endings, UTF-8)
- [ ] **Test:** ContentHasherTests - Cross-platform stable
- [ ] **Impl:** Normalize line endings, sort paths
- [ ] **Test:** ContentHasherTests - Verify returns true for matching hash
- [ ] **Impl:** Verify method
- [ ] **Test:** ContentHasherTests - Verify returns false for mismatched hash
- [ ] **Impl:** Verification logic
- [ ] **Commit:** "feat(task-008a): implement ContentHasher with deterministic SHA-256"

### Phase 1.5: Manifest Schema Validation

#### 1.5.1: ManifestSchemaValidator
- [ ] **Test:** ManifestSchemaValidatorTests - Validates required fields present
- [ ] **Impl:** ManifestSchemaValidator.Validate(PackManifest)
- [ ] **Test:** ManifestSchemaValidatorTests - Validates id format (lowercase, hyphens)
- [ ] **Impl:** Id format validation
- [ ] **Test:** ManifestSchemaValidatorTests - Validates version is SemVer
- [ ] **Impl:** Version validation
- [ ] **Test:** ManifestSchemaValidatorTests - Validates component paths no traversal
- [ ] **Impl:** Path traversal check
- [ ] **Test:** ManifestSchemaValidatorTests - Validates content_hash format
- [ ] **Impl:** Hash format validation
- [ ] **Commit:** "feat(task-008a): implement ManifestSchemaValidator"

### Phase 1.6: Integration Tests for Task 008a

#### 1.6.1: End-to-End Pack Structure Tests
- [ ] **Test:** Create valid pack directory structure on disk
- [ ] **Test:** Parse manifest.yml
- [ ] **Test:** Compute content hash
- [ ] **Test:** Verify hash matches manifest
- [ ] **Test:** Detect path traversal attempts
- [ ] **Commit:** "test(task-008a): add integration tests for pack structure"

#### 1.6.2: Cross-Platform Path Tests
- [ ] **Test:** Paths normalize correctly on Windows
- [ ] **Test:** Paths normalize correctly on Linux
- [ ] **Test:** Hashes identical cross-platform
- [ ] **Commit:** "test(task-008a): add cross-platform path tests"

### Task 008a Completion Checklist

- [ ] All 63 acceptance criteria verified
- [ ] All domain models implemented and tested
- [ ] All value objects implemented and tested
- [ ] Path handling secure (no traversal)
- [ ] Content hashing deterministic and cross-platform
- [ ] Build succeeds with zero warnings
- [ ] All tests pass (100%)
- [ ] Code coverage > 90% for domain layer
- [ ] XML documentation complete
- [ ] Commit pushed to feature branch
- [ ] **Task 008a marked COMPLETE**

---

## Phase 2: Task 008b - Loader, Validator, Selection

### Phase 2.1: Validation Infrastructure

#### 2.1.1: ValidationResult Record
- [ ] **Test:** ValidationResultTests - Constructor with IsValid flag
- [ ] **Impl:** ValidationResult record (IsValid, Errors)
- [ ] **Test:** ValidationResultTests - Success factory method
- [ ] **Impl:** ValidationResult.Success()
- [ ] **Test:** ValidationResultTests - Failure factory method
- [ ] **Impl:** ValidationResult.Failure(errors)
- [ ] **Commit:** "feat(task-008b): implement ValidationResult"

#### 2.1.2: ValidationError Record
- [ ] **Test:** ValidationErrorTests - Constructor with code and message
- [ ] **Impl:** ValidationError record (Code, Message, Path?, Severity)
- [ ] **Test:** ValidationErrorTests - Severity levels work
- [ ] **Impl:** Severity enum (Error, Warning, Info)
- [ ] **Commit:** "feat(task-008b): implement ValidationError"

#### 2.1.3: IPackValidator Interface
- [ ] **Test:** (Will test via fake implementation)
- [ ] **Impl:** IPackValidator interface in Application layer
  ```csharp
  public interface IPackValidator
  {
      ValidationResult Validate(PromptPack pack);
      ValidationResult ValidateManifest(PackManifest manifest);
  }
  ```
- [ ] **Commit:** "feat(task-008b): define IPackValidator interface"

#### 2.1.4: PackValidator Implementation
- [ ] **Test:** PackValidatorTests - Validates manifest required fields
- [ ] **Impl:** PackValidator with manifest validation
- [ ] **Test:** PackValidatorTests - Validates component files referenced exist
- [ ] **Impl:** File existence checks
- [ ] **Test:** PackValidatorTests - Validates total size under 5MB limit
- [ ] **Impl:** Size limit check
- [ ] **Test:** PackValidatorTests - Validates template variable syntax
- [ ] **Impl:** Template syntax validation
- [ ] **Test:** PackValidatorTests - Completes in < 100ms
- [ ] **Impl:** Performance optimization if needed
- [ ] **Commit:** "feat(task-008b): implement PackValidator with all checks"

### Phase 2.2: Pack Loading Infrastructure

#### 2.2.1: IPromptPackLoader Interface
- [ ] **Test:** (Will test via fake implementation)
- [ ] **Impl:** IPromptPackLoader interface in Application layer
  ```csharp
  public interface IPromptPackLoader
  {
      Task<PromptPack> LoadPackAsync(string path, CancellationToken ct = default);
      Task<PromptPack> LoadBuiltInPackAsync(string id, CancellationToken ct = default);
      Task<PromptPack> LoadUserPackAsync(string path, CancellationToken ct = default);
  }
  ```
- [ ] **Commit:** "feat(task-008b): define IPromptPackLoader interface"

#### 2.2.2: ManifestParser Implementation
- [ ] **Test:** ManifestParserTests - Parses valid YAML
- [ ] **Impl:** ManifestParser using YamlDotNet
- [ ] **Test:** ManifestParserTests - Throws on invalid YAML
- [ ] **Impl:** Error handling
- [ ] **Test:** ManifestParserTests - Handles missing required fields
- [ ] **Impl:** Missing field handling
- [ ] **Commit:** "feat(task-008b): implement ManifestParser with YamlDotNet"

#### 2.2.3: ComponentReader Implementation
- [ ] **Test:** ComponentReaderTests - Reads .md files as UTF-8
- [ ] **Impl:** ComponentReader
- [ ] **Test:** ComponentReaderTests - Handles missing files gracefully
- [ ] **Impl:** Error handling
- [ ] **Test:** ComponentReaderTests - Handles encoding issues
- [ ] **Impl:** Encoding fallback logic
- [ ] **Commit:** "feat(task-008b): implement ComponentReader"

#### 2.2.4: PromptPackLoader Implementation
- [ ] **Test:** PromptPackLoaderTests - LoadPackAsync reads manifest
- [ ] **Impl:** PromptPackLoader.LoadPackAsync
- [ ] **Test:** PromptPackLoaderTests - Loads all component files
- [ ] **Impl:** Component loading
- [ ] **Test:** PromptPackLoaderTests - Verifies content hash (warns on mismatch)
- [ ] **Impl:** Hash verification
- [ ] **Test:** PromptPackLoaderTests - Rejects path traversal
- [ ] **Impl:** Path validation
- [ ] **Test:** PromptPackLoaderTests - LoadBuiltInPackAsync reads from embedded resources
- [ ] **Impl:** Embedded resource reading
- [ ] **Test:** PromptPackLoaderTests - LoadUserPackAsync reads from filesystem
- [ ] **Impl:** Filesystem reading
- [ ] **Commit:** "feat(task-008b): implement PromptPackLoader with embedded and filesystem support"

### Phase 2.3: Pack Registry and Discovery

#### 2.3.1: IPromptPackRegistry Interface
- [ ] **Test:** (Will test via fake implementation)
- [ ] **Impl:** IPromptPackRegistry interface in Application layer
  ```csharp
  public interface IPromptPackRegistry
  {
      Task<PromptPack> GetPackAsync(string id, CancellationToken ct = default);
      Task<PromptPack?> TryGetPackAsync(string id, CancellationToken ct = default);
      Task<PromptPack> GetActivePackAsync(CancellationToken ct = default);
      Task<IReadOnlyList<PackInfo>> ListPacksAsync(CancellationToken ct = default);
      Task RefreshAsync(CancellationToken ct = default);
  }
  ```
- [ ] **Commit:** "feat(task-008b): define IPromptPackRegistry interface"

#### 2.3.2: PackDiscovery Implementation
- [ ] **Test:** PackDiscoveryTests - Discovers built-in packs from embedded resources
- [ ] **Impl:** PackDiscovery with embedded resource scanning
- [ ] **Test:** PackDiscoveryTests - Discovers user packs from .acode/prompts/
- [ ] **Impl:** Filesystem scanning
- [ ] **Test:** PackDiscoveryTests - Skips invalid packs (logs warning)
- [ ] **Impl:** Error handling and logging
- [ ] **Test:** PackDiscoveryTests - Handles permission errors gracefully
- [ ] **Impl:** Permission error handling
- [ ] **Commit:** "feat(task-008b): implement PackDiscovery"

#### 2.3.3: PackCache Implementation
- [ ] **Test:** PackCacheTests - Thread-safe concurrent access
- [ ] **Impl:** PackCache using ConcurrentDictionary
- [ ] **Test:** PackCacheTests - Cache key is ID + hash
- [ ] **Impl:** Cache keying logic
- [ ] **Test:** PackCacheTests - Invalidation works
- [ ] **Impl:** Invalidation methods
- [ ] **Commit:** "feat(task-008b): implement PackCache with thread-safety"

#### 2.3.4: PromptPackRegistry Implementation
- [ ] **Test:** PromptPackRegistryTests - GetPackAsync returns by ID
- [ ] **Impl:** PromptPackRegistry.GetPackAsync
- [ ] **Test:** PromptPackRegistryTests - User pack overrides built-in with same ID
- [ ] **Impl:** Override logic
- [ ] **Test:** PromptPackRegistryTests - GetActivePackAsync uses configuration
- [ ] **Impl:** Active pack selection
- [ ] **Test:** PromptPackRegistryTests - ListPacksAsync returns all available
- [ ] **Impl:** List all packs
- [ ] **Test:** PromptPackRegistryTests - RefreshAsync reloads all packs
- [ ] **Impl:** Refresh logic
- [ ] **Commit:** "feat(task-008b): implement PromptPackRegistry with discovery and caching"

### Phase 2.4: Configuration Integration

#### 2.4.1: PackConfiguration Implementation
- [ ] **Test:** PackConfigurationTests - Reads prompts.pack_id from .agent/config.yml
- [ ] **Impl:** PackConfiguration using IConfigLoader (Task 002)
- [ ] **Test:** PackConfigurationTests - Environment override ACODE_PROMPT_PACK takes precedence
- [ ] **Impl:** Environment variable override
- [ ] **Test:** PackConfigurationTests - Default is "acode-standard"
- [ ] **Impl:** Default fallback
- [ ] **Test:** PackConfigurationTests - Logs active pack selection
- [ ] **Impl:** Logging
- [ ] **Commit:** "feat(task-008b): implement PackConfiguration with env var override"

### Phase 2.5: CLI Commands

#### 2.5.1: PromptListCommand
- [ ] **Test:** PromptListCommandTests - Displays table of all packs
- [ ] **Impl:** PromptListCommand with table formatting
- [ ] **Test:** PromptListCommandTests - Shows id, version, source, active flag
- [ ] **Impl:** Column data
- [ ] **Test:** PromptListCommandTests - --show-overrides flag shows overridden built-ins
- [ ] **Impl:** Override detection and display
- [ ] **Commit:** "feat(task-008b): implement 'acode prompts list' command"

#### 2.5.2: PromptShowCommand
- [ ] **Test:** PromptShowCommandTests - Shows pack details
- [ ] **Impl:** PromptShowCommand
- [ ] **Test:** PromptShowCommandTests - Shows component list
- [ ] **Impl:** Component listing
- [ ] **Commit:** "feat(task-008b): implement 'acode prompts show' command"

#### 2.5.3: PromptValidateCommand
- [ ] **Test:** PromptValidateCommandTests - Validates pack at path
- [ ] **Impl:** PromptValidateCommand
- [ ] **Test:** PromptValidateCommandTests - Outputs all validation errors
- [ ] **Impl:** Error formatting
- [ ] **Test:** PromptValidateCommandTests - Exit 0 on valid, 1 on invalid
- [ ] **Impl:** Exit code logic
- [ ] **Commit:** "feat(task-008b): implement 'acode prompts validate' command"

#### 2.5.4: PromptReloadCommand
- [ ] **Test:** PromptReloadCommandTests - Calls registry.RefreshAsync()
- [ ] **Impl:** PromptReloadCommand
- [ ] **Test:** PromptReloadCommandTests - Logs reload completion
- [ ] **Impl:** Logging
- [ ] **Commit:** "feat(task-008b): implement 'acode prompts reload' command"

### Phase 2.6: Integration Tests for Task 008b

#### 2.6.1: End-to-End Load Tests
- [ ] **Test:** Load real pack from filesystem
- [ ] **Test:** Load built-in pack from embedded resources
- [ ] **Test:** Validate pack
- [ ] **Test:** Cache pack
- [ ] **Test:** Reload after modification
- [ ] **Commit:** "test(task-008b): add end-to-end load tests"

#### 2.6.2: Configuration Integration Tests
- [ ] **Test:** Select pack via .agent/config.yml
- [ ] **Test:** Override via ACODE_PROMPT_PACK environment variable
- [ ] **Test:** Fallback to default when configured pack not found
- [ ] **Commit:** "test(task-008b): add configuration integration tests"

### Task 008b Completion Checklist

- [ ] All 80 acceptance criteria verified
- [ ] Loader implemented and tested
- [ ] Validator implemented and tested
- [ ] Registry implemented and tested
- [ ] Configuration integration works
- [ ] All CLI commands implemented
- [ ] Build succeeds with zero warnings
- [ ] All tests pass (100%)
- [ ] Code coverage > 80% for infrastructure layer
- [ ] XML documentation complete
- [ ] Commit pushed to feature branch
- [ ] **Task 008b marked COMPLETE**

---

## Phase 3: Task 008c - Starter Packs

### Phase 3.1: acode-standard Pack

#### 3.1.1: Pack Structure
- [ ] **Create:** Resources/PromptPacks/acode-standard/ directory structure
- [ ] **Create:** manifest.yml with id "acode-standard", version 1.0.0
- [ ] **Commit:** "feat(task-008c): create acode-standard pack structure"

#### 3.1.2: system.md
- [ ] **Write:** Base system prompt (~200 lines)
  - Agent identity and purpose
  - Strict minimal diff principle
  - Capabilities and limitations
  - Template variables: {{workspace_name}}, {{language}}, {{framework}}, {{date}}
- [ ] **Test:** Validate syntax and template variables
- [ ] **Commit:** "feat(task-008c): add system.md for acode-standard"

#### 3.1.3: Role Prompts
- [ ] **Write:** roles/planner.md (~80 lines)
  - Strategic planning guidance
  - Dependency identification
  - Task breakdown
- [ ] **Write:** roles/coder.md (~120 lines)
  - TDD enforcement
  - Clean code principles
  - Strict minimal diff
- [ ] **Write:** roles/reviewer.md (~100 lines)
  - Security analysis
  - Code quality checks
  - Bug detection
- [ ] **Test:** Validate all role prompts
- [ ] **Commit:** "feat(task-008c): add role prompts for acode-standard"

#### 3.1.4: Content Hash and Validation
- [ ] **Generate:** Content hash using CLI tool
- [ ] **Update:** manifest.yml with hash
- [ ] **Test:** Load pack and verify hash
- [ ] **Test:** Validate pack passes all checks
- [ ] **Test:** Token count < 4000 for system.md
- [ ] **Test:** Token count < 2000 for each role prompt
- [ ] **Commit:** "feat(task-008c): finalize acode-standard pack with hash"

### Phase 3.2: acode-dotnet Pack

#### 3.2.1: Pack Structure
- [ ] **Create:** Resources/PromptPacks/acode-dotnet/ directory structure
- [ ] **Copy:** All files from acode-standard as base
- [ ] **Create:** manifest.yml with id "acode-dotnet", version 1.0.0, 6 components
- [ ] **Commit:** "feat(task-008c): create acode-dotnet pack structure"

#### 3.2.2: Language-Specific Prompt
- [ ] **Write:** languages/csharp.md (~180 lines)
  - PascalCase naming conventions
  - Async/await patterns (ConfigureAwait(false))
  - Nullable reference types
  - Dependency injection patterns
  - LINQ guidance
  - XML documentation requirements
- [ ] **Test:** Validate csharp.md
- [ ] **Test:** Token count < 2000
- [ ] **Commit:** "feat(task-008c): add csharp.md for acode-dotnet"

#### 3.2.3: Framework-Specific Prompt
- [ ] **Write:** frameworks/aspnetcore.md (~150 lines)
  - Controller patterns
  - Middleware guidance
  - EF Core conventions
  - API versioning
  - Minimal APIs
  - Dependency injection
- [ ] **Test:** Validate aspnetcore.md
- [ ] **Test:** Token count < 1000
- [ ] **Commit:** "feat(task-008c): add aspnetcore.md for acode-dotnet"

#### 3.2.4: Content Hash and Validation
- [ ] **Generate:** Content hash
- [ ] **Update:** manifest.yml with hash
- [ ] **Test:** Load pack and verify hash
- [ ] **Test:** Validate pack passes all checks
- [ ] **Test:** Composition works (system + role + csharp + aspnetcore)
- [ ] **Commit:** "feat(task-008c): finalize acode-dotnet pack"

### Phase 3.3: acode-react Pack

#### 3.3.1: Pack Structure
- [ ] **Create:** Resources/PromptPacks/acode-react/ directory structure
- [ ] **Copy:** All files from acode-standard as base
- [ ] **Create:** manifest.yml with id "acode-react", version 1.0.0, 6 components
- [ ] **Commit:** "feat(task-008c): create acode-react pack structure"

#### 3.3.2: Language-Specific Prompt
- [ ] **Write:** languages/typescript.md (~120 lines)
  - Strict mode enforcement
  - Type safety
  - Interface vs type
  - Generics patterns
  - Utility types
- [ ] **Test:** Validate typescript.md
- [ ] **Test:** Token count < 2000
- [ ] **Commit:** "feat(task-008c): add typescript.md for acode-react"

#### 3.3.3: Framework-Specific Prompt
- [ ] **Write:** frameworks/react.md (~180 lines)
  - Functional components only
  - Hooks patterns (useState, useEffect, useCallback, useMemo)
  - useEffect cleanup
  - Component composition
  - Props destructuring
  - Key props for lists
  - Performance optimization
- [ ] **Test:** Validate react.md
- [ ] **Test:** Token count < 2000
- [ ] **Commit:** "feat(task-008c): add react.md for acode-react"

#### 3.3.4: Content Hash and Validation
- [ ] **Generate:** Content hash
- [ ] **Update:** manifest.yml with hash
- [ ] **Test:** Load pack and verify hash
- [ ] **Test:** Validate pack passes all checks
- [ ] **Test:** Composition works (system + role + typescript + react)
- [ ] **Commit:** "feat(task-008c): finalize acode-react pack"

### Phase 3.4: Embedded Resource Configuration

#### 3.4.1: Resource Embedding
- [ ] **Update:** Acode.Infrastructure.csproj to embed packs as resources
  ```xml
  <ItemGroup>
    <EmbeddedResource Include="Resources\PromptPacks\**\*" />
  </ItemGroup>
  ```
- [ ] **Test:** EmbeddedPackProviderTests - Can read embedded resources
- [ ] **Test:** All 3 packs loadable from embedded resources
- [ ] **Commit:** "feat(task-008c): configure embedded resources for starter packs"

### Phase 3.5: Quality Verification Tests

#### 3.5.1: Validation Tests
- [ ] **Test:** All 3 packs validate successfully
- [ ] **Test:** Content hashes match
- [ ] **Test:** All template variables valid
- [ ] **Commit:** "test(task-008c): add validation tests for all starter packs"

#### 3.5.2: Composition Tests
- [ ] **Test:** acode-standard composes correctly for each role
- [ ] **Test:** acode-dotnet includes csharp and aspnetcore components
- [ ] **Test:** acode-react includes typescript and react components
- [ ] **Test:** Template variable substitution works
- [ ] **Commit:** "test(task-008c): add composition tests for all starter packs"

#### 3.5.3: Token Count Tests
- [ ] **Test:** acode-standard system.md < 4000 tokens
- [ ] **Test:** All role prompts < 2000 tokens
- [ ] **Test:** All language/framework prompts within limits
- [ ] **Commit:** "test(task-008c): add token count verification tests"

### Task 008c Completion Checklist

- [ ] All 75+ acceptance criteria verified
- [ ] acode-standard pack complete (system + 3 roles)
- [ ] acode-dotnet pack complete (standard + csharp + aspnetcore)
- [ ] acode-react pack complete (standard + typescript + react)
- [ ] All packs embedded as resources
- [ ] All packs validate successfully
- [ ] All composition tests pass
- [ ] Token limits respected
- [ ] Build succeeds with zero warnings
- [ ] All tests pass (100%)
- [ ] XML documentation complete
- [ ] Commit pushed to feature branch
- [ ] **Task 008c marked COMPLETE**

---

## Phase 4: Parent Task 008 - Composition Engine

### Phase 4.1: Template Engine

#### 4.1.1: ITemplateEngine Interface
- [ ] **Test:** (Will test via fake implementation)
- [ ] **Impl:** ITemplateEngine interface in Application layer
  ```csharp
  public interface ITemplateEngine
  {
      string Substitute(string template, Dictionary<string, string> variables);
      ValidationResult ValidateTemplate(string template);
  }
  ```
- [ ] **Commit:** "feat(task-008): define ITemplateEngine interface"

#### 4.1.2: TemplateEngine Implementation
- [ ] **Test:** TemplateEngineTests - Substitute single variable
- [ ] **Impl:** TemplateEngine.Substitute (Mustache-style)
- [ ] **Test:** TemplateEngineTests - Substitute multiple variables
- [ ] **Impl:** Multiple variable support
- [ ] **Test:** TemplateEngineTests - Missing variable becomes empty string
- [ ] **Impl:** Missing variable handling
- [ ] **Test:** TemplateEngineTests - Escape special characters in values
- [ ] **Impl:** HTML/Markdown escaping
- [ ] **Test:** TemplateEngineTests - Reject variable value exceeding 1024 chars
- [ ] **Impl:** Length validation
- [ ] **Test:** TemplateEngineTests - Variable resolution priority (config > env > context > default)
- [ ] **Impl:** Priority resolution
- [ ] **Test:** TemplateEngineTests - Detect recursive expansion
- [ ] **Impl:** Recursion detection (max depth 3)
- [ ] **Commit:** "feat(task-008): implement TemplateEngine with Mustache syntax"

### Phase 4.2: Prompt Composer

#### 4.2.1: CompositionContext Record
- [ ] **Test:** CompositionContextTests - Constructor with required fields
- [ ] **Impl:** CompositionContext record (Role?, Language?, Framework?, Variables)
- [ ] **Test:** CompositionContextTests - Variables dictionary works
- [ ] **Impl:** Variables as Dictionary<string, string>
- [ ] **Commit:** "feat(task-008): implement CompositionContext"

#### 4.2.2: IPromptComposer Interface
- [ ] **Test:** (Will test via fake implementation)
- [ ] **Impl:** IPromptComposer interface in Application layer
  ```csharp
  public interface IPromptComposer
  {
      Task<string> ComposeAsync(PromptPack pack, CompositionContext context, CancellationToken ct = default);
  }
  ```
- [ ] **Commit:** "feat(task-008): define IPromptComposer interface"

#### 4.2.3: PromptComposer Implementation
- [ ] **Test:** PromptComposerTests - Compose base system prompt only
- [ ] **Impl:** PromptComposer.ComposeAsync (base)
- [ ] **Test:** PromptComposerTests - Compose base + role prompt
- [ ] **Impl:** Role component merging
- [ ] **Test:** PromptComposerTests - Compose full stack (system + role + language + framework)
- [ ] **Impl:** Full hierarchical merging
- [ ] **Test:** PromptComposerTests - Skip optional missing components
- [ ] **Impl:** Optional component handling
- [ ] **Test:** PromptComposerTests - Deduplicate repeated sections
- [ ] **Impl:** Deduplication logic
- [ ] **Test:** PromptComposerTests - Enforce maximum prompt length (32,000 chars)
- [ ] **Impl:** Length enforcement with truncation
- [ ] **Test:** PromptComposerTests - Log composition hash
- [ ] **Impl:** Logging
- [ ] **Commit:** "feat(task-008): implement PromptComposer with hierarchical merging"

### Phase 4.3: Integration with Task 004 (Model Provider)

#### 4.3.1: Composition Integration Point
- [ ] **Test:** Integration test - Compose prompt and pass to IModelProvider
- [ ] **Impl:** Integration wiring (if Task 004 exists)
- [ ] **Commit:** "feat(task-008): integrate PromptComposer with Model Provider Interface"

### Task 008 Parent Completion Checklist

- [ ] All acceptance criteria verified
- [ ] Template engine implemented and tested
- [ ] Prompt composer implemented and tested
- [ ] Integration with Model Provider works
- [ ] Build succeeds with zero warnings
- [ ] All tests pass (100%)
- [ ] XML documentation complete
- [ ] Commit pushed to feature branch
- [ ] **Task 008 Parent marked COMPLETE**

---

## Phase 5: Final Audit and PR

### Phase 5.1: Comprehensive Audit

#### 5.1.1: Subtask Verification
- [ ] **Verify:** Task 008a COMPLETE (all 63 ACs)
- [ ] **Verify:** Task 008b COMPLETE (all 80 ACs)
- [ ] **Verify:** Task 008c COMPLETE (all 75+ ACs)
- [ ] **Verify:** Task 008 parent COMPLETE (all ACs)
- [ ] **Document:** Create TASK-008-AUDIT.md

#### 5.1.2: TDD Compliance Audit
- [ ] **Verify:** Every source file has tests
- [ ] **Verify:** Domain layer > 90% coverage
- [ ] **Verify:** Application layer > 80% coverage
- [ ] **Verify:** Infrastructure layer > 80% coverage
- [ ] **Verify:** No NotImplementedException in production code

#### 5.1.3: Code Quality Audit
- [ ] **Verify:** Build succeeds with zero errors and zero warnings
- [ ] **Verify:** All public APIs have XML documentation
- [ ] **Verify:** Async/await patterns correct (ConfigureAwait(false))
- [ ] **Verify:** Null handling correct (ArgumentNullException.ThrowIfNull)
- [ ] **Verify:** Resource disposal correct (using statements)

#### 5.1.4: Layer Boundary Audit
- [ ] **Verify:** Domain has no Infrastructure dependencies
- [ ] **Verify:** Application only references Domain
- [ ] **Verify:** Infrastructure implements Application interfaces
- [ ] **Verify:** No circular dependencies

#### 5.1.5: Integration Audit
- [ ] **Verify:** All 3 starter packs load correctly
- [ ] **Verify:** Pack selection via config works
- [ ] **Verify:** Environment override works
- [ ] **Verify:** User pack overrides built-in pack
- [ ] **Verify:** CLI commands all work
- [ ] **Verify:** Composition produces valid prompts

#### 5.1.6: Security Audit
- [ ] **Verify:** Path traversal prevention works
- [ ] **Verify:** Template variable injection prevented
- [ ] **Verify:** Content hash verification works
- [ ] **Verify:** Size limits enforced

### Phase 5.2: Documentation

#### 5.2.1: Update Implementation Plan
- [ ] **Update:** Mark all items as ✅ completed
- [ ] **Document:** Any deviations or issues encountered
- [ ] **Document:** Performance metrics if measured

#### 5.2.2: Create Audit Document
- [ ] **Create:** docs/TASK-008-AUDIT.md with evidence matrix
- [ ] **Include:** Build output (0 errors, 0 warnings)
- [ ] **Include:** Test output (X/X tests passed)
- [ ] **Include:** Coverage report
- [ ] **Include:** FR evidence matrix (file paths + line numbers)

### Phase 5.3: Pull Request

#### 5.3.1: Prepare PR
- [ ] **Verify:** All commits on feature branch
- [ ] **Verify:** Branch up to date with main
- [ ] **Verify:** All tests pass
- [ ] **Verify:** Build succeeds

#### 5.3.2: Create PR
- [ ] **Create:** Pull request with title "feat(epic-01): Task 008 - Prompt Pack System (008, 008a, 008b, 008c)"
- [ ] **Include:** Summary of implementation
- [ ] **Include:** Link to task specifications
- [ ] **Include:** Link to audit document
- [ ] **Include:** Test coverage summary

---

## Progress Tracking

### Current Status
- **Phase:** Not started
- **Current Item:** None
- **Blocker:** None
- **Next Action:** Create feature branch and begin Phase 1.1.1

### Metrics
- **Commits:** 0
- **Tests Written:** 0
- **Tests Passing:** 0
- **Code Coverage:** 0%
- **Files Created:** 0

### Recent Updates
- 2026-01-05: Implementation plan created

---

## Notes and Decisions

### Key Design Decisions
1. **Implementation order:** 008a → 008b → 008c → 008 (dependency-driven)
2. **Strict TDD:** Red → Green → Refactor for every single behavior
3. **One commit per logical unit:** Fine-grained commits for easy review
4. **Built-in packs as embedded resources:** Simplifies distribution

### Risks and Mitigations
| Risk | Mitigation |
|------|------------|
| Large scope (162+ FRs) | Phase-based approach, frequent commits |
| Context exhaustion | Detailed implementation plan for seamless resume |
| Integration complexity | Integration tests at end of each phase |
| Starter pack quality | Quality verification tests in Phase 3.5 |

### Dependencies on Other Tasks
- **Task 002 (Config System):** Needed for reading .agent/config.yml
- **Task 004 (Model Provider):** Consumes composed prompts
- **Task 007 (Tool Registry):** Future integration for tool guidance in prompts

---

## Resumption Instructions

If context runs out mid-implementation:

1. **Check:** Last completed item in progress tracking above
2. **Find:** Next unchecked item in implementation plan
3. **Review:** Corresponding section in task specifications (docs/tasks/refined-tasks/Epic 01/)
4. **Continue:** From next RED test in TDD cycle
5. **Update:** Progress tracking and metrics as you work

Remember: **Nothing is optional. All 162+ FRs, all 218+ ACs must be implemented.**
