# Task 012.d: Reviewer Stage

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 012 (Multi-Stage Loop), Task 012.c (Verifier), Task 049 (Conversation)  

---

## Description

Task 012.d implements the Reviewer stage—the final quality gate before task completion. While the Verifier checks individual step correctness, the Reviewer evaluates holistic quality: Does the implementation match user intent? Is the code well-structured? Are there any issues the step-by-step verification might have missed?

The Reviewer operates at a higher level than the Verifier. It sees the complete picture: the original request, the plan, all execution results, and verification outcomes. This bird's-eye view enables assessment that step-level verification cannot provide.

Quality dimensions assessed by the Reviewer include: intent alignment (does it do what was asked?), code quality (is it well-written?), completeness (is anything missing?), consistency (does it fit the codebase?), and safety (are there any concerns?). Each dimension produces a pass/fail with explanation.

The Reviewer uses LLM-based reasoning because these assessments are inherently fuzzy. What makes code "well-structured" depends on context, conventions, and judgment. The Reviewer presents the complete task context and asks for structured quality evaluation.

Reviewer rejection triggers re-planning. Unlike Verifier failure (which cycles to Executor), Reviewer rejection means the plan itself may be flawed. The Planner receives feedback about what's wrong and creates a revised plan. This enables fundamental corrections, not just step-level fixes.

Human review integration optionally involves the user. Before final completion, the Reviewer can present a summary for human approval. This is configurable—some modes auto-approve if LLM review passes, others always prompt, others never prompt.

The review summary is a key output artifact. It documents what was done, what was changed, any notes or concerns, and the reviewer's assessment. This summary is persisted and can be referenced later. It serves as documentation of the work performed.

Review speed matters. Complex reasoning takes time, but users shouldn't wait excessively. The Reviewer balances thoroughness with responsiveness. Simple tasks get quick reviews. Complex tasks justify more time. Progress is indicated during review.

The Reviewer respects Task 001 constraints. All LLM evaluation uses local models. No external code review services. The review runs locally, producing a local determination.

Observability captures review decisions. The reasoning chain is logged, enabling understanding of why work was approved or rejected. Review metrics (approval rate, common issues, cycle triggers) inform system improvement.

Error handling ensures review never blocks indefinitely. Timeouts trigger, escalation occurs, and users can always override. The Reviewer is a gate, not a wall—it can be bypassed when necessary (with appropriate logging).

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Reviewer Stage | Final quality assessment stage |
| Review | Holistic quality evaluation |
| Approval | Work passes quality gate |
| Rejection | Work needs changes |
| Intent Alignment | Matches what user asked |
| Code Quality | Well-written, maintainable |
| Completeness | Nothing missing |
| Consistency | Fits existing codebase |
| Safety | No concerning issues |
| Re-planning | Creating revised plan |
| Review Summary | Documentation of assessment |
| Human Review | User confirmation step |
| Quality Dimension | Aspect being evaluated |
| Override | Bypassing rejection |
| Feedback | Guidance for improvement |

---

## Out of Scope

The following items are explicitly excluded from Task 012.d:

- **Step-level verification** - Task 012.c
- **Code execution** - Task 012.b
- **Planning logic** - Task 012.a
- **Security auditing** - Future enhancement
- **Performance review** - Future enhancement
- **External review services** - Task 001 constraint
- **Multi-reviewer consensus** - Single review
- **Historical quality trends** - Future analytics
- **Review templates** - Future enhancement
- **Custom quality metrics** - Future enhancement

---

## Assumptions

### Technical Assumptions

- ASM-001: Quality assessment uses LLM judgment
- ASM-002: Review criteria are defined and consistent
- ASM-003: Quality dimensions are weighted for overall score
- ASM-004: Review considers full task, not individual steps
- ASM-005: Feedback is actionable for re-planning

### Behavioral Assumptions

- ASM-006: Review occurs after all steps complete and verify
- ASM-007: Failed review can trigger iteration (plan → execute → verify → review)
- ASM-008: Maximum iterations prevent infinite loops
- ASM-009: Human override can accept despite review failure
- ASM-010: Review results are persisted for learning

### Dependency Assumptions

- ASM-011: Task 012 orchestrator provides IStage contract
- ASM-012: Task 012.c verification provides test outcomes
- ASM-013: Task 012.b execution provides artifacts to review
- ASM-014: Session state tracks iteration count

### Quality Assumptions

- ASM-015: Code quality is primary review focus
- ASM-016: Maintainability and readability are considered
- ASM-017: Review balances perfectionism with practicality

---

## Functional Requirements

### Stage Lifecycle

- FR-001: Reviewer MUST implement IStage interface
- FR-002: OnEnter MUST load task context
- FR-003: Execute MUST perform review
- FR-004: OnExit MUST report decision
- FR-005: Lifecycle events MUST be logged

### Context Loading

- FR-006: Original request MUST be loaded
- FR-007: Task plan MUST be loaded
- FR-008: Execution results MUST be loaded
- FR-009: Verification results MUST be loaded
- FR-010: Changed files MUST be loaded
- FR-011: Context MUST respect token budget

### Quality Dimensions

- FR-012: Intent alignment MUST be assessed
- FR-013: Code quality MUST be assessed
- FR-014: Completeness MUST be assessed
- FR-015: Consistency MUST be assessed
- FR-016: Safety MUST be assessed
- FR-017: Each dimension MUST have result

### Intent Alignment

- FR-018: Compare result to request
- FR-019: Identify mismatches
- FR-020: Rate alignment level
- FR-021: Explain any gaps

### Code Quality

- FR-022: Evaluate structure
- FR-023: Evaluate naming
- FR-024: Evaluate documentation
- FR-025: Evaluate error handling
- FR-026: Identify issues

### Completeness

