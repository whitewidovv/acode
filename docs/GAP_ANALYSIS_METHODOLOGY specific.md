# Gap Analysis Methodology for Task Suite Completion

## Executive Summary

This document provides a systematic process to identify incomplete task implementations and bring them to 100% completion. The methodology verifies **actual implementation**, not just file presence, by checking method signatures, test coverage, and functional correctness.

**Critical Principle**: A file existing does NOT mean a feature is implemented. We must verify:
1. ✅ File exists
2. ✅ File contains real implementation (not stubs/NotImplementedException)
3. ✅ Implementation matches spec method signatures
4. ✅ Tests exist and actually test the implementation
5. ✅ Tests are passing (proving implementation works)

**Application**: Change `002` throughout this document to target any task suite (e.g., 001, 002, 050, 051).

---

## Problem Statement

**Observed Pattern**: Task implementations marked "complete" were actually 30-70% complete because:
1. Only Description and Acceptance Criteria sections were read
2. Implementation Prompt section (containing complete code) was skipped
3. Testing Requirements section (containing full test implementations) was skipped
4. Files were created but contained stubs (NotImplementedException, empty methods)
5. No verification of actual implementation vs specification requirements

**Impact**: Missing 30-70% of required functionality despite files existing.

---

## Gap Analysis Methodology (Step-by-Step)

### Phase 1: Locate Task Specification Files

**Objective**: Find all specification files for the target task suite.

**Commands**:
```bash
# Find all files for task suite {TASK_NUMBER}
find docs/tasks/refined-tasks -name "task-{TASK_NUMBER}*.md" -type f | sort

# Example for task 050:
find docs/tasks/refined-tasks -name "task-050*.md" -type f | sort
```

**Expected Output**:
```
docs/tasks/refined-tasks/Epic NN/task-002-parent-task-name.md
docs/tasks/refined-tasks/Epic NN/task-002a-subtask-name.md
docs/tasks/refined-tasks/Epic NN/task-002b-subtask-name.md
...
```

**Action**: List all subtasks found (parent + subtasks a, b, c, etc.)

---

### Phase 2: Check Line Counts and Locate Critical Sections

**Objective**: Understand specification size and locate sections containing complete code.

**Commands**:
```bash
# Get total line count for each spec file
wc -l docs/tasks/refined-tasks/Epic\ NN/task-002*.md

# Find Implementation Prompt section line number (contains complete production code)
grep -n "## Implementation Prompt" docs/tasks/refined-tasks/Epic\ NN/task-002a*.md

# Find Testing Requirements section line number (contains complete test code)
grep -n "## Testing Requirements" docs/tasks/refined-tasks/Epic\ NN/task-002a*.md

# Find Acceptance Criteria section (for feature checklist)
grep -n "## Acceptance Criteria" docs/tasks/refined-tasks/Epic\ NN/task-002a*.md
```

**Example Output** (task-050a):
```
4014 total lines in task-050a-workspace-db-layout-migration-strategy.md

Acceptance Criteria: line 600
Testing Requirements: line 2118
Implementation Prompt: line 3195
```

**Critical Insight**: Implementation Prompt and Testing Requirements sections typically occupy 40-50% of spec file and contain **complete, working code examples**.

---

### Phase 3: Read Complete Specification Sections

**Objective**: Extract **all** requirements including complete code examples.

**Read Order** (CRITICAL - must read in this order):

1. **Description** (lines ~1-200)
   - Business value, architecture overview, constraints

2. **Acceptance Criteria** (lines ~600-900)
   - Complete checklist of deliverables
   - Use this to verify completion later

3. **Testing Requirements** (lines ~2100-3200)
   - **COMPLETE test code with full implementations**
   - Count expected test methods
   - Note test patterns and assertions

4. **Implementation Prompt** (lines ~3200-4000)
   - **COMPLETE production code with full implementations**
   - Extract all class names, method signatures
   - Note expected file paths

**Tool Calls**:
```python
# Read Acceptance Criteria (get complete deliverables list)
Read(file_path="docs/tasks/refined-tasks/Epic NN/task-002a-*.md",
     offset=600, limit=300)

# Read Testing Requirements (get complete test code)
Read(file_path="docs/tasks/refined-tasks/Epic NN/task-002a-*.md",
     offset=2118, limit=1077)

# Read Implementation Prompt (get complete production code)
Read(file_path="docs/tasks/refined-tasks/Epic NN/task-002a-*.md",
     offset=3195, limit=819)
```

**What to Extract**:

From **Acceptance Criteria**:
```
- [ ] ValidationResult.cs created with Success(), Failure(), WithWarnings(), Combine()
- [ ] ColumnSchema.cs created with Name, DataType, computed properties
- [ ] NamingConventionValidator.cs created with 8 validation methods
- [ ] 45 unit tests for domain models (12 + 15 + 18)
- [ ] 44 unit tests for NamingConventionValidator
- [ ] conventions.md documentation (350+ lines)
```

From **Testing Requirements**:
```
Test method count: 102 total
- ValidationResultTests: 12 methods
- ColumnSchemaTests: 15 methods
- TableSchemaTests: 18 methods
- NamingConventionValidatorTests: 44 methods (using Theory/InlineData)
- DataTypeValidatorTests: 6 methods
- MigrationFileValidatorTests: 7 methods
```

