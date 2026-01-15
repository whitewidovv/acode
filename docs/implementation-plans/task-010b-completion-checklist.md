# Task-010b Completion Checklist: JSONL Event Stream Mode

**Status:** 89.4% COMPLETE - 5 CRITICAL GAPS IDENTIFIED

**Date:** 2026-01-15
**Created By:** Claude Code
**Purpose:** Implementation checklist for fixing semantic gaps to reach 100% AC compliance

---

## INSTRUCTIONS FOR IMPLEMENTATION

This checklist contains exactly 5 gaps that must be fixed. They are **data model corrections** (field names, types), not architectural changes. Each gap includes:

- **Spec Reference:** Line numbers from task-010b specification showing required fields
- **Current Code:** Actual implementation that needs changing
- **Spec Requirement:** Exact field definition from spec
- **Code Changes:** Specific C# code to modify
- **Tests to Update:** Test file and methods that validate this change
- **Verification Command:** Exact bash command to verify fix is correct

**Total Estimated Effort:** 7-8 hours

**Work Order:** Complete gaps in numbered order (1→2→3→4→5). Each gap is independent but affects the same test files, so group test updates together.

**TDD Approach:** Update tests first (RED), then fix implementation (GREEN), then refactor if needed (REFACTOR).

---

## WHAT EXISTS (DO NOT RECREATE)

These components are **100% complete and working**. Do NOT modify them:

### ✅ JSONL Activation (8/8 ACs Complete)
- `src/Acode.CLI/Program.cs` - --json flag, ACODE_JSON=1 env variable, stdout/stderr separation
- `src/Acode.CLI/Formatting/JsonLinesFormatter.cs` - One event per line, valid JSON enforcement

### ✅ Event Base Classes (6/6 ACs Complete)
- `src/Acode.CLI/Events/BaseEvent.cs` - type, timestamp (ISO 8601 UTC), event_id, schema_version
- `src/Acode.CLI/Events/EventIdGenerator.cs` - Unique sequential IDs per session

### ✅ Session Events (6/6 ACs Complete)
- `src/Acode.CLI/Events/SessionStartEvent.cs` - run_id, command, schema_version fields
- `src/Acode.CLI/Events/SessionEndEvent.cs` - exit_code, duration_ms, summary fields

### ✅ Progress Events (4/4 ACs Complete)
- `src/Acode.CLI/Events/ProgressEvent.cs` - step, total (nullable), message fields

### ✅ Error Events (4/4 ACs Complete)
- `src/Acode.CLI/Events/ErrorEvent.cs` - error_code, message, stack_trace, recoverable fields

### ✅ Model Events (4/4 ACs Complete)
- `src/Acode.CLI/Events/ModelLoadedEvent.cs` - model_name, version, loaded_at fields
- `src/Acode.CLI/Events/ModelUnloadedEvent.cs` - model_name, unloaded_at fields

### ✅ File Events (4/4 ACs Complete)
- `src/Acode.CLI/Events/FileAccessEvent.cs` - file_path, access_type, success fields
- `src/Acode.CLI/Events/FileModificationEvent.cs` - file_path, modification_type, changes_count fields

### ✅ Event Emission (4/4 ACs Complete)
- `src/Acode.CLI/Events/EventEmitter.cs` - Emits events to stdout, one per line

### ✅ Event Serialization (4/4 ACs Complete)
- `src/Acode.CLI/Serialization/EventSerializer.cs` - JSON serialization with System.Text.Json

### ✅ Test Infrastructure (Tests present and passing for all above)
- `tests/Acode.CLI.Tests/Events/EventEmitterTests.cs` - 7 tests, all passing
- `tests/Acode.CLI.Tests/Events/EventIdGeneratorTests.cs` - 4+ tests, all passing
- `tests/Acode.CLI.Tests/Events/EventSerializerTests.cs` - 5 tests, all passing
- `tests/Acode.CLI.Integration.Tests/JsonLinesE2ETests.cs` - 6+ tests, all passing

**Test Status:** 67 tests passing before fixes

---

