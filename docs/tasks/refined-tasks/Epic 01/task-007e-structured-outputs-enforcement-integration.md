# Task 007.e: Structured Outputs Enforcement Integration

**Priority:** P1 – High Priority
**Tier:** Core Infrastructure
**Complexity:** 13 (Fibonacci points)
**Phase:** Foundation
**Dependencies:** Task 007, Task 007.a, Task 007.b, Task 007.c, Task 007.d, Task 006, Task 006.a, Task 004.a, Task 001, Task 002

**Note:** This task was originally 006.b but moved to 007.e due to dependency on IToolSchemaRegistry (Task 007). vLLM's structured output enforcement requires tool schema definitions, which are provided by Task 007. The move was approved by the user on 2026-01-04 during Task 006 implementation planning.  

---

## Description

Task 006.b implements structured output enforcement integration for the vLLM provider, enabling deterministic JSON generation that conforms to specified schemas. This is a key differentiator for vLLM compared to Ollama—while Ollama requires retry-on-invalid-JSON logic (Task 005.b), vLLM can enforce schema compliance during generation through guided decoding, eliminating the need for post-hoc validation and retries.

Structured outputs are critical for reliable tool calling and agent workflows. When the model generates a tool call, the arguments MUST be valid JSON matching the tool's parameter schema. Without enforcement, models sometimes produce syntactically invalid JSON, use wrong types, or omit required fields. These failures trigger retries, waste compute, and degrade user experience. Guided decoding eliminates these failures at the source.

vLLM's structured output support comes through multiple mechanisms. The response_format parameter with type "json_object" enables basic JSON mode. The json_schema parameter provides schema-based enforcement using vLLM's guided decoding engine. For tool calls, tool_choice with function schemas enables structured function arguments. This task integrates all three mechanisms into Acode's provider abstraction.

The integration connects vLLM's structured output capabilities with Acode's Tool Schema Registry (Task 007). When a request includes tool definitions, the adapter extracts JSON Schema definitions from the tools and configures vLLM's guided decoding accordingly. This ensures tool arguments always validate against their schemas without post-generation validation.

vLLM's guided decoding uses constrained generation algorithms like Outlines or grammar-based approaches. These algorithms restrict the token vocabulary at each step to only those tokens that could lead to valid output. The performance impact is minimal—guided decoding typically adds <5% overhead compared to unconstrained generation.

The adapter MUST support fallback behavior for models that don't support guided decoding. Not all models work with all structured output modes—some require specific tokenizers, some don't support nested schemas, and some have size limits. The adapter MUST detect these limitations and fall back to unconstrained generation with retry-based validation when needed.

Configuration controls structured output behavior at multiple levels. Global configuration sets defaults (enabled/disabled, fallback behavior). Per-request configuration overrides globals. The adapter MUST respect configuration precedence and log which mode is active. Users can disable structured outputs entirely if they cause issues with specific models.

Schema transformation handles differences between Acode's tool schema format and vLLM's expected format. While both use JSON Schema, subtle differences exist in how schemas are structured, validated, and applied. The adapter MUST transform schemas correctly, handling nested objects, arrays, enums, optional fields, and complex constraints.

Error handling addresses structured output specific failures. vLLM may reject schemas that are too complex, reference unsupported types, or exceed size limits. The adapter MUST catch these errors, provide helpful messages, and fall back gracefully. Error codes enable callers to distinguish structured output failures from other errors.

Observability includes logging when structured outputs are enabled, which schema is applied, whether fallback occurred, and generation statistics. This visibility helps users understand behavior and debug issues. Metrics track structured output success rates, fallback rates, and performance impact.

Integration testing verifies that structured outputs work correctly with real vLLM instances. Tests confirm that output matches schemas, that invalid schemas cause appropriate errors, and that fallback works correctly. Performance tests verify that guided decoding overhead is acceptable.

