# Task-013b Completion Checklist: Persist Approvals + Decisions

**Status:** 📋 READY FOR IMPLEMENTATION (0% complete, all gaps identified)

**Date Created:** 2026-01-16

**Spec Reference:** docs/tasks/refined-tasks/Epic 02/task-013b-persist-approvals-decisions.md

**Total Estimated Effort:** 15-22 hours to 100% completion

---

## CRITICAL INSTRUCTIONS FOR IMPLEMENTING AGENT

### Important Notes

1. **This checklist is COMPLETE and self-contained.** You should NOT need to re-read the 2,680-line spec to implement these gaps. Every gap includes implementation code from the spec.

2. **Follow TDD strictly:** RED → GREEN → REFACTOR for every gap. Write tests FIRST, then implementation.

3. **Mark progress as you go:**
   - [ ] 🔄 When starting a gap (in_progress)
   - [x] ✅ When complete (completed)

4. **Test counts:** This task requires 40+ test methods. The spec provides complete test code you can copy/modify.

5. **All code in this checklist is from the spec's Implementation Prompt and Testing Requirements sections.** Do not guess or improvise; follow spec exactly.

6. **Commit after each gap completes** with message: `feat(task-013b): implement [GapName] - AC-XXX verified`

---

## PHASE 1: Domain Entity + Tests (2-3 hours)

### Gap 1.1: Create ApprovalRecord Domain Entity

**Current State:** ❌ MISSING

**Spec Reference:** lines 2573-2595, Acceptance Criteria AC-001-012

**What Exists:** Nothing

**What's Missing:**
- src/Acode.Domain/Approvals/ApprovalRecord.cs (sealed class with immutable properties)
- ULID ID generation
- SessionId, Category, Details, Decision, MatchedRule, UserReason, CreatedAt properties
- Create() factory method
- Property validation (description length, required fields)

**Implementation Details (from spec lines 2573-2595):**

The ApprovalRecord entity must be a sealed class with these exact properties:

```csharp
namespace Acode.Domain.Approvals;

/// <summary>
/// Immutable record of an approval decision made during a session.
/// This entity forms the basis of the audit trail and cannot be modified after creation.
/// </summary>
public sealed class ApprovalRecord
{
    // Unique identifier (ULID for time-ordering)
    public ApprovalRecordId Id { get; }

    // Session context
    public SessionId SessionId { get; }

    // Operation details
    public OperationCategory Category { get; }
    public string OperationDescription { get; }  // Max 1000 chars
    public string? OperationPath { get; }        // For file operations
    public JsonDocument Details { get; }         // Extensible operation context

    // Decision
    public ApprovalDecision Decision { get; }
    public string? UserReason { get; }           // Why user denied/skipped
    public string? MatchedRule { get; }          // Which rule determined decision

    // Timing & Integrity
    public DateTimeOffset DecidedAt { get; }
    public TimeSpan DecisionDuration { get; }
    public string? Signature { get; }            // HMAC for tamper detection
    public DateTimeOffset CreatedAt { get; }
    public int Version { get; }

    // Factory method for creation
    public static ApprovalRecord Create(
        SessionId sessionId,
        OperationCategory category,
        string description,
        ApprovalDecision decision,
        string matchedRule,
        string? reason = null,
        string? path = null,
        JsonDocument? details = null)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty");

        if (description.Length > 1000)
            description = description[..1000];

        // Create record with generated ID
        return new ApprovalRecord(
            id: ApprovalRecordId.New(),
            sessionId: sessionId,
            category: category,
            operationDescription: description.Trim(),
            decision: decision,
            matchedRule: matchedRule,
            userReason: reason,
            operationPath: path,
            details: details ?? JsonDocument.Parse("{}"),
            decisionDuration: TimeSpan.Zero,
            decidedAt: DateTimeOffset.UtcNow,
            createdAt: DateTimeOffset.UtcNow,
            signature: null,
            version: 1);
    }

    // Constructor (private, only Create() factory should construct)
    private ApprovalRecord(
        ApprovalRecordId id,
        SessionId sessionId,
        OperationCategory category,
        string operationDescription,
        ApprovalDecision decision,
        string matchedRule,
        string? userReason,
        string? operationPath,
        JsonDocument details,
        TimeSpan decisionDuration,
        DateTimeOffset decidedAt,
        DateTimeOffset createdAt,
        string? signature,
        int version)
    {
        Id = id;
        SessionId = sessionId;
        Category = category;
        OperationDescription = operationDescription;
        Decision = decision;
        MatchedRule = matchedRule;
        UserReason = userReason;
        OperationPath = operationPath;
        Details = details;
        DecisionDuration = decisionDuration;
        DecidedAt = decidedAt;
        CreatedAt = createdAt;
        Signature = signature;
        Version = version;
    }
}

// Required value objects (assume these exist or create them)
public record ApprovalRecordId(string Value)
{
    public static ApprovalRecordId New() => new(Ulid.NewUlid().ToString());
    public override string ToString() => Value;
}
```

**Additional Requirements:**
- ApprovalRecord must be immutable (no setters, all properties init-only or auto-property get)
- Description is max 1000 characters (spec AC-004)
- SessionId, Category, Decision, MatchedRule are required (non-nullable)
- UserReason, OperationPath, Details are optional (nullable)
- DecidedAt should be UTC with millisecond precision (spec AC-008)
- Signature field for HMAC (spec AC-012)
- Version field for schema migration (spec AC-001)

**Acceptance Criteria Covered:** AC-001, AC-002, AC-003, AC-004, AC-005, AC-006, AC-007, AC-008, AC-009, AC-010, AC-011, AC-012

**Test Requirements:**
```csharp
// From spec lines 2015-2091
// 6 test methods needed:
1. Should_Create_Valid_Record
2. Should_Reject_Null_Or_Empty_Description
3. Should_Truncate_Description_Over_1000_Chars
4. Should_Generate_Unique_Ids
5. Should_Enforce_Immutability
6. Should_Store_All_Decision_Types
```

**Success Criteria:**
- [ ] ApprovalRecord.cs created in src/Acode.Domain/Approvals/
- [ ] Class is sealed and immutable (no setters)
- [ ] All 12 properties present with correct types
- [ ] Create() factory method validates inputs
- [ ] Description truncated to max 1000 chars
- [ ] ULID ID generation works
- [ ] Constructor is private (cannot construct directly)
- [ ] ApprovalRecordTests.cs created with 6 tests
- [ ] All 6 tests passing
- [ ] No NotImplementedException
- [ ] Build succeeds with 0 warnings

**Gap Checklist Item:**
- [ ] 🔄 Phase 1.1: ApprovalRecord entity complete with tests passing

---

### Gap 1.2: Create Enums and Value Objects

**Current State:** ❌ MISSING

**Spec Reference:** lines 1782-1817, Acceptance Criteria AC-003, AC-007