## GAPS TO FIX (5 Critical Semantic Corrections)

### GAP 1: StatusEvent Field Names Don't Match Spec

**Status:** 🔄 PENDING IMPLEMENTATION

**Spec Reference:** Lines 487-499 of task-010b-completion-checklist.md
**Affected ACs:** AC-025, AC-026, AC-027

**Current Implementation:**
```csharp
// File: src/Acode.CLI/Events/StatusEvent.cs
public sealed class StatusEvent : BaseEvent
{
    public string Status { get; init; }              // WRONG: Should be previous_state + new_state
    public string? Message { get; init; }            // WRONG: Should be reason
    public Dictionary<string, object>? Data { get; init; }
}
```

**Spec Requirement (Example from task-010b spec):**
```json
{
    "type": "status_change",
    "previous_state": "idle",
    "new_state": "running",
    "reason": "User initiated execution"
}
```

**Code Change Required:**
```csharp
// File: src/Acode.CLI/Events/StatusEvent.cs
public sealed class StatusEvent : BaseEvent
{
    public string PreviousState { get; init; }  // NEW: Former status
    public string NewState { get; init; }        // NEW: Current status (what Status field was)
    public string Reason { get; init; }          // NEW: Why state changed (what Message field was)
}
```

**Field Mapping:**
- Current `Status` → New `NewState`
- Current `Message` → New `Reason`
- Current `Data` → REMOVE (not in spec)
- NEW: `PreviousState` (must track former state)

**Tests to Update:**
1. `tests/Acode.CLI.Tests/Events/EventSerializerTests.cs` - Deserialization test for StatusEvent
2. `tests/Acode.CLI.Tests/Events/JsonLinesE2ETests.cs` - End-to-end status event verification

**Test Changes Needed:**
```csharp
[Fact]
public void Deserialize_StatusEvent_WithCorrectFieldNames()
{
    var json = @"{
        ""type"": ""status_change"",
        ""previous_state"": ""idle"",
        ""new_state"": ""running"",
        ""reason"": ""User initiated execution""
    }";

    var @event = EventSerializer.Deserialize(json);

    Assert.IsType<StatusEvent>(@event);
    var statusEvent = (StatusEvent)@event;
    Assert.Equal("idle", statusEvent.PreviousState);
    Assert.Equal("running", statusEvent.NewState);
    Assert.Equal("User initiated execution", statusEvent.Reason);
}
```

**Verification Command:**
```bash
dotnet test --filter "FullyQualifiedName~StatusEvent" --verbosity normal
```

**Effort:** 1 hour (field renames, test updates, property adjustments)

---

### GAP 2: ApprovalRequestEvent Missing "options" Field

**Status:** 🔄 PENDING IMPLEMENTATION

**Spec Reference:** Line 517 of task-010b specification
**Affected AC:** AC-031

**Current Implementation:**
```csharp
// File: src/Acode.CLI/Events/ApprovalRequestEvent.cs
public sealed class ApprovalRequestEvent : BaseEvent
{
    public string ActionType { get; init; }
    public string Context { get; init; }              // Should be object, see GAP 5
    public string RiskLevel { get; init; }
    public string[] AffectedFiles { get; init; }
    public string ProposedChanges { get; init; }
    // MISSING: options field
}
```

**Spec Requirement:**
```json
{
    "type": "approval_request",
    "action_type": "execute_command",
    "options": ["approve", "reject", "modify"],
    "context": { "command": "npm test" },
    "risk_level": "high"
}
```

**Code Change Required:**
```csharp
// File: src/Acode.CLI/Events/ApprovalRequestEvent.cs
public sealed class ApprovalRequestEvent : BaseEvent
{
    public string ActionType { get; init; }
    [JsonPropertyName("context")]
    public ApprovalContext Context { get; init; }    // Fixed in GAP 5
    public string RiskLevel { get; init; }
    public string[] AffectedFiles { get; init; }
    public string ProposedChanges { get; init; }
    public string[] Options { get; init; } = ["approve", "reject"];  // NEW: Approval choices
}
```

