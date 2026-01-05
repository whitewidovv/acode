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

## Use Cases

### Use Case 1: DevBot (AI Agent) Executes File Operations with Guaranteed Schema Compliance

**Scenario:** DevBot is executing a user request to "analyze the codebase and create a summary report." This requires calling multiple file system tools (ReadFile, WriteFile, ListDirectory) with complex argument structures. Each tool call must have valid JSON arguments matching the tool's parameter schema, or the operation will fail.

**Without Structured Output Enforcement:**
DevBot calls the vLLM model to generate tool calls for file operations. The model returns a tool call for `ReadFile` with arguments `{"path": "/src/main.cs", max_lines: null}`. This JSON is syntactically valid, but `max_lines` should be `"maxLines"` (camelCase), and `null` should be omitted or converted to an integer. The argument validation fails with error `ACODE-VAL-003: Invalid field name 'max_lines', expected 'maxLines'`. DevBot retries the request, wasting 2.3 seconds of generation time. On retry, the model produces `{"path": "/src/main.cs", "maxLines": "100"}` - now the field name is correct, but `"100"` is a string instead of integer. Validation fails again. After 3 retries (total 7.8 seconds), the model finally produces valid arguments. This retry loop occurs on 23% of tool calls, adding cumulative latency of 47 seconds per complex task (15 tool calls × 23% failure rate × 3.1s average retry time). User experience degrades due to unpredictable delays.

**With Structured Output Enforcement:**
DevBot calls vLLM with structured output enabled. The adapter extracts the `ReadFile` tool schema from the Tool Schema Registry (Task 007), which defines `{"type": "object", "properties": {"path": {"type": "string"}, "maxLines": {"type": "integer"}}, "required": ["path"]}`. The adapter transforms this schema to vLLM's guided decoding format and sets `guided_json` parameter. vLLM's guided decoding engine restricts token generation at each step to only produce valid JSON matching the schema. The model generates `{"path": "/src/main.cs", "maxLines": 100}` on the first attempt - correct field names (camelCase), correct types (integer not string), no extraneous fields. Arguments validate immediately without retries. **Result: 100% first-attempt success rate, 0 seconds wasted on retries, 47 seconds saved per complex task (67% latency reduction).** User perceives DevBot as reliably fast.

**Business Impact:**
- **Time Savings:** 47 seconds per complex task × 850 tasks/month = 11.1 hours/month saved = $555/month at $50/hour developer time
- **Reliability:** 23% tool call failure rate → 0% failure rate (100% first-attempt success)
- **User Satisfaction:** Predictable performance eliminates frustrating retry delays

---

### Use Case 2: Jordan (System Admin) Configures Structured Output Fallback for New Models

**Scenario:** Jordan is integrating a new vLLM-compatible model (`deepseek-coder-33b`) into Acode's model registry. This model is optimized for code generation but has limited support for guided decoding - it only supports basic `json_object` mode, not full `json_schema` mode with complex nested structures. Jordan needs to configure fallback behavior so that Acode gracefully handles this model's limitations without breaking tool calling workflows.

**Without Structured Output Enforcement:**
There's no centralized configuration for model-specific structured output capabilities. Jordan manually adds the model to `.agent/config.yml` under `model.providers.vllm.models`. When a user tries to use this model for a tool-heavy workflow, vLLM returns error `400: Model does not support json_schema parameter`. This error isn't caught by the adapter, propagates to the user as `ACODE-VLM-REQ-002: vLLM request failed`, and the task fails entirely. Jordan spends 2 hours debugging by reading vLLM server logs, discovering the capability limitation, and patching the adapter code to hardcode a fallback for this specific model. This manual process repeats for each new model, taking 1.5-2 hours per model integration.

**With Structured Output Enforcement:**
Jordan updates `.agent/config.yml` with model-specific structured output configuration:
```yaml
model:
  providers:
    vllm:
      models:
        deepseek-coder-33b:
          structured_output:
            enabled: true
            supported_modes: [json_object]  # Only basic JSON, no schema
            fallback:
              enabled: true
              validation_mode: strict
              max_retries: 3
```
When a user selects `deepseek-coder-33b` for a tool calling task, the adapter queries this configuration and detects that `json_schema` mode is unsupported. It automatically enables fallback mode: sends the request without `guided_json` parameter, receives unconstrained JSON output from vLLM, validates the output against the tool schema using the FallbackHandler, and retries if validation fails (up to 3 attempts). The first attempt succeeds 78% of the time (deepseek-coder is good at JSON despite lack of guided decoding), and retries cover the remaining 22%. **Result: 100% success rate with automatic fallback, 0 hours manual debugging, seamless integration of new models.** Jordan configures each new model in 10 minutes instead of 2 hours (92% time savings).

**Business Impact:**
- **Time Savings:** 1.83 hours saved per model × 6 new models/year = 11 hours/year = $550/year at $50/hour
- **Reliability:** New models work immediately without code patches or debugging
- **Flexibility:** Acode supports diverse model ecosystem (guided decoding + fallback models)

---

### Use Case 3: Alex (Developer) Debugs Schema Complexity Error Using Structured Output Logs

**Scenario:** Alex is implementing a new tool `AnalyzeCodeComplexity` that takes a complex nested schema with 12 properties, 4 nested objects, and 3 arrays. When testing the tool with vLLM, the request fails with error `ACODE-VLM-SO-001: Schema exceeds complexity limits (depth: 6, max: 5)`. Alex needs to understand which part of the schema is too complex and how to simplify it.

