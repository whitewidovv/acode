# Task 007a - 100% Completion Checklist
## JSON Schema Definitions for All Core Tools

## INSTRUCTIONS FOR FRESH CONTEXT AGENT

**Your Mission**: Complete task-007a (JSON Schema Definitions for All Core Tools) to 100% specification compliance - all 17 schemas must be semantically complete with comprehensive tests.

**Current Status**:
- **Structural Completion**: ~85% (all 17 schemas exist)
- **Semantic Completion**: ~60% (bugs and gaps found)
- **Test Coverage**: ~10% (11 tests vs 108+ needed)
- **Critical Bug Found**: WriteFileSchema.create_directories has wrong default (true instead of false)

**Gap Analysis**: See docs/implementation-plans/task-007a-gap-analysis.md for detailed findings

**How to Use This File**:
1. Read entire file first (~1800 lines)
2. Read task spec: docs/tasks/refined-tasks/Epic 01/task-007a-json-schema-definitions-for-all-core-tools.md (4250+ lines)
3. Re-read CLAUDE.md Section 3.2 (Gap Analysis and Completion Checklist)
4. Work through Phases 0-5 sequentially
5. For each gap:
   - Mark as [🔄] when starting
   - Follow TDD strictly: RED → GREEN → REFACTOR
   - Run tests after each change
   - Mark as [✅] when complete with evidence
6. Update this file after EACH completed item
7. Commit after each logical unit
8. When context low (<10k tokens): commit, update progress, stop

**Status Legend**:
- `[ ]` = TODO (not started)
- `[🔄]` = IN PROGRESS (actively working)
- `[✅]` = COMPLETE (implemented + tested + verified)

**Critical Rules** (CLAUDE.md Section 3):
- NO deferrals - implement EVERYTHING
- NO placeholders - full implementations only
- TESTS FIRST - RED before GREEN always
- SEMANTIC COMPLETENESS - not just presence
- Commit frequently - after each logical unit

**Key Spec Sections**:
- Implementation Prompt: lines 3048-4249 (complete code examples)
- Testing Requirements: lines 549-604 (test files and counts)
- Acceptance Criteria: lines 429-546 (what must work)
- Functional Requirements: lines 479+ (all FR-XXX items)
- User Verification Steps: lines 2737-3044 (CLI validation commands)

---

## PHASE 0: VERIFY CURRENT STATE AND IDENTIFY ISSUES

**Goal**: Read all existing schemas, identify bugs, document semantic gaps.

### Gap 0.1: Verify All 17 Schemas Exist

**Status**: [ ]

**Files to Check**:
- [ ] ReadFileSchema.cs
- [ ] WriteFileSchema.cs
- [ ] ListDirectorySchema.cs
- [ ] SearchFilesSchema.cs
- [ ] DeleteFileSchema.cs
- [ ] MoveFileSchema.cs
- [ ] ExecuteCommandSchema.cs
- [ ] ExecuteScriptSchema.cs
- [ ] SemanticSearchSchema.cs
- [ ] FindSymbolSchema.cs
- [ ] GetDefinitionSchema.cs
- [ ] GitStatusSchema.cs
- [ ] GitDiffSchema.cs
- [ ] GitLogSchema.cs
- [ ] GitCommitSchema.cs
- [ ] AskUserSchema.cs
- [ ] ConfirmActionSchema.cs

**Success Criteria**:
- [ ] All 17 files exist and can be read
- [ ] No NotImplementedException in any file
- [ ] CoreToolsProvider correctly yields all 17 tools
- [ ] All schemas follow same pattern (SchemaBuilder usage)

---

### Gap 0.2: Document Known Semantic Issues

**Status**: [ ]

**Known Issues** (from gap analysis):

1. **WriteFileSchema.create_directories** - DEFAULT VALUE WRONG
   - File: src/Acode.Infrastructure/ToolSchemas/Providers/Schemas/FileOperations/WriteFileSchema.cs (line 29)
   - Current: `defaultValue: true`
   - Should be: `defaultValue: false` (per spec line 499)
   - Impact: HIGH - This is semantic incorrectness