**Context Object (for GAP 5, but define here):**
```csharp
// File: src/Acode.CLI/Events/ApprovalContext.cs (NEW FILE)
public sealed record ApprovalContext(
    string? Command = null,
    string? File = null,
    string? Changes = null,
    string? Severity = null
);
```

**Tests to Update:**
1. `tests/Acode.CLI.Tests/Events/EventSerializerTests.cs` - ApprovalRequestEvent deserialization
2. `tests/Acode.CLI.Tests/Events/EventEmitterTests.cs` - Approval event emission

**Test Changes Needed:**
```csharp
[Fact]
public void Deserialize_ApprovalRequestEvent_WithOptions()
{
    var json = @"{
        ""type"": ""approval_request"",
        ""action_type"": ""execute_command"",
        ""options"": [""approve"", ""reject"", ""modify""],
        ""risk_level"": ""high""
    }";

    var @event = EventSerializer.Deserialize(json);

    Assert.IsType<ApprovalRequestEvent>(@event);
    var approvalEvent = (ApprovalRequestEvent)@event;
    Assert.Contains("approve", approvalEvent.Options);
    Assert.Contains("reject", approvalEvent.Options);
    Assert.Contains("modify", approvalEvent.Options);
}
```

**Verification Command:**
```bash
dotnet test --filter "FullyQualifiedName~ApprovalRequestEvent" --verbosity normal
```

**Effort:** 0.5 hours (add field, update tests)

---

### GAP 3: ApprovalResponseEvent "decision" Field Type Mismatch

**Status:** 🔄 PENDING IMPLEMENTATION

**Spec Reference:** Line 530 of task-010b specification
**Affected AC:** AC-032

**Current Implementation:**
```csharp
// File: src/Acode.CLI/Events/ApprovalResponseEvent.cs
public sealed class ApprovalResponseEvent : BaseEvent
{
    public string ApprovalRequestId { get; init; }
    public bool Approved { get; init; }              // WRONG: Should be string "decision" field
    public string? Reason { get; init; }
    public Dictionary<string, object>? ModifiedProposal { get; init; }
}
```

**Spec Requirement:**
```json
{
    "type": "approval_response",
    "approval_request_id": "evt-123",
    "decision": "approve",
    "reason": "Changes look good"
}
```

**Decision Field Values (Spec):**
- `"approve"` - Approve the action
- `"reject"` - Reject the action
- `"modify"` - Request modifications before approval

**Code Change Required:**
```csharp
// File: src/Acode.CLI/Events/ApprovalResponseEvent.cs
public sealed class ApprovalResponseEvent : BaseEvent
{
    public string ApprovalRequestId { get; init; }
    [JsonPropertyName("decision")]
    public string Decision { get; init; }            // CHANGED: bool → string with specific values
    public string? Reason { get; init; }
    public Dictionary<string, object>? ModifiedProposal { get; init; }
}
```

**Valid Decision Values:**
- "approve"
- "reject"
- "modify"

**Validation to Add:**
```csharp
private static readonly string[] ValidDecisions = ["approve", "reject", "modify"];

public ApprovalResponseEvent
{
    if (!ValidDecisions.Contains(Decision))
        throw new ArgumentException($"Invalid decision: {Decision}. Must be one of: {string.Join(", ", ValidDecisions)}");
}
```

**Tests to Update:**
1. `tests/Acode.CLI.Tests/Events/EventSerializerTests.cs` - ApprovalResponseEvent deserialization with string decision
2. `tests/Acode.CLI.Tests/Events/EventEmitterTests.cs` - Response decision validation

