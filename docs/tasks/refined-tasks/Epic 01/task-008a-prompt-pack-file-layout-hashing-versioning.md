# Task 008.a: Prompt Pack File Layout + Hashing/Versioning

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 008, Task 002 (.agent/config.yml)  

---

## Description

Task 008.a defines the file layout, directory structure, content hashing, and versioning scheme for prompt packs. This subtask establishes the physical organization that enables prompt packs to be stored, validated, versioned, and distributed. A well-defined file layout is essential for tooling, validation, and user comprehension.

Prompt packs are directories containing markdown files organized in a conventional structure. The structure is predictable—users and tools know exactly where to find system prompts, role-specific instructions, and language-specific patterns. This predictability reduces cognitive load and enables automation.

The manifest file (manifest.yml) is the central metadata file for each pack. It describes the pack's identity, version, components, and content hash. The manifest enables tooling to understand pack contents without parsing every file. It also enables integrity verification through content hashing.

Content hashing provides integrity verification. When a pack is loaded, the loader computes a hash of all component files and compares it against the manifest's recorded hash. A mismatch indicates that files have been modified since the hash was generated. This catches accidental corruption and unauthorized modifications.

Versioning follows semantic versioning (SemVer). The pack version indicates compatibility—major version changes indicate breaking changes, minor versions add features, patch versions fix bugs. Version information enables users to understand pack evolution and enables tooling to handle version migration.

The file layout supports multiple pack sources. Built-in packs are embedded in the application assembly. User packs reside in `.acode/prompts/` within the workspace. The loader discovers packs from all sources and presents a unified view through the registry.

Component files use markdown format. Markdown is human-readable, version-controllable, and supports rich formatting. Component types (system, role, language, framework, custom) are identified by their location in the directory structure and by metadata in the manifest.

File naming follows conventions for discoverability. Role prompts are named after their role (planner.md, coder.md, reviewer.md). Language prompts are named after their language (csharp.md, typescript.md). This consistency enables both human navigation and programmatic discovery.

The layout supports extension. Custom component types can be added by placing files in appropriately named directories. The manifest lists all components explicitly, so custom components are fully supported without layout changes.

Path handling is cross-platform. Paths in the manifest use forward slashes regardless of operating system. The loader normalizes paths when reading files. This ensures packs are portable between Windows, Linux, and macOS.

The format version field in the manifest enables schema evolution. The current format version is 1.0. Future versions can add fields or change structure while maintaining backward compatibility through version-aware loaders.

Hash generation is deterministic. The same pack contents always produce the same hash, regardless of file order or platform. This determinism is achieved by sorting component paths alphabetically before hashing and using consistent line endings during hash computation.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Pack Directory | Root folder containing pack files |
| manifest.yml | Metadata file describing pack |
| Content Hash | SHA-256 hash of all component content |
| Format Version | Schema version of manifest format |
| Pack Version | Semantic version of pack content |
| Pack ID | Unique identifier for pack |
| Component | Individual prompt file in pack |
| Component Type | Category: system, role, language, framework, custom |
| Component Path | Relative path from pack root |
| Built-in Pack | Pack embedded in application |
| User Pack | Pack in workspace .acode/prompts |
| SemVer | Semantic versioning scheme |
| Major Version | Breaking change indicator |
| Minor Version | Feature addition indicator |
| Patch Version | Bug fix indicator |
| Pre-release | Development version suffix |
| Hash Algorithm | SHA-256 for content hashing |
| Normalized Path | Forward-slash path format |
| Line Ending | LF for hash computation |

---

## Out of Scope

The following items are explicitly excluded from Task 008.a:

- **Pack loading logic** - Covered in Task 008.b
- **Pack validation logic** - Covered in Task 008.b
- **Pack selection from config** - Covered in Task 008.b
- **Starter pack content** - Covered in Task 008.c
- **Template variable substitution** - Covered in Task 008
- **Prompt composition** - Covered in Task 008
- **Remote pack repositories** - Not in MVP
- **Pack encryption** - Not in MVP
- **Pack signing** - Not in MVP
- **Binary attachments** - Text only

---

## Functional Requirements

### Directory Structure