The structured output integration makes vLLM a premium choice for production workloads where reliability is paramount. Users can configure vLLM for critical paths and Ollama for development, with confidence that vLLM's structured outputs will never produce invalid tool arguments.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Structured Output | Output constrained to match a schema |
| Guided Decoding | Constraining generation at token level |
| JSON Schema | Schema language for JSON validation |
| response_format | OpenAI parameter for output format |
| json_object | Response format enabling JSON mode |
| json_schema | Specific schema for guided decoding |
| tool_choice | Parameter controlling tool selection |
| Outlines | Library for constrained generation |
| Grammar-Based | Using formal grammars to constrain output |
| Token Vocabulary | Set of valid next tokens |
| Fallback | Using retry-based validation when guided decoding unavailable |
| Schema Transformation | Converting between schema formats |
| Nested Schema | Schema containing embedded object schemas |
| Required Fields | Schema fields that must be present |
| Default Values | Schema values used when field omitted |
| Constrained Generation | Generation limited to valid outputs |
| Schema Registry | Centralized schema definitions (Task 007) |
| Schema Validation | Checking output against schema |

---

## Out of Scope

The following items are explicitly excluded from Task 006.b:

- **Tool Schema Registry** - Task 007 defines schemas
- **Schema creation** - Schemas come from tool definitions
- **Complex schema validation** - vLLM does validation
- **Retry-based validation** - Ollama approach, Task 005.b
- **Custom grammar definitions** - Uses JSON Schema only
- **vLLM configuration** - Server-side config is user responsibility
- **Model-specific tuning** - Not optimizing per model
- **Streaming structured output** - Non-streaming focus
- **Partial output validation** - Complete output only
- **Schema caching** - Simple pass-through

---

## Functional Requirements

### Structured Output Configuration

- FR-001: Configuration MUST support global enable/disable
- FR-002: Configuration MUST support per-model enable/disable
- FR-003: Configuration MUST support fallback behavior setting
- FR-004: Configuration MUST support strict vs lenient mode
- FR-005: Configuration MUST be readable from .agent/config.yml
- FR-006: Configuration MUST support environment variable override
- FR-007: Configuration MUST validate on startup

### Response Format Integration

- FR-008: Adapter MUST support response_format parameter
- FR-009: Adapter MUST support type: "json_object"
- FR-010: Adapter MUST support type: "json_schema"
- FR-011: Adapter MUST pass schema in json_schema type
- FR-012: Adapter MUST set response_format when configured
- FR-013: Adapter MUST NOT set response_format when disabled
- FR-014: Adapter MUST log response_format setting

### Tool Schema Integration

- FR-015: Adapter MUST extract schemas from ToolDefinition
- FR-016: Adapter MUST use tool parameter schemas for arguments
- FR-017: Adapter MUST transform schemas to vLLM format
- FR-018: Adapter MUST handle multiple tools with schemas
- FR-019: Adapter MUST apply tool_choice structured constraints
- FR-020: Adapter MUST merge tool schemas when needed

### Schema Transformation

- FR-021: Transformer MUST handle object type schemas
- FR-022: Transformer MUST handle array type schemas
- FR-023: Transformer MUST handle primitive type schemas
- FR-024: Transformer MUST handle enum constraints
- FR-025: Transformer MUST handle required field lists
- FR-026: Transformer MUST handle additionalProperties
- FR-027: Transformer MUST handle nested object schemas
- FR-028: Transformer MUST handle array item schemas
- FR-029: Transformer MUST handle $ref references (inline)
- FR-030: Transformer MUST preserve field descriptions

### Guided Decoding Request Construction

- FR-031: Adapter MUST set guided_json when schema provided
- FR-032: Adapter MUST set guided_choice for enum output
- FR-033: Adapter MUST set guided_regex for pattern output
- FR-034: Adapter MUST validate schema before sending
- FR-035: Adapter MUST handle schema that is too large
- FR-036: Adapter MUST use appropriate guided parameter

### Capability Detection

- FR-037: Adapter MUST detect if model supports structured output
- FR-038: Adapter MUST query vLLM for model capabilities
- FR-039: Adapter MUST cache capability information
- FR-040: Adapter MUST refresh capabilities on model change
- FR-041: Adapter MUST handle unknown models conservatively

### Fallback Behavior

- FR-042: Adapter MUST fall back when structured output unavailable
- FR-043: Adapter MUST fall back on schema rejection error
- FR-044: Adapter MUST fall back on unsupported schema type
- FR-045: Adapter MUST log fallback with reason
- FR-046: Adapter MUST validate output when in fallback mode
- FR-047: Adapter MUST retry on validation failure (fallback)
- FR-048: Adapter MUST limit fallback retries (default 3)

### Output Validation

