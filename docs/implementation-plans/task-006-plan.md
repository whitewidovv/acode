# Task 006 Implementation Plan: vLLM Provider Adapter

## Status: ✅ COMPLETE - PR #9 Created

## Overview

Implement complete vLLM Provider Adapter for Acode - a high-performance inference backend using OpenAI-compatible API with guided decoding, SSE streaming, and comprehensive health checking.

**Total Scope**: 3 specifications (4th deferred to Task 007)
- Task 006 (parent): Core VllmProvider implementation (21 FP)
- Task 006a: HTTP Client + SSE Streaming (13 FP)
- ~~Task 006b: Structured Outputs (13 FP)~~ → **MOVED TO TASK 007e**
- Task 006c: Health Checking + Error Handling (8 FP)

**Total Complexity**: 42 Fibonacci Points (55 FP originally, 13 FP deferred to Task 007e)

**IMPORTANT**: Task 006b renamed to Task 007e due to dependency on IToolSchemaRegistry from Task 007. User approved this move on 2026-01-04.

## Key Differences from Task 005 (Ollama)

| Aspect | Ollama (Task 005) | vLLM (Task 006) |
|--------|-------------------|-----------------|
| API Format | Custom | OpenAI-compatible |
| Streaming | NDJSON | SSE (Server-Sent Events) |
| Structured Output | Retry-based (005b) | Guided decoding (native) |
| Endpoint | /api/chat | /v1/chat/completions |
| Target Use | Development | Production |
| Throughput | Moderate | High (PagedAttention) |

## Critical Success Factors

1. **ALL subtasks MUST be complete** - No exceptions per CLAUDE.md
2. **Strict TDD** - Red-Green-Refactor for EVERY component
3. **Dependency awareness** - STOP if blocker found, don't self-approve
4. **Commit frequently** - After each logical unit
5. **Comprehensive audit** - ALL items per AUDIT-GUIDELINES.md

## Subtask Verification

Before starting, verified all subtasks exist:
```
✅ task-006-vllm-provider-adapter.md (parent)
✅ task-006a-implement-serving-assumptions-client-adapter.md
⚠️ task-006b → MOVED TO task-007e (dependency blocker)
✅ task-006c-loadhealth-check-endpoints-error-handling.md
```

**ALL 3 specifications MUST be implemented to completion.** Task 006b deferred to Task 007e with user approval due to IToolSchemaRegistry dependency.

## Dependencies

### External Dependencies
- ✅ Task 004 (IModelProvider interface) - COMPLETE
- ✅ Task 001 (Operating Modes) - COMPLETE
- ✅ Task 002 (Configuration) - COMPLETE
- ✅ Task 004.a (Message/ToolCall types) - COMPLETE
- ✅ Task 004.b (Response types) - COMPLETE
- ❓ Task 004.c (Provider Registry) - Check status during Phase 4

### Dependency Resolution
- ✅ Task 007 dependency: Originally blocked 006b (Structured Outputs)
- ✅ **RESOLVED**: Moved 006b → 007e with user approval (2026-01-04)
- ✅ No blockers remaining for Tasks 006a, 006c, 006 parent

## Implementation Strategy

### Phase 1: Foundation (Task 006a - HTTP Client & SSE)

**Goal**: Low-level HTTP communication layer with vLLM

**Components** (in TDD order):
1. VllmClientConfiguration (connection pooling, timeouts)
2. Exception types (VllmException hierarchy)
3. VllmRequest/VllmResponse model types
4. VllmRequestSerializer (System.Text.Json source generators)
5. VllmResponseParser
6. VllmHttpClient (SocketsHttpHandler with pooling)
7. VllmSseReader (SSE format: "data: " prefix, [DONE] sentinel)
8. VllmDeltaMapper (stream chunks → ResponseDelta)
9. VllmRetryPolicy (exponential backoff, transient/permanent classification)
10. VllmAuthHandler (Bearer token support)

**Key Technical Details**:
- SSE format: `data: {"chunk"}\n\n` NOT NDJSON
- [DONE] sentinel terminates stream
- Connection pooling via SocketsHttpHandler
- Correlation IDs for request tracing
- API key redaction in all logs

**Test Strategy**:
- Unit tests for each component (VllmHttpClientTests, VllmSseReaderTests, etc.)
- Mock HttpMessageHandler for HTTP tests
- Integration tests with real vLLM (conditional on vLLM availability)

