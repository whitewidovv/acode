# Task 025: Task Spec Format

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 002 (Config), Task 003 (Interfaces), Task 018 (Outbox)  

---

## Description

Task 025 defines the task specification format. A task spec is a structured document describing work for a worker to execute. This format MUST be parseable, validatable, and serializable.

Task specs MUST support both YAML and JSON formats. The schema MUST enforce required fields. Optional fields MUST have documented defaults. Validation MUST catch errors before queue insertion.

The task spec format MUST capture enough information for autonomous execution. This includes title, description, affected files, dependencies on other tasks, priority, and execution constraints.

### Business Value

A well-defined task spec format enables:
- Consistent task representation across the system
- Automated validation before execution
- Clear handoff between planner and executor
- Queryable task metadata for queue management
- Reproducible task execution

### Scope Boundaries

This task covers the core task spec format. Subtask 025.a covers schema details. Subtask 025.b covers CLI commands. Subtask 025.c covers error formatting.

### Integration Points

- Task 002: Config influences defaults
- Task 003: Task spec implements ITaskSpec
- Task 018: Completed tasks emit to outbox
- Task 026: Queue stores task specs

### Failure Modes

- Invalid YAML/JSON syntax → ParseError
- Missing required field → ValidationError
- Invalid field type → ValidationError
- Unknown field → Warning (lenient mode)
- Circular dependency → DependencyError

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| TaskSpec | Structured document describing work |
| ULID | Universally Unique Lexicographically Sortable Identifier |
| Schema | Structure definition for validation |
| Required Field | Field that MUST be present |
| Optional Field | Field with default value |
| Dependency | Task that MUST complete first |
| Priority | Execution order hint (1-5) |
| Tag | Categorization label |
| Metadata | Extension key-value pairs |
| Parser | Converts text to TaskSpec |
| Validator | Checks TaskSpec against schema |
| Serializer | Converts TaskSpec to text |

---

## Out of Scope

- Task execution logic (Epic 06, Task 027)
- Queue persistence (Task 026)
- Task decomposition (Epic 03)
- Git worktree mapping (Epic 05)
- Visual task editor
- Task templates
- Remote task submission
- Task versioning history

---

## Functional Requirements

### FR-001 to FR-025: Core Structure

- FR-001: TaskSpec MUST have `id` field (ULID)
- FR-002: TaskSpec MUST have `title` field (string, 1-200 chars)
- FR-003: TaskSpec MUST have `description` field (string, 1-10000 chars)
- FR-004: TaskSpec MUST have `status` field (enum)
- FR-005: TaskSpec MUST have `createdAt` field (ISO8601)
- FR-006: TaskSpec MAY have `priority` field (int 1-5, default 3)
- FR-007: TaskSpec MAY have `dependencies` field (string[])
- FR-008: TaskSpec MAY have `files` field (string[])
- FR-009: TaskSpec MAY have `tags` field (string[])
- FR-010: TaskSpec MAY have `metadata` field (object)
- FR-011: TaskSpec MAY have `timeout` field (int seconds)
- FR-012: TaskSpec MAY have `retryLimit` field (int, default 3)
- FR-013: TaskSpec MAY have `parentId` field (ULID)
- FR-014: TaskSpec MAY have `estimatedDuration` field (int seconds)
- FR-015: TaskSpec MAY have `labels` field (object)
- FR-016: ID MUST be auto-generated if not provided
- FR-017: Status MUST default to Pending
- FR-018: CreatedAt MUST default to now
- FR-019: Unknown fields MUST be preserved in metadata
- FR-020: Null values MUST be treated as absent
- FR-021: Empty string MUST fail validation for required fields
- FR-022: Whitespace-only MUST fail validation for required fields
- FR-023: Title MUST be trimmed
- FR-024: Description MUST be trimmed
- FR-025: Dependencies MUST be valid ULIDs

### FR-026 to FR-045: Parsing

