# Task 012.a: Planner Stage

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 012 (Multi-Stage Loop), Task 049 (Conversation), Task 050 (Workspace DB)  

---

## Description

Task 012.a implements the Planner stage—the first stage in the multi-stage agent loop. The Planner is responsible for analyzing user requests and decomposing them into executable task plans. Without effective planning, the Executor has no guidance, the Verifier has no criteria, and the Reviewer has no baseline for quality assessment.

The Planner receives the user's request in natural language along with relevant context: conversation history, workspace structure, and file contents when needed. It must understand what the user wants, determine what changes are required, and create a structured plan of tasks and steps that will achieve the goal.

Planning is inherently a reasoning-heavy operation. The LLM must consider multiple approaches, evaluate trade-offs, anticipate obstacles, and synthesize a coherent plan. The Planner stage provides the LLM with appropriate context and prompts to guide this reasoning, then parses the structured output into executable artifacts.

Task decomposition follows a hierarchy: Goal → Tasks → Steps. A goal like "Add email validation" might decompose into tasks like "Create validator class" and "Update form handler", each with steps like "Read existing code", "Generate validator", "Write file". This hierarchy enables progress tracking, checkpointing, and targeted retry on failure.

The Planner outputs a TaskPlan—a structured representation of the work to be done. The TaskPlan includes dependency information (which tasks/steps must complete before others), estimated complexity, resource requirements (files to read/write), and acceptance criteria that the Verifier will check.

Re-planning is a critical capability. When the Reviewer rejects work or when execution reveals new information, the Planner may be invoked again to adjust the plan. Re-planning maintains continuity—completed work is preserved while remaining work is revised. The Planner tracks plan versions and can explain what changed and why.

Context preparation is the Planner's first responsibility. It must gather relevant information: workspace structure, key file contents, existing patterns in the codebase, configuration files. The Planner uses the Workspace Database (Task 050) to efficiently query context without exceeding token budgets.

Error handling in the Planner focuses on graceful degradation. If the request is ambiguous, the Planner asks clarifying questions rather than guessing. If required files are missing, the Planner notes assumptions. If the task seems too large, the Planner suggests breaking it into phases. The goal is never to fail silently.

The Planner respects token budgets. Complex requests may exceed available context. The Planner uses summarization, selective inclusion, and progressive disclosure to stay within limits while maintaining effectiveness. Token usage is tracked and reported.

Integration with Task 001 constraints is essential. The Planner uses only local LLM providers (Ollama, LM Studio). Planning prompts are designed for the models available. Burst mode affects timeout and retry policies. Air-gapped mode may limit available context sources.

Performance is important—users shouldn't wait long for planning. Simple requests should plan quickly. Complex requests justify more time. The Planner provides progress indicators during planning. Timeouts prevent runaway planning sessions.

Testing the Planner requires diverse scenarios: simple single-file changes, multi-file refactorings, new feature implementations, bug fixes, and edge cases. Each scenario validates that the plan is reasonable, complete, and executable. Regression tests ensure plan quality doesn't degrade.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Planner Stage | First stage that creates execution plan |
| TaskPlan | Structured output of planning |
| Task | High-level unit of work |
| Step | Atomic unit within a task |
| Decomposition | Breaking goal into tasks/steps |
| Dependency Graph | Order constraints between tasks |
| Acceptance Criteria | Conditions for step verification |
| Context Preparation | Gathering info for planning |
| Re-planning | Revising plan after feedback |
| Plan Version | Tracking plan revisions |
| Token Budget | Allowed tokens for planning |
| Clarifying Question | Request for more info |
| Complexity Estimate | Predicted effort level |
| Resource Requirement | Files/tools needed |
| Progressive Disclosure | Revealing context gradually |

---

## Out of Scope

The following items are explicitly excluded from Task 012.a:

- **Step execution** - Task 012.b
- **Verification logic** - Task 012.c
- **Quality review** - Task 012.d
- **Tool invocation** - Executor responsibility
- **Code generation** - Executor responsibility
- **Multi-model planning** - Single model only
- **Parallel planning** - Sequential only
- **Plan caching** - Future optimization
- **Plan templates** - Future feature
- **External planning services** - Task 001 constraint

---

## Functional Requirements

### Stage Lifecycle

