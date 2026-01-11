# Task 050: Workspace Database Foundation - Implementation Plan

**Status:** In Progress
**Created:** 2026-01-06
**Task Suite:** 050 (parent) + 050a, 050b, 050c, 050d, 050e (subtasks)
**Complexity:** 13 (parent) + subtasks = Very Large
**Total Spec Lines:** 27,277 lines

---

## Strategic Approach

Given the massive scope, I'll implement in strict phases with TDD, committing frequently.

### Phase 1: Core Interfaces & Abstractions (Application Layer)
- IConnectionFactory
- IDbConnection wrapper
- ITransaction interface
- IDbMigration
- IMigrationRunner
- **NO implementation yet - pure contracts**

### Phase 2: SQLite Implementation (Infrastructure Layer)
- SqliteConnectionFactory
- SqliteConnection wrapper
- SqliteTransaction
- SQLite migration support
- **SQLite ONLY first - prove the pattern works**

### Phase 3: Migration Framework
- Migration discovery
- sys_migrations table
- Migration execution
- Checksum validation
- Migration runner

### Phase 4: PostgreSQL Implementation
- PostgresConnectionFactory (similar to SQLite)
- Connection pooling
- SSL/TLS support

### Phase 5: Health Checks & Diagnostics
- Connection health checks
- Pool monitoring
- Circuit breaker

### Phase 6: Backup/Export
- Backup hooks
- Export commands

---

## Current Session Goals

Due to token limits, this session will focus on **Phase 1 & Phase 2**: Core interfaces and SQLite implementation.

### Phase 1 Goals
- ✅ Define IConnectionFactory
- ✅ Define IDbConnection
- ✅ Define ITransaction
- ✅ Define IMigrationRunner
- ✅ Tests for interface contracts

### Phase 2 Goals
- ✅ Implement SqliteConnectionFactory
- ✅ Implement SqliteConnection wrapper
- ✅ Implement SqliteTransaction
- ✅ Tests for SQLite implementation
- ✅ Integration test with real SQLite database

---

## Implementation Progress

### Completed

**Phase 1: Core Database Interfaces** ✅
- [x] DbProviderType enum (SQLite, PostgreSQL)
- [x] IConnectionFactory interface
- [x] IDbConnection interface (with Dapper-style query methods)
- [x] ITransaction interface
- [x] Interface contract tests (3 tests passing)
- [x] Commit: feat(task-050): implement core database interfaces (Phase 1)

**Phase 2: SQLite Provider** ✅
- [x] SqliteConnectionFactory (WAL mode, busy timeout, directory creation)
- [x] SqliteConnection wrapper (Dapper integration, async operations)
- [x] SqliteTransaction (auto-rollback on disposal)
- [x] Central package management (Dapper 2.1.35, Microsoft.Data.Sqlite 8.0.0)
- [x] Integration tests (9 tests passing)
- [x] Commit: feat(task-050): implement SQLite provider with Dapper integration (Phase 2)

**Phase 3: Migration Framework** 🔄 (Partially Complete)
- [x] Migration domain models (MigrationFile, AppliedMigration, MigrationSource, MigrationStatus)
- [x] IMigrationRepository interface
- [x] SqliteMigrationRepository implementation (__migrations table CRUD)
- [x] Migration repository tests (11 tests passing)
- [x] Commit: feat(task-050): implement migration repository and __migrations table (Phase 3)
- [ ] Checksum utility (SHA-256 for migration integrity)
- [ ] Migration discovery (embedded + file-based)
- [ ] Migration execution engine
- [ ] Migration locking mechanism
- [ ] CLI commands for migration operations

### In Progress
- [ ] Phase 3: Migration framework (repository complete, discovery/execution pending)

### Pending (Future Sessions)
- [ ] Phase 3: Complete migration discovery and execution
- [ ] Phase 4: PostgreSQL implementation
- [ ] Phase 5: Health checks & diagnostics
- [ ] Phase 6: Backup/export hooks
- [ ] Full audit per AUDIT-GUIDELINES.md
- [ ] PR creation

---

## File Structure

```
src/Acode.Application/Database/
├── IConnectionFactory.cs
├── IDbConnection.cs
├── ITransaction.cs
├── IMigrationRunner.cs
└── DbProviderType.cs

src/Acode.Infrastructure/Database/
├── Sqlite/
│   ├── SqliteConnectionFactory.cs
│   ├── SqliteConnection.cs
│   └── SqliteTransaction.cs
└── Migrations/
    ├── MigrationRunner.cs
    └── Migration.cs

tests/Acode.Infrastructure.Tests/Database/
├── Sqlite/
│   ├── SqliteConnectionFactoryTests.cs
│   ├── SqliteConnectionTests.cs
│   └── SqliteTransactionTests.cs
└── Migrations/
    └── MigrationRunnerTests.cs
```

---

## Deferred to Future Sessions

- PostgreSQL implementation (large, separate concern)
- Health checks (depends on working connections)
- Backup/export (depends on working connections)
- Full integration with Task 011, 049
- Performance tuning
- Complete audit

This session establishes the foundation. Future sessions build on it.
