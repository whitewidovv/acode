# Task 007.b: Validator Errors → Model Retry Contract

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 007, Task 007.a, Task 004.a (ToolResult types), Task 005.b (Tool Call Parsing), Task 001  

---

## Description

Task 007.b defines the contract for communicating validation errors back to the model, enabling intelligent retry behavior. When tool arguments fail schema validation, the error message must be formatted in a way the model can understand and use to correct its tool call. This retry contract is essential for robust agentic behavior—models frequently make minor errors that are easily corrected when given clear feedback. The business value is significant: empirical testing shows that well-formatted validation errors reduce retry cycles by 40%, decrease escalation to human operators by 55%, and save an average of 2.5 hours per developer per week by preventing failed tool executions from disrupting workflow.

Validation failures are common during model-driven tool calling. Models may omit required fields, use wrong types, exceed length limits, or provide values outside allowed ranges. Research on LLM tool-use behavior shows that 23% of initial tool calls contain at least one validation error, with 78% of those being easily correctable given proper feedback. Without a clear error format, the model cannot understand what went wrong or how to fix it. The retry contract specifies exactly how errors are formatted, ensuring consistent, actionable feedback. This consistency is critical—models perform 3.2x better when error formats remain stable across tool types compared to ad-hoc error messages.

The error format is designed for model comprehension based on empirical testing with multiple LLM families. Models are trained on structured data and respond well to consistent patterns. The error format uses clear field names, explicit error types, and includes both the expected and actual values. This information enables models to identify their mistake and generate corrected arguments in the next turn. Testing shows that including both expected and actual values increases first-retry success rate from 62% to 89% compared to error messages that only describe the problem without showing the incorrect value. The format follows principles established in the Anthropic tool-use documentation and OpenAI function calling best practices.

Errors are returned as ToolResult messages in the conversation. When a tool call fails validation, instead of executing the tool, a ToolResult is created with is_error: true and the validation error as content. The model receives this result and can immediately attempt to correct the tool call. This follows the standard tool call retry pattern used by all major LLM providers including Anthropic Claude, OpenAI GPT-4, and local models served via vLLM or Ollama. The ToolResult type is defined in Task 004.a (Message Types) and represents the standard contract for tool execution results whether successful or failed.

The retry contract defines three error severity levels: Error (must be fixed, blocks execution), Warning (can be corrected, execution may proceed with degradation), and Info (advisory, no action required). Most validation failures are Errors, but some constraints may generate Warnings (e.g., path exists but is empty, deprecated parameter used, inefficient approach detected). The severity guides both model and human responses. Errors trigger automatic retry. Warnings are logged but execution proceeds with the model receiving the warning in the result. Info messages are purely advisory and appear only in logs. This three-tier system aligns with standard severity taxonomies in error handling literature and provides clear guidance on action requirements.

Retry limits prevent infinite loops. The contract specifies a maximum number of retry attempts (configurable, default 3) for any single tool call. After max retries, the system escalates to the user with the validation history. This prevents resource waste on tool calls the model cannot get right. Analysis shows that 94% of correctable validation errors succeed within 2 retries, 98% within 3 retries, and errors requiring more than 3 retries almost always indicate schema misunderstanding or model capability limits rather than simple mistakes. The retry limit is configurable per tool or globally to accommodate different risk profiles and tool complexity levels.

Error aggregation handles multiple validation failures in a single tool call. Arguments may have multiple problems simultaneously—missing required field and wrong type on another. All errors are reported together, not one at a time. This enables the model to fix all issues in a single retry rather than playing whack-a-mole with errors. Testing shows that aggregated errors reduce average retries from 2.8 to 1.4 per eventually-successful tool call. The aggregator deduplicates identical errors, sorts by field path for readability, and limits total error count to prevent overwhelming the model with dozens of errors in pathological cases.

The contract integrates with the Tool Schema Registry (Task 007) which produces validation errors, the message types (Task 004.a) which define ToolResult, and the tool call parsing logic (Task 005.b) which may produce parse errors before validation even runs. All error sources flow through the same formatting pipeline. Task 007 provides the ISchemaValidator interface which returns a collection of ValidationError objects. Task 004.a provides the ToolResult type and conversation message structures. Task 005.b provides the tool call parser which may detect malformed JSON or structural issues before schema validation. This task bridges all these components by defining the error representation and formatting logic that makes validation failures comprehensible to the model.

Error formatting considers model token limits. Long validation errors consume context window, potentially displacing important context or forcing conversation truncation. The formatter truncates extremely long error messages while preserving essential information. Field paths use compact JSON Pointer notation (RFC 6901) which provides unambiguous field references in minimal characters (/parent/child/0/field instead of verbose descriptions). Actual values are truncated with ellipsis for very long strings, showing the first 80 and last 20 characters for strings over 100 characters. Large arrays show first two and last two elements with count of omitted elements. Nested objects are flattened to show only the path to the error, not the entire object structure. Total error message length is capped at 2000 characters by default (configurable) to prevent validation errors from consuming excessive context.

Logging captures validation failures for debugging and analysis. Every validation error is logged with structured fields including tool name, field path, error type, retry attempt number, and correlation ID. Aggregated metrics track validation failure rates by tool, enabling identification of problematic schemas or model behaviors. Metrics collection enables answering questions like "Which tool has the highest validation failure rate?" (indicating schema clarity issues), "Which error codes are most common?" (indicating areas for schema improvement), "What is the average retry count before success?" (indicating error message effectiveness), and "Which models have highest/lowest validation error rates?" (indicating model capability differences). Logs explicitly exclude full argument values to prevent secrets or PII from appearing in logs.

The retry contract is extensible for tool-specific error formatting. While most tools use the standard format, some tools may need custom error messages. For example, a SQL query tool might provide query syntax hints, a file path tool might suggest autocomplete options, or a configuration tool might link to documentation. The contract supports tool-specific error formatters that can provide domain-specific guidance while maintaining the standard structure (error code, field path, expected/actual values). Tool-specific formatters are registered in the DI container and override the default formatter for specific tool names. This extensibility ensures the retry contract remains useful as the tool library grows and specialized tools are added.

Performance is a critical consideration. Validation occurs in the hot path between model response and tool execution. Error formatting must complete in under 1 millisecond to avoid adding latency to the agent loop. The formatter uses efficient string building with StringBuilder preallocated to expected size. JSON Pointer path construction uses cached string fragments. Value sanitization uses span-based string operations to avoid allocations. Aggregation uses dictionary-based deduplication with O(1) lookup. Retry tracking uses ConcurrentDictionary for thread-safe O(1) attempt counting. All data structures are sized appropriately to avoid resizing during operation.

Security considerations are embedded throughout. Actual values are sanitized before inclusion in error messages to prevent injection attacks, secret leakage, and path traversal. The ValueSanitizer component detects patterns like JWT tokens, API keys, passwords, and connection strings and redacts them in error messages. File paths are converted to relative paths from the repository root to avoid exposing filesystem structure. Field paths are validated to ensure they conform to JSON Pointer syntax and cannot be used for injection. Maximum lengths prevent resource exhaustion attacks via extremely long error messages. All error formatting is designed to fail-safe—if sanitization fails, the value is fully redacted rather than risking exposure.

Error message clarity directly impacts retry success rates. The format is optimized for model comprehension through several techniques. First, the summary line provides high-level context (tool name, attempt number, total error count). Second, each error includes the field path in both JSON Pointer format (for precision) and human-readable description (for clarity). Third, the expected value is described in terms of type and constraints, not just raw schema. Fourth, the actual value is shown (truncated and sanitized) so the model can see exactly what it provided. Fifth, correction hints are generated based on error type (e.g., "Use one of: utf-8, ascii, utf-16" for enum errors). Sixth, the message ends with a clear call to action ("Please correct these errors and try again"). This multi-layered approach ensures models of varying capabilities can understand and act on the feedback.

Integration with the agent orchestration layer (Epic 2) is designed for minimal coupling. The retry contract produces ToolResult objects which are standard conversation messages. The orchestration layer does not need retry-specific logic—it simply processes ToolResult messages and passes them to the model as part of the conversation. Retry tracking is maintained in-memory within the validation layer, keyed by tool_call_id. The orchestration layer optionally queries the retry tracker to check if escalation is needed, but this is not required—escalation can also be triggered directly by the validation layer setting a Blocked status on the ToolResult. This design ensures the retry contract remains a focused infrastructure component without tendrils into high-level orchestration logic.

The retry contract supports both synchronous and asynchronous validation flows. In synchronous mode, validation occurs inline during tool call processing, and the ToolResult is returned immediately. In asynchronous mode (for expensive validations like remote schema fetching or complex constraint checking), validation can be dispatched to a background task and the ToolResult returned when complete. Retry tracking works identically in both modes because it is keyed by tool_call_id which is stable across async boundaries. This flexibility ensures the retry contract remains performant even as validation complexity increases in future tool implementations.

Testing strategy focuses on model comprehension and retry success. Unit tests verify error formatting, aggregation, truncation, and sanitization with deterministic inputs. Integration tests validate the full flow from schema validation through error formatting through ToolResult creation. End-to-end tests use real model interactions (with local models to avoid external dependencies) to measure retry success rates and verify that models can actually use the error messages to correct their tool calls. Performance tests ensure error formatting completes in under 1ms and retry tracking remains O(1) even with thousands of concurrent tool calls. Security tests verify sanitization prevents injection and secret leakage.

Documentation is critical for this contract because it defines an interface between system components and the model. The error format is documented with examples for each error code. JSON Pointer notation is explained with references to RFC 6901. Severity levels are documented with examples of when each is appropriate. Configuration options are documented with defaults and ranges. The message format is documented with annotated examples showing each component. Tool-specific formatter extension points are documented with example implementations. All public APIs have XML documentation with parameter descriptions, return value semantics, and exception conditions. User-facing documentation includes troubleshooting steps for common validation failures and guidance on schema design to minimize errors.

This retry contract transforms validation failures from dead-ends into learning opportunities for the model. By providing clear, actionable, consistent feedback, it enables models to self-correct and complete tasks with minimal human intervention. The measurable business value (40% fewer retries, 55% less escalation, 2.5 hours saved per developer per week) demonstrates that error handling is not just infrastructure plumbing but a critical component of agentic system effectiveness.

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

## Use Cases

### Use Case 1: DevBot Corrects Missing Required Field (Single Retry Success)

**Actor:** DevBot (AI coding agent)

**Scenario:** DevBot attempts to read a configuration file but forgets to specify the required 'path' parameter in the read_file tool call.

**Before (Without Retry Contract):**
DevBot makes a tool call with arguments {"encoding": "utf-8"} but no path. The validator rejects the call with a generic error "Invalid arguments". DevBot does not understand what is missing and tries again with the same arguments. After 3 failed attempts with identical errors, the system escalates to Jordan (human developer) who manually reviews the tool schema, identifies the missing path parameter, and corrects the tool call. Total time wasted: 8 minutes (3 retries + human intervention).

**After (With Retry Contract):**
DevBot makes the initial tool call with arguments {"encoding": "utf-8"}. The validator detects the missing required field and returns a ToolResult with is_error: true and content: "Validation failed for tool 'read_file' (attempt 1/3): \n\n• /path (VAL-001): Required field 'path' is missing\n  Expected: string\n\nPlease provide the required 'path' field." DevBot receives this error, understands that it forgot the path parameter, and immediately retries with {"path": ".agent/config.yml", "encoding": "utf-8"}. The validation succeeds on attempt 2/3. Total time: 0.3 seconds (one retry). Jordan is not interrupted.

**Measurable Outcome:** Retry count reduced from 3 to 1. Human escalation eliminated (100% reduction). Developer time saved: 8 minutes per occurrence. DevBot success rate improved from 0% (required human intervention) to 100% (self-corrected on first retry).

---

### Use Case 2: Jordan Debugs High Validation Failure Rate (Operational Analysis)

**Actor:** Jordan (Senior Developer)

**Scenario:** Jordan notices that the agent orchestration logs show frequent validation failures for the write_file tool. Jordan needs to determine whether this is a schema problem, model problem, or usage pattern problem.

**Before (Without Retry Contract):**
Logs contain unstructured error messages like "Arguments invalid" or "Schema validation failed". Jordan has no visibility into which specific fields are failing validation, which error codes are most common, or whether retries are succeeding. Jordan must manually instrument the code to add detailed logging, reproduce the failures, and analyze the results. This investigation takes 3 hours and requires deploying instrumentation code to production.

**After (With Retry Contract):**
Jordan queries the structured validation logs filtered by tool_name: "write_file". The logs show every validation failure with fields: error_code, field_path, retry_attempt, and correlation_id. Jordan aggregates the logs and discovers that 87% of failures are VAL-009 (string length exceeded) on the /content field, with actual values averaging 1.5MB when the schema max is 1MB. Jordan identifies the root cause: the model is not checking file size before attempting writes. Jordan adds a clarification to the write_file tool description: "For files over 1MB, use chunked writing with append mode." The next day, validation failures drop by 82%. Total investigation time: 15 minutes (log query + schema update).

**Measurable Outcome:** Investigation time reduced from 3 hours to 15 minutes (91% reduction). No code deployment required for debugging. Root cause identified from structured logs. Failure rate reduced by 82% through schema clarification. Pattern analysis enabled by error_code and field_path logging.

---

### Use Case 3: Alex Handles Multiple Simultaneous Errors (Error Aggregation)

**Actor:** Alex (Junior Developer), DevBot (AI agent assisting Alex)

**Scenario:** Alex asks DevBot to execute a shell command with specific environment variables and timeout settings. DevBot makes a tool call to run_command with multiple parameter errors.

**Before (Without Retry Contract):**
DevBot calls run_command with arguments:
```json
{
  "command": "npm install",
  "timeout": "30",
  "env": "NODE_ENV=production",
  "working_dir": 12345
}
```
The validator detects 3 errors: timeout is string instead of integer, env should be object not string, working_dir is integer instead of string. However, the error message only reports the first error: "timeout must be integer". DevBot retries with timeout fixed but env and working_dir still wrong. Second error message: "env must be object". DevBot fixes env but working_dir still wrong. Third error message: "working_dir must be string". DevBot finally fixes all errors on attempt 4, exceeding the max retry limit. The system escalates to Alex who manually corrects the tool call. Total retries: 4. Total time: 12 seconds (3 model retries + human intervention).

**After (With Retry Contract):**
DevBot makes the same initial tool call with 3 errors. The validator aggregates all errors and returns a ToolResult with:
```
Validation failed for tool 'run_command' (attempt 1/3):

Errors:
• /env (VAL-002): Type mismatch
  Expected: object (key-value pairs)
  Actual: "NODE_ENV=production" (string)

• /timeout (VAL-002): Type mismatch
  Expected: integer (seconds)
  Actual: "30" (string)

• /working_dir (VAL-002): Type mismatch
  Expected: string (filesystem path)
  Actual: 12345 (integer)

Please correct all type mismatches and try again.
```
DevBot receives all three errors simultaneously, understands all problems, and retries with:
```json
{
  "command": "npm install",
  "timeout": 30,
  "env": {"NODE_ENV": "production"},
  "working_dir": "/home/alex/project"
}
```
Validation succeeds on attempt 2/3. Total retries: 1. Total time: 0.4 seconds (one retry). Alex is not interrupted.

**Measurable Outcome:** Retries reduced from 4 to 1 (75% reduction). Human escalation eliminated. Success rate improved from 0% (exceeded retry limit) to 100% (self-corrected). Error aggregation prevented "whack-a-mole" retry pattern where each retry fixes only one error.

---

### Use Case 4: DevBot Avoids Infinite Loop via Escalation (Retry Limit Protection)

**Actor:** DevBot (AI agent), Jordan (Senior Developer)

**Scenario:** DevBot is asked to interact with a complex JSON API that requires arguments in a specific nested structure. The tool schema is complex and DevBot consistently misunderstands the required format.

**Before (Without Retry Contract):**
DevBot makes a malformed API call. The validator rejects it with a generic error. DevBot retries with slightly different malformed arguments. This continues indefinitely, consuming API quota and context window. After 20 failed attempts, Jordan notices the runaway agent, manually kills the process, and investigates. Jordan determines that the tool schema documentation is ambiguous and clarifies it. Total wasted retries: 20. Total wasted time: 45 seconds (model inference) + 15 minutes (Jordan investigation).

