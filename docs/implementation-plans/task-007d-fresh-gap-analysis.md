# Task 007d (formerly 005b) - Tool Call Parsing + Retry - FRESH GAP ANALYSIS

**Date**: 2026-01-13
**Analysis Type**: Fresh from-scratch gap analysis per CLAUDE.md Section 3.2
**Spec Source**: docs/tasks/refined-tasks/Epic 01/task-007d-tool-call-parsing-retry-on-invalid-json.md (4356 lines)

---

## METHODOLOGY

Per CLAUDE.md Section 3.2, this is a FRESH gap analysis performed by:

1. Reading the full task specification (task-007d, formerly task-005b)
2. Reading the Implementation Prompt section (lines 3172+) for all expected files
3. Reading the Testing Requirements section (lines 1506+) for all expected tests
4. Verifying what ACTUALLY exists in the codebase now
5. Creating a checklist of ONLY what's MISSING or INCOMPLETE

This analysis IGNORES any previous completion checklists or session work. We're starting from zero.

---

## STEP 1: WHAT THE SPEC REQUIRES

### Functional Requirements Summary (87 total: FR-001 through FR-087)

**Core Components Required:**
1. ToolCallParser - Extract tool calls from Ollama responses
2. JsonRepairer - Auto-fix malformed JSON (9 heuristics)
3. SchemaValidator - Validate arguments against JSON Schema
4. ToolCallRetryHandler - Re-prompt model on failures
5. StreamingToolCallAccumulator - Assemble tool calls from stream fragments

**Models Required:**
- OllamaToolCall + OllamaFunction (Ollama format)
- RepairResult (repair outcome)
- ToolCallParseResult (parsing outcome with errors/successes)
- ToolCallError (error details with error codes)
- ValidationResult + ValidationError (schema validation)
- RetryResult (retry outcome)
- ToolCallDelta (streaming fragments)
- RetryConfig (retry configuration)

**Exceptions Required:**
- ToolCallParseException
- ToolCallValidationException
- ToolCallRetryExhaustedException

**Integration Points:**
- IToolSchemaRegistry (Task 007) - for schema lookup
- OllamaResponseMapper - integrate parser
- OllamaDeltaMapper - integrate streaming accumulator

---

## STEP 2: VERIFY WHAT EXISTS

### Implementation Files Found

✅ **src/Acode.Infrastructure/Ollama/ToolCall/ToolCallParser.cs** - EXISTS (basic parsing, NO schema validation)
✅ **src/Acode.Infrastructure/Ollama/ToolCall/JsonRepairer.cs** - EXISTS
❌ **src/Acode.Infrastructure/Ollama/ToolCall/SchemaValidator.cs** - MISSING
✅ **src/Acode.Infrastructure/Ollama/ToolCall/ToolCallRetryHandler.cs** - EXISTS
✅ **src/Acode.Infrastructure/Ollama/ToolCall/StreamingToolCallAccumulator.cs** - EXISTS

### Model Files Found

✅ **OllamaToolCall.cs** + **OllamaFunction.cs** - EXISTS
✅ **RepairResult.cs** - EXISTS
✅ **ToolCallParseResult.cs** - EXISTS
✅ **ToolCallError.cs** - EXISTS
❌ **ValidationResult.cs** - MISSING
❌ **ValidationError.cs** - MISSING
❌ **RetryResult.cs** - MISSING
✅ **ToolCallDelta.cs** - EXISTS
✅ **RetryConfig.cs** - EXISTS

### Exception Files Found

✅ **ToolCallParseException.cs** - EXISTS
✅ **ToolCallValidationException.cs** - EXISTS
✅ **ToolCallRetryExhaustedException.cs** - EXISTS

### Test Files Found

