# Task 012: Multi-Stage Agent Loop

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 34 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 010 (CLI Framework), Task 011 (State Machine), Task 049 (Conversation), Task 050 (Workspace DB)  

---

## Description

Task 012 implements the multi-stage agent loop that orchestrates the complete lifecycle of an agentic coding session. This is the heart of Acode—the central execution engine that coordinates planning, execution, verification, and review stages into a coherent, reliable, and auditable workflow.

The multi-stage architecture separates concerns that are fundamentally different. Planning requires broad reasoning about goals and strategies. Execution requires precise, step-by-step tool invocation. Verification requires skeptical assessment of outcomes. Review requires holistic quality evaluation. By separating these into distinct stages, each can be optimized for its purpose.

Stage transitions are the critical control points. Each transition represents a decision about whether work is ready to proceed. Transitions may require human approval (Task 013), may trigger persistence checkpoints (Task 011.c), and always generate audit events. The orchestrator manages these transitions according to configured policies.

The loop is not strictly linear. Verification failure may cycle back to execution. Review may request re-planning. The orchestrator supports these cycles while preventing infinite loops through iteration limits and escalation policies. Each cycle is tracked and auditable.

Conversation context flows through stages but in stage-appropriate ways. The planner needs the full conversation to understand user intent. The executor needs focused context for the current step. The verifier needs the step output and acceptance criteria. The reviewer needs the complete task history. Context management is central to effective multi-stage operation.

Integration with the state machine (Task 011) provides persistence and resume capability. Every stage transition is a state machine event. Crashes at any point allow resume from the last transition. The orchestrator never loses work—at worst, partial work in the current stage is retried.

Error handling follows a hierarchy: step-level retry, stage-level retry, task-level abort, session-level pause. Each level has configured limits and policies. Transient failures (network blips, model overload) trigger retries. Persistent failures trigger escalation. Unrecoverable failures pause the session for human intervention.

The orchestrator respects Task 001 constraints. All LLM invocations go through local models (Ollama, LM Studio). No external API calls. Burst mode configuration affects timeout and retry policies. Air-gapped mode affects available verification tools.

Performance matters—users shouldn't wait for orchestration overhead. Stage transitions are quick. Context preparation is lazy where possible. Parallelization is used where stages permit. The orchestrator optimizes throughput while maintaining correctness.

Observability is comprehensive. Every stage entry, transition, and completion is logged with structured data. Metrics track stage duration, retry counts, cycle counts. Distributed tracing correlates operations across stages. Users can always understand what Acode is doing and why.

Extensibility is designed in. New stages can be added. Stage behavior can be customized through configuration. The orchestrator is an abstract pipeline that specific stages plug into. This future-proofs the architecture for evolving agentic workflows.

Testing multi-stage loops is challenging but essential. Unit tests verify individual stage transitions. Integration tests verify stage coordination. E2E tests verify complete workflows. Property-based tests explore edge cases in state transitions. The test suite ensures confidence in this critical system.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Agent Loop | Continuous cycle of LLM reasoning and tool use |
| Stage | Distinct phase with specific purpose |
| Orchestrator | Component managing stage flow |
| Pipeline | Sequential stage execution |
| Transition | Movement between stages |
| Planner Stage | Breaks task into executable steps |
| Executor Stage | Invokes tools to complete steps |
| Verifier Stage | Validates execution results |
| Reviewer Stage | Assesses overall quality |
| Cycle | Returning to earlier stage |
| Iteration Limit | Maximum cycle count |
| Escalation | Moving up error hierarchy |
| Context Window | Available LLM context |
| Token Budget | Allocated tokens per stage |
| Stage Policy | Configuration for stage behavior |

---

## Out of Scope

The following items are explicitly excluded from Task 012:

- **Individual stage implementation** - Task 012.a-d
- **Human approval gates** - Task 013
- **Tool implementation** - Task 014+
- **Code generation specifics** - Task 015
- **Multi-agent coordination** - Future epic
- **Parallel task execution** - Future epic
- **Custom stage plugins** - Extension mechanism
- **Stage-specific prompts** - Per-stage tasks
- **Model switching per stage** - Single model
- **Stage caching** - Performance optimization
- **Stage timeouts per-tool** - Tool-level task

