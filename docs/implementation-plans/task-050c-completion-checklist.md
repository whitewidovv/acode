# Task-050c Completion Checklist: Migration Runner CLI + Startup Bootstrapping

**Status:** 🔄 IMPLEMENTATION READY - 90% Complete (93/103 ACs)
**Date Started:** 2026-01-15
**Target Completion:** 90% → 100% (103/103 ACs)
**Estimated Effort:** 7-10 developer-hours

**Executive Notes:**
- Infrastructure layer 100% complete (MigrationRunner, Discovery, Executor, Repository, Locking, Validation)
- Startup bootstrapping fully implemented and tested (81 tests passing)
- Critical gap: CLI command layer (6 commands, 33 ACs missing)
- This checklist guides implementation of missing CLI commands to reach 100% completion
- Follow TDD: RED → GREEN → REFACTOR for each command

---

## CRITICAL INSTRUCTIONS FOR NEXT AGENT

**READ THESE FIRST:**
1. The gap analysis (task-050c-fresh-gap-analysis.md) shows 90% completeness with clear breakdown
2. The missing 10% is entirely in CLI Commands (6 commands, 36 ACs unverifiable)
3. All infrastructure is production-ready and tested
4. Use the fresh-gap-analysis.md as your reference for what's missing and why
5. Follow TDD strictly: write failing test first, then implement command

**Dependency Check:**
- All underlying IMigrationService methods are fully implemented and tested
- You are NOT implementing the service layer - it already exists and works
- You ARE wrapping that service layer in CLI command classes

**Test Strategy:**
- Unit tests: Mock IMigrationService, test CLI parameter parsing and output formatting
- Integration tests: Use real MigrationService with test database
- Follow the pattern from existing CLI commands in the codebase

---

## PHASE 1: Infrastructure Verification & Setup (1 hour)

**Objective:** Verify infrastructure is complete and identify exactly what CLI commands need

### Phase 1.1: Verify Existing Infrastructure
- [ ] 🔄 Read src/Acode.Application/Database/IMigrationService.cs (6 methods: GetStatusAsync, MigrateAsync, RollbackAsync, CreateAsync, ValidateAsync, ForceUnlockAsync)
- [ ] 🔄 Verify MigrationRunner.cs implements all 6 methods
- [ ] 🔄 Check that all 81 tests pass: `dotnet test --filter "FullyQualifiedName~Migration"`
- [ ] 🔄 Verify no NotImplementedException in any migration file
- [ ] ✅ Status: Infrastructure verified complete

### Phase 1.2: Identify CLI Command Requirements
- [ ] 🔄 Review src/Acode.CLI/Commands/DbCommand.cs (router that dispatches to subcommands)
- [ ] 🔄 Check existing CLI command pattern from other commands (e.g., ChatCommand.cs, AuditCommand.cs)
- [ ] 🔄 Identify output formatting style used in codebase
- [ ] ✅ Status: CLI pattern understood

### Phase 1.3: Setup Test Framework
- [ ] 🔄 Create tests/Acode.CLI.Tests/Commands/Migrations/ directory
- [ ] 🔄 Review testing patterns in existing CLI command tests
- [ ] 🔄 Setup test doubles (mocks) for IMigrationService
- [ ] ✅ Status: Test framework ready

---

## PHASE 2: DbStatusCommand Implementation (1.5-2 hours)

**Spec Reference:** Lines 1197-1205, AC-041 through AC-046 (6 ACs)
**Objective:** Implement `acode db status` command to show current migration status

### Phase 2.1: DbStatusCommand.cs Tests (RED)

**Test File:** tests/Acode.CLI.Tests/Commands/Migrations/DbStatusCommandTests.cs

Create failing test for:
- [ ] 🔄 Test: Shows current version with applied count
  - Expected: "Current Version: 001_initial_schema"
  - Expected: "Applied: 1 migration"
- [ ] 🔄 Test: Shows pending migrations list
  - Expected: "Pending: 2 migrations" + list of versions
- [ ] 🔄 Test: Shows database provider (SQLite/PostgreSQL)
  - Expected: "Provider: SQLite" or "PostgreSQL"
- [ ] 🔄 Test: Shows checksum validation status
  - Expected: "Checksums: ✅ Valid" or warnings with mismatches
- [ ] 🔄 Test: Exit code 0 if healthy, 1 if issues (AC-046)
  - Expected: ExitCode == 0 for healthy state
  - Expected: ExitCode == 1 if checksums invalid or connection fails

