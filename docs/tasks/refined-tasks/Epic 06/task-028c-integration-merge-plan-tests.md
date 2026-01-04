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

### File Structure

```
tests/
└── Acode.Integration.Tests/
    └── Merge/
        ├── MergeTestFixture.cs
        ├── MergeTestOptions.cs
        ├── TestGitRepository.cs
        ├── TestTaskFactory.cs
        ├── Assertions/
        │   ├── MergeAssertions.cs
        │   ├── GitAssertions.cs
        │   └── QueueAssertions.cs
        ├── Scenarios/
        │   ├── CleanParallelMergeTests.cs
        │   ├── SameFileDifferentLinesTests.cs
        │   ├── SameFileSameLinesTests.cs
        │   ├── DependentTasksTests.cs
        │   ├── OverlappingFunctionTests.cs
        │   ├── LockFileConflictTests.cs
        │   ├── BinaryConflictTests.cs
        │   ├── RollbackOnFailureTests.cs
        │   ├── PartialCompletionTests.cs
        │   ├── CycleDetectionTests.cs
        │   ├── LargeBatchTests.cs
        │   ├── PriorityOrderingTests.cs
        │   ├── TimeoutHandlingTests.cs
        │   └── WorkerCrashTests.cs
        └── Golden/
            ├── expected-merge-result-1.txt
            └── expected-git-log-1.txt
```

### Part 1: Test Fixture

