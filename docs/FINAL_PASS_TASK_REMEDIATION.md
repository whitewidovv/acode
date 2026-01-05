# Final Pass Task Remediation - Comprehensive Quality Expansion

## Instructions

**Before beginning ANY expansion:**

1. **Re-read `CLAUDE.md`** - Refresh the full context of quality requirements
2. **Re-read `PROMPT_TO_EXPAND_TASK.md`** - Refresh the expansion methodology  
3. **Re-read `e-commerce golden standard task sample.md`** - This is the quality bar (3,699 lines)

**Expansion Process:**

1. Read the current task file completely
2. Evaluate each section against the golden standard
3. Expand section-by-section, saving after each section to avoid context loss
4. A section is NOT complete just because it exists - it must be semantically complete
5. The test: Could an inept junior developer implement this by following the instructions alone, with no questions asked and no Googling required?

**Quality Bar (Non-Negotiable):**

- **Description**: 300+ lines with business value, ROI justification, technical architecture decisions, integration points, constraints - all with specific numbers
- **Use Cases**: 3+ scenarios, each 10-15 lines minimum with named personas, before/after comparisons, concrete metrics
- **User Manual**: 200-400 lines with step-by-step instructions, ASCII mockups, configuration tables, best practices, troubleshooting, FAQ
- **Assumptions**: 15-20 items covering technical, operational, and integration assumptions
- **Security Considerations**: Threat analysis, mitigation strategies, audit requirements specific to this feature
- **Acceptance Criteria**: 50-80+ testable items with specific benchmarks (exact ms, exact percentages, exact counts)
- **Best Practices**: 12+ items organized by category
- **Troubleshooting**: 3+ common issues with Symptoms/Causes/Solutions format
- **Testing Requirements**: Complete test names for Unit (5-8+), Integration (3-5+), E2E (3-5+), Performance (3-4+), Regression (2-3+)
- **User Verification**: 8-10 manual testing scenarios with step-by-step instructions
- **Implementation Prompt**: 12+ implementation steps with complete code examples (not snippets), 400-600 lines

**DO NOT check the box unless:**

- [ ] All sections are semantically complete (not just present)
- [ ] Line count is 1,200+ for subtasks, 1,500+ for parent tasks (this is a FLOOR, not a target)
- [ ] No abbreviations, placeholders, "see above", "etc.", or "..." anywhere
- [ ] A junior developer could implement from this document alone
- [ ] Matches quality depth of the e-commerce golden standard sample

---

## Task Checklist

Work through this list sequentially. Only mark complete when all quality requirements are met.

