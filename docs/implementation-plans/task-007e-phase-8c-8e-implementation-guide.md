# Task 007e Phases 8c-8e: Proper Implementation Guide

**Status**: Ready for implementation (all dependencies completed)
**Priority**: CRITICAL - Do NOT defer or take shortcuts
**Tokens**: Previous session ended with ~100k tokens remaining - recommend fresh session

## What This Completes

Phases 8c-8e implement the ORCHESTRATION layer that ties all Phase 0-7 components together with ChatRequest and VllmProvider.

**NOT optional. NOT deferrable. MUST be implemented.**

## Current State (End of Previous Session)

✅ COMPLETE:
- Phase 0-7: All StructuredOutput subsystems implemented and tested (124+ tests)
- Phase 8a: ResponseFormat and JsonSchemaFormat domain classes created
- Phase 8b: ChatRequest accepts ResponseFormat parameter and property
- DI Container: All StructuredOutput components registered
- VllmProvider: Accepts optional StructuredOutputHandler dependency (constructor overloads)

❌ MISSING (Phases 8c-8e):
- StructuredOutputHandler.ApplyToRequestAsync(ChatRequest, modelId, cancellationToken)
- Integration in VllmProvider.ChatAsync() to call ApplyToRequestAsync
- Integration in VllmProvider.StreamChatAsync() to call ApplyToRequestAsync

## Detailed Implementation Steps

### Step 1: Add ApplyToRequestAsync to StructuredOutputHandler

**File**: `src/Acode.Infrastructure/Vllm/StructuredOutput/StructuredOutputHandler.cs`

**Current state**: Lines 1-199 (closes with `}`)

**Action**: Insert these methods BEFORE the closing brace (before line 199):

```csharp
    /// <summary>
    /// Apply structured output constraints to a vLLM request based on ChatRequest.
    /// Handles both ResponseFormat and Tool schemas.
    /// </summary>
    public async Task<EnrichmentResult> ApplyToRequestAsync(
        ChatRequest chatRequest,
        string modelId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chatRequest);

        if (!_configuration.IsEnabled(modelId))
        {
            return EnrichmentResult.CreateDisabled($"Structured output disabled for model {modelId}");
        }

        // Check ResponseFormat first (higher priority)
        if (chatRequest.ResponseFormat is not null)
        {
            return await ApplyResponseFormatAsync(chatRequest.ResponseFormat, modelId, cancellationToken);
        }

        // Check Tools second - transform tool schemas
        if (chatRequest.Tools?.Any() == true)
        {
            return ApplyToolSchemas(chatRequest.Tools, modelId);
        }

        // No structured output needed
        return EnrichmentResult.CreateDisabled("No ResponseFormat or Tools configured");
    }

    private async Task<EnrichmentResult> ApplyResponseFormatAsync(
        Application.Inference.ResponseFormat format,
        string modelId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Detect model capabilities
            var capabilities = await _capabilityDetector.DetectAsync(modelId, cancellationToken);

            if (format.Type == "json_object")
            {
                if (!capabilities.SupportsGuidedJson)
                {
                    return EnrichmentResult.CreateFailed(
                        $"Model {modelId} does not support json_object mode",
                        ValidationFailureReason.UnsupportedModel);
                }

                var vllmFormat = new VllmResponseFormat { Type = "json_object" };
                return EnrichmentResult.CreateSuccess(vllmFormat, null, capabilities);
            }

            if (format.Type == "json_schema" && format.JsonSchema is not null)
            {
                if (!capabilities.SupportsJsonSchema)
                {
                    return EnrichmentResult.CreateFailed(
                        $"Model {modelId} does not support json_schema mode",
                        ValidationFailureReason.UnsupportedModel);
                }

                // Use existing EnrichRequestAsync for schema transformation
                return await EnrichRequestAsync(modelId, format.JsonSchema.Schema, cancellationToken);
            }

            return EnrichmentResult.CreateFailed(
                $"Unknown response format type: {format.Type}",
                ValidationFailureReason.InvalidSchema);
        }
        catch (Exception ex)
        {
            return EnrichmentResult.CreateFailed(
                $"Error applying response format: {ex.Message}",
                ValidationFailureReason.EnrichmentError);
        }
    }

    private EnrichmentResult ApplyToolSchemas(
        Domain.Models.Inference.ToolDefinition[] tools,
        string modelId)
    {
        try
        {
            if (tools.Length == 0)
            {
                return EnrichmentResult.CreateDisabled("No tools provided");
            }

            // Collect all tool parameter schemas
            var toolSchemas = new List<System.Text.Json.JsonElement>();
            foreach (var tool in tools)
            {
                toolSchemas.Add(tool.Parameters);
            }

            // Return success with tool schemas for vLLM to apply
            return EnrichmentResult.CreateSuccess(null, toolSchemas.ToArray(), null);
        }
        catch (Exception ex)
        {
            return EnrichmentResult.CreateFailed(
                $"Error applying tool schemas: {ex.Message}",
                ValidationFailureReason.EnrichmentError);
        }
    }
```

