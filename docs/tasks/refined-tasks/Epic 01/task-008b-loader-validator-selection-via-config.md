# Task 008.b: Loader/Validator + Selection via Config

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 008, Task 008.a, Task 002 (.agent/config.yml)  

---

## Description

Task 008.b implements the prompt pack loader, validator, and configuration-based selection mechanism. This subtask provides the runtime components that read pack files from disk, validate their structure and content, and select the appropriate pack based on user configuration. These components are the bridge between pack storage and prompt composition.

The loader reads pack files from disk and constructs in-memory representations. It parses the manifest, reads component files, and assembles a complete PromptPack object. The loader handles file I/O errors gracefully, providing clear error messages when packs cannot be loaded. It supports both built-in packs (from embedded resources) and user packs (from workspace directories).

The validator ensures packs are well-formed before use. Validation happens at load time, not during agent execution, to fail fast and provide actionable feedback. The validator checks manifest schema compliance, component file existence, template variable syntax, and size limits. Validation errors include specific file paths and line numbers where applicable.

Configuration-based selection determines which pack is active. The `.agent/config.yml` file specifies the pack ID to use. Environment variables can override the config file for deployment flexibility. When the specified pack is not found, the system falls back to the default pack with a warning. Selection is deterministic—the same configuration always selects the same pack.

The pack registry maintains an index of all available packs from all sources. It discovers packs on initialization and supports refresh for hot-reloading during development. The registry resolves pack IDs to pack instances, handling the priority rules (user packs override built-in packs with the same ID).

Integration with the Model Provider Interface (Task 004) occurs through the prompt composition system. When constructing chat requests, the system requests prompts from the active pack. The loader ensures the pack is ready for use—loaded, validated, and cached.

Caching optimizes repeated access. Once a pack is loaded and validated, it is cached in memory for the session duration. Cache invalidation occurs when packs are explicitly refreshed or when the configuration changes. The cache key includes the pack ID and content hash to detect modifications.

Error handling follows a layered approach. File-level errors (missing files, parse errors) are wrapped in pack-level errors with context. Pack-level errors include the pack ID and path for debugging. The loader never crashes—it returns Result types or throws well-typed exceptions that callers can handle.

Logging provides visibility into pack lifecycle. Pack loading is logged at INFO level with pack ID, version, and component count. Validation failures are logged at WARNING or ERROR level with specific error details. Hash mismatches are logged as warnings, not errors, to allow development workflows.

The loader supports the development workflow where users edit pack files and want changes reflected without restarting the agent. The CLI provides a reload command that refreshes the pack registry and reloads the active pack. Hot reload is opt-in—normal operation uses the cached pack.

Configuration precedence follows standard rules: environment variables override config file values, which override defaults. The default pack ID is "acode-standard". This precedence allows deployment customization without modifying workspace files.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Pack Loader | Component that reads pack from disk |
| Pack Validator | Component that checks pack validity |
| Pack Registry | Index of available packs |
| Pack Selector | Logic for choosing active pack |
| Active Pack | Currently selected pack for session |
| Pack Cache | In-memory store for loaded packs |
| Cache Key | Pack ID + content hash combination |
| Cache Invalidation | Clearing cached pack data |
| Hot Reload | Refreshing packs without restart |
| Fallback Pack | Default when specified not found |
| Configuration Precedence | Order: env > config > default |
| Validation Error | Pack validity check failure |
| Load Error | File I/O or parse failure |
| Result Type | Success/failure return type |
| Pack Discovery | Finding packs from all sources |
| Source Priority | User packs override built-in |

---

## Out of Scope

The following items are explicitly excluded from Task 008.b:

- **Pack file layout definition** - Covered in Task 008.a
- **Content hashing algorithm** - Covered in Task 008.a
- **Versioning scheme** - Covered in Task 008.a
- **Starter pack content** - Covered in Task 008.c
- **Prompt composition logic** - Covered in Task 008
- **Template variable substitution** - Covered in Task 008
- **Remote pack download** - Not in MVP
- **Pack dependency resolution** - Not in MVP
- **Automatic pack updates** - Not in MVP
- **Pack migration between versions** - Future enhancement

---

## Functional Requirements

### IPromptPackLoader Interface

- FR-001: Interface MUST be defined in Application layer
- FR-002: Interface MUST have LoadPack(string path) method
- FR-003: LoadPack MUST return PromptPack or error
- FR-004: Interface MUST have LoadBuiltInPack(string id) method
- FR-005: LoadBuiltInPack MUST read from embedded resources
- FR-006: Interface MUST have LoadUserPack(string path) method
- FR-007: LoadUserPack MUST read from file system

