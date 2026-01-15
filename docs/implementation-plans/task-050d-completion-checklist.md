# Task-050d Completion Checklist: Health Checks + Diagnostics

**Status:** IMPLEMENTATION PLAN
**Created:** 2026-01-15
**Methodology:** Following task-050b-completion-checklist.md pattern
**Estimated Total Effort:** 13-19 developer-hours
**Current Completion:** 30% (32/106 ACs, 9/24 production files)

---

## INTRODUCTION & INSTRUCTIONS

This checklist guides implementation from 30% to 100% completion. Each phase systematically addresses one domain, ensuring:
1. Tests written FIRST (TDD RED step)
2. Implementation follows (TDD GREEN step)
3. Verification at each step
4. Commit after each logical unit

### How to Use This Checklist

**For Clean-Context Agent:**
1. Read "Current State" section for each phase
2. Work through gaps 1-by-1 in order
3. Mark complete: [✅] when done, including evidence
4. Follow TDD: tests first, then implementation
5. Run verification commands at each step

**Phase Structure:**
```
# Phase N: [Name] (Hours: X-Y)
## Gap N.1: [Specific Gap]
- Current State: [MISSING/INCOMPLETE/COMPLETE]
- Spec Reference: [Line numbers in task-050d spec]
- What Exists: [Brief description]
- What's Missing: [Detailed requirements]
- Implementation Details: [Code from spec or patterns to follow]
- Acceptance Criteria: [AC-XXX to AC-YYY]
- Test Requirements: [Test methods needed]
- Success Criteria: [How to verify]
- [ ] 🔄 Complete: [Mark when done with evidence]
```

---

## PHASE 1: Infrastructure Foundation - Caching + Sanitization
**Estimated Hours: 2-3**
**Goal:** Implement cache and sanitization infrastructure
**Current State:** HealthCheckRegistry tries to use _cache and _sanitizer but both are missing
**Blocked ACs:** 7 caching, 7 security = 14 ACs

### Gap 1.1: Create IHealthCheckCache Interface

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, lines 3921-3944 (Application Prompt)
**What Exists:** Nothing - interface is referenced but doesn't exist
**What's Missing:** Interface with 5 methods for cache operations

**Implementation Details (from spec):**
```csharp
// Application/Health/IHealthCheckCache.cs
namespace Acode.Application.Health;

/// <summary>
/// Cache for health check results to prevent hammering.
/// </summary>
public interface IHealthCheckCache
{
    /// <summary>Attempts to get a cached composite result.</summary>
    bool TryGetCached(out CompositeHealthResult? result);

    /// <summary>Attempts to get a cached individual result.</summary>
    bool TryGetCached(string checkName, out HealthCheckResult? result);

    /// <summary>Caches a composite result.</summary>
    void Cache(CompositeHealthResult result);

    /// <summary>Caches an individual result.</summary>
    void Cache(HealthCheckResult result);

    /// <summary>Clears all cached results.</summary>
    void Clear();
}
```

**Acceptance Criteria Covered:** AC-011, AC-012, AC-013, AC-014, AC-015, AC-016, AC-017

**Test Requirements:**
- None (interface, no implementation to test yet)

**Success Criteria:**
- [ ] File created at src/Acode.Application/Health/IHealthCheckCache.cs
- [ ] Interface compiles without errors
- [ ] All 5 methods present
- [ ] XML documentation complete

**Evidence:**
- [ ] 🔄 Complete: File exists, interface present, 5 methods defined

---

### Gap 1.2: Create HealthCheckCache Implementation

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, AC-011-017 (caching behavior requirements)
**What Exists:** IHealthCheckCache interface (just created)
**What's Missing:** In-memory cache with TTL support for health check results

**Implementation Details:**
Create an in-memory cache with TTL support following specification requirements:
- Healthy results: cache for 30 seconds (configurable)
- Degraded results: cache for 10 seconds (configurable)
- Unhealthy results: cache for 5 seconds (configurable)
- Support cache clear for testing
- Thread-safe access

**Pattern to Follow:**
```csharp
namespace Acode.Infrastructure.Health;

public sealed class HealthCheckCache : IHealthCheckCache
{
    private CompositeHealthResult? _cachedCompositeResult;
    private DateTime _cacheExpiryTime = DateTime.MinValue;
    private readonly Dictionary<string, (HealthCheckResult Result, DateTime ExpiryTime)> _individualCache = new();
    private readonly object _lock = new();

    private readonly TimeSpan _healthyTtl = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _degradedTtl = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _unhealthyTtl = TimeSpan.FromSeconds(5);

    public bool TryGetCached(out CompositeHealthResult? result)
    {
        lock (_lock)
        {
            if (_cachedCompositeResult != null && DateTime.UtcNow < _cacheExpiryTime)
            {
                result = _cachedCompositeResult;
                return true;
            }
            result = null;
            return false;
        }
    }

    public void Cache(CompositeHealthResult result)
    {
        lock (_lock)
        {
            var ttl = result.AggregateStatus switch
            {
                HealthStatus.Healthy => _healthyTtl,
                HealthStatus.Degraded => _degradedTtl,
                HealthStatus.Unhealthy => _unhealthyTtl,
                _ => TimeSpan.FromSeconds(5)
            };

            _cachedCompositeResult = result;
            _cacheExpiryTime = DateTime.UtcNow.Add(ttl);
        }
    }

    // Implement similar for individual results and Clear()
}
```

**Acceptance Criteria Covered:** AC-011, AC-012, AC-013, AC-014, AC-015, AC-016, AC-017

**Test Requirements:** ✅ Tests created in Gap 1.4

**Success Criteria:**
- [ ] File created at src/Acode.Infrastructure/Health/HealthCheckCache.cs
- [ ] Implements IHealthCheckCache fully
- [ ] TTL logic works for all three statuses
- [ ] Thread-safe (uses lock)
- [ ] Clear method empties cache
- [ ] Cache hit returns in <1ms (AC-016)

**Evidence:**
- [ ] 🔄 Complete: File exists, 5 methods implemented, thread-safe, tests passing

---

### Gap 1.3: Create IHealthOutputSanitizer Interface

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, AC-083-089 (sanitization requirements)
**What Exists:** HealthCheckRegistry references _sanitizer but interface doesn't exist
**What's Missing:** Interface for output sanitization

**Implementation Details:**
```csharp
// Application/Health/IHealthOutputSanitizer.cs
namespace Acode.Application.Health;

public interface IHealthOutputSanitizer
{
    /// <summary>Sanitize raw text, redacting sensitive information.</summary>
    string Sanitize(string input);

    /// <summary>Sanitize a health check result object.</summary>
    HealthCheckResult SanitizeResult(HealthCheckResult result);
}
```

**Acceptance Criteria Covered:** AC-083, AC-084, AC-085, AC-086, AC-087, AC-088, AC-089

**Success Criteria:**
- [ ] File created at src/Acode.Application/Health/IHealthOutputSanitizer.cs
- [ ] Interface compiles
- [ ] 2 methods: Sanitize(string) and SanitizeResult(HealthCheckResult)

