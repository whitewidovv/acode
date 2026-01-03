# Task 007.b: Validator Errors → Model Retry Contract

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 007, Task 007.a, Task 004.a (ToolResult types), Task 005.b (Tool Call Parsing), Task 001  

---

## Description

Task 007.b defines the contract for communicating validation errors back to the model, enabling intelligent retry behavior. When tool arguments fail schema validation, the error message must be formatted in a way the model can understand and use to correct its tool call. This retry contract is essential for robust agentic behavior—models frequently make minor errors that are easily corrected when given clear feedback.

Validation failures are common during model-driven tool calling. Models may omit required fields, use wrong types, exceed length limits, or provide values outside allowed ranges. Without a clear error format, the model cannot understand what went wrong or how to fix it. The retry contract specifies exactly how errors are formatted, ensuring consistent, actionable feedback.

The error format is designed for model comprehension. Models are trained on structured data and respond well to consistent patterns. The error format uses clear field names, explicit error types, and includes both the expected and actual values. This information enables models to identify their mistake and generate corrected arguments in the next turn.

Errors are returned as ToolResult messages in the conversation. When a tool call fails validation, instead of executing the tool, a ToolResult is created with is_error: true and the validation error as content. The model receives this result and can immediately attempt to correct the tool call. This follows the standard tool call retry pattern used by all major LLM providers.

The retry contract defines three error severity levels: Error (must be fixed, blocks execution), Warning (can be corrected, execution may proceed with degradation), and Info (advisory, no action required). Most validation failures are Errors, but some constraints may generate Warnings (e.g., path exists but is empty). The severity guides both model and human responses.

Retry limits prevent infinite loops. The contract specifies a maximum number of retry attempts (configurable, default 3) for any single tool call. After max retries, the system escalates to the user with the validation history. This prevents resource waste on tool calls the model cannot get right.

Error aggregation handles multiple validation failures in a single tool call. Arguments may have multiple problems simultaneously—missing required field and wrong type on another. All errors are reported together, not one at a time. This enables the model to fix all issues in a single retry rather than playing whack-a-mole with errors.

The contract integrates with the Tool Schema Registry (Task 007) which produces validation errors, the message types (Task 004.a) which define ToolResult, and the tool call parsing logic (Task 005.b) which may produce parse errors before validation even runs. All error sources flow through the same formatting pipeline.

Error formatting considers model token limits. Long validation errors consume context window. The formatter truncates extremely long error messages while preserving essential information. Field paths use compact JSON Pointer notation. Actual values are truncated with ellipsis for very long strings.

Logging captures validation failures for debugging and analysis. Every validation error is logged with structured fields including tool name, field path, error type, and request ID. Aggregated metrics track validation failure rates by tool, enabling identification of problematic schemas or model behaviors.

The retry contract is extensible for tool-specific error formatting. While most tools use the standard format, some tools may need custom error messages. The contract supports tool-specific error formatters that can provide domain-specific guidance while maintaining the standard structure.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Retry Contract | Specification for error-to-model communication |
| Validation Error | Failure when arguments don't match schema |
| ToolResult | Message type for tool execution results |
| Error Severity | Error, Warning, or Info classification |
| JSON Pointer | Path notation for JSON fields (/field/nested) |
| Error Aggregation | Collecting multiple errors together |
| Retry Limit | Maximum attempts before escalation |
| Model Comprehension | How well model understands errors |
| Error Formatter | Component converting errors to messages |
| Escalation | Passing control to user after failures |
| Validation History | Record of retry attempts |
| Token Budget | Context window consumed by errors |
| Compact Format | Minimized error representation |
| Tool-Specific Formatter | Custom error formatting per tool |
| Error Code | Structured identifier for error type |
| Expected Value | What the schema requires |
| Actual Value | What the model provided |

---

## Out of Scope

The following items are explicitly excluded from Task 007.b:

- **Schema validation logic** - Task 007 handles
- **Tool execution** - Separate orchestration task
- **User interaction for escalation** - UI/CLI separate
- **Model prompting strategies** - Agent orchestration
- **Automatic error correction** - Model decides corrections
- **Error recovery strategies** - Model decides retry
- **Persistent error storage** - In-memory only
- **Error analytics dashboard** - Logging only
- **Multi-language error messages** - English only
- **Error notification system** - Logging only

