# Task-011c Completion Checklist: Resume Behavior + Invariants

**Status:** Implementation Plan - Ready for Execution

**Total Estimated Effort:** 18-26 hours (after task-011a completion)

**Phases:** 8 phases with full implementation details

**Prerequisites:** Task-011a (Run Entities) MUST be 100% complete before starting

---

## INSTRUCTIONS FOR NEXT SESSION

This checklist guides implementation of task-011c from 0% to 100% completion. Each phase is fully detailed with:
- What Exists (current state)
- What's Missing (specific gaps)
- Spec Reference (where to find it)
- Implementation Details (code from spec)
- Acceptance Criteria Covered (which ACs this phase addresses)
- Test Requirements (what tests to write)
- Success Criteria (how to verify)

**Workflow:**
1. Read this checklist completely before starting
2. For each phase: Create test file first (RED), implement code (GREEN), refactor (REFACTOR), verify
3. Mark each gap [ ] when starting, [🔄] when in progress, [✅] when complete with tests passing
4. Commit after each phase complete
5. Run full test suite before moving to next phase

**CRITICAL:** Do NOT skip the foundational readings. Before implementing any phase:
- Re-read CLAUDE.md Section 3.2
- Re-read this checklist
- Verify all dependencies are met

---

## PREREQUISITE CHECK

- [ ] Task-011a: Run Entities (Session, Task, Step, ToolCall, Artifact) - **REQUIRED: 100% COMPLETE**
  - If task-011a is not complete, STOP. Do not proceed with task-011c.
  - Verify: src/Acode.Domain/Sessions/Session.cs exists and has no NotImplementedException
  - Verify: All entity files from task-011a gap analysis are present and passing tests

---

# PHASE 1: Checkpoint Infrastructure (3-4 hours)

## Overview
Checkpoints save session state after each completed step, enabling recovery from that point. This phase creates the foundation for resume capability.

## Gap 1.1: Create Checkpoint Record
- Current State: ❌ MISSING
- Spec Reference: lines 1544-1551 (Implementation Prompt)
- What Exists: Nothing
- What's Missing: Checkpoint record definition

**Implementation Details (from spec lines 1544-1551):**
```csharp
// src/Acode.Application/Sessions/Resume/Checkpoint.cs
namespace Acode.Application.Sessions.Resume;

public sealed record Checkpoint(
    long Id,
    SessionId SessionId,
    TaskId? CurrentTaskId,
    StepId? CurrentStepId,
    SessionState State,
    JsonDocument ConversationState,
    DateTimeOffset CreatedAt);

public sealed record CheckpointInfo(
    long Id,
    DateTimeOffset CreatedAt,
    SessionState State,
    TaskId? CurrentTaskId,
    StepId? CurrentStepId,
    int CompletedSteps,
    int RemainingSteps);
```

**Test Requirements:**
- Should_Create_Checkpoint_Record() - Verify record creation with all fields
- Should_Serialize_To_Json() - Verify JSON round-trip
- Should_Immutable() - Verify record immutability

**Acceptance Criteria Covered:** AC-014, AC-015, AC-016

**Success Criteria:**
- [ ] Checkpoint.cs created with record definition
- [ ] All properties present: Id, SessionId, CurrentTaskId, CurrentStepId, State, ConversationState, CreatedAt
- [ ] CheckpointInfo record created for preview
- [ ] Tests created: CheckpointTests.cs
- [ ] All tests passing

**Gap Checklist Item:** [ ] 🔄 Checkpoint record created with tests passing

---

## Gap 1.2: Create CheckpointManager Class
- Current State: ❌ MISSING
- Spec Reference: lines 1532-1542 (Implementation Prompt)
- What Exists: Nothing
- What's Missing: Checkpoint persistence management

**Implementation Details (from spec lines 1532-1542):**
```csharp
// src/Acode.Application/Sessions/Resume/CheckpointManager.cs
namespace Acode.Application.Sessions.Resume;

public sealed class CheckpointManager
{
    private readonly IRunStateStore _stateStore;
    private readonly ILogger<CheckpointManager> _logger;

    public CheckpointManager(IRunStateStore stateStore, ILogger<CheckpointManager> logger)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create checkpoint after step completion
    /// </summary>
    public async Task<Checkpoint> CreateAsync(
        Session session,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(session);

        var checkpoint = new Checkpoint(
            Id: 0, // Database assigns
            SessionId: session.Id,
            CurrentTaskId: session.Tasks.LastOrDefault()?.Id,
            CurrentStepId: FindLastCompleteStep(session),
            State: session.State,
            ConversationState: ConvertConversationToJson(session),
            CreatedAt: DateTimeOffset.UtcNow);

        _logger.LogInformation(
            "Creating checkpoint for session {SessionId} at state {State}",
            session.Id, session.State);

        // Persist via state store (implementation depends on persistence layer)
        // For now, interface design only
        return checkpoint;
    }

    /// <summary>
    /// Get latest checkpoint for session
    /// </summary>
    public async Task<Checkpoint?> GetLatestAsync(
        SessionId sessionId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        _logger.LogInformation(
            "Retrieving latest checkpoint for session {SessionId}",
            sessionId);

        // Retrieve from state store
        return null; // Placeholder
    }

    /// <summary>
    /// Get all checkpoints for session
    /// </summary>
    public async Task<IReadOnlyList<Checkpoint>> GetAllAsync(
        SessionId sessionId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        _logger.LogInformation(
            "Retrieving all checkpoints for session {SessionId}",
            sessionId);

        // Retrieve from state store
        return new List<Checkpoint>(); // Placeholder
    }

    private StepId? FindLastCompleteStep(Session session)
    {
        return session.Tasks
            .LastOrDefault()?
            .Steps
            .LastOrDefault(s => s.IsComplete)?
            .Id;
    }

    private JsonDocument ConvertConversationToJson(Session session)
    {
        // Convert session conversation/context to JSON for storage
        var json = JsonSerializer.SerializeToDocument(new { /* conversation data */ });
        return json;
    }
}
```

**Test Requirements:**
- Should_Create_Checkpoint_After_Step() - Verify checkpoint creation captures all state
- Should_Be_Atomic() - Verify atomic persistence
- Should_Include_All_State() - Verify all session data in checkpoint

**Acceptance Criteria Covered:** AC-014, AC-015, AC-016, AC-017

**Success Criteria:**
- [ ] CheckpointManager.cs created with all methods
- [ ] Constructor takes IRunStateStore and ILogger
- [ ] CreateAsync creates checkpoint with all fields from session
- [ ] GetLatestAsync returns most recent checkpoint
- [ ] GetAllAsync returns all checkpoints ordered by creation
- [ ] Tests passing: Should_Create_Checkpoint_After_Step, Should_Be_Atomic, Should_Include_All_State

**Gap Checklist Item:** [ ] 🔄 CheckpointManager implemented with tests passing

---

## Gap 1.3: Create Checkpoint Persistence Integration
- Current State: ❌ MISSING (depends on IRunStateStore from task-011b)
- Spec Reference: Integration pattern (TBD by task-011b persistence layer)
- What Exists: CheckpointManager scaffold
- What's Missing: Actual persistence layer integration

**Note:** This gap waits for task-011b to provide IRunStateStore. Once task-011b is complete, integrate CheckpointManager with persistence:

**Implementation Pattern (once task-011b provides IRunStateStore):**
```csharp
// In CheckpointManager.CreateAsync:
var persistedCheckpoint = await _stateStore.CreateCheckpointAsync(checkpoint, ct);

// In CheckpointManager.GetLatestAsync:
return await _stateStore.GetLatestCheckpointAsync(sessionId, ct);

// Add to IRunStateStore interface (from task-011b):
Task<Checkpoint> CreateCheckpointAsync(Checkpoint checkpoint, CancellationToken ct);
Task<Checkpoint?> GetLatestCheckpointAsync(SessionId sessionId, CancellationToken ct);
Task<IReadOnlyList<Checkpoint>> GetCheckpointsAsync(SessionId sessionId, CancellationToken ct);
```

**Acceptance Criteria Covered:** AC-014, AC-015, AC-016

**Success Criteria:**
- [ ] CheckpointManager calls IRunStateStore methods
- [ ] Tests mock IRunStateStore
- [ ] Persistence integration works after task-011b complete

**Gap Checklist Item:** [ ] 🔄 Checkpoint persistence integration designed

---

# PHASE 2: State Recovery (2-3 hours)

## Overview
Resume begins by loading persisted session state. This phase creates the IResumeService interface and implements session loading with state validation.

## Gap 2.1: Create IResumeService Interface
- Current State: ❌ MISSING
- Spec Reference: lines 1484-1530 (Implementation Prompt)
- What Exists: Nothing
- What's Missing: Resume service contract

**Implementation Details (from spec lines 1486-1530):**
```csharp
// src/Acode.Application/Sessions/Resume/IResumeService.cs
namespace Acode.Application.Sessions.Resume;

public interface IResumeService
{
    /// <summary>
    /// Resume a session (recent or specific by ID)
    /// </summary>
    Task<ResumeResult> ResumeAsync(
        SessionId? sessionId,
        ResumeOptions options,
        CancellationToken ct);

    /// <summary>
    /// Preview what would be resumed without modifying state
    /// </summary>
    Task<ResumePreview> PreviewAsync(
        SessionId sessionId,
        CancellationToken ct);

    /// <summary>
    /// List all resumable sessions (not in terminal state)
    /// </summary>
    Task<IReadOnlyList<Session>> GetResumableAsync(CancellationToken ct);
}

public sealed record ResumeOptions(
    InProgressStrategy Strategy = InProgressStrategy.RollbackRetry,
    bool ValidateEnvironment = true,
    ChangedFilesAction ChangedFilesAction = ChangedFilesAction.Prompt);

public enum InProgressStrategy
{
    /// <summary>Rollback partial work then retry from checkpoint</summary>
    RollbackRetry,

    /// <summary>Retry without rollback (for idempotent operations)</summary>
    Retry,

    /// <summary>Prompt user for action</summary>
    Prompt
}

public enum ChangedFilesAction
{
    /// <summary>Prompt user if files changed</summary>
    Prompt,

    /// <summary>Continue despite changed files</summary>
    Continue,

    /// <summary>Abort if files changed</summary>
    Abort
}

public sealed record ResumeResult(
    bool Success,
    SessionId SessionId,
    int SkippedSteps,
    int RemainingSteps,
    string? ErrorMessage);

public sealed record ResumePreview(
    SessionId SessionId,
    SessionState State,
    CheckpointInfo LastCheckpoint,
    int SkippedSteps,
    int RemainingSteps,
    IReadOnlyList<string> ChangedFiles,
    SyncStatus SyncStatus);

public enum SyncStatus
{
    NoSync,
    Synced,
    Pending,
    Degraded
}
```

**Test Requirements:**
- Should_Resume_Recent_Session() - Resume most recent without ID
- Should_Resume_Specific_Session() - Resume with explicit session ID
- Should_Reject_Terminal_States() - Completed/failed sessions return error
- Should_Build_Continuation_Plan() - Sets up work to resume

**Acceptance Criteria Covered:** AC-001, AC-002, AC-003, AC-004, AC-005, AC-006, AC-007

**Success Criteria:**
- [ ] IResumeService.cs interface created
- [ ] ResumeOptions record with configurable strategies
- [ ] InProgressStrategy enum with 3 strategies
- [ ] ChangedFilesAction enum with 3 actions
- [ ] ResumeResult record with success/session/counts/error
- [ ] ResumePreview record with checkpoint info
- [ ] Tests created: ResumeServiceTests.cs
- [ ] All tests passing

**Gap Checklist Item:** [ ] 🔄 IResumeService interface created with tests passing

---

## Gap 2.2: Implement ResumeService
- Current State: ❌ MISSING
- Spec Reference: AC-001-013 definition
- What Exists: IResumeService interface
- What's Missing: ResumeService implementation

