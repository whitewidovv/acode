# Prompts for Task Expansion and Implementation

This document contains standardized prompts for working on tasks in this project.

---

## 📝 PROMPT 1: TASK SPECIFICATION EXPANSION (Documentation Only)

**Use this prompt when expanding task stub files from 50 lines to 1200-2500+ lines**

```
Fully expand Task [XXX] from refined-tasks following Task 063 meta-planning specifications and CLAUDE.md requirements.

CONTEXT:
- Task [XXX] is currently a stub file (~50 lines) located at refined-tasks/phase-[XX]-[name]/task-[XXX]-[name].md
- This is a DOCUMENTATION EXPANSION task (no code implementation required at this stage)
- Must be expanded to 1200-2500+ lines following quality standards from Task 042 (2041 lines) and Task 044 (3699 lines)
- Review CLAUDE.md for complete expansion requirements

EXPANSION REQUIREMENTS (per CLAUDE.md):

1. DESCRIPTION (300+ lines):
   - Business value and ROI justification with specific numbers
   - Technical approach with architectural decisions explained
   - Integration points with existing systems (specific endpoints/services)
   - Constraints and limitations
   - Complete, detailed paragraphs (NO abbreviations like "see above", "etc.", or "...")

2. USE CASES (3 scenarios, 10-15 lines EACH):
   - Real personas with names, roles, and specific goals
   - Before/after workflow comparisons showing improvement
   - Concrete examples with real numbers/metrics
   - Each use case fully written out independently (NO "similar to above" references)

3. USER MANUAL DOCUMENTATION (200-400 lines):
   - Step-by-step configuration guides with screenshots/ASCII mockups
   - Settings and options tables with all possible values
   - Integration setup instructions (exact commands, API keys, etc.)
   - Best practices section (5-10 recommendations)
   - Troubleshooting common issues with complete solutions
   **User Manual / Admin Manual** - Create or update user-facing documentation:
  - **Admin Manual** - If feature affects admin panel, document how admins use it
  - **User Manual** - If feature affects storefront, document how customers use it
  - **FAQ** - Add common questions and answers if applicable
  - Provide step-by-step instructions for common tasks

4. ACCEPTANCE CRITERIA (50-80 items):
   - Core functionality (15-20 specific, testable criteria)
   - Advanced features (10-15 specific criteria)
   - UI/UX requirements (8-10 criteria with specific behaviors)
   - Performance benchmarks (5-8 criteria with EXACT targets: "< 500ms", "60 FPS", "< 2s")
   - Security requirements (5-8 criteria)
   - Testing coverage (5-8 criteria with test counts: "50+ unit tests")
   - Data persistence (3-5 criteria)
   - Documentation (3-5 criteria)
   - Each criterion written as a checkbox item: `- [ ] Specific testable requirement`

5. TESTING REQUIREMENTS (complete code examples, 200-300 lines total):
   - Unit Tests: 5-8 complete C# test methods with full Arrange-Act-Assert code
   - Integration Tests: 3-5 complete test scenarios with setup/teardown code
   - E2E Tests: 3-5 complete user journey test scenarios with full code
   - Performance Tests: 3-4 benchmarks with specific targets and measurement code
   - Regression Tests: 2-3 tests ensuring existing features unaffected (full code)
   - ALL test code written completely (NO "// ... additional tests" placeholders)
   - Use realistic test data and actual class/method names from the feature

6. USER VERIFICATION STEPS (8-10 scenarios, 100-150 lines):
   - Complete step-by-step manual testing instructions
   - Expected outcomes clearly stated for each step
   - Screenshots or ASCII diagrams where helpful
   - Each verification scenario 10-15 lines with full details

7. IMPLEMENTATION PROMPT FOR CLAUDE (12+ steps, 400-600 lines):
   - Entity definitions with complete C# code (all properties, navigation properties)
   - Service layer implementations with complete methods (not stubs)
   - API controller endpoints with complete code (all CRUD operations)
   - Blazor/MAUI component examples with complete XAML/Razor code
   - Database migration scripts with full Up/Down methods
   - Each step 30-50 lines with full implementation code (NO snippets or "...")
   - Include validation logic, error handling, logging in code examples

CRITICAL RULES (NO EXCEPTIONS):

❌ NEVER use abbreviations:
- "... (additional tests omitted for brevity)" → Write ALL tests in full
- "... (see Task 001 for pattern)" → Repeat the full pattern/code
- "... (similar to above)" → Write it out completely
- "[Sections 6-10 follow same pattern]" → Write each section fully
- "// ... rest of code" → Include complete code
- "etc." → List all items

✅ ALWAYS write complete content:
- Every test case with full C# code (30-50 lines per test)
- Every use case with 10-15 lines of narrative detail
- Every acceptance criterion listed individually
- Every implementation step with complete, runnable code

WORK IN CHUNKS (to avoid token limits):

Since this file will be 1200-2500+ lines, work section by section:

1. Read the stub file to understand scope
2. Write Description section (300+ lines) → SAVE to file immediately
3. Write Use Cases section (300+ lines) → APPEND to file
4. Write User Manual section (200-400 lines) → APPEND to file
5. Write Acceptance Criteria section (100-150 lines) → APPEND to file
6. Write Testing Requirements section (200-300 lines) → APPEND to file
7. Write User Verification section (100-150 lines) → APPEND to file
8. Write Implementation Prompt section (400-600 lines) → APPEND to file
9. Run final audit:
   - Check line count: `wc -l [filename]` (must be >= 1200)
   - Verify all sections present
   - Check for NO abbreviation phrases
   - Confirm acceptance criteria count (50-80+)

QUALITY TARGETS:
- Minimum: 1200 lines (FLOOR, not target)
- Target: 1500-2000 lines
- Maximum: 2500+ lines (exceed if needed for completeness)
- Reference examples: Task 042 (2041 lines), Task 044 (3699 lines)

TASK STATE MANAGEMENT:
1. Task file is already in refined-tasks/phase-[XX]-[name]/
2. No need to move during expansion (this is documentation only)
3. When expansion complete, verify line count and completeness
4. Git commit with message: "Task [XXX]: Full specification expansion complete ([XXXX] lines)"

FINAL AUDIT CHECKLIST:
- [ ] Line count >= 1200 lines (run: wc -l [task-file])
- [ ] All 7 sections complete with substantive content
- [ ] NO abbreviations ("see above", "etc.", "...", placeholders)
- [ ] All test code written in full (5-8 unit + 3-5 integration + 3-5 E2E)
- [ ] All use cases 10-15 lines each
- [ ] All acceptance criteria individually listed (50-80 items)
- [ ] Implementation prompt has 12+ steps with complete code (400-600 lines)
- [ ] File quality matches Task 042/044 examples

Begin full expansion of Task [XXX] now. Work section by section, saving after each section to avoid token limit issues.
```

