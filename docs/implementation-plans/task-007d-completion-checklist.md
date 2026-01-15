# Task 007d - Tool Call Parsing + Retry + Schema Validation - 100% Completion Checklist

## 🎯 CURRENT STATUS (2026-01-13)

**Implementation Status**: TDD RED phase complete (tests written), GREEN phase needed
- 🔄 Phase 1 (TDD RED): COMPLETE - 6 schema validation tests written (commit: 8e83c0a)
- ⏳ Phase 2 (TDD GREEN): PENDING - Feature implementation needed to pass tests
- ⏳ Phase 3 (Update instantiations): PENDING
- ⏳ Phase 4 (Audit): PENDING

**Tests Status**:
- Current: 196 passing (baseline before schema validation)
- Target: 210+ passing (196 existing + 6 new schema + others)

**Critical Gaps Identified** (from verified gap analysis):
1. ❌ IToolSchemaRegistry NOT integrated into ToolCallParser
2. ❌ Unknown tool checking NOT implemented (FR-015, FR-016)
3. ❌ Schema validation NOT implemented (FR-057 through FR-063)
4. ❌ Error codes ACODE-TLP-005, ACODE-TLP-006 NOT implemented

**Key Dependencies**:
- ✅ Task 007 (IToolSchemaRegistry) - COMPLETE (23/23 tests passing)
- ✅ Domain SchemaValidationError - EXISTS
- ✅ Domain ErrorSeverity - EXISTS

---

## INSTRUCTIONS FOR FRESH CONTEXT AGENT

**Your Mission**: Complete task-007d (Tool Call Parsing + Retry + Schema Validation) to 100% specification compliance.

**Task Context**:
- This is subtask 'd' of Task 007 (Tool Schema Registry)
- Previously implemented as "task-005b" (basic parsing) but missing schema validation requirements
- Task 007 provides IToolSchemaRegistry interface - now integrate it

**Current Status**:
- Core tool call parsing EXISTS and works (18 tests passing)
- JSON repair EXISTS and works (16 tests passing)
- Retry handler EXISTS and works (10 tests passing)
- Streaming accumulator EXISTS and works (15 tests passing)
- **MISSING**: Schema validation integration with IToolSchemaRegistry

**How to Use This File**:
1. Read entire file first to understand scope (~ 1-2 hours to complete all phases)
2. Work through sections sequentially (Phase 1 already done, start at Phase 2)
3. For each item:
   - Mark as [🔄] when starting work
   - Implement following TDD (tests already written in Phase 1!)
   - Run tests to verify (`dotnet test --filter "ToolCallParserTests"`)
   - Mark as [✅] when complete with evidence (paste test output)
4. Update this file after EACH completed phase (not at end)
5. Commit after each phase complete

**Status Legend**:
- `[ ]` = TODO (not started)
- `[🔄]` = IN PROGRESS (actively working on this)
- `[✅]` = COMPLETE (implemented + tested + verified)

**Critical Rules** (from CLAUDE.md):
- NO deferrals - implement everything in spec
- NO placeholders - full implementations only
- NO "TODO" comments in production code
- TESTS FIRST - already written in Phase 1 (RED), now implement (GREEN)
- VERIFY SEMANTICALLY - tests must actually validate the FR, not just pass

**Context Limits**:
- If context runs low (<10k tokens), commit all work and update this file
- Mark items [🔄] with details of what's partially done
- Next session picks up from this file

**Files You'll Modify**:
- src/Acode.Infrastructure/Ollama/ToolCall/ToolCallParser.cs (ADD registry integration)
- src/Acode.Infrastructure/Ollama/ToolCall/ToolCallRetryHandler.cs (handle validation errors)
- tests/Acode.Infrastructure.Tests/Ollama/ToolCall/ToolCallParserTests.cs (DONE - tests written)
- Multiple files that instantiate ToolCallParser (update constructor calls)

