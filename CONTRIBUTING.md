# Contributing to Acode

Thank you for your interest in contributing to Acode! This document provides guidelines and instructions for contributing.

## Table of Contents

- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Testing Requirements](#testing-requirements)
- [Pull Request Process](#pull-request-process)
- [Commit Message Format](#commit-message-format)
- [Branch Naming](#branch-naming)
- [Code Review](#code-review)
- [Troubleshooting](#troubleshooting)

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later
- [Git](https://git-scm.com/)
- A code editor (VS 2022, VS Code, or Rider recommended)
- Local model provider (Ollama, vLLM, etc.) for testing

### Clone the Repository

```bash
git clone https://github.com/whitewidovv/acode.git
cd acode
```

## Development Setup

### 1. Restore Dependencies

```bash
dotnet restore
```

### 2. Build the Solution

```bash
dotnet build
```

### 3. Run Tests

```bash
dotnet test
```

All tests should pass before you start making changes.

### 4. Verify Code Format

```bash
dotnet format --verify-no-changes
```

If this fails, run `dotnet format` to auto-format your code.

## Development Workflow

1. **Create a feature branch** (never commit to `main`)
   ```bash
   git checkout -b feature/task-XXX-description
   ```

2. **Make your changes** following TDD (see CLAUDE.md)
   - Write a failing test first (RED)
   - Implement the minimal code to pass (GREEN)
   - Refactor while keeping tests green (REFACTOR)

3. **Run format, analyzers, and tests**
   ```bash
   dotnet format
   dotnet build
   dotnet test
   ```

4. **Commit your changes** (one logical change per commit)
   ```bash
   git add .
   git commit -m "feat: add user authentication"
   ```

5. **Push to your branch**
   ```bash
   git push origin feature/task-XXX-description
   ```

6. **Create a Pull Request** on GitHub

## Coding Standards

### .NET Conventions

- **Naming**:
  - PascalCase for public members, classes, methods
  - _camelCase for private fields
  - camelCase for parameters and local variables
- **Braces**: Allman style (opening brace on new line)
- **Using directives**: Outside namespace, System directives first
- **var**: Use for built-in types and when type is apparent

### Clean Architecture

- **Domain** layer: No external dependencies
- **Application** layer: Depends only on Domain
- **Infrastructure** layer: Implements interfaces from Domain/Application
- **CLI** layer: Thin layer, delegates to Application

See [docs/REPO_STRUCTURE.md](docs/REPO_STRUCTURE.md) for details.

### EditorConfig

We use `.editorconfig` to enforce code style. Your IDE should automatically apply these settings.

To verify compliance:
```bash
dotnet format --verify-no-changes
```

### Analyzers

We use Roslyn analyzers and StyleCop to enforce code quality. Build warnings are treated as errors.

Common analyzer rules:
- CA1062: Validate public method arguments
- CA2007: ConfigureAwait warnings
- IDE0005: Unnecessary using directives are errors
- SA1600: Public members should be documented (suggestion)

## Testing Requirements

**All code MUST be tested.** We follow Test-Driven Development (TDD).

### Test Types

1. **Unit Tests**: Test a single class in isolation
   - Location: `tests/<ProjectName>.Tests/`
   - Mock external dependencies
   - Fast (<1ms each)

2. **Integration Tests**: Test multiple components together
   - Location: `tests/Acode.Integration.Tests/`
   - May use file system, but isolated
   - Moderate speed

3. **End-to-End Tests**: Test complete user workflows
   - Location: `tests/Acode.Integration.Tests/`
   - Test via CLI interface
   - Slower (seconds)

### Test Naming

Tests MUST follow this pattern:
```
MethodName_Scenario_ExpectedResult
```

Examples:
- `CreateUser_WithValidEmail_ReturnsUserId`
- `CreateUser_WithNullEmail_ThrowsArgumentNullException`
- `ParseConfig_WhenFileDoesNotExist_ReturnsDefaultConfig`

### Test Structure

```csharp
[Fact]
public void MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var input = "test";

    // Act
    var result = MethodUnderTest(input);

    // Assert
    result.Should().Be("expected");
}
```

### Coverage Requirements

- **Unit tests**: >80% coverage for new code
- **Critical paths**: 100% coverage (safety, security, mode enforcement)
- Run coverage:
  ```bash
  dotnet test --collect:"XPlat Code Coverage"
  ```

## Pull Request Process

### Before Creating a PR

- [ ] All tests pass
- [ ] Code is formatted (`dotnet format`)
- [ ] Build succeeds with zero warnings
- [ ] Added tests for new functionality
- [ ] Updated documentation if needed
- [ ] Followed commit message format
- [ ] Branch is up to date with main

### PR Checklist

When creating a PR, ensure:

1. **Title**: Descriptive and follows format (e.g., `feat: add user authentication`)
2. **Description**: Includes:
   - What problem does this solve?
   - What approach did you take?
   - Any breaking changes?
   - Testing performed
3. **Labels**: Add appropriate labels (bug, enhancement, documentation, etc.)
4. **Tests**: All new code is tested
5. **Documentation**: Relevant docs updated

### PR Template

```markdown
## Description

[Brief description of changes]

## Motivation

[Why are these changes needed?]

## Changes

- [Change 1]
- [Change 2]

## Testing

- [ ] Unit tests added
- [ ] Integration tests added
- [ ] Manual testing performed

## Checklist

- [ ] Code follows style guidelines
- [ ] Self-review performed
- [ ] Tests pass locally
- [ ] Documentation updated
```

## Commit Message Format

We follow **Conventional Commits**:

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Formatting, no code change
- `refactor`: Code change that neither fixes a bug nor adds a feature
- `test`: Adding or correcting tests
- `chore`: Maintenance tasks (dependencies, tooling)
- `perf`: Performance improvement
- `ci`: CI/CD changes

### Examples

```bash
feat(domain): add User entity with validation

fix(cli): handle null input gracefully

docs: update README quick start section

refactor(application): extract use case interface

test(domain): add User entity edge cases

chore: update dependencies to latest versions
```

### Scope

Optional, indicates affected area:
- `domain`, `application`, `infrastructure`, `cli`
- `config`, `safety`, `audit`
- Specific component names

## Branch Naming

Use descriptive branch names:

```
<type>/<task-or-issue>-<short-description>
```

Examples:
- `feature/task-001-model-runtime`
- `fix/issue-42-null-reference`
- `docs/update-contributing-guide`
- `refactor/clean-architecture`

## Code Review

### For Authors

- Respond to all comments
- Make requested changes or explain why not
- Keep discussions professional and constructive
- Squash commits if requested
- Be patient - reviews take time

### For Reviewers

- Be respectful and constructive
- Explain why, not just what
- Suggest alternatives
- Approve when satisfied
- Review for:
  - Correctness
  - Test coverage
  - Architecture adherence
  - Code style
  - Documentation

## Troubleshooting

### Build Fails with Analyzer Errors

```bash
# View all warnings
dotnet build -v normal 2>&1 | grep -E "warning (CA|SA|IDE)"

# Fix formatting
dotnet format

# Rebuild
dotnet build
```

### Tests Fail

```bash
# Run tests with verbose output
dotnet test -v normal

# Run specific test
dotnet test --filter "FullyQualifiedName~ClassName.TestName"

# Run tests in specific project
dotnet test tests/Acode.Domain.Tests/
```

### Code Coverage Not Generating

```bash
# Ensure coverlet is installed
dotnet restore

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report (requires reportgenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

### Formatting Issues

```bash
# Check what would be formatted
dotnet format --verify-no-changes

# Apply formatting
dotnet format

# Format specific file
dotnet format --include src/Acode.Domain/User.cs
```

## Documentation

When adding new features, update:

- **README.md**: If user-facing changes
- **docs/CONFIG.md**: If configuration added
- **docs/OPERATING_MODES.md**: If mode behavior changes
- **docs/REPO_STRUCTURE.md**: If new projects/folders added
- **Code comments**: All public APIs should have XML docs

## Security

- **Never commit secrets**: Use `.env.example` for templates
- **Follow operating modes**: Respect LocalOnly/Burst/Airgapped constraints
- **Report vulnerabilities**: See [SECURITY.md](SECURITY.md)

## License

By contributing, you agree that your contributions will be licensed under the same license as the project (MIT License).

## Reporting Issues

When reporting bugs or requesting features, please use our issue templates:

- [Bug Report](.github/ISSUE_TEMPLATE/bug_report.md) - Report a bug or unexpected behavior
- [Feature Request](.github/ISSUE_TEMPLATE/feature_request.md) - Suggest a new feature or improvement

These templates help ensure we have all the information needed to address your issue quickly.

## Questions?

- Open a GitHub Discussion
- Check existing issues
- Read the [task specifications](docs/tasks/)

---

**Thank you for contributing to Acode!**
