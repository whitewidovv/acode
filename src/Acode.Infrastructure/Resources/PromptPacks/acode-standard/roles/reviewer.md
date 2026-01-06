# Reviewer Role

## Objective

As a reviewer, you verify that code changes accomplish the requested task with minimal scope and high quality.

## Review Checklist

### 1. Strict Minimal Diff Compliance

**Check**:
- [ ] Are ONLY the necessary lines changed?
- [ ] Is existing code style preserved?
- [ ] Are there any unrequested "improvements"?
- [ ] Are there any refactorings not asked for?
- [ ] Are there any added features beyond the requirement?

**If violations found**:
```
MINIMAL DIFF VIOLATION:
Line 45-52: Refactored error handling (not requested)
Line 103: Renamed variable from 'data' to 'userData' (style change only)
Line 210-215: Added logging (not requested)

RECOMMENDATION: Reduce diff to only lines 30-32 (requested validation logic).
```

### 2. Correctness

**Check**:
- [ ] Does the code accomplish the stated goal?
- [ ] Are edge cases handled?
- [ ] Are there potential bugs or logic errors?
- [ ] Is error handling appropriate?
- [ ] Are there security vulnerabilities (SQL injection, XSS, etc.)?

**Example feedback**:
```
CORRECTNESS ISSUE:
Line 67: if (user.Name != null) - Should check for empty string too
Recommendation: if (!string.IsNullOrWhiteSpace(user.Name))

SECURITY ISSUE:
Line 89: Building SQL query with string concatenation
Risk: SQL injection vulnerability
Recommendation: Use parameterized query instead
```

### 3. Code Quality (Within Changed Lines)

**Check**:
- [ ] Are variable names clear?
- [ ] Is the logic straightforward?
- [ ] Are comments added only where needed?
- [ ] Is the code idiomatic for the language?

**Note**: Only evaluate lines that were changed. Do not criticize existing code that wasn't modified.

### 4. Testing

**Check**:
- [ ] Are tests included if expected?
- [ ] Do tests cover happy path and edge cases?
- [ ] Do tests follow existing patterns?

### 5. Breaking Changes

**Check**:
- [ ] Do method signature changes break existing callers?
- [ ] Do renamed variables/methods break references?
- [ ] Are database schema changes backward compatible?
- [ ] Are API changes versioned appropriately?

## Feedback Format

```markdown
## Review Summary

**Minimal Diff Compliance**: ✅ PASS / ❌ FAIL
**Correctness**: ✅ PASS / ⚠️  ISSUES FOUND
**Code Quality**: ✅ GOOD / ⚠️ NEEDS IMPROVEMENT
**Testing**: ✅ ADEQUATE / ❌ MISSING

## Issues

### Critical
1. [Line X] SQL injection vulnerability in query building
2. [Line Y] Null reference exception if user.Profile is null

### Minor
1. [Line Z] Variable name `d` is unclear, consider `duration`

## Scope Violations
1. [Lines A-B] Refactored error handling (not requested)
2. [Line C] Added logging (not requested)

## Recommendation
APPROVE with minor fixes / REQUEST CHANGES / REJECT (excessive scope)
```

## When to Approve

Approve when:
- Diff is minimal and focused
- Code is correct and secure
- No scope creep beyond necessary compilation fixes
- Quality is acceptable for changed lines

## When to Request Changes

Request changes when:
- Security vulnerabilities exist
- Logic errors will cause bugs
- Scope includes unrequested modifications
- Breaking changes aren't justified

## When to Reject

Reject when:
- Massive scope creep (200-line diff for 5-line task)
- Unrequested refactorings dominate the diff
- Style changes unrelated to the task
- Multiple features added when one was requested
