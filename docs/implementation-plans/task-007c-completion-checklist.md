# Task-007c Completion Checklist: Truncation + Artifact Attachment Rules

**Task**: task-007c-truncation-artifact-attachment-rules.md
**Status**: Gap Analysis Complete - Ready for Implementation
**Last Updated**: 2026-01-13
**Specification**: docs/tasks/refined-tasks/Epic 01/task-007c-truncation-artifact-attachment-rules.md (~1768 lines)

---

## How to Use This Checklist

This checklist guides implementation of task-007c with focus on completing test coverage and reconciling interface differences. A fresh Claude agent can:

1. Read this document completely
2. Pick any incomplete item [🔄]
3. Follow implementation instructions
4. Mark complete [✅] when done
5. Proceed to next item

### Critical Notes

- **Interface Decision Required**: ITruncationProcessor uses async ProcessAsync() in implementation but spec shows sync Process(). Current async is likely correct (needed for artifact storage). Verify this assumption before proceeding.
- **GetArtifactTool Missing**: Brand new tool needed for artifact retrieval
- **Namespace**: Spec shows "AgenticCoder" but codebase uses "Acode" (correct choice, spec is outdated)
- **Test Coverage Gap**: 20+ tests required, only ~5 partial tests exist

### TDD Workflow

For each test item:
1. RED: Create failing test (shows missing implementation)
2. GREEN: Implement minimum code to pass
3. REFACTOR: Clean up while tests pass
4. COMMIT: One commit per logical test group

---

## Phase 1: Interface Verification & Documentation

### 1. Verify ITruncationProcessor Interface [🔄]

**Requirement**: Confirm ITruncationProcessor signature matches expected usage pattern

**Location**: `src/Acode.Application/Truncation/ITruncationProcessor.cs`

**Decision Point**:
- Spec shows: `TruncationResult Process(string content, string toolName, TruncationConfiguration? config = null)`
- Implementation has: `Task<TruncationResult> ProcessAsync(string content, string toolName, string contentType = "text/plain")`

**Action**:
- [ ] Confirm async is correct (artifact storage requires I/O)
- [ ] Document why ProcessAsync differs from spec
- [ ] Add XML documentation explaining all methods
- [ ] Verify parameter usage with TruncationProcessor implementation

**Acceptance Criteria**:
- [x] All public methods documented with `<summary>`, `<param>`, `<returns>`
- [x] Async nature explained (artifact storage I/O operations)
- [x] Method signatures match actual implementation
- [x] All methods functional (no NotImplementedException)

**Evidence Needed**:
```bash
grep -A 50 "public interface ITruncationProcessor" src/Acode.Application/Truncation/ITruncationProcessor.cs | grep -c "summary\|param\|returns"
# Should show multiple documentation entries
```

**Dependencies**: None (verification only)

---

### 2. Verify ITruncationStrategy Interface [🔄]

**Requirement**: Verify strategy interface has all required methods

**Location**: `src/Acode.Application/Truncation/ITruncationStrategy.cs`

**Spec Reference**: Testing Requirements, strategies section

**Acceptance Criteria**:
- [x] Interface defines Truncate(string content) method (or ProcessAsync equivalent)
- [x] Returns TruncationResult
- [x] All implementations inherit correctly
- [x] Complete XML documentation

**Evidence Needed**: `grep "TruncationResult" src/Acode.Application/Truncation/ITruncationStrategy.cs`

**Dependencies**: None

---

### 3. Verify IArtifactStore Interface [🔄]

**Requirement**: Confirm artifact storage interface has all required methods

**Location**: `src/Acode.Application/Truncation/IArtifactStore.cs`

**Methods Required**:
- CreateAsync(content, toolName, contentType)
- GetContentAsync(artifactId)
- CleanupAsync()
- Optional: GetArtifactAsync(id) for partial retrieval

**Acceptance Criteria**:
- [x] All required methods present
- [x] Async signatures (returns Task<T>)
- [x] Complete documentation
- [x] Concurrent-access safe (noted in docs)