From **Implementation Prompt**:
```
Production files expected:
- src/Acode.Domain/Database/ValidationResult.cs
  - Methods: Success(), Failure(), WithWarnings(), Combine()
  - Properties: IsValid, Errors, Warnings

- src/Acode.Infrastructure/Database/Layout/NamingConventionValidator.cs
  - Methods: ValidateTableName(), ValidateColumnName(), ValidateIndexName(),
             ValidatePrimaryKey(), ValidateForeignKeyColumn(),
             ValidateTimestampColumn(), ValidateBooleanColumn()
  - Uses GeneratedRegex for performance
```

---

### Phase 4: Assess Current Implementation State (DEEP VERIFICATION)

**Objective**: Determine what's actually implemented (not just what files exist).

#### Step 4.1: List Existing Files

```bash
# List existing production files
find src/Acode.Domain -name "*.cs" -path "*/{task_domain}/*"
find src/Acode.Infrastructure -name "*.cs" -path "*/{task_domain}/*"

# List existing test files
find tests/Acode.Domain.Tests -name "*Tests.cs" -path "*/{task_domain}/*"
find tests/Acode.Infrastructure.Tests -name "*Tests.cs" -path "*/{task_domain}/*"

# Example for task-050a (database domain):
find src/Acode.Domain/Database -name "*.cs"
find src/Acode.Infrastructure/Database -name "*.cs"
find tests -name "*Tests.cs" | grep -E "Database|Migration|Layout"
```

#### Step 4.2: Verify File Contents (NOT JUST PRESENCE)

**For Each Production File Found**:

```bash
# Check for NotImplementedException (stub indicator)
grep -n "NotImplementedException" src/path/to/File.cs

# Check for TODO comments (incomplete indicator)
grep -n "TODO\|FIXME\|HACK" src/path/to/File.cs

# Count methods in file
grep -c "public.*{" src/path/to/File.cs

# Check specific methods from spec exist
# Example: Spec requires Success(), Failure(), WithWarnings(), Combine()
grep "public.*Success\|public.*Failure\|public.*WithWarnings\|public.*Combine" src/Acode.Domain/Database/ValidationResult.cs
```

**Verification Checklist Per File**:
- [ ] File exists
- [ ] No NotImplementedException
- [ ] No TODO/FIXME indicating incomplete work
- [ ] All methods from spec are present (check signatures)
- [ ] Method bodies have actual logic (not just `return null;` or `throw new NotImplementedException();`)

**Example - ValidationResult.cs Verification**:
```bash
# Expected methods from spec:
# - Success() - static factory
# - Failure(params string[]) - static factory
# - WithWarnings(params string[]) - static factory
# - Combine(ValidationResult) - instance method

# Verify all exist:
grep -E "public static.*Success|public static.*Failure|public static.*WithWarnings|public.*Combine" src/Acode.Domain/Database/ValidationResult.cs

# Expected output (all 4 found):
# public static ValidationResult Success() => ...
# public static ValidationResult Failure(params string[] errors) => ...
# public static ValidationResult WithWarnings(params string[] warnings) => ...
# public ValidationResult Combine(ValidationResult other) ...

# If any missing, file is INCOMPLETE despite existing
```

#### Step 4.3: Verify Test Files (NOT JUST PRESENCE)

**For Each Test File Found**:

```bash
# Count test methods in file
grep -c "\[Fact\]\|\[Theory\]" tests/path/to/FileTests.cs

# Compare to spec expected count
# Example: NamingConventionValidatorTests should have 44 tests

# Check for NotImplementedException in tests (incomplete tests)
grep -n "NotImplementedException" tests/path/to/FileTests.cs

# Check tests actually assert something (not empty)
grep -n "Should\(\)\|Assert\|Verify" tests/path/to/FileTests.cs | wc -l
```

**Verification Checklist Per Test File**:
- [ ] File exists
- [ ] Test count matches spec (within ±2)
- [ ] No NotImplementedException
- [ ] Tests contain actual assertions (Should(), Assert, etc.)
- [ ] Tests are passing when run

**Example - NamingConventionValidatorTests Verification**:
```bash
# Spec says: 44 tests (8 methods with Theory/InlineData)

# Count actual tests:
grep -c "\[Fact\]\|\[Theory\]" tests/Acode.Infrastructure.Tests/Database/Layout/NamingConventionValidatorTests.cs

# Expected: 8 (methods decorated with [Fact] or [Theory])
# Note: [InlineData] adds test cases but doesn't add to method count

# Verify tests have assertions:
grep "Should()" tests/Acode.Infrastructure.Tests/Database/Layout/NamingConventionValidatorTests.cs | wc -l

# Expected: 40+ assertions (should be plenty for 44 test cases)

# If count is low (e.g., 5 assertions for 44 tests), tests are STUBS
```

#### Step 4.4: Run Tests to Verify Implementation

**CRITICAL**: Tests passing proves implementation exists and works.

```bash
# Run tests for specific component
dotnet test --filter "FullyQualifiedName~ValidationResultTests" --verbosity normal

# Check results:
# - All tests passing = implementation is real
# - Tests failing = implementation incomplete or wrong
# - Tests not found = tests don't exist despite file existing
```

**Verification Outcomes**:
- ✅ **Tests pass** = Implementation is real and correct
- ❌ **Tests fail** = Implementation exists but is incorrect/incomplete
- ❌ **No tests found** = Test file is empty/stub despite existing
- ❌ **Compilation errors** = Implementation doesn't match test expectations

---

### Phase 5: Create Gap Analysis Document with Verification Details

**Objective**: Document missing/incomplete components with evidence.

**File Location**: `docs/implementation-plans/task-002a-gap-analysis.md`

**Document Structure**:

