# Task 001a - Gap Analysis and Implementation Checklist

## 📋 TASK OVERVIEW

**Task**: Task-001a: Define Mode Matrix (LocalOnly / Burst / Airgapped)
**Spec**: docs/tasks/refined-tasks/Epic 00/task-001a-define-mode-matrix.md (871 lines)
**Date**: 2026-01-11
**Status**: ~90% COMPLETE - Missing tests only

## ✅ WHAT EXISTS (Already Implemented)

**Production Code** (ALL COMPLETE):
- ✅ src/Acode.Domain/Modes/OperatingMode.cs - 3 modes defined
- ✅ src/Acode.Domain/Modes/Capability.cs - 26 capabilities defined
- ✅ src/Acode.Domain/Modes/Permission.cs - 5 permission levels
- ✅ src/Acode.Domain/Modes/MatrixEntry.cs - Record with Mode, Capability, Permission, Rationale, Prerequisite
- ✅ src/Acode.Domain/Modes/ModeMatrix.cs - 78 entries (3 modes × 26 capabilities), all query methods
- ✅ src/Acode.Domain/Modes/MatrixExporter.cs - ToJson(), ToMarkdownTable(), ToCsv(), ToCapabilityComparison()
- ✅ src/Acode.CLI/Commands/ConfigMatrixCommand.cs - Full implementation with all options
- ✅ docs/mode-matrix.md - User documentation

**Existing Tests**:
- ✅ tests/Acode.Domain.Tests/Modes/OperatingModeTests.cs - 3 tests
- ✅ tests/Acode.Domain.Tests/Modes/CapabilityTests.cs - 6 tests
- ✅ tests/Acode.Domain.Tests/Modes/PermissionTests.cs - 7 tests
- ✅ tests/Acode.Domain.Tests/Modes/MatrixEntryTests.cs - 5 tests
- ✅ tests/Acode.Domain.Tests/Modes/ModeMatrixTests.cs - 12 tests (covers UT-001a-01 to UT-001a-08)

**Total Existing Tests**: 33 tests passing

## ❌ GAPS IDENTIFIED (What's Missing)

All production code exists. **Only tests are missing.**

### Gap #1: MatrixExporterTests.cs - MISSING ENTIRELY
### Gap #2: ConfigMatrixCommandTests.cs - MISSING ENTIRELY
### Gap #3: Integration Tests - MISSING ENTIRELY (10 tests)
### Gap #4: E2E Tests - MISSING ENTIRELY (8 tests)

---

## 🎯 IMPLEMENTATION INSTRUCTIONS FOR FRESH AGENT

### Your Mission
Create the missing test files listed below following strict TDD. All production code already exists and works - you're just adding test coverage to meet the spec requirements.

