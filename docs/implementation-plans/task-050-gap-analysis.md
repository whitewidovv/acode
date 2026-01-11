# Task 050 Gap Analysis - What's Built vs. What's Specified

**Date**: 2026-01-06
**Status**: In Progress
**Estimated Completion**: ~70% of specification missing

## Executive Summary

After implementing ~30% of Task 050 based on reading only Description and Acceptance Criteria sections, I discovered that the **Implementation Prompt** and **Testing Requirements** sections at the end of the task file contain complete working code examples for the entire specification. This gap analysis compares what I built versus what the specification actually requires.

## What I Built (Phase 1-3)

### ✅ Completed Components

#### Phase 1 - Core Abstractions (PARTIAL)
1. ✅ `DbProviderType` enum (SQLite, PostgreSQL)
2. ✅ `IConnectionFactory` interface - **INCOMPLETE** (missing `CheckHealthAsync`, missing `Provider` property)
3. ✅ `IDbConnection` interface with Dapper-style methods
4. ✅ `ITransaction` interface
5. ✅ 3 interface contract tests (passing)

**Gap**: Spec requires `DatabaseProvider` enum (not `DbProviderType`), `HealthCheckResult` record, `HealthStatus` enum

#### Phase 2 - SQLite Provider (PARTIAL)
1. ✅ `SqliteConnectionFactory` - **INCOMPLETE** (missing IOptions<DatabaseOptions>, missing advanced PRAGMAs, wrong constructor signature)
2. ✅ `SqliteConnection` wrapper
3. ✅ `SqliteTransaction` with auto-rollback
4. ✅ Dapper 2.1.35 added to Directory.Packages.props
5. ✅ Microsoft.Data.Sqlite 8.0.0 added
6. ✅ 9 integration tests (passing) - **WRONG FRAMEWORK** (xUnit instead of MSTest)

**Gap**: Spec requires:
- IOptions<DatabaseOptions> dependency injection pattern
- Advanced PRAGMAs: `foreign_keys`, `synchronous`, `temp_store`, `mmap_size`
- `CheckHealthAsync` method with file existence check, integrity check, WAL size reporting
- `DatabaseConnectionException` with error code "ACODE-DB-001"
- IDisposable pattern with `_disposed` field

#### Phase 3 - Migration Repository (PARTIAL)
1. ✅ `MigrationFile` domain model
2. ✅ `AppliedMigration` domain model
3. ✅ `MigrationSource` enum
4. ✅ `MigrationStatus` enum
5. ✅ `IMigrationRepository` interface
6. ✅ `SqliteMigrationRepository` implementation
7. ✅ 11 integration tests (passing) - **WRONG FRAMEWORK** (xUnit instead of MSTest)

**Gap**: Spec calls for completely different migration architecture (see below)

## What the Specification Requires (70% Missing)

### ❌ MISSING: Configuration System (0% Complete)

**Required Classes**:
```csharp
// src/Acode.Infrastructure/Database/DatabaseOptions.cs
public class DatabaseOptions
{
    public LocalDatabaseOptions? Local { get; set; }
    public RemoteDatabaseOptions? Remote { get; set; }
}

public class LocalDatabaseOptions
{
    public string? Path { get; set; }
    public int? BusyTimeoutMs { get; set; }
}

public class RemoteDatabaseOptions
{
    public string? ConnectionString { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? Database { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public SslMode? SslMode { get; set; }
    public int? ConnectionTimeoutSeconds { get; set; }
    public int? CommandTimeoutSeconds { get; set; }
    public PoolOptions? Pool { get; set; }
}

public class PoolOptions
{
    public int? MinSize { get; set; }
    public int? MaxSize { get; set; }
    public int? IdleTimeout { get; set; }
    public int? ConnectionLifetime { get; set; }
}
```

**Impact**: All connection factories require IOptions<DatabaseOptions> injection pattern

### ❌ MISSING: Health Checking System (0% Complete)

**Required Types**:
```csharp
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

public record HealthCheckResult(
    HealthStatus Status,
    string Description,
    IReadOnlyDictionary<string, object>? Data = null);
```

**Required Methods**:
- `IConnectionFactory.CheckHealthAsync(CancellationToken ct)` - missing from interface
- `SqliteConnectionFactory.CheckHealthAsync()` - not implemented
- `PostgresConnectionFactory.CheckHealthAsync()` - entire class missing

