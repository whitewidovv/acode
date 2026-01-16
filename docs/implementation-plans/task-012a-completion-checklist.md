# Task-012a Completion Checklist: Planner Stage Implementation

**Status:** Ready for implementation following strict TDD

**Total Phases:** 7

**Estimated Effort:** 12-16 hours total (can run some phases in parallel)

**Methodology:** RED → GREEN → REFACTOR with verification at each step

---

## CRITICAL NOTES FOR IMPLEMENTING AGENT

1. **Read the Entire Checklist First** - Don't skip sections, every phase builds on previous work
2. **Follow TDD Strictly** - Write failing tests FIRST, then implement to make them pass
3. **Reference Spec Directly** - Line numbers point to exact location in task-012a spec
4. **Verify After Each Phase** - Check for NotImplementedException, verify all tests pass
5. **Commit After Each Phase** - One logical unit per commit with meaningful message
6. **Update Progress** - Mark items [🔄] when starting, [✅] when complete

---

## PHASE 1: DOMAIN MODELS - ActionType & AcceptanceCriteria (3-4 hours)

### Gap 1.1: ActionType Enum

**Current State:** ❌ MISSING

**Spec Reference:** Lines 2102-2111 (Implementation Prompt section)

**What Exists:** Nothing

**What's Missing:** Enum with 7 action types

**Implementation Details from Spec:**

```csharp
namespace Acode.Domain.Planning;

public enum ActionType
{
    ReadFile,
    WriteFile,
    ModifyFile,
    CreateDirectory,
    RunCommand,
    AnalyzeCode,
    GenerateCode
}
```

**Acceptance Criteria Covered:** AC-027 (step has action type)

**Test Requirements:**
- Can serialize/deserialize ActionType
- All 7 values are valid
- Invalid values rejected

**Success Criteria:**
- [ ] File exists: `src/Acode.Domain/Planning/ActionType.cs`
- [ ] Enum has all 7 values from spec
- [ ] No NotImplementedException
- [ ] Can be serialized to/from JSON

**Gap Checklist Item:** [ ] 🔄 ActionType enum created and tested

---

### Gap 1.2: AcceptanceCriterion Entity

**Current State:** ❌ MISSING

**Spec Reference:** Lines 2087-2088 in domain entities section shows it's used by PlannedTask

**What Exists:** Nothing

**What's Missing:** Entity class for acceptance criteria

**Implementation Details from Spec (inferred from usage in PlannedTask):**

```csharp
namespace Acode.Domain.Planning;

public sealed class AcceptanceCriterion
{
    public Guid Id { get; }
    public string Description { get; }
    public bool IsMet { get; private set; }

    public AcceptanceCriterion(Guid id, string description, bool isMet = false)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        IsMet = isMet;
    }

    public void MarkMet()
    {
        IsMet = true;
    }

    public override bool Equals(object? obj) => obj is AcceptanceCriterion other && other.Id == Id;
    public override int GetHashCode() => Id.GetHashCode();
}
```

**Acceptance Criteria Covered:** AC-025 (task has criteria), AC-034 (plan lists tasks)

**Test Requirements:**
- Create with valid description
- Mark as met
- Equality based on ID
- Cannot create with null description

**Success Criteria:**
- [ ] File exists: `src/Acode.Domain/Planning/AcceptanceCriterion.cs`
- [ ] Constructor with Guid, string, bool
- [ ] MarkMet() method works
- [ ] Equality implemented correctly
- [ ] Tests verify all behavior

**Gap Checklist Item:** [ ] 🔄 AcceptanceCriterion entity created and tested

---

### Gap 1.3: PlannedStep Entity

**Current State:** ❌ MISSING

**Spec Reference:** Lines 2091-2100 (Implementation Prompt section shows complete definition)

**What Exists:** Nothing

**What's Missing:** Step entity with action type and verification

**Implementation Details from Spec:**

```csharp
namespace Acode.Domain.Planning;

public sealed class PlannedStep
{
    public StepId Id { get; }
    public string Title { get; }
    public string Description { get; }
    public ActionType Action { get; }
    public string? ExpectedOutput { get; }
    public VerificationCriteria Verification { get; }
    public StepStatus Status { get; private set; }

    public PlannedStep(
        StepId id,
        string title,
        string description,
        ActionType action,
        string? expectedOutput,
        VerificationCriteria verification,
        StepStatus status = StepStatus.Pending)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Action = action;
        ExpectedOutput = expectedOutput;
        Verification = verification ?? throw new ArgumentNullException(nameof(verification));
        Status = status;
    }

    public void Complete()
    {
        Status = StepStatus.Completed;
    }
}

public enum StepStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Skipped
}

public record VerificationCriteria(string? Command = null, string? Pattern = null, bool Strict = false);
```

Note: StepId and related value objects should exist from prior tasks. If not, create minimal versions.

**Acceptance Criteria Covered:** AC-026-029 (step definition)

**Test Requirements:**
- Create step with all properties
- Status starts as Pending
- Can complete step
- Cannot create with null required fields

**Success Criteria:**
- [ ] File exists: `src/Acode.Domain/Planning/PlannedStep.cs`
- [ ] Constructor takes 7 parameters
- [ ] Complete() method works
- [ ] StepStatus enum defined
- [ ] VerificationCriteria record defined
- [ ] All null checks in place

**Gap Checklist Item:** [ ] 🔄 PlannedStep entity and related types created

---

### Gap 1.4: PlannedTask Entity

**Current State:** ❌ MISSING

**Spec Reference:** Lines 2079-2089 (Implementation Prompt section)

**What Exists:** Nothing

**What's Missing:** Task entity with steps, resources, and complexity

**Implementation Details from Spec:**

```csharp
namespace Acode.Domain.Planning;

public sealed class PlannedTask
{
    public TaskId Id { get; }
    public string Title { get; }
    public string Description { get; }
    public int Complexity { get; private set; }
    public IReadOnlyList<PlannedStep> Steps { get; private set; }
    public ResourceRequirements Resources { get; }
    public IReadOnlyList<AcceptanceCriterion> AcceptanceCriteria { get; private set; }
    public TaskStatus Status { get; private set; }

    public PlannedTask(
        TaskId id,
        string title,
        string description,
        int complexity,
        List<PlannedStep> steps,
        ResourceRequirements resources,
        List<AcceptanceCriterion> acceptanceCriteria,
        TaskStatus status = TaskStatus.Pending)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Complexity = complexity;
        Steps = steps?.AsReadOnly() ?? throw new ArgumentNullException(nameof(steps));
        Resources = resources ?? throw new ArgumentNullException(nameof(resources));
        AcceptanceCriteria = acceptanceCriteria?.AsReadOnly() ?? throw new ArgumentNullException(nameof(acceptanceCriteria));
        Status = status;
    }

    public void SetComplexity(int value)
    {
        if (value < 0) throw new ArgumentException("Complexity must be >= 0");
        Complexity = value;
    }

    public void Complete()
    {
        Status = TaskStatus.Completed;
    }
}

public enum TaskStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Skipped
}

public record ResourceRequirements(
    int? EstimatedHours = null,
    List<string>? RequiredTools = null,
    List<string>? RequiredDependencies = null);
```