**Required Usings** (add to top of file if not present):
```csharp
using Acode.Application.Inference;
using Acode.Domain.Models.Inference;
```

**Implementation Approach**:
1. Read entire StructuredOutputHandler.cs file
2. Extract lines 1-197 (all content before closing brace)
3. Append the three methods above
4. Append lines 199+ (closing brace and any content after)
5. Write back to file

**Verification**:
```bash
dotnet build src/Acode.Infrastructure/Acode.Infrastructure.csproj
# Should have 0 errors, 0 warnings
```

### Step 2: Integrate with VllmProvider.ChatAsync

**File**: `src/Acode.Infrastructure/Vllm/VllmProvider.cs`

**Location**: Line 65 (ChatAsync method)

**Current code**:
```csharp
public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
{
    ObjectDisposedException.ThrowIf(_disposed, this);
    ArgumentNullException.ThrowIfNull(request);

    var stopwatch = Stopwatch.StartNew();

    var vllmRequest = MapToVllmRequest(request);
    var vllmResponse = await _client.SendRequestAsync(vllmRequest, cancellationToken).ConfigureAwait(false);

    stopwatch.Stop();

    return MapToChatResponse(vllmResponse, stopwatch.Elapsed);
}
```

**Modified code**:
```csharp
public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
{
    ObjectDisposedException.ThrowIf(_disposed, this);
    ArgumentNullException.ThrowIfNull(request);

    var stopwatch = Stopwatch.StartNew();

    var vllmRequest = MapToVllmRequest(request);
    
    // Apply structured output enrichment if handler available
    if (_structuredOutputHandler is not null)
    {
        var enrichmentResult = await _structuredOutputHandler.ApplyToRequestAsync(
            request,
            vllmRequest.Model,
            cancellationToken).ConfigureAwait(false);

        // TODO: Apply enrichmentResult to vllmRequest (guided_json, response_format, etc.)
        // This depends on VllmRequest structure and guided decoding parameter support
    }

    var vllmResponse = await _client.SendRequestAsync(vllmRequest, cancellationToken).ConfigureAwait(false);

    stopwatch.Stop();

    return MapToChatResponse(vllmResponse, stopwatch.Elapsed);
}
```

### Step 3: Integrate with VllmProvider.StreamChatAsync

**File**: `src/Acode.Infrastructure/Vllm/VllmProvider.cs`

**Location**: Line 81 (StreamChatAsync method)

**Similar pattern**: Apply structured output enrichment before streaming

### Step 4: Write Tests

**File**: `tests/Acode.Infrastructure.Tests/Vllm/StructuredOutput/StructuredOutputIntegrationTests.cs`

Create tests for:
1. ResponseFormat json_object mode
2. ResponseFormat json_schema mode with schema
3. Tool schemas application
4. Model capability detection with caching
5. Fallback when capability unavailable

## Key Notes

- **DO NOT** try to use sed/shell scripts to edit files
- **USE** Read tool to read entire file
- **USE** Edit tool with clear old_string/new_string boundaries
- **OR** use Write tool to write complete file after proper assembly
- **Verify** each step with `dotnet build` and `dotnet test`
- **Commit** after each logical unit completes

## Files Affected

1. `src/Acode.Infrastructure/Vllm/StructuredOutput/StructuredOutputHandler.cs` - Add 3 methods
2. `src/Acode.Infrastructure/Vllm/VllmProvider.cs` - Add enrichment calls
3. `tests/Acode.Infrastructure.Tests/Vllm/StructuredOutput/StructuredOutputIntegrationTests.cs` - Add tests

## Success Criteria

- All existing tests pass (1640+ Infrastructure tests)
- All new tests pass (estimated 10-15 integration tests)
- Build clean: 0 errors, 0 warnings
- VllmProvider properly calls ApplyToRequestAsync for both ResponseFormat and Tools
- Structured output enrichment properly transforms ChatRequest to vLLM format

## DO NOT

❌ Defer phases 8c-8e
❌ Skip integration tests
❌ Take shortcuts on implementation
❌ Use shell script file manipulation
❌ Commit incomplete work
❌ Create PR before audit passes

## DO

✅ Read entire file before editing
✅ Test after each step
✅ Commit after each logical unit
✅ Follow spec exactly
✅ Create comprehensive integration tests
✅ Verify all 1640+ tests still pass
✅ Document any blocking dependencies