- FR-027: Check all requirements addressed
- FR-028: Identify missing pieces
- FR-029: Check edge cases considered
- FR-030: Flag gaps

### Consistency

- FR-031: Compare to existing patterns
- FR-032: Check style conformance
- FR-033: Check naming conventions
- FR-034: Identify deviations

### Safety

- FR-035: Check for security concerns
- FR-036: Check for data leaks
- FR-037: Check for unsafe operations
- FR-038: Flag concerns

### Review Decision

- FR-039: Decision MUST be APPROVED or REJECTED
- FR-040: APPROVED advances to completion
- FR-041: REJECTED triggers cycle check
- FR-042: Decision MUST include reasoning

### Rejection Feedback

- FR-043: Rejection MUST include issues
- FR-044: Issues MUST be specific
- FR-045: Feedback MUST be actionable
- FR-046: Feedback MUST flow to Planner

### Re-planning Trigger

- FR-047: Rejection MUST trigger re-plan check
- FR-048: Cycle count MUST be checked
- FR-049: Within limit cycles to Planner
- FR-050: At limit escalates

### Review Summary

- FR-051: Summary MUST be generated
- FR-052: Summary MUST list changes
- FR-053: Summary MUST include notes
- FR-054: Summary MUST include decision
- FR-055: Summary MUST be persisted

### Human Review

- FR-056: Human review MAY be required
- FR-057: Policy determines requirement
- FR-058: Prompt MUST show summary
- FR-059: User can approve/reject
- FR-060: User decision MUST be logged

### Override Capability

- FR-061: Rejection MAY be overridden
- FR-062: Override requires explicit action
- FR-063: Override reason MUST be logged
- FR-064: Override MUST be auditable

### Configuration

- FR-065: Quality thresholds configurable
- FR-066: Dimensions can be disabled
- FR-067: Human review policy configurable
- FR-068: Timeout configurable

---

## Non-Functional Requirements

### Performance

- NFR-001: Simple review < 30 seconds
- NFR-002: Complex review < 2 minutes
- NFR-003: Summary generation < 5 seconds

### Reliability

- NFR-004: Deterministic for same input
- NFR-005: Timeout recovery
- NFR-006: No blocking indefinitely

### Accuracy

- NFR-007: High-quality work approved
- NFR-008: Issues correctly identified
- NFR-009: Actionable feedback

### Security

- NFR-010: No secrets in summary
- NFR-011: Secure human prompt
- NFR-012: Audit trail complete

### Observability

- NFR-013: All reviews logged
- NFR-014: Decisions logged
- NFR-015: Reasoning captured

---

## User Manual Documentation

### Overview

The Reviewer stage performs final quality assessment before completing a task. It evaluates holistic quality—does the implementation achieve the goal with good code?

### How Review Works

After verification passes:

1. **Load Context** - Get full task history
2. **Assess Dimensions** - Evaluate quality aspects
3. **Make Decision** - Approve or reject
4. **Generate Summary** - Document the work
5. **Complete or Cycle** - Finish or re-plan

### Review Output

```
$ acode run "Add email validation"

[VERIFIER] All checks passed ✓

[REVIEWER] Reviewing implementation...

  Quality Assessment:
    ✓ Intent Alignment: Excellent
      Implementation matches request
    ✓ Code Quality: Good
      Well-structured, clear naming
    ✓ Completeness: Excellent
      All requirements addressed
    ✓ Consistency: Good
      Follows existing patterns
    ✓ Safety: Excellent
      No concerns identified

  Decision: APPROVED

[COMPLETE] Task finished successfully
```

### Review Rejection

```
[REVIEWER] Reviewing implementation...

  Quality Assessment:
    ✓ Intent Alignment: Good
    ✗ Code Quality: Needs Work
      - Missing error handling for null input
      - No documentation on public methods
    ✓ Completeness: Good
    ✓ Consistency: Good
    ✓ Safety: Good

  Decision: REJECTED
  Reason: Code quality issues need addressing

[ORCHESTRATOR] Cycling to Planner for revision
  Feedback: "Add null handling and JSDoc comments"

[PLANNER] Re-planning (v2)...
```

### Review Summary

```bash
$ acode summary

Task Summary: Add email validation
Session: abc123
Status: COMPLETED

Changes Made:
  + src/validators/EmailValidator.ts (new)
  ~ src/forms/ContactForm.ts (modified)
  + tests/EmailValidator.test.ts (new)

Reviewer Notes:
  - Clean implementation following existing patterns
  - Good test coverage with edge cases
  - Consider adding phone validation in future

Quality Assessment: APPROVED
  Intent: Excellent | Quality: Good | Complete: Excellent
  Consistency: Good | Safety: Excellent

Completed: 2024-01-15 14:32:00
Duration: 2m 34s
```

### Configuration

```yaml
# .agent/config.yml
reviewer:
  # Enable/disable dimensions
  dimensions:
    intent_alignment: true
    code_quality: true
    completeness: true
    consistency: true
    safety: true
    
  # Timeout for review
  timeout_seconds: 120
  
  # Human review policy
  human_review: prompt  # auto | prompt | require
  
  # Quality threshold (all dimensions must meet)
  min_quality: good  # excellent | good | acceptable
```

### Human Review

When configured to prompt:

```
[REVIEWER] LLM review: APPROVED

Human Review Required:
─────────────────────────────────────
Task: Add email validation

Changes:
  + EmailValidator.ts (45 lines)
  ~ ContactForm.ts (3 lines changed)
  + EmailValidator.test.ts (89 lines)

Assessment: All quality dimensions passed

[A]pprove  [R]eject  [V]iew changes  [D]etails

Choice: a

✓ Approved by user
[COMPLETE] Task finished
```

### Override

If reviewer rejects but you want to proceed:

```
[REVIEWER] Decision: REJECTED
  Issue: Minor code quality concerns

Override review rejection? [y/N] y
Reason for override: Acceptable for prototype

⚠ Review rejection overridden
  Reason: Acceptable for prototype
  Logged for audit

[COMPLETE] Task finished (with override)
```

### CLI Commands

```bash
# View last review
$ acode review show

Review for session abc123:
  Decision: APPROVED
  Time: 5 minutes ago
  
  Dimensions:
    Intent: Excellent
    Quality: Good
    Completeness: Excellent
    ...

# Force re-review
$ acode review run

[REVIEWER] Running review...
  Decision: APPROVED

# View review history
$ acode review history

Session   Decision   Time
abc123    APPROVED   5m ago
def456    REJECTED   1h ago
ghi789    APPROVED   2d ago
```

### Quality Dimensions

| Dimension | What It Checks |
|-----------|---------------|
| Intent Alignment | Does result match request? |
| Code Quality | Is code well-written? |
| Completeness | Is anything missing? |
| Consistency | Does it fit codebase? |
| Safety | Any security concerns? |

### Quality Levels

| Level | Meaning |
|-------|---------|
| Excellent | No issues, high quality |
| Good | Minor issues, acceptable |
| Acceptable | Some issues, passable |
| Needs Work | Issues must be addressed |
| Poor | Significant problems |

### Troubleshooting

#### Review Takes Too Long

**Problem:** Review exceeds timeout

**Solutions:**
1. Increase timeout: `reviewer.timeout_seconds: 180`
2. Reduce context size
3. Simplify task scope

#### False Rejections

**Problem:** Good work rejected

**Solutions:**
1. Review dimension settings
2. Adjust min_quality threshold
3. Override if appropriate

#### Missing Feedback

**Problem:** Rejection without clear guidance

**Solutions:**
1. Check review logs for details
2. Request re-review with more context
3. Use override with reason

---

## Acceptance Criteria

### Stage Lifecycle

- [ ] AC-001: IStage implemented
- [ ] AC-002: Context loaded
- [ ] AC-003: Review runs
- [ ] AC-004: Decision reported
- [ ] AC-005: Events logged

### Context

- [ ] AC-006: Request loaded
- [ ] AC-007: Plan loaded
- [ ] AC-008: Execution loaded
- [ ] AC-009: Verification loaded
- [ ] AC-010: Files loaded
- [ ] AC-011: Budget respected

### Dimensions

- [ ] AC-012: Intent assessed
- [ ] AC-013: Quality assessed
- [ ] AC-014: Completeness assessed
- [ ] AC-015: Consistency assessed
- [ ] AC-016: Safety assessed
- [ ] AC-017: Results per dimension

### Decision

- [ ] AC-018: APPROVED/REJECTED
- [ ] AC-019: Approved completes
- [ ] AC-020: Rejected cycles
- [ ] AC-021: Reasoning included

### Rejection

- [ ] AC-022: Issues listed
- [ ] AC-023: Specific and actionable
- [ ] AC-024: Flows to Planner

### Re-planning

- [ ] AC-025: Cycle triggered
- [ ] AC-026: Count checked
- [ ] AC-027: Escalation works

### Summary

- [ ] AC-028: Generated
- [ ] AC-029: Lists changes
- [ ] AC-030: Includes notes
- [ ] AC-031: Includes decision
- [ ] AC-032: Persisted

### Human Review

- [ ] AC-033: Policy works
- [ ] AC-034: Summary shown
- [ ] AC-035: Decision captured
- [ ] AC-036: Logged

### Override

- [ ] AC-037: Works
- [ ] AC-038: Requires reason
- [ ] AC-039: Logged
- [ ] AC-040: Auditable

---

## Testing Requirements

### Unit Tests

