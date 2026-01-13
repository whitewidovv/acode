# Task 004b - Gap Analysis and Implementation Checklist

## INSTRUCTIONS FOR FRESH AGENT

If you're resuming this task:
1. Read this checklist to understand what's been completed
2. Look for the first [ ] (incomplete) item
3. Continue from there following TDD: RED → GREEN → REFACTOR
4. Mark items [🔄] when starting, [✅] when done
5. Commit after each gap is complete
6. Update Evidence section with test output when done

## WHAT EXISTS (Already Complete)

Based on thorough verification of the codebase:

✅ **FinishReason.cs** - Complete enum with JSON converter
  - File: `src/Acode.Domain/Models/Inference/FinishReason.cs`
  - Has Stop, Length, ToolCalls, ContentFilter, Error, Cancelled values
  - Serializes to lowercase snake_case strings
  - FR-004b-019 to FR-004b-029 complete
  - Tests: `tests/Acode.Domain.Tests/Models/Inference/FinishReasonTests.cs`

✅ **UsageInfo.cs** - Complete record with validation
  - File: `src/Acode.Domain/Models/Inference/UsageInfo.cs`
  - Has PromptTokens, CompletionTokens, CachedTokens, ReasoningTokens
  - TotalTokens computed property
  - Add method for combining usage
  - ToString implementation
  - FR-004b-030 to FR-004b-041 complete
  - Tests: `tests/Acode.Domain.Tests/Models/Inference/UsageInfoTests.cs`

✅ **ResponseMetadata.cs** - Complete record (needs one addition)
  - File: `src/Acode.Domain/Models/Inference/ResponseMetadata.cs`
  - Has ProviderId, ModelId, RequestDuration, TimeToFirstToken, Extensions
  - Validation for non-empty ProviderId/ModelId, non-negative duration
  - FR-004b-042 to FR-004b-053 mostly complete (missing CompletionTokenCount for TokensPerSecond)
  - Tests: `tests/Acode.Domain.Tests/Models/Inference/ResponseMetadataTests.cs`

✅ **ResponseDelta.cs** - Record exists (needs ToolCallDelta type fix)
  - File: `src/Acode.Domain/Models/Inference/ResponseDelta.cs`
  - Has Index, ContentDelta, ToolCallDelta (string), FinishReason, Usage
  - IsComplete computed property
  - FR-004b-056 to FR-004b-063 mostly complete (ToolCallDelta should be ToolCallDelta type, not string)
  - Tests: `tests/Acode.Domain.Tests/Models/Inference/ResponseDeltaTests.cs`

✅ **ChatResponse.cs** - Record exists (needs additions)
  - File: `src/Acode.Domain/Models/Inference/ChatResponse.cs`
  - Has Id, Message, FinishReason, Usage, Metadata, Created, Model, Refusal
  - IsComplete, IsTruncated, HasToolCalls computed properties
  - Validation for non-empty Id, non-null Message, valid FinishReason
  - FR-004b-001 to FR-004b-018 mostly complete (missing ContentFilterResults property and factory methods)
  - Tests: `tests/Acode.Domain.Tests/Models/Inference/ChatResponseTests.cs`

✅ **DeltaAccumulator.cs** - Complete class in Application layer
  - File: `src/Acode.Application/Inference/DeltaAccumulator.cs`
  - Mutable accumulator with Append, Build, Current, DeltaCount
  - Thread-safe with lock
  - StringBuilder for efficient concatenation
  - FR-004b-066 to FR-004b-077 complete
  - Tests: `tests/Acode.Application.Tests/Inference/DeltaAccumulatorTests.cs`

## GAPS IDENTIFIED (What's Missing or Incomplete)

### Gap #1: ResponseMetadata Missing CompletionTokenCount
**Status**: [ ]
**File to Fix**: `src/Acode.Domain/Models/Inference/ResponseMetadata.cs`
**Why Needed**: FR-004b-047 requires TokensPerSecond computed property, which needs CompletionTokenCount
**Current State**:
  - Line 72-73: TokensPerSecond returns null with comment "Will be computed in ChatResponse with Usage data"
  - Should be computed directly in ResponseMetadata if given CompletionTokenCount
**Required Changes**:
  1. Add optional `int? CompletionTokenCount` property to constructor and init
  2. Update TokensPerSecond computation: `CompletionTokenCount / RequestDuration.TotalSeconds` when both available
  3. Return 0 when duration is zero (avoid divide by zero)
  4. Update tests to verify TokensPerSecond computation
