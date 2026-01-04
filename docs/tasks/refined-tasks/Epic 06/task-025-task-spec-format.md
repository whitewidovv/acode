# Task 025: Task Spec Format

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 002 (Config), Task 003 (Interfaces), Task 018 (Outbox)  

---

## Description

Task 025 defines the task specification format. A task spec is a structured document describing work for a worker to execute. This format MUST be parseable, validatable, and serializable.

Task specs MUST support both YAML and JSON formats. The schema MUST enforce required fields. Optional fields MUST have documented defaults. Validation MUST catch errors before queue insertion.

The task spec format MUST capture enough information for autonomous execution. This includes title, description, affected files, dependencies on other tasks, priority, and execution constraints.

### Business Value

A well-defined task spec format enables:
- Consistent task representation across the system
- Automated validation before execution
- Clear handoff between planner and executor
- Queryable task metadata for queue management
- Reproducible task execution

### Scope Boundaries

This task covers the core task spec format. Subtask 025.a covers schema details. Subtask 025.b covers CLI commands. Subtask 025.c covers error formatting.

### Integration Points

- Task 002: Config influences defaults
- Task 003: Task spec implements ITaskSpec
- Task 018: Completed tasks emit to outbox
- Task 026: Queue stores task specs

### Failure Modes

- Invalid YAML/JSON syntax → ParseError
- Missing required field → ValidationError
- Invalid field type → ValidationError
- Unknown field → Warning (lenient mode)
- Circular dependency → DependencyError

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| TaskSpec | Structured document describing work |
| ULID | Universally Unique Lexicographically Sortable Identifier |
| Schema | Structure definition for validation |
| Required Field | Field that MUST be present |
| Optional Field | Field with default value |
| Dependency | Task that MUST complete first |
| Priority | Execution order hint (1-5) |
| Tag | Categorization label |
| Metadata | Extension key-value pairs |
| Parser | Converts text to TaskSpec |
| Validator | Checks TaskSpec against schema |
| Serializer | Converts TaskSpec to text |

---

## Out of Scope

- Task execution logic (Epic 06, Task 027)
- Queue persistence (Task 026)
- Task decomposition (Epic 03)
- Git worktree mapping (Epic 05)
- Visual task editor
- Task templates
- Remote task submission
- Task versioning history

---

## Functional Requirements

### FR-001 to FR-025: Core Structure

- FR-001: TaskSpec MUST have `id` field (ULID)
- FR-002: TaskSpec MUST have `title` field (string, 1-200 chars)
- FR-003: TaskSpec MUST have `description` field (string, 1-10000 chars)
- FR-004: TaskSpec MUST have `status` field (enum)
- FR-005: TaskSpec MUST have `createdAt` field (ISO8601)
- FR-006: TaskSpec MAY have `priority` field (int 1-5, default 3)
- FR-007: TaskSpec MAY have `dependencies` field (string[])
- FR-008: TaskSpec MAY have `files` field (string[])
- FR-009: TaskSpec MAY have `tags` field (string[])
- FR-010: TaskSpec MAY have `metadata` field (object)
- FR-011: TaskSpec MAY have `timeout` field (int seconds)
- FR-012: TaskSpec MAY have `retryLimit` field (int, default 3)
- FR-013: TaskSpec MAY have `parentId` field (ULID)
- FR-014: TaskSpec MAY have `estimatedDuration` field (int seconds)
- FR-015: TaskSpec MAY have `labels` field (object)
- FR-016: ID MUST be auto-generated if not provided
- FR-017: Status MUST default to Pending
- FR-018: CreatedAt MUST default to now
- FR-019: Unknown fields MUST be preserved in metadata
- FR-020: Null values MUST be treated as absent
- FR-021: Empty string MUST fail validation for required fields
- FR-022: Whitespace-only MUST fail validation for required fields
- FR-023: Title MUST be trimmed
- FR-024: Description MUST be trimmed
- FR-025: Dependencies MUST be valid ULIDs

### FR-026 to FR-045: Parsing

- FR-026: YAML format MUST be supported
- FR-027: JSON format MUST be supported
- FR-028: Parser MUST detect format automatically
- FR-029: Parser MUST handle BOM
- FR-030: Parser MUST handle CRLF and LF
- FR-031: Parser MUST reject binary content
- FR-032: Parser MUST enforce size limit (1MB)
- FR-033: Parser MUST handle UTF-8
- FR-034: Parser MUST reject invalid UTF-8
- FR-035: YAML anchors MUST be supported
- FR-036: YAML merge keys MUST be supported
- FR-037: JSON comments MUST NOT be supported
- FR-038: Multi-document YAML MUST parse first only
- FR-039: Parse errors MUST include line/column
- FR-040: Parse errors MUST include context
- FR-041: Parser MUST be streaming for large files
- FR-042: Parser MUST timeout after 5 seconds
- FR-043: Parser MUST handle nested objects
- FR-044: Parser MUST limit nesting depth (20)
- FR-045: Parser MUST limit array size (10000)

### FR-046 to FR-070: Validation