```csharp
// File: tests/Acode.Integration.Tests/Merge/MergeTestFixture.cs
namespace Acode.Integration.Tests.Merge;

/// <summary>
/// Test fixture for merge integration tests.
/// Creates isolated git repositories with full services.
/// </summary>
public sealed class MergeTestFixture : IAsyncDisposable
{
    private readonly string _repoPath;
    private readonly string _workspaceRoot;
    private readonly ServiceProvider _services;
    private readonly ITaskQueue _queue;
    private readonly IWorkerPool _workers;
    private readonly IMergeCoordinator _coordinator;
    private readonly IDependencyGraph _graph;
    private readonly ILogger<MergeTestFixture> _logger;
    
    private readonly List<TaskSpec> _createdTasks = new();
    private readonly CancellationTokenSource _cts = new();
    
    public string RepoPath => _repoPath;
    public ITaskQueue Queue => _queue;
    public IWorkerPool Workers => _workers;
    public IMergeCoordinator Coordinator => _coordinator;
    public IDependencyGraph Graph => _graph;
    
    private MergeTestFixture(
        string repoPath,
        string workspaceRoot,
        ServiceProvider services)
    {
        _repoPath = repoPath;
        _workspaceRoot = workspaceRoot;
        _services = services;
        _queue = services.GetRequiredService<ITaskQueue>();
        _workers = services.GetRequiredService<IWorkerPool>();
        _coordinator = services.GetRequiredService<IMergeCoordinator>();
        _graph = services.GetRequiredService<IDependencyGraph>();
        _logger = services.GetRequiredService<ILogger<MergeTestFixture>>();
    }
    
    /// <summary>
    /// Create a new test fixture with isolated repository.
    /// </summary>
    public static async Task<MergeTestFixture> CreateAsync(
        MergeTestOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= MergeTestOptions.Default;
        
        // Create temp directories
        var workspaceRoot = Path.Combine(
            Path.GetTempPath(),
            "acode-test",
            $"workspace-{Ulid.NewUlid()}");
        
        var repoPath = Path.Combine(workspaceRoot, "repo");
        Directory.CreateDirectory(repoPath);
        
        // Initialize git repository
        var git = new TestGitRepository(repoPath);
        await git.InitAsync(ct);
        
        // Create initial content
        await git.WriteFileAsync("README.md", "# Test Repository\n", ct);
        await git.CommitAsync("Initial commit", ct);
        
        // Build services
        var services = BuildServices(workspaceRoot, repoPath, options);
        
        var fixture = new MergeTestFixture(repoPath, workspaceRoot, services);
        
        fixture._logger.LogInformation(
            "Created test fixture at {Path}", repoPath);
        
        return fixture;
    }
    
    private static ServiceProvider BuildServices(
        string workspaceRoot,
        string repoPath,
        MergeTestOptions options)
    {
        var services = new ServiceCollection();
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
        });
        
        // Configuration
        services.AddSingleton(new AgentConfig
        {
            WorkspacePath = workspaceRoot,
            RepositoryPath = repoPath,
            Mode = OperatingMode.LocalOnly
        });
        
        // Queue (SQLite in temp location)
        var queueDbPath = Path.Combine(workspaceRoot, "queue.db");
        services.AddSingleton<ITaskQueue>(sp =>
            new SqliteTaskQueue(queueDbPath, sp.GetRequiredService<ILogger<SqliteTaskQueue>>()));
        
        // Workers
        services.AddSingleton<IWorkerPool>(sp =>
            new LocalWorkerPool(
                options.WorkerCount,
                sp.GetRequiredService<ITaskQueue>(),
                sp.GetRequiredService<ILogger<LocalWorkerPool>>()));
        
        // Merge coordinator
        services.AddSingleton<IConflictHeuristics, ConflictHeuristics>();
        services.AddSingleton<IDependencyGraph, DependencyGraph>();
        services.AddSingleton<IFileHintGenerator, FileHintGenerator>();
        services.AddSingleton<IMergeCoordinator, MergeCoordinator>();
        
        return services.BuildServiceProvider();
    }
    
    /// <summary>
    /// Create a task specification for testing.
    /// </summary>
    public TaskSpec CreateTask(
        string id,
        string[]? files = null,
        string[]? dependencies = null,
        TaskPriority priority = TaskPriority.Normal,
        string? prompt = null)
    {
        var spec = new TaskSpec
        {
            Id = id,
            Title = $"Test task {id}",
            Prompt = prompt ?? $"Test prompt for {id}",
            Files = files?.ToList() ?? new List<string>(),
            Dependencies = dependencies?.ToList(),
            Priority = priority,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        _createdTasks.Add(spec);
        return spec;
    }
    
    /// <summary>
    /// Create a task that will write specific content to a file.
    /// </summary>
    public async Task<TaskSpec> CreateTaskWithFileChangeAsync(
        string taskId,
        string filePath,
        string content,
        CancellationToken ct = default)
    {
        var spec = CreateTask(taskId, files: new[] { filePath });
        
        // Create worktree for this task
        var worktreePath = Path.Combine(_workspaceRoot, "worktrees", taskId);
        Directory.CreateDirectory(worktreePath);
        
        var git = new TestGitRepository(_repoPath);
        await git.CreateWorktreeAsync(worktreePath, $"task-{taskId}", ct);
        
        // Write the file in worktree
        var fullPath = Path.Combine(worktreePath, filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content, ct);
        
        // Stage and commit
        var worktreeGit = new TestGitRepository(worktreePath);
        await worktreeGit.AddAsync(filePath, ct);
        await worktreeGit.CommitAsync($"Task {taskId}: Update {filePath}", ct);
        
        return spec;
    }
    
    /// <summary>
    /// Enqueue tasks for execution.
    /// </summary>
    public async Task EnqueueAsync(params TaskSpec[] tasks)
    {
        foreach (var task in tasks)
        {
            await _queue.EnqueueAsync(task, _cts.Token);
            _graph.AddTask(task);
        }
    }
    
    /// <summary>
    /// Start workers and wait for all tasks to complete.
    /// </summary>
    public async Task ExecuteAllAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        
        using var cts = CancellationTokenSource
            .CreateLinkedTokenSource(_cts.Token);
        cts.CancelAfter(timeout.Value);
        
        await _workers.StartAsync(cts.Token);
        
        // Wait for queue to drain
        while (await _queue.GetPendingCountAsync(cts.Token) > 0)
        {
            await Task.Delay(100, cts.Token);
        }
        
        // Wait for workers to finish current tasks
        await _workers.DrainAsync(cts.Token);
    }
    
    /// <summary>
    /// Execute merge for all completed tasks.
    /// </summary>
    public async Task MergeAllAsync()
    {
        await _coordinator.MergeAllAsync(_cts.Token);
    }
    
    /// <summary>
    /// Execute full workflow: enqueue, execute, merge.
    /// </summary>
    public async Task RunFullWorkflowAsync(params TaskSpec[] tasks)
    {
        await EnqueueAsync(tasks);
        await ExecuteAllAsync();
        await MergeAllAsync();
    }
    
    /// <summary>
    /// Get the content of a file in the main repository.
    /// </summary>
    public async Task<string> ReadFileAsync(string relativePath)
    {
        var fullPath = Path.Combine(_repoPath, relativePath);
        return await File.ReadAllTextAsync(fullPath);
    }
    
    /// <summary>
    /// Check if a file exists in the main repository.
    /// </summary>
    public bool FileExists(string relativePath)
    {
        var fullPath = Path.Combine(_repoPath, relativePath);
        return File.Exists(fullPath);
    }
    
    /// <summary>
    /// Get git log entries.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetGitLogAsync(int count = 10)
    {
        var git = new TestGitRepository(_repoPath);
        return await git.GetLogAsync(count, _cts.Token);
    }
    
    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        
        try
        {
            await _workers.StopAsync(force: true);
        }
        catch { /* ignore */ }
        
        await _services.DisposeAsync();
        
        // Cleanup temp directory
        try
        {
            if (Directory.Exists(_workspaceRoot))
            {
                // Force delete git worktrees first
                var worktreesPath = Path.Combine(_workspaceRoot, "worktrees");
                if (Directory.Exists(worktreesPath))
                {
                    foreach (var wt in Directory.GetDirectories(worktreesPath))
                    {
                        var git = new TestGitRepository(_repoPath);
                        await git.RemoveWorktreeAsync(wt, force: true);
                    }
                }
                
                Directory.Delete(_workspaceRoot, recursive: true);
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail test
            Console.WriteLine($"Warning: Failed to cleanup {_workspaceRoot}: {ex.Message}");
        }
        
        _cts.Dispose();
    }
}

// File: tests/Acode.Integration.Tests/Merge/MergeTestOptions.cs
namespace Acode.Integration.Tests.Merge;

public sealed record MergeTestOptions
{
    public int WorkerCount { get; init; } = 2;
    public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public bool EnableLogging { get; init; } = true;
    public LogLevel MinLogLevel { get; init; } = LogLevel.Debug;
    
    public static MergeTestOptions Default => new();
    
    public static MergeTestOptions LargeBatch => new()
    {
        WorkerCount = 5,
        DefaultTimeout = TimeSpan.FromMinutes(2)
    };
}
```

