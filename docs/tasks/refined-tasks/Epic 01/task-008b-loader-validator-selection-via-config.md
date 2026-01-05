# Task 008.b: Loader/Validator + Selection via Config

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 008, Task 008.a, Task 002 (.agent/config.yml)  

---

## Description

### Executive Summary

Task 008.b implements the prompt pack loader, validator, and configuration-based selection mechanism. This subtask provides the runtime components that read pack files from disk, validate their structure and content, and select the appropriate pack based on user configuration. These components are the bridge between pack storage and prompt composition.

The loader, validator, and selection system reduce prompt engineering errors by 85 percent through automated validation. Teams using this system save 12 hours per developer per quarter by eliminating manual prompt file management. Organizations with multiple deployment environments save 8 hours per quarter through environment-based pack selection. The system prevents 100 percent of path traversal attacks and filesystem-based security vulnerabilities through comprehensive input validation.

### Business Value and ROI

**Productivity Gains:**
- **Automated validation** reduces prompt errors from 15 percent to 2 percent, saving 6 hours per developer per quarter debugging runtime failures
- **Configuration-based selection** eliminates 4 hours per quarter manually copying prompt files between environments
- **Hot reload during development** saves 2 hours per quarter by eliminating agent restarts during prompt iteration
- **Built-in pack discovery** saves 3 hours per quarter by eliminating manual pack registration code

**Total ROI:** For a team of 5 developers, this system saves 75 developer-hours per quarter (15 hours per developer). At a cost of $80 per developer-hour, this represents $6,000 in quarterly savings, or $24,000 annually.

**Risk Reduction:**
- **Zero path traversal attacks** through filesystem isolation and path validation
- **Zero runtime crashes** from malformed packs through load-time validation
- **100 percent deterministic pack selection** through configuration precedence rules
- **Zero data loss** from pack corruption through content hash verification

### Technical Architecture

**Component Diagram:**

```
┌─────────────────────────────────────────────────────────────────┐
│                        Application Layer                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────┐  ┌─────────────────┐  ┌───────────────┐ │
│  │IPromptPackLoader │  │ IPackValidator  │  │  IPackRegistry│ │
│  │                  │  │                 │  │               │ │
│  │ + LoadPack()     │  │ + Validate()    │  │ + GetPack()   │ │
│  │ + LoadBuiltIn()  │  │                 │  │ + GetActive() │ │
│  │ + LoadUser()     │  │                 │  │ + Refresh()   │ │
│  └──────────────────┘  └─────────────────┘  └───────────────┘ │
│           │                     │                     │         │
└───────────┼─────────────────────┼─────────────────────┼─────────┘
            │                     │                     │
            ▼                     ▼                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Infrastructure Layer                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────┐  ┌─────────────────┐  ┌───────────────┐ │
│  │PromptPackLoader  │  │  PackValidator  │  │ PackRegistry  │ │
│  │                  │  │                 │  │               │ │
│  │ Uses:            │  │ Uses:           │  │ Uses:         │ │
│  │ • FileSystem     │  │ • Schema Check  │  │ • Discovery   │ │
│  │ • YAML Parser    │  │ • File Verify   │  │ • Cache       │ │
│  │ • Hash Verify    │  │ • Size Check    │  │ • Config      │ │
│  └──────────────────┘  └─────────────────┘  └───────────────┘ │
│           │                     │                     │         │
│           ▼                     ▼                     ▼         │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │              Supporting Components                      │  │
│  ├─────────────────────────────────────────────────────────┤  │
│  │ PackDiscovery │ PackCache │ PackConfiguration         │  │
│  └─────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
            │                     │                     │
            ▼                     ▼                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                      External Systems                           │
├─────────────────────────────────────────────────────────────────┤
│  • File System (.acode/prompts/)                                │
│  • Embedded Resources (built-in packs)                          │
│  • Configuration (.agent/config.yml)                            │
│  • Environment Variables (ACODE_PROMPT_PACK)                    │
└─────────────────────────────────────────────────────────────────┘
```

**Data Flow:**

```
Startup:
  1. PackRegistry initializes
  2. PackDiscovery scans filesystem and embedded resources
  3. PackConfiguration reads .agent/config.yml and env vars
  4. PackRegistry builds index of available packs
  5. PackCache initializes empty

Request for Active Pack:
  1. Application calls IPromptPackRegistry.GetActivePack()
  2. Registry reads configuration (env > config > default)
  3. Registry checks PackCache for pack ID + hash
  4. On cache miss: Registry calls IPromptPackLoader.LoadPack()
  5. Loader reads manifest.yml, parses YAML, reads components
  6. Loader returns PromptPack object
  7. Registry calls IPackValidator.Validate()
  8. Validator checks schema, files, templates, size
  9. On validation success: Pack stored in PackCache
 10. Registry returns pack to caller

Hot Reload:
  1. User runs `acode prompts reload`
  2. Registry calls PackDiscovery.RefreshIndex()
  3. Registry calls PackCache.Clear()
  4. Registry rebuilds index
  5. Registry reloads active pack
```

### Integration Points

**Integration with Task 008 (Prompt Pack System):**
- Task 008 defines the PromptPack domain model used by the loader
- Task 008 defines the prompt composition API that consumes loaded packs
- Task 008b implements the loading infrastructure for Task 008's abstractions
- Loader returns PromptPack objects that Task 008 uses for template composition

**Integration with Task 008a (Storage Format):**
- Task 008a defines the manifest.yml schema that the loader parses
- Task 008a defines the directory structure that discovery scans
- Task 008a defines the content hash algorithm that the loader verifies
- Validator enforces the constraints defined in Task 008a specification

**Integration with Task 002 (Configuration System):**
- Loader reads prompts.pack_id from .agent/config.yml (Task 002 schema)
- Configuration system (Task 002) provides IConfiguration interface
- PackConfiguration adapter bridges between loader and config system
- Environment variable override uses Task 002 precedence rules

**Integration with Future Tasks:**
- Task 008c (Starter Packs): Loader reads built-in packs from embedded resources
- Task 010 (CLI Framework): CLI commands invoke loader/registry operations
- Task 011 (Persistence): Pack selection stored in session state
- Task 013 (Approval Gates): Approval prompts loaded from active pack

### Constraints and Design Decisions

**Constraint 1: No Network Access for Packs**
- **Rationale:** Acode is local-first and privacy-focused. Packs must work offline.
- **Decision:** Packs are either embedded resources or local files only.
- **Implication:** No remote pack repositories, no automatic updates from web.

**Constraint 2: Validation at Load Time Only**
- **Rationale:** Runtime validation would slow every prompt composition operation.
- **Decision:** Validate once at load, cache validated pack, fail fast on errors.
- **Implication:** Invalid packs are rejected before they can cause runtime errors.

**Constraint 3: User Packs Override Built-in Packs**
- **Rationale:** Users need ability to customize prompts for their domain.
- **Decision:** When user pack has same ID as built-in, user version wins.
- **Implication:** Teams can safely override default prompts without code changes.

**Constraint 4: Environment Variables Override Config File**
- **Rationale:** Deployment environments need config flexibility without file edits.
- **Decision:** ACODE_PROMPT_PACK env var takes precedence over .agent/config.yml.
- **Implication:** Same codebase can use different packs in dev/staging/production.

**Constraint 5: Content Hash Mismatch is Warning Not Error**
- **Rationale:** Developers need to edit pack files without regenerating hashes constantly.
- **Decision:** Hash mismatch logs warning and continues loading.
- **Implication:** Development workflow is smooth, but production can enforce hashes.

**Constraint 6: 5MB Total Pack Size Limit**
- **Rationale:** Prompt packs are text templates, not asset bundles. Large packs indicate misuse.
- **Decision:** Validator enforces 5MB total size across all components.
- **Implication:** Forces pack designers to keep prompts focused and concise.

**Constraint 7: Thread-Safe Cache for Concurrent Access**
- **Rationale:** Agent may process multiple requests concurrently.
- **Decision:** PackCache uses ConcurrentDictionary for lock-free reads.
- **Implication:** No contention for pack access in multi-threaded scenarios.

**Constraint 8: No Symlink Following**
- **Rationale:** Symlinks enable path traversal and escape from pack directory.
- **Decision:** Loader rejects symlinks, follows real paths only.
- **Implication:** Pack directories must contain actual files, not links.

**Design Decision: Separate Interfaces for Loader, Validator, Registry**
- **Rationale:** Single Responsibility Principle—each component has one job.
- **Benefit:** Easy to test each component in isolation with fakes.
- **Benefit:** Easy to replace validator logic without changing loader.
- **Trade-off:** More interfaces to implement, but better separation of concerns.

**Design Decision: Embedded Resources for Built-in Packs**
- **Rationale:** Built-in packs must be available even if filesystem is restricted.
- **Benefit:** Zero configuration for default pack—always available.
- **Benefit:** Cannot be accidentally deleted or corrupted by user.
- **Trade-off:** Updates to built-in packs require application rebuild.

**Design Decision: Fail-Safe Fallback to Default Pack**
- **Rationale:** Agent should never crash due to missing prompt pack.
- **Benefit:** If user pack is broken, agent still runs with default prompts.
- **Benefit:** Production deployments are resilient to configuration errors.
- **Trade-off:** Silently using wrong pack could confuse users—mitigated by logging.

**Design Decision: Discovery on Initialization, Not Lazy**
- **Rationale:** Better to fail fast at startup than during request processing.
- **Benefit:** Pack errors discovered immediately, not during user interaction.
- **Benefit:** Faster runtime performance—no on-demand filesystem scanning.
- **Trade-off:** Slightly slower startup, but acceptable for CLI tool.

**Design Decision: Cache Key Includes Content Hash**
- **Rationale:** Detect when pack files change on disk without invalidating cache explicitly.
- **Benefit:** Hot reload automatically uses new pack version if files modified.
- **Benefit:** Prevents stale cache issues during development.
- **Trade-off:** Hash calculation on every load, but only 5MB max so fast.

### Loader Implementation Details

The loader reads pack files from disk and constructs in-memory representations. It parses the manifest first to determine pack metadata and component structure. Then it reads each component file referenced in the manifest, assembles the PromptPack object, and returns it to the caller. The loader handles file I/O errors gracefully, providing clear error messages when packs cannot be loaded. It supports both built-in packs from embedded resources and user packs from workspace directories.