### Phase 2: Health & Error Handling (Task 006c)

**Goal**: Health checking, load monitoring, error classification

**Components** (in TDD order):
1. VllmHealthConfiguration
2. VllmHealthResult (Healthy/Degraded/Unhealthy/Unknown)
3. VllmLoadStatus (request queue, GPU utilization)
4. VllmMetricsClient (Prometheus /metrics endpoint)
5. VllmMetricsParser (parse vllm_num_requests_*, vllm_gpu_cache_usage_perc)
6. VllmErrorParser (extract error.message, error.type, error.code)
7. VllmErrorClassifier (transient vs permanent)
8. VllmExceptionMapper (HTTP status → exception type)
9. IVllmException interface (with IsTransient flag)
10. VllmOutOfMemoryException (special CUDA OOM handling)
11. VllmHealthChecker (orchestrates health checks)

**Key Technical Details**:
- Health endpoints: /health (liveness), /v1/models (readiness), /metrics (load)
- Health status thresholds: <1s = Healthy, >5s = Degraded
- Load monitoring: queue depth, GPU cache usage
- Error classification: 4xx = permanent (except 429), 5xx = transient
- Correlation IDs in all exceptions

**Test Strategy**:
- Unit tests for error parsing (VllmErrorParserTests)
- Unit tests for classification (VllmErrorClassifierTests)
- Health checker tests with mocked endpoints
- Metrics parsing tests (Prometheus format)

### Phase 3: Core Provider (Task 006 parent)

**Goal**: Wire all components into IModelProvider implementation

**Components** (in TDD order):
1. VllmConfiguration (consolidate all config)
2. VllmMessageMapper (ChatRequest → VllmRequest)
3. VllmToolMapper (ToolDefinition → vLLM tools format)
4. VllmResponseParser (VllmResponse → ChatResponse)
5. VllmProvider (implements IModelProvider)
   - CompleteAsync (non-streaming)
   - StreamAsync (SSE streaming)
   - ListModelsAsync (/v1/models endpoint)
   - CheckHealthAsync (delegates to VllmHealthChecker)
6. DI registration (wire all components)

**Key Technical Details**:
- IModelProvider interface implementation
- ProviderId = "vllm"
- OpenAI-compatible message roles
- Tool calling support (tools array, tool_choice parameter)
- Streaming: IAsyncEnumerable<ResponseDelta>
- Proper resource cleanup (IAsyncDisposable)

**Test Strategy**:
- Unit tests for each component (VllmProviderTests, VllmMessageMapperTests, etc.)
- Integration tests with real vLLM
- End-to-end tests for tool calling
- Streaming tests (verify SSE parsing, [DONE] handling)

### Phase 5: Integration & Audit

**Goal**: Verify all components work together and meet specification

**Integration Steps**:
1. Register VllmProvider with Provider Registry (Task 004.c)
2. Create smoke test script (scripts/smoke-test-vllm.sh, .ps1)
3. Verify CLI integration (`acode providers health`, `acode providers status vllm`)
4. Verify configuration loading (.agent/config.yml)
5. Test all error scenarios (connection refused, timeout, OOM, etc.)
6. Performance validation (connection pooling, serialization overhead)

**Audit Checklist** (per AUDIT-GUIDELINES.md):
1. ✅ ALL subtasks verified complete (`find` command)
2. ✅ Build: 0 errors, 0 warnings
3. ✅ All tests passing
4. ✅ Every source file has tests
5. ✅ TDD followed (tests before implementation)
6. ✅ Layer boundaries respected (Domain → Application → Infrastructure → CLI)
7. ✅ XML documentation on all public APIs
8. ✅ Exception hierarchy documented
9. ✅ Configuration documented with examples
10. ✅ Integration tests passing
11. ✅ No NotImplementedException remaining
12. ✅ Resource cleanup verified (IAsyncDisposable)
13. ✅ Security: API keys redacted, no content at INFO level
14. ✅ Observability: correlation IDs, structured logging

## File Structure

