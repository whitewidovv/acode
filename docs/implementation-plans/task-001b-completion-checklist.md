# Task 001b Completion Checklist

**Task**: Define "No External LLM API" Validation Rules
**Spec**: docs/tasks/refined-tasks/Epic 00/task-001b-define-no-external-llm-validation-rules.md
**Created**: 2026-01-11
**Status**: 🔄 IN PROGRESS

---

## Instructions for Implementation

This checklist was created via gap analysis per CLAUDE.md Section 3.2. Each item represents a specific requirement from the task specification that must be implemented, tested, and verified.

**How to use this checklist:**

1. Work through items **in order** (top to bottom)
2. Follow strict TDD: **Write tests FIRST (RED), then implement (GREEN), then refactor (REFACTOR)**
3. Mark items as you progress:
   - `- [ ]` = Pending
   - `- [🔄]` = In progress
   - `- [✅]` = Complete (tests passing, code committed)
4. **Commit after each logical unit** (each checklist item or small group of related items)
5. **Do NOT skip any items** - everything in this checklist is required per the spec
6. **Verify tests pass** before marking complete

---

## Current Implementation Status

**Existing (from previous session):**
- ✅ `OperatingMode` enum (src/Acode.Domain/Modes/OperatingMode.cs) - Task 001a
- ✅ `EndpointValidationResult` record (src/Acode.Domain/Validation/EndpointValidationResult.cs)
  - Has Allowed() and Denied() factory methods
  - Has implicit bool conversion
  - Tests exist and passing
- ✅ `LlmApiDenylist` static class (src/Acode.Domain/Validation/LlmApiDenylist.cs)
  - Basic denylist with exact and subdomain matching
  - Includes major LLM providers (OpenAI, Anthropic, Google, Cohere, etc.)
  - Tests exist and passing

**Gaps (what this checklist will implement):**
- ❌ Pattern-based matching (wildcard, regex) - spec requires (FR-001b-13, FR-001b-34, FR-001b-35)
- ❌ Allowlist implementation - spec requires (FR-001b-41 to FR-001b-55)
- ❌ IEndpointValidator interface - spec requires (lines 758-767)
- ❌ EndpointValidator implementation - spec requires (lines 706-798)
- ❌ Loadable denylist from file - spec requires (FR-001b-36, FR-001b-37)
- ❌ DenylistProvider class - spec mentions (line 717)
- ❌ AllowlistProvider class - spec mentions (line 718)
- ❌ data/denylist.json file - spec requires (lines 800-864)
- ❌ User documentation - spec requires (lines 290-421)
- ❌ Comprehensive test coverage - spec requires 20 unit tests, 10 integration tests

---

## Phase 1: Enhance Pattern Matching (Foundation)

**Goal**: Implement advanced pattern matching per spec lines 770-798

### 1.1 Create PatternType Enum
- [✅] **File**: `src/Acode.Domain/Validation/PatternType.cs`
- [✅] Define enum with: Exact, Wildcard, Regex (per spec line 792-797)
- [✅] Add XML documentation referencing FR-001b-13, FR-001b-34, FR-001b-35
- [✅] **Tests**: `tests/Acode.Domain.Tests/Validation/PatternTypeTests.cs`
  - Test enum values exist
  - Test enum can be compared

**Implementation notes from spec:**
```csharp
public enum PatternType
{
    Exact,      // api.openai.com
    Wildcard,   // *.openai.com
    Regex       // .*\.openai\.azure\.com
}
```

### 1.2 Create EndpointPattern Record
- [✅] **File**: `src/Acode.Domain/Validation/EndpointPattern.cs`
- [✅] Properties: Pattern (string), Type (PatternType), Description (string?)
- [✅] Method: `bool Matches(Uri uri)` with switch on PatternType
- [✅] Private field: `Lazy<Regex?>` for pre-compiled regex patterns
- [✅] Private methods: `MatchExact(Uri)`, `MatchWildcard(Uri)`, `MatchRegex(Uri)`
- [✅] Constructor with lazy regex compilation
- [✅] Custom Equals/GetHashCode excluding _compiledRegex
- [✅] **Tests**: `tests/Acode.Domain.Tests/Validation/EndpointPatternTests.cs`
  - [✅] Test exact matching: "api.openai.com" matches https://api.openai.com/v1/chat
  - [✅] Test exact matching: "api.openai.com" does NOT match https://chat.openai.com
  - [✅] Test wildcard matching: "*.openai.com" matches https://chat.openai.com
  - [✅] Test wildcard matching: "*.openai.com" matches https://api.openai.com
  - [✅] Test wildcard matching: "*.openai.com" does NOT match https://openai.com (no subdomain)
  - [✅] Test regex matching: ".*\\.azure\\.com" matches https://foo.azure.com
  - [✅] Test regex matching: "bedrock.*\\.amazonaws\\.com" matches bedrock-runtime.us-east-1.amazonaws.com
  - [✅] Test case insensitive matching for all types
  - [✅] Test invalid regex pattern throws on first use (lazy validation)
  - [✅] Test pre-compiled regex is reused (performance)
  - [✅] Test record equality
  - [✅] Test unknown pattern type returns false