- FR-046: Validation MUST check required fields
- FR-047: Validation MUST check field types
- FR-048: Validation MUST check string lengths
- FR-049: Validation MUST check numeric ranges
- FR-050: Validation MUST check enum values
- FR-051: Validation MUST check date formats
- FR-052: Validation MUST check ULID format
- FR-053: Validation MUST collect all errors
- FR-054: Validation MUST NOT stop at first error
- FR-055: Validation errors MUST include field path
- FR-056: Validation errors MUST include expected value
- FR-057: Validation errors MUST include actual value
- FR-058: Validation MUST check dependency cycles
- FR-059: Validation MUST check file paths
- FR-060: File paths MUST be relative
- FR-061: File paths MUST NOT traverse above root
- FR-062: File paths MUST be normalized
- FR-063: Tags MUST match pattern [a-z0-9-]+
- FR-064: Labels keys MUST match pattern [a-z0-9-]+
- FR-065: Labels values MUST be strings
- FR-066: Metadata keys MUST NOT start with underscore
- FR-067: Metadata MUST be JSON-serializable
- FR-068: Priority MUST be 1-5
- FR-069: Timeout MUST be positive
- FR-070: RetryLimit MUST be 0-10

---

## Non-Functional Requirements

- NFR-001: Parse MUST complete in <100ms for 1MB
- NFR-002: Validation MUST complete in <50ms
- NFR-003: Serialization MUST complete in <50ms
- NFR-004: Memory MUST NOT exceed 10x input size
- NFR-005: Parser MUST be thread-safe
- NFR-006: Validator MUST be thread-safe
- NFR-007: No external dependencies for parsing
- NFR-008: Schema MUST be versioned
- NFR-009: Schema MUST be backward compatible
- NFR-010: Schema changes MUST be documented
- NFR-011: Error messages MUST be actionable
- NFR-012: Error messages MUST NOT leak secrets
- NFR-013: Secrets in metadata MUST be redacted
- NFR-014: Stack traces MUST NOT appear in errors
- NFR-015: Logging MUST include correlation ID

---

## User Manual Documentation

### Quick Start

Create a task spec file:

```yaml
# task.yaml
title: "Implement user login"
description: |
  Add login functionality with email/password.
  Include validation and error handling.
priority: 2
files:
  - src/Auth/LoginHandler.cs
  - tests/Auth/LoginHandlerTests.cs
tags:
  - auth
  - feature
```

Parse and validate:

```bash
$ acode task validate task.yaml
✓ Task spec valid

$ acode task add task.yaml
Task abc123 added to queue
```

### Schema Reference

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| id | ULID | No | Auto | Unique identifier |
| title | string | Yes | - | Short title (1-200 chars) |
| description | string | Yes | - | Full description |
| status | enum | No | Pending | Current status |
| priority | int | No | 3 | Priority 1-5 (1=highest) |
| dependencies | string[] | No | [] | Task IDs to wait for |
| files | string[] | No | [] | Affected files |
| tags | string[] | No | [] | Categorization |
| metadata | object | No | {} | Extension data |
| timeout | int | No | 3600 | Seconds before timeout |
| retryLimit | int | No | 3 | Max retry attempts |

### CLI Examples

```bash
# Validate a task spec
acode task validate task.yaml

# Add from file
acode task add task.yaml

# Add from stdin
cat task.yaml | acode task add -

# Add with inline YAML
acode task add --title "Fix bug" --description "Fix login bug"

# Show task details
acode task show abc123

# List all tasks
acode task list

# List with filter
acode task list --status pending --priority 1

# Retry failed task
acode task retry abc123

# Cancel task
acode task cancel abc123
```

### Configuration

```yaml
# .agent/config.yml
task:
  defaultPriority: 3
  defaultTimeout: 3600
  defaultRetryLimit: 3
  maxDescriptionLength: 10000
  strictValidation: true
  unknownFieldsAllowed: false
```

### Troubleshooting

**"Missing required field: title"**
- Ensure your task spec includes a `title` field
- Check for typos in field names

**"Invalid ULID format"**
- IDs must be valid 26-character ULIDs
- Let the system auto-generate IDs

**"Circular dependency detected"**
- Check dependencies list for cycles
- Task A → B → A is not allowed

---

## Acceptance Criteria / Definition of Done

### Functionality
- [ ] AC-001: YAML parsing works
- [ ] AC-002: JSON parsing works
- [ ] AC-003: Format auto-detected
- [ ] AC-004: Required fields enforced
- [ ] AC-005: Optional fields have defaults
- [ ] AC-006: Unknown fields preserved
- [ ] AC-007: Validation catches all errors
- [ ] AC-008: Error messages are clear
- [ ] AC-009: Serialization round-trips
- [ ] AC-010: ID auto-generation works

### Safety
- [ ] AC-011: Size limits enforced
- [ ] AC-012: Depth limits enforced
- [ ] AC-013: Timeout enforced
- [ ] AC-014: Secrets redacted
- [ ] AC-015: Path traversal blocked

### CLI
- [ ] AC-016: validate command works
- [ ] AC-017: add command works
- [ ] AC-018: show command works
- [ ] AC-019: list command works
- [ ] AC-020: retry command works
- [ ] AC-021: cancel command works

### Logging
- [ ] AC-022: Parse events logged
- [ ] AC-023: Validation events logged
- [ ] AC-024: Errors logged with context
- [ ] AC-025: Correlation IDs included

### Performance
- [ ] AC-026: Parse <100ms for 1MB
- [ ] AC-027: Validate <50ms
- [ ] AC-028: Memory bounds respected

### Docs
- [ ] AC-029: Schema documented
- [ ] AC-030: CLI documented
- [ ] AC-031: Errors documented
- [ ] AC-032: Examples provided