- FR-001: Planner MUST implement IStage interface
- FR-002: OnEnter MUST prepare context
- FR-003: Execute MUST produce TaskPlan
- FR-004: OnExit MUST persist plan
- FR-005: Lifecycle events MUST be logged

### Context Preparation

- FR-006: Planner MUST load conversation history
- FR-007: Planner MUST query workspace structure
- FR-008: Planner MUST identify relevant files
- FR-009: Planner MUST load file contents
- FR-010: Context MUST respect token budget
- FR-011: Context MUST be summarized if too large
- FR-012: Context loading MUST be logged

### Request Analysis

- FR-013: Planner MUST parse user request
- FR-014: Planner MUST identify intent
- FR-015: Planner MUST extract entities
- FR-016: Ambiguous requests MUST trigger clarification
- FR-017: Invalid requests MUST be rejected
- FR-018: Analysis MUST be logged

### Task Decomposition

- FR-019: Request MUST decompose into tasks
- FR-020: Tasks MUST decompose into steps
- FR-021: Each task MUST have unique ID
- FR-022: Each step MUST have unique ID
- FR-023: IDs SHOULD be UUID v7 for time-ordered identifiers
  - Implementations MUST either:
    - (a) Use a UUID library that supports UUID v7 (RFC 9562, May 2024), OR
    - (b) Document and use a fallback strategy (e.g., UUID v4 combined with explicit `created_at` timestamps) in environments where UUID v7 is not yet available
  - .NET 9.0+ has native UUID v7 support via `Guid.CreateVersion7()`
  - .NET 8.0 and earlier: Use NuGet package or fallback to UUID v4 + timestamp
- FR-024: Decomposition MUST be logged

**UUID v7 Rationale:**
- Time-ordered for efficient database indexing
- Sortable by creation time without additional fields
- Better than UUID v4 for distributed systems
- Fallback preserves functionality in older runtimes

### Task Definition

- FR-025: Task MUST have title
- FR-026: Task MUST have description
- FR-027: Task MUST have complexity estimate
- FR-028: Task MUST list resource requirements
- FR-029: Task MUST have acceptance criteria

### Step Definition

- FR-030: Step MUST have title
- FR-031: Step MUST have description
- FR-032: Step MUST have action type
- FR-033: Step MUST have expected output
- FR-034: Step MUST have verification criteria

### Action Types

- FR-035: READ_FILE action type
- FR-036: WRITE_FILE action type
- FR-037: MODIFY_FILE action type
- FR-038: CREATE_DIRECTORY action type
- FR-039: RUN_COMMAND action type
- FR-040: ANALYZE_CODE action type
- FR-041: GENERATE_CODE action type

### Dependency Graph

- FR-042: Tasks MAY have dependencies
- FR-043: Steps MAY have dependencies
- FR-044: Dependencies MUST be acyclic
- FR-045: Dependency order MUST be validated
- FR-046: Circular dependencies MUST error

### Resource Requirements

- FR-047: Files to read MUST be listed
- FR-048: Files to write MUST be listed
- FR-049: Directories to create MUST be listed
- FR-050: Commands to run MUST be listed
- FR-051: Requirements MUST be validated

### Acceptance Criteria

- FR-052: Each task MUST have criteria
- FR-053: Criteria MUST be verifiable
- FR-054: Criteria MUST include assertions
- FR-055: Test criteria MUST be flagged

### TaskPlan Output

- FR-056: Plan MUST have unique ID
- FR-057: Plan MUST have version number
- FR-058: Plan MUST list all tasks
- FR-059: Plan MUST include dependency graph
- FR-060: Plan MUST include total estimate
- FR-061: Plan MUST be JSON-serializable

### Re-planning

- FR-062: Re-plan MUST increment version
- FR-063: Re-plan MUST preserve completed tasks
- FR-064: Re-plan MUST explain changes
- FR-065: Re-plan MUST log reason
- FR-066: Previous versions MUST be retained

### Clarifying Questions

- FR-067: Questions MUST be specific
- FR-068: Questions MUST have options if possible
- FR-069: Questions MUST timeout to defaults
- FR-070: User responses MUST update context
- FR-071: Questions MUST be logged

### Complexity Estimation