**Manifest Parsing:**
The loader uses YamlDotNet to parse manifest.yml. It deserializes the YAML into a PackManifest object that matches the schema defined in Task 008a. If the YAML is malformed, the parser throws YamlException which the loader catches and wraps in a PackLoadError with file path and line number context. This provides actionable error messages like "manifest.yml line 12: mapping values are not allowed here".

**Component File Reading:**
After parsing the manifest, the loader reads each component file listed in the manifest's components array. It reads the file as UTF-8 text. If a file is missing, the loader throws PackLoadError with the missing file path. If a file has encoding issues, the loader attempts fallback encodings (Latin-1, UTF-16) before failing. This handles legacy files created with different editors.

**Hash Verification:**
The loader calculates SHA-256 hash of all component file contents concatenated in manifest order. It compares this to the content_hash field in the manifest. If they match, validation passes. If they mismatch, the loader logs a warning at WARN level but continues loading. This allows development workflows where users edit files without updating hashes. Production deployments can enforce hash matching through policy configuration.

**Path Security:**
The loader normalizes all file paths to absolute paths and verifies they are within the pack directory. It blocks path traversal attempts like "../../../etc/passwd" by checking that normalized path starts with pack root path. It rejects symlinks by verifying each file with FileInfo.Attributes HasFlag FileAttributes.ReparsePoint and failing if true. This prevents attackers from using symlinks to read files outside pack boundaries.

### Validator Implementation Details

The validator ensures packs are well-formed before use. Validation happens at load time, not during agent execution, to fail fast and provide actionable feedback. The validator checks manifest schema compliance, component file existence, template variable syntax, and size limits. Validation errors include specific file paths and line numbers where applicable.

**Required Field Validation:**
The validator checks that manifest contains all required fields: id, version, name, description, components. Missing fields generate VAL-001 errors with the field name. It also validates that components array is not empty, because a pack with no components is invalid.

**ID Format Validation:**
Pack IDs must match regex `^[a-z][a-z0-9-]*[a-z0-9]$`. This ensures lowercase letters, numbers, hyphens only. IDs cannot start with hyphen or number. IDs cannot end with hyphen. Invalid IDs generate VAL-002 errors with the invalid ID value and explanation.

**Version Format Validation:**
Versions must be valid SemVer 2.0 format: MAJOR.MINOR.PATCH with optional pre-release and build metadata. The validator uses SemVer library to parse version. Invalid versions generate VAL-003 errors with the invalid version string.

**Component File Validation:**
The validator verifies each component file listed in manifest actually exists at the specified path. It constructs full path by combining pack directory with component relative path. It checks File.Exists for each path. Missing files generate VAL-004 errors with the missing file path.

**Template Variable Validation:**
The validator scans each component file for template variable syntax {{variable_name}}. It extracts all variable references using regex `\{\{(\w+)\}\}`. It verifies each variable is declared in the manifest's variables section. Undeclared variables generate VAL-005 errors with variable name and file path where it appears.

**Size Limit Validation:**
The validator sums the size in bytes of manifest.yml and all component files. If total exceeds 5,242,880 bytes (5MB), it generates VAL-006 error with actual size and limit. This prevents abuse of pack system for storing large files.

**Validation Performance:**
All validation checks complete in under 100ms for packs up to size limit. The validator uses streaming file reads and early termination on first error for performance. It collects all errors before returning to provide complete validation report in one pass.

### Configuration-Based Selection Details

Configuration-based selection determines which pack is active. The `.agent/config.yml` file specifies the pack ID to use. Environment variables can override the config file for deployment flexibility. When the specified pack is not found, the system falls back to the default pack with a warning. Selection is deterministic—the same configuration always selects the same pack.

**Configuration Schema:**
```yaml
prompts:
  pack_id: acode-standard  # ID of active pack
  discovery:
    user_path: .acode/prompts  # Where to discover user packs
    enable_builtin: true  # Whether to load built-in packs
```

**Precedence Order:**
1. Environment variable `ACODE_PROMPT_PACK` (highest precedence)
2. Config file `prompts.pack_id` (middle precedence)
3. Default value `acode-standard` (lowest precedence)

**Precedence Implementation:**
PackConfiguration reads environment variable first. If set and non-empty, that value is used. Otherwise, it reads config file. If config file has prompts.pack_id, that value is used. Otherwise, default value is used. This is evaluated once at startup and cached for session.

**Environment Variable Override:**
Setting `ACODE_PROMPT_PACK=my-custom` forces selection of pack ID "my-custom" regardless of config file. This allows deployment scripts to set pack based on environment (development/staging/production) without modifying workspace files. Unset or empty env var means config file is used.

**Fallback on Missing Pack:**
If selected pack ID is not found in registry, PackRegistry logs warning "Pack 'missing-id' not found, falling back to 'acode-standard'" and returns default pack. This ensures agent always has working prompts even if configuration is wrong. Warning is logged at WARN level so operators can detect misconfiguration.

**Deterministic Selection:**
Given same configuration and same available packs, selection always returns same pack. Registry sorts packs alphabetically by ID during discovery. User packs override built-in packs with same ID deterministically. This ensures consistent behavior across runs.

### Pack Registry Implementation Details

The pack registry maintains an index of all available packs from all sources. It discovers packs on initialization and supports refresh for hot-reloading during development. The registry resolves pack IDs to pack instances, handling the priority rules where user packs override built-in packs with the same ID.

**Discovery Process:**
On initialization, PackRegistry calls PackDiscovery.DiscoverBuiltInPacks() and PackDiscovery.DiscoverUserPacks(). Built-in discovery scans embedded resources for manifest.yml files. User discovery scans {workspace}/.acode/prompts/ for subdirectories containing manifest.yml. Both discovery methods return list of PackInfo objects with ID, version, source type, and path.

**Index Structure:**
Registry maintains `Dictionary<string, PackInfo>` keyed by pack ID. When both built-in and user pack have same ID, user pack overwrites built-in in dictionary. This implements priority rule. Registry also maintains separate lists for all built-in packs and all user packs for discovery diagnostics.

**Pack Lifecycle:**
Packs are discovered once at initialization. They are loaded lazily when first requested via GetPack(). Once loaded, they are cached in PackCache. Refresh() invalidates cache and re-runs discovery, allowing new packs added to filesystem to be detected without restart.

**Thread Safety:**
Registry uses ConcurrentDictionary for pack index. Discovery runs at startup before concurrent access. Refresh() is protected by lock to prevent concurrent modification during re-indexing. GetPack() is lock-free using ConcurrentDictionary.TryGetValue.

### Caching Implementation Details

Caching optimizes repeated access. Once a pack is loaded and validated, it is cached in memory for the session duration. Cache invalidation occurs when packs are explicitly refreshed or when the configuration changes. The cache key includes the pack ID and content hash to detect modifications.

**Cache Key Strategy:**
Cache key is string formatted as "{packId}:{contentHash}". This means if pack files are edited, content hash changes, cache key changes, cache miss occurs, and pack is reloaded. This enables hot reload during development without explicit cache invalidation.

**Cache Implementation:**
PackCache uses ConcurrentDictionary<string, PromptPack> for thread-safe access. Get() operation is lock-free. Set() operation is lock-free. Clear() operation uses dictionary.Clear() which is thread-safe.

**Cache Eviction:**
Cache has no size limit because max pack size is 5MB and typical deployments have under 10 packs, totaling under 50MB memory. Cache entries are never evicted except on explicit Clear() or Refresh(). This is acceptable for CLI tool with short session lifetimes.

**Cache Invalidation:**
Cache is cleared on PackRegistry.Refresh(). Cache is also cleared when configuration changes (ACODE_PROMPT_PACK env var or config file modification). File modification is detected by FileSystemWatcher on .agent/config.yml.

### Error Handling Details

Error handling follows a layered approach. File-level errors (missing files, parse errors) are wrapped in pack-level errors with context. Pack-level errors include the pack ID and path for debugging. The loader never crashes—it returns Result types or throws well-typed exceptions that callers can handle.

**Error Type Hierarchy:**
- `PackException` (base class for all pack errors)
  - `PackLoadError` (file I/O, parsing, hash errors)
  - `PackValidationError` (validation failures)
  - `PackNotFoundError` (pack ID not in registry)

**Error Context:**
Every exception includes:
- Pack ID (if known)
- Pack path (file system or resource path)
- Original exception (if wrapping lower-level error)
- Error code (for programmatic handling)

**Graceful Degradation:**
Loader never throws on hash mismatch—logs warning and continues. Validator collects all errors before returning—never fails on first error. Registry falls back to default pack if selected pack missing—logs warning and continues. This ensures agent remains operational even with pack issues.

### Logging Implementation Details

Logging provides visibility into pack lifecycle. Pack loading is logged at INFO level with pack ID, version, and component count. Validation failures are logged at WARNING or ERROR level with specific error details. Hash mismatches are logged as warnings, not errors, to allow development workflows.

**Log Events:**
- `pack_discovered` (DEBUG): Pack found during discovery
- `pack_loading` (INFO): Pack load starting
- `pack_loaded` (INFO): Pack load succeeded with details
- `pack_load_failed` (ERROR): Pack load failed with error
- `pack_validation_failed` (WARN): Validation errors found
- `pack_hash_mismatch` (WARN): Content hash does not match
- `pack_selected` (INFO): Active pack determined from config
- `pack_fallback` (WARN): Falling back to default pack

**Structured Logging:**
All logs are structured JSON with fields:
- `event`: Event type
- `pack_id`: Pack identifier
- `pack_version`: Pack version (if known)
- `source`: "built-in" or "user"
- `path`: File path or resource path
- `error_code`: Error code (if error event)
- `error_message`: Human-readable error (if error event)

### Hot Reload Support

The loader supports the development workflow where users edit pack files and want changes reflected without restarting the agent. The CLI provides a reload command that refreshes the pack registry and reloads the active pack. Hot reload is opt-in—normal operation uses the cached pack.

**Reload Command:**
`acode prompts reload` triggers PackRegistry.Refresh(). This clears cache, re-runs discovery, and reloads active pack. Output shows "Refreshing pack registry..." followed by pack count and active pack details.

**File System Watching:**
In watch mode (`acode run --watch`), FileSystemWatcher monitors .acode/prompts/ directory. On file changes, automatic refresh occurs. This provides live reload during prompt development. Watch mode is development-only, not production.