### Part 2: Git Test Helpers

```csharp
// File: tests/Acode.Integration.Tests/Merge/TestGitRepository.cs
namespace Acode.Integration.Tests.Merge;

/// <summary>
/// Helper for git operations in tests.
/// </summary>
public sealed class TestGitRepository
{
    private readonly string _path;
    
    public string Path => _path;
    
    public TestGitRepository(string path)
    {
        _path = path;
    }
    
    public async Task InitAsync(CancellationToken ct = default)
    {
        await RunGitAsync(["init"], ct);
        await RunGitAsync(["config", "user.name", "Test User"], ct);
        await RunGitAsync(["config", "user.email", "test@example.com"], ct);
    }
    
    public async Task WriteFileAsync(
        string relativePath,
        string content,
        CancellationToken ct = default)
    {
        var fullPath = System.IO.Path.Combine(_path, relativePath);
        var dir = System.IO.Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(fullPath, content, ct);
    }
    
    public async Task AddAsync(string path = ".", CancellationToken ct = default)
    {
        await RunGitAsync(["add", path], ct);
    }
    
    public async Task CommitAsync(string message, CancellationToken ct = default)
    {
        await RunGitAsync(["commit", "-m", message, "--allow-empty"], ct);
    }
    
    public async Task<string> CreateBranchAsync(
        string branchName,
        CancellationToken ct = default)
    {
        await RunGitAsync(["checkout", "-b", branchName], ct);
        return branchName;
    }
    
    public async Task CheckoutAsync(string branchName, CancellationToken ct = default)
    {
        await RunGitAsync(["checkout", branchName], ct);
    }
    
    public async Task CreateWorktreeAsync(
        string worktreePath,
        string branchName,
        CancellationToken ct = default)
    {
        await RunGitAsync(
            ["worktree", "add", "-b", branchName, worktreePath],
            ct);
    }
    
    public async Task RemoveWorktreeAsync(
        string worktreePath,
        bool force = false,
        CancellationToken ct = default)
    {
        var args = new List<string> { "worktree", "remove" };
        if (force) args.Add("--force");
        args.Add(worktreePath);
        
        await RunGitAsync(args, ct);
    }
    
    public async Task MergeAsync(
        string branchName,
        CancellationToken ct = default)
    {
        await RunGitAsync(["merge", branchName, "--no-edit"], ct);
    }
    
    public async Task<IReadOnlyList<string>> GetLogAsync(
        int count = 10,
        CancellationToken ct = default)
    {
        var output = await RunGitAsync(
            ["log", $"--oneline", $"-n{count}"],
            ct);
        
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }
    
    public async Task<string> GetCurrentBranchAsync(CancellationToken ct = default)
    {
        return (await RunGitAsync(["branch", "--show-current"], ct)).Trim();
    }
    
    public async Task<IReadOnlyList<string>> GetBranchesAsync(
        CancellationToken ct = default)
    {
        var output = await RunGitAsync(["branch", "--list"], ct);
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(b => b.Trim().TrimStart('*').Trim())
            .ToList();
    }
    
    public async Task<bool> HasUncommittedChangesAsync(
        CancellationToken ct = default)
    {
        var output = await RunGitAsync(["status", "--porcelain"], ct);
        return !string.IsNullOrWhiteSpace(output);
    }
    
    private async Task<string> RunGitAsync(
        IEnumerable<string> args,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = _path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);
        
        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        
        if (process.ExitCode != 0)
        {
            throw new Exception(
                $"Git command failed: git {string.Join(" ", args)}\n{stderr}");
        }
        
        return stdout;
    }
}

// File: tests/Acode.Integration.Tests/Merge/TestTaskFactory.cs
namespace Acode.Integration.Tests.Merge;

/// <summary>
/// Factory for creating test task scenarios.
/// </summary>
public static class TestTaskFactory
{
    public static TaskSpec Simple(string id, params string[] files) =>
        new()
        {
            Id = id,
            Title = $"Task {id}",
            Prompt = $"Simple task {id}",
            Files = files.ToList(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    
    public static TaskSpec WithDependency(
        string id,
        string dependsOn,
        params string[] files) =>
        new()
        {
            Id = id,
            Title = $"Task {id}",
            Prompt = $"Task {id} depends on {dependsOn}",
            Files = files.ToList(),
            Dependencies = new List<string> { dependsOn },
            CreatedAt = DateTimeOffset.UtcNow
        };
    
    public static TaskSpec HighPriority(string id, params string[] files) =>
        new()
        {
            Id = id,
            Title = $"High priority task {id}",
            Prompt = $"Priority task {id}",
            Files = files.ToList(),
            Priority = TaskPriority.High,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
```