### Tests
- [ ] AC-033: Unit tests pass
- [ ] AC-034: Integration tests pass
- [ ] AC-035: Coverage >80%

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Parse valid YAML
- [ ] UT-002: Parse valid JSON
- [ ] UT-003: Parse error handling
- [ ] UT-004: Validate required fields
- [ ] UT-005: Validate field types
- [ ] UT-006: Validate ranges
- [ ] UT-007: Validate dependencies
- [ ] UT-008: Serialize to YAML
- [ ] UT-009: Serialize to JSON
- [ ] UT-010: Round-trip equality

### Integration Tests

- [ ] IT-001: Full parse-validate-serialize
- [ ] IT-002: CLI validate command
- [ ] IT-003: CLI add command
- [ ] IT-004: Large file handling
- [ ] IT-005: Error aggregation

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Core/
│   └── Domain/
│       └── Tasks/
│           ├── TaskSpec.cs               # Task spec entity
│           ├── TaskStatus.cs             # Status enum
│           ├── TaskId.cs                 # ULID wrapper
│           └── TaskSpecDefaults.cs       # Default values
│
├── Acode.Application/
│   └── Services/
│       └── TaskSpec/
│           ├── ITaskSpecParser.cs        # Parser interface
│           ├── TaskSpecParser.cs         # YAML/JSON parser
│           ├── ITaskSpecValidator.cs     # Validator interface
│           ├── TaskSpecValidator.cs      # Validation rules
│           ├── ITaskSpecSerializer.cs    # Serializer interface
│           ├── TaskSpecSerializer.cs     # YAML/JSON output
│           ├── ParseResult.cs            # Parse result type
│           └── ValidationResult.cs       # Validation result
│
└── Acode.Cli/
    └── Commands/
        └── Task/
            ├── TaskValidateCommand.cs
            ├── TaskAddCommand.cs
            ├── TaskShowCommand.cs
            ├── TaskListCommand.cs
            ├── TaskRetryCommand.cs
            └── TaskCancelCommand.cs

tests/
├── Acode.Application.Tests/
│   └── Services/
│       └── TaskSpec/
│           ├── TaskSpecParserTests.cs
│           ├── TaskSpecValidatorTests.cs
│           └── TaskSpecSerializerTests.cs
│
└── Acode.Integration.Tests/
    └── TaskSpec/
        └── TaskSpecRoundTripTests.cs
```

### Domain Models

```csharp
// Acode.Core/Domain/Tasks/TaskId.cs
namespace Acode.Core.Domain.Tasks;

/// <summary>
/// Strongly-typed ULID wrapper for task identification.
/// </summary>
public readonly record struct TaskId
{
    private readonly string _value;
    
    public TaskId(string value)
    {
        if (!IsValid(value))
            throw new ArgumentException($"Invalid task ID: {value}", nameof(value));
        _value = value;
    }
    
    public static TaskId NewId() => new(Ulid.NewUlid().ToString());
    
    public static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 26)
            return false;
        
        return value.All(c => 
            (c >= '0' && c <= '9') || 
            (c >= 'A' && c <= 'Z'));
    }
    
    public static bool TryParse(string value, out TaskId id)
    {
        if (IsValid(value))
        {
            id = new TaskId(value);
            return true;
        }
        id = default;
        return false;
    }
    
    public string Value => _value;
    public override string ToString() => _value;
    
    public static implicit operator string(TaskId id) => id._value;
}

// Acode.Core/Domain/Tasks/TaskStatus.cs
namespace Acode.Core.Domain.Tasks;

/// <summary>
/// Status of a task in the queue.
/// </summary>
public enum TaskStatus
{
    /// <summary>Waiting to be executed.</summary>
    Pending = 0,
    
    /// <summary>Currently being executed.</summary>
    Running = 1,
    
    /// <summary>Successfully completed.</summary>
    Completed = 2,
    
    /// <summary>Execution failed.</summary>
    Failed = 3,
    
    /// <summary>Cancelled by user.</summary>
    Cancelled = 4,
    
    /// <summary>Blocked by dependencies.</summary>
    Blocked = 5
}

// Acode.Core/Domain/Tasks/TaskSpec.cs
namespace Acode.Core.Domain.Tasks;

/// <summary>
/// A structured task specification describing work for execution.
/// </summary>
public sealed record TaskSpec
{
    /// <summary>Unique task identifier (ULID).</summary>
    public required TaskId Id { get; init; }
    
    /// <summary>Short title (1-200 chars).</summary>
    public required string Title { get; init; }
    
    /// <summary>Full description (1-10000 chars).</summary>
    public required string Description { get; init; }
    
    /// <summary>Current status.</summary>
    public TaskStatus Status { get; init; } = TaskStatus.Pending;
    
    /// <summary>Priority 1-5 (1 = highest).</summary>
    public int Priority { get; init; } = TaskSpecDefaults.Priority;
    
    /// <summary>Task IDs that must complete first.</summary>
    public IReadOnlyList<TaskId> Dependencies { get; init; } = Array.Empty<TaskId>();
    
    /// <summary>Files affected by this task (relative paths).</summary>
    public IReadOnlyList<string> Files { get; init; } = Array.Empty<string>();
    
    /// <summary>Categorization tags.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    
    /// <summary>Extension metadata (JSON-serializable).</summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = 
        new Dictionary<string, object?>();
    