**Implementation Pattern**:
```csharp
public sealed record ResponseMetadata(
    string ProviderId,
    string ModelId,
    TimeSpan RequestDuration,
    TimeSpan? TimeToFirstToken = null,
    int? CompletionTokenCount = null,  // ADD THIS
    IReadOnlyDictionary<string, JsonElement>? Extensions = null)
{
    // ... existing properties ...

    [JsonPropertyName("tokensPerSecond")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TokensPerSecond =>
        CompletionTokenCount.HasValue && RequestDuration.TotalSeconds > 0
            ? CompletionTokenCount.Value / RequestDuration.TotalSeconds
            : null;
}
```
**Success Criteria**:
  - CompletionTokenCount property exists
  - TokensPerSecond computed correctly
  - Tests verify computation with various inputs
**TDD Approach**:
  1. RED: Add tests for TokensPerSecond computation in ResponseMetadataTests.cs
  2. GREEN: Implement CompletionTokenCount property and TokensPerSecond computation
  3. REFACTOR: Ensure clean code
**Evidence**: [To be filled when complete]

---

### Gap #2: ResponseDelta ToolCallDelta Should Be Typed
**Status**: [ ]
**File to Fix**: `src/Acode.Domain/Models/Inference/ResponseDelta.cs`
**Why Needed**: FR-004b-059 requires ToolCallDelta property of type ToolCallDelta (from 004.a)
**Current State**:
  - Line 62: `public string? ToolCallDelta { get; init; }` - uses string instead of ToolCallDelta type
  - Comment at line 59: "Simplified to string for now - full ToolCallDelta type would be more complex"
**Required Changes**:
  1. Change ToolCallDelta property type from `string?` to `ToolCallDelta?`
  2. Update validation in constructor to handle ToolCallDelta type
  3. Update ResponseDeltaTests to use ToolCallDelta type
  4. Update DeltaAccumulator to handle ToolCallDelta type (merge by Index into complete ToolCalls)
**Implementation Pattern**:
```csharp
[JsonPropertyName("toolCallDelta")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public ToolCallDelta? ToolCallDelta { get; init; }
```
**Success Criteria**:
  - ToolCallDelta property uses correct type
  - DeltaAccumulator merges tool calls correctly
  - All tests pass
**TDD Approach**:
  1. RED: Update ResponseDeltaTests to expect ToolCallDelta type (tests fail)
  2. GREEN: Change property type and update DeltaAccumulator
  3. REFACTOR: Ensure clean code
**Evidence**: [To be filled when complete]

---

### Gap #3: ChatResponse Missing ContentFilterResults Property
**Status**: [ ]
**File to Fix**: `src/Acode.Domain/Models/Inference/ChatResponse.cs`
**Why Needed**: FR-004b-094 requires optional ContentFilterResults property
**Required Implementation**:
```csharp
/// <summary>
/// Gets the content moderation results (optional).
/// </summary>
/// <remarks>
/// FR-004b-094: ChatResponse MUST include optional ContentFilterResults (IReadOnlyList&lt;ContentFilterResult&gt;?).
/// </remarks>
[JsonPropertyName("contentFilterResults")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public IReadOnlyList<ContentFilterResult>? ContentFilterResults { get; init; }
```
**Success Criteria**:
  - Property exists on ChatResponse
  - Serializes correctly (null omitted)
  - Tests verify property works
**TDD Approach**:
  1. RED: Add tests for ContentFilterResults in ChatResponseTests.cs
  2. GREEN: Add property to ChatResponse
  3. REFACTOR: Ensure clean code
**Evidence**: [To be filled when complete]

---