- FR-049: Validator MUST check output is valid JSON
- FR-050: Validator MUST check output matches schema
- FR-051: Validator MUST report validation errors clearly
- FR-052: Validator MUST handle null values correctly
- FR-053: Validator MUST handle missing optional fields
- FR-054: Validator MUST flag missing required fields
- FR-055: Validator MUST check type correctness

### Tool Call Argument Handling

- FR-056: Adapter MUST ensure tool arguments are valid JSON
- FR-057: Adapter MUST ensure tool arguments match schema
- FR-058: Adapter MUST extract arguments from response
- FR-059: Adapter MUST handle multiple tool calls
- FR-060: Adapter MUST handle partial tool call responses

### Error Handling

- FR-061: Adapter MUST handle schema too complex error
- FR-062: Adapter MUST handle unsupported type error
- FR-063: Adapter MUST handle guided decoding timeout
- FR-064: Adapter MUST handle invalid schema format
- FR-065: Adapter MUST return clear error messages
- FR-066: Adapter MUST set appropriate error codes
- FR-067: Adapter MUST log errors with schema context

### Performance

- FR-068: Schema transformation MUST cache results
- FR-069: Capability detection MUST cache results
- FR-070: Adapter MUST minimize overhead per request
- FR-071: Adapter MUST timeout on slow schema processing

---

## Non-Functional Requirements

### Performance

- NFR-001: Schema transformation MUST complete in < 1ms
- NFR-002: Capability detection MUST complete in < 100ms (cached)
- NFR-003: Structured output overhead MUST be < 5% generation time
- NFR-004: Schema cache MUST hold 100+ schemas
- NFR-005: Memory for schema caching MUST be < 10MB

### Reliability

- NFR-006: Fallback MUST work when structured output fails
- NFR-007: Adapter MUST not crash on invalid schemas
- NFR-008: Adapter MUST handle vLLM restarts gracefully
- NFR-009: Capability cache MUST expire appropriately
- NFR-010: Validation MUST be deterministic

### Security

- NFR-011: Schema content MUST NOT be logged at INFO level
- NFR-012: Validation errors MUST NOT expose sensitive data
- NFR-013: Schema processing MUST be sandboxed
- NFR-014: $ref MUST only resolve local references

### Observability

- NFR-015: Structured output mode MUST be logged per request
- NFR-016: Fallback events MUST be logged with reason
- NFR-017: Validation failures MUST be logged
- NFR-018: Schema complexity MUST be trackable
- NFR-019: Performance metrics MUST be available

### Maintainability

- NFR-020: All public APIs MUST have XML documentation
- NFR-021: Schema handling MUST be testable in isolation
- NFR-022: Fallback logic MUST be testable independently
- NFR-023: Configuration MUST be documented

---

## User Manual Documentation

### Overview

Structured output enforcement ensures vLLM generates valid JSON matching specified schemas. This eliminates the retry cycles needed with Ollama and improves reliability for tool calling.

### Quick Start

Structured outputs are enabled by default for vLLM:

```yaml
model:
  providers:
    vllm:
      structured_output:
        enabled: true
```

Verify with a tool call:

```csharp
var response = await provider.CompleteAsync(new ChatRequest
{
    Messages = new[] { new ChatMessage(MessageRole.User, "Read file.txt") },
    Tools = new[] { fileReadTool }
});
// Arguments are guaranteed to be valid JSON matching the tool schema
```

### Configuration

```yaml
model:
  providers:
    vllm:
      structured_output:
        # Master enable/disable
        enabled: true
        
        # Fallback when structured output unavailable
        fallback:
          enabled: true
          max_retries: 3
          validation_mode: strict
        
        # Schema handling
        schema:
          max_depth: 10
          max_size_bytes: 65536
          cache_size: 100
```

### Structured Output Modes

#### JSON Object Mode

Basic JSON enforcement without schema:

```csharp
var request = new ChatRequest
{
    Messages = messages,
    ResponseFormat = new ResponseFormat { Type = "json_object" }
};
```

Output will be valid JSON, but structure is not enforced.

#### JSON Schema Mode

Schema-enforced JSON output:

```csharp
var request = new ChatRequest
{
    Messages = messages,
    ResponseFormat = new ResponseFormat
    {
        Type = "json_schema",
        JsonSchema = new JsonSchemaFormat
        {
            Name = "user_info",
            Schema = userInfoSchema
        }
    }
};
```

