# Task 000.c: Add Baseline Tooling + Formatting + Test Scaffolding

**Priority:** 3 / 49  
**Tier:** Foundation  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 0 — Foundation  
**Dependencies:** Task 000.a (requires repository structure), Task 000.b (docs exist)  

---

## Description

### Overview

Task 000.c establishes the development tooling, code formatting standards, and test infrastructure for the Agentic Coding Bot (Acode) project. This task ensures that all code contributed to the project meets consistent quality standards, follows uniform formatting, and is properly tested from day one.

Development tooling is not optional infrastructure—it is a force multiplier that prevents bugs, reduces code review friction, and enables automated quality gates. This task creates the foundation for a high-quality, maintainable codebase.

### Business Value

Investing in tooling provides ongoing returns:

1. **Reduces code review time** — Automated formatting eliminates style debates
2. **Catches bugs early** — Static analysis finds issues before runtime
3. **Enables CI/CD** — Automated checks gate merges to main
4. **Improves consistency** — All developers produce uniform code
5. **Accelerates onboarding** — Tools enforce conventions automatically

### Scope Boundaries

**In Scope:**
- .editorconfig for IDE settings
- dotnet format configuration
- Roslyn analyzers configuration
- Security analyzers
- Test infrastructure (xUnit, FluentAssertions, NSubstitute)
- Code coverage configuration (coverlet)
- CONTRIBUTING.md with development workflow
- Pre-commit hooks (optional, documented)
- Solution-level analyzer packages

**Out of Scope:**
- CI/CD pipeline configuration (Epic 8)
- Docker configuration (Epic 4)
- Mutation testing setup (future enhancement)
- Performance profiling tools
- Production monitoring tools

### Integration Points

| Task | Relationship | Description |
|------|--------------|-------------|
| Task 000.a | Predecessor | Solution structure must exist |
| Task 000.b | Predecessor | Docs structure must exist |
| Task 001+ | Downstream | All code must follow these standards |
| Epic 8 | Consumer | CI/CD will run these checks |

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **EditorConfig** | Standard for defining coding styles across editors |
| **dotnet format** | .NET CLI tool for formatting code |
| **Roslyn Analyzer** | Compile-time code analysis tool for .NET |
| **StyleCop** | Analyzer for C# coding style enforcement |
| **Static Analysis** | Code analysis without execution |
| **Code Coverage** | Metric measuring code exercised by tests |
| **Coverlet** | Cross-platform code coverage library for .NET |
| **Pre-commit Hook** | Git hook running before commits |
| **Mutation Testing** | Testing technique that modifies code to verify test quality |
| **xUnit** | Unit testing framework for .NET |
| **FluentAssertions** | Readable assertion library |
| **NSubstitute** | Mocking/stubbing library |
| **Analyzer Severity** | Warning, error, suggestion, or hidden |

---

## Out of Scope

- Setting up continuous integration pipelines
- Docker container configuration
- Mutation testing (Stryker.NET)
- Performance benchmarking tools
- Memory profiling tools
- Production APM integration
- Log aggregation tools
- Secrets management tools
- Dependency scanning (beyond basic)

---

## Functional Requirements

### EditorConfig (FR-000c-01 to FR-000c-20)

| ID | Requirement |
|----|-------------|
| FR-000c-01 | .editorconfig MUST exist at repository root |
| FR-000c-02 | .editorconfig MUST set indent_style = space |
| FR-000c-03 | .editorconfig MUST set indent_size = 4 for C# |
| FR-000c-04 | .editorconfig MUST set end_of_line = lf |
| FR-000c-05 | .editorconfig MUST set charset = utf-8 |
| FR-000c-06 | .editorconfig MUST set insert_final_newline = true |
| FR-000c-07 | .editorconfig MUST set trim_trailing_whitespace = true |
| FR-000c-08 | .editorconfig MUST configure C# file header |
| FR-000c-09 | .editorconfig MUST configure naming conventions |
| FR-000c-10 | .editorconfig MUST enforce PascalCase for public members |
| FR-000c-11 | .editorconfig MUST enforce camelCase for private fields |
| FR-000c-12 | .editorconfig MUST enforce _prefix for private fields |
| FR-000c-13 | .editorconfig MUST configure bracing style (Allman) |
| FR-000c-14 | .editorconfig MUST configure using directive placement |
| FR-000c-15 | .editorconfig MUST configure var usage preferences |
| FR-000c-16 | .editorconfig MUST be compatible with VS 2022 |
| FR-000c-17 | .editorconfig MUST be compatible with VS Code |
| FR-000c-18 | .editorconfig MUST be compatible with Rider |
| FR-000c-19 | .editorconfig settings MUST be enforceable by build |
| FR-000c-20 | .editorconfig MUST include markdown and JSON sections |

