# Task 025.c: Human-Readable Errors

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 025 (Task Spec), Task 025.a (Schema), Task 025.b (CLI)  

---

## Description

Task 025.c implements human-readable error formatting for task spec operations. Errors MUST be clear, actionable, and helpful. Technical details MUST be available but not overwhelming.

Error messages MUST explain what went wrong, why it matters, and how to fix it. Validation errors MUST point to the exact location in the input. Parse errors MUST show context around the problem.

The error formatting MUST support multiple output modes. Terminal output MUST use colors and formatting. JSON output MUST be structured. Log output MUST be grep-friendly.

### Business Value

Clear errors enable:
- Faster problem resolution
- Reduced support burden
- Better user experience
- Self-service debugging
- Fewer repeat mistakes

### Scope Boundaries

This task covers error formatting only. Error generation is in Tasks 025 and 025.a. CLI integration is in Task 025.b.

### Integration Points

- Task 025: Parse errors
- Task 025.a: Validation errors
- Task 025.b: CLI display
- Task 009: Error framework

### Failure Modes

- Formatter crash → Fallback to raw error
- Missing context → Show partial info
- Color not supported → Plain text fallback

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Error Code | Machine-readable error identifier |
| Error Message | Human-readable description |
| Context | Surrounding content for location |
| Caret | ^ pointer to error position |
| Suggestion | Recommended fix action |
| Severity | Error, Warning, Info levels |
| Source | File/line/column of error |
| Snippet | Code/content extract |

---

## Out of Scope

- Error recovery/auto-fix
- Interactive error resolution
- Error statistics/analytics
- Error translation/i18n
- Error learning system

---

## Functional Requirements

### FR-001 to FR-025: Error Structure

- FR-001: Error MUST have code (e.g., TASK-001)
- FR-002: Error MUST have message
- FR-003: Error MUST have severity
- FR-004: Error MAY have source location
- FR-005: Error MAY have context snippet
- FR-006: Error MAY have suggestion
- FR-007: Error MAY have documentation URL
- FR-008: Error MAY have related errors
- FR-009: Multiple errors MUST be grouped
- FR-010: Errors MUST be sorted by location
- FR-011: Duplicate errors MUST be deduplicated
- FR-012: Error count MUST be shown
- FR-013: Warning count MUST be shown
- FR-014: Summary MUST appear at end
- FR-015: First error MUST be highlighted
- FR-016: Critical errors MUST appear first
- FR-017: Errors MUST include field path
- FR-018: Nested paths MUST use dot notation
- FR-019: Array indices MUST be bracketed
- FR-020: Root errors MUST show "(root)"
- FR-021: Expected value MUST be shown
- FR-022: Actual value MUST be shown
- FR-023: Value MUST be truncated if long
- FR-024: Truncation MUST show length
- FR-025: Secrets MUST be redacted

### FR-026 to FR-050: Display Formatting

- FR-026: Terminal MUST use ANSI colors
- FR-027: Error MUST be red
- FR-028: Warning MUST be yellow
- FR-029: Info MUST be blue
- FR-030: Path MUST be cyan
- FR-031: Code MUST be bold
- FR-032: NO_COLOR MUST disable colors
- FR-033: TERM=dumb MUST disable colors
- FR-034: Context snippet MUST show 3 lines
- FR-035: Error line MUST be highlighted
- FR-036: Line numbers MUST be shown
- FR-037: Caret MUST point to column
- FR-038: Caret line MUST be colored
- FR-039: Suggestion MUST be prefixed
- FR-040: URL MUST be clickable (OSC 8)
- FR-041: Width MUST respect terminal
- FR-042: Long lines MUST wrap
- FR-043: Indent MUST be consistent
- FR-044: Spacing MUST aid readability
- FR-045: Icons MAY be used (✗, ⚠, ℹ)
- FR-046: Icons MUST have text fallback
- FR-047: Output MUST be UTF-8
- FR-048: Box drawing MAY enhance
- FR-049: Plain mode MUST be available
- FR-050: Plain MUST be grep-friendly

### FR-051 to FR-070: JSON Format

- FR-051: JSON MUST be valid
- FR-052: JSON MUST have `errors` array
- FR-053: Each error MUST have `code`
- FR-054: Each error MUST have `message`
- FR-055: Each error MUST have `severity`
- FR-056: Each error MAY have `source`
- FR-057: Source MUST have `file`
- FR-058: Source MUST have `line`
- FR-059: Source MUST have `column`
- FR-060: Each error MAY have `context`
- FR-061: Context MUST have `snippet`
- FR-062: Context MUST have `highlightLine`
- FR-063: Each error MAY have `suggestion`
- FR-064: Each error MAY have `url`
- FR-065: JSON MUST have `summary`
- FR-066: Summary MUST have `errorCount`
- FR-067: Summary MUST have `warningCount`
- FR-068: Summary MUST have `valid` boolean
- FR-069: JSON MUST be compact by default
- FR-070: `--pretty` MUST format JSON