```csharp
namespace AgenticCoder.Application.Tests.Unit.Orchestration.Stages.Reviewer;

public class ReviewerStageTests
{
    private readonly Mock<IDimensionAssessor> _mockAssessor;
    private readonly Mock<ISummaryGenerator> _mockSummaryGenerator;
    private readonly Mock<IHumanReviewService> _mockHumanReview;
    private readonly ILogger<ReviewerStage> _logger;
    private readonly ReviewerStage _reviewer;
    
    public ReviewerStageTests()
    {
        _mockAssessor = new Mock<IDimensionAssessor>();
        _mockSummaryGenerator = new Mock<ISummaryGenerator>();
        _mockHumanReview = new Mock<IHumanReviewService>();
        _logger = NullLogger<ReviewerStage>.Instance;
        _reviewer = new ReviewerStage(_mockAssessor.Object, _mockSummaryGenerator.Object, 
            _mockHumanReview.Object, _logger);
    }
    
    [Fact]
    public async Task Should_Approve_When_All_Dimensions_Pass()
    {
        // Arrange
        var context = CreateReviewContext();
        var options = new ReviewOptions { HumanReview = false };
        var excellentDimensions = new List<DimensionResult>
        {
            new DimensionResult("IntentAlignment", QualityLevel.Excellent, "Perfect alignment", Array.Empty<string>()),
            new DimensionResult("CodeQuality", QualityLevel.Good, "Well written", Array.Empty<string>()),
            new DimensionResult("Completeness", QualityLevel.Excellent, "All requirements met", Array.Empty<string>())
        };
        
        _mockAssessor
            .Setup(a => a.AssessAllAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(excellentDimensions);
        
        var summary = new ReviewSummary(Guid.NewGuid(), "test goal", Array.Empty<string>(),
            Array.Empty<string>(), ReviewDecision.Approved, new Dictionary<string, QualityLevel>());
        
        _mockSummaryGenerator
            .Setup(s => s.Generate(It.IsAny<ReviewContext>(), ReviewDecision.Approved, excellentDimensions))
            .Returns(summary);
        
        // Act
        var result = await _reviewer.ReviewAsync(context, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(ReviewDecision.Approved, result.Decision);
        Assert.Equal(3, result.DimensionResults.Count);
        Assert.Null(result.Feedback);
    }
    
    [Fact]
    public async Task Should_Reject_And_Provide_Feedback_When_Dimensions_Fail()
    {
        // Arrange
        var context = CreateReviewContext();
        var options = new ReviewOptions { HumanReview = false };
        var poorDimensions = new List<DimensionResult>
        {
            new DimensionResult("IntentAlignment", QualityLevel.Poor, "Misaligned", 
                new[] { "Missing core feature X" }.ToList().AsReadOnly()),
            new DimensionResult("CodeQuality", QualityLevel.NeedsWork, "Quality issues", 
                new[] { "No error handling", "Magic numbers" }.ToList().AsReadOnly())
        };
        
        _mockAssessor
            .Setup(a => a.AssessAllAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(poorDimensions);
        
        var summary = new ReviewSummary(Guid.NewGuid(), "test goal", Array.Empty<string>(),
            Array.Empty<string>(), ReviewDecision.Rejected, new Dictionary<string, QualityLevel>());
        
        _mockSummaryGenerator
            .Setup(s => s.Generate(It.IsAny<ReviewContext>(), ReviewDecision.Rejected, poorDimensions))
            .Returns(summary);
        
        // Act
        var result = await _reviewer.ReviewAsync(context, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(ReviewDecision.Rejected, result.Decision);
        Assert.NotNull(result.Feedback);
        Assert.Contains("Missing core feature X", result.Feedback.Issues.First());
    }
    
    [Fact]
    public async Task Should_Prompt_Human_Review_When_Configured()
    {
        // Arrange
        var context = CreateReviewContext();
        var options = new ReviewOptions { HumanReview = true };
        var dimensions = CreateGoodDimensions();
        
        _mockAssessor
            .Setup(a => a.AssessAllAsync(It.IsAny<ReviewContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dimensions);
        
        _mockHumanReview
            .Setup(h => h.PromptForReviewAsync(It.IsAny<ReviewContext>(), dimensions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HumanReviewResponse(Approved: true, Reason: null));
        
        var summary = new ReviewSummary(Guid.NewGuid(), "test goal", Array.Empty<string>(),
            Array.Empty<string>(), ReviewDecision.Approved, new Dictionary<string, QualityLevel>());
        
        _mockSummaryGenerator
            .Setup(s => s.Generate(It.IsAny<ReviewContext>(), ReviewDecision.Approved, dimensions))
            .Returns(summary);
        
        // Act
        var result = await _reviewer.ReviewAsync(context, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(ReviewDecision.Approved, result.Decision);
        _mockHumanReview.Verify(
            h => h.PromptForReviewAsync(It.IsAny<ReviewContext>(), dimensions, It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    private static ReviewContext CreateReviewContext()
    {
        return new ReviewContext(
            Session: new SessionInfo(Guid.NewGuid(), "test"),
            Request: new UserRequest("test goal"),
            Plan: new Plan("plan", Array.Empty<PlanTask>()),
            ExecutionResults: Array.Empty<StepResult>());
    }
    
    private static List<DimensionResult> CreateGoodDimensions()
    {
        return new List<DimensionResult>
        {
            new DimensionResult("IntentAlignment", QualityLevel.Good, "Good alignment", Array.Empty<string>())
        };
    }
}

public class DimensionTests
{
    [Fact]
    public async Task Should_Assess_Intent_Alignment()
    {
        // Arrange
        var mockLlm = new Mock<ILlmClient>();
        var dimension = new IntentAlignmentDimension(mockLlm.Object, NullLogger<IntentAlignmentDimension>.Instance);
        
        var context = new ReviewContext(
            Session: new SessionInfo(Guid.NewGuid(), "test"),
            Request: new UserRequest("Create a user service"),
            Plan: new Plan("plan", new[] { new PlanTask("Create UserService class") }),
            ExecutionResults: new[] { new StepResult(StepStatus.Success, "Created UserService.cs", "Success", 100) });
        
        mockLlm
            .Setup(l => l.GenerateAsync(It.IsAny<string>(), It.IsAny<LlmOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<assessment level=\"excellent\">The implementation perfectly aligns with the user's intent.</assessment>");
        
        // Act
        var result = await dimension.AssessAsync(context, CancellationToken.None);
        
        // Assert
        Assert.Equal("IntentAlignment", result.Name);
        Assert.Equal(QualityLevel.Excellent, result.Level);
        Assert.Contains("aligns", result.Explanation, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public async Task Should_Assess_Code_Quality()
    {
        // Arrange
        var mockLlm = new Mock<ILlmClient>();
        var dimension = new CodeQualityDimension(mockLlm.Object, NullLogger<CodeQualityDimension>.Instance);
        
        var context = new ReviewContext(
            Session: new SessionInfo(Guid.NewGuid(), "test"),
            Request: new UserRequest("test"),
            Plan: new Plan("plan", Array.Empty<PlanTask>()),
            ExecutionResults: Array.Empty<StepResult>());
        
        mockLlm
            .Setup(l => l.GenerateAsync(It.IsAny<string>(), It.IsAny<LlmOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<assessment level=\"good\"><issues>Missing null checks</issues>Well structured code.</assessment>");
        
        // Act
        var result = await dimension.AssessAsync(context, CancellationToken.None);
        
        // Assert
        Assert.Equal("CodeQuality", result.Name);
        Assert.Equal(QualityLevel.Good, result.Level);
        Assert.Single(result.Issues);
    }
}

public class SummaryGeneratorTests
{
    [Fact]
    public void Should_Generate_Summary_With_Changes_And_Notes()
    {
        // Arrange
        var generator = new SummaryGenerator(NullLogger<SummaryGenerator>.Instance);
        var context = new ReviewContext(
            Session: new SessionInfo(Guid.NewGuid(), "test"),
            Request: new UserRequest("Create user service"),
            Plan: new Plan("plan", Array.Empty<PlanTask>()),
            ExecutionResults: new[] {
                new StepResult(StepStatus.Success, "Created UserService.cs with 3 methods", "Success", 100),
                new StepResult(StepStatus.Success, "Added IUserService interface", "Success", 50)
            });
        
        var dimensions = new List<DimensionResult>
        {
            new DimensionResult("IntentAlignment", QualityLevel.Excellent, "Perfect", Array.Empty<string>()),
            new DimensionResult("CodeQuality", QualityLevel.Good, "Well written", Array.Empty<string>())
        };
        
        // Act
        var summary = generator.Generate(context, ReviewDecision.Approved, dimensions);
        
        // Assert
        Assert.Equal(ReviewDecision.Approved, summary.Decision);
        Assert.Equal(2, summary.Changes.Count);
        Assert.Contains("UserService.cs", summary.Changes[0]);
        Assert.Contains("IUserService", summary.Changes[1]);
        Assert.Equal(2, summary.DimensionLevels.Count);
    }
}
```