---

## Functional Requirements

### Orchestrator Core

- FR-001: Orchestrator MUST manage stage pipeline
- FR-002: Pipeline MUST include Plan→Execute→Verify→Review
- FR-003: Stages MUST execute in order
- FR-004: Each stage MUST complete before next
- FR-005: Orchestrator MUST track current stage
- FR-006: Orchestrator MUST track stage history

### Stage Lifecycle

- FR-007: Stage MUST have OnEnter handler
- FR-008: Stage MUST have Execute handler
- FR-009: Stage MUST have OnExit handler
- FR-010: OnEnter MUST prepare context
- FR-011: Execute MUST perform stage work
- FR-012: OnExit MUST clean up resources

### Stage Transitions

- FR-013: Transition MUST be atomic
- FR-014: Transition MUST generate event
- FR-015: Transition MUST update state machine
- FR-016: Transition MUST create checkpoint
- FR-017: Failed transition MUST revert
- FR-018: Transition MUST log source/target

### Stage Results

- FR-019: Stage MUST return structured result
- FR-020: Result MUST include status (success/fail/retry)
- FR-021: Result MUST include output data
- FR-022: Result MUST include next stage hint
- FR-023: Result MUST include metrics

### Cycle Handling

- FR-024: Verifier failure MUST allow executor retry
- FR-025: Reviewer rejection MUST allow re-planning
- FR-026: Cycle MUST increment counter
- FR-027: Cycle MUST be limited
- FR-028: Limit reached MUST escalate
- FR-029: Cycle reason MUST be logged

### Iteration Limits

- FR-030: Default limit MUST be configurable
- FR-031: Per-stage limit MUST be configurable
- FR-032: Default limit MUST be 3
- FR-033: Maximum limit MUST be 10
- FR-034: Limit reached MUST trigger policy

### Escalation Policies

- FR-035: Step retry limit: 3
- FR-036: Stage retry limit: 2
- FR-037: Task abort triggers pause
- FR-038: Session pause requires human
- FR-039: Escalation MUST be logged

### Context Management

- FR-040: Context MUST flow between stages
- FR-041: Each stage MUST get appropriate context
- FR-042: Planner gets full conversation
- FR-043: Executor gets step-focused context
- FR-044: Verifier gets step output
- FR-045: Reviewer gets task history
- FR-046: Context MUST respect token budget

### Token Budgets

- FR-047: Total budget MUST be configurable
- FR-048: Per-stage budget MUST be allocated
- FR-049: Planner: 40% of available
- FR-050: Executor: 30% of available
- FR-051: Verifier: 15% of available
- FR-052: Reviewer: 15% of available
- FR-053: Budget MUST account for system prompts

### State Machine Integration

- FR-054: Stage changes MUST update state
- FR-055: All events MUST be persisted
- FR-056: Crash recovery MUST resume at stage
- FR-057: State MUST be queryable

### Error Handling

- FR-058: Transient errors MUST retry
- FR-059: Retry MUST have backoff
- FR-060: Persistent errors MUST escalate
- FR-061: Unrecoverable MUST pause session
- FR-062: All errors MUST be logged

### Timeout Handling

- FR-063: Stage MUST have timeout
- FR-064: Default timeout: 5 minutes
- FR-065: Configurable per stage
- FR-066: Timeout MUST cancel cleanly
- FR-067: Timeout MUST allow retry

### Pipeline Control

- FR-068: Pipeline MUST be pausable
- FR-069: Pipeline MUST be resumable
- FR-070: Pipeline MUST be cancellable
- FR-071: Control changes MUST log reason

### Progress Reporting

- FR-072: Stage start MUST be reported
- FR-073: Stage progress MUST be streamable
- FR-074: Stage completion MUST be reported
- FR-075: Cycle start MUST be reported
- FR-076: Overall progress MUST be trackable

### Metrics Collection