    /// <summary>Max execution time in seconds.</summary>
    public int TimeoutSeconds { get; init; } = TaskSpecDefaults.TimeoutSeconds;
    
    /// <summary>Max retry attempts (0-10).</summary>
    public int RetryLimit { get; init; } = TaskSpecDefaults.RetryLimit;
    
    /// <summary>Parent task ID (for subtasks).</summary>
    public TaskId? ParentId { get; init; }
    
    /// <summary>Estimated duration in seconds.</summary>
    public int? EstimatedDurationSeconds { get; init; }
    
    /// <summary>Key-value labels for filtering.</summary>
    public IReadOnlyDictionary<string, string> Labels { get; init; } = 
        new Dictionary<string, string>();
    
    /// <summary>When the task was created.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

// Acode.Core/Domain/Tasks/TaskSpecDefaults.cs
namespace Acode.Core.Domain.Tasks;

/// <summary>
/// Default values for task spec fields.
/// </summary>
public static class TaskSpecDefaults
{
    public const int Priority = 3;
    public const int TimeoutSeconds = 3600;
    public const int RetryLimit = 3;
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 10000;
    public const int MaxDependencies = 100;
    public const int MaxFiles = 1000;
    public const int MaxTags = 50;
    public const int MaxMetadataKeys = 100;
    public const int MaxFileSize = 1024 * 1024; // 1MB
    public const int MaxNestingDepth = 20;
    public const int MaxArraySize = 10000;
}
```

### Parse Result Types

```csharp
// Acode.Application/Services/TaskSpec/ParseResult.cs
namespace Acode.Application.Services.TaskSpec;

/// <summary>
/// Result of parsing a task spec.
/// </summary>
public sealed record ParseResult<T>
{
    public bool Success { get; init; }
    public T? Value { get; init; }
    public IReadOnlyList<ParseError> Errors { get; init; } = Array.Empty<ParseError>();
    public SerializationFormat DetectedFormat { get; init; }
    public TimeSpan Duration { get; init; }
    
    public static ParseResult<T> Ok(T value, SerializationFormat format, TimeSpan duration) => new()
    {
        Success = true,
        Value = value,
        DetectedFormat = format,
        Duration = duration
    };
    
    public static ParseResult<T> Fail(IEnumerable<ParseError> errors, TimeSpan duration) => new()
    {
        Success = false,
        Errors = errors.ToList(),
        Duration = duration
    };
}

/// <summary>
/// A parse error with location information.
/// </summary>
public sealed record ParseError
{
    public required string Message { get; init; }
    public int? Line { get; init; }
    public int? Column { get; init; }
    public string? Context { get; init; }
    public ParseErrorCode Code { get; init; } = ParseErrorCode.Unknown;
}

public enum ParseErrorCode
{
    Unknown,
    InvalidSyntax,
    InvalidEncoding,
    SizeLimitExceeded,
    NestingDepthExceeded,
    ArraySizeExceeded,
    Timeout,
    BinaryContent
}

public enum SerializationFormat
{
    Unknown,
    Yaml,
    Json
}
```

### Validation Result Types

```csharp
// Acode.Application/Services/TaskSpec/ValidationResult.cs
namespace Acode.Application.Services.TaskSpec;

/// <summary>
/// Result of validating a task spec.
/// </summary>
public sealed record ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();
    public IReadOnlyList<ValidationWarning> Warnings { get; init; } = Array.Empty<ValidationWarning>();
    
    public static ValidationResult Valid() => new();
    
    public static ValidationResult Invalid(IEnumerable<ValidationError> errors) => new()
    {
        Errors = errors.ToList()
    };
    
    public static ValidationResult WithWarnings(IEnumerable<ValidationWarning> warnings) => new()
    {
        Warnings = warnings.ToList()
    };
}

/// <summary>
/// A validation error with field path.
/// </summary>
public sealed record ValidationError
{
    public required string FieldPath { get; init; }
    public required string Message { get; init; }
    public string? ExpectedValue { get; init; }
    public string? ActualValue { get; init; }
    public ValidationErrorCode Code { get; init; }
}

public sealed record ValidationWarning
{
    public required string FieldPath { get; init; }
    public required string Message { get; init; }
}

public enum ValidationErrorCode
{
    Unknown,
    RequiredFieldMissing,
    InvalidFieldType,
    ValueOutOfRange,
    InvalidFormat,
    InvalidUlid,
    CircularDependency,
    InvalidFilePath,
    PathTraversal,
    StringTooLong,
    StringTooShort,
    InvalidPattern
}
```

### Parser Interface and Implementation

```csharp
// Acode.Application/Services/TaskSpec/ITaskSpecParser.cs
namespace Acode.Application.Services.TaskSpec;

/// <summary>
/// Parses task specifications from YAML or JSON.
/// </summary>
public interface ITaskSpecParser
{
    /// <summary>
    /// Parses a task spec from a stream.
    /// </summary>
    Task<ParseResult<TaskSpec>> ParseAsync(
        Stream input, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Parses a task spec from a string.
    /// </summary>
    Task<ParseResult<TaskSpec>> ParseAsync(
        string content, 
        CancellationToken ct = default);
}

// Acode.Application/Services/TaskSpec/TaskSpecParser.cs
namespace Acode.Application.Services.TaskSpec;

public sealed class TaskSpecParser : ITaskSpecParser
{
    private readonly ILogger<TaskSpecParser> _logger;
    private static readonly TimeSpan ParseTimeout = TimeSpan.FromSeconds(5);
    