```markdown
# Task 002a Gap Analysis

## Specification Requirements Summary

**From Acceptance Criteria** (lines 600-900):
- Total acceptance criteria items: XX
- Production files expected: X
- Test files expected: X
- Documentation files expected: X
- Total test methods expected: XXX

**From Implementation Prompt** (lines 3195-4014):
[List each file with expected methods]

**From Testing Requirements** (lines 2118-3194):
[List each test file with expected test count]

---

## Current Implementation State (VERIFIED)

### Production Files

#### ✅ COMPLETE: src/Acode.Domain/Database/ValidationResult.cs
**Status**: Fully implemented
- ✅ File exists (74 lines)
- ✅ No NotImplementedException
- ✅ All 4 methods from spec present: Success(), Failure(), WithWarnings(), Combine()
- ✅ Method bodies contain real logic
- ✅ Tests passing (12/12)

**Evidence**:
```bash
$ grep "public static.*Success\|Failure\|WithWarnings\|public.*Combine" ValidationResult.cs
public static ValidationResult Success() => new() { ... }
public static ValidationResult Failure(params string[] errors) => new() { ... }
public static ValidationResult WithWarnings(params string[] warnings) => new() { ... }
public ValidationResult Combine(ValidationResult other) { ... }

$ dotnet test --filter "FullyQualifiedName~ValidationResultTests"
Passed: 12, Failed: 0
```

#### ❌ INCOMPLETE: src/Acode.Infrastructure/Database/Layout/NamingConventionValidator.cs
**Status**: File exists but incomplete (stub)
- ✅ File exists (45 lines)
- ❌ Contains NotImplementedException in 6 of 8 methods
- ❌ Only 2 of 8 methods from spec implemented: ValidateTableName(), ValidateColumnName()
- ❌ Missing methods: ValidateIndexName(), ValidatePrimaryKey(), ValidateForeignKeyColumn(), ValidateTimestampColumn(), ValidateBooleanColumn()
- ❌ Tests failing (2/44 passing, 42 failing)

**Evidence**:
```bash
$ grep -c "NotImplementedException" src/Acode.Infrastructure/Database/Layout/NamingConventionValidator.cs
6

$ grep "public ValidationResult Validate" NamingConventionValidator.cs
public ValidationResult ValidateTableName(...) { ... }  # IMPLEMENTED
public ValidationResult ValidateColumnName(...) { ... }  # IMPLEMENTED
public ValidationResult ValidateIndexName(...) { throw new NotImplementedException(); }  # STUB
public ValidationResult ValidatePrimaryKey(...) { throw new NotImplementedException(); }  # STUB
public ValidationResult ValidateForeignKeyColumn(...) { throw new NotImplementedException(); }  # STUB
public ValidationResult ValidateTimestampColumn(...) { throw new NotImplementedException(); }  # STUB
public ValidationResult ValidateBooleanColumn(...) { throw new NotImplementedException(); }  # STUB

$ dotnet test --filter "FullyQualifiedName~NamingConventionValidatorTests"
Passed: 2, Failed: 42
```

**Required Work**:
- Implement 6 missing methods following spec (Implementation Prompt lines 3400-3550)
- Verify all 44 tests passing

#### ❌ MISSING: src/Acode.Infrastructure/Database/Layout/DataTypeValidator.cs
**Status**: File does not exist
- ❌ File not found
- ❌ Spec requires 6 validation methods (Implementation Prompt lines 3600-3750)
- ❌ Spec requires 6 tests (Testing Requirements lines 2800-2900)

**Required Work**:
- Create file from spec
- Implement all 6 methods: ValidateIdColumn(), ValidateTimestampColumn(), ValidateBooleanColumn(), ValidateJsonColumn(), ValidateForeignKeyColumn(), ValidateEnumColumn()
- Write 6 tests
- Verify all tests passing

### Test Files

#### ✅ COMPLETE: tests/Acode.Domain.Tests/Database/ValidationResultTests.cs
**Status**: Fully implemented
- ✅ File exists (98 lines)
- ✅ Contains 12 test methods (matches spec expectation)
- ✅ All tests have assertions (42 Should() calls)
- ✅ All tests passing (12/12)

**Evidence**:
```bash
$ grep -c "\[Fact\]" ValidationResultTests.cs
12

$ grep -c "Should()" ValidationResultTests.cs
42

$ dotnet test --filter "ValidationResultTests"
Passed: 12, Failed: 0
```

#### ❌ INCOMPLETE: tests/Acode.Infrastructure.Tests/Database/Layout/NamingConventionValidatorTests.cs
**Status**: File exists but has stub tests
- ✅ File exists (156 lines)
- ⚠️ Contains 8 test methods (spec expects 8 methods with 44 total test cases via Theory/InlineData)
- ❌ Only 2 test methods have real assertions
- ❌ 6 test methods have NotImplementedException
- ❌ Tests failing (2/44 passing, 42 failing)

**Evidence**:
```bash
$ grep -c "\[Theory\]" NamingConventionValidatorTests.cs
8

$ grep -c "NotImplementedException" NamingConventionValidatorTests.cs
6