### Code Formatting (FR-000c-21 to FR-000c-35)

| ID | Requirement |
|----|-------------|
| FR-000c-21 | `dotnet format` MUST run without errors |
| FR-000c-22 | `dotnet format --verify-no-changes` MUST pass after formatting |
| FR-000c-23 | Format check MUST complete in under 30 seconds |
| FR-000c-24 | Formatting MUST be deterministic |
| FR-000c-25 | Formatted code MUST compile |
| FR-000c-26 | Formatting MUST NOT change logic |
| FR-000c-27 | Formatting rules MUST match .editorconfig |
| FR-000c-28 | .globalconfig MUST exist for analyzer settings |
| FR-000c-29 | Format failures MUST have actionable messages |
| FR-000c-30 | IDE quick-fixes MUST apply formatting |
| FR-000c-31 | Format on save MUST be supported |
| FR-000c-32 | Formatting MUST handle edge cases |
| FR-000c-33 | Long lines MUST be wrapped appropriately |
| FR-000c-34 | LINQ queries MUST be formatted consistently |
| FR-000c-35 | Lambda expressions MUST be formatted consistently |

### Analyzers (FR-000c-36 to FR-000c-55)

| ID | Requirement |
|----|-------------|
| FR-000c-36 | Microsoft.CodeAnalysis.NetAnalyzers MUST be enabled |
| FR-000c-37 | StyleCop.Analyzers MUST be enabled |
| FR-000c-38 | Analyzer packages MUST be in Directory.Packages.props |
| FR-000c-39 | CA rules MUST be configured in .globalconfig |
| FR-000c-40 | SA rules MUST be configured in .globalconfig |
| FR-000c-41 | Analyzer severity MUST be appropriate (warning/error) |
| FR-000c-42 | Security analyzers MUST be enabled |
| FR-000c-43 | Microsoft.Security.CodeAnalysis SHOULD be evaluated |
| FR-000c-44 | IDE rules MUST be configured |
| FR-000c-45 | Nullable reference type warnings MUST be errors |
| FR-000c-46 | Unused variable warnings MUST be errors |
| FR-000c-47 | Async/await warnings MUST be errors |
| FR-000c-48 | Dispose pattern warnings MUST be errors |
| FR-000c-49 | Thread safety analyzers SHOULD be evaluated |
| FR-000c-50 | Suppression file MUST document exceptions |
| FR-000c-51 | Suppressions MUST have justification |
| FR-000c-52 | Analyzer warnings MUST fail the build |
| FR-000c-53 | Analyzer run time MUST be under 60 seconds |
| FR-000c-54 | False positives MUST be suppressible |
| FR-000c-55 | Analyzer output MUST be actionable |

### Test Infrastructure (FR-000c-56 to FR-000c-75)

| ID | Requirement |
|----|-------------|
| FR-000c-56 | xUnit MUST be configured as test framework |
| FR-000c-57 | FluentAssertions MUST be available in all test projects |
| FR-000c-58 | NSubstitute MUST be available for mocking |
| FR-000c-59 | Test discovery MUST work in VS, VS Code, and CLI |
| FR-000c-60 | Test execution MUST work with `dotnet test` |
| FR-000c-61 | Test categories MUST be supported (Unit, Integration, E2E) |
| FR-000c-62 | Test parallelization MUST be enabled by default |
| FR-000c-63 | Test isolation MUST be ensured |
| FR-000c-64 | Test output MUST be captured |
| FR-000c-65 | Coverlet MUST be configured for coverage |
| FR-000c-66 | Coverage collection MUST work with `--collect:"XPlat Code Coverage"` |
| FR-000c-67 | Coverage reports MUST be generated in Cobertura format |
| FR-000c-68 | Coverage threshold MUST be configurable |
| FR-000c-69 | Test naming convention MUST be documented |
| FR-000c-70 | Test file organization MUST match production code |
| FR-000c-71 | Shared test utilities MUST be in dedicated project if needed |
| FR-000c-72 | Test data builders SHOULD be used |
| FR-000c-73 | Fixture classes MUST follow xUnit patterns |
| FR-000c-74 | Async tests MUST be properly awaited |
| FR-000c-75 | Flaky test detection SHOULD be configured |

