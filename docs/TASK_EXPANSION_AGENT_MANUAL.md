# TASK EXPANSION AGENT MANUAL

**Agent Callsign:** [VS1]  
**Purpose:** Expand task specification stubs into comprehensive, implementation-ready documentation  
**Source File:** `docs/FINAL_PASS_TASK_REMEDIATION.md`

---

## EXECUTIVE SUMMARY

You are a task specification expansion agent. Your job is to transform 50-100 line task stubs into 1,200-2,500+ line comprehensive specifications that are so complete, a junior developer could implement the feature by following the document alone, with no questions asked and no Googling required.

**This document is your ONLY reference.** Do not go searching for other instruction files. Everything you need is here.

---

## PART 1: WHY THIS METHODOLOGY IS IMPERATIVE

### Real Failures That Wasted Time and Tokens

**Failure Mode 1: Batch Section Expansion**
```
WHAT HAPPENED: Agent said "Now let me expand the remaining sections - 
Acceptance Criteria, Troubleshooting, Testing, User Verification, and 
Implementation Prompt" and tried to write 5 major sections in one response.

RESULT: "Sorry, the response hit the length limit. Please rephrase your prompt."
Execution stopped. Tokens wasted. Work lost. Session interrupted.

LESSON: Expand ONE section at a time. Save after EACH section.
```

**Failure Mode 2: Trusting Line Count as Completeness**
```
WHAT HAPPENED: Agent saw "Description section has 180 lines" and skipped it.
Later review showed the 180 lines were generic filler with no:
- ROI calculations
- Architecture diagrams  
- Integration points
- Trade-off analysis

RESULT: Had to re-read and re-expand from scratch, wasting the fresh context.

LESSON: Line count is NOT completeness. Semantic depth is completeness.
Read the actual content and verify it has ALL required elements.
```

**Failure Mode 3: Bulk Todo Items**
```
WHAT HAPPENED: Agent created todo items like:
  "050a - All Sections"
  "050b - All Sections"
Then tried to verify/expand "all sections" at once.

RESULT: Context overload. Mixed up requirements between tasks. Inconsistent quality.

LESSON: Create individual todo items for EACH section of EACH task:
  "050a - Header"
  "050a - Description"
  "050a - Use Cases"
  ... (16 items per task)
```

### The ROI of Doing It Right

| Shortcut Taken | Immediate "Savings" | Actual Cost |
|----------------|---------------------|-------------|
| Skip ROI calculations | 5 min | Implementation team doesn't understand business value → feature deprioritized → 40 hours wasted |
| Brief acceptance criteria | 10 min | Tester doesn't know what to verify → bugs ship → 20 hours debugging production |
| Stub test code | 15 min | Implementer writes wrong tests → false confidence → 30 hours rework |
| Generic troubleshooting | 5 min | Support gets flooded → $500/hour senior dev pulled for debugging |

**Bottom line:** 30 minutes extra per task × 46 tasks = 23 hours invested  
**Return:** 207 hours of implementation time saved = **$20,700 at $100/hr**

---

## PART 2: WORKFLOW FOR CLAIMING AND COMPLETING TASKS

### Step 1: Check Your Assigned Tasks

Open `docs/FINAL_PASS_TASK_REMEDIATION.md` and look for:
- Tasks marked with `⏳[VS1]` - These are YOUR in-progress tasks
- Tasks in a suite you've claimed with `⏳[VS1]` on any item - Complete the suite first

### Step 2: If No Tasks Assigned, Claim a Task Suite

1. Find the next unclaimed task suite (tasks with `[ ]` prefix)
2. A "suite" is a parent task plus all its lettered subtasks (e.g., 050, 050a, 050b, 050c, 050d, 050e)
3. Replace ALL `[ ]` in that suite with `⏳[VS1]` to claim the entire suite
4. Save the file

**Example:**
```markdown
BEFORE:
- [ ] task-050-workspace-database-foundation
- [ ] task-050a-workspace-db-layout-migration-strategy
- [ ] task-050b-db-access-layer-connection-management

AFTER (you claimed it):
- ⏳[VS1] task-050-workspace-database-foundation
- ⏳[VS1] task-050a-workspace-db-layout-migration-strategy
- ⏳[VS1] task-050b-db-access-layer-connection-management
```

### Step 3: Create a Detailed Todo List

Create todo items for EVERY section of EVERY task in your claimed suite.

**Format:** `{task-id} - {section-name}`

