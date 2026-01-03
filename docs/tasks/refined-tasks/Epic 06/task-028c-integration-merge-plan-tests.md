# Task 028.c: Integration Merge Plan + Tests

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 028 (Merge), Task 028.a (Heuristics), Task 028.b (Graph)  

---

## Description

Task 028.c implements integration testing for the merge coordinator. End-to-end tests MUST verify parallel task execution and merging. Merge plans MUST be validated against real scenarios.

Integration tests MUST cover common merge scenarios. Multiple tasks modifying different files MUST merge cleanly. Overlapping changes MUST trigger appropriate conflicts. Edge cases MUST be handled.

Test fixtures MUST be reproducible. Test repositories MUST be created programmatically. Test scenarios MUST be documented. Results MUST be verifiable.

### Business Value

Integration tests enable:
- Confidence in merge system
- Regression prevention
- Edge case coverage
- Documentation via tests
- Safe refactoring

### Scope Boundaries

This task covers integration testing. Unit tests are in individual task specs. Merge logic is in Task 028.

### Integration Points

- Task 028: Merge coordinator under test
- Task 028.a: Heuristics under test
- Task 028.b: Dependency graph under test
- Task 027: Worker pool for execution

### Failure Modes

- Test flaky → Retry with isolation
- Fixture corrupt → Rebuild
- Timeout → Extend or optimize
- Resource leak → Cleanup handler

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Integration Test | Multi-component test |
| Fixture | Preconditioned test state |
| Scenario | Specific test case |
| Golden File | Expected output reference |
| Flaky Test | Non-deterministic failure |
| Test Harness | Test execution framework |
| Cleanup | Post-test resource release |

---

## Out of Scope

- Load testing
- Chaos engineering
- Mutation testing
- Fuzzing
- UI testing
- Cross-platform matrix

---

## Functional Requirements

### FR-001 to FR-025: Test Fixtures

- FR-001: Test repo MUST be created
- FR-002: Repo MUST be isolated
- FR-003: Repo MUST be disposable
- FR-004: Files MUST be created programmatically
- FR-005: Commits MUST be created
- FR-006: Branches MUST be created
- FR-007: Fixture MUST be reproducible
- FR-008: Same seed MUST produce same fixture
- FR-009: Fixture MUST be fast to create
- FR-010: Creation MUST be <5 seconds
- FR-011: Cleanup MUST be automatic
- FR-012: Cleanup MUST be thorough
- FR-013: Parallel fixtures MUST be isolated
- FR-014: Fixture ID MUST be unique
- FR-015: Fixture path MUST be temporary
- FR-016: Fixture MUST include config
- FR-017: Config MUST be test-specific
- FR-018: Fixture MUST include queue
- FR-019: Queue MUST be empty initially
- FR-020: Fixture MUST include workers
- FR-021: Worker count MUST be configurable
- FR-022: Default: 2 workers for tests
- FR-023: Fixture MUST be resettable
- FR-024: Reset MUST restore initial state
- FR-025: Fixture logging MUST be available

### FR-026 to FR-055: Test Scenarios

- FR-026: Scenario: Clean parallel merge
- FR-027: Two tasks, different files, merge OK
- FR-028: Scenario: Same file, different lines
- FR-029: Both succeed, merge OK
- FR-030: Scenario: Same file, same lines
- FR-031: Conflict detected, merge blocked
- FR-032: Scenario: Dependent tasks
- FR-033: Run in order, no conflict
- FR-034: Scenario: Overlapping function
- FR-035: Same function, conflict warning
- FR-036: Scenario: Lock file changes
- FR-037: Regenerate lock file
- FR-038: Scenario: Binary file conflict
- FR-039: Block merge, require resolution
- FR-040: Scenario: Rollback on failure
- FR-041: Merge fails validation, rollback
- FR-042: Scenario: Partial completion
- FR-043: One task fails, other merges
- FR-044: Scenario: All tasks fail
- FR-045: Queue empty, nothing to merge
- FR-046: Scenario: Cycle detection
- FR-047: Circular dependency rejected
- FR-048: Scenario: Large parallel batch
- FR-049: 10 tasks, 5 workers, all merge
- FR-050: Scenario: Priority ordering
- FR-051: High priority merges first
- FR-052: Scenario: Timeout handling
- FR-053: Slow task times out, recovery
- FR-054: Scenario: Worker crash
- FR-055: Task recovered, merge continues

### FR-056 to FR-075: Test Verification

- FR-056: Merge result MUST be verified
- FR-057: Files MUST match expected
- FR-058: Git history MUST be correct
- FR-059: Commits MUST be present
- FR-060: Branches MUST be cleaned
- FR-061: Queue state MUST be verified
- FR-062: All tasks MUST be completed
- FR-063: No pending tasks MUST remain
- FR-064: Failed tasks MUST be marked
- FR-065: Events MUST be verified
- FR-066: Expected events MUST fire
- FR-067: Event order MUST be correct
- FR-068: Metrics MUST be verified
- FR-069: Counters MUST increment
- FR-070: Timing MUST be reasonable
- FR-071: Logs MUST be captured
- FR-072: Error logs MUST be checked
- FR-073: No unexpected errors
- FR-074: Warnings MUST be expected
- FR-075: Golden file comparison MUST work

---

## Non-Functional Requirements

