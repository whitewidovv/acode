# Changelog

All notable changes to the Acode project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

#### Task 003a: Risk Enumeration & Mitigation Infrastructure
- Added comprehensive risk and mitigation domain models
  - `Risk` aggregate with DREAD scoring, STRIDE categorization, mitigation tracking
  - `Mitigation` entity with implementation status tracking
  - `RiskId` value object with format validation (RISK-{S|T|R|I|D|E}-NNN)
  - `MitigationId` value object with format validation (MIT-NNN)
  - `RiskStatus` enum (Active, Deprecated, Accepted)
  - `MitigationStatus` enum (Implemented, InProgress, Pending, NotApplicable)
- Added YAML-based risk register infrastructure
  - `IRiskRegister` interface for risk repository pattern (7 query methods, 2 properties)
  - `RiskRegisterLoader` for parsing risk-register.yaml with YamlDotNet
  - `YamlRiskRegisterRepository` file-based implementation with caching
  - Support for filtering risks by STRIDE category and severity
  - Support for searching risks by keyword
  - Validation: duplicate detection, required field checking, mitigation cross-references
- Added CLI commands for risk management (on `SecurityCommand`)
  - `ShowRisksAsync(category?, severity?)` - List and filter risks
  - `ShowRiskDetailAsync(riskId)` - Display detailed risk information with DREAD scores
  - `ShowMitigationsAsync()` - List all mitigations with status summary
  - `VerifyMitigationsAsync()` - Generate mitigation verification report
- Added comprehensive test coverage (31 tests)
  - 5 unit tests for `RiskRegisterLoader` (YAML parsing and validation)
  - 11 integration tests against actual risk-register.yaml (42 risks, 21 mitigations)
  - 15 CLI unit tests using mocked `IRiskRegister` (error handling, output formatting)
  - All tests passing with 0 errors, 0 warnings

### Changed
- Enhanced `SecurityCommand` with optional `IRiskRegister` dependency injection
- Updated risk register to use permissive mitigation reference validation

### Technical Details
- Zero build warnings or errors
- Full StyleCop and Roslyn analyzer compliance
- Complete XML documentation on all public APIs
- Proper async/await patterns with ConfigureAwait
- Clean layer boundaries (Domain → Application → Infrastructure → CLI)