---

## Functional Requirements

### Validation Error Types

- FR-001: ValidationError MUST be defined in Application layer
- FR-002: ValidationError MUST include ErrorCode property
- FR-003: ValidationError MUST include FieldPath property (JSON Pointer)
- FR-004: ValidationError MUST include Message property
- FR-005: ValidationError MUST include Severity property
- FR-006: ValidationError MUST include ExpectedValue property
- FR-007: ValidationError MUST include ActualValue property (sanitized)
- FR-008: Severity MUST be enum: Error, Warning, Info

### Error Code Definitions

- FR-009: MUST define code for missing required field
- FR-010: MUST define code for type mismatch
- FR-011: MUST define code for constraint violation
- FR-012: MUST define code for invalid JSON
- FR-013: MUST define code for unknown field (strict mode)
- FR-014: MUST define code for array length violation
- FR-015: MUST define code for pattern mismatch
- FR-016: MUST define code for enum value invalid
- FR-017: Error codes MUST be string format: VAL-XXX

### Error Formatting

- FR-018: ErrorFormatter MUST convert errors to model message
- FR-019: Format MUST be structured for model comprehension
- FR-020: Format MUST include tool name
- FR-021: Format MUST list all errors
- FR-022: Format MUST include field paths
- FR-023: Format MUST include expected types/values
- FR-024: Format MUST include actual values (truncated)
- FR-025: Format MUST be under 2000 characters total

### ToolResult Integration

- FR-026: MUST create ToolResult for validation failures
- FR-027: ToolResult.IsError MUST be true
- FR-028: ToolResult.ToolCallId MUST match original call
- FR-029: ToolResult.Content MUST be formatted error
- FR-030: ToolResult MUST be returnable in conversation

### Error Aggregation

- FR-031: Aggregator MUST collect all errors from validation
- FR-032: Aggregator MUST deduplicate identical errors
- FR-033: Aggregator MUST sort errors by path
- FR-034: Aggregator MUST prioritize Error severity
- FR-035: Aggregator MUST limit total error count (default 10)

### Retry Tracking

- FR-036: RetryTracker MUST track attempts per tool call
- FR-037: RetryTracker MUST store validation history
- FR-038: RetryTracker MUST check against max retries
- FR-039: RetryTracker MUST provide retry count
- FR-040: Max retries MUST be configurable (default 3)

### Escalation

- FR-041: MUST escalate after max retries exceeded
- FR-042: Escalation MUST include all validation history
- FR-043: Escalation MUST include original tool call
- FR-044: Escalation MUST provide clear summary
- FR-045: Escalation MUST set status to Blocked

### Message Formatting

- FR-046: Message MUST start with error summary line
- FR-047: Message MUST list errors with bullets
- FR-048: Message MUST include correction hints
- FR-049: Message MUST include retry attempt number
- FR-050: Message MUST be parseable by model

### Truncation

- FR-051: Long strings MUST be truncated with "..."
- FR-052: Max string preview MUST be 100 characters
- FR-053: Arrays MUST show first/last items if too long
- FR-054: Nested objects MUST show depth limit
- FR-055: Total message MUST fit token budget

### Configuration

- FR-056: Max retries MUST be configurable
- FR-057: Error limit per validation MUST be configurable
- FR-058: Message max length MUST be configurable
- FR-059: Configuration MUST have defaults
- FR-060: Configuration MUST be in .agent/config.yml

### Logging

- FR-061: MUST log validation failure
- FR-062: Log MUST include tool name
- FR-063: Log MUST include error count
- FR-064: Log MUST include retry attempt
- FR-065: Log MUST include field paths
- FR-066: Log MUST NOT include full argument values

---

## Non-Functional Requirements

### Performance

- NFR-001: Error formatting MUST complete in < 1ms
- NFR-002: Aggregation MUST complete in < 100μs
- NFR-003: Retry tracking MUST be O(1) lookup
- NFR-004: Memory per retry history MUST be < 10KB

