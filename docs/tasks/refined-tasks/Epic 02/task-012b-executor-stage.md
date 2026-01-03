# Task 012.b: Executor Stage

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 34 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 012 (Multi-Stage Loop), Task 012.a (Planner), Task 014 (Tool Definitions), Task 050 (Workspace DB)  

---

## Description

Task 012.b implements the Executor stage—the workhorse of the multi-stage agent loop. While the Planner decides what to do and the Verifier checks the results, the Executor actually does the work. It takes the TaskPlan and executes each step by invoking the appropriate tools: reading files, writing code, running commands, and generating content.

The Executor operates in a loop: take the next step, prepare context, invoke the LLM to determine the exact action, execute the action via tools, record the result, and move to the next step. This loop continues until all steps complete, a failure occurs that can't be retried, or the Executor decides verification is needed.

Tool invocation is the Executor's primary capability. Each step in the plan has an action type (READ_FILE, WRITE_FILE, etc.) but the LLM determines the exact parameters. The Executor provides tools as structured function definitions that the LLM can invoke. Tool results flow back to the LLM for further reasoning.

The agentic loop within the Executor is distinct from the outer stage loop. The Executor may invoke the LLM multiple times per step—reasoning, tool call, observe result, reason again. This inner loop continues until the step completes or fails. Each inner loop iteration is a "turn" in agent parlance.

Context management is critical. The LLM needs enough context to understand what to do but not so much that it loses focus. The Executor maintains a sliding window of recent conversation, the current step details, and relevant file contents. Token budget is carefully managed.

State persistence ensures reliability. Every tool call and its result is persisted immediately. If the Executor crashes mid-step, it can resume. The step's partial progress is available. Idempotent operations can be safely retried.

Human approval gates (Task 013) integrate here. Before executing certain operations (file writes, command execution), the Executor may pause to request human approval. The approval policy is configurable—some modes auto-approve everything, others require explicit confirmation.

Error handling uses the escalation hierarchy from Task 012. Transient failures (network blips, model busy) trigger retries with exponential backoff. Persistent failures (invalid tool parameters, permission errors) may retry with adjustments or escalate. Unrecoverable failures abort the step and may pause the session.

Progress reporting keeps users informed. Each step start, tool call, and step completion is reported. In interactive mode, users see real-time updates. In JSONL mode (Task 010.b), structured events stream for machine consumption.

Sandboxing ensures safety. File operations are confined to the workspace. Terminal commands run in restricted environments. The Executor never executes arbitrary code outside designated sandboxes. Security boundaries are enforced regardless of what the LLM requests.

Tool results are typed and validated. Each tool has a defined result schema. Results are validated against the schema before acceptance. Invalid results trigger retries or errors. This ensures downstream stages receive clean data.

The Executor respects Task 001 constraints. All LLM calls go to local providers. No external APIs. Burst mode affects timeout and parallelization. Air-gapped mode affects available tools.

Testing the Executor requires simulating diverse scenarios: successful executions, transient failures, permission errors, timeout handling, and approval flows. Each scenario must produce correct, auditable behavior.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Executor Stage | Stage that executes planned steps |
| Tool | Capability the LLM can invoke |
| Tool Call | Single invocation of a tool |
| Turn | One LLM interaction cycle |
| Agentic Loop | LLM reasoning + tool use cycle |
| Step Execution | Completing one planned step |
| Context Window | Available LLM context |
| Function Calling | LLM feature for structured tools |
| Tool Result | Output from tool execution |
| Retry | Attempting failed operation again |
| Backoff | Increasing delay between retries |
| Idempotent | Same result on repeat execution |
| Sandbox | Restricted execution environment |
| Approval Gate | Human confirmation checkpoint |
| Progress Report | Status update to user |

---

## Out of Scope

The following items are explicitly excluded from Task 012.b:

- **Plan creation** - Task 012.a
- **Result verification** - Task 012.c
- **Quality review** - Task 012.d
- **Individual tool implementations** - Task 014+
- **Human approval UI** - Task 013
- **Parallel step execution** - Future enhancement
- **Cross-session tool sharing** - Single session
- **Tool plugin system** - Future epic
- **Streaming tool output** - Future enhancement
- **Tool timeout per-tool** - Unified timeout

---

## Functional Requirements

### Stage Lifecycle