    public TaskSpecParser(ILogger<TaskSpecParser> logger)
    {
        _logger = logger;
    }
    
    public async Task<ParseResult<TaskSpec>> ParseAsync(
        Stream input, 
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        using var timeoutCts = new CancellationTokenSource(ParseTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        
        try
        {
            // Check size limit
            if (input.CanSeek && input.Length > TaskSpecDefaults.MaxFileSize)
            {
                return ParseResult<TaskSpec>.Fail(
                    [new ParseError 
                    { 
                        Message = $"File exceeds size limit ({TaskSpecDefaults.MaxFileSize} bytes)",
                        Code = ParseErrorCode.SizeLimitExceeded
                    }],
                    stopwatch.Elapsed);
            }
            
            // Read content (with size limit for non-seekable streams)
            using var reader = new StreamReader(input, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await ReadWithLimitAsync(reader, TaskSpecDefaults.MaxFileSize, linkedCts.Token);
            
            return await ParseAsync(content, linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return ParseResult<TaskSpec>.Fail(
                [new ParseError 
                { 
                    Message = "Parse timeout exceeded",
                    Code = ParseErrorCode.Timeout
                }],
                stopwatch.Elapsed);
        }
    }
    
    public async Task<ParseResult<TaskSpec>> ParseAsync(
        string content, 
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Validate UTF-8
        if (!IsValidUtf8(content))
        {
            return ParseResult<TaskSpec>.Fail(
                [new ParseError 
                { 
                    Message = "Invalid UTF-8 encoding",
                    Code = ParseErrorCode.InvalidEncoding
                }],
                stopwatch.Elapsed);
        }
        
        // Check for binary content
        if (ContainsBinaryContent(content))
        {
            return ParseResult<TaskSpec>.Fail(
                [new ParseError 
                { 
                    Message = "Binary content not allowed",
                    Code = ParseErrorCode.BinaryContent
                }],
                stopwatch.Elapsed);
        }
        
        // Detect format
        var format = DetectFormat(content);
        
        try
        {
            var spec = format == SerializationFormat.Json
                ? ParseJson(content)
                : ParseYaml(content);
            
            return ParseResult<TaskSpec>.Ok(spec, format, stopwatch.Elapsed);
        }
        catch (YamlException ex)
        {
            return ParseResult<TaskSpec>.Fail(
                [new ParseError 
                { 
                    Message = ex.Message,
                    Line = ex.Start.Line,
                    Column = ex.Start.Column,
                    Code = ParseErrorCode.InvalidSyntax
                }],
                stopwatch.Elapsed);
        }
        catch (JsonException ex)
        {
            return ParseResult<TaskSpec>.Fail(
                [new ParseError 
                { 
                    Message = ex.Message,
                    Line = (int?)ex.LineNumber,
                    Code = ParseErrorCode.InvalidSyntax
                }],
                stopwatch.Elapsed);
        }
    }
    
    private static SerializationFormat DetectFormat(string content)
    {
        var trimmed = content.TrimStart();
        if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
            return SerializationFormat.Json;
        return SerializationFormat.Yaml;
    }
    
    private TaskSpec ParseYaml(string content)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        
        var dto = deserializer.Deserialize<TaskSpecDto>(content);
        return MapToTaskSpec(dto);
    }
    
    private TaskSpec ParseJson(string content)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            MaxDepth = TaskSpecDefaults.MaxNestingDepth
        };
        
        var dto = JsonSerializer.Deserialize<TaskSpecDto>(content, options)
            ?? throw new JsonException("Empty JSON document");
        
        return MapToTaskSpec(dto);
    }
    
    private TaskSpec MapToTaskSpec(TaskSpecDto dto)
    {
        var id = string.IsNullOrWhiteSpace(dto.Id) 
            ? TaskId.NewId() 
            : new TaskId(dto.Id.ToUpperInvariant());
        
        return new TaskSpec
        {
            Id = id,
            Title = dto.Title?.Trim() ?? throw new InvalidOperationException("Title is required"),
            Description = dto.Description?.Trim() ?? throw new InvalidOperationException("Description is required"),
            Status = dto.Status ?? TaskStatus.Pending,
            Priority = dto.Priority ?? TaskSpecDefaults.Priority,
            Dependencies = dto.Dependencies?.Select(d => new TaskId(d)).ToList() 
                ?? Array.Empty<TaskId>(),
            Files = dto.Files?.ToList() ?? Array.Empty<string>(),
            Tags = dto.Tags?.ToList() ?? Array.Empty<string>(),
            Metadata = dto.Metadata ?? new Dictionary<string, object?>(),
            TimeoutSeconds = dto.Timeout ?? TaskSpecDefaults.TimeoutSeconds,
            RetryLimit = dto.RetryLimit ?? TaskSpecDefaults.RetryLimit,
            ParentId = dto.ParentId != null ? new TaskId(dto.ParentId) : null,
            EstimatedDurationSeconds = dto.EstimatedDuration,
            Labels = dto.Labels ?? new Dictionary<string, string>(),
            CreatedAt = dto.CreatedAt ?? DateTimeOffset.UtcNow
        };
    }
    
    private static bool IsValidUtf8(string content) => true; // String is already UTF-16
    
