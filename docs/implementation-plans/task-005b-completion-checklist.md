# Task 005b - Tool Call Parsing + Retry-on-Invalid-JSON - Gap Analysis and Implementation Checklist

## INSTRUCTIONS FOR CONTINUATION

This checklist identifies ALL gaps between the spec requirements and current implementation for task-005b (Tool Call Parsing + Retry-on-Invalid-JSON). Task-005a (implemented by another agent) handles request/response serialization and streaming. Task-005b focuses ONLY on:
1. Tool call parsing from Ollama responses
2. JSON repair for malformed tool call arguments
3. Retry logic when parsing fails
4. Integration into OllamaProvider response mapping

**Current State**: Core parsing infrastructure EXISTS (ToolCallParser, JsonRepairer, models, exceptions, 18 tests) but is NOT integrated into the actual Ollama response flow. Retry logic EXISTS as models/exceptions but NOT implemented.

## WHAT EXISTS (Already Complete)

✅ **src/Acode.Infrastructure/Ollama/ToolCall/ToolCallParser.cs** (200 lines)
- Parses OllamaToolCall[] to domain ToolCall objects
- Validates function name format (alphanumeric + underscore, max 64 chars)
- Generates IDs for tool calls missing IDs
- Uses JsonRepairer for malformed arguments
- Returns ToolCallParseResult with parsed calls, errors, repairs

✅ **src/Acode.Infrastructure/Ollama/ToolCall/JsonRepairer.cs** (472 lines)
- Repairs common JSON errors: trailing commas, unbalanced braces/brackets, single quotes, unquoted keys, unclosed strings
- Deterministic and idempotent repairs
- Timeout protection (100ms default)
- Returns RepairResult with success status and repairs applied

✅ **Models**:
- RepairResult.cs - Result of JSON repair attempt
- RetryConfig.cs - Configuration for retry behavior (max attempts, delay, prompt template)
- ToolCallError.cs - Error details for failed parsing
- ToolCallParseResult.cs - Result of parsing with successes, errors, repairs
- OllamaToolCall.cs, OllamaFunction.cs - Ollama-specific types
- ToolCallDelta.cs - For streaming accumulation

✅ **Exceptions**:
- ToolCallParseException.cs - Parse failure exception
- ToolCallRetryExhaustedException.cs - All retries failed exception
- ToolCallValidationException.cs - Validation failure exception

✅ **Tests**:
- tests/Acode.Infrastructure.Tests/Ollama/ToolCall/ToolCallParserTests.cs (18 tests)
- tests/Acode.Infrastructure.Tests/Ollama/ToolCall/StreamingToolCallAccumulatorTests.cs

✅ **src/Acode.Infrastructure/Ollama/ToolCall/StreamingToolCallAccumulator.cs**
- Accumulates tool call deltas from streaming responses

## GAPS IDENTIFIED (What's Missing or Incomplete)

### Gap #1: Integration of ToolCallParser into OllamaResponseMapper
**Status**: [✅]
**File to Modify**: src/Acode.Infrastructure/Ollama/Mapping/OllamaResponseMapper.cs
**Why Needed**: FR-053, FR-054, FR-055 require tool call parsing in responses. Current OllamaResponseMapper doesn't handle tool calls at all - it only maps message content.
**Required Changes**:
1. Accept ToolCallParser as parameter or create instance
2. Check if ollamaResponse.Message.ToolCalls is present
3. Parse tool calls using ToolCallParser.Parse()
4. Attach parsed ToolCall[] to ChatMessage
5. Set FinishReason.ToolCalls when tool calls present (FR-054)
6. Support multiple simultaneous tool calls (FR-055)

