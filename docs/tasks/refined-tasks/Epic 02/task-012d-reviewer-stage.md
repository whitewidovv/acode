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

```
Tests/Unit/Orchestration/Stages/Reviewer/
├── ReviewerStageTests.cs
│   ├── Should_Load_Full_Context()
│   ├── Should_Assess_All_Dimensions()
│   ├── Should_Approve_Good_Work()
│   └── Should_Reject_Poor_Work()
│
├── DimensionTests.cs
│   ├── Should_Assess_Intent_Alignment()
│   ├── Should_Assess_Code_Quality()
│   ├── Should_Assess_Completeness()
│   ├── Should_Assess_Consistency()
│   └── Should_Assess_Safety()
│
├── SummaryGeneratorTests.cs
│   ├── Should_Generate_Summary()
│   ├── Should_Include_Changes()
│   └── Should_Include_Notes()
│
└── HumanReviewTests.cs
    ├── Should_Prompt_When_Configured()
    ├── Should_Auto_When_Configured()
    └── Should_Handle_Override()
```

### Integration Tests

```
Tests/Integration/Orchestration/Stages/Reviewer/
├── ReviewerIntegrationTests.cs
│   ├── Should_Review_Real_Task()
│   └── Should_Persist_Summary()
│
└── CycleIntegrationTests.cs
    ├── Should_Cycle_To_Planner()
    └── Should_Escalate_At_Limit()
```

### E2E Tests

```
Tests/E2E/Orchestration/Stages/Reviewer/
├── ReviewerE2ETests.cs
│   ├── Should_Complete_Good_Task()
│   ├── Should_Reject_And_Retry()
│   └── Should_Handle_Override()
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

### SummaryGenerator

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Reviewer;

public sealed class SummaryGenerator
{
    public ReviewSummary Generate(
        ReviewContext context,
        ReviewDecision decision,
        IReadOnlyList<DimensionResult> dimensions)
    {
        var changes = ExtractChanges(context.ExecutionResults);
        var notes = GenerateNotes(dimensions);
        
        return new ReviewSummary(
            context.Session.Id,
            context.Request.Goal,
            changes,
            notes,
            decision,
            dimensions.ToDictionary(d => d.Name, d => d.Level));
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