    private static bool ContainsBinaryContent(string content)
    {
        foreach (var c in content)
        {
            if (c < 0x20 && c != '\r' && c != '\n' && c != '\t')
                return true;
        }
        return false;
    }
    
    private static async Task<string> ReadWithLimitAsync(
        StreamReader reader, 
        int maxBytes,
        CancellationToken ct)
    {
        var buffer = new char[8192];
        var sb = new StringBuilder();
        int totalRead = 0;
        int read;
        
        while ((read = await reader.ReadAsync(buffer, ct)) > 0)
        {
            totalRead += read * 2; // Approximate bytes
            if (totalRead > maxBytes)
                throw new InvalidOperationException("Size limit exceeded");
            
            sb.Append(buffer, 0, read);
        }
        
        return sb.ToString();
    }
}

/// <summary>
/// DTO for deserializing task specs.
/// </summary>
internal sealed class TaskSpecDto
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public TaskStatus? Status { get; set; }
    public int? Priority { get; set; }
    public List<string>? Dependencies { get; set; }
    public List<string>? Files { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
    public int? Timeout { get; set; }
    public int? RetryLimit { get; set; }
    public string? ParentId { get; set; }
    public int? EstimatedDuration { get; set; }
    public Dictionary<string, string>? Labels { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}
```

### Validator Implementation

```csharp
// Acode.Application/Services/TaskSpec/ITaskSpecValidator.cs
namespace Acode.Application.Services.TaskSpec;

/// <summary>
/// Validates task specifications.
/// </summary>
public interface ITaskSpecValidator
{
    /// <summary>
    /// Validates a task spec against all rules.
    /// </summary>
    Task<ValidationResult> ValidateAsync(
        TaskSpec spec, 
        CancellationToken ct = default);
}

// Acode.Application/Services/TaskSpec/TaskSpecValidator.cs
namespace Acode.Application.Services.TaskSpec;

public sealed class TaskSpecValidator : ITaskSpecValidator
{
    private static readonly Regex TagPattern = new(
        @"^[a-z0-9-]+$", 
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));
    
    private static readonly Regex LabelKeyPattern = new(
        @"^[a-z0-9-]+$", 
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));
    
    public Task<ValidationResult> ValidateAsync(
        TaskSpec spec, 
        CancellationToken ct = default)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();
        
        // Required fields
        ValidateRequired(spec.Title, "title", errors);
        ValidateRequired(spec.Description, "description", errors);
        
        // String lengths
        ValidateLength(spec.Title, "title", 1, TaskSpecDefaults.MaxTitleLength, errors);
        ValidateLength(spec.Description, "description", 1, TaskSpecDefaults.MaxDescriptionLength, errors);
        
        // Numeric ranges
        ValidateRange(spec.Priority, "priority", 1, 5, errors);
        ValidateRange(spec.RetryLimit, "retryLimit", 0, 10, errors);
        
        if (spec.TimeoutSeconds <= 0)
        {
            errors.Add(new ValidationError
            {
                FieldPath = "timeout",
                Message = "Timeout must be positive",
                ActualValue = spec.TimeoutSeconds.ToString(),
                Code = ValidationErrorCode.ValueOutOfRange
            });
        }
        
        // Collection sizes
        if (spec.Dependencies.Count > TaskSpecDefaults.MaxDependencies)
        {
            errors.Add(new ValidationError
            {
                FieldPath = "dependencies",
                Message = $"Too many dependencies (max {TaskSpecDefaults.MaxDependencies})",
                Code = ValidationErrorCode.ValueOutOfRange
            });
        }
        
        // Dependency cycle detection
        if (spec.Dependencies.Contains(spec.Id))
        {
            errors.Add(new ValidationError
            {
                FieldPath = "dependencies",
                Message = "Task cannot depend on itself",
                Code = ValidationErrorCode.CircularDependency
            });
        }
        
        // File paths
        for (int i = 0; i < spec.Files.Count; i++)
        {
            ValidateFilePath(spec.Files[i], $"files[{i}]", errors);
        }
        
        // Tags
        for (int i = 0; i < spec.Tags.Count; i++)
        {
            ValidatePattern(spec.Tags[i], $"tags[{i}]", TagPattern, errors);
        }
        
        // Label keys
        foreach (var key in spec.Labels.Keys)
        {
            ValidatePattern(key, $"labels.{key}", LabelKeyPattern, errors);
        }
        
        // Metadata keys
        foreach (var key in spec.Metadata.Keys)
        {
            if (key.StartsWith("_"))
            {
                errors.Add(new ValidationError
                {
                    FieldPath = $"metadata.{key}",
                    Message = "Metadata keys must not start with underscore",
                    Code = ValidationErrorCode.InvalidPattern
                });
            }
        }
        
        var result = errors.Count > 0
            ? ValidationResult.Invalid(errors)
            : ValidationResult.Valid();
        
        if (warnings.Count > 0)
        {
            result = result with { Warnings = warnings };
        }
        
