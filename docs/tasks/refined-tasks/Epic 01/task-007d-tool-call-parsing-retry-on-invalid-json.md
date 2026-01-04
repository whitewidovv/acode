# Task 007.d: Tool-Call Parsing and Retry-on-Invalid-JSON

**Priority:** P0 – Critical Path
**Tier:** Core Infrastructure
**Complexity:** 13 (Fibonacci points)
**Phase:** Foundation
**Dependencies:** Task 007, Task 007.a, Task 007.b, Task 007.c, Task 005, Task 004.a

**Note:** This task was originally 005.b but moved to 007.d due to dependency on IToolSchemaRegistry (Task 007).  

---

## Description

Task 007.d specifies the detailed implementation of tool call parsing and the retry-on-invalid-JSON mechanism for the Ollama provider adapter. This task is a subtask of Task 007 (Tool Schema Registry) because it depends on the IToolSchemaRegistry interface for validating tool call arguments against JSON schemas. Tool calling is a fundamental capability enabling the Acode agent to interact with its environment—reading files, executing commands, and modifying code. This task ensures that tool calls from Ollama are parsed correctly and that the system gracefully handles the common case where models produce malformed JSON in tool call arguments.

Tool call parsing transforms Ollama's raw response format into Acode's canonical ToolCall type (Task 004.a). Ollama's tool calling format is based on OpenAI's function calling convention but with subtle differences that require careful mapping. The parser MUST extract the tool name, unique call ID, and JSON arguments from each tool call in the response. Multiple tool calls in a single response are common when the model decides to invoke several tools simultaneously.

The most significant challenge this task addresses is handling malformed JSON in tool call arguments. Large language models, even when explicitly prompted for valid JSON, frequently produce syntactically invalid JSON—missing closing brackets, trailing commas, unquoted property names, and embedded natural language. Rather than failing outright on these cases, the Ollama adapter MUST implement a repair-and-retry strategy that maximizes the chance of successful tool execution.

The retry-on-invalid-JSON mechanism operates in two phases. First, when tool call arguments fail JSON parsing, the system attempts automatic repair using heuristics: removing trailing commas, adding missing brackets, escaping unescaped quotes, and similar common fixes. If repair succeeds and produces valid JSON, execution proceeds. If repair fails, the system invokes the model again with an error message explaining the JSON error, prompting the model to output corrected JSON.

The retry mechanism MUST be bounded to prevent infinite loops. Configuration specifies the maximum number of retry attempts (default 3). Each retry MUST include the previous malformed output and the specific parse error in the prompt, giving the model context to correct its mistake. If all retries are exhausted without valid JSON, the system fails with a clear error indicating the JSON repair attempts were unsuccessful.

This task integrates closely with the Tool Schema Registry (Task 007), which defines the expected structure of tool arguments. After JSON parsing succeeds, the arguments MUST be validated against the tool's JSON Schema. Schema validation failures trigger a different retry path—the JSON is syntactically valid but semantically incorrect (wrong types, missing required fields). The adapter MUST distinguish parse failures from validation failures in error messages and retry prompts.

Streaming tool calls add complexity to parsing. When streaming is enabled, tool calls may arrive incrementally with partial JSON arguments spread across multiple chunks. The parser MUST accumulate argument fragments correctly before attempting to parse the complete JSON. The streaming accumulator (from Task 005.a) handles content accumulation; this task extends that pattern to tool call arguments.

Error handling for tool call parsing covers multiple failure modes: no tool_calls field when expected, tool call with missing required fields (id, function name), malformed JSON in arguments, valid JSON but wrong schema, and unexpected argument types. Each failure mode MUST produce a distinct error with diagnostic information. The error MUST include the raw tool call data for debugging.

Logging throughout the tool call parsing process enables troubleshooting. The adapter MUST log (at DEBUG level) each tool call extracted, JSON repair attempts, retry iterations, and final success or failure. Logs MUST NOT include argument values at INFO level since they may contain sensitive data, but MUST include them at DEBUG for local troubleshooting. Tool names and call IDs are safe to log at INFO.

