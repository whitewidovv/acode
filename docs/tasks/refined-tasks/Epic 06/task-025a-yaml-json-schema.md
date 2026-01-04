# Task 025.a: YAML/JSON Schema

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 025 (Task Spec Format)  

---

## Description

Task 025.a defines the JSON Schema for task specs. The schema MUST be the single source of truth for validation. YAML and JSON MUST both validate against this schema.

The schema MUST define all field types, constraints, and defaults. The schema MUST support draft-07 or later. The schema MUST be exportable for external tool integration.

Schema validation MUST produce detailed errors. Each error MUST identify the field path and constraint violated. Errors MUST suggest corrections where possible.

### Business Value

A formal schema enables:
- Automated validation in any language
- IDE autocomplete and validation
- Documentation generation
- API contract enforcement
- External tool integration

### Scope Boundaries

This task covers schema definition. Parsing is in Task 025. CLI is in Task 025.b. Error formatting is in Task 025.c.

### Integration Points

- Task 025: Uses schema for validation
- Task 025.b: CLI exposes schema commands
- External: Schema exported for IDE plugins

### Failure Modes

- Schema load failure → System cannot start
- Schema version mismatch → Migration required
- Invalid schema → Build-time failure

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| JSON Schema | IETF standard for JSON structure |
| Draft-07 | JSON Schema specification version |
| $ref | Schema reference for reuse |
| oneOf | Schema union type |
| allOf | Schema intersection type |
| pattern | Regex for string validation |
| format | Semantic string format |
| additionalProperties | Unknown property handling |
| required | Mandatory field list |
| default | Value when absent |

---

## Out of Scope

- Custom schema language
- GraphQL schema
- Protobuf definition
- XML Schema (XSD)
- Dynamic schema generation
- Schema UI builder

---

## Functional Requirements

### FR-001 to FR-030: Schema Structure

- FR-001: Schema MUST use JSON Schema draft-07+
- FR-002: Schema MUST define `$schema` field
- FR-003: Schema MUST define `$id` field
- FR-004: Schema MUST define `title` field
- FR-005: Schema MUST define `description` field
- FR-006: Schema MUST define `type` as "object"
- FR-007: Schema MUST define `properties` object
- FR-008: Schema MUST define `required` array
- FR-009: Schema MUST define `additionalProperties`
- FR-010: Each property MUST have `type`
- FR-011: Each property MUST have `description`
- FR-012: String properties MUST have `maxLength`
- FR-013: String properties MAY have `minLength`
- FR-014: String properties MAY have `pattern`
- FR-015: String properties MAY have `format`
- FR-016: Number properties MUST have `minimum`
- FR-017: Number properties MUST have `maximum`
- FR-018: Array properties MUST have `items`
- FR-019: Array properties MUST have `maxItems`
- FR-020: Array properties MAY have `minItems`
- FR-021: Array properties MAY have `uniqueItems`
- FR-022: Object properties MUST have `properties`
- FR-023: Enum properties MUST have `enum` array
- FR-024: Properties MAY have `default` value
- FR-025: Properties MAY have `examples` array
- FR-026: Schema MUST define `definitions` for reuse
- FR-027: Schema MUST use `$ref` for shared types
- FR-028: ULID format MUST be custom-defined
- FR-029: ISO8601 format MUST use "date-time"
- FR-030: File path format MUST be custom-defined

### FR-031 to FR-050: Field Definitions

- FR-031: `id` MUST be ULID format
- FR-032: `id` MUST be optional (auto-generated)
- FR-033: `title` MUST be string 1-200 chars
- FR-034: `title` MUST be required
- FR-035: `description` MUST be string 1-10000 chars
- FR-036: `description` MUST be required
- FR-037: `status` MUST be enum
- FR-038: `status` MUST default to "pending"
- FR-039: `priority` MUST be integer 1-5
- FR-040: `priority` MUST default to 3
- FR-041: `dependencies` MUST be array of ULID
- FR-042: `dependencies` MUST default to empty
- FR-043: `files` MUST be array of file-path
- FR-044: `files` MUST default to empty
- FR-045: `tags` MUST be array of tag-pattern
- FR-046: `tags` MUST default to empty
- FR-047: `metadata` MUST be object
- FR-048: `metadata` MUST default to empty
- FR-049: `timeout` MUST be positive integer
- FR-050: `retryLimit` MUST be 0-10

### FR-051 to FR-065: Validation Behavior

