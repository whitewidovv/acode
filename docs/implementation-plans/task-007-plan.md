# Task 007 Implementation Plan - Tool Schema Registry & Strict Validation

## Status: In Progress

## Branch: feature/task-007-tool-schema-registry

## Current Session Progress

### Completed ✅

1. **Domain Layer - Tools Types**
   - `ErrorSeverity` enum (Error, Warning, Info)
   - `SchemaValidationError` record with Severity, Path, Code, Message, ExpectedType, ActualValue
   - 5 tests passing

2. **Application Layer - Retry Contract Types**
   - `ValidationErrorCode` static class with VAL-001 to VAL-010 constants
   - `RetryConfiguration` record with defaults (MaxRetries=3, MaxErrorsShown=10, etc.)
   - `IValidationErrorFormatter` interface for formatting errors to model messages
   - `IRetryTracker` interface for tracking retry attempts
   - `ValidationAttempt` record for history tracking
   - 14 tests passing

### Remaining for Task 007 Parent ⏳

1. **Domain Layer**
   - `SchemaValidationException` - exception wrapping validation errors

2. **Application Layer**
   - `IToolSchemaRegistry` interface
   - `IToolSchemaProvider` interface
   - `ToolSchemaValidationResult` class

3. **Infrastructure Layer**
   - `ToolSchemaRegistry` implementation with NJsonSchema
   - `ToolSchemaServiceExtensions` for DI registration
   - `ValidationErrorFormatter` implementation
   - `RetryTracker` implementation

### Remaining for Subtasks ⏳

- Task 007a: 17 core tool schemas (CoreToolsProvider)
- Task 007b: Complete error formatter and retry tracker
- Task 007c: Truncation processor and strategies
- Task 007d: Tool call parsing and JSON repair
- Task 007e: Structured outputs for vLLM

## Next Session Resume Point

Continue with Task 007 Parent:
1. Create `SchemaValidationException` in Domain
2. Create `IToolSchemaRegistry` in Application
3. Create `IToolSchemaProvider` in Application
4. Implement `ToolSchemaRegistry` in Infrastructure with NJsonSchema

## Files Created This Session

```
src/Acode.Domain/Tools/
├── ErrorSeverity.cs
└── SchemaValidationError.cs

src/Acode.Application/Tools/Retry/
├── IRetryTracker.cs
├── IValidationErrorFormatter.cs
├── RetryConfiguration.cs
└── ValidationErrorCode.cs

tests/Acode.Domain.Tests/Tools/
└── ErrorSeverityTests.cs

tests/Acode.Application.Tests/Tools/Retry/
└── ValidationErrorCodeTests.cs
```

## Test Results

- Domain Tools: 5 tests passing
- Application Tools: 14 tests passing
- Total: 19 tests passing
