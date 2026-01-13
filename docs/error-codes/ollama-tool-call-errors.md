# Ollama Tool Call Error Codes

This document describes the error codes used by the Ollama tool call parsing system (`ToolCallParser`). These error codes help diagnose issues with tool call format, validation, and JSON parsing.

## Error Code Format

All tool call parsing error codes follow the format: `ACODE-TLP-XXX`

- **ACODE**: Agentic Coding Bot (project prefix)
- **TLP**: Tool Call Parser (component prefix)
- **XXX**: Sequential error number

---

## ACODE-TLP-001: Missing or Null Function

### Description
The tool call is missing the required `function` property or the `function` property is null.

### Cause
- Ollama response contains a tool call without a function definition
- Malformed response structure from the LLM
- Serialization/deserialization issue resulting in null function

### Resolution
- **Model Side**: Ensure the model is correctly trained to output tool calls with function definitions
- **Integration Side**: Verify the response mapping from Ollama format to `OllamaToolCall` is correct
- **Configuration**: Check that tool definitions are properly registered with the model

### Example
```json
// Invalid: Missing function
{
  "id": "call_123"
  // No function property
}

// Valid:
{
  "id": "call_123",
  "function": {
    "name": "read_file",
    "arguments": "{\"path\": \"test.txt\"}"
  }
}
```

### When This Occurs
- During initial parsing in `ToolCallParser.ParseSingle()` (line 82-87)
- Before any validation of function name or arguments

---

## ACODE-TLP-002: Empty Function Name

### Description
The tool call has a function definition, but the function name is null, empty, or contains only whitespace.

### Cause
- Model generated a tool call without specifying which function to invoke
- Function name was stripped during processing
- Malformed training data leading to empty function names

### Resolution
- **Model Side**: Retrain or fine-tune the model to always provide a function name
- **Retry Logic**: The retry handler will re-request the tool call with a prompt to include the function name
- **Validation**: Ensure tool definitions have non-empty names before sending to the model

### Example
```json
// Invalid: Empty function name
{
  "id": "call_123",
  "function": {
    "name": "",
    "arguments": "{\"path\": \"test.txt\"}"
  }
}

// Valid:
{
  "id": "call_123",
  "function": {
    "name": "read_file",
    "arguments": "{\"path\": \"test.txt\"}"
  }
}
```

### When This Occurs
- During function name validation in `ToolCallParser.ParseSingle()` (line 93-98)
- After checking that function exists, before format validation

---

## ACODE-TLP-003: Invalid Function Name Format

### Description
The function name contains characters that are not allowed. Only alphanumeric characters (a-z, A-Z, 0-9) and underscores (_) are permitted.

### Cause
- Model generated a function name with special characters, spaces, or punctuation
- Function name contains hyphens (use underscores instead)
- Function name includes Unicode characters or emojis

### Resolution
- **Model Side**: Ensure tool definitions only use alphanumeric characters and underscores
- **Retry Logic**: The retry handler will re-request with a prompt explaining valid characters
- **Tool Registration**: Validate tool names match the pattern `^[a-zA-Z0-9_]+$` before registering

### Pattern
Valid function names must match the regular expression: `^[a-zA-Z0-9_]+$`

### Example
```json
// Invalid: Contains hyphen
{
  "function": {
    "name": "read-file",
    "arguments": "{}"
  }
}

// Invalid: Contains space
{
  "function": {
    "name": "read file",
    "arguments": "{}"
  }
}

// Invalid: Contains special characters
{
  "function": {
    "name": "read_file!",
    "arguments": "{}"
  }
}

// Valid: Alphanumeric and underscore only
{
  "function": {
    "name": "read_file",
    "arguments": "{}"
  }
}

// Valid: Numbers are allowed
{
  "function": {
    "name": "tool_123_abc",
    "arguments": "{}"
  }
}
```

### When This Occurs
- During function name format validation in `ToolCallParser.ParseSingle()` (line 100-109)
- After empty name check, before length validation

---

## ACODE-TLP-004: Failed to Parse JSON Arguments

### Description
The arguments string could not be parsed as valid JSON, even after attempting automatic repair.

### Cause
- Model generated malformed JSON (unbalanced braces, trailing commas, etc.)
- Arguments contain syntax errors that `JsonRepairer` cannot fix
- Arguments exceed complexity limits (nesting depth, size)
- Non-JSON content in the arguments field

### Resolution
- **Automatic Repair**: The `JsonRepairer` attempts to fix common issues:
  - Trailing commas
  - Missing closing braces/brackets
  - Single quotes instead of double quotes
  - Unclosed strings
- **Retry Logic**: If repair fails, `ToolCallRetryHandler` re-requests the tool call with error details
- **Model Tuning**: If errors persist, consider fine-tuning the model on correct JSON formatting