$ dotnet test --filter "NamingConventionValidatorTests"
Passed: 2, Failed: 42
```

**Required Work**:
- Implement 6 missing test methods from Testing Requirements (lines 2400-2600)
- Verify all 44 test cases passing

#### ❌ MISSING: tests/Acode.Infrastructure.Tests/Database/Layout/DataTypeValidatorTests.cs
**Status**: File does not exist
- ❌ File not found
- ❌ Spec expects 6 test methods (Testing Requirements lines 2800-2900)

**Required Work**:
- Create file from spec
- Implement all 6 tests
- Verify all tests passing

---

## Gap Summary

### Files Requiring Work

| Category | Complete | Incomplete (Stubs) | Missing | Total |
|----------|----------|-------------------|---------|-------|
| Production | 1 | 1 | 4 | 6 |
| Tests | 1 | 1 | 4 | 6 |
| SQL | 0 | 0 | 10 | 10 |
| Docs | 0 | 0 | 1 | 1 |
| **TOTAL** | **2** | **2** | **18** | **23** |

**Completion Percentage**: 8.7% (2 complete / 23 total)

### Test Coverage Gap

| Component | Tests Expected | Tests Passing | Gap |
|-----------|---------------|---------------|-----|
| ValidationResult | 12 | 12 | ✅ 0 |
| ColumnSchema | 15 | 0 | ❌ 15 missing |
| TableSchema | 18 | 0 | ❌ 18 missing |
| NamingConventionValidator | 44 | 2 | ❌ 42 incomplete |
| DataTypeValidator | 6 | 0 | ❌ 6 missing |
| MigrationFileValidator | 7 | 0 | ❌ 7 missing |
| **TOTAL** | **102** | **14** | **❌ 88 tests missing/incomplete** |

**Test Completion Percentage**: 13.7% (14 passing / 102 expected)

---

## Strategic Implementation Plan

### Phase 1: Complete NamingConventionValidator (INCOMPLETE → COMPLETE)
**Current State**: 2/8 methods implemented, 2/44 tests passing
**Target State**: 8/8 methods implemented, 44/44 tests passing

**Files to Modify**:
1. src/Acode.Infrastructure/Database/Layout/NamingConventionValidator.cs
   - Implement 6 missing methods from spec (lines 3400-3550)
   - Remove NotImplementedException

2. tests/Acode.Infrastructure.Tests/Database/Layout/NamingConventionValidatorTests.cs
   - Implement 6 missing test methods from spec (lines 2400-2600)
   - Remove NotImplementedException

**TDD Process**:
1. RED: Implement missing test methods (tests will fail)
2. GREEN: Implement missing production methods (tests will pass)
3. REFACTOR: Clean up, fix StyleCop violations
4. VERIFY: Run `dotnet test --filter "NamingConventionValidatorTests"` → expect 44/44 passing

**Acceptance**:
- [ ] All 8 methods implemented (no NotImplementedException)
- [ ] All 44 tests passing
- [ ] Build GREEN (0 errors, 0 warnings)

### Phase 2: Implement ColumnSchema (MISSING → COMPLETE)
**Current State**: File missing
**Target State**: File created with 15 tests passing

**Files to Create**:
1. src/Acode.Domain/Database/ColumnSchema.cs (~85 lines from spec lines 3250-3335)
2. tests/Acode.Domain.Tests/Database/ColumnSchemaTests.cs (~120 lines from spec lines 2250-2370)

**TDD Process**:
1. RED: Create test file first (15 test methods)
2. GREEN: Create production file
3. REFACTOR: Fix violations
4. VERIFY: 15/15 tests passing

**Acceptance**:
- [ ] ColumnSchema.cs created with all properties and computed properties
- [ ] ColumnSchemaTests.cs created with 15 tests
- [ ] All 15 tests passing
- [ ] Build GREEN

### Phase 3-7: [Continue for remaining components]

[List all remaining phases with same detail level]

---

## Verification Checklist (Before Marking Task Complete)

### File Existence Check
- [ ] All production files from spec exist
- [ ] All test files from spec exist
- [ ] All SQL files from spec exist
- [ ] All documentation files from spec exist

### Implementation Verification Check
For each production file:
- [ ] No NotImplementedException
- [ ] No TODO/FIXME comments
- [ ] All methods from spec present (grep verification)
- [ ] Method signatures match spec
- [ ] Method bodies contain logic (not just `return null;`)

### Test Verification Check
For each test file:
- [ ] Test count matches spec (±2)
- [ ] No NotImplementedException
- [ ] Tests contain assertions (Should/Assert/Verify)
- [ ] All tests passing when run

### Build & Test Execution Check
- [ ] `dotnet build` → 0 errors, 0 warnings
- [ ] `dotnet test` → all tests passing
- [ ] Test count: XXX passing (matches spec expected count ±5%)

### Functional Verification Check
- [ ] Run sample code from spec examples (if provided)
- [ ] Verify validators reject invalid inputs
- [ ] Verify validators accept valid inputs
- [ ] Verify error messages match spec expectations

### Completeness Cross-Check
- [ ] Compare file count: Built XX / Spec requires XX
- [ ] Compare test count: Passing XXX / Spec expects XXX
- [ ] Compare line count: Delivered XXXX / Spec estimates XXXX
- [ ] Review Acceptance Criteria: XX/XX items complete

---

## Execution Checklist
- [ ] Phase 1 complete (verified with tests passing)
- [ ] Phase 2 complete (verified with tests passing)
- [ ] Phase 3 complete (verified with tests passing)
- [ ] Phase 4 complete (verified with tests passing)
- [ ] Phase 5 complete (verified with tests passing)
- [ ] Phase 6 complete (verified with tests passing)
- [ ] Phase 7 complete (verified with tests passing)
- [ ] All verification checks passed
- [ ] Documentation complete
- [ ] Integration tests passing (if applicable)

**Task Status**: [IN PROGRESS / READY FOR REVIEW / COMPLETE]
```

---

### Phase 6: Execute Implementation Systematically

**Objective**: Implement/complete missing components following strict TDD.

#### For Each Phase:

**Step 1: Update Todo List**
```json
[
  {"content": "Task 002a Phase N: [Component] (INCOMPLETE → COMPLETE)", "status": "in_progress", "activeForm": "Completing [Component]"},
  {"content": "Task 002a Phase N+1: [Next]", "status": "pending", "activeForm": "Next phase"}
]
```

**Step 2: RED State - Write/Complete Tests First**

If test file missing:
```bash
# Create test file from Testing Requirements section
Write(file_path="tests/.../ComponentTests.cs", content="[copy from spec lines XXXX-YYYY]")
```

If test file has stubs (NotImplementedException):
```bash
# Read current file
Read(file_path="tests/.../ComponentTests.cs")

# Replace NotImplementedException with real test code from spec
Edit(file_path="tests/.../ComponentTests.cs",
     old_string="throw new NotImplementedException();",
     new_string="[real test code from spec]")
```

Verify RED state:
```bash
dotnet test --filter "FullyQualifiedName~ComponentTests" --verbosity normal
# Expected: Tests fail because implementation missing/incomplete
```

**Step 3: GREEN State - Implement/Complete Production Code**

If production file missing:
```bash
# Create from Implementation Prompt section
Write(file_path="src/.../Component.cs", content="[copy from spec lines XXXX-YYYY]")
```

If production file has stubs:
```bash
# Read current file
Read(file_path="src/.../Component.cs")

# Check for NotImplementedException
Grep(pattern="NotImplementedException", path="src/.../Component.cs", output_mode="content")

# Replace each NotImplementedException with real implementation from spec
Edit(file_path="src/.../Component.cs",
     old_string="throw new NotImplementedException();",
     new_string="[real implementation from spec]")
```

Verify GREEN state:
```bash
dotnet test --filter "FullyQualifiedName~ComponentTests" --verbosity normal
# Expected: All tests passing
```

**Step 4: REFACTOR State - Fix Violations**
```bash
# Run full build
dotnet build

# Fix violations found:
# - CA1062: Add ArgumentNullException.ThrowIfNull()
# - SA1201: Reorder members (constructor → properties → methods)
# - SA1642: Fix documentation
# - CA2007: Add #pragma warning disable if needed

# Verify still GREEN
dotnet test --filter "FullyQualifiedName~ComponentTests"
# Expected: Still all passing
```

**Step 5: Verify Implementation (NOT JUST TESTS PASSING)**
```bash
# Verify no NotImplementedException remains
grep -r "NotImplementedException" src/.../Component.cs
# Expected: No matches

# Verify method count matches spec
grep -c "public.*{" src/.../Component.cs
# Compare to spec expected method count

# Verify specific methods from spec exist
grep "public.*MethodNameFromSpec" src/.../Component.cs
# Check each method from spec is present
```

**Step 6: Commit**
```bash
git add -A
git commit -m "feat(task-002a): complete [Component] implementation

Phase N of task-002a:
- [Component].cs - completed X missing methods, removed NotImplementedException
- [Component]Tests.cs - implemented Y missing tests
- All X tests now passing (was Y/X, now X/X)

Verified:
- No NotImplementedException remaining
- All methods from spec present
- Build GREEN (0 errors, 0 warnings)

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"

git push origin feature/task-002-[branch-name]
```

**Step 7: Update Gap Analysis Document**
```bash
# Mark phase complete with evidence
Edit(file_path="docs/implementation-plans/task-002a-gap-analysis.md",
     old_string="- [ ] Phase N complete",
     new_string="- [x] Phase N complete - Verified: X/X tests passing, 0 NotImplementedException")
```

**Step 8: Mark Phase Complete in Todo**
```json
{"content": "✅ Task 002a Phase N: [Component] COMPLETE", "status": "completed", "activeForm": "Verified complete with tests"}
```

**Step 9: Repeat for Next Phase**

---

### Phase 7: Final Verification (100% Completion)

**Objective**: Prove task is actually complete, not just files existing.

#### Verification Checklist:

**1. File Count Verification**
```bash
# Count production files created
find src -name "*.cs" -newer [start_commit] | wc -l

# Count test files created
find tests -name "*Tests.cs" -newer [start_commit] | wc -l

# Compare to gap analysis expected counts
# Must match exactly (±1 for helper files)
```

**2. NotImplementedException Scan (CRITICAL)**
```bash
# Scan ALL files for stubs
grep -r "NotImplementedException" src/Acode.Domain/[domain]/
grep -r "NotImplementedException" src/Acode.Infrastructure/[domain]/
grep -r "NotImplementedException" tests/Acode.Domain.Tests/[domain]/
grep -r "NotImplementedException" tests/Acode.Infrastructure.Tests/[domain]/

# Expected: NO MATCHES
# If ANY matches found, task is INCOMPLETE
```

**3. TODO/FIXME Scan**
```bash
# Scan for incomplete work markers
grep -r "TODO\|FIXME\|HACK" src/Acode.Domain/[domain]/
grep -r "TODO\|FIXME\|HACK" src/Acode.Infrastructure/[domain]/

# Expected: NO MATCHES (or only acceptable TODOs like "TODO: Performance optimization in future")
```

**4. Method Signature Verification**
```bash
# For each file, verify methods from spec exist
# Example: ValidationResult should have Success(), Failure(), WithWarnings(), Combine()

grep "public static.*Success\|Failure\|WithWarnings\|public.*Combine" src/Acode.Domain/Database/ValidationResult.cs

# All 4 methods should be found
# If any missing, implementation is INCOMPLETE
```