- FR-072: Estimate MUST use Fibonacci scale
- FR-073: Estimate MUST consider file count
- FR-074: Estimate MUST consider change scope
- FR-075: Estimate MUST be logged

### Token Management

- FR-076: Budget MUST be checked before LLM call
- FR-077: Context MUST be trimmed if needed
- FR-078: Trimming MUST preserve important info
- FR-079: Token usage MUST be tracked
- FR-080: Budget exceeded MUST warn

### Error Handling

- FR-081: Parse errors MUST be retried
- FR-082: Invalid plans MUST be rejected
- FR-083: Timeout MUST be handled
- FR-084: LLM errors MUST escalate
- FR-085: All errors MUST be logged

---

## Non-Functional Requirements

### Performance

- NFR-001: Simple plan < 10 seconds
- NFR-002: Complex plan < 60 seconds
- NFR-003: Context preparation < 5 seconds
- NFR-004: Memory < 100MB overhead

### Reliability

- NFR-005: Plans MUST be valid JSON
- NFR-006: Plans MUST be complete
- NFR-007: Parse failures < 5%

### Quality

- NFR-008: Plans MUST be executable
- NFR-009: Steps MUST be atomic
- NFR-010: Dependencies MUST be correct

### Security

- NFR-011: No secrets in plans
- NFR-012: No arbitrary code in plans
- NFR-013: Paths MUST be validated

### Observability

- NFR-014: All operations logged
- NFR-015: Token usage tracked
- NFR-016: Plan quality metrics

---

## User Manual Documentation

### Overview

The Planner stage analyzes your request and creates an execution plan. This plan guides all subsequent stages in completing your task.

### How Planning Works

When you submit a request, the Planner:

1. **Gathers Context** - Reads relevant files, understands workspace
2. **Analyzes Request** - Identifies what you want to accomplish
3. **Decomposes Work** - Breaks into tasks and steps
4. **Creates Plan** - Structures work with dependencies
5. **Returns Result** - Plan passes to Executor

### Viewing Plans

```bash
# View current plan
$ acode plan show

Task Plan (v1) - Session abc123
Goal: Add email validation

Tasks:
  1. [PENDING] Create EmailValidator class
     Steps:
       1.1 Read existing validators (analyze)
       1.2 Generate EmailValidator (generate)
       1.3 Write to validators/ (write)
       
  2. [PENDING] Update form handler
     Depends: Task 1
     Steps:
       2.1 Read form handler (read)
       2.2 Add validation call (modify)
       
  3. [PENDING] Add unit tests
     Depends: Task 1
     Steps:
       3.1 Read test patterns (analyze)
       3.2 Generate tests (generate)
       3.3 Write tests (write)

Estimated Complexity: 8 (Fibonacci)
```

### Plan Details

```bash
# View task details
$ acode plan show --task 1

Task 1: Create EmailValidator class
  Status: PENDING
  Complexity: 3
  
  Resources:
    Read: src/validators/*.ts
    Write: src/validators/EmailValidator.ts
    
  Acceptance Criteria:
    - EmailValidator class exists
    - Validates email format
    - Handles edge cases (empty, null)
    - Has TypeDoc comments
    
  Steps:
    1.1 Read existing validators
        Action: ANALYZE_CODE
        Purpose: Understand patterns
        
    1.2 Generate EmailValidator
        Action: GENERATE_CODE
        Template: validator
        
    1.3 Write to validators/
        Action: WRITE_FILE
        Target: src/validators/EmailValidator.ts
```

### Re-planning

When the Reviewer requests changes:

```
$ acode run "Add email validation"
...
[REVIEWER] Issue: Missing phone validation
[REVIEWER] Requesting re-plan

[PLANNER] Re-planning (v2)
[PLANNER] Changes:
  + Added Task 4: Create PhoneValidator
  + Added Step 2.3: Add phone validation call
  
Updated plan:
  Tasks 1-3: Unchanged
  Task 4: [NEW] Create PhoneValidator
```

### Clarifying Questions

When request is ambiguous:

```
$ acode run "Add validation"

[PLANNER] Request needs clarification:

What type of validation should I add?
  1. Email format validation
  2. Phone number validation  
  3. Required field validation
  4. Custom (describe)

Enter choice [1-4]: 1

[PLANNER] Planning email validation...
```

### Configuration