### CONTRIBUTING.md (FR-000c-76 to FR-000c-90)

| ID | Requirement |
|----|-------------|
| FR-000c-76 | CONTRIBUTING.md MUST exist at repository root |
| FR-000c-77 | CONTRIBUTING.md MUST explain development setup |
| FR-000c-78 | CONTRIBUTING.md MUST list prerequisites |
| FR-000c-79 | CONTRIBUTING.md MUST explain build process |
| FR-000c-80 | CONTRIBUTING.md MUST explain test process |
| FR-000c-81 | CONTRIBUTING.md MUST explain formatting requirements |
| FR-000c-82 | CONTRIBUTING.md MUST explain analyzer requirements |
| FR-000c-83 | CONTRIBUTING.md MUST explain PR process |
| FR-000c-84 | CONTRIBUTING.md MUST explain commit message format |
| FR-000c-85 | CONTRIBUTING.md MUST explain code review expectations |
| FR-000c-86 | CONTRIBUTING.md MUST link to coding standards |
| FR-000c-87 | CONTRIBUTING.md MUST explain branch naming |
| FR-000c-88 | CONTRIBUTING.md MUST explain issue templates |
| FR-000c-89 | CONTRIBUTING.md MUST explain documentation requirements |
| FR-000c-90 | CONTRIBUTING.md MUST have troubleshooting section |

---

## Non-Functional Requirements

### Performance (NFR-000c-01 to NFR-000c-08)

| ID | Requirement |
|----|-------------|
| NFR-000c-01 | `dotnet format` MUST complete in under 30 seconds |
| NFR-000c-02 | Analyzer checks MUST complete in under 60 seconds |
| NFR-000c-03 | Test execution MUST complete in under 60 seconds |
| NFR-000c-04 | Coverage collection MUST add less than 50% overhead |
| NFR-000c-05 | IDE responsiveness MUST NOT degrade |
| NFR-000c-06 | IntelliSense MUST remain fast |
| NFR-000c-07 | Build time increase MUST be under 20% |
| NFR-000c-08 | Memory usage MUST remain reasonable |

### Reliability (NFR-000c-09 to NFR-000c-15)

| ID | Requirement |
|----|-------------|
| NFR-000c-09 | Formatting MUST be deterministic |
| NFR-000c-10 | Analyzer results MUST be reproducible |
| NFR-000c-11 | Test results MUST be consistent |
| NFR-000c-12 | Coverage numbers MUST be stable |
| NFR-000c-13 | Tools MUST work offline (after restore) |
| NFR-000c-14 | Tools MUST work on Windows, macOS, Linux |
| NFR-000c-15 | No race conditions in parallel tests |

### Maintainability (NFR-000c-16 to NFR-000c-22)

| ID | Requirement |
|----|-------------|
| NFR-000c-16 | Configuration centralized in few files |
| NFR-000c-17 | Rule changes propagate to all projects |
| NFR-000c-18 | Suppressions are documented |
| NFR-000c-19 | Tool versions pinned |
| NFR-000c-20 | Upgrade path documented |
| NFR-000c-21 | Custom rules isolated |
| NFR-000c-22 | Configuration readable and commented |

---

## User Manual Documentation

### .editorconfig Reference

```ini
# Top-most EditorConfig file
root = true

# Default settings for all files
[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# C# files
[*.cs]
indent_size = 4

# Naming conventions
dotnet_naming_rule.public_members_should_be_pascal_case.severity = error
dotnet_naming_rule.public_members_should_be_pascal_case.symbols = public_members
dotnet_naming_rule.public_members_should_be_pascal_case.style = pascal_case

dotnet_naming_symbols.public_members.applicable_kinds = property, method, event
dotnet_naming_symbols.public_members.applicable_accessibilities = public

dotnet_naming_style.pascal_case.capitalization = pascal_case

# Private fields with underscore
dotnet_naming_rule.private_fields_should_be_camel_case_with_underscore.severity = error
dotnet_naming_rule.private_fields_should_be_camel_case_with_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_camel_case_with_underscore.style = camel_case_underscore

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_style.camel_case_underscore.required_prefix = _
dotnet_naming_style.camel_case_underscore.capitalization = camel_case

# Using directives
csharp_using_directive_placement = outside_namespace:error
dotnet_sort_system_directives_first = true

# Braces
csharp_new_line_before_open_brace = all

# var preferences
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion

# Expression-bodied members
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_expression_bodied_properties = true:suggestion
csharp_style_expression_bodied_accessors = true:suggestion

# Pattern matching
csharp_style_prefer_switch_expression = true:suggestion
csharp_style_prefer_pattern_matching = true:suggestion

# Null checking
csharp_style_prefer_null_check_over_type_check = true:suggestion

# JSON files
[*.json]
indent_size = 2

# YAML files
[*.{yml,yaml}]
indent_size = 2

# Markdown files
[*.md]
trim_trailing_whitespace = false

# Solution files
[*.sln]
indent_style = tab
```