**What's Missing:**
- ApprovalDecision enum (Approved, Denied, Skipped, Timeout, AutoApproved)
- OperationCategory enum (FileRead, FileWrite, FileDelete, DirectoryCreate, TerminalCommand, etc.)
- SessionId value object (wrapper around Guid)

**Implementation Details (from spec lines 1782-1817):**

Create these enums and value objects:

```csharp
namespace Acode.Domain.Approvals;

/// <summary>
/// The decision made on an approval request.
/// </summary>
public enum ApprovalDecision
{
    Approved = 1,
    Denied = 2,
    Skipped = 3,
    Timeout = 4,
    AutoApproved = 5
}

/// <summary>
/// Category of operation requiring approval.
/// </summary>
public enum OperationCategory
{
    FileRead = 1,
    FileWrite = 2,
    FileDelete = 3,
    DirectoryCreate = 4,
    TerminalCommand = 5,
    ExternalRequest = 6
}

/// <summary>
/// Session ID value object.
/// </summary>
public record SessionId(Guid Value)
{
    public static SessionId New() => new(Guid.NewGuid());
    public static SessionId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}
```

**Acceptance Criteria Covered:** AC-003 (OperationCategory), AC-007 (Decision enum), AC-013-017 (all decision types persist)

**Test Requirements:** None (enums are self-evident)

**Success Criteria:**
- [ ] Enums defined with exact values from spec
- [ ] SessionId value object has New() and Parse() methods
- [ ] All enum values match spec (AC-013-017)
- [ ] Compiles without warnings

**Gap Checklist Item:**
- [ ] 🔄 Phase 1.2: Enums and value objects created

---

## PHASE 2: Repository Interface + Models (1-2 hours)

### Gap 2.1: Create IApprovalRecordRepository Interface

**Current State:** ❌ MISSING

**Spec Reference:** lines 2600-2611, Acceptance Criteria AC-030-038, AC-053

**What's Missing:**
- src/Acode.Application/Approvals/Persistence/IApprovalRecordRepository.cs
- All 6 repository methods
- Return types: ApprovalRecordId, PagedResult<ApprovalRecord>, ApprovalAggregation

**Implementation Details (from spec lines 2600-2611):**

```csharp
namespace Acode.Application.Approvals.Persistence;

/// <summary>
/// Repository interface for approval record persistence.
/// Implemented by both SQLite (local) and PostgreSQL (remote) providers.
/// </summary>
public interface IApprovalRecordRepository
{
    /// <summary>
    /// Creates and persists a new approval record.
    /// Record is atomically written and signature is computed before persistence.
    /// </summary>
    Task<ApprovalRecordId> CreateAsync(ApprovalRecord record, CancellationToken ct);

    /// <summary>
    /// Retrieves a single record by ID.
    /// </summary>
    Task<ApprovalRecord?> GetByIdAsync(ApprovalRecordId id, CancellationToken ct);

    /// <summary>
    /// Retrieves all records for a session in chronological order.
    /// </summary>
    Task<List<ApprovalRecord>> GetBySessionIdAsync(SessionId sessionId, CancellationToken ct);

    /// <summary>
    /// Queries records with filtering, sorting, and pagination.
    /// Supports multiple filter combinations with AND logic (spec AC-037).
    /// </summary>
    Task<PagedResult<ApprovalRecord>> QueryAsync(ApprovalRecordQuery query, CancellationToken ct);

    /// <summary>
    /// Aggregates records for statistics.
    /// Returns counts grouped by decision, category, rule, day, and duration stats.
    /// </summary>
    Task<ApprovalAggregation> AggregateAsync(ApprovalRecordQuery query, CancellationToken ct);

    /// <summary>
    /// Deletes records expired according to retention policy.
    /// Returns count of deleted records (spec AC-071).
    /// </summary>
    Task<int> DeleteExpiredAsync(DateTimeOffset before, CancellationToken ct);

    /// <summary>
    /// Soft-deletes all records for a session.
    /// Returns count of deleted records.
    /// </summary>
    Task<int> DeleteBySessionAsync(SessionId sessionId, CancellationToken ct);
}

/// <summary>
/// Paginated query results with metadata for pagination controls.
/// </summary>
public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasNextPage);

/// <summary>
/// Query parameters for filtering, sorting, and pagination.
/// </summary>
public record ApprovalRecordQuery(
    SessionId? SessionId = null,
    ApprovalDecision? Decision = null,
    OperationCategory? Category = null,
    DateTimeOffset? StartTime = null,
    DateTimeOffset? EndTime = null,
    string? MatchedRuleName = null,
    string? PathPattern = null,
    int PageSize = 50,
    int Page = 1,
    OrderByColumn OrderBy = OrderByColumn.DecidedAt,
    OrderDirection OrderDirection = OrderDirection.Descending)
{
    // Validation
    public void Validate()
    {
        if (PageSize < 1 || PageSize > 1000)
            throw new ArgumentException("PageSize must be 1-1000");
        if (Page < 1)
            throw new ArgumentException("Page must be >= 1");
    }
}

public enum OrderByColumn
{
    DecidedAt,
    SessionId,
    Decision
}

public enum OrderDirection
{
    Ascending,
    Descending
}

/// <summary>
/// Aggregation results for statistics (spec AC-048-052).
/// </summary>
public record ApprovalAggregation(
    Dictionary<ApprovalDecision, int> CountByDecision,
    Dictionary<OperationCategory, int> CountByCategory,
    Dictionary<string, int> CountByRule,
    Dictionary<DateTime, int> CountByDay,
    DurationStats DurationStats);

public record DurationStats(
    TimeSpan MinDuration,
    TimeSpan MaxDuration,
    TimeSpan AvgDuration);
```

**Acceptance Criteria Covered:** AC-030-038 (Query operations), AC-053-054 (SQLite/PostgreSQL interface)

**Test Requirements:** Contract tests in IApprovalRecordRepositoryTests.cs (12 tests) - write these after creating the interface

**Success Criteria:**
- [ ] IApprovalRecordRepository created in src/Acode.Application/Approvals/Persistence/
- [ ] All 6 methods defined with correct signatures
- [ ] Return types match spec exactly
- [ ] ApprovalRecordQuery record has all properties (SessionId, Decision, Category, StartTime, EndTime, MatchedRuleName, PathPattern, PageSize, Page, OrderBy)
- [ ] PageSize defaults to 50, validates 1-1000
- [ ] Page defaults to 1, must be >= 1
- [ ] OrderByColumn enum has DecidedAt, SessionId, Decision
- [ ] OrderDirection enum has Ascending, Descending
- [ ] ApprovalAggregation has all stats (CountByDecision, CountByCategory, CountByRule, CountByDay, DurationStats)
- [ ] Compiles without errors