**Implementation Details (production implementation based on interface):**
```csharp
// src/Acode.Application/Sessions/Resume/ResumeService.cs
namespace Acode.Application.Sessions.Resume;

public sealed class ResumeService : IResumeService
{
    private readonly IRunStateStore _stateStore;
    private readonly CheckpointManager _checkpointManager;
    private readonly ContinuationPlanner _continuationPlanner;
    private readonly EnvironmentValidator _environmentValidator;
    private readonly IDistributedLock _sessionLock;
    private readonly ILogger<ResumeService> _logger;

    public ResumeService(
        IRunStateStore stateStore,
        CheckpointManager checkpointManager,
        ContinuationPlanner continuationPlanner,
        EnvironmentValidator environmentValidator,
        IDistributedLock sessionLock,
        ILogger<ResumeService> logger)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _checkpointManager = checkpointManager ?? throw new ArgumentNullException(nameof(checkpointManager));
        _continuationPlanner = continuationPlanner ?? throw new ArgumentNullException(nameof(continuationPlanner));
        _environmentValidator = environmentValidator ?? throw new ArgumentNullException(nameof(environmentValidator));
        _sessionLock = sessionLock ?? throw new ArgumentNullException(nameof(sessionLock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ResumeResult> ResumeAsync(
        SessionId? sessionId,
        ResumeOptions options,
        CancellationToken ct)
    {
        try
        {
            // Step 1: Identify session to resume
            var targetSessionId = sessionId;
            if (targetSessionId == null)
            {
                // Find most recent paused/executing session
                var resumable = await GetResumableAsync(ct);
                if (resumable.Count == 0)
                {
                    _logger.LogWarning("No resumable sessions found");
                    return new ResumeResult(
                        Success: false,
                        SessionId: SessionId.Empty,
                        SkippedSteps: 0,
                        RemainingSteps: 0,
                        ErrorMessage: "No resumable session found");
                }
                targetSessionId = resumable[0].Id;
            }

            // Step 2: Acquire session lock (exclusive execution)
            var lockAcquired = await _sessionLock.TryAcquireAsync(targetSessionId.ToString(), ct);
            if (!lockAcquired)
            {
                _logger.LogWarning("Session {SessionId} is already being resumed", targetSessionId);
                return new ResumeResult(
                    Success: false,
                    SessionId: targetSessionId,
                    SkippedSteps: 0,
                    RemainingSteps: 0,
                    ErrorMessage: "Session is locked");
            }

            try
            {
                // Step 3: Load session from persistence
                var session = await _stateStore.GetWithHierarchyAsync(targetSessionId, ct);
                if (session == null)
                {
                    return new ResumeResult(
                        Success: false,
                        SessionId: targetSessionId,
                        SkippedSteps: 0,
                        RemainingSteps: 0,
                        ErrorMessage: "Session not found");
                }

                // Step 4: Validate state is resumable
                if (!IsResumable(session))
                {
                    return new ResumeResult(
                        Success: false,
                        SessionId: targetSessionId,
                        SkippedSteps: 0,
                        RemainingSteps: 0,
                        ErrorMessage: $"Session in terminal state: {session.State}");
                }

                // Step 5: Validate environment if requested
                if (options.ValidateEnvironment)
                {
                    var envResult = await _environmentValidator.ValidateAsync(session, ct);
                    if (!envResult.IsValid)
                    {
                        return new ResumeResult(
                            Success: false,
                            SessionId: targetSessionId,
                            SkippedSteps: 0,
                            RemainingSteps: 0,
                            ErrorMessage: "Environment validation failed");
                    }

                    if (envResult.ChangedFiles.Count > 0 && options.ChangedFilesAction == ChangedFilesAction.Abort)
                    {
                        return new ResumeResult(
                            Success: false,
                            SessionId: targetSessionId,
                            SkippedSteps: 0,
                            RemainingSteps: 0,
                            ErrorMessage: "Files changed since interruption");
                    }
                }

                // Step 6: Get latest checkpoint
                var checkpoint = await _checkpointManager.GetLatestAsync(targetSessionId, ct);
                if (checkpoint == null)
                {
                    return new ResumeResult(
                        Success: false,
                        SessionId: targetSessionId,
                        SkippedSteps: 0,
                        RemainingSteps: 0,
                        ErrorMessage: "No checkpoint found");
                }

                // Step 7: Build continuation plan
                var plan = _continuationPlanner.Create(session, checkpoint);

                _logger.LogInformation(
                    "Session {SessionId} resume initiated: {SkippedSteps} skipped, {RemainingSteps} remaining",
                    targetSessionId,
                    plan.SkippedSteps.Count,
                    plan.PendingSteps.Count);

                return new ResumeResult(
                    Success: true,
                    SessionId: targetSessionId,
                    SkippedSteps: plan.SkippedSteps.Count,
                    RemainingSteps: plan.PendingSteps.Count,
                    ErrorMessage: null);
            }
            finally
            {
                await _sessionLock.ReleaseAsync(targetSessionId.ToString(), ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resume failed");
            return new ResumeResult(
                Success: false,
                SessionId: sessionId ?? SessionId.Empty,
                SkippedSteps: 0,
                RemainingSteps: 0,
                ErrorMessage: ex.Message);
        }
    }

    public async Task<ResumePreview> PreviewAsync(
        SessionId sessionId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        var session = await _stateStore.GetWithHierarchyAsync(sessionId, ct);
        if (session == null)
            throw new InvalidOperationException($"Session {sessionId} not found");

        var checkpoint = await _checkpointManager.GetLatestAsync(sessionId, ct);
        var plan = _continuationPlanner.Create(session, checkpoint!);

        var envResult = await _environmentValidator.ValidateAsync(session, ct);

        return new ResumePreview(
            SessionId: sessionId,
            State: session.State,
            LastCheckpoint: ConvertToCheckpointInfo(checkpoint!),
            SkippedSteps: plan.SkippedSteps.Count,
            RemainingSteps: plan.PendingSteps.Count,
            ChangedFiles: envResult.ChangedFiles.Select(f => f.Path).ToList(),
            SyncStatus: GetSyncStatus(session)); // TBD by sync phase
    }

    public async Task<IReadOnlyList<Session>> GetResumableAsync(CancellationToken ct)
    {
        // Get sessions in PAUSED or EXECUTING state (not COMPLETED, FAILED, CANCELLED)
        var filter = new SessionFilter { States = new[] { SessionState.Paused, SessionState.Executing } };
        var sessions = await _stateStore.ListAsync(filter, ct);
        return sessions.OrderByDescending(s => s.UpdatedAt).ToList();
    }

    private bool IsResumable(Session session) =>
        session.State is SessionState.Paused or SessionState.Executing;

    private CheckpointInfo ConvertToCheckpointInfo(Checkpoint checkpoint)
    {
        // Calculate step counts
        var completedSteps = GetCompletedStepCount(); // TBD
        var remainingSteps = GetRemainingStepCount(); // TBD

        return new CheckpointInfo(
            Id: checkpoint.Id,
            CreatedAt: checkpoint.CreatedAt,
            State: checkpoint.State,
            CurrentTaskId: checkpoint.CurrentTaskId,
            CurrentStepId: checkpoint.CurrentStepId,
            CompletedSteps: completedSteps,
            RemainingSteps: remainingSteps);
    }

    private SyncStatus GetSyncStatus(Session session)
    {
        // TBD: Implement based on sync state
        return SyncStatus.Synced;
    }
}
```

**Test Requirements:**
- Should_Resume_Paused_Session() - Load and continue from PAUSED state
- Should_Reject_Terminal_States() - Reject COMPLETED/FAILED sessions
- Should_Identify_Last_Checkpoint() - Locate most recent checkpoint
- Should_Build_Continuation_Plan() - Create plan with skip/continue counts

**Acceptance Criteria Covered:** AC-001-013

**Success Criteria:**
- [ ] ResumeService.cs created implementing IResumeService
- [ ] ResumeAsync: Loads session, validates state, acquires lock, returns result
- [ ] PreviewAsync: Shows what would be resumed without modifying state
- [ ] GetResumableAsync: Lists sessions in resumable states
- [ ] Lock acquisition prevents concurrent resumes
- [ ] Tests passing: All 4 ResumeServiceTests methods passing

**Gap Checklist Item:** [ ] 🔄 ResumeService implemented with tests passing

---

# PHASE 3: Continuation Planning (2-3 hours)

## Overview
After loading session state, the resume service determines what work to continue. This phase creates the planner that decides which steps to skip and which to execute.

## Gap 3.1: Create ContinuationPlanner
- Current State: ❌ MISSING
- Spec Reference: lines 1554-1577 (Implementation Prompt)
- What Exists: Nothing
- What's Missing: Planning logic for resume