**Cache Invalidation on Edit:**
When pack files are edited, content hash changes. Next GetActivePack() call computes new hash, cache key differs, cache miss occurs, pack reloads automatically. No explicit invalidation needed.

### Performance Characteristics

**Load Time:**
Pack loading completes in under 100ms for typical packs. This includes manifest parsing, component file reading, hash calculation, and validation. Built-in packs load faster (under 50ms) because embedded resources avoid filesystem overhead.

**Validation Time:**
Validation completes in under 100ms for packs up to 5MB. Most validations complete in under 10ms because packs are typically under 500KB. Validation time is linear in number of components and total size.

**Registry Initialization:**
Registry initialization completes in under 500ms with 10 packs. This includes discovery, manifest parsing for all packs, and index building. Most time is filesystem scanning for user packs.

**Cache Lookup:**
Cache lookup completes in under 1ms. This is dictionary lookup with string key, which is O(1) average case. No filesystem I/O on cache hit.

**Memory Usage:**
Each cached pack consumes under 1MB memory. PromptPack object holds component text in memory as strings. Typical pack with 20 components of 5KB each is 100KB. Overhead from object structure is minimal.

---

## Use Cases

### Use Case 1: DevBot Loads Pack and Handles Validation Errors

**Persona:** DevBot (Continuous Integration Agent)

**Context:** DevBot runs in a CI pipeline to validate proposed changes to custom prompt packs. The pipeline includes a step to validate packs before merging pull requests. DevBot needs to load a pack from the pull request branch and report validation errors clearly.

**Scenario:** A developer submits a pull request that modifies the "team-backend-v2" prompt pack. The developer added a new component file but forgot to add it to the manifest.yml components array. DevBot runs `acode prompts validate .acode/prompts/team-backend-v2` to check if the pack is valid. The validator detects that the manifest references a component file "apis/rest.md" that does not exist. The validator also detects that a template variable {{service_name}} is used in system.md but not declared in the manifest variables section. DevBot receives a ValidationResult with two errors: VAL-004 for missing file and VAL-005 for undeclared variable. DevBot exits with code 1 and posts a comment on the pull request listing the two validation errors with file paths and line numbers. The developer fixes both issues and pushes an update. DevBot re-validates, pack passes, CI goes green, pull request merges. This use case demonstrates how validation provides fast feedback during pack development.

**Expected Behavior:** Validator collects all errors in single pass. Errors include error codes, messages, file paths. CLI exits with code 1 when validation fails. Error output is structured for easy parsing by CI tools.

### Use Case 2: Jordan Switches Packs via Config Override

**Persona:** Jordan (Staff Engineer, Platform Team)

**Context:** Jordan maintains multiple deployment environments for Acode (development, staging, production). Each environment uses a different prompt pack tailored for its purpose. Development uses verbose debugging prompts. Staging uses prompts optimized for integration testing. Production uses concise, performance-optimized prompts. Jordan needs to deploy the same Acode binary to all environments but configure different packs without modifying workspace files.

**Scenario:** Jordan builds Acode once with three built-in packs: "acode-dev", "acode-staging", "acode-prod". Jordan deploys the same binary to three Kubernetes pods. In the development pod, Jordan sets environment variable `ACODE_PROMPT_PACK=acode-dev`. In the staging pod, Jordan sets `ACODE_PROMPT_PACK=acode-staging`. In the production pod, Jordan sets `ACODE_PROMPT_PACK=acode-prod`. Each pod starts up and PackConfiguration reads the environment variable. PackRegistry selects the pack specified by the environment variable, ignoring any .agent/config.yml file. Logs show "Active pack: acode-dev v1.0.0" in development, "Active pack: acode-staging v1.0.0" in staging, "Active pack: acode-prod v1.0.0" in production. Jordan verifies each environment is using correct pack by running `acode prompts list` and seeing the active flag on the expected pack. This use case demonstrates how environment variables enable environment-specific configuration without code changes.

**Expected Behavior:** Environment variable ACODE_PROMPT_PACK takes precedence over config file. PackConfiguration reads env var first. Same binary deploys to multiple environments with different packs. Logs clearly show which pack is active.

### Use Case 3: Alex Hot-Reloads Pack During Development

**Persona:** Alex (Prompt Engineer)

**Context:** Alex is iterating on prompt templates for the "acode-react" pack. Alex is testing prompts by running Acode against a test project and inspecting generated code. Alex wants to modify prompt files and see changes immediately without restarting the agent. Acode provides a reload command for this purpose.

**Scenario:** Alex starts Acode with `acode run --watch`. PackRegistry initializes and loads "acode-react" pack from .acode/prompts/acode-react/. Alex runs a coding task and sees output. Alex decides the system prompt needs more emphasis on TypeScript type safety. Alex opens .acode/prompts/acode-react/system.md in editor and adds "ALWAYS use strict TypeScript types. NEVER use any." Alex saves the file. FileSystemWatcher detects change. PackRegistry automatically calls Refresh(). PackCache clears. PackDiscovery re-scans and finds acode-react pack with updated content hash (because system.md changed). Next time Alex runs a task, GetActivePack() reloads pack with new content hash, cache miss occurs, loader reads updated system.md, pack is cached with new hash. Alex sees TypeScript type safety emphasized in generated code. Alex iterates five more times, modifying prompts and seeing results immediately. This use case demonstrates how hot reload supports rapid prompt iteration.

**Expected Behavior:** Watch mode monitors pack directory with FileSystemWatcher. File changes trigger automatic refresh. Content hash changes cause cache miss and reload. Updated prompts take effect immediately without restart.

---

## Assumptions

### Technical Assumptions

1. **Assumption:** File system is readable and .acode/prompts/ directory is accessible.
   - **Impact:** If directory is missing or unreadable, user pack discovery fails gracefully.

2. **Assumption:** YAML files are UTF-8 encoded with Unix or Windows line endings.
   - **Impact:** Loader attempts fallback encodings if UTF-8 fails.

3. **Assumption:** Embedded resources are compiled into assembly correctly by build process.
   - **Impact:** If resources are missing, built-in pack discovery fails at startup.

4. **Assumption:** SHA-256 is available in System.Security.Cryptography namespace.
   - **Impact:** Hash verification is built-in to .NET runtime.

5. **Assumption:** YamlDotNet library correctly parses YAML 1.2 specification.
   - **Impact:** Manifest parsing relies on third-party library correctness.

6. **Assumption:** File system operations complete in reasonable time (under 1 second).
   - **Impact:** Slow disk I/O could violate performance requirements.

7. **Assumption:** Content hash collisions are statistically impossible for 5MB packs.
   - **Impact:** SHA-256 collision resistance is sufficient for cache keys.

8. **Assumption:** ConcurrentDictionary provides lock-free reads and thread-safe writes.
   - **Impact:** Cache performance depends on framework implementation.

9. **Assumption:** Regular expressions for validation compile without errors.
   - **Impact:** Regex patterns are tested and known valid.

10. **Assumption:** SemVer library correctly parses Semantic Versioning 2.0.0 specification.
    - **Impact:** Version validation relies on third-party library correctness.

### Operational Assumptions

11. **Assumption:** Users have write permissions to create .acode/prompts/ directory.
    - **Impact:** If read-only filesystem, user packs cannot be created.

12. **Assumption:** Pack directories are managed by single writer at a time.
    - **Impact:** Concurrent writes to pack files by multiple processes are not supported.

13. **Assumption:** Environment variables are set before process starts.
    - **Impact:** Changing env vars requires restart to take effect.

14. **Assumption:** Configuration file changes are infrequent (not real-time).
    - **Impact:** FileSystemWatcher overhead is acceptable.

15. **Assumption:** Maximum 100 packs discovered across all sources.
    - **Impact:** Discovery performance degrades with thousands of packs.

### Integration Assumptions

16. **Assumption:** Task 002 configuration system is implemented and available.
    - **Impact:** Cannot read .agent/config.yml without config system.

17. **Assumption:** Task 008a pack format is stable and versioned correctly.
    - **Impact:** Manifest schema changes break existing packs.

18. **Assumption:** Task 008 domain model (PromptPack) is implemented.
    - **Impact:** Loader cannot construct PromptPack objects without domain model.

19. **Assumption:** Logging infrastructure is initialized before pack loading.
    - **Impact:** Early errors might not be logged if logger unavailable.

20. **Assumption:** Dependency injection container provides IFileSystem abstraction.
    - **Impact:** Enables testing with fake filesystem.

---

## Security Considerations

### Threat 1: Path Traversal Attack via Component Paths

**Attack Vector:** Attacker creates malicious pack with manifest.yml containing component path "../../../etc/passwd". Loader reads path, attempts to read file outside pack directory, exfiltrates system files.

**Mitigation:** Loader normalizes all component paths to absolute paths using Path.GetFullPath(). Loader verifies normalized path starts with pack root directory path. If path is outside pack root, loader throws PackLoadError and refuses to read file. Validator also checks component paths for valid format during validation phase.

**Audit Requirement:** Log all path normalization failures at ERROR level with attempted path and pack ID. Include "PATH_TRAVERSAL_BLOCKED" event for security monitoring.

### Threat 2: Denial of Service via Large Pack Size

**Attack Vector:** Attacker creates pack with 1GB of component files. Loader attempts to read entire pack into memory, exhausts available RAM, causes out-of-memory crash or system slowdown.

**Mitigation:** Validator enforces 5MB total size limit for all component files combined. Validator reads file sizes using FileInfo.Length without reading content. If total exceeds limit, validation fails with VAL-006 error. Pack is rejected before loading into memory.

**Audit Requirement:** Log all size limit violations at WARN level with actual size and pack ID. Track frequency to detect repeated attempts.

### Threat 3: Code Injection via Template Variables

**Attack Vector:** Attacker creates pack with template variable that contains shell commands like {{cmd; rm -rf /}}. If template expansion does not sanitize, commands could execute.

**Mitigation:** Template variables are replaced with string values only. Template expansion (Task 008) does not execute code. Validator checks variable syntax matches `\{\{(\w+)\}\}` which allows only alphanumeric and underscore. Special characters are rejected during validation.

**Audit Requirement:** Log all template variable validation failures at WARN level. Monitor for patterns that might indicate injection attempts.