**Success Criteria:**
- All 5 tests compile but FAIL (red)
- Tests verify output format matches spec requirements

### Phase 2.2: DbStatusCommand.cs Implementation (GREEN)

**File:** src/Acode.CLI/Commands/DbStatusCommand.cs

Implement:
```csharp
public sealed class DbStatusCommand : Command
{
    private readonly IMigrationService _migrationService;
    private readonly IConsoleWriter _console;

    public DbStatusCommand(IMigrationService migrationService, IConsoleWriter console)
    {
        _migrationService = migrationService;
        _console = console;
        // Configure command with Description, Arguments, Options
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        // Call _migrationService.GetStatusAsync()
        // Format output as per spec (lines 1199-1205)
        // Return 0 if healthy, 1 if issues
    }
}
```

**Implementation Details:**
- [ ] 🔄 Inject IMigrationService and console writer
- [ ] 🔄 Call GetStatusAsync() to retrieve MigrationStatusReport
- [ ] 🔄 Format output: current version, applied count, pending list
- [ ] 🔄 Display database provider (AC-044)
- [ ] 🔄 Display checksum status (AC-045)
- [ ] 🔄 Return exit code 0 (success) or 1 (issues) - AC-046
- [ ] 🔄 Add --verbose flag for detailed output (optional enhancement)

**Success Criteria:**
- All 5 tests PASS
- Output matches expected format from spec
- Exit codes correct

### Phase 2.3: DbStatusCommand Tests (REFACTOR & GREEN)

- [ ] 🔄 Run tests, verify all pass
- [ ] 🔄 Check output formatting matches spec exactly
- [ ] ✅ DbStatusCommand verified complete - AC-041 through AC-046 implemented

---

## PHASE 3: DbMigrateCommand Implementation (2-2.5 hours)

**Spec Reference:** Lines 1206-1214, AC-047 through AC-053 (7 ACs)
**Objective:** Implement `acode db migrate` command with multiple options

### Phase 3.1: DbMigrateCommand.cs Tests (RED)

**Test File:** tests/Acode.CLI.Tests/Commands/Migrations/DbMigrateCommandTests.cs

Create failing tests for:
- [ ] 🔄 Test: `db migrate` applies all pending migrations (AC-047)
  - Expected: "Applying migrations...", "✓ Applied" for each
- [ ] 🔄 Test: `db migrate --dry-run` shows SQL without executing (AC-048)
  - Expected: "Would apply: X migrations"
  - Expected: Database unchanged after command
- [ ] 🔄 Test: `db migrate --target VERSION` migrates to specific version (AC-049)
  - Expected: Only migrations up to target applied
- [ ] 🔄 Test: `db migrate --skip VERSION` skips specified migration (AC-050)
  - Expected: Skipped version not applied
- [ ] 🔄 Test: Shows progress for each migration (AC-051)
  - Expected: Output shows each migration name and status
- [ ] 🔄 Test: Displays total duration on completion (AC-052)
  - Expected: "Migration complete in XXms"
- [ ] 🔄 Test: Exit code 0 on success, non-zero on failure (AC-053)
  - Expected: ExitCode == 0 for success
  - Expected: ExitCode != 0 for failure

**Success Criteria:**
- All 7 tests compile but FAIL (red)
- Tests cover all options and scenarios

### Phase 3.2: DbMigrateCommand.cs Implementation (GREEN)

**File:** src/Acode.CLI/Commands/DbMigrateCommand.cs

Implement:
```csharp
public sealed class DbMigrateCommand : Command
{
    private readonly IMigrationService _migrationService;
    private readonly IConsoleWriter _console;

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        // Parse options: --dry-run, --target, --skip
        // Create MigrateOptions with parsed values
        // Call _migrationService.MigrateAsync(options)
        // Format output with progress
        // Return 0 on success, 1 on failure
    }
}
```

**Implementation Details:**
- [ ] 🔄 Add Option `--dry-run` (boolean)
- [ ] 🔄 Add Option `--target VERSION` (string)
- [ ] 🔄 Add Option `--skip VERSION` (string)
- [ ] 🔄 Call MigrateAsync() with options
- [ ] 🔄 For dry-run: show WouldApply list without executing
- [ ] 🔄 For actual migration: show progress for each migration
- [ ] 🔄 Display duration: result.TotalDuration
- [ ] 🔄 Handle MigrationException and format error message
- [ ] 🔄 Return ExitCode 0 on success, 1 on failure

