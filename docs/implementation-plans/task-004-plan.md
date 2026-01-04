# Task 004 Implementation Plan

## Task: Model Provider Interface

**Status:** In Progress
**Branch:** `feature/task-004-model-provider-interface`
**Date Started:** 2026-01-03

---

## Overview

Task 004 implements the Model Provider Interface - the central abstraction for communicating with local language model runtimes (Ollama, vLLM). This includes all message types, request/response contracts, usage reporting, and provider registry.

### Subtasks

1. **Task 004a:** Define Message/Tool-Call Types (ChatMessage, ToolCall, ToolResult, MessageRole)
2. **Task 004b:** Define Response Format and Usage Reporting (ChatResponse, UsageInfo, StreamingChunk)
3. **Task 004c:** Provider Registry and Config Selection (IProviderRegistry, ProviderDescriptor)

---

## Strategic Approach

### Phase 1: Foundation Types (Task 004a)
**Domain Layer - Core message/tool types**

1. ✅ Create Domain layer Models/Inference folder structure
2. ✅ Define MessageRole enum
3. ✅ Define ToolCall record
4. ✅ Define ToolResult record
5. ✅ Define ToolDefinition record
6. ✅ Define ToolCallDelta record
7. ✅ Define ChatMessage record with factory methods
8. ✅ Create comprehensive tests for all types

### Phase 2: Response Types (Task 004b)
**Domain Layer - Response and usage types**

1. ✅ Define FinishReason enum (25 tests)
2. ✅ Define UsageInfo record (18 tests)
3. ✅ Define ResponseMetadata record (16 tests)
4. ✅ Define ChatResponse record (17 tests)
5. ✅ Define ResponseDelta record (12 tests)
6. ✅ Define DeltaAccumulator class (12 tests)
7. 🔄 Define ResponseBuilder class (IN PROGRESS)
8. - Create comprehensive tests for all types

### Phase 3: Provider Interface (Task 004)
**Application Layer - Provider contract**

1. ✅ Define IModelProvider interface
2. ✅ Define ChatRequest record
3. ✅ Define ModelParameters record
4. ✅ Define ProviderCapabilities record
5. ✅ Create tests for all types

### Phase 4: Provider Registry (Task 004c)
**Application + Infrastructure - Registry implementation**

1. ✅ Define IProviderRegistry interface (Application)
2. ✅ Define ProviderDescriptor record
3. ✅ Define ProviderType enum
4. ✅ Define ProviderEndpoint record
5. ✅ Define ProviderConfig record
6. ✅ Define RetryPolicy record
7. ✅ Define ProviderHealth record
8. ✅ Define HealthStatus enum
9. ✅ Implement ProviderRegistry (Infrastructure)
10. ✅ Create comprehensive tests

### Phase 5: Integration & Documentation
1. ✅ Integration tests
2. ✅ Documentation
3. ✅ Audit
4. ✅ PR creation

---

## Progress Tracking

### Completed ✅
- (None yet - starting fresh)

### In Progress 🔄
- Task 004a: Message/Tool-Call Types

### Remaining ⏳
- Task 004b: Response Format/Usage
- Task 004c: Provider Registry
- Integration & Documentation

---

## Dependencies

- **Task 001:** Operating Modes (OperatingMode enum)
- **Task 002:** Config Contract (.agent/config.yml structure)
- **Task 003:** Audit/Security (audit event types, protected paths)

---

## File Structure