### Reliability

- NFR-005: Formatter MUST NOT throw on malformed input
- NFR-006: Formatter MUST handle null values gracefully
- NFR-007: Truncation MUST NOT corrupt unicode
- NFR-008: Retry tracking MUST be thread-safe

### Security

- NFR-009: Actual values MUST be sanitized before logging
- NFR-010: Secrets MUST NOT appear in error messages
- NFR-011: File paths MUST be relative in messages
- NFR-012: Long strings MUST be truncated to prevent injection

### Observability

- NFR-013: All validation failures MUST be logged
- NFR-014: Retry attempts MUST be logged
- NFR-015: Escalations MUST be logged at WARN level
- NFR-016: Metrics MUST track failure rates

### Maintainability

- NFR-017: Error codes MUST be documented
- NFR-018: Message format MUST be documented
- NFR-019: All public APIs MUST have XML docs
- NFR-020: Format MUST be versioned

---

## User Manual Documentation

### Overview

When tool arguments fail validation, Acode formats errors for the model to understand and retry. This contract ensures consistent, actionable feedback that enables models to self-correct.

### Error Format

Validation errors are returned as ToolResult messages:

```json
{
    "role": "tool",
    "tool_call_id": "call_abc123",
    "content": "Validation failed for tool 'read_file' (attempt 1/3):\n\n- /path: Required field is missing (expected: string)\n- /encoding: Invalid enum value 'uft8' (expected: utf-8, ascii, utf-16)\n\nPlease correct these errors and try again.",
    "is_error": true
}
```

### Error Codes

| Code | Description |
|------|-------------|
| VAL-001 | Required field missing |
| VAL-002 | Type mismatch |
| VAL-003 | Constraint violation (min/max) |
| VAL-004 | Invalid JSON syntax |
| VAL-005 | Unknown field (strict mode) |
| VAL-006 | Array length violation |
| VAL-007 | Pattern mismatch |
| VAL-008 | Invalid enum value |
| VAL-009 | String length violation |
| VAL-010 | Format violation |

### Severity Levels

| Severity | Description | Retry? |
|----------|-------------|--------|
| Error | Must be fixed | Yes, blocks execution |
| Warning | Should be fixed | Execution may proceed |
| Info | Advisory only | No action needed |

### Configuration

```yaml
tools:
  validation:
    retry:
      max_attempts: 3
      
    formatting:
      max_message_length: 2000
      max_errors_shown: 10
      max_value_preview: 100
```

### Retry Flow

1. Model makes tool call
2. Arguments fail validation
3. Formatted error returned as ToolResult
4. Model receives error and attempts correction
5. If still failing after max_attempts, escalate to user

### Escalation Message

After max retries:

```
Tool 'read_file' validation failed after 3 attempts.

Attempt 1: Missing required field 'path'
Attempt 2: Type mismatch on 'path' (got: integer)
Attempt 3: String too long for 'path' (max: 4096)

The model was unable to provide valid arguments. Please intervene or provide guidance.
```

### Logging

Validation failures are logged with structured fields:

```json
{
    "level": "warn",
    "message": "Tool validation failed",
    "tool_name": "read_file",
    "error_count": 2,
    "retry_attempt": 1,
    "max_retries": 3,
    "errors": [
        {"path": "/path", "code": "VAL-001"},
        {"path": "/encoding", "code": "VAL-008"}
    ],
    "correlation_id": "abc-123"
}
```

### Best Practices

1. **Clear Schemas**: Well-documented schemas reduce validation failures
2. **Reasonable Limits**: Don't set constraints too tight
3. **Good Descriptions**: Help models understand expectations
4. **Monitor Failures**: Track which tools have high failure rates

---

## Acceptance Criteria

### Error Types

- [ ] AC-001: ValidationError defined
- [ ] AC-002: ErrorCode property exists
- [ ] AC-003: FieldPath uses JSON Pointer
- [ ] AC-004: Message property exists
- [ ] AC-005: Severity enum exists
- [ ] AC-006: ExpectedValue property exists
- [ ] AC-007: ActualValue sanitized
- [ ] AC-008: Severity has Error/Warning/Info

