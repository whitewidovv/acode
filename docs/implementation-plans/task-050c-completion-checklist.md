# Task-050c Completion Checklist: Migration Runner CLI + Startup Bootstrapping

**Status:** 🔄 IN PROGRESS - 67% COMPLETE (67/103 ACs Verified)
**Date:** 2026-01-15
**Analyzer:** Claude Code Gap Analysis Methodology
**Spec Reference:** docs/tasks/refined-tasks/Epic 02/task-050c-migration-runner-startup-bootstrapping.md

---

## INSTRUCTIONS FOR COMPLETION

This checklist represents **all remaining work** needed to reach 100% semantic completion for task-050c. Each item includes:

1. **What Exists:** Current implementation state
2. **What's Missing:** Specific gap to close
3. **Specification Reference:** Line numbers in spec
4. **Implementation Details:** Code examples from spec
5. **Success Criteria:** How to verify completion
6. **Acceptance Criteria Covered:** Which ACs this addresses

### How to Use This Checklist

1. Work through **PHASE 1 → PHASE 5** in order (dependencies exist)
2. For each gap:
   - Read the spec section referenced
   - Implement following TDD (tests first)
   - Mark [✅] when complete with test evidence
   - Commit after each logical unit
3. After all phases: Run full `dotnet test` and audit against acceptance criteria
4. When all checked [✅], task is semantically 100% complete

### TDD Workflow

For each gap:
1. **RED:** Write failing test based on spec
2. **GREEN:** Implement minimum code to pass
3. **REFACTOR:** Clean up, verify no NotImplementedExceptions
4. **COMMIT:** `git commit -m "feat(task-050c): [gap description]"`
5. **MARK COMPLETE:** Check [✅] below

---

## PHASE 1: CLI COMMAND LAYER - FOUNDATION (Hours: 3-4)

**Dependency:** None (can start immediately)
**Deliverable:** DbCommand router + DbStatusCommand, DbMigrateCommand with tests
**Specification:** Section "Implementation Prompt" lines 3726-3734 (CLI Commands file structure)

### Gap 1.1: Create DbCommand Router

**Current State:** ❌ MISSING
**Spec Reference:** lines 3728 (DbCommand.cs in file structure)
**Test Spec Reference:** CLI integration tests section (lines 3088-3095)

**What Exists:**
- Other command routers exist (ChatCommand, SearchCommand) - use as pattern
- IMigrationService interface exists with all methods
- MigrationOptions, RollbackOptions, CreateOptions records exist

**What's Missing:**
- src/Acode.CLI/Commands/DbCommand.cs - Command router
- Subcommand routing logic
- Help text formatting
- Parameter parsing

**Implementation Details (from spec lines 3728):**
```
src/Acode.Cli/Commands/DbCommand.cs - Main router command
```

**Acceptance Criteria Covered:**
- AC-041-073: All CLI commands depend on this router existing

**Test Requirements:**
- Unit test: DbCommand recognizes all subcommands (status, migrate, rollback, create, validate, unlock)
- Unit test: DbCommand displays help when called without subcommand
- Unit test: DbCommand routes to correct subcommand handler

**Success Criteria:**
- [✅] `DbCommand.cs` exists in src/Acode.CLI/Commands/
- [✅] Implements ICommand or Command base class pattern
- [✅] Handles subcommands: status, migrate, rollback, create, validate, unlock
- [✅] All subcommands call IMigrationService methods
- [✅] Tests pass: `dotnet test --filter "DbCommand"`

**Gap Checklist Item:** [ ] 🔄 Implementation complete with tests passing

---

### Gap 1.2: Create DbStatusCommand

**Current State:** ❌ MISSING
**Spec Reference:** lines 1197-1205 (AC-041-046), 3100-3115 (E2E test example)
**Test Spec Reference:** lines 3100-3116 (E2E test for db status)

**What Exists:**
- IMigrationService.GetStatusAsync() method
- MigrationStatusReport record with all properties
- Status output formatting examples in spec

**What's Missing:**
- src/Acode.CLI/Commands/DbStatusCommand.cs
- Output formatting
- Exit code handling

**Implementation Details (from spec lines 1041-1046):**
```
- AC-041: `acode db status` shows current database version
- AC-042: lists all applied migrations with timestamps
- AC-043: lists all pending migrations
- AC-044: shows database provider (SQLite/PostgreSQL)
- AC-045: shows checksum validation status
- AC-046: returns exit code 0 if healthy, 1 if issues
```

