# Task 002.a: Define Schema + Examples

**Priority:** 9 / 49  
**Tier:** Foundation  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 0 — Foundation  
**Dependencies:** Task 002 (parent task defines config contract)  

---

## Description

### Overview

Task 002.a defines the formal JSON Schema for the `.agent/config.yml` configuration file and provides comprehensive examples for all supported project types. The schema serves as the authoritative specification for config validation, IDE autocompletion, and documentation generation.

A well-designed schema enables tooling integration, prevents configuration errors, and provides developers with immediate feedback when editing configuration files. The examples serve as templates that developers can copy and customize.

### Business Value

Formal schema definition provides:

1. **IDE Integration** — Autocompletion and inline validation in VS Code, JetBrains, etc.
2. **Error Prevention** — Catch config errors before runtime
3. **Documentation Generation** — Auto-generate config reference from schema
4. **Validation Automation** — Single source of truth for validators
5. **API Contracts** — Clear contract for configuration structure

### Scope Boundaries

**In Scope:**
- JSON Schema definition (Draft 2020-12)
- All config sections in schema
- Type definitions for all fields
- Default value specifications
- Constraint definitions (min/max, patterns, enums)
- Schema documentation annotations
- Example configs for: .NET, Node.js, Python, Go, Rust, Java
- Minimal example (quick start)
- Full example (all options)
- Invalid examples with explanations

**Out of Scope:**
- Parser implementation (Task 002.b)
- Validator implementation (Task 002.b)
- Command execution (Task 002.c)
- IDE plugin development
- Schema registry publication

### Integration Points

| Task | Relationship | Description |
|------|--------------|-------------|
| Task 002 | Parent | Provides config structure |
| Task 002.b | Consumer | Uses schema for validation |
| Task 002.c | Consumer | Command group schemas |
| All tasks | Consumer | All use config schema |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Schema incomplete | Validation gaps | Comprehensive testing |
| Schema too strict | Valid configs rejected | Real-world testing |
| Schema too loose | Invalid configs accepted | Edge case coverage |
| Examples outdated | User confusion | Version with schema |

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **JSON Schema** | Vocabulary for annotating and validating JSON/YAML |
| **Draft 2020-12** | Latest JSON Schema specification version |
| **$schema** | Declaration of which schema version is used |
| **$defs** | Reusable schema definitions |
| **$ref** | Reference to another schema definition |
| **enum** | Restricted set of allowed values |
| **pattern** | Regular expression constraint |
| **additionalProperties** | Controls unknown property handling |
| **required** | List of mandatory properties |
| **default** | Default value when not specified |
| **description** | Human-readable documentation |
| **examples** | Example values for documentation |

---

## Out of Scope

- Parser code implementation
- Validator code implementation
- Schema compilation to code
- IDE plugin development
- Schema registry hosting
- Schema versioning infrastructure
- Backward compatibility enforcement
- Schema migration tooling
- GraphQL schema generation
- OpenAPI schema generation

---

## Functional Requirements

### JSON Schema Structure (FR-002a-01 to FR-002a-25)

| ID | Requirement |
|----|-------------|
| FR-002a-01 | Schema MUST use JSON Schema Draft 2020-12 |
| FR-002a-02 | Schema MUST have $schema declaration |
| FR-002a-03 | Schema MUST have $id for identification |
| FR-002a-04 | Schema MUST have title and description |
| FR-002a-05 | Schema MUST define type as object |
| FR-002a-06 | Schema MUST list required properties |
| FR-002a-07 | Schema MUST define all properties |
| FR-002a-08 | Schema MUST use $defs for reuse |
| FR-002a-09 | Schema MUST use $ref for references |
| FR-002a-10 | All properties MUST have descriptions |
| FR-002a-11 | All properties MUST have types |
| FR-002a-12 | Enums MUST have all valid values |
| FR-002a-13 | Patterns MUST be valid regex |
| FR-002a-14 | Defaults MUST be specified |
| FR-002a-15 | Examples MUST be included |
| FR-002a-16 | Schema MUST be valid JSON |
| FR-002a-17 | Schema MUST pass meta-validation |
| FR-002a-18 | Schema MUST be readable |
| FR-002a-19 | Schema MUST be maintainable |
| FR-002a-20 | Schema version MUST be tracked |
| FR-002a-21 | Breaking changes MUST increment major |
| FR-002a-22 | additionalProperties MUST be controlled |
| FR-002a-23 | Nested objects MUST have schemas |
| FR-002a-24 | Arrays MUST have item schemas |
| FR-002a-25 | Constraints MUST match code |