### PromptPackLoader Implementation

- FR-008: Loader MUST be in Infrastructure layer
- FR-009: Loader MUST parse manifest.yml first
- FR-010: Loader MUST validate manifest schema
- FR-011: Loader MUST load all component files
- FR-012: Loader MUST verify content hash
- FR-013: Hash mismatch MUST log warning, not fail
- FR-014: Loader MUST handle missing files gracefully
- FR-015: Loader MUST handle invalid YAML gracefully
- FR-016: Loader MUST handle encoding issues
- FR-017: Loader MUST normalize paths on read
- FR-018: Loader MUST reject path traversal attempts

### IPackValidator Interface

- FR-019: Interface MUST be defined in Application layer
- FR-020: Interface MUST have Validate(PromptPack) method
- FR-021: Validate MUST return ValidationResult
- FR-022: ValidationResult MUST contain list of errors
- FR-023: ValidationResult MUST have IsValid property
- FR-024: Each error MUST have code and message
- FR-025: Each error MUST have path if applicable

### PackValidator Implementation

- FR-026: Validator MUST check manifest required fields
- FR-027: Validator MUST check id format (lowercase, hyphens)
- FR-028: Validator MUST check version is SemVer
- FR-029: Validator MUST check component files exist
- FR-030: Validator MUST check component paths valid
- FR-031: Validator MUST check template variable syntax
- FR-032: Validator MUST check total size under limit
- FR-033: Size limit MUST be 5MB total
- FR-034: Validator MUST check for circular references
- FR-035: Validation MUST complete in < 100ms

### IPromptPackRegistry Interface

- FR-036: Interface MUST be defined in Application layer
- FR-037: Interface MUST have GetPack(string id) method
- FR-038: Interface MUST have ListPacks() method
- FR-039: Interface MUST have GetActivePack() method
- FR-040: Interface MUST have Refresh() method
- FR-041: Interface MUST have TryGetPack(string id) method

### PromptPackRegistry Implementation

- FR-042: Registry MUST discover built-in packs on init
- FR-043: Registry MUST discover user packs on init
- FR-044: Registry MUST index packs by ID
- FR-045: User packs MUST override built-in with same ID
- FR-046: GetPack MUST throw if ID not found
- FR-047: TryGetPack MUST return null if not found
- FR-048: ListPacks MUST return all available packs
- FR-049: GetActivePack MUST use configuration
- FR-050: Refresh MUST reload all packs

### Pack Selection via Configuration

- FR-051: Selection MUST read prompts.pack_id from config
- FR-052: Config path: .agent/config.yml
- FR-053: Default pack_id MUST be "acode-standard"
- FR-054: Environment override: ACODE_PROMPT_PACK
- FR-055: Env var takes precedence over config file
- FR-056: Config file takes precedence over default
- FR-057: Missing config section uses default
- FR-058: Invalid pack_id falls back to default
- FR-059: Fallback MUST log WARNING
- FR-060: Selected pack MUST be logged at INFO

### Pack Caching

- FR-061: Loaded packs MUST be cached in memory
- FR-062: Cache key MUST be pack ID + content hash
- FR-063: Cache hit MUST return same instance
- FR-064: Cache miss MUST trigger load
- FR-065: Refresh MUST invalidate cache
- FR-066: Config change MUST invalidate cache
- FR-067: Cache MUST be thread-safe

### Discovery

- FR-068: Built-in discovery path: embedded resources
- FR-069: User discovery path: {workspace}/.acode/prompts/
- FR-070: Discovery MUST scan for manifest.yml
- FR-071: Discovery MUST skip invalid packs
- FR-072: Discovery MUST log skipped packs
- FR-073: Discovery MUST handle permission errors

### CLI Integration

- FR-074: `acode prompts list` MUST show all packs
- FR-075: List MUST show: id, version, source, active flag
- FR-076: `acode prompts show <id>` MUST show details
- FR-077: Show MUST include component list
- FR-078: `acode prompts validate <path>` MUST validate
- FR-079: Validate MUST output all errors
- FR-080: Validate MUST exit 0 on valid, 1 on invalid
- FR-081: `acode prompts reload` MUST refresh registry
- FR-082: Reload MUST reload active pack

### Error Handling