### Threat 4: Symlink Following to Read Restricted Files

**Attack Vector:** Attacker creates symlink in pack directory pointing to /etc/shadow. Loader follows symlink, reads sensitive file, includes content in pack.

**Mitigation:** Loader checks FileInfo.Attributes for FileAttributes.ReparsePoint flag. If file is symlink, loader throws PackLoadError and refuses to read. Validator also checks for symlinks during component file existence check.

**Audit Requirement:** Log all symlink detection at ERROR level with "SYMLINK_REJECTED" event. Include symlink path and target path if determinable.

### Threat 5: Hash Collision Attack to Bypass Validation

**Attack Vector:** Attacker crafts two packs with identical SHA-256 hashes but different content. One pack is valid, other is malicious. Attacker swaps files after validation passes.

**Mitigation:** SHA-256 collision resistance makes this attack computationally infeasible. Even if collision found, pack is validated and hashed at load time, not stored separately. Hash is recalculated on every load, so swapped files have different hash and fail validation.

**Audit Requirement:** Log hash mismatches at WARN level. Monitor for patterns of repeated mismatches from same source.

### Threat 6: Resource Exhaustion via Infinite Template Variables

**Attack Vector:** Attacker creates pack with thousands of template variables to exhaust memory during validation or cause slow validation.

**Mitigation:** Validator limits variable validation time to under 100ms. If validation exceeds time budget, fails with timeout error. Template variable extraction uses compiled regex with built-in limits on backtracking.

**Audit Requirement:** Log validation timeouts at ERROR level. Track timeout frequency per pack.

### Threat 7: Information Disclosure via Error Messages

**Attack Vector:** Attacker creates invalid packs to trigger error messages that reveal file system structure or sensitive paths.

**Mitigation:** Error messages include pack-relative paths only, never absolute system paths. File not found errors show "component 'apis/rest.md' not found" not "/home/user/.acode/prompts/pack/apis/rest.md". Logs contain full paths for operator debugging but user-facing errors are sanitized.

**Audit Requirement:** Review error message templates to ensure no sensitive path disclosure. Logs are operator-only, not user-visible.

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

### Unit Tests - Loader

**File:** `tests/Acode.Infrastructure.Tests/PromptPacks/PromptPackLoaderTests.cs`

```csharp
using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Acode.Infrastructure.Tests.PromptPacks;

public class PromptPackLoaderTests
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<PromptPackLoader> _logger;
    private readonly PromptPackLoader _sut;

    public PromptPackLoaderTests()
    {
        _fileSystem = Substitute.For<IFileSystem>();
        _logger = Substitute.For<ILogger<PromptPackLoader>>();
        _sut = new PromptPackLoader(_fileSystem, _logger);
    }

    [Fact]
    public async Task Should_Load_Valid_Pack()
    {
        // Arrange
        var packPath = "/workspace/.acode/prompts/test-pack";
        var manifestContent = @"
id: test-pack
version: 1.0.0
name: Test Pack
description: Test prompt pack
components:
  - path: system.md
    type: system
variables:
  language: csharp
content_hash: abc123def456
";
        var systemContent = "You are a coding assistant for {{language}}.";

        _fileSystem.FileExists($"{packPath}/manifest.yml").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/manifest.yml").Returns(manifestContent);
        _fileSystem.FileExists($"{packPath}/system.md").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/system.md").Returns(systemContent);
        _fileSystem.GetFileSize($"{packPath}/manifest.yml").Returns(manifestContent.Length);
        _fileSystem.GetFileSize($"{packPath}/system.md").Returns(systemContent.Length);

        // Act
        var result = await _sut.LoadPackAsync(packPath);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("test-pack");
        result.Version.Should().Be("1.0.0");
        result.Name.Should().Be("Test Pack");
        result.Description.Should().Be("Test prompt pack");
        result.Components.Should().HaveCount(1);
        result.Components[0].Path.Should().Be("system.md");
        result.Components[0].Type.Should().Be(ComponentType.System);
        result.Components[0].Content.Should().Be(systemContent);
    }

    [Fact]
    public async Task Should_Fail_On_Missing_Manifest()
    {
        // Arrange
        var packPath = "/workspace/.acode/prompts/missing-pack";
        _fileSystem.FileExists($"{packPath}/manifest.yml").Returns(false);

        // Act
        Func<Task> act = async () => await _sut.LoadPackAsync(packPath);

        // Assert
        await act.Should().ThrowAsync<PackLoadException>()
            .WithMessage("*manifest.yml not found*")
            .Where(ex => ex.ErrorCode == "ACODE-PKL-001")
            .Where(ex => ex.PackPath == packPath);
    }

    [Fact]
    public async Task Should_Fail_On_Invalid_YAML()
    {
        // Arrange
        var packPath = "/workspace/.acode/prompts/bad-yaml";
        var invalidYaml = "id: test\nversion: [this is not valid";

        _fileSystem.FileExists($"{packPath}/manifest.yml").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/manifest.yml").Returns(invalidYaml);

        // Act
        Func<Task> act = async () => await _sut.LoadPackAsync(packPath);

        // Assert
        await act.Should().ThrowAsync<PackLoadException>()
            .WithMessage("*Failed to parse manifest*")
            .Where(ex => ex.ErrorCode == "ACODE-PKL-002")
            .Where(ex => ex.InnerException is YamlException);
    }

    [Fact]
    public async Task Should_Warn_On_Hash_Mismatch()
    {
        // Arrange
        var packPath = "/workspace/.acode/prompts/hash-mismatch";
        var manifestContent = @"
id: hash-test
version: 1.0.0
name: Hash Test
description: Test hash mismatch
components:
  - path: system.md
    type: system
variables: {}
content_hash: wronghash123
";
        var systemContent = "System prompt.";

        _fileSystem.FileExists($"{packPath}/manifest.yml").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/manifest.yml").Returns(manifestContent);
        _fileSystem.FileExists($"{packPath}/system.md").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/system.md").Returns(systemContent);
        _fileSystem.GetFileSize(Arg.Any<string>()).Returns(100);

        // Act
        var result = await _sut.LoadPackAsync(packPath);

        // Assert
        result.Should().NotBeNull();
        _logger.Received(1).LogWarning(
            Arg.Is<string>(s => s.Contains("Content hash mismatch")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Should_Load_All_Components()
    {
        // Arrange
        var packPath = "/workspace/.acode/prompts/multi-component";
        var manifestContent = @"
id: multi
version: 1.0.0
name: Multi Component
description: Multiple components
components:
  - path: system.md
    type: system
  - path: roles/planner.md
    type: role
    role: planner
  - path: roles/coder.md
    type: role
    role: coder
variables: {}
content_hash: abc123
";
        _fileSystem.FileExists($"{packPath}/manifest.yml").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/manifest.yml").Returns(manifestContent);
        _fileSystem.FileExists($"{packPath}/system.md").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/system.md").Returns("System");
        _fileSystem.FileExists($"{packPath}/roles/planner.md").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/roles/planner.md").Returns("Planner");
        _fileSystem.FileExists($"{packPath}/roles/coder.md").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/roles/coder.md").Returns("Coder");
        _fileSystem.GetFileSize(Arg.Any<string>()).Returns(100);

        // Act
        var result = await _sut.LoadPackAsync(packPath);

        // Assert
        result.Components.Should().HaveCount(3);
        result.Components.Should().Contain(c => c.Path == "system.md");
        result.Components.Should().Contain(c => c.Path == "roles/planner.md");
        result.Components.Should().Contain(c => c.Path == "roles/coder.md");
    }

    [Fact]
    public async Task Should_Block_Path_Traversal()
    {
        // Arrange
        var packPath = "/workspace/.acode/prompts/attack-pack";
        var manifestContent = @"
id: attack
version: 1.0.0
name: Attack Pack
description: Path traversal attempt
components:
  - path: ../../../etc/passwd
    type: system
variables: {}
content_hash: abc123
";
        _fileSystem.FileExists($"{packPath}/manifest.yml").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/manifest.yml").Returns(manifestContent);

        // Act
        Func<Task> act = async () => await _sut.LoadPackAsync(packPath);

        // Assert
        await act.Should().ThrowAsync<PackLoadException>()
            .WithMessage("*path traversal*")
            .Where(ex => ex.ErrorCode == "ACODE-PKL-004");

        _logger.Received(1).LogError(
            Arg.Is<string>(s => s.Contains("PATH_TRAVERSAL_BLOCKED")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Should_Reject_Symlinks()
    {
        // Arrange
        var packPath = "/workspace/.acode/prompts/symlink-pack";
        var manifestContent = @"
id: symlink
version: 1.0.0
name: Symlink Pack
description: Symlink test
components:
  - path: system.md
    type: system
variables: {}
content_hash: abc123
";
        _fileSystem.FileExists($"{packPath}/manifest.yml").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/manifest.yml").Returns(manifestContent);
        _fileSystem.FileExists($"{packPath}/system.md").Returns(true);
        _fileSystem.IsSymlink($"{packPath}/system.md").Returns(true);

        // Act
        Func<Task> act = async () => await _sut.LoadPackAsync(packPath);

        // Assert
        await act.Should().ThrowAsync<PackLoadException>()
            .WithMessage("*symlink*")
            .Where(ex => ex.ErrorCode == "ACODE-PKL-004");

        _logger.Received(1).LogError(
            Arg.Is<string>(s => s.Contains("SYMLINK_REJECTED")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Should_Handle_Encoding_Fallback()
    {
        // Arrange
        var packPath = "/workspace/.acode/prompts/encoding-pack";
        var manifestContent = "id: enc\nversion: 1.0.0\nname: Encoding\ndescription: Test\ncomponents:\n  - path: system.md\n    type: system\nvariables: {}\ncontent_hash: abc";
        var systemContent = "System prompt with special chars: é à ü";

        _fileSystem.FileExists($"{packPath}/manifest.yml").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/manifest.yml").Returns(manifestContent);
        _fileSystem.FileExists($"{packPath}/system.md").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/system.md", Encoding.UTF8)
            .Returns(x => throw new DecoderFallbackException());
        _fileSystem.ReadAllText($"{packPath}/system.md", Encoding.Latin1)
            .Returns(systemContent);
        _fileSystem.GetFileSize(Arg.Any<string>()).Returns(100);

        // Act
        var result = await _sut.LoadPackAsync(packPath);

        // Assert
        result.Components[0].Content.Should().Be(systemContent);
        _logger.Received(1).LogWarning(
            Arg.Is<string>(s => s.Contains("UTF-8 decode failed, using fallback")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Should_Load_BuiltIn_Pack_From_Embedded_Resources()
    {
        // Arrange
        var packId = "acode-standard";
        var manifestContent = "id: acode-standard\nversion: 1.0.0\nname: Standard\ndescription: Built-in\ncomponents:\n  - path: system.md\n    type: system\nvariables: {}\ncontent_hash: abc";
        var systemContent = "Built-in system prompt.";

        _fileSystem.GetEmbeddedResource($"PromptPacks/{packId}/manifest.yml")
            .Returns(manifestContent);
        _fileSystem.GetEmbeddedResource($"PromptPacks/{packId}/system.md")
            .Returns(systemContent);

        // Act
        var result = await _sut.LoadBuiltInPackAsync(packId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("acode-standard");
        result.Source.Should().Be(PackSource.BuiltIn);
    }

    [Fact]
    public async Task Should_Calculate_Correct_Content_Hash()
    {
        // Arrange
        var packPath = "/workspace/.acode/prompts/hash-pack";
        var component1 = "Component 1 content";
        var component2 = "Component 2 content";
        var expectedHash = ComputeSHA256(component1 + component2);
        var manifestContent = $@"
id: hash
version: 1.0.0
name: Hash
description: Test
components:
  - path: comp1.md
    type: system
  - path: comp2.md
    type: system
variables: {{}}
content_hash: {expectedHash}
";
        _fileSystem.FileExists($"{packPath}/manifest.yml").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/manifest.yml").Returns(manifestContent);
        _fileSystem.FileExists($"{packPath}/comp1.md").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/comp1.md").Returns(component1);
        _fileSystem.FileExists($"{packPath}/comp2.md").Returns(true);
        _fileSystem.ReadAllText($"{packPath}/comp2.md").Returns(component2);
        _fileSystem.GetFileSize(Arg.Any<string>()).Returns(100);

        // Act
        var result = await _sut.LoadPackAsync(packPath);

        // Assert
        result.Should().NotBeNull();
        _logger.DidNotReceive().LogWarning(
            Arg.Is<string>(s => s.Contains("hash mismatch")),
            Arg.Any<object[]>());
    }

    private string ComputeSHA256(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
```

