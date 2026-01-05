## 📝 PROMPT 1: TASK SPECIFICATION EXPANSION (Documentation Only)

**Use this prompt when expanding task stub files from 50 lines to 1200-2500+ lines**

```
Fully expand Task [XXX] from refined-tasks following Task 063 meta-planning specifications and CLAUDE.md requirements.

CONTEXT:
- Task [XXX] is currently a stub file (~50 lines) located at refined-tasks/phase-[XX]-[name]/task-[XXX]-[name].md
- This is a DOCUMENTATION EXPANSION task (no code implementation required at this stage)
- Must be expanded to 1200-2500+ lines following quality standards from Task 042 (2041 lines) and Task 044 (3699 lines)
- Review CLAUDE.md for complete expansion requirements

TIPS TO HELP EXPANSION: 
Write Section-by-Section

To avoid losing work due to token limits, write and save incrementally:

1. Write **Description** section → Save to file
2. Write **Use Cases** section → Append to file
3. Write **User Manual** section → Append to file
4. Write **Acceptance Criteria** section → Append to file
5. Write **Testing Requirements** section → Append to file
6. Write **User Verification** section → Append to file
7. Write **Implementation Prompt** section → Append to file
8. **Verify file completeness** → Run `wc -l [filename]` to check line count (must be ≥1200)

### Step 4: Quality Verification

**Automated Checks:**
```bash
# Check line count (must be >= 1200)
wc -l refined-tasks/phase-XX-*/task-XXX-*.md

# Verify all sections exist
grep -E "^## (Description|Use Cases|User Manual|Acceptance Criteria|Testing Requirements|User Verification|Implementation Prompt)" refined-tasks/phase-XX-*/task-XXX-*.md

# Count acceptance criteria (should be 50-80+)
grep -E "^- \[ \]" refined-tasks/phase-XX-*/task-XXX-*.md | wc -l
```

### Why Comprehensive Specs Matter

**Time Investment vs. ROI:**
- Time to write complete spec: +30 minutes per task
- Time saved in implementation: -5 hours of confusion, rework, bug fixes
- **Net savings: 4.5 hours per task × 109 tasks = 490 hours saved**
- At $100/hour developer rate: **$49,000 project cost savings**

**Quality Impact:**
- Clear specs → Correct implementation → Fewer bugs → Happier customers → Better reviews → More sales
- Incomplete specs → Assumptions → Bugs → Rework → Delayed launch → Lost revenue


EXPANSION REQUIREMENTS (per CLAUDE.md) - ALL 16 SECTIONS REQUIRED:

1. DESCRIPTION (300+ lines MINIMUM):
   - Business value with specific ROI calculations ($X saved, Y hours reduced)
   - Technical approach with architectural decisions fully explained
   - Integration points with existing systems (specific classes, methods, endpoints)
   - Constraints and limitations with workarounds
   - Trade-offs and alternative approaches considered
   - Complete, detailed paragraphs (NO abbreviations like "see above", "etc.", or "...")

2. USE CASES (3+ scenarios, 10-15 lines EACH MINIMUM):
   - Real personas with names, roles, company context, and specific goals
   - Before/after workflow comparisons showing concrete improvement
   - Concrete examples with real numbers/metrics (time saved, errors prevented, cost reduced)
   - Each use case fully written out independently (NO "similar to above" references)
   - Include code examples showing before/after when relevant

3. GLOSSARY (10-20 terms):
   - All domain-specific terms defined
   - Technical jargon explained in plain language
   - Acronyms spelled out with context
   - Table format: | Term | Definition |

4. OUT OF SCOPE (8-15 items):
   - Explicit list of what is NOT included in this task
   - Related features deferred to other tasks (with task IDs)
   - Boundaries clearly defined to prevent scope creep
   - Future enhancements mentioned but excluded from current work

5. FUNCTIONAL REQUIREMENTS (50-100+ items):
   - All functional capabilities listed as FR-001, FR-002, etc.
   - Each requirement testable and specific (not "system should be fast")
   - Use MUST/SHOULD language clearly
   - Organized by subsystem or feature area

6. NON-FUNCTIONAL REQUIREMENTS (15-30 items):
   - Performance requirements with specific targets (< 500ms, 60 FPS)
   - Security requirements (NFR-001, etc.)
   - Scalability targets (handle X concurrent users)
   - Maintainability standards (test coverage %, documentation)
   - Compatibility requirements (OS, browsers, frameworks)

7. USER MANUAL DOCUMENTATION (200-400 lines):
   - Step-by-step configuration guides with screenshots/ASCII mockups
   - Settings and options tables with all possible values
   - Integration setup instructions (exact commands, API keys, etc.)
   - Best practices section (5-10 recommendations)
   - Troubleshooting common issues with complete solutions
   - FAQ section (5-10 questions with detailed answers)

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
- [ ] Line count >= 1200 lines (run: wc -l [task-file]) **1200-2500 lines total** (can exceed for complex features like Task 044's 3,699 lines)

- [ ] **All 8 sections complete** (no abbreviations, placeholders, or "see above" references)
- [ ] **at least 3 use cases with personas, more if deemed relevant** (10-15 lines each minimum)
- [ ] All test cases captured, with descriptions and reason for testing (at the very least, 5-8 unit + 3-5 integration + 3-5 E2E, likely there should be many more test cases identified. )
- [ ] **ASCII mockups in user manual** (when applicable for UI features)
- [ ] **50-80+ acceptance criteria** (comprehensive checklist)
- [ ] File quality matches e-commerce golden standard task sample
- [ ] **12+ implementation steps** with full code examples
- [ ] **No "TODO" or placeholder text**
- [ ] **Technically accurate** (follows .NET best practices)
- [ ] **Follows Clean Architecture** (Domain → Application → Infrastructure → API/UI)
- [ ] **ENABLES AND FOLLOWS TDD** 

Begin full expansion of Task [XXX] now. Work section by section, saving after each section to avoid token limit issues.