**Acceptance Criteria Covered:** AC-017, AC-021-025, AC-034-039

**Test Requirements:**
- Create task with all properties
- Steps collection is read-only from outside
- Can set complexity
- Can complete task
- Cannot create with null title/description

**Success Criteria:**
- [ ] File exists: `src/Acode.Domain/Planning/PlannedTask.cs`
- [ ] Constructor with 8 parameters
- [ ] SetComplexity() validation works
- [ ] Complete() method works
- [ ] Collections are IReadOnlyList
- [ ] TaskStatus enum defined
- [ ] ResourceRequirements record defined

**Gap Checklist Item:** [ ] 🔄 PlannedTask entity created with validation

---

### Gap 1.5: TaskPlan Root Aggregate

**Current State:** ❌ MISSING

**Spec Reference:** Lines 2064-2077 (Implementation Prompt section)

**What Exists:** Nothing

**What's Missing:** Root aggregate with tasks, dependencies, versioning

**Implementation Details from Spec:**

```csharp
namespace Acode.Domain.Planning;

public sealed class TaskPlan
{
    public PlanId Id { get; }
    public int Version { get; private set; }
    public SessionId SessionId { get; }
    public string Goal { get; }
    public IReadOnlyList<PlannedTask> Tasks { get; private set; }
    public DependencyGraph Dependencies { get; }
    public int TotalComplexity { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public TaskPlan(
        PlanId id,
        int version,
        SessionId sessionId,
        string goal,
        IEnumerable<PlannedTask> tasks,
        DependencyGraph dependencies,
        int totalComplexity,
        DateTimeOffset createdAt)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Version = version;
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        Goal = goal ?? throw new ArgumentNullException(nameof(goal));
        Tasks = tasks?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(tasks));
        Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        TotalComplexity = totalComplexity;
        CreatedAt = createdAt;
    }

    public TaskPlan IncrementVersion()
    {
        return new TaskPlan(
            Id,
            Version + 1,
            SessionId,
            Goal,
            Tasks,
            Dependencies,
            TotalComplexity,
            CreatedAt);
    }

    public TaskPlan WithTasks(IEnumerable<PlannedTask> tasks)
    {
        var taskList = tasks?.ToList() ?? throw new ArgumentNullException(nameof(tasks));
        var newComplexity = taskList.Sum(t => t.Complexity);

        return new TaskPlan(
            Id,
            Version,
            SessionId,
            Goal,
            taskList,
            Dependencies,
            newComplexity,
            CreatedAt);
    }
}

public sealed record PlanId(Guid Value)
{
    public static PlanId NewId() => new(Guid.NewGuid());
}
```

**Acceptance Criteria Covered:** AC-034-039, AC-040-043

**Test Requirements:**
- Create plan with all properties
- IncrementVersion() creates new instance
- WithTasks() updates collection
- Total complexity calculated correctly
- SessionId required

**Success Criteria:**
- [ ] File exists: `src/Acode.Domain/Planning/TaskPlan.cs`
- [ ] Constructor with 8 parameters
- [ ] IncrementVersion() method works correctly
- [ ] WithTasks() updates complexity
- [ ] PlanId record defined with NewId()
- [ ] Tasks collection is IReadOnlyList
- [ ] Immutable (all changes return new instance)

**Gap Checklist Item:** [ ] 🔄 TaskPlan aggregate created with versioning

---

### Gap 1.6: DependencyGraph Service

**Current State:** ❌ MISSING

**Spec Reference:** Lines 1786-1862 (Testing Requirements section shows usage and expected behavior)

**What Exists:** Nothing

**What's Missing:** Graph structure for task dependencies, cycle detection, topological sort

**Implementation Details from Spec (inferred from tests):**

```csharp
namespace Acode.Domain.Planning;

public sealed class DependencyGraph
{
    private readonly Dictionary<TaskId, HashSet<TaskId>> _dependencies = new();

    public void AddDependency(TaskId dependent, TaskId dependency)
    {
        if (dependent == null) throw new ArgumentNullException(nameof(dependent));
        if (dependency == null) throw new ArgumentNullException(nameof(dependency));

        if (!_dependencies.ContainsKey(dependent))
            _dependencies[dependent] = new HashSet<TaskId>();

        _dependencies[dependent].Add(dependency);

        // Check for cycles before returning
        if (WouldCreateCycle(dependent, dependency))
        {
            _dependencies[dependent].Remove(dependency);
            throw new CircularDependencyException($"Adding dependency would create cycle: {dependent.Value} -> {dependency.Value}");
        }
    }

    public bool DependsOn(TaskId dependent, TaskId dependency)
    {
        if (!_dependencies.ContainsKey(dependent))
            return false;

        return _dependencies[dependent].Contains(dependency) ||
               _dependencies[dependent].Any(d => DependsOn(d, dependency)); // transitive
    }

    public bool HasCycles()
    {
        foreach (var node in _dependencies.Keys)
        {
            if (HasCycleDFS(node, new HashSet<TaskId>(), new HashSet<TaskId>()))
                return true;
        }
        return false;
    }

    public IReadOnlyList<PlannedTask> TopologicalSort(PlannedTask[] tasks)
    {
        var sorted = new List<PlannedTask>();
        var visited = new HashSet<TaskId>();
        var visiting = new HashSet<TaskId>();

        foreach (var task in tasks)
        {
            if (!visited.Contains(task.Id))
            {
                TopoSortDFS(task, tasks.ToDictionary(t => t.Id), visited, visiting, sorted);
            }
        }

        return sorted.AsReadOnly();
    }

    private bool WouldCreateCycle(TaskId dependent, TaskId dependency)
    {
        return DependsOn(dependency, dependent);
    }

    private bool HasCycleDFS(TaskId node, HashSet<TaskId> visited, HashSet<TaskId> recursionStack)
    {
        visited.Add(node);
        recursionStack.Add(node);

        if (_dependencies.ContainsKey(node))
        {
            foreach (var neighbor in _dependencies[node])
            {
                if (!visited.Contains(neighbor))
                {
                    if (HasCycleDFS(neighbor, visited, recursionStack))
                        return true;
                }
                else if (recursionStack.Contains(neighbor))
                {
                    return true;
                }
            }
        }

        recursionStack.Remove(node);
        return false;
    }

    private void TopoSortDFS(
        PlannedTask task,
        Dictionary<TaskId, PlannedTask> taskMap,
        HashSet<TaskId> visited,
        HashSet<TaskId> visiting,
        List<PlannedTask> result)
    {
        if (visited.Contains(task.Id))
            return;

        visiting.Add(task.Id);

        if (_dependencies.ContainsKey(task.Id))
        {
            foreach (var depId in _dependencies[task.Id])
            {
                if (taskMap.TryGetValue(depId, out var depTask))
                {
                    TopoSortDFS(depTask, taskMap, visited, visiting, result);
                }
            }
        }

        visiting.Remove(task.Id);
        visited.Add(task.Id);
        result.Add(task);
    }
}

public sealed class CircularDependencyException : Exception
{
    public CircularDependencyException(string message) : base(message) { }
}
```

