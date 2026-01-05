# Task 008.a: Prompt Pack File Layout + Hashing/Versioning

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 008, Task 002 (.agent/config.yml)  

---

## Description

Task 008.a defines the file layout, directory structure, content hashing, and versioning scheme for prompt packs. This subtask establishes the physical organization that enables prompt packs to be stored, validated, versioned, and distributed. A well-defined file layout is essential for tooling, validation, and user comprehension.

### Business Value and ROI

Prompt packs are a significant productivity multiplier for development teams using Acode. By standardizing prompt organization and ensuring integrity through hashing, teams avoid the hidden costs of prompt management chaos:

**Problem Without This Feature:**
- Teams copy-paste prompts between projects, losing track of versions
- Prompt modifications go undetected, causing inconsistent agent behavior
- No way to verify a prompt pack hasn't been corrupted or tampered with
- Difficulty sharing and distributing custom prompt configurations
- Time wasted recreating prompts for each new project

**Quantified Business Impact:**
- **Prompt Management Time:** Teams spend 2.5 hours/week managing prompts manually
- **Version Confusion Incidents:** 1.2 incidents/week where wrong prompt version causes agent misbehavior (30 min to debug each = 36 min/week)
- **Prompt Recreation Time:** 1 hour/month per new project recreating standard prompts
- **Integrity Issues:** 0.5 incidents/month where corrupted prompts cause agent failures (2 hours to diagnose = 1 hour/month)

**Total Time Wasted:** 2.5 + 0.6 + 1 + 1 = 5.1 hours/developer/week

**With Standardized File Layout + Hashing:**
- **Prompt Discovery:** Structured layout reduces management time by 80% = 2 hours/week saved
- **Version Clarity:** SemVer + manifest eliminates version confusion = 36 min/week saved
- **Reusability:** Copy pack directory, prompts work immediately = 50 min/month saved
- **Integrity Verification:** Hash mismatch detection catches corruption = 50 min/month saved

**Net Savings:** 2.6 hours/developer/week × 48 weeks/year × $50/hour = **$6,240/developer/year**

For a 10-developer team: **$62,400/year** in productivity gains from standardized prompt pack infrastructure.

### Technical Architecture

The prompt pack system uses a layered architecture with clear separation between domain models, infrastructure services, and discovery mechanisms.

```
┌─────────────────────────────────────────────────────────────┐
│                     Application Layer                        │
│  ┌─────────────────────┐    ┌─────────────────────────────┐ │
│  │  PromptPackService  │    │    PackRegistryService      │ │
│  └─────────────────────┘    └─────────────────────────────┘ │
└──────────────────────────────┬──────────────────────────────┘
                               │
┌──────────────────────────────┼──────────────────────────────┐
│                     Domain Layer                             │
│  ┌───────────────┐    ┌─────────────┐    ┌───────────────┐  │
│  │ PackManifest  │    │ PackVersion │    │ ContentHash   │  │
│  │ PackComponent │    │ ComponentType│    │ ComponentPath │  │
│  └───────────────┘    └─────────────┘    └───────────────┘  │
└──────────────────────────────┬──────────────────────────────┘
                               │
┌──────────────────────────────┼──────────────────────────────┐
│                  Infrastructure Layer                        │
│  ┌────────────────┐   ┌──────────────┐   ┌───────────────┐  │
│  │ ManifestParser │   │ContentHasher │   │PathNormalizer │  │
│  ├────────────────┤   ├──────────────┤   ├───────────────┤  │
│  │ PackDiscovery  │   │ PackReader   │   │ HashValidator │  │
│  └────────────────┘   └──────────────┘   └───────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

**Components:**

1. **PackManifest (Domain):** Immutable record representing pack metadata. Contains format version, pack ID, semantic version, name, description, content hash, timestamps, author, and component list.

2. **PackComponent (Domain):** Represents a single prompt file within a pack. Contains relative path, component type, and optional metadata.

3. **ContentHash (Value Object):** Encapsulates SHA-256 hash with validation (64 lowercase hex chars). Provides deterministic computation and comparison.

4. **PackVersion (Value Object):** SemVer 2.0 implementation with comparison, pre-release support, and build metadata.

5. **ManifestParser (Infrastructure):** YAML 1.2 parser using YamlDotNet. Validates schema, converts to domain objects.

6. **ContentHasher (Infrastructure):** Computes deterministic SHA-256 hash over sorted, normalized component contents.

7. **PathNormalizer (Infrastructure):** Cross-platform path handling. Normalizes slashes, rejects traversal, validates format.

8. **PackDiscovery (Infrastructure):** Finds packs from built-in resources and user directories. Handles precedence.

### Prompt Pack Directory Structure

Prompt packs use a conventional directory layout that is both human-readable and machine-parseable:

```
{pack-id}/
├── manifest.yml          # REQUIRED: Pack metadata and component list
├── system.md             # OPTIONAL: Base system prompt
├── roles/                # OPTIONAL: Role-specific prompts
│   ├── planner.md        # Planning phase prompt
│   ├── coder.md          # Implementation phase prompt
│   └── reviewer.md       # Code review phase prompt
├── languages/            # OPTIONAL: Language-specific prompts
│   ├── csharp.md
│   ├── typescript.md
│   ├── python.md
│   ├── go.md
│   └── rust.md
├── frameworks/           # OPTIONAL: Framework-specific prompts
│   ├── aspnetcore.md
│   ├── react.md
│   ├── nextjs.md
│   ├── angular.md
│   └── fastapi.md
└── custom/               # OPTIONAL: User-defined prompts
    ├── team-rules.md
    ├── code-style.md
    └── security-rules.md
```

**Design Decisions:**

1. **Directory-Based Organization:** Each pack is a self-contained directory. This enables git versioning, easy copying, and clear boundaries.

2. **Subdirectory Categorization:** Components are organized by type (roles, languages, frameworks). This makes it easy to find and edit specific prompts.

3. **Lowercase Naming:** All file and directory names are lowercase. This ensures cross-platform compatibility (Windows is case-insensitive, Linux is case-sensitive).

4. **Hyphenated Names:** Multi-word names use hyphens (e.g., `aspnet-core.md`). This is URL-safe, filesystem-safe, and readable.

5. **Markdown Format:** All prompt files use Markdown (.md). This provides rich formatting, good editor support, and version control friendliness.

6. **Two-Level Maximum:** Subdirectories don't nest more than 2 levels deep (e.g., `roles/coder.md`). This keeps the structure flat and predictable.

### Manifest Schema Design

The manifest.yml file is the pack's source of truth. It uses YAML 1.2 for readability and provides all metadata needed for pack management:

```yaml
# Schema definition
format_version: "1.0"        # Required: Schema version for forward compatibility
id: my-pack                  # Required: Unique identifier (3-50 chars, lowercase, hyphens)
version: "1.2.3"             # Required: SemVer 2.0 version
name: "My Pack"              # Required: Display name (3-100 chars)
description: "..."           # Required: Description (10-500 chars)
content_hash: "abc123..."    # Required: SHA-256 of component contents
created_at: "2024-01-15T..."# Required: ISO 8601 creation timestamp
updated_at: "2024-02-20T..."# Optional: ISO 8601 last update timestamp
author: "Team Name"          # Optional: Pack author
components:                  # Required: List of components
  - path: system.md          # Required: Relative path
    type: system             # Required: Component type
    metadata: {}             # Optional: Type-specific metadata
```

**Format Version Rationale:** The `format_version` field enables schema evolution without breaking backward compatibility. When the manifest schema changes, the version number increments, and the parser can handle different versions appropriately.

**ID Constraints:** Pack IDs are restricted to lowercase alphanumeric with hyphens because:
- They're used as directory names (filesystem-safe)
- They're used in configuration files (YAML-safe)
- They're used in CLI commands (shell-safe)
- They enable unambiguous matching

### Content Hashing Algorithm

The content hash provides integrity verification. The algorithm is designed for determinism across platforms:

```
Algorithm: SHA-256 (deterministic content hash)

Input Preparation:
1. Collect all component files listed in manifest
2. Sort paths alphabetically (case-sensitive ASCII sort)
3. For each file in sorted order:
   a. Normalize line endings to LF (Unix-style)
   b. Encode as UTF-8
4. Concatenate: path + "\n" + content + "\n" for each file
5. Compute SHA-256 of concatenated byte array
6. Encode as 64-character lowercase hexadecimal

Properties:
- Deterministic: Same contents always produce same hash
- Cross-platform: Works identically on Windows, Linux, macOS
- Tamper-evident: Any change produces different hash
- Collision-resistant: SHA-256 provides 128-bit security
```

**Why Not Include manifest.yml in Hash?**

The manifest contains the hash itself. If we included the manifest in the hash, we'd have a circular dependency. Additionally, the manifest is metadata about the pack, not part of the pack's functional content.

**Line Ending Normalization:**

Different operating systems use different line endings (CRLF on Windows, LF on Unix). Without normalization, the same file would produce different hashes on different platforms. By normalizing to LF before hashing, we ensure cross-platform consistency.

### Semantic Versioning Implementation

Pack versions follow SemVer 2.0.0 specification:

```
Format: MAJOR.MINOR.PATCH[-PRERELEASE][+BUILDMETADATA]

Examples:
  1.0.0           - Initial stable release
  1.1.0           - Added new language prompt (minor feature)
  1.1.1           - Fixed typo in coder prompt (patch)
  2.0.0           - Restructured prompt format (breaking change)
  2.0.0-alpha.1   - Pre-release version
  2.0.0-beta.2    - Second beta
  2.0.0-rc.1      - Release candidate
  2.0.0+build.456 - With build metadata
```

**Version Increment Guidelines:**

- **MAJOR:** Breaking changes that require user action
  - Removed components
  - Renamed components
  - Changed variable syntax
  - Incompatible prompt structure changes

- **MINOR:** New features that are backward compatible
  - Added new language prompts
  - Added new role prompts
  - Enhanced existing prompts (non-breaking)

- **PATCH:** Bug fixes and corrections
  - Typo fixes
  - Grammar improvements
  - Clarifications that don't change behavior

### Pack Sources and Precedence

Prompt packs can come from multiple sources with defined precedence:

```
Source Priority (highest to lowest):
1. Workspace user packs:  {workspace}/.acode/prompts/{pack-id}/
2. Global user packs:     ~/.acode/prompts/{pack-id}/
3. Built-in packs:        [embedded in application assembly]