**Without Structured Output Enforcement:**
The error message is generic: `ACODE-VLM-SO-001: Schema too complex`. Alex has no visibility into which schema property caused the issue, what the actual depth/size limits are, or how to fix it. Alex spends 45 minutes manually inspecting the JSON Schema, counting nesting levels, and guessing which parts to simplify. After trial-and-error (7 attempts), Alex discovers that the `fileMetrics` nested object has 6 levels of nesting (exceeding vLLM's 5-level limit). Alex flattens the schema by promoting nested fields to top-level, but this breaks the logical structure and makes the tool harder to use.

**With Structured Output Enforcement:**
When the schema validation error occurs, the adapter logs detailed diagnostic information at DEBUG level:
```
[DEBUG] StructuredOutputHandler: Schema validation failed
  Tool: AnalyzeCodeComplexity
  Error: Schema exceeds depth limit
  Max Depth Allowed: 5
  Actual Depth: 6
  Path to deepest property: fileMetrics.complexity.cyclomaticComplexity.perFunction.histogram.buckets
  Schema Size: 4,832 bytes (limit: 65,536 bytes - OK)
  Suggestion: Flatten nested structure or split into multiple tool calls
```
Alex reads this log and immediately identifies the problem: `fileMetrics.complexity.cyclomaticComplexity.perFunction.histogram.buckets` is 6 levels deep. Alex refactors by splitting `AnalyzeCodeComplexity` into two tools: `AnalyzeCodeComplexity` (4 levels deep) and `GetComplexityHistogram` (separate tool for histogram data). Both schemas now comply with limits. **Result: 5 minutes to diagnose and fix (vs 45 minutes trial-and-error), cleaner tool design with better separation of concerns.**

**Business Impact:**
- **Time Savings:** 40 minutes saved per schema debugging session × 8 sessions/month = 5.3 hours/month = $265/month at $50/hour
- **Better Tool Design:** Diagnostic feedback encourages clean, well-structured tool schemas
- **Developer Experience:** Clear error messages reduce frustration and learning curve

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

## Assumptions

This task makes the following technical, operational, and integration assumptions:

### Technical Assumptions

1. **vLLM Server Version**: vLLM server is running version 0.4.0 or newer, which includes guided decoding support via Outlines library or grammar-based constraints.

2. **JSON Schema Support**: vLLM's guided decoding engine supports JSON Schema Draft 07 or newer for schema definitions.

3. **Model Compatibility**: Not all models support guided decoding equally - some require specific tokenizers (e.g., models with custom tokenizers may fail), and compatibility is detected at runtime via capability detection.

4. **Schema Complexity Limits**: vLLM imposes limits on schema complexity (max depth: 5-10 levels depending on version, max size: 64KB), which the adapter must respect.

5. **Performance Overhead**: Guided decoding adds 2-8% generation latency overhead compared to unconstrained generation, which is acceptable for the reliability benefits.

6. **Token Vocabulary Constraints**: Guided decoding works by restricting the token vocabulary at each generation step, which requires the model's tokenizer to align with JSON syntax (most modern models satisfy this).

7. **Streaming Limitations**: Structured output enforcement is primarily designed for non-streaming requests; streaming with guided decoding may have delayed first-token latency.

8. **Error Propagation**: vLLM errors related to structured outputs (e.g., schema too complex, unsupported type) are returned as HTTP 400 Bad Request with specific error messages that the adapter can parse.

### Operational Assumptions

9. **Configuration Management**: `.agent/config.yml` is the source of truth for structured output configuration, and changes require application restart (no hot-reloading of structured output settings).

10. **Fallback Availability**: When structured output is unavailable, fallback validation using retry-on-invalid-JSON is always possible (relies on Task 007b's ValidationErrorModel).

11. **Capability Caching**: Model capability detection results are cached for 1 hour, reducing overhead but potentially causing stale capability information if vLLM server is upgraded mid-session.

12. **Logging Level**: Detailed schema transformation and validation logs are available at DEBUG level; production deployments typically run at INFO or WARN, hiding these details unless explicitly enabled.

13. **Schema Registry Availability**: Tool Schema Registry (Task 007) is initialized before any vLLM provider calls are made, ensuring schemas are always available when needed.

### Integration Assumptions

14. **Tool Schema Format**: All tool schemas in the Tool Schema Registry conform to JSON Schema standard and include `type`, `properties`, and `required` fields at minimum.

15. **Request-Response Contract**: vLLM follows OpenAI-compatible API contract for `response_format`, `tools`, and `tool_choice` parameters, ensuring Acode's request construction works correctly.

16. **Error Code Uniqueness**: Error codes `ACODE-VLM-SO-001` through `ACODE-VLM-SO-006` are unique and don't conflict with other vLLM adapter error codes.

17. **Test Environment**: Integration tests have access to a running vLLM instance with a model that supports guided decoding (e.g., `mistralai/Mistral-7B-Instruct-v0.2`).

18. **DI Registration Order**: `StructuredOutputHandler` is registered in the DI container after `VllmHttpClient` and `ToolSchemaRegistry` are registered, ensuring dependencies are available.

19. **Thread Safety**: `StructuredOutputHandler` and related classes are designed to be thread-safe for concurrent requests (schema cache uses `ConcurrentDictionary`, capability cache has appropriate locking).

20. **Backward Compatibility**: Disabling structured outputs (`enabled: false`) completely bypasses structured output logic, ensuring backward compatibility with vLLM servers that don't support guided decoding.

---

## Security Considerations

### Threat Model

Structured output enforcement introduces several security considerations that must be addressed to prevent vulnerabilities:

#### Threat 1: Schema Injection Attack

**Attack Vector:** An attacker provides a malicious tool schema containing crafted JSON that, when transformed and sent to vLLM, causes server-side code execution or denial of service.

**Example:** Schema contains `$ref: "file:///etc/passwd"` attempting to read local files on vLLM server, or deeply nested structures designed to cause stack overflow during schema parsing.

**Mitigation:**
- **Schema Sanitization**: `SchemaTransformer` MUST validate all incoming schemas and reject any containing `$ref` references to external URIs (only local `#/definitions/` references allowed).
- **Depth Limits**: Enforce maximum schema depth of 10 levels before sending to vLLM, preventing stack overflow attacks.
- **Size Limits**: Enforce maximum schema size of 64KB to prevent memory exhaustion.
- **Type Whitelist**: Only allow primitive types (`string`, `number`, `integer`, `boolean`, `null`) and structural types (`object`, `array`); reject custom types or extensions.
- **Input Validation**: All schemas are validated against JSON Schema meta-schema before transformation.

**Audit Requirement:** Log all schema validation failures at WARN level with schema hash (not full content) for security audit trail.

#### Threat 2: Sensitive Data Exposure via Schema Descriptions

**Attack Vector:** Tool schemas include sensitive information in `description` fields (e.g., internal server paths, API keys, business logic details) which are logged or exposed in error messages.

**Example:** Tool schema has `"description": "Read file from internal server at 10.0.1.42:8080/api/v1/files"`, which leaks internal network topology when logged.

**Mitigation:**
- **Description Redaction**: Scrub schema descriptions before logging - replace with `<description redacted>` at INFO level, full description only logged at DEBUG level (which should be disabled in production).
- **PII Detection**: Use regex patterns to detect and redact common sensitive patterns (IP addresses, UUIDs, paths starting with `/internal/`) before logging.
- **Error Message Sanitization**: Error messages MUST NOT include full schema content; only include schema name and high-level structure (e.g., "Schema for tool 'ReadFile' has 3 properties").

**Audit Requirement:** Security team MUST review all schema-related error messages before release to ensure no sensitive data leakage.

#### Threat 3: Denial of Service via Complex Schema

**Attack Vector:** Attacker submits tool schema with extreme complexity (thousands of properties, deeply nested objects) designed to exhaust vLLM server resources or cause timeout during guided decoding.

**Example:** Schema has 50,000 properties with 20 levels of nesting, causing vLLM's guided decoding engine to timeout or consume excessive memory.

**Mitigation:**
- **Complexity Validation**: `SchemaValidator` MUST reject schemas exceeding limits: max 1,000 properties per schema, max 10 nesting levels, max 64KB total size.
- **Timeout Protection**: All schema transformation and validation operations MUST complete within 100ms; if exceeded, reject schema with error `ACODE-VLM-SO-003: Schema processing timeout`.
- **Circuit Breaker**: If > 3 schema validation failures occur within 60 seconds from same source, temporarily reject all requests from that source for 5 minutes.

**Audit Requirement:** Monitor schema complexity metrics (depth, property count, size) and alert if patterns suggest DoS attack (e.g., repeated submissions of maximum-complexity schemas).

#### Threat 4: Model Output Manipulation

**Attack Vector:** In fallback mode (when guided decoding unavailable), an attacker crafts prompts that trick the model into generating seemingly-valid JSON that passes schema validation but contains malicious payloads (e.g., SQL injection strings, script tags).

**Example:** User prompt: "Read file: `test.txt'; DROP TABLE users; --`". Model generates tool call `{"tool": "ReadFile", "path": "test.txt'; DROP TABLE users; --"}` which passes schema validation but contains SQL injection payload.

**Mitigation:**
- **Content Sanitization**: Even in fallback mode with validation, tool argument values MUST be sanitized before execution - apply escaping, input validation, and allowlists appropriate to each tool's requirements.
- **Layered Validation**: Schema validation is ONLY the first layer; each tool implementation MUST perform its own input validation and sanitization (defense in depth).
- **Prompt Injection Detection**: Log and monitor for common prompt injection patterns in user inputs; flag suspicious patterns for security review.

**Audit Requirement:** All tool executions MUST be logged with full argument values (sanitized if containing sensitive data) for audit trail and post-incident forensics.

#### Threat 5: Cache Poisoning

**Attack Vector:** Attacker manipulates capability cache or schema cache to store false information, causing legitimate requests to fail or bypass security checks.

**Example:** Attacker gains write access to cache storage (Redis, memory) and sets capability for model `gpt-4` to `supports_guided_decoding: false`, forcing all requests to use insecure fallback mode.

**Mitigation:**
- **Cache Integrity**: Use cryptographic signatures (HMAC-SHA256) for all cache entries; verify signature before reading.
- **Cache Expiration**: All cache entries MUST expire within 1 hour maximum; capability cache refreshed on any error that suggests stale data.
- **Read-Only Cache Access**: Application code MUST only have read access to cache; writes performed by dedicated cache manager with restricted permissions.
- **Cache Isolation**: Each model provider has isolated cache namespace to prevent cross-contamination.

**Audit Requirement:** Log all cache writes with timestamp, key, and value hash; detect anomalies (e.g., capability suddenly changing for established model).

#### Threat 6: Information Disclosure via Error Messages

**Attack Vector:** Detailed error messages expose internal system architecture, file paths, or configuration details to unauthorized users.

**Example:** Error message: `ACODE-VLM-SO-001: Schema validation failed at /usr/local/acode/Infrastructure/Vllm/StructuredOutput/SchemaValidator.cs:147 - depth limit exceeded`. This leaks internal file structure.

**Mitigation:**
- **Error Message Levels**: Production error messages MUST be user-friendly without internal details; detailed errors only in DEBUG logs accessible to admins.
- **Exception Scrubbing**: All exceptions MUST be scrubbed to remove stack traces, file paths, and internal variable names before returning to client.
- **Structured Logging**: Use structured logging with separate fields for `user_message` (safe for users) and `internal_details` (logged separately with restricted access).

**Audit Requirement:** Security team MUST review all custom exception classes to ensure messages don't leak sensitive information.

### Security Best Practices

1. **Principle of Least Privilege**: StructuredOutputHandler operates with minimal permissions - only read access to Tool Schema Registry, no write access to database or file system.
2. **Defense in Depth**: Structured output validation is layer 1; each tool MUST implement its own input validation (layer 2), and execution sandbox MUST enforce resource limits (layer 3).
3. **Fail Secure**: All errors default to rejecting the request rather than proceeding with potentially unsafe fallback behavior.
4. **Audit Trail**: All structured output mode changes (enabled → fallback, fallback → enabled) MUST be logged for security audit.
5. **Regular Security Reviews**: Schema transformation and validation logic MUST be reviewed quarterly for new attack vectors as vLLM and JSON Schema standards evolve.

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

## Assumptions

### Technical Assumptions

1. **vLLM Version 0.4.0+:** vLLM must be version 0.4.0 or later to support guided decoding features. Earlier versions lack the `guided_json`, `guided_choice`, and `guided_regex` parameters required for structured output enforcement.

2. **OpenAI-Compatible API:** vLLM is configured in OpenAI-compatible mode with `/v1/chat/completions` endpoint. The structured output parameters (`response_format`, `json_schema`) follow OpenAI's API specification.

3. **JSON Schema Draft-07 Compatibility:** Schemas provided follow JSON Schema Draft-07 specification. vLLM's guided decoding engine uses Outlines library which supports Draft-07 primitives. Schemas using Draft-2019-09+ features may cause fallback.

4. **Tool Schema Registry Available:** Task 007 (IToolSchemaRegistry) is complete and provides `GetSchema(toolName)` returning `ToolSchema` with `Parameters` property containing JSON Schema for tool arguments.

5. **Single Model Per Request:** Each chat request targets a single vLLM model. Schema enforcement is model-specific and cannot span multiple models in a single request.

6. **UTF-8 Encoding:** All JSON schemas and outputs use UTF-8 encoding. Non-UTF-8 schemas will cause encoding errors during transformation.

7. **Memory Availability:** Schema caching requires up to 10MB memory for 100+ cached schemas. Systems with less than 100MB available RAM may experience cache evictions.

### Operational Assumptions

8. **vLLM Server Responsive:** The vLLM server responds to capability queries within 5 seconds. Unresponsive servers trigger fallback mode with default capabilities.

9. **Network Latency < 100ms:** Network latency between Acode and vLLM server is under 100ms. Higher latency may cause capability detection timeouts.

10. **Schema Size Reasonable:** Schemas do not exceed 64KB. Larger schemas are rejected with `ACODE-VLM-SO-001` error.

11. **Nesting Depth ≤ 10:** Schema nesting does not exceed 10 levels. Deeper nesting triggers fallback or rejection depending on configuration.

12. **Fallback Retries Acceptable:** When fallback mode activates, up to 3 retry attempts are acceptable latency (3-9 seconds additional).

13. **Logs Collected:** DEBUG-level logs are collected for troubleshooting. Production deployments have log aggregation in place.

### Integration Assumptions

14. **VllmProvider Ready:** Task 006 (VllmProvider) is complete with `CompleteAsync` method accepting `ChatRequest` with `ResponseFormat` and `Tools` properties.

15. **VllmHttpClient Available:** Task 006.a (VllmHttpClient) provides `SendAsync` for HTTP communication with vLLM.

16. **ToolCall Type Defined:** Task 004.a defines `ToolCall` domain type with `Id`, `Name`, and `Arguments` properties.

17. **Configuration System Ready:** Task 001 provides `IConfigurationService` for reading `.agent/config.yml` settings.

18. **Logging Infrastructure Ready:** Task 002 provides `ILogger<T>` for structured logging with correlation IDs.

### Resource Assumptions

19. **CPU Available for Validation:** Schema validation adds < 1ms CPU time per request. Systems are not CPU-bound to the point where this causes noticeable delays.

20. **Concurrent Requests Handled:** Up to 50 concurrent requests can be processed with schema transformation without thread contention issues.

---

## Security Considerations

### Threat 1: Schema Injection Attack

**Description:** A malicious actor crafts a schema containing executable code or commands embedded in description fields, default values, or enum options. When the schema is logged or processed, the embedded payload executes.

**Attack Vector:** User provides tool definition with malicious schema: `{"type": "string", "description": "$(rm -rf /)", "default": "; DROP TABLE users;"}`. If descriptions are interpolated into shell commands or SQL queries, the payload executes.

**Impact:** Code execution on Acode host or vLLM server. Data loss. Privilege escalation.

**Likelihood:** Low (requires user-provided schemas and insecure interpolation).

**Mitigation:**
1. Never interpolate schema content into commands or queries
2. Sanitize schema content before logging (strip control characters)
3. Validate schema content against allowlists for known fields
4. Log schemas at DEBUG level only, never INFO or higher

**Residual Risk:** Negligible after mitigation.

---

### Threat 2: Denial of Service via Complex Schemas

**Description:** Attacker provides extremely complex schema with exponential validation cost (deeply nested allOf/anyOf, recursive $ref, large enum lists). Schema processing consumes excessive CPU/memory, blocking other requests.

**Attack Vector:** Schema with 1000-element enum, 50-level nesting, or circular $ref. Processing hangs or crashes the service.

**Impact:** Service unavailability for all users. Resource exhaustion on host.

**Likelihood:** Medium (easy to construct, requires schema submission capability).

**Mitigation:**
1. Enforce max nesting depth (10 levels)
2. Enforce max schema size (64KB)
3. Enforce max enum elements (100)
4. Timeout schema processing (100ms)
5. Reject unsupported constructs (allOf/anyOf/oneOf)

**Residual Risk:** Low after limits enforced.

---

### Threat 3: Information Disclosure via Error Messages

**Description:** Schema validation errors include sensitive schema content in error messages. Error messages are logged or returned to users, exposing internal schema structure or sample data.

**Attack Vector:** Attacker triggers validation error with schema containing API keys in default values. Error message includes "Expected default 'sk-1234secret' but got...".

**Impact:** Credential exposure. Internal structure disclosure.

**Likelihood:** Medium (error messages often include context).

**Mitigation:**
1. Sanitize error messages before returning to users
2. Truncate schema content in errors (max 100 chars)
3. Replace sensitive patterns (API keys) with redacted placeholders
4. Log full details only at DEBUG level with correlation ID

**Residual Risk:** Low after sanitization.

---

### Threat 4: Schema Bypass via Fallback Mode

**Description:** Attacker crafts requests that intentionally trigger fallback mode where schema validation is lenient. In fallback mode, malformed arguments are accepted after retries, bypassing strict validation.

**Attack Vector:** Send schema that causes vLLM to reject guided decoding. System falls back to retry-based validation. Provide arguments that pass lenient validation but fail security checks.

**Impact:** Invalid or malicious tool arguments accepted. Downstream tools execute with unexpected inputs.

**Likelihood:** Low (requires understanding of fallback triggers and validation gaps).

**Mitigation:**
1. Use strict validation in fallback mode (same as guided decoding)
2. Log all fallback activations for audit
3. Alert on excessive fallback rates (>5%)
4. Limit fallback retries to prevent exploit iteration

**Residual Risk:** Low after strict fallback validation.

---

### Threat 5: Model Capability Spoofing

**Description:** Attacker provides false capability information causing Acode to use incorrect structured output mode. This could cause requests to fail or bypass validation.

**Attack Vector:** Man-in-the-middle modifies vLLM capability response to claim guided decoding support when unavailable. Requests fail unpredictably.

**Impact:** Service reliability degradation. Potential validation bypass if fallback misconfigured.

**Likelihood:** Very Low (requires network interception).

**Mitigation:**
1. Validate capability responses against known model baselines
2. Cache capabilities to reduce attack window
3. Use TLS for vLLM communication
4. Fall back conservatively on unexpected responses

**Residual Risk:** Negligible after TLS and validation.

---

## Best Practices

### Schema Design

1. **Keep Schemas Shallow:** Limit nesting to 3-4 levels when possible. Deeply nested schemas are harder for models to follow and may trigger complexity limits.

2. **Use Explicit Types:** Always specify `type` for every property. Implicit types cause validation ambiguity.

3. **Prefer Required Over Optional:** Explicitly list required fields rather than relying on optional defaults. This gives the model clearer guidance.

4. **Include Descriptions:** Add `description` to each property. Models use descriptions as hints for generation.

5. **Use Enums for Fixed Values:** When a property has a fixed set of valid values, use `enum` constraint. This enables `guided_choice` mode for more reliable output.

6. **Avoid Complex Combinators:** `allOf`, `anyOf`, and `oneOf` are not fully supported. Use explicit schemas instead.

### Configuration

7. **Enable Structured Output for Production:** In production, enable structured output to eliminate retry latency and ensure reliability.

8. **Configure Fallback for Development:** In development, enable fallback mode to support diverse models and schema experimentation.

9. **Set Appropriate Retry Limits:** 3 retries is a good balance. More than 5 wastes resources on fundamentally broken schemas.

10. **Monitor Fallback Rates:** Track fallback activation. Rates above 5% indicate schema or model issues.

### Performance

11. **Cache Schema Transformations:** Schema transformation is deterministic. Cache results to avoid repeated processing.

12. **Cache Capability Detections:** Model capabilities don't change. Cache for the lifetime of the vLLM connection.

13. **Use Lazy Capability Detection:** Don't query capabilities until first request for a model. This avoids startup delays.

14. **Timeout Long Processing:** Set 100ms timeout on schema processing to prevent hangs.

### Reliability

15. **Test Schemas Independently:** Validate schemas against vLLM before integrating into tools. This catches compatibility issues early.

16. **Log Fallback Reasons:** Always log why fallback activated. This aids debugging and schema improvement.

17. **Handle vLLM Restarts:** Invalidate capability cache when vLLM connection is lost. Capabilities may change after restart.

18. **Graceful Degradation:** When structured output fails, fall back to retry-based validation rather than failing requests entirely.

---

## Troubleshooting

### Issue 1: Schema Too Complex Error

**Symptoms:**
- Error code: `ACODE-VLM-SO-001`
- Message: "Schema exceeds complexity limits"
- Request fails immediately

**Possible Causes:**
1. Schema nesting exceeds 10 levels
2. Schema size exceeds 64KB
3. Enum has more than 100 values
4. Uses unsupported constructs (allOf/anyOf)

**Solutions:**
1. Check schema depth: Count nesting levels from root to deepest property
2. Check schema size: `echo $schema | wc -c` (should be < 65536)
3. Simplify enums: Split large enums into multiple properties
4. Refactor combinators: Replace allOf with explicit merged schema
5. Split into multiple tools: If schema is inherently complex, split into separate tools with simpler schemas

**Verification:**
```bash
# Check schema after simplification
acode tools validate --schema path/to/schema.json

# Test with vLLM directly
curl -X POST http://vllm:8000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{"model": "test", "messages": [...], "response_format": {"type": "json_schema", "json_schema": {...}}}'
```

---

### Issue 2: Fallback Activated Unexpectedly

**Symptoms:**
- Log message: "Fallback activated for model X"
- Structured output not used
- Retry-based validation occurring

**Possible Causes:**
1. Model doesn't support guided decoding
2. vLLM version too old (< 0.4.0)
3. Schema rejected by vLLM
4. Capability detection failed

**Solutions:**
1. Check model support: `acode providers models vllm --capabilities`
2. Check vLLM version: `curl http://vllm:8000/version`
3. Check schema compatibility: Test schema directly with vLLM
4. Check capability cache: `acode providers cache clear vllm`

**Verification:**
```bash
# Force capability refresh
acode providers refresh vllm

# Test with simple schema first
acode chat --provider vllm --response-format json_object
```

---

### Issue 3: Validation Failed After Retries

**Symptoms:**
- Error code: `ACODE-VLM-SO-006`
- Message: "Max retries exceeded"
- All retry attempts failed

**Possible Causes:**
1. Schema is unachievable for model
2. Prompt doesn't guide toward schema
3. Model context too limited
4. Schema conflicts with prompt

**Solutions:**
1. Simplify schema: Reduce required fields
2. Improve prompt: Add explicit schema guidance in system prompt
3. Use more capable model: Larger models follow schemas better
4. Add examples: Include example output in prompt

**Verification:**
```bash
# Test with explicit example
acode chat --provider vllm --system "Return JSON: {\"name\": \"test\", \"value\": 42}"
```

---

### Issue 4: Guided Decoding Timeout

**Symptoms:**
- Error code: `ACODE-VLM-SO-003`
- Message: "Guided decoding timeout"
- Request takes longer than expected

**Possible Causes:**
1. Schema too complex for efficient decoding
2. vLLM server overloaded
3. Large output requested
4. Network issues

**Solutions:**
1. Simplify schema: Reduce complexity
2. Check vLLM load: `curl http://vllm:8000/health`
3. Reduce output size: Add `max_tokens` limit
4. Increase timeout: Adjust in configuration (not recommended)

**Verification:**
```bash
# Test with simple schema and short output
acode chat --provider vllm --max-tokens 100 --response-format json_object
```

---

### Issue 5: Capability Detection Returning Wrong Results

**Symptoms:**
- Model claimed to support structured output but fails
- Model claimed to not support but works when forced
- Inconsistent behavior

**Possible Causes:**
1. Stale capability cache
2. vLLM restarted with different configuration
3. Model updated on vLLM server
4. Capability detection query failed

**Solutions:**
1. Clear cache: `acode providers cache clear vllm`
2. Restart Acode: Force fresh capability detection
3. Check vLLM logs: Look for model loading messages
4. Manually override: Set capabilities in configuration

**Verification:**
```yaml
# Manual capability override in config
model:
  providers:
    vllm:
      models:
        your-model:
          structured_output:
            capabilities:
              json_schema: true
              guided_json: true
```

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

#### StructuredOutputConfigurationTests.cs

```csharp
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Configuration;
using Acode.Infrastructure.Vllm.StructuredOutput;

namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput;

public class StructuredOutputConfigurationTests
{
    [Fact]
    public void Should_Enable_By_Default()
    {
        // Arrange
        var config = new StructuredOutputConfiguration();
        
        // Act
        var enabled = config.IsEnabled("any-model");
        
        // Assert
        enabled.Should().BeTrue("structured output is enabled by default");
    }
    
    [Fact]
    public void Should_Disable_When_Configured()
    {
        // Arrange
        var config = new StructuredOutputConfiguration
        {
            Enabled = false
        };
        
        // Act
        var enabled = config.IsEnabled("any-model");
        
        // Assert
        enabled.Should().BeFalse("global disable should apply to all models");
    }
    
    [Fact]
    public void Should_Override_Per_Model()
    {
        // Arrange
        var config = new StructuredOutputConfiguration
        {
            Enabled = true,
            ModelOverrides = new Dictionary<string, ModelStructuredOutputConfig>
            {
                ["disabled-model"] = new() { Enabled = false }
            }
        };
        
        // Act
        var enabledDefault = config.IsEnabled("other-model");
        var enabledOverride = config.IsEnabled("disabled-model");
        
        // Assert
        enabledDefault.Should().BeTrue("default should be enabled");
        enabledOverride.Should().BeFalse("override should disable");
    }
    
    [Fact]
    public void Should_Override_From_Environment()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ACODE_VLLM_STRUCTURED_OUTPUT_ENABLED", "false");
        var configBuilder = new ConfigurationBuilder()
            .AddEnvironmentVariables("ACODE_");
        
        // Act
        var config = StructuredOutputConfiguration.FromConfiguration(configBuilder.Build());
        
        // Assert
        config.Enabled.Should().BeFalse("environment override should apply");
        
        // Cleanup
        Environment.SetEnvironmentVariable("ACODE_VLLM_STRUCTURED_OUTPUT_ENABLED", null);
    }
    
    [Fact]
    public void Should_Validate_Configuration()
    {
        // Arrange
        var config = new StructuredOutputConfiguration
        {
            Fallback = new FallbackConfiguration
            {
                MaxRetries = -1 // Invalid
            }
        };
        
        // Act
        var result = config.Validate();
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxRetries"));
    }
    
    [Fact]
    public void Should_Load_From_Yaml_Config()
    {
        // Arrange
        var yaml = """
            model:
              providers:
                vllm:
                  structured_output:
                    enabled: true
                    fallback:
                      max_retries: 5
            """;
        var configBuilder = new ConfigurationBuilder()
            .AddYamlStream(new MemoryStream(Encoding.UTF8.GetBytes(yaml)));
        
        // Act
        var config = StructuredOutputConfiguration.FromConfiguration(configBuilder.Build());
        
        // Assert
        config.Enabled.Should().BeTrue();
        config.Fallback.MaxRetries.Should().Be(5);
    }
}
```

#### SchemaTransformerTests.cs

```csharp
using System.Text.Json;
using FluentAssertions;
using Xunit;
using Acode.Infrastructure.Vllm.StructuredOutput.Schema;

namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput;

public class SchemaTransformerTests
{
    private readonly SchemaTransformer _transformer = new();
    
    [Fact]
    public void Should_Transform_Object_Schema()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": { "type": "string" },
                    "age": { "type": "integer" }
                },
                "required": ["name"]
            }
            """).RootElement;
        
        // Act
        var result = _transformer.Transform(schema);
        
        // Assert
        result.GetProperty("type").GetString().Should().Be("object");
        result.GetProperty("properties").GetProperty("name")
            .GetProperty("type").GetString().Should().Be("string");
        result.GetProperty("required").GetArrayLength().Should().Be(1);
    }
    
    [Fact]
    public void Should_Transform_Array_Schema()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "array",
                "items": { "type": "string" },
                "minItems": 1,
                "maxItems": 10
            }
            """).RootElement;
        
        // Act
        var result = _transformer.Transform(schema);
        
        // Assert
        result.GetProperty("type").GetString().Should().Be("array");
        result.GetProperty("items").GetProperty("type").GetString().Should().Be("string");
    }
    
    [Fact]
    public void Should_Handle_Enums()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "string",
                "enum": ["red", "green", "blue"]
            }
            """).RootElement;
        
        // Act
        var result = _transformer.Transform(schema);
        
        // Assert
        result.GetProperty("enum").GetArrayLength().Should().Be(3);
        result.GetProperty("enum")[0].GetString().Should().Be("red");
    }
    
    [Fact]
    public void Should_Handle_Required_Fields()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "id": { "type": "string" },
                    "name": { "type": "string" },
                    "optional": { "type": "string" }
                },
                "required": ["id", "name"]
            }
            """).RootElement;
        
        // Act
        var result = _transformer.Transform(schema);
        
        // Assert
        var required = result.GetProperty("required");
        required.GetArrayLength().Should().Be(2);
        required[0].GetString().Should().Be("id");
        required[1].GetString().Should().Be("name");
    }
    
    [Fact]
    public void Should_Handle_Nested_Objects()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "address": {
                        "type": "object",
                        "properties": {
                            "street": { "type": "string" },
                            "city": { "type": "string" }
                        }
                    }
                }
            }
            """).RootElement;
        
        // Act
        var result = _transformer.Transform(schema);
        
        // Assert
        result.GetProperty("properties").GetProperty("address")
            .GetProperty("properties").GetProperty("city")
            .GetProperty("type").GetString().Should().Be("string");
    }
    
    [Fact]
    public void Should_Inline_Refs()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "user": { "$ref": "#/$defs/User" }
                },
                "$defs": {
                    "User": {
                        "type": "object",
                        "properties": {
                            "name": { "type": "string" }
                        }
                    }
                }
            }
            """).RootElement;
        
        // Act
        var result = _transformer.Transform(schema);
        
        // Assert
        // $ref should be resolved and inlined
        result.GetProperty("properties").GetProperty("user")
            .GetProperty("type").GetString().Should().Be("object");
        result.TryGetProperty("$defs", out _).Should().BeFalse("$defs should be removed");
    }
    
    [Fact]
    public void Should_Preserve_Descriptions()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "description": "A user object",
                "properties": {
                    "name": { 
                        "type": "string",
                        "description": "The user's full name"
                    }
                }
            }
            """).RootElement;
        
        // Act
        var result = _transformer.Transform(schema);
        
        // Assert
        result.GetProperty("description").GetString().Should().Be("A user object");
        result.GetProperty("properties").GetProperty("name")
            .GetProperty("description").GetString().Should().Be("The user's full name");
    }
    
    [Fact]
    public void Should_Reject_Too_Deep_Nesting()
    {
        // Arrange
        var deepSchema = BuildDeepSchema(15); // 15 levels, limit is 10
        
        // Act
        var act = () => _transformer.Transform(deepSchema);
        
        // Assert
        act.Should().Throw<SchemaTooComplexException>()
            .WithMessage("*exceeds depth limit*");
    }
    
    [Fact]
    public void Should_Reject_Large_Schema()
    {
        // Arrange
        var largeSchema = JsonDocument.Parse($$"""
            {
                "type": "object",
                "properties": {
                    "data": { "type": "string", "description": "{{new string('x', 100000)}}" }
                }
            }
            """).RootElement;
        
        // Act
        var act = () => _transformer.Transform(largeSchema);
        
        // Assert
        act.Should().Throw<SchemaTooComplexException>()
            .WithMessage("*exceeds size limit*");
    }
    
    private static JsonElement BuildDeepSchema(int depth)
    {
        var json = "{\"type\": \"object\", \"properties\": {\"nested\": ";
        for (int i = 0; i < depth; i++)
        {
            json += "{\"type\": \"object\", \"properties\": {\"nested\": ";
        }
        json += "{\"type\": \"string\"}";
        for (int i = 0; i <= depth; i++)
        {
            json += "}}";
        }
        return JsonDocument.Parse(json).RootElement;
    }
}
```

#### FallbackHandlerTests.cs

```csharp
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Acode.Infrastructure.Vllm.StructuredOutput.Fallback;

namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput;

public class FallbackHandlerTests
{
    private readonly Mock<IOutputValidator> _validatorMock = new();
    private readonly Mock<ILogger<FallbackHandler>> _loggerMock = new();
    
    [Fact]
    public void Should_Trigger_On_Unavailable()
    {
        // Arrange
        var handler = CreateHandler();
        var context = new FallbackContext
        {
            Reason = FallbackReason.CapabilityUnavailable,
            ModelId = "test-model"
        };
        
        // Act
        var activated = handler.ShouldActivate(context);
        
        // Assert
        activated.Should().BeTrue();
    }
    
    [Fact]
    public void Should_Not_Trigger_When_Disabled()
    {
        // Arrange
        var config = new FallbackConfiguration { Enabled = false };
        var handler = CreateHandler(config);
        var context = new FallbackContext
        {
            Reason = FallbackReason.CapabilityUnavailable
        };
        
        // Act
        var activated = handler.ShouldActivate(context);
        
        // Assert
        activated.Should().BeFalse("fallback is disabled");
    }
    
    [Fact]
    public async Task Should_Validate_Output()
    {
        // Arrange
        var handler = CreateHandler();
        var schema = JsonDocument.Parse("""{"type": "object"}""").RootElement;
        var output = JsonDocument.Parse("""{"name": "test"}""").RootElement;
        
        _validatorMock.Setup(v => v.Validate(output, schema))
            .Returns(ValidationResult.Valid());
        
        // Act
        var result = await handler.HandleAsync(output, schema, CancellationToken.None);
        
        // Assert
        result.Success.Should().BeTrue();
        _validatorMock.Verify(v => v.Validate(output, schema), Times.Once);
    }
    
    [Fact]
    public async Task Should_Retry_On_Invalid()
    {
        // Arrange
        var handler = CreateHandler();
        var schema = JsonDocument.Parse("""{"type": "object", "required": ["name"]}""").RootElement;
        var invalidOutput = JsonDocument.Parse("""{}""").RootElement;
        var validOutput = JsonDocument.Parse("""{"name": "test"}""").RootElement;
        
        var callCount = 0;
        _validatorMock.Setup(v => v.Validate(It.IsAny<JsonElement>(), schema))
            .Returns(() =>
            {
                callCount++;
                return callCount < 2 
                    ? ValidationResult.Invalid("missing required: name")
                    : ValidationResult.Valid();
            });
        
        // Act
        var result = await handler.HandleWithRetryAsync(
            () => Task.FromResult(callCount < 2 ? invalidOutput : validOutput),
            schema,
            CancellationToken.None);
        
        // Assert
        result.Success.Should().BeTrue();
        callCount.Should().Be(2);
    }
    
    [Fact]
    public async Task Should_Limit_Retries()
    {
        // Arrange
        var config = new FallbackConfiguration { MaxRetries = 3 };
        var handler = CreateHandler(config);
        var schema = JsonDocument.Parse("""{"type": "object", "required": ["name"]}""").RootElement;
        var invalidOutput = JsonDocument.Parse("""{}""").RootElement;
        
        _validatorMock.Setup(v => v.Validate(It.IsAny<JsonElement>(), schema))
            .Returns(ValidationResult.Invalid("missing required: name"));
        
        var retryCount = 0;
        
        // Act
        var act = async () => await handler.HandleWithRetryAsync(
            () => { retryCount++; return Task.FromResult(invalidOutput); },
            schema,
            CancellationToken.None);
        
        // Assert
        await act.Should().ThrowAsync<ValidationFailedException>()
            .WithMessage("*Max retries*");
        retryCount.Should().Be(4); // Initial + 3 retries
    }
    
    [Fact]
    public void Should_Log_Fallback_Reason()
    {
        // Arrange
        var handler = CreateHandler();
        var context = new FallbackContext
        {
            Reason = FallbackReason.SchemaRejected,
            ModelId = "test-model",
            Error = "Schema too complex"
        };
        
        // Act
        handler.ActivateFallback(context);
        
        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SchemaRejected")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    private FallbackHandler CreateHandler(FallbackConfiguration? config = null)
    {
        return new FallbackHandler(
            _validatorMock.Object,
            config ?? new FallbackConfiguration(),
            _loggerMock.Object);
    }
}
```

#### CapabilityDetectorTests.cs

```csharp
using FluentAssertions;
using Moq;
using Xunit;
using Acode.Infrastructure.Vllm.StructuredOutput.Capability;

namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput;

public class CapabilityDetectorTests
{
    private readonly Mock<IVllmClient> _clientMock = new();
    private readonly CapabilityCache _cache = new(TimeSpan.FromMinutes(5));
    
    [Fact]
    public async Task Should_Detect_Support()
    {
        // Arrange
        _clientMock.Setup(c => c.GetModelInfoAsync("test-model", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VllmModelInfo
            {
                Id = "test-model",
                SupportedFeatures = new[] { "guided_json", "json_schema" }
            });
        
        var detector = new CapabilityDetector(_clientMock.Object, _cache);
        
        // Act
        var capabilities = await detector.DetectAsync("test-model", CancellationToken.None);
        
        // Assert
        capabilities.SupportsGuidedJson.Should().BeTrue();
        capabilities.SupportsJsonSchema.Should().BeTrue();
    }
    
    [Fact]
    public async Task Should_Cache_Results()
    {
        // Arrange
        _clientMock.Setup(c => c.GetModelInfoAsync("test-model", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VllmModelInfo
            {
                Id = "test-model",
                SupportedFeatures = new[] { "guided_json" }
            });
        
        var detector = new CapabilityDetector(_clientMock.Object, _cache);
        
        // Act
        await detector.DetectAsync("test-model", CancellationToken.None);
        await detector.DetectAsync("test-model", CancellationToken.None);
        
        // Assert
        _clientMock.Verify(
            c => c.GetModelInfoAsync("test-model", It.IsAny<CancellationToken>()),
            Times.Once,
            "second call should use cache");
    }
    
    [Fact]
    public async Task Should_Handle_Unknown_Models()
    {
        // Arrange
        _clientMock.Setup(c => c.GetModelInfoAsync("unknown-model", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Model not found"));
        
        var detector = new CapabilityDetector(_clientMock.Object, _cache);
        
        // Act
        var capabilities = await detector.DetectAsync("unknown-model", CancellationToken.None);
        
        // Assert
        capabilities.SupportsGuidedJson.Should().BeFalse("unknown models should be conservative");
        capabilities.SupportsJsonSchema.Should().BeFalse();
        capabilities.FallbackRequired.Should().BeTrue();
    }
    
    [Fact]
    public async Task Should_Refresh_On_Cache_Expiry()
    {
        // Arrange
        var shortCache = new CapabilityCache(TimeSpan.FromMilliseconds(50));
        _clientMock.Setup(c => c.GetModelInfoAsync("test-model", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VllmModelInfo { Id = "test-model" });
        
        var detector = new CapabilityDetector(_clientMock.Object, shortCache);
        
        // Act
        await detector.DetectAsync("test-model", CancellationToken.None);
        await Task.Delay(100); // Wait for cache expiry
        await detector.DetectAsync("test-model", CancellationToken.None);
        
        // Assert
        _clientMock.Verify(
            c => c.GetModelInfoAsync("test-model", It.IsAny<CancellationToken>()),
            Times.Exactly(2),
            "should refresh after expiry");
    }
}
```

#### OutputValidatorTests.cs

```csharp
using System.Text.Json;
using FluentAssertions;
using Xunit;
using Acode.Infrastructure.Vllm.StructuredOutput.Fallback;

namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput;

public class OutputValidatorTests
{
    private readonly OutputValidator _validator = new();
    
    [Fact]
    public void Should_Validate_Valid_JSON()
    {
        // Arrange
        var schema = JsonDocument.Parse("""{"type": "object"}""").RootElement;
        var output = JsonDocument.Parse("""{"name": "test"}""").RootElement;
        
        // Act
        var result = _validator.Validate(output, schema);
        
        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
    
    [Fact]
    public void Should_Detect_Invalid_Type()
    {
        // Arrange
        var schema = JsonDocument.Parse("""{"type": "object"}""").RootElement;
        var output = JsonDocument.Parse("""["array"]""").RootElement;
        
        // Act
        var result = _validator.Validate(output, schema);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("Expected object"));
    }
    
    [Fact]
    public void Should_Detect_Missing_Required()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": { "type": "string" },
                    "id": { "type": "string" }
                },
                "required": ["name", "id"]
            }
            """).RootElement;
        var output = JsonDocument.Parse("""{"name": "test"}""").RootElement;
        
        // Act
        var result = _validator.Validate(output, schema);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Path == "id" && e.Message.Contains("required"));
    }
    
    [Fact]
    public void Should_Handle_Optional_Fields()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": { "type": "string" },
                    "optional": { "type": "string" }
                },
                "required": ["name"]
            }
            """).RootElement;
        var output = JsonDocument.Parse("""{"name": "test"}""").RootElement;
        
        // Act
        var result = _validator.Validate(output, schema);
        
        // Assert
        result.IsValid.Should().BeTrue("optional field can be omitted");
    }
    
    [Fact]
    public void Should_Report_Clear_Errors()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "count": { "type": "integer" }
                }
            }
            """).RootElement;
        var output = JsonDocument.Parse("""{"count": "not-a-number"}""").RootElement;
        
        // Act
        var result = _validator.Validate(output, schema);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Path.Should().Be("count");
        result.Errors[0].Message.Should().Contain("Expected integer");
    }
}
```

### Integration Tests

#### StructuredOutputIntegrationTests.cs

```csharp
using FluentAssertions;
using Xunit;
using Acode.Infrastructure.Vllm;
using Acode.Domain.Inference;

namespace Acode.Integration.Tests.Vllm;

[Collection("VllmIntegration")]
public class StructuredOutputIntegrationTests : IClassFixture<VllmTestFixture>
{
    private readonly VllmProvider _provider;
    
    public StructuredOutputIntegrationTests(VllmTestFixture fixture)
    {
        _provider = fixture.CreateProvider();
    }
    
    [Fact]
    public async Task Should_Generate_Valid_JSON()
    {
        // Arrange
        var request = new ChatRequest
        {
            Messages = new[] { new ChatMessage(MessageRole.User, "Return a JSON object with name and age") },
            ResponseFormat = new ResponseFormat { Type = "json_object" }
        };
        
        // Act
        var response = await _provider.CompleteAsync(request, CancellationToken.None);
        
        // Assert
        response.Should().NotBeNull();
        var content = response.Message?.Content;
        var act = () => JsonDocument.Parse(content!);
        act.Should().NotThrow("response should be valid JSON");
    }
    
    [Fact]
    public async Task Should_Match_Schema()
    {
        // Arrange
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": { "type": "string" },
                    "age": { "type": "integer" }
                },
                "required": ["name", "age"]
            }
            """).RootElement;
        
        var request = new ChatRequest
        {
            Messages = new[] { new ChatMessage(MessageRole.User, "Return person info") },
            ResponseFormat = new ResponseFormat
            {
                Type = "json_schema",
                JsonSchema = new JsonSchemaFormat
                {
                    Name = "person",
                    Schema = schema
                }
            }
        };
        
        // Act
        var response = await _provider.CompleteAsync(request, CancellationToken.None);
        
        // Assert
        var content = response.Message?.Content;
        var json = JsonDocument.Parse(content!).RootElement;
        json.TryGetProperty("name", out var name).Should().BeTrue();
        name.ValueKind.Should().Be(JsonValueKind.String);
        json.TryGetProperty("age", out var age).Should().BeTrue();
        age.ValueKind.Should().Be(JsonValueKind.Number);
    }
    
    [Fact]
    public async Task Should_Handle_Tool_Calls()
    {
        // Arrange
        var readFileTool = new ToolDefinition
        {
            Name = "read_file",
            Description = "Read a file",
            Parameters = JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "path": { "type": "string" }
                    },
                    "required": ["path"]
                }
                """).RootElement
        };
        
        var request = new ChatRequest
        {
            Messages = new[] { new ChatMessage(MessageRole.User, "Read the file test.txt") },
            Tools = new[] { readFileTool },
            ToolChoice = "auto"
        };
        
        // Act
        var response = await _provider.CompleteAsync(request, CancellationToken.None);
        
        // Assert
        response.ToolCalls.Should().HaveCount(1);
        var toolCall = response.ToolCalls[0];
        toolCall.Name.Should().Be("read_file");
        toolCall.Arguments.TryGetProperty("path", out var path).Should().BeTrue();
        path.ValueKind.Should().Be(JsonValueKind.String);
    }
    
    [Fact]
    public async Task Should_Fallback_Gracefully()
    {
        // Arrange - Use schema that forces fallback
        var complexSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "allOf": [
                    { "properties": { "a": { "type": "string" } } },
                    { "properties": { "b": { "type": "string" } } }
                ]
            }
            """).RootElement;
        
        var request = new ChatRequest
        {
            Messages = new[] { new ChatMessage(MessageRole.User, "Return a and b") },
            ResponseFormat = new ResponseFormat
            {
                Type = "json_schema",
                JsonSchema = new JsonSchemaFormat { Name = "test", Schema = complexSchema }
            }
        };
        
        // Act
        var response = await _provider.CompleteAsync(request, CancellationToken.None);
        
        // Assert - Should succeed via fallback
        response.Should().NotBeNull();
        var content = response.Message?.Content;
        content.Should().NotBeNullOrEmpty();
    }
}
```

### Performance Tests

#### StructuredOutputBenchmarks.cs

```csharp
using BenchmarkDotNet.Attributes;
using System.Text.Json;
using Acode.Infrastructure.Vllm.StructuredOutput.Schema;
using Acode.Infrastructure.Vllm.StructuredOutput.Capability;
using Acode.Infrastructure.Vllm.StructuredOutput.Fallback;

namespace Acode.Performance.Tests.Vllm;

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class StructuredOutputBenchmarks
{
    private SchemaTransformer _transformer = null!;
    private JsonElement _simpleSchema;
    private JsonElement _complexSchema;
    private CapabilityCache _cache = null!;
    private OutputValidator _validator = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _transformer = new SchemaTransformer();
        _cache = new CapabilityCache(TimeSpan.FromMinutes(5));
        _validator = new OutputValidator();
        
        _simpleSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": { "type": "string" },
                    "age": { "type": "integer" }
                },
                "required": ["name"]
            }
            """).RootElement;
        
        _complexSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "user": {
                        "type": "object",
                        "properties": {
                            "profile": {
                                "type": "object",
                                "properties": {
                                    "name": { "type": "string" },
                                    "email": { "type": "string" },
                                    "settings": {
                                        "type": "object",
                                        "properties": {
                                            "theme": { "type": "string" },
                                            "notifications": { "type": "boolean" }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    "items": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "id": { "type": "integer" },
                                "data": { "type": "string" }
                            }
                        }
                    }
                }
            }
            """).RootElement;
    }
    
    [Benchmark(Description = "Transform simple schema")]
    public JsonElement Benchmark_Schema_Transformation_Simple()
    {
        return _transformer.Transform(_simpleSchema);
    }
    
    [Benchmark(Description = "Transform complex schema")]
    public JsonElement Benchmark_Schema_Transformation_Complex()
    {
        return _transformer.Transform(_complexSchema);
    }
    
    [Benchmark(Description = "Capability cache lookup")]
    public ModelCapabilities? Benchmark_Capability_Cache_Hit()
    {
        _cache.Set("test-model", new ModelCapabilities { SupportsGuidedJson = true });
        return _cache.Get("test-model");
    }
    
    [Benchmark(Description = "Validate simple output")]
    public ValidationResult Benchmark_Validation_Simple()
    {
        var output = JsonDocument.Parse("""{"name": "test", "age": 25}""").RootElement;
        return _validator.Validate(output, _simpleSchema);
    }
    
    [Benchmark(Description = "Validate complex output")]
    public ValidationResult Benchmark_Validation_Complex()
    {
        var output = JsonDocument.Parse("""
            {
                "user": {
                    "profile": {
                        "name": "test",
                        "email": "test@example.com",
                        "settings": {
                            "theme": "dark",
                            "notifications": true
                        }
                    }
                },
                "items": [
                    { "id": 1, "data": "a" },
                    { "id": 2, "data": "b" }
                ]
            }
            """).RootElement;
        return _validator.Validate(output, _complexSchema);
    }
}
```

---

## User Verification Steps

### Scenario 1: Basic JSON Object Mode

**Objective:** Verify that response_format="json_object" produces valid JSON.

**Prerequisites:**
- vLLM server running at localhost:8000
- Acode configured with vllm provider

**Steps:**

```bash
# Step 1: Send a request with json_object format
acode chat --provider vllm --response-format json_object \
  --message "List three programming languages with their release years"

# Expected output: Valid JSON object (parseable)
# Example: {"languages": [{"name": "Python", "year": 1991}, ...]}
```

```bash
# Step 2: Verify JSON validity
acode chat --provider vllm --response-format json_object \
  --message "Return user info" --output response.json

# Validate with jq
cat response.json | jq .
# Should not error
```

```bash
# Step 3: Check logs for structured output activation
acode logs --tail 10 --filter "structured_output=true"
# Should see: "Structured output mode: json_object"
```

**Expected Results:**
- Response is valid JSON (no parse errors)
- Logs show structured output activated
- No validation errors

---

### Scenario 2: JSON Schema Enforcement

**Objective:** Verify that json_schema mode produces output matching the exact schema.

**Prerequisites:**
- Schema file prepared at test-schema.json

**Steps:**

```bash
# Step 1: Create a test schema
cat > test-schema.json << 'EOF'
{
  "type": "object",
  "properties": {
    "name": { "type": "string" },
    "age": { "type": "integer", "minimum": 0 },
    "email": { "type": "string", "format": "email" }
  },
  "required": ["name", "age"]
}
EOF
```

```bash
# Step 2: Send request with schema enforcement
acode chat --provider vllm \
  --response-format json_schema \
  --schema-file test-schema.json \
  --message "Generate a user profile"

# Expected: Output exactly matches schema
# {"name": "John Doe", "age": 30, "email": "john@example.com"}
```

```bash
# Step 3: Verify all required fields present
acode chat --provider vllm \
  --response-format json_schema \
  --schema-file test-schema.json \
  --message "Generate profile" --output result.json

jq 'has("name") and has("age")' result.json
# Should output: true
```

```bash
# Step 4: Verify type correctness
jq '.age | type' result.json
# Should output: "number" (not "string")
```

**Expected Results:**
- Output matches schema structure exactly
- Required fields always present
- Types are correct (integer, not string)

---

### Scenario 3: Tool Call Argument Validation

**Objective:** Verify that tool call arguments match the tool's parameter schema.

**Steps:**

```bash
# Step 1: Define a tool with specific schema
acode tools define read_file --params '{"type":"object","properties":{"path":{"type":"string"},"maxLines":{"type":"integer"}},"required":["path"]}'
```

```bash
# Step 2: Invoke tool through chat
acode chat --provider vllm --tools read_file \
  --message "Read the file /src/main.cs with max 100 lines"

# Expected: Tool call with valid arguments
# Tool: read_file, Arguments: {"path": "/src/main.cs", "maxLines": 100}
```

```bash
# Step 3: Verify argument types
acode chat --provider vllm --tools read_file \
  --message "Read test.txt" --show-tool-calls

# Verify: "path" is string, "maxLines" (if present) is integer
```

```bash
# Step 4: Test with multiple tools
acode chat --provider vllm --tools read_file,write_file,list_directory \
  --message "List files in /src then read main.cs"

# Verify: Both tool calls have valid arguments
```

**Expected Results:**
- Tool arguments are valid JSON
- Argument types match schema
- Required fields always present
- No validation errors

---

### Scenario 4: Nested Schema Enforcement

**Objective:** Verify that deeply nested schemas are enforced correctly.

**Steps:**

```bash
# Step 1: Create nested schema
cat > nested-schema.json << 'EOF'
{
  "type": "object",
  "properties": {
    "user": {
      "type": "object",
      "properties": {
        "profile": {
          "type": "object",
          "properties": {
            "name": { "type": "string" },
            "contact": {
              "type": "object",
              "properties": {
                "email": { "type": "string" },
                "phone": { "type": "string" }
              }
            }
          },
          "required": ["name"]
        }
      },
      "required": ["profile"]
    }
  },
  "required": ["user"]
}
EOF
```

```bash
# Step 2: Send request with nested schema
acode chat --provider vllm \
  --response-format json_schema \
  --schema-file nested-schema.json \
  --message "Generate user with profile and contact"

# Expected: All nested levels correct
```

```bash
# Step 3: Verify nested structure
acode chat --provider vllm \
  --response-format json_schema \
  --schema-file nested-schema.json \
  --message "Generate user" --output nested.json

jq '.user.profile.name' nested.json
# Should output string value

jq '.user.profile.contact | type' nested.json
# Should output "object" (if present)
```

**Expected Results:**
- All nesting levels present
- Types correct at each level
- Required fields enforced at each level

---

### Scenario 5: Fallback Activation

**Objective:** Verify that fallback activates when structured output is unavailable and works correctly.

**Steps:**

```bash
# Step 1: Disable structured output for a model
cat >> ~/.agent/config.yml << 'EOF'
model:
  providers:
    vllm:
      models:
        test-fallback-model:
          structured_output:
            enabled: false
EOF
```

```bash
# Step 2: Send request with disabled model
acode chat --provider vllm --model test-fallback-model \
  --response-format json_schema \
  --schema-file test-schema.json \
  --message "Generate user"

# Should succeed via fallback
```

```bash
# Step 3: Check logs for fallback
acode logs --tail 10 --filter "fallback=true"
# Should see: "Fallback activated for model test-fallback-model"
# Should see: "Reason: structured_output_disabled"
```

```bash
# Step 4: Verify output still validated
acode chat --provider vllm --model test-fallback-model \
  --response-format json_schema \
  --schema-file test-schema.json \
  --message "Generate user" --output fallback.json

jq . fallback.json
# Should be valid JSON matching schema
```

**Expected Results:**
- Fallback activates when structured output disabled
- Fallback reason logged
- Output still validates against schema

---

### Scenario 6: Retry on Validation Failure

**Objective:** Verify that fallback mode retries on validation failure.

**Steps:**

```bash
# Step 1: Enable fallback retry logging
export ACODE_LOG_LEVEL=DEBUG
```

```bash
# Step 2: Use strict schema with fallback model
acode chat --provider vllm --model test-fallback-model \
  --response-format json_schema \
  --schema-file test-schema.json \
  --message "Generate user with name and age"
```

```bash
# Step 3: Check for retry attempts
acode logs --filter "validation_retry"
# May see: "Validation failed, retry attempt 1/3"
# May see: "Validation succeeded on retry 2"
```

```bash
# Step 4: Verify final output is valid
# If retries succeeded, output should be valid
# If retries exhausted, should see ACODE-VLM-SO-006 error
```

**Expected Results:**
- Retries occur on validation failure (in fallback mode)
- Retry count logged
- Eventually succeeds or returns clear error

---

### Scenario 7: Schema Too Complex Error

**Objective:** Verify that overly complex schemas produce helpful error messages.

**Steps:**

```bash
# Step 1: Create excessively complex schema (11+ levels)
python3 << 'EOF'
schema = {"type": "object", "properties": {}}
current = schema["properties"]
for i in range(12):
    current[f"level{i}"] = {"type": "object", "properties": {}}
    current = current[f"level{i}"]["properties"]
current["value"] = {"type": "string"}
import json
print(json.dumps(schema))
EOF > complex-schema.json
```

```bash
# Step 2: Send request with complex schema
acode chat --provider vllm \
  --response-format json_schema \
  --schema-file complex-schema.json \
  --message "Generate deep nested object"

# Expected: Error with code ACODE-VLM-SO-001
# Expected: Message includes "exceeds depth limit"
```

```bash
# Step 3: Verify error provides guidance
# Error should include:
# - Error code: ACODE-VLM-SO-001
# - Depth limit exceeded (max 10)
# - Path to deepest property
# - Suggestion to simplify
```

**Expected Results:**
- Clear error code (ACODE-VLM-SO-001)
- Error message includes actual vs limit
- Suggestion for remediation

---

### Scenario 8: Capability Caching

**Objective:** Verify that model capabilities are cached to avoid repeated vLLM queries.

**Steps:**

```bash
# Step 1: Clear capability cache
acode providers cache clear vllm
```

```bash
# Step 2: First request (triggers capability detection)
time acode chat --provider vllm \
  --response-format json_schema \
  --schema-file test-schema.json \
  --message "Generate user" 2>&1 | head -1
# Note the time
```

```bash
# Step 3: Check logs for capability query
acode logs --tail 5 --filter "capability_detection"
# Should see: "Querying vLLM for model capabilities"
```

```bash
# Step 4: Second request (uses cache)
time acode chat --provider vllm \
  --response-format json_schema \
  --schema-file test-schema.json \
  --message "Generate another user" 2>&1 | head -1
# Should be faster (no capability query)
```

```bash
# Step 5: Verify no second capability query
acode logs --tail 5 --filter "capability_detection"
# Should NOT see new query since step 3
```

**Expected Results:**
- First request queries vLLM for capabilities
- Second request uses cached capabilities
- No redundant HTTP calls

---

### Scenario 9: Performance Overhead Measurement

**Objective:** Verify that structured output overhead is < 5% of generation time.

**Steps:**

```bash
# Step 1: Baseline without structured output
acode chat --provider vllm \
  --response-format text \
  --message "Write a 200 word story" \
  --timing

# Note: generation_time_ms
```

```bash
# Step 2: With structured output (json_object)
acode chat --provider vllm \
  --response-format json_object \
  --message "Write a story as JSON with title, content, word_count" \
  --timing

# Note: generation_time_ms
```

```bash
# Step 3: Calculate overhead
# overhead = (structured_time - baseline_time) / baseline_time * 100
# Should be < 5%
```

```bash
# Step 4: With json_schema (more constraint)
acode chat --provider vllm \
  --response-format json_schema \
  --schema-file story-schema.json \
  --message "Write a story" \
  --timing

# Should still be < 5% overhead vs baseline
```

**Expected Results:**
- Structured output overhead < 5%
- json_schema overhead similar to json_object
- Overhead consistent across runs

---

### Scenario 10: End-to-End Tool Calling Workflow

**Objective:** Verify complete tool calling workflow with structured output from start to finish.

**Steps:**

```bash
# Step 1: Create a workspace with tools
mkdir test-workspace && cd test-workspace
echo "# Test Project" > README.md
acode init --tools read_file,write_file,list_directory
```

```bash
# Step 2: Execute multi-tool task
acode run --provider vllm --structured-output \
  --task "List files in current directory, then read README.md"

# Expected:
# 1. Tool call: list_directory with valid args
# 2. Tool result returned
# 3. Tool call: read_file with valid args
# 4. Tool result returned
# 5. Final response
```

```bash
# Step 3: Verify all tool calls were valid
acode run show --last --tool-calls

# All tool calls should show:
# - Valid JSON arguments
# - Schema validation: passed
# - Retries: 0
```

```bash
# Step 4: Check structured output metrics
acode providers metrics vllm

# Should show:
# - structured_output_enabled: true
# - validation_success_rate: 100%
# - fallback_rate: 0% (or low)
# - average_overhead_ms: < 50
```

**Expected Results:**
- All tool calls have valid arguments
- No validation retries needed
- Workflow completes successfully
- Metrics show high reliability

---

## Implementation Prompt for Claude

### Implementation Overview

This task implements structured output enforcement integration for the vLLM provider, enabling deterministic JSON generation that conforms to specified schemas. The implementation uses vLLM's guided decoding capabilities to eliminate the need for retry-based validation.

**What You'll Build:**
- StructuredOutputConfiguration: Configuration for structured output behavior
- SchemaTransformer: Transforms tool schemas to vLLM format
- CapabilityDetector: Detects model structured output capabilities
- FallbackHandler: Handles fallback when structured output unavailable
- OutputValidator: Validates output against schemas in fallback mode
- StructuredOutputHandler: Orchestrates structured output enforcement

### Prerequisites

**Required:**
- .NET 8 SDK installed
- Task 006 (VllmProvider) complete
- Task 007 (Tool Schema Registry) complete
- Task 001 (Configuration Service) complete

**NuGet Packages:**
- System.Text.Json (included in .NET 8)
- JsonSchema.Net (for schema validation)
- Microsoft.Extensions.Caching.Memory

### Step 1: Create Directory Structure

```
src/Acode.Infrastructure/Vllm/StructuredOutput/
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
│   ├── CapabilityCache.cs
│   └── ModelCapabilities.cs
├── Fallback/
│   ├── FallbackHandler.cs
│   ├── FallbackContext.cs
│   ├── FallbackConfiguration.cs
│   └── OutputValidator.cs
└── Exceptions/
    ├── StructuredOutputException.cs
    ├── SchemaTooComplexException.cs
    └── ValidationFailedException.cs
```

### Step 2: Implement Configuration

#### StructuredOutputConfiguration.cs

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace Acode.Infrastructure.Vllm.StructuredOutput;

/// <summary>
/// Configuration for vLLM structured output enforcement.
/// </summary>
public sealed class StructuredOutputConfiguration
{
    /// <summary>
    /// Global enable/disable for structured output.
    /// Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Fallback configuration when structured output unavailable.
    /// </summary>
    public FallbackConfiguration Fallback { get; set; } = new();
    
    /// <summary>
    /// Schema processing configuration.
    /// </summary>
    public SchemaConfiguration Schema { get; set; } = new();
    
    /// <summary>
    /// Per-model configuration overrides.
    /// </summary>
    public Dictionary<string, ModelStructuredOutputConfig> ModelOverrides { get; set; } = new();
    
    /// <summary>
    /// Check if structured output is enabled for a specific model.
    /// </summary>
    public bool IsEnabled(string modelId)
    {
        if (!Enabled)
        {
            return false;
        }
        
        if (ModelOverrides.TryGetValue(modelId, out var modelConfig))
        {
            return modelConfig.Enabled;
        }
        
        return true;
    }
    
    /// <summary>
    /// Get fallback configuration for a specific model.
    /// </summary>
    public FallbackConfiguration GetFallbackConfig(string modelId)
    {
        if (ModelOverrides.TryGetValue(modelId, out var modelConfig) &&
            modelConfig.Fallback is not null)
        {
            return modelConfig.Fallback;
        }
        
        return Fallback;
    }
    
    /// <summary>
    /// Validate configuration values.
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();
        
        if (Fallback.MaxRetries < 0 || Fallback.MaxRetries > 10)
        {
            errors.Add("MaxRetries must be between 0 and 10");
        }
        
        if (Schema.MaxDepth < 1 || Schema.MaxDepth > 20)
        {
            errors.Add("Schema.MaxDepth must be between 1 and 20");
        }
        
        if (Schema.MaxSizeBytes < 1024 || Schema.MaxSizeBytes > 1_048_576)
        {
            errors.Add("Schema.MaxSizeBytes must be between 1KB and 1MB");
        }
        
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
    
    /// <summary>
    /// Load configuration from IConfiguration.
    /// </summary>
    public static StructuredOutputConfiguration FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("model:providers:vllm:structured_output");
        var config = new StructuredOutputConfiguration();
        
        if (section.Exists())
        {
            section.Bind(config);
        }
        
        // Environment variable overrides
        var envEnabled = Environment.GetEnvironmentVariable("ACODE_VLLM_STRUCTURED_OUTPUT_ENABLED");
        if (envEnabled is not null)
        {
            config.Enabled = bool.Parse(envEnabled);
        }
        
        return config;
    }
}

/// <summary>
/// Per-model structured output configuration.
/// </summary>
public sealed class ModelStructuredOutputConfig
{
    public bool Enabled { get; set; } = true;
    public FallbackConfiguration? Fallback { get; set; }
    public string[]? SupportedModes { get; set; }
}

/// <summary>
/// Schema processing configuration.
/// </summary>
public sealed class SchemaConfiguration
{
    public int MaxDepth { get; set; } = 10;
    public int MaxSizeBytes { get; set; } = 65536;
    public int MaxEnumElements { get; set; } = 100;
    public int CacheSize { get; set; } = 100;
    public int ProcessingTimeoutMs { get; set; } = 100;
}

/// <summary>
/// Validation result container.
/// </summary>
public sealed class ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
```

#### FallbackConfiguration.cs

```csharp
namespace Acode.Infrastructure.Vllm.StructuredOutput.Fallback;

/// <summary>
/// Configuration for fallback behavior when structured output is unavailable.
/// </summary>
public sealed class FallbackConfiguration
{
    /// <summary>
    /// Whether fallback is enabled.
    /// Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Maximum retry attempts in fallback mode.
    /// Default: 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Validation mode in fallback.
    /// "strict" = same as guided decoding, "lenient" = allow extra fields.
    /// Default: strict.
    /// </summary>
    public string ValidationMode { get; set; } = "strict";
    
    /// <summary>
    /// Delay between retry attempts in milliseconds.
    /// Default: 100.
    /// </summary>
    public int RetryDelayMs { get; set; } = 100;
}
```

### Step 3: Implement SchemaTransformer

```csharp
using System.Text.Json;
using Acode.Infrastructure.Vllm.StructuredOutput.Exceptions;

namespace Acode.Infrastructure.Vllm.StructuredOutput.Schema;

/// <summary>
/// Transforms tool schemas to vLLM's expected format.
/// Handles $ref resolution, depth limits, and size limits.
/// </summary>
public sealed class SchemaTransformer
{
    private readonly int _maxDepth;
    private readonly int _maxSize;
    private readonly int _timeoutMs;
    
    public SchemaTransformer(
        int maxDepth = 10,
        int maxSize = 65536,
        int timeoutMs = 100)
    {
        _maxDepth = maxDepth;
        _maxSize = maxSize;
        _timeoutMs = timeoutMs;
    }
    
    /// <summary>
    /// Transform a schema to vLLM format.
    /// </summary>
    public JsonElement Transform(JsonElement schema)
    {
        // Check size limit
        var schemaJson = schema.GetRawText();
        if (schemaJson.Length > _maxSize)
        {
            throw new SchemaTooComplexException(
                $"Schema exceeds size limit ({schemaJson.Length} > {_maxSize} bytes)",
                "ACODE-VLM-SO-001");
        }
        
        using var cts = new CancellationTokenSource(_timeoutMs);
        
        try
        {
            // Resolve $refs and transform
            var resolved = ResolveRefs(schema, schema, new HashSet<string>());
            
            // Check depth limit
            var depth = CalculateDepth(resolved);
            if (depth > _maxDepth)
            {
                throw new SchemaTooComplexException(
                    $"Schema exceeds depth limit ({depth} > {_maxDepth} levels)",
                    "ACODE-VLM-SO-001");
            }
            
            return resolved;
        }
        catch (OperationCanceledException)
        {
            throw new SchemaTooComplexException(
                $"Schema processing timeout ({_timeoutMs}ms exceeded)",
                "ACODE-VLM-SO-003");
        }
    }
    
    private JsonElement ResolveRefs(
        JsonElement element,
        JsonElement rootSchema,
        HashSet<string> visited)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return element;
        }
        
        // Check for $ref
        if (element.TryGetProperty("$ref", out var refProp))
        {
            var refPath = refProp.GetString();
            if (refPath is null || !refPath.StartsWith("#/"))
            {
                throw new SchemaTooComplexException(
                    $"Only local $ref supported: {refPath}",
                    "ACODE-VLM-SO-002");
            }
            
            if (visited.Contains(refPath))
            {
                throw new SchemaTooComplexException(
                    $"Circular $ref detected: {refPath}",
                    "ACODE-VLM-SO-002");
            }
            
            visited.Add(refPath);
            var resolved = ResolveRefPath(rootSchema, refPath);
            return ResolveRefs(resolved, rootSchema, visited);
        }
        
        // Transform properties recursively
        var writer = new Utf8JsonWriter(new MemoryStream());
        writer.WriteStartObject();
        
        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name == "$defs" || prop.Name == "definitions")
            {
                // Remove $defs after resolution
                continue;
            }
            
            writer.WritePropertyName(prop.Name);
            
            if (prop.Name == "properties" || prop.Name == "items")
            {
                // Recursively transform nested schemas
                WriteTransformedValue(writer, prop.Value, rootSchema, visited);
            }
            else
            {
                prop.Value.WriteTo(writer);
            }
        }
        
        writer.WriteEndObject();
        writer.Flush();
        
        var stream = (MemoryStream)writer.BaseStream;
        return JsonDocument.Parse(stream.ToArray()).RootElement;
    }
    
    private void WriteTransformedValue(
        Utf8JsonWriter writer,
        JsonElement element,
        JsonElement rootSchema,
        HashSet<string> visited)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var transformed = ResolveRefs(element, rootSchema, new HashSet<string>(visited));
            transformed.WriteTo(writer);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (var item in element.EnumerateArray())
            {
                WriteTransformedValue(writer, item, rootSchema, visited);
            }
            writer.WriteEndArray();
        }
        else
        {
            element.WriteTo(writer);
        }
    }
    
    private static JsonElement ResolveRefPath(JsonElement root, string refPath)
    {
        // Parse path like "#/$defs/User"
        var parts = refPath.TrimStart('#', '/').Split('/');
        var current = root;
        
        foreach (var part in parts)
        {
            if (!current.TryGetProperty(part, out current))
            {
                throw new SchemaTooComplexException(
                    $"Cannot resolve $ref: {refPath}",
                    "ACODE-VLM-SO-002");
            }
        }
        
        return current;
    }
    
    private static int CalculateDepth(JsonElement element, int currentDepth = 0)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return currentDepth;
        }
        
        var maxChildDepth = currentDepth;
        
        if (element.TryGetProperty("properties", out var props))
        {
            foreach (var prop in props.EnumerateObject())
            {
                var childDepth = CalculateDepth(prop.Value, currentDepth + 1);
                maxChildDepth = Math.Max(maxChildDepth, childDepth);
            }
        }
        
        if (element.TryGetProperty("items", out var items))
        {
            var itemDepth = CalculateDepth(items, currentDepth + 1);
            maxChildDepth = Math.Max(maxChildDepth, itemDepth);
        }
        
        return maxChildDepth;
    }
}
```

### Step 4: Implement CapabilityDetector

```csharp
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Vllm.StructuredOutput.Capability;

