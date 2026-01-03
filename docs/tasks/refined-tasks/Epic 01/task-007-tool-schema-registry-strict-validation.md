# Task 007: Tool Schema Registry + Strict Validation

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 004.a (ToolDefinition types), Task 005.b (Tool Call Parsing), Task 006.b (Structured Outputs), Task 001, Task 002  

---

## Description

Task 007 implements the Tool Schema Registry and strict validation system for Acode. The registry serves as the central repository for all tool definitions and their JSON Schema specifications, enabling consistent tool discovery, parameter validation, and integration with model providers. Strict validation ensures tool arguments from models conform exactly to expected schemas, preventing runtime errors and security vulnerabilities.

Tool calling is the primary mechanism by which LLMs interact with external systems. When a model decides to call a tool, it generates JSON arguments matching the tool's parameter schema. If those arguments are invalid—wrong types, missing required fields, malformed JSON—the tool execution fails. The Tool Schema Registry prevents these failures by providing authoritative schema definitions and enforcing validation before execution.

The registry follows a provider pattern where tool implementations register their schemas during application startup. Each tool registers its name, description, and parameter schema. The registry validates schemas are well-formed during registration, catching definition errors early. At runtime, the registry provides schemas to model providers for prompt construction and validates tool call arguments returned by models.

JSON Schema is the standard for defining tool parameter structures. Acode uses JSON Schema Draft 2020-12 for maximum compatibility with model providers. The schema defines required and optional fields, field types, constraints (minimum, maximum, pattern, enum), and nested structures. The registry validates schemas conform to the JSON Schema specification during registration.

Strict validation rejects any tool call arguments that don't match the schema. This is a security boundary—tools may have file system access, execute code, or modify system state. Accepting malformed or unexpected arguments could enable injection attacks or cause undefined behavior. Strict mode rejects anything that isn't explicitly allowed; there is no lenient mode.

Validation error handling follows a contract that enables model retry. When arguments fail validation, the error message describes what's wrong in a format the model can understand. The error is returned to the model in a tool_result message, giving the model a chance to correct its arguments. This retry contract is defined in Task 007.b.

The registry integrates with both Ollama (Task 005) and vLLM (Task 006) providers. Ollama receives tool definitions during prompt construction but doesn't enforce schemas during generation—post-hoc validation catches errors. vLLM can use structured output enforcement (Task 006.b) to prevent invalid arguments during generation. The registry supports both patterns through a consistent interface.

Schema versioning enables evolution without breaking compatibility. Each schema has a version number; when schemas change, old versions remain available for backwards compatibility. The registry tracks schema versions and validates against the appropriate version. Schema migration utilities help upgrade stored data when schemas evolve.

Performance is critical since validation occurs on every tool call. The registry caches compiled schemas for fast validation. Schema compilation happens once at registration time; validation uses compiled schemas with minimal overhead. Benchmarks ensure validation adds <1ms to tool execution latency.

Observability includes logging when tools are registered, when validation occurs, and when validation fails. Metrics track validation success/failure rates, validation latency, and schema usage. These insights help identify problematic tools or models that frequently produce invalid arguments.

The registry is extensible for custom tools. Users can define tools in configuration or code, registering schemas following documented patterns. Custom tools integrate seamlessly with built-in tools, using the same registry and validation infrastructure.

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

## Testing Requirements

### Unit Tests

```
Tests/Unit/Application/ToolSchemas/
├── ToolSchemaRegistryTests.cs
│   ├── Should_Register_Tool()
│   ├── Should_Reject_Duplicate()
│   ├── Should_Reject_Invalid_Schema()
│   ├── Should_Get_Tool_Definition()
│   ├── Should_Get_All_Tools()
│   └── Should_Validate_Arguments()
│
├── SchemaValidationTests.cs
│   ├── Should_Validate_Required_Fields()
│   ├── Should_Validate_Types()
│   ├── Should_Validate_String_Constraints()
│   ├── Should_Validate_Number_Constraints()
│   ├── Should_Validate_Enum()
│   ├── Should_Validate_Nested_Objects()
│   └── Should_Validate_Arrays()
│
├── ValidationErrorTests.cs
│   ├── Should_Include_Path()
│   ├── Should_Include_Code()
│   ├── Should_Include_Message()
│   └── Should_Sanitize_Values()
│
└── SchemaProviderTests.cs
    ├── Should_Discover_Providers()
    ├── Should_Run_At_Startup()
    └── Should_Register_Core_Tools()
```

### Integration Tests

```
Tests/Integration/ToolSchemas/
├── RegistryIntegrationTests.cs
│   ├── Should_Register_All_Core_Tools()
│   ├── Should_Load_Config_Tools()
│   └── Should_Validate_Real_Tool_Calls()
```