```
src/Acode.Infrastructure/Vllm/
├── VllmProvider.cs                          # Phase 4
├── VllmConfiguration.cs                     # Phase 4
├── Client/
│   ├── VllmHttpClient.cs                   # Phase 1
│   ├── VllmClientConfiguration.cs          # Phase 1
│   ├── Serialization/
│   │   ├── VllmJsonSerializerContext.cs   # Phase 1
│   │   ├── VllmRequestSerializer.cs       # Phase 1
│   │   └── VllmResponseParser.cs          # Phase 1
│   ├── Streaming/
│   │   ├── VllmSseReader.cs               # Phase 1
│   │   └── VllmSseParser.cs               # Phase 1
│   ├── Retry/
│   │   ├── IVllmRetryPolicy.cs            # Phase 1
│   │   ├── VllmRetryPolicy.cs             # Phase 1
│   │   └── VllmRetryContext.cs            # Phase 1
│   └── Authentication/
│       └── VllmAuthHandler.cs              # Phase 1
├── StructuredOutput/
│   ├── StructuredOutputConfiguration.cs    # Phase 2
│   ├── StructuredOutputHandler.cs          # Phase 2
│   ├── Schema/
│   │   ├── SchemaTransformer.cs           # Phase 2
│   │   ├── SchemaValidator.cs             # Phase 2
│   │   └── SchemaCache.cs                 # Phase 2
│   ├── ResponseFormat/
│   │   ├── ResponseFormatBuilder.cs       # Phase 2
│   │   └── GuidedDecodingBuilder.cs       # Phase 2
│   ├── Capability/
│   │   ├── CapabilityDetector.cs          # Phase 2
│   │   └── CapabilityCache.cs             # Phase 2
│   └── Fallback/
│       ├── FallbackHandler.cs              # Phase 2
│       └── OutputValidator.cs              # Phase 2
├── Health/
│   ├── VllmHealthChecker.cs                # Phase 3
│   ├── VllmHealthConfiguration.cs          # Phase 3
│   ├── VllmHealthResult.cs                 # Phase 3
│   ├── VllmLoadStatus.cs                   # Phase 3
│   ├── Metrics/
│   │   ├── VllmMetricsClient.cs           # Phase 3
│   │   └── VllmMetricsParser.cs           # Phase 3
│   └── Errors/
│       ├── VllmErrorParser.cs              # Phase 3
│       ├── VllmErrorClassifier.cs          # Phase 3
│       └── VllmExceptionMapper.cs          # Phase 3
├── Mapping/
│   ├── VllmMessageMapper.cs                # Phase 4
│   ├── VllmToolMapper.cs                   # Phase 4
│   └── VllmDeltaMapper.cs                  # Phase 1
├── Models/
│   ├── VllmRequest.cs                      # Phase 1
│   ├── VllmResponse.cs                     # Phase 1
│   ├── VllmMessage.cs                      # Phase 1
│   ├── VllmToolCall.cs                     # Phase 1
│   ├── VllmStreamChunk.cs                  # Phase 1
│   └── VllmUsage.cs                        # Phase 1
└── Exceptions/
    ├── VllmException.cs                     # Phase 1
    ├── IVllmException.cs                    # Phase 3
    ├── VllmConnectionException.cs           # Phase 1
    ├── VllmTimeoutException.cs              # Phase 1
    ├── VllmRequestException.cs              # Phase 1
    ├── VllmServerException.cs               # Phase 1
    ├── VllmParseException.cs                # Phase 1
    ├── VllmAuthException.cs                 # Phase 1
    ├── VllmModelNotFoundException.cs        # Phase 1
    ├── VllmRateLimitException.cs            # Phase 1
    ├── VllmOutOfMemoryException.cs          # Phase 3
    ├── StructuredOutputException.cs         # Phase 2
    ├── SchemaTooComplexException.cs         # Phase 2
    └── ValidationFailedException.cs         # Phase 2

tests/Acode.Infrastructure.Tests/Vllm/
├── Client/
│   ├── VllmHttpClientTests.cs
│   ├── VllmRequestSerializerTests.cs
│   ├── VllmSseReaderTests.cs
│   ├── VllmRetryPolicyTests.cs
│   └── VllmAuthenticationTests.cs
├── StructuredOutput/
│   ├── StructuredOutputConfigurationTests.cs
│   ├── SchemaTransformerTests.cs
│   ├── ResponseFormatBuilderTests.cs
│   ├── CapabilityDetectorTests.cs
│   ├── FallbackHandlerTests.cs
│   └── OutputValidatorTests.cs
├── Health/
│   ├── VllmHealthCheckerTests.cs
│   ├── VllmMetricsParserTests.cs
│   ├── VllmErrorParserTests.cs
│   ├── VllmErrorClassifierTests.cs
│   └── VllmExceptionMapperTests.cs
├── VllmProviderTests.cs
├── VllmMessageMapperTests.cs
├── VllmToolMapperTests.cs
└── VllmResponseParserTests.cs
```

