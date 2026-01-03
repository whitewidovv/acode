# EPIC 1 — Model Runtime, Inference, Tool-Calling Contract

**Priority:** P0 (Critical)  
**Phase:** 1 — Core Infrastructure  
**Dependencies:** Epic 0 (Product Definition, Constraints, Repo Contracts)  

---

## Epic Overview

### Purpose

Epic 1 establishes the foundational infrastructure for local model inference—the core capability that enables Agentic Coding Bot to operate without external LLM API calls. This epic implements the abstraction layer between the application and local model runtimes (Ollama, vLLM), defining how the system sends prompts, receives responses, handles tool calls, and manages the complex interaction patterns required for agentic coding workflows.

The model runtime layer is the most critical technical component of Acode. All autonomous operations—planning, coding, reviewing, testing—flow through this layer. The quality and reliability of this infrastructure directly determines the effectiveness and safety of the entire system.

### Scope

This epic covers:

1. **Model Provider Interface (Task 004):** Abstract interface defining how the application communicates with any model provider. Includes message types, tool-call contracts, response formats, and usage reporting. Provider-agnostic to enable future extensibility.

2. **Ollama Provider Adapter (Task 005):** Concrete implementation for the Ollama runtime. Handles streaming, tool-call parsing, retries on malformed JSON, and connection management. Includes setup documentation and smoke tests.

3. **vLLM Provider Adapter (Task 006):** Concrete implementation for vLLM serving. Handles structured outputs, load balancing hints, health checks, and the specific requirements of high-performance serving scenarios.

4. **Tool Schema Registry (Task 007):** Central registry of all tool definitions with JSON Schema validation. Ensures tool calls are well-formed before execution. Handles validation errors gracefully with model retry opportunities.

5. **Prompt Pack System (Task 008):** Modular system for loading, versioning, and selecting prompt templates. Enables different coding styles, frameworks, and methodologies while maintaining version control and reproducibility.

6. **Model Routing Policy (Task 009):** Logic for routing requests to appropriate model configurations based on task type (planning, coding, reviewing). Includes fallback and escalation rules.

### Boundaries

**In Scope:**
- Model provider abstraction layer
- Two concrete providers (Ollama, vLLM)
- Tool schema registration and validation
- Prompt template management
- Request routing logic
- Streaming response handling
- Error handling and retries
- Usage tracking and reporting

**Out of Scope:**
- External LLM API providers (OpenAI, Anthropic, etc.)
- Model training or fine-tuning
- Prompt optimization or tuning
- Multi-node distributed inference
- GPU resource scheduling
- Model downloading or management

### Dependencies

| Dependency | From Epic | Required For |
|------------|-----------|--------------|
| Operating Modes | Epic 0, Task 001 | Mode-aware provider behavior |
| Config Contract | Epic 0, Task 002 | Provider configuration loading |
| Audit Baseline | Epic 0, Task 003.c | Inference event logging |
| Security Posture | Epic 0, Task 003 | Tool call security validation |

---

## Outcomes

1. Unified model provider interface that abstracts runtime differences
2. Ollama provider fully functional with streaming and tool calls
3. vLLM provider fully functional with structured outputs
4. All tool calls validated against JSON Schema before execution
5. Prompt packs loadable, versionable, and selectable via config
6. Request routing assigns appropriate models to task types
7. Fallback mechanisms handle provider failures gracefully
8. Usage metrics tracked for all inference operations
9. Streaming responses handled correctly with partial results
10. Retry logic handles transient failures and malformed outputs
11. Health checks verify provider availability before operations
12. Setup documentation enables quick provider configuration
13. Smoke tests verify basic functionality after setup
14. Tool schema errors produce actionable feedback to models
15. Prompt packs include starter templates for common frameworks
16. Routing overrides allow user customization
17. All inference events are auditable per Task 003.c
18. Token usage reported for cost/resource tracking
19. Model responses cacheable where appropriate
20. Error messages provide clear debugging information

---

## Non-Goals

1. Supporting external LLM APIs (OpenAI, Anthropic, Google, etc.)
2. Implementing model quantization or optimization
3. Managing GPU memory allocation
4. Downloading or updating model files
5. Training or fine-tuning models
6. Implementing RAG (Retrieval Augmented Generation)
7. Vector database integration
8. Multi-modal inputs (images, audio)
9. Concurrent multi-model inference
10. Real-time model switching during operation
11. Custom model architectures
12. ONNX or other runtime formats (beyond Ollama/vLLM)
13. Cloud-based model hosting
14. Model performance benchmarking
15. Automatic prompt optimization
16. A/B testing of prompts
17. Model output caching across sessions
18. Federation with remote model endpoints
19. Custom tokenizers
20. Embedding generation (separate from chat completions)