- FR-051: Validation MUST use schema directly
- FR-052: Validation MUST collect all errors
- FR-053: Validation MUST return structured result
- FR-054: Each error MUST have path
- FR-055: Each error MUST have message
- FR-056: Each error MUST have code
- FR-057: Each error MUST have schemaPath
- FR-058: Custom formats MUST have validators
- FR-059: ULID validator MUST check format
- FR-060: File-path validator MUST check traversal
- FR-061: Coercion MUST be disabled
- FR-062: Additional properties MUST be preserved
- FR-063: Unknown properties MUST emit warning
- FR-064: Strict mode MUST reject unknowns
- FR-065: Schema MUST be cached after load

---

## Non-Functional Requirements

- NFR-001: Schema load MUST complete in <100ms
- NFR-002: Validation MUST complete in <50ms
- NFR-003: Schema MUST be <100KB
- NFR-004: Schema MUST be embeddable
- NFR-005: Schema MUST be exportable
- NFR-006: Schema MUST be human-readable
- NFR-007: Schema MUST be versioned
- NFR-008: Schema version MUST use semver
- NFR-009: Breaking changes MUST bump major
- NFR-010: Additions MUST bump minor

---

## User Manual Documentation

### Schema File Location

The schema is located at:
```
src/Domain/TaskSpecs/Schemas/task-spec.schema.json
```

### Export Schema

```bash
# Export to file
acode schema export task-spec > task-spec.schema.json

# Export with version
acode schema export task-spec --version 1.0.0
```

### Validate Against Schema

```bash
# Validate file
acode task validate task.yaml --schema

# Show schema errors
acode task validate task.yaml --verbose
```

### Schema in IDE

VS Code users can add to settings:

```json
{
  "yaml.schemas": {
    "./task-spec.schema.json": "*.task.yaml"
  }
}
```

### Custom Formats

| Format | Pattern | Example |
|--------|---------|---------|
| ulid | `[0-9A-HJKMNP-TV-Z]{26}` | `01ARZ3NDEKTSV4RRFFQ69G5FAV` |
| file-path | Relative, no traversal | `src/Handler.cs` |
| tag | `[a-z0-9-]+` | `feature-auth` |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Schema file exists
- [ ] AC-002: Schema is valid JSON Schema
- [ ] AC-003: All fields defined
- [ ] AC-004: Required fields marked
- [ ] AC-005: Defaults specified
- [ ] AC-006: Constraints documented
- [ ] AC-007: Custom formats work
- [ ] AC-008: ULID validation works
- [ ] AC-009: File-path validation works
- [ ] AC-010: Tag validation works
- [ ] AC-011: Export command works
- [ ] AC-012: IDE integration works
- [ ] AC-013: Errors include path
- [ ] AC-014: Errors include code
- [ ] AC-015: Schema versioned

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Valid schema loads
- [ ] UT-002: All properties defined
- [ ] UT-003: ULID format validates
- [ ] UT-004: File-path format validates
- [ ] UT-005: Required fields enforced
- [ ] UT-006: Defaults applied

### Integration Tests

- [ ] IT-001: Full validation flow
- [ ] IT-002: Schema export
- [ ] IT-003: IDE integration

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Core/
│   └── Domain/
│       └── Tasks/
│           └── Schemas/
│               └── task-spec.schema.json  # JSON Schema file
│
├── Acode.Application/
│   └── Services/
│       └── TaskSpec/
│           ├── ISchemaValidator.cs       # Validation interface
│           ├── SchemaValidator.cs        # JSON Schema validator
│           ├── ISchemaProvider.cs        # Schema loading
│           ├── SchemaProvider.cs         # Embedded/file schema
│           ├── IFormatValidator.cs       # Custom format interface
│           └── Formats/
│               ├── UlidFormatValidator.cs
│               ├── FilePathFormatValidator.cs
│               └── TagFormatValidator.cs
│
└── Acode.Cli/
    └── Commands/
        └── Schema/
            └── SchemaExportCommand.cs