**Spec Reference**:
- Task file: docs/tasks/refined-tasks/Epic 01/task-007d-tool-call-parsing-retry-on-invalid-json.md (4356 lines)
- Functional Requirements: FR-001 through FR-087
- Testing Requirements section: lines 1506-3171
- Implementation Prompt section: lines 3172-4356

**Gap Analysis References**:
- Verified gap analysis: docs/implementation-plans/task-007d-gap-analysis-verified.md
- Fresh gap analysis: docs/implementation-plans/task-007d-fresh-gap-analysis.md

---

## PHASE 1: TDD RED - Write Tests (ALREADY COMPLETE ✅)

**Status**: [✅] COMPLETE (commit: 8e83c0a)

**What Was Done**:
- Added 6 comprehensive schema validation tests to ToolCallParserTests.cs
- Tests cover: unknown tool rejection, schema validation success/failure, multiple errors, partial success
- Tests use NSubstitute to mock IToolSchemaRegistry
- Helper method created: `CreateParserWithRegistry(out IToolSchemaRegistry registry)`

**Tests Written** (lines 468-732 in ToolCallParserTests.cs):
1. `Parse_UnknownTool_ReturnsErrorACODETLP005` - FR-016
2. `Parse_KnownToolWithValidArguments_CallsSchemaValidationAndSucceeds` - FR-057
3. `Parse_SchemaValidationFailure_ReturnsErrorACODETLP006` - FR-059
4. `Parse_MultipleValidationErrors_CombinesIntoSingleToolCallError` - FR-060-062
5. `Parse_PartialSuccess_ReturnsSuccessfulCallsAndValidationErrors` - mixed scenarios

**Current Build Status**:
- Build FAILS (expected - RED phase)
- Error: CS1739 - ToolCallParser does not have parameter named 'schemaRegistry'

**Evidence**:
```bash
$ git log -1 --oneline
8e83c0a test(task-007d): add schema validation tests (TDD RED)
```

---

## PHASE 2: TDD GREEN - Implement Feature to Pass Tests

This phase implements the feature to make the tests pass. Follow these steps sequentially.

### P2.1: Modify ToolCallParser Constructor to Accept IToolSchemaRegistry

**Status**: [✅] COMPLETE

**Problem**:
- ToolCallParser currently has no IToolSchemaRegistry parameter
- Tests expect: `new ToolCallParser(schemaRegistry: registry)`
- FR-058: Parser MUST use IToolSchemaRegistry for schemas

**Location**: src/Acode.Infrastructure/Ollama/ToolCall/ToolCallParser.cs:14-44

**What Changed**:
1. Added IToolSchemaRegistry field at line 23:
```csharp
private readonly IToolSchemaRegistry? schemaRegistry;
```

2. Added using statement at top of file (line 5):
```csharp
using Acode.Application.Tools;
```

3. Updated constructor signature and body (lines 27-44):
```csharp
/// <summary>
/// Initializes a new instance of the <see cref="ToolCallParser"/> class.
/// </summary>
/// <param name="repairer">The JSON repairer to use. If null, creates a default repairer.</param>
/// <param name="idGenerator">Function to generate IDs for tool calls missing an ID.</param>
/// <param name="schemaRegistry">
/// The schema registry for tool validation. If null, schema validation is skipped.
/// When provided, enables FR-015, FR-016 (unknown tool checking) and FR-057 through FR-063 (schema validation).
/// </param>
public ToolCallParser(
    JsonRepairer? repairer = null,
    Func<string>? idGenerator = null,
    IToolSchemaRegistry? schemaRegistry = null)
{
    this.repairer = repairer ?? new JsonRepairer();
    this.idGenerator = idGenerator ?? this.DefaultIdGenerator;
    this.schemaRegistry = schemaRegistry;
}
```

**Why Optional?**:
- Makes it backward compatible with existing code
- Allows gradual rollout - existing instantiations still work
- Schema validation only happens when registry provided
- Tests explicitly pass registry, production code will too