**Output Format (from E2E test lines 3112-3115):**
```
Applied: 1
Pending: 1
001_applied
002_pending
```

**Acceptance Criteria Covered:**
- AC-041, AC-042, AC-043, AC-044, AC-045, AC-046 (6 ACs)

**Test Requirements:**
- Unit test: Shows current version when available
- Unit test: Lists applied migrations with timestamps
- Unit test: Lists pending migrations
- Unit test: Shows database provider
- Unit test: Shows checksum validation status
- Unit test: Returns exit code 0 on healthy database
- Unit test: Returns exit code 1 on issues
- Integration test: `acode db status` command execution

**Success Criteria:**
- [✅] `DbStatusCommand.cs` exists in src/Acode.CLI/Commands/
- [✅] Calls IMigrationService.GetStatusAsync()
- [✅] Displays all required information
- [✅] Returns correct exit codes (0 for healthy, 1 for issues)
- [✅] Tests pass: `dotnet test --filter "DbStatus"`
- [✅] E2E test shows correct output format

**Gap Checklist Item:** [ ] 🔄 Implementation complete with tests passing

---

### Gap 1.3: Create DbMigrateCommand

**Current State:** ❌ MISSING
**Spec Reference:** lines 1206-1214 (AC-047-053), 3119-3134 (E2E dry-run test)
**Test Spec Reference:** lines 2568-2705 (MigrationRunnerTests.cs unit tests)

**What Exists:**
- IMigrationService.MigrateAsync(MigrateOptions, ct) method
- MigrateResult record with all properties
- MigrateOptions record with DryRun, TargetVersion, SkipVersion flags
- Output format examples in spec

**What's Missing:**
- src/Acode.CLI/Commands/DbMigrateCommand.cs
- Option parsing (--dry-run, --target, --skip, --force)
- Progress display
- Duration summary
- Exit code handling

**Implementation Details (from spec lines 1047-1053):**
```
- AC-047: `acode db migrate` applies all pending migrations
- AC-048: `acode db migrate --dry-run` shows SQL without executing
- AC-049: `acode db migrate --target VERSION` migrates to specific version
- AC-050: `acode db migrate --skip VERSION` skips specified migration
- AC-051: shows progress for each migration
- AC-052: displays total duration on completion
- AC-053: returns exit code 0 on success, non-zero on failure
```

**Output Format (from E2E test lines 3125-3131):**
```
Would apply:
001_test
No changes made.
```

**Acceptance Criteria Covered:**
- AC-047, AC-048, AC-049, AC-050, AC-051, AC-052, AC-053 (7 ACs)

**Test Requirements:**
- Unit test: Calls MigrateAsync with correct MigrateOptions
- Unit test: --dry-run sets DryRun flag
- Unit test: --target VERSION sets TargetVersion
- Unit test: --skip VERSION sets SkipVersion
- Unit test: Displays progress for each migration
- Unit test: Displays total duration
- Unit test: Returns exit code 0 on success
- Unit test: Returns non-zero on failure
- Integration test: `acode db migrate --dry-run` doesn't modify database
- E2E test: Full migration execution flow

**Success Criteria:**
- [✅] `DbMigrateCommand.cs` exists in src/Acode.CLI/Commands/
- [✅] Parses all command-line options
- [✅] Calls IMigrationService.MigrateAsync()
- [✅] Displays progress and duration
- [✅] Returns correct exit codes
- [✅] Tests pass: `dotnet test --filter "DbMigrate"`
- [✅] E2E test shows correct output format
- [✅] --dry-run doesn't modify database

**Gap Checklist Item:** [ ] 🔄 Implementation complete with tests passing

---

## PHASE 2: CLI COMMANDS - ROLLBACK/CREATE/VALIDATE (Hours: 2-3)

**Dependency:** PHASE 1 (DbCommand router must exist first)
**Deliverable:** DbRollbackCommand, DbCreateCommand, DbValidateCommand with tests
**Specification:** Section "Implementation Prompt" lines 3729-3734

### Gap 2.1: Create DbRollbackCommand

**Current State:** ❌ MISSING
**Spec Reference:** lines 1216-1224 (AC-054-059)
**Test Spec Reference:** lines 2983-3001 (Integration test example)

**What Exists:**
- IMigrationService.RollbackAsync(RollbackOptions, ct) method
- RollbackResult record with all properties
- RollbackOptions record with Steps, TargetVersion, DryRun, Confirm flags

