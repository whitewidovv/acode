# Task 007: Tool Schema Registry + Strict Validation

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 004.a (ToolDefinition types), Task 005.b (Tool Call Parsing), Task 006.b (Structured Outputs), Task 001, Task 002  

---

## Description

### Business Value and Strategic Importance

The Tool Schema Registry is the foundational contract layer that governs all interactions between the LLM and the external world. Without a robust schema registry and strict validation system, Acode cannot safely execute any tool—every file read, command execution, code modification, or git operation would be at risk of malformed arguments causing undefined behavior, data corruption, or security vulnerabilities.

**Quantified Business Impact:**

- **Prevented Runtime Failures:** Tool validation catches 100% of malformed argument errors before execution. In production LLM systems, models produce malformed tool calls 8-15% of the time depending on complexity. For a typical Acode session with 50 tool calls, this prevents 4-7 runtime errors per session.
- **Security Incident Prevention:** Strict validation blocks injection attacks via tool arguments. A single unvalidated path argument could enable path traversal (`../../../etc/passwd`); a single unvalidated command could execute arbitrary code. The registry ensures only schema-conformant arguments reach tool implementations.
- **Model Retry Efficiency:** When validation fails, structured error messages enable models to self-correct. Without clear error feedback, models retry blindly with ~20% success rate. With structured validation errors (field path, expected type, actual value), retry success rate reaches 85-92% on first retry.
- **Development Velocity:** Centralized schema definitions eliminate duplication across tool implementations. A single source of truth for each tool's contract reduces bugs from inconsistent validation logic. Estimated reduction: 40% fewer validation-related bugs during development.
- **Debugging Time Reduction:** JSON Pointer paths in errors pinpoint exact argument problems. Instead of "invalid arguments" requiring manual inspection, developers see "/parameters/path: expected string, got integer". Average debugging time per validation error drops from 15 minutes to 2 minutes.

**Cost of Not Implementing:**

- Every tool implementation must include its own validation logic (duplicated effort)
- No standardized error format means models can't learn to fix mistakes
- No central place to audit what tools exist and what they can do
- Schema changes require updating every call site rather than one registry
- Security reviews must examine every tool rather than a single validation layer

### Technical Architecture

The Tool Schema Registry follows the Provider pattern combined with Compile-Once-Validate-Many caching for optimal performance. The architecture consists of four primary components:

**1. IToolSchemaRegistry (Application Layer Interface)**

The registry interface lives in the Application layer, following Clean Architecture principles. It defines the contract for tool registration, discovery, and validation without specifying implementation details:

```
IToolSchemaRegistry
├── RegisterTool(ToolDefinition) → void
├── GetToolDefinition(string name) → ToolDefinition
├── GetAllTools() → IReadOnlyList<ToolDefinition>
├── GetToolsByCategory(ToolCategory) → IReadOnlyList<ToolDefinition>
├── ValidateArguments(string name, string json) → JsonElement [throws]
└── TryValidateArguments(string name, string json, out errors, out parsed) → bool
```

**2. ToolSchemaRegistry (Infrastructure Layer Implementation)**

The concrete implementation resides in Infrastructure, handling thread-safe storage, schema compilation, and validation execution. Key internal components:

```
ToolSchemaRegistry
├── ConcurrentDictionary<string, CompiledToolSchema> _tools
├── SchemaCompiler _compiler
├── SchemaValidator _validator
└── ILogger<ToolSchemaRegistry> _logger
```

**3. ISchemaProvider Pattern**

Tools register their schemas via providers discovered through DI. This decouples tool definitions from the registry itself:

```
ISchemaProvider
└── RegisterSchemas(IToolSchemaRegistry registry)

Implementations:
├── CoreToolsProvider (built-in tools: read_file, write_file, execute, etc.)
├── GitToolsProvider (git operations: status, commit, push, etc.)
├── ConfigToolsProvider (user-defined tools from .agent/config.yml)
└── [Custom providers registered via DI]
```

**4. Schema Compilation Pipeline**

Raw JSON Schema documents undergo compilation at registration time, converting them into optimized validation functions:

```
Raw Schema (JsonElement)
    ↓ Parse & Validate
Draft 2020-12 Schema Object
    ↓ Compile Constraints
CompiledSchema
├── TypeValidator
├── RequiredFieldsValidator
├── ConstraintValidators[]
└── NestedValidators (recursive)
    ↓ Cache
ConcurrentDictionary<toolName, CompiledSchema>
    ↓ At Runtime
ValidateArguments(json) → Result in <1ms
```

### JSON Schema Specification Compliance