**Evidence:**
- [ ] 🔄 Complete: Interface created, 2 methods defined

---

### Gap 1.4: Create HealthOutputSanitizer Implementation

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, lines 1706-1761 (troubleshooting example with sanitizer code)
**What Exists:** IHealthOutputSanitizer interface (just created)
**What's Missing:** Implementation with regex patterns for sanitization

**Implementation Details:**
Create sanitizer that redacts:
1. Connection strings: `Server=xxx;Password=xxx` → `Server=[REDACTED];Password=[REDACTED]`
2. User paths (Windows): `C:\Users\john.doe\` → `C:\Users\[REDACTED]\`
3. User paths (Unix): `/home/john.doe/` → `/home/[REDACTED]/`
4. API keys: `api_key=sk-abc123xyz789` → `api_key=[REDACTED]`
5. Tokens: `Bearer eyJhbGc...` → `Bearer [REDACTED]`
6. Stack traces: Redact local paths

**Pattern from spec (lines 1712-1743):**
```csharp
namespace Acode.Infrastructure.Health;

public sealed class HealthOutputSanitizer : IHealthOutputSanitizer
{
    private readonly List<Func<string, string>> _sanitizers = new()
    {
        // Connection strings
        s => Regex.Replace(s,
            @"(Server|Host|Data Source|User Id|Password|Pwd)=[^;]+",
            "$1=[REDACTED]",
            RegexOptions.IgnoreCase),

        // User paths (Windows)
        s => Regex.Replace(s,
            @"[A-Za-z]:\\Users\\[^\\]+",
            @"C:\Users\[REDACTED]",
            RegexOptions.IgnoreCase),

        // User paths (Unix)
        s => Regex.Replace(s,
            @"/home/[^/]+",
            "/home/[REDACTED]"),

        // API keys (various formats)
        s => Regex.Replace(s,
            @"(api[_-]?key|token|secret|bearer)\s*[=:]\s*[A-Za-z0-9+/=_-]{16,}",
            "$1=[REDACTED]",
            RegexOptions.IgnoreCase),

        // Stack traces with paths
        s => Regex.Replace(s,
            @"at .+ in (/[^:]+|[A-Za-z]:\\[^:]+):",
            "at [method] in [REDACTED]:"),

        // IP addresses
        s => Regex.Replace(s,
            @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b",
            "[IP-REDACTED]")
    };

    public string Sanitize(string input)
        => _sanitizers.Aggregate(input, (current, sanitizer) => sanitizer(current));

    public HealthCheckResult SanitizeResult(HealthCheckResult result)
    {
        // Sanitize Description, Details, Suggestion, ErrorMessage
        return result with
        {
            Description = Sanitize(result.Description ?? ""),
            Suggestion = Sanitize(result.Suggestion ?? ""),
            Details = SanitizeDetails(result.Details)
        };
    }

    private IReadOnlyDictionary<string, object>? SanitizeDetails(
        IReadOnlyDictionary<string, object>? details)
    {
        if (details is null) return null;

        return details.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value is string str
                ? (object)Sanitize(str)
                : kvp.Value
        ).AsReadOnly();
    }
}
```

**Acceptance Criteria Covered:** AC-083, AC-084, AC-085, AC-086, AC-087, AC-088, AC-089

**Test Requirements:**
- Tests created in Gap 1.5 below

**Success Criteria:**
- [ ] File created at src/Acode.Infrastructure/Health/HealthOutputSanitizer.cs
- [ ] Implements IHealthOutputSanitizer
- [ ] All 6 sanitization patterns working
- [ ] SanitizeResult applies to all fields
- [ ] Thread-safe (no shared state)

**Evidence:**
- [ ] 🔄 Complete: Implementation complete, all patterns verified

---

### Gap 1.5: Create Unit Tests for Caching & Sanitization

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, lines 2793-2863 (HealthOutputSanitizerTests example)
**What Exists:** HealthCheckRegistryTests.cs (9 methods)
**What's Missing:**
- HealthCheckCacheTests (5-6 test methods)
- HealthOutputSanitizerTests (4 test methods from spec)

**Test Requirements:**

Create: `tests/Acode.Application.Tests/Health/HealthCheckCacheTests.cs`
- Test cache hit returns quickly
- Test cache TTL expiration
- Test different TTLs by status (Healthy 30s, Degraded 10s, Unhealthy 5s)
- Test clear empties cache
- Test thread-safety

Create: `tests/Acode.Application.Tests/Health/HealthOutputSanitizerTests.cs`
(from spec lines 2806-2862):
- Test connection string redaction
- Test user path redaction (Windows and Unix)
- Test API key and token redaction
- Test SanitizeResult applies to all fields

**Example Test from Spec:**
```csharp
public sealed class HealthOutputSanitizerTests
{
    [Theory]
    [InlineData("Server=localhost;Password=secret123", "Server=[REDACTED];Password=[REDACTED]")]
    [InlineData("Data Source=db;User Id=admin;Pwd=hunter2", "Data Source=[REDACTED];User Id=[REDACTED];Pwd=[REDACTED]")]
    public void Sanitize_RedactsConnectionStrings(string input, string expected)
    {
        var sanitizer = new HealthOutputSanitizer();
        var result = sanitizer.Sanitize(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(@"C:\Users\john.doe\acode\db.sqlite", @"C:\Users\[REDACTED]\acode\db.sqlite")]
    [InlineData("/home/john.doe/.acode/db.sqlite", "/home/[REDACTED]/.acode/db.sqlite")]
    public void Sanitize_RedactsUserPaths(string input, string expected)
    {
        // Similar implementation
    }
}
```

**Success Criteria:**
- [ ] HealthCheckCacheTests.cs created with 5+ test methods
- [ ] HealthOutputSanitizerTests.cs created with 4+ test methods
- [ ] All tests passing
- [ ] Coverage includes edge cases (empty strings, null values)

**Evidence:**
- [ ] 🔄 Complete: Both test files created, all tests passing

---

### Gap 1.6: Update DI Configuration (Dependency Injection)

**Current State:** ⚠️ PARTIAL (HealthCheckRegistry references cache/sanitizer but DI not updated)
**Spec Reference:** Look for AddHealthChecks() or similar extension method
**What Exists:** HealthCheckRegistry with constructor dependencies
**What's Missing:** Registration of cache and sanitizer in DI container

**Implementation Details:**
Find or create extension method (e.g., in Program.cs or Extensions/ServiceCollectionExtensions.cs):
```csharp
services.AddSingleton<IHealthCheckCache, HealthCheckCache>();
services.AddSingleton<IHealthOutputSanitizer, HealthOutputSanitizer>();
services.AddSingleton<IHealthCheckRegistry, HealthCheckRegistry>();
```

**Success Criteria:**
- [ ] IHealthCheckCache registered as singleton
- [ ] IHealthOutputSanitizer registered as singleton
- [ ] DI can create HealthCheckRegistry without errors
- [ ] Application builds and runs without DI errors

**Evidence:**
- [ ] 🔄 Complete: DI configured, application starts successfully

---

## PHASE 2: Additional Health Checks
**Estimated Hours: 2-2.5**
**Goal:** Implement SchemaVersionCheck and ConnectionPoolCheck
**Current State:** 3/5 health checks exist (Database, Sync, Storage)
**Missing:** SchemaVersion, ConnectionPool checks
**Blocked ACs:** 13 ACs

### Gap 2.1: Create SchemaVersionCheck

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, lines 1443-1452 (AC-026-032), lines 2553-2629 (SchemaVersionCheckTests)
**What Exists:** DatabaseConnectivityCheck and StorageCheck as patterns
**What's Missing:** SchemaVersionCheck implementation

**Acceptance Criteria (AC-026-032):**
- AC-026: Check named "SchemaVersion" with Category "Database"
- AC-027: Returns Healthy when actual version matches expected version
- AC-028: Returns Degraded when version mismatch (needs migration)
- AC-029: Returns Degraded when migrations table missing (needs init)
- AC-030: Includes expected_version and actual_version in Details
- AC-031: Includes needs_migration boolean in Details
- AC-032: Suggestion includes command: "Run `acode migrate --apply`"

**Implementation Pattern (from spec lines 2101-2152):**
```csharp
namespace Acode.Infrastructure.Health.Checks;

public sealed class SchemaVersionCheck : IHealthCheck
{
    public string Name => "SchemaVersion";
    public string Category => "Database";

