# Task 002.b: Implement Parser + Validator Requirements

**Priority:** 10 / 49  
**Tier:** Foundation  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 0 — Foundation  
**Dependencies:** Task 002.a (schema defined), Task 002 (config contract defined)  

---

## Description

### Overview

Task 002.b specifies the complete requirements for implementing the configuration parser and validator for the `.agent/config.yml` file. This task defines how Acode reads, parses, validates, and exposes configuration data to the rest of the system. The parser transforms raw YAML text into strongly-typed configuration objects, while the validator ensures all values conform to the schema defined in Task 002.a.

The parser and validator are foundational infrastructure—every other component in Acode depends on correctly parsed and validated configuration. A bug in the parser affects every feature. A gap in validation allows invalid configurations to cause runtime failures. This task must be implemented with extreme precision and comprehensive error handling.

### Business Value

A robust parser and validator provide:

1. **Fail-Fast Behavior** — Invalid configurations fail at startup with clear errors, not at runtime with cryptic failures
2. **Developer Productivity** — Clear error messages with line numbers and suggestions reduce debugging time
3. **Security Assurance** — Validation prevents injection attacks and malformed input from reaching business logic
4. **Configuration Portability** — Consistent parsing ensures configs work the same on all platforms
5. **Testability** — Strongly-typed configuration objects are easier to mock and test
6. **IDE Integration** — Validation logic can be shared with IDE plugins for real-time feedback

### Scope Boundaries

**In Scope:**
- YAML parsing requirements and error handling
- Schema validation using JSON Schema Draft 2020-12
- Strongly-typed configuration model classes
- Default value application
- Environment variable interpolation
- Error message formatting and localization
- Configuration caching and reload
- Cross-field validation rules
- Security validation (path traversal, size limits)
- Logging of configuration (with redaction)

**Out of Scope:**
- Schema definition (Task 002.a)
- Command execution (Task 002.c)
- Provider implementation (Epic 1)
- IDE plugin implementation
- Remote configuration fetching
- Configuration encryption

### Integration Points

| Task | Relationship | Description |
|------|--------------|-------------|
| Task 002 | Parent | Defines config structure |
| Task 002.a | Producer | Provides JSON Schema |
| Task 002.c | Consumer | Command config parsing |
| Task 001 | Consumer | Mode validation rules |
| All tasks | Consumer | All use parsed config |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| YAML parse error | Startup blocked | Helpful error with line number |
| Schema violation | Startup blocked | Clear violation message |
| Encoding error | Corrupted values | Enforce UTF-8, detect BOM |
| Circular reference | Stack overflow | Limit YAML anchor depth |
| Huge file | Memory exhaustion | Enforce size limit |
| Path traversal | Security breach | Validate all paths |

### Assumptions

1. YAML 1.2 parser is available (YamlDotNet or equivalent)
2. JSON Schema validator is available (NJsonSchema or equivalent)
3. Configuration is read from filesystem (not network)
4. Configuration encoding is UTF-8
5. Environment variables are available for interpolation

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **Parser** | Component that transforms YAML text to object graph |
| **Validator** | Component that checks objects against schema |
| **Deserializer** | Converts parsed YAML to typed objects |
| **Schema Validation** | Checking structure against JSON Schema |
| **Semantic Validation** | Checking business rules beyond schema |
| **Interpolation** | Replacing ${VAR} with environment values |
| **Default Application** | Setting default values for missing fields |
| **Redaction** | Removing sensitive data before logging |
| **Fail-Fast** | Detect errors as early as possible |
| **Configuration Model** | Strongly-typed classes representing config |
| **Parse Error** | Failure to interpret YAML syntax |
| **Validation Error** | Config violates schema or business rules |
| **Cross-Field Validation** | Rules involving multiple fields |
| **Anchor** | YAML feature for reusing nodes |
| **Alias** | YAML reference to an anchor |
| **BOM** | Byte Order Mark (encoding indicator) |

---

## Out of Scope

- JSON Schema definition and maintenance (Task 002.a)
- Command execution logic (Task 002.c)
- IDE plugin for real-time validation
- GraphQL schema generation
- OpenAPI spec generation
- Configuration migration between versions
- Remote configuration sources
- Configuration encryption at rest
- Configuration signing or integrity verification
- Multi-file configuration includes
- Configuration inheritance across files
- Watch mode for live config changes
- Configuration diff and merge tools

---

## Functional Requirements

### YAML Parsing (FR-002b-01 to FR-002b-25)

| ID | Requirement |
|----|-------------|
| FR-002b-01 | Parser MUST support YAML 1.2 specification |
| FR-002b-02 | Parser MUST handle UTF-8 encoding |
| FR-002b-03 | Parser MUST detect and handle UTF-8 BOM |
| FR-002b-04 | Parser MUST reject non-UTF-8 encodings with clear error |
| FR-002b-05 | Parser MUST support all YAML scalar types |
| FR-002b-06 | Parser MUST support YAML sequences (arrays) |
| FR-002b-07 | Parser MUST support YAML mappings (objects) |
| FR-002b-08 | Parser MUST support YAML comments |
| FR-002b-09 | Parser MUST support multi-line strings |
| FR-002b-10 | Parser MUST support YAML anchors with depth limit of 10 |
| FR-002b-11 | Parser MUST support YAML aliases with reference limit of 100 |
| FR-002b-12 | Parser MUST reject circular anchor references |
| FR-002b-13 | Parser MUST enforce maximum file size of 1MB |
| FR-002b-14 | Parser MUST enforce maximum nesting depth of 20 levels |
| FR-002b-15 | Parser MUST enforce maximum key count of 1000 |
| FR-002b-16 | Parser MUST return line number on parse error |
| FR-002b-17 | Parser MUST return column number on parse error |
| FR-002b-18 | Parser MUST return error context (surrounding lines) |
| FR-002b-19 | Parser MUST handle empty files gracefully |
| FR-002b-20 | Parser MUST handle whitespace-only files gracefully |
| FR-002b-21 | Parser MUST reject YAML with multiple documents |
| FR-002b-22 | Parser MUST reject YAML executable tags |
| FR-002b-23 | Parser MUST complete parsing in under 100ms |
| FR-002b-24 | Parser MUST NOT execute any code during parsing |
| FR-002b-25 | Parser MUST be deterministic (same input = same output) |

### Schema Validation (FR-002b-26 to FR-002b-50)