/// <summary>
/// Detects model capabilities for structured output.
/// </summary>
public sealed class CapabilityDetector
{
    private readonly IVllmClient _client;
    private readonly CapabilityCache _cache;
    private readonly ILogger<CapabilityDetector> _logger;
    
    public CapabilityDetector(
        IVllmClient client,
        CapabilityCache cache,
        ILogger<CapabilityDetector>? logger = null)
    {
        _client = client;
        _cache = cache;
        _logger = logger ?? NullLogger<CapabilityDetector>.Instance;
    }
    
    /// <summary>
    /// Detect structured output capabilities for a model.
    /// </summary>
    public async Task<ModelCapabilities> DetectAsync(
        string modelId,
        CancellationToken cancellationToken)
    {
        // Check cache first
        var cached = _cache.Get(modelId);
        if (cached is not null)
        {
            _logger.LogDebug("Using cached capabilities for {Model}", modelId);
            return cached;
        }
        
        _logger.LogDebug("Querying vLLM for {Model} capabilities", modelId);
        
        try
        {
            var modelInfo = await _client.GetModelInfoAsync(modelId, cancellationToken);
            
            var capabilities = new ModelCapabilities
            {
                ModelId = modelId,
                SupportsGuidedJson = modelInfo.SupportedFeatures?.Contains("guided_json") ?? false,
                SupportsJsonSchema = modelInfo.SupportedFeatures?.Contains("json_schema") ?? false,
                SupportsGuidedChoice = modelInfo.SupportedFeatures?.Contains("guided_choice") ?? false,
                SupportsGuidedRegex = modelInfo.SupportedFeatures?.Contains("guided_regex") ?? false,
                MaxSchemaDepth = 10, // vLLM default
                MaxSchemaSize = 65536
            };
            
            _cache.Set(modelId, capabilities);
            return capabilities;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect capabilities for {Model}, using conservative defaults", modelId);
            
            // Conservative fallback
            return new ModelCapabilities
            {
                ModelId = modelId,
                SupportsGuidedJson = false,
                SupportsJsonSchema = false,
                FallbackRequired = true
            };
        }
    }
}