    private readonly IDbConnection _connection;
    private readonly string _expectedVersion;

    public SchemaVersionCheck(IDbConnection connection, IOptions<SchemaVersionCheckOptions> options)
    {
        _connection = connection;
        _expectedVersion = options.Value.ExpectedVersion;
    }

    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct)
    {
        try
        {
            // Check if metadata table exists
            var tableExists = await _connection.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__acode_migrations'",
                ct);

            if (tableExists == 0)
            {
                return new HealthCheckResult
                {
                    Name = Name,
                    Status = HealthStatus.Degraded,
                    Duration = TimeSpan.Zero,
                    Description = "Migrations table not found - database may need initialization",
                    Details = new Dictionary<string, object>
                    {
                        ["expected_version"] = _expectedVersion,
                        ["actual_version"] = "N/A",
                        ["needs_init"] = true
                    }
                };
            }

            var version = await _connection.QueryFirstOrDefaultAsync<string>(
                "SELECT version FROM __acode_migrations ORDER BY applied_at DESC LIMIT 1",
                ct);

            var isMatch = version == _expectedVersion;

            return new HealthCheckResult
            {
                Name = Name,
                Status = isMatch ? HealthStatus.Healthy : HealthStatus.Degraded,
                Duration = TimeSpan.Zero,
                Description = isMatch ? "Schema version matches" : $"Schema mismatch: expected {_expectedVersion}, found {version}",
                Details = new Dictionary<string, object>
                {
                    ["expected_version"] = _expectedVersion,
                    ["actual_version"] = version ?? "none",
                    ["needs_migration"] = !isMatch
                },
                Suggestion = isMatch ? null : "Run `acode migrate --apply` to update schema"
            };
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                Name,
                TimeSpan.Zero,
                $"Schema check failed: {ex.Message}",
                "ACODE-HLT-006",
                "Verify database path and connectivity");
        }
    }
}

public sealed class SchemaVersionCheckOptions
{
    public string ExpectedVersion { get; set; } = "1.0.0";
}
```

**Test Requirements:**
- Create SchemaVersionCheckTests.cs with 3 test methods:
  1. CheckAsync_ReturnsHealthy_WhenVersionMatches
  2. CheckAsync_ReturnsDegraded_WhenVersionMismatch
  3. CheckAsync_ReturnsDegraded_WhenTableMissing

**Success Criteria:**
- [ ] File created at src/Acode.Infrastructure/Health/Checks/SchemaVersionCheck.cs
- [ ] Implements IHealthCheck
- [ ] Queries migrations table correctly
- [ ] Returns correct status for match/mismatch/missing
- [ ] All 7 ACs verified
- [ ] Tests passing

**Evidence:**
- [ ] 🔄 Complete: Implementation done, 3/3 tests passing

---

### Gap 2.2: Create ConnectionPoolCheck

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, lines 1453-1461 (AC-033-038)
**What Exists:** Similar checks as patterns
**What's Missing:** ConnectionPoolCheck implementation

**Acceptance Criteria (AC-033-038):**
- AC-033: Check named "ConnectionPool" with Category "Database"
- AC-034: Returns Healthy when pool has available connections
- AC-035: Returns Degraded when pool utilization > 80%
- AC-036: Returns Unhealthy when pool exhausted
- AC-037: Includes active, available, peak, total in Details
- AC-038: Suggestion for exhausted pool: reduce concurrent operations

**Implementation Pattern:**
```csharp
namespace Acode.Infrastructure.Health.Checks;

public sealed class ConnectionPoolCheck : IHealthCheck
{
    public string Name => "ConnectionPool";
    public string Category => "Database";

    private readonly IDbConnectionFactory _factory;

    public ConnectionPoolCheck(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct)
    {
        try
        {
            // Get pool statistics from factory
            var stats = _factory.GetPoolStatistics();

            var utilization = (double)stats.ActiveConnections / stats.MaxConnections;
            var percentFree = (stats.AvailableConnections / (double)stats.MaxConnections) * 100;

            var status = stats.AvailableConnections == 0
                ? HealthStatus.Unhealthy
                : utilization > 0.8
                    ? HealthStatus.Degraded
                    : HealthStatus.Healthy;

            var description = status switch
            {
                HealthStatus.Healthy => $"Connection pool healthy: {stats.AvailableConnections}/{stats.MaxConnections} available",
                HealthStatus.Degraded => $"Connection pool utilization {utilization:P}",
                HealthStatus.Unhealthy => "Connection pool exhausted",
                _ => "Unknown"
            };

            return new HealthCheckResult
            {
                Name = Name,
                Status = status,
                Duration = TimeSpan.Zero,
                Description = description,
                Details = new Dictionary<string, object>
                {
                    ["active"] = stats.ActiveConnections,
                    ["available"] = stats.AvailableConnections,
                    ["peak"] = stats.PeakConnections,
                    ["total"] = stats.MaxConnections
                },
                Suggestion = status == HealthStatus.Unhealthy
                    ? "Connection pool exhausted. Reduce concurrent operations or increase pool size."
                    : null
            };
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                Name,
                TimeSpan.Zero,
                $"Pool check failed: {ex.Message}",
                "ACODE-HLT-007",
                "Verify database connection factory is properly configured");
        }
    }
}
```

**Test Requirements:**
- Create ConnectionPoolCheckTests.cs with 3 test methods:
  1. CheckAsync_ReturnsHealthy_WhenPoolAvailable
  2. CheckAsync_ReturnsDegraded_WhenHighUtilization
  3. CheckAsync_ReturnsUnhealthy_WhenPoolExhausted

**Success Criteria:**
- [ ] File created at src/Acode.Infrastructure/Health/Checks/ConnectionPoolCheck.cs
- [ ] Implements IHealthCheck
- [ ] Gets pool statistics from factory
- [ ] Returns correct status based on utilization
- [ ] All 6 ACs verified
- [ ] Tests passing

**Evidence:**
- [ ] 🔄 Complete: Implementation done, 3/3 tests passing

---

### Gap 2.3: Register New Checks with DI

**Current State:** ⚠️ PARTIAL (checks created but not registered)
**What Exists:** Both new check implementations
**What's Missing:** DI registration and registry initialization

**Implementation:**
```csharp
// In IHostedService or startup code
registry.Register(new SchemaVersionCheck(connection, options));
registry.Register(new ConnectionPoolCheck(factory));
```

**Success Criteria:**
- [ ] Both checks registered with registry
- [ ] Registry.GetRegisteredChecks() returns 5 checks (Connectivity, Schema, Pool, Sync, Storage)
- [ ] CheckAllAsync() calls all 5 checks
- [ ] All 5 checks report successfully

**Evidence:**
- [ ] 🔄 Complete: Checks registered and callable

---

## PHASE 3: Diagnostics Infrastructure
**Estimated Hours: 2-3**
**Goal:** Build diagnostics reporting system
**Current State:** No diagnostics infrastructure
**Missing:** IDiagnosticsReportBuilder, DiagnosticsReportBuilder, and 3 section implementations
**Blocked ACs:** 9 ACs

### Gap 3.1: Create IDiagnosticsReportBuilder Interface

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, Implementation Prompt
**What Exists:** Nothing
**What's Missing:** Interface for building diagnostics reports

**Implementation Details:**
```csharp
// Application/Health/IDiagnosticsReportBuilder.cs
namespace Acode.Application.Health;