| ID | Requirement |
|----|-------------|
| FR-002b-26 | Validator MUST use JSON Schema Draft 2020-12 |
| FR-002b-27 | Validator MUST load schema from embedded resource |
| FR-002b-28 | Validator MUST cache compiled schema |
| FR-002b-29 | Validator MUST validate all required fields |
| FR-002b-30 | Validator MUST validate all field types |
| FR-002b-31 | Validator MUST validate enum constraints |
| FR-002b-32 | Validator MUST validate pattern constraints |
| FR-002b-33 | Validator MUST validate minimum/maximum constraints |
| FR-002b-34 | Validator MUST validate array item constraints |
| FR-002b-35 | Validator MUST validate nested object schemas |
| FR-002b-36 | Validator MUST report all violations (not just first) |
| FR-002b-37 | Validator MUST include field path in error (e.g., "model.parameters.temperature") |
| FR-002b-38 | Validator MUST include expected type in error |
| FR-002b-39 | Validator MUST include actual value in error (redacted if sensitive) |
| FR-002b-40 | Validator MUST include line number in error when available |
| FR-002b-41 | Validator MUST suggest corrections for common errors |
| FR-002b-42 | Validator MUST warn on unknown fields (not error) |
| FR-002b-43 | Validator MUST warn on deprecated fields |
| FR-002b-44 | Validator MUST complete validation in under 50ms |
| FR-002b-45 | Validator MUST be thread-safe |
| FR-002b-46 | Validator MUST support custom validation rules |
| FR-002b-47 | Validator MUST expose validation result as structured object |
| FR-002b-48 | Validator result MUST include error severity (error/warning) |
| FR-002b-49 | Validator result MUST include error code for programmatic handling |
| FR-002b-50 | Validator MUST NOT modify input during validation |

### Semantic Validation (FR-002b-51 to FR-002b-70)

| ID | Requirement |
|----|-------------|
| FR-002b-51 | Validator MUST check mode.default is not "burst" |
| FR-002b-52 | Validator MUST check airgapped_lock prevents mode override |
| FR-002b-53 | Validator MUST check model.endpoint is localhost in LocalOnly mode |
| FR-002b-54 | Validator MUST check model.provider is "ollama" or "lmstudio" in LocalOnly mode |
| FR-002b-55 | Validator MUST check paths do not escape repository root |
| FR-002b-56 | Validator MUST check paths do not include ".." traversal |
| FR-002b-57 | Validator MUST check command strings for shell injection patterns |
| FR-002b-58 | Validator MUST check network.allowlist only in Burst mode |
| FR-002b-59 | Validator MUST check project.type matches project.languages |
| FR-002b-60 | Validator MUST check schema_version is supported |
| FR-002b-61 | Validator MUST check no duplicate entries in arrays |
| FR-002b-62 | Validator MUST check ignore patterns are valid globs |
| FR-002b-63 | Validator MUST check path patterns are valid globs |
| FR-002b-64 | Validator MUST check temperature is within valid range (0.0-2.0) |
| FR-002b-65 | Validator MUST check max_tokens is positive integer |
| FR-002b-66 | Validator MUST check timeout_seconds is positive integer |
| FR-002b-67 | Validator MUST check retry_count is non-negative integer |
| FR-002b-68 | Validator MUST check endpoint URL format when specified |
| FR-002b-69 | Validator MUST check all referenced files exist (paths.source, etc.) |
| FR-002b-70 | Validator MUST aggregate all semantic errors before returning |

### Configuration Model (FR-002b-71 to FR-002b-90)

| ID | Requirement |
|----|-------------|
| FR-002b-71 | Model MUST be immutable after construction |
| FR-002b-72 | Model MUST use strongly-typed properties |
| FR-002b-73 | Model MUST use nullable reference types |
| FR-002b-74 | Model MUST have AcodeConfig as root class |
| FR-002b-75 | Model MUST have ProjectConfig nested class |
| FR-002b-76 | Model MUST have ModeConfig nested class |
| FR-002b-77 | Model MUST have ModelConfig nested class |
| FR-002b-78 | Model MUST have ModelParametersConfig nested class |
| FR-002b-79 | Model MUST have CommandsConfig nested class |
| FR-002b-80 | Model MUST have PathsConfig nested class |
| FR-002b-81 | Model MUST have IgnoreConfig nested class |
| FR-002b-82 | Model MUST have NetworkConfig nested class |
| FR-002b-83 | Model MUST implement IEquatable for testing |
| FR-002b-84 | Model MUST override ToString for debugging |
| FR-002b-85 | Model MUST be serializable to JSON for logging |
| FR-002b-86 | Model MUST support deep clone operation |
| FR-002b-87 | Model MUST expose default values as static properties |
| FR-002b-88 | Model MUST document all properties with XML comments |
| FR-002b-89 | Model MUST be in Domain layer (no infrastructure dependencies) |
| FR-002b-90 | Model MUST NOT contain parsing or validation logic |

### Default Value Application (FR-002b-91 to FR-002b-105)

| ID | Requirement |
|----|-------------|
| FR-002b-91 | Defaults MUST be applied after parsing, before validation |
| FR-002b-92 | Defaults MUST NOT override explicit values |
| FR-002b-93 | Defaults MUST be defined in single location |
| FR-002b-94 | Default schema_version MUST be "1.0.0" |
| FR-002b-95 | Default mode.default MUST be "local-only" |
| FR-002b-96 | Default mode.allow_burst MUST be true |
| FR-002b-97 | Default mode.airgapped_lock MUST be false |
| FR-002b-98 | Default model.provider MUST be "ollama" |
| FR-002b-99 | Default model.name MUST be "codellama:7b" |
| FR-002b-100 | Default model.endpoint MUST be "http://localhost:11434" |
| FR-002b-101 | Default model.parameters.temperature MUST be 0.7 |
| FR-002b-102 | Default model.parameters.max_tokens MUST be 4096 |
| FR-002b-103 | Default model.timeout_seconds MUST be 120 |
| FR-002b-104 | Default model.retry_count MUST be 3 |
| FR-002b-105 | Defaults MUST be documented in schema and code |

### Environment Variable Interpolation (FR-002b-106 to FR-002b-120)