---

## 🏗️ PROMPT 2: FEATURE IMPLEMENTATION (Code, Tests, Docs, Educational Chapter)

**Use this prompt when implementing a feature from an expanded task specification**

```
Implement Task [XXX] from refined-tasks following TDD principles and CLAUDE.md definition of done.

CONTEXT:
- Task [XXX] specification is located at: refined-tasks/in-progress/task-[XXX]-[name].md
- This is a FULL IMPLEMENTATION task (code, tests, documentation, educational chapter)
- Follow strict TDD: Write tests FIRST, then implement (Red → Green → Refactor)
- Database migrations should be SKIPPED (manually create entities/implementations instead)
- Review CLAUDE.md for complete definition of done requirements

IMPLEMENTATION WORKFLOW:

1. PREPARATION:
   - Move task to in-progress: `git mv refined-tasks/phase-XX-*/task-XXX-*.md refined-tasks/in-progress/`
   - Create feature branch: `git checkout -b feature/task-XXX-[short-description]`
   - Read complete task specification (may need to read in chunks due to size)
   - Identify all objectives from task file

2. TDD IMPLEMENTATION (for each objective):

   Step A: Write Tests FIRST (Red phase)
   - Write unit tests for business logic (Arrange-Act-Assert pattern)
   - Write integration tests for API endpoints
   - Write E2E tests for user workflows
   - Run tests → Verify they FAIL (red)
   - Target: 80%+ code coverage

   Step B: Implement Feature (Green phase)
   - Create/update domain entities (manually, NO migrations)
   - Implement service layer with business logic
   - Implement repository methods
   - Create API controllers/endpoints
   - Build Blazor/MAUI UI components
   - Run tests → Verify they PASS (green)

   Step C: Refactor (Refactor phase)
   - Clean up code, extract methods, improve naming
   - Add XML documentation comments
   - Remove duplication
   - Run tests → Verify still PASS

3. MANUAL ENTITY/DB WORK (since migrations are skipped):
   - Manually create entity classes in Domain project
   - Manually add DbSet properties to DbContext
   - Manually configure entity relationships in OnModelCreating
   - Manually seed test data in database (if applicable)
   - Document what migration WOULD contain in task notes

4. DOCUMENTATION UPDATES:

   - Task File Updates:
     - Mark objectives complete as you go
     - Document any deviations from original plan
     - Add implementation notes (challenges, decisions)
     - Keep task in sync with actual implementation

   - User Manual (if customer-facing feature):
     - Create/update docs/user-manuals/[feature-name].md
     - Step-by-step usage instructions with screenshots/ASCII mockups
     - Configuration options and settings
     - Troubleshooting section

   - Admin Manual (if admin panel feature):
     - Create/update docs/admin-manuals/[feature-name].md
     - Admin workflow documentation
     - Configuration and management instructions

   - Code Comments:
     - XML documentation on all public APIs
     - Inline comments explaining complex logic
     - Document WHY, not just WHAT


5. FINAL AUDIT (before marking task complete):

   Run these checks:
   ```bash
   # Build succeeds
   dotnet build

   # All tests pass
   dotnet test

   # Code coverage >= 80%
   dotnet test /p:CollectCoverage=true

   # No compiler warnings
   dotnet build --warnaserror

   # Task file line count (should be 1200-2500+)
   wc -l refined-tasks/in-progress/task-[XXX]-*.md

   # Educational chapter exists and is complete
   wc -l docs/educational-chapters/chapter-[XXX]-*.md
   # (should be 1000-2500+ lines = 10,000-25,000 words)
   ```

   Verify Definition of Done checklist (CLAUDE.md):
   - [ ] Feature functions correctly, no bugs
   - [ ] UI looks polished (if applicable)
   - [ ] Code in correct architectural location
   - [ ] Unit tests written and passing (80%+ coverage)
   - [ ] Integration tests written and passing
   - [ ] E2E tests written for critical paths
   - [ ] All tests follow TDD (written BEFORE implementation)
   - [ ] Task file updated with notes and deviations
   - [ ] User manual created/updated (if applicable)
   - [ ] Admin manual created/updated (if applicable)
   - [ ] Code comments and XML docs complete
   - [ ] No compiler or analysis warnings
   - [ ] Educational chapter written and complete (20-50 pages)
   - [ ] All commits made with clear messages
   - [ ] Feature branch pushed to remote

   If ANY item incomplete → Work is NOT done

6. GIT WORKFLOW:

   - Commit per objective:
     ```bash
     git add .
     git commit -m "Task [XXX]: [Objective description]

     - Specific change 1
     - Specific change 2

     Task [XXX] - Objective [X]/[Y]"
     ```

   - Push feature branch:
     ```bash
     git push -u origin feature/task-[XXX]-[short-description]
     ```

   - Create pull request:
     ```bash
     gh pr create --title "Task [XXX]: [Task Name]" \
       --body "Completes all objectives for Task [XXX].

       - All tests passing (80%+ coverage)
       - User/Admin manuals updated
       - Educational chapter written (XX pages)

       See task file for complete details."
     ```

SPECIAL NOTES:

- NO DATABASE MIGRATIONS: Create entities and DbContext updates manually
- Document migration script in task notes for future reference
- Seed database manually if test data needed
- Work in chunks: Read large task files in sections if needed
- Save progress frequently to avoid losing work
- Educational chapter is MANDATORY - feature NOT done without it
- Target audience for educational chapter: Technical school students (associate's degree level)

FINAL REMINDER:

This task is NOT complete until:
1. All code implemented and tests passing (80%+ coverage)
2. User/Admin manuals updated
3. Task file updated with implementation notes
4. Educational chapter written (20-50 pages)
5. All commits pushed
6. Pull request created

Review CLAUDE.md definition of done. Verify EVERY checkbox item. No exceptions.

Begin implementation of Task [XXX] now following strict TDD workflow.
```