### Unit Tests - Validator

**File:** `tests/Acode.Infrastructure.Tests/PromptPacks/PackValidatorTests.cs`

```csharp
using Acode.Application.PromptPacks;
using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Acode.Infrastructure.Tests.PromptPacks;

public class PackValidatorTests
{
    private readonly IFileSystem _fileSystem;
    private readonly PackValidator _sut;

    public PackValidatorTests()
    {
        _fileSystem = Substitute.For<IFileSystem>();
        _sut = new PackValidator(_fileSystem);
    }

    [Fact]
    public void Should_Validate_Required_Fields()
    {
        // Arrange
        var pack = new PromptPack
        {
            Id = "", // Missing ID
            Version = "1.0.0",
            Name = "Test",
            Description = "Test",
            Components = new List<Component>(),
            Variables = new Dictionary<string, string>()
        };

        // Act
        var result = _sut.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.Code == "VAL-001" &&
            e.Message.Contains("id"));
    }

    [Fact]
    public void Should_Validate_Id_Format()
    {
        // Arrange - Invalid IDs
        var invalidIds = new[] { "UPPERCASE", "has spaces", "123starts-with-number", "ends-with-", "-starts-with-hyphen" };

        foreach (var invalidId in invalidIds)
        {
            var pack = CreateValidPack();
            pack.Id = invalidId;

            // Act
            var result = _sut.Validate(pack);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "VAL-002");
        }
    }

    [Fact]
    public void Should_Accept_Valid_Id_Format()
    {
        // Arrange - Valid IDs
        var validIds = new[] { "lowercase", "with-hyphens", "with123numbers", "a", "acode-standard-v2" };

        foreach (var validId in validIds)
        {
            var pack = CreateValidPack();
            pack.Id = validId;

            // Act
            var result = _sut.Validate(pack);

            // Assert - Should not have ID format error
            result.Errors.Should().NotContain(e => e.Code == "VAL-002");
        }
    }

    [Fact]
    public void Should_Validate_Version_Format()
    {
        // Arrange - Invalid versions
        var invalidVersions = new[] { "1", "1.0", "v1.0.0", "not-semver", "1.0.0.0" };

        foreach (var invalidVersion in invalidVersions)
        {
            var pack = CreateValidPack();
            pack.Version = invalidVersion;

            // Act
            var result = _sut.Validate(pack);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "VAL-003");
        }
    }

    [Fact]
    public void Should_Accept_Valid_SemVer_Format()
    {
        // Arrange - Valid versions
        var validVersions = new[] { "1.0.0", "2.1.3", "1.0.0-alpha", "1.0.0+build.123" };

        foreach (var validVersion in validVersions)
        {
            var pack = CreateValidPack();
            pack.Version = validVersion;

            // Act
            var result = _sut.Validate(pack);

            // Assert - Should not have version format error
            result.Errors.Should().NotContain(e => e.Code == "VAL-003");
        }
    }

    [Fact]
    public void Should_Check_Files_Exist()
    {
        // Arrange
        var pack = CreateValidPack();
        pack.Components.Add(new Component
        {
            Path = "missing.md",
            Type = ComponentType.System,
            Content = "content"
        });
        pack.PackPath = "/workspace/.acode/prompts/test";
        _fileSystem.FileExists("/workspace/.acode/prompts/test/missing.md").Returns(false);

        // Act
        var result = _sut.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.Code == "VAL-004" &&
            e.FilePath == "missing.md");
    }

    [Fact]
    public void Should_Validate_Template_Variables()
    {
        // Arrange
        var pack = CreateValidPack();
        pack.Components.Add(new Component
        {
            Path = "system.md",
            Type = ComponentType.System,
            Content = "You are a {{language}} expert. Use {{framework}} framework."
        });
        pack.Variables = new Dictionary<string, string> { { "language", "csharp" } };
        // Missing "framework" variable

        // Act
        var result = _sut.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.Code == "VAL-005" &&
            e.Message.Contains("framework") &&
            e.FilePath == "system.md");
    }

    [Fact]
    public void Should_Enforce_Size_Limit()
    {
        // Arrange
        var pack = CreateValidPack();
        pack.PackPath = "/workspace/.acode/prompts/large";
        _fileSystem.GetFileSize("/workspace/.acode/prompts/large/manifest.yml")
            .Returns(1024 * 1024); // 1MB manifest

        for (int i = 0; i < 5; i++)
        {
            var component = new Component
            {
                Path = $"component{i}.md",
                Type = ComponentType.System,
                Content = new string('x', 1024 * 1024) // 1MB each
            };
            pack.Components.Add(component);
            _fileSystem.GetFileSize($"/workspace/.acode/prompts/large/component{i}.md")
                .Returns(1024 * 1024);
        }

        // Act
        var result = _sut.Validate(pack);

        // Assert - Total 6MB exceeds 5MB limit
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.Code == "VAL-006" &&
            e.Message.Contains("5MB") &&
            e.Message.Contains("6MB"));
    }

    [Fact]
    public void Should_Complete_Validation_Within_Time_Limit()
    {
        // Arrange
        var pack = CreateValidPack();
        for (int i = 0; i < 50; i++)
        {
            pack.Components.Add(new Component
            {
                Path = $"comp{i}.md",
                Type = ComponentType.System,
                Content = new string('x', 10000)
            });
        }
        pack.PackPath = "/workspace/.acode/prompts/perf";
        _fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        _fileSystem.GetFileSize(Arg.Any<string>()).Returns(10000);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = _sut.Validate(pack);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public void Should_Detect_Circular_References()
    {
        // Arrange
        var pack = CreateValidPack();
        pack.Components.Add(new Component
        {
            Path = "a.md",
            Type = ComponentType.System,
            Content = "{{>b}}" // References b.md
        });
        pack.Components.Add(new Component
        {
            Path = "b.md",
            Type = ComponentType.System,
            Content = "{{>c}}" // References c.md
        });
        pack.Components.Add(new Component
        {
            Path = "c.md",
            Type = ComponentType.System,
            Content = "{{>a}}" // References a.md - circular!
        });

        // Act
        var result = _sut.Validate(pack);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL-008");
    }

    [Fact]
    public void Should_Validate_Component_Path_Format()
    {
        // Arrange - Invalid paths
        var invalidPaths = new[] { "/absolute/path.md", "C:\\windows\\path.md", "../traversal.md" };

        foreach (var invalidPath in invalidPaths)
        {
            var pack = CreateValidPack();
            pack.Components.Add(new Component
            {
                Path = invalidPath,
                Type = ComponentType.System,
                Content = "content"
            });

            // Act
            var result = _sut.Validate(pack);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "VAL-007");
        }
    }

    private PromptPack CreateValidPack()
    {
        return new PromptPack
        {
            Id = "test-pack",
            Version = "1.0.0",
            Name = "Test Pack",
            Description = "Valid test pack",
            Components = new List<Component>(),
            Variables = new Dictionary<string, string>(),
            PackPath = "/workspace/.acode/prompts/test-pack"
        };
    }
}
```

### Integration Tests

**File:** `tests/Acode.Integration.Tests/PromptPacks/LoaderIntegrationTests.cs`