- ✅ task-007-tool-schema-registry-strict-validation ✅ **COMPLETE (4,355 lines)** - Description 300+ lines, Assumptions 20 items, Security Considerations with threat model and 5 attack vectors, Best Practices 18 items, Troubleshooting 5 issues, Testing Requirements with complete test code, User Verification 10 scenarios with step-by-step commands, Implementation Prompt 14 steps with complete code
- ✅ task-007a-json-schema-definitions-for-all-core-tools ✅ **COMPLETE (3,524 lines)** - Description 300+ lines with business value ($1,460 savings), ROI metrics, 5 tool categories, 18 tools, Use Cases 3 scenarios with DevBot/Jordan/Alex personas, Assumptions 20 items, Security Considerations 5 threats with mitigations, Best Practices 12 items, Troubleshooting 5 issues, Testing Requirements with complete C# test code, User Verification 10 scenarios, Implementation Prompt 600+ lines with complete code for all 18 tool schemas
- ✅ task-007b-validator-errors-model-retry-contract ✅ **COMPLETE (4,196 lines)** - Description 300+ lines with business value metrics and ROI, Use Cases 4 scenarios with DevBot/Jordan/Alex personas, User Manual 800+ lines with complete config schema and 15 error codes, Assumptions 20 items, Security Considerations with 5 threats and 6 sanitization rules, Best Practices 17 items, Troubleshooting 5 issues, Acceptance Criteria 107 items, Testing Requirements 1,000+ lines with complete C# test code, User Verification 10 scenarios, Implementation Prompt 1,500+ lines with complete class implementations
- ✅ task-007c-truncation-artifact-attachment-rules ✅ **COMPLETE (1,768 lines)** - Expanded Description, Use Cases, User Manual, Assumptions, Security Considerations, Best Practices, Troubleshooting, Acceptance Criteria, Testing Requirements, User Verification, Implementation Prompt 
- ✅ task-007d-tool-call-parsing-retry-on-invalid-json ✅ **COMPLETE (3,710 lines)** - Description 300+ lines with ROI ($22,000/developer/year savings), 5-component architecture, Use Cases 3 scenarios, Assumptions 20 items, Security Considerations 5 threats with mitigations, Best Practices 18 items, Troubleshooting 5 issues, User Manual 400+ lines with ASCII diagrams, Testing Requirements with complete C# test code, User Verification 10 scenarios, Implementation Prompt 600+ lines with complete code for all components 
- ✅ task-007e-structured-outputs-enforcement-integration ✅ **COMPLETE (3,057 lines)** - Description 300+ lines with business value ($555/month savings), 3 Use Cases, Glossary 18 terms, 71 FRs, 23 NFRs, Assumptions 20 items, Security Considerations 5 threats, Best Practices 18 items, Troubleshooting 5 issues, User Manual with modes/monitoring/error codes, Acceptance Criteria 73 items, Testing Requirements with complete C# test code, User Verification 10 scenarios, Implementation Prompt 600+ lines with complete code for all components 
- ✅ task-008-prompt-pack-system ✅ **COMPLETE (3,830 lines)** - Description 400+ lines with ROI ($275k/year for 10 devs), architecture diagrams, prompt composition flow, template variable system, Use Cases 3 scenarios, Assumptions 20 items, Security Considerations 6 threats with mitigations, Testing Requirements 1,100+ lines with complete C# test code (34 tests), Best Practices 21 items, Troubleshooting 5 issues, User Verification 10 scenarios, Implementation Prompt 1,200+ lines with complete classes 
- ✅ task-008a-prompt-pack-file-layout-hashing-versioning ✅ **COMPLETE (3,399 lines)** - Description 300+ lines with ROI ($6,240/developer/year savings), architecture diagram, directory structure, manifest schema, SHA-256 hashing algorithm, SemVer 2.0 versioning, pack sources/precedence, cross-platform path handling, 10 error codes, Use Cases 3 scenarios with DevBot/Jordan/Alex personas, Assumptions 20 items, Security Considerations 5 threats with mitigations and code, Best Practices 18 items, Troubleshooting 5 detailed issues, Testing Requirements with complete C# test code for 6 test classes including benchmarks, User Verification 10 detailed scenarios with step-by-step commands and expected outputs, Implementation Prompt 600+ lines with complete code for all domain models and infrastructure services 
- ✅ task-008b-loader-validator-selection-via-config ✅ **COMPLETE (2,968 lines)** - Description 300+ lines with ROI ($24k/year savings), architecture diagrams, Use Cases 3 scenarios, Assumptions 20 items, Security Considerations 7 threats with mitigations, Testing Requirements 1,100+ lines with complete C# test code (39 tests total), Best Practices 18 items, Troubleshooting 5 issues, User Verification 10 scenarios, Implementation Prompt 600+ lines with complete interfaces and classes 
- ✅ task-008c-starter-packs-dotnet-react-strict-minimal-diff ✅ **COMPLETE (3,299 lines)** - Description 300+ lines with ROI calculations ($17K-$18K per developer per year), business value metrics, technical architecture (embedded resources, layered composition, template variables), integration points, constraints, trade-offs; Use Cases 3 scenarios with DevBot/Jordan/Alex personas showing 96% diff reduction, 42 hours saved, concrete metrics; Glossary 15 terms; Out of Scope 10 items; FRs 83 requirements covering all 3 packs; NFRs 20 requirements for quality/size/compatibility/security; User Manual 200+ lines with pack comparisons, switching methods, extending packs, troubleshooting; Assumptions 20 items (technical, operational, integration, content); Security Considerations 5 threats with complete C# mitigation code (prompt injection sanitization, malicious override detection, secret scanning, audit logging, DoS prevention); Best Practices 18 items organized by category; Troubleshooting 5 detailed issues with Symptoms/Causes/Solutions; Acceptance Criteria 48 items; Testing Requirements 800+ lines with complete C# test code (StarterPackTests, PromptContentTests, StarterPackLoadingTests, StarterPackE2ETests, PackLoadingBenchmarks with full implementations); User Verification 10 detailed scenarios with complete bash/PowerShell commands and expected outputs; Implementation Prompt 900+ lines with complete prompt content for all 3 packs (full system.md, planner.md, coder.md, reviewer.md, csharp.md, aspnetcore.md, typescript.md, react.md with 16-step checklist) 
- ✅ task-009-model-routing-policy ✅ **COMPLETE (3,212 lines)** - Description 400+ lines with ROI ($10,000 annual infrastructure savings), multi-model routing strategy (Llama 3.3 70B for planning, Qwen 2.5 Coder 32B for code, DeepSeek-R1 14B for review), Use Cases 3 scenarios with DevBot/Jordan/Alex personas, Assumptions 20 items, Security Considerations 6 threats (model confusion, cost amplification, capability spoofing, routing bypass, model probing, audit evasion) with complete mitigations, Best Practices 24 items, Troubleshooting 5 issues, Testing Requirements 1,500+ lines with complete C# test code (23 complete tests with 30-50 lines each: ModelRoutingPolicyTests with 8 tests for route resolution/capability/fallback/cost/latency, RouterConfigurationTests with 5 tests for validation/defaults/override/cache, RoutingHeuristicsTests with 5 tests for task-based/context-window/load-balancing/cost-optimization heuristics, RoutingIntegrationTests with 3 tests for end-to-end routing/fallback/load scenarios, RoutingBenchmarks with 2 performance tests for routing overhead <1ms and cache hit rate >95%), User Verification 10 scenarios with step-by-step commands, Implementation Prompt 700+ lines with complete code for ModelRoutingPolicy/RoutingHeuristics/RoutingConfiguration/ModelCapability/RoutingDecision/RoutingMetrics classes 
- ⏳ task-009a-planner-coder-reviewer-roles ⏳ **IN PROGRESS** - Reading instructions and beginning expansion
- [ ] task-009b-routing-heuristics-overrides -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-009c-fallback-escalation-rules -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-010-cli-command-framework -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-010a-command-routing-help-output-standard -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-010b-jsonl-event-stream-mode -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-010c-non-interactive-mode-behaviors -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-011-run-session-state-machine-persistence -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-011a-run-entities-session-task-step-tool-call-artifacts -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-011b-persistence-model-sqlite-postgres -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-011c-resume-behavior-invariants -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-012-multi-stage-agent-loop -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-012a-planner-stage -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-012b-executor-stage -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-012c-verifier-stage -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-012d-reviewer-stage -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-013-human-approval-gates -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-013a-gate-rules-prompts -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-013b-persist-approvals-decisions -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-013c-yes-scoping-rules -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-049-conversation-history-multi-chat-management -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-049a-conversation-data-model-storage-provider -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-049b-crusd-apis-cli-commands -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-049c-multi-chat-concurrency-worktree-binding -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-049d-indexing-fast-search -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-049e-retention-export-privacy-redaction -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-049f-sqlite-postgres-sync-engine -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-050-workspace-database-foundation -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-050a-workspace-db-layout-migration-strategy -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-050b-db-access-layer-connection-management -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-050c-migration-runner-startup-bootstrapping -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-050d-health-checks-diagnostics -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-050e-backup-export-hooks -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-014-repofs-abstraction -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-014a-local-fs-implementation -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-014b-docker-mounted-fs-implementation -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-014c-atomic-patch-application-behavior -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-015-indexing-v1-search-ignores -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-015a-ignore-rules-gitignore-support -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-015b-search-tool-integration -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-015c-index-update-strategy -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-016-context-packer -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-016a-chunking-rules -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-016b-ranking-rules -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-016c-token-budgeting-dedupe -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-017-symbol-index-v2 -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-017a-c-symbol-extraction -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-017b-tsjs-symbol-extraction -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-017c-dependency-mapping-retrieval-apis -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-018-structured-command-runner -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-018a-stdout-stderr-capture-exit-code-timeout -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-018b-working-dir-env-enforcement -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-018c-artifact-logging-truncation -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-019-language-runners-net-js -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-019a-detect-solution-package-layouts -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-019b-implement-runtests-wrapper -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-019c-integrate-repo-contract-commands -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-020-docker-sandbox-mode -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-020a-per-task-container-strategy -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-020b-cache-volumes-nuget-npm -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-020c-policy-enforcement-inside-sandbox -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-021-artifact-collection-run-inspection -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-021a-artifact-directory-standards -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-021b-run-showlogsdiff-cli-commands -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-021c-export-bundle-format -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-022-git-tool-layer -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-022a-status-diff-log -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-022b-branch-create-checkout -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-022c-add-commit-push -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-023-worktree-per-task -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-023a-worktree-create-remove-list -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-023b-persist-worktree-task-mapping -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-023c-cleanup-policy-rules -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-024-safe-commit-push-workflow -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-024a-pre-commit-verification-pipeline -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-024b-commit-message-rules -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-024c-push-gating-failure-handling -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-025-task-spec-format -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-025a-yaml-json-schema -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-025b-cli-add-list-show-retry-cancel -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-025c-human-readable-errors -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-026-queue-persistence-transition-invariants -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-026a-sqlite-schema -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-026b-state-transitions-logging -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-026c-crash-recovery-handling -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-027-worker-pool-parallel-execution -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-027a-local-worker-pool -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-027b-docker-worker-pool -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-027c-log-multiplexing-dashboard -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-028-parallel-safety-merge-coordinator -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-028a-conflict-heuristics -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-028b-dependency-graph-hints -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-028c-integration-merge-plan-tests -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-029-computetarget-interface -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-029a-prepare-workspace -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-029b-exec-commands -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-029c-upload-download-artifacts -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-029d-teardown -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-030-ssh-target -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-030a-ssh-connection-management -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-030b-ssh-command-execution -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-030c-ssh-file-transfer -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-031-aws-ec2-target -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-031a-ec2-instance-provisioning -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-031b-ec2-instance-management -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-031c-ec2-cost-controls -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-032-placement-strategies -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-032a-capability-discovery -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-032b-capability-matching -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-032c-placement-strategy-implementations -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-033-cloud-burst-heuristics -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-033a-burst-trigger-conditions -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-033b-trigger-aggregation -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-033c-burst-rate-limiting -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-034-ci-template-generator -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-034a-github-actions-templates -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-034b-pinned-versions-minimal-permissions -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-034c-caching-setup -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-035-ci-maintenance-mode -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-035a-workflow-change-proposals-diffs -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-035b-approval-gates -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-035c-ci-specific-task-runner-support -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-036-deployment-hook-tool -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-036a-deploy-tool-schema -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-036b-disabled-by-default -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-036c-non-bypassable-approvals-default -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-037-policy-as-config-engine -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-037a-global-policy-config -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-037b-repo-overrides -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-037c-per-task-overrides -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-038-secrets-redaction-diff-scanning -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-038a-redact-tool-output-before-model-sees-it -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-038b-block-commitpush-on-secret-detection -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-038c-configurable-patterns-corpus-tests -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-039-audit-trail-export -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-039a-record-tool-calls-commands-diffs-models-prompts -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-039b-export-bundle -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-039c-verify-export-contains-no-raw-secrets -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-040-crash-safe-event-log -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-040a-append-only-event-log -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-040b-resume-rules -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-040c-ordering-guarantees -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-041-retry-policy-framework -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-041a-categorize-failures -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-041b-capped-retries -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-041c-needs-human-transition-rules -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-042-reproducibility-knobs -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-042a-persist-prompts-settings -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-042b-deterministic-mode-switches -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-042c-replay-tooling -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-043-output-summarization-pipeline -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-043a-summarize-failures -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-043b-attach-full-logs -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-043c-size-limits -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-044-retrieval-index-caching -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-044a-cache-keys-include-commit-hash -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-044b-stats-and-clear-commands -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-044c-hit-miss-telemetry -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-045-model-performance-harness -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-045a-microbench-metrics -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-045b-tool-call-correctness-rate -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-045c-report-comparisons -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-046-benchmark-task-suite -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-046a-store-tasks-as-specs -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-046b-runner-cli -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-046c-json-results -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-047-scoring-promotion-gates -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-047a-passfail-runtime-iterations -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-047b-thresholds-gating-rules -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-047c-diffable-historical-reports -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-048-golden-baseline-maintenance -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-048a-baseline-runs-recorded -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-048b-change-log-for-promptmodel-upgrades -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 
- [ ] task-048c-regression-triage-workflow -- Read instructions at top of this file and follow them, including reading the mentioned files, before starting expansion 