public interface IDiagnosticsReportBuilder
{
    /// <summary>Builds a comprehensive diagnostics report.</summary>
    Task<DiagnosticsReport> BuildAsync(CancellationToken ct);

    /// <summary>Builds a report with specific sections.</summary>
    Task<DiagnosticsReport> BuildAsync(DiagnosticsOptions options, CancellationToken ct);
}

public record DiagnosticsReport(Dictionary<string, SectionResult> Sections);

public record SectionResult(string Name, Dictionary<string, object> Data);

public class DiagnosticsOptions
{
    public string[]? Sections { get; set; } // e.g., new[] { "database", "sync", "storage" }
}
```

**Success Criteria:**
- [ ] Interface created at src/Acode.Application/Health/IDiagnosticsReportBuilder.cs
- [ ] Compiles without errors
- [ ] 2 BuildAsync methods defined

**Evidence:**
- [ ] 🔄 Complete: Interface created

---

### Gap 3.2: Create DatabaseDiagnosticsSection

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, AC-067 (database section shows path, size, version, pool)
**What Exists:** Pattern from health checks
**What's Missing:** Diagnostics section implementation

**Implementation Pattern:**
```csharp
// Infrastructure/Health/Diagnostics/DatabaseDiagnosticsSection.cs
namespace Acode.Infrastructure.Health.Diagnostics;

public interface IDiagnosticsSection
{
    string Name { get; }
    Task<Dictionary<string, object>> GatherAsync(CancellationToken ct);
}

public sealed class DatabaseDiagnosticsSection : IDiagnosticsSection
{
    public string Name => "database";

    private readonly IDbConnectionFactory _factory;
    private readonly IConfiguration _config;

    public async Task<Dictionary<string, object>> GatherAsync(CancellationToken ct)
    {
        var dbPath = _config["Database:Path"];
        var fileInfo = new FileInfo(dbPath);

        return new Dictionary<string, object>
        {
            ["path"] = dbPath,
            ["size_bytes"] = fileInfo.Length,
            ["size_human"] = FormatBytes(fileInfo.Length),
            ["schema_version"] = await GetSchemaVersionAsync(ct),
            ["pool_stats"] = new
            {
                active = _factory.GetPoolStatistics().ActiveConnections,
                available = _factory.GetPoolStatistics().AvailableConnections,
                total = _factory.GetPoolStatistics().MaxConnections
            }
        };
    }

    private async Task<string> GetSchemaVersionAsync(CancellationToken ct)
    {
        // Query __acode_migrations table
        // ... implementation
        return "1.0.5";
    }
}
```

**Success Criteria:**
- [ ] File created at src/Acode.Infrastructure/Health/Diagnostics/DatabaseDiagnosticsSection.cs
- [ ] Implements IDiagnosticsSection
- [ ] GatherAsync returns path, size, version, pool stats
- [ ] Returns human-readable format (e.g., "12.4 MB")

**Evidence:**
- [ ] 🔄 Complete: Implementation done, returns correct data

---

### Gap 3.3: Create SyncDiagnosticsSection

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, AC-068 (sync section shows queue depth, last sync, processing time)
**What Exists:** Pattern from database section
**What's Missing:** Sync diagnostics section

**Implementation Pattern:**
```csharp
// Infrastructure/Health/Diagnostics/SyncDiagnosticsSection.cs
public sealed class SyncDiagnosticsSection : IDiagnosticsSection
{
    public string Name => "sync";

    private readonly ISyncQueueMetrics _metrics;

    public async Task<Dictionary<string, object>> GatherAsync(CancellationToken ct)
    {
        var queueDepth = await _metrics.GetQueueDepthAsync(ct);
        var lastSync = await _metrics.GetLastProcessedTimeAsync(ct);
        var avgProcessTime = await _metrics.GetAverageProcessingTimeAsync(ct);

        return new Dictionary<string, object>
        {
            ["queue_depth"] = queueDepth,
            ["last_sync"] = lastSync,
            ["last_sync_ago"] = FormatTimeSpan(DateTime.UtcNow - lastSync),
            ["avg_processing_time_ms"] = avgProcessTime.TotalMilliseconds,
            ["processing_rate_per_second"] = await _metrics.GetProcessingRateAsync(ct)
        };
    }
}
```

**Success Criteria:**
- [ ] File created at src/Acode.Infrastructure/Health/Diagnostics/SyncDiagnosticsSection.cs
- [ ] Returns queue depth, last sync timestamp, processing metrics
- [ ] Formats timestamps as human-readable ("2 minutes ago")

**Evidence:**
- [ ] 🔄 Complete: Implementation done

---

### Gap 3.4: Create StorageDiagnosticsSection

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, AC-069 (storage section shows size, available, growth rate)
**What Exists:** Pattern from other sections
**What's Missing:** Storage diagnostics section

**Implementation Pattern:**
```csharp
// Infrastructure/Health/Diagnostics/StorageDiagnosticsSection.cs
public sealed class StorageDiagnosticsSection : IDiagnosticsSection
{
    public string Name => "storage";

    private readonly IFileSystem _fileSystem;