- FR-083: LoadError MUST include pack path
- FR-084: LoadError MUST include original exception
- FR-085: ValidationError MUST include error code
- FR-086: ValidationError MUST include file path
- FR-087: NotFoundError MUST include searched paths

---

## Non-Functional Requirements

### Performance

- NFR-001: Pack loading MUST complete in < 100ms
- NFR-002: Validation MUST complete in < 100ms
- NFR-003: Registry init MUST complete in < 500ms
- NFR-004: Cache lookup MUST complete in < 1ms
- NFR-005: Memory per cached pack MUST be < 1MB

### Reliability

- NFR-006: Invalid pack MUST NOT crash agent
- NFR-007: Missing user directory MUST NOT error
- NFR-008: Concurrent access MUST be thread-safe
- NFR-009: Cache MUST survive pack reload

### Security

- NFR-010: Path traversal MUST be blocked
- NFR-011: Symlinks MUST be rejected
- NFR-012: User packs MUST be sandboxed
- NFR-013: File permissions MUST be checked

### Observability

- NFR-014: Pack loading MUST be logged
- NFR-015: Validation errors MUST be logged
- NFR-016: Cache hits/misses SHOULD be logged (DEBUG)
- NFR-017: Pack selection MUST be logged
- NFR-018: Fallback MUST be logged (WARNING)

### Maintainability

- NFR-019: All public APIs MUST have XML docs
- NFR-020: Error messages MUST be actionable
- NFR-021: Validation errors MUST be specific
- NFR-022: Tests MUST cover all error paths

---

## User Manual Documentation

### Overview

The prompt pack loader reads and validates packs from disk. The registry indexes available packs and provides the active pack based on configuration. This document covers loading, validation, selection, and troubleshooting.

### Quick Start

1. **List available packs:**
   ```bash
   $ acode prompts list
   ┌───────────────────────────────────────────────────────────────────┐
   │ Available Prompt Packs                                            │
   ├──────────────────┬─────────┬──────────┬──────────────────────────┤
   │ ID               │ Version │ Source   │ Status                    │
   ├──────────────────┼─────────┼──────────┼──────────────────────────┤
   │ acode-standard   │ 1.0.0   │ built-in │ active                    │
   │ acode-dotnet     │ 1.0.0   │ built-in │                           │
   │ acode-react      │ 1.0.0   │ built-in │                           │
   │ my-custom        │ 1.2.0   │ user     │                           │
   └──────────────────┴─────────┴──────────┴──────────────────────────┘
   ```

2. **Select a pack:**
   ```yaml
   # .agent/config.yml
   prompts:
     pack_id: acode-dotnet
   ```

3. **Verify selection:**
   ```bash
   $ acode prompts list
   ... acode-dotnet ... active
   ```

4. **Validate a custom pack:**
   ```bash
   $ acode prompts validate .acode/prompts/my-custom
   Validating pack 'my-custom'...
   ✓ Manifest valid
   ✓ Components found (5)
   ✓ Template variables valid
   ✓ Size within limits
   Pack 'my-custom' is valid
   ```

### Configuration

#### Config File Selection

```yaml
# .agent/config.yml
prompts:
  # Which pack to use (required)
  pack_id: acode-standard
  
  # Pack discovery paths (optional, defaults shown)
  discovery:
    user_path: .acode/prompts
    enable_builtin: true
```

#### Environment Override

```bash
# Override config file selection
export ACODE_PROMPT_PACK=my-custom

# Now any config is ignored
acode run  # Uses 'my-custom' pack
```

#### Precedence Rules

1. Environment variable `ACODE_PROMPT_PACK`
2. Config file `prompts.pack_id`
3. Default: `acode-standard`

### Loader Behavior

#### Loading Built-in Packs

Built-in packs are embedded in the application:
- `acode-standard` - General purpose
- `acode-dotnet` - .NET/C# development
- `acode-react` - React/TypeScript

These are always available and cannot be modified.

#### Loading User Packs

User packs are discovered in:
```
{workspace}/.acode/prompts/{pack-id}/
```

User packs with the same ID as built-in packs take precedence.

#### Hash Verification

When loading, the loader verifies content hash:
```
[INFO] Loading pack 'my-custom' v1.2.0
[INFO] Verifying content hash...
[WARN] Content hash mismatch - pack may have been modified
[INFO] Pack loaded successfully (5 components)
```

Hash mismatch is a warning, not an error, to support development.

### Validation

#### Automatic Validation

Packs are validated when loaded:
- Manifest schema
- Component file existence
- Template variable syntax
- Size limits