**Gap Checklist Item:**
- [ ] 🔄 Phase 2.1: IApprovalRecordRepository interface created

---

### Gap 2.2: Write Repository Contract Tests

**Current State:** ❌ MISSING

**Spec Reference:** lines 2015-2315, Testing Requirements section

**What's Missing:**
- tests/Acode.Application.Tests/Approvals/Persistence/IApprovalRecordRepositoryTests.cs
- 12 contract test methods
- Tests that verify both SQLite and PostgreSQL implementations meet the contract

**Implementation Details (test framework):**

Write 12 test methods in ApprovalRecordRepositoryTests abstract base class:

```csharp
// 12 test methods following spec patterns:
1. Should_Create_And_Retrieve_Record (AC-018, AC-030)
2. Should_Query_By_Session (AC-031)
3. Should_Query_By_Decision (AC-032)
4. Should_Query_By_Category (AC-033)
5. Should_Query_By_Time_Range (AC-034)
6. Should_Query_By_Matched_Rule (AC-035)
7. Should_Query_By_Path_Pattern (AC-036)
8. Should_Support_Multiple_Filters (AC-037)
9. Should_Paginate_Results (AC-039-043)
10. Should_Aggregate_By_Decision (AC-048)
11. Should_Count_By_Category (AC-049)
12. Should_Delete_Expired_Records (AC-071)
```

**Acceptance Criteria Covered:** AC-030-038 (all query operations), AC-048-052 (aggregation), AC-071 (cleanup)

**Test Requirements:** RED state - these tests will fail initially until implementations added

**Success Criteria:**
- [ ] IApprovalRecordRepositoryTests.cs created with 12 test methods
- [ ] Tests use abstract TestFixture for both SQLite and PostgreSQL
- [ ] All tests written before implementation (RED state)
- [ ] Tests verify exact behavior from AC (not just "test passes")
- [ ] No NotImplementedException (except in implementations being tested)

**Gap Checklist Item:**
- [ ] 🔄 Phase 2.2: Repository contract tests written (RED state)

---

## PHASE 3: Supporting Services (2-3 hours)

### Gap 3.1: Supporting Security and Utility Services

**Current State:** ⚠️ PARTIALLY EXISTS (need to verify and integrate)

**Spec Reference:** lines 2164-2315 (test examples), Acceptance Criteria AC-063-068

**What Might Exist:**
- RecordSanitizer (redacts passwords, API keys, tokens) - may exist in redaction module
- ApprovalStorageGuard (enforces session/daily limits)
- SafeQueryBuilder (prevents SQL injection)
- DurationAnalyzer (differential privacy for stats)
- ApprovalRecordIntegrityVerifier (HMAC verification)

**What's Missing (if not already implemented):**
- Verification that services exist and are tested
- Integration with ApprovalRecord persistence
- Verification of HMAC signature computation
- Verification of query parameterization

**Implementation Guidance (from spec patterns):**

If these don't exist, create them:

```csharp
// RecordSanitizer - redact sensitive patterns
// Pattern: Regex match for password=, api_key=, Bearer, mysql -u -p, etc.
// Result: [REDACTED] in place of matched values

// ApprovalStorageGuard - check session/daily limits
// Pattern: Query count of recent records for session
// Reject if exceeds max (default: 10,000 per session, 100,000 per day)

// SafeQueryBuilder - parameterize all queries
// Pattern: Replace user inputs with @ParameterName placeholders
// Result: SQL with SqlParameters, never raw user strings

// DurationAnalyzer - privacy-preserving duration stats
// Pattern: Quantize durations to prevent exact identification
// Result: Min, Max, Avg with precision loss

// ApprovalRecordIntegrityVerifier - HMAC verification
// Pattern: Compute HMAC-SHA256 over record content
// Verify signature matches on record read
// Flag if tampering detected
```

**Acceptance Criteria Covered:** AC-063-068 (security requirements)

**Test Requirements:** Write tests for each service (from spec lines 2094-2315)

**Success Criteria:**
- [ ] All 5 security services exist and are integrated
- [ ] RecordSanitizer tested with password, API key, token patterns
- [ ] ApprovalStorageGuard tested with session/daily limits
- [ ] SafeQueryBuilder tested with malicious SQL patterns
- [ ] DurationAnalyzer tested for privacy preservation
- [ ] ApprovalRecordIntegrityVerifier tested with valid/tampered records
- [ ] Services are injected into repositories
- [ ] All tests passing

**Gap Checklist Item:**
- [ ] 🔄 Phase 3.1: Security and utility services verified/implemented

---

### Gap 3.2: Write Query Builder Tests

**Current State:** ❌ MISSING

**Spec Reference:** lines 2261-2315, Acceptance Criteria AC-067

**What's Missing:**
- tests/Acode.Infrastructure.Tests/Persistence/Approvals/QueryBuilderTests.cs
- 8 test methods for SafeQueryBuilder
- SQL injection prevention verification

**Implementation Details (from spec):**

```csharp
// 8 test methods
1. Should_Build_Parameterized_Query (AC-037)
2. Should_Prevent_Sql_Injection_With_Semicolon (AC-067)
3. Should_Prevent_Sql_Injection_With_Comment (AC-067)
4. Should_Prevent_Sql_Injection_With_Script (AC-067)
5. Should_Convert_Glob_Pattern_To_Like (AC-036)
6. Should_Handle_Null_Filters_Gracefully (AC-037)
7. Should_Apply_Pagination_Correctly (AC-039-043)
8. Should_Support_Order_By_With_Enum (AC-046-047)

// Test patterns from spec lines 2270-2315
[Fact]
public void Should_Build_Parameterized_Query()
{
    // Query with SessionId + Decision filters
    // Assert: SQL contains "@SessionId" and "@Decision"
    // Assert: Query parameters populated with values (not in SQL string)
}

[Theory]
[InlineData("'; DROP TABLE --")]
[InlineData("<script>alert(1)</script>")]
[InlineData("${malicious}")]
public void Should_Reject_Malicious_Path_Pattern(string pattern)
{
    // Assert: QueryValidationException thrown
    // Pattern never reaches SQL layer
}
```

**Acceptance Criteria Covered:** AC-067 (SQL injection prevention), AC-037 (parameterized queries)

**Test Requirements:** 8 test methods with parametrized test cases for SQL injection

**Success Criteria:**
- [ ] QueryBuilderTests.cs created
- [ ] 8 test methods written
- [ ] All malicious patterns rejected (AC-067)
- [ ] Parameterization verified
- [ ] All tests passing

**Gap Checklist Item:**
- [ ] 🔄 Phase 3.2: Query builder tests written and passing

---

## PHASE 4: SQLite Repository Implementation (4-6 hours)

### Gap 4.1: Implement SqliteApprovalRecordRepository

**Current State:** ❌ MISSING