- NFR-001: Each test MUST complete in <30s
- NFR-002: Full suite MUST complete in <5min
- NFR-003: Tests MUST be parallelizable
- NFR-004: Tests MUST be deterministic
- NFR-005: No flaky tests allowed
- NFR-006: Tests MUST clean up
- NFR-007: Tests MUST be documented
- NFR-008: Failure messages MUST be clear
- NFR-009: Tests MUST run in CI
- NFR-010: Coverage MUST be tracked

---

## User Manual Documentation

### Running Tests

```bash
# Run all merge integration tests
dotnet test --filter "Category=MergeIntegration"

# Run specific scenario
dotnet test --filter "FullyQualifiedName~CleanParallelMerge"

# Run with verbose output
dotnet test --filter "Category=MergeIntegration" -v detailed

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
```

### Test Scenarios

| Scenario | Description | Expected |
|----------|-------------|----------|
| CleanParallelMerge | 2 tasks, different files | Both merge |
| SameFileDifferentLines | Same file, no overlap | Merge OK |
| SameFileSameLines | Overlapping edits | Conflict |
| DependentTasks | Task B depends on A | Sequential |
| LockFileConflict | Both edit package-lock | Regenerate |
| BinaryConflict | Both edit image | Block |
| RollbackOnFailure | Validation fails | Rollback |
| LargeBatch | 10 tasks parallel | All merge |

### Writing New Tests

```csharp
[Fact]
[Trait("Category", "MergeIntegration")]
public async Task CleanParallelMerge_DifferentFiles_BothMerge()
{
    // Arrange
    await using var fixture = await MergeTestFixture.CreateAsync();
    
    var task1 = fixture.CreateTask("task-1", 
        files: ["src/FileA.cs"]);
    var task2 = fixture.CreateTask("task-2", 
        files: ["src/FileB.cs"]);
    
    // Act
    await fixture.EnqueueAsync(task1, task2);
    await fixture.ExecuteAllAsync();
    
    // Assert
    await fixture.AssertMergedAsync(task1, task2);
    await fixture.AssertFileExistsAsync("src/FileA.cs");
    await fixture.AssertFileExistsAsync("src/FileB.cs");
    await fixture.AssertQueueEmptyAsync();
}
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Fixtures create correctly
- [ ] AC-002: Fixtures cleanup
- [ ] AC-003: Clean merge scenario passes
- [ ] AC-004: Conflict scenario passes
- [ ] AC-005: Dependency scenario passes
- [ ] AC-006: Rollback scenario passes
- [ ] AC-007: Large batch passes
- [ ] AC-008: All scenarios documented
- [ ] AC-009: Tests are deterministic
- [ ] AC-010: Tests run in CI
- [ ] AC-011: Coverage tracked
- [ ] AC-012: No flaky tests

---

## Testing Requirements

### Scenario Tests

- [ ] ST-001: CleanParallelMerge
- [ ] ST-002: SameFileDifferentLines
- [ ] ST-003: SameFileSameLines
- [ ] ST-004: DependentTasks
- [ ] ST-005: OverlappingFunction
- [ ] ST-006: LockFileConflict
- [ ] ST-007: BinaryConflict
- [ ] ST-008: RollbackOnFailure
- [ ] ST-009: PartialCompletion
- [ ] ST-010: CycleDetection
- [ ] ST-011: LargeBatch
- [ ] ST-012: PriorityOrdering
- [ ] ST-013: TimeoutHandling
- [ ] ST-014: WorkerCrash

### Performance Tests

- [ ] PT-001: 10 tasks in <30s
- [ ] PT-002: 50 tasks in <2min
- [ ] PT-003: Memory bounded

---

## Implementation Prompt

### Test Fixture

```csharp
public class MergeTestFixture : IAsyncDisposable
{
    private readonly string _repoPath;
    private readonly ITaskQueue _queue;
    private readonly IWorkerPool _workers;
    private readonly IMergeCoordinator _coordinator;
    
    public static async Task<MergeTestFixture> CreateAsync(
        MergeTestOptions? options = null)
    {
        options ??= MergeTestOptions.Default;
        
        var tempPath = Path.Combine(
            Path.GetTempPath(), 
            $"acode-test-{Ulid.NewUlid()}");
        
        Directory.CreateDirectory(tempPath);
        
        // Initialize git repo
        await Git.InitAsync(tempPath);
        
        // Create initial commit
        await File.WriteAllTextAsync(
            Path.Combine(tempPath, "README.md"), 
            "# Test Repo");
        await Git.CommitAsync(tempPath, "Initial commit");
        
        // Setup services
        var services = BuildServices(tempPath, options);
        
        return new MergeTestFixture(
            tempPath,
            services.GetRequiredService<ITaskQueue>(),
            services.GetRequiredService<IWorkerPool>(),
            services.GetRequiredService<IMergeCoordinator>());
    }
    
    public TaskSpec CreateTask(string id, 
        string[]? files = null,
        string[]? dependencies = null)
    {
        // Create task with worktree changes
    }
    
    public async Task EnqueueAsync(params TaskSpec[] tasks)
    {
        foreach (var task in tasks)
            await _queue.EnqueueAsync(task);
    }
    
    public async Task ExecuteAllAsync()
    {
        await _workers.StartAsync();
        await WaitForQueueEmptyAsync();
        await _coordinator.MergeAllAsync();
    }
    
    public async Task AssertMergedAsync(params TaskSpec[] tasks)
    {
        foreach (var task in tasks)
        {
            var status = await _queue.GetStatusAsync(task.Id);
            Assert.Equal(TaskStatus.Completed, status);
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        await _workers.StopAsync(force: true);
        Directory.Delete(_repoPath, recursive: true);
    }
}
```

---

**End of Task 028.c Specification**