        return Task.FromResult(result);
    }
    
    private static void ValidateRequired(string? value, string fieldPath, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError
            {
                FieldPath = fieldPath,
                Message = $"Required field '{fieldPath}' is missing or empty",
                Code = ValidationErrorCode.RequiredFieldMissing
            });
        }
    }
    
    private static void ValidateLength(
        string? value, 
        string fieldPath, 
        int min, 
        int max, 
        List<ValidationError> errors)
    {
        if (value == null) return;
        
        if (value.Length < min)
        {
            errors.Add(new ValidationError
            {
                FieldPath = fieldPath,
                Message = $"Value too short (min {min} chars)",
                ExpectedValue = $">= {min}",
                ActualValue = value.Length.ToString(),
                Code = ValidationErrorCode.StringTooShort
            });
        }
        else if (value.Length > max)
        {
            errors.Add(new ValidationError
            {
                FieldPath = fieldPath,
                Message = $"Value too long (max {max} chars)",
                ExpectedValue = $"<= {max}",
                ActualValue = value.Length.ToString(),
                Code = ValidationErrorCode.StringTooLong
            });
        }
    }
    
    private static void ValidateRange(
        int value, 
        string fieldPath, 
        int min, 
        int max, 
        List<ValidationError> errors)
    {
        if (value < min || value > max)
        {
            errors.Add(new ValidationError
            {
                FieldPath = fieldPath,
                Message = $"Value must be between {min} and {max}",
                ExpectedValue = $"{min}-{max}",
                ActualValue = value.ToString(),
                Code = ValidationErrorCode.ValueOutOfRange
            });
        }
    }
    
    private static void ValidateFilePath(string path, string fieldPath, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add(new ValidationError
            {
                FieldPath = fieldPath,
                Message = "File path cannot be empty",
                Code = ValidationErrorCode.RequiredFieldMissing
            });
            return;
        }
        
        // Must be relative
        if (Path.IsPathRooted(path))
        {
            errors.Add(new ValidationError
            {
                FieldPath = fieldPath,
                Message = "File path must be relative",
                ActualValue = path,
                Code = ValidationErrorCode.InvalidFilePath
            });
            return;
        }
        
        // No path traversal
        var normalized = Path.GetFullPath(Path.Combine("/root", path));
        if (!normalized.StartsWith("/root"))
        {
            errors.Add(new ValidationError
            {
                FieldPath = fieldPath,
                Message = "Path traversal not allowed",
                ActualValue = path,
                Code = ValidationErrorCode.PathTraversal
            });
        }
    }
    
    private static void ValidatePattern(
        string value, 
        string fieldPath, 
        Regex pattern, 
        List<ValidationError> errors)
    {
        try
        {
            if (!pattern.IsMatch(value))
            {
                errors.Add(new ValidationError
                {
                    FieldPath = fieldPath,
                    Message = $"Value does not match required pattern",
                    ExpectedValue = pattern.ToString(),
                    ActualValue = value,
                    Code = ValidationErrorCode.InvalidPattern
                });
            }
        }
        catch (RegexMatchTimeoutException)
        {
            errors.Add(new ValidationError
            {
                FieldPath = fieldPath,
                Message = "Pattern validation timed out",
                Code = ValidationErrorCode.Unknown
            });
        }
    }
}
```

### Serializer Implementation

```csharp
// Acode.Application/Services/TaskSpec/ITaskSpecSerializer.cs
namespace Acode.Application.Services.TaskSpec;

/// <summary>
/// Serializes task specifications to YAML or JSON.
/// </summary>
public interface ITaskSpecSerializer
{
    /// <summary>
    /// Serializes a task spec to the specified format.
    /// </summary>
    Task<string> SerializeAsync(
        TaskSpec spec, 
        SerializationFormat format,
        CancellationToken ct = default);
}

// Acode.Application/Services/TaskSpec/TaskSpecSerializer.cs
namespace Acode.Application.Services.TaskSpec;

public sealed class TaskSpecSerializer : ITaskSpecSerializer
{
    public Task<string> SerializeAsync(
        TaskSpec spec, 
        SerializationFormat format,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        var result = format switch
        {
            SerializationFormat.Json => SerializeJson(spec),
            SerializationFormat.Yaml => SerializeYaml(spec),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
        
        return Task.FromResult(result);
    }
    
    private static string SerializeJson(TaskSpec spec)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        return JsonSerializer.Serialize(spec, options);
    }
    
    private static string SerializeYaml(TaskSpec spec)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();
        
        return serializer.Serialize(spec);
    }
}
```

### CLI Commands

```csharp
// Acode.Cli/Commands/Task/TaskValidateCommand.cs
namespace Acode.Cli.Commands.Task;

[Command("task validate", Description = "Validate a task spec file")]
public sealed class TaskValidateCommand : ICommand
{
    [CommandArgument(0, "<file>", Description = "Task spec file path")]
    public string FilePath { get; init; } = string.Empty;
    
    [CommandOption("--json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var parser = GetParser(); // DI
        var validator = GetValidator();
        
        await using var stream = File.OpenRead(FilePath);
        var parseResult = await parser.ParseAsync(stream);
        
        if (!parseResult.Success)
        {
            foreach (var error in parseResult.Errors)
            {
                var location = error.Line.HasValue 
                    ? $":{error.Line}:{error.Column}" 
                    : "";
                console.Error.WriteLine($"✗ Parse error{location}: {error.Message}");
            }
            Environment.ExitCode = ExitCodes.ParseError;
            return;
        }
        
        var validationResult = await validator.ValidateAsync(parseResult.Value!);
        
        if (Json)
        {
            console.Output.WriteLine(JsonSerializer.Serialize(validationResult));
            return;
        }
        
        if (validationResult.IsValid)
        {
            console.Output.WriteLine("✓ Task spec valid");
            return;
        }
        
        foreach (var error in validationResult.Errors)
        {
            console.Error.WriteLine($"✗ {error.FieldPath}: {error.Message}");
        }
        
        Environment.ExitCode = ExitCodes.ValidationError;
    }
}

// Acode.Cli/Commands/Task/TaskAddCommand.cs
namespace Acode.Cli.Commands.Task;

[Command("task add", Description = "Add a task to the queue")]
public sealed class TaskAddCommand : ICommand
{
    [CommandArgument(0, "[file]", Description = "Task spec file (use - for stdin)")]
    public string? FilePath { get; init; }
    
