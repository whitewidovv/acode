## 🚨 CRITICAL: TASK SPECIFICATION EXPANSION REQUIREMENTS 🚨

**IMPERATIVE - NO SHORTCUTS, NO STREAMLINING, NO EXCEPTIONS**

When expanding task specifications from stubs into comprehensive specifications:

### Rule #1: Complete Every Section Fully (1200 Lines Minimum)

Each refined task MUST include ALL 16 sections with NO abbreviations:

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

**Target Length:** 1200 lines MINIMUM for subtasks, 1500+ for parent tasks. Golden standard example (e-commerce) is 3,699 lines. The minimum is a FLOOR, not a target. Large files must be read and written in chunks to avoid token limits.

These are the quality standards. Match or exceed them.

### Rule #2: No Abbreviations or Placeholders

**❌ NEVER DO THIS:**
- "... (additional tests omitted for brevity)"
- "... (see Task 001 for pattern)"
- "... (similar to above)"
- "[Sections 6-10 follow same pattern as Section 5]"
- "**END OF TASK (Abbreviated for space)**"
- "... focusing on essentials only"
- "... streamlined for token efficiency"
- "// ... additional code here"
- "<!-- More examples follow same pattern -->"

**✅ ALWAYS DO THIS:**
- Write every test case name expected to see in final implementation
- Write every use case with full detail (10-15 lines each minimum)
- Write every verification step with complete instructions
- Write every code example in full (not snippets or "...")
- Include ALL 50+ acceptance criteria items individually

### Rule #3: Work Section-by-Section to Avoid Loss

To prevent losing work due to token limits, write and save incrementally:

1. Write **Description** section (300+ lines) → Save to file
2. Write **Use Cases** section (3+ scenarios) → Append to file
3. Write **Glossary** section (10-20 terms) → Append to file
4. Write **Out of Scope** section (8-15 items) → Append to file
5. Write **Functional Requirements** section (50-100+ items) → Append to file
6. Write **Non-Functional Requirements** section (15-30 items) → Append to file
7. Write **User Manual** section (200-400 lines) → Append to file
8. Write **Assumptions** section (15-20 items) → Append to file
9. Write **Security Considerations** section (5+ threats with code) → Append to file
10. Write **Best Practices** section (12-20 items) → Append to file
11. Write **Troubleshooting** section (5+ issues) → Append to file
12. Write **Acceptance Criteria** section (50-80+ items) → Append to file
13. Write **Testing Requirements** section (200-400 lines complete code) → Append to file
14. Write **User Verification** section (8-10 scenarios) → Append to file
15. Write **Implementation Prompt** section (400-600 lines complete code) → Append to file
16. **Verify semantic completeness** → Check each section has required depth, not just presence, reattempt expansion of a section if needed
17. **Verify line count** → Run line count check (must be >= 1500)
18. **Mark task as completed** → Update tracking file

If interrupted mid-task, resume from last completed section.

### Why This Is Absolutely Critical

**Comprehensive task specifications are the blueprint for implementation.** Cutting corners now means:

1. **Bugs and Missing Features**: If validation rules aren't documented, they won't be implemented. If edge cases aren't listed in acceptance criteria, they won't be tested. Shortcuts in specs = bugs in production.

2. **Developer Confusion**: Another Claude instance (or human developer) implementing this task months later needs COMPLETE guidance. "See above for pattern" doesn't help when "above" is in a different context window.

3. **Testing Gaps**: Incomplete test sections mean features ship without coverage. "Additional tests omitted" = untested code paths = production failures.

4. **Technical Debt**: Brief specs force developers to make assumptions. Different assumptions = inconsistent implementation. Comprehensive specs prevent this.

5. **Lost Knowledge**: Detailed specs serve as evergreen documentation. Future developers (6 months, 2 years later) can understand design decisions. Brief specs lose context.

6. **Client/Stakeholder Trust**: The user requested comprehensive specs for a reason - they want quality, completeness, predictability. Delivering abbreviated specs breaks trust.

7. **Cost of Rework**: Fixing unclear requirements during implementation costs 10-100x more than getting specs right upfront. An extra 30 minutes writing complete acceptance criteria saves 5 hours of debugging later.

**ROI Calculation:**
- Time to write complete spec: +30 minutes per task
- Time saved in implementation: -5 hours of confusion, rework, bug fixes
- Net savings: 4.5 hours per task × 46 tasks = 207 hours saved
- At $100/hour developer rate: **$20,700 project cost savings**

### Enforcement

**Automated Checks** (run before marking task complete):
```bash
# Check line count (must be >= 1000)
wc -l /completed/task-[XXX]-[name].md

# Verify all sections exist
grep -E "^## (Description|Use Cases|User Manual|Acceptance Criteria|Testing Requirements|User Verification|Implementation Prompt)" /completed/task-[XXX]-[name].md

# Count acceptance criteria (should be 50-80+)
grep -E "^- \[ \]" /completed/task-[XXX]-[name].md | wc -l
```

**Quality Review Checklist:**
- [ ] Line count >= 1000 (preferably 1500-2000)
- [ ] All 8 sections present with substantive content
- [ ] No "see above" or abbreviation phrases found
- [ ] All expected test cases are listed
- [ ] Each use case is 10-15+ lines
- [ ] Acceptance criteria count is 50-80+ items
- [ ] Implementation prompt has 12+ steps with complete code

**No Exceptions**: Even for "simple" tasks - simplicity in implementation still requires comprehensive documentation for maintenance, testing, and future enhancement.