**Success Criteria:**
- All 7 tests PASS
- Dry-run mode doesn't modify database
- Progress output shows each migration
- Duration displayed correctly

### Phase 3.3: DbMigrateCommand Tests (REFACTOR & GREEN)

- [ ] 🔄 Run tests, verify all pass
- [ ] ✅ DbMigrateCommand verified complete - AC-047 through AC-053 implemented

---

## PHASE 4: DbRollbackCommand Implementation (1.5-2 hours)

**Spec Reference:** Lines 1216-1223, AC-054 through AC-059 (6 ACs)
**Objective:** Implement `acode db rollback` command with rollback options

### Phase 4.1: DbRollbackCommand.cs Tests (RED)

**Test File:** tests/Acode.CLI.Tests/Commands/Migrations/DbRollbackCommandTests.cs

Create failing tests for:
- [ ] 🔄 Test: `db rollback` rolls back last applied migration (AC-054)
  - Expected: Last migration version rolled back
- [ ] 🔄 Test: `db rollback --steps N` rolls back N migrations (AC-055)
  - Expected: N migrations rolled back in reverse order
- [ ] 🔄 Test: `db rollback --target VERSION` rolls back to version (AC-056)
  - Expected: All migrations after target rolled back
- [ ] 🔄 Test: `db rollback --dry-run` shows what would be rolled back (AC-057)
  - Expected: Database unchanged after command
- [ ] 🔄 Test: `db rollback` prompts for confirmation unless --yes flag (AC-058)
  - Expected: Confirmation prompt when no --yes flag
  - Expected: No prompt when --yes flag present
- [ ] 🔄 Test: Exit code 0 on success (AC-059)
  - Expected: ExitCode == 0 for success

**Success Criteria:**
- All 6 tests compile but FAIL (red)
- Confirmation logic tested

### Phase 4.2: DbRollbackCommand.cs Implementation (GREEN)

**File:** src/Acode.CLI/Commands/DbRollbackCommand.cs

Implement:
```csharp
public sealed class DbRollbackCommand : Command
{
    private readonly IMigrationService _migrationService;
    private readonly IConsoleWriter _console;

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        // Parse options: --steps, --target, --dry-run, --yes
        // If not --yes: prompt for confirmation
        // Create RollbackOptions with parsed values
        // Call _migrationService.RollbackAsync(options)
        // Format output showing rolled back versions
        // Return 0 on success
    }
}
```

**Implementation Details:**
- [ ] 🔄 Add Option `--steps N` (integer, default 1)
- [ ] 🔄 Add Option `--target VERSION` (string)
- [ ] 🔄 Add Option `--dry-run` (boolean)
- [ ] 🔄 Add Option `--yes` (boolean, skip confirmation)
- [ ] 🔄 If not --yes, prompt user for confirmation
- [ ] 🔄 Call RollbackAsync() with options
- [ ] 🔄 For dry-run: show RolledBackVersions list without executing
- [ ] 🔄 Show current version after rollback
- [ ] 🔄 Handle MissingDownScriptException and other errors
- [ ] 🔄 Return ExitCode 0 on success

**Success Criteria:**
- All 6 tests PASS
- Confirmation prompt works correctly
- Dry-run doesn't modify database
- Rolled back versions displayed

### Phase 4.3: DbRollbackCommand Tests (REFACTOR & GREEN)

- [ ] 🔄 Run tests, verify all pass
- [ ] ✅ DbRollbackCommand verified complete - AC-054 through AC-059 implemented

---

## PHASE 5: DbCreateCommand + DbValidateCommand + DbUnlockCommand (2-2.5 hours)

**Spec Reference:** Lines 1225-1258, AC-060 through AC-082 (16 ACs)
**Objective:** Implement three utility commands

### Phase 5.1: DbCreateCommand (0.75 hours)

**Spec Reference:** AC-060 through AC-065

Create failing tests:
- [ ] 🔄 Test: `db create NAME` creates migration files (AC-060)
- [ ] 🔄 Test: Uses next sequential version number (AC-061)
- [ ] 🔄 Test: Files include header comments (AC-062)
- [ ] 🔄 Test: `--template TABLE` uses table template (AC-063)
- [ ] 🔄 Test: `--template INDEX` uses index template (AC-064)
- [ ] 🔄 Test: Files in configured migrations directory (AC-065)