**Success Criteria**:
- [✅] IToolSchemaRegistry field added to class
- [✅] Constructor accepts schemaRegistry parameter (optional, defaults to null)
- [✅] Using statement added for Acode.Application.Tools
- [✅] Build succeeds (no CS1739 error)
- [✅] Tests still FAIL (expected - feature not implemented yet)

**Evidence**:
```bash
$ dotnet build tests/Acode.Infrastructure.Tests/Acode.Infrastructure.Tests.csproj
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:38.62

$ dotnet test tests/Acode.Infrastructure.Tests/Acode.Infrastructure.Tests.csproj --filter "ToolCallParserTests"
Total tests: 23
     Passed: 18
     Failed: 5
# 5 schema validation tests failing (expected - feature not yet implemented)
```

---

### P2.2: Implement Unknown Tool Checking (FR-015, FR-016)

**Status**: [ ]

**Problem**:
- Parser doesn't check if tool exists in registry
- FR-015: Parser MUST validate function.name matches known tools
- FR-016: Parser MUST reject tool calls for unknown tools
- Test expects error code: ACODE-TLP-005

**Location**: src/Acode.Infrastructure/Ollama/ToolCall/ToolCallParser.cs, in ParseSingle method

**Current ParseSingle Method** (starts at line 78):
```csharp
private SingleParseResult ParseSingle(OllamaToolCall ollamaCall)
{
    // Validate function exists
    if (ollamaCall.Function == null)
    {
        return SingleParseResult.WithError(new ToolCallError(
            "Tool call is missing function definition",
            "ACODE-TLP-001"));
    }

    var function = ollamaCall.Function;
    var toolName = function.Name;

    // Validate function name is not empty
    if (string.IsNullOrWhiteSpace(toolName))
    {
        return SingleParseResult.WithError(new ToolCallError(
            "Tool call has empty function name",
            "ACODE-TLP-002"));
    }

    // ... rest of method continues with format validation, JSON parsing, etc.
}
```

**What to Add** (after the "empty function name" check, around line ~100):
```csharp
// FR-015, FR-016: Check if tool is registered (if registry available)
if (this.schemaRegistry != null && !this.schemaRegistry.IsRegistered(toolName))
{
    return SingleParseResult.WithError(new ToolCallError(
        $"Unknown tool '{toolName}'. Tool is not registered in the schema registry.",
        "ACODE-TLP-005")
    {
        ToolName = toolName,
    });
}
```

**Placement**:
- AFTER the name format/length validation (lines ~100-120)
- BEFORE the JSON parsing logic (current lines ~120-160)
- This way we fail fast for unknown tools before parsing arguments