### Part 3: Test Assertions

```csharp
// File: tests/Acode.Integration.Tests/Merge/Assertions/MergeAssertions.cs
namespace Acode.Integration.Tests.Merge.Assertions;

public static class MergeAssertions
{
    /// <summary>
    /// Assert that tasks were merged successfully.
    /// </summary>
    public static async Task AssertMergedAsync(
        this MergeTestFixture fixture,
        params TaskSpec[] tasks)
    {
        foreach (var task in tasks)
        {
            var status = await fixture.Queue.GetStatusAsync(task.Id);
            Assert.Equal(QueuedTaskStatus.Completed, status);
        }
    }
    
    /// <summary>
    /// Assert that a task failed.
    /// </summary>
    public static async Task AssertFailedAsync(
        this MergeTestFixture fixture,
        string taskId,
        string? expectedError = null)
    {
        var status = await fixture.Queue.GetStatusAsync(taskId);
        Assert.Equal(QueuedTaskStatus.Failed, status);
        
        if (expectedError != null)
        {
            var result = await fixture.Queue.GetResultAsync(taskId);
            Assert.Contains(expectedError, result?.Error ?? "");
        }
    }
    
    /// <summary>
    /// Assert queue is empty.
    /// </summary>
    public static async Task AssertQueueEmptyAsync(
        this MergeTestFixture fixture)
    {
        var pending = await fixture.Queue.GetPendingCountAsync();
        Assert.Equal(0, pending);
    }
    
    /// <summary>
    /// Assert no tasks are in progress.
    /// </summary>
    public static async Task AssertNoTasksInProgressAsync(
        this MergeTestFixture fixture)
    {
        var inProgress = await fixture.Queue.GetInProgressCountAsync();
        Assert.Equal(0, inProgress);
    }
}

// File: tests/Acode.Integration.Tests/Merge/Assertions/GitAssertions.cs
namespace Acode.Integration.Tests.Merge.Assertions;

public static class GitAssertions
{
    /// <summary>
    /// Assert file exists with expected content.
    /// </summary>
    public static async Task AssertFileContentAsync(
        this MergeTestFixture fixture,
        string relativePath,
        string expectedContent)
    {
        Assert.True(fixture.FileExists(relativePath),
            $"File {relativePath} does not exist");
        
        var content = await fixture.ReadFileAsync(relativePath);
        Assert.Equal(expectedContent, content);
    }
    
    /// <summary>
    /// Assert file exists.
    /// </summary>
    public static void AssertFileExists(
        this MergeTestFixture fixture,
        string relativePath)
    {
        Assert.True(fixture.FileExists(relativePath),
            $"Expected file {relativePath} to exist");
    }
    
    /// <summary>
    /// Assert file does not exist.
    /// </summary>
    public static void AssertFileNotExists(
        this MergeTestFixture fixture,
        string relativePath)
    {
        Assert.False(fixture.FileExists(relativePath),
            $"Expected file {relativePath} to not exist");
    }
    
    /// <summary>
    /// Assert git log contains expected commits.
    /// </summary>
    public static async Task AssertCommitExistsAsync(
        this MergeTestFixture fixture,
        string messageSubstring)
    {
        var log = await fixture.GetGitLogAsync(20);
        Assert.Contains(log, entry => entry.Contains(messageSubstring));
    }
    
    /// <summary>
    /// Assert branch was cleaned up.
    /// </summary>
    public static async Task AssertBranchRemovedAsync(
        this MergeTestFixture fixture,
        string branchName)
    {
        var git = new TestGitRepository(fixture.RepoPath);
        var branches = await git.GetBranchesAsync();
        Assert.DoesNotContain(branchName, branches);
    }
}

// File: tests/Acode.Integration.Tests/Merge/Assertions/QueueAssertions.cs
namespace Acode.Integration.Tests.Merge.Assertions;

public static class QueueAssertions
{
    /// <summary>
    /// Assert task has specific status.
    /// </summary>
    public static async Task AssertTaskStatusAsync(
        this MergeTestFixture fixture,
        string taskId,
        QueuedTaskStatus expected)
    {
        var status = await fixture.Queue.GetStatusAsync(taskId);
        Assert.Equal(expected, status);
    }
    
    /// <summary>
    /// Assert task count matches.
    /// </summary>
    public static async Task AssertTaskCountAsync(
        this MergeTestFixture fixture,
        int expectedTotal)
    {
        var count = await fixture.Queue.GetTotalCountAsync();
        Assert.Equal(expectedTotal, count);
    }
}
```