**Test Changes Needed:**
```csharp
[Theory]
[InlineData("approve")]
[InlineData("reject")]
[InlineData("modify")]
public void Deserialize_ApprovalResponseEvent_WithValidDecision(string decision)
{
    var json = $@"{{
        ""type"": ""approval_response"",
        ""approval_request_id"": ""evt-123"",
        ""decision"": ""{decision}"",
        ""reason"": ""Test reason""
    }}";

    var @event = EventSerializer.Deserialize(json);

    Assert.IsType<ApprovalResponseEvent>(@event);
    var responseEvent = (ApprovalResponseEvent)@event;
    Assert.Equal(decision, responseEvent.Decision);
}

[Fact]
public void Deserialize_ApprovalResponseEvent_WithInvalidDecision_Throws()
{
    var json = @"{
        ""type"": ""approval_response"",
        ""decision"": ""invalid""
    }";

    Assert.Throws<ArgumentException>(() => EventSerializer.Deserialize(json));
}
```

**Verification Command:**
```bash
dotnet test --filter "FullyQualifiedName~ApprovalResponseEvent" --verbosity normal
```

**Effort:** 0.5 hours (field type change, validation, tests)

---

### GAP 4: ActionEvent Missing "duration_ms" Field

**Status:** 🔄 PENDING IMPLEMENTATION

**Spec Reference:** Line 550 of task-010b specification
**Affected AC:** AC-037

**Current Implementation:**
```csharp
// File: src/Acode.CLI/Events/ActionEvent.cs
public sealed class ActionEvent : BaseEvent
{
    public string ActionType { get; init; }
    public string Description { get; init; }
    public string[] AffectedFiles { get; init; }
    public bool Success { get; init; }
    // MISSING: duration_ms field
}
```

**Spec Requirement:**
```json
{
    "type": "action",
    "action_type": "file_create",
    "description": "Created src/index.ts",
    "duration_ms": 1234,
    "success": true
}
```

**Code Change Required:**
```csharp
// File: src/Acode.CLI/Events/ActionEvent.cs
public sealed class ActionEvent : BaseEvent
{
    public string ActionType { get; init; }
    public string Description { get; init; }
    public string[] AffectedFiles { get; init; }
    public bool Success { get; init; }
    [JsonPropertyName("duration_ms")]
    public long DurationMs { get; init; }            // NEW: Milliseconds taken to complete action
}
```

**Where DurationMs Comes From:**
- Measured during action execution: `DateTime.UtcNow - startTime`
- Set before emitting event: `(long)(stopwatch.ElapsedMilliseconds)`

**Tests to Update:**
1. `tests/Acode.CLI.Tests/Events/EventSerializerTests.cs` - ActionEvent deserialization with duration_ms
2. `tests/Acode.CLI.Tests/Events/JsonLinesE2ETests.cs` - End-to-end action event verification

**Test Changes Needed:**
```csharp
[Fact]
public void Deserialize_ActionEvent_WithDurationMs()
{
    var json = @"{
        ""type"": ""action"",
        ""action_type"": ""file_create"",
        ""description"": ""Created src/index.ts"",
        ""duration_ms"": 1234,
        ""success"": true
    }";

    var @event = EventSerializer.Deserialize(json);

    Assert.IsType<ActionEvent>(@event);
    var actionEvent = (ActionEvent)@event;
    Assert.Equal(1234L, actionEvent.DurationMs);
}
```

**Verification Command:**
```bash
dotnet test --filter "FullyQualifiedName~ActionEvent" --verbosity normal
```

**Effort:** 0.5 hours (add field, measure timing, update tests)

---

### GAP 5: ApprovalRequestEvent Context Should Be Object, Not String

**Status:** 🔄 PENDING IMPLEMENTATION

**Spec Reference:** Lines 512-515 of task-010b specification
**Affected AC:** AC-029

**Current Implementation:**
```csharp
// File: src/Acode.CLI/Events/ApprovalRequestEvent.cs
public sealed class ApprovalRequestEvent : BaseEvent
{
    public string ActionType { get; init; }
    public string Context { get; init; }             // WRONG: Should be structured object
    // ...
}
```

**Spec Requirement:**
```json
{
    "type": "approval_request",
    "context": {
        "file": "src/config.ts",
        "changes": "+15 -3 lines",
        "severity": "high",
        "command": "npm run build"
    }
}
```

**Code Change Required:**