This subtask is critical for Acode's agentic capabilities. Without robust tool call parsing and error recovery, the agent cannot reliably interact with its environment. The retry mechanism transforms occasional model JSON errors from hard failures into recoverable situations, significantly improving the system's practical reliability.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Tool Call | Model request to invoke an external function/tool |
| ToolCall | Acode's canonical type representing a tool invocation |
| OllamaToolCall | Ollama-specific tool call format from response |
| Arguments | JSON object containing parameters for tool execution |
| JSON Repair | Automatic correction of common JSON syntax errors |
| Retry Prompt | Message sent to model requesting JSON correction |
| Retry Count | Number of correction attempts before giving up |
| Parse Error | Syntax error preventing JSON deserialization |
| Validation Error | Semantic error where JSON doesn't match schema |
| JSON Schema | Formal definition of expected argument structure |
| Streaming Accumulator | Buffer collecting incremental tool call fragments |
| Function Name | Identifier specifying which tool to invoke |
| Call ID | Unique identifier for a specific tool invocation |
| Trailing Comma | Common JSON error: comma before closing brace |
| Unquoted Key | Common JSON error: property name without quotes |
| Truncated JSON | Incomplete JSON due to token limit |
| Repair Heuristics | Rules for automatically fixing JSON errors |
| Error Context | Information about parse failure for retry prompt |
| Strict Mode | Require exact JSON schema match (no extra fields) |

---

## Out of Scope

The following items are explicitly excluded from Task 005.b:

- **Tool execution** - Running the actual tool is outside this task
- **Tool result handling** - Processing tool results is separate
- **Tool definition** - Tool schemas are defined in Task 007
- **Multi-turn tool conversations** - Orchestration is higher-level
- **Tool timeout handling** - Execution timeout is tool-level concern
- **Tool permission checking** - Security is Task 003 concern
- **Tool dependency resolution** - Tool ordering is orchestration
- **Parallel tool execution** - Execution strategy is separate
- **Tool result caching** - Caching is optimization task
- **Natural language tool output** - Formatting is CLI concern

---

## Functional Requirements

### Tool Call Extraction

- FR-001: ToolCallParser MUST be defined in Infrastructure layer
- FR-002: ToolCallParser MUST extract tool_calls from OllamaResponse
- FR-003: ToolCallParser MUST handle missing tool_calls field (return empty list)
- FR-004: ToolCallParser MUST extract function.name from each tool call
- FR-005: ToolCallParser MUST extract function.arguments from each tool call
- FR-006: ToolCallParser MUST extract or generate unique ID for each call
- FR-007: ToolCallParser MUST handle multiple tool calls in single response
- FR-008: ToolCallParser MUST preserve tool call ordering
- FR-009: ToolCallParser MUST map to ToolCall type (Task 004.a)

### Ollama Tool Call Format

- FR-010: Parser MUST handle Ollama tool_call format with function object
- FR-011: Parser MUST handle function.name as string (required)
- FR-012: Parser MUST handle function.arguments as JSON string or object
- FR-013: Parser MUST generate call ID if not present (prefix: "call_")
- FR-014: Parser MUST validate function.name is non-empty
- FR-015: Parser MUST validate function.name matches known tools
- FR-016: Parser MUST reject tool calls for unknown tools

### JSON Argument Parsing

- FR-017: Parser MUST parse arguments JSON string to JsonElement
- FR-018: Parser MUST validate arguments is JSON object (not array/primitive)
- FR-019: Parser MUST handle empty arguments object {}
- FR-020: Parser MUST detect JSON parse errors with specific error type
- FR-021: Parser MUST include error position in parse error details
- FR-022: Parser MUST handle arguments already as JsonElement (streaming)

### JSON Repair Heuristics

- FR-023: JsonRepairer MUST be defined as separate component
- FR-024: JsonRepairer MUST attempt to fix trailing commas
- FR-025: JsonRepairer MUST attempt to fix missing closing braces
- FR-026: JsonRepairer MUST attempt to fix missing closing brackets
- FR-027: JsonRepairer MUST attempt to fix unescaped quotes in strings
- FR-028: JsonRepairer MUST attempt to fix truncated strings
- FR-029: JsonRepairer MUST attempt to fix unquoted property names
- FR-030: JsonRepairer MUST attempt to fix single quotes to double quotes
- FR-031: JsonRepairer MUST NOT modify valid JSON
- FR-032: JsonRepairer MUST return RepairResult indicating success/failure
- FR-033: JsonRepairer MUST log all repair attempts
- FR-034: JsonRepairer MUST timeout after 100ms

### Repair Result

