# Task 008: Prompt Pack System

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 004 (Model Provider Interface), Task 007 (Tool Schema Registry), Task 001, Task 002  

---

## Description

Task 008 implements the Prompt Pack System, a modular architecture for managing system prompts, coding guidelines, and behavioral configurations for the Acode agent. Prompt packs encapsulate the instructions that shape how the model approaches coding tasks—language conventions, framework patterns, code style preferences, and workflow behaviors. This system enables customization without modifying core agent code.

System prompts are the foundational instructions that determine model behavior. They establish the agent's persona, define its capabilities and limitations, specify output formats, and encode domain knowledge. Without well-crafted prompts, even powerful models produce inconsistent or incorrect results. The Prompt Pack System formalizes prompt management, enabling version control, validation, and easy customization.

Prompt packs are self-contained bundles of related prompts and metadata. A pack includes system prompts for different agent roles (planner, coder, reviewer), tool guidance, language-specific instructions, and framework patterns. Packs are versioned and hashed for integrity verification. Users select packs via configuration, enabling different behaviors for different projects.

The system supports multiple prompt pack sources. Built-in packs ship with Acode and cover common scenarios (general coding, .NET development, React/TypeScript). User packs are stored in `.acode/prompts/` within the workspace. Community packs can be downloaded from trusted sources. The loader prioritizes user packs over built-in packs, enabling customization.

Prompt composition assembles final prompts from pack components. A complete system prompt may combine base instructions, role-specific guidance, language-specific patterns, and project-specific rules. The composition engine merges these components, handles conflicts, and produces the final prompt. Composition is deterministic and logged for debugging.

Template variables enable dynamic content in prompts. Templates use Mustache-style syntax (`{{variable}}`) for placeholders. Variables are populated from context—workspace name, current file, language, framework. This allows prompts to adapt to context while maintaining consistent structure.

Validation ensures prompts are well-formed before use. The validator checks syntax, template variables, size limits, and required sections. Invalid prompts fail fast with clear error messages. Validation runs at pack load time, not during agent execution, to catch problems early.

The Prompt Pack System integrates with the Model Provider Interface (Task 004). When constructing chat requests, the system fetches appropriate prompts from the active pack and includes them as system messages. Different providers may receive different prompt formats, though content remains consistent.

Configuration controls pack selection and behavior. The `.agent/config.yml` file specifies which pack to use, template variable overrides, and component inclusion/exclusion. Environment variables can override configuration for deployment flexibility. The CLI provides commands for listing, validating, and switching packs.

Observability includes logging when packs are loaded, when prompts are composed, and when template variables are substituted. Metrics track prompt sizes, composition times, and validation results. This visibility helps optimize prompts and debug behavioral issues.

The Prompt Pack System is extensible for future enhancements. Pack formats are versioned; loaders can support multiple versions. New component types can be added without breaking existing packs. The composition engine supports plugins for custom merge strategies.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Prompt Pack | Bundle of related prompts and metadata |
| System Prompt | Instructions defining agent behavior |
| Pack Manifest | Metadata file describing pack contents |
| Pack Version | Semantic version of pack format |
| Content Hash | SHA-256 hash for integrity verification |
| Role Prompt | Instructions for specific agent role |
| Language Prompt | Language-specific coding guidance |
| Framework Prompt | Framework-specific patterns |
| Template Variable | Placeholder for dynamic content |
| Prompt Composition | Assembling final prompt from components |
| Pack Loader | Component that reads and parses packs |
| Pack Validator | Component that checks pack validity |
| Built-in Pack | Pack shipped with Acode |
| User Pack | Pack in workspace .acode directory |
| Pack Selector | Logic for choosing active pack |
| Component | Individual prompt file within pack |
| Merge Strategy | How components combine |
| Pack Registry | Index of available packs |

---

## Out of Scope

The following items are explicitly excluded from Task 008:

- **Remote pack repositories** - Local packs only
- **Pack marketplace** - No distribution system
- **Pack encryption** - Plain text only
- **Pack signing** - No cryptographic verification
- **Dynamic pack switching** - Pack fixed per session
- **Pack inheritance** - No pack extension
- **Prompt optimization** - Static prompts only
- **A/B testing** - Single pack per session
- **Multi-language prompts** - English only
- **Prompt generation** - Manual authoring only

---

## Functional Requirements