**Implementation notes from spec (lines 770-798):**
```csharp
public sealed record EndpointPattern
{
    public required string Pattern { get; init; }
    public required PatternType Type { get; init; }
    public string? Description { get; init; }

    private readonly Regex? _compiledRegex;

    // Constructor compiles regex if needed
    public EndpointPattern()
    {
        if (Type == PatternType.Regex)
        {
            _compiledRegex = new Regex(Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }

    public bool Matches(Uri uri)
    {
        return Type switch
        {
            PatternType.Exact => MatchExact(uri),
            PatternType.Wildcard => MatchWildcard(uri),
            PatternType.Regex => MatchRegex(uri),
            _ => false
        };
    }

    private bool MatchExact(Uri uri) => uri.Host.Equals(Pattern, StringComparison.OrdinalIgnoreCase);

    private bool MatchWildcard(Uri uri)
    {
        // *.openai.com should match api.openai.com, chat.openai.com, etc.
        if (Pattern.StartsWith("*."))
        {
            var domain = Pattern.Substring(2);
            return uri.Host.EndsWith(domain, StringComparison.OrdinalIgnoreCase) ||
                   uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase);
        }
        return uri.Host.Equals(Pattern, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchRegex(Uri uri) => _compiledRegex?.IsMatch(uri.Host) ?? false;
}
```

### 1.3 Update LlmApiDenylist to Use Patterns
- [✅] **File**: `src/Acode.Domain/Validation/LlmApiDenylist.cs` (UPDATED)
- [✅] Change from `FrozenSet<string>` to `FrozenSet<EndpointPattern>`
- [✅] Update static constructor to create EndpointPattern instances
- [✅] Update `IsDenied(Uri)` to iterate patterns and call `Matches(uri)`
- [✅] Add regex patterns for Azure OpenAI: `.*\.openai\.azure\.com`
- [✅] Add regex patterns for AWS Bedrock: `bedrock.*\.amazonaws\.com`
- [✅] Keep existing exact patterns for major providers
- [✅] Add wildcard patterns for subdomains: `*.openai.com`, `*.anthropic.com`
- [✅] Changed method: `GetDeniedHosts()` → `GetDeniedPatterns()`
- [✅] **Tests**: `tests/Acode.Domain.Tests/Validation/LlmApiDenylistTests.cs` (UPDATED)
  - [✅] Test Azure OpenAI regex pattern: https://foo.openai.azure.com should be denied
  - [✅] Test AWS Bedrock regex pattern: all regions should be denied
  - [✅] Test wildcard pattern: https://chat.openai.com should be denied
  - [✅] Test subdomain not matching root: https://openai.com should NOT be denied
  - [✅] All existing tests still passing (26/26 tests passing)

**Implementation notes:**
- This enhances the existing implementation to support FR-001b-34, FR-001b-35
- Pre-compiled regex ensures NFR-001b-17 (performance)

---

## Phase 2: Implement Allowlist

**Goal**: Implement allowlist per spec FR-001b-41 to FR-001b-55

### 2.1 Create AllowlistEntry Record
- [ ] **File**: `src/Acode.Domain/Validation/AllowlistEntry.cs`
- [ ] Properties: Host (string), Ports (int[]?), Reason (string), RequireMode (OperatingMode?)
- [ ] Method: `bool Matches(Uri uri)` - check host and optionally port
- [ ] Handle localhost, 127.0.0.1, ::1 equivalence
- [ ] **Tests**: `tests/Acode.Domain.Tests/Validation/AllowlistEntryTests.cs`
  - Test localhost matches http://localhost:11434
  - Test 127.0.0.1 matches http://127.0.0.1:11434
  - Test ::1 matches http://[::1]:11434
  - Test port restriction: entry with ports=[11434] matches :11434 but not :8080
  - Test no port restriction: entry with ports=null matches any port
  - Test case insensitive host matching