- FR-001: Executor MUST implement IStage interface
- FR-002: OnEnter MUST load TaskPlan
- FR-003: Execute MUST process all steps
- FR-004: OnExit MUST finalize results
- FR-005: Lifecycle events MUST be logged

### Step Iteration

- FR-006: Steps MUST execute in dependency order
- FR-007: Independent steps MAY execute in any order
- FR-008: Failed dependency MUST block dependents
- FR-009: Step start MUST be logged
- FR-010: Step end MUST be logged

### Agentic Loop

- FR-011: Loop MUST continue until step complete
- FR-012: Loop MUST detect completion
- FR-013: Loop MUST detect failure
- FR-014: Loop MUST have iteration limit
- FR-015: Limit default: 10 turns per step
- FR-016: Limit exceeded MUST escalate

### Context Preparation

- FR-017: Current step MUST be in context
- FR-018: Relevant files MUST be in context
- FR-019: Recent conversation MUST be in context
- FR-020: Token budget MUST be respected
- FR-021: Context MUST be trimmed if needed

### Tool Definitions

- FR-022: Tools MUST be provided to LLM
- FR-023: Tools MUST have JSON schema
- FR-024: Required tools: read_file, write_file
- FR-025: Required tools: modify_file, run_terminal
- FR-026: Tools MUST match action types

### Tool Invocation

- FR-027: LLM MUST select tool via function calling
- FR-028: Tool parameters MUST be extracted
- FR-029: Parameters MUST be validated
- FR-030: Invalid parameters MUST retry
- FR-031: Tool MUST be executed
- FR-032: Result MUST be captured

### Tool Results

- FR-033: Results MUST have status (success/error)
- FR-034: Results MUST have output data
- FR-035: Results MUST be typed
- FR-036: Results MUST be validated
- FR-037: Results MUST be persisted
- FR-038: Results MUST flow back to LLM

### File Operations

- FR-039: read_file MUST return content
- FR-040: write_file MUST create/overwrite
- FR-041: modify_file MUST apply diff
- FR-042: All paths MUST be in workspace
- FR-043: Path traversal MUST be blocked

### Terminal Operations

- FR-044: run_terminal MUST execute command
- FR-045: Commands MUST be sandboxed
- FR-046: Output MUST be captured
- FR-047: Exit code MUST be captured
- FR-048: Timeout MUST be enforced

### Human Approval

- FR-049: Approval MUST be checked per policy
- FR-050: File writes MAY require approval
- FR-051: Terminal commands MAY require approval
- FR-052: Pending approval MUST pause execution
- FR-053: Approval MUST be logged

### State Persistence

- FR-054: Tool calls MUST be persisted
- FR-055: Tool results MUST be persisted
- FR-056: Step progress MUST be persisted
- FR-057: Crash recovery MUST resume step
- FR-058: Idempotent retry MUST work

### Error Handling

- FR-059: Transient errors MUST retry
- FR-060: Retry MUST use backoff
- FR-061: Persistent errors MUST escalate
- FR-062: Unrecoverable MUST abort step
- FR-063: Errors MUST be logged

### Retry Policy

- FR-064: Default retries: 3
- FR-065: Backoff: exponential 1s, 2s, 4s
- FR-066: Configurable per step type
- FR-067: Retry count MUST be tracked

### Progress Reporting

- FR-068: Step start MUST be reported
- FR-069: Tool call MUST be reported
- FR-070: Tool result MUST be reported
- FR-071: Step complete MUST be reported
- FR-072: Overall progress MUST be trackable

### Security

- FR-073: Workspace sandbox MUST be enforced
- FR-074: No arbitrary code execution
- FR-075: Path validation MUST occur
- FR-076: Command allowlist MUST be checked
- FR-077: Secrets MUST be redacted in logs
- FR-077a: Secrets in tool parameters and results (e.g., API keys, passwords, tokens) MUST be redacted or omitted from persisted state, traces, and error messages

**Secret Protection Scope:**
- Log files (FR-077)
- Tool invocation parameters (FR-077a)
- Tool execution results (FR-077a)
- Persisted task state/database records (FR-077a)
- Error messages and stack traces (FR-077a)
- Debug output and traces (FR-077a)

**Secret Detection:**
- Pattern matching for common secret formats (API keys, tokens, passwords)
- Environment variable references (e.g., `${API_KEY}`)
- Explicit secret marking in tool schemas
- Redaction: Replace with `[REDACTED]` or omit entirely

### Completion Detection

