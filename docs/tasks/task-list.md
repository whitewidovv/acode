# Full task list (names only), grouped by epic
### Format: Epic → Task → Subtasks

# DEFINED 

## EPIC 0 — Product Definition, Constraints, Repo Contracts

### Task 000: Project Bootstrap & Solution Structure (Agentic Coding Bot)

#### Create repo + .NET solution + baseline project layout (Domain/Application/Infrastructure/Cli/Tests)

#### Add baseline docs (README, REPO_STRUCTURE, CONFIG, OPERATING_MODES)

#### Add baseline tooling + formatting + test scaffolding

# TO DEFINE


### Task 001: Define Operating Modes & Hard Constraints

#### Define mode matrix (local-only / burst / airgapped)

#### Define “no external LLM API” validation rules

#### Write constraints doc + enforcement checklist

### Task 002: Define Repo Contract File (.agent/config.yml)

#### Define schema + examples

#### Implement parser + validator requirements

#### Define command groups (setup/build/test/lint/format/start)

### Task 003: Threat Model & Default Safety Posture

#### Enumerate risk categories + mitigations

#### Define default denylist + protected paths

#### Define audit baseline requirements

## EPIC 1 — Model Runtime, Inference, Tool-Calling Contract

### Task 004: Model Provider Interface

#### Define message/tool-call types

#### Define response format + usage reporting

#### Provider registry + config selection

### Task 005: Ollama Provider Adapter

#### Implement request/response + streaming handling

#### Tool-call parsing + retry-on-invalid-json

#### Setup docs + smoke test script

### Task 006: vLLM Provider Adapter

#### Implement serving assumptions + client adapter

#### Structured outputs enforcement integration

#### Load/health-check endpoints + error handling

### Task 007: Tool Schema Registry + Strict Validation

#### JSON Schema definitions for all core tools

#### Validator errors → model retry contract

#### Truncation + artifact attachment rules

### Task 008: Prompt Pack System

#### Prompt pack file layout + hashing/versioning

#### Loader/validator + selection via config

#### Starter packs (dotnet/react, strict minimal diff)

### Task 009: Model Routing Policy

#### Planner/coder/reviewer roles

#### Routing heuristics + overrides

#### Fallback escalation rules

## EPIC 2 — CLI + Agent Orchestration Core

### Task 010: CLI Command Framework

#### Command routing + help output standard

#### JSONL event stream mode

#### Non-interactive mode behaviors

### Task 050: Workspace Database Foundation (SQLite + Migrations + Postgres Connector)

#### Define workspace DB layout + migration strategy (SQLite local, Postgres remote)

#### Implement DB access layer + connection management (SQLite + Postgres)

#### Implement migration runner CLI + startup bootstrapping

#### Implement health checks + diagnostics (db status, sync status, storage stats)

#### Implement backup/export hooks for workspace DB (safe, redacted)

### Task 049: Conversation History & Multi-Chat Management (CRU(sD)) — Offline-first (SQLite cache + Postgres)

#### Define conversation data model + storage provider abstraction (SQLite cache + Postgres source-of-truth)

#### Implement CRU(sD) APIs + CLI commands (chat create/list/open/rename/delete/restore/purge)

#### Implement multi-chat concurrency model + run/worktree binding

#### Implement indexing + fast search over chats/runs/messages

#### Define retention, export, privacy + redaction controls (local cache + remote DB)

#### Implement SQLite→Postgres sync engine (outbox, batching, retries, idempotency, conflict policy)

### Task 011: Run Session State Machine + Persistence

#### Run entities (session/task/step/tool call/artifacts)

#### Persistence model (SQLite recommended)

#### Resume behavior + invariants

### Task 012: Multi-Stage Agent Loop

#### Planner stage

#### Executor stage

#### Verifier stage

#### Reviewer stage

### Task 013: Human Approval Gates

#### Gate rules + prompts

#### Persist approvals + decisions

#### --yes scoping rules

## EPIC 3 — Repo Intelligence (Indexing, Retrieval, Context Packing)

### Task 014: RepoFS Abstraction

#### Local FS implementation

#### Docker-mounted FS implementation

#### Atomic patch application behavior

### Task 015: Indexing v1 (search + ignores)

#### Ignore rules + gitignore support

#### Search tool integration

#### Index update strategy

### Task 016: Context Packer

#### Chunking rules

#### Ranking rules

#### Token budgeting + dedupe

### Task 017: Symbol Index v2

#### C# symbol extraction

#### TS/JS symbol extraction

#### Dependency mapping + retrieval APIs

## EPIC 4 — Execution & Sandboxing