### 2.2 Create AllowlistProvider Class (Domain)
- [ ] **File**: `src/Acode.Domain/Validation/IAllowlistProvider.cs`
- [ ] Interface with methods:
  - `IReadOnlyList<AllowlistEntry> GetDefaultAllowlist()`
  - `bool IsAllowed(Uri uri, OperatingMode mode)`
- [ ] **File**: `src/Acode.Domain/Validation/DefaultAllowlist.cs`
- [ ] Static class with default allowlist:
  - 127.0.0.1 ports [11434] (Ollama)
  - localhost ports [11434] (Ollama)
  - ::1 ports [11434] (Ollama IPv6)
- [ ] **Tests**: `tests/Acode.Domain.Tests/Validation/DefaultAllowlistTests.cs`
  - Test default allowlist contains localhost
  - Test default allowlist contains 127.0.0.1
  - Test default allowlist contains ::1
  - Test default allowlist specifies port 11434
  - Test http://127.0.0.1:11434 is allowed
  - Test http://localhost:11434 is allowed
  - Test http://[::1]:11434 is allowed
  - Test http://external.com is NOT allowed
  - Test http://127.0.0.1:8080 is allowed (port optional in default)

**Implementation notes from spec:**
- FR-001b-41 to FR-001b-43: Must include 127.0.0.1, localhost, ::1
- FR-001b-44: Must include Ollama default port 11434
- FR-001b-47: NO external IPs by default

---

## Phase 3: Implement IEndpointValidator Interface and Implementation

**Goal**: Create the main validation interface and implementation per spec lines 758-767

### 3.1 Create IEndpointValidator Interface
- [ ] **File**: `src/Acode.Domain/Validation/IEndpointValidator.cs`
- [ ] Method: `EndpointValidationResult Validate(Uri endpoint, OperatingMode mode)`
- [ ] Method: `EndpointValidationResult ValidateIp(IPAddress ip, OperatingMode mode)`
- [ ] XML documentation referencing FR-001b-56 to FR-001b-75
- [ ] **Tests**: Interface doesn't need direct tests (implementation will test)

**Implementation notes from spec (lines 758-767):**
```csharp
public interface IEndpointValidator
{
    EndpointValidationResult Validate(Uri endpoint, OperatingMode mode);
    EndpointValidationResult ValidateIp(IPAddress ip, OperatingMode mode);
}
```

### 3.2 Create EndpointValidator Implementation
- [ ] **File**: `src/Acode.Infrastructure/Network/EndpointValidator.cs` (CREATE Network dir)
- [ ] Implement IEndpointValidator
- [ ] Constructor takes IAllowlistProvider, denylist patterns
- [ ] Validate() logic:
  1. Check allowlist FIRST (FR-001b-49)
  2. If allowed, return Allowed()
  3. Check denylist
  4. If denied, return Denied() with pattern info and remediation
  5. Check mode: if Airgapped, deny ALL non-localhost
  6. If LocalOnly, deny non-localhost except allowlist
  7. If Burst, allow all
- [ ] ValidateIp() logic:
  1. Check if IP is loopback (127.0.0.1, ::1)
  2. If loopback, allowed
  3. Otherwise apply mode rules
- [ ] Handle null URLs (FR-001b-28: must be blocked)
- [ ] Handle malformed URLs (FR-001b-30: must be blocked)
- [ ] **Tests**: `tests/Acode.Infrastructure.Tests/Network/EndpointValidatorTests.cs`
  - Test LocalOnly mode denies https://api.openai.com
  - Test LocalOnly mode allows http://127.0.0.1:11434
  - Test LocalOnly mode allows http://localhost:11434
  - Test Burst mode allows https://api.openai.com
  - Test Airgapped mode denies ALL network (even 127.0.0.1)
  - Test allowlist checked before denylist
  - Test null URI blocked
  - Test malformed URI blocked
  - Test error message includes remediation
  - Test error message includes matched pattern
  - Test error message includes current mode
  - Test ValidateIp() for 127.0.0.1 allowed
  - Test ValidateIp() for 8.8.8.8 denied in LocalOnly
  - Test ValidateIp() for ::1 allowed
  - Test case insensitive matching

**Implementation notes:**
- Satisfies FR-001b-56 to FR-001b-75 (validation checkpoints)
- Satisfies FR-001b-76 to FR-001b-90 (violation response)
- Satisfies NFR-001b-01 to NFR-001b-15 (security)