### Integration Tests

```csharp
namespace AgenticCoder.Application.Tests.Integration.Orchestration.Stages.Reviewer;

public class ReviewerIntegrationTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;
    
    public ReviewerIntegrationTests(TestServerFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Should_Review_Real_Task_End_To_End()
    {
        // Arrange
        var reviewer = _fixture.GetService<IReviewerStage>();
        var workspace = await _fixture.CreateTestWorkspaceAsync();
        await File.WriteAllTextAsync(Path.Combine(workspace.RootPath, "UserService.cs"), 
            "public class UserService { }");
        
        var context = new ReviewContext(
            Session: new SessionInfo(Guid.NewGuid(), "test"),
            Request: new UserRequest("Create user service"),
            Plan: new Plan("plan", Array.Empty<PlanTask>()),
            ExecutionResults: new[] { new StepResult(StepStatus.Success, "Created UserService.cs", "Success", 100) });
        
        var options = new ReviewOptions { HumanReview = false };
        
        // Act
        var result = await reviewer.ReviewAsync(context, options, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Summary);
    }
}
```

### E2E Tests

```csharp
namespace AgenticCoder.Application.Tests.E2E.Orchestration.Stages.Reviewer;

public class ReviewerE2ETests : IClassFixture<E2ETestFixture>
{
    private readonly E2ETestFixture _fixture;
    
    public ReviewerE2ETests(E2ETestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Should_Approve_Good_Task_Through_All_Dimensions()
    {
        // Arrange
        var reviewer = _fixture.GetService<IReviewerStage>();
        var workspace = await _fixture.CreateTestWorkspaceWithValidCodeAsync();
        var executionResults = await _fixture.ExecuteGoodTaskAsync(workspace);
        
        var context = new ReviewContext(
            Session: new SessionInfo(Guid.NewGuid(), "test"),
            Request: new UserRequest("Create user service"),
            Plan: new Plan("plan", Array.Empty<PlanTask>()),
            ExecutionResults: executionResults);
        
        var options = new ReviewOptions { HumanReview = false };
        
        // Act
        var result = await reviewer.ReviewAsync(context, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(ReviewDecision.Approved, result.Decision);
        Assert.All(result.DimensionResults, d => 
            Assert.True(d.Level >= QualityLevel.Acceptable, $"Dimension {d.Name} failed"));
    }
    
    [Fact]
    public async Task Should_Reject_And_Cycle_To_Planner_On_Poor_Quality()
    {
        // Arrange
        var reviewer = _fixture.GetService<IReviewerStage>();
        var workspace = await _fixture.CreateTestWorkspaceAsync();
        await File.WriteAllTextAsync(Path.Combine(workspace.RootPath, "bad.cs"), "badcode");
        
        var context = new ReviewContext(
            Session: new SessionInfo(Guid.NewGuid(), "test"),
            Request: new UserRequest("Create service"),
            Plan: new Plan("plan", Array.Empty<PlanTask>()),
            ExecutionResults: new[] { new StepResult(StepStatus.Success, "Created bad.cs", "Success", 100) });
        
        var options = new ReviewOptions { HumanReview = false };
        
        // Act
        var result = await reviewer.ReviewAsync(context, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(ReviewDecision.Rejected, result.Decision);
        Assert.NotNull(result.Feedback);
        Assert.NotEmpty(result.Feedback.Issues);
    }
}
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Simple review | 15s | 30s |
| Complex review | 60s | 120s |
| Summary generation | 2s | 5s |
| Context loading | 1s | 5s |

---

## User Verification Steps

### Scenario 1: Approved

1. Complete good implementation
2. Run review
3. Verify: All dimensions pass
4. Verify: APPROVED decision

### Scenario 2: Rejected

1. Complete with quality issues
2. Run review
3. Verify: Issues identified
4. Verify: REJECTED with feedback

### Scenario 3: Re-plan Cycle

1. Trigger rejection
2. Observe: Cycle to Planner
3. Verify: Feedback provided
4. Verify: Plan revised

### Scenario 4: Human Review

1. Configure human_review: prompt
2. Complete task
3. Verify: Prompt shown
4. Approve manually
5. Verify: Completes

### Scenario 5: Override

1. Trigger rejection
2. Override rejection
3. Provide reason
4. Verify: Completes
5. Verify: Override logged

### Scenario 6: Summary

1. Complete task
2. View summary
3. Verify: Changes listed
4. Verify: Notes included
5. Verify: Decision shown

### Scenario 7: Timeout

1. Configure short timeout
2. Run complex review
3. Verify: Timeout handled
4. Verify: Escalation occurs

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/
├── Orchestration/
│   └── Stages/
│       └── Reviewer/
│           ├── IReviewerStage.cs
│           ├── ReviewerStage.cs
│           ├── DimensionAssessor.cs
│           ├── SummaryGenerator.cs
│           └── Dimensions/
│               ├── IDimension.cs
│               ├── IntentAlignmentDimension.cs
│               ├── CodeQualityDimension.cs
│               ├── CompletenessDimension.cs
│               ├── ConsistencyDimension.cs
│               └── SafetyDimension.cs
```