```yaml
# .agent/config.yml
planner:
  # Timeout for planning (seconds)
  timeout: 60
  
  # Maximum tokens for context
  max_context_tokens: 8000
  
  # Include patterns for context
  context_includes:
    - "src/**/*.ts"
    - "*.config.js"
    
  # Exclude patterns
  context_excludes:
    - "node_modules/**"
    - "**/*.test.ts"
    
  # Default complexity cap (Fibonacci)
  max_complexity: 21
```

### Complexity Estimates

| Fibonacci | Description |
|-----------|-------------|
| 1 | Trivial (config change) |
| 2 | Simple (single file) |
| 3 | Small (few files, clear) |
| 5 | Medium (multiple files) |
| 8 | Moderate (cross-cutting) |
| 13 | Complex (significant change) |
| 21 | Large (major feature) |
| 34 | Very Large (epic scope) |

### Best Practices

1. **Be Specific** - Clear requests get better plans
2. **Provide Context** - Mention relevant files/patterns
3. **One Goal Per Request** - Don't combine unrelated work
4. **Review Plans** - Check before execution starts

### Troubleshooting

#### Plan Takes Too Long

**Problem:** Planning exceeds timeout

**Solutions:**
1. Simplify request
2. Increase timeout: `acode config set planner.timeout 120`
3. Reduce context: exclude large directories

#### Plan Seems Wrong

**Problem:** Plan doesn't match intent

**Solutions:**
1. Reject and re-request with more detail
2. Answer clarifying questions carefully
3. Provide example of expected output

#### Context Not Found

**Problem:** Planner can't find relevant files

**Solutions:**
1. Check include patterns
2. Ensure files exist
3. Mention specific paths in request

---

## Acceptance Criteria

### Stage Lifecycle

- [ ] AC-001: IStage implemented
- [ ] AC-002: OnEnter prepares context
- [ ] AC-003: Execute produces plan
- [ ] AC-004: OnExit persists plan
- [ ] AC-005: Events logged

### Context Preparation

- [ ] AC-006: History loaded
- [ ] AC-007: Workspace queried
- [ ] AC-008: Relevant files identified
- [ ] AC-009: Contents loaded
- [ ] AC-010: Token budget respected
- [ ] AC-011: Summarization works

### Request Analysis

- [ ] AC-012: Request parsed
- [ ] AC-013: Intent identified
- [ ] AC-014: Entities extracted
- [ ] AC-015: Clarification triggered
- [ ] AC-016: Invalid rejected

### Decomposition

- [ ] AC-017: Tasks created
- [ ] AC-018: Steps created
- [ ] AC-019: IDs are UUID v7
- [ ] AC-020: Logged

### Task Definition

- [ ] AC-021: Has title
- [ ] AC-022: Has description
- [ ] AC-023: Has estimate
- [ ] AC-024: Has resources
- [ ] AC-025: Has criteria

### Step Definition

- [ ] AC-026: Has title
- [ ] AC-027: Has action type
- [ ] AC-028: Has expected output
- [ ] AC-029: Has verification

### Dependencies

- [ ] AC-030: Task deps work
- [ ] AC-031: Step deps work
- [ ] AC-032: Acyclic validated
- [ ] AC-033: Order correct

### TaskPlan

- [ ] AC-034: Has ID
- [ ] AC-035: Has version
- [ ] AC-036: Lists tasks
- [ ] AC-037: Has graph
- [ ] AC-038: Has estimate
- [ ] AC-039: JSON serializable

### Re-planning

- [ ] AC-040: Version incremented
- [ ] AC-041: Completed preserved
- [ ] AC-042: Changes explained
- [ ] AC-043: Logged

### Questions

- [ ] AC-044: Specific
- [ ] AC-045: Has options
- [ ] AC-046: Timeout works
- [ ] AC-047: Responses update context

### Tokens

- [ ] AC-048: Budget checked
- [ ] AC-049: Trimming works
- [ ] AC-050: Usage tracked

### Errors