| ID | Requirement |
|----|-------------|
| FR-002b-106 | Interpolation MUST support ${VAR} syntax |
| FR-002b-107 | Interpolation MUST support ${VAR:-default} syntax |
| FR-002b-108 | Interpolation MUST support ${VAR:?error} syntax |
| FR-002b-109 | Interpolation MUST occur after parsing, before validation |
| FR-002b-110 | Undefined variable without default MUST cause error |
| FR-002b-111 | Interpolation MUST NOT be recursive |
| FR-002b-112 | Interpolation MUST have maximum replacement count of 100 |
| FR-002b-113 | Interpolation MUST preserve type (not stringify everything) |
| FR-002b-114 | Interpolation MUST work in string values only |
| FR-002b-115 | Interpolation MUST escape $$ to literal $ |
| FR-002b-116 | Interpolation MUST log variable names used (not values) |
| FR-002b-117 | Interpolation MUST NOT log sensitive variable values |
| FR-002b-118 | Interpolation errors MUST include variable name |
| FR-002b-119 | Interpolation MUST support nested paths (${ACODE_MODEL_NAME}) |
| FR-002b-120 | Interpolation MUST be case-sensitive for variable names |

---

## Non-Functional Requirements

### Security (NFR-002b-01 to NFR-002b-15)

| ID | Requirement |
|----|-------------|
| NFR-002b-01 | Parser MUST NOT execute YAML tags or code |
| NFR-002b-02 | Parser MUST use safe YAML loading mode |
| NFR-002b-03 | Parser MUST reject potentially dangerous constructs |
| NFR-002b-04 | Validator MUST reject path traversal attempts |
| NFR-002b-05 | Validator MUST reject shell injection patterns |
| NFR-002b-06 | Config logging MUST redact sensitive fields |
| NFR-002b-07 | Sensitive fields: api_key, token, password, secret |
| NFR-002b-08 | Redacted format MUST be "[REDACTED:field_name]" |
| NFR-002b-09 | Error messages MUST NOT include sensitive values |
| NFR-002b-10 | Interpolation MUST NOT expand in unsafe contexts |
| NFR-002b-11 | File permissions SHOULD be validated (warn if world-readable) |
| NFR-002b-12 | Parser MUST handle malicious input without crash |
| NFR-002b-13 | Memory allocation MUST be bounded |
| NFR-002b-14 | CPU usage MUST be bounded (timeout) |
| NFR-002b-15 | Stack depth MUST be bounded |

### Performance (NFR-002b-16 to NFR-002b-28)

| ID | Requirement |
|----|-------------|
| NFR-002b-16 | Config load MUST complete in under 50ms |
| NFR-002b-17 | Config validation MUST complete in under 50ms |
| NFR-002b-18 | Total parse+validate MUST complete in under 100ms |
| NFR-002b-19 | Schema compilation MUST be cached |
| NFR-002b-20 | Parsed config MUST be cached |
| NFR-002b-21 | Cache invalidation MUST be explicit |
| NFR-002b-22 | Memory usage MUST be under 5MB for parsing |
| NFR-002b-23 | Memory usage MUST be under 1MB for config object |
| NFR-002b-24 | Parser MUST NOT load entire file into memory for streaming |
| NFR-002b-25 | Validator MUST use compiled schema (not re-parse) |
| NFR-002b-26 | Config access MUST be O(1) after load |
| NFR-002b-27 | Repeated validation calls MUST be idempotent |
| NFR-002b-28 | Hot path MUST avoid allocations |

### Reliability (NFR-002b-29 to NFR-002b-40)

| ID | Requirement |
|----|-------------|
| NFR-002b-29 | Parser MUST NOT crash on any input |
| NFR-002b-30 | Parser MUST return structured error on failure |
| NFR-002b-31 | Validator MUST NOT crash on any input |
| NFR-002b-32 | Validator MUST return structured error on failure |
| NFR-002b-33 | Missing config file MUST return MissingConfigError |
| NFR-002b-34 | Empty config file MUST apply defaults |
| NFR-002b-35 | Invalid YAML MUST return YamlParseError |
| NFR-002b-36 | Invalid schema MUST return SchemaValidationError |
| NFR-002b-37 | Invalid semantics MUST return SemanticValidationError |
| NFR-002b-38 | All errors MUST include actionable message |
| NFR-002b-39 | Concurrent config reads MUST be safe |
| NFR-002b-40 | Config reload during operation MUST NOT corrupt state |

### Maintainability (NFR-002b-41 to NFR-002b-50)

| ID | Requirement |
|----|-------------|
| NFR-002b-41 | Parser code MUST have > 90% test coverage |
| NFR-002b-42 | Validator code MUST have > 90% test coverage |
| NFR-002b-43 | All error codes MUST be documented |
| NFR-002b-44 | All default values MUST be documented |
| NFR-002b-45 | Configuration model MUST be documented |
| NFR-002b-46 | Code MUST follow project style guidelines |
| NFR-002b-47 | Public APIs MUST have XML documentation |
| NFR-002b-48 | Complex logic MUST have inline comments |
| NFR-002b-49 | Parser and validator MUST be unit-testable in isolation |
| NFR-002b-50 | Dependencies MUST be injected (DI-friendly) |

---

## User Manual Documentation

### Configuration Loading Overview

Acode loads configuration from `.agent/config.yml` in your repository root. The loading process follows these steps:

1. **Locate** — Find `.agent/config.yml` relative to repository root
2. **Read** — Read file contents with UTF-8 encoding
3. **Parse** — Parse YAML into object structure
4. **Interpolate** — Replace environment variable references
5. **Default** — Apply default values for missing fields
6. **Validate** — Validate against JSON Schema and semantic rules
7. **Construct** — Build strongly-typed configuration object
8. **Cache** — Store configuration for repeated access

### Error Message Format

Configuration errors follow a consistent format:

```
ACODE-CFG-{CODE}: {message}
  at {file}:{line}:{column}
  field: {field_path}
  expected: {expected_type_or_value}
  actual: {actual_value}
  
Suggestion: {fix_suggestion}
```

### Error Codes Reference

