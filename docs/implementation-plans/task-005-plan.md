# Task 005 Implementation Plan: Ollama Provider Adapter

## Status: In Progress

**Branch**: `feature/task-005-ollama-provider-adapter`

## Overview

Implement complete Ollama provider adapter with:
- Task 005 (parent): Core OllamaProvider class, configuration, health checking, retry logic
- Task 005a: HTTP communication layer (request/response, streaming, NDJSON parsing)
- Task 005b: Tool call parsing with JSON repair and retry-on-invalid-JSON
- Task 005c: Setup documentation and smoke test scripts

## Strategic Approach

Following TDD best practices and Clean Architecture:
1. **Domain layer**: Already complete (IModelProvider from Task 004)
2. **Infrastructure layer**: Implement Ollama-specific concrete types and mappers
3. **Tests first**: RED-GREEN-REFACTOR for every component
4. **No external dependencies in tests**: Mock HttpClient boundaries, fake time/IDs
5. **Commit frequently**: One commit per logical unit (enum, interface, mapper, etc.)

## Architecture

```
Infrastructure Layer (Acode.Infrastructure):
├── Ollama/
│   ├── OllamaProvider.cs (implements IModelProvider)
│   ├── OllamaConfiguration.cs
│   ├── Http/
│   │   ├── OllamaHttpClient.cs
│   │   └── IOllamaHttpClient.cs (boundary for mocking)
│   ├── Mapping/
│   │   ├── OllamaMessageMapper.cs
│   │   ├── OllamaToolMapper.cs
│   │   └── OllamaResponseParser.cs
│   ├── Models/
│   │   ├── OllamaRequest.cs
│   │   ├── OllamaResponse.cs
│   │   ├── OllamaMessage.cs
│   │   ├── OllamaStreamChunk.cs
│   │   └── OllamaToolCall.cs
│   ├── Streaming/
│   │   └── OllamaStreamReader.cs
│   ├── ToolCall/
│   │   ├── ToolCallParser.cs
│   │   ├── JsonRepairer.cs
│   │   ├── ToolCallRetryHandler.cs
│   │   └── StreamingToolCallAccumulator.cs
│   ├── Health/
│   │   └── OllamaHealthChecker.cs
│   ├── Retry/
│   │   ├── OllamaRetryPolicy.cs
│   │   └── IOllamaRetryPolicy.cs
│   └── Exceptions/
│       ├── OllamaException.cs
│       ├── OllamaConnectionException.cs
│       ├── OllamaTimeoutException.cs
│       ├── OllamaRequestException.cs
│       ├── OllamaServerException.cs
│       ├── OllamaParseException.cs
│       ├── ToolCallParseException.cs
│       ├── ToolCallValidationException.cs
│       └── ToolCallRetryExhaustedException.cs
```

## Subtask Breakdown

### Task 005a: Request/Response & Streaming Handling (13 FP)

#### Subtask 005a-1: Ollama Model Types
- OllamaRequest record (with JsonPropertyName attributes)
- OllamaResponse record
- OllamaMessage record
- OllamaStreamChunk record
- OllamaOptions record
- Use System.Text.Json source generators

#### Subtask 005a-2: Request Serialization
- RequestSerializer class
- Maps ChatRequest → OllamaRequest
- Maps messages, tools, options
- Snake_case JSON properties
- Tests: unit tests for all mappings

#### Subtask 005a-3: Response Parsing
- ResponseParser class
- Maps OllamaResponse → ChatResponse
- Maps done_reason → FinishReason enum
- Calculates UsageInfo from token counts
- Tests: unit tests for all finish reasons

#### Subtask 005a-4: HTTP Client
- IOllamaHttpClient interface (mockable boundary)
- OllamaHttpClient implementation
- PostAsync<T> for non-streaming
- PostStreamAsync for streaming
- Connection pooling, timeouts
- Tests: unit tests with mocked HttpClient

#### Subtask 005a-5: NDJSON Stream Reading
- OllamaStreamReader class
- Reads NDJSON line-by-line
- Yields IAsyncEnumerable<OllamaStreamChunk>
- Handles partial lines, cancellation
- Tests: unit tests with mock streams