/// <summary>
/// Model structured output capabilities.
/// </summary>
public sealed class ModelCapabilities
{
    public string ModelId { get; init; } = string.Empty;
    public bool SupportsGuidedJson { get; init; }
    public bool SupportsJsonSchema { get; init; }
    public bool SupportsGuidedChoice { get; init; }
    public bool SupportsGuidedRegex { get; init; }
    public bool FallbackRequired { get; init; }
    public int MaxSchemaDepth { get; init; } = 10;
    public int MaxSchemaSize { get; init; } = 65536;
}

/// <summary>
/// Cache for model capabilities.
/// </summary>
public sealed class CapabilityCache
{
    private readonly Dictionary<string, (ModelCapabilities, DateTime)> _cache = new();
    private readonly TimeSpan _expiry;
    private readonly object _lock = new();
    
    public CapabilityCache(TimeSpan expiry)
    {
        _expiry = expiry;
    }
    
    public ModelCapabilities? Get(string modelId)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(modelId, out var entry))
            {
                if (DateTime.UtcNow - entry.Item2 < _expiry)
                {
                    return entry.Item1;
                }
                _cache.Remove(modelId);
            }
            return null;
        }
    }
    
    public void Set(string modelId, ModelCapabilities capabilities)
    {
        lock (_lock)
        {
            _cache[modelId] = (capabilities, DateTime.UtcNow);
        }
    }
    
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
        }
    }
}
```

### Step 5: Implement StructuredOutputHandler

```csharp
using System.Text.Json;
using Acode.Application.Interfaces;
using Acode.Domain.Inference;
using Acode.Infrastructure.Vllm.StructuredOutput.Capability;
using Acode.Infrastructure.Vllm.StructuredOutput.Fallback;
using Acode.Infrastructure.Vllm.StructuredOutput.Schema;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Vllm.StructuredOutput;