**Implementation Pattern**:
```csharp
public static ChatResponse Map(OllamaResponse ollamaResponse, ToolCallParser? parser = null)
{
    // ... existing code ...

    ToolCall[]? toolCalls = null;
    if (ollamaResponse.Message?.ToolCalls != null && ollamaResponse.Message.ToolCalls.Length > 0)
    {
        var toolCallParser = parser ?? new ToolCallParser();
        var parseResult = toolCallParser.Parse(ollamaResponse.Message.ToolCalls);

        if (parseResult.AllSucceeded)
        {
            toolCalls = parseResult.ToolCalls.ToArray();
        }
        else if (!parseResult.Errors.Any())
        {
            toolCalls = parseResult.ToolCalls.ToArray(); // Partial success
        }
        // else: all failed, toolCalls remains null
    }

    // Update ChatMessage creation to include tool calls
    var message = toolCalls != null
        ? ChatMessage.CreateAssistantWithTools(ollamaMessage.Content ?? string.Empty, toolCalls)
        : ChatMessage.CreateAssistant(ollamaMessage.Content ?? string.Empty);

    // Update finish reason
    var finishReason = toolCalls != null && toolCalls.Length > 0
        ? FinishReason.ToolCalls
        : MapFinishReason(ollamaResponse.DoneReason);

    // ... rest of mapping ...
}
```