Output will match the provided schema exactly.

#### Tool Calling Mode

Automatic schema enforcement for tool arguments:

```csharp
var request = new ChatRequest
{
    Messages = messages,
    Tools = tools,  // Tools include parameter schemas
    ToolChoice = "auto"
};
```

Tool arguments are guaranteed valid.

### Schema Requirements

Supported JSON Schema features:

- Type: object, array, string, number, integer, boolean, null
- Properties with nested schemas
- Required field lists
- Enum constraints
- Array items schemas
- Default values
- Descriptions (for model guidance)

Unsupported features (cause fallback):

- $ref to external schemas
- allOf/anyOf/oneOf combinators
- if/then/else conditionals
- Regular expressions (partial support)
- Format validations

### Fallback Behavior

When structured output is unavailable or fails:

1. Request sent without structured output constraints
2. Response validated against schema
3. If invalid, retry up to max_retries
4. If still invalid, return error

Fallback reasons:
- Model doesn't support guided decoding
- Schema too complex for vLLM
- vLLM returned guided decoding error
- Timeout during constrained generation

### Monitoring Fallback

```bash
# Check structured output status
acode providers status vllm

# Recent fallback events
acode logs --filter "structured_output.fallback=true"
```

### Error Codes

| Code | Description |
|------|-------------|
| ACODE-VLM-SO-001 | Schema too complex |
| ACODE-VLM-SO-002 | Unsupported schema type |
| ACODE-VLM-SO-003 | Guided decoding timeout |
| ACODE-VLM-SO-004 | Invalid schema format |
| ACODE-VLM-SO-005 | Validation failed (fallback) |
| ACODE-VLM-SO-006 | Max retries exceeded |

### Troubleshooting

#### "Schema too complex"

**Cause:** Schema exceeds vLLM's limits

**Solution:**
1. Simplify schema (reduce nesting)
2. Split into smaller schemas
3. Increase max_size_bytes if allowed

#### "Fallback activated"

**Cause:** Structured output unavailable

**Solution:**
1. Check model supports guided decoding
2. Check vLLM version (0.4.0+)
3. Simplify schema if needed

#### "Validation failed after retries"

**Cause:** Model can't produce valid output

**Solution:**
1. Check schema is achievable
2. Add better descriptions
3. Use more capable model

---

## Acceptance Criteria

### Configuration

- [ ] AC-001: Global enable/disable works
- [ ] AC-002: Per-model enable/disable works
- [ ] AC-003: Fallback behavior configurable
- [ ] AC-004: Strict/lenient mode works
- [ ] AC-005: Config from .agent/config.yml
- [ ] AC-006: Environment override works
- [ ] AC-007: Config validated on startup

### Response Format

- [ ] AC-008: response_format parameter supported
- [ ] AC-009: json_object type works
- [ ] AC-010: json_schema type works
- [ ] AC-011: Schema passed correctly
- [ ] AC-012: Not set when disabled
- [ ] AC-013: Mode logged

### Tool Schema Integration

- [ ] AC-014: Schemas extracted from tools
- [ ] AC-015: Parameter schemas used
- [ ] AC-016: Schemas transformed correctly
- [ ] AC-017: Multiple tools handled
- [ ] AC-018: tool_choice constraints applied

### Schema Transformation

- [ ] AC-019: Object type handled
- [ ] AC-020: Array type handled
- [ ] AC-021: Primitive types handled
- [ ] AC-022: Enums handled
- [ ] AC-023: Required fields handled
- [ ] AC-024: additionalProperties handled
- [ ] AC-025: Nested objects handled
- [ ] AC-026: Array items handled
- [ ] AC-027: $ref inlined
- [ ] AC-028: Descriptions preserved

### Guided Decoding

- [ ] AC-029: guided_json set correctly
- [ ] AC-030: guided_choice set correctly
- [ ] AC-031: guided_regex set correctly
- [ ] AC-032: Schema validated before send
- [ ] AC-033: Large schema handled
- [ ] AC-034: Correct parameter used

### Capability Detection

- [ ] AC-035: Model capability detected
- [ ] AC-036: vLLM queried
- [ ] AC-037: Capabilities cached
- [ ] AC-038: Cache refreshed on change
- [ ] AC-039: Unknown models handled

### Fallback