### Error Codes

- [ ] AC-009: VAL-001 for required missing
- [ ] AC-010: VAL-002 for type mismatch
- [ ] AC-011: VAL-003 for constraints
- [ ] AC-012: VAL-004 for invalid JSON
- [ ] AC-013: VAL-005 for unknown field
- [ ] AC-014: VAL-006 for array length
- [ ] AC-015: VAL-007 for pattern
- [ ] AC-016: VAL-008 for enum
- [ ] AC-017: Codes are string format

### Formatting

- [ ] AC-018: Formatter converts errors
- [ ] AC-019: Format is structured
- [ ] AC-020: Includes tool name
- [ ] AC-021: Lists all errors
- [ ] AC-022: Includes field paths
- [ ] AC-023: Includes expected values
- [ ] AC-024: Includes actual (truncated)
- [ ] AC-025: Under 2000 chars

### ToolResult

- [ ] AC-026: Creates ToolResult
- [ ] AC-027: IsError is true
- [ ] AC-028: ToolCallId matches
- [ ] AC-029: Content is formatted
- [ ] AC-030: Returnable in conversation

### Aggregation

- [ ] AC-031: Collects all errors
- [ ] AC-032: Deduplicates
- [ ] AC-033: Sorts by path
- [ ] AC-034: Prioritizes errors
- [ ] AC-035: Limits count

### Retry Tracking

- [ ] AC-036: Tracks attempts
- [ ] AC-037: Stores history
- [ ] AC-038: Checks max retries
- [ ] AC-039: Provides count
- [ ] AC-040: Max configurable

### Escalation

- [ ] AC-041: Escalates after max
- [ ] AC-042: Includes history
- [ ] AC-043: Includes original call
- [ ] AC-044: Clear summary
- [ ] AC-045: Sets Blocked status

### Message Format

- [ ] AC-046: Summary line
- [ ] AC-047: Bulleted errors
- [ ] AC-048: Correction hints
- [ ] AC-049: Attempt number
- [ ] AC-050: Model parseable

### Truncation

- [ ] AC-051: Long strings truncated
- [ ] AC-052: Max 100 chars preview
- [ ] AC-053: Arrays show first/last
- [ ] AC-054: Objects show depth
- [ ] AC-055: Fits token budget

### Configuration

- [ ] AC-056: Max retries configurable
- [ ] AC-057: Error limit configurable
- [ ] AC-058: Max length configurable
- [ ] AC-059: Defaults exist
- [ ] AC-060: In config.yml

### Logging

- [ ] AC-061: Logs validation failure
- [ ] AC-062: Includes tool name
- [ ] AC-063: Includes error count
- [ ] AC-064: Includes attempt
- [ ] AC-065: Includes paths
- [ ] AC-066: No full values

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Application/ToolSchemas/Retry/
├── ValidationErrorTests.cs
│   ├── Should_Create_With_All_Fields()
│   ├── Should_Sanitize_ActualValue()
│   └── Should_Use_JSON_Pointer_Path()
│
├── ErrorFormatterTests.cs
│   ├── Should_Format_Single_Error()
│   ├── Should_Format_Multiple_Errors()
│   ├── Should_Include_Tool_Name()
│   ├── Should_Include_Attempt_Number()
│   ├── Should_Truncate_Long_Values()
│   └── Should_Respect_Max_Length()
│
├── ErrorAggregatorTests.cs
│   ├── Should_Collect_All_Errors()
│   ├── Should_Deduplicate()
│   ├── Should_Sort_By_Path()
│   └── Should_Limit_Count()
│
├── RetryTrackerTests.cs
│   ├── Should_Track_Attempts()
│   ├── Should_Store_History()
│   ├── Should_Check_Max_Retries()
│   └── Should_Be_Thread_Safe()
│
└── EscalationFormatterTests.cs
    ├── Should_Include_History()
    ├── Should_Summarize_Failures()
    └── Should_Format_For_User()
