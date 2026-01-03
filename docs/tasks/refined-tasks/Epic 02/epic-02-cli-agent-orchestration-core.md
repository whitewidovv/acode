# EPIC 2 — CLI + Agent Orchestration Core

**Priority:** P0 – Critical Path  
**Phase:** Foundation  
**Dependencies:** Epic 00 (Product Definition), Epic 01 (Model Runtime)  

---

## Epic Overview

Epic 2 establishes the command-line interface and agent orchestration layer that users interact with directly. This epic transforms the model runtime and tool infrastructure from Epic 01 into a cohesive agentic system that can receive user requests, plan tasks, execute changes, and verify results through structured multi-stage workflows.

The CLI Command Framework provides the entry point for all user interactions. It implements a consistent command structure with help documentation, output formats, and behavioral modes (interactive, non-interactive, streaming). The CLI is the user's window into Acode's operation—every feature surfaces through commands.

The Run Session State Machine tracks execution state across multi-step workflows. Unlike simple request-response interactions, agentic coding involves extended sessions with many steps. The state machine provides persistence (survive restarts), resumability (continue after interruption), and auditability (track what happened and why).

The Multi-Stage Agent Loop structures how the agent approaches tasks. Rather than jumping straight to implementation, the agent follows a disciplined process: Plan (decompose the task), Execute (make changes), Verify (check correctness), Review (ensure quality). This structure improves output quality and provides natural checkpoints for human intervention.

Human Approval Gates inject human judgment at critical points. Not all model actions should proceed automatically—file deletions, security-sensitive changes, and ambiguous situations benefit from human confirmation. The approval system provides configurable gates with clear prompts, persisted decisions, and bypass options for trusted operations.

Epic 2 is the central nervous system of Acode. It coordinates all other components—model providers from Epic 01, tools from Epic 03, sandbox execution from Epic 04, and beyond. The orchestration layer ensures these components work together coherently while maintaining safety and predictability.

The epic includes database infrastructure (Tasks 049-050) for conversation history and workspace state. These foundations enable features like multi-chat management, session persistence across restarts, and synchronization with remote databases when configured.

---

## Outcomes

1. Users can invoke Acode through a well-designed CLI
2. Commands follow consistent patterns for discoverability
3. Help documentation is comprehensive and accessible
4. Output can be human-readable or machine-parseable (JSONL)
5. Non-interactive mode enables automation and CI/CD integration
6. Sessions survive process restarts via persistence
7. Users can resume interrupted sessions
8. Run history provides full auditability
9. Agent follows structured multi-stage workflow
10. Each stage (Plan, Execute, Verify, Review) is configurable
11. Human approval gates provide safety checkpoints
12. Users can configure which operations require approval
13. Approvals are persisted for audit trail
14. --yes flag enables controlled bypass for trusted operations
15. Conversation history is stored and searchable
16. Multi-chat support enables parallel work streams
17. SQLite provides local storage with PostgreSQL sync option
18. Database migrations are versioned and reliable
19. Data can be exported and backed up
20. Privacy controls enable redaction of sensitive content

---

## Non-Goals

1. GUI or web interface - CLI only in this epic
2. Real-time collaboration - Single-user focus
3. Cloud-hosted agent - Local execution only
4. Automatic conflict resolution - Human-guided
5. Plugin system for stages - Fixed stage structure
6. Parallel task execution - Sequential for MVP
7. Natural language commands - Structured CLI
8. Voice interaction - Text only
9. Remote execution - Local sandbox only
10. Team-shared history - Personal workspace
11. AI-powered suggestions - Explicit user requests
12. Autonomous operation - Human-in-the-loop
13. Mobile clients - Desktop CLI only
14. Integration APIs - CLI is the interface
15. Multi-workspace - Single workspace per session

---

## Architecture & Integration Points

### Command Structure