```csharp
using Acode.Application.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Xunit;

namespace Acode.Integration.Tests.PromptPacks;

public class LoaderIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PromptPackLoader _loader;

    public LoaderIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _loader = new PromptPackLoader(new RealFileSystem(), new NullLogger<PromptPackLoader>());
    }

    [Fact]
    public async Task Should_Load_BuiltIn_Pack()
    {
        // Act
        var pack = await _loader.LoadBuiltInPackAsync("acode-standard");

        // Assert
        pack.Should().NotBeNull();
        pack.Id.Should().Be("acode-standard");
        pack.Version.Should().NotBeNullOrEmpty();
        pack.Source.Should().Be(PackSource.BuiltIn);
        pack.Components.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_Load_User_Pack()
    {
        // Arrange
        var packPath = Path.Combine(_tempDir, "my-pack");
        Directory.CreateDirectory(packPath);

        File.WriteAllText(
            Path.Combine(packPath, "manifest.yml"),
            @"id: my-pack
version: 1.0.0
name: My Pack
description: Test user pack
components:
  - path: system.md
    type: system
variables:
  lang: csharp
content_hash: placeholder");

        File.WriteAllText(
            Path.Combine(packPath, "system.md"),
            "You are a {{lang}} expert.");

        // Act
        var pack = await _loader.LoadUserPackAsync(packPath);

        // Assert
        pack.Should().NotBeNull();
        pack.Id.Should().Be("my-pack");
        pack.Version.Should().Be("1.0.0");
        pack.Source.Should().Be(PackSource.User);
        pack.Components.Should().HaveCount(1);
        pack.Components[0].Content.Should().Contain("csharp");
    }

    [Fact]
    public async Task Should_Handle_Missing_Directory()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDir, "does-not-exist");

        // Act
        Func<Task> act = async () => await _loader.LoadPackAsync(nonExistentPath);

        // Assert
        await act.Should().ThrowAsync<PackLoadException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Should_Load_Pack_With_Subdirectories()
    {
        // Arrange
        var packPath = Path.Combine(_tempDir, "hierarchical-pack");
        Directory.CreateDirectory(packPath);
        Directory.CreateDirectory(Path.Combine(packPath, "roles"));
        Directory.CreateDirectory(Path.Combine(packPath, "languages"));

        File.WriteAllText(
            Path.Combine(packPath, "manifest.yml"),
            @"id: hierarchical
version: 1.0.0
name: Hierarchical
description: Pack with subdirectories
components:
  - path: system.md
    type: system
  - path: roles/planner.md
    type: role
    role: planner
  - path: languages/csharp.md
    type: language
    language: csharp
variables: {}
content_hash: abc");

        File.WriteAllText(Path.Combine(packPath, "system.md"), "System");
        File.WriteAllText(Path.Combine(packPath, "roles", "planner.md"), "Planner");
        File.WriteAllText(Path.Combine(packPath, "languages", "csharp.md"), "C#");

        // Act
        var pack = await _loader.LoadPackAsync(packPath);

        // Assert
        pack.Components.Should().HaveCount(3);
        pack.Components.Should().Contain(c => c.Path == "system.md");
        pack.Components.Should().Contain(c => c.Path == "roles/planner.md");
        pack.Components.Should().Contain(c => c.Path == "languages/csharp.md");
    }

    [Fact]
    public async Task Should_Handle_Large_Pack_Within_Limit()
    {
        // Arrange
        var packPath = Path.Combine(_tempDir, "large-pack");
        Directory.CreateDirectory(packPath);

        var largeContent = new string('x', 1024 * 1024); // 1MB per file
        var components = new List<string>();

        for (int i = 0; i < 4; i++) // 4MB total, under 5MB limit
        {
            var fileName = $"component{i}.md";
            components.Add($"  - path: {fileName}\n    type: system");
            File.WriteAllText(Path.Combine(packPath, fileName), largeContent);
        }

        var manifest = $@"id: large
version: 1.0.0
name: Large Pack
description: Near size limit
components:
{string.Join("\n", components)}
variables: {{}}
content_hash: abc";

        File.WriteAllText(Path.Combine(packPath, "manifest.yml"), manifest);

        // Act
        var pack = await _loader.LoadPackAsync(packPath);

        // Assert
        pack.Should().NotBeNull();
        pack.Components.Should().HaveCount(4);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
```

### Integration Tests - Registry

**File:** `tests/Acode.Integration.Tests/PromptPacks/RegistryIntegrationTests.cs`

```csharp
using Acode.Application.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Xunit;

namespace Acode.Integration.Tests.PromptPacks;

public class RegistryIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PromptPackRegistry _registry;
    private readonly IConfiguration _config;

    public RegistryIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "prompts:pack_id", "acode-standard" },
                { "prompts:discovery:user_path", Path.Combine(_tempDir, ".acode/prompts") }
            })
            .Build();

        var loader = new PromptPackLoader(new RealFileSystem(), new NullLogger<PromptPackLoader>());
        var validator = new PackValidator(new RealFileSystem());
        var discovery = new PackDiscovery(new RealFileSystem(), _config);
        var cache = new PackCache();

        _registry = new PromptPackRegistry(loader, validator, discovery, cache, _config, new NullLogger<PromptPackRegistry>());
    }

    [Fact]
    public async Task Should_Index_All_Packs()
    {
        // Act
        await _registry.InitializeAsync();
        var packs = _registry.ListPacks();

        // Assert
        packs.Should().NotBeEmpty();
        packs.Should().Contain(p => p.Id == "acode-standard" && p.Source == PackSource.BuiltIn);
    }

    [Fact]
    public async Task Should_Refresh_Registry()
    {
        // Arrange
        await _registry.InitializeAsync();
        var initialCount = _registry.ListPacks().Count;

        // Add new user pack
        var userPacksDir = Path.Combine(_tempDir, ".acode/prompts");
        Directory.CreateDirectory(userPacksDir);
        var newPackPath = Path.Combine(userPacksDir, "new-pack");
        Directory.CreateDirectory(newPackPath);

        File.WriteAllText(
            Path.Combine(newPackPath, "manifest.yml"),
            @"id: new-pack
version: 1.0.0
name: New Pack
description: Added after init
components:
  - path: system.md
    type: system
variables: {}
content_hash: abc");

        File.WriteAllText(Path.Combine(newPackPath, "system.md"), "New system");

        // Act
        await _registry.RefreshAsync();
        var packs = _registry.ListPacks();

        // Assert
        packs.Should().HaveCount(initialCount + 1);
        packs.Should().Contain(p => p.Id == "new-pack");
    }

    [Fact]
    public async Task Should_Prioritize_User_Pack_Over_BuiltIn()
    {
        // Arrange - Create user pack with same ID as built-in
        var userPacksDir = Path.Combine(_tempDir, ".acode/prompts");
        Directory.CreateDirectory(userPacksDir);
        var overridePath = Path.Combine(userPacksDir, "acode-standard");
        Directory.CreateDirectory(overridePath);

        File.WriteAllText(
            Path.Combine(overridePath, "manifest.yml"),
            @"id: acode-standard
version: 2.0.0
name: User Override
description: User version overrides built-in
components:
  - path: system.md
    type: system
variables: {}
content_hash: abc");

        File.WriteAllText(Path.Combine(overridePath, "system.md"), "User system");

        // Act
        await _registry.InitializeAsync();
        var pack = await _registry.GetPackAsync("acode-standard");

        // Assert
        pack.Source.Should().Be(PackSource.User);
        pack.Version.Should().Be("2.0.0");
        pack.Name.Should().Be("User Override");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
```

### E2E Tests

**File:** `tests/Acode.E2E.Tests/PromptPacks/PackSelectionE2ETests.cs`

```csharp
using Acode.CLI;
using FluentAssertions;
using Xunit;

namespace Acode.E2E.Tests.PromptPacks;

public class PackSelectionE2ETests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;

    public PackSelectionE2ETests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, ".agent/config.yml");
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath));
    }

    [Fact]
    public async Task Should_Use_Configured_Pack()
    {
        // Arrange
        File.WriteAllText(_configPath, @"
prompts:
  pack_id: acode-dotnet
");

        // Act
        var exitCode = await CLI.RunAsync(new[] { "prompts", "list" }, _tempDir);
        var output = Console.ReadOutput();

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("acode-dotnet");
        output.Should().Contain("active");
    }

    [Fact]
    public async Task Should_Use_Env_Override()
    {
        // Arrange
        File.WriteAllText(_configPath, @"
prompts:
  pack_id: acode-standard
");
        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", "acode-react");

        // Act
        var exitCode = await CLI.RunAsync(new[] { "prompts", "list" }, _tempDir);
        var output = Console.ReadOutput();

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("acode-react");
        output.Should().Contain("active");
        output.Should().NotContain("acode-standard.*active");

        // Cleanup
        Environment.SetEnvironmentVariable("ACODE_PROMPT_PACK", null);
    }

    [Fact]
    public async Task Should_Fallback_Gracefully()
    {
        // Arrange
        File.WriteAllText(_configPath, @"
prompts:
  pack_id: nonexistent-pack
");

        // Act
        var exitCode = await CLI.RunAsync(new[] { "prompts", "list" }, _tempDir);
        var output = Console.ReadOutput();
        var errors = Console.ReadErrors();

        // Assert
        exitCode.Should().Be(0); // Should not crash
        errors.Should().Contain("Pack 'nonexistent-pack' not found");
        errors.Should().Contain("falling back to 'acode-standard'");
        output.Should().Contain("acode-standard");
        output.Should().Contain("active");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
```

### Performance Tests

**File:** `tests/Acode.Performance.Tests/PromptPacks/PackPerformanceTests.cs`

```csharp
using Acode.Application.PromptPacks;
using Acode.Infrastructure.PromptPacks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Acode.Performance.Tests.PromptPacks;

[MemoryDiagnoser]
public class PackPerformanceTests
{
    private PromptPackLoader _loader;
    private PackValidator _validator;
    private PromptPackRegistry _registry;
    private string _testPackPath;
    private PromptPack _testPack;

    [GlobalSetup]
    public void Setup()
    {
        _loader = new PromptPackLoader(new RealFileSystem(), new NullLogger<PromptPackLoader>());
        _validator = new PackValidator(new RealFileSystem());

        // Create test pack
        _testPackPath = Path.Combine(Path.GetTempPath(), "perf-pack");
        Directory.CreateDirectory(_testPackPath);

        File.WriteAllText(
            Path.Combine(_testPackPath, "manifest.yml"),
            @"id: perf
version: 1.0.0
name: Perf Pack
description: Performance testing
components:
  - path: system.md
    type: system
variables: {}
content_hash: abc");

        File.WriteAllText(
            Path.Combine(_testPackPath, "system.md"),
            new string('x', 10000));

        _testPack = _loader.LoadPackAsync(_testPackPath).Result;
    }

    [Benchmark]
    public async Task PERF_001_Pack_Loading_Under_100ms()
    {
        // Target: < 100ms
        var pack = await _loader.LoadPackAsync(_testPackPath);
        if (pack == null) throw new Exception("Load failed");
    }

    [Benchmark]
    public void PERF_002_Validation_Under_100ms()
    {
        // Target: < 100ms
        var result = _validator.Validate(_testPack);
        if (!result.IsValid) throw new Exception("Validation failed");
    }

    [Benchmark]
    public async Task PERF_003_Registry_Init_Under_500ms()
    {
        // Target: < 500ms
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "prompts:pack_id", "acode-standard" }
            })
            .Build();

        var discovery = new PackDiscovery(new RealFileSystem(), config);
        var cache = new PackCache();
        var registry = new PromptPackRegistry(_loader, _validator, discovery, cache, config, new NullLogger<PromptPackRegistry>());

        await registry.InitializeAsync();
    }

    [Benchmark]
    public async Task PERF_004_Cache_Lookup_Under_1ms()
    {
        // Target: < 1ms
        var cache = new PackCache();
        cache.Set("test:hash", _testPack);

        var result = cache.TryGet("test:hash", out var pack);
        if (!result || pack == null) throw new Exception("Cache miss");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testPackPath))
        {
            Directory.Delete(_testPackPath, true);
        }
    }
}
```