- FR-026: YAML format MUST be supported
- FR-027: JSON format MUST be supported
- FR-028: Parser MUST detect format automatically
- FR-029: Parser MUST handle BOM
- FR-030: Parser MUST handle CRLF and LF
- FR-031: Parser MUST reject binary content
- FR-032: Parser MUST enforce size limit (1MB)
- FR-033: Parser MUST handle UTF-8
- FR-034: Parser MUST reject invalid UTF-8
- FR-035: YAML anchors MUST be supported
- FR-036: YAML merge keys MUST be supported
- FR-037: JSON comments MUST NOT be supported
- FR-038: Multi-document YAML MUST parse first only
- FR-039: Parse errors MUST include line/column
- FR-040: Parse errors MUST include context
- FR-041: Parser MUST be streaming for large files
- FR-042: Parser MUST timeout after 5 seconds
- FR-043: Parser MUST handle nested objects
- FR-044: Parser MUST limit nesting depth (20)
- FR-045: Parser MUST limit array size (10000)

### FR-046 to FR-070: Validation

- FR-046: Validation MUST check required fields
- FR-047: Validation MUST check field types
- FR-048: Validation MUST check string lengths
- FR-049: Validation MUST check numeric ranges
- FR-050: Validation MUST check enum values
- FR-051: Validation MUST check date formats
- FR-052: Validation MUST check ULID format
- FR-053: Validation MUST collect all errors
- FR-054: Validation MUST NOT stop at first error
- FR-055: Validation errors MUST include field path
- FR-056: Validation errors MUST include expected value
- FR-057: Validation errors MUST include actual value
- FR-058: Validation MUST check dependency cycles
- FR-059: Validation MUST check file paths
- FR-060: File paths MUST be relative
- FR-061: File paths MUST NOT traverse above root
- FR-062: File paths MUST be normalized
- FR-063: Tags MUST match pattern [a-z0-9-]+
- FR-064: Labels keys MUST match pattern [a-z0-9-]+
- FR-065: Labels values MUST be strings
- FR-066: Metadata keys MUST NOT start with underscore
- FR-067: Metadata MUST be JSON-serializable
- FR-068: Priority MUST be 1-5
- FR-069: Timeout MUST be positive
- FR-070: RetryLimit MUST be 0-10

---

## Non-Functional Requirements

- NFR-001: Parse MUST complete in <100ms for 1MB
- NFR-002: Validation MUST complete in <50ms
- NFR-003: Serialization MUST complete in <50ms
- NFR-004: Memory MUST NOT exceed 10x input size
- NFR-005: Parser MUST be thread-safe
- NFR-006: Validator MUST be thread-safe
- NFR-007: No external dependencies for parsing
- NFR-008: Schema MUST be versioned
- NFR-009: Schema MUST be backward compatible
- NFR-010: Schema changes MUST be documented
- NFR-011: Error messages MUST be actionable
- NFR-012: Error messages MUST NOT leak secrets
- NFR-013: Secrets in metadata MUST be redacted
- NFR-014: Stack traces MUST NOT appear in errors
- NFR-015: Logging MUST include correlation ID

---

## User Manual Documentation

### Quick Start

Create a task spec file:

```yaml
# task.yaml
title: "Implement user login"
description: |
  Add login functionality with email/password.
  Include validation and error handling.
priority: 2
files:
  - src/Auth/LoginHandler.cs
  - tests/Auth/LoginHandlerTests.cs
tags:
  - auth
  - feature
```

Parse and validate:

```bash
$ acode task validate task.yaml
✓ Task spec valid

$ acode task add task.yaml
Task abc123 added to queue
```

### Schema Reference

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| id | ULID | No | Auto | Unique identifier |
| title | string | Yes | - | Short title (1-200 chars) |
| description | string | Yes | - | Full description |
| status | enum | No | Pending | Current status |
| priority | int | No | 3 | Priority 1-5 (1=highest) |
| dependencies | string[] | No | [] | Task IDs to wait for |
| files | string[] | No | [] | Affected files |
| tags | string[] | No | [] | Categorization |
| metadata | object | No | {} | Extension data |
| timeout | int | No | 3600 | Seconds before timeout |
| retryLimit | int | No | 3 | Max retry attempts |

### CLI Examples

```bash
# Validate a task spec
acode task validate task.yaml

# Add from file
acode task add task.yaml

# Add from stdin
cat task.yaml | acode task add -

# Add with inline YAML
acode task add --title "Fix bug" --description "Fix login bug"

# Show task details
acode task show abc123

# List all tasks
acode task list

# List with filter
acode task list --status pending --priority 1

# Retry failed task
acode task retry abc123

# Cancel task
acode task cancel abc123
```

### Configuration