- [ ] AC-040: Falls back when unavailable
- [ ] AC-041: Falls back on rejection
- [ ] AC-042: Falls back on unsupported
- [ ] AC-043: Fallback logged with reason
- [ ] AC-044: Output validated in fallback
- [ ] AC-045: Retry works in fallback
- [ ] AC-046: Retry limit respected

### Output Validation

- [ ] AC-047: Valid JSON checked
- [ ] AC-048: Schema match checked
- [ ] AC-049: Errors reported clearly
- [ ] AC-050: Null values handled
- [ ] AC-051: Optional fields handled
- [ ] AC-052: Required fields flagged
- [ ] AC-053: Types checked

### Tool Call Arguments

- [ ] AC-054: Arguments valid JSON
- [ ] AC-055: Arguments match schema
- [ ] AC-056: Extraction works
- [ ] AC-057: Multiple calls handled
- [ ] AC-058: Partial responses handled

### Error Handling

- [ ] AC-059: Complex schema error handled
- [ ] AC-060: Unsupported type error handled
- [ ] AC-061: Timeout handled
- [ ] AC-062: Invalid format handled
- [ ] AC-063: Clear error messages
- [ ] AC-064: Error codes set
- [ ] AC-065: Errors logged with context

### Performance

- [ ] AC-066: Schema transformation cached
- [ ] AC-067: Capability detection cached
- [ ] AC-068: Minimal overhead per request
- [ ] AC-069: Slow processing times out

### Security

- [ ] AC-070: Schema not logged at INFO
- [ ] AC-071: Errors don't expose data
- [ ] AC-072: Processing sandboxed
- [ ] AC-073: Only local $ref resolved

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Infrastructure/Vllm/StructuredOutput/
├── StructuredOutputConfigurationTests.cs
│   ├── Should_Enable_By_Default()
│   ├── Should_Disable_When_Configured()
│   ├── Should_Override_From_Environment()
│   └── Should_Validate_Configuration()
│
├── SchemaTransformerTests.cs
│   ├── Should_Transform_Object_Schema()
│   ├── Should_Transform_Array_Schema()
│   ├── Should_Handle_Enums()
│   ├── Should_Handle_Required_Fields()
│   ├── Should_Handle_Nested_Objects()
│   ├── Should_Inline_Refs()
│   └── Should_Preserve_Descriptions()
│
├── ResponseFormatBuilderTests.cs
│   ├── Should_Build_JsonObject_Format()
│   ├── Should_Build_JsonSchema_Format()
│   └── Should_Include_Schema_Name()
│
├── CapabilityDetectorTests.cs
│   ├── Should_Detect_Support()
│   ├── Should_Cache_Results()
│   └── Should_Handle_Unknown_Models()
│
├── FallbackHandlerTests.cs
│   ├── Should_Trigger_On_Unavailable()
│   ├── Should_Validate_Output()
│   ├── Should_Retry_On_Invalid()
│   └── Should_Limit_Retries()
│
└── OutputValidatorTests.cs
    ├── Should_Validate_JSON()
    ├── Should_Validate_Schema()
    ├── Should_Report_Errors()
    └── Should_Handle_Optional_Fields()