### Gap #4: ChatResponse Missing Factory Methods
**Status**: [ ]
**File to Fix**: `src/Acode.Domain/Models/Inference/ChatResponse.cs`
**Why Needed**: FR-004b-110 to FR-004b-115 require factory methods for common response types
**Required Factory Methods**:
```csharp
// FR-004b-110: Success factory
public static ChatResponse Success(
    ChatMessage message,
    UsageInfo usage,
    string model,
    ResponseMetadata? metadata = null) => new()
{
    Id = Guid.NewGuid().ToString(),
    Message = message,
    FinishReason = FinishReason.Stop,
    Usage = usage,
    Metadata = metadata ?? CreateDefaultMetadata(model, usage.CompletionTokens),
    Created = DateTimeOffset.UtcNow,
    Model = model
};

// FR-004b-111: Truncated factory
public static ChatResponse Truncated(
    ChatMessage message,
    UsageInfo usage,
    string model,
    ResponseMetadata? metadata = null) => new()
{
    Id = Guid.NewGuid().ToString(),
    Message = message,
    FinishReason = FinishReason.Length,
    Usage = usage,
    Metadata = metadata ?? CreateDefaultMetadata(model, usage.CompletionTokens),
    Created = DateTimeOffset.UtcNow,
    Model = model
};

// FR-004b-112: ToolCallsRequired factory
public static ChatResponse ToolCallsRequired(
    ChatMessage message,
    UsageInfo usage,
    string model,
    ResponseMetadata? metadata = null) => new()
{
    Id = Guid.NewGuid().ToString(),
    Message = message,
    FinishReason = FinishReason.ToolCalls,
    Usage = usage,
    Metadata = metadata ?? CreateDefaultMetadata(model, usage.CompletionTokens),
    Created = DateTimeOffset.UtcNow,
    Model = model
};

// FR-004b-113: Refused factory
public static ChatResponse Refused(
    string refusalMessage,
    UsageInfo usage,
    string model,
    ResponseMetadata? metadata = null) => new()
{
    Id = Guid.NewGuid().ToString(),
    Message = ChatMessage.CreateAssistant(""),
    FinishReason = FinishReason.Stop,
    Usage = usage,
    Metadata = metadata ?? CreateDefaultMetadata(model, usage.CompletionTokens),
    Created = DateTimeOffset.UtcNow,
    Model = model,
    Refusal = refusalMessage
};

// FR-004b-114: Error factory
public static ChatResponse Error(
    string errorMessage,
    string model,
    ResponseMetadata? metadata = null) => new()
{
    Id = Guid.NewGuid().ToString(),
    Message = ChatMessage.CreateAssistant(""),
    FinishReason = FinishReason.Error,
    Usage = UsageInfo.Empty,
    Metadata = metadata ?? CreateDefaultMetadata(model, 0),
    Created = DateTimeOffset.UtcNow,
    Model = model,
    Refusal = errorMessage
};

// FR-004b-115: FromDelta factory
public static ChatResponse FromDelta(
    DeltaAccumulator accumulator,
    string model,
    ResponseMetadata metadata) => accumulator.Build() with
{
    Model = model,
    Metadata = metadata
};

private static ResponseMetadata CreateDefaultMetadata(string model, int completionTokens) => new(
    ProviderId: "unknown",
    ModelId: model,
    RequestDuration: TimeSpan.Zero,
    TimeToFirstToken: null,
    CompletionTokenCount: completionTokens,
    Extensions: null
);
```
**Success Criteria**:
  - All 6 factory methods exist
  - Each has correct FinishReason
  - Auto-generates Id and Created timestamp
  - Tests verify each factory method
**TDD Approach**:
  1. RED: Add tests for all factory methods
  2. GREEN: Implement factory methods
  3. REFACTOR: Extract common patterns
**Evidence**: [To be filled when complete]

---