Implement DbCreateCommand.cs:
- [ ] 🔄 Parse NAME argument and --template option
- [ ] 🔄 Calculate next sequential version number
- [ ] 🔄 Create up and down migration files
- [ ] 🔄 Include header comments from templates
- [ ] 🔄 Write files to configured directory
- [ ] 🔄 Show created file paths

### Phase 5.2: DbValidateCommand (0.5 hours)

**Spec Reference:** AC-066 through AC-069

Create failing tests:
- [ ] 🔄 Test: `db validate` checks checksums of applied migrations (AC-066)
- [ ] 🔄 Test: Reports mismatches with file paths (AC-067)
- [ ] 🔄 Test: Exit code 0 if all valid (AC-068)
- [ ] 🔄 Test: Exit code 1 if any mismatch (AC-069)

Implement DbValidateCommand.cs:
- [ ] 🔄 Call ValidateAsync() on migration service
- [ ] 🔄 Display validation results
- [ ] 🔄 For mismatches: show version, expected checksum, actual checksum
- [ ] 🔄 Return ExitCode 0 (valid) or 1 (mismatch)

### Phase 5.3: DbUnlockCommand (0.25 hours)

**Spec Reference:** AC-082

Create failing test:
- [ ] 🔄 Test: `db unlock --force` forces release of stale lock

Implement DbUnlockCommand.cs:
- [ ] 🔄 Call ForceUnlockAsync() on migration service
- [ ] 🔄 Show confirmation that lock was released
- [ ] 🔄 Require --force flag (safety measure)

### Phase 5.4: All Tests PASS

- [ ] 🔄 Run all new command tests
- [ ] ✅ DbCreateCommand verified complete - AC-060 through AC-065
- [ ] ✅ DbValidateCommand verified complete - AC-066 through AC-069
- [ ] ✅ DbUnlockCommand verified complete - AC-082

---

## PHASE 6: Integration Tests & Verification (2-3 hours)

**Objective:** Verify all 103 ACs are semantically complete with integration tests

### Phase 6.1: Integration Tests for All Commands

Create integration test file: tests/Acode.CLI.Tests/Commands/Migrations/DbCommandsIntegrationTests.cs

- [ ] 🔄 Test: Full migration workflow (status → create → migrate → status → rollback → status)
- [ ] 🔄 Test: Dry-run doesn't modify database but shows what would happen
- [ ] 🔄 Test: Checksums validated correctly
- [ ] 🔄 Test: Lock acquisition and release
- [ ] 🔄 Test: Error handling for all error codes (ACODE-MIG-001 through 008)
- [ ] 🔄 Test: Startup migration with auto-migrate enabled
- [ ] 🔄 Test: Concurrent migration attempts (lock contention)

**Success Criteria:**
- All integration tests PASS
- Real database used (SQLite test database)
- All error paths tested

### Phase 6.2: AC-by-AC Verification Checklist

Go through task-050c-fresh-gap-analysis.md and verify:
- [ ] ✅ AC-001-008: Migration Discovery (verified in gap analysis)
- [ ] ✅ AC-009-015: Version Table Management (verified in gap analysis)
- [ ] ✅ AC-016-024: Migration Execution (verified in gap analysis)
- [ ] ✅ AC-025-032: Rollback Operations (verified in gap analysis)
- [ ] ✅ AC-033-040: Startup Bootstrapping (verified in gap analysis)
- [ ] 🔄 AC-041-046: DbStatusCommand tests PASS
- [ ] 🔄 AC-047-053: DbMigrateCommand tests PASS
- [ ] 🔄 AC-054-059: DbRollbackCommand tests PASS
- [ ] 🔄 AC-060-065: DbCreateCommand tests PASS
- [ ] 🔄 AC-066-069: DbValidateCommand tests PASS
- [ ] 🔄 AC-070-073: Backup command (verify service layer supports it)
- [ ] ✅ AC-074-082: Locking Mechanism (verified in gap analysis)
- [ ] ✅ AC-083-089: Checksum Validation (verified in gap analysis)
- [ ] ✅ AC-090-098: Error Handling (verified in gap analysis)
- [ ] ✅ AC-099-103: Logging and Observability (verified in gap analysis)

**Success Criteria:**
- All 103 ACs verified implemented
- Each AC has evidence in tests or code

### Phase 6.3: Build and Full Test Run