- FR-077: Stage duration MUST be tracked
- FR-078: Token usage MUST be tracked
- FR-079: Retry count MUST be tracked
- FR-080: Cycle count MUST be tracked
- FR-081: Metrics MUST be per-stage

### Observability

- FR-082: All operations MUST have trace ID
- FR-083: Spans MUST be created per stage
- FR-084: Events MUST include timing
- FR-085: Logs MUST be structured

---

## Non-Functional Requirements

### Performance

- NFR-001: Stage transition < 50ms
- NFR-002: Context preparation < 200ms
- NFR-003: No blocking on non-critical IO
- NFR-004: Memory < 200MB orchestrator overhead

### Reliability

- NFR-005: Crash-safe at all points
- NFR-006: No lost work on restart
- NFR-007: Eventual completion or pause

### Scalability

- NFR-008: Handle 100+ steps per session
- NFR-009: Handle 10+ cycles per task
- NFR-010: Handle deep nesting (task→step→toolcall)

### Maintainability

- NFR-011: Stages loosely coupled
- NFR-012: New stages addable
- NFR-013: Configuration over code

### Security

- NFR-014: No secrets in stage context
- NFR-015: Sandbox stage execution
- NFR-016: Audit all transitions

### Correctness

- NFR-017: Deterministic given same input
- NFR-018: Idempotent retries
- NFR-019: Consistent state always

---

## User Manual Documentation

### Overview

The multi-stage agent loop is Acode's execution engine. When you run a task, the orchestrator guides it through four stages: Plan, Execute, Verify, Review.

### Stage Overview

```
┌─────────────────────────────────────────────────────┐
│                      Agent Loop                      │
├─────────────────────────────────────────────────────┤
│                                                      │
│   ┌──────────┐    ┌──────────┐    ┌──────────┐      │
│   │  PLAN    │───►│ EXECUTE  │───►│  VERIFY  │      │
│   └──────────┘    └──────────┘    └──────────┘      │
│        ▲               ▲               │            │
│        │               │     fail      │            │
│        │               └───────────────┘            │
│        │                               │            │
│        │                          success           │
│        │                               ▼            │
│        │          reject        ┌──────────┐       │
│        └────────────────────────│  REVIEW  │       │
│                                 └──────────┘       │
│                                      │             │
│                                   approve          │
│                                      ▼             │
│                                   DONE             │
└─────────────────────────────────────────────────────┘
```

### Stages Explained

**1. Planner Stage**
- Analyzes user request
- Breaks into executable steps
- Estimates complexity
- Creates task graph

**2. Executor Stage**  
- Executes each step
- Invokes tools (file read/write, terminal)
- Collects results
- Handles errors

**3. Verifier Stage**
- Validates execution results
- Checks acceptance criteria
- Runs tests if applicable
- Reports verification status

**4. Reviewer Stage**
- Holistic quality assessment
- Code style compliance
- Coherence with user intent
- Final approval

### CLI Integration

```bash
# Standard run with all stages
$ acode run "Add input validation"

# View current stage
$ acode status
Session: abc123
Stage: EXECUTOR
Progress: Step 3/7

# Skip verification (careful!)
$ acode run "Quick fix" --skip-verify

# Force re-review
$ acode review abc123 --force
```

### Stage Progress

During execution, you see stage transitions:

```
$ acode run "Add email validation"

[PLANNER] Analyzing request...
[PLANNER] Created 4-step plan:
  1. Analyze existing validation code
  2. Create email validator class
  3. Add unit tests
  4. Integrate with form handler

[EXECUTOR] Step 1: Analyzing code...
[EXECUTOR] Step 1 complete
[EXECUTOR] Step 2: Creating validator...
...

[VERIFIER] Validating results...
[VERIFIER] Running tests...
[VERIFIER] All tests passed ✓

[REVIEWER] Reviewing changes...
[REVIEWER] Quality check passed ✓

✓ Task complete
```

### Configuration

```yaml
# .agent/config.yml
orchestration:
  # Stage timeouts (seconds)
  planner_timeout: 120
  executor_timeout: 300
  verifier_timeout: 180
  reviewer_timeout: 120
  
  # Retry limits
  step_retry_limit: 3
  stage_retry_limit: 2
  cycle_limit: 3
  
  # Token budgets (percentage of context)
  token_budget:
    planner: 40
    executor: 30
    verifier: 15
    reviewer: 15
```