## Error Codes

All exceptions use ACODE-VLM-XXX format:

| Code | Exception | Transient | Description |
|------|-----------|-----------|-------------|
| ACODE-VLM-001 | VllmConnectionException | Yes | Connection failed |
| ACODE-VLM-002 | VllmTimeoutException | Yes | Request timeout |
| ACODE-VLM-003 | VllmModelNotFoundException | No | Model not found |
| ACODE-VLM-004 | VllmRequestException | No | Invalid request (400) |
| ACODE-VLM-005 | VllmServerException | Yes | Server error (5xx) |
| ACODE-VLM-006 | VllmParseException | No | Parse error |
| ACODE-VLM-011 | VllmAuthException | No | Auth failed (401) |
| ACODE-VLM-012 | VllmRateLimitException | Yes | Rate limited (429) |
| ACODE-VLM-013 | VllmOutOfMemoryException | Maybe | CUDA OOM |
| ACODE-VLM-SO-001 | SchemaTooComplexException | No | Schema too complex |
| ACODE-VLM-SO-002 | StructuredOutputException | No | Unsupported schema type |
| ACODE-VLM-SO-003 | StructuredOutputException | Yes | Guided decoding timeout |
| ACODE-VLM-SO-004 | StructuredOutputException | No | Invalid schema format |
| ACODE-VLM-SO-005 | ValidationFailedException | No | Validation failed (fallback) |
| ACODE-VLM-SO-006 | ValidationFailedException | No | Max retries exceeded |

## Implementation Order (Detailed)

### Phase 1: Task 006a (13 FP) - Estimated 20-25 commits

1. **VllmClientConfiguration** (2 commits)
   - RED: Test configuration validation
   - GREEN: Implement configuration record
   - COMMIT: "test(task-006a): add VllmClientConfiguration tests"
   - COMMIT: "feat(task-006a): implement VllmClientConfiguration"

2. **Exception Hierarchy** (3 commits)
   - RED: Test VllmException base class
   - GREEN: Implement VllmException with error codes
   - RED: Test all derived exceptions
   - GREEN: Implement all exception types
   - REFACTOR: Extract common patterns
   - COMMIT: "test(task-006a): add VllmException hierarchy tests"
   - COMMIT: "feat(task-006a): implement VllmException hierarchy"
   - COMMIT: "refactor(task-006a): extract exception patterns"

3. **Model Types** (4 commits)
   - RED: Test VllmRequest serialization
   - GREEN: Implement VllmRequest with JSON attributes
   - RED: Test VllmResponse deserialization
   - GREEN: Implement VllmResponse
   - RED: Test VllmMessage, VllmToolCall
   - GREEN: Implement VllmMessage, VllmToolCall
   - RED: Test VllmStreamChunk
   - GREEN: Implement VllmStreamChunk
   - COMMIT each: "test/feat(task-006a): implement [type]"

4. **VllmRequestSerializer** (2 commits)
   - RED: Test serialization with source generators
   - GREEN: Implement VllmJsonSerializerContext + serializer
   - COMMIT: "test(task-006a): add VllmRequestSerializer tests"
   - COMMIT: "feat(task-006a): implement VllmRequestSerializer with source generators"

5. **VllmResponseParser** (2 commits)
   - RED: Test response parsing
   - GREEN: Implement parser
   - COMMIT: "test(task-006a): add VllmResponseParser tests"
   - COMMIT: "feat(task-006a): implement VllmResponseParser"

6. **VllmHttpClient** (3 commits)
   - RED: Test HTTP client initialization
   - GREEN: Implement VllmHttpClient with SocketsHttpHandler
   - RED: Test PostAsync method
   - GREEN: Implement PostAsync
   - RED: Test PostStreamingAsync method
   - GREEN: Implement PostStreamingAsync
   - COMMIT: "test(task-006a): add VllmHttpClient tests"
   - COMMIT: "feat(task-006a): implement VllmHttpClient with connection pooling"
   - COMMIT: "feat(task-006a): implement streaming support in VllmHttpClient"

7. **VllmSseReader** (2 commits)
   - RED: Test SSE parsing ("data: " prefix, [DONE] sentinel)
   - GREEN: Implement VllmSseReader
   - COMMIT: "test(task-006a): add VllmSseReader tests"
   - COMMIT: "feat(task-006a): implement VllmSseReader for SSE streaming"