**Evidence Needed**: Grep for "async Task" signatures in interface

**Dependencies**: None

---

## Phase 2: Create Missing Classes & Tools

### 4. Create GetArtifactTool (Brand New) [🔄]

**Requirement**: Implement tool for retrieving artifact content

**Location**: `src/Acode.Infrastructure/Truncation/Tools/GetArtifactTool.cs`

**Purpose**: User-facing tool for requesting artifact content instead of inline truncated content

**Spec References**:
- Description lines 39-40: "The model can then decide whether it needs the full content, specific portions..."
- User Verification steps line 1673-1678: "Use get_artifact(id='...') to retrieve"

**Tool Parameters**:
- `id` (required): Artifact ID string
- `start_line` (optional): First line to retrieve
- `end_line` (optional): Last line to retrieve

**Tool Output**:
- Full artifact content if no range specified
- Specified line range if start/end provided
- Error if artifact not found or doesn't belong to session

**Implementation Pattern**:
```csharp
// Should register with ToolSchemaRegistry
// Should validate artifact ID belongs to current session
// Should support partial retrieval
// Should handle concurrent requests safely
```

**Acceptance Criteria**:
- [x] Inherits from ToolImplementation base class
- [x] Takes IArtifactStore via DI
- [x] Validates artifact ID (prevents access to other sessions' artifacts)
- [x] Supports line range parameters
- [x] Returns clear error messages (artifact not found, out of range, etc.)
- [x] Complete XML documentation

**Evidence Needed**:
```bash
grep -c "class GetArtifactTool" src/Acode.Infrastructure/Truncation/Tools/GetArtifactTool.cs
# Should show 1
```

**Dependencies**: IArtifactStore (item 3)

**Tests**: Covered in integration tests (item 17)

---

### 5. Create ArtifactReference.cs (If Needed) [🔄]

**Requirement**: Verify if ArtifactReference class exists or needs creation

**Location**: `src/Acode.Infrastructure/Truncation/ArtifactReference.cs`

**Purpose**: Represents reference/metadata about an artifact

**Check First**:
```bash
grep -r "class ArtifactReference" src/
```

**If Missing, Create**:
- [ ] Public string Id { get; }
- [ ] Public long Size { get; }
- [ ] Public DateTime CreatedAt { get; }
- [ ] Public string ContentType { get; }
- [ ] Public string SourceTool { get; }
- [ ] Public string? Summary { get; }

**Acceptance Criteria**:
- [x] If exists: proper structure with all properties
- [x] If missing: created with above properties
- [x] All properties have documentation

**Dependencies**: None

---

## Phase 3: Complete Unit Tests (20+ Tests Required)

### 6. HeadTailStrategyTests - Complete Coverage [🔄]

**Requirement**: Ensure HeadTailStrategyTests has all required test methods

**Location**: `tests/Acode.Application.Tests/Truncation/Strategies/HeadTailStrategyTests.cs`

**Spec Reference**: Testing Requirements lines 1267-1356

**Tests Required** (4 tests):
1. [ ] Should_Not_Truncate_Content_Under_Limit
2. [ ] Should_Truncate_Content_Preserving_Head_And_Tail
3. [ ] Should_Include_Omission_Marker_With_Counts
4. [ ] Should_Respect_UTF8_Character_Boundaries

**Test Details** (from spec):

**Test 1 - No truncation for short content**:
- Input: Content under InlineLimit (e.g., 100 chars total, limit 1000)
- Expected: Content unchanged, WasTruncated = false

**Test 2 - Head/tail preservation**:
- Input: HeadTailStrategy with 100 char limit, HeadRatio 0.6
- Input: 200 chars total (50 A's + 100 B's + 50 C's)
- Expected: Starts with "AAAA", ends with "CCCC", contains "omitted" marker, OmittedCharacters > 0

**Test 3 - Omission marker format**:
- Input: 50 lines, InlineLimit 100
- Expected: Regex match: `\.\.\. \[\d+ lines / \d+ chars omitted\] \.\.\.\`
- Expected: Metadata["omitted_lines"] and metadata["omitted_characters"] keys exist

**Test 4 - UTF-8 boundaries**:
- Input: "Hello 世界 " + 100 X's + " 再见 World" with limit 50
- Expected: No replacement char (\\ufffd), valid UTF-8 output

**Acceptance Criteria**:
- [x] All 4 tests present
- [x] Tests follow Arrange-Act-Assert pattern
- [x] Use FluentAssertions (Should().*)
- [x] All tests passing

**Evidence Needed**:
```bash
dotnet test tests/Acode.Application.Tests/Truncation/Strategies/HeadTailStrategyTests.cs -v normal
# Should show 4 passed
```

**Dependencies**: HeadTailStrategy implementation must exist

---

### 7. TailStrategyTests - Create New [🔄]

**Requirement**: Create tests for tail-only truncation strategy (used for logs/commands)

**Location**: `tests/Acode.Application.Tests/Truncation/Strategies/TailStrategyTests.cs`

**Spec Reference**: Testing Requirements lines 1359-1406

**Tests Required** (2 tests):

**Test 1 - Should_Keep_Only_Tail_Lines**:
- Input: 20 lines, keep only last 5 (TailLines = 5)
- Expected: Contains "Line 16" through "Line 20"
- Expected: Does NOT contain "Line 01" or "Line 10"
- Expected: Starts with "..." (omission marker)

**Test 2 - Should_Preserve_Complete_Lines**:
- Input: 5 lines, keep last 3, limit 100
- Expected: Result.Content.Split('\n') filtered to remove omission markers
- Expected: All lines complete (start with "Line" prefix)
- Expected: Contains "Line 3" and "Line 5"

**Acceptance Criteria**:
- [x] Both tests passing
- [x] Handles line boundary preservation (no mid-line truncation)
- [x] Omission marker at start (not end)

**Evidence Needed**:
```bash
dotnet test tests/Acode.Application.Tests/Truncation/Strategies/TailStrategyTests.cs -v normal
# Should show 2 passed
```

**Dependencies**: TailStrategy implementation must exist

---

### 8. ElementStrategyTests - Create New [🔄]

**Requirement**: Create tests for element-based truncation (JSON arrays/objects)

**Location**: `tests/Acode.Application.Tests/Truncation/Strategies/ElementStrategyTests.cs`

**Spec Reference**: Testing Requirements lines 1408-1458

**Tests Required** (2 tests):

**Test 1 - Should_Preserve_Valid_JSON_Array**:
- Input: JSON array with 20 items, show first 2, last 2
- Expected: result.Content starts with "[" and ends with "]"
- Expected: JsonSerializer.Deserialize() doesn't throw (valid JSON)
- Expected: Parsed result is JsonValueKind.Array

**Test 2 - Should_Show_Omitted_Count**:
- Input: Array of 100 integers, keep first 2 and last 2
- Expected: result.Content contains "96 items omitted"
- Expected: result.Metadata["omitted_elements"] == 96

**Acceptance Criteria**:
- [x] Both tests passing
- [x] JSON validity guaranteed
- [x] Element count metadata accurate

**Evidence Needed**:
```bash
dotnet test tests/Acode.Application.Tests/Truncation/Strategies/ElementStrategyTests.cs -v normal
# Should show 2 passed
```

**Dependencies**: ElementStrategy implementation must exist

---

### 9. ArtifactStorageTests - Create New [🔄]

**Requirement**: Create tests for artifact storage (FileSystemArtifactStore)

**Location**: `tests/Acode.Infrastructure.Tests/Truncation/ArtifactStorageTests.cs`

**Spec Reference**: Testing Requirements lines 1460-1525

**Tests Required** (5 tests):

**Test 1 - Should_Create_Artifact_With_Unique_ID**:
- Input: storage.CreateAsync(100KB content, "test_tool", "text/plain")
- Expected: artifact.Id matches `^art_\d+_[a-zA-Z0-9]+$` regex
- Expected: artifact.Size == 100000
- Expected: artifact.SourceTool == "test_tool"
- Expected: artifact.ContentType == "text/plain"

**Test 2 - Should_Retrieve_Artifact_By_ID**:
- Input: Create artifact, retrieve by ID
- Expected: Retrieved content equals original content

**Test 3 - Should_Handle_Concurrent_Artifact_Creation**:
- Input: Create 10 artifacts concurrently
- Expected: artifacts.Select(a => a.Id).Should().OnlyHaveUniqueItems() (no collisions)
- Expected: artifacts.Should().HaveCount(10)

**Test 4 - Should_Cleanup_Artifacts_On_Session_End**:
- Input: Create artifact, call CleanupAsync()
- Expected: Directory.Exists(sessionDir/.acode/artifacts) == false

**Test 5** (from spec): Validate artifact ID generation is cryptographically random

**Acceptance Criteria**:
- [x] All 5 tests passing
- [x] Artifact IDs are unique and random
- [x] Concurrent operations don't corrupt state
- [x] Cleanup works correctly

**Evidence Needed**:
```bash
dotnet test tests/Acode.Infrastructure.Tests/Truncation/ArtifactStorageTests.cs -v normal
# Should show 5 passed
```

**Dependencies**: FileSystemArtifactStore implementation must exist

---

## Phase 4: Integration & Performance Tests

### 10. TruncationIntegrationTests - Create New [🔄]

**Requirement**: Create end-to-end tests verifying full truncation flow

**Location**: `tests/Acode.Integration.Tests/Truncation/TruncationIntegrationTests.cs`

**Spec Reference**: Testing Requirements lines 1540-1592

**Tests Required** (2 tests):

**Test 1 - Should_Truncate_Large_Command_Output**:
- Setup: TruncationConfiguration with InlineLimit=1000, ArtifactThreshold=10000
- Setup: Tool override: execute_command uses Tail strategy, TailLines=50
- Input: 500 lines of log output
- Expected: result.WasTruncated = true
- Expected: result.Content contains "Log line 451" (tail)
- Expected: result.Content contains "Log line 500" (last line)
- Expected: result.Content doesn't contain "Log line 1"
- Expected: result.Metadata["strategy"] == "tail"

**Test 2 - Should_Create_Artifact_For_Massive_Content**:
- Setup: InlineLimit=1000, ArtifactThreshold=5000
- Input: 100KB content
- Expected: result.ArtifactId is not null/empty
- Expected: result.Content contains "[Artifact:" and result.ArtifactId
- Expected: result.Metadata["artifact_created"] == true
- Expected: storage.GetContentAsync(result.ArtifactId) returns original 100KB

**Acceptance Criteria**:
- [x] Both tests passing
- [x] Full pipeline works (configure → process → validate)
- [x] Artifacts persist correctly

**Evidence Needed**:
```bash
dotnet test tests/Acode.Integration.Tests/Truncation/TruncationIntegrationTests.cs -v normal
# Should show 2 passed
```

**Dependencies**: TruncationProcessor, all strategy implementations, FileSystemArtifactStore

---

### 11. TruncationPerformanceTests - Create New [🔄]

**Requirement**: Verify performance requirements from spec

**Location**: `tests/Acode.Performance.Tests/Truncation/TruncationPerformanceTests.cs`

**Spec Reference**: Testing Requirements lines 1597-1632, Description lines 31-32

**Performance Requirements**:
- Under 100KB: < 10 milliseconds
- 100KB to 10MB: < 100 milliseconds
- Over 10MB: async artifact streaming

**Tests Required** (2 tests):

**Test 1 - Should_Truncate_100KB_In_Under_10ms**:
- Input: 100KB content with HeadTailStrategy, InlineLimit=10000
- Process 1 iteration and measure
- Expected: stopwatch.ElapsedMilliseconds < 10

**Test 2 - Should_Handle_10_Concurrent_Truncations**:
- Input: 10 concurrent tasks, each processing 50KB
- Expected: All tasks complete in < 100ms total
- Expected: tasks.Select(t => t.Result.Content).Should().OnlyHaveUniqueItems()

**Acceptance Criteria**:
- [x] Both performance tests passing
- [x] Latency requirements met
- [x] Concurrent operations don't degrade performance

**Evidence Needed**:
```bash
dotnet test tests/Acode.Performance.Tests/Truncation/TruncationPerformanceTests.cs -v normal
# Should show 2 passed with timing assertions
```

**Dependencies**: All truncation implementations

---

## Phase 5: Documentation & Configuration

### 12. Add XML Documentation [🔄]

**Requirement**: Ensure all public types have complete XML documentation

**Check**:
- [ ] All public classes documented with `<summary>`, `<remarks>`
- [ ] All public properties documented with `<summary>`, `<value>`
- [ ] All public methods documented with `<summary>`, `<param>`, `<returns>`, `<exception>`
- [ ] All enums documented with values explained
- [ ] Examples in remarks where helpful

**Locations to Document**:
- TruncationResult.cs
- TruncationMetadata.cs
- TruncationConfiguration.cs
- All strategy classes
- FileSystemArtifactStore.cs
- GetArtifactTool.cs

**Evidence Needed**:
```bash
grep -r "///" src/Acode.Application/Truncation/ src/Acode.Infrastructure/Truncation/ | wc -l
# Should be 50+ documentation lines
```

**Dependencies**: None (documentation only)

---

### 13. Verify DI Registration [🔄]

**Requirement**: Ensure truncation services registered in DependencyInjection

**Location**: `src/Acode.Infrastructure/DependencyInjection.cs`

**Method to Add or Verify**:
```csharp
public static IServiceCollection AddTruncationServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var config = new TruncationConfiguration();
    configuration.GetSection("Truncation").Bind(config);
    services.AddSingleton(config);

    services.AddSingleton<ITruncationProcessor, TruncationProcessor>();
    services.AddSingleton<IArtifactStore, FileSystemArtifactStore>();

    // Register all strategies
    services.AddSingleton<ITruncationStrategy, HeadTailStrategy>();
    services.AddSingleton<ITruncationStrategy, TailStrategy>();
    services.AddSingleton<ITruncationStrategy, HeadStrategy>();
    services.AddSingleton<ITruncationStrategy, ElementStrategy>();

    return services;
}
```

**Acceptance Criteria**:
- [x] Method exists and can be called
- [x] All services registered
- [x] Configuration binds correctly
- [x] Strategies available for lookup

**Evidence Needed**:
```bash
grep -A 20 "AddTruncationServices" src/Acode.Infrastructure/DependencyInjection.cs
```

**Dependencies**: All application and infrastructure classes

---

### 14. Configure .agent/config.yml [🔄]

**Requirement**: Add truncation configuration section to config file

**Location**: `.agent/config.yml`

**Add Section**:
```yaml
Truncation:
  InlineLimit: 8000
  ArtifactThreshold: 50000
  DefaultStrategy: HeadTail
  ToolOverrides:
    execute_command:
      Strategy: Tail
      TailLines: 50
    read_file:
      Strategy: HeadTail
      HeadRatio: 0.6
    search_files:
      Strategy: Element
      FirstElements: 5
      LastElements: 5
```

**Acceptance Criteria**:
- [x] Section exists and valid YAML
- [x] Default limits present
- [x] At least 2-3 tool overrides configured
- [x] Configuration can be bound to TruncationConfiguration class

**Evidence Needed**: `grep -A 15 "Truncation:" .agent/config.yml`

**Dependencies**: None

---

### 15. Register GetArtifactTool with ToolSchemaRegistry [🔄]

**Requirement**: Ensure GetArtifactTool is discoverable as a tool

**Location**: ToolSchemaRegistry or DI configuration

**Action Required**:
- [ ] GetArtifactTool registered in ToolSchemaRegistry
- [ ] Tool parameters defined (id, start_line, end_line)
- [ ] Tool description documented
- [ ] Available to model at runtime

**Acceptance Criteria**:
- [x] Tool can be discovered by name "get_artifact"
- [x] Tool schema includes all parameters
- [x] Model can call tool and get artifact content

**Evidence Needed**:
```bash
# After running, should be discoverable:
# Call to ToolSchemaRegistry.GetTool("get_artifact") should return tool
```

**Dependencies**: GetArtifactTool (item 4)

---

## Phase 6: Build & Verification

### 16. Build Success [🔄]

**Requirement**: Project builds with zero errors and warnings

**Action**:
```bash
dotnet build --configuration Release --verbosity minimal
```

**Acceptance Criteria**:
- [x] Build succeeds
- [x] No CS* errors
- [x] No warnings in truncation code
- [x] All projects build (Application, Infrastructure, Tests)

**Evidence Needed**:
```bash
dotnet build 2>&1 | grep -i "error\|warning" | wc -l
# Should be 0
```

**Dependencies**: All previous items

---

### 17. Test Suite Pass (20+ Tests) [🔄]

**Requirement**: All truncation tests pass

**Action**:
```bash
dotnet test --filter "FullyQualifiedName~Truncation" --verbosity normal
```

**Acceptance Criteria**:
- [x] HeadTailStrategyTests: 4 passing
- [x] TailStrategyTests: 2 passing
- [x] ElementStrategyTests: 2 passing
- [x] ArtifactStorageTests: 5 passing
- [x] TruncationIntegrationTests: 2 passing
- [x] TruncationPerformanceTests: 2 passing
- [x] Existing config/limits tests: 2+ passing
- [x] **Total: 20+ tests passing**

**Evidence Needed**:
```bash
dotnet test --filter "FullyQualifiedName~Truncation" -v normal 2>&1 | grep "passed"
# Should show "20 passed" or higher
```

**Dependencies**: All test files (items 6-11)

---

### 18. Code Review Audit [🔄]

**Requirement**: Manual audit against spec requirements

**Checklist**:
- [x] All file paths match spec (or documented as intentional changes)
- [x] All classes/interfaces implemented, not stubbed
- [x] Tests follow Arrange-Act-Assert pattern
- [x] Async operations properly handled (no deadlocks)
- [x] Performance requirements verified by tests
- [x] Security requirements met (artifact isolation, cleanup)
- [x] Error handling appropriate (fail-safe)
- [x] No direct DateTime.Now (if timestamps needed, injected)
- [x] Thread safety (concurrent operations don't corrupt state)
- [x] Memory constraints (≤ 2x input size)

**Evidence Needed**: Auditor sign-off checklist

**Dependencies**: All implementation items

---

### 19. User Verification Scenarios [🔄]

**Requirement**: Manually verify all 6 user scenarios work end-to-end

**Scenarios** (from spec lines 1639-1678):

1. **Small Content No Truncation**:
   - [ ] Read small file (< 8000 chars)
   - [ ] Verify: Full content in result
   - [ ] Verify: No truncation marker
   - [ ] Verify: No artifact created

2. **Medium Content Truncation**:
   - [ ] Read medium file (8000-50000 chars)
   - [ ] Verify: Content truncated
   - [ ] Verify: Marker present
   - [ ] Verify: Head and tail preserved

3. **Large Content Artifact**:
   - [ ] Read large file (> 50000 chars)
   - [ ] Verify: Artifact created
   - [ ] Verify: Reference in result
   - [ ] Verify: Can retrieve via get_artifact

4. **Tail Strategy**:
   - [ ] Run command with long output
   - [ ] Verify: Only tail preserved
   - [ ] Verify: Most recent lines visible

5. **JSON Truncation**:
   - [ ] Search producing many results
   - [ ] Verify: Valid JSON preserved
   - [ ] Verify: First/last elements present
   - [ ] Verify: Count of omitted shown

6. **Partial Artifact Retrieval**:
   - [ ] Get artifact with line range
   - [ ] Verify: Only requested lines returned
   - [ ] Verify: Correct content

**Acceptance Criteria**:
- [x] All 6 scenarios pass manual testing
- [x] Output matches expected format
- [x] No errors or exceptions

**Dependencies**: Full implementation (items 1-18)

---

## Final Steps

### 20. Commit & Push [🔄]

**Requirement**: All changes committed to feature branch

**Action**:
```bash
git add .
git commit -m "feat(task-007c): complete truncation and artifact tests

- Add HeadTailStrategyTests (4 tests, including UTF-8)
- Add TailStrategyTests (2 tests)
- Add ElementStrategyTests (2 tests)
- Add ArtifactStorageTests (5 tests)
- Add TruncationIntegrationTests (2 tests)
- Add TruncationPerformanceTests (2 tests)
- Create GetArtifactTool for artifact retrieval
- Add comprehensive XML documentation
- Verify DI registration
- Configure truncation in .agent/config.yml
- All 20+ tests passing
- Performance requirements verified

🤖 Generated with Claude Code"

git push origin feature/task-006a-fix-gaps
```

**Acceptance Criteria**:
- [x] All changes staged and committed
- [x] Commits follow Conventional Commits
- [x] Multiple commits if substantial work
- [x] Pushed to feature branch

**Evidence Needed**: `git log --oneline | head -5`

**Dependencies**: All previous items

---

### 21. Create Pull Request [🔄]

**Requirement**: Create PR for code review

**Action**: Use `gh pr create` or GitHub UI

**PR Description Should Include**:
- Summary of all tests added
- Summary of implementations (GetArtifactTool, etc.)
- Confirmation of performance requirements met
- User verification scenarios completed
- Link to task-007c specification
- Link to this completion checklist

**Acceptance Criteria**:
- [x] PR created and linked
- [x] All checks passing
- [x] Ready for review

**Evidence Needed**: PR URL

**Dependencies**: Item 20

---

## Summary

### Total Checklist Items: 21

### Implementation Phases:
1. **Interface Verification** (items 1-3): Confirm existing interfaces
2. **Create Missing Components** (items 4-5): GetArtifactTool, ArtifactReference
3. **Unit Tests** (items 6-9): 20+ comprehensive tests for strategies and storage
4. **Integration & Performance** (items 10-11): End-to-end and performance validation
5. **Documentation & Configuration** (items 12-15): Complete documentation and setup
6. **Build & Verification** (items 16-19): Build, tests, audit, scenarios
7. **Git & PR** (items 20-21): Commit and create pull request

### Test Count Target: 20+ passing tests
- HeadTailStrategyTests: 4
- TailStrategyTests: 2
- ElementStrategyTests: 2
- ArtifactStorageTests: 5
- TruncationIntegrationTests: 2
- TruncationPerformanceTests: 2
- Existing tests (config/limits): 2+

### Key Performance Requirements:
- ✅ < 10ms for 100KB content
- ✅ < 100ms for 100KB-10MB content
- ✅ Async streaming for 10MB+

### Key Security Requirements:
- ✅ Artifacts session-scoped
- ✅ Automatic cleanup
- ✅ Cryptographically-random IDs
- ✅ Session validation on retrieval

---

## How to Continue

If context runs low during implementation:
1. Mark currently working item as IN PROGRESS [🔄]
2. Document exact stopping point (file path, line number, test name)
3. Update this checklist with progress
4. Commit and push
5. Next session continues from marked item

Example:
```
### 9. ArtifactStorageTests - Create New [🔄]
**Status**: IN PROGRESS - Completed tests 1-3, test 4 needs implementation
**Last worked on**: 2026-01-13 14:35 UTC
**Next step**: Implement Should_Cleanup_Artifacts_On_Session_End test
**File**: tests/Acode.Infrastructure.Tests/Truncation/ArtifactStorageTests.cs:line 1512
```

---

## References

- **Task Specification**: docs/tasks/refined-tasks/Epic 01/task-007c-truncation-artifact-attachment-rules.md
- **Gap Analysis**: docs/implementation-plans/task-007c-gap-analysis.md
- **Testing Requirements**: Spec lines 1253-1632
- **Implementation Prompt**: Spec lines 1681-1734
- **User Verification**: Spec lines 1637-1678
- **CLAUDE.md Section 3.2**: Gap Analysis methodology
- **Task-049d Checklist**: Reference template for this document format