**Spec Reference:** lines 2546-2680, Acceptance Criteria AC-018-025, AC-030-052, AC-053-057, AC-080-083

**What's Missing:**
- src/Acode.Infrastructure/Persistence/Approvals/SqliteApprovalRecordRepository.cs (~350 lines)
- CreateAsync with HMAC signature and RecordSanitizer integration
- QueryAsync with pagination and filtering using SafeQueryBuilder
- AggregateAsync with SQL aggregation functions
- DeleteExpiredAsync with retention logic
- WAL mode configuration

**Implementation Details (from spec structure):**

```csharp
namespace Acode.Infrastructure.Persistence.Approvals;

/// <summary>
/// SQLite implementation of IApprovalRecordRepository.
/// Provides fast local persistence with WAL mode for concurrent access.
/// </summary>
public class SqliteApprovalRecordRepository : IApprovalRecordRepository
{
    private readonly IDbConnection _connection;
    private readonly IApprovalRecordIntegrityVerifier _verifier;
    private readonly IRecordSanitizer _sanitizer;
    private readonly IApprovalStorageGuard _guard;
    private readonly ISafeQueryBuilder _queryBuilder;
    private readonly ILogger<SqliteApprovalRecordRepository> _logger;

    public SqliteApprovalRecordRepository(
        IDbConnection connection,
        IApprovalRecordIntegrityVerifier verifier,
        IRecordSanitizer sanitizer,
        IApprovalStorageGuard guard,
        ISafeQueryBuilder queryBuilder,
        ILogger<SqliteApprovalRecordRepository> logger)
    {
        _connection = connection;
        _verifier = verifier;
        _sanitizer = sanitizer;
        _guard = guard;
        _queryBuilder = queryBuilder;
        _logger = logger;
    }

    public async Task<ApprovalRecordId> CreateAsync(ApprovalRecord record, CancellationToken ct)
    {
        // AC-022: Validate storage limits
        var guardResult = await _guard.CanStoreAsync(record.SessionId, ct);
        if (!guardResult.IsAllowed)
            throw new StorageLimitExceededException(guardResult.RejectionReason);

        // AC-021: Sanitize sensitive data
        var sanitized = _sanitizer.Sanitize(record);

        // AC-020: Compute HMAC signature
        var signature = _verifier.ComputeSignature(sanitized);

        // AC-018: Atomic write to SQLite
        using (var transaction = _connection.BeginTransaction())
        {
            try
            {
                const string sql = @"
                    INSERT INTO approval_records (
                        id, session_id, category, description, path, details,
                        decision, reason, matched_rule, decided_at, duration_ms,
                        signature, created_at, version, is_deleted)
                    VALUES (
                        @Id, @SessionId, @Category, @Description, @Path, @Details,
                        @Decision, @Reason, @Rule, @DecidedAt, @Duration,
                        @Signature, @CreatedAt, @Version, @IsDeleted)
                    RETURNING id";

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Transaction = transaction;

                    // Add parameters (not embedded strings for injection prevention)
                    cmd.Parameters.AddWithValue("@Id", record.Id.Value);
                    cmd.Parameters.AddWithValue("@SessionId", record.SessionId.Value);
                    cmd.Parameters.AddWithValue("@Category", (int)record.Category);
                    cmd.Parameters.AddWithValue("@Description", sanitized.OperationDescription);
                    cmd.Parameters.AddWithValue("@Path", record.OperationPath ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Details", record.Details.RootElement.GetRawText());
                    cmd.Parameters.AddWithValue("@Decision", (int)record.Decision);
                    cmd.Parameters.AddWithValue("@Reason", record.UserReason ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Rule", record.MatchedRule ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DecidedAt", record.DecidedAt.UtcTicks);
                    cmd.Parameters.AddWithValue("@Duration", (int)record.DecisionDuration.TotalMilliseconds);
                    cmd.Parameters.AddWithValue("@Signature", signature);
                    cmd.Parameters.AddWithValue("@CreatedAt", record.CreatedAt.UtcTicks);
                    cmd.Parameters.AddWithValue("@Version", record.Version);
                    cmd.Parameters.AddWithValue("@IsDeleted", false);

                    var id = await cmd.ExecuteScalarAsync(ct);

                    transaction.Commit();

                    // AC-025: Log record creation
                    _logger.LogInformation("Approval record created: {RecordId} for session {SessionId}",
                        id, record.SessionId);

                    return new ApprovalRecordId(id.ToString()!);
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Failed to create approval record for session {SessionId}",
                    record.SessionId);
                throw;
            }
        }
    }

    public async Task<ApprovalRecord?> GetByIdAsync(ApprovalRecordId id, CancellationToken ct)
    {
        const string sql = "SELECT * FROM approval_records WHERE id = @Id AND is_deleted = 0";

        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@Id", id.Value);

            using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                if (await reader.ReadAsync(ct))
                {
                    return MapRecord(reader);
                }
            }
        }

        return null;
    }

    public async Task<List<ApprovalRecord>> GetBySessionIdAsync(SessionId sessionId, CancellationToken ct)
    {
        // AC-031: Get all records for session in chronological order
        const string sql = @"
            SELECT * FROM approval_records
            WHERE session_id = @SessionId AND is_deleted = 0
            ORDER BY decided_at ASC";

        var records = new List<ApprovalRecord>();
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@SessionId", sessionId.Value);

            using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    records.Add(MapRecord(reader));
                }
            }
        }

        return records;
    }

    public async Task<PagedResult<ApprovalRecord>> QueryAsync(ApprovalRecordQuery query, CancellationToken ct)
    {
        // AC-037-038: Build parameterized query with SafeQueryBuilder
        query.Validate();
        var (sql, parameters) = _queryBuilder.Build(query);

        // Count total records for pagination
        var countSql = $"SELECT COUNT(*) FROM approval_records WHERE {sql}";
        var totalCount = (int)await ExecuteScalarAsync(countSql, parameters, ct);

        // Get paginated results
        // AC-039: Default page size 50, AC-041: pages start at 1
        var offset = (query.Page - 1) * query.PageSize;
        var paginatedSql = $@"
            {sql}
            ORDER BY {GetOrderColumn(query.OrderBy)} {GetOrderDirection(query.OrderDirection)}
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("@PageSize", query.PageSize);
        parameters.Add("@Offset", offset);

        var items = new List<ApprovalRecord>();
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = paginatedSql;
            foreach (var (key, value) in parameters)
            {
                cmd.Parameters.AddWithValue(key, value);
            }

            using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    items.Add(MapRecord(reader));
                }
            }
        }

        // AC-042: Include HasNextPage
        var hasNextPage = (query.Page * query.PageSize) < totalCount;

        return new PagedResult<ApprovalRecord>(items, totalCount, query.Page, query.PageSize, hasNextPage);
    }

    public async Task<ApprovalAggregation> AggregateAsync(ApprovalRecordQuery query, CancellationToken ct)
    {
        // AC-048-052: Aggregate by decision, category, rule, day, duration
        query.Validate();
        var (whereSql, parameters) = _queryBuilder.Build(query);

        // Count by decision
        var countByDecision = new Dictionary<ApprovalDecision, int>();
        var decisionSql = $@"
            SELECT decision, COUNT(*) as count
            FROM approval_records
            WHERE {whereSql} AND is_deleted = 0
            GROUP BY decision";
        // ... execute and populate

        // Count by category
        var countByCategory = new Dictionary<OperationCategory, int>();
        var categorySql = $@"
            SELECT category, COUNT(*) as count
            FROM approval_records
            WHERE {whereSql} AND is_deleted = 0
            GROUP BY category";
        // ... execute and populate

        // Count by rule
        var countByRule = new Dictionary<string, int>();
        var ruleSql = $@"
            SELECT matched_rule, COUNT(*) as count
            FROM approval_records
            WHERE {whereSql} AND is_deleted = 0 AND matched_rule IS NOT NULL
            GROUP BY matched_rule";
        // ... execute and populate

        // Count by day
        var countByDay = new Dictionary<DateTime, int>();
        var daySql = $@"
            SELECT DATE(decided_at) as day, COUNT(*) as count
            FROM approval_records
            WHERE {whereSql} AND is_deleted = 0
            GROUP BY DATE(decided_at)";
        // ... execute and populate

        // Duration stats with DurationAnalyzer
        var durationSql = $@"
            SELECT
                MIN(duration_ms) as min_duration,
                MAX(duration_ms) as max_duration,
                AVG(duration_ms) as avg_duration
            FROM approval_records
            WHERE {whereSql} AND is_deleted = 0";
        // ... execute and apply DurationAnalyzer

        return new ApprovalAggregation(
            countByDecision, countByCategory, countByRule, countByDay, durationStats);
    }

    public async Task<int> DeleteExpiredAsync(DateTimeOffset before, CancellationToken ct)
    {
        // AC-071: Delete records before retention date
        const string sql = @"
            DELETE FROM approval_records
            WHERE created_at < @Before";

        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@Before", before.UtcTicks);

            var rowsDeleted = await cmd.ExecuteNonQueryAsync(ct);
            _logger.LogInformation("Deleted {RowCount} expired approval records", rowsDeleted);
            return rowsDeleted;
        }
    }

    public async Task<int> DeleteBySessionAsync(SessionId sessionId, CancellationToken ct)
    {
        // AC-028: Soft delete (mark IsDeleted = true)
        const string sql = @"
            UPDATE approval_records
            SET is_deleted = 1
            WHERE session_id = @SessionId AND is_deleted = 0";

        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@SessionId", sessionId.Value);

            return await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    // Helper methods
    private ApprovalRecord MapRecord(IDataReader reader)
    {
        // Map database row to ApprovalRecord entity
        // Handle ULID parsing, DateTimeOffset reconstruction, etc.
    }

    private string GetOrderColumn(OrderByColumn col) => col switch
    {
        OrderByColumn.DecidedAt => "decided_at",
        OrderByColumn.SessionId => "session_id",
        OrderByColumn.Decision => "decision",
        _ => "decided_at"
    };

    private string GetOrderDirection(OrderDirection dir) => dir switch
    {
        OrderDirection.Ascending => "ASC",
        OrderDirection.Descending => "DESC",
        _ => "DESC"
    };

    private async Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, CancellationToken ct)
    {
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = sql;
            foreach (var (key, value) in parameters)
            {
                cmd.Parameters.AddWithValue(key, value);
            }
            return await cmd.ExecuteScalarAsync(ct) ?? 0;
        }
    }
}
```