#### Subtask 005a-6: Delta Parsing
- DeltaParser class
- Maps OllamaStreamChunk → ResponseDelta
- Extracts content delta, tool call delta
- Sets FinishReason on final chunk
- Tests: unit tests for streaming scenarios

### Task 005b: Tool Call Parsing & Retry (13 FP)

#### Subtask 005b-1: Tool Call Extraction
- OllamaToolCall model
- ToolCallParser class
- Extracts tool_calls from OllamaResponse
- Maps to ToolCall type (Task 004.a)
- Generates IDs if missing
- Tests: unit tests for extraction

#### Subtask 005b-2: JSON Repair Heuristics
- JsonRepairer class
- RepairResult type
- Fixes trailing commas, missing braces, unquoted keys, etc.
- Timeout after 100ms
- Tests: unit tests for each repair heuristic

#### Subtask 005b-3: Retry Mechanism
- ToolCallRetryHandler class
- Retry on parse failure with error context
- Constructs retry prompts
- Respects max retry count
- Tracks token usage across retries
- Tests: unit tests with mock provider calls

#### Subtask 005b-4: Schema Validation (stub for Task 007)
- IToolSchemaRegistry interface (placeholder)
- Schema validation in ToolCallParser
- Distinguish parse vs validation errors
- Tests: unit tests with fake registry

#### Subtask 005b-5: Streaming Tool Call Accumulation
- StreamingToolCallAccumulator class
- Buffers tool call fragments
- Correlates by index
- Detects completion
- Tests: unit tests for fragment accumulation

#### Subtask 005b-6: Exception Types
- ToolCallParseException
- ToolCallValidationException
- ToolCallRetryExhaustedException
- Include correlation ID, attempt count
- Tests: exception properties validated

### Task 005 (parent): Core Provider & Integration (21 FP)

#### Subtask 005-1: Configuration
- OllamaConfiguration class
- Endpoint, timeouts, retry settings, model mappings
- Validation on construction
- Environment variable overrides
- Tests: unit tests for config loading and validation

#### Subtask 005-2: Exception Types
- OllamaException (base)
- OllamaConnectionException
- OllamaTimeoutException
- OllamaRequestException
- OllamaServerException
- OllamaParseException
- Error codes (ACODE-OLM-001 to ACODE-OLM-010)
- Tests: exception hierarchy and properties

#### Subtask 005-3: Retry Policy
- IOllamaRetryPolicy interface
- OllamaRetryPolicy implementation
- Exponential backoff
- Transient error detection
- Max retries enforcement
- Tests: unit tests for retry logic

#### Subtask 005-4: Health Checking
- IOllamaHealthChecker interface
- OllamaHealthChecker implementation
- Calls /api/tags endpoint
- Measures response time
- Returns HealthStatus (Healthy/Degraded/Unhealthy)
- Never throws exceptions
- Tests: unit tests with mock HTTP client

#### Subtask 005-5: OllamaProvider Core
- OllamaProvider class (implements IModelProvider)
- ChatAsync (non-streaming) implementation
- Uses RequestSerializer, ResponseParser
- Uses retry policy
- Proper error wrapping
- Tests: unit tests with all mocked dependencies

#### Subtask 005-6: OllamaProvider Streaming
- StreamAsync implementation
- Uses StreamReader, DeltaParser
- Handles cancellation
- Resource cleanup
- Tests: unit tests with mock streaming responses

#### Subtask 005-7: Model Management
- ListModelsAsync implementation
- GetModelInfoAsync implementation
- Model info caching
- Calls /api/tags, /api/show endpoints
- Tests: unit tests with mock responses

#### Subtask 005-8: DI Registration
- Register OllamaProvider with DI container
- Register with Provider Registry (Task 004c)
- Configure HttpClientFactory
- Tests: integration test verifying DI resolution

#### Subtask 005-9: Integration Tests
- Integration tests with live Ollama (marked [RequiresOllama])
- Non-streaming completion
- Streaming completion
- Tool calling
- Health check
- Model listing
- Tests skip if Ollama not available

### Task 005c: Setup Docs & Smoke Tests (5 FP)

#### Subtask 005c-1: Setup Documentation
- docs/ollama-setup.md
- Prerequisites, installation verification
- Configuration reference
- Quick start (< 50 lines)
- Troubleshooting guide
- Version compatibility table