#### Manual Validation

```bash
# Validate specific pack
$ acode prompts validate .acode/prompts/my-custom

# Validate with verbose output
$ acode prompts validate .acode/prompts/my-custom --verbose
```

#### Validation Errors

```
$ acode prompts validate .acode/prompts/bad-pack
Validating pack 'bad-pack'...
✗ Manifest error: missing required field 'description'
✗ Component not found: roles/analyst.md
✗ Invalid template variable: {{unknown_var}} in system.md
✗ Pack exceeds size limit (5.2MB > 5MB)

Pack 'bad-pack' has 4 errors
```

#### Error Codes

| Code | Description |
|------|-------------|
| VAL-001 | Missing required manifest field |
| VAL-002 | Invalid pack ID format |
| VAL-003 | Invalid version format |
| VAL-004 | Component file not found |
| VAL-005 | Invalid template variable |
| VAL-006 | Pack exceeds size limit |
| VAL-007 | Invalid component path |
| VAL-008 | Circular reference detected |

### Registry Operations

#### Listing Packs

```bash
$ acode prompts list

# With details
$ acode prompts list --verbose
```

#### Showing Pack Details

```bash
$ acode prompts show acode-standard
Pack: acode-standard
Version: 1.0.0
Source: built-in
Description: General purpose coding assistant prompts

Components:
  - system.md (system)
  - roles/planner.md (role: planner)
  - roles/coder.md (role: coder)
  - roles/reviewer.md (role: reviewer)
  - languages/csharp.md (language: csharp)
  - languages/typescript.md (language: typescript)
  ...
```

#### Refreshing Registry

```bash
# Reload all packs (for development)
$ acode prompts reload
Refreshing pack registry...
Found 4 packs (3 built-in, 1 user)
Active pack: acode-dotnet v1.0.0
```

### Fallback Behavior

When the configured pack is not found:

```yaml
# .agent/config.yml
prompts:
  pack_id: nonexistent-pack  # Does not exist
```

```
[WARN] Pack 'nonexistent-pack' not found, falling back to 'acode-standard'
[INFO] Active pack: acode-standard v1.0.0
```

### Caching

Packs are cached after first load:
- Cache key: pack ID + content hash
- Cache duration: session lifetime
- Cache invalidation: on refresh or config change

```bash
# Force cache refresh
$ acode prompts reload
```

### Troubleshooting

#### Pack Not Found

```
Error: Pack 'my-pack' not found
  Searched:
    - embedded://Resources/PromptPacks/my-pack
    - file://.acode/prompts/my-pack
```

**Solution:** Check pack ID matches directory name.

#### Permission Denied

```
Error: Cannot read pack at '.acode/prompts/my-pack'
  Access denied to manifest.yml
```

**Solution:** Check file permissions.

#### Invalid YAML

```
Error: Failed to parse manifest
  Line 5: mapping values are not allowed here
```

**Solution:** Check YAML syntax in manifest.yml.

#### Pack Too Large

```
Error: Pack 'my-pack' exceeds size limit
  Size: 5.2MB
  Limit: 5MB
```

**Solution:** Reduce component file sizes.

### Best Practices

1. **Version your packs** - Use SemVer for tracking changes
2. **Regenerate hashes** - After modifying components
3. **Validate before commit** - Use `acode prompts validate`
4. **Use descriptive IDs** - `team-backend-v2` not `pack1`
5. **Keep packs focused** - One pack per use case

---

## Acceptance Criteria

### Loader Interface

- [ ] AC-001: IPromptPackLoader in Application
- [ ] AC-002: LoadPack method exists
- [ ] AC-003: LoadBuiltInPack method exists
- [ ] AC-004: LoadUserPack method exists
- [ ] AC-005: Returns PromptPack or error

### Loader Implementation

- [ ] AC-006: Infrastructure layer implementation
- [ ] AC-007: Parses manifest first
- [ ] AC-008: Validates manifest schema
- [ ] AC-009: Loads all components
- [ ] AC-010: Verifies content hash
- [ ] AC-011: Hash mismatch = warning
- [ ] AC-012: Missing file = error
- [ ] AC-013: Invalid YAML = error
- [ ] AC-014: Normalizes paths
- [ ] AC-015: Blocks path traversal

### Validator Interface

- [ ] AC-016: IPackValidator in Application
- [ ] AC-017: Validate method exists
- [ ] AC-018: Returns ValidationResult
- [ ] AC-019: ValidationResult has errors list
- [ ] AC-020: ValidationResult has IsValid
- [ ] AC-021: Errors have code
- [ ] AC-022: Errors have message
- [ ] AC-023: Errors have path