**Remember:** You are not being judged on speed or token efficiency. You are being judged on completeness, accuracy, and enabling future success. Take the time to do it right the first time.## 🚨 CRITICAL: TASK SPECIFICATION EXPANSION REQUIREMENTS 🚨

**IMPERATIVE - NO SHORTCUTS, NO STREAMLINING, NO EXCEPTIONS**

When expanding task specifications from stubs into comprehensive specifications:

### Rule #1: Complete Every Section Fully (1200 Lines Minimum)

Each refined task MUST include ALL 16 sections with NO abbreviations:

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

**Target Length:** 1200 lines MINIMUM for subtasks, 1500+ for parent tasks. Golden standard example (e-commerce) is 3,699 lines. The minimum is a FLOOR, not a target. Large files must be read and written in chunks to avoid token limits.

These are the quality standards. Match or exceed them.

### Rule #2: No Abbreviations or Placeholders

**❌ NEVER DO THIS:**
- "... (additional tests omitted for brevity)"
- "... (see Task 001 for pattern)"
- "... (similar to above)"
- "[Sections 6-10 follow same pattern as Section 5]"
- "**END OF TASK (Abbreviated for space)**"
- "... focusing on essentials only"
- "... streamlined for token efficiency"
- "// ... additional code here"
- "<!-- More examples follow same pattern -->"

**✅ ALWAYS DO THIS:**
- Write every test case name expected to see in final implementation
- Write every use case with full detail (10-15 lines each minimum)
- Write every verification step with complete instructions
- Write every code example in full (not snippets or "...")
- Include ALL 50+ acceptance criteria items individually

### Rule #3: Work Section-by-Section to Avoid Loss

To prevent losing work due to token limits, write and save incrementally:

1. Write **Description** section (300+ lines) → Save to file
2. Write **Use Cases** section (3+ scenarios) → Append to file
3. Write **Glossary** section (10-20 terms) → Append to file
4. Write **Out of Scope** section (8-15 items) → Append to file
5. Write **Functional Requirements** section (50-100+ items) → Append to file
6. Write **Non-Functional Requirements** section (15-30 items) → Append to file
7. Write **User Manual** section (200-400 lines) → Append to file
8. Write **Assumptions** section (15-20 items) → Append to file
9. Write **Security Considerations** section (5+ threats with code) → Append to file
10. Write **Best Practices** section (12-20 items) → Append to file
11. Write **Troubleshooting** section (5+ issues) → Append to file
12. Write **Acceptance Criteria** section (50-80+ items) → Append to file
13. Write **Testing Requirements** section (200-400 lines complete code) → Append to file
14. Write **User Verification** section (8-10 scenarios) → Append to file
15. Write **Implementation Prompt** section (400-600 lines complete code) → Append to file
16. **Verify semantic completeness** → Check each section has required depth, not just presence, reattempt expansion of a section if needed
17. **Verify line count** → Run line count check (must be >= 1500)
18. **Mark task as completed** → Update tracking file

If interrupted mid-task, resume from last completed section.

### Why This Is Absolutely Critical

**Comprehensive task specifications are the blueprint for implementation.** Cutting corners now means:

1. **Bugs and Missing Features**: If validation rules aren't documented, they won't be implemented. If edge cases aren't listed in acceptance criteria, they won't be tested. Shortcuts in specs = bugs in production.

2. **Developer Confusion**: Another Claude instance (or human developer) implementing this task months later needs COMPLETE guidance. "See above for pattern" doesn't help when "above" is in a different context window.

3. **Testing Gaps**: Incomplete test sections mean features ship without coverage. "Additional tests omitted" = untested code paths = production failures.

4. **Technical Debt**: Brief specs force developers to make assumptions. Different assumptions = inconsistent implementation. Comprehensive specs prevent this.

5. **Lost Knowledge**: Detailed specs serve as evergreen documentation. Future developers (6 months, 2 years later) can understand design decisions. Brief specs lose context.

6. **Client/Stakeholder Trust**: The user requested comprehensive specs for a reason - they want quality, completeness, predictability. Delivering abbreviated specs breaks trust.

7. **Cost of Rework**: Fixing unclear requirements during implementation costs 10-100x more than getting specs right upfront. An extra 30 minutes writing complete acceptance criteria saves 5 hours of debugging later.

**ROI Calculation:**
- Time to write complete spec: +30 minutes per task
- Time saved in implementation: -5 hours of confusion, rework, bug fixes
- Net savings: 4.5 hours per task × 46 tasks = 207 hours saved
- At $100/hour developer rate: **$20,700 project cost savings**

### Enforcement

**Automated Checks** (run before marking task complete):
```bash
# Check line count (must be >= 1000)
wc -l /completed/task-[XXX]-[name].md

# Verify all sections exist
grep -E "^## (Description|Use Cases|User Manual|Acceptance Criteria|Testing Requirements|User Verification|Implementation Prompt)" /completed/task-[XXX]-[name].md

# Count acceptance criteria (should be 50-80+)
grep -E "^- \[ \]" /completed/task-[XXX]-[name].md | wc -l
```

**Quality Review Checklist:**
- [ ] Line count >= 1000 (preferably 1500-2000)
- [ ] All 8 sections present with substantive content
- [ ] No "see above" or abbreviation phrases found
- [ ] All expected test cases are listed
- [ ] Each use case is 10-15+ lines
- [ ] Acceptance criteria count is 50-80+ items
- [ ] Implementation prompt has 12+ steps with complete code

**No Exceptions**: Even for "simple" tasks - simplicity in implementation still requires comprehensive documentation for maintenance, testing, and future enhancement.

**Remember:** You are not being judged on speed or token efficiency. You are being judged on completeness, accuracy, and enabling future success. Take the time to do it right the first time.