---

## Progress Log

Record expansion work here as tasks are completed.

| Date | Task | Lines | Notes |
|------|------|-------|-------|
| | | | |

---

## Quality Reminders

### What "Complete" Looks Like

From the e-commerce golden standard (Task 044 - Subscription/Recurring Orders):

- **3,699 lines** of comprehensive documentation
- **Description** explains business value with specific revenue projections ($88k ARR), customer LTV comparisons (3.2× higher), churn metrics (8-12% monthly)
- **Use Cases** have 3 fully-written scenarios with named personas (Sarah, Alex, Jordan), before/after workflows with specific numbers
- **User Manual** includes ASCII mockups of every screen, step-by-step instructions, pricing tables, billing frequency options
- **Implementation Prompt** contains complete C# code for entities, services, controllers, Blazor components - not snippets

### What "Incomplete" Looks Like

- Sections that exist but contain only 10-20 lines of generic content
- Acceptance criteria like "System should work correctly" (not testable)
- Testing requirements that say "Write appropriate tests" (not specific)
- Implementation prompts with "// ... rest of implementation" (incomplete)
- Missing Security Considerations section entirely
- Best Practices with 4 generic items instead of 12+ specific ones

### The Junior Developer Test

Before marking complete, ask:

1. Could someone with 6 months of C# experience implement this?
2. Would they need to ask ANY clarifying questions?
3. Would they need to Google how to do anything?
4. Are all edge cases documented?
5. Are all error scenarios described?
6. Is every configuration option listed with all possible values?

If ANY answer is "no" - the task is NOT complete.