**Error Code Definition**:
- ACODE-TLP-005: Unknown tool
- Should be documented in docs/error-codes/ollama-tool-call-errors.md (verify it's there)

**How to Test**:
```bash
# Run the unknown tool test
dotnet test --filter "Parse_UnknownTool_ReturnsErrorACODETLP005" --verbosity normal
# Expect: Test PASSES (1/1)

# Verify existing tests still pass
dotnet test --filter "ToolCallParserTests" --verbosity normal
# Expect: 19/24 passing (18 old + 1 new, 5 still failing for validation logic)
```

**Success Criteria**:
- [ ] Unknown tool check added after name validation
- [ ] Check only runs if schemaRegistry is not null
- [ ] Returns error ACODE-TLP-005 with tool name
- [ ] Test `Parse_UnknownTool_ReturnsErrorACODETLP005` PASSES
- [ ] All 18 existing tests still PASS (backward compatible)

**Evidence**: [To be filled when complete]

---

### P2.3: Implement Schema Validation After JSON Parsing (FR-057 through FR-063)

**Status**: [ ]

**Problem**:
- Parser doesn't validate arguments against tool's JSON Schema
- FR-057: Parser MUST validate arguments against tool's JSON Schema
- FR-059: Parser MUST distinguish parse errors from validation errors
- Tests expect error code: ACODE-TLP-006 for validation failures

**Location**: src/Acode.Infrastructure/Ollama/ToolCall/ToolCallParser.cs, in ParseSingle method

**Current Flow** (around lines 120-180):
1. Parse JSON arguments (with repair if needed)
2. Create ToolCall with parsed arguments
3. Return success

**What to Add** (AFTER successful JSON parsing, BEFORE creating ToolCall):

```csharp
// After successful JSON parsing (around line ~170):
JsonElement arguments = ... // Successfully parsed

// FR-057 through FR-063: Validate arguments against schema (if registry available)
if (this.schemaRegistry != null)
{
    var validationSuccess = this.schemaRegistry.TryValidateArguments(
        toolName,
        arguments,
        out var validationErrors,
        out var validatedArguments);

    if (!validationSuccess)
    {
        // FR-059: Distinguish parse errors (ACODE-TLP-003) from validation errors (ACODE-TLP-006)
        var errorMessage = validationErrors.Count == 1
            ? $"Schema validation failed for tool '{toolName}': {validationErrors.First().Message}"
            : $"Schema validation failed for tool '{toolName}' with {validationErrors.Count} validation errors:\n" +
              string.Join("\n", validationErrors.Select(e => $"  - {e.Path}: {e.Message}"));

        return SingleParseResult.WithError(new ToolCallError(
            errorMessage,
            "ACODE-TLP-006")
        {
            ToolName = toolName,
            RawArguments = function.Arguments,
        });
    }

    // Use validated arguments (may have defaults applied)
    arguments = validatedArguments;
}

// Continue with creating ToolCall using validated arguments
```

**Key Points**:
- Only validate if schemaRegistry is not null
- Call TryValidateArguments (from IToolSchemaRegistry interface)
- SchemaValidationError collection → single ToolCallError with combined message
- Error code: ACODE-TLP-006 (schema validation failure)
- Format multiple errors: "X validation errors:\n  - path1: msg1\n  - path2: msg2"

**Using Statement Needed** (top of file):
```csharp
using Acode.Domain.Tools;  // For SchemaValidationError
```

**How to Test**:
```bash
# Run schema validation tests
dotnet test --filter "Parse_SchemaValidationFailure" --verbosity normal
# Expect: 1/1 passing

dotnet test --filter "Parse_KnownToolWithValidArguments" --verbosity normal
# Expect: 1/1 passing

dotnet test --filter "Parse_MultipleValidationErrors" --verbosity normal
# Expect: 1/1 passing

dotnet test --filter "Parse_PartialSuccess" --verbosity normal
# Expect: 1/1 passing

# Run all ToolCallParser tests
dotnet test --filter "ToolCallParserTests" --verbosity normal
# Expect: All 24 tests passing (18 old + 6 new)
```

**Success Criteria**:
- [ ] Schema validation added after JSON parsing
- [ ] Only runs if schemaRegistry is not null
- [ ] Returns error ACODE-TLP-006 for validation failures
- [ ] Combines multiple validation errors into single message
- [ ] Uses validated arguments when validation succeeds
- [ ] All 6 new schema validation tests PASS
- [ ] All 18 existing tests still PASS

**Evidence**: [To be filled when complete]

---

### P2.4: Verify Error Code Documentation Exists

**Status**: [ ]

**Problem**: Need to verify error codes ACODE-TLP-005 and ACODE-TLP-006 are documented

**Location**: docs/error-codes/ollama-tool-call-errors.md

**What to Check**:
1. Read the error codes file
2. Verify ACODE-TLP-005 (Unknown tool) is documented
3. Verify ACODE-TLP-006 (Schema validation failure) is documented
4. If missing, add them following the existing pattern

**Expected Entries**:

**ACODE-TLP-005: Unknown Tool**
- **Severity**: Error (blocks tool execution)
- **Cause**: Tool name in response doesn't match any registered tool in IToolSchemaRegistry
- **Example**: `Unknown tool 'invalid_tool'. Tool is not registered in the schema registry.`
- **Resolution**:
  - Verify tool is registered with IToolSchemaRegistry
  - Check tool name spelling matches exactly
  - Ensure tool registration happens before parsing responses
- **Related FRs**: FR-015, FR-016

**ACODE-TLP-006: Schema Validation Failure**
- **Severity**: Error (blocks tool execution)
- **Cause**: Tool arguments don't match the tool's JSON Schema (missing required fields, type mismatches, constraint violations)
- **Example**: `Schema validation failed for tool 'read_file': Property 'path' is required`
- **Resolution**:
  - Review validation error details in message
  - Ensure model produces arguments matching tool schema
  - May trigger automatic retry with error context
- **Related FRs**: FR-057 through FR-063

**How to Verify**:
```bash
# Check if error codes file exists
ls -lh docs/error-codes/ollama-tool-call-errors.md

# Search for the error codes
grep "ACODE-TLP-005\|ACODE-TLP-006" docs/error-codes/ollama-tool-call-errors.md

# If missing, add them following the pattern of existing error codes
```

**Success Criteria**:
- [ ] Error code file exists
- [ ] ACODE-TLP-005 documented with cause, example, resolution
- [ ] ACODE-TLP-006 documented with cause, example, resolution
- [ ] Documentation follows existing format

**Evidence**: [To be filled when complete]

---

## PHASE 3: Update All ToolCallParser Instantiations

This phase updates all code that creates ToolCallParser instances to pass IToolSchemaRegistry.

### P3.1: Find All ToolCallParser Instantiations

**Status**: [ ]

**What to Do**:
```bash
# Find all instantiations of ToolCallParser
grep -r "new ToolCallParser" --include="*.cs" src/ tests/
# Review each location to determine if it needs registry

# Common locations to check:
# - OllamaResponseMapper (non-streaming responses)
# - OllamaDeltaMapper (streaming responses)
# - Integration tests (may need mocked registry)
# - Any factories or dependency injection configuration
```

**Expected Locations**:
1. src/Acode.Infrastructure/Ollama/Mapping/OllamaResponseMapper.cs
2. Possibly in StreamingToolCallAccumulator or related streaming code
3. Test files (already updated in Phase 1)

**Success Criteria**:
- [ ] All instantiation locations identified
- [ ] List documented here for next step

**Evidence**: [To be filled when complete]

---

### P3.2: Update OllamaResponseMapper to Pass IToolSchemaRegistry

**Status**: [ ]

**Problem**: OllamaResponseMapper creates ToolCallParser without registry

**Location**: src/Acode.Infrastructure/Ollama/Mapping/OllamaResponseMapper.cs

**What to Change**:
1. Add IToolSchemaRegistry field to OllamaResponseMapper
2. Update constructor to accept IToolSchemaRegistry
3. Pass registry when creating ToolCallParser
4. Update all OllamaResponseMapper instantiations

**How to Implement**:
```csharp
// Add field
private readonly IToolSchemaRegistry schemaRegistry;

// Update constructor
public OllamaResponseMapper(IToolSchemaRegistry schemaRegistry)
{
    this.schemaRegistry = schemaRegistry;
}

// When creating parser
var parser = new ToolCallParser(schemaRegistry: this.schemaRegistry);
```

**Dependency Injection**:
- If using DI, register IToolSchemaRegistry as singleton
- OllamaResponseMapper should receive it via constructor injection

**How to Test**:
```bash
# Build should succeed
dotnet build

# Run integration tests
dotnet test --filter "ToolCallIntegrationTests" --verbosity normal
# Expect: All integration tests passing
```

**Success Criteria**:
- [ ] OllamaResponseMapper accepts IToolSchemaRegistry in constructor
- [ ] Registry passed to ToolCallParser when created
- [ ] All instantiations of OllamaResponseMapper updated
- [ ] Integration tests still pass

**Evidence**: [To be filled when complete]

---

### P3.3: Update Other Instantiations (If Any)

**Status**: [ ]

**Problem**: May be other places creating ToolCallParser

**What to Do**:
- Review list from P3.1
- Update each location following same pattern as P3.2
- Ensure registry is available in context or injected

**Success Criteria**:
- [ ] All instantiations updated
- [ ] No compilation errors
- [ ] All tests pass

**Evidence**: [To be filled when complete]

---

## PHASE 4: Integration Testing and Verification

### P4.1: Run Full Test Suite

**Status**: [ ]

**What to Do**:
```bash
# Run ALL tests
dotnet test --verbosity normal

# Specifically check:
# 1. ToolCallParserTests (should be 24/24 passing now)
dotnet test --filter "ToolCallParserTests" --verbosity normal

# 2. Integration tests
dotnet test --filter "ToolCallIntegrationTests" --verbosity normal

# 3. All Ollama-related tests
dotnet test --filter "FullyQualifiedName~Ollama" --verbosity normal
```

**Expected Results**:
- ToolCallParserTests: 24/24 (18 old + 6 new)
- ToolCallIntegrationTests: 8/8
- All Ollama tests: 100% pass rate
- Total test count: 202+ (196 baseline + 6 new)

**Success Criteria**:
- [ ] All ToolCallParserTests passing (24/24)
- [ ] All integration tests passing
- [ ] No test failures anywhere
- [ ] Test count increased by 6

**Evidence**: [To be filled when complete]

---

### P4.2: Build Verification

**Status**: [ ]

**What to Do**:
```bash
# Clean build
dotnet clean
dotnet build --no-incremental

# Check for warnings
# Expect: 0 errors, 0 warnings
```

**Success Criteria**:
- [ ] Build succeeds
- [ ] 0 errors
- [ ] 0 warnings (StyleCop violations)

**Evidence**: [To be filled when complete]

---

### P4.3: Manual Verification of FR Coverage

**Status**: [ ]

**Critical FRs to Verify**:

**FR-015**: Parser MUST validate function.name matches known tools
- [ ] Implementation: P2.2 (unknown tool checking)
- [ ] Test: Parse_UnknownTool_ReturnsErrorACODETLP005
- [ ] Verified: Unknown tools are rejected

**FR-016**: Parser MUST reject tool calls for unknown tools
- [ ] Implementation: P2.2 (unknown tool checking)
- [ ] Test: Parse_UnknownTool_ReturnsErrorACODETLP005
- [ ] Verified: Returns error ACODE-TLP-005

**FR-057**: Parser MUST validate arguments against tool's JSON Schema
- [ ] Implementation: P2.3 (schema validation)
- [ ] Test: Parse_KnownToolWithValidArguments_CallsSchemaValidationAndSucceeds
- [ ] Verified: TryValidateArguments called for each tool

**FR-058**: Parser MUST use IToolSchemaRegistry for schemas
- [ ] Implementation: P2.1 (constructor integration)
- [ ] Test: All schema validation tests use registry
- [ ] Verified: IToolSchemaRegistry field exists and is used

**FR-059**: Parser MUST distinguish parse errors from validation errors
- [ ] Implementation: P2.3 (different error codes)
- [ ] Test: Parse_SchemaValidationFailure_ReturnsErrorACODETLP006
- [ ] Verified: Parse errors use ACODE-TLP-003, validation errors use ACODE-TLP-006

**FR-060**: Validation errors MUST include path to invalid field
- [ ] Implementation: P2.3 (SchemaValidationError has Path property)
- [ ] Test: Parse_SchemaValidationFailure checks error message contains path
- [ ] Verified: Error message includes "/path: Property 'path' is required"

**FR-061**: Validation errors MUST include expected vs actual type
- [ ] Implementation: SchemaValidationError already has ExpectedType, ActualValue
- [ ] Test: Implicitly tested through schema validation
- [ ] Verified: IToolSchemaRegistry provides this info

**FR-062**: Validation errors MUST include missing required fields
- [ ] Implementation: P2.3 (error message shows missing fields)
- [ ] Test: Parse_MultipleValidationErrors checks combined message
- [ ] Verified: Error lists all missing required properties

**FR-063**: Validation MUST enforce strict mode (no extra properties)
- [ ] Implementation: Handled by IToolSchemaRegistry (Task 007)
- [ ] Test: Implicitly tested if registry configured for strict mode
- [ ] Verified: Schema validation enforces all constraints

**Success Criteria**:
- [ ] All 9 critical FRs implemented
- [ ] Each FR has corresponding test
- [ ] Each FR verified manually

**Evidence**: [To be filled when complete]

---

## PHASE 5: Commit and Documentation

### P5.1: Commit Schema Validation Implementation

**Status**: [ ]

**What to Do**:
```bash
# Stage all changes
git add -A

# Commit with detailed message
git commit -m "feat(task-007d): implement schema validation (TDD GREEN)

Integrate IToolSchemaRegistry into ToolCallParser for schema validation:

FR-015, FR-016: Unknown tool checking
- Check if tool registered before parsing
- Return error ACODE-TLP-005 for unknown tools

FR-057 through FR-063: Schema validation
- Validate arguments against tool's JSON Schema
- Distinguish parse errors (TLP-003) from validation errors (TLP-006)
- Combine multiple validation errors into single message
- Use validated arguments (may have defaults applied)

Changes:
- ToolCallParser: Added IToolSchemaRegistry constructor parameter
- ParseSingle: Added unknown tool check (FR-015, FR-016)
- ParseSingle: Added schema validation after JSON parsing (FR-057-063)
- OllamaResponseMapper: Updated to pass registry to parser
- Error codes: TLP-005 (unknown tool), TLP-006 (validation failure)

Test Results:
- All 24 ToolCallParserTests passing (18 old + 6 new)
- All 8 ToolCallIntegrationTests passing
- Total: 202/202 tests passing

Verified FRs:
- FR-015, FR-016: Unknown tool rejection ✅
- FR-057: Arguments validated against schema ✅
- FR-058: IToolSchemaRegistry used ✅
- FR-059: Parse vs validation errors distinguished ✅
- FR-060 through FR-063: Validation error details ✅

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"

# Push to remote
git push origin feature/task-005b-tool-output-capture
```

**Success Criteria**:
- [ ] Changes committed
- [ ] Commit message follows Conventional Commits
- [ ] Commit pushed to remote

**Evidence**: [To be filled when complete]

---

### P5.2: Update Gap Analysis Document

**Status**: [ ]

**What to Do**:
1. Open docs/implementation-plans/task-007d-gap-analysis-verified.md
2. Update status for completed gaps:
   - Gap #1 (SchemaValidator): Actually not needed - IToolSchemaRegistry does validation
   - Gap #2 (IToolSchemaRegistry integration): COMPLETE
3. Add evidence section with test results
4. Update completion percentage

**Success Criteria**:
- [ ] Gap analysis marked complete
- [ ] Evidence added
- [ ] Completion: 100% (all critical gaps closed)

**Evidence**: [To be filled when complete]

---

### P5.3: Update This Completion Checklist

**Status**: [ ]

**What to Do**:
1. Mark all Phase 2-5 items as [✅]
2. Add evidence sections with test output
3. Update status at top: Implementation COMPLETE
4. Commit the updated checklist

**Success Criteria**:
- [ ] All phases marked complete
- [ ] Evidence filled in
- [ ] Checklist committed

**Evidence**: [To be filled when complete]

---

## PHASE 6: Final Audit (Per AUDIT-GUIDELINES.md)

### P6.1: Fresh Gap Analysis (Avoid Confirmation Bias)

**Status**: [ ]

**Problem**: Need to verify nothing was missed

**What to Do**:
1. Re-read task spec Implementation Prompt section (lines 3172-4356)
2. Re-read Testing Requirements section (lines 1506-3171)
3. Create NEW gap analysis from scratch (ignore previous analyses)
4. Look for anything missed

**Success Criteria**:
- [ ] Fresh gap analysis completed
- [ ] 0 gaps found (or document any found and implement)

**Evidence**: [To be filled when complete]

---

### P6.2: Run Audit Checklist

**Status**: [ ]

**What to Do**:
Follow docs/AUDIT-GUIDELINES.md line by line:

1. **Build Quality**:
   ```bash
   dotnet build
   # Expect: 0 errors, 0 warnings
   ```

2. **Test Coverage**:
   ```bash
   dotnet test
   # Expect: 202+ tests, 100% pass rate
   ```

3. **NotImplementedException Scan**:
   ```bash
   grep -r "NotImplementedException" src/Acode.Infrastructure/Ollama/ToolCall/
   # Expect: NO MATCHES
   ```

4. **TODO Comment Scan**:
   ```bash
   grep -r "TODO\|FIXME\|HACK" src/Acode.Infrastructure/Ollama/ToolCall/
   # Expect: NO MATCHES or only benign comments
   ```

5. **FR Coverage Verification**:
   - [ ] FR-015: Known tools only ✅
   - [ ] FR-016: Unknown tools rejected ✅
   - [ ] FR-057 through FR-063: Schema validation ✅

6. **Layer Boundaries**:
   - [ ] No Domain types in Infrastructure (using Application interface)
   - [ ] No circular dependencies

**Success Criteria**:
- [ ] All audit checks pass
- [ ] No violations found

**Evidence**: [To be filled when complete]

---

### P6.3: Create Audit Report

**Status**: [ ]

**What to Do**:
Create docs/audits/task-007d-audit-report.md with:
1. Summary of implementation
2. Test results
3. FR coverage verification
4. Audit checklist results
5. Conclusion: PASS or FAIL

**Success Criteria**:
- [ ] Audit report created
- [ ] All sections filled
- [ ] Result: PASS

**Evidence**: [To be filled when complete]

---

## PHASE 7: Create Pull Request

### P7.1: Update PR Description

**Status**: [ ]

**What to Do**:
Update existing PR #43 description with:
1. Summary of schema validation implementation
2. Test results (202+ tests passing)
3. FR coverage (FR-015, FR-016, FR-057 through FR-063)
4. Examples of validation errors
5. Link to audit report

**Success Criteria**:
- [ ] PR description updated
- [ ] Comprehensive and clear

**Evidence**: [To be filled when complete]

---

### P7.2: Final Verification Before Merge

**Status**: [ ]

**Checklist**:
- [ ] All tests passing (202+/202+)
- [ ] Build: 0 errors, 0 warnings
- [ ] Audit report: PASS
- [ ] Fresh gap analysis: 0 gaps
- [ ] No NotImplementedException
- [ ] No TODO comments
- [ ] All commits pushed
- [ ] PR updated

**When ALL checked**, task is COMPLETE and ready for merge.

---

## COMPLETION SUMMARY

**When This Checklist is 100% Complete**:
- ✅ Schema validation fully integrated
- ✅ Unknown tools rejected (FR-015, FR-016)
- ✅ Schema validation working (FR-057 through FR-063)
- ✅ All tests passing (202+)
- ✅ Audit: PASS
- ✅ Documentation complete
- ✅ PR ready for merge

**Final Step**: Mark PR as ready for review and merge to main.

---

**END OF CHECKLIST**