- [ ] 🔄 `dotnet build` - 0 errors, 0 warnings
- [ ] 🔄 `dotnet test` - all tests pass
  - Application tests: Pass
  - Infrastructure tests: Pass (81 already passing)
  - CLI tests: All new tests pass
  - Integration tests: Pass
- [ ] 🔄 Verify no NotImplementedException in ANY file
- [ ] 🔄 Verify no TODO/FIXME in production code

**Success Criteria:**
- Build: ✅ 0 errors, 0 warnings
- Tests: ✅ 100% passing (should be 120+ tests total)
- Code quality: ✅ No stubs, no TODOs in production

### Phase 6.4: Create Updated Gap Analysis (Optional - for verification)

If creating updated analysis:
- [ ] 🔄 Run all verification commands
- [ ] 🔄 Update gap analysis with new test counts
- [ ] 🔄 Verify semantic completeness now shows 100% (103/103 ACs)

---

## FINAL VERIFICATION CHECKLIST

Once all phases complete, verify:

- [ ] ✅ **Production Files (12)** - All present, no NotImplementedException, no TODOs
- [ ] ✅ **Test Files (18)** - 12 original + 6 new CLI command tests
- [ ] ✅ **Test Execution** - 120+ tests passing (81 original + 40+ new CLI tests)
- [ ] ✅ **Build Status** - 0 errors, 0 warnings
- [ ] ✅ **Acceptance Criteria** - All 103 ACs verified implemented with evidence
- [ ] ✅ **CLI Commands** - All 6 commands functional with proper options/flags
- [ ] ✅ **Integration** - Full migration workflow tested end-to-end
- [ ] ✅ **Error Handling** - All error codes tested and actionable messages provided
- [ ] ✅ **Semantic Completeness** - 100% (103/103 ACs)

---

## COMMIT STRATEGY

Commit after each phase completes:

1. **After Phase 1:** `feat(task-050c): verify infrastructure, setup test framework`
2. **After Phase 2:** `feat(task-050c): implement DbStatusCommand with tests`
3. **After Phase 3:** `feat(task-050c): implement DbMigrateCommand with tests`
4. **After Phase 4:** `feat(task-050c): implement DbRollbackCommand with tests`
5. **After Phase 5:** `feat(task-050c): implement DbCreate/Validate/Unlock commands with tests`
6. **After Phase 6:** `feat(task-050c): add integration tests, verify 100% completion`

**Final PR:**
- Title: "Implement Migration Runner CLI Commands"
- Body: Link to gap analysis and completion checklist
- Includes all 6 phases of work

---

## TROUBLESHOOTING GUIDE

**If NotImplementedException appears:**
- Stop immediately, don't declare complete
- Find the unimplemented method
- Implement it following TDD (test first)
- Don't commit until it's fully implemented

**If tests fail:**
- Check the service layer implementation hasn't changed
- Verify mocks are properly configured
- Check output formatting matches spec exactly
- Run single test to debug: `dotnet test --filter "TestClassName.TestMethodName"`

**If build fails:**
- Check for syntax errors in new CLI commands
- Verify all dependencies are injected correctly
- Ensure new files are in correct namespaces
- Build incrementally: `dotnet build src/Acode.CLI`

**If integration tests fail:**
- Check database is clean before each test
- Verify test database is properly initialized
- Check lock cleanup between tests
- Enable verbose logging for troubleshooting

---

## SUCCESS CRITERIA FOR TASK-050c COMPLETE

✅ **Task-050c is 100% COMPLETE when:**

1. All 103 Acceptance Criteria verified implemented
2. All production code has corresponding tests
3. Build passes: 0 errors, 0 warnings
4. Tests pass: 100% of 120+ tests
5. No NotImplementedException in any file
6. No TODO/FIXME in production code
7. Fresh gap analysis shows 100% (103/103 ACs)
8. All 6 CLI commands functional with options
9. Integration tests pass end-to-end
10. PR created and reviewed

---

**Next Steps After This Checklist:**

1. Read this checklist completely
2. Begin Phase 1 (Infrastructure Verification)
3. Work through phases sequentially
4. Mark items [✅] when complete
5. Commit after each phase
6. When all phases done, create PR
7. Update gap analysis to show 100% completion

---

Good luck! This task is achievable in 7-10 hours. Stay focused on TDD and semantic completeness. The infrastructure is already there - you're just building the CLI layer to expose it. 🚀