| Code | Name | Description |
|------|------|-------------|
| ACODE-CFG-001 | FileNotFound | Config file does not exist |
| ACODE-CFG-002 | FileReadError | Cannot read config file |
| ACODE-CFG-003 | EncodingError | File is not valid UTF-8 |
| ACODE-CFG-004 | YamlSyntaxError | Invalid YAML syntax |
| ACODE-CFG-005 | YamlStructureError | YAML structure not allowed |
| ACODE-CFG-006 | FileTooLarge | Config exceeds 1MB limit |
| ACODE-CFG-007 | NestingTooDeep | YAML nesting exceeds 20 levels |
| ACODE-CFG-008 | TooManyKeys | Config exceeds 1000 keys |
| ACODE-CFG-009 | CircularReference | YAML anchor creates cycle |
| ACODE-CFG-010 | RequiredFieldMissing | Required field not present |
| ACODE-CFG-011 | TypeMismatch | Field has wrong type |
| ACODE-CFG-012 | EnumViolation | Value not in allowed set |
| ACODE-CFG-013 | PatternViolation | Value doesn't match pattern |
| ACODE-CFG-014 | RangeViolation | Value outside allowed range |
| ACODE-CFG-015 | UnknownField | Field not in schema (warning) |
| ACODE-CFG-016 | DeprecatedField | Field is deprecated (warning) |
| ACODE-CFG-017 | EnvVarMissing | Environment variable not set |
| ACODE-CFG-018 | EnvVarError | Environment variable syntax error |
| ACODE-CFG-019 | PathTraversal | Path attempts directory escape |
| ACODE-CFG-020 | InvalidGlob | Glob pattern is malformed |
| ACODE-CFG-021 | ModeViolation | Mode configuration conflict |
| ACODE-CFG-022 | ProviderViolation | Provider not allowed in mode |
| ACODE-CFG-023 | SchemaVersionUnsupported | Schema version not recognized |
| ACODE-CFG-024 | SemanticViolation | Cross-field validation failed |
| ACODE-CFG-025 | SecurityViolation | Potentially dangerous config |

### Environment Variable Interpolation

Use environment variables in configuration:

```yaml
model:
  endpoint: ${OLLAMA_HOST:-http://localhost:11434}
  name: ${ACODE_MODEL:-codellama:7b}
```

#### Syntax Reference

| Syntax | Behavior |
|--------|----------|
| `${VAR}` | Replace with VAR value, error if undefined |
| `${VAR:-default}` | Replace with VAR value, use "default" if undefined |
| `${VAR:?error message}` | Replace with VAR value, error with message if undefined |
| `$$` | Escape to literal `$` |

#### Best Practices

1. **Use defaults for optional values**: `${OPTIONAL_VAR:-default_value}`
2. **Use error syntax for required values**: `${REQUIRED_VAR:?Must set REQUIRED_VAR}`
3. **Prefix Acode variables**: Use `ACODE_` prefix for clarity
4. **Document variables**: List required env vars in README

### Validation Levels

Acode validates configuration at three levels:

#### Level 1: Structural Validation
- YAML syntax is correct
- File encoding is UTF-8
- Size and nesting limits respected

#### Level 2: Schema Validation
- All required fields present
- All field types correct
- All constraints satisfied (enums, patterns, ranges)

#### Level 3: Semantic Validation
- Mode rules enforced (local-only constraints)
- Path safety verified (no traversal)
- Cross-field consistency checked

### Programmatic Access

```csharp
// Load configuration
var config = await configLoader.LoadAsync(repoRoot);

// Access typed properties
Console.WriteLine(config.Project.Name);
Console.WriteLine(config.Mode.Default);
Console.WriteLine(config.Model.Provider);

// Check validation result
var result = await validator.ValidateAsync(configPath);
if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.Code}: {error.Message}");
    }
}
```

### Troubleshooting

#### "Config file not found"

```bash
# Create minimal config
mkdir .agent
echo "schema_version: \"1.0.0\"" > .agent/config.yml
```

#### "YAML syntax error"

```bash
# Validate YAML syntax
acode config validate --syntax-only
```

#### "Unknown field warning"

Unknown fields are warned but not rejected. Check for typos:

```yaml
# Wrong
mdoel:          # Typo!
  provider: ollama

# Correct  
model:
  provider: ollama
```

#### "Environment variable not found"

```bash
# Set required variable
export ACODE_MODEL=codellama:7b

# Or use default syntax in config
model:
  name: ${ACODE_MODEL:-codellama:7b}
```

### Configuration Caching

Acode caches parsed configuration for performance. To force reload:

```bash
acode config reload
```

Cache is automatically invalidated when:
- Config file modification time changes
- `acode config reload` is run
- A new session starts

---

## Acceptance Criteria / Definition of Done

### YAML Parsing (30 items)

- [ ] Parser supports YAML 1.2 specification
- [ ] Parser handles UTF-8 encoding correctly
- [ ] Parser detects and handles UTF-8 BOM
- [ ] Parser rejects non-UTF-8 with clear error
- [ ] Parser supports all scalar types (string, int, float, bool, null)
- [ ] Parser supports sequences (arrays)
- [ ] Parser supports mappings (objects)
- [ ] Parser preserves comments (for error reporting)
- [ ] Parser supports multi-line strings (literal, folded)
- [ ] Parser supports anchors with depth limit
- [ ] Parser supports aliases with reference limit
- [ ] Parser rejects circular references
- [ ] Parser enforces 1MB file size limit
- [ ] Parser enforces 20-level nesting limit
- [ ] Parser enforces 1000-key limit
- [ ] Parse errors include line number
- [ ] Parse errors include column number
- [ ] Parse errors include context lines
- [ ] Empty files produce empty config (use defaults)
- [ ] Whitespace-only files produce empty config
- [ ] Multiple YAML documents rejected
- [ ] Executable YAML tags rejected
- [ ] Parsing completes in under 100ms
- [ ] No code execution during parsing
- [ ] Parsing is deterministic
- [ ] Parser is thread-safe
- [ ] Parser handles all edge cases without crash
- [ ] Parser memory usage under 5MB
- [ ] Parser has >90% test coverage
- [ ] Parser error messages are actionable

### Schema Validation (35 items)

- [ ] Validator uses JSON Schema Draft 2020-12
- [ ] Schema loaded from embedded resource
- [ ] Compiled schema is cached
- [ ] All required fields validated
- [ ] All field types validated
- [ ] Enum constraints validated
- [ ] Pattern constraints validated
- [ ] Min/max constraints validated
- [ ] Array item constraints validated
- [ ] Nested object schemas validated
- [ ] All violations reported (not just first)
- [ ] Field path included in errors
- [ ] Expected type included in errors
- [ ] Actual value included in errors (redacted)
- [ ] Line number included when available
- [ ] Suggestions provided for common errors
- [ ] Unknown fields generate warnings
- [ ] Deprecated fields generate warnings
- [ ] Validation completes in under 50ms
- [ ] Validator is thread-safe
- [ ] Custom validation rules supported
- [ ] Validation result is structured object
- [ ] Error severity included (error/warning)
- [ ] Error code included for programmatic use
- [ ] Input not modified during validation
- [ ] Validator handles all edge cases
- [ ] Validator memory usage under 1MB
- [ ] Validator has >90% test coverage
- [ ] Validator error messages are actionable
- [ ] All 25 error codes implemented
- [ ] Error codes documented
- [ ] Validation result serializable
- [ ] Warnings can be promoted to errors (strict mode)
- [ ] Multiple validation modes (lenient, normal, strict)
- [ ] Validation bypassed for performance in tests (explicit)