### Running Code Analysis

```bash
# Format code
dotnet format

# Check formatting without changes
dotnet format --verify-no-changes

# Run analyzers during build
dotnet build

# See analyzer warnings
dotnet build -v normal 2>&1 | grep -E "warning (CA|SA|IDE)"

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report"
```

### Test Naming Conventions

Tests MUST follow this naming pattern:

```
MethodName_Scenario_ExpectedBehavior
```

Examples:
- `CreateUser_WithValidData_ReturnsUserId`
- `CreateUser_WithNullEmail_ThrowsArgumentNullException`
- `ProcessOrder_WhenStockAvailable_ReducesInventory`

### Test File Organization

```
tests/Acode.Domain.Tests/
├── Entities/
│   ├── UserTests.cs           # Tests for User entity
│   └── OrderTests.cs          # Tests for Order entity
├── ValueObjects/
│   └── EmailTests.cs          # Tests for Email value object
└── Services/
    └── PricingServiceTests.cs # Tests for PricingService
```

### Commit Message Format

Follow Conventional Commits:

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Formatting, no code change
- `refactor`: Code change that neither fixes a bug nor adds a feature
- `test`: Adding or correcting tests
- `chore`: Maintenance tasks

Examples:
```
feat(domain): add User entity with validation
fix(cli): handle null input gracefully
docs: update README quick start section
```

---

## Acceptance Criteria / Definition of Done

### EditorConfig (25 items)

- [ ] .editorconfig exists at repository root
- [ ] indent_style = space configured
- [ ] indent_size = 4 for C# files
- [ ] end_of_line = lf configured
- [ ] charset = utf-8 configured
- [ ] trim_trailing_whitespace = true
- [ ] insert_final_newline = true
- [ ] C# naming conventions configured
- [ ] Public members require PascalCase
- [ ] Private fields require _camelCase
- [ ] Bracing style configured (Allman)
- [ ] Using directive placement configured
- [ ] var preferences configured
- [ ] Expression-bodied members configured
- [ ] JSON indent_size = 2
- [ ] YAML indent_size = 2
- [ ] Markdown trailing whitespace preserved
- [ ] Works in Visual Studio 2022
- [ ] Works in VS Code
- [ ] Works in JetBrains Rider
- [ ] IDE shows violations
- [ ] Build enforces violations
- [ ] No conflicts with .globalconfig
- [ ] Documented in CONTRIBUTING.md
- [ ] All existing code complies

### Code Formatting (20 items)

- [ ] `dotnet format` runs without errors
- [ ] `dotnet format --verify-no-changes` passes
- [ ] Formatting is deterministic
- [ ] Formatted code compiles
- [ ] Format completes in under 30 seconds
- [ ] Formatting matches .editorconfig
- [ ] Long lines handled appropriately
- [ ] LINQ formatted consistently
- [ ] Lambdas formatted consistently
- [ ] Async/await formatted correctly
- [ ] Regions handled (or disabled)
- [ ] Comments preserved
- [ ] XML docs formatted
- [ ] Blank lines consistent
- [ ] Trailing whitespace removed
- [ ] Final newline added
- [ ] IDE format-on-save works
- [ ] Format command documented
- [ ] Pre-commit hook documented
- [ ] All existing code formatted

### Analyzers (30 items)