---

## Architecture & Integration Points

### Component Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     AgenticCoder.Application                     │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                    Model Orchestrator                        ││
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────────────────┐││
│  │  │   Router    │ │  Retry      │ │  Usage Tracker          │││
│  │  │   Policy    │ │  Handler    │ │                         │││
│  │  └─────────────┘ └─────────────┘ └─────────────────────────┘││
│  └─────────────────────────────────────────────────────────────┘│
│                              │                                   │
│  ┌───────────────────────────┴─────────────────────────────────┐│
│  │                 IModelProvider Interface                     ││
│  └───────────────────────────┬─────────────────────────────────┘│
└──────────────────────────────┼───────────────────────────────────┘
                               │
┌──────────────────────────────┼───────────────────────────────────┐
│              AgenticCoder.Infrastructure                         │
│  ┌───────────────────────────┴─────────────────────────────────┐│
│  │                  Provider Registry                           ││
│  │  ┌─────────────────┐              ┌─────────────────┐       ││
│  │  │ OllamaProvider  │              │  vLLMProvider   │       ││
│  │  │                 │              │                 │       ││
│  │  │ - Streaming     │              │ - Structured    │       ││
│  │  │ - Tool Parse    │              │ - Health Check  │       ││
│  │  │ - Retry Logic   │              │ - Load Balance  │       ││
│  │  └────────┬────────┘              └────────┬────────┘       ││
│  └───────────┼────────────────────────────────┼─────────────────┘│
│              │                                │                  │
│  ┌───────────┴────────────────────────────────┴─────────────────┐│
│  │                  Tool Schema Registry                        ││
│  │  ┌─────────────────────────────────────────────────────────┐ ││
│  │  │  JSON Schema Validator  │  Error → Retry Contract       │ ││
│  │  └─────────────────────────────────────────────────────────┘ ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
│  ┌──────────────────────────────────────────────────────────────┐│
│  │                  Prompt Pack System                          ││
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────────────────┐ ││
│  │  │   Loader    │ │  Validator  │ │  Version Hash           │ ││
│  │  └─────────────┘ └─────────────┘ └─────────────────────────┘ ││
│  └──────────────────────────────────────────────────────────────┘│
└──────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

```csharp
// Core model provider contract
public interface IModelProvider
{
    string ProviderId { get; }
    Task<bool> IsAvailableAsync(CancellationToken ct);
    Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct);
    IAsyncEnumerable<StreamingChunk> StreamAsync(ChatRequest request, CancellationToken ct);
}

// Tool schema registry contract
public interface IToolSchemaRegistry
{
    void Register(ToolDefinition tool);
    ToolDefinition? Get(string toolName);
    ValidationResult Validate(string toolName, JsonElement arguments);
    IReadOnlyList<ToolDefinition> GetAll();
}

// Prompt pack contract
public interface IPromptPackLoader
{
    PromptPack Load(string packId);
    IReadOnlyList<PromptPackInfo> ListAvailable();
    string ComputeHash(PromptPack pack);
}

// Routing policy contract
public interface IModelRouter
{
    ProviderConfiguration Route(TaskContext context);
    ProviderConfiguration GetFallback(ProviderConfiguration failed);
}
```

### Data Contracts

```csharp
// Chat message types
public record ChatMessage(MessageRole Role, string Content, IReadOnlyList<ToolCall>? ToolCalls);
public enum MessageRole { System, User, Assistant, Tool }

// Tool call types
public record ToolCall(string Id, string Name, JsonElement Arguments);
public record ToolResult(string ToolCallId, string Result, bool IsError);

// Request/Response
public record ChatRequest(
    IReadOnlyList<ChatMessage> Messages,
    IReadOnlyList<ToolDefinition>? Tools,
    ModelParameters Parameters);

public record ChatResponse(
    ChatMessage Message,
    UsageInfo Usage,
    FinishReason FinishReason);

public record UsageInfo(int PromptTokens, int CompletionTokens, int TotalTokens);
```

### Events Published

