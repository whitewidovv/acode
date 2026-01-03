# Task 026.a: SQLite Schema

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 026 (Queue Persistence)  

---

## Description

Task 026.a defines the SQLite schema for the task queue. Tables MUST store tasks, state history, and worker assignments. Indexes MUST optimize common queries.

The schema MUST support efficient queue operations. Dequeue MUST be fast even with many tasks. Filtering by status and priority MUST be indexed. History queries MUST be efficient.

Schema migrations MUST be versioned. Upgrades MUST be automatic and safe. Downgrades MUST NOT be supported (forward-only). Schema version MUST be tracked.

### Business Value

Proper schema design enables:
- Fast queue operations
- Efficient queries
- Data integrity
- Safe migrations
- Audit compliance

### Scope Boundaries

This task covers SQLite schema only. Queue logic is in Task 026. State transitions are in Task 026.b. Crash recovery is in Task 026.c.

### Integration Points

- Task 026: Uses schema
- Task 017: Similar patterns
- Task 018: Outbox reference

### Failure Modes

- Migration failure → Rollback transaction
- Schema mismatch → Require upgrade
- Index corruption → Rebuild

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| DDL | Data Definition Language |
| Index | Query optimization structure |
| Covering Index | Index with all query columns |
| Composite Key | Multi-column primary key |
| Foreign Key | Referential integrity constraint |
| Migration | Schema version change |
| Trigger | Automatic action on change |
| View | Virtual table from query |
| Constraint | Data validation rule |

---

## Out of Scope

- Stored procedures
- Full-text search
- Spatial indexes
- JSON columns
- Generated columns
- Virtual tables

---

## Functional Requirements

### FR-001 to FR-025: Tasks Table

- FR-001: `tasks` table MUST exist
- FR-002: `id` MUST be TEXT PRIMARY KEY
- FR-003: `title` MUST be TEXT NOT NULL
- FR-004: `description` MUST be TEXT NOT NULL
- FR-005: `status` MUST be TEXT NOT NULL
- FR-006: `priority` MUST be INTEGER NOT NULL DEFAULT 3
- FR-007: `dependencies` MUST be TEXT (JSON array)
- FR-008: `files` MUST be TEXT (JSON array)
- FR-009: `tags` MUST be TEXT (JSON array)
- FR-010: `metadata` MUST be TEXT (JSON object)
- FR-011: `timeout_seconds` MUST be INTEGER DEFAULT 3600
- FR-012: `retry_limit` MUST be INTEGER DEFAULT 3
- FR-013: `attempt_count` MUST be INTEGER DEFAULT 0
- FR-014: `worker_id` MUST be TEXT (nullable)
- FR-015: `parent_id` MUST be TEXT (nullable FK)
- FR-016: `created_at` MUST be TEXT NOT NULL (ISO8601)
- FR-017: `started_at` MUST be TEXT (nullable)
- FR-018: `completed_at` MUST be TEXT (nullable)
- FR-019: `last_error` MUST be TEXT (nullable)
- FR-020: `result` MUST be TEXT (nullable, JSON)
- FR-021: `spec_version` MUST be INTEGER DEFAULT 1
- FR-022: `updated_at` MUST be TEXT NOT NULL
- FR-023: Status CHECK constraint MUST exist
- FR-024: Priority CHECK constraint MUST exist
- FR-025: Parent FK MUST reference tasks(id)

### FR-026 to FR-040: History Table

- FR-026: `task_history` table MUST exist
- FR-027: `id` MUST be INTEGER PRIMARY KEY AUTOINCREMENT
- FR-028: `task_id` MUST be TEXT NOT NULL FK
- FR-029: `from_status` MUST be TEXT (nullable)
- FR-030: `to_status` MUST be TEXT NOT NULL
- FR-031: `actor` MUST be TEXT NOT NULL
- FR-032: `reason` MUST be TEXT (nullable)
- FR-033: `timestamp` MUST be TEXT NOT NULL (ISO8601)
- FR-034: FK MUST cascade on delete
- FR-035: Index on task_id MUST exist
- FR-036: Index on timestamp MUST exist
- FR-037: Actor values: worker, user, system
- FR-038: History MUST be append-only
- FR-039: No UPDATE on history MUST be enforced
- FR-040: No DELETE on history except cascade