### Gap #5: ContentFilterResult Record Missing
**Status**: [ ]
**File to Create**: `src/Acode.Domain/Models/Inference/ContentFilterResult.cs`
**Why Needed**: FR-004b-089 to FR-004b-095 require ContentFilterResult type
**Required Implementation**:
```csharp
namespace Acode.Domain.Models.Inference;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a content moderation result.
/// </summary>
/// <remarks>
/// FR-004b-089 to FR-004b-095: Content filter type for moderation results.
/// </remarks>
public sealed record ContentFilterResult
{
    /// <summary>
    /// Gets the content category that was evaluated.
    /// </summary>
    /// <remarks>
    /// FR-004b-090: ContentFilterResult MUST include Category property.
    /// </remarks>
    [JsonPropertyName("category")]
    public required FilterCategory Category { get; init; }

    /// <summary>
    /// Gets the severity level of the content.
    /// </summary>
    /// <remarks>
    /// FR-004b-091: ContentFilterResult MUST include Severity property.
    /// </remarks>
    [JsonPropertyName("severity")]
    public required FilterSeverity Severity { get; init; }

    /// <summary>
    /// Gets a value indicating whether the content was blocked/filtered.
    /// </summary>
    /// <remarks>
    /// FR-004b-092: ContentFilterResult MUST include Filtered property.
    /// </remarks>
    [JsonPropertyName("filtered")]
    public required bool Filtered { get; init; }

    /// <summary>
    /// Gets the optional explanation of why content was filtered.
    /// </summary>
    /// <remarks>
    /// FR-004b-093: ContentFilterResult MUST include optional Reason property.
    /// </remarks>
    [JsonPropertyName("reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; init; }
}

/// <summary>
/// Content moderation categories.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FilterCategory>))]
public enum FilterCategory
{
    Sexual = 0,
    Violence = 1,
    Hate = 2,
    SelfHarm = 3
}

/// <summary>
/// Content filter severity levels.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FilterSeverity>))]
public enum FilterSeverity
{
    Safe = 0,
    Low = 1,
    Medium = 2,
    High = 3
}
```
**Success Criteria**:
  - File exists at correct path
  - All properties present
  - Enums serialize correctly
  - Tests verify behavior
**TDD Approach**:
  1. RED: Write ContentFilterResultTests.cs first
  2. GREEN: Create ContentFilterResult.cs
  3. REFACTOR: Ensure clean code
**Evidence**: [To be filled when complete]

---

### Gap #6: ResponseBuilder Class Missing
**Status**: [ ]
**File to Create**: `src/Acode.Domain/Models/Inference/ResponseBuilder.cs`
**Why Needed**: FR-004b-078 to FR-004b-088 require ResponseBuilder for fluent API
**Required Implementation**:
```csharp
namespace Acode.Domain.Models.Inference;

using System;

/// <summary>
/// Fluent builder for constructing ChatResponse instances.
/// </summary>
/// <remarks>
/// FR-004b-078 to FR-004b-088: Fluent API for building responses.
/// </remarks>
public sealed class ResponseBuilder
{
    private string? id;
    private ChatMessage? message;
    private FinishReason? finishReason;
    private UsageInfo? usage;
    private ResponseMetadata? metadata;
    private string? model;
    private string? refusal;
    private IReadOnlyList<ContentFilterResult>? contentFilterResults;

    /// <summary>
    /// Sets the response ID.
    /// </summary>
    /// <remarks>
    /// FR-004b-079: ResponseBuilder MUST have WithId(string) method.
    /// </remarks>
    public ResponseBuilder WithId(string id)
    {
        this.id = id;
        return this;
    }

    /// <summary>
    /// Sets the message.
    /// </summary>
    /// <remarks>
    /// FR-004b-080: ResponseBuilder MUST have WithMessage(ChatMessage) method.
    /// </remarks>
    public ResponseBuilder WithMessage(ChatMessage message)
    {
        this.message = message;
        return this;
    }

    /// <summary>
    /// Sets the finish reason.
    /// </summary>
    /// <remarks>
    /// FR-004b-081: ResponseBuilder MUST have WithFinishReason(FinishReason) method.
    /// </remarks>
    public ResponseBuilder WithFinishReason(FinishReason finishReason)
    {
        this.finishReason = finishReason;
        return this;
    }

    /// <summary>
    /// Sets the usage information.
    /// </summary>
    /// <remarks>
    /// FR-004b-082: ResponseBuilder MUST have WithUsage(UsageInfo) method.
    /// </remarks>
    public ResponseBuilder WithUsage(UsageInfo usage)
    {
        this.usage = usage;
        return this;
    }

    /// <summary>
    /// Sets the metadata.
    /// </summary>
    /// <remarks>
    /// FR-004b-083: ResponseBuilder MUST have WithMetadata(ResponseMetadata) method.
    /// </remarks>
    public ResponseBuilder WithMetadata(ResponseMetadata metadata)
    {
        this.metadata = metadata;
        return this;
    }

    /// <summary>
    /// Sets the model identifier.
    /// </summary>
    public ResponseBuilder WithModel(string model)
    {
        this.model = model;
        return this;
    }

    /// <summary>
    /// Sets the refusal message.
    /// </summary>
    /// <remarks>
    /// FR-004b-084: ResponseBuilder MUST have WithRefusal(string?) method.
    /// </remarks>
    public ResponseBuilder WithRefusal(string? refusal)
    {
        this.refusal = refusal;
        return this;
    }

    /// <summary>
    /// Sets the content filter results.
    /// </summary>
    public ResponseBuilder WithContentFilterResults(IReadOnlyList<ContentFilterResult>? results)
    {
        this.contentFilterResults = results;
        return this;
    }

    /// <summary>
    /// Builds the ChatResponse with validation.
    /// </summary>
    /// <returns>A validated ChatResponse.</returns>
    /// <exception cref="InvalidOperationException">Thrown if required fields not set.</exception>
    /// <remarks>
    /// FR-004b-085: ResponseBuilder MUST have Build() method returning validated ChatResponse.
    /// FR-004b-086: ResponseBuilder MUST generate Id if not explicitly provided (GUID).
    /// FR-004b-087: ResponseBuilder MUST set Created timestamp automatically.
    /// FR-004b-088: ResponseBuilder MUST validate required fields on Build().
    /// </remarks>
    public ChatResponse Build()
    {
        // Validation
        if (this.message is null)
        {
            throw new InvalidOperationException("Message is required.");
        }

        if (!this.finishReason.HasValue)
        {
            throw new InvalidOperationException("FinishReason is required.");
        }

        if (this.usage is null)
        {
            throw new InvalidOperationException("Usage is required.");
        }

        if (this.metadata is null)
        {
            throw new InvalidOperationException("Metadata is required.");
        }

        if (string.IsNullOrWhiteSpace(this.model))
        {
            throw new InvalidOperationException("Model is required.");
        }

        // Auto-generate Id if not set
        var responseId = string.IsNullOrWhiteSpace(this.id)
            ? Guid.NewGuid().ToString()
            : this.id;

        // Create response with auto-generated Created timestamp
        return new ChatResponse(
            Id: responseId,
            Message: this.message,
            FinishReason: this.finishReason.Value,
            Usage: this.usage,
            Metadata: this.metadata,
            Created: DateTimeOffset.UtcNow,
            Model: this.model,
            Refusal: this.refusal)
        {
            ContentFilterResults = this.contentFilterResults
        };
    }
}
```
**Success Criteria**:
  - Fluent API works
  - Auto-generates Id when not set
  - Auto-sets Created timestamp
  - Validates required fields
  - Tests verify all scenarios