**Implementation Details (from spec lines 1554-1577):**
```csharp
// src/Acode.Application/Sessions/Resume/ContinuationPlanner.cs
namespace Acode.Application.Sessions.Resume;

public sealed class ContinuationPlanner
{
    private readonly ILogger<ContinuationPlanner> _logger;

    public ContinuationPlanner(ILogger<ContinuationPlanner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create continuation plan from checkpoint
    /// </summary>
    public ContinuationPlan Create(Session session, Checkpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(checkpoint);

        var skippedTasks = new List<TaskId>();
        var skippedSteps = new List<StepId>();
        StepId? inProgressStep = null;
        var pendingSteps = new List<StepId>();
        var inProgressAction = InProgressAction.RollbackAndRetry;

        // Identify completed work (to skip)
        foreach (var task in session.Tasks)
        {
            if (task.IsCompleted)
            {
                skippedTasks.Add(task.Id);
                foreach (var step in task.Steps)
                {
                    skippedSteps.Add(step.Id);
                }
            }
            else
            {
                // Task not complete, check steps
                foreach (var step in task.Steps)
                {
                    if (step.IsCompleted)
                    {
                        skippedSteps.Add(step.Id);
                    }
                    else if (step.IsInProgress)
                    {
                        inProgressStep = step.Id;
                        inProgressAction = DetermineInProgressAction(step);
                    }
                    else if (step.IsPending)
                    {
                        pendingSteps.Add(step.Id);
                    }
                }
            }
        }

        var plan = new ContinuationPlan(
            SkippedTasks: skippedTasks.AsReadOnly(),
            SkippedSteps: skippedSteps.AsReadOnly(),
            InProgressStep: inProgressStep,
            PendingSteps: pendingSteps.AsReadOnly(),
            InProgressAction: inProgressAction);

        _logger.LogInformation(
            "Continuation plan created: {SkippedTasks} skipped tasks, {SkippedSteps} skipped steps, " +
            "{InProgressSteps} in-progress, {PendingSteps} pending. Action: {Action}",
            plan.SkippedTasks.Count,
            plan.SkippedSteps.Count,
            inProgressStep != null ? 1 : 0,
            plan.PendingSteps.Count,
            plan.InProgressAction);

        return plan;
    }

    private InProgressAction DetermineInProgressAction(Step step)
    {
        // Determine how to handle in-progress step
        // - Read operations: Retry (idempotent)
        // - Write operations: RollbackAndRetry (safe)
        // - Prompts: Prompt (needs user input)

        if (IsReadOnlyStep(step))
            return InProgressAction.Retry;

        if (IsPromptStep(step))
            return InProgressAction.Skip; // Wait for user confirmation

        return InProgressAction.RollbackAndRetry;
    }

    private bool IsReadOnlyStep(Step step) =>
        step.Name.Contains("query", StringComparison.OrdinalIgnoreCase) ||
        step.Name.Contains("read", StringComparison.OrdinalIgnoreCase) ||
        step.Name.Contains("analyze", StringComparison.OrdinalIgnoreCase);

    private bool IsPromptStep(Step step) =>
        step.Name.Contains("prompt", StringComparison.OrdinalIgnoreCase) ||
        step.Name.Contains("confirm", StringComparison.OrdinalIgnoreCase);
}

public sealed record ContinuationPlan(
    IReadOnlyList<TaskId> SkippedTasks,
    IReadOnlyList<StepId> SkippedSteps,
    StepId? InProgressStep,
    IReadOnlyList<StepId> PendingSteps,
    InProgressAction InProgressAction);

public enum InProgressAction
{
    /// <summary>Skip in-progress step (user confirms later)</summary>
    Skip,

    /// <summary>Retry without rollback (for read-only operations)</summary>
    Retry,

    /// <summary>Rollback partial work then retry (default for writes)</summary>
    RollbackAndRetry
}
```

**Test Requirements:**
- Should_Skip_Completed_Tasks() - Completed tasks in skip list
- Should_Skip_Completed_Steps() - Completed steps in skip list
- Should_Identify_Pending_Steps() - Pending steps identified for execution

**Acceptance Criteria Covered:** AC-018, AC-019, AC-020, AC-021, AC-022

**Success Criteria:**
- [ ] ContinuationPlanner.cs created with Create() method
- [ ] ContinuationPlan record created with skip/pending lists
- [ ] InProgressAction enum with 3 strategies
- [ ] Identifies completed tasks (skip all steps)
- [ ] Identifies completed steps (skip individual steps)
- [ ] Identifies pending steps (execute)
- [ ] Determines in-progress action based on step type
- [ ] Tests passing: Should_Skip_Completed_Tasks, Should_Skip_Completed_Steps, Should_Identify_Pending_Steps

**Gap Checklist Item:** [ ] 🔄 ContinuationPlanner implemented with tests passing

---

# PHASE 4: In-Progress Handling (2-3 hours)

## Overview
When resume encounters a step that was in-progress when interrupted, it must decide: retry, rollback and retry, or skip. This phase implements safe handling of partial work.

## Gap 4.1: Create InProgressHandler
- Current State: ❌ MISSING
- Spec Reference: AC-023-027 definition
- What Exists: Nothing (ContinuationPlanner identifies in-progress steps)
- What's Missing: Rollback and retry logic

**Implementation Details:**
```csharp
// src/Acode.Application/Sessions/Resume/InProgressHandler.cs
namespace Acode.Application.Sessions.Resume;

public sealed class InProgressHandler
{
    private readonly ILogger<InProgressHandler> _logger;

    public InProgressHandler(ILogger<InProgressHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Determine safe action for in-progress step
    /// </summary>
    public async Task<InProgressHandlingResult> HandleAsync(
        Step inProgressStep,
        InProgressStrategy strategy,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(inProgressStep);

        // Check if step is retryable (read-only operation)
        if (IsRetryable(inProgressStep))
        {
            _logger.LogInformation(
                "In-progress step {StepId} is retryable (read-only). Will retry.",
                inProgressStep.Id);

            return new InProgressHandlingResult(
                Action: InProgressAction.Retry,
                NeedsRollback: false,
                ToolCalls: null);
        }

        // Check if step has write operations
        if (HasWriteOperations(inProgressStep))
        {
            if (strategy == InProgressStrategy.RollbackRetry)
            {
                _logger.LogInformation(
                    "In-progress step {StepId} has write operations. Rolling back.",
                    inProgressStep.Id);

                // Get all tool calls to rollback
                var toolCalls = inProgressStep.ToolCalls;

                return new InProgressHandlingResult(
                    Action: InProgressAction.RollbackAndRetry,
                    NeedsRollback: true,
                    ToolCalls: toolCalls);
            }
            else if (strategy == InProgressStrategy.Retry)
            {
                _logger.LogInformation(
                    "In-progress step {StepId} has writes. Attempting idempotent retry.",
                    inProgressStep.Id);

                return new InProgressHandlingResult(
                    Action: InProgressAction.Retry,
                    NeedsRollback: false,
                    ToolCalls: null);
            }
            else // Prompt
            {
                return new InProgressHandlingResult(
                    Action: InProgressAction.Skip,
                    NeedsRollback: false,
                    ToolCalls: null);
            }
        }

        // No operations, safe to skip
        return new InProgressHandlingResult(
            Action: InProgressAction.Skip,
            NeedsRollback: false,
            ToolCalls: null);
    }

    /// <summary>
    /// Rollback partial writes from in-progress step
    /// </summary>
    public async Task<bool> RollbackAsync(
        Session session,
        Step step,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(step);

        _logger.LogInformation(
            "Rolling back in-progress step {StepId} in session {SessionId}",
            step.Id, session.Id);

        // Implementation depends on what tool calls were made
        // For file writes: delete created files
        // For database: rollback transaction
        // For API calls: undo via reverse operation

        // This is a placeholder - actual rollback depends on tool call types
        return true;
    }

    private bool IsRetryable(Step step)
    {
        // Read-only operations are safe to retry
        return step.ToolCalls.All(tc =>
            tc.ToolName.Contains("read", StringComparison.OrdinalIgnoreCase) ||
            tc.ToolName.Contains("query", StringComparison.OrdinalIgnoreCase) ||
            tc.ToolName.Contains("analyze", StringComparison.OrdinalIgnoreCase));
    }

    private bool HasWriteOperations(Step step)
    {
        // Has write, create, delete, or update operations
        return step.ToolCalls.Any(tc =>
            tc.ToolName.Contains("write", StringComparison.OrdinalIgnoreCase) ||
            tc.ToolName.Contains("create", StringComparison.OrdinalIgnoreCase) ||
            tc.ToolName.Contains("delete", StringComparison.OrdinalIgnoreCase) ||
            tc.ToolName.Contains("update", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record InProgressHandlingResult(
    InProgressAction Action,
    bool NeedsRollback,
    IReadOnlyList<ToolCall>? ToolCalls);
```