tests/
├── Acode.Application.Tests/
│   └── Services/
│       └── TaskSpec/
│           ├── SchemaValidatorTests.cs
│           └── FormatValidatorTests.cs
```

### Complete JSON Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://acode.dev/schemas/task-spec/v1",
  "title": "Acode Task Specification",
  "description": "Schema for defining tasks in the Acode task queue system",
  "type": "object",
  "required": ["title", "description"],
  "properties": {
    "id": {
      "$ref": "#/definitions/ulid",
      "description": "Unique task identifier (ULID). Auto-generated if omitted."
    },
    "title": {
      "type": "string",
      "minLength": 1,
      "maxLength": 200,
      "description": "Short, descriptive task title",
      "examples": ["Implement user login", "Fix validation bug #123"]
    },
    "description": {
      "type": "string",
      "minLength": 1,
      "maxLength": 10000,
      "description": "Full task description with implementation details",
      "examples": ["Add login functionality with email/password validation"]
    },
    "status": {
      "$ref": "#/definitions/taskStatus",
      "default": "pending",
      "description": "Current task status (defaults to pending)"
    },
    "priority": {
      "type": "integer",
      "minimum": 1,
      "maximum": 5,
      "default": 3,
      "description": "Priority level: 1 (highest) to 5 (lowest)"
    },
    "dependencies": {
      "type": "array",
      "items": { "$ref": "#/definitions/ulid" },
      "default": [],
      "uniqueItems": true,
      "maxItems": 100,
      "description": "Task IDs that must complete before this task"
    },
    "files": {
      "type": "array",
      "items": { "$ref": "#/definitions/filePath" },
      "default": [],
      "maxItems": 1000,
      "description": "Files affected by this task (relative paths)"
    },
    "tags": {
      "type": "array",
      "items": { "$ref": "#/definitions/tag" },
      "default": [],
      "maxItems": 50,
      "uniqueItems": true,
      "description": "Categorization tags"
    },
    "metadata": {
      "type": "object",
      "default": {},
      "additionalProperties": true,
      "propertyNames": {
        "pattern": "^[a-zA-Z][a-zA-Z0-9_]*$",
        "not": { "pattern": "^_" }
      },
      "description": "Extension metadata (keys must not start with underscore)"
    },
    "timeout": {
      "type": "integer",
      "minimum": 1,
      "maximum": 86400,
      "default": 3600,
      "description": "Maximum execution time in seconds"
    },
    "retryLimit": {
      "type": "integer",
      "minimum": 0,
      "maximum": 10,
      "default": 3,
      "description": "Maximum retry attempts on failure"
    },
    "parentId": {
      "$ref": "#/definitions/ulid",
      "description": "Parent task ID for subtasks"
    },
    "estimatedDuration": {
      "type": "integer",
      "minimum": 1,
      "description": "Estimated execution time in seconds"
    },
    "labels": {
      "type": "object",
      "additionalProperties": { "type": "string" },
      "propertyNames": { "$ref": "#/definitions/labelKey" },
      "default": {},
      "description": "Key-value labels for filtering"
    },
    "createdAt": {
      "type": "string",
      "format": "date-time",
      "description": "Creation timestamp (ISO 8601)"
    }
  },
  "additionalProperties": true,
  "definitions": {
    "ulid": {
      "type": "string",
      "pattern": "^[0-9A-HJKMNP-TV-Z]{26}$",
      "description": "Universally Unique Lexicographically Sortable Identifier"
    },
    "taskStatus": {
      "type": "string",
      "enum": ["pending", "running", "completed", "failed", "cancelled", "blocked"],
      "description": "Valid task status values"
    },
    "filePath": {
      "type": "string",
      "minLength": 1,
      "maxLength": 500,
      "pattern": "^(?!.*\\.\\.)(?!/)(?!.*//)[a-zA-Z0-9_./-]+$",
      "description": "Relative file path (no traversal, no absolute paths)"
    },
    "tag": {
      "type": "string",
      "minLength": 1,
      "maxLength": 50,
      "pattern": "^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$",
      "description": "Lowercase alphanumeric tag with optional hyphens"
    },
    "labelKey": {
      "type": "string",
      "minLength": 1,
      "maxLength": 63,
      "pattern": "^[a-z][a-z0-9-]*[a-z0-9]$|^[a-z]$",
      "description": "Label key format"
    }
  }
}
```

### Schema Provider

