# Task 002: Define Repo Contract File (.agent/config.yml)

**Priority:** 8 / 49  
**Tier:** Foundation  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 0 — Foundation  
**Dependencies:** Task 001 (operating modes defined), Task 001.c (constraints documented)  

---

## Description

### Overview

Task 002 defines the repository contract file—a standardized configuration file that allows Acode to understand any repository's structure, build system, and developer preferences. This file, located at `.agent/config.yml`, serves as the primary interface between the repository and the Acode agent.

The repo contract is not optional infrastructure—it is the mechanism by which Acode learns to work with any codebase. Without this contract, Acode must guess at build commands, test runners, and project structure. With this contract, Acode operates with precision and confidence.

### Business Value

The repo contract file enables:

1. **Zero-Configuration Operation** — Acode works correctly on first run when config exists
2. **Repository Portability** — Config travels with the repository, not the developer
3. **Team Consistency** — All team members use same Acode settings
4. **CI/CD Integration** — Automated systems can use same config
5. **Multi-Language Support** — Config abstracts away language differences
6. **Enterprise Customization** — Organizations can standardize configs

### Scope Boundaries

**In Scope:**
- File location and naming (.agent/config.yml)
- Core schema definition
- Mode configuration
- Model provider configuration
- Command group definitions
- Path and ignore patterns
- Validation requirements
- Default values
- Error handling on invalid config
- Migration from older versions

**Out of Scope:**
- Implementation of config parsing (Task 002.b)
- Schema JSON generation (Task 002.a)
- Command execution implementation (later epics)
- Provider implementation (Epic 1)
- Security/secrets handling (beyond basic redaction)

### Integration Points

| Task | Relationship | Description |
|------|--------------|-------------|
| Task 002.a | Subtask | Defines detailed schema |
| Task 002.b | Subtask | Implements parser/validator |
| Task 002.c | Subtask | Defines command groups |
| Task 001 | Producer | Provides mode values |
| Tasks 004-006 | Consumer | Provider config |
| Task 007 | Consumer | Network settings |
| Epic 2 | Consumer | Tool execution config |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Config not found | Fallback to defaults | Clear guidance to create |
| Config invalid YAML | Startup failure | Helpful parse errors |
| Unknown fields | Potential misconfiguration | Warn on unknown fields |
| Mode conflicts | Undefined behavior | Validate mode rules |
| Secrets in config | Security breach | Never log full config |

### Assumptions

1. YAML is acceptable format (widely supported)
2. .agent directory is available for Acode files
3. Repository has write access for config creation
4. Config is version-controlled with repository
5. JSON Schema can be used for validation

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **Repo Contract** | Agreement between repository and Acode agent |
| **.agent/config.yml** | Primary configuration file location |
| **Schema** | Formal definition of configuration structure |
| **YAML** | YAML Ain't Markup Language (config format) |
| **JSON Schema** | Schema language for YAML/JSON validation |
| **Command Group** | Category of related commands (build, test, etc.) |
| **Provider Config** | Settings for LLM provider |
| **Mode Config** | Operating mode settings |
| **Path Pattern** | Glob pattern for file matching |
| **Ignore Pattern** | Pattern for files to skip |
| **Default Value** | Value used when not specified |
| **Config Precedence** | Order of config source priority |
| **Schema Version** | Version identifier for config schema |
| **Migration** | Upgrading config from older schema |
| **Validation** | Checking config against schema |

---

## Out of Scope

- Implementation of YAML parsing library
- Implementation of command execution
- Implementation of provider adapters
- Secret management and encryption
- Config encryption at rest
- Remote config fetching
- Config synchronization across machines
- GUI for config editing
- Config generation wizards
- IDE integration for config editing
- Real-time config reloading
- Config inheritance across repos

---

## Functional Requirements

### File Location and Structure (FR-002-01 to FR-002-20)