### Property Definitions (FR-002a-26 to FR-002a-55)

| ID | Requirement |
|----|-------------|
| FR-002a-26 | schema_version MUST be string pattern |
| FR-002a-27 | schema_version pattern MUST be semver |
| FR-002a-28 | project.name MUST be string |
| FR-002a-29 | project.name MUST have pattern |
| FR-002a-30 | project.type MUST be enum |
| FR-002a-31 | project.type enum MUST list all types |
| FR-002a-32 | project.languages MUST be array |
| FR-002a-33 | project.languages items MUST be strings |
| FR-002a-34 | mode.default MUST be enum |
| FR-002a-35 | mode.default enum MUST exclude burst |
| FR-002a-36 | mode.allow_burst MUST be boolean |
| FR-002a-37 | mode.airgapped_lock MUST be boolean |
| FR-002a-38 | model.provider MUST be string |
| FR-002a-39 | model.name MUST be string |
| FR-002a-40 | model.endpoint MUST be URI format |
| FR-002a-41 | model.parameters MUST be object |
| FR-002a-42 | temperature MUST be number 0-2 |
| FR-002a-43 | max_tokens MUST be integer > 0 |
| FR-002a-44 | top_p MUST be number 0-1 |
| FR-002a-45 | timeout_seconds MUST be integer > 0 |
| FR-002a-46 | retry_count MUST be integer >= 0 |
| FR-002a-47 | commands MUST be object |
| FR-002a-48 | command values MUST be string or object |
| FR-002a-49 | paths MUST be object of arrays |
| FR-002a-50 | path items MUST be strings |
| FR-002a-51 | ignore.patterns MUST be array |
| FR-002a-52 | pattern items MUST be strings |
| FR-002a-53 | network MUST be optional object |
| FR-002a-54 | allowlist MUST be array of objects |
| FR-002a-55 | allowlist items MUST have host, ports |

### Example Configurations (FR-002a-56 to FR-002a-80)

| ID | Requirement |
|----|-------------|
| FR-002a-56 | Minimal example MUST exist |
| FR-002a-57 | Minimal example MUST be valid |
| FR-002a-58 | Full example MUST exist |
| FR-002a-59 | Full example MUST show all options |
| FR-002a-60 | .NET example MUST exist |
| FR-002a-61 | .NET example MUST be realistic |
| FR-002a-62 | Node.js example MUST exist |
| FR-002a-63 | Node.js example MUST be realistic |
| FR-002a-64 | Python example MUST exist |
| FR-002a-65 | Python example MUST be realistic |
| FR-002a-66 | Go example MUST exist |
| FR-002a-67 | Go example MUST be realistic |
| FR-002a-68 | Rust example MUST exist |
| FR-002a-69 | Rust example MUST be realistic |
| FR-002a-70 | Java example MUST exist |
| FR-002a-71 | Java example MUST be realistic |
| FR-002a-72 | All examples MUST pass validation |
| FR-002a-73 | All examples MUST have comments |
| FR-002a-74 | All examples MUST be documented |
| FR-002a-75 | Invalid example MUST exist |
| FR-002a-76 | Invalid example MUST explain errors |
| FR-002a-77 | Examples MUST be copy-pasteable |
| FR-002a-78 | Examples MUST use defaults where appropriate |
| FR-002a-79 | Examples MUST be version-controlled |
| FR-002a-80 | Examples MUST be tested in CI |