    [CommandOption("--title", Description = "Task title (inline mode)")]
    public string? Title { get; init; }
    
    [CommandOption("--description", Description = "Task description (inline mode)")]
    public string? Description { get; init; }
    
    [CommandOption("--priority", Description = "Priority 1-5")]
    public int? Priority { get; init; }
    
    [CommandOption("--json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var parser = GetParser();
        var validator = GetValidator();
        var queue = GetQueue();
        
        TaskSpec spec;
        
        if (Title != null && Description != null)
        {
            // Inline mode
            spec = new TaskSpec
            {
                Id = TaskId.NewId(),
                Title = Title,
                Description = Description,
                Priority = Priority ?? TaskSpecDefaults.Priority
            };
        }
        else if (FilePath == "-")
        {
            // Stdin mode
            using var reader = new StreamReader(Console.OpenStandardInput());
            var content = await reader.ReadToEndAsync();
            var result = await parser.ParseAsync(content);
            if (!result.Success)
            {
                console.Error.WriteLine($"Parse error: {result.Errors[0].Message}");
                Environment.ExitCode = ExitCodes.ParseError;
                return;
            }
            spec = result.Value!;
        }
        else if (FilePath != null)
        {
            // File mode
            await using var stream = File.OpenRead(FilePath);
            var result = await parser.ParseAsync(stream);
            if (!result.Success)
            {
                console.Error.WriteLine($"Parse error: {result.Errors[0].Message}");
                Environment.ExitCode = ExitCodes.ParseError;
                return;
            }
            spec = result.Value!;
        }
        else
        {
            console.Error.WriteLine("Provide file path, use - for stdin, or --title/--description for inline");
            Environment.ExitCode = ExitCodes.InvalidArgument;
            return;
        }
        
        var validation = await validator.ValidateAsync(spec);
        if (!validation.IsValid)
        {
            console.Error.WriteLine($"Validation error: {validation.Errors[0].Message}");
            Environment.ExitCode = ExitCodes.ValidationError;
            return;
        }
        
        var taskId = await queue.EnqueueAsync(spec);
        
        if (Json)
        {
            console.Output.WriteLine(JsonSerializer.Serialize(new { id = taskId }));
        }
        else
        {
            console.Output.WriteLine($"Task {taskId} added to queue");
        }
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| TASK-001 | Parse error |
| TASK-002 | Missing required field |
| TASK-003 | Invalid field type |
| TASK-004 | Value out of range |
| TASK-005 | Invalid ULID |
| TASK-006 | Circular dependency |
| TASK-007 | Invalid file path |
| TASK-008 | Size limit exceeded |

### Implementation Checklist

- [ ] Create `TaskId` value object with ULID validation
- [ ] Create `TaskStatus` enum
- [ ] Create `TaskSpec` record with all fields
- [ ] Create `TaskSpecDefaults` constants
- [ ] Create `ParseResult<T>` and `ParseError` types
- [ ] Create `ValidationResult` and `ValidationError` types
- [ ] Implement `ITaskSpecParser` interface
- [ ] Implement YAML parsing with YamlDotNet
- [ ] Implement JSON parsing with System.Text.Json
- [ ] Add format auto-detection
- [ ] Add size limit checking
- [ ] Add timeout handling
- [ ] Implement `ITaskSpecValidator` interface
- [ ] Add required field validation
- [ ] Add string length validation
- [ ] Add numeric range validation
- [ ] Add file path validation (relative, no traversal)
- [ ] Add tag/label pattern validation
- [ ] Add dependency cycle detection (self-reference)
- [ ] Implement `ITaskSpecSerializer` interface
- [ ] Add YAML serialization
- [ ] Add JSON serialization
- [ ] Create `TaskValidateCommand` CLI
- [ ] Create `TaskAddCommand` CLI with file/stdin/inline modes
- [ ] Register services in DI
- [ ] Write unit tests for parser
- [ ] Write unit tests for validator
- [ ] Write round-trip tests

### Rollout Plan

1. **Phase 1: Domain Models** (Day 1)
   - Create all records and enums
   - Create TaskId with ULID support
   - Unit test value objects

2. **Phase 2: Parser** (Day 2)
   - Implement YAML parsing
   - Implement JSON parsing
   - Add format detection
   - Add size/timeout limits
   - Unit test parse scenarios

3. **Phase 3: Validator** (Day 2)
   - Implement all validation rules
   - Add regex pattern matching
   - Add path traversal detection
   - Unit test validation rules

4. **Phase 4: Serializer** (Day 3)
   - Implement YAML output
   - Implement JSON output
   - Test round-trip equality

5. **Phase 5: CLI** (Day 3)
   - Implement validate command
   - Implement add command
   - Manual testing

6. **Phase 6: Integration** (Day 4)
   - Wire up DI
   - Integration tests
   - Documentation

---

**End of Task 025 Specification**