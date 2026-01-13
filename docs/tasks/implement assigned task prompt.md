## 🚨 CRITICAL: READ THIS FIRST 🚨

**BEFORE WRITING ANY CODE, YOU MUST:**

1. **Read the FULL "Implementation Prompt" section** at the end of the task file
   - This section contains complete, working code examples for ALL classes and methods
   - It shows the EXACT file structure, interfaces, and implementations expected
   - It includes error codes, exception hierarchies, and result types
   - **Skipping this wastes massive amounts of time and context**

2. **Read the FULL "Testing Requirements" section** in the task file
   - This section contains complete test code for ALL scenarios
   - It shows which testing framework to use (MSTest vs xUnit)
   - It defines all test cases you need to implement
   - **Use this to guide your TDD - write these tests FIRST**

3. **Follow the code examples EXACTLY unless you have a demonstrably better approach**
   - The Implementation Prompt was written to guide you
   - Don't reinvent patterns when they're already specified
   - If you deviate, you MUST have a technical justification

**WHY THIS MATTERS:**
- Previous agents skipped Implementation Prompts and built 30% of what was specified
- This created massive rework and wasted ~50k tokens
- The Implementation Prompt exists SOLELY FOR YOU - it's the blueprint
- Reading it takes 10 minutes; NOT reading it wastes hours

**ENFORCEMENT:**
- If you find yourself implementing without having read these sections, STOP immediately
- Go back and read them
- Verify your approach matches the specification
- Only then continue

---