- FR-078: LLM MUST signal completion
- FR-079: Completion MUST match criteria
- FR-080: Premature completion MUST be caught
- FR-081: Completion MUST be validated

### Token Management

- FR-082: Token usage MUST be tracked
- FR-083: Budget MUST be enforced
- FR-084: Overflow MUST trigger trimming
- FR-085: Usage MUST be reported

---

## Non-Functional Requirements

### Performance

- NFR-001: Step overhead < 100ms
- NFR-002: Tool call overhead < 50ms
- NFR-003: No blocking on non-critical IO
- NFR-004: Parallel tool calls where safe

### Reliability

- NFR-005: Crash-safe at all points
- NFR-006: No lost tool results
- NFR-007: Idempotent retry guaranteed

### Throughput

- NFR-008: Handle 100+ steps per session
- NFR-009: Handle 10+ tools per step
- NFR-010: Handle large file reads

### Security

- NFR-011: Sandbox enforced always
- NFR-012: No path traversal
- NFR-013: Command allowlist enforced

### Observability

- NFR-014: All tool calls logged
- NFR-015: Token usage tracked
- NFR-016: Timing tracked per step

---

## User Manual Documentation

### Overview

The Executor stage takes the plan and makes it happen. It invokes tools to read files, write code, run commands, and complete each step of your task.

### How Execution Works

For each step in the plan:

1. **Prepare Context** - Load relevant files, step details
2. **Reason** - LLM determines exact action
3. **Invoke Tool** - Execute the action
4. **Observe Result** - Capture output
5. **Continue or Complete** - Loop until done

### Watching Execution

```bash
$ acode run "Add email validation"

[PLANNER] Created 3-task plan
[EXECUTOR] Starting execution...

  Task 1: Create EmailValidator
    Step 1.1: Reading validators...
      [tool:read_file] src/validators/*.ts
      [result] Found 3 validators
    Step 1.2: Generating code...
      [tool:generate] EmailValidator
      [result] Generated 45 lines
    Step 1.3: Writing file...
      [tool:write_file] src/validators/EmailValidator.ts
      [result] File created

  Task 2: Update form handler
    ...
```

### Tool Calls

The Executor uses these tools:

| Tool | Purpose |
|------|---------|
| read_file | Read file contents |
| write_file | Create or overwrite file |
| modify_file | Apply changes to file |
| list_directory | List directory contents |
| run_terminal | Execute shell command |
| search_code | Search codebase |

### Tool Call Examples

```
Step: Read existing validators
  [tool:read_file]
    path: src/validators/BaseValidator.ts
  [result]
    content: "export abstract class BaseValidator..."
    lines: 42
    
Step: Create new file
  [tool:write_file]
    path: src/validators/EmailValidator.ts
    content: "import { BaseValidator }..."
  [result]
    status: created
    bytes: 1234
```

### Approval Gates

Some operations require approval:

```
Step: Write EmailValidator.ts
  ⚠ Approval required for file write

  The agent wants to create:
    src/validators/EmailValidator.ts

  Preview:
    1  | import { BaseValidator } from './BaseValidator';
    2  | 
    3  | export class EmailValidator extends BaseValidator {
    ...

  [A]pprove  [D]eny  [V]iew full  [S]kip step

  Choice: a

  ✓ Approved. Writing file...
```

### Configuration

```yaml
# .agent/config.yml
executor:
  # Turns per step limit
  max_turns_per_step: 10
  
  # Retry configuration
  retry_count: 3
  retry_backoff_base_ms: 1000
  
  # Timeout per step
  step_timeout_seconds: 120
  
  # Token budget for executor
  token_budget_percent: 30
  
  # Sandbox settings
  workspace_only: true
  allowed_commands:
    - npm
    - dotnet
    - git
```

### Approval Policies

```yaml
# .agent/config.yml
approvals:
  # File operations
  file_write: prompt     # prompt | auto | deny
  file_delete: prompt
  
  # Terminal operations
  terminal_commands: prompt
  
  # In non-interactive mode
  non_interactive_policy: auto  # auto | skip | fail
```

### Progress Output

Default (interactive):
```
[EXECUTOR] Task 1, Step 2: Generating code...
  ● Reasoning...
  ● Tool call: generate_code
  ● Result: 45 lines generated
  ✓ Step complete
```