### Cycle Behavior

When verification fails:

```
[EXECUTOR] Step 2: Creating validator...
[EXECUTOR] Step 2 complete

[VERIFIER] Validating results...
[VERIFIER] Test failure: email format not validated
[VERIFIER] ✗ Verification failed

[EXECUTOR] Retry 1/3: Step 2
[EXECUTOR] Adjusting based on feedback...
```

When review rejects:

```
[REVIEWER] Reviewing changes...
[REVIEWER] Issue: Missing edge case handling
[REVIEWER] Requesting re-plan

[PLANNER] Cycle 2/3: Adjusting plan...
[PLANNER] Added step: Handle edge cases
```

### Error Escalation

```
Error Hierarchy:
  Step failure (retry 3x) → 
  Stage failure (retry 2x) → 
  Task abort (pause session) →
  Human intervention required
```

Example escalation:

```
[EXECUTOR] Step 3 failed: File write error
[EXECUTOR] Retry 1/3...
[EXECUTOR] Retry 2/3...
[EXECUTOR] Retry 3/3...
[EXECUTOR] Step retry limit reached

[ORCHESTRATOR] Stage retry 1/2...
[EXECUTOR] Retrying from step 3...
[EXECUTOR] Step 3 failed again

[ORCHESTRATOR] Stage retry 2/2...
[EXECUTOR] Step 3 failed again

[ORCHESTRATOR] Task aborted
[ORCHESTRATOR] Session paused: Human intervention required

⚠ Session paused. Use 'acode resume' after fixing the issue.
```

### Troubleshooting

#### Infinite Loops

**Problem:** Stage keeps cycling

**Solution:**
1. Check cycle limit: `acode config get orchestration.cycle_limit`
2. View cycle history: `acode session history abc123 --cycles`
3. Increase limit: `acode config set orchestration.cycle_limit 5`

#### Stage Timeouts

**Problem:** Stage times out

**Solution:**
1. Increase timeout: `--planner-timeout 180`
2. Check model performance
3. Reduce task complexity

#### Context Too Large

**Problem:** Token budget exceeded

**Solution:**
1. Reduce conversation history
2. Split into smaller tasks
3. Use summarization

### Advanced Configuration

```yaml
orchestration:
  # Stage-specific settings
  stages:
    planner:
      timeout: 120
      retry_limit: 2
      context_strategy: full
      
    executor:
      timeout: 300
      retry_limit: 3
      context_strategy: focused
      parallel_steps: false
      
    verifier:
      timeout: 180
      retry_limit: 2
      context_strategy: minimal
      auto_retry_on_failure: true
      
    reviewer:
      timeout: 120
      retry_limit: 1
      context_strategy: summary
      require_human_approval: false
```

### Metrics and Observability

View stage metrics:

```bash
$ acode metrics session abc123
Stage Metrics:
  Planner:
    Duration: 12.3s
    Tokens: 2,450 / 4,000
    Retries: 0
    
  Executor:
    Duration: 45.7s
    Tokens: 2,890 / 3,000
    Retries: 1
    Steps: 7
    
  Verifier:
    Duration: 8.2s
    Tokens: 890 / 1,500
    Cycles: 2
    
  Reviewer:
    Duration: 5.1s
    Tokens: 720 / 1,500
    Approved: true
```

---

## Acceptance Criteria

### Orchestrator Core

- [ ] AC-001: Pipeline manages 4 stages
- [ ] AC-002: Stages execute in order
- [ ] AC-003: Stage completion before next
- [ ] AC-004: Current stage tracked
- [ ] AC-005: History maintained

### Stage Lifecycle

- [ ] AC-006: OnEnter prepares context
- [ ] AC-007: Execute performs work
- [ ] AC-008: OnExit cleans up
- [ ] AC-009: All handlers invoked

### Transitions