| Event | Published By | Consumed By |
|-------|--------------|-------------|
| InferenceStarted | ModelOrchestrator | Audit, Metrics |
| InferenceCompleted | ModelOrchestrator | Audit, Metrics |
| InferenceFailed | ModelOrchestrator | Audit, Metrics, Retry |
| ToolCallValidated | ToolSchemaRegistry | Audit |
| ToolCallRejected | ToolSchemaRegistry | Audit, Retry |
| ProviderHealthChanged | HealthChecker | Router |
| PromptPackLoaded | PromptPackLoader | Audit |

---

## Operational Considerations

### Operating Mode Compliance

| Mode | Behavior |
|------|----------|
| Local-Only | All inference MUST use local providers. No network calls except to localhost. |
| Burst Mode | May queue requests for batch processing. Usage tracking required. |
| Air-Gapped | All resources MUST be pre-loaded. No network at all, not even localhost. |

### Safety Requirements

1. Tool calls MUST be validated before execution
2. Malformed tool calls MUST NOT be executed
3. Infinite retry loops MUST be prevented (max 3 retries)
4. Provider timeouts MUST be enforced
5. Streaming MUST be interruptible
6. Large responses MUST be truncated per limits

### Audit Requirements

All inference operations MUST be audited per Task 003.c:
- Request/response timestamps
- Token usage
- Provider used
- Tool calls made
- Validation errors
- Retry events

### Resource Limits

| Resource | Limit | Behavior on Exceed |
|----------|-------|-------------------|
| Request timeout | 120s default, configurable | Cancel with error |
| Max tokens per request | Model-dependent | Truncate input |
| Max tool calls per response | 10 | Log warning, process first 10 |
| Retry attempts | 3 | Fail with aggregated errors |
| Streaming buffer | 1MB | Flush to consumer |

---

## Acceptance Criteria / Definition of Done

### Model Provider Interface (Task 004)

- [ ] IModelProvider interface defined
- [ ] ChatMessage types defined
- [ ] ToolCall types defined
- [ ] ChatRequest type defined
- [ ] ChatResponse type defined
- [ ] UsageInfo type defined
- [ ] MessageRole enum defined
- [ ] FinishReason enum defined
- [ ] Streaming interface defined
- [ ] Provider registry implemented
- [ ] Config selection logic works
- [ ] Multiple providers can be registered
- [ ] Provider selection by ID works
- [ ] Default provider configurable

### Ollama Provider (Task 005)

- [ ] OllamaProvider implements IModelProvider
- [ ] Connection to Ollama API works
- [ ] Chat completion works
- [ ] Streaming works
- [ ] Tool call parsing works
- [ ] Invalid JSON retry works
- [ ] Timeout handling works
- [ ] Connection error handling works
- [ ] Setup documentation complete
- [ ] Smoke test script works

### vLLM Provider (Task 006)

- [ ] vLLMProvider implements IModelProvider
- [ ] Connection to vLLM API works
- [ ] Chat completion works
- [ ] Streaming works
- [ ] Structured outputs work
- [ ] Health check endpoint works
- [ ] Load information available
- [ ] Error handling complete
- [ ] Setup documentation complete
- [ ] Smoke test script works

### Tool Schema Registry (Task 007)

- [ ] IToolSchemaRegistry interface defined
- [ ] JSON Schema validation works
- [ ] All core tools have schemas
- [ ] Validation errors are clear
- [ ] Retry contract defined
- [ ] Truncation rules defined
- [ ] Artifact attachment rules defined
- [ ] Invalid tool calls rejected
- [ ] Valid tool calls pass

### Prompt Pack System (Task 008)

- [ ] Prompt pack file layout defined
- [ ] Hashing mechanism works
- [ ] Versioning mechanism works
- [ ] Loader implemented
- [ ] Validator implemented
- [ ] Config selection works
- [ ] Starter packs created
- [ ] dotnet pack works
- [ ] react pack works
- [ ] minimal-diff pack works

### Model Routing Policy (Task 009)

- [ ] IModelRouter interface defined
- [ ] Planner role routing works
- [ ] Coder role routing works
- [ ] Reviewer role routing works
- [ ] Heuristics applied correctly
- [ ] User overrides work
- [ ] Fallback rules work
- [ ] Escalation rules work
- [ ] Routing is auditable

### Cross-Cutting