| ID | Requirement |
|----|-------------|
| FR-002-01 | Config MUST be located at .agent/config.yml |
| FR-002-02 | Config file MUST use YAML format |
| FR-002-03 | Config MUST support YAML 1.2 specification |
| FR-002-04 | Config MUST have schema_version field |
| FR-002-05 | Schema version MUST follow semver |
| FR-002-06 | Current schema version MUST be 1.0.0 |
| FR-002-07 | Config MUST be UTF-8 encoded |
| FR-002-08 | Config MUST be valid YAML on parse |
| FR-002-09 | Config MUST be under 1MB in size |
| FR-002-10 | Config MUST NOT contain binary data |
| FR-002-11 | Config MUST support comments |
| FR-002-12 | Config MUST be human-readable |
| FR-002-13 | Config MUST be version-controllable |
| FR-002-14 | Alternative locations MUST NOT be supported |
| FR-002-15 | File extension MUST be .yml (not .yaml) |
| FR-002-16 | Directory .agent MUST be created if missing |
| FR-002-17 | Config MUST be loadable without .agent dir |
| FR-002-18 | Empty config file MUST use defaults |
| FR-002-19 | Config MUST be re-readable without restart |
| FR-002-20 | Config changes MUST NOT require reinstall |

### Core Configuration Sections (FR-002-21 to FR-002-45)

| ID | Requirement |
|----|-------------|
| FR-002-21 | Config MUST have project section |
| FR-002-22 | project.name MUST identify the project |
| FR-002-23 | project.type MUST specify project type |
| FR-002-24 | project.languages MUST list languages used |
| FR-002-25 | Config MUST have mode section |
| FR-002-26 | mode.default MUST specify default operating mode |
| FR-002-27 | mode.allow_burst MUST control Burst availability |
| FR-002-28 | Config MUST have model section |
| FR-002-29 | model.provider MUST specify LLM provider |
| FR-002-30 | model.name MUST specify model name |
| FR-002-31 | model.parameters MUST allow model tuning |
| FR-002-32 | Config MUST have commands section |
| FR-002-33 | commands MUST include setup group |
| FR-002-34 | commands MUST include build group |
| FR-002-35 | commands MUST include test group |
| FR-002-36 | commands MUST include lint group |
| FR-002-37 | commands MUST include format group |
| FR-002-38 | commands MUST include start group |
| FR-002-39 | Config MUST have paths section |
| FR-002-40 | paths.source MUST specify source directories |
| FR-002-41 | paths.tests MUST specify test directories |
| FR-002-42 | paths.output MUST specify build output |
| FR-002-43 | Config MUST have ignore section |
| FR-002-44 | ignore.patterns MUST list ignored patterns |
| FR-002-45 | Config MAY have extensions section |

### Mode Configuration (FR-002-46 to FR-002-60)

| ID | Requirement |
|----|-------------|
| FR-002-46 | mode.default MUST accept local-only value |
| FR-002-47 | mode.default MUST accept airgapped value |
| FR-002-48 | mode.default MUST NOT accept burst value |
| FR-002-49 | mode.default MUST default to local-only |
| FR-002-50 | mode.allow_burst MUST be boolean |
| FR-002-51 | mode.allow_burst MUST default to true |
| FR-002-52 | mode.airgapped_lock MUST prevent mode changes |
| FR-002-53 | mode.airgapped_lock MUST be boolean |
| FR-002-54 | mode.airgapped_lock MUST default to false |
| FR-002-55 | Invalid mode values MUST fail validation |
| FR-002-56 | Mode config MUST be case-insensitive |
| FR-002-57 | Mode config MUST log effective mode |
| FR-002-58 | Mode config MUST be overridable by CLI |
| FR-002-59 | Mode config MUST be overridable by env var |
| FR-002-60 | Airgapped lock MUST NOT be overridable |

### Model Configuration (FR-002-61 to FR-002-80)