---

## Non-Functional Requirements

- NFR-001: Formatting MUST complete in <10ms
- NFR-002: Memory MUST be bounded
- NFR-003: Large files MUST stream
- NFR-004: Fallback MUST always work
- NFR-005: No crashes on malformed input
- NFR-006: Thread-safe formatting
- NFR-007: Testable output
- NFR-008: Consistent across platforms
- NFR-009: Accessible color choices
- NFR-010: Screen reader compatible

---

## User Manual Documentation

### Error Display Examples

**Validation Error:**
```
✗ TASK-002: Missing required field

  --> task.yaml:1:1
   |
 1 | priority: 2
 2 | files:
   |
   = missing field: title
   
  Suggestion: Add a 'title' field (1-200 characters)
  
  See: https://acode.dev/docs/errors/TASK-002
```

**Parse Error:**
```
✗ TASK-001: Invalid YAML syntax

  --> task.yaml:5:3
   |
 4 |   - src/file.cs
 5 |   - bad indent
   |   ^^^
 6 | tags:
   |
   = expected proper indentation
   
  Suggestion: Check indentation matches surrounding lines
```

**Multiple Errors:**
```
Found 3 errors and 1 warning in task.yaml

✗ TASK-002: Missing required field 'title' at (root)
✗ TASK-002: Missing required field 'description' at (root)
✗ TASK-004: Value out of range at 'priority' (got: 10, expected: 1-5)
⚠ TASK-010: Unknown field 'custom' will be stored in metadata

Summary: 3 errors, 1 warning - validation failed
```

### JSON Output

```json
{
  "errors": [
    {
      "code": "TASK-002",
      "message": "Missing required field",
      "severity": "error",
      "source": {
        "file": "task.yaml",
        "line": 1,
        "column": 1
      },
      "context": {
        "snippet": "priority: 2\nfiles:",
        "highlightLine": 1
      },
      "suggestion": "Add a 'title' field (1-200 characters)",
      "url": "https://acode.dev/docs/errors/TASK-002"
    }
  ],
  "summary": {
    "errorCount": 1,
    "warningCount": 0,
    "valid": false
  }
}
```

### Error Codes

| Code | Category | Description |
|------|----------|-------------|
| TASK-001 | Parse | YAML/JSON syntax error |
| TASK-002 | Validation | Missing required field |
| TASK-003 | Validation | Invalid field type |
| TASK-004 | Validation | Value out of range |
| TASK-005 | Validation | Invalid ULID format |
| TASK-006 | Validation | Circular dependency |
| TASK-007 | Validation | Invalid file path |
| TASK-008 | Limit | Size limit exceeded |
| TASK-009 | Limit | Nesting too deep |
| TASK-010 | Warning | Unknown field |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Error code shown
- [ ] AC-002: Message is clear
- [ ] AC-003: Location shown
- [ ] AC-004: Context snippet shown
- [ ] AC-005: Suggestion provided
- [ ] AC-006: Colors work
- [ ] AC-007: NO_COLOR respected
- [ ] AC-008: JSON format correct
- [ ] AC-009: Summary shown
- [ ] AC-010: Multiple errors grouped
- [ ] AC-011: Secrets redacted
- [ ] AC-012: Long values truncated
- [ ] AC-013: Fallback works
- [ ] AC-014: Screen reader OK
- [ ] AC-015: Documentation URL works

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Single error format
- [ ] UT-002: Multiple errors format
- [ ] UT-003: Context extraction
- [ ] UT-004: Color codes
- [ ] UT-005: JSON output
- [ ] UT-006: Plain text fallback

### Integration Tests

- [ ] IT-001: Full validation error flow
- [ ] IT-002: Parse error display
- [ ] IT-003: Terminal detection

---

## Implementation Prompt

### Interfaces

```csharp
public interface IErrorFormatter
{
    string Format(IReadOnlyList<TaskError> errors, 
        ErrorFormatOptions options);
}

public record TaskError(
    string Code,
    string Message,
    ErrorSeverity Severity,
    SourceLocation? Source,
    string? Context,
    string? Suggestion,
    string? DocumentationUrl);

public record SourceLocation(
    string File,
    int Line,
    int Column);

public record ErrorFormatOptions(
    OutputFormat Format,
    bool UseColors,
    int ContextLines,
    int MaxWidth);

public enum OutputFormat { Terminal, Plain, Json }
public enum ErrorSeverity { Error, Warning, Info }
```

### Color Scheme

```csharp
public static class ErrorColors
{
    public const string Error = "\x1b[31m";      // Red
    public const string Warning = "\x1b[33m";   // Yellow
    public const string Info = "\x1b[34m";      // Blue
    public const string Path = "\x1b[36m";      // Cyan
    public const string Code = "\x1b[1m";       // Bold
    public const string Reset = "\x1b[0m";
}
```

---

**End of Task 025.c Specification**