- [ ] AC-051: Parse errors retried
- [ ] AC-052: Invalid rejected
- [ ] AC-053: Timeout handled
- [ ] AC-054: Escalation works

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Orchestration/Stages/Planner/
├── PlannerStageTests.cs
│   ├── Should_Create_Plan_For_Simple_Request()
│   ├── Should_Decompose_Into_Tasks()
│   ├── Should_Decompose_Into_Steps()
│   └── Should_Generate_UUID_v7_IDs()
│
├── ContextPreparationTests.cs
│   ├── Should_Load_Conversation_History()
│   ├── Should_Query_Workspace()
│   ├── Should_Respect_Token_Budget()
│   └── Should_Summarize_When_Large()
│
├── DependencyGraphTests.cs
│   ├── Should_Create_Valid_Graph()
│   ├── Should_Reject_Circular_Dependencies()
│   └── Should_Order_Correctly()
│
├── ReplanningTests.cs
│   ├── Should_Increment_Version()
│   ├── Should_Preserve_Completed()
│   └── Should_Explain_Changes()
│
└── ComplexityEstimationTests.cs
    ├── Should_Use_Fibonacci_Scale()
    ├── Should_Consider_File_Count()
    └── Should_Consider_Change_Scope()
```

### Integration Tests

```
Tests/Integration/Orchestration/Stages/Planner/
├── PlannerIntegrationTests.cs
│   ├── Should_Plan_Real_Workspace()
│   ├── Should_Handle_Large_Context()
│   └── Should_Persist_Plan()
│
└── ClarificationTests.cs
    ├── Should_Ask_Questions()
    └── Should_Update_Context()
```

### E2E Tests

```
Tests/E2E/Orchestration/Stages/Planner/
├── PlannerE2ETests.cs
│   ├── Should_Plan_File_Creation()
│   ├── Should_Plan_Refactoring()
│   └── Should_Plan_Bug_Fix()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Simple plan | 5s | 10s |
| Complex plan | 30s | 60s |
| Context prep | 2s | 5s |
| Parse output | 100ms | 500ms |

---

## User Verification Steps

### Scenario 1: Simple Plan

1. Run `acode run "Add README"`
2. Observe: Planning starts
3. Observe: Plan created
4. Verify: Single task, few steps

### Scenario 2: Multi-Task Plan

1. Run `acode run "Add validation and tests"`
2. Observe: Planning
3. Verify: Multiple tasks
4. Verify: Dependencies shown

### Scenario 3: Clarification

1. Run `acode run "Add validation"` (ambiguous)
2. Observe: Question asked
3. Answer question
4. Verify: Plan reflects answer

### Scenario 4: Re-planning

1. Start task
2. Trigger review rejection
3. Observe: Re-planning
4. Verify: Version incremented

### Scenario 5: Complex Context

1. Run in large workspace
2. Observe: Context gathered
3. Verify: Relevant files included
4. Verify: Token budget respected

### Scenario 6: Plan Viewing

1. Run `acode plan show`
2. Verify: Tasks listed
3. Verify: Steps shown
4. Verify: Dependencies shown

### Scenario 7: Timeout

1. Set short timeout
2. Run complex request
3. Observe: Timeout occurs
4. Verify: Handled gracefully

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/
├── Orchestration/
│   └── Stages/
│       └── Planner/
│           ├── IPlannerStage.cs
│           ├── PlannerStage.cs
│           ├── ContextPreparator.cs
│           ├── RequestAnalyzer.cs
│           ├── TaskDecomposer.cs
│           ├── PlanBuilder.cs
│           └── ComplexityEstimator.cs
│
src/AgenticCoder.Domain/
├── Planning/
│   ├── TaskPlan.cs
│   ├── PlannedTask.cs
│   ├── PlannedStep.cs
│   ├── ActionType.cs
│   ├── DependencyGraph.cs
│   └── AcceptanceCriteria.cs
```

### IPlannerStage Interface

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Planner;

public interface IPlannerStage : IStage
{
    Task<TaskPlan> CreatePlanAsync(PlanningContext context, CancellationToken ct);
    Task<TaskPlan> ReplanAsync(TaskPlan existing, ReplanReason reason, CancellationToken ct);
}
```

### TaskPlan Domain Entity