- [ ] All providers are auditable
- [ ] Usage tracking works
- [ ] Error handling consistent
- [ ] Timeouts enforced
- [ ] Retry logic correct
- [ ] Config loading works
- [ ] Operating modes respected
- [ ] Unit tests complete
- [ ] Integration tests complete
- [ ] Documentation complete

---

## Risks & Mitigations

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Ollama API changes | Medium | High | Pin version, abstract API calls |
| vLLM API changes | Medium | High | Pin version, abstract API calls |
| Tool call parsing fragile | High | High | Comprehensive test suite, retry logic |
| Streaming buffer overflow | Low | Medium | Configurable limits, backpressure |
| Provider unavailable | Medium | High | Health checks, fallback providers |
| Token counting inaccurate | Medium | Low | Use provider-reported tokens |
| Prompt injection via tools | Low | Critical | Schema validation, sanitization |
| Model returns invalid JSON | High | Medium | Retry with feedback, max attempts |
| Infinite retry loops | Medium | High | Max retry limit, exponential backoff |
| Memory pressure from large responses | Low | Medium | Streaming, truncation limits |
| Config errors prevent startup | Medium | Medium | Validation with clear messages |
| Prompt pack corruption | Low | Medium | Hash verification, fallback |
| Router picks wrong model | Medium | Medium | Override mechanism, logging |
| Provider timeout too short | Medium | Medium | Configurable timeouts |

---

## Milestone Plan

### Milestone 1: Provider Foundation (Tasks 004, 005.a)

**Deliverables:**
- IModelProvider interface complete
- All message and tool types defined
- Ollama basic request/response working
- Streaming implemented

**Exit Criteria:**
- Can send prompt to Ollama and receive response
- Streaming works end-to-end
- Types are fully defined and tested

### Milestone 2: Ollama Complete (Tasks 005.b, 005.c)

**Deliverables:**
- Tool call parsing complete
- Retry on invalid JSON complete
- Setup documentation
- Smoke test script

**Exit Criteria:**
- Tool calls work reliably
- Retry logic handles common failures
- New developer can set up in < 30 minutes

### Milestone 3: vLLM Complete (Task 006)

**Deliverables:**
- vLLM provider complete
- Structured outputs working
- Health checks working
- Documentation complete

**Exit Criteria:**
- vLLM provider passes same tests as Ollama
- Structured output enforcement verified
- Health endpoint integration working

### Milestone 4: Tool Validation (Task 007)

**Deliverables:**
- Tool schema registry complete
- All core tool schemas defined
- Validation working
- Error → retry contract defined

**Exit Criteria:**
- All tool calls validated before execution
- Invalid calls rejected with clear feedback
- Model can retry on validation failure

### Milestone 5: Prompt System (Task 008)

**Deliverables:**
- Prompt pack system complete
- Starter packs created
- Selection via config working

**Exit Criteria:**
- Prompts load correctly
- Version hashing works
- Can switch packs via config

### Milestone 6: Routing Complete (Task 009)

**Deliverables:**
- Routing policy complete
- All role routing working
- Fallback and escalation working

**Exit Criteria:**
- Requests route to correct providers
- Fallbacks handle failures
- Routing is auditable

### Milestone 7: Epic Integration

**Deliverables:**
- All components integrated
- End-to-end tests passing
- Documentation complete

**Exit Criteria:**
- Full agentic workflow uses model layer
- All tests passing
- Ready for Epic 2

---

## Definition of Epic Complete

- [ ] All tasks (004-009) complete
- [ ] All acceptance criteria met
- [ ] IModelProvider interface stable
- [ ] Ollama provider production-ready
- [ ] vLLM provider production-ready
- [ ] Tool schema registry complete
- [ ] All core tools have schemas
- [ ] Prompt pack system complete
- [ ] Starter packs available
- [ ] Routing policy complete
- [ ] Fallback mechanisms working
- [ ] All unit tests passing
- [ ] All integration tests passing
- [ ] All E2E tests passing
- [ ] Performance benchmarks met
- [ ] Setup documentation complete
- [ ] API documentation complete
- [ ] Smoke tests passing
- [ ] Security review complete
- [ ] Audit integration verified
- [ ] Config loading verified
- [ ] Operating mode compliance verified
- [ ] Code review complete
- [ ] No critical bugs outstanding
- [ ] Ready for Epic 2 integration

---

**END OF EPIC 1**