```yaml
# .agent/config.yml
task:
  defaultPriority: 3
  defaultTimeout: 3600
  defaultRetryLimit: 3
  maxDescriptionLength: 10000
  strictValidation: true
  unknownFieldsAllowed: false
```

### Troubleshooting

**"Missing required field: title"**
- Ensure your task spec includes a `title` field
- Check for typos in field names

**"Invalid ULID format"**
- IDs must be valid 26-character ULIDs
- Let the system auto-generate IDs

**"Circular dependency detected"**
- Check dependencies list for cycles
- Task A → B → A is not allowed

---

## Acceptance Criteria / Definition of Done

### Functionality
- [ ] AC-001: YAML parsing works
- [ ] AC-002: JSON parsing works
- [ ] AC-003: Format auto-detected
- [ ] AC-004: Required fields enforced
- [ ] AC-005: Optional fields have defaults
- [ ] AC-006: Unknown fields preserved
- [ ] AC-007: Validation catches all errors
- [ ] AC-008: Error messages are clear
- [ ] AC-009: Serialization round-trips
- [ ] AC-010: ID auto-generation works

### Safety
- [ ] AC-011: Size limits enforced
- [ ] AC-012: Depth limits enforced
- [ ] AC-013: Timeout enforced
- [ ] AC-014: Secrets redacted
- [ ] AC-015: Path traversal blocked

### CLI
- [ ] AC-016: validate command works
- [ ] AC-017: add command works
- [ ] AC-018: show command works
- [ ] AC-019: list command works
- [ ] AC-020: retry command works
- [ ] AC-021: cancel command works

### Logging
- [ ] AC-022: Parse events logged
- [ ] AC-023: Validation events logged
- [ ] AC-024: Errors logged with context
- [ ] AC-025: Correlation IDs included

### Performance
- [ ] AC-026: Parse <100ms for 1MB
- [ ] AC-027: Validate <50ms
- [ ] AC-028: Memory bounds respected

### Docs
- [ ] AC-029: Schema documented
- [ ] AC-030: CLI documented
- [ ] AC-031: Errors documented
- [ ] AC-032: Examples provided

### Tests
- [ ] AC-033: Unit tests pass
- [ ] AC-034: Integration tests pass
- [ ] AC-035: Coverage >80%

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Parse valid YAML
- [ ] UT-002: Parse valid JSON
- [ ] UT-003: Parse error handling
- [ ] UT-004: Validate required fields
- [ ] UT-005: Validate field types
- [ ] UT-006: Validate ranges
- [ ] UT-007: Validate dependencies
- [ ] UT-008: Serialize to YAML
- [ ] UT-009: Serialize to JSON
- [ ] UT-010: Round-trip equality

### Integration Tests

- [ ] IT-001: Full parse-validate-serialize
- [ ] IT-002: CLI validate command
- [ ] IT-003: CLI add command
- [ ] IT-004: Large file handling
- [ ] IT-005: Error aggregation

---

## Implementation Prompt

### Interfaces

```csharp
public interface ITaskSpecParser
{
    Task<ParseResult<TaskSpec>> ParseAsync(
        Stream input, 
        CancellationToken ct = default);
        
    Task<ParseResult<TaskSpec>> ParseAsync(
        string content, 
        CancellationToken ct = default);
}

public interface ITaskSpecValidator
{
    Task<ValidationResult> ValidateAsync(
        TaskSpec spec, 
        CancellationToken ct = default);
}

public interface ITaskSpecSerializer
{
    Task<string> SerializeAsync(
        TaskSpec spec, 
        SerializationFormat format,
        CancellationToken ct = default);
}

public record TaskSpec(
    string Id,
    string Title,
    string Description,
    TaskStatus Status,
    int Priority,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> Files,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, object> Metadata,
    int TimeoutSeconds,
    int RetryLimit,
    DateTimeOffset CreatedAt);

public enum SerializationFormat { Yaml, Json }
```

### Error Codes

| Code | Meaning |
|------|---------|
| TASK-001 | Parse error |
| TASK-002 | Missing required field |
| TASK-003 | Invalid field type |
| TASK-004 | Value out of range |
| TASK-005 | Invalid ULID |
| TASK-006 | Circular dependency |
| TASK-007 | Invalid file path |
| TASK-008 | Size limit exceeded |

---

**End of Task 025 Specification**