Precedence Rules:
- User packs override built-in packs with same ID
- Workspace packs override global user packs with same ID
- First match wins (highest priority source)
```

**Built-in Pack Storage:**

Built-in packs are embedded as .NET assembly resources. At runtime, they are extracted to a temporary directory for file-based access. The extraction is atomic (write to temp, rename) to prevent partial reads.

### Cross-Platform Path Handling

Path handling is critical for portability. The system normalizes paths at every boundary:

```
Input Path             → Normalized Path
roles\coder.md         → roles/coder.md
roles//coder.md        → roles/coder.md
roles/./coder.md       → roles/coder.md
roles/../other/file.md → REJECTED (traversal)
/roles/coder.md        → REJECTED (absolute)
```

**Normalization Rules:**
1. Replace backslashes with forward slashes
2. Collapse multiple consecutive slashes
3. Remove `.` (current directory) components
4. Reject `..` (parent directory) components
5. Reject absolute paths (leading slash)
6. Remove trailing slashes

### Error Handling Strategy

All errors use structured error codes for programmatic handling:

| Code | Severity | Category | Description |
|------|----------|----------|-------------|
| ACODE-PKL-001 | Error | Parse | Invalid YAML syntax in manifest |
| ACODE-PKL-002 | Error | Validation | Required manifest field missing |
| ACODE-PKL-003 | Error | Version | Invalid format_version value |
| ACODE-PKL-004 | Error | Validation | Pack ID format invalid |
| ACODE-PKL-005 | Error | Version | SemVer version parse failed |
| ACODE-PKL-006 | Error | Filesystem | Component file not found |
| ACODE-PKL-007 | Error | Security | Path traversal attempt detected |
| ACODE-PKL-008 | Warning | Integrity | Content hash mismatch |
| ACODE-PKL-009 | Error | Size | Component file exceeds 1MB |
| ACODE-PKL-010 | Error | Validation | Duplicate component path |

### Integration Points

This task integrates with:

1. **Task 008 (Prompt Pack System):** Parent task defining overall pack system
2. **Task 008.b (Loader/Validator):** Uses layout for loading and validation
3. **Task 002 (Configuration):** Reads pack locations from config
4. **Task 010 (CLI):** Provides `acode prompts` commands

### Constraints and Limitations

1. **Maximum Component Size:** 1MB per component file
2. **Maximum Nesting Depth:** 2 levels (e.g., `roles/planner.md`)
3. **Maximum Pack Size:** 10MB total (all components)
4. **Maximum Components:** 100 components per pack
5. **No Binary Files:** Only UTF-8 text (Markdown)
6. **No Symlinks:** Symbolic links are rejected for security
7. **No Hidden Files:** Files starting with `.` are ignored

---

## Use Cases

### Use Case 1: DevBot (AI Agent) Loads and Verifies Pack Integrity

**Scenario:** DevBot is starting a new coding session and needs to load the configured prompt pack. The pack was last modified 2 weeks ago by a team member. DevBot must verify the pack hasn't been corrupted or tampered with before using it.

**Before (Without Hashing/Versioning):**
DevBot loads prompt files directly from the `.acode/prompts/` directory. There's no way to verify the files are intact. Last week, a developer accidentally saved their scratch notes over `roles/coder.md`. DevBot uses the corrupted prompt and produces confused, unhelpful responses. The team spends 3 hours debugging before discovering the corrupted file. They restore from git, but lose trust in the prompt system.

**After (With Content Hashing):**
DevBot loads the pack manifest and computes the content hash of all components. The manifest contains `content_hash: abc123...`. DevBot computes the current hash: `def456...`. **Mismatch detected.** DevBot logs warning: `ACODE-PKL-008: Content hash mismatch for pack 'team-dotnet'. Expected abc123..., computed def456...`. The developer is alerted immediately, inspects `roles/coder.md`, discovers the corruption, and restores from git. **Total time: 5 minutes instead of 3 hours.**

**Business Impact:**
- **Time Saved:** 2.9 hours per corruption incident
- **Incidents Prevented:** 0.5/month = 1.45 hours/month saved
- **Trust:** Team confidence in prompt consistency

---

### Use Case 2: Jordan (System Admin) Distributes Pack to Multiple Projects

**Scenario:** Jordan has created a standardized prompt pack for the .NET team (50 developers across 12 projects). Jordan needs to distribute the pack and ensure all projects use the exact same version.

**Before (Without Standardized Layout):**
Jordan emails prompt files to team leads. Each lead copies files into their project differently. Some projects have `prompts/system.md`, others have `prompts/base/system.md`. Version tracking is manual—Jordan maintains a spreadsheet mapping projects to versions. When Jordan updates the pack, they email a new ZIP file. Adoption is inconsistent; 3 months later, projects use 4 different versions. Debugging cross-project issues is impossible because prompts differ.

**After (With Pack Layout + Versioning):**
Jordan creates `.acode/prompts/team-dotnet/` with:
```yaml
id: team-dotnet
version: 2.3.1
content_hash: abc123...
```
Jordan publishes the pack to an internal git repository. Each project adds:
```yaml
prompt_pack:
  id: team-dotnet
  source: git@internal:prompts/team-dotnet.git
  version: ">=2.3.0 <3.0.0"
```
Projects automatically pull updates. Jordan checks version distribution: `acode prompts audit` shows 48/50 projects on 2.3.1, 2 on 2.3.0 (pending CI run). **All projects are within compatible range.**

**Business Impact:**
- **Distribution Time:** 4 hours/update → 15 minutes = 3.75 hours saved/update
- **Version Consistency:** 100% of projects on compatible versions
- **Audit Capability:** Instant visibility into version distribution

---

### Use Case 3: Alex (Developer) Creates Custom Pack from Standard Pack

**Scenario:** Alex's team has specialized requirements for their security-focused project. They need the standard `team-dotnet` pack but with additional security-focused prompts and modifications to the coder role.

**Before (Without Pack Structure):**
Alex copies random prompt files into the project. There's no clear structure—files are in `docs/prompts/`, `scripts/ai-prompts/`, and `.config/prompts/`. When the team updates the base pack, Alex manually merges changes, missing some updates. The pack has no version number, so there's no way to know if it's based on team-dotnet v2.3.1 or v2.1.0.

**After (With Standard Layout):**
Alex creates `.acode/prompts/team-security/`:
```yaml
id: team-security
version: 1.0.0
name: Team Security Pack
description: Extended pack for security-focused development
author: Alex
inherits: team-dotnet@2.3.1  # Future feature: inheritance
created_at: 2024-01-15T10:00:00Z
components:
  - path: system.md
    type: system
  - path: roles/coder.md
    type: role
    metadata:
      role: coder
      extends: team-dotnet/roles/coder.md  # Future: extension
  - path: custom/security-rules.md
    type: custom
```
Alex regenerates the hash: `acode prompts hash team-security`. The pack is versioned, structured, and can be tracked independently. When team-dotnet updates to 2.4.0, Alex can diff the changes and selectively incorporate them.

**Business Impact:**
- **Customization Clarity:** Clear tracking of custom vs base content
- **Upgrade Path:** Structured diffs between versions
- **Reusability:** Other security projects can fork team-security

---

## Assumptions

### Technical Assumptions

1. **Filesystem Access:** The application has read access to `.acode/prompts/` directory and write access for hash regeneration.

2. **UTF-8 Encoding:** All prompt files use UTF-8 encoding. Other encodings will cause hash computation errors.

3. **YAML 1.2 Support:** The YamlDotNet library (version 13.x+) is available for manifest parsing.

4. **SHA-256 Availability:** .NET's `System.Security.Cryptography.SHA256` is available (included in .NET 8).

5. **Case-Sensitive Path Handling:** Pack IDs and component paths are case-sensitive internally, even on case-insensitive filesystems.

6. **Embedded Resources Support:** Built-in packs are embedded using MSBuild `<EmbeddedResource>` elements.

7. **Temporary Directory Access:** The application can write to system temp directory for built-in pack extraction.

### Operational Assumptions

8. **Pack Directory Exists:** The `.acode/prompts/` directory is created by the user or `acode init` command before pack operations.

9. **Network Not Required:** Pack loading is fully offline. No network calls for local packs.

10. **Git Version Control:** Users version control their packs with git for history and collaboration.

11. **Single-Machine Scope:** Packs are not shared across machines automatically (requires git or manual copy).

12. **No Concurrent Writes:** Only one process writes to a pack at a time. Concurrent writes may corrupt the pack.

### Integration Assumptions

13. **Task 008 Complete:** The parent Prompt Pack System task provides the registry interface.

14. **Task 002 Complete:** Configuration service provides pack locations from `.agent/config.yml`.

15. **Task 010 Available:** CLI framework provides `acode prompts` command infrastructure.

16. **Logging Available:** ILogger infrastructure is available for structured logging.

### Resource Assumptions

17. **Memory for Hashing:** Up to 10MB can be loaded into memory for hash computation.

18. **Disk Space for Extraction:** Up to 50MB temporary space for built-in pack extraction.

19. **Performance Budget:** Pack loading adds < 100ms to startup time.

20. **Manifest Parse Time:** Manifests parse in < 10ms for typical pack sizes.

---

## Security Considerations

### Threat 1: Path Traversal Attack

**Description:** Malicious manifest contains component path that escapes the pack directory (e.g., `../../../etc/passwd`). Loading this component reads files outside the pack.

**Attack Vector:**
```yaml
components:
  - path: ../../../home/user/.ssh/id_rsa
    type: custom
```

**Impact:** Information disclosure. Attacker reads sensitive files.

**Likelihood:** Medium (requires user to load malicious pack).

**Mitigation:**
1. **Path Validation:** Reject any path containing `..`
2. **Canonical Path Check:** Resolve path and verify it's under pack root
3. **Jail Enforcement:** All file reads use safe reader that validates paths
4. **Symlink Rejection:** Don't follow symlinks

**Implementation:**
```csharp
public static bool IsPathSafe(string basePath, string relativePath)
{
    if (relativePath.Contains("..")) return false;
    var fullPath = Path.GetFullPath(Path.Combine(basePath, relativePath));
    var normalizedBase = Path.GetFullPath(basePath);
    return fullPath.StartsWith(normalizedBase + Path.DirectorySeparatorChar);
}
```

---

### Threat 2: Denial of Service via Large Files

**Description:** Malicious pack contains enormous component files (e.g., 10GB). Loading the pack exhausts memory or disk.

**Attack Vector:** Create component that appears small in manifest but is actually huge.

**Impact:** Memory exhaustion, disk exhaustion, service unavailability.

**Likelihood:** Low (requires user to load malicious pack).

**Mitigation:**
1. **Size Limits:** Reject components > 1MB
2. **Total Size Limit:** Reject packs > 10MB total
3. **Streaming Hash:** Hash files in chunks, don't load fully
4. **Early Termination:** Check size before reading content

---

### Threat 3: Hash Collision/Pre-image Attack

**Description:** Attacker crafts malicious content that produces the same hash as legitimate content, bypassing integrity verification.

**Attack Vector:** Create modified prompt with same SHA-256 hash.

**Impact:** Integrity bypass. Malicious prompts accepted as legitimate.

**Likelihood:** Negligible (SHA-256 has 128-bit security; no practical attacks exist).

**Mitigation:**
1. **Use SHA-256:** Current algorithm is secure
2. **Algorithm Agility:** Format version allows future algorithm migration
3. **Defense in Depth:** Combine with code review, git history

---

### Threat 4: YAML Injection/Deserialization Attack

**Description:** Malicious YAML in manifest triggers unsafe deserialization, executing arbitrary code.

**Attack Vector:** Use YAML tags or complex types that invoke constructors.

**Impact:** Remote code execution.

**Likelihood:** Low (YamlDotNet safe mode prevents this by default).

**Mitigation:**
1. **Safe YAML Mode:** Use `DeserializerBuilder().WithTypeConverter()` with explicit types only
2. **No Type Tags:** Reject YAML with `!` tags
3. **Schema Validation:** Validate manifest structure before processing
4. **Library Updates:** Keep YamlDotNet updated

---

### Threat 5: Information Disclosure via Error Messages

**Description:** Detailed error messages reveal filesystem structure or sensitive paths to users/logs.

**Attack Vector:** Trigger errors with crafted paths to enumerate filesystem.

**Impact:** Information disclosure. Attacker learns server layout.

**Likelihood:** Medium (error handling often verbose).

**Mitigation:**
1. **Sanitize Paths:** Log relative paths, not absolute
2. **Generic User Messages:** Show "Component not found" not full path
3. **DEBUG-only Details:** Full paths only at DEBUG level
4. **Audit Logging:** Log access attempts for security review

---

## Best Practices

### Pack Organization

1. **One Pack Per Project Type:** Create separate packs for different project types (dotnet-api, react-frontend) rather than one mega-pack.

2. **Use Standard Subdirectories:** Follow the conventional layout (roles/, languages/, frameworks/) for discoverability.

3. **Keep Components Focused:** Each prompt file should address one concern. Split large prompts into multiple components.

4. **Name Descriptively:** Use names that describe the prompt's purpose (`security-rules.md` not `rules2.md`).

### Versioning

5. **Start at 1.0.0:** Use semantic versioning from the beginning. Don't use 0.x.x unless truly pre-release.

6. **Increment Appropriately:** Follow SemVer strictly. Breaking changes = MAJOR, features = MINOR, fixes = PATCH.

7. **Document Changes:** Maintain a CHANGELOG.md in the pack root listing changes per version.

8. **Tag in Git:** Create git tags for each version (e.g., `v1.2.3`).

### Hashing

9. **Regenerate After Changes:** Always run `acode prompts hash` after modifying components.

10. **Verify Before Deploy:** Run `acode prompts validate` before distributing a pack.

11. **Don't Edit Hash Manually:** Let the tooling compute hashes. Manual edits will cause mismatches.

### Manifest Management

12. **Keep Manifest Minimal:** Only include required fields and components. Avoid clutter.

13. **Update Timestamps:** Set `updated_at` when modifying the pack.

14. **Use Meaningful Descriptions:** Write descriptions that help users understand the pack's purpose.

15. **List All Components:** Ensure every prompt file is listed in components. Unlisted files are ignored.

### Security

16. **Review Third-Party Packs:** Before using external packs, review all components for malicious content.

17. **Pin Versions:** Specify exact versions or ranges to prevent unexpected updates.

18. **Version Control Packs:** Store packs in git for history, review, and rollback capability.

---

## Troubleshooting

### Issue 1: Content Hash Mismatch

**Symptoms:**
- Warning: `ACODE-PKL-008: Content hash mismatch`
- Pack loads but with warning
- Version shown as "unverified"

**Possible Causes:**
1. Component files modified after hash was generated
2. Line endings changed (git autocrlf)
3. Encoding changed (UTF-8 BOM added/removed)
4. Component added/removed but manifest not updated

**Solutions:**
1. **Regenerate hash:** `acode prompts hash .acode/prompts/my-pack`
2. **Check line endings:** `file .acode/prompts/my-pack/system.md` should show "ASCII text"
3. **Check encoding:** Open in hex editor, verify no BOM (EF BB BF)
4. **Verify components:** Ensure manifest components list matches actual files
5. **Review git config:** Check `core.autocrlf` setting

**Verification:**
```bash
# Regenerate hash
acode prompts hash .acode/prompts/my-pack