8. **VllmDeltaMapper** (2 commits)
   - RED: Test delta mapping from stream chunks
   - GREEN: Implement VllmDeltaMapper
   - COMMIT: "test(task-006a): add VllmDeltaMapper tests"
   - COMMIT: "feat(task-006a): implement VllmDeltaMapper"

9. **VllmRetryPolicy** (2 commits)
   - RED: Test retry logic (exponential backoff, transient classification)
   - GREEN: Implement VllmRetryPolicy
   - COMMIT: "test(task-006a): add VllmRetryPolicy tests"
   - COMMIT: "feat(task-006a): implement VllmRetryPolicy with exponential backoff"

10. **VllmAuthHandler** (2 commits)
    - RED: Test Bearer token handling
    - GREEN: Implement VllmAuthHandler
    - COMMIT: "test(task-006a): add VllmAuthHandler tests"
    - COMMIT: "feat(task-006a): implement VllmAuthHandler"

**Phase 1 Completion**: Create commit summarizing Phase 1
- COMMIT: "chore(task-006a): complete HTTP client and SSE streaming implementation"

### Phase 2: Task 006c (8 FP) - Estimated 15-18 commits

1. **VllmHealthConfiguration** (2 commits)
   - RED: Test configuration
   - GREEN: Implement configuration
   - COMMIT: "test(task-006c): add VllmHealthConfiguration tests"
   - COMMIT: "feat(task-006c): implement VllmHealthConfiguration"

2. **VllmHealthResult + VllmLoadStatus** (2 commits)
   - RED: Test result types
   - GREEN: Implement types
   - COMMIT: "test(task-006c): add VllmHealthResult and VllmLoadStatus tests"
   - COMMIT: "feat(task-006c): implement VllmHealthResult and VllmLoadStatus"

3. **VllmMetricsClient + Parser** (3 commits)
   - RED: Test metrics client
   - GREEN: Implement client
   - RED: Test Prometheus parsing
   - GREEN: Implement parser
   - COMMIT: "test(task-006c): add VllmMetricsClient tests"
   - COMMIT: "feat(task-006c): implement VllmMetricsClient"
   - COMMIT: "feat(task-006c): implement VllmMetricsParser for Prometheus format"

4. **VllmErrorParser** (2 commits)
   - RED: Test error parsing (extract message, type, code)
   - GREEN: Implement parser
   - COMMIT: "test(task-006c): add VllmErrorParser tests"
   - COMMIT: "feat(task-006c): implement VllmErrorParser"

5. **VllmErrorClassifier** (2 commits)
   - RED: Test transient/permanent classification
   - GREEN: Implement classifier
   - COMMIT: "test(task-006c): add VllmErrorClassifier tests"
   - COMMIT: "feat(task-006c): implement VllmErrorClassifier"

6. **VllmExceptionMapper** (2 commits)
   - RED: Test HTTP status → exception mapping
   - GREEN: Implement mapper
   - COMMIT: "test(task-006c): add VllmExceptionMapper tests"
   - COMMIT: "feat(task-006c): implement VllmExceptionMapper"

7. **IVllmException + VllmOutOfMemoryException** (2 commits)
   - RED: Test interface and CUDA OOM exception
   - GREEN: Implement interface and exception
   - COMMIT: "test(task-006c): add IVllmException and VllmOutOfMemoryException tests"
   - COMMIT: "feat(task-006c): implement IVllmException and VllmOutOfMemoryException"

8. **VllmHealthChecker** (2 commits)
   - RED: Test health checking (/health, /v1/models, /metrics)
   - GREEN: Implement health checker
   - COMMIT: "test(task-006c): add VllmHealthChecker tests"
   - COMMIT: "feat(task-006c): implement VllmHealthChecker"

**Phase 2 Completion**: Create commit summarizing Phase 2
- COMMIT: "chore(task-006c): complete health checking and error handling implementation"

### Phase 3: Task 006 Parent (21 FP) - Estimated 12-15 commits

1. **VllmConfiguration** (2 commits)
   - RED: Test consolidated configuration
   - GREEN: Implement configuration
   - COMMIT: "test(task-006): add VllmConfiguration tests"
   - COMMIT: "feat(task-006): implement VllmConfiguration"