**What's Missing:**
- src/Acode.CLI/Commands/DbRollbackCommand.cs
- Option parsing (--steps, --target, --dry-run, --yes)
- Confirmation prompt
- Duration summary
- Exit code handling

**Implementation Details (from spec lines 1054-1059):**
```
- AC-054: `acode db rollback` rolls back last applied migration
- AC-055: `acode db rollback --steps N` rolls back N migrations
- AC-056: `acode db rollback --target VERSION` rolls back to version
- AC-057: `acode db rollback --dry-run` shows what would be rolled back
- AC-058: prompts for confirmation unless --yes flag
- AC-059: returns exit code 0 on success
```

**Acceptance Criteria Covered:**
- AC-054, AC-055, AC-056, AC-057, AC-058, AC-059 (6 ACs)

**Test Requirements:**
- Unit test: Calls RollbackAsync with correct RollbackOptions
- Unit test: --steps N sets Steps parameter
- Unit test: --target VERSION sets TargetVersion
- Unit test: --dry-run sets DryRun flag
- Unit test: --yes flag bypasses confirmation prompt
- Unit test: Prompts for confirmation when --yes not set
- Unit test: Returns exit code 0 on success
- Integration test: `acode db rollback --dry-run` shows what would be rolled back
- Integration test: Multiple --steps rolls back correctly

**Success Criteria:**
- [✅] `DbRollbackCommand.cs` exists in src/Acode.CLI/Commands/
- [✅] Parses all command-line options
- [✅] Prompts for confirmation (unless --yes)
- [✅] Calls IMigrationService.RollbackAsync()
- [✅] Returns correct exit codes
- [✅] Tests pass: `dotnet test --filter "DbRollback"`

**Gap Checklist Item:** [ ] 🔄 Implementation complete with tests passing

---

### Gap 2.2: Create DbCreateCommand

**Current State:** ❌ MISSING
**Spec Reference:** lines 1225-1232 (AC-060-065), 3085-3096 (E2E test)
**Test Spec Reference:** lines 3085-3096 (E2E test for db create)

**What Exists:**
- IMigrationService.CreateAsync(CreateOptions, ct) method
- CreateResult record with all properties
- CreateOptions record with Name, Template, NoDown flags
- Migration template examples

**What's Missing:**
- src/Acode.CLI/Commands/DbCreateCommand.cs
- Next version number generation
- File creation
- Template support (TABLE, INDEX)
- Header comment generation

**Implementation Details (from spec lines 1060-1065):**
```
- AC-060: `acode db create NAME` creates new migration files
- AC-061: Created files use next sequential version number
- AC-062: Created files include header comments with metadata
- AC-063: `acode db create --template TABLE` uses table template
- AC-064: `acode db create --template INDEX` uses index template
- AC-065: Created files are placed in configured migrations directory
```

**Acceptance Criteria Covered:**
- AC-060, AC-061, AC-062, AC-063, AC-064, AC-065 (6 ACs)

**Test Requirements:**
- Unit test: Calls CreateAsync with migration name
- Unit test: Next sequential version number assigned
- Unit test: Creates both up and down files
- Unit test: Includes header comments with metadata
- Unit test: --template TABLE uses table template
- Unit test: --template INDEX uses index template
- Unit test: Files placed in configured directory
- E2E test: `acode db create add_users` creates migration files

**Success Criteria:**
- [✅] `DbCreateCommand.cs` exists in src/Acode.CLI/Commands/
- [✅] Accepts migration name as argument
- [✅] Parses --template option
- [✅] Calls IMigrationService.CreateAsync()
- [✅] Creates up and down files with headers
- [✅] Tests pass: `dotnet test --filter "DbCreate"`

**Gap Checklist Item:** [ ] 🔄 Implementation complete with tests passing

---

### Gap 2.3: Create DbValidateCommand

**Current State:** ❌ MISSING
**Spec Reference:** lines 1234-1239 (AC-066-069)
**Test Spec Reference:** ValidationResult handling in MigrationRunnerTests

**What Exists:**
- IMigrationService.ValidateAsync(ct) method
- ValidationResult record with Mismatches array
- ChecksumMismatch record with version, expected, actual

**What's Missing:**
- src/Acode.CLI/Commands/DbValidateCommand.cs
- Output formatting for mismatches
- Exit code handling