Verbose mode:
```
$ acode run "task" --verbose

[EXECUTOR] Task 1, Step 2: Generating code
  Context: 2,340 tokens loaded
  Turn 1:
    LLM: "I'll generate an email validator..."
    Tool: generate_code(template=validator, name=Email)
    Result: { lines: 45, status: success }
  Turn 2:
    LLM: "Code generated successfully. Step complete."
  Duration: 3.2s, Tokens: 890
```

JSONL mode:
```json
{"type":"step_start","task":1,"step":2,"title":"Generating code"}
{"type":"tool_call","tool":"generate_code","params":{"template":"validator"}}
{"type":"tool_result","status":"success","lines":45}
{"type":"step_complete","task":1,"step":2,"duration_ms":3200}
```

### Error Handling

When errors occur:

```
Step: Write to protected directory
  [tool:write_file]
    path: /etc/config.txt
  [error] Path outside workspace

  Retry 1/3: Adjusting path...
  [tool:write_file]
    path: config/config.txt
  [result] File created ✓
```

Escalation:
```
Step: Run failing command
  [tool:run_terminal]
    command: npm test
  [error] Tests failed (exit code 1)

  Retry 1/3...
  Retry 2/3...
  Retry 3/3...

  ⚠ Step retry limit reached
  [EXECUTOR] Escalating to stage retry

  Stage retry 1/2...
  ...

  ⚠ Stage retry limit reached
  [ORCHESTRATOR] Task aborted, session paused
```

### Troubleshooting

#### Tool Call Fails

**Problem:** Tool returns error

**Solutions:**
1. Check tool parameters
2. Verify file paths exist
3. Check permissions
4. Review error message

#### Step Loops Forever

**Problem:** Step doesn't complete

**Cause:** LLM not signaling completion

**Solutions:**
1. Check turn limit (default 10)
2. Add completion criteria
3. Break into smaller steps

#### Sandbox Violation

**Problem:** Path outside workspace

**Solution:**
All paths must be relative to workspace.
Absolute paths are rejected.

### Best Practices

1. **Keep Steps Atomic** - One clear action per step
2. **Set Appropriate Limits** - Adjust turn limits for complex steps
3. **Use Approval Gates** - For destructive operations
4. **Monitor Progress** - Watch for stalled steps
5. **Review Tool Output** - Catch issues early

---

## Acceptance Criteria

### Stage Lifecycle

- [ ] AC-001: IStage implemented
- [ ] AC-002: OnEnter loads plan
- [ ] AC-003: Execute processes steps
- [ ] AC-004: OnExit finalizes
- [ ] AC-005: Events logged

### Step Iteration

- [ ] AC-006: Dependency order works
- [ ] AC-007: Failed deps block
- [ ] AC-008: Start/end logged

### Agentic Loop

- [ ] AC-009: Continues until complete
- [ ] AC-010: Detects completion
- [ ] AC-011: Detects failure
- [ ] AC-012: Limit enforced
- [ ] AC-013: Escalation works

### Context

- [ ] AC-014: Step in context
- [ ] AC-015: Files in context
- [ ] AC-016: Budget respected
- [ ] AC-017: Trimming works

### Tools

- [ ] AC-018: Definitions provided
- [ ] AC-019: JSON schema valid
- [ ] AC-020: read_file works
- [ ] AC-021: write_file works
- [ ] AC-022: modify_file works
- [ ] AC-023: run_terminal works

### Invocation

- [ ] AC-024: LLM selects tool
- [ ] AC-025: Params extracted
- [ ] AC-026: Validation works
- [ ] AC-027: Execution works
- [ ] AC-028: Results captured

### Results

- [ ] AC-029: Status included
- [ ] AC-030: Data included
- [ ] AC-031: Typed correctly
- [ ] AC-032: Persisted
- [ ] AC-033: Flows to LLM

### File Ops

- [ ] AC-034: Read works
- [ ] AC-035: Write works
- [ ] AC-036: Modify works
- [ ] AC-037: Workspace enforced
- [ ] AC-038: Traversal blocked

### Terminal

- [ ] AC-039: Execute works
- [ ] AC-040: Sandbox works
- [ ] AC-041: Output captured
- [ ] AC-042: Exit code captured
- [ ] AC-043: Timeout works

### Approval

- [ ] AC-044: Policy checked
- [ ] AC-045: Writes prompt
- [ ] AC-046: Commands prompt
- [ ] AC-047: Pause works
- [ ] AC-048: Logged

### Persistence