    public async Task<Dictionary<string, object>> GatherAsync(CancellationToken ct)
    {
        var (available, total) = _fileSystem.GetDiskSpaceInfo();
        var dbSize = _fileSystem.GetFileSize("acode.db");

        return new Dictionary<string, object>
        {
            ["database_size_bytes"] = dbSize,
            ["database_size_human"] = FormatBytes(dbSize),
            ["available_bytes"] = available,
            ["available_human"] = FormatBytes(available),
            ["total_bytes"] = total,
            ["percent_free"] = (available / (double)total) * 100,
            ["growth_rate_per_day"] = await CalculateGrowthRateAsync(ct)
        };
    }
}
```

**Success Criteria:**
- [ ] File created at src/Acode.Infrastructure/Health/Diagnostics/StorageDiagnosticsSection.cs
- [ ] Returns size, available space, growth rate
- [ ] Formats bytes as human-readable (MB, GB)

**Evidence:**
- [ ] 🔄 Complete: Implementation done

---

### Gap 3.5: Create DiagnosticsReportBuilder Implementation

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, AC-062-070 (diagnostics command requirements)
**What Exists:** IDiagnosticsReportBuilder interface
**What's Missing:** Implementation that gathers all sections

**Implementation Pattern:**
```csharp
// Infrastructure/Health/DiagnosticsReportBuilder.cs
public sealed class DiagnosticsReportBuilder : IDiagnosticsReportBuilder
{
    private readonly IEnumerable<IDiagnosticsSection> _sections;
    private readonly ILogger<DiagnosticsReportBuilder> _logger;

    public async Task<DiagnosticsReport> BuildAsync(CancellationToken ct)
    {
        return await BuildAsync(new DiagnosticsOptions(), ct);
    }

    public async Task<DiagnosticsReport> BuildAsync(DiagnosticsOptions options, CancellationToken ct)
    {
        var sections = _sections
            .Where(s => options.Sections == null || options.Sections.Contains(s.Name))
            .ToList();

        var tasks = sections.Select(async s =>
        {
            try
            {
                var data = await s.GatherAsync(ct);
                return (s.Name, (SectionResult)new SectionResult(s.Name, data));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to gather {Section} diagnostics", s.Name);
                return (s.Name, (SectionResult)new SectionResult(s.Name, new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                }));
            }
        });

        var results = await Task.WhenAll(tasks);
        return new DiagnosticsReport(results.ToDictionary(r => r.Name, r => r.Item2));
    }
}
```

**Success Criteria:**
- [ ] File created at src/Acode.Infrastructure/Health/DiagnosticsReportBuilder.cs
- [ ] Implements IDiagnosticsReportBuilder
- [ ] Gathers all sections in parallel
- [ ] Filters sections based on DiagnosticsOptions
- [ ] Handles exceptions gracefully

**Evidence:**
- [ ] 🔄 Complete: Implementation done, sections gathered successfully

---

## PHASE 4: CLI Commands
**Estimated Hours: 3-4**
**Goal:** Implement status and diagnostics commands
**Current State:** No CLI commands
**Missing:** StatusCommand, DiagnosticsCommand, and formatters
**Blocked ACs:** 18 ACs

### Gap 4.1: Create StatusCommand

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, AC-053-061 (status command requirements)
**What Exists:** Pattern from other CLI commands in codebase
**What's Missing:** StatusCommand implementation

**Acceptance Criteria (AC-053-061):**
- AC-053: `acode status` command exists
- AC-054: Default output is human-readable text format
- AC-055: `--format json` produces valid JSON output
- AC-056: `--verbose` shows detailed component information
- AC-057: `--no-cache` bypasses cache
- AC-058: `--timeout` sets overall timeout (default 30s)
- AC-059: Shows overall status with visual indicator (✓/⚠/✗)
- AC-060: Shows each component with name, duration, description
- AC-061: Shows suggestions for non-healthy components

**Implementation Pattern:**
```csharp
// Cli/Commands/StatusCommand.cs
namespace Acode.Cli.Commands;

[Command("status", Description = "Show health status of database and system")]
public sealed class StatusCommand : ICommand
{
    [Option("--format", Description = "Output format: text or json", Default = "text")]
    public string Format { get; set; } = "text";

    [Option("--verbose", Description = "Show detailed information")]
    public bool Verbose { get; set; }

    [Option("--no-cache", Description = "Bypass cache")]
    public bool NoCache { get; set; }

    [Option("--timeout", Description = "Overall timeout in seconds", Default = 30)]
    public int TimeoutSeconds { get; set; } = 30;

    private readonly IHealthCheckRegistry _registry;
    private readonly IOutputFormatter _formatter;

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

            var result = await _registry.CheckAllAsync(cts.Token);

            // Format output
            var output = Format.ToLower() switch
            {
                "json" => await _formatter.FormatAsJsonAsync(result),
                "text" => await _formatter.FormatAsTextAsync(result, Verbose),
                _ => throw new InvalidOperationException($"Unknown format: {Format}")
            };

            await Console.Out.WriteLineAsync(output);

            // Return exit code
            return (int)result.AggregateStatus;
        }
        catch (OperationCanceledException)
        {
            await Console.Error.WriteLineAsync("Status check timed out");
            return 3;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            return 3;
        }
    }
}
```

**Test Requirements:**
- Create StatusCommandE2ETests.cs with 7 test methods (from spec lines 3030-3147)

**Success Criteria:**
- [ ] File created at src/Acode.Cli/Commands/StatusCommand.cs
- [ ] Implements ICommand
- [ ] Has --format, --verbose, --no-cache, --timeout options
- [ ] Returns exit codes: 0=Healthy, 1=Degraded, 2=Unhealthy, 3=Error
- [ ] Works with formatters

**Evidence:**
- [ ] 🔄 Complete: Command implemented, E2E tests passing

---

### Gap 4.2: Create DiagnosticsCommand

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, AC-062-070 (diagnostics command requirements)
**What Exists:** StatusCommand pattern
**What's Missing:** DiagnosticsCommand implementation

**Acceptance Criteria (AC-062-070):**
- AC-062: `acode diagnostics` command exists
- AC-063: Default output shows all sections
- AC-064: `--section <name>` filters to specific section
- AC-065: `--section a,b` supports multiple sections
- AC-066: `--format json` produces valid JSON
- AC-067: Database section shows path, size, version, pool
- AC-068: Sync section shows queue depth, last sync, processing time
- AC-069: Storage section shows size, available, growth rate
- AC-070: Invalid section name shows helpful error

**Implementation Pattern:**
```csharp
// Cli/Commands/DiagnosticsCommand.cs
[Command("diagnostics", Description = "Show detailed system diagnostics")]
public sealed class DiagnosticsCommand : ICommand
{
    [Option("--section", Description = "Section(s) to report: database,sync,storage")]
    public string? Sections { get; set; }

    [Option("--format", Description = "Output format: text or json", Default = "text")]
    public string Format { get; set; } = "text";

    private readonly IDiagnosticsReportBuilder _builder;
    private readonly IOutputFormatter _formatter;

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            var options = new DiagnosticsOptions
            {
                Sections = Sections?.Split(',') ?? null
            };

            var report = await _builder.BuildAsync(options, ct);

            var output = Format.ToLower() switch
            {
                "json" => JsonSerializer.Serialize(report),
                "text" => FormatAsText(report),
                _ => throw new InvalidOperationException($"Unknown format: {Format}")
            };