# Validate pack
acode prompts validate .acode/prompts/my-pack
# Should output: "Pack 'my-pack' is valid"
```

---

### Issue 2: Component File Not Found

**Symptoms:**
- Error: `ACODE-PKL-006: Component file not found: roles/analyst.md`
- Pack fails to load

**Possible Causes:**
1. File deleted but manifest not updated
2. File renamed but manifest not updated
3. Path typo in manifest
4. Case sensitivity issue (Linux vs Windows)

**Solutions:**
1. **Check file exists:** `ls .acode/prompts/my-pack/roles/`
2. **Check path in manifest:** Verify exact path matches
3. **Check case:** Linux is case-sensitive; `Coder.md` ≠ `coder.md`
4. **Update manifest:** Remove or fix the component entry

**Verification:**
```bash
# List actual files
find .acode/prompts/my-pack -name "*.md"

# Compare with manifest
acode prompts list my-pack --components
```

---

### Issue 3: Invalid Pack ID Format

**Symptoms:**
- Error: `ACODE-PKL-004: Invalid pack ID format: 'My Pack'`
- Pack not recognized

**Possible Causes:**
1. ID contains spaces
2. ID contains uppercase letters
3. ID contains special characters
4. ID too short (< 3 chars) or too long (> 50 chars)
5. Directory name doesn't match ID in manifest

**Solutions:**
1. **Use lowercase:** `my-pack` not `My-Pack` or `MY_PACK`
2. **Use hyphens:** `my-custom-pack` not `my_custom_pack` or `my custom pack`
3. **Check length:** Must be 3-50 characters
4. **Match directory:** Directory name must equal ID in manifest

**Verification:**
```bash
# Check directory name
basename .acode/prompts/my-pack

# Check manifest ID
grep "^id:" .acode/prompts/my-pack/manifest.yml
```

---

### Issue 4: Invalid SemVer Version

**Symptoms:**
- Error: `ACODE-PKL-005: Invalid version format: '1.0'`
- Pack validation fails

**Possible Causes:**
1. Missing patch number (1.0 instead of 1.0.0)
2. Non-numeric version parts
3. Invalid pre-release format
4. Invalid build metadata format

**Solutions:**
1. **Use full format:** `1.0.0` not `1.0` or `1`
2. **Numbers only in core:** `1.0.0` not `1.0.zero`
3. **Valid pre-release:** `-alpha.1` not `-alpha 1`
4. **Valid build:** `+build.123` not `+ build`

**Verification:**
```bash
# Validate version format
acode prompts validate .acode/prompts/my-pack --verbose
```

---

### Issue 5: Path Traversal Rejected

**Symptoms:**
- Error: `ACODE-PKL-007: Path traversal detected in component: ../system.md`
- Pack fails security check

**Possible Causes:**
1. Component path contains `..`
2. Absolute path used instead of relative
3. Path starts with `/`

**Solutions:**
1. **Use relative paths:** `roles/coder.md` not `/roles/coder.md`
2. **Remove traversal:** `system.md` not `../system.md`
3. **Keep within pack:** All paths must resolve to pack directory or subdirectory

**Verification:**
```bash
# Check paths in manifest
grep "path:" .acode/prompts/my-pack/manifest.yml

# Validate
acode prompts validate .acode/prompts/my-pack
```

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

#### PackManifestTests.cs

```csharp
using FluentAssertions;
using Xunit;
using Acode.Domain.PromptPacks;
using Acode.Infrastructure.PromptPacks;

namespace Acode.Domain.Tests.PromptPacks;

public class PackManifestTests
{
    [Fact]
    public void Should_Parse_Valid_Manifest()
    {
        // Arrange
        var yaml = """
            format_version: "1.0"
            id: test-pack
            version: "1.2.3"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components:
              - path: system.md
                type: system
            """;
        var parser = new ManifestParser();
        
        // Act
        var manifest = parser.Parse(yaml);
        
        // Assert
        manifest.FormatVersion.Should().Be("1.0");
        manifest.Id.Should().Be("test-pack");
        manifest.Version.ToString().Should().Be("1.2.3");
        manifest.Name.Should().Be("Test Pack");
        manifest.Description.Should().Be("A test prompt pack");
        manifest.ContentHash.ToString().Should().StartWith("a1b2c3d4");
        manifest.Components.Should().HaveCount(1);
    }
    
    [Fact]
    public void Should_Reject_Invalid_Format_Version()
    {
        // Arrange
        var yaml = """
            format_version: "2.0"
            id: test-pack
            version: "1.0.0"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();
        
        // Act
        var act = () => parser.Parse(yaml);
        
        // Assert
        act.Should().Throw<ManifestParseException>()
            .WithMessage("*format_version*")
            .Where(e => e.ErrorCode == "ACODE-PKL-003");
    }
    
    [Theory]
    [InlineData("My Pack")]           // spaces
    [InlineData("MyPack")]            // uppercase
    [InlineData("my_pack")]           // underscore
    [InlineData("ab")]                // too short
    [InlineData("a")]                 // too short
    public void Should_Validate_Pack_Id_Format(string invalidId)
    {
        // Arrange
        var yaml = $"""
            format_version: "1.0"
            id: {invalidId}
            version: "1.0.0"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();
        
        // Act
        var act = () => parser.Parse(yaml);
        
        // Assert
        act.Should().Throw<ManifestParseException>()
            .Where(e => e.ErrorCode == "ACODE-PKL-004");
    }
    