- [ ] AC-049: Tool calls saved
- [ ] AC-050: Results saved
- [ ] AC-051: Progress saved
- [ ] AC-052: Crash recovery works
- [ ] AC-053: Idempotent retry

### Errors

- [ ] AC-054: Transient retry
- [ ] AC-055: Backoff works
- [ ] AC-056: Escalation works
- [ ] AC-057: Errors logged

### Progress

- [ ] AC-058: Step start reported
- [ ] AC-059: Tool calls reported
- [ ] AC-060: Completion reported

### Security

- [ ] AC-061: Sandbox enforced
- [ ] AC-062: Paths validated
- [ ] AC-063: Commands checked
- [ ] AC-064: Secrets redacted

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Orchestration/Stages/Executor/
├── ExecutorStageTests.cs
│   ├── Should_Execute_Steps_In_Order()
│   ├── Should_Handle_Dependencies()
│   ├── Should_Detect_Completion()
│   └── Should_Respect_Turn_Limit()
│
├── ToolInvocationTests.cs
│   ├── Should_Select_Correct_Tool()
│   ├── Should_Validate_Parameters()
│   ├── Should_Capture_Results()
│   └── Should_Handle_Tool_Errors()
│
├── AgenticLoopTests.cs
│   ├── Should_Loop_Until_Complete()
│   ├── Should_Limit_Iterations()
│   └── Should_Escalate_On_Limit()
│
├── SandboxTests.cs
│   ├── Should_Block_Path_Traversal()
│   ├── Should_Enforce_Workspace()
│   └── Should_Check_Command_Allowlist()
│
└── PersistenceTests.cs
    ├── Should_Persist_Tool_Calls()
    ├── Should_Persist_Results()
    └── Should_Resume_After_Crash()
```

### Integration Tests

```
Tests/Integration/Orchestration/Stages/Executor/
├── ExecutorIntegrationTests.cs
│   ├── Should_Execute_Real_Steps()
│   ├── Should_Handle_File_Operations()
│   └── Should_Handle_Terminal_Operations()
│
├── ApprovalIntegrationTests.cs
│   ├── Should_Pause_For_Approval()
│   └── Should_Resume_After_Approval()
│
└── RecoveryIntegrationTests.cs
    ├── Should_Recover_Mid_Step()
    └── Should_Retry_Idempotently()
```

### E2E Tests

```
Tests/E2E/Orchestration/Stages/Executor/
├── ExecutorE2ETests.cs
│   ├── Should_Complete_File_Task()
│   ├── Should_Complete_Multi_Step_Task()
│   └── Should_Handle_Errors_Gracefully()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Step overhead | 50ms | 100ms |
| Tool dispatch | 25ms | 50ms |
| File read | 50ms | 200ms |
| File write | 50ms | 200ms |

---

## User Verification Steps

### Scenario 1: Simple Step

1. Plan single-step task
2. Execute
3. Verify: Tool invoked
4. Verify: Result captured

### Scenario 2: Multi-Step

1. Plan multi-step task
2. Execute
3. Verify: Steps in order
4. Verify: All complete

### Scenario 3: File Read

1. Execute read step
2. Verify: Content returned
3. Verify: In context

### Scenario 4: File Write

1. Execute write step
2. Verify: File created
3. Verify: Content correct

### Scenario 5: Approval Gate

1. Enable approval
2. Execute write
3. Verify: Prompted
4. Approve
5. Verify: Continues

### Scenario 6: Path Traversal

1. Try writing ../../../etc/passwd
2. Verify: Blocked
3. Verify: Error logged

### Scenario 7: Tool Retry

1. Cause transient failure
2. Verify: Retries occur
3. Verify: Backoff applied

### Scenario 8: Turn Limit

1. Configure low limit
2. Run complex step
3. Verify: Limit reached
4. Verify: Escalated

### Scenario 9: Crash Recovery

1. Start step
2. Kill process
3. Resume
4. Verify: Continues

### Scenario 10: Progress