---

## Non-Functional Requirements

### Correctness (NFR-002a-01 to NFR-002a-10)

| ID | Requirement |
|----|-------------|
| NFR-002a-01 | Schema MUST match implementation |
| NFR-002a-02 | Schema MUST validate valid configs |
| NFR-002a-03 | Schema MUST reject invalid configs |
| NFR-002a-04 | Schema MUST match documentation |
| NFR-002a-05 | Schema MUST be tested |
| NFR-002a-06 | Edge cases MUST be covered |
| NFR-002a-07 | Boundaries MUST be correct |
| NFR-002a-08 | Patterns MUST work |
| NFR-002a-09 | Enums MUST be complete |
| NFR-002a-10 | Defaults MUST be accurate |

### Usability (NFR-002a-11 to NFR-002a-18)

| ID | Requirement |
|----|-------------|
| NFR-002a-11 | Schema MUST enable IDE completion |
| NFR-002a-12 | Error messages MUST be helpful |
| NFR-002a-13 | Documentation MUST be comprehensive |
| NFR-002a-14 | Examples MUST be clear |
| NFR-002a-15 | Structure MUST be logical |
| NFR-002a-16 | Naming MUST be consistent |
| NFR-002a-17 | Complexity MUST be minimized |
| NFR-002a-18 | Learning curve MUST be low |

### Performance (NFR-002a-19 to NFR-002a-24)

| ID | Requirement |
|----|-------------|
| NFR-002a-19 | Schema size MUST be under 100KB |
| NFR-002a-20 | Validation MUST be under 100ms |
| NFR-002a-21 | IDE load MUST be fast |
| NFR-002a-22 | No circular references |
| NFR-002a-23 | Reasonable nesting depth |
| NFR-002a-24 | Efficient pattern matching |

---

## User Manual Documentation

### JSON Schema Overview

The Acode configuration schema is available at:
- Repository: `data/config-schema.json`
- Published: `https://acode.dev/schemas/config-v1.json`

### IDE Integration

#### VS Code

Add to your config file:
```yaml
# yaml-language-server: $schema=https://acode.dev/schemas/config-v1.json
schema_version: "1.0.0"
...
```

Or add to VS Code settings:
```json
{
  "yaml.schemas": {
    "https://acode.dev/schemas/config-v1.json": ".agent/config.yml"
  }
}
```

#### JetBrains IDEs

The schema is auto-detected from the file pattern `.agent/config.yml`.

### Schema Structure

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://acode.dev/schemas/config-v1.json",
  "title": "Acode Configuration",
  "description": "Configuration schema for .agent/config.yml",
  "type": "object",
  "required": ["schema_version"],
  "properties": {
    "schema_version": { ... },
    "project": { ... },
    "mode": { ... },
    "model": { ... },
    "commands": { ... },
    "paths": { ... },
    "ignore": { ... },
    "network": { ... }
  }
}
```

### Example: Minimal Configuration

```yaml
# Minimal .agent/config.yml
schema_version: "1.0.0"

project:
  name: my-project
  type: dotnet
```

### Example: .NET Project

```yaml
# .agent/config.yml for .NET project
schema_version: "1.0.0"

project:
  name: my-dotnet-app
  type: dotnet
  languages: [csharp, fsharp]
  description: A .NET 8 web application

mode:
  default: local-only
  allow_burst: true

model:
  provider: ollama
  name: codellama:7b
  parameters:
    temperature: 0.7
    max_tokens: 4096

commands:
  setup:
    - dotnet restore
    - dotnet tool restore
  build: dotnet build --configuration Release
  test: dotnet test --collect:"XPlat Code Coverage"
  lint: dotnet format --verify-no-changes
  format: dotnet format
  start: dotnet run --project src/MyApp

paths:
  source: [src/]
  tests: [tests/]
  output: [bin/, obj/]

ignore:
  patterns:
    - "**/bin/**"
    - "**/obj/**"
    - "**/.vs/**"
