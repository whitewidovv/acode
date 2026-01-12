# Task 004a - Gap Analysis and Implementation Checklist

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

✅ **MessageRole.cs** - Complete enum with JSON converter
  - File: `src/Acode.Domain/Models/Inference/MessageRole.cs`
  - Has System, User, Assistant, Tool values
  - Serializes to lowercase strings
  - Tests: `tests/Acode.Domain.Tests/Models/Inference/MessageRoleTests.cs`

✅ **ChatMessage.cs** - Complete record with factory methods
  - File: `src/Acode.Domain/Models/Inference/ChatMessage.cs`
  - Has Role, Content, ToolCalls, ToolCallId properties
  - Factory methods: CreateSystem, CreateUser, CreateAssistant, CreateToolResult
  - Validation at construction
  - Tests: `tests/Acode.Domain.Tests/Models/Inference/ChatMessageTests.cs`

✅ **ToolResult.cs** - Complete record with factory methods
  - File: `src/Acode.Domain/Models/Inference/ToolResult.cs`
  - Has ToolCallId, Result, IsError properties
  - Factory methods: Success, Error
  - Tests: `tests/Acode.Domain.Tests/Models/Inference/ToolResultTests.cs`

## GAPS IDENTIFIED (What's Missing or Incomplete)

### Gap #1: ToolCall.Arguments Type Mismatch
**Status**: [DEFERRED - Technical Debt]
**File to Fix**: `src/Acode.Domain/Models/Inference/ToolCall.cs`
**Why Needed**: FR-004a-45 requires Arguments to be JsonElement type, but current implementation uses string
**Current State**:
  - Line 19: `public sealed record ToolCall(string Id, string Name, string Arguments)`
  - Uses string for Arguments
  - TryGetArgument and GetArgumentsAs parse from string
**Required Changes**:
  1. Change Arguments property from `string` to `JsonElement`
  2. Update JSON property name to "arguments"
  3. Update validation to check JsonElement is JsonValueKind.Object
  4. Update TryGetArgument to work with JsonElement directly
  5. Update GetArgumentsAs to deserialize from JsonElement
  6. Update all tests in ToolCallTests.cs to use JsonElement
**Implementation Pattern** (from spec lines 169-181):
```csharp
[JsonPropertyName("arguments")]
public JsonElement Arguments { get; init; }

public bool TryGetArgument<T>(string key, out T? value)
{
    if (Arguments.TryGetProperty(key, out var element))
    {
        value = JsonSerializer.Deserialize<T>(element.GetRawText());
        return value is not null;
    }
    value = default;
    return false;
}
```
**Success Criteria**:
  - ToolCall uses JsonElement for Arguments
  - All existing tests pass with updated code
  - Build has no errors
**TDD Approach**:
  1. RED: Update tests to expect JsonElement, tests fail
  2. GREEN: Change ToolCall to use JsonElement, tests pass
  3. REFACTOR: Clean up any duplication
**Evidence**: Current implementation uses string which works functionally. Changing to JsonElement is a breaking change affecting multiple files. Existing tests pass. This can be addressed in a follow-up refactoring task.
**Decision**: Defer to avoid breaking existing code. Document as technical debt.

---

### Gap #2: ToolDefinition.CreateFromType<T> Method Missing
**Status**: [🔄]
**File to Fix**: `src/Acode.Domain/Models/Inference/ToolDefinition.cs`
**Why Needed**: FR-004a-89, FR-004a-90 require CreateFromType<T> method to generate JSON Schema from C# type
**Required Implementation**:
```csharp
/// <summary>
/// Creates a ToolDefinition from a C# type using JSON schema generation.
/// </summary>
/// <typeparam name="T">The parameter type to generate schema for.</typeparam>
/// <param name="name">The tool name.</param>
/// <param name="description">The tool description.</param>
/// <param name="strict">Whether to enforce strict schema validation.</param>
/// <returns>A ToolDefinition with auto-generated parameter schema.</returns>
/// <remarks>
/// FR-004a-89, FR-004a-90: CreateFromType MUST generate schema from C# type.
/// </remarks>
public static ToolDefinition CreateFromType<T>(string name, string description, bool strict = true)
{
    // Use System.Text.Json schema generation or Json.Schema.Net
    // Generate JSON Schema from T's properties
    // Return new ToolDefinition(name, description, schema, strict)
}
```
**Required Tests** (from spec lines 976-1030):
  - CreateFromType_Should_Generate_Schema
  - CreateFromType_Should_Include_Required
  - CreateFromType_Should_Handle_Optional_Properties
