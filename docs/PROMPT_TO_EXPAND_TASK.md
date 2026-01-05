## 📝 TASK SPECIFICATION EXPANSION (Documentation Only)

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

1. **Header** - Priority, Tier, Complexity, Phase, Dependencies (complete)
2. **Description** (300+ lines) - Business value with ROI calculations, technical approach with architectural decisions, integration points with specific systems, constraints and limitations, trade-offs explained
3. **Use Cases** (3+ scenarios, 10-15 lines each) - Real personas with names/roles, before/after workflow comparisons, concrete metrics showing improvement
4. **Glossary** (10-20 terms) - All domain-specific terms, technical jargon, acronyms defined with clear explanations
5. **Out of Scope** (8-15 items) - Explicit list of what is NOT included in this task, boundaries clearly defined
6. **Functional Requirements** (50-100+ items) - All functional capabilities listed as FR-001, FR-002, etc. with testable statements
7. **Non-Functional Requirements** (15-30 items) - Performance, security, scalability, maintainability requirements as NFR-001, etc.
8. **User Manual Documentation** (200-400 lines) - Complete guide with step-by-step instructions, ASCII mockups, configuration examples, best practices, troubleshooting
9. **Assumptions** (15-20 items) - Technical assumptions, operational assumptions, integration assumptions explicitly stated
10. **Security Considerations** (5+ threats) - Each threat with risk description, attack scenario, complete mitigation code (not snippets)
11. **Best Practices** (12-20 items) - Organized by category, specific actionable guidance
12. **Troubleshooting** (5+ issues) - Each with Symptoms, Causes, Solutions format including code/commands
13. **Acceptance Criteria** (50-80+ items) - Comprehensive testable checklist across all functional areas
14. **Testing Requirements** (complete test code, 200-400 lines) - Full C# test implementations with Arrange-Act-Assert, realistic test data, all test types
15. **User Verification Steps** (8-10 scenarios, 100-150 lines) - Detailed step-by-step manual testing with complete commands and expected outputs
16. **Implementation Prompt for Claude** (400-600 lines) - Complete code for all entities, services, controllers with full implementations (not stubs)


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

1. **Header** - Priority, Tier, Complexity, Phase, Dependencies (complete)
2. **Description** (300+ lines) - Business value with ROI calculations, technical approach with architectural decisions, integration points with specific systems, constraints and limitations, trade-offs explained
3. **Use Cases** (3+ scenarios, 10-15 lines each) - Real personas with names/roles, before/after workflow comparisons, concrete metrics showing improvement
4. **Glossary** (10-20 terms) - All domain-specific terms, technical jargon, acronyms defined with clear explanations
5. **Out of Scope** (8-15 items) - Explicit list of what is NOT included in this task, boundaries clearly defined
6. **Functional Requirements** (50-100+ items) - All functional capabilities listed as FR-001, FR-002, etc. with testable statements
7. **Non-Functional Requirements** (15-30 items) - Performance, security, scalability, maintainability requirements as NFR-001, etc.
8. **User Manual Documentation** (200-400 lines) - Complete guide with step-by-step instructions, ASCII mockups, configuration examples, best practices, troubleshooting
9. **Assumptions** (15-20 items) - Technical assumptions, operational assumptions, integration assumptions explicitly stated
10. **Security Considerations** (5+ threats) - Each threat with risk description, attack scenario, complete mitigation code (not snippets)
11. **Best Practices** (12-20 items) - Organized by category, specific actionable guidance
12. **Troubleshooting** (5+ issues) - Each with Symptoms, Causes, Solutions format including code/commands
13. **Acceptance Criteria** (50-80+ items) - Comprehensive testable checklist across all functional areas
14. **Testing Requirements** (complete test code, 200-400 lines) - Full C# test implementations with Arrange-Act-Assert, realistic test data, all test types
15. **User Verification Steps** (8-10 scenarios, 100-150 lines) - Detailed step-by-step manual testing with complete commands and expected outputs
16. **Implementation Prompt for Claude** (400-600 lines) - Complete code for all entities, services, controllers with full implementations (not stubs)

1. HEADDER
    - Priority
    - Tier
    - Complexity
    - Phase
    - Dependencies

2. DESCRIPTION (300+ lines MINIMUM):
   - Business value with specific ROI calculations ($X saved, Y hours reduced)
   - Technical approach with architectural decisions fully explained
   - Integration points with existing systems (specific classes, methods, endpoints)
   - Constraints and limitations with workarounds
   - Trade-offs and alternative approaches considered
   - Complete, detailed paragraphs (NO abbreviations like "see above", "etc.", or "...")

3. USE CASES (3+ scenarios, 10-15 lines EACH MINIMUM):
   - Real personas with names, roles, company context, and specific goals
   - Before/after workflow comparisons showing concrete improvement
   - Concrete examples with real numbers/metrics (time saved, errors prevented, cost reduced)
   - Each use case fully written out independently (NO "similar to above" references)
   - Include code examples showing before/after when relevant

4. GLOSSARY (10-20 terms):
   - All domain-specific terms defined
   - Technical jargon explained in plain language
   - Acronyms spelled out with context
   - Table format: | Term | Definition |

5. OUT OF SCOPE (8-15 items):
   - Explicit list of what is NOT included in this task
   - Related features deferred to other tasks (with task IDs)
   - Boundaries clearly defined to prevent scope creep
   - Future enhancements mentioned but excluded from current work

6. FUNCTIONAL REQUIREMENTS (50-100+ items):
   - All functional capabilities listed as FR-001, FR-002, etc.
   - Each requirement testable and specific (not "system should be fast")
   - Use MUST/SHOULD language clearly
   - Organized by subsystem or feature area