✅ **ToolCallParserTests.cs** - EXISTS (18 tests)
✅ **JsonRepairerTests.cs** - EXISTS (16 tests)
❌ **SchemaValidatorTests.cs** - MISSING (SchemaValidator doesn't exist)
✅ **ToolCallRetryHandlerTests.cs** - EXISTS (10 tests)
✅ **StreamingToolCallAccumulator Tests.cs** - EXISTS
✅ **ToolCallIntegrationTests.cs** - EXISTS (8 tests)

---

## STEP 3: IDENTIFY GAPS

### ❌ **GAP #1: SchemaValidator Component Missing**

**What's Required** (FR-057 through FR-063):
- Separate SchemaValidator class
- Integration with IToolSchemaRegistry (Task 007)
- Validate arguments against tool's JSON Schema
- Distinguish parse errors vs validation errors
- Return ValidationResult with ValidationError details

**What Exists**:
- ToolCallParser does basic parsing and JSON repair
- NO schema validation
- NO IToolSchemaRegistry integration
- NO ValidationResult/ValidationError types

**Impact**: CRITICAL - Tool calls with syntactically valid but semantically invalid arguments are not caught

**Dependencies**: Requires IToolSchemaRegistry from Task 007

**Implementation Needed**:
1. Create ValidationResult.cs and ValidationError.cs models
2. Create SchemaValidator.cs component
3. Integrate IToolSchemaRegistry
4. Modify ToolCallParser to use SchemaValidator
5. Add SchemaValidatorTests.cs (minimum 15 tests per spec)

---

### ❌ **GAP #2: IToolSchemaRegistry Integration in ToolCallParser**

**What's Required** (FR-015, FR-016, FR-058):
- ToolCallParser MUST validate function.name matches known tools
- ToolCallParser MUST reject tool calls for unknown tools
- Parser MUST use IToolSchemaRegistry for schemas

**What Exists**:
- ToolCallParser validates name format (alphanumeric + underscore)
- ToolCallParser validates name length (max 64 chars)
- NO registry lookup to check if tool exists
- NO schema retrieval for validation

**Impact**: HIGH - Unknown tools are not detected, invalid arguments pass through

**Dependencies**: Requires IToolSchemaRegistry from Task 007

**Implementation Needed**:
1. Add IToolSchemaRegistry parameter to ToolCallParser constructor
2. Check if tool exists in registry before parsing
3. Return error ACODE-TLP-005 for unknown tools
4. Pass schema to SchemaValidator for argument validation

---

### ❌ **GAP #3: RetryResult Model Missing**

**What's Required** (from Implementation Prompt section):
- RetryResult model to track retry attempts
- Include: Success, AttemptCount, FinalResult, TokensUsed, etc.

**What Exists**:
- ToolCallRetryHandler exists but may not return detailed retry results
- No RetryResult model file

**Impact**: MEDIUM - Retry metrics not properly tracked/exposed

**Implementation Needed**:
1. Create RetryResult.cs model
2. Update ToolCallRetryHandler to return RetryResult
3. Include token usage tracking across retries (FR-050)

---

### ❌ **GAP #4: Streaming Tool Call End-to-End Integration Test Missing**

**What's Required** (from Testing Requirements, Integration Tests section):
- Test 4: EndToEnd_StreamingToolCalls_AccumulateAndParse
- Verify complete streaming pipeline: chunks → accumulator → parser → domain

**What Exists**:
- ToolCallIntegrationTests.cs has 8 tests
- Tests cover non-streaming scenarios
- NO streaming end-to-end test

**Impact**: MEDIUM - Streaming path not verified end-to-end

**Implementation Needed**:
1. Add streaming integration test to ToolCallIntegrationTests.cs
2. Test: OllamaStreamChunk[] → StreamingToolCallAccumulator → ToolCallParser → ToolCall[]
3. Verify tool calls correctly assembled from fragments

---

### ❌ **GAP #5: Smoke Test Integration (FR-082 through FR-087)**

**What's Required**:
- FR-082: Tool calling smoke test MUST be implemented when 007d completes
- FR-083: Verify tool call extraction from response
- FR-084: Verify JSON argument parsing succeeds
- FR-085: Verify tool call mapped to ToolCall type
- FR-086: Test added to scripts/smoke-tests/ollama-smoke-test.sh
- FR-087: Integrate with `acode providers smoke-test ollama` command

**What Exists**:
- Task 005c implemented a stub: "Requires Task 007d - skipped"
- Stub needs to be replaced with actual implementation

**Impact**: LOW - Smoke test is for validation, not core functionality

**Implementation Needed**:
1. Update ollama-smoke-test.sh to test tool calling
2. Remove "Requires Task 007d - skipped" stub
3. Add real tool call smoke test scenario

---

### ⚠️ **GAP #6: Schema Validation Tests in ToolCallParserTests**

**What's Required** (from Testing Requirements):
- ToolCallParserTests should include schema validation scenarios
- Test: Should_Reject_Invalid_Arguments_Against_Schema
- Test: Should_Provide_Validation_Error_Details
- Test: Should_Distinguish_Parse_vs_Validation_Errors

**What Exists**:
- ToolCallParserTests.cs has 18 tests
- Tests cover basic parsing, JSON repair, format validation
- NO schema validation tests (because schema validation doesn't exist)

**Impact**: MEDIUM - Schema validation path not tested

**Implementation Needed**:
1. Add schema validation test scenarios to ToolCallParserTests.cs
2. Tests should use mocked IToolSchemaRegistry
3. Verify validation errors are returned correctly

---

## STEP 4: PRIORITIZED IMPLEMENTATION ORDER

Following TDD (tests first) and dependency order:

### **Phase 1: Schema Validation Foundation (CRITICAL)**

1. **Create ValidationResult and ValidationError models** (Gap #3 partial)
   - File: src/Acode.Infrastructure/Ollama/ToolCall/Models/ValidationResult.cs
   - File: src/Acode.Infrastructure/Ollama/ToolCall/Models/ValidationError.cs
   - Models to represent schema validation outcomes

2. **Create SchemaValidator component** (Gap #1)
   - File: src/Acode.Infrastructure/Ollama/ToolCall/SchemaValidator.cs
   - Takes IToolSchemaRegistry, validates JsonElement against schema
   - Returns ValidationResult with detailed errors

3. **Write SchemaValidator Tests FIRST** (Gap #1, TDD)
   - File: tests/Acode.Infrastructure.Tests/Ollama/ToolCall/SchemaValidatorTests.cs
   - Minimum 15 tests covering all validation scenarios
   - RED phase - tests fail because SchemaValidator not yet complete

4. **Implement SchemaValidator to pass tests** (Gap #1)
   - GREEN phase - make tests pass
   - REFACTOR phase - clean up code

### **Phase 2: Integrate Schema Validation into Parser (CRITICAL)**

5. **Modify ToolCallParser to accept IToolSchemaRegistry** (Gap #2)
   - Add constructor parameter
   - Lookup tool in registry before parsing
   - Return error for unknown tools

6. **Integrate SchemaValidator into ToolCallParser** (Gap #1 + Gap #2)
   - Call SchemaValidator after JSON parsing succeeds
   - Return validation errors in ToolCallParseResult
   - Distinguish parse errors vs validation errors

7. **Add schema validation tests to ToolCallParserTests** (Gap #6)
   - Update existing test file
   - Add 5-8 new tests for schema validation scenarios
   - Test unknown tools, invalid arguments, validation error details

8. **Update ToolCallRetryHandler for validation errors** (Gap #3 partial)
   - Ensure retry works for both parse AND validation failures
   - Build retry prompt with validation error context

### **Phase 3: Retry Result Tracking (MEDIUM)**

9. **Create RetryResult model** (Gap #3)
   - File: src/Acode.Infrastructure/Ollama/ToolCall/Models/RetryResult.cs
   - Track attempt count, token usage, final outcome

10. **Update ToolCallRetryHandler to return RetryResult** (Gap #3)
    - Change method signature
    - Populate retry metrics
    - Update tests to verify RetryResult

### **Phase 4: Integration & E2E (MEDIUM)**

11. **Add streaming integration test** (Gap #4)
    - Update ToolCallIntegrationTests.cs
    - Test: EndToEnd_StreamingToolCalls_AccumulateAndParse
    - Verify complete streaming pipeline

12. **Update smoke test for tool calling** (Gap #5)
    - Update scripts/smoke-tests/ollama-smoke-test.sh
    - Remove stub, add real tool call test
    - Verify smoke test command works

---

## STEP 5: COMPLETION CRITERIA

Task 007d is COMPLETE when ALL of the following are true:

### Code Completeness
- [ ] SchemaValidator.cs exists and implements full JSON Schema validation
- [ ] ValidationResult.cs and ValidationError.cs models exist
- [ ] RetryResult.cs model exists
- [ ] ToolCallParser integrates IToolSchemaRegistry
- [ ] ToolCallParser uses SchemaValidator for argument validation
- [ ] ToolCallParser rejects unknown tools (error ACODE-TLP-005)
- [ ] ToolCallRetryHandler returns RetryResult with metrics

### Test Completeness
- [ ] SchemaValidatorTests.cs exists with ≥15 tests, all passing
- [ ] ToolCallParserTests.cs includes ≥5 schema validation tests, all passing
- [ ] ToolCallIntegrationTests.cs includes streaming E2E test, passing
- [ ] All existing tests remain passing (currently 196/196)

### Integration Completeness
- [ ] OllamaResponseMapper integrates schema-validated parser
- [ ] OllamaDeltaMapper integrates streaming accumulator
- [ ] Smoke test updated for tool calling (FR-082 through FR-087)

### Documentation Completeness
- [ ] Error code docs include validation errors
- [ ] Configuration docs include schema validation settings
- [ ] Audit report updated to reflect 100% task completion

### Build Quality
- [ ] Build succeeds with 0 errors, 0 warnings
- [ ] All tests passing
- [ ] No NotImplementedException in code
- [ ] XML documentation complete

---

## STEP 6: COMPARISON WITH PREVIOUS SESSION

**What previous session claimed was complete:**
- 13 of 13 gaps (100%)
- All streaming integration done (Gaps #5, #8)
- 196/196 tests passing

**What this FRESH analysis reveals:**
- **Schema validation completely missing** (FR-057 through FR-063 not implemented)
- **IToolSchemaRegistry not integrated** (FR-058 violated)
- **Unknown tool detection not working** (FR-015, FR-016 not implemented)
- **Validation models missing** (ValidationResult, ValidationError, RetryResult)
- **SchemaValidator component doesn't exist** - this is a REQUIRED component per spec
- **Schema validation tests don't exist** (15+ tests required per spec)

**Critical Finding:**
The previous session focused on the wrong scope - it implemented task-005b (basic parsing + retry) but NOT task-007d (which requires schema validation integration with Task 007's IToolSchemaRegistry). Task 007d has ADDITIONAL requirements beyond 005b.

---

## DEPENDENCIES ON OTHER TASKS

**Task 007** (Tool Schema Registry):
- **Status**: Should be complete (this is 007d, a subtask of 007)
- **Provides**: IToolSchemaRegistry interface
- **Needed For**: Gaps #1 and #2 (schema validation)

**Task 004.a** (ToolCall Type):
- **Status**: Complete
- **Provides**: Domain ToolCall type
- **Used By**: Parser output

**Task 005** (Ollama Provider Adapter):
- **Status**: Complete
- **Provides**: OllamaResponse types, HTTP layer
- **Used By**: Parser input

---

## NEXT STEPS

1. Verify that Task 007 (IToolSchemaRegistry) is complete and available
2. If Task 007 is complete, begin Phase 1 immediately (schema validation)
3. If Task 007 is NOT complete, this task is BLOCKED until 007 completes
4. Create fresh TODO list based on prioritized gaps above
5. Follow TDD strictly: tests first, then implementation