### How to Use This Checklist
1. Work through gaps sequentially (Gap #1 → #2 → #3 → #4)
2. For each gap:
   - Mark as `[🔄]` when starting
   - Follow RED-GREEN-REFACTOR (write test, verify it passes with existing code, refactor if needed)
   - Update checklist with ✅ and evidence
   - Commit after each test file is complete
3. When all items are ✅, run full test suite and create PR

### Status Legend
- `[ ]` = TODO
- `[🔄]` = IN PROGRESS
- `[✅]` = COMPLETE with evidence

### Critical Rules
- TESTS ONLY - do NOT modify production code unless tests reveal a bug
- All existing production code is correct - verify tests pass against it
- Follow spec Testing Requirements section (lines 580-638) exactly
- Use xUnit, FluentAssertions pattern from existing test files
- Commit after each gap is complete

---

## GAP #1: Create MatrixExporterTests.cs

**Status**: [✅] COMPLETE

**File to Create**: `tests/Acode.Domain.Tests/Modes/MatrixExporterTests.cs`

**Spec Requirements** (Testing Requirements UT-001a-09, UT-001a-10, UT-001a-11):
- UT-001a-09: Matrix serializes to JSON
- UT-001a-10: Matrix deserializes from JSON
- UT-001a-11: Matrix prints as table

**Required Tests** (minimum 6 tests):

1. **ToJson_ReturnsValidJson**
   - Call MatrixExporter.ToJson()
   - Verify result is valid JSON
   - Deserialize and verify it contains 78 entries

2. **ToJson_ContainsAllMatrixEntries**
   - Serialize to JSON
   - Deserialize to List<MatrixEntry>
   - Verify all 3 modes present
   - Verify all 26 capabilities present

3. **ToJson_RoundTripPreservesData**
   - Serialize matrix to JSON
   - Deserialize back to objects
   - Compare with original entries
   - Verify Mode, Capability, Permission, Rationale, Prerequisite all preserved

4. **ToMarkdownTable_ProducesFormattedTable**
   - Call MatrixExporter.ToMarkdownTable()
   - Verify result contains "| Mode | Capability |" header
   - Verify result contains "LocalOnly", "Burst", "Airgapped"
   - Verify result has 78+ data rows (one per entry)

5. **ToMarkdownTable_WithModeFilter_OnlyIncludesThatMode**
   - Call MatrixExporter.ToMarkdownTable(OperatingMode.LocalOnly)
   - Verify result only contains "LocalOnly" rows
   - Verify result has 26 rows (one per capability for LocalOnly)

6. **ToCsv_ProducesValidCsv**
   - Call MatrixExporter.ToCsv()
   - Verify result contains CSV header
   - Verify result has 78 data rows
   - Verify commas and quotes properly escaped

7. **ToCapabilityComparison_ShowsCapabilityAcrossModes**
   - Call MatrixExporter.ToCapabilityComparison(Capability.OpenAiApi)
   - Verify result shows OpenAiApi for all 3 modes
   - Verify LocalOnly=Denied, Burst=ConditionalOnConsent, Airgapped=Denied

**Implementation Pattern**:
```csharp
using Acode.Domain.Modes;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Acode.Domain.Tests.Modes;

public sealed class MatrixExporterTests
{
    [Fact]
    public void ToJson_ReturnsValidJson()
    {
        // Act
        var json = MatrixExporter.ToJson();

        // Assert
        json.Should().NotBeNullOrWhiteSpace();

        // Should be valid JSON
        var entries = JsonSerializer.Deserialize<List<MatrixEntry>>(json);
        entries.Should().HaveCount(78); // 3 modes × 26 capabilities
    }

    // ... rest of tests
}
```

**How to Verify**:
```bash
# Run tests
dotnet test --filter "FullyQualifiedName~MatrixExporterTests" --verbosity normal

# Should see 6-7 tests passing
```

**Success Criteria**:
- [✅] File created at correct path
- [✅] Minimum 6 tests implemented (32 tests created, exceeding minimum)
- [✅] All tests pass against existing MatrixExporter code
- [✅] UT-001a-09, UT-001a-10, UT-001a-11 satisfied

**Commit Message Template**:
```
test(task-001a): add MatrixExporter test coverage

Created MatrixExporterTests.cs with 7 tests covering:
- JSON serialization/deserialization (UT-001a-09, UT-001a-10)
- Markdown table formatting (UT-001a-11)
- CSV export
- Capability comparison

All tests pass. Closes gap #1 for task-001a.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
```

**Evidence**:
```
Test Run Successful.
Total tests: 32
     Passed: 32
 Total time: 1.3895 Seconds

Tests created:
- JSON Export: 7 tests (ToJson_ReturnsValidJson, ToJson_ContainsAllMatrixEntries, ToJson_ContainsAllThreeModes, ToJson_ContainsAll26Capabilities, ToJson_RoundTripPreservesData, ToJson_UsesIndentedFormatting, ToJson_UsesCamelCasePropertyNames)
- Markdown Table: 10 tests (ProducesFormattedTable, ContainsAllModes, ContainsAllCapabilities, Has78DataRows, WithModeFilter_OnlyIncludesThatMode, WithBurstMode, WithAirgappedMode, ShowsRationaleForAllEntries, ShowsPrerequisitesForConditionalEntries, WithNullMode_ShowsAllModes)
- CSV Export: 5 tests (ProducesValidCsv, Has78DataRows, EscapesQuotesInFields, ContainsAllModes, ContainsAllPermissionTypes)
- Capability Comparison: 7 tests (ShowsCapabilityAcrossAllModes, FormatsAsMarkdownTable, HasHeading, OrdersModesByEnum, ShowsRationaleForEachMode, OpenAiApi_ShowsExpectedPermissions, WithEveryCapability_Succeeds)
- Performance: 3 tests (ToJson_PerformanceIsAcceptable, ToMarkdownTable_PerformanceIsAcceptable, ToCsv_PerformanceIsAcceptable)

All 32 tests pass. File: tests/Acode.Domain.Tests/Modes/MatrixExporterTests.cs
```

---

## GAP #2: Create ConfigMatrixCommandTests.cs

**Status**: [ ]

**File to Create**: `tests/Acode.Cli.Tests/Commands/ConfigMatrixCommandTests.cs`

**Spec Requirements** (Testing Requirements IT-001a-04):
- IT-001a-04: Matrix query via CLI - Correct output

**Required Tests** (minimum 8 tests):

1. **ExecuteAsync_NoOptions_DisplaysFullMatrix**
   - Create command with empty args
   - Execute command
   - Verify exit code = Success
   - Verify output contains all modes and capabilities

2. **ExecuteAsync_WithModeFilter_OnlyDisplaysThatMode**
   - Create command with args: ["--mode", "LocalOnly"]
   - Execute command
   - Verify output contains "LocalOnly" entries
   - Verify output does NOT contain "Burst" or "Airgapped"

3. **ExecuteAsync_WithCapabilityFilter_ShowsCapabilityAcrossModes**
   - Create command with args: ["--capability", "OpenAiApi"]
   - Execute command
   - Verify output shows OpenAiApi for all 3 modes

4. **ExecuteAsync_WithJsonFormat_OutputsJson**
   - Create command with args: ["--format", "json"]
   - Execute command
   - Verify output is valid JSON
   - Verify JSON contains 78 entries

5. **ExecuteAsync_WithTableFormat_OutputsTable**
   - Create command with args: ["--format", "table"]
   - Execute command
   - Verify output contains markdown table format
   - Verify output has "| Mode | Capability |" header

6. **ExecuteAsync_WithInvalidMode_UsesNoFilter**
   - Create command with args: ["--mode", "InvalidMode"]
   - Execute command
   - Verify still succeeds (invalid mode ignored)
   - Verify displays full matrix

7. **GetHelp_ReturnsUsageInstructions**
   - Call GetHelp()
   - Verify result contains "Usage:"
   - Verify result contains "--mode", "--capability", "--format" options
   - Verify result contains examples

8. **Name_ReturnsMatrix**
   - Verify command.Name == "matrix"

**Implementation Pattern**:
```csharp
using Acode.Cli.Commands;
using Acode.Domain.Modes;
using Acode.Cli.Formatting;
using FluentAssertions;
using Xunit;

namespace Acode.Cli.Tests.Commands;

public sealed class ConfigMatrixCommandTests
{
    [Fact]
    public async Task ExecuteAsync_NoOptions_DisplaysFullMatrix()
    {
        // Arrange
        var command = new ConfigMatrixCommand();
        var output = new StringWriter();
        var formatter = new ConsoleFormatter(output);
        var context = new CommandContext
        {
            Args = Array.Empty<string>(),
            Formatter = formatter
        };

        // Act
        var exitCode = await command.ExecuteAsync(context);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
        var result = output.ToString();
        result.Should().Contain("LocalOnly");
        result.Should().Contain("Burst");
        result.Should().Contain("Airgapped");
    }

    // ... rest of tests
}
```

**How to Verify**:
```bash
# Run tests
dotnet test --filter "FullyQualifiedName~ConfigMatrixCommandTests" --verbosity normal

# Should see 8+ tests passing
```

**Success Criteria**:
- [ ] File created at correct path
- [ ] Minimum 8 tests implemented
- [ ] All tests pass against existing ConfigMatrixCommand code
- [ ] IT-001a-04 satisfied

**Commit Message Template**:
```
test(task-001a): add ConfigMatrixCommand test coverage

Created ConfigMatrixCommandTests.cs with 8 tests covering:
- Default execution (full matrix display)
- Mode filtering (--mode option)
- Capability filtering (--capability option)
- Format options (--format json/table)
- Help text
- Error handling

All tests pass. Closes gap #2 for task-001a.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
```

**Evidence**: [To be filled when complete - paste test run output]

---

## GAP #3: Create Integration Tests

**Status**: [ ]

**File to Create**: `tests/Acode.Integration.Tests/Modes/ModeMatrixIntegrationTests.cs`

**Spec Requirements** (Testing Requirements IT-001a-01 to IT-001a-10):
- IT-001a-02: Matrix matches documentation
- IT-001a-03: Matrix loaded from assembly (fast load)
- IT-001a-05: Matrix export to JSON (valid file)
- IT-001a-06: All modes represented
- IT-001a-07: All capabilities listed
- IT-001a-08: Conditional prerequisites shown
- IT-001a-09: Legend displayed

**Note**: IT-001a-01 (Matrix matches ModeValidator) and IT-001a-10 (Matrix used in validation) are for future tasks (Task 001b ModeValidator doesn't exist yet).

**Required Tests** (minimum 7 tests):

1. **Matrix_MatchesDocumentation**
   - Read docs/mode-matrix.md
   - Parse tables from documentation
   - Compare with ModeMatrix.GetAllEntries()
   - Verify permissions match for all mode-capability combinations

2. **Matrix_LoadsFromAssemblyQuickly**
   - Measure time to access ModeMatrix.GetAllEntries() first time
   - Verify load time < 10ms (NFR-001a-24)

3. **Matrix_ExportToJsonCreatesValidFile**
   - Export matrix to temporary file
   - Verify file exists and is valid JSON
   - Verify file size > 0
   - Deserialize and verify 78 entries

4. **Matrix_ContainsAllThreeModes**
   - Get all entries
   - Group by Mode
   - Verify exactly 3 modes: LocalOnly, Burst, Airgapped

5. **Matrix_ContainsAll26Capabilities**
   - Get all entries
   - Group by Capability
   - Verify all 26 capabilities from spec present

6. **Matrix_ConditionalEntriesHavePrerequisites**
   - Get all entries with Permission = ConditionalOnConsent or ConditionalOnConfig
   - Verify each has non-null, non-empty Prerequisite

7. **Matrix_AllEntriesHaveRationale**
   - Get all 78 entries
   - Verify each has non-null, non-empty Rationale

**Implementation Pattern**:
```csharp
using Acode.Domain.Modes;
using FluentAssertions;
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Acode.Integration.Tests.Modes;

public sealed class ModeMatrixIntegrationTests
{
    [Fact]
    public void Matrix_ContainsAllThreeModes()
    {
        // Act
        var entries = ModeMatrix.GetAllEntries();
        var modes = entries.Select(e => e.Mode).Distinct();

        // Assert
        modes.Should().HaveCount(3);
        modes.Should().Contain(OperatingMode.LocalOnly);
        modes.Should().Contain(OperatingMode.Burst);
        modes.Should().Contain(OperatingMode.Airgapped);
    }

    [Fact]
    public void Matrix_LoadsFromAssemblyQuickly()
    {
        // Arrange
        var sw = Stopwatch.StartNew();

        // Act - first access triggers static constructor
        var entries = ModeMatrix.GetAllEntries();

        // Assert
        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(10);
        entries.Should().HaveCount(78);
    }

    // ... rest of tests
}
```

**How to Verify**:
```bash
# Run tests
dotnet test --filter "FullyQualifiedName~ModeMatrixIntegrationTests" --verbosity normal

# Should see 7+ tests passing
```

**Success Criteria**:
- [ ] File created at correct path
- [ ] Minimum 7 integration tests implemented
- [ ] All tests pass
- [ ] IT-001a-02, 03, 05, 06, 07, 08, 09 satisfied

**Commit Message Template**:
```
test(task-001a): add mode matrix integration tests

Created ModeMatrixIntegrationTests.cs with 7 tests covering:
- Documentation consistency (IT-001a-02)
- Fast load performance (IT-001a-03)
- JSON export validation (IT-001a-05)
- Complete mode coverage (IT-001a-06)
- Complete capability coverage (IT-001a-07)
- Conditional prerequisite validation (IT-001a-08)
- Rationale completeness (IT-001a-09)

All tests pass. Closes gap #3 for task-001a.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
```

**Evidence**: [To be filled when complete - paste test run output]

---

## GAP #4: Create E2E Tests

**Status**: [ ]

**File to Create**: `tests/Acode.Integration.Tests/Modes/ModeMatrixE2ETests.cs`

**Spec Requirements** (Testing Requirements E2E-001a-01 to E2E-001a-08):
- E2E-001a-04: User queries matrix via CLI
- E2E-001a-05: Denied action matches matrix
- E2E-001a-06: Allowed action matches matrix
- E2E-001a-07: Conditional action matches matrix

**Note**: E2E-001a-01, 02, 03 (Run in mode, check matrix) and E2E-001a-08 (Transition follows matrix) are for future tasks (mode enforcement is Task 001 parent, not 001a).

**Required Tests** (minimum 4 tests):

1. **CLI_QueryMatrix_DisplaysFullMatrix**
   - Invoke CLI command: acode matrix
   - Verify output contains all modes
   - Verify output contains all capabilities
   - Verify exit code = 0

2. **Matrix_DeniedActionExample_MatchesSpec**
   - Check matrix: GetPermission(LocalOnly, OpenAiApi)
   - Verify returns Permission.Denied
   - Verify rationale explains why (matches spec line ~829)

3. **Matrix_AllowedActionExample_MatchesSpec**
   - Check matrix: GetPermission(LocalOnly, LocalhostNetwork)
   - Verify returns Permission.Allowed
   - Verify rationale explains why (matches spec line ~821)

4. **Matrix_ConditionalActionExample_MatchesSpec**
   - Check matrix: GetPermission(Burst, OpenAiApi)
   - Verify returns Permission.ConditionalOnConsent
   - Verify prerequisite is "User consent required" or similar

**Implementation Pattern**:
```csharp
using Acode.Domain.Modes;
using FluentAssertions;
using Xunit;

namespace Acode.Integration.Tests.Modes;

public sealed class ModeMatrixE2ETests
{
    [Fact]
    public void Matrix_DeniedActionExample_MatchesSpec()
    {
        // Act - Check LocalOnly mode prohibits external LLM APIs (spec requirement)
        var permission = ModeMatrix.GetPermission(
            OperatingMode.LocalOnly,
            Capability.OpenAiApi);

        // Assert
        permission.Should().Be(Permission.Denied);

        var entry = ModeMatrix.GetEntry(OperatingMode.LocalOnly, Capability.OpenAiApi);
        entry.Should().NotBeNull();
        entry!.Rationale.Should().NotBeNullOrWhiteSpace();
        entry.Rationale.Should().Contain("privacy"); // Spec says "Core privacy constraint"
    }

    [Fact]
    public void Matrix_AllowedActionExample_MatchesSpec()
    {
        // Act - Check LocalOnly mode allows localhost for Ollama (spec requirement)
        var permission = ModeMatrix.GetPermission(
            OperatingMode.LocalOnly,
            Capability.LocalhostNetwork);

        // Assert
        permission.Should().Be(Permission.Allowed);

        var entry = ModeMatrix.GetEntry(OperatingMode.LocalOnly, Capability.LocalhostNetwork);
        entry.Should().NotBeNull();
        entry!.Rationale.Should().Contain("Ollama"); // Spec says "Required for Ollama communication"
    }

    // ... rest of tests
}
```

**How to Verify**:
```bash
# Run tests
dotnet test --filter "FullyQualifiedName~ModeMatrixE2ETests" --verbosity normal

# Should see 4+ tests passing
```

**Success Criteria**:
- [ ] File created at correct path
- [ ] Minimum 4 E2E tests implemented
- [ ] All tests pass
- [ ] E2E-001a-04, 05, 06, 07 satisfied

**Commit Message Template**:
```
test(task-001a): add mode matrix E2E tests

Created ModeMatrixE2ETests.cs with 4 tests covering:
- CLI matrix query (E2E-001a-04)
- Denied permission example (E2E-001a-05)
- Allowed permission example (E2E-001a-06)
- Conditional permission example (E2E-001a-07)

All tests pass. Closes gap #4 for task-001a.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
```

**Evidence**: [To be filled when complete - paste test run output]

---

## FINAL VERIFICATION

After completing all 4 gaps, run these commands to verify task-001a is 100% complete:

### 1. Run All Tests
```bash
# Run all mode-related tests
dotnet test --filter "FullyQualifiedName~Modes" --verbosity normal

# Expected: 50+ tests passing (33 existing + ~20 new)
```

### 2. Verify No Missing Tests
Check against Testing Requirements spec (lines 580-638):
- [ ] UT-001a-01 to UT-001a-11: Covered (12 existing + 7 new MatrixExporter = 19 tests)
- [ ] IT-001a-02, 03, 05-09: Covered (7 integration tests)
- [ ] E2E-001a-04-07: Covered (4 E2E tests)
- [ ] Total: 30+ tests for task-001a

### 3. Clean Build
```bash
dotnet clean
dotnet build

# Expected: Build succeeded, 0 errors, 0 warnings
```

### 4. Create PR
```bash
gh pr create --title "Task 001a: Complete mode matrix test coverage" --body "$(cat <<'EOF'
## Summary

Completes test coverage for task-001a (Mode Capability Matrix).

All production code already existed and was correct. This PR adds missing test coverage:

### New Test Files
- ✅ MatrixExporterTests.cs (7 tests)
- ✅ ConfigMatrixCommandTests.cs (8 tests)
- ✅ ModeMatrixIntegrationTests.cs (7 tests)
- ✅ ModeMatrixE2ETests.cs (4 tests)

### Test Coverage
- Unit tests: 19 tests (was 12, added 7)
- Integration tests: 7 tests (was 0, added 7)
- E2E tests: 4 tests (was 0, added 4)
- **Total: 30 new tests, 63 total mode tests**

### Verification
- All tests passing
- Build clean (0 errors, 0 warnings)
- All Testing Requirements from spec satisfied

Closes task-001a completely.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

---

## COMPLETION CHECKLIST

Task 001a is 100% complete when:

- [✅] All production code exists (already done)
- [ ] Gap #1 complete: MatrixExporterTests.cs created with 6+ tests passing
- [ ] Gap #2 complete: ConfigMatrixCommandTests.cs created with 8+ tests passing
- [ ] Gap #3 complete: ModeMatrixIntegrationTests.cs created with 7+ tests passing
- [ ] Gap #4 complete: ModeMatrixE2ETests.cs created with 4+ tests passing
- [ ] All tests passing (50+ total)
- [ ] Build clean (0 errors, 0 warnings)
- [ ] PR created and merged

---

## NOTES

**Why only tests are missing**:
Previous implementation agent created all production code but skipped writing comprehensive tests. The code works correctly, but doesn't meet the Testing Requirements section of the spec (lines 580-638).

**Order matters**:
Implement gaps sequentially (#1 → #2 → #3 → #4) because:
- Gap #1 and #2 are unit/component tests (faster to run)
- Gap #3 and #4 are integration/E2E tests (may be slower)
- Easier to debug if tests written in logical order

**Mode transitions**:
UT-001a-13, 14, 15 about mode transitions are NOT in scope for task-001a. Task-001a only defines the matrix. Mode transition enforcement is parent Task 001.

---

**END OF CHECKLIST**