- [ ] Microsoft.CodeAnalysis.NetAnalyzers added
- [ ] StyleCop.Analyzers added
- [ ] Packages in Directory.Packages.props
- [ ] .globalconfig exists
- [ ] CA rules configured
- [ ] SA rules configured
- [ ] IDE rules configured
- [ ] Nullable warnings are errors
- [ ] Async warnings are errors
- [ ] Dispose warnings are errors
- [ ] Unused code warnings enabled
- [ ] Security rules enabled
- [ ] Build fails on warnings
- [ ] Analyzer runs in under 60 seconds
- [ ] False positives suppressible
- [ ] Suppressions documented
- [ ] Suppressions have justifications
- [ ] No unexplained suppressions
- [ ] IDE shows warnings inline
- [ ] Quick-fixes available
- [ ] Output is actionable
- [ ] Existing code passes
- [ ] Severity levels appropriate
- [ ] Custom rules isolated (if any)
- [ ] Upgrade path documented
- [ ] Works on all platforms
- [ ] Works in CI (when configured)
- [ ] Performance acceptable
- [ ] Memory usage acceptable
- [ ] Results reproducible

### Test Infrastructure (30 items)

- [ ] xUnit configured in all test projects
- [ ] FluentAssertions available
- [ ] NSubstitute available
- [ ] Coverlet configured
- [ ] Tests discovered by VS
- [ ] Tests discovered by VS Code
- [ ] Tests run with `dotnet test`
- [ ] Test categories work (Trait)
- [ ] Parallel execution enabled
- [ ] Test isolation verified
- [ ] Test output captured
- [ ] Coverage collection works
- [ ] Cobertura reports generated
- [ ] Coverage threshold configurable
- [ ] Test naming convention documented
- [ ] Test organization documented
- [ ] Async tests work
- [ ] Mocking works
- [ ] Assertions readable
- [ ] Failure messages clear
- [ ] Tests complete in under 60s
- [ ] No flaky tests
- [ ] Fixture pattern documented
- [ ] Theory data supported
- [ ] Test data builders available (if needed)
- [ ] Integration test project works
- [ ] Test results exportable
- [ ] Coverage excludes generated code
- [ ] Coverage excludes test code
- [ ] Minimum coverage documented

### CONTRIBUTING.md (25 items)

- [ ] CONTRIBUTING.md exists at root
- [ ] Prerequisites listed
- [ ] Development setup documented
- [ ] Build commands documented
- [ ] Test commands documented
- [ ] Format commands documented
- [ ] Analyzer expectations documented
- [ ] PR process documented
- [ ] Commit message format documented
- [ ] Branch naming documented
- [ ] Code review expectations documented
- [ ] Issue templates referenced
- [ ] PR template referenced
- [ ] Coding standards linked
- [ ] Architecture overview linked
- [ ] Troubleshooting section present
- [ ] Contact information (or links)
- [ ] License reminder
- [ ] DCO/CLA requirements (if any)
- [ ] Security issue handling
- [ ] Spell-checked
- [ ] Links valid
- [ ] Markdown lint passes
- [ ] Consistent with README
- [ ] Up to date with tooling

---

## Testing Requirements

### Unit Tests

| ID | Test | Expected |
|----|------|----------|
| UT-000c-01 | .editorconfig exists | File found |
| UT-000c-02 | .globalconfig exists | File found |
| UT-000c-03 | CONTRIBUTING.md exists | File found |
| UT-000c-04 | Analyzer packages in props | Packages defined |
| UT-000c-05 | `dotnet format --verify-no-changes` | Exit code 0 |
| UT-000c-06 | `dotnet build` with analyzers | Zero warnings |
| UT-000c-07 | `dotnet test` discovers tests | Tests found |
| UT-000c-08 | Coverage collection works | Report generated |
| UT-000c-09 | EditorConfig enforces indent | Violation detected |
| UT-000c-10 | Naming convention enforced | Violation detected |

### Integration Tests

| ID | Test | Expected |
|----|------|----------|
| IT-000c-01 | Full format + build cycle | Success |
| IT-000c-02 | VS loads with analyzers | No errors |
| IT-000c-03 | VS Code shows warnings | Inline warnings |
| IT-000c-04 | Test run with coverage | Coverage report |
| IT-000c-05 | Pre-commit hook (if installed) | Runs format |

### End-to-End Tests

| ID | Test | Expected |
|----|------|----------|
| E2E-000c-01 | Contributor follows CONTRIBUTING.md | All steps work |
| E2E-000c-02 | New file added, formatted, analyzed | Passes all checks |
| E2E-000c-03 | PR with formatting issue | Detected and blocked |
| E2E-000c-04 | Coverage report generated | HTML report works |

### Performance Benchmarks