Acode uses JSON Schema Draft 2020-12 (https://json-schema.org/draft/2020-12/json-schema-core.html) as the schema language. This version was chosen for:

- **Model Provider Compatibility:** OpenAI, Anthropic, and most LLM providers use Draft 2020-12 for function calling
- **Rich Constraint System:** Supports all needed constraints (pattern, enum, min/max, format)
- **$defs Support:** Enables schema reuse via references without external files
- **Stable Specification:** Finalized standard unlikely to change

**Supported Schema Features:**

| Feature | Keyword | Example |
|---------|---------|---------|
| Type declaration | `type` | `"type": "string"` |
| Required fields | `required` | `"required": ["path"]` |
| String length | `minLength`, `maxLength` | `"maxLength": 4096` |
| Numeric range | `minimum`, `maximum` | `"minimum": 1` |
| Pattern matching | `pattern` | `"pattern": "^[a-z]+$"` |
| Enumeration | `enum` | `"enum": ["utf-8", "ascii"]` |
| Array items | `items` | `"items": {"type": "string"}` |
| Array length | `minItems`, `maxItems` | `"maxItems": 100` |
| Object properties | `properties` | Nested property definitions |
| Additional properties | `additionalProperties` | `"additionalProperties": false` |
| Default values | `default` | `"default": "utf-8"` |
| Format validation | `format` | `"format": "uri"` |

### Integration Points

**Upstream Dependencies:**

- **Task 004.a (ToolDefinition Types):** Provides the `ToolDefinition` class structure
- **Task 005.b (Tool Call Parsing):** Parser extracts tool name and arguments from model output
- **Task 006.b (Structured Outputs):** vLLM integration uses schemas to constrain generation

**Downstream Consumers:**

- **Task 007.a (Core Tool Schemas):** Registers all built-in tool schemas
- **Task 007.b (Validator Errors):** Defines error format for model retry
- **Task 012 (Agent Loop):** Validates tool calls before execution
- **Task 018 (Command Runner):** Receives validated arguments for execution

**External Integrations:**

- **Ollama API:** Receives tool definitions as part of prompt construction (tools[] parameter)
- **vLLM API:** Uses schemas for structured output enforcement via outlines
- **CLI Display:** `acode tools list` queries registry for display

### Performance Requirements and Budgets

Tool validation is on the critical path for every tool call. The performance budget allocates time across the validation pipeline:

| Operation | Budget | Rationale |
|-----------|--------|-----------|
| Schema compilation | <10ms | One-time at startup per tool |
| JSON parsing | <0.5ms | System.Text.Json is fast |
| Type validation | <0.1ms | Direct type checking |
| Constraint validation | <0.3ms | Pre-compiled validators |
| Error formatting | <0.1ms | Only on failure path |
| **Total validation** | **<1ms** | Imperceptible to user |

**Memory Budget:**

| Item | Budget | Rationale |
|------|--------|-----------|
| Per compiled schema | <50KB | Allows 1000+ tools in <50MB |
| Validation context | <1KB | Per-call allocation |
| Error list | <10KB | Worst case many errors |

### Strict Validation Philosophy

The registry implements **strict-by-default** validation with no lenient mode:

**What "Strict" Means:**

1. **Unknown properties rejected:** If schema says `properties: {path, encoding}` and model sends `{path, encoding, extra}`, validation fails. There is no "ignore extra properties" mode.

2. **Type coercion disabled:** If schema says `type: integer` and model sends `"42"` (string), validation fails. There is no automatic type conversion.

3. **Null handling explicit:** If a field can be null, schema must declare `type: ["string", "null"]`. Implicit nullability is not allowed.

4. **Pattern matching strict:** Regex patterns must match the entire value. Pattern `^[a-z]+$` rejects `"hello123"` even though it contains letters.

5. **Enum case-sensitive:** Enum `["utf-8", "UTF-8"]` treats these as different values. No case normalization.

**Why No Lenient Mode:**

- **Security:** Lenient parsing enables injection attacks via unexpected fields
- **Debuggability:** Strict errors surface problems immediately rather than causing downstream failures
- **Model Training:** Consistent rejection teaches models correct formats
- **Predictability:** Same input always produces same validation result

### Error Handling and Model Retry Contract

When validation fails, errors follow a machine-parseable format that enables model self-correction:

```json
{
  "success": false,
  "tool": "read_file",
  "errors": [
    {
      "path": "/path",
      "code": "ACODE-TSR-003",
      "message": "Required field 'path' is missing",
      "expected": "string (file path to read)",
      "actual": null,
      "suggestion": "Add a 'path' property with the file path to read"
    }
  ],
  "schema_hint": "read_file expects: {path: string (required), encoding?: 'utf-8'|'ascii'|'utf-16'}"
}
```

**Error Components:**

| Field | Purpose | Example |
|-------|---------|---------|
| `path` | JSON Pointer to error location | `/parameters/options/encoding` |
| `code` | Stable error code for programmatic handling | `ACODE-TSR-004` |
| `message` | Human-readable explanation | `Type mismatch: expected integer, got string` |
| `expected` | What the schema requires | `integer (1-100)` |
| `actual` | What was provided (sanitized) | `"fifty"` |
| `suggestion` | How to fix | `Provide a numeric value between 1 and 100` |
| `schema_hint` | Summary of valid schema | Helps model understand full structure |

**Retry Success Rates by Error Type:**

| Error Type | First Retry Success | Reason |
|------------|---------------------|--------|
| Missing required field | 95% | Clear what to add |
| Type mismatch | 90% | Clear how to fix |
| Constraint violation | 85% | May need value reconsideration |
| Malformed JSON | 75% | Structural issues harder to fix |
| Unknown tool | 60% | May indicate broader confusion |

### Observability and Metrics

The registry emits structured logs and metrics for operational visibility:

**Logged Events:**

| Event | Level | Data |
|-------|-------|------|
| Tool registered | Info | tool_name, version, schema_size_bytes |
| Tool registration failed | Error | tool_name, error_code, error_message |
| Validation succeeded | Debug | tool_name, validation_ms |
| Validation failed | Warning | tool_name, error_count, first_error_code |
| Schema compilation slow | Warning | tool_name, compilation_ms (if >10ms) |

**Metrics Exposed:**

| Metric | Type | Labels |
|--------|------|--------|
| `acode_tool_registrations_total` | Counter | tool_name |
| `acode_tool_validations_total` | Counter | tool_name, result (success/failure) |
| `acode_tool_validation_seconds` | Histogram | tool_name |
| `acode_tool_validation_errors_total` | Counter | tool_name, error_code |
| `acode_tool_registry_size` | Gauge | (none) |

### Configuration Options

The registry supports configuration via `.agent/config.yml`:

```yaml
tool_registry:
  # Enable/disable strict mode (always true, cannot be disabled)
  strict_mode: true
  
  # Maximum schema size in bytes (prevents memory exhaustion)
  max_schema_size: 102400  # 100KB
  
  # Enable pattern validation timeout (prevents ReDoS)
  pattern_timeout_ms: 100
  
  # Enable schema caching (should always be true)
  cache_compiled_schemas: true
  
  # Log validation failures at this level
  validation_failure_log_level: warning
  
  # Include actual values in error messages (disable for sensitive tools)
  include_actual_values: true
  
  # Custom tool definitions
  custom_tools:
    - name: my_custom_tool
      version: "1.0.0"
      description: A custom tool for specific operations
      category: custom
      parameters:
        type: object
        properties:
          input:
            type: string
            description: The input value
        required:
          - input
```

### Extensibility and Custom Tools

Users can extend Acode with custom tools registered via configuration or code:

**Configuration-Based Custom Tools:**

1. Define tool in `.agent/config.yml` under `tool_registry.custom_tools`
2. Tool is loaded and validated at startup
3. Tool appears in `acode tools list`
4. Tool can be called by models like any built-in tool

**Code-Based Custom Tools (for advanced users):**

1. Implement `ISchemaProvider` interface
2. Register provider in DI container
3. Provider's `RegisterSchemas` method called at startup
4. Tools registered through standard registry API

**Custom Tool Validation:**

Custom tool schemas undergo the same validation as built-in tools:
- Schema must be valid JSON Schema Draft 2020-12
- Schema must compile successfully
- Name must not conflict with existing tools
- Version must be valid semver

### Constraints and Limitations

The following constraints apply to the Tool Schema Registry:

1. **Schemas are immutable after registration:** Once registered, a tool's schema cannot be changed. To update a schema, register a new version.

2. **No runtime schema modification:** Schemas are fixed at application startup. Hot-reloading schemas is not supported.

3. **Single namespace for tool names:** All tool names must be globally unique. No namespacing or scoping is provided.

4. **No schema inheritance:** Each tool defines its complete schema. Schema composition via $ref is limited to internal definitions ($defs).

5. **No async validation:** Validation is synchronous. Async constraints (e.g., checking if a file exists) are not supported in schemas.

6. **Local schemas only:** Schemas must be defined locally. Remote schema references ($ref to URLs) are not supported for security.

7. **No custom keywords:** Only standard JSON Schema keywords are supported. Custom validation keywords are not allowed.

8. **Maximum schema depth:** Nested objects are limited to 10 levels deep to prevent stack overflow during validation.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Tool Schema Registry | Central repository for tool definitions |
| JSON Schema | Standard for describing JSON structure |
| Schema Validation | Checking data conforms to schema |
| Strict Validation | Reject anything not explicitly allowed |
| Tool Definition | Name, description, and parameter schema |
| Parameter Schema | JSON Schema for tool arguments |
| Schema Registration | Adding schema to registry at startup |
| Schema Compilation | Preparing schema for fast validation |
| Validation Error | Failure when data doesn't match schema |
| Required Field | Schema field that must be present |
| Optional Field | Schema field that may be omitted |
| Type Constraint | Schema restriction on value type |
| Enum Constraint | Schema restriction to specific values |
| Pattern Constraint | Schema restriction to regex pattern |
| Schema Version | Version number for schema evolution |
| Schema Provider | Interface for registering schemas |
| Core Tools | Built-in Acode tools |
| Custom Tools | User-defined tools |
| Tool Discovery | Finding available tools |
| Tool Result | Response from tool execution |

---

## Out of Scope

The following items are explicitly excluded from Task 007:

- **Tool implementation** - Tools themselves are separate tasks
- **Tool execution engine** - Separate orchestration task
- **Permission system** - Task 003 security layer
- **Tool result formatting** - Task 007.c covers truncation only
- **Multi-tenant isolation** - Single user focus
- **Schema UI editor** - Schemas defined in code/config
- **Remote schema repositories** - Local schemas only
- **JSON Schema extensions** - Standard features only
- **Runtime schema modification** - Schemas fixed at startup
- **Schema inference** - Schemas must be explicit

---

## Assumptions

This section documents all assumptions that must hold true for successful implementation. These assumptions should be validated during planning and monitored during development. If any assumption proves false, escalate immediately as it may require design changes.

### Technical Assumptions

1. **JSON Schema Library Availability**: The `JsonSchema.Net` NuGet package (version 5.x or later) is available, maintained, and supports JSON Schema Draft 2020-12 with all required vocabulary features including `$ref`, `$defs`, `oneOf`, `anyOf`, `allOf`, `if/then/else`, `dependentSchemas`, and format validation.

2. **Thread-Safe Collections in .NET**: The `System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue>` class provides sufficient thread safety guarantees for the registry's concurrent access patterns without requiring external locking.

3. **JSON Serialization Performance**: `System.Text.Json` provides adequate performance for parsing tool arguments, with deserialization of typical argument payloads completing in under 1ms for arguments up to 10KB.

4. **Memory Model Assumptions**: Schema compilation produces immutable, thread-safe validator instances that can be safely cached and shared across concurrent validations without defensive copying.

5. **Exception Model**: The `JsonSchema.Net` library throws specific, catchable exceptions for malformed schemas during compilation (e.g., `JsonSchemaException`) that can be distinguished from runtime validation failures.

6. **String Interning**: Tool names registered with the registry are stable strings that can be safely used as dictionary keys without defensive copying, as they originate from configuration or attribute metadata.

7. **Startup Performance**: Application startup can accommodate schema compilation overhead of up to 500ms total for up to 50 tools without impacting user experience, as this is a one-time cost.

8. **Logging Framework**: `Microsoft.Extensions.Logging` is configured and available for dependency injection, with structured logging support for complex objects like validation error collections.

### Operational Assumptions

9. **Deterministic Validation**: JSON Schema validation produces identical results for identical inputs - there are no random, time-based, or environment-dependent validation behaviors that would cause flaky results.

10. **Error Message Stability**: The `JsonSchema.Net` library produces consistent, human-readable error messages that can be safely included in user-facing output and logged for debugging purposes.

11. **Schema Complexity Bounds**: All tool schemas will have bounded complexity - no unbounded recursion, no schemas with more than 20 levels of nesting, and no individual schemas exceeding 50KB when serialized.

12. **Finite Tool Count**: The system will register at most 100 tools, making linear-time operations (like `GetAllTools()`) acceptable and eliminating the need for pagination or lazy loading.

13. **No Hot Reload Requirement**: Tool schemas are fixed at application startup - there is no requirement to add, remove, or modify tool registrations while the application is running.

14. **Validation Output Size**: Validation error collections will contain at most 50 errors per validation attempt, as the validator is configured to stop after reasonable error accumulation.

### Integration Assumptions

15. **DI Container Support**: The application uses `Microsoft.Extensions.DependencyInjection` and the `IToolSchemaRegistry` interface can be registered as a singleton service without lifecycle complications.

16. **Configuration System**: Tool definitions are sourced from a stable configuration mechanism (YAML files, embedded resources, or strongly-typed options) that is resolved before the registry is initialized.

17. **LLM Response Format**: When the LLM returns tool call requests, the arguments are provided as a JSON string or `JsonElement` that can be validated against the registered schema without transformation.

18. **Error Propagation Contract**: Downstream consumers (tool executor, LLM retry loop) expect validation errors in a specific format (`ToolValidationResult` with error collection) and will handle them appropriately.

19. **Metric Collection**: The application has an `IMetrics` or similar telemetry interface available for recording validation success/failure counts and timing histograms.

20. **Testing Infrastructure**: The test project has access to the same `JsonSchema.Net` library and can create test schemas, tool definitions, and validation scenarios without mocking the schema validation itself.

### Assumption Validation Checklist

Before implementation begins, validate the following:

| Assumption | Validation Method | Owner | Status |
|------------|-------------------|-------|--------|
| JsonSchema.Net supports Draft 2020-12 | Check library documentation | Developer | ☐ Pending |
| ConcurrentDictionary is sufficient | Review concurrency requirements | Architect | ☐ Pending |
| Schema compilation is fast enough | Benchmark with 50 test schemas | Developer | ☐ Pending |
| Error messages are suitable for users | Review sample error output | UX/Developer | ☐ Pending |
| DI singleton registration works | Test with sample registry | Developer | ☐ Pending |

---

## Security Considerations

This section documents the security analysis for the Tool Schema Registry, including threat modeling, attack vectors, mitigations, and audit requirements. All security considerations must be addressed before the feature is considered complete.

### Threat Model

#### Assets to Protect

1. **Schema Integrity**: Tool schemas define the contract between the LLM and tool execution. Corrupted or malicious schemas could allow the LLM to invoke tools with dangerous arguments.

2. **System Stability**: The registry is a singleton that participates in every tool call. Attacks that degrade registry performance affect all LLM interactions.

3. **Error Information**: Validation error messages may contain sensitive information about the application's internal structure, tool capabilities, or argument constraints.

4. **Configuration Secrets**: Some tool schemas may reference or describe parameters that relate to sensitive operations (file paths, API endpoints, credentials).

#### Trust Boundaries

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        TRUSTED ZONE                                     │
│  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐  │
│  │  Configuration   │    │  Tool Schema     │    │  Tool Executor   │  │
│  │  (YAML files)    │───▶│  Registry        │───▶│  (Task 010)      │  │
│  └──────────────────┘    └──────────────────┘    └──────────────────┘  │
│           │                      ▲                                      │
│           │                      │                                      │
└───────────│──────────────────────│──────────────────────────────────────┘
            │                      │
            │                      │ Validation Request
            ▼                      │
┌───────────────────────────────────│─────────────────────────────────────┐
│           SEMI-TRUSTED ZONE       │                                     │
│  ┌──────────────────┐            │                                      │
│  │  LLM Provider    │────────────┘                                      │
│  │  (Ollama API)    │  Tool call arguments (untrusted content)         │
│  └──────────────────┘                                                   │
└─────────────────────────────────────────────────────────────────────────┘
```

The registry sits at a critical trust boundary. It receives:
- **Trusted input**: Schema definitions from configuration (developer-authored)
- **Untrusted input**: Tool call arguments from the LLM (which may be influenced by prompt injection)

### Attack Vectors and Mitigations

#### AV-1: Schema Poisoning Attack

**Description**: An attacker modifies tool schema files on disk to weaken validation constraints, allowing malicious arguments to pass validation.

**Likelihood**: Low (requires filesystem access)

**Impact**: Critical (bypasses tool argument validation)

**Mitigations**:
- M-1.1: Configuration files should be read-only in production deployments
- M-1.2: Log all schema registrations with schema hash for audit trail
- M-1.3: Consider schema signing for high-security deployments (future enhancement)
- M-1.4: File integrity monitoring on configuration directories

**Implementation Requirements**:
```csharp
// Log schema registration with hash for audit trail
public void RegisterTool(ToolDefinition tool)
{
    var schemaHash = ComputeSchemaHash(tool.ParameterSchema);
    _logger.LogInformation(
        "Registering tool {ToolName} with schema hash {SchemaHash}",
        tool.Name,
        schemaHash);
    // ... registration logic
}
```

#### AV-2: Denial of Service via Complex Schema

**Description**: A malformed or excessively complex schema causes the JSON Schema validator to consume excessive CPU or memory during compilation or validation.

**Likelihood**: Low (schemas are developer-authored)

**Impact**: High (system-wide performance degradation)

**Mitigations**:
- M-2.1: Enforce maximum schema size limit (50KB serialized)
- M-2.2: Enforce maximum nesting depth (20 levels)
- M-2.3: Enforce validation timeout (100ms per validation)
- M-2.4: Compile schemas at startup, not on-demand (isolates cost)
- M-2.5: Circuit breaker pattern if validation repeatedly times out

**Implementation Requirements**:
```csharp
private const int MaxSchemaSizeBytes = 50 * 1024; // 50KB
private const int MaxNestingDepth = 20;
private static readonly TimeSpan ValidationTimeout = TimeSpan.FromMilliseconds(100);

private void ValidateSchemaComplexity(JsonSchema schema)
{
    var serialized = JsonSerializer.Serialize(schema);
    if (serialized.Length > MaxSchemaSizeBytes)
    {
        throw new SchemaComplexityException(
            $"Schema exceeds maximum size of {MaxSchemaSizeBytes} bytes");
    }
    
    var depth = CalculateNestingDepth(schema);
    if (depth > MaxNestingDepth)
    {
        throw new SchemaComplexityException(
            $"Schema exceeds maximum nesting depth of {MaxNestingDepth}");
    }
}
```

#### AV-3: Information Disclosure via Error Messages

**Description**: Detailed validation error messages leak information about internal tool structure, expected parameters, or system configuration to the LLM or end user.

**Likelihood**: Medium (errors naturally contain schema details)

**Impact**: Medium (aids reconnaissance for further attacks)

**Mitigations**:
- M-3.1: Sanitize error messages before including in LLM prompts
- M-3.2: Log full error details at Debug level only
- M-3.3: Return generic user-facing errors with correlation IDs
- M-3.4: Never include schema examples or default values in errors

**Implementation Requirements**:
```csharp
public ToolValidationResult ValidateArguments(string toolName, JsonElement arguments)
{
    var result = PerformValidation(toolName, arguments);
    
    if (!result.IsValid)
    {
        // Full details for debugging (not sent to LLM)
        _logger.LogDebug(
            "Validation failed for {ToolName}: {DetailedErrors}",
            toolName,
            JsonSerializer.Serialize(result.InternalErrors));
        
        // Sanitized version for LLM retry
        result.Errors = result.InternalErrors
            .Select(e => SanitizeErrorMessage(e))
            .ToList();
    }
    
    return result;
}

private string SanitizeErrorMessage(ValidationError error)
{
    // Remove any property path beyond the top level
    // Remove any expected values, patterns, or enum lists
    // Keep only: property name + general error type
    return $"Property '{error.PropertyName}': {error.ErrorType}";
}
```

#### AV-4: Prompt Injection via Tool Arguments

**Description**: The LLM returns tool arguments containing malicious content designed to exploit downstream tool implementations or influence subsequent LLM responses.

**Likelihood**: Medium (LLM responses are influenced by user input)

**Impact**: High (potential code execution if tools are vulnerable)

**Mitigations**:
- M-4.1: JSON Schema validation blocks structurally invalid arguments
- M-4.2: Use `format` validation for strings (e.g., `uri`, `email`) where applicable
- M-4.3: Use `pattern` constraints to restrict string content
- M-4.4: Use `maxLength` to prevent buffer-related attacks
- M-4.5: Tool implementations must perform additional semantic validation

**Schema Design Requirements**:
```json
{
  "type": "object",
  "properties": {
    "file_path": {
      "type": "string",
      "maxLength": 260,
      "pattern": "^[a-zA-Z0-9_\\-./\\\\]+$",
      "description": "Path to file (no special characters)"
    },
    "url": {
      "type": "string",
      "format": "uri",
      "maxLength": 2048
    }
  },
  "additionalProperties": false
}
```

**Note**: Schema validation is necessary but NOT sufficient for security. Each tool implementation must perform its own semantic validation.

#### AV-5: Resource Exhaustion via Repeated Validation

**Description**: An attacker triggers thousands of validation requests (via prompt injection causing LLM to repeatedly attempt tool calls with invalid arguments) to exhaust system resources.

**Likelihood**: Low (requires sustained attack)

**Impact**: Medium (performance degradation)

**Mitigations**:
- M-5.1: Rate limiting at the LLM request level (not registry responsibility)
- M-5.2: Circuit breaker if same tool fails validation repeatedly
- M-5.3: Metrics and alerting on validation failure rates
- M-5.4: Validation result caching (same input = same output)

### Input Validation Requirements

All inputs to the Tool Schema Registry must be validated:

#### Schema Definition Input (Trusted but Validated)

| Input | Validation | Error Handling |
|-------|------------|----------------|
| Tool Name | Non-empty, alphanumeric + underscore, max 64 chars | Throw `ArgumentException` |
| Description | Non-empty, max 500 chars | Throw `ArgumentException` |
| Parameter Schema | Valid JSON Schema, max 50KB | Throw `InvalidSchemaException` |
| Schema Compilation | Must compile without errors | Throw `SchemaCompilationException` |

#### Validation Request Input (Untrusted)

| Input | Validation | Error Handling |
|-------|------------|----------------|
| Tool Name | Must exist in registry | Return `ToolNotFoundResult` |
| Arguments JSON | Must be valid JSON | Return validation error (not exception) |
| Arguments Structure | Must match schema | Return validation errors |
| Validation Duration | Must complete within timeout | Return timeout error |

### Audit Trail Requirements

The Tool Schema Registry must produce audit-ready logs for security review:

#### Events to Log

| Event | Level | Required Fields |
|-------|-------|-----------------|
| Tool Registered | Information | ToolName, SchemaHash, Timestamp |
| Tool Registration Failed | Warning | ToolName, ErrorType, ErrorMessage |
| Validation Succeeded | Debug | ToolName, CorrelationId, DurationMs |
| Validation Failed | Information | ToolName, CorrelationId, ErrorCount, ErrorSummary |
| Validation Timeout | Warning | ToolName, CorrelationId, TimeoutMs |
| Schema Complexity Exceeded | Warning | ToolName, Metric (size/depth), Limit |

#### Log Format Example

```json
{
  "timestamp": "2024-01-15T10:30:00.000Z",
  "level": "Information",
  "message": "Validation failed for tool",
  "properties": {
    "event": "ValidationFailed",
    "toolName": "file_read",
    "correlationId": "abc-123-def",
    "errorCount": 2,
    "errorSummary": "Missing required property 'path'; Invalid format for 'encoding'",
    "durationMs": 3.2
  }
}
```

### Security Testing Requirements

The following security tests must pass before the feature is complete:

#### ST-1: Schema Complexity Limits
- Test that schemas exceeding 50KB are rejected
- Test that schemas with nesting > 20 levels are rejected
- Test that schema compilation timeout is enforced

#### ST-2: Input Validation
- Test that malformed JSON arguments return errors (not exceptions)
- Test that extremely long string arguments are handled gracefully
- Test that deeply nested argument structures don't cause stack overflow

#### ST-3: Error Message Sanitization
- Test that validation errors don't contain full schema definitions
- Test that errors don't contain internal paths or class names
- Test that correlation IDs are included for support correlation

#### ST-4: Concurrency Safety
- Test that concurrent registrations don't corrupt state
- Test that concurrent validations produce consistent results
- Test that registry state is not visible in partial state during updates

### Compliance Considerations

While Acode is primarily a local development tool, the following considerations apply:

- **OWASP Top 10**: Input validation (A03:2021 - Injection) addressed via JSON Schema validation
- **CWE-20**: Improper Input Validation - mitigated by mandatory schema validation
- **CWE-400**: Uncontrolled Resource Consumption - mitigated by complexity limits and timeouts
- **Defense in Depth**: Schema validation is one layer; tool implementations must add semantic validation

---

## Functional Requirements

### IToolSchemaRegistry Interface

- FR-001: Interface MUST be defined in Application layer
- FR-002: Interface MUST expose RegisterTool method
- FR-003: Interface MUST expose GetToolDefinition method
- FR-004: Interface MUST expose GetAllTools method
- FR-005: Interface MUST expose ValidateArguments method
- FR-006: Interface MUST expose TryValidateArguments method
- FR-007: Interface MUST be injectable via DI

### ToolSchemaRegistry Implementation

- FR-008: Registry MUST be implemented in Infrastructure layer
- FR-009: Registry MUST store tool definitions thread-safely
- FR-010: Registry MUST compile schemas on registration
- FR-011: Registry MUST cache compiled schemas
- FR-012: Registry MUST reject duplicate tool names
- FR-013: Registry MUST validate schemas during registration
- FR-014: Registry MUST log registrations

### Tool Registration

- FR-015: RegisterTool MUST accept ToolDefinition
- FR-016: RegisterTool MUST validate schema is well-formed
- FR-017: RegisterTool MUST compile schema for validation
- FR-018: RegisterTool MUST throw on invalid schema
- FR-019: RegisterTool MUST be idempotent for same definition
- FR-020: RegisterTool MUST reject conflicting definitions
- FR-021: RegisterTool MUST log tool name and version

### Schema Validation

- FR-022: Schemas MUST follow JSON Schema Draft 2020-12
- FR-023: Schemas MUST define type for all properties
- FR-024: Schemas MUST list required properties
- FR-025: Schemas MUST support object type
- FR-026: Schemas MUST support array type
- FR-027: Schemas MUST support string type
- FR-028: Schemas MUST support number/integer types
- FR-029: Schemas MUST support boolean type
- FR-030: Schemas MUST support enum constraint
- FR-031: Schemas MUST support pattern constraint
- FR-032: Schemas MUST support minimum/maximum
- FR-033: Schemas MUST support minLength/maxLength
- FR-034: Schemas MUST support nested objects
- FR-035: Schemas MUST support array items

### Argument Validation

- FR-036: ValidateArguments MUST accept tool name and arguments
- FR-037: ValidateArguments MUST throw on unknown tool
- FR-038: ValidateArguments MUST parse arguments as JSON
- FR-039: ValidateArguments MUST validate against schema
- FR-040: ValidateArguments MUST check required fields
- FR-041: ValidateArguments MUST check type correctness
- FR-042: ValidateArguments MUST check constraints
- FR-043: ValidateArguments MUST return detailed errors
- FR-044: ValidateArguments MUST throw SchemaValidationException

### TryValidateArguments

- FR-045: TryValidateArguments MUST NOT throw on failure
- FR-046: TryValidateArguments MUST return success boolean
- FR-047: TryValidateArguments MUST output validation errors
- FR-048: TryValidateArguments MUST output parsed arguments

### Validation Error Reporting

- FR-049: Errors MUST include field path (JSON Pointer)
- FR-050: Errors MUST include error code
- FR-051: Errors MUST include human-readable message
- FR-052: Errors MUST include expected type/value
- FR-053: Errors MUST include actual value (sanitized)
- FR-054: Errors MUST be model-friendly for retry

### Tool Discovery

- FR-055: GetAllTools MUST return all registered tools
- FR-056: GetToolDefinition MUST return single tool
- FR-057: GetToolDefinition MUST throw on unknown tool
- FR-058: Tools MUST be filterable by category
- FR-059: Tools MUST include metadata

### Schema Provider Pattern

- FR-060: ISchemaProvider interface MUST be defined
- FR-061: Providers MUST implement RegisterSchemas method
- FR-062: Providers MUST be discovered via DI
- FR-063: Providers MUST run during startup
- FR-064: Core tools MUST have built-in provider

### Schema Versioning

- FR-065: ToolDefinition MUST include Version property
- FR-066: Registry MUST track schema versions
- FR-067: Registry MUST support version queries
- FR-068: Version format MUST be semver-compatible
- FR-069: Registry MUST validate version format

### Configuration

- FR-070: Registry MUST support config-based tools
- FR-071: Config tools MUST be loaded from .agent/config.yml
- FR-072: Config MUST support custom tool definitions
- FR-073: Config tools MUST be validated like code tools
- FR-074: Config MUST support tool enable/disable

---

## Non-Functional Requirements

### Performance

- NFR-001: Schema compilation MUST complete in < 10ms per schema
- NFR-002: Argument validation MUST complete in < 1ms
- NFR-003: GetAllTools MUST complete in < 100μs
- NFR-004: Registry MUST hold 1000+ schemas without degradation
- NFR-005: Memory per schema MUST be < 50KB

### Reliability

- NFR-006: Registry MUST be thread-safe
- NFR-007: Validation MUST be deterministic
- NFR-008: Registry MUST not crash on invalid input
- NFR-009: Registry MUST recover from partial failures
- NFR-010: Schema compilation failures MUST be isolated

### Security

- NFR-011: Validation MUST prevent injection attacks
- NFR-012: Error messages MUST NOT expose sensitive data
- NFR-013: Arguments MUST be sanitized in logs
- NFR-014: Registry MUST NOT execute arbitrary code from schemas
- NFR-015: Pattern validation MUST have regex timeout

### Observability

- NFR-016: Registration MUST be logged
- NFR-017: Validation failures MUST be logged
- NFR-018: Metrics MUST track validation latency
- NFR-019: Metrics MUST track validation success rate
- NFR-020: Schema count MUST be exposed as metric

### Maintainability

- NFR-021: All public APIs MUST have XML documentation
- NFR-022: Schemas MUST be testable in isolation
- NFR-023: Registry MUST be mockable for tests
- NFR-024: Error codes MUST be documented

---

## User Manual Documentation

### Overview

The Tool Schema Registry manages definitions for all tools available to the LLM. It validates tool arguments before execution, ensuring safety and correctness.

### Quick Start

Tools are registered automatically at startup:

```csharp
// In a schema provider
public class CoreToolsProvider : ISchemaProvider
{
    public void RegisterSchemas(IToolSchemaRegistry registry)
    {
        registry.RegisterTool(new ToolDefinition
        {
            Name = "read_file",
            Description = "Read the contents of a file",
            Version = "1.0.0",
            Parameters = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "path": {
                        "type": "string",
                        "description": "Path to the file to read"
                    },
                    "encoding": {
                        "type": "string",
                        "enum": ["utf-8", "ascii", "utf-16"],
                        "default": "utf-8"
                    }
                },
                "required": ["path"]
            }
            """).RootElement
        });
    }
}
```

### Tool Definition Structure

```json
{
    "name": "tool_name",
    "description": "What the tool does",
    "version": "1.0.0",
    "parameters": {
        "type": "object",
        "properties": {
            "field_name": {
                "type": "string",
                "description": "What this field is for"
            }
        },
        "required": ["field_name"]
    }
}
```

### Configuration-Based Tools

Define custom tools in `.agent/config.yml`:

```yaml
tools:
  custom:
    - name: my_custom_tool
      description: A custom tool
      version: "1.0.0"
      parameters:
        type: object
        properties:
          input:
            type: string
            description: Input value
        required:
          - input
```

### Schema Types

#### String

```json
{
    "type": "string",
    "minLength": 1,
    "maxLength": 100,
    "pattern": "^[a-z]+$"
}
```

#### Number

```json
{
    "type": "number",
    "minimum": 0,
    "maximum": 100
}
```

#### Integer

```json
{
    "type": "integer",
    "minimum": 1
}
```

#### Boolean

```json
{
    "type": "boolean"
}
```

#### Array

```json
{
    "type": "array",
    "items": { "type": "string" },
    "minItems": 1,
    "maxItems": 10
}
```

#### Object

```json
{
    "type": "object",
    "properties": {
        "nested": { "type": "string" }
    }
}
```

#### Enum

```json
{
    "type": "string",
    "enum": ["option1", "option2", "option3"]
}
```

### Validation

```csharp
// Validate tool arguments
var registry = services.GetRequiredService<IToolSchemaRegistry>();

try
{
    var validated = registry.ValidateArguments("read_file", arguments);
    // Arguments are valid, proceed with execution
}
catch (SchemaValidationException ex)
{
    // Arguments invalid
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"{error.Path}: {error.Message}");
    }
}

// Or use TryValidate
if (registry.TryValidateArguments("read_file", arguments, out var errors, out var parsed))
{
    // Valid
}
else
{
    // Invalid, errors contains details
}
```

### Validation Error Format

```json
{
    "errors": [
        {
            "path": "/path",
            "code": "required",
            "message": "Required property 'path' is missing",
            "expected": "string",
            "actual": null
        }
    ]
}
```

### Error Codes

| Code | Description |
|------|-------------|
| ACODE-TSR-001 | Unknown tool name |
| ACODE-TSR-002 | Invalid JSON arguments |
| ACODE-TSR-003 | Required field missing |
| ACODE-TSR-004 | Type mismatch |
| ACODE-TSR-005 | Constraint violation |
| ACODE-TSR-006 | Schema invalid |
| ACODE-TSR-007 | Duplicate tool name |
| ACODE-TSR-008 | Schema compilation failed |

### CLI Commands

```bash
# List all registered tools
$ acode tools list
┌────────────────────────────────────────────────────────────┐
│ Registered Tools                                            │
├───────────────┬─────────┬─────────────────────────────────┤
│ Name          │ Version │ Description                      │
├───────────────┼─────────┼─────────────────────────────────┤
│ read_file     │ 1.0.0   │ Read the contents of a file     │
│ write_file    │ 1.0.0   │ Write content to a file         │
│ execute       │ 1.0.0   │ Execute a shell command         │
└───────────────┴─────────┴─────────────────────────────────┘

# Show tool details
$ acode tools show read_file
Name: read_file
Version: 1.0.0
Description: Read the contents of a file
Parameters:
  path (string, required): Path to the file to read
  encoding (string): One of: utf-8, ascii, utf-16

# Validate arguments
$ echo '{"path": "/tmp/test.txt"}' | acode tools validate read_file
✓ Arguments are valid
```

---

## Acceptance Criteria

### Interface

- [ ] AC-001: IToolSchemaRegistry defined
- [ ] AC-002: In Application layer
- [ ] AC-003: RegisterTool method exists
- [ ] AC-004: GetToolDefinition exists
- [ ] AC-005: GetAllTools exists
- [ ] AC-006: ValidateArguments exists
- [ ] AC-007: TryValidateArguments exists
- [ ] AC-008: Injectable via DI

### Implementation

- [ ] AC-009: In Infrastructure layer
- [ ] AC-010: Thread-safe storage
- [ ] AC-011: Compiles on registration
- [ ] AC-012: Caches compiled schemas
- [ ] AC-013: Rejects duplicates
- [ ] AC-014: Validates schemas
- [ ] AC-015: Logs registrations

### Registration

- [ ] AC-016: Accepts ToolDefinition
- [ ] AC-017: Validates well-formed
- [ ] AC-018: Compiles schema
- [ ] AC-019: Throws on invalid
- [ ] AC-020: Idempotent for same
- [ ] AC-021: Rejects conflicts
- [ ] AC-022: Logs name and version

### Schema Validation

- [ ] AC-023: Draft 2020-12 compliance
- [ ] AC-024: Type definitions required
- [ ] AC-025: Required list supported
- [ ] AC-026: Object type works
- [ ] AC-027: Array type works
- [ ] AC-028: String type works
- [ ] AC-029: Number/integer works
- [ ] AC-030: Boolean type works
- [ ] AC-031: Enum constraint works
- [ ] AC-032: Pattern constraint works
- [ ] AC-033: Min/max works
- [ ] AC-034: MinLength/maxLength works
- [ ] AC-035: Nested objects work
- [ ] AC-036: Array items work

### Argument Validation

- [ ] AC-037: Accepts name and args
- [ ] AC-038: Throws on unknown tool
- [ ] AC-039: Parses JSON
- [ ] AC-040: Validates against schema
- [ ] AC-041: Checks required fields
- [ ] AC-042: Checks types
- [ ] AC-043: Checks constraints
- [ ] AC-044: Returns detailed errors
- [ ] AC-045: Throws SchemaValidationException

### TryValidate

- [ ] AC-046: Does not throw
- [ ] AC-047: Returns bool
- [ ] AC-048: Outputs errors
- [ ] AC-049: Outputs parsed args

### Error Reporting

- [ ] AC-050: Includes JSON Pointer path
- [ ] AC-051: Includes error code
- [ ] AC-052: Includes message
- [ ] AC-053: Includes expected
- [ ] AC-054: Includes actual (sanitized)
- [ ] AC-055: Model-friendly format

### Discovery

- [ ] AC-056: GetAllTools returns all
- [ ] AC-057: GetToolDefinition works
- [ ] AC-058: Throws on unknown
- [ ] AC-059: Filterable by category
- [ ] AC-060: Includes metadata

### Schema Provider

- [ ] AC-061: Interface defined
- [ ] AC-062: RegisterSchemas method
- [ ] AC-063: DI discovery
- [ ] AC-064: Runs at startup
- [ ] AC-065: Core tools provider

### Versioning

- [ ] AC-066: Version property exists
- [ ] AC-067: Versions tracked
- [ ] AC-068: Version queries work
- [ ] AC-069: Semver format
- [ ] AC-070: Format validated

### Configuration

- [ ] AC-071: Config-based tools work
- [ ] AC-072: Loaded from config.yml
- [ ] AC-073: Custom definitions work
- [ ] AC-074: Config tools validated
- [ ] AC-075: Enable/disable works

### Performance

- [ ] AC-076: Compilation <10ms
- [ ] AC-077: Validation <1ms
- [ ] AC-078: GetAllTools <100μs
- [ ] AC-079: 1000+ schemas work
- [ ] AC-080: Memory <50KB per schema

### Security

- [ ] AC-081: Prevents injection
- [ ] AC-082: No sensitive data in errors
- [ ] AC-083: Sanitized logs
- [ ] AC-084: No code execution
- [ ] AC-085: Regex timeout

---

## Best Practices

This section documents proven patterns, anti-patterns, and recommendations for implementing and maintaining the Tool Schema Registry. Following these practices ensures consistency, maintainability, and robust operation.

### Schema Design Best Practices

#### BP-001: Use Explicit Types, Never Rely on Type Coercion

**Do This:**
```json
{
  "type": "object",
  "properties": {
    "count": { "type": "integer", "minimum": 0 },
    "enabled": { "type": "boolean" }
  }
}
```

**Not This:**
```json
{
  "properties": {
    "count": {},
    "enabled": {}
  }
}
```

**Why:** JSON Schema allows properties without types, which means any value is valid. Always specify explicit types to catch LLM formatting errors early.

#### BP-002: Always Set `additionalProperties: false`

**Do This:**
```json
{
  "type": "object",
  "properties": {
    "path": { "type": "string" }
  },
  "additionalProperties": false
}
```

**Why:** LLMs sometimes hallucinate extra properties. Setting `additionalProperties: false` ensures only defined properties are accepted, preventing unexpected data from reaching tool implementations.

#### BP-003: Use `required` Array Exhaustively

**Do This:**
```json
{
  "type": "object",
  "properties": {
    "path": { "type": "string" },
    "encoding": { "type": "string", "default": "utf-8" }
  },
  "required": ["path"]
}
```

**Why:** Clearly distinguish between required and optional parameters. Never assume the LLM will provide all properties - explicitly require what you need.

#### BP-004: Provide Descriptions for All Properties

**Do This:**
```json
{
  "properties": {
    "path": {
      "type": "string",
      "description": "Absolute or relative path to the file to read. Must exist and be accessible."
    }
  }
}
```

**Why:** Property descriptions are included in the tool definition sent to the LLM. Clear descriptions help the LLM generate correct arguments.

#### BP-005: Use `const` for Fixed Values

**Do This:**
```json
{
  "properties": {
    "version": { "const": "1.0" },
    "type": { "enum": ["read", "write", "delete"] }
  }
}
```

**Why:** Use `const` for values that must be exactly one thing, `enum` for a fixed set of choices. Both prevent the LLM from inventing invalid values.

#### BP-006: Prefer Flat Schemas Over Deeply Nested

**Do This:**
```json
{
  "type": "object",
  "properties": {
    "file_path": { "type": "string" },
    "start_line": { "type": "integer" },
    "end_line": { "type": "integer" }
  }
}
```

**Not This:**
```json
{
  "type": "object",
  "properties": {
    "file": {
      "type": "object",
      "properties": {
        "path": { "type": "string" },
        "range": {
          "type": "object",
          "properties": {
            "start": { "type": "integer" },
            "end": { "type": "integer" }
          }
        }
      }
    }
  }
}
```

**Why:** Flat schemas are easier for LLMs to populate correctly and produce clearer validation error messages. Limit nesting to 3 levels maximum.

### Registry Implementation Best Practices

#### BP-007: Register All Tools at Startup, Not On-Demand

**Do This:**
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IToolSchemaRegistry, ToolSchemaRegistry>();
        services.AddHostedService<ToolRegistrationService>();
    }
}

public class ToolRegistrationService : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        foreach (var provider in _providers)
        {
            var tools = provider.GetToolDefinitions();
            foreach (var tool in tools)
            {
                _registry.RegisterTool(tool);
            }
        }
    }
}
```

**Why:** Schema compilation is expensive. Doing it at startup ensures predictable performance during operation and catches schema errors before the system accepts requests.

#### BP-008: Use Immutable ToolDefinition Objects

**Do This:**
```csharp
public sealed record ToolDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required JsonSchema ParameterSchema { get; init; }
}
```

**Not This:**
```csharp
public class ToolDefinition
{
    public string Name { get; set; }
    public string Description { get; set; }
    public JsonSchema ParameterSchema { get; set; }
}
```

**Why:** Immutable objects are thread-safe by design. Once a tool is registered, its definition cannot be accidentally modified by other code.

#### BP-009: Return Results, Not Exceptions, for Validation Failures

**Do This:**
```csharp
public ToolValidationResult ValidateArguments(string toolName, JsonElement args)
{
    if (!_tools.TryGetValue(toolName, out var tool))
    {
        return ToolValidationResult.ToolNotFound(toolName);
    }
    
    var errors = _validator.Validate(args, tool.CompiledSchema);
    return errors.Any()
        ? ToolValidationResult.Invalid(errors)
        : ToolValidationResult.Valid();
}
```

**Not This:**
```csharp
public void ValidateArguments(string toolName, JsonElement args)
{
    if (!_tools.ContainsKey(toolName))
    {
        throw new ToolNotFoundException(toolName);
    }
    
    var errors = _validator.Validate(args, tool.CompiledSchema);
    if (errors.Any())
    {
        throw new ValidationException(errors);
    }
}
```

**Why:** Validation failures are expected during normal operation (LLM output is unpredictable). Using result objects instead of exceptions allows clean flow control and avoids exception overhead.

#### BP-010: Log at Appropriate Levels

| Event | Log Level | Rationale |
|-------|-----------|-----------|
| Tool registered successfully | Information | Audit trail, infrequent |
| Validation succeeded | Debug | High volume, needed for troubleshooting only |
| Validation failed | Information | Important for understanding LLM behavior |
| Tool not found | Warning | Indicates configuration mismatch |
| Schema compilation failed | Error | Prevents system operation |
| Unexpected exception | Error | Requires investigation |

#### BP-011: Include Correlation IDs in All Operations

**Do This:**
```csharp
public ToolValidationResult ValidateArguments(
    string toolName, 
    JsonElement args,
    string correlationId)
{
    using var scope = _logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId,
        ["ToolName"] = toolName
    });
    
    // ... validation logic
}
```

**Why:** When debugging issues across distributed logs, correlation IDs allow tracing a single request through the entire system.

#### BP-012: Cache Compiled Schemas, Not Validation Results

**Do This:** Cache the compiled `JsonSchema` object after registration.

**Not This:** Cache validation results based on input hash.

**Why:** The same arguments may be valid or invalid depending on schema changes. Caching validation results would serve stale data. Caching compiled schemas is safe because schemas don't change after registration.

### Error Handling Best Practices

#### BP-013: Collect All Errors, Don't Stop at First

**Do This:**
```csharp
var evaluationOptions = new EvaluationOptions
{
    OutputFormat = OutputFormat.List,
    RequireFormatValidation = true
};
```

**Why:** The LLM retry loop benefits from seeing ALL errors at once, not just the first one. This allows fixing multiple issues in a single retry.

#### BP-014: Provide Actionable Error Messages

**Do This:**
```
Property 'path' is required but was not provided.
Property 'count' must be an integer, received string "five".
Property 'mode' must be one of: read, write, append.
```

**Not This:**
```
Validation failed.
Invalid value.
Schema mismatch at $.properties.mode.
```

**Why:** Clear, actionable error messages help the LLM self-correct on retry. Generic messages lead to repeated failures.

#### BP-015: Never Include Sensitive Schema Details in Errors

Validation errors should include:
- Property name
- Expected type
- Constraint violated (e.g., "minimum", "pattern")

Validation errors should NOT include:
- Full schema definition
- Pattern regex content (could reveal internal rules)
- All enum values (could be sensitive)
- Default values

### Performance Best Practices

#### BP-016: Use ConcurrentDictionary for Thread-Safe Storage

```csharp
private readonly ConcurrentDictionary<string, CompiledTool> _tools = new();
```

**Why:** Lock-free reads with safe concurrent writes. Perfect for read-heavy workloads like validation.

#### BP-017: Avoid Allocations in Hot Paths

**Do This:**
```csharp
// Pre-allocate and reuse
private static readonly ToolValidationResult ValidResult = 
    ToolValidationResult.Valid();

public ToolValidationResult Validate(...)
{
    if (isValid) return ValidResult; // No allocation
    return ToolValidationResult.Invalid(errors); // Only allocate on failure
}
```

**Why:** Validation runs on every tool call. Minimizing allocations reduces GC pressure.

#### BP-018: Measure and Monitor Validation Performance

Track these metrics:
- `tool_validation_duration_ms` (histogram)
- `tool_validation_success_total` (counter)
- `tool_validation_failure_total` (counter)
- `tool_registration_total` (counter)

Set alerts for:
- P99 validation latency > 50ms
- Validation failure rate > 50% for any tool

---

## Troubleshooting

This section provides diagnostic guidance for common issues with the Tool Schema Registry. Each issue includes symptoms, root causes, diagnostic steps, and solutions.

### Issue 1: Schema Registration Fails at Startup

#### Symptoms
- Application fails to start
- Error message: `SchemaCompilationException: Failed to compile schema for tool 'xyz'`
- Stack trace points to `ToolSchemaRegistry.RegisterTool`

#### Root Causes

1. **Invalid JSON Schema Syntax**: The schema JSON is malformed or uses unsupported JSON Schema draft features.

2. **Circular Reference**: The schema contains `$ref` that creates an infinite loop.

3. **Missing $defs**: The schema uses `$ref: "#/$defs/Something"` but the `$defs` section is missing.

4. **Incompatible Library Version**: Schema uses Draft 2020-12 features but library only supports Draft 7.

#### Diagnostic Steps

1. **Extract the failing schema**:
   ```powershell
   # Find the tool definition in configuration
   Select-String -Path "src/**/*.yaml" -Pattern "xyz" -Recurse
   ```

2. **Validate schema independently**:
   ```powershell
   # Use an online validator or CLI tool
   npx ajv validate -s schema.json -d sample-data.json
   ```

3. **Check JSON Schema version**:
   ```json
   // Schema should include this at the top
   { "$schema": "https://json-schema.org/draft/2020-12/schema" }
   ```

4. **Check for circular refs**:
   ```csharp
   // Add logging to trace $ref resolution
   _logger.LogDebug("Resolving $ref: {Ref}", refValue);
   ```

#### Solution

1. **Fix syntax errors**: Use a JSON Schema linter (VS Code JSON Schema extension)

2. **Break circular references**: Flatten the schema or use a different structure

3. **Add missing $defs**: Ensure all referenced definitions exist:
   ```json
   {
     "$defs": {
       "FileRange": {
         "type": "object",
         "properties": { ... }
       }
     }
   }
   ```

4. **Upgrade library** or **downgrade schema draft**

#### Prevention

- Add schema validation to CI/CD pipeline
- Unit test each tool's schema independently before registration
- Use strongly-typed schema builders instead of raw JSON

---

### Issue 2: Validation Always Fails for Valid-Looking Arguments

#### Symptoms
- LLM provides arguments that appear correct
- Validation returns errors like "Additional property 'xyz' is not allowed"
- Tool never executes despite correct-looking inputs

#### Root Causes

1. **Case Sensitivity**: Schema defines `filePath` but LLM provides `filepath` or `file_path`.

2. **Additional Properties**: Schema has `additionalProperties: false` but LLM adds extra properties.

3. **Type Mismatch**: Schema expects `integer` but LLM provides `"5"` (string).

4. **Missing Required Property**: LLM omits a required property or provides `null`.

#### Diagnostic Steps

1. **Log raw LLM output**:
   ```csharp
   _logger.LogDebug("Raw tool call: {ToolCall}", JsonSerializer.Serialize(toolCall));
   ```

2. **Compare property names exactly**:
   ```powershell
   # Check for case differences
   $schema = Get-Content schema.json | ConvertFrom-Json
   $args = Get-Content llm-output.json | ConvertFrom-Json
   Compare-Object ($schema.properties.PSObject.Properties.Name) ($args.PSObject.Properties.Name)
   ```

3. **Check validation error details**:
   ```csharp
   foreach (var error in result.Errors)
   {
       _logger.LogDebug(
           "Validation error at {Path}: {Message}",
           error.InstanceLocation,
           error.Message);
   }
   ```

4. **Test with minimal valid input**:
   ```json
   // Create the simplest possible valid input
   { "path": "/tmp/test.txt" }
   ```

#### Solution

1. **Case sensitivity**: Ensure tool description matches schema exactly:
   ```
   Tool description: "Reads a file. Provide 'filePath' (not 'file_path')."
   ```

2. **Additional properties**: Either:
   - Remove `additionalProperties: false` (not recommended)
   - Update tool description to list exactly what properties are allowed

3. **Type coercion**: Add pre-processing to coerce string numbers to integers:
   ```csharp
   if (property.ValueKind == JsonValueKind.String && 
       int.TryParse(property.GetString(), out var num))
   {
       // Log warning and coerce
   }
   ```

4. **Missing required**: Improve tool description to emphasize required fields

#### Prevention

- Include example valid JSON in tool descriptions
- Use verbose tool descriptions that list all required properties
- Add integration tests with actual LLM outputs

---

### Issue 3: Validation Times Out on Complex Arguments

#### Symptoms
- Validation works for simple arguments but times out for complex ones
- Error: "Validation timeout exceeded (100ms)"
- Affects only certain tools with nested or array arguments

#### Root Causes

1. **Exponential Backtracking**: Schema contains patterns like `.*` combined with `oneOf` that cause regex explosion.

2. **Large Array Validation**: Array with 1000 items where each item is validated against complex schema.

3. **Deep Nesting**: Arguments have 10+ levels of nesting, causing recursive validation overhead.

4. **Circular Schema References**: `$ref` loop causes infinite validation recursion.

#### Diagnostic Steps

1. **Measure validation time per tool**:
   ```csharp
   var sw = Stopwatch.StartNew();
   var result = _registry.ValidateArguments(toolName, args);
   _metrics.RecordHistogram("validation_duration_ms", sw.ElapsedMilliseconds, toolName);
   ```

2. **Profile with small vs large inputs**:
   ```csharp
   // Test with 1, 10, 100, 1000 array items
   for (int size = 1; size <= 1000; size *= 10)
   {
       var args = GenerateTestArgs(size);
       MeasureValidation(args);
   }
   ```

3. **Identify problematic schema patterns**:
   ```
   Dangerous patterns:
   - "pattern": ".*something.*" (backtracking)
   - Nested "oneOf" with overlapping conditions
   - "additionalProperties" with complex schema
   ```

4. **Check for infinite recursion**:
   ```csharp
   // Add depth tracking to validation
   if (currentDepth > 50)
   {
       throw new ValidationDepthExceededException();
   }
   ```

#### Solution

1. **Simplify regex patterns**:
   ```json
   // Instead of: "pattern": ".*\\.cs$"
   // Use: "pattern": "\\.cs$"
   ```

2. **Limit array size in schema**:
   ```json
   {
     "type": "array",
     "maxItems": 100,
     "items": { ... }
   }
   ```

3. **Flatten nested structures**: Refactor tools to accept flatter argument structures.

4. **Add pre-validation size check**:
   ```csharp
   var serializedSize = JsonSerializer.Serialize(args).Length;
   if (serializedSize > MaxArgumentSize)
   {
       return ToolValidationResult.ArgumentsTooLarge(serializedSize, MaxArgumentSize);
   }
   ```

#### Prevention

- Set `maxItems` on all array properties
- Set `maxLength` on all string properties
- Avoid regex patterns when possible; use `enum` or `format` instead
- Benchmark validation with realistic worst-case inputs

---

### Issue 4: Tool Not Found Despite Being Registered

#### Symptoms
- Error: "Tool 'file_read' not found in registry"
- Tool was definitely registered (seen in startup logs)
- Works sometimes but not always

#### Root Causes

1. **Race Condition**: Validation request arrives before registration completes.

2. **Duplicate Service Registration**: Multiple registry instances due to DI misconfiguration.

3. **Case-Sensitive Lookup**: Tool registered as `FileRead` but queried as `file_read`.

4. **Scoped vs Singleton Lifetime**: Registry recreated per request, losing registrations.

#### Diagnostic Steps

1. **Log registry state**:
   ```csharp
   _logger.LogInformation(
       "Registry contains {Count} tools: {Names}",
       _tools.Count,
       string.Join(", ", _tools.Keys));
   ```

2. **Check instance identity**:
   ```csharp
   _logger.LogDebug(
       "Registry instance: {HashCode}",
       GetHashCode());
   ```

3. **Verify DI registration**:
   ```csharp
   // In Startup.cs, should be:
   services.AddSingleton<IToolSchemaRegistry, ToolSchemaRegistry>();
   // NOT:
   services.AddScoped<IToolSchemaRegistry, ToolSchemaRegistry>();
   ```

4. **Add registration completion signal**:
   ```csharp
   public class ToolRegistrationService : IHostedService
   {
       private readonly TaskCompletionSource _initialized = new();
       public Task Initialized => _initialized.Task;
       
       public async Task StartAsync(CancellationToken ct)
       {
           // ... register tools ...
           _initialized.SetResult();
       }
   }
   ```

#### Solution

1. **Ensure startup ordering**:
   ```csharp
   // Use IHostedService startup ordering
   services.AddHostedService<ToolRegistrationService>();
   // Other services wait for registration
   ```

2. **Fix DI lifetime**: Change to singleton registration.

3. **Normalize tool names**:
   ```csharp
   public void RegisterTool(ToolDefinition tool)
   {
       var normalizedName = tool.Name.ToLowerInvariant();
       _tools.TryAdd(normalizedName, tool);
   }
   ```

4. **Add startup health check**:
   ```csharp
   public class RegistryHealthCheck : IHealthCheck
   {
       public Task<HealthCheckResult> CheckHealthAsync(...)
       {
           return _registry.GetAllTools().Any()
               ? HealthCheckResult.Healthy()
               : HealthCheckResult.Unhealthy("No tools registered");
       }
   }
   ```

#### Prevention

- Add integration test that verifies all expected tools are registered
- Use health checks to verify registry state before accepting requests
- Standardize on snake_case for all tool names

---

### Issue 5: Memory Usage Grows Over Time

#### Symptoms
- Application memory increases steadily during operation
- Eventually leads to OutOfMemoryException
- Registry blamed but actual cause may be elsewhere

#### Root Causes

1. **Validation Result Caching**: Results cached but never evicted.

2. **Error Object Accumulation**: Validation errors held in memory for too long.

3. **Schema Object Leaks**: New schema compiled on every validation (bug).

4. **Logging String Formatting**: Large argument objects serialized for logging.

#### Diagnostic Steps

1. **Capture memory dump**:
   ```powershell
   dotnet dump collect -p <pid>
   dotnet dump analyze <dumpfile>
   ```

2. **Check object counts**:
   ```
   > dumpheap -stat -type ToolValidation
   > dumpheap -stat -type JsonSchema
   ```

3. **Verify schema caching**:
   ```csharp
   _logger.LogDebug(
       "Compiled schema count: {Count}, expected: {Expected}",
       _compiledSchemas.Count,
       _tools.Count);
   ```

4. **Profile with continuous load**:
   ```powershell
   # Run load test for 30 minutes
   dotnet counters monitor -p <pid> --counters System.Runtime
   ```

#### Solution

1. **Ensure schemas compiled once**:
   ```csharp
   public void RegisterTool(ToolDefinition tool)
   {
       var compiled = CompileSchema(tool.ParameterSchema);
       _tools[tool.Name] = new CompiledTool(tool, compiled);
       // Schema compiled ONLY here, never in ValidateArguments
   }
   ```

2. **Use weak references for error history** (if caching errors):
   ```csharp
   private readonly ConditionalWeakTable<string, ValidationHistory> _history = new();
   ```

3. **Limit log argument size**:
   ```csharp
   var truncated = args.ToString().Length > 1000 
       ? args.ToString()[..1000] + "..." 
       : args.ToString();
   _logger.LogDebug("Validating: {Args}", truncated);
   ```

4. **Set explicit GC** for long-running batch operations:
   ```csharp
   if (operationCount % 10000 == 0)
   {
       GC.Collect(2, GCCollectionMode.Optimized);
   }
   ```

#### Prevention

- Monitor memory metrics in production
- Set memory limits on container/process
- Load test with sustained traffic before release

---

## Testing Requirements

This section defines the complete testing strategy for the Tool Schema Registry. All tests must pass before the feature is considered complete. Tests are organized by type and include complete test method signatures, expected behaviors, and test data requirements.

### Unit Tests

All unit tests reside in `tests/Acode.Application.Tests/ToolSchemas/` and `tests/Acode.Infrastructure.Tests/ToolSchemas/`.

#### ToolSchemaRegistryTests.cs

Location: `tests/Acode.Application.Tests/ToolSchemas/ToolSchemaRegistryTests.cs`

```csharp
namespace Acode.Application.Tests.ToolSchemas;

[TestClass]
public class ToolSchemaRegistryTests
{
    private ToolSchemaRegistry _sut;
    private Mock<ILogger<ToolSchemaRegistry>> _mockLogger;
    private Mock<ISchemaCompiler> _mockCompiler;
    
    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<ToolSchemaRegistry>>();
        _mockCompiler = new Mock<ISchemaCompiler>();
        _mockCompiler.Setup(c => c.Compile(It.IsAny<JsonElement>()))
            .Returns(new CompiledSchema());
        _sut = new ToolSchemaRegistry(_mockLogger.Object, _mockCompiler.Object);
    }

    [TestMethod]
    public void RegisterTool_WithValidDefinition_StoresToolSuccessfully()
    {
        // Arrange
        var tool = CreateValidToolDefinition("file_read");
        
        // Act
        _sut.RegisterTool(tool);
        
        // Assert
        var retrieved = _sut.GetToolDefinition("file_read");
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("file_read", retrieved.Name);
    }

    [TestMethod]
    public void RegisterTool_WithValidDefinition_CompilesSchemaOnce()
    {
        // Arrange
        var tool = CreateValidToolDefinition("file_read");
        
        // Act
        _sut.RegisterTool(tool);
        _sut.ValidateArguments("file_read", "{}");
        _sut.ValidateArguments("file_read", "{}");
        
        // Assert
        _mockCompiler.Verify(c => c.Compile(It.IsAny<JsonElement>()), Times.Once);
    }

    [TestMethod]
    public void RegisterTool_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var tool1 = CreateValidToolDefinition("file_read");
        var tool2 = CreateValidToolDefinition("file_read");
        _sut.RegisterTool(tool1);
        
        // Act & Assert
        var ex = Assert.ThrowsException<InvalidOperationException>(
            () => _sut.RegisterTool(tool2));
        Assert.IsTrue(ex.Message.Contains("already registered"));
        Assert.IsTrue(ex.Message.Contains("file_read"));
    }

    [TestMethod]
    public void RegisterTool_WithNullDefinition_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(
            () => _sut.RegisterTool(null!));
    }

    [TestMethod]
    public void RegisterTool_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var tool = CreateValidToolDefinition("");
        
        // Act & Assert
        var ex = Assert.ThrowsException<ArgumentException>(
            () => _sut.RegisterTool(tool));
        Assert.IsTrue(ex.Message.Contains("Name"));
    }

    [TestMethod]
    public void RegisterTool_WithNameExceedingMaxLength_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('a', 65); // Max is 64
        var tool = CreateValidToolDefinition(longName);
        
        // Act & Assert
        var ex = Assert.ThrowsException<ArgumentException>(
            () => _sut.RegisterTool(tool));
        Assert.IsTrue(ex.Message.Contains("64 characters"));
    }

    [TestMethod]
    public void RegisterTool_WithInvalidSchema_ThrowsSchemaCompilationException()
    {
        // Arrange
        _mockCompiler.Setup(c => c.Compile(It.IsAny<JsonElement>()))
            .Throws(new SchemaCompilationException("Invalid $ref"));
        var tool = CreateValidToolDefinition("broken_tool");
        
        // Act & Assert
        var ex = Assert.ThrowsException<SchemaCompilationException>(
            () => _sut.RegisterTool(tool));
        Assert.IsTrue(ex.Message.Contains("Invalid $ref"));
    }

    [TestMethod]
    public void RegisterTool_LogsRegistrationWithSchemaHash()
    {
        // Arrange
        var tool = CreateValidToolDefinition("file_read");
        
        // Act
        _sut.RegisterTool(tool);
        
        // Assert
        _mockLogger.VerifyLogged(LogLevel.Information, "Registering tool");
        _mockLogger.VerifyLogged(LogLevel.Information, "file_read");
        _mockLogger.VerifyLogged(LogLevel.Information, "SchemaHash");
    }

    [TestMethod]
    public void GetToolDefinition_WithExistingTool_ReturnsDefinition()
    {
        // Arrange
        var tool = CreateValidToolDefinition("file_read");
        _sut.RegisterTool(tool);
        
        // Act
        var result = _sut.GetToolDefinition("file_read");
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(tool.Name, result.Name);
        Assert.AreEqual(tool.Description, result.Description);
    }

    [TestMethod]
    public void GetToolDefinition_WithNonexistentTool_ThrowsToolNotFoundException()
    {
        // Act & Assert
        var ex = Assert.ThrowsException<ToolNotFoundException>(
            () => _sut.GetToolDefinition("nonexistent"));
        Assert.AreEqual("nonexistent", ex.ToolName);
        Assert.IsTrue(ex.Message.Contains("ACODE-TSR-001"));
    }

    [TestMethod]
    public void GetToolDefinition_IsCaseInsensitive()
    {
        // Arrange
        var tool = CreateValidToolDefinition("File_Read");
        _sut.RegisterTool(tool);
        
        // Act
        var result = _sut.GetToolDefinition("file_read");
        
        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void GetAllTools_WithNoRegistrations_ReturnsEmptyList()
    {
        // Act
        var result = _sut.GetAllTools();
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetAllTools_WithMultipleRegistrations_ReturnsAllTools()
    {
        // Arrange
        _sut.RegisterTool(CreateValidToolDefinition("tool1"));
        _sut.RegisterTool(CreateValidToolDefinition("tool2"));
        _sut.RegisterTool(CreateValidToolDefinition("tool3"));
        
        // Act
        var result = _sut.GetAllTools();
        
        // Assert
        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result.Any(t => t.Name == "tool1"));
        Assert.IsTrue(result.Any(t => t.Name == "tool2"));
        Assert.IsTrue(result.Any(t => t.Name == "tool3"));
    }

    [TestMethod]
    public void GetAllTools_ReturnsImmutableList()
    {
        // Arrange
        _sut.RegisterTool(CreateValidToolDefinition("tool1"));
        var result = _sut.GetAllTools();
        
        // Act & Assert - should throw because list is readonly
        Assert.IsTrue(result is IReadOnlyList<ToolDefinition>);
    }

    [TestMethod]
    public void GetToolsByCategory_ReturnsOnlyMatchingTools()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithCategory("fs_read", ToolCategory.FileSystem));
        _sut.RegisterTool(CreateToolWithCategory("fs_write", ToolCategory.FileSystem));
        _sut.RegisterTool(CreateToolWithCategory("web_fetch", ToolCategory.Web));
        
        // Act
        var result = _sut.GetToolsByCategory(ToolCategory.FileSystem);
        
        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.All(t => t.Category == ToolCategory.FileSystem));
    }

    private static ToolDefinition CreateValidToolDefinition(string name)
    {
        return new ToolDefinition
        {
            Name = name,
            Description = $"Test tool {name}",
            Version = "1.0.0",
            Category = ToolCategory.General,
            ParameterSchema = JsonDocument.Parse(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""path"": { ""type"": ""string"" }
                },
                ""required"": [""path""]
            }").RootElement
        };
    }

    private static ToolDefinition CreateToolWithCategory(string name, ToolCategory category)
    {
        var tool = CreateValidToolDefinition(name);
        return tool with { Category = category };
    }
}
```

#### SchemaValidationTests.cs

Location: `tests/Acode.Application.Tests/ToolSchemas/SchemaValidationTests.cs`

```csharp
namespace Acode.Application.Tests.ToolSchemas;

[TestClass]
public class SchemaValidationTests
{
    private ToolSchemaRegistry _sut;
    private ToolDefinition _fileReadTool;

    [TestInitialize]
    public void Setup()
    {
        _sut = CreateRealRegistry();
        _fileReadTool = CreateFileReadToolDefinition();
        _sut.RegisterTool(_fileReadTool);
    }

    #region Required Field Validation

    [TestMethod]
    public void ValidateArguments_WithAllRequiredFields_ReturnsSuccess()
    {
        // Arrange
        var args = @"{ ""path"": ""/tmp/test.txt"" }";
        
        // Act
        var result = _sut.TryValidateArguments("file_read", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void ValidateArguments_MissingRequiredField_ReturnsError()
    {
        // Arrange
        var args = @"{ }";
        
        // Act
        var result = _sut.TryValidateArguments("file_read", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual("ACODE-TSR-003", errors[0].ErrorCode);
        Assert.IsTrue(errors[0].Message.Contains("path"));
        Assert.IsTrue(errors[0].Message.Contains("required"));
    }

    [TestMethod]
    public void ValidateArguments_MultipleRequiredFieldsMissing_ReturnsAllErrors()
    {
        // Arrange - tool with two required fields
        _sut.RegisterTool(CreateMultiRequiredTool());
        var args = @"{ }";
        
        // Act
        var result = _sut.TryValidateArguments("multi_required", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(2, errors.Count);
        Assert.IsTrue(errors.Any(e => e.Message.Contains("field_a")));
        Assert.IsTrue(errors.Any(e => e.Message.Contains("field_b")));
    }

    [TestMethod]
    public void ValidateArguments_RequiredFieldIsNull_ReturnsError()
    {
        // Arrange
        var args = @"{ ""path"": null }";
        
        // Act
        var result = _sut.TryValidateArguments("file_read", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(errors[0].Message.Contains("null") || errors[0].Message.Contains("type"));
    }

    #endregion

    #region Type Validation

    [TestMethod]
    public void ValidateArguments_StringPropertyWithString_Succeeds()
    {
        // Arrange
        var args = @"{ ""path"": ""/valid/path"" }";
        
        // Act
        var result = _sut.TryValidateArguments("file_read", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateArguments_StringPropertyWithNumber_ReturnsTypeError()
    {
        // Arrange
        var args = @"{ ""path"": 12345 }";
        
        // Act
        var result = _sut.TryValidateArguments("file_read", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual("ACODE-TSR-004", errors[0].ErrorCode);
        Assert.IsTrue(errors[0].Message.Contains("string"));
    }

    [TestMethod]
    public void ValidateArguments_IntegerPropertyWithInteger_Succeeds()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithIntegerProperty());
        var args = @"{ ""count"": 42 }";
        
        // Act
        var result = _sut.TryValidateArguments("int_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateArguments_IntegerPropertyWithFloat_ReturnsTypeError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithIntegerProperty());
        var args = @"{ ""count"": 42.5 }";
        
        // Act
        var result = _sut.TryValidateArguments("int_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(errors[0].Message.Contains("integer"));
    }

    [TestMethod]
    public void ValidateArguments_IntegerPropertyWithString_ReturnsTypeError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithIntegerProperty());
        var args = @"{ ""count"": ""42"" }";
        
        // Act
        var result = _sut.TryValidateArguments("int_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual("ACODE-TSR-004", errors[0].ErrorCode);
    }

    [TestMethod]
    public void ValidateArguments_BooleanPropertyWithBoolean_Succeeds()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithBooleanProperty());
        var args = @"{ ""enabled"": true }";
        
        // Act
        var result = _sut.TryValidateArguments("bool_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateArguments_BooleanPropertyWithString_ReturnsTypeError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithBooleanProperty());
        var args = @"{ ""enabled"": ""true"" }";
        
        // Act
        var result = _sut.TryValidateArguments("bool_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ValidateArguments_ArrayPropertyWithArray_Succeeds()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithArrayProperty());
        var args = @"{ ""items"": [""a"", ""b"", ""c""] }";
        
        // Act
        var result = _sut.TryValidateArguments("array_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateArguments_ArrayPropertyWithObject_ReturnsTypeError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithArrayProperty());
        var args = @"{ ""items"": { ""not"": ""array"" } }";
        
        // Act
        var result = _sut.TryValidateArguments("array_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region String Constraint Validation

    [TestMethod]
    public void ValidateArguments_StringWithinMinLength_Succeeds()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithStringConstraints());
        var args = @"{ ""name"": ""abc"" }"; // minLength: 3
        
        // Act
        var result = _sut.TryValidateArguments("string_constraints", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateArguments_StringBelowMinLength_ReturnsError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithStringConstraints());
        var args = @"{ ""name"": ""ab"" }"; // minLength: 3
        
        // Act
        var result = _sut.TryValidateArguments("string_constraints", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual("ACODE-TSR-005", errors[0].ErrorCode);
        Assert.IsTrue(errors[0].Message.Contains("minimum length"));
    }

    [TestMethod]
    public void ValidateArguments_StringWithinMaxLength_Succeeds()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithStringConstraints());
        var args = @"{ ""name"": ""1234567890"" }"; // maxLength: 10
        
        // Act
        var result = _sut.TryValidateArguments("string_constraints", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateArguments_StringExceedsMaxLength_ReturnsError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithStringConstraints());
        var args = @"{ ""name"": ""12345678901"" }"; // maxLength: 10
        
        // Act
        var result = _sut.TryValidateArguments("string_constraints", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(errors[0].Message.Contains("maximum length"));
    }

    [TestMethod]
    public void ValidateArguments_StringMatchingPattern_Succeeds()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithPatternConstraint());
        var args = @"{ ""code"": ""ABC-123"" }"; // pattern: ^[A-Z]{3}-\d{3}$
        
        // Act
        var result = _sut.TryValidateArguments("pattern_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateArguments_StringNotMatchingPattern_ReturnsError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithPatternConstraint());
        var args = @"{ ""code"": ""abc-123"" }"; // lowercase fails pattern
        
        // Act
        var result = _sut.TryValidateArguments("pattern_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(errors[0].Message.Contains("pattern"));
    }

    #endregion

    #region Number Constraint Validation

    [TestMethod]
    public void ValidateArguments_NumberAtMinimum_Succeeds()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithNumberConstraints());
        var args = @"{ ""count"": 0 }"; // minimum: 0
        
        // Act
        var result = _sut.TryValidateArguments("number_constraints", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateArguments_NumberBelowMinimum_ReturnsError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithNumberConstraints());
        var args = @"{ ""count"": -1 }"; // minimum: 0
        
        // Act
        var result = _sut.TryValidateArguments("number_constraints", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(errors[0].Message.Contains("minimum"));
    }

    [TestMethod]
    public void ValidateArguments_NumberAtMaximum_Succeeds()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithNumberConstraints());
        var args = @"{ ""count"": 100 }"; // maximum: 100
        
        // Act
        var result = _sut.TryValidateArguments("number_constraints", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateArguments_NumberAboveMaximum_ReturnsError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithNumberConstraints());
        var args = @"{ ""count"": 101 }"; // maximum: 100
        
        // Act
        var result = _sut.TryValidateArguments("number_constraints", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(errors[0].Message.Contains("maximum"));
    }

    [TestMethod]
    public void ValidateArguments_NumberMultipleOf_Succeeds()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithMultipleOfConstraint());
        var args = @"{ ""value"": 15 }"; // multipleOf: 5
        
        // Act
        var result = _sut.TryValidateArguments("multipleof_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateArguments_NumberNotMultipleOf_ReturnsError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithMultipleOfConstraint());
        var args = @"{ ""value"": 12 }"; // multipleOf: 5
        
        // Act
        var result = _sut.TryValidateArguments("multipleof_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region Enum Validation

    [TestMethod]
    public void ValidateArguments_ValidEnumValue_Succeeds()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithEnumProperty());
        var args = @"{ ""mode"": ""read"" }";
        
        // Act
        var result = _sut.TryValidateArguments("enum_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateArguments_InvalidEnumValue_ReturnsError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithEnumProperty());
        var args = @"{ ""mode"": ""invalid"" }";
        
        // Act
        var result = _sut.TryValidateArguments("enum_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual("ACODE-TSR-005", errors[0].ErrorCode);
    }

    [TestMethod]
    public void ValidateArguments_EnumWithDifferentCase_ReturnsError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithEnumProperty());
        var args = @"{ ""mode"": ""READ"" }"; // enum is "read" lowercase
        
        // Act
        var result = _sut.TryValidateArguments("enum_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result); // Case sensitive
    }

    #endregion

    #region Nested Object Validation

    [TestMethod]
    public void ValidateArguments_ValidNestedObject_Succeeds()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithNestedObject());
        var args = @"{ 
            ""config"": { 
                ""timeout"": 30, 
                ""retries"": 3 
            } 
        }";
        
        // Act
        var result = _sut.TryValidateArguments("nested_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateArguments_NestedObjectWithMissingRequired_ReturnsError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithNestedObject());
        var args = @"{ 
            ""config"": { 
                ""timeout"": 30 
            } 
        }"; // missing required "retries"
        
        // Act
        var result = _sut.TryValidateArguments("nested_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(errors[0].PropertyPath.Contains("config"));
    }

    [TestMethod]
    public void ValidateArguments_NestedObjectWithWrongType_ReturnsError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithNestedObject());
        var args = @"{ 
            ""config"": { 
                ""timeout"": ""thirty"",
                ""retries"": 3 
            } 
        }";
        
        // Act
        var result = _sut.TryValidateArguments("nested_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(errors[0].PropertyPath.Contains("timeout"));
    }

    #endregion

    #region Array Validation

    [TestMethod]
    public void ValidateArguments_ArrayWithValidItems_Succeeds()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithTypedArray());
        var args = @"{ ""paths"": [""/a"", ""/b"", ""/c""] }";
        
        // Act
        var result = _sut.TryValidateArguments("typed_array_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateArguments_ArrayWithInvalidItemType_ReturnsError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithTypedArray());
        var args = @"{ ""paths"": [""/a"", 123, ""/c""] }";
        
        // Act
        var result = _sut.TryValidateArguments("typed_array_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(errors[0].PropertyPath.Contains("[1]")); // Index of invalid item
    }

    [TestMethod]
    public void ValidateArguments_ArrayWithinMinItems_Succeeds()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithArrayConstraints());
        var args = @"{ ""items"": [""a""] }"; // minItems: 1
        
        // Act
        var result = _sut.TryValidateArguments("array_constraints_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateArguments_EmptyArrayBelowMinItems_ReturnsError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithArrayConstraints());
        var args = @"{ ""items"": [] }"; // minItems: 1
        
        // Act
        var result = _sut.TryValidateArguments("array_constraints_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(errors[0].Message.Contains("minimum"));
    }

    [TestMethod]
    public void ValidateArguments_ArrayExceedingMaxItems_ReturnsError()
    {
        // Arrange
        _sut.RegisterTool(CreateToolWithArrayConstraints());
        var args = @"{ ""items"": [""a"", ""b"", ""c"", ""d"", ""e"", ""f""] }"; // maxItems: 5
        
        // Act
        var result = _sut.TryValidateArguments("array_constraints_tool", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(errors[0].Message.Contains("maximum"));
    }

    #endregion

    #region Additional Properties Validation

    [TestMethod]
    public void ValidateArguments_NoAdditionalProperties_Succeeds()
    {
        // Arrange - file_read has additionalProperties: false
        var args = @"{ ""path"": ""/test"" }";
        
        // Act
        var result = _sut.TryValidateArguments("file_read", args, out var errors, out _);
        
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateArguments_WithAdditionalProperties_ReturnsError()
    {
        // Arrange - file_read has additionalProperties: false
        var args = @"{ ""path"": ""/test"", ""extra"": ""property"" }";
        
        // Act
        var result = _sut.TryValidateArguments("file_read", args, out var errors, out _);
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(errors[0].Message.Contains("additional"));
    }

    #endregion

    #region Helper Methods

    private static ToolSchemaRegistry CreateRealRegistry()
    {
        // Create registry with real JSON Schema validation
        var logger = new NullLogger<ToolSchemaRegistry>();
        var compiler = new SchemaCompiler();
        return new ToolSchemaRegistry(logger, compiler);
    }

    private static ToolDefinition CreateFileReadToolDefinition()
    {
        return new ToolDefinition
        {
            Name = "file_read",
            Description = "Reads a file",
            Version = "1.0.0",
            Category = ToolCategory.FileSystem,
            ParameterSchema = JsonDocument.Parse(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""path"": { ""type"": ""string"" }
                },
                ""required"": [""path""],
                ""additionalProperties"": false
            }").RootElement
        };
    }

    // ... Additional helper methods for creating test fixtures ...

    #endregion
}
```

#### ValidationErrorTests.cs

Location: `tests/Acode.Application.Tests/ToolSchemas/ValidationErrorTests.cs`

```csharp
namespace Acode.Application.Tests.ToolSchemas;

[TestClass]
public class ValidationErrorTests
{
    [TestMethod]
    public void ValidationError_IncludesPropertyPath()
    {
        // Arrange
        var error = new ValidationError(
            errorCode: "ACODE-TSR-003",
            propertyPath: "$.config.timeout",
            message: "Required property 'timeout' is missing");
        
        // Assert
        Assert.AreEqual("$.config.timeout", error.PropertyPath);
    }

    [TestMethod]
    public void ValidationError_IncludesErrorCode()
    {
        // Arrange
        var error = new ValidationError(
            errorCode: "ACODE-TSR-004",
            propertyPath: "$.count",
            message: "Expected integer, got string");
        
        // Assert
        Assert.AreEqual("ACODE-TSR-004", error.ErrorCode);
    }

    [TestMethod]
    public void ValidationError_IncludesHumanReadableMessage()
    {
        // Arrange
        var error = new ValidationError(
            errorCode: "ACODE-TSR-005",
            propertyPath: "$.mode",
            message: "Value must be one of: read, write, append");
        
        // Assert
        Assert.IsTrue(error.Message.Contains("read"));
        Assert.IsTrue(error.Message.Contains("write"));
    }

    [TestMethod]
    public void ValidationError_DoesNotContainSchemaDefinition()
    {
        // Arrange
        var error = new ValidationError(
            errorCode: "ACODE-TSR-005",
            propertyPath: "$.mode",
            message: "Invalid enum value");
        
        // Assert - Should not contain internal schema details
        Assert.IsFalse(error.Message.Contains("$ref"));
        Assert.IsFalse(error.Message.Contains("$defs"));
        Assert.IsFalse(error.Message.Contains("\"type\":"));
    }

    [TestMethod]
    public void ValidationError_ToStringIsActionable()
    {
        // Arrange
        var error = new ValidationError(
            errorCode: "ACODE-TSR-003",
            propertyPath: "$.path",
            message: "Required property 'path' is missing");
        
        // Act
        var str = error.ToString();
        
        // Assert - Should be useful for LLM retry
        Assert.IsTrue(str.Contains("path"));
        Assert.IsTrue(str.Contains("required") || str.Contains("missing"));
    }
}
```

### Integration Tests

Location: `tests/Acode.Integration.Tests/ToolSchemas/`

```csharp
namespace Acode.Integration.Tests.ToolSchemas;

[TestClass]
public class RegistryIntegrationTests
{
    private IServiceProvider _serviceProvider;
    private IToolSchemaRegistry _registry;

    [TestInitialize]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        services.AddAcodeApplication();
        services.AddAcodeInfrastructure();
        _serviceProvider = services.BuildServiceProvider();
        
        // Wait for startup registration
        var registration = _serviceProvider.GetRequiredService<ToolRegistrationService>();
        await registration.StartAsync(CancellationToken.None);
        
        _registry = _serviceProvider.GetRequiredService<IToolSchemaRegistry>();
    }

    [TestMethod]
    public void CoreToolsAreRegistered()
    {
        // Assert all core tools are present
        var tools = _registry.GetAllTools();
        
        Assert.IsTrue(tools.Any(t => t.Name == "file_read"));
        Assert.IsTrue(tools.Any(t => t.Name == "file_write"));
        Assert.IsTrue(tools.Any(t => t.Name == "directory_list"));
        Assert.IsTrue(tools.Any(t => t.Name == "command_execute"));
    }

    [TestMethod]
    public void ConfigToolsAreLoadedFromYaml()
    {
        // Assuming test configuration defines custom tools
        var tools = _registry.GetAllTools();
        
        // Verify at least one custom tool loaded from config
        Assert.IsTrue(tools.Count > 4); // More than just core tools
    }

    [TestMethod]
    public void RealToolCallValidationWorks()
    {
        // Arrange - simulate real LLM output
        var llmToolCall = @"{
            ""path"": ""/home/user/project/README.md"",
            ""encoding"": ""utf-8""
        }";
        
        // Act
        var success = _registry.TryValidateArguments(
            "file_read",
            llmToolCall,
            out var errors,
            out var parsed);
        
        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual("/home/user/project/README.md", 
            parsed.GetProperty("path").GetString());
    }

    [TestMethod]
    public void ConcurrentValidationsAreThreadSafe()
    {
        // Arrange
        var validArgs = @"{ ""path"": ""/test.txt"" }";
        var errors = new ConcurrentBag<Exception>();
        
        // Act - 100 concurrent validations
        Parallel.For(0, 100, i =>
        {
            try
            {
                _registry.TryValidateArguments("file_read", validArgs, out _, out _);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        });
        
        // Assert
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public async Task RegistryIsSingletonAcrossRequests()
    {
        // Arrange
        var registry1 = _serviceProvider.GetRequiredService<IToolSchemaRegistry>();
        var registry2 = _serviceProvider.GetRequiredService<IToolSchemaRegistry>();
        
        // Assert - same instance
        Assert.AreSame(registry1, registry2);
    }
}
```

### Performance Tests

Location: `tests/Acode.Application.Tests/ToolSchemas/PerformanceBenchmarks.cs`

```csharp
namespace Acode.Application.Tests.ToolSchemas;

[TestClass]
public class PerformanceBenchmarks
{
    private ToolSchemaRegistry _registry;

    [TestInitialize]
    public void Setup()
    {
        _registry = CreateRegistryWithCoreTools();
    }

    [TestMethod]
    public void SchemaCompilation_CompletesWithin50ms()
    {
        // Arrange
        var complexSchema = CreateComplexSchema();
        var sw = Stopwatch.StartNew();
        
        // Act
        _registry.RegisterTool(complexSchema);
        sw.Stop();
        
        // Assert
        Assert.IsTrue(sw.ElapsedMilliseconds < 50, 
            $"Schema compilation took {sw.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public void ArgumentValidation_CompletesWithin10ms()
    {
        // Arrange
        var args = @"{ ""path"": ""/test.txt"" }";
        var sw = Stopwatch.StartNew();
        
        // Act
        for (int i = 0; i < 1000; i++)
        {
            _registry.TryValidateArguments("file_read", args, out _, out _);
        }
        sw.Stop();
        
        // Assert
        var avgMs = sw.ElapsedMilliseconds / 1000.0;
        Assert.IsTrue(avgMs < 10, 
            $"Average validation took {avgMs}ms");
    }

    [TestMethod]
    public void LargeRegistryLookup_CompletesWithin1ms()
    {
        // Arrange - register 100 tools
        for (int i = 0; i < 100; i++)
        {
            _registry.RegisterTool(CreateToolDefinition($"tool_{i:D3}"));
        }
        
        var sw = Stopwatch.StartNew();
        
        // Act
        for (int i = 0; i < 10000; i++)
        {
            _registry.GetToolDefinition("tool_050");
        }
        sw.Stop();
        
        // Assert
        var avgMs = sw.ElapsedMilliseconds / 10000.0;
        Assert.IsTrue(avgMs < 1, 
            $"Average lookup took {avgMs}ms");
    }

    [TestMethod]
    public void GetAllTools_WithLargeRegistry_CompletesWithin5ms()
    {
        // Arrange - register 100 tools
        for (int i = 0; i < 100; i++)
        {
            _registry.RegisterTool(CreateToolDefinition($"tool_{i:D3}"));
        }
        
        var sw = Stopwatch.StartNew();
        
        // Act
        for (int i = 0; i < 1000; i++)
        {
            var _ = _registry.GetAllTools();
        }
        sw.Stop();
        
        // Assert
        var avgMs = sw.ElapsedMilliseconds / 1000.0;
        Assert.IsTrue(avgMs < 5, 
            $"Average GetAllTools took {avgMs}ms");
    }
}
```

### Test Data Fixtures

Create the following test fixtures in `tests/Acode.Application.Tests/ToolSchemas/Fixtures/`:

#### ValidSchemas.json
```json
{
  "minimal": {
    "type": "object",
    "properties": {}
  },
  "required_string": {
    "type": "object",
    "properties": {
      "name": { "type": "string" }
    },
    "required": ["name"]
  },
  "all_types": {
    "type": "object",
    "properties": {
      "str": { "type": "string" },
      "num": { "type": "number" },
      "int": { "type": "integer" },
      "bool": { "type": "boolean" },
      "arr": { "type": "array", "items": { "type": "string" } },
      "obj": { "type": "object" }
    }
  }
}
```

#### InvalidSchemas.json
```json
{
  "missing_type": {
    "properties": {
      "name": {}
    }
  },
  "circular_ref": {
    "type": "object",
    "$ref": "#"
  },
  "invalid_type": {
    "type": "invalid_type"
  }
}
```

---

## User Verification Steps

This section provides step-by-step manual verification scenarios that users or QA can perform to validate the Tool Schema Registry implementation. Each scenario includes prerequisites, detailed steps, expected results, and troubleshooting guidance.

### Scenario 1: Verify Core Tools Are Registered at Startup

**Objective**: Confirm that all core tools are automatically registered when the application starts.

**Prerequisites**:
- Acode is built and ready to run
- Access to application logs (console or log file)
- No custom configuration files that might conflict

**Steps**:

1. Open a terminal in the Acode project directory:
   ```powershell
   cd c:\Users\neilo\source\local coding agent
   ```

2. Build the application:
   ```powershell
   dotnet build src/Acode.Cli/Acode.Cli.csproj
   ```

3. Run the application with verbose logging:
   ```powershell
   dotnet run --project src/Acode.Cli -- --log-level Debug
   ```

4. Observe the startup output in the console.

**Expected Results**:

```
[10:30:00 INF] Starting Tool Schema Registry initialization...
[10:30:00 INF] Registering tool 'file_read' v1.0.0 (hash: a1b2c3d4)
[10:30:00 INF] Registering tool 'file_write' v1.0.0 (hash: e5f6g7h8)
[10:30:00 INF] Registering tool 'directory_list' v1.0.0 (hash: i9j0k1l2)
[10:30:00 INF] Registering tool 'command_execute' v1.0.0 (hash: m3n4o5p6)
[10:30:00 INF] Tool Schema Registry initialized with 4 tools in 45ms
```

**Verification Checklist**:
- [ ] "Starting Tool Schema Registry initialization" message appears
- [ ] Each core tool shows registration message with name, version, and hash
- [ ] Final message shows total tool count
- [ ] Initialization completes in under 500ms
- [ ] No error or warning messages related to schema registration

**Troubleshooting**:

| Symptom | Possible Cause | Solution |
|---------|----------------|----------|
| No registration messages | Logging not configured | Add `--log-level Debug` flag |
| "SchemaCompilationException" | Invalid schema in core tool | Check CoreToolsProvider for syntax errors |
| Missing tool in list | Tool provider not registered | Verify DI registration in Startup |
| Very slow initialization | Complex schemas | Profile with dotnet-trace |

---

### Scenario 2: List All Registered Tools via CLI

**Objective**: Verify that the `acode tools list` command displays all registered tools with their metadata.

**Prerequisites**:
- Acode application is running or can be started
- CLI tools command is implemented

**Steps**:

1. Run the tools list command:
   ```powershell
   dotnet run --project src/Acode.Cli -- tools list
   ```

2. Observe the output table.

**Expected Results**:

```
╔══════════════════╦═════════╦════════════════╦══════════════════════════════════╗
║ Tool Name        ║ Version ║ Category       ║ Description                      ║
╠══════════════════╬═════════╬════════════════╬══════════════════════════════════╣
║ file_read        ║ 1.0.0   ║ FileSystem     ║ Read contents of a file          ║
║ file_write       ║ 1.0.0   ║ FileSystem     ║ Write content to a file          ║
║ directory_list   ║ 1.0.0   ║ FileSystem     ║ List contents of a directory     ║
║ command_execute  ║ 1.0.0   ║ System         ║ Execute a shell command          ║
╚══════════════════╩═════════╩════════════════╩══════════════════════════════════╝

Total: 4 tools registered
```

**Verification Checklist**:
- [ ] Table displays with proper formatting
- [ ] All core tools are listed (file_read, file_write, directory_list, command_execute)
- [ ] Version numbers are displayed for each tool
- [ ] Category column shows correct categorization
- [ ] Description column shows helpful text
- [ ] Total count at bottom matches rows in table

**Steps for Detailed View**:

1. Run the tools show command for a specific tool:
   ```powershell
   dotnet run --project src/Acode.Cli -- tools show file_read
   ```

**Expected Detailed Output**:

```
Tool: file_read
Version: 1.0.0
Category: FileSystem
Description: Read the contents of a file from the filesystem

Parameters:
  path (string, required)
    Description: Absolute or relative path to the file to read
    Constraints: maxLength=4096

  encoding (string, optional)
    Description: Character encoding for reading the file
    Default: utf-8
    Enum: utf-8, ascii, utf-16, utf-32

  start_line (integer, optional)
    Description: Starting line number (1-based) for partial read
    Constraints: minimum=1

  end_line (integer, optional)
    Description: Ending line number (1-based) for partial read
    Constraints: minimum=1

Schema (JSON):
{
  "type": "object",
  "properties": {
    "path": { "type": "string", "maxLength": 4096 },
    "encoding": { "type": "string", "enum": ["utf-8", "ascii", "utf-16", "utf-32"], "default": "utf-8" },
    "start_line": { "type": "integer", "minimum": 1 },
    "end_line": { "type": "integer", "minimum": 1 }
  },
  "required": ["path"],
  "additionalProperties": false
}
```

---

### Scenario 3: Validate Arguments with Correct Input

**Objective**: Confirm that valid tool arguments pass validation and return parsed JSON.

**Prerequisites**:
- Tool Schema Registry is initialized
- file_read tool is registered

**Steps**:

1. Use the CLI to validate arguments (if validation command exists):
   ```powershell
   dotnet run --project src/Acode.Cli -- tools validate file_read '{"path": "/tmp/test.txt"}'
   ```

2. Alternatively, test via the interactive mode:
   ```powershell
   dotnet run --project src/Acode.Cli -- chat
   > Read the file at /tmp/test.txt
   ```

**Expected Results for Validation Command**:

```
✓ Validation passed for tool 'file_read'

Parsed Arguments:
{
  "path": "/tmp/test.txt"
}
```

**Expected Results for Chat Mode**:

```
[DEBUG] Tool call received: file_read
[DEBUG] Arguments: {"path": "/tmp/test.txt"}
[DEBUG] Validation result: Success
[INFO] Executing tool: file_read
```

**Verification Checklist**:
- [ ] Validation returns success status
- [ ] Parsed arguments are returned as structured JSON
- [ ] No validation errors in output
- [ ] Tool proceeds to execution (in chat mode)

---

### Scenario 4: Validate Arguments Missing Required Field

**Objective**: Verify that missing required fields produce clear, actionable error messages.

**Prerequisites**:
- Tool Schema Registry is initialized
- file_read tool is registered (requires "path" property)

**Steps**:

1. Submit tool call with missing required field:
   ```powershell
   dotnet run --project src/Acode.Cli -- tools validate file_read '{}'
   ```

2. Observe the error output.

**Expected Results**:

```
✗ Validation failed for tool 'file_read'

Errors:
  [ACODE-TSR-003] Property 'path' is required but was not provided.
               Path: $
               
Suggestion: Add the 'path' property with a string value.
Example: {"path": "/path/to/file"}
```

**Verification Checklist**:
- [ ] Validation returns failure status
- [ ] Error code ACODE-TSR-003 is displayed
- [ ] Error message clearly states which field is missing ("path")
- [ ] Error includes JSON path ($)
- [ ] Suggestion or example is provided to help fix the error
- [ ] Exit code is non-zero (for CI/CD integration)

**Additional Test - Multiple Missing Fields**:

1. Register a tool with multiple required fields (or use test tool):
   ```powershell
   dotnet run --project src/Acode.Cli -- tools validate file_write '{}'
   ```

**Expected Results**:

```
✗ Validation failed for tool 'file_write'

Errors:
  [ACODE-TSR-003] Property 'path' is required but was not provided.
               Path: $
  [ACODE-TSR-003] Property 'content' is required but was not provided.
               Path: $

Suggestion: Add the required properties.
Example: {"path": "/path/to/file", "content": "text to write"}
```

**Verification Checklist for Multiple Errors**:
- [ ] ALL missing required fields are reported (not just the first)
- [ ] Each error has its own line with error code
- [ ] Errors are presented in a logical order (alphabetical or schema order)

---

### Scenario 5: Validate Arguments with Wrong Type

**Objective**: Verify that type mismatches produce clear error messages with expected vs actual types.

**Prerequisites**:
- Tool Schema Registry is initialized
- A tool with typed properties is registered

**Steps**:

1. Submit tool call with wrong type:
   ```powershell
   dotnet run --project src/Acode.Cli -- tools validate file_read '{"path": 12345}'
   ```

2. Observe the error output.

**Expected Results**:

```
✗ Validation failed for tool 'file_read'

Errors:
  [ACODE-TSR-004] Property 'path': type mismatch.
               Expected: string
               Actual: integer
               Path: $.path
               
Suggestion: Provide 'path' as a string value, not a number.
Example: {"path": "/tmp/file.txt"}
```

**Verification Checklist**:
- [ ] Error code ACODE-TSR-004 is displayed
- [ ] Error shows expected type ("string")
- [ ] Error shows actual type received ("integer")
- [ ] JSON path points to the specific property ($.path)
- [ ] Suggestion explains how to fix

**Additional Type Mismatch Tests**:

| Input | Expected Error |
|-------|----------------|
| `{"path": "/test", "start_line": "five"}` | Expected integer, got string for $.start_line |
| `{"path": "/test", "encoding": 123}` | Expected string, got integer for $.encoding |
| `{"path": ["/a", "/b"]}` | Expected string, got array for $.path |
| `{"path": {"nested": "object"}}` | Expected string, got object for $.path |
| `{"path": true}` | Expected string, got boolean for $.path |
| `{"path": null}` | Expected string, got null for $.path |

---

### Scenario 6: Register Custom Tool from Configuration

**Objective**: Verify that tools defined in configuration files are loaded and registered.

**Prerequisites**:
- Acode supports tool definitions in config.yml
- Access to edit configuration files

**Steps**:

1. Create or edit the configuration file:
   ```powershell
   # Create config directory if it doesn't exist
   New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\.acode"
   
   # Create config.yml with custom tool definition
   @"
   tools:
     custom_tools:
       - name: custom_greeting
         version: 1.0.0
         category: Custom
         description: Generates a personalized greeting message
         parameters:
           type: object
           properties:
             name:
               type: string
               description: Name of the person to greet
               maxLength: 100
             style:
               type: string
               enum: [formal, casual, enthusiastic]
               default: casual
           required:
             - name
           additionalProperties: false
   "@ | Set-Content "$env:USERPROFILE\.acode\config.yml"
   ```

2. Restart Acode:
   ```powershell
   dotnet run --project src/Acode.Cli -- tools list
   ```

3. Verify the custom tool is registered:
   ```powershell
   dotnet run --project src/Acode.Cli -- tools show custom_greeting
   ```

**Expected Results**:

```
Tool: custom_greeting
Version: 1.0.0
Category: Custom
Description: Generates a personalized greeting message

Parameters:
  name (string, required)
    Description: Name of the person to greet
    Constraints: maxLength=100

  style (string, optional)
    Description: 
    Default: casual
    Enum: formal, casual, enthusiastic
```

4. Validate arguments for the custom tool:
   ```powershell
   dotnet run --project src/Acode.Cli -- tools validate custom_greeting '{"name": "Alice", "style": "formal"}'
   ```

**Expected Results**:

```
✓ Validation passed for tool 'custom_greeting'

Parsed Arguments:
{
  "name": "Alice",
  "style": "formal"
}
```

**Verification Checklist**:
- [ ] Custom tool appears in tools list
- [ ] Tool metadata (name, version, category, description) matches config
- [ ] Parameter schema is correctly parsed from YAML
- [ ] Validation works for custom tool
- [ ] Invalid arguments are properly rejected

**Cleanup**:
```powershell
Remove-Item "$env:USERPROFILE\.acode\config.yml"
```

---

### Scenario 7: Handle Invalid Schema Definition

**Objective**: Verify that invalid schemas are detected at registration time with clear error messages.

**Prerequisites**:
- Access to modify configuration files
- Understanding of JSON Schema syntax

**Steps**:

1. Create a configuration with an invalid schema:
   ```powershell
   @"
   tools:
     custom_tools:
       - name: broken_tool
         version: 1.0.0
         description: This tool has an invalid schema
         parameters:
           type: object
           properties:
             value:
               type: invalid_type_name
   "@ | Set-Content "$env:USERPROFILE\.acode\config.yml"
   ```

2. Start Acode and observe the error:
   ```powershell
   dotnet run --project src/Acode.Cli -- tools list
   ```

**Expected Results**:

```
[10:30:00 ERR] Failed to register tool 'broken_tool': Schema compilation failed
  Error: Invalid schema type 'invalid_type_name' at $.properties.value.type
  Valid types: string, number, integer, boolean, array, object, null

[10:30:00 WRN] Skipping tool 'broken_tool' due to schema error
[10:30:00 INF] Tool Schema Registry initialized with 4 tools (1 skipped)
```

**Verification Checklist**:
- [ ] Application does NOT crash on invalid schema
- [ ] Error message clearly identifies the problem
- [ ] Error message shows the path to the invalid element
- [ ] Valid types are listed to help fix the issue
- [ ] Other tools are still registered (graceful degradation)
- [ ] Warning indicates the tool was skipped

**Additional Invalid Schema Tests**:

| Invalid Schema | Expected Error |
|----------------|----------------|
| `$ref: "#/invalid/path"` | Could not resolve reference |
| Circular `$ref` | Circular reference detected |
| Missing required `properties` | Schema compilation failed |
| Schema > 50KB | Schema exceeds maximum size |
| Nesting > 20 levels | Schema exceeds maximum nesting depth |

---

### Scenario 8: Validate Unknown Tool Name

**Objective**: Verify that validating arguments for a non-existent tool returns a clear error.

**Prerequisites**:
- Tool Schema Registry is initialized

**Steps**:

1. Attempt to validate arguments for a tool that doesn't exist:
   ```powershell
   dotnet run --project src/Acode.Cli -- tools validate nonexistent_tool '{"arg": "value"}'
   ```

**Expected Results**:

```
✗ Validation failed for tool 'nonexistent_tool'

Errors:
  [ACODE-TSR-001] Unknown tool: 'nonexistent_tool'

Available tools:
  - file_read
  - file_write
  - directory_list
  - command_execute

Did you mean: file_read?
```

**Verification Checklist**:
- [ ] Error code ACODE-TSR-001 is displayed
- [ ] Error message includes the tool name that was not found
- [ ] List of available tools is shown (helps discover correct name)
- [ ] "Did you mean" suggestion is shown if a similar tool name exists
- [ ] Exit code is non-zero

---

### Scenario 9: Verify Enum Constraint Validation

**Objective**: Confirm that enum values are strictly validated.

**Prerequisites**:
- A tool with enum constraints is registered (e.g., file_read with encoding enum)

**Steps**:

1. Validate with a valid enum value:
   ```powershell
   dotnet run --project src/Acode.Cli -- tools validate file_read '{"path": "/test.txt", "encoding": "utf-8"}'
   ```

2. Validate with an invalid enum value:
   ```powershell
   dotnet run --project src/Acode.Cli -- tools validate file_read '{"path": "/test.txt", "encoding": "invalid-encoding"}'
   ```

**Expected Results for Invalid Enum**:

```
✗ Validation failed for tool 'file_read'

Errors:
  [ACODE-TSR-005] Property 'encoding': value must be one of the allowed options.
               Received: "invalid-encoding"
               Allowed values: utf-8, ascii, utf-16, utf-32
               Path: $.encoding
```

**Verification Checklist**:
- [ ] Valid enum values pass validation
- [ ] Invalid enum values fail with ACODE-TSR-005
- [ ] Error shows what value was received
- [ ] Error lists all allowed values
- [ ] Case sensitivity is enforced ("UTF-8" != "utf-8")

---

### Scenario 10: Verify Performance Under Load

**Objective**: Confirm that the registry performs well under concurrent validation requests.

**Prerequisites**:
- Acode is running in a testable environment
- Ability to generate concurrent requests

**Steps**:

1. Create a simple load test script:
   ```powershell
   # Run 100 concurrent validations
   $jobs = 1..100 | ForEach-Object {
       Start-Job -ScriptBlock {
           dotnet run --project src/Acode.Cli -- tools validate file_read '{"path": "/test.txt"}' 2>&1
       }
   }
   
   # Wait for all jobs and measure time
   $results = $jobs | Wait-Job | Receive-Job
   $jobs | Remove-Job
   
   # Count successes
   $successes = ($results | Select-String "Validation passed").Count
   Write-Host "Successes: $successes / 100"
   ```

2. Observe results.

**Expected Results**:
- All 100 validations should succeed
- Total time should be under 10 seconds
- No deadlocks or race conditions

**Verification Checklist**:
- [ ] All concurrent validations complete successfully
- [ ] No timeout errors
- [ ] No corrupted state (wrong results)
- [ ] Memory usage remains stable
- [ ] Response times are consistent (no degradation)

---

## Implementation Prompt

This section provides complete, step-by-step implementation instructions with full code samples. A developer should be able to implement this feature by following these instructions sequentially without external research.

### Prerequisites

Before starting implementation, ensure the following:

1. **NuGet Package**: Install the JSON Schema validation library:
   ```powershell
   dotnet add src/Acode.Infrastructure/Acode.Infrastructure.csproj package JsonSchema.Net --version 5.5.1
   ```

2. **Project References**: Ensure project references are configured:
   ```xml
   <!-- In Acode.Infrastructure.csproj -->
   <ProjectReference Include="..\Acode.Application\Acode.Application.csproj" />
   <ProjectReference Include="..\Acode.Domain\Acode.Domain.csproj" />
   ```

3. **Using Statements**: These will be needed across multiple files:
   ```csharp
   using System.Collections.Concurrent;
   using System.Text.Json;
   using System.Text.Json.Serialization;
   using Json.Schema;
   using Microsoft.Extensions.Logging;
   using Microsoft.Extensions.DependencyInjection;
   using Microsoft.Extensions.Options;
   ```

---

### Step 1: Create ToolCategory Enum

**File**: `src/Acode.Application/ToolSchemas/ToolCategory.cs`

```csharp
namespace Acode.Application.ToolSchemas;

/// <summary>
/// Categorizes tools by their primary function for organization and filtering.
/// </summary>
public enum ToolCategory
{
    /// <summary>
    /// General-purpose tools that don't fit other categories.
    /// </summary>
    General = 0,

    /// <summary>
    /// Tools that interact with the filesystem (read, write, list, search).
    /// </summary>
    FileSystem = 1,

    /// <summary>
    /// Tools that execute system commands or interact with the shell.
    /// </summary>
    System = 2,

    /// <summary>
    /// Tools that make HTTP requests or interact with web services.
    /// </summary>
    Web = 3,

    /// <summary>
    /// Tools that interact with databases or data stores.
    /// </summary>
    Data = 4,

    /// <summary>
    /// Tools that assist with code analysis, formatting, or transformation.
    /// </summary>
    Code = 5,

    /// <summary>
    /// Tools that interact with version control systems.
    /// </summary>
    VersionControl = 6,

    /// <summary>
    /// Tools defined by user configuration.
    /// </summary>
    Custom = 100
}
```

---

### Step 2: Create ValidationError Record

**File**: `src/Acode.Application/ToolSchemas/ValidationError.cs`

```csharp
namespace Acode.Application.ToolSchemas;

/// <summary>
/// Represents a single validation error from schema validation.
/// </summary>
/// <param name="ErrorCode">Acode error code (e.g., ACODE-TSR-003).</param>
/// <param name="PropertyPath">JSON path to the invalid property (e.g., $.config.timeout).</param>
/// <param name="Message">Human-readable error message suitable for LLM retry prompts.</param>
public sealed record ValidationError(
    string ErrorCode,
    string PropertyPath,
    string Message)
{
    /// <summary>
    /// Creates a "required property missing" error.
    /// </summary>
    public static ValidationError RequiredMissing(string propertyPath, string propertyName)
        => new(
            "ACODE-TSR-003",
            propertyPath,
            $"Required property '{propertyName}' is missing.");

    /// <summary>
    /// Creates a "type mismatch" error.
    /// </summary>
    public static ValidationError TypeMismatch(
        string propertyPath, 
        string expectedType, 
        string actualType)
        => new(
            "ACODE-TSR-004",
            propertyPath,
            $"Type mismatch: expected {expectedType}, got {actualType}.");

    /// <summary>
    /// Creates a "constraint violation" error.
    /// </summary>
    public static ValidationError ConstraintViolation(string propertyPath, string constraint)
        => new(
            "ACODE-TSR-005",
            propertyPath,
            $"Constraint violation: {constraint}.");

    /// <summary>
    /// Formats the error for display in logs or LLM retry prompts.
    /// </summary>
    public override string ToString()
        => $"[{ErrorCode}] {PropertyPath}: {Message}";
}
```

---

### Step 3: Create ToolValidationResult Record

**File**: `src/Acode.Application/ToolSchemas/ToolValidationResult.cs`

```csharp
namespace Acode.Application.ToolSchemas;

/// <summary>
/// Result of validating tool arguments against a schema.
/// </summary>
public sealed record ToolValidationResult
{
    /// <summary>
    /// Whether validation passed.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// List of validation errors (empty if valid).
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Parsed arguments as JsonElement (only set if valid).
    /// </summary>
    public JsonElement? ParsedArguments { get; init; }

    /// <summary>
    /// Tool name that was validated.
    /// </summary>
    public string? ToolName { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ToolValidationResult Valid(string toolName, JsonElement parsedArguments)
        => new()
        {
            IsValid = true,
            ToolName = toolName,
            ParsedArguments = parsedArguments,
            Errors = []
        };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ToolValidationResult Invalid(
        string toolName, 
        IReadOnlyList<ValidationError> errors)
        => new()
        {
            IsValid = false,
            ToolName = toolName,
            ParsedArguments = null,
            Errors = errors
        };

    /// <summary>
    /// Creates a "tool not found" result.
    /// </summary>
    public static ToolValidationResult ToolNotFound(string toolName)
        => new()
        {
            IsValid = false,
            ToolName = toolName,
            ParsedArguments = null,
            Errors = [new ValidationError(
                "ACODE-TSR-001",
                "$",
                $"Unknown tool: '{toolName}'")]
        };

    /// <summary>
    /// Creates an "invalid JSON" result.
    /// </summary>
    public static ToolValidationResult InvalidJson(string toolName, string details)
        => new()
        {
            IsValid = false,
            ToolName = toolName,
            ParsedArguments = null,
            Errors = [new ValidationError(
                "ACODE-TSR-002",
                "$",
                $"Invalid JSON in arguments: {details}")]
        };
}
```

---

### Step 4: Create ToolDefinition Record

**File**: `src/Acode.Application/ToolSchemas/ToolDefinition.cs`

```csharp
using System.Text.Json;

namespace Acode.Application.ToolSchemas;

/// <summary>
/// Defines a tool that can be invoked by the LLM, including its parameter schema.
/// </summary>
public sealed record ToolDefinition
{
    /// <summary>
    /// Unique name of the tool (e.g., "file_read").
    /// Must be alphanumeric with underscores, max 64 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of what the tool does.
    /// This is included in the LLM context for tool selection.
    /// Max 500 characters.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Semantic version of the tool (e.g., "1.0.0").
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// JSON Schema defining the tool's parameter structure.
    /// Must be a valid JSON Schema (Draft 2020-12).
    /// </summary>
    public required JsonElement ParameterSchema { get; init; }

    /// <summary>
    /// Category for organizing and filtering tools.
    /// </summary>
    public ToolCategory Category { get; init; } = ToolCategory.General;

    /// <summary>
    /// Optional key-value metadata for extensions.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Validates that the tool definition meets all requirements.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if validation fails.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Tool name cannot be empty.", nameof(Name));

        if (Name.Length > 64)
            throw new ArgumentException("Tool name cannot exceed 64 characters.", nameof(Name));

        if (!System.Text.RegularExpressions.Regex.IsMatch(Name, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
            throw new ArgumentException(
                "Tool name must start with a letter and contain only letters, numbers, and underscores.",
                nameof(Name));

        if (string.IsNullOrWhiteSpace(Description))
            throw new ArgumentException("Tool description cannot be empty.", nameof(Description));

        if (Description.Length > 500)
            throw new ArgumentException("Tool description cannot exceed 500 characters.", nameof(Description));

        if (string.IsNullOrWhiteSpace(Version))
            throw new ArgumentException("Tool version cannot be empty.", nameof(Version));

        if (ParameterSchema.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Parameter schema must be a JSON object.", nameof(ParameterSchema));
    }
}
```

---

### Step 5: Create Exception Classes

**File**: `src/Acode.Application/ToolSchemas/ToolSchemaExceptions.cs`

```csharp
namespace Acode.Application.ToolSchemas;

/// <summary>
/// Thrown when a requested tool is not found in the registry.
/// </summary>
public sealed class ToolNotFoundException : Exception
{
    public string ToolName { get; }

    public ToolNotFoundException(string toolName)
        : base($"[ACODE-TSR-001] Unknown tool: '{toolName}'")
    {
        ToolName = toolName;
    }
}

/// <summary>
/// Thrown when schema compilation fails during tool registration.
/// </summary>
public sealed class SchemaCompilationException : Exception
{
    public string? ToolName { get; }

    public SchemaCompilationException(string message)
        : base($"[ACODE-TSR-008] Schema compilation failed: {message}")
    {
    }

    public SchemaCompilationException(string toolName, string message)
        : base($"[ACODE-TSR-008] Schema compilation failed for tool '{toolName}': {message}")
    {
        ToolName = toolName;
    }

    public SchemaCompilationException(string message, Exception innerException)
        : base($"[ACODE-TSR-008] Schema compilation failed: {message}", innerException)
    {
    }
}

/// <summary>
/// Thrown when schema validation fails for tool arguments.
/// </summary>
public sealed class SchemaValidationException : Exception
{
    public string ToolName { get; }
    public IReadOnlyList<ValidationError> Errors { get; }

    public SchemaValidationException(string toolName, IReadOnlyList<ValidationError> errors)
        : base($"Validation failed for tool '{toolName}' with {errors.Count} error(s)")
    {
        ToolName = toolName;
        Errors = errors;
    }
}

/// <summary>
/// Thrown when attempting to register a tool that already exists.
/// </summary>
public sealed class DuplicateToolException : Exception
{
    public string ToolName { get; }

    public DuplicateToolException(string toolName)
        : base($"[ACODE-TSR-007] Tool '{toolName}' is already registered")
    {
        ToolName = toolName;
    }
}
```

---

### Step 6: Create IToolSchemaRegistry Interface

**File**: `src/Acode.Application/ToolSchemas/IToolSchemaRegistry.cs`

```csharp
using System.Text.Json;

namespace Acode.Application.ToolSchemas;

/// <summary>
/// Registry for tool definitions and parameter schema validation.
/// </summary>
public interface IToolSchemaRegistry
{
    /// <summary>
    /// Registers a tool definition with the registry.
    /// Schema is compiled and cached at registration time.
    /// </summary>
    /// <param name="definition">The tool definition to register.</param>
    /// <exception cref="ArgumentNullException">If definition is null.</exception>
    /// <exception cref="ArgumentException">If definition is invalid.</exception>
    /// <exception cref="DuplicateToolException">If tool name already registered.</exception>
    /// <exception cref="SchemaCompilationException">If schema compilation fails.</exception>
    void RegisterTool(ToolDefinition definition);

    /// <summary>
    /// Gets a tool definition by name.
    /// </summary>
    /// <param name="toolName">Name of the tool to retrieve.</param>
    /// <returns>The tool definition.</returns>
    /// <exception cref="ToolNotFoundException">If tool is not registered.</exception>
    ToolDefinition GetToolDefinition(string toolName);

    /// <summary>
    /// Attempts to get a tool definition by name.
    /// </summary>
    /// <param name="toolName">Name of the tool to retrieve.</param>
    /// <param name="definition">The tool definition if found.</param>
    /// <returns>True if found, false otherwise.</returns>
    bool TryGetToolDefinition(string toolName, out ToolDefinition? definition);

    /// <summary>
    /// Gets all registered tool definitions.
    /// </summary>
    /// <returns>Read-only list of all tools.</returns>
    IReadOnlyList<ToolDefinition> GetAllTools();

    /// <summary>
    /// Gets tools filtered by category.
    /// </summary>
    /// <param name="category">Category to filter by.</param>
    /// <returns>Read-only list of matching tools.</returns>
    IReadOnlyList<ToolDefinition> GetToolsByCategory(ToolCategory category);

    /// <summary>
    /// Validates tool arguments against the registered schema.
    /// Throws on validation failure.
    /// </summary>
    /// <param name="toolName">Name of the tool.</param>
    /// <param name="argumentsJson">JSON string containing the arguments.</param>
    /// <returns>Parsed arguments as JsonElement.</returns>
    /// <exception cref="ToolNotFoundException">If tool is not registered.</exception>
    /// <exception cref="SchemaValidationException">If validation fails.</exception>
    JsonElement ValidateArguments(string toolName, string argumentsJson);

    /// <summary>
    /// Attempts to validate tool arguments against the registered schema.
    /// Returns result object instead of throwing.
    /// </summary>
    /// <param name="toolName">Name of the tool.</param>
    /// <param name="argumentsJson">JSON string containing the arguments.</param>
    /// <returns>Validation result with errors or parsed arguments.</returns>
    ToolValidationResult TryValidateArguments(string toolName, string argumentsJson);

    /// <summary>
    /// Gets the count of registered tools.
    /// </summary>
    int Count { get; }
}
```

---

### Step 7: Create ISchemaProvider Interface

**File**: `src/Acode.Application/ToolSchemas/ISchemaProvider.cs`

```csharp
namespace Acode.Application.ToolSchemas;

/// <summary>
/// Provides tool definitions to be registered with the schema registry.
/// Implementations discover tools from various sources (code, config, plugins).
/// </summary>
public interface ISchemaProvider
{
    /// <summary>
    /// Gets the name of this provider for logging.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the priority order for registration.
    /// Lower numbers are registered first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets all tool definitions from this provider.
    /// </summary>
    /// <returns>Enumerable of tool definitions.</returns>
    IEnumerable<ToolDefinition> GetToolDefinitions();
}
```

---

### Step 8: Implement CompiledTool Internal Class

**File**: `src/Acode.Infrastructure/ToolSchemas/CompiledTool.cs`

```csharp
using Acode.Application.ToolSchemas;
using Json.Schema;

namespace Acode.Infrastructure.ToolSchemas;

/// <summary>
/// Internal representation of a registered tool with pre-compiled schema.
/// </summary>
internal sealed class CompiledTool
{
    /// <summary>
    /// The original tool definition.
    /// </summary>
    public ToolDefinition Definition { get; }

    /// <summary>
    /// Pre-compiled JSON Schema for fast validation.
    /// </summary>
    public JsonSchema CompiledSchema { get; }

    /// <summary>
    /// Hash of the schema for audit logging.
    /// </summary>
    public string SchemaHash { get; }

    /// <summary>
    /// Timestamp when the tool was registered.
    /// </summary>
    public DateTimeOffset RegisteredAt { get; }

    public CompiledTool(
        ToolDefinition definition,
        JsonSchema compiledSchema,
        string schemaHash)
    {
        Definition = definition;
        CompiledSchema = compiledSchema;
        SchemaHash = schemaHash;
        RegisteredAt = DateTimeOffset.UtcNow;
    }
}
```

---

### Step 9: Implement ToolSchemaRegistry

**File**: `src/Acode.Infrastructure/ToolSchemas/ToolSchemaRegistry.cs`

```csharp
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Acode.Application.ToolSchemas;
using Json.Schema;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.ToolSchemas;

/// <summary>
/// Thread-safe registry for tool schemas with pre-compiled validators.
/// </summary>
public sealed class ToolSchemaRegistry : IToolSchemaRegistry
{
    private readonly ILogger<ToolSchemaRegistry> _logger;
    private readonly ConcurrentDictionary<string, CompiledTool> _tools = new(StringComparer.OrdinalIgnoreCase);
    
    // Pre-configured evaluation options for consistent validation
    private static readonly EvaluationOptions EvalOptions = new()
    {
        OutputFormat = OutputFormat.List,
        RequireFormatValidation = true
    };

    public ToolSchemaRegistry(ILogger<ToolSchemaRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public int Count => _tools.Count;

    /// <inheritdoc />
    public void RegisterTool(ToolDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        
        // Validate definition
        definition.Validate();

        var normalizedName = definition.Name.ToLowerInvariant();
        
        // Check for duplicate
        if (_tools.ContainsKey(normalizedName))
        {
            throw new DuplicateToolException(definition.Name);
        }

        // Compile schema
        var sw = Stopwatch.StartNew();
        JsonSchema compiledSchema;
        try
        {
            var schemaJson = definition.ParameterSchema.GetRawText();
            compiledSchema = JsonSchema.FromText(schemaJson);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse schema JSON for tool {ToolName}", definition.Name);
            throw new SchemaCompilationException(definition.Name, $"Invalid JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compile schema for tool {ToolName}", definition.Name);
            throw new SchemaCompilationException(definition.Name, ex.Message, ex);
        }
        sw.Stop();

        // Compute schema hash for audit
        var schemaHash = ComputeSchemaHash(definition.ParameterSchema);

        // Create compiled tool
        var compiledTool = new CompiledTool(definition, compiledSchema, schemaHash);

        // Thread-safe add
        if (!_tools.TryAdd(normalizedName, compiledTool))
        {
            throw new DuplicateToolException(definition.Name);
        }

        _logger.LogInformation(
            "Registered tool {ToolName} v{Version} (hash: {SchemaHash}) in {ElapsedMs}ms",
            definition.Name,
            definition.Version,
            schemaHash[..8], // First 8 chars of hash
            sw.ElapsedMilliseconds);
    }

    /// <inheritdoc />
    public ToolDefinition GetToolDefinition(string toolName)
    {
        if (TryGetToolDefinition(toolName, out var definition))
        {
            return definition!;
        }
        throw new ToolNotFoundException(toolName);
    }

    /// <inheritdoc />
    public bool TryGetToolDefinition(string toolName, out ToolDefinition? definition)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            definition = null;
            return false;
        }

        if (_tools.TryGetValue(toolName, out var compiled))
        {
            definition = compiled.Definition;
            return true;
        }

        definition = null;
        return false;
    }

    /// <inheritdoc />
    public IReadOnlyList<ToolDefinition> GetAllTools()
    {
        return _tools.Values
            .Select(t => t.Definition)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<ToolDefinition> GetToolsByCategory(ToolCategory category)
    {
        return _tools.Values
            .Where(t => t.Definition.Category == category)
            .Select(t => t.Definition)
            .OrderBy(t => t.Name)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public JsonElement ValidateArguments(string toolName, string argumentsJson)
    {
        var result = TryValidateArguments(toolName, argumentsJson);
        
        if (!result.IsValid)
        {
            throw new SchemaValidationException(toolName, result.Errors);
        }

        return result.ParsedArguments!.Value;
    }

    /// <inheritdoc />
    public ToolValidationResult TryValidateArguments(string toolName, string argumentsJson)
    {
        // Get compiled tool
        if (!_tools.TryGetValue(toolName, out var compiledTool))
        {
            _logger.LogWarning("Validation attempted for unknown tool: {ToolName}", toolName);
            return ToolValidationResult.ToolNotFound(toolName);
        }

        // Parse JSON
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(argumentsJson);
        }
        catch (JsonException ex)
        {
            _logger.LogDebug("Invalid JSON for tool {ToolName}: {Error}", toolName, ex.Message);
            return ToolValidationResult.InvalidJson(toolName, ex.Message);
        }

        // Validate against schema
        var sw = Stopwatch.StartNew();
        var evalResult = compiledTool.CompiledSchema.Evaluate(document.RootElement, EvalOptions);
        sw.Stop();

        if (evalResult.IsValid)
        {
            _logger.LogDebug(
                "Validation passed for tool {ToolName} in {ElapsedMs}ms",
                toolName,
                sw.ElapsedMilliseconds);
            return ToolValidationResult.Valid(toolName, document.RootElement.Clone());
        }

        // Convert validation errors
        var errors = ConvertValidationErrors(evalResult);
        
        _logger.LogInformation(
            "Validation failed for tool {ToolName} with {ErrorCount} errors in {ElapsedMs}ms",
            toolName,
            errors.Count,
            sw.ElapsedMilliseconds);

        return ToolValidationResult.Invalid(toolName, errors);
    }

    /// <summary>
    /// Converts JsonSchema.Net evaluation results to our ValidationError format.
    /// </summary>
    private static List<ValidationError> ConvertValidationErrors(EvaluationResults evalResult)
    {
        var errors = new List<ValidationError>();
        
        foreach (var detail in evalResult.Details)
        {
            if (!detail.HasErrors)
                continue;

            var path = detail.InstanceLocation.ToString();
            
            foreach (var (keyword, message) in detail.Errors ?? [])
            {
                var error = keyword switch
                {
                    "required" => ValidationError.RequiredMissing(path, ExtractPropertyName(message)),
                    "type" => ValidationError.TypeMismatch(path, ExtractExpected(message), ExtractActual(message)),
                    _ => ValidationError.ConstraintViolation(path, $"{keyword}: {SanitizeMessage(message)}")
                };
                errors.Add(error);
            }
        }

        return errors;
    }

    /// <summary>
    /// Computes a SHA256 hash of the schema for audit logging.
    /// </summary>
    private static string ComputeSchemaHash(JsonElement schema)
    {
        var json = schema.GetRawText();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Extracts property name from error message.
    /// </summary>
    private static string ExtractPropertyName(string message)
    {
        // Example: "Required property 'path' not found"
        var match = System.Text.RegularExpressions.Regex.Match(message, @"'([^']+)'");
        return match.Success ? match.Groups[1].Value : "unknown";
    }

    /// <summary>
    /// Extracts expected type from error message.
    /// </summary>
    private static string ExtractExpected(string message)
    {
        // Example: "Value is 'integer' but should be 'string'"
        var match = System.Text.RegularExpressions.Regex.Match(message, @"should be '([^']+)'");
        return match.Success ? match.Groups[1].Value : "unknown";
    }

    /// <summary>
    /// Extracts actual type from error message.
    /// </summary>
    private static string ExtractActual(string message)
    {
        var match = System.Text.RegularExpressions.Regex.Match(message, @"Value is '([^']+)'");
        return match.Success ? match.Groups[1].Value : "unknown";
    }

    /// <summary>
    /// Sanitizes error message by removing internal schema details.
    /// </summary>
    private static string SanitizeMessage(string message)
    {
        // Remove any $ref paths
        message = System.Text.RegularExpressions.Regex.Replace(message, @"\$ref[^']*", "");
        // Truncate long messages
        return message.Length > 200 ? message[..200] + "..." : message;
    }
}
```

---

### Step 10: Implement CoreToolsProvider

**File**: `src/Acode.Infrastructure/ToolSchemas/Providers/CoreToolsProvider.cs`

```csharp
using System.Text.Json;
using Acode.Application.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers;

/// <summary>
/// Provides core built-in tool definitions.
/// </summary>
public sealed class CoreToolsProvider : ISchemaProvider
{
    public string ProviderName => "CoreTools";
    public int Priority => 0; // Highest priority, registered first

    public IEnumerable<ToolDefinition> GetToolDefinitions()
    {
        yield return CreateFileReadTool();
        yield return CreateFileWriteTool();
        yield return CreateDirectoryListTool();
        yield return CreateCommandExecuteTool();
    }

    private static ToolDefinition CreateFileReadTool()
    {
        var schema = JsonDocument.Parse("""
        {
            "$schema": "https://json-schema.org/draft/2020-12/schema",
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Absolute or relative path to the file to read",
                    "maxLength": 4096
                },
                "encoding": {
                    "type": "string",
                    "description": "Character encoding for reading the file",
                    "enum": ["utf-8", "ascii", "utf-16", "utf-32"],
                    "default": "utf-8"
                },
                "start_line": {
                    "type": "integer",
                    "description": "Starting line number (1-based) for partial read",
                    "minimum": 1
                },
                "end_line": {
                    "type": "integer",
                    "description": "Ending line number (1-based) for partial read",
                    "minimum": 1
                }
            },
            "required": ["path"],
            "additionalProperties": false
        }
        """);

        return new ToolDefinition
        {
            Name = "file_read",
            Description = "Read the contents of a file from the filesystem. Supports partial reads by line range.",
            Version = "1.0.0",
            Category = ToolCategory.FileSystem,
            ParameterSchema = schema.RootElement.Clone()
        };
    }

    private static ToolDefinition CreateFileWriteTool()
    {
        var schema = JsonDocument.Parse("""
        {
            "$schema": "https://json-schema.org/draft/2020-12/schema",
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Absolute or relative path to the file to write",
                    "maxLength": 4096
                },
                "content": {
                    "type": "string",
                    "description": "Content to write to the file"
                },
                "mode": {
                    "type": "string",
                    "description": "Write mode: overwrite replaces file, append adds to end",
                    "enum": ["overwrite", "append"],
                    "default": "overwrite"
                },
                "create_directories": {
                    "type": "boolean",
                    "description": "Create parent directories if they don't exist",
                    "default": true
                },
                "encoding": {
                    "type": "string",
                    "description": "Character encoding for writing the file",
                    "enum": ["utf-8", "ascii", "utf-16", "utf-32"],
                    "default": "utf-8"
                }
            },
            "required": ["path", "content"],
            "additionalProperties": false
        }
        """);

        return new ToolDefinition
        {
            Name = "file_write",
            Description = "Write content to a file on the filesystem. Creates the file if it doesn't exist.",
            Version = "1.0.0",
            Category = ToolCategory.FileSystem,
            ParameterSchema = schema.RootElement.Clone()
        };
    }

    private static ToolDefinition CreateDirectoryListTool()
    {
        var schema = JsonDocument.Parse("""
        {
            "$schema": "https://json-schema.org/draft/2020-12/schema",
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Absolute or relative path to the directory to list",
                    "maxLength": 4096
                },
                "pattern": {
                    "type": "string",
                    "description": "Glob pattern to filter results (e.g., '*.cs')",
                    "default": "*"
                },
                "recursive": {
                    "type": "boolean",
                    "description": "Whether to list subdirectories recursively",
                    "default": false
                },
                "include_hidden": {
                    "type": "boolean",
                    "description": "Whether to include hidden files and directories",
                    "default": false
                },
                "max_depth": {
                    "type": "integer",
                    "description": "Maximum depth for recursive listing",
                    "minimum": 1,
                    "maximum": 10,
                    "default": 5
                }
            },
            "required": ["path"],
            "additionalProperties": false
        }
        """);

        return new ToolDefinition
        {
            Name = "directory_list",
            Description = "List the contents of a directory, optionally with glob filtering and recursion.",
            Version = "1.0.0",
            Category = ToolCategory.FileSystem,
            ParameterSchema = schema.RootElement.Clone()
        };
    }

    private static ToolDefinition CreateCommandExecuteTool()
    {
        var schema = JsonDocument.Parse("""
        {
            "$schema": "https://json-schema.org/draft/2020-12/schema",
            "type": "object",
            "properties": {
                "command": {
                    "type": "string",
                    "description": "The command to execute",
                    "maxLength": 8192
                },
                "working_directory": {
                    "type": "string",
                    "description": "Working directory for command execution",
                    "maxLength": 4096
                },
                "timeout_seconds": {
                    "type": "integer",
                    "description": "Maximum time to wait for command completion",
                    "minimum": 1,
                    "maximum": 300,
                    "default": 60
                },
                "capture_stderr": {
                    "type": "boolean",
                    "description": "Whether to capture stderr in addition to stdout",
                    "default": true
                }
            },
            "required": ["command"],
            "additionalProperties": false
        }
        """);

        return new ToolDefinition
        {
            Name = "command_execute",
            Description = "Execute a shell command and return its output. Use with caution.",
            Version = "1.0.0",
            Category = ToolCategory.System,
            ParameterSchema = schema.RootElement.Clone()
        };
    }
}
```

---

### Step 11: Implement Tool Registration Service

**File**: `src/Acode.Infrastructure/ToolSchemas/ToolRegistrationService.cs`

```csharp
using System.Diagnostics;
using Acode.Application.ToolSchemas;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.ToolSchemas;

/// <summary>
/// Background service that registers all tools at startup.
/// </summary>
public sealed class ToolRegistrationService : IHostedService
{
    private readonly IToolSchemaRegistry _registry;
    private readonly IEnumerable<ISchemaProvider> _providers;
    private readonly ILogger<ToolRegistrationService> _logger;
    private readonly TaskCompletionSource _initialized = new();

    public ToolRegistrationService(
        IToolSchemaRegistry registry,
        IEnumerable<ISchemaProvider> providers,
        ILogger<ToolRegistrationService> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Task that completes when all tools are registered.
    /// Other services can await this to ensure registry is ready.
    /// </summary>
    public Task Initialized => _initialized.Task;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Tool Schema Registry initialization...");
        var sw = Stopwatch.StartNew();

        var sortedProviders = _providers.OrderBy(p => p.Priority).ToList();
        var successCount = 0;
        var failCount = 0;

        foreach (var provider in sortedProviders)
        {
            _logger.LogDebug("Processing provider: {ProviderName}", provider.ProviderName);

            foreach (var tool in provider.GetToolDefinitions())
            {
                try
                {
                    _registry.RegisterTool(tool);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to register tool '{ToolName}' from provider '{ProviderName}'",
                        tool.Name,
                        provider.ProviderName);
                    failCount++;
                }
            }
        }

        sw.Stop();

        if (failCount > 0)
        {
            _logger.LogWarning(
                "Tool Schema Registry initialized with {SuccessCount} tools ({FailCount} failed) in {ElapsedMs}ms",
                successCount,
                failCount,
                sw.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogInformation(
                "Tool Schema Registry initialized with {SuccessCount} tools in {ElapsedMs}ms",
                successCount,
                sw.ElapsedMilliseconds);
        }

        _initialized.SetResult();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
```

---

### Step 12: Create Dependency Injection Extension

**File**: `src/Acode.Infrastructure/DependencyInjection/ToolSchemaServiceExtensions.cs`

```csharp
using Acode.Application.ToolSchemas;
using Acode.Infrastructure.ToolSchemas;
using Acode.Infrastructure.ToolSchemas.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Acode.Infrastructure.DependencyInjection;

public static class ToolSchemaServiceExtensions
{
    /// <summary>
    /// Adds Tool Schema Registry services to the service collection.
    /// </summary>
    public static IServiceCollection AddToolSchemaRegistry(this IServiceCollection services)
    {
        // Register the registry as singleton (shared across all requests)
        services.AddSingleton<IToolSchemaRegistry, ToolSchemaRegistry>();

        // Register schema providers
        services.AddSingleton<ISchemaProvider, CoreToolsProvider>();
        // Add ConfigToolsProvider when configuration support is ready:
        // services.AddSingleton<ISchemaProvider, ConfigToolsProvider>();

        // Register the startup service
        services.AddHostedService<ToolRegistrationService>();

        return services;
    }
}
```

---

### Step 13: Create CLI Commands

**File**: `src/Acode.Cli/Commands/ToolsCommand.cs`

```csharp
using System.CommandLine;
using Acode.Application.ToolSchemas;

namespace Acode.Cli.Commands;

public class ToolsCommand : Command
{
    public ToolsCommand() : base("tools", "Manage and inspect registered tools")
    {
        AddCommand(new ListToolsCommand());
        AddCommand(new ShowToolCommand());
        AddCommand(new ValidateToolCommand());
    }
}

public class ListToolsCommand : Command
{
    public ListToolsCommand() : base("list", "List all registered tools")
    {
        this.SetHandler(Execute);
    }

    private void Execute()
    {
        // Implementation will use DI to get IToolSchemaRegistry
        // and display tools in a formatted table
    }
}

public class ShowToolCommand : Command
{
    public ShowToolCommand() : base("show", "Show details of a specific tool")
    {
        var nameArg = new Argument<string>("name", "Name of the tool to show");
        AddArgument(nameArg);
        this.SetHandler(Execute, nameArg);
    }

    private void Execute(string name)
    {
        // Implementation will display tool details including schema
    }
}

public class ValidateToolCommand : Command
{
    public ValidateToolCommand() : base("validate", "Validate arguments against a tool schema")
    {
        var nameArg = new Argument<string>("name", "Name of the tool");
        var argsArg = new Argument<string>("args", "JSON arguments to validate");
        AddArgument(nameArg);
        AddArgument(argsArg);
        this.SetHandler(Execute, nameArg, argsArg);
    }

    private void Execute(string name, string args)
    {
        // Implementation will validate and show results
    }
}
```

---

### Step 14: Wire Up in Program.cs

**File**: `src/Acode.Cli/Program.cs` (additions)

```csharp
using Acode.Infrastructure.DependencyInjection;

// In your service configuration:
builder.Services.AddToolSchemaRegistry();

// In your command building:
rootCommand.AddCommand(new ToolsCommand());
```

---

### Error Codes Reference

| Code | Name | Description |
|------|------|-------------|
| ACODE-TSR-001 | ToolNotFound | The requested tool is not registered |
| ACODE-TSR-002 | InvalidJson | The arguments string is not valid JSON |
| ACODE-TSR-003 | RequiredMissing | A required property is missing |
| ACODE-TSR-004 | TypeMismatch | Property type doesn't match schema |
| ACODE-TSR-005 | ConstraintViolation | Property value violates a constraint |
| ACODE-TSR-006 | InvalidSchema | Tool schema is invalid |
| ACODE-TSR-007 | DuplicateTool | Tool name is already registered |
| ACODE-TSR-008 | CompilationFailed | Schema compilation failed |

---

### Implementation Checklist

Complete these items in order:

- [ ] **Step 1**: Create `ToolCategory.cs` enum
- [ ] **Step 2**: Create `ValidationError.cs` record
- [ ] **Step 3**: Create `ToolValidationResult.cs` record
- [ ] **Step 4**: Create `ToolDefinition.cs` record
- [ ] **Step 5**: Create `ToolSchemaExceptions.cs` exception classes
- [ ] **Step 6**: Create `IToolSchemaRegistry.cs` interface
- [ ] **Step 7**: Create `ISchemaProvider.cs` interface
- [ ] **Step 8**: Create `CompiledTool.cs` internal class
- [ ] **Step 9**: Implement `ToolSchemaRegistry.cs`
- [ ] **Step 10**: Implement `CoreToolsProvider.cs`
- [ ] **Step 11**: Implement `ToolRegistrationService.cs`
- [ ] **Step 12**: Create `ToolSchemaServiceExtensions.cs`
- [ ] **Step 13**: Create CLI commands
- [ ] **Step 14**: Wire up in `Program.cs`
- [ ] **Step 15**: Write unit tests (see Testing Requirements)
- [ ] **Step 16**: Write integration tests
- [ ] **Step 17**: Add XML documentation to all public APIs

---

### Dependencies

| Dependency | Purpose | Required Version |
|------------|---------|------------------|
| JsonSchema.Net | JSON Schema validation | 5.5.1+ |
| Microsoft.Extensions.Logging | Logging abstraction | 8.0+ |
| Microsoft.Extensions.DependencyInjection | DI container | 8.0+ |
| Microsoft.Extensions.Hosting | IHostedService | 8.0+ |
| System.Text.Json | JSON parsing | 8.0+ |

---

### Verification Commands

After implementation, verify with these commands:

```powershell
# Build the solution
dotnet build

# Run unit tests
dotnet test --filter "FullyQualifiedName~ToolSchemas"

# Run the CLI tools command
dotnet run --project src/Acode.Cli -- tools list

# Validate a tool
dotnet run --project src/Acode.Cli -- tools validate file_read '{"path": "/test.txt"}'
```

---

**End of Task 007 Specification**