- FR-001: Pack MUST be a directory at root level
- FR-002: Pack directory name MUST match pack ID
- FR-003: Pack MUST contain manifest.yml at root
- FR-004: Pack MAY contain system.md at root
- FR-005: Pack MAY contain roles/ subdirectory
- FR-006: Pack MAY contain languages/ subdirectory
- FR-007: Pack MAY contain frameworks/ subdirectory
- FR-008: Pack MAY contain custom/ subdirectory
- FR-009: Directory names MUST be lowercase
- FR-010: File names MUST be lowercase
- FR-011: File extension MUST be .md for prompts
- FR-012: File extension MUST be .yml for manifest

### Standard Directory Layout

- FR-013: roles/ MUST contain role-specific prompts
- FR-014: Role files MUST be named {role}.md
- FR-015: Standard roles: planner.md, coder.md, reviewer.md
- FR-016: languages/ MUST contain language prompts
- FR-017: Language files MUST be named {language}.md
- FR-018: frameworks/ MUST contain framework prompts
- FR-019: Framework files MUST be named {framework}.md
- FR-020: custom/ MAY contain user-defined prompts
- FR-021: Subdirectory nesting MUST NOT exceed 2 levels

### Manifest Schema

- FR-022: Manifest MUST be valid YAML 1.2
- FR-023: Manifest MUST have format_version field (string)
- FR-024: Current format_version MUST be "1.0"
- FR-025: Manifest MUST have id field (string)
- FR-026: id MUST match pack directory name
- FR-027: id MUST be lowercase alphanumeric with hyphens
- FR-028: id MUST be 3-50 characters
- FR-029: Manifest MUST have version field (string)
- FR-030: version MUST be valid SemVer 2.0
- FR-031: Manifest MUST have name field (string)
- FR-032: name MUST be 3-100 characters
- FR-033: Manifest MUST have description field (string)
- FR-034: description MUST be 10-500 characters
- FR-035: Manifest MUST have content_hash field (string)
- FR-036: Manifest MUST have created_at field (ISO 8601)
- FR-037: Manifest MAY have updated_at field (ISO 8601)
- FR-038: Manifest MAY have author field (string)
- FR-039: Manifest MUST have components array

### Component Entry Schema

- FR-040: Each component MUST have path field
- FR-041: path MUST use forward slashes
- FR-042: path MUST be relative to pack root
- FR-043: path MUST NOT start with /
- FR-044: path MUST NOT contain ..
- FR-045: Each component MUST have type field
- FR-046: type MUST be: system, role, language, framework, custom
- FR-047: Component MAY have metadata object
- FR-048: Role type MUST have role in metadata
- FR-049: Language type MUST have language in metadata
- FR-050: Framework type MUST have framework in metadata

### Content Hashing

- FR-051: Hash algorithm MUST be SHA-256
- FR-052: Hash MUST be lowercase hex-encoded
- FR-053: Hash MUST be 64 characters
- FR-054: Hash computation MUST sort paths alphabetically
- FR-055: Hash computation MUST normalize line endings to LF
- FR-056: Hash computation MUST use UTF-8 encoding
- FR-057: Hash MUST include all component file contents
- FR-058: Hash MUST NOT include manifest.yml
- FR-059: Hash input MUST be: sorted paths + contents concatenated
- FR-060: Hash MUST be deterministic across platforms
- FR-061: Hash regeneration MUST update manifest

### Versioning

- FR-062: Version MUST follow SemVer 2.0.0
- FR-063: Version MUST have MAJOR.MINOR.PATCH format
- FR-064: Version MAY have pre-release suffix (-alpha.1)
- FR-065: Version MAY have build metadata (+build.123)
- FR-066: MAJOR MUST increment for breaking changes
- FR-067: MINOR MUST increment for new features
- FR-068: PATCH MUST increment for bug fixes
- FR-069: Version comparison MUST follow SemVer rules
- FR-070: Version MUST be unique within pack history

### Built-in Pack Location

- FR-071: Built-in packs MUST be in embedded resources
- FR-072: Embedded resource path: Resources/PromptPacks/{id}/
- FR-073: Built-in packs MUST be extractable to temp
- FR-074: Extraction MUST preserve directory structure
- FR-075: Extraction MUST be atomic

### User Pack Location