- [ ] AC-010: Transitions atomic
- [ ] AC-011: Events generated
- [ ] AC-012: State machine updated
- [ ] AC-013: Checkpoints created
- [ ] AC-014: Failed transitions revert

### Results

- [ ] AC-015: Structured result returned
- [ ] AC-016: Status included
- [ ] AC-017: Output data included
- [ ] AC-018: Metrics included

### Cycles

- [ ] AC-019: Verify fail → executor retry
- [ ] AC-020: Review reject → re-plan
- [ ] AC-021: Counter incremented
- [ ] AC-022: Limit enforced
- [ ] AC-023: Escalation triggered

### Limits

- [ ] AC-024: Default limit configurable
- [ ] AC-025: Per-stage limit works
- [ ] AC-026: Default is 3
- [ ] AC-027: Maximum is 10

### Escalation

- [ ] AC-028: Step retry limit 3
- [ ] AC-029: Stage retry limit 2
- [ ] AC-030: Task abort pauses
- [ ] AC-031: Human intervention works
- [ ] AC-032: Escalation logged

### Context

- [ ] AC-033: Context flows between stages
- [ ] AC-034: Planner gets full
- [ ] AC-035: Executor gets focused
- [ ] AC-036: Verifier gets minimal
- [ ] AC-037: Reviewer gets summary
- [ ] AC-038: Token budget respected

### Token Budgets

- [ ] AC-039: Total configurable
- [ ] AC-040: Allocation works
- [ ] AC-041: System prompts counted

### State Machine

- [ ] AC-042: Stage changes update state
- [ ] AC-043: Events persisted
- [ ] AC-044: Crash recovery works
- [ ] AC-045: State queryable

### Errors

- [ ] AC-046: Transient errors retry
- [ ] AC-047: Backoff applied
- [ ] AC-048: Persistent errors escalate
- [ ] AC-049: Unrecoverable pauses
- [ ] AC-050: All errors logged

### Timeouts

- [ ] AC-051: Stage timeout works
- [ ] AC-052: Default is 5 min
- [ ] AC-053: Configurable per stage
- [ ] AC-054: Clean cancellation
- [ ] AC-055: Retry after timeout

### Pipeline Control

- [ ] AC-056: Pause works
- [ ] AC-057: Resume works
- [ ] AC-058: Cancel works
- [ ] AC-059: Control logged

### Progress

- [ ] AC-060: Stage start reported
- [ ] AC-061: Progress streamable
- [ ] AC-062: Completion reported
- [ ] AC-063: Overall trackable

### Metrics

- [ ] AC-064: Duration tracked
- [ ] AC-065: Tokens tracked
- [ ] AC-066: Retries tracked
- [ ] AC-067: Cycles tracked

### Observability

- [ ] AC-068: Trace ID present
- [ ] AC-069: Spans per stage
- [ ] AC-070: Timing in events
- [ ] AC-071: Logs structured

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Orchestration/
├── OrchestratorTests.cs
│   ├── Should_Execute_Stages_In_Order()
│   ├── Should_Handle_Stage_Failure()
│   ├── Should_Track_Current_Stage()
│   └── Should_Generate_Events()
│
├── StageLifecycleTests.cs
│   ├── Should_Call_OnEnter()
│   ├── Should_Call_Execute()
│   ├── Should_Call_OnExit()
│   └── Should_Handle_Lifecycle_Error()
│
├── TransitionTests.cs
│   ├── Should_Be_Atomic()
│   ├── Should_Create_Checkpoint()
│   └── Should_Revert_On_Failure()
│
├── CycleTests.cs
│   ├── Should_Cycle_On_Verify_Fail()
│   ├── Should_Cycle_On_Review_Reject()
│   ├── Should_Enforce_Limit()
│   └── Should_Escalate_At_Limit()
│
└── ContextTests.cs
    ├── Should_Provide_Full_To_Planner()
    ├── Should_Provide_Focused_To_Executor()
    └── Should_Respect_Token_Budget()