- FR-035: RepairResult MUST indicate whether repair was attempted
- FR-036: RepairResult MUST include original JSON if repair failed
- FR-037: RepairResult MUST include repaired JSON if repair succeeded
- FR-038: RepairResult MUST include list of repairs applied
- FR-039: RepairResult MUST include error if repair failed

### Retry Mechanism

- FR-040: Retry MUST be triggered on JSON parse failure
- FR-041: Retry MUST be triggered on JSON schema validation failure
- FR-042: Retry MUST respect max retry count (configurable, default 3)
- FR-043: Retry MUST construct prompt with error context
- FR-044: Retry prompt MUST include original malformed JSON
- FR-045: Retry prompt MUST include specific error message
- FR-046: Retry prompt MUST include expected format hint
- FR-047: Retry MUST send new inference request to model
- FR-048: Retry MUST extract new tool call from response
- FR-049: Retry MUST parse and validate new tool call
- FR-050: Retry MUST track cumulative token usage across attempts
- FR-051: Retry MUST log each attempt with attempt number

### Retry Configuration

- FR-052: RetryConfig MUST include max_retries (default 3)
- FR-053: RetryConfig MUST include enable_auto_repair (default true)
- FR-054: RetryConfig MUST include repair_timeout_ms (default 100)
- FR-055: RetryConfig MUST include retry_prompt_template
- FR-056: RetryConfig MUST be loadable from .agent/config.yml

### Schema Validation

- FR-057: Parser MUST validate arguments against tool's JSON Schema
- FR-058: Parser MUST use IToolSchemaRegistry (Task 007) for schemas
- FR-059: Parser MUST distinguish parse errors from validation errors
- FR-060: Validation errors MUST include path to invalid field
- FR-061: Validation errors MUST include expected vs actual type
- FR-062: Validation errors MUST include missing required fields
- FR-063: Validation MUST enforce strict mode (no extra properties)

### Streaming Tool Calls

- FR-064: StreamingToolCallParser MUST accumulate tool call fragments
- FR-065: Parser MUST handle partial function name in stream
- FR-066: Parser MUST handle partial arguments in stream
- FR-067: Parser MUST buffer argument fragments until complete
- FR-068: Parser MUST detect tool call completion in stream
- FR-069: Parser MUST handle multiple concurrent tool calls in stream
- FR-070: Parser MUST use index to correlate tool call fragments

### Error Types

- FR-071: ToolCallParseException MUST be defined for parse failures
- FR-072: ToolCallValidationException MUST be defined for schema failures
- FR-073: ToolCallRetryExhaustedException MUST be defined for max retries
- FR-074: All exceptions MUST include original response data
- FR-075: All exceptions MUST include attempt count
- FR-076: All exceptions MUST include correlation ID

### Result Types

- FR-077: ToolCallParseResult MUST indicate success/failure
- FR-078: ToolCallParseResult MUST include parsed ToolCalls on success
- FR-079: ToolCallParseResult MUST include errors on failure
- FR-080: ToolCallParseResult MUST include repair details if applied
- FR-081: ToolCallParseResult MUST include retry count if retried

### Smoke Test Integration (Task 005c Stub)

- FR-082: Tool calling smoke test MUST be implemented when 007d completes
- FR-083: Smoke test MUST verify tool call extraction from response
- FR-084: Smoke test MUST verify JSON argument parsing succeeds
- FR-085: Smoke test MUST verify tool call mapped to ToolCall type
- FR-086: Smoke test MUST be added to scripts/smoke-tests/ollama-smoke-test.sh
- FR-087: Smoke test MUST integrate with `acode providers smoke-test ollama` command

**Note:** Task 005c implemented a stub for the tool calling smoke test with message "Requires Task 007d - skipped". This section ensures the stub is replaced with actual implementation when 007d is complete.

---

## Non-Functional Requirements

### Performance

- NFR-001: Tool call extraction MUST complete in < 1 millisecond
- NFR-002: JSON repair MUST timeout after 100 milliseconds
- NFR-003: Schema validation MUST complete in < 5 milliseconds
- NFR-004: Memory for argument parsing MUST be < 1 MB per call
- NFR-005: Streaming accumulation MUST be O(n) for n fragments

### Reliability

- NFR-006: Parser MUST NOT crash on any malformed input
- NFR-007: Parser MUST handle deeply nested JSON (limit 64 levels)
- NFR-008: Parser MUST handle large arguments (up to 1 MB)
- NFR-009: Retry mechanism MUST terminate after max attempts
- NFR-010: Streaming parser MUST handle out-of-order fragments