2. **Description quality across all schemas**
   - Current: Descriptions are 30-50 chars
   - Spec requirement: 50-200 chars
   - Examples: Include usage context, constraints, defaults

3. **Missing parameter constraints** (need detailed review per schema)

**Task**: Read each schema and document any semantic gaps vs spec requirements.

---

## PHASE 1: FIX CRITICAL BUGS

**Goal**: Fix WriteFileSchema default value and other critical issues.
**ACs Affected**: AC-xxx (those depending on write_file defaults)

### Gap 1.1: Fix WriteFileSchema.create_directories Default Value

**Status**: [ ]

**File**: src/Acode.Infrastructure/ToolSchemas/Providers/Schemas/FileOperations/WriteFileSchema.cs (line 29)

**TDD Steps**:

**RED**:
```csharp
[Fact]
public void WriteFileSchema_CreateDirectories_Should_Default_To_False()
{
    // Arrange
    var schema = WriteFileSchema.CreateToolDefinition();
    var defaultValue = schema.Parameters.GetProperty("properties")
        .GetProperty("create_directories")
        .GetProperty("default");

    // Act & Assert
    defaultValue.GetBoolean().Should().BeFalse("spec says default: false");
}
```

Run: `dotnet test --filter "CreateDirectories_Should_Default_To_False"`
Expected: RED (currently returns true)

**GREEN**:

Change line 29 from:
```csharp
defaultValue: true),  // ❌ WRONG
```

To:
```csharp
defaultValue: false),  // ✅ CORRECT
```

Also fix the description (line 28):
```csharp
// Old: "Create parent directories if they don't exist (default: true)"
// New: "Create parent directories if they don't exist (default: false)"
```

Run test: Expected GREEN

**Success Criteria**:
- [ ] Test passes (default is false)
- [ ] Description mentions "default: false"
- [ ] No other changes to WriteFileSchema

---

## PHASE 2: ENHANCE DESCRIPTION QUALITY

**Goal**: Improve all schema descriptions to 50-200 characters per spec requirement.
**ACs Affected**: AC-xxx (description quality requirements)

### Gap 2.1: ReadFileSchema - Enhance Descriptions

**Status**: [ ]

**File**: src/Acode.Infrastructure/ToolSchemas/Providers/Schemas/FileOperations/ReadFileSchema.cs

**Spec Requirements** (lines 490-492):
- path: "Path to the file to read. Can be absolute or relative to workspace root. Examples: 'README.md', 'src/Program.cs', '/home/user/file.txt'"
- start_line: "Line number to start reading from (1-indexed). First line of file is 1. Must be <= end_line when both are specified."
- end_line: "Line number to stop reading at (inclusive). Must be >= start_line when both are specified."
- encoding: "Text encoding for reading the file. Use 'utf-8' for most source code (default), 'ascii' for legacy files, 'utf-16' for Windows Unicode files."

**Current Descriptions** (too brief - 30-50 chars):
- path: "Path to the file to read (relative or absolute, e.g., 'src/main.cs')"
- start_line: "Line number to start reading from (1-indexed, optional)"
- end_line: "Line number to stop reading at, inclusive (must be >= start_line)"
- encoding: "Text encoding for reading the file (default: utf-8)"

**TDD Steps**:

**RED**:
```csharp
[Fact]
public void ReadFileSchema_Descriptions_Should_Meet_MinimumLength()
{
    var schema = ReadFileSchema.CreateToolDefinition();
    var properties = schema.Parameters.GetProperty("properties");

    foreach (var property in properties.EnumerateObject())
    {
        var description = property.Value.GetProperty("description").GetString();

        description!.Length.Should().BeGreaterThanOrEqualTo(30,
            $"Parameter '{property.Name}' description is too brief: '{description}'");
    }
}
```

Run test: Expected RED or mixed results (some descriptions OK, some too brief)

**GREEN**:

Update each property's description to match spec. Example:

```csharp
["path"] = SchemaBuilder.StringProperty(
    "Path to the file to read from the file system. Can be absolute or relative to workspace root. Examples: 'README.md', 'src/Program.cs', '/home/user/file.txt'",
    minLength: 1,
    maxLength: 4096),
```

Continue for all 4 parameters.

Run test: Expected GREEN

**Success Criteria**:
- [ ] All path descriptions 50-200 chars
- [ ] All descriptions include context and examples
- [ ] Parameter interdependencies documented (start_line must be <=  end_line)
- [ ] Test verifies minimum description length

---

### Gap 2.2: WriteFileSchema - Enhance Descriptions

**Status**: [ ]

**Similar approach to 2.1** - update descriptions per spec lines 495-502

**Spec Descriptions**:
- path: "Path where the file will be written. Creates file if it doesn't exist. Examples: 'output.txt', 'src/NewClass.cs'"
- content: "Content to write to the file. Maximum 1MB (1,048,576 characters). For larger files, write in multiple chunks."
- create_directories: "Create parent directories if they don't exist (default: false). Set to true when writing to new folder structures."
- overwrite: "Overwrite file if it already exists (default: true). Set to false to prevent accidental overwrites."
- encoding: "Text encoding for writing the file (default: utf-8)."

**Success Criteria**:
- [ ] All descriptions 50-200 chars
- [ ] Examples included where appropriate
- [ ] Tests pass

---

### Gap 2.3 through 2.7: Other Schemas

**Status**: [ ]

**Repeat for ListDirectorySchema, SearchFilesSchema, DeleteFileSchema, MoveFileSchema** - enhance all descriptions per spec lines 509-560

**List of Schemas to Update**:
- [ ] 2.3: ListDirectorySchema (spec lines 509-528)
- [ ] 2.4: SearchFilesSchema (spec lines 530-548)
- [ ] 2.5: DeleteFileSchema (spec lines 550-560)
- [ ] 2.6: MoveFileSchema (spec lines 562-575)
- [ ] 2.7: ExecuteCommandSchema (spec lines 577-604)
- [ ] 2.8: ExecuteScriptSchema (spec lines 606-625)
- [ ] 2.9: SemanticSearchSchema (spec lines 630-657)
- [ ] 2.10: FindSymbolSchema (spec lines 659-686)
- [ ] 2.11: GetDefinitionSchema (spec lines 688-704)
- [ ] 2.12: GitStatusSchema (spec lines 709-720)
- [ ] 2.13: GitDiffSchema (spec lines 722-740)
- [ ] 2.14: GitLogSchema (spec lines 742-760)
- [ ] 2.15: GitCommitSchema (spec lines 762-787)
- [ ] 2.16: AskUserSchema (spec lines 789-807)
- [ ] 2.17: ConfirmActionSchema (spec lines 809-825)

**Success Criteria for Each**:
- [ ] All descriptions 50-200 chars
- [ ] Examples included
- [ ] Constraints documented in descriptions
- [ ] Tests pass

---

## PHASE 3: VERIFY SCHEMA CONSTRAINTS

**Goal**: Ensure all constraints match spec exactly (minLength, maxLength, enum, defaults, minimum, maximum)
**ACs Affected**: AC-xxx (constraint enforcement)

### Gap 3.1: Systematic Constraint Verification

**Status**: [ ]

**Process**:
1. For each of 17 schemas:
   - Compare current constraints to spec
   - Document any mismatches
   - List missing constraints

**Example - ReadFileSchema**:

| Parameter | Spec Requirement | Current State | Match? |
|-----------|-----------------|---------------|--------|
| path | maxLength 4096 | ✅ maxLength: 4096 | ✅ |
| start_line | minimum 1 | ✅ minimum: 1 | ✅ |
| end_line | minimum 1 | ✅ minimum: 1 | ✅ |
| encoding | enum ["utf-8", "ascii", "utf-16"], default "utf-8" | ✅ | ✅ |

**All 17 Schemas to Verify** (spec lines 483-825):