1. Run multi-step
2. Verify: Progress shown
3. Verify: JSONL events

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/
├── Orchestration/
│   └── Stages/
│       └── Executor/
│           ├── IExecutorStage.cs
│           ├── ExecutorStage.cs
│           ├── StepRunner.cs
│           ├── AgenticLoop.cs
│           ├── ToolDispatcher.cs
│           ├── ContextBuilder.cs
│           └── CompletionDetector.cs
│
src/AgenticCoder.Application/
├── Tools/
│   ├── ITool.cs
│   ├── ToolDefinition.cs
│   ├── ToolResult.cs
│   ├── ToolRegistry.cs
│   └── Sandbox/
│       ├── ISandbox.cs
│       ├── WorkspaceSandbox.cs
│       └── PathValidator.cs
```

### IExecutorStage Interface

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Executor;

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

### ITool Interface

```csharp
namespace AgenticCoder.Application.Tools;

public interface ITool
{
    string Name { get; }
    ToolDefinition Definition { get; }
    Task<ToolResult> ExecuteAsync(ToolParameters parameters, CancellationToken ct);
}

public sealed record ToolDefinition(
    string Name,
    string Description,
    JsonSchema ParameterSchema);

public sealed record ToolResult(
    ToolStatus Status,
    object? Output,
    string? Error);

public enum ToolStatus
{
    Success,
    Error,
    Timeout,
    Denied
}
```

### AgenticLoop Implementation

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Executor;

public sealed class AgenticLoop
{
    private readonly ILlmService _llm;
    private readonly ToolDispatcher _dispatcher;
    private readonly ILogger _logger;
    
    public async Task<LoopResult> RunAsync(
        PlannedStep step,
        StepContext context,
        int maxTurns,
        CancellationToken ct)
    {
        var turns = 0;
        var toolCalls = new List<ToolCallRecord>();
        
        while (turns < maxTurns && !ct.IsCancellationRequested)
        {
            turns++;
            
            var response = await _llm.CompleteWithToolsAsync(
                context.Messages,
                context.Tools,
                ct);
            
            if (response.IsComplete)
            {
                return LoopResult.Complete(turns, toolCalls);
            }
            
            if (response.HasToolCall)
            {
                var result = await _dispatcher.DispatchAsync(
                    response.ToolCall, ct);
                    
                toolCalls.Add(new(response.ToolCall, result));
                context.AddToolResult(result);
            }
        }
        
        return LoopResult.TurnLimitReached(turns, toolCalls);
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-EXEC-001 | Step execution failed |
| ACODE-EXEC-002 | Tool invocation failed |
| ACODE-EXEC-003 | Turn limit reached |
| ACODE-EXEC-004 | Sandbox violation |
| ACODE-EXEC-005 | Approval denied |
| ACODE-EXEC-006 | Step timeout |
| ACODE-EXEC-007 | Invalid tool parameters |

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All steps completed |
| 1 | General execution failure |
| 30 | Step failed |
| 31 | Turn limit reached |
| 32 | Sandbox violation |
| 33 | Approval denied |
| 34 | Step timeout |

### Logging Fields

```json
{
  "event": "tool_call",
  "session_id": "abc123",
  "task_id": "task_1",
  "step_id": "step_1",
  "tool": "write_file",
  "parameters": {"path": "src/file.ts"},
  "result_status": "success",
  "duration_ms": 45,
  "turn": 2,
  "tokens_used": 890
}
```

### Implementation Checklist

1. [ ] Create IExecutorStage interface
2. [ ] Implement ExecutorStage
3. [ ] Create StepRunner
4. [ ] Implement step iteration
5. [ ] Create AgenticLoop
6. [ ] Implement turn logic
7. [ ] Create ToolDispatcher
8. [ ] Implement tool routing
9. [ ] Create ITool interface
10. [ ] Implement tool registry
11. [ ] Create WorkspaceSandbox
12. [ ] Implement path validation
13. [ ] Add approval integration
14. [ ] Implement retry logic
15. [ ] Add progress reporting
16. [ ] Implement persistence
17. [ ] Write unit tests
18. [ ] Write integration tests
19. [ ] Write E2E tests

### Validation Checklist Before Merge

- [ ] Steps execute in order
- [ ] Dependencies respected
- [ ] Tool calls work
- [ ] Results captured
- [ ] Sandbox enforced
- [ ] Approvals work
- [ ] Retries work
- [ ] Crash recovery works
- [ ] Progress reported
- [ ] Secrets redacted
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** Step runner skeleton
2. **Phase 2:** Agentic loop
3. **Phase 3:** Tool dispatcher
4. **Phase 4:** File tools
5. **Phase 5:** Terminal tools
6. **Phase 6:** Sandbox
7. **Phase 7:** Approvals
8. **Phase 8:** Persistence

---

**End of Task 012.b Specification**