### Security

- NFR-011: Argument values MUST NOT be logged at INFO level
- NFR-012: Repair MUST NOT execute embedded code in JSON
- NFR-013: Schema validation MUST prevent injection attacks
- NFR-014: Large arguments MUST NOT cause memory exhaustion

### Observability

- NFR-015: All parse attempts MUST be logged with correlation ID
- NFR-016: Repair attempts MUST be logged at DEBUG level
- NFR-017: Retry iterations MUST be logged with attempt number
- NFR-018: Final success/failure MUST be logged at INFO level

### Maintainability

- NFR-019: Repair heuristics MUST be individually testable
- NFR-020: Parser MUST be configurable without code changes
- NFR-021: Error messages MUST be clear and actionable
- NFR-022: All public APIs MUST have XML documentation

---

## User Manual Documentation

### Overview

This component parses tool calls from Ollama responses and handles the common case where models produce malformed JSON. It implements automatic repair and retry mechanisms to maximize successful tool execution.

### How Tool Call Parsing Works

1. **Extract**: Tool calls are extracted from the Ollama response
2. **Parse**: JSON arguments are parsed for each tool call
3. **Repair** (if needed): Malformed JSON is automatically repaired
4. **Validate**: Arguments are validated against tool's JSON Schema
5. **Retry** (if needed): Model is asked to correct invalid output

### Configuration

Configure tool call parsing in `.agent/config.yml`:

```yaml
model:
  providers:
    ollama:
      tool_call:
        # Maximum retry attempts for invalid JSON
        max_retries: 3
        
        # Enable automatic JSON repair
        enable_auto_repair: true
        
        # Timeout for repair attempts (ms)
        repair_timeout_ms: 100
        
        # Enforce strict schema validation
        strict_validation: true
```

### JSON Repair Capabilities

The auto-repair system can fix these common errors:

| Error Type | Example | Repair |
|------------|---------|--------|
| Trailing comma | `{"a": 1,}` | Remove comma |
| Missing brace | `{"a": 1` | Add `}` |
| Unquoted key | `{path: "x"}` | Quote key |
| Single quotes | `{'a': 1}` | Double quotes |
| Truncated | `{"a": "hel` | Close string |
| Unescaped quote | `{"a": "say "hi""}` | Escape quotes |

### Retry Prompt Template

When automatic repair fails, the model is prompted to correct:

```
The tool call arguments you provided contain invalid JSON.

Error: {error_message}
Position: {error_position}

Your output:
{malformed_json}

Please provide the corrected JSON for the '{tool_name}' tool:
```

### Handling Multiple Tool Calls

When the model requests multiple tools:

```csharp
var result = await parser.ParseToolCallsAsync(response);

foreach (var toolCall in result.ToolCalls)
{
    Console.WriteLine($"Tool: {toolCall.Name}");
    Console.WriteLine($"ID: {toolCall.Id}");
    Console.WriteLine($"Args: {toolCall.Arguments}");
}
```

### Streaming Tool Calls

Tool calls in streaming responses are accumulated:

```csharp
var accumulator = new StreamingToolCallAccumulator();

await foreach (var delta in stream)
{
    if (delta.ToolCallDelta is not null)
    {
        accumulator.Append(delta.ToolCallDelta);
    }
    
    if (delta.IsComplete)
    {
        var toolCalls = accumulator.GetToolCalls();
    }
}
```

### Error Handling

```csharp
try
{
    var result = await parser.ParseToolCallsAsync(response);
    // Process tool calls...
}
catch (ToolCallParseException ex)
{
    // JSON syntax error that couldn't be repaired
    logger.LogError(ex, "Tool call JSON is invalid: {Error}", ex.Message);
}
catch (ToolCallValidationException ex)
{
    // JSON is valid but doesn't match schema
    logger.LogError(ex, "Tool call arguments don't match schema: {Path}", ex.ValidationPath);
}
catch (ToolCallRetryExhaustedException ex)
{
    // Model couldn't produce valid JSON after retries
    logger.LogError(ex, "Failed after {Attempts} retry attempts", ex.AttemptCount);
}
```

### Logging Fields

Tool call parsing logs include:

| Field | Type | Description |
|-------|------|-------------|
| correlation_id | string | Request correlation ID |
| tool_name | string | Name of tool being parsed |
| tool_call_id | string | Unique ID of tool call |
| parse_result | string | success, repaired, retry, failed |
| repair_applied | string[] | List of repairs applied |
| retry_count | int | Number of retry attempts |
| validation_errors | string[] | Schema validation errors |

### Troubleshooting

#### Frequent JSON Parse Errors

**Symptom**: Many tool calls require repair or retry

**Cause**: Model may not support native function calling

**Solution**:
1. Use model with function calling support
2. Add JSON examples to system prompt
3. Enable strict JSON mode if supported

#### Schema Validation Failures

**Symptom**: JSON parses but fails validation

**Cause**: Model output doesn't match expected schema

**Solution**:
1. Review tool definition for clarity
2. Add parameter descriptions
3. Provide examples in tool description

#### Retry Loop

**Symptom**: All retry attempts exhausted

**Cause**: Model cannot produce valid JSON for tool

**Solution**:
1. Increase max_retries
2. Simplify tool parameters
3. Use model with better JSON capabilities

---

## Acceptance Criteria

### Tool Call Extraction

- [ ] AC-001: ToolCallParser defined in Infrastructure
- [ ] AC-002: Extracts tool_calls from response
- [ ] AC-003: Handles missing tool_calls (empty list)
- [ ] AC-004: Extracts function.name
- [ ] AC-005: Extracts function.arguments
- [ ] AC-006: Generates ID if missing
- [ ] AC-007: Handles multiple tool calls
- [ ] AC-008: Preserves ordering
- [ ] AC-009: Maps to ToolCall type

### Ollama Format Handling

- [ ] AC-010: Handles function object format
- [ ] AC-011: Validates function.name
- [ ] AC-012: Handles arguments as string
- [ ] AC-013: Handles arguments as object
- [ ] AC-014: Generates call ID with prefix
- [ ] AC-015: Rejects empty function name
- [ ] AC-016: Rejects unknown tools

### JSON Argument Parsing

- [ ] AC-017: Parses JSON string to JsonElement
- [ ] AC-018: Validates is object type
- [ ] AC-019: Handles empty object
- [ ] AC-020: Detects parse errors
- [ ] AC-021: Includes error position
- [ ] AC-022: Handles JsonElement input

### JSON Repair

- [ ] AC-023: JsonRepairer defined
- [ ] AC-024: Fixes trailing commas
- [ ] AC-025: Fixes missing braces
- [ ] AC-026: Fixes missing brackets
- [ ] AC-027: Fixes unescaped quotes
- [ ] AC-028: Fixes truncated strings
- [ ] AC-029: Fixes unquoted keys
- [ ] AC-030: Fixes single quotes
- [ ] AC-031: Doesn't modify valid JSON
- [ ] AC-032: Returns RepairResult
- [ ] AC-033: Logs repairs
- [ ] AC-034: Respects timeout

### Repair Result

- [ ] AC-035: Indicates attempt status
- [ ] AC-036: Includes original on failure
- [ ] AC-037: Includes repaired on success
- [ ] AC-038: Lists repairs applied
- [ ] AC-039: Includes error on failure

### Retry Mechanism

- [ ] AC-040: Retries on parse failure
- [ ] AC-041: Retries on validation failure
- [ ] AC-042: Respects max retry count
- [ ] AC-043: Constructs retry prompt
- [ ] AC-044: Includes malformed JSON
- [ ] AC-045: Includes error message
- [ ] AC-046: Includes format hint
- [ ] AC-047: Sends new request
- [ ] AC-048: Extracts new tool call
- [ ] AC-049: Validates new tool call
- [ ] AC-050: Tracks token usage
- [ ] AC-051: Logs each attempt

### Retry Configuration

- [ ] AC-052: max_retries configurable
- [ ] AC-053: enable_auto_repair configurable
- [ ] AC-054: repair_timeout_ms configurable
- [ ] AC-055: Template configurable
- [ ] AC-056: Loads from config.yml

### Schema Validation

- [ ] AC-057: Validates against schema
- [ ] AC-058: Uses IToolSchemaRegistry
- [ ] AC-059: Distinguishes error types
- [ ] AC-060: Includes invalid path
- [ ] AC-061: Includes type mismatch
- [ ] AC-062: Includes missing fields
- [ ] AC-063: Enforces strict mode

### Streaming