```

### Example: Node.js Project

```yaml
# .agent/config.yml for Node.js project
schema_version: "1.0.0"

project:
  name: my-node-app
  type: node
  languages: [typescript, javascript]

mode:
  default: local-only

model:
  provider: ollama
  name: codellama:7b

commands:
  setup: npm install
  build: npm run build
  test: npm test
  lint: npm run lint
  format: npm run format
  start: npm start

paths:
  source: [src/]
  tests: [tests/, __tests__/]
  output: [dist/, build/]

ignore:
  patterns:
    - "**/node_modules/**"
    - "**/dist/**"
    - "**/.next/**"
```

### Example: Python Project

```yaml
# .agent/config.yml for Python project
schema_version: "1.0.0"

project:
  name: my-python-app
  type: python
  languages: [python]

mode:
  default: local-only

model:
  provider: ollama
  name: codellama:7b

commands:
  setup:
    - python -m venv .venv
    - pip install -r requirements.txt
  build: python -m build
  test: pytest
  lint: ruff check .
  format: ruff format .
  start: python -m myapp

paths:
  source: [src/, myapp/]
  tests: [tests/]
  output: [dist/, build/]

ignore:
  patterns:
    - "**/__pycache__/**"
    - "**/.venv/**"
    - "**/*.egg-info/**"
```

### Example: Invalid Configuration

```yaml
# INVALID - This will fail validation

schema_version: 1.0.0  # ERROR: Must be string "1.0.0"

project:
  name: my project  # ERROR: Spaces not allowed in name
  type: cpp         # ERROR: 'cpp' not in allowed types

mode:
  default: burst    # ERROR: 'burst' cannot be default
  allow_burst: "yes"  # ERROR: Must be boolean

model:
  parameters:
    temperature: 5.0  # ERROR: Must be 0-2
    max_tokens: -100  # ERROR: Must be positive