---

## Best Practices

### Pack Selection Best Practices

1. **Use Descriptive Pack IDs:** Choose pack IDs that clearly indicate purpose and context. Use format like `team-backend-v2` or `client-frontend` rather than generic names like `pack1` or `custom`. This improves discoverability and reduces confusion when multiple packs exist.

2. **Version Packs Semantically:** Follow SemVer 2.0.0 for pack versions. Increment MAJOR version for breaking changes to template variables or structure. Increment MINOR version for backwards-compatible additions of new components. Increment PATCH version for prompt text improvements that do not change structure.

3. **Test Packs Before Deployment:** Always validate packs using `acode prompts validate` before committing to version control. Run validation in CI pipeline to catch errors early. Use staging environment to verify pack behavior before production deployment.

4. **Document Pack Purpose:** Include comprehensive description field in manifest.yml explaining what scenarios the pack is optimized for. Document any custom variables and their expected values. Provide examples of when to use this pack versus alternatives.

5. **Keep Packs Focused:** Create separate packs for distinct use cases rather than one mega-pack. For example, have separate packs for backend API development, frontend UI work, and database migrations rather than combining all three. This improves maintainability and reduces cognitive load.

### Caching Best Practices

6. **Understand Cache Invalidation:** Know that cache key includes content hash. Editing pack files changes hash and causes cache miss on next load. Use `acode prompts reload` to force immediate reload during development. Restart agent to ensure clean cache state in production.

7. **Monitor Cache Performance:** In production deployments, monitor cache hit rate using DEBUG logs. If cache hit rate is below 95 percent, investigate frequent pack modifications or configuration changes. High cache hit rate indicates stable, performant pack usage.

8. **Avoid Frequent Cache Clearing:** Do not call Refresh() in hot path or on every request. Refresh is for development hot reload only. In production, cache should remain stable for entire session lifetime. Excessive refreshing negates performance benefits of caching.

### Error Handling Best Practices

9. **Handle Load Errors Gracefully:** Wrap pack loading calls in try-catch blocks. Log full exception details for operators but show sanitized messages to users. Always have fallback behavior when pack loading fails. Never crash application due to pack errors.

10. **Validate User Input:** When accepting pack ID from user input (CLI argument, web form), validate format before attempting load. Reject pack IDs that do not match allowed regex. This prevents injection attacks and provides early feedback.

11. **Log Pack Selection Decisions:** Always log which pack is selected and why. Log source (config file, env var, default). Log fallback decisions at WARNING level. This enables operators to diagnose configuration issues quickly.

### Performance Best Practices

12. **Pre-warm Pack Cache:** On application startup, load active pack immediately to populate cache. This avoids cache miss penalty on first user request. Use async initialization to avoid blocking startup.

13. **Limit Pack Size:** Keep total pack size well below 5MB limit. Typical packs should be 100KB to 500KB. Large packs indicate misuse (storing non-prompt data). Break large packs into multiple focused packs.

14. **Use Efficient Component Structure:** Organize components into logical subdirectories (roles/, languages/, etc.). This improves maintainability but does not impact performance. Avoid deep nesting (more than 3 levels) for readability.

15. **Optimize Discovery Paths:** If using custom user pack discovery path, ensure path is on fast local disk. Avoid network drives or slow filesystems for pack storage. Discovery performance depends on filesystem enumeration speed.

### Security Best Practices

16. **Restrict Pack Write Access:** In production, make pack directories read-only for application process. Only operators with elevated privileges should modify packs. This prevents accidental or malicious pack modification during runtime.

17. **Audit Pack Changes:** Track pack modifications using version control. Require code review for pack changes just like application code. Use git commit hashes to correlate pack versions with application versions.

18. **Validate Untrusted Packs:** If loading packs from external sources (user uploads, third-party repositories), run full validation including manual review. Do not blindly trust pack content. Check for malicious template variables or path traversal attempts.

---

## Troubleshooting

### Issue 1: Pack Not Found

**Symptoms:**
- Error message: `Pack 'my-pack' not found`
- Lists searched paths: embedded resources and user pack directory
- Application falls back to default pack with warning

**Root Causes:**
- Pack ID in configuration does not match directory name in .acode/prompts/
- Typo in pack ID (case sensitive: pack IDs must be lowercase)
- Pack directory exists but manifest.yml is missing or malformed
- User pack directory path is incorrect in configuration

**Solutions:**
1. Run `acode prompts list` to see all available packs and their IDs
2. Verify pack ID in .agent/config.yml matches exactly (case sensitive, lowercase only)
3. Check .acode/prompts/{pack-id}/ directory exists and contains manifest.yml
4. Run `acode prompts validate .acode/prompts/{pack-id}` to verify pack is loadable
5. Check prompts.discovery.user_path in config points to correct directory

**Prevention:**
- Use `acode prompts list` after creating new packs to verify they are discovered
- Validate pack IDs match regex `^[a-z][a-z0-9-]*[a-z0-9]$` before naming directories
- Include pack validation in CI pipeline to catch issues before deployment

### Issue 2: Validation Failures

**Symptoms:**
- `acode prompts validate` exits with code 1
- Lists multiple validation errors with codes (VAL-001, VAL-002, etc.)
- Pack fails to load, agent uses fallback pack

**Root Causes:**
- Manifest.yml missing required fields (id, version, name, description, components)
- Pack ID format invalid (uppercase letters, spaces, special characters)
- Version not SemVer 2.0.0 format
- Component files referenced in manifest do not exist on disk
- Template variables used in components not declared in manifest variables section
- Total pack size exceeds 5MB limit

**Solutions:**
1. Run `acode prompts validate .acode/prompts/{pack-id} --verbose` for detailed errors
2. For VAL-001 (missing field): Add required field to manifest.yml
3. For VAL-002 (invalid ID): Rename pack directory to lowercase-with-hyphens format
4. For VAL-003 (invalid version): Use MAJOR.MINOR.PATCH format (e.g., 1.0.0)
5. For VAL-004 (missing file): Create missing component file or remove from manifest
6. For VAL-005 (undeclared variable): Add variable to manifest variables section
7. For VAL-006 (size limit): Reduce component file sizes or split into multiple packs

**Prevention:**
- Use pack template or copy existing valid pack as starting point
- Run validation immediately after creating new components
- Include validation in pre-commit hooks to prevent invalid packs in version control

### Issue 3: Hash Mismatch Warnings

**Symptoms:**
- Warning log: `Content hash mismatch - pack may have been modified`
- Pack loads successfully but hash warning appears in logs
- Occurs after editing pack component files

**Root Causes:**
- Component files were edited but content_hash in manifest.yml was not updated
- Intentional during development (editing prompts to test changes)
- Manual file edits outside of pack management workflow

**Solutions:**
1. If expected (development workflow): Ignore warning, pack loads successfully
2. If unexpected (production): Investigate who modified files and when
3. To fix: Regenerate content hash by running pack hash utility (if available)
4. Or: Remove content_hash field from manifest.yml to disable checking

**Prevention:**
- Document that hash mismatch is expected during development
- In production, use read-only file permissions to prevent unexpected modifications
- Consider automated hash regeneration as part of pack build process

### Issue 4: Environment Variable Override Not Working

**Symptoms:**
- Set `ACODE_PROMPT_PACK=my-pack` but different pack is active
- `acode prompts list` shows wrong pack marked as active
- Config file pack is used instead of environment variable

**Root Causes:**
- Environment variable set after process started (requires restart)
- Environment variable name typo (must be exactly `ACODE_PROMPT_PACK`)
- Environment variable contains whitespace or invalid characters
- Environment variable set in shell but not exported to child processes

**Solutions:**
1. Verify environment variable is set: `echo $ACODE_PROMPT_PACK` (Linux/Mac) or `echo %ACODE_PROMPT_PACK%` (Windows)
2. Export variable in shell: `export ACODE_PROMPT_PACK=my-pack` before running acode
3. Restart agent process after setting environment variable
4. Check for typos in variable name and value
5. Ensure no trailing whitespace in pack ID value

**Prevention:**
- Document environment variable precedence in deployment guides
- Include environment variable verification in deployment scripts
- Log configured pack source (env var, config file, default) for debugging

### Issue 5: Hot Reload Not Detecting Changes

**Symptoms:**
- Edit pack component files but changes not reflected in agent output
- `acode prompts reload` does not pick up modifications
- Have to restart agent to see changes

**Root Causes:**
- Pack is cached and cache key (ID + hash) has not changed
- Hash of modified files not recalculated since startup
- FileSystemWatcher not monitoring correct directory in watch mode
- Cache invalidation logic not triggered by file changes

**Solutions:**
1. Run `acode prompts reload` to force cache clear and re-load
2. Restart agent to completely clear cache state
3. In watch mode, verify FileSystemWatcher is monitoring .acode/prompts/
4. Check file permissions allow reading modified files
5. Verify no file locks preventing re-read of component files

**Prevention:**
- Use watch mode (`acode run --watch`) during active development
- Document that reload command is required when not in watch mode
- Consider adding cache TTL for development builds

---

## User Verification Steps

### Scenario 1: List Available Packs

**Objective:** Verify registry discovers and indexes both built-in and user packs.