---

## Phase 4: Loadable Denylist from File

**Goal**: Implement loadable denylist per FR-001b-36, FR-001b-37, spec lines 800-864

### 4.1 Create data/denylist.json
- [ ] **File**: `data/denylist.json`
- [ ] JSON structure per spec lines 802-864:
  - version: "1.0.0"
  - updated: (current date)
  - patterns: array of {pattern, type, description}
- [ ] Include all patterns from spec:
  - OpenAI: api.openai.com (exact), *.openai.com (wildcard)
  - Anthropic: api.anthropic.com (exact), *.anthropic.com (wildcard)
  - Azure OpenAI: .*\.openai\.azure\.com (regex)
  - Google AI: generativelanguage.googleapis.com (exact)
  - AWS Bedrock: bedrock.*\.amazonaws\.com (regex)
  - Cohere: api.cohere.ai (exact)
  - Hugging Face: api-inference.huggingface.co (exact)
  - Together.ai: api.together.xyz (exact)
  - Replicate: api.replicate.com (exact)
- [ ] Validate JSON syntax
- [ ] **Tests**: No direct tests for JSON file, but integration tests will load it

### 4.2 Create DenylistProvider Class
- [ ] **File**: `src/Acode.Infrastructure/Network/DenylistProvider.cs`
- [ ] Class with methods:
  - `IReadOnlyList<EndpointPattern> LoadFromFile(string path)`
  - `IReadOnlyList<EndpointPattern> GetBuiltInDenylist()` (fallback)
- [ ] Parse JSON into EndpointPattern objects
- [ ] Validate pattern type is valid
- [ ] Compile regex patterns immediately (per NFR-001b-23)
- [ ] Handle file not found gracefully (use built-in)
- [ ] Handle invalid JSON gracefully (use built-in)
- [ ] **Tests**: `tests/Acode.Infrastructure.Tests/Network/DenylistProviderTests.cs`
  - Test loading valid JSON file
  - Test pattern count matches expected
  - Test pattern types parsed correctly
  - Test regex patterns compiled
  - Test file not found falls back to built-in
  - Test invalid JSON falls back to built-in
  - Test built-in denylist contains all major providers

**Implementation notes:**
- Satisfies FR-001b-36, FR-001b-37 (loadable, updatable)
- Satisfies NFR-001b-23 (pre-compiled regex)

---

## Phase 5: Integration and End-to-End Tests

**Goal**: Implement integration tests per spec lines 608-633

### 5.1 Integration Tests
- [ ] **File**: `tests/Acode.Integration.Tests/Validation/EndpointValidationIntegrationTests.cs`
- [ ] Test: Load denylist from file and validate against known LLM APIs
- [ ] Test: Validate OpenAI API denied in LocalOnly mode
- [ ] Test: Validate Anthropic API denied in LocalOnly mode
- [ ] Test: Validate Ollama allowed in LocalOnly mode
- [ ] Test: Validate all APIs allowed in Burst mode
- [ ] Test: Validate NO APIs allowed in Airgapped mode (even localhost)
- [ ] Test: Custom allowlist entry works
- [ ] Test: Allowlist checked before denylist
- [ ] Test: Error messages include remediation
- [ ] Test: Error messages include matched pattern

### 5.2 Performance Benchmarks (Optional but per spec)
- [ ] **File**: `tests/Acode.Infrastructure.Benchmarks/Validation/EndpointValidationBenchmarks.cs`
- [ ] Benchmark: URL validation time < 1ms (per NFR-001b-16)
- [ ] Benchmark: Denylist check time < 500μs (per NFR-001b-17)
- [ ] Benchmark: Allowlist check time < 100μs (per NFR-001b-18)
- [ ] Benchmark: Memory usage < 1MB (per NFR-001b-21)
- [ ] Use BenchmarkDotNet
- [ ] Run and document results

**Implementation notes:**
- Satisfies testing requirements (lines 580-606)
- Satisfies performance requirements (NFR-001b-16 to NFR-001b-25)

---

## Phase 6: Documentation

**Goal**: Create user-facing documentation per spec lines 290-421