/// <summary>
/// Orchestrates structured output enforcement for vLLM requests.
/// </summary>
public sealed class StructuredOutputHandler
{
    private readonly StructuredOutputConfiguration _config;
    private readonly SchemaTransformer _transformer;
    private readonly CapabilityDetector _capabilities;
    private readonly FallbackHandler _fallback;
    private readonly IToolSchemaRegistry _schemaRegistry;
    private readonly ILogger<StructuredOutputHandler> _logger;
    
    public StructuredOutputHandler(
        StructuredOutputConfiguration config,
        SchemaTransformer transformer,
        CapabilityDetector capabilities,
        FallbackHandler fallback,
        IToolSchemaRegistry schemaRegistry,
        ILogger<StructuredOutputHandler> logger)
    {
        _config = config;
        _transformer = transformer;
        _capabilities = capabilities;
        _fallback = fallback;
        _schemaRegistry = schemaRegistry;
        _logger = logger;
    }
    
    /// <summary>
    /// Apply structured output constraints to a vLLM request.
    /// </summary>
    public async Task<ApplyResult> ApplyToRequestAsync(
        VllmRequest request,
        ChatRequest chatRequest,
        string modelId,
        CancellationToken cancellationToken)
    {
        if (!_config.IsEnabled(modelId))
        {
            _logger.LogDebug("Structured output disabled for {Model}", modelId);
            return ApplyResult.Disabled();
        }
        
        // Detect capabilities
        var caps = await _capabilities.DetectAsync(modelId, cancellationToken);
        
        if (chatRequest.ResponseFormat is not null)
        {
            return await ApplyResponseFormatAsync(
                request, chatRequest.ResponseFormat, caps, modelId);
        }
        
        if (chatRequest.Tools?.Any() == true)
        {
            return ApplyToolSchemas(request, chatRequest.Tools, caps, modelId);
        }
        
        _logger.LogDebug("No structured output needed for request");
        return ApplyResult.NotApplicable();
    }
    
