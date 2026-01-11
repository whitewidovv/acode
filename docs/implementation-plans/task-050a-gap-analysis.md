# Task 050a Gap Analysis: Workspace DB Layout & Migration Strategy

## Status: In Progress
**Created**: 2026-01-06
**Last Updated**: 2026-01-06

## Specification Source
- Task File: `docs/tasks/refined-tasks/Epic 02/task-050a-workspace-db-layout-migration-strategy.md`
- Lines: 4014 total (Testing Requirements: 2118-3194, Implementation Prompt: 3195-4014)

## What Already Exists

### Application Layer (Acode.Application/Database/)
- ✅ **MigrationFile.cs** - Record for migration metadata (compatible with spec)
- ✅ **MigrationSource.cs** - Enum for Embedded vs File source
- ✅ **IMigrationRepository.cs** - Interface for migration tracking
- ✅ **AppliedMigration.cs** - Record for applied migrations
- ⚠️  **ValidationResult.cs** (in Configuration namespace) - DIFFERENT structure than spec requires

### Infrastructure Layer (Acode.Infrastructure/Database/)
- ✅ **SqliteMigrationRepository.cs** - Implementation of IMigrationRepository
- ✅ **SqliteConnectionFactory.cs** - Connection factory with IOptions pattern
- ✅ **DatabaseOptions.cs** - Configuration classes

## What's MISSING (Task 050a Scope)

### Domain Models - NEW (src/Acode.Domain/Database/)
1. ❌ **ColumnSchema.cs** - Database column schema definition
   - Properties: Name, DataType, IsNullable, IsPrimaryKey, IsForeignKey, etc.
   - Computed: IsTimestamp, IsBoolean, IsId

2. ❌ **TableSchema.cs** - Database table schema definition
   - Properties: Name, Columns, Indexes
   - Computed: PrimaryKey, ForeignKeys, DomainPrefix

3. ❌ **ValidationResult.cs** - Simple validation result for DB schema
   - **NOTE**: Different from Application.Configuration.ValidationResult
   - Properties: IsValid, Errors (string list), Warnings (string list)
   - Methods: Success(), Failure(), WithWarnings(), Combine()

### Infrastructure Validators - NEW (src/Acode.Infrastructure/Database/Layout/)
1. ❌ **NamingConventionValidator.cs** - 700+ lines
   - ValidateTableName()
   - ValidateColumnName()
   - ValidateIndexName()
   - ValidatePrimaryKey()
   - ValidateForeignKeyColumn()
   - ValidateTimestampColumn()
   - ValidateBooleanColumn()
   - Uses GeneratedRegex for patterns

2. ❌ **DataTypeValidator.cs** - 200+ lines
   - ValidateIdColumn()
   - ValidateTimestampColumn()
   - ValidateBooleanColumn()
   - ValidateJsonColumn()
   - ValidateForeignKeyColumn()
   - ValidateEnumColumn()

3. ❌ **SchemaConventions.cs** - (not explicitly defined, may be constants file)

### Migration Infrastructure - NEW (src/Acode.Infrastructure/Database/Migrations/)
1. ❌ **MigrationFileValidator.cs** - 300+ lines
   - ValidateFileName() - NNN_description.sql pattern
   - ValidateDownScriptExists() - matching _down.sql
   - ValidateContent() - forbidden patterns, header comments, idempotency
   - ExtractVersion() - parse version from filename
   - ValidateMigrationSequence() - detect gaps

2. ❌ **MigrationLoader.cs** - (mentioned in tests, not in Implementation Prompt code)
   - LoadAllMigrations()
   - Load from embedded resources
   - Load from filesystem

3. ❌ **MigrationSqlValidator.cs** - (mentioned in file structure)
   - SQL-specific validation

### Schema Infrastructure - NEW (src/Acode.Infrastructure/Database/Schema/)
1. ❌ **SchemaInspector.cs** - (mentioned in file structure)
   - Inspect actual database schema
   - Compare against conventions

### Migration SQL Files - NEW (migrations/)
1. ❌ **001_initial_schema.sql** + **001_initial_schema_down.sql**
   - __migrations table
   - sys_config, sys_locks, sys_health tables
   - Indexes

2. ❌ **002_add_conversations.sql** + **002_add_conversations_down.sql**
   - conv_chats, conv_runs, conv_messages, conv_tool_calls tables
   - 8 indexes for conversation domain

3. ❌ **003_add_sessions.sql** + **003_add_sessions_down.sql**
   - Session domain tables (not fully specified in Implementation Prompt)

4. ❌ **004_add_approvals.sql** + **004_add_approvals_down.sql**
   - Approval domain tables (not fully specified)

5. ❌ **005_add_sync.sql** + **005_add_sync_down.sql**
   - Sync domain tables (not fully specified)