```csharp
// Acode.Application/Services/TaskSpec/ISchemaProvider.cs
namespace Acode.Application.Services.TaskSpec;

/// <summary>
/// Provides access to the task spec JSON schema.
/// </summary>
public interface ISchemaProvider
{
    /// <summary>
    /// Gets the JSON schema as a string.
    /// </summary>
    Task<string> GetSchemaAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets the parsed JSON schema.
    /// </summary>
    Task<JsonSchema> GetParsedSchemaAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Schema version.
    /// </summary>
    string Version { get; }
}

// Acode.Application/Services/TaskSpec/SchemaProvider.cs
namespace Acode.Application.Services.TaskSpec;

public sealed class SchemaProvider : ISchemaProvider
{
    private const string SchemaResourceName = "Acode.Core.Domain.Tasks.Schemas.task-spec.schema.json";
    private JsonSchema? _cachedSchema;
    private string? _cachedSchemaText;
    private readonly SemaphoreSlim _lock = new(1, 1);
    
    public string Version => "1.0.0";
    
    public async Task<string> GetSchemaAsync(CancellationToken ct = default)
    {
        if (_cachedSchemaText != null)
            return _cachedSchemaText;
        
        await _lock.WaitAsync(ct);
        try
        {
            if (_cachedSchemaText != null)
                return _cachedSchemaText;
            
            var assembly = typeof(TaskSpec).Assembly;
            using var stream = assembly.GetManifestResourceStream(SchemaResourceName);
            
            if (stream == null)
                throw new InvalidOperationException($"Schema resource not found: {SchemaResourceName}");
            
            using var reader = new StreamReader(stream);
            _cachedSchemaText = await reader.ReadToEndAsync(ct);
            
            return _cachedSchemaText;
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task<JsonSchema> GetParsedSchemaAsync(CancellationToken ct = default)
    {
        if (_cachedSchema != null)
            return _cachedSchema;
        
        var schemaText = await GetSchemaAsync(ct);
        _cachedSchema = JsonSchema.FromText(schemaText);
        
        return _cachedSchema;
    }
}
```

### Schema Validator