**File Operations**:
- [ ] ReadFileSchema - verify all constraints (FR-001 through FR-007)
- [ ] WriteFileSchema - verify all constraints (FR-008 through FR-014)
- [ ] ListDirectorySchema - verify all constraints (FR-015 through FR-021)
- [ ] SearchFilesSchema - verify all constraints (FR-022 through FR-028)
- [ ] DeleteFileSchema - verify all constraints (FR-029 through FR-032)
- [ ] MoveFileSchema - verify all constraints (FR-033 through FR-037)

**Code Execution**:
- [ ] ExecuteCommandSchema - verify timeout bounds, command maxLength
- [ ] ExecuteScriptSchema - verify language enum, script maxLength

**Code Analysis**:
- [ ] SemanticSearchSchema - verify query bounds, scope enum
- [ ] FindSymbolSchema - verify symbol_name bounds
- [ ] GetDefinitionSchema - verify coordinate bounds

**Version Control**:
- [ ] GitStatusSchema - verify optional path
- [ ] GitDiffSchema - verify all optional parameters
- [ ] GitLogSchema - verify count bounds (1-100)
- [ ] GitCommitSchema - verify message bounds (1-500)

**User Interaction**:
- [ ] AskUserSchema - verify question bounds, options maxItems
- [ ] ConfirmActionSchema - verify action_description minLength (10)

**Success Criteria**:
- [ ] All constraints documented vs spec
- [ ] Any mismatches fixed
- [ ] New tests added for constraint violations

---

## PHASE 4: ADD COMPREHENSIVE UNIT TESTS

**Goal**: Add 100+ validation tests (spec requires 108+ minimum) for all schemas.
**ACs Affected**: AC-xxx (test coverage requirements)

### Gap 4.1: Create FileOperationsSchemaTests.cs

**Status**: [ ]

**File to Create**: tests/Acode.Infrastructure.Tests/ToolSchemas/Providers/Schemas/FileOperationsSchemaTests.cs

**Test Structure** (per spec pattern lines 2377-2536):

```csharp
public class FileOperationsSchemaTests
{
    // ReadFileSchema Tests
    [Fact]
    public void ReadFile_ValidArguments_Should_Pass_Validation()

    [Fact]
    public void ReadFile_MissingPath_Should_Fail()

    [Fact]
    public void ReadFile_WrongType_StartLine_Should_Fail()

    [Fact]
    public void ReadFile_ConstraintViolation_Path_Should_Fail()

    [Theory]
    [InlineData("utf-8")]
    [InlineData("ascii")]
    [InlineData("utf-16")]
    public void ReadFile_ValidEncoding_Should_Pass(string encoding)

    [Fact]
    public void ReadFile_InvalidEncoding_Should_Fail()

    [Fact]
    public void ReadFile_Examples_Should_Validate()

    // WriteFileSchema Tests
    [Fact]
    public void WriteFile_ValidArguments_Should_Pass()

    [Fact]
    public void WriteFile_CreateDirectories_Default_Should_Be_False()

    [Fact]
    public void WriteFile_Overwrite_Default_Should_Be_True()

    // ... etc for all file operation tools
}
```

**Per Tool** (6 file operation tools):
- Valid arguments test
- Missing required field test (per required field)
- Wrong type test (per parameter)
- Constraint violation test (per constraint: minLength, maxLength, enum, minimum, maximum)
- Default value test
- Examples validation test

**Estimated Tests**: 6 tools × 6 test categories = 36 tests minimum

---

### Gap 4.2: Create CodeExecutionSchemaTests.cs

**Status**: [ ]

**File to Create**: tests/Acode.Infrastructure.Tests/ToolSchemas/Providers/Schemas/CodeExecutionSchemaTests.cs

**Per Tool** (2 tools: execute_command, execute_script):
- Execute_Command: 6 test categories (valid, missing, wrong type, constraints, defaults, examples)
- Execute_Script: language enum tests, script maxLength tests

**Estimated Tests**: 2 tools × 7 test categories = 14 tests