**Implementation Details (from spec lines 1066-1069):**
```
- AC-066: `acode db validate` checks checksums of all applied migrations
- AC-067: reports mismatches with file paths
- AC-068: returns exit code 0 if all valid
- AC-069: returns exit code 1 if any mismatch
```

**Acceptance Criteria Covered:**
- AC-066, AC-067, AC-068, AC-069 (4 ACs)

**Test Requirements:**
- Unit test: Calls ValidateAsync()
- Unit test: Displays all mismatches with details
- Unit test: Returns exit code 0 when valid
- Unit test: Returns exit code 1 when mismatches
- Integration test: Detects modified migration files

**Success Criteria:**
- [✅] `DbValidateCommand.cs` exists in src/Acode.CLI/Commands/
- [✅] Calls IMigrationService.ValidateAsync()
- [✅] Displays mismatches clearly
- [✅] Returns correct exit codes (0 = valid, 1 = mismatch)
- [✅] Tests pass: `dotnet test --filter "DbValidate"`

**Gap Checklist Item:** [ ] 🔄 Implementation complete with tests passing

---

### Gap 2.4: Create DbUnlockCommand

**Current State:** ❌ MISSING
**Spec Reference:** lines 1257-1258 (AC-082 reference to `acode db unlock --force`)
**Test Spec Reference:** MigrationLockTests pattern (lines 2865-2908)

**What Exists:**
- IMigrationService.ForceUnlockAsync(ct) method
- IMigrationLock.ForceReleaseAsync() in interface
- Lock implementation in FileMigrationLock.cs and PostgreSqlAdvisoryLock.cs

**What's Missing:**
- src/Acode.CLI/Commands/DbUnlockCommand.cs
- Force unlock command logic
- Confirmation handling

**Implementation Details (from spec line 1258):**
```
- AC-082: `acode db unlock --force` manually releases stale lock
```

**Acceptance Criteria Covered:**
- AC-082 (partially - lock mechanism exists, CLI missing)

**Test Requirements:**
- Unit test: Calls ForceUnlockAsync()
- Unit test: Prompts for confirmation (for safety)
- Unit test: Returns success message
- Integration test: Releases stale lock

**Success Criteria:**
- [✅] `DbUnlockCommand.cs` exists in src/Acode.CLI/Commands/
- [✅] Calls IMigrationService.ForceUnlockAsync()
- [✅] Prompts for confirmation before force unlock
- [✅] Tests pass: `dotnet test --filter "DbUnlock"`

**Gap Checklist Item:** [ ] 🔄 Implementation complete with tests passing

---

## PHASE 3: STARTUP BOOTSTRAPPING REGISTRATION (Hours: 1-2)

**Dependency:** PHASE 1 (CLI foundation needed for manual testing)
**Deliverable:** Host integration + DI registration + E2E tests
**Specification:** Section "Startup Bootstrap Sequence" lines 119-189

### Gap 3.1: Register MigrationBootstrapper as IHostedService

**Current State:** ⚠️ PARTIAL (MigrationBootstrapper.cs exists, but not registered)
**Spec Reference:** lines 119-189 (startup sequence diagram), 2353-3055 (test code)
**Test Spec Reference:** lines 2963-3081 (MigrationRunnerIntegrationTests), 3067-3082 (E2E startup test)

**What Exists:**
- MigrationBootstrapper.cs (250+ lines) - implements startup logic
- BootstrapAsync() method with configuration checks
- Auto-migrate configuration handling
- Application startup checks

**What's Missing:**
- Registration as IHostedService in DI container
- Hook into application startup (IApplicationBuilder or IHostBuilder)
- Configuration binding for database.autoMigrate
- Startup blocking until migrations complete
- E2E verification

**Implementation Details (from spec lines 2963-3081):**
```csharp
// Expected in Program.cs or Startup.cs:
services.AddMigrationServices();
services.AddHostedService<MigrationBootstrapper>();

// Or in IHostApplicationBuilder:
app.Services.AddMigrationServices();
app.Services.AddHostedService<MigrationBootstrapper>();
```

**Acceptance Criteria Covered:**
- AC-033: Application startup checks for pending migrations
- AC-034: Auto-migrate applies pending if `database.autoMigrate: true`
- AC-035: Startup blocks until all migrations complete
- AC-036: Startup logs migration activity
- AC-037: Startup fails fast if migration fails
- AC-038: Startup respects `autoMigrate: false`
- AC-039: Displays migration summary
- AC-040: Timeout configurable (default 120s)