| ID | Requirement |
|----|-------------|
| FR-002-61 | model.provider MUST specify provider name |
| FR-002-62 | model.provider MUST default to ollama |
| FR-002-63 | model.name MUST specify model identifier |
| FR-002-64 | model.name MUST default to codellama:7b |
| FR-002-65 | model.endpoint MUST allow custom URL |
| FR-002-66 | model.endpoint MUST default to localhost:11434 |
| FR-002-67 | model.parameters.temperature MUST be number |
| FR-002-68 | model.parameters.temperature MUST default to 0.7 |
| FR-002-69 | model.parameters.max_tokens MUST be integer |
| FR-002-70 | model.parameters.max_tokens MUST default to 4096 |
| FR-002-71 | model.parameters.top_p MUST be number 0-1 |
| FR-002-72 | model.parameters.context_window MUST be integer |
| FR-002-73 | model.timeout_seconds MUST be positive integer |
| FR-002-74 | model.timeout_seconds MUST default to 120 |
| FR-002-75 | model.retry_count MUST be non-negative integer |
| FR-002-76 | model.retry_count MUST default to 3 |
| FR-002-77 | Invalid provider MUST fail validation |
| FR-002-78 | External providers MUST respect mode constraints |
| FR-002-79 | Model config MUST be logged (without secrets) |
| FR-002-80 | API keys MUST NOT be in config file |

### Validation Rules (FR-002-81 to FR-002-95)

| ID | Requirement |
|----|-------------|
| FR-002-81 | Config MUST be validated on load |
| FR-002-82 | Schema violations MUST fail with clear error |
| FR-002-83 | Error MUST include line number |
| FR-002-84 | Error MUST include field path |
| FR-002-85 | Error MUST include expected type |
| FR-002-86 | Error MUST include actual value |
| FR-002-87 | Multiple errors MUST be reported together |
| FR-002-88 | Unknown fields MUST generate warnings |
| FR-002-89 | Deprecated fields MUST generate warnings |
| FR-002-90 | Required fields MUST fail if missing |
| FR-002-91 | Type mismatches MUST fail validation |
| FR-002-92 | Range violations MUST fail validation |
| FR-002-93 | Pattern violations MUST fail validation |
| FR-002-94 | Cross-field validation MUST be supported |
| FR-002-95 | Validation MUST complete in under 100ms |

---

## Non-Functional Requirements

### Security (NFR-002-01 to NFR-002-12)

| ID | Requirement |
|----|-------------|
| NFR-002-01 | API keys MUST NOT be stored in config |
| NFR-002-02 | Secrets MUST use environment variables |
| NFR-002-03 | Config MUST NOT be logged in full |
| NFR-002-04 | Sensitive fields MUST be redacted in logs |
| NFR-002-05 | Config parsing MUST be safe (no code exec) |
| NFR-002-06 | YAML anchors MUST be limited |
| NFR-002-07 | Config size MUST be bounded |
| NFR-002-08 | Deeply nested YAML MUST be limited |
| NFR-002-09 | Config MUST NOT include executable YAML |
| NFR-002-10 | Path traversal MUST be prevented |
| NFR-002-11 | Config permissions SHOULD be checked |
| NFR-002-12 | World-writable config SHOULD warn |

### Performance (NFR-002-13 to NFR-002-20)

| ID | Requirement |
|----|-------------|
| NFR-002-13 | Config load MUST complete in under 50ms |
| NFR-002-14 | Config validation MUST complete in under 100ms |
| NFR-002-15 | Config MUST be cached after first load |
| NFR-002-16 | Cache invalidation MUST be supported |
| NFR-002-17 | Config reload MUST NOT block operations |
| NFR-002-18 | Large configs MUST NOT degrade performance |
| NFR-002-19 | Memory usage MUST be under 1MB |
| NFR-002-20 | Config access MUST be O(1) after load |

### Reliability (NFR-002-21 to NFR-002-30)

| ID | Requirement |
|----|-------------|
| NFR-002-21 | Config parsing MUST NOT crash on invalid YAML |
| NFR-002-22 | Missing config MUST use defaults gracefully |
| NFR-002-23 | Corrupt config MUST fail with clear error |
| NFR-002-24 | Config lock MUST handle concurrent access |
| NFR-002-25 | Config file MUST handle encoding issues |
| NFR-002-26 | Config MUST handle BOM markers |
| NFR-002-27 | Config MUST handle line ending variations |
| NFR-002-28 | Config MUST be atomic on reload |
| NFR-002-29 | Partial config MUST NOT be applied |
| NFR-002-30 | Rollback on validation failure MUST work |

---