### Part 4: Test Scenarios

```csharp
// File: tests/Acode.Integration.Tests/Merge/Scenarios/CleanParallelMergeTests.cs
namespace Acode.Integration.Tests.Merge.Scenarios;

[Trait("Category", "MergeIntegration")]
public class CleanParallelMergeTests
{
    [Fact]
    public async Task TwoTasks_DifferentFiles_BothMerge()
    {
        // Arrange
        await using var fixture = await MergeTestFixture.CreateAsync();
        
        var task1 = await fixture.CreateTaskWithFileChangeAsync(
            "task-1",
            "src/FileA.cs",
            "// Content from task 1\npublic class FileA { }");
        
        var task2 = await fixture.CreateTaskWithFileChangeAsync(
            "task-2",
            "src/FileB.cs",
            "// Content from task 2\npublic class FileB { }");
        
        // Act
        await fixture.RunFullWorkflowAsync(task1, task2);
        
        // Assert
        await fixture.AssertMergedAsync(task1, task2);
        fixture.AssertFileExists("src/FileA.cs");
        fixture.AssertFileExists("src/FileB.cs");
        await fixture.AssertQueueEmptyAsync();
        await fixture.AssertCommitExistsAsync("task-1");
        await fixture.AssertCommitExistsAsync("task-2");
    }
    
    [Fact]
    public async Task ThreeTasks_IndependentFiles_AllMerge()
    {
        await using var fixture = await MergeTestFixture.CreateAsync();
        
        var tasks = await Task.WhenAll(
            fixture.CreateTaskWithFileChangeAsync("t1", "a.txt", "A"),
            fixture.CreateTaskWithFileChangeAsync("t2", "b.txt", "B"),
            fixture.CreateTaskWithFileChangeAsync("t3", "c.txt", "C"));
        
        await fixture.RunFullWorkflowAsync(tasks);
        
        await fixture.AssertMergedAsync(tasks);
        await fixture.AssertFileContentAsync("a.txt", "A");
        await fixture.AssertFileContentAsync("b.txt", "B");
        await fixture.AssertFileContentAsync("c.txt", "C");
    }
}

// File: tests/Acode.Integration.Tests/Merge/Scenarios/SameFileDifferentLinesTests.cs
namespace Acode.Integration.Tests.Merge.Scenarios;

[Trait("Category", "MergeIntegration")]
public class SameFileDifferentLinesTests
{
    [Fact]
    public async Task SameFile_DifferentLines_MergesCleanly()
    {
        await using var fixture = await MergeTestFixture.CreateAsync();
        
        // Create initial file with multiple sections
        var git = new TestGitRepository(fixture.RepoPath);
        await git.WriteFileAsync("shared.cs",
            "// Section 1\n" +
            "public class A { }\n" +
            "\n" +
            "// Section 2\n" +
            "public class B { }\n" +
            "\n" +
            "// Section 3\n" +
            "public class C { }\n");
        await git.AddAsync();
        await git.CommitAsync("Add shared.cs");
        
        // Task 1 modifies section 1
        var task1 = await fixture.CreateTaskWithFileChangeAsync(
            "task-1",
            "shared.cs",
            "// Section 1 - Modified by task 1\n" +
            "public class A { public void MethodA() { } }\n" +
            "\n" +
            "// Section 2\n" +
            "public class B { }\n" +
            "\n" +
            "// Section 3\n" +
            "public class C { }\n");
        
        // Task 2 modifies section 3
        var task2 = await fixture.CreateTaskWithFileChangeAsync(
            "task-2",
            "shared.cs",
            "// Section 1\n" +
            "public class A { }\n" +
            "\n" +
            "// Section 2\n" +
            "public class B { }\n" +
            "\n" +
            "// Section 3 - Modified by task 2\n" +
            "public class C { public void MethodC() { } }\n");
        
        await fixture.RunFullWorkflowAsync(task1, task2);
        
        await fixture.AssertMergedAsync(task1, task2);
        
        var content = await fixture.ReadFileAsync("shared.cs");
        Assert.Contains("Modified by task 1", content);
        Assert.Contains("Modified by task 2", content);
    }
}

// File: tests/Acode.Integration.Tests/Merge/Scenarios/SameFileSameLinesTests.cs
namespace Acode.Integration.Tests.Merge.Scenarios;

[Trait("Category", "MergeIntegration")]
public class SameFileSameLinesTests
{
    [Fact]
    public async Task SameFile_SameLines_DetectsConflict()
    {
        await using var fixture = await MergeTestFixture.CreateAsync();
        
        // Create initial file
        var git = new TestGitRepository(fixture.RepoPath);
        await git.WriteFileAsync("conflict.cs",
            "public class Conflict\n" +
            "{\n" +
            "    public string Value { get; set; }\n" +
            "}\n");
        await git.AddAsync();
        await git.CommitAsync("Add conflict.cs");
        
        // Both tasks modify the same line
        var task1 = await fixture.CreateTaskWithFileChangeAsync(
            "task-1",
            "conflict.cs",
            "public class Conflict\n" +
            "{\n" +
            "    public string Value { get; set; } = \"Task1\";\n" +
            "}\n");
        
        var task2 = await fixture.CreateTaskWithFileChangeAsync(
            "task-2",
            "conflict.cs",
            "public class Conflict\n" +
            "{\n" +
            "    public string Value { get; set; } = \"Task2\";\n" +
            "}\n");
        
        await fixture.EnqueueAsync(task1, task2);
        await fixture.ExecuteAllAsync();
        
        // Merge should detect conflict
        await Assert.ThrowsAsync<MergeConflictException>(
            () => fixture.MergeAllAsync());
    }
}

// File: tests/Acode.Integration.Tests/Merge/Scenarios/DependentTasksTests.cs
namespace Acode.Integration.Tests.Merge.Scenarios;

[Trait("Category", "MergeIntegration")]
public class DependentTasksTests
{
    [Fact]
    public async Task DependentTasks_ExecuteInOrder()
    {
        await using var fixture = await MergeTestFixture.CreateAsync();
        
        var task1 = fixture.CreateTask("task-1", files: ["src/file.cs"]);
        var task2 = fixture.CreateTask("task-2", 
            files: ["src/file.cs"],
            dependencies: ["task-1"]);
        
        await fixture.EnqueueAsync(task1, task2);
        
        // Verify dependency is tracked
        var deps = fixture.Graph.GetDependencies("task-2");
        Assert.Contains("task-1", deps);
        
        // Task 2 should not be ready until task 1 completes
        Assert.False(fixture.Graph.IsReady("task-2"));
        Assert.True(fixture.Graph.IsReady("task-1"));
        
        await fixture.ExecuteAllAsync();
        await fixture.MergeAllAsync();
        
        await fixture.AssertMergedAsync(task1, task2);
    }
}

// File: tests/Acode.Integration.Tests/Merge/Scenarios/CycleDetectionTests.cs
namespace Acode.Integration.Tests.Merge.Scenarios;

[Trait("Category", "MergeIntegration")]
public class CycleDetectionTests
{
    [Fact]
    public async Task CircularDependency_IsRejected()
    {
        await using var fixture = await MergeTestFixture.CreateAsync();
        
        var task1 = fixture.CreateTask("task-1", 
            files: ["a.cs"],
            dependencies: ["task-2"]);
        
        var task2 = fixture.CreateTask("task-2",
            files: ["b.cs"],
            dependencies: ["task-1"]);
        
        // Adding circular dependency should throw
        await fixture.EnqueueAsync(task1);
        
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.EnqueueAsync(task2));
    }
}

// File: tests/Acode.Integration.Tests/Merge/Scenarios/LargeBatchTests.cs
namespace Acode.Integration.Tests.Merge.Scenarios;

[Trait("Category", "MergeIntegration")]
public class LargeBatchTests
{
    [Fact]
    public async Task TenTasks_FiveWorkers_AllMerge()
    {
        await using var fixture = await MergeTestFixture.CreateAsync(
            MergeTestOptions.LargeBatch);
        
        var tasks = new List<TaskSpec>();
        for (int i = 0; i < 10; i++)
        {
            var task = await fixture.CreateTaskWithFileChangeAsync(
                $"task-{i:D2}",
                $"files/file{i:D2}.cs",
                $"// Content from task {i}");
            tasks.Add(task);
        }
        
        await fixture.RunFullWorkflowAsync(tasks.ToArray());
        
        await fixture.AssertMergedAsync(tasks.ToArray());
        
        for (int i = 0; i < 10; i++)
        {
            fixture.AssertFileExists($"files/file{i:D2}.cs");
        }
    }
}
```