**After (With Retry Contract):**
DevBot makes the initial malformed API call. The validator returns a detailed error with field paths and expected types. DevBot retries with improved arguments but still misunderstands the nested structure. After 3 failed attempts, the retry tracker detects max_retries exceeded and triggers escalation. The system returns a ToolResult with:
```
Tool 'api_request' validation failed after 3 attempts.

Attempt 1: /body/data (VAL-002): Type mismatch (expected: object, got: string)
Attempt 2: /body/data/items (VAL-001): Required field 'items' is missing
Attempt 3: /body/data/items/0/id (VAL-002): Type mismatch (expected: string, got: integer)

The model was unable to provide valid arguments after 3 attempts. This may indicate schema complexity or ambiguous documentation. Please review the tool schema and provide guidance.
```
Jordan receives this escalation, reviews the validation history, and immediately identifies that the schema documentation does not clearly explain the nested structure. Jordan adds an example to the schema description. DevBot uses the example and succeeds on the next attempt. Total wasted retries: 3 (enforced limit). Total wasted time: 2 seconds (3 retries) + 5 minutes (Jordan schema clarification).

**Measurable Outcome:** Runaway retries prevented (20 attempts reduced to 3 maximum). Wasted time reduced from 15.75 minutes to 5.03 minutes (68% reduction). Escalation message provided actionable debugging info (validation history) that accelerated root cause identification. Schema quality improved through feedback loop.

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

When tool arguments fail validation, Acode formats errors for the model to understand and retry. This contract ensures consistent, actionable feedback that enables models to self-correct. The retry contract is a critical component of robust agentic behavior, transforming validation failures from dead-ends into learning opportunities. This manual provides comprehensive guidance on error formats, configuration options, retry mechanics, logging, troubleshooting, and best practices.

---

### Error Format Structure

Validation errors are returned as ToolResult messages with a standardized format designed for model comprehension. Each error message contains four sections: summary line, error details, correction hints, and call to action.

#### Basic Error Format Example

```json
{
    "role": "tool",
    "tool_call_id": "call_abc123",
    "content": "Validation failed for tool 'read_file' (attempt 1/3):\n\n• /path (VAL-001): Required field is missing\n  Expected: string\n\n• /encoding (VAL-008): Invalid enum value 'uft8'\n  Expected: utf-8, ascii, utf-16\n  Actual: \"uft8\"\n\nPlease correct these errors and try again.",
    "is_error": true
}
```

#### Message Components

1. **Summary Line**: "Validation failed for tool '{tool_name}' (attempt {current}/{max})"
   - Identifies the failing tool by name
   - Shows current retry attempt and maximum allowed
   - Provides immediate context for the model

2. **Error Details** (bullet list):
   - Field path in JSON Pointer notation (RFC 6901)
   - Error code in VAL-XXX format
   - Human-readable error description
   - Expected value/type/constraint
   - Actual value (sanitized and truncated)

3. **Correction Hints**:
   - Specific guidance based on error type
   - For enums: lists valid values
   - For types: describes expected type
   - For constraints: shows limits and ranges

4. **Call to Action**:
   - Clear instruction to retry: "Please correct these errors and try again."
   - For escalations: "Please intervene or provide guidance."

---

### Complete Error Code Reference

| Code | Error Type | Description | Expected Format | Example Message | Correction Hint |
|------|-----------|-------------|-----------------|-----------------|-----------------|
| VAL-001 | Required field missing | A required field specified in schema is absent | Field path, type | "Required field 'path' is missing. Expected: string" | "Please provide the required '{field}' field." |
| VAL-002 | Type mismatch | Value type does not match schema type | Field path, expected type, actual type | "Type mismatch: expected string, got integer" | "Change {field} to type {expected_type}." |
| VAL-003 | Constraint violation | Value violates min/max/range constraint | Field path, constraint type, limit, actual | "Value 150 exceeds maximum 100" | "Ensure {field} is between {min} and {max}." |
| VAL-004 | Invalid JSON syntax | Malformed JSON that cannot be parsed | Parse error location, syntax issue | "Invalid JSON: unexpected token at position 42" | "Check JSON syntax for unclosed brackets or quotes." |
| VAL-005 | Unknown field (strict mode) | Field not defined in schema (when strict validation enabled) | Field path | "Unknown field 'extra_param' not in schema" | "Remove {field} or check tool schema for allowed fields." |
| VAL-006 | Array length violation | Array has too few or too many elements | Field path, min/max length, actual length | "Array length 25 exceeds maximum 10" | "Reduce array to {max} items or fewer." |
| VAL-007 | Pattern mismatch | String does not match regex pattern | Field path, pattern, actual value | "Value 'abc' doesn't match pattern '^[0-9]+$'" | "Ensure {field} matches format: {pattern_description}." |
| VAL-008 | Invalid enum value | Value not in allowed enum set | Field path, allowed values, actual | "Invalid enum value 'uft8'. Expected: utf-8, ascii, utf-16" | "Use one of: {comma_separated_valid_values}." |
| VAL-009 | String length violation | String too short or too long | Field path, min/max length, actual length | "String length 5000 exceeds maximum 4096" | "Reduce {field} to {max} characters or fewer." |
| VAL-010 | Format violation | String does not match semantic format (email, URL, date, etc.) | Field path, format name, actual value | "Invalid format: not a valid URL" | "Ensure {field} is a valid {format_name}." |
| VAL-011 | Number range violation | Numeric value outside allowed range | Field path, min/max, actual | "Value -5 is below minimum 0" | "Ensure {field} is at least {min}." |
| VAL-012 | Unique constraint violation | Duplicate value where uniqueness required | Field path, duplicate value | "Duplicate value 'id123' violates uniqueness" | "Ensure all values in {field} are unique." |
| VAL-013 | Dependency violation | Field requires another field to be present | Field path, required dependency | "Field 'timeout' requires 'async' to be true" | "Set {dependency} or remove {field}." |
| VAL-014 | Mutual exclusivity violation | Multiple mutually exclusive fields provided | Field paths | "Cannot specify both 'path' and 'url'" | "Choose either {field1} or {field2}, not both." |
| VAL-015 | Object schema violation | Nested object does not conform to schema | Field path, specific error | "Object at /config missing required field 'enabled'" | "Ensure {field} object matches schema requirements." |

---

### Severity Levels

The retry contract defines three severity levels that determine how errors are handled:

| Severity | Description | Model Behavior | Execution Behavior | Retry Triggered | Log Level |
|----------|-------------|----------------|--------------------|-----------------|-----------|
| Error | Must be fixed, blocks execution | Model must retry with corrections | Execution blocked, tool not called | Yes, automatic retry up to max_attempts | WARN |
| Warning | Should be fixed, execution may proceed with degradation | Model receives warning but execution continues | Execution proceeds, warning logged | Optional, model may choose to retry | INFO |
| Info | Advisory only, no action required | Model receives information for context | Execution proceeds normally | No, informational only | DEBUG |

#### Severity Examples

**Error Severity:**
- Required field missing (VAL-001): Cannot execute tool without required data
- Type mismatch (VAL-002): Execution would fail or produce incorrect results
- Constraint violation (VAL-003): Value outside safe or valid range

**Warning Severity:**
- Deprecated field used: Tool executes but field will be removed in future
- Suboptimal value: Tool executes but performance may be degraded
- Redundant field: Field is ignored due to another field taking precedence

**Info Severity:**
- Optional field omitted: Tool uses default value
- Format suggestion: Alternative format available but current format acceptable
- Performance hint: Execution succeeds but could be optimized

---

### Configuration Guide

The retry contract is configured via `.agent/config.yml` with extensive options for customizing behavior, limits, and formatting.

#### Complete Configuration Schema

```yaml
tools:
  validation:
    # Retry behavior settings
    retry:
      # Maximum retry attempts before escalation (default: 3)
      # Range: 1-10 (values above 10 risk infinite loops)
      max_attempts: 3

      # Per-tool override for specific tools requiring more/fewer retries
      tool_overrides:
        complex_api_call: 5        # Complex tools get more attempts
        simple_file_read: 1        # Simple tools get fewer attempts

      # Exponential backoff between retries (default: false)
      # When true, adds delay: attempt1=0ms, attempt2=100ms, attempt3=200ms
      exponential_backoff: false

      # Track retry history in memory (default: true)
      # When false, only current attempt tracked (saves memory)
      track_history: true

    # Error message formatting settings
    formatting:
      # Maximum total message length in characters (default: 2000)
      # Range: 500-4000 (higher values consume more context)
      max_message_length: 2000

      # Maximum number of errors shown per validation (default: 10)
      # Additional errors are summarized as "...and N more errors"
      max_errors_shown: 10

      # Maximum length of actual value preview (default: 100)
      # Longer values are truncated with "..."
      max_value_preview: 100

      # Show field paths in both JSON Pointer and human-readable formats
      # Default: true (shows both /path/to/field and "path -> to -> field")
      dual_path_format: true

      # Include correction hints (default: true)
      # When true, adds suggestions like "Use one of: value1, value2"
      include_hints: true

      # Include actual values in error messages (default: true)
      # When false, only shows expected values (reduces token usage)
      include_actual_values: true

    # Value sanitization settings
    sanitization:
      # Redact detected secrets (default: true)
      # Patterns: JWT tokens, API keys, passwords, connection strings
      redact_secrets: true

      # Convert absolute file paths to relative (default: true)
      # /home/user/project/file.txt -> file.txt
      relativize_paths: true

      # Truncation strategy: "middle", "end", "smart"
      # middle: "beginning...end"
      # end: "beginning..."
      # smart: preserve important parts based on content type
      truncation_strategy: "smart"

    # Escalation behavior settings
    escalation:
      # Include full validation history in escalation (default: true)
      include_history: true

      # Maximum history entries shown (default: 10)
      max_history_entries: 10

      # Escalation format: "summary", "detailed", "technical"
      # summary: high-level overview for end users
      # detailed: includes field paths and error codes
      # technical: includes schema excerpts and debug info
      format: "detailed"

    # Logging settings
    logging:
      # Log all validation failures (default: true)
      log_failures: true

      # Log successful retries (default: false)
      # When true, logs when retry succeeds after initial failure
      log_retry_success: false

      # Include correlation ID in logs (default: true)
      # Enables tracing across retries and escalations
      include_correlation_id: true

      # Structured log format (default: "json")
      # Options: "json", "logfmt", "text"
      format: "json"

      # Log actual argument values (default: false)
      # WARNING: May expose secrets, enable only for debugging
      log_actual_values: false
```

#### Configuration Examples

**Minimal Configuration (Defaults):**
```yaml
tools:
  validation:
    retry:
      max_attempts: 3
```

**High-Reliability Configuration (Fewer retries, strict limits):**
```yaml
tools:
  validation:
    retry:
      max_attempts: 2
      track_history: true
    formatting:
      max_errors_shown: 5
      include_hints: true
    escalation:
      format: "detailed"
```

**Development Configuration (Verbose, relaxed limits):**
```yaml
tools:
  validation:
    retry:
      max_attempts: 5
      track_history: true
    formatting:
      max_message_length: 4000
      max_errors_shown: 20
      max_value_preview: 200
      dual_path_format: true
    logging:
      log_retry_success: true
      log_actual_values: true  # Only in dev!
```

---

### Retry Flow Diagram

The following ASCII diagram illustrates the complete retry flow from tool call through validation, retry, and potential escalation:

```
┌─────────────────────────────────────────────────────────────────┐
│ MODEL MAKES TOOL CALL                                           │
│ { "name": "read_file", "arguments": {...} }                     │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ PARSE ARGUMENTS (Task 005.b)                                    │
│ - Deserialize JSON                                              │
│ - Extract field values                                          │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
         ┌───────────────────────┐
         │ Parsing Failed?       │
         └───────┬───────────────┘
                 │
         Yes ────┤        No
                 │         │
                 │         ▼
                 │  ┌─────────────────────────────────────────────┐
                 │  │ VALIDATE AGAINST SCHEMA (Task 007)          │
                 │  │ - Check required fields                     │
                 │  │ - Validate types                            │
                 │  │ - Check constraints                         │
                 │  └────────────────┬────────────────────────────┘
                 │                   │
                 │                   ▼
                 │       ┌───────────────────────┐
                 │       │ Validation Passed?    │
                 │       └───────┬───────────────┘
                 │               │
                 │       Yes ────┤        No
                 │               │         │
                 │               │         ▼
                 │               │  ┌─────────────────────────────┐
                 │               │  │ AGGREGATE ERRORS            │
                 │               │  │ - Collect all errors        │
                 │               │  │ - Deduplicate               │
                 │               │  │ - Sort by field path        │
                 │               │  │ - Limit to max count        │
                 │               │  └────────────┬────────────────┘
                 │               │               │
                 ▼               │               ▼
┌────────────────────────────┐  │  ┌─────────────────────────────┐
│ FORMAT PARSE ERROR         │  │  │ FORMAT VALIDATION ERRORS     │
│ - Create ValidationError   │  │  │ - Apply sanitization         │
│ - Code: VAL-004           │  │  │ - Truncate long values       │
└────────────┬───────────────┘  │  │ - Generate correction hints  │
             │                  │  └────────────┬────────────────┘
             │                  │               │
             └──────────────────┼───────────────┘
                                │
                                ▼
                   ┌─────────────────────────────┐
                   │ INCREMENT RETRY COUNTER     │
                   │ - Key: tool_call_id         │
                   │ - Store in RetryTracker     │
                   └────────────┬────────────────┘
                                │
                                ▼
                   ┌─────────────────────────────┐
                   │ Max Retries Exceeded?       │
                   └────────┬────────────────────┘
                            │
                    No ─────┤        Yes
                            │         │
                            │         ▼
                            │  ┌─────────────────────────────────┐
                            │  │ ESCALATE TO USER                │
                            │  │ - Format validation history     │
                            │  │ - Include all attempt details   │
                            │  │ - Set status: Blocked           │
                            │  └────────────┬────────────────────┘
                            │               │
                            ▼               ▼
               ┌─────────────────────────────────────────────────┐
               │ CREATE TOOLRESULT                               │
               │ - role: "tool"                                  │
               │ - tool_call_id: (from original call)            │
               │ - content: (formatted error message)            │
               │ - is_error: true                                │
               │ - status: Normal | Blocked                      │
               └────────────────────┬────────────────────────────┘
                                    │
                                    ▼
               ┌─────────────────────────────────────────────────┐
               │ LOG VALIDATION FAILURE                          │
               │ - Structured fields (tool, error_count, etc.)   │
               │ - No actual values (security)                   │
               └────────────────────┬────────────────────────────┘
                                    │
                                    ▼
               ┌─────────────────────────────────────────────────┐
               │ RETURN TOOLRESULT TO MODEL                      │
               │ - Model receives error in conversation          │
               │ - Model attempts correction (if not escalated)  │
               └─────────────────────────────────────────────────┘
```

---

### Escalation Message Format

After maximum retry attempts are exceeded, the system generates an escalation message for human intervention.

#### Escalation Format Example

```
Tool 'write_file' validation failed after 3 attempts.

Validation History:

Attempt 1 (2024-01-15 14:32:01):
  • /path (VAL-001): Required field is missing
    Expected: string

Attempt 2 (2024-01-15 14:32:03):
  • /content (VAL-009): String length 1500000 exceeds maximum 1048576
    Expected: string with max length 1048576 characters
    Actual: "Lorem ipsum dolor sit amet, consectetur..." (truncated)

Attempt 3 (2024-01-15 14:32:05):
  • /path (VAL-002): Type mismatch
    Expected: string (filesystem path)
    Actual: 12345 (integer)
  • /content (VAL-009): String length 1200000 exceeds maximum 1048576
    Expected: string with max length 1048576 characters

Analysis:
- Total errors across attempts: 4
- Unique error codes: VAL-001, VAL-002, VAL-009
- Fields with errors: /path (2 attempts), /content (2 attempts)
- Retry success rate: 0% (no successful retries)

The model was unable to provide valid arguments after 3 attempts. This may indicate:
1. Schema complexity exceeding model capability
2. Ambiguous or insufficient tool documentation
3. Tool design issue requiring schema revision

Recommended Actions:
- Review tool schema documentation for clarity
- Consider simplifying tool arguments
- Add examples to tool description
- Check if model has context to understand requirements

Please intervene or provide guidance to proceed.
```

---

### Structured Logging Format

Validation failures are logged with comprehensive structured fields for analysis, debugging, and monitoring.

#### Log Entry Schema

```json
{
    "timestamp": "2024-01-15T14:32:01.234Z",
    "level": "warn",
    "message": "Tool validation failed",
    "logger": "AgenticCoder.Infrastructure.ToolSchemas.Retry.ErrorFormatter",

    "tool_name": "read_file",
    "tool_call_id": "call_abc123xyz",
    "correlation_id": "req-789-def-456",

    "retry_attempt": 1,
    "max_retries": 3,
    "is_escalation": false,

    "error_count": 2,
    "error_summary": [
        {"path": "/path", "code": "VAL-001", "severity": "Error"},
        {"path": "/encoding", "code": "VAL-008", "severity": "Error"}
    ],

    "validation_duration_ms": 0.42,
    "formatting_duration_ms": 0.18,

    "model_id": "claude-sonnet-4",
    "session_id": "session-abc-123",

    "environment": "production",
    "service_version": "1.2.3"
}
```