**Key Requirements from Spec:**
- AC-080: Record creation < 10ms
- AC-081: Query < 50ms for 10,000 records
- AC-082: Pagination < 100ms any page
- AC-083: Storage guard check < 1ms
- AC-054: WAL mode enabled for concurrent access
- AC-019: Atomic writes
- AC-021: Sanitization before persistence
- AC-020: HMAC signature computation

**Acceptance Criteria Covered:** AC-018-025 (create), AC-030-038 (query), AC-048-052 (aggregation), AC-053-054 (SQLite WAL), AC-063-068 (security), AC-080-083 (performance)

**Test Requirements:** Write SqliteApprovalRepositoryIntegrationTests.cs (8 tests) - tests that verify actual SQLite operations

**Success Criteria:**
- [ ] SqliteApprovalRecordRepository.cs created in src/Acode.Infrastructure/Persistence/Approvals/
- [ ] All 7 methods implemented (Create, GetById, GetBySession, Query, Aggregate, DeleteExpired, DeleteBySession)
- [ ] All parameters are parameterized (no string interpolation)
- [ ] HMAC signature computed before insert
- [ ] Sanitization applied
- [ ] Storage guard checked
- [ ] Pagination with correct offset/limit calculation
- [ ] Aggregation queries group correctly by decision/category/rule/day
- [ ] DeleteExpired removes records before date
- [ ] DeleteBySession marks as_deleted = true (soft delete)
- [ ] All AC requirements met
- [ ] Build succeeds
- [ ] All 8 integration tests passing

**Gap Checklist Item:**
- [ ] 🔄 Phase 4.1: SqliteApprovalRecordRepository implemented and all tests passing

---

### Gap 4.2: Database Schema Creation

**Current State:** ❌ MISSING

**Spec Reference:** Implicit in AC-053-054

**What's Missing:**
- SQL migration/schema for approval_records table in SQLite
- Columns: id, session_id, category, description, path, details, decision, reason, matched_rule, decided_at, duration_ms, signature, created_at, version, is_deleted
- Indexes on frequently queried columns (session_id, decided_at, decision, category)
- WAL mode enabled

**Implementation Details:**

Create a migration that creates the schema:

```sql
-- Enable WAL mode for concurrent access
PRAGMA journal_mode = WAL;

-- Create table
CREATE TABLE approval_records (
    id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    category INTEGER NOT NULL,
    description TEXT NOT NULL,
    path TEXT,
    details TEXT, -- JSON
    decision INTEGER NOT NULL,
    reason TEXT,
    matched_rule TEXT,
    decided_at INTEGER NOT NULL, -- UTC ticks
    duration_ms INTEGER NOT NULL,
    signature TEXT NOT NULL,
    created_at INTEGER NOT NULL, -- UTC ticks
    version INTEGER NOT NULL DEFAULT 1,
    is_deleted INTEGER NOT NULL DEFAULT 0
);

-- Create indexes for common queries
CREATE INDEX idx_approval_records_session_id ON approval_records(session_id);
CREATE INDEX idx_approval_records_decided_at ON approval_records(decided_at);
CREATE INDEX idx_approval_records_decision ON approval_records(decision);
CREATE INDEX idx_approval_records_category ON approval_records(category);
CREATE INDEX idx_approval_records_created_at ON approval_records(created_at);
CREATE INDEX idx_approval_records_is_deleted ON approval_records(is_deleted);
```