```

### Integration Tests

```
Tests/Integration/Orchestration/
├── PipelineIntegrationTests.cs
│   ├── Should_Complete_Full_Pipeline()
│   ├── Should_Resume_After_Crash()
│   └── Should_Handle_Mixed_Outcomes()
│
├── StateIntegrationTests.cs
│   ├── Should_Persist_Stage_Changes()
│   └── Should_Recover_State()
│
└── ErrorIntegrationTests.cs
    ├── Should_Escalate_Correctly()
    └── Should_Pause_On_Unrecoverable()
```

### E2E Tests

```
Tests/E2E/Orchestration/
├── FullWorkflowTests.cs
│   ├── Should_Plan_Execute_Verify_Review()
│   ├── Should_Handle_Verification_Failure()
│   └── Should_Handle_Review_Rejection()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Stage transition | 25ms | 50ms |
| Context preparation | 100ms | 200ms |
| Full pipeline overhead | 500ms | 1s |
| Memory per session | 100MB | 200MB |

---

## User Verification Steps

### Scenario 1: Full Pipeline

1. Run `acode run "simple task"`
2. Observe: Plan stage
3. Observe: Execute stage
4. Observe: Verify stage
5. Observe: Review stage
6. Verify: Task completes

### Scenario 2: Verification Failure

1. Create task that will fail verify
2. Run task
3. Observe: Verify fails
4. Observe: Executor retries
5. Verify: Up to 3 cycles

### Scenario 3: Review Rejection

1. Create task with quality issues
2. Run task
3. Observe: Review rejects
4. Observe: Re-planning occurs
5. Verify: Improved plan

### Scenario 4: Escalation

1. Create task that always fails
2. Run task
3. Observe: Retries occur
4. Observe: Escalation happens
5. Verify: Session pauses

### Scenario 5: Resume Mid-Stage

1. Start task
2. Interrupt during executor
3. Resume
4. Verify: Continues from executor

### Scenario 6: Timeout

1. Configure short timeout
2. Run slow task
3. Observe: Timeout triggers
4. Verify: Clean cancellation

### Scenario 7: Progress Tracking

1. Run multi-step task
2. Monitor progress
3. Verify: Stage updates shown
4. Verify: Step progress shown

### Scenario 8: Token Budgets

1. Configure tight budgets
2. Run complex task
3. Verify: Budgets respected
4. Verify: Truncation logged

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/
├── Orchestration/
│   ├── IOrchestrator.cs
│   ├── Orchestrator.cs
│   ├── Pipeline/
│   │   ├── IPipeline.cs
│   │   ├── Pipeline.cs
│   │   └── PipelineState.cs
│   │
│   ├── Stages/
│   │   ├── IStage.cs
│   │   ├── StageBase.cs
│   │   ├── StageResult.cs
│   │   ├── StageContext.cs
│   │   └── StageTransition.cs
│   │
│   ├── Context/
│   │   ├── IContextManager.cs
│   │   ├── ContextManager.cs
│   │   └── TokenBudget.cs
│   │
│   ├── Escalation/
│   │   ├── IEscalationPolicy.cs
│   │   ├── DefaultEscalationPolicy.cs
│   │   └── EscalationLevel.cs
│   │
│   └── Metrics/
│       ├── IStageMetrics.cs
│       └── StageMetrics.cs
```

### IOrchestrator Interface

```csharp
namespace AgenticCoder.Application.Orchestration;

public interface IOrchestrator
{
    Task<OrchestratorResult> RunAsync(
        Session session, 
        OrchestratorOptions options, 
        CancellationToken ct);
        
    Task PauseAsync(SessionId sessionId, CancellationToken ct);
    Task ResumeAsync(SessionId sessionId, CancellationToken ct);
    Task CancelAsync(SessionId sessionId, string reason, CancellationToken ct);
}

public sealed record OrchestratorOptions(
    TimeSpan PlannerTimeout = default,
    TimeSpan ExecutorTimeout = default,
    TimeSpan VerifierTimeout = default,
    TimeSpan ReviewerTimeout = default,
    int CycleLimit = 3,
    TokenBudgetOptions? TokenBudget = null);

public sealed record OrchestratorResult(
    bool Success,
    SessionState FinalState,
    StageMetrics Metrics,
    IReadOnlyList<StageResult> StageResults);