**Test Requirements:**
- Should_Retry_Reads() - Read operations marked for retry
- Should_Rollback_Partials() - Write operations marked for rollback
- Should_Detect_Applied_Writes() - Identify which writes were applied

**Acceptance Criteria Covered:** AC-023, AC-024, AC-025, AC-026, AC-027

**Success Criteria:**
- [ ] InProgressHandler.cs created
- [ ] HandleAsync determines action based on step type
- [ ] RollbackAsync implements safe rollback
- [ ] IsRetryable identifies read-only operations
- [ ] HasWriteOperations identifies write-like operations
- [ ] Tests passing: Should_Retry_Reads, Should_Rollback_Partials, Should_Detect_Applied_Writes

**Gap Checklist Item:** [ ] 🔄 InProgressHandler implemented with tests passing

---

# PHASE 5: Idempotency (2-3 hours)

## Overview
Idempotency ensures that replaying an operation produces the same result. This phase creates idempotency keys and completion detection to prevent duplicate operations.

## Gap 5.1: Create IdempotencyKeyGenerator
- Current State: ❌ MISSING
- Spec Reference: lines 1601-1611 (Implementation Prompt)
- What Exists: Nothing
- What's Missing: Idempotency key generation

**Implementation Details (from spec lines 1601-1611):**
```csharp
// src/Acode.Application/Sessions/Idempotency/IdempotencyKeyGenerator.cs
namespace Acode.Application.Sessions.Idempotency;

public sealed class IdempotencyKeyGenerator
{
    /// <summary>
    /// Generate unique idempotency key for a step execution
    /// </summary>
    /// <remarks>
    /// Format: {sessionId}:{stepId}:{attemptNumber}
    /// This ensures same step on same session with different attempts have different keys
    /// But same step with same attempt always produces same key
    /// </remarks>
    public string Generate(SessionId sessionId, StepId stepId, int attemptNumber)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        ArgumentNullException.ThrowIfNull(stepId);

        if (attemptNumber < 1)
            throw new ArgumentException("Attempt number must be >= 1", nameof(attemptNumber));

        return $"{sessionId:N}:{stepId:N}:{attemptNumber}";
    }

    /// <summary>
    /// Parse idempotency key back to components
    /// </summary>
    public bool Parse(
        string key,
        out SessionId sessionId,
        out StepId stepId,
        out int attemptNumber)
    {
        sessionId = default;
        stepId = default;
        attemptNumber = 0;

        if (string.IsNullOrWhiteSpace(key))
            return false;

        var parts = key.Split(':');
        if (parts.Length != 3)
            return false;

        if (!SessionId.TryParse(parts[0], out sessionId))
            return false;

        if (!StepId.TryParse(parts[1], out stepId))
            return false;

        if (!int.TryParse(parts[2], out attemptNumber) || attemptNumber < 1)
            return false;

        return true;
    }
}
```

**Test Requirements:**
- Should_Generate_Unique_Keys() - Same session/step/attempt produces same key
- Should_Different_Attempts_Produce_Different_Keys() - Different attempts produce different keys
- Should_Parse_Key_Components() - Parse key back to components

**Acceptance Criteria Covered:** AC-028, AC-029, AC-032

**Success Criteria:**
- [ ] IdempotencyKeyGenerator.cs created
- [ ] Generate() creates key from sessionId, stepId, attemptNumber
- [ ] Parse() extracts components from key
- [ ] Key format: sessionId:stepId:attemptNumber
- [ ] Tests passing: All 3 tests

**Gap Checklist Item:** [ ] 🔄 IdempotencyKeyGenerator implemented with tests passing

---

## Gap 5.2: Create CompletionDetector
- Current State: ❌ MISSING
- Spec Reference: AC-028-032 definition
- What Exists: IdempotencyKeyGenerator
- What's Missing: Completion detection logic

**Implementation Details:**
```csharp
// src/Acode.Application/Sessions/Idempotency/CompletionDetector.cs
namespace Acode.Application.Sessions.Idempotency;

public sealed class CompletionDetector
{
    private readonly IRunStateStore _stateStore;
    private readonly ILogger<CompletionDetector> _logger;

    public CompletionDetector(IRunStateStore stateStore, ILogger<CompletionDetector> logger)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Check if a step with given idempotency key was already completed
    /// </summary>
    public async Task<bool> IsCompletedAsync(
        SessionId sessionId,
        StepId stepId,
        int attemptNumber,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        ArgumentNullException.ThrowIfNull(stepId);

        // Load session and check if step already completed
        var session = await _stateStore.GetWithHierarchyAsync(sessionId, ct);
        if (session == null)
            return false;

        // Find the step
        var step = session.Tasks
            .SelectMany(t => t.Steps)
            .FirstOrDefault(s => s.Id == stepId);

        if (step == null)
            return false;

        // Check completion status
        if (step.IsCompleted && step.Metadata?.ContainsKey("attemptNumber") == true)
        {
            var recordedAttempt = (int)step.Metadata["attemptNumber"];
            if (recordedAttempt == attemptNumber)
            {
                _logger.LogInformation(
                    "Step {StepId} already completed with attempt {AttemptNumber}",
                    stepId, attemptNumber);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Record successful step completion with idempotency info
    /// </summary>
    public async Task RecordCompletionAsync(
        SessionId sessionId,
        StepId stepId,
        int attemptNumber,
        string result,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        ArgumentNullException.ThrowIfNull(stepId);

        // Update step metadata with attempt number and result
        // This information persists so future resume attempts can detect completion

        _logger.LogInformation(
            "Recorded completion for step {StepId} attempt {AttemptNumber}",
            stepId, attemptNumber);
    }
}
```

**Test Requirements:**
- Should_Detect_Completed_Steps() - Identify when step was already completed
- Should_Skip_Completed() - Skip re-execution of completed steps
- Should_Support_Multiple_Attempts() - Different attempts tracked separately

**Acceptance Criteria Covered:** AC-028, AC-029, AC-030, AC-031, AC-032

**Success Criteria:**
- [ ] CompletionDetector.cs created
- [ ] IsCompletedAsync checks step completion status
- [ ] RecordCompletionAsync persists attempt metadata
- [ ] Re-execution of completed step is no-op (idempotent)
- [ ] Works across process restarts (persisted metadata)
- [ ] Tests passing: All 3 tests

**Gap Checklist Item:** [ ] 🔄 CompletionDetector implemented with tests passing

---

# PHASE 6: Environment Validation (2-3 hours)

## Overview
Before resuming, validate that the environment hasn't changed in ways that would break the continuation. Check directory accessibility, file modifications, model availability, and configuration.