**Acceptance Criteria Covered:** AC-030-033

**Test Requirements:**
- AddDependency works
- DependsOn checks transitive dependencies
- Rejects circular dependencies
- TopologicalSort returns correct order
- HasCycles detects cycles

**Success Criteria:**
- [ ] File exists: `src/Acode.Domain/Planning/DependencyGraph.cs`
- [ ] AddDependency with cycle detection
- [ ] DependsOn with transitive checking
- [ ] TopologicalSort implementation
- [ ] HasCycles detection
- [ ] CircularDependencyException defined
- [ ] All 3 DependencyGraphTests passing

**Gap Checklist Item:** [ ] 🔄 DependencyGraph created with cycle detection

---

### Gap 1.7: DependencyGraphTests Unit Tests

**Current State:** ❌ MISSING

**Spec Reference:** Lines 1786-1862 (complete test code in Testing Requirements)

**What Exists:** Nothing

**What's Missing:** 3 test methods for DependencyGraph

**Test Code from Spec:**

```csharp
namespace Acode.Domain.Tests.Planning;

public class DependencyGraphTests
{
    [Fact]
    public void Should_Create_Valid_Dependency_Graph()
    {
        // Arrange
        var taskA = CreateTask("A");
        var taskB = CreateTask("B");
        var taskC = CreateTask("C");

        var graph = new DependencyGraph();

        // Act
        graph.AddDependency(taskB.Id, taskA.Id); // B depends on A
        graph.AddDependency(taskC.Id, taskB.Id); // C depends on B

        // Assert
        Assert.True(graph.DependsOn(taskB.Id, taskA.Id));
        Assert.True(graph.DependsOn(taskC.Id, taskB.Id));
        Assert.False(graph.DependsOn(taskA.Id, taskB.Id)); // A does not depend on B
    }

    [Fact]
    public void Should_Reject_Circular_Dependencies()
    {
        // Arrange
        var taskA = CreateTask("A");
        var taskB = CreateTask("B");
        var taskC = CreateTask("C");

        var graph = new DependencyGraph();
        graph.AddDependency(taskB.Id, taskA.Id); // B depends on A
        graph.AddDependency(taskC.Id, taskB.Id); // C depends on B

        // Act & Assert
        Assert.Throws<CircularDependencyException>(() =>
            graph.AddDependency(taskA.Id, taskC.Id)); // A depends on C would create cycle A -> C -> B -> A
    }

    [Fact]
    public void Should_Topologically_Sort_Tasks()
    {
        // Arrange
        var taskA = CreateTask("A");
        var taskB = CreateTask("B");
        var taskC = CreateTask("C");
        var taskD = CreateTask("D");

        var graph = new DependencyGraph();
        graph.AddDependency(taskB.Id, taskA.Id); // B depends on A
        graph.AddDependency(taskC.Id, taskA.Id); // C depends on A
        graph.AddDependency(taskD.Id, taskB.Id); // D depends on B
        graph.AddDependency(taskD.Id, taskC.Id); // D depends on C

        // Act
        var sorted = graph.TopologicalSort(new[] { taskA, taskB, taskC, taskD });

        // Assert
        Assert.Equal(4, sorted.Count);
        Assert.Equal(taskA.Id, sorted[0].Id); // A first
        // B and C can be in any order (both depend only on A)
        Assert.Equal(taskD.Id, sorted[3].Id); // D last (depends on B and C)
    }

    private static PlannedTask CreateTask(string title)
    {
        return new PlannedTask(
            Id: TaskId.NewId(),
            Title: title,
            Description: $"Task {title}",
            Complexity: 1,
            Steps: new List<PlannedStep>(),
            Resources: new ResourceRequirements(),
            AcceptanceCriteria: new List<AcceptanceCriterion>(),
            Status: TaskStatus.Pending);
    }
}
```

**Acceptance Criteria Covered:** AC-030-033

**Test File Location:** `tests/Acode.Domain.Tests/Planning/DependencyGraphTests.cs`

**Success Criteria:**
- [ ] File exists with 3 [Fact] methods
- [ ] Should_Create_Valid_Dependency_Graph passes
- [ ] Should_Reject_Circular_Dependencies passes
- [ ] Should_Topologically_Sort_Tasks passes
- [ ] No NotImplementedException
- [ ] Helper method CreateTask() works

**Gap Checklist Item:** [ ] 🔄 DependencyGraphTests created and passing

---

## PHASE 1 VERIFICATION CHECKLIST

Before moving to Phase 2, verify:

- [ ] All 6 domain model files created (ActionType, AcceptanceCriterion, PlannedStep, PlannedTask, TaskPlan, DependencyGraph)
- [ ] All 7 domain model files have NO NotImplementedException
- [ ] DependencyGraphTests file exists with 3 test methods
- [ ] All 3 DependencyGraph tests passing: `dotnet test --filter "DependencyGraphTests"`
- [ ] Build clean: `dotnet build` → 0 errors, 0 warnings
- [ ] No null reference issues
- [ ] All value objects and entities created correctly

**Phase 1 Status:** [ ] 🔄 COMPLETE

---

## PHASE 2: INTERFACES & PlannerStage BASE (2-3 hours)

### Gap 2.1: IPlannerStage Interface