### Semantic Validation (25 items)

- [ ] mode.default cannot be "burst"
- [ ] airgapped_lock prevents mode override
- [ ] model.endpoint must be localhost in LocalOnly
- [ ] model.provider must be local in LocalOnly
- [ ] Paths cannot escape repository root
- [ ] Paths cannot include ".." traversal
- [ ] Command strings checked for injection
- [ ] network.allowlist only valid in Burst mode
- [ ] project.type matches project.languages
- [ ] schema_version is supported version
- [ ] No duplicate entries in arrays
- [ ] Ignore patterns are valid globs
- [ ] Path patterns are valid globs
- [ ] Temperature is 0.0-2.0
- [ ] max_tokens is positive
- [ ] timeout_seconds is positive
- [ ] retry_count is non-negative
- [ ] Endpoint URL format valid
- [ ] Referenced paths exist (warning if not)
- [ ] All semantic errors aggregated
- [ ] Semantic validation runs after schema validation
- [ ] Semantic rules are extensible
- [ ] Semantic rules are documented
- [ ] Semantic rules have test coverage
- [ ] Semantic rules report clear errors

### Configuration Model (25 items)

- [ ] AcodeConfig root class exists
- [ ] ProjectConfig class exists
- [ ] ModeConfig class exists
- [ ] ModelConfig class exists
- [ ] ModelParametersConfig class exists
- [ ] CommandsConfig class exists
- [ ] PathsConfig class exists
- [ ] IgnoreConfig class exists
- [ ] NetworkConfig class exists
- [ ] All classes are immutable
- [ ] All properties are strongly-typed
- [ ] Nullable reference types used correctly
- [ ] IEquatable implemented on all classes
- [ ] ToString overridden for debugging
- [ ] Serializable to JSON for logging
- [ ] Deep clone operation supported
- [ ] Default values exposed as static properties
- [ ] XML documentation on all public members
- [ ] Classes in Domain layer
- [ ] No parsing/validation logic in models
- [ ] Models follow project naming conventions
- [ ] Models have unit test coverage
- [ ] Models have equality tests
- [ ] Models have serialization tests
- [ ] Models are readonly records or classes

### Default Values (20 items)

- [ ] Defaults applied after parsing
- [ ] Defaults applied before validation
- [ ] Explicit values not overridden
- [ ] Defaults defined in single location
- [ ] schema_version defaults to "1.0.0"
- [ ] mode.default defaults to "local-only"
- [ ] mode.allow_burst defaults to true
- [ ] mode.airgapped_lock defaults to false
- [ ] model.provider defaults to "ollama"
- [ ] model.name defaults to "codellama:7b"
- [ ] model.endpoint defaults to "http://localhost:11434"
- [ ] model.parameters.temperature defaults to 0.7
- [ ] model.parameters.max_tokens defaults to 4096
- [ ] model.timeout_seconds defaults to 120
- [ ] model.retry_count defaults to 3
- [ ] Defaults documented in schema
- [ ] Defaults documented in code
- [ ] Defaults have unit tests
- [ ] Default constants are public
- [ ] Defaults match schema specification

### Environment Variable Interpolation (20 items)

- [ ] ${VAR} syntax supported
- [ ] ${VAR:-default} syntax supported
- [ ] ${VAR:?error} syntax supported
- [ ] Interpolation after parsing
- [ ] Interpolation before validation
- [ ] Undefined variable causes error
- [ ] Recursion prevented
- [ ] Maximum 100 replacements
- [ ] Type preservation (numbers stay numbers)
- [ ] String values only
- [ ] $$ escapes to $
- [ ] Variable names logged (not values)
- [ ] Sensitive values not logged
- [ ] Errors include variable name
- [ ] Nested variable names supported
- [ ] Case-sensitive matching
- [ ] Interpolation has unit tests
- [ ] Edge cases covered (empty, special chars)
- [ ] Performance under 10ms for interpolation
- [ ] Interpolation errors are actionable

### Error Handling (20 items)

- [ ] All error codes documented
- [ ] Error code format: ACODE-CFG-NNN
- [ ] Error includes message
- [ ] Error includes file path
- [ ] Error includes line number
- [ ] Error includes column number
- [ ] Error includes field path
- [ ] Error includes expected value
- [ ] Error includes actual value
- [ ] Error includes suggestion
- [ ] Errors serializable to JSON
- [ ] Errors loggable with structured data
- [ ] Exit code 1 on validation failure
- [ ] Exit code 2 on parse failure
- [ ] Exit code 3 on file not found
- [ ] Multiple errors batched
- [ ] Warnings distinguishable from errors
- [ ] Error output to stderr
- [ ] Error format suitable for IDE parsing
- [ ] Error recovery where possible

### Security (15 items)

- [ ] No code execution during parsing
- [ ] Safe YAML loading mode used
- [ ] Dangerous constructs rejected
- [ ] Path traversal prevented
- [ ] Shell injection patterns detected
- [ ] Sensitive fields redacted in logs
- [ ] api_key, token, password, secret redacted
- [ ] Redaction format: [REDACTED:field_name]
- [ ] Sensitive values excluded from errors
- [ ] Unsafe interpolation prevented
- [ ] File permissions warned if world-readable
- [ ] Malicious input handled without crash
- [ ] Memory bounded
- [ ] CPU bounded (timeout)
- [ ] Stack depth bounded

### Performance (15 items)

- [ ] Config load under 50ms
- [ ] Validation under 50ms
- [ ] Total parse+validate under 100ms
- [ ] Schema compiled once and cached
- [ ] Parsed config cached
- [ ] Cache invalidation explicit
- [ ] Memory under 5MB for parsing
- [ ] Memory under 1MB for config object
- [ ] Streaming parse (not load entire file)
- [ ] Compiled schema used
- [ ] O(1) config access after load
- [ ] Idempotent validation
- [ ] Hot path avoids allocations
- [ ] Benchmarks documented
- [ ] Performance regression tests