### FR-041 to FR-055: Indexes

- FR-041: `idx_tasks_status_priority` composite index
- FR-042: `idx_tasks_status` single index
- FR-043: `idx_tasks_priority` single index
- FR-044: `idx_tasks_created_at` single index
- FR-045: `idx_tasks_worker_id` single index
- FR-046: `idx_tasks_parent_id` single index
- FR-047: `idx_history_task_id` single index
- FR-048: `idx_history_timestamp` single index
- FR-049: Dequeue query MUST use covering index
- FR-050: Status filter MUST use index
- FR-051: Priority sort MUST use index
- FR-052: Worker lookup MUST use index
- FR-053: EXPLAIN QUERY PLAN MUST show index use
- FR-054: Index stats MUST be available
- FR-055: ANALYZE MUST run periodically

### FR-056 to FR-070: Migrations

- FR-056: `schema_version` table MUST exist
- FR-057: Version MUST be single row
- FR-058: Current version MUST be queryable
- FR-059: Migration scripts MUST be numbered
- FR-060: Migration MUST run in transaction
- FR-061: Migration failure MUST rollback
- FR-062: Applied migrations MUST be logged
- FR-063: Missing migrations MUST auto-apply
- FR-064: Version mismatch MUST error
- FR-065: Schema hash MUST be stored
- FR-066: Hash mismatch MUST warn
- FR-067: Backup before migration MUST be optional
- FR-068: Migration dry-run MUST be supported
- FR-069: Migration timing MUST be logged
- FR-070: Post-migration validation MUST run

---

## Non-Functional Requirements

- NFR-001: Dequeue MUST use index
- NFR-002: Index size MUST be reasonable
- NFR-003: Migration MUST complete in <30s
- NFR-004: Schema MUST fit in memory
- NFR-005: No table scans for common queries
- NFR-006: History MUST be purgeable
- NFR-007: Purge MUST not block operations
- NFR-008: Schema MUST be self-documenting
- NFR-009: Comments MUST exist
- NFR-010: Constraints MUST be named

---

## User Manual Documentation

### Schema Diagram

```
┌─────────────────────────────────────────────────────────────┐
│ tasks                                                        │
├─────────────────────────────────────────────────────────────┤
│ id            TEXT PRIMARY KEY                               │
│ title         TEXT NOT NULL                                  │
│ description   TEXT NOT NULL                                  │
│ status        TEXT NOT NULL CHECK (...)                      │
│ priority      INTEGER NOT NULL DEFAULT 3 CHECK (1-5)         │
│ dependencies  TEXT (JSON)                                    │
│ files         TEXT (JSON)                                    │
│ tags          TEXT (JSON)                                    │
│ metadata      TEXT (JSON)                                    │
│ timeout_secs  INTEGER DEFAULT 3600                           │
│ retry_limit   INTEGER DEFAULT 3                              │
│ attempt_count INTEGER DEFAULT 0                              │
│ worker_id     TEXT                                           │
│ parent_id     TEXT → tasks(id)                               │
│ created_at    TEXT NOT NULL                                  │
│ started_at    TEXT                                           │
│ completed_at  TEXT                                           │
│ updated_at    TEXT NOT NULL                                  │
│ last_error    TEXT                                           │
│ result        TEXT (JSON)                                    │
│ spec_version  INTEGER DEFAULT 1                              │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ FK
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ task_history                                                 │
├─────────────────────────────────────────────────────────────┤
│ id           INTEGER PRIMARY KEY AUTOINCREMENT               │
│ task_id      TEXT NOT NULL → tasks(id) ON DELETE CASCADE     │
│ from_status  TEXT                                            │
│ to_status    TEXT NOT NULL                                   │
│ actor        TEXT NOT NULL                                   │
│ reason       TEXT                                            │
│ timestamp    TEXT NOT NULL                                   │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ schema_version                                               │
├─────────────────────────────────────────────────────────────┤
│ version      INTEGER NOT NULL                                │
│ applied_at   TEXT NOT NULL                                   │
│ hash         TEXT NOT NULL                                   │
└─────────────────────────────────────────────────────────────┘
```