```csharp
// Acode.Application/Services/TaskSpec/ISchemaValidator.cs
namespace Acode.Application.Services.TaskSpec;

/// <summary>
/// Validates JSON/YAML content against the task spec schema.
/// </summary>
public interface ISchemaValidator
{
    /// <summary>
    /// Validates a JSON node against the schema.
    /// </summary>
    Task<SchemaValidationResult> ValidateAsync(
        JsonNode content, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Validates parsed content against the schema.
    /// </summary>
    Task<SchemaValidationResult> ValidateAsync(
        string jsonContent, 
        CancellationToken ct = default);
}

/// <summary>
/// Result of schema validation.
/// </summary>
public sealed record SchemaValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<SchemaError> Errors { get; init; } = Array.Empty<SchemaError>();
    public IReadOnlyList<SchemaWarning> Warnings { get; init; } = Array.Empty<SchemaWarning>();
    
    public static SchemaValidationResult Valid() => new();
    
    public static SchemaValidationResult Invalid(IEnumerable<SchemaError> errors) => new()
    {
        Errors = errors.ToList()
    };
}

/// <summary>
/// A schema validation error.
/// </summary>
public sealed record SchemaError
{
    /// <summary>JSON Pointer path to the error.</summary>
    public required string Path { get; init; }
    
    /// <summary>Human-readable error message.</summary>
    public required string Message { get; init; }
    
    /// <summary>Error code.</summary>
    public required string Code { get; init; }
    
    /// <summary>Schema path that failed.</summary>
    public string? SchemaPath { get; init; }
    
    /// <summary>Expected value/type.</summary>
    public string? Expected { get; init; }
    
    /// <summary>Actual value (truncated).</summary>
    public string? Actual { get; init; }
}

public sealed record SchemaWarning
{
    public required string Path { get; init; }
    public required string Message { get; init; }
}

// Acode.Application/Services/TaskSpec/SchemaValidator.cs
namespace Acode.Application.Services.TaskSpec;

public sealed class SchemaValidator : ISchemaValidator
{
    private readonly ISchemaProvider _schemaProvider;
    private readonly IEnumerable<IFormatValidator> _formatValidators;
    private readonly ILogger<SchemaValidator> _logger;
    
    public SchemaValidator(
        ISchemaProvider schemaProvider,
        IEnumerable<IFormatValidator> formatValidators,
        ILogger<SchemaValidator> logger)
    {
        _schemaProvider = schemaProvider;
        _formatValidators = formatValidators;
        _logger = logger;
    }
    
    public async Task<SchemaValidationResult> ValidateAsync(
        JsonNode content, 
        CancellationToken ct = default)
    {
        var schema = await _schemaProvider.GetParsedSchemaAsync(ct);
        
        // Configure evaluation options
        var options = new EvaluationOptions
        {
            OutputFormat = OutputFormat.List,
            RequireFormatValidation = true
        };
        
        // Register custom format validators
        foreach (var validator in _formatValidators)
        {
            options.OnlyKnownFormats = false;
            // Register validator here based on library used
        }
        
        var result = schema.Evaluate(content, options);
        
        if (result.IsValid)
        {
            var warnings = CheckUnknownProperties(content, schema);
            return SchemaValidationResult.Valid() with { Warnings = warnings };
        }
        
        var errors = result.Details
            .Where(d => !d.IsValid && d.Errors != null)
            .SelectMany(d => d.Errors!.Select(e => new SchemaError
            {
                Path = d.InstanceLocation.ToString(),
                Message = e.Value,
                Code = MapErrorCode(e.Key),
                SchemaPath = d.SchemaLocation.ToString(),
                Expected = GetExpected(d),
                Actual = GetActual(d, content)
            }))
            .ToList();
        
        return SchemaValidationResult.Invalid(errors);
    }
    
    public async Task<SchemaValidationResult> ValidateAsync(
        string jsonContent, 
        CancellationToken ct = default)
    {
        var node = JsonNode.Parse(jsonContent);
        if (node == null)
        {
            return SchemaValidationResult.Invalid(new[]
            {
                new SchemaError
                {
                    Path = "",
                    Message = "Invalid JSON",
                    Code = "TASK-001"
                }
            });
        }
        
        return await ValidateAsync(node, ct);
    }
    
    private static string MapErrorCode(string schemaKeyword) => schemaKeyword switch
    {
        "required" => "TASK-002",
        "type" => "TASK-003",
        "minimum" or "maximum" or "minLength" or "maxLength" => "TASK-004",
        "pattern" or "format" => "TASK-005",
        "enum" => "TASK-003",
        _ => "TASK-001"
    };
    
    private static string? GetExpected(EvaluationResults detail)
    {
        // Extract expected value from schema constraint
        return null; // Implementation depends on library
    }
    
    private static string? GetActual(EvaluationResults detail, JsonNode root)
    {
        // Navigate to actual value and truncate
        return null; // Implementation depends on library
    }
    
    private IReadOnlyList<SchemaWarning> CheckUnknownProperties(JsonNode content, JsonSchema schema)
    {
        var warnings = new List<SchemaWarning>();
        
        if (content is JsonObject obj)
        {
            var knownProps = new HashSet<string>
            {
                "id", "title", "description", "status", "priority",
                "dependencies", "files", "tags", "metadata", "timeout",
                "retryLimit", "parentId", "estimatedDuration", "labels", "createdAt"
            };
            
            foreach (var prop in obj)
            {
                if (!knownProps.Contains(prop.Key))
                {
                    warnings.Add(new SchemaWarning
                    {
                        Path = $"/{prop.Key}",
                        Message = $"Unknown property '{prop.Key}' will be stored in metadata"
                    });
                }
            }
        }
        
        return warnings;
    }
}
```

### Custom Format Validators

```csharp
// Acode.Application/Services/TaskSpec/IFormatValidator.cs
namespace Acode.Application.Services.TaskSpec;

/// <summary>
/// Validates a custom string format.
/// </summary>
public interface IFormatValidator
{
    /// <summary>Format name in schema.</summary>
    string FormatName { get; }
    
    /// <summary>Validates the value.</summary>
    bool Validate(string value);
    
    /// <summary>Error message on failure.</summary>
    string ErrorMessage { get; }
}

// Acode.Application/Services/TaskSpec/Formats/UlidFormatValidator.cs
namespace Acode.Application.Services.TaskSpec.Formats;

public sealed class UlidFormatValidator : IFormatValidator
{
    private static readonly Regex UlidPattern = new(
        @"^[0-9A-HJKMNP-TV-Z]{26}$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));
    
    public string FormatName => "ulid";
    
    public string ErrorMessage => "Must be a valid 26-character ULID";
    
    public bool Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 26)
            return false;
        
        try
        {
            return UlidPattern.IsMatch(value);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}

// Acode.Application/Services/TaskSpec/Formats/FilePathFormatValidator.cs
namespace Acode.Application.Services.TaskSpec.Formats;

public sealed class FilePathFormatValidator : IFormatValidator
{
    public string FormatName => "file-path";
    
    public string ErrorMessage => "Must be a valid relative file path (no traversal, no absolute paths)";
    
    public bool Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        // Check for path traversal
        if (value.Contains(".."))
            return false;
        
        // Check for absolute path
        if (Path.IsPathRooted(value))
            return false;
        
        // Check for double slashes
        if (value.Contains("//"))
            return false;
        
        // Check for leading slash
        if (value.StartsWith("/") || value.StartsWith("\\"))
            return false;
        
        // Check for invalid characters (platform-agnostic)
        var invalidChars = new[] { '<', '>', ':', '"', '|', '?', '*', '\0' };
        if (value.Any(c => invalidChars.Contains(c)))
            return false;
        
        return true;
    }
}

// Acode.Application/Services/TaskSpec/Formats/TagFormatValidator.cs
namespace Acode.Application.Services.TaskSpec.Formats;

public sealed class TagFormatValidator : IFormatValidator
{
    private static readonly Regex TagPattern = new(
        @"^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));
    
    public string FormatName => "tag";
    
    public string ErrorMessage => "Must be lowercase alphanumeric with optional hyphens (not at start/end)";
    
    public bool Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 50)
            return false;
        
        try
        {
            return TagPattern.IsMatch(value);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
```