---
## Testing Requirements

### Unit Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| UT-002b-01 | Parse valid minimal YAML | Returns parsed object |
| UT-002b-02 | Parse valid full YAML | Returns parsed object with all fields |
| UT-002b-03 | Parse YAML with comments | Comments ignored, data preserved |
| UT-002b-04 | Parse YAML with anchors | Anchors resolved correctly |
| UT-002b-05 | Parse YAML with aliases | Aliases resolved correctly |
| UT-002b-06 | Parse empty file | Returns empty object |
| UT-002b-07 | Parse whitespace-only file | Returns empty object |
| UT-002b-08 | Reject invalid YAML syntax | Returns YamlSyntaxError |
| UT-002b-09 | Reject file over 1MB | Returns FileTooLarge error |
| UT-002b-10 | Reject nesting over 20 levels | Returns NestingTooDeep error |
| UT-002b-11 | Reject over 1000 keys | Returns TooManyKeys error |
| UT-002b-12 | Reject circular anchor | Returns CircularReference error |
| UT-002b-13 | Reject multiple documents | Returns error |
| UT-002b-14 | Handle UTF-8 BOM | Parses correctly |
| UT-002b-15 | Reject non-UTF-8 | Returns EncodingError |
| UT-002b-16 | Validate required field present | Passes validation |
| UT-002b-17 | Validate required field missing | Returns RequiredFieldMissing |
| UT-002b-18 | Validate correct type | Passes validation |
| UT-002b-19 | Validate incorrect type | Returns TypeMismatch |
| UT-002b-20 | Validate valid enum value | Passes validation |
| UT-002b-21 | Validate invalid enum value | Returns EnumViolation |
| UT-002b-22 | Validate pattern match | Passes validation |
| UT-002b-23 | Validate pattern mismatch | Returns PatternViolation |
| UT-002b-24 | Validate value in range | Passes validation |
| UT-002b-25 | Validate value out of range | Returns RangeViolation |
| UT-002b-26 | Warn on unknown field | Returns warning, not error |
| UT-002b-27 | Warn on deprecated field | Returns warning with migration hint |
| UT-002b-28 | Interpolate ${VAR} | Value replaced |
| UT-002b-29 | Interpolate ${VAR:-default} | Default used when undefined |
| UT-002b-30 | Interpolate ${VAR:?error} | Error returned when undefined |
| UT-002b-31 | Escape $$ to $ | Literal $ in output |
| UT-002b-32 | Reject undefined variable | Returns EnvVarMissing |
| UT-002b-33 | Apply default values | Missing fields have defaults |
| UT-002b-34 | Preserve explicit values | Defaults don't override |
| UT-002b-35 | Semantic: reject burst as default mode | Returns ModeViolation |
| UT-002b-36 | Semantic: reject path traversal | Returns PathTraversal |
| UT-002b-37 | Semantic: reject external provider in LocalOnly | Returns ProviderViolation |
| UT-002b-38 | Config model immutability | Properties cannot be changed |
| UT-002b-39 | Config model equality | Equal configs are equal |
| UT-002b-40 | Config model serialization | Serializes to JSON correctly |

### Integration Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| IT-002b-01 | Load config from filesystem | Config loaded and parsed |
| IT-002b-02 | Load config with env vars from environment | Variables interpolated |
| IT-002b-03 | Load config in LocalOnly mode | Local constraints enforced |
| IT-002b-04 | Load config in Airgapped mode | Airgapped constraints enforced |
| IT-002b-05 | Reload config after file change | New config loaded |
| IT-002b-06 | Cache invalidation on reload | Old config discarded |
| IT-002b-07 | Concurrent config loads | Thread-safe operation |
| IT-002b-08 | Config loading with DI container | Dependencies injected |
| IT-002b-09 | Config validation end-to-end | All stages complete |
| IT-002b-10 | Error reporting with real file | Line numbers correct |
| IT-002b-11 | Load .NET project config | All .NET fields parsed |
| IT-002b-12 | Load Node.js project config | All Node fields parsed |
| IT-002b-13 | Load Python project config | All Python fields parsed |
| IT-002b-14 | Load config with all optional fields | All fields populated |
| IT-002b-15 | Load config with minimal fields | Defaults applied |

### End-to-End Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| E2E-002b-01 | acode config validate (valid) | Exit code 0, "Configuration valid" |
| E2E-002b-02 | acode config validate (invalid) | Exit code 1, errors displayed |
| E2E-002b-03 | acode config validate (parse error) | Exit code 2, parse error shown |
| E2E-002b-04 | acode config validate (file missing) | Exit code 3, helpful message |
| E2E-002b-05 | acode config show | Displays parsed config |
| E2E-002b-06 | acode config show --json | Outputs valid JSON |
| E2E-002b-07 | acode config show (with secrets) | Secrets redacted |
| E2E-002b-08 | acode config reload | Cache invalidated, success message |
| E2E-002b-09 | acode config validate --strict | Warnings treated as errors |
| E2E-002b-10 | acode config init | Creates minimal config |
| E2E-002b-11 | Config error in IDE-parseable format | Line:column in output |
| E2E-002b-12 | Multiple errors displayed | All errors shown |

### Performance / Benchmarks

| ID | Benchmark | Target | Measurement Method |
|----|-----------|--------|-------------------|
| PERF-002b-01 | Parse minimal config | < 10ms | Stopwatch, 1000 iterations |
| PERF-002b-02 | Parse full config | < 30ms | Stopwatch, 1000 iterations |
| PERF-002b-03 | Validate minimal config | < 10ms | Stopwatch, 1000 iterations |
| PERF-002b-04 | Validate full config | < 30ms | Stopwatch, 1000 iterations |
| PERF-002b-05 | Total load (parse+validate) | < 100ms | Stopwatch, 1000 iterations |
| PERF-002b-06 | Cached config access | < 1ms | Stopwatch, 10000 iterations |
| PERF-002b-07 | Memory: parse 1MB file | < 5MB peak | Memory profiler |
| PERF-002b-08 | Memory: config object | < 100KB | Object size calculation |
| PERF-002b-09 | Interpolation (100 vars) | < 10ms | Stopwatch, 1000 iterations |
| PERF-002b-10 | Schema compilation | < 500ms | First-load measurement |