**TDD Approach**:
  1. RED: Write ResponseBuilderTests.cs first
  2. GREEN: Create ResponseBuilder.cs
  3. REFACTOR: Ensure clean fluent API
**Evidence**: [To be filled when complete]

---

### Gap #7: ResponseJsonContext Source Generator Missing
**Status**: [ ]
**File to Create**: `src/Acode.Domain/Models/Inference/Serialization/ResponseJsonContext.cs`
**Why Needed**: FR-004b-101, FR-004b-109, NFR-004b-06/07 require System.Text.Json source generators for performance
**Required Implementation**:
```csharp
namespace Acode.Domain.Models.Inference.Serialization;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Acode.Domain.Models.Inference;

[JsonSerializable(typeof(ChatResponse))]
[JsonSerializable(typeof(FinishReason))]
[JsonSerializable(typeof(UsageInfo))]
[JsonSerializable(typeof(ResponseMetadata))]
[JsonSerializable(typeof(ResponseDelta))]
[JsonSerializable(typeof(ContentFilterResult))]
[JsonSerializable(typeof(FilterCategory))]
[JsonSerializable(typeof(FilterSeverity))]
[JsonSerializable(typeof(List<ChatResponse>))]
[JsonSerializable(typeof(List<ContentFilterResult>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
internal partial class ResponseJsonContext : JsonSerializerContext
{
}
```
**Success Criteria**:
  - Source generator context exists
  - All response types registered
  - CamelCase naming configured
  - Null omission configured
  - Can be used for serialization
**Evidence**: [To be filled when complete]

---

### Gap #8: ResponseMetadataTests Missing Tests from Spec
**Status**: [ ]
**File to Update**: `tests/Acode.Domain.Tests/Models/Inference/ResponseMetadataTests.cs`
**Why Needed**: Testing Requirements section lines 800-1082 specify comprehensive tests
**Required Tests** (from spec):
  - Should_Require_ProviderId
  - Should_Require_ModelId
  - Should_Validate_NonNegative_Duration
  - Should_Compute_TokensPerSecond
  - Should_Handle_Zero_Duration
  - Should_Handle_Zero_Tokens
  - Should_Allow_Null_TTFT
  - Should_Accept_Valid_TTFT
  - Should_Preserve_Extensions
  - Should_Default_Empty_Extensions
  - Extensions_Should_Be_ReadOnly
  - Should_Be_Immutable
  - Should_Serialize_To_Json