**Test Requirements:**
- Unit test: MigrationBootstrapper.StartAsync() calls discovery
- Unit test: Blocks startup when migrations pending and autoMigrate=true
- Unit test: Continues startup when no pending migrations
- Unit test: Logs migration summary to console
- Unit test: Fails application startup on migration failure
- Integration test: Complete startup flow with migrations
- E2E test: `acode run --test-mode "echo hello"` auto-applies pending migrations (lines 3067-3082)

**Success Criteria:**
- [✅] MigrationBootstrapper registered as IHostedService
- [✅] IHostedService.StartAsync() calls BootstrapAsync()
- [✅] Configuration binding for database.autoMigrate works
- [✅] Startup blocks until migrations complete
- [✅] Timeout is configurable
- [✅] Tests pass: `dotnet test --filter "MigrationBootstrapper"`
- [✅] E2E test shows "Applying" message in startup output

**Gap Checklist Item:** [ ] 🔄 Implementation complete with tests passing

---

### Gap 3.2: Update DI Configuration

**Current State:** ⚠️ PARTIAL (MigrationServiceCollectionExtensions.cs exists, may need updates)
**Spec Reference:** lines 3723-3724 (file structure), implementation prompt code
**Test Spec Reference:** DI setup in integration tests (lines 2946-2950)

**What Exists:**
- MigrationServiceCollectionExtensions.cs likely has AddMigrationServices()
- Probably registers: IMigrationRunner, IMigrationService, core services

**What's Missing:**
- Verify all required services registered (IMigrationDiscovery, IMigrationRepository, IMigrationExecutor, IMigrationLock, IMigrationValidator)
- Security validators registration (MigrationSqlValidator, PrivilegeEscalationDetector)
- Configuration binding for MigrationOptions
- Startup timeout configuration

**Implementation Details (expected services to register):**
```csharp
services.AddScoped<IMigrationService, MigrationRunner>();
services.AddScoped<IMigrationDiscovery, MigrationDiscovery>();
services.AddScoped<IMigrationRepository, MigrationRepository>();
services.AddScoped<IMigrationExecutor, MigrationExecutor>();
services.AddScoped<IMigrationLock, DistributedMigrationLock>();
services.AddScoped<IMigrationValidator, MigrationValidator>();
services.AddScoped<MigrationSqlValidator>();
services.AddScoped<PrivilegeEscalationDetector>();
services.AddScoped<MigrationLockGuard>();
services.Configure<MigrationOptions>(config.GetSection("database"));
```

**Test Requirements:**
- Unit test: AddMigrationServices() registers IMigrationService
- Unit test: All core interfaces have registered implementations
- Unit test: Security validators registered
- Unit test: Configuration binding works

**Success Criteria:**
- [✅] All required services registered
- [✅] Configuration options properly bound
- [✅] No missing service exceptions at runtime
- [✅] Tests pass: Integration test DI setup

**Gap Checklist Item:** [ ] 🔄 Implementation complete with tests passing

---

### Gap 3.3: Create E2E Startup Tests

**Current State:** ❌ MISSING (MigrationE2ETests.cs)
**Spec Reference:** lines 3064-3135 (E2E tests section), 3067-3082 (startup bootstrap test)
**Test Spec Reference:** Full E2E test code in spec

**What's Missing:**
- tests/Acode.E2E.Tests/Migrations/MigrationE2ETests.cs
- Startup auto-bootstrap verification
- `db create` command E2E test
- `db status` command E2E test
- `db migrate --dry-run` command E2E test

**Test Cases (from spec lines 3067-3135):**
1. Startup_BootstrapsMigrationsAutomatically (lines 3067-3082)
2. DbCreate_CreatesNewMigrationFiles (lines 3085-3096)
3. DbStatus_ShowsMigrationStatus (lines 3100-3115)
4. DbMigrateDryRun_ShowsPreview (lines 3119-3134)

**Acceptance Criteria Covered:**
- AC-033, AC-034, AC-035, AC-036, AC-037, AC-039, AC-040 (startup ACs)
- AC-060, AC-061, AC-062, AC-065 (create ACs)
- AC-041, AC-042, AC-043, AC-045, AC-046 (status ACs)
- AC-048, AC-051, AC-052 (migrate dry-run ACs)

**Test Requirements (from spec):**
- 4+ E2E test methods
- Each tests full CLI flow from `acode` command
- Verify output format
- Verify database state changes
- Verify exit codes