2. **VllmMessageMapper** (2 commits)
   - RED: Test ChatRequest → VllmRequest mapping
   - GREEN: Implement mapper
   - COMMIT: "test(task-006): add VllmMessageMapper tests"
   - COMMIT: "feat(task-006): implement VllmMessageMapper"

3. **VllmToolMapper** (2 commits)
   - RED: Test ToolDefinition → vLLM tools mapping
   - GREEN: Implement mapper
   - COMMIT: "test(task-006): add VllmToolMapper tests"
   - COMMIT: "feat(task-006): implement VllmToolMapper"

4. **VllmResponseParser (consolidated)** (2 commits)
   - RED: Test VllmResponse → ChatResponse parsing
   - GREEN: Implement parser
   - COMMIT: "test(task-006): add VllmResponseParser tests"
   - COMMIT: "feat(task-006): implement VllmResponseParser"

5. **VllmProvider - CompleteAsync** (2 commits)
   - RED: Test non-streaming completion
   - GREEN: Implement CompleteAsync
   - COMMIT: "test(task-006): add VllmProvider CompleteAsync tests"
   - COMMIT: "feat(task-006): implement VllmProvider CompleteAsync"

6. **VllmProvider - StreamAsync** (2 commits)
   - RED: Test streaming completion
   - GREEN: Implement StreamAsync
   - COMMIT: "test(task-006): add VllmProvider StreamAsync tests"
   - COMMIT: "feat(task-006): implement VllmProvider StreamAsync"

7. **VllmProvider - ListModelsAsync + CheckHealthAsync** (2 commits)
   - RED: Test model listing and health checks
   - GREEN: Implement methods
   - COMMIT: "test(task-006): add VllmProvider ListModelsAsync and CheckHealthAsync tests"
   - COMMIT: "feat(task-006): implement VllmProvider ListModelsAsync and CheckHealthAsync"

8. **DI Registration** (2 commits)
   - RED: Test DI registration
   - GREEN: Implement registration
   - COMMIT: "test(task-006): add DI registration tests"
   - COMMIT: "feat(task-006): implement VllmProvider DI registration"

**Phase 3 Completion**: Create commit summarizing Phase 3
- COMMIT: "chore(task-006): complete VllmProvider core implementation"

### Phase 4: Integration & Audit (No specific FP) - Estimated 5-8 commits

1. **Provider Registry Integration** (1 commit)
   - Verify VllmProvider registered with Provider Registry
   - COMMIT: "feat(task-006): register VllmProvider with Provider Registry"

2. **Smoke Test Scripts** (2 commits)
   - Create scripts/smoke-test-vllm.sh
   - Create scripts/smoke-test-vllm.ps1
   - COMMIT: "test(task-006): add vLLM smoke test scripts (Bash + PowerShell)"
   - COMMIT: "docs(task-006): add vLLM setup guide (docs/vllm-setup.md)"

3. **Integration Tests** (2 commits)
   - Write integration tests (conditional on vLLM availability)
   - COMMIT: "test(task-006): add VllmProvider integration tests"
   - COMMIT: "test(task-006): add end-to-end tool calling tests"

4. **Documentation** (2 commits)
   - Update XML documentation
   - Create/update configuration examples
   - COMMIT: "docs(task-006): add XML documentation for all public APIs"
   - COMMIT: "docs(task-006): update configuration examples"

5. **Audit** (1 commit)
   - Create docs/TASK-006-AUDIT.md
   - Run full audit checklist
   - Fix any issues found
   - COMMIT: "chore(task-006): complete audit (ALL subtasks verified)"

**Phase 4 Completion**: Task 006 COMPLETE
- COMMIT: "chore(task-006): Task 006 complete - ALL subtasks verified and passing"

## Estimated Total Commits: 52-66 commits (70-88 originally, minus 18-22 for deferred 006b)

## Test Coverage Targets

- **Unit Tests**: 150+ tests (200+ originally, minus ~50 for deferred 006b)
  - Task 006a: ~60 tests (HTTP client, SSE, retry, auth)
  - ~~Task 006b: ~50 tests~~ → DEFERRED TO TASK 007e
  - Task 006c: ~40 tests (health, metrics, error classification)
  - Task 006: ~50 tests (provider, mappers, integration)

- **Integration Tests**: 15+ tests
  - VllmProvider end-to-end tests
  - Tool calling integration tests
  - Streaming integration tests
  - Health checking integration tests

- **Test Execution Time**: Target <5 seconds for unit tests, <30 seconds total