## User Manual Documentation

### Quick Start

Create `.agent/config.yml` in your repository root:

```yaml
schema_version: "1.0.0"

project:
  name: my-project
  type: dotnet
  languages: [csharp]

mode:
  default: local-only

model:
  provider: ollama
  name: codellama:7b

commands:
  build: dotnet build
  test: dotnet test
  lint: dotnet format --verify-no-changes
  format: dotnet format
```

### Complete Configuration Reference

```yaml
# Acode Repository Configuration
# Schema Version 1.0.0

schema_version: "1.0.0"  # Required

# ─────────────────────────────────────────────────────────────
# Project Information
# ─────────────────────────────────────────────────────────────
project:
  name: my-awesome-project          # Project identifier
  type: dotnet                       # dotnet | node | python | go | rust | java | other
  languages:                         # List of languages used
    - csharp
    - fsharp
  description: Optional description  # For documentation

# ─────────────────────────────────────────────────────────────
# Operating Mode Configuration
# ─────────────────────────────────────────────────────────────
mode:
  default: local-only                # local-only | airgapped (NOT burst)
  allow_burst: true                  # Allow CLI to enter burst mode
  airgapped_lock: false              # If true, cannot change mode

# ─────────────────────────────────────────────────────────────
# Model Provider Configuration
# ─────────────────────────────────────────────────────────────
model:
  provider: ollama                   # ollama | openai | anthropic | azure
  name: codellama:7b                 # Model identifier
  endpoint: http://localhost:11434   # Provider endpoint
  parameters:
    temperature: 0.7                 # 0.0 - 2.0
    max_tokens: 4096                 # Maximum response tokens
    top_p: 0.95                      # Nucleus sampling
    context_window: 8192             # Context size
  timeout_seconds: 120               # Request timeout
  retry_count: 3                     # Retry attempts

# ─────────────────────────────────────────────────────────────
# Command Definitions
# ─────────────────────────────────────────────────────────────
commands:
  # Setup - Run once to prepare environment
  setup:
    - dotnet restore
    - dotnet tool restore
  
  # Build - Compile the project
  build: dotnet build --configuration Release
  
  # Test - Run test suite
  test:
    command: dotnet test
    timeout: 300                     # 5 minutes
    coverage: true                   # Collect coverage
  
  # Lint - Check code quality
  lint: dotnet format --verify-no-changes
  
  # Format - Auto-fix formatting
  format: dotnet format
  
  # Start - Run the application
  start: dotnet run

# ─────────────────────────────────────────────────────────────
# Path Configuration
# ─────────────────────────────────────────────────────────────
paths:
  source:
    - src/
    - lib/
  tests:
    - tests/
  output:
    - bin/
    - obj/
  docs:
    - docs/

# ─────────────────────────────────────────────────────────────
# Ignore Patterns
# ─────────────────────────────────────────────────────────────
ignore:
  patterns:
    - "**/bin/**"
    - "**/obj/**"
    - "**/node_modules/**"
    - "**/.git/**"
    - "**/*.min.js"
  additional:
    - "legacy/"
    - "vendor/"

# ─────────────────────────────────────────────────────────────
# Network Configuration
# ─────────────────────────────────────────────────────────────
network:
  allowlist:
    - host: "127.0.0.1"
      ports: [11434]
      reason: "Ollama"
    - host: "localhost"
      ports: [11434]
      reason: "Ollama"

# ─────────────────────────────────────────────────────────────
# Extensions (Future)
# ─────────────────────────────────────────────────────────────
extensions: {}
```

### Configuration Precedence

Settings are resolved in this order (highest priority first):

1. **CLI Flags** — `acode --mode burst`
2. **Environment Variables** — `ACODE_MODE=burst`
3. **Repository Config** — `.agent/config.yml`
4. **User Config** — `~/.acode/config.yml`
5. **Built-in Defaults**

**Exception:** `airgapped_lock: true` cannot be overridden by any source.

### Default Values