**Impact**: Cannot implement health check endpoints, cannot diagnose database issues

### ❌ MISSING: Exception Hierarchy (0% Complete)

**Required Classes**:
```csharp
// src/Acode.Infrastructure/Database/DatabaseException.cs
public class DatabaseConnectionException : Exception
{
    public string ErrorCode { get; }

    public DatabaseConnectionException(string errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
```

**Error Codes Required**:
| Code | Meaning |
|------|---------|
| ACODE-DB-001 | Connection failed |
| ACODE-DB-002 | Migration failed |
| ACODE-DB-003 | Transaction failed |
| ACODE-DB-004 | Database locked |
| ACODE-DB-005 | Schema error |
| ACODE-DB-006 | Constraint violation |
| ACODE-DB-007 | Timeout |
| ACODE-DB-008 | Pool exhausted |
| ACODE-DB-009 | Checksum mismatch |
| ACODE-DB-010 | Validation failed |

**Impact**: No structured error handling, no error codes for diagnostics

### ❌ MISSING: PostgreSQL Support (0% Complete)

**Required Classes**:
```csharp
// src/Acode.Infrastructure/Database/PostgresConnectionFactory.cs
public sealed class PostgresConnectionFactory : IConnectionFactory, IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    // ... (370 lines of implementation code in spec)
}
```

**Features Required**:
- NpgsqlDataSource-based connection pooling
- Environment variable expansion (${VAR} patterns)
- SSL mode configuration
- Pool statistics tracking
- Health checking with pool stats

**Dependencies Required**:
- `Npgsql` NuGet package (not added yet)

**Impact**: Cannot support PostgreSQL databases, only SQLite

### ❌ MISSING: Migration Runner (0% Complete)

**Required Classes**:
```csharp
// src/Acode.Infrastructure/Migrations/IMigrationRunner.cs
public interface IMigrationRunner
{
    Task<MigrationResult> MigrateAsync(string? targetVersion = null, bool dryRun = false, CancellationToken ct = default);
    Task<MigrationResult> RollbackAsync(string? targetVersion = null, CancellationToken ct = default);
    Task<IReadOnlyList<Migration>> GetPendingMigrationsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AppliedMigration>> GetAppliedMigrationsAsync(CancellationToken ct = default);
}

// src/Acode.Infrastructure/Migrations/MigrationRunner.cs
public sealed class MigrationRunner : IMigrationRunner
{
    // ... (300+ lines of implementation code in spec)
}
```

**Features Required**:
- Embedded resource loading from assembly manifest
- SHA-256 checksum validation
- Transaction-based migration execution
- Rollback support with _down.sql scripts
- Migration validation
- Dry-run mode
- Target version support

**Domain Models Required**:
```csharp
public record Migration(string Version, string Content, string? RollbackContent = null);
public record AppliedMigration(string Version, string Checksum, DateTime AppliedAt);
public record MigrationResult(bool Success, int AppliedCount, IReadOnlyList<string> Errors);
```

**Impact**: No way to run migrations, no migration versioning, no rollback capability

### ❌ MISSING: Migration Validation (0% Complete)

**Required Classes**:
```csharp
// src/Acode.Infrastructure/Migrations/MigrationValidator.cs
public class MigrationValidator
{
    public ValidationResult Validate(string sql);
}

public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);
```

**Features Required**:
- SQL syntax validation
- Dangerous operation detection
- Naming convention enforcement

**Impact**: Unsafe migrations can be executed

### ❌ MISSING: CLI Commands (0% Complete)

**Required Classes**:
```csharp
// src/Acode.Cli/Commands/DatabaseCommand.cs
public class DatabaseCommand : AsyncCommand<DatabaseCommand.Settings>
{
    // Subcommands: status, migrate, rollback, schema, backup, verify
    // ... (150+ lines of implementation code in spec)
}
```

**Subcommands Required**:
1. `acode db status` - Show database health and connection info
2. `acode db migrate [--to VERSION] [--dry-run]` - Run migrations
3. `acode db rollback [--to VERSION]` - Rollback migrations
4. `acode db schema [--table NAME]` - Display schema
5. `acode db backup --output PATH` - Backup database
6. `acode db verify` - Verify migration integrity