**Current State:** ❌ MISSING

**Spec Reference:** Lines 2050-2056 (Implementation Prompt)

**What Exists:** Nothing

**What's Missing:** Interface extending IStage with planning methods

**Implementation Details from Spec:**

```csharp
namespace Acode.Application.Orchestration.Stages.Planner;

public interface IPlannerStage : IStage
{
    Task<TaskPlan> CreatePlanAsync(PlanningContext context, CancellationToken ct);
    Task<TaskPlan> ReplanAsync(TaskPlan existing, ReplanReason reason, CancellationToken ct);
}

public record PlanningContext(
    Session Session,
    ConversationContext Conversation,
    Workspace Workspace,
    TaskPlan? ExistingPlan = null,
    ReplanReason? ReplanReason = null);

public enum ReplanReason
{
    ReviewRejected,
    ExecutionFailed,
    UserRequest,
    PlanOutdated
}
```

Note: Assumes IStage, StageType, StageBase exist from prior tasks

**Acceptance Criteria Covered:** AC-001, AC-040-043

**Success Criteria:**
- [ ] File exists: `src/Acode.Application/Orchestration/Stages/Planner/IPlannerStage.cs`
- [ ] Extends IStage
- [ ] Has CreatePlanAsync method
- [ ] Has ReplanAsync method
- [ ] PlanningContext record defined
- [ ] ReplanReason enum defined

**Gap Checklist Item:** [ ] 🔄 IPlannerStage interface created

---

### Gap 2.2: Supporting Interfaces

**Current State:** ❌ MISSING

**Spec Reference:** Lines 2242-2248, 2345-2349, 2498-2501, 2658-2661 (Implementation Prompt)

**What Exists:** Nothing

**What's Missing:** IContextPreparator, IRequestAnalyzer, ITaskDecomposer, IPlanBuilder interfaces

**Implementation Details from Spec:**

```csharp
namespace Acode.Application.Orchestration.Stages.Planner;

public interface IContextPreparator
{
    Task PrepareAsync(StageContext context, CancellationToken ct);
    Task<PlanningContext> PrepareForReplanAsync(TaskPlan existing, ReplanReason reason, CancellationToken ct);
}

public interface IRequestAnalyzer
{
    Task<RequestAnalysis> AnalyzeAsync(UserRequest request, StageContext context, CancellationToken ct);
    Task<RequestAnalysis> AnalyzeAsync(string goal, PlanningContext context, CancellationToken ct);
}

public interface ITaskDecomposer
{
    Task<List<PlannedTask>> DecomposeAsync(RequestAnalysis analysis, StageContext context, CancellationToken ct);
}

public interface IPlanBuilder
{
    TaskPlan Build(SessionId sessionId, string goal, List<PlannedTask> tasks, int version = 1);
}

public interface IComplexityEstimator
{
    Task<int> EstimateAsync(PlannedTask task, CancellationToken ct);
}

public record RequestAnalysis(
    string Intent,
    List<string> Requirements,
    bool IsAmbiguous,
    List<string> Questions,
    string? SuggestedApproach,
    int TokensUsed);
```

**Success Criteria:**
- [ ] All 5 interface files created
- [ ] RequestAnalysis record defined
- [ ] All methods match spec signatures
- [ ] No implementation code (interfaces only)

**Gap Checklist Item:** [ ] 🔄 All supporting interfaces created

---

### Gap 2.3: PlannerStage Base Implementation

**Current State:** ❌ MISSING

**Spec Reference:** Lines 2117-2236 (Implementation Prompt shows complete PlannerStage code)

**What Exists:** Nothing

**What's Missing:** Main PlannerStage class with lifecycle methods

**Implementation Details from Spec (simplified - see full spec for complete code):**

```csharp
namespace Acode.Application.Orchestration.Stages.Planner;

public sealed class PlannerStage : StageBase, IPlannerStage
{
    private readonly IContextPreparator _contextPreparator;
    private readonly IRequestAnalyzer _requestAnalyzer;
    private readonly ITaskDecomposer _decomposer;
    private readonly IPlanBuilder _builder;
    private readonly ILlmService _llm;
    private readonly ILogger<PlannerStage> _logger;

    public override StageType Type => StageType.Planner;

    public PlannerStage(
        IContextPreparator contextPreparator,
        IRequestAnalyzer requestAnalyzer,
        ITaskDecomposer decomposer,
        IPlanBuilder builder,
        ILlmService llm,
        ILogger<PlannerStage> logger) : base(logger)
    {
        _contextPreparator = contextPreparator ?? throw new ArgumentNullException(nameof(contextPreparator));
        _requestAnalyzer = requestAnalyzer ?? throw new ArgumentNullException(nameof(requestAnalyzer));
        _decomposer = decomposer ?? throw new ArgumentNullException(nameof(decomposer));
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task OnEnterAsync(StageContext context, CancellationToken ct)
    {
        _logger.LogInformation("Planner stage entered for session {SessionId}", context.Session.Id);
        await _contextPreparator.PrepareAsync(context, ct);
    }

    protected override async Task<StageResult> ExecuteStageAsync(
        StageContext context,
        CancellationToken ct)
    {
        var request = context.Session.CurrentRequest;
        _logger.LogInformation("Analyzing request: {Goal}", request.Goal);

        var analysis = await _requestAnalyzer.AnalyzeAsync(request, context, ct);

        if (analysis.NeedsClarification)
        {
            _logger.LogInformation("Request needs clarification");
            return new StageResult(
                Status: StageStatus.Retry,
                Output: analysis,
                NextStage: StageType.Planner,
                Message: "Clarification needed",
                Metrics: new StageMetrics(StageType.Planner, TimeSpan.Zero, analysis.TokensUsed));
        }

        var tasks = await _decomposer.DecomposeAsync(analysis, context, ct);
        var plan = _builder.Build(context.Session.Id, request.Goal, tasks);

        _logger.LogInformation("Plan created: {PlanId}, Tasks: {TaskCount}",
            plan.Id, plan.Tasks.Count);

        return new StageResult(
            Status: StageStatus.Success,
            Output: plan,
            NextStage: StageType.Executor,
            Message: $"Plan created with {plan.Tasks.Count} tasks",
            Metrics: new StageMetrics(StageType.Planner, TimeSpan.Zero, analysis.TokensUsed));
    }

    public async Task<TaskPlan> CreatePlanAsync(PlanningContext context, CancellationToken ct)
    {
        var stageContext = new StageContext(
            Session: context.Session,
            CurrentTask: context.Session.CurrentTask,
            Conversation: context.Conversation,
            Budget: TokenBudget.Default(StageType.Planner),
            StageData: new Dictionary<string, object>());

        var result = await ExecuteStageAsync(stageContext, ct);

        if (result.Status != StageStatus.Success)
            throw new PlanningException($"Planning failed: {result.Message}");

        return (TaskPlan)result.Output!;
    }

    public async Task<TaskPlan> ReplanAsync(TaskPlan existing, ReplanReason reason, CancellationToken ct)
    {
        _logger.LogInformation("Re-planning: {Reason}", reason);

        var newVersion = existing.IncrementVersion();
        var tasksToReplan = existing.Tasks.Where(t => t.Status != TaskStatus.Completed).ToList();

        _logger.LogInformation("Re-planning {Count} tasks", tasksToReplan.Count);

        return newVersion;
    }
}

public sealed class PlanningException : Exception
{
    public PlanningException(string message) : base(message) { }
}
```