### 6.1 Create endpoint-validation.md
- [ ] **File**: `docs/endpoint-validation.md`
- [ ] Section: What Counts as an External LLM API? (lines 296-309)
- [ ] Section: The Denylist (lines 311-335)
- [ ] Section: The Allowlist (lines 337-351)
- [ ] Section: Validation Checkpoints (lines 353-361)
- [ ] Section: What Happens When Blocked (lines 363-375)
- [ ] Section: Viewing Blocked Attempts (lines 377-389) [stub - actual implementation in future tasks]
- [ ] Section: Configuring Custom Allowlist (lines 391-405)
- [ ] Section: Troubleshooting (lines 407-420)
- [ ] Use exact content from spec or improve clarity
- [ ] Include code examples
- [ ] Include configuration examples

---

## Phase 7: Final Verification and Audit

**Goal**: Verify 100% completion per CLAUDE.md Section 3.4

### 7.1 Run All Tests
- [ ] Run: `dotnet test --no-build`
- [ ] Verify: ALL tests passing
- [ ] Verify: Test count matches expected (unit + integration)
- [ ] Check coverage: `dotnet test --collect:"XPlat Code Coverage"`
- [ ] Verify: Validation code has near 100% coverage

### 7.2 Scan for Gaps
- [ ] Scan: `grep -r "NotImplementedException" src/Acode.Domain/Validation/`
- [ ] Scan: `grep -r "NotImplementedException" src/Acode.Infrastructure/Network/`
- [ ] Expected result: NO MATCHES
- [ ] Scan: `grep -r "TODO\|FIXME" src/Acode.Domain/Validation/`
- [ ] Scan: `grep -r "TODO\|FIXME" src/Acode.Infrastructure/Network/`
- [ ] Expected result: NO MATCHES (or only acceptable TODOs)

### 7.3 Verify Acceptance Criteria
- [ ] Review spec Acceptance Criteria (lines 424-576)
- [ ] Check each of 135 items manually
- [ ] Document which items are met
- [ ] Verify all 25 External LLM API Definition items met
- [ ] Verify all 25 Denylist Implementation items met
- [ ] Verify all 20 Allowlist Implementation items met
- [ ] Verify all 25 Validation Checkpoints items met
- [ ] Verify all 20 Violation Response items met
- [ ] Verify all 20 Testing items met

### 7.4 Run User Verification Steps
- [ ] Follow spec User Verification Steps (lines 649-698)
- [ ] Verification 1: OpenAI blocked in LocalOnly [will need CLI - skip or stub]
- [ ] Verification 2: Ollama allowed in LocalOnly [will need CLI - skip or stub]
- [ ] Verification 3: Denylist matches patterns (unit test covers this)
- [ ] Verification 4: Allowlist shows localhost (unit test covers this)
- [ ] Other verifications require CLI integration (out of scope for 001b)

### 7.5 Build Verification
- [ ] Run: `dotnet clean`
- [ ] Run: `dotnet build`
- [ ] Expected: Build succeeded, 0 errors, 0 warnings
- [ ] Run: `dotnet build --configuration Release`
- [ ] Expected: Release build succeeded, 0 errors, 0 warnings

### 7.6 Create Audit Report
- [ ] **File**: `docs/audits/task-001b-audit.md`
- [ ] Follow `docs/AUDIT-GUIDELINES.md` checklist
- [ ] Document all files created
- [ ] Document all tests written
- [ ] Document test results
- [ ] Document acceptance criteria met
- [ ] Include evidence (grep results, test output)
- [ ] Sign off on completion

---

## Completion Criteria (ALL must be true)

- [ ] All checklist items above marked ✅
- [ ] `dotnet build` succeeds with 0 warnings, 0 errors
- [ ] `dotnet test` passes 100% (all tests)
- [ ] No `NotImplementedException` in validation code
- [ ] Test coverage for validation code >95%
- [ ] All 135 acceptance criteria from spec verified met
- [ ] Documentation created and reviewed
- [ ] Audit report created
- [ ] All commits pushed to feature branch

**Only when ALL criteria met → Task 001b is COMPLETE**

---

## Notes for Resumption (if context runs out)

If this session ends before completion:

1. Check this file to see last ✅ item
2. Review recent commits: `git log --oneline -10`
3. Run tests to verify current state: `dotnet test`
4. Continue from next unchecked item
5. Update this file as you progress

**Key files to check when resuming:**
- `src/Acode.Domain/Validation/` - domain validation types
- `src/Acode.Infrastructure/Network/` - infrastructure implementation
- `tests/.../Validation/` and `tests/.../Network/` - test files
- `data/denylist.json` - denylist data file

---

**END OF CHECKLIST**