### Repair Heuristics Applied
The parser automatically attempts these repairs before failing:
1. Remove trailing commas in objects and arrays
2. Add missing closing braces `}` and brackets `]`
3. Replace single quotes with double quotes (where valid)
4. Close unclosed strings
5. Balance nested structures

### Example
```json
// Invalid: Unbalanced braces (repairable)
{
  "function": {
    "name": "read_file",
    "arguments": "{\"path\": \"test.txt\""
  }
}
// After repair: {"path": "test.txt"}

// Invalid: Trailing comma (repairable)
{
  "function": {
    "name": "read_file",
    "arguments": "{\"path\": \"test.txt\",}"
  }
}
// After repair: {"path": "test.txt"}

// Invalid: Not JSON at all (not repairable)
{
  "function": {
    "name": "read_file",
    "arguments": "just a plain string"
  }
}
// Error: ACODE-TLP-004

// Valid:
{
  "function": {
    "name": "read_file",
    "arguments": "{\"path\": \"test.txt\"}"
  }
}
```

### Retry Behavior
When this error occurs and retries are enabled:
1. Parser attempts automatic JSON repair
2. If repair fails, error is returned with details
3. `ToolCallRetryHandler` sends retry prompt to model with:
   - Original malformed JSON
   - Error message from repair attempt
   - Expected schema (if available)
4. Model generates corrected tool call
5. Process repeats up to `MaxRetries` times (default: 3)

### When This Occurs
- During JSON repair in `ToolCallParser.ParseSingle()` (line 132-143)
- After function name validation, before tool call construction

---

## ACODE-TLP-005: Function Name Too Long

### Description
The function name exceeds the maximum allowed length of 64 characters.

### Cause
- Model generated an excessively long function name
- Function name includes verbose descriptions instead of concise identifiers
- Concatenation of multiple function names

### Resolution
- **Tool Design**: Keep function names concise (recommended: 8-32 characters)
- **Naming Convention**: Use `snake_case` or `camelCase` for multi-word names
- **Model Training**: Ensure training data uses appropriately sized function names
- **Validation**: Check tool names during registration, before sending to model

### Maximum Length
**64 characters** (defined as `MaxNameLength` in `ToolCallParser`)

### Example
```json
// Invalid: 71 characters (exceeds 64)
{
  "function": {
    "name": "read_file_from_filesystem_with_error_handling_and_retry_logic_enabled",
    "arguments": "{}"
  }
}

// Valid: 30 characters
{
  "function": {
    "name": "read_file_with_retry",
    "arguments": "{}"
  }
}

// Valid: Short and concise
{
  "function": {
    "name": "read_file",
    "arguments": "{}"
  }
}
```

### Best Practices
- Use abbreviations where appropriate (e.g., `get` instead of `retrieve`)
- Avoid redundant words (e.g., `read_file` not `read_file_tool`)
- Keep names focused on the primary action
- Use parameters for variations instead of encoding in the name

### When This Occurs
- During function name length validation in `ToolCallParser.ParseSingle()` (line 112-120)
- After format validation, before argument parsing

---

## ACODE-TLP-006: Failed to Create Tool Call

### Description
The tool call passed all validation checks (function exists, name is valid, arguments are valid JSON), but an error occurred while constructing the domain `ToolCall` object.

### Cause
- `ArgumentException` thrown by `ToolCall` constructor
- Invariant violation in domain model (e.g., ID constraints)
- Edge case in argument structure that passes JSON validation but violates domain rules
- Internal consistency check failure

### Resolution
- **Investigate Exception**: Check the error message for specific details
- **Domain Rules**: Review `ToolCall` constructor invariants
- **Validation Gap**: If this error occurs frequently, add validation earlier in the pipeline
- **Bug Report**: This error may indicate a bug in either the parser or the domain model

### Example
```csharp
// In ToolCallParser.ParseSingle() (line 152-168)
try
{
    using var doc = JsonDocument.Parse(repairResult.RepairedJson);
    var argumentsElement = doc.RootElement.Clone();
    var toolCall = new ToolCall(id, toolName, argumentsElement);
    return new SingleParseResult(toolCall, null, repairResult.WasRepaired ? repairResult : null);
}
catch (ArgumentException ex)
{
    return SingleParseResult.WithError(new ToolCallError(
        $"Failed to create tool call: {ex.Message}",
        "ACODE-TLP-006")
    {
        ToolName = toolName,
        RawArguments = rawArguments,
    });
}
```

### Possible Scenarios
1. **ID Validation Failure**: The generated or provided ID violates `ToolCall` invariants
2. **Arguments Structure**: The JSON structure is valid but semantically incorrect for the domain model
3. **Resource Exhaustion**: Out of memory or other system-level failure during object construction

### When This Occurs
- During tool call object construction in `ToolCallParser.ParseSingle()` (line 159-168)
- After all parsing and validation, as final step before returning success

### Diagnostic Steps
1. Log the complete exception message and stack trace
2. Inspect the values of `id`, `toolName`, and `argumentsElement`
3. Check for any custom validation logic in `ToolCall` constructor
4. Verify that the `JsonElement` is properly cloned and remains valid