    private async Task<ApplyResult> ApplyResponseFormatAsync(
        VllmRequest request,
        ResponseFormat format,
        ModelCapabilities caps,
        string modelId)
    {
        if (format.Type == "json_object")
        {
            if (caps.SupportsGuidedJson)
            {
                request.ResponseFormat = new { type = "json_object" };
                _logger.LogDebug("Applied json_object format for {Model}", modelId);
                return ApplyResult.Applied(StructuredOutputMode.JsonObject);
            }
            else
            {
                _logger.LogWarning("json_object not supported by {Model}, fallback activated", modelId);
                return ApplyResult.Fallback(FallbackReason.CapabilityUnavailable);
            }
        }
        
        if (format.Type == "json_schema" && format.JsonSchema is not null)
        {
            if (!caps.SupportsJsonSchema)
            {
                _logger.LogWarning("json_schema not supported by {Model}, fallback activated", modelId);
                return ApplyResult.Fallback(FallbackReason.CapabilityUnavailable);
            }
            
            try
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
                _logger.LogDebug("Applied json_schema format for {Model}", modelId);
                return ApplyResult.Applied(StructuredOutputMode.JsonSchema);
            }
            catch (SchemaTooComplexException ex)
            {
                _logger.LogWarning(ex, "Schema rejected for {Model}, fallback activated", modelId);
                return ApplyResult.Fallback(FallbackReason.SchemaRejected, ex.Message);
            }
        }
        