- [ ] AC-064: Accumulates fragments
- [ ] AC-065: Handles partial name
- [ ] AC-066: Handles partial args
- [ ] AC-067: Buffers until complete
- [ ] AC-068: Detects completion
- [ ] AC-069: Handles concurrent calls
- [ ] AC-070: Uses index correlation

### Error Types

- [ ] AC-071: ToolCallParseException defined
- [ ] AC-072: ToolCallValidationException defined
- [ ] AC-073: ToolCallRetryExhaustedException defined
- [ ] AC-074: Includes response data
- [ ] AC-075: Includes attempt count
- [ ] AC-076: Includes correlation ID

### Performance

- [ ] AC-077: Extraction < 1ms
- [ ] AC-078: Repair timeout enforced
- [ ] AC-079: Validation < 5ms
- [ ] AC-080: Memory < 1MB per call

### Security

- [ ] AC-081: No args at INFO level
- [ ] AC-082: No code execution in repair
- [ ] AC-083: Injection prevention
- [ ] AC-084: Memory bounds enforced

### Documentation

- [ ] AC-085: XML docs complete
- [ ] AC-086: Config examples
- [ ] AC-087: Troubleshooting guide

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Infrastructure/Ollama/ToolCall/
├── ToolCallParserTests.cs
│   ├── Should_Extract_Single_ToolCall()
│   ├── Should_Extract_Multiple_ToolCalls()
│   ├── Should_Handle_Missing_ToolCalls()
│   ├── Should_Generate_Id_When_Missing()
│   ├── Should_Validate_FunctionName()
│   └── Should_Reject_Unknown_Tool()
│
├── JsonRepairerTests.cs
│   ├── Should_Fix_TrailingComma()
│   ├── Should_Fix_MissingBrace()
│   ├── Should_Fix_MissingBracket()
│   ├── Should_Fix_UnescapedQuotes()
│   ├── Should_Fix_TruncatedString()
│   ├── Should_Fix_UnquotedKeys()
│   ├── Should_Fix_SingleQuotes()
│   ├── Should_Not_Modify_ValidJson()
│   └── Should_Timeout_OnComplexInput()
│
├── RetryMechanismTests.cs
│   ├── Should_Retry_OnParseFailure()
│   ├── Should_Retry_OnValidationFailure()
│   ├── Should_Respect_MaxRetries()
│   ├── Should_Include_ErrorContext()
│   ├── Should_Track_TokenUsage()
│   └── Should_Log_Attempts()
│
├── SchemaValidationTests.cs
│   ├── Should_Validate_RequiredFields()
│   ├── Should_Validate_FieldTypes()
│   ├── Should_Reject_ExtraFields_StrictMode()
│   └── Should_Report_ValidationPath()
│
└── StreamingAccumulatorTests.cs
    ├── Should_Accumulate_Fragments()
    ├── Should_Handle_PartialName()
    ├── Should_Handle_PartialArgs()
    └── Should_Handle_ConcurrentCalls()
```

### Integration Tests

```
Tests/Integration/Ollama/ToolCall/
├── ToolCallIntegrationTests.cs
│   ├── Should_Parse_Real_ToolCall()
│   ├── Should_Repair_And_Succeed()
│   └── Should_Retry_And_Succeed()
```

### Performance Tests

```
Tests/Performance/Ollama/
├── ToolCallBenchmarks.cs
│   ├── Benchmark_Extraction()
│   ├── Benchmark_Repair()
│   └── Benchmark_Validation()
```

---

## User Verification Steps

### Scenario 1: Parse Valid Tool Call

1. Create response with valid tool_calls
2. Parse with ToolCallParser
3. Verify ToolCall extracted correctly
4. Verify arguments are valid JSON

### Scenario 2: Repair Trailing Comma

1. Create response with `{"path": "test",}` args
2. Parse with auto-repair enabled
3. Verify JSON repaired to `{"path": "test"}`
4. Verify tool call succeeds

### Scenario 3: Retry on Parse Failure

1. Create response with invalid JSON args
2. Configure max_retries: 3
3. Mock model to return valid on retry 2
4. Verify retry occurred
5. Verify final success

### Scenario 4: Schema Validation Failure

1. Create response with wrong argument type
2. Parse and validate
3. Verify ToolCallValidationException
4. Verify error includes field path

### Scenario 5: Multiple Tool Calls

1. Create response with 3 tool_calls
2. Parse all calls
3. Verify all 3 extracted
4. Verify ordering preserved

### Scenario 6: Streaming Accumulation

1. Stream response with tool call deltas
2. Accumulate fragments
3. Verify complete tool call extracted

### Scenario 7: Max Retries Exhausted

1. Configure max_retries: 2
2. Model always returns invalid JSON
3. Verify ToolCallRetryExhaustedException after 2 attempts

### Scenario 8: Unknown Tool Rejection

1. Create response with unknown tool name
2. Parse with strict mode
3. Verify rejection with clear error

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/Ollama/ToolCall/
├── ToolCallParser.cs
├── JsonRepairer.cs
├── ToolCallRetryHandler.cs
├── StreamingToolCallAccumulator.cs
├── SchemaValidator.cs
├── Models/
│   ├── OllamaToolCall.cs
│   ├── RepairResult.cs
│   └── ToolCallParseResult.cs
└── Exceptions/
    ├── ToolCallParseException.cs
    ├── ToolCallValidationException.cs
    └── ToolCallRetryExhaustedException.cs
```