---

## Error Handling Flow

### Parsing Pipeline
```
OllamaToolCall Input
       ↓
[1] Check function exists → ACODE-TLP-001
       ↓
[2] Check name not empty → ACODE-TLP-002
       ↓
[3] Check name format → ACODE-TLP-003
       ↓
[4] Check name length → ACODE-TLP-005
       ↓
[5] Parse/repair arguments → ACODE-TLP-004
       ↓
[6] Construct ToolCall → ACODE-TLP-006
       ↓
   Success → ToolCall
```

### Retry Integration
When parsing fails, the `ToolCallRetryHandler` provides automatic retry with exponential backoff:

1. **First Attempt**: `ToolCallParser.Parse()` attempts parsing all tool calls
2. **On Failure**: Collect all `ToolCallError` objects
3. **Build Retry Prompt**: Create prompt with error details, malformed JSON, and guidance
4. **Re-invoke Model**: Send retry prompt to model with full conversation context
5. **Parse Again**: Attempt to parse the corrected tool calls
6. **Repeat**: Continue up to `MaxRetries` (default: 3) with exponential backoff
7. **Exhaustion**: If all retries fail, throw `ToolCallRetryExhaustedException`

### Configuration
Retry behavior is configured via `RetryConfig` in `OllamaConfiguration`:

```csharp
public RetryConfig ToolCallRetryConfig { get; init; } = new RetryConfig
{
    MaxRetries = 3,                    // Retry up to 3 times
    EnableAutoRepair = true,           // Attempt JSON repair before failing
    RetryDelayMs = 100,                // Base delay between retries (exponential backoff)
    RepairTimeoutMs = 100,             // Timeout for repair attempts
    StrictValidation = true,           // Enforce strict JSON validation
    MaxNestingDepth = 64,              // Maximum JSON nesting depth
    MaxArgumentSize = 1_048_576,       // Maximum argument size (1MB)
    RetryPromptTemplate = "...",       // Prompt template for retry requests
};
```

---

## Telemetry and Logging

### Recommended Logging
When handling tool call errors, log the following information for diagnostics:

- **Error Code**: The specific ACODE-TLP-XXX code
- **Tool Name**: The function name (if available)
- **Raw Arguments**: The original malformed JSON (for TLP-004)
- **Repair Attempts**: Whether JSON repair was attempted and what heuristics were applied
- **Retry Count**: How many retries were performed before success or exhaustion
- **Timing**: Duration of parsing and retry operations

### Audit Requirements
Per FR-053 and task-005b requirements:
- All tool call parsing errors should be logged to the audit trail
- Include full context: request ID, model, timestamp, error details
- Track repair success/failure rates for monitoring
- Alert on high error rates for specific error codes

### Example Log Entry (JSONL Format)
```jsonl
{"timestamp":"2026-01-13T10:30:45.123Z","level":"error","component":"ToolCallParser","error_code":"ACODE-TLP-004","message":"Failed to parse arguments: Unbalanced braces","tool_name":"read_file","raw_arguments":"{\"path\": \"test.txt\"","repair_attempted":true,"repair_success":false,"request_id":"req_123"}
```

---

## Testing

### Unit Tests
All error codes are covered by unit tests in `ToolCallParserTests.cs`:

- `Parse_NullFunction_ReturnsError` → ACODE-TLP-001
- `Parse_EmptyFunctionName_ReturnsError` → ACODE-TLP-002
- `Parse_InvalidFunctionNameCharacters_ReturnsError` → ACODE-TLP-003
- `Parse_MalformedJson_ReturnsError` → ACODE-TLP-004
- `Parse_FunctionNameTooLong_ReturnsError` → ACODE-TLP-005
- `Parse_ToolCallConstructorThrows_ReturnsError` → ACODE-TLP-006

### Integration Tests
End-to-end tests in `ToolCallRetryHandlerTests.cs` verify:
- Automatic retry on parse failures
- Exponential backoff behavior
- Retry exhaustion after max attempts
- Success after partial failures

---

## Related Documentation

- **Implementation**: `src/Acode.Infrastructure/Ollama/ToolCall/ToolCallParser.cs`
- **Tests**: `tests/Acode.Infrastructure.Tests/Ollama/ToolCall/ToolCallParserTests.cs`
- **Retry Logic**: `src/Acode.Infrastructure/Ollama/ToolCall/ToolCallRetryHandler.cs`
- **JSON Repair**: `src/Acode.Infrastructure/Ollama/ToolCall/JsonRepairer.cs`
- **Configuration**: `src/Acode.Infrastructure/Ollama/OllamaConfiguration.cs`
- **Task Specification**: `docs/tasks/refined-tasks/Epic 00/task-005b-tool-call-parsing-retry-on-invalid-json.md`

---

## Version History

- **2026-01-13**: Initial documentation (task-005b, Gap #10)