```

### Integration Tests

```
Tests/Integration/ToolSchemas/Retry/
├── RetryContractIntegrationTests.cs
│   ├── Should_Create_ToolResult_On_Failure()
│   ├── Should_Track_Retry_Across_Turns()
│   └── Should_Escalate_After_Max()
```

---

## User Verification Steps

### Scenario 1: Single Error

1. Call tool with missing required field
2. Verify: Error returned as ToolResult
3. Verify: Message mentions field path
4. Verify: Attempt 1/3 shown

### Scenario 2: Multiple Errors

1. Call tool with multiple problems
2. Verify: All errors in one message
3. Verify: Bulleted list format
4. Verify: Sorted by path

### Scenario 3: Retry Success

1. First call fails validation
2. Model corrects and retries
3. Verify: Attempt 2/3 shown
4. Verify: Validation passes

### Scenario 4: Max Retries

1. Fail validation 3 times
2. Verify: Escalation triggered
3. Verify: History in message
4. Verify: Blocked status

### Scenario 5: Long Value Truncation

1. Provide very long string
2. Verify: Truncated in message
3. Verify: "..." shown
4. Verify: Under length limit

### Scenario 6: Logging

1. Fail validation
2. Check logs
3. Verify: Structured fields present
4. Verify: No full values logged

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/ToolSchemas/Retry/
├── ValidationError.cs
├── ErrorSeverity.cs
├── ErrorCode.cs
├── IErrorFormatter.cs
├── IRetryTracker.cs
├── IEscalationHandler.cs
└── RetryConfiguration.cs

src/AgenticCoder.Infrastructure/ToolSchemas/Retry/
├── ErrorFormatter.cs
├── ErrorAggregator.cs
├── RetryTracker.cs
├── EscalationFormatter.cs
└── ValueSanitizer.cs
```

### ValidationError Class

```csharp
namespace AgenticCoder.Application.ToolSchemas.Retry;

public sealed class ValidationError
{
    public required string ErrorCode { get; init; }
    public required string FieldPath { get; init; }
    public required string Message { get; init; }
    public required ErrorSeverity Severity { get; init; }
    public string? ExpectedValue { get; init; }
    public string? ActualValue { get; init; }
}
```

### Error Codes

| Code | Message Template |
|------|-----------------|
| VAL-001 | Required field '{field}' is missing |
| VAL-002 | Type mismatch: expected {expected}, got {actual} |
| VAL-003 | Value out of range: {constraint} |
| VAL-004 | Invalid JSON: {parse_error} |
| VAL-005 | Unknown field '{field}' |
| VAL-006 | Array length {actual} exceeds {max} |
| VAL-007 | Value doesn't match pattern: {pattern} |
| VAL-008 | Invalid enum value '{value}' |
| VAL-009 | String length {actual} exceeds {max} |
| VAL-010 | Invalid format: {format} |

### Formatted Message Example

```
Validation failed for tool 'write_file' (attempt 2/3):

Errors:
• /path (VAL-001): Required field 'path' is missing
  Expected: string

• /content (VAL-009): String length 1500000 exceeds maximum 1048576
  Expected: string with max length 1048576
  Actual: "Lorem ipsum dolor sit amet..." (truncated)

Please provide the missing 'path' field and reduce 'content' length.
```

### Implementation Checklist

1. [ ] Create ValidationError class
2. [ ] Create ErrorSeverity enum
3. [ ] Create ErrorCode constants
4. [ ] Create IErrorFormatter interface
5. [ ] Create IRetryTracker interface
6. [ ] Create IEscalationHandler interface
7. [ ] Create RetryConfiguration
8. [ ] Implement ErrorFormatter
9. [ ] Implement ErrorAggregator
10. [ ] Implement RetryTracker
11. [ ] Implement EscalationFormatter
12. [ ] Implement ValueSanitizer
13. [ ] Wire up DI registration
14. [ ] Write unit tests
15. [ ] Add XML documentation

### Dependencies

- Task 007 (Tool Schema Registry)
- Task 007.a (Core tool schemas)
- Task 004.a (ToolResult type)

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~Retry"
```

---

**End of Task 007.b Specification**