**Success Criteria**: OllamaResponseMapper correctly parses tool calls and sets FinishReason.ToolCalls when present
**Evidence**:
- Modified OllamaResponseMapper.Map() to accept optional ToolCallParser parameter
- Added ParseToolCalls() helper method that converts from Models.OllamaToolCall to ToolCall.Models.OllamaToolCall (needed due to duplicate type issue - Gap #13)
- Added ConvertToolCalls() method to bridge incompatible OllamaToolCall types
- Updated FinishReason logic to prefer ToolCalls when tool calls present (FR-054)
- All 20 OllamaResponseMapperTests pass including 8 new tool call integration tests (Gap #7)

---

### Gap #2: ChatMessage Creation Methods for Tool Calls
**Status**: [✅]
**File to Check**: src/Acode.Domain/Models/Inference/ChatMessage.cs or src/Acode.Domain/Conversation/ChatMessage.cs
**Why Needed**: Gap #1 requires ChatMessage.CreateAssistantWithTools() method to attach tool calls to assistant messages.
**Required Changes**:
1. Check if ChatMessage supports tool calls in constructor
2. Add CreateAssistantWithTools(string content, ToolCall[] toolCalls) factory method if missing
3. Ensure ToolCalls property exists and is accessible

**Implementation Pattern**:
```csharp
public static ChatMessage CreateAssistantWithTools(string content, ToolCall[] toolCalls)
{
    return new ChatMessage(
        Role: MessageRole.Assistant,
        Content: content,
        ToolCalls: toolCalls);
}
```

**Success Criteria**: ChatMessage can store and expose tool calls
**Evidence**: [To be filled when complete]

---

### Gap #3: Retry Logic Wrapper - ToolCallRetryHandler
**Status**: [✅]
**File to Create**: src/Acode.Infrastructure/Ollama/ToolCall/ToolCallRetryHandler.cs
**Why Needed**: FR-053 requires "retry on malformed tool call JSON (configurable)". Current ToolCallParser only attempts repair once - no retry loop exists.
**Required Implementation**:
1. Accepts RetryConfig, ToolCallParser, IModelProvider (for re-requesting)
2. ParseWithRetry(OllamaToolCall[] toolCalls, ChatRequest originalRequest) method
3. Implements retry loop:
   - First attempt: ToolCallParser.Parse()
   - If all succeed: return result
   - If some/all fail: build retry prompt with error details
   - Re-invoke model with retry prompt
   - Parse new response
   - Repeat up to MaxRetryAttempts
4. Throws ToolCallRetryExhaustedException when retries exhausted
5. Logs each retry attempt with correlation ID

**Implementation Pattern**:
```csharp
public sealed class ToolCallRetryHandler
{
    private readonly RetryConfig _config;
    private readonly ToolCallParser _parser;
    private readonly IModelProvider _provider;

    public ToolCallRetryHandler(RetryConfig config, ToolCallParser parser, IModelProvider provider)
    {
        _config = config;
        _parser = parser;
        _provider = provider;
    }

    public async Task<ToolCallParseResult> ParseWithRetryAsync(
        OllamaToolCall[] toolCalls,
        ChatRequest originalRequest,
        CancellationToken ct = default)
    {
        var attempt = 0;
        var lastResult = _parser.Parse(toolCalls);

        while (!lastResult.AllSucceeded && attempt < _config.MaxRetryAttempts)
        {
            attempt++;
            await Task.Delay(_config.RetryDelayMs * attempt, ct); // Exponential backoff

            // Build retry prompt with error details
            var retryPrompt = BuildRetryPrompt(lastResult.Errors);
            var retryRequest = BuildRetryRequest(originalRequest, retryPrompt);

            // Re-invoke model
            var retryResponse = await _provider.ChatAsync(retryRequest, ct);

            // Extract and parse new tool calls
            var newToolCalls = ExtractToolCalls(retryResponse);
            lastResult = _parser.Parse(newToolCalls);
        }

        if (!lastResult.AllSucceeded)
        {
            throw new ToolCallRetryExhaustedException(
                $"Failed to parse tool calls after {attempt} retry attempts");
        }

        return lastResult;
    }

    private string BuildRetryPrompt(List<ToolCallError> errors) { /* ... */ }
    private ChatRequest BuildRetryRequest(ChatRequest original, string retryPrompt) { /* ... */ }
    private OllamaToolCall[] ExtractToolCalls(ChatResponse response) { /* ... */ }
}
```

**Success Criteria**: Retry handler successfully retries failed tool call parsing up to configured limit
**Evidence**:
- Implemented ToolCallRetryHandler.cs with full retry logic
- ParseWithRetryAsync method implements retry loop with exponential backoff
- BuildRetryPrompt method constructs error-specific prompts from template
- ExtractToolCalls method converts ChatResponse back to OllamaToolCall format
- All 10 tests pass (TDD GREEN phase complete):
  - Valid calls (no retry needed)
  - Malformed JSON with successful retry
  - Max retries exceeded with exception
  - Partial failure handling
  - Multiple retry attempts
  - Exponential backoff timing (40-100ms tolerance)
  - Cancellation token support
  - Retry prompt construction with error details
  - Zero retries configuration
  - Custom retry prompt templates

---

### Gap #4: RetryConfig Integration into OllamaConfiguration
**Status**: [✅]
**File to Modify**: src/Acode.Infrastructure/Ollama/OllamaConfiguration.cs
**Why Needed**: Retry behavior must be configurable per FR-053 "configurable".
**Required Changes**:
1. Add ToolCallRetryConfig property of type RetryConfig
2. Default to RetryConfig with MaxRetryAttempts = 3
3. Load from configuration (.agent/config.yml)

**Implementation Pattern**:
```csharp
public sealed class OllamaConfiguration
{
    // ... existing properties ...

    public RetryConfig ToolCallRetryConfig { get; set; } = new RetryConfig
    {
        MaxRetryAttempts = 3,
        EnableAutoRepair = true,
        RetryDelayMs = 100,
        RetryPromptTemplate = RetryConfig.DefaultRetryPromptTemplate
    };
}
```

**Success Criteria**: OllamaConfiguration exposes retry configuration
**Evidence**:
- Added ToolCallRetryConfig property to OllamaConfiguration
- Initialized with sensible defaults: MaxRetries=3, RetryDelayMs=100, EnableAutoRepair=true
- Uses property initializer pattern consistent with record style
- All existing configuration tests still pass
- Configuration is ready for use by ToolCallRetryHandler

---

### Gap #5: Streaming Tool Call Integration
**Status**: [ ]
**File to Modify**: src/Acode.Infrastructure/Ollama/Mapping/OllamaDeltaMapper.cs
**Why Needed**: Tool calls can arrive in streaming responses - must accumulate and parse correctly.
**Required Changes**:
1. Check if OllamaStreamChunk contains tool call deltas
2. Use StreamingToolCallAccumulator to accumulate deltas
3. Parse complete tool calls when final chunk arrives
4. Include tool calls in final ResponseDelta

**Implementation Pattern**:
```csharp
public static ResponseDelta MapToDelta(OllamaStreamChunk chunk, int index)
{
    // ... existing content delta mapping ...

    ToolCallDelta[]? toolCallDeltas = null;
    if (chunk.Message?.ToolCalls != null)
    {
        toolCallDeltas = chunk.Message.ToolCalls
            .Select(tc => new ToolCallDelta(
                Index: index,
                Id: tc.Id,
                Name: tc.Function?.Name,
                ArgumentsDelta: tc.Function?.Arguments))
            .ToArray();
    }

    return new ResponseDelta(
        Index: index,
        ContentDelta: contentDelta,
        ToolCallDeltas: toolCallDeltas,
        FinishReason: finishReason,
        Usage: usage);
}
```

**Success Criteria**: Streaming responses correctly accumulate and parse tool calls
**Evidence**: [To be filled when complete]

---

### Gap #6: Tests for Retry Logic - ToolCallRetryHandlerTests.cs
**Status**: [✅]
**File to Create**: tests/Acode.Infrastructure.Tests/Ollama/ToolCall/ToolCallRetryHandlerTests.cs
**Why Needed**: TDD requirement - test retry logic before/during implementation.
**Required Tests** (minimum 10):
1. ParseWithRetryAsync_ValidToolCalls_NoRetryNeeded
2. ParseWithRetryAsync_MalformedJson_RetriesAndSucceeds
3. ParseWithRetryAsync_MaxRetriesExceeded_ThrowsException
4. ParseWithRetryAsync_PartialFailure_RetriesOnlyFailed
5. ParseWithRetryAsync_RetrySucceedsOnSecondAttempt
6. ParseWithRetryAsync_ExponentialBackoff_DelaysCorrectly
7. ParseWithRetryAsync_CancellationToken_CancelsRetry
8. BuildRetryPrompt_IncludesErrorDetails
9. ParseWithRetryAsync_ZeroMaxRetries_NoRetryAttempted
10. ParseWithRetryAsync_RetryPromptUsedCorrectly

**Implementation Pattern**: See ToolCallParserTests.cs for test structure
**Success Criteria**: All retry logic tests passing (tests written, awaiting implementation)
**Evidence**:
- Created ToolCallRetryHandlerTests.cs with 10 comprehensive tests
- Tests cover: valid calls (no retry), malformed JSON with retry, max retries exceeded, partial failures, multiple retry attempts, exponential backoff, cancellation, retry prompt construction, zero retries, custom templates
- Tests use NSubstitute to mock IModelProvider
- All tests compile correctly (RED phase - awaiting ToolCallRetryHandler implementation)

---

### Gap #7: Tests for OllamaResponseMapper Tool Call Integration
**Status**: [✅]
**File to Modify**: tests/Acode.Infrastructure.Tests/Ollama/Mapping/OllamaResponseMapperTests.cs (create if missing)
**Why Needed**: Verify tool call parsing integration in response mapping.
**Required Tests** (minimum 8):
1. Map_ResponseWithToolCalls_ParsesCorrectly ✅
2. Map_ResponseWithMultipleToolCalls_ParsesAll ✅
3. Map_ResponseWithToolCalls_SetsFinishReasonToolCalls ✅
4. Map_ResponseWithNoToolCalls_ReturnsNormalMessage ✅
5. Map_ResponseWithToolCalls_PreservesOtherFields ✅
6. Map_ResponseWithEmptyToolCallsArray_ReturnsNullToolCalls ✅
7. Map_ResponseWithNullParameters_UsesEmptyObject ✅
8. Map_ResponseWithMissingToolCallId_GeneratesId ✅

**Success Criteria**: All integration tests passing
**Evidence**:
- Added 8 new tests to OllamaResponseMapperTests.cs
- Fixed existing test Map_Should_Map_DoneReason_ToolCalls_To_FinishReason_ToolCalls to include actual tool calls
- All 20 tests pass (12 original + 8 new)
- Test coverage: single/multiple tool calls, FinishReason mapping, empty arrays, null parameters, ID generation, field preservation

---

### Gap #8: Tests for Streaming Tool Call Integration
**Status**: [ ]
**File to Modify**: tests/Acode.Infrastructure.Tests/Ollama/Streaming/OllamaStreamReaderTests.cs or create new file
**Why Needed**: Verify streaming tool call accumulation works correctly.
**Required Tests** (minimum 6):
1. StreamAsync_WithToolCallDeltas_AccumulatesCorrectly
2. StreamAsync_MultipleToolCalls_AccumulatesIndependently
3. StreamAsync_FinalChunk_ParsesCompleteToolCalls
4. StreamAsync_MalformedToolCallInStream_RepairsAndParses
5. StreamAsync_ToolCallSpansMultipleChunks_ReassemblesCorrectly
6. StreamAsync_ToolCallWithoutDeltas_HandlesGracefully

**Success Criteria**: All streaming tool call tests passing
**Evidence**: [To be filled when complete]

---

### Gap #9: JsonRepairer Tests (separate from parser tests)
**Status**: [✅]
**File to Create**: tests/Acode.Infrastructure.Tests/Ollama/ToolCall/JsonRepairerTests.cs
**Why Needed**: JsonRepairer is a complex component (472 lines) and should have dedicated tests separate from ToolCallParserTests.
**Required Tests** (minimum 15):
1. TryRepair_ValidJson_ReturnsAlreadyValid
2. TryRepair_TrailingComma_Removes
3. TryRepair_MissingClosingBrace_Adds
4. TryRepair_MissingClosingBracket_Adds
5. TryRepair_SingleQuotes_ReplacesWithDouble
6. TryRepair_UnquotedKeys_AddsQuotes
7. TryRepair_UnclosedString_ClosesString
8. TryRepair_MultipleErrors_AppliesAllRepairs
9. TryRepair_Timeout_ReturnsFailure
10. TryRepair_EmptyInput_ReturnsFailure
11. TryRepair_NestedObjects_RepairsCorrectly
12. TryRepair_Arrays_RepairsCorrectly
13. TryRepair_Idempotent_SameResultOnRerun
14. TryRepair_ComplexMalformed_Repairs
15. TryRepair_IrrepairableJson_ReturnsFailure

**Success Criteria**: All JSON repair tests passing (15+ tests)
**Evidence**:
- JsonRepairerTests.cs exists with 16 tests (exceeds 15 minimum)
- All 16 tests pass ✅
- Coverage includes: valid JSON, null/empty, trailing commas, missing braces/brackets, single quotes, unclosed strings, combined errors, timeout, nested objects, empty collections
- Tests verify repair heuristics, error handling, and edge cases

---

### Gap #10: Error Code Documentation
**Status**: [✅]
**File to Create**: docs/error-codes/ollama-tool-call-errors.md
**Why Needed**: Error codes (ACODE-TLP-001 through ACODE-TLP-006) are used but not documented.
**Required Content**:
1. ACODE-TLP-001: Tool call missing function definition
2. ACODE-TLP-002: Tool call has empty function name
3. ACODE-TLP-003: Tool name contains invalid characters
4. ACODE-TLP-004: Unable to parse arguments
5. ACODE-TLP-005: Tool name exceeds maximum length
6. ACODE-TLP-006: Failed to create tool call

Each with: description, cause, resolution, example

**Success Criteria**: Error code documentation complete and accurate
**Evidence**:
- Created docs/error-codes/ollama-tool-call-errors.md (comprehensive documentation)
- All 6 error codes documented with:
  - Description: Clear explanation of what the error means
  - Cause: Common scenarios that trigger the error
  - Resolution: Step-by-step fixes (model side, retry logic, validation)
  - Example: Invalid and valid JSON examples for each error
- Additional sections:
  - Error handling flow diagram (parsing pipeline)
  - Retry integration documentation
  - Configuration reference (RetryConfig)
  - Telemetry and logging guidance with JSONL format example
  - Testing coverage reference
  - Related documentation links

---

### Gap #11: Configuration Documentation for Retry Settings
**Status**: [ ]
**File to Update**: docs/user-manual/ollama-configuration.md or similar
**Why Needed**: Users need to know how to configure retry behavior.
**Required Content**:
1. tool_call_retry.max_retry_attempts (default: 3)
2. tool_call_retry.enable_auto_repair (default: true)
3. tool_call_retry.retry_delay_ms (default: 100)
4. tool_call_retry.retry_prompt_template (optional)

**Example**:
```yaml
providers:
  ollama:
    tool_call_retry:
      max_retry_attempts: 3
      enable_auto_repair: true
      retry_delay_ms: 100
```

**Success Criteria**: Configuration documentation complete with examples
**Evidence**: [To be filled when complete]

---

### Gap #12: Integration Tests for End-to-End Tool Call Flow
**Status**: [ ]
**File to Create**: tests/Acode.Integration.Tests/Ollama/ToolCallIntegrationTests.cs
**Why Needed**: Verify complete flow from Ollama response → parsing → retry → domain ToolCall works end-to-end.
**Required Tests** (minimum 5):
1. EndToEnd_ValidToolCall_ParsesSuccessfully
2. EndToEnd_MalformedToolCall_RetriesAndSucceeds
3. EndToEnd_MultipleToolCalls_AllParsed
4. EndToEnd_StreamingToolCalls_AccumulateAndParse
5. EndToEnd_RetryExhausted_ThrowsException

**Success Criteria**: All integration tests passing
**Evidence**: [To be filled when complete]

---

## IMPLEMENTATION ORDER

Following TDD (tests first):

**Phase 1: Core Integration**
1. Gap #2 (ChatMessage tool call support) - check/add
2. Gap #1 (OllamaResponseMapper integration) - implement
3. Gap #7 (OllamaResponseMapper tests) - write tests first, then implement

**Phase 2: Retry Logic**
4. Gap #6 (ToolCallRetryHandler tests) - write tests first
5. Gap #3 (ToolCallRetryHandler) - implement to pass tests
6. Gap #4 (RetryConfig in OllamaConfiguration) - add config

**Phase 3: Streaming**
7. Gap #8 (Streaming tool call tests) - write tests first
8. Gap #5 (Streaming integration) - implement

**Phase 4: Missing Tests**
9. Gap #9 (JsonRepairer tests) - write comprehensive tests

**Phase 5: Integration & Documentation**
10. Gap #12 (Integration tests) - end-to-end verification
11. Gap #10 (Error code docs) - document error codes
12. Gap #11 (Config docs) - document retry settings

## PROGRESS TRACKING

Mark each gap:
- [ ] Not started
- [🔄] In progress
- [✅] Complete with evidence

## DEPENDENCIES ON TASK-005A

**Potential Dependencies**:
- OllamaResponse type structure (should be complete in 005a)
- OllamaStreamChunk type structure (should be complete in 005a)
- OllamaHttpClient for making retry requests (should be complete in 005a)

**Deferral Strategy**: If any Gap depends on incomplete 005a work, defer ONLY that Gap to end of implementation. Document clearly what's waiting on 005a merge.

## AUDIT CHECKLIST

Before declaring complete:
- [ ] All 12 gaps addressed
- [ ] All tests passing (existing 18 + new ~44 = 62 total minimum)
- [ ] Build has 0 warnings, 0 errors
- [ ] No NotImplementedException in code
- [ ] All error codes documented
- [ ] Configuration documented
- [ ] Integration tests passing
- [ ] Retry logic working with configurable settings
- [ ] Tool calls parsed in both streaming and non-streaming
- [ ] FinishReason.ToolCalls set correctly
- [ ] Multiple simultaneous tool calls supported