**Success Criteria:**
- [✅] `MigrationE2ETests.cs` exists in tests/Acode.E2E.Tests/Migrations/
- [✅] 4+ test methods covering all key scenarios
- [✅] All tests passing: `dotnet test --filter "MigrationE2E"`
- [✅] Output matches expected format
- [✅] Database state verified after each test

**Gap Checklist Item:** [ ] 🔄 Implementation complete with tests passing

---

## PHASE 4: SECURITY VALIDATORS COMPLETION (Hours: 2-3)

**Dependency:** PHASE 3 (DI registration complete)
**Deliverable:** MigrationSqlValidator, PrivilegeEscalationDetector fully implemented with tests
**Specification:** Section "Security Considerations" lines 1317-1831

### Gap 4.1: Verify/Complete MigrationSqlValidator Implementation

**Current State:** ⚠️ UNCLEAR (may exist in MigrationValidator.cs or missing)
**Spec Reference:** lines 1327-1383 (Threat 1: SQL Injection mitigation code), 1070 (AC-023)
**Test Spec Reference:** Security validation patterns should be tested

**What Exists:**
- MigrationValidator.cs in infrastructure
- Validation logic calls before execution

**What's Missing (if not already in MigrationValidator.cs):**
- Check for DROP DATABASE pattern
- Check for DROP SCHEMA pattern
- Check for TRUNCATE __migrations pattern
- Check for DELETE FROM __migrations pattern
- Check for INTO OUTFILE pattern
- Check for LOAD_FILE pattern
- Check for xp_cmdshell pattern
- Check for sp_executesql pattern
- Verification that validation runs BEFORE execution

**Implementation Details (from spec lines 1333-1347):**
```csharp
private static readonly string[] ForbiddenPatterns = new[]
{
    @"DROP\s+DATABASE",
    @"DROP\s+SCHEMA",
    @"TRUNCATE\s+TABLE\s+__migrations",
    @"DELETE\s+FROM\s+__migrations",
    @"INTO\s+OUTFILE",
    @"INTO\s+DUMPFILE",
    @"LOAD_FILE\s*\(",
    @"xp_cmdshell",
    @"sp_executesql",
    @"EXEC\s*\(",
    @"EXECUTE\s+IMMEDIATE",
    @"--\s*BYPASS",
    @"/\*.*ADMIN.*\*/"
};
```

**Acceptance Criteria Covered:**
- AC-023: MigrationRunner validates SQL patterns before execution
- AC-024: MigrationRunner blocks migrations with privilege escalation patterns

**Test Requirements:**
- Unit test: Blocks DROP DATABASE pattern
- Unit test: Blocks TRUNCATE __migrations pattern
- Unit test: Blocks multiple dangerous patterns
- Unit test: Logs warning for detected patterns
- Unit test: Migration fails when forbidden pattern found (unless --force)
- Integration test: Dangerous migration is rejected before execution

**Success Criteria:**
- [✅] Forbidden patterns clearly defined
- [✅] Validation runs before each migration
- [✅] Dangerous migrations are blocked
- [✅] Tests pass: `dotnet test --filter "SqlValidator"`
- [✅] No NotImplementedException in validation code

**Gap Checklist Item:** [ ] 🔄 Implementation complete with tests passing

---

### Gap 4.2: Verify/Complete PrivilegeEscalationDetector Implementation

**Current State:** ❌ MISSING (not found in code)
**Spec Reference:** lines 1644-1730 (Threat 4: Privilege Escalation mitigation code)
**Test Spec Reference:** Security pattern detection tests

**What's Missing:**
- Detection of GRANT ALL pattern
- Detection of GRANT SUPERUSER pattern
- Detection of CREATE USER pattern
- Detection of CREATE ROLE pattern
- Detection of ALTER USER PASSWORD pattern
- Detection of SECURITY DEFINER pattern
- Detection of SET ROLE pattern
- Detection of pg_read_server_files pattern
- Severity levels (Critical, High, Medium)
- Log security events
- Optional manual approval requirement

**Implementation Details (from spec lines 1649-1664):**
```csharp
private static readonly string[] DangerousPatterns = new[]
{
    @"GRANT\s+ALL",
    @"GRANT\s+.*SUPERUSER",
    @"GRANT\s+.*ADMIN",
    @"CREATE\s+USER",
    @"CREATE\s+ROLE",
    @"ALTER\s+USER.*PASSWORD",
    @"ALTER\s+ROLE.*PASSWORD",
    @"SECURITY\s+DEFINER",
    @"SET\s+ROLE",
    @"SET\s+SESSION\s+AUTHORIZATION",
    @"pg_read_server_files",
    @"pg_write_server_files",
    @"pg_execute_server_program"
};
```

