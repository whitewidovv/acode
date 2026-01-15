# Task-007b Gap Analysis: Validator Errors & Model Retry Contract

**Task**: task-007b-validator-errors-model-retry-contract.md (Epic 01)
**Completed By**: Gap Analysis Phase
**Date**: 2026-01-13
**Specification**: ~1600 lines, sections 1-16 covering complete retry contract with error formatting, validation error types, retry tracking, escalation, sanitization, and configuration

---

## Executive Summary

The task-007b specification defines a complete validator errors and model retry contract system. The codebase has partial infrastructure (Domain-level types and some interfaces), but is **significantly misaligned** with the spec regarding:

1. **Namespace Organization**: Current code uses Domain.Tools and Application.Tools.Retry; spec requires Application.ToolSchemas.Retry and Infrastructure.ToolSchemas.Retry
2. **Type Definitions**: Spec defines ValidationError (sealed class with required fields) but code uses SchemaValidationError (record)
3. **Interface Signatures**: Spec uses IErrorFormatter.FormatErrors(string, IEnumerable\<ValidationError\>, int, int) but code uses IValidationErrorFormatter.FormatErrors(string, IReadOnlyCollection\<SchemaValidationError\>, int, int)
4. **Enum Values**: ErrorSeverity values reversed (spec: Info=0, Warning=1, Error=2; code: Error=0, Warning=1, Info=2)
5. **Missing Classes**: ErrorAggregator, ValueSanitizer, IEscalationFormatter, EscalationFormatter, ErrorFormatter don't exist
6. **Test Coverage**: Insufficient - spec requires 6+ test files with specific test methods, only 3 partial files exist

**Scope**: All 15+ classes, 50+ tests, integration/E2E/performance tests, configuration, DI registration

**Recommendation**: Significant refactoring required. Current infrastructure must be restructured to match spec exactly. Consider this a partial restart with specification-driven implementation.

---

## Current State Analysis

### What Exists in Codebase

#### Domain Layer (src/Acode.Domain/Tools/)
- ✅ **ErrorSeverity.cs** - enum with Error/Warning/Info (values inverted vs spec)
- ✅ **SchemaValidationError.cs** - record type (should be ValidationError in Application layer)

#### Application Layer (src/Acode.Application/Tools/Retry/)
- ✅ **IRetryTracker.cs** - interface (signatures mismatched with spec)
- ✅ **IValidationErrorFormatter.cs** - interface (should be IErrorFormatter; includes ValidationAttempt record)
- ✅ **RetryConfiguration.cs** - configuration class (appears spec-compliant)
- ✅ **ValidationErrorCode.cs** - error code constants (should be ErrorCode.cs)

#### Infrastructure Layer (src/Acode.Infrastructure/Tools/)
- ✅ **RetryTracker.cs** - implementation (not in correct Retry subdirectory)
- ✅ **ValidationErrorFormatter.cs** - implementation (should be ErrorFormatter.cs in ToolSchemas/Retry/)

#### Tests (src/Acode.Application.Tests/Tools/Retry/)
- ✅ **ValidationErrorCodeTests.cs** - tests for error codes

#### Tests (src/Acode.Infrastructure.Tests/Tools/)
- ✅ **RetryTrackerTests.cs** - retry tracker tests (not in ToolSchemas/Retry/)
- ✅ **ValidationErrorFormatterTests.cs** - formatter tests (not in ToolSchemas/Retry/)

### What Does NOT Exist (Per Spec)

#### Application Layer - Required by Spec
- ❌ **ValidationError.cs** - sealed class (currently as SchemaValidationError in Domain)
- ❌ **ErrorSeverity.cs** - should be in Application, not Domain
- ❌ **ErrorCode.cs** - currently as ValidationErrorCode.cs
- ❌ **IErrorFormatter.cs** - currently as IValidationErrorFormatter.cs
- ❌ **IEscalationFormatter.cs** - completely missing