### Query Optimization

**Dequeue (optimized):**
```sql
SELECT * FROM tasks
WHERE status = 'pending'
ORDER BY priority ASC, created_at ASC
LIMIT 1;
-- Uses: idx_tasks_status_priority
```

**List by status:**
```sql
SELECT * FROM tasks WHERE status = ?
-- Uses: idx_tasks_status
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Tasks table created
- [ ] AC-002: History table created
- [ ] AC-003: All columns defined
- [ ] AC-004: Constraints enforced
- [ ] AC-005: Indexes created
- [ ] AC-006: Foreign keys work
- [ ] AC-007: Migration versioned
- [ ] AC-008: Schema hash tracked
- [ ] AC-009: Query plans use indexes
- [ ] AC-010: Documentation complete

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Table creation
- [ ] UT-002: Constraint enforcement
- [ ] UT-003: Index usage
- [ ] UT-004: Migration application

### Integration Tests

- [ ] IT-001: Full schema creation
- [ ] IT-002: Data integrity
- [ ] IT-003: Query performance

---

## Implementation Prompt

### DDL Script

```sql
-- Schema version tracking
CREATE TABLE IF NOT EXISTS schema_version (
    version INTEGER NOT NULL,
    applied_at TEXT NOT NULL,
    hash TEXT NOT NULL
);

-- Tasks table
CREATE TABLE IF NOT EXISTS tasks (
    id TEXT PRIMARY KEY,
    title TEXT NOT NULL,
    description TEXT NOT NULL,
    status TEXT NOT NULL CHECK (status IN 
        ('pending', 'running', 'completed', 'failed', 'cancelled', 'blocked')),
    priority INTEGER NOT NULL DEFAULT 3 CHECK (priority BETWEEN 1 AND 5),
    dependencies TEXT DEFAULT '[]',
    files TEXT DEFAULT '[]',
    tags TEXT DEFAULT '[]',
    metadata TEXT DEFAULT '{}',
    timeout_seconds INTEGER DEFAULT 3600,
    retry_limit INTEGER DEFAULT 3,
    attempt_count INTEGER DEFAULT 0,
    worker_id TEXT,
    parent_id TEXT REFERENCES tasks(id),
    created_at TEXT NOT NULL,
    started_at TEXT,
    completed_at TEXT,
    updated_at TEXT NOT NULL,
    last_error TEXT,
    result TEXT,
    spec_version INTEGER DEFAULT 1
);

-- Task history table
CREATE TABLE IF NOT EXISTS task_history (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    task_id TEXT NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    from_status TEXT,
    to_status TEXT NOT NULL,
    actor TEXT NOT NULL,
    reason TEXT,
    timestamp TEXT NOT NULL
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_tasks_status_priority 
    ON tasks(status, priority, created_at);
CREATE INDEX IF NOT EXISTS idx_tasks_status ON tasks(status);
CREATE INDEX IF NOT EXISTS idx_tasks_worker_id ON tasks(worker_id);
CREATE INDEX IF NOT EXISTS idx_tasks_parent_id ON tasks(parent_id);
CREATE INDEX IF NOT EXISTS idx_history_task_id ON task_history(task_id);
CREATE INDEX IF NOT EXISTS idx_history_timestamp ON task_history(timestamp);
```

---

**End of Task 026.a Specification**