**Acceptance Criteria Covered:**
- AC-024: Blocks migrations with privilege escalation patterns

**Test Requirements:**
- Unit test: Detects GRANT ALL pattern
- Unit test: Detects CREATE USER pattern
- Unit test: Detects CREATE ROLE pattern
- Unit test: Detects ALTER USER PASSWORD pattern
- Unit test: Detects SECURITY DEFINER pattern
- Unit test: Assigns correct severity levels (Critical > High > Medium)
- Unit test: Logs warning for HIGH severity patterns
- Unit test: Blocks Critical severity patterns unless --allow-privileged
- Integration test: Migration with privilege pattern is rejected

**Success Criteria:**
- [✅] `PrivilegeEscalationDetector.cs` exists in src/Acode.Infrastructure/Persistence/Migrations/
- [✅] All dangerous patterns defined
- [✅] Severity levels assigned correctly
- [✅] Security events logged at WARNING/ERROR level
- [✅] Critical patterns block migration
- [✅] Tests pass: `dotnet test --filter "PrivilegeEscalation"`

**Gap Checklist Item:** [ ] 🔄 Implementation complete with tests passing

---

### Gap 4.3: Verify/Complete MigrationLockGuard Implementation

**Current State:** ❌ MISSING (DoS protection)
**Spec Reference:** lines 1747-1799 (Threat 5: DoS via lock exhaustion mitigation)
**Test Spec Reference:** Lock health check tests

**What's Missing:**
- DoS protection for lock exhaustion
- Max lock duration enforcement
- Force release of old locks
- Security event recording
- Warning threshold before force release

**Implementation Details (from spec lines 1761-1762):**
```csharp
public sealed class MigrationLockGuard
{
    private readonly TimeSpan _maxLockDuration;      // Default: 10 minutes
    private readonly TimeSpan _warningThreshold;     // Default: just before max
}
```

**Acceptance Criteria Covered:**
- AC-075: Lock prevents concurrent migrations (this guards the lock itself)
- AC-081: Stale locks auto-released with warning

**Test Requirements:**
- Unit test: Detects lock held > max duration
- Unit test: Force releases stale lock
- Unit test: Logs security event when releasing
- Unit test: Logs warning at threshold
- Integration test: DoS attempt blocked

**Success Criteria:**
- [✅] `MigrationLockGuard.cs` exists in src/Acode.Infrastructure/Persistence/Migrations/
- [✅] Max lock duration enforced (default 10 min)
- [✅] Stale locks force released
- [✅] Security events recorded
- [✅] Tests pass: `dotnet test --filter "LockGuard"`

**Gap Checklist Item:** [ ] 🔄 Implementation complete with tests passing

---

## PHASE 5: BACKUP INTEGRATION & E2E + BENCHMARKS (Hours: 2-3)

**Dependency:** PHASE 4 (everything else must be working)
**Deliverable:** Backup commands + comprehensive benchmarks
**Specification:** Section "CLI Commands - db backup" lines 1241-1246

### Gap 5.1: Implement Backup Integration

**Current State:** ❌ MISSING (backup not integrated with migrations)
**Spec Reference:** lines 1241-1246 (AC-070-073)
**Test Spec Reference:** Backup creation before migration

**What's Missing:**
- Backup creation before migration (when enabled)
- Backup pruning by retention days
- Backup directory configuration
- `acode db backup` command CLI support

**Implementation Details (from spec lines 1070-1073):**
```
- AC-070: `acode db backup` creates timestamped database backup
- AC-071: `acode db backup --path DIR` specifies backup directory
- AC-072: Backup is created before migration if `backup.enabled: true`
- AC-073: Old backups are pruned based on `backup.retentionDays`
```

**Acceptance Criteria Covered:**
- AC-070, AC-071, AC-072, AC-073 (4 ACs)

**Test Requirements:**
- Unit test: Backup created before migration when enabled
- Unit test: Backup skipped when disabled
- Unit test: Old backups pruned by retention days
- Unit test: Timestamped backup files created
- Integration test: Migration with backup flow
- Integration test: Backup restoration possible

**Success Criteria:**
- [✅] Backup created before each migration
- [✅] Configurable retention days
- [✅] Old backups pruned
- [✅] Timestamped filenames
- [✅] Tests pass: `dotnet test --filter "Backup"`