---

## 🔄 PROMPT 3: CONTINUATION PROMPT (When Work is Interrupted)

**Use this when Claude pauses and asks what to do next**

```
Continue with current work following CLAUDE.md standards.

REMINDER OF REQUIREMENTS:

If expanding task specification:
- Work section-by-section (Description → Use Cases → User Manual → Acceptance Criteria → Testing → Verification → Implementation Prompt)
- Save after EACH section (use Append to avoid overwriting)
- Target: 1200-2500+ lines total
- NO abbreviations or "see above" references
- Write ALL test code in full
- Report progress: "Completed [Section Name], [XXX] lines so far, next: [Next Section]"

If implementing feature:
- Follow TDD: Tests first → Implementation → Refactor
- Update documentation as you go (task file, user manuals, admin manuals)
- CRITICAL: Write educational chapter after implementation complete
- Educational chapter: 20-50 pages, technical school level audience
- No database migrations (create entities manually)
- Report progress: "Completed Objective [X]/[Y]: [Description], Tests: [Pass/Fail], Coverage: [XX]%"

WHAT TO DO NOW:

1. Check where you left off
2. Continue from that point
3. If you completed a major section, SAVE/APPEND to file
4. Report current status and next step
5. If hitting token limits, save progress and tell me

DO NOT abbreviate. DO NOT skip sections. DO NOT use placeholders.

Continue now.
```