### IReviewerStage Interface

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Reviewer;

public interface IReviewerStage : IStage
{
    Task<ReviewResult> ReviewAsync(
        ReviewContext context,
        ReviewOptions options,
        CancellationToken ct);
}

public sealed record ReviewResult(
    ReviewDecision Decision,
    IReadOnlyList<DimensionResult> DimensionResults,
    ReviewSummary Summary,
    ReviewFeedback? Feedback);

public enum ReviewDecision
{
    Approved,
    Rejected
}
```

### IDimension Interface

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Reviewer.Dimensions;

public interface IDimension
{
    string Name { get; }
    Task<DimensionResult> AssessAsync(ReviewContext context, CancellationToken ct);
}

public sealed record DimensionResult(
    string Name,
    QualityLevel Level,
    string Explanation,
    IReadOnlyList<string> Issues);

public enum QualityLevel
{
    Excellent,
    Good,
    Acceptable,
    NeedsWork,
    Poor
}
```

### ReviewerStage Complete Implementation

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Reviewer;

public sealed class ReviewerStage : StageBase, IReviewerStage
{
    private readonly IDimensionAssessor _dimensionAssessor;
    private readonly ISummaryGenerator _summaryGenerator;
    private readonly IHumanReviewService _humanReviewService;
    private readonly ILogger<ReviewerStage> _logger;
    
    public override StageType Type => StageType.Reviewer;
    