```csharp
namespace AgenticCoder.Domain.Planning;

public sealed class TaskPlan
{
    public PlanId Id { get; }
    public int Version { get; }
    public SessionId SessionId { get; }
    public string Goal { get; }
    public IReadOnlyList<PlannedTask> Tasks { get; }
    public DependencyGraph Dependencies { get; }
    public int TotalComplexity { get; }
    public DateTimeOffset CreatedAt { get; }
    
    public TaskPlan IncrementVersion() => ...;
    public TaskPlan WithTasks(IEnumerable<PlannedTask> tasks) => ...;
}

public sealed class PlannedTask
{
    public TaskId Id { get; }
    public string Title { get; }
    public string Description { get; }
    public int Complexity { get; }
    public IReadOnlyList<PlannedStep> Steps { get; }
    public ResourceRequirements Resources { get; }
    public IReadOnlyList<AcceptanceCriterion> AcceptanceCriteria { get; }
    public TaskStatus Status { get; }
}

public sealed class PlannedStep
{
    public StepId Id { get; }
    public string Title { get; }
    public string Description { get; }
    public ActionType Action { get; }
    public string? ExpectedOutput { get; }
    public VerificationCriteria Verification { get; }
    public StepStatus Status { get; }
}

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

### PlannerStage Implementation

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Planner;

public sealed class PlannerStage : StageBase, IPlannerStage
{
    private readonly ContextPreparator _contextPreparator;
    private readonly RequestAnalyzer _requestAnalyzer;
    private readonly TaskDecomposer _decomposer;
    private readonly PlanBuilder _builder;
    private readonly ILlmService _llm;
    
    public override StageType Type => StageType.Planner;
    
    protected override async Task OnEnterAsync(StageContext context, CancellationToken ct)
    {
        await _contextPreparator.PrepareAsync(context, ct);
    }
    
    protected override async Task<StageResult> ExecuteAsync(
        StageContext context, 
        CancellationToken ct)
    {
        var request = context.Session.CurrentRequest;
        var analysis = await _requestAnalyzer.AnalyzeAsync(request, context, ct);
        
        if (analysis.NeedsClarification)
        {
            return StageResult.Clarification(analysis.Questions);
        }
        
        var tasks = await _decomposer.DecomposeAsync(analysis, context, ct);
        var plan = _builder.Build(context.Session.Id, request.Goal, tasks);
        
        return StageResult.Success(plan, nextStage: StageType.Executor);
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-PLAN-001 | Request parsing failed |
| ACODE-PLAN-002 | Context preparation failed |
| ACODE-PLAN-003 | Decomposition failed |
| ACODE-PLAN-004 | Invalid plan structure |
| ACODE-PLAN-005 | Circular dependency |
| ACODE-PLAN-006 | Token budget exceeded |
| ACODE-PLAN-007 | Planning timeout |

### Logging Fields

```json
{
  "event": "plan_created",
  "session_id": "abc123",
  "plan_id": "plan_xyz",
  "version": 1,
  "goal": "Add email validation",
  "task_count": 3,
  "step_count": 8,
  "complexity": 8,
  "context_tokens": 4500,
  "duration_ms": 8234
}
```

### Implementation Checklist

1. [ ] Create IPlannerStage interface
2. [ ] Implement PlannerStage
3. [ ] Create ContextPreparator
4. [ ] Implement context gathering
5. [ ] Create RequestAnalyzer
6. [ ] Implement intent extraction
7. [ ] Create TaskDecomposer
8. [ ] Implement decomposition logic
9. [ ] Create PlanBuilder
10. [ ] Implement plan assembly
11. [ ] Create TaskPlan domain entity
12. [ ] Create PlannedTask entity
13. [ ] Create PlannedStep entity
14. [ ] Implement DependencyGraph
15. [ ] Add complexity estimation
16. [ ] Implement re-planning
17. [ ] Add clarification questions
18. [ ] Write unit tests
19. [ ] Write integration tests
20. [ ] Write E2E tests

### Validation Checklist Before Merge

- [ ] Simple requests produce valid plans
- [ ] Complex requests decompose correctly
- [ ] Dependencies are acyclic
- [ ] Token budgets respected
- [ ] Re-planning works
- [ ] Clarification questions work
- [ ] Plans are JSON serializable
- [ ] Performance targets met
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** Domain entities
2. **Phase 2:** Context preparation
3. **Phase 3:** Request analysis
4. **Phase 4:** Task decomposition
5. **Phase 5:** Plan building
6. **Phase 6:** Re-planning
7. **Phase 7:** Integration

---

**End of Task 012.a Specification**