**Success Criteria:**
- [ ] Migration creates approval_records table
- [ ] All columns present with correct types
- [ ] Primary key is id (ULID)
- [ ] Foreign key to sessions (session_id)
- [ ] Indexes created on session_id, decided_at, decision, category, created_at, is_deleted
- [ ] WAL mode enabled
- [ ] Migration runs without errors
- [ ] Existing tests can insert/query

**Gap Checklist Item:**
- [ ] 🔄 Phase 4.2: Database schema and migrations created

---

## PHASE 5: PostgreSQL + Sync (4-5 hours)

### Gap 5.1: Implement PostgresApprovalRecordRepository

**Current State:** ❌ MISSING

**Spec Reference:** Lines 2556-2566, Acceptance Criteria AC-053-057

**What's Missing:**
- src/Acode.Infrastructure/Persistence/Approvals/PostgresApprovalRecordRepository.cs (~300 lines)
- Identical interface to SQLite but with PostgreSQL syntax
- Connection pooling configuration
- Support for remote sync

**Implementation Details:**

Create PostgreSQL implementation with:
- Same IApprovalRecordRepository interface
- Same method signatures
- PostgreSQL-specific SQL (RETURNING, OFFSET/FETCH NEXT, etc.)
- Connection string from config
- Connection pooling setup
- All parameterized queries (NpgsqlParameter)

**Key Differences from SQLite:**
- Use `OFFSET x FETCH NEXT y ROWS ONLY` instead of `LIMIT y OFFSET x`
- Use `RETURNING id` instead of `RETURNING id`
- Connection pooling via NpgsqlDataSource
- PostgreSQL JSON support
- Aggregate functions syntax

**Acceptance Criteria Covered:** AC-053-057 (PostgreSQL repository with connection pooling)

**Test Requirements:** Write SyncServiceIntegrationTests.cs to test sync between SQLite → PostgreSQL

**Success Criteria:**
- [ ] PostgresApprovalRecordRepository.cs created
- [ ] All 7 methods implemented (same as SQLite)
- [ ] Connection pooling configured (min 5, max 20)
- [ ] All queries use NpgsqlParameter (SQL injection prevention)
- [ ] RETURNING clause used correctly
- [ ] OFFSET/FETCH NEXT pagination correct
- [ ] Aggregation functions work (PostgreSQL syntax)
- [ ] Compiles without errors
- [ ] Can be injected instead of SQLite for testing

**Gap Checklist Item:**
- [ ] 🔄 Phase 5.1: PostgresApprovalRecordRepository implemented

---

### Gap 5.2: Implement ApprovalRecordSyncService

**Current State:** ❌ MISSING

**Spec Reference:** Lines 129-145, Acceptance Criteria AC-058-062

**What's Missing:**
- src/Acode.Infrastructure/Persistence/Approvals/ApprovalRecordSyncService.cs (~150 lines)
- Outbox polling for pending records
- Batch sync to PostgreSQL
- Exponential backoff retry (max 5 attempts)
- "Latest wins" conflict resolution
- Status update and logging

**Implementation Details (from spec):**

```csharp
namespace Acode.Infrastructure.Persistence.Approvals;

/// <summary>
/// Syncs approval records from SQLite local database to PostgreSQL remote.
/// Uses outbox pattern for reliable delivery with retry logic.
/// </summary>
public class ApprovalRecordSyncService
{
    private readonly IApprovalRecordRepository _localRepository;
    private readonly IApprovalRecordRepository _remoteRepository;
    private readonly ILogger<ApprovalRecordSyncService> _logger;
    private readonly SyncOptions _options;

    public ApprovalRecordSyncService(
        IApprovalRecordRepository localRepository,
        IApprovalRecordRepository remoteRepository,
        IOptions<SyncOptions> options,
        ILogger<ApprovalRecordSyncService> logger)
    {
        _localRepository = localRepository;
        _remoteRepository = remoteRepository;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Syncs pending records to remote in batches.
    /// AC-059: Polls outbox table for pending records
    /// AC-060: Retries failed syncs with exponential backoff
    /// AC-062: Uses "latest wins" conflict resolution
    /// </summary>
    public async Task SyncPendingAsync(CancellationToken ct)
    {
        try
        {
            // Get pending records from outbox (AC-058)
            var pending = await GetPendingOutboxEntriesAsync(ct);

            if (!pending.Any())
                return;

            // Batch sync (e.g., 100 records per batch)
            var batches = pending
                .Chunk(100)
                .ToList();

            foreach (var batch in batches)
            {
                await SyncBatchAsync(batch, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed");
        }
    }

    private async Task SyncBatchAsync(ApprovalRecord[] batch, CancellationToken ct)
    {
        foreach (var record in batch)
        {
            await SyncRecordWithRetryAsync(record, retryCount: 0, ct);
        }
    }

    /// <summary>
    /// Syncs single record with exponential backoff retry.
    /// AC-060: Retries with exponential backoff
    /// AC-061: Max retry attempts = 5
    /// </summary>
    private async Task SyncRecordWithRetryAsync(ApprovalRecord record, int retryCount, CancellationToken ct)
    {
        try
        {
            // Check for conflict (latest wins by DecidedAt)
            var remoteRecord = await _remoteRepository.GetByIdAsync(record.Id, ct);
            if (remoteRecord != null && remoteRecord.DecidedAt > record.DecidedAt)
            {
                // Remote is newer, skip sync
                _logger.LogInformation("Skipping sync: remote record is newer");
                return;
            }

            // Sync to remote
            await _remoteRepository.CreateAsync(record, ct);

            // Update outbox status (record synced)
            await UpdateOutboxStatusAsync(record.Id, SyncStatus.Synced, ct);

            _logger.LogInformation("Synced record {RecordId} to remote", record.Id);
        }
        catch (Exception ex) when (retryCount < _options.MaxRetryAttempts)
        {
            // Exponential backoff: 1s, 2s, 4s, 8s, 16s
            var delayMs = (int)Math.Pow(2, retryCount) * 1000;
            _logger.LogWarning(ex, "Sync failed for {RecordId}, retrying in {DelayMs}ms",
                record.Id, delayMs);

            await Task.Delay(delayMs, ct);
            await SyncRecordWithRetryAsync(record, retryCount + 1, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for {RecordId} after {Attempts} retries",
                record.Id, _options.MaxRetryAttempts);
            await UpdateOutboxStatusAsync(record.Id, SyncStatus.Failed, ct);
        }
    }

    private async Task<List<ApprovalRecord>> GetPendingOutboxEntriesAsync(CancellationToken ct)
    {
        // Query outbox table for records with SyncStatus = Pending
        // AC-058: Outbox table stores pending syncs
        // Typical query: SELECT * FROM outbox WHERE status = 'Pending' ORDER BY created_at ASC

        // For now, mock implementation
        return new List<ApprovalRecord>();
    }

    private async Task UpdateOutboxStatusAsync(ApprovalRecordId recordId, SyncStatus status, CancellationToken ct)
    {
        // Update outbox entry status
        // status = Synced, Failed, or Pending
    }
}

public enum SyncStatus
{
    Pending,
    Synced,
    Failed
}

public record SyncOptions
{
    public int MaxRetryAttempts { get; init; } = 5;
    public int BatchSize { get; init; } = 100;
    public int PollIntervalSeconds { get; init; } = 300; // 5 minutes
}
```