### Performance Tests

```
Tests/Performance/ToolSchemas/
├── RegistryBenchmarks.cs
│   ├── Benchmark_Schema_Compilation()
│   ├── Benchmark_Argument_Validation()
│   └── Benchmark_Large_Registry()
```

---

## User Verification Steps

### Scenario 1: Tool Registration

1. Start application
2. Check logs for registration
3. Verify: All core tools registered
4. Verify: Versions logged

### Scenario 2: List Tools

1. Run `acode tools list`
2. Verify: All tools shown
3. Verify: Names, versions, descriptions

### Scenario 3: Valid Arguments

1. Call tool with valid args
2. Verify: Validation passes
3. Verify: Tool executes

### Scenario 4: Missing Required Field

1. Call tool without required field
2. Verify: Validation fails
3. Verify: Error mentions field
4. Verify: Error code ACODE-TSR-003

### Scenario 5: Wrong Type

1. Call tool with wrong type
2. Verify: Validation fails
3. Verify: Error shows expected type
4. Verify: Error code ACODE-TSR-004

### Scenario 6: Config-Based Tool

1. Define tool in config.yml
2. Restart application
3. Verify: Tool registered
4. Verify: Can be called

### Scenario 7: Invalid Schema

1. Try to register invalid schema
2. Verify: Registration fails
3. Verify: Clear error message

### Scenario 8: Unknown Tool

1. Validate args for nonexistent tool
2. Verify: Error code ACODE-TSR-001

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/ToolSchemas/
├── IToolSchemaRegistry.cs
├── ISchemaProvider.cs
├── ToolDefinition.cs
├── ValidationError.cs
├── SchemaValidationException.cs
└── ToolCategory.cs

src/AgenticCoder.Infrastructure/ToolSchemas/
├── ToolSchemaRegistry.cs
├── SchemaCompiler.cs
├── SchemaValidator.cs
├── Providers/
│   ├── CoreToolsProvider.cs
│   └── ConfigToolsProvider.cs
└── Configuration/
    └── ToolSchemaConfiguration.cs
```

### IToolSchemaRegistry Interface

```csharp
namespace AgenticCoder.Application.ToolSchemas;

public interface IToolSchemaRegistry
{
    void RegisterTool(ToolDefinition definition);
    
    ToolDefinition GetToolDefinition(string toolName);
    
    IReadOnlyList<ToolDefinition> GetAllTools();
    
    IReadOnlyList<ToolDefinition> GetToolsByCategory(ToolCategory category);
    
    JsonElement ValidateArguments(string toolName, string argumentsJson);
    
    bool TryValidateArguments(
        string toolName,
        string argumentsJson,
        out IReadOnlyList<ValidationError> errors,
        out JsonElement parsedArguments);
}
```

### ToolDefinition Class

```csharp
namespace AgenticCoder.Application.ToolSchemas;

public sealed class ToolDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Version { get; init; }
    public required JsonElement Parameters { get; init; }
    public ToolCategory Category { get; init; } = ToolCategory.General;
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-TSR-001 | Unknown tool: '{name}' |
| ACODE-TSR-002 | Invalid JSON in arguments |
| ACODE-TSR-003 | Required field '{field}' missing |
| ACODE-TSR-004 | Type mismatch: expected {expected}, got {actual} |
| ACODE-TSR-005 | Constraint violation: {details} |
| ACODE-TSR-006 | Schema is invalid: {reason} |
| ACODE-TSR-007 | Tool '{name}' already registered |
| ACODE-TSR-008 | Schema compilation failed: {reason} |

### Implementation Checklist

1. [ ] Create IToolSchemaRegistry interface
2. [ ] Create ISchemaProvider interface
3. [ ] Create ToolDefinition class
4. [ ] Create ValidationError class
5. [ ] Create SchemaValidationException
6. [ ] Create ToolCategory enum
7. [ ] Implement SchemaCompiler
8. [ ] Implement SchemaValidator
9. [ ] Implement ToolSchemaRegistry
10. [ ] Implement CoreToolsProvider
11. [ ] Implement ConfigToolsProvider
12. [ ] Wire up DI registration
13. [ ] Add CLI commands
14. [ ] Write unit tests
15. [ ] Write integration tests
16. [ ] Add XML documentation

### Dependencies

- Task 004.a (ToolDefinition types)
- Task 005.b (Tool call parsing)
- Task 006.b (Structured outputs)
- System.Text.Json
- Json.Schema.Net or similar

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~ToolSchemas"
```

---

**End of Task 007 Specification**