### Tests - ALL NEW
**Unit Tests** (tests/Acode.Infrastructure.Tests/Database/Layout/):
- ❌ NamingConventionValidatorTests.cs (270+ lines, 10+ test methods)
- ❌ DataTypeValidatorTests.cs (150+ lines, 6+ test methods)
- ❌ MigrationFileValidatorTests.cs (120+ lines, 6+ test methods)

**Integration Tests** (tests/Acode.Integration.Tests/Database/Layout/):
- ❌ SchemaValidationIntegrationTests.cs (200+ lines, 8+ test methods)
- ❌ MigrationOrderIntegrationTests.cs (100+ lines, 4+ test methods)

**E2E Tests** (tests/Acode.E2E.Tests/Database/):
- ❌ LayoutE2ETests.cs (100+ lines, 3+ test methods)

### Documentation - NEW
1. ❌ **docs/database/conventions.md**
   - Table naming rules
   - Column naming rules
   - Data type conventions
   - Index conventions
   - Migration conventions

## Implementation Strategy (TDD)

### Phase 1: Domain Models (Foundation)
1. Write tests for ValidationResult (RED)
2. Implement ValidationResult (GREEN)
3. Write tests for ColumnSchema (RED)
4. Implement ColumnSchema (GREEN)
5. Write tests for TableSchema (RED)
6. Implement TableSchema (GREEN)
7. Commit: "feat(task-050a): add domain models for database schema"

### Phase 2: Naming Convention Validator
1. Write tests for ValidateTableName (RED)
2. Implement ValidateTableName (GREEN)
3. Write tests for ValidateColumnName (RED)
4. Implement ValidateColumnName (GREEN)
5. Write tests for ValidateIndexName (RED)
6. Implement ValidateIndexName (GREEN)
7. Continue for all validation methods...
8. Commit: "feat(task-050a): add naming convention validator"

### Phase 3: Data Type Validator
1. Write tests for each validator method (RED)
2. Implement each method (GREEN)
3. Commit: "feat(task-050a): add data type validator"

### Phase 4: Migration File Validator
1. Write tests for filename validation (RED)
2. Implement (GREEN)
3. Write tests for content validation (RED)
4. Implement (GREEN)
5. Write tests for sequence validation (RED)
6. Implement (GREEN)
7. Commit: "feat(task-050a): add migration file validator"

### Phase 5: Migration SQL Files
1. Create migrations/ directory structure
2. Write 001_initial_schema.sql + _down.sql
3. Write 002_add_conversations.sql + _down.sql
4. Write remaining migrations (003, 004, 005)
5. Validate all migration files using validators
6. Commit: "feat(task-050a): add initial migration SQL files"

### Phase 6: Integration Tests
1. Write and run SchemaValidationIntegrationTests
2. Write and run MigrationOrderIntegrationTests
3. Commit: "test(task-050a): add integration tests"

### Phase 7: Documentation
1. Create docs/database/conventions.md
2. Commit: "docs(task-050a): add database conventions documentation"

## Error Codes Reference

| Code | Description |
|------|-------------|
| ACODE-DB-LAY-001 | Invalid table name (wrong case or missing prefix) |
| ACODE-DB-LAY-002 | Invalid column name (wrong case or pattern) |
| ACODE-DB-LAY-003 | Missing rollback script (_down.sql) |
| ACODE-DB-LAY-004 | Invalid migration version number |
| ACODE-DB-LAY-005 | Foreign key dependency violation |
| ACODE-DB-LAY-006 | Data type mismatch (wrong type for column category) |
| ACODE-DB-LAY-007 | Invalid index name pattern |
| ACODE-DB-LAY-008 | Migration sequence gap detected |
| ACODE-DB-LAY-009 | Forbidden SQL pattern detected |
| ACODE-DB-LAY-010 | Checksum validation failed |

## Progress Tracker

- [ ] Phase 1: Domain Models
  - [ ] ValidationResult.cs
  - [ ] ColumnSchema.cs
  - [ ] TableSchema.cs
- [ ] Phase 2: Naming Convention Validator
- [ ] Phase 3: Data Type Validator
- [ ] Phase 4: Migration File Validator
- [ ] Phase 5: Migration SQL Files
  - [ ] 001_initial_schema.sql
  - [ ] 002_add_conversations.sql
  - [ ] 003_add_sessions.sql
  - [ ] 004_add_approvals.sql
  - [ ] 005_add_sync.sql
- [ ] Phase 6: Integration Tests
- [ ] Phase 7: Documentation

## Completion Criteria

Task 050a is COMPLETE when:
1. All domain models implemented with tests
2. All validators implemented with tests
3. All 5 migration files created (up + down scripts)
4. All unit tests passing (26+ tests)
5. All integration tests passing (12+ tests)
6. All E2E tests passing (3+ tests)
7. Documentation complete
8. Build GREEN (0 errors, 0 warnings)
9. All code audited per AUDIT-GUIDELINES.md