    public ReviewerStage(
        IDimensionAssessor dimensionAssessor,
        ISummaryGenerator summaryGenerator,
        IHumanReviewService humanReviewService,
        ILogger<ReviewerStage> logger) : base(logger)
    {
        _dimensionAssessor = dimensionAssessor ?? throw new ArgumentNullException(nameof(dimensionAssessor));
        _summaryGenerator = summaryGenerator ?? throw new ArgumentNullException(nameof(summaryGenerator));
        _humanReviewService = humanReviewService ?? throw new ArgumentNullException(nameof(humanReviewService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    protected override async Task<StageResult> ExecuteStageAsync(
        StageContext context,
        CancellationToken ct)
    {
        var reviewContext = (ReviewContext)context.StageData["review_context"];
        var options = new ReviewOptions { HumanReview = false };
        
        var reviewResult = await ReviewAsync(reviewContext, options, ct);
        
        return new StageResult(
            Status: reviewResult.Decision == ReviewDecision.Approved ? StageStatus.Success : StageStatus.Cycle,
            Output: reviewResult,
            NextStage: reviewResult.Decision == ReviewDecision.Approved ? null : StageType.Planner,
            Message: $"Review {reviewResult.Decision}",
            Metrics: new StageMetrics(StageType.Reviewer, TimeSpan.Zero, 0));
    }
    
    public async Task<ReviewResult> ReviewAsync(
        ReviewContext context,
        ReviewOptions options,
        CancellationToken ct)
    {
        _logger.LogInformation("Starting review for session {SessionId}", context.Session.Id);
        
        // Assess all quality dimensions
        var dimensionResults = await _dimensionAssessor.AssessAllAsync(context, ct);
        
        // Determine decision based on dimension results
        var decision = DetermineDecision(dimensionResults);
        
        // If human review is required, prompt for confirmation
        if (options.HumanReview)
        {
            var humanResponse = await _humanReviewService.PromptForReviewAsync(context, dimensionResults, ct);
            if (!humanResponse.Approved)
            {
                decision = ReviewDecision.Rejected;
                _logger.LogWarning("Human reviewer rejected the work: {Reason}", humanResponse.Reason);
            }
        }
        
        // Generate summary
        var summary = _summaryGenerator.Generate(context, decision, dimensionResults);
        
        // Generate feedback if rejected
        ReviewFeedback? feedback = null;
        if (decision == ReviewDecision.Rejected)
        {
            feedback = GenerateFeedback(dimensionResults);
        }
        
        _logger.LogInformation("Review complete: {Decision}", decision);
        
        return new ReviewResult(decision, dimensionResults, summary, feedback);
    }
    
    private ReviewDecision DetermineDecision(IReadOnlyList<DimensionResult> dimensions)
    {
        // Reject if any dimension is Poor or more than 2 are NeedsWork
        var poorCount = dimensions.Count(d => d.Level == QualityLevel.Poor);
        var needsWorkCount = dimensions.Count(d => d.Level == QualityLevel.NeedsWork);
        
        if (poorCount > 0 || needsWorkCount > 2)
        {
            return ReviewDecision.Rejected;
        }
        
        return ReviewDecision.Approved;
    }
    
    private ReviewFeedback GenerateFeedback(IReadOnlyList<DimensionResult> dimensions)
    {
        var issues = dimensions
            .Where(d => d.Level <= QualityLevel.NeedsWork)
            .SelectMany(d => d.Issues.Select(issue => $"{d.Name}: {issue}"))
            .ToList();
        
        var suggestions = dimensions
            .Where(d => d.Level <= QualityLevel.NeedsWork)
            .Select(d => $"Improve {d.Name}: {d.Explanation}")
            .ToList();
        
        return new ReviewFeedback(issues.AsReadOnly(), suggestions.AsReadOnly());
    }
}
```

### DimensionAssessor Implementation

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Reviewer;

public interface IDimensionAssessor
{
    Task<IReadOnlyList<DimensionResult>> AssessAllAsync(ReviewContext context, CancellationToken ct);
}

public sealed class DimensionAssessor : IDimensionAssessor
{
    private readonly IEnumerable<IDimension> _dimensions;
    private readonly ILogger<DimensionAssessor> _logger;
    
    public DimensionAssessor(IEnumerable<IDimension> dimensions, ILogger<DimensionAssessor> logger)
    {
        _dimensions = dimensions ?? throw new ArgumentNullException(nameof(dimensions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<IReadOnlyList<DimensionResult>> AssessAllAsync(ReviewContext context, CancellationToken ct)
    {
        _logger.LogInformation("Assessing {DimensionCount} quality dimensions", _dimensions.Count());
        
        var results = new List<DimensionResult>();
        
        // Run dimensions in parallel for efficiency
        var tasks = _dimensions.Select(dim => AssessDimensionAsync(dim, context, ct));
        var dimensionResults = await Task.WhenAll(tasks);
        
        results.AddRange(dimensionResults);
        
        var excellentCount = results.Count(r => r.Level == QualityLevel.Excellent);
        var poorCount = results.Count(r => r.Level <= QualityLevel.NeedsWork);
        
        _logger.LogInformation("Assessment complete: {Excellent} excellent, {Poor} poor/needs work",
            excellentCount, poorCount);
        
        return results.AsReadOnly();
    }
    
    private async Task<DimensionResult> AssessDimensionAsync(
        IDimension dimension,
        ReviewContext context,
        CancellationToken ct)
    {
        _logger.LogDebug("Assessing dimension: {DimensionName}", dimension.Name);
        
        try
        {
            return await dimension.AssessAsync(context, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dimension {DimensionName} assessment failed", dimension.Name);
            return new DimensionResult(
                Name: dimension.Name,
                Level: QualityLevel.Poor,
                Explanation: $"Assessment failed: {ex.Message}",
                Issues: new[] { "Dimension assessment error" }.ToList().AsReadOnly());
        }
    }
}
```

### SummaryGenerator Complete Implementation

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Reviewer;

public interface ISummaryGenerator
{
    ReviewSummary Generate(
        ReviewContext context,
        ReviewDecision decision,
        IReadOnlyList<DimensionResult> dimensions);
}

public sealed class SummaryGenerator : ISummaryGenerator
{
    private readonly ILogger<SummaryGenerator> _logger;
    
    public SummaryGenerator(ILogger<SummaryGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public ReviewSummary Generate(
        ReviewContext context,
        ReviewDecision decision,
        IReadOnlyList<DimensionResult> dimensions)
    {
        _logger.LogInformation("Generating review summary for session {SessionId}", context.Session.Id);
        
        var changes = ExtractChanges(context.ExecutionResults);
        var notes = GenerateNotes(dimensions);
        var dimensionLevels = dimensions.ToDictionary(d => d.Name, d => d.Level);
        
        return new ReviewSummary(
            SessionId: context.Session.Id,
            Goal: context.Request.Goal,
            Changes: changes.AsReadOnly(),
            Notes: notes.AsReadOnly(),
            Decision: decision,
            DimensionLevels: dimensionLevels);
    }
    
    private List<string> ExtractChanges(IReadOnlyList<StepResult> executionResults)
    {
        return executionResults
            .Where(r => r.Status == StepStatus.Success)
            .Select(r => r.Output?.ToString() ?? "")
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .ToList();
    }
    
    private List<string> GenerateNotes(IReadOnlyList<DimensionResult> dimensions)
    {
        var notes = new List<string>();
        
        foreach (var dim in dimensions)
        {
            var levelText = dim.Level switch
            {
                QualityLevel.Excellent => "✓ Excellent",
                QualityLevel.Good => "✓ Good",
                QualityLevel.Acceptable => "~ Acceptable",
                QualityLevel.NeedsWork => "! Needs Work",
                QualityLevel.Poor => "✗ Poor",
                _ => "?"
            };
            
            notes.Add($"{dim.Name}: {levelText} - {dim.Explanation}");
            
            if (dim.Issues.Any())
            {
                notes.Add($"  Issues: {string.Join(", ", dim.Issues)}");
            }
        }
        
        return notes;
    }
}

public sealed record ReviewSummary(
    Guid SessionId,
    string Goal,
    IReadOnlyList<string> Changes,
    IReadOnlyList<string> Notes,
    ReviewDecision Decision,
    IReadOnlyDictionary<string, QualityLevel> DimensionLevels);

public sealed record ReviewFeedback(
    IReadOnlyList<string> Issues,
    IReadOnlyList<string> Suggestions);
```

### IntentAlignmentDimension Implementation

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Reviewer.Dimensions;

public sealed class IntentAlignmentDimension : IDimension
{
    private readonly ILlmClient _llmClient;
    private readonly ILogger<IntentAlignmentDimension> _logger;
    
    public string Name => "IntentAlignment";
    
    public IntentAlignmentDimension(ILlmClient llmClient, ILogger<IntentAlignmentDimension> logger)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<DimensionResult> AssessAsync(ReviewContext context, CancellationToken ct)
    {
        _logger.LogDebug("Assessing intent alignment for goal: {Goal}", context.Request.Goal);
        
        var prompt = $@"
You are reviewing whether the implementation aligns with the user's intent.

User Goal: {context.Request.Goal}

Plan Tasks:
{string.Join("\n", context.Plan.Tasks.Select((t, i) => $"{i + 1}. {t.Description}"))}

Execution Results:
{string.Join("\n", context.ExecutionResults.Select((r, i) => $"{i + 1}. {r.Output}"))}

Assess how well the implementation aligns with the user's stated intent.
Provide your assessment in this format:
<assessment level=\"excellent|good|acceptable|needswork|poor\">
<issues>Issue 1</issues>
<issues>Issue 2</issues>
Explanation text here.
</assessment>
";
        
        var response = await _llmClient.GenerateAsync(prompt, new LlmOptions(), ct);
        
        return ParseAssessmentResponse(response);
    }
    
    private DimensionResult ParseAssessmentResponse(string response)
    {
        var match = Regex.Match(response, @"<assessment level=""(\w+)"">(.*?)</assessment>", RegexOptions.Singleline);
        if (!match.Success)
        {
            return new DimensionResult(Name, QualityLevel.Acceptable, "Unable to parse assessment", Array.Empty<string>());
        }
        
        var levelStr = match.Groups[1].Value.ToLowerInvariant();
        var content = match.Groups[2].Value;
        
        var level = levelStr switch
        {
            "excellent" => QualityLevel.Excellent,
            "good" => QualityLevel.Good,
            "acceptable" => QualityLevel.Acceptable,
            "needswork" => QualityLevel.NeedsWork,
            "poor" => QualityLevel.Poor,
            _ => QualityLevel.Acceptable
        };
        
        var issues = Regex.Matches(content, @"<issues>(.*?)</issues>")
            .Select(m => m.Groups[1].Value.Trim())
            .ToList();
        
        var explanation = Regex.Replace(content, @"<issues>.*?</issues>", "").Trim();
        
        return new DimensionResult(Name, level, explanation, issues.AsReadOnly());
    }
}
```

### CodeQualityDimension Implementation

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Reviewer.Dimensions;

public sealed class CodeQualityDimension : IDimension
{
    private readonly ILlmClient _llmClient;
    private readonly ILogger<CodeQualityDimension> _logger;
    
    public string Name => "CodeQuality";
    
    public CodeQualityDimension(ILlmClient llmClient, ILogger<CodeQualityDimension> logger)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<DimensionResult> AssessAsync(ReviewContext context, CancellationToken ct)
    {
        _logger.LogDebug("Assessing code quality");
        
        var prompt = $@"
You are reviewing code quality.

Execution Results:
{string.Join("\n", context.ExecutionResults.Select((r, i) => $"{i + 1}. {r.Output}"))}

Assess the code quality including:
- Readability and maintainability
- Error handling
- Code organization
- Best practices
- Potential bugs

Provide your assessment in this format:
<assessment level=\"excellent|good|acceptable|needswork|poor\">
<issues>Issue 1</issues>
<issues>Issue 2</issues>
Explanation text here.
</assessment>
";
        
        var response = await _llmClient.GenerateAsync(prompt, new LlmOptions(), ct);
        
        return ParseAssessmentResponse(response);
    }
    
    private DimensionResult ParseAssessmentResponse(string response)
    {
        // Same parsing logic as IntentAlignmentDimension
        var match = Regex.Match(response, @"<assessment level=""(\w+)"">(.*?)</assessment>", RegexOptions.Singleline);
        if (!match.Success)
        {
            return new DimensionResult(Name, QualityLevel.Acceptable, "Unable to parse assessment", Array.Empty<string>());
        }
        
        var levelStr = match.Groups[1].Value.ToLowerInvariant();
        var content = match.Groups[2].Value;
        
        var level = levelStr switch
        {
            "excellent" => QualityLevel.Excellent,
            "good" => QualityLevel.Good,
            "acceptable" => QualityLevel.Acceptable,
            "needswork" => QualityLevel.NeedsWork,
            "poor" => QualityLevel.Poor,
            _ => QualityLevel.Acceptable
        };
        
        var issues = Regex.Matches(content, @"<issues>(.*?)</issues>")
            .Select(m => m.Groups[1].Value.Trim())
            .ToList();
        
        var explanation = Regex.Replace(content, @"<issues>.*?</issues>", "").Trim();
        
        return new DimensionResult(Name, level, explanation, issues.AsReadOnly());
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-REVIEW-001 | Review failed |
| ACODE-REVIEW-002 | Dimension assessment failed |
| ACODE-REVIEW-003 | Review timeout |
| ACODE-REVIEW-004 | Human review declined |
| ACODE-REVIEW-005 | Summary generation failed |

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Review approved |
| 1 | General failure |
| 50 | Review rejected |
| 51 | Human declined |
| 52 | Review timeout |

### Logging Fields

```json
{
  "event": "review_decision",
  "session_id": "abc123",
  "decision": "approved",
  "dimensions": {
    "intent_alignment": "excellent",
    "code_quality": "good",
    "completeness": "excellent",
    "consistency": "good",
    "safety": "excellent"
  },
  "duration_ms": 15234,
  "human_review": false
}
```

### Implementation Checklist

1. [ ] Create IReviewerStage interface
2. [ ] Implement ReviewerStage
3. [ ] Create IDimension interface
4. [ ] Implement IntentAlignmentDimension
5. [ ] Implement CodeQualityDimension
6. [ ] Implement CompletenessDimension
7. [ ] Implement ConsistencyDimension
8. [ ] Implement SafetyDimension
9. [ ] Create DimensionAssessor
10. [ ] Create SummaryGenerator
11. [ ] Implement human review
12. [ ] Implement override
13. [ ] Add persistence
14. [ ] Write unit tests
15. [ ] Write integration tests
16. [ ] Write E2E tests

### Validation Checklist Before Merge

- [ ] All dimensions work
- [ ] Good work approved
- [ ] Poor work rejected
- [ ] Feedback generated
- [ ] Re-plan cycle works
- [ ] Summary generated
- [ ] Human review works
- [ ] Override works
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** Dimension interface
2. **Phase 2:** Core dimensions
3. **Phase 3:** Assessment logic
4. **Phase 4:** Decision logic
5. **Phase 5:** Summary generation
6. **Phase 6:** Human review
7. **Phase 7:** Override
8. **Phase 8:** Integration

---

**End of Task 012.d Specification**