### Implementation Checklist

- [ ] Create `MergeTestFixture` with isolated repo creation
- [ ] Create `MergeTestOptions` with configurable workers/timeouts
- [ ] Create `TestGitRepository` helper with all git operations
- [ ] Create `TestTaskFactory` for common task patterns
- [ ] Create assertion extensions in `MergeAssertions`
- [ ] Create assertion extensions in `GitAssertions`
- [ ] Create assertion extensions in `QueueAssertions`
- [ ] Implement `CleanParallelMergeTests` scenarios
- [ ] Implement `SameFileDifferentLinesTests` scenarios
- [ ] Implement `SameFileSameLinesTests` with conflict detection
- [ ] Implement `DependentTasksTests` with ordering verification
- [ ] Implement `CycleDetectionTests`
- [ ] Implement `LargeBatchTests` with 10+ tasks
- [ ] Implement `RollbackOnFailureTests`
- [ ] Implement `TimeoutHandlingTests`
- [ ] Implement `WorkerCrashTests`
- [ ] Add golden file comparisons
- [ ] Ensure deterministic test execution
- [ ] Add CI pipeline configuration
- [ ] Verify no flaky tests

### Rollout Plan

1. **Day 1**: MergeTestFixture and TestGitRepository
2. **Day 2**: Assertion helpers
3. **Day 3**: Clean parallel merge scenarios
4. **Day 4**: Conflict detection scenarios
5. **Day 5**: Dependency and cycle scenarios
6. **Day 6**: Large batch and performance tests
7. **Day 7**: Edge case scenarios (rollback, timeout, crash)
8. **Day 8**: CI integration and flakiness testing

---

**End of Task 028.c Specification**