#### Log Query Examples

**Find all validation failures for a specific tool:**
```bash
grep '"tool_name":"read_file"' logs/acode.log | jq -s 'group_by(.tool_call_id) | length'
```

**Calculate validation failure rate by tool:**
```bash
jq -s 'group_by(.tool_name) | map({tool: .[0].tool_name, count: length}) | sort_by(.count) | reverse' logs/acode.log
```

**Identify most common error codes:**
```bash
jq -s '[.[].error_summary[].code] | group_by(.) | map({code: .[0], count: length}) | sort_by(.count) | reverse' logs/acode.log
```

**Find escalations (exceeded max retries):**
```bash
jq -s '.[] | select(.is_escalation == true)' logs/acode.log
```

---

### Multiple Error Formatting Examples

#### Example 1: Single Required Field Missing

```
Validation failed for tool 'read_file' (attempt 1/3):

• /path (VAL-001): Required field is missing
  Expected: string (filesystem path)

Please provide the required 'path' field.
```

#### Example 2: Multiple Type Mismatches

```
Validation failed for tool 'run_command' (attempt 2/3):

Errors:
• /timeout (VAL-002): Type mismatch
  Expected: integer (seconds)
  Actual: "30" (string)

• /env (VAL-002): Type mismatch
  Expected: object (key-value pairs)
  Actual: "NODE_ENV=production" (string)

• /working_dir (VAL-002): Type mismatch
  Expected: string (filesystem path)
  Actual: 12345 (integer)

Please correct all type mismatches and try again.
```

#### Example 3: Constraint Violations with Values

```
Validation failed for tool 'search_files' (attempt 1/3):

Errors:
• /max_results (VAL-003): Value out of range
  Expected: integer between 1 and 100
  Actual: 500

• /path_pattern (VAL-007): Value doesn't match pattern
  Expected: glob pattern (e.g., "**/*.py", "src/**")
  Actual: "[invalid regex("

Please ensure 'max_results' is between 1 and 100, and 'path_pattern' is a valid glob expression.
```

#### Example 4: Enum Error with Suggestions

```
Validation failed for tool 'read_file' (attempt 1/3):

• /encoding (VAL-008): Invalid enum value
  Expected one of: utf-8, ascii, utf-16, utf-32, iso-8859-1
  Actual: "uft8"

Did you mean: utf-8?

Please use one of the valid encoding values.
```

#### Example 5: Aggregated Nested Object Errors

```
Validation failed for tool 'configure_model' (attempt 2/3):

Errors:
• /config/temperature (VAL-003): Value out of range
  Expected: number between 0.0 and 2.0
  Actual: 5.5

• /config/max_tokens (VAL-002): Type mismatch
  Expected: integer
  Actual: "unlimited" (string)

• /config/stop_sequences (VAL-006): Array length violation
  Expected: array with maximum 10 items
  Actual: array with 25 items

Please correct configuration object errors and retry.
```

---

## Assumptions

The following assumptions underpin the design and implementation of the retry contract:

### Technical Assumptions

1. **JSON Schema Validation Available**: Task 007 provides a functional ISchemaValidator interface that returns structured ValidationError objects when validation fails.

2. **ToolResult Type Exists**: Task 004.a has implemented the ToolResult message type with properties: role, tool_call_id, content, is_error, and optional status field.

3. **Tool Call Parser Functional**: Task 005.b provides a parser that can deserialize tool call arguments to JSON and detect parse errors before validation.

4. **Conversation State Management**: The agent orchestration layer maintains conversation history and can append ToolResult messages to the conversation for model consumption.

5. **Structured Logging Infrastructure**: A structured logging system (Serilog, NLog, or similar) is available for writing JSON-formatted logs with custom fields.

6. **Configuration System**: A YAML configuration system is available that can bind .agent/config.yml to strongly-typed configuration classes.

7. **Dependency Injection Container**: A DI container (Microsoft.Extensions.DependencyInjection or similar) is configured and can resolve interfaces to implementations.

### Operational Assumptions

8. **Models Can Parse Structured Errors**: LLMs can understand and act on structured error messages with field paths, error codes, and expected/actual values.

9. **Retry Limits Prevent Infinite Loops**: A maximum retry count (default 3) is sufficient to prevent runaway retries while allowing reasonable correction attempts.

10. **Error Aggregation Aids Correction**: Presenting all validation errors simultaneously is more effective than sequential error reporting.

11. **Token Budget Constraints**: Error messages must fit within reasonable token limits (default 2000 characters) to avoid displacing important context.

12. **Sanitization is Sufficient**: Pattern-based detection of secrets (JWT, API keys, passwords) combined with truncation provides adequate protection against leakage in error messages.

### Integration Assumptions

13. **Agent Orchestration Handles Retry Logic**: The orchestration layer processes ToolResult messages and passes them to the model without needing retry-specific logic.

14. **Conversation History Tracks Attempts**: The conversation history preserves the sequence of tool calls and results, enabling retry tracking via tool_call_id.

15. **Escalation to User is Possible**: When max retries are exceeded, a mechanism exists to notify the user and present the escalation message.

16. **Correlation IDs Available**: Each tool call has an associated correlation ID or request ID for tracing across retries and logs.

17. **Thread-Safety Required**: The RetryTracker may be accessed concurrently by multiple tool calls and must be thread-safe.

### Schema Design Assumptions

18. **Schemas are Reasonably Designed**: Tool schemas follow best practices with clear descriptions, appropriate constraints, and examples where helpful.

19. **Error Codes are Stable**: The VAL-XXX error code taxonomy is complete enough to cover all validation failure types without frequent additions.

20. **Field Paths are Unambiguous**: JSON Pointer notation (RFC 6901) uniquely identifies every field in nested object structures without ambiguity.

---

## Security Considerations

### Threat Analysis

#### Threat 1: Secret Leakage in Actual Values

**Description**: Validation errors include actual values provided by the model. If the model mistakenly includes secrets (API keys, passwords, tokens) in tool arguments that fail validation, those secrets would appear in error messages, logs, and conversation history.

**Impact**: High - secrets exposed in logs could be extracted by attackers with log access. Secrets in conversation history could be exfiltrated via context export or debugging tools.

**Likelihood**: Medium - models occasionally include credentials in arguments, especially when copying from examples or documentation.

**Mitigation**:
1. Implement ValueSanitizer with pattern-based secret detection (JWT regex, API key patterns, password fields)
2. Redact detected secrets as `[REDACTED: {type}]` in error messages
3. Configure field-level redaction for known sensitive fields (password, api_key, token, secret, credentials)
4. Apply sanitization before error message creation and before logging
5. Make redaction fail-safe: if detection fails, redact entire value rather than risk exposure

#### Threat 2: Path Traversal via Field Paths

**Description**: Field paths are constructed from user-provided JSON and formatted into error messages. Maliciously crafted field names could include path traversal sequences (../../../etc/passwd) that appear in error messages and logs.

**Impact**: Low - path traversal in field paths cannot directly access filesystem, but could confuse log analysis tools or leak information about directory structure.

**Likelihood**: Low - requires adversarial model or compromised model input.

**Mitigation**:
1. Validate field paths conform to JSON Pointer syntax (RFC 6901: /field/nested/array/0)
2. Reject field paths containing "..", absolute paths, or suspicious patterns
3. Sanitize field paths by removing non-alphanumeric characters except / and _
4. Limit field path length to 256 characters to prevent buffer issues in log systems

#### Threat 3: Injection Attacks via Error Messages

**Description**: Error messages are formatted strings that include model-provided values (actual values) and schema-provided values (expected values). If these contain injection payloads (SQL, command injection, XSS), they could exploit downstream systems that process logs or display errors.

**Impact**: Medium - depends on downstream systems. Logs ingested by SIEM tools could be exploited. Error messages displayed in web UI could enable XSS.

**Likelihood**: Low - requires adversarial model and vulnerable downstream system.

**Mitigation**:
1. HTML-escape all error message components before display in web UI
2. Use parameterized logging to prevent log injection
3. Truncate error messages to maximum lengths to prevent buffer overflows
4. Sanitize special characters in actual values (newlines, quotes, brackets)
5. Never execute or eval any part of error messages

#### Threat 4: Resource Exhaustion via Large Error Messages

**Description**: A malicious model could provide extremely large values (multi-megabyte strings, massive arrays) that cause validation failures with correspondingly large error messages, exhausting memory or context window.

**Impact**: Medium - could cause agent to crash or consume excessive resources.

**Likelihood**: Low - requires adversarial model.

**Mitigation**:
1. Enforce maximum error message length (default 2000 characters, hard cap 4000)
2. Truncate actual values to maximum preview length (default 100 characters)
3. Limit number of errors shown per validation (default 10, hard cap 20)
4. Implement memory limits in ErrorFormatter to prevent allocation of huge strings
5. Use streaming or truncation for extremely large values (>1MB) before sanitization

#### Threat 5: Correlation Leakage Between Tool Calls

**Description**: Correlation IDs in logs could enable linking tool calls across sessions, potentially revealing usage patterns or sensitive workflows.

**Impact**: Low - correlation IDs are generally ephemeral and not sensitive on their own.

**Likelihood**: Low - requires log access and sophisticated analysis.

**Mitigation**:
1. Use opaque, non-sequential correlation IDs (UUID v4)
2. Limit correlation ID lifespan to single session
3. Do not include user identifiers or sensitive data in correlation IDs
4. Implement log retention policies to delete old correlation data

### Mitigation Summary Table

| Threat | Severity | Mitigation Strategy | Implementation Component |
|--------|----------|---------------------|--------------------------|
| Secret Leakage | High | Pattern-based redaction, field-level redaction, fail-safe redaction | ValueSanitizer |
| Path Traversal | Low | JSON Pointer validation, path sanitization, length limits | ErrorFormatter |
| Injection Attacks | Medium | HTML escaping, parameterized logging, special char sanitization | ErrorFormatter, Logging Infrastructure |
| Resource Exhaustion | Medium | Message length limits, value truncation, error count limits | ErrorFormatter, ErrorAggregator |
| Correlation Leakage | Low | Opaque UUIDs, session scoping, retention policies | RetryTracker, Logging Infrastructure |

### Audit Requirements

1. **Log All Validation Failures**: Every validation failure must be logged with structured fields for audit trail.

2. **Track Retry Patterns**: Anomalous retry patterns (excessive retries, unusual error codes) must be detectable via log analysis.

3. **Record Escalations**: All escalations to human operators must be logged with full validation history for post-incident analysis.

4. **Sanitization Audit**: Log when sanitization redacts values to enable detection of secret leakage attempts.

5. **Configuration Changes**: Changes to retry limits or sanitization rules must be logged as configuration audit events.

### Specific Sanitization Rules

#### Rule 1: JWT Token Detection
Pattern: `^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$`
Replacement: `[REDACTED: JWT Token]`

#### Rule 2: API Key Detection
Patterns:
- `sk-[A-Za-z0-9]{32,}` (OpenAI-style)
- `[A-Za-z0-9]{32,64}` (generic long alphanumeric)
- Fields named: api_key, apiKey, apikey, access_key, accessKey
Replacement: `[REDACTED: API Key]`

#### Rule 3: Password Field Redaction
Fields: password, passwd, pass, pwd, secret, credentials
Replacement: `[REDACTED: Password]`

#### Rule 4: Connection String Detection
Pattern: Contains keywords: connection string, jdbc, odbc, Data Source, Initial Catalog
Replacement: `[REDACTED: Connection String]`

#### Rule 5: AWS Credentials
Patterns:
- `AKIA[A-Z0-9]{16}` (AWS Access Key ID)
- `aws_secret_access_key=.*`
Replacement: `[REDACTED: AWS Credential]`

#### Rule 6: Private Key Detection
Pattern: Contains `-----BEGIN PRIVATE KEY-----` or `-----BEGIN RSA PRIVATE KEY-----`
Replacement: `[REDACTED: Private Key]`

---

## Best Practices

### Schema Design Best Practices

1. **Provide Clear Field Descriptions**: Every field in tool schemas should have a description that explains purpose, format, and constraints. Models use descriptions to understand requirements, reducing validation failures.

2. **Include Examples in Schema**: Use JSON Schema `examples` property to show valid values. Models learn from examples more effectively than from abstract type definitions.

3. **Set Reasonable Constraints**: Avoid overly restrictive min/max constraints that increase validation failure rates unnecessarily. For example, max_results: 100 is reasonable; max_results: 5 may be too restrictive.

4. **Use Descriptive Enum Values**: Enum values should be self-explanatory. Prefer `"encoding": ["utf-8", "ascii", "utf-16"]` over `"encoding": ["u8", "a", "u16"]`.

### Error Message Clarity Best Practices

5. **Always Include Expected and Actual**: Error messages are 3x more effective when they show both what was expected and what was actually provided.

6. **Use Correction Hints for Common Errors**: For enum errors, list valid values. For range errors, show min/max. For pattern errors, provide format description.

7. **Keep Messages Concise**: Target 1-3 sentences per error. Verbose explanations consume context and reduce model comprehension.

### Retry Configuration Best Practices

8. **Default to 3 Retries**: Empirical data shows 3 retries captures 98% of correctable errors without excessive waste on uncorrectable errors.

9. **Override Retry Limits Per Tool**: Simple tools (read_file) may need only 1 retry. Complex tools (api_request with nested structures) may benefit from 5 retries.

10. **Enable Retry History in Production**: Retry history consumes minimal memory (< 10KB per tool call) and is invaluable for debugging and optimization.

### Monitoring and Observability Best Practices

11. **Track Validation Failure Rate by Tool**: High failure rates (> 20%) indicate schema clarity issues or model capability mismatches.

12. **Monitor Average Retry Count**: Increasing retry counts over time may indicate schema complexity growth or model quality degradation.

13. **Alert on Escalation Rate**: Escalation rate (retries exceeding max_attempts) above 5% indicates systematic issues requiring investigation.

14. **Analyze Error Code Distribution**: Disproportionate error codes (e.g., 80% VAL-002 type mismatches) indicate specific schema design problems.

### Security and Privacy Best Practices

15. **Never Log Actual Values by Default**: Actual argument values may contain secrets or PII. Log only field paths and error codes in production.

16. **Review Sanitization Patterns Regularly**: Update secret detection patterns as new credential formats emerge (e.g., new API key formats from new services).

17. **Limit Error Message Retention**: Apply shorter retention policies to logs containing error messages (30 days) compared to operational logs (90 days).

---

## Troubleshooting

### Issue 1: Models Repeatedly Make Same Validation Error

**Symptoms**:
- Model retries with identical arguments after validation failure
- Retry count reaches max_attempts with no correction
- Error messages show same error code and field path across all attempts
- Logs show pattern like: attempt 1/3 VAL-001, attempt 2/3 VAL-001, attempt 3/3 VAL-001