### Task 018: Structured Command Runner

#### stdout/stderr capture + exit code + timeout

#### Working dir/env enforcement

#### Artifact logging + truncation

### Task 019: Language Runners (.NET + JS)

#### Detect solution/package layouts

#### Implement run_tests wrapper

#### Integrate repo contract commands

### Task 020: Docker Sandbox Mode

#### Per-task container strategy

#### Cache volumes (nuget/npm)

#### Policy enforcement inside sandbox

### Task 021: Artifact Collection + Run Inspection

#### Artifact directory standards

#### run show/logs/diff CLI commands

#### Export bundle format

## EPIC 5 — Git Automation + Worktrees

### Task 022: Git Tool Layer

#### status/diff/log

#### branch create/checkout

#### add/commit/push

### Task 023: Worktree-per-Task

#### worktree create/remove/list

#### persist worktree ↔ task mapping

#### cleanup policy rules

### Task 024: Safe Commit/Push Workflow

#### pre-commit verification pipeline

#### commit message rules

#### push gating + failure handling

## EPIC 6 — Task Queue + Parallel Worker System

### Task 025: Task Spec Format

#### YAML/JSON schema

#### CLI add/list/show/retry/cancel

#### human-readable errors

### Task 026: Queue Persistence + Transition Invariants

#### SQLite schema

#### state transitions + logging

#### crash recovery handling

### Task 027: Worker Pool + Parallel Execution

#### local worker pool

#### docker worker pool

#### log multiplexing/dashboard

### Task 028: Parallel Safety + Merge Coordinator

#### conflict heuristics

#### dependency graph hints

#### integration merge plan + tests

## EPIC 7 — Cloud Burst Compute (Provider-Swappable)

### Task 029: ComputeTarget Interface

#### prepare workspace

#### exec commands

#### upload/download artifacts

#### teardown

### Task 030: SSH Target

#### sync repo/worktree

#### remote exec + caching

#### artifact pullback

### Task 031: AWS EC2 Target

#### IaC templates

#### bootstrap scripts

#### spawn-per-task + pooled modes

### Task 032: Inference/Execution Placement Strategies

#### local inference + local exec

#### cloud inference + cloud exec

#### local inference + cloud exec

### Task 033: Cloud Burst Heuristics

#### thresholds + metrics

#### auto/always/never modes

#### worker scaling controls

## EPIC 8 — CI/CD Authoring + Deployment Hooks

### Task 034: CI Template Generator

#### GitHub Actions templates (dotnet/node)

#### pinned versions + minimal permissions

#### caching setup

### Task 035: CI Maintenance Mode

#### workflow change proposals + diffs

#### approval gates

#### CI-specific task runner support

### Task 036: Deployment Hook Tool

#### deploy tool schema

#### disabled by default

#### non-bypassable approvals (default)

## EPIC 9 — Safety, Policy Engine, Secrets Hygiene, Audit

### Task 037: Policy-as-Config Engine

#### global policy config

#### repo overrides

#### per-task overrides

### Task 038: Secrets Redaction + Diff Scanning

#### redact tool output before model sees it

#### block commit/push on secret detection

#### configurable patterns + corpus tests

### Task 039: Audit Trail + Export

#### record tool calls/commands/diffs/models/prompts

#### export bundle

#### verify export contains no raw secrets

## EPIC 10 — Reliability, Resumability, Deterministic Runs

### Task 040: Crash-Safe Event Log

#### append-only event log

#### resume rules

#### ordering guarantees

### Task 041: Retry Policy Framework

#### categorize failures

#### capped retries

#### needs-human transition rules

### Task 042: Reproducibility Knobs

#### persist prompts/settings (redacted)

#### deterministic mode switches

#### replay tooling

## EPIC 11 — Performance + Scaling

### Task 043: Output Summarization Pipeline

#### summarize failures

#### attach full logs

#### size limits

### Task 044: Retrieval/Index Caching

#### cache keys include commit hash

#### stats and clear commands

#### hit/miss telemetry

### Task 045: Model Performance Harness

#### microbench metrics

#### tool-call correctness rate

#### report comparisons

## EPIC 12 — Evaluation Suite + Regression Gates

### Task 046: Benchmark Task Suite

#### store tasks as specs

#### runner CLI

#### JSON results

### Task 047: Scoring + Promotion Gates

#### pass/fail + runtime + iterations

#### thresholds + gating rules

#### diffable historical reports

### Task 048: Golden Baseline Maintenance

#### baseline runs recorded

#### change log for prompt/model upgrades

#### regression triage workflow