### Regression / Impacted Areas

| Area | Impact | Regression Test |
|------|--------|-----------------|
| All commands | Config loading | Commands work with valid config |
| Mode enforcement | Mode parsing | Modes enforced correctly |
| Provider selection | Model config | Correct provider used |
| Command execution | Commands config | Commands executed |
| Path handling | Paths config | Paths resolved correctly |
| Ignore patterns | Ignore config | Patterns applied |
| Error messages | Error formatting | Messages unchanged |
| Exit codes | Error handling | Codes consistent |
| Logging | Config logging | Redaction works |
| CLI output | Config display | Format unchanged |

---
## User Verification Steps

### Scenario 1: Valid Configuration
1. Create `.agent/config.yml` with valid minimal config
2. Run `acode config validate`
3. **Verify:** Exit code is 0
4. **Verify:** Output shows "Configuration valid"

### Scenario 2: Invalid YAML Syntax
1. Create `.agent/config.yml` with invalid YAML (missing colon)
2. Run `acode config validate`
3. **Verify:** Exit code is 2
4. **Verify:** Error includes line number and column
5. **Verify:** Error includes suggestion to fix

### Scenario 3: Schema Violation
1. Create config with `mode: { default: "burst" }`
2. Run `acode config validate`
3. **Verify:** Exit code is 1
4. **Verify:** Error code is ACODE-CFG-021 (ModeViolation)
5. **Verify:** Error explains burst cannot be default

### Scenario 4: Environment Variable Interpolation
1. Set `export ACODE_MODEL=deepseek-coder:6.7b`
2. Create config with `model: { name: "${ACODE_MODEL}" }`
3. Run `acode config show`
4. **Verify:** Model name shows "deepseek-coder:6.7b"

### Scenario 5: Missing Environment Variable
1. Unset `ACODE_MODEL`
2. Create config with `model: { name: "${ACODE_MODEL}" }`
3. Run `acode config validate`
4. **Verify:** Exit code is 1
5. **Verify:** Error code is ACODE-CFG-017 (EnvVarMissing)

### Scenario 6: Environment Variable Default
1. Unset `ACODE_MODEL`
2. Create config with `model: { name: "${ACODE_MODEL:-codellama:7b}" }`
3. Run `acode config show`
4. **Verify:** Model name shows "codellama:7b"

### Scenario 7: Path Traversal Attempt
1. Create config with `paths: { source: ["../outside-repo"] }`
2. Run `acode config validate`
3. **Verify:** Exit code is 1
4. **Verify:** Error code is ACODE-CFG-019 (PathTraversal)

### Scenario 8: Unknown Field Warning
1. Create config with `unknown_field: "value"`
2. Run `acode config validate`
3. **Verify:** Exit code is 0 (warning, not error)
4. **Verify:** Warning mentions unknown field

### Scenario 9: Strict Mode Validation
1. Create config with unknown field
2. Run `acode config validate --strict`
3. **Verify:** Exit code is 1 (warning promoted to error)

### Scenario 10: Config Show with Redaction
1. Create config (even without secrets)
2. Set `ACODE_API_KEY=secret123` in environment
3. Create config referencing `${ACODE_API_KEY}`
4. Run `acode config show`
5. **Verify:** API key shows as "[REDACTED:api_key]"

### Scenario 11: Config File Not Found
1. Ensure `.agent/config.yml` does not exist
2. Run `acode config validate`
3. **Verify:** Exit code is 3
4. **Verify:** Helpful message about creating config

### Scenario 12: Large File Rejection
1. Create `.agent/config.yml` over 1MB
2. Run `acode config validate`
3. **Verify:** Exit code is 2
4. **Verify:** Error code is ACODE-CFG-006 (FileTooLarge)

### Scenario 13: Multiple Errors Reported
1. Create config with multiple violations
2. Run `acode config validate`
3. **Verify:** All errors listed, not just first

### Scenario 14: Config Reload
1. Start acode in watch mode
2. Modify `.agent/config.yml`
3. Run `acode config reload`
4. **Verify:** New config values active

### Scenario 15: Default Values Applied
1. Create minimal config (only schema_version)
2. Run `acode config show --json`
3. **Verify:** All default values present

### Scenario 16: LocalOnly Mode Constraints
1. Create config with `mode: { default: "local-only" }` and `model: { provider: "openai" }`
2. Run `acode config validate`
3. **Verify:** Exit code is 1
4. **Verify:** Error explains local mode requires local provider

### Scenario 17: IDE-Parseable Error Format
1. Create config with error on line 10
2. Run `acode config validate`
3. **Verify:** Output contains "config.yml:10:" for IDE parsing

### Scenario 18: JSON Output Mode
1. Create valid config
2. Run `acode config show --json`
3. **Verify:** Output is valid JSON
4. **Verify:** JSON can be piped to `jq`

---

## Implementation Prompt for Claude

### Objective

Implement the configuration parser and validator for Acode's `.agent/config.yml` file. This implementation provides the foundation for all configuration handling in the system.

### Architecture Constraints

- **Clean Architecture:** Domain models in Domain layer, parsing/validation in Application layer, file I/O in Infrastructure layer
- **Dependency Injection:** All dependencies injected via constructor
- **Immutability:** Configuration models are immutable after construction
- **Testability:** All components testable in isolation

### File Structure

```
src/
├── Acode.Domain/
│   └── Configuration/
│       ├── AcodeConfig.cs
│       ├── ProjectConfig.cs
│       ├── ModeConfig.cs
│       ├── ModelConfig.cs
│       ├── ModelParametersConfig.cs
│       ├── CommandsConfig.cs
│       ├── PathsConfig.cs
│       ├── IgnoreConfig.cs
│       ├── NetworkConfig.cs
│       └── ConfigDefaults.cs
├── Acode.Application/
│   └── Configuration/
│       ├── IConfigLoader.cs
│       ├── IConfigValidator.cs
│       ├── IConfigCache.cs
│       ├── ConfigLoader.cs
│       ├── ConfigValidator.cs
│       ├── ConfigCache.cs
│       ├── EnvironmentInterpolator.cs
│       ├── DefaultValueApplicator.cs
│       ├── SemanticValidator.cs
│       ├── ValidationResult.cs
│       ├── ValidationError.cs
│       ├── ValidationSeverity.cs
│       └── ConfigErrorCodes.cs
├── Acode.Infrastructure/
│   └── Configuration/
│       ├── YamlConfigReader.cs
│       ├── JsonSchemaValidator.cs
│       └── FileSystemConfigProvider.cs
└── Acode.Cli/
    └── Commands/
        └── ConfigCommands.cs
```