**Dependencies Required**:
- `Spectre.Console.Cli` NuGet package (not added yet)

**Impact**: No CLI interface for database operations

### ❌ MISSING: Advanced SQLite Configuration (50% Complete)

**Current PRAGMAs**:
- ✅ `journal_mode=WAL`
- ✅ `busy_timeout=5000`

**Missing PRAGMAs**:
- ❌ `foreign_keys=ON` (referential integrity enforcement)
- ❌ `synchronous=NORMAL` (performance optimization)
- ❌ `temp_store=MEMORY` (faster temporary tables)
- ❌ `mmap_size=268435456` (256MB memory-mapped I/O)

**Impact**: Suboptimal SQLite performance, no foreign key enforcement

### ❌ MISSING: Dependency Injection Registration (0% Complete)

**Required**:
- Service registration for IConnectionFactory
- Service registration for IMigrationRunner
- Options pattern configuration
- Health check integration

**Impact**: Cannot wire up services in application startup

### ❌ MISSING: Testing Framework Mismatch (100% Wrong Framework)

**Current**: xUnit (`[Fact]`, `[Theory]`, FluentAssertions)

**Spec Requires**: MSTest (`[TestClass]`, `[TestMethod]`, Assert.AreEqual)

**Impact**: All tests need rewriting or justification for framework deviation

## Detailed Gap Analysis by Component

### 1. IConnectionFactory Interface

**What I Built**:
```csharp
public interface IConnectionFactory
{
    DbProviderType ProviderType { get; }
    string ConnectionString { get; }
    Task<IDbConnection> CreateAsync(CancellationToken cancellationToken = default);
}
```

**What Spec Requires**:
```csharp
public interface IConnectionFactory
{
    Task<IDbConnection> CreateAsync(CancellationToken ct = default);
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default);
    DatabaseProvider Provider { get; }
}
```

**Differences**:
- ❌ Missing `CheckHealthAsync` method
- ❌ Property name mismatch: `ProviderType` vs `Provider`
- ❌ Enum name mismatch: `DbProviderType` vs `DatabaseProvider`
- ❌ Extra property `ConnectionString` not in spec

**Fix Required**: Refactor interface to match spec exactly

### 2. SqliteConnectionFactory

**What I Built**:
```csharp
public SqliteConnectionFactory(
    string databasePath,
    ILogger<SqliteConnectionFactory> logger,
    int busyTimeoutMs = 5000)
{
    // Direct string path, no options pattern
}
```

**What Spec Requires**:
```csharp
public SqliteConnectionFactory(
    IOptions<DatabaseOptions> options,
    ILogger<SqliteConnectionFactory> logger)
{
    // Options pattern with DatabaseOptions.Local.Path
}
```

**Differences**:
- ❌ Wrong constructor signature (string path vs IOptions)
- ❌ Missing 4 PRAGMAs (foreign_keys, synchronous, temp_store, mmap_size)
- ❌ Missing `CheckHealthAsync` implementation
- ❌ Missing IDisposable pattern
- ❌ Not throwing `DatabaseConnectionException` with error codes

**Fix Required**: Complete rewrite following spec pattern

### 3. Migration Architecture Mismatch

**What I Built**:
- `IMigrationRepository` - CRUD for __migrations table
- `SqliteMigrationRepository` - SQLite-specific implementation
- Table name: `__migrations`
- Columns: version, checksum, applied_at, duration_ms, applied_by, status

**What Spec Requires**:
- `IMigrationRunner` - High-level migration orchestration
- `MigrationRunner` - Embedded resource loading, validation, execution
- Table name: `sys_migrations`
- Columns: version, checksum, applied_at (simpler)

**Differences**:
- ❌ Different abstraction level (repository vs runner)
- ❌ Different table name
- ❌ Different column set
- ❌ Missing embedded resource loading
- ❌ Missing validation layer
- ❌ Missing rollback support