        return ApplyResult.NotApplicable();
    }
    
    private ApplyResult ApplyToolSchemas(
        VllmRequest request,
        IReadOnlyList<ToolDefinition> tools,
        ModelCapabilities caps,
        string modelId)
    {
        if (!caps.SupportsGuidedJson)
        {
            _logger.LogWarning("guided_json not supported by {Model}, tool schemas not enforced", modelId);
            return ApplyResult.Fallback(FallbackReason.CapabilityUnavailable);
        }
        
        // Transform each tool's parameter schema
        var transformedTools = new List<object>();
        
        foreach (var tool in tools)
        {
            try
            {
                var transformed = _transformer.Transform(tool.Parameters);
                transformedTools.Add(new
                {
                    type = "function",
                    function = new
                    {
                        name = tool.Name,
                        description = tool.Description,
                        parameters = transformed
                    }
                });
            }
            catch (SchemaTooComplexException ex)
            {
                _logger.LogWarning(ex, "Tool {ToolName} schema rejected", tool.Name);
                // Continue with other tools
            }
        }
        
        if (transformedTools.Count > 0)
        {
            request.Tools = transformedTools.ToArray();
            _logger.LogDebug("Applied {Count} tool schemas for {Model}", transformedTools.Count, modelId);
            return ApplyResult.Applied(StructuredOutputMode.ToolSchemas);
        }
        
        return ApplyResult.Fallback(FallbackReason.AllSchemasRejected);
    }
}