## Build & Quality Targets

- ✅ 0 errors
- ✅ 0 warnings
- ✅ 100% test pass rate
- ✅ All source files have corresponding tests
- ✅ XML documentation on all public APIs
- ✅ No NotImplementedException
- ✅ Resource cleanup verified (IAsyncDisposable)

## Performance Targets (NFRs)

- Connection establishment: <500ms
- Request serialization: <1ms
- Response parsing: <5ms
- SSE chunk parsing: <100μs
- Health check: <2s
- Schema transformation: <1ms
- Memory per request: <10KB (excluding content)

## Security Checklist

- ✅ API keys redacted in all logs
- ✅ No request content at INFO level
- ✅ Error messages sanitized
- ✅ Only configured endpoints contacted
- ✅ Local-only validation in airgapped mode
- ✅ Schema processing sandboxed
- ✅ Correlation IDs in all exceptions

## Completion Criteria (ALL MUST BE TRUE)

1. ✅ **ALL 3 subtasks implemented** (4th deferred with user approval):
   - ✅ Task 006a (HTTP Client)
   - ⚠️ Task 006b → MOVED TO TASK 007e (user approved 2026-01-04)
   - ✅ Task 006c (Health Checking)
   - ✅ Task 006 (Core Provider)

2. ✅ **Build clean**: 0 errors, 0 warnings

3. ✅ **All tests passing**: 150+ unit tests, 15+ integration tests

4. ✅ **TDD followed**: Every source file has tests written FIRST

5. ✅ **Layer boundaries respected**: Clean Architecture

6. ✅ **Documentation complete**:
   - XML documentation on all public APIs
   - Configuration examples
   - Setup guide (docs/vllm-setup.md)
   - Smoke test scripts

7. ✅ **Integration verified**:
   - Provider Registry registration
   - CLI commands work (`acode providers health`)
   - Configuration loading works

8. ✅ **Audit passes**: docs/TASK-006-AUDIT.md created and ALL items pass

9. ✅ **No self-approved deferrals**: User approved all scope changes

10. ✅ **Security verified**: API keys redacted, no sensitive data logged

## Risk Mitigation

### Risk 1: Task 007 (Tool Schema Registry) Dependency - ✅ RESOLVED

**Status**: MITIGATED
- ✅ Checked Task 007 status (not implemented)
- ✅ Stopped implementation and explained to user
- ✅ Got user approval to defer 006b → 007e
- ✅ Renamed task-006b-*.md → task-007e-*.md
- ✅ Updated all references in implementation plan
- ✅ Will document in audit

### Risk 2: vLLM Availability for Integration Tests

**Mitigation**:
- Make integration tests conditional (skip if vLLM not running)
- Provide clear instructions for running with vLLM
- Use mocked responses for unit tests (don't require vLLM)

### Risk 3: Complex SSE Streaming Logic

**Mitigation**:
- Comprehensive unit tests for VllmSseReader
- Test edge cases: incomplete lines, comment lines, [DONE] sentinel
- Reference Ollama's NDJSON reader as comparison

### ~~Risk 4: Schema Transformation Complexity~~ - ✅ DEFERRED

**Status**: No longer applicable (deferred to Task 007e)

## Success Metrics

- ✅ Task 006 fully functional vLLM provider
- ✅ ALL 3 subtasks complete (006b deferred to 007e with user approval)
- ✅ 150+ tests passing
- ✅ 0 build warnings
- ✅ Audit passes on first attempt (no rework)
- ✅ No antipattern recurrence (no rushing, all subtasks verified)

## Next Actions

1. ✅ Create feature branch: `feature/task-006-vllm-provider-adapter`
2. ✅ CHECK DEPENDENCY: Task 007 - RESOLVED (006b → 007e)
3. ✅ Update task specifications (renamed 006b → 007e)
4. ✅ Update implementation plan (removed Phase 2)
5. ⏭️ BEGIN Phase 1: Task 006a (HTTP Client)
6. ⏭️ Work autonomously until complete or <5k tokens
7. ⏭️ Update this plan as progress is made

---

**Plan Created**: 2026-01-04
**Plan Updated**: 2026-01-04 (006b deferred to 007e)
**Status**: Ready to Begin Implementation
**Estimated Duration**: 52-66 commits across 4 phases (70-88 originally)
**Dependencies**: ✅ ALL RESOLVED (006b → 007e with user approval)