**5. Test Count Verification**
```bash
# Count test methods in each test file
grep -c "\[Fact\]\|\[Theory\]" tests/Acode.Domain.Tests/Database/ValidationResultTests.cs
# Compare to spec expected: 12 tests

grep -c "\[Fact\]\|\[Theory\]" tests/Acode.Infrastructure.Tests/Database/Layout/NamingConventionValidatorTests.cs
# Compare to spec expected: 8 methods (44 test cases via InlineData)

# Test count must be within ±5% of spec expectation
# If significantly lower, tests are INCOMPLETE/STUBBED
```

**6. Build Verification**
```bash
# Clean build to catch any issues
dotnet clean
dotnet build

# Expected: Build succeeded, 0 errors, 0 warnings
# Any warnings/errors = INCOMPLETE
```

**7. Test Execution Verification**
```bash
# Run ALL tests for this task
dotnet test --filter "FullyQualifiedName~Acode.Domain.Tests.Database" --verbosity normal
dotnet test --filter "FullyQualifiedName~Acode.Infrastructure.Tests.Database.Layout" --verbosity normal

# Expected: X/X tests passing (100% pass rate)
# Any failures = INCOMPLETE or INCORRECT implementation
```

**8. Test Assertion Verification**
```bash
# Verify tests actually test something (not empty)
grep -c "Should()\|Assert\|Verify" tests/Acode.Domain.Tests/Database/ValidationResultTests.cs

# Expected: High count (multiple assertions per test)
# Low count (e.g., 5 assertions for 12 tests) = tests are STUBS
```

**9. Acceptance Criteria Cross-Check**
```bash
# Open spec file, go to Acceptance Criteria section
Read(file_path="docs/tasks/refined-tasks/Epic NN/task-002a-*.md",
     offset=600, limit=300)

# Manually check each item:
# - [ ] Item 1 from spec → ✅ Verified in codebase
# - [ ] Item 2 from spec → ✅ Verified in codebase
# ...

# ALL items must be checked and verified
```

**10. Gap Analysis Completeness Check**
```bash
# Review gap analysis document
Read(file_path="docs/implementation-plans/task-002a-gap-analysis.md")

# Verify all phases marked complete:
# - [x] Phase 1 complete
# - [x] Phase 2 complete
# - [x] Phase N complete

# Verify completion percentages updated:
# - Completion Percentage: 100% (was 8.7%)
# - Test Completion Percentage: 100% (was 13.7%)
```

#### Completion Criteria (ALL Must Be True)

- [ ] File count matches spec (±1 for helpers)
- [ ] NO NotImplementedException found in ANY file
- [ ] NO TODO/FIXME indicating incomplete work
- [ ] All methods from spec present in production files
- [ ] Test count matches spec (±5%)
- [ ] All tests passing (100% pass rate)
- [ ] Tests contain real assertions (not stubs)
- [ ] Build: 0 errors, 0 warnings
- [ ] All Acceptance Criteria items verified
- [ ] Gap analysis document shows 100% completion
- [ ] All commits pushed to remote branch

**If ANY criterion fails, task is INCOMPLETE.**

---

## Reusable Template for Any Task Suite

### Quick Start: Gap Analysis for Task 002

**Step 1**: Find task files
```bash
find docs/tasks/refined-tasks -name "task-002*.md" -type f | sort
```

**Step 2**: Get section locations
```bash
SPEC_FILE="docs/tasks/refined-tasks/Epic NN/task-002a-*.md"
wc -l "$SPEC_FILE"
grep -n "## Acceptance Criteria" "$SPEC_FILE"
grep -n "## Testing Requirements" "$SPEC_FILE"
grep -n "## Implementation Prompt" "$SPEC_FILE"
```

**Step 3**: Read all critical sections
```bash
# Read Acceptance Criteria (lines ~600-900)
# Read Testing Requirements (lines ~2100-3200)
# Read Implementation Prompt (lines ~3200-4000)
# Extract: expected files, methods, test counts
```

**Step 4**: Verify current state (DEEP CHECK)
```bash
# List files
find src tests -name "*.cs" | grep -i "{task_keyword}"

# For each file found:
# - Check for NotImplementedException
# - Verify methods from spec exist
# - Count test methods vs spec
# - Run tests and verify passing
```

**Step 5**: Create gap analysis document
```bash
# Use template above
# Include:
# - Spec requirements (with evidence from spec)
# - Current state (with verification evidence)
# - Gap summary (percentages)
# - Implementation plan (phases)
# - Verification checklist
```

**Step 6**: Execute phases (TDD + Verification)
```bash
# For each phase:
# 1. RED: Write/complete tests
# 2. GREEN: Write/complete implementation
# 3. REFACTOR: Fix violations
# 4. VERIFY: No NotImplementedException, all tests passing
# 5. COMMIT: Push to branch
```

**Step 7**: Final verification (100% proof)
```bash
# Run all verification checks from Phase 7
# Ensure ALL criteria met
# Update gap analysis to 100%
```

---

## Key Insights from Task-050a Experience

### What Worked Well

1. **Reading Implementation Prompt section completely**
   - Contains complete, working code
   - Shows exact method signatures expected
   - Prevents guessing at requirements

2. **Reading Testing Requirements for test method count**
   - Knew exactly how many tests to write (102 total)
   - Test code showed expected patterns
   - Avoided under-testing (would have written only ~40 tests otherwise)

3. **Verifying implementation, not just file existence**
   - Checked for NotImplementedException
   - Verified method signatures match spec
   - Ran tests to prove implementation works

4. **Phase-based implementation with verification**
   - Each phase independently verifiable
   - Easy to track progress
   - Clear stopping points