| Field | Default |
|-------|---------|
| mode.default | local-only |
| mode.allow_burst | true |
| mode.airgapped_lock | false |
| model.provider | ollama |
| model.name | codellama:7b |
| model.endpoint | http://localhost:11434 |
| model.parameters.temperature | 0.7 |
| model.parameters.max_tokens | 4096 |
| model.timeout_seconds | 120 |
| model.retry_count | 3 |

### Validating Your Config

```bash
# Validate config file
acode config validate

# Output:
#   ✓ Schema version: 1.0.0
#   ✓ Project: my-project (dotnet)
#   ✓ Mode: local-only
#   ✓ Model: ollama/codellama:7b
#   ✓ Commands: 6 defined
#   ✓ Configuration valid

# Show effective config (with precedence applied)
acode config show

# Show config as JSON
acode config show --format json
```

### Troubleshooting

**Q: Config file not found**
```
Warning: No .agent/config.yml found. Using defaults.
  Create config: acode init
```

**Q: Invalid YAML syntax**
```
Error: Invalid YAML at line 15, column 3
  Expected mapping value, found ':'
  
  14 |   commands:
  15 |     build: : dotnet build
                 ^
```

**Q: Unknown field warning**
```
Warning: Unknown field 'model.foo' will be ignored.
  Did you mean: model.name, model.provider?
```

**Q: Type mismatch**
```
Error: Invalid type for 'mode.default'
  Expected: string (one of: local-only, airgapped)
  Got: number (42)
```

---

## Acceptance Criteria / Definition of Done

### File Structure (25 items)

- [ ] Config at .agent/config.yml
- [ ] YAML 1.2 format supported
- [ ] schema_version field required
- [ ] Version is semver
- [ ] UTF-8 encoding
- [ ] Under 1MB limit
- [ ] Comments preserved
- [ ] Human-readable
- [ ] Version-controllable
- [ ] .yml extension enforced
- [ ] .agent directory auto-created
- [ ] Empty file uses defaults
- [ ] Reloadable without restart
- [ ] Parse errors clear
- [ ] File not found handled
- [ ] Permission errors handled
- [ ] Encoding issues handled
- [ ] BOM handled
- [ ] Line endings handled
- [ ] Symlinks handled
- [ ] Case sensitivity documented
- [ ] Example config provided
- [ ] Schema documentation
- [ ] Changelog for schema
- [ ] Migration guide exists

### Core Sections (25 items)

- [ ] project section defined
- [ ] project.name works
- [ ] project.type works
- [ ] project.languages works
- [ ] mode section defined
- [ ] mode.default works
- [ ] mode.allow_burst works
- [ ] mode.airgapped_lock works
- [ ] model section defined
- [ ] model.provider works
- [ ] model.name works
- [ ] model.endpoint works
- [ ] model.parameters works
- [ ] model.timeout_seconds works
- [ ] model.retry_count works
- [ ] commands section defined
- [ ] All command groups work
- [ ] paths section defined
- [ ] paths.source works
- [ ] paths.tests works
- [ ] ignore section defined
- [ ] ignore.patterns works
- [ ] network section defined
- [ ] network.allowlist works
- [ ] All sections documented

### Validation (30 items)

- [ ] Schema validated on load
- [ ] Clear error messages
- [ ] Line numbers in errors
- [ ] Field paths in errors
- [ ] Expected types shown
- [ ] Actual values shown
- [ ] Multiple errors collected
- [ ] Unknown fields warn
- [ ] Deprecated fields warn
- [ ] Required fields enforced
- [ ] Type checking works
- [ ] Range checking works
- [ ] Pattern checking works
- [ ] Cross-field validation works
- [ ] Validation under 100ms
- [ ] Invalid mode rejected
- [ ] Invalid provider rejected
- [ ] Invalid paths rejected
- [ ] Invalid patterns rejected
- [ ] Circular refs detected
- [ ] Deep nesting limited
- [ ] Anchors limited
- [ ] No code execution
- [ ] Path traversal blocked
- [ ] Validation tests pass
- [ ] Fuzzing tested
- [ ] Edge cases covered
- [ ] Empty values handled
- [ ] Null values handled
- [ ] List vs scalar handled

### Security (20 items)