#### Subtask 005c-2: Smoke Test Implementation
- OllamaSmokeTestRunner class
- Individual test classes (HealthCheckTest, CompletionTest, etc.)
- TestResult and TestReporter classes
- Tests: unit tests for smoke test runner

#### Subtask 005c-3: CLI Integration
- SmokeTestCommand class
- `acode providers smoke-test ollama` command
- Flags: --verbose, --skip-tool-test, --model, --timeout
- Text and JSON output formatters
- Tests: unit tests for CLI command

#### Subtask 005c-4: Standalone Scripts
- scripts/smoke-test-ollama.ps1 (PowerShell)
- scripts/smoke-test-ollama.sh (Bash)
- Cross-platform compatibility
- Exit codes (0/1/2)
- Tests: manual verification on Windows/Linux

## Completion Tracking

### Completed
✅ Task 005a-1: Ollama model types (34 tests passing)
   - OllamaRequest, OllamaMessage, OllamaOptions
   - OllamaTool, OllamaFunction, OllamaToolCall
   - OllamaResponse, OllamaStreamChunk
   - Committed: 3eb92ba

### In Progress
🔄 Task 005a-2: Request serialization

### Remaining
- Task 005a-2: Request serialization
- Task 005a-3: Response parsing
- Task 005a-4: HTTP client
- Task 005a-5: NDJSON stream reading
- Task 005a-6: Delta parsing
- Task 005b-1: Tool call extraction
- Task 005b-2: JSON repair
- Task 005b-3: Retry mechanism
- Task 005b-4: Schema validation stub
- Task 005b-5: Streaming tool call accumulation
- Task 005b-6: Tool call exceptions
- Task 005-1: Configuration
- Task 005-2: Core exceptions
- Task 005-3: Retry policy
- Task 005-4: Health checking
- Task 005-5: OllamaProvider core
- Task 005-6: OllamaProvider streaming
- Task 005-7: Model management
- Task 005-8: DI registration
- Task 005-9: Integration tests
- Task 005c-1: Setup documentation
- Task 005c-2: Smoke test implementation
- Task 005c-3: CLI integration
- Task 005c-4: Standalone scripts
- Final audit using docs/AUDIT-GUIDELINES.md
- Create PR

## TDD Workflow for Each Component

For every class/type:

1. **RED**: Write failing test first
   ```bash
   dotnet test --filter "FullyQualifiedName~OllamaRequest"
   # FAIL: Type does not exist
   ```

2. **GREEN**: Implement minimum code to pass
   ```csharp
   public sealed record OllamaRequest(...);
   ```

3. **REFACTOR**: Clean up while keeping tests green

4. **COMMIT**: One commit per logical unit
   ```bash
   git add .
   git commit -m "feat(task-005a): implement OllamaRequest record"
   git push origin feature/task-005-ollama-provider-adapter
   ```

## Key Constraints

- **No DateTime.Now, Guid.NewGuid() in production code**: Inject IClock, IGuidGenerator
- **No external LLM APIs**: Ollama only, local-first
- **Mock external boundaries**: HttpClient, filesystem, process execution
- **Thread-safe**: ConcurrentDictionary for caching, lock for shared state
- **Deterministic tests**: No network calls, no randomness, no time dependence
- **Clean Architecture**: Domain → Application → Infrastructure → CLI
- **Source-generated JSON**: Use System.Text.Json source generators for performance

## Acceptance Criteria Summary

- 281 tests from Task 004 still pass
- All new tests pass (estimated 200+ tests for Task 005)
- Build: 0 errors, 0 warnings
- All FR (functional requirements) from task specs satisfied
- All AC (acceptance criteria) from task specs checked
- Audit passes using docs/AUDIT-GUIDELINES.md
- PR created and ready for Copilot review

## Estimated Effort

- Task 005a: 13 Fibonacci points (~1-2 days)
- Task 005b: 13 Fibonacci points (~1-2 days)
- Task 005 (parent): 21 Fibonacci points (~2-3 days)
- Task 005c: 5 Fibonacci points (~0.5-1 day)

**Total**: 52 Fibonacci points (~4-7 days of implementation)

## Notes

- This is a large task spanning 4 specifications and 50+ subtasks
- Implementation will occur across multiple context sessions if needed
- Implementation plan will be updated as each subtask completes
- Focus on quality over speed - running out of context is acceptable