    [Theory]
    [InlineData("my-pack")]
    [InlineData("my-custom-pack")]
    [InlineData("team-dotnet-v2")]
    [InlineData("abc")]
    public void Should_Accept_Valid_Pack_Id_Format(string validId)
    {
        // Arrange
        var yaml = $"""
            format_version: "1.0"
            id: {validId}
            version: "1.0.0"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();
        
        // Act
        var manifest = parser.Parse(yaml);
        
        // Assert
        manifest.Id.Should().Be(validId);
    }
    
    [Fact]
    public void Should_Parse_SemVer_Version()
    {
        // Arrange
        var yaml = """
            format_version: "1.0"
            id: test-pack
            version: "2.3.4-beta.1+build.456"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """;
        var parser = new ManifestParser();
        
        // Act
        var manifest = parser.Parse(yaml);
        
        // Assert
        manifest.Version.Major.Should().Be(2);
        manifest.Version.Minor.Should().Be(3);
        manifest.Version.Patch.Should().Be(4);
        manifest.Version.PreRelease.Should().Be("beta.1");
        manifest.Version.BuildMetadata.Should().Be("build.456");
    }
    
    [Fact]
    public void Should_Parse_Components_With_Metadata()
    {
        // Arrange
        var yaml = """
            format_version: "1.0"
            id: test-pack
            version: "1.0.0"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components:
              - path: roles/coder.md
                type: role
                metadata:
                  role: coder
              - path: languages/csharp.md
                type: language
                metadata:
                  language: csharp
                  version: "12"
            """;
        var parser = new ManifestParser();
        
        // Act
        var manifest = parser.Parse(yaml);
        
        // Assert
        manifest.Components.Should().HaveCount(2);
        manifest.Components[0].Type.Should().Be(ComponentType.Role);
        manifest.Components[0].Metadata!["role"].Should().Be("coder");
        manifest.Components[1].Type.Should().Be(ComponentType.Language);
        manifest.Components[1].Metadata!["language"].Should().Be("csharp");
    }
    
    [Fact]
    public void Should_Require_Created_At_Field()
    {
        // Arrange - missing created_at
        var yaml = """
            format_version: "1.0"
            id: test-pack
            version: "1.0.0"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            components: []
            """;
        var parser = new ManifestParser();
        
        // Act
        var act = () => parser.Parse(yaml);
        
        // Assert
        act.Should().Throw<ManifestParseException>()
            .Where(e => e.ErrorCode == "ACODE-PKL-002")
            .WithMessage("*created_at*");
    }
}
```

#### ContentHasherTests.cs

```csharp
using System.Text;
using FluentAssertions;
using Xunit;
using Acode.Infrastructure.PromptPacks;

namespace Acode.Infrastructure.Tests.PromptPacks;

public class ContentHasherTests
{
    private readonly ContentHasher _hasher = new();
    
    [Fact]
    public void Should_Compute_SHA256_Hash()
    {
        // Arrange
        var components = new[]
        {
            ("system.md", "You are an AI assistant.")
        };
        
        // Act
        var hash = _hasher.ComputeHash(components);
        
        // Assert
        hash.ToString().Should().HaveLength(64);
        hash.ToString().Should().MatchRegex("^[a-f0-9]+$");
    }
    
    [Fact]
    public void Should_Sort_Paths_Alphabetically()
    {
        // Arrange
        var components1 = new[]
        {
            ("b.md", "content B"),
            ("a.md", "content A")
        };
        var components2 = new[]
        {
            ("a.md", "content A"),
            ("b.md", "content B")
        };
        
        // Act
        var hash1 = _hasher.ComputeHash(components1);
        var hash2 = _hasher.ComputeHash(components2);
        
        // Assert
        hash1.Should().Be(hash2, "order should not affect hash");
    }
    
    [Fact]
    public void Should_Normalize_Line_Endings()
    {
        // Arrange
        var crlfContent = "Line 1\r\nLine 2\r\n";
        var lfContent = "Line 1\nLine 2\n";
        
        var componentsCrlf = new[] { ("file.md", crlfContent) };
        var componentsLf = new[] { ("file.md", lfContent) };
        
        // Act
        var hashCrlf = _hasher.ComputeHash(componentsCrlf);
        var hashLf = _hasher.ComputeHash(componentsLf);
        
        // Assert
        hashCrlf.Should().Be(hashLf, "line endings should be normalized");
    }
    
    [Fact]
    public void Should_Be_Deterministic()
    {
        // Arrange
        var components = new[]
        {
            ("system.md", "You are an AI."),
            ("roles/coder.md", "Implement code.")
        };
        
        // Act
        var hash1 = _hasher.ComputeHash(components);
        var hash2 = _hasher.ComputeHash(components);
        var hash3 = _hasher.ComputeHash(components);
        
        // Assert
        hash1.Should().Be(hash2);
        hash2.Should().Be(hash3);
    }
    
    [Fact]
    public void Should_Produce_Different_Hash_For_Different_Content()
    {
        // Arrange
        var components1 = new[] { ("file.md", "content A") };
        var components2 = new[] { ("file.md", "content B") };
        
        // Act
        var hash1 = _hasher.ComputeHash(components1);
        var hash2 = _hasher.ComputeHash(components2);
        
        // Assert
        hash1.Should().NotBe(hash2);
    }
    
    [Fact]
    public void Should_Include_Path_In_Hash()
    {
        // Arrange - same content, different paths
        var components1 = new[] { ("path1.md", "same content") };
        var components2 = new[] { ("path2.md", "same content") };
        
        // Act
        var hash1 = _hasher.ComputeHash(components1);
        var hash2 = _hasher.ComputeHash(components2);
        
        // Assert
        hash1.Should().NotBe(hash2, "path should affect hash");
    }
    
    [Fact]
    public void Should_Handle_Empty_Components()
    {
        // Arrange
        var components = Array.Empty<(string, string)>();
        
        // Act
        var hash = _hasher.ComputeHash(components);
        
        // Assert
        hash.Should().NotBeNull();
        hash.ToString().Should().HaveLength(64);
    }
    
    [Fact]
    public void Should_Handle_Unicode_Content()
    {
        // Arrange
        var components = new[]
        {
            ("file.md", "日本語テスト 🚀 émojis")
        };
        
        // Act
        var hash = _hasher.ComputeHash(components);
        
        // Assert
        hash.ToString().Should().HaveLength(64);
    }
}
```

#### ComponentPathTests.cs

```csharp
using FluentAssertions;
using Xunit;
using Acode.Infrastructure.PromptPacks;

namespace Acode.Infrastructure.Tests.PromptPacks;

public class ComponentPathTests
{
    private readonly PathNormalizer _normalizer = new();
    
    [Theory]
    [InlineData(@"roles\coder.md", "roles/coder.md")]
    [InlineData(@"roles\\coder.md", "roles/coder.md")]
    [InlineData("roles//coder.md", "roles/coder.md")]
    [InlineData("roles/./coder.md", "roles/coder.md")]
    [InlineData("roles/coder.md/", "roles/coder.md")]
    public void Should_Normalize_Paths(string input, string expected)
    {
        // Act
        var result = _normalizer.Normalize(input);
        
        // Assert
        result.Should().Be(expected);
    }
    
    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("roles/../../../etc/passwd")]
    [InlineData("roles/..")]
    [InlineData("..")]
    public void Should_Reject_Traversal_Paths(string path)
    {
        // Act
        var act = () => _normalizer.Normalize(path);
        
        // Assert
        act.Should().Throw<PathTraversalException>()
            .Where(e => e.ErrorCode == "ACODE-PKL-007");
    }
    
    [Theory]
    [InlineData("/roles/coder.md")]
    [InlineData("C:\\Users\\test\\file.md")]
    [InlineData("C:/Users/test/file.md")]
    public void Should_Reject_Absolute_Paths(string path)
    {
        // Act
        var act = () => _normalizer.Normalize(path);
        
        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*absolute*");
    }
    
    [Fact]
    public void Should_Handle_Unicode_Paths()
    {
        // Arrange
        var unicodePath = "languages/日本語.md";
        
        // Act
        var result = _normalizer.Normalize(unicodePath);
        
        // Assert
        result.Should().Be(unicodePath);
    }
    
    [Fact]
    public void Should_Validate_Path_Is_Under_Root()
    {
        // Arrange
        var root = "/pack";
        var safePath = "roles/coder.md";
        var unsafePath = "../other/file.md";
        
        // Act
        var isSafe = _normalizer.IsPathSafe(root, safePath);
        var isUnsafe = !_normalizer.IsPathSafe(root, unsafePath);
        
        // Assert
        isSafe.Should().BeTrue();
        isUnsafe.Should().BeTrue();
    }
}
```

#### SemVerTests.cs

```csharp
using FluentAssertions;
using Xunit;
using Acode.Domain.PromptPacks;

namespace Acode.Domain.Tests.PromptPacks;

public class SemVerTests
{
    [Theory]
    [InlineData("1.0.0", 1, 0, 0, null, null)]
    [InlineData("2.3.4", 2, 3, 4, null, null)]
    [InlineData("1.0.0-alpha", 1, 0, 0, "alpha", null)]
    [InlineData("1.0.0-alpha.1", 1, 0, 0, "alpha.1", null)]
    [InlineData("1.0.0+build", 1, 0, 0, null, "build")]
    [InlineData("1.0.0-beta+build.123", 1, 0, 0, "beta", "build.123")]
    public void Should_Parse_Major_Minor_Patch(
        string version, int major, int minor, int patch,
        string? preRelease, string? buildMetadata)
    {
        // Act
        var v = PackVersion.Parse(version);
        
        // Assert
        v.Major.Should().Be(major);
        v.Minor.Should().Be(minor);
        v.Patch.Should().Be(patch);
        v.PreRelease.Should().Be(preRelease);
        v.BuildMetadata.Should().Be(buildMetadata);
    }
    
    [Theory]
    [InlineData("1.0")]        // missing patch
    [InlineData("1")]          // missing minor and patch
    [InlineData("a.b.c")]      // non-numeric
    [InlineData("1.0.0.0")]    // too many parts
    [InlineData("")]           // empty
    public void Should_Reject_Invalid_Versions(string version)
    {
        // Act
        var act = () => PackVersion.Parse(version);
        
        // Assert
        act.Should().Throw<ArgumentException>();
    }
    
    [Theory]
    [InlineData("1.0.0", "1.0.1", -1)]   // patch comparison
    [InlineData("1.0.0", "1.1.0", -1)]   // minor comparison
    [InlineData("1.0.0", "2.0.0", -1)]   // major comparison
    [InlineData("2.0.0", "1.0.0", 1)]    // reverse
    [InlineData("1.0.0", "1.0.0", 0)]    // equal
    [InlineData("1.0.0-alpha", "1.0.0", -1)] // pre-release < release
    [InlineData("1.0.0-alpha", "1.0.0-beta", -1)] // alpha < beta
    public void Should_Compare_Versions(string v1, string v2, int expected)
    {
        // Arrange
        var version1 = PackVersion.Parse(v1);
        var version2 = PackVersion.Parse(v2);
        
        // Act
        var result = version1.CompareTo(version2);
        
        // Assert
        Math.Sign(result).Should().Be(expected);
    }
    
    [Fact]
    public void Should_Sort_Versions()
    {
        // Arrange
        var versions = new[]
        {
            "2.0.0",
            "1.0.0-alpha",
            "1.0.0",
            "1.0.0-beta",
            "1.0.1"
        }.Select(PackVersion.Parse).ToList();
        
        // Act
        var sorted = versions.Order().ToList();
        
        // Assert
        sorted.Select(v => v.ToString()).Should().Equal(
            "1.0.0-alpha",
            "1.0.0-beta",
            "1.0.0",
            "1.0.1",
            "2.0.0"
        );
    }
}
```

### Integration Tests

#### PackDiscoveryTests.cs

```csharp
using FluentAssertions;
using Xunit;
using Acode.Infrastructure.PromptPacks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acode.Integration.Tests.PromptPacks;

public class PackDiscoveryTests : IDisposable
{
    private readonly string _testDir;
    private readonly PackDiscovery _discovery;
    
    public PackDiscoveryTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"pack-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _discovery = new PackDiscovery(
            new PackDiscoveryOptions
            {
                UserPacksPath = Path.Combine(_testDir, ".acode", "prompts")
            },
            NullLogger<PackDiscovery>.Instance);
    }
    
    [Fact]
    public async Task Should_Find_BuiltIn_Packs()
    {
        // Act
        var packs = await _discovery.DiscoverAsync(CancellationToken.None);
        
        // Assert
        packs.Should().Contain(p => p.Id == "acode-standard");
    }
    
    [Fact]
    public async Task Should_Find_User_Packs()
    {
        // Arrange
        CreateTestPack("my-pack");
        
        // Act
        var packs = await _discovery.DiscoverAsync(CancellationToken.None);
        
        // Assert
        packs.Should().Contain(p => p.Id == "my-pack");
    }
    
    [Fact]
    public async Task Should_Prioritize_User_Packs()
    {
        // Arrange - create user pack with same ID as built-in
        CreateTestPack("acode-standard");
        
        // Act
        var packs = await _discovery.DiscoverAsync(CancellationToken.None);
        var pack = packs.Single(p => p.Id == "acode-standard");
        
        // Assert
        pack.Source.Should().Be(PackSource.User);
    }
    
    private void CreateTestPack(string id)
    {
        var packDir = Path.Combine(_testDir, ".acode", "prompts", id);
        Directory.CreateDirectory(packDir);
        File.WriteAllText(Path.Combine(packDir, "manifest.yml"), $"""
            format_version: "1.0"
            id: {id}
            version: "1.0.0"
            name: Test Pack
            description: A test prompt pack
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components: []
            """);
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }
}
```

#### HashVerificationTests.cs

```csharp
using FluentAssertions;
using Xunit;
using Acode.Infrastructure.PromptPacks;

namespace Acode.Integration.Tests.PromptPacks;

public class HashVerificationTests : IDisposable
{
    private readonly string _testDir;
    
    public HashVerificationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"hash-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }
    
    [Fact]
    public async Task Should_Verify_Valid_Hash()
    {
        // Arrange
        var packDir = CreatePackWithHash();
        var verifier = new HashVerifier();
        
        // Act
        var result = await verifier.VerifyAsync(packDir, CancellationToken.None);
        
        // Assert
        result.IsValid.Should().BeTrue();
        result.HashMismatch.Should().BeFalse();
    }
    
    [Fact]
    public async Task Should_Detect_Modified_Content()
    {
        // Arrange
        var packDir = CreatePackWithHash();
        File.AppendAllText(Path.Combine(packDir, "system.md"), "\nModified!");
        var verifier = new HashVerifier();
        
        // Act
        var result = await verifier.VerifyAsync(packDir, CancellationToken.None);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.HashMismatch.Should().BeTrue();
        result.ExpectedHash.Should().NotBe(result.ActualHash);
    }
    
    [Fact]
    public async Task Should_Regenerate_Hash()
    {
        // Arrange
        var packDir = CreatePackWithHash();
        File.AppendAllText(Path.Combine(packDir, "system.md"), "\nModified!");
        var hasher = new ContentHasher();
        
        // Act
        var newHash = await hasher.RegenerateAsync(packDir, CancellationToken.None);
        
        // Assert
        newHash.Should().NotBeNull();
        // Verify manifest updated
        var manifest = File.ReadAllText(Path.Combine(packDir, "manifest.yml"));
        manifest.Should().Contain(newHash.ToString());
    }
    
    private string CreatePackWithHash()
    {
        var packDir = Path.Combine(_testDir, "test-pack");
        Directory.CreateDirectory(packDir);
        
        var systemContent = "You are an AI assistant.";
        File.WriteAllText(Path.Combine(packDir, "system.md"), systemContent);
        
        var hasher = new ContentHasher();
        var hash = hasher.ComputeHash(new[] { ("system.md", systemContent) });
        
        File.WriteAllText(Path.Combine(packDir, "manifest.yml"), $"""
            format_version: "1.0"
            id: test-pack
            version: "1.0.0"
            name: Test Pack
            description: A test prompt pack
            content_hash: {hash}
            created_at: 2024-01-15T10:00:00Z
            components:
              - path: system.md
                type: system
            """);
        
        return packDir;
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }
}
```

### E2E Tests

#### PackCreationE2ETests.cs

```csharp
using FluentAssertions;
using Xunit;
using Acode.Cli.Commands;
using Acode.Infrastructure.PromptPacks;

namespace Acode.E2E.Tests.PromptPacks;

public class PackCreationE2ETests : IDisposable
{
    private readonly string _testDir;
    
    public PackCreationE2ETests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"e2e-pack-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }
    
    [Fact]
    public async Task Should_Create_Pack_Structure()
    {
        // Arrange
        var packPath = Path.Combine(_testDir, ".acode", "prompts", "my-pack");
        
        // Act - Simulate CLI command
        var command = new PackCreateCommand();
        var result = await command.ExecuteAsync(new PackCreateOptions
        {
            PackId = "my-pack",
            Name = "My Custom Pack",
            OutputPath = _testDir
        });
        
        // Assert
        result.ExitCode.Should().Be(0);
        Directory.Exists(packPath).Should().BeTrue();
        File.Exists(Path.Combine(packPath, "manifest.yml")).Should().BeTrue();
        File.Exists(Path.Combine(packPath, "system.md")).Should().BeTrue();
        Directory.Exists(Path.Combine(packPath, "roles")).Should().BeTrue();
    }
    
    [Fact]
    public async Task Should_Generate_Valid_Manifest()
    {
        // Arrange
        var packPath = Path.Combine(_testDir, ".acode", "prompts", "my-pack");
        
        // Act
        var command = new PackCreateCommand();
        await command.ExecuteAsync(new PackCreateOptions
        {
            PackId = "my-pack",
            Name = "My Custom Pack",
            OutputPath = _testDir
        });
        
        // Assert
        var manifestContent = await File.ReadAllTextAsync(
            Path.Combine(packPath, "manifest.yml"));
        var parser = new ManifestParser();
        var manifest = parser.Parse(manifestContent);
        
        manifest.Id.Should().Be("my-pack");
        manifest.Name.Should().Be("My Custom Pack");
        manifest.Version.ToString().Should().Be("1.0.0");
        manifest.FormatVersion.Should().Be("1.0");
    }
    
    [Fact]
    public async Task Should_Compute_Initial_Hash()
    {
        // Arrange
        var packPath = Path.Combine(_testDir, ".acode", "prompts", "my-pack");
        
        // Act
        var command = new PackCreateCommand();
        await command.ExecuteAsync(new PackCreateOptions
        {
            PackId = "my-pack",
            Name = "My Custom Pack",
            OutputPath = _testDir,
            ComputeHash = true
        });
        
        // Assert
        var manifestContent = await File.ReadAllTextAsync(
            Path.Combine(packPath, "manifest.yml"));
        var parser = new ManifestParser();
        var manifest = parser.Parse(manifestContent);
        
        manifest.ContentHash.ToString().Should().HaveLength(64);
        manifest.ContentHash.ToString().Should().MatchRegex("^[a-f0-9]+$");
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }
}
```

### Performance Tests

#### PackLayoutBenchmarks.cs

```csharp
using BenchmarkDotNet.Attributes;
using Acode.Infrastructure.PromptPacks;

namespace Acode.Performance.Tests.PromptPacks;

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class PackLayoutBenchmarks
{
    private ContentHasher _hasher = null!;
    private ManifestParser _parser = null!;
    private string _manifestYaml = null!;
    private (string Path, string Content)[] _smallPack = null!;
    private (string Path, string Content)[] _largePack = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _hasher = new ContentHasher();
        _parser = new ManifestParser();
        
        _manifestYaml = """
            format_version: "1.0"
            id: benchmark-pack
            version: "1.0.0"
            name: Benchmark Pack
            description: Pack for benchmarking
            content_hash: a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
            created_at: 2024-01-15T10:00:00Z
            components:
              - path: system.md
                type: system
              - path: roles/coder.md
                type: role
                metadata:
                  role: coder
            """;
        
        _smallPack = new[]
        {
            ("system.md", new string('x', 1000)),
            ("roles/coder.md", new string('y', 1000))
        };
        
        // 1MB pack (100 files × 10KB each)
        _largePack = Enumerable.Range(0, 100)
            .Select(i => ($"components/file{i:D3}.md", new string('z', 10000)))
            .ToArray();
    }
    
    [Benchmark(Description = "Hash small pack (2 files, 2KB)")]
    public ContentHash Benchmark_Hash_Small_Pack()
    {
        return _hasher.ComputeHash(_smallPack);
    }
    
    [Benchmark(Description = "Hash large pack (100 files, 1MB)")]
    public ContentHash Benchmark_Hash_Large_Pack()
    {
        return _hasher.ComputeHash(_largePack);
    }
    
    [Benchmark(Description = "Parse manifest")]
    public PackManifest Benchmark_Parse_Manifest()
    {
        return _parser.Parse(_manifestYaml);
    }
}
```

### Performance Requirements

| Metric | Requirement | Rationale |
|--------|-------------|-----------|
| PERF-001 | Hash 1MB content < 50ms | SHA-256 should process 20MB/s minimum |
| PERF-002 | Parse manifest < 10ms | YAML parsing with YamlDotNet is efficient |
| PERF-003 | Scan directory < 100ms | File system enumeration should be fast |
| PERF-004 | Memory < 5MB for 1MB pack | Streaming hash avoids loading entire content |

---

## User Verification Steps

### Scenario 1: Create Pack Directory Structure

**Objective:** Verify that the standard pack directory structure is recognized.

**Prerequisites:**
- .NET 9 SDK installed
- Acode CLI available in PATH

**Steps:**

```bash
# Step 1: Create pack directory
mkdir -p .acode/prompts/my-pack

# Step 2: Create manifest
cat > .acode/prompts/my-pack/manifest.yml << 'EOF'
format_version: "1.0"
id: my-pack
version: "1.0.0"
name: My Pack
description: Custom prompts for my project
created_at: 2024-01-15T10:00:00Z
content_hash: ""
components: []
EOF

# Step 3: Validate structure
acode prompts validate my-pack

# Expected output:
# ✓ Pack 'my-pack' structure is valid
# ✓ Manifest format: 1.0
# ✓ Version: 1.0.0
# ⚠ Warning: content_hash is empty - run 'acode prompts hash my-pack'
```

**Expected Result:** Pack structure validated successfully with warning about empty hash.

---

### Scenario 2: Add Components to Pack

**Objective:** Verify that components are discovered and listed correctly.

**Prerequisites:**
- Pack from Scenario 1 exists

**Steps:**

```bash
# Step 1: Create subdirectories
mkdir -p .acode/prompts/my-pack/roles
mkdir -p .acode/prompts/my-pack/languages

# Step 2: Create system prompt
cat > .acode/prompts/my-pack/system.md << 'EOF'
You are an AI coding assistant specialized in .NET development.
Follow best practices and write clean, maintainable code.
EOF

# Step 3: Create role prompt
cat > .acode/prompts/my-pack/roles/coder.md << 'EOF'
As the coder, you implement features following TDD principles.
Always write tests before implementation code.
EOF

# Step 4: Create language prompt
cat > .acode/prompts/my-pack/languages/csharp.md << 'EOF'
When writing C# code:
- Use file-scoped namespaces
- Prefer records for DTOs
- Use primary constructors where appropriate
EOF

# Step 5: Update manifest components
cat > .acode/prompts/my-pack/manifest.yml << 'EOF'
format_version: "1.0"
id: my-pack
version: "1.0.0"
name: My Pack
description: Custom prompts for my project
created_at: 2024-01-15T10:00:00Z
content_hash: ""
components:
  - path: system.md
    type: system
  - path: roles/coder.md
    type: role
    metadata:
      role: coder
  - path: languages/csharp.md
    type: language
    metadata:
      language: csharp
      version: "12"
EOF

# Step 6: List components
acode prompts list my-pack --components

# Expected output:
# Pack: my-pack (v1.0.0)
# Components:
#   - system.md (type: system)
#   - roles/coder.md (type: role, role: coder)
#   - languages/csharp.md (type: language, language: csharp, version: 12)
```

**Expected Result:** All three components listed with correct types and metadata.

---

### Scenario 3: Generate Content Hash

**Objective:** Verify that content hashing works correctly.

**Prerequisites:**
- Pack with components from Scenario 2 exists

**Steps:**

```bash
# Step 1: Generate hash for pack
acode prompts hash my-pack

# Expected output:
# Computing content hash for pack 'my-pack'...
# Hashing 3 components...
#   ✓ languages/csharp.md
#   ✓ roles/coder.md
#   ✓ system.md
# Content hash: abc123def456789012345678901234567890abcdef1234567890abcdef123456
# Manifest updated successfully.

# Step 2: Verify hash in manifest
grep content_hash .acode/prompts/my-pack/manifest.yml

# Expected output:
# content_hash: abc123def456789012345678901234567890abcdef1234567890abcdef123456

# Step 3: Validate pack (should pass now with no warnings)
acode prompts validate my-pack

# Expected output:
# ✓ Pack 'my-pack' is valid
# ✓ Manifest format: 1.0
# ✓ Version: 1.0.0
# ✓ Content hash: verified
# ✓ All 3 components found
```

**Expected Result:** Content hash computed and stored in manifest, validation passes.

---

### Scenario 4: Detect Modified Content

**Objective:** Verify that hash mismatch is detected when content changes.

**Prerequisites:**
- Pack with valid hash from Scenario 3 exists

**Steps:**

```bash
# Step 1: Modify a component
echo "Additional instruction: Always use async/await." >> .acode/prompts/my-pack/system.md

# Step 2: Attempt to load pack
acode prompts load my-pack

# Expected output:
# ⚠ Warning [ACODE-PKL-008]: Content hash mismatch for pack 'my-pack'
#   Expected: abc123def456789012345678901234567890abcdef1234567890abcdef123456
#   Actual:   def456789012345678901234567890abcdef1234567890abcdef123456789abc
# Pack loaded with warning. Content may have been modified.

# Step 3: Regenerate hash
acode prompts hash my-pack

# Expected output:
# Content hash: def456789012345678901234567890abcdef1234567890abcdef123456789abc
# Manifest updated successfully.

# Step 4: Load again (no warning)
acode prompts load my-pack

# Expected output:
# ✓ Pack 'my-pack' loaded successfully
# ✓ Version: 1.0.0
# ✓ Content hash: verified
```

**Expected Result:** Hash mismatch detected, warning displayed, regeneration fixes it.

---

### Scenario 5: Validate SemVer Version Format

**Objective:** Verify that semantic versioning is enforced.

**Prerequisites:**
- Pack from Scenario 1 exists

**Steps:**

```bash
# Step 1: Set valid version with prerelease and build metadata
cat > .acode/prompts/my-pack/manifest.yml << 'EOF'
format_version: "1.0"
id: my-pack
version: "2.3.4-beta.1+build.456"
name: My Pack
description: Custom prompts for my project
created_at: 2024-01-15T10:00:00Z
content_hash: ""
components: []
EOF

# Step 2: Validate
acode prompts validate my-pack

# Expected output:
# ✓ Pack 'my-pack' structure is valid
# ✓ Version: 2.3.4-beta.1+build.456 (prerelease)

# Step 3: Set invalid version (missing patch)
cat > .acode/prompts/my-pack/manifest.yml << 'EOF'
format_version: "1.0"
id: my-pack
version: "1.0"
name: My Pack
description: Custom prompts for my project
created_at: 2024-01-15T10:00:00Z
content_hash: ""
components: []
EOF

# Step 4: Validate
acode prompts validate my-pack

# Expected output:
# ✗ Validation failed
# Error [ACODE-PKL-005]: Invalid version format: '1.0'
#   Expected: SemVer 2.0 format (MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD])
#   Examples: 1.0.0, 2.3.4-beta.1, 1.0.0+build.123
```

**Expected Result:** Valid SemVer accepted, invalid format rejected with clear error.

---

### Scenario 6: Cross-Platform Path Normalization

**Objective:** Verify that paths work across platforms.

**Prerequisites:**
- Pack from Scenario 1 exists

**Steps:**

```bash
# Step 1: Create component file
mkdir -p .acode/prompts/my-pack/roles
cat > .acode/prompts/my-pack/roles/coder.md << 'EOF'
You are the coder.
EOF

# Step 2: Use backslashes in manifest (Windows-style)
cat > .acode/prompts/my-pack/manifest.yml << 'EOF'
format_version: "1.0"
id: my-pack
version: "1.0.0"
name: My Pack
description: Custom prompts
created_at: 2024-01-15T10:00:00Z
content_hash: ""
components:
  - path: roles\coder.md
    type: role
EOF

# Step 3: Load pack on any platform
acode prompts load my-pack

# Expected output:
# ✓ Pack 'my-pack' loaded successfully
# Path normalized: roles\coder.md → roles/coder.md
# ✓ Component found: roles/coder.md

# Step 4: Verify normalization preserved
acode prompts list my-pack --components

# Expected output:
# Pack: my-pack (v1.0.0)
# Components:
#   - roles/coder.md (type: role)  # Note: forward slashes
```

**Expected Result:** Backslashes converted to forward slashes, component found.

---

### Scenario 7: Reject Invalid Pack ID

**Objective:** Verify that pack ID validation works correctly.

**Prerequisites:**
- None

**Steps:**

```bash
# Step 1: Create pack with invalid ID (contains spaces)
mkdir -p ".acode/prompts/My Pack"
cat > ".acode/prompts/My Pack/manifest.yml" << 'EOF'
format_version: "1.0"
id: My Pack
version: "1.0.0"
name: Bad ID Pack
description: Pack with invalid ID
created_at: 2024-01-15T10:00:00Z
content_hash: ""
components: []
EOF

# Step 2: Validate
acode prompts validate "My Pack"

# Expected output:
# ✗ Validation failed
# Error [ACODE-PKL-004]: Invalid pack ID format: 'My Pack'
#   Pack IDs must:
#   - Be 3+ characters long
#   - Use only lowercase letters, numbers, and hyphens
#   - Start with a letter
#   Valid examples: my-pack, team-dotnet-v2, custom-prompts

# Step 3: Test uppercase
cat > ".acode/prompts/MyPack/manifest.yml" << 'EOF'
format_version: "1.0"
id: MyPack
version: "1.0.0"
name: Uppercase Pack
description: Pack with uppercase
created_at: 2024-01-15T10:00:00Z
content_hash: ""
components: []
EOF

acode prompts validate "MyPack"

# Expected output:
# ✗ Validation failed
# Error [ACODE-PKL-004]: Invalid pack ID format: 'MyPack'
```

**Expected Result:** Both space-containing and uppercase IDs rejected with helpful error.

---

### Scenario 8: Detect Path Traversal Attempt

**Objective:** Verify that path traversal attacks are blocked.

**Prerequisites:**
- Pack from Scenario 1 exists

**Steps:**

```bash
# Step 1: Create manifest with traversal path
cat > .acode/prompts/my-pack/manifest.yml << 'EOF'
format_version: "1.0"
id: my-pack
version: "1.0.0"
name: My Pack
description: Pack with traversal attempt
created_at: 2024-01-15T10:00:00Z
content_hash: ""
components:
  - path: ../../../etc/passwd
    type: custom
EOF

# Step 2: Load pack
acode prompts load my-pack

# Expected output:
# ✗ Security error
# Error [ACODE-PKL-007]: Path traversal detected in component path
#   Path: ../../../etc/passwd
#   Component paths must remain within the pack directory.
#   Relative paths using '..' are not allowed.

# Step 3: Try another traversal variant
cat > .acode/prompts/my-pack/manifest.yml << 'EOF'
format_version: "1.0"
id: my-pack
version: "1.0.0"
name: My Pack
description: Pack with traversal attempt
created_at: 2024-01-15T10:00:00Z
content_hash: ""
components:
  - path: roles/../../secret.txt
    type: custom
EOF

acode prompts load my-pack

# Expected output:
# ✗ Security error
# Error [ACODE-PKL-007]: Path traversal detected in component path
#   Path: roles/../../secret.txt
```

**Expected Result:** All path traversal attempts blocked with security error.

---

### Scenario 9: Built-in Pack Discovery

**Objective:** Verify that built-in packs are discovered.

**Prerequisites:**
- Acode installed with built-in packs

**Steps:**

```bash
# Step 1: List all packs (no user packs yet)
rm -rf .acode/prompts  # Remove any user packs
acode prompts list

# Expected output:
# Built-in packs:
#   ✓ acode-standard (1.0.0) [built-in]
#       Standard prompts for agentic code development
#   ✓ acode-minimal (1.0.0) [built-in]
#       Minimal prompts for simple tasks
#
# User packs:
#   (none)

# Step 2: Show detailed info for built-in pack
acode prompts info acode-standard

# Expected output:
# Pack: acode-standard
# Version: 1.0.0
# Source: Built-in (embedded resource)
# Description: Standard prompts for agentic code development
# Created: 2024-01-01T00:00:00Z
# Content Hash: <hash>
#
# Components (5):
#   - system.md (type: system)
#   - roles/coder.md (type: role)
#   - roles/reviewer.md (type: role)
#   - languages/csharp.md (type: language)
#   - languages/python.md (type: language)

# Step 3: View component content
acode prompts show acode-standard system.md

# Expected output: (system prompt content)
```

**Expected Result:** Built-in packs discovered and accessible.

---

### Scenario 10: User Pack Override

**Objective:** Verify that user packs take precedence over built-in packs.

**Prerequisites:**
- Built-in acode-standard pack exists

**Steps:**

```bash
# Step 1: Verify built-in pack is active
acode prompts info acode-standard

# Expected output:
# Source: Built-in (embedded resource)
# Version: 1.0.0

# Step 2: Create user pack with same ID
mkdir -p .acode/prompts/acode-standard
cat > .acode/prompts/acode-standard/manifest.yml << 'EOF'
format_version: "1.0"
id: acode-standard
version: "2.0.0"
name: Customized Standard Pack
description: User override of acode-standard with team customizations
created_at: 2024-01-15T10:00:00Z
content_hash: ""
components:
  - path: system.md
    type: system
EOF

cat > .acode/prompts/acode-standard/system.md << 'EOF'
You are a specialized AI assistant for our team.
Follow our internal coding standards.
EOF

# Step 3: List packs - verify user version shown
acode prompts list

# Expected output:
# Built-in packs:
#   - acode-minimal (1.0.0) [built-in]
#
# User packs:
#   ✓ acode-standard (2.0.0) [user] ← overrides built-in

# Step 4: Show precedence info
acode prompts info acode-standard

# Expected output:
# Pack: acode-standard
# Version: 2.0.0
# Source: User (.acode/prompts/acode-standard)
# Description: User override of acode-standard with team customizations
#
# ⚠ This pack overrides a built-in pack:
#   Built-in version: 1.0.0
#   User version: 2.0.0
#
# Components (1):
#   - system.md (type: system)

# Step 5: Remove user pack, verify fallback to built-in
rm -rf .acode/prompts/acode-standard
acode prompts info acode-standard

# Expected output:
# Source: Built-in (embedded resource)
# Version: 1.0.0
```

**Expected Result:** User pack takes precedence, removal falls back to built-in.

---

## Implementation Prompt

You are implementing TASK-008.a: Prompt Pack File Layout, Hashing, and Versioning for the Acode agentic coding system. This task establishes the foundational file system conventions, content integrity verification, and semantic versioning for prompt packs.

### Overview

Implement the complete prompt pack layout system including:
1. Domain models for pack manifest and components
2. Content hashing with SHA-256 for integrity verification
3. SemVer 2.0 versioning support
4. Path normalization for cross-platform compatibility
5. Pack discovery from built-in and user sources

### File Structure

```
src/Acode.Domain/PromptPacks/
├── PackManifest.cs           # Manifest domain model
├── PackComponent.cs          # Component domain model
├── ComponentType.cs          # Component type enumeration
├── ContentHash.cs            # SHA-256 hash value object
├── PackVersion.cs            # SemVer 2.0 value object
├── PackSource.cs             # Pack source enumeration
└── Exceptions/
    ├── ManifestParseException.cs
    ├── PathTraversalException.cs
    └── PackValidationException.cs

src/Acode.Infrastructure/PromptPacks/
├── ContentHasher.cs          # Hash computation
├── HashVerifier.cs           # Hash verification
├── ManifestParser.cs         # YAML parsing
├── PathNormalizer.cs         # Cross-platform paths
├── PackDiscovery.cs          # Pack discovery service
├── PackDiscoveryOptions.cs   # Discovery configuration
└── Resources/                # Built-in packs
    └── acode-standard/
        ├── manifest.yml
        ├── system.md
        └── roles/
```

### Domain Models

#### PackManifest.cs

```csharp
using System;
using System.Collections.Generic;

namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents a prompt pack manifest containing metadata and component definitions.
/// </summary>
/// <remarks>
/// The manifest is the central configuration file for a prompt pack.
/// It defines the pack identity, version, and all included components.
/// </remarks>
public sealed class PackManifest
{
    /// <summary>
    /// The manifest format version. Currently only "1.0" is supported.
    /// </summary>
    /// <example>1.0</example>
    public required string FormatVersion { get; init; }
    
    /// <summary>
    /// Unique identifier for the pack. Must be lowercase with hyphens only.
    /// </summary>
    /// <example>my-custom-pack</example>
    public required string Id { get; init; }
    
    /// <summary>
    /// Semantic version of the pack following SemVer 2.0 specification.
    /// </summary>
    public required PackVersion Version { get; init; }
    
    /// <summary>
    /// Human-readable name of the pack.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Description of the pack's purpose and contents.
    /// </summary>
    public required string Description { get; init; }
    
    /// <summary>
    /// SHA-256 hash of all component contents for integrity verification.
    /// </summary>
    public required ContentHash ContentHash { get; init; }
    
    /// <summary>
    /// Timestamp when the pack was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }
    
    /// <summary>
    /// Timestamp when the pack was last updated. Null if never updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }
    
    /// <summary>
    /// Optional author information.
    /// </summary>
    public string? Author { get; init; }
    
    /// <summary>
    /// List of components included in the pack.
    /// </summary>
    public required IReadOnlyList<PackComponent> Components { get; init; }
    
    /// <summary>
    /// Source of the pack (built-in or user).
    /// </summary>
    public PackSource Source { get; init; } = PackSource.User;
    
    /// <summary>
    /// Absolute path to the pack directory. Set during discovery.
    /// </summary>
    public string? PackPath { get; init; }
    
    /// <summary>
    /// Validates the manifest structure and content.
    /// </summary>
    /// <exception cref="PackValidationException">Thrown when validation fails.</exception>
    public void Validate()
    {
        if (FormatVersion != "1.0")
        {
            throw new PackValidationException(
                "ACODE-PKL-003",
                $"Unsupported format_version: '{FormatVersion}'. Only '1.0' is supported.");
        }
        
        if (!IsValidPackId(Id))
        {
            throw new PackValidationException(
                "ACODE-PKL-004",
                $"Invalid pack ID format: '{Id}'. Pack IDs must be 3+ characters, lowercase, letters/numbers/hyphens only.");
        }
    }
    
    private static bool IsValidPackId(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length < 3)
            return false;
        
        if (!char.IsAsciiLetterLower(id[0]))
            return false;
        
        return id.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');
    }
}
```

#### PackComponent.cs

```csharp
using System.Collections.Generic;

namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents a single component within a prompt pack.
/// </summary>
public sealed class PackComponent
{
    /// <summary>
    /// Relative path to the component file within the pack directory.
    /// Uses forward slashes regardless of platform.
    /// </summary>
    /// <example>roles/coder.md</example>
    public required string Path { get; init; }
    
    /// <summary>
    /// Type of the component (system, role, language, framework, custom).
    /// </summary>
    public required ComponentType Type { get; init; }
    
    /// <summary>
    /// Optional metadata key-value pairs for the component.
    /// </summary>
    /// <example>
    /// metadata:
    ///   role: coder
    ///   priority: high
    /// </example>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    
    /// <summary>
    /// Optional description of the component's purpose.
    /// </summary>
    public string? Description { get; init; }
}
```

#### ComponentType.cs

```csharp
namespace Acode.Domain.PromptPacks;

/// <summary>
/// Defines the types of components that can be included in a prompt pack.
/// </summary>
public enum ComponentType
{
    /// <summary>
    /// System-level prompt that sets the AI's base behavior.
    /// </summary>
    System,
    
    /// <summary>
    /// Role-specific prompt (e.g., coder, reviewer, architect).
    /// </summary>
    Role,
    
    /// <summary>
    /// Programming language-specific prompt (e.g., csharp, python).
    /// </summary>
    Language,
    
    /// <summary>
    /// Framework-specific prompt (e.g., aspnetcore, react).
    /// </summary>
    Framework,
    
    /// <summary>
    /// Custom component type for user-defined purposes.
    /// </summary>
    Custom
}
```

#### ContentHash.cs

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents a SHA-256 content hash for integrity verification.
/// </summary>
public sealed class ContentHash : IEquatable<ContentHash>
{
    private readonly string _value;
    
    /// <summary>
    /// Creates a new ContentHash from a hex string.
    /// </summary>
    /// <param name="value">64-character lowercase hex string.</param>
    /// <exception cref="ArgumentException">Thrown when value is invalid.</exception>
    public ContentHash(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        
        if (value.Length != 64)
        {
            throw new ArgumentException(
                $"Hash must be 64 hex characters, got {value.Length}.",
                nameof(value));
        }
        
        if (!value.All(c => char.IsAsciiHexDigitLower(c)))
        {
            throw new ArgumentException(
                "Hash must contain only lowercase hex characters (0-9, a-f).",
                nameof(value));
        }
        
        _value = value;
    }
    
    /// <summary>
    /// Empty hash for packs without computed hashes.
    /// </summary>
    public static ContentHash Empty { get; } = new ContentHash(new string('0', 64));
    
    /// <summary>
    /// Computes a content hash from a collection of path/content pairs.
    /// </summary>
    /// <param name="components">Tuples of (normalized path, file content).</param>
    /// <returns>SHA-256 hash of all content.</returns>
    public static ContentHash Compute(IEnumerable<(string Path, string Content)> components)
    {
        // Sort by path for deterministic ordering
        var sortedComponents = components
            .OrderBy(c => c.Path, StringComparer.Ordinal)
            .ToList();
        
        using var sha256 = SHA256.Create();
        
        foreach (var (path, content) in sortedComponents)
        {
            // Include path in hash (prevents same content at different paths)
            var pathBytes = Encoding.UTF8.GetBytes(path);
            sha256.TransformBlock(pathBytes, 0, pathBytes.Length, null, 0);
            
            // Normalize line endings to LF for cross-platform consistency
            var normalizedContent = content.Replace("\r\n", "\n").Replace("\r", "\n");
            var contentBytes = Encoding.UTF8.GetBytes(normalizedContent);
            sha256.TransformBlock(contentBytes, 0, contentBytes.Length, null, 0);
        }
        
        // Finalize hash
        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var hashBytes = sha256.Hash!;
        
        return new ContentHash(Convert.ToHexStringLower(hashBytes));
    }
    
    /// <summary>
    /// Checks if this hash matches another hash.
    /// </summary>
    public bool Matches(ContentHash other) => 
        string.Equals(_value, other._value, StringComparison.Ordinal);
    
    public bool Equals(ContentHash? other) => 
        other is not null && _value == other._value;
    
    public override bool Equals(object? obj) => 
        obj is ContentHash other && Equals(other);
    
    public override int GetHashCode() => _value.GetHashCode();
    
    public override string ToString() => _value;
    
    public static bool operator ==(ContentHash? left, ContentHash? right) =>
        left?.Equals(right) ?? right is null;
    
    public static bool operator !=(ContentHash? left, ContentHash? right) =>
        !(left == right);
}
```

#### PackVersion.cs

```csharp
using System;
using System.Text.RegularExpressions;

namespace Acode.Domain.PromptPacks;

/// <summary>
/// Represents a semantic version following SemVer 2.0 specification.
/// </summary>
/// <remarks>
/// Format: MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]
/// </remarks>
public sealed partial class PackVersion : IComparable<PackVersion>, IEquatable<PackVersion>
{
    private static readonly Regex SemVerRegex = CreateSemVerRegex();
    
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string? PreRelease { get; }
    public string? BuildMetadata { get; }
    
    private PackVersion(int major, int minor, int patch, string? preRelease, string? buildMetadata)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
        BuildMetadata = buildMetadata;
    }
    
    /// <summary>
    /// Parses a version string into a PackVersion.
    /// </summary>
    /// <param name="version">Version string in SemVer 2.0 format.</param>
    /// <returns>Parsed PackVersion instance.</returns>
    /// <exception cref="ArgumentException">Thrown when version format is invalid.</exception>
    public static PackVersion Parse(string version)
    {
        ArgumentException.ThrowIfNullOrEmpty(version);
        
        var match = SemVerRegex.Match(version);
        if (!match.Success)
        {
            throw new ArgumentException(
                $"Invalid SemVer format: '{version}'. Expected MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD].",
                nameof(version));
        }
        
        return new PackVersion(
            int.Parse(match.Groups["major"].Value),
            int.Parse(match.Groups["minor"].Value),
            int.Parse(match.Groups["patch"].Value),
            match.Groups["prerelease"].Success ? match.Groups["prerelease"].Value : null,
            match.Groups["build"].Success ? match.Groups["build"].Value : null
        );
    }
    
    /// <summary>
    /// Tries to parse a version string, returning false if invalid.
    /// </summary>
    public static bool TryParse(string version, out PackVersion? result)
    {
        try
        {
            result = Parse(version);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
    
    /// <summary>
    /// Creates a new version with default values.
    /// </summary>
    public static PackVersion Default { get; } = new(1, 0, 0, null, null);
    
    public int CompareTo(PackVersion? other)
    {
        if (other is null) return 1;
        
        var majorCompare = Major.CompareTo(other.Major);
        if (majorCompare != 0) return majorCompare;
        
        var minorCompare = Minor.CompareTo(other.Minor);
        if (minorCompare != 0) return minorCompare;
        
        var patchCompare = Patch.CompareTo(other.Patch);
        if (patchCompare != 0) return patchCompare;
        
        // Pre-release versions have lower precedence than release versions
        if (PreRelease is null && other.PreRelease is not null) return 1;
        if (PreRelease is not null && other.PreRelease is null) return -1;
        if (PreRelease is not null && other.PreRelease is not null)
        {
            return string.Compare(PreRelease, other.PreRelease, StringComparison.Ordinal);
        }
        
        // Build metadata is ignored in precedence comparison per SemVer spec
        return 0;
    }
    
    public bool Equals(PackVersion? other) =>
        other is not null &&
        Major == other.Major &&
        Minor == other.Minor &&
        Patch == other.Patch &&
        PreRelease == other.PreRelease;
    
    public override bool Equals(object? obj) =>
        obj is PackVersion other && Equals(other);
    
    public override int GetHashCode() =>
        HashCode.Combine(Major, Minor, Patch, PreRelease);
    
    public override string ToString()
    {
        var result = $"{Major}.{Minor}.{Patch}";
        if (PreRelease is not null) result += $"-{PreRelease}";
        if (BuildMetadata is not null) result += $"+{BuildMetadata}";
        return result;
    }
    
    public static bool operator <(PackVersion left, PackVersion right) =>
        left.CompareTo(right) < 0;
    
    public static bool operator >(PackVersion left, PackVersion right) =>
        left.CompareTo(right) > 0;
    
    public static bool operator <=(PackVersion left, PackVersion right) =>
        left.CompareTo(right) <= 0;
    
    public static bool operator >=(PackVersion left, PackVersion right) =>
        left.CompareTo(right) >= 0;
    
    [GeneratedRegex(
        @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+(?<build>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$",
        RegexOptions.Compiled)]
    private static partial Regex CreateSemVerRegex();
}
```

#### PackSource.cs

```csharp
namespace Acode.Domain.PromptPacks;

/// <summary>
/// Indicates the source of a prompt pack.
/// </summary>
public enum PackSource
{
    /// <summary>
    /// Pack is embedded in the application as a resource.
    /// </summary>
    BuiltIn,
    
    /// <summary>
    /// Pack is located in the user's .acode/prompts directory.
    /// </summary>
    User
}
```

### Infrastructure Services

#### PathNormalizer.cs

```csharp
using System;
using System.IO;
using Acode.Domain.PromptPacks;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Normalizes file paths for cross-platform compatibility and security.
/// </summary>
public sealed class PathNormalizer
{
    /// <summary>
    /// Normalizes a path to use forward slashes and removes redundant segments.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>Normalized path with forward slashes.</returns>
    /// <exception cref="PathTraversalException">Thrown when path traversal is detected.</exception>
    /// <exception cref="ArgumentException">Thrown when path is absolute.</exception>
    public string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        
        // Check for absolute paths
        if (Path.IsPathRooted(path) || path.StartsWith('/') || 
            (path.Length >= 2 && path[1] == ':'))
        {
            throw new ArgumentException(
                $"Component paths must be relative, not absolute: '{path}'",
                nameof(path));
        }
        
        // Normalize separators to forward slash
        var normalized = path.Replace('\\', '/');
        
        // Remove double slashes
        while (normalized.Contains("//"))
        {
            normalized = normalized.Replace("//", "/");
        }
        
        // Remove trailing slash
        normalized = normalized.TrimEnd('/');
        
        // Remove current directory references
        normalized = normalized.Replace("/./", "/");
        if (normalized.StartsWith("./"))
        {
            normalized = normalized[2..];
        }
        
        // Check for path traversal
        if (ContainsTraversal(normalized))
        {
            throw new PathTraversalException(
                "ACODE-PKL-007",
                $"Path traversal detected in component path: '{path}'");
        }
        
        return normalized;
    }
    
    /// <summary>
    /// Checks if a path would escape the root directory.
    /// </summary>
    public bool IsPathSafe(string root, string relativePath)
    {
        try
        {
            var normalized = Normalize(relativePath);
            var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
            var normalizedRoot = Path.GetFullPath(root);
            
            return fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
    
    private static bool ContainsTraversal(string path)
    {
        // Check for parent directory references
        if (path == ".." || path.StartsWith("../") || 
            path.Contains("/../") || path.EndsWith("/.."))
        {
            return true;
        }
        
        return false;
    }
}
```

#### ContentHasher.cs

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.PromptPacks;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Computes and verifies content hashes for prompt packs.
/// </summary>
public sealed class ContentHasher
{
    private readonly PathNormalizer _pathNormalizer = new();
    
    /// <summary>
    /// Computes a hash from path/content pairs (synchronous).
    /// </summary>
    public ContentHash ComputeHash(IEnumerable<(string Path, string Content)> components)
    {
        return ContentHash.Compute(components);
    }
    
    /// <summary>
    /// Computes a hash for all components in a pack directory.
    /// </summary>
    /// <param name="packDirectory">Path to the pack directory.</param>
    /// <param name="manifest">Pack manifest with component definitions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Computed content hash.</returns>
    public async Task<ContentHash> ComputeHashAsync(
        string packDirectory,
        PackManifest manifest,
        CancellationToken cancellationToken = default)
    {
        var components = new List<(string Path, string Content)>();
        
        foreach (var component in manifest.Components)
        {
            var normalizedPath = _pathNormalizer.Normalize(component.Path);
            var fullPath = Path.Combine(packDirectory, normalizedPath);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    $"Component file not found: {normalizedPath}",
                    fullPath);
            }
            
            var content = await File.ReadAllTextAsync(fullPath, cancellationToken);
            components.Add((normalizedPath, content));
        }
        
        return ContentHash.Compute(components);
    }
    
    /// <summary>
    /// Regenerates the hash for a pack and updates the manifest file.
    /// </summary>
    public async Task<ContentHash> RegenerateAsync(
        string packDirectory,
        CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(packDirectory, "manifest.yml");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Manifest not found", manifestPath);
        }
        
        var manifestContent = await File.ReadAllTextAsync(manifestPath, cancellationToken);
        var parser = new ManifestParser();
        var manifest = parser.Parse(manifestContent);
        
        var newHash = await ComputeHashAsync(packDirectory, manifest, cancellationToken);
        
        // Update manifest with new hash
        var updatedContent = manifestContent.Replace(
            $"content_hash: {manifest.ContentHash}",
            $"content_hash: {newHash}");
        
        await File.WriteAllTextAsync(manifestPath, updatedContent, cancellationToken);
        
        return newHash;
    }
}
```

#### ManifestParser.cs

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using Acode.Domain.PromptPacks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Parses prompt pack manifest files from YAML.
/// </summary>
public sealed class ManifestParser
{
    private readonly IDeserializer _deserializer;
    private readonly PathNormalizer _pathNormalizer = new();
    
    public ManifestParser()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }
    
    /// <summary>
    /// Parses a manifest from YAML content.
    /// </summary>
    /// <param name="yaml">YAML content.</param>
    /// <returns>Parsed PackManifest.</returns>
    /// <exception cref="ManifestParseException">Thrown when parsing fails.</exception>
    public PackManifest Parse(string yaml)
    {
        ArgumentException.ThrowIfNullOrEmpty(yaml);
        
        try
        {
            var dto = _deserializer.Deserialize<ManifestDto>(yaml);
            return MapToManifest(dto);
        }
        catch (Exception ex) when (ex is not ManifestParseException)
        {
            throw new ManifestParseException(
                "ACODE-PKL-001",
                $"Invalid manifest YAML: {ex.Message}",
                ex);
        }
    }
    
    /// <summary>
    /// Parses a manifest from a file.
    /// </summary>
    public PackManifest ParseFile(string path)
    {
        var yaml = File.ReadAllText(path);
        var manifest = Parse(yaml);
        return manifest with { PackPath = Path.GetDirectoryName(path) };
    }
    
    private PackManifest MapToManifest(ManifestDto dto)
    {
        // Validate required fields
        ValidateRequiredField(dto.FormatVersion, "format_version");
        ValidateRequiredField(dto.Id, "id");
        ValidateRequiredField(dto.Version, "version");
        ValidateRequiredField(dto.Name, "name");
        ValidateRequiredField(dto.Description, "description");
        
        if (dto.CreatedAt == default)
        {
            throw new ManifestParseException(
                "ACODE-PKL-002",
                "Missing required field: created_at");
        }
        
        // Validate format version
        if (dto.FormatVersion != "1.0")
        {
            throw new ManifestParseException(
                "ACODE-PKL-003",
                $"Unsupported format_version: '{dto.FormatVersion}'. Only '1.0' is supported.");
        }
        
        // Validate pack ID
        if (!IsValidPackId(dto.Id))
        {
            throw new ManifestParseException(
                "ACODE-PKL-004",
                $"Invalid pack ID format: '{dto.Id}'");
        }
        
        // Parse version
        if (!PackVersion.TryParse(dto.Version, out var version))
        {
            throw new ManifestParseException(
                "ACODE-PKL-005",
                $"Invalid version format: '{dto.Version}'");
        }
        
        // Parse content hash (allow empty)
        var contentHash = string.IsNullOrEmpty(dto.ContentHash) 
            ? ContentHash.Empty 
            : new ContentHash(dto.ContentHash);
        
        // Map components
        var components = new List<PackComponent>();
        foreach (var componentDto in dto.Components ?? [])
        {
            var normalizedPath = _pathNormalizer.Normalize(componentDto.Path);
            
            components.Add(new PackComponent
            {
                Path = normalizedPath,
                Type = ParseComponentType(componentDto.Type),
                Metadata = componentDto.Metadata,
                Description = componentDto.Description
            });
        }
        
        return new PackManifest
        {
            FormatVersion = dto.FormatVersion,
            Id = dto.Id,
            Version = version!,
            Name = dto.Name,
            Description = dto.Description,
            ContentHash = contentHash,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            Author = dto.Author,
            Components = components.AsReadOnly()
        };
    }
    
    private static void ValidateRequiredField(string? value, string fieldName)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ManifestParseException(
                "ACODE-PKL-002",
                $"Missing required field: {fieldName}");
        }
    }
    
    private static bool IsValidPackId(string id) =>
        !string.IsNullOrEmpty(id) &&
        id.Length >= 3 &&
        char.IsAsciiLetterLower(id[0]) &&
        id.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');
    
    private static ComponentType ParseComponentType(string type) =>
        type.ToLowerInvariant() switch
        {
            "system" => ComponentType.System,
            "role" => ComponentType.Role,
            "language" => ComponentType.Language,
            "framework" => ComponentType.Framework,
            "custom" => ComponentType.Custom,
            _ => ComponentType.Custom
        };
    
    private class ManifestDto
    {
        public string FormatVersion { get; set; } = "";
        public string Id { get; set; } = "";
        public string Version { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ContentHash { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? Author { get; set; }
        public List<ComponentDto>? Components { get; set; }
    }
    
    private class ComponentDto
    {
        public string Path { get; set; } = "";
        public string Type { get; set; } = "";
        public Dictionary<string, string>? Metadata { get; set; }
        public string? Description { get; set; }
    }
}
```

#### PackDiscovery.cs

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.PromptPacks;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Discovers prompt packs from built-in resources and user directories.
/// </summary>
public sealed class PackDiscovery
{
    private readonly PackDiscoveryOptions _options;
    private readonly ILogger<PackDiscovery> _logger;
    private readonly ManifestParser _parser = new();
    
    public PackDiscovery(PackDiscoveryOptions options, ILogger<PackDiscovery> logger)
    {
        _options = options;
        _logger = logger;
    }
    
    /// <summary>
    /// Discovers all available prompt packs.
    /// </summary>
    /// <returns>List of discovered packs, user packs taking precedence.</returns>
    public async Task<IReadOnlyList<PackManifest>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        var packs = new Dictionary<string, PackManifest>(StringComparer.OrdinalIgnoreCase);
        
        // First, load built-in packs
        var builtInPacks = await DiscoverBuiltInPacksAsync(cancellationToken);
        foreach (var pack in builtInPacks)
        {
            packs[pack.Id] = pack with { Source = PackSource.BuiltIn };
            _logger.LogDebug("Discovered built-in pack: {PackId} v{Version}", 
                pack.Id, pack.Version);
        }
        
        // Then, load user packs (override built-in)
        var userPacks = await DiscoverUserPacksAsync(cancellationToken);
        foreach (var pack in userPacks)
        {
            if (packs.ContainsKey(pack.Id))
            {
                _logger.LogInformation(
                    "User pack {PackId} v{Version} overrides built-in pack",
                    pack.Id, pack.Version);
            }
            packs[pack.Id] = pack with { Source = PackSource.User };
        }
        
        return packs.Values.OrderBy(p => p.Id).ToList();
    }
    
    private Task<IReadOnlyList<PackManifest>> DiscoverBuiltInPacksAsync(
        CancellationToken cancellationToken)
    {
        var packs = new List<PackManifest>();
        var assembly = Assembly.GetExecutingAssembly();
        var resourcePrefix = "Acode.Infrastructure.PromptPacks.Resources.";
        
        var manifestResources = assembly
            .GetManifestResourceNames()
            .Where(n => n.StartsWith(resourcePrefix) && n.EndsWith(".manifest.yml"));
        
        foreach (var resourceName in manifestResources)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null) continue;
            
            using var reader = new StreamReader(stream);
            var yaml = reader.ReadToEnd();
            
            try
            {
                var manifest = _parser.Parse(yaml);
                packs.Add(manifest);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "Failed to parse built-in manifest: {Resource}", resourceName);
            }
        }
        
        return Task.FromResult<IReadOnlyList<PackManifest>>(packs);
    }
    
    private async Task<IReadOnlyList<PackManifest>> DiscoverUserPacksAsync(
        CancellationToken cancellationToken)
    {
        var packs = new List<PackManifest>();
        var userPath = _options.UserPacksPath;
        
        if (!Directory.Exists(userPath))
        {
            _logger.LogDebug("User packs directory does not exist: {Path}", userPath);
            return packs;
        }
        
        foreach (var packDir in Directory.GetDirectories(userPath))
        {
            var manifestPath = Path.Combine(packDir, "manifest.yml");
            if (!File.Exists(manifestPath))
            {
                _logger.LogWarning(
                    "Pack directory missing manifest: {PackDir}", packDir);
                continue;
            }
            
            try
            {
                var yaml = await File.ReadAllTextAsync(manifestPath, cancellationToken);
                var manifest = _parser.Parse(yaml);
                packs.Add(manifest with { PackPath = packDir });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "Failed to parse user manifest: {Path}", manifestPath);
            }
        }
        
        return packs;
    }
}
```

#### PackDiscoveryOptions.cs

```csharp
using System;
using System.IO;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Configuration options for pack discovery.
/// </summary>
public sealed class PackDiscoveryOptions
{
    /// <summary>
    /// Path to the user's prompt packs directory.
    /// Defaults to .acode/prompts in the current directory.
    /// </summary>
    public string UserPacksPath { get; set; } = 
        Path.Combine(Environment.CurrentDirectory, ".acode", "prompts");
}
```

### Dependency Injection Registration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Acode.Infrastructure.PromptPacks;

namespace Acode.Infrastructure.DependencyInjection;

public static class PromptPacksServiceExtensions
{
    public static IServiceCollection AddPromptPacks(
        this IServiceCollection services,
        Action<PackDiscoveryOptions>? configure = null)
    {
        var options = new PackDiscoveryOptions();
        configure?.Invoke(options);
        
        services.AddSingleton(options);
        services.AddSingleton<ManifestParser>();
        services.AddSingleton<PathNormalizer>();
        services.AddSingleton<ContentHasher>();
        services.AddSingleton<PackDiscovery>();
        
        return services;
    }
}
```

### Error Codes Reference

| Code | Message | Resolution |
|------|---------|------------|
| ACODE-PKL-001 | Invalid manifest YAML | Check YAML syntax, ensure proper indentation |
| ACODE-PKL-002 | Missing required field: {field} | Add the required field to manifest |
| ACODE-PKL-003 | Invalid format_version | Use format_version: "1.0" |
| ACODE-PKL-004 | Invalid pack ID format | Use lowercase, hyphens, 3+ chars |
| ACODE-PKL-005 | Invalid SemVer version | Use MAJOR.MINOR.PATCH format |
| ACODE-PKL-006 | Component file not found | Ensure file exists at specified path |
| ACODE-PKL-007 | Path traversal detected | Remove ../ from component paths |
| ACODE-PKL-008 | Content hash mismatch | Regenerate hash with `acode prompts hash` |
| ACODE-PKL-009 | Manifest not found | Create manifest.yml in pack directory |
| ACODE-PKL-010 | Invalid component type | Use system/role/language/framework/custom |

### Implementation Checklist

1. [ ] Create PackManifest domain class with validation
2. [ ] Create PackComponent domain class
3. [ ] Create ComponentType enum
4. [ ] Create ContentHash value object with SHA-256 computation
5. [ ] Create PackVersion value object with SemVer parsing
6. [ ] Create PackSource enum
7. [ ] Implement ManifestParser with YAML deserialization
8. [ ] Implement ContentHasher with cross-platform normalization
9. [ ] Implement PathNormalizer with traversal detection
10. [ ] Implement PackDiscovery with built-in and user sources
11. [ ] Create exception classes (ManifestParseException, PathTraversalException)
12. [ ] Create embedded resource structure for built-in packs
13. [ ] Add DI registration extension method
14. [ ] Write unit tests for all domain models
15. [ ] Write unit tests for all infrastructure services
16. [ ] Write integration tests for pack discovery
17. [ ] Add XML documentation to all public members

### Verification Commands

```bash
# Run all prompt pack tests
dotnet test --filter "FullyQualifiedName~PromptPacks"

# Run unit tests only
dotnet test --filter "FullyQualifiedName~PromptPacks&Category=Unit"

# Run integration tests only
dotnet test --filter "FullyQualifiedName~PromptPacks&Category=Integration"

# Verify build
dotnet build src/Acode.Domain/Acode.Domain.csproj
dotnet build src/Acode.Infrastructure/Acode.Infrastructure.csproj
```

---

**End of Task 008.a Specification**