| ID | Metric | Target |
|----|--------|--------|
| PB-000c-01 | Format time | < 30 seconds |
| PB-000c-02 | Analyze time | < 60 seconds |
| PB-000c-03 | Test time | < 60 seconds |
| PB-000c-04 | Build overhead | < 20% |
| PB-000c-05 | Coverage overhead | < 50% |

---

## User Verification Steps

### Verification 1: EditorConfig Applied
1. Open any .cs file in VS/VS Code
2. Check indent shows as 4 spaces
3. **Verify:** Settings match .editorconfig

### Verification 2: Format Command Works
1. Run `dotnet format --verify-no-changes`
2. **Verify:** Exit code 0

### Verification 3: Analyzers Active
1. Introduce a CA/SA violation
2. Build the project
3. **Verify:** Warning or error reported

### Verification 4: Tests Run
1. Run `dotnet test`
2. **Verify:** All tests discovered and pass

### Verification 5: Coverage Works
1. Run `dotnet test --collect:"XPlat Code Coverage"`
2. Check for coverage.cobertura.xml
3. **Verify:** File exists with data

### Verification 6: CONTRIBUTING.md Complete
1. Read CONTRIBUTING.md
2. Follow all steps as new contributor
3. **Verify:** All instructions work

---

## Implementation Prompt for Claude

### Files to Create

1. **.editorconfig** (see template above)
2. **.globalconfig** for analyzer settings
3. **CONTRIBUTING.md**
4. **.github/ISSUE_TEMPLATE/bug_report.md**
5. **.github/ISSUE_TEMPLATE/feature_request.md**
6. **.github/PULL_REQUEST_TEMPLATE.md**

### .globalconfig Content

```ini
is_global = true

# CA1000: Do not declare static members on generic types
dotnet_diagnostic.CA1000.severity = warning

# CA1062: Validate arguments of public methods
dotnet_diagnostic.CA1062.severity = error

# CA1303: Do not pass literals as localized parameters
dotnet_diagnostic.CA1303.severity = none

# CA1307: Specify StringComparison
dotnet_diagnostic.CA1307.severity = warning

# CA1308: Normalize strings to uppercase
dotnet_diagnostic.CA1308.severity = suggestion

# CA1707: Identifiers should not contain underscores (allow in tests)
dotnet_diagnostic.CA1707.severity = warning

# CA1716: Identifiers should not match keywords
dotnet_diagnostic.CA1716.severity = warning

# CA1720: Identifier contains type name
dotnet_diagnostic.CA1720.severity = warning

# CA1721: Property names should not match get methods
dotnet_diagnostic.CA1721.severity = warning

# CA1724: Type names should not match namespaces
dotnet_diagnostic.CA1724.severity = warning

# CA1725: Parameter names should match base declaration
dotnet_diagnostic.CA1725.severity = warning

# CA2007: Consider calling ConfigureAwait
dotnet_diagnostic.CA2007.severity = warning

# CA2227: Collection properties should be read only
dotnet_diagnostic.CA2227.severity = warning

# IDE0005: Using directive is unnecessary
dotnet_diagnostic.IDE0005.severity = error

# IDE0055: Fix formatting
dotnet_diagnostic.IDE0055.severity = error

# IDE0161: Convert to file-scoped namespace
dotnet_diagnostic.IDE0161.severity = warning

# SA1101: Prefix local calls with this
dotnet_diagnostic.SA1101.severity = none

# SA1200: Using directives should be placed correctly
dotnet_diagnostic.SA1200.severity = none

# SA1309: Field names should not begin with underscore
dotnet_diagnostic.SA1309.severity = none

# SA1413: Use trailing comma in multi-line initializers
dotnet_diagnostic.SA1413.severity = suggestion

# SA1600: Elements should be documented
dotnet_diagnostic.SA1600.severity = suggestion

# SA1633: File should have header
dotnet_diagnostic.SA1633.severity = none
```

### Add to Directory.Packages.props

```xml
<!-- Analyzers -->
<PackageVersion Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0" />
<PackageVersion Include="StyleCop.Analyzers" Version="1.2.0-beta.556" />
```

### Add to Directory.Build.props

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
  <PackageReference Include="StyleCop.Analyzers">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

### Validation Checklist

- [ ] .editorconfig passes validation
- [ ] .globalconfig recognized by build
- [ ] Analyzers run during build
- [ ] Format command works
- [ ] Tests run with coverage
- [ ] CONTRIBUTING.md complete

---

**END OF TASK 000.c**