---

### Gap 4.3: Create CodeAnalysisSchemaTests.cs

**Status**: [ ]

**Per Tool** (3 tools: semantic_search, find_symbol, get_definition):
- Query/symbol bounds tests
- Scope enum test (semantic_search)
- Include_references default test

**Estimated Tests**: 3 tools × 6 test categories = 18 tests

---

### Gap 4.4: Create VersionControlSchemaTests.cs

**Status**: [ ]

**Per Tool** (4 tools: git_status, git_diff, git_log, git_commit):
- git_log: count bounds test (1-100, rejection of 0 and 101)
- git_commit: message bounds test (minLength 1, maxLength 500)
- Files array validation test

**Estimated Tests**: 4 tools × 6 test categories = 24 tests

---

### Gap 4.5: Create UserInteractionSchemaTests.cs

**Status**: [ ]

**Per Tool** (2 tools: ask_user, confirm_action):
- ask_user: options maxItems test (10), default_option validation
- confirm_action: action_description minLength test (10)

**Estimated Tests**: 2 tools × 6 test categories = 12 tests

---

**Total Unit Tests from Gaps 4.1-4.5**: ~104 tests

**Success Criteria**:
- [ ] All test files created
- [ ] All tests pass
- [ ] Test coverage includes: valid, missing, wrong type, constraints, defaults, examples
- [ ] All constraint violations caught with specific error messages

---

## PHASE 5: ADD INTEGRATION AND PERFORMANCE TESTS

**Goal**: Add integration tests for registration and performance tests.
**ACs Affected**: AC-xxx (performance requirements)

### Gap 5.1: Create Integration Tests

**Status**: [ ]

**File to Create**: tests/Acode.Infrastructure.Tests/ToolSchemas/Integration/SchemaValidationIntegrationTests.cs

**Tests Required** (per spec lines 2549-2667):

```csharp
[Fact]
public void Should_Register_All_17_Core_Tools()
{
    // Verify all 17 tools can be registered and retrieved
}

[Fact]
public void Should_Validate_All_Example_Arguments()
{
    // For each tool, validate all its example arguments
    // Should pass validation
}

[Fact]
public void Should_Categorize_Tools_Correctly()
{
    // 6 file ops, 2 code exec, 3 code analysis, 4 version control, 2 user interaction
}

[Fact]
public void Schema_Compilation_Should_Complete_Under_500ms()
{
    // Verify all schemas compile in <500ms total
}
```

**Success Criteria**:
- [ ] 4+ integration tests pass
- [ ] All 17 tools register successfully
- [ ] All examples validate
- [ ] Compilation benchmark met

---

### Gap 5.2: Create Performance Tests

**Status**: [ ]

**File to Create**: tests/Acode.Infrastructure.Tests/ToolSchemas/Performance/SchemaValidationPerformanceTests.cs

**Tests Required** (per spec lines 2682-2732):

```csharp
[Fact]
public void SingleValidation_Should_Complete_Under_1ms()
{
    // Validate single tool arguments 100 times
    // Average time < 1ms per validation
}

[Fact]
public void All17Schemas_Validation_Should_Complete_Under_20ms()
{
    // Validate all 17 tools once each
    // Total time < 20ms
}
```

**Success Criteria**:
- [ ] Single validation <1ms average
- [ ] All 17 validations <20ms total
- [ ] Benchmarks documented

---

## PHASE 6: VERIFY ALL ACCEPTANCE CRITERIA

**Goal**: Systematically verify all 80+ acceptance criteria are met.
**ACs Affected**: All ACs (1-80+)

### Gap 6.1: AC Verification Matrix

**Status**: [ ]

**Process**:
Go through spec lines 429-546 (or equivalent AC section) and verify each AC:

**Example AC Verification**:

| AC# | Requirement | Verification Method | Status |
|-----|-------------|-------------------|--------|
| AC-001 | All 17 tools defined | Count tools in CoreToolsProvider | ✅ |
| AC-002 | Tools organized by category | GetToolsByCategory works | ✅ |
| AC-003 | read_file has path parameter | Schema has 'path' property | ✅ |
| AC-004 | path maxLength 4096 | Read spec, verify code | ✅ |
| AC-005 | write_file default overwrite true | Test write_file defaults | ✅ (after phase 1 fix) |
| ... | ... | ... | ... |

**All ACs to Verify**: Check against spec lines 429-546

**Success Criteria**:
- [ ] All 80+ ACs verified
- [ ] Any failed ACs identified and added to fixes
- [ ] Fixes implemented
- [ ] Re-verified after fixes

---

## PHASE 7: RUN FULL TEST SUITE

**Goal**: Ensure all tests pass and no regressions introduced.

### Gap 7.1: Run All Tests

**Status**: [ ]

**Commands**:
```bash
# Run all ToolSchemas tests
dotnet test --filter "FullyQualifiedName~ToolSchemas" --verbosity normal

# Expected: All ~130 tests pass (11 existing + 104 new unit + 4 integration + 2 performance + other)
```

**Success Criteria**:
- [ ] 0 test failures
- [ ] All tests pass
- [ ] Build succeeds with 0 errors, 0 warnings

---

### Gap 7.2: Verify Schema Examples

**Status**: [ ]

**Command**:
```bash
# For each tool, verify examples validate
acode tools validate read_file <<< '{"path": "README.md"}'
acode tools validate write_file <<< '{"path": "out.txt", "content": "hello"}'
acode tools validate execute_command <<< '{"command": "dotnet build"}'
# ... and so on for all 17 tools
```

**Success Criteria**:
- [ ] All 17 tool examples validate successfully
- [ ] CLI commands work correctly

---

## PHASE 8: DI REGISTRATION VERIFICATION

**Goal**: Ensure CoreToolsProvider is properly registered in DI container.

### Gap 8.1: Check DI Registration

**Status**: [ ]

**File**: src/Acode.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs

**What to Verify**:
- [ ] CoreToolsProvider registered as ISchemaProvider
- [ ] OR registered as IToolSchemaProvider
- [ ] Registered as Singleton (not Transient)
- [ ] No conflicts with other providers

**Expected Pattern** (per spec lines 4174-4191):
```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services)
{
    // ...existing registrations...

    // Register CoreToolsProvider
    services.AddSingleton<ISchemaProvider, CoreToolsProvider>();
    // OR
    services.AddSingleton<IToolSchemaProvider, CoreToolsProvider>();

    return services;
}
```

**Success Criteria**:
- [ ] CoreToolsProvider is registered
- [ ] Correct interface type
- [ ] Singleton scope

---

## PHASE 9: BUILD WITH ZERO ERRORS/WARNINGS

**Goal**: Ensure clean build.

### Gap 9.1: Clean Build

**Status**: [ ]

**Commands**:
```bash
dotnet clean
dotnet build --configuration Release
```

**Success Criteria**:
- [ ] Build succeeds
- [ ] 0 Errors
- [ ] 0 Warnings
- [ ] All projects build successfully

---

## PHASE 10: CREATE AUDIT REPORT

**Goal**: Document all work completed.

### Gap 10.1: Create Audit Report

**Status**: [ ]

**File**: docs/audits/task-007a-audit-report.md

**Contents**:
- All 17 schemas verified semantically complete
- All 80+ ACs documented as complete
- All ~130 tests passing
- Build clean with 0 errors
- Performance benchmarks met
- CLI verification steps completed

**Success Criteria**:
- [ ] Audit report created
- [ ] All verification checks documented
- [ ] No outstanding issues

---

## PHASE 11: COMMIT AND CREATE PR

**Goal**: Package work for review and merge.

### Gap 11.1: Commit All Work

**Status**: [ ]