**Success Criteria**:
  - All tests from spec implemented
  - All tests pass
  - Coverage for validation, computation, extensions
**TDD Approach**:
  1. Read existing tests
  2. Add missing tests from spec
  3. Verify all pass
**Evidence**: [To be filled when complete]

---

### Gap #9: ResponseDeltaTests Missing Tests from Spec
**Status**: [ ]
**File to Update**: `tests/Acode.Domain.Tests/Models/Inference/ResponseDeltaTests.cs`
**Why Needed**: Testing Requirements section lines 1085-1319 specify comprehensive tests
**Required Tests** (from spec):
  - Should_Require_Content_Or_Complete
  - Should_Allow_ContentDelta_Only
  - Should_Allow_FinishReason_Only
  - Should_Compute_IsComplete_True
  - Should_Compute_IsComplete_False
  - Should_Be_Complete_For_Any_FinishReason
  - Should_Allow_ToolCallDelta
  - Should_Allow_Both_Content_And_ToolCall
  - Should_Have_Index
  - Should_Accept_Valid_Index_Values
  - Should_Include_Usage_On_Final_Delta
  - Usage_Should_Be_Null_For_Intermediate_Deltas
**Success Criteria**:
  - All tests from spec implemented
  - All tests pass
  - Coverage for validation, IsComplete, ToolCallDelta, Usage
**TDD Approach**:
  1. Read existing tests
  2. Add missing tests from spec
  3. Verify all pass
**Evidence**: [To be filled when complete]

---

### Gap #10: DeltaAccumulatorTests Missing Tests from Spec
**Status**: [ ]
**File to Update**: `tests/Acode.Application.Tests/Inference/DeltaAccumulatorTests.cs`
**Why Needed**: Testing Requirements section lines 1323-1661 specify comprehensive tests
**Required Tests** (from spec):
  - Should_Concatenate_Content
  - Should_Handle_Empty_ContentDeltas
  - Should_Handle_Unicode_Content
  - Should_Merge_ToolCalls_By_Index
  - Should_Handle_Multiple_ToolCalls
  - Should_Capture_FinishReason
  - Should_Capture_All_FinishReason_Types
  - Should_Capture_Usage
  - Build_Should_Return_Response
  - Build_Should_Throw_If_Incomplete
  - Build_Should_Generate_Response_Id
  - Current_Should_Return_Partial
  - Current_Should_Track_Delta_Count
  - Should_Be_ThreadSafe
**Success Criteria**:
  - All tests from spec implemented
  - All tests pass
  - Coverage for content concatenation, tool call merging, build validation, thread safety
**TDD Approach**:
  1. Read existing tests
  2. Add missing tests from spec
  3. Verify all pass
**Evidence**: [To be filled when complete]

---

### Gap #11: ResponseBuilderTests File Missing
**Status**: [ ]
**File to Create**: `tests/Acode.Domain.Tests/Models/Inference/ResponseBuilderTests.cs`
**Why Needed**: Testing Requirements section lines 1665-1921 specify comprehensive tests
**Required Tests** (from spec):
  - Should_Build_Valid_Response
  - Should_AutoGenerate_Id
  - Should_Use_Provided_Id
  - Should_AutoSet_Created
  - Should_Validate_Required_Message
  - Should_Validate_Required_Usage
  - Should_Validate_Required_Metadata
  - Should_Support_Fluent_API
  - Should_Support_Refusal
  - Should_Support_ContentFilterResults
  - Should_Allow_Reuse_After_Build
**Implementation Pattern**: See spec lines 1676-1921 for complete test code
**Success Criteria**:
  - All tests implemented
  - All tests pass
  - Coverage for fluent API, validation, auto-generation