### Validator Implementation

- [ ] AC-024: Checks required fields
- [ ] AC-025: Checks id format
- [ ] AC-026: Checks version format
- [ ] AC-027: Checks files exist
- [ ] AC-028: Checks paths valid
- [ ] AC-029: Checks template syntax
- [ ] AC-030: Checks size limit
- [ ] AC-031: 5MB limit enforced
- [ ] AC-032: < 100ms validation

### Registry Interface

- [ ] AC-033: IPromptPackRegistry in Application
- [ ] AC-034: GetPack method exists
- [ ] AC-035: ListPacks method exists
- [ ] AC-036: GetActivePack method exists
- [ ] AC-037: Refresh method exists
- [ ] AC-038: TryGetPack method exists

### Registry Implementation

- [ ] AC-039: Discovers built-in packs
- [ ] AC-040: Discovers user packs
- [ ] AC-041: Indexes by ID
- [ ] AC-042: User overrides built-in
- [ ] AC-043: GetPack throws if missing
- [ ] AC-044: TryGetPack returns null
- [ ] AC-045: ListPacks returns all
- [ ] AC-046: GetActivePack uses config
- [ ] AC-047: Refresh reloads all

### Configuration

- [ ] AC-048: Reads prompts.pack_id
- [ ] AC-049: Config path correct
- [ ] AC-050: Default is acode-standard
- [ ] AC-051: ACODE_PROMPT_PACK works
- [ ] AC-052: Env overrides config
- [ ] AC-053: Config overrides default
- [ ] AC-054: Missing section = default
- [ ] AC-055: Invalid pack = fallback
- [ ] AC-056: Fallback logged WARNING
- [ ] AC-057: Selection logged INFO

### Caching

- [ ] AC-058: Packs cached after load
- [ ] AC-059: Cache key = id + hash
- [ ] AC-060: Cache hit returns same
- [ ] AC-061: Cache miss loads
- [ ] AC-062: Refresh invalidates
- [ ] AC-063: Config change invalidates
- [ ] AC-064: Thread-safe cache

### CLI

- [ ] AC-065: list command works
- [ ] AC-066: list shows id, version, source
- [ ] AC-067: list shows active flag
- [ ] AC-068: show command works
- [ ] AC-069: show includes components
- [ ] AC-070: validate command works
- [ ] AC-071: validate outputs errors
- [ ] AC-072: validate exit 0/1 correct
- [ ] AC-073: reload command works

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Application/PromptPacks/
├── PromptPackLoaderTests.cs
│   ├── Should_Load_Valid_Pack()
│   ├── Should_Fail_On_Missing_Manifest()
│   ├── Should_Fail_On_Invalid_YAML()
│   ├── Should_Warn_On_Hash_Mismatch()
│   ├── Should_Load_All_Components()
│   └── Should_Block_Path_Traversal()
│
├── PackValidatorTests.cs
│   ├── Should_Validate_Required_Fields()
│   ├── Should_Validate_Id_Format()
│   ├── Should_Validate_Version_Format()
│   ├── Should_Check_Files_Exist()
│   ├── Should_Validate_Template_Variables()
│   └── Should_Enforce_Size_Limit()
│
├── PromptPackRegistryTests.cs
│   ├── Should_Discover_BuiltIn_Packs()
│   ├── Should_Discover_User_Packs()
│   ├── Should_Prioritize_User_Packs()
│   ├── Should_Get_Pack_By_Id()
│   └── Should_Get_Active_Pack()
│
└── PackSelectionTests.cs
    ├── Should_Select_From_Config()
    ├── Should_Use_Default()
    ├── Should_Override_With_Env()
    └── Should_Fallback_On_Missing()
```

### Integration Tests

```
Tests/Integration/PromptPacks/
├── LoaderIntegrationTests.cs
│   ├── Should_Load_BuiltIn_Pack()
│   ├── Should_Load_User_Pack()
│   └── Should_Handle_Missing_Directory()
│
└── RegistryIntegrationTests.cs
    ├── Should_Index_All_Packs()
    └── Should_Refresh_Registry()
