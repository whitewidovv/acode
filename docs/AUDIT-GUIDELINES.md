# Audit Guidelines for Task Completion

## Purpose

This document defines **mandatory** audit procedures to verify task completion. These guidelines exist because of critical oversights in Task 002, where Infrastructure layer implementation was delivered without tests (TDD violation) and with multiple code quality issues that an initial audit missed.

**Core Principle:** Perfection and completeness over speed. It's acceptable to run out of context and continue in the next session rather than rush and deliver incomplete work.

---

## Audit Checklist

### 1. Specification Compliance

- [ ] **Read the refined task specification completely** before starting audit
- [ ] **Read the parent epic specification** to understand context
- [ ] **Verify all Functional Requirements (FR-XXX-YYY) are implemented**
  - Go line-by-line through the FR list
  - For each FR, identify the corresponding code artifact
  - Mark each FR as ✅ or ❌ with file path and line number
- [ ] **Verify all Acceptance Criteria are met**
  - Each AC must have corresponding evidence (code, test, or deliverable)
  - No "assumed complete" items without proof
- [ ] **Verify all Deliverables exist**
  - List file paths for each deliverable
  - Check file sizes are non-zero and contain expected content

### 2. Test-Driven Development (TDD) Compliance

**Critical:** This was the primary failure in Task 002.b audit.

- [ ] **For every source file, verify tests exist**
  - Domain layer: `src/Acode.Domain/Foo/Bar.cs` → `tests/Acode.Domain.Tests/Foo/BarTests.cs`
  - Application layer: `src/Acode.Application/Foo/Bar.cs` → `tests/Acode.Application.Tests/Foo/BarTests.cs`
  - Infrastructure layer: `src/Acode.Infrastructure/Foo/Bar.cs` → `tests/Acode.Infrastructure.Tests/Foo/BarTests.cs`
- [ ] **Verify test coverage percentages** (if tooling available)
  - Domain: Target >80% line coverage
  - Application: Target >70% line coverage
  - Infrastructure: Target >60% line coverage (harder due to external dependencies)
- [ ] **Verify test types exist per specification**
  - Unit tests (isolate single class/method)
  - Integration tests (verify layers work together)
  - End-to-end tests (verify user scenarios)
  - Performance tests (if FR specifies performance requirements)
  - Regression tests (if fixing bugs)
- [ ] **Run all tests and verify 100% pass rate**
  - `dotnet test --verbosity normal`
  - Zero failures, zero skips (unless explicitly documented why)

### 3. Code Quality Standards

**Critical:** This was the secondary failure - multiple Copilot issues found post-audit.

- [ ] **Build succeeds with zero errors**
  - `dotnet build --verbosity quiet`
  - Check exit code is 0
- [ ] **Build succeeds with zero warnings**
  - StyleCop analyzers enabled
  - Roslyn analyzers enabled
  - No CS/SA/CA warnings
- [ ] **XML documentation complete**
  - All public types have `/// <summary>`
  - All public methods have `/// <summary>` and `/// <param>` and `/// <returns>`
  - Complex internal logic has explanatory comments
- [ ] **Naming consistency with schema**
  - If YAML schema uses `cwd`, C# property should be `Cwd` (with mapping docs)
  - If YAML schema uses `timeout`, C# property should be `Timeout`
  - Document any intentional deviations
- [ ] **Async/await patterns correct**
  - No `GetAwaiter().GetResult()` in library code (deadlock risk)
  - All `await` calls use `.ConfigureAwait(false)` in library code
  - CancellationToken parameters present and wired through
- [ ] **Resource disposal correct**
  - All `IDisposable` objects in `using` statements or `using` declarations
  - No leaked file handles, streams, database connections
- [ ] **Null handling correct**
  - `ArgumentNullException.ThrowIfNull()` for all reference-type parameters
  - Nullable reference types enabled and warnings addressed

### 4. Dependency Management

- [ ] **Packages added to central management**
  - Check `Directory.Packages.props` for all new packages
  - Versions pinned (not floating)
  - No security vulnerabilities (check GitHub/Dependabot)
- [ ] **Package references added to correct projects**
  - Domain: Should have ZERO external dependencies (pure .NET)
  - Application: Only domain references
  - Infrastructure: Can reference external packages (YamlDotNet, NJsonSchema, etc.)
  - CLI: References Application + Infrastructure
- [ ] **Verify packages actually used**
  - Grep codebase for package types
  - Remove any unused package references

### 5. Layer Boundary Compliance (Clean Architecture)

- [ ] **Domain layer purity**
  - No Infrastructure dependencies
  - No Application dependencies
  - Only pure .NET types (no external packages)
  - No concrete implementations of external I/O
- [ ] **Application layer dependencies**
  - Only references Domain
  - Defines interfaces for Infrastructure to implement
  - No direct file I/O, database calls, or HTTP requests
- [ ] **Infrastructure layer implements Application interfaces**
  - Each Application interface (IFoo) has Infrastructure implementation (FooAdapter, FooRepository, etc.)
  - Infrastructure can reference external packages
  - Infrastructure wired to Application via DI container
- [ ] **No circular dependencies**
  - Build dependency graph
  - Verify Domain → Application → Infrastructure → CLI flow
  - No backward references

### 6. Integration Verification

**Critical:** Task 002 had disconnected layers - ConfigLoader threw NotImplementedException while YamlConfigReader existed.

- [ ] **Verify interfaces are implemented**
  - `IConfigLoader` → `ConfigLoader` implementation exists AND is wired
  - `IConfigValidator` → `ConfigValidator` implementation exists AND is wired