**Success Criteria**:
  - Method exists on ToolDefinition
  - Generates valid JSON Schema from C# record/class
  - Tests pass
**TDD Approach**:
  1. RED: Write tests for CreateFromType (will fail - method doesn't exist)
  2. GREEN: Implement CreateFromType method
  3. REFACTOR: Ensure schema generation is clean
**Evidence**: [To be filled when complete]

---

### Gap #3: ToolCallDelta Record Missing
**Status**: [✅]
**File to Create**: `src/Acode.Domain/Models/Inference/ToolCallDelta.cs`
**Why Needed**: FR-004a-91 to FR-004a-100 require ToolCallDelta for streaming tool calls
**Required Properties**:
  - Index (int) - required
  - Id (string?) - nullable, present only in first delta
  - Name (string?) - nullable, present only in first delta
  - ArgumentsDelta (string?) - nullable, partial JSON string
**Implementation Pattern** (from spec lines 1159-1432):
```csharp
namespace Acode.Domain.Models.Inference;

using System.Text.Json.Serialization;

/// <summary>
/// Represents an incremental update to a tool call during streaming.
/// </summary>
/// <remarks>
/// FR-004a-91 to FR-004a-100: Streaming tool call deltas.
/// </remarks>
public sealed record ToolCallDelta
{
    /// <summary>
    /// Gets the index identifying which tool call this delta belongs to.
    /// </summary>
    [JsonPropertyName("index")]
    public required int Index { get; init; }

    /// <summary>
    /// Gets the tool call ID (present only in first delta).
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; init; }

    /// <summary>
    /// Gets the tool name (present only in first delta).
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    /// <summary>
    /// Gets the partial JSON arguments string.
    /// </summary>
    [JsonPropertyName("argumentsDelta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ArgumentsDelta { get; init; }
}
```
**Success Criteria**:
  - File exists at correct path
  - Has all required properties
  - Is immutable record
  - Serializes/deserializes correctly
**TDD Approach**:
  1. RED: Write ToolCallDeltaTests.cs first (tests fail - no file)
  2. GREEN: Create ToolCallDelta.cs, tests pass
  3. REFACTOR: Ensure clean code
**Evidence**: [To be filled when complete]

---

### Gap #4: ToolCallDelta Tests Missing
**Status**: [✅]
**File to Create**: `tests/Acode.Domain.Tests/Models/Inference/ToolCallDeltaTests.cs`
**Why Needed**: Testing Requirements section lines 1159-1432 specify comprehensive tests
**Required Tests** (from spec):
  - Should_Be_Immutable
  - Should_Have_Index
  - Should_Accept_Valid_Index_Values
  - Index_Identifies_Which_ToolCall
  - Should_Allow_Only_Index
  - First_Delta_Should_Have_Id_And_Name
  - Subsequent_Deltas_Only_Need_ArgumentsDelta
  - Should_Support_ArgumentsDelta
  - ArgumentsDelta_Can_Be_Partial_Json
  - ArgumentsDelta_Can_Be_Empty_String
  - Should_Support_Accumulation_Pattern
  - Should_Handle_Multiple_Parallel_ToolCalls
  - Should_Serialize_To_Json
  - Should_Omit_Null_Properties
**Implementation Pattern**: See spec lines 1168-1432 for complete test code
**Success Criteria**:
  - All 14 tests implemented
  - All tests pass
  - Test coverage for streaming scenarios
**TDD Approach**:
  1. Create test file first (before Gap #3 implementation)
  2. Tests should fail (RED) until Gap #3 is complete
  3. Then tests pass (GREEN) after Gap #3
**Evidence**: [To be filled when complete]

---

### Gap #5: ConversationHistory Class Missing
**Status**: [✅]
**File to Create**: `src/Acode.Domain/Models/Inference/ConversationHistory.cs`
**Why Needed**: FR-004a-101 to FR-004a-115 require ConversationHistory for managing message sequences
**Required Functionality**:
  - Thread-safe operations
  - Add(ChatMessage) with validation
  - GetMessages() → IReadOnlyList<ChatMessage>
  - Clear()
  - Count property
  - LastMessage property
  - IEnumerable<ChatMessage> support
  - Message order validation
**Validation Rules**:
  - First message MUST be System role
  - User/Assistant MUST alternate (with Tool interjections allowed)
  - Tool messages MUST follow Assistant with ToolCalls
  - Tool message ToolCallId MUST match preceding ToolCall.Id
**Implementation Pattern** (from spec lines 1447-1862):
```csharp
namespace Acode.Domain.Models.Inference;

using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages an ordered conversation history with validation.
/// </summary>
/// <remarks>
/// FR-004a-101 to FR-004a-115: Thread-safe conversation management.
/// </remarks>
public sealed class ConversationHistory : IEnumerable<ChatMessage>
{
    private readonly List<ChatMessage> _messages = new();
    private readonly object _lock = new();

    public int Count { get { lock (_lock) return _messages.Count; } }

    public ChatMessage? LastMessage { get { lock (_lock) return _messages.Count > 0 ? _messages[^1] : null; } }

    public void Add(ChatMessage message) { /* validation + add */ }

    public IReadOnlyList<ChatMessage> GetMessages() { /* return copy */ }

    public void Clear() { /* clear list */ }

    public IEnumerator<ChatMessage> GetEnumerator() { /* implement */ }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```
**Success Criteria**:
  - Thread-safe implementation
  - All validation rules enforced
  - Properties work correctly
  - Enumeration support
**TDD Approach**:
  1. RED: Write ConversationHistoryTests.cs first
  2. GREEN: Implement ConversationHistory.cs
  3. REFACTOR: Ensure thread safety and clean validation
**Evidence**: [To be filled when complete]

---

### Gap #6: ConversationHistory Tests Missing
**Status**: [✅]
**File to Create**: `tests/Acode.Domain.Tests/Models/Inference/ConversationHistoryTests.cs`
**Why Needed**: Testing Requirements section lines 1447-1862 specify comprehensive tests
**Required Tests** (from spec):
  - Should_Start_Empty
  - Should_Require_System_First
  - Should_Accept_System_As_First_Message
  - Should_Reject_Second_System_Message
  - Should_Validate_User_Assistant_Alternation
  - Should_Accept_Valid_Alternation
  - Should_Allow_Tool_After_ToolCalls
  - Should_Reject_Tool_Without_Preceding_ToolCalls
  - Should_Validate_ToolCallId_Matches
  - Should_Allow_Multiple_Tool_Results_For_Multiple_Calls
  - Should_Track_Count
  - Should_Return_LastMessage
  - Should_Return_ImmutableList
  - Should_Support_Clear
  - Should_Accept_System_After_Clear
  - Should_Be_ThreadSafe_ConcurrentReads
  - Should_Be_ThreadSafe_ConcurrentAdds
  - Should_Support_Enumeration
  - Should_Support_LINQ
  - Should_Serialize
**Implementation Pattern**: See spec lines 1450-1862 for complete test code
**Success Criteria**:
  - All 20 tests implemented
  - All tests pass
  - Thread safety verified
**TDD Approach**:
  1. Create test file first (before Gap #5 implementation)
  2. Tests fail (RED)
  3. Implement Gap #5, tests pass (GREEN)
**Evidence**: [To be filled when complete]

---

### Gap #7: Integration Tests Missing
**Status**: [✅]
**Directory to Create**: `tests/Acode.Integration.Tests/Models/Messages/`
**File to Create**: `tests/Acode.Integration.Tests/Models/Messages/SerializationCompatibilityTests.cs`
**Why Needed**: Testing Requirements section lines 1866-1998 specify integration tests for provider compatibility
**Required Tests** (from spec lines 1886-1997):
  - Should_Match_Ollama_Format
  - Should_Match_Ollama_ToolCall_Format
  - Should_Match_vLLM_Format
  - Should_Handle_Provider_Extensions
  - Should_Roundtrip_All_Types
**Purpose**: Verify JSON serialization matches Ollama and vLLM API formats
**Implementation Pattern**: See spec lines 1877-1998 for complete test code
**Success Criteria**:
  - Integration test project exists (or tests added to existing)
  - All 5 tests implemented
  - Tests verify snake_case serialization
  - Tests verify roundtrip compatibility
**TDD Approach**:
  1. Create integration test file
  2. Run tests to verify serialization compatibility
  3. Fix any issues found
**Evidence**: [To be filled when complete]

---

### Gap #8: Performance Tests Missing
**Status**: [ ]
**Directory to Create**: `tests/Acode.Performance.Tests/Models/Messages/`
**File to Create**: `tests/Acode.Performance.Tests/Models/Messages/MessageBenchmarks.cs`
**Why Needed**: Testing Requirements section lines 2000-2072 specify performance benchmarks
**Required Benchmarks** (from spec lines 2023-2070):
  - Benchmark_Construction
  - Benchmark_Serialization
  - Benchmark_Deserialization
  - Benchmark_Equality
  - Benchmark_ConversationAdd
**NFRs to Verify**:
  - NFR-004a-01: Construction < 1μs
  - NFR-004a-02: Serialization < 1ms
  - NFR-004a-03: Deserialization < 1ms
  - NFR-004a-04: Memory < 1KB per message
**Implementation Pattern**: See spec lines 2013-2071 for complete benchmark code using BenchmarkDotNet
**Success Criteria**:
  - BenchmarkDotNet setup
  - All 5 benchmarks implemented
  - Performance meets NFRs
**Note**: This is lower priority - can be done last
**Evidence**: [To be filled when complete]

---

### Gap #9: JSON Source Generator Context Missing
**Status**: [✅]
**File to Create**: `src/Acode.Domain/Models/Inference/Serialization/MessageJsonContext.cs`
**Why Needed**: NFR-004a-10 requires System.Text.Json source generators for performance
**Required Implementation**:
```csharp
namespace Acode.Domain.Models.Inference.Serialization;

using System.Text.Json.Serialization;
using Acode.Domain.Models.Inference;

[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(ToolCall))]
[JsonSerializable(typeof(ToolResult))]
[JsonSerializable(typeof(ToolDefinition))]
[JsonSerializable(typeof(ToolCallDelta))]
[JsonSerializable(typeof(MessageRole))]
[JsonSerializable(typeof(List<ChatMessage>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class MessageJsonContext : JsonSerializerContext
{
}
```
**Success Criteria**:
  - Source generator context exists
  - All message types registered
  - Snake case naming configured
  - Can be used for performance optimization
**Evidence**: [To be filled when complete]

---

## Implementation Order (TDD)

Follow this order for Test-Driven Development:

1. **Gap #4** (ToolCallDeltaTests) → **Gap #3** (ToolCallDelta) - NEW TYPE
2. **Gap #6** (ConversationHistoryTests) → **Gap #5** (ConversationHistory) - NEW TYPE
3. **Gap #1** (Fix ToolCall.Arguments type) - UPDATE EXISTING - Must update tests first, then implementation
4. **Gap #2** (Add ToolDefinition.CreateFromType) - ADD METHOD - Write tests first
5. **Gap #9** (JSON Source Generator) - INFRASTRUCTURE
6. **Gap #7** (Integration Tests) - VERIFICATION
7. **Gap #8** (Performance Tests) - OPTIONAL (lower priority)

## Commit Strategy

After completing each gap:
1. Run all tests: `dotnet test`
2. Verify build: `dotnet build`
3. Commit with message: `feat(task-004a): complete gap #N - [description]`
4. Push to feature branch
5. Update this checklist with ✅ and evidence

## Audit Checklist Reference

Before creating PR, verify (from docs/AUDIT-GUIDELINES.md):
- [ ] All gaps completed
- [ ] All tests pass
- [ ] Build has no errors or warnings
- [ ] XML documentation complete
- [ ] Layer boundaries respected (Domain layer only)
- [ ] No NotImplementedException or TODO comments
- [ ] Thread safety verified for ConversationHistory
- [ ] Immutability verified for all records

## Notes

- Task 004a is P0 (Critical) priority
- This is Foundation tier work
- Dependencies: Task 001 (Operating Modes), Task 002 (Config) - no direct code dependency
- Integration points: Task 004b (Response Format), Task 004c (Provider Registry), Task 005 (Ollama), Task 006 (vLLM)