---

## 📋 Quick Reference

### Which Prompt to Use?

| Scenario | Prompt |
|----------|--------|
| Expanding task stub (50 lines → 1200-2500 lines) | **PROMPT 1: Task Specification Expansion** |
| Implementing feature (code + tests + docs + chapter) | **PROMPT 2: Feature Implementation** |
| Claude paused and asking what to do | **PROMPT 3: Continuation Prompt** |

### Task Numbers to Replace

When using prompts, replace:
- `[XXX]` with task number (e.g., 064, 065, 078, etc.)
- `[XX]` with phase number (e.g., 14, 15, 16)
- `[name]` with task name slug (e.g., maui-project-setup)
- `[short-description]` with branch name (e.g., maui-setup)

### Example Usage

**Expanding Task 064:**
```
Fully expand Task 064 from refined-tasks following Task 063 meta-planning specifications...
```

**Implementing Task 078:**
```
Implement Task 078 from refined-tasks following TDD principles and CLAUDE.md definition of done...
```

---

## 🎯 Key Success Criteria

**For Expansion (PROMPT 1):**
- [ ] Line count >= 1200 (preferably 1500-2000)
- [ ] All 7 sections complete
- [ ] NO abbreviations or placeholders
- [ ] All test code examples complete
- [ ] Quality matches Task 042 (2041 lines) or Task 044 (3699 lines)

**For Implementation (PROMPT 2):**
- [ ] TDD workflow followed (tests before code)
- [ ] 80%+ code coverage
- [ ] User/Admin manuals updated
- [ ] Task file updated with notes
- [ ] Educational chapter written (20-50 pages)
- [ ] All CLAUDE.md definition of done items checked

---

**Remember**: Quality over speed. Completeness over efficiency. Future developers and students depend on your thoroughness today.