**Fix Required**: Keep what I built (it's more detailed), but ADD the spec's MigrationRunner on top

## Test Coverage Gaps

### xUnit Tests I Wrote (20 tests)
- ✅ SqliteConnectionFactoryTests: 9 tests
- ✅ SqliteMigrationRepositoryTests: 11 tests

### MSTest Tests Spec Requires
- ❌ SqliteConnectionFactoryTests: 9 tests (rewrite in MSTest)
- ❌ PostgresConnectionFactoryTests: 5 tests (not written)
- ❌ MigrationRunnerTests: 8 tests (not written)

**Decision Required**: Keep xUnit or convert to MSTest?

## Implementation Priority

### High Priority (Blocking CLI Functionality)
1. **DatabaseOptions configuration system** - Required by all factories
2. **HealthCheckResult + HealthStatus** - Required by IConnectionFactory
3. **Fix IConnectionFactory interface** - Breaks existing code
4. **DatabaseConnectionException hierarchy** - Required by factories
5. **IMigrationRunner + MigrationRunner** - Required by CLI

### Medium Priority (Required for PostgreSQL)
6. **PostgresConnectionFactory** - Add Npgsql support
7. **Add missing SQLite PRAGMAs** - Performance optimization
8. **Environment variable expansion** - Configuration flexibility

### Low Priority (Nice to Have)
9. **DatabaseCommand CLI** - User-facing commands
10. **MigrationValidator** - Safety checks
11. **Convert tests to MSTest** - Match spec framework

## Rollout Strategy

### Phase 4: Configuration + Health Checking (NEXT)
- Create DatabaseOptions, LocalDatabaseOptions, RemoteDatabaseOptions, PoolOptions
- Create HealthStatus enum, HealthCheckResult record
- Update IConnectionFactory interface (breaking change)
- Add CheckHealthAsync to SqliteConnectionFactory
- Create DatabaseConnectionException + error codes
- Update existing tests to use IOptions pattern

**Duration**: 4-6 hours

### Phase 5: Migration Runner
- Create IMigrationRunner interface
- Implement MigrationRunner with embedded resource loading
- Implement checksum validation (SHA-256)
- Implement rollback support
- Create Migration, MigrationResult records
- Rename sys_migrations table (or keep __migrations and map)
- Write MigrationRunnerTests

**Duration**: 6-8 hours

### Phase 6: PostgreSQL Support
- Add Npgsql NuGet package
- Implement PostgresConnectionFactory with NpgsqlDataSource
- Implement environment variable expansion
- Add PostgreSQL-specific migrations
- Write PostgresConnectionFactoryTests

**Duration**: 4-6 hours

### Phase 7: CLI Commands
- Add Spectre.Console.Cli NuGet package
- Implement DatabaseCommand with 6 subcommands
- Wire up services in DI container
- Integration test CLI commands

**Duration**: 4-6 hours

### Phase 8: Test Framework Decision
- Decide: Keep xUnit or convert to MSTest?
- If converting: Rewrite all 20 tests in MSTest format
- If keeping: Justify deviation from spec in audit report

**Duration**: 2-4 hours (if converting)

## Risk Assessment

### High Risk
- **Breaking changes to IConnectionFactory** - Existing tests will fail
- **Constructor signature changes** - Test fixtures need updating
- **Testing framework mismatch** - May require large-scale rewrite

### Medium Risk
- **PostgreSQL dependencies** - May have environment setup issues
- **Embedded resource loading** - Build configuration complexity
- **Environment variable expansion** - Security considerations

### Low Risk
- **Configuration options** - Backward compatible additions
- **CLI commands** - Additive, doesn't affect core

## Questions for User

1. **Testing Framework**: Should I convert all tests from xUnit to MSTest to match spec, or keep xUnit and justify the deviation?

2. **Migration Table Name**: Spec uses `sys_migrations`, I built `__migrations`. Should I:
   - Rename to match spec exactly?
   - Keep `__migrations` and document the difference?
   - Support both for backward compatibility?

3. **Breaking Changes**: Updating IConnectionFactory interface will break existing code. Should I:
   - Update everything in one commit?
   - Create new interface and deprecate old one?
   - Proceed with breaking changes immediately?

4. **Phase Execution**: Should I:
   - Complete all phases before committing anything?
   - Commit after each phase completes?
   - Continue until context runs out, then resume?

## Next Steps

1. Mark gap analysis complete in TodoWrite
2. Start Phase 4 implementation following TDD
3. Create DatabaseOptions configuration classes
4. Update IConnectionFactory interface
5. Fix SqliteConnectionFactory to match spec
6. Continue with Phase 5-8 implementation

---

**Total Estimated Remaining Work**: 20-30 hours
**Current Progress**: 30%
**Target Progress**: 100%