Your next task is to review docs\TDD_INSTRUCTIONS.md and be sure claude.md is fresh in context. keep in mind that You MUST follow strict Test-Driven Development with no exceptions. when you have a comprehensive, complete understanding of the assigned task, come up with a strategic, phase-based implementation plan to implement the entirety of task following CLAUDE.md instructions i nsection 3 ( ## Section 3. Core Working Principles). we must implement the whole task. the task is not done if any of the requirements (FRs, ACs, NFRs, imlementation prompt code, testing requirements) are not done, as per CLAUDE.md, without deferring anything except future-slated work. in the implementation plan checklist, include auditing (including a fresh, brand new gap analysis, from scratch, as if it had never been performed, to make a final pass to try to catch everything, avoiding confirmation bias).  we must do this to ensure that the ENTIRE TASK is implemented as per the guidelines in claude.md

put the implementation plan in writing as instructed in claude.md, create a new feature branch for it, and begin implementation.

 be sure to commit as each phase is completed, if not more often at your discretion. work autonomously until the task is complete. final reminders:  remember to audit when you think you are done following the gap analysis methodology, docs\AUDIT-GUIDELINES.md and claude.md guidelines, and remember that you are not to decide to defer anything that is in this task unless you discover a future-task dependency, and then you stop, bring it to my attention, and suggest moving it from where it is to reside within that future task that it is dependent upon as a new subtask therein. remember, and this is critically important -- there is NOTHING that is optional, nice to have, or deferrable. it is all in scope, if it is in these documents. if it is not possible to complete due to down-the-line dependencies, then the task doesn't belong where it is, and raise the issue with me and we will move the task / subtask to where it belongs, as per the instructions in Claude.md. i know i seem to be repeating myself here, but you continually violate this rule, so i want to be absolutely clear and call attention to this process and requirement. 

---

## Lessons Learned from Previous Task Implementations

### From Task 010 (CLI Command Framework)

**1. Record Types and Equality Testing**
- When using C# record types with `required` properties, test equality carefully
- Records use value equality by default, comparing ALL properties
- If properties include reference types (like IOutputFormatter), use the SAME instance in both contexts for equality tests
- Example: `var formatter = new ConsoleFormatter(...); context1 = new() { Formatter = formatter }; context2 = new() { Formatter = formatter };`

**2. Test Isolation for Console.Out**
- Tests that manipulate `Console.Out` or other shared global state MUST disable parallelization
- Use xUnit collections with `[CollectionDefinition("Sequential", DisableParallelization = true)]`
- Apply `[Collection("Sequential")]` to test classes that use Console.Out
- Without this, tests pass individually but fail when run in parallel (race conditions)

**3. Adding Required Properties to Existing Types**
- When adding a new `required` property to a widely-used type (like CommandContext), expect to update MANY test files
- Grep for all usages first: `grep -r "new CommandContext" tests/`
- Update systematically, file by file, to avoid missing any
- Verify compilation after each file to catch issues early

**4. StyleCop Documentation Requirements**
- All bullet points in `<remarks>` sections must end with periods
- StyleCop SA1629 enforces this even for list items
- Example: `/// - Option A.` (not `/// - Option A`)

**5. Subtask Organization and Tracking**
- ALWAYS use TodoWrite to track subtasks explicitly
- Break down parent tasks into subtasks immediately upon reading the spec
- Update TodoWrite after EACH logical unit of work, not just at subtask boundaries
- This helps with context recovery if you run out of tokens mid-task

**6. Formatter Pattern for Output Abstraction**
- Passing an IOutputFormatter via context is cleaner than commands creating their own formatters
- Allows global flags (--json, --no-color) to control formatting centrally
- Commands stay focused on logic, not presentation concerns
- Test formatters independently, then test command logic with mocked formatters

**7. Args-Based Subcommand Routing**
- For commands with subcommands (like `config validate`, `config show`), the Args array pattern works well
- CommandRouter strips the parent command name, passes remaining args to the command
- Command uses `context.Args[0]` to determine subcommand
- Simpler than hierarchical ICommand nesting, more flexible for complex parsing

**8. Exit Code Standardization**
- Define ExitCode enum early (Success=0, InvalidArguments=2, ConfigurationError=3, etc.)
- Use consistent exit codes across all commands
- Test exit codes explicitly - they're part of the contract
- Helps with shell scripting and CI/CD integration

**9. Integration Tests with Real Components**
- ProgramTests that invoke `Main()` directly are valuable for catching integration issues
- Test the ACTUAL wiring, not just mocked dependencies
- Catches issues like: flags not parsed correctly, routing not wired, etc.

**10. Audit Report as Living Document**
- Create the audit report as you go, not at the end
- Use it as a checklist to ensure nothing is missed
- Include file paths, line numbers, and specific evidence for each acceptance criterion
- The audit report serves as permanent documentation of what was delivered

**11. Comprehensive PR Descriptions**
- Include examples of actual usage in PR descriptions
- Show command output, error messages, edge cases
- Makes review easier and serves as documentation
- Use code blocks with syntax highlighting for terminal output

**12. Early and Frequent Commits**
- Commit after each logical unit (one enum, one test class, one feature)
- Push immediately after each commit (enables collaboration, reduces risk)
- Don't wait until "subtask complete" to commit - that's too coarse
- Use conventional commit format consistently (feat:, test:, docs:, refactor:)

**13. Build Validation Before Committing**
- ALWAYS run `dotnet build` before committing
- ALWAYS run `dotnet test` before committing
- Catch StyleCop violations early (they block CI)
- Faster feedback loop than waiting for CI to fail

**14. Reference Types in Task Specs**
- When specs mention specific classes/interfaces, check if they already exist
- Don't assume you need to create everything from scratch
- Grep for existing types: `grep -r "IConfigLoader" src/`
- Reuse existing abstractions when appropriate

**15. Test Naming Conventions**
- Use descriptive test names: `MethodName_Scenario_ExpectedBehavior`
- Example: `RouteAsync_WithUnknownCommand_ReturnsInvalidArguments`
- Makes test failures immediately understandable
- Enables "test as documentation" pattern

**16. Nullable Reference Types**
- Enable nullable reference types (`<Nullable>enable</Nullable>`) from the start
- Use `string?` for optional properties
- Use `ArgumentNullException.ThrowIfNull()` for required parameters
- Compiler catches null-related bugs at build time, not runtime

**17. Async/Await Consistency**
- Use `.ConfigureAwait(false)` on ALL library code awaits
- Use `.ConfigureAwait(true)` on TEST code awaits (safer for test runners)
- Be consistent - mixing ConfigureAwait(true/false) in same method is confusing
- Tests should use `async Task` not `async void`

**18. Levenshtein Distance for Fuzzy Matching**
- Standard algorithm for "Did you mean?" suggestions
- Threshold of 3 edits works well for command names
- Show max 3 suggestions, ordered by distance
- Common pattern - reusable across any CLI tool

**19. Documentation Remarks vs Summary**
- `<summary>` = one-sentence description (no period at end)
- `<remarks>` = detailed explanation, examples, constraints (periods required)
- Use `<remarks>` to explain WHY, not just WHAT
- Reference functional requirements (FR-XXX) in remarks when applicable

**20. Feature Branch Workflow**
- Create feature branch at start: `git checkout -b feature/task-XXX-description`
- Never commit directly to main (per CLAUDE.md)
- Push frequently to enable collaboration
- Create PR only when audit passes

### From Task 011 Planning (Dependency Discovery)

**21. Identify Dependencies Early**
- Read full task specifications BEFORE starting implementation
- Check for explicit dependencies in task header (Dependencies: Task XXX)
- Verify dependencies are implemented: `grep -r "IInterfaceName" src/`
- Raise blockers immediately, don't assume you can work around them

**22. Implementation Order Matters**
- Foundation tasks (database, config) must come before orchestration tasks
- Domain entities can often be implemented independently
- Persistence/infrastructure requires abstractions to exist first
- When blocked, escalate to user - don't stub fake interfaces

**23. Task Suite Size Awareness**
- Large task suites (10,000+ lines) require multi-session work
- Break into phases explicitly in implementation plan
- Update plan frequently to enable seamless resume
- Don't attempt to complete massive suites in one session

---

## Key Reminders for All Implementations

1. **Read TDD_INSTRUCTIONS.md and CLAUDE.md FIRST** - They contain critical workflow requirements
2. **Create implementation plan BEFORE coding** - Break into phases with checklists as per CLAUDE.md section 3.2
3. **Write tests FIRST (RED), then code (GREEN), then refactor (REFACTOR)** - No exceptions
4. **Update implementation plan as you progress** - Mark ✅/🔄/- for completed/in-progress/pending
5. **Commit after EVERY logical unit** - Don't batch commits
6. **Run build + tests before EVERY commit** - Catch issues early
7. **Update TodoWrite frequently** - Helps with context recovery
8. **Audit BEFORE creating PR** - Use docs/AUDIT-GUIDELINES.md checklist and run fresh gap analysis
9. **Create audit report in docs/audits/** - Permanent record of what was delivered

---

## Common Pitfalls to Avoid

- ❌ Implementing without reading the full spec (leads to missing features)
- ❌ Skipping test-first workflow (leads to untested code)
- ❌ Forgetting to check for subtasks (leads to incomplete parent task)
- ❌ Self-approving deferrals without user consultation (violates CLAUDE.md)
- ❌ Creating new abstractions when existing ones work (over-engineering)
- ❌ Batching many changes into one commit (makes review harder)
- ❌ Waiting until "feature complete" to commit (loses progress if context runs out)
- ❌ Creating PR before running audit (delivers incomplete work)
- ❌ Implementing "just the essentials" (EVERYTHING in spec is essential)
- ❌ Assuming specs have optional sections (they don't - it's ALL required)
- ❌ Ignoring dependency blockers (check dependencies before starting)

---

## Success Criteria Checklist

Before creating PR, verify:
- [ ] Implementation plan exists and is up-to-date
- [ ] Build succeeds with 0 warnings, 0 errors
- [ ] All tests pass (0 failures, 0 skips)
- [ ] Every source file has corresponding test file
- [ ] Audit report created in docs/audits/
- [ ] Audit checklist 100% complete
- [ ] Fresh gap analysis performed and documented and 0 issues found
- [ ] All commits follow Conventional Commits format
- [ ] Feature branch used (no direct commits to main)
- [ ] Code pushed to remote
- [ ] PR description comprehensive with examples

When ALL boxes checked → Create PR. Not before.