**Key Features:**
- AC-058: Outbox table (pending syncs)
- AC-059: Polling outbox every 5 minutes
- AC-060: Exponential backoff retry
- AC-061: Max 5 retry attempts
- AC-062: Latest wins based on DecidedAt
- Batching for efficiency
- Logging for debugging

**Acceptance Criteria Covered:** AC-058-062 (sync operations)

**Test Requirements:** SyncServiceIntegrationTests.cs (4 tests)

**Success Criteria:**
- [ ] ApprovalRecordSyncService.cs created
- [ ] SyncPendingAsync polls outbox and batches records
- [ ] SyncRecordWithRetryAsync implements exponential backoff
- [ ] Retry limit = 5 (AC-061)
- [ ] Conflict resolution uses "latest wins" (AC-062)
- [ ] Outbox status updated after sync (AC-059)
- [ ] Failed syncs logged with full details
- [ ] All 4 integration tests passing
- [ ] Compiles without errors

**Gap Checklist Item:**
- [ ] 🔄 Phase 5.2: ApprovalRecordSyncService implemented and tested

---

## PHASE 6: E2E + CLI (2-3 hours)

### Gap 6.1: Write E2E Tests

**Current State:** ❌ MISSING

**Spec Reference:** Lines 2443-2482, Acceptance Criteria AC-001-083

**What's Missing:**
- tests/Acode.Integration.Tests/Approvals/ApprovalPersistenceE2ETests.cs
- 2 full end-to-end test scenarios

**Implementation Details (from spec):**

```csharp
// From spec lines 2453-2482
[Fact]
public async Task Should_Persist_During_Session_And_Query_After()
{
    // Scenario: User approves an operation, then queries history
    // Arrange: Create session
    // Act:
    //  1. Execute operation that requires approval
    //  2. Approve via prompt
    //  3. Run "acode approvals list"
    // Assert: Record appears in history
}

[Fact]
public async Task Should_Export_To_Json()
{
    // Scenario: User exports approval history
    // Arrange: Create multiple records
    // Act: Run "acode approvals export --format json"
    // Assert: Valid JSON output with all records
}
```

**Acceptance Criteria Covered:** AC-001-083 (full system functionality)

**Test Requirements:** 2 E2E test methods covering:
1. Full approval workflow → persistence → query
2. Export functionality

**Success Criteria:**
- [ ] ApprovalPersistenceE2ETests.cs created
- [ ] 2 test methods written
- [ ] Both tests execute full workflows
- [ ] Assert on actual CLI output or database state
- [ ] Both tests passing
- [ ] Build succeeds

**Gap Checklist Item:**
- [ ] 🔄 Phase 6.1: E2E tests written and passing

---

### Gap 6.2: Implement ApprovalsCommand CLI

**Current State:** ❌ MISSING

**Spec Reference:** Lines 2568-2570, Acceptance Criteria AC-075-079

**What's Missing:**
- src/Acode.Cli/Commands/ApprovalsCommand.cs
- Five CLI commands:
  - acode approvals list (with filters and pagination)
  - acode approvals show <id>
  - acode approvals delete <id>
  - acode approvals export (JSON/CSV)
  - acode approvals stats

**Implementation Details (from spec AC-075-079):**

```csharp
[Command("approvals")]
public class ApprovalsCommand : Command
{
    private readonly IApprovalRecordRepository _repository;

    // AC-075: List command with filters and pagination
    [Command("list")]
    public async Task<int> ListAsync(
        [Option("--session")] string? sessionId = null,
        [Option("--decision")] string? decision = null,
        [Option("--category")] string? category = null,
        [Option("--page")] int page = 1,
        [Option("--size")] int pageSize = 50)
    {
        // Build query from options
        var query = new ApprovalRecordQuery(
            SessionId: sessionId != null ? new SessionId(Guid.Parse(sessionId)) : null,
            Decision: decision != null ? Enum.Parse<ApprovalDecision>(decision) : null,
            Category: category != null ? Enum.Parse<OperationCategory>(category) : null,
            Page: page,
            PageSize: pageSize);

        var result = await _repository.QueryAsync(query, CancellationToken.None);

        // Format and display paginated results
        foreach (var record in result.Items)
        {
            Console.WriteLine($"{record.Id.Value} | {record.Decision} | {record.SessionId}");
        }

        Console.WriteLine($"\nPage {result.Page} of {(result.TotalCount + result.PageSize - 1) / result.PageSize}");
        return 0;
    }

    // AC-076: Show command
    [Command("show")]
    public async Task<int> ShowAsync(string recordId)
    {
        var record = await _repository.GetByIdAsync(new ApprovalRecordId(recordId), CancellationToken.None);
        if (record == null)
        {
            Console.WriteLine("Record not found");
            return 1;
        }

        // Format and display full details
        Console.WriteLine($"ID: {record.Id}");
        Console.WriteLine($"Decision: {record.Decision}");
        Console.WriteLine($"Reason: {record.UserReason ?? "N/A"}");
        return 0;
    }

    // AC-077: Delete command (soft delete)
    [Command("delete")]
    public async Task<int> DeleteAsync(string recordId)
    {
        var record = await _repository.GetByIdAsync(new ApprovalRecordId(recordId), CancellationToken.None);
        if (record == null)
            return 1;

        // Soft delete
        await _repository.DeleteBySessionAsync(record.SessionId, CancellationToken.None);
        Console.WriteLine("Record deleted");
        return 0;
    }

    // AC-078: Export command
    [Command("export")]
    public async Task<int> ExportAsync(
        [Option("--format")] string format = "json",
        [Option("--output")] string? outputPath = null)
    {
        // Get all records
        var query = new ApprovalRecordQuery();
        var result = await _repository.QueryAsync(query, CancellationToken.None);

        string content = format.ToLower() switch
        {
            "json" => JsonSerializer.Serialize(result.Items, new JsonSerializerOptions { WriteIndented = true }),
            "csv" => ExportToCsv(result.Items),
            _ => throw new InvalidOperationException("Unsupported format")
        };

        if (outputPath != null)
            await File.WriteAllTextAsync(outputPath, content);
        else
            Console.WriteLine(content);

        return 0;
    }

    // AC-079: Stats command
    [Command("stats")]
    public async Task<int> StatsAsync()
    {
        var query = new ApprovalRecordQuery();
        var stats = await _repository.AggregateAsync(query, CancellationToken.None);

        Console.WriteLine("Approval Statistics:");
        foreach (var (decision, count) in stats.CountByDecision)
        {
            Console.WriteLine($"  {decision}: {count}");
        }

        return 0;
    }
}
```