**Gap Checklist Item:** [ ] 🔄 Implementation complete with tests passing

---

### Gap 5.2: Create MigrationBenchmarks.cs

**Current State:** ❌ MISSING (performance benchmarks)
**Spec Reference:** lines 3140-3199 (Benchmarks section)
**Test Spec Reference:** Full benchmark code in spec

**What's Missing:**
- tests/Acode.Benchmarks/Migrations/MigrationBenchmarks.cs
- Status check benchmark (10 applied migrations)
- Single migration apply benchmark
- Checksum validation benchmark
- Setup/teardown for benchmarks

**Implementation Details (from spec lines 3176-3197):**
```csharp
[Benchmark(Description = "Status check (10 applied)")]
public async Task<MigrationStatus> StatusCheck()
[Benchmark(Description = "Single migration apply")]
public async Task<MigrationResult> SingleMigration()
[Benchmark(Description = "Checksum validation")]
public async Task<ChecksumValidationResult> ChecksumValidation()
```

**Test Requirements (BenchmarkDotNet):**
- Setup initializes database with 10 migrations
- Status check benchmark runs
- Single migration benchmark runs
- Checksum validation benchmark runs
- Results output in console/file

**Success Criteria:**
- [✅] `MigrationBenchmarks.cs` exists in tests/Acode.Benchmarks/Migrations/
- [✅] 3+ benchmark methods
- [✅] Benchmarks run successfully: `dotnet run -c Release` in Benchmarks project
- [✅] Performance metrics captured

**Gap Checklist Item:** [ ] 🔄 Implementation complete with benchmark runs successful

---

### Gap 5.3: Final Audit Against All 103 Acceptance Criteria

**Current State:** ⚠️ NOT YET STARTED (comprehensive verification)
**Spec Reference:** lines 1140-1289 (all 103 ACs)
**Test Spec Reference:** All test files

**What's Missing:**
- Systematic verification of all 103 ACs
- Cross-reference of AC coverage in code
- Documentation of completion evidence
- Final test run with all tests passing

**Verification Steps:**
1. Run full test suite: `dotnet test`
2. For each AC category, verify:
   - Code implementation exists (no NotImplementedException)
   - Tests pass
   - Tests cover all scenarios
   - Logging at appropriate levels
3. Create evidence document
4. Mark audit complete

**Success Criteria:**
- [✅] All 103 ACs verified implemented
- [✅] All test suites passing (100+ total tests)
- [✅] No NotImplementedException in any file
- [✅] Build succeeds with 0 errors, 0 warnings
- [✅] E2E tests pass
- [✅] Benchmarks complete successfully
- [✅] Audit documentation created

**Gap Checklist Item:** [ ] 🔄 Final audit complete - 100% semantic completion verified

---

## SUMMARY: WORK REMAINING

| Phase | Description | Hours | AC Coverage | Status |
|-------|-------------|-------|------------|--------|
| 1 | CLI Foundation (Router + Status + Migrate) | 3-4 | 13 ACs | [ ] 🔄 |
| 2 | CLI Complete (Rollback + Create + Validate + Unlock) | 2-3 | 20 ACs | [ ] 🔄 |
| 3 | Startup Bootstrapping + DI + E2E tests | 1-2 | 8 ACs | [ ] 🔄 |
| 4 | Security Validators Complete | 2-3 | 2 ACs | [ ] 🔄 |
| 5 | Backup + Benchmarks + Final Audit | 2-3 | 4 ACs | [ ] 🔄 |
| **TOTAL** | **Full Completion** | **10-15** | **67 remaining → 100%** | [ ] 🔄 |

---

## COMPLETION SIGN-OFF

When all phases complete and all items checked [✅]:

```
✅ PHASE 1: CLI Foundation - 13 ACs verified
✅ PHASE 2: CLI Complete - 20 ACs verified
✅ PHASE 3: Startup Bootstrapping - 8 ACs verified
✅ PHASE 4: Security Validators - 2 ACs verified
✅ PHASE 5: Backup + Benchmarks + Audit - 4 ACs verified
✅ FINAL: All 103 ACs verified - 100% semantic completion
```

**Task-050c Status: ✅ COMPLETE**
- Implementation: 100% per spec
- Tests: 100+ passing
- Build: 0 errors, 0 warnings
- Audit: PASSED
- Ready for: PR review

---
