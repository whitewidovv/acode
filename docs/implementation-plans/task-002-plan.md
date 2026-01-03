# Task 002 Implementation Plan - Repo Contract (.agent/config.yml)

**Task:** Define Repo Contract File (.agent/config.yml)
**Branch:** `feature/task-002-repo-contract`
**Started:** 2026-01-03
**Status:** In Progress

---

## Strategic Approach

Task 002 establishes the configuration contract for `.agent/config.yml`. This task is broken into three subtasks:

- **Task 002.a** - Define JSON Schema + Example Configs
- **Task 002.b** - Implement Parser + Validator (Domain models, validation logic)
- **Task 002.c** - Define Command Groups (setup, build, test, lint, format, start)

**Key Insight:** Unlike typical config implementations, we're approaching this from a SCHEMA-FIRST perspective. The JSON Schema is the source of truth, with comprehensive examples serving as living documentation and validation test cases.

**Implementation Order:**
1. Create JSON Schema (002.a)
2. Create example configurations (002.a)
3. Implement domain models (002.b - foundational)
4. Defer parser/validator implementation (002.b - depends on YamlDotNet/NJsonSchema packages not yet added)
5. Document command groups (002.c - specification only for now)

**Rationale:** We can deliver value incrementally by defining the contract (schema + examples) without blocking on full parser implementation. This allows other tasks to reference the spec while we add dependencies.

---

## Task 002.a - Schema + Examples

### Strategy

Create authoritative JSON Schema and comprehensive example configurations. The schema serves triple duty:
1. **Developer UX** - IDE autocompletion and inline validation
2. **Documentation** - Single source of truth for config structure
3. **Validation** - Will be used by parser implementation

### Completed
(None yet)

### In Progress
🔄 JSON Schema definition

### Remaining
- JSON Schema Draft 2020-12 with all properties
- $defs for reusable definitions
- Validation constraints (patterns, enums, ranges)
- Example configs:
  - minimal.yml (quick start)
  - full.yml (all options with comments)
  - dotnet.yml (.NET project)
  - node.yml (Node.js project)
  - python.yml (Python project)
  - go.yml (Go project)
  - rust.yml (Rust project)
  - java.yml (Java project)
  - invalid.yml (with error explanations)

---

## Task 002.b - Parser + Validator

### Strategy

**IMPORTANT:** This subtask requires external dependencies (YamlDotNet, NJsonSchema) which haven't been added to the project yet. We'll implement the foundational domain models now, but defer the actual parser/validator implementation to a follow-up task after dependencies are added.

**What we'll implement NOW:**
- Domain models (records) for configuration
- Validation error types and error codes
- Default value constants
- Interfaces for parser/validator (contracts)

**What we'll implement LATER (separate task/PR):**
- Actual YAML parser implementation
- JSON Schema validator implementation
- Environment variable interpolation
- Semantic validation rules
- Config caching

### Completed
(None yet)

### In Progress
🔄 Domain models

### Remaining (Now)
- AcodeConfig record
- ProjectConfig, ModeConfig, ModelConfig records
- ModelParametersConfig, CommandsConfig records
- PathsConfig, IgnoreConfig, NetworkConfig records
- ConfigDefaults static class
- ValidationResult, ValidationError records
- ConfigErrorCodes static class
- IConfigLoader, IConfigValidator interfaces

### Deferred (Later PR)
- YamlConfigReader (Infrastructure)
- JsonSchemaValidator (Infrastructure)
- ConfigLoader, ConfigValidator (Application)
- EnvironmentInterpolator (Application)
- DefaultValueApplicator (Application)
- SemanticValidator (Application)

---

## Task 002.c - Command Groups

### Strategy

Define the six command groups (setup, build, test, lint, format, start) as domain models and specification documentation. Command execution is out of scope for this PR - we're defining the CONTRACT only.

### Completed
(None yet)

### In Progress
(Pending 002.a completion)

### Remaining
- CommandGroup enum
- CommandSpec record
- CommandOptions record
- CommandResult record
- ExitCodeDescriptions static class
- PlatformVariant definitions
- ICommandParser, ICommandExecutor interfaces
- Command group documentation

---

## Deliverables Summary

### Documentation Artifacts
1. **data/config-schema.json** - JSON Schema Draft 2020-12
2. **docs/config-examples/*.yml** - 9 example configurations
3. **docs/config-examples/README.md** - Example documentation

### Domain Models (Code)
4. **src/Acode.Domain/Configuration/*** - 9+ config model records
5. **src/Acode.Domain/Commands/*** - 5+ command model records

### Validation Infrastructure (Code)
6. **src/Acode.Domain/Configuration/ConfigDefaults.cs** - Default constants
7. **src/Acode.Domain/Configuration/ConfigErrorCodes.cs** - Error code constants
8. **src/Acode.Application/Configuration/I*.cs** - Interface contracts

### Test Coverage (Code)
9. **tests/Acode.Domain.Tests/Configuration/*** - Domain model tests
10. **tests/Acode.Domain.Tests/Commands/*** - Command model tests

**Target:** 40+ unit tests for domain models, all passing, 0 warnings

---

## Test Coverage Goals

### Task 002.a (Schema + Examples)
- Schema passes meta-validation ✓
- All examples validate against schema ✓
- Invalid example fails validation with expected errors ✓
- Schema is valid JSON ✓
- Schema size < 100KB ✓

### Task 002.b (Domain Models)
- AcodeConfig construction and equality ✓
- All nested config objects construction ✓
- Default values match specification ✓
- Immutability enforced ✓
- ToString() works for debugging ✓
- Serialization to JSON works ✓

### Task 002.c (Command Models)
- CommandGroup enum has exactly 6 values ✓
- CommandSpec validation ✓
- Exit code descriptions ✓
- Platform variant selection ✓

---

## Build Status

🔄 Build: TBD
🔄 Tests: TBD
🔄 All commits pushed: TBD

---

## Next Steps

1. Create feature branch
2. Implement Task 002.a (JSON Schema + Examples)
3. Implement Task 002.b domain models
4. Implement Task 002.c domain models
5. Add comprehensive unit tests
6. Create PR for review
7. **FUTURE PR:** Add YamlDotNet/NJsonSchema packages and implement full parser

---

## Notes

- Following CLAUDE.md guidance: autonomous work until task complete or context exhausted
- One commit per logical unit of work
- Strict TDD for all code
- Update this plan as implementation progresses
- Schema-first approach means documentation is code