7. NON-FUNCTIONAL REQUIREMENTS (15-30 items):
   - Performance requirements with specific targets (< 500ms, 60 FPS)
   - Security requirements (NFR-001, etc.)
   - Scalability targets (handle X concurrent users)
   - Maintainability standards (test coverage %, documentation)
   - Compatibility requirements (OS, browsers, frameworks)

8. USER MANUAL DOCUMENTATION (200-400 lines):
   - Step-by-step configuration guides with screenshots/ASCII mockups
   - Settings and options tables with all possible values
   - Integration setup instructions (exact commands, API keys, etc.)
   - Best practices section (5-10 recommendations)
   - Troubleshooting common issues with complete solutions
   - FAQ section (5-10 questions with detailed answers)

9. ASSUMPTIONS (15-20 items):
   - Technical assumptions (e.g., "System has .NET 7 installed")
   - Operational assumptions (e.g., "Users have admin rights")
   - Integration assumptions (e.g., "External API is available 99.9% uptime")
   - Each assumption clearly stated and justified

10. SECURITY CONSIDERATIONS (5+ threats):
    - Each threat with risk description and potential impact
    - Attack scenario fully detailed
    - Complete mitigation code examples (NO snippets)
    - Testing strategies to verify mitigations

11. BEST PRACTICES (12-20 items):
    - Organized by category (coding, security, performance, UX)
    - Specific, actionable guidance (e.g., "Use async/await for I/O operations")
    - Examples of correct vs. incorrect implementations

12. TROUBLESHOOTING (5+ issues):
    - Each issue with Symptoms, Causes, Solutions format
    - Complete code/commands for solutions (NO placeholders)
    - Common pitfalls and how to avoid them

13. ACCEPTANCE CRITERIA (50-80 items):
   - Core functionality (15-20 specific, testable criteria)
   - Advanced features (10-15 specific criteria)
   - UI/UX requirements (8-10 criteria with specific behaviors)
   - Performance benchmarks (5-8 criteria with EXACT targets: "< 500ms", "60 FPS", "< 2s")
   - Security requirements (5-8 criteria)
   - Testing coverage (5-8 criteria with test counts: "50+ unit tests")
   - Data persistence (3-5 criteria)
   - Documentation (3-5 criteria)
   - Each criterion written as a checkbox item: `- [ ] Specific testable requirement`

14. TESTING REQUIREMENTS (complete code examples, 200-300 lines total):
   - Unit Tests: 5-8 complete C# test methods with full Arrange-Act-Assert code
   - Integration Tests: 3-5 complete test scenarios with setup/teardown code
   - E2E Tests: 3-5 complete user journey test scenarios with full code
   - Performance Tests: 3-4 benchmarks with specific targets and measurement code
   - Regression Tests: 2-3 tests ensuring existing features unaffected (full code)
   - ALL test code written completely (NO "// ... additional tests" placeholders)
   - Use realistic test data and actual class/method names from the feature

15. USER VERIFICATION STEPS (8-10 scenarios, 100-150 lines):
   - Complete step-by-step manual testing instructions
   - Expected outcomes clearly stated for each step
   - Screenshots or ASCII diagrams where helpful
   - Each verification scenario 10-15 lines with full details

16. IMPLEMENTATION PROMPT FOR CLAUDE (12+ steps, 400-600 lines):
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
REMEMBER, if the section is present and has content, we must read the content and determine if it is semantically complete. If it is, we move on to the next section. If it is not, we expand it fully, 
possibly replacing the existing content, possibly expanding upon it, depending on what is already there. LINE COUNT IS NOT ENOUGH TO DETERMINE COMPLETENESS, NOR IS PRESENCE OF THE SECTION ALONE.

1. Read the stub file to understand scope
2. Write the **Header** section first (if not already complete)
3. Write the **Description** section next (300+ lines)
4. Write the **Use Cases** section (3+ scenarios, 10-15 lines each)
5. Write the **Glossary** section (10-20 terms)
6. Write the **Out of Scope** section (8-15 items)
7. Write the **Functional Requirements** section (50-100+ items)
8. Write the **Non-Functional Requirements** section (15-30 items)
9. Write the **User Manual Documentation** section (200-400 lines)
10. Write the **Assumptions** section (15-20 items)
11. Write the **Security Considerations** section (5+ threats)
12. Write the **Best Practices** section (12-20 items)
13. Write the **Troubleshooting** section (5+ issues)
14. Write the **Acceptance Criteria** section (50-80+ items)
15. Write the **Testing Requirements** section (200-400 lines of complete test code)
16. Write the **User Verification Steps** section (8-10 scenarios, 100-150 lines)
17. Write the **Implementation Prompt for Claude** section (400-600 lines of complete code)
18. Run final audit:
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
- [ ] **All sections SEMANTICALLY complete** (it's critically important to remember that presence of a section, or linecount, is not an indication of completion. this is because we could have a description with 300 lines of lorem ipsum, but that doesn't mean we skip and move on; that is semantically useless, so should be caught and replaced with adequate relevant semantically complete text according to our standards.)
- [ ] File quality matches e-commerce golden standard task sample
- [ ] **Technically accurate** (follows .NET best practices)
- [ ] **Follows Clean Architecture** (Domain → Application → Infrastructure → API/UI)
- [ ] **ENABLES AND FOLLOWS TDD** 

Begin full expansion of Task [XXX] now. Work section by section, saving after each section to avoid token limit issues.