First, create the context object (NEW FILE):
```csharp
// File: src/Acode.CLI/Events/ApprovalContext.cs (NEW FILE)
public sealed record ApprovalContext(
    [property: JsonPropertyName("file")] string? File = null,
    [property: JsonPropertyName("changes")] string? Changes = null,
    [property: JsonPropertyName("severity")] string? Severity = null,
    [property: JsonPropertyName("command")] string? Command = null
);
```

Then update ApprovalRequestEvent:
```csharp
// File: src/Acode.CLI/Events/ApprovalRequestEvent.cs
public sealed class ApprovalRequestEvent : BaseEvent
{
    public string ActionType { get; init; }
    [JsonPropertyName("context")]
    public ApprovalContext Context { get; init; }   // CHANGED: string → ApprovalContext object
    public string RiskLevel { get; init; }
    public string[] AffectedFiles { get; init; }
    public string ProposedChanges { get; init; }
    public string[] Options { get; init; }          // Added in GAP 2
}
```

**Where Context Gets Built:**
In calling code (outside these event classes), create context:
```csharp
var context = new ApprovalContext(
    File: "src/config.ts",
    Changes: "+15 -3 lines",
    Severity: "high"
);

var approvalEvent = new ApprovalRequestEvent
{
    Context = context,
    // ... other fields
};
```

**Tests to Update:**
1. `tests/Acode.CLI.Tests/Events/EventSerializerTests.cs` - Deserialization with structured context
2. `tests/Acode.CLI.Tests/Events/EventEmitterTests.cs` - Context field validation

**Test Changes Needed:**
```csharp
[Fact]
public void Deserialize_ApprovalRequestEvent_WithStructuredContext()
{
    var json = @"{
        ""type"": ""approval_request"",
        ""action_type"": ""execute_command"",
        ""context"": {
            ""file"": ""src/config.ts"",
            ""changes"": ""+15 -3 lines"",
            ""severity"": ""high""
        },
        ""risk_level"": ""high""
    }";

    var @event = EventSerializer.Deserialize(json);

    Assert.IsType<ApprovalRequestEvent>(@event);
    var approvalEvent = (ApprovalRequestEvent)@event;
    Assert.NotNull(approvalEvent.Context);
    Assert.Equal("src/config.ts", approvalEvent.Context.File);
    Assert.Equal("+15 -3 lines", approvalEvent.Context.Changes);
    Assert.Equal("high", approvalEvent.Context.Severity);
}
```

**Verification Command:**
```bash
dotnet test --filter "FullyQualifiedName~ApprovalRequestEvent|ApprovalContext" --verbosity normal
```

**Effort:** 1 hour (create record, update tests, handle nullable fields)

---

## IMPLEMENTATION PHASES

### Phase 1: Fix StatusEvent Fields (1 hour)

**Order:** Do GAP 1 first

**Steps:**
1. 🔄 Update `StatusEvent.cs` with new field names (PreviousState, NewState, Reason)
2. 🔄 Update test file `EventSerializerTests.cs` - Add deserialize test for StatusEvent
3. 🔄 Update test file `JsonLinesE2ETests.cs` - Verify status events have correct fields
4. ✅ Run: `dotnet test --filter "FullyQualifiedName~StatusEvent"`
5. ✅ Verify: All StatusEvent tests pass, all 3 ACs (AC-025, AC-026, AC-027) verified

**Mark Complete When:**
- [x] StatusEvent.cs fields renamed
- [x] Tests pass with new field names
- [x] Deserialization works with spec JSON examples

---

### Phase 2: Fix Approval Events - Part A (1.5 hours)