```

### E2E Tests

```
Tests/E2E/PromptPacks/
├── PackSelectionE2ETests.cs
│   ├── Should_Use_Configured_Pack()
│   ├── Should_Use_Env_Override()
│   └── Should_Fallback_Gracefully()
```

### Performance Tests

- PERF-001: Pack loading < 100ms
- PERF-002: Validation < 100ms
- PERF-003: Registry init < 500ms
- PERF-004: Cache lookup < 1ms

---

## User Verification Steps

### Scenario 1: List Packs

1. Run `acode prompts list`
2. Verify: Built-in packs shown
3. Verify: User packs shown
4. Verify: Active pack marked

### Scenario 2: Select via Config

1. Set pack_id in config
2. Run `acode prompts list`
3. Verify: Configured pack is active

### Scenario 3: Environment Override

1. Set ACODE_PROMPT_PACK env var
2. Run `acode prompts list`
3. Verify: Env pack is active

### Scenario 4: Fallback on Missing

1. Configure nonexistent pack
2. Start agent
3. Verify: Falls back to default
4. Verify: Warning logged

### Scenario 5: Validate Pack

1. Run `acode prompts validate`
2. Verify: Valid pack passes
3. Verify: Invalid pack shows errors

### Scenario 6: Reload Registry

1. Add new user pack
2. Run `acode prompts reload`
3. Verify: New pack appears in list

### Scenario 7: User Override

1. Create user pack with built-in ID
2. Run `acode prompts list`
3. Verify: User source shown

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/PromptPacks/
├── IPromptPackLoader.cs
├── IPackValidator.cs
├── IPromptPackRegistry.cs
├── ValidationResult.cs
└── ValidationError.cs

src/AgenticCoder.Infrastructure/PromptPacks/
├── PromptPackLoader.cs
├── PackValidator.cs
├── PromptPackRegistry.cs
├── PackDiscovery.cs
├── PackCache.cs
└── PackConfiguration.cs
```

### IPromptPackLoader Interface

```csharp
namespace AgenticCoder.Application.PromptPacks;

public interface IPromptPackLoader
{
    PromptPack LoadPack(string path);
    PromptPack LoadBuiltInPack(string packId);
    PromptPack LoadUserPack(string path);
    bool TryLoadPack(string path, out PromptPack? pack, out string? error);
}
```

### IPackValidator Interface

```csharp
namespace AgenticCoder.Application.PromptPacks;

public interface IPackValidator
{
    ValidationResult Validate(PromptPack pack);
    ValidationResult ValidatePath(string packPath);
}

public sealed class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<ValidationError> Errors { get; }
}

public sealed class ValidationError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public string? FilePath { get; init; }
    public int? LineNumber { get; init; }
}
```

### IPromptPackRegistry Interface

```csharp
namespace AgenticCoder.Application.PromptPacks;

public interface IPromptPackRegistry
{
    PromptPack GetPack(string packId);
    PromptPack? TryGetPack(string packId);
    IReadOnlyList<PromptPackInfo> ListPacks();
    PromptPack GetActivePack();
    void Refresh();
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-PKL-001 | Pack not found |
| ACODE-PKL-002 | Manifest parse error |
| ACODE-PKL-003 | Component read error |
| ACODE-PKL-004 | Permission denied |
| ACODE-VAL-001 | Missing required field |
| ACODE-VAL-002 | Invalid pack ID |
| ACODE-VAL-003 | Invalid version |
| ACODE-VAL-004 | Component not found |
| ACODE-VAL-005 | Invalid template |
| ACODE-VAL-006 | Size exceeded |

### Logging Fields

```json
{
  "event": "pack_loaded",
  "pack_id": "acode-dotnet",
  "pack_version": "1.0.0",
  "source": "built-in",
  "component_count": 12,
  "load_time_ms": 45
}
```

### CLI Exit Codes

| Exit Code | Meaning |
|-----------|---------|
| 0 | Success |
| 1 | Validation failed |
| 2 | Pack not found |
| 3 | File I/O error |

### Implementation Checklist

1. [ ] Create IPromptPackLoader interface
2. [ ] Create IPackValidator interface
3. [ ] Create IPromptPackRegistry interface
4. [ ] Create ValidationResult class
5. [ ] Create ValidationError class
6. [ ] Implement PromptPackLoader
7. [ ] Implement PackValidator
8. [ ] Implement PromptPackRegistry
9. [ ] Implement PackDiscovery
10. [ ] Implement PackCache
11. [ ] Implement PackConfiguration
12. [ ] Add CLI commands
13. [ ] Write unit tests
14. [ ] Write integration tests
15. [ ] Add XML documentation

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~PromptPacks"
```

---

**End of Task 008.b Specification**