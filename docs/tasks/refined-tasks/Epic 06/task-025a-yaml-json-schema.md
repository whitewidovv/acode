# Task 025.a: YAML/JSON Schema

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 025 (Task Spec Format)  

---

## Description

Task 025.a defines the JSON Schema for task specs. The schema MUST be the single source of truth for validation. YAML and JSON MUST both validate against this schema.

The schema MUST define all field types, constraints, and defaults. The schema MUST support draft-07 or later. The schema MUST be exportable for external tool integration.

Schema validation MUST produce detailed errors. Each error MUST identify the field path and constraint violated. Errors MUST suggest corrections where possible.

### Business Value

A formal schema enables:
- Automated validation in any language
- IDE autocomplete and validation
- Documentation generation
- API contract enforcement
- External tool integration

### Scope Boundaries

This task covers schema definition. Parsing is in Task 025. CLI is in Task 025.b. Error formatting is in Task 025.c.

### Integration Points

- Task 025: Uses schema for validation
- Task 025.b: CLI exposes schema commands
- External: Schema exported for IDE plugins

### Failure Modes

- Schema load failure → System cannot start
- Schema version mismatch → Migration required
- Invalid schema → Build-time failure

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| JSON Schema | IETF standard for JSON structure |
| Draft-07 | JSON Schema specification version |
| $ref | Schema reference for reuse |
| oneOf | Schema union type |
| allOf | Schema intersection type |
| pattern | Regex for string validation |
| format | Semantic string format |
| additionalProperties | Unknown property handling |
| required | Mandatory field list |
| default | Value when absent |

---

## Out of Scope

- Custom schema language
- GraphQL schema
- Protobuf definition
- XML Schema (XSD)
- Dynamic schema generation
- Schema UI builder

---

## Functional Requirements

### FR-001 to FR-030: Schema Structure

- FR-001: Schema MUST use JSON Schema draft-07+
- FR-002: Schema MUST define `$schema` field
- FR-003: Schema MUST define `$id` field
- FR-004: Schema MUST define `title` field
- FR-005: Schema MUST define `description` field
- FR-006: Schema MUST define `type` as "object"
- FR-007: Schema MUST define `properties` object
- FR-008: Schema MUST define `required` array
- FR-009: Schema MUST define `additionalProperties`
- FR-010: Each property MUST have `type`
- FR-011: Each property MUST have `description`
- FR-012: String properties MUST have `maxLength`
- FR-013: String properties MAY have `minLength`
- FR-014: String properties MAY have `pattern`
- FR-015: String properties MAY have `format`
- FR-016: Number properties MUST have `minimum`
- FR-017: Number properties MUST have `maximum`
- FR-018: Array properties MUST have `items`
- FR-019: Array properties MUST have `maxItems`
- FR-020: Array properties MAY have `minItems`
- FR-021: Array properties MAY have `uniqueItems`
- FR-022: Object properties MUST have `properties`
- FR-023: Enum properties MUST have `enum` array
- FR-024: Properties MAY have `default` value
- FR-025: Properties MAY have `examples` array
- FR-026: Schema MUST define `definitions` for reuse
- FR-027: Schema MUST use `$ref` for shared types
- FR-028: ULID format MUST be custom-defined
- FR-029: ISO8601 format MUST use "date-time"
- FR-030: File path format MUST be custom-defined

### FR-031 to FR-050: Field Definitions

- FR-031: `id` MUST be ULID format
- FR-032: `id` MUST be optional (auto-generated)
- FR-033: `title` MUST be string 1-200 chars
- FR-034: `title` MUST be required
- FR-035: `description` MUST be string 1-10000 chars
- FR-036: `description` MUST be required
- FR-037: `status` MUST be enum
- FR-038: `status` MUST default to "pending"
- FR-039: `priority` MUST be integer 1-5
- FR-040: `priority` MUST default to 3
- FR-041: `dependencies` MUST be array of ULID
- FR-042: `dependencies` MUST default to empty
- FR-043: `files` MUST be array of file-path
- FR-044: `files` MUST default to empty
- FR-045: `tags` MUST be array of tag-pattern
- FR-046: `tags` MUST default to empty
- FR-047: `metadata` MUST be object
- FR-048: `metadata` MUST default to empty
- FR-049: `timeout` MUST be positive integer
- FR-050: `retryLimit` MUST be 0-10

### FR-051 to FR-065: Validation Behavior