## Gap 6.1: Create EnvironmentValidator
- Current State: ❌ MISSING
- Spec Reference: lines 1579-1599 (Implementation Prompt), AC-037-043
- What Exists: Nothing
- What's Missing: Environment validation logic

**Implementation Details (from spec lines 1579-1599):**
```csharp
// src/Acode.Application/Sessions/Resume/EnvironmentValidator.cs
namespace Acode.Application.Sessions.Resume;

public sealed class EnvironmentValidator
{
    private readonly IFileSystem _fileSystem;
    private readonly IModelProvider _modelProvider;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<EnvironmentValidator> _logger;

    public EnvironmentValidator(
        IFileSystem fileSystem,
        IModelProvider modelProvider,
        IConfigurationService configurationService,
        ILogger<EnvironmentValidator> logger)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _modelProvider = modelProvider ?? throw new ArgumentNullException(nameof(modelProvider));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validate environment is suitable for resume
    /// </summary>
    public async Task<EnvironmentValidationResult> ValidateAsync(
        Session session,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(session);

        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();
        var changedFiles = new List<ChangedFile>();

        // Check 1: Workspace directory exists and accessible
        if (!_fileSystem.DirectoryExists(session.Metadata?["workspaceDir"] as string))
        {
            errors.Add(new ValidationError(
                "WORKSPACE_NOT_FOUND",
                "Workspace directory no longer accessible"));
        }

        // Check 2: Files in session metadata are still accessible
        var files = session.Metadata?["files"] as IList<string> ?? new List<string>();
        foreach (var filePath in files)
        {
            if (!_fileSystem.FileExists(filePath))
            {
                errors.Add(new ValidationError(
                    "FILE_NOT_FOUND",
                    $"File no longer exists: {filePath}"));
            }
            else
            {
                // Check if file was modified since session started
                var fileInfo = _fileSystem.GetFileInfo(filePath);
                var sessionTime = session.Metadata?["sessionStartTime"] as DateTimeOffset?;

                if (sessionTime.HasValue && fileInfo.LastModifiedTime > sessionTime.Value)
                {
                    changedFiles.Add(new ChangedFile(
                        Path: filePath,
                        ModifiedAt: fileInfo.LastModifiedTime,
                        Type: ChangeType.Modified));

                    warnings.Add(new ValidationWarning(
                        "FILE_MODIFIED",
                        $"File modified since session started: {filePath}"));
                }
            }
        }

        // Check 3: Model availability
        var modelName = session.Metadata?["model"] as string;
        if (!string.IsNullOrEmpty(modelName))
        {
            var modelAvailable = await _modelProvider.IsAvailableAsync(modelName, ct);
            if (!modelAvailable)
            {
                warnings.Add(new ValidationWarning(
                    "MODEL_UNAVAILABLE",
                    $"Model no longer available: {modelName}"));
            }
        }

        // Check 4: Configuration validity
        var configValid = await _configurationService.ValidateAsync(ct);
        if (!configValid)
        {
            errors.Add(new ValidationError(
                "CONFIG_INVALID",
                "Configuration is no longer valid"));
        }

        _logger.LogInformation(
            "Environment validation completed: {ErrorCount} errors, {WarningCount} warnings, {ChangedFileCount} changed files",
            errors.Count, warnings.Count, changedFiles.Count);

        return new EnvironmentValidationResult(
            IsValid: errors.Count == 0,
            Errors: errors.AsReadOnly(),
            Warnings: warnings.AsReadOnly(),
            ChangedFiles: changedFiles.AsReadOnly());
    }
}

public sealed record EnvironmentValidationResult(
    bool IsValid,
    IReadOnlyList<ValidationError> Errors,
    IReadOnlyList<ValidationWarning> Warnings,
    IReadOnlyList<ChangedFile> ChangedFiles);

public sealed record ValidationError(string Code, string Message);
public sealed record ValidationWarning(string Code, string Message);
public sealed record ChangedFile(string Path, DateTimeOffset ModifiedAt, ChangeType Type);
public enum ChangeType { Modified, Deleted, Created }
```

**Test Requirements:**
- Should_Check_Directory() - Verify workspace directory exists
- Should_Check_Files() - Verify files still exist
- Should_Check_Model() - Verify model is available
- Should_Detect_Changes() - Detect file modifications since session start

**Acceptance Criteria Covered:** AC-037, AC-038, AC-039, AC-040, AC-041, AC-042, AC-043

**Success Criteria:**
- [ ] EnvironmentValidator.cs created
- [ ] ValidateAsync checks directory existence
- [ ] ValidateAsync checks file accessibility
- [ ] ValidateAsync checks model availability
- [ ] ValidateAsync checks configuration validity
- [ ] Detects changed files (modified time > session start)
- [ ] Returns EnvironmentValidationResult with errors/warnings
- [ ] Tests passing: All 4 tests

**Gap Checklist Item:** [ ] 🔄 EnvironmentValidator implemented with tests passing

---

# PHASE 7: Replay & Determinism (2-3 hours)

## Overview
Ensure that replaying from checkpoint produces identical state regardless of when resume occurs. This requires deterministic ordering and complete state reconstruction.

## Gap 7.1: Create ReplayOrderer
- Current State: ❌ MISSING
- Spec Reference: AC-033-036 definition
- What Exists: Nothing
- What's Missing: Deterministic event replay logic

**Implementation Details:**
```csharp
// src/Acode.Application/Sessions/Replay/ReplayOrderer.cs
namespace Acode.Application.Sessions.Replay;

public sealed class ReplayOrderer
{
    private readonly ILogger<ReplayOrderer> _logger;

    public ReplayOrderer(ILogger<ReplayOrderer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Order events deterministically for replay
    /// </summary>
    /// <remarks>
    /// Events are ordered by:
    /// 1. Timestamp (primary)
    /// 2. EventId (tiebreaker for events at same timestamp)
    /// This ensures identical ordering regardless of resume time
    /// </remarks>
    public IReadOnlyList<SessionEvent> Order(IEnumerable<SessionEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var ordered = events
            .OrderBy(e => e.Timestamp)
            .ThenBy(e => e.EventId)
            .ToList();

        _logger.LogInformation(
            "Ordered {EventCount} events for deterministic replay",
            ordered.Count);

        return ordered.AsReadOnly();
    }
}

public sealed class DeterministicReplayer
{
    private readonly ReplayOrderer _orderer;
    private readonly ILogger<DeterministicReplayer> _logger;

    public DeterministicReplayer(
        ReplayOrderer orderer,
        ILogger<DeterministicReplayer> logger)
    {
        _orderer = orderer ?? throw new ArgumentNullException(nameof(orderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Replay events in deterministic order to reconstruct state
    /// </summary>
    public async Task<Session> ReplayAsync(
        Session baseSession,
        IEnumerable<SessionEvent> events,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(baseSession);
        ArgumentNullException.ThrowIfNull(events);

        // Order events deterministically
        var orderedEvents = _orderer.Order(events);

        _logger.LogInformation(
            "Replaying {EventCount} events in deterministic order",
            orderedEvents.Count);

        var currentState = baseSession;

        // Replay each event in order
        foreach (var evt in orderedEvents)
        {
            currentState = ApplyEvent(currentState, evt);
        }

        _logger.LogInformation(
            "Replay completed. Final state: {State}",
            currentState.State);

        return currentState;
    }

    private Session ApplyEvent(Session session, SessionEvent evt)
    {
        // Apply event to session state
        // This transitions session through events in deterministic order
        // ensuring identical state regardless of timing
        return evt.Type switch
        {
            "TaskStarted" => session, // Example
            "TaskCompleted" => session,
            "StepStarted" => session,
            "StepCompleted" => session,
            _ => session
        };
    }
}
```