5. **Strict TDD enforcement**
   - Tests first caught design issues early
   - Implementation focused on passing tests
   - Refactoring maintained test coverage

### Critical Failures to Avoid

1. **❌ CRITICAL: Don't assume file existence = feature implemented**
   - File may contain only stubs (NotImplementedException)
   - File may have methods but they're empty
   - File may be completely unrelated to spec

2. **❌ CRITICAL: Don't skip Implementation Prompt section**
   - This is where complete code lives
   - Previous sessions skipped this, completed only 30%
   - Reading this section saved hours of guesswork

3. **❌ CRITICAL: Don't skip Testing Requirements section**
   - This defines how many tests to write
   - Shows test patterns and assertions
   - Previous sessions wrote 40 tests, spec expected 102

4. **❌ CRITICAL: Don't trust test count without running tests**
   - File may have 44 test methods but all throw NotImplementedException
   - Must verify tests are passing
   - Passing tests = proof implementation exists

5. **❌ CRITICAL: Don't mark complete without scanning for NotImplementedException**
   - Single grep command reveals incomplete work
   - Found in both production and test files
   - Must be zero occurrences for task to be complete

6. **❌ Don't batch multiple phases into one commit**
   - Harder to review
   - Harder to debug if issues arise
   - Lose granularity of progress tracking

7. **❌ Don't skip verification checklist**
   - Easy to miss incomplete components
   - Verification catches gaps in implementation
   - Must verify EVERY criterion before marking complete

---

## Application to Other Task Suites

### Identify Candidate Tasks for Gap Analysis

**Find all task suites**:
```bash
# Find all subtask "a" files (indicates task suite exists)
find docs/tasks/refined-tasks -name "task-*a-*.md" | sed 's/a-.*//' | sort -u

# Example output:
# task-002  (Operating Modes & Safety Posture)
# task-002  (Repository Contract)
# task-003  (Threat Model)
# task-050  (Database Foundation)
# task-051  (Model Runtime)
# ...
```

**For Each Task Suite Found**:
1. Run gap analysis using methodology above
2. Replace `{TASK_NUMBER}` with actual number
3. Create gap analysis document
4. Execute missing/incomplete phases
5. Verify 100% completion with checklist

### Effort Estimation

Based on task-050a experience (70% gap):
- **Gap Analysis**: 30-60 minutes
  - Reading specs: 20 minutes
  - Verifying current state: 20 minutes
  - Creating plan: 20 minutes

- **Implementation per Phase**: 30-90 minutes
  - Tests first: 15-30 minutes
  - Implementation: 15-45 minutes
  - Verification: 5-15 minutes

- **Total for 70% gap (7 phases)**: 4-8 hours
- **Total for 30% gap (3 phases)**: 2-4 hours

**Adjust based on**:
- Complexity of components
- Amount of stubbed code to replace
- Test count from spec

---

## Handoff Instructions for Another Agent Session

Copy this template and replace `002`:

```markdown
# Task 002 Gap Analysis and Completion - Execution Instructions

## Your Mission
Verify and complete task-002 by implementing ALL components from the specification and PROVING they work with passing tests.

## Critical Requirements

**DO NOT assume a file existing means it's implemented.**

You must verify:
1. ✅ File exists
2. ✅ File has NO NotImplementedException
3. ✅ All methods from spec are present
4. ✅ Tests exist with real assertions
5. ✅ Tests are PASSING (proves implementation works)

## Step-by-Step Instructions

### Step 1: Find and Read Full Specification

```bash
# Find spec file
SPEC_FILE=$(find docs/tasks/refined-tasks -name "task-002a-*.md")

# Get section line numbers
grep -n "## Acceptance Criteria" "$SPEC_FILE"
grep -n "## Testing Requirements" "$SPEC_FILE"
grep -n "## Implementation Prompt" "$SPEC_FILE"
```

**YOU MUST READ**:
- Acceptance Criteria (complete deliverables list)
- Testing Requirements (complete test code + expected test count)
- Implementation Prompt (complete production code + expected methods)

**Extract from each section**:
- Expected file paths
- Expected method signatures
- Expected test count
- Expected line counts

### Step 2: Verify Current Implementation State

**For Each Production File in Spec**:

```bash
# Check if file exists
ls -lh src/path/to/File.cs

# If exists, verify it's not a stub:
grep -n "NotImplementedException" src/path/to/File.cs
# Expected: NO MATCHES (if matches found, file is STUB)

grep -n "TODO\|FIXME" src/path/to/File.cs
# Expected: NO MATCHES (or only benign TODOs)

# Verify methods from spec exist
grep "public.*MethodNameFromSpec" src/path/to/File.cs
# Check EACH method from Implementation Prompt section
```

**Record in Gap Analysis**:
- ✅ COMPLETE: File exists, no NotImplementedException, all methods present
- ⚠️ INCOMPLETE: File exists but has NotImplementedException or missing methods
- ❌ MISSING: File doesn't exist

**For Each Test File in Spec**:

```bash
# Check if file exists
ls -lh tests/path/to/FileTests.cs

# If exists, count test methods
grep -c "\[Fact\]\|\[Theory\]" tests/path/to/FileTests.cs
# Compare to spec expected count

# Check for stub tests
grep -n "NotImplementedException" tests/path/to/FileTests.cs
# Expected: NO MATCHES

# Verify tests have assertions
grep -c "Should()\|Assert\|Verify" tests/path/to/FileTests.cs
# Should be high count (multiple assertions per test)