```
acode [global-options] <command> [command-options] [arguments]

Global Options:
  --config <path>     Configuration file
  --verbose           Verbose output
  --quiet             Minimal output
  --json              JSONL output mode
  --yes               Skip approval prompts
  --dry-run           Preview without executing

Commands:
  run                 Start agent run
  resume              Resume interrupted run
  chat                Multi-chat management
  models              Model configuration
  prompts             Prompt pack management
  db                  Database operations
  config              Configuration management
```

### Core Interfaces

```csharp
// CLI Layer
ICommandRouter - Routes commands to handlers
IOutputFormatter - Formats output for different modes
IApprovalGate - Handles human approval prompts

// Orchestration Layer
IRunSessionManager - Manages run lifecycle
IStateMachine - Tracks run state transitions
IStagePipeline - Executes multi-stage workflow

// Persistence Layer
ISessionRepository - Persists session data
IConversationRepository - Stores chat history
IMigrationRunner - Manages database migrations
```

### State Machine States

```
CREATED → PLANNING → EXECUTING → VERIFYING → REVIEWING → COMPLETED
                ↓          ↓           ↓           ↓
              PAUSED ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
                ↓
            RESUMED → (continues from pause point)
                ↓
            FAILED → TERMINATED
```

### Event Stream (JSONL)

```json
{"type": "run_started", "run_id": "...", "timestamp": "..."}
{"type": "stage_entered", "stage": "planning", "timestamp": "..."}
{"type": "tool_call", "tool": "read_file", "args": {...}}
{"type": "tool_result", "tool": "read_file", "result": {...}}
{"type": "approval_requested", "action": "delete_file", "path": "..."}
{"type": "approval_granted", "action": "delete_file", "by": "user"}
{"type": "stage_completed", "stage": "planning", "timestamp": "..."}
{"type": "run_completed", "run_id": "...", "timestamp": "..."}
```

### Database Schema Overview

```
Sessions
├── runs
├── tasks
├── steps
├── tool_calls
└── artifacts

Conversations
├── chats
├── messages
└── attachments

Configuration
├── migrations
├── settings
└── approvals
```

---

## Operational Considerations

### Operating Modes (Task 001)

- All CLI commands respect local-only/air-gapped constraints
- Database sync features are disabled in air-gapped mode
- Commands fail gracefully when constraints are violated

### Safety

- Approval gates are enabled by default
- --yes flag requires explicit user action
- Dangerous operations (delete, overwrite) always log
- Session state is checkpointed before risky operations

### Audit

- All commands are logged with timestamps
- Run history includes full operation traces
- Approvals are persisted with decision rationale
- Export functions support audit requirements

### Performance

- CLI startup under 500ms
- State machine transitions under 10ms
- Database operations cached appropriately
- Large outputs streamed, not buffered

---

## Acceptance Criteria / Definition of Done

### CLI Framework

- [ ] Command router implemented
- [ ] Help output for all commands
- [ ] Global options work
- [ ] Exit codes are consistent
- [ ] Verbose mode provides detail
- [ ] Quiet mode minimizes output
- [ ] JSONL mode outputs valid JSON
- [ ] Non-interactive mode works
- [ ] Config file loading works
- [ ] Environment overrides work

### State Machine

- [ ] All states defined
- [ ] Transitions are valid
- [ ] Invalid transitions rejected
- [ ] State persisted to database
- [ ] Pause/resume works
- [ ] Failure handling works
- [ ] Timeout handling works
- [ ] Session cleanup works

### Run Entities

- [ ] Session entity defined
- [ ] Task entity defined
- [ ] Step entity defined
- [ ] ToolCall entity defined
- [ ] Artifact entity defined
- [ ] Relationships correct
- [ ] Persistence works
- [ ] Query works

### Agent Loop

- [ ] Planner stage implemented
- [ ] Executor stage implemented
- [ ] Verifier stage implemented
- [ ] Reviewer stage implemented
- [ ] Stage transitions work
- [ ] Stage configuration works
- [ ] Stage skipping works
- [ ] Error handling per stage