- FR-051: Validation MUST use schema directly
- FR-052: Validation MUST collect all errors
- FR-053: Validation MUST return structured result
- FR-054: Each error MUST have path
- FR-055: Each error MUST have message
- FR-056: Each error MUST have code
- FR-057: Each error MUST have schemaPath
- FR-058: Custom formats MUST have validators
- FR-059: ULID validator MUST check format
- FR-060: File-path validator MUST check traversal
- FR-061: Coercion MUST be disabled
- FR-062: Additional properties MUST be preserved
- FR-063: Unknown properties MUST emit warning
- FR-064: Strict mode MUST reject unknowns
- FR-065: Schema MUST be cached after load

---

## Non-Functional Requirements

- NFR-001: Schema load MUST complete in <100ms
- NFR-002: Validation MUST complete in <50ms
- NFR-003: Schema MUST be <100KB
- NFR-004: Schema MUST be embeddable
- NFR-005: Schema MUST be exportable
- NFR-006: Schema MUST be human-readable
- NFR-007: Schema MUST be versioned
- NFR-008: Schema version MUST use semver
- NFR-009: Breaking changes MUST bump major
- NFR-010: Additions MUST bump minor

---

## User Manual Documentation

### Schema File Location

The schema is located at:
```
src/Domain/TaskSpecs/Schemas/task-spec.schema.json
```

### Export Schema

```bash
# Export to file
acode schema export task-spec > task-spec.schema.json

# Export with version
acode schema export task-spec --version 1.0.0
```

### Validate Against Schema

```bash
# Validate file
acode task validate task.yaml --schema

# Show schema errors
acode task validate task.yaml --verbose
```

### Schema in IDE

VS Code users can add to settings:

```json
{
  "yaml.schemas": {
    "./task-spec.schema.json": "*.task.yaml"
  }
}
```

### Custom Formats

| Format | Pattern | Example |
|--------|---------|---------|
| ulid | `[0-9A-HJKMNP-TV-Z]{26}` | `01ARZ3NDEKTSV4RRFFQ69G5FAV` |
| file-path | Relative, no traversal | `src/Handler.cs` |
| tag | `[a-z0-9-]+` | `feature-auth` |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Schema file exists
- [ ] AC-002: Schema is valid JSON Schema
- [ ] AC-003: All fields defined
- [ ] AC-004: Required fields marked
- [ ] AC-005: Defaults specified
- [ ] AC-006: Constraints documented
- [ ] AC-007: Custom formats work
- [ ] AC-008: ULID validation works
- [ ] AC-009: File-path validation works
- [ ] AC-010: Tag validation works
- [ ] AC-011: Export command works
- [ ] AC-012: IDE integration works
- [ ] AC-013: Errors include path
- [ ] AC-014: Errors include code
- [ ] AC-015: Schema versioned

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Valid schema loads
- [ ] UT-002: All properties defined
- [ ] UT-003: ULID format validates
- [ ] UT-004: File-path format validates
- [ ] UT-005: Required fields enforced
- [ ] UT-006: Defaults applied

### Integration Tests

- [ ] IT-001: Full validation flow
- [ ] IT-002: Schema export
- [ ] IT-003: IDE integration

---

## Implementation Prompt

### Schema Structure

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://acode.dev/schemas/task-spec/v1",
  "title": "Acode Task Specification",
  "description": "Schema for Acode task specifications",
  "type": "object",
  "required": ["title", "description"],
  "properties": {
    "id": {
      "type": "string",
      "format": "ulid",
      "description": "Unique task identifier (auto-generated if omitted)"
    },
    "title": {
      "type": "string",
      "minLength": 1,
      "maxLength": 200,
      "description": "Short task title"
    },
    "description": {
      "type": "string",
      "minLength": 1,
      "maxLength": 10000,
      "description": "Full task description"
    },
    "status": {
      "type": "string",
      "enum": ["pending", "running", "completed", "failed", "cancelled", "blocked"],
      "default": "pending"
    },
    "priority": {
      "type": "integer",
      "minimum": 1,
      "maximum": 5,
      "default": 3
    },
    "dependencies": {
      "type": "array",
      "items": { "type": "string", "format": "ulid" },
      "default": [],
      "uniqueItems": true
    },
    "files": {
      "type": "array",
      "items": { "type": "string", "format": "file-path" },
      "default": [],
      "maxItems": 1000
    },
    "tags": {
      "type": "array",
      "items": { "type": "string", "pattern": "^[a-z0-9-]+$" },
      "default": [],
      "maxItems": 50
    },
    "metadata": {
      "type": "object",
      "default": {},
      "additionalProperties": true
    },
    "timeout": {
      "type": "integer",
      "minimum": 1,
      "default": 3600
    },
    "retryLimit": {
      "type": "integer",
      "minimum": 0,
      "maximum": 10,
      "default": 3
    }
  },
  "additionalProperties": true
}
```

---

**End of Task 025.a Specification**