```
src/Acode.Domain/
├── Models/
│   └── Inference/
│       ├── MessageRole.cs
│       ├── ChatMessage.cs
│       ├── ToolCall.cs
│       ├── ToolResult.cs
│       ├── ToolDefinition.cs
│       ├── ToolCallDelta.cs
│       ├── FinishReason.cs
│       ├── UsageInfo.cs
│       ├── ResponseMetadata.cs
│       ├── ChatResponse.cs
│       ├── StreamingChunk.cs
│       └── ModelParameters.cs

src/Acode.Application/
├── Inference/
│   ├── IModelProvider.cs
│   ├── IProviderRegistry.cs
│   ├── ChatRequest.cs
│   ├── ProviderCapabilities.cs
│   ├── ProviderDescriptor.cs
│   ├── ProviderType.cs
│   ├── ProviderEndpoint.cs
│   ├── ProviderConfig.cs
│   ├── RetryPolicy.cs
│   ├── ProviderHealth.cs
│   ├── HealthStatus.cs
│   └── DeltaAccumulator.cs

src/Acode.Infrastructure/
└── Inference/
    └── ProviderRegistry.cs

tests/Acode.Domain.Tests/
└── Models/
    └── Inference/
        ├── MessageRoleTests.cs
        ├── ChatMessageTests.cs
        ├── ToolCallTests.cs
        ├── ToolResultTests.cs
        ├── ToolDefinitionTests.cs
        ├── ToolCallDeltaTests.cs
        ├── FinishReasonTests.cs
        ├── UsageInfoTests.cs
        ├── ResponseMetadataTests.cs
        ├── ChatResponseTests.cs
        └── StreamingChunkTests.cs

tests/Acode.Application.Tests/
└── Inference/
    ├── ChatRequestTests.cs
    ├── ModelParametersTests.cs
    ├── ProviderCapabilitiesTests.cs
    ├── ProviderDescriptorTests.cs
    ├── ProviderEndpointTests.cs
    ├── ProviderConfigTests.cs
    ├── RetryPolicyTests.cs
    ├── ProviderHealthTests.cs
    └── DeltaAccumulatorTests.cs

tests/Acode.Infrastructure.Tests/
└── Inference/
    └── ProviderRegistryTests.cs
```

---

## Test Strategy

### Unit Tests
- **Domain Types:** Immutability, validation, equality, serialization
- **Application Interfaces:** Contract enforcement
- **Infrastructure Registry:** Registration, lookup, health checks, fallback

### Integration Tests
- Provider registry with mock providers
- Streaming accumulation end-to-end
- Config loading → registry setup

---

## Key Design Decisions

1. **Immutable Records:** All message/response types are immutable records for thread safety and value equality
2. **Nullable Reference Types:** Enabled for compile-time null safety
3. **Factory Methods:** ChatMessage has factory methods for ergonomic construction
4. **Streaming Support:** IAsyncEnumerable<StreamingChunk> for streaming responses
5. **Provider Agnostic:** All types work with Ollama, vLLM, or future providers
6. **Clean Architecture:** Domain types have no dependencies, Application defines interfaces, Infrastructure implements

---

## Validation Rules

- **MessageRole:** Must be valid enum value
- **ChatMessage:** Content OR ToolCalls must be non-null for Assistant; Content required for User/System/Tool
- **ToolCall:** Id and Name must be non-empty; Name max 64 chars, alphanumeric+underscore only
- **UsageInfo:** All token counts non-negative
- **ProviderDescriptor:** Id must be unique across registry
- **ProviderEndpoint:** BaseUrl must be valid URI, timeouts positive
- **RetryPolicy:** MaxRetries >= 0, delays positive

---

## Acceptance Criteria Highlights

- [ ] All 115 FRs from Task 004 parent implemented
- [ ] All 70 FRs from Task 004a implemented
- [ ] All 93 FRs from Task 004b implemented
- [ ] All 84 FRs from Task 004c implemented
- [ ] All types immutable and thread-safe
- [ ] All types support JSON serialization
- [ ] Provider registry supports registration/lookup/health checks
- [ ] Streaming responses work with accumulator
- [ ] All tests passing (TDD: Red-Green-Refactor)
- [ ] Build 0 errors, 0 warnings
- [ ] Documentation complete

---

## TDD Workflow

For each type:
1. **RED:** Write failing test
2. **GREEN:** Implement minimal code to pass
3. **REFACTOR:** Clean up while keeping tests green
4. **COMMIT:** One logical commit per feature

---

## Notes

- Follow strict TDD per docs/TDD_INSTRUCTIONS.md
- Commit after each logical unit
- Update this plan as work progresses
- Task 004 NOT complete until all subtasks (004a, 004b, 004c) are done

---

**Last Updated:** 2026-01-03
**Next Step:** Create feature branch and begin Task 004a implementation