- FR-076: User packs MUST be in .acode/prompts/
- FR-077: Path: {workspace}/.acode/prompts/{pack-id}/
- FR-078: User packs take precedence over built-in
- FR-079: User pack directory MUST exist before use
- FR-080: Missing user pack directory is not error

### Path Normalization

- FR-081: Paths in manifest MUST use forward slashes
- FR-082: Paths MUST be normalized on read
- FR-083: Normalization MUST handle backslashes
- FR-084: Normalization MUST remove trailing slashes
- FR-085: Normalization MUST collapse multiple slashes
- FR-086: Path validation MUST reject traversal attempts

---

## Non-Functional Requirements

### Performance

- NFR-001: Hash computation MUST complete in < 50ms for 1MB
- NFR-002: Directory scan MUST complete in < 100ms
- NFR-003: Manifest parsing MUST complete in < 10ms
- NFR-004: Memory for pack metadata MUST be < 100KB

### Reliability

- NFR-005: File operations MUST handle locked files
- NFR-006: Unicode filenames MUST be supported
- NFR-007: Large files (>1MB) MUST be rejected
- NFR-008: Hash MUST be stable across platforms

### Security

- NFR-009: Path traversal MUST be prevented
- NFR-010: Symlinks MUST be rejected
- NFR-011: Hidden files MUST be skipped
- NFR-012: Executable permissions ignored

### Compatibility

- NFR-013: Windows paths MUST work
- NFR-014: Linux paths MUST work
- NFR-015: macOS paths MUST work
- NFR-016: Git line endings MUST be handled

### Maintainability

- NFR-017: Format version enables migration
- NFR-018: Schema changes documented
- NFR-019: Backward compatibility for minor versions
- NFR-020: Deprecation warnings for old formats

---

## User Manual Documentation

### Overview

Prompt packs use a standardized directory layout for organization, versioning, and integrity verification. This document describes the file structure, manifest format, and hashing scheme.

### Directory Structure

```
my-pack/
├── manifest.yml              # Required: pack metadata
├── system.md                 # Optional: base system prompt
├── roles/                    # Optional: role-specific prompts
│   ├── planner.md
│   ├── coder.md
│   └── reviewer.md
├── languages/                # Optional: language prompts
│   ├── csharp.md
│   ├── typescript.md
│   ├── python.md
│   └── go.md
├── frameworks/               # Optional: framework prompts
│   ├── aspnetcore.md
│   ├── react.md
│   ├── nextjs.md
│   └── fastapi.md
└── custom/                   # Optional: user-defined prompts
    ├── team-rules.md
    └── code-style.md
```

### Pack Locations

**Built-in packs:** Embedded in application, always available.

**User packs:** Store in workspace:
```
{workspace}/
└── .acode/
    └── prompts/
        └── my-pack/
            ├── manifest.yml
            └── ...
```

### Manifest Format

```yaml
# manifest.yml - Complete example
format_version: "1.0"
id: my-custom-pack
version: 1.2.3
name: My Custom Pack
description: Customized prompts for .NET microservices development
author: Backend Team
created_at: 2024-01-15T10:30:00Z
updated_at: 2024-02-20T14:45:00Z
content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef12345

components:
  - path: system.md
    type: system
    
  - path: roles/planner.md
    type: role
    metadata:
      role: planner
      
  - path: roles/coder.md
    type: role
    metadata:
      role: coder
      
  - path: languages/csharp.md
    type: language
    metadata:
      language: csharp
      version: "12"
      
  - path: frameworks/aspnetcore.md
    type: framework
    metadata:
      framework: aspnetcore
      version: "8.0"
```

### Field Reference

| Field | Required | Type | Description |
|-------|----------|------|-------------|
| format_version | Yes | String | Schema version, currently "1.0" |
| id | Yes | String | Unique identifier, lowercase with hyphens |
| version | Yes | String | SemVer version (e.g., "1.0.0") |
| name | Yes | String | Display name (3-100 chars) |
| description | Yes | String | Pack description (10-500 chars) |
| content_hash | Yes | String | SHA-256 hash of components |
| created_at | Yes | String | ISO 8601 timestamp |
| updated_at | No | String | ISO 8601 timestamp |
| author | No | String | Pack author name |
| components | Yes | Array | List of component entries |