**TDD Approach**:
  1. Create test file (before Gap #6 implementation)
  2. Tests fail (RED)
  3. Implement Gap #6, tests pass (GREEN)
**Evidence**: [To be filled when complete]

---

### Gap #12: ContentFilterResultTests File Missing
**Status**: [ ]
**File to Create**: `tests/Acode.Domain.Tests/Models/Inference/ContentFilterResultTests.cs`
**Why Needed**: Testing Requirements section lines 1925-2081 specify comprehensive tests
**Required Tests** (from spec):
  - Should_Have_Category (parameterized for all categories)
  - Should_Have_Severity (parameterized for all severities)
  - Should_Have_Filtered_Flag
  - Should_Support_Optional_Reason
  - Should_Serialize_To_Json
  - Should_Deserialize_From_Json
**Implementation Pattern**: See spec lines 1935-2081 for complete test code
**Success Criteria**:
  - All tests implemented
  - All tests pass
  - Coverage for enums, serialization, deserialization
**TDD Approach**:
  1. Create test file (before Gap #5 implementation)
  2. Tests fail (RED)
  3. Implement Gap #5, tests pass (GREEN)
**Evidence**: [To be filled when complete]

---

### Gap #13: ResponseSerializationTests Integration Tests Missing
**Status**: [ ]
**File to Create**: `tests/Acode.Integration.Tests/Models/Responses/ResponseSerializationTests.cs`
**Why Needed**: Testing Requirements section lines 2086-2264 specify integration tests for provider compatibility
**Required Tests** (from spec):
  - Should_Match_Ollama_Format
  - Should_Match_Vllm_Format
  - Should_Handle_Extensions
  - Should_Roundtrip_All_Types
**Implementation Pattern**: See spec lines 2097-2264 for complete test code
**Purpose**: Verify JSON serialization matches Ollama and vLLM API formats
**Success Criteria**:
  - Integration test file exists
  - All 4 tests implemented
  - Tests verify provider format compatibility
  - Tests verify roundtrip serialization
**TDD Approach**:
  1. Create integration test file
  2. Run tests to verify serialization compatibility
  3. Fix any issues found
**Evidence**: [To be filled when complete]

---

## Implementation Order (TDD)

Follow this order for Test-Driven Development:

1. **Gap #5** (ContentFilterResult) → **Gap #12** (ContentFilterResultTests) - NEW TYPES
2. **Gap #6** (ResponseBuilder) → **Gap #11** (ResponseBuilderTests) - NEW CLASS
3. **Gap #1** (ResponseMetadata.CompletionTokenCount) - UPDATE EXISTING
4. **Gap #8** (ResponseMetadataTests) - VERIFY COVERAGE
5. **Gap #2** (ResponseDelta.ToolCallDelta type) - UPDATE EXISTING
6. **Gap #9** (ResponseDeltaTests) - VERIFY COVERAGE
7. **Gap #3** (ChatResponse.ContentFilterResults) - UPDATE EXISTING
8. **Gap #4** (ChatResponse factory methods) - ADD METHODS
9. **Gap #10** (DeltaAccumulatorTests) - VERIFY COVERAGE
10. **Gap #7** (ResponseJsonContext) - INFRASTRUCTURE
11. **Gap #13** (Integration Tests) - VERIFICATION

## Commit Strategy

After completing each gap:
1. Run all tests: `dotnet test`
2. Verify build: `dotnet build`
3. Commit with message: `feat(task-004b): complete gap #N - [description]`
4. Push to feature branch
5. Update this checklist with ✅ and evidence

## Audit Checklist Reference

Before creating PR, verify (from docs/AUDIT-GUIDELINES.md):
- [ ] All gaps completed
- [ ] All tests pass
- [ ] Build has no errors or warnings
- [ ] XML documentation complete
- [ ] Layer boundaries respected (Domain layer for models, Application for DeltaAccumulator)
- [ ] No NotImplementedException or TODO comments
- [ ] Thread safety verified for DeltaAccumulator
- [ ] Immutability verified for all records
- [ ] Factory methods on ChatResponse tested
- [ ] JSON serialization verified with integration tests

## Notes

- Task 004b is P0 (Critical) priority
- This is Core Infrastructure tier work
- Dependencies: Task 004 (IModelProvider interface), Task 004a (Message types)
- Integration points: Task 004c (Provider Registry), Task 005 (Ollama), Task 006 (vLLM)
- DeltaAccumulator already exists in Application layer - verify tests complete
- ResponseMetadata.TokensPerSecond needs CompletionTokenCount to compute properly
- ResponseDelta.ToolCallDelta should use ToolCallDelta type from 004a (currently string)