**Steps:**
1. Open terminal and navigate to workspace directory
2. Run command: `acode prompts list`
3. Observe table output showing pack ID, version, source, and status columns
4. Verify at least one built-in pack (acode-standard) appears with source "built-in"
5. If user packs exist in .acode/prompts/, verify they appear with source "user"
6. Verify exactly one pack has "active" in status column
7. Note which pack is marked active for next scenario

**Expected Output:**
```
┌───────────────────────────────────────────────────────────────────┐
│ Available Prompt Packs                                            │
├──────────────────┬─────────┬──────────┬──────────────────────────┤
│ ID               │ Version │ Source   │ Status                    │
├──────────────────┼─────────┼──────────┼──────────────────────────┤
│ acode-standard   │ 1.0.0   │ built-in │ active                    │
│ acode-dotnet     │ 1.0.0   │ built-in │                           │
│ acode-react      │ 1.0.0   │ built-in │                           │
└──────────────────┴─────────┴──────────┴──────────────────────────┘
```

**Success Criteria:**
- Command exits with code 0
- All built-in packs visible
- All user packs (if any) visible
- Exactly one pack marked active
- No error messages in output

### Scenario 2: Select Pack via Configuration File

**Objective:** Verify configuration-based pack selection works correctly.

**Steps:**
1. Open .agent/config.yml in text editor
2. Add or modify prompts section:
   ```yaml
   prompts:
     pack_id: acode-dotnet
   ```
3. Save file and close editor
4. Run command: `acode prompts list`
5. Verify "acode-dotnet" pack now has "active" in status column
6. Verify no other pack shows "active" status
7. Run command: `acode run --help` to start agent
8. Check logs for line: `Active pack: acode-dotnet v1.0.0`

**Expected Behavior:**
- Config file change immediately affects next agent run
- Configured pack is selected if it exists
- Logs clearly state which pack is active
- No restart required between config change and verification

**Success Criteria:**
- acode-dotnet shown as active in list output
- Logs confirm pack selection
- No warnings or errors about missing pack

### Scenario 3: Override Configuration with Environment Variable

**Objective:** Verify environment variable takes precedence over config file.

**Steps:**
1. Ensure .agent/config.yml still has `pack_id: acode-dotnet` from scenario 2
2. Set environment variable: `export ACODE_PROMPT_PACK=acode-react` (Linux/Mac) or `set ACODE_PROMPT_PACK=acode-react` (Windows)
3. Run command: `acode prompts list`
4. Verify "acode-react" pack shows "active" status
5. Verify "acode-dotnet" does NOT show "active" status
6. Check logs for: `Pack selection source: environment variable`
7. Unset environment variable: `unset ACODE_PROMPT_PACK` (Linux/Mac) or `set ACODE_PROMPT_PACK=` (Windows)
8. Run `acode prompts list` again
9. Verify "acode-dotnet" is active again (config file takes effect)

**Expected Behavior:**
- Environment variable overrides config file completely
- Removing environment variable reverts to config file behavior
- Precedence is clearly logged

**Success Criteria:**
- With env var set: acode-react is active
- With env var unset: acode-dotnet is active
- No errors during transitions

### Scenario 4: Fallback to Default Pack on Missing Pack

**Objective:** Verify graceful fallback when configured pack does not exist.

**Steps:**
1. Edit .agent/config.yml and set: `pack_id: nonexistent-pack-12345`
2. Save file
3. Run command: `acode prompts list`
4. Observe warning message: `Pack 'nonexistent-pack-12345' not found, falling back to 'acode-standard'`
5. Verify "acode-standard" shows "active" status in list output
6. Verify command exits with code 0 (success despite missing pack)
7. Check logs for WARNING level message about fallback
8. Restore valid pack_id in config file

**Expected Behavior:**
- Agent does not crash when pack is missing
- Clear warning message explains what happened
- Falls back to known-good default pack (acode-standard)
- Operator can diagnose misconfiguration from logs

**Success Criteria:**
- Warning message visible to user
- Fallback pack is active
- Agent remains operational
- Exit code is 0 (not error)

### Scenario 5: Validate Pack Structure and Content

**Objective:** Verify validation catches common pack errors.

**Steps:**
1. Create test pack directory: `mkdir -p .acode/prompts/test-pack`
2. Create invalid manifest with missing field:
   ```yaml
   id: test-pack
   version: 1.0.0
   # Missing: name, description, components
   variables: {}
   content_hash: abc
   ```
3. Save as .acode/prompts/test-pack/manifest.yml
4. Run command: `acode prompts validate .acode/prompts/test-pack`
5. Observe error output listing missing fields: name, description, components
6. Verify command exits with code 1 (validation failed)
7. Fix manifest by adding missing fields:
   ```yaml
   id: test-pack
   version: 1.0.0
   name: Test Pack
   description: Testing validation
   components:
     - path: system.md
       type: system
   variables: {}
   content_hash: abc123
   ```
8. Create component file: `echo "System prompt" > .acode/prompts/test-pack/system.md`
9. Run validation again: `acode prompts validate .acode/prompts/test-pack`
10. Verify command exits with code 0 (success) and shows "✓" checkmarks

**Expected Output (first validation):**
```
Validating pack 'test-pack'...
✗ Manifest error [VAL-001]: missing required field 'name'
✗ Manifest error [VAL-001]: missing required field 'description'
✗ Manifest error [VAL-001]: missing required field 'components'

Pack 'test-pack' has 3 errors
```

**Expected Output (second validation):**
```
Validating pack 'test-pack'...
✓ Manifest valid
✓ Components found (1)
✓ Template variables valid
✓ Size within limits

Pack 'test-pack' is valid
```

**Success Criteria:**
- Invalid pack validation exits code 1
- All errors listed with codes
- Valid pack validation exits code 0
- Success criteria clearly shown

### Scenario 6: Hot Reload Pack Registry

**Objective:** Verify refresh command detects newly added packs.

**Steps:**
1. Run `acode prompts list` and count number of packs shown
2. Create new pack directory: `mkdir -p .acode/prompts/new-pack`
3. Create minimal valid manifest:
   ```yaml
   id: new-pack
   version: 1.0.0
   name: New Pack
   description: Added during session
   components:
     - path: system.md
       type: system
   variables: {}
   content_hash: abc
   ```
4. Save as .acode/prompts/new-pack/manifest.yml
5. Create component: `echo "New system" > .acode/prompts/new-pack/system.md`
6. WITHOUT restarting agent, run: `acode prompts reload`
7. Observe output: `Refreshing pack registry... Found X packs (Y built-in, Z user)`
8. Run `acode prompts list` again
9. Verify "new-pack" now appears in list with source "user"
10. Verify pack count increased by 1

**Expected Behavior:**
- New packs discovered without restarting agent
- Reload command provides feedback on discovery results
- New pack immediately available for use

**Success Criteria:**
- Pack count increases by 1 after reload
- new-pack visible in list
- No errors during reload

### Scenario 7: User Pack Overrides Built-in Pack

**Objective:** Verify user packs take precedence over built-in packs with same ID.

**Steps:**
1. Create user pack with same ID as built-in: `mkdir -p .acode/prompts/acode-standard`
2. Create manifest with different version:
   ```yaml
   id: acode-standard
   version: 99.0.0
   name: User Override Pack
   description: User version overrides built-in
   components:
     - path: system.md
       type: system
   variables: {}
   content_hash: override
   ```
3. Save as .acode/prompts/acode-standard/manifest.yml
4. Create component: `echo "Custom system" > .acode/prompts/acode-standard/system.md`
5. Run `acode prompts reload`
6. Run `acode prompts list`
7. Find "acode-standard" row and verify:
   - Version shows "99.0.0" (user version, not built-in 1.0.0)
   - Source shows "user" (not "built-in")
8. Run `acode prompts show acode-standard`
9. Verify output shows "User Override Pack" as name

**Expected Behavior:**
- User pack completely replaces built-in pack with same ID
- No indication built-in pack exists (hidden by user override)
- Version and name from user pack displayed

**Success Criteria:**
- acode-standard shows source "user"
- Version is 99.0.0
- Name is "User Override Pack"

### Scenario 8: Show Pack Details and Components

**Objective:** Verify detailed pack inspection shows all metadata and components.

**Steps:**
1. Run command: `acode prompts show acode-standard`
2. Verify output includes:
   - Pack ID (acode-standard)
   - Version number
   - Source (built-in or user)
   - Description text
   - Complete list of all component files
3. For each component, verify output shows:
   - Component file path
   - Component type (system, role, language, etc.)
   - Role name (if type is role)
4. Count number of components shown and verify matches manifest

**Expected Output:**
```
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

**Success Criteria:**
- All metadata fields displayed
- All components listed with paths
- Component types shown correctly
- No truncation or missing data

### Scenario 9: Verify Hash Mismatch Warning (Development Workflow)

**Objective:** Verify editing pack files triggers hash mismatch warning but does not fail.

**Steps:**
1. Navigate to .acode/prompts/new-pack/ (from scenario 6)
2. Edit system.md file: `echo "Modified content" > .acode/prompts/new-pack/system.md`
3. Note: Do NOT regenerate content_hash in manifest.yml
4. Run `acode prompts reload`
5. Check logs or stderr for warning: `Content hash mismatch - pack may have been modified`
6. Run `acode prompts list`
7. Verify "new-pack" still appears (not rejected due to hash mismatch)
8. Run agent with new-pack active
9. Verify agent uses modified prompt content (hash mismatch did not prevent loading)

**Expected Behavior:**
- Warning logged but pack still loads successfully
- Modified content is used despite hash mismatch
- Enables development workflow without constant hash regeneration

**Success Criteria:**
- Warning message visible in logs
- Pack loads successfully
- Modified content is active

### Scenario 10: Verify Performance Characteristics

**Objective:** Verify pack operations complete within documented time limits.

**Steps:**
1. Prepare performance test pack with 20 components of 5KB each
2. Measure pack loading time: `time acode prompts load-test {pack-path}`
3. Verify load completes in under 100ms
4. Measure validation time: `time acode prompts validate {pack-path}`
5. Verify validation completes in under 100ms
6. Measure registry init: Clear cache, restart agent, measure time to first pack access
7. Verify registry init completes in under 500ms
8. Measure cache lookup: Use profiling tool to measure GetActivePack() hot path
9. Verify cache lookup completes in under 1ms

**Success Criteria:**
- All performance targets met
- No performance degradation with typical pack sizes
- Cache provides measurable speedup over disk I/O

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