```

---

## Acceptance Criteria / Definition of Done

### Schema Definition (30 items)

- [ ] Schema uses Draft 2020-12
- [ ] $schema declared
- [ ] $id set correctly
- [ ] Title and description present
- [ ] All properties defined
- [ ] All types specified
- [ ] All required fields listed
- [ ] All defaults specified
- [ ] All descriptions present
- [ ] All enums complete
- [ ] All patterns valid
- [ ] All constraints correct
- [ ] $defs used for reuse
- [ ] $refs work correctly
- [ ] No circular references
- [ ] additionalProperties controlled
- [ ] Nested schemas complete
- [ ] Array schemas complete
- [ ] Schema is valid JSON
- [ ] Schema passes meta-validation
- [ ] Schema versioned
- [ ] Schema documented
- [ ] Schema matches code
- [ ] Schema matches docs
- [ ] Schema tested
- [ ] Schema reviewed
- [ ] Schema under 100KB
- [ ] IDE integration works
- [ ] Completion works
- [ ] Validation works

### Examples (25 items)

- [ ] Minimal example exists
- [ ] Full example exists
- [ ] .NET example exists
- [ ] Node.js example exists
- [ ] Python example exists
- [ ] Go example exists
- [ ] Rust example exists
- [ ] Java example exists
- [ ] All examples valid
- [ ] All examples documented
- [ ] All examples commented
- [ ] All examples tested
- [ ] Invalid example exists
- [ ] Invalid example explained
- [ ] Examples in version control
- [ ] Examples tested in CI
- [ ] Examples copy-pasteable
- [ ] Examples use defaults
- [ ] Examples are realistic
- [ ] Examples are complete
- [ ] Examples are minimal
- [ ] Examples are consistent
- [ ] Examples match schema
- [ ] Examples reviewed
- [ ] Examples published

### Testing (20 items)

- [ ] Valid configs pass
- [ ] Invalid configs fail
- [ ] Edge cases tested
- [ ] Boundaries tested
- [ ] Patterns tested
- [ ] Enums tested
- [ ] Defaults tested
- [ ] Types tested
- [ ] Required tested
- [ ] Optional tested
- [ ] Nested tested
- [ ] Arrays tested
- [ ] All examples validated
- [ ] Schema meta-validated
- [ ] Performance tested
- [ ] IDE integration tested
- [ ] Cross-platform tested
- [ ] Regression tests exist
- [ ] Fuzzing performed
- [ ] Coverage complete

---

## Testing Requirements

### Unit Tests

| ID | Test | Expected |
|----|------|----------|
| UT-002a-01 | Schema is valid JSON | Parses |
| UT-002a-02 | Schema passes meta-validation | Valid |
| UT-002a-03 | Minimal example validates | Pass |
| UT-002a-04 | Full example validates | Pass |
| UT-002a-05 | .NET example validates | Pass |
| UT-002a-06 | Node.js example validates | Pass |
| UT-002a-07 | Python example validates | Pass |
| UT-002a-08 | Invalid example fails | Fail |
| UT-002a-09 | Missing schema_version fails | Fail |
| UT-002a-10 | Invalid mode.default fails | Fail |
| UT-002a-11 | Invalid temperature fails | Fail |
| UT-002a-12 | Unknown field warns | Warn |
| UT-002a-13 | Pattern validation works | Correct |
| UT-002a-14 | Enum validation works | Correct |
| UT-002a-15 | Type validation works | Correct |

### Integration Tests

| ID | Test | Expected |
|----|------|----------|
| IT-002a-01 | VS Code loads schema | Works |
| IT-002a-02 | Completion works | Suggestions |
| IT-002a-03 | Inline validation works | Errors shown |
| IT-002a-04 | Parser uses schema | Validated |
| IT-002a-05 | Schema URL accessible | 200 OK |

### Performance Benchmarks

| ID | Metric | Target |
|----|--------|--------|
| PB-002a-01 | Schema load time | < 50ms |
| PB-002a-02 | Validation time | < 100ms |
| PB-002a-03 | Schema size | < 100KB |

---

## User Verification Steps

### Verification 1: Schema Valid
1. Load config-schema.json in JSON validator
2. **Verify:** No schema errors

### Verification 2: Example Validates
1. Run validator on minimal example
2. **Verify:** Validation passes

### Verification 3: IDE Completion
1. Open config in VS Code with schema
2. Type partial property name
3. **Verify:** Completion suggestions appear

### Verification 4: IDE Error Detection
1. Add invalid value to config
2. **Verify:** Red underline and error message

### Verification 5: All Examples Valid
1. Validate each example file
2. **Verify:** All pass validation

---

## Implementation Prompt for Claude

### Files to Create

```
data/
├── config-schema.json           # Main JSON Schema
│
docs/
├── config-examples/
│   ├── minimal.yml              # Minimal example
│   ├── full.yml                 # All options
│   ├── dotnet.yml               # .NET example
│   ├── node.yml                 # Node.js example
│   ├── python.yml               # Python example
│   ├── go.yml                   # Go example
│   ├── rust.yml                 # Rust example
│   ├── java.yml                 # Java example
│   └── invalid.yml              # Invalid example
```

### JSON Schema Content

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://acode.dev/schemas/config-v1.json",
  "title": "Acode Configuration",
  "description": "Configuration schema for Acode .agent/config.yml",
  "type": "object",
  "required": ["schema_version"],
  "additionalProperties": false,
  "properties": {
    "schema_version": {
      "type": "string",
      "pattern": "^\\d+\\.\\d+\\.\\d+$",
      "description": "Schema version (semver)",
      "default": "1.0.0",
      "examples": ["1.0.0"]
    },
    "project": {
      "$ref": "#/$defs/project"
    },
    "mode": {
      "$ref": "#/$defs/mode"
    },
    "model": {
      "$ref": "#/$defs/model"
    },
    "commands": {
      "$ref": "#/$defs/commands"
    },
    "paths": {
      "$ref": "#/$defs/paths"
    },
    "ignore": {
      "$ref": "#/$defs/ignore"
    },
    "network": {
      "$ref": "#/$defs/network"
    }
  },
  "$defs": {
    "project": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string",
          "pattern": "^[a-z0-9][a-z0-9-_]*$",
          "description": "Project identifier"
        },
        "type": {
          "type": "string",
          "enum": ["dotnet", "node", "python", "go", "rust", "java", "other"],
          "description": "Project type"
        },
        "languages": {
          "type": "array",
          "items": { "type": "string" },
          "description": "Programming languages used"
        },
        "description": {
          "type": "string",
          "description": "Project description"
        }
      }
    },
    "mode": {
      "type": "object",
      "properties": {
        "default": {
          "type": "string",
          "enum": ["local-only", "airgapped"],
          "default": "local-only",
          "description": "Default operating mode (burst not allowed)"
        },
        "allow_burst": {
          "type": "boolean",
          "default": true,
          "description": "Allow CLI to enter burst mode"
        },
        "airgapped_lock": {
          "type": "boolean",
          "default": false,
          "description": "Lock to airgapped mode permanently"
        }
      }
    },
    "model": {
      "type": "object",
      "properties": {
        "provider": {
          "type": "string",
          "default": "ollama",
          "description": "LLM provider name"
        },
        "name": {
          "type": "string",
          "default": "codellama:7b",
          "description": "Model identifier"
        },
        "endpoint": {
          "type": "string",
          "format": "uri",
          "default": "http://localhost:11434",
          "description": "Provider endpoint URL"
        },
        "parameters": {
          "type": "object",
          "properties": {
            "temperature": {
              "type": "number",
              "minimum": 0,
              "maximum": 2,
              "default": 0.7
            },
            "max_tokens": {
              "type": "integer",
              "minimum": 1,
              "default": 4096
            },
            "top_p": {
              "type": "number",
              "minimum": 0,
              "maximum": 1,
              "default": 0.95
            }
          }
        },
        "timeout_seconds": {
          "type": "integer",
          "minimum": 1,
          "default": 120
        },
        "retry_count": {
          "type": "integer",
          "minimum": 0,
          "default": 3
        }
      }
    },
    "commands": {
      "type": "object",
      "properties": {
        "setup": { "$ref": "#/$defs/command" },
        "build": { "$ref": "#/$defs/command" },
        "test": { "$ref": "#/$defs/command" },
        "lint": { "$ref": "#/$defs/command" },
        "format": { "$ref": "#/$defs/command" },
        "start": { "$ref": "#/$defs/command" }
      }
    },
    "command": {
      "oneOf": [
        { "type": "string" },
        { "type": "array", "items": { "type": "string" } },
        {
          "type": "object",
          "properties": {
            "command": { "type": "string" },
            "timeout": { "type": "integer" },
            "env": { "type": "object" }
          },
          "required": ["command"]
        }
      ]
    },
    "paths": {
      "type": "object",
      "properties": {
        "source": { "type": "array", "items": { "type": "string" } },
        "tests": { "type": "array", "items": { "type": "string" } },
        "output": { "type": "array", "items": { "type": "string" } },
        "docs": { "type": "array", "items": { "type": "string" } }
      }
    },
    "ignore": {
      "type": "object",
      "properties": {
        "patterns": { "type": "array", "items": { "type": "string" } },
        "additional": { "type": "array", "items": { "type": "string" } }
      }
    },
    "network": {
      "type": "object",
      "properties": {
        "allowlist": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "host": { "type": "string" },
              "ports": { "type": "array", "items": { "type": "integer" } },
              "reason": { "type": "string" }
            },
            "required": ["host"]
          }
        }
      }
    }
  }
}
```

### Validation Checklist Before Merge

- [ ] Schema is valid JSON Schema
- [ ] Schema passes meta-validation
- [ ] All examples validate
- [ ] Invalid example fails
- [ ] IDE integration tested
- [ ] Documentation complete
- [ ] Schema versioned
- [ ] Schema published

---

**END OF TASK 002.a**