- [ ] No secrets in config
- [ ] Secrets via env vars
- [ ] Config not logged fully
- [ ] Sensitive fields redacted
- [ ] Safe YAML parsing
- [ ] Anchor limits enforced
- [ ] Size limits enforced
- [ ] Nesting limits enforced
- [ ] No executable YAML
- [ ] Path traversal blocked
- [ ] Permissions checked
- [ ] World-writable warned
- [ ] Injection prevented
- [ ] Template attacks blocked
- [ ] Entity expansion limited
- [ ] Memory limits enforced
- [ ] CPU limits enforced
- [ ] Security tested
- [ ] Audit reviewed
- [ ] CVE-free parser

### Documentation (20 items)

- [ ] Schema documented
- [ ] All fields documented
- [ ] Types documented
- [ ] Defaults documented
- [ ] Examples provided
- [ ] Quick start exists
- [ ] Full reference exists
- [ ] Precedence documented
- [ ] Troubleshooting section
- [ ] FAQ section
- [ ] Error messages documented
- [ ] Migration guide exists
- [ ] Changelog exists
- [ ] JSON Schema published
- [ ] IDE integration docs
- [ ] CLI commands documented
- [ ] Validation documented
- [ ] Best practices documented
- [ ] Anti-patterns documented
- [ ] Changelog maintained

---

## Testing Requirements

### Unit Tests

| ID | Test | Expected |
|----|------|----------|
| UT-002-01 | Parse valid config | Success |
| UT-002-02 | Parse empty config | Uses defaults |
| UT-002-03 | Parse invalid YAML | Clear error |
| UT-002-04 | Validate schema version | Passes |
| UT-002-05 | Validate missing required | Fails |
| UT-002-06 | Validate type mismatch | Fails |
| UT-002-07 | Validate range violation | Fails |
| UT-002-08 | Validate unknown field | Warns |
| UT-002-09 | Default values applied | Correct |
| UT-002-10 | mode.default = local-only | Works |
| UT-002-11 | mode.default = airgapped | Works |
| UT-002-12 | mode.default = burst | Fails |
| UT-002-13 | model.provider = ollama | Works |
| UT-002-14 | model.parameters parsed | Correct |
| UT-002-15 | commands section parsed | Correct |
| UT-002-16 | paths section parsed | Correct |
| UT-002-17 | ignore patterns parsed | Correct |
| UT-002-18 | Large config (1MB) | Works |
| UT-002-19 | Deep nesting (10 levels) | Works |
| UT-002-20 | Unicode in values | Works |

### Integration Tests

| ID | Test | Expected |
|----|------|----------|
| IT-002-01 | Load config from file | Success |
| IT-002-02 | Config not found | Uses defaults |
| IT-002-03 | Config with BOM | Works |
| IT-002-04 | Config with CRLF | Works |
| IT-002-05 | Config reload | Works |
| IT-002-06 | CLI override | Wins |
| IT-002-07 | Env var override | Wins |
| IT-002-08 | Precedence correct | Verified |
| IT-002-09 | Mode constraint respected | Enforced |
| IT-002-10 | Airgapped lock works | Cannot override |

### End-to-End Tests

| ID | Test | Expected |
|----|------|----------|
| E2E-002-01 | acode init creates config | File created |
| E2E-002-02 | acode config validate | Shows result |
| E2E-002-03 | acode config show | Displays config |
| E2E-002-04 | Invalid config on startup | Clear error |
| E2E-002-05 | Config in real repository | Works |
| E2E-002-06 | Team shares config via git | Works |
| E2E-002-07 | Config with all sections | Works |
| E2E-002-08 | Config with minimal sections | Works |

### Performance Benchmarks

| ID | Metric | Target |
|----|--------|--------|
| PB-002-01 | Config load time | < 50ms |
| PB-002-02 | Config validate time | < 100ms |
| PB-002-03 | Config memory usage | < 1MB |
| PB-002-04 | 1MB config load | < 200ms |
| PB-002-05 | Cache hit time | < 1ms |

---

## User Verification Steps

### Verification 1: Create Config File
1. Run `acode init` in new repository
2. Check .agent/config.yml exists
3. **Verify:** File created with valid content

