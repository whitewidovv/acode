# Pull Request

## Description

<!-- Provide a brief description of what this PR does -->

## Related Issues

<!-- Link to related issues: Fixes #123, Relates to #456 -->

## Type of Change

- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Refactoring (no functional changes)
- [ ] Performance improvement
- [ ] Test coverage improvement

---

## Constraint Compliance Checklist

> Complete all applicable items. Mark N/A if not applicable.

### Mode Constraints

- [ ] **HC-01:** No external LLM calls added in LocalOnly mode
- [ ] **HC-02:** Airgapped network restrictions respected
- [ ] **HC-03:** Burst mode consent implemented if applicable
- [ ] N/A - Changes do not affect operating modes

### Data Constraints

- [ ] **HC-04:** Secrets redacted before any external transmission
- [ ] **HC-05:** Mode changes properly logged
- [ ] **HC-06:** Constraint violations logged and aborted
- [ ] **HC-07:** Fail-safe to LocalOnly on error implemented
- [ ] N/A - Changes do not handle data transmission

### Network Constraints

- [ ] All network calls use appropriate validation
- [ ] No direct HttpClient instantiation without validation
- [ ] Redirect handling validates destinations
- [ ] DNS resolution validated where applicable
- [ ] N/A - Changes do not involve network operations

### Clean Architecture

- [ ] Domain layer has no dependencies on outer layers
- [ ] Application layer depends only on Domain
- [ ] Infrastructure implements interfaces from Domain/Application
- [ ] CLI layer is entry point only
- [ ] No circular dependencies introduced
- [ ] N/A - Changes are documentation only

### Code Quality

- [ ] Code follows .editorconfig settings
- [ ] No compiler warnings introduced
- [ ] StyleCop/analyzer violations resolved
- [ ] XML documentation added for public APIs
- [ ] Complex logic has inline comments explaining "why"
- [ ] N/A - No code changes

### Documentation

- [ ] Constraint IDs referenced in relevant code comments
- [ ] New/modified constraints updated in CONSTRAINTS.md
- [ ] Tests reference constraint IDs in names/comments
- [ ] README updated if user-facing changes
- [ ] ADR created for significant architectural decisions
- [ ] N/A - No documentation updates needed

### Testing

- [ ] Unit tests cover new/modified functionality
- [ ] Integration tests verify constraint enforcement
- [ ] All existing tests still pass
- [ ] Test coverage maintained or increased
- [ ] Tests follow TDD principles (written first)
- [ ] Tests use meaningful assertions (not just NotImplementedException)
- [ ] N/A - No testable code changes

### Verification

- [ ] `dotnet build` completes with 0 errors, 0 warnings
- [ ] `dotnet test` all tests passing
- [ ] `dotnet format` applied (if SDK available)
- [ ] Changes reviewed for security implications
- [ ] Changes reviewed for performance implications
- [ ] Breaking changes documented in commit message

---

## Test Plan

<!-- Describe how you tested these changes -->

---

## Checklist

- [ ] PR title follows [Conventional Commits](https://www.conventionalcommits.org/) format
- [ ] Commits are atomic and have descriptive messages
- [ ] All TODO comments have issue numbers
- [ ] Sensitive data (keys, passwords, secrets) not committed
- [ ] .gitignore updated if new generated files added
- [ ] No commented-out code committed (unless explicitly needed)

---

## Screenshots / Examples

<!-- If applicable, add screenshots or code examples -->

---

## Reviewer Notes

<!-- Any special instructions for reviewers -->

---

## Post-Merge Tasks

<!-- Any follow-up tasks needed after merge -->
- [ ] None