### Interface Contracts

```csharp
// IConfigLoader.cs
public interface IConfigLoader
{
    Task<AcodeConfig> LoadAsync(string repositoryRoot, CancellationToken ct = default);
    Task<AcodeConfig> LoadAsync(string repositoryRoot, ConfigLoadOptions options, CancellationToken ct = default);
}

// IConfigValidator.cs
public interface IConfigValidator
{
    Task<ValidationResult> ValidateAsync(string configPath, CancellationToken ct = default);
    Task<ValidationResult> ValidateAsync(string configPath, ValidationOptions options, CancellationToken ct = default);
    ValidationResult Validate(AcodeConfig config);
}

// IConfigCache.cs
public interface IConfigCache
{
    bool TryGet(string repositoryRoot, out AcodeConfig? config);
    void Set(string repositoryRoot, AcodeConfig config);
    void Invalidate(string repositoryRoot);
    void InvalidateAll();
}
```

### Domain Models

```csharp
// AcodeConfig.cs
public sealed record AcodeConfig
{
    public required string SchemaVersion { get; init; }
    public required ProjectConfig Project { get; init; }
    public required ModeConfig Mode { get; init; }
    public required ModelConfig Model { get; init; }
    public required CommandsConfig Commands { get; init; }
    public required PathsConfig Paths { get; init; }
    public required IgnoreConfig Ignore { get; init; }
    public NetworkConfig? Network { get; init; }
}

// ConfigDefaults.cs
public static class ConfigDefaults
{
    public const string SchemaVersion = "1.0.0";
    public const string DefaultMode = "local-only";
    public const bool AllowBurst = true;
    public const bool AirgappedLock = false;
    public const string DefaultProvider = "ollama";
    public const string DefaultModel = "codellama:7b";
    public const string DefaultEndpoint = "http://localhost:11434";
    public const double DefaultTemperature = 0.7;
    public const int DefaultMaxTokens = 4096;
    public const int DefaultTimeoutSeconds = 120;
    public const int DefaultRetryCount = 3;
}
```

### Error Codes

```csharp
// ConfigErrorCodes.cs
public static class ConfigErrorCodes
{
    public const string FileNotFound = "ACODE-CFG-001";
    public const string FileReadError = "ACODE-CFG-002";
    public const string EncodingError = "ACODE-CFG-003";
    public const string YamlSyntaxError = "ACODE-CFG-004";
    public const string YamlStructureError = "ACODE-CFG-005";
    public const string FileTooLarge = "ACODE-CFG-006";
    public const string NestingTooDeep = "ACODE-CFG-007";
    public const string TooManyKeys = "ACODE-CFG-008";
    public const string CircularReference = "ACODE-CFG-009";
    public const string RequiredFieldMissing = "ACODE-CFG-010";
    public const string TypeMismatch = "ACODE-CFG-011";
    public const string EnumViolation = "ACODE-CFG-012";
    public const string PatternViolation = "ACODE-CFG-013";
    public const string RangeViolation = "ACODE-CFG-014";
    public const string UnknownField = "ACODE-CFG-015";
    public const string DeprecatedField = "ACODE-CFG-016";
    public const string EnvVarMissing = "ACODE-CFG-017";
    public const string EnvVarError = "ACODE-CFG-018";
    public const string PathTraversal = "ACODE-CFG-019";
    public const string InvalidGlob = "ACODE-CFG-020";
    public const string ModeViolation = "ACODE-CFG-021";
    public const string ProviderViolation = "ACODE-CFG-022";
    public const string SchemaVersionUnsupported = "ACODE-CFG-023";
    public const string SemanticViolation = "ACODE-CFG-024";
    public const string SecurityViolation = "ACODE-CFG-025";
}
```

### Logging Schema

```csharp
// Log fields for configuration operations
public static class ConfigLogFields
{
    public const string ConfigPath = "config_path";
    public const string SchemaVersion = "schema_version";
    public const string ParseDurationMs = "parse_duration_ms";
    public const string ValidationDurationMs = "validation_duration_ms";
    public const string ErrorCount = "error_count";
    public const string WarningCount = "warning_count";
    public const string ErrorCode = "error_code";
    public const string FieldPath = "field_path";
    public const string LineNumber = "line_number";
    public const string CacheHit = "cache_hit";
}
```

### CLI Exit Codes

| Exit Code | Meaning |
|-----------|---------|
| 0 | Success / Valid configuration |
| 1 | Validation errors (schema or semantic) |
| 2 | Parse errors (YAML syntax) |
| 3 | File not found |
| 4 | Internal error |

### Validation Checklist Before Merge

- [ ] All 120 functional requirements implemented
- [ ] All 50 non-functional requirements verified
- [ ] All 25 error codes implemented and tested
- [ ] All 40 unit tests passing
- [ ] All 15 integration tests passing
- [ ] All 12 E2E tests passing
- [ ] All 10 performance benchmarks met
- [ ] Code coverage > 90%
- [ ] No security vulnerabilities (path traversal, injection)
- [ ] XML documentation on all public APIs
- [ ] README section updated
- [ ] CHANGELOG entry added
- [ ] Schema version compatibility verified
- [ ] Default values match schema
- [ ] Error messages reviewed for clarity
- [ ] Logging includes all specified fields
- [ ] Redaction works for all sensitive fields
- [ ] Thread safety verified
- [ ] Memory usage within bounds
- [ ] Performance within targets

### Rollout Plan

1. **Phase 1: Core Implementation**
   - Implement domain models
   - Implement YAML parser with limits
   - Implement schema validator
   - Add unit tests

2. **Phase 2: Semantic Validation**
   - Implement semantic validation rules
   - Implement environment interpolation
   - Implement default value application
   - Add integration tests

3. **Phase 3: CLI Integration**
   - Implement config commands
   - Implement error formatting
   - Add E2E tests
   - Run performance benchmarks

4. **Phase 4: Polish**
   - Review error messages
   - Add suggestions for common errors
   - Document all error codes
   - Update user manual

5. **Phase 5: Release**
   - Final testing pass
   - Documentation review
   - Merge to main
   - Update dependent tasks

---

**END OF TASK 002.b**