### ToolCallParser Implementation

```csharp
namespace AgenticCoder.Infrastructure.Ollama.ToolCall;

public sealed class ToolCallParser
{
    private readonly JsonRepairer _repairer;
    private readonly IToolSchemaRegistry _schemaRegistry;
    private readonly ILogger<ToolCallParser> _logger;
    
    public ToolCallParseResult Parse(OllamaResponse response)
    {
        if (response.Message?.ToolCalls is null or { Count: 0 })
        {
            return ToolCallParseResult.Empty();
        }
        
        var toolCalls = new List<ToolCall>();
        var errors = new List<ToolCallError>();
        
        foreach (var ollamaCall in response.Message.ToolCalls)
        {
            var parseResult = ParseSingleCall(ollamaCall);
            
            if (parseResult.Success)
            {
                toolCalls.Add(parseResult.ToolCall!);
            }
            else
            {
                errors.Add(parseResult.Error!);
            }
        }
        
        return new ToolCallParseResult(toolCalls, errors);
    }
    
    private SingleParseResult ParseSingleCall(OllamaToolCall call)
    {
        // Validate function name
        if (string.IsNullOrEmpty(call.Function?.Name))
        {
            return SingleParseResult.Fail(new ToolCallError(
                "Function name is required",
                "ACODE-TLP-001"));
        }
        
        // Parse arguments
        var argsJson = call.Function.Arguments;
        if (!TryParseJson(argsJson, out var arguments, out var parseError))
        {
            // Attempt repair
            var repairResult = _repairer.TryRepair(argsJson);
            if (repairResult.Success)
            {
                arguments = repairResult.RepairedJson;
                _logger.LogDebug("Repaired JSON: {Repairs}", repairResult.Repairs);
            }
            else
            {
                return SingleParseResult.Fail(new ToolCallError(
                    $"Invalid JSON: {parseError}",
                    "ACODE-TLP-002"));
            }
        }
        
        // Validate against schema
        // ...
        
        return SingleParseResult.Ok(new ToolCall
        {
            Id = call.Id ?? $"call_{Guid.NewGuid():N}",
            Name = call.Function.Name,
            Arguments = arguments
        });
    }
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-TLP-001 | Function name is required |
| ACODE-TLP-002 | Invalid JSON in arguments |
| ACODE-TLP-003 | JSON repair failed |
| ACODE-TLP-004 | Schema validation failed |
| ACODE-TLP-005 | Unknown tool name |
| ACODE-TLP-006 | Max retries exhausted |
| ACODE-TLP-007 | Repair timeout |
| ACODE-TLP-008 | Streaming accumulation error |

### Implementation Checklist

1. [ ] Define OllamaToolCall model
2. [ ] Define RepairResult type
3. [ ] Define ToolCallParseResult type
4. [ ] Implement JsonRepairer
5. [ ] Implement repair heuristics
6. [ ] Implement ToolCallParser
7. [ ] Implement schema validation
8. [ ] Implement retry handler
9. [ ] Implement streaming accumulator
10. [ ] Define exception types
11. [ ] Write unit tests
12. [ ] Write integration tests
13. [ ] Add XML documentation

### Dependencies

- Task 005 (Provider adapter)
- Task 005.a (HTTP handling)
- Task 004.a (ToolCall type)
- Task 007 (Schema registry)

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~ToolCall"
```

---

**End of Task 005.b Specification**