**Commands**:
```bash
git add .
git commit -m "feat(task-007a): fix bugs and add comprehensive test coverage

- CRITICAL: Fix WriteFileSchema.create_directories default (false, not true)
- ENHANCEMENT: Improve all 17 schema descriptions (50-200 chars per spec)
- TESTS: Add 104 unit tests for schema validation
  * File operations: 36 tests
  * Code execution: 14 tests
  * Code analysis: 18 tests
  * Version control: 24 tests
  * User interaction: 12 tests
- TESTS: Add 4 integration tests for registration and examples
- TESTS: Add 2 performance tests (validation <1ms, compilation <500ms)
- VERIFY: All 80+ ACs verified complete
- VERIFY: Build clean with 0 errors, 0 warnings

All 17 core tools now 100% spec compliant with comprehensive test coverage.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Haiku 4.5 <noreply@anthropic.com>"

git push origin feature/task-006a-fix-gaps
```

**Success Criteria**:
- [ ] All work committed
- [ ] Commit message comprehensive
- [ ] Pushed to feature branch

---

### Gap 11.2: Create PR

**Status**: [ ]

**Command**:
```bash
gh pr create --title "Task 007a: Fix schema bugs and add comprehensive test coverage" \
  --body "$(cat <<'EOF'
## Summary

Fixed critical bug in WriteFileSchema and added 110+ tests to achieve 100% specification compliance for all 17 core tool schemas.

### Key Changes

- **CRITICAL FIX**: WriteFileSchema.create_directories default was true, now correctly false
- **ENHANCEMENT**: Improved all 17 schema descriptions to meet spec quality standards
- **TESTS**: Added 104 unit tests across 5 test suites
- **TESTS**: Added 4 integration tests for registration and validation
- **TESTS**: Added 2 performance tests meeting <1ms per validation benchmark
- **VERIFY**: All 80+ acceptance criteria verified complete

### Test Results

- 130+ tests passing (11 existing + 119 new)
- All 17 tools register correctly
- All examples validate successfully
- Performance benchmarks met (<1ms per validation, <500ms compilation)
- Build: 0 errors, 0 warnings

### Manual Verification

```bash
# Verify all tools registered
acode tools list
# Expected: 17 tools in 5 categories

# Verify schema constraints
acode tools validate read_file <<< '{\"path\": \"test.txt\"}'
# Expected: ✓ Valid

acode tools validate write_file <<< '{\"path\": \"out.txt\", \"content\": \"x\"}'
# Expected: ✓ Valid (create_directories defaults to false)
```

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

**Success Criteria**:
- [ ] PR created successfully
- [ ] PR description comprehensive
- [ ] PR links to gap analysis and checklist

---

## COMPLETION CRITERIA

**Task is COMPLETE when ALL of the following are true:**

- [ ] All 17 schemas verified semantically complete
- [ ] WriteFileSchema.create_directories bug fixed (default false)
- [ ] All 17 schema descriptions enhanced (50-200 chars)
- [ ] All schema constraints verified vs spec
- [ ] 104 unit tests created and passing
- [ ] 4 integration tests created and passing
- [ ] 2 performance tests created and passing
- [ ] All 80+ ACs verified complete
- [ ] Build succeeds with 0 errors, 0 warnings
- [ ] Audit report created
- [ ] PR created with comprehensive description
- [ ] CoreToolsProvider registered in DI
- [ ] All tests pass: `dotnet test --filter "FullyQualifiedName~ToolSchemas"`

**DO NOT mark task complete until ALL checkboxes are ✅**

---

## NOTES FOR IMPLEMENTATION AGENT

- Schema descriptions are too brief currently - expand all to 50-200 chars per spec
- WriteFileSchema has critical bug: create_directories defaults to true, must be false
- Testing is very light (11 tests) - spec requires 108+ minimum, aim for ~130 tests
- Use existing CoreToolsProviderTests as pattern for new test files
- SchemaBuilder approach is superior to raw JsonDocument.Parse() - keep it
- No CLI `acode tools show` or `acode tools validate` commands in this task (that's 010)
- This task only defines schemas and validates them work correctly
- All constraints must be verified test-first (RED-GREEN-REFACTOR)

---

**END OF COMPLETION CHECKLIST**