#### Infrastructure Layer - Required by Spec
- ❌ **Directory**: src/Acode.Infrastructure/ToolSchemas/Retry/ (doesn't exist)
- ❌ **ErrorFormatter.cs** - implementation (currently ValidationErrorFormatter.cs in wrong location)
- ❌ **ErrorAggregator.cs** - error deduplication/sorting logic
- ❌ **ValueSanitizer.cs** - secret redaction and value truncation
- ❌ **EscalationFormatter.cs** - escalation message formatting
- ❌ **DI Registration**: AddRetryContract() extension in DependencyInjection.cs

#### Test Files - Required by Spec
- ❌ **ValidationErrorTests.cs** - 9+ tests for ValidationError sealed class
- ❌ **ErrorCodeTests.cs** - tests for ErrorCode constants
- ❌ **ErrorFormatterTests.cs** - 12+ tests for error formatting in correct location
- ❌ **ErrorAggregatorTests.cs** - 8+ tests for error aggregation
- ❌ **ValueSanitizerTests.cs** - 8+ tests for value sanitization
- ❌ **EscalationFormatterTests.cs** - 5+ tests for escalation formatting
- ❌ **RetryContractIntegrationTests.cs** - integration tests (5+ tests)
- ❌ **ModelRetryE2ETests.cs** - E2E tests with model simulation (2+ tests)
- ❌ **RetryPerformanceTests.cs** - performance benchmarks (5+ tests)

#### Optional Infrastructure
- ⚠️ **Database Integration**: Existing DatabaseRetryPolicy in Infrastructure/Persistence/Retry (not required by 007b but may need alignment)

---

## Critical Misalignments

### 1. Namespace Hierarchy Mismatch

**Spec Requires**:
```
src/Acode.Application/ToolSchemas/Retry/
src/Acode.Infrastructure/ToolSchemas/Retry/
tests/Acode.Application.Tests/ToolSchemas/Retry/
tests/Acode.Infrastructure.Tests/ToolSchemas/Retry/
```

**Current State**:
```
src/Acode.Application/Tools/Retry/                    (WRONG: Application.Tools not Application.ToolSchemas)
src/Acode.Infrastructure/Tools/                        (WRONG: no Retry subdirectory)
src/Acode.Domain/Tools/                                (WRONG: Domain layer, not Application)
tests/Acode.Application.Tests/Tools/Retry/
tests/Acode.Infrastructure.Tests/Tools/
```

**Impact**: Major - requires moving/restructuring all files

### 2. Type and Interface Naming

**Spec Names vs Current Names**:
| Spec Name | Spec Location | Current Name | Current Location |
|-----------|---------------|--------------|------------------|
| ValidationError | Application.ToolSchemas.Retry | SchemaValidationError | Domain.Tools |
| ErrorSeverity | Application.ToolSchemas.Retry | ErrorSeverity | Domain.Tools |
| ErrorCode (static class) | Application.ToolSchemas.Retry | ValidationErrorCode (static class) | Application.Tools.Retry |
| IErrorFormatter | Application.ToolSchemas.Retry | IValidationErrorFormatter | Application.Tools.Retry |
| IRetryTracker | Application.ToolSchemas.Retry | IRetryTracker | Application.Tools.Retry |
| IEscalationFormatter | Application.ToolSchemas.Retry | (missing) | N/A |
| RetryConfiguration | Application.ToolSchemas.Retry | RetryConfiguration | Application.Tools.Retry |
| ErrorFormatter | Infrastructure.ToolSchemas.Retry | ValidationErrorFormatter | Infrastructure.Tools |
| ErrorAggregator | Infrastructure.ToolSchemas.Retry | (missing) | N/A |
| ValueSanitizer | Infrastructure.ToolSchemas.Retry | (missing) | N/A |
| EscalationFormatter | Infrastructure.ToolSchemas.Retry | (missing) | N/A |
| RetryTracker | Infrastructure.ToolSchemas.Retry | RetryTracker | Infrastructure.Tools |

### 3. ErrorSeverity Enum Values

**Spec Definition** (Implementation Prompt, lines 3226-3241):
```csharp
public enum ErrorSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}
```

**Current Definition** (Domain/Tools/ErrorSeverity.cs):
```csharp
public enum ErrorSeverity
{
    Error = 0,
    Warning = 1,
    Info = 2
}
```

**Impact**: High - spec intends Info as lowest severity (0), current code has Error as lowest. This affects sorting logic and model comprehension. MUST be changed.

### 4. Interface Method Signatures

**Spec IRetryTracker** (Implementation Prompt, lines 3362-3411):
```csharp
int IncrementAttempt(string toolCallId);
int GetCurrentAttempt(string toolCallId);
void RecordError(string toolCallId, string errorMessage);
IReadOnlyList<string> GetHistory(string toolCallId);
bool HasExceededMaxRetries(string toolCallId);
void Clear(string toolCallId);
```

**Current IRetryTracker** (Application/Tools/Retry/IRetryTracker.cs):
```csharp
void RecordAttempt(string toolCallId, IReadOnlyCollection<SchemaValidationError> errors);
int GetAttemptCount(string toolCallId);
IReadOnlyList<ValidationAttempt> GetHistory(string toolCallId);
bool HasExceededMaxRetries(string toolCallId);
void Clear(string toolCallId);
```

**Issues**:
- Spec's `IncrementAttempt()` missing (replaced with `RecordAttempt()`)
- Spec's `GetCurrentAttempt()` renamed to `GetAttemptCount()`
- Spec's `RecordError(string)` replaced with `RecordAttempt(SchemaValidationError[])`
- History return type changed from `IReadOnlyList<string>` to `IReadOnlyList<ValidationAttempt>`

**Spec IErrorFormatter** (Implementation Prompt, lines 3342-3355):
```csharp
string FormatErrors(string toolName, IEnumerable<ValidationError> errors, int attemptNumber, int maxAttempts);
```

**Current IValidationErrorFormatter** (Application/Tools/Retry/IValidationErrorFormatter.cs):
```csharp
string FormatErrors(string toolName, IReadOnlyCollection<SchemaValidationError> errors, int attemptNumber, int maxAttempts);
string FormatEscalation(string toolName, IReadOnlyList<ValidationAttempt> history);
```

**Issues**:
- Interface name different (IValidationErrorFormatter vs IErrorFormatter)
- Parameter types different (ValidationError vs SchemaValidationError)
- FormatEscalation() in formatter interface (spec has separate IEscalationFormatter)

### 5. Missing Error Aggregation

Spec requires ErrorAggregator (Implementation Prompt, lines 3809-3856) that:
- Deduplicates errors by field path + error code
- Sorts by severity (Error > Warning > Info), then by field path
- Limits error count
- Returns IReadOnlyList\<ValidationError\>

**Current State**: No aggregation logic exists; error formatting implementation must add this.

### 6. Missing Value Sanitization

Spec requires ValueSanitizer (Implementation Prompt, lines 3678-3806) that:
- Detects and redacts JWT tokens via regex pattern
- Detects and redacts OpenAI API keys
- Detects and redacts AWS credentials
- Detects and redacts generic long alphanumeric strings
- Redacts field-level sensitive fields (password, token, api_key, etc.)
- Truncates long strings with smart middle-elision ("prefix...suffix")
- Relativizes absolute file paths
- Preserves Unicode

**Current State**: No dedicated sanitizer exists; some redaction may be in ValidationErrorFormatter but needs extraction and formalization.

### 7. Missing Escalation Formatter

Spec requires IEscalationFormatter and EscalationFormatter (Implementation Prompt, lines 3414-4017) that:
- Formats messages after max retries exceeded
- Includes full validation history with timestamps
- Provides analysis and recommendations
- Suggests actions for improving schema/documentation

**Current State**: Escalation logic missing entirely; current formatter doesn't handle escalation.

---

## Test Coverage Gaps

### Unit Tests (15+ tests missing)

**ValidationErrorTests.cs** - 9 tests (MISSING):
- Should create with all fields
- Should use JSON Pointer path format
- Should support all severity levels
- Should support all error codes
- Should handle null optional fields
- Should preserve Unicode in values
- Should not exist elsewhere in codebase

**ErrorCodeTests.cs** - Tests (MISSING):
- Should have all 15 error codes (VAL-001 through VAL-015) as constants
- Tests exist in ValidationErrorCodeTests.cs but may not be comprehensive

**ErrorFormatterTests.cs** - 12+ tests (MISSING, partially in ValidationErrorFormatterTests.cs):
- Should format single error
- Should format multiple errors
- Should include tool name
- Should include attempt number
- Should truncate long values
- Should respect max length
- Should include correction hints
- Should sort errors by field path
- Should complete in under 1ms
- (Others)

**ErrorAggregatorTests.cs** - 8+ tests (MISSING):
- Should deduplicate by field path + error code
- Should sort by severity then field path
- Should respect max errors limit
- Should handle empty input
- (Others)

**ValueSanitizerTests.cs** - 8+ tests (MISSING):
- Should redact JWT tokens
- Should redact OpenAI API keys
- Should redact password fields by name
- Should redact AWS access keys
- Should truncate long strings
- Should relativize absolute file paths
- Should preserve Unicode
- Should handle null values
- Should use smart truncation strategy

**EscalationFormatterTests.cs** - 5+ tests (MISSING):
- Should format escalation message
- Should include full history
- Should include attempt timestamps
- Should provide recommendations
- (Others)

**RetryTrackerTests.cs** - Already exists but may need updates for new interface signature changes

### Integration Tests (5+ tests missing)

**RetryContractIntegrationTests.cs** (MISSING):
- Should create ToolResult on validation failure
- Should track retry across turns
- Should escalate after max retries
- Should handle concurrent tool calls
- Should apply sanitization in full flow

### End-to-End Tests (2+ tests missing)

**ModelRetryE2ETests.cs** (MISSING):
- Should correct missing required field on retry
- Should correct type mismatch on retry

### Performance Tests (5+ tests missing)

**RetryPerformanceTests.cs** (MISSING):
- Error formatting should complete under 1ms (1000 iterations)
- Retry tracker should be O(1) lookup
- Benchmark test suite using BenchmarkDotNet

---

## Dependency Analysis

### Internal Dependencies
- **Application Layer**: No external dependencies; depends on clean architecture
- **Infrastructure Layer**: Depends on Application.ToolSchemas.Retry interfaces
- **Tests**: Depend on xUnit, FluentAssertions, NSubstitute (already available)

### Cross-Task Dependencies
- **Task-007a** (Model Tool Calling): Produces ToolCall/ToolResult objects that this task consumes
- **Task-007c** (Agentic Loop Orchestration): Will consume retry contract for orchestrating retries
- **Task-005b** (Validation Schema Engine): Produces SchemaValidationError objects that must be converted to ValidationError

### Configuration Dependencies
- **Configuration Source**: .agent/config.yml (Tools:Validation:Retry section)
- **DI Registration**: src/Acode.Infrastructure/DependencyInjection.cs needs AddRetryContract() extension

---

## Missing Implementation Details

### Configuration Structure (Spec-Defined)

From Implementation Prompt (lines 3449-3505), RetryConfiguration must have:
```csharp
public int MaxAttempts { get; set; } = 3;
public int MaxMessageLength { get; set; } = 2000;
public int MaxErrorsShown { get; set; } = 10;
public int MaxValuePreview { get; set; } = 100;
public bool IncludeHints { get; set; } = true;
public bool IncludeActualValues { get; set; } = true;
public bool TrackHistory { get; set; } = true;
public bool RedactSecrets { get; set; } = true;
public bool RelativizePaths { get; set; } = true;
```

Current RetryConfiguration needs verification against spec (may already match).

### Error Code Constants (15 codes required)

From ErrorCode.cs spec (lines 3244-3331), all these constants must exist:
- VAL-001: RequiredFieldMissing
- VAL-002: TypeMismatch
- VAL-003: ConstraintViolation
- VAL-004: InvalidJsonSyntax
- VAL-005: UnknownField
- VAL-006: ArrayLengthViolation
- VAL-007: PatternMismatch
- VAL-008: InvalidEnumValue
- VAL-009: StringLengthViolation
- VAL-010: FormatViolation
- VAL-011: NumberRangeViolation
- VAL-012: UniqueConstraintViolation
- VAL-013: DependencyViolation
- VAL-014: MutualExclusivityViolation
- VAL-015: ObjectSchemaViolation

Current ValidationErrorCode.cs needs verification that all 15 are defined.

### Secret Redaction Patterns (Spec-Required)

ValueSanitizer must detect these pattern types:
- JWT tokens: `^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$`
- OpenAI API keys: `sk-[A-Za-z0-9]{32,}`
- AWS Access Keys: `AKIA[A-Z0-9]{16}`
- Generic long alphanumeric: `^[A-Za-z0-9]{32,64}$`
- Private keys: Contains "-----BEGIN PRIVATE KEY-----"
- Connection strings: Contains connectionString, jdbc:, or Data Source=
- Field-level: password, passwd, pass, pwd, secret, credentials, api_key, apiKey, apikey, access_key, accessKey, token, auth_token, authToken, bearer, jwt

---

## Remediation Strategy

### Phase 1: Restructure & Create Application Layer Types (Critical Path)

1. ✅ Create src/Acode.Application/ToolSchemas/ directory structure
2. ❌ Move/create ValidationError.cs from SchemaValidationError (different namespace, sealed class)
3. ❌ Move/create ErrorSeverity.cs (from Domain.Tools, fix enum values: Info=0, Warning=1, Error=2)
4. ❌ Create ErrorCode.cs (rename and restructure from ValidationErrorCode.cs)
5. ❌ Create IErrorFormatter.cs interface (refactor from IValidationErrorFormatter.cs)
6. ❌ Create IRetryTracker.cs interface (refactor with correct method signatures)
7. ❌ Create IEscalationFormatter.cs interface (brand new)
8. ✅ Move RetryConfiguration.cs to correct location (already mostly correct)

### Phase 2: Create Infrastructure Layer Implementation (Critical Path)

9. ✅ Create src/Acode.Infrastructure/ToolSchemas/Retry/ directory
10. ❌ Create ErrorFormatter.cs (refactor from ValidationErrorFormatter.cs)
11. ❌ Create ErrorAggregator.cs (brand new - error deduplication & sorting)
12. ❌ Create ValueSanitizer.cs (brand new - secret redaction & truncation)
13. ❌ Create EscalationFormatter.cs (brand new - escalation messages)
14. ❌ Create RetryTracker.cs implementation (move from Infrastructure.Tools/, update interface)
15. ❌ Add DI registration AddRetryContract() to DependencyInjection.cs

### Phase 3: Create Unit Tests (TDD - Tests First, Then Implementation)

16. ❌ tests/Acode.Application.Tests/ToolSchemas/Retry/ValidationErrorTests.cs (9 tests)
17. ❌ tests/Acode.Application.Tests/ToolSchemas/Retry/ErrorCodeTests.cs (per spec)
18. ❌ tests/Acode.Infrastructure.Tests/ToolSchemas/Retry/ErrorFormatterTests.cs (12+ tests)
19. ❌ tests/Acode.Infrastructure.Tests/ToolSchemas/Retry/ErrorAggregatorTests.cs (8+ tests)
20. ❌ tests/Acode.Infrastructure.Tests/ToolSchemas/Retry/ValueSanitizerTests.cs (8+ tests)
21. ❌ tests/Acode.Infrastructure.Tests/ToolSchemas/Retry/EscalationFormatterTests.cs (5+ tests)
22. ❌ tests/Acode.Infrastructure.Tests/ToolSchemas/Retry/RetryTrackerTests.cs (update existing or recreate)

### Phase 4: Create Integration & E2E Tests

23. ❌ tests/Acode.Integration.Tests/ToolSchemas/Retry/RetryContractIntegrationTests.cs (5+ tests)
24. ❌ tests/Acode.E2E.Tests/ToolSchemas/Retry/ModelRetryE2ETests.cs (2+ tests)
25. ❌ tests/Acode.Performance.Tests/ToolSchemas/Retry/RetryPerformanceTests.cs (5+ tests)

### Phase 5: Address Cross-Cutting Concerns

26. ❌ Handle SchemaValidationError → ValidationError conversion (may stay in Domain or create converter)
27. ❌ Verify .agent/config.yml schema for retry configuration
28. ❌ Update any code consuming old interfaces (ValidationErrorFormatter, etc.)
29. ❌ Verify error code compatibility with downstream task-007c

### Phase 6: Verification & Cleanup

30. ❌ Run full test suite (target: 50+ tests passing)
31. ❌ Verify build succeeds with no warnings/errors
32. ❌ Verify performance benchmarks meet <1ms requirement
33. ❌ Run audit checklist against spec
34. ❌ Create PR with detailed description of changes

---

## Acceptance Criteria for Gap Analysis

This gap analysis is complete when:
1. ✅ All missing files identified
2. ✅ All type/interface misalignments documented
3. ✅ All namespace hierarchy issues detailed
4. ✅ All enum/constant value discrepancies noted
5. ✅ Test coverage gaps enumerated with exact test counts
6. ✅ Configuration requirements specified
7. ✅ Cross-task dependencies identified
8. ✅ Clear remediation strategy provided in order of implementation

---

## Summary Statistics

| Metric | Count | Status |
|--------|-------|--------|
| **Classes to Create/Move** | 15+ | Identified |
| **Interfaces to Create/Refactor** | 5 | Identified |
| **Tests to Create** | 50+ | Identified |
| **Namespaces to Restructure** | 4 | Identified |
| **Enum Values to Fix** | 3 | Identified |
| **File Moves Required** | 4+ | Identified |
| **Performance Requirements** | 3 | Identified |
| **Error Codes to Verify** | 15 | Identified |
| **Secret Patterns to Detect** | 6+ | Identified |

**Total Implementation Work**: ~2000-2500 lines of code + 1200+ lines of tests + configuration + documentation

---

## References

- **Spec File**: docs/tasks/refined-tasks/Epic 01/task-007b-validator-errors-model-retry-contract.md
- **Implementation Prompt**: Lines 3118-4141
- **Testing Requirements**: Lines 1571-3117
- **Acceptance Criteria**: Lines 1415-1570
- **User Verification Steps**: Lines 2749-3115
- **CLAUDE.md Section 3.2**: Gap Analysis methodology (mandatory)
- **Task-049d Completion Checklist**: Reference example for checklist format