**Acceptance Criteria Covered:** AC-075-079 (all CLI commands)

**Test Requirements:** None required (tested via E2E tests above)

**Success Criteria:**
- [ ] ApprovalsCommand.cs created in src/Acode.Cli/Commands/
- [ ] All 5 commands implemented (list, show, delete, export, stats)
- [ ] list command supports filters (--session, --decision, --category, --page, --size)
- [ ] show command displays full record details
- [ ] delete command performs soft delete
- [ ] export command supports json and csv formats
- [ ] stats command shows aggregated statistics
- [ ] All commands are registered in CLI host
- [ ] E2E tests pass using these commands
- [ ] Build succeeds

**Gap Checklist Item:**
- [ ] 🔄 Phase 6.2: ApprovalsCommand implemented and working

---

### Gap 6.3: Final Verification

**Current State:** ❌ NOT STARTED

**What's Required:**
- All 83 ACs verified complete
- All 40+ tests passing
- No NotImplementedException remaining
- Build succeeds with 0 warnings
- All phases completed and committed

**Success Criteria:**

- [ ] **File Count Verification:**
  - [ ] 7 production files exist (1 Domain + 3 Application + 3 Infrastructure)
  - [ ] 8 test files exist
  - [ ] Database schema created

- [ ] **NotImplementedException Scan (CRITICAL):**
  - [ ] grep -r "NotImplementedException" src/Acode.Domain/Approvals/ → NO MATCHES
  - [ ] grep -r "NotImplementedException" src/Acode.Application/Approvals/ → NO MATCHES
  - [ ] grep -r "NotImplementedException" src/Acode.Infrastructure/Persistence/Approvals/ → NO MATCHES
  - [ ] grep -r "NotImplementedException" tests/Acode.*/Approvals/ → NO MATCHES

- [ ] **TODO/FIXME Scan:**
  - [ ] grep -r "TODO\|FIXME" src/Acode.*/Approvals/ → NO MATCHES or only benign TODOs

- [ ] **Test Execution:**
  - [ ] dotnet test → All tests passing
  - [ ] Expected: 40+ tests passing for task-013b
  - [ ] No skipped tests

- [ ] **Build Verification:**
  - [ ] dotnet build → 0 errors, 0 warnings
  - [ ] Build duration < 60 seconds

- [ ] **AC Verification (spot check):**
  - [ ] AC-001: ULID ID exists on ApprovalRecord ✅
  - [ ] AC-018: CreateAsync persists to SQLite ✅
  - [ ] AC-037: QueryAsync supports multiple filters ✅
  - [ ] AC-048: Aggregation by decision works ✅
  - [ ] AC-067: SQL injection prevention via parameterization ✅
  - [ ] AC-075-079: All 5 CLI commands working ✅

- [ ] **Integration Test:**
  - [ ] End-to-end workflow: Create approval → Query → Export passes ✅

**Gap Checklist Item:**
- [ ] 🔄 Phase 6.3: Final verification complete, all 83 ACs verified

---

## SUMMARY TABLE

| Phase | Description | Hours | AC Coverage | Status |
|-------|-------------|-------|-------------|--------|
| 1.1 | ApprovalRecord entity + tests | 2-3h | AC-001-012 | [ ] 🔄 Not started |
| 1.2 | Enums and value objects | 0.5-1h | AC-003, AC-007 | [ ] 🔄 Not started |
| 2.1 | IApprovalRecordRepository interface | 1h | AC-053 | [ ] 🔄 Not started |
| 2.2 | Repository contract tests (RED) | 1-2h | AC-030-038, AC-048-052, AC-071 | [ ] 🔄 Not started |
| 3.1 | Supporting security services | 1-2h | AC-063-068 | [ ] 🔄 Not started |
| 3.2 | Query builder tests | 1-2h | AC-067 | [ ] 🔄 Not started |
| 4.1 | SqliteApprovalRecordRepository | 4-6h | AC-018-025, AC-030-038, AC-048-052, AC-080-083 | [ ] 🔄 Not started |
| 4.2 | Database schema + migrations | 1h | AC-053-054 | [ ] 🔄 Not started |
| 5.1 | PostgresApprovalRecordRepository | 2-3h | AC-053-057 | [ ] 🔄 Not started |
| 5.2 | ApprovalRecordSyncService | 2-3h | AC-058-062 | [ ] 🔄 Not started |
| 6.1 | E2E tests | 1-2h | AC-001-083 | [ ] 🔄 Not started |
| 6.2 | ApprovalsCommand CLI | 1-2h | AC-075-079 | [ ] 🔄 Not started |
| 6.3 | Final verification | 1h | AC-001-083 (all) | [ ] 🔄 Not started |
| **TOTAL** | **15-22 hours** | **All 83 ACs** | **0% → 100%** |

---

## GIT WORKFLOW

Commit after each gap with message format:

```
feat(task-013b): implement [GapName]

- [Gap-specific change 1]
- [Gap-specific change 2]
- Verified: AC-XXX, AC-YYY, AC-ZZZ
- Tests: X/X passing

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

Example:
```
feat(task-013b): implement ApprovalRecord entity

- Create ApprovalRecord.cs with immutable properties
- Add ULID ID generation
- Implement Create() factory method with validation
- Enforce max 1000 char description
- Verified: AC-001-012 (record structure)
- Tests: 6/6 passing (ApprovalRecordTests)

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

---

## INSTRUCTIONS FOR NEXT SESSION

If context runs out mid-task:

1. **Check this checklist** - it shows exactly where you stopped
2. **Look for the last completed gap** - shows which phase is done
3. **Review the "Gap Checklist Item"** for the next phase - shows what to implement
4. **Use the spec code examples** - all provided in "Implementation Details" sections
5. **Follow TDD** - RED → GREEN → REFACTOR
6. **Commit after each gap** - don't batch work
7. **Update this checklist** - mark completed gaps with [x] ✅

This checklist is complete and self-contained. You should NOT need the full spec to implement.

---

**Document Complete**: Ready for implementation

**Created By**: Claude Code (Established 050b Pattern)

**Quality Standard**: Based on task-049d (1,700+ line reference checklist)

**Test Coverage Target**: 40+ test methods across 8 test files

**Code Coverage Target**: All 83 ACs verified with semantic completeness

---