```

### IStage Interface

```csharp
namespace AgenticCoder.Application.Orchestration.Stages;

public interface IStage
{
    StageType Type { get; }
    Task<StageResult> ExecuteAsync(StageContext context, CancellationToken ct);
}

public enum StageType
{
    Planner,
    Executor,
    Verifier,
    Reviewer
}

public sealed record StageResult(
    StageStatus Status,
    object? Output,
    StageType? NextStage,
    string? Message,
    StageMetrics Metrics);

public enum StageStatus
{
    Success,
    Failed,
    Retry,
    Cycle,
    Timeout
}

public sealed record StageContext(
    Session Session,
    StageGuideDef CurrentTask,
    ConversationContext Conversation,
    TokenBudget Budget,
    IReadOnlyDictionary<string, object> StageData);
```

### Pipeline Implementation

```csharp
namespace AgenticCoder.Application.Orchestration.Pipeline;

public sealed class Pipeline : IPipeline
{
    private readonly IStage[] _stages;
    private readonly IEscalationPolicy _escalation;
    private readonly IStateManager _stateManager;
    
    public async Task<PipelineResult> ExecuteAsync(
        Session session,
        PipelineOptions options,
        CancellationToken ct)
    {
        var state = new PipelineState(session);
        
        while (!state.IsComplete)
        {
            var stage = _stages[(int)state.CurrentStage];
            var context = await BuildContextAsync(state, stage, ct);
            
            try
            {
                var result = await ExecuteStageWithTimeoutAsync(
                    stage, context, options.GetTimeout(stage.Type), ct);
                    
                await HandleResultAsync(state, result, ct);
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(state, ex, ct);
            }
        }
        
        return state.ToResult();
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-ORCH-001 | Stage execution failed |
| ACODE-ORCH-002 | Stage timeout |
| ACODE-ORCH-003 | Cycle limit reached |
| ACODE-ORCH-004 | Escalation triggered |
| ACODE-ORCH-005 | Context budget exceeded |
| ACODE-ORCH-006 | Transition failed |
| ACODE-ORCH-007 | Pipeline cancelled |

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Pipeline completed successfully |
| 1 | General pipeline failure |
| 20 | Stage timeout |
| 21 | Cycle limit reached |
| 22 | Escalation - human required |
| 23 | Pipeline cancelled |

### Logging Fields

```json
{
  "event": "stage_transition",
  "session_id": "abc123",
  "from_stage": "PLANNER",
  "to_stage": "EXECUTOR",
  "duration_ms": 12345,
  "tokens_used": 2450,
  "cycle_count": 1,
  "retry_count": 0
}
```

### Implementation Checklist

1. [ ] Create IOrchestrator interface
2. [ ] Implement Orchestrator
3. [ ] Create IStage interface
4. [ ] Implement StageBase
5. [ ] Create IPipeline interface
6. [ ] Implement Pipeline
7. [ ] Create PipelineState
8. [ ] Implement stage transitions
9. [ ] Create IContextManager
10. [ ] Implement token budgeting
11. [ ] Create IEscalationPolicy
12. [ ] Implement escalation logic
13. [ ] Add state machine integration
14. [ ] Implement metrics collection
15. [ ] Add progress reporting
16. [ ] Write unit tests
17. [ ] Write integration tests
18. [ ] Write E2E tests

### Validation Checklist Before Merge

- [ ] All 4 stages execute in order
- [ ] Stage transitions are atomic
- [ ] Checkpoints created at transitions
- [ ] Cycles work correctly
- [ ] Cycle limits enforced
- [ ] Escalation works
- [ ] Context flows appropriately
- [ ] Token budgets respected
- [ ] Timeouts work
- [ ] Crash recovery works
- [ ] Metrics collected
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** Pipeline skeleton
2. **Phase 2:** Stage lifecycle
3. **Phase 3:** Transitions + state
4. **Phase 4:** Context management
5. **Phase 5:** Escalation
6. **Phase 6:** Metrics
7. **Phase 7:** CLI integration

---

**End of Task 012 Specification**