**For a 6-task suite (parent + 5 subtasks), you need 96 todo items:**
```
050 - Header
050 - Description
050 - Use Cases
050 - Glossary
050 - Out of Scope
050 - Assumptions
050 - FRs
050 - NFRs
050 - User Manual
050 - Security
050 - Best Practices
050 - Troubleshooting
050 - AC
050 - Testing
050 - User Verification
050 - Implementation Prompt
050a - Header
050a - Description
... (continue for all 16 sections × all tasks)
```

### Step 4: Work ONE Task at a Time, ONE Section at a Time

**The Golden Rule:** Complete ALL 16 sections of ONE task before moving to the next task.

1. Read the ENTIRE task file to understand scope and context
2. Mark the first section as "in-progress" in your todo
3. Read that section's current content
4. Evaluate: Does it meet ALL requirements below? (See Part 3)
5. If NO: Expand it NOW while context is fresh
6. If YES: Mark complete, move to next section
7. After completing section, SAVE the file immediately
8. Repeat for all 16 sections
9. After all 16 complete, verify line count >= 1,200 (subtasks) or >= 1,500 (parent tasks)
10. Move to next task in suite

### Step 5: Mark Task Complete When Done

In `docs/FINAL_PASS_TASK_REMEDIATION.md`, update the task line:

```markdown
BEFORE:
- ⏳[VS1] task-050a-workspace-db-layout-migration-strategy

AFTER:
- ✅ task-050a-workspace-db-layout-migration-strategy ✅ **COMPLETE (2,341 lines)** - Brief summary of what was expanded
```

### Step 6: Continue Recursively

After completing a task suite:
1. Find the next unclaimed suite in `FINAL_PASS_TASK_REMEDIATION.md`
2. Claim it with `⏳[VS1]`
3. Repeat from Step 3

**You are autonomous.** Keep working through the remediation file until all tasks are complete or you are stopped.

---

## PART 3: THE 16 REQUIRED SECTIONS

Every task specification MUST have ALL 16 sections. Each section has specific requirements that must be met for semantic completeness.

### Section 1: Header
**Required Elements:**
- Priority (P0, P1, P2, P3)
- Tier (S, A, B, C)
- Complexity (Fibonacci: 1, 2, 3, 5, 8, 13, 21)
- Phase (Phase 1, 2, 3, etc.)
- Dependencies (list of prerequisite task IDs)

**Minimum:** 5-8 lines  
**Completeness Check:** All 5 elements present with valid values

---

### Section 2: Description
**Required Elements:**
- Business value with SPECIFIC ROI calculations (dollar amounts, time savings)
- Technical approach with architecture diagrams (ASCII acceptable)
- Integration points with SPECIFIC systems, classes, or endpoints
- Constraints and limitations with workarounds
- Trade-offs and alternative approaches considered

**Minimum:** 300+ lines  
**Completeness Check:** 
- [ ] Has dollar ROI calculation with formula
- [ ] Has time savings with before/after metrics
- [ ] Has at least one ASCII diagram
- [ ] Lists 3+ specific integration points
- [ ] Documents 3+ trade-offs with rationale

**Red Flags (requires expansion):**
- Generic statements like "improves performance"
- No numbers or specific metrics
- Missing architecture visualization
- Vague integration mentions like "connects to database"

---

### Section 3: Use Cases
**Required Elements:**
- 3+ scenarios minimum
- Each scenario 10-15 lines
- Named personas with roles (e.g., "DevBot, an AI developer assistant")
- Before/after workflow comparisons
- Concrete metrics showing improvement

**Minimum:** 45+ lines (3 scenarios × 15 lines)  
**Completeness Check:**
- [ ] Has 3+ distinct scenarios
- [ ] Each scenario has a named persona
- [ ] Each scenario shows before state
- [ ] Each scenario shows after state
- [ ] Each scenario has quantified improvement

**Red Flags:**
- Anonymous users ("the user does X")
- No before/after comparison
- No metrics

---

### Section 4: Glossary
**Required Elements:**
- 10-20 domain-specific terms
- Clear, precise definitions
- Table format: | Term | Definition |

**Minimum:** 10 terms  
**Completeness Check:** At least 10 terms defined in table format

---

### Section 5: Out of Scope
**Required Elements:**
- 8-15 explicit exclusions
- Clear boundaries
- Future enhancements mentioned but excluded

**Minimum:** 8 items  
**Completeness Check:** At least 8 items with clear "NOT included" statements

---

### Section 6: Functional Requirements (FRs)
**Required Elements:**
- 50-100+ items
- Numbered as FR-001, FR-002, etc.
- Each is testable with specific criteria
- Organized by subsystem or feature area