**Note:** This is simplified. Refer to spec lines 2117-2236 for complete implementation with full OnEnterAsync, ExecuteStageAsync, CreatePlanAsync, and ReplanAsync methods.

**Acceptance Criteria Covered:** AC-001-005, AC-040-043

**Success Criteria:**
- [ ] File exists: `src/Acode.Application/Orchestration/Stages/Planner/PlannerStage.cs`
- [ ] Extends StageBase and implements IPlannerStage
- [ ] Constructor with 6 dependencies
- [ ] OnEnterAsync calls ContextPreparator
- [ ] ExecuteStageAsync implements full flow
- [ ] CreatePlanAsync works
- [ ] ReplanAsync implemented
- [ ] PlanningException defined

**Gap Checklist Item:** [ ] 🔄 PlannerStage base implementation created

---

## PHASE 2 VERIFICATION CHECKLIST

Before moving to Phase 3:

- [ ] IPlannerStage interface created
- [ ] All 4 supporting interfaces created (IContextPreparator, IRequestAnalyzer, ITaskDecomposer, IPlanBuilder)
- [ ] RequestAnalysis record defined
- [ ] PlanningContext and ReplanReason defined
- [ ] PlannerStage implementation created
- [ ] No NotImplementedException in any file
- [ ] No compile errors: `dotnet build` → 0 errors

**Phase 2 Status:** [ ] 🔄 COMPLETE

---

---

## PHASE 3: CONTEXT PREPARATION (2-3 hours)

### Gap 3.1: ContextPreparator Service Implementation

**Current State:** ❌ MISSING

**Spec Reference:** Lines 2242-2337 (Implementation Prompt section)

**What Exists:** Nothing (interfaces created in Phase 2)

**What's Missing:** Complete ContextPreparator implementation with conversation loading, workspace analysis, token management

**Implementation Details from Spec:**

```csharp
namespace Acode.Application.Orchestration.Stages.Planner;

public sealed class ContextPreparator : IContextPreparator
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IFileSearchService _fileSearch;
    private readonly IConversationRepository _conversationRepo;
    private readonly ITokenCounter _tokenCounter;
    private readonly ILogger<ContextPreparator> _logger;

    public ContextPreparator(
        IWorkspaceRepository workspaceRepo,
        IFileSearchService fileSearch,
        IConversationRepository conversationRepo,
        ITokenCounter tokenCounter,
        ILogger<ContextPreparator> logger)
    {
        _workspaceRepo = workspaceRepo ?? throw new ArgumentNullException(nameof(workspaceRepo));
        _fileSearch = fileSearch ?? throw new ArgumentNullException(nameof(fileSearch));
        _conversationRepo = conversationRepo ?? throw new ArgumentNullException(nameof(conversationRepo));
        _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PrepareAsync(StageContext context, CancellationToken ct)
    {
        _logger.LogInformation("Preparing context for planning");

        // Load full conversation history
        var conversation = await _conversationRepo.GetBySessionAsync(context.Session.Id, ct);
        var conversationTokens = _tokenCounter.Count(conversation);

        // Load workspace metadata
        var workspace = await _workspaceRepo.GetByIdAsync(context.Session.WorkspaceId, ct);
        var workspaceStructure = await _fileSearch.GetStructureAsync(workspace.RootPath, ct);
        var structureTokens = _tokenCounter.Count(workspaceStructure);

        // Check if context fits budget
        var totalTokens = conversationTokens + structureTokens;
        var budget = context.Budget.MaxTokens;

        if (totalTokens > budget)
        {
            _logger.LogWarning("Context exceeds budget ({Total} > {Budget}), summarizing",
                totalTokens, budget);

            var recentConversation = conversation.Messages.TakeLast(20).ToList();
            var summarizedStructure = SummarizeStructure(workspaceStructure, budget - conversationTokens);

            context.StageData["conversation"] = recentConversation;
            context.StageData["workspace"] = summarizedStructure;
        }
        else
        {
            context.StageData["conversation"] = conversation.Messages;
            context.StageData["workspace"] = workspaceStructure;
        }

        _logger.LogInformation("Context prepared: {ConvTokens} + {StructTokens} = {Total} tokens",
            conversationTokens, structureTokens, totalTokens);
    }

    public async Task<PlanningContext> PrepareForReplanAsync(
        TaskPlan existing,
        ReplanReason reason,
        CancellationToken ct)
    {
        var conversation = await _conversationRepo.GetBySessionAsync(existing.SessionId, ct);
        var workspace = await _workspaceRepo.GetBySessionIdAsync(existing.SessionId, ct);

        return new PlanningContext(
            Session: null, // TODO: Load from repository
            Conversation: conversation,
            Workspace: workspace,
            ExistingPlan: existing,
            ReplanReason: reason);
    }

    private WorkspaceStructure SummarizeStructure(WorkspaceStructure full, int targetTokens)
    {
        return new WorkspaceStructure(
            RootPath: full.RootPath,
            Directories: full.Directories.Take(50).ToList(),
            Files: full.Files.Take(100).ToList(),
            IsSummarized: true);
    }
}
```

**Acceptance Criteria Covered:** AC-006-011 (context preparation)

**Success Criteria:**
- [ ] File exists: `src/Acode.Application/Orchestration/Stages/Planner/ContextPreparator.cs`
- [ ] Constructor with 5 dependencies
- [ ] PrepareAsync loads conversation and workspace
- [ ] Token budget checking works
- [ ] Summarization triggers when needed
- [ ] PrepareForReplanAsync implemented
- [ ] SummarizeStructure method works