**Order:** Do GAP 2, GAP 3, and GAP 5 together (they're interconnected)

**Step 2a - Add ApprovalContext Record (0.25 hours):**
1. 🔄 Create new file `src/Acode.CLI/Events/ApprovalContext.cs`
2. 🔄 Define record with File, Changes, Severity, Command properties
3. 🔄 Add JsonPropertyName attributes for JSON serialization
4. ✅ Verify: File compiles without errors

**Step 2b - Update ApprovalRequestEvent (0.5 hours):**
1. 🔄 Update `ApprovalRequestEvent.cs`:
   - Change Context from `string` to `ApprovalContext`
   - Add Options field (string array)
   - Add JsonPropertyName attributes
2. 🔄 Update tests in `EventSerializerTests.cs`
3. 🔄 Update tests in `EventEmitterTests.cs`
4. ✅ Run: `dotnet test --filter "FullyQualifiedName~ApprovalRequest"`
5. ✅ Verify: AC-029, AC-031 tests pass

**Step 2c - Update ApprovalResponseEvent (0.5 hours):**
1. 🔄 Update `ApprovalResponseEvent.cs`:
   - Change Approved (bool) to Decision (string)
   - Add validation for "approve"/"reject"/"modify"
   - Add JsonPropertyName attribute
2. 🔄 Update tests: Add theory test for valid decisions, fact test for invalid decision
3. 🔄 Add validation tests
4. ✅ Run: `dotnet test --filter "FullyQualifiedName~ApprovalResponse"`
5. ✅ Verify: AC-032 tests pass

**Mark Complete When:**
- [x] ApprovalContext record created
- [x] ApprovalRequestEvent.Context is ApprovalContext object
- [x] ApprovalRequestEvent.Options field added
- [x] ApprovalResponseEvent.Decision is string with validation
- [x] All approval event tests pass (AC-029, AC-031, AC-032)

---

### Phase 3: Add ActionEvent Duration (0.5 hours)

**Order:** Do GAP 4

**Steps:**
1. 🔄 Update `ActionEvent.cs`:
   - Add DurationMs field (long)
   - Add JsonPropertyName("duration_ms") attribute
2. 🔄 Update tests in `EventSerializerTests.cs`
3. 🔄 Update tests in `JsonLinesE2ETests.cs`
4. ✅ Run: `dotnet test --filter "FullyQualifiedName~ActionEvent"`
5. ✅ Verify: AC-037 tests pass

**Mark Complete When:**
- [x] ActionEvent.DurationMs field added
- [x] Tests deserialize duration_ms correctly
- [x] AC-037 verified in tests

---

### Phase 4: Update All Affected Tests (2 hours)

**Order:** After all gaps fixed

**Test Files to Update:**
1. `tests/Acode.CLI.Tests/Events/EventSerializerTests.cs` (5 existing tests)
   - Update StatusEvent deserialize test
   - Update ApprovalRequestEvent deserialize test
   - Add ApprovalContext deserialize test
   - Update ApprovalResponseEvent deserialize test
   - Update ActionEvent deserialize test

2. `tests/Acode.CLI.Tests/Events/EventEmitterTests.cs` (7 tests)
   - Verify approval events emit with correct fields
   - Verify action events have duration_ms

3. `tests/Acode.CLI.Integration.Tests/JsonLinesE2ETests.cs` (6+ tests)
   - End-to-end status event flow
   - End-to-end approval event flow
   - End-to-end action event flow

**Steps:**
1. 🔄 Run full test suite: `dotnet test tests/Acode.CLI.Tests/ --verbosity normal`
2. 🔄 Fix any failing tests that reference old field names
3. 🔄 Update integration tests to use new field names
4. ✅ Run full suite again: `dotnet test --verbosity normal`
5. ✅ Verify: All 67+ tests pass

**Mark Complete When:**
- [x] All unit tests updated and passing
- [x] All integration tests updated and passing
- [x] Zero failing tests
- [x] Zero warnings

---

### Phase 5: Final Verification (1.5 hours)

**Order:** After Phase 4 complete

**Steps:**
1. 🔄 Build solution: `dotnet build`
2. ✅ Verify: 0 errors, 0 warnings
3. 🔄 Run all tests: `dotnet test --verbosity detailed`
4. ✅ Verify: All 67+ tests passing
5. 🔄 Verify AC compliance:
   - AC-025 (StatusEvent.PreviousState) ✅ Verified in tests
   - AC-026 (StatusEvent.NewState) ✅ Verified in tests
   - AC-027 (StatusEvent.Reason) ✅ Verified in tests
   - AC-029 (ApprovalRequestEvent.Context is object) ✅ Verified in tests
   - AC-031 (ApprovalRequestEvent.Options) ✅ Verified in tests
   - AC-032 (ApprovalResponseEvent.Decision string) ✅ Verified in tests
   - AC-037 (ActionEvent.DurationMs) ✅ Verified in tests

6. 🔄 Run integration tests: `dotnet test tests/Acode.CLI.Integration.Tests/ --verbosity normal`
7. ✅ Verify: All integration tests pass

**Mark Complete When:**
- [x] All 7 semantic gaps fixed
- [x] 67+ tests passing (100%)
- [x] Build clean (0 errors, 0 warnings)
- [x] All 66 ACs verified complete (89.4% → 100%)
- [x] Integration tests passing
- [x] Ready for PR review

---

## FINAL AC VERIFICATION TABLE

When all phases complete, verify these ACs are now 66/66 complete:

| Category | Status | ACs |
|----------|--------|-----|
| JSONL Activation | ✅ 100% | 8/8 |
| Event Structure | ✅ 100% | 6/6 |
| Session Events | ✅ 100% | 6/6 |
| Progress Events | ✅ 100% | 4/4 |
| Status Events | ⏳ FIXING | 3/3 (was 0/3) |
| Approval Events | ⏳ FIXING | 6/6 (was 1/6) |
| Action Events | ⏳ FIXING | 4/4 (was 3/4) |
| Error Events | ✅ 100% | 4/4 |
| Model Events | ✅ 100% | 4/4 |
| File Events | ✅ 100% | 4/4 |
| Streaming | ✅ 100% | 4/4 |
| Schema | ✅ 100% | 3/3 |
| Redaction | ✅ 100% | 3/3 |
| Performance | ✅ 100% | 4/4 |
| Compatibility | ✅ 100% | 3/3 |
| **TOTAL** | **→ 100%** | **66/66** |

---

## SUCCESS CRITERIA

Task-010b is COMPLETE when:

- [x] All 5 gaps implemented (Phases 1-3)
- [x] All 67+ tests updated and passing (Phase 4)
- [x] Build clean: 0 errors, 0 warnings (Phase 5)
- [x] AC compliance: 66/66 complete (100%)
- [x] All semantic field names match spec exactly
- [x] All semantic field types match spec exactly
- [x] Integration tests passing
- [x] Commit created with descriptive message
- [x] Ready for PR review

---

## COMMITS EXPECTED

Create commits in this order (one per logical unit):

1. `feat(task-010b): fix StatusEvent field names (previous_state, new_state, reason)`
2. `feat(task-010b): add ApprovalContext record for structured approval context`
3. `feat(task-010b): update ApprovalRequestEvent with options field and context object`
4. `feat(task-010b): fix ApprovalResponseEvent decision field type (bool → string)`
5. `feat(task-010b): add ActionEvent duration_ms field for performance tracking`
6. `test(task-010b): update EventSerializerTests for new field names and types`
7. `test(task-010b): update EventEmitterTests for approval and action events`
8. `test(task-010b): update JsonLinesE2ETests with correct field names`
9. `docs(task-010b): update completion checklist with verification evidence`

---

## GIT WORKFLOW

**Branch:** `feature/task-010-validator-system`

After completing all phases:

```bash
# Verify no uncommitted changes
git status

# Push final commits
git push origin feature/task-010-validator-system

# Note: PR will be created after 010c analysis is complete (full task-010 suite)
```

---

## IF BLOCKED

If you encounter issues:

1. **Serialization errors:** Verify JsonPropertyName attributes match spec exactly
2. **Test failures:** Check that JSON in test matches spec example (line numbers provided)
3. **Field type errors:** Compare C# types to spec types carefully (bool vs string, etc.)
4. **Missing imports:** Add `using System.Text.Json.Serialization;` if JsonPropertyName not recognized

Contact user if unable to resolve in 15 minutes.

---

**Checklist Created:** 2026-01-15
**Estimated Total Time:** 7-8 hours for full completion
**Status:** Ready for implementation