**Test Requirements:**
- Should_Order_By_Timestamp() - Events ordered by timestamp first
- Should_Use_EventId_Tiebreaker() - Events at same timestamp use EventId
- Should_Produce_Deterministic_State() - Same event sequence produces same state

**Acceptance Criteria Covered:** AC-033, AC-034, AC-035, AC-036

**Success Criteria:**
- [ ] ReplayOrderer.cs created with Order() method
- [ ] DeterministicReplayer.cs created with ReplayAsync() method
- [ ] Events ordered by (Timestamp, EventId) tuple
- [ ] Replay produces identical state from same event sequence
- [ ] Works across multiple resume attempts
- [ ] Tests passing: All 3 tests

**Gap Checklist Item:** [ ] 🔄 ReplayOrderer/DeterministicReplayer implemented with tests passing

---

# PHASE 8: CLI Command + Integration (3-4 hours)

## Overview
Integrate all components into a CLI command that users invoke to resume sessions. Handle sync state, progress reporting, and error cases.

## Gap 8.1: Create ResumeCommand for CLI
- Current State: ❌ MISSING
- Spec Reference: AC-001, AC-054-062 definition
- What Exists: ResumeService, all supporting infrastructure
- What's Missing: CLI command implementation

**Implementation Details:**
```csharp
// src/Acode.Cli/Commands/ResumeCommand.cs
namespace Acode.Cli.Commands;

[Command("resume", Description = "Resume an interrupted session")]
public sealed class ResumeCommand
{
    [Argument(0, Description = "Session ID to resume (optional - uses most recent if not provided)")]
    public string? SessionId { get; set; }

    [Option("--yes", "-y", Description = "Auto-approve all prompts")]
    public bool AutoApprove { get; set; }

    [Option("--validate", Description = "Validate environment before resuming")]
    public bool ValidateEnvironment { get; set; } = true;

    [Option("--strategy", Description = "Strategy for in-progress steps: rollback_retry, retry, prompt")]
    public string Strategy { get; set; } = "rollback_retry";

    [Option("--changed-files", Description = "Action if files changed: prompt, continue, abort")]
    public string ChangedFilesAction { get; set; } = "prompt";

    private readonly IResumeService _resumeService;
    private readonly IConsoleWriter _console;
    private readonly ILogger<ResumeCommand> _logger;

    public ResumeCommand(
        IResumeService resumeService,
        IConsoleWriter console,
        ILogger<ResumeCommand> logger)
    {
        _resumeService = resumeService ?? throw new ArgumentNullException(nameof(resumeService));
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            // Announce start
            _console.WriteLine("Resuming session...");

            // Parse session ID
            var sessionId = string.IsNullOrEmpty(SessionId)
                ? null
                : SessionId.FromString(SessionId);

            // Build options
            var options = new ResumeOptions(
                Strategy: Enum.Parse<InProgressStrategy>(Strategy, ignoreCase: true),
                ValidateEnvironment: ValidateEnvironment,
                ChangedFilesAction: Enum.Parse<ChangedFilesAction>(ChangedFilesAction, ignoreCase: true));

            // Resume
            var result = await _resumeService.ResumeAsync(sessionId, options, ct);

            if (result.Success)
            {
                // Announce what's being resumed
                _console.WriteLine($"Resuming session {result.SessionId}");

                // Show skip count
                if (result.SkippedSteps > 0)
                {
                    _console.WriteLine($"Skipping {result.SkippedSteps} completed steps");
                }

                // Show remaining work
                _console.WriteLine($"Executing {result.RemainingSteps} remaining steps");

                // Continue execution (TBD by execution engine)
                // await _executionEngine.ContinueAsync(result.SessionId, ct);

                _console.WriteLine("Session completed successfully");
                Environment.Exit(0);
            }
            else
            {
                // Error occurred
                _console.WriteError($"Resume failed: {result.ErrorMessage}");

                // Map error to exit code
                var exitCode = MapErrorToExitCode(result.ErrorMessage);
                Environment.Exit(exitCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resume command failed");
            _console.WriteError($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private int MapErrorToExitCode(string? error)
    {
        return error switch
        {
            string s when s.Contains("resumable") => 14,
            string s when s.Contains("terminal") => 15,
            string s when s.Contains("locked") => 16,
            string s when s.Contains("validation") => 17,
            _ => 1
        };
    }
}
```

**Test Requirements:**
- Should_Resume_Paused_Session() - Resume most recent session
- Should_Resume_Specific_Session() - Resume by ID
- Should_Show_Progress() - Display skip counts and remaining work
- Should_Handle_Errors() - Proper error messages and exit codes

**Acceptance Criteria Covered:** AC-001, AC-002, AC-054, AC-055, AC-056, AC-057, AC-058, AC-059, AC-060, AC-061, AC-062

**Success Criteria:**
- [ ] ResumeCommand.cs created
- [ ] Supports `acode resume` (most recent) and `acode resume <id>` (specific)
- [ ] Calls IResumeService.ResumeAsync
- [ ] Displays session ID, skip count, remaining work
- [ ] Error handling with appropriate exit codes (14, 15, 16, 17)
- [ ] Tests passing: All 4 tests

**Gap Checklist Item:** [ ] 🔄 ResumeCommand implemented with tests passing

---

## Gap 8.2: Sync State Integration
- Current State: ❌ INCOMPLETE (ResumeService has placeholder)
- Spec Reference: AC-044-048 definition
- What Exists: ResumeService structure
- What's Missing: Sync state detection and degraded mode

**Implementation Details (TBD by task-011b Sync Service):**

Once task-011b provides the Sync service, integrate:

```csharp
// In ResumeService.ResumeAsync:
var syncStatus = await _syncService.GetStatusAsync(targetSessionId, ct);

if (syncStatus == SyncStatus.Pending)
{
    // Degraded mode - continue locally but warn user
    _console.WriteWarning("Running in degraded mode - pending remote sync");
    _console.WriteWarning("Network sync will continue in background");
}

// Prevent network blocking - always proceed locally
return new ResumeResult(Success: true, ...);
```

**Acceptance Criteria Covered:** AC-044, AC-045, AC-046, AC-047, AC-048

**Success Criteria:**
- [ ] Detect pending sync state
- [ ] Display "degraded mode" when sync pending
- [ ] Continue execution without network blocking
- [ ] Background sync proceeds during execution
- [ ] Tests passing after task-011b complete

**Gap Checklist Item:** [ ] 🔄 Sync state integration implemented with tests passing

---

## Gap 8.3: Remote Reconnect Handling
- Current State: ❌ INCOMPLETE (TBD by remote execution)
- Spec Reference: AC-049-053 definition
- What Exists: Session lock mechanism (lock coordination)
- What's Missing: Duplicate step prevention, event sync

**Implementation Pattern (for execution engine):**