### Pack Structure

- FR-001: Pack MUST be a directory with manifest.yml
- FR-002: Manifest MUST include pack id (unique identifier)
- FR-003: Manifest MUST include version (semver)
- FR-004: Manifest MUST include name (display name)
- FR-005: Manifest MUST include description
- FR-006: Manifest MUST list components
- FR-007: Each component MUST have path and type
- FR-008: Pack MUST support system.md component
- FR-009: Pack MUST support roles/*.md components
- FR-010: Pack MUST support languages/*.md components
- FR-011: Pack MUST support frameworks/*.md components

### Pack Manifest Schema

- FR-012: Manifest MUST be valid YAML
- FR-013: Manifest MUST include format_version field
- FR-014: Current format_version MUST be "1.0"
- FR-015: Manifest MUST include content_hash field
- FR-016: content_hash MUST be SHA-256 of components
- FR-017: Manifest MUST include created_at timestamp
- FR-018: Manifest MAY include author field

### IPromptPackLoader Interface

- FR-019: Interface MUST be defined in Application layer
- FR-020: LoadPack MUST accept pack path
- FR-021: LoadPack MUST return PromptPack object
- FR-022: LoadPack MUST validate manifest
- FR-023: LoadPack MUST verify content hash
- FR-024: LoadPack MUST load all components
- FR-025: LoadPack MUST throw on invalid pack

### IPromptPackRegistry Interface

- FR-026: Registry MUST discover built-in packs
- FR-027: Registry MUST discover user packs
- FR-028: Registry MUST index by pack id
- FR-029: GetPack MUST return by id
- FR-030: ListPacks MUST return all available
- FR-031: Registry MUST refresh on request

### Pack Selection

- FR-032: Selection MUST read from .agent/config.yml
- FR-033: Config key: prompts.pack_id
- FR-034: Default pack MUST be "acode-standard"
- FR-035: Selection MUST fallback to default if specified not found
- FR-036: Selection MUST log which pack is active
- FR-037: Environment override: ACODE_PROMPT_PACK

### Prompt Composition

- FR-038: Composer MUST accept role and context
- FR-039: Composer MUST load base system prompt
- FR-040: Composer MUST append role-specific prompt
- FR-041: Composer MUST append language prompt (if applicable)
- FR-042: Composer MUST append framework prompt (if applicable)
- FR-043: Composer MUST apply template variables
- FR-044: Composer MUST deduplicate content
- FR-045: Composer MUST respect max length
- FR-046: Composed prompt MUST be logged (hash only)

### Template Variables

- FR-047: Variables MUST use {{name}} syntax
- FR-048: MUST support {{workspace_name}} variable
- FR-049: MUST support {{language}} variable
- FR-050: MUST support {{framework}} variable
- FR-051: MUST support {{date}} variable
- FR-052: MUST support custom variables from config
- FR-053: Missing variables MUST be replaced with empty
- FR-054: Variables MUST be escaped in final output

### Pack Validation

- FR-055: Validator MUST check manifest schema
- FR-056: Validator MUST check component files exist
- FR-057: Validator MUST check component syntax
- FR-058: Validator MUST check template variables valid
- FR-059: Validator MUST check total size under limit
- FR-060: Validator MUST return detailed errors
- FR-061: Validation MUST run at load time

### Content Hash Verification

- FR-062: Hash MUST be SHA-256
- FR-063: Hash MUST include all component content
- FR-064: Hash MUST be hex-encoded lowercase
- FR-065: Hash mismatch MUST log warning
- FR-066: Hash can be regenerated via CLI

### Built-in Packs

- FR-067: acode-standard pack MUST be included
- FR-068: Built-in packs MUST be in embedded resources
- FR-069: Built-in packs MUST be extractable
- FR-070: Built-in packs MUST be immutable at runtime

### CLI Integration

- FR-071: `acode prompts list` MUST show available packs
- FR-072: `acode prompts show <id>` MUST show pack details
- FR-073: `acode prompts validate <path>` MUST validate pack
- FR-074: `acode prompts hash <path>` MUST regenerate hash
- FR-075: CLI MUST exit 0 on success, 1 on failure

---

## Non-Functional Requirements

### Performance

- NFR-001: Pack loading MUST complete in < 100ms
- NFR-002: Prompt composition MUST complete in < 10ms
- NFR-003: Hash verification MUST complete in < 50ms
- NFR-004: Registry indexing MUST complete in < 200ms
- NFR-005: Memory per pack MUST be < 1MB

### Reliability

- NFR-006: Invalid pack MUST NOT crash agent
- NFR-007: Missing component MUST fail gracefully
- NFR-008: Loader MUST handle Unicode correctly
- NFR-009: Composition MUST be deterministic
- NFR-010: Hash MUST be stable across platforms

### Security

- NFR-011: User packs MUST be sandboxed to workspace
- NFR-012: Paths MUST be validated against traversal
- NFR-013: Template injection MUST be prevented
- NFR-014: Pack content MUST NOT be executable

### Observability

- NFR-015: Pack loading MUST be logged
- NFR-016: Prompt composition MUST be logged
- NFR-017: Validation errors MUST be logged
- NFR-018: Active pack MUST be visible in status

### Maintainability

- NFR-019: Pack format MUST be documented
- NFR-020: All public APIs MUST have XML docs
- NFR-021: Built-in packs MUST have comments
- NFR-022: Version migration MUST be supported

---

## User Manual Documentation

### Overview

The Prompt Pack System manages the instructions that shape Acode's coding behavior. Packs bundle system prompts, role-specific guidance, and language/framework patterns into configurable units.

### Quick Start

1. List available packs:
   ```bash
   $ acode prompts list
   ┌────────────────────────────────────────────────────────────┐
   │ Available Prompt Packs                                      │
   ├──────────────────┬─────────┬───────────────────────────────┤
   │ ID               │ Version │ Description                    │
   ├──────────────────┼─────────┼───────────────────────────────┤
   │ acode-standard   │ 1.0.0   │ General purpose coding         │
   │ acode-dotnet     │ 1.0.0   │ .NET/C# development           │
   │ acode-react      │ 1.0.0   │ React/TypeScript              │
   └──────────────────┴─────────┴───────────────────────────────┘
   ```

2. Select a pack in config:
   ```yaml
   # .agent/config.yml
   prompts:
     pack_id: acode-dotnet
   ```

3. Verify active pack:
   ```bash
   $ acode status
   Prompt Pack: acode-dotnet v1.0.0
   ```

### Pack Structure

```
my-pack/
├── manifest.yml          # Required: pack metadata
├── system.md             # Base system prompt
├── roles/
│   ├── planner.md        # Planner role instructions
│   ├── coder.md          # Coder role instructions
│   └── reviewer.md       # Reviewer role instructions
├── languages/
│   ├── csharp.md         # C# specific guidance
│   ├── typescript.md     # TypeScript guidance
│   └── python.md         # Python guidance
└── frameworks/
    ├── aspnetcore.md     # ASP.NET Core patterns
    ├── react.md          # React patterns
    └── nextjs.md         # Next.js patterns
```

### Manifest Format

```yaml
# manifest.yml
format_version: "1.0"
id: my-custom-pack
version: 1.0.0
name: My Custom Pack
description: Custom prompts for my team
author: Team Name
created_at: 2024-01-15T10:30:00Z
content_hash: a1b2c3d4e5f6...

components:
  - path: system.md
    type: system
  - path: roles/coder.md
    type: role
    role: coder
  - path: languages/csharp.md
    type: language
    language: csharp
```

### Template Variables

Use variables in prompts for dynamic content:

```markdown
# system.md

You are an AI coding assistant working on the {{workspace_name}} project.

Current context:
- Primary language: {{language}}
- Framework: {{framework}}
- Date: {{date}}
```

Available variables:

| Variable | Description |
|----------|-------------|
| `{{workspace_name}}` | Name of current workspace |
| `{{language}}` | Primary programming language |
| `{{framework}}` | Detected framework |
| `{{date}}` | Current date (ISO format) |
| `{{os}}` | Operating system |

### Configuration

```yaml
# .agent/config.yml
prompts:
  # Which pack to use
  pack_id: acode-standard
  
  # Custom variable values
  variables:
    team_name: "Backend Team"
    code_style: "strict"
  
  # Component overrides
  components:
    # Disable specific components
    exclude:
      - frameworks/angular.md
    
    # Add custom components
    include:
      - path: custom/team-rules.md
        type: custom
```

### Creating Custom Packs

1. Create pack directory:
   ```bash
   mkdir -p .acode/prompts/my-pack
   ```

2. Create manifest:
   ```yaml
   # .acode/prompts/my-pack/manifest.yml
   format_version: "1.0"
   id: my-pack
   version: 1.0.0
   name: My Custom Pack
   description: Customized prompts
   ```

3. Add system prompt:
   ```markdown
   # .acode/prompts/my-pack/system.md
   
   You are a careful, methodical coding assistant...
   ```

4. Generate content hash:
   ```bash
   $ acode prompts hash .acode/prompts/my-pack
   Content hash updated: a1b2c3d4...
   ```

5. Validate pack:
   ```bash
   $ acode prompts validate .acode/prompts/my-pack
   ✓ Pack 'my-pack' is valid
   ```

6. Select pack:
   ```yaml
   prompts:
     pack_id: my-pack
   ```

### CLI Commands

```bash
# List all packs
acode prompts list

# Show pack details
acode prompts show acode-dotnet

# Validate a pack
acode prompts validate ./my-pack

# Regenerate content hash
acode prompts hash ./my-pack

# Show composed prompt for role
acode prompts compose --role coder --language csharp
```

### Troubleshooting

#### Pack Not Found

```
Error: Prompt pack 'unknown-pack' not found
```

Solution: Check pack_id in config matches available packs.

#### Hash Mismatch

```
Warning: Content hash mismatch for pack 'my-pack'
```

Solution: Regenerate hash with `acode prompts hash ./my-pack`.

#### Invalid Manifest

```
Error: Invalid manifest: missing required field 'id'
```

Solution: Ensure manifest.yml has all required fields.

---

## Acceptance Criteria

### Pack Structure

- [ ] AC-001: Directory with manifest.yml
- [ ] AC-002: Manifest has id
- [ ] AC-003: Manifest has version
- [ ] AC-004: Manifest has name
- [ ] AC-005: Manifest has description
- [ ] AC-006: Manifest lists components
- [ ] AC-007: Components have path and type
- [ ] AC-008: system.md supported
- [ ] AC-009: roles/*.md supported
- [ ] AC-010: languages/*.md supported
- [ ] AC-011: frameworks/*.md supported

### Manifest Schema

- [ ] AC-012: Valid YAML
- [ ] AC-013: format_version present
- [ ] AC-014: Version is 1.0
- [ ] AC-015: content_hash present
- [ ] AC-016: Hash is SHA-256
- [ ] AC-017: created_at present
- [ ] AC-018: author optional

### Loader

- [ ] AC-019: Interface in Application
- [ ] AC-020: Accepts pack path
- [ ] AC-021: Returns PromptPack
- [ ] AC-022: Validates manifest
- [ ] AC-023: Verifies hash
- [ ] AC-024: Loads components
- [ ] AC-025: Throws on invalid

### Registry

- [ ] AC-026: Discovers built-in
- [ ] AC-027: Discovers user packs
- [ ] AC-028: Indexes by id
- [ ] AC-029: GetPack works
- [ ] AC-030: ListPacks works
- [ ] AC-031: Refresh works

### Selection

- [ ] AC-032: Reads from config
- [ ] AC-033: Uses prompts.pack_id
- [ ] AC-034: Default is acode-standard
- [ ] AC-035: Fallback on missing
- [ ] AC-036: Logs active pack
- [ ] AC-037: Environment override works

### Composition

- [ ] AC-038: Accepts role and context
- [ ] AC-039: Loads base system
- [ ] AC-040: Appends role prompt
- [ ] AC-041: Appends language
- [ ] AC-042: Appends framework
- [ ] AC-043: Applies variables
- [ ] AC-044: Deduplicates
- [ ] AC-045: Respects max length
- [ ] AC-046: Logs composition

### Variables

- [ ] AC-047: {{name}} syntax
- [ ] AC-048: workspace_name works
- [ ] AC-049: language works
- [ ] AC-050: framework works
- [ ] AC-051: date works
- [ ] AC-052: custom from config
- [ ] AC-053: missing = empty
- [ ] AC-054: escaped output

### Validation

- [ ] AC-055: Checks manifest
- [ ] AC-056: Checks files exist
- [ ] AC-057: Checks syntax
- [ ] AC-058: Checks variables
- [ ] AC-059: Checks size
- [ ] AC-060: Returns errors
- [ ] AC-061: Runs at load

### CLI

- [ ] AC-062: list works
- [ ] AC-063: show works
- [ ] AC-064: validate works
- [ ] AC-065: hash works
- [ ] AC-066: Exit codes correct

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Application/PromptPacks/
├── PromptPackLoaderTests.cs
│   ├── Should_Load_Valid_Pack()
│   ├── Should_Fail_On_Missing_Manifest()
│   ├── Should_Verify_Content_Hash()
│   └── Should_Load_All_Components()
│
├── PromptPackRegistryTests.cs
│   ├── Should_Discover_BuiltIn_Packs()
│   ├── Should_Discover_User_Packs()
│   └── Should_Get_Pack_By_Id()
│
├── PromptComposerTests.cs
│   ├── Should_Compose_Base_Prompt()
│   ├── Should_Include_Role_Prompt()
│   ├── Should_Include_Language_Prompt()
│   └── Should_Apply_Template_Variables()
│
└── PackValidatorTests.cs
    ├── Should_Validate_Manifest_Schema()
    ├── Should_Check_Component_Exists()
    └── Should_Validate_Template_Variables()
```

### Integration Tests

```
Tests/Integration/PromptPacks/
├── PromptPackIntegrationTests.cs
│   ├── Should_Load_BuiltIn_Packs()
│   ├── Should_Load_User_Pack()
│   └── Should_Compose_Full_Prompt()
```

---

## User Verification Steps

### Scenario 1: List Packs

1. Run `acode prompts list`
2. Verify: Built-in packs shown
3. Verify: Columns correct

### Scenario 2: Select Pack

1. Set pack_id in config
2. Run `acode status`
3. Verify: Selected pack shown

### Scenario 3: Create Custom Pack

1. Create pack directory
2. Add manifest and components
3. Run validate
4. Verify: Validation passes

### Scenario 4: Template Variables

1. Use {{workspace_name}} in prompt
2. Compose prompt
3. Verify: Variable substituted

### Scenario 5: Hash Verification

1. Modify component file
2. Load pack
3. Verify: Hash mismatch warning

### Scenario 6: Fallback

1. Configure nonexistent pack
2. Start agent
3. Verify: Falls back to default

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/PromptPacks/
├── IPromptPackLoader.cs
├── IPromptPackRegistry.cs
├── IPromptComposer.cs
├── IPackValidator.cs
├── PromptPack.cs
├── PackManifest.cs
├── PackComponent.cs
└── PromptPackConfiguration.cs

src/AgenticCoder.Infrastructure/PromptPacks/
├── PromptPackLoader.cs
├── PromptPackRegistry.cs
├── PromptComposer.cs
├── PackValidator.cs
├── TemplateEngine.cs
├── ContentHasher.cs
└── BuiltInPacks/
    ├── acode-standard/
    ├── acode-dotnet/
    └── acode-react/
```

### IPromptPackRegistry Interface

```csharp
namespace AgenticCoder.Application.PromptPacks;

public interface IPromptPackRegistry
{
    PromptPack GetPack(string packId);
    IReadOnlyList<PromptPack> ListPacks();
    PromptPack GetActivePack();
    void Refresh();
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-PRM-001 | Pack not found |
| ACODE-PRM-002 | Invalid manifest |
| ACODE-PRM-003 | Component file missing |
| ACODE-PRM-004 | Hash mismatch |
| ACODE-PRM-005 | Invalid template variable |
| ACODE-PRM-006 | Pack too large |

### Implementation Checklist

1. [ ] Create IPromptPackLoader
2. [ ] Create IPromptPackRegistry
3. [ ] Create IPromptComposer
4. [ ] Create IPackValidator
5. [ ] Create PromptPack class
6. [ ] Create PackManifest class
7. [ ] Implement PromptPackLoader
8. [ ] Implement PromptPackRegistry
9. [ ] Implement PromptComposer
10. [ ] Implement PackValidator
11. [ ] Implement TemplateEngine
12. [ ] Create built-in packs
13. [ ] Add CLI commands
14. [ ] Write unit tests
15. [ ] Add XML documentation

### Dependencies

- Task 004 (Model Provider uses prompts)
- Task 007 (Tool schemas in prompts)
- YamlDotNet for manifest parsing

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~PromptPacks"
```

---

**End of Task 008 Specification**