**Gap Checklist Item:** [ ] 🔄 ContextPreparator service implemented

---

## PHASE 4: REQUEST ANALYSIS (2-3 hours)

### Gap 4.1: RequestAnalyzer Service Implementation

**Current State:** ❌ MISSING

**Spec Reference:** Lines 2343-2491 (Implementation Prompt section)

**What Exists:** Nothing (interface created in Phase 2)

**What's Missing:** RequestAnalyzer with intent extraction, requirement parsing, ambiguity detection

**Implementation Details (simplified - see spec lines 2343-2491 for full code):**

```csharp
namespace Acode.Application.Orchestration.Stages.Planner;

public sealed class RequestAnalyzer : IRequestAnalyzer
{
    private readonly ILlmService _llm;
    private readonly IPromptTemplateService _promptTemplates;
    private readonly ILogger<RequestAnalyzer> _logger;

    public RequestAnalyzer(
        ILlmService llm,
        IPromptTemplateService promptTemplates,
        ILogger<RequestAnalyzer> logger)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _promptTemplates = promptTemplates ?? throw new ArgumentNullException(nameof(promptTemplates));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RequestAnalysis> AnalyzeAsync(
        UserRequest request,
        StageContext context,
        CancellationToken ct)
    {
        _logger.LogInformation("Analyzing request: {Goal}", request.Goal);

        var prompt = _promptTemplates.RenderTemplate("analyze-request", new
        {
            goal = request.Goal,
            conversation = context.Conversation.Messages,
            workspace = context.StageData.GetValueOrDefault("workspace")
        });

        var response = await _llm.CompleteAsync(prompt, new LlmOptions
        {
            Temperature = 0.3,
            MaxTokens = 1000,
            StopSequences = new[] { "END_ANALYSIS" }
        }, ct);

        var parsed = ParseAnalysisResponse(response.Text);

        _logger.LogInformation("Analysis: Intent={Intent}, Ambiguous={Ambiguous}, Questions={Count}",
            parsed.Intent, parsed.IsAmbiguous, parsed.Questions.Count);

        return new RequestAnalysis(
            Intent: parsed.Intent,
            Requirements: parsed.Requirements,
            IsAmbiguous: parsed.IsAmbiguous,
            Questions: parsed.Questions,
            SuggestedApproach: parsed.Approach,
            TokensUsed: response.TokensUsed);
    }

    public async Task<RequestAnalysis> AnalyzeAsync(
        string goal,
        PlanningContext context,
        CancellationToken ct)
    {
        var prompt = _promptTemplates.RenderTemplate("analyze-request-replan", new
        {
            goal,
            conversation = context.Conversation.Messages,
            existingPlan = context.ExistingPlan,
            replanReason = context.ReplanReason
        });

        var response = await _llm.CompleteAsync(prompt, new LlmOptions
        {
            Temperature = 0.3,
            MaxTokens = 1000
        }, ct);

        return ParseAnalysisResponse(response.Text);
    }

    private AnalysisParsed ParseAnalysisResponse(string responseText)
    {
        var lines = responseText.Split('\n');
        var intent = ExtractSection(lines, "INTENT:");
        var requirements = ExtractListSection(lines, "REQUIREMENTS:");
        var isAmbiguous = ExtractSection(lines, "AMBIGUOUS:").ToLower().Contains("yes");
        var questions = ExtractListSection(lines, "QUESTIONS:");
        var approach = ExtractSection(lines, "APPROACH:");

        return new AnalysisParsed(intent, requirements, isAmbiguous, questions, approach);
    }

    private string ExtractSection(string[] lines, string sectionHeader)
    {
        var line = lines.FirstOrDefault(l => l.StartsWith(sectionHeader));
        return line?.Substring(sectionHeader.Length).Trim() ?? string.Empty;
    }

    private List<string> ExtractListSection(string[] lines, string sectionHeader)
    {
        var items = new List<string>();
        var inSection = false;

        foreach (var line in lines)
        {
            if (line.StartsWith(sectionHeader))
            {
                inSection = true;
                continue;
            }

            if (inSection && line.StartsWith("- "))
            {
                items.Add(line.Substring(2).Trim());
            }
            else if (inSection && !string.IsNullOrWhiteSpace(line) && !line.StartsWith(" "))
            {
                break;
            }
        }

        return items;
    }

    private record AnalysisParsed(
        string Intent,
        List<string> Requirements,
        bool IsAmbiguous,
        List<string> Questions,
        string Approach);
}
```

**Acceptance Criteria Covered:** AC-012-016 (request analysis)

**Success Criteria:**
- [ ] File exists: `src/Acode.Application/Orchestration/Stages/Planner/RequestAnalyzer.cs`
- [ ] Calls LLM with proper prompt
- [ ] Parses response correctly
- [ ] Extracts intent, requirements, ambiguity, questions
- [ ] Returns RequestAnalysis record
- [ ] Handles replan context

**Gap Checklist Item:** [ ] 🔄 RequestAnalyzer service implemented

---

## PHASE 5: TASK DECOMPOSITION (2-3 hours)

### Gap 5.1: TaskDecomposer Service Implementation

**Current State:** ❌ MISSING

**Spec Reference:** Lines 2495-2651 (Implementation Prompt section)

**What Exists:** Nothing (interface created in Phase 2)

**What's Missing:** Task decomposition with LLM-driven parsing and complexity estimation

**Key Implementation from Spec (simplified - see lines 2495-2651 for complete code):**

```csharp
namespace Acode.Application.Orchestration.Stages.Planner;

public sealed class TaskDecomposer : ITaskDecomposer
{
    private readonly ILlmService _llm;
    private readonly IPromptTemplateService _promptTemplates;
    private readonly IComplexityEstimator _complexityEstimator;
    private readonly ILogger<TaskDecomposer> _logger;

    public TaskDecomposer(
        ILlmService llm,
        IPromptTemplateService promptTemplates,
        IComplexityEstimator complexityEstimator,
        ILogger<TaskDecomposer> logger)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _promptTemplates = promptTemplates ?? throw new ArgumentNullException(nameof(promptTemplates));
        _complexityEstimator = complexityEstimator ?? throw new ArgumentNullException(nameof(complexityEstimator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<PlannedTask>> DecomposeAsync(
        RequestAnalysis analysis,
        StageContext context,
        CancellationToken ct)
    {
        _logger.LogInformation("Decomposing into tasks");

        var prompt = _promptTemplates.RenderTemplate("decompose-tasks", new
        {
            intent = analysis.Intent,
            requirements = analysis.Requirements,
            approach = analysis.SuggestedApproach,
            workspace = context.StageData.GetValueOrDefault("workspace")
        });

        var response = await _llm.CompleteAsync(prompt, new LlmOptions
        {
            Temperature = 0.5,
            MaxTokens = 2000
        }, ct);

        var tasks = ParseTasksResponse(response.Text);

        foreach (var task in tasks)
        {
            var complexity = await _complexityEstimator.EstimateAsync(task, ct);
            task.SetComplexity(complexity);
        }

        _logger.LogInformation("Decomposed into {TaskCount} tasks", tasks.Count);
        return tasks;
    }

    private List<PlannedTask> ParseTasksResponse(string responseText)
    {
        // Parse format:
        // TASK: <title>
        // DESCRIPTION: <desc>
        // STEPS: ...
        // See spec lines 2558-2650 for complete parsing logic

        var tasks = new List<PlannedTask>();
        // [Full parsing implementation in spec]
        return tasks;
    }
}
```