            await Console.Out.WriteLineAsync(output);
            return 0;
        }
        catch (ArgumentException ex)
        {
            await Console.Error.WriteLineAsync($"Invalid section: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            return 3;
        }
    }
}
```

**Test Requirements:**
- E2E tests for diagnostics command

**Success Criteria:**
- [ ] File created at src/Acode.Cli/Commands/DiagnosticsCommand.cs
- [ ] Implements ICommand
- [ ] Has --section and --format options
- [ ] Supports comma-separated sections
- [ ] Returns error on invalid section name

**Evidence:**
- [ ] 🔄 Complete: Command implemented

---

## PHASE 5: Output Formatters
**Estimated Hours: 1-2**
**Goal:** Format output for display (text and JSON)
**Current State:** No formatters
**Missing:** TextHealthFormatter, JsonHealthFormatter
**Blocked ACs:** 10 ACs

### Gap 5.1: Create TextHealthFormatter

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, AC-077-078 (text output with colors)
**What Exists:** Pattern from other commands
**What's Missing:** Human-readable formatter with colors

**Acceptance Criteria (AC-077-078):**
- AC-077: Text output uses color (green/yellow/red) when terminal supports
- AC-078: Text output degrades gracefully without color

**Implementation Pattern:**
```csharp
// Cli/Formatters/TextHealthFormatter.cs
public sealed class TextHealthFormatter
{
    public string FormatHealthStatus(CompositeHealthResult result, bool verbose = false)
    {
        var sb = new StringBuilder();
        var supportsColor = Console.IsOutputRedirected == false;

        // Overall status
        var (icon, color) = result.AggregateStatus switch
        {
            HealthStatus.Healthy => ("✓", ConsoleColor.Green),
            HealthStatus.Degraded => ("⚠", ConsoleColor.Yellow),
            HealthStatus.Unhealthy => ("✗", ConsoleColor.Red),
            _ => ("?", ConsoleColor.Gray)
        };

        sb.AppendLine("┌─────────────────────────────────────────────────┐");
        sb.AppendLine("│ ACODE HEALTH STATUS                             │");
        sb.AppendLine("├─────────────────────────────────────────────────┤");

        if (supportsColor)
            Console.ForegroundColor = color;

        sb.AppendLine($"│ Overall Status: {icon} {result.AggregateStatus}");

        if (supportsColor)
            Console.ResetColor();

        // Component details
        foreach (var check in result.Results)
        {
            var checkIcon = check.Status switch
            {
                HealthStatus.Healthy => "✓",
                HealthStatus.Degraded => "⚠",
                HealthStatus.Unhealthy => "✗",
                _ => "?"
            };

            sb.AppendLine($"│ {checkIcon} {check.Name,-20} ({check.Duration.TotalMilliseconds:F0}ms)");

            if (verbose)
            {
                sb.AppendLine($"│   {check.Description}");
                if (check.Suggestion != null)
                    sb.AppendLine($"│   → {check.Suggestion}");
            }
        }

        sb.AppendLine("└─────────────────────────────────────────────────┘");
        return sb.ToString();
    }
}
```

**Success Criteria:**
- [ ] File created at src/Acode.Cli/Formatters/TextHealthFormatter.cs
- [ ] Uses colored output (✓ green, ⚠ yellow, ✗ red)
- [ ] Degrades gracefully without color
- [ ] Verbose mode shows descriptions and suggestions
- [ ] Formats timing with 1 decimal place

**Evidence:**
- [ ] 🔄 Complete: Formatter implemented, output renders correctly

---

### Gap 5.2: Create JsonHealthFormatter

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, AC-079-081 (JSON output structure)
**What Exists:** Pattern from text formatter
**What's Missing:** JSON formatter with proper structure

**Acceptance Criteria (AC-079-081):**
- AC-079: JSON output includes `status`, `timestamp`, `checks` array
- AC-080: JSON output includes `duration_ms` for each check
- AC-081: JSON timestamp uses ISO 8601 format

**Implementation Pattern:**
```csharp
// Cli/Formatters/JsonHealthFormatter.cs
public sealed class JsonHealthFormatter
{
    public string FormatAsJson(CompositeHealthResult result)
    {
        var json = new
        {
            status = result.AggregateStatus.ToString(),
            timestamp = result.CheckedAt.ToString("O"), // ISO 8601
            total_duration_ms = result.TotalDuration.TotalMilliseconds,
            checks = result.Results.Select(c => new
            {
                name = c.Name,
                status = c.Status.ToString(),
                duration_ms = c.Duration.TotalMilliseconds,
                description = c.Description,
                suggestion = c.Suggestion,
                details = c.Details
            })
        };

        return JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true });
    }
}
```

**Success Criteria:**
- [ ] File created at src/Acode.Cli/Formatters/JsonHealthFormatter.cs
- [ ] Outputs valid JSON
- [ ] Includes all required fields
- [ ] Timestamps in ISO 8601 format
- [ ] Formatted with proper indentation

**Evidence:**
- [ ] 🔄 Complete: Formatter implemented, valid JSON output

---

## PHASE 6: Testing & Validation
**Estimated Hours: 3-4**
**Goal:** Complete test coverage across all components
**Current State:** 1 test file (HealthCheckRegistryTests)
**Missing:** 8+ additional test files with 60+ test methods
**Blocked ACs:** Testing requirements (AC-100-106)

### Gap 6.1: Create Database Check Tests

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, lines 2428-2549 (DatabaseConnectivityCheckTests)
**What Exists:** HealthCheckRegistryTests pattern
**What's Missing:** 6 test methods for DatabaseConnectivityCheck

**Test Methods from Spec:**
1. CheckAsync_ReturnsHealthy_WhenDatabaseResponds
2. CheckAsync_ReturnsUnhealthy_WhenConnectionFails
3. CheckAsync_ReturnsUnhealthy_WhenTimeout
4. CheckAsync_ReturnsDegraded_WhenSlowButSuccessful
5. CheckAsync_IncludesDuration_InResult
6. Plus parametrized tests for various timeout scenarios

**Success Criteria:**
- [ ] File created at tests/Acode.Application.Tests/Health/DatabaseConnectivityCheckTests.cs
- [ ] 6+ test methods
- [ ] All tests passing
- [ ] Coverage: normal response, timeout, locked database, slow response

**Evidence:**
- [ ] 🔄 Complete: Tests created, 6/6 passing

---

### Gap 6.2: Create Schema Version Check Tests

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, lines 2553-2629 (SchemaVersionCheckTests)
**What Exists:** Pattern from database connectivity tests
**What's Missing:** 3 test methods

**Test Methods from Spec:**
1. CheckAsync_ReturnsHealthy_WhenVersionMatches
2. CheckAsync_ReturnsDegraded_WhenVersionMismatch
3. CheckAsync_ReturnsDegraded_WhenTableMissing

**Success Criteria:**
- [ ] File created at tests/Acode.Application.Tests/Health/SchemaVersionCheckTests.cs
- [ ] 3+ test methods
- [ ] All tests passing

**Evidence:**
- [ ] 🔄 Complete: Tests created, 3/3 passing

---

### Gap 6.3: Create Storage Check Tests

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, lines 2633-2716 (StorageCheckTests)
**What Exists:** Pattern from other check tests
**What's Missing:** 5 test methods

**Test Methods from Spec:**
1. CheckAsync_ReturnsHealthy_WhenSpaceAboveThreshold
2. CheckAsync_ReturnsDegraded_WhenLowSpace
3. CheckAsync_ReturnsUnhealthy_WhenCriticalSpace
4. CheckAsync_IncludesDatabaseSize_InDetails

**Success Criteria:**
- [ ] File created at tests/Acode.Application.Tests/Health/StorageCheckTests.cs
- [ ] 5+ test methods
- [ ] All tests passing
- [ ] Mocks IFileSystem correctly

**Evidence:**
- [ ] 🔄 Complete: Tests created, 5/5 passing

---

### Gap 6.4: Create Sync Queue Check Tests

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, lines 2720-2789 (SyncQueueCheckTests)
**What Exists:** Pattern from other checks
**What's Missing:** 4 test methods

**Test Methods from Spec:**
1. CheckAsync_ReturnsHealthy_WhenQueueEmpty
2. CheckAsync_ReturnsDegraded_WhenQueueHigh
3. CheckAsync_ReturnsUnhealthy_WhenSyncStalled

**Success Criteria:**
- [ ] File created at tests/Acode.Application.Tests/Health/SyncQueueCheckTests.cs
- [ ] 4+ test methods
- [ ] All tests passing

**Evidence:**
- [ ] 🔄 Complete: Tests created, 4/4 passing

---

### Gap 6.5: Create Sanitizer Tests (Already Done in Phase 1)

**Status:** Already created in Gap 1.5 ✅

---

### Gap 6.6: Create Integration Tests

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, lines 2868-3019 (Integration tests)
**What Exists:** Pattern from unit tests
**What's Missing:** 2 integration test files

**Files to Create:**
1. tests/Acode.Integration.Tests/Health/HealthCheckIntegrationTests.cs
   - Tests with real SQLite database
   - 4 test methods from spec (lines 2887-2943)

2. tests/Acode.Integration.Tests/Health/DiagnosticsReportIntegrationTests.cs
   - Tests with real database and diagnostics
   - 4 test methods from spec (lines 2966-3018)

**Success Criteria:**
- [ ] HealthCheckIntegrationTests.cs created with 4 tests
- [ ] DiagnosticsReportIntegrationTests.cs created with 4 tests
- [ ] All tests passing
- [ ] Real database operations verified

**Evidence:**
- [ ] 🔄 Complete: Integration tests created, 8/8 passing

---

### Gap 6.7: Create E2E Tests

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, lines 3025-3147 (StatusCommandE2ETests)
**What Exists:** Pattern from other E2E tests
**What's Missing:** E2E test file

**Test Methods from Spec:**
1. StatusCommand_WithHealthyDatabase_ReturnsZeroExitCode
2. StatusCommand_WithDegradedStatus_ReturnsOneExitCode
3. StatusCommand_WithUnhealthyStatus_ReturnsTwoExitCode
4. StatusCommand_WithJsonFormat_ReturnsValidJson
5. StatusCommand_WithVerbose_ShowsDetailedOutput
6. DiagnosticsCommand_ShowsComprehensiveReport
7. DiagnosticsCommand_WithSectionFilter_ShowsOnlyThatSection
8. DiagnosticsCommand_DoesNotExposeSecrets

**Success Criteria:**
- [ ] File created at tests/Acode.E2E.Tests/Health/StatusCommandE2ETests.cs
- [ ] 8 test methods
- [ ] Tests execute actual CLI commands
- [ ] All tests passing
- [ ] Exit codes verified

**Evidence:**
- [ ] 🔄 Complete: E2E tests created, 8/8 passing

---

### Gap 6.8: Create Performance Benchmarks

**Current State:** ❌ MISSING
**Spec Reference:** Task-050d, lines 3152-3240 (Performance benchmarks)
**What Exists:** Pattern from infrastructure benchmarks
**What's Missing:** Performance benchmark file

**Benchmark Methods from Spec:**
1. FullHealthCheck (target: <100ms)
2. DatabaseConnectivityCheck (target: <50ms)
3. SchemaVersionCheck (target: <50ms)
4. FullDiagnosticsReport (target: <5s)
5. SingleSectionDiagnostics (target: <1s)

**Success Criteria:**
- [ ] File created at tests/Acode.Benchmarks/Health/HealthCheckBenchmarks.cs
- [ ] 5 benchmark methods
- [ ] All benchmarks pass performance targets:
  - Full health check: <100ms
  - Individual probe: <50ms
  - Full diagnostics: <5s
  - Single section: <1s

**Evidence:**
- [ ] 🔄 Complete: Benchmarks created, all targets met

---

### Gap 6.9: Final Test Summary

**Test Coverage Summary:**

| Test File | Test Count | Status |
|-----------|-----------|--------|
| HealthCheckRegistryTests | 9 | ✅ |
| DatabaseConnectivityCheckTests | 6 | ✅ |
| SchemaVersionCheckTests | 3 | ✅ |
| StorageCheckTests | 5 | ✅ |
| SyncQueueCheckTests | 4 | ✅ |
| HealthOutputSanitizerTests | 4 | ✅ |
| HealthCheckIntegrationTests | 4 | ✅ |
| DiagnosticsReportIntegrationTests | 4 | ✅ |
| StatusCommandE2ETests | 8 | ✅ |
| HealthCheckBenchmarks | 5 | ✅ |
| **TOTAL** | **52** | **✅** |

**Coverage Analysis:**
- Unit tests: 35 methods across 6 files
- Integration tests: 8 methods across 2 files
- E2E tests: 8 methods
- Performance benchmarks: 5 benchmarks
- Total test methods: 56+ (exceeds spec requirement)

**Acceptance:**
- [ ] All 52+ test methods passing
- [ ] Build: 0 errors, 0 warnings
- [ ] Performance benchmarks within targets
- [ ] Coverage includes happy path, error handling, edge cases

---

## FINAL VERIFICATION CHECKLIST

### Production Code Verification

- [ ] **Core Interfaces (5 files, 100% complete)**
  - [ ] IHealthCheck.cs ✅
  - [ ] IHealthCheckRegistry.cs ✅
  - [ ] IHealthCheckCache.cs ✅ (created in Phase 1)
  - [ ] HealthCheckResult.cs ✅
  - [ ] CompositeHealthResult.cs ✅
  - [ ] HealthStatus.cs ✅

- [ ] **Infrastructure Implementations (13 files)**
  - [ ] HealthCheckRegistry.cs ✅
  - [ ] HealthCheckCache.cs ✅ (created in Phase 1)
  - [ ] HealthOutputSanitizer.cs ✅ (created in Phase 1)
  - [ ] HealthCheckRateLimiter.cs ✅ (created in Phase 1)
  - [ ] DatabaseConnectivityCheck.cs ✅
  - [ ] SchemaVersionCheck.cs ✅ (created in Phase 2)
  - [ ] ConnectionPoolCheck.cs ✅ (created in Phase 2)
  - [ ] SyncQueueCheck.cs ✅
  - [ ] StorageCheck.cs ✅
  - [ ] DiagnosticsReportBuilder.cs ✅ (created in Phase 3)
  - [ ] DatabaseDiagnosticsSection.cs ✅ (created in Phase 3)
  - [ ] SyncDiagnosticsSection.cs ✅ (created in Phase 3)
  - [ ] StorageDiagnosticsSection.cs ✅ (created in Phase 3)

- [ ] **CLI Commands & Formatters (4 files)**
  - [ ] StatusCommand.cs ✅ (created in Phase 4)
  - [ ] DiagnosticsCommand.cs ✅ (created in Phase 4)
  - [ ] TextHealthFormatter.cs ✅ (created in Phase 5)
  - [ ] JsonHealthFormatter.cs ✅ (created in Phase 5)

### Code Quality Verification

- [ ] **No NotImplementedException Scan**
  ```bash
  grep -r "NotImplementedException" src/Acode.Application/Health/
  grep -r "NotImplementedException" src/Acode.Infrastructure/Health/
  grep -r "NotImplementedException" src/Acode.Cli/Commands/
  grep -r "NotImplementedException" src/Acode.Cli/Formatters/
  # Expected: No matches
  ```

- [ ] **No TODO/FIXME Markers**
  ```bash
  grep -r "TODO\|FIXME\|HACK" src/Acode.*/Health/ src/Acode.Cli/
  # Expected: No matches (or only acceptable future optimization TODOs)
  ```

- [ ] **Build Status**
  ```bash
  dotnet build
  # Expected: Build succeeded, 0 errors, 0 warnings
  ```

### Test Verification

- [ ] **All Tests Passing**
  ```bash
  dotnet test --filter "FullyQualifiedName~Health" --verbosity normal
  # Expected: 52+ tests passing, 0 failed
  ```

- [ ] **Performance Benchmarks**
  ```bash
  # Run performance tests
  # Expected: All benchmarks within targets
  # - Full health check: <100ms
  # - Individual probe: <50ms
  # - Diagnostics: <5s
  ```

### Acceptance Criteria Verification

- [ ] **Framework ACs (AC-001-010): 10/10 ✅**
  - IHealthCheck exists
  - IHealthCheckRegistry exists
  - Parallel execution
  - Exception handling
  - Timeout support
  - Logging

- [ ] **Caching ACs (AC-011-017): 7/7 ✅**
  - Cache interface exists
  - TTL support (30s healthy, 10s degraded, 5s unhealthy)
  - Cache bypass
  - Performance (<1ms)

- [ ] **Database Connectivity (AC-018-025): 8/8 ✅**
  - Check exists with correct name
  - Status based on response time
  - Error handling
  - Timeout configuration

- [ ] **Schema Version (AC-026-032): 7/7 ✅**
  - Check exists
  - Status for match/mismatch/missing table
  - Proper suggestions

- [ ] **Connection Pool (AC-033-038): 6/6 ✅**
  - Check exists
  - Status based on utilization
  - Proper suggestions

- [ ] **Sync Queue (AC-039-044): 6/6 ✅**
  - Check exists
  - Status for queue depth/staleness
  - Proper suggestions

- [ ] **Storage (AC-045-052): 8/8 ✅**
  - Check exists
  - Status based on free space %
  - Cross-platform support

- [ ] **Status Command (AC-053-061): 9/9 ✅**
  - Command exists
  - Text and JSON output
  - Verbose mode
  - Exit codes

- [ ] **Diagnostics Command (AC-062-070): 9/9 ✅**
  - Command exists
  - Section filtering
  - All sections implemented

- [ ] **Exit Codes (AC-071-076): 6/6 ✅**
  - 0 = Healthy
  - 1 = Degraded
  - 2 = Unhealthy
  - 3 = Error

- [ ] **Output Formatting (AC-077-082): 6/6 ✅**
  - Text with colors
  - Graceful degradation
  - JSON with correct structure

- [ ] **Security & Sanitization (AC-083-089): 7/7 ✅**
  - Connection strings redacted
  - Paths redacted
  - API keys redacted
  - Tokens redacted
  - Stack traces redacted
  - Applied to all output

- [ ] **Rate Limiting (AC-090-093): 4/4 ✅**
  - Rate limiter implemented
  - Prevents check hammering

- [ ] **Performance (AC-094-099): 6/6 ✅**
  - Health check <100ms
  - Individual probe <50ms
  - Diagnostics <5s
  - JSON serialization <5ms
  - Cache hit <1ms

- [ ] **Testing (AC-100-106): 7/7 ✅**
  - Unit tests for each check
  - Registry tests
  - Cache TTL tests
  - Sanitizer tests
  - Integration tests
  - E2E tests
  - Performance benchmarks

### Summary

- [ ] **Total Production Files: 24/24 (100%) ✅**
  - Domain/Application: 7/7 ✅
  - Infrastructure: 13/13 ✅
  - CLI: 4/4 ✅

- [ ] **Total Test Files: 10/10 (100%) ✅**
  - Unit tests: 6/6 ✅
  - Integration tests: 2/2 ✅
  - E2E tests: 1/1 ✅
  - Benchmarks: 1/1 ✅

- [ ] **Total ACs: 106/106 (100%) ✅**
  - All domains: 100% verified

- [ ] **Build Status: ✅ PASSING**
  - 0 Errors
  - 0 Warnings

- [ ] **Test Status: ✅ PASSING**
  - 50+ tests passing
  - 0 failures

---

## COMMIT STRATEGY

Follow Git Workflow (one commit per phase or logical unit):

```bash
# Phase 1
git commit -m "feat(task-050d): add health check caching and output sanitization

Phase 1: Infrastructure Foundation
- Add IHealthCheckCache interface and in-memory implementation
- Add IHealthOutputSanitizer interface and regex-based sanitizer
- Add HealthCheckRateLimiter for preventing check hammering
- Update DI configuration for new services
- All 14 caching and security ACs verified working

Tests passing: 4/4 (HealthCheckCacheTests, HealthOutputSanitizerTests)
Build: 0 errors, 0 warnings"

# Phase 2
git commit -m "feat(task-050d): add schema version and connection pool checks

Phase 2: Additional Health Checks
- Add SchemaVersionCheck for migration version tracking
- Add ConnectionPoolCheck for pool statistics
- Register both checks with DI container
- Coverage: 13 ACs (all database checks now complete)

Tests passing: 6/6 (SchemaVersionCheckTests, ConnectionPoolCheckTests)
Build: 0 errors, 0 warnings"

# Continue for each phase...
```

---

**ESTIMATED TIMELINE:** 13-19 developer-hours
**STATUS:** Ready for Phase 1 implementation
**NEXT ACTION:** Start Gap 1.1 (IHealthCheckCache interface)

---