/// <summary>
/// Result of applying structured output.
/// </summary>
public sealed class ApplyResult
{
    public bool WasApplied { get; init; }
    public bool RequiresFallback { get; init; }
    public StructuredOutputMode Mode { get; init; }
    public FallbackReason FallbackReason { get; init; }
    public string? Error { get; init; }
    
    public static ApplyResult Applied(StructuredOutputMode mode) =>
        new() { WasApplied = true, Mode = mode };
    
    public static ApplyResult Fallback(FallbackReason reason, string? error = null) =>
        new() { RequiresFallback = true, FallbackReason = reason, Error = error };
    
    public static ApplyResult Disabled() =>
        new() { Mode = StructuredOutputMode.Disabled };
    
    public static ApplyResult NotApplicable() =>
        new() { Mode = StructuredOutputMode.None };
}

public enum StructuredOutputMode
{
    None,
    Disabled,
    JsonObject,
    JsonSchema,
    ToolSchemas
}

public enum FallbackReason
{
    None,
    CapabilityUnavailable,
    SchemaRejected,
    AllSchemasRejected,
    Timeout
}
```

### Step 6: Register in Dependency Injection

Add to `src/Acode.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`:

```csharp
using Acode.Infrastructure.Vllm.StructuredOutput;
using Acode.Infrastructure.Vllm.StructuredOutput.Capability;
using Acode.Infrastructure.Vllm.StructuredOutput.Fallback;
using Acode.Infrastructure.Vllm.StructuredOutput.Schema;

// In AddVllmProvider method:
services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return StructuredOutputConfiguration.FromConfiguration(config);
});

services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<StructuredOutputConfiguration>();
    return new SchemaTransformer(
        config.Schema.MaxDepth,
        config.Schema.MaxSizeBytes,
        config.Schema.ProcessingTimeoutMs);
});

services.AddSingleton<SchemaCache>(sp =>
{
    var config = sp.GetRequiredService<StructuredOutputConfiguration>();
    return new SchemaCache(config.Schema.CacheSize);
});

services.AddSingleton<CapabilityCache>(sp =>
    new CapabilityCache(TimeSpan.FromMinutes(5)));

services.AddSingleton<CapabilityDetector>();
services.AddSingleton<OutputValidator>();
services.AddSingleton<FallbackHandler>();
services.AddScoped<StructuredOutputHandler>();
```

### Error Codes Reference

| Code | Message | Cause |
|------|---------|-------|
| ACODE-VLM-SO-001 | Schema exceeds complexity limits | Depth > 10 or size > 64KB |
| ACODE-VLM-SO-002 | Unsupported JSON Schema type | External $ref, allOf/anyOf |
| ACODE-VLM-SO-003 | Guided decoding timeout | Processing > 100ms |
| ACODE-VLM-SO-004 | Invalid schema format | Malformed JSON Schema |
| ACODE-VLM-SO-005 | Output validation failed | Fallback validation error |
| ACODE-VLM-SO-006 | Max retries exceeded | 3+ retry failures |

### Implementation Checklist

- [ ] Create folder structure under Vllm/StructuredOutput/
- [ ] Implement StructuredOutputConfiguration
- [ ] Implement FallbackConfiguration
- [ ] Implement SchemaConfiguration
- [ ] Implement ModelStructuredOutputConfig
- [ ] Implement SchemaTransformer with $ref resolution
- [ ] Implement SchemaCache
- [ ] Implement CapabilityDetector
- [ ] Implement CapabilityCache
- [ ] Implement ModelCapabilities
- [ ] Implement FallbackHandler
- [ ] Implement FallbackContext
- [ ] Implement OutputValidator
- [ ] Implement StructuredOutputHandler
- [ ] Implement ApplyResult type
- [ ] Implement SchemaTooComplexException
- [ ] Implement ValidationFailedException
- [ ] Register all services in DI
- [ ] Integrate with VllmProvider.CompleteAsync
- [ ] Write StructuredOutputConfigurationTests
- [ ] Write SchemaTransformerTests (10 tests)
- [ ] Write FallbackHandlerTests (6 tests)
- [ ] Write CapabilityDetectorTests (4 tests)
- [ ] Write OutputValidatorTests (5 tests)
- [ ] Write StructuredOutputIntegrationTests (4 tests)
- [ ] Write StructuredOutputBenchmarks (5 benchmarks)
- [ ] Add XML documentation to all public types
- [ ] Update VllmProvider to use StructuredOutputHandler

### Dependencies

- **Task 006:** VllmProvider for integration
- **Task 006.a:** VllmHttpClient for capability queries
- **Task 007:** IToolSchemaRegistry for tool schemas
- **Task 001:** IConfigurationService for configuration

### Verification Commands

```bash
# Build
dotnet build

# Run unit tests
dotnet test --filter "FullyQualifiedName~Vllm.StructuredOutput"

# Run benchmarks
dotnet run -c Release --project tests/Acode.Performance.Tests -- --filter "StructuredOutput*"

# Verify configuration
acode providers config vllm --show structured_output

# Test with vLLM
acode chat --provider vllm --response-format json_object --message "Return JSON"
```

---

**End of Task 007.e Specification**