```csharp
// Before remote execution:
// 1. Detect reconnection
if (PreviouslyDisconnected())
{
    // 2. Sync pending events first
    await _syncService.SyncPendingAsync(sessionId, ct);

    // 3. Verify no duplicate work
    var appliedToolCalls = await _completionDetector.GetAppliedToolCallsAsync(sessionId, ct);

    // 4. Skip already-applied steps
    var plan = AdjustPlanForAppliedWork(plan, appliedToolCalls);

    // 5. Lock coordination - exclusive execution
    var lockAcquired = await _sessionLock.TryAcquireAsync(sessionId.ToString(), ct);
    if (!lockAcquired)
        throw new InvalidOperationException("Session locked by remote execution");
}
```

**Acceptance Criteria Covered:** AC-049, AC-050, AC-051, AC-052, AC-053

**Success Criteria:**
- [ ] Reconnect detection implemented
- [ ] Events synced before remote execution
- [ ] No duplicate steps executed
- [ ] No double commits
- [ ] Lock prevents concurrent execution

**Gap Checklist Item:** [ ] 🔄 Remote reconnect handling implemented with tests passing

---

## Gap 8.4: Integration Tests (3 test files)
- Current State: ❌ MISSING
- Spec Reference: Testing Requirements (lines 1336-1351)
- What Exists: All unit tests
- What's Missing: Integration scenarios

**Integration Test Files:**

1. **tests/Acode.Integration.Tests/Sessions/Resume/ResumeIntegrationTests.cs** (3 test methods)
   - Should_Resume_After_Crash() - Simulate process crash, resume from checkpoint
   - Should_Skip_Completed_Work() - Verify completed work is skipped
   - Should_Handle_Changed_Files() - Resume with modified workspace

2. **tests/Acode.Integration.Tests/Sessions/Resume/SyncResumeTests.cs** (3 test methods)
   - Should_Resume_With_Pending_Sync() - Resume with unsynced events
   - Should_Handle_Reconnect() - Remote reconnect coordination
   - Should_Prevent_Duplicates() - No duplicate tool calls

3. **tests/Acode.Integration.Tests/Sessions/Resume/InProgressHandlingTests.cs** (3 test methods)
   - Should_Retry_Reads() - Read operations retried safely
   - Should_Rollback_Partials() - Write operations rolled back
   - Should_Detect_Applied_Writes() - Know which operations completed

**Test Requirements:**
- End-to-end scenarios with real components
- Test crash/recovery flows
- Verify idempotency across multiple resumes
- Test multi-step execution with checkpoints

**Acceptance Criteria Covered:** AC-001-062 (all verified via E2E scenarios)

**Success Criteria:**
- [ ] ResumeIntegrationTests.cs created with 3 test methods
- [ ] SyncResumeTests.cs created with 3 test methods
- [ ] InProgressHandlingTests.cs created with 3 test methods
- [ ] All 9 integration tests passing
- [ ] Test coverage > 90% for resume code paths

**Gap Checklist Item:** [ ] 🔄 Integration tests implemented with all passing

---

# FINAL VERIFICATION CHECKLIST

Before marking task-011c complete, verify ALL criteria:

- [ ] **File Existence (9 production files)**
  - IResumeService.cs exists
  - ResumeService.cs exists
  - CheckpointManager.cs exists
  - ContinuationPlanner.cs exists
  - EnvironmentValidator.cs exists
  - InProgressHandler.cs exists
  - IdempotencyKeyGenerator.cs exists
  - CompletionDetector.cs exists
  - ResumeCommand.cs exists

- [ ] **No Stubs/NotImplementedException**
  - grep "NotImplementedException" src/Acode.Application/Sessions/**/*.cs → 0 matches
  - grep "NotImplementedException" src/Acode.Cli/Commands/ResumeCommand.cs → 0 matches

- [ ] **Test Coverage (17+ test methods, 8 test files)**
  - CheckpointTests.cs (3 methods, all passing)
  - ResumeServiceTests.cs (4 methods, all passing)
  - IdempotencyTests.cs (3 methods, all passing)
  - ReplayOrderTests.cs (3 methods, all passing)
  - EnvironmentValidationTests.cs (4 methods, all passing)
  - ResumeIntegrationTests.cs (3 methods, all passing)
  - SyncResumeTests.cs (3 methods, all passing)
  - InProgressHandlingTests.cs (3 methods, all passing)

- [ ] **Build Status**
  - dotnet build → 0 errors, 0 warnings
  - dotnet test → all tests passing

- [ ] **Acceptance Criteria (62 total)**
  - AC-001-007: Resume initiation (✅ ResumeCommand, ResumeService)
  - AC-008-013: State recovery (✅ ResumeService, CheckpointManager)
  - AC-014-017: Checkpoints (✅ CheckpointManager)
  - AC-018-022: Continuation (✅ ContinuationPlanner)
  - AC-023-027: In-progress handling (✅ InProgressHandler)
  - AC-028-032: Idempotency (✅ IdempotencyKeyGenerator, CompletionDetector)
  - AC-033-036: Replay (✅ ReplayOrderer, DeterministicReplayer)
  - AC-037-043: Environment (✅ EnvironmentValidator)
  - AC-044-048: Sync state (✅ ResumeService integration)
  - AC-049-053: Remote reconnect (✅ Lock coordination, duplicate prevention)
  - AC-054-058: Progress (✅ ResumeCommand display)
  - AC-059-062: Errors (✅ Error mapping, exit codes)

- [ ] **All Phases Complete**
  - Phase 1: Checkpoint ✅
  - Phase 2: State Recovery ✅
  - Phase 3: Continuation Planning ✅
  - Phase 4: In-Progress Handling ✅
  - Phase 5: Idempotency ✅
  - Phase 6: Environment Validation ✅
  - Phase 7: Replay & Determinism ✅
  - Phase 8: CLI + Integration ✅

- [ ] **Commits**
  - All work pushed to feature branch
  - Each phase committed separately
  - Commit messages follow Conventional Commits

---

## SUMMARY TABLE

| Phase | Description | Hours | AC Coverage | Status |
|-------|-------------|-------|-------------|--------|
| 1 | Checkpoint Infrastructure | 3-4 | AC-014-017 | [ ] 🔄 |
| 2 | State Recovery | 2-3 | AC-001-013 | [ ] 🔄 |
| 3 | Continuation Planning | 2-3 | AC-018-022 | [ ] 🔄 |
| 4 | In-Progress Handling | 2-3 | AC-023-027 | [ ] 🔄 |
| 5 | Idempotency | 2-3 | AC-028-032 | [ ] 🔄 |
| 6 | Environment Validation | 2-3 | AC-037-043 | [ ] 🔄 |
| 7 | Replay & Determinism | 2-3 | AC-033-036 | [ ] 🔄 |
| 8 | CLI + Integration | 3-4 | AC-044-062 | [ ] 🔄 |
| **TOTAL** | **8 Phases** | **18-26 hours** | **All 62 ACs** | **[ ] COMPLETE** |

---

**Status:** ✅ CHECKLIST COMPLETE - Ready for implementation (after task-011a)

**Next Agent:**
1. Verify task-011a is 100% complete
2. Start Phase 1: Checkpoint Infrastructure
3. Work through all 8 phases following TDD workflow
4. Mark each gap complete when tests passing
5. Final verification: All criteria checked
6. Create PR when complete

---