```

### Integration Tests

```
Tests/Integration/Vllm/StructuredOutput/
├── StructuredOutputIntegrationTests.cs
│   ├── Should_Generate_Valid_JSON()
│   ├── Should_Match_Schema()
│   ├── Should_Handle_Tool_Calls()
│   └── Should_Fallback_Gracefully()
```

### Performance Tests

```
Tests/Performance/Vllm/StructuredOutput/
├── StructuredOutputBenchmarks.cs
│   ├── Benchmark_Schema_Transformation()
│   ├── Benchmark_Capability_Detection()
│   ├── Benchmark_Validation()
│   └── Benchmark_Generation_Overhead()
```

---

## User Verification Steps

### Scenario 1: Basic JSON Object Mode

1. Send request with response_format = json_object
2. Verify: Response is valid JSON
3. Verify: No parsing errors

### Scenario 2: Schema Enforcement

1. Define JSON Schema for output
2. Send request with json_schema format
3. Verify: Response matches schema exactly
4. Verify: All required fields present

### Scenario 3: Tool Call Arguments

1. Define tool with parameter schema
2. Send request with tool
3. Model calls tool
4. Verify: Arguments match schema
5. Verify: No validation errors

### Scenario 4: Nested Schema

1. Define schema with nested objects
2. Send request with schema
3. Verify: Nested objects correct
4. Verify: All levels validated

### Scenario 5: Fallback Activation

1. Disable structured output for model
2. Send request with schema
3. Verify: Fallback activated
4. Verify: Logged with reason
5. Verify: Output still validated

### Scenario 6: Validation Failure

1. Configure lenient fallback
2. Force invalid output
3. Verify: Retry occurs
4. Verify: Eventually succeeds or errors

### Scenario 7: Schema Too Complex

1. Send very complex schema
2. Verify: Error returned
3. Verify: Error code ACODE-VLM-SO-001
4. Verify: Helpful error message

### Scenario 8: Capability Caching

1. Check model capability
2. Check again immediately
3. Verify: Second check uses cache
4. Verify: No extra vLLM calls

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/Vllm/StructuredOutput/
├── StructuredOutputConfiguration.cs
├── StructuredOutputHandler.cs
├── Schema/
│   ├── SchemaTransformer.cs
│   ├── SchemaValidator.cs
│   └── SchemaCache.cs
├── ResponseFormat/
│   ├── ResponseFormatBuilder.cs
│   └── GuidedDecodingBuilder.cs
├── Capability/
│   ├── CapabilityDetector.cs
│   └── CapabilityCache.cs
├── Fallback/
│   ├── FallbackHandler.cs
│   └── OutputValidator.cs
└── Exceptions/
    ├── StructuredOutputException.cs
    ├── SchemaTooComplexException.cs
    └── ValidationFailedException.cs
```

### StructuredOutputHandler Implementation

```csharp
namespace AgenticCoder.Infrastructure.Vllm.StructuredOutput;

public sealed class StructuredOutputHandler
{
    private readonly StructuredOutputConfiguration _config;
    private readonly SchemaTransformer _transformer;
    private readonly CapabilityDetector _capabilities;
    private readonly FallbackHandler _fallback;
    private readonly ILogger<StructuredOutputHandler> _logger;
    
    public void ApplyToRequest(
        VllmRequest request,
        ChatRequest chatRequest,
        string modelId)
    {
        if (!_config.IsEnabled(modelId))
        {
            _logger.LogDebug("Structured output disabled for {Model}", modelId);
            return;
        }
        
        if (chatRequest.ResponseFormat is not null)
        {
            ApplyResponseFormat(request, chatRequest.ResponseFormat);
        }
        else if (chatRequest.Tools?.Any() == true)
        {
            ApplyToolSchemas(request, chatRequest.Tools);
        }
    }
    
    private void ApplyResponseFormat(VllmRequest request, ResponseFormat format)
    {
        if (format.Type == "json_object")
        {
            request.ResponseFormat = new { type = "json_object" };
        }
        else if (format.Type == "json_schema")
        {
            var transformed = _transformer.Transform(format.JsonSchema.Schema);
            request.ResponseFormat = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = format.JsonSchema.Name,
                    schema = transformed
                }
            };
        }
    }
    
    // Additional implementation...
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-VLM-SO-001 | Schema exceeds complexity limits |
| ACODE-VLM-SO-002 | Unsupported JSON Schema type |
| ACODE-VLM-SO-003 | Guided decoding timeout |
| ACODE-VLM-SO-004 | Invalid schema format |
| ACODE-VLM-SO-005 | Output validation failed |
| ACODE-VLM-SO-006 | Max retries exceeded |

### Implementation Checklist

1. [ ] Create StructuredOutputConfiguration
2. [ ] Create SchemaTransformer
3. [ ] Create SchemaValidator
4. [ ] Create ResponseFormatBuilder
5. [ ] Create GuidedDecodingBuilder
6. [ ] Create CapabilityDetector
7. [ ] Create CapabilityCache
8. [ ] Create FallbackHandler
9. [ ] Create OutputValidator
10. [ ] Create StructuredOutputHandler
11. [ ] Integrate with VllmProvider
12. [ ] Create exception types
13. [ ] Wire up DI registration
14. [ ] Write unit tests
15. [ ] Write integration tests
16. [ ] Add XML documentation

### Dependencies

- Task 006 (VllmProvider)
- Task 006.a (VllmHttpClient)
- Task 007 (Tool Schema Registry)
- System.Text.Json

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~Vllm.StructuredOutput"
```

---

**End of Task 006.b Specification**