### CLI Export Command

```csharp
// Acode.Cli/Commands/Schema/SchemaExportCommand.cs
namespace Acode.Cli.Commands.Schema;

[Command("schema export", Description = "Export JSON schema")]
public sealed class SchemaExportCommand : ICommand
{
    [CommandArgument(0, "<name>", Description = "Schema name (task-spec)")]
    public string SchemaName { get; init; } = string.Empty;
    
    [CommandOption("--output|-o", Description = "Output file path")]
    public string? OutputPath { get; init; }
    
    [CommandOption("--pretty", Description = "Pretty-print JSON")]
    public bool Pretty { get; init; } = true;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var schemaProvider = GetSchemaProvider(); // DI
        
        if (!SchemaName.Equals("task-spec", StringComparison.OrdinalIgnoreCase))
        {
            console.Error.WriteLine($"Unknown schema: {SchemaName}");
            console.Error.WriteLine("Available: task-spec");
            Environment.ExitCode = ExitCodes.InvalidArgument;
            return;
        }
        
        var schema = await schemaProvider.GetSchemaAsync();
        
        if (Pretty)
        {
            var node = JsonNode.Parse(schema);
            schema = node!.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }
        
        if (OutputPath != null)
        {
            await File.WriteAllTextAsync(OutputPath, schema);
            console.Output.WriteLine($"Schema exported to: {OutputPath}");
        }
        else
        {
            console.Output.WriteLine(schema);
        }
    }
}
```

### Implementation Checklist

- [ ] Create `task-spec.schema.json` file
- [ ] Embed schema as resource in Core assembly
- [ ] Define all property schemas with constraints
- [ ] Define `definitions` for reusable types
- [ ] Add ULID pattern definition
- [ ] Add file-path pattern definition
- [ ] Add tag pattern definition
- [ ] Add label-key pattern definition
- [ ] Define `ISchemaProvider` interface
- [ ] Implement `SchemaProvider` with caching
- [ ] Define `ISchemaValidator` interface
- [ ] Implement `SchemaValidator` using Json.Schema.Net
- [ ] Map schema errors to error codes
- [ ] Add unknown property warning detection
- [ ] Define `IFormatValidator` interface
- [ ] Implement `UlidFormatValidator`
- [ ] Implement `FilePathFormatValidator`
- [ ] Implement `TagFormatValidator`
- [ ] Create `SchemaExportCommand` CLI
- [ ] Register validators in DI
- [ ] Write unit tests for validators
- [ ] Write integration tests for full validation

### Rollout Plan

1. **Phase 1: Schema File** (Day 1)
   - Create complete JSON schema
   - Add as embedded resource
   - Validate schema syntax

2. **Phase 2: Provider** (Day 1)
   - Implement schema provider
   - Add caching
   - Unit test loading

3. **Phase 3: Format Validators** (Day 2)
   - Implement ULID validator
   - Implement file-path validator
   - Implement tag validator
   - Unit test each

4. **Phase 4: Schema Validator** (Day 2)
   - Implement validator with Json.Schema.Net
   - Add error mapping
   - Add warning detection

5. **Phase 5: CLI** (Day 3)
   - Export command
   - Manual testing

6. **Phase 6: Integration** (Day 3)
   - Wire up DI
   - IDE integration testing
   - Documentation

---

**End of Task 025.a Specification**