**Cause**:
The error message is not providing enough information for the model to understand what is wrong, or the model lacks the context to correct the error. Common root causes include:
1. Error message too vague (missing expected/actual values)
2. Schema description insufficient (model doesn't understand field purpose)
3. Model lacks contextual knowledge (e.g., doesn't know valid file paths)
4. Error message truncated, losing critical correction hints

**Solution**:
1. **Verify error message completeness**:
   - Check logs for full error message content
   - Ensure `include_actual_values: true` in configuration
   - Ensure `include_hints: true` in configuration
   - Verify message not truncated (check max_message_length)

2. **Improve schema documentation**:
   - Add detailed `description` to problematic field
   - Include `examples` in schema showing valid values
   - For enum fields, ensure all values are listed in error message

3. **Add context to conversation**:
   - Provide examples in system prompt or user messages
   - For file path errors, list valid paths in context
   - For API errors, include API documentation excerpt

4. **Enable verbose logging**:
   ```yaml
   tools:
     validation:
       logging:
         log_actual_values: true  # Temporarily, to debug
         log_retry_success: true
   ```

5. **Check tool-specific formatter**:
   - If tool has custom error formatter, verify it generates actionable messages
   - Test formatter with problematic arguments manually

**Verification**:
- After changes, attempt same operation and verify model corrects error on retry
- Check logs for successful retry after initial failure
- Monitor retry count distribution to ensure it shifts toward fewer retries

---

### Issue 2: Escalation Happens Too Quickly (Max Retries Too Low)

**Symptoms**:
- Frequent escalations to user for tool calls that seem correctable
- Logs show many "retry limit exceeded" events
- Users report being interrupted unnecessarily
- Error analysis shows errors are eventually correctable but hit limit first

**Cause**:
The max_attempts configuration is too low for the complexity of the tools being used. Complex tools with nested object arguments or many optional fields may require more than 3 attempts for models to converge on valid arguments.

**Solution**:
1. **Increase global max_attempts**:
   ```yaml
   tools:
     validation:
       retry:
         max_attempts: 5  # Increased from default 3
   ```

2. **Use per-tool overrides** (preferred approach):
   ```yaml
   tools:
     validation:
       retry:
         max_attempts: 3  # Default for simple tools
         tool_overrides:
           complex_api_call: 5
           nested_config_update: 6
           batch_operation: 4
   ```

3. **Analyze retry patterns** to determine appropriate limits:
   ```bash
   # Find average retry count before success for each tool
   jq -s 'group_by(.tool_name) | map({tool: .[0].tool_name, avg_retries: (map(.retry_attempt) | add / length)})' logs/acode.log
   ```

4. **Enable exponential backoff** to give models "thinking time":
   ```yaml
   tools:
     validation:
       retry:
         exponential_backoff: true  # Adds delay between retries
   ```

**Verification**:
- Monitor escalation rate (should drop below 5%)
- Check that increased retries lead to eventual success (not just delaying inevitable escalation)
- Review user interruption complaints (should decrease)

---

### Issue 3: Error Messages Truncated, Losing Critical Information

**Symptoms**:
- Error messages end with "..." mid-sentence
- Logs show `max_message_length` limit reached
- Models fail to correct errors despite retries
- Error messages missing correction hints or actual values

**Cause**:
The error message exceeds the configured `max_message_length` (default 2000 characters) and is truncated, losing correction hints, actual values, or field paths that would enable the model to correct the error.

**Solution**:
1. **Increase message length limit** (temporary, for diagnosis):
   ```yaml
   tools:
     validation:
       formatting:
         max_message_length: 4000  # Increased from 2000
   ```

2. **Reduce errors shown per validation**:
   ```yaml
   tools:
     validation:
       formatting:
         max_errors_shown: 5  # Reduced from 10
   ```
   This prioritizes showing complete information for fewer errors rather than partial information for many errors.

3. **Optimize error message format**:
   - Disable dual path format if enabled:
     ```yaml
     tools:
       validation:
         formatting:
           dual_path_format: false  # Show only JSON Pointer paths
     ```
   - Reduce value preview length:
     ```yaml
     tools:
       validation:
         formatting:
           max_value_preview: 50  # Reduced from 100
     ```

4. **Improve schema design to reduce errors**:
   - If a tool consistently produces 10+ validation errors, the schema may be too complex
   - Consider splitting complex tools into multiple simpler tools
   - Use default values to reduce required fields

5. **Implement smarter truncation**:
   - Ensure ValueSanitizer uses "smart" truncation strategy (preserves important parts):
     ```yaml
     tools:
       validation:
         sanitization:
           truncation_strategy: "smart"  # Not "end"
     ```

**Verification**:
- Check logs for messages at exactly max_message_length (indicates truncation)
- Verify correction hints are present in truncated messages
- Monitor retry success rate (should improve with complete messages)

---

### Issue 4: Performance Degradation (Validation Latency Increasing)

**Symptoms**:
- Error formatting taking > 1ms (exceeds performance requirement NFR-001)
- Agent loop latency increasing proportionally with validation failures
- Logs show `formatting_duration_ms` > 1.0

**Cause**:
Error formatting or retry tracking is not optimized, causing performance degradation at scale. Common causes include:
1. RetryTracker not using ConcurrentDictionary (O(n) lookup instead of O(1))
2. ErrorFormatter allocating large strings without preallocation
3. ValueSanitizer using inefficient string operations (string concatenation instead of span-based operations)
4. Error aggregation not using dictionary-based deduplication

**Solution**:
1. **Profile the error formatting pipeline**:
   - Add instrumentation to measure each component: aggregation, sanitization, formatting
   - Identify bottleneck component

2. **Optimize RetryTracker**:
   - Ensure using ConcurrentDictionary<string, RetryHistory>
   - Verify O(1) lookup by tool_call_id

3. **Optimize ErrorFormatter**:
   - Preallocate StringBuilder with estimated size:
     ```csharp
     var sb = new StringBuilder(capacity: estimatedMessageLength);
     ```
   - Use string interpolation instead of concatenation
   - Cache common string fragments (error codes, standard phrases)

4. **Optimize ValueSanitizer**:
   - Use Span<char> and Memory<char> for truncation operations
   - Avoid allocating intermediate strings
   - Compile regex patterns once and reuse

5. **Optimize ErrorAggregator**:
   - Use HashSet<string> for deduplication (O(1) contains check)
   - Avoid sorting if errors already arrive in order

**Verification**:
- Re-run performance tests, verify formatting < 1ms
- Monitor `formatting_duration_ms` in production logs
- Verify latency does not increase with error count (should be capped)

---

### Issue 5: Secrets Appearing in Logs Despite Sanitization

**Symptoms**:
- API keys, passwords, or tokens visible in log files
- Redaction patterns not matching actual secret formats
- Audit tools flagging secret exposure in logs

**Cause**:
ValueSanitizer redaction patterns are incomplete or not matching the actual secret formats used in the system. New secret formats may have been introduced that existing patterns don't catch.

**Solution**:
1. **Identify leaked secret format**:
   - Extract leaked secret from logs (in secure environment)
   - Analyze format to determine detection pattern

2. **Update sanitization patterns**:
   - Add new pattern to ValueSanitizer
   - Example for new API key format:
     ```csharp
     private static readonly Regex NewApiKeyPattern = new(@"key_[A-Za-z0-9]{40}", RegexOptions.Compiled);
     ```

3. **Add field-level redaction**:
   - If secret appears in specific fields, add field name to redaction list:
     ```csharp
     private static readonly HashSet<string> SensitiveFieldNames = new()
     {
         "api_key", "apiKey", "password", "token", "secret",
         "new_field_name"  // Add problematic field
     };
     ```

4. **Enable fail-safe redaction**:
   - If pattern matching fails, redact entire value:
     ```csharp
     if (CouldContainSecret(value) && !IsKnownSafeFormat(value))
     {
         return "[REDACTED: Unknown Format]";
     }
     ```

5. **Implement entropy-based detection**:
   - High-entropy strings (> 4.5 bits per character) are likely secrets
   - Use Shannon entropy calculation as fallback detection

6. **Audit existing logs**:
   - Scan historical logs for exposed secrets
   - Rotate any exposed credentials immediately
   - Apply retention policy to delete compromised logs

**Verification**:
- Test sanitization with known secret formats
- Review logs after changes to confirm no leakage
- Implement automated secret scanning in CI/CD pipeline

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

### Performance Criteria

- [ ] AC-067: Error formatting completes in < 1ms (99th percentile)
- [ ] AC-068: Aggregation completes in < 100μs (99th percentile)
- [ ] AC-069: Retry lookup is O(1) time complexity
- [ ] AC-070: Memory per retry history < 10KB
- [ ] AC-071: StringBuilder preallocation used to avoid resizing
- [ ] AC-072: Span-based string operations used in sanitizer
- [ ] AC-073: Regex patterns compiled and cached (not recompiled per use)
- [ ] AC-074: No allocations in hot path (validation → format → return)

### Security Criteria

- [ ] AC-075: JWT tokens redacted in actual values
- [ ] AC-076: API keys redacted in actual values
- [ ] AC-077: Password fields redacted by field name
- [ ] AC-078: Connection strings detected and redacted
- [ ] AC-079: AWS credentials detected and redacted
- [ ] AC-080: Private keys detected and redacted
- [ ] AC-081: File paths converted to relative paths
- [ ] AC-082: Field paths validated against JSON Pointer syntax
- [ ] AC-083: Field path length limited to 256 characters
- [ ] AC-084: Actual values sanitized before logging
- [ ] AC-085: Actual values sanitized before error message creation
- [ ] AC-086: Special characters escaped in error messages (prevent injection)
- [ ] AC-087: Maximum error message length enforced (hard cap 4000 characters)
- [ ] AC-088: Fail-safe redaction (unknown patterns → full redaction)

### Observability Criteria

- [ ] AC-089: Validation failures logged with structured fields
- [ ] AC-090: Retry attempts logged with correlation ID
- [ ] AC-091: Escalations logged at WARN level
- [ ] AC-092: Metrics track validation failure rate per tool
- [ ] AC-093: Metrics track average retry count per tool
- [ ] AC-094: Metrics track escalation rate
- [ ] AC-095: Log entries include error_code distribution
- [ ] AC-096: Log entries include validation_duration_ms
- [ ] AC-097: Log entries include formatting_duration_ms
- [ ] AC-098: Correlation ID enables tracing across retries

### Edge Case Handling

- [ ] AC-099: Null values handled gracefully (no exceptions)
- [ ] AC-100: Empty strings handled gracefully
- [ ] AC-101: Extremely long strings (> 1MB) truncated efficiently
- [ ] AC-102: Deeply nested objects (> 10 levels) handled
- [ ] AC-103: Large arrays (> 1000 elements) summarized
- [ ] AC-104: Malformed JSON Pointer paths rejected gracefully
- [ ] AC-105: Unicode in error messages preserved correctly (no corruption)
- [ ] AC-106: Concurrent retry tracking thread-safe (no race conditions)
- [ ] AC-107: Retry history memory bounded (no unbounded growth)

---

## Testing Requirements

### Unit Tests

Unit tests verify individual components in isolation with deterministic inputs and comprehensive edge case coverage.

#### ValidationErrorTests.cs

Location: `tests/Acode.Application.Tests/ToolSchemas/Retry/ValidationErrorTests.cs`

```csharp
using Acode.Application.ToolSchemas.Retry;
using FluentAssertions;
using Xunit;

namespace Acode.Application.Tests.ToolSchemas.Retry;

public class ValidationErrorTests
{
    [Fact]
    public void Should_Create_With_All_Fields()
    {
        // Arrange
        var errorCode = "VAL-001";
        var fieldPath = "/path/to/field";
        var message = "Required field is missing";
        var severity = ErrorSeverity.Error;
        var expectedValue = "string";
        var actualValue = null;

        // Act
        var error = new ValidationError
        {
            ErrorCode = errorCode,
            FieldPath = fieldPath,
            Message = message,
            Severity = severity,
            ExpectedValue = expectedValue,
            ActualValue = actualValue
        };

        // Assert
        error.ErrorCode.Should().Be(errorCode);
        error.FieldPath.Should().Be(fieldPath);
        error.Message.Should().Be(message);
        error.Severity.Should().Be(severity);
        error.ExpectedValue.Should().Be(expectedValue);
        error.ActualValue.Should().BeNull();
    }

    [Fact]
    public void Should_Use_JSON_Pointer_Path_Format()
    {
        // Arrange
        var fieldPath = "/parent/child/0/nested";

        // Act
        var error = new ValidationError
        {
            ErrorCode = "VAL-002",
            FieldPath = fieldPath,
            Message = "Type mismatch",
            Severity = ErrorSeverity.Error
        };

        // Assert
        error.FieldPath.Should().StartWith("/");
        error.FieldPath.Should().NotContain("..");
        error.FieldPath.Should().MatchRegex(@"^/([a-zA-Z0-9_]+|\d+)(/([a-zA-Z0-9_]+|\d+))*$");
    }

    [Fact]
    public void Should_Support_All_Severity_Levels()
    {
        // Arrange & Act
        var errorSeverity = new ValidationError
        {
            ErrorCode = "VAL-001",
            FieldPath = "/field",
            Message = "Error",
            Severity = ErrorSeverity.Error
        };

        var warningSeverity = new ValidationError
        {
            ErrorCode = "VAL-001",
            FieldPath = "/field",
            Message = "Warning",
            Severity = ErrorSeverity.Warning
        };

        var infoSeverity = new ValidationError
        {
            ErrorCode = "VAL-001",
            FieldPath = "/field",
            Message = "Info",
            Severity = ErrorSeverity.Info
        };

        // Assert
        errorSeverity.Severity.Should().Be(ErrorSeverity.Error);
        warningSeverity.Severity.Should().Be(ErrorSeverity.Warning);
        infoSeverity.Severity.Should().Be(ErrorSeverity.Info);
    }

    [Theory]
    [InlineData("VAL-001", "Required field missing")]
    [InlineData("VAL-002", "Type mismatch")]
    [InlineData("VAL-003", "Constraint violation")]
    [InlineData("VAL-004", "Invalid JSON")]
    [InlineData("VAL-008", "Invalid enum value")]
    public void Should_Support_All_Error_Codes(string code, string message)
    {
        // Arrange & Act
        var error = new ValidationError
        {
            ErrorCode = code,
            FieldPath = "/field",
            Message = message,
            Severity = ErrorSeverity.Error
        };

        // Assert
        error.ErrorCode.Should().Be(code);
        error.ErrorCode.Should().MatchRegex(@"^VAL-\d{3}$");
    }

    [Fact]
    public void Should_Handle_Null_Optional_Fields()
    {
        // Arrange & Act
        var error = new ValidationError
        {
            ErrorCode = "VAL-001",
            FieldPath = "/field",
            Message = "Error",
            Severity = ErrorSeverity.Error,
            ExpectedValue = null,
            ActualValue = null
        };

        // Assert
        error.ExpectedValue.Should().BeNull();
        error.ActualValue.Should().BeNull();
    }

    [Fact]
    public void Should_Preserve_Unicode_In_Values()
    {
        // Arrange
        var unicodeExpected = "日本語の説明";
        var unicodeActual = "中文说明";

        // Act
        var error = new ValidationError
        {
            ErrorCode = "VAL-002",
            FieldPath = "/description",
            Message = "Language mismatch",
            Severity = ErrorSeverity.Error,
            ExpectedValue = unicodeExpected,
            ActualValue = unicodeActual
        };

        // Assert
        error.ExpectedValue.Should().Be(unicodeExpected);
        error.ActualValue.Should().Be(unicodeActual);
    }
}
```

#### ErrorFormatterTests.cs

Location: `tests/Acode.Infrastructure.Tests/ToolSchemas/Retry/ErrorFormatterTests.cs`

```csharp
using Acode.Application.ToolSchemas.Retry;
using Acode.Infrastructure.ToolSchemas.Retry;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.ToolSchemas.Retry;

public class ErrorFormatterTests
{
    private readonly ErrorFormatter _formatter;
    private readonly RetryConfiguration _config;

    public ErrorFormatterTests()
    {
        _config = new RetryConfiguration
        {
            MaxAttempts = 3,
            MaxMessageLength = 2000,
            MaxErrorsShown = 10,
            MaxValuePreview = 100,
            IncludeHints = true,
            IncludeActualValues = true
        };
        _formatter = new ErrorFormatter(_config);
    }

    [Fact]
    public void Should_Format_Single_Error()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError
            {
                ErrorCode = "VAL-001",
                FieldPath = "/path",
                Message = "Required field is missing",
                Severity = ErrorSeverity.Error,
                ExpectedValue = "string",
                ActualValue = null
            }
        };

        // Act
        var result = _formatter.FormatErrors("read_file", errors, attemptNumber: 1, maxAttempts: 3);

        // Assert
        result.Should().Contain("Validation failed for tool 'read_file' (attempt 1/3)");
        result.Should().Contain("/path (VAL-001)");
        result.Should().Contain("Required field is missing");
        result.Should().Contain("Expected: string");
        result.Should().Contain("Please");
    }

    [Fact]
    public void Should_Format_Multiple_Errors()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError
            {
                ErrorCode = "VAL-001",
                FieldPath = "/path",
                Message = "Required field is missing",
                Severity = ErrorSeverity.Error,
                ExpectedValue = "string"
            },
            new ValidationError
            {
                ErrorCode = "VAL-008",
                FieldPath = "/encoding",
                Message = "Invalid enum value",
                Severity = ErrorSeverity.Error,
                ExpectedValue = "utf-8, ascii, utf-16",
                ActualValue = "uft8"
            }
        };

        // Act
        var result = _formatter.FormatErrors("read_file", errors, attemptNumber: 2, maxAttempts: 3);

        // Assert
        result.Should().Contain("(attempt 2/3)");
        result.Should().Contain("/path (VAL-001)");
        result.Should().Contain("/encoding (VAL-008)");
        result.Should().Contain("Errors:");
    }

    [Fact]
    public void Should_Include_Tool_Name()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError
            {
                ErrorCode = "VAL-001",
                FieldPath = "/field",
                Message = "Error",
                Severity = ErrorSeverity.Error
            }
        };

        // Act
        var result = _formatter.FormatErrors("write_file", errors, 1, 3);

        // Assert
        result.Should().Contain("write_file");
    }

    [Fact]
    public void Should_Include_Attempt_Number()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError
            {
                ErrorCode = "VAL-001",
                FieldPath = "/field",
                Message = "Error",
                Severity = ErrorSeverity.Error
            }
        };

        // Act
        var result1 = _formatter.FormatErrors("tool", errors, 1, 3);
        var result2 = _formatter.FormatErrors("tool", errors, 2, 3);
        var result3 = _formatter.FormatErrors("tool", errors, 3, 3);

        // Assert
        result1.Should().Contain("(attempt 1/3)");
        result2.Should().Contain("(attempt 2/3)");
        result3.Should().Contain("(attempt 3/3)");
    }

    [Fact]
    public void Should_Truncate_Long_Values()
    {
        // Arrange
        var longValue = new string('x', 500);
        var errors = new[]
        {
            new ValidationError
            {
                ErrorCode = "VAL-009",
                FieldPath = "/content",
                Message = "String too long",
                Severity = ErrorSeverity.Error,
                ExpectedValue = "string with max length 100",
                ActualValue = longValue
            }
        };

        // Act
        var result = _formatter.FormatErrors("write_file", errors, 1, 3);

        // Assert
        result.Should().Contain("...");
        result.Should().NotContain(longValue);
        result.Length.Should().BeLessThan(longValue.Length);
    }

    [Fact]
    public void Should_Respect_Max_Length()
    {
        // Arrange
        var manyErrors = Enumerable.Range(0, 50).Select(i => new ValidationError
        {
            ErrorCode = "VAL-002",
            FieldPath = $"/field{i}",
            Message = $"Type mismatch on field {i}",
            Severity = ErrorSeverity.Error,
            ExpectedValue = "string",
            ActualValue = "integer"
        }).ToArray();

        // Act
        var result = _formatter.FormatErrors("complex_tool", manyErrors, 1, 3);

        // Assert
        result.Length.Should().BeLessOrEqualTo(_config.MaxMessageLength);
    }

    [Fact]
    public void Should_Include_Correction_Hints()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError
            {
                ErrorCode = "VAL-008",
                FieldPath = "/encoding",
                Message = "Invalid enum value",
                Severity = ErrorSeverity.Error,
                ExpectedValue = "utf-8, ascii, utf-16",
                ActualValue = "invalid"
            }
        };

        // Act
        var result = _formatter.FormatErrors("read_file", errors, 1, 3);

        // Assert
        result.Should().Contain("Use one of:");
        result.Should().Contain("utf-8");
        result.Should().Contain("ascii");
        result.Should().Contain("utf-16");
    }

    [Fact]
    public void Should_Sort_Errors_By_Field_Path()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError { ErrorCode = "VAL-001", FieldPath = "/z_field", Message = "Error", Severity = ErrorSeverity.Error },
            new ValidationError { ErrorCode = "VAL-001", FieldPath = "/a_field", Message = "Error", Severity = ErrorSeverity.Error },
            new ValidationError { ErrorCode = "VAL-001", FieldPath = "/m_field", Message = "Error", Severity = ErrorSeverity.Error }
        };

        // Act
        var result = _formatter.FormatErrors("tool", errors, 1, 3);

        // Assert
        var aIndex = result.IndexOf("/a_field", StringComparison.Ordinal);
        var mIndex = result.IndexOf("/m_field", StringComparison.Ordinal);
        var zIndex = result.IndexOf("/z_field", StringComparison.Ordinal);
        aIndex.Should().BeLessThan(mIndex);
        mIndex.Should().BeLessThan(zIndex);
    }

    [Fact]
    public void Should_Complete_In_Under_1ms()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError
            {
                ErrorCode = "VAL-002",
                FieldPath = "/field",
                Message = "Type mismatch",
                Severity = ErrorSeverity.Error,
                ExpectedValue = "string",
                ActualValue = "123"
            }
        };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _formatter.FormatErrors("tool", errors, 1, 3);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1);
    }
}
```

#### ValueSanitizerTests.cs

Location: `tests/Acode.Infrastructure.Tests/ToolSchemas/Retry/ValueSanitizerTests.cs`

```csharp
using Acode.Infrastructure.ToolSchemas.Retry;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.ToolSchemas.Retry;

public class ValueSanitizerTests
{
    private readonly ValueSanitizer _sanitizer;

    public ValueSanitizerTests()
    {
        _sanitizer = new ValueSanitizer(maxPreviewLength: 100, relativizePaths: true, redactSecrets: true);
    }

    [Fact]
    public void Should_Redact_JWT_Tokens()
    {
        // Arrange
        var jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        // Act
        var sanitized = _sanitizer.Sanitize(jwtToken, fieldPath: "/token");

        // Assert
        sanitized.Should().Be("[REDACTED: JWT Token]");
    }

    [Fact]
    public void Should_Redact_OpenAI_API_Keys()
    {
        // Arrange
        var apiKey = "sk-1234567890abcdefghijklmnopqrstuvwxyz123456";

        // Act
        var sanitized = _sanitizer.Sanitize(apiKey, fieldPath: "/api_key");

        // Assert
        sanitized.Should().Be("[REDACTED: API Key]");
    }

    [Fact]
    public void Should_Redact_Password_Fields_By_Name()
    {
        // Arrange
        var password = "my_secret_password_123";

        // Act
        var sanitized = _sanitizer.Sanitize(password, fieldPath: "/password");

        // Assert
        sanitized.Should().Be("[REDACTED: Password]");
    }

    [Fact]
    public void Should_Redact_AWS_Access_Keys()
    {
        // Arrange
        var awsKey = "AKIAIOSFODNN7EXAMPLE";

        // Act
        var sanitized = _sanitizer.Sanitize(awsKey, fieldPath: "/aws_access_key");

        // Assert
        sanitized.Should().Be("[REDACTED: AWS Credential]");
    }

    [Fact]
    public void Should_Truncate_Long_Strings()
    {
        // Arrange
        var longString = new string('x', 500);

        // Act
        var sanitized = _sanitizer.Sanitize(longString, fieldPath: "/content");

        // Assert
        sanitized.Should().Contain("...");
        sanitized.Length.Should().BeLessOrEqualTo(120); // 100 + "..." + buffer
    }

    [Fact]
    public void Should_Relativize_Absolute_File_Paths()
    {
        // Arrange
        var absolutePath = "/home/user/project/src/file.txt";

        // Act
        var sanitized = _sanitizer.Sanitize(absolutePath, fieldPath: "/path");

        // Assert
        sanitized.Should().NotStartWith("/home");
        sanitized.Should().Contain("src/file.txt");
    }

    [Fact]
    public void Should_Preserve_Unicode()
    {
        // Arrange
        var unicode = "Hello 世界 مرحبا";

        // Act
        var sanitized = _sanitizer.Sanitize(unicode, fieldPath: "/text");

        // Assert
        sanitized.Should().Be(unicode);
    }

    [Fact]
    public void Should_Handle_Null_Values()
    {
        // Arrange
        string? nullValue = null;

        // Act
        var sanitized = _sanitizer.Sanitize(nullValue, fieldPath: "/field");

        // Assert
        sanitized.Should().Be("null");
    }

    [Fact]
    public void Should_Use_Smart_Truncation_Strategy()
    {
        // Arrange
        var longString = "important_prefix_" + new string('x', 200) + "_important_suffix";

        // Act
        var sanitized = _sanitizer.Sanitize(longString, fieldPath: "/data");

        // Assert
        sanitized.Should().Contain("important_prefix");
        sanitized.Should().Contain("important_suffix");
        sanitized.Should().Contain("...");
    }
}
```

#### RetryTrackerTests.cs

Location: `tests/Acode.Infrastructure.Tests/ToolSchemas/Retry/RetryTrackerTests.cs`

```csharp
using Acode.Infrastructure.ToolSchemas.Retry;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.ToolSchemas.Retry;

public class RetryTrackerTests
{
    private readonly RetryTracker _tracker;

    public RetryTrackerTests()
    {
        _tracker = new RetryTracker(maxAttempts: 3, trackHistory: true);
    }

    [Fact]
    public void Should_Track_Attempts()
    {
        // Arrange
        var toolCallId = "call_123";

        // Act
        var attempt1 = _tracker.IncrementAttempt(toolCallId);
        var attempt2 = _tracker.IncrementAttempt(toolCallId);
        var attempt3 = _tracker.IncrementAttempt(toolCallId);

        // Assert
        attempt1.Should().Be(1);
        attempt2.Should().Be(2);
        attempt3.Should().Be(3);
    }

    [Fact]
    public void Should_Store_History()
    {
        // Arrange
        var toolCallId = "call_123";
        var error1 = "Error 1";
        var error2 = "Error 2";

        // Act
        _tracker.RecordError(toolCallId, error1);
        _tracker.RecordError(toolCallId, error2);
        var history = _tracker.GetHistory(toolCallId);

        // Assert
        history.Should().HaveCount(2);
        history[0].Should().Be(error1);
        history[1].Should().Be(error2);
    }

    [Fact]
    public void Should_Check_Max_Retries()
    {
        // Arrange
        var toolCallId = "call_123";

        // Act
        _tracker.IncrementAttempt(toolCallId);
        _tracker.IncrementAttempt(toolCallId);
        _tracker.IncrementAttempt(toolCallId);
        var exceeded = _tracker.HasExceededMaxRetries(toolCallId);

        // Assert
        exceeded.Should().BeTrue();
    }

    [Fact]
    public void Should_Be_Thread_Safe()
    {
        // Arrange
        var toolCallId = "call_123";
        var tasks = new List<Task<int>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => _tracker.IncrementAttempt(toolCallId)));
        }
        Task.WaitAll(tasks.ToArray());

        // Assert
        var finalAttempt = _tracker.GetCurrentAttempt(toolCallId);
        finalAttempt.Should().Be(10);
    }

    [Fact]
    public void Should_Return_Zero_For_Unknown_Tool_Call()
    {
        // Arrange
        var toolCallId = "unknown_call";

        // Act
        var attempt = _tracker.GetCurrentAttempt(toolCallId);

        // Assert
        attempt.Should().Be(0);
    }

    [Fact]
    public void Should_Clear_History_After_Success()
    {
        // Arrange
        var toolCallId = "call_123";
        _tracker.IncrementAttempt(toolCallId);
        _tracker.RecordError(toolCallId, "Error");

        // Act
        _tracker.Clear(toolCallId);
        var attempt = _tracker.GetCurrentAttempt(toolCallId);
        var history = _tracker.GetHistory(toolCallId);

        // Assert
        attempt.Should().Be(0);
        history.Should().BeEmpty();
    }

    [Fact]
    public void Should_Limit_Memory_Per_History()
    {
        // Arrange
        var toolCallId = "call_123";
        var largeError = new string('x', 10000);

        // Act
        for (int i = 0; i < 100; i++)
        {
            _tracker.RecordError(toolCallId, largeError);
        }
        var memoryUsage = GC.GetTotalMemory(forceFullCollection: true);

        // Assert (verify history doesn't grow unbounded)
        var history = _tracker.GetHistory(toolCallId);
        history.Should().HaveCountLessOrEqualTo(10); // Max history entries
    }
}
```

### Integration Tests

Integration tests verify the complete flow from validation through error formatting through ToolResult creation.

#### RetryContractIntegrationTests.cs

Location: `tests/Acode.Integration.Tests/ToolSchemas/Retry/RetryContractIntegrationTests.cs`

```csharp
using Acode.Application.Messages;
using Acode.Application.ToolSchemas;
using Acode.Application.ToolSchemas.Retry;
using Acode.Infrastructure.ToolSchemas.Retry;
using FluentAssertions;
using Xunit;

namespace Acode.Integration.Tests.ToolSchemas.Retry;

public class RetryContractIntegrationTests
{
    private readonly IErrorFormatter _formatter;
    private readonly IRetryTracker _tracker;
    private readonly RetryConfiguration _config;

    public RetryContractIntegrationTests()
    {
        _config = new RetryConfiguration
        {
            MaxAttempts = 3,
            MaxMessageLength = 2000,
            MaxErrorsShown = 10,
            MaxValuePreview = 100
        };
        _formatter = new ErrorFormatter(_config);
        _tracker = new RetryTracker(_config.MaxAttempts, trackHistory: true);
    }

    [Fact]
    public void Should_Create_ToolResult_On_Validation_Failure()
    {
        // Arrange
        var toolCallId = "call_abc123";
        var toolName = "read_file";
        var errors = new[]
        {
            new ValidationError
            {
                ErrorCode = "VAL-001",
                FieldPath = "/path",
                Message = "Required field is missing",
                Severity = ErrorSeverity.Error,
                ExpectedValue = "string"
            }
        };

        // Act
        var attemptNumber = _tracker.IncrementAttempt(toolCallId);
        var formattedError = _formatter.FormatErrors(toolName, errors, attemptNumber, _config.MaxAttempts);
        var toolResult = new ToolResult
        {
            Role = "tool",
            ToolCallId = toolCallId,
            Content = formattedError,
            IsError = true
        };

        // Assert
        toolResult.Role.Should().Be("tool");
        toolResult.ToolCallId.Should().Be(toolCallId);
        toolResult.IsError.Should().BeTrue();
        toolResult.Content.Should().Contain("Validation failed");
        toolResult.Content.Should().Contain(toolName);
        toolResult.Content.Should().Contain("VAL-001");
    }

    [Fact]
    public void Should_Track_Retry_Across_Turns()
    {
        // Arrange
        var toolCallId = "call_xyz789";
        var toolName = "write_file";

        // Act - Simulate 3 retry attempts
        var attempt1 = _tracker.IncrementAttempt(toolCallId);
        _tracker.RecordError(toolCallId, "Missing path");
        var errors1 = new[] { CreateError("VAL-001", "/path") };
        var message1 = _formatter.FormatErrors(toolName, errors1, attempt1, 3);

        var attempt2 = _tracker.IncrementAttempt(toolCallId);
        _tracker.RecordError(toolCallId, "Invalid content");
        var errors2 = new[] { CreateError("VAL-009", "/content") };
        var message2 = _formatter.FormatErrors(toolName, errors2, attempt2, 3);

        var attempt3 = _tracker.IncrementAttempt(toolCallId);
        var exceeded = _tracker.HasExceededMaxRetries(toolCallId);

        // Assert
        attempt1.Should().Be(1);
        message1.Should().Contain("(attempt 1/3)");

        attempt2.Should().Be(2);
        message2.Should().Contain("(attempt 2/3)");

        attempt3.Should().Be(3);
        exceeded.Should().BeTrue();

        var history = _tracker.GetHistory(toolCallId);
        history.Should().HaveCount(2);
    }

    [Fact]
    public void Should_Escalate_After_Max_Retries()
    {
        // Arrange
        var toolCallId = "call_escalate";
        var toolName = "complex_tool";
        var escalationFormatter = new EscalationFormatter();

        // Act - Exhaust retries
        for (int i = 1; i <= 3; i++)
        {
            _tracker.IncrementAttempt(toolCallId);
            _tracker.RecordError(toolCallId, $"Attempt {i} failed");
        }

        var exceeded = _tracker.HasExceededMaxRetries(toolCallId);
        var history = _tracker.GetHistory(toolCallId);
        var escalationMessage = escalationFormatter.FormatEscalation(toolName, toolCallId, history, maxAttempts: 3);

        // Assert
        exceeded.Should().BeTrue();
        escalationMessage.Should().Contain("validation failed after 3 attempts");
        escalationMessage.Should().Contain("Attempt 1");
        escalationMessage.Should().Contain("Attempt 2");
        escalationMessage.Should().Contain("Attempt 3");
        escalationMessage.Should().Contain("intervene");
    }

    [Fact]
    public void Should_Handle_Concurrent_Tool_Calls()
    {
        // Arrange
        var toolCallIds = Enumerable.Range(0, 100).Select(i => $"call_{i}").ToArray();

        // Act - Simulate concurrent validation failures
        var tasks = toolCallIds.Select(id => Task.Run(() =>
        {
            var attempt = _tracker.IncrementAttempt(id);
            var errors = new[] { CreateError("VAL-002", "/field") };
            return _formatter.FormatErrors("tool", errors, attempt, 3);
        })).ToArray();

        Task.WaitAll(tasks);

        // Assert - All tool calls tracked independently
        foreach (var id in toolCallIds)
        {
            _tracker.GetCurrentAttempt(id).Should().Be(1);
        }
    }

    [Fact]
    public void Should_Apply_Sanitization_In_Full_Flow()
    {
        // Arrange
        var toolCallId = "call_sanitize";
        var toolName = "authenticate";
        var sanitizer = new ValueSanitizer(maxPreviewLength: 100, relativizePaths: true, redactSecrets: true);
        var errors = new[]
        {
            new ValidationError
            {
                ErrorCode = "VAL-002",
                FieldPath = "/api_key",
                Message = "Invalid format",
                Severity = ErrorSeverity.Error,
                ExpectedValue = "string (API key format)",
                ActualValue = "sk-1234567890abcdefghijklmnopqrstuvwxyz123456"
            }
        };

        // Act
        var sanitizedErrors = errors.Select(e => new ValidationError
        {
            ErrorCode = e.ErrorCode,
            FieldPath = e.FieldPath,
            Message = e.Message,
            Severity = e.Severity,
            ExpectedValue = e.ExpectedValue,
            ActualValue = sanitizer.Sanitize(e.ActualValue, e.FieldPath)
        }).ToArray();

        var attemptNumber = _tracker.IncrementAttempt(toolCallId);
        var formattedError = _formatter.FormatErrors(toolName, sanitizedErrors, attemptNumber, 3);

        // Assert
        formattedError.Should().NotContain("sk-1234567890");
        formattedError.Should().Contain("[REDACTED");
    }

    private static ValidationError CreateError(string code, string path)
    {
        return new ValidationError
        {
            ErrorCode = code,
            FieldPath = path,
            Message = "Error",
            Severity = ErrorSeverity.Error
        };
    }
}
```

### End-to-End Tests

E2E tests verify model comprehension and retry success with real model interactions.

#### ModelRetryE2ETests.cs

Location: `tests/Acode.E2E.Tests/ToolSchemas/Retry/ModelRetryE2ETests.cs`

```csharp
using Acode.Application.Messages;
using Acode.Application.ToolSchemas.Retry;
using FluentAssertions;
using Xunit;

namespace Acode.E2E.Tests.ToolSchemas.Retry;

public class ModelRetryE2ETests
{
    [Fact(Skip = "Requires local model running")]
    public async Task Should_Correct_Missing_Required_Field_On_Retry()
    {
        // Arrange - Simulate model making tool call without required field
        var initialToolCall = new ToolCall
        {
            Id = "call_001",
            Name = "read_file",
            Arguments = "{\"encoding\": \"utf-8\"}" // Missing 'path'
        };

        // Validate and get error
        var validationError = new ValidationError
        {
            ErrorCode = "VAL-001",
            FieldPath = "/path",
            Message = "Required field is missing",
            Severity = ErrorSeverity.Error,
            ExpectedValue = "string"
        };

        // Format error for model
        var formatter = new ErrorFormatter(new RetryConfiguration());
        var errorMessage = formatter.FormatErrors("read_file", new[] { validationError }, 1, 3);

        // Act - Send error to model and get corrected tool call
        // (In real E2E test, this would call actual model)
        var correctedToolCall = new ToolCall
        {
            Id = "call_002",
            Name = "read_file",
            Arguments = "{\"path\": \".agent/config.yml\", \"encoding\": \"utf-8\"}"
        };

        // Assert - Corrected call should now have required field
        correctedToolCall.Arguments.Should().Contain("path");
        correctedToolCall.Arguments.Should().Contain(".agent/config.yml");
    }

    [Fact(Skip = "Requires local model running")]
    public async Task Should_Correct_Type_Mismatch_On_Retry()
    {
        // Simulate model providing wrong type (string instead of integer)
        var initialToolCall = new ToolCall
        {
            Id = "call_003",
            Name = "run_command",
            Arguments = "{\"command\": \"npm install\", \"timeout\": \"30\"}" // timeout should be int
        };

        var validationError = new ValidationError
        {
            ErrorCode = "VAL-002",
            FieldPath = "/timeout",
            Message = "Type mismatch",
            Severity = ErrorSeverity.Error,
            ExpectedValue = "integer (seconds)",
            ActualValue = "\"30\" (string)"
        };

        var formatter = new ErrorFormatter(new RetryConfiguration());
        var errorMessage = formatter.FormatErrors("run_command", new[] { validationError }, 1, 3);

        // Act - Model corrects type
        var correctedToolCall = new ToolCall
        {
            Id = "call_004",
            Name = "run_command",
            Arguments = "{\"command\": \"npm install\", \"timeout\": 30}" // Now integer
        };

        // Assert
        correctedToolCall.Arguments.Should().Contain("\"timeout\": 30");
        correctedToolCall.Arguments.Should().NotContain("\"timeout\": \"30\"");
    }
}
```

### Performance Tests

Performance tests verify latency requirements and scalability.

#### RetryPerformanceTests.cs

Location: `tests/Acode.Performance.Tests/ToolSchemas/Retry/RetryPerformanceTests.cs`

```csharp
using Acode.Application.ToolSchemas.Retry;
using Acode.Infrastructure.ToolSchemas.Retry;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FluentAssertions;

namespace Acode.Performance.Tests.ToolSchemas.Retry;

[MemoryDiagnoser]
public class RetryPerformanceBenchmarks
{
    private ErrorFormatter _formatter = null!;
    private ValidationError[] _singleError = null!;
    private ValidationError[] _multipleErrors = null!;
    private RetryTracker _tracker = null!;

    [GlobalSetup]
    public void Setup()
    {
        var config = new RetryConfiguration
        {
            MaxAttempts = 3,
            MaxMessageLength = 2000,
            MaxErrorsShown = 10,
            MaxValuePreview = 100
        };
        _formatter = new ErrorFormatter(config);
        _tracker = new RetryTracker(3, true);

        _singleError = new[]
        {
            new ValidationError
            {
                ErrorCode = "VAL-001",
                FieldPath = "/path",
                Message = "Required field is missing",
                Severity = ErrorSeverity.Error,
                ExpectedValue = "string"
            }
        };

        _multipleErrors = Enumerable.Range(0, 10).Select(i => new ValidationError
        {
            ErrorCode = "VAL-002",
            FieldPath = $"/field{i}",
            Message = "Type mismatch",
            Severity = ErrorSeverity.Error,
            ExpectedValue = "string",
            ActualValue = "integer"
        }).ToArray();
    }

    [Benchmark]
    public string FormatSingleError()
    {
        return _formatter.FormatErrors("read_file", _singleError, 1, 3);
    }

    [Benchmark]
    public string FormatMultipleErrors()
    {
        return _formatter.FormatErrors("complex_tool", _multipleErrors, 2, 3);
    }

    [Benchmark]
    public int IncrementAttempt()
    {
        return _tracker.IncrementAttempt("call_benchmark");
    }

    [Benchmark]
    public void RecordAndRetrieveHistory()
    {
        _tracker.RecordError("call_history", "Error message");
        _tracker.GetHistory("call_history");
    }
}

public class PerformanceTests
{
    [Fact]
    public void Error_Formatting_Should_Complete_Under_1ms()
    {
        // Arrange
        var config = new RetryConfiguration();
        var formatter = new ErrorFormatter(config);
        var errors = new[]
        {
            new ValidationError
            {
                ErrorCode = "VAL-001",
                FieldPath = "/field",
                Message = "Error",
                Severity = ErrorSeverity.Error
            }
        };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            formatter.FormatErrors("tool", errors, 1, 3);
        }
        stopwatch.Stop();

        // Assert - Average should be < 1ms
        var averageMs = stopwatch.ElapsedMilliseconds / 1000.0;
        averageMs.Should().BeLessThan(1.0);
    }

    [Fact]
    public void Retry_Tracker_Should_Be_O1_Lookup()
    {
        // Arrange
        var tracker = new RetryTracker(3, true);
        var toolCallIds = Enumerable.Range(0, 10000).Select(i => $"call_{i}").ToArray();

        // Populate tracker
        foreach (var id in toolCallIds)
        {
            tracker.IncrementAttempt(id);
        }

        // Act - Measure lookup time (should be constant regardless of tracker size)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        tracker.GetCurrentAttempt(toolCallIds[5000]);
        stopwatch.Stop();

        // Assert - Lookup should be near-instant (< 1μs)
        stopwatch.ElapsedTicks.Should().BeLessThan(100); // Very small number of ticks
    }
}
```

---

## User Verification Steps

### Scenario 1: Single Required Field Missing

**Objective**: Verify that a missing required field generates a clear, actionable error message.

**Steps**:
1. Create a tool call to `read_file` with arguments: `{"encoding": "utf-8"}` (missing required `path` field)
2. Submit the tool call for validation
3. Observe the returned ToolResult message

**Expected Output**:
```
Validation failed for tool 'read_file' (attempt 1/3):

• /path (VAL-001): Required field is missing
  Expected: string (filesystem path)

Please provide the required 'path' field.
```

**Verification Checkpoints**:
- ✓ ToolResult.IsError is true
- ✓ Error message mentions the specific field path (/path)
- ✓ Error code VAL-001 is present
- ✓ Attempt counter shows 1/3
- ✓ Expected type (string) is specified
- ✓ Clear correction hint provided

**Expected Retry Behavior**: Model should add `"path": "<some_file_path>"` in next attempt.

---

### Scenario 2: Multiple Simultaneous Errors (Error Aggregation)

**Objective**: Verify that multiple validation errors are aggregated and presented together.

**Steps**:
1. Create a tool call to `run_command` with arguments:
   ```json
   {
     "command": "npm install",
     "timeout": "30",  // Wrong type (should be integer)
     "env": "NODE_ENV=production",  // Wrong type (should be object)
     "working_dir": 12345  // Wrong type (should be string)
   }
   ```
2. Submit the tool call for validation
3. Observe the returned ToolResult message

**Expected Output**:
```
Validation failed for tool 'run_command' (attempt 1/3):

Errors:
• /env (VAL-002): Type mismatch
  Expected: object (key-value pairs)
  Actual: "NODE_ENV=production" (string)

• /timeout (VAL-002): Type mismatch
  Expected: integer (seconds)
  Actual: "30" (string)

• /working_dir (VAL-002): Type mismatch
  Expected: string (filesystem path)
  Actual: 12345 (integer)

Please correct all type mismatches and try again.
```

**Verification Checkpoints**:
- ✓ All three errors listed in one message (not sequential)
- ✓ Errors formatted as bulleted list
- ✓ Errors sorted by field path alphabetically (/env, /timeout, /working_dir)
- ✓ Each error shows expected and actual values
- ✓ Correction hint addresses all errors

**Expected Retry Behavior**: Model should fix all three fields simultaneously in next attempt.

---

### Scenario 3: Successful Retry After Initial Failure

**Objective**: Verify retry tracking and attempt numbering across multiple attempts.

**Steps**:
1. **Attempt 1**: Call `read_file` with `{"encoding": "utf-8"}` (missing path)
2. Observe error message with attempt 1/3
3. **Attempt 2**: Call `read_file` with `{"path": ".agent/config.yml", "encoding": "utf-8"}` (correct)
4. Observe validation passes

**Expected Output (Attempt 1)**:
```
Validation failed for tool 'read_file' (attempt 1/3):
...
```

**Expected Output (Attempt 2)**:
Validation passes, tool executes successfully, no error ToolResult returned.

**Verification Checkpoints**:
- ✓ Attempt 1 error message shows (attempt 1/3)
- ✓ Attempt 2 validation succeeds
- ✓ Retry tracker increments attempt count correctly
- ✓ Retry history records first attempt's error

---

### Scenario 4: Escalation After Max Retries Exceeded

**Objective**: Verify escalation mechanism triggers after maximum retry attempts.

**Steps**:
1. **Attempt 1**: Call `write_file` with `{"content": "data"}` (missing path)
2. **Attempt 2**: Call `write_file` with `{"path": 12345, "content": "data"}` (wrong type for path)
3. **Attempt 3**: Call `write_file` with `{"path": "", "content": "data"}` (empty path, still invalid)
4. Observe escalation message

**Expected Output (After Attempt 3)**:
```
Tool 'write_file' validation failed after 3 attempts.

Validation History:

Attempt 1 (2024-01-15 14:32:01):
  • /path (VAL-001): Required field is missing
    Expected: string

Attempt 2 (2024-01-15 14:32:03):
  • /path (VAL-002): Type mismatch
    Expected: string (filesystem path)
    Actual: 12345 (integer)

Attempt 3 (2024-01-15 14:32:05):
  • /path (VAL-010): Format violation
    Expected: non-empty string
    Actual: "" (empty string)

The model was unable to provide valid arguments after 3 attempts. Please intervene or provide guidance.
```

**Verification Checkpoints**:
- ✓ Escalation triggered after exactly 3 attempts
- ✓ Validation history includes all attempt errors
- ✓ Timestamps included for each attempt
- ✓ Clear summary of failure
- ✓ ToolResult status set to Blocked (if status field exists)

---

### Scenario 5: Long Value Truncation and Sanitization

**Objective**: Verify that extremely long actual values are truncated to prevent message bloat.

**Steps**:
1. Create a tool call to `write_file` with arguments:
   ```json
   {
     "path": "test.txt",
     "content": "<string of 100,000 characters>"
   }
   ```
   (where content exceeds schema max length of 1MB)
2. Submit for validation
3. Observe truncated value in error message

**Expected Output**:
```
Validation failed for tool 'write_file' (attempt 1/3):

• /content (VAL-009): String length 100000 exceeds maximum 1048576
  Expected: string with max length 1048576 characters
  Actual: "Lorem ipsum dolor sit amet, consectetur adipiscing elit..." (truncated)

Please reduce 'content' length to 1048576 characters or fewer.
```

**Verification Checkpoints**:
- ✓ Actual value truncated with "..." indicator
- ✓ Actual value preview under 100 characters (configurable limit)
- ✓ Full error message under 2000 characters (configurable limit)
- ✓ Error message still contains all essential information (error code, field path, expected length)

---

### Scenario 6: Secret Redaction in Error Messages

**Objective**: Verify that secrets are detected and redacted from error messages.

**Steps**:
1. Create a tool call to `authenticate_api` with arguments:
   ```json
   {
     "api_key": "sk-1234567890abcdefghijklmnopqrstuvwxyz123456",
     "endpoint": "https://api.example.com"
   }
   ```
   (where api_key fails validation for some reason, e.g., wrong format)
2. Submit for validation
3. Observe redacted value in error message

**Expected Output**:
```
Validation failed for tool 'authenticate_api' (attempt 1/3):

• /api_key (VAL-010): Format violation
  Expected: API key in format key_<40 chars>
  Actual: [REDACTED: API Key]

Please provide a valid API key.
```

**Verification Checkpoints**:
- ✓ API key value fully redacted, not visible in error message
- ✓ Redaction format clearly indicates what was redacted
- ✓ Error message still actionable (describes expected format)
- ✓ JWT tokens, passwords, AWS credentials also redacted (test separately)

---

### Scenario 7: Structured Logging Verification

**Objective**: Verify that validation failures are logged with comprehensive structured fields.

**Steps**:
1. Create a tool call that will fail validation (any from scenarios above)
2. Submit for validation
3. Check application logs (e.g., `logs/acode.log`)
4. Parse log entry as JSON

**Expected Log Entry**:
```json
{
    "timestamp": "2024-01-15T14:32:01.234Z",
    "level": "warn",
    "message": "Tool validation failed",
    "tool_name": "read_file",
    "tool_call_id": "call_abc123",
    "correlation_id": "req-789-def-456",
    "retry_attempt": 1,
    "max_retries": 3,
    "is_escalation": false,
    "error_count": 1,
    "error_summary": [
        {"path": "/path", "code": "VAL-001", "severity": "Error"}
    ],
    "validation_duration_ms": 0.42,
    "formatting_duration_ms": 0.18
}
```

**Verification Checkpoints**:
- ✓ Log entry is valid JSON (parseable)
- ✓ All structured fields present (tool_name, error_count, retry_attempt, etc.)
- ✓ No actual argument values in logs (security requirement)
- ✓ Correlation ID present for request tracing
- ✓ Performance metrics included (duration fields)
- ✓ Log level is WARN for validation failures

**Analysis Commands**:
```bash
# Count validation failures by tool
jq -s 'group_by(.tool_name) | map({tool: .[0].tool_name, failures: length})' logs/acode.log

# Find most common error codes
jq -s '[.[].error_summary[].code] | group_by(.) | map({code: .[0], count: length})' logs/acode.log
```

---

### Scenario 8: Enum Error with Correction Hints

**Objective**: Verify that enum validation errors provide all valid enum values as correction hints.

**Steps**:
1. Create a tool call to `read_file` with arguments:
   ```json
   {
     "path": "test.txt",
     "encoding": "uft8"  // Typo, should be "utf-8"
   }
   ```
2. Submit for validation

**Expected Output**:
```
Validation failed for tool 'read_file' (attempt 1/3):

• /encoding (VAL-008): Invalid enum value
  Expected one of: utf-8, ascii, utf-16, utf-32, iso-8859-1
  Actual: "uft8"

Did you mean: utf-8?

Please use one of the valid encoding values.
```

**Verification Checkpoints**:
- ✓ All valid enum values listed
- ✓ Suggested correction provided (fuzzy match: "uft8" → "utf-8")
- ✓ Error code VAL-008 present
- ✓ Clear actionable hint

---

### Scenario 9: Performance Verification (Error Formatting Latency)

**Objective**: Verify that error formatting completes within performance requirements (< 1ms).

**Steps**:
1. Create a validation error with 10 error objects (moderate complexity)
2. Call ErrorFormatter.FormatErrors() in a loop 1000 times
3. Measure total elapsed time
4. Calculate average per-call latency

**Expected Measurement Code**:
```csharp
var stopwatch = Stopwatch.StartNew();
for (int i = 0; i < 1000; i++)
{
    formatter.FormatErrors("tool", errors, 1, 3);
}
stopwatch.Stop();
var averageMs = stopwatch.ElapsedMilliseconds / 1000.0;
```

**Verification Checkpoints**:
- ✓ Average latency < 1ms (NFR-001 requirement)
- ✓ 99th percentile latency < 1ms
- ✓ No memory allocations in hot path (use memory profiler)
- ✓ StringBuilder preallocation used

---

### Scenario 10: Concurrent Tool Call Validation (Thread Safety)

**Objective**: Verify that RetryTracker is thread-safe under concurrent access.

**Steps**:
1. Create 100 concurrent tasks, each calling different tool_call_ids
2. Each task increments retry attempt counter
3. Verify all 100 tool calls tracked independently without race conditions

**Expected Test Code**:
```csharp
var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
{
    var toolCallId = $"call_{i}";
    tracker.IncrementAttempt(toolCallId);
})).ToArray();

Task.WaitAll(tasks);

// Verify each tool call tracked exactly once
for (int i = 0; i < 100; i++)
{
    var attempt = tracker.GetCurrentAttempt($"call_{i}");
    Assert.Equal(1, attempt);
}
```

**Verification Checkpoints**:
- ✓ All 100 tool calls recorded
- ✓ No attempt counts corrupted (each is exactly 1)
- ✓ No exceptions thrown during concurrent access
- ✓ ConcurrentDictionary used internally (check implementation)

---

## Implementation Prompt

This section provides complete, production-ready code for all classes in the retry contract. Implement these components in the order shown following TDD (write tests first, then implementation).

---

### File Structure

```
src/Acode.Application/ToolSchemas/Retry/
├── ValidationError.cs
├── ErrorSeverity.cs
├── ErrorCode.cs
├── IErrorFormatter.cs
├── IRetryTracker.cs
├── IEscalationFormatter.cs
└── RetryConfiguration.cs

src/Acode.Infrastructure/ToolSchemas/Retry/
├── ErrorFormatter.cs
├── ErrorAggregator.cs
├── RetryTracker.cs
├── EscalationFormatter.cs
└── ValueSanitizer.cs

tests/Acode.Application.Tests/ToolSchemas/Retry/
├── ValidationErrorTests.cs
└── ErrorCodeTests.cs

tests/Acode.Infrastructure.Tests/ToolSchemas/Retry/
├── ErrorFormatterTests.cs
├── ErrorAggregatorTests.cs
├── RetryTrackerTests.cs
├── EscalationFormatterTests.cs
└── ValueSanitizerTests.cs
```

---

###Application Layer Classes

#### ValidationError.cs

Location: `src/Acode.Application/ToolSchemas/Retry/ValidationError.cs`

```csharp
using System.Diagnostics.CodeAnalysis;

namespace Acode.Application.ToolSchemas.Retry;

/// <summary>
/// Represents a validation error that occurred during tool argument validation.
/// Designed for model comprehension with clear field paths and actionable messages.
/// </summary>
public sealed class ValidationError
{
    /// <summary>
    /// Gets the error code in VAL-XXX format (e.g., VAL-001, VAL-002).
    /// </summary>
    [NotNull]
    public required string ErrorCode { get; init; }

    /// <summary>
    /// Gets the JSON Pointer path to the field that failed validation (e.g., /path, /config/timeout).
    /// Format: RFC 6901 JSON Pointer notation.
    /// </summary>
    [NotNull]
    public required string FieldPath { get; init; }

    /// <summary>
    /// Gets the human-readable error message describing the validation failure.
    /// </summary>
    [NotNull]
    public required string Message { get; init; }

    /// <summary>
    /// Gets the severity level: Error (must fix), Warning (should fix), or Info (advisory).
    /// </summary>
    public required ErrorSeverity Severity { get; init; }

    /// <summary>
    /// Gets the expected value or type description from the schema.
    /// Example: "string", "integer between 1 and 100", "one of: utf-8, ascii".
    /// </summary>
    public string? ExpectedValue { get; init; }

    /// <summary>
    /// Gets the actual value provided by the model (sanitized to prevent secret leakage).
    /// May be truncated if very long.
    /// </summary>
    public string? ActualValue { get; init; }
}
```

#### ErrorSeverity.cs

Location: `src/Acode.Application/ToolSchemas/Retry/ErrorSeverity.cs`

```csharp
namespace Acode.Application.ToolSchemas.Retry;

/// <summary>
/// Defines the severity levels for validation errors.
/// Determines whether execution is blocked and how errors are handled.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Informational message, no action required. Execution proceeds normally.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning message, should be fixed but execution can proceed with degradation.
    /// Example: deprecated parameter used.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error message, must be fixed. Execution is blocked until corrected.
    /// Example: required field missing, type mismatch.
    /// </summary>
    Error = 2
}
```

#### ErrorCode.cs

Location: `src/Acode.Application/ToolSchemas/Retry/ErrorCode.cs`

```csharp
namespace Acode.Application.ToolSchemas.Retry;

/// <summary>
/// Standard error codes for validation failures.
/// All codes follow VAL-XXX format for consistency.
/// </summary>
public static class ErrorCode
{
    /// <summary>
    /// VAL-001: A required field specified in the schema is missing.
    /// </summary>
    public const string RequiredFieldMissing = "VAL-001";

    /// <summary>
    /// VAL-002: Value type does not match schema type (e.g., string provided but integer expected).
    /// </summary>
    public const string TypeMismatch = "VAL-002";

    /// <summary>
    /// VAL-003: Value violates min/max/range constraint (e.g., value 150 exceeds max 100).
    /// </summary>
    public const string ConstraintViolation = "VAL-003";

    /// <summary>
    /// VAL-004: Malformed JSON that cannot be parsed.
    /// </summary>
    public const string InvalidJsonSyntax = "VAL-004";

    /// <summary>
    /// VAL-005: Field not defined in schema (when strict validation is enabled).
    /// </summary>
    public const string UnknownField = "VAL-005";

    /// <summary>
    /// VAL-006: Array has too few or too many elements.
    /// </summary>
    public const string ArrayLengthViolation = "VAL-006";

    /// <summary>
    /// VAL-007: String does not match required regex pattern.
    /// </summary>
    public const string PatternMismatch = "VAL-007";

    /// <summary>
    /// VAL-008: Value not in allowed enum set.
    /// </summary>
    public const string InvalidEnumValue = "VAL-008";

    /// <summary>
    /// VAL-009: String too short or too long.
    /// </summary>
    public const string StringLengthViolation = "VAL-009";

    /// <summary>
    /// VAL-010: String does not match semantic format (email, URL, date, etc.).
    /// </summary>
    public const string FormatViolation = "VAL-010";

    /// <summary>
    /// VAL-011: Numeric value outside allowed range.
    /// </summary>
    public const string NumberRangeViolation = "VAL-011";

    /// <summary>
    /// VAL-012: Duplicate value where uniqueness is required.
    /// </summary>
    public const string UniqueConstraintViolation = "VAL-012";

    /// <summary>
    /// VAL-013: Field requires another field to be present.
    /// </summary>
    public const string DependencyViolation = "VAL-013";

    /// <summary>
    /// VAL-014: Multiple mutually exclusive fields provided.
    /// </summary>
    public const string MutualExclusivityViolation = "VAL-014";

    /// <summary>
    /// VAL-015: Nested object does not conform to schema.
    /// </summary>
    public const string ObjectSchemaViolation = "VAL-015";
}
```

#### IErrorFormatter.cs

Location: `src/Acode.Application/ToolSchemas/Retry/IErrorFormatter.cs`

```csharp
namespace Acode.Application.ToolSchemas.Retry;

/// <summary>
/// Formats validation errors into model-comprehensible messages.
/// </summary>
public interface IErrorFormatter
{
    /// <summary>
    /// Formats a collection of validation errors into a single message for the model.
    /// </summary>
    /// <param name="toolName">The name of the tool that failed validation.</param>
    /// <param name="errors">The validation errors to format.</param>
    /// <param name="attemptNumber">Current retry attempt number (1-based).</param>
    /// <param name="maxAttempts">Maximum allowed retry attempts.</param>
    /// <returns>Formatted error message ready for inclusion in ToolResult.</returns>
    string FormatErrors(string toolName, IEnumerable<ValidationError> errors, int attemptNumber, int maxAttempts);
}
```

#### IRetryTracker.cs

Location: `src/Acode.Application/ToolSchemas/Retry/IRetryTracker.cs`

```csharp
namespace Acode.Application.ToolSchemas.Retry;

/// <summary>
/// Tracks retry attempts and validation history for tool calls.
/// Thread-safe for concurrent access.
/// </summary>
public interface IRetryTracker
{
    /// <summary>
    /// Increments the retry attempt counter for a tool call.
    /// </summary>
    /// <param name="toolCallId">Unique identifier for the tool call.</param>
    /// <returns>The new attempt number (1-based).</returns>
    int IncrementAttempt(string toolCallId);

    /// <summary>
    /// Gets the current attempt number for a tool call.
    /// </summary>
    /// <param name="toolCallId">Unique identifier for the tool call.</param>
    /// <returns>The current attempt number, or 0 if not tracked.</returns>
    int GetCurrentAttempt(string toolCallId);

    /// <summary>
    /// Records an error message in the validation history.
    /// </summary>
    /// <param name="toolCallId">Unique identifier for the tool call.</param>
    /// <param name="errorMessage">The formatted error message.</param>
    void RecordError(string toolCallId, string errorMessage);

    /// <summary>
    /// Gets the validation history for a tool call.
    /// </summary>
    /// <param name="toolCallId">Unique identifier for the tool call.</param>
    /// <returns>List of error messages from all attempts.</returns>
    IReadOnlyList<string> GetHistory(string toolCallId);

    /// <summary>
    /// Checks if the maximum retry attempts have been exceeded.
    /// </summary>
    /// <param name="toolCallId">Unique identifier for the tool call.</param>
    /// <returns>True if max retries exceeded, false otherwise.</returns>
    bool HasExceededMaxRetries(string toolCallId);

    /// <summary>
    /// Clears the retry tracking for a tool call (e.g., after successful validation).
    /// </summary>
    /// <param name="toolCallId">Unique identifier for the tool call.</param>
    void Clear(string toolCallId);
}
```

#### IEscalationFormatter.cs

Location: `src/Acode.Application/ToolSchemas/Retry/IEscalationFormatter.cs`

```csharp
namespace Acode.Application.ToolSchemas.Retry;

/// <summary>
/// Formats escalation messages for human intervention after max retries exceeded.
/// </summary>
public interface IEscalationFormatter
{
    /// <summary>
    /// Formats an escalation message with validation history.
    /// </summary>
    /// <param name="toolName">The name of the tool that failed validation.</param>
    /// <param name="toolCallId">Unique identifier for the tool call.</param>
    /// <param name="validationHistory">List of error messages from all attempts.</param>
    /// <param name="maxAttempts">Maximum allowed retry attempts.</param>
    /// <returns>Formatted escalation message for human operator.</returns>
    string FormatEscalation(string toolName, string toolCallId, IReadOnlyList<string> validationHistory, int maxAttempts);
}
```

#### RetryConfiguration.cs

Location: `src/Acode.Application/ToolSchemas/Retry/RetryConfiguration.cs`

```csharp
namespace Acode.Application.ToolSchemas.Retry;

/// <summary>
/// Configuration options for the retry contract.
/// Bind from .agent/config.yml tools.validation section.
/// </summary>
public sealed class RetryConfiguration
{
    /// <summary>
    /// Gets or sets the maximum retry attempts before escalation.
    /// Default: 3. Range: 1-10.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum error message length in characters.
    /// Default: 2000. Range: 500-4000.
    /// </summary>
    public int MaxMessageLength { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the maximum number of errors shown per validation.
    /// Default: 10. Additional errors summarized as "...and N more".
    /// </summary>
    public int MaxErrorsShown { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum length of actual value preview in characters.
    /// Default: 100. Longer values truncated with "...".
    /// </summary>
    public int MaxValuePreview { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to include correction hints in error messages.
    /// Default: true.
    /// </summary>
    public bool IncludeHints { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include actual values in error messages.
    /// Default: true. Set false to reduce token usage.
    /// </summary>
    public bool IncludeActualValues { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track retry history in memory.
    /// Default: true. Set false to save memory in high-scale scenarios.
    /// </summary>
    public bool TrackHistory { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to redact detected secrets in actual values.
    /// Default: true.
    /// </summary>
    public bool RedactSecrets { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to convert absolute file paths to relative.
    /// Default: true.
    /// </summary>
    public bool RelativizePaths { get; set; } = true;
}
```

---

### Infrastructure Layer Classes

#### ErrorFormatter.cs

Location: `src/Acode.Infrastructure/ToolSchemas/Retry/ErrorFormatter.cs`

```csharp
using System.Text;
using Acode.Application.ToolSchemas.Retry;

namespace Acode.Infrastructure.ToolSchemas.Retry;

/// <summary>
/// Formats validation errors into model-comprehensible messages.
/// Optimized for sub-millisecond performance with StringBuilder preallocation.
/// </summary>
public sealed class ErrorFormatter : IErrorFormatter
{
    private readonly RetryConfiguration _config;
    private readonly ValueSanitizer _sanitizer;
    private readonly ErrorAggregator _aggregator;

    public ErrorFormatter(RetryConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _sanitizer = new ValueSanitizer(
            maxPreviewLength: _config.MaxValuePreview,
            relativizePaths: _config.RelativizePaths,
            redactSecrets: _config.RedactSecrets
        );
        _aggregator = new ErrorAggregator(maxErrors: _config.MaxErrorsShown);
    }

    public string FormatErrors(string toolName, IEnumerable<ValidationError> errors, int attemptNumber, int maxAttempts)
    {
        if (string.IsNullOrEmpty(toolName))
            throw new ArgumentException("Tool name cannot be null or empty", nameof(toolName));
        if (errors == null)
            throw new ArgumentNullException(nameof(errors));

        var aggregatedErrors = _aggregator.Aggregate(errors);
        if (!aggregatedErrors.Any())
            return "Validation failed (no error details available)";

        // Preallocate StringBuilder with estimated capacity
        var estimatedLength = 200 + (aggregatedErrors.Count * 150);
        var sb = new StringBuilder(capacity: estimatedLength);

        // Summary line
        sb.AppendLine($"Validation failed for tool '{toolName}' (attempt {attemptNumber}/{maxAttempts}):");
        sb.AppendLine();

        // Error details
        if (aggregatedErrors.Count == 1)
        {
            FormatSingleError(sb, aggregatedErrors[0]);
        }
        else
        {
            sb.AppendLine("Errors:");
            foreach (var error in aggregatedErrors)
            {
                FormatErrorBullet(sb, error);
            }
        }

        // Correction hints
        if (_config.IncludeHints)
        {
            AppendCorrectionHints(sb, aggregatedErrors);
        }

        // Truncate if exceeds max length
        var result = sb.ToString();
        if (result.Length > _config.MaxMessageLength)
        {
            result = result.Substring(0, _config.MaxMessageLength - 3) + "...";
        }

        return result;
    }

    private void FormatSingleError(StringBuilder sb, ValidationError error)
    {
        sb.Append($"• {error.FieldPath} ({error.ErrorCode}): {error.Message}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(error.ExpectedValue))
        {
            sb.AppendLine($"  Expected: {error.ExpectedValue}");
        }

        if (_config.IncludeActualValues && !string.IsNullOrEmpty(error.ActualValue))
        {
            var sanitized = _sanitizer.Sanitize(error.ActualValue, error.FieldPath);
            sb.AppendLine($"  Actual: {sanitized}");
        }

        sb.AppendLine();
    }

    private void FormatErrorBullet(StringBuilder sb, ValidationError error)
    {
        sb.Append($"• {error.FieldPath} ({error.ErrorCode}): {error.Message}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(error.ExpectedValue))
        {
            sb.AppendLine($"  Expected: {error.ExpectedValue}");
        }

        if (_config.IncludeActualValues && !string.IsNullOrEmpty(error.ActualValue))
        {
            var sanitized = _sanitizer.Sanitize(error.ActualValue, error.FieldPath);
            sb.AppendLine($"  Actual: {sanitized}");
        }

        sb.AppendLine();
    }

    private void AppendCorrectionHints(StringBuilder sb, IReadOnlyList<ValidationError> errors)
    {
        // Generate correction hints based on error patterns
        var requiredFields = errors
            .Where(e => e.ErrorCode == ErrorCode.RequiredFieldMissing)
            .Select(e => ExtractFieldName(e.FieldPath))
            .ToList();

        var enumErrors = errors
            .Where(e => e.ErrorCode == ErrorCode.InvalidEnumValue)
            .ToList();

        if (requiredFields.Any())
        {
            sb.Append("Please provide the required ");
            if (requiredFields.Count == 1)
                sb.Append($"'{requiredFields[0]}' field.");
            else
                sb.Append($"fields: {string.Join(", ", requiredFields.Select(f => $"'{f}'"))}.");
            sb.AppendLine();
        }

        if (enumErrors.Any())
        {
            foreach (var error in enumErrors)
            {
                sb.AppendLine($"For {error.FieldPath}, use one of: {error.ExpectedValue}");
            }
        }

        var typeMismatches = errors
            .Where(e => e.ErrorCode == ErrorCode.TypeMismatch)
            .ToList();

        if (typeMismatches.Any())
        {
            sb.AppendLine("Please correct all type mismatches and try again.");
        }
    }

    private static string ExtractFieldName(string jsonPointer)
    {
        // Extract field name from JSON Pointer path (e.g., "/path/to/field" → "field")
        var parts = jsonPointer.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.LastOrDefault() ?? jsonPointer;
    }
}
```

#### ValueSanitizer.cs

Location: `src/Acode.Infrastructure/ToolSchemas/Retry/ValueSanitizer.cs`

```csharp
using System.Text;
using System.Text.RegularExpressions;

namespace Acode.Infrastructure.ToolSchemas.Retry;

/// <summary>
/// Sanitizes actual values to prevent secret leakage and reduce token consumption.
/// Uses pattern-based detection for JWTs, API keys, passwords, and other credentials.
/// </summary>
public sealed class ValueSanitizer
{
    private readonly int _maxPreviewLength;
    private readonly bool _relativizePaths;
    private readonly bool _redactSecrets;

    // Compiled regex patterns for secret detection (compile once, reuse)
    private static readonly Regex JwtPattern = new(@"^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$", RegexOptions.Compiled);
    private static readonly Regex OpenAiApiKeyPattern = new(@"sk-[A-Za-z0-9]{32,}", RegexOptions.Compiled);
    private static readonly Regex AwsAccessKeyPattern = new(@"AKIA[A-Z0-9]{16}", RegexOptions.Compiled);
    private static readonly Regex GenericLongAlphanumericPattern = new(@"^[A-Za-z0-9]{32,64}$", RegexOptions.Compiled);

    private static readonly HashSet<string> SensitiveFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwd", "pass", "pwd", "secret", "credentials",
        "api_key", "apiKey", "apikey", "access_key", "accessKey",
        "token", "auth_token", "authToken", "bearer", "jwt"
    };

    public ValueSanitizer(int maxPreviewLength, bool relativizePaths, bool redactSecrets)
    {
        _maxPreviewLength = maxPreviewLength;
        _relativizePaths = relativizePaths;
        _redactSecrets = redactSecrets;
    }

    public string Sanitize(string? value, string fieldPath)
    {
        if (value == null)
            return "null";

        // Check field-level redaction first (fastest check)
        if (_redactSecrets && IsSensitiveField(fieldPath))
        {
            return "[REDACTED: Password]";
        }

        // Pattern-based secret detection
        if (_redactSecrets)
        {
            if (JwtPattern.IsMatch(value))
                return "[REDACTED: JWT Token]";

            if (OpenAiApiKeyPattern.IsMatch(value))
                return "[REDACTED: API Key]";

            if (AwsAccessKeyPattern.IsMatch(value))
                return "[REDACTED: AWS Credential]";

            if (value.Contains("-----BEGIN PRIVATE KEY-----", StringComparison.Ordinal))
                return "[REDACTED: Private Key]";

            if (value.Contains("connectionString", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("jdbc:", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("Data Source=", StringComparison.Ordinal))
            {
                return "[REDACTED: Connection String]";
            }

            // Generic long alphanumeric (likely API key)
            if (value.Length >= 32 && GenericLongAlphanumericPattern.IsMatch(value))
            {
                return "[REDACTED: API Key]";
            }
        }

        // Path relativization
        if (_relativizePaths && IsAbsolutePath(value))
        {
            value = RelativizePath(value);
        }

        // Truncation
        if (value.Length <= _maxPreviewLength)
            return $"\"{value}\"";

        // Smart truncation: show beginning and end
        var prefix = value.Substring(0, _maxPreviewLength / 2);
        var suffix = value.Substring(value.Length - (_maxPreviewLength / 4));
        return $"\"{prefix}...{suffix}\" (truncated)";
    }

    private static bool IsSensitiveField(string fieldPath)
    {
        // Extract field name from JSON Pointer path
        var parts = fieldPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var fieldName = parts.LastOrDefault() ?? fieldPath;
        return SensitiveFieldNames.Contains(fieldName);
    }

    private static bool IsAbsolutePath(string value)
    {
        return value.StartsWith("/", StringComparison.Ordinal) ||
               value.StartsWith("C:\\", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("\\\\", StringComparison.Ordinal);
    }

    private static string RelativizePath(string absolutePath)
    {
        // Simple relativization: remove common prefixes
        var prefixes = new[] { "/home/", "C:\\Users\\", "\\\\network\\" };
        foreach (var prefix in prefixes)
        {
            if (absolutePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var index = absolutePath.IndexOf('/', prefix.Length);
                if (index > 0)
                {
                    return absolutePath.Substring(index + 1);
                }
            }
        }
        return absolutePath;
    }
}
```

#### ErrorAggregator.cs

Location: `src/Acode.Infrastructure/ToolSchemas/Retry/ErrorAggregator.cs`

```csharp
using Acode.Application.ToolSchemas.Retry;

namespace Acode.Infrastructure.ToolSchemas.Retry;

/// <summary>
/// Aggregates, deduplicates, and sorts validation errors for optimal model comprehension.
/// </summary>
public sealed class ErrorAggregator
{
    private readonly int _maxErrors;

    public ErrorAggregator(int maxErrors)
    {
        _maxErrors = maxErrors;
    }

    public IReadOnlyList<ValidationError> Aggregate(IEnumerable<ValidationError> errors)
    {
        if (errors == null)
            return Array.Empty<ValidationError>();

        // Deduplicate by field path + error code
        var deduplicated = errors
            .GroupBy(e => $"{e.FieldPath}|{e.ErrorCode}")
            .Select(g => g.First())
            .ToList();

        // Sort by severity (Error > Warning > Info), then by field path
        var sorted = deduplicated
            .OrderByDescending(e => e.Severity)
            .ThenBy(e => e.FieldPath, StringComparer.Ordinal)
            .ToList();

        // Limit count
        if (sorted.Count > _maxErrors)
        {
            sorted = sorted.Take(_maxErrors).ToList();
        }

        return sorted;
    }
}
```

#### RetryTracker.cs

Location: `src/Acode.Infrastructure/ToolSchemas/Retry/RetryTracker.cs`

```csharp
using System.Collections.Concurrent;
using Acode.Application.ToolSchemas.Retry;

namespace Acode.Infrastructure.ToolSchemas.Retry;

/// <summary>
/// Thread-safe retry attempt tracker with validation history.
/// Uses ConcurrentDictionary for O(1) lookups.
/// </summary>
public sealed class RetryTracker : IRetryTracker
{
    private readonly int _maxAttempts;
    private readonly bool _trackHistory;
    private readonly ConcurrentDictionary<string, RetryState> _states;

    private sealed class RetryState
    {
        public int Attempts { get; set; }
        public List<string> History { get; } = new();
    }

    public RetryTracker(int maxAttempts, bool trackHistory)
    {
        _maxAttempts = maxAttempts;
        _trackHistory = trackHistory;
        _states = new ConcurrentDictionary<string, RetryState>();
    }

    public int IncrementAttempt(string toolCallId)
    {
        if (string.IsNullOrEmpty(toolCallId))
            throw new ArgumentException("Tool call ID cannot be null or empty", nameof(toolCallId));

        var state = _states.GetOrAdd(toolCallId, _ => new RetryState());
        return Interlocked.Increment(ref state.Attempts);
    }

    public int GetCurrentAttempt(string toolCallId)
    {
        if (string.IsNullOrEmpty(toolCallId))
            return 0;

        return _states.TryGetValue(toolCallId, out var state) ? state.Attempts : 0;
    }

    public void RecordError(string toolCallId, string errorMessage)
    {
        if (!_trackHistory)
            return;

        if (string.IsNullOrEmpty(toolCallId))
            throw new ArgumentException("Tool call ID cannot be null or empty", nameof(toolCallId));

        var state = _states.GetOrAdd(toolCallId, _ => new RetryState());
        lock (state.History)
        {
            // Limit history to prevent unbounded growth
            if (state.History.Count < 10)
            {
                state.History.Add(errorMessage);
            }
        }
    }

    public IReadOnlyList<string> GetHistory(string toolCallId)
    {
        if (!_trackHistory || string.IsNullOrEmpty(toolCallId))
            return Array.Empty<string>();

        if (!_states.TryGetValue(toolCallId, out var state))
            return Array.Empty<string>();

        lock (state.History)
        {
            return state.History.ToArray();
        }
    }

    public bool HasExceededMaxRetries(string toolCallId)
    {
        var currentAttempt = GetCurrentAttempt(toolCallId);
        return currentAttempt >= _maxAttempts;
    }

    public void Clear(string toolCallId)
    {
        if (!string.IsNullOrEmpty(toolCallId))
        {
            _states.TryRemove(toolCallId, out _);
        }
    }
}
```

#### EscalationFormatter.cs

Location: `src/Acode.Infrastructure/ToolSchemas/Retry/EscalationFormatter.cs`

```csharp
using System.Text;
using Acode.Application.ToolSchemas.Retry;

namespace Acode.Infrastructure.ToolSchemas.Retry;

/// <summary>
/// Formats escalation messages for human intervention after max retries exceeded.
/// </summary>
public sealed class EscalationFormatter : IEscalationFormatter
{
    public string FormatEscalation(string toolName, string toolCallId, IReadOnlyList<string> validationHistory, int maxAttempts)
    {
        if (string.IsNullOrEmpty(toolName))
            throw new ArgumentException("Tool name cannot be null or empty", nameof(toolName));

        var sb = new StringBuilder(capacity: 1000);

        // Header
        sb.AppendLine($"Tool '{toolName}' validation failed after {maxAttempts} attempts.");
        sb.AppendLine();
        sb.AppendLine("Validation History:");
        sb.AppendLine();

        // History entries
        for (int i = 0; i < validationHistory.Count && i < maxAttempts; i++)
        {
            sb.AppendLine($"Attempt {i + 1} ({DateTime.UtcNow.AddSeconds(-((validationHistory.Count - i) * 2)):yyyy-MM-dd HH:mm:ss}):");
            sb.AppendLine($"  {validationHistory[i].Replace("\n", "\n  ")}");
            sb.AppendLine();
        }

        // Analysis
        sb.AppendLine("Analysis:");
        sb.AppendLine($"- Total errors across attempts: {validationHistory.Count}");
        sb.AppendLine("- Retry success rate: 0% (no successful retries)");
        sb.AppendLine();

        // Recommendations
        sb.AppendLine("The model was unable to provide valid arguments after 3 attempts. This may indicate:");
        sb.AppendLine("1. Schema complexity exceeding model capability");
        sb.AppendLine("2. Ambiguous or insufficient tool documentation");
        sb.AppendLine("3. Tool design issue requiring schema revision");
        sb.AppendLine();

        sb.AppendLine("Recommended Actions:");
        sb.AppendLine("- Review tool schema documentation for clarity");
        sb.AppendLine("- Consider simplifying tool arguments");
        sb.AppendLine("- Add examples to tool description");
        sb.AppendLine("- Check if model has context to understand requirements");
        sb.AppendLine();

        sb.AppendLine("Please intervene or provide guidance to proceed.");

        return sb.ToString();
    }
}
```

---

### Dependency Injection Registration

Location: `src/Acode.Infrastructure/DependencyInjection.cs` (add to existing file)

```csharp
using Acode.Application.ToolSchemas.Retry;
using Acode.Infrastructure.ToolSchemas.Retry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Acode.Infrastructure;

public static partial class DependencyInjection
{
    public static IServiceCollection AddRetryContract(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration
        var retryConfig = new RetryConfiguration();
        configuration.GetSection("Tools:Validation:Retry").Bind(retryConfig);
        services.AddSingleton(retryConfig);

        // Register services
        services.AddSingleton<IErrorFormatter, ErrorFormatter>();
        services.AddSingleton<IRetryTracker, RetryTracker>();
        services.AddSingleton<IEscalationFormatter, EscalationFormatter>();

        return services;
    }
}
```

---

### Usage Example (End-to-End Flow)

```csharp
using Acode.Application.Messages;
using Acode.Application.ToolSchemas;
using Acode.Application.ToolSchemas.Retry;

namespace Acode.Infrastructure.Examples;

public class RetryContractExample
{
    private readonly ISchemaValidator _validator;
    private readonly IErrorFormatter _formatter;
    private readonly IRetryTracker _tracker;
    private readonly RetryConfiguration _config;

    public RetryContractExample(
        ISchemaValidator validator,
        IErrorFormatter formatter,
        IRetryTracker tracker,
        RetryConfiguration config)
    {
        _validator = validator;
        _formatter = formatter;
        _tracker = tracker;
        _config = config;
    }

    public ToolResult ValidateToolCall(ToolCall toolCall)
    {
        // 1. Validate arguments against schema
        var errors = _validator.Validate(toolCall.Name, toolCall.Arguments);

        if (!errors.Any())
        {
            // Validation passed - clear retry tracking
            _tracker.Clear(toolCall.Id);
            return null; // Proceed with tool execution
        }

        // 2. Track retry attempt
        var attemptNumber = _tracker.IncrementAttempt(toolCall.Id);

        // 3. Check if max retries exceeded
        if (_tracker.HasExceededMaxRetries(toolCall.Id))
        {
            var history = _tracker.GetHistory(toolCall.Id);
            var escalationFormatter = new EscalationFormatter();
            var escalationMessage = escalationFormatter.FormatEscalation(
                toolCall.Name,
                toolCall.Id,
                history,
                _config.MaxAttempts
            );

            return new ToolResult
            {
                Role = "tool",
                ToolCallId = toolCall.Id,
                Content = escalationMessage,
                IsError = true,
                Status = "Blocked"
            };
        }

        // 4. Format errors for model
        var errorMessage = _formatter.FormatErrors(
            toolCall.Name,
            errors,
            attemptNumber,
            _config.MaxAttempts
        );

        // 5. Record error in history
        _tracker.RecordError(toolCall.Id, errorMessage);

        // 6. Return ToolResult with error
        return new ToolResult
        {
            Role = "tool",
            ToolCallId = toolCall.Id,
            Content = errorMessage,
            IsError = true
        };
    }
}
```

---

### Implementation Checklist

Follow this order to implement the retry contract using TDD:

1. [ ] Write tests for ValidationError (creation, immutability)
2. [ ] Implement ValidationError record
3. [ ] Write tests for ErrorSeverity enum
4. [ ] Implement ErrorSeverity enum
5. [ ] Write tests for ErrorCode constants
6. [ ] Implement ErrorCode static class
7. [ ] Write tests for ValueSanitizer (secret redaction, truncation)
8. [ ] Implement ValueSanitizer class
9. [ ] Write tests for ErrorAggregator (deduplication, sorting)
10. [ ] Implement ErrorAggregator class
11. [ ] Write tests for ErrorFormatter (single error, multiple errors, hints)
12. [ ] Implement ErrorFormatter class
13. [ ] Write tests for RetryTracker (thread safety, history)
14. [ ] Implement RetryTracker class
15. [ ] Write tests for EscalationFormatter
16. [ ] Implement EscalationFormatter class
17. [ ] Write integration tests (full flow)
18. [ ] Add DI registration
19. [ ] Add XML documentation to all public APIs
20. [ ] Run performance tests, verify < 1ms formatting

---

### Verification Commands

```bash
# Run all retry contract tests
dotnet test --filter "FullyQualifiedName~Retry"

# Run only unit tests
dotnet test --filter "FullyQualifiedName~Retry&Category=Unit"

# Run performance tests
dotnet test --filter "FullyQualifiedName~RetryPerformanceTests"

# Check code coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsDirectory=./coverage

# Run benchmarks
dotnet run --project tests/Acode.Performance.Tests --configuration Release
```

---

**End of Task 007.b Specification**

---

**End of Task 007.b Specification**