### Verification 2: Validate Config
1. Run `acode config validate`
2. **Verify:** Shows validation success

### Verification 3: Invalid Config Detected
1. Add syntax error to config
2. Run `acode config validate`
3. **Verify:** Clear error with line number

### Verification 4: Mode Setting Works
1. Set `mode.default: airgapped` in config
2. Run `acode config show`
3. **Verify:** Mode shows airgapped

### Verification 5: CLI Override Works
1. Set mode in config
2. Run `acode --mode local-only config show`
3. **Verify:** CLI flag wins

### Verification 6: Airgapped Lock Works
1. Set `airgapped_lock: true` in config
2. Run `acode --mode burst analyze`
3. **Verify:** Burst mode denied

### Verification 7: Model Config Works
1. Configure custom model settings
2. Run operation using model
3. **Verify:** Settings applied

### Verification 8: Commands Defined
1. Define commands in config
2. Run `acode exec build`
3. **Verify:** Command executed

### Verification 9: Defaults Work
1. Create minimal config
2. Run `acode config show`
3. **Verify:** All defaults applied

### Verification 10: Unknown Fields Warn
1. Add unknown field to config
2. Run any command
3. **Verify:** Warning shown

---

## Implementation Prompt for Claude

### Files to Create

```
src/Acode.Domain/
├── Configuration/
│   ├── RepoConfig.cs           # Domain model
│   ├── ModeConfig.cs           # Mode settings
│   ├── ModelConfig.cs          # Model settings
│   ├── CommandConfig.cs        # Command settings
│   └── IConfigValidator.cs     # Validation interface
│
src/Acode.Infrastructure/
├── Configuration/
│   ├── YamlConfigLoader.cs     # YAML parsing
│   ├── ConfigValidator.cs      # Validation impl
│   ├── ConfigCache.cs          # Caching
│   └── ConfigMerger.cs         # Precedence handling
│
data/
├── config-schema.json          # JSON Schema
│
docs/
├── config-reference.md         # Full reference
└── config-examples/            # Example configs
    ├── dotnet.yml
    ├── node.yml
    └── python.yml
```

### Core Types

```csharp
namespace Acode.Domain.Configuration;

/// <summary>
/// Root configuration model for .agent/config.yml
/// </summary>
public sealed record RepoConfig
{
    public required string SchemaVersion { get; init; }
    public required ProjectConfig Project { get; init; }
    public ModeConfig Mode { get; init; } = new();
    public ModelConfig Model { get; init; } = new();
    public CommandsConfig Commands { get; init; } = new();
    public PathsConfig Paths { get; init; } = new();
    public IgnoreConfig Ignore { get; init; } = new();
    public NetworkConfig? Network { get; init; }
}

public sealed record ModeConfig
{
    public OperatingMode Default { get; init; } = OperatingMode.LocalOnly;
    public bool AllowBurst { get; init; } = true;
    public bool AirgappedLock { get; init; } = false;
}

public sealed record ModelConfig
{
    public string Provider { get; init; } = "ollama";
    public string Name { get; init; } = "codellama:7b";
    public string Endpoint { get; init; } = "http://localhost:11434";
    public ModelParameters Parameters { get; init; } = new();
    public int TimeoutSeconds { get; init; } = 120;
    public int RetryCount { get; init; } = 3;
}

public sealed record ModelParameters
{
    public double Temperature { get; init; } = 0.7;
    public int MaxTokens { get; init; } = 4096;
    public double TopP { get; init; } = 0.95;
    public int ContextWindow { get; init; } = 8192;
}
```

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 20 | Config file not found (with defaults used) |
| 21 | Config parse error (invalid YAML) |
| 22 | Config validation error |
| 23 | Schema version mismatch |
| 24 | Config permission error |
| 25 | Config too large |

### Validation Checklist Before Merge

- [ ] All config sections implemented
- [ ] All default values correct
- [ ] Validation comprehensive
- [ ] Error messages helpful
- [ ] Performance targets met
- [ ] Security review passed
- [ ] Documentation complete
- [ ] Examples work
- [ ] Tests passing
- [ ] Schema published

---

**END OF TASK 002**