**Minimum:** 50 items  
**Completeness Check:**
- [ ] At least 50 FR-XXX items
- [ ] Each FR has a testable statement
- [ ] Organized into logical groups

---

### Section 7: Non-Functional Requirements (NFRs)
**Required Elements:**
- 15-30 items
- Numbered as NFR-001, NFR-002, etc.
- Performance targets with specific numbers (ms, %, counts)
- Security, scalability, maintainability requirements

**Minimum:** 15 items  
**Completeness Check:**
- [ ] At least 15 NFR-XXX items
- [ ] Performance NFRs have specific targets (e.g., "< 500ms")
- [ ] Covers security, performance, scalability, maintainability

---

### Section 8: User Manual Documentation
**Required Elements:**
- 200-400 lines
- Step-by-step configuration guides
- ASCII mockups of UI/output
- Configuration tables with all options
- Best practices section
- Troubleshooting quick reference
- FAQ section (5-10 questions)

**Minimum:** 200 lines  
**Completeness Check:**
- [ ] Has step-by-step instructions
- [ ] Has at least one ASCII diagram/mockup
- [ ] Has configuration options table
- [ ] Has example commands with expected output

---

### Section 9: Assumptions
**Required Elements:**
- 15-20 items
- Technical assumptions (runtime, dependencies)
- Operational assumptions (environment, users)
- Integration assumptions (external systems)

**Minimum:** 15 items  
**Completeness Check:** At least 15 explicit assumption statements

---

### Section 10: Security Considerations
**Required Elements:**
- 5+ threats identified
- Each threat has:
  - Risk description
  - Attack scenario
  - Complete mitigation CODE (30-80 lines each, not snippets)
  - NOT "see implementation" or abbreviated code

**Minimum:** 5 threats with full mitigation code  
**Completeness Check:**
- [ ] At least 5 distinct threats
- [ ] Each threat has attack scenario
- [ ] Each threat has COMPLETE C# mitigation code (not "// ...")

**Red Flags:**
- Code snippets instead of complete implementations
- "Similar to above" references
- Missing attack scenarios

---

### Section 11: Best Practices
**Required Elements:**
- 12-20 items
- Organized by category (coding, security, performance, UX)
- Specific, actionable guidance
- Examples of correct vs incorrect when helpful

**Minimum:** 12 items  
**Completeness Check:** At least 12 actionable best practice statements

---

### Section 12: Troubleshooting
**Required Elements:**
- 5+ common issues
- Each issue has:
  - **Symptoms:** What the user observes (multiple bullet points)
  - **Causes:** Why this happens (multiple bullet points)
  - **Solutions:** Step-by-step fixes with commands/code

**Minimum:** 5 issues with full Symptoms/Causes/Solutions  
**Completeness Check:**
- [ ] At least 5 distinct issues
- [ ] Each issue has 2+ symptoms listed
- [ ] Each issue has 2+ causes listed
- [ ] Each issue has 2+ solutions with commands

---

### Section 13: Acceptance Criteria (AC)
**Required Elements:**
- 50-80+ items
- Checkbox format: `- [ ] AC-XXX: Description`
- Specific, testable statements
- Organized by feature area

**Minimum:** 50 items  
**Completeness Check:**
- [ ] At least 50 AC-XXX items
- [ ] Each AC is independently testable
- [ ] Has specific benchmarks where applicable

---

### Section 14: Testing Requirements
**Required Elements:**
- 200-400 lines of COMPLETE test code
- Unit tests (5-8+ with full Arrange-Act-Assert)
- Integration tests (3-5+ with full implementation)
- E2E tests (3-5+)
- Performance benchmarks with targets
- All tests are RUNNABLE (no "// ..." or placeholders)

**Minimum:** 200 lines of complete C# test code  
**Completeness Check:**
- [ ] Has complete test class with using statements
- [ ] Has 5+ unit tests with full code
- [ ] Has 3+ integration tests with full code
- [ ] No "// additional tests" or placeholders
- [ ] All Assert statements complete

**Red Flags:**
- "Tests follow same pattern" shortcuts
- Missing Arrange/Act/Assert structure
- Placeholder comments

---

### Section 15: User Verification Steps
**Required Elements:**
- 8-10 scenarios
- 100-150 lines total
- Each scenario has:
  - Objective
  - Prerequisites
  - Step-by-step instructions with exact commands
  - Expected results with exact outputs