### Approval Gates

- [ ] Gate rules configurable
- [ ] Prompts are clear
- [ ] Approvals persisted
- [ ] Rejections handled
- [ ] --yes flag works
- [ ] --yes scoping works
- [ ] Timeout handling works

### Database

- [ ] SQLite storage works
- [ ] Migrations run correctly
- [ ] Schema is versioned
- [ ] Queries are efficient
- [ ] Backup works
- [ ] Export works

### Conversation History

- [ ] Chat CRUD works
- [ ] Message storage works
- [ ] Search works
- [ ] Multi-chat works
- [ ] Retention works
- [ ] Privacy controls work

---

## Risks & Mitigations

1. **Risk:** State machine complexity causes bugs
   **Mitigation:** Comprehensive state transition tests, formal state diagram

2. **Risk:** Database corruption loses session data
   **Mitigation:** WAL mode, checkpointing, backup on critical operations

3. **Risk:** Approval prompts interrupt workflow excessively
   **Mitigation:** Configurable gates, --yes scoping, learned preferences

4. **Risk:** JSONL output format insufficient for consumers
   **Mitigation:** Versioned schema, extensible event types

5. **Risk:** Resume behavior has edge cases
   **Mitigation:** Explicit invariants, extensive scenario testing

6. **Risk:** Multi-stage loop adds latency
   **Mitigation:** Stage skipping for simple tasks, parallel where safe

7. **Risk:** CLI startup time impacts UX
   **Mitigation:** Lazy loading, minimal init path

8. **Risk:** Non-interactive mode fails silently
   **Mitigation:** Exit codes, error streams, health checks

9. **Risk:** Database schema evolution breaks compatibility
   **Mitigation:** Forward-compatible migrations, version checks

10. **Risk:** Conversation history grows unbounded
    **Mitigation:** Retention policies, archival, pagination

11. **Risk:** PostgreSQL sync fails in flaky networks
    **Mitigation:** Outbox pattern, retries, idempotency

12. **Risk:** Privacy-sensitive data in logs
    **Mitigation:** Redaction controls, scrubbing on export

---

## Milestone Plan

### Milestone 1: CLI Foundation (Tasks 010.a-010.c)
- Command routing and parsing
- Help system
- Output formatting
- Non-interactive mode

### Milestone 2: Session Management (Tasks 011.a-011.c)
- Run entities and relationships
- SQLite persistence
- State machine implementation
- Resume behavior

### Milestone 3: Agent Loop (Tasks 012.a-012.d)
- Planner stage
- Executor stage
- Verifier stage
- Reviewer stage

### Milestone 4: Human Approval (Tasks 013.a-013.c)
- Approval gate framework
- Decision persistence
- --yes flag scoping

### Milestone 5: Database Foundation (Tasks 050.a-050.e)
- Database layout and migrations
- Connection management
- Health checks and diagnostics
- Backup and export

### Milestone 6: Conversation Management (Tasks 049.a-049.f)
- Conversation data model
- CRUD operations
- Multi-chat support
- Search and indexing
- Retention and privacy
- Sync engine (optional)

---

## Definition of Epic Complete

- [ ] All tasks 010-013 implemented and tested
- [ ] All tasks 049-050 implemented and tested
- [ ] CLI commands documented in user manual
- [ ] State machine has full test coverage
- [ ] Database migrations are versioned
- [ ] Approval gates are configurable
- [ ] JSONL output schema is documented
- [ ] Non-interactive mode works in CI
- [ ] Resume behavior is reliable
- [ ] Multi-chat management works
- [ ] Search is performant
- [ ] Retention policies enforced
- [ ] Backup/restore tested
- [ ] Privacy controls validated
- [ ] Integration tests pass
- [ ] Performance benchmarks met
- [ ] Documentation complete
- [ ] Code review completed
- [ ] No critical bugs open
- [ ] Ready for Epic 03 dependencies

---

**END OF EPIC 2**