- [ ] **Verify implementations are called**
  - ConfigLoader should actually call YamlConfigReader
  - ConfigValidator should actually call JsonSchemaValidator
  - No `throw new NotImplementedException()` in "complete" code
- [ ] **Verify DI registration (when DI layer exists)**
  - Check `Program.cs` or `ServiceCollectionExtensions.cs`
  - All interfaces registered with concrete implementations
  - Singleton vs Scoped vs Transient lifetimes correct
- [ ] **Verify end-to-end scenarios work**
  - Write integration test that exercises full stack
  - Load config from disk → parse YAML → validate schema → deserialize domain model
  - No mocking in integration tests (use real dependencies)

### 7. Documentation Completeness

- [ ] **User manual documentation exists** (if task specifies)
  - 150-300 lines as specified in task template
  - Clear examples with expected output
  - Common error scenarios with solutions
- [ ] **README updated** (if new feature is user-visible)
  - Installation instructions
  - Configuration examples
  - Usage examples
- [ ] **Implementation plan updated** (if one exists)
  - Mark completed sections as ✅
  - Remove "Future Work" sections that are now complete
  - Update file paths if they changed during implementation

### 8. Regression Prevention

- [ ] **Check for similar patterns elsewhere**
  - If you implemented `YamlConfigReader`, are there other readers?
  - If you implemented `JsonSchemaValidator`, are there other validators?
  - Ensure consistency across similar components
- [ ] **Check for property naming inconsistencies**
  - Grep for old property names (WorkingDirectory → Cwd)
  - Verify all tests updated
  - Verify all documentation updated
- [ ] **Check for broken references**
  - XML doc `<see cref="Foo"/>` tags resolve
  - No undefined types in comments

---

## Audit Evidence Requirements

The audit document (e.g., `TASK-XXX-AUDIT.md`) must include:

1. **Evidence Matrix**: Table mapping each FR to file paths + line numbers
2. **Test Coverage Report**: List of test files + test counts per component
3. **Build Output**: Paste of `dotnet build` showing 0 errors, 0 warnings
4. **Test Output**: Paste of `dotnet test` showing X/X tests passed
5. **Missing Items Section**: Explicitly call out what is NOT implemented and why
6. **Quality Issues Section**: Any known issues, workarounds, or technical debt

Example:

```markdown
## FR-002b-71: Domain Models

| Requirement | Status | Evidence |
|-------------|--------|----------|
| AcodeConfig record | ✅ | src/Acode.Domain/Configuration/AcodeConfig.cs:15 |
| ProjectConfig record | ✅ | src/Acode.Domain/Configuration/AcodeConfig.cs:45 |
| ... | ... | ... |

## Test Coverage

| Component | Test File | Test Count |
|-----------|-----------|------------|
| AcodeConfig | tests/Acode.Domain.Tests/Configuration/AcodeConfigTests.cs | 23 |
| YamlConfigReader | tests/Acode.Infrastructure.Tests/Configuration/YamlConfigReaderTests.cs | 0 ❌ |

**FAILURE:** YamlConfigReader has NO TESTS. TDD violation.
```

---

## Audit Failure Criteria

An audit FAILS if:

1. **Any FR is not implemented** (unless explicitly documented as deferred with stakeholder approval)
2. **Any source file has zero tests** (TDD violation)
3. **Build has errors or warnings**
4. **Any test fails**
5. **Layer boundaries violated** (e.g., Domain references Infrastructure)
6. **Integration broken** (e.g., interface exists but no implementation)
7. **Documentation missing** (user manual, README, examples)

---

## Post-Audit Actions

If audit **passes**:
- Create audit document (TASK-XXX-AUDIT.md)
- Commit with message: "Audit: Task XXX complete - all requirements verified"
- Create PR
- Request review

If audit **fails**:
- **DO NOT** create PR
- **DO NOT** mark task as complete
- **DO NOT** move to next task
- Create list of failures
- Fix each failure
- Re-run audit from step 1
- Repeat until audit passes

---

## Special Case: Out of Context

If audit cannot complete due to context limits:

1. Document what was audited so far
2. Document what remains to be audited
3. Create checkpoint commit: "Checkpoint: Task XXX partial audit"
4. In next session, resume from checkpoint
5. Complete audit
6. Only then create PR

**Never skip the audit to "save context."**

---

## Lessons Learned (Task 002)

### What Went Wrong

1. **TDD Violation**: Implemented YamlConfigReader and JsonSchemaValidator without tests
2. **Superficial Audit**: Audit checked "files exist" but not "files are tested and integrated"
3. **Rushing**: Focused on "getting it done" rather than "getting it right"
4. **False Completion**: Marked task complete while ConfigLoader threw NotImplementedException

### How These Guidelines Prevent It

- **Section 2 (TDD Compliance)**: Would have caught missing Infrastructure tests
- **Section 6 (Integration Verification)**: Would have caught NotImplementedException
- **Section 8 (Regression Prevention)**: Would have caught property naming issues
- **Audit Failure Criteria**: Would have failed the audit, preventing PR creation

### Commitment

Going forward:
- **No rushing**
- **No "assumed complete"**
- **Follow the checklist**
- **If unsure, audit fails**
- **Perfection and completeness over speed**

---

**Last Updated:** 2026-01-03
**Version:** 1.0
**Author:** Claude Code
**Triggered By:** Task 002 TDD violation and missed issues