**Acceptance Criteria Covered:** AC-017-020, AC-043 (decomposition)

**Success Criteria:**
- [ ] File exists: `src/Acode.Application/Orchestration/Stages/Planner/TaskDecomposer.cs`
- [ ] Renders decompose-tasks prompt
- [ ] Calls LLM
- [ ] Parses task/step/acceptance structure
- [ ] Estimates complexity per task
- [ ] Returns List<PlannedTask>

**Gap Checklist Item:** [ ] 🔄 TaskDecomposer service implemented

---

### Gap 5.2: ComplexityEstimator Service

**Current State:** ❌ MISSING

**Spec Reference:** Referenced in PlanBuilder and TaskDecomposer sections

**What Exists:** Nothing

**What's Missing:** Service to estimate task complexity (1-5 points)

**Implementation Details:**

```csharp
namespace Acode.Application.Orchestration.Stages.Planner;

public interface IComplexityEstimator
{
    Task<int> EstimateAsync(PlannedTask task, CancellationToken ct);
}

public sealed class ComplexityEstimator : IComplexityEstimator
{
    private readonly ILogger<ComplexityEstimator> _logger;

    public ComplexityEstimator(ILogger<ComplexityEstimator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<int> EstimateAsync(PlannedTask task, CancellationToken ct)
    {
        // Estimate based on:
        // - Number of steps
        // - Action types (ReadFile=1, WriteFile=2, ModifyFile=2, RunCommand=3, etc.)
        // - Description length/complexity

        var baseComplexity = Math.Min(task.Steps.Count, 5);
        var actionComplexity = task.Steps.Sum(s => GetActionComplexity(s.Action)) / task.Steps.Count;
        var estimated = Math.Min(5, (baseComplexity + actionComplexity) / 2);

        _logger.LogInformation("Complexity estimated: {Task} = {Complexity}", task.Title, estimated);
        return Task.FromResult(estimated);
    }

    private int GetActionComplexity(ActionType action) => action switch
    {
        ActionType.ReadFile => 1,
        ActionType.WriteFile => 2,
        ActionType.ModifyFile => 3,
        ActionType.CreateDirectory => 1,
        ActionType.RunCommand => 4,
        ActionType.AnalyzeCode => 3,
        ActionType.GenerateCode => 5,
        _ => 2
    };
}
```

**Success Criteria:**
- [ ] File exists: `src/Acode.Application/Orchestration/Stages/Planner/ComplexityEstimator.cs`
- [ ] Implements IComplexityEstimator
- [ ] Returns 1-5 complexity score
- [ ] Based on step count and action types

**Gap Checklist Item:** [ ] 🔄 ComplexityEstimator service implemented

---

## PHASE 6: PLAN BUILDING (1-2 hours)

### Gap 6.1: PlanBuilder Service Implementation

**Current State:** ❌ MISSING

**Spec Reference:** Lines 2655-2708 (Implementation Prompt section)

**What Exists:** Nothing (interface created in Phase 2)

**What's Missing:** Plan building with dependency analysis and validation

**Implementation Details from Spec:**

```csharp
namespace Acode.Application.Orchestration.Stages.Planner;

public sealed class PlanBuilder : IPlanBuilder
{
    private readonly IDependencyAnalyzer _dependencyAnalyzer;
    private readonly ILogger<PlanBuilder> _logger;

    public PlanBuilder(
        IDependencyAnalyzer dependencyAnalyzer,
        ILogger<PlanBuilder> logger)
    {
        _dependencyAnalyzer = dependencyAnalyzer ?? throw new ArgumentNullException(nameof(dependencyAnalyzer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public TaskPlan Build(SessionId sessionId, string goal, List<PlannedTask> tasks, int version = 1)
    {
        _logger.LogInformation("Building plan with {TaskCount} tasks", tasks.Count);

        var dependencyGraph = _dependencyAnalyzer.AnalyzeDependencies(tasks);

        if (dependencyGraph.HasCycles())
            throw new PlanningException("Dependency graph contains cycles");

        var totalComplexity = tasks.Sum(t => t.Complexity);

        var plan = new TaskPlan(
            Id: PlanId.NewId(),
            Version: version,
            SessionId: sessionId,
            Goal: goal,
            Tasks: tasks.AsReadOnly(),
            Dependencies: dependencyGraph,
            TotalComplexity: totalComplexity,
            CreatedAt: DateTimeOffset.UtcNow);

        _logger.LogInformation("Plan built: {PlanId}, Complexity: {Complexity}",
            plan.Id, totalComplexity);

        return plan;
    }
}

public interface IDependencyAnalyzer
{
    DependencyGraph AnalyzeDependencies(List<PlannedTask> tasks);
}

public sealed class DependencyAnalyzer : IDependencyAnalyzer
{
    public DependencyGraph AnalyzeDependencies(List<PlannedTask> tasks)
    {
        var graph = new DependencyGraph();
        // Analyze task descriptions for dependency keywords
        // (This is a simplified stub - can be extended with LLM analysis)
        return graph;
    }
}
```

**Acceptance Criteria Covered:** AC-030-039 (dependencies, plan)

**Success Criteria:**
- [ ] File exists: `src/Acode.Application/Orchestration/Stages/Planner/PlanBuilder.cs`
- [ ] Analyzes dependencies
- [ ] Validates no cycles
- [ ] Creates TaskPlan with all metadata
- [ ] Calculates total complexity
- [ ] Returns immutable plan

**Gap Checklist Item:** [ ] 🔄 PlanBuilder service implemented