# RUN TESTS TO VERIFY
dotnet test --filter "FullyQualifiedName~FileTests" --verbosity normal
# Check: How many passing vs total?
```

**Record in Gap Analysis**:
- ✅ COMPLETE: Tests exist, real assertions, all passing
- ⚠️ INCOMPLETE: Tests exist but have NotImplementedException or failing
- ❌ MISSING: Tests don't exist

### Step 3: Create Gap Analysis Document

Create: `docs/implementation-plans/task-002a-gap-analysis.md`

**Use template from this document** (Phase 5 section).

**Must include**:
- Specification requirements summary (with line numbers)
- Current state with VERIFICATION EVIDENCE for each file
- Gap summary with percentages
- Phase-based implementation plan
- Verification checklist

**Evidence format for each file**:
```markdown
#### ⚠️ INCOMPLETE: src/Acode.Domain/Foo.cs
**Status**: Stub (NotImplementedException)
- ✅ File exists (45 lines)
- ❌ Contains NotImplementedException in 6 methods
- ❌ Tests failing (2/44 passing)

**Evidence**:
```bash
$ grep -c "NotImplementedException" src/Acode.Domain/Foo.cs
6

$ dotnet test --filter "FooTests"
Passed: 2, Failed: 42
```

**Required Work**: Implement 6 methods from spec lines 3400-3550
```

### Step 4: Execute Implementation (Strict TDD)

**For Each Phase in Gap Analysis**:

1. **Update Todo List**:
```json
{"content": "Phase N: [Component] - INCOMPLETE → COMPLETE", "status": "in_progress"}
```

2. **RED State**:
   - If test file missing: Create from Testing Requirements section
   - If test file has stubs: Replace NotImplementedException with real tests from spec
   - Run tests: Verify they fail (proving implementation missing)

3. **GREEN State**:
   - If production file missing: Create from Implementation Prompt section
   - If production file has stubs: Replace NotImplementedException with real code from spec
   - Run tests: Verify they ALL pass (proving implementation works)

4. **VERIFY State** (CRITICAL):
```bash
# Verify no stubs remain
grep "NotImplementedException" src/path/to/File.cs
# Expected: NO MATCHES

# Verify all methods from spec present
grep "public.*MethodName" src/path/to/File.cs
# Check EACH method from spec

# Verify all tests passing
dotnet test --filter "FileTests"
# Expected: X/X passing (100%)
```

5. **COMMIT**:
```bash
git add -A
git commit -m "feat(task-002a): complete [Component]

Phase N:
- [Component].cs - implemented X methods, removed NotImplementedException
- [Component]Tests.cs - implemented Y tests, all passing

Verified: No NotImplementedException, X/X tests passing

🤖 Generated with [Claude Code](https://claude.com/claude-code)
Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"

git push
```

6. **Mark phase complete in gap analysis document**

7. **Repeat for next phase**

### Step 5: Final Verification (100% Proof)

**Before marking task complete, RUN ALL VERIFICATION CHECKS**:

```bash
# 1. Scan for NotImplementedException (MUST BE ZERO)
grep -r "NotImplementedException" src/Acode.*/[task_domain]/
grep -r "NotImplementedException" tests/Acode.*.Tests/[task_domain]/
# Expected: NO MATCHES ANYWHERE

# 2. Scan for incomplete work markers
grep -r "TODO\|FIXME" src/Acode.*/[task_domain]/
# Expected: NO MATCHES or only benign TODOs

# 3. Run ALL tests (MUST BE 100% PASSING)
dotnet test --filter "FullyQualifiedName~[task_domain]" --verbosity normal
# Expected: X/X passing (from spec expected count)

# 4. Build verification (MUST BE CLEAN)
dotnet build
# Expected: 0 errors, 0 warnings

# 5. Count verification
find src tests -name "*.cs" -newer [start_commit] | wc -l
# Compare to spec expected file count (must match ±1)
```

**ONLY mark complete if ALL checks pass.**

## Success Criteria

Task is complete when:
- [ ] All files from spec exist
- [ ] NO NotImplementedException found ANYWHERE
- [ ] All methods from spec present in production files
- [ ] All tests from spec exist with real assertions
- [ ] ALL tests passing (100% pass rate)
- [ ] Build: 0 errors, 0 warnings
- [ ] File count matches spec (±1)
- [ ] Test count matches spec (±5%)
- [ ] Gap analysis shows 100% completion
- [ ] All commits pushed

**If ANY criterion fails, task is INCOMPLETE.**

## Estimated Effort

Based on gap size:
- 30% gap (3 phases): 2-4 hours
- 50% gap (5 phases): 4-6 hours
- 70% gap (7 phases): 6-10 hours

Task-050a had 70% gap, took 6 hours.

## Final Note

**DO NOT trust file existence as proof of implementation.**

**ONLY trust passing tests as proof.**

A file with NotImplementedException is NOT implemented.
A test with NotImplementedException is NOT a test.

Verify. Verify. Verify.
```

---

## Summary

**Gap Analysis Methodology** ensures task completion by:

1. **Reading complete specifications** (Implementation Prompt + Testing Requirements)
2. **Verifying actual implementation** (not just file presence)
3. **Checking for stubs** (NotImplementedException scan)
4. **Proving with tests** (tests must pass to prove implementation works)
5. **Creating evidence-based gap analysis** (verification results documented)
6. **Executing systematically** (TDD with verification at each step)
7. **Final verification** (comprehensive checklist before marking complete)

**Reusability**: Replace `002` to target any task suite.

**Proven Success**: Applied to task-050a, discovered 87% incomplete despite files existing (NotImplementedException found in multiple files), implemented missing work, achieved 100% verified completion.

---

**End of Document**