### Component Types

| Type | Location | Purpose |
|------|----------|---------|
| system | system.md | Base system prompt |
| role | roles/*.md | Role-specific instructions |
| language | languages/*.md | Language conventions |
| framework | frameworks/*.md | Framework patterns |
| custom | custom/*.md | User-defined prompts |

### Versioning Scheme

Packs use Semantic Versioning 2.0.0:

```
MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]
```

**Examples:**
- `1.0.0` - Initial release
- `1.1.0` - Added TypeScript language prompt
- `1.1.1` - Fixed typo in coder role
- `2.0.0` - Breaking change to prompt structure
- `2.0.0-beta.1` - Pre-release version
- `2.0.0+build.456` - With build metadata

**Version increment rules:**
- MAJOR: Breaking changes to prompt behavior
- MINOR: New prompts or non-breaking enhancements
- PATCH: Bug fixes, typo corrections

### Content Hashing

The content hash ensures pack integrity.

**Hash computation:**
1. List all component files
2. Sort paths alphabetically
3. For each file: normalize line endings to LF
4. Concatenate: path + newline + content + newline
5. Compute SHA-256 of concatenated content
6. Encode as lowercase hex

**Example hash computation:**
```
Input (sorted, concatenated):
  languages/csharp.md\n{content}\n
  roles/coder.md\n{content}\n
  system.md\n{content}\n

Output:
  a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef12345
```

**Regenerate hash:**
```bash
$ acode prompts hash .acode/prompts/my-pack
Computing hash for 5 components...
Hash updated: a1b2c3d4...
```

### Creating a Pack

1. **Create directory:**
   ```bash
   mkdir -p .acode/prompts/my-pack
   ```

2. **Create manifest:**
   ```yaml
   # .acode/prompts/my-pack/manifest.yml
   format_version: "1.0"
   id: my-pack
   version: 1.0.0
   name: My Pack
   description: Custom prompts for my project
   created_at: 2024-01-15T10:00:00Z
   content_hash: ""  # Will be generated
   components: []    # Will be populated
   ```

3. **Add system prompt:**
   ```markdown
   <!-- .acode/prompts/my-pack/system.md -->
   
   You are an AI coding assistant...
   ```

4. **Add role prompts:**
   ```bash
   mkdir .acode/prompts/my-pack/roles
   ```
   
   ```markdown
   <!-- .acode/prompts/my-pack/roles/coder.md -->
   
   As the coder, you implement features...
   ```

5. **Update manifest components:**
   ```yaml
   components:
     - path: system.md
       type: system
     - path: roles/coder.md
       type: role
       metadata:
         role: coder
   ```

6. **Generate hash:**
   ```bash
   acode prompts hash .acode/prompts/my-pack
   ```

7. **Validate:**
   ```bash
   acode prompts validate .acode/prompts/my-pack
   ```

### Naming Conventions

**Pack IDs:**
- Lowercase letters, numbers, hyphens
- 3-50 characters
- Must start with letter
- Examples: `my-pack`, `team-dotnet-v2`, `acode-standard`

**File names:**
- Lowercase
- Use hyphens for multi-word names
- Examples: `coder.md`, `aspnet-core.md`, `team-rules.md`

**Standard role names:**
- `planner.md` - Task planning role
- `coder.md` - Implementation role
- `reviewer.md` - Code review role

**Standard language names:**
- `csharp.md`, `typescript.md`, `python.md`, `go.md`, `rust.md`

**Standard framework names:**
- `aspnetcore.md`, `react.md`, `nextjs.md`, `angular.md`, `fastapi.md`

### Path Rules

- Use forward slashes: `roles/coder.md`
- Relative to pack root
- No leading slash: ✓ `roles/coder.md` ✗ `/roles/coder.md`
- No parent references: ✗ `../other/file.md`
- Max depth: 2 levels
- Case-sensitive matching

### Troubleshooting

**Hash mismatch:**
```
Warning: Content hash mismatch for pack 'my-pack'
  Expected: a1b2c3d4...
  Computed: e5f6a7b8...
```
Regenerate: `acode prompts hash .acode/prompts/my-pack`

**Component not found:**
```
Error: Component file not found: roles/analyst.md
```
Ensure file exists and path in manifest matches.

**Invalid pack ID:**
```
Error: Invalid pack ID 'My Pack' - must be lowercase with hyphens
```
Use: `my-pack` instead of `My Pack`.

---

## Acceptance Criteria

### Directory Structure

- [ ] AC-001: Pack is directory at root
- [ ] AC-002: Directory name matches ID
- [ ] AC-003: manifest.yml required at root
- [ ] AC-004: system.md optional at root
- [ ] AC-005: roles/ subdirectory works
- [ ] AC-006: languages/ subdirectory works
- [ ] AC-007: frameworks/ subdirectory works
- [ ] AC-008: custom/ subdirectory works
- [ ] AC-009: Directory names lowercase
- [ ] AC-010: File names lowercase
- [ ] AC-011: .md extension for prompts
- [ ] AC-012: .yml extension for manifest

### Manifest Schema

- [ ] AC-013: Valid YAML 1.2
- [ ] AC-014: format_version present
- [ ] AC-015: format_version is "1.0"
- [ ] AC-016: id present
- [ ] AC-017: id matches directory
- [ ] AC-018: id is valid format
- [ ] AC-019: version present
- [ ] AC-020: version is SemVer
- [ ] AC-021: name present
- [ ] AC-022: name 3-100 chars
- [ ] AC-023: description present
- [ ] AC-024: description 10-500 chars
- [ ] AC-025: content_hash present
- [ ] AC-026: created_at present
- [ ] AC-027: created_at is ISO 8601
- [ ] AC-028: components array present

### Component Entries

- [ ] AC-029: path field present
- [ ] AC-030: path uses forward slashes
- [ ] AC-031: path is relative
- [ ] AC-032: path no leading slash
- [ ] AC-033: path no traversal
- [ ] AC-034: type field present
- [ ] AC-035: type is valid value
- [ ] AC-036: role metadata for role type
- [ ] AC-037: language metadata for language type
- [ ] AC-038: framework metadata for framework type

### Content Hashing

- [ ] AC-039: SHA-256 algorithm used
- [ ] AC-040: Hash is lowercase hex
- [ ] AC-041: Hash is 64 characters
- [ ] AC-042: Paths sorted alphabetically
- [ ] AC-043: Line endings normalized to LF
- [ ] AC-044: UTF-8 encoding used
- [ ] AC-045: All components included
- [ ] AC-046: Manifest excluded from hash
- [ ] AC-047: Hash is deterministic
- [ ] AC-048: Cross-platform stability

### Versioning

- [ ] AC-049: SemVer 2.0.0 format
- [ ] AC-050: MAJOR.MINOR.PATCH format
- [ ] AC-051: Pre-release suffix works
- [ ] AC-052: Build metadata works
- [ ] AC-053: Version comparison works

### Pack Locations

- [ ] AC-054: Built-in packs in resources
- [ ] AC-055: User packs in .acode/prompts/
- [ ] AC-056: User packs override built-in
- [ ] AC-057: Missing directory not error

### Path Handling

- [ ] AC-058: Forward slashes in manifest
- [ ] AC-059: Path normalization works
- [ ] AC-060: Backslash handling
- [ ] AC-061: Trailing slash removal
- [ ] AC-062: Multiple slash collapse
- [ ] AC-063: Traversal rejected

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Domain/PromptPacks/
├── PackManifestTests.cs
│   ├── Should_Parse_Valid_Manifest()
│   ├── Should_Reject_Invalid_Format_Version()
│   ├── Should_Validate_Pack_Id_Format()
│   └── Should_Parse_SemVer_Version()
│
├── ContentHasherTests.cs
│   ├── Should_Compute_SHA256_Hash()
│   ├── Should_Sort_Paths_Alphabetically()
│   ├── Should_Normalize_Line_Endings()
│   ├── Should_Be_Deterministic()
│   └── Should_Exclude_Manifest()
│
├── ComponentPathTests.cs
│   ├── Should_Normalize_Forward_Slashes()
│   ├── Should_Reject_Traversal_Paths()
│   ├── Should_Reject_Absolute_Paths()
│   └── Should_Handle_Unicode_Paths()
│
└── SemVerTests.cs
    ├── Should_Parse_Major_Minor_Patch()
    ├── Should_Parse_PreRelease()
    ├── Should_Compare_Versions()
    └── Should_Sort_Versions()
```

### Integration Tests

```
Tests/Integration/PromptPacks/
├── PackDiscoveryTests.cs
│   ├── Should_Find_BuiltIn_Packs()
│   ├── Should_Find_User_Packs()
│   └── Should_Prioritize_User_Packs()
│
└── HashVerificationTests.cs
    ├── Should_Verify_Valid_Hash()
    ├── Should_Detect_Modified_Content()
    └── Should_Regenerate_Hash()
```

### E2E Tests

```
Tests/E2E/PromptPacks/
├── PackCreationE2ETests.cs
│   ├── Should_Create_Pack_Directory()
│   ├── Should_Generate_Manifest()
│   └── Should_Compute_Hash()
```

### Performance Tests

- PERF-001: Hash 1MB content < 50ms
- PERF-002: Parse manifest < 10ms
- PERF-003: Scan directory < 100ms

---

## User Verification Steps

### Scenario 1: Create Pack Directory

1. Create `.acode/prompts/my-pack/`
2. Add manifest.yml with required fields
3. Verify: Directory structure accepted

### Scenario 2: Add Components

1. Create roles/coder.md
2. Update manifest components
3. Verify: Component listed

### Scenario 3: Generate Hash

1. Run `acode prompts hash my-pack`
2. Verify: content_hash updated

### Scenario 4: Verify Hash

1. Modify component file
2. Load pack
3. Verify: Hash mismatch warning

### Scenario 5: Version Format

1. Set version "1.2.3-beta.1"
2. Validate pack
3. Verify: Version accepted

### Scenario 6: Path Normalization

1. Use backslashes in manifest
2. Load on Linux
3. Verify: Paths normalized

### Scenario 7: Invalid ID

1. Set id "My Pack" (spaces)
2. Validate pack
3. Verify: Error reported

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/PromptPacks/
├── PackManifest.cs
├── PackComponent.cs
├── ComponentType.cs
├── ContentHash.cs
└── PackVersion.cs

src/AgenticCoder.Infrastructure/PromptPacks/
├── ContentHasher.cs
├── ManifestParser.cs
├── PathNormalizer.cs
└── PackDiscovery.cs
```

### PackManifest Class

```csharp
namespace AgenticCoder.Domain.PromptPacks;

public sealed class PackManifest
{
    public required string FormatVersion { get; init; }
    public required string Id { get; init; }
    public required PackVersion Version { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required ContentHash ContentHash { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? Author { get; init; }
    public required IReadOnlyList<PackComponent> Components { get; init; }
}
```

### ContentHash Class

```csharp
namespace AgenticCoder.Domain.PromptPacks;

public sealed class ContentHash
{
    private readonly string _value;
    
    public ContentHash(string value)
    {
        if (value.Length != 64)
            throw new ArgumentException("Hash must be 64 hex characters");
        if (!value.All(c => char.IsAsciiHexDigitLower(c)))
            throw new ArgumentException("Hash must be lowercase hex");
        _value = value;
    }
    
    public static ContentHash Compute(IEnumerable<(string Path, string Content)> components);
    public bool Matches(ContentHash other);
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-PKL-001 | Invalid manifest YAML |
| ACODE-PKL-002 | Missing required field |
| ACODE-PKL-003 | Invalid format version |
| ACODE-PKL-004 | Invalid pack ID format |
| ACODE-PKL-005 | Invalid SemVer version |
| ACODE-PKL-006 | Component file not found |
| ACODE-PKL-007 | Path traversal detected |
| ACODE-PKL-008 | Content hash mismatch |

### Implementation Checklist

1. [ ] Create PackManifest domain class
2. [ ] Create PackComponent domain class
3. [ ] Create ComponentType enum
4. [ ] Create ContentHash value object
5. [ ] Create PackVersion value object
6. [ ] Implement ManifestParser
7. [ ] Implement ContentHasher
8. [ ] Implement PathNormalizer
9. [ ] Implement PackDiscovery
10. [ ] Create embedded resource structure
11. [ ] Write unit tests
12. [ ] Write integration tests
13. [ ] Add XML documentation

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~PromptPacks"
```

---

**End of Task 008.a Specification**