---

## PHASE 7: INTEGRATION & TESTING (2-3 hours)

### Gap 7.1: PlannerStageTests Unit Tests

**Current State:** ❌ MISSING

**Spec Reference:** Lines 1549-1693 (Testing Requirements section)

**What Exists:** Nothing

**What's Missing:** 2 test methods for PlannerStage

**Test Code from Spec:**

```csharp
namespace Acode.Application.Tests.Orchestration.Stages.Planner;

public class PlannerStageTests
{
    [Fact]
    public async Task Should_Create_Plan_For_Simple_Request()
    {
        // [See spec lines 1577-1642 for full test code]
        // Tests creating plan for simple request
        // Verifies: Status=Success, Tasks created, NextStage=Executor
    }

    [Fact]
    public async Task Should_Request_Clarification_When_Ambiguous()
    {
        // [See spec lines 1644-1672 for full test code]
        // Tests ambiguity detection
        // Verifies: Status=Retry, Message contains "Clarification needed"
    }
}
```

**File Location:** `tests/Acode.Application.Tests/Orchestration/Stages/Planner/PlannerStageTests.cs`

**Acceptance Criteria Covered:** AC-001-005, AC-012-016, AC-044-047

**Success Criteria:**
- [ ] File exists with 2 [Fact] methods
- [ ] Both tests passing
- [ ] Mock setup correct
- [ ] Assertions verify key behaviors

**Gap Checklist Item:** [ ] 🔄 PlannerStageTests created and passing

---

### Gap 7.2: TaskDecomposerTests Unit Tests

**Current State:** ❌ MISSING

**Spec Reference:** Lines 1695-1784 (Testing Requirements section)

**Test Code from Spec:**

```csharp
namespace Acode.Application.Tests.Orchestration.Stages.Planner;

public class TaskDecomposerTests
{
    [Fact]
    public async Task Should_Decompose_Into_Tasks_And_Steps()
    {
        // [See spec lines 1717-1773 for full test code]
        // Tests decomposition logic
        // Verifies: Correct task count, step count, complexity estimation
    }
}
```

**File Location:** `tests/Acode.Application.Tests/Orchestration/Stages/Planner/TaskDecomposerTests.cs`

**Acceptance Criteria Covered:** AC-017-020

**Success Criteria:**
- [ ] File exists with 1 [Fact] method
- [ ] Test passing
- [ ] Verifies task and step parsing

**Gap Checklist Item:** [ ] 🔄 TaskDecomposerTests created and passing

---

### Gap 7.3: PlannerIntegrationTests Integration Tests

**Current State:** ❌ MISSING

**Spec Reference:** Lines 1870-1905 (Testing Requirements section)

**Test Code from Spec:**

```csharp
namespace Acode.Application.Tests.Integration.Orchestration.Stages.Planner;

public class PlannerIntegrationTests : IClassFixture<TestServerFixture>
{
    [Fact]
    public async Task Should_Plan_Real_Workspace_With_Full_Context()
    {
        // [See spec lines 1880-1903 for full test code]
        // Tests with real services
        // Verifies: Full end-to-end planning with actual workspace
    }
}
```

**File Location:** `tests/Acode.Application.Tests/Integration/Orchestration/Stages/Planner/PlannerIntegrationTests.cs`

**Acceptance Criteria Covered:** AC-001-054 (all through integration)

**Success Criteria:**
- [ ] File exists with 1 [Fact] method
- [ ] Uses TestServerFixture
- [ ] Test passing with real services

**Gap Checklist Item:** [ ] 🔄 PlannerIntegrationTests created and passing

---

### Gap 7.4: PlannerE2ETests End-to-End Tests

**Current State:** ❌ MISSING

**Spec Reference:** Lines 1912-1954 (Testing Requirements section)

**Test Code from Spec:**

```csharp
namespace Acode.Application.Tests.E2E.Orchestration.Stages.Planner;

public class PlannerE2ETests : IClassFixture<E2ETestFixture>
{
    [Fact]
    public async Task Should_Plan_File_Creation_Task()
    {
        // [See spec lines 1922-1936 for full test code]
        // Tests file creation planning
    }

    [Fact]
    public async Task Should_Plan_Refactoring_Task_With_Multiple_Steps()
    {
        // [See spec lines 1939-1952 for full test code]
        // Tests complex refactoring planning
    }
}
```

**File Location:** `tests/Acode.Application.Tests/E2E/Orchestration/Stages/Planner/PlannerE2ETests.cs`

**Acceptance Criteria Covered:** AC-001-054 (realistic scenarios)

**Success Criteria:**
- [ ] File exists with 2 [Fact] methods
- [ ] Both tests passing
- [ ] Tests realistic planning scenarios

**Gap Checklist Item:** [ ] 🔄 PlannerE2ETests created and passing

---

## PHASE 7 VERIFICATION CHECKLIST

Before completing task-012a:

- [ ] ContextPreparator service created and working
- [ ] RequestAnalyzer service parsing correctly
- [ ] TaskDecomposer service decomposing tasks
- [ ] ComplexityEstimator service estimating
- [ ] PlanBuilder service building plans
- [ ] PlannerStageTests (2 tests) passing
- [ ] TaskDecomposerTests (1 test) passing
- [ ] PlannerIntegrationTests (1 test) passing
- [ ] PlannerE2ETests (2 tests) passing
- [ ] All 9+ test methods passing
- [ ] No NotImplementedException anywhere
- [ ] Build: 0 errors, 0 warnings

**Phase 7 Status:** [ ] 🔄 COMPLETE

---

## FINAL VERIFICATION BEFORE MARKING COMPLETE

### File Count
- [ ] 13 production files created (7 Application + 6 Domain)
- [ ] 5 test files created
- [ ] All files in correct directories

### Semantic Completeness
- [ ] Zero NotImplementedException anywhere
- [ ] Zero TODO/FIXME indicators
- [ ] All 54 ACs verified implemented

### Test Coverage
- [ ] 9+ test methods total
- [ ] All tests passing: `dotnet test --filter "Planner"`
- [ ] Coverage includes: units, integration, E2E

### Build Status
- [ ] dotnet build → 0 errors, 0 warnings
- [ ] All dependencies resolved
- [ ] No CS warnings

### Acceptance Criteria Verification
- [ ] AC-001-054: All manually verified in code

---

**IMPLEMENTATION COMPLETE WHEN:**
- All 7 phases done
- All verification checks passed
- All 54 ACs verified
- PR created and tests passing

---