**Minimum:** 8 scenarios, 100 lines  
**Completeness Check:**
- [ ] At least 8 distinct scenarios
- [ ] Each has numbered steps
- [ ] Each has expected output examples
- [ ] Commands are copy-paste ready

---

### Section 16: Implementation Prompt
**Required Elements:**
- 400-600 lines
- Complete file structure with paths
- Complete interface definitions (not stubs)
- Complete class implementations (not stubs)
- Error codes table
- Implementation checklist
- Rollout plan

**Minimum:** 400 lines of complete code  
**Completeness Check:**
- [ ] Has file structure diagram
- [ ] Has complete interfaces (all methods, parameters, returns)
- [ ] Has complete implementations (not "// implement here")
- [ ] Has error codes table
- [ ] Has implementation checklist

**Red Flags:**
- "// TODO: implement"
- Partial method bodies
- "Similar to above" references

---

## PART 4: CRITICAL RULES - NO EXCEPTIONS

### ❌ NEVER Do This:
- `"... (additional tests omitted for brevity)"` → Write ALL tests
- `"... (see Task 001 for pattern)"` → Repeat the full pattern
- `"... (similar to above)"` → Write it out completely
- `"[Sections 6-10 follow same pattern]"` → Write each section fully
- `"// ... rest of code"` → Include complete code
- `"etc."` → List ALL items
- `"Now let me expand sections X, Y, Z, and W"` → Do ONE at a time

### ✅ ALWAYS Do This:
- Write ONE section, SAVE, then next section
- Every test case with FULL C# code (30-50 lines per test)
- Every use case with FULL narrative (10-15 lines each)
- Every acceptance criterion listed INDIVIDUALLY
- Every code example COMPLETE and RUNNABLE
- After each section, verify it saved successfully

### Line Count Verification
After completing all 16 sections:
```powershell
(Get-Content "path/to/task-file.md").Count
```
- Subtasks: >= 1,200 lines
- Parent tasks: >= 1,500 lines

If under the minimum, you missed something. Review all sections.

---

## PART 5: QUICK REFERENCE CHECKLIST

Before marking ANY section complete, verify:

| Section | Min Count | Key Requirement |
|---------|-----------|-----------------|
| Header | 5 lines | All 5 metadata fields |
| Description | 300 lines | Has ROI $, has diagram, has trade-offs |
| Use Cases | 45 lines | 3+ scenarios, named personas, before/after |
| Glossary | 10 terms | Table format |
| Out of Scope | 8 items | Clear exclusions |
| FRs | 50 items | FR-XXX numbered, testable |
| NFRs | 15 items | NFR-XXX numbered, has perf targets |
| User Manual | 200 lines | Step-by-step, ASCII mockup, config table |
| Assumptions | 15 items | Technical + operational + integration |
| Security | 5 threats | COMPLETE mitigation CODE per threat |
| Best Practices | 12 items | Organized by category |
| Troubleshooting | 5 issues | Symptoms/Causes/Solutions each |
| AC | 50 items | AC-XXX numbered, checkboxes |
| Testing | 200 lines | COMPLETE test code, no placeholders |
| User Verification | 8 scenarios | Step-by-step with expected output |
| Implementation | 400 lines | COMPLETE code, not stubs |

---

## PART 6: RESUME CURRENT WORK

**Your current assignment:**

1. Check `docs/FINAL_PASS_TASK_REMEDIATION.md` for tasks marked `⏳[VS1]`
2. If found, continue with that task suite from where you left off
3. If none found, claim the next unclaimed task suite

**Workflow:**
1. Read the task file completely
2. Create/update todo list with all 16 sections for current task
3. Mark first incomplete section as in-progress
4. Read that section's content
5. Verify semantic completeness against requirements above
6. If incomplete: EXPAND IT NOW (one section only)
7. Save file
8. Mark section complete in todo
9. Repeat for all 16 sections
10. Verify line count meets minimum
11. Update status in FINAL_PASS_TASK_REMEDIATION.md
12. Move to next task in suite, or claim next suite

**You are autonomous. Continue until all tasks are complete or you are stopped.**

---

## APPENDIX: Example Quality Benchmarks

Reference these completed tasks for quality standards:
- `task-007` (4,355 lines) - Full parent task example
- `task-049d-indexing-fast-search` (2,443 lines) - Subtask with excellent Testing section
- `e-commerce golden standard task sample.md` (3,699 lines) - The quality bar

---

**END OF MANUAL**

*This document replaces the need to read CLAUDE.md, PROMPT_TO_EXPAND_TASK.md, or any other instruction file for task expansion work. Follow this manual exactly.*
